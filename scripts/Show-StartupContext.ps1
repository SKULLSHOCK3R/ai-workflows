param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    # Claude selects the J.A.R.V.I.S. persona; default mode selects the E.V. persona.
    [switch]$Claude
)

# Prints the core project rule files so an assistant starts every session with them.
# Default (Codex) mode prints the common core and the E.V. persona in full; keep it unchanged.
# -Claude (SessionStart hook in .claude/settings.json) verifies the files and the CLAUDE.md @imports
# and prints only a short manifest and the J.A.R.V.I.S. per-reply card: Claude Code cuts hook output
# over 10,000 characters to a short preview (see lessons.md), so keep -Claude output under 9,000.
# Codex or a person can run it manually: powershell -File scripts/Show-StartupContext.ps1
# Read-only: never writes, moves, or deletes anything.
# Size alarm (both modes): one maintenance-only WARNING line per file over its byte budget in $sizeBudgets.
# Change budgets only in that table. Keep this file ASCII only.

try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

$core = @('Project Startup.md', 'lessons.md', 'Tool Conventions.md', 'Shaka Ethics.md')
if ($Claude) { $core += 'JARVIS Paraphrasing Rules.md' } else { $core += 'EV Paraphrasing Rules.md' }
$delegation = if ($Claude) { '.claude/skills/jarvis-work-delegation/SKILL.md (your optional delegation policy for substantial tasks)' } else { 'Skills/ev-work-delegation/SKILL.md (your optional delegation policy for substantial tasks)' }
$onDemand = @(
    'File Workflow.md (before handling files in inputs)',
    'lessons/<Area> Lessons.md (area-specific lessons; see lessons/Example Area Lessons.md)',
    $delegation
)
# Byte budgets (1 KB = 1024). A file over budget prints a maintenance notice only; it is never a startup error.
$sizeBudgets = [ordered]@{
    'AGENTS.md'               = 18432
    'lessons.md'              = 24576
    'Tool Conventions.md'     = 14336
    'Project Startup.md'      = 14336
    'Shaka Ethics.md'         = 8192
    'lessons/Example Area Lessons.md' = 16384
}

Write-Output '=== STARTUP CONTEXT: checking core project rules ==='

$contents = @{}
$problems = @()
foreach ($name in $core) {
    $path = Join-Path $Root $name
    try {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf -ErrorAction Stop)) {
            throw 'missing or not a file'
        }
        # Read fully before printing a BEGIN marker, so an interrupted read cannot look complete.
        $contents[$name] = Get-Content -LiteralPath $path -Raw -Encoding UTF8 -ErrorAction Stop
    } catch {
        $problems += "Cannot read ${path}: $($_.Exception.Message)"
    }
}

$hashes = @{}
foreach ($name in @('AGENTS.md', 'CLAUDE.md')) {
    $path = Join-Path $Root $name
    try {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf -ErrorAction Stop)) {
            throw 'missing or not a file'
        }
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256 -ErrorAction Stop).Hash
        if ([string]::IsNullOrWhiteSpace($hash)) {
            throw 'SHA-256 hash was empty'
        }
        $hashes[$name] = $hash
    } catch {
        $problems += "Cannot verify ${path}: $($_.Exception.Message)"
    }
}

if ($hashes.ContainsKey('AGENTS.md') -and $hashes.ContainsKey('CLAUDE.md')) {
    if ($hashes['AGENTS.md'] -ne $hashes['CLAUDE.md']) {
        $problems += "Rulebook drift: $(Join-Path $Root 'AGENTS.md') and $(Join-Path $Root 'CLAUDE.md') differ. Review before relying on CLAUDE.md."
    }
}

if ($Claude) {
    # Claude loads the core files through CLAUDE.md @imports (AGENTS.md, Required reading); confirm they are present.
    $imports = @('@lessons.md', '@Tool\ Conventions.md', '@Project\ Startup.md', '@Shaka\ Ethics.md', '@JARVIS\ Paraphrasing\ Rules.md')
    $claudePath = Join-Path $Root 'CLAUDE.md'
    try {
        $claudeText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $claudePath -ErrorAction Stop).ProviderPath, (New-Object System.Text.UTF8Encoding($false)))
        foreach ($imp in $imports) {
            if ($claudeText.IndexOf($imp, [StringComparison]::Ordinal) -lt 0) {
                $problems += "CLAUDE.md is missing the import $imp (Required reading)."
            }
        }
        if ($claudeText.IndexOf('@EV\ Paraphrasing', [StringComparison]::Ordinal) -ge 0) {
            $problems += 'CLAUDE.md imports EV Paraphrasing Rules.md; Claude must never load it.'
        }
    } catch {
        $problems += "Cannot read ${claudePath}: $($_.Exception.Message)"
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Output "ERROR: $_" }
    Write-Output 'Startup context could not be verified. Tell the user before starting work.'
    if ($Claude) { exit 0 }
    exit 1
}

$sizeNotices = @()
foreach ($name in $sizeBudgets.Keys) {
    try {
        $item = Get-Item -LiteralPath (Join-Path $Root $name) -ErrorAction Stop
        if ($item.Length -gt $sizeBudgets[$name]) {
            $sizeNotices += [string]::Format([Globalization.CultureInfo]::InvariantCulture, 'WARNING (maintenance notice, not a startup error): {0} is {1:N0} bytes, over its {2:N0}-byte budget. No action is needed in this session; do not trim it unless the user asks. Consider merging or trimming it when convenient.', $name, $item.Length, $sizeBudgets[$name])
        }
    } catch {}
}

if ($Claude) {
    $out = New-Object System.Collections.Generic.List[string]
    $out.Add('CLAUDE.md and AGENTS.md match; all core project rules were read successfully.')
    foreach ($n in $sizeNotices) { $out.Add($n) }
    $out.Add('These core files load in full through the @imports under Required reading in CLAUDE.md. If any is missing from your context, open it in full before working:')
    foreach ($name in $core) { $out.Add("- $name") }
    $out.Add('')
    $cardScript = Join-Path $PSScriptRoot 'Show-PersonaCard.ps1'
    if (Test-Path -LiteralPath $cardScript -PathType Leaf) {
        foreach ($line in @(& $cardScript -Root $Root)) { $out.Add([string]$line) }
    } else {
        # Stderr is hidden from Claude on exit 0, so report a missing card script on stdout.
        $out.Add("WARNING: ${cardScript} is missing, so the per-reply card was not printed. Open JARVIS Paraphrasing Rules.md in full before replying, and tell the user the card hook needs attention.")
    }
    $out.Add('The same card is injected with every user message by Show-PersonaCard.ps1.')
    $out.Add('')
    $out.Add('Open when relevant (not preloaded):')
    foreach ($item in $onDemand) { $out.Add("- $item") }
    $out.Add('=== END STARTUP CONTEXT ===')
    $total = ($out -join "`n").Length
    if ($total -gt 9000) {
        Write-Output "WARNING: startup context is $total characters, close to the 10,000-character hook cap; shorten it."
    }
    $out | ForEach-Object { Write-Output $_ }
    exit 0
}

Write-Output 'CLAUDE.md and AGENTS.md match; all core project rules were read successfully.'
$sizeNotices | ForEach-Object { Write-Output $_ }
Write-Output "The files below are the rulebook's required companions."
Write-Output ''
foreach ($name in $core) {
    Write-Output "----- BEGIN $name -----"
    Write-Output $contents[$name]
    Write-Output "----- END $name -----"
    Write-Output ''
}

Write-Output 'Open when relevant (not preloaded):'
$onDemand | ForEach-Object { Write-Output "- $_" }
Write-Output '=== END STARTUP CONTEXT ==='
exit 0
