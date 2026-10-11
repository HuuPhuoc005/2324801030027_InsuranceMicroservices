$ErrorActionPreference = 'Stop'
docker compose -f (Join-Path $PSScriptRoot 'infra.yml') up -d --build --wait --wait-timeout 180
if ($LASTEXITCODE -ne 0) { throw 'Infrastructure startup failed.' }
docker exec postgres psql -U postgres -d postgres -c '\l'
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL database check failed.' }
