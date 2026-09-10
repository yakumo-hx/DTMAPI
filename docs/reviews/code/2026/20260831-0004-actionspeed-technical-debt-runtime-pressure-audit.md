# ActionSpeed 技术债、冗余、过度设计与运行时压力审计

## 记录信息

- Review ID：20260831-0004
- 日期：2026-08-31
- 状态：recorded
- 性质：audit-only 代码 Review；不承接实现生命周期
- Source：用户要求审计基于 DTMAPI 的第一方动画加速 Mod 的技术债、冗余、过度设计，并研究可轻量化代码与可降低的运行时压力
- 基线：分支 codex/major-update-batch0-20260713，HEAD 98d75c6c51300768e3d7a8b209c858c9ad110493
- Implementation owner：尚未建立。后续整改必须新建对应 Update，并按本 Review 的风险边界选择 focused validation

审计开始时工作树已有大量与本任务无关的未提交修改，主要位于 DebugConsole、文档和发布脚本。
ActionSpeed 产品源码、冻结 ActionSpeed Compatibility 执行器和 mandatory 薄代理本身没有现有工作树修改。
本 Review 不覆盖、不清理、不接管这些既有改动。

本轮只读审计产品源码、冻结兼容宿主、Hook/native authority、QA fixture、历史 GC 证据和相关文档。
除本 Review 外，不修改 Runtime、产品、Compatibility、API matrix、测试、Workshop、Official MODS 或存档；
不启动 Doloc Town，不获取 runtime lock，也不把静态推断写成已测量的毫秒、字节或帧率收益。

## 1. 执行结论

ActionSpeed 当前最需要减轻的不是 11 个 Harmony Hook 的数量，而是两个逐帧 Continues Prefix 和
AutoFill updater 命中后的工作量：

1. 主开关关闭或对应功能关闭时，产品仍可能先完成 DolocAPI、物品、装备、当前交互物和接口分类，
   到分类末端才发现 policy 不生效；
2. AutoFill 开启后，产品在标题页仍保持 UpdateTicked 订阅；只有探测已经走到原生 UseAsItem 调用前
   才推进 cooldown，普通标题、陆地、错误物品等不合格状态会在每帧重复反射；
3. 连续使用匹配后，每帧都会构造 target/nativeOwner/logKey 字符串；Debug Profile 还会每帧写日志；
4. 反射元数据、接口数组、params 候选数组、Animator speed PropertyInfo 和 restore 临时 List 都没有复用。

没有发现产品内的 lock、FindObject(s)、GetComponent、全场景扫描或每帧动态 Hook 安装。当前主要压力是
高频小分配、重复反射和日志，而不是线程竞争或 Unity scene traversal。

最小高收益路线是：

1. 保留 11 Hook、原子安装、精确 owner 清理和配置快照；
2. 用单一不可变 runtime snapshot 加功能位掩码，在任何 native 反射前快速返回；
3. 让 AutoFill 只在已加载存档的 gameplay context 订阅，并在每次探测后用单调时钟推进下一探测时间；
4. 缓存反射元数据，分类先返回小型 enum/倍率，只有首次日志或显式 QA 观察时才格式化描述；
5. 去掉 restore 副本、callback 闭包、逐帧 Debug/异常日志和确认无调用者的配置/测试脚手架；
6. 补直接针对 ProductNative Engine 的测试和真实持续按住输入的短窗口验证。

冻结 IActionSpeedApi 执行器与当前产品高度重复，但它已经位于首次真实旧 ABI 调用才加载的可选
Compatibility Host。它是维护和包体债，不是普通 ProductNative 玩家路径上的第二份默认执行成本。
现有旧消费者仍要求 KEEP / NO-GO；不得为了去重把产品实现重新抽回 mandatory GameBridge，也不得把
兼容宿主改为依赖当前产品 DLL。

## 2. 当前拓扑与代码重量

### 2.1 编译/加载边界

当前路径为：

1. ModEntry 创建 ActionSpeedNativeRuntime；
2. ActionSpeedHookInstaller 预解析并原子安装 11 个 ProductNative Hook；
3. ActionSpeedCallbacks 通过一个静态 runtime 根分发 Harmony callback；
4. ActionSpeedEngine 持有配置快照、分类、反射、Animator 原速度、AutoFill 和可选 QA 观察；
5. 旧 IActionSpeedApi 调用经 mandatory ActionSpeedServiceProxy 首次触发可选 Compatibility Host；
6. 当前 Advanced 产品不消费 IActionSpeedApi。

代码规模按当前工作树物理行和非空行统计：

| 边界 | 文件数 | 物理行 | 非空行 | 普通产品进程是否默认加载 |
| --- | ---: | ---: | ---: | --- |
| 当前 ActionSpeed 产品生产源码 | 6 | 1,834 | 1,620 | 是，产品加载时 |
| 其中 ActionSpeedEngine.cs | 1 | 1,201 | 1,039 | 是 |
| 冻结 ActionSpeed Compatibility domain | 3 | 1,486 | 1,268 | 否，首次旧 ABI 调用后 |
| 其中冻结 ActionSpeedService.cs | 1 | 1,316 | 1,136 | 否 |
| mandatory ActionSpeed 薄代理 | 1 | 89 | 81 | 是，但后端未请求时保持薄代理 |

产品 Engine 与冻结 service 的 trimmed non-empty multiset line overlap 为 896/1,039，约 86.24%；
只统计长度至少 20 个字符的归一化行时为 446/564，约 79.08%。该数值只说明当前仓库存在明显的
双实现维护面，不是语义等价证明，也不是二进制大小、内存驻留或性能测量。

[CompatibilityHostBroker.cs](../../../../src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/CompatibilityHostBroker.cs)
明确把重型宿主保持为 dormant-shipped；
[ActionSpeedServiceProxy.cs:8-34](../../../../src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/ActionSpeedServiceProxy.cs#L8)
是 mandatory 薄代理。当前 0.6 consumer/removal 审计仍记录一个具有 22 个冻结家族 MemberRef 的已知旧
ActionSpeed 消费者，并判定 KEEP / NO-GO：
[20260804-0019:92-126](../../../archive/reviews/code/2026/20260804-0019-dtmapi-060-nine-family-consumer-and-removal-review.md#L92)。

### 2.2 调用频率

| 路径 | 频率类别 | 当前主要工作 |
| --- | --- | --- |
| Tool/Water/Interact/Eat Enter/Exit | 每动作或状态切换 | 反射读取 state/body/Animator、记录/恢复速度、日志 |
| UseItemContinues Prefix | 按住使用期间每 native Update | 完整 use-item 分类、dt 缩放、target/log/QA |
| InteractContinues Prefix | 按住交互期间每 native Update | 完整 interaction 分类、dt 缩放、target/log/QA |
| AnimalRenderer.OnInteract Prefix | 每次动物交互 | 分类并保留 pending marker |
| AutoFill UpdateTicked | 配置开启后的每个 DTMAPI Update | policy/cooldown、DolocAPI 静态状态、物品、反射 Invoke |
| Configure/SaveLoaded/ReturnedToTitle/Dispose | 冷路径 | restore、清状态、订阅或 owner cleanup |

当前 public build 的反编译 authority 证明 AgentControllerState 的 UseItemContinues 与
InteractContinues 在按住输入期间由原生 Update 循环持续调用。因此两个 Prefix 是真实热路径，不能按
“11 个 Hook 中的两个普通方法”估计成本。

## 3. 发现总表

| ID | 优先级 | 类型 | 结论 |
| --- | --- | --- | --- |
| AS-01 | P1 | 已证实热路径缺陷 | master/feature disabled 仍可能完成反射分类，缺少真正的 no-demand fast path |
| AS-02 | P1 | 已证实闲置压力 | AutoFill 在标题页仍订阅，且不合格状态在 cooldown 过期后逐帧探测 |
| AS-03 | P1 | 测量有效性缺口 | Debug/QA 在热路制造日志和摘要；历史 ladder 的连续调用密度远低于真实按住输入 |
| AS-04 | P1 | 高可信清理风险 | Animator restore 吞写入失败后仍清空原速度表，零 transient 不等于已恢复 |
| AS-05 | P2 | 过度设计 | 单产品仍保留多-owner dictionary、委托 selector、倍率竞争和 owner tie-break |
| AS-06 | P2 | 反射/分配债 | MemberInfo、接口、params 数组、target/log 字符串和 restore List 重复创建 |
| AS-07 | P2 | 诊断/回调债 | 捕获 lambda、无界 target 字符串 key、Debug/失败日志和 QA summary 放大热路 |
| AS-08 | P2 | 测试/Hook 债 | 产品 Engine 无直接 Unit；Hook selector 只核对方法名和参数个数 |
| AS-09 | P3 | 死代码/配置/文档漂移 | 已发货无行为字段、无调用 seam、未使用参数和九/十一 Hook 文档分叉 |
| AS-10 | 分离处理 | 冻结兼容债 | 重型旧执行器可节流但不可删除、共享或计入默认产品热路 |
| AS-11 | P3 | 工具链债 | 产品 focused gate 混入跨域源码 token；Advanced build/pack 存在重复编译 |

## 4. P1 热路径与正确性问题

### AS-01：关闭不等于低成本

ActionSpeed 当前只有一个硬编码 ProductOwnerId，但 Configure 无论 Enabled 是否为 false 都会向
actionSpeedOptions 写入一条 policy：
[ActionSpeedEngine.cs:44-55](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L44)。

UseItemContinues 与 InteractContinues 只检查 dt 和字典 Count：
[ActionSpeedEngine.cs:147-166](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L147)。
因为 Count 始终为 1，它们随后读取 DolocAPI、SelectedItem、SelectedEquipment、当前交互物和水中状态；
真正的 candidate.Enabled/feature/multiplier 检查位于分类分支后半：
[ActionSpeedEngine.cs:268-379](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L268)。

分支顺序还先执行 IsBottleFillInteraction、IsPlantInteraction、IsMachineInteraction 或
IsHarvestInteraction，再调用 policy selector。即使某一类别开关关闭，其分类成本也已经发生。

MarkNativeAnimalInteract 更早：它没有检查 master、MachineAddSpeedEnabled 或有效倍率，就可能保留
AnimalRenderer 与时间戳：
[ActionSpeedEngine.cs:123-130](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L123)。

这不是要求动态卸载 Hook。最小方案是在 Configure 时生成一个不可变 runtime snapshot 和功能位：

- Enabled 且 ToolMultiplier 有效；
- interaction 各子域 demand；
- UseItemContinues demand；
- animal marker demand；
- AutoFill demand；
- logging/QA demand。

每个 callback 必须在读取任何 native member 前检查自己对应的位。单 snapshot 仍保留“ConfigMenu 草稿
尚未保存时不改变运行行为”的必要语义；不能让 Engine 直接引用正在编辑的 ActionSpeedConfig。

### AS-02：AutoFill 标题页与不合格状态逐帧探测

ModEntry 在 Entry 时只按 config.Enabled && config.AutoFillBottle 订阅 UpdateTicked：
[ModEntry.cs:22-45](../../../../products/first-party/ActionSpeed/src/ModEntry.cs#L22)。
ReturnedToTitle 只调用 ResetBoundary，没有退订：
[ModEntry.cs:138-158](../../../../products/first-party/ActionSpeed/src/ModEntry.cs#L138)。

UpdateActionSpeedAutoFill 每次先选 policy、读取墙上时钟；cooldown 过期后解析 DolocAPI 并反射读取
IsNormalState、IsAgentInWater、SelectedItem、agent 和 UseAsItem：
[ActionSpeedEngine.cs:186-214](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L186)。
lastActionSpeedAutoFillAt 只在已经满足这些条件、即将 Invoke 前更新。因此标题、陆地、错误物品、空状态
或 accessor 漂移等普通不合格结果不会限流下一帧。

在 60 FPS 的说明性模型中，标题页一小时可产生 216,000 次产品 Update 回调。这个数字是调用次数算式，
不是实测帧率或耗时。

最小方案：

1. 增加明确的 gameplay/save context 状态；
2. Entry 和 ReturnedToTitle 保持 AutoFill updater 退订；
3. SaveLoaded 后且配置 demand 为真才订阅；
4. 使用单调时间源维护 nextProbeAt；
5. 每次实际 probe 后都推进 nextProbeAt，而不是仅在走到 Invoke 时推进；
6. 配置关闭、返回标题和 owner cleanup 都清理 subscription/cadence state。

若平台支持在存档已经加载后热加载产品，初始 gameplay context 还必须由权威 Runtime 状态恢复，不能只等
下一次 SaveLoaded 事件；本 Review 没有假定该热加载路线不存在。

按当前常见 0.08–0.25 秒间隔，60 FPS 模型会把 native probe 从最多 60 次/秒限制在约 4–12.5 次/秒，
理论调用降幅约 79%–93%；这只是静态 cadence 上限，实施后必须测 callback/probe 计数，不能写成已获得
CPU 或 GC 百分比。

### AS-03：现有性能证据被低密度 workload 和诊断工作限制

历史正式 ActionSpeed ladder 的每个 stage 为 600 秒、10 个 workload unit。ContinuousUse fixture 每个
unit 最多直接调用 Prefix 5 次：
[ActionSpeedFixtureCase.cs:134-155](../../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/ActionSpeedFixtureCase.cs#L134)、
[ActionSpeedFixtureCase.cs:658-678](../../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/ActionSpeedFixtureCase.cs#L658)。

因此默认上限是 50 次主动 Prefix / 600 秒。真实 60 FPS 下持续按住 600 秒的说明性上限是 36,000 次，
密度约相差 720 倍。历史 24-child 结果仍可证明其确切候选记录的有界结构、生命周期、无强制 GC 和
save/source/exit receipts，但不能证明当前产品在真实连续输入密度下低分配。

更重要的是：

- 该 ladder 是 2026-07-22 Advanced ProductNative 迁移前基线；迁移 Update 明确未重跑完整 L0–L5；
- 2026-08-06 将 Hook 从 9 增到 11 后，Runtime Validation 仍为 not-run：
  [20260806-0002:39-50](../../../updates/2026/20260806-0002-actionspeed-manual-watering-tool-speed.md#L39)；
- 当前 Unity Mono 的 current-thread allocation counter 已证明不可用，必须保持 Blocked/null；
- smoke runner 为 ActionSpeed 固定写入 Profile=Debug：
  [run-game-smoke.ps1:6800-6830](../../../../tools/scripts/run-game-smoke.ps1#L6800)；
- QA observer 第一次 Observe 即调用 EnableQaObservation：
  [Batch6ActionSpeedReflectionObserver.cs:68-88](../../../../products/first-party/ActionSpeed/qa/batch6/Batch6ActionSpeedReflectionObserver.cs#L68)；
- QA 开启后 continuous 每次应用都会重建 LastActionSpeedContinuousUseSummary。

Debug Profile 又令 VerboseLogging=true；continuous 成功路径每帧写一条完整日志：
[ActionSpeedEngine.cs:452-465](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L452)。
当前性能 fixture 因而同时测量产品行为、QA summary 和日志 I/O，无法隔离正常玩家路径。

后续性能验证应使用 Normal/Safe 日志策略，QA 热路只累加原始整数/枚举/最后值；描述字符串在 observer
读取时惰性生成。Debug 也只能按操作首次、时间窗采样或边界汇总，不能逐帧写磁盘。

### AS-04：restore 失败被吞后仍丢弃恢复凭据

RestoreActionSpeed 先复制整个 originalAnimatorSpeeds 字典，然后对每项调用 TryWriteAnimatorSpeed；
无论写入成功与否，最后都会 Clear：
[ActionSpeedEngine.cs:169-184](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L169)。

TryWriteAnimatorSpeed 捕获所有异常并只返回 false：
[ActionSpeedEngine.cs:929-943](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L929)。

因此 QA 看到 AnimatorSnapshotCount=0 只说明字典已清空，不证明所有 live Animator 已恢复原速度。
在 accessor 漂移、反射 setter 失败或暂态异常下，owner deactivation 也收不到异常，无法触发其现有的
restore/unpatch 双失败聚合。

这不是简单地“永远保留失败对象”：Unity 对象已经销毁时，持续强引用会制造另一种生命周期债。实施前
必须分类恢复结果：

- 已成功恢复；
- 原生对象已不可恢复且可安全释放；
- live 对象恢复失败，需要结构化 warning/failure 和 owner cleanup 决策。

不得在没有分类证据时静默清空。restore 的常态 info 日志也应改为失败或采样日志，避免每个加速动作退出
都产生字符串和 I/O。

## 5. P2 过度设计、反射与分配

### AS-05：单 owner 产品保留多 owner 仲裁器

产品固定只写入 ProductOwnerId，却继续维护：

- Dictionary<string, ActionSpeedPolicy>；
- 五组 selector wrapper；
- Func<ActionSpeedPolicy,...> 委托；
- 遍历、倍率最大值竞争和 owner ID 字符串 tie-break；
- null policy 回退和 ownerId 在完整调用链中的传递。

证据位于：
[ActionSpeedEngine.cs:11-19](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L11)、
[ActionSpeedEngine.cs:381-450](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L381)。
当前 focused gate 本身报告 policies=1。

这些机制来自 Frozen Compatibility 的多消费者模型，在 ProductNative 单产品中没有现实第二 owner。
可改为一个 normalized immutable snapshot、固定 ProductOwnerId 和预计算倍率/功能位。该改动既减少代码，
也消除每帧 dictionary enumeration 和 delegate 调用。

不应把两份执行器抽成共享 Runtime library。Compatibility 必须在产品缺席时独立工作，当前 9/11 Hook
责任也不同；共享执行器会重新把单产品策略拉进 Platform/SharedNative，并破坏 frozen host 的独立加载边界。

### AS-06：确定存在的高频反射和临时分配

当前 Engine 静态调用点统计包含 31 个 ReadMember、12 个 ReadStaticMember、4 个 ResolveType、
11 个 FindActionSpeedTarget 和 4 个 FirstText 调用点。调用点数不等于每帧次数，但源码可直接证明以下
重复工作：

1. ResolveType 每次先 Type.GetType，失败后扫描全部 loaded assemblies：
   [ActionSpeedEngine.cs:975-995](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L975)。
   Hook installer 已通过 typeof(DolocAPI).Assembly 获取 game assembly：
   [ActionSpeedHookInstaller.cs:21-27](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedHookInstaller.cs#L21)。
   Engine 可接收/缓存该 Type，不需要扩大 Advanced compiler surface。
2. ReadMember 每次沿继承链重新 GetField/GetProperty：
   [ActionSpeedEngine.cs:1079-1112](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L1079)。
3. ReadStaticMember 同样每次查找元数据：
   [ActionSpeedEngine.cs:1019-1049](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L1019)。
4. ImplementsInterface 每次调用 GetInterfaces，创建 Type 数组：
   [ActionSpeedEngine.cs:1007-1017](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L1007)。
5. FindActionSpeedTarget 的 params object[] 在每个调用点创建候选数组：
   [ActionSpeedEngine.cs:747-755](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L747)。
6. FirstText 的 params string[] 也创建数组：
   [ActionSpeedEngine.cs:1057-1065](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L1057)。
7. Animator speed 的 PropertyInfo 在每次读/写重新查询，并使用 Convert.ChangeType：
   [ActionSpeedEngine.cs:903-943](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L903)。
8. ReadCurrentActionSpeedInteractable 最坏会重复遍历 subManagers：
   [ActionSpeedEngine.cs:812-841](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L812)。

最小 NativeAccess cache 应按 Type + member name + static/instance 保存元数据，而不是缓存 Unity 对象实例。
必须保留当前 field/property/base 的查找顺序；property getter 一次抛错不能被误当成永久 member 缺失。
Type 负缓存也应在 game assembly 尚未可用时允许重试，成功后再作为进程期稳定 metadata。

FindActionSpeedTarget 可改为固定一/二/三参数重载或显式 if，FirstText 改固定二参数。Animator 字典使用
TryGetValue；restore 在证明遍历期间不会修改字典后直接遍历，去掉每动作 new List。

### AS-07：描述、日志、callback 与 QA 负担

TryClassifyActionSpeedInteraction/UseItem 在匹配后立即构造完整 item/equipment/interactable/nativeOwner
字符串：
[ActionSpeedEngine.cs:283-327](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L283)、
[ActionSpeedEngine.cs:351-375](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L351)。
ScaleActionSpeedContinuousDelta 随后每帧再构造 logKey，即使该 key 已在 HashSet：
[ActionSpeedEngine.cs:452-465](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L452)。

分类结果应先是小型 kind enum、multiplier 和必要 native references。只有以下条件之一成立时才构造长描述：

- 该固定类别尚未记录首次日志；
- 当前存在显式 QA observation window；
- 当前 callback 报错且未被节流。

loggedActionSpeedApplications 不应以 caller-derived target 字符串形成持续增长的 key；产品类别是固定集合，
可用 enum 位集或有界固定 key。Profile=Debug 和 callback exception 都应按类别/time window 限流并在边界
汇总被抑制条数。

ActionSpeedCallbacks 的五个含 __instance 入口使用捕获 lambda：
[ActionSpeedCallbacks.cs:22-30](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedCallbacks.cs#L22)。
改为直接读取 runtime 并 try/catch，或固定参数 overload，可去掉每次动作命中的 closure/delegate。
两个 ref-float Prefix 已经使用直接分发并在异常时恢复原 dt，应保留该 fail-open 语义。

QA diagnostics 默认 null 的设计是正确的；问题是 observer 一旦开启就没有有界 observation token，而且
RecordContinuousUseApplication 每帧保存长 summary。应让观察窗口显式启停，热路只记录固定 scalar；
不能为了测试方便重新让普通玩家永久承担 QA 字符串。

## 6. 测试、Hook 与维护债

### AS-08：产品 Engine 没有直接 Unit；Hook selector 过宽

产品保留 NormalizeActionSpeedPolicyForTest、TrySelectPolicyForTest 和多组分类 ForTest wrapper，但仓库
搜索只发现这些定义，没有产品侧调用者。UnitTests 的 ActionSpeed helper 实际反射调用冻结
Compatibility ActionSpeedService：
[Program.cs:14638](../../../../tests/DTMAPI.UnitTests/Program.cs#L14638)。

因此现有分类 Unit 不能保护 ProductNative Engine 的删 selector、改 fast guard、fail-closed accessor 或
日志惰性化。下一实施必须先建立直接加载已构建产品 assembly 的 focused fixture，或其它不复制生产源码的
直接产品测试。不要再添加 production ForTest wrapper 来维持 token gate。

ActionSpeedHookInstaller 当前 selector 只核对方法名和参数个数：
[ActionSpeedHookInstaller.cs:83-105](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedHookInstaller.cs#L83)。
当前受跟踪 authority 没有同名同 arity 歧义，所以这不是已发生错 Hook；未来新增相同参数数目的 overload
时可能静默命中错误目标。descriptor 应记录精确 declaring type、参数类型和返回类型，两个 continuous
目标明确要求 float。仍需先解析全部 11 个目标再安装，任何漂移整组失败关闭。

### AS-09：死配置、死 seam 与文档漂移

已确认的候选包括：

- ContinuousDrinkHoldKey：只声明与 Normalize，从未驱动输入；
- DebugLabel：只声明与 null Normalize，从未驱动行为；
- 对应 hold-key/debug-label i18n；
- GetActionSpeedLifecycleSummary 与 ActionSpeedNativeRuntime.LifecycleSummary；
- IsEatDrinkItem；
- 产品自己的 NormalizeActionSpeedPolicyForTest、TrySelectPolicyForTest 和六个分类 wrapper；
- ScaleActionSpeedContinuousDelta 的 hookSource 参数；
- ApplyActionSpeedToBody 的 source 参数；
- ReadSelectedItemEquipment 的 allowSourceFallback=true 分支，当前两个调用都固定 false；
- hasLoggedAutoFillApplication 在 Configure/ResetBoundary 未复位；
- mandatory ActionSpeedServiceProxy 中无调用者的 TryInvokeVoid。

其中 Profile 与 AutoActionCooldownSeconds 不是死字段：Profile 改变 cooldown 与 VerboseLogging，
AutoActionCooldownSeconds 影响 Safe normal AutoFill。它们当前没有在 ConfigMenu 暴露，属于隐藏行为与
迁移设计债，不能与两个无行为字段一起机械删除。

旧 JSON 多余字段是否由当前 serializer 安全忽略必须用真实 config round-trip fixture 锁定；删除已发货
字段仍需 Update 记录迁移语义。

[public-api-matrix.md:78](../../../api/public-api-matrix.md#L78) 和
[public-api-matrix.md:174](../../../api/public-api-matrix.md#L174) 仍描述九目标产品 inventory，当前 Hook map
和源码已是十一目标。该漂移不改变 IActionSpeedApi 的 Frozen 状态，但后续实现 Update 应修正当前事实；
本 audit-only Review 不直接改写 API authority。

### 需要验证的行为风险：不能在性能重构中默认固化

以下事实有源码依据，但本轮没有游戏复现，因此不写成现有玩家故障：

1. IsEmptyBottleItem 在读不到 GlobalParameter.ItemRefWastePlasticBottle 时，会把任意 ItemBottle 视为空瓶：
   [ActionSpeedEngine.cs:506-514](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L506)。
   AutoFill 对 IsCurrentStateSupportInteract 的读取失败又默认 true：
   [ActionSpeedEngine.cs:203-205](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L203)。
   accessor 漂移可能从“功能不可用”变成 fail-open 尝试；直接产品 fixture 应要求身份/能力读取失败时
   fail-closed。
2. IsBottleFillInteraction 只要 selectedEquipment 或 currentInteractable 实现 IWaterContainer 就返回 true，
   即使 selectedItem 不是 ItemBottle：
   [ActionSpeedEngine.cs:489-495](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L489)。
   当前 native owner 描述指向 ItemBottle.DrawWaterInContainer，需要非瓶子 + 水容器负例或新的原生依据。
3. cooldown 和 animal marker 都使用可跳变的 DateTimeOffset.Now。marker 的两秒过期只在下一次分类读取时
   执行：
   [ActionSpeedEngine.cs:720-731](../../../../products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs#L720)。
   如果后续没有分类，它会持有 AnimalRenderer 到其它 restore boundary。应使用单调时钟，并让 demand
   关闭、退出/失败和 lifecycle boundary 明确清理，不要为清 marker 反向增加永久 updater。

### AS-10：冻结 Compatibility 需要独立节流，不是当前产品瘦身捷径

对于真正调用旧 IActionSpeedApi 的消费者，冻结 ActionSpeedService 仍有自己的活动压力：

- AutoFill Update 中反复 owner reconciliation；
- reconciliation 解析 native target 并读取多个 Harmony owner；
- 工具、交互、continuous 和 AutoFill 路径构造 List、状态摘要与 Hook status。

可以在 frozen ABI 不变的前提下缓存 MethodInfo/target，按产品 load/patch/lifecycle dirty boundary
reconcile，并让 status/summary 只在状态变化或 GetStatus/QA 读取时构造。

但必须保留：

- product-first 与 compatibility-first 两种加载顺序；
- AutoFill-only compatibility demand 的冲突仲裁；
- 产品缺席时旧消费者仍能工作；
- mandatory proxy 的相同 provider/ABI；
- Mono 宿主一旦加载不承诺卸载。

不得让 frozen host 依赖当前产品 DLL，也不得以当前产品不消费 API 为由删除 ABI。Compatibility 的物理
源码将来可移入 Compatibility 项目以减少所有权误读，但这只有维护收益，没有普通玩家运行时收益。

### AS-11：工具链与 focused gate 仍有历史重量

tools/scripts/test-batch6-actionspeed-advanced-product.ps1 当前通过，输出：

    DTMAPI Batch 6 ActionSpeed Advanced product checks: OK
    source-files=6, hooks=11, policies=1, native-authorities=9

但该 focused gate 主要依赖源码 token，并同时检查全局 smoke source selection、G5/G6 顺序和 reverse
文件存在性。这些跨域事实应分别归入 smoke-runner contract 和 native-owner audit；产品 focused gate
保留 manifest/policy、11 Hook、atomic cleanup、zero frozen API consumption 和 owner collision 不变量。

通用 Advanced builder 还会先 build 再 pack，而 pack 内部再次 Build；Release 的 primary/repeat
determinism 又执行两轮，形成每产品约四次编译。可让每轮 pack 消费同一轮已绑定 build artifact，仍保留
两次独立 pack/hash 比较。该项降低 CI/发布耗时，不降低玩家运行时压力，应与产品热路径优化分开计量。

## 7. 必须保留的复杂度

以下结构有现行 native/lifecycle/ABI 依据，不属于可直接删除的过度设计：

1. 11 个 exact-owner Hook。AgentStateWater 是独立工具状态；AgentStateBase.OnExit 为没有专用 OnExit
   override 的状态和中断提供恢复兜底。把它们合并成 AgentStateBase.Update 的中央每帧 Hook 反而扩大热路。
2. 安装前解析全部目标、任一失败整组回滚、exact-owner unpatch 和兼容 owner collision fail-closed。
3. callback detach、native restore 与 unpatch 分别尝试并聚合失败的 deactivation 结构。
4. ModEntry 的 Entry rollback flags，以及 Deactivating owner 阶段不主动从平台 event root 执行 -= 的边界。
5. ActionSpeedPolicy 作为已保存配置的 immutable runtime snapshot 语义；可缩成单 snapshot，不能删掉隔离。
6. 默认 null 的 QA diagnostics 和独立 QA inventory；它们需要改成有界/低干扰，不应删除 exact candidate
   观察能力，也不应从产品 installer 自动生成全部独立期望值而失去交叉校验。
7. Frozen IActionSpeedApi、mandatory proxy、dormant Compatibility Host 和双加载顺序仲裁。
8. ProductNative 所有权。当前没有第二个独立真实消费者共享同一 native owner，不得提升到 GameBridge。

## 8. 建议的轻量化实施顺序

### Slice A：先消除无需求工作

1. 建立直接 ProductNative Engine fixture；
2. 将配置归一化为单一 immutable snapshot；
3. 预计算 Tool/Interact/UseItem/Animal/AutoFill demand bits；
4. 所有 callback 在 native reflection 前 fast return；
5. 增加 gameplay/save-context gate，标题页 AutoFill updater 为零；
6. AutoFill 每次 probe 都推进单调 nextProbeAt；
7. 复位 hasLoggedAutoFillApplication 和所有边界态。

这是最高收益、最小行为变化的一片。不要在同一片做动态 Hook 安装/卸载。

### Slice B：消除可确定的短命分配

1. 缓存 DolocAPI Type、member metadata、interface flags、Animator speed 与 UseAsItem MethodInfo；
2. 用固定参数 helper 替代 params 数组；
3. 分类返回 enum/倍率，description/logKey 惰性生成；
4. 用固定 enum 位集替代 target 字符串 HashSet；
5. callback 改直接分发；
6. restore 去掉 List copy，并结构化处理失败；
7. normal/Debug/exception 日志统一限流；
8. QA observation 改显式有界窗口与 scalar-only 热路。

### Slice C：再做结构和维护收口

1. 删除有直接零调用证据的 seam、参数和无行为配置/i18n；
2. 精确化 11 个 Hook descriptor；
3. 修正文档九/十一 target 漂移；
4. 将产品 focused gate 与跨域 historical audit 分开；
5. 消除 build/pack 重复编译；
6. 单独为 frozen Compatibility executor 做 metadata/reconciliation/status 节流。

### 暂缓：state-scoped 分类缓存

把 Interact/UseItem 分类在 OnEnter 计算一次、Continues 只复用 multiplier，可能进一步消除逐帧交互树读取。
但当前 UseItemContinues 不一定总有对应的 Interact OnEnter，native target/水中状态/选中物也可能在按住
期间变化。该优化必须先建立原生状态矩阵和 stale-object 清理，不应与 Slice A/B 一起凭假设实施。

## 9. 后续验证设计

### 9.1 Source/Unit

直接产品 fixture 至少覆盖：

- Enabled=false 时所有 callback 不访问 native accessor；
- 单个 feature 关闭时其分类 lookup 为零；
- animal demand 关闭时 marker 写入为零；
- 配置保存前后 snapshot 隔离；
- Bottle/Plant/Machine/Harvest/Animal/EatDrink 正负例；
- accessor 缺失 fail-closed，不把任意 ItemBottle 或状态读取失败默认当成可执行；
- Animator 首次记录、重复应用、成功恢复、live restore failure 与 destroyed-object 分类；
- AutoFill title/save context、probe cadence、异常、config toggle 和边界复位；
- log/QA string builder 只在显式需要时调用；
- callback 异常恢复原 dt 且日志限流；
- exact 11 target resolution，包括 float 参数类型。

CoreCLR focused benchmark 可用 GC.GetAllocatedBytesForCurrentThread 比较优化前后同一 fake workload 的
相对托管分配，但它只证明 host-side 相对变化，不能替代 Unity Mono 游戏证据。

### 9.2 有界游戏验收

实施后使用第三存档、NoNativeSave、shared runtime lock，做一个真实持续输入短窗口，不重放完整历史
L0–L5：

- 标题页且 AutoFill 配置开启：产品 Update callback/probe 均为 0；
- gameplay 空闲、陆地、错误物品、空瓶：probe 遵守 cadence 上限；
- master off、feature off、1x、常用倍率、4x；
- 真实按住 UseItem 与 Interact，而不是每十分钟十个 fixture unit；
- Tool、Interact、Eat/Drink、AutoFill、手动浇水开/关；
- ConfigApply、ReturnedToTitle、reload、owner deactivation；
- 11 target/patch、callback、Animator snapshot、marker、cooldown、event/Core root 和进程退出归零。

QA 热路只记录固定 scalar：

- updateCallbackCount；
- nativeProbeCount；
- continuousCallbackCount；
- classificationCount；
- metadataLookup/cacheMissCount；
- descriptionBuildCount；
- normal/debug/failureLogCount；
- restoreSuccess/failure/deadObjectCount；
- fixed log-category cardinality。

Unity allocation counter 不可用时继续记录 Blocked/null。Mono/Gen0、process memory 和帧时只作趋势诊断；
不得从短窗口或无 Fatal 推导全局 Unity/Mono GC 问题已解决。

### 9.3 验收停止条件

Slice A/B 的接受标准应是可观察工作量：

- 标题页 ActionSpeed updater 为 0；
- master/对应 demand 关闭时 native accessor、classification 和 marker 为 0；
- AutoFill probe 不超过配置 cadence；
- active continuous 首次类别日志之后不再构造 target/logKey；
- QA 开关不改变 normal hot-path string allocation；
- restore failure 不再以清空字典伪装成功；
- 11 Hook 行为、手动浇水和 exact-owner cleanup 不回归。

不以“Engine 从 1,201 行降到某个数字”或“与 Compatibility 重合率下降”作为性能验收。

## 10. Disposition

GO：

- 建立一个有界 ProductNative implementation Update，先完成 Slice A；
- Slice A 绿后批量完成 Slice B 的确定性缓存/分配收口；
- 将当前手动浇水 runtime pending gate 与优化后的一个第三存档 NoNativeSave 短验收合并；
- 单独规划 frozen Compatibility 节流和工具链去重。

NO-GO：

- 为减少数字而删除 11 Hook 或改成中央每帧 AgentStateBase.Update Hook；
- 先做动态 patch/unpatch；
- 把 ProductNative Engine 抽回 GameBridge/SharedNative；
- 让 Compatibility Host 依赖当前产品 DLL；
- 删除 IActionSpeedApi、proxy 或双加载顺序仲裁；
- 用 Debug Profile/永久 QA summary 做性能基准；
- 将历史低密度 ladder 或不可用 allocation counter 写成当前产品低分配证明；
- 在未分类 destroyed/live Animator 失败前简单保留或简单丢弃全部 restore snapshot。

## 11. 验证与边界

- 已阅读项目 required context、文档治理、Review/feedback workflow、ActionSpeed Hook map、active smoke matrix、
  ISSUE-010、当前 API matrix、managed-product admission、最新 ActionSpeed Update/Review 和两套相关 native
  reverse build；
- 已逐项核对 ActionSpeed 产品 6 个生产文件、QA observer/fixture、frozen Compatibility service、
  mandatory proxy/broker、focused source gate、historical GC ladder 和 retained ABI consumer evidence；
- tools/scripts/test-batch6-actionspeed-advanced-product.ps1：PASS
  （source-files=6, hooks=11, policies=1, native-authorities=9）；
- tools/scripts/check-doc-governance.ps1：PASS（7429 checks）；
- 本 Review 的 41 个相对 Markdown 文件链接解析检查：PASS（missing=0）；
- 本 Review 的 git diff --check：PASS；
- 本 Review 写入前未运行产品 build、Unit、Release 或 game smoke；源码 gate 通过不冒充编译、行为或运行时
  性能证据；
- 未启动 Doloc Town，未获取 runtime lock，未安装/卸载 Runtime，未修改 Workshop、Official MODS、
  profile 或存档；
- 后续实现完成叙事不得追加回本 Review；只可增加简短 resolution link，生命周期事实由 owning Update
  维护。
