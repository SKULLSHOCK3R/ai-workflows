// GoWTriggers - DualSense adaptive triggers for God of War (2018, PC) through DSX.
//
// What it does: about 30 times a second it READS a few bytes of the running game's memory (what is in Kratos's
// hands, whether he is aiming, which weapon) and L2 from the controller, and sends DSX the matching trigger effect
// over UDP to 127.0.0.1 (DSX's published mod interface, "DSX by Paliverse"). It reads DSX's replies on the same
// local socket to report problems. It never writes to the game, injects nothing and changes no game file. Nothing
// leaves this PC.
//
// Build (Windows' own C# compiler, nothing to install):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /platform:x64
//     /reference:System.Web.Extensions.dll /out:GoWTriggers.exe GoWTriggers.cs
//
// Run: GoWTriggers.exe [--test] [--try STATE] [--watch SECONDS] [--selftest]
//      GoWTriggers.exe --with-game %command%   (as the game's Steam launch option: starts the game, runs while it
//                                               runs, exits when it closes)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace GoWTriggers
{
    static class Native
    {
        public const uint PROCESS_QUERY_INFORMATION = 0x0400, PROCESS_VM_READ = 0x0010;   // read rights only
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, IntPtr size, out IntPtr read);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeProcess(IntPtr h, out uint code);
        public delegate bool CtrlHandler(uint ctrlType);
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(CtrlHandler handler, bool add);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XInputState
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LX, LY, RX, RY;
    }

    static class XInput14 { [DllImport("xinput1_4.dll")] public static extern uint XInputGetState(uint i, ref XInputState s); }
    static class XInput910 { [DllImport("xinput9_1_0.dll")] public static extern uint XInputGetState(uint i, ref XInputState s); }

    static class Pad
    {
        static int which = 0;   // 0 untried, 1 xinput1_4, 2 xinput9_1_0, -1 none
        /// <summary>L2 0-255 from the first connected XInput pad (DSX's virtual Xbox 360 controller), or -1.</summary>
        public static int LeftTrigger()
        {
            for (uint i = 0; i < 4; i++)
            {
                XInputState s = new XInputState();
                uint r = 1;
                if (which == 0 || which == 1)
                {
                    try { r = XInput14.XInputGetState(i, ref s); which = 1; }
                    catch (DllNotFoundException) { which = 2; }
                }
                if (which == 2)
                {
                    try { r = XInput910.XInputGetState(i, ref s); }
                    catch (DllNotFoundException) { which = -1; }
                }
                if (which == -1) return -1;
                if (r == 0) return s.LeftTrigger;
            }
            return -1;
        }
    }

    class Config
    {
        public int DsxPort = 6969, Controller = 0, PollHz = 30, L2Threshold = 30;
        public double ResendSeconds = 2.0;
        public bool XInputAim = true;
        // DSX's TriggerMode numbers (Models.cs in Paliverse's DSX Mod SDK). The DSX 3 modes (20-26) are the current
        // ones; the DSX 2 names are still accepted so older GoWTriggers.json files load, but the SDK marks them
        // "Avoid Using".
        public Dictionary<string, int> Modes = new Dictionary<string, int> {
            {"OFF", 20}, {"FEEDBACK", 21}, {"WEAPON", 22}, {"VIBRATION", 23}, {"SLOPE_FEEDBACK", 24},
            {"MULTIPLE_POSITION_FEEDBACK", 25}, {"MULTIPLE_POSITION_VIBRATION", 26},
            {"Normal", 0}, {"Rigid", 7}, {"VibrateTrigger", 8}, {"Resistance", 13}, {"Bow", 14},
            {"Galloping", 15}, {"SemiAutomaticGun", 16}, {"AutomaticGun", 17}, {"Machine", 18} };
        // The feels: [mode, numbers...] for L2 and R2. Tunable in GoWTriggers.json beside the exe; try one on its
        // own with --try. "unknown" (loading, menus, game closed) is not a feel: it hands the
        // controller back to the user's own DSX profile.
        public Dictionary<string, object[][]> Feels = new Dictionary<string, object[][]> {
            // Legacy DSX 2 modes, which DSX 3 still takes: in play testing they gave the strongest, best-liked feel
            // (Bow and Galloping give the rolling R2 vibration while aiming)
            {"axe",        new[] { new object[] {"Resistance", 2, 3}, new object[] {"Bow", 1, 6, 4, 4} }},
            {"axe_aim",    new[] { new object[] {"Resistance", 1, 5}, new object[] {"Bow", 1, 5, 6, 8} }},
            {"thrown",     new[] { new object[] {"Resistance", 2, 2}, new object[] {"Resistance", 3, 1} }},
            {"thrown_aim", new[] { new object[] {"Resistance", 1, 4}, new object[] {"Resistance", 3, 1} }},
            {"blades",     new[] { new object[] {"Resistance", 2, 3}, new object[] {"Galloping", 1, 8, 3, 5, 12} }},
            {"blades_aim", new[] { new object[] {"Resistance", 1, 4}, new object[] {"Galloping", 1, 8, 3, 5, 12} }} };

        /// <summary>The numbers each DSX 3 mode takes, as {lowest, highest} per number, from the SDK's protocol guide.
        /// DSX turns a trigger off when a value is out of range, so a mistake is caught here at start instead.</summary>
        public static readonly Dictionary<string, int[][]> Ranges = new Dictionary<string, int[][]> {
            {"OFF", new int[0][]},
            {"FEEDBACK", new[] { new[] {1, 9}, new[] {1, 8} }},
            {"WEAPON", new[] { new[] {2, 7}, new[] {3, 8}, new[] {1, 8} }},
            {"VIBRATION", new[] { new[] {1, 9}, new[] {1, 8}, new[] {1, 40} }},
            {"SLOPE_FEEDBACK", new[] { new[] {1, 8}, new[] {2, 9}, new[] {1, 8}, new[] {1, 8} }},
            {"MULTIPLE_POSITION_FEEDBACK", Repeat(new[] {0, 8}, 10, null)},
            {"MULTIPLE_POSITION_VIBRATION", Repeat(new[] {0, 8}, 10, new[] {1, 40})} };

        static int[][] Repeat(int[] range, int n, int[] first)
        {
            var r = new List<int[]>();
            if (first != null) r.Add(first);
            for (int i = 0; i < n; i++) r.Add(range);
            return r.ToArray();
        }

        public const string ModId = "GoWTriggers";
        public const long SteamAppId = 1593500;    // God of War (2018) on Steam

        /// <summary>Defaults, overridden by GoWTriggers.json if present. Every value is checked here, so a mistake
        /// in the file stops the program at start with a clear message instead of in the middle of play.</summary>
        public static Config Load(string path)
        {
            Config c = new Config();
            if (File.Exists(path))
            {
                var d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8));
                object v;
                if (d.TryGetValue("dsx_port", out v)) c.DsxPort = Convert.ToInt32(v);
                if (d.TryGetValue("controller", out v)) c.Controller = Convert.ToInt32(v);
                if (d.TryGetValue("xinput_aim", out v)) c.XInputAim = Convert.ToBoolean(v);
                if (d.TryGetValue("xinput_l2_threshold", out v)) c.L2Threshold = Convert.ToInt32(v);
                if (d.TryGetValue("feels", out v))
                {
                    var feels = v as Dictionary<string, object>;
                    if (feels == null) throw new Exception("\"feels\" must be an object");
                    foreach (var kv in feels)
                    {
                        if (kv.Key == "unknown")
                        {
                            Console.WriteLine("Note: GoWTriggers.json's \"unknown\" feel is ignored: loading and menus now give your own DSX profile back.");
                            continue;
                        }
                        var f = kv.Value as Dictionary<string, object>;
                        if (f == null || !f.ContainsKey("left") || !f.ContainsKey("right"))
                            throw new Exception("feel \"" + kv.Key + "\" needs \"left\" and \"right\"");
                        c.Feels[kv.Key] = new[] { ToArr(f["left"], kv.Key), ToArr(f["right"], kv.Key) };
                    }
                }
            }
            if (c.DsxPort < 1 || c.DsxPort > 65535) throw new Exception("dsx_port must be 1 to 65535");
            if (c.Controller < -1 || c.Controller > 255) throw new Exception("controller must be -1 (the one selected in DSX) to 255");
            if (c.L2Threshold < 0 || c.L2Threshold > 254) throw new Exception("xinput_l2_threshold must be 0 to 254");
            foreach (var kv in c.Feels) Logic.Packet(c, kv.Key);   // throws on an unknown mode or a bad number
            return c;
        }

        static object[] ToArr(object o, string feel)
        {
            var list = o as System.Collections.ArrayList;
            if (list == null || list.Count == 0) throw new Exception("feel \"" + feel + "\": each side needs [mode, numbers...]");
            return list.ToArray();
        }
    }

    static class Logic
    {
        /// <summary>hands (0 axe on back, 1 weapon drawn, 2 axe thrown, 3 Blades), aim (7 aiming), flag (1 axe in
        /// hand, 0 Blades or axe away) -> a feel name; anything unreadable or unexpected is "unknown".</summary>
        public static string StateName(int hands, int aim, int flag)
        {
            if (hands < 0 || aim < 0) return "unknown";
            string b;
            if (hands == 2) b = "thrown";                 // the flag also reads 0 while the axe is thrown
            else if (flag == 0) b = "blades";
            else if (flag == 1) b = "axe";
            else if (hands == 0 || hands == 1) b = "axe";
            else if (hands == 3) b = "blades";
            else return "unknown";
            return aim == 7 ? b + "_aim" : b;
        }

        /// <summary>True when the three bytes look like this game's player data.</summary>
        public static bool Plausible(int hands, int aim, int flag)
        {
            return hands >= 0 && hands <= 3 && (aim == 0 || aim == 7) && (flag == 0 || flag == 1);
        }

        public static int Aim(int aimByte, int l2, Config c)
        {
            if (c.XInputAim && l2 >= 0 && l2 > c.L2Threshold) return 7;
            return aimByte;
        }

        /// <summary>The message for a feel. Every message names the mod and the game (ModId, AppId: DSX shows them in
        /// its Mod System feed; older DSX builds ignore them). A state with no feel ("unknown") sends
        /// ResetToUserSettings (type 7), which gives the controller back to the user's own DSX profile.</summary>
        public static string Packet(Config c, string state)
        {
            // "Diagnostics":true asks DSX to explain anything it did not apply (Paliverse's UDP example; older DSX
            // builds ignore it)
            var sb = new StringBuilder("{\"ModId\":\"" + Config.ModId + "\",\"AppId\":" + Config.SteamAppId
                                       + ",\"Diagnostics\":true,\"instructions\":[");
            object[][] feel;
            if (!c.Feels.TryGetValue(state, out feel))
                return sb.Append("{\"type\":7,\"parameters\":[").Append(c.Controller).Append("]}]}").ToString();
            for (int side = 0; side < 2; side++)
            {
                object[] f = feel[side];
                string mode = f[0] as string;
                if (mode == null || !c.Modes.ContainsKey(mode))
                    throw new Exception("feel \"" + state + "\": unknown mode \"" + f[0] + "\" (use " + string.Join(", ", c.Modes.Keys) + ")");
                int[][] ranges;
                bool checkedMode = Config.Ranges.TryGetValue(mode, out ranges);
                if (checkedMode && f.Length - 1 != ranges.Length)
                    throw new Exception("feel \"" + state + "\": " + mode + " takes " + ranges.Length + " numbers, not " + (f.Length - 1));
                sb.Append(side == 0 ? "" : ",");
                sb.Append("{\"type\":1,\"parameters\":[").Append(c.Controller).Append(',').Append(side + 1)
                  .Append(',').Append(c.Modes[mode]);
                for (int i = 1; i < f.Length; i++)
                {
                    int n;
                    try { n = Convert.ToInt32(f[i]); } catch (Exception) { throw new Exception("feel \"" + state + "\": \"" + f[i] + "\" is not a number"); }
                    if ((f[i] is decimal && (decimal)f[i] != n) || (f[i] is double && (double)f[i] != n))
                        throw new Exception("feel \"" + state + "\": " + f[i] + " is not a whole number");
                    int lo = checkedMode ? ranges[i - 1][0] : 0, hi = checkedMode ? ranges[i - 1][1] : 255;
                    if (n < lo || n > hi)
                        throw new Exception("feel \"" + state + "\": " + mode + " number " + i + " is " + n + ", outside " + lo + " to " + hi);
                    sb.Append(',').Append(n);
                }
                sb.Append("]}");
            }
            return sb.Append("]}").ToString();
        }

        /// <summary>What a DSX reply is worth telling the user (empty when nothing). Newer DSX builds list what they did not
        /// apply in "Diagnostics" (codes from the SDK's protocol guide); older builds send only a status.</summary>
        public static string[] ReplyNotes(Dictionary<string, object> reply)
        {
            var notes = new List<string>();
            object v;
            if (reply.TryGetValue("isControllerConnected", out v) && v is bool && !(bool)v)
                notes.Add("DSX sees no controller connected.");
            if (reply.TryGetValue("Diagnostics", out v) && v is System.Collections.ArrayList)
                foreach (object o in (System.Collections.ArrayList)v)
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    object code, msg;
                    d.TryGetValue("Code", out code); d.TryGetValue("Message", out msg);
                    string c = code == null ? "" : code.ToString();
                    string hint = c == "DisabledByUser" ? " In DSX, turn on Settings > Networking > Mod system > Let mods control adaptive triggers."
                                : c == "NotActivated" ? " DSX is not activated for controller output right now."
                                : c == "NoController" ? " Connect the controller, or set \"controller\" in GoWTriggers.json."
                                : "";
                    notes.Add("DSX did not apply part of a message (" + c + (msg == null ? "" : ": " + msg) + ")." + hint);
                }
            return notes.ToArray();
        }

        /// <summary>Index of a byte pattern (-1 = wildcard) in buf, or -1.</summary>
        public static int Find(byte[] buf, int[] pat)
        {
            for (int i = 0; i + pat.Length <= buf.Length; i++)
            {
                if (buf[i] != pat[0]) continue;
                bool ok = true;
                for (int k = 1; k < pat.Length && ok; k++) ok = pat[k] < 0 || buf[i + k] == pat[k];
                if (ok) return i;
            }
            return -1;
        }
    }

    class Game
    {
        // Code that fills the game's object list (found on build 4757534, each exactly once there): the 32-bit
        // displacement at byte 8 of each pattern points at the list (RIP = pattern start + 12). The player record
        // is slot 2 of that list; the offsets below are for that build's record layout.
        static readonly int[][] Patterns = {
            new[] {0x83,0xFA,0x20,0x7D,0x13,0x4C,0x8D,0x05,-1,-1,-1,-1,0x49,0x89,0x0C,0xD0,0xFF,0xC2,0x89,0x15},
            new[] {0x83,0xF9,0x20,0x7D,0x13,0x48,0x8D,0x15,-1,-1,-1,-1,0x48,0x89,0x3C,0xCA,0xFF,0xC1,0x89,0x0D} };
        const int HandsOff = 0x198, AimOff = 0xF45C, FlagOff = 0x96B8, PlayerSlot = 2;
        const int PatternTries = 15;          // about 30 s of retries while a starting game unpacks its code

        IntPtr h = IntPtr.Zero;
        long slotAddr = 0;
        int lastPid = 0, patternMisses = 0, moduleMisses = 0;
        public bool Unsupported = false;
        public string Problem = "";

        byte[] Read(long addr, int n)
        {
            var buf = new byte[n];
            IntPtr got;
            if (Native.ReadProcessMemory(h, new IntPtr(addr), buf, new IntPtr(n), out got) && got.ToInt64() == n) return buf;
            return null;
        }

        public bool Attach()
        {
            Problem = "";
            Process[] ps = Process.GetProcessesByName("GoW");
            if (ps.Length == 0) { lastPid = 0; Unsupported = false; patternMisses = moduleMisses = 0; return false; }
            Process p = ps[0];
            if (p.Id != lastPid) { lastPid = p.Id; Unsupported = false; patternMisses = moduleMisses = 0; }
            if (Unsupported) return false;                   // already refused this run of the game: no rescans
            long baseAddr;
            try { baseAddr = p.MainModule.BaseAddress.ToInt64(); }
            catch (Exception)
            {
                if (++moduleMisses == 5)
                    Problem = "Found God of War but cannot read it yet. If the game runs as administrator, run GoWTriggers as administrator too.";
                return false;
            }
            h = Native.OpenProcess(Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_VM_READ, false, p.Id);
            if (h == IntPtr.Zero) { Problem = "Could not open God of War for reading."; return false; }
            long list = 0;
            try { list = FindList(baseAddr); } catch (Exception) { list = 0; }
            if (list == 0)
            {
                Detach();
                if (++patternMisses >= PatternTries)
                {
                    Unsupported = true;
                    Problem = "This God of War build is not supported (its code pattern was not found). GoWTriggers will not read this game; triggers stay on your DSX profile.";
                }
                return false;
            }
            slotAddr = list + 8 * PlayerSlot;
            return true;
        }

        /// <summary>The list address from the patterns in the code sections; 0 if not found or if the two
        /// patterns disagree.</summary>
        long FindList(long baseAddr)
        {
            byte[] hdr = Read(baseAddr, 0x1000);
            if (hdr == null) return 0;
            int pe = BitConverter.ToInt32(hdr, 0x3C);
            if (pe < 0 || pe > 0xE00) return 0;
            int nsec = BitConverter.ToUInt16(hdr, pe + 6), opt = BitConverter.ToUInt16(hdr, pe + 20);
            long[] found = { 0, 0 };
            for (int s = 0; s < nsec && pe + 24 + opt + s * 40 + 40 <= hdr.Length; s++)
            {
                int o = pe + 24 + opt + s * 40;
                uint chars = BitConverter.ToUInt32(hdr, o + 36);
                if ((chars & 0x20000000) == 0) continue;                    // code sections only
                int va = BitConverter.ToInt32(hdr, o + 12), vsize = BitConverter.ToInt32(hdr, o + 8);
                for (long off = 0; off < vsize; off += 0x400000)
                {
                    int n = (int)Math.Min(0x400000 + 64, vsize - off);
                    byte[] chunk = Read(baseAddr + va + off, n);
                    if (chunk == null) continue;
                    for (int k = 0; k < 2; k++)
                    {
                        if (found[k] != 0) continue;
                        int i = Logic.Find(chunk, Patterns[k]);
                        if (i < 0) continue;
                        found[k] = baseAddr + va + off + i + 12 + BitConverter.ToInt32(chunk, i + 8);
                    }
                }
            }
            if (found[0] != 0 && found[1] != 0 && found[0] != found[1]) return 0;   // disagreement: do not guess
            return found[0] != 0 ? found[0] : found[1];
        }

        public bool Alive()
        {
            uint code;
            return h != IntPtr.Zero && Native.GetExitCodeProcess(h, out code) && code == 259;
        }

        public void Detach()
        {
            if (h != IntPtr.Zero) Native.CloseHandle(h);
            h = IntPtr.Zero;
        }

        /// <summary>hands, aim, flag (-1 when unreadable, for example while loading).</summary>
        public int[] Values()
        {
            int[] r = { -1, -1, -1 };
            byte[] p = Read(slotAddr, 8);
            if (p == null) return r;
            long rec = BitConverter.ToInt64(p, 0);
            if (rec == 0) return r;
            int[] offs = { HandsOff, AimOff, FlagOff };
            for (int i = 0; i < 3; i++) { byte[] b = Read(rec + offs[i], 1); if (b != null) r[i] = b[0]; }
            return r;
        }
    }

    static class Program
    {
        const string Version = "1.0.0";
        // bound to the loopback address only: sends to and hears from this PC alone. Made in StartListening, after
        // --with-game has started the game, so a socket problem can never keep the game from starting.
        static UdpClient udp = null;
        static volatile bool lastWasReset = false;   // a reset can draw no "blocked" note, so its quiet reply proves nothing
        static Config cfg;
        static volatile bool stop = false;
        static volatile bool sending = false;     // only after a feel has been sent is there anything to release
        static Native.CtrlHandler handler;        // kept in a field so the garbage collector does not free it
        static readonly object sendLock = new object();
        static int replies = 0;                   // DSX replies heard (Interlocked)
        static string lastNotes = "";
        static Stopwatch firstSend = null;

        static void Raw(string state)
        {
            if (udp == null) return;
            byte[] b = Encoding.ASCII.GetBytes(Logic.Packet(cfg, state));
            lastWasReset = !cfg.Feels.ContainsKey(state);
            try { udp.Send(b, b.Length, "127.0.0.1", cfg.DsxPort); } catch (Exception) { }
            if (firstSend == null) firstSend = Stopwatch.StartNew();
        }

        /// <summary>Listens for DSX's replies on the same local socket, only from 127.0.0.1 and DSX's port, and prints
        /// what a reply says is wrong, once per change. The replies never change what is sent.</summary>
        static void StartListening()
        {
            udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            try { udp.Client.IOControl(unchecked((int)0x9800000C), new byte[4], null); } catch (Exception) { }   // ignore "port closed" resets
            udp.Client.ReceiveTimeout = 500;
            var t = new Thread(() =>
            {
                var json = new JavaScriptSerializer();
                while (!stop)
                {
                    byte[] data;
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    try { data = udp.Receive(ref from); }
                    catch (SocketException) { continue; }
                    catch (ObjectDisposedException) { return; }
                    if (!IPAddress.IsLoopback(from.Address) || from.Port != cfg.DsxPort) continue;
                    Interlocked.Increment(ref replies);
                    Dictionary<string, object> reply;
                    try { reply = json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(data)); }
                    catch (Exception) { continue; }
                    if (reply == null) continue;
                    string[] notes = Logic.ReplyNotes(reply);
                    string joined = string.Join("\n", notes);
                    if (joined == lastNotes || (joined == "" && lastWasReset)) continue;
                    if (joined != "") Console.WriteLine(joined);
                    else if (lastNotes != "") Console.WriteLine("DSX is taking the triggers again.");
                    lastNotes = joined;
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        static string NoAnswer()
        {
            return "No answer from DSX on port " + cfg.DsxPort + ". Is DSX running, with incoming UDP turned on (port " + cfg.DsxPort + ")?";
        }

        /// <summary>Send a feel; once stopping has begun nothing but the release goes out (a feel sent by the
        /// loop must not land after the close handler's release).</summary>
        static void Send(string state)
        {
            lock (sendLock)
            {
                if (stop && state != "unknown") return;
                Raw(state);
                if (state != "unknown") sending = true;
            }
        }

        static void Release()
        {
            lock (sendLock)
            {
                if (sending && cfg != null) { Raw("unknown"); sending = false; }
            }
        }

        /// <summary>Ctrl+C, Ctrl+Break, closing the window, logging off or shutting down: release the triggers.
        /// Console.CancelKeyPress alone misses closing the window.</summary>
        static bool OnCtrl(uint type)
        {
            lock (sendLock) { stop = true; }     // after this, the loop can no longer send a feel
            Release();
            return type <= 1;   // Ctrl+C or Break: let the loop finish; close/logoff/shutdown: Windows ends us
        }

        static int Fail(string message)
        {
            Console.WriteLine(message);
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Console.WriteLine("Press Enter to close.");
                Console.ReadLine();
            }
            return 2;
        }

        /// <summary>The raw text after --with-game in this program's own command line: the game command exactly as
        /// Steam wrote it, with no parse-and-requote round trip.</summary>
        public static string GameCommand(string commandLine)
        {
            string s = commandLine.TrimStart();
            int end = s.StartsWith("\"") ? s.IndexOf('"', 1) : s.IndexOf(' ');    // skip our own exe
            if (end < 0) return "";
            int at = s.IndexOf("--with-game", end, StringComparison.Ordinal);
            return at < 0 ? "" : s.Substring(at + "--with-game".Length).Trim();
        }

        /// <summary>Split a game command into the program and its arguments. A quoted program is taken as quoted; an
        /// unquoted one (spaces in the path, if a launcher leaves it bare) is the shortest prefix that names an
        /// existing .exe, so a path such as C:\Program Files\...\GoW.exe still starts the game.</summary>
        public static string[] SplitGame(string command, Func<string, bool> exists)
        {
            if (command.StartsWith("\""))
            {
                int q = command.IndexOf('"', 1);
                if (q < 0) return new[] { command.Substring(1), "" };
                return new[] { command.Substring(1, q - 1), command.Substring(q + 1).Trim() };
            }
            for (int i = 0; i <= command.Length; i++)
            {
                if (i < command.Length && command[i] != ' ') continue;
                string head = command.Substring(0, i);
                if (head.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && exists(head))
                    return new[] { head, command.Substring(i).Trim() };
            }
            int sp = command.IndexOf(' ');
            return sp < 0 ? new[] { command, "" } : new[] { command.Substring(0, sp), command.Substring(sp + 1).Trim() };
        }

        /// <summary>--with-game: start the game command Steam passes (%command%) with this program's environment,
        /// so Steam's own variables reach the game. The game is a separate process: stopping GoWTriggers never stops it.</summary>
        static Process StartGame(string[] game)
        {
            var psi = new ProcessStartInfo(game[0], game[1]);
            psi.UseShellExecute = false;
            string gameDir = Path.GetDirectoryName(game[0]);
            if (!string.IsNullOrEmpty(gameDir)) psi.WorkingDirectory = gameDir;
            return Process.Start(psi);
        }

        /// <summary>With --with-game, after a problem: keep the window (and its message) until the game has closed,
        /// so it never outlives the game or waits for Enter.</summary>
        static void WaitForGameToClose(Process launched)
        {
            while (!stop)
            {
                bool running = !launched.HasExited;
                if (!running)
                    foreach (var p in Process.GetProcessesByName("GoW")) { running = true; p.Dispose(); }
                if (!running) return;
                Thread.Sleep(500);
            }
        }

        static int Main(string[] args)
        {
            Console.WriteLine("GoWTriggers " + Version + " - God of War adaptive triggers through DSX (reads game memory only)");
            handler = OnCtrl;
            Native.SetConsoleCtrlHandler(handler, true);
            // --with-game starts the game before anything else, so a settings mistake can never keep it from starting
            Process launched = null;
            if (args.Length > 0 && args[0] == "--with-game")
            {
                string command = GameCommand(Environment.CommandLine);
                Console.WriteLine("Game command from Steam: " + command);
                if (command == "" || command == "%command%")
                    return Fail("--with-game needs the game after it. In Steam's launch options for God of War use:\n"
                                + "  \"<this folder>\\GoWTriggers.exe\" --with-game %command%");
                string[] game = SplitGame(command, File.Exists);
                // a failed start keeps Fail's pause on purpose: no game is running, so the message is the only clue
                try { launched = StartGame(game); }
                catch (Exception e) { return Fail("Could not start the game (" + game[0] + "): " + e.Message); }
                Console.WriteLine("Started the game; GoWTriggers closes by itself when the game closes.");
            }
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            try { cfg = Config.Load(Path.Combine(dir, "GoWTriggers.json")); }
            catch (Exception e)
            {
                if (launched == null) return Fail("GoWTriggers.json has a problem: " + e.Message);
                // with the game: no "Press Enter" pause, so the window still closes with the game
                Console.WriteLine("GoWTriggers.json has a problem: " + e.Message + "\nThe triggers stay on your DSX profile; this window closes with the game.");
                WaitForGameToClose(launched);
                return 2;
            }
            try
            {
                if (args.Length > 0 && args[0] == "--selftest") return SelfTest();
                StartListening();
                if (launched != null) { Run(0, launched); return 0; }
                if (args.Length > 0 && args[0] == "--test")
                {
                    cfg.Feels["test"] = new[] { new object[] {"OFF"}, new object[] {"FEEDBACK", 1, 8} };
                    Send("test"); Console.WriteLine("R2 should now be stiff for 3 seconds: press it.");
                    for (int i = 0; i < 30 && !stop; i++) Thread.Sleep(100);
                    Console.WriteLine(Interlocked.CompareExchange(ref replies, 0, 0) > 0 ? "DSX answered." : NoAnswer());
                    Release(); Console.WriteLine("Back to your own DSX profile.");
                    return 0;
                }
                if (args.Length > 0 && args[0] == "--try")
                {
                    if (args.Length < 2 || !cfg.Feels.ContainsKey(args[1]) || args[1] == "unknown")
                        return Fail("Choose a feel to try: axe, axe_aim, thrown, thrown_aim, blades, blades_aim");
                    Console.WriteLine("Holding the '" + args[1] + "' feel. Ctrl+C or close the window to stop.");
                    bool told = false;
                    var since = Stopwatch.StartNew();
                    while (!stop)
                    {
                        Send(args[1]);
                        for (int i = 0; i < 10 && !stop; i++) Thread.Sleep(100);
                        if (!told && since.Elapsed.TotalSeconds > 2 && Interlocked.CompareExchange(ref replies, 0, 0) == 0)
                        { Console.WriteLine(NoAnswer()); told = true; }
                    }
                    Release();
                    return 0;
                }
                int watch = 0;
                if (args.Length > 0 && args[0] == "--watch" && (args.Length < 2 || !int.TryParse(args[1], out watch) || watch < 1))
                    return Fail("Use --watch SECONDS, for example --watch 30");
                if (args.Length > 0 && args[0] != "--watch") return Fail("Unknown option. Use --test, --try FEEL, --watch SECONDS, --with-game %command% or no option.");
                Run(watch, null);
                return 0;
            }
            catch (Exception e)
            {
                Release();
                if (launched == null) return Fail("GoWTriggers stopped: " + e.Message);
                Console.WriteLine("GoWTriggers stopped: " + e.Message + "\nThe triggers are back on your DSX profile; this window closes with the game.");
                WaitForGameToClose(launched);
                return 2;
            }
        }

        /// <summary>The main loop. With launched (--with-game) it ends when the game closes, or, if the game was never
        /// recognised, when the process Steam asked for has exited.</summary>
        static void Run(int watchSeconds, Process launched)
        {
            bool watching = watchSeconds > 0;
            Console.WriteLine(watching ? "Watching only (sends nothing) for " + watchSeconds + " s."
                                       : "Waiting for God of War; sending to DSX on port " + cfg.DsxPort + ". Ctrl+C or close the window to stop.");
            var game = new Game();
            string current = null, lastProblem = "";
            bool attached = false, warned = false;
            var since = Stopwatch.StartNew();
            var resend = Stopwatch.StartNew();
            var odd = new Stopwatch();
            var grace = new Stopwatch();
            bool noAnswerSaid = false;
            while (!stop && (!watching || since.Elapsed.TotalSeconds < watchSeconds))
            {
                if (!noAnswerSaid && firstSend != null && firstSend.Elapsed.TotalSeconds > 5
                    && Interlocked.CompareExchange(ref replies, 0, 0) == 0)
                { Console.WriteLine(NoAnswer()); noAnswerSaid = true; }
                if (!attached)
                {
                    // the process Steam started has exited before the game was found: allow 60 s in case it was a
                    // stub or the game restarted itself, then stop
                    if (launched != null && launched.HasExited)
                    {
                        if (!grace.IsRunning) grace.Start();
                        else if (grace.Elapsed.TotalSeconds > 60) break;
                    }
                    if (game.Attach()) { attached = true; Console.WriteLine("God of War found; " + (watching ? "watching." : "triggers active.")); }
                    else
                    {
                        if (game.Problem != "" && game.Problem != lastProblem) { Console.WriteLine(game.Problem); lastProblem = game.Problem; }
                        for (int i = 0; i < 20 && !stop; i++) Thread.Sleep(100);
                    }
                    continue;
                }
                if (!game.Alive())
                {
                    if (!watching) Release();
                    game.Detach(); attached = false; current = null; lastProblem = "";
                    if (launched != null) { Console.WriteLine("God of War closed; triggers back on your DSX profile."); return; }
                    Console.WriteLine("God of War closed; triggers back on your DSX profile. Waiting for the game.");
                    continue;
                }
                int[] v = game.Values();
                // values that do not look like this game's player data for 10 s in a row: warn once (game
                // updated in a way the pattern did not catch); unknown values already give normal triggers
                if (v[0] >= 0 && !Logic.Plausible(v[0], v[1], v[2])) { if (!odd.IsRunning) odd.Restart(); }
                else odd.Reset();
                if (!warned && odd.Elapsed.TotalSeconds > 10)
                {
                    Console.WriteLine("The game's values look unusual (hands=" + v[0] + " aim=" + v[1] + " flag=" + v[2] + "). This build may not be fully supported.");
                    warned = true;
                }
                int l2 = Pad.LeftTrigger();
                int aim = v[1] < 0 ? -1 : Logic.Aim(v[1], l2, cfg);
                string state = Logic.StateName(v[0], aim, v[2]);
                if (watching && state != current)
                    Console.WriteLine(string.Format("{0,6:F2} s  hands={1} aim={2} flag={3} L2={4} -> {5}",
                        since.Elapsed.TotalSeconds, v[0], v[1], v[2], l2, state));
                if (state != current || resend.Elapsed.TotalSeconds >= cfg.ResendSeconds)
                {
                    if (!watching)
                    {
                        if (state == "unknown") Release(); else Send(state);
                        if (state != current) Console.WriteLine("feel: " + state);
                    }
                    current = state; resend.Restart();
                }
                Thread.Sleep(1000 / cfg.PollHz);
            }
            if (attached) { if (!watching) Release(); game.Detach(); }
            Console.WriteLine(watching ? "Done watching." : launched != null && launched.HasExited
                              ? "The game closed; GoWTriggers is closing." : "Stopped; triggers back on your DSX profile.");
        }

        static int SelfTest()
        {
            int pass = 0, fail = 0;
            Action<string, bool> check = (name, ok) => { if (ok) pass++; else fail++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); };
            var c = new Config();
            check("axe, axe aiming, thrown, Blades", Logic.StateName(0, 0, 1) == "axe" && Logic.StateName(1, 7, 1) == "axe_aim"
                  && Logic.StateName(2, 0, 0) == "thrown" && Logic.StateName(1, 0, 0) == "blades" && Logic.StateName(3, 7, 0) == "blades_aim");
            check("unreadable -> unknown", Logic.StateName(-1, -1, -1) == "unknown" && Logic.StateName(9, 0, 5) == "unknown");
            check("L2 past the threshold aims; no pad never aims", Logic.Aim(0, 200, c) == 7 && Logic.Aim(0, 10, c) == 0
                  && Logic.Aim(7, -1, c) == 7 && Logic.Aim(0, -1, c) == 0);
            const string id = "{\"ModId\":\"GoWTriggers\",\"AppId\":1593500,\"Diagnostics\":true,\"instructions\":[";
            check("axe_aim message: Resistance and Bow, named mod and game", Logic.Packet(c, "axe_aim") ==
                  id + "{\"type\":1,\"parameters\":[0,1,13,1,5]},{\"type\":1,\"parameters\":[0,2,14,1,5,6,8]}]}");
            check("axe R2 is the Bow", Logic.Packet(c, "axe").EndsWith("{\"type\":1,\"parameters\":[0,2,14,1,6,4,4]}]}"));
            check("blades_aim R2 is the legacy Galloping", Logic.Packet(c, "blades_aim").EndsWith(
                  "{\"type\":1,\"parameters\":[0,2,15,1,8,3,5,12]}]}"));
            check("unknown hands the controller back to the user's DSX profile", Logic.Packet(c, "unknown") ==
                  id + "{\"type\":7,\"parameters\":[0]}]}");
            bool allValid = true;
            foreach (var kv in c.Feels) { try { Logic.Packet(c, kv.Key); } catch (Exception) { allValid = false; } }
            check("every default feel is a known mode, DSX 3 ones within their ranges", allValid && !c.Feels.ContainsKey("unknown")
                  && Array.TrueForAll(new List<object[][]>(c.Feels.Values).ToArray(), f => c.Modes.ContainsKey((string)f[0][0]) && c.Modes.ContainsKey((string)f[1][0])));
            Func<object[], bool> refused = side =>
            {
                var cx = new Config(); cx.Feels["axe"] = new[] { side, new object[] {"OFF"} };
                try { Logic.Packet(cx, "axe"); return false; } catch (Exception) { return true; }
            };
            check("DSX 3 ranges and counts enforced", refused(new object[] {"FEEDBACK", 0, 3}) && refused(new object[] {"FEEDBACK", 1, 9})
                  && refused(new object[] {"WEAPON", 2, 9, 4}) && refused(new object[] {"VIBRATION", 1, 8, 41}) && refused(new object[] {"FEEDBACK", 1})
                  && refused(new object[] {"OFF", 1}) && refused(new object[] {"FEEDBACK", 2.5m, 3}) && !refused(new object[] {"FEEDBACK", 2.0m, 3}) && !refused(new object[] {"VIBRATION", 9, 8, 40}) && !refused(new object[] {"WEAPON", 7, 8, 1}));
            var legacy = new Config(); legacy.Feels["axe"] = new[] { new object[] {"Resistance", 2, 3}, new object[] {"Bow", 1, 6, 4, 4} };
            check("older GoWTriggers.json mode names still load", Logic.Packet(legacy, "axe") ==
                  id + "{\"type\":1,\"parameters\":[0,1,13,2,3]},{\"type\":1,\"parameters\":[0,2,14,1,6,4,4]}]}");
            var js = new JavaScriptSerializer();
            string[] none = Logic.ReplyNotes(js.Deserialize<Dictionary<string, object>>(
                "{\"Status\":\"DSX Received UDP Instructions\",\"isControllerConnected\":true,\"BatteryLevel\":80}"));
            string[] two = Logic.ReplyNotes(js.Deserialize<Dictionary<string, object>>(
                "{\"isControllerConnected\":false,\"Diagnostics\":[{\"Instruction\":0,\"Code\":\"DisabledByUser\",\"Message\":\"x\"}]}"));
            check("DSX replies: quiet when fine, explained when not", none.Length == 0 && two.Length == 2
                  && two[0].Contains("no controller") && two[1].Contains("DisabledByUser") && two[1].Contains("Let mods control adaptive triggers"));
            byte[] buf = { 1, 2, 0x83, 0xFA, 0x20, 0x7D, 0x13, 0x4C, 0x8D, 0x05, 9, 9, 9, 9, 0x49, 0x89, 0x0C, 0xD0, 0xFF, 0xC2, 0x89, 0x15 };
            check("pattern finder with wildcards", Logic.Find(buf, new[] {0x83,0xFA,0x20,0x7D,0x13,0x4C,0x8D,0x05,-1,-1,-1,-1,0x49,0x89,0x0C,0xD0,0xFF,0xC2,0x89,0x15}) == 2);
            bool badMode = false, badNum = false;
            var c2 = new Config();
            c2.Feels["axe"] = new[] { new object[] {"resistance", 1}, new object[] {"Normal"} };
            try { Logic.Packet(c2, "axe"); } catch (Exception) { badMode = true; }
            c2.Feels["axe"] = new[] { new object[] {"Resistance", 300}, new object[] {"Normal"} };
            try { Logic.Packet(c2, "axe"); } catch (Exception) { badNum = true; }
            check("a misspelt mode or an out-of-range number is refused", badMode && badNum);
            check("plausible player data", Logic.Plausible(1, 7, 0) && !Logic.Plausible(9, 0, 0) && !Logic.Plausible(1, 3, 1));
            check("read rights only", (Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_VM_READ) == 0x410);
            check("game command taken verbatim after --with-game",
                  GameCommand("\"C:\\M o\\GoWTriggers.exe\" --with-game \"D:\\G W\\GoW.exe\" -x \"a b\"") == "\"D:\\G W\\GoW.exe\" -x \"a b\""
                  && GameCommand("GoWTriggers.exe --with-game") == "" && GameCommand("GoWTriggers.exe") == "");
            Func<string, bool> onlyGow = p => p == "D:\\God of War\\GoW.exe";
            string[] q = SplitGame("\"D:\\God of War\\GoW.exe\" -x \"a b\"", onlyGow);
            string[] bare = SplitGame("D:\\God of War\\GoW.exe -x", onlyGow);
            check("game found whether Steam quotes its path or not", q[0] == "D:\\God of War\\GoW.exe" && q[1] == "-x \"a b\""
                  && bare[0] == "D:\\God of War\\GoW.exe" && bare[1] == "-x");
            Console.WriteLine(pass + " pass, " + fail + " fail");
            return fail == 0 ? 0 : 1;
        }
    }
}
