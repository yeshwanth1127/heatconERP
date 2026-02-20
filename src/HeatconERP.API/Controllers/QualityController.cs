using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualityController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public QualityController(HeatconDbContext db) => _db = db;

    [HttpGet("inspections")]
    public async Task<ActionResult<IReadOnlyList<QualityInspectionDto>>> GetInspections(
        [FromQuery] string? result,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var query = _db.QualityInspections.AsNoTracking();

        if (!string.IsNullOrEmpty(result))
            query = query.Where(q => q.Result == result);

        var items = await query
            .OrderByDescending(q => q.InspectedAt)
            .Take(limit)
            .Select(q => new QualityInspectionDto(q.Id, q.WorkOrderNumber, q.Result, q.Notes, q.InspectedAt, q.InspectedBy))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("inspections")]
    public async Task<ActionResult<QualityInspectionDto>> CreateInspection([FromBody] CreateInspectionRequest req, CancellationToken ct)
    {
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.OrderNumber == req.WorkOrderNumber, ct);
        if (wo == null) return BadRequest(new { error = "Work order not found" });

        var inspection = new QualityInspection
        {
            Id = Guid.NewGuid(),
            WorkOrderId = wo.Id,
            WorkOrderNumber = wo.OrderNumber,
            Result = req.Result ?? "Pending",
            Notes = req.Notes,
            InspectedAt = DateTime.UtcNow,
            InspectedBy = req.InspectedBy ?? "System"
        };
        _db.QualityInspections.Add(inspection);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetInspections), new QualityInspectionDto(
            inspection.Id, inspection.WorkOrderNumber, inspection.Result, inspection.Notes,
            inspection.InspectedAt, inspection.InspectedBy));
    }
}

public record QualityInspectionDto(Guid Id, string WorkOrderNumber, string Result, string? Notes, DateTime InspectedAt, string InspectedBy);
public record CreateInspectionRequest(string WorkOrderNumber, string? Result, string? Notes, string? InspectedBy);
