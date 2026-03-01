using HeatconERP.Domain.Enums;
using HeatconERP.Domain.Enums.Inventory;
using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductionController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public ProductionController(HeatconDbContext db) => _db = db;

    private static readonly WorkOrderStage[] StageOrder =
    [
        WorkOrderStage.Planning,
        WorkOrderStage.Material,
        WorkOrderStage.Assembly,
        WorkOrderStage.Testing,
        WorkOrderStage.QC,
        WorkOrderStage.Packing
    ];

    private async Task EnsureGatesExistAsync(Guid workOrderId, CancellationToken ct)
    {
        var existing = await _db.WorkOrderQualityGates
            .Where(g => g.WorkOrderId == workOrderId)
            .Select(g => g.Stage)
            .ToListAsync(ct);
        var set = existing.ToHashSet();
        var now = DateTime.UtcNow;
        var toAdd = new List<WorkOrderQualityGate>();
        foreach (var s in StageOrder)
        {
            if (set.Contains(s)) continue;
            toAdd.Add(new WorkOrderQualityGate { WorkOrderId = workOrderId, Stage = s, CreatedAt = now });
        }
        if (toAdd.Count > 0) _db.WorkOrderQualityGates.AddRange(toAdd);
    }

    private async Task<List<string>> GetStageBlockersAsync(Guid workOrderId, WorkOrderStage targetStage, CancellationToken ct)
    {
        var targetIndex = Array.IndexOf(StageOrder, targetStage);
        if (targetIndex <= 0) return [];

        var requiredStages = StageOrder.Take(targetIndex).ToArray();

        var gates = await _db.WorkOrderQualityGates.AsNoTracking()
            .Where(g => g.WorkOrderId == workOrderId && requiredStages.Contains(g.Stage))
            .Select(g => new { g.Stage, g.GateStatus })
            .ToListAsync(ct);
        var gateMap = gates.ToDictionary(x => x.Stage, x => x.GateStatus);

        var openNcrStages = await _db.Ncrs.AsNoTracking()
            .Where(n => n.WorkOrderId == workOrderId && requiredStages.Contains(n.Stage) && n.Status == NcrStatus.Open)
            .Select(n => n.Stage)
            .Distinct()
            .ToListAsync(ct);
        var openSet = openNcrStages.ToHashSet();

        var blockers = new List<string>();
        foreach (var s in requiredStages)
        {
            if (!gateMap.TryGetValue(s, out var st))
            {
                blockers.Add($"{s}: gate missing (treat as Pending)");
                continue;
            }
            if (st != QualityGateStatus.Passed)
                blockers.Add($"{s}: gate is {st}");
            if (openSet.Contains(s))
                blockers.Add($"{s}: NCR is Open");
        }
        return blockers;
    }

    [HttpGet("workorders")]
    public async Task<ActionResult<IReadOnlyList<WorkOrderDto>>> GetWorkOrders(
        [FromQuery] string? stage,
        [FromQuery] string? status,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var query = _db.WorkOrders.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);
        else
            query = query.Where(w => w.Status == "Active");

        if (!string.IsNullOrEmpty(stage) && Enum.TryParse<WorkOrderStage>(stage, true, out var s))
            query = query.Where(w => w.Stage == s);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Take(limit)
            .Select(w => new WorkOrderDto(w.Id, w.OrderNumber, w.Stage.ToString(), w.Status, w.CreatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("stages")]
    public IActionResult GetStages() => Ok(new[]
    {
        new { Key = "Planning", Label = "Planning" },
        new { Key = "Material", Label = "Material" },
        new { Key = "Assembly", Label = "Assembly" },
        new { Key = "Testing", Label = "Testing" },
        new { Key = "QC", Label = "QC" },
        new { Key = "Packing", Label = "Packing" }
    });

    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateStageRequest req, CancellationToken ct)
    {
        Console.WriteLine($"[ProductionController] UpdateStage called for WO {id}, target stage: {req.Stage}");
        
        var wo = await _db.WorkOrders.FindAsync([id], ct);
        if (wo == null) return NotFound();

        Console.WriteLine($"[ProductionController] Current stage: {wo.Stage}");

        if (Enum.TryParse<WorkOrderStage>(req.Stage, true, out var stage))
        {
            // Enforce QC gating for forward moves
            await EnsureGatesExistAsync(wo.Id, ct);
            await _db.SaveChangesAsync(ct);

            var currentIndex = Array.IndexOf(StageOrder, wo.Stage);
            var targetIndex = Array.IndexOf(StageOrder, stage);
            
            var now = DateTime.UtcNow;
            
            // For forward moves, auto-pass all previous stages' gates and check for open NCRs
            if (targetIndex > currentIndex)
            {
                var previousStages = StageOrder.Take(targetIndex).ToArray();
                
                // Auto-pass all previous stages' quality gates
                var gates = await _db.WorkOrderQualityGates
                    .Where(g => g.WorkOrderId == wo.Id && previousStages.Contains(g.Stage))
                    .ToListAsync(ct);
                
                foreach (var gate in gates)
                {
                    if (gate.GateStatus != QualityGateStatus.Passed)
                    {
                        gate.GateStatus = QualityGateStatus.Passed;
                        gate.PassedAt = now;
                        gate.PassedBy = "System";
                    }
                }
                await _db.SaveChangesAsync(ct);
                
                // Check for open NCRs as blockers (gates are now handled above)
                var openNcrStages = await _db.Ncrs.AsNoTracking()
                    .Where(n => n.WorkOrderId == wo.Id && previousStages.Contains(n.Stage) && n.Status == NcrStatus.Open)
                    .Select(n => n.Stage)
                    .Distinct()
                    .ToListAsync(ct);
                
                if (openNcrStages.Count > 0)
                {
                    var blockers = openNcrStages.Select(s => $"{s}: NCR is Open").ToList();
                    return BadRequest(new { error = "Stage move blocked by QC/NCR gates.", blockers });
                }
            }

            wo.Stage = stage;

            // Mark all previous stages as completed if they aren't already
            for (int i = 0; i < targetIndex; i++)
            {
                var previousStage = StageOrder[i];
                switch (previousStage)
                {
                    case WorkOrderStage.Planning:
                        wo.PlanningCompletedAt ??= now;
                        break;
                    case WorkOrderStage.Material:
                        wo.MaterialCompletedAt ??= now;
                        break;
                    case WorkOrderStage.Assembly:
                        wo.AssemblyCompletedAt ??= now;
                        break;
                    case WorkOrderStage.Testing:
                        wo.TestingCompletedAt ??= now;
                        break;
                    case WorkOrderStage.QC:
                        wo.QcCompletedAt ??= now;
                        break;
                    case WorkOrderStage.Packing:
                        wo.PackingCompletedAt ??= now;
                        break;
                }
            }

            await _db.SaveChangesAsync(ct);
            
            Console.WriteLine($"[ProductionController] Stage updated successfully. New stage: {wo.Stage}");
            Console.WriteLine($"[ProductionController] Completion timestamps - Planning: {wo.PlanningCompletedAt}, Material: {wo.MaterialCompletedAt}, Assembly: {wo.AssemblyCompletedAt}, Testing: {wo.TestingCompletedAt}, QC: {wo.QcCompletedAt}, Packing: {wo.PackingCompletedAt}");
            
            return Ok(new { wo.OrderNumber, Stage = wo.Stage.ToString() });
        }
        return BadRequest(new { error = "Invalid stage" });
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteWorkOrder(Guid id, CancellationToken ct)
    {
        var wo = await _db.WorkOrders.FindAsync([id], ct);
        if (wo == null) return NotFound();

        if (wo.Stage != WorkOrderStage.Packing)
            return BadRequest(new { error = "Work order can only be completed from Packing stage." });

        wo.PackingCompletedAt ??= DateTime.UtcNow;
        wo.WorkCompletedAt = DateTime.UtcNow;
        wo.WorkCompletedBy = "System";
        wo.Status = "Completed";

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Tag = "AUDIT",
            Message = $"Work order {wo.OrderNumber} completed"
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { completed = true, wo.OrderNumber, wo.WorkCompletedAt });
    }

    [HttpGet("{id:guid}/pipeline")]
    public async Task<ActionResult<WorkOrderPipelineDto>> GetWorkOrderPipeline(Guid id, CancellationToken ct)
    {
        var wo = await _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.LineItems)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (wo == null) return NotFound();

        // Check if materials are assigned (SRS exists and not Pending)
        var srs = await _db.SRSs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkOrderId == wo.Id, ct);
        
        var isMaterialsAssigned = srs != null && !string.Equals(srs.Status.ToString(), "Pending", StringComparison.OrdinalIgnoreCase);

        // Build stage timeline
        var stageTimeline = BuildStageTimeline(wo);

        var dto = new WorkOrderPipelineDto(
            wo.Id,
            wo.OrderNumber,
            wo.Status,
            wo.Stage.ToString(),
            wo.CreatedAt,
            wo.ProductionReceivedAt,
            wo.WorkStartedAt,
            wo.WorkStartedBy,
            wo.WorkCompletedAt,
            wo.WorkCompletedBy,
            isMaterialsAssigned,
            stageTimeline,
            wo.LineItems.Select(li => new WorkOrderLineItemDto(
                li.Id,
                li.SortOrder,
                li.PartNumber,
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.TaxPercent,
                li.LineTotal)).ToList());

        return Ok(dto);
    }

    private List<WorkOrderStageTimelineDto> BuildStageTimeline(WorkOrder wo)
    {
        var timeline = new List<WorkOrderStageTimelineDto>();
        var stageStartedAt = wo.ProductionReceivedAt ?? wo.CreatedAt;

        for (int idx = 0; idx < StageOrder.Length; idx++)
        {
            var stage = StageOrder[idx];
            var completedAt = stage switch
            {
                WorkOrderStage.Planning => wo.PlanningCompletedAt,
                WorkOrderStage.Material => wo.MaterialCompletedAt,
                WorkOrderStage.Assembly => wo.AssemblyCompletedAt,
                WorkOrderStage.Testing => wo.TestingCompletedAt,
                WorkOrderStage.QC => wo.QcCompletedAt,
                WorkOrderStage.Packing => wo.PackingCompletedAt,
                _ => null
            };

            var status = stage == wo.Stage && wo.WorkStartedAt != null
                ? "In Progress"
                : completedAt.HasValue
                    ? "Completed"
                    : "Not Started";

            timeline.Add(new WorkOrderStageTimelineDto(
                stage.ToString(),
                idx,
                stage == wo.Stage && wo.WorkStartedAt != null ? wo.WorkStartedAt : (completedAt.HasValue ? stageStartedAt : null),
                completedAt,
                status));
        }

        return timeline;
    }

    [HttpPost("{id:guid}/start-work")]
    public async Task<IActionResult> StartWorkOrder(Guid id, [FromBody] StartWorkRequest req, CancellationToken ct)
    {
        var wo = await _db.WorkOrders.FindAsync([id], ct);
        if (wo == null) return NotFound();

        // Check if materials are assigned  
        var srs = await _db.SRSs
            .FirstOrDefaultAsync(s => s.WorkOrderId == wo.Id, ct);

        if (srs == null || string.Equals(srs.Status.ToString(), "Pending", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Cannot start work: materials are not yet assigned." });

        if (wo.WorkStartedAt.HasValue)
            return BadRequest(new { error = "Work has already been started for this order." });

        wo.WorkStartedAt = DateTime.UtcNow;
        wo.WorkStartedBy = req.Username;

        await _db.SaveChangesAsync(ct);
        return Ok(new { wo.OrderNumber, WorkStartedAt = wo.WorkStartedAt, WorkStartedBy = wo.WorkStartedBy });
    }

    [HttpGet("pipeline/all")]
    public async Task<ActionResult<IReadOnlyList<WorkOrderPipelineDto>>> GetAllWorkOrderPipelines(
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var query = _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.LineItems)
            .AsQueryable();

        // Filter by status if provided, otherwise default to Active
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(w => w.Status == status);
        }
        else
        {
            query = query.Where(w => w.Status == "Active");
        }

        if (!string.IsNullOrEmpty(filter))
        {
            filter = filter.ToLower();
            query = query.Where(w => w.OrderNumber.ToLower().Contains(filter));
        }

        // Only include work orders that have an approved SRS (not Pending)
        var approvedSrsWorkOrderIds = await _db.SRSs
            .AsNoTracking()
            .Where(s => s.Status != SrsStatus.Pending && !s.IsDeleted)
            .Select(s => s.WorkOrderId)
            .Distinct()
            .ToListAsync(ct);

        query = query.Where(w => approvedSrsWorkOrderIds.Contains(w.Id) && w.ProductionReceivedAt != null);

        var workOrders = await query
            .OrderByDescending(w => w.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        var srsMap = await _db.SRSs
            .AsNoTracking()
            .Where(s => workOrders.Select(w => w.Id).Contains(s.WorkOrderId))
            .ToDictionaryAsync(s => s.WorkOrderId, ct);

        var pipelines = workOrders.Select(wo =>
        {
            var srs = srsMap.GetValueOrDefault(wo.Id);
            var isMaterialsAssigned = srs != null && !string.Equals(srs.Status.ToString(), "Pending", StringComparison.OrdinalIgnoreCase);
            var stageTimeline = BuildStageTimeline(wo);

            return new WorkOrderPipelineDto(
                wo.Id,
                wo.OrderNumber,
                wo.Status,
                wo.Stage.ToString(),
                wo.CreatedAt,
                wo.ProductionReceivedAt,
                wo.WorkStartedAt,
                wo.WorkStartedBy,
                wo.WorkCompletedAt,
                wo.WorkCompletedBy,
                isMaterialsAssigned,
                stageTimeline,
                wo.LineItems.Select(li => new WorkOrderLineItemDto(
                    li.Id,
                    li.SortOrder,
                    li.PartNumber,
                    li.Description,
                    li.Quantity,
                    li.UnitPrice,
                    li.TaxPercent,
                    li.LineTotal)).ToList());
        }).ToList();

        return Ok(pipelines);
    }

}

public record WorkOrderDto(Guid Id, string OrderNumber, string Stage, string Status, DateTime CreatedAt);
public record UpdateStageRequest(string Stage);public record StartWorkRequest(string Username);
public record WorkOrderPipelineDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string CurrentStage,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? WorkStartedAt,
    string? WorkStartedBy,
    DateTime? WorkCompletedAt,
    string? WorkCompletedBy,
    bool IsMaterialsAssigned,
    IReadOnlyList<WorkOrderStageTimelineDto> StageTimeline,
    IReadOnlyList<WorkOrderLineItemDto> LineItems);

public record WorkOrderStageTimelineDto(
    string StageName,
    int StageOrder,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string Status);