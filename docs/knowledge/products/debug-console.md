# Y 控制台：输入、事务与玩法边界

本页提炼历史产品诊断，不能替代当前源码、Issue 和验收。当前产品入口：[DebugConsole](../../../products/first-party/DebugConsole/README.md)；反复输入问题：[ISSUE-014](../../debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md)；Hook 事务：[ISSUE-015](../../debug/issues/ISSUE-015-20260727-debugconsole-hook-transactions.md)。

## 输入顺序与有效证据

2026-07 双切换来自旧 raw-Y close 与普通 Mod typed toggle 同时消费一个物理边沿。2026-08 焦点回归则来自 Bootstrap 先采样并同步派发 typed toggle，产品稍后才在 UI Update 中查询文本焦点；单测分别验证两个 helper 没覆盖真实顺序。修复点应在产品 toggle 前，保留原生 InputField/TMP 及控制台自身文本输入、opener-cycle、快速释放再按和普通 Y 行为。

IME 消费了 Y 且画面保持开启，是可见结果，不能代替实际执行 focus guard 的因果证据。用户最终逐项接受搜索框、宝箱重命名及退出输入后的普通切换，才关闭该次回归；无需倒改此前未完成的十一产品 smoke。后续测试槽变更不使旧第三槽证据失效。

## 事务与原生责任

2026-07 旧 Compatibility 的不变 modal/drain setter 每帧触发 Hook 重建，28 秒出现 804 次 cleanup。需要分离 desired/installed topology，保持 warmed frame 无 patch 操作；部分安装、卸载和原生恢复失败时保留可重试的 owner/ledger。Catalog 唯一身份不能阻止另一旧 API 消费者要求 Compatibility，必须验证物理 owner 互斥。

2026-08 原生研究进一步否定了 `MoveScaler` 的简单精确旧值恢复：它是共享 Buff 聚合，值相同也不能证明贡献身份。后续以最终移动速度 Postfix 施加本产品倍率；这不否定之前有效的输入/creative 事务证据。

## 产品语义不能由旧实现推导

Y 已被选择为可选 Diagnostic 产品。先等价迁出 Bootstrap、保留旧消费者，再独立改变 UI 的决定有助于归因回归。旧“Generator/Monster/Resource 全隐藏”路线已冻结；Resource 的旧 backend 仍不代表存在现行 UI。复合怪物后续选择可见部分成功，不应从旧讨论恢复伪事务回滚或共享熔断。当前后续项查对应 Update/Issue。

来源：[Y 产品边界](../../archive/reviews/code/2026/20260713-0007-yconsole-bootstrap-product-boundary-review.md)、[冻结世界动作路线](../../archive/planning/2026/20260801-debugconsole-world-actions-roadmap.md)及上列 Issue。

7 月多轮修正还揭示了几类不能由 success smoke 证明的事务：desired 状态与实际 Hook 安装要一起提交；partial install 后 unpatch 也失败必须留下 cleanup tombstone；Dispose 只在全部清理成功才设 disposed；title restore 失败后 SaveLoaded 应先重试再进入新存档；Close 失败不能留下不可见 Core modal token。真实测试应经过 UI.Close、服务构造顺序和 Loader.Dispose，单测直接调用底层 helper 或只断言某行代码已删，可能漏掉整个生命周期。

删除重复 Update 曾把 DebugConsole-first 创建的嵌套 DebugActions 变成零 Update；“只应有一个 owner”需要两个服务构造顺序下 exactly-once 行为证明。Compatibility 若直接编译 current 产品 UI/actions，产品重写会暗改冻结旧实现，因此需明确冻结源边界。Save here 的两次点击、失败可见与成功调用，只证明按钮路径；money Working 失败回冷档、成功提交再冷档才证明该项保存语义，不能泛化所有 action。

1.00 原生比较将 CostItemAt 目标改为四参数，天气只调用当前房间二参数官方 Command_SetWeather 并读回 LocalWeatherType；相同的 UseTool/UseItem 方法体不需要无谓重写。科技按钮固定四类各加 100，某类失败仍尝试后续类；未接线世界动作不因保留 backend 自动进入本轮验收。

来源：[迁移缺口](../../archive/reviews/code/2026/20260727-0001-mine-fixes-and-debugconsole-split-audit.md)、[拓扑/Dispose/写后失败](../../archive/reviews/code/2026/20260727-0002-mine-and-debugconsole-fix-recheck.md)、[完整生命周期与构造顺序](../../archive/reviews/code/2026/20260727-0003-mine-debugconsole-transaction-fix-audit.md)、[1.00 native owner](../../archive/reviews/code/2026/20260804-0003-debugconsole-100-native-body-review.md)。

8 月轻量化选择了普通 Close/Open 的折中：保留 Canvas/结构与实际页大小的 cell，关闭释放 DTO、动作上下文、动态文本、tooltip 与 sprite；同几何/语言重开复用，title/owner 清理是另一生命周期。打开态 RawInput 反射/布局/EventSystem 扫描与目录投影应按真实变化处理，但不可删 raw Y/Escape 或缩短两干净帧 drain。其后续催熟/选中物销毁路线明确是新功能：当前房间距离过滤＋稳定排序＋上限、三类原生 DEBUG_SetLevel；删除走 DestroyItem 精确槽，易失确认快照复核对象/数量/会话，内存 Working 后只由原生保存提交。无需新 Crop Hook、删除 journal 或 sidecar。该路线是否已实施只查 owning Update。

来源：[折中 UI 生命周期](../../archive/reviews/code/2026/20260831-0006-y-console-runtime-lightweighting-review.md)、[催熟与原生删除路线](../../reviews/code/2026/20260901-0001-y-console-crop-maturity-and-selected-item-destruction-roadmap-review.md)。

6 月旧右键故障来自八秒内复用上次 hover/左键目标，toast 干扰后会错给物品；应以实际 pointer target 判定。DTMAPI 自身 hotkey suppression 不等于游戏原生输入被隔离。两者是后续 typed/modal 方案的历史来源，旧 Bootstrap/GameBridge 物理归属不再是当前产品归属。

来源：[Mine/Animal/Y 手测代码审查](../../archive/reviews/manual-qa/2026/20260605-0001-mine-animal-yconsole-code-review.md)。

## Y、Escape 与旧订阅 ABI 的消费边界

Y 关闭后重开的直接原因曾是 UI host 的 raw Y Close 与普通 Mod 的 typed Y Toggle 同时消费同一边沿；早期 smoke 直接调用 Close，因而漏掉了真实 toggle 路径。Y 的当前契约应跟随产品和输入 owner。历史最终兼容方案没有把旧 DLL 的关闭动作交还 host：只在当前 DebugConsole modal 的 owner 存在 legacy Y 注册且不存在 typed Y 注册时，定向派发其真实 ButtonPressed handler，关闭后抑制同一物理周期的普通广播。验收同时绑定 owner route、旧 handler 日志、modal close 三者 1:1，并验证实际 Workshop DLL 来源。曾提出的 host-direct fallback 已在原文中被否决。来源：[Y 双切换及后续 ABI 反证](../../archive/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md)。

Escape 的 raw fallback 在当时仍覆盖 typed sampler 丢失的短按，不能照搬 Y 的删除方案。关闭后 native EnterUICheck 仍可能消费同一 Escape 并打开暂停菜单，因此需要关闭期间持续执行的有界 release drain，而非只在 IsOpen 时清理标记；同时 title settings host 只能关闭自己拥有的 modal。该历史集成矩阵和存档恢复方案属于当次跨 owner/旧 ABI 验收，普通 Y 修正的当前测试范围由产品验证工作流决定。来源：[同一 Review 的 7 月 15 日集成追踪](../../archive/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md)。

玩家日志诊断应优先保留具体 inner exception、owner、对象生命周期和小范围状态。Y 已触发后立即关闭是消费路径证据，不应误判安装失效；旧版本首次黑屏、收集前多次启动的大日志、第三方噪声也不能自动解释当前问题。收集器曾因单个 DirectoryInfo 的标量 .Count 失败，需要 0/1/多项边界，而非重新跑产品行为。来源：[诊断与右键/移速审查](../../archive/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md)、[Y 与日志收集复核](../../archive/reviews/manual-qa/2026/20260617-0001-player-y-console-and-collect-log-review.md)。

## 反射、内容和实机几何的具体漏口

天气目录曾用字符串 1..7 查以枚举名为键的原生表，导致调用前全部禁用；怪物后置条件曾对 boxed 值类型 proto 做 ReferenceEquals，随后又把 Vector3 直接传给需要 Vector2 的反射方法。修正分别针对真实枚举键、值语义和按目标签名构造实际 Vector2；MethodInfo.Invoke 不执行 C# 用户定义的隐式转换。动物成功、天气成功不能代替怪物路径验收。来源：[首轮 1.1.0 玩家反馈](../../archive/reviews/manual-qa/2026/20260813-0001-y-console-110-player-feedback-layout-native-actions.md)、[第二轮反馈](../../archive/reviews/manual-qa/2026/20260813-0002-y-console-110-second-player-feedback-monster-dense-world-ui.md)。

正方形卡片根并不保证子文本留在框内；全拉伸锚点再加固定偏移曾把名称推出卡片。宽屏世界栏强制 ScrollRect 和固定 8 项传送分页也违背了 22 项同页的玩家用途。知识保留 RectTransform 与实际点击/显示区域的因果，尺寸与目录现值跟随 UI owner，不把历史 1500×820、44 像素等验收数值复制为新的规则表。来源：[第二轮布局复核](../../archive/reviews/manual-qa/2026/20260813-0002-y-console-110-second-player-feedback-monster-dense-world-ui.md)。

2026-08-30 用户明确接受动物生成后的保存/重载/隐藏产物，以及普通机器燃料消耗与保存/重载，原先等待的同一矩阵无需重跑；这是人工验收，不转写为自动 smoke。旧 0.3.1 消费者的保留限制也被解除，但当时当前产品仍消费 28 个 Debug DTO/enum，不能据此直接删除整个 ABI；实际退役应追对应 breaking cleanup owner。复合 space_ship 的旧回滚/熔断建议已被可见部分成功取代，1→3、10→30 有运行和后续玩家观察，Boss 保存安全仍未被该证据证明。后续 Y 测试 slot 10 的用户指定由当前测试 owner 管理，历史 slot 3 记录不改写。来源：[1.1.2 接收与退役 Review](../../archive/reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)、[1.1.0 限制后续取代](../../archive/reviews/manual-qa/2026/20260813-0003-y-console-space-ship-release-limitation.md)。

动物目录后续以运行时 TbAnimal 的真实 proto 为权威，custom-animals.json 只提供物种来源映射，不能单凭声明展示可生成动物。统一来源计数按当前可选卡计算，Child/Adult/Ready 是三张；分类筛选不重建来源集合，也不接管其他 Mod 的普通物品发现。旧传送 CSV 和“当前位置”玩家控件可以删除，但动作前后 TeleportSnapshot 仍服务结果验证。删除导出控件本身不需要触发原生传送或保存。来源：[动物来源与传送旧辅助 Review](../../archive/reviews/manual-qa/2026/20260831-0001-y-console-animalpack-catalog-and-teleport-ui-debt.md)。

8 月 31 日收尾还区分了‘保留状态’与‘保留结构’：搜索、来源、分类、页码字段已经跨关闭保留，未完成的是 Catalog chrome/listener 结构复用。后续十五项与 F1/F2 的唯一路线 owner 是 [路线归一化 Update](../../updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md)，知识页不复制另一张进度表。该记录也将 nullable 清理改为已闭合，因为完整编译已 0 warning；旧十六警告不能再作为新任务前提。量化 Mono/Unity 分配仍是另一个可选性能任务，不能用行为 smoke 虚报收益。来源：[轻量化最终玩家接收](../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)。

运行时动物目录的原 Unit 只证明 TbAnimal 新物种枚举和来源，不穿过真实 native host；当时 runner 不能选中并断言精确 AnimalPack source/category 组合，故没有制造一个较弱游戏 PASS。后续 1.1.2 整体玩家观察已接收，但具体原 Update 的未验组合仍应由当前 owner 对账，而不是知识迁移自动补成实测。来源：[目录实施证据边界](../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)。

早期官方控制台研究提供调用线索，不把约 490 条命令转为玩家产品范围。原生推进一天/时间段与改日期字段、倍速与移速、配方资格与无耗料、机器时长与电力分别有 owner；跨房间材料查询仍需真实扣除事务，一次生成资源也不是刷新点。首轮 `AdvancedDebug=true` 曾同时包含仅 toggle 的 creative、未载入 generator 和缺方法的 monster，后继补真实 Hook/原生结果才关闭该轮。按总布尔数或总命令数报告能力会掩盖这些差别。来源：[6 月 6 日官方控制台审查](../../archive/reviews/manual-qa/2026/20260606-0003-official-console-yconsole-new-mods-review.md)。
