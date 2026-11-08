$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$processFile = Join-Path $repoRoot '.local/bai9-runtime/processes.json'

if (Test-Path -LiteralPath $processFile) {
    try {
        $records = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $processFile -Raw)
        foreach ($record in $records) {
            if ($null -eq $record -or $null -eq $record.Id -or $null -eq $record.StartTicks) { continue }
            $process = Get-Process -Id $record.Id -ErrorAction SilentlyContinue
            if ($null -eq $process -or $null -eq $process.StartTime) { continue }
            if ($process.StartTime.ToUniversalTime().Ticks.ToString() -eq [string]$record.StartTicks) {
                Write-Host "Stopping recorded process $($process.ProcessName) (PID $($process.Id))..."
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
    } catch { }
    Remove-Item -LiteralPath $processFile -Force -ErrorAction SilentlyContinue
}

# Also ensure all microservices ports are freed
$ports = @(4099, 5030, 5035, 5040, 5050, 5065, 5070, 5081, 6060, 8099, 8761)
foreach ($port in $ports) {
    $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    foreach ($conn in $conns) {
        if ($conn.OwningProcess -gt 4) {
            Write-Host "Freeing port $port (Killing PID $($conn.OwningProcess))..."
            Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host 'All Bai 9 recorded processes and ports stopped successfully.'
