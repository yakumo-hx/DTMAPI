# Hook 所有权与验收知识

本文合并历史阅读中可复用的定位线索，不拥有当前 Hook 状态、产品身份或测试门。当前 Hook 事实查 [focused maps](../../hook-map/README.md)，身份与保存语义查 [PROJECT](../../../PROJECT.md)。原件与逐篇阅读记录在 [API 阅读清单](../../archive/migrations/20260908-workspace.json)。

## 可复用的分析方法

- 从原生责任方法及实际状态 holder 定位 Hook；同一方法被两个产品观察，并不自动构成共享状态或 SharedNative 抽取理由。
- 多目标安装先完整解析，再原子安装；失败独立执行本地状态清理与 exact-owner unpatch，不能因前一步抛异常跳过后一步。
- 生命周期证据应区分产品实例、真实 Harmony patch/target、callback、Core roots 和 native transient；内部计数不能替代实际 owner 查询。Mono 中 resident 不表示程序集可卸载。
- 冻结兼容实现与新 ProductNative 实现是不同消费者路径；历史兼容 PASS 不能作为新产品字节的行为证明。

## 原生定位线索

以下为所读记录自身声明的基线，未据此推断当前游戏签名仍相同。

| 领域 | 记录基线与责任边界 | 当前事实入口 |
| --- | --- | --- |
| OneActionComplete | build `23762374`；`ToolCollider.HandleTools(Collider2D)` 的原生首击后观察、`DungeonResource._Fell(ResourceFellData)` 付费额外击打、`AgentStateInteract.OnExit()` 燃料/饲料后处理 | [ActionCompletion](../../hook-map/focused/ActionCompletion.md) |
| ActionSpeed | July 基线 `23762374`；action state 进入/退出保存与恢复 Animator；全局命名空间 `AgentStateBase` 曾被误判。August 6 的 watering 扩展记录为十一 Hook，旧九 Hook 游戏证据不覆盖新增路径 | [ActionSpeed](../../hook-map/focused/ActionSpeed.md) |
| AnimalViewer | `23762374_public_C416D4`；`AnimalFullInfoData::.ctor`、`AnimalViewer::Show`、`AnimalPanelUiState::Unregister`；产品绘制只读行/clone，共享 item title adapter 不拥有动物状态 | [AnimalViewer](../../hook-map/focused/AnimalViewer.md) |
| FishRoeTitle | 同上；`Item::get_title()` 装饰，native lookup 失败保留原文本；空生成表不是内容增长能力的兜底 | [FishRoeTitle](../../hook-map/focused/FishRoeTitle.md) |
| ChestLocator | 同上；三参数 `ArchiveDataHandle::GetAvailableInventories(Vector2Int,Vector2Int,bool)` 只拓宽返回集合；native 库存 identity、Count/Cost 与保存仍由游戏处理 | [ChestLocatorEnhancer](../../hook-map/focused/ChestLocatorEnhancer.md) |
| Advanced synthetic | `23762374_public_C416D4`、`Assembly-CSharp` SHA `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`；只观察 `DolocAPI.Has087DemoData()`，不改布尔结果或存档 | [AdvancedFixture](../../hook-map/focused/AdvancedFixture.md) |

## 未完成事项与历史边界

- 所读 ActionSpeed map 的 `manual-watering-implemented-source-unit-verified-pending-runtime` 是该记录的待验收状态；后续状态由其 linked Update 与当前 map 判定，不能因迁移文档直接关闭。
- Oil 的旧独立随机/背包投放 Hook 已在记录中退役；历史替代路线为官方 item-spawn JSON 扩展与 native `GuaranteedManager.SpawnResourceDropItems`。此结论不授权新增 drop API 或恢复旧 Hook。
- 各产品冻结兼容冲突规则、确切 Hook 数、当前证据与剩余门留在原 owner；本知识页不复制新的测试清单。

## 事务、清理与场景边界

- [Mine](../../hook-map/focused/Mine.md) 的 scheduler 是 session-derived，native Case 库存和电力才是保存状态。失败的 restore closure 不能被从队列提前移除；记录中的 recipe/tech 恢复失败重试仍有待验收归属。
- [StrongPlantingGun](../../hook-map/focused/StrongPlantingGun.md) 说明物品在 title 期间已经反序列化，因此 SaveLoaded 有界扫描可以补齐构造期遗漏，无需每帧扫描。持有 mutation 后必须完成成功或精确回滚，不能再 fall-through 原方法；一处 UI receiver 失败不能阻止其他 receiver 重同步。
- [Camera](../../hook-map/focused/Camera.md) 将 product scale 缩回 orthographicSize；native RefreshResolution 暂时观察真实 1x 基线，Finalizer 恢复倍率且传播原异常。重复 native callback 不能把自己已放大的值重新捕获为 baseline。July camSize/range 恢复证据是旧阶段证据，不能恢复被 August 修正移除的写权限。
- [NativeUiLayout](../../hook-map/focused/NativeUiLayout.md) 是已记录的不同清理类别：无独立物理 unpatch 证明时保留 `ProcessPinnedDormant` 并释放 callback/更新 roots，不能套用产品 exact-unpatch 结果宣称卸载。generic Hook 失败亦不能用另一 exact Hook 的 readiness 掩盖。
- [NativeLoadContinuationQa](../../hook-map/focused/NativeLoadContinuationQa.md) 绑定 `23762374_public_C416D4` 的四组方法；诊断启用与关闭由可选 QA owner 管理，失败关闭必须可重试。该探针生命周期 PASS 不关闭 ISSUE-010 的 GC 问题。
- [WorkshopSourceAuthority](../../hook-map/focused/WorkshopSourceAuthority.md) 绑定 `23762374_public_C416D4`。订阅 ID、原生安装目录、enablement enrichment 与目录存在是不同事实；官方页面 preview 不能直接发布，精确 manager 的成功保存和 close 回调之后才 next-frame commit。源码里的生成 callback 名称应随对应 build 查证。

July 的 [Hook 阶段摘要](../../archive/hook-map/2026/phase-summary-20260706.md) 与 [API 阶段摘要](../../archive/reviews/api/2026/20260706-0001-api-native-owner-phase-summary.md) 含“所有脆弱逻辑进 GameBridge”和 Goal 交接旧规则；它们只保留历史语境，现行 ProductNative/SharedNative 分类与工作流查 PROJECT/当前路由。

## 早期修复保留的失败知识

[June 生命周期隔离 review](../../archive/reviews/api/2026/20260610-0001-lifecycle-callback-isolation-review.md) 记录一个通用失败模式：低层 native boundary 连续执行多个 cleanup/通知时，单个最外层 catch 仍会跳过后续步骤。应按独立责任隔离，尤其 phase 通知失败不能跳过 Animator 恢复。其固定五 smoke 是当时变更的验收，不是每次产品修复都要执行的套装。

[Camera 0.4.2 Update](../../archive/updates/2026/20260607-0014-camerazoom-api-rebuild.md) 留下两项失败证据：`SetPosition` 的 `Vector2` 参数不能用 `Vector3` 假定替代；异步截图 API 返回不代表文件已经存在，必须等实际文件再收口。其 background/fog compensation 权限和版本前置条件属于 June 方案，已不能替代当前 Camera map。旧 active Goal 也曾因把手工 QA 当成实施任务造成阻塞，随后由 [归档决定](../../archive/updates/2026/20260607-0013-archive-041-unblock-api-rebuild.md) 解除。

## 阅读来源

[Hook router](../../hook-map/README.md)、[ActionCompletion](../../hook-map/focused/ActionCompletion.md)、[ActionSpeed](../../hook-map/focused/ActionSpeed.md)、[AdvancedFixture](../../hook-map/focused/AdvancedFixture.md)、[AnimalViewer](../../hook-map/focused/AnimalViewer.md)、[ChestLocator](../../hook-map/focused/ChestLocatorEnhancer.md)、[FishRoe](../../hook-map/focused/FishRoeTitle.md)、[Fishing compatibility](../../hook-map/focused/FishingAutomationCompatibility.md)。

## 性能与最小运行门的来历

[July 10 hot-path review](../../archive/reviews/api/2026/20260710-0001-autofishing-hot-path-native-cache-review.md) 在 `23762374_public_C416D4` 把读 snapshot 触发 reflection/scene scan/状态变化的问题收敛到 Hook-fed cache。可复用规则是 snapshot 保持只读、温热路径缓存 typed delegates、先判断是否需要日志再构造文字、scene 引用失效与 process-stable type delegate 失效分开。

[seam review](../../archive/reviews/api/2026/20260710-0002-autofishing-hook-runtime-seam-review.md) 明确先用一条可达路径证明 Mono Expression.Compile，而不是先做 100/500 fish 或 soak；前次外围 marker 缺失不否定实际已跑通的路径。它还纠正 NextState 只是返回状态并不替 StateManager 提交，Avoid enum 在该 build 不存在时应按可选能力处理。

[July 11 reliability review](../../archive/reviews/api/2026/20260711-0001-autofishing-reliability-version-freeze-review.md) 的 10 分钟/100/500 鱼上下文是特定内存性能调查，不能成为每次功能修复的默认门。它的有效机制是完整 Hook set 才 acquire，activeSession 为 commit 点、之前反向回滚，已消耗输入边沿还需 native phase 确认，阶段失败不回落到每帧 reflection。

[计数器 review](../../archive/reviews/api/2026/20260711-0002-autofishing-performance-counter-reel-telemetry-review.md) 用同线程保持存活的 4096-byte 分配校准，证明 Mono 有方法但计数始终 0；于是撤销 allocated=0 结论，保留相同运行里的 cast/log/accessor/cleanup 事实。native GC 次数也不等于线程字节数。计量工具必须先证明可用，不能为取得一个虚假的数字继续延长运行；blocked 字段用 null 而非 0。detach 前一次性累计诊断快照，避免清理后丢数或重复计数。

该 review 当时留下的 500ms rearm 重复 native energy/reel 风险，随后由 [native-reel acceptance review](../../archive/reviews/api/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance-review.md) 收口：在 `23762374_public_C416D4` 的 NextState Postfix 同步确认返回不同状态，故障注入延迟 phase 超过 500ms 仍只发生一次 effect/acceptance，不重发。不能继续把这个已关闭的特定问题列为新修复前提，也不能用 getter-consumed 数字代替原生副作用证明。

同一 review 又纠正“allocation 指标不可用就停止整个场景”：不可用的子指标应是 null，独立 process GC/趋势继续按任务时长采样；Windows managed process memory 也曾为 0，需功能性校验后用 native provider 或 null。只有当任务本身调查这些趋势才需要该测量，不用于普通行为修复。

[stable-window review](../../archive/reviews/api/2026/20260711-0004-runtime-memory-stable-window-review.md) 与 [smoke boundary review](../../archive/reviews/api/2026/20260711-0005-autofishing-smoke-architecture-boundary-review.md) 已以一次 1800 秒、61 samples 的 inactive 运行归因共同 warming/flattening，并明确停止进一步 inactive/no-consumer 和平台分层运行。保留的是 bounded roots、trailing/Gen2 low-water 解释，不是“AutoFishing 永远无泄漏”。当时剩余 active 调查被压到发布前约 10 分钟或 10–20 鱼，100/500 继续推迟；最新实际需要仍归当前 ISSUE-010 和产品验证流程。

primitive smoke 与 frozen compatibility smoke 分开，避免新产品测试为了读计数反而构造 legacy service。July 的“产品不得含 native/Harmony”静态门属于当时架构，后续 ProductNative 已取代，不能在迁移时恢复旧禁令。

## 总表中应保留的精确失败机制

[June–July Hook 历史总表](../../archive/hook-map/2026/README-history-through-20260711.md) 的运行条目与后续补记已归档，以下保留定位经验，不新增测试门：

- `23465763 workshop` 的存档 UI 问题不是任意布局漂移：从继承的 `DolocGridUI<T>.Select` 解析到的 Hook 误及 HomePageTextMenu，又经 SaveSlots restore 路径写入 `ResetLayoutSize(2)`。找到实际 writer 后移除了轮询修复、Update 修复和全局 GridLayoutGroup setter 归一化。June 13 `GAME-SMOKE/20260613-142631` 留有栈，后续 `143434`、`143621` 和 `144038` 覆盖修正后的标题/暂停与固定 12 槽。实际写入者已修复时，旧补偿不应继续成为永久层。
- SaveLoad coordinator 的原生 `LoadGame` 可能在 `SaveLoaded` 后才返回；迟到的返回必须关联同一已完成 request，不能创建第二次载入身份。内部重复请求可以抑制，原生 UI 点击只能诊断，不能随意屏蔽。一次 3600 秒无故障不证明已解决所有 fatal GC；后来探针只有 `LoadGameEnter` 的崩溃属于独立未完成调查。
- 原生输入 drain 在 July 10 采用 `NormalGameState.OnUpdate` 游戏阶段路径，PlayerLoop 仅作标题/非 normal 后备，250ms Timer 只检查健康；一个 Unity frame 只消费一次边沿。外部按键发送工具未成功和用户 F6 实际通过是两种证据，不能前者失败就否定后者，也不能 timer 代替模拟帧。
- 失败聚合需要区分累计次数和告警频率。总表记录首条完整、接着两条简报、以后 30 秒汇总，连续三次成功重置 episode 但不清累计数。这是历史诊断实现经验，具体阈值不提升为通用规则。

旧总表中的少数 pending 已被自身后文或 later Update 关闭，包括 Workshop 原生上传在 June 15 用户重启后成功、固定 12 槽和 July 11 fishing native-reel acceptance。对新修复应先查最终 owner，不能按每段旧状态重复建立阻塞。