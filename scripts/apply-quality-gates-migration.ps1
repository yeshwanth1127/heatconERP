# Apply Phase 1 QC gates + NCR migration (non-interactive)
# Run from repo root: .\scripts\apply-quality-gates-migration.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

$dbName = $env:HEATCONERP_DB_NAME
if ([string]::IsNullOrWhiteSpace($dbName)) { $dbName = "heatconerp" }

$dbUser = $env:HEATCONERP_DB_USER
if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = "postgres" }

$sqlFile = Join-Path $ProjectRoot "scripts\migrate-quality-gates.sql"

Write-Host "=== Applying Phase 1 Quality Gates Migration ===" -ForegroundColor Cyan
Write-Host "Database: $dbName" -ForegroundColor Gray
Write-Host "User:     $dbUser" -ForegroundColor Gray
Write-Host "SQL:      $sqlFile" -ForegroundColor Gray

& psql -U $dbUser -d $dbName -f $sqlFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nMigration failed. Check PostgreSQL connection/credentials." -ForegroundColor Red
    exit 1
}

Write-Host "`nMigration applied successfully." -ForegroundColor Green
Write-Host "Restart the API/Web if they are running." -ForegroundColor Yellow


