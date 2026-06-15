# 20260615-0006 Low-Risk Workspace Cleanup Round 1

Date: 2026-06-15
Status: implemented-static
Branch: `codex/refactor-four-step-cleanup`

## Source Request

The user asked for a second-level branch to run a four-step workspace/API cleanup and refactor plan, with each round committed separately before any merge back to the first-level branch. Round 1 covers low-risk cleanup that should not change runtime behavior.

## Summary

Removed stale active project references and legacy source that made the current workspace harder for a fresh Codex or IDE user to understand. Changed log collection so heavy runtime evidence is no longer copied recursively by default, then added a compact current-state onboarding document.

## Changed Files

- `DTMAPI.sln`
- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `PROJECT.md`
- `docs/onboarding/current-state.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/issues/ISSUE-003-hotkey-openconfig-no-overlay.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260615-0006-low-risk-workspace-cleanup-round1.md`

This commit also keeps the prior audit records created for this cleanup track:

- `docs/reviews/code/2026/20260615-0001-codex-handoff-space-maintainability-audit.md`
- `docs/updates/2026/20260615-0005-codex-handoff-space-maintainability-audit.md`

## Details

- Removed the archived `SecondMotorMod` project block, build configuration rows, and nested-project row from `DTMAPI.sln`.
- Deleted `ReflectedImGuiOverlay.cs`, which was not constructed by `BootstrapPlugin` and represented an old overlay route beside the active title settings and debug console UI hosts.
- Added `collect-logs.ps1 -IncludeRuntimeEvidence` for explicit full runtime evidence copying.
- Changed the default `collect-logs.ps1` behavior to write `DTMAPI-evidence-skipped.txt` with the runtime evidence root and recent child folders instead of recursively copying the whole runtime evidence tree.
- Kept formal game-smoke evidence portable by making `run-game-smoke.ps1` call `collect-logs.ps1 -IncludeRuntimeEvidence`.
- Removed archived `DTMAPI.SecondMotorMod` from the current title config screenshot rotation so title UI smoke no longer creates second-motor screenshot false positives.
- Added `docs/onboarding/current-state.md` as a short current-state entry for fresh Codex sessions.
- Linked the new onboarding entry from `PROJECT.md`.
- Updated old hook/design/debug docs so they no longer point future work at the deleted IMGUI fallback path, while preserving historical evidence as historical.

## Validation

- Passed: `git diff --check` with line-ending warnings only.
- Passed: PowerShell parser check for `tools/scripts/collect-logs.ps1` and `tools/scripts/run-game-smoke.ps1`.
- Passed: `tools/scripts/collect-logs.ps1 -OutputDirectory tmp/collect-logs-round1-light-2`; default output wrote `DTMAPI-evidence-skipped.txt` and did not copy a `DTMAPI-evidence` directory.
- Passed: `tools/scripts/test.ps1` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: bundled dotnet `sln DTMAPI.sln list`; the active solution no longer lists `SecondMotorMod`.
- Passed: bundled dotnet `build DTMAPI.sln -c Release --no-restore` with 0 warnings and 0 errors.

No game smoke has been run yet for this static cleanup round. Runtime hook behavior is not expected to change.

## Evidence And Related Records

- Review source: `docs/reviews/code/2026/20260615-0001-codex-handoff-space-maintainability-audit.md`
- Prior archive update: `docs/updates/2026/20260615-0004-second-motor-archive.md`
- API matrix note: `docs/api/public-api-matrix.md` marks `IMotorVehicleApi` as Experimental and archived SecondMotor evidence as research history only.
- Smoke matrix archived vehicle row: `docs/debug/regressions/smoke-matrix.md`

## Rollback

- Re-add the removed `SecondMotorMod` solution project rows only if the archived sample is deliberately restored to `testmods/SecondMotorMod`.
- Restore `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs` only if a future UI task intentionally resurrects that host and wires it from `BootstrapPlugin`.
- Revert the `collect-logs.ps1` default behavior only if full runtime evidence copying is explicitly required for every collection. Prefer `-IncludeRuntimeEvidence` for one-off full payloads.

## Follow-Up

- Run the Round 1 sub-agent review and commit this round separately.
- Start Round 2 with a MotorVehicle/SecondMotor reference audit before deleting public or GameBridge API code.
