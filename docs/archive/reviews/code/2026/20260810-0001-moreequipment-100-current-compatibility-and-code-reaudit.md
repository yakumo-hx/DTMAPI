# MoreEquipmentSlots 1.0.0 直接替换、当前兼容与代码复审

## Metadata

- Review ID: `20260810-0001`
- Date: `2026-08-10`
- Status: `recorded — original drawer decision superseded by the lifecycle Update; publication blocked`
- Audited HEAD: `af0c1f13fd0c3e11af746988047ac2101521ee24`
- Source: 用户要求把 MoreEquipmentSlots `1.0.0` 作为 0.6 系列最后一个集中关闭项；随后明确它会在同一 Workshop 项上直接替换 `0.3.1`，要求三个位置保持原版本帽子/饰品语义，并依据玩家截图否决可能遮挡背包的第二行方案，先采用固定入口与无人机模态抽屉；完成该候选后，用户又依据新截图改为自然挂在当前已解锁官方尾槽之后，并明确删除展开栏但保留此前设计的历史记录。
- Scope: 当前正式版 public 静态兼容、现有 Product 源码、0.3.1 直接升级边界、UI/导航/装备语义与 incoming withdrawal 事务；本 Review 不修改 Runtime、Mod、Steam、游戏或玩家存档，也不授权上传。

## Inputs and authority

- [正式版 public 1.00.03 / build 24650773 差异审查](20260811-0001-public-10003-baseline-difference-audit.md)
- [冻结的 0.6 执行记录](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)（只作历史 handoff，不再接收进度）
- [MoreEquipmentSlots 第八个 Advanced 产品历史生命周期](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- [Branch B 重叠决策](20260804-0007-moreequipment-100-overlap-decision-gate.md)
- [动态官方饰品栏手测复查](../../manual-qa/2026/20260806-0001-moreequipment-official-slot-growth-review.md)
- [生产事务三轮复审](20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)
- [ISSUE-021](../../../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md)
- [MoreEquipmentSlots Hook map](../../../../hook-map/focused/MoreEquipmentSlots.md)
- [Product Catalog](../../../../../tools/release/dtmapi-product-catalog.json) 与 [current subscription manifest](../../../../../tools/release/current-subscription-manifest.json)

当前公开/订阅、已发布制品和未来上传授权必须继续由 Catalog、subscription manifest 及其最新发布 Update 分别裁决。冻结 Batch 6/0.5.5 annex 与冻结 0.6 路线都不能预授权本次发布。实现必须新建一个单一 `1.0.0` Update，并由该 Update 独占源码、测试、候选包、实机与发布后证据。

## 一、最终裁决

MoreEquipmentSlots `1.0.0` 的静态 public 兼容方向已经明确，但现有代码不能直接发布。必须在同一个 Product 生命周期内关闭三项 P1：

1. 当前 UI 仍按旧字段克隆并在每次 render 销毁重建，既与官方动态 passive owner 冲突，又没有正确导航和监听隔离；
2. 当前装备分类只按 `ItemPassive` / `ItemHat` 基类粗分，不能证明三个位置完整保持 `0.3.1` 的帽子与被动饰品语义；
3. backpack `CostItem` 与原生手持 buffer 在调用后异常、后状态不可读或矛盾 delta 时，还没有统一进入 incoming outcome-unknown 与保存隔离。

UI 方案冻结为：**官方饰品区右端一个独立固定入口；展开后在官方 `drone_panel` 内显示不透明、模态的三槽抽屉**。三个 Product 槽保持原始 `112 × 112`，不缩放、不换行、不进入官方数组/池。若任一分辨率仍遮挡非无人机 UI、抽屉越界，或无人机状态不能精确恢复，发布立即停止并重新决策；不自动退回第二行、缩放或修改官方槽池。

## 二、同一 Workshop 直接替换事实

| 轴 | 当前事实 | 本次约束 |
| --- | --- | --- |
| Workshop identity | `3744059735` | `1.0.0` 更新同一 item，不创建并行 Workshop 项 |
| UniqueID | `DTMAPI.MoreEquipmentSlotsMod` | 保持不变，玩家更新后直接运行新入口 |
| 当前订阅制品 | `0.3.1-dtmapi`，9 files / 539,565 bytes，retained tree `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6` | 只作为升级输入、ABI 与回滚证据，不是 1.0 实现模板 |
| 目标源码 | `1.0.0`，minimum Runtime `0.6.0`，Advanced/ProductNative | 最终包只能有一个 1.0 入口 DLL和对应 SDK receipt |
| exact author policy | `doloctown-24456188-moreequipmentslots-v1` | 继续用于构建；public 246 只作静态观察头 |
| release authority | 当前 `releaseStop` 为零上传授权 | 到冻结候选时才由 1.0 Update 写入唯一、精确、可消费的上传授权 |

发布不是新旧两个产品并存：Steam 更新后玩家在原位从 `0.3.1` 进入 `1.0.0`。因此必须保留现有 Product-v3 sidecar、旧 flat/pre-schema 迁移、存档指纹、cold recovery 与 exact-owner 冲突保护；候选包不得携带旧 `0.3.1` DLL、Host 产品字节、重复入口或第二套 schema/receipt。

## 三、当前 public 跨版本裁决

`24650773_public_76C24E / 1.00.03` 已在独立 Review 中完成身份一致的完整捕获。相对 `24585411_test_68AEA1` 只有 6 个无关代码文件发生方法体或私有字段变化；MoreEquipment 实际消费的 `AccessoriesBar`、`AgentEquipmentManager`、`EquipmentBarUiState`、shield/attack owner 和 LocalSave 路径、备份、云目录、`backupCount` 成员均未漂移。`LocalSave.GetArchiveDataAsString` 的正式版修订不是本产品消费者。

结论：

- 正式版字节未知这一静态 blocker 已关闭；
- 不新增 `24650773` policy，不把 public DLL 冒充 244 exact reference，不批量重签；
- 继续用 244 exact policy 编译，然后在真实 public 上走 `Drift -> type/entry/hook/product transaction` 与玩家 UI 验收；
- 静态兼容不能代替游戏行为证据。

| 世代 | UI owner | 本次支持策略 |
| --- | --- | --- |
| `23762374` | 固定 `passiveItem1/passiveItem2`，没有当前 `slotRoot` / pool / getter owner | 缺少五 Hook 任一目标时，在存档加载和 sidecar 修改前原子失败；不承诺兼容 |
| `24256979` 起 | `passiveItems[]`、`SetPassiveSlotCount`、`slotRoot`、`passiveItemPool`、动态 `allSelectablesArray` | 当前实现世代 |
| `24456188` | 与 242 同一相关 owner | exact Author SDK reference authority |
| `24567135`、`24585411`、`24650773 public` | MoreEquipment 消费成员未漂移 | 真实 drift 激活与 focused runtime evidence，不新增 policy |

## 四、玩家截图事实与 UI 决策

临时截图为 `1805 × 671`，不作为仓库资产提交；以下文字是其 durable evidence：

1. 官方帽子和主动饰品仍在角色信息区的上方一行；三个 Product 槽以完整大格显示在其下方第二行。
2. 第二行从角色面板左缘横跨到背包/库存背景：三个大格直接覆盖库存网格的左上区域，第三个空槽明显落在背包面板上方，已经不是独立装备区。
3. 继续按官方数组顺延虽然能把槽排出来，但在玩家可能取得最大官方 passive 数量时会进一步把 Product UI 推入背包、角色信息或其他库存控件；“放下一行”因此被否决，而不是待实机后再默认采用。

### 4.1 固定入口

- 入口是 Product 自有、`ignoreLayout` 的固定控件，不进入 `passiveItems`、`SetPassiveSlotCount`、`passiveItemPool.Instances` 或官方保存。
- 它右对齐角色装备区，并为已确认的官方最大两个 passive 槽留出完整空间。当前几何下官方四格（帽、主动、两个 passive）占 `484` 宽；入口使用剩余右端 `112` 宽区域，中间保持约 `10` 间距。
- 每次官方 passive render 后检测全部官方槽 Rect 与入口 Rect。相交时不挪动官方 UI，而是隐藏全部 Product UI、记录明确错误并设置发布阻断。

### 4.2 无人机模态抽屉

- 官方 `drone_panel` 可用区域为 `580 × 236`。三个 `112 × 112` 槽以 `12` 间距水平排列，总宽 `360`；按实际 Rect 计算时水平余量各 `110`、垂直余量各 `62`。
- 展开时显示不透明 Product 抽屉，覆盖的唯一官方区域是无人机面板；官方饰品区、背包、关闭按钮和其他 UI 不移动、不遮挡。
- 抽屉开启期间保存无人机控件原始 active/interactable/raycast/navigation 状态并临时禁用其射线与选择；关闭、面板 `ClearCallBack`、配置禁用、返回标题或 owner deactivation 时逐项精确恢复。
- UI 根、入口、抽屉和三槽只创建一次并复用。重复 render 只更新图标、文案、Rect 与可见状态，不销毁重建。

## 五、当前代码发现

### P1-1：四个旧 Hook 与动态 UI owner 不匹配

`MoreEquipmentSlotsHookInstaller` 当前原子安装 `ReloadParams`、`TryGetShieldItem`、`AccessoriesBar.__Init`、`AccessoriesBar.OnStartShow` 四个 Hook。`RenderAccessoriesBar` 每次先 `ClearUi`，再按 `passiveItem2 -> passiveItem1 -> positiveItem` 取模板；242+ 没有两个旧 passive 字段，因而回退克隆已绑定主动槽闭包的 `positiveItem`。每次 render 都销毁三槽再重建，且没有加入 `allSelectablesArray`。

当前 `BindUiLease` 只对部分 click/pointer 事件调用 `RemoveAllListeners`，没有完整清除 select、deselect、move、pointer 与 `SetClickCallbacks` 中继承的全部闭包。结果是 Product 槽可能触发官方主动槽行为，也不能进入原生键盘/手柄导航。

修正后的原子 Hook 集合必须恰好为五个：

1. `AgentEquipmentManager.ReloadParams`；
2. `AgentEquipmentManager.TryGetShieldItem`；
3. `AccessoriesBar.RenderPassiveItems`；
4. `AccessoriesBar.get_allSelectablesArray`；
5. `AccessoriesBar.ClearCallBack`。

收起时 getter 结果为官方 Selectable 加入口；展开时再加入三个 Product 槽，无人机控件不可导航。打开聚焦第一个 Product 槽，关闭聚焦入口，并在两次转换后重建官方 navigation。

### P1-2：装备语义校验过宽

当前 `BuildStorageEntry` 只检查 native item 是否继承 `ItemPassive` 或 `ItemHat`。对 passive，它不要求 `proto.Function` 必须是 `ItemFunctionPassive` / `ItemFunctionHerbPackage`，也不要求非空技能；因此“能生成 ItemPassive”不足以证明它是 0.3.1 允许的被动饰品。帽子路径应保持宽于饰品路径：普通帽子允许零防御、空技能；只有盾帽要求正数 `MaxShieldValue`。

三个位置必须采用同一判定：

- 接受带 `ItemFunctionPassive` 或 `ItemFunctionHerbPackage`、且技能非空的 `ItemPassive`；
- 接受已注册、可精确生成的普通帽子与盾帽；
- 普通帽子允许 `Defense=0` 和空技能；盾帽必须有 `MaxShieldValue > 0`；
- 拒绝主动饰品和其他 item 类型。

Product 帽子只贡献技能、防御和盾值；不得写官方 `hatItem`、不得改变人物外观。现有 typed shield provider 保留，由原生 `BodyController` 执行完整 attack tail。

### P1-3：incoming withdrawal 仍会把未知结果降级成普通失败

`EquipmentSlotNativePlacement.TryWithdrawOne` 只有精确 `after-before == -1` 才成功，这是正确起点；但 Cost 调用后抛错、after count 不可读及矛盾 delta 仍抛普通 `InvalidDataException`。`TryTakeMatchingNativeBuffer` 只检查 `Take()` 返回对象名称，不观察匹配物品是否已移除且 buffer 变空，也不能表达 clear-then-throw、错误返回或后状态不可读。

统一 Product-local incoming evidence 后：

- backpack 只有精确 `before -> after -1` 成功；
- buffer 只有调用前匹配、调用后匹配物品已取出且 buffer 为空成功；
- 调用后异常、错误返回、后状态不可读或矛盾 delta 一律为 `EquipmentSlotNativeMutationOutcomeUnknownException`；
- gameplay unknown 阻止其他槽操作并 veto `SaveSaving`；
- durable journal unknown 在当前进程标记已尝试并禁止重放，重启后才按现有 prepared/committed 语义恢复；
- `EquipFromBackpack` 与 `OnSaveSaving` 使用同一证据 helper，不新增 schema 或 receipt family。

### P2：README 与测试夹具漂移

产品 README 把“准入和旧数据迁移子边界已验证”写成整个产品 `verified/closed`，但 Catalog 仍是 `RebuildBlocked`，动态 UI、语义和 incoming 事务尚未关闭。实现 Update 应只改 front sheet，不重写其长历史技术说明。

现有 native fixture 仍只表达四旧 Hook 和简化 UI；gateway 只覆盖 Boolean/delta 的普通组合。它没有当前 official passive `1 -> 2`、getter/navigation、监听清除、重复 render，也没有 Cost/buffer mutate-then-throw、后读取失败或第二次 SaveSaving 不重放。源码字符串检查不能替代这些物理 fault tests。

## 六、frozen Compatibility Host 的本次边界

`0.3.1` 是被同一 Workshop `1.0.0` 原位替换的旧代码，不是本次发布后继续并行服务的第二产品。冻结 Compatibility Host 继续保留：

- `IEquipmentSlotsApi` 二进制 ABI；
- 旧 sidecar/迁移、rollback 与 orphan/cold recovery 证据；
- Product-first 与 Host-first 两种加载顺序下 canonical Harmony owner 冲突必须在任何 sidecar、物品、UI 或 Hook 状态变化前失败关闭。

本次不以修复 Host 的旧固定 UI、Boolean `CostItem` 事务或反射 attack tail 作为 `1.0.0` 发布门，也不把这些旧状态机重新放回 mandatory GameBridge。只有实际回滚/旧包支持任务另行授权时，才重新打开 Host 实现。

## 七、关闭与发布门

### 自动证据

- 五 Hook exact owner、237 缺目标原子失败、242+ 动态 UI fixture；
- official passive `1 -> 2`、固定入口、抽屉开关、getter/navigation、重复 render 和所有继承监听清除；
- 普通帽、盾帽、两类被动饰品与主动/其他拒绝项；
- backpack mutate-then-throw、after-count unreadable、contradictory delta；buffer clear-then-throw、错误返回、后状态不可读；第二次 `SaveSaving` 不重放；
- no-save、正常保存、保存中断、Product/Host 两种加载顺序 exact-owner 冲突；
- focused checks 通过后，冻结最终候选再从头运行一次完整 Release。

### 实机 UI 与保存

- 第三存档、`NoNativeSave`，同一有界验收覆盖 `1920 × 1080` 与 `1024 × 768`；
- 官方最大两个 passive、三个 Product 槽、最大背包布局和完整无人机控件；收起/展开截图与全部 Selectable Rect 结果；
- 收起零相交；展开只允许抽屉占用无人机区域；鼠标、导航、装备/卸下、关闭重开、配置禁用、返回标题全部通过；
- 任何恢复动作前证明玩家 archive 与 committed sidecar 未变；
- 正常保存和 cold recovery 只用隔离 disposable fixture，不写回 Steam AutoCloud 玩家存档。

### 包与发布

- Workshop `3744059735`、UniqueID `DTMAPI.MoreEquipmentSlotsMod`、version `1.0.0`、minimum Runtime `0.6.0`；
- Author SDK 只生成一个 1.0 entry DLL及其 Advanced receipt，无 0.3.1/Host 产品字节或重复入口；
- shared Runtime lock 内只准备官方上传目录并保留原 `workshop.json`，不替用户提交；
- 用户手动提交后，必须从 Steam 重新下载订阅制品，验证新 manifest/version/tree/单入口、0.3.1 原位升级、旧 sidecar 迁移与零残留，再更新 Catalog、subscription manifest 和本产品 Update。

## 八、不扩面的部分

- 保持 Product-v3 sidecar、迁移、current save fingerprint、cold recovery 与 typed shield provider；
- 不新增公开 API、SharedNative equipment engine、sidecar schema、receipt family 或版本专用 DLL；
- 不修改官方 `passiveItems`、`SetPassiveSlotCount` 或 `passiveItemPool.Instances`；
- 若固定入口在未来官方槽增长后相交，隐藏 Product UI并阻断发布；不暗中移动或修改官方槽。

## Validation

- 完整阅读最新 public 1.00.03 差异审查、Catalog/subscription authority、现有 Product runtime、Hook installer/callbacks、native placement、产品 manifest/README、历史 Review/Update/Issue/Hook map。
- 只读复核 `24650773_public_76C24E` 的 `AccessoriesBar` 动态 pool、`RenderPassiveItems`、`allSelectablesArray` 与 `ClearCallBack` owner。
- 逐像素查看用户 `1805 × 671` 临时截图，并把第二行覆盖背包/库存区的事实转换为第四节文字；截图未复制进仓库。
- 未修改 Product/Host/Runtime，未构建、部署、启动游戏、获取 Runtime lock、写 Steam、官方上传目录、玩家存档或 sidecar。

## 2026-08-11 public runtime root-cause follow-up

`GAME-SMOKE/20260811-050642` 在 public `24650773 / 1.00.03` 上把第三次双分辨率尝试推进到真实入口鼠标命中。候选五 Hook、三个 Product 槽和 `1920 × 1080` 收起态 Rect 均正常；入口与两个官方 passive 槽、背包、关闭按钮、无人机区域零相交。真实 `onPointerEnter` 随后连续暴露两条 Product hover 缺陷，故本次仍为发布阻断证据：

1. `ShowSlotHint` 调用克隆 `AccessorySlot.ShowEquipmentItemViewer(null, text)`。Unity `Instantiate` 没有为该克隆保留 `DolocUiObject.rectTransform` 的运行时初始化状态；官方方法继续进入 `HoverTextSmall -> HoverBoxGroup.UpdateAnchorPosition` 后发生 `NullReferenceException`。现有 fixture 的 `rectTransform => transform` 简化实现掩盖了该差异。
2. `HideHover` 使用 `typeof(DolocAPI).GetMethod("HideHoverBox", Public|Static)`。public 1.00.03 同时存在 `HideHoverBox(DolocUiObject)`、`HideHoverBox(RectTransform)` 与零参数 `HideHoverBox()`，所以真实 pointer exit 抛 `AmbiguousMatchException`。fixture 没有表达这组三重重载。

这不是入口 Rect、射线或 runner 坐标错误：异常堆栈本身证明真实 pointer enter/exit 已到达 Product 绑定。最小修正是把每个克隆的 `rectTransform` 运行时成员显式绑定到其实际 `transform`，再保留官方 `ShowEquipmentItemViewer` 文案语义；隐藏路径必须按零参数精确选择 overload。物理 fixture 需模拟“克隆后 runtime rect 未初始化”与多 overload，保证提示成功、pointer exit 无异常，并继续证明 inherited listeners 已清空。

同一运行还暴露验收编排的失败长尾：首个 hover 阶段失败后，UI 仍保持打开，组合的 title lifecycle 继续等待其独立期限，直到人工正常 `WM_CLOSE` 才进入 cleanup。runner 应在任一 Product toggle phase 失败时立即记录该阶段、发送一次有 provenance 的 Escape 关闭并终止本组合场景，不能把失败串成多个 `TimeoutSeconds`。这只缩短失败传播，不放宽任何验收断言。

运行退出前，玩家第三档 current/prev/bak 与 committed Product sidecar 均证明未变；runner 恢复原 Local11 profile，wrapper 恢复原本地 `0.3.1` 树 `B47EA3E2...46FE3`，精确 QA recovery receipt 已消费，进程/activation/QA root/事务根归零且共享锁释放。修复后必须重建新的 exact Author 候选并从头重跑，不得拼接本次收起态与未来展开态。

## 2026-08-11 public runtime drawer-layout root cause

`GAME-SMOKE/20260811-054506` 使用 hover 修正后的 clean `8024e9e3` 候选继续推进。真实日志依次证明入口 pointer enter、首个 Product 槽 focus、`drawer expanded reason=fixed entrance` 均成功；QA 随即以 `The expanded Product drawer escaped the native drone panel` 失败，`AccessoriesBar.ClearCallBack` 再把抽屉正常收起。因此本次不是 runner 点击、提示、展开状态或清理失败，而是展开边沿发生了真实 Rect 漂移。

根因已由当前源码与 public `24650773` prefab 共同确定：Product 把抽屉直接挂到 `drone_panel`，只给固定入口设置了 `LayoutElement.ignoreLayout=true`；抽屉自身没有该隔离。public `EquipmentBarWidget.prefab` 的 `drone_panel` 带启用的 `HorizontalLayoutGroup`，`spacing=12`，并同时强制扩展子项宽高。收起时 inactive 抽屉不参加原生布局，所以 QA 记录的抽屉与无人机面板 Rect 都是 `[985,744 -> 1565,980]`；入口点击把抽屉激活后，它立即成为原生 layout child，HorizontalLayoutGroup 覆盖 Product 的 stretch Rect，导致抽屉越出 `580 × 236` owner。现有物理 fixture 没有模拟父级 LayoutGroup，因而只证明了 inactive/statically stretched Rect。

最小修正不是改尺寸、换行或移动官方控件，而是给抽屉根本身增加 `LayoutElement.ignoreLayout=true`，继续以 stretch anchors 覆盖原 drone panel；入口仍保持独立 ignore-layout。物理 fixture 必须同时断言入口和抽屉两个 Product 根均不参与原生 layout，防止以后只保护入口。修正后必须重建新的 exact 候选并从头跑双分辨率验收；本次收起截图和未来展开截图仍不得拼接为 acceptance。

该运行在 QA 失败后按新 fail-fast 路径约 66 秒退出；玩家存档与 committed sidecar 在恢复前再次证明未变，wrapper 已恢复原 Product 树并释放共享锁。QA stage 因预期的五图矩阵尚未完成而按 receipt 保留，只能在下一次部署前按该 receipt 精确消费，不能做宽目录删除。

## 2026-08-11 complete-image run and QA false-positive root cause

`GAME-SMOKE/20260811-055523` 使用 drawer-layout 修正后的 exact 候选 `F546A51F...DE6F63`，把 UI 路线推进到五张图片全部产出。运行时与人工逐图复核共同确认四个核心状态成立：

- `1920 × 1080` 与 `1024 × 768` 的收起态均保留帽、主动饰品、两个官方 passive 和独立固定入口；入口没有挤动官方槽，也没有进入背包；
- 两个展开态的抽屉 Rect 与官方 drone Rect 完全相同，三个 Product 槽都在抽屉内，非无人机 UI 零相交；官方无人机控件被不透明抽屉覆盖且失去导航/射线，收起后 `DroneSnapshots=0`；
- 最大 40 格背包、关闭按钮和官方装备区在两种分辨率下都没有被 Product UI 覆盖；导航投影、首槽 focus、配置 disable `patches=0/roots=0/clones=0`、re-enable `5/1/3` 均通过；
- 玩家第三档 current/prev/bak 与 committed sidecar 在任何恢复前再次逐项未变，wrapper 恢复原 `0.3.1` 树并释放锁；完整七文件 QA stage 由 runner 精确归档并清除。

但该场不能接受为完整 UI PASS。第五张 `equipment-slots-1024x768-reopen.png` 实际仍是游戏世界与快捷栏画面，没有可见的装备面板。相应报告读取的是仍 active、正在 native reopen tween 中的旧 UI generation：入口 Rect 为 `[443.642,706.416 -> 512.616,775.39]`，抽屉/无人机从 Y=`785.244` 开始，背包 Selectable 则已在负 Y；这些 Rect 明确越出 `1024 × 768`。QA 只检查 `currentState is EquipmentBarUiState`、root identity 和相交，没有要求 Rect 位于 viewport，也没有等待 reopen 动画稳定，因而把 stale/off-screen root 误写成 `Status=verified`。修正必须在重开后等待入口与 drone owner 连续多个 frame 同时位于屏内且 Rect 稳定，并在所有 snapshot 上断言 Product、官方装备、背包、关闭按钮和 drone Selectable 的 Rect 都在当前 viewport；不能用 PNG 非空或 state type 代替可见性。

同一场的整体失败来自第二个 QA 漂移，而不是 Product 运行时。`MoreEquipmentSlotsNoNativeSave` 仍复用 `AdvancedProductOwnerDeactivationFixture` 中冻结 Host 世代的四目标清单：`ReloadParams`、四参数 `BodyController.OnAttacked`、`AccessoriesBar.__Init`、`AccessoriesBar.OnStartShow`，并硬编码 `patches=4/targets=4/4`。当前 1.0 Product 实际拥有五目标：`ReloadParams`、`TryGetShieldItem`、`RenderPassiveItems`、`get_allSelectablesArray`、`ClearCallBack`；日志因此只在旧清单中观察到 `1/3` resolved target 和一个 owner patch，然后错误拒绝已真实安装的五 Hook。所有当前 Product 的 gameplay/no-save/cold/owner-deactivation readiness 必须共享这五目标，并从 inventory/target array 派生计数；冻结 Host 只保留独立 ABI/冲突证据，不能继续充当 1.0 readiness authority。

这两项都是 QA authority 修正，不改变已通过四图和 Rect 证明的 Product 布局，也不放宽发布门。修正后可以用同一 Product 字节重建并证明 deterministic hash 不变，但必须从头重跑整场；`055523` 的四个核心状态只能作为诊断证据，不能与后续重开或 gameplay 结果拼接。

## 2026-08-11 stable-reopen pass and native function namespace root cause

`GAME-SMOKE/20260811-061243` 证明上一节的两项 QA 修正都有效。第五张 `1024 × 768` 重开图真实显示装备面板；入口与 drone owner 连续五帧稳定且位于视口内，重开 generation 复用原 Product roots。五张图的 Product、官方装备、40 格背包、关闭按钮与 drone Rect 全部通过 viewport containment，QA 最终发布 `Smoke.EquipmentSlotsUiObservation=verified`。因此当前固定入口、无人机模态抽屉、双分辨率、关闭重开、配置 disable/re-enable、导航与无人机恢复的 UI 门已经取得一场完整证据；后续 gameplay 失败不推翻该 UI 事实，但完整组合验收仍不能标绿。

当前五 Hook readiness 随后首次进入真实 `MoreEquipmentSlotsNoNativeSave` 操作。DebugConsole 精确把一个 `grandmas_button` 放入背包，Product 在 `EquipFromBackpack` 的 admission 阶段立即以 `Passive equipment must use ItemFunctionPassive or ItemFunctionHerbPackage` 拒绝。public `24650773` 的 `item_tbitem.json` 明确该物品是 `sub_type=kit_passiveprop`、`function.$type=ItemFunctionPassive`、`skill=grandmas_button`；所以这不是夹具使用了 active item。

根因是 Product 对 CLR full name 的常量少了真实子命名空间 `.Item`：代码比较 `DolocTown.Config.ItemFunctionPassive` / `DolocTown.Config.ItemFunctionHerbPackage`，而 237、242、244、245 与当前 246 的反编译类型都位于 `DolocTown.Config.Item`，真实 full name 分别是 `DolocTown.Config.Item.ItemFunctionPassive` 与 `DolocTown.Config.Item.ItemFunctionHerbPackage`。物理 fixture 又把假类型同样声明在错误的 `DolocTown.Config` 下，形成同源假绿。最小修正是把 Product exact type names 和 fixture namespace 同时对齐真实 native owner；普通帽、盾帽、skill-less passive、cooldown/active 拒绝规则、sidecar schema 与事务语义均不变。修正后必须由物理测试同时证明两类真实 full name 被接受，并重新构建候选；原 `F546…DE6F63` 因 Product 字节改变而失效。

该场在任何恢复前再次证明玩家第三档 current/prev/bak 与 committed sidecar 未变；QA stage、Local11 profile、原 `B47EA3E2…46FE3` Product 树和共享锁均精确恢复。运行只在 give 后、withdraw 前失败，玩家背包变更随 `NoNativeSave` 进程退出回滚，不构成持久验收或 sidecar mutation。

## 2026-08-11 current native attack-route QA drift

命名空间修正后的 exact 候选 `1AD74EBD…C65444A` 在 `GAME-SMOKE/20260811-062040` 再次完整通过 UI 子门，并越过上一场的 admission：日志证明 `grandmas_button` 成功装备，随后 `box_hat` 成功 give、替换并进入带正数盾值的 Product Working slot。运行接着在 QA helper 寻找 `BodyController.OnAttacked(float,bool,Vector2,out bool)` 时以 `found 0` 失败；这发生在 shield damage 调用前，不是 Product typed shield provider 拒绝或异常。

237、242、244、245 与当前 246 的 `BodyController.OnAttacked` 都是五参数 `(float, bool, Vector2, AttackProperties, out bool)`；其当前原生实现内部调用 `AgentEquipmentManager.TryGetShieldItem(out IAgentEquipmentShieldItem)`，再调用 Product 提供的 typed `TryBlockAttack`。QA helper 的四参数签名来自冻结 Compatibility Host/旧 attack-tail 假设，且没有被当前 Product 五 Hook readiness 清单覆盖。继续修补 Host prefix 或把 `OnAttacked` 放回 Product Hook 集合都会违反本次已批准边界。

最小修正仅属于 QA：精确选择五参数方法，要求第四参数 full name 为 `DolocTown.AttackProperties`，使用其 public static `Physical` 值，并把第五参数作为 `out bool isDead` 验证。这样仍通过真实 `BodyController -> AgentEquipmentManager.TryGetShieldItem -> Product IAgentEquipmentShieldItem` 链验证非破盾和破盾语义，但不恢复冻结 Host 的 attack patch。源码测试必须锁定五参数、`AttackProperties.Physical` 和第五个 out-bool 索引。候选 Product 字节不变，修正后仍须从头重跑组合场。

`062040` 的玩家 archive、committed sidecar、七文件 QA stage、Local11 profile、原 Product 树与共享锁也全部精确恢复；失败发生在 `NoNativeSave` 进程内，未形成 native save 或持久 sidecar 证据。

## 2026-08-11 bounded UI and NoNativeSave closeout

QA-only 修正后的 Author 重建保持 Product ZIP 精确为 `1AD74EBD…C65444A`。`GAME-SMOKE/20260811-062611` 从头通过同一组合路线，前序证据没有被拼接：

- 五张候选截图经逐图复核，`1920 × 1080` 与 `1024 × 768` 的收起/展开均保留官方帽、主动饰品和最大两个 passive；固定入口不改变官方布局，三个 Product 槽只在不透明抽屉内覆盖无人机区域，40 格背包和关闭按钮不受覆盖；`1024 × 768` 重开图真实可见；
- Rect 报告在每个状态均为 `ViewportContainment=exact`、`Intersections=none-outside-allowed-drone-modal`、`NavigationProjection=exact`、双 Product root `IgnoreLayout=true`；展开时 drone snapshots 为 38，收起/重开后回到 0；配置 disable 为 `patches=0,roots=0,clones=0`，re-enable 为 `5,1,3`；
- real gameplay 依次完成 `grandmas_button` give/equip、`grandmas_button -> box_hat` 替换、typed shield `80 -> 40` 非破坏攻击、破盾清槽、再装备与卸下；native item count 回到基线，Committed generation/occupied/slots 保持 `0/0/empty`，Working 最终空且只保留未保存 dirty 标记；
- terminal status 为 UI `verified`、MoreEquipmentSlotsNoNativeSave `verified`、G5 `verified`、TitleButtonLifecycle `verified`；Product exact owner 为五 Hook `patches=5; targets=5/5`。

运行在任何恢复动作前证明第三档 current/prev/bak 和 committed sidecar 全部未变；七个 QA evidence 文件、stage tree、Local11 profile、原 `B47EA…46FE3` Product、进程和共享锁均精确收口。这个结果关闭当前候选的双分辨率 UI 与玩家第三档 `NoNativeSave` 子门；它不替代 disposable normal-save/interruption/cold-recovery、完整 Release 或 Steam 订阅制品审计。

## 2026-08-11 disposable normal-save first-run root cause

`GAME-SMOKE/20260811-064033` 没有进入 Product：外层 disposable helper 只复制第三档 archive，没有把同一候选放进被重定向后的 `fixture/MODS`，也没有提供 fixture 自己的 `mod_infos.json`。日志明确为 `Official local MODS root=fixture/MODS; exists=false` 与 `productAssemblyLoaded=false`。这不是候选失败；进程、原 Product 与共享锁已恢复，未使用的 cold 子场没有启动。

补齐 fixture 的候选根和原生 enablement 后，`GAME-SMOKE/20260811-064433` 精确加载 `1AD74EBD…C65444A`，报告 public drift 仍在 receipt 允许边界内并安装五 Hook；它在第一次 give、withdraw 或 native save 前被旧 QA 前置条件阻断。旧 `NativeSaveExpected` fixture 用 `TryReadValidated` 要求磁盘 sidecar 预先存在，但全新 Product-v3 存档的正确状态是：`SaveLoaded` 在内存建立空 Committed 文档，只有真实 Product mutation 后的 `SaveSaving / SaveSaved` 才创建磁盘 authority。把“首次运行没有文件”当损坏既会误拒绝新玩家，也会迫使 Product 为零状态做无意义磁盘写入。

最小 QA 修正只让 protected-transaction 初始前置读取 Product 当前内存 `document`；第一次 native save 之后的阶段仍通过 `sidecarPath + scope + TryReadValidated` 读取磁盘，不接受内存结果冒充持久提交。现有 committed-shield setup 已采用同一边界。QA Release build、完整 QA Unit、save-mode focused suite 和 `git diff --check` 通过；Product 源码、Author 包与 `1AD7…544A` 候选字节均未变化。两个失败场都保留为非验收，下一场必须从全新 disposable fixture 起跑。

QA-only 修正后的 `GAME-SMOKE/20260811-065006` 再次精确加载 Product 与五 Hook，并越过首次空 sidecar 前置；随后在第一次 `GiveItem` 前失败。原因是 disposable fixture 隔离了 DTMAPI state，而 `DebugConsoleActionFixtureAdapter` 在没有当前 DebugConsole Product 时退回冻结 Compatibility Host；空 fixture 又没有已安装 Runtime release manifest，因此 broker 按合同 fail-closed。这个失败不属于 MoreEquipmentSlots，也没有授权把 Host 重新纳入 1.0 发布门。下一场 fixture 只加入当前 DebugConsole Product 作为 QA 原生发物助手；冻结 Host 继续只保留 ABI/冲突/回滚的 focused 证据。`065006` 没有 Product withdrawal、sidecar write 或 native save，进程、原 Product 和共享锁精确恢复。

## 2026-08-11 disposable normal-save attack-tail boundary correction

`GAME-SMOKE/20260811-065512` 在全新隔离 fixture 中同时加载 exact `1AD74EBD…C65444A` Product 和当前 DebugConsole QA helper。日志证明 `grandmas_button`、`box_hat` 均成功 give，Product 完成被动饰品装备与盾帽替换，五 Hook 保持 `5/5`；用例随后在第一次 native save 之前以 `BodyController.OnAttacked ... handled=False` 失败。失败没有来自 `TryGetShieldItem`、Product typed shield adapter、装备事务或 sidecar，而来自正常保存 helper 对 `BodyController.OnAttacked` 返回值的额外发布断言。

这条断言越过了本 Review 第六节已冻结的责任边界。MoreEquipmentSlots 1.0 拥有 `AgentEquipmentManager.TryGetShieldItem` postfix 与 typed `IAgentEquipmentShieldItem`，不拥有 `BodyController.OnAttacked` 的攻击分派、伤害类型、无敌帧或 `handled` 返回语义；把 disposable normal-save 接受绑定到完整 attack tail，会重新把冻结 Host 的历史责任引入当前 Product 发布门。`062611` 已在玩家第三档 `NoNativeSave` 场独立取得真实 `80 -> 40`、破盾和恢复证据，本场需要证明的是 Product-v3 的正常保存/卸下/清理与 cold recovery，不能重复要求一条非 owner 的 attack tail 才允许保存。

最小 QA 修正是直接调用当前原生 `AgentEquipmentManager.TryGetShieldItem(out IAgentEquipmentShieldItem)`，要求返回的 provider 来自 exact Product assembly、`ShieldValue` 与 Working shield 一致且为正数；这精确覆盖五 Hook 中的盾 provider，而不调用 `BodyController.OnAttacked`。第一次 native save提交完整盾状态；随后通过 Product 自己的 `RequestUnequip` 卸下盾帽，再装备/卸下 `grandmas_button`，第二次 native save提交空 sidecar，最后精确移除两件 QA 发放物并回到各自背包基线。Product、schema、receipt 与候选字节均不改变；修正后的 normal-save 必须从全新 fixture 重跑，`065512` 只保留为根因证据。

## 2026-08-11 disposable cold-recovery trust projection root cause

[GAME-SMOKE/20260811-070554](../../../../debug/evidence/GAME-SMOKE/20260811-070554) 是未改变 `1AD74EBD…C65444A` 候选后的正常保存验收。它精确加载 Product provider 与五 Hook，完成两次真实 native save，先提交已装备盾帽，再经 Product 卸下盾帽、装备并卸下被动饰品后提交空 Product-v3 状态；两件 QA 物品回到精确原生基线，标记 fixture 被删除，进程与共享状态均正常退出。这关闭 normal-save 子门，不借用此前 attack-tail 诊断结果。

独立三进程路线随后在 [GAME-SMOKE/20260811-070648](../../../../debug/evidence/GAME-SMOKE/20260811-070648) 的 `ColdPrepare` 末尾失败。真实 Sleep save 与 `SaveSaved` 已完成，Product-v3 cold seed 已写入；Product 按设计禁用后，保留的冻结 Host recovery lane 被正确 demand，但 compatibility broker 因 smoke 将 `runtime.Paths.DtmApiPath` 重定向到隔离 fixture、而该 fixture 没有 `DTMAPI/release-manifest.json` 而 fail-closed。已安装 0.6.1 的真实 authority 位于解析出的游戏根 `DTMAPI/release-manifest.json`，其中恰有一个 `gamebridge-compatibility-host` receipt，且其相对路径、策略、长度和 SHA-256 与同一游戏根下的 dormant component 一致。因此这是 fixture 信任投影缺失，不是 Product、schema、迁移或 Host policy 失败。

最小修正由 cold runner 自己拥有：持有共享 Runtime lock 后，读取并校验已安装 release manifest 中唯一 frozen-Host receipt，同时校验它引用的组件仍位于游戏根内且长度/哈希精确；随后只把该 manifest 的原始字节复制到 disposable fixture 重定向后的 `DTMAPI` 目录。不得复制 Host 字节、install state、玩家配置或日志，不得放宽 broker，也不得续跑已经失败的 fixture。修正后必须从全新标记 fixture 依次重跑 `ColdPrepare -> ColdCommit -> ColdObserve`。

## 2026-08-11 current candidate save and cold closeout

[GAME-SMOKE/20260811-072147](../../../../debug/evidence/GAME-SMOKE/20260811-072147) 的 aggregate receipt 从全新 fixture 收齐三个独立进程，关闭了上述 trust-projection 根因。`072005` 先通过一次真实 Sleep save 后写 exact-scope seed；`072056` 在 Product 禁用下通过 demand、Host、destination 三门，只恢复一个 `grandmas_button` 并用第二次真实 SaveSaved 清空 terminal journal；`072147` 不请求 native save，确认同一件物品仍在背包、terminal Product-v3 authority 空、recovery session/replay 为零，并在删除 fixture 前通过 archive family 与 committed-sidecar unchanged 检查。

聚合 receipt 将投影来源固定为已安装 Runtime `0.6.1` / build `db5e518a6d7f`，manifest SHA-256 为 `460960EC…DC04A`，其 frozen component SHA-256 为 `63853864…F89F`，且明确 `CopiedComponentBytes=false`。外层交易同时核对 candidate tree `8509BE31…33F97` 与原 0.3.1 tree `B47EA3E2…46FE3` 后恢复，fixture、进程和共享锁均归零。结合 `070554` normal-save PASS，当前候选的保存、提交、中断隔离与 cold recovery 发布前运行时矩阵已关闭；尚未关闭的是完整 Release、上传准备和 Steam 订阅制品替换验证。

## 2026-08-11 complete Release first-run routing-assertion drift

clean `e3ad056c` 上针对冻结候选 `1AD74EBD…C65444A` 的首次完整 Release 从头运行完成 Release build 后，在 aggregate Unit 门停止；QA Unit、Doctor、package/installer 与后续 Release 门均尚未到达。失败断言仍匹配 EquipmentSlots G4 的旧文案：无 QA Host 分支要求子串 `require -StageQaHost`，G4/G5 混合分支要求已被 1.0 有界路线取代的 `independent cold-run G4 route`。

Windows PowerShell 5.1 对两条原命令的独立复现均以 exit `1` 正确 fail-closed。无 Host 分支当前明确报告 `All automated fixture scenarios require -StageQaHost after the embedded harness removal`；`QaObserveEquipmentSlotsUi + AutoExerciseNewContentApis` 当前明确报告 observer 只接受自己的有界路线或 exact 1.0 `NoNativeSave` 组合，并点名拒绝 `AutoExerciseNewContentApis`。因此生产 runner 没有放宽 G4/G5 所有权，也没有误把通用 NewContent 纳入 1.0 特例；漂移只存在于 aggregate Unit 的错误文本断言。

最小修正只把测试绑定到上述两个当前 fail-closed 原因，并继续要求混合分支同时包含 exact 1.0 route 限定与被拒参数名。`run-game-smoke.ps1`、Product、Author policy 与候选字节均不改变。本次完整 Release 是非验收；修正先过 focused 路由/Unit 检查，再按 Release 治理运行一次未到达门的诊断尾段，最后才允许对冻结候选做一次新的 clean from-start 完整验收。

修正后的 aggregate `DTMAPI.UnitTests` 与 `moreequipment-acceptance-routing` focused route 均通过；托管测试会话由 tracked cleanup 删除。Catalog `27 / 11 / 22 / 48`、generated admission registry `12` 行、文档治理、`git diff --check` 与 frozen candidate SHA-256 `1AD74EBD…C65444A` 复核也通过。既有 DebugConsole nullable warnings 没有新增且不属于本次变更。

## 2026-08-11 unreached-tail Candidate11 historical-gate drift

非权威 Release 诊断尾段先通过 QA Unit、InstallDoctor、Runtime package/installer、evidence retention、Author SDK、Runtime-only uninstall、invalid-target、upgrade 与 official-local install transaction。随后默认套件调用完整 `test-candidate11-source-transaction.ps1`，其 synthetic Catalog 没有治理拆分后才成为 current-public 身份的 `currentPublishedArtifact`，因此 `Get-DtmApiReleaseContractAdvancedProducts` 正确返回空集合，旧测试仍要求 `9 source + 2 retained` 而停止。游戏未启动，共享锁为空，Product 与 frozen candidate 未改变。

这不是给 synthetic fixture 补九个假“当前发布制品”即可长期解决的问题。Candidate11 是冻结 0.6/Local11 里程碑；MoreEquipment 1.0 Steam 发布后，真实当前发布集合本来就会从历史 `9 + 2` 变化，继续把该计数作为所有后续产品 Release 的默认门，会让历史路线重新拥有当前 Catalog。当前公开/订阅集合已经由 Catalog `currentPublishedArtifact`、subscription manifest 和 latest release Update 拥有；Catalog checker、release-artifact-set、Author SDK、Runtime transaction、ABI 与当前产品门分别保护可回归的 live invariant。

因此完整 Candidate11 helper、专用 replay、历史 evidence 和 Windows PowerShell syntax validation 全部保留，但从默认 `tools/scripts/test.ps1` 移除自动 replay。需要审计历史 Local11 事务时仍可显式调用专用测试；它的 PASS 不再是 MoreEquipment 1.0、未来单产品发布或当前公开集合的授权。这落实了 frozen 0.6 路线的 no-write/no-direct-reference 边界和“历史 milestone 不自动常驻 default suite”规则，而没有放宽任何当前包、Catalog、订阅、ABI 或 Product 检查。

## 2026-08-11 unreached-tail AnimalViewer semantic-inventory drift

移除历史 Candidate11 默认 replay 后，诊断尾段通过 Catalog、Phase0/G2 historical receipt-only、四个现行 Advanced 产品、current-published artifact、exact reference fixture 与九产品 deterministic Author package/release-contract 门，随后在 Batch4 semantic boundary 停止。失败只来自 `batch4-production-qa-semantic-inventory.json` 仍固定 7 月旧 AnimalViewer token：通用 `Hook status` visible、native close 与 overlay clear 两次独立 wait，以及 `nativeCloseIndex -> overlayClearIndex` 顺序。

当前 runner 从 `b0aa552e` 起已经使用更严格的 Product-owned 原子 receipt：两个带 `receiptSequence` 且 `receipt=new-data` 的 visible receipt 后发送一次有 provenance 的 Escape；再以 bounded absolute deadline 等待 `AnimalHusbandryProgress native panel closed; product-owned derived rows and clones are zero.`。该单一 receipt 同时证明 native close、derived rows 与 clones 清零，随后用 `productCloseIndex > lastRenderIndex` 证明因果顺序。旧 checker 不是额外安全门，而是要求已被同一原子事实取代的中间日志和变量名。

最小修正只同步 inventory 的 required patterns、ordered tokens 与对应 stale negative sample；runner、AnimalHusbandry Product、MoreEquipment Product 和候选字节均不改变。focused Batch4 checker 通过 `25/25` contracts、`149/149` negative patterns、`20` lifecycle contracts、`49` negative samples与四个 IL artifacts；semantic-inventory meta-negative `12` cases、Catalog projection `1` case和 receipt-set `6` cases也通过。

## 2026-08-11 unreached Release tail closeout

上述两项修正合并后，非权威诊断尾段从 QA Unit 一直覆盖到最终治理门并以 exit `0` 收口。除已单独通过的 focused Batch4 门外，后半段通过 Batch5 GC source/plan、Batch6 AutoFishing ladder/behavior/Manager retirement、no-demand/deadline、synthetic retained ABI、retained input resolution、Runtime-floor compatibility、完整 retained Release ABI、test-artifact governance 与最终 document governance。前半段已通过的 Doctor、Runtime package/installer/evidence-retention、Author SDK、Runtime/install transaction、Catalog、current-published artifact、exact reference fixture与 deterministic package/release-contract结果均来自同一诊断循环；它们不被拼接成完整 Release PASS。

诊断期间没有启动游戏、持有共享 Runtime lock、修改玩家/订阅/上传目录或改变 Product。冻结候选 SHA-256 始终为 `1AD74EBD…C65444A`。根据 complete-suite failure policy，下一步只剩在 clean final HEAD 对该 exact 候选重新运行一次从头完整 Release；此前任何 partial/tail PASS 都不替代它。

## 2026-08-11 authoritative complete Release PASS

最终权威运行在 clean HEAD `eae8c424b1fe691a9cdb4b0a6adbe643c70eccd1` 上从头执行 `tools/scripts/test.ps1 -Configuration Release`，以 exit `0` 完成全部门，历时 `957.321` 秒（`2026-08-11T08:13:59+08:00` 至 `08:29:56+08:00`）。结果记录证明运行前后 HEAD 相同、Git status 均为空，冻结 ZIP SHA-256 前后均为 `1AD74EBDCE492A35C9659144C635742F0C9E72D864E61E4FE8D60EA45C65444A`；退出后 `DolocTownProcessCountAfter=0` 且 `RuntimeLockExistsAfter=false`。

本次从头运行重新覆盖 Release build、aggregate Unit、QA Unit、Doctor、Runtime package/installer、evidence retention、Author SDK、Runtime/install transaction、Catalog/current-published artifact、exact reference fixture、deterministic package/release contract、Batch4/5/6、no-demand/deadline、retained ABI、Runtime floor、test-artifact governance 与 document governance；没有拼接此前的 partial/tail PASS。stderr 只有 Unit 阶段托管会话的 cleanup-pending 提示，随后 tracked cleanup manifest 显示候选数为零且会话路径不存在。

因此 exact 1.0.0 候选的发布前完整 Release 门已关闭。该结果不证明 Steam 已发布，也不授权提前修改 Catalog 的当前订阅制品；生命周期保持 `implemented`，直到官方上传目录准备、用户手动提交、Steam 重新下载与订阅制品审计全部完成。

## 2026-08-11 dynamic-tail redesign and transient-animation root cause

用户在入口/抽屉候选完成后重新审阅实际装备截图，最终批准三个 Product 槽自然接在当前已解锁官方尾槽之后，并删除全部展开、入口、模态覆盖和无人机状态接管。前文 4.1/4.2、相应实现缺陷、运行时 PASS、完整 Release 与上传准备仍按发生顺序保留，但只解释已被否决的 `1AD74EBD…C65444A` 历史候选；当前实现与发布事实只由同一生命周期 Update 的后续段落拥有。

clean `fa2fdc47` 动态候选经未放宽的 244 exact policy 构建为 ZIP `902890E8…1695E3`、DLL `F9BAC345…91EE2`。`GAME-SMOKE/20260811-100346` 在 public `24650773` 上生成了两张可审查的核心分辨率图和完整 Rect 报告：官方帽、主动槽、两个 passive 与三个 Product 槽形成单一横排，Product 在 `1920 x 1080` 为 `[851,616 -> 1211,728]`、在 `1024 x 768` 为 `[444.873,482.25 -> 666.576,551.224]`，均紧随实际官方尾部且没有覆盖装备槽、40 格背包、关闭控件或无人机 owner。因此“自然顺延”几何本身可行，不需要缩放、第二行、官方槽池修改或恢复抽屉。

该运行仍是 `partial`。配置切换后的第一次 native render 曾把一个仍在动画/布局转换中的原生 Selectable 与 Product 判为相交，下一次 render 自动恢复；关闭重开时 `RenderPassiveItems` 又在整个 equipment panel 尚处于 off-screen tween 时执行，生产校验把 Product 判为逃出 panel viewport并永久隐藏，QA 随后无法获得五帧稳定可见状态而等待到外层超时。`Canvas.ForceUpdateCanvases` 只结算 layout，不推进 tween；所以在 render callback 中枚举整个 Selectable 树或要求祖先 panel 已位于最终 viewport，混合了稳定槽位关系与瞬时页面动画。

最小修正继续保持零逐帧 Product 路径。运行时只在原生 `RenderPassiveItems` 回调中验证该回调真正拥有的稳定事实：已审计官方 count、`112 x 112`/`12` 几何、Product 与当前官方槽零相交、Product 与只读 drone owner 零相交。它不再反射枚举整个 native Selectable 树，也不在 tween 中测试祖先 viewport containment。QA 保留更严格的发布门，并在 panel 连续五帧稳定且位于屏内之后检查 Product、官方装备、背包、关闭按钮、drone 与所有 Selectable 的 viewport/交叉关系。这样没有把风险藏掉，只把校验放回具有稳定时序的 owner；修正后的 exact 候选必须从头重跑，`100346` 不可拼接成 acceptance。

## 2026-08-11 corrected dynamic-tail runtime acceptance

clean `efd7d252` 经同一 244 exact policy 生成 ZIP `0F4BD87B…7527F`、DLL `8CC36D1F…AF566` 和本地八文件树 `D02C035B…AC85`。`GAME-SMOKE/20260811-103047` 在 public `24650773` 从头通过，不拼接 `100346`：两种分辨率的最大两官方 passive 状态均为单一自然横排；重开状态把官方 passive 改回一个，三个 retained Product roots 精确左移一个原生步距。三张非重复截图经人工复核，40 格背包、关闭按钮、无人机与官方装备均未遮挡；每份稳定报告均为 `Intersections=none`、`NativeTailOrder=exact`、`NavigationProjection=native-plus-three`、`ViewportContainment=exact`、`IgnoreLayout=true` 和 `PerFramePolling=none`。

配置 disable/re-enable 从 `patches/roots/clones=0/0/0` 精确回到 `5/1/3`；关闭重开在第一帧正确等待 off-screen tween，连续五帧稳定后复用同一 Product roots 并通过最终截图。日志中没有 Product layout block、碰撞/越界错误或 outcome-unknown。随后真实 `grandmas_button`、`box_hat` 和 typed shield 路线完成装备、替换、`80 -> 40`、破盾、再装备/卸下与 native item baseline 恢复，Hook 为 `5/5`、`nativeSaveRequested=false`、Committed 仍为空。

在任何 cleanup 前，第三档 current/prev/bak 的长度、SHA-256、mtime 与文件集合全部未变，两个 committed-sidecar 路径继续不存在。QA 五文件 archive、QA root/activation、Local11 profile、进程与共享锁精确清零，外层事务恢复 retained 0.3.1 树 `E0854CEE…E0CA6`。这关闭当前动态 UI 与玩家档 `NoNativeSave` 子门，并证明不需要 per-frame 轮询；仍不替代最终 exact-binary save/compatibility、clean complete Release、上传授权或 Steam 订阅制品审计。

## 2026-08-11 dynamic-tail exact save and cold closeout

第一次 dynamic-tail 正常保存运行 `GAME-SMOKE/20260811-104209` 在 Product mutation 前 fail-closed。一次性 fixture 已隔离 `SAVE` 与 `DTMAPI`，但外层编排遗漏了重定向后的 `fixture/MODS` Product/DebugConsole 根和 fixture 自己的 enablement 投影；Runtime 因而正确报告 `Official local MODS root ... exists=False`、`productAssemblyLoaded=false`。这与本 Review 前文 `064033` 的隔离边界一致，不是当前 Product 回归。运行没有 give、withdraw、sidecar write 或 native save；进程、原生 profile、retained 0.3.1 官方树和共享锁全部恢复，失败 fixture 在诊断后删除。

全新 `GAME-SMOKE/20260811-104433` 把 exact `0F4BD87B…7527F` Product 和当前 DebugConsole 发物助手作为 fixture 内唯一两个 Local 源，且只在标记的 Steam AutoCloud 隔离存档根运行。它加载一个 Product 实例、五个 Hook/五个 target 和 exact typed shield provider，完成两次真实 native save：先提交完整盾帽 Working 状态，再经 Product-owned unequip、被动饰品 equip/unequip 提交空 Product-v3 状态；两件 QA 物品回到原生计数基线。fixture 删除、TitleButtonLifecycle、QA cleanup、进程退出、live profile 与 retained 0.3.1 树恢复全部通过。正常保存门没有重新调用或要求冻结 Host 的 `BodyController.OnAttacked` tail。

独立 aggregate `GAME-SMOKE/20260811-104904` 随后从全新 fixture 依次完成 `ColdPrepare/104716 -> ColdCommit/104810 -> ColdObserve/104904`。前两阶段各自完成一次真实 Sleep save；中间进程禁用 Product，只 demand frozen recovery Host，把一个 `grandmas_button` 恢复到背包并清空 terminal journal；最后进程使用 `NoNativeSave`，证明同一件物品保持、terminal Product-v3 authority 为空、recovery replay/session 为零，并在删除 fixture 前通过 archive family 与 committed-sidecar unchanged 门。Installed Runtime `0.6.1 / db5e518a6d7f` 的 release manifest 以 SHA-256 `460960EC…DC04A` 原字节投影，Host component `63853864…F89F` 只在安装位置校验且 `CopiedComponentBytes=false`。

因此 current dynamic-tail 候选自己的 UI、玩家档 `NoNativeSave`、正常保存提交和 Product-disabled cold recovery 已分别取得不拼接的 exact-binary evidence。旧 `070554/072147` 仍只属于已否决的 drawer 候选；新结果不恢复旧入口/抽屉、不改变 Host ABI、sidecar schema、receipt 家族或 244 exact policy。当前剩余发布前门只有 clean from-start 完整 Release，随后才可建立新的 exact 上传授权与准备树。

## 2026-08-11 dynamic-candidate Release harness root cause and diagnostic tail

clean `a31676ca` 的第一条临时 orchestration 在 `test.ps1` 外把 Unit 的已知 cleanup-pending stderr 当成终止异常；纠正外层捕获后，从头 Release 通过 build、Unit、QA、Doctor、Catalog 与 Runtime package/source-boundary，停在 installer matrix 的受控并发用例。该用例先由测试进程持有 per-game mutex，再要求第二次 install 以明确消息失败；mutex 实际正确工作，但 `Invoke-Installer061Bat` 在全局 `ErrorActionPreference=Stop` 下把预期 native stderr 提升成 `NativeCommandError`，所以断言来不及读取 exit code。没有残留 installer owner、游戏进程或共享 Runtime lock，也没有 Product/候选字节变化。

最小修正只在 BAT 捕获窗口暂时使用 `Continue` 并在 `finally` 恢复原策略，仍要求 exact non-zero/message；全新的 focused Runtime package 与完整 0.6.1 installer matrix 随后通过。诊断过程中还识别出临时 Release wrapper 错用 WinPS 5.1：它应只承担 syntax/player-installer compatibility，Author SDK 的 frozen store-mode ZIP 必须由 PowerShell 7 驱动。cleanup durable-root、Steam manifest identity 和 Workshop marker 测试的受影响 hash 调用同步改为已有 `Get-DtmApiFileSha256`，消除可选 cmdlet 依赖而不改变摘要算法或权威。

`test.ps1` 新增默认不启用的 `PostRuntimeInstaller` 入口，并在输出中明确它是 non-authoritative、绝不构成 complete Release PASS。以当前 `pwsh` 执行后，整条此前未到达尾段在 `685.868` 秒内 exit `0`：evidence retention、WinPS child checks、Author SDK、事务矩阵、Catalog/current release contract、exact fixtures、九产品双构建、Batch4/5/6、no-demand、ABI、artifact/doc governance 全通过。冻结 ZIP 前后均为 `0F4BD87B…7527F`，没有启动游戏或持有共享锁。按 complete-suite policy，仍必须先提交这些 harness 修正，再在 clean final HEAD 从头跑一次完整 Release；不能把 focused/tail 结果拼接成验收。

## 2026-08-11 dynamic-candidate authoritative complete Release PASS

harness 修正提交为 `e42a284f` 后，默认 `tools/scripts/test.ps1 -Configuration Release` 在该 clean HEAD 从头运行，并以 exit `0` 完成，历时 `940.285` 秒（`11:23:01+08:00` 至 `11:38:41+08:00`）。外层 receipt 证明运行前后 HEAD 都是 `e42a284f50d243f0c5821adab43fc5a9e8afb1d6`、tracked status 均为空，冻结候选前后都是 `0F4BD87B5F899FC74648A202EFF1430F17D9AEF8AA149C7C19D03B11EB97527F`，退出后游戏进程为零且共享 Runtime lock 不存在。

该轮从 build、aggregate Unit/QA/Doctor 开始，重新经过 Runtime package/source boundary/installer、evidence retention、WinPS compatibility children、Author SDK、Runtime/install transactions、Catalog/current release artifacts、exact reference fixtures、九产品 deterministic double build、Batch4/5/6、no-demand/deadline、synthetic/retained ABI、Runtime floor、artifact governance 和最终 document governance，没有使用 `StartAt`，也没有拼接 focused 或 tail PASS。stderr 只有 Unit 托管会话 cleanup-pending 提示；随后 tracked cleanup 报告零候选且该路径已不存在。因此 current dynamic candidate 的发布前完整 Release 门关闭，下一步可以建立只指向 `0F4BD87B…7527F / D02C035B…AC85` 的临时 Catalog 上传授权；Steam 当前公开/订阅事实仍必须保持 0.3.1，直到用户提交并重新下载审计。

## 2026-08-11 exact dynamic upload authorization

只读 preflight 重新冻结七文件候选 `0F4BD87B…7527F`（`206,272` bytes）与当前官方目录中的原始 33-byte `workshop.json`（`EF4FD946…6543D`）。二者唯一允许的合成树是八文件、`206,305` bytes、`D02C035B…AC85`，其中只有一个入口 DLL：`201,728` bytes / `8CC36D1F…AF566`；manifest 仍为 `DTMAPI.MoreEquipmentSlotsMod / 1.0.0 / minimum Runtime 0.6.0`，Advanced receipt 仍绑定 `doloctown-24456188-moreequipmentslots-v1`。

Catalog 的 global stop 继续阻止新 Workshop 项、身份移动、批量版本编辑和所有未列出的 existing-item update；临时 mutation entrypoint 只允许 Workshop `3744059735` 使用上述 package/tree，并由本生命周期 Update 拥有。checker 同时验证 package SHA 与 tree SHA，避免同版本或同目标名下换包。该授权不改变 `currentPublishedArtifact`、subscription manifest、publishedVersion 或 latest release Update，也不证明上传目录已准备或 Steam 已发布。

## 2026-08-11 exact upload folder preparation

前两次 locked orchestration 都在目标移动前 fail-closed。第一次只是 StrictMode 在构造“无 transient sibling”诊断时读取空数组 `.Name`；没有创建 stage。第二次已生成 exact stage，但 ADS 检查的错误消息读取空数组 `.Stream`，catch 以 owner/exact-candidate 双证据删除 stage。每次之后，独立复核都得到官方与订阅同为 `9 / 539,565 / E0854CEE…E0CA6`，零兄弟、零游戏、锁空闲。因此这两次只属于外层诊断表达式缺陷，不是包、UI、事务或 atomic swap 失败。

修正后，commit `69bdced5` 下的事务先验证 Catalog 唯一 entrypoint、candidate SHA、旧官方/订阅树和原始 `workshop.json`，再把 ZIP 安全展开到同卷 direct sibling。stage 拒绝 traversal、reparse point 和 ADS，并在任何移动前要求 exact `8 / 206,305 / D02C035B…AC85`、单 DLL `201,728 / 8CC36D1F…AF566`、manifest `DTMAPI.MoreEquipmentSlotsMod / 1.0.0 / min 0.6.0` 与 244 policy receipt。随后通过两次同卷 rename 发布，新目标和未变订阅复核通过后才删除旧树 backup。

最终 receipt 状态为 `PreparedNotSubmitted`，时间 `2026-08-11T11:45:34+08:00`；官方目录精确为动态候选树且保留 `workshop.json` `EF4FD946…6543D`，transient/game/lock 均为零。独立 post-check 同时证明 Steam subscription 仍是原 0.3.1 九文件树与 `092807CC…71ED` DLL。故当前只允许用户在 Steam 客户端手动“提交更新”；在重新下载到订阅目录并审计前，不得称为 published、不得改 current public/subscription authority，也不得消费临时授权。
