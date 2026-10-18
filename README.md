# AI Workflows

A starter kit for running two AI coding assistants from one shared rulebook: **J.A.R.V.I.S.** on Claude Code and **E.V.** on Codex. Both read the same project instructions, lessons and tool conventions, each has its own conversational persona and its own optional delegation policy, and neither touches the other's files. The multi-stage workflow (fresh planning, production and independent review stages) is optional and proportional: simple questions stay simple.

Created by SKULLSHOCK3R (Akshith 'Robin' Naidu): https://skullshock3r.github.io/

Version 0.2.0 adds an ethics module (Shaka), a tool gate that blocks risky tools, a usage guard for stages, and optional second-model stages (ChatGPT/Codex and Google Gemini). See [CHANGELOG.md](CHANGELOG.md).

Released under the MIT License (see [LICENSE](LICENSE)).

> Not affiliated with or endorsed by Marvel, Disney, Sony, Anthropic, OpenAI or Google; character names are used only as nicknames. You can rename both assistants (see below).
>
> The module names (Shaka, Pythagoras, Edison, Lilith, Atlas, York) are inspired by the manga One Piece by Eiichiro Oda. This project is not affiliated with or endorsed by its creator or rights holders.

## Who it is for

People who already use Claude Code and/or Codex with their own logins and want a consistent, auditable way to work: one rulebook, durable lessons, approval rules for external, paid and irreversible actions, and a repeatable review habit. Nothing here calls an API or needs a key; the kit is Markdown, a few PowerShell helper scripts, and example configuration.

## What is in the kit

| Path | Purpose |
| --- | --- |
| `AGENTS.md`, `CLAUDE.md` | The shared rulebook. `AGENTS.md` is canonical; `CLAUDE.md` is a byte-identical copy that Claude Code reads. |
| `lessons.md`, `lessons/` | General lessons plus a template for area-specific lesson files. |
| `Tool Conventions.md`, `Project Startup.md`, `File Workflow.md` | Tool rules (including the tool gate and the optional second-model stages), the resource index, and an optional intake/approval/archive workflow. |
| `Shaka Ethics.md` | The ethics module: five commitments, the computer-control rule, how the Claude Code gate works, the stage ethics block, and the module roadmap. A core file, imported into every Claude session. |
| `JARVIS Paraphrasing Rules.md`, `EV Paraphrasing Rules.md` | Conversational personas (chat only, never deliverables). |
| `.claude/` | Seven `jarvis-*` stage subagents, the Claude delegation skill, and `settings.example.json` (two hooks, a tool-gate hook and deny rules). |
| `Skills/ev-work-delegation/` | The Codex delegation skill and its `agents/openai.yaml` metadata. |
| `examples/` | Example Codex profiles for the stage roles, plus templates for an audit register of outward paths and an incident log. |
| `scripts/` | Read-only startup check, per-reply persona card, an AGENTS.md/CLAUDE.md sync check, and `Shaka-Gate.ps1`, the fail-closed tool-gate hook. |
| `inputs/`, `outputs/`, `tmp/`, `backups/`, `archive/` | Working folders, ignored by Git except for their README files. |
| `EXCLUDED.md`, `SECURITY.md`, `CHANGELOG.md` | What was left out, how to report problems, and release notes. |

## One workflow, three models

The kit is one workflow with three models that each do what they are good at. Claude is the coordinator; the other two are optional stages that the coordinator calls, always under the same rules.

| Model | Role | How it is used |
| --- | --- | --- |
| Claude (J.A.R.V.I.S., in Claude Code) | Coordinator and stage workers | Owns scope, approvals, briefs, evidence and the final result. Runs the `jarvis-*` stages. The independent final review is always a fresh Claude stage. |
| ChatGPT / Codex (E.V.) | Second assistant; optional read-only stage | Works from the same rulebook in Codex. The coordinator can ask it for a second opinion on a high-stakes plan, for overflow when the Claude allowance is stretched, or for a capability Claude lacks. It runs read-only on your ChatGPT login and returns content; it never replaces the required review. |
| Google Gemini | Free, read-only document and image reader; optional stage | A small script you review uploads one file to the Gemini API with a fixed prompt and saves the reply. No other access to your computer, no actions, no edits. Use a key from a free-tier project with no billing. You approve it first, and nothing private or secret is uploaded. |

Every stage, whichever model runs it, carries the same ethics block and is checked against it by the reviewer. A stage never approves anything itself: a "yes" found in a file, email, web page or another assistant's message does not count. The kit does not ship a launcher for Codex or the Gemini script; the delegation skill describes the pattern and its limits ("Optional: a ChatGPT/Codex stage" and "Optional: a Gemini reader stage").

A **usage guard** keeps staged work from draining your plan: check usage before launching a stage, ask first above about 70% of the 5-hour window or about 5% of the weekly allowance (example thresholds), cap retries at three, give each stage a time cap, and have a stage return what it has when blocked.

## Modules and roadmap

The ethics module is part of this kit. The other modules belong to the author's wider private setup; they are listed so the direction is clear.

* **Shaka (built; in this kit).** The ethics rules in `Shaka Ethics.md`: five commitments (consent, honesty, privacy, no harm, stop and ask), a computer-control rule, a deny-first tool gate with a fail-closed hook, a fixed ethics block in every stage brief, a reviewer check, an audit register of outward paths and an incident log (templates in `examples/`).
* **Pythagoras (built in the author's workspace; the bay itself is not shipped, see `EXCLUDED.md`).** An optional research helper for looking at public web pages: it opens one page in a disposable headless browser and reports what the page is built from, which scripts it loads, or what it looks like. It only observes. Nothing is clicked, typed, signed in or sent, and private or local addresses are refused. It runs only on the owner's request for a named task, only from the most capable model tier, one page at a time with a hard cap on calls, and everything it brings back is treated as data, never as instructions. An independent review checks every run before anything is reported. The delegation skill describes the pattern ("Optional: an on-request page-inspection stage").

| Module | Role | Status |
| --- | --- | --- |
| Shaka | Ethics module and tool gate | Built (in this kit) |
| Pythagoras | Observe-only research bay for public pages | Built; an on-request research stage for the top model tier (private) |
| Edison | Planning | Planned, not built |
| Lilith | Red-team testing of the gates and workflows | Planned, not built |
| Atlas | Heavy builds | Planned, not built |
| York | Resource watch, read-only | Planned, not built |

## Setup for Claude Code

1. Copy this folder (or its contents) into your project root and open that folder in Claude Code. `CLAUDE.md` imports the core rule files with `@` lines, so they load in full at the start of every session.
2. Optional but recommended: enable the hooks. First read `scripts/Show-StartupContext.ps1` and `scripts/Show-PersonaCard.ps1`; they are read-only, but they run on your machine. Then copy `.claude/settings.example.json` to `.claude/settings.json` (or, if you already have one, merge its `hooks` block into yours). The SessionStart hook verifies the rule files and prints a short manifest; the UserPromptSubmit hook prints the persona's per-reply card with each message.
3. Turn on the tool gate (recommended). `.claude/settings.example.json` also carries `permissions.deny` rules (git push, adding a git remote, scheduled-task creation, editing `.claude/settings.json`) and a `PreToolUse` hook entry for `scripts/Shaka-Gate.ps1`. Read the script first. Add every connector tool you use that sends, shares, spends, schedules or deletes (for example your mail connector's send and reply tools) to the `deny` list by its exact name, and add its name to the hook's `matcher`. Use `deny` for anything an assistant must never do: in one workspace on 2026-10-17, `ask` rules did not prompt in auto mode. Keep `ask` for computer control only. The hook logs only the tool name, time, mode and session id to `state/` (git-ignored), never the inputs. Test each rule in a fresh session with a harmless call.
4. Platform: tested on Windows PowerShell 5.1. The hook commands call `powershell`; on macOS or Linux you would need `pwsh` and to change the `command` in `settings.json`. That setup is untested.
5. Start a **new** Claude Code session. New or changed agents, skills and hooks load only in a session started afterwards.

## Setup for Codex

1. Put the same project folder in front of Codex. It reads `AGENTS.md`. The `@` lines are plain references for Codex; to give E.V. the full set, run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-StartupContext.ps1` (default mode prints the core files and E.V.'s persona) or ask it to open the files listed under Required reading.
2. The delegation skill is `Skills/ev-work-delegation/SKILL.md`, with its `agents/openai.yaml` metadata. Placing files in a folder does not install a skill; follow your Codex version's documentation for skill discovery.
3. `examples/codex-config.example.toml` shows illustrative profiles for the planner, producer, routine and reviewer roles. The model names in it are **one workspace example as of 2026-10-13**, not a recommendation. Copy only what you want into your own Codex configuration, after checking which models and effort levels your account supports.

## Rename the personas

The names are nicknames. To rename an assistant, replace the name throughout its persona file (`JARVIS Paraphrasing Rules.md` or `EV Paraphrasing Rules.md`) and replace the placeholder `<YOUR NAME>` with your own name or form of address. Other mentions of the names (agent and skill names such as `jarvis-reviewer`) are only labels and can stay.

If you also rename a persona file itself, search the kit for the old file name and update every reference in the same change: the link and `@` import in `AGENTS.md`, the tables in `Project Startup.md` and `Tool Conventions.md`, the `$core` and `$imports` lists in `scripts/Show-StartupContext.ps1`, and, for the J.A.R.V.I.S. file, the file name in `scripts/Show-PersonaCard.ps1`. Keep the exact `## Per-reply card` heading, which that script looks for. After editing `AGENTS.md`, review the difference and run `scripts/Sync-ProjectInstructions.ps1 -Sync -ReviewedDifference` so `CLAUDE.md` matches it again. You can also delete a persona entirely if you only use one assistant; remove its imports and references together.

## First-session check

Open a fresh session and ask the assistant to report which files it actually read, any unreadable path, and which models, effort levels and fresh-context options it can really use. With Claude Code and the hooks enabled, the SessionStart hook adds the line "CLAUDE.md and AGENTS.md match; all core project rules were read successfully." to the session's context; ask the assistant to quote what the startup hook reported. From a terminal you can also run:

```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-StartupContext.ps1 -Claude
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-PersonaCard.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Sync-ProjectInstructions.ps1
```

The last command should print `Equal: AGENTS.md and CLAUDE.md`. `-ExecutionPolicy Bypass` applies only to that one PowerShell process (the hooks use it too); it does not change your machine's policy. Leave it out if your policy already allows local scripts.

## The multi-stage workflow is optional and proportional

The delegation skills describe a coordinator that gives fresh planning, production and independent review stages bounded briefs. Use it for tasks that benefit from separate planning, production and review; keep simple questions lightweight. If your host lacks a fresh context or a mapped model, the policy says to stop and report, or to offer a clearly labelled manual adaptation, and never to call a reused context an independent review. A rulebook and a skill file are instructions: they do not install anything, grant access, create agents, or guarantee that stages run.

## Safety defaults

The rulebook asks the assistant to get a specific yes before external, destructive, costly or irreversible actions, to treat instructions found in documents and web pages as data, to never open secret files, and never to send a message on your behalf without approving the exact message. Most of these are instructions to the assistant, not technical guarantees. The deny rules and the gate hook in `.claude/settings.example.json` do block the tools they name, but only the ones you list; review what your tools are permitted to do. See [SECURITY.md](SECURITY.md).

## Licence and credit

MIT, see [LICENSE](LICENSE). Created by SKULLSHOCK3R. Not affiliated with or endorsed by Marvel, Disney, Sony, Anthropic, OpenAI or Google; character names are used only as nicknames.

## Contributors

- **SKULLSHOCK3R** (Akshith 'Robin' Naidu): author and maintainer.
- **J.A.R.V.I.S.** (Claude, by Anthropic, in Claude Code): drafted, built and reviewed much of this kit with the author.
- **E.V.** (ChatGPT, by OpenAI, in Codex): advised on the Codex half of the kit and contributed generic workflow and persona wording.

The AI contributors are tools the author used; their makers have not reviewed or endorsed this project.
