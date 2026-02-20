using HeatconERP.Domain.Entities;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnquiriesController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public EnquiriesController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EnquiryListDto>>> Get(
        [FromQuery] string? status,
        [FromQuery] bool? isAerospace,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var query = _db.Enquiries.AsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);
        if (isAerospace.HasValue)
            query = query.Where(e => e.IsAerospace == isAerospace.Value);
        if (dateFrom.HasValue)
            query = query.Where(e => e.DateReceived >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(e => e.DateReceived <= dateTo.Value.Date.AddDays(1));

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new EnquiryListDto(e.Id, e.EnquiryNumber, e.CompanyName, e.ProductDescription ?? "", e.Status, e.DateReceived, e.Priority, e.Source, e.AssignedTo, e.FeasibilityStatus, e.IsAerospace))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnquiryDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _db.Enquiries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (e == null) return NotFound();

        return Ok(new EnquiryDetailDto(
            e.Id, e.EnquiryNumber, e.DateReceived, e.Source, e.AssignedTo, e.Status,
            e.CompanyName, e.ContactPerson, e.Email, e.Phone, e.Gst, e.Address,
            e.ProductDescription, e.Quantity, e.ExpectedDeliveryDate, e.AttachmentPath,
            e.IsAerospace, e.Priority, e.FeasibilityStatus, e.FeasibilityNotes,
            e.CreatedBy, e.CreatedAt, e.UpdatedAt));
    }

    [HttpPost("seed")]
    public async Task<ActionResult<object>> Seed(CancellationToken ct)
    {
        var samples = new[]
        {
            ("ENQ-001", "BOEING", "R&D Thermal Sensors - Next Gen Aircraft", "Under Review", "Email", "Sara Jenkins", "John Smith", "john@boeing.com", "+1-206-555-0100", true, "High"),
            ("ENQ-002", "Airbus", "A350 XWB - Cabin Temperature Probes", "Feasible", "Email", "Mike Chen", "Marie Dupont", "marie@airbus.com", "+33-1-555-0200", true, "High"),
            ("ENQ-003", "Lockheed Martin", "F-35 Engine Bay Thermal Monitoring", "Under Review", "Phone", null, "Robert Lee", "r.lee@lmco.com", "+1-301-555-0300", true, "High"),
            ("ENQ-004", "Rolls-Royce", "Trent Engine Sensor Suite", "Pending", "IndiaMart", "Sara Jenkins", "James Wilson", "j.wilson@rolls-royce.com", "+44-555-0400", true, "Medium"),
            ("ENQ-005", "GE Aviation", "LEAP Engine Thermal Mapping", "Feasible", "Email", "Mike Chen", "Anna Brown", "a.brown@ge.com", "+1-513-555-0500", true, "High"),
            ("ENQ-006", "Northrop Grumman", "B-21 Thermal Sensor Integration", "Under Review", "Manual", null, "David Clark", "d.clark@northrop.com", "+1-703-555-0600", true, "High"),
            ("ENQ-007", "Raytheon", "Defense Thermal Imaging Components", "Not Feasible", "WhatsApp", "Sara Jenkins", "Susan Davis", "s.davis@raytheon.com", "+1-781-555-0700", false, "Medium"),
            ("ENQ-008", "Safran", "Helicopter Engine Temperature Probes", "Converted", "Email", "Mike Chen", "Pierre Martin", "p.martin@safran.com", "+33-555-0800", true, "Medium"),
            ("ENQ-009", "Honeywell", "Auxiliary Power Unit Sensors", "Feasible", "Email", "Sara Jenkins", "Lisa Anderson", "l.anderson@honeywell.com", "+1-602-555-0900", true, "High"),
            ("ENQ-010", "Collins Aerospace", "Cabin Climate Control Sensors", "Under Review", "IndiaMart", null, "Kevin Taylor", "k.taylor@collins.com", "+1-860-555-1000", true, "Medium")
        };

        await _db.Enquiries.Where(e => !e.IsDeleted).ExecuteDeleteAsync(ct);
        var now = DateTime.UtcNow;
        for (var i = 0; i < samples.Length; i++)
        {
            var (num, company, desc, status, source, assigned, contact, email, phone, aero, priority) = samples[i];
            _db.Enquiries.Add(new Enquiry
            {
                Id = Guid.NewGuid(),
                EnquiryNumber = num,
                DateReceived = now.AddDays(-i - 1),
                Source = source,
                AssignedTo = assigned,
                Status = status,
                CompanyName = company,
                ContactPerson = contact,
                Email = email,
                Phone = phone,
                ProductDescription = desc,
                Quantity = 100 + i * 50,
                IsAerospace = aero,
                Priority = priority,
                FeasibilityStatus = status == "Feasible" ? "Feasible" : status == "Not Feasible" ? "Not Feasible" : "Pending",
                CreatedBy = "System",
                CreatedAt = now.AddDays(-i - 1),
                IsDeleted = false
            });
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { seeded = samples.Length, message = "Sample enquiries seeded." });
    }
}

public record EnquiryListDto(Guid Id, string EnquiryNumber, string CompanyName, string ProductDescription, string Status, DateTime DateReceived, string Priority, string Source, string? AssignedTo, string FeasibilityStatus, bool IsAerospace);
public record EnquiryDetailDto(
    Guid Id, string EnquiryNumber, DateTime DateReceived, string Source, string? AssignedTo, string Status,
    string CompanyName, string? ContactPerson, string? Email, string? Phone, string? Gst, string? Address,
    string? ProductDescription, int? Quantity, DateTime? ExpectedDeliveryDate, string? AttachmentPath,
    bool IsAerospace, string Priority, string FeasibilityStatus, string? FeasibilityNotes,
    string? CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt);
