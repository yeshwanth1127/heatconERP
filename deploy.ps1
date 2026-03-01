# HeatconERP Docker Deployment Quick Start (Windows)
# This script automates the setup process

param(
    [switch]$SkipPrerequisites = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "HeatconERP Docker Deployment" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
if (-not $SkipPrerequisites) {
    Write-Host -NoNewline "Checking Docker... "
    try {
        docker --version | Out-Null
        Write-Host "OK" -ForegroundColor Green
    } catch {
        Write-Host "NOT FOUND" -ForegroundColor Red
        Write-Host "Please install Docker Desktop from https://www.docker.com/products/docker-desktop"
        exit 1
    }

    Write-Host -NoNewline "Checking Docker Compose... "
    try {
        docker compose version | Out-Null
        Write-Host "OK" -ForegroundColor Green
    } catch {
        Write-Host "NOT FOUND" -ForegroundColor Red
        Write-Host "Docker Compose is included with Docker Desktop"
        exit 1
    }
}

# Check if .env exists
Write-Host ""
if (-not (Test-Path .env)) {
    Write-Host "⚠️  .env file not found" -ForegroundColor Yellow
    Write-Host "Creating .env from template..."
    
    if (Test-Path .env.example) {
        Copy-Item .env.example .env
    } else {
        @"
DB_USER=postgres
DB_PASSWORD=SecurePasswordHere123!@#
DB_NAME=heatconerp
DB_PORT=5432
API_PORT=5212
WEB_PORT=5118
"@ | Out-File .env -Encoding UTF8
    }
    
    Write-Host "✓ .env created" -ForegroundColor Green
    Write-Host "Please edit .env with your production values:"
    Write-Host "  notepad .env"
    Read-Host "Press Enter after editing .env"
}

# Build images
Write-Host ""
Write-Host "Building Docker images..." -ForegroundColor Yellow
docker compose build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Start services
Write-Host ""
Write-Host "Starting services..." -ForegroundColor Yellow
docker compose up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start services!" -ForegroundColor Red
    exit 1
}

# Wait for database to be ready
Write-Host ""
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    try {
        docker compose exec -T postgres pg_isready -U postgres 2>$null | Out-Null
        Write-Host "✓ PostgreSQL is ready" -ForegroundColor Green
        break
    } catch {
        Write-Host -NoNewline "."
        Start-Sleep -Seconds 2
        $attempt++
    }
}

# Apply migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
try {
    docker compose exec -T api dotnet ef database update `
        --project /app/HeatconERP.Infrastructure.dll `
        --no-build 2>$null
    Write-Host "✓ Migrations applied" -ForegroundColor Green
} catch {
    Write-Host "Note: Migrations may already be applied or manual step needed" -ForegroundColor Yellow
}

# Display status
Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

docker compose ps

Write-Host ""
Write-Host "Access your application:"
Write-Host "  Web Application: http://localhost:5118" -ForegroundColor Yellow
Write-Host "  API Swagger: http://localhost:5212/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "Default credentials:"
Write-Host "  Username: admin" -ForegroundColor Yellow
Write-Host "  Password: admin123" -ForegroundColor Yellow
Write-Host ""
Write-Host "View logs:"
Write-Host "  docker compose logs -f" -ForegroundColor Cyan
Write-Host ""
Write-Host "Useful commands:"
Write-Host "  Stop services: docker compose stop" -ForegroundColor Cyan
Write-Host "  Start services: docker compose start" -ForegroundColor Cyan
Write-Host "  View logs: docker compose logs -f" -ForegroundColor Cyan
Write-Host "  List containers: docker compose ps" -ForegroundColor Cyan
Write-Host ""
