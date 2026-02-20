using DotNetEnv;
using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Enums;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

// Load .env from project root (searches current dir and parents)
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Add DbContext with PostgreSQL - connection string from .env (DATABASE_URL)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:DefaultConnection not found.");
builder.Services.AddDbContext<HeatconDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Apply pending migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HeatconDbContext>();
    await db.Database.MigrateAsync();

    // Seed users (excluded from "remove hardcoded" - keep as-is)
    var seedUsers = new[]
    {
        (Username: "admin", Password: "admin123", Role: UserRole.MD),
        (Username: "crmmanager", Password: "crm123", Role: UserRole.CRMManager),
        (Username: "crm_manager", Password: "crm123", Role: UserRole.CRMManager),
        (Username: "crmstaff", Password: "crm123", Role: UserRole.CRMStaff),
        (Username: "prodmanager", Password: "prod123", Role: UserRole.ProductionManager),
        (Username: "prodstaff", Password: "prod123", Role: UserRole.ProductionStaff),
        (Username: "qualitycheck", Password: "qc123", Role: UserRole.QualityCheck)
    };
    foreach (var (username, password, role) in seedUsers)
    {
        if (!await db.Users.AnyAsync(u => u.Username == username))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            });
        }
    }

    // Seed dashboard data (only if tables are empty) - each block in try to avoid one failure breaking others
    try { if (!await db.Enquiries.AnyAsync())
        {
            var sampleEnquiries = new[]
            {
                ("ENQ-001", "BOEING", "R&D Thermal Sensors - Next Gen Aircraft", "Under Review"),
                ("ENQ-002", "Airbus", "A350 XWB - Cabin Temperature Probes", "Feasible"),
                ("ENQ-003", "Lockheed Martin", "F-35 Engine Bay Thermal Monitoring", "Under Review"),
                ("ENQ-004", "Rolls-Royce", "Trent Engine Sensor Suite", "New"),
                ("ENQ-005", "GE Aviation", "LEAP Engine Thermal Mapping", "Feasible"),
                ("ENQ-006", "Northrop Grumman", "B-21 Thermal Sensor Integration", "Under Review"),
                ("ENQ-007", "Raytheon", "Defense Thermal Imaging Components", "Not Feasible"),
                ("ENQ-008", "Safran", "Helicopter Engine Temperature Probes", "Converted"),
                ("ENQ-009", "Honeywell", "Auxiliary Power Unit Sensors", "Feasible"),
                ("ENQ-010", "Collins Aerospace", "Cabin Climate Control Sensors", "Under Review")
            };
            var today = DateTime.UtcNow;
            for (var i = 0; i < sampleEnquiries.Length; i++)
            {
                var (num, company, desc, status) = sampleEnquiries[i];
                db.Enquiries.Add(new Enquiry
                {
                    Id = Guid.NewGuid(),
                    EnquiryNumber = num,
                    DateReceived = today.AddDays(-i - 1),
                    Source = "Email",
                    Status = status,
                    CompanyName = company,
                    ProductDescription = desc,
                    Quantity = 100 + i * 50,
                    IsAerospace = true,
                    Priority = i % 3 == 0 ? "High" : i % 3 == 1 ? "Medium" : "Low",
                    FeasibilityStatus = status == "Feasible" ? "Feasible" : status == "Not Feasible" ? "Not Feasible" : "Pending",
                    CreatedAt = today.AddDays(-i - 1),
                    IsDeleted = false
                });
            }
        } } catch { /* Enquiries seed skipped */ }
    try { /* Quotations: no seed - created only from enquiries or manually */ } catch { }
    try { if (!await db.PurchaseOrders.AnyAsync())
        {
            var pos = new[] { ("PO-9943-Z", "Mark Thompson", 14500m), ("PO-9944-A", "Jane Doe", 8200m) };
            foreach (var (num, name, val) in pos)
                db.PurchaseOrders.Add(new PurchaseOrder { Id = Guid.NewGuid(), OrderNumber = num, Status = "Active", Value = val, CreatedAt = DateTime.UtcNow, CreatedByUserName = name });
        } } catch { /* PurchaseOrders seed skipped */ }
    try { if (!await db.WorkOrders.AnyAsync())
        {
            var stages = new[] { WorkOrderStage.Planning, WorkOrderStage.Material, WorkOrderStage.Assembly, WorkOrderStage.Testing, WorkOrderStage.QC, WorkOrderStage.Packing };
            var counts = new[] { 42, 31, 124, 98, 22, 11 };
            for (var s = 0; s < stages.Length; s++)
                for (var i = 0; i < counts[s]; i++)
                    db.WorkOrders.Add(new WorkOrder { Id = Guid.NewGuid(), OrderNumber = $"WO-{4400 + s}-{i:D2}", Stage = stages[s], Status = "Active", CreatedAt = DateTime.UtcNow });
        } } catch { /* WorkOrders seed skipped */ }
    try { if (!await db.ActivityLogs.AnyAsync())
        {
            var logs = new[]
            {
                ("14:40:12", "SYSTEM", "Inventory sync complete. 1,422 items indexed."),
                ("14:38:55", "AUDIT", "User S.Chen approved PO-992-A1"),
                ("14:35:10", "CRITICAL", "QC Failure on Work Order #WO-4402. Batch rejected."),
                ("14:30:22", "USER", "New Enquiry added: BOEING - R&D THERMAL SENSORS"),
                ("14:15:00", "WARN", "Low Stock Alert: M3-Screws-Titani (< 200 units)"),
                ("14:02:41", "AUDIT", "Shift Handover: Shift-B logged in.")
            };
            var today = DateTime.UtcNow.Date;
            foreach (var (time, tag, msg) in logs)
            {
                var parts = time.Split(':');
                var occurred = today.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1])).AddSeconds(int.Parse(parts[2]));
                db.ActivityLogs.Add(new ActivityLog { Id = Guid.NewGuid(), OccurredAt = occurred, Tag = tag, Message = msg });
            }
        } } catch { /* ActivityLogs seed skipped */ }
    try { if (!await db.PendingApprovals.AnyAsync())
        {
            db.PendingApprovals.AddRange(
                new PendingApproval { Id = Guid.NewGuid(), ReferenceId = "PO-9943-Z", Module = "Procurement", Originator = "Mark Thompson", CreatedAt = DateTime.UtcNow.AddHours(-2), Value = "$14,500.00" },
                new PendingApproval { Id = Guid.NewGuid(), ReferenceId = "WO-4401-01", Module = "Production", Originator = "Systems Auto", CreatedAt = DateTime.UtcNow.AddHours(-1), Value = null },
                new PendingApproval { Id = Guid.NewGuid(), ReferenceId = "QT-8890", Module = "Quotations", Originator = "Sara Jenkins", CreatedAt = DateTime.UtcNow, Value = "$882,000.00" });
        } } catch { /* PendingApprovals seed skipped */ }
    try { if (!await db.QualityInspections.AnyAsync())
        {
            var workOrders = await db.WorkOrders.Take(10).ToListAsync();
            foreach (var wo in workOrders.Take(5))
                db.QualityInspections.Add(new QualityInspection { Id = Guid.NewGuid(), WorkOrderId = wo.Id, WorkOrderNumber = wo.OrderNumber, Result = "Pass", Notes = "Initial inspection OK", InspectedAt = DateTime.UtcNow.AddDays(-1), InspectedBy = "J.Smith" });
            foreach (var wo in workOrders.Skip(5).Take(3))
                db.QualityInspections.Add(new QualityInspection { Id = Guid.NewGuid(), WorkOrderId = wo.Id, WorkOrderNumber = wo.OrderNumber, Result = "Fail", Notes = "Dimensional tolerance exceeded", InspectedAt = DateTime.UtcNow.AddHours(-2), InspectedBy = "M.Jones" });
        } } catch { /* QualityInspections seed skipped */ }
    try { await db.SaveChangesAsync(); } catch { /* SaveChanges skipped */ }
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
