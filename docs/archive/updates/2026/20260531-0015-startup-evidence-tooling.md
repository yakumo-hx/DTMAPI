# 20260531-0015 Startup Evidence Tooling

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing completed UI, ActionSpeed, AutoFishing, instant-save, or OneAction work.
- Move the startup 30-second investigation closer to repeatable evidence by making normal/slow/blocked launch classification scriptable.
- Do not optimize startup paths without a true DTMAPI 30-second segment sample.

## Changed Files

- `tools/scripts/analyze-startup-evidence.ps1`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0015-startup-evidence-tooling.md`

## Known Facts And Rejected Hypotheses

- Known: normal DTMAPI startup segment samples currently complete in under one second.
- Known: the reproduced blocked samples do not have fresh DTMAPI segment logs and do not create `DolocTown.exe`.
- Rejected: treating missing runtime logs as DTMAPI slowness.
- Rejected: guessing which startup phase to optimize before a sample has `Startup segment` lines near 30000ms.

## Implementation

- Added `analyze-startup-evidence.ps1`.
  - Accepts one or more evidence folders.
  - Parses `result.json`, `process-check.txt`, `fatal-window-check.txt`, `DTMAPI-latest.log`, and `BepInEx-LogOutput.log`.
  - Extracts startup segment timings for manifest scan, official MODS scan, Workshop scan, content query index, mod load, runtime start, Harmony initialize, icon load, and total Bootstrap Awake.
  - Classifies each evidence folder as normal DTMAPI startup, true DTMAPI slow startup, Steam/pre-process launch blocker, or needs review.
- Updated `collect-logs.ps1`.
  - Copies `steam-appmanifest-2285550.acf`.
  - Writes `steam-info.txt`.
  - Captures tails of Steam `console_log.txt`, `bootstrap_log.txt`, `content_log.txt`, and `workshop_log.txt`.
  - Generates `startup-analysis.json` and `startup-analysis.md` for the collected evidence folder.

## Validation

- Startup comparison:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\analyze-startup-evidence.ps1 -EvidencePath docs\debug\evidence\GAME-SMOKE\20260531-160943,docs\debug\evidence\GAME-SMOKE\20260531-161545,docs\debug\evidence\GAME-SMOKE\20260531-123258`
  - Passed: `docs/debug/evidence/STARTUP-COMPARE/20260531-162725`.
- Log collection:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\collect-logs.ps1 -CaseId STARTUP-COLLECT-TEST`
  - Passed: `docs/debug/evidence/STARTUP-COLLECT-TEST/20260531-162737`.

## Evidence

- `STARTUP-COMPARE/20260531-162725/startup-analysis.md` reports:
  - Normal DTMAPI startup: 2
  - DTMAPI startup slow: 0
  - Steam/pre-process blocked or no fresh runtime log: 1
  - Needs review: 0
- The report classifies `GAME-SMOKE/20260531-160943` and `GAME-SMOKE/20260531-161545` as normal DTMAPI startup.
- The report classifies `GAME-SMOKE/20260531-123258` as `SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog`.
- `STARTUP-COLLECT-TEST/20260531-162737` contains `steam-appmanifest-2285550.acf`, `steam-info.txt`, Steam log tails, and generated startup-analysis files.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0014-startup-normal-vs-blocked.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If the analyzer misclassifies a future sample, disable report generation in `collect-logs.ps1` and keep the raw Steam/appmanifest copies.

## Follow-Up

- A true DTMAPI 30-second startup sample is still pending. It must include fresh `Startup segment` lines before scanner, icon, Harmony, config, or mod-load code should be optimized.
