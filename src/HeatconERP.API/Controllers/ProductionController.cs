using HeatconERP.Domain.Enums;
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
            wo.Stage = stage;
            await _db.SaveChangesAsync(ct);
            return Ok(new { wo.OrderNumber, Stage = wo.Stage.ToString() });
        }
        return BadRequest(new { error = "Invalid stage" });
    }
}

public record WorkOrderDto(Guid Id, string OrderNumber, string Stage, string Status, DateTime CreatedAt);
public record UpdateStageRequest(string Stage);
