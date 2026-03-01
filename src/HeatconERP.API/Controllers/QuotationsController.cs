using System.Text.Json;
using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public QuotationsController(HeatconDbContext db) => _db = db;

    /// <summary>
    /// Calculates the next revision version for a quotation following semantic versioning:
    /// - Increments minor version for updates to the same major version
    /// - Returns v{major}.{count of revisions with this major version}
    /// </summary>
    private string GetNextRevisionVersion(Quotation q)
    {
        // Extract major version from q.Version (e.g., 1 from "v1.0")
        var majorMatch = System.Text.RegularExpressions.Regex.Match(q.Version, @"v(\d+)");
        int major = 1;
        if (majorMatch.Success && int.TryParse(majorMatch.Groups[1].Value, out var m))
        {
            major = m;
        }

        // Count revisions with this major version to determine next minor
        int minorCount = 0;
        foreach (var rev in q.Revisions)
        {
            var match = System.Text.RegularExpressions.Regex.Match(rev.Version, @"v(\d+)\.(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var revMajor) && revMajor == major)
            {
                minorCount++;
            }
        }

        return $"v{major}.{minorCount}";
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuotationListDto>>> Get(
        [FromQuery] Guid? enquiryId,
        [FromQuery] string? searchId,
        [FromQuery] string? client,
        [FromQuery] string? status,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        IQueryable<Quotation> query = _db.Quotations.AsNoTracking();

        if (enquiryId.HasValue)
            query = query.Where(q => q.EnquiryId == enquiryId.Value);
        if (!string.IsNullOrEmpty(searchId))
            query = query.Where(q => q.ReferenceNumber.Contains(searchId));
        if (!string.IsNullOrEmpty(client))
            query = query.Where(q => q.ClientName != null && q.ClientName.Contains(client));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        var items = await query
            .Include(q => q.Enquiry)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .Select(q => new QuotationListDto(
                q.Id,
                q.ReferenceNumber,
                q.EnquiryId,
                q.Enquiry != null ? q.Enquiry.EnquiryNumber : null,
                q.ClientName,
                q.ProjectName,
                q.Version,
                q.CreatedAt,
                q.Amount,
                q.Status))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var q = await _db.Quotations.AsNoTracking()
            .Include(x => x.Enquiry)
            .Include(x => x.LineItems.OrderBy(li => li.SortOrder))
            .Include(x => x.Revisions.OrderByDescending(r => r.ChangedAt))
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q == null) return NotFound();

        var lineItems = q.LineItems.Select(li => new QuotationLineItemDto(
            li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList();
        var revisions = q.Revisions.Select(r => new QuotationRevisionDto(
            r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName, r.SentToCustomerAt, r.SentToCustomerBy)).ToList();

        return Ok(new QuotationDetailDto(
            q.Id, q.ReferenceNumber, q.EnquiryId, q.Enquiry?.EnquiryNumber,
            q.ClientName, q.ProjectName, q.Description, q.Attachments, q.ManualPrice, q.PriceBreakdown, q.Version, q.CreatedAt, q.Amount, q.Status,
            q.CreatedByUserName, lineItems, revisions));
    }

    [HttpGet("{quotationId:guid}/revisions/{revisionId:guid}")]
    public async Task<ActionResult<QuotationRevisionDetailDto>> GetRevisionById(Guid quotationId, Guid revisionId, CancellationToken ct)
    {
        var rev = await _db.QuotationRevisions.AsNoTracking()
            .Include(r => r.Quotation)
            .ThenInclude(q => q!.Enquiry)
            .FirstOrDefaultAsync(r => r.Id == revisionId && r.QuotationId == quotationId, ct);
        if (rev == null) return NotFound();

        var lineItems = Array.Empty<QuotationLineItemDto>();
        if (!string.IsNullOrEmpty(rev.SnapshotLineItemsJson))
        {
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<SnapshotLineItem>>(rev.SnapshotLineItemsJson, opts);
                if (items != null)
                {
                    var sortOrder = 0;
                    lineItems = items.Select(li => new QuotationLineItemDto(
                        Guid.Empty, sortOrder++, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToArray();
                }
            }
            catch { /* fallback to empty */ }
        }

        return Ok(new QuotationRevisionDetailDto(
            rev.Id,
            rev.QuotationId,
            rev.Quotation.ReferenceNumber,
            rev.Quotation.EnquiryId,
            rev.Quotation.Enquiry?.EnquiryNumber,
            rev.Version,
            rev.Action,
            rev.ChangedBy,
            rev.ChangedAt,
            rev.ChangeDetails,
            rev.SnapshotClientName,
            rev.SnapshotProjectName,
            rev.SnapshotDescription,
            rev.SnapshotAttachments,
            rev.SnapshotManualPrice,
            rev.SnapshotPriceBreakdown,
            rev.SnapshotStatus,
            rev.SnapshotAmount,
            lineItems,
            rev.SentToCustomerAt,
            rev.SentToCustomerBy));
    }

    [HttpPost("{quotationId:guid}/revisions/{revisionId:guid}/send")]
    public async Task<ActionResult<SendRevisionResponse>> SendRevisionToCustomer(Guid quotationId, Guid revisionId, [FromBody] SendRevisionRequest? req, CancellationToken ct)
    {
        var rev = await _db.QuotationRevisions
            .Include(r => r.Quotation)
            .ThenInclude(q => q!.LineItems.OrderBy(li => li.SortOrder))
            .FirstOrDefaultAsync(r => r.Id == revisionId && r.QuotationId == quotationId, ct);
        if (rev == null) return NotFound();
        
        rev.SentToCustomerAt = DateTime.UtcNow;
        rev.SentToCustomerBy = req?.SentBy ?? "System";
        
        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Quotation {rev.Quotation.ReferenceNumber} revision {rev.Version} sent to customer by {rev.SentToCustomerBy}"
        });

        // Create a draft Purchase Order linked to this quotation and revision
        var year = DateTime.UtcNow.Year;
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextYearStart = yearStart.AddYears(1);
        var count = await _db.PurchaseOrders.CountAsync(p => p.CreatedAt >= yearStart && p.CreatedAt < nextYearStart, ct);
        var orderNumber = $"PO-{year % 100}{count + 1:D4}";

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            QuotationId = rev.QuotationId,
            QuotationRevisionId = rev.Id,
            CustomerPONumber = "", // Empty for draft, to be filled when customer PO is received
            PODate = DateTime.UtcNow,
            DeliveryTerms = null,
            PaymentTerms = null,
            Status = "Draft",
            // Customer PO: pricing is set later on Purchase Invoice, not on PO.
            Value = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserName = rev.SentToCustomerBy ?? "System"
        };

        // Copy line items from revision snapshot or quotation
        var sortOrder = 0;
        
        if (!string.IsNullOrEmpty(rev.SnapshotLineItemsJson))
        {
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var snapshotItems = JsonSerializer.Deserialize<List<SnapshotLineItem>>(rev.SnapshotLineItemsJson, opts);
                if (snapshotItems != null)
                {
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
                            UnitPrice = 0,
                            TaxPercent = 0,
                            AttachmentPath = qli.AttachmentPath
                        };
                        po.LineItems.Add(li);
                        _db.PurchaseOrderLineItems.Add(li);
                    }
                    po.Value = null;
                }
            }
            catch { /* fallback to quotation line items below */ }
        }
        
        // Fallback to quotation line items if snapshot not available
        if (po.LineItems.Count == 0 && rev.Quotation != null)
        {
            foreach (var qli in rev.Quotation.LineItems)
            {
                var li = new PurchaseOrderLineItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    SortOrder = sortOrder++,
                    PartNumber = qli.PartNumber,
                    Description = qli.Description,
                    Quantity = qli.Quantity,
                    UnitPrice = 0,
                    TaxPercent = 0,
                    AttachmentPath = qli.AttachmentPath
                };
                po.LineItems.Add(li);
                _db.PurchaseOrderLineItems.Add(li);
            }
            po.Value = null;
        }

        _db.PurchaseOrders.Add(po);
        
        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Purchase order {po.OrderNumber} created automatically from quotation {(rev.Quotation?.ReferenceNumber ?? $"QT-{rev.QuotationId.ToString()[..8]}")} revision {rev.Version}"
        });

        await _db.SaveChangesAsync(ct);

        // Return response with both revision and PO info
        return Ok(new SendRevisionResponse(
            new QuotationRevisionDto(rev.Id, rev.Version, rev.Action, rev.ChangedBy, rev.ChangedAt, rev.ChangeDetails, rev.AttachmentFileName, rev.SentToCustomerAt, rev.SentToCustomerBy),
            new PurchaseOrderCreatedDto(po.Id, po.OrderNumber, po.QuotationId, po.QuotationRevisionId, po.Status, po.Value)
        ));
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDetailDto>> Create([FromBody] CreateQuotationRequest req, CancellationToken ct)
    {
        string refNum;
        string version = "v1.0";
        string? clientName = null;
        string? projectName = null;
        string? enquiryNumber = null;

        if (req.EnquiryId.HasValue)
        {
            var enquiry = await _db.Enquiries.FirstOrDefaultAsync(e => e.Id == req.EnquiryId.Value && !e.IsDeleted, ct);
            if (enquiry == null) return BadRequest("Enquiry not found");
            clientName = enquiry.CompanyName;
            projectName = enquiry.ProductDescription;
            enquiryNumber = enquiry.EnquiryNumber;
            enquiry.Status = "Converted";
        }

        var year = DateTime.UtcNow.Year;
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextYearStart = yearStart.AddYears(1);
        var count = await _db.Quotations.CountAsync(q => q.CreatedAt >= yearStart && q.CreatedAt < nextYearStart, ct);
        refNum = $"QT-{year % 100}{count + 1:D4}";

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = refNum,
            EnquiryId = req.EnquiryId,
            Version = version,
            ClientName = clientName,
            ProjectName = projectName,
            Status = "Draft",
            Amount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserName = req.CreatedBy ?? "System"
        };
        _db.Quotations.Add(quotation);

        var revision = new QuotationRevision
        {
            Id = Guid.NewGuid(),
            QuotationId = quotation.Id,
            Version = version,
            Action = "initiated draft",
            ChangedBy = quotation.CreatedByUserName ?? "System",
            ChangedAt = DateTime.UtcNow,
            ChangeDetails = projectName != null ? $"Project: \"{projectName}\"" : null,
            SnapshotClientName = clientName,
            SnapshotProjectName = projectName,
            SnapshotStatus = "Draft",
            SnapshotAmount = 0,
            SnapshotLineItemsJson = "[]"
        };
        _db.QuotationRevisions.Add(revision);

        await _db.SaveChangesAsync(ct);

        var created = await _db.Quotations.AsNoTracking()
            .Include(x => x.LineItems)
            .Include(x => x.Revisions)
            .FirstAsync(x => x.Id == quotation.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = quotation.Id }, new QuotationDetailDto(
            created.Id, created.ReferenceNumber, created.EnquiryId, enquiryNumber,
            created.ClientName, created.ProjectName, created.Description, created.Attachments, created.ManualPrice, created.PriceBreakdown, created.Version, created.CreatedAt, created.Amount, created.Status,
            created.CreatedByUserName,
            created.LineItems.OrderBy(li => li.SortOrder).Select(li => new QuotationLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList(),
            created.Revisions.OrderByDescending(r => r.ChangedAt).Select(r => new QuotationRevisionDto(r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName, r.SentToCustomerAt, r.SentToCustomerBy)).ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateQuotationRequest req, CancellationToken ct)
    {
        // Include Revisions so version increment logic works correctly.
        var q = await _db.Quotations
            .Include(x => x.LineItems)
            .Include(x => x.Revisions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q == null) return NotFound();

        if (req.Status != null) q.Status = req.Status;
        if (req.ClientName != null) q.ClientName = req.ClientName;
        if (req.ProjectName != null) q.ProjectName = req.ProjectName;
        if (req.Description != null) q.Description = req.Description;
        if (req.Attachments != null) q.Attachments = req.Attachments;
        if (req.ManualPrice.HasValue) q.ManualPrice = req.ManualPrice;
        if (req.PriceBreakdown != null) q.PriceBreakdown = req.PriceBreakdown;

        if (req.LineItems != null)
        {
            _db.QuotationLineItems.RemoveRange(q.LineItems);
            var sortOrder = 0;
            decimal subtotal = 0;
            decimal totalTax = 0;
            foreach (var item in req.LineItems)
            {
                var li = new QuotationLineItem
                {
                    Id = Guid.NewGuid(),
                    QuotationId = q.Id,
                    SortOrder = sortOrder++,
                    PartNumber = item.PartNumber ?? "",
                    Description = item.Description ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxPercent = item.TaxPercent,
                    AttachmentPath = item.AttachmentPath
                };
                _db.QuotationLineItems.Add(li);
                var lineTotal = li.Quantity * li.UnitPrice;
                subtotal += lineTotal;
                totalTax += lineTotal * (li.TaxPercent / 100m);
            }
            q.Amount = subtotal + totalTax;
        }

        if (q.ManualPrice.HasValue) q.Amount = q.ManualPrice.Value;

        var changedBy = req.ChangedBy ?? "System";
        var changeDetails = "Details and line items saved";
        var lineItemsForSnapshot = req.LineItems != null
            ? req.LineItems.Select(li => new { PartNumber = li.PartNumber ?? "", Description = li.Description ?? "", li.Quantity, li.UnitPrice, li.TaxPercent, AttachmentPath = li.AttachmentPath }).ToList()
            : q.LineItems.OrderBy(li => li.SortOrder).Select(li => new { li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath }).ToList();
        var nextRevisionVersion = GetNextRevisionVersion(q);
        q.Version = nextRevisionVersion;
        _db.QuotationRevisions.Add(new QuotationRevision
        {
            Id = Guid.NewGuid(),
            QuotationId = q.Id,
            Version = nextRevisionVersion,
            Action = "updated",
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            ChangeDetails = changeDetails,
            SnapshotClientName = q.ClientName,
            SnapshotProjectName = q.ProjectName,
            SnapshotDescription = q.Description,
            SnapshotAttachments = q.Attachments,
            SnapshotManualPrice = q.ManualPrice,
            SnapshotPriceBreakdown = q.PriceBreakdown,
            SnapshotStatus = q.Status,
            SnapshotAmount = q.Amount,
            SnapshotLineItemsJson = JsonSerializer.Serialize(lineItemsForSnapshot)
        });

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Quotation {q.ReferenceNumber} details saved by {changedBy}"
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/revision")]
    public async Task<ActionResult<QuotationDetailDto>> GenerateRevision(Guid id, [FromBody] GenerateRevisionRequest req, CancellationToken ct)
    {
        var q = await _db.Quotations.Include(x => x.LineItems).Include(x => x.Revisions).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q == null) return NotFound();

        // Use the same incrementing logic as Save Details (v1.1, v1.2, ...)
        var newVersion = GetNextRevisionVersion(q);
        q.Version = newVersion;
        if (req.ManualPrice.HasValue) q.ManualPrice = req.ManualPrice;
        if (req.PriceBreakdown != null) q.PriceBreakdown = req.PriceBreakdown;

        decimal subtotal = 0;
        decimal totalTax = 0;
        if (req.LineItems != null)
        {
            _db.QuotationLineItems.RemoveRange(q.LineItems);
            var sortOrder = 0;
            foreach (var item in req.LineItems)
            {
                var li = new QuotationLineItem
                {
                    Id = Guid.NewGuid(),
                    QuotationId = q.Id,
                    SortOrder = sortOrder++,
                    PartNumber = item.PartNumber ?? "",
                    Description = item.Description ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxPercent = item.TaxPercent,
                    AttachmentPath = item.AttachmentPath
                };
                _db.QuotationLineItems.Add(li);
                var lineTotal = li.Quantity * li.UnitPrice;
                subtotal += lineTotal;
                totalTax += lineTotal * (li.TaxPercent / 100m);
            }
        }
        else
        {
            var lineItems = await _db.QuotationLineItems.Where(li => li.QuotationId == id).OrderBy(li => li.SortOrder).ToListAsync(ct);
            foreach (var li in lineItems)
            {
                var lineTotal = li.Quantity * li.UnitPrice;
                subtotal += lineTotal;
                totalTax += lineTotal * (li.TaxPercent / 100m);
            }
        }
        q.Amount = subtotal + totalTax;
        if (q.ManualPrice.HasValue) q.Amount = q.ManualPrice.Value;

        var changeDetails = req.ChangeDetails ?? "Line items updated";
        var lineItemsForSnapshot = await _db.QuotationLineItems.Where(li => li.QuotationId == id).OrderBy(li => li.SortOrder).Select(li => new { li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath }).ToListAsync(ct);
        var revision = new QuotationRevision
        {
            Id = Guid.NewGuid(),
            QuotationId = q.Id,
            Version = newVersion,
            Action = "created",
            ChangedBy = req.ChangedBy ?? "System",
            ChangedAt = DateTime.UtcNow,
            ChangeDetails = changeDetails,
            SnapshotClientName = q.ClientName,
            SnapshotProjectName = q.ProjectName,
            SnapshotDescription = q.Description,
            SnapshotAttachments = q.Attachments,
            SnapshotManualPrice = q.ManualPrice,
            SnapshotPriceBreakdown = q.PriceBreakdown,
            SnapshotStatus = q.Status,
            SnapshotAmount = q.Amount,
            SnapshotLineItemsJson = JsonSerializer.Serialize(lineItemsForSnapshot)
        };
        _db.QuotationRevisions.Add(revision);

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Revision {newVersion} created for {q.ReferenceNumber}"
        });

        await _db.SaveChangesAsync(ct);

        var updated = await _db.Quotations.AsNoTracking()
            .Include(x => x.Enquiry)
            .Include(x => x.LineItems.OrderBy(li => li.SortOrder))
            .Include(x => x.Revisions.OrderByDescending(r => r.ChangedAt))
            .FirstAsync(x => x.Id == id, ct);

        return Ok(new QuotationDetailDto(
            updated.Id, updated.ReferenceNumber, updated.EnquiryId, updated.Enquiry?.EnquiryNumber,
            updated.ClientName, updated.ProjectName, updated.Description, updated.Attachments, updated.ManualPrice, updated.PriceBreakdown, updated.Version, updated.CreatedAt, updated.Amount, updated.Status,
            updated.CreatedByUserName,
            updated.LineItems.Select(li => new QuotationLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList(),
            updated.Revisions.Select(r => new QuotationRevisionDto(r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName, r.SentToCustomerAt, r.SentToCustomerBy)).ToList()));
    }
}

public record CreateQuotationRequest(Guid? EnquiryId, string? CreatedBy);
public record UpdateQuotationRequest(string? Status, string? ClientName, string? ProjectName, string? Description, string? Attachments, decimal? ManualPrice, string? PriceBreakdown, List<LineItemInput>? LineItems, string? ChangedBy);
public record GenerateRevisionRequest(List<LineItemInput>? LineItems, decimal? ManualPrice, string? PriceBreakdown, string? ChangeDetails, string? ChangedBy);
public record LineItemInput(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);

public record QuotationListDto(Guid Id, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string? ClientName, string? ProjectName, string Version, DateTime CreatedAt, decimal? Amount, string Status);
public record QuotationDetailDto(Guid Id, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string? ClientName, string? ProjectName, string? Description, string? Attachments, decimal? ManualPrice, string? PriceBreakdown, string Version, DateTime CreatedAt, decimal? Amount, string Status, string? CreatedByUserName, IReadOnlyList<QuotationLineItemDto> LineItems, IReadOnlyList<QuotationRevisionDto> Revisions);
public record QuotationLineItemDto(Guid Id, int SortOrder, string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);
public record SendRevisionRequest(string? SentBy);
public record SendRevisionResponse(QuotationRevisionDto Revision, PurchaseOrderCreatedDto PurchaseOrder);
public record PurchaseOrderCreatedDto(Guid Id, string OrderNumber, Guid? QuotationId, Guid? QuotationRevisionId, string Status, decimal? Value);
public record QuotationRevisionDto(Guid Id, string Version, string Action, string ChangedBy, DateTime ChangedAt, string? ChangeDetails, string? AttachmentFileName, DateTime? SentToCustomerAt, string? SentToCustomerBy);
public record QuotationRevisionDetailDto(Guid Id, Guid QuotationId, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string Version, string Action, string ChangedBy, DateTime ChangedAt, string? ChangeDetails, string? ClientName, string? ProjectName, string? Description, string? Attachments, decimal? ManualPrice, string? PriceBreakdown, string? Status, decimal? Amount, IReadOnlyList<QuotationLineItemDto> LineItems, DateTime? SentToCustomerAt, string? SentToCustomerBy);
internal record SnapshotLineItem(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);
