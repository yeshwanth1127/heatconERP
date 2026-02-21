using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialController : ControllerBase
{
    private readonly HeatconDbContext _db;
    public MaterialController(HeatconDbContext db) => _db = db;

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<MaterialCategoryDto>>> GetCategories(CancellationToken ct)
    {
        var items = await _db.MaterialCategories.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new MaterialCategoryDto(x.Id, x.Name, x.Description))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<MaterialCategoryDto>> CreateCategory([FromBody] CreateMaterialCategoryRequest req, CancellationToken ct)
    {
        try
        {
            // TODO: role auth placeholder
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");

            var entity = new MaterialCategory { Id = Guid.NewGuid(), Name = req.Name.Trim(), Description = req.Description?.Trim() };
            _db.MaterialCategories.Add(entity);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetCategories), new { }, new MaterialCategoryDto(entity.Id, entity.Name, entity.Description));
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            return StatusCode(500,
                "Inventory schema is not applied yet (tables missing). Run migrations (`scripts/apply-all-migrations.ps1`) and restart the API.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "42P01")
        {
            return StatusCode(500,
                "Inventory schema is not applied yet (tables missing). Run migrations (`scripts/apply-all-migrations.ps1`) and restart the API.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Unique constraint
            return Conflict("Duplicate value. Ensure material category/variant keys are unique (SKU is unique).");
        }
    }

    [HttpGet("variants")]
    public async Task<ActionResult<IReadOnlyList<MaterialVariantDto>>> GetVariants([FromQuery] Guid? categoryId, CancellationToken ct)
    {
        var q = _db.MaterialVariants.AsNoTracking().AsQueryable();
        if (categoryId.HasValue) q = q.Where(v => v.MaterialCategoryId == categoryId.Value);

        var items = await q
            .OrderBy(v => v.SKU)
            .Select(v => new MaterialVariantDto(v.Id, v.MaterialCategoryId, v.Grade, v.Size, v.Unit, v.SKU, v.MinimumStockLevel))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("variants")]
    public async Task<ActionResult<MaterialVariantDto>> CreateVariant([FromBody] CreateMaterialVariantRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.SKU)) return BadRequest("SKU is required.");
            if (string.IsNullOrWhiteSpace(req.Unit)) return BadRequest("Unit is required.");

            var exists = await _db.MaterialVariants.AnyAsync(v => v.SKU == req.SKU.Trim(), ct);
            if (exists) return Conflict("SKU must be unique.");

            var entity = new MaterialVariant
            {
                Id = Guid.NewGuid(),
                MaterialCategoryId = req.MaterialCategoryId,
                Grade = req.Grade?.Trim() ?? "",
                Size = req.Size?.Trim() ?? "",
                Unit = req.Unit.Trim(),
                SKU = req.SKU.Trim(),
                MinimumStockLevel = req.MinimumStockLevel
            };
            _db.MaterialVariants.Add(entity);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetVariants), new { }, new MaterialVariantDto(entity.Id, entity.MaterialCategoryId, entity.Grade, entity.Size, entity.Unit, entity.SKU, entity.MinimumStockLevel));
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            return StatusCode(500,
                "Inventory schema is not applied yet (tables missing). Run migrations (`scripts/apply-all-migrations.ps1`) and restart the API.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "42P01")
        {
            return StatusCode(500,
                "Inventory schema is not applied yet (tables missing). Run migrations (`scripts/apply-all-migrations.ps1`) and restart the API.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict("SKU must be unique.");
        }
    }
}

public record MaterialCategoryDto(Guid Id, string Name, string? Description);
public record CreateMaterialCategoryRequest(string Name, string? Description);

public record MaterialVariantDto(Guid Id, Guid MaterialCategoryId, string Grade, string Size, string Unit, string SKU, decimal MinimumStockLevel);
public record CreateMaterialVariantRequest(Guid MaterialCategoryId, string? Grade, string? Size, string Unit, string SKU, decimal MinimumStockLevel);


