using HeatconERP.Application.Services.Procurement;
using HeatconERP.Domain.Enums.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcurementController : ControllerBase
{
    private readonly IProcurementService _procurement;
    private readonly HeatconDbContext _db;

    public ProcurementController(IProcurementService procurement, HeatconDbContext db)
    {
        _procurement = procurement;
        _db = db;
    }

    [HttpPost("vendor-po")]
    public async Task<ActionResult<VendorPoDto>> CreateVendorPo([FromBody] CreateVendorPoRequest req, CancellationToken ct)
    {
        try
        {
            var po = await _procurement.CreateVendorPoAsync(req.VendorId, req.OrderDate, req.Lines, ct);
            return Ok(new VendorPoDto(po.Id, po.VendorId, po.OrderDate, po.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("grn")]
    public async Task<ActionResult<GrnDto>> CreateGrn([FromBody] CreateGrnRequest req, CancellationToken ct)
    {
        try
        {
            var grn = await _procurement.CreateGrnAsync(req.VendorPurchaseOrderId, req.ReceivedDate, req.InvoiceNumber, req.Lines, ct);
            return Ok(new GrnDto(grn.Id, grn.VendorPurchaseOrderId, grn.ReceivedDate, grn.InvoiceNumber));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("grn/line/{grnLineItemId:guid}/process")]
    public async Task<ActionResult<StockBatchDto>> ProcessGrnLine(Guid grnLineItemId, [FromBody] ProcessGrnLineRequest req, CancellationToken ct)
    {
        try
        {
            var batch = await _procurement.ProcessGrnLineAndCreateBatchAsync(grnLineItemId, req.VendorId, req.QualityStatus, ct);
            return Ok(new StockBatchDto(batch.Id, batch.MaterialVariantId, batch.BatchNumber, batch.QuantityReceived, batch.QuantityAvailable, batch.QuantityReserved, batch.QuantityConsumed, batch.QualityStatus.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    // Direct receipt (Store): auto-create PO + GRN + batches in one call.
    [HttpPost("direct-grn")]
    public async Task<ActionResult<DirectGrnResultDto>> ReceiveDirectGrn([FromBody] CreateDirectGrnRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _procurement.ReceiveDirectGrnAsync(req.VendorId, req.ReceivedDate, req.InvoiceNumber, req.Lines, ct);
            return Ok(new DirectGrnResultDto(result.GrnId, result.VendorPurchaseOrderId, result.CreatedBatchIds));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict("Duplicate batch detected. BatchNumber must be unique per material.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Submit an existing GRN draft (typically created from Vendor Invoice acceptance) and create stock batches + StockTransaction(GRN).
    [HttpPost("grns/{grnId:guid}/submit-draft")]
    public async Task<ActionResult<SubmitGrnDraftResultDto>> SubmitGrnDraft(Guid grnId, [FromBody] SubmitGrnDraftRequest req, CancellationToken ct)
    {
        try
        {
            if (req.Lines.Count == 0) return BadRequest("No GRN lines provided.");
            var created = await _procurement.SubmitGrnDraftAsync(grnId, req.InvoiceNumber, req.ReceivedDate, req.Lines, ct);
            return Ok(new SubmitGrnDraftResultDto(grnId, created));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict("Duplicate batch detected. BatchNumber must be unique per material.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Helper for UI: returns the next suggested batch number for a given material variant.
    // Format: "{SKU}-B0001" increments per variant based on existing StockBatches.
    [HttpGet("next-batch-number/{materialVariantId:guid}")]
    public async Task<ActionResult<NextBatchNumberDto>> GetNextBatchNumber(Guid materialVariantId, CancellationToken ct)
    {
        var sku = await _db.MaterialVariants.AsNoTracking()
            .Where(v => v.Id == materialVariantId)
            .Select(v => v.SKU)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(sku)) return NotFound("Material variant not found.");

        var prefix = $"{sku}-B";
        var escapedPrefix = Regex.Escape(prefix);
        var rx = new Regex($"^{escapedPrefix}(\\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Read existing batch numbers for this variant that match our pattern.
        var existing = await _db.StockBatches.AsNoTracking()
            .Where(b => b.MaterialVariantId == materialVariantId && b.BatchNumber.StartsWith(prefix))
            .Select(b => b.BatchNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var bn in existing)
        {
            var m = rx.Match(bn);
            if (!m.Success) continue;
            if (int.TryParse(m.Groups[1].Value, out var n) && n > max) max = n;
        }

        var next = max + 1;
        var suggested = $"{prefix}{next:D4}";
        return Ok(new NextBatchNumberDto(suggested));
    }

    [HttpGet("grns")]
    public async Task<ActionResult<IReadOnlyList<GrnListItemDto>>> GetGrns(
        [FromQuery] int limit = 50,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool qcPassedOnly = false,
        CancellationToken ct = default)
    {
        if (limit <= 0) limit = 50;
        if (limit > 500) limit = 500;

        var q = _db.GRNs.AsNoTracking()
            .Include(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(g => g.LineItems)
            .AsQueryable();

        if (vendorId.HasValue)
            q = q.Where(g => g.VendorPurchaseOrder.VendorId == vendorId.Value);

        if (from.HasValue)
            q = q.Where(g => g.ReceivedDate >= from.Value);

        if (to.HasValue)
            q = q.Where(g => g.ReceivedDate <= to.Value);

        if (qcPassedOnly)
            q = q.Where(g => g.LineItems.All(li => li.QualityStatus == QualityStatus.Approved));

        var items = await q
            .OrderByDescending(g => g.ReceivedDate)
            .ThenByDescending(g => g.CreatedAt)
            .Take(limit)
            .Select(g => new GrnListItemDto(
                g.Id,
                g.VendorPurchaseOrder.VendorId,
                g.VendorPurchaseOrder.Vendor.Name,
                g.InvoiceNumber,
                g.ReceivedDate,
                g.LineItems.Count,
                g.LineItems.Sum(li => li.QuantityReceived)))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("grns/{grnId:guid}")]
    public async Task<ActionResult<GrnDetailDto>> GetGrnById(Guid grnId, CancellationToken ct = default)
    {
        var grn = await _db.GRNs.AsNoTracking()
            .Include(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(g => g.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .FirstOrDefaultAsync(g => g.Id == grnId, ct);

        if (grn == null) return NotFound();

        var liIds = grn.LineItems.Select(x => x.Id).ToList();
        var batchMap = await _db.StockBatches.AsNoTracking()
            .Where(b => liIds.Contains(b.GRNLineItemId))
            .Select(b => new { b.GRNLineItemId, b.Id })
            .ToDictionaryAsync(x => x.GRNLineItemId, x => (Guid?)x.Id, ct);

        var dto = new GrnDetailDto(
            grn.Id,
            grn.VendorPurchaseOrderId,
            grn.VendorPurchaseOrder.VendorId,
            grn.VendorPurchaseOrder.Vendor.Name,
            grn.ReceivedDate,
            grn.InvoiceNumber,
            grn.LineItems
                .OrderBy(li => li.CreatedAt)
                .Select(li => new GrnLineDto(
                    li.Id,
                    li.MaterialVariantId,
                    li.MaterialVariant.SKU,
                    li.MaterialVariant.Grade,
                    li.MaterialVariant.Size,
                    li.MaterialVariant.Unit,
                    li.BatchNumber,
                    li.QuantityReceived,
                    li.UnitPrice,
                    li.QualityStatus.ToString(),
                    StockBatchId: batchMap.GetValueOrDefault(li.Id)))
                .ToList());

        return Ok(dto);
    }

    // View-only Vendor POs (used by Store visibility / auto-created by direct GRN)
    [HttpGet("vendor-pos")]
    public async Task<ActionResult<IReadOnlyList<VendorPoListItemDto>>> GetVendorPos([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        var items = await _db.VendorPurchaseOrders.AsNoTracking()
            .Include(po => po.Vendor)
            .Include(po => po.LineItems)
            .OrderByDescending(po => po.OrderDate)
            .ThenByDescending(po => po.CreatedAt)
            .Take(limit)
            .Select(po => new VendorPoListItemDto(
                po.Id,
                po.VendorId,
                po.Vendor.Name,
                po.OrderDate,
                po.Status.ToString(),
                po.LineItems.Count,
                po.LineItems.Sum(li => li.OrderedQuantity)))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("vendor-pos/{vendorPoId:guid}")]
    public async Task<ActionResult<VendorPoDetailDto>> GetVendorPoById(Guid vendorPoId, CancellationToken ct = default)
    {
        var po = await _db.VendorPurchaseOrders.AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .FirstOrDefaultAsync(x => x.Id == vendorPoId, ct);

        if (po == null) return NotFound();

        var dto = new VendorPoDetailDto(
            po.Id,
            po.VendorId,
            po.Vendor.Name,
            po.OrderDate,
            po.Status.ToString(),
            po.LineItems
                .OrderBy(li => li.CreatedAt)
                .Select(li => new VendorPoLineDto(
                    li.Id,
                    li.MaterialVariantId,
                    li.MaterialVariant.SKU,
                    li.MaterialVariant.Grade,
                    li.MaterialVariant.Size,
                    li.MaterialVariant.Unit,
                    li.OrderedQuantity,
                    li.UnitPrice))
                .ToList());

        return Ok(dto);
    }
    
    // Incoming Material QC endpoints
    [HttpGet("grns/pending-qc")]
    public async Task<ActionResult<IReadOnlyList<GrnQcListDto>>> GetPendingQcGrns([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        var grns = await _db.GRNs.AsNoTracking()
            .Include(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(g => g.LineItems)
            .OrderByDescending(g => g.ReceivedDate)
            .ThenByDescending(g => g.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        var dtos = grns.Select(g =>
        {
            var totalLines = g.LineItems.Count;
            var pendingLines = g.LineItems.Count(li => li.QualityStatus == QualityStatus.PendingQC);
            var qcStatus = pendingLines == totalLines ? "Pending" : 
                           pendingLines > 0 ? "Partial" : "Complete";

            return new GrnQcListDto(
                g.Id,
                g.VendorPurchaseOrder.Vendor.Name,
                g.ReceivedDate,
                g.InvoiceNumber,
                totalLines,
                pendingLines,
                qcStatus);
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("grns/{grnId:guid}/qc-detail")]
    public async Task<ActionResult<GrnQcDetailDto>> GetGrnQcDetail(Guid grnId, CancellationToken ct = default)
    {
        var grn = await _db.GRNs.AsNoTracking()
            .Include(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(g => g.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .FirstOrDefaultAsync(g => g.Id == grnId, ct);

        if (grn == null) return NotFound();

        var totalLines = grn.LineItems.Count;
        var pendingLines = grn.LineItems.Count(li => li.QualityStatus == QualityStatus.PendingQC);
        var qcStatus = pendingLines == totalLines ? "Pending" : 
                       pendingLines > 0 ? "Partial" : "Complete";

        var dto = new GrnQcDetailDto(
            grn.Id,
            grn.VendorPurchaseOrder.Vendor.Name,
            grn.InvoiceNumber,
            grn.ReceivedDate,
            qcStatus,
            grn.LineItems
                .OrderBy(li => li.CreatedAt)
                .Select(li => new GrnQcLineDto(
                    li.Id,
                    li.MaterialVariant.SKU,
                    li.MaterialVariant.Grade,
                    li.MaterialVariant.Size,
                    li.MaterialVariant.Unit,
                    li.BatchNumber,
                    li.QuantityReceived,
                    li.UnitPrice,
                    li.QualityStatus.ToString()))
                .ToList());

        return Ok(dto);
    }

    [HttpPost("grns/lines/{lineId:guid}/approve")]
    public async Task<ActionResult> ApproveGrnLine(Guid lineId, [FromBody] ApproveGrnLineRequest req, CancellationToken ct = default)
    {
        var line = await _db.GRNLineItems
            .Include(li => li.GRN)
            .FirstOrDefaultAsync(li => li.Id == lineId, ct);

        if (line == null) return NotFound();

        if (line.QualityStatus != QualityStatus.PendingQC)
            return BadRequest("Line already processed.");

        // Approve the line
        line.QualityStatus = QualityStatus.Approved;

        // Create stock batch (make material available)
        var batch = new Domain.Entities.Inventory.StockBatch
        {
            Id = Guid.NewGuid(),
            MaterialVariantId = line.MaterialVariantId,
            BatchNumber = line.BatchNumber,
            GRNLineItemId = line.Id,
            VendorId = line.GRN.VendorPurchaseOrder.VendorId,
            QuantityReceived = line.QuantityReceived,
            QuantityAvailable = line.QuantityReceived,
            QuantityReserved = 0,
            QuantityConsumed = 0,
            UnitPrice = line.UnitPrice,
            QualityStatus = QualityStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.StockBatches.Add(batch);

        // Log activity
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "QC",
            Message = $"Material approved: {line.BatchNumber} (GRN line {lineId}). Approved by {req.ApprovedBy ?? "System"}."
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new { approved = true, batchId = batch.Id });
    }

    [HttpPost("grns/lines/{lineId:guid}/reject")]
    public async Task<ActionResult> RejectGrnLine(Guid lineId, [FromBody] RejectGrnLineRequest req, CancellationToken ct = default)
    {
        var line = await _db.GRNLineItems
            .Include(li => li.GRN)
            .ThenInclude(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(li => li.MaterialVariant)
            .FirstOrDefaultAsync(li => li.Id == lineId, ct);

        if (line == null) return NotFound();

        if (line.QualityStatus != QualityStatus.PendingQC)
            return BadRequest("Line already processed.");

        // Reject the line
        line.QualityStatus = QualityStatus.Rejected;

        // TODO: Generate vendor material NCR
        // For now, just log the rejection
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "QC_REJECT",
            Message = $"Material rejected: {line.MaterialVariant.SKU} Batch {line.BatchNumber}. Reason: {req.Reason}. Vendor: {line.GRN.VendorPurchaseOrder.Vendor.Name}. Rejected by {req.RejectedBy ?? "System"}."
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new { rejected = true, ncrGenerated = false }); // TODO: Set ncrGenerated = true when NCR system is in place
    }
}

public record GrnQcListDto(
    Guid Id,
    string VendorName,
    DateTime ReceivedDate,
    string InvoiceNumber,
    int TotalLines,
    int PendingLines,
    string QcStatus);

public record GrnQcDetailDto(
    Guid Id,
    string VendorName,
    string InvoiceNumber,
    DateTime ReceivedDate,
    string QcStatus,
    IReadOnlyList<GrnQcLineDto> Lines);

public record GrnQcLineDto(
    Guid Id,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    string BatchNumber,
    decimal QuantityReceived,
    decimal UnitPrice,
    string QcStatus);

public record ApproveGrnLineRequest(string? Notes, string? ApprovedBy);
public record RejectGrnLineRequest(string Reason, string? NcrDescription, string? RejectedBy);

public record VendorPoDto(Guid Id, Guid VendorId, DateTime OrderDate, string Status);

public record CreateVendorPoRequest(Guid VendorId, DateTime OrderDate, List<CreateVendorPoLine> Lines);

public record GrnDto(Guid Id, Guid VendorPurchaseOrderId, DateTime ReceivedDate, string InvoiceNumber);

public record CreateGrnRequest(Guid VendorPurchaseOrderId, DateTime ReceivedDate, string InvoiceNumber, List<CreateGrnLine> Lines);

public record ProcessGrnLineRequest(Guid VendorId, QualityStatus QualityStatus);

public record StockBatchDto(Guid Id, Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal QuantityAvailable, decimal QuantityReserved, decimal QuantityConsumed, string QualityStatus);

public record CreateDirectGrnRequest(Guid VendorId, DateTime ReceivedDate, string InvoiceNumber, List<ReceiveDirectGrnLine> Lines);

public record DirectGrnResultDto(Guid GrnId, Guid VendorPurchaseOrderId, IReadOnlyList<Guid> CreatedBatchIds);

public record NextBatchNumberDto(string BatchNumber);

public record SubmitGrnDraftRequest(string? InvoiceNumber, DateTime? ReceivedDate, List<SubmitGrnDraftLine> Lines);

public record SubmitGrnDraftResultDto(Guid GrnId, IReadOnlyList<Guid> CreatedBatchIds);

public record GrnListItemDto(Guid Id, Guid VendorId, string VendorName, string InvoiceNumber, DateTime ReceivedDate, int LineCount, decimal TotalQuantity);

public record GrnDetailDto(
    Guid Id,
    Guid VendorPurchaseOrderId,
    Guid VendorId,
    string VendorName,
    DateTime ReceivedDate,
    string InvoiceNumber,
    IReadOnlyList<GrnLineDto> Lines);

public record GrnLineDto(
    Guid Id,
    Guid MaterialVariantId,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    string BatchNumber,
    decimal QuantityReceived,
    decimal UnitPrice,
    string QualityStatus,
    Guid? StockBatchId);

public record VendorPoListItemDto(Guid Id, Guid VendorId, string VendorName, DateTime OrderDate, string Status, int LineCount, decimal TotalQuantity);

public record VendorPoDetailDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    DateTime OrderDate,
    string Status,
    IReadOnlyList<VendorPoLineDto> Lines);

public record VendorPoLineDto(Guid Id, Guid MaterialVariantId, string Sku, string Grade, string Size, string Unit, decimal OrderedQuantity, decimal UnitPrice);


