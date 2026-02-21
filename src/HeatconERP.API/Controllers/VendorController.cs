using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{
    private readonly HeatconDbContext _db;
    public VendorController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Vendors.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VendorDto(v.Id, v.Name, v.GSTNumber, v.ContactDetails, v.IsApprovedVendor))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] CreateVendorRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        var entity = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            GSTNumber = string.IsNullOrWhiteSpace(req.GSTNumber) ? null : req.GSTNumber.Trim(),
            ContactDetails = string.IsNullOrWhiteSpace(req.ContactDetails) ? null : req.ContactDetails.Trim(),
            IsApprovedVendor = req.IsApprovedVendor
        };
        _db.Vendors.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { }, new VendorDto(entity.Id, entity.Name, entity.GSTNumber, entity.ContactDetails, entity.IsApprovedVendor));
    }
}

public record VendorDto(Guid Id, string Name, string? GSTNumber, string? ContactDetails, bool IsApprovedVendor);
public record CreateVendorRequest(string Name, string? GSTNumber, string? ContactDetails, bool IsApprovedVendor);


