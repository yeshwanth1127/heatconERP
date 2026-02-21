using DotNetEnv;
using HeatconERP.Application.Abstractions;
using HeatconERP.Application.Services.Inventory;
using HeatconERP.Application.Services.Procurement;
using HeatconERP.Application.Services.Srs;
using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Entities.Inventory;
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

// Application services (Inventory & Procurement)
builder.Services.AddScoped<IHeatconDbContext>(sp => sp.GetRequiredService<HeatconDbContext>());
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<ISrsService, SrsService>();

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
        (Username: "qualitycheck", Password: "qc123", Role: UserRole.QualityCheck),
        (Username: "storemanager", Password: "store123", Role: UserRole.DispatchStoreManager)
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
            var today = DateTime.UtcNow.Date;
            var pos = new[] { ("PO-2501", "PO-9943-Z", "Mark Thompson", 14500m), ("PO-2502", "PO-9944-A", "Jane Doe", 8200m) };
            foreach (var (orderNum, custPo, name, val) in pos)
                db.PurchaseOrders.Add(new PurchaseOrder { Id = Guid.NewGuid(), OrderNumber = orderNum, CustomerPONumber = custPo, PODate = today, Status = "Active", Value = val, CreatedAt = DateTime.UtcNow, CreatedByUserName = name });
        } } catch { /* PurchaseOrders seed skipped */ }
    // WorkOrders: do not seed - should come from "Convert to Work Order"
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

    // Inventory & Procurement: seed material hierarchy (only if empty)
    try
    {
        if (!await db.MaterialCategories.AnyAsync())
        {
            var catSs = new MaterialCategory { Id = Guid.NewGuid(), Name = "Stainless Steel", Description = "Sheet/rod/plate variants" };
            var catAl = new MaterialCategory { Id = Guid.NewGuid(), Name = "Aluminium", Description = "Lightweight structural variants" };
            var catTi = new MaterialCategory { Id = Guid.NewGuid(), Name = "Titanium", Description = "Aerospace-grade variants" };

            db.MaterialCategories.AddRange(catSs, catAl, catTi);

            db.MaterialVariants.AddRange(
                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catSs.Id, Grade = "SS304", Size = "10mm", Unit = "Kg", SKU = "SS304-10MM-KG", MinimumStockLevel = 50 },
                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catSs.Id, Grade = "SS316", Size = "12mm", Unit = "Kg", SKU = "SS316-12MM-KG", MinimumStockLevel = 30 },
                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catSs.Id, Grade = "SS304", Size = "2mm Sheet", Unit = "SqM", SKU = "SS304-2MM-SQM", MinimumStockLevel = 20 },

                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catAl.Id, Grade = "AL6061", Size = "8mm", Unit = "Kg", SKU = "AL6061-8MM-KG", MinimumStockLevel = 40 },
                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catAl.Id, Grade = "AL7075", Size = "6mm", Unit = "Kg", SKU = "AL7075-6MM-KG", MinimumStockLevel = 25 },

                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catTi.Id, Grade = "TI6AL4V", Size = "5mm", Unit = "Kg", SKU = "TI6AL4V-5MM-KG", MinimumStockLevel = 15 },
                new MaterialVariant { Id = Guid.NewGuid(), MaterialCategoryId = catTi.Id, Grade = "TI6AL4V", Size = "3mm Sheet", Unit = "SqM", SKU = "TI6AL4V-3MM-SQM", MinimumStockLevel = 10 }
            );
        }
    }
    catch { /* Inventory seed skipped */ }
    try { await db.SaveChangesAsync(); } catch { /* SaveChanges skipped */ }
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();
