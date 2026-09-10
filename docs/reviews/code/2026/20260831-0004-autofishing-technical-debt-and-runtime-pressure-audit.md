# 20260831-0004 AutoFishing 技术债、冗余与运行时压力审计

- Status: `recorded`
- Date: `2026-08-31`
- Audited HEAD: `98d75c6c51300768e3d7a8b209c858c9ad110493`
- Scope: 当前第一方 `Yuuka.DTMAPI.AutoFishing` Advanced / ProductNative 产品源码、产品专属 QA、既有行为/GC 证据和冻结兼容边界
- Source: 用户要求审计基于 DTMAPI 的第一方 AutoFishing 技术债、冗余、过度设计，并研究可轻量化代码与可降低的运行时压力
- Runtime impact: 本轮只做审计；不修改产品/Runtime 代码，不安装或发布包，不启动游戏，不访问存档

## 结论先行

1. 当前玩家产品源码是 **25 个 C# 文件 / 6,054 物理行 / 5,591 非空行**。其中 `src/Native` 是 **21 个文件 / 5,277 物理行**，占生产源码 **87.2%**。相对 2026-08-02 的 23 文件 / 5,647 行基线，当前因 game-1.00 movement parity 与不可逆 bite commit 修复增加了 2 个文件、407 行；这部分增长有真实正确性理由，但也使下一轮减重更应集中在产品私有 native 外壳，而非 57 行决策器。
2. 当前最明确、最值得先修的运行时债不是“反射文件很多”，而是 **Harmony 热回调中的捕获 lambda 和逐帧委托构造**。当前编译 IL 证实：14 个 `Safe(...)` 回调会创建 compiler display-class；闭包对象在 inactive guard 之前构造。活跃小游戏每次原生 `UpdateGame` 的前缀、后缀、service 和 session 决策链合计至少有 **7 条 `newobj`**。这是按调用次数线性增长的确定分配点。
3. 第二个高价值方向是 **按 phase 拆分 native snapshot**。当前 F6 活跃 updater 每帧都读取 Agent、选中物品、正常状态、物品使用能力、状态管理器/current state 和三路 movement；movement 必须逐帧保留，但 rod/cast preflight 和 native-state watchdog 不必在所有 Wait/MiniGame/Pull 帧都全量执行。
4. 重复 native binding/cache、单产品 owner/session facade、双层 cast scheduler 和 QA-only retained state 是真实维护债；它们主要增加源码、激活期构建和排障表面积，**目前没有证据证明它们是主要帧耗时或 GC 来源**。应排在热回调去分配和 phase-aware polling 之后。
5. `InstantBite` 在主线程一次最多同步调用 100 次原生 `RollFish`，是明确的模式特定尖峰候选；但当前没有 attempts/elapsed 分布。它又跨过不可逆 RNG/item 创建点，不能为了“轻量”直接异步化、降低上限或回滚四个字段。
6. 当前 22-Hook 原子安装、sequence/重复结算防护、scoped input、原值恢复、movement fail-close、bite commit-bit-last、标题/存档/owner cleanup 和冻结旧 ABI 不是过度设计。更晚的 native-body Review 已明确保留完整 22-patch 事务；本轮没有新的 body/callsite 证据授权拆组。
7. 产品专属 QA 当前是 23 个 C# 文件 / 9,488 物理行，但 Author SDK 的 `sourceDirectory=src` 会把它排除在玩家 DLL 外。它是维护成本，不是玩家运行时压力；冻结 `IFishingAutomationApi` Compatibility Host 同样不是当前新产品的运行时路径，不能混入 AutoFishing 产品体量或优化收益。
8. 默认 `FastAnimations=false`、`CastChargeRatio=0` 时，F6 session 仍无条件构造完整 animation controller/cache、多个 HashSet/Dictionary 和候选数组；这不是帧级泄漏，却是可通过 lazy capability 明确消除的启用期/常驻小对象压力。

## 1. 审计边界与当前权威

AutoFishing 当前唯一合法身份是 Catalog 准入的 Advanced CodeMod / ProductNative：状态机、Hook、cache、输入/动画覆盖和 cleanup 都属于产品。轻量化不能把单产品责任迁回 GameBridge，也不能复活已退役的 first-party fishing primitives。只有至少两个独立真实消费者共享同一 native owner、冲突点或全局生命周期不变量，才有 SharedNative 提炼理由。

当前还必须区分三条互不替代的轴：

- Catalog 记录产品为 `PublicWorkshop / ProtectedCurrent / RebuildBlocked`，当前源码 policy 是 `doloctown-24456188-autofishing-v1`；本审计不产生重建或上传授权。
- Catalog 的已发布 artifact 是旧的 exact publication fact；本机 subscription manifest 只证明观察到的订阅/cache membership，不证明当前源码已发布、启用、加载或行为正确。
- 当前实现/行为 owner 仍是 [20260802-0001 roadmap Update](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)，而不是旧 Batch 5 receipt 或历史包字节。

本 Review 使用当前干净的 AutoFishing `src`/`qa` 树；工作树中存在其他用户任务的未提交修改，但 `git diff` 与 `git status` 均确认 AutoFishing 目录在本轮开始时没有已有修改。本轮不读取或改写那些不相关内容。

## 2. 体量与压力口径

| 范围 | C# 文件 | 物理行 | 非空行 | 是否进入玩家产品 DLL | 解读 |
| --- | ---: | ---: | ---: | --- | --- |
| `products/first-party/AutoFishing/src` | 25 | 6,054 | 5,591 | 是 | 当前完整产品实现 |
| `src/Native` | 21 | 5,277 | 4,879 | 是 | 生产源码的 87.2%；主要减重区 |
| `qa` | 23 | 9,488 | 8,828 | 否 | 验收维护面，不是普通玩家压力 |
| 2026-08-02 生产基线 | 23 | 5,647 | 5,202 | 当时是 | 历史可比口径；当前已增加 407 行 |

行数只能表示维护表面积。它不能直接换算 CPU、分配字节、GC 暂停或崩溃概率。当前 Unity Mono 的 allocation counter 已被 ISSUE-010 证明不工作，历史 `AllocatedBytes=0` 不能用来反驳本轮 IL 证据；`.NET 8` 微测试的零分配结果也不能自动扩大为 Unity Mono 全 Harmony 路径证明。

已有 GC 证据应准确解释为：

- 30 分钟 inactive AutoFishing 观察已清除“关闭态持续保留增长”嫌疑，但不是 active 热路径预算。
- 历史第五存档 L0-L5 有界阶梯通过了 6 个 600 秒阶段、无 forced GC 的结构/生命周期趋势门；后续 current-candidate 阶段也证明 owner roots、input、event/API/demand/runtime records 在短窗口内有界。
- 这些证据没有建立“每次小游戏 Update 分配多少字节”“一次 RefreshFrame 花多少微秒”或“InstantBite 100 次 roll 的尾延迟”。ISSUE-010 仍是 open，不能写成 GC 已解决。

## 3. 确定债务：热回调闭包和逐帧委托分配

### 3.1 `Safe(string, Action)` 把异常隔离变成了热分配

[`FishingProductCallbacks.cs`](../../../../products/first-party/AutoFishing/src/Native/FishingProductCallbacks.cs) 的 Ready/Wait/MiniGame/Pull 等回调使用捕获 lambda 调用 `Safe(string, Action)`。源代码的 `active == null` 检查看似在 lambda 之前，但 C# 编译器必须先建立承载 `active`/`__instance` 的 display-class。

对仓库内、编译时间晚于当前 AutoFishing 源码最后修改时间的产品 DLL 做只读 IL 解码，得到：

| 方法 | `newobj` | `ldftn` | 直接含义 |
| --- | ---: | ---: | --- |
| `FishingReadyPlayPostfix` | 2 | 1 | display-class + `Action` delegate |
| `FishingWaitPlayPostfix` | 2 | 1 | display-class + `Action` delegate |
| `FishingMiniGameUpdatePrefix` | 2 | 1 | display-class + `Action` delegate |
| `FishingMiniGameUpdatePostfix` | 2 | 1 | display-class + `Action` delegate |

这四个方法的第一个 `newobj` 都位于 `IL_0000`，早于 `CallbackRuntime` inactive guard。完整扫描发现 14 个 `Safe(...)` callback 方法都有同类 display-class 构造；`Wait.OnEnter` 和 `Pull.OnExit` 还各自建立两条 delegate。

因此：

- F6 关闭时，遇到这些原生方法仍至少创建 display-class，之后才发现没有 active runtime；`AgentStateBase.OnExit` 还是一个比 fishing-only callback 更宽的状态退出目标。
- F6 开启时，每条这类回调通常再创建一个 `Action` delegate。
- 现有“关闭只剩 fast-return callback”仍然成立于生命周期/行为意义，但不能再理解为 allocation-free callback。

### 3.2 MiniGame 决策链每次调用还会新建 3 个 delegate

活跃小游戏的 Prefix 进入 `FishingPrimitivesService.TryDecideMiniGameInput` 后，每次把实例方法组 `nativeAdapter.TryBuildMiniGameFrame` 转成 delegate；`FishingRuntimeSession.TryDecideMiniGameInput` 又每次构造 publish-success 和 publish-fault 两个捕获 `this` 的 delegate。

IL 结果：

| 方法 | `newobj` | `ldftn` |
| --- | ---: | ---: |
| `FishingPrimitivesService.TryDecideMiniGameInput` | 1 | 1 |
| `FishingRuntimeSession.TryDecideMiniGameInput` | 2 | 2 |

把 MiniGame Prefix 的 2 个对象、上述 3 个 delegate 和 Postfix 的 2 个对象相加，当前一次 `FishingGameScrollBar.UpdateGame` 自动化链至少执行 **7 条对象构造指令**。这是 IL 级每调用事实，不是分配字节或帧率实测。

### 3.3 轻量化方向

保持每个 Harmony callback 的异常隔离，但移除捕获 lambda：

- 可使用显式 `try/catch`；或
- 使用非捕获的 typed dispatcher/枚举 switch，让 callback 只传 `active`、`__instance`、`__result`，并在 guard 之后调用；或
- 在 session 构造时缓存 frame reader、publish-success 和 publish-fault delegate，生命周期结束时随 session 一起释放。

实现验收应直接检查编译 IL：Ready/Wait/MiniGame 热 callback 与 MiniGame 决策链不得再有按调用构造的 display-class/delegate，同时保留 callback fault isolation、input fault-close 与每个 Bonus note 一次 edge。不要用 `.NET 8 GC.GetAllocatedBytesForCurrentThread=0` 代替 Unity Mono/实际产品路径证据。

**优先级：P0。** 这是本轮唯一同时具备确定 hot frequency、确定对象构造和低语义改动面的项目。

## 4. 强候选：按 phase 拆分每帧 native snapshot

[`ModEntry.OnUpdateTicked`](../../../../products/first-party/AutoFishing/src/ModEntry.cs) 在 active session 每个 DTMAPI update 调用 `primitives.RefreshNativeState()`。随后 [`FishingNativeStateCache.RefreshFrame`](../../../../products/first-party/AutoFishing/src/Native/FishingNativeStateCache.cs) 每次都会：

1. 读取 Agent 与当前选中物品并判定 FishingRod；
2. 读取 Agent StateManager、status 和 current state；
3. 读取 `MoveModifier.inputMultiplier`、`VelocityX`、`MoveModifier.OffsetX`；
4. 读取全局 normal-state 与 Agent `SupportsUseItem`；
5. 构造完整 `FishingNativeAvailability`，再由 session reconciliation 和 decision engine 消费。

这些 getter 已经是缓存的 fast accessor delegate，并非每帧重新用 `MethodInfo.Invoke`；问题是 **所有 phase 都做同一组读取**。

历史 exact-candidate Batch 6 的 L1-L4 每个约 600 秒阶段记录了 36,402–36,794 次 `nativeFrameRefreshCount`，约 60.7–61.3 次/秒，证明该设计在实际游戏中确实是帧级调用。那组候选后来又发生 movement-parity package 变化，不能冒充当前 exact package 的性能验收；这里只把它用作调用频率数量级证据。

不能删除的逐帧责任：

- movement 三信号必须保留当前 native Wait 顺序和设备无关语义；缺失/非有限值必须 fault-close；
- Ready/Cast/WaitEntered 的 pending-native watchdog 必须仍能发现原生链提前退出；
- input fault 与 F6/title lifecycle 必须先于普通 decision 执行。

可以研究的拆分：

- `MovementSnapshot`：active session 每帧读取，保持当前取消语义；
- `CastPreflightSnapshot`：只在 Idle/PullExited/Interrupted 且 retry 到期、真正准备 cast 时读取 selected rod、normal state、SupportsUseItem、pool；
- `NativeTransitionSnapshot`：只在 Ready/Cast/WaitEntered watchdog 需要时读取 StateManager/current state；
- MiniGame/Pull phase 的常规帧不再为了下一次 cast 提前读取 rod/castability。

这会减少主线程 delegate 调用和无用 native graph traversal，但当前没有 per-phase 调用次数/耗时基线，所以它是强候选，不是已量化收益。实现前应先加 QA-only 计数，不给普通玩家常驻 metrics；验收比较每 phase accessor-call count，而不是只比较总 LOC。

**优先级：P0/P1。** 先完成回调去分配，再以第五存档实际 phase 分布决定拆分幅度。

### 4.1 Wait `OnEnter` 有一条确定无效调用

`FishingWaitEnterPostfix` 先通知 phase，又调用 `ApplyFishingWaitAutomation(..., "AgentStateFishingWait.OnEnter Postfix")`；但被调用方法明确要求 `hookSource` 包含 `OnPlay`，否则立即返回 false。于是每条鱼在 Wait Enter 都白建一条 delegate，并做一次字符串搜索，真正工作仍要等后续 Wait OnPlay。

这条调用可以直接删除；随后再研究 Wait OnPlay 的深读链。当前每个 fixed tick 从 Wait body 进入 StateManager/current、wait flag、FishingCache/FishProto/IsFish。较窄的方案是先读取 wait flag，只有 bite-ready edge 才读取 cache/proto；是否能移除每 tick 的 current-state 复核仍需 current body/provenance 证明。

**优先级：P0（删除确定死调用）/ P1（拆 Wait 读取）。**

## 5. 确定维护债，但运行时收益以 cold/activation 为主

### 5.1 重复的 native binding 与 failure state

`FishingNativeStateCache` 和 `FishingNativeTransactionCache` 分别维护 Wait body、StateManager/current、FishingCache/FishProto/IsFish 的类型与 accessor；adapter/energy/transaction 路径也分别解析 `DolocAPI`、`GlobalParameter` 等 owner。每个 cache 又各自维护 unavailable-type latch、build/rebuild/failure counters、日志门和 invalidation。

这会带来：

- 同一类型的重复 `Expression.Compile`/delegate 构建与 cold allocations；
- game generation 失效时多个 cache 的重复重建状态；
- 同一 member drift 在不同文件产生不同错误文本和 unavailable latch；
- 生产与 QA 必须合并五组 counter 才能解释整体状态。

建议在产品内部建立按 native type/capability 分组的不可变 `FishingNativeBindings`，让 read/write consumer 共享已解析 accessor，而不是建立一个无边界“大反射工具箱”。应保留 required write/read 的差异、失败 provenance 和 generation invalidation；不能把它迁回 GameBridge。

**优先级：P1。** 主要收益是代码量、激活期分配和一致性，不应宣称为每帧 GC 修复。

### 5.2 单产品仍保留迁移期 owner/facade 层

当前同一个产品同时维护：

- `ModEntry.enabled/session/updateSubscribed`；
- `FishingPrimitivesService.activeSession/hookRuntime`；
- `FishingRuntimeSession.IsReleased/IsFaulted/OwnerId`；
- `AutoFishingNativeRuntime.primitiveHookRuntime`；
- `FishingPrimitiveHookRuntime.fishingHooksInstalled`；
- `FishingProductHookInstaller.IsInstalled` 和 static callback runtime。

`StartSession(ownerId)` 还在仲裁 owner，但生产调用者始终是当前产品 manifest owner。这个结构来源于旧跨程序集 primitives 迁移，有些层已不再代表独立物理 owner。

建议将产品控制面收束成一个 `AutoFishingSession` lifecycle owner，并保留独立的 process-lifetime Hook installer。session 直接拥有 phase、sequence、fault-close、input/animation leases、native bindings 和 cleanup；ModEntry 只负责 toggle/config/event bridging。

不能机械合并的部分：

- Hook 安装/卸载事务与 F6 session 不是同一生命周期；F6 off 不应反复 Harmony patch/unpatch；
- pending input fault 的“先关闭原生 override，再由 ModEntry 收口 enabled/updater”是 2026-07-31 已修复的半状态问题；
- owner cleanup、sequence、exactly-once settlement 和 rollback ordering 必须有等价测试后才能删层。

**优先级：P2。** 它可以显著降低维护表面积，但比 P0 热路径更容易制造生命周期回归。

### 5.3 两层调度状态职责相近但命名边界模糊

`ModEntry.nextCastAtUtc` 控制产品 policy 的下一次尝试；`FishingAutoCastScheduler` 控制已经调用 native cast 后的 3 秒确认 timeout 与 2.5 秒 backoff。两者不是完全重复，但都在回答“下一次何时允许动作”，且分别跨 ModEntry/service/session 更新。

建议合并为 session-owned scheduler，明确两个字段：`NextAttemptAt` 与 `PendingNativeConfirmationDeadline`。保留 Pull 后 0.25 秒、无杆/无池/失败分类 retry、3 秒 pending timeout 和 native transition reconciliation。收益主要是减少状态漂移和分支，不是已证明的 CPU 优化。

**优先级：P2。** 与 lifecycle shell 收束一起做，不能单独改 timing 常量。

### 5.4 默认配置仍预构造完整 animation 子系统

默认 `FastAnimations=false`、`CastChargeRatio=0`；只有 FastAnimations 开启时 session 才获得 animation lease。但每次 F6 建立 `FishingPrimitiveHookRuntime` 时仍无条件构造 animation controller/cache、两个 Ready HashSet、四个恢复 Dictionary；`FishingAnimationNativeCache` 又预构造多组 type/member Dictionary 和 `object?[16]` candidate buffer，并在构造期解析 Unity 类型。

建议按首次真实 capability 使用 lazy-create：

- charge ratio 大于 0 或 Ready multiplier 大于 1 时才创建 Ready charge/animation state；
- FastAnimations lease 存在时才创建 animator/hook/pull restore cache；
- cleanup 对尚未创建与已创建两种状态都保持幂等；
- F6 off/title/owner unload 仍必须恢复任何已经写过的原值。

这能减少默认启用时的一批集合/数组分配和常驻小对象，但不是 steady-state frame allocation 修复。不要为了 lazy 化合并不同 restore owner。

**优先级：P1，低到中风险。**

### 5.5 QA observation 不应塑造生产 hot DTO

`FishingPrimitiveSnapshot` 携带 12 个字段；生产决策实际主要消费 sequence、phase 与 movement，而多个 availability/result 字段在 `src` 中投影后只供 QA 反射/观察。类似地，一些 accessor/frame counters 即使 QA observation 没开启也持续递增。

可拆成最小 runtime frame 与按需 QA snapshot；只有 `EnableQaObservation()` 后才维护纯观测 counter。readonly struct copy 本身不是 heap allocation，收益主要是减少热路径赋值、生产/QA耦合和维护面，不应夸大为 GC 修复。

**优先级：P2。** 需要同步 reflection observer，不得丢失验收可观测性。

## 6. 小而确定的冗余

以下可在后续实现中先做 reference guard，再删除或降为 QA-only：

| 项目 | 当前事实 | 建议 | 主要收益 |
| --- | --- | --- | --- |
| `ProductNativeHelpers.TryWriteAnimatorSpeed` | 只有定义；生产调用使用 `FishingAnimationNativeCache.TryWriteAnimatorSpeed` | 删除 | 死代码/认知负担 |
| `ProductNativeHelpers.ClampMultiplier` | 只有定义；runtime/service 各有自己的实际实现 | 删除，随后决定是否统一两个活实现 | 死代码；避免第三个规则源 |
| `FishingNativeMovementPolicy.ShouldCancel(...)` | 只被 UnitTests 直接调用，生产使用 `Evaluate(...)` | 测试改为断言 `Evaluate` 后删除 wrapper | 生产 API 面 |
| `FishingProductContext` | 只包装 `IDtmHelper`，生产实际只消费 `RuntimeMonitor`；`Helper` 无其他直接读取 | 传入 `IMonitor` 或更窄产品服务 | 一个对象、间接层、16 行 |
| `FishingPrimitiveHookRuntime.readyTargets` | 生产只 `Add/Clear`，不参与行为；QA 反射读取 Count | QA observation 开启时才跟踪，或改 QA-only scalar | 一次 HashSet/capacity 和 retained root |
| `FishingPrimitiveHookRuntime.readyReleased` | 只表达当前 Ready state 是否已经释放，但使用 HashSet | 以 `currentReadyState` 配对一个 released state/scalar；先证明不会交错多 Ready owner | 集合与 Unity object equality 表面积 |
| `FishingAutoCastScheduler.NextDueAtUtc` | 只有属性定义，生产/QA没有读取 | 删除 getter；调度器内部字段继续保留 | 死 API 面 |
| `FishingAnimationController.TryAdvanceReadyCharge` 的四个 out 值 | 生产调用全部丢弃，主要服务测试/诊断 | 将行为结果收束成小 struct/enum，QA 另取细节 | 签名与调用噪声 |
| `FishingRuntimeSession.Release` | try 和 finally 重复清空 input/animation，且正常赋值后 `if (!IsReleased)` fallback 实际不可达 | 让 finally 成为唯一幂等 cleanup owner，保留 subscriber exception isolation | 重复生命周期代码 |

这些项总收益有限，不能替代 P0。删除 `readyTargets` 时必须同步 product QA reflection observer，且保留 `readyReleased`；后者参与每个 Ready state 只释放一次的实际语义。

## 7. 模式特定压力：InstantBite 同步 roll 上限

`FishingNativeTransactionCache.TryPrepareNativeBite` 在 `getWaitForBite` 为 true 时，于 Unity 主线程同步循环最多 100 次 `rollFish(waitState)`，直到原生返回成功。之后才执行 ordered field commit 与 post-commit observation。

确定事实：

- 单次 InstantBite 最坏会连续进入原生 RNG、FishingCache 和 ItemFactory 100 次；
- 该路径低频且只在玩家启用 InstantBite 时发生，不是默认循环或每帧成本；
- 当前没有记录每次成功所需 attempts、elapsed 或 95/99 分位，也没有证明游戏 build 24456188 的实际上限压力。

不可直接采用的“优化”：

- 把 `RollFish` 搬到后台线程：native owner 与 Unity/game state 不是线程安全契约；
- 只恢复四个 Wait 字段：无法恢复已消耗 RNG、FishingCache、ItemFactory 创建与潜在副作用；
- 任意降低 100 上限：可能改变现有 InstantBite 成功语义；
- 成功 roll 后跨帧完成字段 commit：会扩大原生 Wait 与产品之间的不确定窗口。

先通过 QA-only 记录 attempts/elapsed，不进入普通玩家日志。若真实尾延迟不可接受，再在同一主线程设计“roll 前可中断、成功后同帧原子收口”的 bounded-step state，并重新做 AF-D2 fault injection 与第五存档行为矩阵。

**优先级：P2 / evidence-gated。** 它可能造成尖峰，但当前不能宣称已经造成 GC 或帧卡顿。

### 7.1 每鱼 transaction 仍有 8 个捕获 delegate 与未消费成功文本

成功 roll 后，生产唯一调用点向 `FishingBitePreparationTransaction.Commit` 传入 8 个围绕 `waitState/body` 的捕获 lambda。随后 transaction/cache/service 还构造 stage、provenance 和成功 message，而上层成功路径通常只保留 `result.Status`。FastAnimations 或缩短等待后，这不是一次性 cold cost，而是随每条鱼重复。

可把生产路径改为接收一个 bound transaction context/已缓存 accessors，直接执行 ordered commit；保留 injectable fault-test overload，或让测试 context 实现同一窄接口。成功结果使用 enum/常量，只有 QA 请求、状态变化或失败时格式化 detail。

必须保留的语义不是 186 行 delegate facade，而是：每个 write checkpoint、commit-bit last、after-write verification、post-commit observation 不反转成功，以及不确定时 fault-close。

**优先级：P1/P2。** 分配是确定的但频率低于每帧 callback，先完成 P0。

### 7.2 字符串和日志应按状态变化格式化

还有三类低到中频压力候选：

- movement 未 neutral-arm 时，每帧用 `ToString("R")` 和拼接生成 reason；应先返回 reason code + 原始数值，只在状态变化/提示/日志时格式化；
- 低能量 cast reject 默认约 0.25 秒重试，上层不记录详细 message，却在 adapter 内反复构造能量文本；可使用 typed status，并单独评估 1 秒 energy backoff 对恢复后首 cast 延迟的影响；
- 高频 callback 若持续抛异常，当前每次都拼字符串并写日志，可能形成 50–60+ 行/秒的二次压力；应保留首因，随后计数/节流摘要或首错 fault-close，不能静默吞错。

这些都应以状态变化和故障注入验证，不要把正常成功路径改成常驻 verbose diagnostics。

### 7.3 默认 charge=0 仍有可避免的 Ready 读取

Ready input override 当前先反射读取 progress，再根据 target=0 决定释放；默认 charge ratio 正是 0。可以先检查 target：0 时直接生成一次 release edge；只有 target>0 才读 progress。Ready state 未变时也无需反复向集合 Add。该调整很小，但必须覆盖 ratio 0 / 0.5 / 1 与 FastAnimations 各档，防止提前释放破坏 native backswing gate。

## 8. Hook 与 animation cache：可继续研究，但当前不授权拆除

当前 22-patch inventory 包含 6 个 input getter、Ready/Wait/MiniGame/Pull、renderer 和 `AgentStateBase.OnExit`。2026-08-02 Review 曾建议 required/optional capability 分组和重新推导最小 getter；2026-08-04 更晚、更具体的 native-body Review 则明确：当时没有证据证明子集在其他目标失败时仍安全，继续保持一个 22-patch 原子事务。

因此本轮只能记录后续研究题：

1. 对 current reverse build 的每个 fishing body/callsite 重做 getter consumer trace，尤其验证 `NormalUseItemInProgress` 是否存在真实 consumer；
2. 量化 disabled mode 下六个 getter detour 的调用次数和冲突面；
3. 如果要形成 Baseline/InstantBite/Skip/FastAnimation capability group，必须先定义每组完整 required set、失效 UI 和 exact-owner rollback，不能运行时按 F6 频繁 patch/unpatch；
4. `FishingAnimationNativeCache` 的 alias/candidate graph/dynamic type dictionaries 主要是维护与 phase-entry cold cost。当前玩家已验证跨 build 正常路径，不能仅为减行改成脆弱的 direct MemberRef；应先用 QA telemetry 证明长期未命中的 alias，再生成/收窄 binding。
5. `HasFishingPool` 当前按 environment generation 做一次全场景 `FindObjectsOfType(Type)` 扫描；它只证明场景存在某个 pool，不证明玩家当前落点可钓，且生产 decision 最终仍以 native cast/phase 为权威。可研究删除该全局数组扫描或换成 room-scoped signal，但必须保留 no-water retry/提示语义。

在新的 body/callsite Review 之前，**保留 22-Hook 原子事务**。这不是说 22 永远最小，而是说当前审计没有足够证据安全改变它。

## 9. 玩家反馈债与性能债分开

2026-08-22 玩家“F6 无明显反应”复核证明 Hook/F6/session 正常；主要原因是短时间二次 F6、移动自动关闭，以及没有屏幕反馈。后续按“站定、只按一次 F6”复测成功。

这是 UX 债，不是已复现的 Hook/input 故障。若后续实现，使用状态变化触发、带节流的 enable/disable/movement/no-rod/no-water 提示；不得把 phase/reason 每帧写入 HUD 或日志，否则会用新的 UI/字符串压力换取可见性。

## 10. 明确保留、排除和不得复活的部分

### Keep：当前可靠性不变量

- 22-patch exact-owner 原子安装、失败全 owner rollback；
- phase sequence、重复 decision/settlement 防护和 Bonus note 单 edge；
- scoped mini-game/reel input、超时、清理与 fault-close；
- Ready/Hook/Pull 原始动画/物理值恢复；
- movement 的 inputMultiplier / pre-base VelocityX / post-base OffsetX 顺序与 fail-close；
- InstantBite `RollFish -> probability -> duration -> hasRolled -> wait=false(last)` 和 uncertain commit 关闭自动化；
- F6、SaveLoaded、ReturnedToTitle、owner unload、clean exit 的 updater/session/native-root 清理；
- 当前产品 ProductNative 所有权与 Author SDK exact policy/package gates。

### Exclude：不计作普通玩家 AutoFishing 压力

- `qa` 的 reflection observer、L0-L5 driver、recovery/reload handshake 和 exact patch-count proof；它们不进入玩家 DLL；
- frozen `IFishingAutomationApi` compatibility host；当前 Advanced 产品不消费它，但旧已发布 DLL 仍是已知二进制消费者；
- DTMAPI Core/Loader/Manager/Doctor/Author SDK 与 BepInEx/Harmony 本身；
- 历史 package bytes、receipt 和 subscription cache membership。

### Already retired：不得以“轻量复用”名义复活

- `IFirstPartyFishingPrimitivesApi` / friend seam / old GameBridge fishing feature；
- Legacy/Shadow 分支、无消费者 helper/facade、常驻 QA observation；
- A/D/Space/Shift synthetic movement fallback；
- 旧 synthetic fifth-save setup、D/menu preflight 与历史 no-demand fake-zero metrics；
- 第三方 DolocPlus/小神实现代码。它只能是语义与数量级参考，不能复制或合并。

## 11. 建议的轻量目标结构

```text
ModEntry: toggle / config / DTMAPI events
    |
    v
AutoFishingSession: phase + sequence + scheduler + fault-close + leases
    |                                  ^
    v                                  |
FishingNativeBindings -------- non-alloc callback dispatcher
    |
    +-- movement snapshot (per active frame)
    +-- cast preflight (when retry is due)
    +-- transition watchdog (only relevant phases)
    +-- bite/reel/minigame transactions

Process-lifetime HookInstaller remains separate and atomic.
```

这不是要求一次大重写。建议按可独立回滚的顺序推进：

1. **Slice A / P0：** 移除 callback 与 MiniGame 每调用 delegate 分配；不改变 Hook inventory 或行为。
2. **Slice B / P1：** lazy-create 默认不用的 animation capability，并删除确定死调用/死成员。
3. **Slice C / P0-P1：** 加 QA-only per-phase accessor call counter，拆 movement/cast/watchdog snapshot；保持第五存档行为完全一致。
4. **Slice D / P1：** 合并重复 bindings/unavailable latch/counter；先保持调用者与签名不变。
5. **Slice E / P2：** 收束 session/facade/scheduler、QA-only DTO 与小冗余；以 lifecycle/fault matrix 防回归。
6. **Slice F / P2：** 只有测到 InstantBite 尾延迟或取得新 native body 证据后，才改变 roll 策略或 Hook group。

每个实现 slice 都应创建自己的 Update；本 audit-only 任务只拥有这一份 Review。

## 12. 后续实现的最小验证矩阵

### Source/IL focused

1. 当前 `autofishing-product` UnitTests 继续通过，包括 movement、AF-D2 fault injection、Bonus edge 和 exact 22 Hook count。
2. 编译 IL 证明 inactive callback guard 前无 display-class `newobj`；活跃 MiniGame Update 路径不再逐调用创建 frame/publish delegates。
3. dead-symbol/reference guard 证明删除项没有生产或 QA contract 消费者。
4. binding consolidation 的 generation invalidation、build/rebuild/failure provenance 与当前 counter 语义等价。

### Fifth-save game acceptance

1. 默认循环、InstantBite、SkipMiniGame、FastAnimations 及独立组合保持原生结果路线。
2. Ready/Wait/MiniGame/Pull 各 phase 的 F6 off、movement cancel、title/reload 后无 input/animation/native root 残留。
3. movement 键盘、改键/手柄及 native VelocityX/OffsetX parity 不退化。
4. fault-close、pending cast timeout、0.25/1/2.5 秒 retry 语义不变。
5. 日志、Hook owner、第五存档加载和无残留 `DolocTown.exe` 证据完整。

### Runtime-pressure evidence

1. 不再使用无效的 Unity allocation counter 或 `.NET 8` microtest 冒充产品证明。
2. 先比较 per-phase callback/accessor invocation counts；若能取得可信 Unity Mono profiler，再报告 bytes/time。
3. InstantBite 只在 QA observation 下记录 attempts/elapsed，普通玩家不常驻采集。
4. 新的 active bounded GC 阶段只能说明测试窗口内趋势，不能自动关闭 ISSUE-010 的更广长时 Mono 问题。

## 13. 本轮验证与限制

- 逐文件审查当前 25 个生产 C# 文件，并重新统计 `src`、`src/Native`、`qa` 体量。
- 只读检查当前产品 DLL 的 metadata/IL，确认 callback display-class、delegate method group 和 MiniGame 决策链的 `newobj`/`ldftn` 数量。
- `dtmapi-author validate products/first-party/AutoFishing --json`: **PASS**，Advanced/Product/manifest authority 有效。
- `DTMAPI_UNIT_TEST_FOCUS=autofishing-product`、仓库本地 .NET 8 toolchain：**PASS**。构建打印了不相关、已有工作树 DebugConsole nullable warnings；AutoFishing focused test 通过。
- 尝试从当前审计工作树执行新的 Author SDK product build时，tracked resolver 因没有显式 `DTMAPI_GAME_DIR`/`local.settings.json` 而 fail-closed；没有猜测或硬编码 Steam 路径。既有产品 DLL 编译时间晚于当前 AutoFishing 源码，足以用于本轮 IL 静态确认，但不是新 package/release receipt。
- 未运行完整 Unit/Release suite，未安装 Runtime/产品，未取得 runtime lock，未启动 Doloc Town，未访问第五存档，也未作新的 GC/帧时实测。原因是本轮为 audit-only，且没有 runtime mutation 或游戏验收需求。

## 14. 相关权威与前序记录

- [2026-08-02 AutoFishing 语义、体量与功能基线](../../../archive/reviews/code/2026/20260802-0001-autofishing-qiuzy-semantic-size-and-feature-baseline.md)
- [2026-08-04 current native body 与 AF-D2 Review](../../../archive/reviews/code/2026/20260804-0008-autofishing-100-native-body-and-af-d2-review.md)
- [2026-07-21 产品重量与配置复用 Review](../../../archive/reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md)
- [2026-07-21 Product Slimming Update](../../../archive/updates/2026/20260721-0004-autofishing-product-slimming.md)
- [2026-08-02 到 08-05 current implementation owner](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- [2026-08-22 玩家 F6 无可见反馈复核](../../../archive/reviews/manual-qa/2026/20260822-0001-autofishing-player-f6-no-visible-response.md)
- [ISSUE-010 长时 Mono GC](../../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)
- [Managed Product Admission Registry](../../../architecture/managed-product-admission-registry.md)
- [Public API Matrix](../../../api/public-api-matrix.md)
- [AutoFishing README](../../../../products/first-party/AutoFishing/README.md)

本 Review 是 2026-08-31 当前源码的技术债与运行时压力基线。它没有授权改变 22-Hook 原子边界、冻结 ABI、发布状态或 ProductNative 所有权；后续实现若得到相反的 native body、Profiler 或第五存档证据，应新增 Review/Update 并链接本记录，不应覆盖本轮静态事实。
