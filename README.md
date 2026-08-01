# DTMAPI

DTMAPI is a Doloc Town Modding API prototype for Windows. It uses a BepInEx bootstrap to start DTMAPI Core. Strict CodeMods consume public abstractions according to two per-contract axes: status/stability in the API matrix and the `Open`, `Frozen`, `Diagnostic`, `Internal`, or `Disabled` disposition; the assembly being public does not make every member stable or open to new adoption. Proven SharedNative reflection/Harmony adapters use `DTMAPI.GameBridge.DolocTown`, while managed Advanced CodeMods own separately admitted single-product ProductNative work under the canonical boundary in [`PROJECT.md`](PROJECT.md). The exact admitted-product set and evidence state are maintained only by the [Batch 6 identity contract](docs/architecture/batch6-managed-mod-identity-contract.md); those first-party policies do not open a general Advanced authoring lane or make 0.5.5 releasable.

## Repository Layout

- `src/`: DTMAPI runtime, public abstractions, bootstrap, GameBridge, and config menu projects.
- `first-party-mods/`: remaining ordinary Strict first-party product sources.
- `products/first-party/`: separately admitted managed Advanced ProductNative sources; presence here is not general authoring permission.
- `testmods/`: examples, migrated feature slices, and QA fixtures used for smoke validation.
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
