# Update 20260531-0007: Title UI screenshot evidence

Date: 2026-05-31

Status: implemented

## Source Request / Goal

- Continue DTMAPI 0.1.12 player-visible cleanup without repeating already closed official-local packaging, base localization, fish roe display, animal bell evidence, OneAction resource-hit evidence, or AutoFishing wait-phase evidence.
- Close the remaining `UI-004` gap: title-page DTMAPI position and menu localization needed visual evidence, not just log evidence.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - Added smoke-only title UI screenshots under `DTMAPI/evidence/UI-004/<timestamp>/`.
  - Captures `title-settings-button.png` before opening the title settings menu, then captures `title-settings-menu.png` after the menu renders.
  - Records `Smoke.TitleSettingsButtonScreenshot` and `Smoke.TitleSettingsMenuScreenshot` hook statuses.
- `tools/scripts/run-game-smoke.ps1`
  - Waits for title button and title menu screenshot log lines during `-AutoOpenTitleSettingsMenu`.
  - Verifies the screenshot PNG files referenced by those log lines exist before marking smoke passed.
  - Adds screenshot log and file-existence checks to `result.json`.
- `docs/debug/regressions/smoke-matrix.md`
  - Promoted `UI-004` from partial visual evidence to verified screenshot evidence.
- `docs/hook-map/README.md`
  - Linked title button/menu screenshot evidence to `UI.TitleSettingsEntry` and `UI.TitleSettingsMenu`.
- `docs/updates/INDEX.md`

## Known Facts / Rejected Hypotheses

- The title DTMAPI menu already opened through the Steam smoke path; this update did not redesign that UI.
- `ConfigMenuExample` is still a dev-only smoke sample. It is included only for explicit title-menu smoke and removed again by formal install.
- DTMAPI still does not take over official Mod enable/disable/order controls; the title menu remains status/config/log/error/hook UI.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Result: passed, 0 warnings, 0 errors.
  - `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 150 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Result: passed.
  - Evidence root: `docs/debug/evidence/GAME-SMOKE/20260531-042021`.
  - Collected logs/screenshots: `docs/debug/evidence/GAME-SMOKE/20260531-042239`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\install-to-game.ps1 -SkipBuild`
  - Result: passed after smoke.
  - Moved temporary dev samples to `D:\steam\steamapps\common\Doloc Town\DTMAPI\backups\disabled-testmods-20260531-042301`.
  - Game `Mods` directory was checked empty after reinstall.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260531-042021/result.json`
  - `StartupLog=true`
  - `GameLaunched=true`
  - `TitleSettingsButton=true`
  - `TitleSettingsButtonScreenshot=true`
  - `TitleSettingsButtonScreenshotFile=true`
  - `TitleSettingsMenu=true`
  - `TitleSettingsMenuScreenshot=true`
  - `TitleSettingsMenuScreenshotFile=true`
  - `NoFatalInstanceWindow=true`
  - `ProcessExited=true`
  - `ForcedClose=false`
- `docs/debug/evidence/GAME-SMOKE/20260531-042021/process-check.txt`
  - `No DolocTown.exe process found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-042021/fatal-window-check.txt`
  - `No fatal instance popup found.`
- `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-latest.log`
  - `DTMAPI title settings button visible on HomePageUiState.`
  - `Title settings button screenshot OK screenshot=...title-settings-button.png.`
  - `Smoke automation opened DTMAPI title settings menu.`
  - `DTMAPI title settings menu opened.`
  - `Title settings menu screenshot OK screenshot=...title-settings-menu.png.`
- Visual screenshots:
  - `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-button.png`
  - `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-menu.png`
- Screenshot summary:
  - `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/summary.txt`

## Related Records

- Regression matrix: `docs/debug/regressions/smoke-matrix.md` / `UI-004`.
- Hook map: `docs/hook-map/README.md` / `UI.TitleSettingsEntry`, `UI.TitleSettingsMenu`.
- Previous text cleanup: `20260531-0006-chinese-lock-conflict-text.md`.

## Rollback Notes

- If `UnityEngine.ScreenCapture.CaptureScreenshot` proves unreliable for title-page smoke, revert the screenshot wait gates in `run-game-smoke.ps1` first so title-menu functional smoke remains usable.
- The runtime screenshot capture is smoke-only and should not affect ordinary play unless `DTMAPI/smoke-settings.json` explicitly enables title-menu automation.

## Follow-Up

- `UI-004` is now visually verified.
- Broader migrated-mod gameplay gaps remain separate: `ACTIONSPEED-001` real timing is still pending, AutoFishing full automation remains pending beyond wait-phase `InstantBite`, and OneAction fuel/feeder paths remain pending.
