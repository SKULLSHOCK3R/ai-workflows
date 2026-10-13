# Security

This kit contains instructions (Markdown), three read-only PowerShell helper scripts, and example configuration. It contains no credentials and calls no network services.

## Before you use it

* Read `scripts/*.ps1` before enabling the hooks in `.claude/settings.example.json`. Hooks run commands on your machine with your permissions.
* The rulebook's approval rules are instructions to an AI assistant, not technical enforcement. Configure your tools' own permission settings to match what you are comfortable with.
* Never put real secrets, tokens or credentials in any file in this folder. The `.gitignore` excludes common secret and local-override file names, but check what you commit.
* The model names in the example Codex configuration are illustrative. Verify availability and pricing in your own account before using them; some uses can incur charges.

## Reporting a problem

If you find a security issue in the scripts or a rule that could lead to unsafe behaviour, please open a private security advisory on the repository (GitHub: Security tab, Report a vulnerability) rather than a public issue. Include the file, what you expected and what happened. This is a hobby-scale project maintained on a best-effort basis.
