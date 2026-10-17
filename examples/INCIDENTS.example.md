# Incident log (example)

Copy this file to `outputs/shaka/INCIDENTS.md`. Record breaches and near misses of the rules in [Shaka Ethics.md](../Shaka%20Ethics.md). The coordinator writes the log; stages and the other assistant propose entries; the user may add their own. Do not put secrets, other people's personal data or tool inputs in an entry.

Flow: report, record, propose a fix, get the user's yes for each rule, settings or code change, apply it through the normal workflow, close with the evidence path.

| ID | Date | What happened | Commitment affected | Fix proposed | Status and evidence |
| --- | --- | --- | --- | --- | --- |
| INC-0001 | 2026-01-01 | Example (fictional): a stage drafted an email and tried to send it instead of leaving it as a draft; the deny rule blocked the call. | Consent | Add the connector's send tool to `permissions.deny` and a reviewer check for gated attempts. | Closed. Settings change approved by the user; gate log shows the blocked attempt (tool name and time only). |
