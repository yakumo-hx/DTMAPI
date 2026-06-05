# 20260604-0002 0.2.5 critical manual QA fixes

## Summary

Bumped DTMAPI from 0.2.4 to 0.2.5 and implemented the critical manual QA fixes for SecondMotor residue/mail gating, Mine official research and internal storage, and Y-console search lifecycle.

## Source Request

The 2026-06-04 `/goal` required a 0.2.5 follow-up to `readme.md`: SecondMotor failed-summon cleanup and disabled-mod empty-mail prevention, Mine via official Industrial tech tree with Oil x10 + Steel Ingot x10 and 16-slot E-key storage, and Y-console search reset at save/title boundaries.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/SecondMotorMod/*`
- `testmods/MineMod/*`
- `testmods/OilMod/*`
- `testmods/MoreEquipmentSlotsMod/*`
- `testmods/*/manifest.json` and `official-info.json` version metadata for controlled 0.2.5 packages
- `tools/scripts/install-to-game.ps1`
- `docs/debug/issues/ISSUE-006-20260604-critical-manual-qa-025.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Version: controlled sources moved from 0.2.4 to 0.2.5 (`Directory.Build.props`, runtime API version, official-local test mod manifests, and installer minimum-version metadata).
- SecondMotor: ordinary mod entry/registration/mail paths now require the official content source `Local.DTMAPI_SecondMotor` to be enabled. Mail delivery can require an enabled content source, verifies native item generation before sending, and treats missing/disabled attachments as failed instead of sending an empty mail. The GameBridge cleanup path runs at title/save boundaries and after failed summons for DTMAPI-owned second-motor runtime objects.
- Mine: the official JSON is now an `EquipmentFuncCase` with 16-slot / 4-column storage, recipe inputs `dtmapi_oil x10` and `steel_ingot x10`, and default unlock off. The bridge injects the official Industrial tech tree node `dtmapi_mine` to the right of `alloy_material` and above `指挥官`, applies 2x renderer scale to placed Mines, and stores production output in the Mine-owned storage before any generic fallback.
- Y console: save/title boundaries reset search text, category, source filter, paging, and transient give targets. Console open logs now record the current lifecycle state so first-open empty state and same-save preserved search can be checked directly.

## Validation

- Release build/unit: passed through `tools/scripts/build.ps1 -Configuration Release` and again through subsequent smoke-install builds. Warnings were NU1900 vulnerability metadata lookups caused by restricted NuGet network access; build/test errors were 0.
- Enabled-state full third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260604-111533`.
  - `result.json`: `VehicleSecondMotor=true`, `NewContentMineOfficialJson=true`, `NewContentMineProduction=true`, `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, `NewContentEquipmentSlots=true`, `DebugConsoleOpenY1=true`, `DebugConsoleHoldYNoFlicker=true`, `InstantSave=true`, `DebugTeleportCsv=true`, `DebugTeleport=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
  - SecondMotor evidence: dual visible original/second motors, appearance isolation, second ride, edge transition with `secondInCurrentRoom=True`, `originalVisibleAfterTransition=False`, `originalAtNewEntry=False`, `noStuck=True`, dismount/original restore, and original summon.
  - Mine evidence: `recipeInputs=dtmapi_oilx10|steel_ingotx10`, `EquipmentFuncCase`, `caseStorage=16/4`, native tech `parent=alloy_material`, `rightOfParent=True`, `aboveCommander=True`, `rendererScale=2x2`, `outputTarget=equipment-storage`, `storage=2/16`, `storageLineCapacity=4`.
- Disabled SecondMotor smoke: `docs/debug/evidence/GAME-SMOKE/20260604-111901`.
  - Log audit: `Skip=2 Registration=0 Mail=0`, proving disabled `Local.DTMAPI_SecondMotor` did not register ordinary vehicle behavior or call mail delivery. Process check reported no `DolocTown.exe`.
- Y-console lifecycle smoke: `docs/debug/evidence/GAME-SMOKE/20260604-112250`.
  - Log audit: `OpenEmpty=1 OpenOil=7 SecondMotorMail=0`. The first open after `SaveLoaded` logged `searchText=<empty> category=<empty> sourceFilter=__base`; later same-save reopens preserved `searchText=石油` and `sourceFilter=Local.DTMAPI_Oil`. Process check reported no `DolocTown.exe`.

## Evidence Links

- Full pass: `docs/debug/evidence/GAME-SMOKE/20260604-111533`
- Disabled SecondMotor pass: `docs/debug/evidence/GAME-SMOKE/20260604-111901`
- Y-console lifecycle pass: `docs/debug/evidence/GAME-SMOKE/20260604-112250`
- Local enablement backups used during validation: `docs/debug/evidence/MANUAL-STATE`

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-006-20260604-critical-manual-qa-025.md`
- Smoke matrix: `MANUALQA-025-SECOND-MOTOR`, `MANUALQA-025-MINE-STORAGE`, `MANUALQA-025-Y-CONSOLE`
- Hook map: `Mail.ItemDelivery`, `Vehicle.MotorApi`, `UI.DebugConsoleHost`, `Machine.ProductionRuntimeLoop`, `Player.EquipmentSlotsApi`
- API matrix: `IDebugConsoleApi`, `IMailDeliveryApi`, `IMotorVehicleApi`, `IMachineProductionApi`, `IEquipmentSlotsApi`

## Rollback Notes

Reverting this update must restore the 0.2.4 version sources and the previous Mine JSON/production behavior, but that would re-open the manual QA blockers. Do not revert only the docs without also reverting the runtime changes they record. The validation temporarily toggled `Local.DTMAPI_SecondMotor`; the final local enablement state was restored to disabled (`enabled=false`, `priority=-1`) to match the pre-validation state.

## Follow-Up

Manual visual QA is still useful for the Mine official research node screenshot and SecondMotor summon animation feel, but the required automated third-save evidence for this 0.2.5 goal is present.
