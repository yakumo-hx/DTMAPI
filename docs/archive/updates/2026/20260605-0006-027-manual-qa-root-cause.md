# 20260605-0006 0.2.7 Manual QA Root-Cause Fixes

Status: implemented
Area: version/gamebridge/ui/packaging/smoke

## Source Request

The user asked to continue the `readme.md` 0.2.7 manual-QA implementation goal after compaction, explicitly excluding motor execution in this continuation.

## Changed Files

- Version/runtime/package sources: `Directory.Build.props`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`, `tools/scripts/install-to-game.ps1`, and 0.2.7 mod manifests/official-info for DebugConsole, Mine, and AnimalHusbandryProgress.
- Y-console host/input: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`, `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`, `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`, and `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`.
- Machine/Animal/GameBridge: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`, and Mine official recipe data.
- Installer/smoke/docs: `tools/scripts/run-game-smoke.ps1`, `docs/debug/INDEX.md`, `docs/debug/issues/ISSUE-008-20260605-manual-qa-027-root-cause.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`, `readme.md`, and this update/index entry.

## Summary

- Bumped controlled DTMAPI sources from 0.2.6 to 0.2.7.
- Fixed Y-console right-click give by removing the stale recent-target fallback and using current mouse-position hit testing against visible item cells; localized right-click give-10 status text.
- Added modal input isolation while the Y console is open through GameBridge prefixes for native `AgentControllerState.EnterUICheck`, `UseTool`, and `UseItem`.
- Reworked AnimalHusbandryProgress UI evidence to a single-pass native data path by writing `AnimalFullInfoData.moodInfo/moodProgress` before `AnimalViewer.OnShow` uses the native mood progress bar, and stopped cloning extra post-render progress rows.
- Updated Machine production catch-up so the runtime loop scans current/root/farm/archive candidate rooms, processes bounded due cycles after native pass-time, preserves due time on failed production, and lets smoke force only the observation poll instead of forcing production due.
- Confirmed normal packaging leaves `ConfigMenuExample` dev-only and not installed in game `Mods`, official local `MODS`, or ordinary `BepInEx/plugins`.

## Validation

- `tools/scripts/build.ps1` passed twice after this slice with 0 errors; UnitTests reported OK. NuGet `NU1900` vulnerability-index warnings remain expected under restricted network access.
- Y-console third-save DirectExe smoke `GAME-SMOKE/20260605-230442` passed with `AutoExerciseVehicle=False`, `DebugConsoleMouseGive=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTeleport=true`, `DebugTime=true`, `ProcessExited=true`, `ForcedClose=false`, and no fatal popup.
- Mine-only third-save DirectExe smoke `GAME-SMOKE/20260605-231924` passed with `AutoExerciseVehicle=False`, `AutoExerciseNewContentApis=False`, `AutoExerciseMineContentApis=True`, `NewContentMineOfficialJson=true`, `NewContentMineOfficialTechTreeUi=true`, `NewContentMineProduction=true`, `NewContentMineApis=true`, `ProcessExited=true`, `ForcedClose=false`, and no fatal popup.
- Animal-only third-save DirectExe smoke `GAME-SMOKE/20260605-232019` passed with `AutoOpenAnimalPanel=True`, `AutoExerciseVehicle=False`, `AnimalViewerUi=true`, `ProcessExited=true`, `ForcedClose=false`, and no fatal popup.
- Packaging check after normal install found no `DTMAPI.ConfigMenuExample`; BepInEx/plugins contains only the `DTMAPI` bootstrap directory.

## Evidence

- Y-console input isolation log: `GAME-SMOKE/20260605-230442` logs `UI.DebugConsoleInputIsolation = verified`, `AgentControllerState.EnterUICheck suppressed`, `Debug console right-click give source=mouse1-hit-test`, and native backpack placement `requested=10 placed=10`.
- Mine pass-time catch-up log: `GAME-SMOKE/20260605-231924` logs native time skip `06:30 -> 18:00`, five `MachineProduction cycle OK` entries, final `afterCycles=5`, `outputTarget=equipment-storage`, `storage=9/16`, `storageLineCapacity=4`, and screenshots under `DTMAPI-evidence/NEWCONTENT-025/20260605-232005`.
- Animal single-pass log: `GAME-SMOKE/20260605-232019` logs `Smoke.AnimalViewerProgressUi = verified`, `single-pass native moodBar`, `moodInfo=羊毛脂 0/100`, and evidence `DTMAPI-evidence/ANIMAL-001/20260605-232057`.
- Retained failed diagnostics: `GAME-SMOKE/20260605-225731` showed stale right-click failure before hit-test; `GAME-SMOKE/20260605-231408` showed Mine first-observation throttling before forced poll-only smoke.

## Rollback Notes

Revert the 0.2.7 version bump, GameBridge/UI changes, Mine recipe/manifests, smoke harness additions, and documentation entries. If only the smoke forced-poll addition is rolled back, Mine pass-time production may again initialize due after the native time skip rather than before it.

## Follow-up

- User manual testing should still check long real gameplay sessions for Mine production across sleep and farm/container rooms, because the automated smoke proves native pass-time catch-up and candidate-room enumeration but does not exhaust every persistent room topology.
- The `IMachineProductionApi`, `IAnimalViewerApi`, and debug-console APIs remain experimental because they still depend on reflected game internals.
