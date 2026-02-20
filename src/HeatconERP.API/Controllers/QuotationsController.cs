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
            r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName)).ToList();

        return Ok(new QuotationDetailDto(
            q.Id, q.ReferenceNumber, q.EnquiryId, q.Enquiry?.EnquiryNumber,
            q.ClientName, q.ProjectName, q.Description, q.Attachments, q.Version, q.CreatedAt, q.Amount, q.Status,
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
            rev.SnapshotStatus,
            rev.SnapshotAmount,
            lineItems));
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDetailDto>> Create([FromBody] CreateQuotationRequest req, CancellationToken ct)
    {
        string refNum;
        string version = "v1";
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
        var count = await _db.Quotations.CountAsync(q => q.CreatedAt.Year == year, ct);
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
            created.ClientName, created.ProjectName, created.Description, created.Attachments, created.Version, created.CreatedAt, created.Amount, created.Status,
            created.CreatedByUserName,
            created.LineItems.OrderBy(li => li.SortOrder).Select(li => new QuotationLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList(),
            created.Revisions.OrderByDescending(r => r.ChangedAt).Select(r => new QuotationRevisionDto(r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName)).ToList()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateQuotationRequest req, CancellationToken ct)
    {
        var q = await _db.Quotations.Include(x => x.LineItems).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q == null) return NotFound();

        if (req.Status != null) q.Status = req.Status;
        if (req.ClientName != null) q.ClientName = req.ClientName;
        if (req.ProjectName != null) q.ProjectName = req.ProjectName;
        if (req.Description != null) q.Description = req.Description;
        if (req.Attachments != null) q.Attachments = req.Attachments;

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

        var changedBy = req.ChangedBy ?? "System";
        var changeDetails = "Details and line items saved";
        var lineItemsForSnapshot = req.LineItems != null
            ? req.LineItems.Select(li => new { PartNumber = li.PartNumber ?? "", Description = li.Description ?? "", li.Quantity, li.UnitPrice, li.TaxPercent, AttachmentPath = li.AttachmentPath }).ToList()
            : q.LineItems.OrderBy(li => li.SortOrder).Select(li => new { li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath }).ToList();
        _db.QuotationRevisions.Add(new QuotationRevision
        {
            Id = Guid.NewGuid(),
            QuotationId = q.Id,
            Version = q.Version,
            Action = "updated",
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            ChangeDetails = changeDetails,
            SnapshotClientName = q.ClientName,
            SnapshotProjectName = q.ProjectName,
            SnapshotDescription = q.Description,
            SnapshotAttachments = q.Attachments,
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

        // Base new version on the highest existing revision (v1, v2, v3, ...)
        var allVersions = q.Revisions.Select(r => r.Version).Append(q.Version).ToList();
        var maxNum = 1;
        foreach (var v in allVersions)
        {
            var numStr = v.TrimStart('v', 'V').Split('.')[0];
            if (int.TryParse(numStr, out var num) && num > maxNum)
                maxNum = num;
        }
        var newVersion = $"v{maxNum + 1}";

        q.Version = newVersion;

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
            updated.ClientName, updated.ProjectName, updated.Description, updated.Attachments, updated.Version, updated.CreatedAt, updated.Amount, updated.Status,
            updated.CreatedByUserName,
            updated.LineItems.Select(li => new QuotationLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.AttachmentPath)).ToList(),
            updated.Revisions.Select(r => new QuotationRevisionDto(r.Id, r.Version, r.Action, r.ChangedBy, r.ChangedAt, r.ChangeDetails, r.AttachmentFileName)).ToList()));
    }
}

public record CreateQuotationRequest(Guid? EnquiryId, string? CreatedBy);
public record UpdateQuotationRequest(string? Status, string? ClientName, string? ProjectName, string? Description, string? Attachments, List<LineItemInput>? LineItems, string? ChangedBy);
public record GenerateRevisionRequest(List<LineItemInput>? LineItems, string? ChangeDetails, string? ChangedBy);
public record LineItemInput(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);

public record QuotationListDto(Guid Id, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string? ClientName, string? ProjectName, string Version, DateTime CreatedAt, decimal? Amount, string Status);
public record QuotationDetailDto(Guid Id, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string? ClientName, string? ProjectName, string? Description, string? Attachments, string Version, DateTime CreatedAt, decimal? Amount, string Status, string? CreatedByUserName, IReadOnlyList<QuotationLineItemDto> LineItems, IReadOnlyList<QuotationRevisionDto> Revisions);
public record QuotationLineItemDto(Guid Id, int SortOrder, string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);
public record QuotationRevisionDto(Guid Id, string Version, string Action, string ChangedBy, DateTime ChangedAt, string? ChangeDetails, string? AttachmentFileName);
public record QuotationRevisionDetailDto(Guid Id, Guid QuotationId, string ReferenceNumber, Guid? EnquiryId, string? EnquiryNumber, string Version, string Action, string ChangedBy, DateTime ChangedAt, string? ChangeDetails, string? ClientName, string? ProjectName, string? Description, string? Attachments, string? Status, decimal? Amount, IReadOnlyList<QuotationLineItemDto> LineItems);
internal record SnapshotLineItem(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath);
