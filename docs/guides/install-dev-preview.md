# Install The DTMAPI 0.5.5 Local Candidate

The current source candidate reports release/API `0.5.5`, file version `0.5.5.0`, and retained assembly compatibility `0.5.3.0`. It has not been published as 0.5.5 and this guide does not authorize a Workshop upload. Public APIs keep their per-surface stability and disposition; the public Abstractions assembly is not uniformly Stable.

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

When the installer writes official-local enablement entries during a developer local install, it first backs up:

```text
DTMAPI/backups/install-YYYYMMDD-HHMMSS/mod_infos.before.json
```

If the existing `SAVE/mod_infos.json` cannot be read, installation stops instead of overwriting the file.

## Developer Local Mod Install Modes

Runtime Workshop payload installs skip official-local mods by default. When running PowerShell locally:

```powershell
tools/scripts/install-to-game.ps1 -InstallPublishedModsOnly
```

installs the Catalog's exact current `PublishedProduct` set as local official packages. Managed Advanced products still deploy through their SDK receipts; the installer must not infer or hand-author their identity.

```powershell
tools/scripts/install-to-game.ps1 -InstallAllDevOfficialMods
```

or no install-mode switch keeps the developer default: the Catalog's current developer official-local set without QA fixtures. This can include blocked prototypes and demand samples; it is not a release/publish claim. Retired vehicle research is not an active package.

Every developer local-install mode is fail-closed for an existing official-local destination. Even if that directory contains legacy `dtmapi-package.json` metadata, the installer leaves the directory and its enablement entry unchanged and emits a warning instead of overwriting or adopting it. A new package is prepared on the same persistent volume but outside the native `MODS` scan root, then published with a collision-failing directory move, so a destination that appears during installation is also preserved and an interrupted staging directory cannot be enumerated as an official-local package.

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
- the exact installed Player Doctor files, version, and recorded hashes for Runtime 0.5.5.
- read-only detection of ordinary DTMAPI CodeMods misplaced under `BepInEx/plugins`, external BepInEx ownership, and minimum-version blocks; the Doctor does not load, move, delete, enable, disable, or adopt scanned DLLs.
- latest DTMAPI log/report status.
- legacy/non-destructive DTMAPI package metadata marker count; these rows are not installer ownership receipts.
- legacy SMAPI/DLK/local mod detections.

If old items are listed, the safest next step is still to unsubscribe or disable old DLK/SMAPI mods, restart Steam and Doloc Town, and then export a DTMAPI report from the Logs page if support is needed.

When the game cannot load DTMAPI, `3_check_dtmapi_status.bat` remains the offline diagnosis path. `4_collect_dtmapi_logs.bat` also runs the same bounded read-only Player Doctor and includes `player-doctor.json`, `player-doctor.txt`, and `player-doctor-summary.txt` in the support evidence. A Doctor finding is collected successfully; a missing, crashed, or timed-out helper becomes a warning and does not prevent the remaining logs from being collected.
