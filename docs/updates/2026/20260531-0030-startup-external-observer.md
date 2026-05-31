# 20260531-0030 Startup External Observer

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Add an evidence path for launches that happen outside the smoke harness, such as manual Steam UI launches.
- Do not optimize startup without a real abnormal DTMAPI runtime sample.

## Changed Files

- `tools/scripts/run-startup-observer.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0030-startup-external-observer.md`

## Known Facts And Rejected Hypotheses

- Known: repeated smoke-launched real-threshold monitor runs have not reproduced a 30-second DTMAPI runtime startup sample.
- Known: a user-visible launch may differ from scripted smoke launch timing, so an observer that does not launch the game widens the capture surface.
- Rejected: using stale `DTMAPI/logs/latest.log` as proof of a fresh external launch.
- Rejected: treating no-launch observer timeout evidence as DTMAPI runtime slowness.

## Implementation

- Added `run-startup-observer.ps1`.
- The observer:
  - creates `STARTUP-OBSERVE` evidence;
  - clears stale DTMAPI/BepInEx startup logs by default when the game is not already running;
  - waits for an external `DolocTown.exe` process;
  - waits for a fresh `DTMAPI runtime starting.` line;
  - writes `startup-timeline.json` using the same timing field names consumed by `analyze-startup-evidence.ps1`;
  - writes `observer-result.json`;
  - runs `collect-logs.ps1` into the same evidence folder.
- `-NoResetLogs` keeps existing logs for already-running/manual diagnostic cases.
- `-WaitForExitSeconds` optionally waits for the externally launched game to exit before collecting logs.

## Validation

- Parser check:
  - `run-startup-observer.ps1`: passed.
- No-launch timeout validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-observer.ps1 -TimeoutSeconds 3`
  - Wrote `docs/debug/evidence/STARTUP-OBSERVE/20260531-191241`.
  - Analyzer classified it as `SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog`, with no process, no fatal popup, and no stale DTMAPI log false positive.
- External launch validation:
  - Started `run-startup-observer.ps1 -TimeoutSeconds 90 -WaitForExitSeconds 45`.
  - Triggered `steam://rungameid/2285550` from a separate process.
  - Wrote `docs/debug/evidence/STARTUP-OBSERVE/20260531-191352`.
  - Analyzer classified it as `NormalDtmapiStartup`.
  - The observer-only launch did not auto-exit, so Codex closed the launched game window afterward and recorded `process-check-after-cleanup.txt`.
- Final validation after docs update:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-OBSERVE/20260531-191241/startup-timeline.json` reports:
  - `StartupLogFound=false`
  - `TimedOut=true`
  - no observed `DolocTown.exe`
  - no observed fresh DTMAPI log
- `STARTUP-OBSERVE/20260531-191352/startup-timeline.json` reports:
  - `LaunchMode=ExternalObserved`
  - `StartupLogFound=true`
  - `LaunchToProcessMs=6208`
  - `LaunchToStartupPatternMs=7236`
- `STARTUP-OBSERVE/20260531-191352/startup-analysis.md` reports:
  - `NormalDtmapiStartup=1`
  - `DtmapiStartupSlow=0`
  - `LaunchDelayBeforeDtmapiRuntime=0`
  - `Bootstrap.Awake totalMs=230`
  - `DiscoverMods totalMs=53`
  - `ModLoad elapsedMs=37`
  - `Bootstrap.HarmonyInitialize elapsedMs=173`
- `STARTUP-OBSERVE/20260531-191352/process-check-after-cleanup.txt` reports no leftover `DolocTown.exe`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0029-startup-monitor-long-run-20-samples.md`

## Rollback Notes

- Runtime behavior is unchanged.
- Roll back by removing `run-startup-observer.ps1` and its README/debug references; smoke-launched startup sampling remains available through `run-startup-samples.ps1` and `run-startup-monitor.ps1`.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- If the user sees a 30-second startup during manual launch, start `run-startup-observer.ps1` before reproducing it so the external path can be compared against the normal smoke-launched evidence.
