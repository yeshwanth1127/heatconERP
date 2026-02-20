# Run the quotation migration SQL against the database
# Uses connection info from appsettings.Development.json
# Requires: psql (PostgreSQL client) in PATH, or set PGPASSWORD, PGHOST, PGPORT, PGDATABASE, PGUSER

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "apply-quotation-migration.sql"

# Try to get connection string from appsettings
$appsettingsPath = Join-Path $scriptDir "..\src\HeatconERP.API\appsettings.Development.json"
if (Test-Path $appsettingsPath) {
    $config = Get-Content $appsettingsPath | ConvertFrom-Json
    $connStr = $config.ConnectionStrings.DefaultConnection
    if ($connStr) {
        # Parse connection string (simple parsing)
        $host = if ($connStr -match "Host=([^;]+)") { $Matches[1] } else { "localhost" }
        $port = if ($connStr -match "Port=(\d+)") { $Matches[1] } else { "5432" }
        $db = if ($connStr -match "Database=([^;]+)") { $Matches[1] } else { "heatconerp" }
        $user = if ($connStr -match "Username=([^;]+)") { $Matches[1] } else { "postgres" }
        $pass = if ($connStr -match "Password=([^;]+)") { $Matches[1] } else { "" }
        $env:PGPASSWORD = $pass
        $env:PGHOST = $host
        $env:PGPORT = $port
        $env:PGDATABASE = $db
        $env:PGUSER = $user
    }
}

Write-Host "Running quotation migration from $sqlFile"
psql -f $sqlFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "Migration failed. You can run the SQL manually: psql -f $sqlFile"
    exit 1
}
Write-Host "Migration completed successfully."
