# 20260531-0028 Startup Monitor Long Run

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Run a longer real-threshold startup monitor after hardening failure capture, without reworking already verified 0.1.13 gameplay fixes.
- Do not optimize DTMAPI startup based on normal samples; keep abnormal 30-second runtime comparison pending until real evidence exists.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0028-startup-monitor-long-run.md`

## Known Facts And Rejected Hypotheses

- Known: `run-startup-monitor.ps1` now preserves failed child sample evidence and valid primary `GAME-SMOKE` links.
- Known: previous real-threshold monitor runs had not reproduced a 30-second launch/runtime sample.
- Rejected: treating another clean monitor run as proof that the intermittent issue cannot recur.
- Rejected: optimizing DTMAPI runtime startup while all captured runtime segment timings are sub-second.

## Validation

- Pre-run checks:
  - `run-startup-monitor.ps1` parser check passed.
  - No `DolocTown.exe` process was running.
- Longer real-threshold monitor run:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 4 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-MONITOR/20260531-183958`.
- Post-run process check:
  - No leftover `DolocTown.exe`.
- Final validation after docs update:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
  - Final process check found no `DolocTown.exe`.

## Evidence

- `STARTUP-MONITOR/20260531-183958/startup-monitor.md` reports:
  - `Triggered=False`
  - `StopReason=max-batches-complete`
  - `Batches=4`
  - `BatchSize=3`
  - all batches had `Normal=3`, `SlowRuntime=0`, `LaunchDelay=0`, `Blocked=0`, `NeedsReview=0`, `SlowLaunch=0`, and `SlowThresholdRuntime=0`
- Across all twelve primary samples:
  - `NormalDtmapiStartup=12`
  - `LaunchToProcessMs=4121-4142`
  - `LaunchToStartupPatternMs=6132-6172`
  - `Bootstrap.Awake totalMs=584-638`
  - `DiscoverMods totalMs=53-64`
  - `ModLoad elapsedMs=387-431`
  - `Bootstrap.HarmonyInitialize elapsedMs=528-578`
- Linked batches:
  - `STARTUP-SAMPLES/20260531-183958`
  - `STARTUP-SAMPLES/20260531-184141`
  - `STARTUP-SAMPLES/20260531-184325`
  - `STARTUP-SAMPLES/20260531-184508`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0027-startup-monitor-failure-capture.md`

## Rollback Notes

- Runtime behavior is unchanged.
- This update only records startup monitor evidence and does not affect gameplay, hooks, packaging, or startup code.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- If future user-visible 30-second startup recurs, capture it with `run-startup-monitor.ps1` or preserve the corresponding `GAME-SMOKE` folder so the normal-vs-abnormal comparison can be completed from segment logs.
