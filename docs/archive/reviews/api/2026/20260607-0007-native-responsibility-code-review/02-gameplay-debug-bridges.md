# 02 gameplay and debug bridges

Date: 2026-06-07
Status: manual code review slice complete for this volume

This volume reviews migrated gameplay APIs and Y-console/debug APIs. Many methods here do reach Doloc Town native owners. The main review question is therefore not only "does it call native code?", but "is the native path safe for ordinary mods, or is it a debug/migrated-mod shortcut?"

## Top Risks

1. `IAdvancedDebugApi` mixes native commands, direct save-field edits, room-host spawns, time scale, and creative singleton flags. It must remain `debug-only`; ordinary mods would pollute saves, skip progression, spawn room residue, or leak global creative/no-cost flags.
2. `IInstantSaveDebugApi.Save` calls the native save path, but the reload variant is explicitly disabled because running-save reload can leave scene residue. Ordinary mods must not treat it as a general save/reload lifecycle API.
3. `IActionCompletionApi`, `IActionSpeedApi`, and `IFishingAutomationApi` are smoked migrated-mod bridges with real hooks, but they depend on specific AgentState/ToolCollider/Fishing state paths and DTMAPI owner arbitration. Ordinary mods can desync animation, energy, wrong-tool, or fishing phase behavior outside the tested policies.
4. `IInventoryDebugApi.GiveItem` and `IMailDeliveryApi.SendItemMail` use real native inventory/mail APIs, but they are still debug/economy mutation paths. Ordinary mods depending on them can bypass progression or duplicate content-source assumptions.
5. `ITeleportDebugApi.Teleport`, `IWeatherDebugApi.SetWeather`, `ITimeDebugApi.SkipToNextWeatherPeriod`, and `IMovementDebugApi.SetSpeedMultiplier` all mutate global game state. They should remain debug-only even when native owner credibility is good.
6. `IItemTooltipApi` and `IAnimalViewerApi` are display-only. DTO/option wording must avoid implying they create fish roe data, hidden-produce data, or native animal stats.
7. `IChestLocatorEnhancerApi` and `IStrongPlantingGunApi` are narrow DTMAPI mod bridges. They patch official paths but still append/mediate behavior with DTMAPI policy.

## Native Owner Map

| API group | Current native owner reached | Current DTMAPI owner | Verdict |
| --- | --- | --- | --- |
| Action completion | `ToolCollider.HandleTools`, resource fell/energy/cost paths, fuel/feed interact hooks | DTMAPI action policy dictionaries | Native hooks reached, migrated-mod scope |
| Action speed | `AgentStateTool`, `AgentStateInteract`, `AgentStateEat`, `AgentControllerState.UseItemContinues` body speed | DTMAPI speed policy dictionaries | Native hooks reached, owner arbitration risk |
| Fishing automation | Fishing AgentState and minigame hooks, BodyController rod use | DTMAPI fishing state | Native hooks reached, phase automation risk |
| Fish roe tooltip | Item title/description/detail postfixes | DTMAPI lookup providers | Display-only native UI hook |
| Animal viewer progress | `AnimalFullInfoData`, `AnimalViewer.Show`, `AnimalPanel.RefreshViewer` | DTMAPI rendering policy | Display-only native UI hook |
| Inventory give | `DolocAPI.QueryItemProto`, `CanPlaceItem`, `TryPlaceInBackpack`, `CountItem` | DTMAPI debug UI source filtering | Native inventory owner reached |
| Mail delivery | `DolocAPI.SendItemAsEmail`, `EmailManager.emails`, item proto/query | DTMAPI request/source guard | Native mail owner reached |
| Weather | `ArchiveDataHandle.SetWeather`, `PatchWeather`, `TbWeather` | DTMAPI debug UI | Native weather owner reached |
| Teleport | `DolocAPI.DoTransport` with whitelisted mark points | DTMAPI destination whitelist/export | Native transport owner reached |
| Instant save | `DolocAPI.SaveGame` | DTMAPI debug guard | Native save owner reached for save-only |
| Time skip | `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp` | DTMAPI target-period calculation | Native time owner reached |
| Movement speed | `MotionAbility.SetMoveScaler` | DTMAPI last multiplier | Native movement owner reached |
| Advanced debug | Mixed: `DolocAPI` commands, archive fields, room hosts, `GameInitConfig` flags, plant crop debug method | DTMAPI whitelist/state | Debug-only, mixed safety |
| Chest locator | `ArchiveDataHandle.GetAvailableInventories`, native CountItem/CostItem shared inventory array | DTMAPI append policy | Narrow experimental bridge |
| Strong planting gun | `ItemFarmingGun`, `FarmingGunUiState`, official basin/tool checks | DTMAPI multi-slot policy | Narrow experimental bridge |

## Ordinary Mod Usability Table

| API group | Usability | Why |
| --- | --- | --- |
| ActionCompletion / ActionSpeed / FishingAutomation | 仅 DTMAPI 自家 mod 可用 | Real hooks exist, but policy arbitration and state-machine assumptions are tuned to shipped migrated mods. |
| ItemTooltip / AnimalViewer display APIs | 普通 mod 可用 with caution | Display-only hooks are less dangerous, but must not imply native data creation. |
| Inventory/Mail/Weather/Teleport/InstantSave/Time/Movement debug APIs | debug-only | They mutate game economy/world/save/time/player state through native functions. |
| AdvancedDebug | debug-only | Explicitly dangerous, whitelisted commands only. |
| ChestLocatorEnhancer / StrongPlantingGun | 仅 DTMAPI 自家 mod 可用 | Narrow mod-specific bridges with append/cloned/policy behavior. |

## Review Blocks

### `DTMAPI.Abstractions.IActionCompletionApi.Configure(...)`

- Symbol: `DTMAPI.Abstractions.IActionCompletionApi.Configure(IManifest owner, ActionCompletionOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental / MoveToDTMAPI-owned mod API
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:6`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408`
- Function body summary: Stores an owner policy in `actionOptions`. Native behavior happens later through hook callbacks that inspect policy and apply extra tool/resource/fuel/feed completion work.
- GameBridge/Harmony/reflection path: `ToolCollider.HandleTools` prefix/postfix, `AgentStateInteract.OnExit`, energy/cost checks, and resource/equipment helper methods such as `ApplyOneActionToolHit`, `TryApplyOneActionFuelFill`, and `TryApplyOneActionFeederFill`.
- Native owner: Tool/resource/fuel/feed owners are reached in specific paths, but DTMAPI controls policy and extra iteration.
- Native state holder: Native resource/equipment/inventory state plus DTMAPI policy dictionaries and last-summary fields.
- DTMAPI registry/status/UI-only vs native runtime: Native hooks reached for smoked paths; not a general action engine.
- Evidence:
  - public-api-matrix entry: ActionCompletion experimental API.
  - update record: action-speed/one-action smoke records under 0.2.8/0.3.1 updates.
  - smoke path: `Smoke.OneActionResourceHit`, `Smoke.OneActionWrongTool`, `Smoke.OneActionFuelFeed`, `Smoke.OneActionVegetation` rows in `docs/debug/regressions/smoke-matrix.md`
  - hook-map entry: action completion / tool collider entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5790`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5941`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:93`
- Result: Watch
- Recommendation: Keep experimental and scoped. Ordinary mods should not use it as a stable action-completion framework because wrong-tool, energy cost, vegetation exception, fuel/feed consumption, and resource lifecycle behavior are only proven for configured DTMAPI policies. A normal mod depending on it could complete the wrong object, skip intended native costs, or create resource-state desync outside the smoked cases.

### `DTMAPI.Abstractions.IActionSpeedApi.Configure(...)`

- Symbol: `DTMAPI.Abstractions.IActionSpeedApi.Configure(IManifest owner, ActionSpeedOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental / owner-arbitrated
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:22`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:423`
- Function body summary: Normalizes and stores owner action-speed options. Later hook callbacks classify the current action and apply a multiplier to the native body/state animation controller.
- GameBridge/Harmony/reflection path: `AgentStateTool.OnEnter/OnExit`, `AgentStateInteract.OnEnter`, `AgentStateEat.OnEnter`, and `AgentControllerState.UseItemContinues` prefix. `ApplyActionSpeedToBody` writes body speed and records hook status.
- Native owner: Agent state body/animation speed objects are reached; policy selection is DTMAPI-owned.
- Native state holder: Native action state body plus DTMAPI `actionSpeedOptions`.
- DTMAPI registry/status/UI-only vs native runtime: Native state mutation for specific AgentState paths.
- Evidence:
  - public-api-matrix entry: ActionSpeed API.
  - update record: `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`
  - smoke path: `Smoke.ActionSpeedTool`, `Smoke.ActionSpeedConfigApply`, `Smoke.ActionSpeedInteraction`, `Smoke.ActionSpeedContinuousUse`, `Smoke.ActionSpeedAutoFillBottle`
  - hook-map entry: ActionSpeed tool/interaction/eat/use paths.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6136`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:6182`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8624`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:107`
- Result: Watch
- Recommendation: Keep experimental. Ordinary mods cannot safely stack independent speed policies because the bridge chooses DTMAPI owners and writes shared body speed. Concrete risks are animation/controller state not restored, multiple mods racing multipliers, continuous-use speed leaking into item use, and config changes applying mid-action.

### `DTMAPI.Abstractions.IFishingAutomationApi.Configure(...)` and `SetEnabled(...)`

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.Configure(IManifest owner, FishingAutomationOptions options)`
- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.SetEnabled(IManifest owner, bool enabled, string reason)`
- Current marker: experimental
- Review advice: 保持 experimental
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:13`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:466`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:476`
- Function body summary: Stores normalized fishing automation options and per-owner enabled/phase state. `SetEnabled` changes DTMAPI state and optionally shows native feedback; automation executes later in fishing state/minigame hook methods.
- GameBridge/Harmony/reflection path: Fishing ready/cast/wait/minigame/pull hooks in `DolocTownHookCallbacks`, with automation helpers for auto-cast, instant bite, minigame completion, and cast/pull speed.
- Native owner: Fishing AgentState and minigame objects are reached; DTMAPI owns automation state.
- Native state holder: Native fishing state machine plus DTMAPI `fishingStates` and `fishingOptions`.
- DTMAPI registry/status/UI-only vs native runtime: Hybrid: DTMAPI policy/state drives native fishing state hooks.
- Evidence:
  - public-api-matrix entry: FishingAutomation API.
  - update record: `docs/updates/2026/20260603-0016-autofishing-skipfalse-minigame-smoke.md`, `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`
  - smoke path: `Smoke.AutoFishingPhase`, `Smoke.AutoFishingMiniGameSkip`, `Smoke.AutoFishingMiniGameComplete`, `Smoke.AutoFishingAnimationSpeed`
  - hook-map entry: fishing state and minigame hooks.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:7892`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8027`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:8324`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:211`
- Result: Watch
- Recommendation: Keep as DTMAPI AutoFishing bridge. Ordinary mods risk phase desync, manual-move cancellation conflicts, no-water/no-rod feedback mismatch, and minigame completion applying at the wrong state if they treat this as a stable general automation engine.

### `DTMAPI.Abstractions.IItemTooltipApi.ConfigureFishRoeProvider(...)`

- Symbol: `DTMAPI.Abstractions.IItemTooltipApi.ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)`
- Current marker: experimental
- Review advice: 升级候选 for display-only contract
- Ordinary mod usability: 普通 mod 可用 with caution
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:29`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:536`
- Function body summary: Stores display options and lookup provider. Item title/description/detail postfixes later call DTMAPI lookup to append tooltip display data.
- GameBridge/Harmony/reflection path: `Item.Title`, `Item.Description`, and item detail info postfixes through `DolocTownHookCallbacks.ItemTitlePostfix`, `ItemDescriptionPostfix`, and `ItemDetailInfoPostfix`.
- Native owner: Native item UI text getters are patched; no native item data/progression owner changes.
- Native state holder: UI string result only; DTMAPI provider dictionaries.
- DTMAPI registry/status/UI-only vs native runtime: Display-only native UI hook.
- Evidence:
  - public-api-matrix entry: ItemTooltip/fish roe row.
  - update record: animal/fish roe UI evidence records.
  - smoke path: fish roe tooltip smoke where present; otherwise No direct evidence in this slice.
  - hook-map entry: item tooltip hook entries.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:52`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:536`
- Result: Watch
- Recommendation: This can become ordinary-mod usable if documented as display-only. It must not imply the API creates fish roe items or native produce state; ordinary mods relying on it for data creation would show UI text only and still have no native item/progression backing.

### `DTMAPI.Abstractions.IAnimalViewerApi.ConfigureSpecialProduceProgress(...)`

- Symbol: `DTMAPI.Abstractions.IAnimalViewerApi.ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)`
- Current marker: experimental
- Review advice: 升级候选 for display-only contract
- Ordinary mod usability: 普通 mod 可用 with caution
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:37`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:553`
- Function body summary: Stores display policy. Animal viewer hooks clone/render progress rows and status text without changing native animal production data.
- GameBridge/Harmony/reflection path: `AnimalFullInfoData` constructor postfix, `AnimalViewer.Show` prefix/postfix, `AnimalPanel.RefreshViewer` postfix.
- Native owner: Native animal viewer UI is reached; animal production state owner is not changed.
- Native state holder: Native UI tree plus DTMAPI render state/options.
- DTMAPI registry/status/UI-only vs native runtime: UI-only display bridge.
- Evidence:
  - public-api-matrix entry: AnimalViewer API.
  - update record: 0.2.7/0.2.8 animal viewer/flicker fixes.
  - smoke path: `Smoke.AnimalPanelUi`, `Smoke.AnimalViewerUi`, `Smoke.AnimalViewerProgressUi`
  - hook-map entry: animal viewer UI hooks.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:553`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5714`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:70`
- Result: Watch
- Recommendation: Usable only as UI display policy. Ordinary mods must not assume hidden produce, animal stats, or produce timers are changed. Failure mode is "UI says progress" while native animal production remains whatever the game already stores.

### `DTMAPI.Abstractions.IInventoryDebugApi.GetItems(...)` and `GiveItem(...)`

- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GetItems(InventoryDebugQuery query)`
- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GiveItem(IManifest owner, string itemId, int count)`
- Current marker: experimental
- Review advice: MoveToDebugOnly
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:56`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3465`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3559`
- Function body summary: `GetItems` merges indexed content-source metadata with runtime item enumeration for Y-console browsing. `GiveItem` validates native item proto, source enablement, stackability, native placement capacity, then calls `DolocAPI.TryPlaceInBackpack` and verifies counts with `DolocAPI.CountItem`.
- GameBridge/Harmony/reflection path: Reflection into `DolocAPI.QueryItemProto`, `CanPlaceItem`, `TryPlaceInBackpack`, and `CountItem`.
- Native owner: Native item table and backpack placement owner reached.
- Native state holder: Player backpack/inventory and runtime item table; DTMAPI content index only filters source metadata.
- DTMAPI registry/status/UI-only vs native runtime: Native inventory mutation is real.
- Evidence:
  - public-api-matrix entry: InventoryDebug API.
  - update record: Y-console/debug records, especially `20260606-0008` and `20260606-0011`
  - smoke path: `Smoke.DebugInventory`, `Smoke.DebugInventoryGive`
  - hook-map entry: debug inventory/native backpack placement.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3465`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3559`
- Result: OK/Watch
- Recommendation: Keep debug-only. Native owner credibility is good, but ordinary mods should not use a debug grant API for normal gameplay because it bypasses economy/progression, can fill inventory unexpectedly, and can depend on debug source filtering rather than mod-owned content flow.

### `DTMAPI.Abstractions.IMailDeliveryApi.SendItemMail(...)`

- Symbol: `DTMAPI.Abstractions.IMailDeliveryApi.SendItemMail(IManifest owner, MailItemDeliveryRequest request)`
- Current marker: experimental
- Review advice: MoveToDebugOnly unless a separate gameplay mail contract is designed
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:64`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645`
- Function body summary: Validates item id/count/source, generates a native item, checks backpack/pending mail duplicates, calls `DolocAPI.SendItemAsEmail`, then verifies pending unaccepted item mail count.
- GameBridge/Harmony/reflection path: Reflection into `DolocAPI.QueryItemProto`, `DolocAPI.SendItemAsEmail`, and email collection reads.
- Native owner: Native email/mail item delivery owner reached.
- Native state holder: `EmailManager.emails`/pending mail and item table; DTMAPI request/source guard.
- DTMAPI registry/status/UI-only vs native runtime: Native mail mutation is real.
- Evidence:
  - public-api-matrix entry: MailDelivery API.
  - update record: Y-console/debug records.
  - smoke path: No direct evidence in this slice beyond debug smoke matrix if present.
  - hook-map entry: debug mail delivery.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645`
- Result: Watch
- Recommendation: Keep debug-only. Ordinary mods depending on it can create duplicate or progression-breaking mail, bypass normal quest/story gating, and couple gameplay to DTMAPI's debug source guard. A separate stable mail API would need normal scheduling, localization, duplicate policy, and save lifecycle evidence.

### `DTMAPI.Abstractions.IWeatherDebugApi.SetWeather(...)`

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod)`
- Current marker: experimental
- Review advice: MoveToDebugOnly
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:71`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3828`
- Function body summary: Parses the native weather enum, reads `DolocAPI.archiveHandle`, invokes `ArchiveDataHandle.SetWeather`, optionally invokes `PatchWeather`, then verifies current weather.
- GameBridge/Harmony/reflection path: Reflection into archive handle weather methods and `TbWeather` list enumeration.
- Native owner: Native archive weather owner reached.
- Native state holder: `ArchiveDataHandle.CurrentWeatherType` and current-day forecast/weather period data.
- DTMAPI registry/status/UI-only vs native runtime: Native weather mutation is real.
- Evidence:
  - public-api-matrix entry: WeatherDebug API.
  - update record: Y-console/debug records.
  - smoke path: `Smoke.DebugWeather`, `Smoke.DebugWeatherSet`
  - hook-map entry: debug weather set.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3828`
- Result: OK/Watch
- Recommendation: Keep debug-only. Ordinary mods using this can globally mutate world weather/forecast and invalidate season/period assumptions for other systems. Stable weather APIs would need scheduling/event semantics rather than direct debug mutation.

### `DTMAPI.Abstractions.ITeleportDebugApi.Teleport(...)`

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.Teleport(IManifest owner, string destinationId)`
- Current marker: experimental
- Review advice: MoveToDebugOnly
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:80`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3896`
- Function body summary: Looks up a DTMAPI whitelisted destination/mark point, reflects `DolocAPI.DoTransport`, invokes it with the mark point, and records snapshot before/after request.
- GameBridge/Harmony/reflection path: Reflection into `DolocAPI.DoTransport`.
- Native owner: Native transport system reached, but only via DTMAPI whitelist.
- Native state holder: Native current room/agent position/transport transition state.
- DTMAPI registry/status/UI-only vs native runtime: Native transport request is real.
- Evidence:
  - public-api-matrix entry: TeleportDebug API.
  - update record: Y-console/teleport CSV records.
  - smoke path: `Smoke.DebugTeleport`, `Smoke.DebugTeleportRequest`
  - hook-map entry: debug teleport.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3875`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3896`
- Result: OK/Watch
- Recommendation: Keep debug-only. Ordinary mods cannot treat this as a stable location API because it is whitelist/mark-point driven and can bypass progression, room entry preconditions, and mod-specific state cleanup.

### `DTMAPI.Abstractions.IInstantSaveDebugApi.Save(...)`

- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.Save(IManifest owner, bool reloadAfterSave)`
- Current marker: experimental-save-only
- Review advice: MoveToDebugOnly; keep reload blocked
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:90`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3997`
- Function body summary: Rejects `reloadAfterSave=true` due known scene residue risk, verifies current save slot/can-save state, invokes `DolocAPI.SaveGame(int)`, and records before/after state.
- GameBridge/Harmony/reflection path: Reflection into `DolocAPI.SaveGame`; lifecycle save hooks are separate.
- Native owner: Native save owner reached for save-only path.
- Native state holder: Current archive slot/save files/native save transaction.
- DTMAPI registry/status/UI-only vs native runtime: Native save mutation is real.
- Evidence:
  - public-api-matrix entry: InstantSaveDebug API.
  - update record: `docs/updates/2026/20260605-0003-026-mine-yconsole-fixes.md`, `docs/updates/2026/20260606-0004-029-readme-implementation.md`
  - smoke path: `Smoke.InstantSave`, `Debug.InstantSave`
  - hook-map entry: save debug path.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3997`
- Result: Watch
- Recommendation: Ordinary mods cannot use this as a general save/reload API. The concrete risk is scene residue and stale runtime state if reload is attempted during a running save; even save-only can serialize mod/debug side effects outside normal player-triggered timing.

### `DTMAPI.Abstractions.ITimeDebugApi.SkipToNextWeatherPeriod(...)`

- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.SkipToNextWeatherPeriod(IManifest owner)`
- Current marker: experimental
- Review advice: MoveToDebugOnly
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:98`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4052`
- Function body summary: Calculates the next 06:00/18:00/24:00 target from native global parameters, invokes `ArchiveDataHandle.PassTimeNoControl`, and passes a wake callback to `DolocAPI.OnWakeUp`.
- GameBridge/Harmony/reflection path: Reflection into archive pass-time and wake-up methods.
- Native owner: Native time/pass-time owner reached.
- Native state holder: Archive time/weather/crop/machine/world progression state.
- DTMAPI registry/status/UI-only vs native runtime: Native time progression is real.
- Evidence:
  - public-api-matrix entry: TimeDebug API.
  - update record: advanced Y-console records.
  - smoke path: `Smoke.DebugTime`, `Smoke.DebugTimeSkip`
  - hook-map entry: debug time skip.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4052`
- Result: Watch
- Recommendation: Keep debug-only. Ordinary mods using this can advance crops, weather, machines, animals, and scheduled state globally, causing save/progression pollution and interactions with other mods' time assumptions.

### `DTMAPI.Abstractions.IMovementDebugApi.SetSpeedMultiplier(...)`

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.SetSpeedMultiplier(IManifest owner, double multiplier)`
- Current marker: experimental
- Review advice: MoveToDebugOnly or design separate movement buff API
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:106`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4120`
- Function body summary: Resolves the player's motion ability, invokes `SetMoveScaler` or `SetMoveSpeedScale`, stores a DTMAPI multiplier, and verifies state.
- GameBridge/Harmony/reflection path: Reflection into the player `MotionAbility` object.
- Native owner: Native movement ability owner reached.
- Native state holder: Player motion ability singleton/current player state.
- DTMAPI registry/status/UI-only vs native runtime: Native speed mutation is real.
- Evidence:
  - public-api-matrix entry: MovementDebug API.
  - update record: Y-console/debug records.
  - smoke path: `Smoke.DebugMovement`, `Smoke.DebugMovementSpeed`
  - hook-map entry: debug movement speed.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4120`
- Result: Watch
- Recommendation: Keep debug-only. Ordinary mods using it can leave movement scale stuck, fight other movement buffs, or mutate player singleton state without duration/stacking/cleanup policy.

### `DTMAPI.Abstractions.IAdvancedDebugApi`

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AdvanceTime(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SetTimeScale(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.ResetTimeScale(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AddMoney(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AddTechPoint(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.UnlockAllTechTrees(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.MatureAllCrops(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SetCreativeMode(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GiveCreativeGenerator(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SpawnMonster(...)`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SpawnResource(...)`
- Current marker: experimental
- Review advice: MoveToDebugOnly
- Ordinary mod usability: debug-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:188`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4410`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4457`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4489`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4519`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4551`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4588`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4624`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4670`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4696`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4703`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4745`
- Function body summary: Methods call native time/time-scale APIs, direct archive money fields or `Command_AddMoney`, tech-point APIs, native tech-tree save collections, crop debug setters, `GameInitConfig` debug flags plus no-cost/no-time hooks, inventory give for creative generator, official monster command, and dungeon resource host creation/render methods.
- GameBridge/Harmony/reflection path: Mixed reflection into `DolocAPI`, `ArchiveDataHandle`, `GameInitConfig`, `Motion/room host` objects, and creative-mode Harmony prefixes/postfixes.
- Native owner: Mixed and method-specific. Some methods use official debug commands; some directly mutate save collections/fields; some create room objects through host interfaces.
- Native state holder: Save archive, current room object lists, player inventory, tech tree collections, crop/equipment instances, time scale singleton, creative config flags.
- DTMAPI registry/status/UI-only vs native runtime: Native mutations are real, but safety is debug-only and mixed.
- Evidence:
  - public-api-matrix entry: AdvancedDebug API rows.
  - update record: `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`, `docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md`
  - smoke path: `Smoke.AdvancedDebug`, creative/no-cost/no-time/monster/resource rows.
  - hook-map entry: advanced Y-console creative and spawn hooks.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4255`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4670`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:163`
- Result: Watch / Gap by method
- Recommendation: Keep strictly debug-only and out of ordinary mod docs. Ordinary mods using these would directly pollute saves or world state: money/tech/tree unlocks bypass progression; crop maturity can invalidate farm state; creative flags can leak no-cost/no-time global behavior; monster/resource spawn can leave room residue; time scale can stay altered after errors. A stable API would need separate, constrained gameplay contracts with cleanup and ownership rules.

### Debug query and `GetStatus` methods

- Symbol: `DTMAPI.Abstractions.IActionCompletionApi.GetStatus(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IActionSpeedApi.GetStatus(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.GetState(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.GetStatus(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IItemTooltipApi.GetStatus(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IAnimalViewerApi.GetStatus(string uniqueId)`
- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.IMailDeliveryApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetState()`
- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetAvailableWeathers()`
- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetDestinations()`
- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetCurrentSnapshot()`
- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.GetState()`
- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.GetState()`
- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.GetState()`
- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.GetStatus()`
- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetStatus()`
- Current marker: experimental
- Review advice: 保持 experimental/debug-only/read-only as applicable
- Ordinary mod usability: debug-only for debug APIs; 仅 DTMAPI 自家 mod 可用 for migrated gameplay status; 普通 mod 可用 with caution for display-only status
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:6`, `:13`, `:22`, `:30`, `:37`, `:56`, `:64`, `:71`, `:80`, `:90`, `:98`, `:106`, `:188`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:416`, `:431`, `:493`, `:515`, `:561`, `:3640`, `:3759`, `:3764`, `:3795`, `:3870`, `:3875`, `:3888`, `:3945`, `:3992`, `:4042`, `:4047`, `:4110`, `:4115`, `:4166`, `:4808`
- Function body summary: Status methods return `BridgeFeatureStatus` values derived from DTMAPI policy dictionaries, hook-installed flags, and last-known hook evidence strings. Query methods read native state where relevant: inventory queries enumerate runtime item/source state, weather queries read archive/weather tables, teleport snapshots read `DolocAPI.CurrentRoom`/`AgentPosition`, save/time/movement state methods read native archive/player state through helper methods, and fishing/action/display status reads DTMAPI policy state.
- GameBridge/Harmony/reflection path: Query methods use the same reflection helpers as the write methods; status-only methods mostly read DTMAPI flags set by Harmony patch installation and smoke/hook callbacks.
- Native owner: Mixed. Weather/teleport/save/time/movement query methods read native owners; action/fishing/display status methods report DTMAPI policy/hook readiness; inventory status describes native backpack placement capability but does not mutate.
- Native state holder: Method-specific native archive, item table, room, player motion, weather/time state; DTMAPI status dictionaries and hook flags.
- DTMAPI registry/status/UI-only vs native runtime: Status strings are DTMAPI evidence/status, not native contracts. Some snapshots are native reads.
- Evidence:
  - public-api-matrix entry: rows 62-75 and debug/gameplay family rows.
  - update record: Y-console/debug and migrated mod smoke records cited in this volume.
  - smoke path: corresponding `Smoke.Debug*`, `Smoke.AutoFishing*`, `Smoke.ActionSpeed*`, and display smoke rows.
  - hook-map entry: matching hook status ids in `docs/hook-map/README.md`.
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:416`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3764`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3888`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4047`
- Result: Watch
- Recommendation: Ordinary mods must not treat `BridgeFeatureStatus.Status` or debug state snapshots as stable native contracts. They are useful evidence and UI state, but the concrete risk is "status says configured/verified" while a different room, save boundary, disabled source, or missing hook target makes the native owner unavailable. Keep debug query/status APIs in debug docs; for display-only status, document it as UI/provider readiness only.

### `DTMAPI.Abstractions.IChestLocatorEnhancerApi.Register(...)`

- Symbol: `DTMAPI.Abstractions.IChestLocatorEnhancerApi.Register(IManifest owner, ChestLocatorEnhancerOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental / DTMAPI-owned mod API
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:172`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1332`
- Function body summary: Registers a DTMAPI policy for appending shared container inventories so chest locator checks can include additional official containers.
- GameBridge/Harmony/reflection path: `ArchiveDataHandle.GetAvailableInventories` array-result postfix appends DTMAPI policy inventories while leaving native CountItem/CostItem transaction logic in place.
- Native owner: Native inventory enumeration owner is reached and extended.
- Native state holder: Native shared inventory arrays and container inventories; DTMAPI policy controls append.
- DTMAPI registry/status/UI-only vs native runtime: Native query extension with DTMAPI policy.
- Evidence:
  - public-api-matrix entry: ChestLocatorEnhancer rows.
  - update record: `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`
  - smoke path: `Smoke.ChestLocatorEnhancer`
  - hook-map entry: `Inventory.ChestLocatorEnhancer`
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:883`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1332`
- Result: Watch
- Recommendation: Keep experimental and mod-scoped. Ordinary mods could accidentally append inventories into global item-count/cost queries, causing shared container pollution or unexpected cross-room availability.

### `DTMAPI.Abstractions.IStrongPlantingGunApi.Register(...)`

- Symbol: `DTMAPI.Abstractions.IStrongPlantingGunApi.Register(IManifest owner, StrongPlantingGunOptions options)`
- Current marker: experimental
- Review advice: 保持 experimental / DTMAPI-owned mod API
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:180`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1373`
- Function body summary: Registers DTMAPI policy for stronger farming gun slots and behavior. GameBridge hooks official farming gun construction/use/UI transfer paths and delegates plant checks to official methods.
- GameBridge/Harmony/reflection path: `ItemFarmingGun` constructors, `ItemFarmingGun.OnUseAsTool`, `FarmingGunUiState.HandlePlaceToOtherSide`, and `HandleSwapOneItem`.
- Native owner: Official farming gun and farming UI methods are reached; DTMAPI owns multi-slot policy.
- Native state holder: Native farming gun/item UI plus DTMAPI slot policy/state.
- DTMAPI registry/status/UI-only vs native runtime: Narrow native tool/UI bridge with DTMAPI policy.
- Evidence:
  - public-api-matrix entry: StrongPlantingGun rows.
  - update record: `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`
  - smoke path: `Smoke.StrongPlantingGun`
  - hook-map entry: `Farming.StrongPlantingGun`
  - code path: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:894`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1373`
- Result: Watch
- Recommendation: Keep experimental and DTMAPI-mod-scoped. Ordinary mods relying on it as a general farming tool API can create UI transfer mismatch, slot-state confusion, or item application outside official basin/tool preconditions.

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Mark all Y-console/debug interfaces `debug-only` in developer docs | They mutate native state and should not be copied into ordinary mod recipes. |
| P0 | Add a `SafeGameplayApi` split for future stable inventory/mail/weather/time needs | Current debug APIs are too permissive and bypass normal progression semantics. |
| P1 | Add owner arbitration docs for ActionSpeed/ActionCompletion/Fishing | Multiple mods cannot safely stack policies without a clear merge model. |
| P1 | Document display-only contracts for ItemTooltip and AnimalViewer | Prevents semantic over-promise around native item/animal data. |
| P1 | Keep ChestLocator and StrongPlantingGun under migrated-mod API namespace or docs section | Their hooks are narrow and policy-driven. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Whether action-speed multipliers restore correctly on every exception path | Read hook callbacks and `ApplyActionSpeedToBody`; smoke covers selected paths only. | Add targeted code-level review of all exit paths before stable docs. |
| Complete mail duplicate/localization semantics | `SendItemMail` checks pending counts and source guards, but no broad mail lifecycle review in this slice. | Review native mail templates, accepted mail history, and localization fields before a stable mail API. |
| Full side effects of advanced spawn/resource commands | Code uses official command/room host creation, but room cleanup/save persistence varies by object. | Map spawned object persistence and cleanup in reverse build before any non-debug use. |
