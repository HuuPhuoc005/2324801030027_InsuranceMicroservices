param([string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'backups'), [switch]$Native, [string]$PostgresBin = 'C:\Program Files\PostgreSQL\16\bin')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$databases = 'lab_netmicro_payments', 'lab_netmicro_jobs', 'lab_netmicro_policy', 'lab_netmicro_pricing'
foreach ($database in $databases) {
    if ($Native) {
        $env:PGPASSWORD = 'lab_pass'
        try {
            $backupPath = Join-Path $OutputDirectory "$database.backup"
            & (Join-Path $PostgresBin 'pg_dump.exe') -h localhost -U postgres -Fc -f $backupPath $database
            if ($LASTEXITCODE -ne 0) { throw "Backup failed: $database" }
            & (Join-Path $PostgresBin 'pg_restore.exe') --list $backupPath
            if ($LASTEXITCODE -ne 0) { throw "Invalid backup: $database" }
        } finally { Remove-Item Env:PGPASSWORD }
        continue
    }
    docker exec postgres pg_dump -U postgres -Fc -f "/tmp/$database.backup" $database
    if ($LASTEXITCODE -ne 0) { throw "Backup failed: $database" }
    docker cp "postgres:/tmp/$database.backup" (Join-Path $OutputDirectory "$database.backup")
    if ($LASTEXITCODE -ne 0) { throw "Could not copy backup: $database" }
    docker exec postgres pg_restore --list "/tmp/$database.backup"
    if ($LASTEXITCODE -ne 0) { throw "Invalid backup: $database" }
}
Write-Host "Verified PostgreSQL backups: $OutputDirectory"
