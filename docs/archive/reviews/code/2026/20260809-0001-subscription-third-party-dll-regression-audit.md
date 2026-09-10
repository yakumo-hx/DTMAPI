# 当前订阅目录非第一方 DLL Mod 与七项玩家回归静态审查

## 记录状态

- 日期：2026-08-09
- 状态：`recorded` — 当前订阅字节的枚举、身份分类和只读反编译审查已完成；实际加载状态与玩家故障首个异常尚未取得，因此不把静态候选写成已确认运行时根因
- 性质：audit-only；本记录不实现修复，不修改 Runtime、第一方产品、订阅包、游戏目录或玩家存档，也不创建 Update
- Source：用户要求只查 Steam 订阅目录，找全非第一方 DLL Mod，反编译或作技术债审查，并重点核对[正式版新建存档与七项功能异常责任边界审查](../../../../reviews/manual-qa/2026/20260808-0002-public-newgame-and-functional-mod-regressions.md)
- 规则基线：已完整阅读仓库 `AGENTS.md`、[`PROJECT.md`](../../../../../PROJECT.md) 及 Required Context；第一方身份只按 [`batch6-managed-mod-identity-contract.md`](../../../../architecture/batch6-managed-mod-identity-contract.md) 的精确已准入集合判断，不按作者名、文件名或“Codex”字样猜测
- 订阅根：`D:\Steam\steamapps\workshop\content\2285550`
- 观察时间：2026-08-09 08:06:35 +08:00；51 个 item 目录、5,038 个文件、84,104,272 bytes、29 个 DLL、0 个 reparse point；最新 item 写入时间为 2026-08-08T23:18:23.6072115Z
- 工具：仓库本地 .NET 8.0.421 与 ILSpy 9.1.0.7988；反编译结果仅作临时本地研究，结论落盘后删除，不保存或分发第三方反编译源码
- 未检查：游戏目录、`BepInEx/plugins` 的实际文件、BepInEx/DTMAPI 配置、玩家日志、其他 Mod 目录、旧工程、包缓存与任何历史安装状态

## 一、最终裁决

当前订阅目录的 29 个 DLL 已穷举分成三类：6 个 DTMAPI Runtime 框架文件、11 个第一方已准入产品 DLL、12 个属于 11 个非第一方 Workshop item 的 DLL。后续技术审查覆盖了全部 12 个非第一方 DLL，没有按文件名抽样。

最重要的静态发现是 Workshop `3759797170` 的两段式加载链：受管入口 `Mxx_DolocTownMod_Installer.dll` 会把同包的 `Mxx_DolocTownMod_Plugins.dll` 复制到 `<game>/BepInEx/plugins`。后者成为 DTMAPI 启停、退订和生命周期承诺之外的 External BepInEx Plugin，并在下一次启动时对 `LinearInventory`、`DataPersistenceManager.NewGame/LoadGame/SaveGame` 等原生路径安装广泛 Harmony 补丁。

该外部插件包含两个与玩家症状逐步同构的未隔离窗口：

1. `LinearInventory.Take/PlaceItem/PlaceItemAt/SwapItem/Clear` 的无保护 Postfix 在原生 mutation 和 receiver 返回后递归扫描背包；扫描本身没有完整空值、循环或异常边界。若它在此时抛错，调用者后续步骤不会执行：源槽已清空却还没放入目标库存，或目标库存已增加却还没移除世界物体。这是 Issues 2、3、4、6 的首要静态因果候选。
2. `DataPersistenceManager.NewGame` 的无保护 Postfix 在原生 NewGame 返回后调用同一扫描；异常会阻止 `DolocAPI.NewGame` 正常返回上层。它与 Issue 7 中 Loading/黑屏仍在、但 DTMAPI 左上角按钮泄漏可见的窗口精确重合，是首要静态候选。

“首要静态候选”不等于“已确认玩家根因”。只看订阅目录不能证明 helper 已被复制到游戏目录、在故障启动中被 BepInEx 加载，也不能证明扫描在玩家数据上确实抛错。确认需要失败当次日志中的 BepInEx 插件加载身份和第一条异常栈，或受控隔离复现。

第二优先候选是 Workshop `3743621104` 的 `DolocStorageExpansionMod.dll`。它直接替换/扩展 `ItemBox`、`StorageBox`、`BoxInventoryWidget` 与整套 `StorageShelfUiState/Panel/Widget`，多数补丁无异常隔离，静态页面/UI 对象也不在标题边界清理。它直接覆盖 Issues 2、4、6 的箱子/储物架域，尤其适合作为 Issue 6 的第二个重启隔离对照；但它不补丁 `LinearInventory`、`StorageShelf.OnInteract` 或 `DropItem`，当前字节不能单独证明手持箱已被原生 Take 后删除。

其余 DLL 没有同等强度的七项直接链路。特别是没有一个订阅 DLL 包含 `ruinedcity_*`、湿地正式任务链或普通 NPC Dialogue 候选补丁；Hunger 的邮件代码只在饥饿晕倒后反射发送独立救助信。Issue 5 已在原 Review 中由玩家 archive 闭合的“官方对话进度非原子提交 + 旧迁移只执行一次”根因不因本审查而改变。

## 二、全量 DLL 边界与身份分类

### 2.1 从 29 个 DLL 中排除的 17 个

Workshop `3743016467` 的 6 个 DLL 是 Runtime 框架文件而非 Mod：`DTMAPI.BepInExBootstrap`、`DTMAPI.Core`、`DTMAPI.Abstractions`、`DTMAPI.GameBridge.DolocTown`、`DTMAPI.ModConfigMenu` 与可选 `DTMAPI.GameBridge.DolocTown.Compatibility`。

另外 11 个 DLL 属于 Batch 6 契约精确承认的 DTMAPI/Yuuka 第一方产品，本次按用户要求排除：DebugConsole、Zoom、MoreSaves、ActionSpeed、OneAction、Fish、Animal、ChestLocator、AutoFishing、MoreEquipment 与 Manbo。`AutoHarvest` 的名字虽与既有研究相似，但契约把它保留为 `LegacyExternal`，本次没有误归为第一方。

### 2.2 全部 12 个非第一方 DLL

下表 TFM 来自 ILSpy 对程序集元数据生成的临时工程；“可达性”只描述包布局和静态入口，不声称当前已启用或已加载。

| Workshop / DLL | Bytes | SHA-256 | TFM | 静态加载边界 | 与七项的最高关联 |
| --- | ---: | --- | --- | --- | --- |
| `3742618545` / `DolocDevModeMod.dll` | 10,240 | `B550FC549E9A4B1757B1E511494DDD995E618DBD991EFD9805171161F0A6E4A4` | netstandard2.1 | 旧 `Content/DolocSMAPI`；DLL 自身是 BepInEx plugin，当前 DTMAPI 订阅发现路径不可达 | 仅常驻 IMGUI 对输入/UI 的低置信干扰 |
| `3742771572` / `DolocTownAutoHarvest.dll` | 8,192 | `251B7238EDDA3CA3C5BB9E206D4CE213158FB490767758CD06D56E33DBF555B0` | net472 | 旧 DolocTownSMAPI SDK 入口；当前 DTMAPI 订阅发现路径不可达 | Issue 3 仅在自动收割产物场景下间接相关 |
| `3743621104` / `DolocStorageExpansionMod.dll` | 10,752 | `45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366` | netstandard2.1 | 省略 `CodeModKind` 的 DTMAPI third-party native compatibility CodeMod | Issues 2/4/6 直接域候选，Issue 6 最强 |
| `3743644065` / `DolocStoreCapacityMod.dll` | 5,632 | `BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF` | netstandard2.1 | 同上 | 七项基本排除 |
| `3743790125` / `DolocNoWeedsMod.dll` | 5,120 | `7869174F2911FBA01D2BAA4517A0BCCE3E9EA0A7A83211F2AB4445BDB95FB5ED` | netstandard2.1 | 同上 | 七项无直接目标 |
| `3754866797` / `DolocPriceHelper.dll` | 17,920 | `EF9A3DA892D8894AAF0E3DA1363B6CDE33F75D6784B036999C49820E56872667` | net40 | 同上 | 箱/物品说明 UI 的低置信间接候选 |
| `3754869009` / `DolocTownQoL.dll` | 20,992 | `FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10` | net40 | 同上 | 箱内容摘要 UI 的低置信间接候选 |
| `3759797170` / `Mxx_DolocTownMod_Installer.dll` | 7,680 | `89F8C282B53B0629975E7F343FC3A15354FFFD44C055C5F125A8A586EA72004B` | net472 | DTMAPI compatibility 入口；执行时向 `BepInEx/plugins` 安装下一行 helper | 间接开启 Issues 2/3/4/6/7 首要候选 |
| `3759797170` / `Mxx_DolocTownMod_Plugins.dll` | 118,272 | `90B8AF0244483EC6CA356C246F503C41D9878FE3BCBAEF77D2D623D4EACC319C` | net472 | 同包 helper；复制后是 External BepInEx Plugin | Issues 2/3/4/6/7 首要静态因果候选 |
| `3772057085` / `DolocTownHungerSystem.dll` | 40,960 | `0CE2585016038469F45DBD044D35BABC805EFE36CFCEB8E4778C15021BF92CCD` | net47 | 订阅根的 loose DLL、无 manifest；只有玩家手工复制到 `BepInEx/plugins` 才可作为外部插件加载 | Issue 5 的正式信件链排除；其余无直接命中 |
| `3779898662` / `BuildingRelocationHelper.dll` | 35,328 | `9A896D9E276FFCE6DDCE117F3E6265D3F0C8171D599DEAA5E472697CF38553FA` | netstandard2.0 | DTMAPI third-party native compatibility CodeMod | 七项无直接目标；与 Mxx 库存 Postfix 有跨 Mod 半事务风险 |
| `3779924188` / `Minato.MagicStorage.dll` | 98,816 | `F07637CE1FD943D67983884536E74CC5D0B5637E216924929F5D2A713D54EEEF` | net471 | 同上 | 若反馈箱是 Magic core，Issues 2/4 有条件相关；另有不同方向的真实掉落丢失风险 |

物理存在、可发现、已启用和已加载是四件不同的事。前两行旧布局和 Hunger loose DLL 不能仅因位于订阅目录就视为当前运行中；反过来，Mxx helper 一旦被安装器复制，随后即使禁用或退订 Workshop item，也不会由该安装器自动从 `BepInEx/plugins` 移除。用户要求限定订阅目录，因此本轮没有越界核实哪一种状态真实发生。

## 三、关键反编译事实

### 3.1 Mxx 安装器与外部插件

`Mxx_DolocTownMod_Installer.DolocModPackInstaller.Entry` 以程序集相邻的 helper 为源，创建或更新：

- `<game>/BepInEx/plugins/Mxx_DolocTownMod_Plugins.dll`
- `<game>/BepInEx/plugins/Mxx_DolocTownMod_Plugins.new.dll`
- `<game>/BepInEx/plugins/mxx_install_lock.dat`

它没有对应卸载、退订撤回或 DTMAPI disable 清理。该行为跨越 [`PROJECT.md`](../../../../../PROJECT.md) 定义的 DTMAPI 管理边界：复制后的 helper 只能分类为 DTMAPI 管理承诺之外的 External BepInEx Plugin，不能随受管入口的禁用或退订自动撤回。安装器本身的文件操作有总 catch；风险来自 helper 在后续进程被 BepInEx 加载。

helper 的 BepInEx 身份是 `com.mxx.doloc.itemlimiter`。其 `Awake` 先对整个程序集 `PatchAll`，随后各模块又对若干类型执行 `PatchAll`；程序集共出现 71 个 `HarmonyPatch` 标记。代码没有统一的 owner 清退、标题边界 reset 或注册原子性门，也存在重复注册尝试对 Harmony 去重行为的隐式依赖。

`BagPassiveBuffPatch` 在以下 `LinearInventory` 方法返回后无保护地调用 `RefreshAllTemporaryBuffSource`：

- `PlaceItem(Item)`、`PlaceItemAt(int, Item)`；
- `Take(int)`、`Take(Item)`；
- `SwapItem(int, Item)`、`Clear()`。

刷新函数直接解引用 `DolocAPI.archiveHandle.InventorySystem.inventory`，递归扫描所有嵌套 `ItemFunctionBox`，再枚举被动装备。入口没有完整 null guard，递归没有 visited set 或深度上限，被动装备数组也没有 null guard；六个 Postfix 没有 catch/finalizer。常驻 tick、新建和加载 Postfix 还会直接调用同一函数。

这与原 Review 已确认的官方非原子窗口组合后，形成可执行的故障链，但仍以“扫描真的抛错”为条件：

- 箱子转移/储物架：原生 `Take` 已清源槽并完成 receiver；Mxx Postfix 抛错后，调用者尚未执行目标 `PlaceItem` 或 `PlaceBox`。
- 世界拾取：原生 `PlaceItem` 已把数量写入背包并完成 receiver；Mxx Postfix 抛错后，`DropItem.OnTouch` 尚未执行 `Host.RemoveDropItem`，所以同一实体可再次触碰。
- 新建存档：`DataPersistenceManager.NewGame` 已返回自身结果；Mxx Postfix 抛错后，上层 `DolocAPI.NewGame` / `GameDataUiState` 收不到正常返回，LoadingPanel 可继续保留。

保存路径还有独立的高风险技术债：`DataPersistenceManager.SaveGame` Prefix 在 native save 前写 `MxxModSaves/lucky_perm_<slot>.json`，并在局部 catch 之外 flush/save 另一组 treasure 记录；sidecar 可以领先 native commit，异常也可以阻断 native SaveGame。另一个 `DolocAPI.SaveGame` Prefix 会在静态 `currentInteractedCrystalBallId` 等于 Mxx 水晶球时直接跳过原生保存，而该静态标记没有统一的 Unload/NewGame/title 清理。两者均不满足 `PROJECT.md` 的 save-commit 语义。

### 3.2 StorageExpansion

`DolocStorageExpansionMod.dll` 对所有 `ItemFunctionBox`、`ItemBox`、`StorageBox` 强制返回容量 10；把 `BoxInventoryWidget` 行容量限制为 5，并在 UnBind 后重设 10x5；完全替换 `StorageShelfWidget.allSelectablesArray`，同时接管 `StorageShelfPanel.RefreshLayout`、`StorageShelfUiState.Show` 和 `OnUiUpdate`。

其分页代码以静态 `CurrentPage/PageText` 保存跨 panel 状态，动态创建 Unity UI 和 listener，却没有 title/dispose 清理。`Show/OnUiUpdate` 每帧反射私有 panel，`OnUiUpdate` 连反射成员缺失都没有判空；大部分补丁没有 catch。`allSelectablesArray` 出错时反而吞掉异常、阻止原方法并返回空数组，可令键盘/手柄导航整体消失。容量降级也没有迁移方案：扩展槽已有物品时，停用后可能不可达。

这些事实使它成为箱/储物架回归的高价值隔离对象，但它不修改 `LinearInventory` mutation、`ContainerBaseUiState`、`DropItem` 或 `StorageShelf.OnInteract`；没有第一条异常栈时，不能把“直接触达 UI 域”升级为“已造成物品删除”。

### 3.3 其余九个 DLL

- DevMode：常驻 IMGUI，可直接改钱、解锁基因并按按钮调用 `DolocAPI.PlaceItem`；PlaceItem 返回值被忽略，窗口不尊重 scene/input context。但它没有 Harmony，也不触达 Dialogue/NewGame/箱/DropItem/任务邮件。旧订阅布局本身不可由当前 DTMAPI 进入。
- AutoHarvest：只补丁 `Crop.CheckGrowthMonth`，每秒扫描成熟 `PlantBasin` 并调用 `Harvest(true,true)`；异常后无业务回滚、下一 tick 会重试，因此只对“反馈物体其实是自动收割作物产物”的特殊 Issue 3 有间接可能。它不补丁世界 `DropItem` 或背包。
- StoreCapacity：只放大 `Store.GetItemSpawnCount`，七项基本排除。其无上界 unchecked 乘法、静态配置和无 unpatch 是独立技术债。
- NoWeeds：只 suppress 农场的原生 vegetation/dungeon-resource 生成，以裸 room type `1` 判断；不触达 wetland/ruined-city 任务、邮件或 Dialogue。
- PriceHelper：只给 `ItemData` 追加价格/种子信息，并给 `ItemHoverBox` 重挂滚动 UI；补丁主体均 catch。静态 UI state 和 Harmony 无清理，但没有库存 mutation。
- DolocTownQoL：只读取箱库存以生成内容摘要并重画烹饪预览；补丁主体均 catch。程序集版本 `0.0.0.0` 与 manifest `0.2.3` 不一致，且无生命周期清理。
- HungerSystem：loose BepInEx DLL；补丁吃食物、睡眠、晕倒、`DolocAPI.SaveGame/LoadGame`，并只在饥饿晕倒恢复时反射发送自己的救助信。邮件操作有 catch，不读取/移除 `ruinedcity` 候选，也不启动正式任务链。它以进程静态保存 food/starving/UI 状态，却没有 NewGame、ReturnedToTitle、换档 reset 或实际调用 UI Cleanup；槽位配置只预建 0--9，且用 slot index 而非 archive 身份，和当前扩展槽/槽位复用不兼容。若玩家手工安装并在对话中恰好饿晕，`FaintGameState.Startup` 有条件打断状态机，但没有当前加载证据。
- BuildingRelocationHelper：只在明确使用斧头打包建筑或还原蓝图时操作背包/建筑，自身七项直接目标缺失。其打包顺序是 `inventory.PlaceItem(blueprint)` 后才 `RemoveBuilding`，且 PlaceItem 位于本地补偿 try 之外；receiver 或 Mxx Postfix 若在已写槽后抛错，会复制蓝图/建筑。还原顺序是创建建筑、清目标地块资源、最后 `item.CostSelf`；后半段失败可能同时丢蓝图、删新建筑，并且已清资源没有回滚。它还在全局 JSON.NET resolver 中把原生 Building 对象图挂进 `ItemBuilding` 存档，没有 schema/version/大小/迁移边界。这不是七项共同原因，却是独立的高严重度复制、丢物和存档兼容债。
- MagicStorage：普通箱/背包经过其 Harmony wrapper 时 `__state=false`，不会进入动态容量和堆叠 scope；若反馈对象是 `magic_storage_core`，`FarmCaseUiState.Show` Postfix 会在原生 Show 后无 catch 地改容量、刷新 inventory panel、重挂 grid 并创建滚动/筛选 UI，`Unregister` Prefix 的无保护 Restore 也能阻止官方注销，因此 Issues 2/4 有条件相关。其 LinearInventory Postfix 本身只退出 thread-static scope，不是高概率首抛者，但它不是 Finalizer：原生/其他 receiver 抛错后 scope 可能泄漏。自动收集器先移除 world drop、再向主存储 `PlaceItem`，只有方法正常返回 leftover 才重建掉落；异常会形成与 Issue 3 方向不同的真实丢物。它在 `SaveSaving` 与 `ReturnedToTitle` 都直接保存 last-seed sidecar，可能让 gameplay state 领先成功 native save 或在未保存返回标题时提交，违反 save-commit 语义。

## 四、按原截图顺序的七项分析

### Issue 1：正式版更新后，和角色对话没有出现对话选项

截图转写：“正式版更新后，和角色对话没有出现对话选项”。

分析：12 个 DLL 中没有普通 NPC、`DialogueState`、`DialoguePlayer`、对话候选数组或 opening dialogue 的直接 Harmony 目标。Mxx、Hunger 会改特殊床的 `SleepUiState` 选项，但这不是角色对话；DevMode 常驻 IMGUI 只有低置信输入遮挡可能。若 Issue 1 与 Issue 7 来自同一失败新档，Mxx NewGame/LoadGame 的半初始化异常可以间接让后续 Dialogue 不完整，但当前没有同一会话证据。直接责任仍留在原 Review 的官方对话候选/state owner；本批 DLL 没有可确认直接原因。

### Issue 2：箱子里的东西点击后消失，未出现在背包

截图转写：“箱子里的东西点击后消失，未出现在背包”。

分析：Mxx 的 `LinearInventory.Take` Postfix 是首要静态因果候选。官方 source Take 已提交并返回后，它若在背包递归扫描中抛错，会阻止 `ContainerBaseUiState` 继续执行目标 backpack Place，逐步匹配“箱槽先空、背包没得到”。StorageExpansion 是第二候选：它直接改变箱容量、Widget index/layout/navigation，且 UI Postfix 未隔离。若反馈中的“箱子”实际是 `magic_storage_core`，MagicStorage 的原生 Show 后 UI 重挂是另一个有条件候选；普通箱则不进入其动态库存分支。PriceHelper/QoL 只有已 catch 的描述渲染路径。必须用首个异常栈确认是 Mxx scan、StorageExpansion/Magic UI 还是原 Review 所列官方 receiver；当前不宣称已归因。

### Issue 3：拾取物品后物品栏变化，但贴图不消失且可重复拾取

截图转写：“拾取物品，物品栏有变化，但是贴图未消失，物品栏数量也一直在变化，可以重复拾取”。这里“贴图未消失”按玩家语义解释为世界物体/贴图未被移除。

分析：Mxx 的 `LinearInventory.PlaceItem` Postfix 是首要静态因果候选。原生 PlaceItem 已把数量写入背包后，Postfix 若抛错，`DropItem.OnTouch` 不会走到 `Host.RemoveDropItem`，下次触碰会再次增加，和反馈顺序完全一致。AutoHarvest 只在物体确为成熟作物自动收获产物时有重试型间接可能；MagicStorage 自动收集路径先删 world drop 再 Place，方向与本条相反。当前缺少物品 ID、当次加载清单和第一栈，不能把结构匹配写成玩家现场确认。

### Issue 4：从箱子里拿工具出来，工具凭空消失

截图转写：“从箱子里拿工具出来，工具凭空消失”。

分析：工具仍走 Issue 2 的 source Take -> target Place 事务，因此 Mxx `Take` Postfix 是首要候选，StorageExpansion 的箱 UI/capacity mismatch 是第二候选；若容器是 `magic_storage_core`，MagicStorage UI Postfix/Prefix 也有条件相关。没有一个 DLL 对“工具删除”另设专门逻辑；普通物品成功也不能排除工具 subtype/receiver 在同一未隔离窗口触发异常。下一次隔离必须保留具体工具 ID、输入设备、箱类型和首个异常栈。

### Issue 5：地形改造完成后，没有前往旧城市废墟的信

截图转写：“地形改造已经完成了，但是没有去旧城市废墟的信”。

分析：12 个 DLL 均没有 `ruinedcity_continue`、`ruinedcity_main`、`wetland_main`、正式 `DialogueTask_SendEmail` 或对应候选节点目标。Hunger 只在饥饿晕倒后构造独立 `rescue_letter_custom_*` 邮件，并不读取或删除正式候选；NoWeeds 的“地形”只是农场资源生成 suppression，语义无关。Mxx 的泛化 save/load 债可以成为任意流程的外部中断面，但静态字节没有命中该正式链，不能用它推翻玩家 archive 已证实的官方非原子提交 + 一次性迁移根因。本条继续按原 Review 结论闭合；历史现场是否还叠加外部异常仍无日志可判。

### Issue 6：储物架纸箱放满后，手持纸箱交互，纸箱和内部物品一起消失

截图转写：“储物架纸箱放满后，手上拿着一个纸箱，此时点击交互键，手上的纸箱和里面的物品都消失了”。

分析：Mxx `LinearInventory.Take` Postfix 和 StorageExpansion 构成两个独立高价值候选。前者可在官方已从手持/背包 Take 纸箱后、`PlaceBox` 前抛错；它还会递归扫描任意嵌套纸箱，若对象是 `mxx_void_bag`，另叠加该 DLL 对容量和底层数组的反射改写。后者精确接管 StorageShelf UI/Widget/分页和所有箱容量，且未隔离，但没有补丁世界 `StorageShelf.OnInteract`。仍需先澄清“放满”是架槽 6/6 还是箱内库存满，并取得第一次异常；若架槽确实 6/6，原生理论上不应 Take，第一栈尤其关键。

### Issue 7：正式版创建新存档后黑屏，只能按 ESC/右键，左上角有快捷图标

截图转写：“正式版创建新存档后黑屏。只能按ESC和右键，左上角有快捷图标”。

分析：Mxx `DataPersistenceManager.NewGame` Postfix 是首要静态候选。它在 native NewGame 后无保护地执行同一背包扫描；新档初始化窗口内任一 null/结构异常都能阻止上层正常返回，使原 Review 所述 LoadingPanel 不被隐藏。原 Review 已确认的 DTMAPI title-button 泄漏可以解释左上角小按钮仍可见，但不能独自制造全屏黑幕；两者组合与截图描述相容。StorageExpansion/NoWeeds 只有“初始化中也可能被调用”的低置信泛化风险，未命中 NewGame/loading/dialogue。确认仍要求失败当次最早异常栈和 `com.mxx.doloc.itemlimiter` 的实际加载证据。

## 五、横向技术债裁决

### 5.1 所有权与可撤回性

- Mxx 以 DTMAPI Mod 安装 External BepInEx Plugin，且无 uninstall/withdraw。这会让“订阅/启用清单”与“真实运行插件”脱钩，是本轮最高优先级治理问题。
- 省略 `CodeModKind` 的八个 DTMAPI 入口只能按 canonical 规则归类为 third-party native compatibility CodeMod，不是 Strict 或 Advanced。DTMAPI 只承诺发现、顺序、冷启动 Entry 隔离、日志和重启提示；未知 Hook/静态状态由作者承担，禁用/退订/更新后必须重启。
- 除 AutoHarvest 与 MagicStorage 有显式退订/Dispose 清理外，多数 Harmony owner 不保存或不 `UnpatchSelf`，静态 UI/缓存不清理；即使 Manager 显示 disabled，也不应宣称已热卸载。

### 5.2 工具链与身份漂移

12 个 DLL 只有 BuildingRelocationHelper 是 `netstandard2.0`。其余 DTMAPI compatibility 入口使用 netstandard2.1、net40、net471 或 net472；External/旧 SDK 也横跨 net47/net472。兼容入口可能容忍旧包，但这仍偏离项目对游戏加载程序集的 Unity Mono/netstandard2.0 工具链方向，增加 API/运行时漂移风险。

已见多处版本身份不一致：StorageExpansion 程序集 `1.0.0.0`、Entry 日志 `1.0.10`；DolocTownQoL 程序集 `0.0.0.0`、manifest `0.2.3`；Mxx `info.json 1.1.0`、manifest/installer assembly `1.0.0`、helper file/assembly `1.1.0.1`、BepInEx plugin `1.1.0`。这会削弱玩家日志、更新比较和故障字节的可追溯性。

### 5.3 事务与异常隔离

- Mxx 把无保护的全背包递归扫描挂到每次关键库存 mutation 的 Postfix，是当前七项最危险的横切行为。
- StorageExpansion 在 UI Postfix 中反射私有成员并直接重建导航，却没有按失败回退官方实现或清理旧 Unity 对象。
- AutoHarvest、BuildingRelocation 与 MagicStorage 各自都有跨多个原生 mutation 的业务事务；与另一个 Mod 的抛错 Postfix 组合时，单 Mod 内已有的 catch/补偿可能根本来不及运行。
- Mxx 与 MagicStorage 的 sidecar 提交不服从 successful native SaveGame，存在无保存回标题仍推进 Mod gameplay state、或 sidecar 领先官方存档的风险。

## 六、下一步证据与隔离顺序

本轮不读取游戏目录，也没有启动游戏；因此只给出下一次复现的最小判定顺序，不执行环境变更：

1. 故障后不要再次启动，先收 `Player.log`、`Player-prev.log`、BepInEx `LogOutput.log` 与 DTMAPI history；确认是否出现 `com.mxx.doloc.itemlimiter` / `MXX_BOOT`，并保留第一条异常而非后续连锁错误。
2. 对 Issues 2/3/4/6，若 Mxx helper 实际已加载，先在可回滚、重启生效的隔离环境中移除这个 External plugin 对照；单次只复现一条，并记录物品/工具/箱 ID、操作前后槽位与数量。仅禁用 Workshop installer 不能证明 helper 已卸载。
3. 若 Mxx 对照仍复现，再重启隔离 `com.user.dolocstorageexpansion`，优先跑 Issue 6 的“架槽未满但箱内满 / 架槽 6/6”双场景。
4. Issue 7 使用与 Steam AutoCloud 隔离的 disposable new-game fixture；先确认 Mxx NewGame Postfix 是否抛首错，再分别判断官方 opening/Dialogue owner 与已确认的 DTMAPI title-button 泄漏。
5. Issue 5 不做广泛 Mod 猜测或重复迁移；沿用原 Review 已闭合的玩家存档结论和定点修复边界。

## 七、验证与保留边界

- 订阅目录检查前后 12 个 DLL 的 SHA-256 未改变；没有修改任何 Workshop 字节。
- 反编译成功覆盖 12/12 个精确 DLL；只保存方法/目标/事务的派生结论，没有把第三方反编译源码写入仓库。
- `git diff --no-index --check` 通过（仅有仓库 CRLF 转换提示）；`tools/scripts/check-doc-governance.ps1` 通过，6,495 checks。未运行 build、test、安装器矩阵或游戏 smoke，因为本次是订阅包的静态 audit-only 工作。
- 不取得失败当次加载身份与首个异常栈前，不把 Issues 2/3/4/6/7 标为 Mxx 已确认根因，也不改原 Manual QA Review 的 ownership/status。
