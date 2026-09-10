# Install DTMAPI 0.6.1

The current source and Steam-published Runtime report release/API `0.6.1`, file version `0.6.1.0`, and retained assembly compatibility `0.5.3.0`. This installation guide is not a Workshop upload authorization. Public APIs keep their per-surface stability and disposition; the public Abstractions assembly is not uniformly Stable.

## Recommended Player Path

1. Uninstall the old DolocTown SMAPI / DLK runtime if it is installed.
2. Unsubscribe from or disable old DLK/SMAPI feature mods.
3. Restart Steam and Doloc Town so Workshop subscriptions and DLL state are refreshed.
4. Subscribe to the new DTMAPI Runtime item.
5. Open the Runtime item local folder and run `1_install_dtmapi.bat`.
6. Subscribe to the new DTMAPI feature mods.
7. Start the game and open title-page `DTMAPI Settings` to check Status, Mods, Errors, Hooks, Features, and Logs.

The Runtime item installs only the DTMAPI runtime, the single dormant-shipped Compatibility component, tools, and state files. Product Mods remain separate Workshop/local packages selected from the authoritative Catalog; this guide does not duplicate the fast-changing product set.

## What The Installer Writes

The installer writes the runtime bootstrap to:

```text
BepInEx/plugins/DTMAPI/
```

It writes release and install state to:

```text
DTMAPI/release-manifest.json
DTMAPI/install-state.json
DTMAPI/tools/
```

`install-state.json` is UTF-8 without BOM and records the DTMAPI version, numeric binary version, source commit when available, game/plugin paths, files installed, BepInEx state, backups, and legacy detections. It does not store Steam account information.

The current 0.6 source installer does not publish managed Advanced products. The historical generic official-local maintenance transaction still backs up enablement state before an explicitly scoped non-managed fixture publication:

```text
DTMAPI/backups/install-YYYYMMDD-HHMMSS/mod_infos.before.json
```

If the existing `SAVE/mod_infos.json` cannot be read, installation stops instead of overwriting the file.

## Developer Runtime Refresh And Paused Product Modes

Runtime Workshop payload installs skip official-local mods by default. For a source-tree Runtime-only refresh, use:

```powershell
tools/scripts/install-to-game.ps1 -SkipOfficialLocalMods -KeepLegacyMigratedGameMods
```

This refreshes Runtime and its receipts without publishing a product package, changing official enablement, or moving an old receipt-bound `<game>/Mods` deployment.

DTMAPI 0.6.1 retains the pause on these former developer product modes:

```powershell
tools/scripts/install-to-game.ps1 -InstallPublishedModsOnly
tools/scripts/install-to-game.ps1 -InstallAllDevOfficialMods
```

Running either switch, or omitting `-SkipOfficialLocalMods` and thereby selecting the developer product set, returns the managed-product pause error before any Runtime, installer-state, official `MODS`, or `mod_infos.json` mutation. The unreleased Author SDK still targets the retired `<game>/Mods` root, so it cannot supply a current official-`MODS` receipt transaction. Existing `deployment-status`, `install-local-status`, `recover`, `withdraw`, and `source local clear` remain available only for old deployment recovery.

`-LegacyOfficialLocalOnly` exists for bounded maintenance and transaction fixtures which explicitly exclude every managed Author SDK product. It is not a supported way to install the 0.6 first-party Advanced product set and is not release evidence. Current product candidates must use their release/candidate transaction authority until a released SDK owns an official-`MODS` workflow.

The retained generic maintenance transaction is fail-closed for an existing official-local destination. Even if that directory contains legacy `dtmapi-package.json` metadata, it leaves the directory and its enablement entry unchanged instead of overwriting or adopting it. A new non-managed fixture is prepared on the same persistent volume but outside the native `MODS` scan root, then published with a collision-failing directory move. These guarantees do not lift the managed-product pause.

## Legacy Detection

The installer checks for old local content, including:

- `BepInEx/plugins/DolocTownSMAPI`
- `BepInEx/DolocTownSMAPI`
- `BepInEx/plugins/DLKWorkshopBridge`
- local `Mods/Yuuka.DTMAPI.*` migration packages
- local official `MODS/DLK_*` packages
- known old Workshop cache directories

Workshop cache is detected only. DTMAPI does not delete Steam Workshop content. Local old packages may be backed up when they are in the legacy migration path; use `-KeepLegacyMigratedGameMods` when running the PowerShell installer manually if you want to keep them in place.

## Status Check

Run `3_check_dtmapi_status.bat` from the Runtime item folder. It reports:

- DTMAPI runtime present/missing.
- BepInEx present/missing.
- install-state and release manifest present/missing.
- the optional Player Doctor result only when that separate support tool was installed explicitly; its absence is normal and does not make Runtime incomplete.
- read-only detection of ordinary DTMAPI CodeMods misplaced under `BepInEx/plugins`, external BepInEx ownership, and minimum-version blocks; the Doctor does not load, move, delete, enable, disable, or adopt scanned DLLs.
- latest DTMAPI log/report status.
- legacy/non-destructive DTMAPI package metadata marker count; these rows are not installer ownership receipts.
- legacy SMAPI/DLK/local mod detections.

If old items are listed, the safest next step is still to unsubscribe or disable old DLK/SMAPI mods, restart Steam and Doloc Town, and then export a DTMAPI report from the Logs page if support is needed.

When the game cannot load DTMAPI, `3_check_dtmapi_status.bat` remains the offline diagnosis path. `4_collect_dtmapi_logs.bat` collects bounded script/runtime evidence without requiring an EXE or a preinstalled .NET runtime. If Player Doctor was supplied separately, status and collection may include its read-only supplemental report; otherwise they record that the optional helper is not installed and continue normally.
