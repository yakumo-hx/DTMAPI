# 20260531-0021 Startup Sample Threshold Summary

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining startup 30-second investigation.
- Make repeated startup samples easier to audit by adding threshold counts and min/max/average ranges.
- Do not change runtime startup behavior without a true abnormal DTMAPI runtime segment sample.

## Changed Files

- `tools/scripts/run-startup-samples.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0021-startup-sample-threshold-summary.md`

## Known Facts And Rejected Hypotheses

- Known: `STARTUP-SAMPLES` already preserves primary `GAME-SMOKE` folders and startup analyzer output.
- Known: future slow samples need quick classification between DTMAPI runtime slowness and Steam/game pre-runtime delay.
- Rejected: relying on manual table reading when the evidence folder can write explicit threshold counts.

## Implementation

- Added `-SlowLaunchThresholdMs` and `-SlowRuntimeThresholdMs` to `run-startup-samples.ps1`; both default to `30000`.
- Added `startup-sample-summary.json` and `startup-sample-summary.md`.
  - Records sample count, threshold values, min/max/average for sample duration, launch-to-process, launch-to-startup, Bootstrap.Awake, DiscoverMods, ModLoad, and Harmony initialization.
  - Records `SlowLaunchCount`, `SlowRuntimeCount`, and the matching evidence paths.
- Appended a compact summary section to `startup-samples.md`.

## Validation

- Parser check:
  - `run-startup-samples.ps1`: passed.
- Threshold-summary startup sampling:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 2 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-172605`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-172605/startup-sample-summary.md` reports:
  - `SlowLaunchCount=0`
  - `SlowRuntimeCount=0`
  - `LaunchToProcessMs=7155-7163`
  - `LaunchToStartupPatternMs=9176-9192`
  - `Bootstrap.Awake totalMs=586-601`
  - `DiscoverModsMs=54-56`
  - `ModLoadMs=390-397`
  - `BootstrapHarmonyInitializeMs=529-543`
- `STARTUP-SAMPLES/20260531-172605/startup-analysis.md` classifies both primary samples as `NormalDtmapiStartup`.
- Primary samples:
  - `GAME-SMOKE/20260531-172606`
  - `GAME-SMOKE/20260531-172640`
- Both primary evidence folders report no leftover `DolocTown.exe` and no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0020-fast-startup-sampling.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If the threshold summary misleads a future run, keep the raw `startup-analysis.*` files as authoritative and adjust only the sampler summary logic.

## Follow-Up

- A true DTMAPI 30-second runtime startup sample is still pending.
- If future sampling reports `SlowRuntimeCount > 0`, inspect the matching primary `GAME-SMOKE` evidence before changing startup code.
- If future sampling reports `SlowLaunchCount > 0` with `SlowRuntimeCount = 0`, treat it as Steam/game pre-runtime delay unless other evidence contradicts that classification.
