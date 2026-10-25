param([string]$JavaExecutable = 'C:\Program Files\Eclipse Adoptium\jdk-11.0.32.101-hotspot\bin\java.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runtime = Join-Path $repoRoot '.local/bai6-runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$started = @()
function Wait-Http([string]$Url, [int]$Attempts = 120, $Process = $null) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        if ($null -ne $Process -and $Process.HasExited) { throw "Service exited before startup: $Url. See logs in $runtime" }
        try { return Invoke-RestMethod $Url -TimeoutSec 2 } catch { Start-Sleep -Milliseconds 500 }
    }
    throw "Startup timed out: $Url. See logs in $runtime"
}
try {
    # Both are required; this script never substitutes in-memory statistics or messaging.
    Invoke-RestMethod 'http://localhost:9200' -TimeoutSec 5 | Out-Null
    $tcp = New-Object System.Net.Sockets.TcpClient
    try { $tcp.Connect('localhost', 5672) } finally { $tcp.Dispose() }
    try { $eureka = Invoke-WebRequest -UseBasicParsing 'http://localhost:8761/' -TimeoutSec 2 } catch { $eureka = $null }
    if ($null -eq $eureka) {
        $directory = Join-Path $repoRoot 'eureka-server'
        $jar = Join-Path $directory 'build/libs/eureka-server-0.0.1-SNAPSHOT.jar'
        if (-not (Test-Path -LiteralPath $jar)) { throw 'Build Eureka with Java 11 and gradlew.bat bootJar first.' }
        $process = Start-Process -FilePath $JavaExecutable -ArgumentList @('-jar', "`"$jar`"") -WorkingDirectory $directory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime 'eureka.log') -RedirectStandardError (Join-Path $runtime 'eureka-error.log') -PassThru
        $started += $process
        Wait-Http 'http://localhost:8761/' -Process $process | Out-Null
    }
    $services = [ordered]@{AuthService=6060;ChatService=4099;DashboardService=5035;ProductService=5030;AgentPortalApiGateway=8099}
    foreach ($name in $services.Keys) {
        $port = $services[$name]
        try { $health = Invoke-RestMethod "http://localhost:$port/health" -TimeoutSec 2 } catch { $health = $null }
        if ($null -ne $health) {
            if ($health.service -ne $name) { throw "Port $port belongs to a different application." }
            continue
        }
        $directory = Join-Path $repoRoot $name
        $assembly = Join-Path $directory "bin/Debug/net8.0/$name.dll"
        if (-not (Test-Path -LiteralPath $assembly)) { throw 'Run dotnet build InsuranceMicroservices.slnx -m:1 first.' }
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @("`"$assembly`"") -WorkingDirectory $directory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime "$name.log") -RedirectStandardError (Join-Path $runtime "$name-error.log") -PassThru
        $started += $process
        Wait-Http "http://localhost:$port/health" -Process $process | Out-Null
    }
    foreach ($name in @('AUTHSERVICE','CHATSERVICE','DASHBOARDSERVICE','PRODUCTSERVICE')) {
        $up = $false
        for ($attempt=0; $attempt -lt 120; $attempt++) {
            try {
                $registration = Invoke-RestMethod "http://localhost:8761/eureka/apps/$name" -Headers @{Accept='application/json'} -TimeoutSec 2
                if (@($registration.application.instance | Where-Object { $_.status -eq 'UP' }).Count -gt 0) { $up=$true; break }
            } catch { }
            Start-Sleep -Milliseconds 500
        }
        if (-not $up) { throw "$name did not register UP in Eureka." }
    }
    Write-Output 'Bai 6 running: Eureka 8761, Gateway 8099, Auth 6060, Chat 4099, Dashboard 5035, Product 5030.'
} catch {
    foreach ($process in $started) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
    throw
}
if ($started.Count -gt 0) {
    $processFile = Join-Path $runtime 'processes.json'
    $records = @()
    if (Test-Path -LiteralPath $processFile) {
        $existing = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $processFile -Raw)
        foreach ($record in $existing) { if ($null -ne $record.Id -and $null -ne $record.StartTicks) { $records += $record } }
    }
    $records += @($started | ForEach-Object { [pscustomobject]@{Id=$_.Id;StartTicks=$_.StartTime.ToUniversalTime().Ticks.ToString()} })
    ConvertTo-Json -InputObject @($records) -Depth 4 | Set-Content -LiteralPath $processFile -Encoding utf8
}
