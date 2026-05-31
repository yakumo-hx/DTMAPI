# 20260531-0026 Startup Monitor Follow-Up Run

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Run another real-threshold monitor sample set to increase the chance of capturing the intermittent slow or blocked launch.
- Do not optimize runtime startup code without abnormal DTMAPI runtime timing evidence.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0026-startup-monitor-followup-run.md`

## Known Facts And Rejected Hypotheses

- Known: previous real-threshold monitor evidence `STARTUP-MONITOR/20260531-180222` captured six normal samples and no trigger.
- Known: a true DTMAPI runtime slow sample still requires startup segment evidence near 30000ms.
- Rejected: treating repeated normal monitor samples as proof that the intermittent issue cannot happen.

## Validation

- Pre-run checks:
  - `DolocTown.exe`: not running.
  - `run-startup-monitor.ps1`: parser check passed.
- Real-threshold monitor run:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 3 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-MONITOR/20260531-181048`.
- Post-run process check:
  - No leftover `DolocTown.exe`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-MONITOR/20260531-181048/startup-monitor.md` reports:
  - `Triggered=False`
  - `StopReason=max-batches-complete`
  - `Batches=3`
  - `BatchSize=3`
  - all batches had `Normal=3`, `SlowRuntime=0`, `LaunchDelay=0`, `Blocked=0`, `NeedsReview=0`, `SlowLaunch=0`, and `SlowThresholdRuntime=0`
- `STARTUP-SAMPLES/20260531-181048/startup-sample-summary.md` reports:
  - `Samples=3`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6153-7170`
  - `Bootstrap.Awake totalMs=586-612`
- `STARTUP-SAMPLES/20260531-181230/startup-sample-summary.md` reports:
  - `Samples=3`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6143-6159`
  - `Bootstrap.Awake totalMs=590-591`
- `STARTUP-SAMPLES/20260531-181413/startup-sample-summary.md` reports:
  - `Samples=3`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6139-6151`
  - `Bootstrap.Awake totalMs=599-601`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0025-startup-monitor-real-run.md`

## Rollback Notes

- Runtime behavior is unchanged.
- This update only records monitor evidence and does not affect gameplay, hooks, packaging, or startup code.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- If repeated real-threshold monitor runs continue to stay normal, keep the issue open but avoid runtime optimization until user-observed 30-second evidence is captured by `STARTUP-MONITOR` or equivalent logs.
