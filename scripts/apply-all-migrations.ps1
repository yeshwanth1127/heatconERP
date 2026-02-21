# Apply all missing migrations via SQL script
# Run from project root: .\scripts\apply-all-migrations.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "=== Applying All Missing Migrations ===" -ForegroundColor Cyan
Write-Host "This will add missing columns/tables directly to the database." -ForegroundColor Yellow
Write-Host "Press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

$dbName = "heatconerp"
$dbUser = "postgres"
$sqlFile = Join-Path $ProjectRoot "scripts\fix-all-migrations.sql"

Write-Host "`nRunning: psql -U $dbUser -d $dbName -f $sqlFile" -ForegroundColor Gray
Write-Host "You may be prompted for PostgreSQL password." -ForegroundColor Yellow

& psql -U $dbUser -d $dbName -f $sqlFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nMigrations applied successfully!" -ForegroundColor Green
    Write-Host "Restart the API to verify." -ForegroundColor Gray
} else {
    Write-Host "`nMigration failed. Check PostgreSQL connection and password." -ForegroundColor Red
    exit 1
}
