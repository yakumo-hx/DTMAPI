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
- every official-local/content package under the user local `MODS` directory
- every entry in `SAVE/mod_infos.json`

`-RemoveBepInEx` is guarded. It only attempts BepInEx removal when `install-state.json` proves DTMAPI installed BepInEx and the user explicitly asks for removal. BepInEx files are backed up before removal.

## Runtime-Only Ownership Boundary

The player uninstaller never scans `MODS`, never reads `dtmapi-package.json` as ownership, never moves an official-local/content package, and never edits `SAVE/mod_infos.json`. Steam Workshop content is also never removed.

`dtmapi-package.json` in an older package is legacy/build metadata only. It is not an installer receipt and cannot authorize overwrite, removal, adoption, or enablement changes. The retired `-RemoveOfficialLocalPackages` switch is accepted only for compatibility: it emits a warning, performs no package scan or enablement change, and continues with Runtime-only uninstall.

Future verified package cleanup belongs to the independent Author SDK/deployment toolchain and requires a package-local receipt matching external transaction state. The player Runtime uninstaller does not provide that operation.

## Dry Run

Use:

```powershell
tools/scripts/uninstall-dtmapi.ps1 -DryRun
```

Dry run prints the planned backup/removal state and does not move or delete files.
