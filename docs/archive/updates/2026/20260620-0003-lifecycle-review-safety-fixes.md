# 20260620-0003 Lifecycle Review Safety Fixes

## Summary

Applied the code-review findings from the first lifecycle cleanup pass, then re-reviewed the result with parallel safety tracks. This narrows high-frequency `EnvironmentReset` handling from destructive cleanup into lightweight refresh, isolates GameBridge environment-reset steps from each other, throttles repeated lifecycle failures, bounds crash-report export size, and changes AutoFishing's pending-cast watchdog from a fail-stop state into a diagnosed short-backoff retry. A second safety pass fixed remaining P1 risks around per-frame feature dispatch allocation, EquipmentSlots stale inactive UI discovery, AutoFishing synchronous phase confirmation being overwritten by pending status, and crash dumps being silently skipped by collection budgets.

This still does not claim the long-run Unity/Mono GC crash is solved. It reduces DTMAPI-owned lifecycle risk and improves the next log package's ability to show whether counts remain bounded.

## Source Request

User request: fix all issues found in the parallel lifecycle safety review, and avoid patch-style workarounds.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/collect-logs.ps1`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Behavior

- `NotifyGameBridgeFeaturesEnvironmentReset` now isolates EquipmentSlots refresh, runtime automation refresh, feature fanout, and lifecycle counter publication. One failure records diagnostics and degraded hook status, but no longer prevents the rest of the environment-reset fanout from running. Repeated failures now use first/short/interval summary publishing instead of writing on every camera reset.
- GameBridge feature fanout no longer allocates a new feature array for every high-frequency `Update` / `EnvironmentReset` dispatch.
- `UpdateRuntimeAutomation` is no longer forced on every `EnvironmentReset`; it uses normal throttling instead of bypassing machine-poll intervals from `DolocAPI.SetEnvCamera`.
- `Runtime.LifecycleRetentionCounters` publishes on the first three environment resets and then by time interval, removing the previous every-100-resets publication path.
- SaveSlots no longer destroys official save UI pager state on high-frequency `EnvironmentReset`. Save/title cleanup still clears retained state, and cleanup now tries to restore active official save-panel slot visibility before releasing pager roots and binders.
- EquipmentSlots no longer destroys active UI clones/binders on high-frequency `EnvironmentReset`. Save/title cleanup still clears clones and binders, while environment reset only refreshes status flags. Rendering now requires a loaded archive and an active `AccessoriesBar` host, so `FindObjectsOfTypeAll` cannot rebuild extra-slot UI from stale or inactive title-return objects. If all providers are disabled, stale UI clones are cleared; render state is marked false before rebuild and only restored after successful clone creation.
- AnimalViewer no longer treats every high-frequency `EnvironmentReset` as a destructive clone-session boundary. SaveLoaded/ReturnedToTitle still clear clone sessions, while EnvironmentReset validates the current parent/clone/session and only clears already-invalid state.
- AutoFishing now marks pending-cast before invoking native `BodyController.UseFishRod`, so synchronous native phase progression can clear the marker in the same call. If the native call synchronously advances to a fishing phase, DTMAPI keeps the verified phase status instead of overwriting it with `pending`. If no native fishing phase/minigame progress arrives before the watchdog timeout, DTMAPI publishes `needs-review`, releases the pending marker, and enters a short backoff instead of permanently blocking auto-cast. Native `UseFishRod` return is now an attempt; the auto-cast count is confirmed only after a real fishing phase/wait hook arrives.
- Disabling AutoFishing now clears transient minigame handles, ready-charge handles, pending cast, backoff, and animation-speed snapshots. `EnvironmentReset` no longer performs a full AutoFishing runtime reset.
- In-game and offline log export now bound Unity crash report collection by directory count, per-directory file count, total file count, per-file bytes, and total bytes. Crash collection prioritizes `error.log`, `Player.log`, `Player-prev.log`, and `crash.dmp`, gives the first crash dump a larger explicit budget, writes skipped-file reasons into `Unity-Crashes/summary.txt`, and writes `Unity-Crashes/MISSING-CRASH-DUMP-README.txt` when a dump is too large or fails to copy so the player can send it separately. File limits count considered files instead of only copied files, avoiding unbounded traversal when many oversized files are present.
- Offline `collect-logs.ps1` now continues collecting LocalLow, Temp crash, Steam-tail, process, and fatal-window evidence when the game directory cannot be resolved. The Workshop root `4_collect_dtmapi_logs.bat` desktop-output path now falls back to `%TEMP%\DTMAPI-logs` when the desktop cannot be created or written.

## Validation

Source validation run so far:

- `git diff --check`
- `tools/scripts/test.ps1 -Configuration Release`
- Windows PowerShell 5.1 parse of `tools/scripts/collect-logs.ps1`
- Temporary offline collector run with no resolved game folder; output still created `Unity-Crashes/summary.txt` and recorded the crash collection budget fields.
- Unit coverage for a too-large fake `crash.dmp` now verifies `Unity-Crashes/MISSING-CRASH-DUMP-README.txt` is written instead of silently losing the dump.
- Temporary offline collector test with missing `DTMAPI_GAME_DIR` and fake `%TEMP%\RedSawGames\DolocTown\Crashes` evidence; output included `Unity-Crashes/summary.txt` with fake `error.log` and `crash.dmp`.
- Temporary package-shaped collector test with a root path containing spaces and Chinese text; packaged `Content/DTMAPIInstaller/tools/collect-logs.ps1` parsed under Windows PowerShell 5.1, root `4_collect_dtmapi_logs.bat` exited `0`, desktop-denied output fell back to the fake `%TEMP%\DTMAPI-logs`, and `Unity-Crashes/summary.txt` included fake `error.log` and `crash.dmp`.

No game smoke was run by design. No local game runtime, official local upload package, or Workshop upload folder was modified.

## Evidence Links

- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Prior audit: `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`
- Prior cleanup update: `docs/updates/2026/20260620-0002-lifecycle-cleanup-watchdog.md`
- Smoke matrix row: `RUNTIME-LIFECYCLE-CLEANUP-WATCHDOG-20260620`
- Hook map entry: `Runtime.LifecycleRetentionCounters`

## Rollback Notes

If manual/game validation shows SaveSlots or EquipmentSlots fail to redraw after save/title transitions, revert the relevant lifecycle boundary changes first. If AutoFishing stops retrying after native vetoes, inspect the `Fishing.Automation.AutoCastWatchdog` `needs-review` status and backoff summary before reverting the whole watchdog.

## Follow-Up

Run a long-play soak or collect a fresh player crash package after this source change. ISSUE-010 stays `open/evidence-improved` until counters remain bounded or the next Unity crash package proves a different native owner.
