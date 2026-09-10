# 20260616-0008 ActionSpeed Native Interaction Follow-up

## Status

implemented-manual-smoke-passed

## Area

gamebridge/actionspeed/native-owner/config/docs

## Source Request

User manual QA for the 0616 ActionSpeed bottom-layer rebuild accepted the well, in-water bottle, bottled-water drink, tool/eat/drink/machine-add/harvest/resin/vegetation slices, then clarified that remaining slow behavior happens during the native action for repeated fertilizer/crop-film attempts, not during the failure text animation. The same feedback requested animal fondle and electric sprinkler/grow-light switch acceleration. Follow-up log review found no current `ResinCollector` hit despite a user report that resin collector acceleration appears ineffective, so the ready resin collector path is now classified explicitly instead of relying on the generic harvest fallback. A later manual retest accepted ready/empty resin collector behavior and steel pickaxe/axe/sickle tool animation, but found animal fondle above room connectors still fell back to native speed while the same action above doors accelerated. A second retest found the initial owner-marker patch still failed when the player was standing directly on a room connector, while horned alpaca interaction accelerated; code review traced this to the marker being cleared by generic state-exit restore before the native interact state could classify the action.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/ActionSpeedMod/official-info.json`
- `testmods/ActionSpeedMod/i18n/english.json`
- `testmods/ActionSpeedMod/i18n/schinese.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260616-0002-actionspeed-native-interaction-followup.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Reclassified fertilizer and crop-film as native attempts instead of only successful state changes, so already-fertilized and fully covered failure branches still accelerate the player action that native `_Interact` plays before showing a message.
- Preferred the selected item's native `SelectedEquipment` path before global `DolocAPI.SelectedEquipment`, matching `Item.TryGetSelectedEquipment` more closely and reducing low-row/overlap target drift. A follow-up review rejected broad `SelectedEquipmentSource` fallback for plant items because source anchors are water/source-oriented and can reintroduce target drift.
- Unwrapped `DolocAPI.CurrentInteractableObject` the same way fallback interactable-manager targets are unwrapped.
- Added separate `AnimalInteract` classification for `AnimalRenderer.OnInteract -> _HandleInteract -> Animal.Fondle`.
- Added an `AnimalRenderer.OnInteract` native-owner marker. The marker now behaves as a short-lived action context, not animator restore state: it survives generic `AgentStateBase.OnExit` / `AgentStateTool.OnExit` during `BodyController._Interact` state replacement, can be read by both `AgentStateInteract.OnEnter` and `AgentControllerState.InteractContinues`, and clears on interact exit, save/title/environment reset, or expiry. This keeps animal fondle accelerated even when room connector/equipment scanner state would otherwise hide `AnimalRenderer`.
- Reworked the old machine-add classification into machine interaction semantics while keeping the existing Experimental `MachineAddSpeedEnabled` DTO/config field for compatibility. Fuel/feed add remains covered, and `Sprinkler` / `FarmLight` switch actions are now included through `AffectorElectric.OnInteract`.
- Added an explicit ready-resin-collector harvest owner: `ResinCollector.OnInteract -> BodyController._Interact -> Collect`. Empty resin collectors are excluded from the generic `IGatherableEquipment` fallback so the no-output/confuse path is not logged as a successful harvest acceleration.
- Kept tree-seed ground planting out of the new classifier until its native precondition path is separately proven; current tree support is limited to `PlantBasinTree` fertilizer attempts.
- Updated ActionSpeed player-facing text from "machine add" to "interaction speed" and documented fertilizer/crop-film repeated-attempt semantics.
- Added unit coverage for repeated fertilizer, repeated crop-film, FlowerPot no-op seed attempts, ordinary planted-basin seed skip, tree fertilizer, electric sprinkler and farm-light switches, `Sound` non-match, animal fondle classification including room-connector scanner overlap, native animal marker lifetime across generic state-exit restore, and ready/empty resin collector classification.
- Added the ActionSpeed Workshop/local description note `update 0616 / 提高稳定性` with Simplified Chinese, Traditional Chinese, and English localized text.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`, including the new `ActionSpeedNativeClassificationCoversAttempts` coverage for repeated plant attempts, animal/machine interactions, room-connector animal scanner overlap, marker lifetime across generic state-exit restore, and ready/empty resin collectors.
- Passed: `git diff --check`
  - Exit code 0; only existing LF-to-CRLF working-copy warnings were printed for touched files.
- Passed: `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -InstallPublishedModsOnly`
  - Refreshed DTMAPI runtime under `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI`.
  - Refreshed official-local ActionSpeed package under `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\Yuuka_DTMAPI_ActionSpeed`.
- Passed: `tools/scripts/check-dtmapi-status.ps1`
  - Required install files were `[OK]`.
  - Installed DTMAPI version reported `0.5.2-alpha`.
  - Official-local `Yuuka_DTMAPI_ActionSpeed` reported `Yuuka.DTMAPI.ActionSpeed 1.3.4-dtmapi`.
- Passed: source/install hash spot-check
  - `DTMAPI.GameBridge.DolocTown.dll` source Release hash matched installed game plugin hash after the animal owner-marker lifetime follow-up: `5E511D2AE856BFE02478637151A01BE0D94C7A03BCBA931BBAD367BAC0D6C230`.
  - `ActionSpeedMod.dll` source Release hash matched official-local upload package `Content/DTMAPI/Yuuka.DTMAPI.ActionSpeed.dll`: `3169DB47BA7686B124F2BC907FED89605DF5B1BD17703C33566A147DD8D7B13A`.
- Passed: latest manual smoke follow-up captured in the runtime log
  - User accepted water well, in-water bottle, bottled-water drink, tool/eat/drink/machine interaction/harvest/vegetation, ready resin collector, steel pickaxe/axe/sickle, and room-connector animal fondle behavior.
  - Latest runtime log `D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log` records `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `ActionSpeed SaveLoaded restore boundary OK slot=9`, and animal fondle above scanner overlap as `nativeOwner=AnimalRenderer.OnInteract->_HandleInteract->BodyController._Interact->Animal.Fondle`, `scanner=TouchIndicator`, `animators=1`, followed by `ActionSpeed animator speeds restored reason=AgentStateInteract.OnExit restored=1`.

## Evidence

- Code-level native owners:
  - `ItemFertilizer.Fertilizer()` and `ItemFilm.Protect()` call `_Interact` before success/failure callbacks.
  - `AnimalRenderer.OnInteract()` is the native owner for animal fondle and calls `_HandleInteract()`, which calls `_Interact` before `Animal.Fondle()`.
  - `Sprinkler` and `FarmLight` inherit `AffectorElectric.OnInteract()`, which calls `_Interact` before `OnSwitch()`.
  - `ResinCollector.OnInteract()` calls `_Interact` before `Collect(...)`, and the native type does not use the continuous interact path.
- Manual feedback review: `docs/reviews/manual-qa/2026/20260616-0002-actionspeed-native-interaction-followup.md`.
- Runtime manual log: `D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log` at 2026-06-17 00:07:38 +08:00 records the connector-overlap animal fondle pass.

## Rollback Notes

Rollback should be scoped to `ActionSpeedService` classification changes and ActionSpeed config text. Do not revert the earlier `InteractContinues` hook rebuild unless the hook itself proves unstable.

## Follow-Up

- Empty resin collectors remain intentionally unaccelerated unless native ready-state ownership is proven for a collected output.
