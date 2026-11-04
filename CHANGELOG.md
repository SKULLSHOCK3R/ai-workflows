# Changelog

## 0.5.0 - 2026-11-04

* Third assistant: `GEMINI.md` (entry point for Gemini in Google Antigravity, nicknamed H.O.M.E.R.) and `HOMER Paraphrasing Rules.md` (chat-only persona). AGENTS.md, Project Startup.md, Tool Conventions.md and Shaka Ethics.md name the third assistant and its limits; no tool gate covers it in its own chat, and the files say so.
* Any assistant can coordinate: three launchers with one shape hand a stage to the other two, each on the user's own sign-in: `scripts/Invoke-ClaudeStage.ps1` (new), `scripts/Invoke-CodexStage.ps1` and `scripts/Invoke-AntigravityStage.ps1`. One brief folder, one exit-code table, a before-and-after hash of the project, refusal of secret-looking briefs, up to three stages per launcher, and in each header an honest list of what that CLI's settings cannot switch off. `examples/antigravity-stage-settings.example.json` is the Antigravity settings lock; its pins stay empty until you fill them.
* Editing as copies: each launcher's `-EditPaths` copies named files into the run folder and the model edits only the copies; protected paths (rule, lesson, persona, policy, settings, secret, state and script files, `.git`, `.claude`, `Skills`) are refused, any project change or stray file fails the run, and `edit-manifest.json` is written after the run. New `scripts/Apply-ModelEdits.ps1` writes the diff and, with `-Apply`, applies it after its checks with a backup. `examples/antigravity-edit-settings.example.json` allows writes only to the run's copy. Claude edit mode was canary-tested; Codex edit mode stays refused until you test it on your own build.
* Nine new agents: `fable`, `opus`, `sonnet` and `haiku` (stages where the coordinator picks the model), and five callers, `astra`, `sol` and `terra` (GPT models through the Codex launcher) and `argon` and `flash` (Gemini models through the Antigravity launcher), which return the answer unchanged; 17 agent files in all. Model ids are one workspace example as of November 2026.
* Delegation skill: "Optional: another assistant as a stage" replaces the Codex-only section, and a short method for choosing models by task type with a small bake-off (no results shipped). The Codex delegation skill points at the launchers.
* Rule: nothing permanent stays in any `tmp` folder (AGENTS.md, Tool Conventions.md, the skill, `tmp/README.md`).
* Six generic lessons (agent CLIs load the enclosing repository's rule files; `tail -F` on Windows; one fixed command per scheduled task; deny rules need a canary; Windows path canonicalisation; testing a lock with a neutral prompt).
* README restructured (requirements, quick start, one modules table, versions table) with facts corrected: the full deny set and three hooks, five Codex example profiles, the network calls, the `Equal:` line. SECURITY.md: eight scripts, and what makes a network call. `examples/AUDIT-REGISTER.example.md`: launcher and apply rows. `.gitignore`: your Antigravity CLI folder and filled settings. EXCLUDED.md: pins, run records and routing results.

## 0.4.1 - 2026-10-21

* Atlas redefined and marked built (in the author's workspace): local heavy lifting, meaning the jobs that run on your own graphics card (local models for drafts and routine coding, a local reader for images and scanned pages, an image upscaler) so heavy work does not use up your plan. Top-tier builds stay the `jarvis-architect` cloud stage. README modules list, table and Local models headings, and Shaka Ethics.md's module table updated to match.

## 0.4.0 - 2026-10-19

* Three modules built: York (`tools/york/york.py`, read-only resource watch: GPU, local Ollama, disk, jobs), Lilith (`tools/lilith/lilith.py` and an example fixture: gate tests with synthetic input that report and never fix) and Edison (`.claude/agents/edison.md`, the planning stage; every plan states usage, an ethics check, approvals and rollback; falls back to `jarvis-planner` when the top tier is unavailable).
* Delegation skill: Edison routing row, top-tier exception and the one allowed fallback; eight roles.
* `.claude/settings.example.json`: Claude in Chrome action tools (click, type, form fill, page scripts, uploads, shortcuts and batched actions) denied and added to the hook matcher; `ask` only for computer use. Reading pages stays allowed.
* README: Local models section with a table by machine size for Atlas and a local reader, and a rule on order and fallbacks when a local model's output gets a cloud check.
* Shaka Ethics.md, Tool Conventions.md, Project Startup.md and `.gitignore` (your own Lilith fixture and `.claude/settings.json`) updated to match.

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
