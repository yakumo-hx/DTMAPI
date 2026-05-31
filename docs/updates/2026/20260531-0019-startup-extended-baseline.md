# 20260531-0019 Startup Extended Baseline

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing already verified A-E gameplay/UI work.
- Try to catch the reported occasional 30-second startup using the new sampler.
- If no abnormal sample appears, strengthen the normal baseline and keep the true DTMAPI 30-second comparison pending.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0019-startup-extended-baseline.md`

## Known Facts And Rejected Hypotheses

- Known: repeated startup sampling can now classify normal, slow runtime, pre-runtime launch delay, and Steam/pre-process blocker samples.
- Known: the earlier blocked launch samples lacked `DolocTown.exe` and lacked fresh DTMAPI startup logs.
- Rejected: optimizing DTMAPI startup code from the current samples; all current samples keep DTMAPI `Bootstrap.Awake totalMs` well under one second.

## Implementation

- No runtime or script code changed in this record.
- Ran a five-sample startup baseline through the sampler.
- Updated startup/debug/hook records with the new evidence range.

## Validation

- Extended startup sampling:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-startup-samples.ps1 -Count 5 -TimeoutSeconds 60 -DelaySeconds 5 -SkipBuild`
  - Passed and wrote `docs/debug/evidence/STARTUP-SAMPLES/20260531-170717`.
- Build:
  - Not rerun for this documentation/evidence-only record. The last code-bearing record `20260531-0018` was followed by a passing `build.ps1` with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-SAMPLES/20260531-170717/startup-analysis.md` reports:
  - Normal DTMAPI startup: 5
  - DTMAPI startup slow: 0
  - Launch delay before DTMAPI runtime: 0
  - Steam/pre-process blocked or no fresh runtime log: 0
  - Needs review: 0
- Primary samples:
  - `GAME-SMOKE/20260531-170718`
  - `GAME-SMOKE/20260531-170815`
  - `GAME-SMOKE/20260531-170912`
  - `GAME-SMOKE/20260531-171007`
  - `GAME-SMOKE/20260531-171105`
- Observed ranges:
  - `LaunchToProcessMs=6147-10232`
  - `LaunchToStartupPatternMs=9173-12251`
  - `Bootstrap.Awake totalMs=624-648`
  - `DiscoverModsMs=56-61`
  - `ModLoadMs=424-446`
  - `HarmonyMs=566-590`
  - `IconMs=8-9`
- All five primary evidence folders report no leftover `DolocTown.exe` and no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0018-startup-sample-runner.md`

## Rollback Notes

- Documentation-only update. If a future sample captures a real 30-second DTMAPI runtime segment, keep this baseline as a normal comparison point rather than removing it.

## Follow-Up

- A true DTMAPI 30-second startup sample is still pending.
- If the user sees another 30-second launch, run `run-startup-samples.ps1` with a larger count or preserve the exact abnormal `GAME-SMOKE` folder, then classify it by comparing `LaunchToStartupPatternMs`, `Bootstrap.Awake totalMs`, and individual startup segments.
