using HeatconERP.Application.Services.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    private readonly HeatconDbContext _db;

    public InventoryController(IInventoryService inventory, HeatconDbContext db)
    {
        _inventory = inventory;
        _db = db;
    }

    [HttpGet("summary/{materialVariantId:guid}")]
    public async Task<ActionResult<VariantStockSummaryDto>> GetVariantSummary(Guid materialVariantId, CancellationToken ct)
    {
        return Ok(await _inventory.GetVariantStockSummaryAsync(materialVariantId, ct));
    }

    [HttpGet("batch/{stockBatchId:guid}")]
    public async Task<ActionResult<BatchDetailsDto>> GetBatchDetails(Guid stockBatchId, CancellationToken ct)
    {
        var b = await _inventory.GetBatchDetailsAsync(stockBatchId, ct);
        if (b == null) return NotFound();

        var dto = new BatchDetailsDto(
            b.Id,
            b.BatchNumber,
            b.MaterialVariantId,
            b.MaterialVariant.SKU,
            b.VendorId,
            b.Vendor.Name,
            b.GRNLineItemId,
            b.QuantityReceived,
            b.QuantityAvailable,
            b.QuantityReserved,
            b.QuantityConsumed,
            b.QualityStatus.ToString(),
            b.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new BatchTransactionDto(t.Id, t.TransactionType.ToString(), t.Quantity, t.LinkedWorkOrderId, t.LinkedSRSId, t.CreatedAt, t.Notes))
                .ToList());

        return Ok(dto);
    }

    [HttpGet("inventory-summary")]
    public async Task<ActionResult<InventorySummaryDto>> GetInventorySummary(CancellationToken ct)
    {
        // Incoming ordered qty = sum(PO ordered) - sum(received in GRN lines) per variant.
        var ordered = await _db.VendorPurchaseOrderLineItems
            .GroupBy(x => x.MaterialVariantId)
            .Select(g => new { VariantId = g.Key, Ordered = g.Sum(x => x.OrderedQuantity) })
            .ToListAsync(ct);

        var received = await _db.GRNLineItems
            .GroupBy(x => x.MaterialVariantId)
            .Select(g => new { VariantId = g.Key, Received = g.Sum(x => x.QuantityReceived) })
            .ToListAsync(ct);

        var stock = await _db.StockBatches.AsNoTracking().ToListAsync(ct);

        var incoming = ordered.ToDictionary(x => x.VariantId, x => x.Ordered);
        foreach (var r in received)
            incoming[r.VariantId] = incoming.GetValueOrDefault(r.VariantId, 0) - r.Received;

        var incomingTotal = incoming.Values.Where(v => v > 0).Sum();

        return Ok(new InventorySummaryDto(
            TotalReceived: stock.Sum(x => x.QuantityReceived),
            TotalAvailable: stock.Sum(x => x.QuantityAvailable),
            TotalReserved: stock.Sum(x => x.QuantityReserved),
            TotalConsumed: stock.Sum(x => x.QuantityConsumed),
            IncomingOrderedQuantity: incomingTotal));
    }

    [HttpGet("batch-history/{batchNumber}")]
    public async Task<ActionResult<IReadOnlyList<BatchHistoryDto>>> GetBatchHistory(string batchNumber, CancellationToken ct)
    {
        var batches = await _db.StockBatches
            .AsNoTracking()
            .Include(b => b.Vendor)
            .Include(b => b.GRNLineItem)
            .ThenInclude(li => li.GRN)
            .Where(b => b.BatchNumber == batchNumber)
            .ToListAsync(ct);

        var dto = batches.Select(b => new BatchHistoryDto(
            b.Id,
            b.BatchNumber,
            b.Vendor.Name,
            b.GRNLineItem.GRN.InvoiceNumber,
            b.GRNLineItem.GRN.Id,
            b.Transactions.Select(t => t.LinkedWorkOrderId).Where(x => x != null).Distinct().Cast<Guid>().ToList()
        )).ToList();

        return Ok(dto);
    }

    [HttpGet("batch-tree")]
    public async Task<ActionResult<IReadOnlyList<BatchTreeNodeDto>>> GetBatchTree(CancellationToken ct)
    {
        var categories = await _db.MaterialCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync(ct);

        var variants = await _db.MaterialVariants
            .AsNoTracking()
            .Select(v => new
            {
                v.Id,
                v.MaterialCategoryId,
                v.SKU,
                v.Grade,
                v.Size,
                v.Unit
            })
            .ToListAsync(ct);

        var batches = await _db.StockBatches
            .AsNoTracking()
            .Include(b => b.Vendor)
            .Include(b => b.GRNLineItem)
            .ThenInclude(li => li.GRN)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);

        var batchesByVariant = batches
            .GroupBy(b => b.MaterialVariantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dto = categories
            .Select(c =>
            {
                var inCat = variants.Where(v => v.MaterialCategoryId == c.Id).ToList();
                var grades = inCat
                    .GroupBy(v => string.IsNullOrWhiteSpace(v.Grade) ? "Unspecified" : v.Grade.Trim())
                    .OrderBy(g => g.Key)
                    .Select(g =>
                        new BatchTreeGradeNodeDto(
                            g.Key,
                            g.OrderBy(v => v.SKU).Select(v =>
                            {
                                var vBatches = batchesByVariant.GetValueOrDefault(v.Id) ?? [];
                                return new BatchTreeVariantNodeDto(
                                    v.Id,
                                    v.SKU,
                                    v.Grade,
                                    v.Size,
                                    v.Unit,
                                    vBatches.Select(b => new BatchTreeBatchDto(
                                        b.Id,
                                        b.BatchNumber,
                                        b.Vendor.Name,
                                        b.GRNLineItem.GRN.InvoiceNumber,
                                        b.GRNLineItem.GRN.Id,
                                        b.QuantityReceived,
                                        b.QuantityAvailable,
                                        b.QuantityReserved,
                                        b.QuantityConsumed,
                                        b.Transactions.Select(t => t.LinkedWorkOrderId).Where(x => x != null).Distinct().Cast<Guid>().ToList()
                                    )).ToList()
                                );
                            }).Where(vn => vn.Batches.Count > 0).ToList()
                        )
                    )
                    .Where(gn => gn.Variants.Count > 0)
                    .ToList();

                return new BatchTreeNodeDto(c.Id, c.Name, c.Description, grades);
            })
            .Where(x => x.Grades.Count > 0)
            .ToList();

        return Ok(dto);
    }

    // Material hierarchy for Production/Store UIs (Type/Category -> Grades -> Variants) with stock totals.
    [HttpGet("material-tree")]
    public async Task<ActionResult<IReadOnlyList<MaterialTypeNodeDto>>> GetMaterialTree(CancellationToken ct)
    {
        var categories = await _db.MaterialCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync(ct);

        var variants = await _db.MaterialVariants
            .AsNoTracking()
            .Select(v => new
            {
                v.Id,
                v.MaterialCategoryId,
                v.SKU,
                v.Grade,
                v.Size,
                v.Unit
            })
            .ToListAsync(ct);

        var totals = await _db.StockBatches
            .AsNoTracking()
            .GroupBy(b => b.MaterialVariantId)
            .Select(g => new
            {
                VariantId = g.Key,
                Received = g.Sum(x => x.QuantityReceived),
                Available = g.Sum(x => x.QuantityAvailable),
                Reserved = g.Sum(x => x.QuantityReserved),
                Consumed = g.Sum(x => x.QuantityConsumed)
            })
            .ToDictionaryAsync(x => x.VariantId, x => x, ct);

        var dto = categories
            .Select(c =>
            {
                var inCat = variants.Where(v => v.MaterialCategoryId == c.Id).ToList();
                var grades = inCat
                    .GroupBy(v => string.IsNullOrWhiteSpace(v.Grade) ? "Unspecified" : v.Grade.Trim())
                    .OrderBy(g => g.Key)
                    .Select(g =>
                        new MaterialGradeNodeDto(
                            g.Key,
                            g.OrderBy(v => v.SKU).Select(v =>
                            {
                                var t = totals.GetValueOrDefault(v.Id);
                                return new MaterialVariantNodeDto(
                                    v.Id,
                                    v.SKU,
                                    v.Grade,
                                    v.Size,
                                    v.Unit,
                                    Received: t?.Received ?? 0,
                                    Available: t?.Available ?? 0,
                                    Reserved: t?.Reserved ?? 0,
                                    Consumed: t?.Consumed ?? 0);
                            }).ToList()
                        )
                    )
                    .ToList();

                return new MaterialTypeNodeDto(c.Id, c.Name, c.Description, grades);
            })
            .Where(x => x.Grades.Count > 0)
            .ToList();

        return Ok(dto);
    }
}

public record InventorySummaryDto(decimal TotalReceived, decimal TotalAvailable, decimal TotalReserved, decimal TotalConsumed, decimal IncomingOrderedQuantity);

public record BatchDetailsDto(
    Guid Id,
    string BatchNumber,
    Guid MaterialVariantId,
    string Sku,
    Guid VendorId,
    string Vendor,
    Guid GrnLineItemId,
    decimal QuantityReceived,
    decimal QuantityAvailable,
    decimal QuantityReserved,
    decimal QuantityConsumed,
    string QualityStatus,
    IReadOnlyList<BatchTransactionDto> Transactions);

public record BatchTransactionDto(Guid Id, string Type, decimal Quantity, Guid? LinkedWorkOrderId, Guid? LinkedSrsId, DateTime OccurredAt, string? Notes);

public record BatchHistoryDto(Guid StockBatchId, string BatchNumber, string Vendor, string InvoiceNumber, Guid GrnId, IReadOnlyList<Guid> LinkedWorkOrders);

public record MaterialTypeNodeDto(Guid Id, string Name, string? Description, IReadOnlyList<MaterialGradeNodeDto> Grades);

public record MaterialGradeNodeDto(string Grade, IReadOnlyList<MaterialVariantNodeDto> Variants);

public record MaterialVariantNodeDto(
    Guid Id,
    string SKU,
    string Grade,
    string Size,
    string Unit,
    decimal Received,
    decimal Available,
    decimal Reserved,
    decimal Consumed);

public record BatchTreeNodeDto(Guid Id, string Name, string? Description, IReadOnlyList<BatchTreeGradeNodeDto> Grades);

public record BatchTreeGradeNodeDto(string Grade, IReadOnlyList<BatchTreeVariantNodeDto> Variants);

public record BatchTreeVariantNodeDto(
    Guid Id,
    string SKU,
    string Grade,
    string Size,
    string Unit,
    IReadOnlyList<BatchTreeBatchDto> Batches);

public record BatchTreeBatchDto(
    Guid Id,
    string BatchNumber,
    string Vendor,
    string InvoiceNumber,
    Guid GrnId,
    decimal QuantityReceived,
    decimal QuantityAvailable,
    decimal QuantityReserved,
    decimal QuantityConsumed,
    IReadOnlyList<Guid> LinkedWorkOrders);
