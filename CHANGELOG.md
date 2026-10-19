# Changelog

## 0.4.0 - 2026-10-19

* Three modules built: York (`tools/york/york.py`, read-only resource watch: GPU, local Ollama, disk, jobs), Lilith (`tools/lilith/lilith.py` and an example fixture: gate tests with synthetic input that report and never fix) and Edison (`.claude/agents/edison.md`, the planning stage; every plan states usage, an ethics check, approvals and rollback; falls back to `jarvis-planner` when the top tier is unavailable).
* Delegation skill: Edison routing row, top-tier exception and the one allowed fallback; eight roles.
* `.claude/settings.example.json`: Claude in Chrome action tools (click, type, form fill, page scripts, uploads, shortcuts) denied and added to the hook matcher; `ask` only for computer use. Reading pages stays allowed.
* README: Local models section with a table by machine size for Atlas and a local reader, and the reading-in-order rule (a failed final pass stops the run; a fallback guess is never a finished read).
* Shaka Ethics.md, Tool Conventions.md, Project Startup.md and `.gitignore` (your own Lilith fixture) updated to match.

## 0.3.0 - 2026-10-18

* Lighter delegation: the coordinator does the work directly by default and uses fresh stages only where they help (parallel pieces, long or specialist work, a second opinion). The quick-task test is gone.
* Independent review is required for work that leaves the machine or is hard to undo (deliverables for other people, money, secrets, live changes to apps others rely on); elsewhere the assistant's own checks and tests suffice and it shows the user what changed. Both delegation policies, AGENTS.md/CLAUDE.md and README updated.
* Backups are grouped: `backups/<group>/<task>-<stamp>/`, never at the top of `backups/`. `scripts/Sync-ProjectInstructions.ps1` now saves its CLAUDE.md backup under `backups/sync/claude-md-<stamp>/`.
* A go-ahead for a task now covers its local, reversible steps (Tool Conventions.md, Approvals); external, paid, destructive and irreversible actions still need a yes that names them.

## 0.2.0 - 2026-10-17

* New core file `Shaka Ethics.md`: five commitments, a computer-control rule, Claude Code enforcement, the stage ethics block, apps and scripts rules, an incident log, and the module roadmap. Wired into AGENTS.md (Required reading, `@` import, quick-task list), Project Startup.md, Tool Conventions.md and `Show-StartupContext.ps1` (`$core`, `$imports`, `$sizeBudgets`).
* New `scripts/Shaka-Gate.ps1`: a fail-closed PreToolUse hook that logs tool name, time, mode and session id only. `.claude/settings.example.json` now carries `permissions.deny` examples and the hook entry.
* New `examples/AUDIT-REGISTER.example.md` and `examples/INCIDENTS.example.md`.
* Delegation policies: fixed ethics block in every stage brief, a reviewer ethics check, a usage guard, and optional ChatGPT/Codex and Gemini stage patterns. New `jarvis-architect` subagent (seven roles); the top-tier roles are shown as one workspace example as of 17 October 2026.
* Tool Conventions: Tool gate, External reader (Gemini) and Calling the other assistant bullets.
* Delegation skill: an optional on-request page-inspection stage pattern (no code shipped); README and EXCLUDED.md updated to match.
* Three new generic lessons; README sections "One workflow, three models" and "Modules and roadmap"; EXCLUDED.md additions.

## 0.1.0 - 2026-10-13

* Initial public starter kit: shared rulebook (AGENTS.md and a byte-identical CLAUDE.md), general lessons and a lesson-file template, tool conventions, project startup index, optional file workflow.
* Two conversational personas (J.A.R.V.I.S. for Claude Code, E.V. for Codex) as original generic styles, with placeholders for the user's name.
* Six Claude Code stage subagents, the Claude delegation skill, and an example hooks file.
* The Codex delegation skill with `agents/openai.yaml` metadata and an example Codex profiles file.
* Three PowerShell helper scripts, tested on Windows PowerShell 5.1: a read-only startup check, a read-only persona card, and a rulebook sync script that only checks unless run with `-Sync -ReviewedDifference`.
