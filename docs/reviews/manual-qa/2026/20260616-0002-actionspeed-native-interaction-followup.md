# 20260616-0002 ActionSpeed Native Interaction Follow-up

## Context

Follow-up to `docs/goals/2026/20260616-0001-actionspeed-bottom-layer-rebuild.md` after third-save manual testing of the 0616 ActionSpeed rebuild. The user clarified that the remaining slow behavior is not the text/tip animation; it is the native player action played when attempting to fertilize an already-fertilized tile or apply crop film to an already-covered tile.

## Feedback And Analysis

1. Water-well behavior is accepted.

   Manual result: empty hand `E` / any item `E` replenishes the well with normal acceleration; empty bottle left-click water fill and watering-can fill are single actions but visibly accelerated.

   Analysis: current `IWaterContainer` and `ItemBottle.DrawWater*` classification through `AgentControllerState.InteractContinues(float)` and `UseItemContinues(float)` is sufficient for these paths.

2. In-water bottle and bottled-water right-click behavior is accepted.

   Manual result: standing in water with an empty bottle and right-click continuous bottled-water drinking do not fall back to native speed.

   Analysis: `ItemBottle.DrawWaterInWater` and bottled-water `UseItemContinues` timer scaling remain valid.

3. Tool, eat/drink, machine add, harvest, resin, and wild vegetation behavior did not regress.

   Manual result: tool animations, eating/drinking, machine add, harvesting, resin collection, and wild vegetation collection remained accelerated.

   Analysis: these previously verified slices should be retained. The follow-up must not broaden harvest classification in a way that masks plant-item targeting failures.

4. Seed/fertilizer/crop-film actions still sometimes fall back to native speed, especially in lower rows; repeated fertilizer/film attempts on an already-applied tile are slow.

   Analysis: decompiled native owners show `ItemFertilizer.Fertilizer()` and `ItemFilm.Protect()` call `DolocAPI.agent._Interact(...)` before the callback checks `PlantBasin.Fertilizer(...)` or `PlantBasin.Protect(...)` and shows a failure message. Existing ActionSpeed classification incorrectly excluded `IsFertilizerd=true` and `IsProtected=true`, so already-fertilized/already-covered failure-branch actions could not be accelerated. A second issue is target-source drift: native `Item.TryGetSelectedEquipment` reads the selected item `cellTip.CellAnchor` path, while GameBridge was primarily reading `DolocAPI.SelectedEquipment`.

5. Animal fondle/petting still sometimes falls back to native speed.

   Analysis: native path is `AnimalRenderer.OnInteract()` -> `_HandleInteract()` -> `DolocAPI.agent._Interact(delegate { animal.Fondle(); ... })`. It is an `AgentStateInteract` body animation path and should be classified separately from harvest instead of relying on incidental scanner state.

6. Electric sprinkler and agricultural grow-light toggles are not accelerated; the "machine add" setting should become "machine interaction".

   Analysis: `Sprinkler` and `FarmLight` share `AffectorElectric.OnInteract()` -> `DolocAPI.agent._Interact(delegate { isTurnOn = !isTurnOn; OnSwitch(); })`. Existing `MachineAdd` classification only recognized `PowerGeneratorFuel` and `Feeder`. The public DTO field remains `MachineAddSpeedEnabled` for compatibility, but the player-facing semantics should be "interaction speed" and include fuel/feed add plus the new switch paths.

7. Follow-up log review found no current resin collector hit, and the user reports resin collector acceleration appears ineffective.

   Analysis: the latest runtime log had `Harvest` evidence for `Toilet` and `ChickenNest`, but no `ResinCollector` / `resin` interaction record, so the previous broad "resin did not regress" manual note is not enough as current proof. Decompiled native owner is `ResinCollector.OnInteract()` -> `BodyController._Interact(...)` -> `Collect(...)`; `ResinCollector` does not override `CanInteractContinues`, so acceleration must happen on the one-shot `AgentStateInteract.OnEnter` body animation, not through `InteractContinues`. GameBridge classification should explicitly recognize ready resin collectors from selected equipment or current interactable, require `currentValue > 0` or `IsGatherable`, log `ResinCollector.OnInteract->BodyController._Interact->Collect`, and prevent empty resin collectors from falling through the generic `IGatherableEquipment` path.

8. Manual retest: ready resin collector acceleration works, empty resin collector intentionally does not accelerate, steel pickaxe/axe/sickle tool animation works, but animal fondle above a room connector still falls back to native speed while the same action above a door is accelerated.

   Analysis: the accepted resin/tool paths prove the shared `AgentStateInteract.OnEnter` and tool animator restore paths are still alive. The remaining animal issue is target-source drift, not a general animator failure: `AnimalRenderer.OnInteract()` is the native owner, but by the time `AgentStateInteract.OnEnter` classifies the body animation, scanner state can be overlapped by room connector equipment/interactable state. The fix should mark `AnimalRenderer.OnInteract` itself as the pending native owner and let the next `AgentStateInteract.OnEnter` consume that short-lived marker before consulting scanner state. The marker must clear on interact exit/save/title/environment reset so it cannot leak into unrelated interactions.

9. Manual retest after the owner-marker patch: the room-connector overlap is still not fixed when the player is standing on the connector, while horned alpaca interaction accelerates.

   Analysis: this narrows the failure from animal type to native interaction timing while overlapping `EquipmentRoom` / room-connector scanner state. Code review found the owner marker was tied to the generic `RestoreActionSpeed()` cleanup path. `AgentStateBase.OnExit` and `AgentStateTool.OnExit` can fire while `BodyController._Interact` is replacing the previous state with `AgentStateInteract`, so those generic exits can erase `AnimalRenderer.OnInteract` ownership before `AgentStateInteract.OnEnter` or `AgentControllerState.InteractContinues` classifies the action. The marker should be a short-lived action context, not part of animator-speed restore state: it must survive generic state replacement, be readable by both OnEnter and continuous timer hooks in the same native interaction, and clear on `AgentStateInteract.OnExit`, save load, return-to-title, environment reset, or expiry.

10. Manual retest after making the animal owner marker survive generic state replacement: animal fondle while standing on the room connector passed.

   Analysis: latest runtime log `D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log` records save slot 9 loading, `Feature.ActionSpeed = ready`, `ActionSpeed.InteractionAnimation = experimental`, and the animal interaction as `nativeOwner=AnimalRenderer.OnInteract->_HandleInteract->BodyController._Interact->Animal.Fondle` with `scanner=TouchIndicator`. The same action restored animator speed on `AgentStateInteract.OnExit`. This confirms the native-owner marker now beats connector scanner overlap and does not leak past the interaction exit.

## Implementation Constraints

- Keep `IActionSpeedApi` Experimental and do not add a stable public API promise.
- Keep fragile native/reflection classification inside `DTMAPI.GameBridge.DolocTown`.
- Do not patch prompt/tip UI animation; classify the native player action before `AgentStateInteract.OnEnter`.
- Prefer the selected item's native `SelectedEquipment` path before global `DolocAPI.SelectedEquipment`.
- Avoid logging every classification miss by default; player logs should remain light.

## Required Verification

- Release build and `DTMAPI.UnitTests`.
- Third-save manual QA for repeated already-fertilized fertilizer, repeated fully covered crop film, low-row seed/fertilizer/film actions, animal fondle above door and above room connector, electric sprinkler switch, and agricultural grow-light switch.
- Regression spot-check for water well, in-water bottle fill, bottled-water right-click, tool, eat/drink, machine fuel/feed add, harvest, ready resin collector collection, empty resin collector no-op, and wild vegetation.
