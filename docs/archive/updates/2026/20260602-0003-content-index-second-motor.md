# 20260602-0003 Content Source Index And Second Motor

## Source Request

Upgrade DTMAPI to 0.2.2, extend the Y-key debug console inventory path so it can read official local MODS and Steam Workshop item sources without taking over official loading, add an experimental motor/vehicle API, and ship a DTMAPI official-local second-motor example mod.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `testmods/DebugConsoleMod/*`
- `testmods/SecondMotorMod/*`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Summary

- Bumped DTMAPI runtime/build metadata to `0.2.2`.
- Added a read-only content item source index over official local `MODS` and Steam Workshop `content/2285550`, including source kind, source title/id, Workshop ID, enabled state, runtime-loaded/can-give separation, icon path, and content root.
- Extended inventory debug DTOs and the Y console inventory UI with mod item filtering, source labels, unavailable-state hints, and native give guards.
- Added experimental `IMotorVehicleApi` for original motor state/summon plus DTMAPI-managed second motor registration, summon, ride, dismount, events, status, speed multiplier, and failure reasons.
- Added GameBridge motor hooks for `ItemMotorKey.OnUse`, `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor/GetOffMotor`, `MotorController.OnFixedUpdate`, `DolocAPI.UnlockMotor`, and `DolocAPI.SetMotorPosition`.
- Added `DTMAPI.SecondMotorMod` as an official-local package with `dtmapi_second_motor_key` content, Chinese/English text, 2x speed multiplier registration, and installer-time copying of local official Workshop example vehicle assets. No official images are stored in the repo.
- Hardened smoke automation for Workshop item enablement, second-motor key summon/ride/dismount/original restore, and Y console key reliability.

## Validation

- Build: `tools/scripts/build.ps1 -Configuration Release` passed with 0 errors and `DTMAPI.UnitTests: OK`.
- Third-save vehicle smoke: `docs/debug/evidence/GAME-SMOKE/20260602-112946` passed through Steam with `SaveLoaded=true`, `DebugInventory=true`, `VehicleSecondMotor=true`, `NoFatalInstanceWindow=true`, and `ProcessExited=true`.
- Workshop item smoke: temporarily enabled `Workshop.3715788753` (`矿物种子`) from `mod_infos.json`, backed up/restored the file under `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-115707`, and passed `docs/debug/evidence/GAME-SMOKE/20260602-115735`; log shows `coal_seed` from `Workshop.3715788753`, `runtimeLoaded=True`, before `0`, after `1`, and `workshopRuntimeItem=verified`.
- Y console smoke: `docs/debug/evidence/GAME-SMOKE/20260602-120014` passed with `DebugConsoleOpenY1=true`, `DebugConsoleCloseEscape=true`, `DebugConsoleOpenY2=true`, `DebugConsoleCloseY=true`, `DebugConsoleTenYShortTaps=true`, `DebugConsoleHoldYNoFlicker=true`, and no leftover process.
- Butter Workshop UI/give smoke: temporarily enabled subscribed `Workshop.3722791728` (`多洛可战前料理 | 奶油 & 黄油系列`), backed up/restored `mod_infos.json` under `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-122846`, and passed `docs/debug/evidence/GAME-SMOKE/20260602-122848`; log and screenshot summary show Y console `modItemsOnly=True`, search `黄油`, hover/source evidence for `mod_butter`, and native backpack give `mod_butter` before `0`, after `1`, `given=1`.
- Latest vehicle smoke in `docs/debug/evidence/GAME-SMOKE/20260602-122848` records `speedMultiplier=2`, `baseMaxSpeed=25`, `effectiveMaxSpeed=50`, and unchanged endurance progress `enduranceSummon=1`, `enduranceRide=1`, `enduranceDismount=1`.
- Disabled motor location evidence: retained failed smoke `docs/debug/evidence/GAME-SMOKE/20260602-112141` records the third save initially in a motor-disabled room and `Second motor key intercepted ... success=False reason=in-house`; the passing vehicle smoke then recovers to the farm outdoor room before key summon/ride.

## Evidence Links

- `docs/debug/evidence/GAME-SMOKE/20260602-112946`
- `docs/debug/evidence/GAME-SMOKE/20260602-115735`
- `docs/debug/evidence/GAME-SMOKE/20260602-120014`
- `docs/debug/evidence/GAME-SMOKE/20260602-122848`
- `docs/debug/evidence/GAME-SMOKE/20260602-112141`
- `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-115707`
- `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-122846`

## Rollback

- Revert the 0.2.2 source/API changes and remove `testmods/SecondMotorMod`.
- Re-run `tools/scripts/install-to-game.ps1` from the previous revision to replace installed DTMAPI binaries and official-local packages.
- If Workshop smoke was interrupted, restore `SAVE/mod_infos.json` from the relevant `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/*/mod_infos.before.json`; the final butter-mod restore confirmation is `WORKSHOP-MODINFO-BACKUP/20260602-122846/restore-confirm.txt`.

## Follow-Up

- Convert the second-motor disabled-location check into a passing-run state-machine step once a stable indoor mark point is available from the teleport whitelist.
- Keep `IMotorVehicleApi` experimental until more than one vehicle mod exercises custom visuals/tuning.
