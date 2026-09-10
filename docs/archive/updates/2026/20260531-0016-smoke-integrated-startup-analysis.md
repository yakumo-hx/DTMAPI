# 20260531-0016 Smoke Integrated Startup Analysis

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up and keep improving the startup 30-second evidence chain.
- Avoid redoing already verified UI, F6, ActionSpeed, instant-save, and OneAction gameplay work.
- Make future smoke evidence self-contained enough to compare startup segments with launch-blocking failures.

## Changed Files

- `tools/scripts/collect-logs.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/analyze-startup-evidence.ps1`
- `tools/scripts/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0016-smoke-integrated-startup-analysis.md`

## Known Facts And Rejected Hypotheses

- Known: before this change, `run-game-smoke.ps1` wrote `result.json` in its primary evidence folder while `collect-logs.ps1` created a separate folder for runtime/Steam logs and startup analysis.
- Known: that split was enough for manual inspection but weaker for future compressed-context audits.
- Rejected: starting a direct `DolocTown.exe` smoke to force another fatal/startup sample; Steam remains the accepted smoke launch path.

## Implementation

- Added `-OutputDirectory` to `collect-logs.ps1`.
  - When provided, it writes logs into the existing evidence folder.
  - It preserves an existing smoke `summary.txt` and writes `collect-summary.txt`.
  - It now writes `fatal-window-check.txt` in addition to process and Steam evidence.
- Added `-Quiet` to `analyze-startup-evidence.ps1` for embedded use by other scripts.
- Updated `run-game-smoke.ps1` so primary smoke evidence includes:
  - `result.json`
  - DTMAPI/BepInEx/Unity logs
  - Steam appmanifest/log tails
  - `process-check.txt`
  - `fatal-window-check.txt`
  - `startup-analysis.json`
  - `startup-analysis.md`

## Validation

- Output-directory log collection:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\collect-logs.ps1 -CaseId STARTUP-COLLECT-OUTPUTDIR-TEST -OutputDirectory docs\debug\evidence\STARTUP-COLLECT-OUTPUTDIR-TEST\20260531-163610`
  - Passed and preserved the existing `summary.txt` while writing `collect-summary.txt`, Steam evidence, fatal-window check, and startup analysis.
- Integrated title smoke:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 120 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-163330`.
- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.

## Evidence

- `GAME-SMOKE/20260531-163330/result.json` has `StartupLog=true`, `GameLaunched=true`, `TitleSettingsMenu=true`, `TitleSettingsMenuScreenshotFile=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`.
- `GAME-SMOKE/20260531-163330/startup-analysis.md` classifies the run as `NormalDtmapiStartup`.
- The same analysis records `Bootstrap.Awake totalMs=659`, `DiscoverModsMs=62`, `ModLoadMs=456`, `HarmonyMs=601`, and `IconMs=9`.
- `GAME-SMOKE/20260531-163330/process-check.txt` says `No DolocTown.exe process found.`
- `GAME-SMOKE/20260531-163330/fatal-window-check.txt` says `No fatal instance popup found.`
- The same folder contains `steam-appmanifest-2285550.acf`, `steam-info.txt`, and Steam log tails.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`, `UI-004`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/hook-map/README.md`: `Diagnostic: Startup.SegmentTiming`
- Previous record: `docs/updates/2026/20260531-0015-startup-evidence-tooling.md`

## Rollback Notes

- Runtime behavior is unchanged.
- If smoke evidence folders become too large, keep `startup-analysis.*`, `steam-info.txt`, and appmanifest, and reduce Steam log tail length rather than removing startup classification.

## Follow-Up

- A true DTMAPI 30-second startup sample is still pending. The integrated smoke evidence now has the right shape to classify it when it appears.
