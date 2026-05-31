# 20260531-0029 Startup Monitor Long Run 20 Samples

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Run another longer real-threshold startup monitor to increase the chance of capturing the intermittent abnormal launch/runtime sample.
- Do not rework already verified official-local packaging, Chinese localization, fish roe, animal bell, UI lifecycle, F6 input, ActionSpeed, instant save, or OneAction fixes.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0029-startup-monitor-long-run-20-samples.md`

## Known Facts And Rejected Hypotheses

- Known: previous real-threshold monitor evidence did not reproduce a 30-second DTMAPI runtime startup sample.
- Known: a valid abnormal comparison requires a fresh DTMAPI runtime startup log with startup segment timings near 30000ms, or a clearly classified pre-runtime/blocked launch sample.
- Rejected: treating twenty more normal samples as proof that the intermittent issue is impossible.
- Rejected: optimizing DTMAPI startup while captured `Bootstrap.Awake` timings are still sub-second.

## Validation

- Pre-run checks:
  - `run-startup-monitor.ps1` parser check passed.
  - No `DolocTown.exe` process was running.
  - No previous startup monitor/sample/smoke PowerShell process was still running.
- Longer real-threshold monitor run:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 5 -BatchSize 4 -TimeoutSeconds 70 -DelaySeconds 2 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 8 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-MONITOR/20260531-185341`.
- Post-run process check:
  - No leftover `DolocTown.exe`.
- Final validation after docs update:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - Final process check found no `DolocTown.exe`.

## Evidence

- `STARTUP-MONITOR/20260531-185341/startup-monitor.md` reports:
  - `Triggered=False`
  - `StopReason=max-batches-complete`
  - `Batches=5`
  - `BatchSize=4`
  - all batches had `Normal=4`, `SlowRuntime=0`, `LaunchDelay=0`, `Blocked=0`, `NeedsReview=0`, `SlowLaunch=0`, and `SlowThresholdRuntime=0`
- Across all twenty primary samples:
  - `NormalDtmapiStartup=20`
  - `LaunchToProcessMs=4114-4153`
  - `LaunchToStartupPatternMs=6133-7181`
  - `Bootstrap.Awake totalMs=586-617`
  - `DiscoverMods totalMs=53-64`
  - `ModLoad elapsedMs=388-408`
  - `Bootstrap.HarmonyInitialize elapsedMs=530-558`
- Linked batches:
  - `STARTUP-SAMPLES/20260531-185341`
  - `STARTUP-SAMPLES/20260531-185551`
  - `STARTUP-SAMPLES/20260531-185759`
  - `STARTUP-SAMPLES/20260531-190010`
  - `STARTUP-SAMPLES/20260531-190217`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0028-startup-monitor-long-run.md`

## Rollback Notes

- Runtime behavior is unchanged.
- This update only records startup monitor evidence and does not affect gameplay, hooks, packaging, or startup code.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- If future user-visible 30-second startup recurs, capture it with `run-startup-monitor.ps1` or preserve the corresponding `GAME-SMOKE` folder so the normal-vs-abnormal comparison can be completed from segment logs.
