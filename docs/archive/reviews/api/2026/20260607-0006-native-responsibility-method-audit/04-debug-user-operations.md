# Debug Console And User Operations Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `IAdvancedDebugApi` contains save/global mutation wrappers; all ordinary-mod usage is debug-only and risks irreversible save pollution.
- `IInventoryDebugApi`/`IMailDeliveryApi` reach native item owners but mutate saves, so they must stay debug or controlled delivery APIs.
- `ITeleportDebugApi` mixes official mark transport and direct room entry; docs must not hide route differences.
- `IInstantSaveDebugApi` save-only semantics must not revive reload-after-save behavior without lifecycle review.

## Audit Blocks

<a id="sym-0812"></a>
### DTMAPI.Abstractions.IDebugConsoleApi

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:44`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:13`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:13
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0813"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.IsOpen

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.IsOpen`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:46`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:95`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:95
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0814"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:47`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:116`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:116
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0815"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:48`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:130`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:130
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0816"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.SetLanguage(IManifest owner, string language)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.SetLanguage(IManifest owner, string language)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:49`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:139`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:139
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0817"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.Open(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Open(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:50`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:148
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0818"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.Close(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Close(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:51`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:113`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:113
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0819"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.Toggle(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.Toggle(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:52`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:190`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:190
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0820"></a>
### DTMAPI.Abstractions.IDebugConsoleApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDebugConsoleApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:53`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:218`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:74 `IDebugConsoleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:218
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0821"></a>
### DTMAPI.Abstractions.IInventoryDebugApi

- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:57`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:76 `IInventoryDebugApi.GetItems/GiveItem` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0822"></a>
### DTMAPI.Abstractions.IInventoryDebugApi.GetItems(InventoryDebugQuery query)

- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GetItems(InventoryDebugQuery query)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:59`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:1184`
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:76 `IInventoryDebugApi.GetItems/GiveItem` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:1184
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0823"></a>
### DTMAPI.Abstractions.IInventoryDebugApi.GiveItem(IManifest owner, string itemId, int count)

- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GiveItem(IManifest owner, string itemId, int count)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:60`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4699`
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:76 `IInventoryDebugApi.GetItems/GiveItem` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4699
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0824"></a>
### DTMAPI.Abstractions.IInventoryDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IInventoryDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:61`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3640`
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:76 `IInventoryDebugApi.GetItems/GiveItem` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3640
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0825"></a>
### DTMAPI.Abstractions.IMailDeliveryApi

- Symbol: `DTMAPI.Abstractions.IMailDeliveryApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:65`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:77 `IMailDeliveryApi.SendItemMail/GetStatus` (experimental)
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0826"></a>
### DTMAPI.Abstractions.IMailDeliveryApi.SendItemMail(IManifest owner, MailItemDeliveryRequest request)

- Symbol: `DTMAPI.Abstractions.IMailDeliveryApi.SendItemMail(IManifest owner, MailItemDeliveryRequest request)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:67`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645`
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:77 `IMailDeliveryApi.SendItemMail/GetStatus` (experimental)
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3645
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0827"></a>
### DTMAPI.Abstractions.IMailDeliveryApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IMailDeliveryApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:68`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3759`
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:77 `IMailDeliveryApi.SendItemMail/GetStatus` (experimental)
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3759
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0828"></a>
### DTMAPI.Abstractions.IWeatherDebugApi

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:72`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:78 `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0829"></a>
### DTMAPI.Abstractions.IWeatherDebugApi.GetState()

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetState()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:74`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:493`
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:78 `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:493
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0830"></a>
### DTMAPI.Abstractions.IWeatherDebugApi.GetAvailableWeathers()

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetAvailableWeathers()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:75`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3775`
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:78 `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3775
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0831"></a>
### DTMAPI.Abstractions.IWeatherDebugApi.SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod)

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:76`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:271`
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:78 `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:271
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0832"></a>
### DTMAPI.Abstractions.IWeatherDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IWeatherDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:77`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3870`
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:78 `IWeatherDebugApi.GetState/GetAvailableWeathers/SetWeather` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3870
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0833"></a>
### DTMAPI.Abstractions.ITeleportDebugApi

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:81`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0834"></a>
### DTMAPI.Abstractions.ITeleportDebugApi.GetDestinations()

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetDestinations()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:83`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3981`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3981
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0835"></a>
### DTMAPI.Abstractions.ITeleportDebugApi.GetCurrentSnapshot()

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetCurrentSnapshot()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:84`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3888`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3888
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0836"></a>
### DTMAPI.Abstractions.ITeleportDebugApi.Teleport(IManifest owner, string destinationId)

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.Teleport(IManifest owner, string destinationId)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:85`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3883`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3883
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0837"></a>
### DTMAPI.Abstractions.ITeleportDebugApi.ExportDestinationsCsv(IManifest owner)

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.ExportDestinationsCsv(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:86`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3206`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3206
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0838"></a>
### DTMAPI.Abstractions.ITeleportDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.ITeleportDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:87`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3945`
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:79 `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3945
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0839"></a>
### DTMAPI.Abstractions.IInstantSaveDebugApi

- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:91`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:81 `IInstantSaveDebugApi.GetState/SaveHere/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0840"></a>
### DTMAPI.Abstractions.IInstantSaveDebugApi.GetState()

- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.GetState()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:93`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3992`
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:81 `IInstantSaveDebugApi.GetState/SaveHere/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3992
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0841"></a>
### DTMAPI.Abstractions.IInstantSaveDebugApi.Save(IManifest owner, bool reloadAfterSave)

- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.Save(IManifest owner, bool reloadAfterSave)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:94`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:278`
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:81 `IInstantSaveDebugApi.GetState/SaveHere/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:278
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0842"></a>
### DTMAPI.Abstractions.IInstantSaveDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IInstantSaveDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:95`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4042`
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:81 `IInstantSaveDebugApi.GetState/SaveHere/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4042
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0843"></a>
### DTMAPI.Abstractions.ITimeDebugApi

- Symbol: `DTMAPI.Abstractions.ITimeDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:99`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:82 `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0844"></a>
### DTMAPI.Abstractions.ITimeDebugApi.GetState()

- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.GetState()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:101`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4047`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:82 `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4047
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0845"></a>
### DTMAPI.Abstractions.ITimeDebugApi.SkipToNextWeatherPeriod(IManifest owner)

- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.SkipToNextWeatherPeriod(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:102`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4052`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:82 `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4052
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0846"></a>
### DTMAPI.Abstractions.ITimeDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.ITimeDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:103`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4110`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:82 `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4110
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0847"></a>
### DTMAPI.Abstractions.IMovementDebugApi

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:107`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:83 `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0848"></a>
### DTMAPI.Abstractions.IMovementDebugApi.GetState()

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.GetState()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:109`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4115`
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:83 `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4115
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0849"></a>
### DTMAPI.Abstractions.IMovementDebugApi.SetSpeedMultiplier(IManifest owner, double multiplier)

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.SetSpeedMultiplier(IManifest owner, double multiplier)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:110`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4120`
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:83 `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4120
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0850"></a>
### DTMAPI.Abstractions.IMovementDebugApi.ResetSpeed(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.ResetSpeed(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:111`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4158`
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:83 `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4158
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0851"></a>
### DTMAPI.Abstractions.IMovementDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IMovementDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:112`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4166`
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:83 `IMovementDebugApi.GetState/SetSpeedMultiplier/ResetSpeed/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4166
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0896"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:189`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0897"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GetTechPointOptions()

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetTechPointOptions()`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:191`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4171`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4171
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0898"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GetMonsterOptions()

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetMonsterOptions()`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:192`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4200`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4200
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0899"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GetResourceOptions()

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetResourceOptions()`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:193`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4229`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4229
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0900"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GetCreativeModeState()

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetCreativeModeState()`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:194`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4255`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4255
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0901"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:195`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4410`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4410
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0902"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.SetTimeScale(IManifest owner, double multiplier)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SetTimeScale(IManifest owner, double multiplier)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:196`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4457`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4457
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0903"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.ResetTimeScale(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.ResetTimeScale(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:197`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4489`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4489
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0904"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.AddMoney(IManifest owner, int amount)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AddMoney(IManifest owner, int amount)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:198`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4519`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4519
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0905"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.AddTechPoint(IManifest owner, string pointTypeId, int amount)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.AddTechPoint(IManifest owner, string pointTypeId, int amount)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:199`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4551`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4551
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0906"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.UnlockAllTechTrees(IManifest owner)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.UnlockAllTechTrees(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:200`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4588`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4588
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0907"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.MatureAllCrops(IManifest owner)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.MatureAllCrops(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:201`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4624`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4624
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0908"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.SetCreativeMode(IManifest owner, bool enabled)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SetCreativeMode(IManifest owner, bool enabled)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:202`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4670`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4670
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0909"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GiveCreativeGenerator(IManifest owner)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GiveCreativeGenerator(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:203`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4696`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4696
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0910"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.SpawnMonster(IManifest owner, string monsterId, int count)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SpawnMonster(IManifest owner, string monsterId, int count)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:204`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4703`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4703
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0911"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.SpawnResource(IManifest owner, string resourceId, int count)

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.SpawnResource(IManifest owner, string resourceId, int count)`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:205`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4745`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4745
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0912"></a>
### DTMAPI.Abstractions.IAdvancedDebugApi.GetStatus()

- Symbol: `DTMAPI.Abstractions.IAdvancedDebugApi.GetStatus()`
- Current marker: `experimental`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:206`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4808`
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:75 `IDebugConsoleApi.BindAdvanced` / `IAdvancedDebugApi` (experimental)
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4808
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-0913"></a>
### DTMAPI.Abstractions.AdvancedTimeAdvanceKind

- Symbol: `DTMAPI.Abstractions.AdvancedTimeAdvanceKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:209`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0914"></a>
### DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Day

- Symbol: `DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Day`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:211`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:615`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:615
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0915"></a>
### DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Week

- Symbol: `DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Week`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:212`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:616`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:616
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0916"></a>
### DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Month

- Symbol: `DTMAPI.Abstractions.AdvancedTimeAdvanceKind.Month`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:213`
- Implementation: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:617`
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:617
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0917"></a>
### DTMAPI.Abstractions.InventoryDebugQuery

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:216`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0918"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.SearchText

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.SearchText`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:218`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0919"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.Category

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.Category`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:219`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0920"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.SourceId

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.SourceId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:220`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0921"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.ModItemsOnly

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.ModItemsOnly`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:221`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0922"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.IncludeUnavailable

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.IncludeUnavailable`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:222`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0923"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.Page

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.Page`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:223`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0924"></a>
### DTMAPI.Abstractions.InventoryDebugQuery.PageSize

- Symbol: `DTMAPI.Abstractions.InventoryDebugQuery.PageSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:224`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0925"></a>
### DTMAPI.Abstractions.InventoryDebugPage

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:227`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0926"></a>
### DTMAPI.Abstractions.InventoryDebugPage.Items

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.Items`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:229`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0927"></a>
### DTMAPI.Abstractions.InventoryDebugPage.Categories

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.Categories`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:230`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0928"></a>
### DTMAPI.Abstractions.InventoryDebugPage.Sources

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.Sources`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:231`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0929"></a>
### DTMAPI.Abstractions.InventoryDebugPage.Page

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.Page`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:232`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0930"></a>
### DTMAPI.Abstractions.InventoryDebugPage.PageSize

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.PageSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:233`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0931"></a>
### DTMAPI.Abstractions.InventoryDebugPage.TotalItems

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.TotalItems`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:234`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0932"></a>
### DTMAPI.Abstractions.InventoryDebugPage.TotalPages

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.TotalPages`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:235`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0933"></a>
### DTMAPI.Abstractions.InventoryDebugPage.Status

- Symbol: `DTMAPI.Abstractions.InventoryDebugPage.Status`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:236`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0934"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:239`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0935"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.Id

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:241`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0936"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.DisplayName

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:242`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0937"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.SourceKind

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.SourceKind`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:243`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0938"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.IsModSource

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.IsModSource`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:244`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0939"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.Count

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.Count`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:245`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0940"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.Enabled

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:246`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0941"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.EnablementKnown

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.EnablementKnown`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:247`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0942"></a>
### DTMAPI.Abstractions.InventoryDebugSourceGroup.WorkshopId

- Symbol: `DTMAPI.Abstractions.InventoryDebugSourceGroup.WorkshopId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:248`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0943"></a>
### DTMAPI.Abstractions.InventoryDebugItem

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:251`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0944"></a>
### DTMAPI.Abstractions.InventoryDebugItem.Id

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:253`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0945"></a>
### DTMAPI.Abstractions.InventoryDebugItem.DisplayName

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:254`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0946"></a>
### DTMAPI.Abstractions.InventoryDebugItem.ChineseName

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.ChineseName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:255`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0947"></a>
### DTMAPI.Abstractions.InventoryDebugItem.EnglishName

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.EnglishName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:256`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0948"></a>
### DTMAPI.Abstractions.InventoryDebugItem.Category

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.Category`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:257`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0949"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SubCategory

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SubCategory`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:258`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0950"></a>
### DTMAPI.Abstractions.InventoryDebugItem.Tags

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.Tags`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:259`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0951"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SearchText

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SearchText`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:260`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0952"></a>
### DTMAPI.Abstractions.InventoryDebugItem.MaxStack

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.MaxStack`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:261`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0953"></a>
### DTMAPI.Abstractions.InventoryDebugItem.CanSpawn

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.CanSpawn`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:262`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0954"></a>
### DTMAPI.Abstractions.InventoryDebugItem.CanGive

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.CanGive`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:263`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0955"></a>
### DTMAPI.Abstractions.InventoryDebugItem.RuntimeLoaded

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.RuntimeLoaded`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:264`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0956"></a>
### DTMAPI.Abstractions.InventoryDebugItem.IsModItem

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.IsModItem`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:265`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0957"></a>
### DTMAPI.Abstractions.InventoryDebugItem.CannotGiveReason

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.CannotGiveReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:266`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0958"></a>
### DTMAPI.Abstractions.InventoryDebugItem.HasIcon

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.HasIcon`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:267`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0959"></a>
### DTMAPI.Abstractions.InventoryDebugItem.IconAssetKey

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.IconAssetKey`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:268`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0960"></a>
### DTMAPI.Abstractions.InventoryDebugItem.IconPath

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.IconPath`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:269`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0961"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SourceKind

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SourceKind`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:270`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0962"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SourceModTitle

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SourceModTitle`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:271`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0963"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SourceId

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SourceId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:272`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0964"></a>
### DTMAPI.Abstractions.InventoryDebugItem.WorkshopId

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.WorkshopId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:273`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0965"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SourceEnabled

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SourceEnabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:274`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0966"></a>
### DTMAPI.Abstractions.InventoryDebugItem.SourceEnablementKnown

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.SourceEnablementKnown`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:275`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0967"></a>
### DTMAPI.Abstractions.InventoryDebugItem.RootPath

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.RootPath`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:276`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0968"></a>
### DTMAPI.Abstractions.InventoryDebugItem.ContentPath

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.ContentPath`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:277`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0969"></a>
### DTMAPI.Abstractions.InventoryDebugItem.LoadOrder

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.LoadOrder`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:278`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0970"></a>
### DTMAPI.Abstractions.InventoryDebugItem.RuntimeOrder

- Symbol: `DTMAPI.Abstractions.InventoryDebugItem.RuntimeOrder`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:279`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0971"></a>
### DTMAPI.Abstractions.InventoryGiveResult

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:282`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0972"></a>
### DTMAPI.Abstractions.InventoryGiveResult.Success

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:284`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0973"></a>
### DTMAPI.Abstractions.InventoryGiveResult.ItemId

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:285`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0974"></a>
### DTMAPI.Abstractions.InventoryGiveResult.DisplayName

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:286`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0975"></a>
### DTMAPI.Abstractions.InventoryGiveResult.RequestedCount

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.RequestedCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:287`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0976"></a>
### DTMAPI.Abstractions.InventoryGiveResult.BeforeCount

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.BeforeCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:288`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0977"></a>
### DTMAPI.Abstractions.InventoryGiveResult.AfterCount

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.AfterCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:289`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0978"></a>
### DTMAPI.Abstractions.InventoryGiveResult.GivenCount

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.GivenCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:290`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0979"></a>
### DTMAPI.Abstractions.InventoryGiveResult.FailureReason

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:291`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0980"></a>
### DTMAPI.Abstractions.InventoryGiveResult.Message

- Symbol: `DTMAPI.Abstractions.InventoryGiveResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:292`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.QueryItemProto/CanPlaceItem/TryPlaceInBackpack and native inventory tables.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0981"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:295`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0982"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.ItemId

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:297`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0983"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.Count

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.Count`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:298`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0984"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.EmailName

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.EmailName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:299`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0985"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.Content

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.Content`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:300`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0986"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.Sender

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.Sender`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:301`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0987"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.TemplateName

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.TemplateName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:302`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0988"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.SkipIfAlreadyOwned

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.SkipIfAlreadyOwned`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:303`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0989"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.PreventDuplicatePendingMail

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.PreventDuplicatePendingMail`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:304`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0990"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.RequireEnabledContentSource

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.RequireEnabledContentSource`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:305`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0991"></a>
### DTMAPI.Abstractions.MailItemDeliveryRequest.RequiredSourceId

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryRequest.RequiredSourceId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:306`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0992"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:309`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0993"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.Success

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:311`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0994"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.Sent

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.Sent`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:312`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0995"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.Skipped

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.Skipped`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:313`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0996"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.ItemId

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:314`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0997"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.DisplayName

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:315`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0998"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.RequestedCount

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.RequestedCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:316`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0999"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.BackpackCount

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.BackpackCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:317`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1000"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.PendingMailCount

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.PendingMailCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:318`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1001"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.EmailName

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.EmailName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:319`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1002"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.TemplateName

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.TemplateName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:320`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1003"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.SourceId

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.SourceId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:321`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1004"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.SourceEnabled

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.SourceEnabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:322`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1005"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.SourceEnablementKnown

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.SourceEnablementKnown`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:323`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1006"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:324`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1007"></a>
### DTMAPI.Abstractions.MailItemDeliveryResult.Message

- Symbol: `DTMAPI.Abstractions.MailItemDeliveryResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:325`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SendItemAsEmail plus native item query/count and unclaimed-mail state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260604-111533
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1008"></a>
### DTMAPI.Abstractions.WeatherDebugState

- Symbol: `DTMAPI.Abstractions.WeatherDebugState`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:328`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1009"></a>
### DTMAPI.Abstractions.WeatherDebugState.CurrentWeatherId

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.CurrentWeatherId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:330`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1010"></a>
### DTMAPI.Abstractions.WeatherDebugState.CurrentWeatherName

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.CurrentWeatherName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:331`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1011"></a>
### DTMAPI.Abstractions.WeatherDebugState.Year

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.Year`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:332`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1012"></a>
### DTMAPI.Abstractions.WeatherDebugState.Month

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.Month`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:333`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1013"></a>
### DTMAPI.Abstractions.WeatherDebugState.Day

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.Day`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:334`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1014"></a>
### DTMAPI.Abstractions.WeatherDebugState.Hour

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.Hour`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:335`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1015"></a>
### DTMAPI.Abstractions.WeatherDebugState.SeasonName

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.SeasonName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:336`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1016"></a>
### DTMAPI.Abstractions.WeatherDebugState.CurrentDayForecastWeatherIds

- Symbol: `DTMAPI.Abstractions.WeatherDebugState.CurrentDayForecastWeatherIds`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:337`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1017"></a>
### DTMAPI.Abstractions.WeatherDebugOption

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:340`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1018"></a>
### DTMAPI.Abstractions.WeatherDebugOption.Id

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:342`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1019"></a>
### DTMAPI.Abstractions.WeatherDebugOption.DisplayName

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:343`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1020"></a>
### DTMAPI.Abstractions.WeatherDebugOption.Description

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.Description`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:344`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1021"></a>
### DTMAPI.Abstractions.WeatherDebugOption.IsCurrent

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.IsCurrent`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:345`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1022"></a>
### DTMAPI.Abstractions.WeatherDebugOption.IsCurrentDayForecast

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.IsCurrentDayForecast`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:346`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1023"></a>
### DTMAPI.Abstractions.WeatherDebugOption.IsMalignant

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.IsMalignant`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:347`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1024"></a>
### DTMAPI.Abstractions.WeatherDebugOption.IsRainy

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.IsRainy`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:348`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1025"></a>
### DTMAPI.Abstractions.WeatherDebugOption.IsWindy

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.IsWindy`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:349`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1026"></a>
### DTMAPI.Abstractions.WeatherDebugOption.Sun

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.Sun`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:350`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1027"></a>
### DTMAPI.Abstractions.WeatherDebugOption.Water

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.Water`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:351`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1028"></a>
### DTMAPI.Abstractions.WeatherDebugOption.Wind

- Symbol: `DTMAPI.Abstractions.WeatherDebugOption.Wind`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:352`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1029"></a>
### DTMAPI.Abstractions.WeatherSetResult

- Symbol: `DTMAPI.Abstractions.WeatherSetResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:355`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1030"></a>
### DTMAPI.Abstractions.WeatherSetResult.Success

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:357`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1031"></a>
### DTMAPI.Abstractions.WeatherSetResult.WeatherId

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.WeatherId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:358`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1032"></a>
### DTMAPI.Abstractions.WeatherSetResult.DisplayName

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:359`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1033"></a>
### DTMAPI.Abstractions.WeatherSetResult.BeforeWeatherId

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.BeforeWeatherId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:360`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1034"></a>
### DTMAPI.Abstractions.WeatherSetResult.AfterWeatherId

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.AfterWeatherId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:361`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1035"></a>
### DTMAPI.Abstractions.WeatherSetResult.PatchedCurrentPeriod

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.PatchedCurrentPeriod`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:362`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1036"></a>
### DTMAPI.Abstractions.WeatherSetResult.FailureReason

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:363`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1037"></a>
### DTMAPI.Abstractions.WeatherSetResult.Message

- Symbol: `DTMAPI.Abstractions.WeatherSetResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:364`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.SetWeather/PatchWeather and TbWeather/current forecast data.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1038"></a>
### DTMAPI.Abstractions.TeleportDestination

- Symbol: `DTMAPI.Abstractions.TeleportDestination`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:367`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1039"></a>
### DTMAPI.Abstractions.TeleportDestination.Id

- Symbol: `DTMAPI.Abstractions.TeleportDestination.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:369`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1040"></a>
### DTMAPI.Abstractions.TeleportDestination.DisplayName

- Symbol: `DTMAPI.Abstractions.TeleportDestination.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:370`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1041"></a>
### DTMAPI.Abstractions.TeleportDestination.SuggestedDisplayName

- Symbol: `DTMAPI.Abstractions.TeleportDestination.SuggestedDisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:371`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1042"></a>
### DTMAPI.Abstractions.TeleportDestination.Group

- Symbol: `DTMAPI.Abstractions.TeleportDestination.Group`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:372`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1043"></a>
### DTMAPI.Abstractions.TeleportDestination.MarkPointId

- Symbol: `DTMAPI.Abstractions.TeleportDestination.MarkPointId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:373`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1044"></a>
### DTMAPI.Abstractions.TeleportDestination.RoomId

- Symbol: `DTMAPI.Abstractions.TeleportDestination.RoomId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:374`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1045"></a>
### DTMAPI.Abstractions.TeleportDestination.X

- Symbol: `DTMAPI.Abstractions.TeleportDestination.X`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:375`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1046"></a>
### DTMAPI.Abstractions.TeleportDestination.Y

- Symbol: `DTMAPI.Abstractions.TeleportDestination.Y`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:376`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1047"></a>
### DTMAPI.Abstractions.TeleportDestination.IsStation

- Symbol: `DTMAPI.Abstractions.TeleportDestination.IsStation`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:377`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1048"></a>
### DTMAPI.Abstractions.TeleportDestination.IsUnlocked

- Symbol: `DTMAPI.Abstractions.TeleportDestination.IsUnlocked`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:378`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1049"></a>
### DTMAPI.Abstractions.TeleportDestination.Source

- Symbol: `DTMAPI.Abstractions.TeleportDestination.Source`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:379`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1050"></a>
### DTMAPI.Abstractions.TeleportSnapshot

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:382`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1051"></a>
### DTMAPI.Abstractions.TeleportSnapshot.RoomId

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.RoomId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:384`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1052"></a>
### DTMAPI.Abstractions.TeleportSnapshot.RoomTitle

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.RoomTitle`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:385`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1053"></a>
### DTMAPI.Abstractions.TeleportSnapshot.RoomType

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.RoomType`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:386`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1054"></a>
### DTMAPI.Abstractions.TeleportSnapshot.X

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.X`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:387`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1055"></a>
### DTMAPI.Abstractions.TeleportSnapshot.Y

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.Y`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:388`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1056"></a>
### DTMAPI.Abstractions.TeleportSnapshot.Z

- Symbol: `DTMAPI.Abstractions.TeleportSnapshot.Z`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:389`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1057"></a>
### DTMAPI.Abstractions.TeleportResult

- Symbol: `DTMAPI.Abstractions.TeleportResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:392`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1058"></a>
### DTMAPI.Abstractions.TeleportResult.Success

- Symbol: `DTMAPI.Abstractions.TeleportResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:394`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1059"></a>
### DTMAPI.Abstractions.TeleportResult.DestinationId

- Symbol: `DTMAPI.Abstractions.TeleportResult.DestinationId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:395`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1060"></a>
### DTMAPI.Abstractions.TeleportResult.DestinationName

- Symbol: `DTMAPI.Abstractions.TeleportResult.DestinationName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:396`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1061"></a>
### DTMAPI.Abstractions.TeleportResult.MarkPointId

- Symbol: `DTMAPI.Abstractions.TeleportResult.MarkPointId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:397`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1062"></a>
### DTMAPI.Abstractions.TeleportResult.Before

- Symbol: `DTMAPI.Abstractions.TeleportResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:398`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1063"></a>
### DTMAPI.Abstractions.TeleportResult.AfterRequest

- Symbol: `DTMAPI.Abstractions.TeleportResult.AfterRequest`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:399`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1064"></a>
### DTMAPI.Abstractions.TeleportResult.FailureReason

- Symbol: `DTMAPI.Abstractions.TeleportResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:400`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1065"></a>
### DTMAPI.Abstractions.TeleportResult.Message

- Symbol: `DTMAPI.Abstractions.TeleportResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:401`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1066"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult`
- Current marker: `experimental`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:404`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1067"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult.Success

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:406`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1068"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult.Path

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult.Path`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:407`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1069"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult.RowCount

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult.RowCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:408`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1070"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult.FailureReason

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:409`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1071"></a>
### DTMAPI.Abstractions.TeleportCsvExportResult.Message

- Symbol: `DTMAPI.Abstractions.TeleportCsvExportResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:410`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.DoTransport for mark points and direct room/position transport where exposed.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:80 `ITeleportDebugApi.ExportDestinationsCsv` and `TeleportCsvExportResult` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1072"></a>
### DTMAPI.Abstractions.InstantSaveDebugState

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:413`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1073"></a>
### DTMAPI.Abstractions.InstantSaveDebugState.CanSave

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState.CanSave`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:415`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1074"></a>
### DTMAPI.Abstractions.InstantSaveDebugState.SaveSlot

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:416`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1075"></a>
### DTMAPI.Abstractions.InstantSaveDebugState.CurrentLocation

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState.CurrentLocation`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:417`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1076"></a>
### DTMAPI.Abstractions.InstantSaveDebugState.FailureReason

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:418`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1077"></a>
### DTMAPI.Abstractions.InstantSaveDebugState.Message

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugState.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:419`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1078"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:422`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1079"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.Success

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:424`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1080"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.ReloadAfterSave

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.ReloadAfterSave`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:425`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1081"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.ReloadRequested

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.ReloadRequested`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:426`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1082"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.SaveSlot

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:427`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1083"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.Before

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:428`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1084"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.AfterSave

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.AfterSave`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:429`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1085"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.AfterReloadRequest

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.AfterReloadRequest`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:430`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1086"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.FailureReason

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:431`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1087"></a>
### DTMAPI.Abstractions.InstantSaveDebugResult.Message

- Symbol: `DTMAPI.Abstractions.InstantSaveDebugResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:432`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.SaveGame / DataPersistenceManager.SaveGame native save transaction.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/hook-map/README.md#hook-save-savesaved
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1088"></a>
### DTMAPI.Abstractions.TimeDebugState

- Symbol: `DTMAPI.Abstractions.TimeDebugState`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:435`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1089"></a>
### DTMAPI.Abstractions.TimeDebugState.Year

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Year`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:437`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1090"></a>
### DTMAPI.Abstractions.TimeDebugState.Month

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Month`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:438`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1091"></a>
### DTMAPI.Abstractions.TimeDebugState.Day

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Day`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:439`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1092"></a>
### DTMAPI.Abstractions.TimeDebugState.Hour

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Hour`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:440`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1093"></a>
### DTMAPI.Abstractions.TimeDebugState.Minute

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Minute`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:441`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1094"></a>
### DTMAPI.Abstractions.TimeDebugState.CurrentWeatherId

- Symbol: `DTMAPI.Abstractions.TimeDebugState.CurrentWeatherId`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:442`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1095"></a>
### DTMAPI.Abstractions.TimeDebugState.CurrentWeatherName

- Symbol: `DTMAPI.Abstractions.TimeDebugState.CurrentWeatherName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:443`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1096"></a>
### DTMAPI.Abstractions.TimeDebugState.SeasonName

- Symbol: `DTMAPI.Abstractions.TimeDebugState.SeasonName`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:444`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1097"></a>
### DTMAPI.Abstractions.TimeDebugState.Period

- Symbol: `DTMAPI.Abstractions.TimeDebugState.Period`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:445`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1098"></a>
### DTMAPI.Abstractions.TimeSkipResult

- Symbol: `DTMAPI.Abstractions.TimeSkipResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:448`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1099"></a>
### DTMAPI.Abstractions.TimeSkipResult.Success

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:450`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1100"></a>
### DTMAPI.Abstractions.TimeSkipResult.Before

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:451`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1101"></a>
### DTMAPI.Abstractions.TimeSkipResult.After

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.After`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:452`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1102"></a>
### DTMAPI.Abstractions.TimeSkipResult.AdvancedGameMinutes

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.AdvancedGameMinutes`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:453`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1103"></a>
### DTMAPI.Abstractions.TimeSkipResult.AdvancedSeconds

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.AdvancedSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:454`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1104"></a>
### DTMAPI.Abstractions.TimeSkipResult.TargetHour

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.TargetHour`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:455`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1105"></a>
### DTMAPI.Abstractions.TimeSkipResult.FailureReason

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:456`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1106"></a>
### DTMAPI.Abstractions.TimeSkipResult.Message

- Symbol: `DTMAPI.Abstractions.TimeSkipResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:457`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1107"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:460`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1108"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.Success

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:462`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1109"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.RequestedMultiplier

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.RequestedMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:463`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1110"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.BeforeMultiplier

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.BeforeMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:464`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1111"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.AfterMultiplier

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.AfterMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:465`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1112"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.FailureReason

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:466`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1113"></a>
### DTMAPI.Abstractions.TimeScaleDebugResult.Message

- Symbol: `DTMAPI.Abstractions.TimeScaleDebugResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:467`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.PassTimeNoControl, DolocAPI.OnWakeUp, and selected debug time-scale hooks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1114"></a>
### DTMAPI.Abstractions.DebugValueResult

- Symbol: `DTMAPI.Abstractions.DebugValueResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:470`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1115"></a>
### DTMAPI.Abstractions.DebugValueResult.Success

- Symbol: `DTMAPI.Abstractions.DebugValueResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:472`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1116"></a>
### DTMAPI.Abstractions.DebugValueResult.ValueId

- Symbol: `DTMAPI.Abstractions.DebugValueResult.ValueId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:473`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1117"></a>
### DTMAPI.Abstractions.DebugValueResult.RequestedDelta

- Symbol: `DTMAPI.Abstractions.DebugValueResult.RequestedDelta`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:474`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1118"></a>
### DTMAPI.Abstractions.DebugValueResult.BeforeValue

- Symbol: `DTMAPI.Abstractions.DebugValueResult.BeforeValue`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:475`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1119"></a>
### DTMAPI.Abstractions.DebugValueResult.AfterValue

- Symbol: `DTMAPI.Abstractions.DebugValueResult.AfterValue`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:476`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1120"></a>
### DTMAPI.Abstractions.DebugValueResult.FailureReason

- Symbol: `DTMAPI.Abstractions.DebugValueResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:477`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1121"></a>
### DTMAPI.Abstractions.DebugValueResult.Message

- Symbol: `DTMAPI.Abstractions.DebugValueResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:478`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1122"></a>
### DTMAPI.Abstractions.DebugCommandResult

- Symbol: `DTMAPI.Abstractions.DebugCommandResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:481`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1123"></a>
### DTMAPI.Abstractions.DebugCommandResult.Success

- Symbol: `DTMAPI.Abstractions.DebugCommandResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:483`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1124"></a>
### DTMAPI.Abstractions.DebugCommandResult.CommandId

- Symbol: `DTMAPI.Abstractions.DebugCommandResult.CommandId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:484`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1125"></a>
### DTMAPI.Abstractions.DebugCommandResult.AffectedCount

- Symbol: `DTMAPI.Abstractions.DebugCommandResult.AffectedCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:485`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1126"></a>
### DTMAPI.Abstractions.DebugCommandResult.FailureReason

- Symbol: `DTMAPI.Abstractions.DebugCommandResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:486`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1127"></a>
### DTMAPI.Abstractions.DebugCommandResult.Message

- Symbol: `DTMAPI.Abstractions.DebugCommandResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:487`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1128"></a>
### DTMAPI.Abstractions.CropMaturityResult

- Symbol: `DTMAPI.Abstractions.CropMaturityResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:490`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1129"></a>
### DTMAPI.Abstractions.CropMaturityResult.Success

- Symbol: `DTMAPI.Abstractions.CropMaturityResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:492`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1130"></a>
### DTMAPI.Abstractions.CropMaturityResult.PlantBasinsVisited

- Symbol: `DTMAPI.Abstractions.CropMaturityResult.PlantBasinsVisited`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:493`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1131"></a>
### DTMAPI.Abstractions.CropMaturityResult.CropsMatured

- Symbol: `DTMAPI.Abstractions.CropMaturityResult.CropsMatured`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:494`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1132"></a>
### DTMAPI.Abstractions.CropMaturityResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CropMaturityResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:495`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1133"></a>
### DTMAPI.Abstractions.CropMaturityResult.Message

- Symbol: `DTMAPI.Abstractions.CropMaturityResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:496`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1134"></a>
### DTMAPI.Abstractions.CreativeModeState

- Symbol: `DTMAPI.Abstractions.CreativeModeState`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:499`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1135"></a>
### DTMAPI.Abstractions.CreativeModeState.Enabled

- Symbol: `DTMAPI.Abstractions.CreativeModeState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:501`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1136"></a>
### DTMAPI.Abstractions.CreativeModeState.RuntimeHooksInstalled

- Symbol: `DTMAPI.Abstractions.CreativeModeState.RuntimeHooksInstalled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:502`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1137"></a>
### DTMAPI.Abstractions.CreativeModeState.GeneratorRuntimeAvailable

- Symbol: `DTMAPI.Abstractions.CreativeModeState.GeneratorRuntimeAvailable`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:503`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1138"></a>
### DTMAPI.Abstractions.CreativeModeState.GeneratorItemId

- Symbol: `DTMAPI.Abstractions.CreativeModeState.GeneratorItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:504`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1139"></a>
### DTMAPI.Abstractions.CreativeModeState.LastMessage

- Symbol: `DTMAPI.Abstractions.CreativeModeState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:505`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1140"></a>
### DTMAPI.Abstractions.CreativeModeResult

- Symbol: `DTMAPI.Abstractions.CreativeModeResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:508`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1141"></a>
### DTMAPI.Abstractions.CreativeModeResult.Success

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:510`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1142"></a>
### DTMAPI.Abstractions.CreativeModeResult.Enabled

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:511`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1143"></a>
### DTMAPI.Abstractions.CreativeModeResult.Before

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:512`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1144"></a>
### DTMAPI.Abstractions.CreativeModeResult.After

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.After`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:513`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1145"></a>
### DTMAPI.Abstractions.CreativeModeResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:514`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1146"></a>
### DTMAPI.Abstractions.CreativeModeResult.Message

- Symbol: `DTMAPI.Abstractions.CreativeModeResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:515`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1147"></a>
### DTMAPI.Abstractions.TechPointDebugOption

- Symbol: `DTMAPI.Abstractions.TechPointDebugOption`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:518`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1148"></a>
### DTMAPI.Abstractions.TechPointDebugOption.Id

- Symbol: `DTMAPI.Abstractions.TechPointDebugOption.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:520`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1149"></a>
### DTMAPI.Abstractions.TechPointDebugOption.DisplayName

- Symbol: `DTMAPI.Abstractions.TechPointDebugOption.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:521`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1150"></a>
### DTMAPI.Abstractions.TechPointDebugOption.CurrentPoints

- Symbol: `DTMAPI.Abstractions.TechPointDebugOption.CurrentPoints`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:522`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1151"></a>
### DTMAPI.Abstractions.TechPointDebugOption.CurrentLevel

- Symbol: `DTMAPI.Abstractions.TechPointDebugOption.CurrentLevel`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:523`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1152"></a>
### DTMAPI.Abstractions.SpawnDebugOption

- Symbol: `DTMAPI.Abstractions.SpawnDebugOption`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:526`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1153"></a>
### DTMAPI.Abstractions.SpawnDebugOption.Id

- Symbol: `DTMAPI.Abstractions.SpawnDebugOption.Id`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:528`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1154"></a>
### DTMAPI.Abstractions.SpawnDebugOption.DisplayName

- Symbol: `DTMAPI.Abstractions.SpawnDebugOption.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:529`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1155"></a>
### DTMAPI.Abstractions.SpawnDebugOption.Category

- Symbol: `DTMAPI.Abstractions.SpawnDebugOption.Category`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:530`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1156"></a>
### DTMAPI.Abstractions.SpawnDebugOption.IsAvailableInCurrentRoom

- Symbol: `DTMAPI.Abstractions.SpawnDebugOption.IsAvailableInCurrentRoom`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:531`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1157"></a>
### DTMAPI.Abstractions.SpawnDebugResult

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:534`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1158"></a>
### DTMAPI.Abstractions.SpawnDebugResult.Success

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:536`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1159"></a>
### DTMAPI.Abstractions.SpawnDebugResult.SpawnId

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.SpawnId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:537`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1160"></a>
### DTMAPI.Abstractions.SpawnDebugResult.DisplayName

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:538`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1161"></a>
### DTMAPI.Abstractions.SpawnDebugResult.RequestedCount

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.RequestedCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:539`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1162"></a>
### DTMAPI.Abstractions.SpawnDebugResult.SpawnedCount

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.SpawnedCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:540`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1163"></a>
### DTMAPI.Abstractions.SpawnDebugResult.FailureReason

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:541`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1164"></a>
### DTMAPI.Abstractions.SpawnDebugResult.Message

- Symbol: `DTMAPI.Abstractions.SpawnDebugResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental/debug-only；普通 mod 不应依赖 save/global mutation wrapper
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:542`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Whitelisted debug owners: archive money/tech/time, crop state, creative hooks, official Command_GenerateMonster/resource generation.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-172855
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#yconsole-030-advanced
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会直接修改 save/global runtime；风险是存档污染、经济/科技/时间状态不可逆或与普通 gameplay 规则冲突。

<a id="sym-1165"></a>
### DTMAPI.Abstractions.MovementDebugState

- Symbol: `DTMAPI.Abstractions.MovementDebugState`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:545`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1166"></a>
### DTMAPI.Abstractions.MovementDebugState.Multiplier

- Symbol: `DTMAPI.Abstractions.MovementDebugState.Multiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:547`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1167"></a>
### DTMAPI.Abstractions.MovementDebugState.MoveSpeed

- Symbol: `DTMAPI.Abstractions.MovementDebugState.MoveSpeed`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:548`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1168"></a>
### DTMAPI.Abstractions.MovementDebugState.IsDefault

- Symbol: `DTMAPI.Abstractions.MovementDebugState.IsDefault`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:549`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1169"></a>
### DTMAPI.Abstractions.MovementDebugState.Source

- Symbol: `DTMAPI.Abstractions.MovementDebugState.Source`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:550`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1170"></a>
### DTMAPI.Abstractions.MovementSpeedResult

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:553`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1171"></a>
### DTMAPI.Abstractions.MovementSpeedResult.Success

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:555`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1172"></a>
### DTMAPI.Abstractions.MovementSpeedResult.RequestedMultiplier

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.RequestedMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:556`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1173"></a>
### DTMAPI.Abstractions.MovementSpeedResult.AppliedMultiplier

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.AppliedMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:557`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1174"></a>
### DTMAPI.Abstractions.MovementSpeedResult.Before

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:558`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1175"></a>
### DTMAPI.Abstractions.MovementSpeedResult.After

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.After`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:559`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1176"></a>
### DTMAPI.Abstractions.MovementSpeedResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:560`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1177"></a>
### DTMAPI.Abstractions.MovementSpeedResult.Message

- Symbol: `DTMAPI.Abstractions.MovementSpeedResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 debug-only/experimental；不要写入普通作者稳定文档
- Ordinary mod usability: debug-only
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:561`
- Implementation: No runtime implementation; data contract member only.
- Native owner: MotionAbility.SetMoveScaler and player movement ability state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150210
  - hook-map entry: docs/debug/regressions/smoke-matrix.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1564"></a>
### DTMAPI.Abstractions.BridgeFeatureStatus

- Symbol: `DTMAPI.Abstractions.BridgeFeatureStatus`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1061`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1565"></a>
### DTMAPI.Abstractions.BridgeFeatureStatus.BridgeFeatureStatus(string status, string details)

- Symbol: `DTMAPI.Abstractions.BridgeFeatureStatus.BridgeFeatureStatus(string status, string details)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1063`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1566"></a>
### DTMAPI.Abstractions.BridgeFeatureStatus.Status

- Symbol: `DTMAPI.Abstractions.BridgeFeatureStatus.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1069`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1567"></a>
### DTMAPI.Abstractions.BridgeFeatureStatus.Details

- Symbol: `DTMAPI.Abstractions.BridgeFeatureStatus.Details`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1070`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1568"></a>
### DTMAPI.Abstractions.DtmColor

- Symbol: `DTMAPI.Abstractions.DtmColor`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1073`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1569"></a>
### DTMAPI.Abstractions.DtmColor.DtmColor(double r, double g, double b, double a)

- Symbol: `DTMAPI.Abstractions.DtmColor.DtmColor(double r, double g, double b, double a)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1075`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1570"></a>
### DTMAPI.Abstractions.DtmColor.R

- Symbol: `DTMAPI.Abstractions.DtmColor.R`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1083`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1571"></a>
### DTMAPI.Abstractions.DtmColor.G

- Symbol: `DTMAPI.Abstractions.DtmColor.G`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1084`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1572"></a>
### DTMAPI.Abstractions.DtmColor.B

- Symbol: `DTMAPI.Abstractions.DtmColor.B`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1085`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1573"></a>
### DTMAPI.Abstractions.DtmColor.A

- Symbol: `DTMAPI.Abstractions.DtmColor.A`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1086`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.
