---
name: ev-work-delegation
description: Optional E.V. (Codex) delegation policy. Routes substantial project tasks among planner, producer and routine-processor roles, with a separate fresh reviewer checking the finished result. Use for substantial tasks only, not simple questions or unrelated work.
---

# E.V. work delegation

This is the Codex (E.V.) delegation policy only. The Claude assistant, J.A.R.V.I.S., has its own separate policy and does not use this file; neither assistant applies the other's models or agent names.

**Scope.** Use this optional workflow for tasks that benefit from separate planning, production and review. Keep simple questions lightweight. Apply the user's instructions and the host's actual capabilities. It is a reusable assignment policy, not an app-level model switch, and it applies the shared rules in [AGENTS.md](../../AGENTS.md). Do not create scheduled automations merely to run this workflow.

**Coordinator.** One coordinator owns scope, approvals, bounded stage briefs, evidence and the final result. Give each stage a fresh context with only the necessary source material and prior findings. Stages complete their assignment without restarting the workflow. Do not fork full history or reuse the preceding stage's context, including for the final review.

## Route the work

**Assigned stage chats:** complete only the stage in the coordinator's brief. Do not create chats, delegate, run intake again, or restart this workflow through AGENTS.md or a linked reference. Return the actual deliverable and handoff notes to the coordinator. The intake and orchestration instructions below apply only to the coordinating chat.

**Role configuration.** Configure planner, producer, routine processor and independent reviewer roles using models and effort settings available in your host. Verify support before starting; report missing capabilities and agree any manual adaptation explicitly. An example mapping is illustrative, not required. The table below shows **one workspace example as of 2026-10-13**; it is not a universal best choice or default. Verify model and effort support in your own account and host before using it, and see [examples/codex-config.example.toml](../../examples/codex-config.example.toml) for matching profile entries.

| Role | Example model and effort |
| --- | --- |
| Planner and independent final reviewer (strategy, planning, consequential decisions, final review) | `gpt-6-astra`, medium for routine work; high for research, finance or high-stakes work; xhigh only for exceptional complexity |
| Research and financial reasoning | `gpt-6-astra`, high (xhigh only for exceptional complexity) |
| Producer (general writing, business work, general analysis, problem solving) | `gpt-6-sol`, medium |
| Routine processor (classification, extraction, monitoring, templated work) | `gpt-6-luna`, high by default |

First sort the request. **Question or chat:** an answer that changes no file or setting and takes no external, paid, persistent or irreversible action needs no stages; answer it in the coordinating chat. **Quick task:** classify it against the quick-task test in AGENTS.md; if every condition holds, do the work in the coordinating chat after the required backup or commit, then hand the request, the changed file or setting and its baseline to a fresh reviewer (step 3). No planning or production stage runs. If the test fails or becomes doubtful at any point, switch to the standard route. **Standard task:** begin with fresh planning. For nontrivial work, write a binding `PLAN.md` before production with scope, exact files, acceptance criteria, risks, and actions needing exact approval; update it before work that expands scope. Route each subsequent stage by its actual work; research, financial reasoning and high-stakes judgments take precedence over general analysis, while literal extraction and template filling belong to the routine processor. A task may need only one production stage, but **every in-scope result needs a separate fresh reviewer**. Carry the user's instructions, source paths and authorization limits into every handoff.

## Execute and hand off

### Recover a stage cut off by a usage limit

Before each assigned stage starts, the coordinator records its brief, identity, mapped model and effort, files it may touch (including expected new files), pre-start SHA-256 hashes or absence, verified baseline backup paths and hashes, required tests, and authorized side effects. A stage interrupted by a usage limit is unfinished. Partial success is never a pass, review verdict or completed task. Wait until the interrupted work has stopped before assessing its files.

Hash every briefed file after interruption, including created and deleted files, and inspect backups created by the stage. Report each changed and unchanged file; missing evidence remains unresolved. If nothing changed, start a fresh stage with the same role, model, effort and brief, adding that the previous attempt was cut off and changed nothing. If anything changed, first verify baseline backup hashes, then back up each current partial file and verify those hashes. Start a fresh recovery stage to compare every partial file with its baseline, identify completed, missing and broken work, finish it, and run the relevant tests. Do not blindly redo or roll back. Do not repeat a possibly completed paid or external action without proof that it did not happen; each paid action still needs its own exact cost approval. Never allow two stages to edit the same files at once. Follow completed production with a separate fresh final review; an interrupted reviewer supplies no valid verdict.

1. Check which models, effort levels and fresh-context tools are actually available. Assign work through supported tools; describing an agent role does not count. Where the host requires internal subagents, spawn a fresh subagent for each stage with no inherited conversation and the mapped model and effort. If a provider supports only stage chats, use separate fresh chats. If the host lacks a fresh context or a mapped model, stop and report the blocked stage, or offer a **clearly labelled manual adaptation**; never describe a reused context or an unreviewed result as an independent final review.
2. Give each stage a compact brief: task and acceptance criteria, relevant constraints and source paths, prior results, and required output. Require each stage to open the shared core instructions, its applicable persona and skill files, and only the scoped lesson files its work needs (name them in the brief). Include: "You are stage [name] of an existing coordinated workflow. Complete only this stage; do not create more chats or restart the delegation policy. Return your deliverable and handoff notes to the coordinator." End every stage with simple notes: results, evidence or artifact paths, assumptions, open issues, proposed lessons and next step. Only the coordinator edits shared lessons. Keep a short execution record of the actual agent or chat ID, selected model and effort, artifact and review outcome. Transfer concise notes and evidence into the next fresh context, not the entire conversation.
3. Once production is complete, hand the **actual completed artifact**, the binding plan where applicable, source material and verified pre-edit backups to a separate fresh reviewer. The reviewer independently diffs every changed file against its backup, reruns relevant tests and checks with actual counts, and inspects applicable sources, calculations, encoding, logs and rendered pages. Mark inapplicable checks with reasons; report warnings and failures honestly. The reviewer returns exactly one verdict: approved, approved with corrections, or rejected, with grounds. Correct defects and recheck the corrected result. Do not make substantive edits after approval without another fresh review. Do not skip the final review.
4. Skip production stages that do not apply; do not manufacture empty work. Run dependent stages in order, and never have multiple chats edit the same artifact concurrently. Corrections within the final review remain in that review chat; any return to an earlier production stage starts a fresh chat, followed by another fresh review.
5. If a required model, effort or fresh-chat mechanism is unavailable, prepare the relevant brief and evidence, report exactly which stage is blocked, and do not silently substitute another model, reuse a chat, or claim the stage was completed. Report only confirmed work as complete.

Keep user-facing reporting brief: what was produced, where it is, the review outcome and any remaining issue. Follow the exact-approval and secret rules in [Tool Conventions.md](../../Tool%20Conventions.md).

## Persona

Use a concise, practical, respectful voice. Lead with the answer, explain uncertainty plainly, and distinguish verified results from checks still underway. Apply this style to conversation; keep documents and code in their requested register. Never invent access, actions, evidence or monitoring. The full conversational rules are in [EV Paraphrasing Rules.md](../../EV%20Paraphrasing%20Rules.md).

## Notes on packaging

This Markdown policy and `agents/openai.yaml` are instructions and skill metadata. They do not install a skill, change a model, grant provider access, create agents or guarantee stage execution. Check your host's own documentation for how it discovers skills and how its subagents or profiles are configured, and verify the result in a fresh session.

## Maintenance

* The quick-task test lives in AGENTS.md Global Rules; when it changes, recheck the quick route here in the same change.
* The model identifiers in the example table are public product names used as one dated example. Update the date and verify availability when you change them.
