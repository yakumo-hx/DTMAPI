# 20260610-0047 Fishing Hardening Audit Packages

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the clean `85cd4af` Refactor baseline, with independent `codex/...` branches merged back to local `Refactor`, final full/web audit packages, and tag `refactor-fishing-hardening-followup-20260610`.

## Summary

- Merged the FishingAutomation service failure throttle, runtime state reset, native helper cleanup, options contract review, and AutoFishing smoke case split branches back to `Refactor`.
- Ran final `Refactor` build/test and third-save AutoFishing smoke after the merges.
- Updated audit package defaults so the refreshed full/web packages include the full FishingAutomation hardening evidence chain.
- Kept `IFishingAutomationApi`, `FishingAutomationOptions`, `FishingAutomationState`, hook/status IDs, smoke result schema, and API stability unchanged.
- Recorded that the AutoFishing smoke mode still leaves `latest-report.txt` pointing at stale `dtmapi-report-20260610-171030.zip`; the full/web packages cite the fresh evidence folders and do not treat that stale report zip as fresh report evidence.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0047-fishing-hardening-audit-packages.md`

## Validation

- `git diff --check` passed before final package refresh.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-205841` passed: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset events for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and animator restore `reason=AgentStateFishingPull.OnExit restored=2`.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- Final full/web audit packages were regenerated with `tools/scripts/update-audit-package.ps1`; package self-audit passed.

## Evidence Links

- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-205841`
- Prior branch evidence: `docs/debug/evidence/GAME-SMOKE/20260610-202047`, `docs/debug/evidence/GAME-SMOKE/20260610-203301`, `docs/debug/evidence/GAME-SMOKE/20260610-204134`, and `docs/debug/evidence/GAME-SMOKE/20260610-205153`
- Full audit package: `E:\Python_project\DTMAPI-audit-package-Refactor`
- Web audit package: `E:\Python_project\DTMAPI-audit-package-Refactor-web`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Revert this final package/documentation commit.
- Re-run `tools/scripts/update-audit-package.ps1` from the previous `Refactor` commit if the older package state is required.

## Follow-Up

- AutoFishing smoke should eventually export a fresh report zip or write an explicit no-report note so future audit packages do not have to explain the stale `latest-report.txt` pointer.
