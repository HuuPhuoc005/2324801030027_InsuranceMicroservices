param([switch]$NativeInfrastructure)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$previousPostgres = $env:PRICING_TEST_POSTGRES
$previousElastic = $env:DASHBOARD_TEST_ELASTICSEARCH
try {
    if ($NativeInfrastructure) {
        if (-not $env:PRICING_TEST_POSTGRES) { $env:PRICING_TEST_POSTGRES='Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=lab_pass' }
        if (-not $env:DASHBOARD_TEST_ELASTICSEARCH) { $env:DASHBOARD_TEST_ELASTICSEARCH='http://localhost:9200' }
    }
    Push-Location $repoRoot
    try {
        dotnet test InsuranceMicroservices.slnx -m:1 --logger trx --results-directory artifacts/bai6-test-results
        if ($LASTEXITCODE -ne 0) { throw 'Assignment tests failed.' }
    } finally { Pop-Location }
} finally {
    $env:PRICING_TEST_POSTGRES=$previousPostgres
    $env:DASHBOARD_TEST_ELASTICSEARCH=$previousElastic
}
