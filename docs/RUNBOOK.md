# HeatconERP - Runbook

## Prerequisites (one-time setup)

1. **.NET 10 SDK** – `dotnet --version` should show 10.x
2. **PostgreSQL** – Running locally on port 5432
3. **.env file** – At project root with `DATABASE_URL`:
   ```
   DATABASE_URL=Host=localhost;Port=5432;Database=heatconerp;Username=postgres;Password=your_password
   ```

---

## Steps Before First Build (one-time)

### 1. Create database (if it doesn't exist)
```powershell
psql -U postgres -f scripts/create_db.sql
```

### 2. Apply EF migrations (stop API first if running)
**Stop the API** (Ctrl+C on watch-api) before running migrations.

```powershell
.\scripts\apply-migrations.ps1
```
Or manually:
```powershell
dotnet ef database update --project src/HeatconERP.Infrastructure --startup-project src/HeatconERP.API
```

This creates: Enquiries, Quotations, PurchaseOrders, WorkOrders, ActivityLogs, PendingApprovals, QualityInspections.  
**Seed data is applied on API startup** after the tables exist.  
If Enquiries is empty, use the **"Seed Sample Data"** button on the Enquiries page to load sample data (BOEING, Airbus, etc.).

**If you get "column ReferenceNumber does not exist" or "QualityInspections does not exist"**: Run the fix script (no need to stop API):
```powershell
psql -U postgres -d heatconerp -f scripts/add-reference-number-column.sql
```
This adds the ReferenceNumber column and creates the QualityInspections table.

**If you get "column EnquiryNumber does not exist"**: Run the Enquiry V1 schema expansion:
```powershell
psql -U postgres -d heatconerp -f scripts/expand-enquiry-schema.sql
```
This adds EnquiryNumber, CompanyName, ProductDescription, and other V1 enquiry fields.

---

## Regular Build & Run

### Build
```powershell
dotnet build HeatconERP.slnx
```

### Run API
```powershell
dotnet run --project src/HeatconERP.API
```

API will be at: http://localhost:5212  
Swagger UI: http://localhost:5212/swagger

### Run Web App (use HTTP to avoid WebSocket issues)
```powershell
dotnet run --project src/HeatconERP.Web --launch-profile http
```

Web app: **http://localhost:5118** (use HTTP, not HTTPS, for Blazor SignalR)

---

## Auto Reload (Development)

Use `dotnet watch` for hot reload when editing code:

| Script | Command | Effect |
|--------|---------|--------|
| API | `.\scripts\watch-api.ps1` | API restarts on .cs file changes |
| Web | `.\scripts\watch-web.ps1` | Builds Tailwind CSS, runs Tailwind watcher + Blazor. Reloads on .razor / .cs changes |

**Web app prerequisites:** Run `npm install` once in `src/HeatconERP.Web` before first use.

Or run directly:
```powershell
dotnet watch run --project src/HeatconERP.API
.\scripts\watch-web.ps1   # Web (includes Tailwind build + watch)
```

Changes are applied without restarting the process manually.

### Sample users (seeded on API startup)

| Username      | Password | Role              |
|---------------|----------|-------------------|
| admin         | admin123 | MD                |
| crmmanager    | crm123   | CRM Manager       |
| crm_manager   | crm123   | CRM Manager       |
| crmstaff      | crm123   | CRM Staff         |
| prodmanager   | prod123  | Production Manager|
| prodstaff     | prod123  | Production Staff  |
| qualitycheck  | qc123    | Quality Check     |

---

## Full Rebuild & Restart (all-in-one)

Use the script:
```powershell
.\scripts\rebuild-and-run.ps1
```

This will:
1. Stop any API already running on port 5212
2. Apply migrations (if any)
3. Rebuild the solution
4. Start the API
