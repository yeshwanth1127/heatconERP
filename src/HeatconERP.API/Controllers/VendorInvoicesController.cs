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
    public async Task<ActionResult<VendorInvoiceAcceptedDto>> Accept(Guid id, CancellationToken ct)
    {
        try
        {
            var grn = await _procurement.AcceptVendorInvoiceAndCreateGrnDraftAsync(id, ct);
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


