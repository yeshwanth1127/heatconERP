# Kill process using port 5212
$port = 5212
$process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
if ($process) {
    Write-Host "Killing process $process using port $port..." -ForegroundColor Yellow
    Stop-Process -Id $process -Force
    Write-Host "Port $port is now free." -ForegroundColor Green
} else {
    Write-Host "No process found using port $port." -ForegroundColor Gray
}
