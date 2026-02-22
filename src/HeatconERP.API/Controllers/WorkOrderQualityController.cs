using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Enums;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api")]
public class WorkOrderQualityController : ControllerBase
{
    private static readonly WorkOrderStage[] StageOrder =
    [
        WorkOrderStage.Planning,
        WorkOrderStage.Material,
        WorkOrderStage.Assembly,
        WorkOrderStage.Testing,
        WorkOrderStage.QC,
        WorkOrderStage.Packing
    ];

    private readonly HeatconDbContext _db;

    public WorkOrderQualityController(HeatconDbContext db) => _db = db;

    private static bool TryParseStage(string stage, out WorkOrderStage parsed) =>
        Enum.TryParse(stage, ignoreCase: true, out parsed);

    private async Task EnsureGatesExistAsync(Guid workOrderId, CancellationToken ct)
    {
        var existing = await _db.WorkOrderQualityGates
            .Where(g => g.WorkOrderId == workOrderId)
            .Select(g => g.Stage)
            .ToListAsync(ct);

        var existingSet = existing.ToHashSet();
        var now = DateTime.UtcNow;
        var toAdd = new List<WorkOrderQualityGate>();
        foreach (var s in StageOrder)
        {
            if (existingSet.Contains(s)) continue;
            toAdd.Add(new WorkOrderQualityGate
            {
                WorkOrderId = workOrderId,
                Stage = s,
                GateStatus = QualityGateStatus.Pending,
                CreatedAt = now
            });
        }

        if (toAdd.Count > 0)
            _db.WorkOrderQualityGates.AddRange(toAdd);
    }

    [HttpGet("quality/production-manager/queues")]
    public async Task<ActionResult<QualityQueuesDto>> GetProductionManagerQueues([FromQuery] int limit = 500, CancellationToken ct = default)
    {
        if (limit <= 0) limit = 500;
        if (limit > 2000) limit = 2000;

        // Production view: only orders that have been accepted by production.
        var wos = await _db.WorkOrders.AsNoTracking()
            .Where(w => w.Status == "Active" && w.ProductionReceivedAt != null)
            .OrderByDescending(w => w.ProductionReceivedAt)
            .Take(limit)
            .Select(w => new { w.Id, w.OrderNumber, w.Stage })
            .ToListAsync(ct);

        var woIds = wos.Select(x => x.Id).ToList();
        if (woIds.Count == 0)
            return Ok(new QualityQueuesDto([], [], []));

        // Ensure gates exist for all (idempotent); keeps the system resilient for older data.
        foreach (var id in woIds)
            await EnsureGatesExistAsync(id, ct);
        await _db.SaveChangesAsync(ct);

        var gates = await _db.WorkOrderQualityGates.AsNoTracking()
            .Where(g => woIds.Contains(g.WorkOrderId))
            .Select(g => new { g.WorkOrderId, g.Stage, g.GateStatus })
            .ToListAsync(ct);

        var gateMap = gates
            .GroupBy(x => x.WorkOrderId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Stage, x => x.GateStatus));

        var openNcrs = await _db.Ncrs.AsNoTracking()
            .Where(n => woIds.Contains(n.WorkOrderId) && n.Status == NcrStatus.Open)
            .Select(n => new { n.Id, n.WorkOrderId, n.Stage })
            .ToListAsync(ct);

        var openByWoStage = openNcrs
            .GroupBy(x => x.WorkOrderId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Stage, x => x.Id));

        // Last QC check per work order + stage (only for work orders in scope)
        var lastChecks = await _db.WorkOrderQualityChecks.AsNoTracking()
            .Where(c => woIds.Contains(c.WorkOrderId))
            .GroupBy(c => new { c.WorkOrderId, c.Stage })
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .Select(x => new { x.WorkOrderId, x.Stage, x.CreatedAt, x.Result })
            .ToListAsync(ct);

        var lastMap = lastChecks
            .GroupBy(x => x.WorkOrderId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Stage, x => new LastCheckInfo(x.CreatedAt, x.Result)));

        static int StageIndex(WorkOrderStage s) => Array.IndexOf(StageOrder, s);

        var awaiting = new List<QualityQueueItemDto>();
        var failedOpen = new List<QualityQueueItemDto>();
        var blocked = new List<QualityQueueItemDto>();

        foreach (var wo in wos)
        {
            var perGate = gateMap.GetValueOrDefault(wo.Id) ?? new Dictionary<WorkOrderStage, QualityGateStatus>();
            var perOpen = openByWoStage.GetValueOrDefault(wo.Id) ?? new Dictionary<WorkOrderStage, Guid>();
            var perLast = lastMap.GetValueOrDefault(wo.Id) ?? new Dictionary<WorkOrderStage, LastCheckInfo>();

            var currentGateStatus = perGate.GetValueOrDefault(wo.Stage, QualityGateStatus.Pending);
            var currentOpenNcrId = perOpen.GetValueOrDefault(wo.Stage, Guid.Empty);
            var hasOpenNcr = currentOpenNcrId != Guid.Empty;

            perLast.TryGetValue(wo.Stage, out var last);

            var item = new QualityQueueItemDto(
                WorkOrderId: wo.Id,
                WorkOrderNumber: wo.OrderNumber,
                CurrentStage: wo.Stage.ToString(),
                CurrentGateStatus: currentGateStatus.ToString(),
                OpenNcrId: hasOpenNcr ? currentOpenNcrId : null,
                LastCheckedAt: last?.CreatedAt,
                LastResult: last?.Result.ToString(),
                Blockers: null);

            if (currentGateStatus == QualityGateStatus.Pending)
                awaiting.Add(item);

            if (currentGateStatus == QualityGateStatus.Failed && hasOpenNcr)
                failedOpen.Add(item);

            // Blocked: any stage prior to current stage not passed, or any open NCR prior to current stage.
            var idx = StageIndex(wo.Stage);
            if (idx > 0)
            {
                var blockers = new List<string>();
                foreach (var s in StageOrder.Take(idx))
                {
                    var st = perGate.GetValueOrDefault(s, QualityGateStatus.Pending);
                    if (st != QualityGateStatus.Passed)
                        blockers.Add($"{s}: {st}");
                    if (perOpen.ContainsKey(s))
                        blockers.Add($"{s}: NCR Open");
                }
                if (blockers.Count > 0)
                    blocked.Add(item with { Blockers = blockers });
            }
        }

        return Ok(new QualityQueuesDto(awaiting, failedOpen, blocked));
    }

    [HttpGet("workorders/{workOrderId:guid}/quality")]
    public async Task<ActionResult<WorkOrderQualitySummaryDto>> GetQualitySummary(Guid workOrderId, CancellationToken ct)
    {
        var wo = await _db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workOrderId, ct);
        if (wo == null) return NotFound();

        await EnsureGatesExistAsync(workOrderId, ct);
        await _db.SaveChangesAsync(ct);

        var gates = await _db.WorkOrderQualityGates.AsNoTracking()
            .Where(g => g.WorkOrderId == workOrderId)
            .OrderBy(g => g.Stage)
            .ToListAsync(ct);

        var openNcrs = await _db.Ncrs.AsNoTracking()
            .Where(n => n.WorkOrderId == workOrderId && n.Status == NcrStatus.Open)
            .ToListAsync(ct);
        var openByStage = openNcrs.ToDictionary(n => n.Stage, n => n);

        // Last check per stage (by CreatedAt)
        var lastChecks = await _db.WorkOrderQualityChecks.AsNoTracking()
            .Where(c => c.WorkOrderId == workOrderId)
            .GroupBy(c => c.Stage)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(ct);
        var lastByStage = lastChecks.ToDictionary(x => x.Stage, x => x);

        var dto = gates.Select(g =>
        {
            lastByStage.TryGetValue(g.Stage, out var last);
            openByStage.TryGetValue(g.Stage, out var ncr);
            return new QualityGateDto(
                Stage: g.Stage.ToString(),
                GateStatus: g.GateStatus.ToString(),
                PassedAt: g.PassedAt,
                PassedBy: g.PassedBy,
                FailedAt: g.FailedAt,
                FailedBy: g.FailedBy,
                LastResult: last?.Result.ToString(),
                LastCheckedAt: last?.CreatedAt,
                LastCheckedBy: last?.CreatedBy,
                OpenNcrId: ncr?.Id,
                OpenNcrStatus: ncr?.Status.ToString());
        }).ToList();

        return Ok(new WorkOrderQualitySummaryDto(wo.Id, wo.OrderNumber, wo.Stage.ToString(), dto));
    }

    [HttpPost("workorders/{workOrderId:guid}/quality/checks")]
    public async Task<ActionResult<QualityCheckRecordedDto>> RecordCheck(Guid workOrderId, [FromBody] RecordQualityCheckRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Stage)) return BadRequest("Stage is required.");
        if (string.IsNullOrWhiteSpace(req.Result)) return BadRequest("Result is required.");
        if (!TryParseStage(req.Stage, out var stage)) return BadRequest("Invalid stage.");
        if (!Enum.TryParse<QualityCheckResult>(req.Result, ignoreCase: true, out var result)) return BadRequest("Invalid result.");

        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId, ct);
        if (wo == null) return NotFound();

        await EnsureGatesExistAsync(workOrderId, ct);
        await _db.SaveChangesAsync(ct);

        var gate = await _db.WorkOrderQualityGates
            .FirstAsync(g => g.WorkOrderId == workOrderId && g.Stage == stage, ct);

        var createdBy = string.IsNullOrWhiteSpace(req.CreatedBy) ? "System" : req.CreatedBy.Trim();

        // Append-only QC log
        var check = new WorkOrderQualityCheck
        {
            WorkOrderId = workOrderId,
            WorkOrderQualityGateId = gate.Id,
            Stage = stage,
            Result = result,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOrderQualityChecks.Add(check);

        // Gate state + NCR
        if (result == QualityCheckResult.Fail)
        {
            gate.GateStatus = QualityGateStatus.Failed;
            gate.FailedAt = DateTime.UtcNow;
            gate.FailedBy = createdBy;
            gate.PassedAt = null;
            gate.PassedBy = null;

            var existingOpen = await _db.Ncrs
                .FirstOrDefaultAsync(n => n.WorkOrderId == workOrderId && n.Stage == stage && n.Status == NcrStatus.Open, ct);

            if (existingOpen == null)
            {
                var desc = req.NcrDescription;
                if (string.IsNullOrWhiteSpace(desc)) desc = check.Notes;
                if (string.IsNullOrWhiteSpace(desc)) desc = $"QC failed at stage {stage}.";

                _db.Ncrs.Add(new Ncr
                {
                    WorkOrderId = workOrderId,
                    Stage = stage,
                    Description = desc.Trim(),
                    Status = NcrStatus.Open,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Only pass the gate if there is NO open NCR for this stage.
            var openNcr = await _db.Ncrs.AsNoTracking()
                .AnyAsync(n => n.WorkOrderId == workOrderId && n.Stage == stage && n.Status == NcrStatus.Open, ct);
            if (!openNcr)
            {
                gate.GateStatus = QualityGateStatus.Passed;
                gate.PassedAt = DateTime.UtcNow;
                gate.PassedBy = createdBy;
                gate.FailedAt = null;
                gate.FailedBy = null;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new QualityCheckRecordedDto(check.Id, gate.Id, gate.GateStatus.ToString()));
    }

    [HttpGet("workorders/{workOrderId:guid}/ncrs")]
    public async Task<ActionResult<IReadOnlyList<NcrListDto>>> ListNcrs(Guid workOrderId, [FromQuery] string? status, CancellationToken ct)
    {
        var q = _db.Ncrs.AsNoTracking().Where(n => n.WorkOrderId == workOrderId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<NcrStatus>(status, true, out var parsed))
                return BadRequest("Invalid status.");
            q = q.Where(n => n.Status == parsed);
        }

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NcrListDto(n.Id, n.Stage.ToString(), n.Status.ToString(), n.Description, n.CreatedAt, n.CreatedBy, n.ClosedAt, n.ClosedBy, n.Disposition != null ? n.Disposition.ToString() : null))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("workorders/{workOrderId:guid}/ncrs/{ncrId:guid}/close")]
    public async Task<ActionResult> CloseNcr(Guid workOrderId, Guid ncrId, [FromBody] CloseNcrRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Disposition)) return BadRequest("Disposition is required.");
        if (!Enum.TryParse<NcrDisposition>(req.Disposition, true, out var disp)) return BadRequest("Invalid disposition.");

        var ncr = await _db.Ncrs.FirstOrDefaultAsync(n => n.Id == ncrId && n.WorkOrderId == workOrderId, ct);
        if (ncr == null) return NotFound();

        if (ncr.Status == NcrStatus.Closed) return Ok(new { alreadyClosed = true });

        ncr.Status = NcrStatus.Closed;
        ncr.Disposition = disp;
        ncr.ClosureNotes = string.IsNullOrWhiteSpace(req.ClosureNotes) ? null : req.ClosureNotes.Trim();
        ncr.ClosedAt = DateTime.UtcNow;
        ncr.ClosedBy = string.IsNullOrWhiteSpace(req.ClosedBy) ? "System" : req.ClosedBy.Trim();

        await _db.SaveChangesAsync(ct);
        return Ok(new { closed = true, ncr.Id, Disposition = ncr.Disposition.ToString() });
    }

    // "Delete" = soft delete (IsDeleted) so we never lose the row in DB.
    [HttpDelete("workorders/{workOrderId:guid}/quality/checks/{checkId:guid}")]
    public async Task<ActionResult> DeleteQualityCheck(Guid workOrderId, Guid checkId, CancellationToken ct)
    {
        var check = await _db.WorkOrderQualityChecks.FirstOrDefaultAsync(x => x.Id == checkId && x.WorkOrderId == workOrderId, ct);
        if (check == null) return NotFound();

        _db.WorkOrderQualityChecks.Remove(check);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("workorders/{workOrderId:guid}/ncrs/{ncrId:guid}")]
    public async Task<ActionResult> DeleteNcr(Guid workOrderId, Guid ncrId, CancellationToken ct)
    {
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == ncrId && x.WorkOrderId == workOrderId, ct);
        if (ncr == null) return NotFound();

        _db.Ncrs.Remove(ncr);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Deletes QC module data for a WO (gates/checks/NCRs). Useful for demo/test resets.
    [HttpDelete("workorders/{workOrderId:guid}/quality")]
    public async Task<ActionResult> DeleteWorkOrderQuality(Guid workOrderId, CancellationToken ct)
    {
        var hasWo = await _db.WorkOrders.AsNoTracking().AnyAsync(w => w.Id == workOrderId, ct);
        if (!hasWo) return NotFound();

        var checks = await _db.WorkOrderQualityChecks.Where(x => x.WorkOrderId == workOrderId).ToListAsync(ct);
        var ncrs = await _db.Ncrs.Where(x => x.WorkOrderId == workOrderId).ToListAsync(ct);
        var gates = await _db.WorkOrderQualityGates.Where(x => x.WorkOrderId == workOrderId).ToListAsync(ct);

        _db.WorkOrderQualityChecks.RemoveRange(checks);
        _db.Ncrs.RemoveRange(ncrs);
        _db.WorkOrderQualityGates.RemoveRange(gates);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record WorkOrderQualitySummaryDto(Guid WorkOrderId, string WorkOrderNumber, string CurrentStage, IReadOnlyList<QualityGateDto> Gates);

public record QualityGateDto(
    string Stage,
    string GateStatus,
    DateTime? PassedAt,
    string? PassedBy,
    DateTime? FailedAt,
    string? FailedBy,
    string? LastResult,
    DateTime? LastCheckedAt,
    string? LastCheckedBy,
    Guid? OpenNcrId,
    string? OpenNcrStatus);

public record RecordQualityCheckRequest(string Stage, string Result, string? Notes, string? CreatedBy, string? NcrDescription);
public record QualityCheckRecordedDto(Guid QualityCheckId, Guid GateId, string GateStatus);

public record NcrListDto(Guid Id, string Stage, string Status, string Description, DateTime CreatedAt, string CreatedBy, DateTime? ClosedAt, string? ClosedBy, string? Disposition);
public record CloseNcrRequest(string Disposition, string? ClosureNotes, string? ClosedBy);

public record QualityQueuesDto(
    IReadOnlyList<QualityQueueItemDto> AwaitingQc,
    IReadOnlyList<QualityQueueItemDto> FailedQcOpenNcr,
    IReadOnlyList<QualityQueueItemDto> BlockedWorkOrders);

public record QualityQueueItemDto(
    Guid WorkOrderId,
    string WorkOrderNumber,
    string CurrentStage,
    string CurrentGateStatus,
    Guid? OpenNcrId,
    DateTime? LastCheckedAt,
    string? LastResult,
    IReadOnlyList<string>? Blockers);

internal record LastCheckInfo(DateTime CreatedAt, QualityCheckResult Result);


