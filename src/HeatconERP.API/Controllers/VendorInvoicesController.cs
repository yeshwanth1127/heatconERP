using HeatconERP.Application.Services.Procurement;
using HeatconERP.Domain.Enums.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/vendor-invoices")]
public class VendorInvoicesController : ControllerBase
{
    private readonly IProcurementService _procurement;
    private readonly HeatconDbContext _db;

    public VendorInvoicesController(IProcurementService procurement, HeatconDbContext db)
    {
        _procurement = procurement;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorInvoiceListItemDto>>> List([FromQuery] string? status, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (limit <= 0) limit = 100;
        if (limit > 500) limit = 500;

        var q = _db.VendorPurchaseInvoices.AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.VendorPurchaseOrder)
            .Include(x => x.LineItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(x => x.Status.ToString() == status);

        var items = await q
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new VendorInvoiceListItemDto(
                x.Id,
                x.VendorId,
                x.Vendor.Name,
                x.VendorPurchaseOrderId,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.Status.ToString(),
                x.LineItems.Count,
                x.LineItems.Sum(li => li.Quantity)))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendorInvoiceDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var inv = await _db.VendorPurchaseInvoices.AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.VendorPurchaseOrder)
            .Include(x => x.LineItems)
            .ThenInclude(li => li.MaterialVariant)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (inv == null) return NotFound();

        var dto = new VendorInvoiceDetailDto(
            inv.Id,
            inv.VendorId,
            inv.Vendor.Name,
            inv.VendorPurchaseOrderId,
            inv.InvoiceNumber,
            inv.InvoiceDate,
            inv.Status.ToString(),
            inv.LineItems
                .OrderBy(li => li.CreatedAt)
                .Select(li => new VendorInvoiceLineDto(
                    li.Id,
                    li.MaterialVariantId,
                    li.MaterialVariant.SKU,
                    li.MaterialVariant.Grade,
                    li.MaterialVariant.Size,
                    li.MaterialVariant.Unit,
                    li.Quantity,
                    li.UnitPrice))
                .ToList());

        return Ok(dto);
    }

    // "Send PO" action: creates a vendor invoice immediately (placeholder behavior).
    [HttpPost("from-vendor-po/{vendorPoId:guid}")]
    public async Task<ActionResult<VendorInvoiceCreatedDto>> CreateFromVendorPo(Guid vendorPoId, [FromBody] CreateVendorInvoiceFromPoRequest req, CancellationToken ct)
    {
        try
        {
            var inv = await _procurement.SendVendorPoAndCreateInvoiceAsync(vendorPoId, req.InvoiceNumber, req.InvoiceDate, ct);
            return Ok(new VendorInvoiceCreatedDto(inv.Id, inv.VendorPurchaseOrderId, inv.InvoiceNumber, inv.InvoiceDate, inv.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict("Duplicate vendor invoice number for this vendor.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<VendorInvoiceAcceptedDto>> Accept(Guid id, [FromBody] VendorInvoiceQcDecisionRequest req, CancellationToken ct)
    {
        try
        {
            await EnsureVendorInvoiceQcDecisionStoreAsync(ct);
            var grn = await _procurement.AcceptVendorInvoiceAndCreateGrnDraftAsync(id, req.Description, req.DecidedBy, ct);
            return Ok(new VendorInvoiceAcceptedDto(id, grn.Id));
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

    [HttpPost("{id:guid}/decline")]
    public async Task<ActionResult> Decline(Guid id, [FromBody] VendorInvoiceQcDecisionRequest req, CancellationToken ct)
    {
        try
        {
            await EnsureVendorInvoiceQcDecisionStoreAsync(ct);
            await _procurement.RejectVendorInvoiceAsync(id, req.Description, req.DecidedBy, ct);
            return NoContent();
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

    [HttpGet("{id:guid}/qc-decline-history")]
    public async Task<ActionResult<IReadOnlyList<VendorInvoiceQcHistoryDto>>> GetQcDeclineHistory(Guid id, CancellationToken ct)
    {
        await EnsureVendorInvoiceQcDecisionStoreAsync(ct);

        var exists = await _db.VendorPurchaseInvoices.AsNoTracking().AnyAsync(x => x.Id == id, ct);
        if (!exists) return NotFound("Vendor invoice not found.");

        var items = await _procurement.GetVendorInvoiceDeclineHistoryAsync(id, ct);
        return Ok(items.Select(x => new VendorInvoiceQcHistoryDto(x.Id, x.Decision, x.Description, x.DecidedBy, x.CreatedAt)).ToList());
    }

    [HttpGet("{id:guid}/grn-qc-status")]
    public async Task<ActionResult<GrnQcStatusDto>> GetGrnQcStatus(Guid id, CancellationToken ct)
    {
        try
        {
            var grn = await _db.GRNs.AsNoTracking()
                .Include(g => g.LineItems)
                .FirstOrDefaultAsync(g => g.VendorPurchaseInvoiceId == id, ct);
            if (grn == null) return NotFound("GRN not found for this invoice.");

            var allApproved = await _procurement.CheckGrnQcAllApprovedAsync(grn.Id, ct);
            var pendingCount = grn.LineItems.Count(li => li.QualityStatus.ToString() == QualityStatus.PendingQC.ToString());
            var approvedCount = grn.LineItems.Count(li => li.QualityStatus.ToString() == QualityStatus.Approved.ToString());
            var rejectedCount = grn.LineItems.Count(li => li.QualityStatus.ToString() == QualityStatus.Rejected.ToString());

            return Ok(new GrnQcStatusDto(grn.Id, allApproved, pendingCount, approvedCount, rejectedCount));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    private async Task EnsureVendorInvoiceQcDecisionStoreAsync(CancellationToken ct)
    {
        const string sql = """
CREATE TABLE IF NOT EXISTS "VendorInvoiceQcDecisions" (
    "Id" uuid NOT NULL,
    "VendorPurchaseInvoiceId" uuid NOT NULL,
    "Decision" text NOT NULL,
    "Description" text NOT NULL,
    "DecidedBy" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL,
    "RowVersion" bytea NOT NULL,
    CONSTRAINT "PK_VendorInvoiceQcDecisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VendorInvoiceQcDecisions_VendorPurchaseInvoices_VendorPurchaseInvoiceId"
        FOREIGN KEY ("VendorPurchaseInvoiceId") REFERENCES "VendorPurchaseInvoices" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_VendorInvoiceQcDecisions_VendorPurchaseInvoiceId_CreatedAt"
    ON "VendorInvoiceQcDecisions" ("VendorPurchaseInvoiceId", "CreatedAt");
""";

        await _db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}

public record VendorInvoiceListItemDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    Guid VendorPurchaseOrderId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string Status,
    int LineCount,
    decimal TotalQuantity);

public record VendorInvoiceDetailDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    Guid VendorPurchaseOrderId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string Status,
    IReadOnlyList<VendorInvoiceLineDto> Lines);

public record VendorInvoiceLineDto(Guid Id, Guid MaterialVariantId, string Sku, string Grade, string Size, string Unit, decimal Quantity, decimal UnitPrice);

public record CreateVendorInvoiceFromPoRequest(string? InvoiceNumber, DateTime InvoiceDate);

public record VendorInvoiceCreatedDto(Guid Id, Guid VendorPurchaseOrderId, string InvoiceNumber, DateTime InvoiceDate, string Status);

public record VendorInvoiceAcceptedDto(Guid VendorPurchaseInvoiceId, Guid GrnId);

public record GrnQcStatusDto(Guid GrnId, bool AllApproved, int PendingCount, int ApprovedCount, int RejectedCount);

public record VendorInvoiceQcDecisionRequest(string Description, string? DecidedBy);

public record VendorInvoiceQcHistoryDto(Guid Id, string Decision, string Description, string DecidedBy, DateTime CreatedAt);

