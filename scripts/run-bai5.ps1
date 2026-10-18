param([string]$JavaExecutable = 'C:\Program Files\Eclipse Adoptium\jdk-11.0.32.101-hotspot\bin\java.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runtimeDirectory = Join-Path $repoRoot '.local/bai5-runtime'
New-Item -ItemType Directory -Path $runtimeDirectory -Force | Out-Null
$started = @()
function Wait-Http([string]$Url, [int]$Attempts = 90) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        try { return Invoke-WebRequest -UseBasicParsing $Url -TimeoutSec 2 } catch { Start-Sleep -Milliseconds 500 }
    }
    throw "HTTP startup timed out: $Url"
}
try {
    try { $eureka = Invoke-WebRequest -UseBasicParsing 'http://localhost:8761/' -TimeoutSec 2 } catch { $eureka = $null }
    if ($null -eq $eureka) {
        $eurekaRoot = Join-Path $repoRoot 'eureka-server'
        $jar = Join-Path $eurekaRoot 'build/libs/eureka-server-0.0.1-SNAPSHOT.jar'
        $process = Start-Process -FilePath $JavaExecutable -ArgumentList @('-jar', $jar) -WorkingDirectory $eurekaRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimeDirectory 'eureka.log') -RedirectStandardError (Join-Path $runtimeDirectory 'eureka-error.log') -PassThru
        $started += $process
        Wait-Http 'http://localhost:8761/' | Out-Null
    }
    $pricingRoot = Join-Path $repoRoot 'PricingService'
    $assembly = Join-Path $pricingRoot 'bin/Debug/net8.0/PricingService.dll'
    try { $health = Invoke-RestMethod 'http://localhost:5040/health' -TimeoutSec 2 } catch { $health = $null }
    if ($null -eq $health) {
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @($assembly) -WorkingDirectory $pricingRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $runtimeDirectory 'pricing.log') -RedirectStandardError (Join-Path $runtimeDirectory 'pricing-error.log') -PassThru
        $started += $process
    }
    Wait-Http 'http://localhost:5040/health' | Out-Null
    $registered = $false
    for ($attempt=0; $attempt -lt 90; $attempt++) {
        try {
            $entry = Invoke-RestMethod 'http://localhost:8761/eureka/apps/PRICINGSERVICE' -Headers @{Accept='application/json'} -TimeoutSec 2
            $registered = @($entry.application.instance | Where-Object { $_.status -eq 'UP' -and $_.port.'$' -eq 5040 }).Count -gt 0
            if ($registered) { break }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    if (-not $registered) { throw 'PricingService is not registered UP on port 5040 in Eureka.' }
    $request = @{
        productCode='TRI'; policyFrom=[DateTimeOffset]::Now.AddDays(5).ToString('o'); policyTo=[DateTimeOffset]::Now.AddDays(10).ToString('o')
        selectedCovers=@('C1','C2','C3'); answers=@(
            @{questionCode='NUM_OF_ADULTS';questionType=1;answer=1},
            @{questionCode='NUM_OF_CHILDREN';questionType=1;answer=1},
            @{questionCode='DESTINATION';questionType=0;answer='EUR'}
        )
    }
    $json = $request | ConvertTo-Json -Depth 8
    $result = Invoke-RestMethod 'http://localhost:5040/api/Pricing' -Method Post -ContentType 'application/json' -Body $json -TimeoutSec 10
    if ($result.totalPrice -ne 98 -or $result.coverPrices.C1 -ne 26 -or $result.coverPrices.C2 -ne 52 -or $result.coverPrices.C3 -ne 20) { throw 'Unexpected travel pricing result.' }
    $request | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $repoRoot 'artifacts/bai5-pricing-request.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $repoRoot 'artifacts/bai5-pricing-response.json') -Encoding utf8
    $invalidStatus = 0
    try { Invoke-WebRequest -UseBasicParsing 'http://localhost:5040/api/Pricing' -Method Post -ContentType 'application/json' -Body '{}' -TimeoutSec 10 | Out-Null } catch { $invalidStatus = [int]$_.Exception.Response.StatusCode }
    if ($invalidStatus -ne 400) { throw 'An invalid pricing request must return HTTP 400.' }
    $summary = @('Eureka localhost:8761: HTTP 200', 'PRICINGSERVICE registered: UP, localhost:5040', 'POST /api/Pricing: HTTP 200, totalPrice=98, C1=26, C2=52, C3=20', 'Invalid pricing request: HTTP 400')
    $summary | Set-Content (Join-Path $repoRoot 'artifacts/bai5-eureka-check.log') -Encoding utf8
    $summary | Write-Output
    if ($started.Count -gt 0) {
        $processFile = Join-Path $runtimeDirectory 'processes.json'
        $records = @()
        if (Test-Path -LiteralPath $processFile) { $records += @(Get-Content -LiteralPath $processFile -Raw | ConvertFrom-Json) }
        $records += @($started | ForEach-Object { [pscustomobject]@{ Id=$_.Id; StartTicks=$_.StartTime.ToUniversalTime().Ticks.ToString() } })
        $records | ConvertTo-Json | Set-Content $processFile -Encoding utf8
    }
} catch {
    foreach ($process in $started) { if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force } }
    throw
}
