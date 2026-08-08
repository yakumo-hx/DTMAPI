# MoreEquipmentSlots 与官方动态饰品栏复查

- Review ID: `20260806-0001`
- Date: `2026-08-06`
- Status: `recorded — current UI conflict confirmed; Product 1.0 publication blocked`
- Scope: current player confirmations, unchanged-product smoke disposition, MoreEquipmentSlots 1.0 native equipment/UI ownership
- Source: user manual-test feedback and request for a fresh code-path review
- Owning Update: [20260802-0001 DTMAPI 0.6.0 authority roadmap](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Related records: [MoreEquipment Branch B design](../../code/2026/20260805-0003-moreequipment-branch-b-minimal-migration-design.md), [ISSUE-021](../../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md), [focused Hook map](../../../hook-map/focused/MoreEquipmentSlots.md), [native-owner domain 08](../../api/native-owner-domains/08-hats-accessories-equipment-slots.md)

本记录按用户本轮反馈顺序冻结事实。它区分用户手测、既有 smoke 和本轮只读静态审查，不把三者互相冒充；本轮没有启动游戏、修改玩家环境或新增 smoke 运行。

## 问题 1：AutoFishing、MoreSaves、DebugConsole 手测正常

### 原始反馈

- `AutoFishing`、`MoreSaves`、`DebugConsole` 手测正常。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：当前已安装候选的三项实际玩家路径均正常；此前 DebugConsole 旧 Local 包在 build `24585411` 上的 `ResolveTargets` 报错不再复现，MoreSaves 的当前包也不再表现为“只有 12 槽 UI、额外档不可读”。
- 证据边界：这是用户人工结果，不是新的 Codex game-smoke receipt。AutoFishing 的行为/Manager 自动门、MoreSaves 官方角色迁移与 fixed-12 冷进程矩阵、DebugConsole 的 Candidate11 ISSUE-011 smoke 仍分别拥有自动证据。
- 处置：三项当前用户手测门记为通过；不新增 smoke-matrix 行，也不扩大用户实际操作范围。

## 问题 2：五项无实质变化产品沿用既有 smoke

### 原始反馈

- `ActionSpeed`、`OneActionComplete`、`FishBreedingAssistant`、`AnimalHusbandryProgress`、`ChestLocatorEnhancer` 无实质变化；smoke 通过即可标记通过。
- 图片转写：无截图。

### 审查记录

- `ActionSpeed`：当前行为源码在后续候选中已有八阶段 `NoNativeSave` 行为/recovery 门；较早 `GAME-SMOKE/20260722-141220` 还拥有 Tool、ConfigApply、Interaction 与九目标 exact-owner cleanup。后续只涉及已验收实现和发布投影，不要求本轮再做人工重放。
- `OneActionComplete`：行为源码在 `GAME-SMOKE/20260722-141220` 之后只有版本/发布投影变化；该 run 已覆盖 partial energy、配置 reload 与两目标 owner cleanup。
- `FishBreedingAssistant`：`GAME-SMOKE/20260722-180502` 覆盖当前产品行为与 exact-owner cleanup；其后只有版本/发布投影变化。
- `AnimalHusbandryProgress`：`GAME-SMOKE/20260723-115821` 位于最后一次 product-local refresh 优化之后，覆盖三次动物选择、native close、标题恢复与四 patch/三 target exact cleanup。
- `ChestLocatorEnhancer`：`GAME-SMOKE/20260723-224200` 绑定最后一次行为修正后的 committed candidate；用户上一轮另已人工确认远端箱子识别与真实扣料正常。
- 处置：按用户授权把五项记为当前通过，不新增专用 smoke。这里的“通过”引用上述各自现有证据，不伪造本轮用户逐项手测。

## 问题 3：Manbo 与 Zoom 不进入本轮复查

### 原始反馈

- `Manbo` 不用管，`Zoom` 看起来也不用管。
- 图片转写：无截图。

### 审查记录

- `Manbo` 继续使用既有 retained 普通激活边界；本轮不新增音频触发、可听替换或专用 cleanup 门。
- `Zoom` 继续保持当前发布 tree 与既有玩家验收；本轮不重开 Panorama、相机生命周期或重打包。
- 处置：两项均不构成本轮待处理项，也不据此新增行为声明。

## 问题 4：官方从三个装备栏位扩为四个时，MoreEquipmentSlots 1.0 如何处理

### 原始反馈

- 官方会随游戏进展开放额外饰品栏。
- 需要重新查代码，判断 UI 是否冲突，以及这是否是 MoreEquipmentSlots 重做的直接原因。
- 当前官方显示三个栏位；如果某节点变成四个，MoreEquipmentSlots 如何处理。
- 图片转写：无截图。

### 当前原生事实

本轮对本机实际 build `24585411` 的 `Assembly-CSharp.dll` 做了只读定向反编译，并从当前 `uu` Addressables bundle 只读解析 `EquipmentBarPanel`。当前 DLL SHA-256 为 `68AEA11BD040805712673E506D1D429BC71338DD52D2659683ADC60B62715739`；当前 `uu` bundle 为 `12AC9F5271E4E2CC92627155EC256FB9AABB27755819C49C0092572CB104AAB8`。没有复制或提交官方源码/资源。

- `AgentEquipmentManager` 不再拥有旧固定 `passiveItem1/passiveItem2` 模型。当前持久状态是 `passiveItems[]`，新档初始长度为 `1`。
- 官方 `add_accessory_slot` 命令读取当前数组长度并调用 `SetPassiveSlotCount(length + 1)`。扩容会保留旧数组前段已有物品、增加空原生槽，然后重算装备参数。
- `AccessoriesBar` 用 `slotRoot` 下的 `ObjectPool<AccessorySlot>`；`RenderPassiveItems` 把 pool 数量直接调整为 `passiveItems.Length`。`EquipmentBarUiState` 每次显示时都按当前数组重新渲染。
- 因而玩家看到的“帽子 + 主动饰品 + 一个被动饰品”三个栏位，在官方扩栏后会成为“帽子 + 主动饰品 + 两个被动饰品”四个栏位。第四个是完整原生槽，有原生索引、装备事务、保存和导航。

### MoreEquipmentSlots 1.0 当前行为

- 产品仍固定拥有三个 Product-v3 边车槽；官方扩栏不会改变、截断或迁移这三个记录。
- UI 重建先尝试反射旧字段 `passiveItem2`、`passiveItem1`。当前两个字段都不存在，因此必然退回 `positiveItem` 作为模板。
- 产品把三个克隆实例化到 `positiveItem.transform.parent`，也就是 `AccessoriesBar` 的顶层 grid；它没有把克隆加入官方 `passiveItemPool` 或 `slotRoot`，也没有把克隆加入 `allSelectablesArray`。
- 当前精确 prefab 中，顶层是单行 `GridLayoutGroup`，cell `112×112`、水平 spacing `12`；直接布局子项依次是帽子、主动饰品、官方被动槽容器，箭头明确 `IgnoreLayout=true`。官方容器内部是 spacing `12` 的 `HorizontalLayoutGroup`，但容器本身只占顶层一个 `112` 宽 cell。
- 初始一个官方被动槽正好落在容器的第一个位置；三个产品克隆作为后续顶层 cell 排在其右侧。官方扩成两个被动槽后，第二个原生槽在容器内向右移动 `112 + 12`，恰好进入第一个产品克隆占用的顶层 cell。

### 结论

1. **数据不冲突。** 官方扩容只改原生 `passiveItems[]`；产品三个槽仍在自己的 Working/Committed Product-v3 边车中。官方第四槽不会覆盖产品物品，产品也不会阻止官方数组从 1 扩到 2。
2. **UI 几何确定冲突。** 官方第二个被动槽与 MoreEquipment 第一个克隆占据同一横向位置。产品克隆是后追加 sibling，通常还会覆盖原生槽的显示/鼠标命中；精确绘制与 raycast 前后关系仍可由后续游戏门观察，但坐标重叠本身已由当前布局确定。
3. **导航也确定不完整。** 官方 `allSelectablesArray` 只含帽子、主动槽与原生 pool；三个产品克隆不在其中。键盘/手柄导航会经过新增原生槽，却跳过产品槽。现有 smoke 只证明固定三个克隆可创建/关闭和 owner 可清理，没有执行官方 `1 -> 2` 被动槽扩容。
4. **这是 MoreEquipmentSlots 1.0 当前必须重做 UI 层的直接原因。** 它不是 2026-08-04 Branch B 最初记录的直接触发点；当时触发点是 `LocalSave` archive-family 与 attack/shield seam 变化。动态饰品数组/对象池是此前审查遗漏的另一项独立原生变化，现在单独足以阻断 1.0 publication。
5. **不需要因此推翻已经完成的存档事务重做。** Product-v3 三槽、正常保存/no-save、cold recovery 与旧 ABI 出口仍有价值；需要重做的是 UI composition 和原生槽增长协作，而不是再造 sidecar/journal。

### 最小后续边界

- 保留三个产品边车槽与旧 `0.3.1-dtmapi` recovery/ABI，不把产品物品自动塞入原生数组，也不替玩家调用 `SetPassiveSlotCount`；否则会篡改官方进度并把产品停用/恢复问题写入原生存档。
- 产品 UI 必须以当前 `passiveItems.Length` 为前缀，使用官方被动槽样式或独立、layout-safe 的产品容器，在官方 `RenderPassiveItems` 完成后追加三个产品槽。
- 产品槽必须进入同一导航/焦点生命周期；官方从 1 扩到 2 后，显示顺序应是全部原生槽，再接产品槽 0--2，不得复用旧 `passiveItem1/2` 字段或主动槽模板兜底。
- 聚焦测试至少覆盖 native count `1 -> 2`、四个官方栏位可见、三个产品栏位仍可见且几何不重叠、鼠标与键盘/手柄导航、两类槽分别装备/卸下、关闭重开、产品禁用清理，以及既有 save/no-save/cold recovery 不回退。
- 在该修复和最小当前游戏 UI 门通过前，MoreEquipmentSlots 1.0 保持局部发布阻断；其他已通过产品不受此阻断牵连。
