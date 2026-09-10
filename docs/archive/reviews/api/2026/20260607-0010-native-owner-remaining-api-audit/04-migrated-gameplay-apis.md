# 04 - Migrated Gameplay APIs

## Scope

Audited migrated gameplay APIs that were not part of the 0008/0009 special passes:

- `IActionCompletionApi.Configure/GetStatus`
- `IActionSpeedApi.Configure/GetStatus`
- `IFishingAutomationApi.Configure/SetEnabled/GetState/GetStatus`
- `IItemTooltipApi.ConfigureFishRoeProvider/GetStatus`
- `IAnimalViewerApi.ConfigureSpecialProduceProgress/GetStatus`

These APIs were built for migrated DTMAPI official-local mods and have smoke evidence. The review question is whether they are ordinary-mod stable public APIs or still restricted DTMAPI-owned adapters.

## Files read

- `docs/api/public-api-matrix.md`: lines 69-73.
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`: lines 6-41 and 1034-1071.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`: lines 922-1135.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`: lines 107-135 and 211-254.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`: lines 408-566, 5465-5665, 5790-6129, 6136-6238, 7949-8066, 8210-8331, 8333-8583, and 8624-8766.
- `testmods/OneActionCompleteMod/ModEntry.cs`: lines 17-84.
- `testmods/ActionSpeedMod/ModEntry.cs`: lines 17-140.
- `testmods/AutoFishingMod/ModEntry.cs`: lines 20-140.
- `testmods/FishBreedingAssistantMod/ModEntry.cs`: lines 17-100.
- `testmods/AnimalHusbandryProgressMod/ModEntry.cs`: lines 17-88.
- `docs/hook-map/README.md`: lines 426-512 and 610-622.
- `docs/debug/regressions/smoke-matrix.md`: lines 72-79.
- `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`: lines 21-36.
- `references/doloc-town/research-notes/README-DolocTown-Modding-API.md`: resource completion and fishing native-owner notes.
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`: Fishing, chest, farming, weather, monster/resource native-owner map references.
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`: fishing minigame, chest, and farming native-owner notes.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Action_Interaction.md`: `AgentControllerState` interaction candidates.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Fishing.md`: `FishingGameScrollBar` and fishing state candidates.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Resource_Gathering.md`: resource gathering candidate owners.

## Functions read

- `DTMAPI.Abstractions.IActionCompletionApi.Configure`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:8`.
- `DTMAPI.Abstractions.IFishingAutomationApi.Configure`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:15`.
- `DTMAPI.Abstractions.IFishingAutomationApi.SetEnabled`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:16`.
- `DTMAPI.Abstractions.IActionSpeedApi.Configure`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:24`.
- `DTMAPI.Abstractions.IItemTooltipApi.ConfigureFishRoeProvider`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:31`.
- `DTMAPI.Abstractions.IAnimalViewerApi.ConfigureSpecialProduceProgress`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:38`.
- `DolocTownExperimentalBridgeApi.IActionCompletionApi.Configure`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408`.
- `DolocTownExperimentalBridgeApi.IActionSpeedApi.Configure`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:423`.
- `DolocTownExperimentalBridgeApi.IFishingAutomationApi.Configure`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:466`.
- `DolocTownExperimentalBridgeApi.IFishingAutomationApi.SetEnabled`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:476`.
- `DolocTownExperimentalBridgeApi.IItemTooltipApi.ConfigureFishRoeProvider`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:536`.
- `DolocTownExperimentalBridgeApi.IAnimalViewerApi.ConfigureSpecialProduceProgress`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:553`.
- `DolocTownExperimentalBridgeApi.ApplyOneActionToolHit`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5790`.
- `DolocTownExperimentalBridgeApi.TryBuildValidatedResourceFellData`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6063`.
- `DolocTownExperimentalBridgeApi.CostOneActionExtraToolHits`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6095`.
- `DolocTownExperimentalBridgeApi.ApplyOneActionEquipmentFillAfterInteract`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5941`.
- `DolocTownExperimentalBridgeApi.ApplyActionSpeedToolEnter`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6136`.
- `DolocTownExperimentalBridgeApi.ApplyActionSpeedInteractEnter`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6182`.
- `DolocTownExperimentalBridgeApi.ApplyActionSpeedEatEnter`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6197`.
- `DolocTownExperimentalBridgeApi.AdjustActionSpeedUseItemContinuesDelta`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6212`.
- `DolocTownExperimentalBridgeApi.NotifyFishingPhase`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7949`.
- `DolocTownExperimentalBridgeApi.ApplyFishingWaitAutomation`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7991`.
- `DolocTownExperimentalBridgeApi.TryAdvanceFishingBite`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8210`.
- `DolocTownExperimentalBridgeApi.TryApplyFishingMiniGameAutomation`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8278`.
- `DolocTownExperimentalBridgeApi.DecorateFishRoeTitle`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5465`.
- `DolocTownExperimentalBridgeApi.DecorateFishRoeDetail`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5491`.
- `DolocTownExperimentalBridgeApi.DecorateAnimalFullInfoData`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5525`.
- `DolocTownExperimentalBridgeApi.RenderAnimalProgressOverlay`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5578`.

## Call graph

```text
ActionCompletion
  OneActionCompleteMod.ConfigureBridge
    IActionCompletionApi.Configure
      stores owner policy
  Harmony: ToolCollider.HandleTools
    ApplyOneActionToolHit
      native ResourceFellData validation
      native DolocAPI.HasEnoughEnergyForUsingTool / CostToolEnergy
      native resource _Fell path
  Harmony: AgentStateInteract.OnExit
    ApplyOneActionEquipmentFillAfterInteract
      PowerGeneratorFuel / Feeder native IsSuitable/AddFuel/AddFeeds
      native item CostSelf

ActionSpeed
  ActionSpeedMod.ConfigureBridge
    IActionSpeedApi.Configure
      normalized policy dictionary
  Harmony: AgentStateTool/Interact/Eat/UseItemContinues
    ApplyActionSpeed* / AdjustActionSpeedUseItemContinuesDelta
      scales native body/tool/collider animator speeds
      DTMAPI sidecar remembers originals and classifications

FishingAutomation
  AutoFishingMod.ConfigureBridge + F6 ButtonPressed
    IFishingAutomationApi.Configure/SetEnabled
      DTMAPI state owner
  Harmony: fishing ready/cast/wait/pull/minigame
    NotifyFishingPhase / ApplyFishingWaitAutomation / TryAdvanceFishingBite / TryApplyFishingMiniGameAutomation
      native RollFish
      native DolocAPI.CostEnergy
      native StateManager.Overwrite
      native FishingGameScrollBar currentGameStatus

Fish roe tooltip
  FishBreedingAssistantMod.ConfigureBridge
    IItemTooltipApi.ConfigureFishRoeProvider
  Harmony: Item title/description/detail
    DecorateFishRoeTitle/Detail
      provider lookup
      display-string decoration only

Animal viewer
  AnimalHusbandryProgressMod.ConfigureBridge
    IAnimalViewerApi.ConfigureSpecialProduceProgress
  Harmony: AnimalFullInfoData / AnimalViewer.Show / AnimalPanel.RefreshViewer
    DecorateAnimalFullInfoData
    PrepareAnimalProgressOverlayBeforeShow
    RenderAnimalProgressOverlay
      cloned progress rows beside native viewer UI
```

## Function body findings

- `IActionCompletionApi.Configure` stores per-owner policy and returns status. It does not itself touch native resources; native work happens only when the Harmony hook later sees matching resource/fuel/feed contexts (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408-420`).
- `ApplyOneActionToolHit` reads the current dungeon resource, selected tool, hit point, native damage/tool metadata, then builds native `ResourceFellData` and invokes the native fell path for extra damage after charging native per-hit energy (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5790-5877`).
- `TryBuildValidatedResourceFellData` rejects tool/resource mismatches by checking native `ResourceFellData.Valid` and level-match data before DTMAPI applies extra completion damage (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6063-6093`).
- Fuel/feed completion is narrower than the API name suggests: it runs after native interaction and only for recognized `PowerGeneratorFuel` or `Feeder` owners, consuming selected items through native item methods (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5941-6061`).
- `IActionSpeedApi.Configure` stores normalized policy, then hook callbacks mutate animator speed/timer deltas only while matching classifications are active (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:423-464`, `6136-6235`).
- ActionSpeed owner tracking is DTMAPI sidecar state: original animator speeds and current classifications are remembered by GameBridge, not by a native modifiable policy object (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6136-6238`, `8424-8583`).
- `IFishingAutomationApi.SetEnabled` toggles DTMAPI state and optionally emits native small-message feedback. It does not create a native fishing job or queue (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:476-513`).
- Fishing wait automation calls native `RollFish`, writes wait-state fields, charges energy via `DolocAPI.CostEnergy`, chooses native fishing battle or pull states, and can set `FishingGameScrollBar.currentGameStatus=Success` after visible-time gating (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7991-8066`, `8210-8331`).
- Fish roe tooltip configuration is display-only: title/detail decorators call the provider and append/return strings while isolating provider exceptions into diagnostics (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5465-5523`, `8333-8350`).
- Animal viewer configuration is display-only: it builds progress rows from configured definitions and renders cloned progress bars; it does not mutate animal data, produce timers, food state, breeding, or save records (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5525-5665`).

## Native owner verdict

| API | Verdict | Native owner reached |
| --- | --- | --- |
| `IActionCompletionApi` | Partial | Resource hit, energy cost, native fell, fuel/feed add owners are reached for specific contexts. General action completion owner is not reached. |
| `IActionSpeedApi` | Partial | Native agent states and animator/timer paths are reached; policy ownership, conflict arbitration, and restore state are DTMAPI sidecar. |
| `IFishingAutomationApi` | Partial | Fishing wait, state overwrite, energy, and minigame owners are reached; automation state and safety policy are DTMAPI-owned. |
| `IItemTooltipApi` | Reached/display-only | Native item string hooks are reached. No item/economy state owner is needed or exposed. |
| `IAnimalViewerApi` | Reached/display-only | Native viewer/data/render hooks are reached. No animal runtime/save owner is modified. |

## Ordinary mod usability

- `IActionCompletionApi`: 仅 DTMAPI 自家 mod 可用; keep `experimental`.
- `IActionSpeedApi`: 仅 DTMAPI 自家 mod 可用; keep `experimental`.
- `IFishingAutomationApi`: 仅 DTMAPI 自家 mod 可用; keep `experimental`.
- `IItemTooltipApi`: 普通 mod 可用 as display-only provider; keep `experimental` until provider arbitration is documented.
- `IAnimalViewerApi`: 普通 mod 可用 as display-only provider with caution; keep `experimental` because UI clone/layout stability is still hook-dependent.

## Concrete failure modes

1. Ordinary mods using `IActionCompletionApi` as a general "finish any action" API can miss native owners for non-resource/non-fuel/non-feed actions, causing partial completion, wrong energy accounting, or drops/lifecycle callbacks not matching native expectations.
2. Multiple ordinary mods configuring `IActionSpeedApi` can fight over global animator speed and restore sidecar state; one mod can leave another's speed multiplier stale or restore to the wrong baseline.
3. Fishing automation can force native fishing states and minigame success in contexts designed for one official migrated mod. A general mod depending on it can bypass progression/economy timing and produce confusing player-facing automation outside intended hotkey/state gates.
4. Fish roe tooltip providers can mislead users if documented as item-data mutation; the current API only changes displayed strings and does not change incubation/growth data.
5. Animal viewer progress rows can drift after native UI changes because DTMAPI clones/probes existing viewer widgets instead of owning a stable native animal info extension point.

## Minimal rebuild direction

- Keep ActionCompletion, ActionSpeed, and FishingAutomation as DTMAPI migrated-mod adapters, not broadly documented public gameplay APIs.
- For stable action completion, split resource-fell, vegetation, fuel, feed, crop harvest, resin, and other interaction owners into separate explicit contracts with native validation and event ordering.
- For stable action speed, introduce scoped, stackable modifiers with owner tokens and automatic restore on state exit/save/title boundaries.
- For stable fishing, expose read-only fishing phase events first, then build opt-in automation verbs with energy, loot, minigame, and cancellation owners documented separately.
- Keep Tooltip and AnimalViewer display APIs display-only, with provider exception isolation and UI layout version notes.

## Evidence gaps

- No native "general action completion" or "global action speed policy" owner was found; the read functions are hook slices around selected states.
- No multi-mod conflict test was run for ActionSpeed/Fishing/Tooltip/AnimalViewer provider arbitration.
- Reverse maps confirmed the candidate owners used in this review, such as `FishingGameScrollBar`, `AgentControllerState`, and `ResourceFellData`, but did not identify a broader stable public owner for arbitrary action completion or action-speed policy.
- No game smoke was run in this pass. Existing evidence comes from public matrix lines 69-73, hook-map gameplay sections, smoke-matrix rows 72-79, and update record `20260606-0007`.
