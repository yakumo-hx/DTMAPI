# Install DTMAPI 0.5.0-alpha Developer Preview

DTMAPI `0.5.0-alpha` is a Developer Preview runtime for new DTMAPI mods. It is intended to be easy for players to install and support, but the underlying GameBridge APIs remain Experimental unless the public API matrix says otherwise.

## Recommended Player Path

1. Uninstall the old DolocTown SMAPI / DLK runtime if it is installed.
2. Unsubscribe from or disable old DLK/SMAPI feature mods.
3. Restart Steam and Doloc Town so Workshop subscriptions and DLL state are refreshed.
4. Subscribe to the new DTMAPI Runtime item.
5. Open the Runtime item local folder and run `1_install_dtmapi.bat`.
6. Subscribe to the new DTMAPI feature mods.
7. Start the game and open title-page `DTMAPI Settings` to check Status, Mods, Errors, Hooks, Features, and Logs.

The Runtime item installs only the DTMAPI runtime, tools, and state files. The first player release mods are separate Workshop items: Zoom, ActionSpeed, OneActionComplete, ChestLocatorEnhancer, YKeyConsole, FishBreedingAssistant, MoreSaves, and AnimalHusbandryProgress.

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

installs only the first eight selected release mods as local official packages.

```powershell
tools/scripts/install-to-game.ps1 -InstallAllDevOfficialMods
```

or no install-mode switch keeps the developer default: all current dev official-local DTMAPI packages, including non-release extras such as AutoFishing, StrongPlantingGun, Mine/Oil, Equipment, and Vehicle.

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
- latest DTMAPI log/report status.
- DTMAPI-owned official-local package count.
- legacy SMAPI/DLK/local mod detections.

If old items are listed, the safest next step is still to unsubscribe or disable old DLK/SMAPI mods, restart Steam and Doloc Town, and then export a DTMAPI report from the Logs page if support is needed.
