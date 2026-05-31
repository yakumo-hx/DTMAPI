# 20260531-0020 Fast Startup Sampling

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused only on the remaining startup 30-second investigation.
- Improve repeated startup sampling efficiency so more attempts can be captured without redoing UI/gameplay evidence.
- Do not optimize DTMAPI runtime startup without a real slow runtime segment sample.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/run-startup-samples.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0020-fast-startup-sampling.md`

## Known Facts And Rejected Hypotheses

- Known: prior title-menu startup samples were useful but spent time collecting UI screenshot evidence that is not needed for startup timing.
- Known: current normal samples keep DTMAPI `Bootstrap.Awake totalMs` under one second even when launch-to-startup wall-clock varies.
- Rejected: treating 9-14s launch-to-startup wall-clock samples as DTMAPI runtime slowness when `Bootstrap.Awake totalMs` remains under one second.

## Implementation

- Added `-AutoExitAfterSecondsOverride` to `run-game-smoke.ps1`.
  - This lets callers set a shorter smoke auto-exit window for startup-only sampling.
- Added `-AutoExitAfterSeconds` to `run-startup-samples.ps1`.
  - This passes the override to `run-game-smoke.ps1`.
  - Use with `-NoTitleSettingsMenu` for faster pure-startup samples without title menu screenshot evidence.
- Updated debug/script documentation with the faster startup sampling command.

## Validation

- Parser checks:
  - `run-game-smoke.ps1`: passed.
  - `run-startup-samples.ps1`: passed.
- Fast pure-startup sampling:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 5 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-171630`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-171630/startup-samples.md` records five samples with duration `31-37s`.
- `STARTUP-SAMPLES/20260531-171630/startup-analysis.md` reports:
  - Normal DTMAPI startup: 5
  - DTMAPI startup slow: 0
  - Launch delay before DTMAPI runtime: 0
  - Steam/pre-process blocked or no fresh runtime log: 0
  - Needs review: 0
- Primary samples:
  - `GAME-SMOKE/20260531-171631`
  - `GAME-SMOKE/20260531-171705`
  - `GAME-SMOKE/20260531-171745`
  - `GAME-SMOKE/20260531-171821`
  - `GAME-SMOKE/20260531-171859`
- Observed ranges:
  - `LaunchToProcessMs=7166-12244`
  - `LaunchToStartupPatternMs=9205-14277`
  - `Bootstrap.Awake totalMs=596-612`
  - `DiscoverModsMs=55-56`
  - `ModLoadMs=390-407`
  - `HarmonyMs=538-552`
  - `IconMs=9-10`
- All five primary evidence folders report no leftover `DolocTown.exe` and no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0019-startup-extended-baseline.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If the override causes a future smoke to exit too early for non-startup checks, do not use it there; keep the default `run-game-smoke.ps1` behavior for UI, save, and gameplay evidence.

## Follow-Up

- Use `run-startup-samples.ps1 -NoTitleSettingsMenu -AutoExitAfterSeconds 20` for higher-count startup-only reproduction attempts.
- A true DTMAPI 30-second runtime startup sample is still pending. If captured, compare `LaunchToStartupPatternMs`, `Bootstrap.Awake totalMs`, and the individual segment timings before changing startup code.
