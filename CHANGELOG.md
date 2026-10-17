# Changelog

## 0.2.0 - 2026-10-17

* New core file `Shaka Ethics.md`: five commitments, a computer-control rule, Claude Code enforcement, the stage ethics block, apps and scripts rules, an incident log, and the module roadmap. Wired into AGENTS.md (Required reading, `@` import, quick-task list), Project Startup.md, Tool Conventions.md and `Show-StartupContext.ps1` (`$core`, `$imports`, `$sizeBudgets`).
* New `scripts/Shaka-Gate.ps1`: a fail-closed PreToolUse hook that logs tool name, time, mode and session id only. `.claude/settings.example.json` now carries `permissions.deny` examples and the hook entry.
* New `examples/AUDIT-REGISTER.example.md` and `examples/INCIDENTS.example.md`.
* Delegation policies: fixed ethics block in every stage brief, a reviewer ethics check, a usage guard, and optional ChatGPT/Codex and Gemini stage patterns. New `jarvis-architect` subagent (seven roles); the top-tier roles are shown as one workspace example as of 17 October 2026.
* Tool Conventions: Tool gate, External reader (Gemini) and Calling the other assistant bullets.
* Three new generic lessons; README sections "One workflow, three models" and "Modules and roadmap"; EXCLUDED.md additions.

## 0.1.0 - 2026-10-13

* Initial public starter kit: shared rulebook (AGENTS.md and a byte-identical CLAUDE.md), general lessons and a lesson-file template, tool conventions, project startup index, optional file workflow.
* Two conversational personas (J.A.R.V.I.S. for Claude Code, E.V. for Codex) as original generic styles, with placeholders for the user's name.
* Six Claude Code stage subagents, the Claude delegation skill, and an example hooks file.
* The Codex delegation skill with `agents/openai.yaml` metadata and an example Codex profiles file.
* Three PowerShell helper scripts, tested on Windows PowerShell 5.1: a read-only startup check, a read-only persona card, and a rulebook sync script that only checks unless run with `-Sync -ReviewedDifference`.
