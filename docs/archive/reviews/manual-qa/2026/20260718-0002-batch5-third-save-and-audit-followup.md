# 20260718-0002 Batch 5 Third-Save And Audit Follow-Up

## Review Header

- Status: recorded
- Time: 2026-07-18
- Source: user follow-up and `D:\图片\Screenshots\屏幕截图 2026-07-18 200419.png`
- Scope: durable review followed by the already-authorized Batch 5 implementation and validation continuation
- User constraints: use the changed third save; preserve the ordinary no-QA boundary; do not treat visually observed animals as a formal pass without a finalized receipt; slightly adjust the Zoom position route so the player is not sent below visible ground
- Related implementation lifecycle: [Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Detailed source/evidence audit: [Batch 5 Completion Audit](../../code/2026/20260718-0003-batch5-completion-audit.md)

## Issue Review

### 问题 1：第三存档出生点已移到大型畜棚右缘、牧铃右侧

原始反馈：

- 玩家出生点现在位于牧铃右侧一点、同时位于大型畜棚右侧边缘。
- 进存档后短按一次或两次 `A` 向左移动，即可显示牧铃的 `E` 交互提示。
- 理论上该位置不影响 Zoom，且场景右侧已经清空。
- 图片转写：截图显示雨季农场室外场景，左中部为大型红色畜棚，人物所在区域靠近畜棚右缘；画面右侧地面区域已留空。截图本身没有显示牧铃的 `E` 提示，因此“短按一至两次 `A` 后出现提示”属于用户实测事实，而不是由静态截图推导。

审查记录：

- 用户确认事实：第三存档已经改变；普通玩家输入从新出生点接近牧铃只需要一至两次短按 `A`，随后按 `E` 可进入 AnimalViewer 路径。
- 代码/文档事实：前两次当前树普通无 QA 运行 `20260718-190150` 和 `20260718-192244` 都没有形成通过的两次因果渲染回执；现有 runner 的等待窗口和交互驱动没有利用这个新的确定性出生点契约。
- Codex 推断：该布局可以把普通无 QA AnimalViewer 验收从长时间人工寻找牧铃，收敛为受限的前台普通输入序列，但仍必须以真实游戏输入、无 QA DLL/开关、两次不同动物因果渲染、原生关闭顺序和最终 `result.json` 为准。
- 反证/未证实：尚未用当前最终代码和新第三存档跑出正式通过回执；静态截图不能证明交互、渲染或关闭行为。
- 归属：普通无 QA runner/第三存档验收夹具和 Batch 5 Update；不改变 AnimalViewer 公共 API 或 native-owner 边界。
- 验收点：从第三存档载入后使用普通前台输入完成一至两次短按 `A`、`E` 交互、两个不同动物选择及原生关闭；回执同时通过无 QA、两次因果渲染、overlay 清理、关闭顺序、状态恢复和进程退出门禁。
- blocker 判定：在新回执通过前，普通无 QA AnimalViewer 仍是 P1 发布证据 blocker。

### 问题 2：Zoom 路线会把人物送到可见地面以下

原始反馈：

- 当前 Zoom 测试过程中有一段会把人物直接送进地图可见地面以下。
- 黑色部分是地图边界遮罩；当前没有明显功能问题，但会阻碍截图验证，未来若有其他机制也可能产生问题。
- 需要稍微改动位置设定。
- 图片转写：截图下方存在大片黑色区域，用户确认其为地图边界遮罩；可见地面与遮罩分界清楚。静态截图没有展示测试自动移动发生的瞬间，也不能独立给出安全坐标。

审查记录：

- 用户确认事实：当前 Zoom 测试的某个定位步骤会落到可见地面以下；目前主要影响截图验收。
- Codex 推断：即使相机缩放本身正确，把玩家放进边界遮罩也会污染截图可读性，并可能触发未来依赖地面、碰撞、交互或可见性状态的机制，因此不能作为稳定测试夹具保留。
- 反证/未证实：用户未报告崩溃、数据破坏或当前机制故障；不能把这一现象升级为 P0。
- 归属：Zoom QA/运行验收的位置夹具，而非玩家存档数据修复或相机 native owner 重做。
- 验收点：只微调测试目标坐标；完整 Zoom 序列中玩家始终留在可见、可站立地面，缩放与恢复仍通过，截图无遮挡关键对象，退出后第三存档恢复校验通过。
- blocker 判定：属于 P1 运行验收可读性与未来机制安全边界；修正前不得以受遮罩截图关闭 Zoom 验收。

### 问题 3：空队列仍在每帧前后重建完整事件/Hook 诊断

原始反馈：

- 空队列仍在每帧前后重建完整事件/Hook 诊断，未满足零需求性能边界。
- 图片转写：无对应截图信息。

审查记录：

- 代码事实：独立审计确认 `DtmApiRuntime.Update` 在帧首、帧尾都调用队列刷新，而空队列路径仍创建 Hook/Event 快照、字典和格式化摘要。
- 根因：诊断发布由每次刷新驱动，而不是由版本变化或有界诊断周期驱动；现有 10,000 帧测试绕过 Core `Update`。
- 验收点：空且无版本变化的 warmed 路径为标量、零分配；详细诊断只在变化或明确有界周期发布；加入包含 Core `Update` 的 10,000 帧调用/分配测试。
- blocker 判定：P1 源码/性能 blocker，修正并量测前不能声称零需求静默。

### 问题 4：需求归零后仍保留常驻回调或轮询

原始反馈：

- CustomAnimals、Camera 及多个产品的常驻回调/轮询，在需求归零后仍未真正休眠。
- 图片转写：无对应截图信息。

审查记录：

- 代码事实：独立审计确认 CustomAnimals 最后一个定义移除后物理 Hook 仍保留且部分回调在空表检查前做反射；Camera 环境回调仍扇出全 Feature；Equipment 终态 updater 与多个产品 evidence-only `UpdateTicked` 订阅仍常驻。
- 根因：需求协调器只控制服务内活动状态，没有完整闭合到 retained Hook 的最前置 fast path、产品事件订阅和终态需求撤销。
- 验收点：每个保留物理 Hook 在反射/诊断前有定义或需求 fast path；证据轮询退出玩家路径；终态需求真正撤销；whole-runtime 10,000 帧测试覆盖回调、文件/反射/native、事件和诊断计数。
- blocker 判定：P1 架构/性能 blocker。

### 问题 5：CustomAnimals 与 Audio 在生成事务完成前发布可见状态

原始反馈：

- CustomAnimals、Audio 在生成事务完成前已经替换可见状态；异常可能造成“状态已发布但生成被重排”。
- 图片转写：无对应截图信息。

审查记录：

- 代码事实：独立审计确认两条路径都先替换 live state，随后执行仍可能失败的资源观察、日志或生命周期发布，最后才完成 generation；后置异常会对已可见状态执行 requeue。
- 根因：事务提交点跨越了权威状态交换、generation 终态和非权威诊断副作用。
- 验收点：所有可能失败的 candidate 准备在发布前完成；live-state swap 与 generation terminal receipt 构成单一权威提交边界；提交后诊断失败不得重排；每个后置步骤有故障注入测试。
- blocker 判定：P1 一致性 blocker；在原子性测试通过前不能关闭 G1/G6。

### 问题 6：事件退订缺少运行线程约束并可能改变已开始发布的成员

原始反馈：

- 事件退订未执行与订阅相同的运行线程约束，并可能改变已经开始发布的事件成员。
- 图片转写：无对应截图信息。

审查记录：

- 代码事实：独立审计确认 `+=` 经 owner/runtime guard，而 `-=` 直接移除；publication 只冻结 registration cutoff，真正 dispatch 时重新读取当前 snapshot。
- 根因：退订绕过 owner-bound 资源变更边界，且 publication 没携带冻结成员快照。
- 验收点：增删使用相同的运行线程/owner-active 约束；发布准备冻结不可变成员；确定性 barrier race 测试证明发布开始后的退订只影响后续发布。
- blocker 判定：P1 线程与事件语义 blocker。

### 问题 7：CoreLifecycle 与 owner cleanup 统计不能权威反映物理清理

原始反馈：

- CoreLifecycle 路由和 owner cleanup 统计不能权威反映实际物理 Hook/资源清理状态。
- 图片转写：无对应截图信息。

审查记录：

- 代码事实：独立审计确认 CoreLifecycle 被声明为 mandatory physical Hook route，却没有真实安装/探针 delegate；owner cleanup 移除了 demand root，却没有把它计入 removed resources。
- 根因：需求路由的逻辑生命周期状态与基础 Hook 的物理 readiness 分属两套状态源，清理报告又遗漏一种真实资源类型。
- 验收点：CoreLifecycle 绑定准确的基础 Hook readiness/闭包；清理统计包含 demand roots；测试同时断言最终字典、物理状态和报告计数。
- blocker 判定：P1 生命周期可观测性 blocker。

### 问题 8：两次普通无 QA AnimalViewer 运行没有正式通过

原始反馈：

- 两次最新普通无 QA AnimalViewer 回执均失败且渲染回执为 0；截图中观察到的两条动物回执没有形成正式通过结果。
- 图片转写：当前新截图只展示第三存档场景布局，不展示两条动物渲染回执；先前口头观察不能替代当前 `result.json`。

审查记录：

- 证据事实：`20260718-190150/result.json` 与 `20260718-192244/result.json` 均为失败，正式 AnimalViewer render receipt 计数为 0。
- 根因边界：前一次载入过晚，后一次真实观察超过 runner 截止时间且未完成 runner 的原生关闭/终态收据；这不等于已发现 AnimalViewer 产品代码回归。
- 验收点：使用问题 1 的新出生点契约，生成一份当前最终树、普通无 QA、两次因果渲染、原生关闭、状态恢复和干净退出全部通过的正式回执。
- blocker 判定：P1 运行/发布证据 blocker。

### 问题 9：GC 梯度仅有计划，AutoFishing L4/L5 语义不独立，Catalog 过度声明

原始反馈：

- GC 梯度只有 plan-only 产物。
- AutoFishing L4/L5 当前没有行为层面的独立语义。
- Catalog 仍过度声明普通无 QA 已通过。
- 图片转写：无对应截图信息。

审查记录：

- 代码/证据事实：现有 retained GC artifacts 的完成 stage 为 0；AutoFishing runner 对所有非 L0 选择同一个 positive case，该 case 自带 stop/title cleanup，因此 L4/L5 只是标签差异；Catalog 的普通无 QA 句子没有绑定通过回执。
- 根因：长时运行前只完成了 plan/source contract，梯度等级没有映射到互斥的行为路径，发布事实校验依赖固定文本而非证据引用。
- 验收点：先为 L4 disable/recovery 与 L5 title-cycle 建立独立 runner case、行为回执和测试；立即把 Catalog 降级到当前失败/待验事实；修正后在锁内执行真实梯度并保留可用指标与 terminal receipts；只有新的普通无 QA 通过回执出现后才能恢复通过声明。
- blocker 判定：P1 运行验收与发布事实 blocker；plan-only 不能关闭 GC 梯度，Catalog 当前必须先纠正。

## Cross-Issue Summary

- 本轮没有用户报告或独立审计支持新的 P0 崩溃、数据破坏或存档损坏结论。
- 问题 3 至 7 是五组源码/架构 P1；问题 8 和 9 是两组运行验收与发布事实 P1。
- 问题 1 提供了可重复普通无 QA 输入契约；问题 2 要求只调整测试位置，不改变 Zoom 产品语义。
- 实施继续由现有 Batch 5 Update 统一拥有；本 Review 只冻结用户事实、根因边界和验收点，不承载完成声明。

## 2026-07-19 追加验收事实（保持原问题顺序）

以下只追加新证据与门禁结果，不改写上面的原始反馈或前置判断；实现范围、改动文件与最终生命周期仍由 Batch 5 Update 拥有。

1. **第三存档出生点 / 普通无 QA 输入契约**：`GAME-SMOKE/20260719-034102` 在当前 Local11 本地候选上正式通过。前台输入回执包含两组短按 `A` / `E` 尝试、第二行动物行的普通鼠标选择和 `Escape` 原生关闭；最终得到恰好两条因果 AnimalViewer 渲染回执，并同时通过无 QA、输入来源、原生关闭、overlay 清理、关闭顺序、第三存档恢复、官方启用状态恢复和干净退出门禁。这关闭的是本问题要求的普通无 QA 回执缺口，不等于 Batch 5 或 Published11 总体验收完成。
2. **Zoom 地面安全位置**：`GAME-SMOKE/20260719-031358` 正式通过 `Zoom`、`ZoomOwnerLifetime`、`GameBridgeZoomCleanupHealth`、`CameraPlayableEvidenceFiles`、QA 生命周期/清理、进程退出与无 Fatal 窗口门禁。七张 CameraPlayable 图片覆盖 before、4x、移动 start/mid/end、2x fallback 和 reset；复核中人物保持在大型畜棚右侧的可见地面。4x 仍会显露地图边界黑色遮罩，但没有遮断本次人物/缩放/恢复证据；因此当前只接受“测试坐标已避开地下区域”，不把背景/边界同步提升为已实现产品能力。
3. **空队列诊断**：Core 空队列路径已改为变更驱动/标量快路，稳定内部帧边界和空闲 Hook 调度有 10,000 次零分配单元门禁；最终整套 Release 重放仍待完成，因此这里只把源码 P1 记为已纠正、最终发布门禁待验。
4. **需求归零后的常驻回调**：保留的 CustomAnimals、Audio、Camera/环境和共享 Hook 回调已在装箱、反射、诊断与 Feature 扇出之前按精确需求位退出；10,000 次全族零需求保留回调测试断言零服务工作、零 Camera 扇出和零当前线程分配。产品证据 tick 与终态 Equipment recovery 也已改为有界生命周期；正式 GC 梯度仍不能由该源码/单元结果替代。
5. **CustomAnimals / Audio 原子发布**：两条内容路径已把可失败 candidate 准备移到可见状态交换之前，并把 generation terminal receipt 与权威发布边界绑定；提交后诊断失败不再把已提交 generation 重排。故障注入/last-good 测试覆盖该边界，但正式游戏 GC 梯度仍待跑。
6. **事件退订与冻结成员**：`+=` / `-=` 现在使用相同的 Runtime 线程和 owner-active 约束；publication 开始时冻结不可变成员，普通退订只影响后续 publication，权威 owner cleanup/quarantine 仍可取消已冻结但尚未调用的失效 owner。确定性 barrier 与离线程拒绝测试覆盖这一区别；API 状态仍为 Experimental。
7. **CoreLifecycle / cleanup 权威统计**：CoreLifecycle 路由现在由真实基础 Hook closure probe 决定 `Activating`、`Active/Installed` 或 shutdown 后的 `ProcessPinnedDormant`，owner cleanup 把 demand roots 纳入实际移除资源数；测试同时核对物理状态、最终字典和报告计数。
8. **普通无 QA AnimalViewer 正式回执**：旧失败 `20260718-190150`、`20260718-192244` 继续保留为负证据；当前接受结果由 `GAME-SMOKE/20260719-034102` 取代。`GAME-SMOKE/20260719-031553` 没有 `result.json`，原因是外层命令超时且没有完成真实存档导航；其 `interruption-recovery.txt` 证明进程、Local11 启用状态、Author source state 和第三存档已精确恢复。该目录只属于基础设施中断/恢复证据，既不是产品通过，也不是产品失败。
9. **GC / L4-L5 / Catalog**：AutoFishing L4/L5 与 ActionSpeed 已有独立行为语义，但正式长时梯度仍无 terminal evidence，不能关闭 GC 或 ISSUE-010。`20260719-034102` 已提供当前树普通无 QA 通过回执；`Batch 5 Final Worktree Candidate 20260719-040700` 的离线 Runtime 审计也以零 blocker 通过，但 Catalog 仍须同步该回执。Published11 仍因五个 Steam 树 hash 漂移在预启动阶段阻塞，Local11/离线包不能替代它。

## 2026-07-19 AutoFishing 长测体力夹具根因追加

- 用户约束：AutoFishing 正式梯度使用第五存档；单级长测会持续消耗体力，允许使用官方增加体力/精力的控制台命令维持真实钓鱼夹具。第三存档语义传送只保留为补充能力证据，不替代第五存档钓鱼基线。
- 正式证据：`BATCH5-GC-LADDER/formal-autofishing-final-candidate-20260719-1915` 的 L0、L1、L2 已分别完成 600 秒窗口、退出码 0 和第五存档恢复。L3 对应 `GAME-SMOKE/20260719-194658` 在约 260 秒处由 QA 主动失败；无 Fatal，`ProcessExited=Passed`、`ForcedClose=Passed`、`PlayerSaveRestored=Passed`，本地 AutoFishing source state 和产品树也精确恢复。
- 根因：QA 已通过官方 `DolocAPI.Command_ComposeEnergy`、`Command_ComposeSpirit` 补能并通过官方百分比命令读回，但维护节拍固定为 1000ms。4x 连续钓鱼可以在两个维护节拍之间把能量降到单次 `FishingEnergyCost` 以下；QA 在产品原生 `TryCast` 能量不足计数增加之前先抛出异常。因此本回执是测试夹具体力维护频率不足，不是 AutoFishing 性能、GC 或生命周期失败，也不是进程残留。
- 修正边界：维护改为 250ms 检查；仍只在原生两次抛竿 reserve 不足或精力低水位时调用官方命令，不写原生字段、不启用创造模式、不每帧无条件补能。官方 delegate 身份、命令后读回、阶段起止百分比、最终能量门禁和 `NativeTryCastInsufficientEnergyDelta == 0` 继续是硬失败条件。
- 重新验收：QA 单测与短梯度通过后，正式 L0-L5 必须从头重新运行；不得把本轮 L0-L2 与修正后的 L3-L5 拼接成一份通过结果。本中断保留为负向夹具证据，不计为产品失败。

## 2026-07-19 最终候选验收追加（保持原问题顺序）

1. **第三存档出生点 / 普通无 QA 输入契约**：最终候选事务 `CANDIDATE11/batch5-final-manual-handshake-20260719-214044` 通过真实游戏 UI 手动选择第三存档；进入存档后，runner 的第一次短按 `A` 和第一次按 `E` 已足以进入 AnimalViewer，因此备用 `A2` / `E2` 重试没有发生。`GAME-SMOKE/20260719-214048` 在截止时间前完成 YConsole、EquipmentSlots 与 AnimalViewer 普通玩家路径，AnimalViewer 产生恰好两条因果渲染回执，并通过真实输入来源、原生关闭、overlay 清理、关闭顺序、无 QA、精确恢复和进程退出门禁。
2. **Zoom 地面安全位置**：沿用 `GAME-SMOKE/20260719-031358` 的通过结论；人物保持在大型畜棚右侧可见地面，宽缩放下的黑色地图边界遮罩继续只作为未来机制风险记录，不阻断本轮截图与生命周期验收。
3. **空队列诊断**：`BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000` 在 300 帧预热后测量 10,000 帧；可选 demand/updater 为空，事件快照、事件/Hook 诊断 revision、可选文件/目录/投影/反射/native updater/保留回调的测量增量均为 0。真实 Unity 回执只声明“可选工作调用/节拍静默”；组合 `GameBridge.Update + DtmApiRuntime.Update` 的 10,000 次当前线程零分配由离线单元门禁负责。
4. **需求归零后的常驻回调**：同一 10,000 帧回执证明 CustomAnimals、Audio、Camera 环境、Fishing native refresh 和共享可选 updater 在零需求窗口均无工作增量；基础 Core UI、content drain 与 native UI 诊断仍按已声明 mandatory 节拍运行。
5. **CustomAnimals / Audio 原子发布**：最终候选继续绑定此前通过的 candidate/commit/reject 与 last-good 故障单测；本轮运行没有产生新的生成事务失败或状态先发布回执。
6. **事件退订与冻结成员**：最终候选继续绑定此前通过的同线程 add/remove、不可变 publication membership、owner cleanup/quarantine 与 barrier 单测；本轮没有放宽 API 的 Experimental 状态。
7. **CoreLifecycle / cleanup 权威统计**：Published11 enabled `GAME-SMOKE/20260719-212635` 和 disabled/CoreOnly `20260719-212758` 都通过 title button lifecycle、owner lifetime cleanup、存档恢复与进程退出门禁；enabled 还通过精确已发布产品组合，disabled 通过禁用组合断言。
8. **普通无 QA AnimalViewer 正式回执**：最终权威结果改为精确候选绑定的 `GAME-SMOKE/20260719-214048`。先行事务 `CANDIDATE11/batch5-final-20260719-212907` / `GAME-SMOKE/20260719-212913` 因普通无 QA lane 不具备内部自动载档、且本轮没有完成人工第三存档握手而在 SaveLoaded 前超时；它没有进入产品行为，保留为运行编排/人工握手超时，不计为产品失败。网络、Codex 前端或子智能体崩溃造成的截断同样只有在 runner 形成可解释的正式 terminal receipt 时才分类；其本身不属于产品测试失败。
9. **GC / L4-L5 / Catalog**：第三存档语义传送 `GAME-SMOKE/20260719-105158` 保留“按语义找到 `mark:下船点-左`、实际从农场传送到 `city_多洛可码头`、原生 readback 得到 `fishingPool=true`”的补充能力证据；它因随后未选中鱼竿而不是正式 GC 结果。ActionSpeed 接受 `BATCH5-GC-LADDER/20260719-044157-70ce39e6` 的 24 个独立 600 秒第三存档阶段。修正体力维护节拍后，`BATCH5-GC-LADDER/formal-autofishing-vitals250-final-20260719-2014` 从头完成第五存档 L0-L5 六阶段：每阶段 600 秒、21 个样本、`ForcedGc=false`、所有 11 类指标可用、正式行为/来源/存档恢复/退出回执通过；L4 的 disable recovery 与 L5 的 title reload cycle 分别有独立 terminal receipt。Catalog 已绑定最终 `214048` 回执，但这些有界通过不关闭更宽泛的 ISSUE-010/011。
