# 20260531-0024 Startup Monitor Capture Loop

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Move from manual repeated sampling to an unattended capture loop that preserves the first slow, blocked, needs-review, failed, or leftover-process batch.
- Do not change runtime startup behavior without a true abnormal DTMAPI runtime segment sample.

## Changed Files

- `tools/scripts/run-startup-monitor.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0024-startup-monitor-capture-loop.md`

## Known Facts And Rejected Hypotheses

- Known: the existing startup sampler can classify normal, true runtime slow, pre-runtime delay, blocked, and needs-review evidence.
- Known: short batches have repeatedly shown normal DTMAPI runtime startup, but the reported occasional 30-second case has not been captured as a true DTMAPI runtime sample.
- Rejected: optimizing DTMAPI startup based on artificial thresholds or Steam pre-process blocked evidence.

## Implementation

- Added `run-startup-monitor.ps1`.
- The monitor runs one or more `run-startup-samples.ps1` batches with real configurable thresholds.
- The monitor stops when a batch reports:
  - slow launch threshold
  - slow runtime threshold
  - DTMAPI runtime slow classification
  - pre-runtime launch-delay classification
  - Steam launch blocked classification
  - needs-review classification
  - sample failure or missing evidence
  - leftover `DolocTown.exe`
- The monitor writes `STARTUP-MONITOR/<stamp>/startup-monitor.json`, `startup-monitor.md`, `summary.txt`, and per-batch script output. Each monitor row links to the underlying `STARTUP-SAMPLES` evidence folder.

## Validation

- Parser checks:
  - `run-startup-monitor.ps1`: passed.
  - `run-startup-samples.ps1`: passed.
- First validation attempt:
  - `run-startup-monitor.ps1 -MaxBatches 1 -BatchSize 2 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 3 -SkipBuild`
  - Created `STARTUP-MONITOR/20260531-175200` and `STARTUP-SAMPLES/20260531-175201`, then failed inside the monitor on PowerShell single-object property/count handling.
  - The underlying two startup samples were normal and no `DolocTown.exe` remained.
  - Fixed by reading JSON properties through `PSObject.Properties[...]` and forcing trigger reasons to an array.
- Normal monitor validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 1 -BatchSize 2 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 3 -SkipBuild`
  - Passed and wrote `STARTUP-MONITOR/20260531-175403`.
- Artificial trigger validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 3 -BatchSize 2 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 3 -SlowLaunchThresholdMs 1 -SkipBuild`
  - Passed and wrote `STARTUP-MONITOR/20260531-175538`.
  - The `1ms` threshold was artificial, used only to prove the monitor stop path.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- Normal monitor validation `STARTUP-MONITOR/20260531-175403/startup-monitor.md` reports:
  - `Triggered=False`
  - `StopReason=max-batches-complete`
  - `Normal=2`
  - `SlowRuntime=0`
  - `LaunchDelay=0`
  - `Blocked=0`
  - `NeedsReview=0`
  - `SlowLaunch=0`
  - linked evidence `STARTUP-SAMPLES/20260531-175403`
- Linked normal sample summary `STARTUP-SAMPLES/20260531-175403/startup-sample-summary.md` reports:
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToStartupPatternMs=6140-6166`
  - `Bootstrap.Awake totalMs=606-626`
- Artificial trigger validation `STARTUP-MONITOR/20260531-175538/startup-monitor.md` reports:
  - `Triggered=True`
  - `StopReason=triggered-batch-1`
  - `Reasons=slow-launch-threshold`
  - linked evidence `STARTUP-SAMPLES/20260531-175538`
- Linked artificial-trigger sample `STARTUP-SAMPLES/20260531-175538/startup-samples.md` reports:
  - `Count requested=2`
  - `Count captured=1`
  - `SlowLaunch=True` only because the test threshold was `1ms`
  - `SlowRuntime=False`
  - `BootstrapAwakeMs=611`
- Process checks after validation found no leftover `DolocTown.exe`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0023-startup-real-threshold-baseline.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If the monitor logic proves noisy, remove or narrow `run-startup-monitor.ps1`; primary `run-startup-samples.ps1` evidence remains authoritative.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- Future unattended capture can run the monitor with default 30000ms thresholds and a higher `MaxBatches`/`MaxMinutes` value.
