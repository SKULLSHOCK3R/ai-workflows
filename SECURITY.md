# Security

This kit contains instructions (Markdown), eight PowerShell helper scripts, two small Python tools and example configuration. The startup check and the persona card only read; the gate hook writes only its own log in `state/`; the sync script only checks by default and writes (`CLAUDE.md`, plus a backup in `backups/`) only when run with `-Sync -ReviewedDifference`. The three launchers start another assistant's command-line tool on your own sign-in, send the brief and whatever that stage reads to that tool's vendor, and write run records in `outputs/stage-records/`. `Apply-ModelEdits.ps1` changes project files only with `-Apply`, after backing each one up. The kit contains no credentials. Its network calls are York's request to a local Ollama server and, if you use them, the launchers' command-line tools.

## Before you use it

* Read `scripts/*.ps1` before running them or enabling the hooks in `.claude/settings.example.json`. Hooks run commands on your machine with your permissions.
* The rulebook's approval rules are instructions to an AI assistant, not technical enforcement. Configure your tools' own permission settings to match what you are comfortable with.
* Read the launchers and the Antigravity settings examples before enabling them. Some of the Antigravity CLI's tools (web search, scheduling, messaging, task management, subagents, questions to the user) could not be switched off by settings in the author's tests; the launcher flags a run that used one, after the fact. Pin that CLI's hash and the settings files yourself, prove each deny rule with a canary, and re-check after every update. Codex edit mode stays off until you have tested it on your own Codex build.
* Never put real secrets, tokens or credentials in any file in this folder. The `.gitignore` excludes common secret and local-override file names, but check what you commit.
* The model names in the example Codex configuration and the agent files are illustrative. Verify availability and pricing in your own account before using them; some uses can incur charges.

## Reporting a problem

If you find a security issue in the scripts or a rule that could lead to unsafe behaviour, please open a private security advisory on the repository (GitHub: Security tab, Report a vulnerability) rather than a public issue. Include the file, what you expected and what happened. This is a hobby-scale project maintained on a best-effort basis.
