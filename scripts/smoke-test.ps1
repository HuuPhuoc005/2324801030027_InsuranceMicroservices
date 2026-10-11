$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$services = [ordered]@{ AuthService=6060; ChatService=4099; PricingService=5040; PolicyService=5050; PolicySearchService=5065; PaymentService=5100; ProductService=5030; DocumentService=5080; DashboardService=5035 }
$startedProcesses = @()
$results = @()
try {
    foreach ($service in $services.Keys) {
        $directory = Join-Path $repoRoot $service
        $assembly = Join-Path $directory "bin/Debug/net8.0/$service.dll"
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @($assembly, '--eureka:client:shouldRegisterWithEureka=false', '--eureka:client:shouldFetchRegistry=false') -WorkingDirectory $directory -WindowStyle Hidden -PassThru
        $startedProcesses += $process
        $response = $null
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            if ($process.HasExited) { throw "$service exited before serving HTTP." }
            try { $response = Invoke-RestMethod "http://localhost:$($services[$service])/health" -TimeoutSec 2; break } catch { Start-Sleep -Milliseconds 500 }
        }
        if ($null -eq $response -or $response.service -ne $service -or $response.status -ne 'UP') { throw "Invalid HTTP health response from $service" }
        $results += [pscustomobject]@{ Service=$service; Port=$services[$service]; Status=$response.status }
    }
    $results | Format-Table | Out-String | Tee-Object (Join-Path $repoRoot 'artifacts/smoke-test.log')
} finally {
    foreach ($process in $startedProcesses) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
