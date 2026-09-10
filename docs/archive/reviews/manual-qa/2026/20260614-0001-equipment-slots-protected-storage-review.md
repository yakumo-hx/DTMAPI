# MoreEquipmentSlots Protected Storage Review

Date: 2026-06-14
Source: user discussion after MoreEquipmentSlots duplicate/cross-save concern and follow-up note about the Workshop "paper box expansion" mod.

## 1. Extra equipment slots need uninstall protection

User feedback:

- MoreEquipmentSlots should provide three extra interactive accessory slots.
- Hover/display should use the official equipment preview path.
- Extra slots may accept hats plus the four known special passive accessories (`grandmas_button`, conductor watch, herb package, propeller radiator) and future similar tagged items.
- Extra-slot effects should load, but player character visuals should remain owned by the vanilla equipment slots.
- The critical missing behavior is uninstall/disable protection: when the mod is closed or unsubscribed, items stored in extra slots should return to the backpack, mail overflow, or ground instead of disappearing or duplicating.

Analysis:

- The current implementation is not a native dynamic equipment-slot expansion. The game has fixed equipment fields (`hatItem`, `droneItem`, `activeItem`, `passiveItem1`, `passiveItem2`), while DTMAPI stores extra attribute-only items in a sidecar and injects `AgentEquipmentFunction` entries.
- The historical duplicate risk comes from sidecar storage identity, not only MoreEquipmentSlots UI. A no-save reload can duplicate if DTMAPI commits sidecar state without the native save, and cross-save pollution can happen if sidecar files are keyed only by owner.
- Existing transaction work already delays writes until native `SaveGame` postfix and clears dirty state at `SaveLoaded` / title return. The remaining high-risk gap is that the storage path was global (`equipment-slots-<owner>.json`) instead of scoped to a save identity.

Decision:

- Keep `IEquipmentSlotsApi` Experimental.
- Do not claim a stable native dynamic equipment-slot API yet.
- Implement the safest first step as DTMAPI-owned protected storage: per-save sidecar path, save identity metadata, compatibility guard, legacy global migration, and missing-mod orphan recovery through native backpack placement with overflow email enabled.

## 2. Paper box expansion suggests tail-first protected slot semantics

User feedback:

- A third-party Workshop "paper box expansion" mod has a similar unresolved problem: after removing the mod, what happens to items stored in expanded slots?
- This suggests expanded slots should be treated as tail slots, saved/recovered from the last slot backwards.

Analysis:

- This is a useful general rule for expanded containers: vanilla slots are usually the head of the container, while mod-added slots are usually appended at the tail.
- Applying tail-first recovery avoids treating low-index vanilla slots as the risky region and makes future generic protected-container APIs more consistent.
- This branch must not ingest third-party mod code or promise third-party protection without a registration contract. The paper-box case is retained as a design input for a future protected-container API, not as implemented compatibility.

Decision:

- MoreEquipmentSlots recovery now processes stored entries by descending index.
- Stored entries include `tailIndexFromEnd` metadata so future protected-container work can reuse the tail-slot model.
- Generic third-party protected storage remains a follow-up requiring an explicit DTMAPI registration API, per-save ownership, and real uninstall/recovery smoke evidence.

## 3. 2026-06-14 user manual QA after local install

User feedback:

- Equipping extra-slot items applies their effects.
- Extra-slot items do not change the player character sprite; native character visuals remain owned by the vanilla equipment slots.
- Equipping, saving, exiting, and re-entering the save does not duplicate items.
- Extra-slot state does not pollute other saves.
- Disabling the MoreEquipmentSlots mod returns protected items to the backpack.
- Remaining issue: after disabling the equipment-slot mod and re-entering the save in the same game process, the three extra slot visuals still appear. They are inert and reject item placement with red warning text. A full game restart removes the visuals. The user classified this as a systemic hot-reload issue that is especially visible on this mod.

Analysis:

- The core protected-storage target passed manual QA: effects, visual containment, save/reload no-copy behavior, cross-save isolation, and disabled-mod item recovery all worked in the user's local install.
- The remaining issue is current-process runtime/UI-registration residue, not evidence that protected storage failed. DTMAPI already treats already-loaded code mods disabled through the official UI as requiring a game restart before the DLL and all runtime registrations are fully gone.
- MoreEquipmentSlots registers its `IEquipmentSlotsApi` owner on entry/save load. There is currently no public unregister path and no internal GameBridge hot-disable cleanup that removes the reflected slot clones for a loaded-but-now-disabled owner.
- The red warning on item placement is useful evidence: disabled-state gating blocks new placement, while the previously cloned slot UI remains visible until process restart clears GameBridge state.

Decision:

- Mark the protected-storage behavior as user-verified except for current-process hot-disable visual cleanup.
- Keep `IEquipmentSlotsApi` Experimental.
- Track the stale empty-slot visuals as a separate DTMAPI hot-reload/unregister lifecycle follow-up. The safer next fix should not unload DLLs; it should detect loaded disabled owners on official reload/save entry and internally unregister or hide their GameBridge UI registrations while preserving orphan recovery.

## Validation Required

- Unit tests: per-save scope key, path-safe owner/scope names, tail-first order, archive/player/save-clock compatibility guard.
- Build/test: Release build and Release tests.
- User manual QA passed: equip extra slot, native save, reload same save, no duplicate; load another save, no cross-save item; disable MoreEquipmentSlots, protected item returns to backpack.
- Remaining validation before marking the broader lifecycle complete: automated coverage for the user-verified flows, mail-overflow recovery, disabled/unsubscribed recovery after Workshop removal, clean exit evidence, and current-process hot-disable visual cleanup.
