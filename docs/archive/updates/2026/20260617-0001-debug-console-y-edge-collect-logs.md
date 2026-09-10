# Debug Console Y Edge And Collect Logs

Date: 2026-06-17
Status: verified

## Source

- Player report: `D:\下载\DTMAPI-logs` showed Y-key console enabled but apparently unresponsive.
- Player report: `D:\下载\dtmapi-report-20260616-213923.zip` plus screenshots showed `3_check_dtmapi_status.bat` OK and `4_collect_dtmapi_logs.bat` failing on `$recentRuntimeEvidence.Count`.
- Manual QA/root-cause record: `docs/reviews/manual-qa/2026/20260617-0001-player-y-console-and-collect-log-review.md`.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `testmods/DebugConsoleMod/official-info.json`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/manual-qa/2026/20260617-0001-player-y-console-and-collect-log-review.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Summary

- Suppressed the opener Y edge in the reflected debug console host until the physical Y key is released. This prevents the same key press from opening the console through `DebugConsoleMod.OnButtonPressed` and immediately closing it through `ReflectedDebugConsoleUi.Update()`.
- Fixed `collect-logs.ps1` runtime-evidence summary enumeration by forcing the recent evidence query to an array before checking `.Count`.
- Tightened the external debug-console smoke fallback so the first Y open must reach `Debug console Canvas visible as Y-key console.`, not only the earlier `opened` state log.
- Added `update 0617` to the Y-key console official local/Workshop description in Simplified Chinese, Traditional Chinese, and English.

## Validation

- Passed: `git diff --check` reported line-ending warnings only.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Passed: `collect-logs.ps1` local strict-mode checks for 0, 1, and 2 runtime-evidence child folders; each produced `DTMAPI-evidence-skipped.txt` without `.Count` failure.
- Passed: runtime workshop package was rebuilt to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`; `workshop.json` retains `3743016467`.
- Passed: official local upload package DTMAPI DLL hashes match Release output:
  - `DTMAPI.BepInExBootstrap.dll`: `76F0AE651EEB9074A34F78E79DB9844F1545D43CDF350159642384EB2A513F5A`
  - `DTMAPI.Core.dll`: `F545F7F241C8A18B609825A5C46F3A5D9E7FF4A9106032577B28E5D255540742`
  - `DTMAPI.GameBridge.DolocTown.dll`: `D6CF2F8646D34116A946E8B2DFBDFA7E8FD34C94FC399DDF71BAC08E24B68F2C`
  - `DTMAPI.ModConfigMenu.dll`: `6B14DB55AB289B8812A62EAF749E62B737875F6F54944A2091C202D23DE9E79A`
  - `DTMAPI.Abstractions.dll`: `53FCB8F2F6E247232EAB7CD34664030F8EDDF8A897ADB9F2FBF24BD75CC33884`
- Passed: packaged `Content/DTMAPIInstaller/tools/collect-logs.ps1` normalized text matches `tools/scripts/collect-logs.ps1` (`PackageHash=AAB60B96BB2BF988873AA5DC752DF135F6A591EF4C07CFD1B804069FA9019490`, source text hash differs by release encoding/line endings).
- Passed: after the game was closed, `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -IncludeDebugConsoleMod` installed the runtime and official local packages to the local game. The installed runtime DLL hashes in `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI` match Release output, and `DTMAPI_YKeyConsole\Content\DTMAPI\DTMAPI.YKeyConsole.dll` matches `DebugConsoleMod.dll` Release output (`E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`).
- Passed: local manual test confirmed the Y console opens/closes correctly after the opener-edge fix.
- Passed: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole\info.json` description contains `update 0617`, matches `testmods/DebugConsoleMod/official-info.json`, and retains `workshop_id=3742714442`.
- Passed: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI` runtime upload package still retains `workshop_id=3743016467`; its five runtime DLL hashes match Release output.

## Evidence Notes

- The player log shows repeated `Input Y pressed` -> `Debug console opened ... reason=hotkey Y` -> `Debug console closed reason=Y` -> `Input Y released` sequences, so the issue is an input-edge lifecycle race, not a missing hotkey registration.
- The player report zip is from installed runtime `0.5.1-alpha` and does not contain enough offline evidence to prove the first-launch black-screen cause.

## Rollback

- Revert the `suppressYCloseUntilReleased` state in `ReflectedDebugConsoleUi`.
- Revert the array wrapping in `collect-logs.ps1`.
- Revert the external Y smoke Canvas-visible requirement if it is too strict for older debug-console evidence.

## Follow-Up

- If the first-launch black-screen problem recurs after players reinstall the current runtime, request a fresh `4_collect_dtmapi_logs.bat` package from the fixed collector so startup analysis includes process/fatal/Steam evidence.
- After upload, ask the player who reported the issue to rerun `1_install_dtmapi.bat`, re-enter a save, and retry Y once in a clean session.
