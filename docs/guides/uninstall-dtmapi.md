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

`-RemoveBepInEx` is guarded. It only attempts BepInEx removal when `install-state.json` proves DTMAPI installed BepInEx and the user explicitly asks for removal. BepInEx files are backed up before removal.

## Dry Run

Use:

```powershell
tools/scripts/uninstall-dtmapi.ps1 -DryRun
```

Dry run prints the planned backup/removal state and does not move or delete files.
