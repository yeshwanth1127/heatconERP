using HeatconERP.Domain.Enums;
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
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var query = _db.WorkOrders.Where(w => w.Status == "Active").AsQueryable();

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
        var wo = await _db.WorkOrders.FindAsync([id], ct);
        if (wo == null) return NotFound();

        if (Enum.TryParse<WorkOrderStage>(req.Stage, true, out var stage))
        {
            // Enforce QC gating for forward moves (allow jumping forward, but require all prior stage gates Passed and NCRs closed).
            await EnsureGatesExistAsync(wo.Id, ct);
            await _db.SaveChangesAsync(ct);

            var currentIndex = Array.IndexOf(StageOrder, wo.Stage);
            var targetIndex = Array.IndexOf(StageOrder, stage);
            if (targetIndex > currentIndex)
            {
                var blockers = await GetStageBlockersAsync(wo.Id, stage, ct);
                if (blockers.Count > 0)
                    return BadRequest(new { error = "Stage move blocked by QC/NCR gates.", blockers });
            }

            wo.Stage = stage;
            await _db.SaveChangesAsync(ct);
            return Ok(new { wo.OrderNumber, Stage = wo.Stage.ToString() });
        }
        return BadRequest(new { error = "Invalid stage" });
    }
}

public record WorkOrderDto(Guid Id, string OrderNumber, string Stage, string Status, DateTime CreatedAt);
public record UpdateStageRequest(string Stage);
