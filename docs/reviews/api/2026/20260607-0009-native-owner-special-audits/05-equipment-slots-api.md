# 05 - EquipmentSlots API

Scope: `IEquipmentSlotsApi.RegisterSlots`, `GetSlots`, `EquipExtraSlot`, `UnequipExtraSlot`, `GetState`, `RecoverExtraSlotItems`, state/result DTO semantics, save transaction, UI clone, and native stat owner.

## 1. Files read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/MoreEquipmentSlotsMod/ModEntry.cs`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260603-0015-equipment-slots-storage-recovery-smoke.md`
- `docs/updates/2026/20260603-0021-mine-placement-equipment-ui-smoke.md`
- `docs/updates/2026/20260606-0002-028-readme-implementation.md`
- `docs/updates/2026/20260606-0004-029-readme-implementation.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/UI.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/UI.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Items_Inventory.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`

## 2. Functions read

- `IEquipmentSlotsApi.*`, `ExperimentalGameBridge.cs:142`.
- `EquipmentSlotsOptions`, `ExperimentalGameBridge.cs:741`.
- `EquipmentSlotsRegisterResult/State/SlotInfo/EquipResult/RecoveryResult`, `ExperimentalGameBridge.cs:910`.
- `DolocTownGameBridge.InstallHarmonyHooks`, equipment slice, `DolocTownGameBridge.cs:1206`.
- `DolocTownHookCallbacks.AgentEquipmentReloadParamsPostfix`, `DolocTownHookCallbacks.cs:316`.
- `AccessoriesBarInitPostfix/AccessoriesBarOnStartShowPostfix`, `DolocTownHookCallbacks.cs:321`.
- `MoreEquipmentSlotsMod.BindEquipmentSlotsApi`, `testmods/MoreEquipmentSlotsMod/ModEntry.cs:36`.
- `NotifyEquipmentSlotsSaveLoaded/SaveSaved/ReturnedToTitle`, `DolocTownExperimentalBridgeApi.cs:134`.
- `ResetEquipmentSlotSessionState`, `DolocTownExperimentalBridgeApi.cs:177`.
- `RegisterSlots`, `DolocTownExperimentalBridgeApi.cs:2597`.
- `GetSlots`, `DolocTownExperimentalBridgeApi.cs:2628`.
- `EquipExtraSlot`, `DolocTownExperimentalBridgeApi.cs:2637`.
- `UnequipExtraSlot`, `DolocTownExperimentalBridgeApi.cs:2717`.
- `GetState`, `DolocTownExperimentalBridgeApi.cs:2761`.
- `RecoverExtraSlotItems`, `DolocTownExperimentalBridgeApi.cs:2785`.
- `BuildEquipmentSlotsState`, `DolocTownExperimentalBridgeApi.cs:2887`.
- `EnsureEquipmentSlotStorageLoaded`, `DolocTownExperimentalBridgeApi.cs:2944`.
- `SaveEquipmentSlotStorage/PersistEquipmentSlotStorage`, `DolocTownExperimentalBridgeApi.cs:2986`.
- `TryApplyStoredEquipmentSlotFunctions`, `DolocTownExperimentalBridgeApi.cs:3072`.
- `GetNativeAgentEquipmentManager`, `DolocTownExperimentalBridgeApi.cs:3172`.
- `TryApplyEquipmentSlotFunction`, `DolocTownExperimentalBridgeApi.cs:3185`.
- `TryValidateExtraEquipmentSlotItem`, `DolocTownExperimentalBridgeApi.cs:3281`.
- `RecoverEquipmentSlotEntries/RecoverEquipmentSlotEntry`, `DolocTownExperimentalBridgeApi.cs:3334`.
- `ApplyEquipmentSlotsAfterReloadParams`, `DolocTownExperimentalBridgeApi.cs:6321`.
- `RenderEquipmentSlotsUi*`, `DolocTownExperimentalBridgeApi.cs:6352`.
- `ConfigureEquipmentSlotUiClone`, `DolocTownExperimentalBridgeApi.cs:6497`.
- `HandleEquipmentSlotUiClick/EquipExtraSlotFromNativeBuffer/HandleEquipmentSlotUiHover`, `DolocTownExperimentalBridgeApi.cs:6597`.

## 3. Call graph

```text
MoreEquipmentSlotsMod.Entry / SaveLoaded
  -> IEquipmentSlotsApi.RegisterSlots
  -> EnsureEquipmentSlotStorageLoaded(config/equipment-slots-{owner}.json)
  -> EnsureEquipmentSlotEntries
  -> TryApplyStoredEquipmentSlotFunctions
     -> GetNativeAgentEquipmentManager
     -> TryGenerateNativeItem
     -> AgentEquipmentFunction.CreateAgentEquipmentFunction
     -> manager.functions[item] = function
     -> manager.ReloadParams()

EquipExtraSlot(owner, slot, item)
  -> CountItem / CostItem native backpack
  -> TryGenerateNativeItem
  -> validate ItemPassive or ItemHat function/skill
  -> store DTMAPI sidecar entry
  -> TryApplyStoredEquipmentSlotFunctions
  -> mark sidecar dirty, wait for native SaveGame postfix

Native hooks
  -> AgentEquipmentManager.ReloadParams postfix updates state
  -> AccessoriesBar.__Init/OnStartShow postfix clones UI slots
  -> AccessorySlot click callbacks call Equip/Unequip

Save lifecycle
  -> SaveLoaded / ReturnedToTitle clears in-memory extra slots and dirty state
  -> SaveGame postfix persists DTMAPI sidecar storage
```

## 4. Function body findings

- Registration accepts options and immediately creates DTMAPI runtime entries/storage; it does not register new native visual slots (`DolocTownExperimentalBridgeApi.cs:2602`, `:2617`).
- `EquipExtraSlot` consumes exactly one backpack item through native `CostItem`, validates generated native item type/function, and then stores the item in a DTMAPI runtime entry (`DolocTownExperimentalBridgeApi.cs:2670`, `:2685`, `:2688`).
- Validation is intentionally narrow: only passive attribute equipment or hats with equipment skills are accepted (`DolocTownExperimentalBridgeApi.cs:3298`, `:3307`, `:3314`, `:3321`).
- Native stat reach is real but attribute-only: DTMAPI creates an `AgentEquipmentFunction` and inserts it into `AgentEquipmentManager.functions` (`DolocTownExperimentalBridgeApi.cs:3200`, `:3217`, `:3225`).
- Sidecar storage is not written when equipment changes. It is only marked dirty, and `PersistEquipmentSlotStorage` runs after native `SaveGame` postfix (`DolocTownExperimentalBridgeApi.cs:2992`, `:147`).
- `ResetEquipmentSlotSessionState` clears entries, loaded storage owners, dirty owners, and states on `SaveLoaded`/`ReturnedToTitle`, explicitly discarding unsaved mutations (`DolocTownExperimentalBridgeApi.cs:177`, `:193`).
- UI is not native slot registration. It clones existing `AccessoriesBar` slots, repositions them, binds click/hover callbacks, and renders a maximum of six DTMAPI entries (`DolocTownExperimentalBridgeApi.cs:6409`, `:6423`, `:6497`).
- Recovery uses native backpack placement with email overflow enabled (`DolocTownExperimentalBridgeApi.cs:3397`), but if that fails the item remains stored and recovery reports failure.

## 5. Native owner verdict

`Partial/Gap`. Native stat and inventory owners are reached, but the public API's extra slot model is DTMAPI-owned sidecar storage plus cloned UI. No native extra-slot save format, native slot list, or official equipment UI data model is registered.

Reverse/map evidence: UI maps confirm `AgentEquipmentManager` and `AccessoriesBar` classes (`23465763.../maps/UI.md:57`, `:68`), `AgentEquipmentFunction` lifecycle candidates (`:153` to `:158`), `DolocAPI.EquipHat` and `DolocAPI.get_AgentEquipmentManager` candidates (`:194`, `:198`), and risky internal fields on `AgentEquipmentFunction` (`:354` to `:358`). Inventory maps list `ItemFunctionHat` and passive-function metadata rows (`23249387.../maps/Items_Inventory.md:470` to `:477`).

## 6. Ordinary mod usability

`仅 DTMAPI 自家 mod 可用`. Ordinary mods should not depend on this as a stable equipment-slot API yet.

## 7. Concrete failure modes

- Unsaved equip/unequip changes are intentionally discarded on `SaveLoaded` or `ReturnedToTitle` because dirty sidecar writes wait for native `SaveGame`.
- Sidecar storage path is per owner id under DTMAPI config, not per native save slot; without a per-save namespace, an ordinary mod can pollute state across saves.
- Stats can remain unapplied until `AgentEquipmentManager` exists and `AgentEquipmentFunction.CreateAgentEquipmentFunction` succeeds.
- UI can render or bind differently after official UI changes because it clones `AccessoriesBar` internals rather than registering a native slot model.
- Recovery can fail if native backpack/email overflow placement fails, leaving an item stored in DTMAPI sidecar state.
- Visual equipment slots are deliberately preserved and not extended; mods expecting hats/clothes visuals from extra slots will get attribute-only behavior.

## 8. Minimal rebuild direction

- Split stable contract into "attribute function adapter" and "extra slot storage/UI" layers.
- Namespace sidecar storage by native save identity before ordinary mods can depend on it.
- Replace cloned UI with a native-backed or explicitly DTMAPI-owned UI contract whose limits and lifecycle are documented.
- Add transaction events around equip, native save postfix, recovery, and rollback.
- Keep current `MoreEquipmentSlotsMod` as DTMAPI-owned experimental consumer until those owners are rebuilt.

## 9. Evidence gaps

- No new smoke was run in this round.
- I did not inspect official decompiled method bodies for `AgentEquipmentManager.ReloadParams`, `AccessoriesBar`, or `AgentEquipmentFunction`; I used DTMAPI code, existing evidence, and reverse map rows.
- Per-save-slot isolation is not evidenced in current code.
- The audit did not prove every `AgentEquipmentFunction` subtype behaves correctly when inserted into `manager.functions` from a generated item outside a native slot.
