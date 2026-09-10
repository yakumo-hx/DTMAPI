# DTMAPI 底层 API 原生职责可信度审查

- 日期: 2026-06-07
- 记录: `20260607-0005-native-responsibility-api-audit`
- 类型: code-level review, durable docs only
- 目标: 审查当前 DTMAPI 低层/公共 API 是否真正抵达 Doloc Town 原生职责函数，而不是只证明 UI、日志、hook 安装或 smoke 成功。
- 约束: 本次不修 bug、不加功能、不 bump 版本、不改游戏目录、不重构 runtime、不创建实施 goal。
- 验证: 未运行 build、未启动游戏、未运行第三存档 smoke。本文是源码与既有证据审查。

## 已读上下文

- 项目/规划: `PROJECT.md`, `docs/planning/DolocTownModdingAPI.md`, `docs/planning/Debug.md`
- 规则/索引: `references/README.md`, `docs/debug/INDEX.md`, `docs/reviews/README.md`, `docs/api/public-api-matrix.md`
- workflow: `docs/workflows/codex-feedback-to-goal.md`, `docs/goals/README.md`
- native map: `Save_Load.md`, `Input.md`, `Items_Inventory.md`, `Action_Interaction.md`, `Fishing.md`, `Motor.md`, `Crops.md`, `Buildings.md`, `ModManager_Workshop.md`, `GameLoop_Scene.md`, `Assets_Content.md`, `Recipe_Crafting.md`, `UI.md`
- 近期研究: `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`, `research-DolocPlus-function-map-20260607.md`, `research-DolocPlus-deep-dive-20260607.md`
- 主要代码入口: `src/DTMAPI.Abstractions/*`, `src/DTMAPI.Core/*`, `src/DTMAPI.GameBridge.DolocTown/*`, `src/DTMAPI.BepInExBootstrap/*`, `src/DTMAPI.ModConfigMenu/*`

## 评级口径

- A: 清楚抵达原生职责函数或 DTMAPI 自有职责边界清楚，副作用已理解，并有真实第三存档结果证据。
- B: 大概率抵达原生职责函数，但缺少一个关键场景、边界或跨房间/重载证据。
- C: 主要证据还是 UI、日志、hook、smoke 或局部 native 调用，原生结果没有完整证明。
- D: 职责函数未知，或存在存档污染、共享状态、跨存档、跨 mod 冲突等高风险。

对纯 DTMAPI 框架 API，本文不强行要求 Doloc native 函数；评级关注“自有职责是否明确、是否冒充 native gameplay 能力”。对 GameBridge/API mod 能力，评级关注是否进入 Doloc Town 的真实责任函数与状态 owner。

## 覆盖清单

本文覆盖 `public-api-matrix` 中的当前 API/API family:

- 框架层: `IDtmHelper`, `ITranslationHelper`, `IConfigHelper`, `IModRegistry`, `IWorkshopHelper`, `IUiHelper`, `IDiagnosticsHelper`, `IContentQueryHelper`, `IInputHelper`
- 事件层: `IGameLoopEvents`, `IInputEvents`, `ISaveEvents`, `IUiEvents`, `IWorkshopEvents`, `IDiagnosticsEvents`
- 配置菜单: `IDtmConfigMenuApi`, `IConfigMenuPage`, `IConfigMenuPendingPreview`, `IConfigMenuItem`
- GameBridge/实验 API: `IActionCompletionApi`, `IActionSpeedApi`, `IFishingAutomationApi`, `IItemTooltipApi`, `IAnimalViewerApi`, `IDebugConsoleApi`, `IInventoryDebugApi`, `IMailDeliveryApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, `IInstantSaveDebugApi`, `ITimeDebugApi`, `IMovementDebugApi`, `IMotorVehicleApi`, `IMachineProductionApi`, `IEquipmentSlotsApi`, `ISaveSlotsApi`, `ICameraZoomApi`, `IChestLocatorEnhancerApi`, `IStrongPlantingGunApi`, `IAdvancedDebugApi`
- 0.4.0 自定义实体: `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, `ICustomDroneApi`

## 总体结论

当前 DTMAPI 的底层 API 可以分成三类:

| 类别 | 代表 API | 可信度 | 结论 |
| --- | --- | --- | --- |
| DTMAPI 自有框架职责 | Config, Translation, Diagnostics, ModRegistry, helper/event 容器 | A/B | 可以继续作为框架 API，但文档应明确它们不是 Doloc 原生 gameplay API。 |
| 已触达原生职责函数但证据未覆盖全部场景 | Save/Workshop events, ActionCompletion, ActionSpeed, Inventory give, Weather, Teleport, InstantSave, Movement, ChestLocator, StrongPlantingGun, SaveSlots | B/C | 适合保留实验或受限 public API；需要按场景补证据。 |
| 自有运行循环或状态替代原生 owner | MachineProduction, EquipmentSlots, CameraZoom, SecondMotor, CustomEntity runtime creation, 部分 AdvancedDebug | C/D | 不应给普通 mod 承诺稳定 native 语义；应降级、标注实验，或先重做 GameBridge/原生职责归属。 |

最大问题不是“没有 hook”，而是部分 API 把“已能看到 UI/日志/烟测结果”当成“原生职责认可”。例如相机缩放只写 `Camera.orthographicSize`，没有同步 `CameraController.camSize`、背景和 depth fog；机器生产有官方 JSON/tech/recipe 注入，但生产循环与输出状态仍由 DTMAPI 管理；自定义实体注册表稳定，但运行时 native 创建明确被阻断。

## A. Baseline

| 审查项 | 结论 |
| --- | --- |
| public-api-matrix 状态 | 当前矩阵能列出 API surface，但状态标签多以版本/ smoke 为中心，缺少“native responsibility owner”列。 |
| 当前代码入口 | `DtmApiRuntime` 注册 helper/events/runtime APIs；`DolocTownGameBridge.RegisterExperimentalApis` 注册 GameBridge APIs；`BootstrapPlugin` 注册 `IDebugConsoleApi`。 |
| GameBridge 边界 | Harmony/反射集中在 `DTMAPI.GameBridge.DolocTown`，方向正确。问题是部分桥接 API 的 public 语义比 native 证据更宽。 |
| native map 结论 | 原生 owner 通常是 `DolocAPI`, `ArchiveDataHandle`, `DataPersistenceManager`, `DolocInputSource`, `ModManager`, `DolocConfig.Tables`, `Room/Equipment/Crop/Motor` 等。 |
| 风险基线 | “视觉/UI可见”不能替代原生状态 owner 改变；“hook installed”不能替代 native responsibility function 完成；“第三存档 smoke”也必须标明 rendered/no-render/current-room/offline-room 范围。 |

## B. Framework APIs

字段顺序: API/状态; matrix evidence; 当前代码入口; GameBridge/hook 入口; native 职责函数; native state owner; 是否抵达; 风险摘要; 评级; 下一步。

| API family | 审查 |
| --- | --- |
| `IDtmHelper` / helper root, 当前框架稳定 | Matrix: base helper surface。代码: `DtmHelper` 聚合 Manifest/Monitor/Events/Config/Registry/Workshop/UI/Diagnostics/Content/Input/Translation。GameBridge: 无。Native: 无直接 Doloc gameplay 职责。Owner: DTMAPI Core。抵达: N/A, 自有职责清楚。风险: 成功观感来自 DTMAPI 容器；无存档污染；render/no-render 无差异；多 mod 只受各 helper 自身限制影响；用户层低风险；开发者风险是误以为 helper 本身代表 native 能力。评级: A(DTMAPI-owned)。下一步: 保持，文档中继续区分 helper 与 native API。 |
| `ITranslationHelper` / 本地 i18n helper | Matrix: localization helper。代码: `TranslationService` 读 mod `i18n/{language}.json` 和 `english.json` fallback。GameBridge: 无。Native: 不调用 `DolocAPI.CurrentL10nInfo` 或官方 `TbStaticText`。Owner: DTMAPI/mod 文件。抵达: 未抵达 Doloc native localization owner。风险: UI 显示成功不代表官方本地化表已扩展；无 save 污染；render 只影响 DTMAPI UI；多 mod 文件隔离；用户层低；开发者可能误用作官方文本注入。评级: A for DTMAPI UI, C for native-content localization。下一步: 保持框架 helper；如果未来支持官方 UI 文本，另开 native localization bridge。 |
| `IConfigHelper` / JSON config | Matrix: config read/write。代码: `ConfigService` 写 `Paths.ConfigPath/{UniqueID}.json`，支持迁移 callback。GameBridge: 无。Native: 不触达 Doloc user settings。Owner: DTMAPI config dir。抵达: N/A。风险: 写文件成功不等于游戏原生配置改变；无 game save 污染；render 由 menu 读取；多 mod 按 UniqueID 分文件；用户层低；开发者风险是 migration 自动写回。评级: A(DTMAPI-owned)。下一步: 保持；重要 runtime config 仍需目标 API 自己证明生效。 |
| `IDtmConfigMenuApi` / DTMAPI config menu | Matrix: config menu APIs。代码: `ConfigMenuRegistry`, `ReflectedTitleMenuSettingsUi`, `ReflectedImGuiOverlay`。GameBridge: DTMAPI UI/IMGUI/Unity UI，非 Doloc 原生设置页。Native: 只借 title/settings UI 可视入口，不是官方配置系统。Owner: DTMAPI UI state。抵达: 未抵达 Doloc settings owner。风险: 看起来在标题设置中成功，但只是 DTMAPI 自绘/反射 UI；无 save 污染，可能有 pending preview/stale-state；render/title/in-save 差异明显；多 mod 有 keybind conflict 检测但缺少全局仲裁；用户层中风险，尤其 flicker/stale input；开发者风险是把 pending preview 当保存。评级: C。下一步: 保持实验/DTMAPI-owned 标记；反复 UI/flicker 反馈必须先 root-cause。 |
| `IModRegistry` / loaded/API registry | Matrix: mod registry。代码: `ModRegistryService` 以 `UniqueID|typeof(TApi).FullName` 存单 provider。GameBridge: 无。Native: 不触达 official ModManager API owner。Owner: DTMAPI Core loaded list。抵达: N/A for DTMAPI registry。风险: API 获取成功不代表官方 mod enabled/load-order 完全一致；无 save 污染；render 无关；多 provider/version negotiation 缺失；用户层低；开发者层中高，可能覆盖同一 API provider。评级: B。下一步: 增加版本/能力查询、冲突诊断、provider ownership 规则。 |
| `IWorkshopHelper` / official mod index view | Matrix: Workshop helper/events。代码: `WorkshopService` 包装 `DiscoveredMod`，`ModScanner`/`OfficialModEnablementIndex` 提供 enablement。GameBridge: `ModManager.ReloadMods` postfix 调 `NotifyWorkshopModListChanged`。Native: `DolocTown.Config.ModManager.ReloadMods` 是刷新 hook，但 helper 本身不调用 Steam UGC 或官方 enable/disable writes。Owner: DTMAPI discovered list + official enablement files。抵达: 部分抵达 reload 观察点，不拥有官方状态。风险: “已发现/已热加载”不等于官方 ModManager 完整缓存一致；无 save 污染；title/in-save reload 差异；多 mod load-order/source conflict 未完全仲裁；用户层中；开发者可能误以为 DTMAPI 可切换 Workshop。评级: B/C。下一步: 文档明确 read-only/observational；toggle/update 能力不得承诺。 |
| `IContentQueryHelper` / read-only content index | Matrix: content query。代码: `ContentQueryService.Rebuild` 扫描 json/png/txt/csv，索引 official local/Workshop item JSON。GameBridge: 间接被 Inventory/Mail 使用。Native: 查询阶段不进 `DolocConfig.Tables`；给物品时才查 `DolocAPI.QueryItemProto`。Owner: DTMAPI index。抵达: 查询本身未抵达 native loaded table。风险: 文件存在和 source enabled 不代表 runtime loaded；无 save 污染；render/no-render 无关；多 mod 重名 item 用排序取一条，冲突解释不足；用户层中；开发者易把 index 当实际 runtime table。评级: B for read-only index, C for native content truth。下一步: 在 API 返回中持续暴露 runtime-loaded/source-enabled/duplicate conflict。 |
| `IInputHelper` + `IInputEvents` / key state/events | Matrix: input helper/events。代码: `InputService`, `DtmApiRuntime.RecordInputPressed/Released`。GameBridge: bootstrap raw/reflected Unity key polling；Y-console 使用 `AgentControllerState.UseTool/UseItem/EnterUICheck` 隔离。Native: 未统一接入 `DolocInputSource.NormalInputActions` / generated action callbacks。Owner: DTMAPI key sets。抵达: 对通用 input 未抵达 native action owner；对 Y-console 隔离有局部 native hooks。风险: “按键可用”不代表 Doloc action binding/device/rebind 正确；无 save 污染；UI open 时 blocks mod updates/hotkeys；render 无关；多 mod key conflict 只在 config menu 局部处理；用户层中；开发者层高，尤其 raw key 名与官方 rebind。评级: C。下一步: 未来应基于 `DolocInputSource` action name 做 native input bridge。 |
| `IUiHelper` + `IUiEvents` / DTMAPI overlay state | Matrix: UI helper/events。代码: `UiRuntimeService`, `EventsService` dispatch。GameBridge: `RefreshUiContext` 读 `DolocAPI.IsNormalState` 与 active UI states。Native: 只是查询/避让 native UI，不进入官方 UI owner。Owner: DTMAPI UI service。抵达: 部分感知 native state，不控制 native UI。风险: overlay open/closed 成功不代表官方 panel lifecycle；无 save 污染；title/in-save/hidden-panel 差异大；多 mod 自绘菜单冲突未仲裁；用户层中；开发者需处理 BlocksModUpdates。评级: B/C。下一步: 保持 DTMAPI-owned，所有 native panel API 另列实验。 |
| `IDiagnosticsHelper` + `IDiagnosticsEvents` / logs, hook status, report | Matrix: diagnostics。代码: `DiagnosticsService`, `DtmApiRuntime.SetHookStatus/ExportLogs`。GameBridge: 各 hook/API 写状态。Native: 读 BepInEx/Unity logs 文件，但不改 native state。Owner: DTMAPI diagnostics。抵达: N/A。风险: hook status “verified”可能被 API 误用为 native result proof；无 save 污染；render 无关；多 mod errors 可聚合但无优先级冲突；用户层低；开发者最大风险是把 status 文本当正式证据。评级: A for logging, C as gameplay proof。下一步: hook status 应增加 `evidenceScope/nativeOwner` 字段。 |
| `IGameLoopEvents` / GameLaunched, UpdateTicked, OneSecond, ReturnedToTitle | Matrix: game loop events。代码: `DtmApiRuntime.Start/Update/NotifyReturnedToTitle`。GameBridge: `DolocTownGameBridge.Initialize` 状态；`DolocAPI.ReturnHome` postfix。Native: ReturnedToTitle 抵达 `DolocAPI.ReturnHome`；Update/OneSecond 是 BepInEx/Core tick。Owner: DTMAPI event service + Doloc title transition。抵达: 部分。风险: Update tick 成功不代表 native TU/FixedUpdate simulation；无 save 污染；pause/menu 状态会 block mod updates；多 mod dispatch 顺序未强约束；用户层低；开发者可能用它驱动 native simulation。评级: B。下一步: 对 time-sensitive gameplay 提供原生 TU/room/equipment events 前不要替代 native update。 |
| `ISaveEvents` / load/save/title lifecycle | Matrix: save events。代码: `DtmApiRuntime.NotifyLoadGameRequested/NotifySaveLoaded/NotifySaveSaving/NotifySaveSaved/NotifyReturnedToTitle`。GameBridge: `DolocAPI.LoadGame`, `DolocAPI.AfterLoadArchiveData`/`OnAfterLoadArchiveData`, `DolocAPI.SaveGame` 或 `DataPersistenceManager` fallback, `DolocAPI.ReturnHome`。Native: 保存/读取 owner 为 `ArchiveDataHandle`, `DataPersistenceManager`, `LocalSave`。抵达: 是，事件层抵达关键 native lifecycle。风险: 事件触发成功不代表所有 sidecar/state 已恢复；save pollution 来自订阅者；new game/reload-after-save/failed-save 场景缺口；render 无关；多 mod 顺序不可控；用户层中；开发者需避免保存中写共享状态。评级: B。下一步: 为 save transaction/sidecar APIs 补 clean restart、slot delete/copy、failed save 证据。 |
| `IWorkshopEvents` / mod list changed | Matrix: Workshop events。代码: `NotifyWorkshopModListChanged` 重新 Discover/LoadMods。GameBridge: `ModManager.ReloadMods` postfix。Native: `DolocTown.Config.ModManager.ReloadMods`。Owner: official ModManager + DTMAPI loader。抵达: 是，观察点抵达；热加载仍由 DTMAPI。风险: UI reload 成功不等于 managed DLL 卸载/禁用生效；无 save 污染；title/in-save 差异；多 mod dependencies/hot-load order 风险；用户层中；开发者风险是假设 disabled mod 已卸载。评级: B/C。下一步: 标明 restart-required 场景，避免承诺 unload。 |

## C. Content And Workshop APIs

| API family | 审查 |
| --- | --- |
| Official/Workshop content visibility | Matrix: content/workshop helper。代码: `OfficialModEnablementIndex`, `ContentQueryService`, `WorkshopService`。GameBridge: `ModManager.ReloadMods` postfix。Native: `ModManager.GetAllEnabledModInfos`, `LoadWithMods`, `CachedConfigs/CachedSprites` 是 native map 候选，但当前 DTMAPI 多为文件扫描。Owner: official path files + DTMAPI cache。抵达: 只在 reload observer 层抵达。风险: 文件扫描“看见”不等于 runtime table 合并；无 direct save 污染；render 无关；多作者 item ID/recipe extension 冲突风险高；用户层中；开发者需先看 `RuntimeLoaded`。评级: C。下一步: 把 content query 继续定位为 read-only diagnostic；不要扩展成 Content Patcher 语义前先证明 `DolocBundleManager/ModManager.LoadWithMods`。 |
| Item/source index used by debug APIs | Matrix: inventory/content APIs。代码: `GetIndexedItems`, `GetIndexedItem`, Inventory debug `EnumerateInventoryDebugItems`。GameBridge: 给物品/邮件前转入 `DolocAPI.QueryItemProto`。Native: `DolocConfig.Tables.TbItem`, `DolocAPI.QueryItemProto`, `TryPlaceInBackpack`, `SendItemAsEmail`。Owner: native item table when used, DTMAPI index when listed。抵达: list 是 C，give/mail 是 B。风险: UI 列表看似可给但 runtime 或 source state 可能不同；give/mail 污染当前 save；render 无关；多 mod duplicate ID 只取优先项；用户层中；开发者风险是 source filters 不等同 official load order。评级: B/C。下一步: 列表中继续显示 source/runtime/load-order/duplicate 信息，普通 mod 不要依赖 debug list。 |

## D. Debug Console And User Ops

| API family | 审查 |
| --- | --- |
| `IDebugConsoleApi` / Y-console host | Matrix: debug console API。代码: `ReflectedDebugConsoleUi`, `BootstrapPlugin.RegisterRuntimeApi<IDebugConsoleApi>`。GameBridge: `DolocTownHookCallbacks.DebugConsoleModalOpen`, input isolation hooks。Native: Unity Canvas/EventSystem, not official console; official console map shows `ConsoleSystem`/`DolocAPI.ExecuteCommand` but current host does not expose raw exec。Owner: DTMAPI UI。抵达: UI 不抵达 native console owner；其按钮调用的 API 单独评级。风险: 打开/关闭成功只是 DTMAPI Canvas；无 save 污染本身；title/save boundary 会重置 filters；多 mod 仅一个 host；用户层中高，曾有 stale search/right-click/input leakage；开发者不应用作普通 gameplay UI。评级: C。下一步: 保持 debug-only；若接官方 console metadata，只做 whitelisted command wrappers。 |
| `IInventoryDebugApi` / list and give item | Matrix: Y-console inventory。代码: `DolocTownExperimentalBridgeApi.GetItems/GiveItem`。GameBridge: 无 Harmony，直接反射 `DolocAPI`。Native: `DolocAPI.QueryItemProto`, `CanPlaceItem`, `TryPlaceInBackpack`, `CountItem`; owner 为 `ArchiveDataHandle` backpack/native item table。抵达: GiveItem 是；GetItems 只是 index+table。风险: UI 给物品成功是 native placement 认可；但会污染当前 save；full inventory/overflow/Workshop disabled 已有 guard 但仍是 debug；render 无关；多 mod duplicate/source risk 中；用户层高，因为直接改存档；开发者不应普通 mod 调用。评级: B。下一步: 保持 debug-only，补不同背包满/邮件 overflow/source duplicate 证据。 |
| `IMailDeliveryApi` / item mail | Matrix: mail delivery。代码: `SendItemMail`。GameBridge: direct reflection。Native: `DolocAPI.SendItemAsEmail`, `EmailManager.emails`, `QueryItemProto`, `CountItem`。Owner: native email/save state。抵达: 是。风险: native mail pending 成功但污染 save；template/source gating 不等于完整 mail content authoring；render 取决于 mail UI；多 mod duplicate mail/emailName risk；用户层高；开发者应仅用于 controlled delivery。评级: B。下一步: 补跨天/读信/重复邮件/disabled source 证据。 |
| `IWeatherDebugApi` | Matrix: weather debug。代码: `GetState/GetAvailableWeathers/SetWeather`。GameBridge: direct reflection。Native: `ArchiveDataHandle.SetWeather`, `PatchWeather`, `TbWeather`, `timeData`。Owner: `ArchiveDataHandle` weather/time data。抵达: 是。风险: set 成功不证明 day-period forecast/weather renderer 全刷新；修改当前 save/time state；rendered/weather effect 与 data state 可能不同；多 mod 无仲裁；用户层高；开发者不应当作 stable weather API。评级: B。下一步: 补 current-period、next-period、reload 后天气和 weather renderer evidence。 |
| `ITeleportDebugApi` | Matrix: teleport debug。代码: `GetDestinations/GetCurrentSnapshot/Teleport/ExportDestinationsCsv`。GameBridge: direct `DolocAPI.DoTransport`。Native: `DolocAPI.DoTransport`, mark points/stations from room config。Owner: scene/room transition system。抵达: Teleport request 是 native accepted。风险: request accepted 不代表 callback complete/after room final state；save 不直接污染但可能改变 current scene; rendered/no-render room transition 差异；multi mod low；用户层高，可能卡转场；开发者不应写任意坐标。评级: B。下一步: 证据应记录 callback/after-load room, not only immediate `AfterRequest`。 |
| `IInstantSaveDebugApi` | Matrix: instant save。代码: `Save(reloadAfterSave)`。GameBridge: direct `DolocAPI.SaveGame`，明确禁止 save-then-load。Native: `DolocAPI.SaveGame`, `DataPersistenceManager.SaveGame`。Owner: native save files/current slot。抵达: 是。风险: 直接写当前 save，污染高；reload path 已知 residue 禁用；render 无关；多 mod save-time subscribers 顺序风险；用户层高；开发者不可普通 mod 自动调用。评级: B。下一步: 保持 save-only debug；补失败/prev/bak/slot copy evidence。 |
| `ITimeDebugApi` | Matrix: time debug。代码: `SkipToNextWeatherPeriod`。GameBridge: direct `ArchiveDataHandle.PassTimeNoControl` + `DolocAPI.OnWakeUp`。Native: time/weather period owner 为 archive/time systems。抵达: 是。风险: 时间推进成功不证明 equipment/crops/animals/renderers/offline rooms 全部正确 update；会污染 save；rendered/no-render/current/offline 差异很大；多 mod 无时间跳转仲裁；用户层高；开发者不应作为 production API。评级: C。下一步: 对机器、作物、天气、动物分别补 rendered/no-render/offline evidence。 |
| `IMovementDebugApi` | Matrix: movement debug。代码: `SetSpeedMultiplier/ResetSpeed`。GameBridge: direct `MotionAbility.SetMoveScaler`。Native: player motion owner。抵达: 是。风险: 运行时状态不一定随场景/坐骑/死亡重置；无 save 污染；render/current scene only；multi mod multiplier conflict；用户层中；开发者风险是叠乘/覆盖。评级: B。下一步: 增加 owner stack 或 conflict policy，补 scene transition reset evidence。 |
| `IAdvancedDebugApi` safe wrappers | Matrix: advanced Y-console。代码: `AdvanceTime`, `SetTimeScale`, `AddMoney`, `AddTechPoint`, `UnlockAllTechTrees`, `MatureAllCrops`, `SetCreativeMode`, `GiveCreativeGenerator`, `SpawnMonster`, `SpawnResource`。GameBridge: direct native calls plus creative Harmony prefixes/postfixes。Native: `PassTimeNoControl`, `SetTimeScale/RevertTimeScale`, `Command_AddMoney` or `ArchiveDataHandle.CurrentMoney`, `AddTechPoint`, save collections, `PlantBasin.Crop.DEBUG_SetLevel`, `GameInitConfig` debug flags, `Command_GenerateMonster`, `IDungeonResourceHost.CreateDungeonResourceNoRender`。Owner: multiple native systems, sometimes direct save collection。抵达: mixed。风险: 很多成功是 destructive/debug save mutation；rendered/no-render/room host checks incomplete；creative no-cost hooks can affect global gameplay; multi mod conflict high; user-layer very high; developer API risk very high。评级: B for whitelisted direct native commands, C/D for collection writes/creative/global bypass。下一步: 全部保持 debug-only；普通 mod 不可依赖。 |

## E. Migrated Mod GameBridge APIs

| API family | 审查 |
| --- | --- |
| `IActionCompletionApi` / OneActionComplete | Matrix: migrated mod。代码: `Configure(ActionCompletionOptions)`, hook callbacks around `ToolCollider.HandleTools`。GameBridge: `ToolCollider.HandleTools` prefix/postfix。Native: resource/tool hit responsibility, `ResourceFellData`, native consume/fill paths for fuel/feeder; vegetation has `VegetationRenderer.OnFell` exception path。Owner: resource/tool interaction systems。抵达: 对已 smoke 的 resource/tool-hit、wrong-tool、fuel/feed path 抵达。风险: “one action”不是所有 action 类型；save pollution 来自 resource changes；rendered/current-room 已证，offline/no-render 不适用或未证；multi mod patch/policy conflict 中；用户层中；developer risk 是扩展到未验证 interaction。评级: B。下一步: 保留实验，按 action type 增加 owner map 与 conflict policy。 |
| `IActionSpeedApi` / ActionSpeed | Matrix: migrated mod。代码: `Configure(ActionSpeedOptions)`, `TryGetConfiguredActionSpeed*`。GameBridge: `AgentStateTool/Interact/Eat`, `AgentControllerState.UseItemContinues`, `AgentStateBase.OnExit` 等。Native: agent state/animation/use item state owners。抵达: 多个 tool/interaction/eat/drink/fill/plant/harvest slices 抵达 native state hooks。风险: speed 调整可能只是 animation/runtime scalar，不一定改变每个 native completion responsibility；save pollution 来自动作本身；rendered/current-room 为主；multi mod multiplier conflict 高；用户层中；developer risk 是认为所有交互都统一提速。评级: B。下一步: 建立每个 option -> native responsibility function 的表，限制多个 speed provider。 |
| `IFishingAutomationApi` / AutoFishing | Matrix: migrated mod。代码: `Configure`, `SetEnabled`, `GetState`。GameBridge: `AgentStateFishingReady/Cast/Wait/Pull`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, input hooks。Native: fishing state machine, `BodyController.UseFishRod`, `FishingGameScrollBar` minigame。Owner: fishing agent states/minigame。抵达: wait-phase instant bite 和 skip=false minigame 有证据；完整 automation policy 仍宽。风险: 自动成功可能绕过 minigame/native input acknowledgement；save pollution 来自 fish catch；rendered/current room water only；multi mod fishing controller conflict 高；用户层中高；developer risk 是状态机 race。评级: B/C。下一步: 每种 mode 需记录 state transition chain 和 catch result, 保持 experimental。 |
| `IItemTooltipApi` / fish roe tooltip | Matrix: migrated tooltip。代码: `ConfigureFishRoeProvider`。GameBridge: `Item.get_title`, `get_description`, `GetDetailInfo` postfix。Native: item display getters。Owner: item UI text/render data。抵达: display getters 抵达；不改 native item data。风险: UI 看起来成功但 native item stats/save 无变化；无 save pollution；render only; multi mod tooltip ordering/conflict 高；用户层低；developer risk 是多 provider覆盖/重复文本。评级: C。下一步: 加 tooltip composition/order API，保留显示层语义。 |
| `IAnimalViewerApi` / animal produce/progress viewer | Matrix: migrated animal viewer。代码: `ConfigureSpecialProduceProgress`。GameBridge: `AnimalFullInfoData` ctor, `AnimalViewer.Show`, `AnimalPanel.RefreshViewer`。Native: animal UI data construction, not produce generation owner。Owner: native UI data + DTMAPI options。抵达: UI 数据层抵达；不抵达 animal production lifecycle。风险: 视觉行成功不代表特殊产物状态 owner 已变更；无 save pollution；rendered UI only; animal panel refresh/flicker 历史风险；multi mod UI row conflict 中；用户层中；developer risk 是把 viewer 当 production API。评级: C。下一步: 明确只读 UI API；任何 animal production API 需另找 `Animal`, `AnimalAI`, `GetAnimalAllProduce` owner。 |

## F. Machines, Equipment, Items

| API family | 审查 |
| --- | --- |
| `IMachineProductionApi` / custom Mine machine | Matrix: new content/machine API。代码: `RegisterMachine`, `EnsureNativeMachineRecipeInputs`, `EnsureNativeMachineTechRoute`, DTMAPI runtime loop/state。GameBridge: machine runtime loop hooks; native recipe/tech reflection injection。Native: partial `DolocConfig.Tables.TbRecipe`, tech tree graph, item/equipment JSON; true machine update candidates are `EquipmentWorker`, `IElectronicComponent.Launch`, `AgentEquipmentFunction.UpdatePerTu/NoRender`, building/equipment update loops。Owner: currently split between native config and DTMAPI machine state。抵达: recipe/tech/config partial; production/output lifecycle largely DTMAPI-owned, not native-acknowledged。风险: UI/Workbench/Mine smoke successful can hide non-native production owner; save/state pollution high; rendered/no-render/offline-room differences are central and not fully solved by current loop; multi mod machine ID/recipe/tech graph conflicts high; user-layer high if ordinary mods rely on it; developer API risk high because public shape suggests stable machine API。评级: D。下一步: 暂停普通 mod 使用；要么重做到 native equipment/worker lifecycle，要么明确降级为 DTMAPI custom runtime machine experimental。 |
| `IEquipmentSlotsApi` / extra attribute slots | Matrix: More Equipment Slots API。代码: `RegisterSlots`, `EquipExtraSlot`, `UnequipExtraSlot`, storage/recovery/render methods。GameBridge: `AgentEquipmentManager.ReloadParams`, `AccessoriesBar.__Init/OnStartShow`, UI strip rendering。Native: `DolocAPI.CostItem`, `TryPlaceInBackpack`, generated `AgentEquipmentFunction`; vanilla visual slots remain official。Owner: extra slots owned by DTMAPI storage/UI, native owner only sees generated attribute functions。抵达: item consume/recover/function application partial native; slot model itself not native。风险: interactive UI success not native slot acknowledgement; storage/save/recovery pollution risk; render/title/current-save boundary historically sensitive; multi mod extra-slot ownership/conflict high; user-layer high; developer risk high around safe recovery/orphans。评级: C/D。下一步: 保持 experimental，文档写“attribute-only DTMAPI slots”，补 disable/reload/cross-slot/corrupt recovery evidence。 |
| `ISaveSlotsApi` / MoreSaves | Matrix: MoreSaves API。代码: `RegisterSlots`, `ApplySaveSlotExpansion`, `GetNativeArchiveSlotCount`。GameBridge: direct write/read `DolocAPI.gameManager.archiveFileCount` and official save UI path。Native: `GameManager.archiveFileCount`, save UI load/delete/copy code path。Owner: native game manager/save UI。抵达: 扩展 slot count 触达 native manager。风险: UI 多槽显示成功不等于所有 slot operations/save file edge cases；save pollution direct; rendered title/save UI only; multi mod last requested slot count conflict; user-layer high; developer risk is assuming arbitrary slot count safe。评级: B。下一步: 补新槽创建/加载/删除/copy/rollback/restart evidence；增加 single owner/conflict policy。 |
| `IInventoryDebugApi` as item mutation | Matrix: item debug。见 D 节。Native: `TryPlaceInBackpack`。评级: B debug-only。下一步: 不迁移为 ordinary item API 前先设计 permission/save policy。 |
| `IMailDeliveryApi` as item delivery | Matrix: mail debug/migration support。见 D 节。Native: `SendItemAsEmail`。评级: B debug/controlled-only。下一步: 普通 mod mail API 需模板、dedupe、source lifecycle 规范。 |

## G. Vehicle, Camera, Farming

| API family | 审查 |
| --- | --- |
| `IMotorVehicleApi` / original + second motor | Matrix: motor/vehicle API。代码: `GetVehicles`, `RegisterSecondMotor`, `UnlockOriginalMotor`, `SummonOriginalMotor`, `SummonVehicle`, `RideVehicle`, `DismountVehicle`。GameBridge: `ItemMotorKey.OnUse`, `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor/GetOffMotor`, `MotorController.OnFixedUpdate`, `DolocAPI.UnlockMotor/SetMotorPosition/EnterRoom`。Native: original motor owner is `MotorDataManager`, `DolocAPI.Motor`, `MotorController`, `SetMotorPosition`, `UnlockMotor`; second motor is DTMAPI runtime clone wrapping singleton riding path。Owner: mixed original native singleton and DTMAPI second-motor state。抵达: original unlock/summon reaches native; second motor ride uses native-ish ride calls but state owner not native multi-vehicle registry。风险: dual-visible smoke can hide singleton/shared-state residue; save pollution/lifecycle residue high; rendered/current-room edge transition partly evidenced, offline/no-render not native; multi mod duplicate key/vehicle conflict high; user-layer high; developer API risk high for custom vehicles。评级: original B, second motor D, overall C/D。下一步: 普通 mod 暂停自定义车辆依赖；需要 native `MotorDataManager` 多实例方案或显式 DTMAPI clone limits。 |
| `ICameraZoomApi` / Zoom | Matrix: Camera Zoom API。代码: `Register`, `SetViewScale/Step/Reset`, `ApplyCameraZoomTarget` writes camera orthographic size。GameBridge: no broad camera controller hook; direct camera field/property write。Native: native camera owner includes `CameraController.camSize`, room camera range, `DolocAPI.envBackgroundEx`, `EnvCovariantController._depthFogController` per DolocPlus study。Owner: currently Unity camera field only。抵达: only visual camera size, not full native camera/environment responsibility。风险: 4x smoke 看似成功但背景/depth fog/room framing 未 native-acknowledged；无 save pollution；rendered-only, title/scene transition gaps; multi mod camera conflict high; user-layer medium; developer risk high if used for stable camera API。评级: C。下一步: 降级 experimental，补 background/fog/range compensation 或重做 GameBridge。 |
| `IChestLocatorEnhancerApi` | Matrix: chest locator mod。代码: `Register`, state; hook callback appends inventories。GameBridge: `ArchiveDataHandle.GetAvailableInventories` array-result postfix。Native: `ArchiveDataHandle.GetAvailableInventories`, `DolocAPI.GetInventoriesAroundEquipment`, `CountItem/CostItem/MaxCostItem`。Owner: native inventory query plus DTMAPI appended Case/StorageShelf inventories。抵达: 查询结果进入 native consumer array，Count/Cost third-save evidence strong for transient shared Case。风险: UI/recipe 成功不一定覆盖所有 crafting UIs/rooms/offline containers；shared inventory append 可影响 native cost scope；save pollution via native consumers; rendered/current-room evidence narrow; multi mod inventory append ordering/conflict medium-high; user-layer medium; developer risk is overbroad inventory reach。评级: B/C。下一步: 补 every UI/room/shared shelf/reload evidence，定义 append order/conflict。 |
| `IStrongPlantingGunApi` | Matrix: strong planting gun。代码: `Register`, `ExpandFarmingGunInventoryIfNeeded`, official basin checks。GameBridge: `ItemFarmingGun` ctor, `ItemFarmingGun.OnUseAsTool`, `FarmingGunUiState.HandlePlaceToOtherSide/HandleSwapOneItem`。Native: official farming gun storage/use and `PlantBasin` interaction checks for seed/film/fertilizer。Owner: native farming gun/basin systems with DTMAPI slot expansion。抵达: 是，核心 use path 走官方 gun/basin check。风险: 三槽 UI 成功不覆盖 watering/tree/edge/weather/all seed classes；save pollution via planting/fertilizing; rendered/current-room only; multi mod gun inventory expansion conflict medium-high; user-layer medium; developer risk是把它当 whole-room planting API。评级: B。下一步: 保持 experimental，补 seed/film/fertilizer/tree/water/offline/reload matrix。 |

## H. 0.4.0 Custom Entity APIs

| API family | 审查 |
| --- | --- |
| `ICustomAnimalApi` | Matrix: 0.4.0 stable custom entity API。代码: `CustomEntityRegistryService.RegisterSpecies/Get/Unregister/RequestSpawn/GetSnapshot/GetStatus`。GameBridge: `PublishStableCustomEntityHookStatuses` reports configured-blocked; runtime registers same Core service。Native: prospective native owners include `Animal`, `AnimalSystem`, `AnimalRoomEnv`, `AnimalAI`, `AnimalMap`, official content tables。Owner: 当前只有 DTMAPI registry。抵达: registration 抵达 DTMAPI contract; spawn explicitly returns `runtime-creation-blocked` and does not create native animal。风险: stable API 名称容易让 modder 以为 runtime spawn works；no save pollution now because blocked; rendered/offline none; multi mod duplicate ID guarded but no content merge; user-layer low until mods expect runtime entity; developer risk high if advertised as full native。评级: A for registry contract, D for native runtime creation。下一步: 文档/矩阵必须双评级；普通 mod 不应依赖 spawn until native adapter exists。 |
| `ICustomMonsterApi` | Matrix: 0.4.0 stable custom monster/spawn table。代码: `RegisterMonster/RegisterSpawnTable/RequestSpawn/GetSnapshot`。GameBridge: configured-blocked status。Native: possible owners `IMonsterHost`, monster assets, `Command_GenerateMonster`, spawn tables。Owner: DTMAPI registry only。抵达: debug `SpawnMonster` can spawn official monsters, but custom monster API does not inject native monster assets/spawn tables。风险: spawn table registration looks successful but native room spawn never sees it; no save pollution while blocked; rendered/offline none; duplicate guarded only inside DTMAPI; user/developer risk high if mistaken as content API。评级: A registry, D native。下一步: keep stable contract wording but runtime status `configured-blocked` must remain prominent。 |
| `ICustomAttackApi` | Matrix: stable attack/projectile API。代码: `RegisterAttack`, `SpawnProjectile`, `ExecuteAttack` return blocked。GameBridge: configured-blocked。Native: possible owners `SkillFactory`, attack behaviours, projectile/bullet assets。Owner: DTMAPI registry only。抵达: no native projectile/attack owner。风险: UI/log registration success not native; no save pollution while blocked; rendered none; multi mod duplicate guarded only in registry; user low, developer high。评级: A registry, D native。下一步: do not mark spawn/execute usable until native projectile factory evidence exists。 |
| `ICustomDroneApi` | Matrix: stable drone API。代码: `RegisterDrone`, `RequestSummon`, `Equip`, `SetMode` all blocked for runtime target。GameBridge: configured-blocked。Native: possible owners `Drone`, `AgentEquipmentFunction`, drone configs. Owner: DTMAPI registry only。抵达: no native drone creation/equipment/mode owner。风险: summon/equip/mode names imply native runtime control but return blocked; no save pollution while blocked; rendered none; multi mod duplicate only; user/developer risk high if docs underspecify。评级: A registry, D native。下一步: keep blocked, require native drone lifecycle review before implementation goal。 |

## I. Multi-Author Compatibility

| Topic | Audit result |
| --- | --- |
| API provider ownership | `ModRegistryService` allows one provider per `UniqueID|TApi`; no semantic version negotiation, capability discovery, or multi-provider composition. Rating B/C. |
| GameBridge policy conflicts | ActionSpeed, CameraZoom, SaveSlots, ChestLocator, StrongPlantingGun, EquipmentSlots, MachineProduction all have potential “last policy wins” or single-owner assumptions. Rating C/D depending API. |
| Content ID conflicts | `ContentQueryService` collapses duplicate item IDs by enabled/load-order/source sort. It does not surface a full conflict graph to ordinary mods. Rating C. |
| Hook ordering | Harmony patch order is mostly implicit. Multiple mods or future bridges touching same `ToolCollider`, `AgentState*`, `MotorController`, `ArchiveDataHandle.GetAvailableInventories`, `ItemFarmingGun` paths can conflict. Rating C/D. |
| Shared native singleton state | Camera, original motor, time scale, creative no-cost, save slot count, archive weather/time, and debug console modal flag are global. Rating D for ordinary mod use without ownership policy. |
| Recovery/rollback | EquipmentSlots and SecondMotor have cleanup/recovery code, but their native owner is still partial. Rating C. |

Minimum compatibility recommendations before stabilizing more low-level APIs:

1. Add owner/capability/status details to API status objects, not only `configured/verified`.
2. Make conflict rules explicit: single-owner, additive list, priority ordered, or rejected duplicate.
3. Add native owner field to `public-api-matrix` and hook status/evidence records.
4. Require rendered/no-render/current-room/offline-room evidence before promoting machine, crop, equipment, vehicle, or time APIs.
5. Keep debug APIs unavailable or strongly discouraged for ordinary content mods.

## J. Tech Debt And Redundancy

| Area | Finding | Risk | Recommendation |
| --- | --- | --- | --- |
| Monolithic GameBridge | `DolocTownExperimentalBridgeApi.cs` holds debug, machine, equipment, vehicle, camera, farming, item, and utility reflection logic. | Hard to audit responsibility owner; one helper can bleed assumptions into unrelated APIs. | Split by native domain after this audit, only when implementation work is explicitly requested. |
| Status strings as evidence | Many `BridgeFeatureStatus`/hook statuses say verified from historical smoke but do not encode evidence scope. | Fresh Codex may treat smoke-only verified as native proof. | Add fields or docs: native owner, evidence path, scenario, room/render scope. |
| DTMAPI-owned APIs named like native gameplay APIs | MachineProduction, EquipmentSlots, CustomEntities, CameraZoom, SecondMotor expose attractive public surfaces while native owner is partial/absent. | Ordinary mod authors may build on unstable semantics. | Downgrade wording, mark experimental/blocked, gate examples. |
| Save/state sidecars | EquipmentSlots, custom entity snapshots, machine runtime state, possible debug state are DTMAPI-owned. | Save pollution/recovery bugs can persist across slots. | Require save transaction evidence, slot switching, title return, restart, disable/re-enable tests. |
| Input abstraction | Raw key strings and reflected Unity input coexist with Doloc native `DolocInputSource` maps. | Rebind/device/controller mismatch. | Future native input bridge should use action names and official binding overrides. |
| Camera API | Only camera size is changed. | Background/depth fog/camera bounds mismatch. | Use DolocPlus-derived lesson: background/fog/camera range compensation is part of native responsibility. |
| Debug and ordinary mod surfaces | Advanced Y-console wrappers are powerful and mutate save/global runtime. | Users/modders may treat debug APIs as stable helpers. | Keep debug namespace/status, add warnings in matrix/docs. |
| Custom entity stability wording | Contract is stable, runtime creation blocked. | “Stable” may be read as spawn usable. | Matrix and docs must always pair stable contract with native runtime rating D. |

## K. Conclusions

Immediate documentation decisions:

1. Keep framework helper APIs stable where they are truly DTMAPI-owned: Config, Translation, Diagnostics, helper container, basic events.
2. Keep Save/Workshop/GameLoop events as framework-native boundary APIs, but document exact native hook points and evidence gaps.
3. Treat ActionCompletion, ActionSpeed, FishingAutomation, ChestLocator, StrongPlantingGun, SaveSlots, debug wrappers as experimental native bridges with scoped evidence, not general-purpose guarantees.
4. Downgrade or pause ordinary mod use for MachineProduction, EquipmentSlots, CameraZoom, SecondMotor, and custom entity runtime creation until native owner evidence is stronger.
5. Do not create an implementation goal from this review unless the user explicitly asks for follow-up implementation planning.

Per-family final rating summary:

| Rating | API families |
| --- | --- |
| A | `IDtmHelper`, `IConfigHelper`, `ITranslationHelper` for DTMAPI UI, `IDiagnosticsHelper` as logging/reporting, custom entity registry contract only |
| B | `ISaveEvents`, `IGameLoopEvents`, `IWorkshopEvents` observer path, `IActionCompletionApi`, `IActionSpeedApi`, `IInventoryDebugApi.GiveItem`, `IMailDeliveryApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, `IInstantSaveDebugApi`, `IMovementDebugApi`, original motor helpers, `ISaveSlotsApi`, `IStrongPlantingGunApi` |
| C | `IInputHelper`, `IUiHelper`, `IDtmConfigMenuApi`, `IContentQueryHelper` as native truth, `IDebugConsoleApi`, `IFishingAutomationApi`, `IItemTooltipApi`, `IAnimalViewerApi`, `ICameraZoomApi`, `IChestLocatorEnhancerApi` broad coverage, `IEquipmentSlotsApi` effect layer |
| D | `IMachineProductionApi` native production credibility, `IMotorVehicleApi` second-motor/custom vehicle semantics, custom entity runtime spawn/summon/execute/equip, advanced creative/global bypass as ordinary mod surface |

Rollback note: this is documentation only. Reverting this audit only removes the durable review and update index entry; it does not change runtime behavior.

Follow-up note: no implementation goal was created by this review. If a future goal is requested, it should choose one exact high-risk family, read this audit first, then create a dedicated `docs/goals/YYYY/...` file and sibling `.goal.txt` prompt.
