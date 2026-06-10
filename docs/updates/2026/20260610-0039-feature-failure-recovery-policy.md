# 20260610-0039 Feature Failure Recovery Policy

## Status

Verified.

## Source Request

User requested the e904d04 post-review stabilization route, second branch `codex/fix-feature-failure-recovery-policy`, to make GameBridge feature failure throttling recover cleanly after a stable success period and to clarify cumulative failure-count semantics.

## Summary

- Kept `FailureCount` as a cumulative total for diagnostics snapshots and hook-status details.
- Added internal consecutive failure tracking and recovery timestamp details to formatted feature status:
  - `consecutiveFailureCount`
  - `lastRecoveredAt`
- Added a recovery episode policy: after three consecutive successful dispatches for the same `featureId + operation`, the previous failure throttle state is cleared, so the next failure starts a fresh diagnostics episode.
- Kept repeated failure throttling behavior unchanged inside an active episode: first full error, two short warnings, then 30-second summary windows.
- Did not add public diagnostics snapshot members, rename existing fields, change public APIs, change feature service behavior, or change smoke result schemas.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0039-feature-failure-recovery-policy.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies:
  - repeated `UnitFeature/Update` failures still record only one diagnostics error inside the active episode;
  - cumulative `FailureCount` reaches 6 while hook-status publication remains throttled;
  - recovered status preserves cumulative `FailureCount=6`, resets `consecutiveFailureCount=0`, and records `lastRecoveredAt`;
  - after three successful `Update` dispatches, a new failure records a second diagnostics error and starts with `consecutiveFailureCount=1`.
- DirectExe third-save Camera smoke `GAME-SMOKE/20260610-164830` passed: `Zoom=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; report zip `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165010.zip`.
- DirectExe third-save ActionSpeed smoke `GAME-SMOKE/20260610-165043` passed: `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; report zip `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165123.zip`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-164830`
  - `docs/debug/evidence/GAME-SMOKE/20260610-165043`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165010.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-165123.zip`

## Rollback

- Remove consecutive failure/recovery fields from `GameBridgeFeatureStatus`.
- Restore feature failure throttle state so success dispatches do not clear the `featureId + operation` episode.
- Remove the recovery episode unit coverage and related matrix/update references.

## Follow-Up

- Continue the post-review stabilization route with the mechanical `FishingAutomationFeature` split.
