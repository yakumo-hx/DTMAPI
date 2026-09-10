# DTMAPI 0.6.0 九家族真实消费者与 Breaking Removal 审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / KEEP / breaking removal NO-GO`
- 性质：0.6.0 九个冻结 Compatibility 家族的只读消费者、加载失败体验与所有权审查
- 审查 HEAD：`d857a3f15de3a66f9310b92755c68bd3f9543cda`
- Source / implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

本 Review 完成路线图第 9.2 节要求的 fresh scan 和逐家结论。它不授权删除公开 API/DTO、Compatibility Host、mandatory proxy/broker 或 live 支持投影，也不建立新的 receipt、Catalog 或发布 authority。结论是：**Action completion、Action speed、Fishing automation、Fish tooltip、Animal viewer、Save slots、Chest locator、Camera 与 DebugConsole 九家族在 0.6.0 全部保持原样；当前没有一家满足 breaking-removal 提案门。**

## 1. 审查边界与方法

本次只读核对以下四组输入：

1. Catalog 已冻结的九项 `currentPublishedArtifact`，并与当前 Steam subscription 中的 entry DLL 精确 SHA-256 对照；
2. `D:\steam\steamapps\workshop\content\2285550` 当前可获得的全部 `44` 个 Workshop item、`4,905` 个文件和 `37` 条 DLL 路径；
3. `references/third-party-mods` 当前 `14` 个本地样本归档及两份 loose DLL；
4. 0.6 候选 Abstractions、当前九个 ProductNative DLL、已冻结旧第一方 DLL、Core Loader、InstallDoctor、optional Compatibility Host 与 mandatory GameBridge 源码。

DLL 扫描使用仓库当前构建的 `DTMAPI.Tooling.Metadata.PeMetadataInspector` 读取 AssemblyRef、TypeRef 与 MemberRef，不加载第三方程序集。源码扫描只匹配九家族接口、DTO 和枚举的完整类型 token。订阅观察时间为 `2026-08-04T16:24:21.2319816Z`；没有改动、解包回写或重新订阅任何 Workshop item。

本地样本只在受管临时根 `tmp/test-runs/compat-consumer-scan-20260804-0001` 中解包可读取归档。清理前确认根为仓库内精确绝对路径、根自身不是 reparse point、`163` 个 descendant 中 reparse point 为零；清理后确认根不存在。没有把第三方 DLL、反编译代码或扫描产物提交到仓库。

## 2. 九项当前 Steam 实物

下表的 manifest、entry DLL SHA-256 和文件/字节数均来自 Catalog 的既有 `currentPublishedArtifact`，且本次当前 subscription 精确匹配。九项都是 `1.0.0 / minimum DTMAPI 0.5.5 / game build 23762374`；合计 `83` 个文件、`2,032,343` 字节，九行规范摘要为 `CBB54977985F3C33BE210A78D8BCEDAD5E83AD718D0B2CD0065EA875743D3587`。

| 家族 / 当前产品 | Workshop | Steam manifest | 当前 entry DLL SHA-256 | 文件 / 字节 |
| --- | --- | --- | --- | ---: |
| Camera / Zoom | `3742717440` | `5286680386495525564` | `8539EC68B299C16095180BBF61F2FB6B8335F8915BBB78726EC01B120B11CD7E` | `10 / 483,303` |
| DebugConsole / Y console | `3742714442` | `4587382938519822005` | `102E616C598BD4576C37014E7974C6934AD552CF2208B1B7D3B49D24FC6180FF` | `11 / 478,488` |
| Save slots / MoreSaves | `3742763050` | `6595190771075205425` | `09FE3D9613A3C085A0CD43ACA46F348C5C251DE3BB21B3941280D97B7C1B9A3F` | `8 / 21,723` |
| Action speed | `3742763309` | `3207868613658110430` | `1C115CBAA92EF2AA5DBB9DF509A216CFCD195B7B5E860C563452E6DB727798D5` | `10 / 368,423` |
| Action completion / OneActionComplete | `3742763540` | `3152857404536535062` | `3E7D9ED3E00E1FA6AD69E7F3FF88B9842BE3F6E07214AEAF4C260CA81E86B3D4` | `10 / 234,109` |
| Fish tooltip | `3742763706` | `749708801000628071` | `16A8EAC5AC2BC98CF039F7A5F496C7950FB9A662F62CCDA4DB5FFB7AD150650E` | `8 / 21,246` |
| Animal viewer | `3742763843` | `1469725703216292805` | `77C026F3F41B2306852E67D7B98D7748C6136FC90C838DA39F922C36B1B240AE` | `8 / 49,134` |
| Chest locator | `3742765514` | `8718044654627401403` | `DC0851584C47FA250117B60A2F761F90C9CFE3DBE6295EFC7D1FF53A2A141447` | `8 / 39,686` |
| Fishing automation / AutoFishing | `3743799721` | `864750910416179819` | `1BBB0D1C26EB5154BA1EFE73F431B623E1B9987A6B230C35DA7AC463E9C65772` | `10 / 336,231` |

该表只证明当前发布实物身份，不证明旧 DLL 已从玩家手工副本、其他下载源或未被本机订阅覆盖的生态中消失。

## 3. Fresh third-party consumer scan

### 3.1 当前 Workshop subscription

`37` 条 DLL 路径中，`20` 个程序集引用 `DTMAPI.Abstractions`：九个当前 ProductNative、五个 Runtime/Host 组件、retained MoreEquipmentSlots、retained Manbo，以及四个外部消费者。对九家族目标类型逐个扫描后：

- 四个外部消费者没有命中九家族任一接口、DTO 或枚举；
- MoreEquipmentSlots 只消费其独立 EquipmentSlots 家族；
- Manbo 只消费独立 AudioReplacement 家族；
- 九家族命中只出现在平台/Host 组件、已知旧第一方消费者，以及下节说明的当前 DebugConsole DTO 复用中。

四个当前外部消费者的精确结果如下：

| Workshop / DLL | SHA-256 | 全部 Abstractions MemberRef | 九家族目标命中 |
| --- | --- | ---: | ---: |
| `3743621104` / `DolocStorageExpansionMod.dll` | `45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366` | `3`（DtmMod/Monitor） | `0` |
| `3743644065` / `DolocStoreCapacityMod.dll` | `BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF` | `4`（framework/config） | `0` |
| `3754869009` / `DolocTownQoL.dll` | `FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10` | `13`（framework/config menu） | `0` |
| `3759797170` / `Mxx_DolocTownMod_Installer.dll` | `F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D` | `3`（DtmMod/Monitor） | `0` |

这四项是当前本机可获得的真实外部消费者，不是完整生态代表；零命中不能被提升为“第三方消费者不存在”。

### 3.2 本地第三方样本

`references/third-party-mods` 当前有 `14` 个归档，共 `41,189,314` 字节。其中 `10` 个可列出/解包，共 `4,322,636` 字节；可读输入产生 `67` 条 DLL 路径，另有两份 loose DLL。所有可读 DLL的 `DTMAPI.Abstractions` AssemblyRef 和九家族 TypeRef 均为 `0`，可读源码/文本也没有目标 token。

四个加密或当前工具无法列出的 7z 输入只能绑定外层 SHA-256，不能宣称完成程序集扫描：

- DolocPlus latest outer：`AE09C98082E145D1FA560FFAA8FAD57F0ACEA53389EDF73ADC1E1A34051DCD14`；
- Lua CT latest：`6DCA52C91C95A69A4EB01C3699E743A017C85DCFA82EB19FFC407AFBF5F93DEA`；
- DolocPlus older outer：`295EB1EA257E8F86521B858738C05E6C6448B8540F2770FABB92E8728058C1A6`；
- Lua trainer older：`BDB48C0C61B29EDE1BD913BF9CB07A9C308459F7771298C0D5C648D42E57BCBC`。

latest DolocPlus 可单独读取的 nested `DolocPlus.dll` 为 `BEAA313EFC99BC5ADDCC76C8220BFE19CDB479BF9F843005B3F592CB0E056EA1`，没有 DTMAPI 引用；它不能替代对四个 opaque outer archive 的完整判断。

## 4. 当前 ProductNative 与 DebugConsole DTO 反证

ActionCompletion、ActionSpeed、AutoFishing、Fish、Animal、MoreSaves、Chest 和 Zoom 的当前已发布 entry DLL 对各自冻结 Compatibility 家族的目标 TypeRef 都为 `0`。

当前 `DTMAPI.DebugConsole.dll` 是明确例外。其精确 SHA-256 为 `102E616C598BD4576C37014E7974C6934AD552CF2208B1B7D3B49D24FC6180FF`：

- 对 `IDebugConsoleApi`、`IInventoryDebugApi`、`IWeatherDebugApi`、`ITeleportDebugApi`、`IInstantSaveDebugApi`、`ITimeDebugApi`、`IMovementDebugApi`、`IAdvancedDebugApi` 八个兼容接口的 TypeRef 为 `0`；
- 但直接引用 `28` 个来自 `DTMAPI.Abstractions` 的冻结 Debug DTO/枚举：`AdvancedTimeAdvanceKind`、`InventoryDebugQuery`、`InventoryDebugPage`、`InventoryDebugSourceGroup`、`InventoryDebugItem`、`InventoryDebugGiveResult`、`WeatherDebugState`、`WeatherDebugOption`、`WeatherDebugSetResult`、`TeleportDestination`、`TeleportSnapshot`、`TeleportResult`、`TeleportCsvExportResult`、`InstantSaveDebugState`、`InstantSaveDebugResult`、`TimeDebugState`、`TimeSkipResult`、`TimeScaleDebugResult`、`DebugValueResult`、`DebugCommandResult`、`CropMaturityResult`、`CreativeModeState`、`CreativeModeResult`、`TechPointDebugOption`、`SpawnDebugOption`、`SpawnDebugResult`、`MovementDebugState`、`MovementSpeedResult`。

产品源码中的 `Ui/IDebugConsoleActions.cs`、`Ui/DebugConsoleUi.cs` 和 `Native/DebugConsoleNativeActions*.cs` 也直接使用这些类型。这说明“ProductNative 不调用兼容 provider 接口”不能推导为“Debug 公共 DTO 可删除”。公共 API 矩阵只需澄清这一当前消费事实；其 `Diagnostic` 状态、接口冻结状态和 Compatibility Host 所有权均不改变。

`products/first-party` 的文本扫描另有 `11` 个目标 token 命中，除文档外唯一执行代码是 AutoFishing QA L0 driver 对 `IFishingAutomationApi` 的旧兼容验证。它是 QA authority，不是当前玩家产品消费者，也不构成删除许可。

## 5. 已知旧第一方消费者仍然成立

当前 Steam item 已更新不会让旧 DLL 从兼容责任中消失。既有 retained authority 仍绑定九个真实已发布 DLL，并在候选 Abstractions `0.5.3.0` 上解析完整 MemberRef：

| 家族 | retained DLL / SHA-256 | 冻结家族 MemberRef |
| --- | --- | ---: |
| Action completion | `Yuuka.DTMAPI.OneActionComplete.dll` / `612B173653EB22E3EB46C7882D97BB1117D308A70EF00773C8031E79DF5626E6` | `11` |
| Action speed | `Yuuka.DTMAPI.ActionSpeed.dll` / `3169DB47BA7686B124F2BC907FED89605DF5B1BD17703C33566A147DD8D7B13A` | `22` |
| Fishing automation | `Yuuka.DTMAPI.AutoFishing.dll` / `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA` | `18` |
| Fish tooltip | `Yuuka.DTMAPI.FishBreedingAssistant.dll` / `128EE6AD1893A8C98C0A6BC879E398371350B555E7BA47CE5D54AEE248031BEA` | `15` |
| Animal viewer | `Yuuka.DTMAPI.AnimalHusbandryProgress.dll` / `E5DAA3B385AD30E9671FD9BC0407534A66390F28B327358183C65D9F68DA7530` | `7` |
| Save slots | `DTMAPI.MoreSaves.dll` / `F9CB4CE42BBECF7541C963B600440C65007E0C9F088C4414046658539862226E` | `15` |
| Chest locator | `DTMAPI.ChestLocatorEnhancer.dll` / `125704135FFF47778993B268F89A90E911B7757D14938B731AB91CCC7BCDE2CE` | `18` |
| Camera | `DTMAPI.Zoom.dll` / `DFA74BDD3561A9E9647FEC01AB9B48F095826D2A752AE920566BE2C5063971B3` | `35` |
| DebugConsole | `DTMAPI.YKeyConsole.dll` / `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E` | `7` |

默认完整 Release 已对 `11` 个 retained 第一方产品和 `4` 个外部消费者解析 `463/463` 个 Abstractions MemberRef，candidate public API deletion 为 `0`。这些旧字节是实际消费者证据，不是仅供阅读的历史叙述。玩家仍可能持有手工副本、旧下载或离线安装；0.6 没有建立能证明这些副本已消失的迁移窗口。

## 6. Loader / Doctor 的旧 DLL 玩家结果

路线图要求未来删除时，旧第一方 DLL必须明确得到“请更新该 Mod”，而不是崩溃、假成功或含糊失败。当前实现不满足：

1. `DeprecatedApiGuidance` 主要从候选 `DTMAPI.Abstractions` 当前 metadata 构造 obsolete catalog；硬编码 fallback 只覆盖 Lamp、Fishing 的部分类型和 `ICameraZoomApi`。如果某家族类型被物理删除，多数家族的迁移提示会随 metadata 一起消失，`ICameraViewApi` 也没有独立 fallback。
2. `DtmApiRuntime.LoadCodeMod` 在任何九家族 removal-aware fingerprint/preflight 之前调用 `Assembly.LoadFrom`。缺失 Type/Member 可能在加载或稍后解析时失败，当前 catch 只形成通用 `Failed to load code mod` 与清理/重启提示，不能可靠分类为“旧第一方包，请更新”。
3. 当前没有逐家旧 DLL identity → 当前 Workshop 更新目标 → 明确玩家动作的 Loader/Doctor 合同，也没有对类型删除后的 Mono 加载失败形状做正负例锁定。

因此即使 future scan 不再发现消费者，仍须先实现并验证 removal-aware Loader/Doctor 体验，才能提交 breaking proposal。本 Review 不授权先删 API 再依靠通用加载异常补救。

## 7. 九家族逐家结论

| 家族 | 当前 ProductNative 对该家族 | fresh 第三方目标命中 | 已知 retained 消费者 | Loader/Doctor removal-ready | 0.6 结论 |
| --- | --- | ---: | ---: | --- | --- |
| Action completion | `0` target TypeRef | `0`（可读范围） | `11` MemberRef | 否 | `KEEP / NO-GO` |
| Action speed | `0` target TypeRef | `0`（可读范围） | `22` MemberRef | 否 | `KEEP / NO-GO` |
| Fishing automation | `0` target TypeRef | `0`（可读范围） | `18` MemberRef | 否 | `KEEP / NO-GO` |
| Fish tooltip | `0` target TypeRef | `0`（可读范围） | `15` MemberRef | 否 | `KEEP / NO-GO` |
| Animal viewer | `0` target TypeRef | `0`（可读范围） | `7` MemberRef | 否 | `KEEP / NO-GO` |
| Save slots | `0` target TypeRef | `0`（可读范围） | `15` MemberRef | 否 | `KEEP / NO-GO` |
| Chest locator | `0` target TypeRef | `0`（可读范围） | `18` MemberRef | 否 | `KEEP / NO-GO` |
| Camera | `0` target TypeRef | `0`（可读范围） | `35` MemberRef | 否 | `KEEP / NO-GO` |
| DebugConsole | 接口 `0`；冻结 DTO/enum `28` | `0`（可读范围） | `7` MemberRef | 否 | `KEEP / NO-GO` |

每家至少同时命中“存在已知 retained 消费者”和“Loader/Doctor 不具备安全删除结果”；DebugConsole 还存在当前产品 DTO 消费。路线图只允许在证据支持时提交精确 breaking proposal，因此本次没有删除提案，也不需要向用户请求 removal 确认。

## 8. 精确影响面与 mandatory zero-leftover

如果未来重新打开 removal，至少要逐家盘点以下现存编译面：

| 家族 | optional Host executor | mandatory proxy / feature |
| --- | --- | --- |
| Action completion | `Compatibility/ActionCompletion/*` | `CompatibilityHost/ActionCompletionServiceProxy.cs` |
| Action speed | `Compatibility/ActionSpeed/*` | `CompatibilityHost/ActionSpeedServiceProxy.cs` |
| Fishing automation | `Compatibility/FishingAutomation/*` | `CompatibilityHost/LegacyFishingAutomationServiceProxy.cs` |
| Fish tooltip | `Compatibility/FishRoeTooltip/*` | `CompatibilityHost/FishRoeTooltipServiceProxy.cs` |
| Animal viewer | `Compatibility/AnimalViewer/*` | `CompatibilityHost/AnimalViewerServiceProxy.cs` |
| Save slots | `Compatibility/SaveSlots/SaveSlotsCompatibilityService.cs` | `Features/SaveSlots/SaveSlotsFeature.cs`、`CompatibilityHost/SaveSlotsServiceProxy.cs` |
| Chest locator | `Compatibility/ChestLocatorEnhancer/ChestLocatorEnhancerCompatibilityService.cs` | `Features/ChestLocatorEnhancer/*` |
| Camera | `Compatibility/Camera/*` | `Features/Camera/*` |
| DebugConsole | `Compatibility/DebugConsole/*` 及 Host 链接的产品源码 | `CompatibilityHost/DebugActionCompatibilityProxy.cs`、`DebugConsoleCompatibilityProxy.cs`、Bootstrap 注册 |

共同入口还包括 `CompatibilityHostFactory.cs` 的服务 ID、`CompatibilityHostBroker.cs`、`DolocTownGameBridge.cs`、`OwnerBoundGameBridgeApis.cs`、Compatibility Host 项目的 linked source，以及 `ExperimentalGameBridge.cs` 中的 public interfaces/DTOs。当前扫描命中的 mandatory 成员都能归类为 frozen proxy/broker、feature registration、Bootstrap/provider wiring、Platform 或 SharedNative 责任；既有 Batch 6 mandatory ProductNative zero-leftover 和 public ABI deletion `0` 门均通过。

本次没有找到同时满足路线图第 9.3 节全部条件的 mandatory ProductNative/dead 残留，因此不做所谓“顺手清理”。optional Host executor 的 dormant 状态也不把它变成 mandatory dead code。

## 9. 被拒绝的推论

- **当前 Steam item 已更新，所以旧 DLL 已没有消费者。** 被 retained exact DLL 和离线/手工副本边界否定。
- **fresh third-party scan 为零，所以生态消费者为零。** 被四个 opaque archive、仅本机 `44` 个 item 的有限覆盖和未枚举生态否定。
- **当前 ProductNative 不调用旧接口，所以可删 DTO。** 被 DebugConsole 的 `28` 个 public DTO/enum TypeRef 否定。
- **删除后 Doctor 会自动给迁移提示。** 被 metadata-derived catalog 和有限 hardcoded fallback 否定。
- **通用 `Assembly.LoadFrom` 失败等同友好的 update guidance。** 通用 load failure 不能绑定旧产品 identity、目标 Workshop 或准确玩家动作。
- **optional Host 未默认加载，所以 mandatory proxy 是 dead code。** proxy/broker 是已保留 frozen ABI 的 live 路由责任，不符合 non-breaking cleanup 条件。

## 10. Validation、回滚与后续门

本审查完成的只读检查为：

```text
current subscription inventory = 44 items / 4,905 files / 147,862,694 bytes
current subscription DLL paths = 37
DTMAPI.Abstractions AssemblyRef assemblies = 20
external target-family hits = 0 across 4 exact external consumers
readable local third-party target-family hits = 0;
  inventory = 67 extracted DLL paths + 2 loose DLLs
current ProductNative target-family hits = 0 for 8 products;
  DebugConsole = 0 interface TypeRefs + 28 frozen DTO/enum TypeRefs
retained first-party frozen references = 148 across the 9 reviewed families
managed temporary extraction root = removed; reparse descendants = 0
```

这里的 `148` 只汇总九家族表中的目标 MemberRef，不替代完整 retained gate 的 `463/463`。本次未构建/安装 Runtime、未启动 Doloc Town、未调用 native save、未写 Steam subscription/官方 `MODS`/玩家文件，也未获取 Runtime lock。

提交前 `check-product-catalog.ps1` 通过 `27 products / 11 public / 22 Workshop items / 48 API rows`，`check-doc-governance.ps1` 通过 `6291` 项，`git diff --check` 无 whitespace error；这些门只验证当前 authority 投影与文档机械一致性，不把本次只读审查提升为 gameplay 或 Runtime acceptance。

回滚本 Review 或公共 API 矩阵中的事实澄清不会让旧消费者消失，也不得用来批准删除。0.6 owning Update 继续拥有发布生命周期；未来只有在更完整的 fresh scan、已知旧消费者迁移窗口、逐家 removal-aware Loader/Doctor 合同和明确 breaking 授权同时成立时，才可重开某一家族。届时必须形成新的精确 proposal，而不是改写本 Review 的当时证据。
