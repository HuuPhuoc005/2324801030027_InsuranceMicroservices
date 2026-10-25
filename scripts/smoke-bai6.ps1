$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$gateway = 'http://localhost:8099'
function Post-Json([string]$Path, $Body, $Headers = @{}) {
    Invoke-RestMethod "$gateway$Path" -Method Post -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 8) -Headers $Headers -TimeoutSec 15
}
function Expect-Status([string]$Path, [string]$Method, [string]$Body, [int]$Expected) {
    $status = 0
    try { Invoke-WebRequest -UseBasicParsing "$gateway$Path" -Method $Method -ContentType 'application/json' -Body $Body -TimeoutSec 15 | Out-Null } catch { $status=[int]$_.Exception.Response.StatusCode }
    if ($status -ne $Expected) { throw "$Path expected $Expected, received $status" }
}
$summary = @()
Expect-Status '/api/User' 'Post' '{"login":"unknown.agent","password":"wrong"}' 400
$summary += 'Gateway -> AuthService: invalid login HTTP 400'
$login = Post-Json '/api/User' @{login='jimmy.solid';password='secret'}
if (-not $login.token) { throw 'Login did not issue a token.' }
$headers = @{Authorization="Bearer $($login.token)"}
$profile = Invoke-RestMethod "$gateway/api/User" -Headers $headers -TimeoutSec 15
if ($profile.login -ne 'jimmy.solid' -or $null -ne $profile.password) { throw 'Unexpected profile response.' }
$summary += 'Gateway -> AuthService: JWT login and protected profile HTTP 200; password omitted'
Expect-Status '/api/Dashboard/total-sales' 'Post' '{}' 401
$summary += 'Gateway rejects unauthenticated Dashboard request: HTTP 401'
$query = @{salesDateFrom=(Get-Date).AddYears(-2).ToString('yyyy-MM-dd');salesDateTo=(Get-Date).AddDays(1).ToString('yyyy-MM-dd')}
$direct = Invoke-RestMethod 'http://localhost:5035/api/Dashboard/total-sales' -Method Post -ContentType 'application/json' -Body ($query | ConvertTo-Json) -TimeoutSec 15
$sales = Post-Json '/api/Dashboard/total-sales' $query $headers
if ($sales.total.policiesCount -le 0 -or $sales.total.policiesCount -ne $direct.total.policiesCount -or $sales.total.premiumAmount -ne $direct.total.premiumAmount) { throw 'Gateway statistics differ from the Elasticsearch backend.' }
$summary += "Gateway -> Dashboard -> Elasticsearch: HTTP 200, $($sales.total.policiesCount) policies; totals match direct API"
$filtered = Post-Json '/api/Dashboard/total-sales' ($query + @{productCode='NO_SUCH_PRODUCT'}) $headers
if ($filtered.total.policiesCount -ne 0) { throw 'Dashboard POST incorrectly reused cached statistics.' }
$summary += 'Dashboard POST with a different body returns independent results'
$product = Invoke-RestMethod "$gateway/Products/health" -Headers $headers -TimeoutSec 15
if ($product.service -ne 'ProductService') { throw 'Product route alias did not resolve through Eureka.' }
$summary += 'Gateway /Products/health -> Eureka -> ProductService: HTTP 200'
Expect-Status '/agentsChat/negotiate?negotiateVersion=1' 'Post' '' 401
$negotiation = Post-Json '/agentsChat/negotiate?negotiateVersion=1' @{} $headers
if (-not $negotiation.connectionToken) { throw 'SignalR negotiation did not return a connection token.' }
$summary += 'Gateway -> Chat SignalR: anonymous HTTP 401, authenticated negotiation HTTP 200'
Post-Json '/api/Notification' @{message='Bai 6 smoke notification'} $headers | Out-Null
$summary += 'Gateway -> Chat notification command: HTTP 200'
$summary | Tee-Object (Join-Path $repoRoot 'artifacts/bai6-gateway-check.log')
