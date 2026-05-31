# 20260531-0017 Startup Launch Timeline

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up and finish the startup 30-second evidence chain without redoing already verified UI, F6, ActionSpeed, instant-save, or OneAction gameplay work.
- Separate Steam/pre-runtime launch delay from DTMAPI runtime startup cost with wall-clock checkpoints.
- Keep the true 30-second DTMAPI runtime sample pending until a run has fresh startup segment logs near 30000ms.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/analyze-startup-evidence.ps1`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0017-startup-launch-timeline.md`

## Known Facts And Rejected Hypotheses

- Known: earlier blocked samples did not create `DolocTown.exe` or fresh `DTMAPI/logs/latest.log`.
- Known: normal DTMAPI segment samples remain sub-second after the 0.1.13 instrumentation.
- Rejected: treating a missing runtime log as evidence of DTMAPI startup slowness.
- Rejected: optimizing scanner, icon, config, Harmony, or mod-load paths without a true slow runtime segment sample.

## Implementation

- Added `startup-timeline.json` to `run-game-smoke.ps1`.
  - Records Steam/direct launch mode, launch command timestamps, wait timestamps, first `DolocTown.exe` timestamp, first DTMAPI log file timestamp, first startup pattern timestamp, fatal instance popup timestamp, timeout state, and elapsed milliseconds.
- Updated `analyze-startup-evidence.ps1`.
  - Reads `startup-timeline.json`.
  - Reports `LaunchToProcessMs` and `LaunchToStartupMs`.
  - Classifies a run with slow launch-to-startup but normal `Bootstrap.Awake` as `LaunchDelayBeforeDtmapiRuntime`.
- Kept `collect-logs.ps1` embedding the analyzer into the primary smoke evidence folder.
- Updated script/debug/hook documentation with the timeline evidence path and interpretation rule.

## Validation

- PowerShell parser checks:
  - `run-game-smoke.ps1`: passed.
  - `analyze-startup-evidence.ps1`: passed.
  - `collect-logs.ps1`: passed.
- Timeline title smoke:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 120 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-164214`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `GAME-SMOKE/20260531-164214/result.json` has `StartupLog=true`, `GameLaunched=true`, `TitleSettingsMenu=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`.
- `GAME-SMOKE/20260531-164214/startup-timeline.json` records:
  - `LaunchMode=Steam`
  - `LaunchToProcessMs=4141`
  - `LaunchToDtmapiLogFileMs=6165`
  - `LaunchToStartupPatternMs=6167`
  - `TimedOut=false`
- `GAME-SMOKE/20260531-164214/startup-analysis.md` classifies the run as `NormalDtmapiStartup`.
- The same analysis records `Bootstrap.Awake totalMs=649`, `DiscoverModsMs=68`, `ModLoadMs=426`, `HarmonyMs=589`, and `IconMs=10`.
- `GAME-SMOKE/20260531-164214/process-check.txt` says `No DolocTown.exe process found.`
- `GAME-SMOKE/20260531-164214/fatal-window-check.txt` says `No fatal instance popup found.`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0016-smoke-integrated-startup-analysis.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If `startup-timeline.json` causes smoke harness issues, revert only the timeline capture and keep existing startup segment logs plus `startup-analysis.*`.

## Follow-Up

- A true DTMAPI 30-second startup sample is still pending. When it appears, compare `LaunchToStartupPatternMs` against `Bootstrap.Awake totalMs` and the individual segment timings before changing runtime startup code.
