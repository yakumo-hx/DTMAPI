# DTMAPI

DTMAPI is a Doloc Town Modding API prototype for Windows. It uses a BepInEx bootstrap to start DTMAPI Core, then routes fragile Doloc Town reflection/Harmony work through `DTMAPI.GameBridge.DolocTown` so ordinary mods can target stable public abstractions.

## Repository Layout

- `src/`: DTMAPI runtime, public abstractions, bootstrap, GameBridge, and config menu projects.
- `testmods/`: DTMAPI mod examples and migrated feature slices used for smoke validation.
- `tests/`: unit tests.
- `tools/scripts/`: build, test, install, smoke, hook-probe, and evidence collection scripts.
- `docs/`: public API docs, architecture notes, debug records, reviews, goals, and update records.
- `references/`: public reference docs plus local-only ignored research folders.

## Build

```powershell
tools/scripts/build.ps1 -Configuration Release
tools/scripts/test.ps1 -Configuration Release
```

The scripts resolve a local .NET SDK through `tools/scripts/common.ps1` when the system `dotnet` command has no SDK.

## Local Game Paths

Do not hard-code a local Steam or game install path in repository files. Use one of these local-only mechanisms instead:

- `DTMAPI_GAME_DIR`
- `local.settings.json`
- script path resolution based on the resolved game directory

`local.settings.json` is intentionally ignored by Git.

## Source Boundaries

DTMAPI is a clean rebuild. Doloc Town reverse data, official Workshop docs, third-party mods, and SMAPI are reference material only. Do not publish official game DLLs, copied decompiled source, private debug evidence, or third-party mod binaries as DTMAPI source.
