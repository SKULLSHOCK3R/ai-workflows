# Project startup and resource index

This project has one shared rulebook for Codex and Claude Code. `AGENTS.md` is canonical, and `CLAUDE.md` is its byte-identical copy. Open the project folder itself as the project root, and resolve Markdown links relative to the file that contains each link. Check access to a path before relying on it.

## Reading order

1. Before the first reply of every new chat or session, read [AGENTS.md](AGENTS.md) or its identical [CLAUDE.md](CLAUDE.md) entry point, then, in full, [lessons.md](lessons.md), [Tool Conventions.md](Tool%20Conventions.md), this file, [Shaka Ethics.md](Shaka%20Ethics.md) and only your own persona file: E.V. reads [EV Paraphrasing Rules.md](EV%20Paraphrasing%20Rules.md); J.A.R.V.I.S. reads its separate persona file. The AGENTS.md Startup check says what counts as a full read. Before work in a named area, also read that area's file under `lessons/`, and no other.
2. For files in `inputs`, read [File Workflow.md](File%20Workflow.md) and inspect the workflow ledger (`file-workflow-state.json`, created from [file-workflow-state.example.json](file-workflow-state.example.json)) before processing or recording status.
3. Read the applicable local skill from the table below. Read linked references relevant to the task. Do not infer a skill, model, plugin or provider permission from the mere presence of a file.

| Resource | Use |
| --- | --- |
| [Skills/ev-work-delegation/SKILL.md](Skills/ev-work-delegation/SKILL.md) | **E.V. (Codex) only:** optional stage routing with E.V.'s own agents; load once per task. Its [agents/openai.yaml](Skills/ev-work-delegation/agents/openai.yaml) is Codex skill metadata. Claude never uses this file. |
| [.claude/skills/jarvis-work-delegation/SKILL.md](.claude/skills/jarvis-work-delegation/SKILL.md) | **J.A.R.V.I.S. (Claude Code) only:** optional stage routing with the `jarvis-*` subagents; load once per task. Codex never uses this file. |
| [Shaka Ethics.md](Shaka%20Ethics.md) | Core ethics rules for every workflow, stage, scheduled task and app (consent, honesty, privacy, no harm, stop and ask; no computer control without the user's yes). Imported at startup; its tool gate lives in `.claude/settings.json` and [scripts/Shaka-Gate.ps1](scripts/Shaka-Gate.ps1); templates in `examples/`. |
| [File Workflow.md](File%20Workflow.md) and `file-workflow-state.json` | Controlled intake, versioned outputs, approval and archival. The coordinating chat is the sole ledger writer. |
| [lessons/Example Area Lessons.md](lessons/Example%20Area%20Lessons.md) | Template for area-specific lesson files. Read the matching file with [lessons.md](lessons.md) before work in that area; not preloaded at startup. |
| [EV Paraphrasing Rules.md](EV%20Paraphrasing%20Rules.md) | Codex-only conversational style for E.V. Separate from the Voice Principles and never applied inside files or deliverables; Claude ignores it. |
| [JARVIS Paraphrasing Rules.md](JARVIS%20Paraphrasing%20Rules.md) | Claude-only conversational style for J.A.R.V.I.S. Separate from the Voice Principles and never applied to files or deliverables; Codex ignores it. |
| `.claude` folder | Claude Code setup: `jarvis-*` subagents, Claude's delegation policy and an example settings file (hooks and deny rules). Described in [Tool Conventions.md](Tool%20Conventions.md). |
| [examples/codex-config.example.toml](examples/codex-config.example.toml) | Example Codex profiles for the E.V. stage roles. Illustrative only; check your own Codex version. |
| [scripts/Show-StartupContext.ps1](scripts/Show-StartupContext.ps1) | Read-only startup check. Default (Codex) mode prints the common core plus E.V.'s persona in full. With `-Claude` (the SessionStart hook) it verifies the files and the CLAUDE.md imports, then prints a short manifest and the J.A.R.V.I.S. Per-reply card, staying under Claude Code's 10,000-character hook cap. In both modes it prints one maintenance-only WARNING per rule or lesson file over its byte budget. |
| [scripts/Show-PersonaCard.ps1](scripts/Show-PersonaCard.ps1) | Claude-only, read-only: prints the Per-reply card section of [JARVIS Paraphrasing Rules.md](JARVIS%20Paraphrasing%20Rules.md) with every user message (UserPromptSubmit hook). |
| [scripts/Shaka-Gate.ps1](scripts/Shaka-Gate.ps1) | Claude-only PreToolUse hook (the tool gate): logs the tool name, time, mode and session id, never the inputs, and answers deny, or ask for computer control; fails closed. |
| [scripts/Sync-ProjectInstructions.ps1](scripts/Sync-ProjectInstructions.ps1) | Checks that AGENTS.md and CLAUDE.md are byte-identical; with `-Sync -ReviewedDifference` it copies AGENTS.md over CLAUDE.md after saving a backup to `backups/`. |
| [inputs](inputs), [outputs](outputs), [archive](archive) | Intake, deliverables and approved originals under the file workflow. |
| [tmp](tmp), [backups](backups) | Scratch work and pre-change copies of files Git does not track. Ignored by Git; see the Backups rule in [Tool Conventions.md](Tool%20Conventions.md). |
| `.git` and [.gitignore](.gitignore) | Optional Git history. `.gitignore` excludes secrets, local settings, `state/` folders, scratch, backups, intake and archive. Commit before editing a tracked file. |

## Claude Code setup and verification

Claude Code documents a project-root `CLAUDE.md` instruction file in its memory guide. Those docs do not establish that every Claude surface loads every linked file, so verify behaviour on your own surface. A short project instruction for any Claude project that accepts one:

> Work from this project folder. Read the root CLAUDE.md, Project Startup.md, lessons.md and Tool Conventions.md, then the applicable local skill and references before acting. Treat AGENTS.md as the canonical instruction copy. Report any unreadable file or unavailable model, stage-chat tool or permission. Do not claim that a linked resource was read until you have opened it.

For a read-only check, ask Claude to open the entry file and core files above, read the header and operating rules of the delegation skill, and report the constraints it found, the files it actually opened, any unreadable paths, and the available model, effort and folder permissions.

For later instruction edits, update AGENTS.md, review drift, and synchronize CLAUDE.md with [scripts/Sync-ProjectInstructions.ps1](scripts/Sync-ProjectInstructions.ps1). The script's default mode only checks equality.

## Maintenance

* When a rule or reference file that sessions load or list is added, renamed or moved, update in the same change: the reading order and table here, the AGENTS.md Required reading link and `@` import if the file is core (and the import list in Show-StartupContext.ps1), the `$core` or `$onDemand` list in [Show-StartupContext.ps1](scripts/Show-StartupContext.ps1), and its `$sizeBudgets` table if the file is a lesson file or a core rule file.
* Rows say what a file is for and link to it; keep the rules themselves in the linked file.
* Files imported into CLAUDE.md (lessons.md, Tool Conventions.md, this file, Shaka Ethics.md and JARVIS Paraphrasing Rules.md) must never contain a space, `@` and a path outside code spans, or Claude Code imports that file too. Keep any hook's printed output under 10,000 characters.
