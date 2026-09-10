# 自动钓鱼：生命周期与测量经验

历史研究入口；当前产品行为查 [AutoFishing](../../../products/first-party/AutoFishing/README.md)，API 状态查 [API matrix](../../api/public-api-matrix.md)，长期崩溃归 [ISSUE-010](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)。原件内旧 GameBridge 归属和 stable 字样不自动转成当前规范。

## 三种成本需要分别证明

2026-07 研究区分了原生对象键残留、诊断记录生命周期及每帧临时分配。正常 StopGame、离开 Ready、Pull exit、disable、save/title 边界各自负责不同对象；某个“expected clean”日志不是实际清理。短轮 transient=0 也不能排除异常路径或长时间累积。

当时 ResourceLifecycleLedger 只保存对象标识字符串，却将每条已释放的 SaveLifetime 记录保留到本代存档结束，且每次 observe/release 全量复制、排序、格式化并发布。它可以在原生对象正确释放后仍造成随捕鱼次数增长的诊断成本。反射、场景扫描、在已处理 guard 前创建 UI 列表、每帧 key 解析则属于另一类分配压力。

这些是历史具体缺陷的解释，后续记录已称部分聚合、缓存与 demand 激活修正；不能直接把它们列成当前未修 bug。`EnvironmentReset` 高频且非销毁边界，不能为了“清干净”中断正常钓鱼。应复用真实原生 cast/wait/reel/result 责任，避免修改 gameplay 语义来制造性能通过。

## 性能结论的边界

AutoFishing 与 ActionSpeed 操作不同原生状态域。各自比较 1x、启用不加速、常用/高倍率、禁用恢复和标题循环，记录每分钟与每完成动作双分母，才能区分吞吐增加、单次成本增加和残留阶梯。未工作的 Mono allocation 计数应记不可用，不写零。旧特定版本 L0–L5 发布门不是每次普通修复的默认全套测试。

来源：[生命周期归因](../../archive/reviews/code/2026/20260703-0001-autofishing-lifecycle-attribution-audit.md)、[五轮长玩 GC 研究](../../archive/reviews/code/2026/20260708-0001-autofishing-longplay-gc-research.md)、[独立速度阶梯](../../archive/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md)。

## 测试前提应支持正在进行的动作

2026-07 的 QA 连续失败展示三类独立问题：已有 fishing session 后还要求 NormalGameState，会阻止它自己制造的非 Normal 状态继续推进；disable 后 pool cache 失效，却在 TryCast 刷新前要求 composite CanCast，会形成循环前提；产品最后一杆尚未退出就创建恢复 session，可能把继承来的 PullExited 当成 QA 发起的恢复。正确的证据需要区分入场条件、运行态推进和该动作的发起者。

no-water 失败后为第三档寻找码头，是遗漏既有第五档真实池塘约定的绕路。正式阶段还需读回当前 selected rod 及体力/精力预算，不能从历史截图或快捷栏位置推定准备完成。探索传送可以保留其有限证明价值；没有进入 warmup/measurement 的超时不产生性能梯度。当前测试保存模式沿 PROJECT，不复用旧文档的例行备份指令。

一次六阶段运行有完整行为、恢复和退出证据，但 QA 每帧构造诊断 snapshot，约 60Hz 的 observer 开销污染了 GC 结论。后来将进度与采样数据分离，标量读取不触发其自身计数，并约束观察者预算。性能证据无效不使同次有效行为证据消失。

AutoFishing 迁出 GameBridge 后仍保留大量跨边界时期的 session/router/helper；这解释了所有权正确但实现仍重。玩家 src、可选 qa、Runtime 兼容层分别度量；统一配置菜单当时已复用，重复标签注册不是最大成本。删除或合并层应说明其保护的真实行为，不能按行数目标裁减恢复。

来源：[正常态死锁](../../archive/reviews/code/2026/20260719-0005-batch5-autofishing-native-session-normal-state-stall.md)、[第五档与预算纠正](../../archive/reviews/code/2026/20260719-0007-batch5-autofishing-fifth-save-energy-budget.md)、[观察者效应](../../archive/reviews/code/2026/20260719-0009-batch5-autofishing-performance-observer-effect.md)、[L4 独立恢复](../../archive/reviews/code/2026/20260721-0002-batch6-autofishing-l4-recovery-root-cause.md)、[产品减重](../../archive/reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md)。

## 语义边界比行数目标更重要

8 月与第三方续钓功能的只读对照发现，DTMAPI 当时玩家生产源码 5,647 行、QA 8,476 行；第三方约 240 行是更窄的手动首杆后续钓语义。DTMAPI F6 持续任务、每轮选中鱼竿、失败重试、可选等待/skip/动画与恢复外壳解释了差异，但不证明每层都必要。第三方专有实现仅作语义研究，不能移植代码。普通 native 结果 owner 相同，也不意味着 InstantBite 等选项与普通钓鱼概率节奏相同。

后续 1.00 方法体审查把 8 月 2 日的两项建议收紧：仍保留原子 22 patch，没有证据支持任意拆开；movement 必须同时读原生 MoveModifier.inputMultiplier 与 VelocityX，键盘 fallback 不覆盖控制器/改键且混入非移动键。Ready/Pull 的新增 base 行为由 Postfix 原样保留，不复制游戏方法体。

InstantBite 的 native RollFish 已耗 RNG 并创建 FishItem，恢复四个 Wait 字段不构成完整 rollback。AF-D2 后选单向 fault-close/reconcile：probability、duration、hasRolled 先完成，waitForFishBite=false 最后作 commit bit；不确定时停止产品自动化并交给原生/玩家继续，已提交后 tip/renderer/read-result 失败只作诊断，不能再伪称未提交并重复 roll。此处是审查结论，实施与当前接受仍查 owner Update。

来源：[语义和维护量对照](../../archive/reviews/code/2026/20260802-0001-autofishing-qiuzy-semantic-size-and-feature-baseline.md)、[1.00 native 与 AF-D2 决定](../../archive/reviews/code/2026/20260804-0008-autofishing-100-native-body-and-af-d2-review.md)。

## 预飞的反证链与测试停止条件

8 月第五档被误判不存在，实际是 runner 仍查旧 `ea-playtest-doloc-archive` 名，当前前六档使用 `doloc-save-{index}.data`。槽位仍按 UI 序号减一，不按第五个非空文件映射。这个历史“需要用户恢复存档”的 blocker 已撤回，不能再据此让用户造夹具。

随后强制 D 朝向预飞把原已朝右且中性的角色移到新位置，连续数次菜单/等待/更长 D 都未消除 tiny X。最初只修 QA、禁止 epsilon 的判断又被更完整 native 链纠正：直接 input 精确非零取消；base 前旧 VelocityX 采用原生 `abs > 0.001f`；base 后 OffsetX 精确非零取消。三条必须分开，不能把任意旧速度非零一概当移动。ground 初帧 false/false 可在同位置、其它条件均成立时有界等待真实接地；第一条已完成原生鱼若没有产品 CastApplied，只能一次性丢弃并整体重建计量基线，不能拼接下一条 cast 造 full-loop。

这些旧记录的重跑要求并不统一：有的 QA-only 修复要求八 profile 从头、随后整套 GC/Manager；另一份明确允许不变产品 L0–L4 与后续 L5 组合。它们保存发生时的范围，不能覆盖当前产品验证流程。PowerShell 分支内 @() 在外层赋值仍可能被展开成 null/scalar，曾让正常绿色 cleanup 在 Count 处失败；这是 runner 输出形状问题，不是产品行为失败或应放宽删除前提。

来源：[存档名误判撤回](../../archive/reviews/code/2026/20260804-0010-autofishing-fifth-save-fixture-availability-review.md)、[D 预飞反证](../../archive/reviews/code/2026/20260804-0012-autofishing-facing-preflight-neutral-handshake-review.md)、[数组形状](../../archive/reviews/code/2026/20260804-0013-release-autofishing-managed-root-cleanup-array-shape-review.md)、[首帧接地](../../archive/reviews/code/2026/20260804-0014-autofishing-direct-neutral-load-contact-race-review.md)、[native 三条件修正](../../archive/reviews/code/2026/20260804-0015-autofishing-l5-reentry-direct-neutral-refresh-race-review.md)、[首条循环归属](../../archive/reviews/code/2026/20260804-0016-autofishing-behavior-initial-nonproduct-cast-race-review.md)。

8 月 31 日压力审查有更直接的优化证据：编译 IL 在 inactive guard 前创建捕获对象，14 个 callback 存在该模式，一次 MiniGame Update 链至少七条 newobj。先移除逐调用闭包/委托并保留 fault isolation；按 phase 分开逐帧 movement、到期 cast preflight 和 transition watchdog。bindings/facade 合并、默认 animation lazy-create 主要是维护/启用成本，InstantBite 最多 100 native roll 的尖峰需先量 attempts/elapsed；不能虚报为已测 GC 收益。该审查的未完成整改仍由原 Review/后续 Update 接手，不自动变成实施已完成。

来源：[8 月 31 日 AutoFishing 压力审查](../../reviews/code/2026/20260831-0004-autofishing-technical-debt-and-runtime-pressure-audit.md)。

## 输入、动画与测试驱动的历史纠偏

2026 年 6—7 月的手测逐步把“全自动”收敛为原生抛竿、等待、收线、小游戏、收获与再抛竿的完整链。早期约 0.75 秒后直接写小游戏成功状态，后来被玩家明确否定；绿条按住、红条释放、黄条单次按下均应让原生小游戏更新消费。Ready、Cast 与 Pull 的动画、计时和抛物线并非同一 owner；只改 Animator 或只观察最终截图，不能证明抛竿全过程加速。默认零蓄力也仍保留原生准备动画完成条件。具体当前实现以产品源码和 Hook owner 为准。来源：[原生循环](../../archive/reviews/manual-qa/2026/20260613-0005-autofishing-native-loop-fifth-save-review.md)、[小游戏与动画反证](../../archive/reviews/manual-qa/2026/20260613-0006-autofishing-minigame-animation-follow-up.md)。

测试本身曾阻塞 Unity 主线程：逐项轮询中的 40 次 125 ms Sleep 与 1,100 ms 移动注入造成首次抛竿停顿。这不能归因于产品。另有 PowerShell 整数重载把 0.5 蓄力截成 0、最后一次 Ready 状态被 Cast/Pull 覆盖、已有证据文件名匹配错误等误报。测试应记录实际输入值、对应阶段与真实 native 结果，等待应由可恢复的分帧驱动承担。已知玩家位置与装备直接形成测试前提，不用重新合成另一套池塘/角色状态。来源：[输入与蓄力修整](../../archive/reviews/manual-qa/2026/20260710-0001-autofishing-input-charge-polish-review.md)、[动画追踪修正](../../archive/reviews/manual-qa/2026/20260613-0006-autofishing-minigame-animation-follow-up.md)。

短按与重复 F6 的历史修复说明，“已安装回调”不能代替实际每帧回调证据：InputSystem、SynchronizationContext 与 PlayerLoop 都曾在原生加载后失效，最终转向游戏内稳定的 NormalGameState 更新边界。多输入源还曾在同一次按住周期重新产生 pressed；按物理按下/释放周期及帧锁存处理，比统一时间去抖更接近根因。2026-07-10 最终用户手测关闭了当次视觉、短按与蓄力验收，但 18:53 那次 F6 实际由用户手按，不能改记为自动 40 ms 注入；单项行为通过但外部发送门失败的运行仍保留整体失败结果。该手测也没有关闭独立 GC issue。来源：[早期热键回归](../../archive/reviews/manual-qa/2026/20260707-0001-hotkey-autofishing-regression-review.md)、[输入驱动与最终人工验收](../../archive/reviews/manual-qa/2026/20260710-0002-autofishing-input-gc-ready-review.md)。

## 正式构建与玩家反馈的判别前提

2026-07-31 的“约 30 秒停钓”实际在 7.932–56.441 秒不等的首个 bonus note 发生：Author SDK 正式编译启用 `checkOverflow: true`，identity hash 乘 397 溢出；普通 Unit 当时没有链接 `FishingPrimitivesService`，只测决策返回 TapBonus，因而漏掉真实 HashSet.Add。异常仅撤掉 provider，留下 enabled 的半状态。有效回归需要正式 SDK 字节/等价 checked 语义、bonus 首次及重复登记、输入故障完整停用；跳过小游戏和完整 Release 都不能替代该触发点。Manager 的备用 Hook 错误与这次故障无因果关系。来源：[玩家 UI 与循环停滞长审查](../../archive/reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md)。

2026-08-22 玩家“F6 无反应”是另一种情况：日志已证明 22 个 Hook、输入和 session 正常，开启窗口被再次 F6 或完整水平移动值 -1 终止。站定、面向可钓水域、选竿、只按一次 F6 后，玩家复测正常，问题关闭且无需实现 Update、Debug issue 或新增 smoke 行。缺少订阅 DLL hash 仍限制精确字节归因，却不妨碍这次玩家问题按真实反馈关闭；该结论也不扩大成全配置或长时稳定验收。来源：[F6 玩家支持审查及复测](../../archive/reviews/manual-qa/2026/20260822-0001-autofishing-player-f6-no-visible-response.md)。

小游戏前提还要区分鱼和垃圾：2026-06-03 的失败运行实际钓到 waste_plastic_bottle，原生正确直接进入 Pull，不会创建小游戏。专门小游戏夹具可以明确保证 FishProto.IsFish，但不能把这种强制输入当成普通玩家随机结果，也不能把旧 0.76 秒写 Success 方案的 PASS 延用到后来原生按键小游戏。来源：[Skip=false 早期专项](../../archive/updates/2026/20260603-0016-autofishing-skipfalse-minigame-smoke.md)。

一次咬钩消耗双倍体力曾由 Wait.OnEnter 内再切状态引起：原生状态机在 OnEnter 返回后仍会把 current 赋为 Wait，覆盖产品刚进入的 Battle/Pull，随后 OnPlay 再推进一次并二次扣能量。历史最小修正是 OnEnter 只准备、原生进入完成后的 OnPlay 才收线；测试应穿过原生 transition 的完整顺序并统计实际扣能量次数。后续 ProductNative 输入路径继续按当前 owner 验证，不能仅直接调用单个回调。来源：[InstantBite 双扣体力](../../archive/updates/2026/20260613-0021-autofishing-instantbite-energy-cost.md)。

7 月的性能测量又提供了明确反证：Unity Mono 中 `GC.GetAllocatedBytesForCurrentThread` 方法存在，但保活 4,096 字节同线程分配后仍是 0→0。旧两条十分钟基线的 AllocatedBytes=0 被正式撤销为零分配证据；鱼数、accessor 稳定、原生移动和清理等独立证据仍可用。计数器应先行为校准，缺失/常数/异常则把计量区间记 Blocked/null；不能延长运行来弥补坏计量。7 月 10 日桌面微测及源码/Unit 通过也不是 Unity Mono 证明。来源：[热路径源码阶段](../../archive/updates/2026/20260710-0006-autofishing-hot-path-native-cache.md)、[错误零分配声明的修订](../../archive/updates/2026/20260711-0001-autofishing-reliability-053-preview.md)、[Mono 计数校准](../../archive/updates/2026/20260711-0002-autofishing-performance-counter-reel-telemetry.md)。

单鱼 Mono gate 曾用真实小游戏检出两个桌面测试漏项：直接调用 Wait.NextState 不等于原生状态机转入 Battle；可选且不存在的 FishingNoteType.Avoid 不能使整个 accessor 建立失败。输入 getter 已消费也不等于收线事务提交，延迟 phase 确认的重试必须计真实扣能量/收线次数。原生 primitive 产品 smoke 后来与冻结 legacy API 专项分离，普通路径不得用 legacy 回退凑 PASS。来源：[单鱼 Mono gate 与 seam](../../archive/updates/2026/20260710-0008-autofishing-hook-runtime-seam.md)、[计数与重试风险](../../archive/updates/2026/20260711-0002-autofishing-performance-counter-reel-telemetry.md)、[独立兼容 smoke](../../archive/updates/2026/20260711-0005-autofishing-smoke-architecture-boundary.md)。

2026-07-11 用户在 inactive 30 分钟曲线回落后明确停止重复 inactive/平台分层/延长时间。约 4.97 GB 回落波峰不作 AutoFishing 泄漏，闭合的只是 inactive 持续保留嫌疑；active 与 ISSUE-010 仍未闭合，后续范围收窄为约十分钟或 10–20 鱼，取代旧 100/500 计划。7 月 13 日又纠正为 Fishing 与 ActionSpeed 各自速度阶梯，只有观察到同一 Animator 重叠才新增仲裁设计；并装本身不是冲突证据。来源：[停止与缩小后续范围](../../archive/updates/2026/20260711-0005-autofishing-smoke-architecture-boundary.md)、[独立压力阶梯](../../archive/updates/2026/20260713-0008-autofishing-actionspeed-parallel-gc-direction.md)。
