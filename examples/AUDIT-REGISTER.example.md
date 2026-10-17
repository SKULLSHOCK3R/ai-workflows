# Audit register (example)

Copy this file to `outputs/shaka/AUDIT-REGISTER.md` and replace the example row. One row per outward path: anything that sends a message, calls a model or service, commands a device, spends money or moves a file off this computer. See [Shaka Ethics.md](../Shaka%20Ethics.md), "Apps and scripts you build".

| # | Outward path | What leaves or what it does | Guard | Owner | Last checked |
| --- | --- | --- | --- | --- | --- |
| 1 | Example: `notify.py` (fictional) | One plain-text alert to the user's own phone | Fixed recipient, rate limit of one alert per 10 minutes, refuses text containing addresses or links | Coordinator | 2026-01-01 |

Rules:

* Add a row in the same change that adds the path. A stage that finds an unlisted path reports it to the coordinator.
* "Guard" names the control that stops misuse: a per-action yes, a fixed recipient, an allowlist or a spending ceiling.
* Re-check a row whenever its code or settings change, and update "Last checked".
