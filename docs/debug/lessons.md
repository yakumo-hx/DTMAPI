# Lessons Learned

Append durable lessons here when a bug investigation disproves a tempting theory or reveals a recurring trap.

Initial rules:

- Do not diagnose Steam "waiting for game to exit" without first checking whether `DolocTown.exe` still exists.
- Do not treat disabling a functional mod without a clean game restart as proof that its DLL/Harmony patches are unloaded.
- Do not add shutdown or lifecycle patches without recording the risk in the matching issue.

## 2026-05-30: Separate Input, Context, And Overlay Rendering

- Doloc Town uses Unity's new Input System. Do not assume `UnityEngine.Input.GetKeyDown(KeyCode)` is sufficient for manual hotkeys.
- Do not treat `DolocAPI.gameUiStates.HasState<T>()` as "currently active UI"; some states such as `HomePageUiState` can remain cached after save load.
- Use `DolocAPI.IsNormalState` as the first gameplay-context signal when deciding whether gameplay hotkeys should dispatch.
- A mod log line like `ActionSpeed hotkey OpenConfig OK` proves input/event routing, but does not prove the overlay was drawn. Track overlay rendering as a separate verification layer.

## 2026-05-31: Decal Equipment Needs The Official Host Path

- `resin_collector` is decal equipment. Do not smoke-test it with ordinary equipment creation and a null decal host; the game will reject it before any DTMAPI hook can prove behavior.
- Use `EquipmentInfo.FitSlots`, `DM_terrain.GetCachedDecalHosts(slotType)`, the target equipment's `HostFilter`, slot world position, and `IEquipmentHost.CreateEquipment(..., decalHost, decalSlotIndex)` when creating decal equipment for evidence.
- If the current loaded room is an indoor farm room, transition to the main farm before testing tree-hosted decal behavior; the third save can start in `farm_大型集装箱`.

## 2026-05-31: Vegetation Harvest Uses The Native Interactable Target

- Do not classify ActionSpeed harvest behavior from `CurrentInteractableObject` alone. Native vegetation harvest is exposed as a `VegetationRenderer` target through `InteractableManagerEx`/`DolocAPI.agent._Interact`, then unwraps to `Vegetation`.
- Clear stale scanner/current equipment selections before water or vegetation smoke samples. Otherwise logs can misleadingly show a previous `SimpleWell` or `ResinCollector` even while the native branch under test is different.
- If the current farm lacks a usable `DM_vegetation` host, direct transient `Vegetation` plus native `VegetationRenderer`/`IVegetationHost.RenderVegetation` is acceptable for smoke evidence, but the log must record that fallback.

## 2026-05-31: Dandelion Is Not A DungeonResource OneAction Target

- `VegetationDandelion` uses `ToolCollider.HandleTools -> VegetationRenderer.OnFell -> VegetationDandelion.OnFell -> Vegetation.CheckToolConstraints(ItemTool)`, not `DungeonResourceRenderer` or `ResourceFellData`.
- Do not add DTMAPI forced damage for dandelion/vegetation to satisfy one-action behavior. The correct behavior is to let the game reject wrong tools and let the expected tool fell through native `OnFell`, with `oneActionDelta=0`.
- Smoke evidence should record the expected tool from `VegetationInfo.ToolConstraints`; for dandelion in build 23465763 this is `SICKLE`/`old_sickle`, while `old_pickaxe` is a clean wrong-tool negative.
