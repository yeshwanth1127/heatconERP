# HeatconERP - Run Web App with Hot Reload (Blazor + Tailwind)
# Run from project root: .\scripts\watch-web.ps1
# Builds Tailwind CSS, runs Tailwind watcher in background, then starts Blazor.
# Press Ctrl+C to stop both.

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WebPath = Join-Path $ProjectRoot "src\HeatconERP.Web"
Set-Location $ProjectRoot

Write-Host "=== HeatconERP Web (Watch Mode) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Build Tailwind CSS
Write-Host "Building Tailwind CSS..." -ForegroundColor Gray
Push-Location $WebPath
try {
    npm run tailwind:build
    if ($LASTEXITCODE -ne 0) { throw "Tailwind build failed" }
} catch {
    Pop-Location
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
Pop-Location
Write-Host "Tailwind CSS built.`n" -ForegroundColor Green

# 2. Start Tailwind watcher in background (PowerShell job)
Write-Host "Starting Tailwind watcher (rebuilds on .razor changes)..." -ForegroundColor Gray
$tailwindJob = Start-Job -ScriptBlock {
    Set-Location $using:WebPath
    npm run tailwind:watch
}
Write-Host "Tailwind watcher running (Job ID: $($tailwindJob.Id)).`n" -ForegroundColor Green

# 3. Run Blazor with hot reload
Write-Host "Starting Blazor app. Changes to .razor and .cs trigger hot reload." -ForegroundColor Gray
Write-Host "Press Ctrl+C to stop.`n" -ForegroundColor Gray

try {
    dotnet watch run --project src/HeatconERP.Web --launch-profile http
} finally {
    Write-Host "`nStopping Tailwind watcher..." -ForegroundColor Gray
    Stop-Job -Id $tailwindJob.Id -ErrorAction SilentlyContinue
    Remove-Job -Id $tailwindJob.Id -Force -ErrorAction SilentlyContinue
}
