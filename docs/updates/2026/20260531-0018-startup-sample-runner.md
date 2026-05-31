# 20260531-0018 Startup Sample Runner

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing already verified official-local packaging, base localization, fish roe display, animal bell display, title-button lifecycle, F6 input, ActionSpeed, instant-save, or OneAction work.
- Move the remaining startup 30-second investigation from one-off title smokes to repeatable normal/slow/blocked sampling.
- Keep the true 30-second DTMAPI runtime comparison pending until a real abnormal runtime segment sample exists.

## Changed Files

- `tools/scripts/run-startup-samples.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0018-startup-sample-runner.md`

## Known Facts And Rejected Hypotheses

- Known: current normal DTMAPI runtime segment samples are under one second.
- Known: earlier reproduced blocked Steam samples did not create `DolocTown.exe` or fresh DTMAPI logs.
- Rejected: treating a missing DTMAPI log as a slow DTMAPI startup.
- Rejected: optimizing startup code before a real slow segment sample identifies the slow phase.

## Implementation

- Added `run-startup-samples.ps1`.
  - Runs repeated `run-game-smoke.ps1` title-startup samples through Steam.
  - Keeps each primary `GAME-SMOKE` evidence folder.
  - Captures each sample's raw script output.
  - Writes `startup-samples.json` and `startup-samples.md` under a `STARTUP-SAMPLES` aggregate folder.
  - Runs `analyze-startup-evidence.ps1` over the captured evidence set.
  - Continues collecting evidence when a sample exits nonzero, because a blocked Steam launch can still be useful evidence.
- Updated debug/script docs so future sessions use the sampler instead of rebuilding the startup investigation from chat memory.

## Validation

- Parser check:
  - `run-startup-samples.ps1`: passed.
- Repeated startup sampling:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 2 -TimeoutSeconds 120 -DelaySeconds 5 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-165252`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-165252/startup-samples.md` records two captured samples:
  - `GAME-SMOKE/20260531-165253`
  - `GAME-SMOKE/20260531-165447`
- `STARTUP-SAMPLES/20260531-165252/startup-analysis.md` reports:
  - Normal DTMAPI startup: 2
  - DTMAPI startup slow: 0
  - Launch delay before DTMAPI runtime: 0
  - Steam/pre-process blocked or no fresh runtime log: 0
  - Needs review: 0
- The two samples record:
  - `LaunchToProcessMs=4128/4134`
  - `LaunchToStartupPatternMs=6155/6163`
  - `Bootstrap.Awake totalMs=644/632`
  - `DiscoverModsMs=57/56`
  - `ModLoadMs=436/428`
  - `HarmonyMs=581/572`
  - `IconMs=10/9`
- Both primary evidence folders say `No DolocTown.exe process found.` and `No fatal instance popup found.`

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0017-startup-launch-timeline.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If the sampler proves noisy, keep the underlying `run-game-smoke.ps1` timeline and analyzer changes, then remove only `run-startup-samples.ps1` and its README/debug references.

## Follow-Up

- Run the sampler with a larger `-Count` when trying to reproduce the reported occasional 30-second startup.
- A true DTMAPI 30-second startup sample is still pending. If the sampler captures one, compare `LaunchToStartupPatternMs`, `Bootstrap.Awake totalMs`, and individual segment timings before changing runtime startup code.
