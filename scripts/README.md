# Database Scripts

## Create database (if it doesn't exist)

**Using psql directly:**
```bash
psql -U postgres -f scripts/create_db.sql
```

**Using PowerShell:**
```powershell
.\scripts\create_db.ps1
```

**One-liner (any shell):**
```bash
psql -U postgres -c "SELECT 'CREATE DATABASE heatconerp OWNER postgres' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'heatconerp')\gexec"
```

Set `PGPASSWORD` environment variable to avoid password prompt:
```powershell
$env:PGPASSWORD = "your_password"
```

## EF migrations

Uses `DATABASE_URL` from `.env`. Run from project root:
```powershell
dotnet ef database update --project src/HeatconERP.Infrastructure --startup-project src/HeatconERP.API
```
