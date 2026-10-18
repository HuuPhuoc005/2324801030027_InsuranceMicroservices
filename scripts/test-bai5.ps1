param([switch]$NativePostgres)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$previousConnection = $env:PRICING_TEST_POSTGRES
Push-Location $repoRoot
try {
    if ($NativePostgres -and -not $env:PRICING_TEST_POSTGRES) {
        $env:PRICING_TEST_POSTGRES = 'Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=lab_pass'
    }
    dotnet test InsuranceMicroservices.slnx -m:1 -nr:false --logger trx --results-directory artifacts/bai5-test-results -v minimal 2>&1 | Tee-Object artifacts/bai5-test.log
    if ($LASTEXITCODE -ne 0) { throw 'Bai 5 tests failed. See artifacts/bai5-test.log.' }
} finally {
    $env:PRICING_TEST_POSTGRES = $previousConnection
    Pop-Location
}
