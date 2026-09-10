# Update 20260531-0006: Chinese lock/conflict text

Date: 2026-05-31

Status: implemented

## Source Request / Goal

- Continue DTMAPI 0.1.12 player-visible cleanup.
- Do not redo official-local packaging, base localization, fish roe display, animal bell evidence, OneAction resource-hit evidence, or AutoFishing wait-phase evidence.
- Close a remaining player-visible UI gap: config conflict messages, official enablement locks, restart-required text, and developer sample fields should be Chinese-first.

## Changed Files

- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
  - Changed keybind conflict text to `按键冲突：...`.
  - Changed blocked-save and locked-page exceptions to Chinese-first text.
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
  - Changed disabled/official enablement reason strings to Chinese-first text.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - Changed config page lock reasons, loaded-then-official-disabled restart warning, EntryDll missing, and missing dependency diagnostics to Chinese-first text.
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
  - Changed official enablement hints to Chinese-first text.
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
  - Updated title-menu reason translation to understand both older English reasons and the new Chinese-first reasons.
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
  - Changed the loaded-disabled reason passed into title UI translation to Chinese-first text.
- `testmods/ConfigMenuExample/ModEntry.cs`
- `testmods/ConfigMenuExample/manifest.json`
  - Marked the dev-only sample config page and fields in Chinese.
- `tests/DTMAPI.UnitTests/Program.cs`
  - Added assertions for Chinese-first conflict, local marker, missing official state, and restart lock text.
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Known Facts / Rejected Hypotheses

- `ConfigMenuExample` and `HelloDtmMod` remain development-only samples; they are allowed during explicit title-menu smoke, then must be removed by formal install.
- The issue was not that the base title DTMAPI menu could not open. The remaining gap was hardcoded English in conflict/lock reasons that can surface inside the player-facing title menu.
- DTMAPI still does not take over official Mod enable/disable ordering. Official-path lock messages now explain the same behavior in Chinese-first wording.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Result: passed, 0 warnings, 0 errors.
  - `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -TimeoutSeconds 150 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Result: passed.
  - Evidence root: `docs/debug/evidence/GAME-SMOKE/20260531-040314`.
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-040354`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\install-to-game.ps1 -SkipBuild`
  - Result: passed after smoke.
  - Moved temporary dev samples to `D:\steam\steamapps\common\Doloc Town\DTMAPI\backups\disabled-testmods-20260531-040359`.
  - Game `Mods` directory was checked empty after reinstall.

## Evidence

- Unit assertions cover:
  - `按键冲突：F9`
  - `本地 DTMAPI 禁用标记`
  - `官方启用状态文件`
  - `重启`
- `docs/debug/evidence/GAME-SMOKE/20260531-040314/result.json`
  - `StartupLog=true`
  - `GameLaunched=true`
  - `TitleSettingsButton=true`
  - `TitleSettingsMenu=true`
  - `NoFatalInstanceWindow=true`
  - `ProcessExited=true`
  - `ForcedClose=false`
- `docs/debug/evidence/GAME-SMOKE/20260531-040314/process-check.txt`
  - `No DolocTown.exe process found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-040314/fatal-window-check.txt`
  - `No fatal instance popup found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-040354/DTMAPI-latest.log`
  - `DTMAPI diagnostics hotkey is disabled by default; use the title-page DTMAPI Settings entry.`
  - `DTMAPI title settings button visible on HomePageUiState.`
  - `Smoke automation opened DTMAPI title settings menu.`
  - `DTMAPI title settings menu opened.`

## Related Records

- Regression matrix: `docs/debug/regressions/smoke-matrix.md` / `CONFIG-001`, `CONFIG-002`, `CONFIG-006`, `UI-005`.
- Hook map: `docs/hook-map/README.md` / `UI.TitleSettingsMenu`.
- Earlier player-visible cleanup records:
  - `20260531-0001-player-visible-hotload-f8-cleanup.md`
  - `20260531-0003-pending-migrated-mod-ui-warnings.md`

## Rollback Notes

- Revert the string changes in Core/ModConfigMenu/BepInExBootstrap if the title UI cannot render the Chinese strings.
- Keep the unit-test assertions updated with whichever player-facing wording is chosen; do not silently return to English-only lock/conflict messages.

## Follow-Up

- `UI-004` still has a separate visual screenshot gap for the title menu position; this update verifies behavior/logs/text ownership but does not add a screenshot.
- Review less common loader error strings during a later error-page pass.
