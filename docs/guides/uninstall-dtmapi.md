# Uninstall DTMAPI

Use `2_uninstall_dtmapi.bat` from the DTMAPI Runtime item folder, or run:

```powershell
tools/scripts/uninstall-dtmapi.ps1
```

The default uninstall only removes DTMAPI-owned runtime files and helper scripts. It backs them up first under:

```text
DTMAPI/backups/uninstall-YYYYMMDD-HHMMSS/
```

It then writes:

```text
DTMAPI/uninstall-state-YYYYMMDD-HHMMSS.json
```

## Default Keep Rules

Default uninstall does not delete:

- player `Mods/`
- Steam Workshop content
- `DTMAPI/reports`
- `DTMAPI/config`
- `DTMAPI/backups`
- `BepInEx/core`
- DTMAPI-owned official-local packages under the user local `MODS` directory

`-RemoveBepInEx` is guarded. It only attempts BepInEx removal when `install-state.json` proves DTMAPI installed BepInEx and the user explicitly asks for removal. BepInEx files are backed up before removal.

## Optional Official-Local Package Removal

Use this only when you want to remove local DTMAPI-generated packages as well as the runtime:

```powershell
tools/scripts/uninstall-dtmapi.ps1 -RemoveOfficialLocalPackages
```

This switch only processes packages with the DTMAPI ownership marker:

```text
Content/DTMAPI/dtmapi-package.json
```

It backs the package folders up under:

```text
DTMAPI/backups/uninstall-YYYYMMDD-HHMMSS/official-local-packages/
```

It also backs up `SAVE/mod_infos.json`, removes matching `Local.<OfficialFolder>` entries, and records `OfficialLocalPackagesDetected`, `OfficialLocalPackagesRemoved`, `ModInfosBackupPath`, and `ModInfosEntriesRemoved` in `uninstall-state-YYYYMMDD-HHMMSS.json`.

Steam Workshop content is never removed by this script.

## Dry Run

Use:

```powershell
tools/scripts/uninstall-dtmapi.ps1 -DryRun
```

Dry run prints the planned backup/removal state and does not move or delete files.

To preview official-local package cleanup too:

```powershell
tools/scripts/uninstall-dtmapi.ps1 -DryRun -RemoveOfficialLocalPackages
```
