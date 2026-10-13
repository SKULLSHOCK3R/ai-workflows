---
name: jarvis-work-delegation
description: Optional J.A.R.V.I.S. (Claude Code) delegation policy for substantial project tasks. Routes stages to the jarvis-* subagents, with an independent jarvis-reviewer final review. Use once per in-scope task; not for simple questions or unrelated work.
---

# J.A.R.V.I.S. work delegation

This is the Claude Code (J.A.R.V.I.S.) delegation policy only. E.V. (Codex) has its own separate policy in `Skills/ev-work-delegation`, which this assistant never uses. Neither assistant applies the other's models, agent names or stage mechanics.

**Scope.** Use this optional workflow for tasks that benefit from separate planning, production and review. Keep simple questions lightweight. Apply the user's instructions and the host's actual capabilities. It follows the shared rules in [AGENTS.md](../../../AGENTS.md). The user asks for fresh contexts between stages, including the final review. Do not create scheduled automations merely to run this workflow.

## Route the work

**Coordinator.** One coordinator (the main J.A.R.V.I.S. session) owns scope, approvals, bounded stage briefs, evidence and the final result. A subagent completes only the stage in the coordinator's brief. It does not spawn agents, delegate, run intake again, or restart this workflow through AGENTS.md or a linked reference, and it returns the deliverable and handoff notes to the coordinator. The routing below applies only to the coordinator.

**Role configuration.** Configure planner, producer, routine-processor and independent-reviewer roles using models and effort settings available in your host. Verify support before starting; report missing capabilities and agree any manual adaptation explicitly. The mapping below is an example, not a requirement.

| Work | Subagent | Model and effort (example) |
| --- | --- | --- |
| Intake, planning, and routine consequential decisions | `jarvis-planner` | Opus, medium |
| Research, financial reasoning, and high-stakes planning or review | `jarvis-analyst` | Opus, high |
| Exceptionally complex research, finance, or review (justify first) | `jarvis-analyst-xhigh` | Opus, xhigh |
| General writing, business work, general analysis, and problem solving | `jarvis-writer` | Sonnet, medium |
| Classification, extraction, monitoring, and templated work | `jarvis-clerk` | Haiku, high |
| Independent final review of every completed deliverable | `jarvis-reviewer` (or `jarvis-analyst` when high-stakes) | Opus, medium (high) |

The subagents are defined in `.claude/agents/`. Each file fixes its model alias and effort, and each alias tracks the current model of its tier. To change a role, edit that agent file and this table together. Do not substitute other models silently.

A question or chat reply that changes no file or setting and takes no external, paid, persistent or irreversible action needs no stages; answer it directly. Otherwise, first classify the task against the quick-task test in AGENTS.md. **Quick task:** do the work in the coordinating session after the required backup or commit, then launch a fresh `jarvis-reviewer` with the request, the changed file or setting, and its baseline; the review follows step 3 below, and no `jarvis-planner` or production stage runs. If the test fails at any point, switch to the standard route. **Standard task:** begin with a `jarvis-planner` stage to assess the request, stakes, needed stages and acceptance criteria. Route each later stage by its actual work: research, finance and high-stakes judgment go to `jarvis-analyst`; literal extraction and template filling go to `jarvis-clerk`; general writing goes to `jarvis-writer`. A task may need only one production stage, but **every in-scope result needs a separate, independent final review**. Carry the user's instructions, source paths and authorization limits into every handoff.

## Execute and hand off

1. Launch each stage with the Agent tool, naming the subagent type from the table and giving a self-contained brief. Every stage is a new Agent call with no conversation history, and receives only the necessary source material and prior findings. Never continue a previous stage's agent, except to finish corrections inside the same final review. Agents added or changed in `.claude/agents` load only in a session started afterwards.
2. Give each stage a compact brief: task and acceptance criteria, constraints and source paths, any scoped lesson files it must read (only those its work touches), prior results, and required output. Include: "You are stage [name] of an existing coordinated workflow. Complete only this stage; do not spawn agents or restart the delegation policy. Return your deliverable and handoff notes to the coordinator." Recompute any expected totals from their components before handing off. Every stage ends with notes covering results, evidence or artifact paths, assumptions, open issues and next step. Keep a short execution record: agent ID, subagent type, artifact and outcome. Pass notes and evidence to the next stage, not the whole conversation.
3. When production is complete, hand the **actual completed artifact** and its sources to a fresh `jarvis-reviewer`, or `jarvis-analyst` for high-stakes work. The reviewer checks independently, never relying on the producing stage's self-check: sources, calculations, encoding, and rendered pages or slides. It corrects defects, rechecks and returns a verdict. Do not make substantive edits after approval without another review. Do not describe a reused context or an unreviewed result as an independent final review.
4. Keep the project root clean. Every backup or pre-change copy made during a task goes in `backups/`, never in the root or beside the original. Scratch files go in the session scratchpad or `tmp`, and deliverables in `outputs/`. Skip stages that do not apply. Run dependent stages in order, and never let two stages edit the same artifact at once. Any return to an earlier production stage starts a fresh agent, followed by a fresh final review.
5. If a mapped subagent is missing or fails to launch, prepare the brief and evidence, report exactly which stage is blocked, and do not substitute another agent type or claim the stage was completed. A clearly labelled manual adaptation is acceptable only if the user agrees to it.
6. Subagent transcripts stay inside the coordinating session, so there are no stage chats to archive. Deliver final results to the user in this chat, and leave a short handoff in `outputs/handoffs/` when the other assistant's coordinating chat needs to know.

Keep user-facing reporting brief: what was produced, where it is, the review outcome and any remaining issue. Ask before an external, destructive, costly or irreversible action unless the user has already authorized it.

## Maintenance

* This file routes work; the quick-task test itself lives in the Global Rules of [AGENTS.md](../../../AGENTS.md). When that test changes, recheck the quick-route paragraph here in the same change and leave a handoff in `outputs/handoffs/` so the E.V. policy can be updated by its own assistant; never edit E.V.'s policy from here.
* Edits here and in `.claude/agents` take effect only in a session started afterwards ([lessons.md](../../../lessons.md), Claude Code setup).
