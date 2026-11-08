# AI Workflows

A starter kit for running three AI coding assistants from one shared rulebook: J.A.R.V.I.S. on Claude Code, E.V. on Codex and H.O.M.E.R. on Google Antigravity (Gemini). All of them read the same project instructions, lessons and tool conventions. Each has its own conversational persona and its own entry point or delegation policy, and none touches another's files. The kit is Markdown, eight PowerShell helper scripts, two small Python tools and example configuration. Nothing in it calls a cloud API or needs a key; the only network calls are York's request to a local Ollama server and, if you use them, the launchers that start another assistant's command-line tool on your own sign-in.

It is for people who already use any one of Claude Code, Codex or Antigravity with their own logins and want one rulebook, lessons that persist between sessions, approval rules for external, paid and irreversible actions, and a repeatable review habit.

Current version: 0.5.0 (see [CHANGELOG.md](CHANGELOG.md)). Released under the MIT License (see [LICENSE](LICENSE)). Created by SKULLSHOCK3R (Akshith 'Robin' Naidu): https://skullshock3r.github.io/

> Not affiliated with or endorsed by Marvel, Disney, Sony, Anthropic, OpenAI or Google; character names are used only as nicknames. You can rename all three assistants (see [Rename the personas](#rename-the-personas)).
>
> The module names (Shaka, Pythagoras, Edison, Lilith, Atlas, York) are inspired by the manga One Piece by Eiichiro Oda. This project is not affiliated with or endorsed by its creator or rights holders.

## Requirements

* Any one of Claude Code, Codex or Google Antigravity (its desktop app and CLI), with your own logins.
* Windows PowerShell 5.1 for the hooks and helper scripts (the tested platform). The hook commands call `powershell`; on macOS or Linux you would need `pwsh` and a changed `command` in `settings.json`. That setup is untested.
* Git, if you use the launchers: they hash the project's tracked and untracked files before and after a run, and refuse to run outside a Git repository.
* Python for the two optional tools, York and Lilith. Lilith also calls PowerShell to test the gate hook.
* Optional: [Ollama](https://ollama.com) and an NVIDIA graphics card for local models (see [Local models](#local-models-optional)).

## Quick start

1. Copy this folder (or its contents) into your project root.
2. Open that folder in Claude Code. `CLAUDE.md` imports the core rule files with `@` lines, so they load in full at the start of every session.
3. Read `scripts/Show-StartupContext.ps1`, `scripts/Show-PersonaCard.ps1` and `scripts/Shaka-Gate.ps1`, then copy `.claude/settings.example.json` to `.claude/settings.json`. This turns on the hooks and the tool gate; the details are under [Setup for Claude Code](#setup-for-claude-code).
4. Start a new Claude Code session. New or changed agents, skills and hooks load only in a session started afterwards.
5. Check the install from a terminal:

```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-StartupContext.ps1 -Claude
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-PersonaCard.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Sync-ProjectInstructions.ps1
```

The last command prints a line beginning `Equal: AGENTS.md and CLAUDE.md`. `-ExecutionPolicy Bypass` applies only to that one PowerShell process (the hooks use it too) and does not change your machine's policy; leave it out if your policy already allows local scripts.

For Codex, see [Setup for Codex](#setup-for-codex); for Antigravity, see [Setup for Google Antigravity](#setup-for-google-antigravity).

## What is in the kit

| Path | Purpose |
| --- | --- |
| `AGENTS.md`, `CLAUDE.md` | The shared rulebook. `AGENTS.md` is canonical; `CLAUDE.md` is a byte-identical copy that Claude Code reads. |
| `GEMINI.md` | The third assistant's entry point (Gemini in Google Antigravity): who is who, the files to read, its limits and the workflow. |
| `lessons.md`, `lessons/` | General lessons plus a template for area-specific lesson files. |
| `Tool Conventions.md`, `Project Startup.md`, `File Workflow.md` | Tool rules (including the tool gate and the launchers), the resource index, and an optional intake, approval and archive workflow. |
| `Shaka Ethics.md` | The ethics module: five commitments, the computer-control rule, how the Claude Code gate works, the stage ethics block, and the module roadmap. A core file, imported into every Claude session. |
| `JARVIS Paraphrasing Rules.md`, `EV Paraphrasing Rules.md`, `HOMER Paraphrasing Rules.md` | Conversational personas (chat only, never deliverables), one per assistant. |
| `.claude/` | Eight stage subagents (Edison and seven `jarvis-*` roles), four agents named by model (`fable`, `opus`, `sonnet`, `haiku`), five caller agents (`astra`, `sol`, `terra`, `argon`, `flash`), the Claude delegation skill, and `settings.example.json` (three hooks, including the tool-gate hook, plus deny rules and one ask rule). |
| `Skills/ev-work-delegation/` | The Codex delegation skill and its `agents/openai.yaml` metadata. |
| `examples/` | Example Codex profiles for the stage roles, settings locks for the Antigravity CLI (read-only and edit), and templates for an audit register of outward paths and an incident log. |
| `tools/` | `york/york.py` (read-only resource watch) and `lilith/lilith.py` with an example fixture (gate tests). |
| `scripts/` | Read-only startup check, per-reply persona card, an AGENTS.md/CLAUDE.md sync check, `Shaka-Gate.ps1` (the fail-closed tool-gate hook), three launchers (`Invoke-ClaudeStage.ps1`, `Invoke-CodexStage.ps1`, `Invoke-AntigravityStage.ps1`) and `Apply-ModelEdits.ps1`. |
| `inputs/`, `outputs/`, `tmp/`, `backups/`, `archive/` | Working folders, ignored by Git except for their README files. `tmp/` holds only scratch for the task in progress. |
| `EXCLUDED.md`, `SECURITY.md`, `CHANGELOG.md` | What was left out, how to report problems, and release notes. |

## How the workflow runs

You pick the coordinator: Claude, Codex or Antigravity. The coordinator owns scope, approvals, briefs, evidence and the final result. It does most work directly and runs stages only where they help: pieces that can run in parallel, long or specialist work, or a second opinion. An independent review, always a fresh stage under the coordinator's own policy, covers work that leaves the machine or is hard to undo. If your host lacks a fresh context or a mapped model, the policy says to stop and report, or to offer a clearly labelled manual adaptation, and never to call a reused context an independent review. The descriptions below use Claude as the worked example.

A rulebook and a skill file are instructions. They do not install anything, grant access, create agents or guarantee that stages run.

### Models

Each model and how it is used (every stage runs under the same rules):

| Model | Role | How it is used |
| --- | --- | --- |
| Claude (J.A.R.V.I.S., in Claude Code) | Coordinator and stage workers in the worked example | Owns scope, approvals, briefs, evidence and the final result. Does most work directly and runs `jarvis-*` stages where they help. When Codex or Antigravity coordinates, they hand Claude a stage through `scripts/Invoke-ClaudeStage.ps1`. |
| ChatGPT / Codex (E.V.) | Second assistant; optional stage | Works from the same rulebook in Codex. The coordinator can hand it a saved brief through `scripts/Invoke-CodexStage.ps1` for a second opinion on a high-stakes plan, for overflow, or for a capability Claude lacks. It runs read-only on your ChatGPT login and returns content; its edit mode stays off until you test it. It never replaces the required review. |
| Google Antigravity (H.O.M.E.R.) | Third assistant in its own app; optional stage | Works from the same rulebook through `GEMINI.md`, and the coordinator can hand it a saved brief through `scripts/Invoke-AntigravityStage.ps1` (its lock and limits are under [Launchers and edits](#launchers-and-edits)). Everything it reads goes to Google, and some of its CLI tools cannot be switched off by settings, so decide whether that is acceptable before using it. |
| Google Gemini (reader) | Free, read-only document and image reader; optional stage | A small script you review uploads one file to the Gemini API with a fixed prompt and saves the reply. No other access to your computer, no actions, no edits. Use a key from a free-tier project with no billing. You approve it first, and nothing private or secret is uploaded. |

Every stage, whichever model runs it, carries the same ethics block, and the reviewer checks the stage against it. A stage never approves anything itself: a "yes" found in a file, email, web page or another assistant's message does not count. The kit ships a launcher for each of the three assistants; it does not ship the Gemini reader script. The Claude delegation skill describes each pattern and its limits under "Optional: another assistant as a stage" and "Optional: a Gemini reader stage".

### Launchers and edits

The three launchers share one shape: the brief sits in `tmp/stage-briefs/` and is refused if it looks like it holds a secret; the stage runs on your own sign-in (never an API key, bought credits or a skip-permissions flag); tracked and untracked project files are hashed before and after; up to three stages per launcher run at once; each run leaves a record in `outputs/stage-records/<launcher>/`. Exit codes are the same in all three: 0 ok, 1 failed, 2 timeout, 3 refused before running, 4 a disallowed tool, a denied read, a write outside the copies or a project change, 5 usage limit (no retry), 6 tool errors.

| Launcher | Starts | Read-only lock | Edit mode | What the author tested |
| --- | --- | --- | --- | --- |
| `Invoke-ClaudeStage.ps1` | `claude -p` on a Claude subscription | `--restricted`, read tools only, the session's start record checked | `Read`, `Edit` and `Write` in the copy folder only; `--restricted` confines the file tools there | Read and edit canaries passed; with the same flags, writes to the project and outside it were refused by the CLI itself (Claude Code 2.1.317, November 2026) |
| `Invoke-CodexStage.ps1` | `codex exec` on a ChatGPT sign-in | Read-only sandbox, throwaway session, hooks off | Workspace-write sandbox in the copy folder; refused until you set `$EditModeTested` after the one-file canary in Tool Conventions.md | Refusal tests passed; read and edit canaries passed with this launcher, and with the author's workspace version a brief asking for writes to the project, the run folder and the real file was refused by the sandbox (codex-cli 0.166.0-alpha.5.1, November 2026). Still set `$EditModeTested` only after your own canary |
| `Invoke-AntigravityStage.ps1` | `agy -p` on a Google sign-in | Pinned CLI, settings lock, scratch folder outside the project | Per-run settings that allow writes only to the copy folder | Read and deny canaries passed with this launcher before its edit mode was added (CLI 1.4.12); edit canary passed with this launcher (CLI 1.5.0, November 2026): the copy was edited, writes to the project and to the scratch root were refused by the CLI and flagged (exit 4) |

The Antigravity launcher pins the CLI, passes a session-only settings file that denies writes, commands, web, browser and MCP tools and reads of secret and state folders, runs the stage from a folder outside the project, lists every tool step and fails the run on anything other than a file read (or, in edit mode, a write to the run's copies). Everything it reads goes to Google. Known limit: in the author's tests some CLI tools (web search, scheduling, messaging, task management, subagents, questions to the user) could not be switched off by settings; the launcher catches them only after they ran and flags the run.

Edits are made on copies only: name the files with `-EditPaths` (project-relative, forward slashes). Rule, lesson, persona, policy, settings, secret, state and script files, `.git`, `.claude` and `Skills` are refused. After the run, `scripts/Apply-ModelEdits.ps1 -RunDir <run folder>` writes `changes.patch` for you to read; rerun it with `-Apply` to apply. It refuses an original that changed since the copy, a path not spelled as on disk (an 8.3 short name or another case), a deleted copy, a copy under a link and a run that did not end cleanly (unless you force it with a reason), backs each original up under `backups/model-edits/` and verifies the result.

### Choosing models by task type

Which model family suits which task type is your call, and the answer changes as models change, so the kit ships a method rather than anyone's results. Run a small bake-off per task type: a few fixed briefs that stand for real work, an answer key for each with its hash recorded before any run, scoring by script wherever an answer can be checked, a blind read by a person for writing voice, and the token count of every run beside its score. One run is a leaning, not a verdict. The delegation skill has the steps under "Choosing models by task type (the method)".

### Usage guard

The usage guard keeps staged work from draining your plan. The coordinator checks usage before launching a stage, asks first above about 70% of the 5-hour window or about 5% of the weekly allowance (example thresholds), caps retries at three, gives each stage a time cap, and has a stage return what it has when blocked.

## Modules

Shaka, York, Lilith and Edison ship in this kit. Pythagoras and the author's own Atlas setup are private; they are listed here so the direction is clear, and [Local models](#local-models-optional) shows how to set up an Atlas of your own.

| Module | Role | Status |
| --- | --- | --- |
| Shaka | Ethics module and tool gate | Built (in this kit) |
| York | Resource watch, read-only | Built (in this kit) |
| Lilith | Red-team tests of the tool gate and rule files | Built (in this kit) |
| Edison | Planning stage with usage, ethics, approvals and rollback in every plan | Built (in this kit) |
| Atlas | Local heavy lifting on your own GPU: local models, a local reader, an image upscaler | Built (private); set up your own with Local models |
| Pythagoras | Observe-only research bay for public pages | Built; an on-request research stage for the top model tier (private) |

**Shaka.** The ethics rules in `Shaka Ethics.md`: five commitments (consent, honesty, privacy, no harm, stop and ask), a computer-control rule, a deny-first tool gate with a fail-closed hook, a fixed ethics block in every stage brief, a reviewer check, an audit register of outward paths and an incident log (templates in `examples/`).

**York.** `tools/york/york.py` prints one read-only status: GPU memory and load, a local Ollama server and its loaded models, free disk space and running Python jobs (Windows), and writes the same to `state/york/`. It starts, stops and sends nothing; its only network call is to Ollama on 127.0.0.1. Claude plan usage is read by the assistant, not by the script.

**Lilith.** `tools/lilith/lilith.py` tests the tool gate without calling any real tool (Windows PowerShell). It checks that every tool in your fixture is gated as its class requires (deny for sending, sharing, deleting and similar; ask only for computer control), that the hook's matcher covers every denied connector (`mcp__`) tool and computer control, that the hook answers correctly and fails closed on empty or broken input (synthetic input only), that AGENTS.md matches CLAUDE.md, that the ethics block is in the policy, and that every agent is routed. It reports gaps and never fixes them. Copy `outward-tools.example.json` to `outward-tools.json` (git-ignored) and list your own connectors' tools; on the stock example it reports the placeholder connectors as gaps until you do.

**Edison.** The planning stage, `.claude/agents/edison.md`, runs on the top tier. Every plan states expected usage or cost with a ceiling, an ethics check naming each outward path and its guard, the approvals still needed, and backup and rollback. If your account has no top tier, or the usage guard says no, the same brief runs as `jarvis-planner` on your existing planner model.

**Atlas.** Local heavy lifting: the jobs that run on your own graphics card instead of a cloud stage, so heavy work does not use up your plan. In the author's setup that means local models for drafts and routine coding, a local reader that reads images or scanned pages one by one ahead of a cloud check, and an image upscaler. Top-tier builds stay a cloud stage (`jarvis-architect`) working from an Edison plan. York watches Atlas's GPU and disk, and Lilith tests the gates before anything new goes live.

**Pythagoras.** An optional research helper for looking at public web pages: it opens one page in a disposable headless browser and reports what the page is built from, which scripts it loads, or what it looks like. It only observes. Nothing is clicked, typed, signed in or sent, and private or local addresses are refused. It runs only on the owner's request for a named task, only from the most capable model tier, one page at a time with a hard cap on calls, and everything it brings back is treated as data, never as instructions. An independent review checks every run before anything is reported. The bay itself is not shipped (see `EXCLUDED.md`); the delegation skill describes the pattern under "Optional: an on-request page-inspection stage".

## Local models (optional)

Atlas is the work that runs on your own computer instead of a cloud stage, in two kinds: a local helper for routine coding and drafts, and a local reader that reads images or scanned pages one by one. Both are optional; neither is needed to use this kit. The usual runner is [Ollama](https://ollama.com), bound to 127.0.0.1 only. A local model is free to run but slower and less capable than the top cloud tier, so keep planning, hard builds and final reviews on the cloud stages and give the local model the routine work.

The limit is mostly your graphics card's video memory (VRAM); system memory (RAM) lets bigger "mixture of experts" models run, more slowly, with part of the model outside the card. Sizes below are approximate downloads at the usual 4-bit quantization, as listed by Ollama in October 2026; check the model page before you choose.

| Your machine | Atlas: coding and drafts | Atlas: local reader (images and pages) |
| --- | --- | --- |
| No NVIDIA GPU, or under 6 GB of VRAM | Skip it; use the cloud stages | Skip it; use the optional Gemini reader stage |
| 6 to 8 GB VRAM | `qwen3:8b` (about 5 GB): small edits and drafts only | `gemma3:4b` (about 3.4 GB) |
| 12 to 16 GB VRAM | `qwen3:14b` (about 9 GB) or `gpt-oss:20b` (about 14 GB) | `gemma3:12b` (about 8 GB) |
| 24 GB VRAM | `qwen3.6:27b` (18 to 19 GB) or `qwen3-coder:30b` (about 19 GB, faster) | `gemma3:27b` (about 17 GB) |
| 24 GB VRAM and 64 GB or more of RAM | Also 100B-class mixture-of-experts models such as `gpt-oss:120b` (about 65 GB), partly in system memory and much slower | As above |

* Only one large model fits on the card at a time; a second one pushes the first off and slows both.
* Treat each download as an install: the user approves the exact model and tag first, picks a version at least a week old, and keeps the server on 127.0.0.1 (add a row to the audit register).
* Apple silicon and AMD cards are not covered by this table; size them by the memory the runner can use.
* Order and fallbacks: if a local model's output is passed to a cloud model for a final check, process items in a fixed order and count an item as done only when its final check succeeds. If a final check fails, stop there rather than accepting the local model's output as finished.

## Setup for Claude Code

The quick start covers the copy and the first session. This section explains what the settings file turns on.

### Hooks

Optional but recommended. First read `scripts/Show-StartupContext.ps1` and `scripts/Show-PersonaCard.ps1`; they are read-only, but they run on your machine. Then copy `.claude/settings.example.json` to `.claude/settings.json` (or, if you already have one, merge its `hooks` block into yours). The SessionStart hook verifies the rule files and prints a short manifest; the UserPromptSubmit hook prints the persona's per-reply card with each message.

### Tool gate

Recommended. `.claude/settings.example.json` also carries `permissions.deny` rules (git push, adding a git remote, creating, changing or deleting scheduled tasks, the Claude in Chrome action tools for clicking, typing, form filling, page scripts, uploads, shortcuts and batched actions, and editing `.claude/settings.json`), one `ask` rule for computer use, and a `PreToolUse` hook entry for `scripts/Shaka-Gate.ps1`. Reading pages stays allowed. Read the script first, then:

1. Add every connector tool you use that sends, shares, spends, schedules or deletes (for example your mail connector's send and reply tools) to the `deny` list by its exact name, and add its name to the hook's `matcher`.
2. Use `deny` for anything an assistant must never do. In one workspace on 2026-10-17, `ask` rules did not prompt in auto mode. Keep `ask` for computer control only.
3. Test each rule in a fresh session with a harmless call.

The hook logs only the tool name, time, mode and session id to `state/` (git-ignored), never the inputs.

## Setup for Codex

1. Put the same project folder in front of Codex. It reads `AGENTS.md`. The `@` lines are plain references for Codex; to give E.V. the full set, run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Show-StartupContext.ps1` (default mode prints the core files and E.V.'s persona) or ask it to open the files listed under Required reading.
2. The delegation skill is `Skills/ev-work-delegation/SKILL.md`, with its `agents/openai.yaml` metadata. Placing files in a folder does not install a skill; follow your Codex version's documentation for skill discovery.
3. `examples/codex-config.example.toml` shows illustrative profiles for the planner, reviewer, researcher, producer and routine roles. The model names in it are one workspace example as of 2026-10-13, not a recommendation. Copy only what you want into your own Codex configuration, after checking which models and effort levels your account supports.
4. Optional, to call Codex from another coordinator: read `scripts/Invoke-CodexStage.ps1`. It needs `codex` on your PATH (or `-CodexPath`) signed in with ChatGPT, and refuses if an OpenAI API key is set. Its edit mode stays off until you run the one-file canary in Tool Conventions.md on your own Codex build.

## Setup for Google Antigravity

1. Open the project folder in Antigravity. It reads `AGENTS.md` and `GEMINI.md` itself.
2. Replace `<YOUR NAME>` in `GEMINI.md` and `HOMER Paraphrasing Rules.md`.
3. Nothing in Claude's gate covers this assistant in its own chat: its limits are the ones in `GEMINI.md` and whatever the Antigravity app's own permission settings allow. Check those settings yourself.
4. Optional, the launcher (`scripts/Invoke-AntigravityStage.ps1`): install the CLI yourself (the kit downloads nothing), check its hash against the vendor's release manifest and its signature, and fill the pins at the top of the launcher. Copy `examples/antigravity-stage-settings.example.json` to `tools/antigravity/stage-settings.json` (git-ignored), write your project root and profile folder with the drive letter and forward slashes, add no other keys (the CLI refuses unknown settings), and pin its hash. Prove each deny rule with a canary read of a file that does not exist. Choose a scratch folder outside the project with no rule file in it or above it. Run one harmless brief and read `tools.txt`.
5. For edit mode, copy `examples/antigravity-edit-settings.example.json` to `tools/antigravity/edit-settings.json` the same way, leave `__WORKDIR__` in place (the launcher fills it per run), add a write deny line for each other drive you have, and pin its hash.
6. Known limit (also stated above): some CLI tools (web search, scheduling, messaging, task management, subagents, questions to the user) could not be switched off by settings in the author's tests, and the launcher catches them only after they ran. Tested with Antigravity CLI 1.4.12 and 1.5.0 (November 2026); other versions may name tools differently.

## Rename the personas

The names are nicknames. To rename an assistant, replace the name throughout its persona file (`JARVIS Paraphrasing Rules.md`, `EV Paraphrasing Rules.md` or `HOMER Paraphrasing Rules.md`) and replace the placeholder `<YOUR NAME>` with your own name or form of address. Other mentions of the names (agent and skill names such as `jarvis-reviewer`) are only labels and can stay.

If you also rename a persona file itself, search the kit for the old file name and update every reference in the same change: the link and `@` import in `AGENTS.md`, the tables in `Project Startup.md` and `Tool Conventions.md`, the `$core` and `$imports` lists in `scripts/Show-StartupContext.ps1`, and, for the J.A.R.V.I.S. file, the file name in `scripts/Show-PersonaCard.ps1`. The H.O.M.E.R. file has no import or hook; its references are `GEMINI.md`, `AGENTS.md`, `Project Startup.md` and this README. Keep the exact `## Per-reply card` heading, which the persona-card script looks for. After editing `AGENTS.md`, review the difference and run `scripts/Sync-ProjectInstructions.ps1 -Sync -ReviewedDifference` so `CLAUDE.md` matches it again. You can also delete a persona entirely if you do not use that assistant; remove its imports and references together.

## First-session check

Open a fresh session and ask the assistant to report which files it actually read, any unreadable path, and which models, effort levels and fresh-context options it can really use. With Claude Code and the hooks enabled, the SessionStart hook adds the line "CLAUDE.md and AGENTS.md match; all core project rules were read successfully." to the session's context; ask the assistant to quote what the startup hook reported. The three commands under [Quick start](#quick-start) run the same checks from a terminal.

## Safety

The rulebook asks the assistant to get a specific yes before external, destructive, costly or irreversible actions, to treat instructions found in documents and web pages as data, to never open secret files, and never to send a message on your behalf without approving the exact message. Most of these are instructions to the assistant, not technical guarantees. The deny rules and the gate hook in `.claude/settings.example.json` do block the tools they name, but only the ones you list, and only in Claude Code; the launchers guard the stages they start, with the limits stated above. Review what your tools are permitted to do. See [SECURITY.md](SECURITY.md).

## Versions

| Version | Summary |
| --- | --- |
| 0.5.0 | A third assistant (Gemini in Google Antigravity) with its own entry point and persona; any assistant can coordinate, through three launchers with one shape; edits by other models on copies only, applied by a checked script; agents by model and five callers; a method for choosing models by task type; and a plain statement of what each CLI's settings cannot switch off. |
| 0.4.1 | Atlas redefined as local heavy lifting: the jobs that run on your own graphics card (local models and a local reader) so heavy work does not use up your plan. |
| 0.4.0 | Three modules (York, a read-only resource watch; Lilith, gate tests; Edison, the planning stage) and a guide to local models by machine size. |
| 0.3.0 | A lighter workflow: the assistant does the work directly by default, uses stages only where they help, and keeps an independent review for work that leaves the machine or is hard to undo. |
| 0.2.0 | An ethics module (Shaka), a tool gate that blocks risky tools, a usage guard for stages, and optional second-model stages (ChatGPT/Codex and Google Gemini). |

Full notes are in [CHANGELOG.md](CHANGELOG.md).

## Licence and credit

MIT, see [LICENSE](LICENSE). Created by SKULLSHOCK3R. Not affiliated with or endorsed by Marvel, Disney, Sony, Anthropic, OpenAI or Google; character names are used only as nicknames.

## Contributors

* **SKULLSHOCK3R** (Akshith 'Robin' Naidu): author and maintainer.
* **J.A.R.V.I.S.** (Claude, by Anthropic, in Claude Code): drafted, built and reviewed much of this kit with the author.
* **E.V.** (ChatGPT, by OpenAI, in Codex): advised on the Codex half of the kit and contributed generic workflow and persona wording.

The AI contributors are tools the author used; their makers have not reviewed or endorsed this project.
