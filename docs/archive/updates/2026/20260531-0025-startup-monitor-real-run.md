# 20260531-0025 Startup Monitor Real Run

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Use the startup monitor with real default `30000ms` thresholds to try to capture a true slow or blocked launch.
- Do not change or optimize runtime startup code without abnormal DTMAPI runtime timing evidence.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0025-startup-monitor-real-run.md`

## Known Facts And Rejected Hypotheses

- Known: previous monitor validation proved both normal completion and artificial trigger-stop behavior.
- Known: true DTMAPI runtime startup slowness requires fresh runtime segment logs near 30000ms, especially `Bootstrap.Awake totalMs` or equivalent DTMAPI startup segment evidence.
- Rejected: treating normal monitor runs as a fix or treating a small Steam launch wall-clock variance as DTMAPI runtime slowness.

## Validation

- Pre-run checks:
  - `DolocTown.exe`: not running.
  - `run-startup-monitor.ps1`: parser check passed.
- Real-threshold monitor run:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 2 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-MONITOR/20260531-180222`.
- Post-run process check:
  - No leftover `DolocTown.exe`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-MONITOR/20260531-180222/startup-monitor.md` reports:
  - `Triggered=False`
  - `StopReason=max-batches-complete`
  - `Batches=2`
  - `BatchSize=3`
  - both batches had `Normal=3`, `SlowRuntime=0`, `LaunchDelay=0`, `Blocked=0`, `NeedsReview=0`, `SlowLaunch=0`, and `SlowThresholdRuntime=0`
- `STARTUP-SAMPLES/20260531-180222/startup-sample-summary.md` reports:
  - `Samples=3`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6145-7172`
  - `Bootstrap.Awake totalMs=595-604`
  - `ModLoadMs=393-399`
  - `BootstrapHarmonyInitializeMs=536-543`
- `STARTUP-SAMPLES/20260531-180406/startup-sample-summary.md` reports:
  - `Samples=3`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6142-6157`
  - `Bootstrap.Awake totalMs=589-608`
  - `ModLoadMs=392-405`
  - `BootstrapHarmonyInitializeMs=531-548`
- Primary samples:
  - `GAME-SMOKE/20260531-180223`
  - `GAME-SMOKE/20260531-180255`
  - `GAME-SMOKE/20260531-180327`
  - `GAME-SMOKE/20260531-180407`
  - `GAME-SMOKE/20260531-180438`
  - `GAME-SMOKE/20260531-180510`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0024-startup-monitor-capture-loop.md`

## Rollback Notes

- Runtime behavior is unchanged.
- This update only records monitor evidence and does not affect gameplay, hooks, packaging, or startup code.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- Future long unattended captures can increase `MaxBatches` or set `MaxMinutes`; any triggered monitor batch should be inspected through its linked `STARTUP-SAMPLES` folder before code changes.
