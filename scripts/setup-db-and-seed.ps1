# HeatconERP - One-click DB bootstrap: create DB (if missing), apply EF migrations, run API seeding once.
# Run from project root: .\scripts\setup-db-and-seed.ps1
#
# Requirements:
# - PostgreSQL running (default localhost:5432)
# - psql in PATH
# - .NET 10 SDK
# - dotnet-ef installed (dotnet tool install --global dotnet-ef)
# - DATABASE_URL set in environment OR in .env at project root
#
# DATABASE_URL example:
#   Host=localhost;Port=5432;Database=heatconerp;Username=postgres;Password=your_password

[CmdletBinding()]
param(
    [int]$ApiPort = 5212,
    [int]$HealthTimeoutSeconds = 60,
    [switch]$SkipCreateDb,
    [switch]$SkipMigrations,
    [switch]$SkipSeed,
    # Optional: apply the raw SQL "fix-all-migrations.sql" (only use if you know you need it)
    [switch]$ApplyFixAllSql
)

$ErrorActionPreference = "Stop"

function Get-ProjectRoot {
    return (Split-Path -Parent $PSScriptRoot)
}

function Get-DotEnvValue([string]$DotEnvPath, [string]$Name) {
    if (-not (Test-Path $DotEnvPath)) { return $null }
    foreach ($line in (Get-Content $DotEnvPath -ErrorAction Stop)) {
        $t = $line.Trim()
        if ($t.Length -eq 0) { continue }
        if ($t.StartsWith("#")) { continue }
        $idx = $t.IndexOf("=")
        if ($idx -lt 1) { continue }
        $key = $t.Substring(0, $idx).Trim()
        if ($key -ne $Name) { continue }
        $val = $t.Substring($idx + 1).Trim()
        # strip surrounding quotes
        if (($val.StartsWith('"') -and $val.EndsWith('"')) -or ($val.StartsWith("'") -and $val.EndsWith("'"))) {
            $val = $val.Substring(1, $val.Length - 2)
        }
        return $val
    }
    return $null
}

function Parse-DbConnectionString([string]$ConnectionString) {
    $map = @{}
    foreach ($part in ($ConnectionString -split ";")) {
        $p = $part.Trim()
        if ($p.Length -eq 0) { continue }
        $idx = $p.IndexOf("=")
        if ($idx -lt 1) { continue }
        $k = $p.Substring(0, $idx).Trim()
        $v = $p.Substring($idx + 1).Trim()
        if ($k.Length -gt 0) { $map[$k] = $v }
    }

    # NOTE: avoid using variable name $host (collides with built-in $Host)
    $dbHost = $map["Host"]; if (-not $dbHost) { $dbHost = $map["Server"] }
    $dbPort = $map["Port"]; if (-not $dbPort) { $dbPort = "5432" }
    $dbName = $map["Database"]; if (-not $dbName) { $dbName = $map["Initial Catalog"] }
    $dbUser = $map["Username"]; if (-not $dbUser) { $dbUser = $map["User ID"] }; if (-not $dbUser) { $dbUser = $map["UserId"] }
    $dbPass = $map["Password"]

    if (-not $dbHost -or -not $dbPort -or -not $dbName -or -not $dbUser) {
        throw "DATABASE_URL is missing required fields. Expected at least Host, Port, Database, Username. Got: $ConnectionString"
    }

    return @{
        Host = $dbHost
        Port = [int]$dbPort
        Database = $dbName
        Username = $dbUser
        Password = $dbPass
    }
}

function Assert-Command([string]$Name, [string]$InstallHint) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Missing required command '$Name'. $InstallHint"
    }
}

function Invoke-Psql([hashtable]$Db, [string]$Database, [string]$Sql) {
    # Uses PGPASSWORD for non-interactive auth
    if ($Db.Password) { $env:PGPASSWORD = $Db.Password }
    try {
        & psql -h $Db.Host -p $Db.Port -U $Db.Username -d $Database -v ON_ERROR_STOP=1 -tAc $Sql
        if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE" }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
}

function Ensure-DatabaseExists([hashtable]$Db) {
    $dbName = $Db.Database.Replace("'", "''")
    $dbIdent = $Db.Database.Replace('"', '""')
    $owner = $Db.Username.Replace('"', '""')

    Write-Host "Checking database '$($Db.Database)'..." -ForegroundColor Gray
    $exists = Invoke-Psql -Db $Db -Database "postgres" -Sql "SELECT 1 FROM pg_database WHERE datname = '$dbName';"
    if ($exists -match "1") {
        Write-Host "Database exists." -ForegroundColor Green
        return
    }

    Write-Host "Creating database '$($Db.Database)'..." -ForegroundColor Yellow
    # Use quoted identifiers for owner/db; keep it simple
    Invoke-Psql -Db $Db -Database "postgres" -Sql "CREATE DATABASE ""$dbIdent"" OWNER ""$owner"";"
    Write-Host "Database created." -ForegroundColor Green
}

function Wait-ForHealth([string]$Url, [int]$TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-RestMethod -Method Get -Uri $Url -TimeoutSec 3
            if ($resp -and $resp.status -eq "healthy") { return $true }
            # If endpoint returns something unexpected but 200, still consider it up
            return $true
        } catch {
            Start-Sleep -Milliseconds 750
        }
    }
    return $false
}

function Get-EnquiriesTableInfo {
    # Returns @{ Name = <actual table_name>; Ident = <schema-qualified, correctly quoted identifier> } or $null
    # Prefer the EF-created quoted table name "Enquiries", fall back to unquoted enquiries if present.
    $tableName = Invoke-Psql -Db $db -Database $db.Database -Sql @"
SELECT table_name FROM (
  SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_name='Enquiries'
  UNION ALL
  SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_name='enquiries'
) t
LIMIT 1;
"@
    if ($null -eq $tableName) { $tableName = "" }
    $tableName = $tableName.Trim()
    if (-not $tableName) { return $null }
    $ident = Invoke-Psql -Db $db -Database $db.Database -Sql "SELECT format('public.%I', '$tableName');"
    if ($null -eq $ident) { $ident = "" }
    $ident = $ident.Trim()
    if (-not $ident) { return $null }
    return @{ Name = $tableName; Ident = $ident }
}

function Ensure-EnquiriesColumn([hashtable]$TableInfo, [string]$ColumnName, [string]$AddColumnSqlFragment) {
    $tName = $TableInfo.Name.Replace("'", "''")
    $hasCol = Invoke-Psql -Db $db -Database $db.Database -Sql "SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='$tName' AND column_name='$ColumnName' LIMIT 1;"
    if ($null -eq $hasCol) { $hasCol = "" }
    if ($hasCol.Trim() -eq "1") { return $false }
    Invoke-Psql -Db $db -Database $db.Database -Sql "ALTER TABLE $($TableInfo.Ident) $AddColumnSqlFragment"
    return $true
}

$ProjectRoot = Get-ProjectRoot
Set-Location $ProjectRoot

Write-Host "=== HeatconERP DB Setup (migrations + seed) ===" -ForegroundColor Cyan
Write-Host "Project root: $ProjectRoot" -ForegroundColor Gray

# Resolve DATABASE_URL
$dbUrl = $env:DATABASE_URL
if (-not $dbUrl) {
    $dotEnvPath = Join-Path $ProjectRoot ".env"
    $dbUrl = Get-DotEnvValue -DotEnvPath $dotEnvPath -Name "DATABASE_URL"
    if ($dbUrl) { $env:DATABASE_URL = $dbUrl }
}
if (-not $dbUrl) {
    throw "DATABASE_URL not found. Set `$env:DATABASE_URL or add DATABASE_URL=... to $ProjectRoot\.env"
}

$db = Parse-DbConnectionString -ConnectionString $dbUrl

# Preconditions
Assert-Command -Name "dotnet" -InstallHint "Install .NET 10 SDK."
Assert-Command -Name "psql" -InstallHint "Install PostgreSQL client tools and ensure 'psql' is on PATH."

Write-Host "`n[1/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore .\HeatconERP.slnx
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
Write-Host "Restore complete." -ForegroundColor Green

if (-not $SkipCreateDb) {
    Write-Host "`n[2/5] Creating database (if missing)..." -ForegroundColor Yellow
    Ensure-DatabaseExists -Db $db
} else {
    Write-Host "`n[2/5] Skipping database creation." -ForegroundColor Gray
}

if (-not $SkipMigrations) {
    Write-Host "`n[3/5] Applying EF Core migrations..." -ForegroundColor Yellow
    # Ensure dotnet-ef is available
    dotnet ef --version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet-ef not available. Install: dotnet tool install --global dotnet-ef"
    }
    dotnet ef database update --project src\HeatconERP.Infrastructure --startup-project src\HeatconERP.API
    if ($LASTEXITCODE -ne 0) { throw "EF migrations failed (check DATABASE_URL credentials and PostgreSQL)."}
    Write-Host "Migrations applied." -ForegroundColor Green
} else {
    Write-Host "`n[3/5] Skipping migrations." -ForegroundColor Gray
}

# Safety net: ensure Enquiries core columns exist (some DBs were created via partial SQL scripts)
try {
    $t = Get-EnquiriesTableInfo
    if ($t) {
        $anyEnsured = $false
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "IsDeleted" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "EnquiryNumber" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "EnquiryNumber" text NOT NULL DEFAULT '''';') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "CompanyName" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "CompanyName" text NOT NULL DEFAULT '''';') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "DateReceived" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "DateReceived" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "Source" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "Source" text NOT NULL DEFAULT ''Manual'';') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "Priority" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "Priority" text NOT NULL DEFAULT ''Medium'';') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "FeasibilityStatus" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "FeasibilityStatus" text NOT NULL DEFAULT ''Pending'';') -or $anyEnsured
        $anyEnsured = (Ensure-EnquiriesColumn -TableInfo $t -ColumnName "IsAerospace" -AddColumnSqlFragment 'ADD COLUMN IF NOT EXISTS "IsAerospace" boolean NOT NULL DEFAULT false;') -or $anyEnsured

        if ($anyEnsured) {
            Write-Host "`nEnquiries safety-net: ensured core columns exist on $($t.Ident)." -ForegroundColor Green
        }
    }

} catch {
    Write-Host "`nWarning: could not verify/add Enquiries core columns. Error: $_" -ForegroundColor Yellow
}

if ($ApplyFixAllSql) {
    Write-Host "`n[4/5] Applying fix-all-migrations.sql (optional)..." -ForegroundColor Yellow
    if ($db.Password) { $env:PGPASSWORD = $db.Password }
    try {
        & psql -h $db.Host -p $db.Port -U $db.Username -d $db.Database -v ON_ERROR_STOP=1 -f (Join-Path $ProjectRoot "scripts\fix-all-migrations.sql")
        if ($LASTEXITCODE -ne 0) { throw "fix-all-migrations.sql failed." }
    } finally {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
    Write-Host "Fix-all SQL applied." -ForegroundColor Green
} else {
    Write-Host "`n[4/5] Skipping fix-all SQL." -ForegroundColor Gray
}

if (-not $SkipSeed) {
    Write-Host "`n[5/5] Seeding data (API startup)..." -ForegroundColor Yellow
    $healthUrl = "http://localhost:$ApiPort/health"
    Write-Host "Starting API on http://localhost:$ApiPort (will auto-migrate + seed)..." -ForegroundColor Gray

    $proc = Start-Process -FilePath "dotnet" -WorkingDirectory $ProjectRoot -NoNewWindow -ArgumentList @(
        "run",
        "--project", "src\HeatconERP.API",
        "--urls", "http://localhost:$ApiPort"
    ) -PassThru

    try {
        $ok = Wait-ForHealth -Url $healthUrl -TimeoutSeconds $HealthTimeoutSeconds
        if (-not $ok) {
            throw "API did not become healthy within $HealthTimeoutSeconds seconds. Check API logs by running: dotnet run --project src\HeatconERP.API"
        }
        Write-Host "Seed complete (API healthy)." -ForegroundColor Green
    } finally {
        if ($proc -and -not $proc.HasExited) {
            Write-Host "Stopping API process (PID $($proc.Id))..." -ForegroundColor Gray
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
} else {
    Write-Host "`n[5/5] Skipping seeding." -ForegroundColor Gray
}

Write-Host "`nDone. You can now run:" -ForegroundColor Cyan
Write-Host "  .\scripts\watch-api.ps1" -ForegroundColor Gray
Write-Host "  .\scripts\watch-web.ps1" -ForegroundColor Gray


