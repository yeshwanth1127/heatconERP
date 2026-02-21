using System.Text.Json;
using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public PurchaseOrdersController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderListDto>>> Get(
        [FromQuery] Guid? quotationId,
        [FromQuery] string? customerPONumber,
        [FromQuery] string? status,
        [FromQuery] string? client,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        IQueryable<PurchaseOrder> query = _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Quotation)
            .Include(p => p.QuotationRevision);

        if (quotationId.HasValue)
            query = query.Where(p => p.QuotationId == quotationId.Value);
        if (!string.IsNullOrEmpty(customerPONumber))
            query = query.Where(p => p.CustomerPONumber != null && p.CustomerPONumber.Contains(customerPONumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrEmpty(client))
            query = query.Where(p => p.Quotation != null && p.Quotation.ClientName != null && p.Quotation.ClientName.Contains(client));

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new PurchaseOrderListDto(
                p.Id,
                p.OrderNumber,
                p.QuotationId,
                p.QuotationRevisionId,
                p.Quotation != null ? p.Quotation.ReferenceNumber : null,
                p.Quotation != null ? p.Quotation.Version : null,
                p.QuotationRevision != null ? p.QuotationRevision.Version : null,
                p.Quotation != null ? p.Quotation.ClientName : null,
                p.CustomerPONumber,
                p.PODate,
                p.Status,
                p.Value,
                p.CreatedAt,
                p.CreatedByUserName))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Quotation)
            .Include(p => p.QuotationRevision)
            .Include(p => p.LineItems.OrderBy(li => li.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po == null) return NotFound();

        var lineItems = po.LineItems.Select(li => new PurchaseOrderLineItemDto(
            li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList();

        return Ok(new PurchaseOrderDetailDto(
            po.Id,
            po.OrderNumber,
            po.QuotationId,
            po.QuotationRevisionId,
            po.Quotation?.ReferenceNumber,
            po.Quotation?.Version,
            po.QuotationRevision?.Version,
            po.Quotation?.ClientName,
            po.CustomerPONumber,
            po.PODate,
            po.DeliveryTerms,
            po.PaymentTerms,
            po.Status,
            po.Value,
            po.CreatedAt,
            po.CreatedByUserName,
            lineItems));
    }

    [HttpGet("{id:guid}/compare")]
    public async Task<ActionResult<PurchaseOrderCompareDto>> GetCompare(Guid id, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Quotation)
            .ThenInclude(q => q!.LineItems.OrderBy(li => li.SortOrder))
            .Include(p => p.LineItems.OrderBy(li => li.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po == null) return NotFound();
        if (po.QuotationId == null || po.Quotation == null)
            return BadRequest("Purchase order is not linked to a quotation.");

        var poLines = po.LineItems.Select(li => new PurchaseOrderLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList();
        var quoteLines = po.Quotation.LineItems.Select(li => new QuotationLineItemCompareDto(li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent)).ToList();

        var comparisons = new List<PurchaseOrderCompareLineDto>();
        var maxRows = Math.Max(poLines.Count, quoteLines.Count);
        for (var i = 0; i < maxRows; i++)
        {
            var pol = i < poLines.Count ? poLines[i] : null;
            var ql = i < quoteLines.Count ? quoteLines[i] : null;
            var qtyDiff = (pol?.Quantity ?? 0) != (ql?.Quantity ?? 0);
            var priceDiff = (pol?.UnitPrice ?? 0) != (ql?.UnitPrice ?? 0);
            var descDiff = (pol?.Description ?? "") != (ql?.Description ?? "");
            comparisons.Add(new PurchaseOrderCompareLineDto(
                pol?.PartNumber ?? ql?.PartNumber ?? "",
                pol?.Description ?? "",
                ql?.Description ?? "",
                descDiff,
                pol?.Quantity ?? 0,
                ql?.Quantity ?? 0,
                qtyDiff,
                pol?.UnitPrice ?? 0,
                ql?.UnitPrice ?? 0,
                priceDiff));
        }

        return Ok(new PurchaseOrderCompareDto(
            po.Id,
            po.OrderNumber,
            po.Quotation!.ReferenceNumber,
            po.Quotation.Version,
            comparisons));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Create([FromBody] CreatePurchaseOrderRequest req, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.PurchaseOrders.CountAsync(p => p.CreatedAt.Year == year, ct);
        var orderNumber = $"PO-{year % 100}{count + 1:D4}";

        Quotation? quotation = null;
        QuotationRevision? sentRevision = null;
        if (req.QuotationRevisionId.HasValue)
        {
            sentRevision = await _db.QuotationRevisions.Include(r => r.Quotation).ThenInclude(q => q!.LineItems.OrderBy(li => li.SortOrder))
                .FirstOrDefaultAsync(r => r.Id == req.QuotationRevisionId.Value, ct);
            if (sentRevision == null) return BadRequest("Quotation revision not found.");
            quotation = sentRevision.Quotation;
        }
        else if (req.QuotationId.HasValue)
        {
            quotation = await _db.Quotations.Include(q => q.LineItems.OrderBy(li => li.SortOrder))
                .FirstOrDefaultAsync(q => q.Id == req.QuotationId.Value, ct);
            if (quotation == null) return BadRequest("Quotation not found.");
        }

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = req.OrderNumber ?? orderNumber,
            QuotationId = quotation?.Id ?? req.QuotationId,
            QuotationRevisionId = req.QuotationRevisionId,
            CustomerPONumber = req.CustomerPONumber ?? "",
            PODate = req.PODate,
            DeliveryTerms = req.DeliveryTerms,
            PaymentTerms = req.PaymentTerms,
            Status = req.Status ?? "Active",
            Value = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserName = req.CreatedByUserName ?? "System"
        };

        var lineItemsToAdd = req.LineItems?.Select((li, i) => new { Item = li, SortOrder = i }).ToList();
        if (lineItemsToAdd == null || lineItemsToAdd.Count == 0)
        {
            if (sentRevision != null && !string.IsNullOrEmpty(sentRevision.SnapshotLineItemsJson))
            {
                var sortOrder = 0;
                decimal subtotal = 0;
                decimal totalTax = 0;
                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshotItems = JsonSerializer.Deserialize<List<PoSnapshotLineItem>>(sentRevision.SnapshotLineItemsJson, opts);
                    if (snapshotItems != null)
                        foreach (var qli in snapshotItems)
                        {
                            var li = new PurchaseOrderLineItem
                            {
                                Id = Guid.NewGuid(),
                                PurchaseOrderId = po.Id,
                                SortOrder = sortOrder++,
                                PartNumber = qli.PartNumber ?? "",
                                Description = qli.Description ?? "",
                                Quantity = qli.Quantity,
                                UnitPrice = qli.UnitPrice,
                                TaxPercent = qli.TaxPercent,
                                AttachmentPath = qli.AttachmentPath
                            };
                            po.LineItems.Add(li);
                            _db.PurchaseOrderLineItems.Add(li);
                            subtotal += li.Quantity * li.UnitPrice;
                            totalTax += li.Quantity * li.UnitPrice * (li.TaxPercent / 100m);
                        }
                    po.Value = subtotal + totalTax;
                }
                catch { /* fallback to quotation line items below */ }
            }
            if (po.LineItems.Count == 0 && quotation != null)
            {
                var sortOrder = 0;
                decimal subtotal = 0;
                decimal totalTax = 0;
                foreach (var qli in quotation.LineItems)
                {
                    var li = new PurchaseOrderLineItem
                    {
                        Id = Guid.NewGuid(),
                        PurchaseOrderId = po.Id,
                        SortOrder = sortOrder++,
                        PartNumber = qli.PartNumber,
                        Description = qli.Description,
                        Quantity = qli.Quantity,
                        UnitPrice = qli.UnitPrice,
                        TaxPercent = qli.TaxPercent,
                        AttachmentPath = qli.AttachmentPath
                    };
                    po.LineItems.Add(li);
                    _db.PurchaseOrderLineItems.Add(li);
                    subtotal += li.Quantity * li.UnitPrice;
                    totalTax += li.Quantity * li.UnitPrice * (li.TaxPercent / 100m);
                }
                po.Value = subtotal + totalTax;
            }
        }
        else
        {
            decimal subtotal = 0;
            decimal totalTax = 0;
            foreach (var x in lineItemsToAdd)
            {
                var li = new PurchaseOrderLineItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    SortOrder = x.SortOrder,
                    PartNumber = x.Item.PartNumber ?? "",
                    Description = x.Item.Description ?? "",
                    Quantity = x.Item.Quantity,
                    UnitPrice = x.Item.UnitPrice,
                    TaxPercent = x.Item.TaxPercent,
                    AttachmentPath = x.Item.AttachmentPath
                };
                po.LineItems.Add(li);
                _db.PurchaseOrderLineItems.Add(li);
                subtotal += li.Quantity * li.UnitPrice;
                totalTax += li.Quantity * li.UnitPrice * (li.TaxPercent / 100m);
            }
            po.Value = subtotal + totalTax;
        }

        _db.PurchaseOrders.Add(po);

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Purchase order {po.OrderNumber} created by {po.CreatedByUserName}" + (req.QuotationId.HasValue ? $" from quotation {quotation?.ReferenceNumber}" : "")
        });

        await _db.SaveChangesAsync(ct);

        var created = await _db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Quotation)
            .Include(p => p.QuotationRevision)
            .Include(p => p.LineItems.OrderBy(li => li.SortOrder))
            .FirstAsync(p => p.Id == po.Id, ct);

        var createdLineItems = created.LineItems.Select(li => new PurchaseOrderLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList();
        return CreatedAtAction(nameof(GetById), new { id = po.Id }, new PurchaseOrderDetailDto(
            created.Id, created.OrderNumber, created.QuotationId, created.QuotationRevisionId, created.Quotation?.ReferenceNumber, created.Quotation?.Version, created.QuotationRevision?.Version, created.Quotation?.ClientName,
            created.CustomerPONumber, created.PODate, created.DeliveryTerms, created.PaymentTerms, created.Status, created.Value, created.CreatedAt, created.CreatedByUserName,
            createdLineItems));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderRequest req, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po == null) return NotFound();

        if (req.OrderNumber != null) po.OrderNumber = req.OrderNumber;
        if (req.CustomerPONumber != null) po.CustomerPONumber = req.CustomerPONumber;
        if (req.PODate.HasValue) po.PODate = req.PODate.Value;
        if (req.DeliveryTerms != null) po.DeliveryTerms = req.DeliveryTerms;
        if (req.PaymentTerms != null) po.PaymentTerms = req.PaymentTerms;
        if (req.Status != null) po.Status = req.Status;

        if (req.LineItems != null)
        {
            _db.PurchaseOrderLineItems.RemoveRange(po.LineItems);
            decimal subtotal = 0;
            decimal totalTax = 0;
            var sortOrder = 0;
            foreach (var item in req.LineItems)
            {
                var li = new PurchaseOrderLineItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    SortOrder = sortOrder++,
                    PartNumber = item.PartNumber ?? "",
                    Description = item.Description ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxPercent = item.TaxPercent,
                    AttachmentPath = item.AttachmentPath
                };
                _db.PurchaseOrderLineItems.Add(li);
                subtotal += li.Quantity * li.UnitPrice;
                totalTax += li.Quantity * li.UnitPrice * (li.TaxPercent / 100m);
            }
            po.Value = subtotal + totalTax;
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Purchase order {po.OrderNumber} updated"
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CreatePurchaseOrderRequest(
    string? OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? CustomerPONumber,
    DateTime PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string? Status,
    string? CreatedByUserName,
    List<PurchaseOrderLineItemInput>? LineItems);

public record UpdatePurchaseOrderRequest(
    string? OrderNumber,
    string? CustomerPONumber,
    DateTime? PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string? Status,
    List<PurchaseOrderLineItemInput>? LineItems);

public record PurchaseOrderLineItemInput(string? PartNumber, string? Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);

public record PurchaseOrderListDto(
    Guid Id,
    string OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    string? SentRevisionVersion,
    string? ClientName,
    string CustomerPONumber,
    DateTime PODate,
    string Status,
    decimal? Value,
    DateTime CreatedAt,
    string CreatedByUserName);

public record PurchaseOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    string? SentRevisionVersion,
    string? ClientName,
    string CustomerPONumber,
    DateTime PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string Status,
    decimal? Value,
    DateTime CreatedAt,
    string CreatedByUserName,
    IReadOnlyList<PurchaseOrderLineItemDto> LineItems);

public record PurchaseOrderLineItemDto(Guid Id, int SortOrder, string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);

public record PurchaseOrderCompareDto(
    Guid PurchaseOrderId,
    string OrderNumber,
    string QuotationReferenceNumber,
    string QuotationVersion,
    IReadOnlyList<PurchaseOrderCompareLineDto> Comparisons);

public record PurchaseOrderCompareLineDto(
    string PartNumber,
    string PoDescription,
    string QuotationDescription,
    bool DescriptionMismatch,
    int PoQuantity,
    int QuotationQuantity,
    bool QuantityMismatch,
    decimal PoUnitPrice,
    decimal QuotationUnitPrice,
    bool PriceMismatch);

public record QuotationLineItemCompareDto(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent);
internal record PoSnapshotLineItem(string? PartNumber, string? Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);
