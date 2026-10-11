$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$localDirectory = Join-Path $repoRoot '.local'
$downloadDirectory = Join-Path $localDirectory 'downloads'
New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
$archive = Join-Path $downloadDirectory 'elasticsearch-8.9.2-windows-x86_64.zip'
$downloadUrl = 'https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-8.9.2-windows-x86_64.zip'
if (-not (Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $downloadUrl -OutFile $archive }
$checksumFile = "$archive.sha512"
Invoke-WebRequest -Uri "$downloadUrl.sha512" -OutFile $checksumFile
$expectedHash = ((Get-Content -LiteralPath $checksumFile -Raw) -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash
if ($actualHash -ne $expectedHash) { throw 'Elasticsearch archive checksum mismatch.' }
$installationDirectory = Join-Path $localDirectory 'elasticsearch-8.9.2'
if (-not (Test-Path -LiteralPath $installationDirectory)) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($archive, $localDirectory)
}
@'
cluster.name: insurance-lab
node.name: insurance-lab-node
network.host: 127.0.0.1
http.port: 9200
discovery.type: single-node
xpack.security.enabled: false
xpack.ml.enabled: false
'@ | Set-Content -LiteralPath (Join-Path $installationDirectory 'config/elasticsearch.yml') -Encoding ascii
$env:ES_JAVA_OPTS = '-Xms512m -Xmx512m'
& (Join-Path $installationDirectory 'bin/elasticsearch-service.bat') install
if ($LASTEXITCODE -ne 0) { throw 'Elasticsearch service installation failed.' }
& (Join-Path $installationDirectory 'bin/elasticsearch-service.bat') start
if ($LASTEXITCODE -ne 0) { throw 'Elasticsearch service startup failed.' }
Write-Host "Installed Elasticsearch: $installationDirectory"
