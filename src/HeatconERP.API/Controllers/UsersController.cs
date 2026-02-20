using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public UsersController(HeatconDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, Role = u.Role.ToString() })
            .ToListAsync(ct);
        return Ok(users);
    }
}
