# Lesson details

Full text behind lesson lines shortened to about 400 bytes (AGENTS.md, Maintenance). Each section is headed by its lesson's date and scope, and the lesson line points here. Read with [lessons.md](lessons.md); not loaded at startup.

## 2026-10-13 | tools (Windows PowerShell 5.1 file writes)

Write text files with an explicit UTF-8 encoder (`[IO.File]::WriteAllText` with `New-Object System.Text.UTF8Encoding($false)`) and byte-compare against the source: the 5.1 defaults re-encode non-ASCII characters into garbage. Always pass absolute paths to `[IO.File]` methods, because .NET resolves relative paths against the process folder, not the `Set-Location` folder, so a failed read followed by a write can silently create an empty file somewhere else.

## 2026-10-13 | tools (Claude Code hooks and rule loading)

Hook output added to context is capped at about 10,000 characters; longer output is saved to a file and only a short preview reaches the model, so a hook that dumps whole rule files silently drops most of them. Load core rule files through `@path` imports in CLAUDE.md instead (escape spaces with a backslash; quoted paths are not imported) and keep any hook's printed output well under the cap.

## 2026-10-13 | general (Git with parallel sessions)

While another session has pending changes, commit with explicit paths (`git add -- <paths>`, then `git commit -- <paths>`), never `git add -A` or `-u` on broad folders, and check for `.git/index.lock` first. To stop tracking a file, run `git rm --cached <path>` and commit with no pathspec: a path-limited commit re-stages the file from disk and records an update instead of the removal.

## Maintenance

* When a lesson line is shortened, move its full text here in the same change, under a heading with its date and scope, and end the line with "Details: [Lesson Details.md](Lesson%20Details.md)." (from a file under `lessons/`, link `../Lesson%20Details.md`) Merge or remove a section when its lesson is merged, promoted or removed.
