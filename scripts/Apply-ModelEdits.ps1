# Apply-ModelEdits.ps1 - review and apply the copy edits another assistant made through a launcher's edit mode
# (Invoke-ClaudeStage.ps1, Invoke-CodexStage.ps1 or Invoke-AntigravityStage.ps1 with -EditPaths). Each such run
# leaves a run folder with edit-manifest.json and a work\ copy; the real files are untouched until the coordinator
# reads the diff and runs this with -Apply. See Tool Conventions.md, "Calling another assistant as a stage". ASCII only.
#
# Default (no -Apply): writes <run>\changes.patch and prints a summary; changes nothing in the project.
# -Apply: for each edited file (or only -Files), refuses a protected path, a path not in canonical form or not
# spelled as on disk (8.3 short name, other case), a deleted copy, a copy that is or sits under a link, an original
# behind a link and an original that changed since the copy was made; refuses a run whose launcher exit was
# not 0 unless -Force -Reason '<why>'; backs the original up to backups\model-edits\apply-<stamp>\<rel> (SHA-256
# checked), rechecks the original just before writing, copies the edited file over, verifies it, and records each
# file in <run>\applied.json.
# Exit codes: 0 ok, 3 precondition failed, 4 an original changed since the copy (nothing more applied).

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RunDir,
    [switch]$Apply,
    [string[]]$Files = @(),
    # apply a run whose launcher exit was not 0 (for example a refused stray write); the reason goes in applied.json
    [switch]$Force,
    [string]$Reason = ''
)

$ErrorActionPreference = 'Stop'
$Project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$Utf8 = New-Object System.Text.UTF8Encoding($false)
# Run folders the launchers create; a run folder anywhere else is refused.
$RunRoots = @('claude', 'codex', 'antigravity') | ForEach-Object { (Join-Path $Project "outputs\stage-records\$_").TrimEnd('\') + '\' }
# The same protected list as the three launchers.
$Protected = @(
    '(^|/)\.git(/|$)', '^\.gitignore$', '^\.gitattributes$', '^\.claude(/|$)', '^\.codex(/|$)', '^\.agents(/|$)', '^skills(/|$)',
    '(^|/)agents\.md$', '(^|/)claude\.md$', '(^|/)gemini\.md$', '^lessons\.md$', '^lessons(/|$)', '^tool conventions\.md$',
    '^project startup\.md$', '^file workflow\.md$', '^shaka ethics\.md$', '(^|/)[^/]*paraphrasing rules\.md$',
    '(^|/)file-workflow-state[^/]*\.json$', '^scripts(/|$)', '^tools/antigravity(/|$)', '^tools/lilith/outward-tools[^/]*$',
    '^inputs(/|$)', '^archive(/|$)', '^backups(/|$)', '^outputs/stage-records(/|$)', '^outputs/shaka(/|$)',
    '(^|/)config[^/]*\.json$', '(^|/)state(/|$)', '\.dpapi$', '(^|/)[^/]*settings[^/]*\.json$',
    '(^|/)\.env', '\.(key|pem|pfx|env)$', '(^|/)auth\.json$', '(^|/)[^/]*(token|secret)[^/]*$', '(^|/)[^/]*credential'
)

function Fail([int]$code, [string]$msg) { [Console]::Out.WriteLine("APPLY-EDITS ERROR: $msg"); exit $code }
function Get-Sha([string]$p) { if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { return 'absent' }; (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash }
function Test-Under([string]$p, [string[]]$roots) { foreach ($r in $roots) { if ($p.StartsWith($r, [StringComparison]::OrdinalIgnoreCase)) { return $true } }; $false }
function Test-Link([string]$p) { (Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) }
function Test-OnDiskName([string]$full, [string]$base) {
    # every segment of $full below $base must be spelled as on disk (long name, same case), so an 8.3 short name
    # (TOOLCO~1.MD) or a case variant cannot slip past the protected list; a segment that does not exist yet must
    # not look like a short name (~ and a digit). Get-Item alone is not enough: it keeps the case as typed.
    $root = $base.TrimEnd('\') + '\'
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return $false }
    $cur = $root
    foreach ($seg in $full.Substring($root.Length).Split('\')) {
        $next = Join-Path $cur $seg
        if (Test-Path -LiteralPath $next) {
            $names = @([IO.Directory]::GetFileSystemEntries($cur, $seg) | ForEach-Object { [IO.Path]::GetFileName($_) })
            if ($names -cnotcontains $seg) { return $false }
        } elseif ($seg -match '~\d') { return $false }
        $cur = $next
    }
    return $true
}

$gitCmd = Get-Command git -ErrorAction SilentlyContinue
$Git = if ($gitCmd) { $gitCmd.Source } else { 'C:\Program Files\Git\cmd\git.exe' }
if (-not (Test-Path -LiteralPath $Git -PathType Leaf)) { Fail 3 'git not found; it writes the diff' }

$run = [IO.Path]::GetFullPath($RunDir).TrimEnd('\')
if (-not (Test-Under ($run + '\') $RunRoots)) { Fail 3 "run folder is not under a launcher's run root (outputs\stage-records\<launcher>\): $run" }
$manifestPath = Join-Path $run 'edit-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { Fail 3 "no edit-manifest.json in $run" }
try { $m = [IO.File]::ReadAllText($manifestPath, $Utf8) | ConvertFrom-Json } catch { Fail 3 'edit-manifest.json is not valid JSON' }
$work = [IO.Path]::GetFullPath([string]$m.workDir).TrimEnd('\')
# the copy must sit in this run folder
if (-not $work.StartsWith($run + '\', [StringComparison]::OrdinalIgnoreCase)) { Fail 3 "the manifest's work folder is not inside the run folder: $work" }

# only a clean run applies, unless -Force with a reason
$runJson = Join-Path $run 'run.json'
$launcherExit = $null; $runPaths = $null
if (Test-Path -LiteralPath $runJson -PathType Leaf) {
    $rj = [IO.File]::ReadAllText($runJson, $Utf8) | ConvertFrom-Json
    if ($null -ne $rj.launcherExit) { $launcherExit = [int]$rj.launcherExit }
    if ($null -ne $rj.editPaths) { $runPaths = @($rj.editPaths | ForEach-Object { [string]$_ }) }
}

$projRoot = $Project.TrimEnd('\') + '\'
$workRoot = $work + '\'
$Files = @($Files | ForEach-Object { $_ -split ';' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$edited = @()
$listed = @()
foreach ($f in @($m.files)) {
    # canonicalise first, then accept only the canonical form (no .., ./, backslashes, trailing dots or spaces)
    $raw = [string]$f.path
    if (-not $raw -or $raw -match '[:*?"<>|\\]' -or $raw.StartsWith('/') -or [IO.Path]::IsPathRooted($raw)) { Fail 3 "bad path in the manifest: $raw" }
    try {
        $orig = [IO.Path]::GetFullPath((Join-Path $Project ($raw.Replace('/', '\'))))
        $copy = [IO.Path]::GetFullPath((Join-Path $work ($raw.Replace('/', '\'))))
    } catch { Fail 3 "bad path in the manifest: $raw" }
    if (-not $orig.StartsWith($projRoot, [StringComparison]::OrdinalIgnoreCase)) { Fail 3 "manifest path leaves the project: $raw" }
    if (-not $copy.StartsWith($workRoot, [StringComparison]::OrdinalIgnoreCase)) { Fail 3 "manifest path leaves the work folder: $raw" }
    $rel = $orig.Substring($projRoot.Length).Replace('\', '/')
    if ($rel -cne $raw -or $copy.Substring($workRoot.Length).Replace('\', '/') -cne $raw) { Fail 3 "manifest path is not in canonical form: $raw" }
    $l = $rel.ToLowerInvariant()
    foreach ($pat in $Protected) { if ($l -match $pat) { Fail 3 "protected path in the manifest: $rel" } }
    if (-not (Test-OnDiskName $orig $Project) -or -not (Test-OnDiskName $copy $work)) { Fail 3 "manifest path is not spelled as on disk (an 8.3 short name or a different case): $rel" }
    $listed += $rel
    if ($Files.Count -and $Files -notcontains $rel) { continue }
    # the copy and every folder above it, up to and including the work folder, must not be a link
    $d = $copy
    while ($d.Length -ge $work.Length) { if (Test-Link $d) { Fail 3 "the copy is a link or sits under a linked folder: $rel" }; $d = Split-Path -Parent $d }
    $now = Get-Sha $copy
    if ($now -eq [string]$f.copySha256) { continue }
    if ($now -eq 'absent') { Fail 3 "copy missing (a deletion is not applied): $rel" }
    $d = $orig
    while ($d.Length -gt $projRoot.Length) { if (Test-Link $d) { Fail 3 "the original goes through a link: $rel" }; $d = Split-Path -Parent $d }
    $edited += [pscustomobject]@{ rel = $rel; copy = $copy; orig = $orig; origSha = [string]$f.originalSha256; newSha = $now }
}
# the manifest must list exactly the files the launcher recorded in run.json
if ($null -ne $runPaths) {
    $a = (@($listed | Sort-Object) -join '|'); $b = (@($runPaths | Sort-Object) -join '|')
    if ($a -cne $b) { Fail 3 'edit-manifest.json and run.json do not list the same files; nothing applied' }
}
if ($edited.Count -eq 0) { Write-Output 'APPLY-EDITS nothing to apply: no copy differs from the original'; exit 0 }

# The diff, always written, so it can be read before -Apply.
$patch = New-Object System.Text.StringBuilder
foreach ($e in $edited) {
    $a = if (Test-Path -LiteralPath $e.orig) { $e.orig } else { '/dev/null' }
    $d = & $Git -c core.quotepath=false diff --no-index --no-color -- $a $e.copy 2>$null
    [void]$patch.AppendLine(($d -join "`n"))
}
$patchPath = Join-Path $run 'changes.patch'
[IO.File]::WriteAllText($patchPath, $patch.ToString(), $Utf8)

$stale = @($edited | Where-Object { (Get-Sha $_.orig) -ne $_.origSha })
foreach ($e in $edited) {
    $flag = if ($stale -contains $e) { ' (ORIGINAL CHANGED SINCE THE COPY)' } else { '' }
    Write-Output "APPLY-EDITS edited: $($e.rel)$flag"
}
Write-Output "APPLY-EDITS diff: $patchPath"
if (-not $Apply) { Write-Output 'APPLY-EDITS dry run: nothing changed; read the diff, then rerun with -Apply'; exit 0 }
if ($stale.Count) { Fail 4 'an original changed after the copy was made; nothing applied (rerun the stage on the current file)' }
if ($launcherExit -ne 0) {
    if (-not $Force -or -not $Reason) { Fail 3 "the run did not end cleanly (launcher exit $launcherExit); nothing applied. Use -Force -Reason '<why>' only after reading its record" }
    Write-Output "APPLY-EDITS forced over launcher exit $launcherExit : $Reason"
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $Project "backups\model-edits\apply-$stamp"
$applied = @()
foreach ($e in $edited) {
    if (Test-Path -LiteralPath $e.orig) {
        $b = Join-Path $backup ($e.rel.Replace('/', '\'))
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $b) | Out-Null
        Copy-Item -LiteralPath $e.orig -Destination $b -Force
        if ((Get-Sha $b) -ne $e.origSha) { Fail 3 "backup check failed for $($e.rel); stopped (files already applied are in applied.json)" }
    } else {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $e.orig) | Out-Null
    }
    # recheck just before the write
    if ((Get-Sha $e.orig) -ne $e.origSha) { Fail 4 "$($e.rel) changed during the apply; stopped (files already applied are in applied.json)" }
    Copy-Item -LiteralPath $e.copy -Destination $e.orig -Force
    if ((Get-Sha $e.orig) -ne $e.newSha) { Fail 3 "apply check failed for $($e.rel)" }
    $applied += [ordered]@{ path = $e.rel; before = $e.origSha; after = $e.newSha }
    Write-Output "APPLY-EDITS applied: $($e.rel)"
    [IO.File]::WriteAllText((Join-Path $run 'applied.json'), (([ordered]@{ applied = (Get-Date).ToString('o'); backup = $backup; launcherExit = $launcherExit; forcedReason = $(if ($Force) { $Reason } else { $null }); files = $applied }) | ConvertTo-Json -Depth 4), $Utf8)
}
Write-Output "APPLY-EDITS backup: $backup"
exit 0
