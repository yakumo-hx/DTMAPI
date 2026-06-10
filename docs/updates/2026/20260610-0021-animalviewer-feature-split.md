# 20260610-0021 AnimalViewer Feature Split

## Status

Verified.

## Source Request

User requested the post-midterm follow-up route and explicitly selected `AnimalViewerFeature` as one of the two additional feature splits.

## Summary

- Split `IAnimalViewerApi` ownership out of `DolocTownExperimentalBridgeApi` into `AnimalViewerFeature` and `AnimalViewerService`.
- Added `AnimalViewerHookBridge` to own the `AnimalFullInfoData(Animal)`, `AnimalViewer.Show`, and `AnimalPanel.RefreshViewer` hook installation.
- Changed AnimalViewer hook callbacks to call `Bridge.AnimalViewerService` rather than `Bridge.ExperimentalApi`.
- Changed smoke delayed screenshot/progress-row checks to use `AnimalViewerService`.
- Moved the animal progress overlay refresh loop from the experimental bridge runtime loop to `AnimalViewerFeature.Update()`.
- Removed the experimental bridge's duplicate `Animals.ViewerRendering` pending status publication.
- Kept `Animals.ViewerRendering`, `Smoke.AnimalPanelUi`, `Smoke.AnimalViewerUi`, `Smoke.AnimalViewerProgressUi`, public DTOs, hidden-produce rendering behavior, result schema, and AnimalHusbandryProgress behavior unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save AnimalViewer smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoOpenAnimalPanel -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `GAME-SMOKE/20260610-103119`
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `AnimalViewerUi=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Logs: `Feature.AnimalViewer = ready`; `Animals.ViewerRendering = verified`; `Smoke.AnimalViewerProgressUi = verified`; `Smoke.AnimalPanelUi = verified`; `Smoke.AnimalViewerUi = verified`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
  - Report pointer: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-103155.zip`.
  - Exit check: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md#hook-animalsviewerrendering`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-103119`

## Rollback

- Re-register `IAnimalViewerApi` directly on `DolocTownExperimentalBridgeApi`.
- Move AnimalViewer hook installation back into `DolocTownGameBridge.InstallHarmonyHooks()`.
- Point callbacks and smoke helpers back to `ExperimentalApi`.
- Re-run the third-save `-IncludeHookProbe -AutoOpenAnimalPanel` smoke.

## Follow-Up

- Keep `IAnimalViewerApi` Experimental; this branch only moved ownership and did not promote the API or change UI/flicker behavior.
