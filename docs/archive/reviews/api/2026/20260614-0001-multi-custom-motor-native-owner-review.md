# 2026-06-14 Multi Custom Motor Native Owner Review

Status: implementation-input
Branch/worktree: `codex/multi-custom-motor-api-20260614` / `E:\Python_project\DTMAPI-multi-motor`
Source request: rebuild the vehicle API and SecondMotorMod toward stable multi custom flying motors, with the first rebuilt mod behaving like the native flying motor but independent, key sold by the official phone booth, eighth/ninth save validation, and no mail delivery.

## Required Context

- `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits/06-vehicle-motor-api.md`
- `docs/reviews/manual-qa/2026/20260614-0002-multi-custom-motor-api-review.md`
- `docs/goals/2026/20260614-0001-multi-custom-motor-api.md`
- reverse baseline `references/doloc-town/reverse/builds/23465763_workshop_38581E` from the main local worktree only; no reverse files are copied into the new worktree.

## Native Owner Findings

### 1. Original motor key is not a generic vehicle key

`DolocTown.ItemMotorKey.OnUse` owns the native key behavior, but it immediately checks the archive's native motor unlock flag. If the original motor is locked, native use exits before any summon. When unlocked and allowed by the current room, the method summons or repositions the singleton `DolocAPI.Motor` through `DolocAPI.SetMotorPosition(...)`, `MotorController.AutoFlyTo(...)`, and native room bookkeeping.

API conclusion: a custom key cannot call the whole native `ItemMotorKey.OnUse` path, especially on the ninth save where the native vehicle is not unlocked. DTMAPI must intercept custom registered key item ids before the native unlock check, then route only the allowed semantic parts: room policy, near-agent summon target, clone visibility/fly-to, and diagnostics.

### 2. Native vehicle persistence is singleton-only

`MotorDataManager` stores a single native motor unlock flag, room id, dungeon name, and position. `DolocAPI.Motor`, `DolocAPI.Agent`, `DolocAPI.AgentTransform`, `DolocAPI.DroneFollowTarget`, `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, and `DolocAPI.ResetMotorStatus` all assume that one native motor.

API conclusion: there is no stable native multi-vehicle registry or key-to-vehicle table. DTMAPI may provide a managed custom-motor bridge, but it must keep per-custom-vehicle state separate and restore native singleton state after custom ride/summon/gate transitions. This remains GameBridge-owned and must not expose `MotorController`, Unity, or Harmony types.

### 3. Ride lifecycle is native-owned and tightly coupled

`AgentControllerState.GetOnMotor/GetOffMotor` hides/shows the body, moves camera/drone follow targets, sets the active motor's riding state, updates driver hat visuals, clears selected item/tips, and on dismount writes native motor room state. `MotorInteractable.OnInteract` always calls the native ride path. `ManagerGate.TryEnterOnMotor/TryQuitOnMotor` also branches through native ride state.

API conclusion: a custom motor can only mimic native riding by a scoped GameBridge route that temporarily gives native ride lifecycle a DTMAPI-owned controller and then restores the original controller/snapshot. This is not an ordinary mod-level patch point.

### 4. Official phone booth sale has a native content owner

The phone booth store exists as `phone_booth_shop`. Store item records are loaded from `store_tbstoreitemlist`, and mod content can extend a store through `mod_tbmodstoreextension`. `Tables.HandleModStoreExtension` appends `extra_items` into the target `StoreInfo.ItemRecords_Ref` after resolving item refs. `Store.SpawnStoreItems` treats records with non-positive spawn weight as fixed items, and `StoreUiState.BuyItem` places bought items through native backpack placement.

API conclusion: the rebuilt SecondMotorMod should provide a custom key item plus `mod_tbmodstoreextension.json` targeting `phone_booth_shop`. This replaces the old mail delivery path and keeps acquisition inside official content/store ownership.

### 5. Official vehicle appearance assets are global replacements

Official Workshop vehicle examples use asset keys like `sprite_vehicle_motor` and `sprite_vehicle_motor_light_mask`, plus an anchor JSON. Installed sample paths include:

- `D:\Steam\steamapps\workshop\content\2285550\3705665433\Content\01 美化模组示例 Replacing Existing Content Assets\09 载具 Vehicle\sprite_vehicle_motor.png`
- `D:\Steam\steamapps\workshop\content\2285550\3705665433\Content\01 美化模组示例 Replacing Existing Content Assets\09 载具 Vehicle\sprite_vehicle_motor_light_mask.png`

These keys globally replace the original native motor if installed as ordinary content.

API conclusion: the rebuilt SecondMotorMod must not copy those assets as global replacements. DTMAPI uses them only as runtime appearance input for the cloned custom motor through an owner-private scoped sprite adapter.

## Current DTMAPI Gap

Existing `IMotorVehicleApi.RegisterSecondMotor` is an experimental, hard-coded second-motor clone adapter:

- one key id, one vehicle id, and a `SecondMotorRuntime`;
- no public definition for multiple key ids mapping to one custom vehicle;
- no per-vehicle acquisition/source metadata;
- speed multiplier is a temporary global motor tuning window around cloned `MotorController.OnFixedUpdate`;
- appearance is instance-scoped tint only;
- `SecondMotorMod` still binds `IMailDeliveryApi` and mails the key after save load;
- the content package defines only `item_tbitem.json` for `dtmapi_second_motor_key`, with no phone booth store extension.

## API Boundary Decision

For this branch:

- Keep original motor helpers restricted/experimental.
- Replace the public author-facing model with a custom motor definition that can register one vehicle id, one or more key item ids, and a native-like flying-motor profile.
- Treat custom motor runtime/summon/ride/transition as GameBridge-owned experimental. Eighth/ninth save evidence now proves the first rebuilt SecondMotor route avoids original-motor unlock contamination, but that is not yet enough to stabilize arbitrary custom vehicle classes.
- Treat phone-booth content acquisition as official JSON/content ownership, not runtime mail delivery.
- Do not claim wholly new vehicle classes, non-motor collision boxes, mechs, tractors, attack animations, or alternative movement forms as stable in this branch. Those remain future API slices.

## Validation Targets

- Eighth save: no official motor key carried; give/buy custom key, use it, summon custom motor, interact/E ride, move left for room transition, dismount, verify original motor state is not overwritten.
- Ninth save: native motor locked; custom key still summons/rides the custom motor, proving the path does not depend on native `IsMotorUnlocked`.
- Phone booth/content: the key appears through `mod_tbmodstoreextension` fixed store item ownership; no mail delivery logs should occur.
- Appearance: custom motor visual differs from original without installing global `sprite_vehicle_motor` replacements into the DTMAPI package.

## Validation Evidence

- Release build/test passed on 2026-06-14 with 0 warnings / 0 errors and `DTMAPI.UnitTests: OK`.
- `git diff --check` passed with line-ending warnings only.
- Slot 8 smoke `GAME-SMOKE/20260614-063456` passed: custom key give/use, scoped sprite appearance from `Content/DTMAPI/vehicle-appearance/sprite_vehicle_motor.png`, summon, ride, dismount, native 1x speed, and clean exit/fatal/process checks.
- Slot 9 smoke `GAME-SMOKE/20260614-063639` passed: custom key/scoped sprite/summon/ride works while the official vehicle remains locked, with clean exit/fatal/process checks.
- A pre-fix slot 8 run `GAME-SMOKE/20260614-043810` failed during summon because a reflected scoped-sprite appearance exception aborted the whole clone path. The fix keeps sprite failures diagnostic-only and falls back to instance-scoped tint so appearance cannot break native-like vehicle behavior.
- Intermediate smokes `GAME-SMOKE/20260614-045125` and `GAME-SMOKE/20260614-045247` proved key/summon/ride/isolation after that failure, but they are superseded because scoped sprite still fell back to tint before the replacement texture rect/pivot fix.

## Blockers

- No native generic vehicle registry exists. Any API name implying arbitrary stable vehicles must stay experimental/proposed.
- Scoped sprite replacement for a cloned `MotorController` is not yet proven. If runtime texture replacement cannot be verified, use tint/diagnostics and keep appearance stable claim blocked.
