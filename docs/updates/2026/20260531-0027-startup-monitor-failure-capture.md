# 20260531-0027 Startup Monitor Failure Capture

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Preserve evidence when a child smoke or sample fails, instead of losing the aggregate monitor output.
- Do not treat title-settings smoke assertion failures as DTMAPI runtime startup slowness without segment timing evidence.

## Changed Files

- `tools/scripts/run-startup-samples.ps1`
- `tools/scripts/run-startup-monitor.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0027-startup-monitor-failure-capture.md`

## Known Facts And Rejected Hypotheses

- Known: title-settings monitor attempts can fail the child smoke even when DTMAPI startup timing is normal.
- Known: PowerShell native-command error wrapping can write misleading `Evidence:` text to captured output when `$ErrorActionPreference` is `Stop`.
- Rejected: classifying a `sample-failure-count-*` trigger as a real 30-second DTMAPI runtime slow sample without `Bootstrap.Awake` or startup segment timings near 30000ms.

## Implementation

- `run-startup-samples.ps1` now temporarily switches `$ErrorActionPreference` to `Continue` while invoking `run-game-smoke.ps1`, captures output and exit code, then restores the prior preference.
- `run-startup-monitor.ps1` does the same while invoking `run-startup-samples.ps1`, so monitor aggregates are still written when a child sample batch fails.
- `run-startup-samples.ps1` now parses only existing `GAME-SMOKE` evidence directories from child output, preventing wrapped error text such as `Evidence: :String) [], RemoteException` from becoming a bogus evidence path.

## Validation

- Parser checks:
  - `run-startup-samples.ps1`: passed.
  - `run-startup-monitor.ps1`: passed.
- Title-settings failure-capture validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-monitor.ps1 -MaxBatches 1 -BatchSize 1 -TimeoutSeconds 90 -DelaySeconds 5 -AutoExitAfterSeconds 25 -DelaySecondsBetweenBatches 10 -IncludeTitleSettingsMenu -SkipBuild`
  - Wrote `docs/debug/evidence/STARTUP-MONITOR/20260531-182957`.
- Post-fix no-title startup sample:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 1 -TimeoutSeconds 70 -DelaySeconds 5 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`
  - Wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-183210`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
- Exit check:
  - No leftover `DolocTown.exe`.

## Evidence

- Pre-fix title-settings monitor failure:
  - `STARTUP-MONITOR/20260531-182126` and `STARTUP-SAMPLES/20260531-182126` were partial because the monitor aborted before writing aggregate output.
  - `STARTUP-MONITOR/20260531-182655` wrote an aggregate, but `STARTUP-SAMPLES/20260531-182655` recorded a bogus evidence path from wrapped PowerShell error text.
- Post-fix title-settings monitor validation:
  - `STARTUP-MONITOR/20260531-182957/startup-monitor.md` reports `Triggered=True`, `Reasons=sample-failure-count-1`, and links `STARTUP-SAMPLES/20260531-182958`.
  - `STARTUP-SAMPLES/20260531-182958/startup-analysis.md` classifies primary smoke `GAME-SMOKE/20260531-182958` as `NormalDtmapiStartup`.
  - Primary smoke timing: `LaunchToStartupPatternMs=6153`, `Bootstrap.Awake totalMs=657`, `DiscoverMods totalMs=67`, `ModLoad elapsedMs=437`, `Bootstrap.HarmonyInitialize elapsedMs=592`.
  - Primary smoke exit checks: no fatal popup and no leftover `DolocTown.exe`.
- Post-fix no-title recheck:
  - `STARTUP-SAMPLES/20260531-183210/startup-sample-summary.md` reports `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=6159`, and `Bootstrap.Awake totalMs=610`.
  - Primary smoke `GAME-SMOKE/20260531-183211` is classified as `NormalDtmapiStartup`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0026-startup-monitor-followup-run.md`

## Rollback Notes

- Runtime behavior is unchanged.
- Roll back by restoring the child-process invocation and evidence parsing logic in `run-startup-samples.ps1` and `run-startup-monitor.ps1`; doing so would make monitor evidence less durable when child scripts fail.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- The title-settings smoke failure is retained as evidence capture validation, not as proof of runtime startup slowness.
