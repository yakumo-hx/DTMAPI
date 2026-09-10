# ISSUE-002: HookProbe left in normal play blocks hotkeys

- State: `mitigated`
- Current boundary: HookProbe/input interaction mitigation retained.

## Status

- Last verified: 2026-05-30
- Severity: medium
- Area: input / config menu / local install hygiene

## Symptom

After entering a save, F8/F10/F11/F6/F9 appear to do nothing. The DTMAPI log still shows the runtime and migrated mods loaded correctly.

## Known Facts

- `HookProbeMod` is a smoke-test mod, not a normal play mod.
- On `SaveLoaded`, HookProbe calls `helper.UI.OpenDtmApiStatusPage`, `OpenModListPage`, `OpenConfigPage`, `OpenErrorPage`, `OpenHookStatusPage`, and `ExportLogs`.
- Those calls leave the DTMAPI UI state open. While `runtime.UI.IsOpen` is true, ordinary gameplay hotkeys are blocked by `UiRuntimeService.BlocksGameplayHotkeys`.
- User evidence on 2026-05-30 showed `HookProbe UI Status OK` through `HookProbe UI ExportLogs OK` immediately after loading save slot/index 4.

## Mitigation

- Moved local game `Mods/DTMAPI.HookProbeMod` to `D:\Steam\steamapps\common\Doloc Town\DTMAPI\backups\disabled-testmods-20260530-1821\DTMAPI.HookProbeMod`.
- Updated `tools/scripts/install-to-game.ps1` so `-IncludeTestMods` no longer installs HookProbe unless `-IncludeHookProbe` is explicitly passed.
- `tools/scripts/run-hook-probe.ps1` still installs HookProbe through `run-game-smoke.ps1 -IncludeHookProbe`.

## Acceptance Check

For normal manual testing:

1. Ensure `D:\Steam\steamapps\common\Doloc Town\Mods\DTMAPI.HookProbeMod` is absent.
2. Launch Doloc Town through Steam.
3. Enter a save.
4. Press F10 or F11 and confirm the matching DTMAPI config page opens, or press F6 and confirm AutoFishing logs a hotkey state change.

Do not mark this solved until manual hotkey evidence is captured without HookProbe installed.
