# 20260531-0023 Startup Real Threshold Baseline

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Use the completed startup sampler with real default thresholds, not the artificial `1ms` stop-path validation.
- Do not optimize startup without an abnormal DTMAPI runtime timing sample.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0023-startup-real-threshold-baseline.md`

## Known Facts And Rejected Hypotheses

- Known: `STARTUP-SAMPLES/20260531-173318` proved `-StopOnSlowSample` only with an artificial threshold.
- Known: a real threshold capture needs default `30000ms` launch/runtime thresholds to be meaningful for the reported occasional 30-second startup.
- Rejected: treating a non-reproducing baseline as proof that the occasional startup issue is solved.

## Validation

- Pre-run checks:
  - `DolocTown.exe`: not running.
  - `run-startup-samples.ps1`: parser check passed.
- Real-threshold stop-on-slow sampling:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 6 -TimeoutSeconds 70 -DelaySeconds 5 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild -StopOnSlowSample`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-174136`.
- Post-run process check:
  - No leftover `DolocTown.exe`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-174136/startup-sample-summary.md` reports:
  - `Samples=6`
  - `SlowLaunchThresholdMs=30000`
  - `SlowRuntimeThresholdMs=30000`
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `DurationSeconds=28-29`
  - `LaunchToProcessMs=4113-4131`
  - `LaunchToStartupPatternMs=6136-6165`
  - `Bootstrap.Awake totalMs=587-615`
  - `DiscoverModsMs=53-65`
  - `ModLoadMs=388-412`
  - `BootstrapHarmonyInitializeMs=530-556`
- `STARTUP-SAMPLES/20260531-174136/startup-analysis.md` classifies all six samples as `NormalDtmapiStartup`.
- Primary samples:
  - `GAME-SMOKE/20260531-174136`
  - `GAME-SMOKE/20260531-174209`
  - `GAME-SMOKE/20260531-174243`
  - `GAME-SMOKE/20260531-174317`
  - `GAME-SMOKE/20260531-174351`
  - `GAME-SMOKE/20260531-174425`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0022-startup-stop-on-slow.md`

## Rollback Notes

- Runtime behavior is unchanged.
- This update only records evidence and should not affect packaging or gameplay behavior.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- Keep future capture runs on default thresholds unless intentionally testing sampler behavior.
