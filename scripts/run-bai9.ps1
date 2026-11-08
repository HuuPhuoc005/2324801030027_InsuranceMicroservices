param([string]$JavaExecutable = 'C:\Program Files\Eclipse Adoptium\jdk-11.0.32.101-hotspot\bin\java.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runtime = Join-Path $repoRoot '.local/bai9-runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$started = @()

function Wait-Http([string]$Url, [int]$Attempts = 120, $Process = $null) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Service exited before startup: $Url. See logs in $runtime"
        }
        try {
            $resp = Invoke-RestMethod $Url -TimeoutSec 2
            return $resp
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    throw "Startup timed out: $Url. See logs in $runtime"
}

try {
    Write-Host "1. Checking Infrastructure (Postgres, RabbitMQ, Elasticsearch)..."
    # Ensure Elasticsearch service is running
    $esService = Get-Service -Name "elasticsearch-service-x64" -ErrorAction SilentlyContinue
    if ($null -ne $esService -and $esService.Status -ne 'Running') {
        Write-Host "Starting Elasticsearch service..."
        Start-Service -Name "elasticsearch-service-x64"
        Start-Sleep -Seconds 5
    }
    Invoke-RestMethod 'http://localhost:9200' -TimeoutSec 5 | Out-Null
    Write-Host " -> Elasticsearch is ready on http://localhost:9200"

    # Ensure RabbitMQ is listening on 5672
    $tcp = New-Object System.Net.Sockets.TcpClient
    try { $tcp.Connect('localhost', 5672) } finally { $tcp.Dispose() }
    Write-Host " -> RabbitMQ is ready on localhost:5672"

    # Ensure Postgres is listening on 5432
    $tcp2 = New-Object System.Net.Sockets.TcpClient
    try { $tcp2.Connect('localhost', 5432) } finally { $tcp2.Dispose() }
    Write-Host " -> PostgreSQL is ready on localhost:5432"

    Write-Host "2. Checking / Starting Eureka Discovery Server on port 8761..."
    try { $eureka = Invoke-WebRequest -UseBasicParsing 'http://localhost:8761/' -TimeoutSec 2 } catch { $eureka = $null }
    if ($null -eq $eureka) {
        $directory = Join-Path $repoRoot 'eureka-server'
        $jar = Join-Path $directory 'build/libs/eureka-server-0.0.1-SNAPSHOT.jar'
        if (-not (Test-Path -LiteralPath $jar)) { throw 'Build Eureka with Java 11 first.' }
        $process = Start-Process -FilePath $JavaExecutable -ArgumentList @('-jar', "`"$jar`"") -WorkingDirectory $directory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime 'eureka.log') -RedirectStandardError (Join-Path $runtime 'eureka-error.log') -PassThru
        $started += $process
        Wait-Http 'http://localhost:8761/' -Process $process | Out-Null
    }
    Write-Host " -> Eureka Server is ready on http://localhost:8761"

    Write-Host "3. Starting Backend Microservices..."
    $services = [ordered]@{
        AuthService            = 6060;
        ChatService            = 4099;
        PricingService         = 5040;
        ProductService         = 5030;
        PolicyService          = 5050;
        PolicySearchService    = 5065;
        PaymentService         = 5070;
        DashboardService       = 5035;
        AgentPortalApiGateway  = 8099
    }

    foreach ($name in $services.Keys) {
        $port = $services[$name]
        try { $health = Invoke-RestMethod "http://localhost:$port/health" -TimeoutSec 2 } catch { $health = $null }
        if ($null -ne $health) {
            Write-Host " -> $name already running on port $port"
            continue
        }
        $directory = Join-Path $repoRoot $name
        $assembly = Join-Path $directory "bin/Debug/net8.0/$name.dll"
        if (-not (Test-Path -LiteralPath $assembly)) {
            Write-Host "Building $name..."
            dotnet build "$directory\$name.csproj" -v q | Out-Null
        }
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @("`"$assembly`"", "--urls", "http://localhost:$port") -WorkingDirectory $directory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime "$name.log") -RedirectStandardError (Join-Path $runtime "$name-error.log") -PassThru
        $started += $process
        Wait-Http "http://localhost:$port/health" -Process $process | Out-Null
        Write-Host " -> $name started successfully on port $port"
    }

    Write-Host "4. Waiting for Eureka registration..."
    $eurekaApps = @('AUTHSERVICE', 'CHATSERVICE', 'PRODUCTSERVICE', 'PRICINGSERVICE', 'POLICYSERVICE', 'POLICYSEARCHSERVICE', 'PAYMENTSERVICE', 'DASHBOARDSERVICE')
    foreach ($app in $eurekaApps) {
        $registered = $false
        for ($i = 0; $i -lt 30; $i++) {
            try {
                $info = Invoke-RestMethod "http://localhost:8761/eureka/apps/$app" -Headers @{Accept='application/json'} -TimeoutSec 2
                if (@($info.application.instance | Where-Object { $_.status -eq 'UP' }).Count -gt 0) {
                    $registered = $true
                    break
                }
            } catch { }
            Start-Sleep -Milliseconds 500
        }
        if ($registered) {
            Write-Host " -> $app registered in Eureka (UP)"
        } else {
            Write-Host " -> $app pending in Eureka"
        }
    }

    Write-Host "5. Starting Frontend BlazorWasmClient on port 5081..."
    try { $wasm = Invoke-WebRequest -UseBasicParsing 'http://localhost:5081/' -TimeoutSec 2 } catch { $wasm = $null }
    if ($null -eq $wasm) {
        $wasmDir = Join-Path $repoRoot 'BlazorWasmClient'
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--project', "`"$wasmDir\BlazorWasmClient.csproj`"", '--no-build') -WorkingDirectory $wasmDir -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtime 'BlazorWasmClient.log') -RedirectStandardError (Join-Path $runtime 'BlazorWasmClient-error.log') -PassThru
        $started += $process
        Wait-Http 'http://localhost:5081/' -Process $process | Out-Null
        Write-Host " -> BlazorWasmClient started on http://localhost:5081"
    } else {
        Write-Host " -> BlazorWasmClient already running on http://localhost:5081"
    }

    Write-Host "`n======================================================="
    Write-Host "ALL SERVICES RUNNING SUCCESSFULLY!"
    Write-Host "Frontend Web URL:  http://localhost:5081"
    Write-Host "API Gateway:       http://localhost:8099"
    Write-Host "Eureka Dashboard:  http://localhost:8761"
    Write-Host "=======================================================`n"
    Start-Process "http://localhost:5081"
} catch {
    Write-Error $_
    foreach ($process in $started) {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    throw
}

if ($started.Count -gt 0) {
    $processFile = Join-Path $runtime 'processes.json'
    $records = @()
    if (Test-Path -LiteralPath $processFile) {
        $existing = ConvertFrom-Json -InputObject (Get-Content -LiteralPath $processFile -Raw)
        foreach ($record in $existing) {
            if ($null -ne $record.Id -and $null -ne $record.StartTicks) {
                $records += $record
            }
        }
    }
    $records += @($started | ForEach-Object { [pscustomobject]@{Id=$_.Id;StartTicks=$_.StartTime.ToUniversalTime().Ticks.ToString()} })
    ConvertTo-Json -InputObject @($records) -Depth 4 | Set-Content -LiteralPath $processFile -Encoding utf8
}
