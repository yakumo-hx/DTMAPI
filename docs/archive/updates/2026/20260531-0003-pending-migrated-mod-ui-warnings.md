# 20260531-0003: Pending Migrated Mod UI Warnings

## Source Request

- Goal: continue DTMAPI 0.1.12 player-visible fixes without treating migrated config pages as completed gameplay features.
- Focus for this update: clearly mark ActionSpeed, AutoFishing, and OneActionComplete as experimental/pending where real gameplay evidence is still missing.

## Known Facts

- `ACTIONSPEED-001` has no real tool/eat-drink/harvest/machine animation timing evidence yet.
- `AUTOFISH-001` has phase hooks installed, but no real fishing-stage automation evidence yet.
- `ONEACTION-001` has the resource hook installed, but no real tool-hit gameplay evidence yet; fuel and feeder paths remain pending.
- Config page visibility, config save, `Entry`, `UpdateTicked`, or `SaveLoaded` logs do not prove gameplay effect.

## Changes

- `testmods/ActionSpeedMod/ModEntry.cs`
  - Added a top-of-page paragraph warning that ActionSpeed settings are saved and input keys register, but real animation-speed hooks are not verified.
- `testmods/ActionSpeedMod/i18n/schinese.json`
  - Renamed the page to `动作加速（实验/未支持）`.
  - Added explicit Chinese `实验/未支持` warning text.
  - Updated the master switch and animation option tooltips so players do not read them as completed gameplay.
- `testmods/ActionSpeedMod/i18n/english.json`
  - Added matching English fallback text.
- `testmods/AutoFishingMod/ModEntry.cs`
  - Added a top-of-page experimental warning with visible current on/off state.
- `testmods/AutoFishingMod/i18n/schinese.json`
  - Renamed the page to `自动钓鱼（实验/待验证）`.
  - Added Chinese warning text and state labels.
- `testmods/AutoFishingMod/i18n/english.json`
  - Added matching English fallback text.
- `testmods/OneActionCompleteMod/ModEntry.cs`
  - Added a top-of-page warning that real tool-hit evidence is pending and fuel/feeder paths are not verified.
- `testmods/OneActionCompleteMod/i18n/schinese.json`
  - Renamed the page to `一键完成（实验/待验证）`.
  - Added Chinese pending-evidence warning text.
- `testmods/OneActionCompleteMod/i18n/english.json`
  - Added matching English fallback text.
- Docs updated:
  - `docs/debug/regressions/smoke-matrix.md`
  - `docs/updates/INDEX.md`

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1`
  - Passed 2026-05-31.
  - `DTMAPI.UnitTests: OK`.
- Title settings smoke:
  - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-game-smoke.ps1 -TimeoutSeconds 120 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-023249/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-023330/DTMAPI-latest.log`
  - Verified `StartupLog`, `GameLaunched`, `TitleSettingsButton`, `TitleSettingsMenu`, `NoFatalInstanceWindow`, and `ProcessExited`.
  - `process-check.txt` says no `DolocTown.exe` process found.
  - `fatal-window-check.txt` says no fatal instance popup found.
- Built i18n inspection:
  - `testmods/ActionSpeedMod/bin/Release/netstandard2.0/i18n/schinese.json` includes `动作加速（实验/未支持）`.
  - `testmods/AutoFishingMod/bin/Release/netstandard2.0/i18n/schinese.json` includes `自动钓鱼（实验/待验证）`.
  - `testmods/OneActionCompleteMod/bin/Release/netstandard2.0/i18n/schinese.json` includes `一键完成（实验/待验证）`.

## Related Records

- Smoke matrix:
  - `CONFIG-004`
  - `CONFIG-005`
  - `ACTIONSPEED-001`
  - `AUTOFISH-001`
  - `ONEACTION-001`
- Hook map:
  - `Fishing.Automation`
  - `Actions.OneActionComplete`

## Not Fully Verified

- This update does not implement or verify ActionSpeed's real animation effect.
- This update does not implement or verify real AutoFishing automation behavior.
- This update does not verify real OneAction resource/tool-hit behavior.

## Rollback

- Revert the listed `testmods/*` source and i18n changes to remove the warnings.
- If rolling back, keep `ACTIONSPEED-001`, `AUTOFISH-001`, and `ONEACTION-001` pending; do not mark those gameplay features complete without real game evidence.

## Follow-up

- Build a focused ActionSpeed smoke that measures a real tool/eat-drink/harvest animation before and after configuration.
- Build an AutoFishing smoke that verifies hotkey toggle, visible state, and at least one real fishing phase behavior.
- Build a OneAction smoke that verifies a real resource/tool hit path.
