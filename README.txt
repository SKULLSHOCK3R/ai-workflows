GoWTriggers 1.0.0
DualSense adaptive triggers for God of War (2018, PC). Works with DSX ("DSX by Paliverse"); not made or endorsed
by Paliverse.

WHAT IT DOES
While God of War runs, GoWTriggers sets the DualSense's adaptive triggers to match what Kratos is doing:

  State                     L2 (aim)              R2 (heavy attack)
  Axe in hand               light resistance      bow tension that snaps back
  Aiming the axe            firmer resistance     strong rolling vibration (Bow)
  Axe thrown                light resistance      light resistance (empty-handed)
  Aiming with axe thrown    firmer resistance     light resistance
  Blades of Chaos           light resistance      galloping pulse (the chains)
  Aiming the Blades         firmer resistance     strong rolling vibration (Galloping, the spin)
  Game loading or closed    your DSX profile      your DSX profile

The triggers are set once the game is running and stay set; when the game closes, or you close GoWTriggers,
DSX gives the controller back to your own DSX profile. DSX lists GoWTriggers by name in its Mod System feed
(newer DSX builds).

HOW IT WORKS, AND WHAT IT DOES NOT DO
When it finds the game, GoWTriggers reads the header and code of GoW.exe in memory once, to locate the game's
player data by a byte pattern. Then, about 30 times a second, it reads a pointer and three bytes of that data
(what is in Kratos's hands, whether he is aiming, which weapon) and the L2 trigger from the controller, and sends
DSX the matching trigger effect over UDP to 127.0.0.1 (DSX's published mod interface). It listens for DSX's
replies on that same local connection only to tell you when something is wrong (DSX not answering, no controller,
and, on DSX builds that explain refusals, a DSX setting blocking mods). It opens the game with read rights only. It never writes to the game, injects
nothing, changes no game file and sends nothing off your PC. The only file it reads is GoWTriggers.json in its
own folder, if you create one.
With --with-game (setup step 5a) it also starts the game exactly as Steam asked, with Steam's settings unchanged.
The full source code is GoWTriggers.cs in this folder; you can build the exe yourself with the command at the top
of that file (Windows' own C# compiler). Some antivirus programs distrust any program that reads another
program's memory; the source is here so you can check it.

REQUIREMENTS
- Windows 10 or 11, 64-bit (uses .NET Framework 4.8, which Windows already includes)
- God of War on Steam, tested with game build 4757534 (right-click GoW.exe, Properties, Details: the
  Product version starts with GoW-4757534)
- DSX 3 or later (DualSenseX) with incoming UDP turned on, port 6969 (it is off until you turn it on). The
  trigger modes GoWTriggers uses arrived in DSX 3.
- One controller is used: "controller" in GoWTriggers.json, 0 by default; -1 means the controller selected in
  DSX.

SETUP
1. In DSX: turn on incoming UDP (port 6969). Newer DSX builds also have Settings > Networking > Mod system >
   "Let mods control adaptive triggers": leave it on (it is on by default). Builds that report refusals make
   GoWTriggers say so if it is off.
2. In DSX: set Controller Emulation to Xbox 360. God of War needs it to see the controller over Bluetooth, and
   GoWTriggers reads L2 (aiming) from that Xbox 360 controller on any connection. If you have another Xbox
   controller plugged in, unplug it: GoWTriggers reads the first one it finds.
3. Optional, for haptic vibration from the game's sound: in DSX create a profile for God of War and set it as
   God of War's game profile (Installed Games). In that profile, under Haptics | Rumble, turn on audio haptics:
   over Bluetooth, the Bluetooth Audio/Haptic Feedback tab (mode "System Audio Capture -> BT Audio/Haptics");
   over a USB cable, Haptic Feedback Mode > Audio to Haptics. DSX switches the profile on when the game starts.
4. Unzip this folder anywhere. To check DSX takes the commands: open the folder, click the address bar, type
   cmd and press Enter, then type   GoWTriggers.exe --test   and press Enter. R2 goes stiff for 3 seconds,
   and it says "DSX answered." (or what to check if DSX did not answer).
5. Choose how it starts:
   a) With the game, automatically (recommended). In Steam, right-click God of War, Properties, General,
      Launch Options, and paste this line, with the folder where you unzipped GoWTriggers:
        "C:\Mods\GoWTriggers\GoWTriggers.exe" --with-game %command%
      Keep the quotes and %command% exactly as shown. Use a folder name without a version number (for example
      C:\Mods\GoWTriggers) and unzip later versions over it, so the line keeps working. Now pressing Play starts
      GoWTriggers, which starts the game; its window says "God of War found; triggers active." and closes by
      itself when the game closes. To undo it, clear the Launch Options box.
      Steam treats GoWTriggers as the game while it runs: leave its window open until you quit the game.
   b) By hand. Double-click GoWTriggers.exe before or after starting the game. It waits for the game, follows it
      through restarts, and stays open until you close its window (or press Ctrl+C).
   Either way, closing the GoWTriggers window gives the triggers back to your DSX profile and does not close the
   game.

TUNING THE FEELS
Copy GoWTriggers.example.json to GoWTriggers.json beside the exe and change the numbers. Each feel is a DSX mode
and its numbers (positions run from the top of the trigger's travel, 1, to the bottom, 9; strengths 1-8):
  FEEDBACK                     start 1-9, strength 1-8 (resistance from start to the bottom)
  WEAPON                       start 2-7, end 3-8, strength 1-8 (tension between start and end, then gives way)
  VIBRATION                    start 1-9, amplitude 1-8, frequency 1-40
  SLOPE_FEEDBACK               start 1-8, end 2-9, start strength 1-8, end strength 1-8
  MULTIPLE_POSITION_FEEDBACK   ten strengths 0-8, one per region of travel
  MULTIPLE_POSITION_VIBRATION  frequency 1-40, then ten amplitudes 0-8
  OFF                          no numbers (no resistance)
The default feels use the older DSX 2 modes (Resistance, Bow, Galloping; Normal and others also work), which DSX
calls legacy but still takes: they give the strongest feel. Mode names are case-sensitive. A mistake in the file (a misspelt mode,
or a number outside its range) stops GoWTriggers at start with a message saying what is wrong (started with
--with-game, the game still starts, the triggers stay on your DSX profile and the window closes with the game).
From a command prompt in this folder (see setup step 4):
  GoWTriggers.exe --try axe_aim     holds one feel until Ctrl+C, so you can feel it on its own
                                    (axe, axe_aim, thrown, thrown_aim, blades, blades_aim)
  GoWTriggers.exe --watch 30        shows what it reads from the game for 30 seconds and sends nothing

IF IT SAYS "this God of War build is not supported"
A game update changed the code GoWTriggers looks for, so it stops reading the game and the triggers stay on your
DSX profile. If the code is still found but the game's data looks unusual, it says so; the triggers then follow
your DSX profile for anything it does not recognise. Check for a newer GoWTriggers.

REMOVING IT
If you used the Steam launch option (setup step 5a), clear God of War's Launch Options box first, or Play will
look for GoWTriggers and the game will not start. Then delete this folder. Nothing else was installed or changed.
