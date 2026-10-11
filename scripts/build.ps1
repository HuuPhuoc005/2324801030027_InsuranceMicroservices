param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    dotnet build InsuranceMicroservices.slnx -m:1 -nr:false -v minimal 2>&1 | Tee-Object artifacts/build.log
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed. See artifacts/build.log.' }
    if (-not $SkipTests) {
        dotnet test InsuranceMicroservices.slnx --no-build --no-restore -m:1 -nr:false --logger 'trx' --results-directory artifacts/test-results 2>&1 | Tee-Object artifacts/test.log
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed. See artifacts/test.log.' }
    }
} finally { Pop-Location }
