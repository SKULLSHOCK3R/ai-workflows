# GoWTriggers

DualSense adaptive triggers for God of War (2018, PC), through DSX.

GoWTriggers sets the DualSense's L2 and R2 triggers to match what Kratos is doing: a bow tension on R2 with the axe, a galloping pulse with the Blades of Chaos, and a strong rolling vibration on R2 while you aim either one. It works with [DSX by Paliverse](https://store.steampowered.com/app/1812620/DSX/) and is not made or endorsed by Paliverse, Santa Monica Studio or Sony Interactive Entertainment.

**Download:** [GoWTriggers 1.0.0](https://github.com/SKULLSHOCK3R/ai-workflows/releases/tag/gowtriggers-1.0.0) (zip with the exe, source, README and example settings).

## What it does

| State | L2 (aim) | R2 (heavy attack) |
|-|-|-|
| Axe in hand | light resistance | bow tension that snaps back |
| Aiming the axe | firmer resistance | strong rolling vibration |
| Axe thrown | light resistance | light resistance (empty-handed) |
| Aiming with axe thrown | firmer resistance | light resistance |
| Blades of Chaos | light resistance | galloping pulse (the chains) |
| Aiming the Blades | firmer resistance | strong rolling vibration (the spin) |
| Game loading or closed | your DSX profile | your DSX profile |

When the game closes, or you close GoWTriggers, DSX gives the controller back to your own DSX profile.

## How it works, and what it does not do

About 30 times a second GoWTriggers reads a few bytes of the running game's memory (what is in Kratos's hands, whether he is aiming, which weapon) and L2 from the controller, and sends DSX the matching trigger effect over UDP to 127.0.0.1, DSX's published mod interface. It opens the game with read rights only: it never writes to the game, injects nothing, changes no game file and sends nothing off your PC. Some antivirus programs distrust any program that reads another program's memory; the full source is in this branch so you can check it and build it yourself.

It is for single-player play. Do not use trigger mods in online games.

## Requirements

* Windows 10 or 11, 64-bit (.NET Framework 4.8, which Windows already includes)
* God of War on Steam, tested with game build 4757534
* DSX 3 or later with incoming UDP turned on (port 6969)
* A DualSense, with DSX's Controller Emulation set to Xbox 360

## Setup

1. In DSX, turn on incoming UDP (port 6969) and set Controller Emulation to Xbox 360.
2. Unzip GoWTriggers to a folder without a version number, for example `C:\Mods\GoWTriggers`.
3. In Steam, right-click God of War, then Properties, General, Launch Options, and paste:
   `"C:\Mods\GoWTriggers\GoWTriggers.exe" --with-game %command%`
4. Press Play. GoWTriggers starts the game, sets the triggers, and closes with the game.

You can also double-click GoWTriggers.exe before or after starting the game. [README.txt](README.txt) has the full guide: checking DSX takes the commands (`--test`), optional audio haptics, tuning each feel in `GoWTriggers.json`, and removal.

## Build it yourself

With Windows' own C# compiler, nothing to install:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /platform:x64 /reference:System.Web.Extensions.dll /out:GoWTriggers.exe GoWTriggers.cs
GoWTriggers.exe --selftest
```

`Build-Release.ps1` builds, self-tests and packages the release zip.

## Credits and licence

Created by SKULLSHOCK3R (Akshith 'Robin' Naidu): https://skullshock3r.github.io/. Uses the DSX mod interface by [Paliverse](https://github.com/Paliverse/DSX). Released under the MIT License (see [LICENSE](LICENSE)).

This branch holds only GoWTriggers. The `main` branch of this repository is a separate project, the [AI Workflows starter kit](https://github.com/SKULLSHOCK3R/ai-workflows).
