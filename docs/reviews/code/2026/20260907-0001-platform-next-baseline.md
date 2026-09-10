# DTMAPI 下一阶段平台工程基线复核

- Status: recorded
- Role: current planning evidence
- Source Request: 用户要求重新验证当前本体、测试、SDK、Public API、Runtime 与 GameBridge，形成第三方作者可独立开发、调试、发布和维护 Mod 的可接手工程计划。
- Scope: 当前编译源与作者流程；不处理 Wiki，不以旧审核结论为前提，不开展大规模实现。
- Owning Update: [平台下一阶段工程计划](../../../updates/2026/20260907-0001-platform-next-engineering-plan.md)。本轮执行、验证结果、原始日志和后续状态由该 Update 维护。
- Contract Authority: [Public API matrix](../../../api/public-api-matrix.md)。本文的建议不是已发布契约或稳定性晋级。
- Stable Boundaries: [PROJECT.md](../../../../PROJECT.md)；规划中的新契约须通过后续实施显式落地，不能把本文当作现有 Runtime 已开放的能力。

## 复核方法与结论边界

重新阅读项目与当前事实入口，检查 csproj 引用、Compile Remove/Include、当前调用路径和行为测试。
旧 Review、Debug issue 与 smoke 只用于识别需要保护的行为和未关闭风险，不作为当前代码正确性的证明。
下面标为“事实”的内容可从链接源码复核；“判断”是由这些事实推导的工程取舍；未运行的假设明确保留为待验证。
本轮没有重新证明游戏 Hook、原生保存、主线程调度或退出行为；这些能力不能凭构建结果宣告通过。

主要判断：平台已具备有价值的加载事务、owner 清理、事件内核、按需原生适配和诊断基础；当前首要缺口是作者流程断点与公共基础工具不足。
近期应修通作者流程，再以小片段收敛内部结构；无需推倒 Runtime 或先重建所有游戏领域 API。

## 八项本体代码发现

### 1. 程序集依赖骨架可以保留

事实：[Core.csproj](../../../../src/DTMAPI.Core/DTMAPI.Core.csproj) 只引用 Abstractions；[GameBridge.csproj](../../../../src/DTMAPI.GameBridge.DolocTown/DTMAPI.GameBridge.DolocTown.csproj) 引用 Core/Abstractions；[Bootstrap.csproj](../../../../src/DTMAPI.BepInExBootstrap/DTMAPI.BepInExBootstrap.csproj) 组合 Core、GameBridge、ModConfigMenu 与宿主入口。
游戏加载程序集仍是 `netstandard2.0`；Core 没有对 GameBridge 的反向项目引用。
判断：先通过内部模块和构造依赖拆分责任，保留当前发布布局。增加 DLL 只有在独立加载、可选分发或稳定契约边界确有需要时才有价值。
验收关注：新平台工具保持 BCL-only；Core 不引用 Unity、Harmony 或具体游戏类型；内部拆分不制造编译环。

### 2. Runtime 总协调器承载过多不同责任

事实：[DtmApiRuntime.cs](../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs) 本次读取为 4,640 行。
`Start`、`DiscoverMods`、`LoadMods`、`LoadCodeMod`、`DeactivateOwner` 与输入、AuthorSession、报告、Doctor、资源观察和状态发布共处一类。
`DiscoverMods` 同时扫描来源、读取历史 Author 状态、发布集合、记录诊断并刷新内容索引；加载完成后还投影多个观察系统。
判断：维护成本来自不同责任和状态投影交织，不能仅以行数定罪。继续把数据、命令、反射直接塞入此类会扩大回归面。
候选拆分顺序是只读报告投影、发现/依赖计划、Entry 事务与 owner 服务；实际切片以 [PN-014](../../../planning/platform-next/tasks.md)及当前作者任务需要为准，不另设第二套任务顺序。不要同时更换来源规则和加载状态机。

### 3. 加载事务与 owner 生命周期是保留资产

事实：`LoadCodeMod` 在程序集加载、构造、AttachContext、Entry 和多个发布步骤设置 `ModLoadCheckpoint`。
程序集进入进程后记录 `loadedAssemblyOwnerIds`；失败统一进入 `DeactivateOwner`，不能把后续 Entry 失败当作程序集从未加载。
[ModOwnerLifecycleCoordinator](../../../../src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs) 拒绝相同 owner 的非法本进程再进入；终态仍允许重试清理。
`DeactivateOwner` 先做准备，准备失败保留原有 roots；随后分步清理，按残余资源与失败确定重启要求，并隔离诊断异常。
判断：这不是应整体删除的防御层。可以移动实现，但必须保留原有事务顺序、清理重试和进程寿命事实。
验收关注：成功 Entry 只发布一次；各 checkpoint 故障无平台资源泄漏；日志失败不反转成功加载；DLL 停用不冒充卸载。

### 4. 通用反射尚未形成一致内核

事实：[GameBridgeNativeHelpers](../../../../src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeHelpers.cs) 的 `ResolveType` 会回退扫描已加载程序集；回退只使用类型全名，丢失请求中的程序集身份约束。
`ReadStaticMember` 可将未找到、合法 null 与访问异常都变成 null；部分调用吞掉异常，调用者无法区分失败类别。
[GameBridgeNativeAccessors](../../../../src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeAccessors.cs) 的 `FindMethod` 按名字和参数数量返回首个候选；其委托 getter 使用 Expression.Compile。
[HarmonyReflectionPatcher](../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs) 已有类型正/负缓存以及 AssemblyLoad 清空机制，不能忽略既有成果。
判断：先建设 BCL-only 查找/调用内核，再公开作者包装；不要直接把现有 helpers 改成 public。
未验证推断：查找在缓存锁外、结果随后写回，与 AssemblyLoad 清空交错时可能重新写入旧负结果；需要代际缓存测试证明或排除。
验收关注：精确程序集身份、继承与隐藏、重载签名、null/missing、异常归因、晚加载失效、缓存不持有游戏实例，以及真实 Mono 调用。

### 5. 事件与按需执行已有行为测试基础

事实：[EventManager](../../../../src/DTMAPI.Core/Services/EventManager.cs) 已有 owner 归属、快照、线程边界、有界队列、合并策略与高频异常隔离。
[Batch5EventKernelTests](../../../../tests/DTMAPI.Core.Tests/Batch5EventKernelTests.cs) 覆盖零监听零分配、copy-on-write、重入、订阅增删、队列策略、清理与诊断分配。
[GameBridge demand routing](../../../../src/DTMAPI.GameBridge.DolocTown/Demand/DolocTownGameBridge.Demand.cs) 使用 capability catalog、活跃 updater snapshot 和 retained callback mask。
[Batch5GameBridgeDemandTests](../../../../tests/DTMAPI.Compatibility.Tests/Batch5GameBridgeDemandTests.cs) 包括 idle scheduler 零分配、一万帧无 optional 工作、同步重入抑制和零需求 callback 休眠测试。
判断：保留这些内核，不另建平行 event bus 或 demand 系统；新增命令、投递、资源订阅都应接入同一 owner 生命周期。
限制：这些源测试存在不等于本轮全部运行通过，更不等于 Mono 中的真实帧开销已被验证。

### 6. 运行时配置混合真实开关与迁移残留

事实：[RuntimeSubsystemOptions](../../../../src/DTMAPI.Core/Runtime/RuntimeSubsystemOptions.cs) 保留 `refactor-scaffold.json`、历史序列化名与 21 个可空选项。
Runtime 的 `LoadRefactorScaffoldOptions` 将 ModLoadTransaction、OwnerBoundInput、EventHandlerQuarantine 强制设为 true。
ShadowResourceLoader 与 RegistryTakesOver 仍主要发布 configured/blocked 诊断，代码明确说明没有接管资源加载。
判断：应区分不可关闭的正确性机制、可选诊断和未实现能力。未实现开关不应继续表现为可启用产品功能。
可逆处理：保留旧配置读取与诊断映射，运行路径只使用真实选项；不为清理命名强迫玩家重写配置。
不得由此推导“所有 ledger 都能删除”；先查每个投影的消费者与实际定位价值。

### 7. Compatibility 源码重复不等于 mandatory Runtime 重复

事实：GameBridge csproj 对多项 Compatibility 大实现使用 Compile Remove；[Compatibility.csproj](../../../../src/DTMAPI.GameBridge.DolocTown.Compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.csproj) 将同一源以 Link 编译到可选组件。
[CompatibilityHostBroker](../../../../src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/CompatibilityHostBroker.cs) 没有可选组件的静态程序集引用，首次 frozen ABI 调用后才加载 executor。
Broker 检查规范路径、程序集身份、版本和目标框架，并明确不承诺卸载已进入进程的组件。
判断：不能通过目录名或磁盘行数决定删除。物理退役必须先查实际消费者、ABI fixture、package manifest 和两个 owner 加载顺序。
近期可整理所有权与源文件位置；完整 frozen ABI 退役应独立成批，不能混入通用反射建设。

### 8. Bootstrap 的复杂性包含宿主约束

事实：[BootstrapPlugin](../../../../src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs) 本次读取为 869 行，接入 NativeGameUpdate、PlayerLoop、InputSystemAfterUpdate 和 Unity callbacks。
`PumpFrame` 包含线程检查、同帧去重、来源优先级/时效；Runtime.Update 再次拒绝非 runtime thread 的普通事件派发。
`OnApplicationQuit` 依次关闭 fallback、UI、GameBridge 与 Runtime；[FrameDriverHealthPolicy](../../../../src/DTMAPI.BepInExBootstrap/FrameDriverHealthPolicy.cs) 区分全局暂停和单个 driver 停滞。
判断：适合抽出 FramePump/HostAdapter，暂不砍 fallback。新的主线程投递必须接入既有唯一 dispatch 边界。
未证明事项：无法仅凭本轮代码阅读判断某个 callback 来源已冗余，或历史 native GC/退出问题已根治。
验收关注：标题/读档/回标题、多来源同帧恰好一次更新、暂停后恢复、禁止 TimerFallback 调 Mod/Unity、干净退出。

## 作者流程与公共契约的直接断点

### SDK 本地部署入口暂停

事实：[AuthorApplication](../../../../src/DTMAPI.AuthorSdk/AuthorApplication.cs) 的 `IsPausedLegacyGameModsMutation` 在分发执行前返回 SDK003，覆盖 deploy、update、install-local、source local select。
[SDK README](../../../../author-sdk/README.md) 说明旧 `<game>/Mods` 不再是玩家发现来源，而新的 official MODS 事务尚未随 SDK 发布。
判断：能够 new/build/pack 还不等于作者闭环完整。优先实现合法官方来源的开发安装、替换、撤回与状态确认，并沿用现有事务恢复能力。
验收应从空项目走到游戏识别与再次迭代，不能由维护者手工复制绕过 SDK 来证明流程完成。

### API 编译目标与 Author Session 协议版本耦合

事实：[AuthorSdkContract](../../../../src/DTMAPI.Authoring.Contracts/AuthorContracts.cs) 的 TargetRuntimeVersion 是 0.5.5；SDK 使用 frozen API payload。
[AuthorSessionService](../../../../src/DTMAPI.AuthorSdk/AuthorSessionService.cs) 的 prepare descriptor、请求及响应检查使用同一目标版本。
[AuthorSessionDescriptorStore.ValidateDescriptor](../../../../src/DTMAPI.Core/Runtime/AuthorSessionDescriptorStore.cs) 精确比较 descriptor.RuntimeVersion 与运行时参数；[DtmApiRuntime.InitializeAuthorSession](../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs) 传入 ApiVersion。
当前 Runtime 版本由 [version props](../../../../tools/release/dtmapi-runtime-version.props) 生成，源代码对应 0.6.1。
组合证据：真实 `AuthorApplication.RunAsync session prepare` 成功生成 descriptor；Core 按当前 0.6.1 消费时拒绝为 `descriptor-runtime-mismatch`，独立目录的 0.5.5 control 接受为 `descriptor-accepted`。
判断：这已构成 SDK 与当前 Runtime 的可复现集成缺陷，超出单纯文档漂移；精确命令、日志与验证状态由 [本轮 Update](../../../updates/2026/20260907-0001-platform-next-engineering-plan.md) 维护。
判断：应拆开 SDK 工具版本、编译 API 基线、Session 协议版本与目标 Runtime 能力协商，不能仅把所有常量一起改到最新版本。

### 辅助程序集与跨 Mod API 的组合能力受限

事实：[ProjectValidator](../../../../src/DTMAPI.AuthorSdk/ProjectValidator.cs) 的 SDK161 拒绝作者输入携带 DLL 依赖。
[ModRegistry.GetApi](../../../../src/DTMAPI.Core/Services/RegistryAndHelpers.cs) 按 `(ownerId, typeof(TApi))` 查找，返回通过 CLR 类型检查的对象；没有结构化接口代理。
判断：两个 Mod 各自复制同名接口不会自然获得同一 CLR 类型身份。放开辅助 DLL 前必须设计共享契约与 Mono 加载冲突行为。
优先支持显式共享契约身份、明确依赖版本和加载错误；不要未经验证承诺任意私有 DLL 隔离，也不应把反射代理当默认兼容方案。
一般第三方原生作者通道仍受当前身份规则约束；要开放必须同时迁移 SDK、manifest、Runtime 和诊断承诺，不能只放宽分类器一条判断。

## 测试基线漂移的审查结论

本轮执行结果、五套测试状态与日志只查 [owning Update](../../../updates/2026/20260907-0001-platform-next-engineering-plan.md)，本文不维护第二套执行台账。
代码事实：[Unit Program 的 PreviewVersionMetadataIsConsistent](../../../../tests/DTMAPI.UnitTests/Program.cs) 在约 2737 行将当前发布记录绑定到硬编码旧 tree hashes。
其旧期望前缀为 `8280dc...` / `c73359...`，而 [Product Catalog](../../../../tools/release/dtmapi-product-catalog.json) 当前对应记录前缀为 `846665...` / `b4ec6a...`。
判断：这是测试期望与当前发布记录漂移，不能据此直接认定 Runtime 行为坏，也不能盲目替换 hash 使测试变绿。
修复前应核对 Catalog、当前 subscription manifest、它指向的发布 Update 和实际生成/校验规则；明确哪些是现行不变量，哪些只是历史包断言。
完整套件遇首个失败后，未执行的断言不能写成通过；应提供有界的剩余测试诊断方式，最终仍保留一次完整从头验收。

## 取舍清单

| 处置 | 对象 | 约束 |
| --- | --- | --- |
| 保留 | 单向程序集依赖、netstandard2.0 宿主边界 | 不为模块拆分额外改变分发布局。 |
| 保留 | Entry 事务、owner 生命周期、平台 roots 清理、重启门 | DLL 进入进程后不能因清理成功声称已卸载。 |
| 保留 | 事件快照、有界队列、无需求 fast path、现有行为测试 | 新能力复用这些机制。 |
| 合并 | 类型/成员查找、异常语义、反射缓存 | 游戏成员名和版本映射留在 native adapter。 |
| 合并 | Manager/Doctor/报告中的运行状态投影 | 不建立第二份实际 loaded/active 事实。 |
| 拆分 | Runtime 报告、发现与加载计划、Entry/owner 服务 | 按此顺序小片移动，不同时改变行为政策。 |
| 拆分 | Bootstrap FramePump 与宿主接线 | 保持现有优先级与退出链，真实游戏证明后才能进一步简化。 |
| 收口 | 不可关闭的正确性选项和没有实现的迁移开关 | 兼容读取旧配置；删除的是无效执行分支，不是证据。 |
| 延后删除 | frozen ABI、Compatibility executor、旧部署 recovery | 先查消费者与恢复责任，独立退役批次。 |
| 不推荐 | 全量 Runtime 重写、第二套事件总线、普遍热卸载、泛化 object 游戏服务 | 当前没有证据表明收益超过兼容与维护成本。 |

## 目标方向与实施排序依据

逻辑边界保留为 Abstractions 契约、Core 平台服务、GameBridge 原生适配、Bootstrap 宿主组合、可选 Content Host 与产品代码。
通用反射属于平台基础工具；具体游戏成员绑定、主线程要求、Unity 对象失效与保存责任仍属于各自 adapter/owner。
普通作者首先应能完成公开接口示例，再按需要进入明确的原生开发通道；通道表达依赖和兼容承诺，不冒充安全沙箱。
第一阶段建议依次处理测试基线可解释性、Session 协议脱耦、官方来源作者安装闭环、反射内核和最小示例；内部抽取只做支撑这些任务的部分。
随后推进辅助程序集/共享契约、owner-bound 主线程投递、通用数据会话，再做资源加载/内容修改管线。
动物、机器、任务等领域内容引擎保持方向性；先以一个真实数据领域和一种资源证明组合、冲突与刷新语义。
详细任务输入、依赖、验收和回滚由本轮工程计划维护，不在本 Review 复制任务进度。

## 后续实现的证据门

- 结构拆分：行为测试维持同一发布/清理顺序；不以源文本匹配替代运行结果。
- 反射：精确签名、合法 null、错误归因、晚加载与缓存寿命；同时覆盖 .NET 8 和游戏 Mono。
- Loader：辅助 DLL 冲突、重复身份、缺依赖、入口异常、更新后重启；真实 SDK 产物进入 Runtime。
- 生命周期：停用后无新注册、未开始的投递可取消、回标题释放 session roots；不把平台清理当未知 Hook 的保证。
- 持久数据：遵循 PROJECT 的原生成功提交边界；未保存回滚、正常保存、保存失败和跨存储中断分别验收。
- native/API 新领域：先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。
- 性能：零 Mod/零监听/零需求基线、cold/warm 反射、启动时间、帧分配与回标题 roots；代码行数不作为性能指标。
- 玩家集成：仅在相关集成边界进行真实游戏/干净退出验证；本规划不生成新的 smoke 成功记录。
