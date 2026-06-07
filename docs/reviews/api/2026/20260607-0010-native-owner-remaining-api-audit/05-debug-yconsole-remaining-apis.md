# 05 - Debug And Y Console Remaining APIs

## Scope

Audited remaining debug/Y-console APIs not already closed by the 0009 Save/Time/Teleport/Inventory pass:

- `IDebugConsoleApi.Bind`, `BindAdvanced`, `SetLanguage`, `Open`, `Close`, `Toggle`, `GetStatus`
- `IWeatherDebugApi.GetState`, `GetAvailableWeathers`, `SetWeather`, `GetStatus`
- `IMovementDebugApi.GetState`, `SetSpeedMultiplier`, `ResetSpeed`, `GetStatus`
- `IMailDeliveryApi.SendItemMail`, `GetStatus`
- `IAdvancedDebugApi` option queries, time/value/crop/creative/spawn methods, and `GetStatus`

`IInventoryDebugApi`, `ITeleportDebugApi`, `IInstantSaveDebugApi`, and `ITimeDebugApi` were covered in 0009; they appear here only as dependencies of `IDebugConsoleApi.Bind`.

## Files read

- `docs/api/public-api-matrix.md`: lines 74-83.
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`: lines 43-78, 106-113, 188-207, 295-365, and 435-589.
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`: lines 13-260.
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`: lines 43-80 and 303-317.
- `testmods/DebugConsoleMod/ModEntry.cs`: lines 17-150.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`: lines 960-1032.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`: lines 138-190.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`: lines 3645-3872, 4115-4169, and 4255-4810.
- `docs/hook-map/README.md`: lines 339-388, 392-418, 544-552, and 357-371.
- `docs/debug/regressions/smoke-matrix.md`: lines 53-58.
- `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`: lines 22-63.
- `docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md`: lines 29-67.
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`: weather and monster/resource debug candidate owner notes.
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`: weather, monster, and native final-effect references.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`: `DolocAPI.SendItemAsEmail`, `ItemFarmingGun`, and inventory/input candidate map.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/UI.md`: `AgentControllerState.EnterUICheck` candidate map.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Action_Interaction.md`: `AgentControllerState.UseTool/UseItem` candidate map.

## Functions read

- `DTMAPI.Abstractions.IDebugConsoleApi.Bind`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:47`.
- `DTMAPI.Abstractions.IDebugConsoleApi.BindAdvanced`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:48`.
- `ReflectedDebugConsoleUi.Bind`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:116`.
- `ReflectedDebugConsoleUi.BindAdvanced`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:130`.
- `ReflectedDebugConsoleUi.SetLanguage`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:139`.
- `ReflectedDebugConsoleUi.Open`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148`.
- `ReflectedDebugConsoleUi.Close`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:176`.
- `ReflectedDebugConsoleUi.Toggle`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:190`.
- `ReflectedDebugConsoleUi.GetStatus`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:218`.
- `ReflectedDebugConsoleUi.Update`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:224`.
- `DebugConsoleMod.ModEntry.BindApis`: `testmods/DebugConsoleMod/ModEntry.cs:35`.
- `DebugConsoleMod.ModEntry.OnButtonPressed`: `testmods/DebugConsoleMod/ModEntry.cs:97`.
- `DolocTownExperimentalBridgeApi.SendItemMail`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645`.
- `DolocTownExperimentalBridgeApi.GetState` for weather: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3764`.
- `DolocTownExperimentalBridgeApi.GetAvailableWeathers`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3795`.
- `DolocTownExperimentalBridgeApi.SetWeather`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3828`.
- `DolocTownExperimentalBridgeApi.SetSpeedMultiplier`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4120`.
- `DolocTownExperimentalBridgeApi.ResetSpeed`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4158`.
- `DolocTownExperimentalBridgeApi.GetCreativeModeState`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4255`.
- `DolocTownExperimentalBridgeApi.AdvanceTime`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4410`.
- `DolocTownExperimentalBridgeApi.SetTimeScale`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4457`.
- `DolocTownExperimentalBridgeApi.ResetTimeScale`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4489`.
- `DolocTownExperimentalBridgeApi.AddMoney`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4519`.
- `DolocTownExperimentalBridgeApi.AddTechPoint`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4551`.
- `DolocTownExperimentalBridgeApi.UnlockAllTechTrees`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4588`.
- `DolocTownExperimentalBridgeApi.MatureAllCrops`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4624`.
- `DolocTownExperimentalBridgeApi.SetCreativeMode`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4670`.
- `DolocTownExperimentalBridgeApi.GiveCreativeGenerator`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4696`.
- `DolocTownExperimentalBridgeApi.SpawnMonster`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4703`.
- `DolocTownExperimentalBridgeApi.SpawnResource`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4745`.
- `DolocTownHookCallbacks.AgentControllerStateEnterUiCheckPrefix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:148`.
- `DolocTownHookCallbacks.AdvancedCreativeBoolTruePrefix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:163`.
- `DolocTownHookCallbacks.AdvancedCreativeRecipeTimePostfix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:182`.

## Call graph

```text
Y console host
  DebugConsoleMod.Entry
    helper.Input.RegisterButton("Y"/"Escape")
    BindApis
      ModRegistry.GetApi<IDebugConsoleApi>("DTMAPI.DebugConsoleHost")
      ModRegistry.GetApi<I*DebugApi>("DTMAPI.GameBridge.DolocTown")
      consoleApi.Bind + BindAdvanced + SetLanguage
  ButtonPressed(Y/Escape)
    IDebugConsoleApi.Toggle/Open/Close
      ReflectedDebugConsoleUi state
      runtime.UI.OpenCustomMenu("DTMAPI.DebugConsole")
      DolocTownHookCallbacks.DebugConsoleModalOpen = true
  Harmony input isolation
    AgentControllerState.EnterUICheck/UseTool/UseItem prefixes
      native gameplay input swallowed only while console is open

Mail
  IMailDeliveryApi.SendItemMail
    QueryItemProto
    content-source gating
    TryGenerateNativeItem preflight
    CountItem + pending email duplicate scan
    DolocAPI.SendItemAsEmail

Weather
  IWeatherDebugApi.GetState/GetAvailableWeathers
    DolocAPI.archiveHandle + TbWeather + current-day forecast
  SetWeather
    ArchiveDataHandle.SetWeather(weather, true)
    optional ArchiveDataHandle.PatchWeather

Movement
  IMovementDebugApi.SetSpeedMultiplier/ResetSpeed
    Resolve player MotionAbility
    MotionAbility.SetMoveScaler(multiplier - 1)

Advanced
  IAdvancedDebugApi.*
    time: ArchiveDataHandle.PassTimeNoControl + DolocAPI.OnWakeUp
    time scale: DolocAPI.SetTimeScale/RevertTimeScale
    money/tech: DolocAPI.Command_AddMoney or archive fallback, DolocAPI.AddTechPoint
    tech unlock: farmData.unlockedTechTree
    crops: PlantBasin.Crop.DEBUG_SetLevel
    creative: GameInitConfig debug flags + Harmony prefixes/postfixes
    spawn: DolocAPI.Command_GenerateMonster, IDungeonResourceHost.CreateDungeonResourceNoRender
```

## Function body findings

- `ReflectedDebugConsoleUi.Bind` stores debug API references and records `UI.DebugConsoleHost`. It requires inventory, weather, teleport, time, movement, instant-save, and advanced APIs to report fully bound status (`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:116-128`, `218-222`).
- `Open` sets `IsOpen`, opens a DTMAPI custom menu, and flips `DolocTownHookCallbacks.DebugConsoleModalOpen=true`; `Close` reverses the flag and closes the active DTMAPI UI menu if needed (`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148-188`).
- `Update` consumes Escape/Y while open before rendering and also handles right-click item give through current pointer hit targets. This host-level isolation is separate from `IInputHelper.Suppress` (`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:224-260`).
- GameBridge patches `AgentControllerState.UseTool`, `UseItem`, and `EnterUICheck` for native input isolation only while `DebugConsoleModalOpen` is true (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:964-980`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:138-160`).
- `SendItemMail` verifies the item exists in native `TbItem`, applies optional source-id/source-enabled guards, pre-generates a native item attachment, checks backpack and pending unaccepted mail duplicates, then calls `DolocAPI.SendItemAsEmail`. It explicitly reports the current build's template-based limitation in `GetStatus` (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645-3762`).
- Weather state reads `DolocAPI.archiveHandle`, date/time/season/current weather, current-day forecast IDs, and `TbWeather`; `SetWeather` parses the native enum and invokes `ArchiveDataHandle.SetWeather`, optionally `PatchWeather` (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3764-3872`).
- Movement uses the player's `MotionAbility.SetMoveScaler` or fallback method name and stores the applied multiplier in GameBridge state (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4115-4169`).
- Advanced creative mode is broad: GameBridge patches multiple `DolocAPI` cost/afford methods plus `Synthesizer.GetRecipeTime`, toggles `GameInitConfig` debug flags, and records smoke verification for cost and recipe-time bypass (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:982-1032`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4255-4371`).
- Advanced money uses `DolocAPI.Command_AddMoney` when present and falls back to writing `ArchiveDataHandle.CurrentMoney` if the command is missing. That fallback is debug-only and should never be a stable economy API (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4519-4548`).
- `UnlockAllTechTrees` edits `farmData.unlockedTechTree` directly after enumerating `TbTechTree`, which is a clear debug operation rather than a progression API (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4588-4622`).
- `SpawnMonster` uses an official command but only after verifying current room implements `IMonsterHost`; `SpawnResource` requires `IDungeonResourceHost` and creates no-render resources near the player before rendering if the room is currently rendered (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4703-4805`).

## Native owner verdict

| API | Verdict | Native owner reached |
| --- | --- | --- |
| `IDebugConsoleApi` | Partial / DTMAPI UI owner | DTMAPI Canvas host; native input isolation reaches `AgentControllerState` only while open. |
| `IMailDeliveryApi` | Reached/debug-only | `DolocAPI.SendItemAsEmail` plus native item/query/count checks; template/title limitations remain. |
| `IWeatherDebugApi` | Reached/debug-only | `TbWeather`, `ArchiveDataHandle.SetWeather`, and `PatchWeather`. |
| `IMovementDebugApi` | Reached/debug-only | Player `MotionAbility.SetMoveScaler`. |
| `IAdvancedDebugApi` | Reached/debug-only | Multiple native debug owners reached, but several operations are broad state edits or Harmony bypasses. |

## Ordinary mod usability

- `IDebugConsoleApi`: debug-only; ordinary gameplay mods should not depend on it.
- `IMailDeliveryApi`: debug-only or DTMAPI feature-only; not a stable ordinary reward/mail API.
- `IWeatherDebugApi`: debug-only.
- `IMovementDebugApi`: debug-only.
- `IAdvancedDebugApi`: debug-only.

## Concrete failure modes

1. An ordinary mod using `IMailDeliveryApi` for quest rewards can create template-only mails whose custom title/content/sender are ignored, causing player-facing text mismatch or duplicate pending attachments.
2. A weather mod using `SetWeather` as a schedule API can desync forecast/current-period expectations because it changes current native weather immediately and optionally patches the current period.
3. A gameplay mod using `SetSpeedMultiplier` can globally overwrite player movement speed without scoped stacking; another mod or return-to-title reset can restore the wrong value.
4. Advanced creative mode can globally bypass money/material/energy costs and recipe time through Harmony prefixes, polluting unrelated gameplay if exposed beyond the Y-console.
5. `UnlockAllTechTrees` and `AddMoney` bypass progression flows and may save permanent state changes without quest/tutorial/event owners.
6. `SpawnMonster` and `SpawnResource` are room-type-gated; ordinary mods can fail in unsupported rooms or create entities/resources without stable ownership over AI, drops, cleanup, or save persistence.

## Minimal rebuild direction

- Keep all APIs in this volume marked `debug-only` in developer docs.
- For mail, design a separate stable `IRewardMailApi` only after native mail templates/title/content/sender behavior is proven and save/unclaimed mail lifecycle is documented.
- For weather, split read-only forecast queries from scheduled weather requests with native calendar owners.
- For movement, design owner-scoped movement modifiers with tokens, stacking, and automatic restore.
- For advanced debug, keep the whitelisted Y-console wrapper and explicitly exclude ordinary mod access to raw creative/progression/spawn verbs.
- Keep Y-console input isolation separate from general input suppression until a stable native action-suppression adapter exists.

## Evidence gaps

- Mail custom title/content/sender remains unproven in the current game build; existing status explicitly says template-based.
- No scoped movement modifier owner was found; only direct `MotionAbility.SetMoveScaler` is used.
- No ordinary-mod safe owner was found for creative/progression/spawn debug verbs.
- Reverse maps/research notes confirmed the candidate native debug owners used here, including `SendItemAsEmail`, `SetWeather/PatchWeather`, `AgentControllerState.EnterUICheck`, and monster/resource spawn candidates, but did not make them ordinary-mod safe gameplay contracts.
- No game smoke was run in this pass. Evidence was read from public matrix lines 74-83, hook-map debug sections, smoke-matrix rows 53-58, and update records `20260606-0008` / `20260606-0011`.
