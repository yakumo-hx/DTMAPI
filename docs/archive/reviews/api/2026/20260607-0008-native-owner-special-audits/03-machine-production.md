# 03 - IMachineProductionApi Special Audit

## 1. Scope

API under review: `DTMAPI.Abstractions.IMachineProductionApi`, especially `RegisterMachine`, `GetMachines`, `GetState`, and `GetStatus`, plus the MineMod consumer that exercises it.

Questions answered:

- Which official tables/json/tech/recipe paths are changed when registering a machine.
- Whether production runs through native machine logic or a DTMAPI sidecar loop.
- Which paths handle electric power, output storage, cross-room observation, sleep/pass-time, Y-console time skips, and scene refresh.
- Why Mine can still have preview scale, texture contamination, and production/electric instability risks.

Result: `Partial/Gap`. Some table, electric, inventory, and visual slices are native-backed, but production scheduling and fuel/output state remain a DTMAPI runtime loop.

## 2. Files read

- `docs/api/public-api-matrix.md:85`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:64`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md:362`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/07-completion-audit.md:83`
- `docs/updates/2026/20260605-0006-027-manual-qa-root-cause.md:37`
- `docs/updates/2026/20260606-0004-029-readme-implementation.md:23`
- `docs/updates/2026/20260606-0007-031-regression-new-content-round.md:26`
- `docs/updates/2026/20260607-0007-native-responsibility-code-review.md:69`
- `docs/hook-map/README.md:644`
- `docs/hook-map/README.md:661`
- `docs/debug/INDEX.md:25`
- `docs/debug/INDEX.md:27`
- `docs/debug/INDEX.md:29`
- `docs/debug/regressions/smoke-matrix.md:23`
- `docs/reviews/manual-qa/2026/20260605-0001-mine-animal-yconsole-code-review.md`
- `docs/reviews/manual-qa/2026/20260605-0002-mine-time-skip-production-review.md`
- `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/MineMod/ModEntry.cs`
- `testmods/MineMod/Content/equipment_tbequipment.json`
- `testmods/MineMod/Content/item_tbitem.json`
- `testmods/MineMod/Content/recipe_tbrecipe.json`
- `testmods/MineMod/Content/mod_tbmodrecipegroupextension.json`
- Reverse candidate search under `references/doloc-town/reverse/builds/` for equipment, recipe, tech-tree, electric component, inventory, room, pass-time, and machine-like owners.

## 3. Functions read

- `DTMAPI.Abstractions.IMachineProductionApi.RegisterMachine(...)`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:133`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.RegisterMachine(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568`
- `EnsureNativeMachineRecipeInputs(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:620`
- `EnsureNativeMachineTechRoute(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:702`
- `InjectNativeTechGraphNode(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:765`
- `EnsureNativeTechNodeInfo(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:871`
- `EnsureNativeMachineTechGraphNodePayload(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:897`
- `GetMachines(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1174`
- `IMachineProductionApi.GetState(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1181`
- `IMachineProductionApi.GetStatus(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1194`
- `UpdateRuntimeAutomation(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6253`
- `UpdateMachineProduction(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6977`
- `TryApplyMachineVisualScale(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7151`
- `ApplyMineBuilderPreviewScale(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7223`
- `TryRunMachineProductionCycle(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7365`
- `TryConsumeMachineElectricPower(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7417`
- `TryPlaceMachineOutput(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7450`
- `BuildMachineRuntimeKey(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7693`
- `EnumerateMachineCandidateRooms(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7717`
- `NormalizeMachineDefinition(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8108`
- `InvokeNativePassTime(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:11111`
- `DolocTownGameBridge.Update()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:290`
- Machine visual hook installation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:1188`
- Machine smoke exercise: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:4661`
- `MineMod.ModEntry.OnSaveLoaded(...)`: `testmods/MineMod/ModEntry.cs:60`
- `MineMod.ModEntry.BuildMachineDefinition()`: `testmods/MineMod/ModEntry.cs:68`
- `MineMod.ModEntry.BuildOutputRules()`: `testmods/MineMod/ModEntry.cs:100`
- `MineMod.ModEntry.BuildRecipeInputs()`: `testmods/MineMod/ModEntry.cs:126`

## 4. Call graph

Registration/table path:

`MineMod.OnSaveLoaded -> IMachineProductionApi.RegisterMachine -> NormalizeMachineDefinition -> machineDefinitions[...] -> EnsureNativeMachineTechRoute -> DolocAPI.assets.techTrees / TbTechNode -> EnsureNativeMachineRecipeInputs -> DolocConfig.Tables.TbRecipe.InputItems -> MachineProductionState`

Runtime production path:

`DolocTownGameBridge.Update -> DolocTownExperimentalBridgeApi.UpdateRuntimeAutomation -> UpdateMachineProduction -> EnumerateMachineCandidateRooms/Equipments -> machineRuntimeEntries -> TryRunMachineProductionCycle -> TryConsumeMachineElectricPower -> TryPlaceMachineOutput -> MachineProductionState`

Native slices inside the loop:

`TryConsumeMachineElectricPower -> equipment.IElectronicComponent.Launch()`
`TryPlaceMachineOutput -> equipment.inventory / ContentFilter / LinearInventory.PlaceItemAt / TryGenerateNativeItem`

Time skip observation:

`Y-console/debug time API -> SkipToNextWeatherPeriod or AdvanceTime -> InvokeNativePassTime / PassTimeNoControl -> later UpdateMachineProduction observes total TUs and catches up if a runtime entry exists`

Visual path:

`DolocTownGameBridge hook install -> EquipmentRenderer.OnReuse / EquipmentBuilder.CreateIndicator / TurnIndicator -> ResetEquipmentRendererScaleOnReuse / ApplyMineBuilderPreviewScale`
`UpdateMachineProduction -> TryApplyMachineVisualScale` for placed machines.

## 5. Function body findings

- `RegisterMachine` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568` normalizes and validates a DTMAPI `MachineDefinition`, stores it in `machineDefinitions`, runs tech/recipe table mutations, creates/updates `MachineProductionState`, and reports an experimental runtime-loop status.
- `EnsureNativeMachineRecipeInputs` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:620` writes recipe input arrays into native `DolocConfig.Tables.TbRecipe` entries. This is a native table slice, not a production scheduler.
- `EnsureNativeMachineTechRoute` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:702` injects or updates a tech-tree route under native tech graph assets and `TbTechNode`.
- `EnsureNativeMachineTechGraphNodePayload` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:897` sets payload entries such as recipes/costs; it does not install an equipment behavior class.
- `IMachineProductionApi.GetState` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1181` returns the stored DTMAPI `MachineProductionState` object, or a `not-configured` object. It does not query a native machine runtime.
- `IMachineProductionApi.GetStatus` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1194` reports `configured-experimental-runtime-loop` when the DTMAPI loop has been installed. The status text itself states that DTMAPI observes placed equipment and native fuel/electric UI remains experimental.
- `UpdateRuntimeAutomation` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6253` is a DTMAPI update-loop dispatcher. `UpdateMachineProduction` is called from there, not from a native machine production callback.
- `UpdateMachineProduction` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6977` polls archive/current room data, enumerates candidate equipment, creates in-memory runtime entries, applies visual scale, and runs bounded catch-up cycles when total TUs advance.
- `TryRunMachineProductionCycle` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7365` uses DTMAPI definition fields for mode, fuel cost, electric cost, output selection, due time, and production count. This is the sidecar production owner.
- `TryConsumeMachineElectricPower` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7417` does reach a native electric component via `Launch()` and retains due time on low power/failure.
- `TryPlaceMachineOutput` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7450` writes output to native equipment storage through inventory APIs when available. For `dtmapi_mine`, it disables backpack fallback if storage is missing.
- `BuildMachineRuntimeKey` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7693` includes owner, machine, equipment id, room id, and equipment index/hash. This confirms production state is keyed by DTMAPI observation, not by a native persistent machine record.
- `EnumerateMachineCandidateRooms` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7717` checks current/root/farm/archive room candidates. It is broader than the old current-room-only path but still not a native all-save-room production owner.
- `InvokeNativePassTime` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:11111` calls native pass-time, but the machine loop is not hooked as the native pass-time owner. It observes time after the fact.
- `MineMod.BuildMachineDefinition` at `testmods/MineMod/ModEntry.cs:68` sets `VisualScale=2`, fuel/electric mode fields, recipe inputs, output rules, tech route, and cycle minutes. This ordinary-looking mod definition is actually relying on DTMAPI's experimental sidecar loop.
- Mine content JSON provides native content slices: `equipment_tbequipment.json:24` uses `EquipmentFuncCase` storage, `:41` uses `EComProtoAppliance`; `item_tbitem.json:29` uses `ItemFunctionEquipment`; `recipe_tbrecipe.json:3` defines `dtmapi_mine`; `mod_tbmodrecipegroupextension.json:3` adds it to `equipment_workbench`.

## 6. Native owner verdict

Verdict: `Partial` for table/electric/storage/visual slices, `Gap` for production ownership.

Reached owners:

- Native recipe table input slice: `DolocConfig.Tables.TbRecipe`.
- Native tech tree/table slice: `DolocAPI.assets.techTrees` and `TbTechNode`.
- Native electric component slice: `IElectronicComponent.Launch()`.
- Native storage slice: equipment inventory / `LinearInventory.PlaceItemAt`.
- Native preview/render hook slice: `EquipmentRenderer.OnReuse`, `EquipmentBuilder.CreateIndicator`, `EquipmentBuilder.TurnIndicator`.

Not reached:

- Native machine production scheduler.
- Native machine fuel-state persistence.
- Native all-room/sleep/offline production owner.
- Native machine save/load restore owner.
- Native visual ownership for every preview/render child and room transition.

Excluded candidates:

- Official table writes are not enough to prove native production because `UpdateMachineProduction` owns cycle due time and output selection.
- Native pass-time calls are not enough to prove native production because DTMAPI only observes total time after the native time skip.
- Native electric `Launch()` is not enough to prove stable machine semantics because fuel and output scheduling remain DTMAPI state.

## 7. Ordinary mod usability

Ordinary mod usability: `仅 DTMAPI 自家 mod 可用`.

External ordinary mods should not treat `IMachineProductionApi` as a stable machine-production contract. It can support DTMAPI's MineMod under controlled smoke settings, but its runtime semantics are still DTMAPI sidecar state plus selected native slices.

## 8. Concrete failure modes

1. Cross-room production miss: if a placed machine is outside the candidate rooms enumerated at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7717`, the DTMAPI loop may not observe or advance it.
2. Save/load or title boundary state loss: runtime entries are in-memory sidecar state; fuel remaining, due time, and last output are not proven to be native persisted machine state.
3. Sleep or Y-console time skip drift: native `PassTimeNoControl` advances time, but DTMAPI catch-up runs later through update polling and can miss machines not already observed or not in candidate rooms.
4. Low-power/storage-full stall: `TryConsumeMachineElectricPower` and `TryPlaceMachineOutput` retain due time on failure, so a mod may see configured status while production never advances.
5. Shared table pollution: recipe/tech graph mutations are global native table writes keyed by ids. A bad ordinary mod can collide with official or other mod ids and alter shared unlock/crafting behavior.
6. Preview/texture contamination: visual scale is applied by runtime observation and specific builder/renderer hooks. If a render child or reuse path is missed, a 2x Mine scale can leak to non-Mine preview/render state or fail to enlarge the preview.
7. Backpack pollution for non-Mine machines: `TryPlaceMachineOutput` supports a backpack fallback outside the strict Mine case, so an ordinary mod can accidentally turn a machine output contract into player-inventory insertion.

## 9. Minimal rebuild direction

- Split the API into two contracts:
  - Stable native content/table bridge: ids, JSON/content availability, recipe input mutation, tech route mutation, and capability/status reporting.
  - Experimental runtime production adapter: explicit DTMAPI sidecar loop with no promise of native scheduling/persistence.
- Build a native machine owner map before stable runtime:
  - Equipment behavior/update owner for per-machine tick/cycle.
  - Native pass-time/sleep catch-up owner.
  - Native save/load state owner for due time and fuel.
  - Native storage/output owner with no backpack fallback unless explicitly requested.
  - Native electric/fuel owner with one source of truth.
  - Native preview/render owner that scopes scale to the Mine object and restores pooled renderers.
- Add tests that prove placed machines in current room, farm root, container/inside rooms, sleep skip, Y-console skip, save/load, and title return all produce the same state.

## 10. Evidence gaps

- Missing reverse proof of a native generic machine scheduler suitable for modded machines.
- Missing evidence that every save room/container room topology is enumerated by `EnumerateMachineCandidateRooms`.
- Missing persistence evidence for `machineRuntimeEntries` across save/load/title; current evidence proves smoke catch-up, not durable native state.
- Missing screenshot or pixel evidence for Mine preview scale across all builder states after the later 0.4.1 manual finding.
- Missing ordinary third-party mod evidence; all current success evidence is DTMAPI-owned MineMod smoke.
