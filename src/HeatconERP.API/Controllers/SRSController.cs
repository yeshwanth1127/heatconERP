using HeatconERP.Application.Services.Srs;
using HeatconERP.Application.Services.Procurement;
using HeatconERP.Domain.Enums.Inventory;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SRSController : ControllerBase
{
    private readonly ISrsService _srs;
    private readonly IProcurementService _procurement;
    private readonly HeatconDbContext _db;

    public SRSController(ISrsService srs, IProcurementService procurement, HeatconDbContext db)
    {
        _srs = srs;
        _procurement = procurement;
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<SrsDto>> Create([FromBody] CreateSrsRequest req, CancellationToken ct)
    {
        try
        {
            var srs = await _srs.CreateSrsFromWorkOrderAsync(req.WorkOrderId, req.Lines, ct);
            return Ok(new SrsDto(srs.Id, srs.WorkOrderId, srs.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SrsListDto>>> List(
        [FromQuery] string? status,
        [FromQuery] Guid? workOrderId,
        [FromQuery] int limit = 200,
        CancellationToken ct = default)
    {
        var q = _db.SRSs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(x => x.Status.ToString() == status);

        if (workOrderId.HasValue)
            q = q.Where(x => x.WorkOrderId == workOrderId.Value);

        var itemsRaw = await (
                from s in q
                join wo in _db.WorkOrders.AsNoTracking()
                    on s.WorkOrderId equals wo.Id into wog
                from wo in wog.DefaultIfEmpty()
                orderby s.CreatedAt descending
                select new
                {
                    s.Id,
                    s.WorkOrderId,
                    WorkOrderNumber = (string?)wo.OrderNumber,
                    Status = s.Status.ToString(),
                    s.CreatedAt
                })
            .Take(limit)
            .ToListAsync(ct);

        var items = itemsRaw
            .Select(x => new SrsListDto(
                x.Id,
                x.WorkOrderId,
                x.WorkOrderNumber ?? $"WO-{x.WorkOrderId.ToString()[..8]}",
                x.Status,
                x.CreatedAt))
            .ToList();

        return Ok(items);
    }

    [HttpGet("{srsId:guid}")]
    public async Task<ActionResult<SrsDetailDto>> GetById(Guid srsId, CancellationToken ct = default)
    {
        var srs = await _db.SRSs
            .AsNoTracking()
            .Include(x => x.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .Include(x => x.LineItems)
            .ThenInclude(li => li.BatchAllocations)
            .ThenInclude(a => a.StockBatch)
            .FirstOrDefaultAsync(x => x.Id == srsId, ct);

        if (srs == null) return NotFound();

        var workOrderNumber = await _db.WorkOrders
            .AsNoTracking()
            .Where(w => w.Id == srs.WorkOrderId)
            .Select(w => w.OrderNumber)
            .FirstOrDefaultAsync(ct);
        workOrderNumber ??= $"WO-{srs.WorkOrderId.ToString()[..8]}";

        var dto = new SrsDetailDto(
            srs.Id,
            srs.WorkOrderId,
            workOrderNumber,
            srs.Status.ToString(),
            srs.CreatedAt,
            srs.LineItems
                .OrderBy(li => li.CreatedAt)
                .Select(li => new SrsLineItemDetailDto(
                    li.Id,
                    li.MaterialVariantId,
                    li.MaterialVariant.SKU,
                    li.MaterialVariant.Grade,
                    li.MaterialVariant.Size,
                    li.MaterialVariant.Unit,
                    li.RequiredQuantity,
                    li.BatchAllocations
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => new SrsAllocationDto(
                            a.Id,
                            a.StockBatchId,
                            a.StockBatch.BatchNumber,
                            a.ReservedQuantity,
                            a.ConsumedQuantity))
                        .ToList()
                ))
                .ToList()
        );

        return Ok(dto);
    }

    [HttpPost("{srsId:guid}/approve")]
    public async Task<ActionResult<SrsDto>> Approve(Guid srsId, [FromQuery] string? approvedBy, CancellationToken ct)
    {
        try
        {
            var srs = await _srs.ApproveSrsAsync(srsId, approvedBy, ct);
            return Ok(new SrsDto(srs.Id, srs.WorkOrderId, srs.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("{srsId:guid}/allocate-fifo")]
    public async Task<IActionResult> AllocateFifo(Guid srsId, CancellationToken ct)
    {
        try
        {
            await _srs.AllocateBatchesFifoAsync(srsId, ct);
            return Ok(new { allocated = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("{srsId:guid}/create-vendor-po")]
    public async Task<ActionResult<VendorPoCreatedDto>> CreateVendorPoForShortage(Guid srsId, [FromQuery] Guid vendorId, CancellationToken ct)
    {
        try
        {
            var po = await _procurement.CreateVendorPoFromSrsAsync(srsId, vendorId, ct);
            return Ok(new VendorPoCreatedDto(po.Id, po.VendorId, po.OrderDate, po.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Store action: consume exactly the batches allocated to this SRS (Reserved -> Consumed),
    /// write StockTransaction(Consume) rows for traceability, and mark SRS status as Consumed.
    /// </summary>
    [HttpPost("{srsId:guid}/consume")]
    public async Task<IActionResult> Consume(Guid srsId, [FromQuery] string? consumedBy, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var srs = await _db.SRSs
            .Include(x => x.LineItems)
            .ThenInclude(li => li.BatchAllocations)
            .FirstOrDefaultAsync(x => x.Id == srsId, ct);
        if (srs == null) return NotFound("SRS not found.");

        if (srs.Status != SrsStatus.Approved && srs.Status != SrsStatus.Issued)
            return BadRequest("SRS must be Approved/Issued before consuming.");

        var who = string.IsNullOrWhiteSpace(consumedBy) ? "System" : consumedBy.Trim();

        // Load all batches referenced by allocations.
        var batchIds = srs.LineItems.SelectMany(li => li.BatchAllocations).Select(a => a.StockBatchId).Distinct().ToList();
        var batches = await _db.StockBatches.Where(b => batchIds.Contains(b.Id)).ToListAsync(ct);
        var batchMap = batches.ToDictionary(b => b.Id, b => b);

        var totalConsumed = 0m;

        foreach (var li in srs.LineItems)
        {
            foreach (var a in li.BatchAllocations)
            {
                var remaining = a.ReservedQuantity - a.ConsumedQuantity;
                if (remaining <= 0) continue;

                if (!batchMap.TryGetValue(a.StockBatchId, out var batch))
                    return StatusCode(500, $"Stock batch {a.StockBatchId} not found for allocation {a.Id}.");

                if (batch.QuantityReserved < remaining)
                    return Conflict($"Batch {batch.BatchNumber} does not have enough reserved qty to consume. Reserved={batch.QuantityReserved}, Need={remaining}.");

                _db.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    StockBatchId = batch.Id,
                    TransactionType = StockTransactionType.Consume,
                    Quantity = remaining,
                    LinkedWorkOrderId = srs.WorkOrderId,
                    LinkedSRSId = srs.Id,
                    Notes = $"SRS consume (allocation {a.Id}) by {who}"
                });

                batch.QuantityReserved -= remaining;
                batch.QuantityConsumed += remaining;
                a.ConsumedQuantity += remaining;
                totalConsumed += remaining;
            }
        }

        // Mark status
        srs.Status = SrsStatus.Consumed;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new { consumed = true, srsId = srs.Id, status = srs.Status.ToString(), totalConsumed });
    }
}

public record SrsDto(Guid Id, Guid WorkOrderId, string Status);
public record CreateSrsRequest(Guid WorkOrderId, List<CreateSrsLine> Lines);

public record SrsListDto(Guid Id, Guid WorkOrderId, string WorkOrderNumber, string Status, DateTime CreatedAt);

public record SrsDetailDto(
    Guid Id,
    Guid WorkOrderId,
    string WorkOrderNumber,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<SrsLineItemDetailDto> LineItems);

public record SrsLineItemDetailDto(
    Guid Id,
    Guid MaterialVariantId,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    decimal RequiredQuantity,
    IReadOnlyList<SrsAllocationDto> Allocations);

public record SrsAllocationDto(Guid Id, Guid StockBatchId, string BatchNumber, decimal ReservedQuantity, decimal ConsumedQuantity);

public record VendorPoCreatedDto(Guid Id, Guid VendorId, DateTime OrderDate, string Status);


