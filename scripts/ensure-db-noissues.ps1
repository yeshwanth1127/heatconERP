# One-shot, non-interactive DB schema fixer
# After it succeeds, the API should not hit "missing table/column" DB errors.
#
# Usage:
#   cd heatconERP
#   $env:PGPASSWORD = "your_password"   # if required
#   .\scripts\ensure-db-noissues.ps1
#
# Optional env vars:
#   HEATCONERP_DB_NAME (default: heatconerp)
#   HEATCONERP_DB_USER (default: postgres)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

$dbName = $env:HEATCONERP_DB_NAME
if ([string]::IsNullOrWhiteSpace($dbName)) { $dbName = "heatconerp" }

$dbUser = $env:HEATCONERP_DB_USER
if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = "postgres" }

$sqlFile = Join-Path $ProjectRoot "scripts\\ensure-db-noissues.sql"

Write-Host "=== Ensuring DB schema (idempotent) ===" -ForegroundColor Cyan
Write-Host "Database: $dbName" -ForegroundColor Gray
Write-Host "User:     $dbUser" -ForegroundColor Gray
Write-Host "SQL:      $sqlFile" -ForegroundColor Gray

& psql -U $dbUser -d $dbName -v ON_ERROR_STOP=1 -f $sqlFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nDB ensure failed. Fix the reported error and re-run." -ForegroundColor Red
    exit 1
}

Write-Host "`nDB schema ensured successfully." -ForegroundColor Green
Write-Host "If API/Web are running, restart them now." -ForegroundColor Yellow


