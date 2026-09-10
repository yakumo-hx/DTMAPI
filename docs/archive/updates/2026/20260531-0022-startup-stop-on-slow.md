# 20260531-0022 Startup Stop On Slow Sampling

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing already verified gameplay/UI items.
- Improve the remaining startup 30-second investigation by letting repeated startup sampling stop as soon as a configured launch/runtime slow threshold is reached.
- Keep runtime startup behavior unchanged until a true abnormal DTMAPI runtime segment sample exists.

## Changed Files

- `tools/scripts/run-startup-samples.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0022-startup-stop-on-slow.md`

## Known Facts And Rejected Hypotheses

- Known: current normal startup samples show sub-second DTMAPI runtime startup segments, while launch wall-clock time varies around the Steam/game startup path.
- Known: reproduced blocked runs have no `DolocTown.exe` and no fresh DTMAPI startup log, so they are not evidence of DTMAPI runtime slowness.
- Rejected: optimizing startup code from a blocked Steam launch or from an artificial threshold validation.

## Implementation

- Added `-StopOnSlowSample` to `run-startup-samples.ps1`.
- Each sample now records per-sample classification, `LaunchToStartupPatternMs`, `BootstrapAwakeTotalMs`, `SlowLaunch`, and `SlowRuntime` in `startup-samples.md/json`.
- When `-StopOnSlowSample` is set and either threshold is reached, the sampler writes `StoppedOnSlowSample`, `StoppedOnSlowLaunch`, `StoppedOnSlowRuntime`, and `StoppedOnEvidence` to aggregate `summary.txt`, then stops immediately.
- Documented the capture-and-stop workflow in debug/tooling records while explicitly noting that the validation used an artificial threshold.

## Validation

- Parser check:
  - `run-startup-samples.ps1`: passed.
- Stop-on-slow validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 3 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild -StopOnSlowSample -SlowLaunchThresholdMs 1`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-173318`.
  - The `1ms` threshold was artificial, used only to prove the stop path.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-173318/summary.txt` reports:
  - `StopOnSlowSample=True`
  - `StoppedOnSlowSample=1`
  - `StoppedOnSlowLaunch=True`
  - `StoppedOnSlowRuntime=False`
  - `StoppedOnEvidence=E:\Python_project\DTMAPI\docs\debug\evidence\GAME-SMOKE\20260531-173318`
- `STARTUP-SAMPLES/20260531-173318/startup-samples.md` reports sample 1 as `NormalDtmapiStartup` with:
  - `LaunchToStartupMs=10243`
  - `BootstrapAwakeMs=591`
  - `SlowLaunch=True` only because the test threshold was `1ms`
  - `SlowRuntime=False`
- Primary evidence:
  - `GAME-SMOKE/20260531-173318`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0021-startup-sample-threshold-summary.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If `-StopOnSlowSample` proves noisy in future sampling, remove only the early-stop branch and keep the per-sample threshold fields and raw evidence folders.

## Follow-Up

- A true DTMAPI 30-second runtime startup sample is still pending.
- Future capture runs can use the default 30000ms thresholds with `-StopOnSlowSample` to stop immediately when the first real abnormal sample appears.
