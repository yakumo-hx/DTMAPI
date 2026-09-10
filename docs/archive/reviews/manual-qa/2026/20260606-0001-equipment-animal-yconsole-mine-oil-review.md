# Manual QA Review: Equipment Slots, Animal UI, Y Console, Mine, Oil

Date: 2026-06-06 00:03:51 +08:00
Reviewer role: feedback-to-goal / root-cause review Codex
Scope: review and conversion only; no runtime implementation, no build/game smoke.
Source: user manual QA with screenshots for MoreEquipmentSlots, AnimalHusbandryProgress, Y console, MineMod, and OilMod.

## Manual Feedback Header

- Time: 2026-06-06 00:03:51 +08:00.
- Source: user manual testing plus three screenshots.
- Scope: create a durable review record, update a dedicated goal file, and prepare a short implementation `/goal`.
- Forbidden: do not implement code in this review pass; do not reset/revert current 0.2.7 worktree changes; do not copy old DLKsmapi code.
- Current baseline: `docs/updates/2026/20260605-0006-027-manual-qa-root-cause.md` says DTMAPI 0.2.7 was build/smoke verified, but this review treats the user's newer manual QA as the current player-visible truth.

## 1. MoreEquipmentSlots extra slots are not real equipment slots

Original feedback:

- MoreEquipmentSlots adds extra slots, but they seem unable to actually equip items.
- Extra slots have no mouse-hover preview. Screenshot 1 shows the native accessory slot hover label, but hovering the extra slots does not show similar information.
- Check whether the game's default accessory 2 slot only accepts grandma's button, conductor's pocket watch, herb pack, and thruster radiator.
- If so, check what the three DTMAPI extra slots can accept.
- Desired behavior: extra slots can accept hats and accessory-2-style passive items. Hats in extra slots should apply special effects only and should not apply the hat texture/appearance.

Screenshot-to-text:

- Screenshot 1 shows the player equipment strip with native slots on the left and three DTMAPI extra slot icons on the right.
- The mouse hover over the native passive slot displays the native label `饰品1`.
- The DTMAPI extra slots are rendered as dark chest-like boxes, with no visible hover tooltip or preview.

Review record:

- User-confirmed facts: the extra slots appear visually but do not behave like real interactive equipment slots; hover preview is missing.
- Screenshot/log observations: native slot hover is visible; extra slot hover is not visible.
- Code/document facts:
  - `testmods/MoreEquipmentSlotsMod/README.md` explicitly describes the current player strip as `read-only player equipment strip rendering`.
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs` logs and exposes the same contract: DTMAPI owns storage, stat application, and a read-only strip.
  - `RenderEquipmentSlotsUi` clones an existing AccessoriesBar slot, calls its `Render(icon)`, then sets the cloned button `interactable=false`.
  - `TryValidateExtraEquipmentSlotItem` rejects anything that is not `DolocTown.ItemPassive`; hats are rejected as `not-passive`.
  - Native `EquipmentBarUiState` uses real slot select/click callbacks and native hover through `ShowEquipmentItemViewer`.
  - Native `AgentEquipmentManager.EquipPassiveItem2` accepts `ItemPassive`; `EquipHat` accepts `ItemHat`.
  - Official content contains `kit_passiveprop` entries for `grandmas_button`, `herbal_pouch`, `conductor_pocket_watch`, and `thruster_radiator`, and separate `kit_hat` entries for hats.
- Codex inference:
  - The user-visible issue is expected from the code: extra slots are storage/stat slots plus read-only UI, not native-like interactive slots.
  - The default accessory-2 behavior is `ItemPassive`; the listed four passiveprop items match the current base content set seen in official item JSON.
  - Allowing hats requires a deliberate attribute-only hat path: apply `ItemFunctionHatBase` skill/effect into `AgentEquipmentManager.functions`, but never write native `hatItem` or call hat visual update.
- Rejected/unproven:
  - It is not proven that every Workshop passive item is safe, only that the native filter is `ItemPassive`.
  - It is not proven that all hat functions are safe as attribute-only effects; implementation needs fallback/blocker if a hat function depends on native hat visuals.
- Ownership: DTMAPI GameBridge EquipmentSlots API plus MoreEquipmentSlotsMod UI/config.
- Needs update: dedicated goal file, `docs/debug`, hook/API matrix if the EquipmentSlots API changes, and smoke/manual evidence for native-like click/hover.
- Acceptance:
  - Extra slots can be clicked, hovered, equipped, unequipped, and recovered through player UI, not only through config text entry.
  - Extra slots accept accessory-2 passive items and hats.
  - Hats in extra slots apply only the special effect/stat and do not change player appearance.
  - Hover shows item name/description/effect for native and extra slots.
  - Disabling the mod safely returns stored items and clears extra functions.
- Blocker:
  - If native UI callbacks cannot be safely cloned or attached, keep the feature incomplete and report exact unsupported callback path instead of calling the read-only strip "done".

## 2. AnimalHusbandryProgress still needs UI/API upgrade

Original feedback:

- Screenshot 2 shows the animal hidden-product display still wrong.
- The animal bell mod or related API still needs upgrading.

Screenshot-to-text:

- Screenshot 2 shows the large animal shed UI.
- The selected animal is `沼泽兽1`.
- The right detail panel shows `饱食 0/100`.
- The hidden product text `沼泽兽的角 30/120` appears on or inside the mood/progress area, overlapping the normal row instead of behaving like a clean separate row.

Review record:

- User-confirmed facts: the animal hidden-product UI is still not acceptable after 0.2.7.
- Screenshot/log observations: the hidden-product label and count overlap the native progress-row area; it is not a stable, clean third row.
- Code/document facts:
  - 0.2.7 update says AnimalHusbandryProgress was changed to a `single-pass native moodBar` path.
  - Current `DecorateAnimalFullInfoData` still inserts hidden-product text into `stateDescription`.
  - `ApplyAnimalProgressSinglePassData` writes hidden-product label/count into `moodInfo` and progress into `moodProgress`.
  - `RenderAnimalProgressOverlay` now mainly records evidence for that moodBar path rather than drawing a separate row.
- Codex inference:
  - The latest screenshot matches the current code path: DTMAPI is still reusing/hijacking the native mood row rather than creating a true independent hidden-product row.
  - The earlier smoke evidence proves a marker reached the native viewer, but does not prove the player-visible layout is correct.
- Rejected/unproven:
  - The issue is not only a font-size tweak. The ownership of the row is wrong: mood row data is being reused for hidden product.
  - It is unproven whether a native extra row can be inserted cleanly; if not, a controlled DTMAPI row clone must be positioned and sized like native UI.
- Ownership: DTMAPI GameBridge Animal Viewer API plus AnimalHusbandryProgressMod.
- Needs update: dedicated goal file, debug issue/evidence, hook map/API matrix if a new animal viewer render-row API is added.
- Acceptance:
  - Animal UI shows hunger, mood, and hidden-product progress as separate player-readable rows.
  - Hidden product row does not overwrite or corrupt `moodInfo/moodProgress`.
  - Switching animals does not flash a wrong row before correction.
  - Font size returns to a native-like size; overlap with the progress bar is acceptable only if the row remains readable and intentional.
- Blocker:
  - If native AnimalViewer cannot support a third row safely, report the missing UI insertion path and leave the goal incomplete rather than hiding the product inside the mood row.

## 3. Y console item area and movement speed

Original feedback:

- Y console left-side item system should show more rows and fill the bottom area better.
- The `0.5x` move-speed button cannot be clicked/useful; remove this move-speed option.

Screenshot-to-text:

- Screenshot 3 shows `Y键控制台 0.2.7`.
- The item grid displays about five rows, with a large empty dark area below the grid.
- The right panel has movement speed buttons `0.5x`, `1x`, `2x`, `3x`, `4x`.

Review record:

- User-confirmed facts: the item grid feels underfilled; `0.5x` speed is not useful/clickable and should be removed.
- Screenshot/log observations: item grid stops high above the bottom edge; `0.5x` is still rendered.
- Code/document facts:
  - `ReflectedDebugConsoleUi.BuildItemsTab` sets `PageSize = 25`.
  - Item cells are arranged as `index % 5` columns, so 25 items means five rows.
  - Page controls are positioned below the existing fixed grid.
  - `BuildDebugSidePanel` hard-codes movement multipliers `{ 0.5, 1, 2, 3, 4 }`.
  - `SetMovementSpeed` resets speed for multipliers `<= 1`, so `0.5` cannot behave as a slow mode in the current implementation.
- Codex inference:
  - The empty bottom area is a direct result of the fixed 5x5 grid and page size.
  - Removing `0.5x` is safer than trying to make slow movement work, because current movement API treats `<= 1` as reset.
- Rejected/unproven:
  - This review does not prove whether a 6th/7th item row will collide with page/source/category controls; implementation must adjust layout and verify.
- Ownership: DTMAPI Bootstrap Y-console UI.
- Needs update: dedicated goal file and debug/smoke evidence for UI screenshot.
- Acceptance:
  - Item grid uses more vertical space and fills toward the bottom without overlapping page controls.
  - Page size matches the larger visible grid.
  - `0.5x` is removed; remaining speed choices are clear and clickable.
- Blocker:
  - If the Y-console fixed panel cannot fit more rows at current resolution, report the layout limit with screenshot evidence and do not silently overlap controls.

## 4. MineMod recipe and energy mode changes

Original feedback:

- Add one Mine crafting recipe rule:
  - If OilMod does not exist: consume metal frame x15, engine core x10, steel ingot x20, coal x100.
  - If OilMod exists and recipe replacement is enabled: consume metal frame x10, engine core x5, steel ingot x20, oil x10.
- Remove fuel consumption and fuel capacity.
- Keep only electric power consumption.

Screenshot-to-text:

- No new Mine screenshot in this turn. The issue is a gameplay/content requirement.

Review record:

- User-confirmed facts: Mine should become electric-only and have conditional recipes based on OilMod availability/config.
- Screenshot/log observations: none in this turn.
- Code/document facts:
  - `testmods/MineMod/Content/recipe_tbrecipe.json` currently consumes `dtmapi_oil x10` and `steel_ingot x10`.
  - `testmods/MineMod/ModEntry.cs` currently declares `AllowFuelMode = true`, `AllowElectricMode`, `DefaultMode`, `FuelCapacity`, `FuelOnlyFuelCostPerCycle`, and `ElectricModeFuelCostPerCycle`.
  - Mine config currently exposes `Fuel capacity` and `Electric mode`.
  - Machine API normalization currently forces at least one mode and still carries fuel fields in state/smoke assertions.
  - Existing smoke code still asserts `dtmapi_oil` and fuel/capacity fields.
- Codex inference:
  - This is both a MineMod content/config change and a DTMAPI Machine API contract cleanup. If the API still assumes fuel fields, Mine can look electric-only in config while retaining stale fuel behavior internally.
  - Conditional official recipe variants are not just a static JSON edit. Implementation must ensure the native recipe table/tech unlock shows only the intended active recipe and does not duplicate Mine in research or crafting.
- Rejected/unproven:
  - Exact official item IDs for metal frame, engine core, steel ingot, and coal must be verified from native content before editing JSON.
  - It is unproven whether official content reload can swap recipes safely after game launch. If not, recipe selection may need to happen before official tables load or require restart.
- Ownership: MineMod official JSON/config plus DTMAPI Machine API/content patch path.
- Needs update: dedicated goal file, update/debug records, smoke matrix, public API matrix if Machine API DTO/mode semantics change.
- Acceptance:
  - Mine config no longer exposes fuel capacity or fuel mode.
  - Machine state and logs show electric-only behavior and power cost only.
  - Without OilMod or with replacement disabled, recipe uses metal frame x15, engine core x10, steel ingot x20, coal x100.
  - With OilMod present and recipe replacement enabled, recipe uses metal frame x10, engine core x5, steel ingot x20, oil x10.
  - Official research/crafting UI shows one Mine recipe, not duplicates.
- Blocker:
  - If conditional official recipe replacement cannot be done safely, keep the goal incomplete and report whether restart/pre-load selection is required.

## 5. Oil item ID should be formal, not `dtmapi_oil`

Original feedback:

- Oil JSON item id should not be `dtmapi_oil`.
- Change it to a more formal item id.

Screenshot-to-text:

- No screenshot for this item in this turn.

Review record:

- User-confirmed facts: current `dtmapi_oil` id is considered too framework-like/test-like.
- Screenshot/log observations: none.
- Code/document facts:
  - `testmods/OilMod/Content/item_tbitem.json` uses `"id": "dtmapi_oil"`.
  - `testmods/OilMod/ModEntry.cs`, `testmods/MineMod/ModEntry.cs`, Mine recipe JSON, GameBridge oil drop logic, smoke assertions, and content index smoke all reference `dtmapi_oil`.
  - Oil localization keys also use `item_dtmapi_oil`.
- Codex inference:
  - This is a cross-cutting rename, not a single JSON edit. Runtime drops, Mine outputs/recipes, Y-console/content-index tests, and docs must be updated together.
  - Because a user may already have `dtmapi_oil` in backpack/storage after testing, implementation should consider a legacy alias/migration or at least document cleanup expectations.
- Rejected/unproven:
  - This review does not choose the final id. A good id should be content-facing and formal, not a framework/internal name.
- Ownership: OilMod official content plus GameBridge oil-drop/runtime references plus smoke/docs.
- Needs update: dedicated goal file, update/debug docs, smoke matrix, possibly content index/API docs.
- Acceptance:
  - New official oil item id is formal and documented.
  - All runtime references use the new id.
  - Mine recipes/output and coal drop use the new id.
  - Y-console can find and give the new oil item.
  - Old `dtmapi_oil` does not remain in active recipes, drops, smoke assertions, or user-facing docs except optional legacy/migration notes.
- Blocker:
  - If existing saves with old oil items cannot be migrated or safely ignored, report the save-content risk instead of silently changing only the JSON.

## Problem Grouping

- UI: MoreEquipmentSlots hover/click, Animal hidden-product row, Y-console item-grid density and movement buttons.
- API/GameBridge: EquipmentSlots API must move from read-only UI to native-like interaction; Animal Viewer API must stop overwriting mood row; Machine API must support electric-only semantics.
- Config/content: Mine conditional recipe, fuel removal, Oil item id rename.
- Testing/evidence: smoke must include player-visible screenshots, native hover/click behavior, Y-console layout, recipe variants, and oil rename checks.

## Boundary Constraints

Must do:

- Keep the user's five issues in order when generating implementation tasks.
- Bump DTMAPI one patch version in the implementation round.
- Treat 0.2.7 smoke as history, not as proof against this manual QA.
- Verify in the third local save with screenshots/logs for player-visible UI.

Must not do:

- Do not call the read-only equipment strip complete.
- Do not hide animal hidden-product progress inside the native mood row.
- Do not keep `0.5x` in the Y-console movement options.
- Do not keep Mine fuel/fuel-capacity UI or state if the target is electric-only.
- Do not leave active references to `dtmapi_oil` after the formal rename, except explicit migration/legacy notes if needed.

Optional:

- Improve MoreEquipmentSlots config-menu quick-equip UX, but it cannot replace real player equipment UI.
- Add legacy cleanup for old oil items if safe.

Blocker rules:

- Native-like extra equipment slot click/hover failure, unsafe animal third-row injection, unsafe conditional recipe swapping, or unsafe oil-id migration should block completion and be reported with code/log evidence.
