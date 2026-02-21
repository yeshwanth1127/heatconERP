using HeatconERP.Application.Services.Srs;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SRSController : ControllerBase
{
    private readonly ISrsService _srs;
    private readonly HeatconDbContext _db;

    public SRSController(ISrsService srs, HeatconDbContext db)
    {
        _srs = srs;
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

        var items = await q
            .Include(x => x.WorkOrder)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new SrsListDto(
                x.Id,
                x.WorkOrderId,
                x.WorkOrder.OrderNumber,
                x.Status.ToString(),
                x.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{srsId:guid}")]
    public async Task<ActionResult<SrsDetailDto>> GetById(Guid srsId, CancellationToken ct = default)
    {
        var srs = await _db.SRSs
            .AsNoTracking()
            .Include(x => x.WorkOrder)
            .Include(x => x.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .Include(x => x.LineItems)
            .ThenInclude(li => li.BatchAllocations)
            .ThenInclude(a => a.StockBatch)
            .FirstOrDefaultAsync(x => x.Id == srsId, ct);

        if (srs == null) return NotFound();

        var dto = new SrsDetailDto(
            srs.Id,
            srs.WorkOrderId,
            srs.WorkOrder.OrderNumber,
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


