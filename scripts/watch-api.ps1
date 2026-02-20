# HeatconERP - Run API with Hot Reload (auto-reload on file changes)
# Run from project root: .\scripts\watch-api.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "=== HeatconERP API (Watch Mode) ===" -ForegroundColor Cyan
Write-Host "Changes to .cs files will trigger hot reload. Press Ctrl+C to stop.`n" -ForegroundColor Gray

dotnet watch run --project src/HeatconERP.API
