# MoreEquipmentSlots 与官方动态饰品栏复查

- Review ID: `20260806-0001`
- Date: `2026-08-06`
- Status: `recorded — one-to-two-slot candidate invalidated by three-slot player evidence; Product 1.0 publication blocked`
- Scope: current player confirmations, unchanged-product smoke disposition, MoreEquipmentSlots 1.0 native equipment/UI ownership
- Source: user manual-test feedback and request for a fresh code-path review
- Historical Owning Update: [20260802-0001 DTMAPI 0.6.0 authority roadmap](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Current Implementing Update: [20260811-0001 MoreEquipmentSlots 1.0 direct replacement](../../../updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md)
- Related records: [MoreEquipment Branch B design](../../code/2026/20260805-0003-moreequipment-branch-b-minimal-migration-design.md), [ISSUE-021](../../../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md), [focused Hook map](../../../../hook-map/focused/MoreEquipmentSlots.md), [native-owner domain 08](../../../../reviews/api/native-owner-domains/08-hats-accessories-equipment-slots.md)

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

## 2026-08-11 发布前复测补充

### 问题 1：两个饰品袋后 Product 三槽消失，当前候选不能发布

#### 原始反馈

- 功能性手测没有发现其他问题，但当前版本不能发布。
- 使用一个官方饰品袋时正常；使用两个后，官方出现饰品 `1 / 2 / 3`，MoreEquipmentSlots 的三个 Product 槽全部消失。
- 官方没有限制继续使用饰品袋；本次暂不承诺任意数量兼容，但至少要保证使用两个至三个袋子时 Product 三槽继续自然延后，不得消失。
- 图片转写：本次无新增截图；用户要求以本次日志为证据。

#### 当前日志与源码事实

- 玩家本次 `Player.log` 更新时间为 `2026-08-11 14:52:14 +08:00`。`14:49:08.533` 进入 `EquipmentBarUiState` 后，Product 精确记录：`MoreEquipmentSlots Product UI failed closed: The native accessories bar must expose one or two reviewed passive slots.`；下一行仍为 `clones=3 visible=False layoutBlocked=True listeners=30`。因此三个 Product 克隆没有丢失，五 Hook 和 render callback 也没有失效；旧审查上限主动把整行隐藏。
- `AccessoriesBarRenderPassiveItemsPostfix` 把官方 `RenderPassiveItems(Sprite[])` 参数的数组长度原样传给 Product。`MoreEquipmentSlotsUiLayoutPolicy.MaximumOfficialPassiveSlots` 当前硬编码为 `2`，任何 `officialPassiveCount > 2` 都返回上述 invalid reason。
- public `24650773 / 1.00.03` 的 `EquipmentBarUiState` 以 `AgentEquipmentManager.passiveItems` 全数组调用 `RenderPassiveItems`；新档基础长度为 `1`。官方 `add_accessory_slot` 每次读取当前长度并调用 `SetPassiveSlotCount(length + 1)`，没有二槽或三槽上限。因此两个袋子对应官方被动槽总数 `3`，用户要求的三个袋子对应总数 `4`。
- 当前 Product 没有逐帧布局路径。几何只在原生 `RenderPassiveItems` postfix 与既有 UI/config 生命周期边界重算；把审查上限从二槽扩到四槽本身不会增加 `Update`、`LateUpdate`、`FixedUpdate` 或 frame-event 压力。

#### 根因与被否决候选

- 根因是审查与验收矩阵只覆盖官方被动槽 `1 -> 2`，并把 `3` 写成预期 fail-closed；不是官方数组、Product-v3 sidecar、装备事务或 Product clone 生命周期损坏。
- exact 候选 `0F4BD87B…7527F` 及上传树 `D02C035B…AC85` 的运行时、save/cold 和完整 Release 结果只对该旧边界成立。玩家在提交前发现缺口，因此这些字节立即失去发布资格；不得把已有 PASS 外推到官方三槽或四槽。
- Catalog 的临时 existing-item 上传例外必须先撤销，官方上传目录必须在共享 Runtime lock 下恢复到 retained 0.3.1。Steam 订阅目录和玩家存档不需要、也不得因本缺陷修改。

#### 修正边界与验收

- 用户随后把有界目标扩大为四个饰品袋：支持官方被动槽总数 `1–5`，即基础一槽加零至四个饰品袋；第五个官方槽后再接三个 Product 槽，总计八个被动饰品 UI。三个完整 Product 槽始终读取实际最后一个官方槽的 settled Rect，并按原生 `112 × 112`、间距 `12` 继续向右；不写 `passiveItems`、不调用 `SetPassiveSlotCount`、不进入官方 pool。
- 追加截图转写：画面为 `2048 × 1152`；装备横排从帽子、主动饰品后继续出现大量官方被动槽，悬浮提示显示“饰品8”，最右端槽已被画面边界裁切。它证明原生数组/UI 能继续扩展，也证明“能生成”不等于“当前 viewport 内可安全承载”。因此官方被动槽总数大于 `5` 暂不纳入 Product 兼容承诺；继续记录明确错误并 fail-closed，不把本轮有界修正表述成无限饰品袋支持。
- focused/物理 fixture 必须覆盖 `1 -> 2 -> 3 -> 4 -> 5`、同一 Product row/三 clone 身份复用、导航为全部官方槽加三个 Product 槽，并把 `6` 保留为不受支持的 fail-closed 边界。
- 新 exact 候选必须在 public `24650773`、第三存档 `NoNativeSave` 下，对 `1920 × 1080` 和 `1024 × 768` 验收官方三、四、五槽动态顺延，且两个分辨率都必须包含五官方槽加三 Product 槽的最大八槽表面；三个 Product 槽必须可见、严格尾随、在 viewport 内且与官方装备、最大背包、关闭按钮和无人机 UI 零相交。任何一项不成立都继续阻断发布。

### 修正结果：四个饰品袋 / 最大八格边界已通过

- Product 的受支持官方被动槽数现为 `1–5`；第六个官方槽仍明确 fail-closed。物理夹具从 `1 -> 2 -> 3 -> 4 -> 5` 连续验证同一 Product row 和三个 clone 未重建、每次只按原生 `112 + 12` 向右移动、导航始终是完整官方前缀加三个 Product 槽；`6` 保持拒绝。源码没有新增 `Update`、`LateUpdate`、`FixedUpdate` 或 frame-event 路径。
- 新候选由 clean `5c0a1765` 和原 244 exact policy 生成：七文件 ZIP SHA-256 `E2EBF0C34DDB500422E906756B0BA1B1AB6864D820962A738BC34AED8620CFE8`，Product DLL SHA-256 `699E95BC05E79F67EE45D83C89D8119EB2BE723FF342CF2DA0CD0A2C7DBC8E31`，Advanced receipt SHA-256 `07ED617F1E87902B6A9668358F2CBA718EA753FD830111D457D3D293938AC920`。
- `GAME-SMOKE/20260811-151705` 在 public `24650773 / 1.00.03`、第三存档 `NoNativeSave` 中从头通过。QA 在同一进程内执行 `1 -> 1 -> 2 -> 3 -> 4 -> 5 -> 1`，最大状态是五个官方被动槽加三个 Product 槽；两个分辨率都记录 `Intersections=none`、`NativeTailOrder=exact`、`NavigationProjection=native-plus-three`、`ViewportContainment=exact`、`IgnoreLayout=true`、`PerFramePolling=none`。
- 人工逐图复核与矩形报告一致：`1920 × 1080` 最大 Product 行为 `[1223,616 -> 1583,728]`；`1024 × 768` 为 `[673.966,482.25 -> 895.669,551.224]`，最右端仍在 1024 像素视口内，Product 行底部与无人机区顶部之间约留 `9.854` 像素。五官方加三 Product 的八格横排、40 格背包和关闭按钮都完整可见；没有非无人机 UI 遮挡，也没有恢复抽屉或第二行。
- 同一运行还通过被动饰品、普通帽、盾帽、替换、盾值伤害/破碎、卸下、配置禁用/重启和返回标题。它没有请求 native save；在任何恢复动作前，第三档 current/prev/bak 与两个 committed-sidecar 路径的长度、SHA-256 和 mtime 均未变化。
- `GAME-SMOKE/20260811-152206` 随后在 AutoCloud 隔离 fixture 中用同一候选通过两次真实 native save、满盾与空 Product-v3 commit、物品清理和整棵 fixture 删除。独立 aggregate `GAME-SMOKE/20260811-152639` 又从头通过 `ColdPrepare/152457 -> ColdCommit/152549 -> ColdObserve/152639`；Product 禁用时只由冻结 recovery Host 恢复一次，最终进程不重放，存档/sidecar 在 cleanup 前不变。
- cold 的第一次编排在包预检处被正确拒绝，因为临时 local mount 把上传目录专属的 `workshop.json` 加进了七文件 SDK 包。该次没有形成 Product/save 失败；正式重跑严格区分“七文件 package/cold authority”和“未来上传时才有的八文件加 `workshop.json` authority”。

### 当前结论

用户本次指出的发布阻断已经关闭：零至四个饰品袋（官方被动槽总数一至五）下，三个 Product 槽都会自然挂在实际官方尾槽之后；最大八格在两种规定分辨率内通过。官方被动槽总数六及以上仍不在兼容承诺内，运行时会隐藏 Product UI 并记录明确错误，不会自动缩放、换行或修改官方槽池。当前仍不能提交 Steam：完整 Release、Catalog 新上传授权和锁内官方目录准备尚未完成。
