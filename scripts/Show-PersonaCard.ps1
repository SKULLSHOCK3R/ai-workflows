param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

# Prints only the "## Per-reply card" section of JARVIS Paraphrasing Rules.md (the single source of truth).
# Claude runs it with every user message through a UserPromptSubmit hook (.claude/settings.json);
# Show-StartupContext.ps1 -Claude reuses it at session start.
# Read-only: never writes, moves, or deletes anything. Always exits 0: exit 2 would block the user's
# prompt, and other codes keep the output from Claude. Problems print a WARNING line instead.
# Keep output well under 10,000 characters: Claude Code cuts longer hook output to a short preview.

try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch {}

$heading = '## Per-reply card'
$maxChars = 4000

function Write-CardWarning([string]$Reason) {
    Write-Output "WARNING: J.A.R.V.I.S. per-reply card unavailable ($Reason). Open JARVIS Paraphrasing Rules.md in full before replying, and tell the user the card hook needs attention."
}

try {
    # .NET resolves relative paths against the process folder, so make Root absolute first.
    $rootPath = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).ProviderPath
    $path = Join-Path $rootPath 'JARVIS Paraphrasing Rules.md'
    $text = [IO.File]::ReadAllText($path, (New-Object System.Text.UTF8Encoding($false)))
} catch {
    Write-CardWarning "cannot read the persona file: $($_.Exception.Message)"
    exit 0
}

$lines = $text -split "\r?\n"
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].TrimEnd() -ceq $heading) { $start = $i; break }
}
if ($start -lt 0) { Write-CardWarning "heading '$heading' not found"; exit 0 }

$end = $lines.Count
for ($j = $start + 1; $j -lt $lines.Count; $j++) {
    if ($lines[$j] -match '^#{1,2}\s') { $end = $j; break }
}
# Guard: a PowerShell range counts downwards when end < start.
if ($end -le $start + 1) { Write-CardWarning 'section is empty'; exit 0 }
$body = ($lines[($start + 1)..($end - 1)] -join "`n").Trim()
if ([string]::IsNullOrWhiteSpace($body)) { Write-CardWarning 'section is empty'; exit 0 }
if ($body.Length -gt $maxChars) {
    Write-CardWarning "section is $($body.Length) characters, over the $maxChars limit; printing the first $maxChars"
    $body = $body.Substring(0, $maxChars)
}

Write-Output '=== J.A.R.V.I.S. PER-REPLY CARD (from JARVIS Paraphrasing Rules.md; apply to this reply) ==='
Write-Output $body
Write-Output '=== END CARD ==='
exit 0
