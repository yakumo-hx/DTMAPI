# Manual QA Review: 0.2.8 Follow-Up, Animal UI, Y Console, Mine, Equipment Slots, More Saves

- 时间：2026-06-06 04:28:05 +08:00
- 来源：用户手测反馈；截图显示 Y键控制台 0.2.8、官方电力面板、MoreEquipmentSlots 配置页。
- 范围：只做审查和代码级分析，不实现，不更新 `readme.md`，不输出 `/goal`。
- 禁止事项：不回退当前工作树；不复制 DLKsmapi 实现；本记录仅作为后续 readme/goal 转化依据。
- 审查记录：`docs/reviews/manual-qa/2026/20260606-0002-028-manual-qa-followup-review.md`

## 问题 1：牧铃信息显示切换动物仍闪一次“心情”

原始反馈：
- 牧铃信息显示 mod 点击切换动物仍然会出现一次“心情”。
- 需要详细分析 DLKsmapi 和其对应牧铃信息显示如何实现。

审查记录：
- 用户确认事实：0.2.8 仍未消除切换动物时先显示原生“心情”的瞬间。
- 截图/日志观察：本轮无新牧铃截图；历史截图中原生详情面板右侧在隐藏产物条出现前会显示 mood row。
- 代码/文档事实：
  - DLKsmapi mod 使用 `helper.Experimental.Animals.ViewerRendering += OnAnimalViewerRendering`，在事件中选择最佳隐藏产物并调用 `e.AddProgressBar(...)`：`E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\src\AnimalHusbandryProgressPlugin.cs:61`, `:77`, `:95`。
  - DLKsmapi runtime patch `AnimalViewer.OnShow` 的 prefix/postfix；prefix 隐藏旧缓存条，postfix 触发 `ViewerRendering` 并 `ApplyAnimalViewerProgressBars(...)`：`E:\Python_project\DLK\src\smapi\DolocTownSMAPI.Core\src\DolocTownSMAPIPlugin.cs:761`, `:918`, `:932`, `:948`, `:949`, `:1313`。
  - 当前 DTMAPI 仅在 `AnimalFullInfoData` 构造后保存 renderRows，然后在 `AnimalViewer.Show` postfix 里 `RenderAnimalProgressOverlay` 克隆 moodBar：`src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:64`, `:69`; `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:3304`, `:3357`。
  - 当前 DTMAPI 状态文字明确是 `independent cloned ProgressBar`、`moodOverride=False`，说明它不替换原生 mood row，而是在 Show 之后追加/覆盖视觉层。
- Codex 推断：闪一次“心情”的根因是 UI 生命周期时序，而不是产物数据读取失败。原生 `AnimalViewer.Show` 先刷新 moodBar，DTMAPI postfix 之后才清旧 overlay、克隆新条、刷新文本；切换动物时至少一帧会暴露原生 mood row。
- 反证/未证实：未证明 DLKsmapi 完全无闪烁；但它用 cache + prefix 清理 + runtime-managed progress bars，比 DTMAPI 当前“数据构造后等待 Show postfix 克隆”更接近单生命周期渲染。
- 归属：DTMAPI GameBridge / AnimalViewer API；牧铃 mod 只是消费者。
- 需要更新：后续实现需更新 readme 任务、debug issue、hook map、smoke matrix；若改 API，更新 public API matrix。
- 验收点：第三存档连续切换 5 只动物，右侧详情从第一帧起只显示隐藏产物条或空状态，不出现“心情”闪烁；录屏或逐帧截图证明。
- blocker 判定：若 `AnimalViewer` 原生绘制无法在同帧前置拦截，必须保留 blocker，不能用 smoke “最终出现产物条”标 complete。

## 问题 2：Y键控制台左侧筛选列未随物品行数扩展，物品贴图不居中

原始反馈：
- Y键控制台增加行数后，左侧两列筛选没有增加行数。
- 物品贴图现在不居中。
- 图片转写：Y键控制台 0.2.8；物品区已显示更多行，左侧“来源/子分类”仍只显示较少按钮并留空；物品格内图标位置看起来偏移。

审查记录：
- 用户确认事实：0.2.8 UI 增加了物品网格容量，但没有同步左侧来源列/分类列容量；图标视觉中心偏离格子中心。
- 截图/日志观察：截图中物品网格更长，底部翻页在低处；来源列、子分类列没有填满相同高度。
- 代码/文档事实：
  - 物品页 `PageSize = 35`：`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:379`。
  - 来源列仍 `sourcePageSize = 12`：`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:389`。
  - 分类列仍 `categoryPageSize = 11`：`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:432`。
  - 物品格尺寸为 `100x74`，图标固定放在 `15,-15,60,60`：`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:610`, `:639`。
- Codex 推断：左侧问题是布局常量未同步；图标不居中很可能是图标区域和 cell 尺寸/锚点不一致，且部分物品 sprite 自身透明边界不同，固定 `15,-15,60,60` 不能保证视觉居中。
- 反证/未证实：未用截图像素测量具体偏移方向；也未检查 `AddImage` 内部 pivot/anchor 实现。
- 归属：Bootstrap UI `ReflectedDebugConsoleUi`。
- 需要更新：后续实现需更新 readme 任务和 debug/smoke matrix，增加 UI 截图验收。
- 验收点：Y 控制台左侧两列按钮数量/翻页位置与扩展后的物品网格高度协调；所有物品 icon 在格子中视觉居中，至少检查 10 个不同 sprite。
- blocker 判定：若官方 sprite 透明边界差异导致无法通用居中，需记录采用统一 icon viewport 或 per-sprite bounds 的方案，不可只调一个固定偏移。

## 问题 3：矿井预览未修，矿井未真正耗电

原始反馈：
- 矿井预览没修。
- 矿井没有真正耗电，这个一直以来都没有真正耗电；不排除是不在游戏内显示。
- 图片转写：官方电力面板显示 `发电量/用电量(2.5/0.0)`、`电池储量/容量(3550/4500)`，说明当前可见用电量为 0.0。

审查记录：
- 用户确认事实：放置预览阶段矿井仍不是期望的 2 倍预览；官方电力面板不显示矿井耗电。
- 截图/日志观察：电力 UI 的 `用电量` 为 `0.0`，和 Mine 配置/状态中的每周期耗电 10 不一致。
- 代码/文档事实：
  - Mine mod 定义 `AllowElectricMode = true`、`DefaultMode = "electric"`、`ElectricModePowerCostPerCycle = config.ElectricPowerCostPerCycle`：`testmods/MineMod/ModEntry.cs:74`, `:76`, `:86`。
  - DTMAPI 只在生产周期中设置 `entry.LastElectricPowerCost`：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5123`, `:5131`。
  - 生产代码之后直接选产物、放入 storage、记录消息，没有看到接入 `DolocAPI.archiveHandle.MainFarm.DM_electric`、电池扣减或官方 `ElectronicComponent` 的调用：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5133-5169`。
  - 预览修复只 patch `EquipmentBuilder.CreateIndicator/TurnIndicator`，读取 `indicatorRenderer.indicator.transform` 并按 equipment id 调 scale：`src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:266`, `:271`; `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4981`, `:4990`, `:4991`。
- Codex 推断：矿井“耗电”目前是 DTMAPI 内部状态字段，不是官方电力系统的负载；所以电力面板不显示是合理结果。预览未修说明当前 hook 可能没有覆盖手持/待放置预览动画的实际对象，只覆盖了 builder indicator 的一个子路径。
- 反证/未证实：未检查官方机器如何注册 `ElectronicComponent`/`AffectorElectric` 到电网；也未证明电池数值是否在周期后下降。
- 归属：DTMAPI GameBridge Machine API；MineMod 配置只是传参。
- 需要更新：后续实现需更新 readme 任务、debug issue、hook map/API matrix、smoke matrix，增加官方电力 UI 验收。
- 验收点：矿井放置预览阶段就是 2x；放置后官方电力面板 `用电量` 包含矿井负载，生产周期真实消耗电池/电力；无电时不生产且显示/日志给出原因。
- blocker 判定：若无法安全接入官方 `DM_electric`/`ElectronicComponent`，必须停止并记录 blocker，不能继续用 `LastElectricPowerCost` 冒充真实耗电。

## 问题 4：MoreEquipmentSlots 配置项过多，额外槽有存档污染/复制风险

原始反馈：
- 更多装备栏要去掉除了启用外的所有按键；安全恢复默认打开，不要放开关。
- 显著安全问题：放入帽子后没有存档，数据依然保存；重进存档出现两个帽子。
- 关闭 mod 不能热关闭；可能关闭 mod 后装备栏物品回退背包没有实现。
- 功能实现可能需要关闭-保存后扫描存档数据，下一次进入存档放回背包或地面。
- 图片转写：配置页显示 `启用`、`额外栏位`、`装备物品 ID`、`装备到空槽`、`恢复全部额外槽`、`安全恢复` 等控件。

审查记录：
- 用户确认事实：饰品效果已生效，但额外槽存储和恢复存在严重安全风险；配置页不应暴露调试按钮/恢复开关。
- 截图/日志观察：配置页仍暴露多个非玩家必要选项，和“只留启用”不一致。
- 代码/文档事实：
  - MoreEquipmentSlots 配置页仍注册 `Extra slots`、`Equip item id`、`Equip first empty`、`Recover all extra slots`、`Safe recovery`：`testmods/MoreEquipmentSlotsMod/ModEntry.cs:38-42`。
  - 放入额外槽后会立即 `SaveEquipmentSlotStorage(ownerId)`：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1133`, `:4457`。
  - 存储路径是 DTMAPI 配置目录 `equipment-slots-<owner>.json`，不是游戏存档事务：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1455`。
  - 装备时会先扣原生背包 `TryCostNativeBackpackItem(...)`，再写 DTMAPI storage：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1118`, `:1133`。
  - 当前校验允许 `ItemPassive` 和带 `ItemFunctionHat` 的 `ItemHat`，即更多槽当前支持饰品和帽子技能，但不应加载帽子贴图外观：`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1702-1748`。
- Codex 推断：复制帽子的根因是事务边界错位。未保存时原生背包状态会随读档回滚，但 DTMAPI 配置目录已经持久化额外槽物品；下次进档同时拥有原背包帽子和额外槽帽子，形成复制/污染。
- 反证/未证实：未检查当前恢复逻辑是否在官方禁用 mod 后仍能运行；如果普通 mod 禁用后不加载，恢复必须由 DTMAPI Core/GameBridge 的 orphan recovery 执行，而不是 mod 自己执行。
- 归属：DTMAPI GameBridge EquipmentSlots API + MoreEquipmentSlotsMod 配置 UI；这是保存事务/恢复策略问题，不只是 mod UI 问题。
- 需要更新：后续实现需更新 readme 任务、debug issue、API matrix、smoke matrix；必须增加“未保存重进不复制”验收。
- 验收点：放入帽子/饰品后不保存直接退出重进，不出现额外槽物品且原背包状态与未保存前一致；保存后重进额外槽存在且不复制；关闭/禁用 mod 后安全恢复到背包或地面，背包满也不丢失。
- blocker 判定：如果无法把额外槽状态绑定到游戏存档保存事务，必须禁用帽子/饰品放入或标 blocker，不能保留会复制物品的实现。

## 问题 5：新增更多存档 mod

原始反馈：
- 额外做一个更多存档 mod。
- 走官方路径，只在官方存档界面翻页、读取更多存档。
- mod 关闭不删除本地存档文件。
- 确保存档可运行。

审查记录：
- 用户确认事实：这是新增需求，不是 0.2.8 回归。
- 截图/日志观察：本轮无存档界面截图。
- 代码/文档事实：
  - 官方 `GameManager.archiveFileCount = 6`：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/GameManager.cs:28`。
  - `LocalSave` 的 `dataFileCount` 直接来自 `DolocAPI.gameManager.archiveFileCount`，文件名格式为 `doloc-archive-{0}.data`：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameData/LocalSave.cs:37`。
  - `GameDataUiState.Show` 使用 `DolocAPI.GetAllArchiveInfos()`，`GameDataPanel.Render(BaseArchiveData[] infos)` 按数组长度渲染，panel 内部会 `SetCapacity(num)`：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameDataUiState.cs:155`; `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/UI/GameDataPanel.cs:75`。
  - `GameDataSlot.SetIndex` 会显示 `#index+1`，理论上支持超过 6 个槽的编号：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/UI/GameDataSlot.cs:74`。
- Codex 推断：更多存档很可能可以走官方路径：提高/patch `archiveFileCount`，让 `LocalSave.GetAllArchiveInfo` 返回更多槽，官方 `GameDataPanel` 自身具备按数组容量扩展的基础。需要评估 UI 导航和滚动是否能容纳更多槽。
- 反证/未证实：未验证 `DolocGridUI<GameDataSlot>` 对大容量的滚动/翻页表现；未确认复制/删除/新建逻辑在超过 6 槽后是否正常。
- 归属：新增官方-local mod + DTMAPI GameBridge save UI API/hook。
- 需要更新：后续实现需更新 readme 任务、hook map、debug/smoke matrix；如果新增 API，更新 public API matrix。
- 验收点：官方存档界面显示超过原 6 槽，可翻页/滚动；新建、读取、复制、删除在扩展槽可用；禁用 mod 后本地额外存档文件保留，不删除，不破坏前 6 槽。
- blocker 判定：如果官方 UI 无法稳定支持扩展槽，必须保留只读/备份方案或 blocker，不允许改写/迁移现有存档文件导致不可读。

## 测试反馈理解

- 用户确认事实：0.2.8 的饰品效果、矿井配方改变已手测确认；本轮关注仍未到位的 UI 生命周期、布局、真实耗电、安全恢复和新增存档功能。
- 截图观察：Y 控制台 0.2.8 的物品区与筛选区高度不匹配；官方电力 UI 未计入矿井耗电；MoreEquipmentSlots 配置页仍暴露调试/恢复/额外槽数量控件。
- Codex 推断：最严重风险是 MoreEquipmentSlots 的跨存档/未保存持久化污染；其次是 Mine API 没有接入官方电力系统；牧铃闪烁属于 UI 生命周期时序。

## 问题分组

- UI：牧铃详情第一帧闪“心情”；Y 控制台列高/图标居中；MoreEquipmentSlots 配置页收敛。
- API/GameBridge：AnimalViewer API、Machine API 真实电力接入、EquipmentSlots save-bound storage、更多存档 save UI API。
- Hook：AnimalViewer.OnShow prefix/postfix、EquipmentBuilder preview path、SaveGame/LoadGame transaction boundary、GameDataUiState/GameManager archive count。
- Config：MoreEquipmentSlots 仅保留启用；安全恢复默认强制开启。
- 测试/证据：需要从 smoke 扩展到“未保存重进”“官方电力面板”“第一帧 UI flicker”“扩展存档读写”。

## 边界约束

- 必须做：玩家可见问题必须在第三存档或真实存档界面验证；MoreEquipmentSlots 不得复制/污染物品；Mine 不得用内部字段冒充真实耗电。
- 禁止做：不要复制 DLKsmapi 代码；不要用 build/smoke 通过代替手测点；不要删除额外存档文件；不要让禁用 mod 导致物品丢失。
- 可选做：更多存档 UI 可先选择翻页或滚动，只要官方存档界面内可用。
- blocker：若官方电力、存档事务、AnimalViewer 前置渲染任一无法安全接入，后续 goal 不得 complete，必须留下日志和代码路径证据。
