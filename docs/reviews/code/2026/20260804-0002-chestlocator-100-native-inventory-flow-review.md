# ChestLocatorEnhancer 1.00 原生库存流审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 ChestLocatorEnhancer 静态 native responsibility / caller / transaction 审查
- Source：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)要求自动阶段证明 caller、Case/Shelf、count/max/actual cost 与排除项
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Native baseline：本地私有 `24456188_test_E861E0`，`Assembly-CSharp.dll` 长度 `6,384,128`，SHA-256 `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923`

本 Review 只保存代码追踪、兼容分类和仍需玩家确认的边界。产品实现生命周期、验证结果和发布状态仍由 owning Update 管理；不复制或分发官方程序集、反编译源码或资源。

## 1. 审查问题与结论

审查问题是：当前 ProductNative 在 `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)` 上做 Postfix 后，新增共享库存究竟会被哪些原生消费者看见，材料数量、最大制作数与实际扣料是否使用同一集合，以及哪些路径明确不应参与。

结论：当前广义语义成立，而且仍有清晰的原生边界。产品不应为 1.00 漂移改源码或重包；现有 `23762374` 包应按路线图走 Drift 真实激活。发布前仍需要用户做一次人工行为确认，静态审查不能把该项写成 player-verified。

## 2. Native owner 与容器资格

当前 `ArchiveDataHandle.GetAvailableInventories`：

- 始终先加入玩家主背包；
- 对当前房间内的 `Case`，接受 `IsShared` 或处于原生 anchor/area 范围内的实例；
- 对 `StorageShelf`，还要求原生 `autoUseBox`，并接受 `IsShared` 或处于范围内的实例；
- 只有 `useBox=true` 时才展开玩家背包与合资格 Shelf 内的 `ItemBox.inventory`。

ChestLocator 的 Postfix 保留原生结果顺序，只从主农场、当前/根房间及递归建筑房间追加 `IsShared=true` 的 `Case.inventory` 和共享 `StorageShelf` 内的 `ItemBox.inventory`。它同时以引用身份去重 inventory、以引用身份去重房间，并保留调用方的 `useBox` 与原生 `autoUseBox` 门。未安装定位器的 `IsShared=false` 容器不追加。

## 3. Caller 图

直接调用 `GetAvailableInventories` 的源码集合只有 owner 自身与 `DolocAPI` 四个包装路径；没有第二个绕过包装器的材料系统。当前消费者分类如下：

| 包装路径 | 当前真实消费者 | 兼容含义 |
| --- | --- | --- |
| `GetInventoriesAroundEquipment` | Workbench、AutomateWorkbench、Synthesizer、FoodPackingStation、EquipmentWorkbench | 工作台、自动工作台、合成器、食品打包站和设备工作台都取得同一个被 Postfix 扩展的数组 |
| `GetInventoriesAroundAgent` | Exchange Store | 商店启动参数获得扩展数组；这确认产品语义不应收窄为 equipment-origin |
| `GetBackpackWithInsideBoxes()` / `true` | BuildingPanel、InventorySystem | 建筑材料界面以及 `checkBox/useBox=true` 的原生数量/扣料路径经过扩展数组 |
| `GetBackpackWithInsideBoxes(false)` | FarmingGunUiState | 显式只取背包和背包内 ItemBox，不调用 `GetAvailableInventories`；种植枪保持排除 |

该边界不是“任意远程取物”。产品只扩展原生本来选择调用 `GetAvailableInventories` 的查询；原生选择 `useSharedContainer=false` 或其他完全不同 inventory owner 的路径不受影响。

## 4. 数量、最大制作数与实际扣料连续性

`IRecipe` 对传入同一 `LinearInventory[] inventories` 执行三步：

1. `AffordCostInputItemsInInventory` 用 `inventories.CountItem` 做材料数量判断；
2. `MaxAffordScale` 仍用 `inventories.CountItem` 计算最大制作数；
3. `TryCostInputItemsInInventory` 先对同一数组重新判断，再用 `inventories.MaxCostItem` 做真实扣料。

各 UI 没有在预览与提交之间重新缩窄 ChestLocator 追加项：RecipePanel、EquipmentPanel、BuildingPanel 都把同一 `inventoriesAround` 用于 max 与真实 cost。RecipePanel 的 dynamic dish 路径在 `GetMaxCraftCountDefault(useBuffer=true)` 中把 `dishItemBuffer` 追加到该数组计算 max，`TryConfirmCraftDish` 再只从 `inventoriesAround` 扣除 `count - 1`，由 buffer 承担首件；PackingPanel 同样把自身 buffer 放在数组首位计算 max，并在确认时从 `inventoriesAround` 扣除 `count - 1`。这两个首件-buffer 特例都没有替换或缩窄远程追加集合。Exchange Store 则把同一数组交给 RecipePanel。因此不存在“预览能看见远程材料、确认后又从另一个集合扣料”的静态分裂。

## 5. 产品源码与可执行反证

产品的精确 Hook 仍是三参数 owner，Postfix 把原生第三参数 `__2` 原样交给 traversal。现有 Unit fixture 已覆盖共享 Case、共享 Shelf/ItemBox、native `autoUseBox=false`、调用 `useBox=false`、配置关闭、引用去重、房间去重与受界日志。本轮补入两个 `IsShared=false` 容器并把扫描数锁为四个，证明容器可以被遍历观察但不会被追加。

可重放检查器 [`test-dtmapi-060-chestlocator-native-trace.ps1`](../../../../tools/scripts/test-dtmapi-060-chestlocator-native-trace.ps1)同时绑定精确 final-test assembly identity、捕获时的 `3666` 文件 decompile inventory、完整 caller 文件集合、具体 owner/wrapper/transaction 方法体、RecipePanel 与 PackingPanel 的首件-buffer 特例、产品 traversal 与种植枪排除项；“token 移到相邻方法”反证也必须失败。它是本路线的聚焦代码追踪工具，不是新的 receipt、schema、发布权威或默认 Release gate。

## 6. 兼容分类与剩余门

- 代码修正：无。
- 新策略/重包：无；保留现有 package/receipt/policy 历史，以 0.6 Loader 的 Drift 路径真实激活。
- 自动专用 game smoke：不要求；普通加载和 owner cleanup 进入 11-Mod 最终集成。
- 发布前人工确认：仍需覆盖农场与建筑各一处、Case 与 StorageShelf/ItemBox、数量判断、最大制作数、实际扣料，以及未安装定位器的箱子不参与。
- 阻断条件：静态 caller 集变化、预览/扣料集合出现分裂、`useSharedContainer=false` 开始进入 Hook、非共享容器被追加，或最终普通激活/cleanup 失败。
