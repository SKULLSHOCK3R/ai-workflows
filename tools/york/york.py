"""York: read-only resource watch (a Shaka module).

Prints one status report: GPU (if nvidia-smi is present), a local Ollama server (if one is running),
free disk space, running Python jobs (Windows only), and a reminder that Claude plan usage is read by
the assistant, not by scripts. It also writes the same facts to <project root>/state/york/status.json
and status.html (state/ is git-ignored).

Read only: it starts, stops and sends nothing. Its only network call is GET to Ollama on 127.0.0.1.
Usage: python -B tools/york/york.py [--json]
"""
import datetime, html, json, os, re, shutil, subprocess, sys, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STATE = os.path.join(ROOT, "state", "york")
OLLAMA = "http://127.0.0.1:11434"


def clock(dt):
    """12-hour time, for people."""
    return dt.strftime("%I:%M %p").lstrip("0").replace("AM", "a.m.").replace("PM", "p.m.")


def run(cmd):
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=20, encoding="utf-8", errors="replace")
        return r.stdout.strip() if r.returncode == 0 else None
    except Exception:
        return None


def gpu():
    out = run(["nvidia-smi", "--query-gpu=name,memory.used,memory.total,utilization.gpu,temperature.gpu",
               "--format=csv,noheader,nounits"])
    if not out:
        return {"ok": False, "note": "no NVIDIA GPU found (nvidia-smi not available)"}
    try:
        name, used, total, util, temp = [x.strip() for x in out.splitlines()[0].split(",")]
        return {"ok": True, "name": name, "mem_used_mib": int(used), "mem_total_mib": int(total),
                "util_pct": int(util), "temp_c": int(temp)}
    except ValueError:
        return {"ok": False, "note": "nvidia-smi output not understood"}


def ollama():
    def get(path):
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))   # never via a proxy
        with opener.open(OLLAMA + path, timeout=5) as r:
            return json.loads(r.read().decode("utf-8"))
    try:
        ver = get("/api/version").get("version")
        loaded = [{"name": m.get("name"), "vram_gib": round(m.get("size_vram", 0) / 2**30, 1)}
                  for m in get("/api/ps").get("models", [])]
        return {"ok": True, "version": ver, "loaded": loaded}
    except Exception as e:
        return {"ok": False, "note": "not reachable on 127.0.0.1:11434 (%s)" % type(e).__name__}


def disks():
    res = []
    for d in sorted({os.path.splitdrive(ROOT)[0] + os.sep, os.path.abspath(os.sep)}):
        try:
            u = shutil.disk_usage(d)
            res.append({"drive": d, "free_gib": round(u.free / 2**30, 1), "total_gib": round(u.total / 2**30, 1),
                        "free_pct": round(100 * u.free / u.total, 1)})
        except OSError:
            res.append({"drive": d, "note": "not available"})
    return res


def python_jobs():
    """Script names (and short plain arguments) of running python.exe processes; Windows only."""
    if os.name != "nt":
        return []
    out = run(["powershell", "-NoProfile", "-Command",
               "Get-CimInstance Win32_Process -Filter \"Name='python.exe'\" | ForEach-Object { $_.CommandLine }"])
    jobs = []
    for line in (out or "").splitlines():
        m = re.search(r"([\w\-]+\.py)\b(.*)$", line)
        if m:
            args = [a for a in m.group(2).split() if re.fullmatch(r"[\w.\-]{1,12}", a)][:3]
            jobs.append(" ".join([m.group(1)] + args))
    return jobs


def report(s):
    r = ["York status, %s" % s["time"], ""]
    g = s["gpu"]
    r.append("GPU: " + ("%s, %d of %d MiB used, %d%% busy, %d C" % (g["name"], g["mem_used_mib"], g["mem_total_mib"],
                                                                     g["util_pct"], g["temp_c"]) if g["ok"] else g["note"]))
    o = s["ollama"]
    r.append("Ollama: " + ("v%s, loaded: %s" % (o["version"], ", ".join("%s (%s GiB)" % (m["name"], m["vram_gib"])
                                                                         for m in o["loaded"]) or "none")
                           if o["ok"] else o["note"]))
    r.append("Disks: " + "; ".join("%s %s GiB free of %s (%s%%)" % (d["drive"], d["free_gib"], d["total_gib"], d["free_pct"])
                                   if "free_gib" in d else "%s %s" % (d["drive"], d["note"]) for d in s["disks"]))
    r.append("Python jobs: " + ("; ".join(s["python_jobs"]) or "none"))
    r.append("Claude plan usage: ask the assistant; no script can read it.")
    return "\n".join(r)


def main():
    now = datetime.datetime.now()
    s = {"time": clock(now) + now.strftime(", %Y-%m-%d"), "gpu": gpu(), "ollama": ollama(), "disks": disks(),
         "python_jobs": python_jobs()}
    text = report(s)
    os.makedirs(STATE, exist_ok=True)
    with open(os.path.join(STATE, "status.json"), "w", encoding="utf-8") as f:
        json.dump(s, f, indent=1)
    with open(os.path.join(STATE, "status.html"), "w", encoding="utf-8") as f:
        f.write("<!doctype html><meta charset=utf-8><title>York status</title>"
                "<body style='font:15px/1.5 Segoe UI,sans-serif;background:#111;color:#eee;padding:16px'>"
                "<pre style='white-space:pre-wrap'>%s</pre>" % html.escape(text))
    print(json.dumps(s, indent=1) if "--json" in sys.argv else text)


if __name__ == "__main__":
    main()
