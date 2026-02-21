using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Enums;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public WorkOrdersController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkOrderCardDto>>> Get(
        [FromQuery] string? status,
        [FromQuery] string? assignedTo,
        [FromQuery] bool? sentToProduction,
        [FromQuery] bool? productionReceived,
        [FromQuery] int limit = 200,
        CancellationToken ct = default)
    {
        try
        {
            // Only show work orders that were created via conversion (linked to an invoice)
            var query = _db.WorkOrders.AsNoTracking().Where(w => w.PurchaseInvoiceId != null).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(w => w.Status == status);
            if (!string.IsNullOrWhiteSpace(assignedTo))
                query = query.Where(w => w.AssignedToUserName != null && w.AssignedToUserName.Contains(assignedTo));
            if (sentToProduction.HasValue)
                query = sentToProduction.Value ? query.Where(w => w.SentToProductionAt != null) : query.Where(w => w.SentToProductionAt == null);
            if (productionReceived.HasValue)
                query = productionReceived.Value ? query.Where(w => w.ProductionReceivedAt != null) : query.Where(w => w.ProductionReceivedAt == null);

            var items = await query
                .Include(w => w.PurchaseInvoice)
                .Include(w => w.LineItems)
                .OrderByDescending(w => w.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);

            var dto = items.Select(w => new WorkOrderCardDto(
                w.Id,
                w.OrderNumber,
                w.Stage.ToString(),
                w.Status,
                w.AssignedToUserName,
                w.CreatedAt,
                w.PurchaseInvoiceId,
                w.PurchaseInvoice?.InvoiceNumber,
                w.SentToProductionAt,
                w.SentToProductionBy,
                w.ProductionReceivedAt,
                w.ProductionReceivedBy,
                w.LineItems
                    .OrderBy(li => li.SortOrder)
                    .Select(li => new WorkOrderCardLineItemDto(li.PartNumber, li.Description, li.Quantity))
                    .ToList()
            )).ToList();

            return Ok(dto);
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            return StatusCode(500,
                "Database schema for Work Orders is not up to date. Run `scripts/apply-all-migrations.ps1` and restart the API.");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrderDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        try
        {
            var wo = await _db.WorkOrders.AsNoTracking()
                .Include(w => w.LineItems)
                .FirstOrDefaultAsync(w => w.Id == id, ct);
            if (wo == null) return NotFound();

            var lineItems = wo.LineItems
                .OrderBy(li => li.SortOrder)
                .Select(li => new WorkOrderLineItemDto(li.Id, li.SortOrder, li.PartNumber, li.Description, li.Quantity, li.UnitPrice, li.TaxPercent, li.LineTotal))
                .ToList();

            return Ok(new WorkOrderDetailDto(
                wo.Id,
                wo.OrderNumber,
                wo.Stage.ToString(),
                wo.Status,
                wo.AssignedToUserName,
                wo.CreatedAt,
                wo.PurchaseInvoiceId,
                lineItems));
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            return StatusCode(500,
                "Database schema for Work Orders is not up to date. Run `scripts/apply-all-migrations.ps1` and restart the API.");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateWorkOrderRequest req, CancellationToken ct = default)
    {
        try
        {
            var wo = await _db.WorkOrders.Include(w => w.LineItems).FirstOrDefaultAsync(w => w.Id == id, ct);
            if (wo == null) return NotFound();

            if (req.Status != null) wo.Status = req.Status;
            if (req.AssignedToUserName != null) wo.AssignedToUserName = req.AssignedToUserName;
            if (req.Stage != null && Enum.TryParse<WorkOrderStage>(req.Stage, true, out var stage))
                wo.Stage = stage;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            return StatusCode(500,
                "Database schema for Work Orders is not up to date. Run `scripts/apply-all-migrations.ps1` and restart the API.");
        }
    }

    [HttpPost("{id:guid}/send-to-production")]
    public async Task<ActionResult> SendToProduction(Guid id, [FromQuery] string? sentBy, CancellationToken ct = default)
    {
        try
        {
            var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id, ct);
            if (wo == null) return NotFound();

            if (wo.SentToProductionAt != null)
                return Ok(new { alreadySent = true, wo.OrderNumber, wo.SentToProductionAt, wo.SentToProductionBy });

            wo.SentToProductionAt = DateTime.UtcNow;
            wo.SentToProductionBy = string.IsNullOrWhiteSpace(sentBy) ? "System" : sentBy.Trim();

            _db.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                Tag = "AUDIT",
                Message = $"Work order {wo.OrderNumber} sent to production by {wo.SentToProductionBy}"
            });

            await _db.SaveChangesAsync(ct);
            return Ok(new { sent = true, wo.OrderNumber, wo.SentToProductionAt, wo.SentToProductionBy });
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            return StatusCode(500,
                "Database schema for Work Orders is not up to date. Run `scripts/apply-all-migrations.ps1` and restart the API.");
        }
    }

    [HttpPost("{id:guid}/receive-by-production")]
    public async Task<ActionResult> ReceiveByProduction(Guid id, [FromQuery] string? receivedBy, CancellationToken ct = default)
    {
        try
        {
            var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id, ct);
            if (wo == null) return NotFound();
            if (wo.SentToProductionAt == null) return BadRequest("Work order has not been sent to production.");

            if (wo.ProductionReceivedAt != null)
                return Ok(new { alreadyReceived = true, wo.OrderNumber, wo.ProductionReceivedAt, wo.ProductionReceivedBy });

            wo.ProductionReceivedAt = DateTime.UtcNow;
            wo.ProductionReceivedBy = string.IsNullOrWhiteSpace(receivedBy) ? "System" : receivedBy.Trim();

            _db.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                Tag = "AUDIT",
                Message = $"Work order {wo.OrderNumber} received by production ({wo.ProductionReceivedBy})"
            });

            await _db.SaveChangesAsync(ct);
            return Ok(new { received = true, wo.OrderNumber, wo.ProductionReceivedAt, wo.ProductionReceivedBy });
        }
        catch (PostgresException ex) when (ex.SqlState is "42703" or "42P01")
        {
            return StatusCode(500,
                "Database schema for Work Orders is not up to date. Run `scripts/apply-all-migrations.ps1` and restart the API.");
        }
    }

    [HttpPost("from-invoice/{purchaseInvoiceId:guid}")]
    public async Task<ActionResult<WorkOrderCardDto>> CreateFromInvoice(Guid purchaseInvoiceId, [FromQuery] string? createdBy, CancellationToken ct = default)
    {
        try
        {
            var inv = await _db.PurchaseInvoices
                .AsNoTracking()
                .Include(i => i.PurchaseOrder)
                .ThenInclude(po => po!.LineItems)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == purchaseInvoiceId, ct);
            if (inv == null) return BadRequest("Purchase invoice not found.");

            var existing = await _db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(w => w.PurchaseInvoiceId == purchaseInvoiceId, ct);
            if (existing != null)
                return Ok(new WorkOrderCardDto(existing.Id, existing.OrderNumber, existing.Stage.ToString(), existing.Status, existing.AssignedToUserName, existing.CreatedAt, existing.PurchaseInvoiceId, inv.InvoiceNumber, existing.SentToProductionAt, existing.SentToProductionBy, existing.ProductionReceivedAt, existing.ProductionReceivedBy, []));

            var year = DateTime.UtcNow.Year;
            var count = await _db.WorkOrders.CountAsync(w => w.CreatedAt.Year == year, ct);
            var orderNumber = $"WO-{year % 100}{count + 1:D4}";

            var wo = new WorkOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                Stage = WorkOrderStage.Planning,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                PurchaseInvoiceId = inv.Id
            };

            _db.WorkOrders.Add(wo);

            // Copy line items from invoice; if invoice has none, fall back to PO lines.
            var sourceLines = inv.LineItems.Count > 0
                ? inv.LineItems.Select(li => new
                {
                    li.PartNumber,
                    li.Description,
                    li.Quantity,
                    li.UnitPrice,
                    li.TaxPercent,
                    LineTotal = li.LineTotal ?? li.Quantity * li.UnitPrice * (1 + li.TaxPercent / 100m)
                }).ToList()
                : (inv.PurchaseOrder?.LineItems ?? []).OrderBy(li => li.SortOrder).Select(li => new
                {
                    li.PartNumber,
                    li.Description,
                    li.Quantity,
                    li.UnitPrice,
                    li.TaxPercent,
                    LineTotal = li.Quantity * li.UnitPrice * (1 + li.TaxPercent / 100m)
                }).ToList();

            var sort = 0;
            var cardLines = new List<WorkOrderCardLineItemDto>();
            foreach (var s in sourceLines)
            {
                _db.WorkOrderLineItems.Add(new WorkOrderLineItem
                {
                    Id = Guid.NewGuid(),
                    WorkOrderId = wo.Id,
                    SortOrder = sort++,
                    PartNumber = s.PartNumber ?? "",
                    Description = s.Description ?? "",
                    Quantity = s.Quantity,
                    UnitPrice = s.UnitPrice,
                    TaxPercent = s.TaxPercent,
                    LineTotal = s.LineTotal
                });

                cardLines.Add(new WorkOrderCardLineItemDto(s.PartNumber ?? "", s.Description ?? "", s.Quantity));
            }

            _db.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                Tag = "AUDIT",
                Message = $"Work order {wo.OrderNumber} created from invoice {inv.InvoiceNumber} (PO {inv.PurchaseOrder?.OrderNumber ?? "—"}) by {createdBy ?? "System"}"
            });

            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = wo.Id }, new WorkOrderCardDto(
                wo.Id,
                wo.OrderNumber,
                wo.Stage.ToString(),
                wo.Status,
                wo.AssignedToUserName,
                wo.CreatedAt,
                wo.PurchaseInvoiceId,
                inv.InvoiceNumber,
                wo.SentToProductionAt,
                wo.SentToProductionBy,
                wo.ProductionReceivedAt,
                wo.ProductionReceivedBy,
                cardLines));
        }
        catch (PostgresException ex) when (ex.SqlState == "42703" && ex.MessageText.Contains("PurchaseInvoiceId", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(500,
                "Database schema is missing column WorkOrders.PurchaseInvoiceId. Apply migrations (run `scripts/apply-all-migrations.ps1` or add the missing column/FK) and restart the API.");
        }
        catch (PostgresException ex) when (ex.SqlState == "42703" && ex.MessageText.Contains("AssignedToUserName", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(500,
                "Database schema is missing column WorkOrders.AssignedToUserName. Apply migrations (run `scripts/apply-all-migrations.ps1`) and restart the API.");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" && ex.MessageText.Contains("WorkOrderLineItems", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(500,
                "Database schema is missing table WorkOrderLineItems. Apply migrations (run `scripts/apply-all-migrations.ps1`) and restart the API.");
        }
    }
}

public record WorkOrderCardDto(
    Guid Id,
    string OrderNumber,
    string Stage,
    string Status,
    string? AssignedToUserName,
    DateTime CreatedAt,
    Guid? PurchaseInvoiceId,
    string? PurchaseInvoiceNumber,
    DateTime? SentToProductionAt,
    string? SentToProductionBy,
    DateTime? ProductionReceivedAt,
    string? ProductionReceivedBy,
    IReadOnlyList<WorkOrderCardLineItemDto> LineItems);

public record WorkOrderCardLineItemDto(
    string PartNumber,
    string Description,
    int Quantity);

public record WorkOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Stage,
    string Status,
    string? AssignedToUserName,
    DateTime CreatedAt,
    Guid? PurchaseInvoiceId,
    IReadOnlyList<WorkOrderLineItemDto> LineItems);

public record WorkOrderLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal);

public record UpdateWorkOrderRequest(string? Stage, string? Status, string? AssignedToUserName);

