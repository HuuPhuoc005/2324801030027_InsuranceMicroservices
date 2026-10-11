param([string]$JavaExecutable = 'C:\Program Files\Eclipse Adoptium\jdk-11.0.32.101-hotspot\bin\java.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$startedProcesses = @()
function Wait-Http([string]$Url, [int]$Attempts = 60) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        try { return Invoke-RestMethod $Url -TimeoutSec 2 } catch { Start-Sleep -Milliseconds 500 }
    }
    throw "HTTP check timed out: $Url"
}
try {
    $eurekaDirectory = Join-Path $repoRoot 'eureka-server'
    $jar = Join-Path $eurekaDirectory 'build/libs/eureka-server-0.0.1-SNAPSHOT.jar'
    $startedProcesses += Start-Process -FilePath $JavaExecutable -ArgumentList @('-jar', $jar) -WorkingDirectory $eurekaDirectory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $repoRoot 'artifacts/eureka-runtime.log') -RedirectStandardError (Join-Path $repoRoot 'artifacts/eureka-runtime-error.log') -PassThru
    Wait-Http 'http://localhost:8761/' | Out-Null
    foreach ($service in @('PaymentService','AgentPortalApiGateway')) {
        $directory = Join-Path $repoRoot $service
        $assembly = Join-Path $directory "bin/Debug/net8.0/$service.dll"
        $arguments = @($assembly, '--Logging:LogLevel:Default=Warning')
        if ($service -eq 'AgentPortalApiGateway') { $arguments += '--urls=http://localhost:8099' }
        $startedProcesses += Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $directory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $repoRoot "artifacts/$service-runtime.log") -RedirectStandardError (Join-Path $repoRoot "artifacts/$service-runtime-error.log") -PassThru
    }
    $direct = Wait-Http 'http://localhost:5100/api/payments/health'
    $gateway = Wait-Http 'http://localhost:8099/api/payments/health' 120
    if ($direct.service -ne 'PaymentService' -or $gateway.service -ne 'PaymentService' -or $gateway.status -ne 'UP') { throw 'Unexpected service discovery response.' }
    "Eureka -> Gateway -> PaymentService HTTP: UP" | Tee-Object (Join-Path $repoRoot 'artifacts/gateway-smoke-test.log')
} finally {
    foreach ($process in $startedProcesses) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
}
