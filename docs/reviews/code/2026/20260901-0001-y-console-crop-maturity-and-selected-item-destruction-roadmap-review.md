# Y 键控制台范围催熟与选中物品原生销毁路线审查

## Review Header

- Date: `2026-09-01`
- Status: `recorded`
- Scope: Y 键控制台 ProductNative 催熟恢复、配置范围、通用 Crop Hook 必要性、快捷栏选中物品销毁、二次确认与原生保存提交语义
- Source: 用户要求在现有 Y 键控制台后续路线图中增加两项独立功能，并进一步明确催熟功能代码必须移出 DTMAPI 本体、由 Y 键控制台单独拥有；同时要求物品删除尽量遵循官方保存逻辑，不做旁路硬删除
- Roadmap owner: [Y 键控制台后续路线图归一化](../../../updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md)
- Related records:
  - [Y 键控制台 1.1.2 手测接收与 0.3.1 退役授权](../../../archive/reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)
  - [Crops Harvesting Native Responsibility Review](../../../archive/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md)
  - [Y 键控制台运行轻量化](../../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)
  - [Public API Matrix](../../../api/public-api-matrix.md)

本 Review 只固化实施前事实、取舍、路线顺序与未来验收门。本轮不修改产品源码、
公开 API、Hook、配置、包、游戏目录或存档，也不把路线图写成已经实现。

## 总体结论

1. 催熟恢复为 Y 键控制台自己的 `ProductNative` 一次性动作。产品使用本地配置、
   本地请求/结果类型和原生反射适配，不新增或扩张公开 Debug API。
2. 催熟采用“当前房间半径过滤 + 最近优先排序 + 最大数量上限”的组合语义；配置菜单
   同时提供半径和数量，而不是只选最近数量或只选半径。
3. 本轮不新增通用 Crop Harmony Hook。当前原生设备集合已经能按需发现作物，现有
   `ICropHarvestingApi` 也明确是无 Hook 的显式操作。只有未来至少两个独立真实消费者
   同时需要同一作物生命周期事件、冲突点或全局状态时，才另开 SharedNative/API Review。
4. 删除当前快捷栏物品使用各留存官方构建均存在的
   `DolocAPI.DestroyItem(int index)`，只绕过官方 UI 的 `IsItemDisposable` 前置限制，
   不直接改 inventory 数组、不写 archive 文件、不自动保存，也不建立 sidecar/journal。
5. 删除整个当前槽位栈。第一次点击只创建易失确认快照；第二次明确确认前重新核对
   存档会话、槽位、对象身份、ID 和数量。选择或物品变化时取消，不猜测目标。
6. 两项玩法变更都只进入原生内存中的 `Working` 状态。之后原生 `SaveGame` 成功才
   提交；未发生成功原生保存就重载/退出时，按官方最后一次存档恢复。

## 问题 1：将催熟完整恢复为 Y 键控制台 ProductNative 功能

### 原始反馈

- 用户希望恢复当年从 DTMAPI 本体退役的催熟功能，但功能代码所有权必须单独回到
  Y 键控制台。
- 用户记得旧行为可能从玩家身边最近的作物开始，希望在“最近数量”与“玩家距离
  范围”之间研究更合理的方案，并允许在 DTMAPI 配置菜单调整。
- 用户怀疑 DTMAPI 本体可能仍应增加一个通用农作物类 Hook。
- 图片转写：本问题没有新截图。

### 用户确认事实与未确认回忆

- 用户确认的目标是玩家主动点击一次才执行的 Y 键控制台功能，不是后台自动催熟。
- “旧实现从最近开始”是用户回忆。本仓库当前源码、可见 Git 历史和官方反编译
  命令均没有保存该排序证据，因此不能把它写成已证实旧契约。

### 当前代码事实

- 当前按钮已经位于 Y 键控制台 Advanced 区，执行类也位于
  `products/first-party/DebugConsole/src/Native`，但签名仍使用
  `DTMAPI.Abstractions.CropMaturityResult`，并保留
  `IAdvancedDebugApi` owner facade、Compatibility proxy 与 QA 依赖；这是功能实现
  已迁入产品、契约与 ABI 依赖尚未完全迁出的半迁移状态。
- 当前 `MatureAllCrops` 递归扫描当前房间和其建筑房间，不计算与玩家的距离。
  不同房间的 Anchor/Cell 坐标属于各自房间，不能拿来与当前玩家坐标直接比较。
- 当前 ProductNative `TryMature` 只解析一个参数的 `DEBUG_SetLevel`。各留存官方构建
  中，普通 `Crop` 的责任函数是 `DEBUG_SetLevel(bool shouldRender, int level)`；
  `TreeCrop` 和 `PlantBasinGrass` 才使用一个整数参数。因此仅凭当前静态代码不能
  声称普通 `PlantBasin` 作物已被正确催熟。
- 官方 `set_crop_level` 命令的默认语义是当前选中的种植设备；`all=true` 时遍历
  当前房间的 `PlantBasin`、`PlantBasinTree` 和 `PlantBasinGrass`。它没有最近排序，
  也没有跨建筑房间的距离语义。
- 官方 `TerrainContent.Anchor` / `CoveredPositions` 与当前房间
  `AgentPositionCell` 提供了同房间、格子级、无需 Hook 的距离事实。
- 现有 `ICropHarvestingApi` 通过显式请求扫描设备并调用原生收获责任函数；
  `CropHarvestingFeature.InstallHooks` 为空，当前 Hook 状态也明确写明没有 Harmony Hook。

### 方案比较

| 方案 | 优点 | 主要问题 | 结论 |
| --- | --- | --- | --- |
| 只取最近 N 个 | 处理量严格有界、结果数量直观 | 当附近不足 N 个时会继续命中房间另一端；“身边”范围不可预测 | 不单独采用 |
| 只按半径 | 空间语义最直观，不会跨到远处 | 高密度农田中处理量无界；同半径内顺序不稳定时难复现部分失败 | 不单独采用 |
| 半径 + 最近排序 + 数量上限 | 同时限定空间和工作量；结果可预测、可复现 | 比单一数字多一个配置项 | 采用 |

### 推荐执行语义

- 只扫描玩家当前房间的原生设备宿主；不把建筑子房间坐标混入当前房间距离。
- 候选包括官方 `set_crop_level all` 已覆盖的三类：普通 `PlantBasin`、
  `PlantBasinTree` 和 `PlantBasinGrass`。空盆与已经成熟的目标不计为本次改变。
- 距离取 `AgentPositionCell` 到设备 `CoveredPositions` 的最小 Manhattan 距离；
  无法读取覆盖格时退回 `Anchor`。先过滤半径，再按距离、Anchor、原生 id 做稳定
  排序，最后截取最大数量。
- 配置建议初值：`MaturityRadiusCells=8`、`MaturityMaxTargets=24`；配置菜单允许
  半径 `1..64`、数量 `1..200`，步长均为 `1`。这些是全局产品配置，不是存档玩法
  数据；保存配置只影响下一次点击，不主动执行催熟。
- 普通作物调用 `Crop.DEBUG_SetLevel(true, matureLevel)`；树盆和草盆调用各自一个
  参数的官方方法。实现必须在调用后读取成熟状态或等级做后置验证，不能仅凭反射
  返回即计成功。
- 批量保持可见部分成功：一个目标失败时记录失败并继续或按明确的本批停止策略收口，
  已成功改变的目标不做伪事务回滚。结果至少显示候选数、实际改变数、已成熟跳过数、
  超半径/超上限跳过数和失败数。

### 所有权与通用 Hook 裁决

- 催熟请求、结果、排序、配置、UI 和三类原生方法适配均归
  `DTMAPI.DebugConsoleMod` 的 `ProductNative`。产品不得继续以
  `IAdvancedDebugApi.MatureAllCrops` 或 public `CropMaturityResult` 作为新功能边界。
- 旧 Compatibility 壳可以在其独立物理退役 Update 前继续冻结存在，但当前产品必须
  先停止消费催熟 DTO；随后 `0.3.1` 路线删除才能证明这一依赖已归零。
- 不新增 `Crop.DEBUG_SetLevel`、`Crop.OnCropMature`、成长 tick 或 `PlantBasin`
  生命周期 Hook。一次性动作从当前设备集合即可完成发现、排序和调用；Hook 不能
  改善这一点，反而会增加默认不开 Y 控制台时的常驻补丁与生命周期成本。
- 未来如果要公开 `CropChanged` / `CropMatured` 事件，必须证明至少两个彼此独立的
  真实消费者需要相同事件语义，并另行审查成长、季节、房间卸载、读档、原生调试
  调用与多 Mod 冲突。本次的 Y 控制台、NeverPublish 示例和 QA fixture 不构成该门。

### 数据分类、保存边界与验收点

- 作物等级/成熟状态是 `save-bound gameplay state`。点击只修改当前原生 Working
  对象；不调用即时保存、不写 DTMAPI sidecar、不在返回标题后重放。
- `NoNativeSave` 验收：在 UI 第十存档的 AutoCloud 隔离副本中放置半径内、半径外
  及超过数量上限的目标，验证稳定选择和三类签名；退出/重载且没有成功原生保存后，
  作物恢复到最后一次提交状态，archive/committed sidecar 保持不变。
- `NativeSaveExpected` 验收：只在与实时 Steam AutoCloud 隔离的可处置 fixture 中，
  经普通原生保存成功并冷重载后，已催熟目标保留；未选中目标不变。InstantSave
  不能替代这条玩家语义。
- 产品 Hook 数量不得因该功能增加；配置菜单缺失时动作仍以规范化默认值可用，
  但必须留下可见警告而不是静默读取未初始化配置。

### Blocker 判定

- 找不到普通 `Crop`、`TreeCrop` 或 `PlantBasinGrass` 的责任函数/成熟后置状态时，
  只阻止该原生家族，不得退回直接写 `CropData` 字段冒充完成。
- 无法证明当前房间与玩家格坐标同域时，不得执行距离排序。
- 若未来确实需要共享 Hook，先做独立 API/native-owner Review；不能在本功能 Update
  中顺手把产品需求提升为 mandatory GameBridge 能力。

## 问题 2：二次确认删除当前快捷栏整个物品栈

### 原始反馈

- 用户需要处理游戏内不可出售、不可丢弃的多余物品，例如多拿的饰品。
- 功能只针对快捷栏当前选中的物品，并要求二次确认。
- 用户进一步要求尽量符合官方保存逻辑，不采用绕过存档、不可恢复的硬删除。
- 图片转写：本问题没有新截图。

### 官方调用链事实

- 所有留存官方构建都公开 `DolocAPI.SelectedItem`、`SelectedItemIndex` 和
  `DestroyItem(int index)`。
- `DolocAPI.DestroyItem(index)` 调用原生 `InventorySystem.Take(index)`；
  `LinearInventory.Take` 清空精确槽位并通过 receiver/emit 更新背包 UI。它没有
  直接写存档文件，也没有调用 `SaveGame`。
- 官方背包 `EquipmentBarUiState.DestroyItem` 先显示问题框，确认后调用同一个
  `DolocAPI.DestroyItem(currentIndex)` 并播放删除音效。官方 UI 之所以不能处理饰品，
  是它在进入问题框前用 `IsItemDisposable` 拦截；不是底层槽位删除不支持。
- 官方丢弃 `DisposeItem` 是另一条路径：它要求物品可丢弃、在世界中创建 DropItem，
  再从背包取走。对本需求调用它会继续被原限制拦截，也不能达到“清除多余物品”。

### 推荐执行语义

- Y 键控制台提供一个独立按钮。第一次点击只读取并显示当前快捷栏槽位的物品标题、
  ID、数量和风险提示；默认焦点落在取消，不执行原生变更。
- 确认快照只保存在当前进程内，至少绑定：当前 SaveLoaded 会话、背包对象、
  `SelectedItemIndex`、物品对象引用、ID 和数量。控制台关闭、SaveLoaded、
  ReturnedToTitle、owner deactivation 或再次发起请求都清除旧快照。
- 第二次确认时重新读取上述事实。槽位、选择、对象引用、ID 或数量任一变化就取消并
  提示“目标已变化”，不得按 ID 去全背包搜索另一个同名栈。
- 只接受非负的普通背包槽位；`-1/-2` 的 active/drone 特殊选择、空槽和非背包对象
  都是安全 no-op。第一版拒绝内部仍有物品的容器，避免一次确认级联删除嵌套库存。
- 确认后调用官方 `DolocAPI.DestroyItem(index)`，删除该槽位的整个栈，而不是一件；
  调用后必须确认该槽位为空。产品只绕过 `IsItemDisposable`，不直接改原生数组，
  不按 ID 扣除其他栈，也不伪造掉落物。
- 删除结果、标题、ID、数量和“尚未原生保存/已经进入 Working 状态”写入可见状态与
  产品日志。底层返回 `void`，后置条件失败时报告失败，不进行第二套反射写字段补救。

### 官方保存语义

- 背包槽位是 `save-bound gameplay state`。确认删除后，该物品只从当前运行中的
  Working inventory 消失；只有之后原生 `SaveGame` 成功才进入新的 Committed 存档。
- 若玩家在没有成功原生保存的情况下返回标题、退出或重载，物品从上一次官方提交
  恢复。这是本功能的恢复边界；产品不新增一个会抢先提交的“永久删除”文件。
- 不自动调用 `SaveGame`，不调用 Y 控制台 InstantSave，不写 sidecar，不建立删除
  journal，也不在下一次存档加载时重放。这样避免“原生未保存但 DTMAPI 已经永久删掉”
  或 journal 重放造成的丢失/重复。
- 二次确认不是进程内 Undo。确认后若尚未保存，恢复方式是放弃本次未提交的游戏
  Working 状态并重新加载；一旦后续原生保存成功，删除与其他当天玩法变化一起提交。

### 验收点

- 聚焦 source/unit：空槽、负索引、槽位切换、同 ID 不同对象、数量变化、跨
  SaveLoaded/title/close 的过期确认全部拒绝；稳定快照只调用一次官方
  `DestroyItem`，且不出现 archive 文件写入、sidecar、自动保存或按 ID 全局扣除。
- `NoNativeSave`：在 UI 第十存档的隔离副本中生成一个明确不可出售/不可丢弃的
  测试物品，确认后当前槽位为空；不保存退出并冷重载后物品恢复，目标 archive
  和 committed sidecar 的 length/hash/mtime 不变。
- `NativeSaveExpected`：在独立可处置 fixture 中确认删除同类物品，经过普通原生
  保存成功和冷重载后该精确槽位栈保持为空；其他同 ID 栈、装备位、容器和经济状态
  不变。不得用 live Steam AutoCloud 存档或测试后的字节写回冒充验收。
- UI 第一次动作和第二次确认必须可被键鼠/控制器清楚区分，确认文案必须显示完整
  栈数量，并明确“成功原生保存后提交”。

### Blocker 判定

- 若当前官方构建缺少 `DestroyItem(int)`，或调用后不能通过原生 receiver 更新并
  验证精确槽位为空，则阻止实现；不得退回直接改 `LinearInventory` 私有数组。
- 若确认状态无法在 UI 关闭、存档切换和返回标题时可靠清除，则阻止 destructive
  UI 上线。
- 若测试只能证明当前内存槽位为空、不能同时证明未保存恢复和原生保存提交，则该项
  仍是部分实现，不能标记 player/save verified。

## 路线图插入位置

保留 `20260831-0007` 的十五项技术债编号，不把两个产品功能伪装成性能清理。推荐
执行顺序如下：

1. 先完成现有路线图 `1..4`：Catalog chrome/搜索/来源/分类/分页结构与稳定 listener
   复用，以及 binder/input 集合 O(1) 移除。当前筛选状态本身已经能跨普通关闭重开
   保留，前置项是结构生命周期，不是重新发明状态持久化。
2. Feature F1：范围催熟 ProductNative 恢复。先内部化配置、请求/结果和三类原生
   调用，停止当前产品对 public `CropMaturityResult` 的消费；不新增 Crop Hook。
3. Feature F2：当前快捷栏整个栈的确认式原生销毁。独立 Update 实现确认快照、
   `DestroyItem` 调用和两种官方保存结果，不与 F1 混成一个运行验收。
4. 继续现有 `5..6` 的本地化标签缓存和传送静态/动态目录分离。
5. 执行 `7..10` 的 `0.3.1` Compatibility 物理退役。F1 先移除当前产品的催熟 DTO
   依赖，使这一退役能证明实际消费者归零，而不是删除后再给产品补洞。
6. 最后执行 `11..15` 的机械清理、十九 Patch 生命周期、强类型 UI、负反射缓存和
   breadcrumb 异步写盘；三个 partially optimized carry-over 仍按原记录保留。

两个功能各自使用独立 bounded Update 和风险相称的测试。它们不等待全面强类型 UI
改造，也不应与整个 Compatibility breaking removal 合成一个不可回滚的大提交。

## API / Hook 安全条款

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

本 Review 已找到两项当前 native owner：催熟由三类原生 `DEBUG_SetLevel` 责任函数
持有，槽位删除由 `DolocAPI.DestroyItem -> InventorySystem.Take` 持有。它们支持
ProductNative 实现，但不构成公开 API 稳定性或新增 SharedNative Hook 的证明。

## 本轮验证分类

- 完成：当前源码、Git 历史、现有 Crop API、配置菜单能力、Public API Matrix、
  0.3.1 退役 Review，以及十个留存官方构建中的 Crop/Inventory/DestroyItem 方法体
  对照。
- 未运行且本轮不需要：构建、Unit、游戏、原生保存、包、Workshop 和玩家手测。
- 后续 Y 键控制台运行测试固定使用 UI 第十存档（runner `-SaveSlot 10`、原生索引
  `9`）；任何 NativeSaveExpected 均须使用与实时 AutoCloud 隔离的可处置 fixture。
