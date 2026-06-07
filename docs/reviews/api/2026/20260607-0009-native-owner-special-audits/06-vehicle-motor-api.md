# 06 - Vehicle/Motor API

Scope: `IMotorVehicleApi` original motor calls, DTMAPI second motor registry/summon/ride/dismount, key/interactable hooks, room transition sync, and DTO state semantics.

## 1. Files read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/SecondMotorMod/ModEntry.cs`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260603-0008-second-motor-mail-status-smoke.md`
- `docs/updates/2026/20260603-0009-second-motor-dual-visible-inline-config.md`
- `docs/updates/2026/20260603-0010-original-motor-appearance-restore.md`
- `docs/updates/2026/20260603-0020-second-motor-edge-transition-smoke.md`
- `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Motor.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Motor.md`

## 2. Functions read

- `IMotorVehicleApi.*`, `ExperimentalGameBridge.cs:116`.
- `SecondMotorOptions` and motor DTOs, `ExperimentalGameBridge.cs:564`.
- `DolocTownGameBridge.InstallHarmonyHooks`, motor slice, `DolocTownGameBridge.cs:1140`.
- `DolocTownHookCallbacks.ItemMotorKeyOnUsePrefix`, `DolocTownHookCallbacks.cs:256`.
- `MotorInteractableOnInteractPrefix`, `DolocTownHookCallbacks.cs:261`.
- `AgentControllerStateGetOnMotorPostfix/GetOffMotorPostfix`, `DolocTownHookCallbacks.cs:266`.
- `MotorControllerOnFixedUpdatePrefix/Postfix`, `DolocTownHookCallbacks.cs:276`.
- `UnlockMotorPostfix/SetMotorPositionPostfix/EnterRoomPostfix`, `DolocTownHookCallbacks.cs:286`.
- `SecondMotorMod.BindVehicleApi`, `testmods/SecondMotorMod/ModEntry.cs:75`.
- `SecondMotorMod.CanRunOfficialFeatures`, `testmods/SecondMotorMod/ModEntry.cs:198`.
- `GetOriginalMotorState/GetVehicleState/GetVehicles`, `DolocTownExperimentalBridgeApi.cs:4813`.
- `RegisterSecondMotor`, `DolocTownExperimentalBridgeApi.cs:4843`.
- `UnlockOriginalMotor`, `DolocTownExperimentalBridgeApi.cs:4890`.
- `SummonOriginalMotor`, `DolocTownExperimentalBridgeApi.cs:4923`.
- `SummonVehicle`, `DolocTownExperimentalBridgeApi.cs:4975`.
- `RideVehicle`, `DolocTownExperimentalBridgeApi.cs:4994`.
- `DismountVehicle`, `DolocTownExperimentalBridgeApi.cs:5038`.
- `HandleMotorKeyUse/HandleMotorInteract`, `DolocTownExperimentalBridgeApi.cs:5209`.
- `NotifyMotorGetOn/NotifyMotorGetOff`, `DolocTownExperimentalBridgeApi.cs:5241`.
- `ApplySecondMotorTuningForFixedUpdate/RestoreSecondMotorTuningAfterFixedUpdate`, `DolocTownExperimentalBridgeApi.cs:5279`.
- `NotifyOriginalMotorPositionChanged`, `DolocTownExperimentalBridgeApi.cs:5315`.
- `NotifyEnterRoomForActiveSecondMotor`, `DolocTownExperimentalBridgeApi.cs:5338`.
- `UpdateActiveSecondMotorRoomSnapshot`, `DolocTownExperimentalBridgeApi.cs:5387`.
- `MirrorOriginalMotorTransformForSecondMotorAgentPosition`, `DolocTownExperimentalBridgeApi.cs:5443`.
- `CleanupSecondMotorResidueForBoundary/CleanupSecondMotorRuntime`, `DolocTownExperimentalBridgeApi.cs:9056`.
- `SummonSecondMotor`, `DolocTownExperimentalBridgeApi.cs:9198`.
- `EnsureSecondMotorInstance`, `DolocTownExperimentalBridgeApi.cs:9255`.
- `TryStartSecondMotorRide`, `DolocTownExperimentalBridgeApi.cs:9396`.
- `CaptureOriginalMotorSnapshot/RestoreOriginalMotorSnapshot`, `DolocTownExperimentalBridgeApi.cs:9477`.
- `BuildOriginalMotorState/BuildSecondMotorState`, `DolocTownExperimentalBridgeApi.cs:9519`.
- `CanCallMotorInCurrentRoom`, `DolocTownExperimentalBridgeApi.cs:9594`.

## 3. Call graph

Original motor:

```text
UnlockOriginalMotor
  -> DolocAPI.UnlockMotor(float)

SummonOriginalMotor
  -> optional DolocAPI.UnlockMotor(float)
  -> CanCallMotorInCurrentRoom
  -> DolocAPI.SetMotorPosition(CurrentRoom, near-agent Vector2)
  -> DolocAPI.Motor.AutoFlyTo(agent-position getter, null)
```

Second motor:

```text
RegisterSecondMotor
  -> DTMAPI dictionaries: secondMotors, secondMotorsByKeyItemId
  -> official enablement check

ItemMotorKey.OnUse prefix or SummonVehicle
  -> SummonSecondMotor
     -> EnsureSecondMotorInstance
        -> clone DolocAPI.Motor
        -> Init/Reset/SetVisible(false)
        -> instance-scoped tint
     -> SetVisible(true)
     -> AutoFlyTo or SetMotorControllerPosition

RideVehicle or MotorInteractable.OnInteract prefix
  -> TryStartSecondMotorRide
     -> save original AgentControllerState.motorController
     -> capture original DolocAPI.Motor snapshot
     -> set AgentControllerState.motorController = cloned second motor
     -> AgentControllerState.GetOnMotor()

GetOff / lifecycle
  -> restore original controller and original motor snapshot
  -> destroy/hide cloned second motor on SaveLoaded/ReturnedToTitle/failure cleanup
```

## 4. Function body findings

- Original motor methods call real native owners. `UnlockOriginalMotor` reflects `DolocAPI.UnlockMotor(float)` (`DolocTownExperimentalBridgeApi.cs:4902`), and `SummonOriginalMotor` reflects `DolocAPI.SetMotorPosition` plus `MotorController.AutoFlyTo` (`:4953`, `:4959`).
- `RideVehicle` deliberately refuses programmatic original-motor ride with `native-only` (`DolocTownExperimentalBridgeApi.cs:4994`).
- `RegisterSecondMotor` is DTMAPI registry state plus official enablement check; it does not mutate a native vehicle table (`DolocTownExperimentalBridgeApi.cs:4862`, `:4868`).
- `EnsureSecondMotorInstance` clones `DolocAPI.Motor`, initializes/resets it, hides it, then tracks controller/interactable in DTMAPI sets (`DolocTownExperimentalBridgeApi.cs:9262`, `:9271`, `:9306`).
- Riding a second motor works by replacing `AgentControllerState.motorController` with the clone before invoking native `GetOnMotor` (`DolocTownExperimentalBridgeApi.cs:9430`, `:9435`, `:9453`).
- During second-motor riding, DTMAPI captures/restores the original motor snapshot with `DolocAPI.SetMotorPosition` and intercepts `SetMotorPosition`/`EnterRoom` effects (`DolocTownExperimentalBridgeApi.cs:9477`, `:9495`, `:5315`, `:5338`).
- Speed multiplier is applied by temporarily changing global motor tuning fields around cloned `MotorController.OnFixedUpdate` (`DolocTownExperimentalBridgeApi.cs:5279`). This is a global-parameter patch window, not per-native-vehicle data.
- Lifecycle cleanup destroys/hides clone objects and restores original controller/snapshot at save-load/title/failure boundaries (`DolocTownExperimentalBridgeApi.cs:9056`, `:9074`).

## 5. Native owner verdict

- Original motor state/summon/unlock: `Partial/Watch`, native owner reached.
- Second motor registry/summon/ride: `Gap`, because the native game appears to own a singleton motor model and DTMAPI implements a clone/routing adapter.

Research evidence: the motor research note lists `DolocAPI.Motor`, `UnlockMotor`, `SetMotorPosition`, and `ResetMotorStatus` as global API owners (`research-DolocTown-Motor-Vehicle-API.md:75` to `:83`), summarizes `UnlockMotor` and `SetMotorPosition` duties (`:88` to `:96`), and identifies `AgentControllerState.GetOnMotor/GetOffMotor` responsibilities including camera, body, drone, scanner, selected item, and operation-tip cleanup (`:184` to `:205`). It also marks true multi-vehicle support as high risk until `DolocAPI.Motor` singleton, save data, UI, camera, drone, collision layer, and tag semantics are handled (`:277` to `:282`, `:346` to `:353`).

## 6. Ordinary mod usability

- Original motor calls: `debug-only / restricted`.
- Second motor calls: `仅 DTMAPI 自家 mod 可用`.

## 7. Concrete failure modes

- Singleton pollution: native owners still revolve around `DolocAPI.Motor`, so a second clone can drift from the original motor's saved room/position.
- Controller routing failure: if `AgentControllerState.motorController` cannot be set or `GetOnMotor` changes, second-motor riding fails or leaves the player/controller in a mismatched state.
- Cross-room transition mismatch: DTMAPI must capture `EnterRoom` and mirror positions; missed callbacks can put clone, agent body, and original motor in different rooms/positions.
- Global tuning contamination: speed changes temporarily mutate `DolocAPI.GlobalParameter` motor fields around fixed update.
- Cleanup residue: failures depend on `CleanupSecondMotorRuntime` restoring controller/snapshot and destroying cloned GameObjects; a missed cleanup can leave hidden or visible clone residue.
- Official enablement mismatch: `SecondMotorMod` blocks behavior when official content source/key source is disabled; ordinary mods ignoring that can register vehicles whose keys or content do not exist.

## 8. Minimal rebuild direction

- Keep original motor helpers restricted/debug until a stable owner map covers summon, ride, room policy, and player consent.
- Do not document second motor as a stable vehicle registry.
- Future stable vehicle API must first decide whether to embrace a DTMAPI-owned clone system or replace native singleton semantics with real multi-vehicle storage.
- Minimum owner slices: vehicle prefab/renderer, save data per vehicle, summon/key routing, riding controller, room transition, speed/tuning, camera/drone/body sync, UI/motor bar, collision/layer/tag, cleanup.

## 9. Evidence gaps

- No new smoke was run in this round.
- I did not copy or inspect decompiled source bodies; native details came from DTMAPI code, update/smoke records, and local research/map notes.
- The second-motor clone has smoke evidence in prior records, but this audit did not revalidate every failure/cleanup branch.
- No ordinary-mod multi-vehicle conflict test exists; current evidence is DTMAPI's own `SecondMotorMod`.
