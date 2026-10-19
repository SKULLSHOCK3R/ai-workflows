"""Lilith: red-team checks of the Shaka gates (a Shaka module).

Checks, against this project (the folder two levels above this script):
  1. Coverage: every tool in outward-tools.json marked deny or ask is gated that way in .claude/settings.json;
     tools marked review are listed for the user if they are not gated at all.
  2. Hook: the PreToolUse matcher covers every denied mcp__ tool (second layer and evidence trail).
  3. Hook answers: Shaka-Gate.ps1 is fed synthetic hook input (no real tool is called) and must answer deny,
     ask for PC control, and deny for empty or broken input. Its log lines carry session id "lilith-test".
  4. Rules: AGENTS.md and CLAUDE.md byte-identical; the fixed Shaka block present in the delegation policy;
     every agent file in .claude/agents present in the policy's routing table.
It never calls a real tool, sends nothing and changes no setting: it reports, and the user decides each fix.
Setup: copy outward-tools.example.json to outward-tools.json (git-ignored) and list your own connectors' tools.
Usage: python -B tools/lilith/lilith.py   (writes outputs/shaka/lilith/lilith-<date>.md; exit 1 on any GAP or FAIL)
"""
import datetime, fnmatch, json, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "outputs", "shaka", "lilith")
SHAKA_BLOCK = '"Shaka (ethics, binding): 1 Consent:'


def load_settings():
    try:
        with open(os.path.join(ROOT, ".claude", "settings.json"), encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        sys.exit("Copy .claude/settings.example.json to .claude/settings.json first.")


def decision(name, deny, ask):
    """Mirror of Claude Code's tool-name rules: exact name or a trailing * wildcard; deny beats ask."""
    hit = lambda rules: any(r == name or ("(" not in r and fnmatch.fnmatchcase(name, r)) for r in rules)
    return "deny" if hit(deny) else "ask" if hit(ask) else "none"


def gate_answer(payload):
    ps = os.path.join(ROOT, "scripts", "Shaka-Gate.ps1")
    r = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps],
                       input=payload, capture_output=True, text=True, timeout=60)
    m = re.search(r'"permissionDecision":"(\w+)"', r.stdout)
    return (m.group(1) if m else "no answer"), r.returncode


def main():
    rows, problems = [], 0
    s = load_settings()
    deny, ask = s.get("permissions", {}).get("deny", []), s.get("permissions", {}).get("ask", [])
    fx = os.path.join(HERE, "outward-tools.json")
    if not os.path.exists(fx):
        fx = os.path.join(HERE, "outward-tools.example.json")
    fixture = json.load(open(fx, encoding="utf-8"))

    # 1. Coverage
    cov, review = [], []
    for prefix, tools in fixture["servers"].items():
        label = tools.get("_name", prefix)
        for tool, want in tools.items():
            if tool.startswith("_"):
                continue
            name, got = prefix + tool, decision(prefix + tool, deny, ask)
            if want == "review":
                if got == "none":
                    review.append("%s: `%s`" % (label, tool))
            elif got != want:
                cov.append("GAP %s: `%s` should be %s, is %s" % (label, tool, want, got)); problems += 1
    for rule, want in fixture["shell"].items():
        if rule not in deny:
            cov.append("GAP shell rule `%s` missing from deny" % rule); problems += 1
    rows.append(("1. Coverage", cov or ["PASS: every deny and ask tool in the fixture is gated as expected."]))

    # 2. Hook matcher
    matchers = [e.get("matcher", "") for e in s.get("hooks", {}).get("PreToolUse", [])]
    miss = [r for r in deny if r.startswith("mcp__") and not any(re.fullmatch(m, r) for m in matchers if m)]
    if not any(re.fullmatch(m, "mcp__computer-use__left_click") for m in matchers if m):
        miss.append("mcp__computer-use__* (PC control)")
    problems += len(miss)
    rows.append(("2. Hook matcher", ["FAIL: hook does not cover `%s`" % m for m in miss] or
                 ["PASS: the PreToolUse matcher covers all %d denied mcp tools and PC control." %
                  sum(r.startswith("mcp__") for r in deny)]))

    # 3. Hook answers (synthetic input only)
    cases = [("denied tool", {"hook_event_name": "PreToolUse", "tool_name": next((r for r in deny if r.startswith("mcp__")), "x")}, "deny"),
             ("PC control", {"hook_event_name": "PreToolUse", "tool_name": "mcp__computer-use__left_click"}, "ask"),
             ("unknown tool", {"hook_event_name": "PreToolUse", "tool_name": "SomethingNew"}, "deny")]
    res = []
    for label, obj, want in cases:
        obj.update({"permission_mode": "lilith-test", "session_id": "lilith-test"})
        got, code = gate_answer(json.dumps(obj))
        ok = got == want and code == 0
        problems += not ok
        res.append("%s %s: answered %s (want %s), exit %d" % ("PASS" if ok else "FAIL", label, got, want, code))
    for label, raw in (("empty input", ""), ("broken JSON", "{not json")):
        got, code = gate_answer(raw)
        ok = got == "deny" and code == 0
        problems += not ok
        res.append("%s %s: answered %s (want deny, fail closed), exit %d" % ("PASS" if ok else "FAIL", label, got, code))
    rows.append(("3. Hook answers (synthetic input; log lines tagged lilith-test)", res))

    # 4. Rules
    rr = []
    a = open(os.path.join(ROOT, "AGENTS.md"), "rb").read(); c = open(os.path.join(ROOT, "CLAUDE.md"), "rb").read()
    rr.append("PASS AGENTS.md and CLAUDE.md byte-identical" if a == c else "FAIL AGENTS.md and CLAUDE.md differ")
    problems += a != c
    pol = open(os.path.join(ROOT, ".claude", "skills", "jarvis-work-delegation", "SKILL.md"), encoding="utf-8").read()
    ok = SHAKA_BLOCK in pol
    rr.append("PASS fixed Shaka block present in the delegation policy" if ok else "FAIL Shaka block missing from the delegation policy")
    problems += not ok
    agents = sorted(f[:-3] for f in os.listdir(os.path.join(ROOT, ".claude", "agents")) if f.endswith(".md"))
    missing = [x for x in agents if "`%s`" % x not in pol]
    problems += len(missing)
    rr.append("FAIL agents not in the routing table: %s" % ", ".join(missing) if missing else
              "PASS all %d agent files appear in the delegation policy (%s)" % (len(agents), ", ".join(agents)))
    rows.append(("4. Rules", rr))

    if review:
        rows.append(("For the user: outward or destructive tools with no gate (review, not counted as failures)", review))

    now = datetime.datetime.now()
    title = "Lilith run, %s, %s" % (now.strftime("%Y-%m-%d"), now.strftime("%I:%M %p").lstrip("0").replace("AM", "a.m.").replace("PM", "p.m."))
    md = ["# " + title, "", "Result: %s" % ("PASS" if not problems else "%d problem(s)" % problems), ""]
    for head, items in rows:
        md += ["## " + head, ""] + ["- " + i for i in items] + [""]
    text = "\n".join(md)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "lilith-%s.md" % now.strftime("%Y-%m-%d"))
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(text)
    print("Saved: " + os.path.relpath(path, ROOT))
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
