# HeatconERP - Apply EF Core migrations to database
# Run from project root: .\scripts\apply-migrations.ps1
# IMPORTANT: Stop the API first (Ctrl+C on watch-api) - migrations fail if API is running

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "=== Applying EF Migrations ===" -ForegroundColor Cyan
Write-Host "Ensure API is stopped. Press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "`nRunning: dotnet ef database update ..." -ForegroundColor Gray
dotnet ef database update --project src/HeatconERP.Infrastructure --startup-project src/HeatconERP.API

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nMigrations applied successfully." -ForegroundColor Green
    Write-Host "Tables created: Enquiries, Quotations, PurchaseOrders, WorkOrders, ActivityLogs, PendingApprovals, QualityInspections" -ForegroundColor Gray
    Write-Host "Restart the API to seed dashboard data." -ForegroundColor Gray
} else {
    Write-Host "`nMigration failed. Ensure API is stopped and DATABASE_URL is correct in .env" -ForegroundColor Red
    exit 1
}
