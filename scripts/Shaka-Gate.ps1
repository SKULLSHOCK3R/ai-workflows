# Shaka-Gate.ps1: PreToolUse hook for the Shaka tool gate (Shaka Ethics.md, "Enforced on the Claude Code side";
# Tool Conventions.md, "Tool gate"). Wire it in .claude/settings.json for the gated tools (see settings.example.json).
# Reads the hook JSON from stdin, appends one line "<ISO time>|<event>|<tool_name>|<mode>|<session_id>" to
# <root>\state\shaka-gate.log (git-ignored; rotated to .1 above 1 MB) and answers "deny", or "ask" only when the
# parsed tool_name starts with mcp__computer-use__ (computer control). It fails closed: unreadable or unparsed
# input, or any other tool, gets "deny". It never reads or logs tool_input. On any failure it still prints the
# decision and exits 0. Windows PowerShell 5.1, ASCII only; output stays far under the 10,000-character hook cap.
# The permissions.deny rules in settings.json are the hard gate; this hook is the evidence trail and a second
# layer. Ask is never the only guard: in one workspace on 2026-10-17 ask rules did not prompt in auto mode,
# so everything except computer control is deny.

$denyDecision = '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"Shaka: this tool sends, shares, spends, schedules, deletes or changes settings. It is blocked for every assistant; the user does it themselves."}}'
$askDecision = '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"ask","permissionDecisionReason":"Shaka: this tool controls the computer. The user''s yes in chat is needed."}}'
$decision = $denyDecision

function Clean([object]$v, [int]$max) {
    if ($null -eq $v) { return '' }
    $s = [string]$v
    $s = $s.Replace("`r", ' ').Replace("`n", ' ').Replace('|', '/')
    if ($s.Length -gt $max) { $s = $s.Substring(0, $max) }
    return $s
}

try {
    $hookEvent = 'PreToolUse'; $tool = ''; $mode = ''; $session = ''
    try {
        $raw = [Console]::In.ReadToEnd()
        if ($raw -and $raw.Length -lt 50000000) {
            Add-Type -AssemblyName System.Web.Extensions -ErrorAction Stop
            $ser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
            $ser.MaxJsonLength = [int]::MaxValue
            $obj = $ser.DeserializeObject($raw)
            if ($obj -is [System.Collections.IDictionary]) {
                if ($obj.ContainsKey('hook_event_name')) { $hookEvent = Clean $obj['hook_event_name'] 40 }
                if ($obj.ContainsKey('tool_name')) { $tool = Clean $obj['tool_name'] 200 }
                if ($obj.ContainsKey('permission_mode')) { $mode = Clean $obj['permission_mode'] 40 }
                if ($obj.ContainsKey('session_id')) { $session = Clean $obj['session_id'] 80 }
            }
        }
    } catch {
        if (-not $tool) { $tool = 'unparsed' }
    }

    if ($tool -and $tool.StartsWith('mcp__computer-use__', [StringComparison]::Ordinal)) { $decision = $askDecision }

    try {
        # The project root is the folder above scripts/. If it cannot be found, skip logging; the decision still stands.
        if ($PSScriptRoot) {
            $root = Split-Path -Parent $PSScriptRoot
            $dir = Join-Path $root 'state'
            if (-not (Test-Path -LiteralPath $dir -PathType Container)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
            $log = Join-Path $dir 'shaka-gate.log'
            if (Test-Path -LiteralPath $log -PathType Leaf) {
                $len = (Get-Item -LiteralPath $log).Length
                if ($len -gt 1048576) {
                    $old = "$log.1"
                    if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old -Force -ErrorAction SilentlyContinue }
                    Move-Item -LiteralPath $log -Destination $old -Force -ErrorAction SilentlyContinue
                }
            }
            $stamp = (Get-Date).ToString('yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture)
            $line = "$stamp|$hookEvent|$tool|$mode|$session`n"
            [IO.File]::AppendAllText($log, $line, (New-Object System.Text.UTF8Encoding($false)))
        }
    } catch {}
} catch {}

[Console]::Out.WriteLine($decision)
exit 0
