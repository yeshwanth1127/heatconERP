# HeatconERP - Rebuild and Restart API
# Run from project root: .\scripts\rebuild-and-run.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ApiPort = 5212

Set-Location $ProjectRoot

Write-Host "=== HeatconERP Rebuild & Restart ===" -ForegroundColor Cyan

# 1. Stop existing API on port 5212
Write-Host "`n[1/4] Stopping existing API on port $ApiPort..." -ForegroundColor Yellow
$stopped = $false
try {
    $conn = Get-NetTCPConnection -LocalPort $ApiPort -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $procId = $conn.OwningProcess | Select-Object -First 1
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "    Stopped process $procId" -ForegroundColor Green
        $stopped = $true
    }
} catch { }
if (-not $stopped) {
    # Fallback: netstat to find PID
    $line = netstat -ano | Select-String ":$ApiPort\s+.*LISTENING"
    if ($line) {
        $procId = ($line -split '\s+')[-1]
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "    Stopped process $procId" -ForegroundColor Green
    } else {
        Write-Host "    No process on port $ApiPort" -ForegroundColor Gray
    }
}

# 2. Apply migrations
Write-Host "`n[2/4] Applying EF migrations..." -ForegroundColor Yellow
dotnet ef database update --project src/HeatconERP.Infrastructure --startup-project src/HeatconERP.API
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Migration failed. Check .env and PostgreSQL." -ForegroundColor Red
    exit 1
}
Write-Host "    Done" -ForegroundColor Green

# 3. Build
Write-Host "`n[3/4] Building solution..." -ForegroundColor Yellow
dotnet build HeatconERP.slnx
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Build failed." -ForegroundColor Red
    exit 1
}
Write-Host "    Done" -ForegroundColor Green

# 4. Start API
Write-Host "`n[4/4] Starting API..." -ForegroundColor Yellow
Write-Host "    API: http://localhost:$ApiPort" -ForegroundColor Cyan
Write-Host "    Swagger: http://localhost:$ApiPort/swagger" -ForegroundColor Cyan
Write-Host "    Press Ctrl+C to stop`n" -ForegroundColor Gray
dotnet run --project src/HeatconERP.API --no-build
