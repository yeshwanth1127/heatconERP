# Apply Enquiry V1 schema - adds EnquiryNumber, CompanyName, etc.
# Run from project root: .\scripts\apply-enquiry-schema.ps1
# No need to stop the API - this runs against the DB directly

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

# Parse DATABASE_URL from .env
$envFile = Join-Path $ProjectRoot ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "ERROR: .env not found" -ForegroundColor Red
    exit 1
}
$envContent = Get-Content $envFile -Raw
if ($envContent -match 'Password=([^;\s]+)') {
    $env:PGPASSWORD = $Matches[1]
}

Write-Host "Applying Enquiry V1 schema..." -ForegroundColor Cyan
$result = psql -U postgres -d heatconerp -f "scripts/apply-enquiry-schema.sql" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Schema applied successfully." -ForegroundColor Green
    Write-Host "Restart the API (or it will pick up on next request)." -ForegroundColor Gray
} else {
    Write-Host $result -ForegroundColor Red
    exit 1
}
