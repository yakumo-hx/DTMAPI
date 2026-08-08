# 20260704-0002 Long Title And SaveLoad Cycle Validation

## Summary

Ran the requested post-phase-8A long validation set: a two-hour title-idle-to-save gate, a short same-process save/load cycle through the seventh save, a three-hour periodic cycle attempt that waits one hour before entering/exiting a save every five minutes, and a follow-up diagnostic-pressure isolation for `Smoke.SaveLoadCycle=pending`.

The first two validations passed. The three-hour periodic run reproduced the native `Fatal error in GC / Unexpected mark stack overflow` class at the first post-idle `LoadGame` transition, before `SaveLoaded` and before any enter/exit cycle completed. The throttled rerun reproduced the same Fatal class on cycle 2 after cycle 1 completed. The SaveLoad coordinator recorded no duplicate load requests in either failure, so this evidence does not point back to the earlier duplicate-LoadGame hypothesis.

The follow-up pressure isolation published exactly 600 `Smoke.SaveLoadCycle=pending` statuses over a 20-minute title-screen window, then loaded slot 3 once. It passed with one `LoadGame`, one `SaveLoaded`, no Fatal GC popup, and no leftover `DolocTown.exe`. That makes dense smoke pending publication unlikely to be the crash trigger by itself.

## Source Request / Goal

- User request: run a two-hour main-menu idle then load, run a short seventh-save repeated enter/exit test, then run a three-hour long test where after one hour the smoke enters/exits a save every five minutes.
- Follow-up user request: check whether the earlier `Smoke.SaveLoadCycle=pending` `statusCount=706` changed across the duplicate-LoadGame fix, then run a separate roughly 20-minute pressure test that emits about 600 pending statuses before entering a save.
- No new refactor goal was opened for runtime behavior. The code change is smoke-harness-only support for the requested same-process save/load cycle test.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md`

## Behavior

- Added opt-in smoke-only save/load cycle controls:
  - `-AutoExerciseSaveLoadCycle`
  - `-SaveLoadCycleCount`
  - `-SaveLoadCycleInitialTitleIdleSeconds`
  - `-SaveLoadCycleIntervalSeconds`
  - `-SaveLoadCycleInSaveSeconds`
- Added smoke result fields:
  - `SaveLoadCycle`
  - `SaveLoadCycleSummary`
- Added opt-in smoke-only pending-pressure controls:
  - `-AutoExerciseSaveLoadCyclePendingPressure`
  - `-SaveLoadCyclePendingPressureSeconds`
  - `-SaveLoadCyclePendingPressureIntervalSeconds`
- Added smoke result fields:
  - `SaveLoadCyclePendingPressure`
  - `SaveLoadCyclePendingPressureSummary`
- The cycle path uses the existing official auto-load path instead of directly calling native `LoadGame`, so the test follows the same UI/fallback path as the established smoke harness.
- The pending-pressure path waits for a stable title screen, publishes `Smoke.SaveLoadCycle=pending` by target sequence count, then loads the selected save once through the existing official auto-load path. It is diagnostic pressure only and does not return to title or repeat cycles.
- Follow-up hardening after the first long-cycle failure throttles initial-title-idle `Smoke.SaveLoadCycle=pending` publication from every 5 seconds to every 5 minutes. Stage transitions such as requested load, observed `SaveLoaded`, requested return-to-title, completion, and failure still publish immediately.
- This does not change player runtime behavior, public APIs, content loading, CustomAnimals, AnimalVoice, AutoFishing, registry takeover, Hook targets, or JSON semantics.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restore/build emitted restricted-network `NU1900` package-vulnerability feed warnings only.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- The shared runtime lock was acquired before smoke execution and released after the long run.

### Two-Hour Title Idle Then Save

- Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 7200 -TimeoutSeconds 7500 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-010303`
- Result: passed.
- Key fields: `RunStatus=Passed`, `LongTitleIdleBeforeSave=Passed`, `SaveLoaded=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `LifecycleObservation=Passed`, `ResourceLifecycleLedger=Passed`, `TitleIdleResourceGrowth=Passed`, `ContentRegistry=Passed`, `ManifestRegistry=Passed`, `RegistryDiffs=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- SaveLoad evidence: one `LoadGame requested for slot/index 2. requestId=SL-0001`, followed by `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`; no fatal popup and no leftover `DolocTown.exe`.

### Seventh-Save Short Save/Load Cycle

- Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 3 -SaveLoadCycleInitialTitleIdleSeconds 5 -SaveLoadCycleIntervalSeconds 5 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 420 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-033112`
- Result: passed after fixing the smoke-only cycle path to reuse the existing auto-load route.
- Key fields: `RunStatus=Passed`, `SaveLoadCycle=Passed`, `SaveLoaded=Passed`, `HookProbe=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Cycle summary: `Smoke exercise SaveLoadCycle OK cycles=3, slot=7, initialIdleSeconds=5, intervalSeconds=5, inSaveSeconds=5, elapsedSeconds=74`.
- SaveLoad evidence: three closed load requests for slot/index 6; each entered native `LoadGame`, reached `SaveLoaded`, returned to title, and closed without duplicate requests.

Retained intermediate failed evidence:

- `docs/debug/evidence/GAME-SMOKE/20260704-031102`
- `docs/debug/evidence/GAME-SMOKE/20260704-032123`

Those failures were smoke-harness reliability failures from direct native `LoadGame` use after returning home. They did not show Fatal GC and are superseded by the passing `20260704-033112` run.

### Three-Hour Periodic Save/Load Cycle Attempt

- Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 24 -SaveLoadCycleInitialTitleIdleSeconds 3600 -SaveLoadCycleIntervalSeconds 300 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 11250 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-033314`
- Result: aborted on native Fatal GC at the first scheduled post-idle save load.
- Fatal evidence: `fatal-window-check.txt` captured `Fatal error in GC` and `Unexpected mark stack overflow` for `DolocTown.exe` process id `29576`.
- Key fields: `RunStatus=Aborted`, `NoFatalInstanceWindow=Failed`, `SaveLoadCycle=Failed`, `SaveLoaded=Failed`, `SaveLoadBoundary=Failed`, `ProcessExited=Passed`, `ForcedClose=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `HookProbe=Passed`, `ContentRegistry=Passed`, `RegistryDiffs=Passed`, `TitleIdleResourceGrowth=Passed`, `ResourceLifecycleLedger=Passed`, and `GameBridgeFinalHealthSnapshot=Passed`.
- Last SaveLoad boundary before the fatal popup: `LoadGame requested for slot/index 2. requestId=SL-0001` at `2026-07-04 04:33:24.601 +08:00`.
- SaveLoad summary at failure: `requests=1; active=SL-0001:slot=2; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=0; saveLoaded=0`.
- No `SaveLoaded hook dispatched` occurred after that request.
- The failure happened before the first cycle completed, so this run does not prove a multi-cycle accumulation problem. It proves the post-title-idle native `LoadGame` transition can still hit the Fatal GC class even when duplicate LoadGame is not present.

### Throttled Periodic Save/Load Cycle Rerun

- Change under test: initial title-idle `Smoke.SaveLoadCycle=pending` logs/statuses throttled to a five-minute heartbeat.
- Source validation after the throttle: `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; parser check for `tools/scripts/run-game-smoke.ps1` passed; `git diff --check` passed with line-ending warnings only.
- Short seventh-save regression passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 3 -SaveLoadCycleInitialTitleIdleSeconds 5 -SaveLoadCycleIntervalSeconds 5 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 420 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-060434`
  - Result: `RunStatus=Passed`, `SaveLoadCycle=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Pending status count in the log was 15 for the whole short run, and the three slot/index 6 load requests all closed.
- Throttled long periodic rerun:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 24 -SaveLoadCycleInitialTitleIdleSeconds 3600 -SaveLoadCycleIntervalSeconds 300 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 11250 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-060648`
  - Result: aborted on native Fatal GC during cycle 2, after cycle 1 had successfully loaded slot 3, reached `SaveLoaded`, returned to title, and completed.
  - Initial idle pending publication was reduced to five-minute heartbeats: current-run log had 12 elapsed-time pending heartbeats (`1/3600` through `3301/3600`) before the first cycle, and total `Smoke.SaveLoadCycle = pending` count was 19 by failure.
  - Cycle 1 succeeded: `LoadGame requested for slot/index 2. requestId=SL-0001` at `2026-07-04 07:06:58.207 +08:00`; `SaveLoaded hook dispatched. slot/index=2 isNewGame=False` at `07:07:00.113`; `Smoke save/load cycle completed 1/24` at `07:07:07.064`.
  - Cycle 2 failed before `SaveLoaded`: `LoadGame requested for slot/index 2. requestId=SL-0002` at `2026-07-04 07:12:10.037 +08:00`; no second `SaveLoaded hook dispatched` followed.
  - `fatal-window-check.txt` captured `Fatal error in GC` and `Unexpected mark stack overflow` for `DolocTown.exe` process id `29044`.
  - Key result fields: `RunStatus=Aborted`, `NoFatalInstanceWindow=Failed`, `SaveLoadCycle=Failed`, `SaveLoaded=Passed` for the first cycle, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, and `ForcedClose=Passed`.
  - SaveLoad summary at failure: `requests=2; active=SL-0002:slot=2; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=2; nativeReturn=1; saveLoaded=1`.

### SaveLoadCycle Pending Pressure Isolation

- Historical check:
  - Before the phase-five duplicate-LoadGame fix, long-title evidence `docs/debug/evidence/GAME-SMOKE/20260702-210050` had `Smoke.SaveLoadCycle=pending` count `0`, four `LoadGame requested for slot/index 2` lines, no `SaveLoaded`, and a Fatal window.
  - After the phase-five duplicate-LoadGame fix, long-title evidence `docs/debug/evidence/GAME-SMOKE/20260703-201302` had `Smoke.SaveLoadCycle=pending` count `0`, one `LoadGame`, one `SaveLoaded`, and passed.
  - Later phase-seven and two-hour title-idle passes `docs/debug/evidence/GAME-SMOKE/20260703-231929` and `docs/debug/evidence/GAME-SMOKE/20260704-010303` also had `Smoke.SaveLoadCycle=pending` count `0`.
  - The `statusCount=706` case first appears in the later periodic save/load cycle smoke `docs/debug/evidence/GAME-SMOKE/20260704-033314`, not in the duplicate-LoadGame fix itself.
- Calibration:
  - First short pressure self-check `docs/debug/evidence/GAME-SMOKE/20260704-074952` passed behaviorally but produced `published=14, expected=15`, showing that raw two-second Update timing can miss one sample at the edge.
  - After final-count catch-up, short pressure self-checks `docs/debug/evidence/GAME-SMOKE/20260704-075237` and `docs/debug/evidence/GAME-SMOKE/20260704-081707` both passed with `published=15, expected=15`, no Fatal, and clean process exit.
  - Intermediate 20-minute pressure check `docs/debug/evidence/GAME-SMOKE/20260704-075419` passed behaviorally but produced `published=570, expected=600`; this was harness timing drift, not a runtime failure, and led to target-sequence catch-up before the formal run.
- Formal command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCyclePendingPressure -SaveLoadCyclePendingPressureSeconds 1200 -SaveLoadCyclePendingPressureIntervalSeconds 2 -TimeoutSeconds 1600 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-081844`
- Result: passed.
- Key fields: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `SaveLoadCyclePendingPressure=Passed`, `NoFatalInstanceWindow=Passed`, `ProcessExited=Passed`, `ForcedClose=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, and `SaveLoadBoundary=Passed`.
- Pressure summary: `Smoke exercise SaveLoadCyclePendingPressure OK published=600, expected=600, slot=3, pressureSeconds=1200, intervalSeconds=2, elapsedSeconds=1204`.
- Log count: exactly 600 current-run `Smoke.SaveLoadCycle = pending` lines. The first was `Pressure pending sample 1/600` at `2026-07-04 08:19:23.450 +08:00`; the last was `Pressure pending sample 600/600` at `2026-07-04 08:39:21.400 +08:00`.
- SaveLoad evidence after pressure: one `LoadGame requested for slot/index 2. requestId=SL-0001` at `2026-07-04 08:39:24.591 +08:00`, one `SaveLoaded hook dispatched. slot/index=2 isNewGame=False` at `2026-07-04 08:39:26.437 +08:00`, and `SaveLoadRequestSummary` reported `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1; timeouts=0; fatalWindows=0`.
- `fatal-window-check.txt` reported no fatal instance popup, and `process-check.txt` reported no `DolocTown.exe`.

## Interpretation

- The two-hour single-load pass and the one-hour periodic-run failure show the issue is not deterministic by title-idle duration alone.
- The newest failure narrows the boundary to native `LoadGame` after title idle, before `SaveLoaded`, with one request and no duplicate DTMAPI/smoke fallback request.
- Current evidence does not implicate registry diffs, title-idle resource generation growth, Hook reinstall loops, or duplicate SaveLoad requests.
- The first periodic-cycle smoke produced repeated `Smoke.SaveLoadCycle=pending` hook status publication during the one-hour idle window (`statusCount=706`). After throttling, the same class of Fatal GC still reproduced with only 19 total pending statuses by failure. Dense smoke pending publication is therefore not a necessary condition for the crash, though the throttled evidence is cleaner.
- The formal 20-minute pressure isolation produced exactly 600 `Smoke.SaveLoadCycle=pending` statuses and then loaded slot 3 successfully with no Fatal GC. This further argues that the `706` pending-count symptom was smoke diagnostic pressure/noise, not the direct reason the periodic long run crashed.
- The throttled rerun moves the useful boundary: cycle 1 can now complete after the one-hour title idle, but cycle 2 can still Fatal at native `LoadGame` before `SaveLoaded`. This points more strongly at title-return-to-next-load native object graph/lifecycle state, not only first-load-after-idle state.

## Rollback Notes

- Do not use `-AutoExerciseSaveLoadCycle` unless explicitly running same-process save/load cycle validation.
- The added smoke fields are opt-in and skipped for normal smoke runs.
- If the cycle smoke path causes confusion, remove or disable only the smoke harness option; player runtime behavior and legacy smoke cases are otherwise unchanged.

## Follow-Up

- Keep ISSUE-010 open. The latest throttled reproduction rules out duplicate LoadGame for this sample and points the next isolation toward the native title-return-to-next-load boundary or native/Unity object graph present at that boundary.
- Do not spend another run proving the same Fatal GC exists; use the next long run only after a focused mitigation or cleaner telemetry change.
