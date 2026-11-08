param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

# Prints the "## Per-reply card" section of JARVIS Paraphrasing Rules.md (the single source of truth) with a chat's
# first message and at session start, and its shorter "## Short card" section with every later message of that chat.
# Claude runs it with every user message through a UserPromptSubmit hook (.claude/settings.json);
# Show-StartupContext.ps1 -Claude reuses it at session start (no hook input, so always the full card).
# The only thing it writes is one empty marker per chat, state/persona-card/<session_id> (state/ is git-ignored), so
# the next message of that chat knows it is a follow-up. Fails safe: unreadable input, an unusual session id or any
# error prints the full card. Never moves or deletes anything. Always exits 0: exit 2 would block the user's prompt,
# and other codes keep the output from Claude. Problems print a WARNING line instead.
# Keep output well under 10,000 characters: Claude Code cuts longer hook output to a short preview.

try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch {}

$heading = '## Per-reply card'
$shortHeading = '## Short card'
$followUp = $false
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

# Follow-up check. Only a UserPromptSubmit hook input counts; the startup call has none and gets the full card.
try {
    if ([Console]::IsInputRedirected) {
        # Bounded read: an open stdin that never closes (a terminal tool, an IDE) must not hang the hook.
        $stdin = New-Object System.IO.StreamReader([Console]::OpenStandardInput(), (New-Object System.Text.UTF8Encoding($false)))
        $readTask = $stdin.ReadToEndAsync()
        $raw = if ($readTask.Wait(3000)) { $readTask.Result } else { $null }
        if ($raw -and $raw.Length -lt 50000000) {
            Add-Type -AssemblyName System.Web.Extensions -ErrorAction Stop
            $ser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
            $ser.MaxJsonLength = [int]::MaxValue
            $obj = $ser.DeserializeObject($raw)
            if ($obj -is [System.Collections.IDictionary] -and $obj.ContainsKey('hook_event_name') -and
                [string]$obj['hook_event_name'] -ceq 'UserPromptSubmit' -and $obj.ContainsKey('session_id')) {
                $session = [string]$obj['session_id']
                # Session ids are UUIDs; anything else is never used as a file name.
                if ($obj['session_id'] -is [string] -and $session -cmatch '^[A-Za-z0-9_-]{1,80}\z') {
                    $markerDir = Join-Path (Join-Path $rootPath 'state') 'persona-card'
                    $marker = Join-Path $markerDir $session
                    if (Test-Path -LiteralPath $marker -PathType Leaf) {
                        $followUp = $true
                    } else {
                        if (-not (Test-Path -LiteralPath $markerDir -PathType Container)) { New-Item -ItemType Directory -Path $markerDir -Force | Out-Null }
                        [IO.File]::WriteAllText($marker, '')
                    }
                }
            }
        }
    }
} catch { $followUp = $false }

$lines = $text -split "\r?\n"
$label = 'PER-REPLY CARD'
if ($followUp) {
    # Fall back to the full card if the Short card section is missing.
    foreach ($l in $lines) { if ($l.TrimEnd() -ceq $shortHeading) { $heading = $shortHeading; $label = 'SHORT CARD'; break } }
}
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

Write-Output "=== J.A.R.V.I.S. $label (from JARVIS Paraphrasing Rules.md; apply to this reply) ==="
Write-Output $body
Write-Output '=== END CARD ==='
exit 0
