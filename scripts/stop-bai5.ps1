$ErrorActionPreference = 'Stop'
$processFile = Join-Path (Split-Path $PSScriptRoot -Parent) '.local/bai5-runtime/processes.json'
if (-not (Test-Path -LiteralPath $processFile)) { Write-Host 'No Bai 5 processes were recorded.'; exit 0 }
$records = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $processFile -Raw)
foreach ($record in $records) {
    if ($null -eq $record -or $null -eq $record.Id -or $null -eq $record.StartTicks) { continue }
    $process = Get-Process -Id $record.Id -ErrorAction SilentlyContinue
    if ($null -eq $process -or $null -eq $process.StartTime) { continue }
    if ($process.StartTime.ToUniversalTime().Ticks.ToString() -eq [string]$record.StartTicks) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
    }
}
Remove-Item -LiteralPath $processFile
