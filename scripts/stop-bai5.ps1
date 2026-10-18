$ErrorActionPreference = 'Stop'
$processFile = Join-Path (Split-Path $PSScriptRoot -Parent) '.local/bai5-runtime/processes.json'
if (-not (Test-Path -LiteralPath $processFile)) { Write-Host 'No Bai 5 processes were recorded.'; exit 0 }
foreach ($record in @(Get-Content -LiteralPath $processFile -Raw | ConvertFrom-Json)) {
    $process = Get-Process -Id $record.Id -ErrorAction SilentlyContinue
    if ($null -ne $process -and $process.StartTime.ToUniversalTime().Ticks.ToString() -eq [string]$record.StartTicks) {
        Stop-Process -Id $process.Id -Force
    }
}
