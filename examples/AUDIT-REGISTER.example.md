# Audit register (example)

Copy this file to `outputs/shaka/AUDIT-REGISTER.md` and replace the example rows (keep rows 2 to 4 if you use the launchers, with your own dates). One row per outward path: anything that sends a message, calls a model or service, commands a device, spends money or moves a file off this computer. See [Shaka Ethics.md](../Shaka%20Ethics.md), "Apps and scripts you build".

| # | Outward path | What leaves or what it does | Guard | Owner | Last checked |
| --- | --- | --- | --- | --- | --- |
| 1 | Example: `send-alert.py` (fictional) | One plain-text alert to the user's own phone | Fixed recipient, rate limit of one alert per 10 minutes, refuses text containing addresses or links | Coordinator | 2026-01-01 |
| 2 | Example: the Antigravity launcher (`scripts/Invoke-AntigravityStage.ps1`) | The brief and every file the stage reads go to Google; in edit mode, the named copies are changed | Pinned CLI and settings lock (no commands, web, browser or MCP tools; no reads of secret or state folders; writes only to the run's copies), scratch folder outside the project, project hashed before and after, every tool step listed, run fails on any other tool. Limit: tools the lock cannot switch off (web search, scheduling, messaging, task management, subagents, questions to the user) are caught only after they ran, with an alert | Coordinator | <date> |
| 3 | Example: the Codex and Claude launchers (`scripts/Invoke-CodexStage.ps1`, `scripts/Invoke-ClaudeStage.ps1`) | The brief and what the stage reads go to that model's vendor on the user's own sign-in; in edit mode, the named copies are changed | Refuses API-key routes, read-only by default, copies only in edit mode (Codex edit mode off until tested), project hashed before and after, run fails on any project change. Claude: start record checked, tools listed. Limit: the Codex sandbox can read any file the user can and does not list them | Coordinator | <date> |
| 4 | Example: `scripts/Apply-ModelEdits.ps1` | Writes a model's edited copies over real project files | Dry run first (diff), protected and non-canonical paths refused, original rechecked just before writing, backup with hash check, a run with a non-zero launcher exit refused unless forced with a reason | Coordinator | <date> |

Rules:

* Add a row in the same change that adds the path. A stage that finds an unlisted path reports it to the coordinator.
* "Guard" names the control that stops misuse: a per-action yes, a fixed recipient, an allowlist or a spending ceiling.
* Re-check a row whenever its code or settings change, and update "Last checked".
