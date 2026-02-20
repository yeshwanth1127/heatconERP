# Create HeatconERP database if it doesn't exist
# Run from project root: .\scripts\create_db.ps1
# Requires: PostgreSQL psql in PATH
# Optional: $env:PGPASSWORD = "your_password" to avoid password prompt

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
psql -U postgres -f "$scriptDir\create_db.sql"
