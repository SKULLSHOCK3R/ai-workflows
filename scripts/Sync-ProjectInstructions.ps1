param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [switch]$Sync,
    [switch]$ReviewedDifference
)

$ErrorActionPreference = 'Stop'
$agents = Join-Path $Root 'AGENTS.md'
$claude = Join-Path $Root 'CLAUDE.md'
if (-not (Test-Path -LiteralPath $agents -PathType Leaf)) { throw "Missing canonical file: $agents" }

function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

$sourceHash = Get-FileHashValue $agents
$hasTarget = Test-Path -LiteralPath $claude -PathType Leaf
$targetHash = if ($hasTarget) { Get-FileHashValue $claude } else { $null }

if (-not $Sync) {
    if ($targetHash -eq $sourceHash) {
        Write-Output "Equal: AGENTS.md and CLAUDE.md (SHA-256 $sourceHash)"
        exit 0
    }
    Write-Output 'Instruction copies differ or CLAUDE.md is missing. Review the difference before synchronization.'
    exit 1
}

if ($targetHash -eq $sourceHash) {
    Write-Output "Already equal (SHA-256 $sourceHash)"
    exit 0
}
if ($hasTarget -and -not $ReviewedDifference) {
    throw 'CLAUDE.md differs. Review its contents, then rerun with -Sync -ReviewedDifference.'
}
if ($hasTarget) {
    # Backups live in backups\<group>\<task>-<stamp>, never in the project root or at the top of backups.
    $backupDir = Join-Path $Root ('backups\sync\claude-md-{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), [guid]::NewGuid().ToString('N'))
    if (-not (Test-Path -LiteralPath $backupDir -PathType Container)) { New-Item -ItemType Directory -Path $backupDir -ErrorAction Stop | Out-Null }
    $backup = Join-Path $backupDir 'CLAUDE.md'
    Copy-Item -LiteralPath $claude -Destination $backup -ErrorAction Stop
    if ((Get-FileHashValue $backup) -ne $targetHash) { throw "Backup verification failed: $backup" }
    Write-Output "Backed up differing CLAUDE.md to $backup"
}
Copy-Item -LiteralPath $agents -Destination $claude -Force -ErrorAction Stop
if ((Get-FileHashValue $claude) -ne $sourceHash) { throw 'Synchronization hash verification failed.' }
Write-Output "Synchronized CLAUDE.md (SHA-256 $sourceHash)"
