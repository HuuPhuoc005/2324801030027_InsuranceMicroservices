param([Parameter(Mandatory)][string]$BackupFile, [Parameter(Mandatory)][string]$Database)
$ErrorActionPreference = 'Stop'
if ($Database -notin @('lab_netmicro_payments','lab_netmicro_jobs','lab_netmicro_policy','lab_netmicro_pricing')) { throw 'Unknown lab database.' }
$source = (Resolve-Path -LiteralPath $BackupFile).Path
docker cp $source 'postgres:/tmp/restore.backup'
if ($LASTEXITCODE -ne 0) { throw 'Copy failed.' }
docker exec postgres pg_restore -U postgres -d $Database --no-owner --no-acl --exit-on-error /tmp/restore.backup
if ($LASTEXITCODE -ne 0) { throw 'Restore failed. Use an empty database to avoid conflicts.' }
