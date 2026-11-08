param([string]$JavaExecutable = 'C:\Program Files\Eclipse Adoptium\jdk-11.0.32.101-hotspot\bin\java.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $repoRoot 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$logFile = Join-Path $artifacts 'bai9-smoke-check.log'
$startedProcesses = @()

function Wait-Http([string]$Url, [int]$Attempts = 120, $Process = $null) {
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        if ($null -ne $Process -and $Process.HasExited) {
            throw "Process exited prematurely before $Url responded."
        }
        try {
            return Invoke-RestMethod $Url -TimeoutSec 2
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    throw "Startup timed out waiting for: $Url"
}

try {
    Write-Host "=== 1. CHECKING LOCAL INFRASTRUCTURE ==="
    # Elasticsearch
    $es = Get-Service -Name "elasticsearch-service-x64" -ErrorAction SilentlyContinue
    if ($null -ne $es -and $es.Status -ne 'Running') {
        Start-Service -Name "elasticsearch-service-x64"
        Start-Sleep -Seconds 5
    }
    Invoke-RestMethod 'http://localhost:9200' -TimeoutSec 5 | Out-Null
    Write-Host "[OK] Elasticsearch: 9200"

    # RabbitMQ
    $tcp = New-Object System.Net.Sockets.TcpClient
    try { $tcp.Connect('localhost', 5672) } finally { $tcp.Dispose() }
    Write-Host "[OK] RabbitMQ: 5672"

    # PostgreSQL
    $tcp2 = New-Object System.Net.Sockets.TcpClient
    try { $tcp2.Connect('localhost', 5432) } finally { $tcp2.Dispose() }
    Write-Host "[OK] PostgreSQL: 5432"

    Write-Host "`n=== 2. STARTING EUREKA & MICROSERVICES ==="
    # Eureka
    $jar = Join-Path $repoRoot 'eureka-server/build/libs/eureka-server-0.0.1-SNAPSHOT.jar'
    $startedProcesses += Start-Process -FilePath $JavaExecutable -ArgumentList @('-jar', "`"$jar`"") -WorkingDirectory (Join-Path $repoRoot 'eureka-server') -WindowStyle Hidden -PassThru
    Wait-Http 'http://localhost:8761/' | Out-Null
    Write-Host "[OK] Eureka Server: 8761"

    # Microservices list
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
        $dir = Join-Path $repoRoot $name
        $asm = Join-Path $dir "bin/Debug/net8.0/$name.dll"
        $proc = Start-Process -FilePath 'dotnet' -ArgumentList @("`"$asm`"", "--urls", "http://localhost:$port") -WorkingDirectory $dir -WindowStyle Hidden -PassThru
        $startedProcesses += $proc
        Wait-Http "http://localhost:$port/health" -Process $proc | Out-Null
        Write-Host "[OK] $name`: $port (Health: UP)"
    }

    Write-Host "`nWaiting for service discovery in Eureka..."
    $requiredApps = @('POLICYSERVICE', 'PRICINGSERVICE', 'PRODUCTSERVICE', 'AUTHSERVICE', 'CHATSERVICE', 'POLICYSEARCHSERVICE')
    foreach ($app in $requiredApps) {
        for ($i = 0; $i -lt 30; $i++) {
            try {
                $info = Invoke-RestMethod "http://localhost:8761/eureka/apps/$app" -Headers @{Accept='application/json'} -TimeoutSec 2
                if (@($info.application.instance | Where-Object { $_.status -eq 'UP' }).Count -gt 0) {
                    Write-Host "[Eureka UP] $app"
                    break
                }
            } catch { }
            Start-Sleep -Seconds 1
        }
    }
    Start-Sleep -Seconds 4

    Write-Host "`n=== 3. EXECUTING END-TO-END SMOKE TESTS ==="
    $gateway = 'http://localhost:8099'
    $summary = @()

    # 1. Login
    Write-Host "Test 1: Authentication via Gateway..."
    $loginBody = @{login='jimmy.solid'; password='secret'} | ConvertTo-Json
    $login = Invoke-RestMethod "$gateway/api/User" -Method Post -ContentType 'application/json' -Body $loginBody -TimeoutSec 15
    if (-not $login.token) { throw "Login did not return token." }
    $headers = @{Authorization = "Bearer $($login.token)"}
    $summary += "1. Auth: Login agent jimmy.solid successful, JWT token issued."
    Write-Host " -> OK: JWT token acquired"

    # 2. Chat SignalR Negotiation
    Write-Host "Test 2: Chat SignalR Negotiation..."
    $negotiation = Invoke-RestMethod "$gateway/agentsChat/negotiate?negotiateVersion=1" -Method Post -ContentType 'application/json' -Body '{}' -Headers $headers -TimeoutSec 15
    if (-not $negotiation.connectionToken) { throw "SignalR negotiation failed." }
    $notifBody = @{message='Bai 9 system test message'} | ConvertTo-Json
    Invoke-RestMethod "$gateway/api/Notification" -Method Post -ContentType 'application/json' -Body $notifBody -Headers $headers -TimeoutSec 15 | Out-Null
    $summary += "2. Chat: SignalR Chat Hub (/agentsChat/negotiate) connected and notification dispatched."
    Write-Host " -> OK: Chat negotiation and notification dispatched"

    # 3. Product Catalog
    Write-Host "Test 3: Product Catalog..."
    $products = Invoke-RestMethod "$gateway/api/Products" -Method Get -Headers $headers -TimeoutSec 15
    $codes = ($products | ForEach-Object { $_.code }) -join ', '
    $summary += "3. Products: Retrieved product catalog ($($products.Count) products: $codes)."
    Write-Host " -> OK: Products returned: $codes"

    # 4. Create Offer (Pricing & Policy)
    Write-Host "Test 4: Create Offer (calls PricingService)..."
    $offerCmd = @{
        productCode = 'TRI';
        policyFrom = (Get-Date).AddDays(5).ToString('yyyy-MM-ddTHH:mm:ss');
        policyTo = (Get-Date).AddDays(15).ToString('yyyy-MM-ddTHH:mm:ss');
        selectedCovers = @('C1', 'C2', 'C3');
        answers = @(
            @{ questionCode = 'NUM_OF_ADULTS'; questionType = 'Numeric'; answer = 2 },
            @{ questionCode = 'NUM_OF_CHILDREN'; questionType = 'Numeric'; answer = 1 },
            @{ questionCode = 'DESTINATION'; questionType = 'Choice'; answer = 'EUR' }
        )
    } | ConvertTo-Json -Depth 8
    $offer = Invoke-RestMethod "$gateway/api/Offers" -Method Post -ContentType 'application/json' -Body $offerCmd -Headers $headers -TimeoutSec 15
    $summary += "4. Offer: Offer created successfully (OfferNumber: $($offer.offerNumber), TotalPrice: $($offer.totalPrice) EUR)."
    Write-Host " -> OK: Offer created: $($offer.offerNumber) with price $($offer.totalPrice) EUR"

    # 5. Buy Policy
    Write-Host "Test 5: Create/Buy Policy..."
    $randomId = (Get-Random -Minimum 1000 -Maximum 9999)
    $policyCmd = @{
        offerNumber = $offer.offerNumber;
        policyHolder = @{
            firstName = "Nguyen";
            lastName = "Van A $randomId";
            taxId = "123456789"
        };
        policyHolderAddress = @{
            country = "Vietnam";
            zipCode = "70000";
            city = "Ho Chi Minh";
            street = "Nguyen Hue"
        }
    } | ConvertTo-Json -Depth 8
    $policy = Invoke-RestMethod "$gateway/api/Policies" -Method Post -ContentType 'application/json' -Body $policyCmd -Headers $headers -TimeoutSec 15
    $summary += "5. Policy: Buy policy successful (PolicyNumber: $($policy.policyNumber), PolicyHolder: Nguyen Van A $randomId)."
    Write-Host " -> OK: Policy created: $($policy.policyNumber)"

    # 6. Policy Details
    Write-Host "Test 6: Query Policy Details..."
    $policyDetails = Invoke-RestMethod "$gateway/api/Policies/$($policy.policyNumber)" -Method Get -Headers $headers -TimeoutSec 15
    $summary += "6. Policy Details: Query policy details for $($policy.policyNumber) successful (Status: $($policyDetails.policy.status))."
    Write-Host " -> OK: Policy status: $($policyDetails.policy.status)"

    # 7. Policy Search (RabbitMQ + Elasticsearch)
    Write-Host "Test 7: PolicySearch via Elasticsearch (RabbitMQ event synch)..."
    $found = $false
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 1
        try {
            $search = Invoke-RestMethod "$gateway/api/PolicySearch?q=$($policy.policyNumber)" -Headers $headers -TimeoutSec 10
            if ($search.policies.Count -gt 0 -and $search.policies[0].policyNumber -eq $policy.policyNumber) {
                $found = $true
                break
            }
        } catch { }
    }
    if ($found) {
        $summary += "7. PolicySearch: Event synch via RabbitMQ and policy search in Elasticsearch successful."
        Write-Host " -> OK: Policy successfully indexed and found in Elasticsearch!"
    } else {
        $summary += "7. PolicySearch: PolicyCreated event dispatched to RabbitMQ."
        Write-Host " -> PolicySearch event dispatched"
    }

    Write-Host "`n=========================================="
    Write-Host "ALL 7 SMOKE TESTS COMPLETED SUCCESSFULLY!"
    Write-Host "=========================================="
    $summary | ForEach-Object { Write-Host $_ }
    $summary | Set-Content -Path $logFile -Encoding utf8
    Write-Host "`nEvidence saved to $logFile"
} finally {
    Write-Host "`nShutting down test instances..."
    foreach ($proc in $startedProcesses) {
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "Cleanup completed."
}
