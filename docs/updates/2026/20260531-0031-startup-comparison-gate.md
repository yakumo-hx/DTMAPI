# 20260531-0031 Startup Comparison Gate

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up, focused on the remaining occasional 30-second startup investigation.
- Add a repeatable comparison step for normal startup evidence plus candidate abnormal evidence.
- Do not claim the normal-vs-abnormal runtime requirement is satisfied unless a true DTMAPI runtime slow sample exists.

## Changed Files

- `tools/scripts/compare-startup-evidence.ps1`
- `tools/scripts/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0031-startup-comparison-gate.md`

## Known Facts And Rejected Hypotheses

- Known: `analyze-startup-evidence.ps1` classifies individual evidence folders but did not produce a specific goal-level comparison status.
- Known: current abnormal candidates are blocked/no-fresh-log or pre-runtime evidence, not true DTMAPI runtime slow evidence.
- Rejected: using blocked/no-fresh-log evidence as the abnormal half of the requested DTMAPI runtime startup comparison.

## Implementation

- Added `compare-startup-evidence.ps1`.
- The script:
  - accepts normal evidence and candidate abnormal evidence paths;
  - invokes `analyze-startup-evidence.ps1`;
  - writes `startup-comparison.json`;
  - writes `startup-comparison.md`;
  - reports one of:
    - `RuntimeSlowComparisonReady`
    - `OnlyPreRuntimeOrBlockedAbnormalEvidence`
    - `PendingAbnormalRuntimeEvidence`
    - `MissingNormalBaseline`
    - `NeedsReview`

## Validation

- Parser checks:
  - `compare-startup-evidence.ps1`: passed.
  - `run-startup-observer.ps1`: passed.
- Comparison validation:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\compare-startup-evidence.ps1 -NormalEvidencePath docs\debug\evidence\STARTUP-OBSERVE\20260531-191352 -CandidateEvidencePath docs\debug\evidence\STARTUP-OBSERVE\20260531-191241`
  - Wrote `docs/debug/evidence/STARTUP-COMPARE/20260531-192226`.
- Final validation after docs update:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `STARTUP-COMPARE/20260531-192226/startup-comparison.md` reports:
  - `Status=OnlyPreRuntimeOrBlockedAbnormalEvidence`
  - `Normal DTMAPI startup=1`
  - `Runtime slow=0`
  - `Blocked/no fresh runtime log=1`
- Normal baseline used in validation:
  - `STARTUP-OBSERVE/20260531-191352`
  - `LaunchToStartupPatternMs=7236`
  - `Bootstrap.Awake totalMs=230`
  - `DiscoverMods totalMs=53`
  - `ModLoad elapsedMs=37`
  - `Bootstrap.HarmonyInitialize elapsedMs=173`
- Candidate abnormal used in validation:
  - `STARTUP-OBSERVE/20260531-191241`
  - Classified as `SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog`
  - No `DolocTown.exe`, no fatal popup, and no fresh DTMAPI runtime log.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0030-startup-external-observer.md`

## Rollback Notes

- Runtime behavior is unchanged.
- Roll back by removing `compare-startup-evidence.ps1` and its README/debug references; raw analysis remains available through `analyze-startup-evidence.ps1`.

## Follow-Up

- A true abnormal 30-second DTMAPI runtime sample is still pending.
- When such evidence is captured, rerun `compare-startup-evidence.ps1` with the current normal baseline and the abnormal evidence; only `RuntimeSlowComparisonReady` should satisfy the runtime comparison portion of F.
