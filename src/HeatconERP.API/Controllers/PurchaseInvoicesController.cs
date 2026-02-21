using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public PurchaseInvoicesController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceListDto>>> Get(
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] string? status,
        [FromQuery] string? invoiceNumber,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        IQueryable<PurchaseInvoice> query = _db.PurchaseInvoices.AsNoTracking()
            .Include(i => i.PurchaseOrder);

        if (purchaseOrderId.HasValue)
            query = query.Where(i => i.PurchaseOrderId == purchaseOrderId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);
        if (!string.IsNullOrEmpty(invoiceNumber))
            query = query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.Contains(invoiceNumber));

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .Select(i => new PurchaseInvoiceListDto(
                i.Id,
                i.InvoiceNumber,
                i.PurchaseOrderId,
                i.PurchaseOrder != null ? i.PurchaseOrder.OrderNumber : null,
                i.InvoiceDate,
                i.DueDate,
                i.Status,
                i.TotalAmount,
                i.CreatedAt,
                i.CreatedByUserName))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var inv = await _db.PurchaseInvoices.AsNoTracking()
            .Include(i => i.PurchaseOrder)
            .ThenInclude(p => p!.Quotation)
            .Include(i => i.PurchaseOrder)
            .ThenInclude(p => p!.LineItems)
            .Include(i => i.LineItems.OrderBy(li => li.SortOrder))
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv == null) return NotFound();

        List<PurchaseInvoiceLineItemDto> lineItems;
        decimal totalAmount;

        if (inv.LineItems.Count > 0)
        {
            lineItems = inv.LineItems
                .OrderBy(li => li.SortOrder)
                .Select(li => new PurchaseInvoiceLineItemDto(
                    li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.LineTotal))
                .ToList();
            totalAmount = inv.TotalAmount;
        }
        else
        {
            // Backward-compat fallback: older invoices may not have stored line items.
            // In that case, display PO line items so invoice details still show amounts/items.
            var poLines = inv.PurchaseOrder?.LineItems?.OrderBy(li => li.SortOrder).ToList() ?? [];
            var sort = 0;
            decimal computedTotal = 0;
            lineItems = poLines.Select(poli =>
            {
                var lineTotal = poli.Quantity * poli.UnitPrice * (1 + poli.TaxPercent / 100m);
                computedTotal += lineTotal;
                return new PurchaseInvoiceLineItemDto(
                    Guid.Empty, sort++, poli.PartNumber, poli.Description, poli.Quantity, poli.UnitPrice, poli.TaxPercent, lineTotal);
            }).ToList();
            totalAmount = computedTotal;
        }

        return Ok(new PurchaseInvoiceDetailDto(
            inv.Id,
            inv.InvoiceNumber,
            inv.PurchaseOrderId,
            inv.PurchaseOrder?.OrderNumber,
            inv.PurchaseOrder?.Quotation?.ReferenceNumber,
            inv.PurchaseOrder?.Quotation?.Version,
            inv.InvoiceDate,
            inv.DueDate,
            inv.Status,
            totalAmount,
            inv.CreatedAt,
            inv.CreatedByUserName,
            lineItems));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDetailDto>> Create([FromBody] CreatePurchaseInvoiceRequest req, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.LineItems.OrderBy(li => li.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == req.PurchaseOrderId, ct);
        if (po == null) return BadRequest("Purchase order not found.");

        if (po.LineItems == null || po.LineItems.Count == 0)
            return BadRequest("Purchase order has no line items. Please add line items to the purchase order before creating an invoice.");

        var year = DateTime.UtcNow.Year;
        var count = await _db.PurchaseInvoices.CountAsync(i => i.CreatedAt.Year == year, ct);
        var invoiceNumber = req.InvoiceNumber ?? $"INV-{year % 100}{count + 1:D4}";

        var inv = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            PurchaseOrderId = po.Id,
            Status = req.Status ?? "Draft",
            InvoiceDate = req.InvoiceDate,
            DueDate = req.DueDate,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserName = req.CreatedByUserName ?? "System"
        };

        decimal total = 0;
        var sortOrder = 0;
        foreach (var poli in po.LineItems)
        {
            var lineTotal = poli.Quantity * poli.UnitPrice * (1 + poli.TaxPercent / 100m);
            var li = new PurchaseInvoiceLineItem
            {
                Id = Guid.NewGuid(),
                PurchaseInvoiceId = inv.Id,
                SortOrder = sortOrder++,
                PartNumber = poli.PartNumber ?? string.Empty,
                Description = poli.Description ?? string.Empty,
                Quantity = poli.Quantity,
                UnitPrice = poli.UnitPrice,
                TaxPercent = poli.TaxPercent,
                LineTotal = lineTotal
            };
            inv.LineItems.Add(li);
            _db.PurchaseInvoiceLineItems.Add(li);
            total += lineTotal;
        }
        inv.TotalAmount = total;

        _db.PurchaseInvoices.Add(inv);

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Purchase invoice {inv.InvoiceNumber} created from PO {po.OrderNumber} by {inv.CreatedByUserName}"
        });

        await _db.SaveChangesAsync(ct);

        var created = await _db.PurchaseInvoices.AsNoTracking()
            .Include(i => i.PurchaseOrder)
            .ThenInclude(p => p!.Quotation)
            .Include(i => i.LineItems.OrderBy(li => li.SortOrder))
            .FirstAsync(i => i.Id == inv.Id, ct);

        var createdLineItems = created.LineItems.Select(li => new PurchaseInvoiceLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.LineTotal)).ToList();
        return CreatedAtAction(nameof(GetById), new { id = inv.Id }, new PurchaseInvoiceDetailDto(
            created.Id, created.InvoiceNumber, created.PurchaseOrderId, created.PurchaseOrder?.OrderNumber,
            created.PurchaseOrder?.Quotation?.ReferenceNumber, created.PurchaseOrder?.Quotation?.Version,
            created.InvoiceDate, created.DueDate, created.Status, created.TotalAmount, created.CreatedAt, created.CreatedByUserName,
            createdLineItems));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePurchaseInvoiceRequest req, CancellationToken ct)
    {
        var inv = await _db.PurchaseInvoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv == null) return NotFound();

        if (req.InvoiceNumber != null) inv.InvoiceNumber = req.InvoiceNumber;
        if (req.InvoiceDate.HasValue) inv.InvoiceDate = req.InvoiceDate.Value;
        if (req.DueDate.HasValue) inv.DueDate = req.DueDate;
        if (req.Status != null) inv.Status = req.Status;

        if (req.LineItems != null)
        {
            _db.PurchaseInvoiceLineItems.RemoveRange(inv.LineItems);
            decimal total = 0;
            var sortOrder = 0;
            foreach (var item in req.LineItems)
            {
                var lineTotal = item.Quantity * item.UnitPrice * (1 + item.TaxPercent / 100m);
                var li = new PurchaseInvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseInvoiceId = inv.Id,
                    SortOrder = sortOrder++,
                    PartNumber = item.PartNumber ?? "",
                    Description = item.Description ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxPercent = item.TaxPercent,
                    LineTotal = lineTotal
                };
                _db.PurchaseInvoiceLineItems.Add(li);
                total += lineTotal;
            }
            inv.TotalAmount = total;
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Purchase invoice {inv.InvoiceNumber} updated"
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CreatePurchaseInvoiceRequest(
    Guid PurchaseOrderId,
    string? InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string? Status,
    string? CreatedByUserName);

public record UpdatePurchaseInvoiceRequest(
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    string? Status,
    List<PurchaseInvoiceLineItemInput>? LineItems);

public record PurchaseInvoiceLineItemInput(string? PartNumber, string? Description, int Quantity, decimal UnitPrice, decimal TaxPercent);

public record PurchaseInvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    Guid PurchaseOrderId,
    string? PurchaseOrderNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CreatedByUserName);

public record PurchaseInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid PurchaseOrderId,
    string? PurchaseOrderNumber,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CreatedByUserName,
    IReadOnlyList<PurchaseInvoiceLineItemDto> LineItems);

public record PurchaseInvoiceLineItemDto(Guid Id, int SortOrder, string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, decimal? LineTotal);
