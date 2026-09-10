# 平台 Runtime、生命周期与执行契约

- Lifecycle: `accepted-design`
- Role: 下一阶段 Runtime 公共语义和内部实现边界的设计依据；不是当前 API 已经发布的声明。
- Parent: [平台目标架构](platform-next.md)
- Implementation order/status: [平台路线](../planning/platform-next/roadmap.md)、[任务卡](../planning/platform-next/tasks.md)、[状态队列](../planning/platform-next/status.md)
- Current contract: [Public API matrix](../api/public-api-matrix.md)；身份、物理归属和保存提交规范仍由 [PROJECT](../../PROJECT.md) 拥有。

本设计保留现有五程序集 Runtime 骨架和旧 `DtmMod.Entry(IDtmHelper)`、`IDtmHelper` ABI。新能力通过路线指定的可选服务扩展提供，不增加新的必选 Runtime DLL。内部实现方案已经选定；实现任务无需重新做全仓架构设计。

## 当前事实与设计差距

| 当前代码 | 已有行为 | 本设计需要补足的能力 |
| --- | --- | --- |
| [Core Start](../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs) 的 `Start` | 全部 Mod Entry 之后派发 GameLaunched | 独立作者能读懂的阶段与上下文；GameLaunched 不能解释为世界就绪 |
| [Owner coordinator](../../src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs) 和 Runtime `DeactivateOwner` | Entry 事务、Owner 关闭、清理重试、已加载 DLL 的重启要求 | 作者可使用的作用域资源登记、排队任务失效及清理可观察结果 |
| [EventManager](../../src/DTMAPI.Core/Services/EventManager.cs) | 主线程边界、有限队列、成员快照、Owner 隔离、handler 熔断 | 与公开生命周期一致的语义；通用 scheduler 不能借普通事件队列伪装实现 |
| [Bootstrap frame driver](../../src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs) 和 [GameBridge hooks](../../src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs) | PlayerLoop/InputSystem/native 帧驱动回退、去重，timer 不执行普通 Mod update | 作者执行阶段、主线程排队、取消和预算；复用当前唯一帧驱动 |
| Runtime `NotifySaveLoaded` / `NotifyReturnedToTitle` | 清输入瞬态、换 save generation、保留进程 Owner 服务 | 世界会话与持久存档身份分离；旧队列和借用对象不可跨档 |
| [SaveGame callbacks](../../src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs) | 原生保存返回成功才派发 SaveSaved；SaveSaving 异常可取消 native save | 通用 Data 的事务参与者、健康状态、失败/中断恢复；留给 M4 专门切片 |
| [Registry](../../src/DTMAPI.Core/Services/RegistryAndHelpers.cs) | 按 provider ID 和准确 CLR Type 获取 API；部分平台 provider 有 owner factory | 共享契约程序集加载政策、直接返回对象的寿命边界、第三方诊断 |

本轮没有新增游戏证据。现有产品 smoke 证明特定产品、候选包和游戏 build 的行为；不能证明下述新契约成立。M1 先记录真实事实和第三方 Mono 调用，M2 实现公共生命周期/调度；M4 才实施保存语义迁移。

## RT-01：生命周期事实先行，旧事件含义不变

M1 的生命周期任务必须产出同一真实进程内的顺序记录：Bootstrap 启动、Core Start、每个 Entry、GameLaunched、native LoadGame 进入/返回、AfterLoadArchiveData、SaveLoaded、第一次可访问当前世界的帧、返回标题和退出。覆盖已有存档、新游戏、加载失败以及标题→档 A→标题→档 B。

记录应携带线程、Owner、runtime instance、native slot（诊断用）、save epoch、world epoch 和准确原生来源；沿用现有日志、生命周期诊断和 smoke 系统，不建立新的验收收据体系。正常配置不保留每帧明细。

旧事件保持以下解释：

- `GameLaunched`：本次进程初始 Mod Entry 阶段已经结束；不是地图、资源或存档已加载，也不代表所有 Mod 均成功。
- `SaveLoaded`：现行 AfterLoadArchiveData 对应边界；不把该名字扩张为所有 native 对象均可用。新服务只有在 M1 证明的原生时点满足条件后才报告 WorldReady。
- `ReturnedToTitle`：现行返回标题边界。Owner 的进程服务继续存在，save/world 临时资源与工作失效。不开第二次 Entry，也不卸载 DLL。
- `UpdateTicked`：沿用现有一帧最多一次的 Core dispatch；计数不是墙钟、游戏日期或物理模拟时钟。

M1 不为验证方便伪造 SaveSaved、不改原生事件触发含义，也不把一次成功读档推广为全生命周期已完成。

## RT-02：三种作用域与可选上下文服务

选择 Runtime/ModOwner、SaveSession、World 三种执行寿命。M4 若因独立存储或实际外部引用需要持久 SaveIdentity，再验证其语义；不与执行寿命混用，也不把永久身份协议预设为同档 SaveData 的前置。

| 标识 | 创建与失效 | 用途 |
| --- | --- | --- |
| RuntimeInstanceId | 每进程唯一；退出失效 | 跨进程诊断和会话隔离；不写入作者存档作为持久 key |
| ModOwner | Entry 开始允许登记；Owner deactivation 或 Runtime shutdown 关闭 | 配置、命令、事件、作者服务、进程缓存 |
| SaveSessionEpoch | 每次新游戏/读档尝试开始分配新单调 generation；失败也不复用；返回标题关闭 | 取消旧档排队任务、区分同一存档的两次载入 |
| WorldEpoch | 当前 save 内每次原生环境切换使旧值失效；新环境达到已证明的 ready 点后才开放新值 | 短期世界对象/目标句柄、场景任务、内容传播 |
| Persistent SaveIdentity（条件性） | M4 的所选后端或实际外部引用需要时，验证产生、复制、重用与恢复后再决定是否公开 | 独立载荷的持久关联不能只靠 native slot、玩家名或执行 epoch；同档载荷正确随原生档移动不要求额外公开永久 ID |

M2 新增的可选上下文服务返回不可变快照。快照至少包含 `RuntimeInstanceId`、单调 `Revision`、`Phase`、可空 `SaveSessionEpoch`、可空 `WorldEpoch`、`IsWorldReady`。阶段仅为 `Starting`、`Title`、`LoadingSave`、`LoadingWorld`、`WorldReady`、`ReturningToTitle`、`ShuttingDown`；不暴露内部诊断 phase 字符串形成无界公共枚举。

同一存档内切换场景使用 `LoadingWorld`：保留 `SaveSessionEpoch`，先关闭旧 `WorldEpoch`、报告 `IsWorldReady=false`，在新的原生环境达到就绪点后发布新 `WorldEpoch` 和 `WorldReady`。不能把换场景冒充重新加载存档，也不能一边保持 `Phase=WorldReady` 一边报告未就绪。完整新游戏/读档才创建新的 save epoch；场景切换只取消 World scope 工作，SaveSession scope 工作仍受同一存档寿命保护。

服务的 `IsMainThread` 判断调用线程，不放进冻结快照。读取已发布快照可以在任何线程进行，读取不触碰 Unity/native。修改只能在 Runtime 主线程由已验证的 GameBridge 边界驱动。没有准确 native 证据时保持未就绪，不能从“上次有存档”推断 readiness。

快照的 WorldReady 只表示契约列出的基础世界查询可以运行。动物、地图编辑等独立能力仍有自己的 availability；它不表示所有可选 Hook/产品已就绪。暂停、失焦、游戏时间和场景名字不先加入该服务；只在原生事实和实际作者消费者需要时追加独立可选信息。

提供 owner-bound 生命周期通知用于已证明的上下文变化；先发布新快照，再调用该通知。普通事件仍按现行次序分发。不得通过给旧稳定接口新增抽象成员实现此服务；旧 helper 实现者和旧 consumer DLL 都要实际加载、调用验证。

## RT-03：统一、有界、非阻塞的主线程 scheduler

Core 实现 owner-bound scheduler。Bootstrap 只提供既有唯一帧入口；GameBridge 只提供 native 生命周期与 readiness。不得再引入独立 Unity Update driver 或用 timer 直接执行作者回调。

首版作者形状由以下操作组成，精确命名在 M2 API 切片一次性冻结：

| 操作 | 承诺 |
| --- | --- |
| `Post(Action, scope, cancellationToken)` | 即使从主线程调用也排队，不内联执行；最早在下一个调度 drain 阶段运行 |
| `NextTick(Action, scope, cancellationToken)` | 最早在后续一次 Core tick 的 drain 阶段运行；与 Post 的 ready-now 入队语义明确区分 |
| `Delay(Action, delay, scope, cancellationToken)` | 使用单调墙钟到期，恢复 drain 时执行；不表示游戏分钟、睡眠或暂停模拟时间 |
| 返回 work handle | 暴露状态、`TryCancel()`、`Completion`；不要求作者轮询；终态释放回调及捕获对象 |

`scope` 必须由作者显式选择 ModOwner、CurrentSaveSession 或 CurrentWorld；后两种在入队时捕获准确 epoch。无当前作用域立即返回拒绝结果。`CurrentWorld` 还要求 WorldReady。作者执行 Unity/native 工作应选择当前 save/world 作用域；ModOwner 适合配置、日志、全局服务和命令管理。

具体执行规则已经选定：

1. 在 Core Update 的固定位置调用一个 scheduler drain，位于普通 UpdateTicked 前；已有开始/结束 runtime queue flush 保留。M1 的帧源证据必须证明该位置不会因来源回退重复运行。
2. drain 开始取得本帧准入截止序号。Post 在 drain 开始前到达且已到期，可以本帧执行；回调内新建工作不得在本次 drain 继续递归执行。NextTick 的最早 tick 必须严格大于提交 tick，即使调用发生在本帧 drain 之前。延迟到期与排队序号共同定义执行顺序。
3. 同一 Owner 的同时到期任务保持 FIFO；多个 Owner 使用轮转公平调度。一个高产 Owner 不得抢光队列或永久饿死其他 Owner。跨 Owner 不承诺严格全局 FIFO。
4. 默认内部上限为全局 1024 个待执行项、每 Owner 128 个，单帧最多开始 64 个且耗时预算 2 ms，先达到者停止启动新工作。它们是可调内部保护值，不是公共 ABI 或跨机器性能承诺；M2 Mono 测量后可调整并记录。已开始的同步回调不会被 2 ms 强制打断。
5. 队满返回 `Rejected/queue-full`，不可覆盖旧任务或静默丢弃。定时等待项也计入容量。到期等待使用统一内部优先结构，不为每个任务创建 Timer。
6. `CancellationToken`、Owner 关闭、epoch 失效、用户 TryCancel 都只取消尚未开始的工作；已经开始则返回不能取消，由回调执行完成。回调不得在执行中被强行卸载、Thread.Abort 或回滚。
7. 可选排队期限只约束开始前的等待，超时终态为 `Expired`。它不是回调超时中断。没有指定期限的合法长期 ModOwner 工作不因默认短超时被丢弃，但仍占容量。
8. 状态依次为 Queued → Running → Succeeded/Failed，或 Queued → Cancelled/Rejected/Expired；每项只有一个终态。Completion 始终完成为结构化结果，作者异常记录在 owner 日志并产生 Failed，不作为未观察 Task exception 传播。结果保存有限错误代码和摘要，不长期持有 exception/native 对象图。
9. Completion 使用异步 continuation 语义；文档明确 continuation 不保证主线程，需再次 Post。Mono 必须实测该实现可用。取消回调只设置状态，不在工作线程调用 Unity 或作者 cleanup。
10. 首版只执行同步 Action，不接收 async delegate、Coroutine 或任意 Task 工厂。SDK 对可识别的 async-void callback 给诊断；作者后台异步工作由作者管理，结果通过 Post 回主线程。禁止在主线程 `.Wait()`/`.Result` 等待同一 scheduler。

所有登记和终态计数沿现有 Owner/diagnostics 记录，正常热路径只更新有界计数。清理将队列、延迟项和回调引用移除；诊断中保留有限摘要，不保留已关闭 Owner 的强引用。

`Post` 与 NextTick 的内部类型、队列结构、锁和时钟实现属于可逆选择；行为、取消范围、错误结果与 scope 是公共契约，需要在 M2 gate 用真正 Mono 调用证明后再发布。

## RT-04：资源登记与命令服务

M2 的可选 Owner 资源服务允许作者登记 `IDisposable` 清理资源，返回可解除登记的 token。登记默认 ModOwner，也可绑定当前 SaveSession/World；逆登记顺序清理、异常隔离、幂等终态和失败资源重试与现有 `IModOwnerCleanupParticipant` 接口衔接。作者提供的清理必须幂等，成功清理的资源不再重试；失败资源保留明确的重试状态。Runtime 主线程执行清理；后台 Dispose 请求只排队，不能绕过关闭状态再登记。

解除登记只解除平台持有关系，是否同步 dispose 使用两个明确操作，不能让一个含糊方法在不同情形偷偷改变所有权。已被清理的 token 不能重新激活。平台可以保证释放自身引用，但不能保证作者静态字段/外部第三方 Hook 已清理。

命令服务归 Core，游戏控制台、Author session 和将来的外部调试前端均为 adapter，不依赖 Y-console 产品才能工作。首版选择：

- command canonical name 使用 `UniqueID/command`；大小写比较规则固定为 ordinal-ignore-case。简短 alias 可选、占用时整次注册失败，错误指出冲突 Owner；不取最后写入者。
- 注册记录包含说明、参数说明、scope/readiness requirement、同步 handler；成功返回 owner-bound IDisposable registration。重复名称、无效名称和 alias 冲突在执行 handler 前完成校验。
- 参数解析统一产生只读字符串序列；首版提供引号/转义、帮助和未知命令错误，不提供 shell、脚本求值或反射执行。调用者输入保持数据。
- `AnyContext` 命令用 ModOwner scope；world 命令在提交和真正执行前都检查同一 epoch 与 readiness。所有前端都经 scheduler，pipe 线程不执行 handler。
- handler 回应通过有界输出 sink，响应含 request ID 和 Owner；允许异步获取 scheduler completion，不允许命令无限积累输出。异常由统一 owner 日志说明，其他命令仍可执行。
- 重复 request ID 的重放/去重沿现有 Author session 协议处理，不在 command service 再建第二个会话系统。普通手动执行同一命令两次是两个调用。

验收必须证明命令帮助可用、真实第三方作者能从错误定位自己的 Mod、跨线程请求最终在主线程执行，且 Owner 停用/换档会取消队列中的旧命令。

## RT-05：依赖、辅助程序集和跨 Mod API

M3 实施所需格式及确定性策略见[包契约 P01–P07](platform-package-contracts.md)：新 manifest DependencyContractVersion=1 与旧 reader 分流，实际 AssemblyRef 闭包、可选依赖排序和 native provenance。此节拥有运行语义，P 文档拥有序列化/绑定细则；不要再造独立 manifest 解释器。

保留现有基于 Mod UniqueID 的加载依赖图。依赖可用性与仅影响顺序的约束分离；源码已支持的最小版本字段继续兼容。范围扩展采用包含下界、不包含上界的语义版本区间；不复用 load order 数字表达兼容性。预发行参与规则由目标 catalog/manifest 切片统一实现，SDK、Core、Doctor 必须同源验证。

PN-011 的新格式采用完整 SemVer 排序：预发行低于同核心版本的正式版，build metadata 不参与优先级；区间需显式表达预发行参与，不允许把 `1.0.0-beta` 截成 `1.0.0` 后满足正式版下界。当前 CoreVersion 的数字比较仍用于旧格式兼容，不能直接替换 reader 而静默改变旧包解释。旧的 optional 依赖降级/警告策略保留，新增区间规则同批进入 SDK/Core/Doctor 和实际加载测试。

开放生态首版选择纯 managed `netstandard2.0` shared contract DLL，不做动态结构代理。SDK 构建描述区分入口、private helper、shared contract；Core 在第一次 Assembly.LoadFrom 之前检查完整闭包，并生成诊断计划：

- platform/Unity/game 程序集由宿主拥有，禁止 Mod payload 覆盖；Shared contract 只含约定接口/DTO，不携带游戏对象或运行逻辑。
- 完整 CLR identity 相同且 bytes 相同的共享契约只加载一份；相同 identity 不同 bytes 在加载前拒绝受影响闭包，诊断指出双方 Owner、路径和期望身份。
- 同 simple name 不同版本在 Unity Mono 可能被不期望地统一，首版不承诺共存。只有独立绑定实验证明具体路径可可靠共存时才扩展；没有实验则拒绝相冲突包，不取“先加载的那个”。
- private helper 不是隔离加载上下文；同 AppDomain 的冲突规则同样适用。准确依赖图不等于 CLR assembly identity 自动隔离。
- provider 失败使必需 dependents 不进入 Entry；可选依赖缺失/不满足时给可操作诊断，消费者仍按其可选路径运行。来源刷新不能用磁盘上新版本替代进程中已经驻留的旧 DLL 满足依赖。

现有 `IModRegistry.GetApi<T>` 的准确 Type 匹配保持不变。对于普通第三方 provider，取到的是直接对象，平台无法撤销消费者已缓存的引用；关闭 registry 不等于代理一切跨 Mod 调用。作者契约必须明确：调用发生在主线程，provider 保持可用，返回对象寿命至 provider 关闭；关闭后消费者不得再调用。平台提供可用性通知/快照、关闭 dependent 的顺序、错误诊断，不宣称可隔离任意方法异常或自动转换接口。

现有内部 owner factory 继续用于平台能力 facade，关闭后拒绝调用并释放平台根。未来如真实协作场景需要自动 lease/proxy，再以独立可选契约设计，不能先改变旧 GetApi 返回类型或把动态代理作为开放生态先决条件。

## RT-06：GameBridge 吸收原生事实，按能力降级

GameBridge 拥有共享 native responsibility：加载/保存/标题/场景就绪事实、原生输入与帧接口、通用内容资产适配、共同冲突点和原生对象有效性。Core 负责 Owner、排序、预算、一般事务和公共 DTO。单产品玩法、补丁、动画、政策不因未来可能复用就迁入强制 GameBridge；物理归属仍按 PROJECT 判断。

每个新增 native capability 使用内部描述：稳定 capability ID、支持的原生签名/构建证据、安装状态、ready 条件、降级原因、可恢复性。公开只暴露准确结果/availability，不泄漏 Harmony MethodInfo、Unity 类型或反编译游戏类型。

一个可选能力失败时关闭该能力并诊断，保留无关 Mods 与平台服务。必需生命周期或保存边界无法证明时，与之依赖的危险操作不得继续假装成功；状态必须清楚区分 NotReady、Unsupported、Unavailable、Failed。重试只发生在已知 readiness 边界或有限 backoff，不在每帧做全程序集扫描。

现有 patch/demand/cleanup 机制继续复用。native owner lease 按准确原函数和 Owner 管理；清理只撤销自己的注册，不调用无界 UnpatchAll。公开 Advanced 是同进程受信代码，Owner 隔离不是沙箱，也不保证未知 Hook 或 native crash 可恢复。

代码更新、安装撤回、官方停用和关闭 Owner 都必须如实报告重启要求。CLR Assembly、未知静态状态和任意 native patch 不支持通用热卸载。内容 reload 是独立能力，需要自己的 generation/冲突/传播验收，不借此宣称 DLL reload。

## RT-07：保存事件与提交参与者在 M4 分开

当前 `SaveGamePrefix` 调用 `TryNotifySaveSaving`，handler failure 变化会使原生保存取消。`EventManager.CreateSlot` 默认 `failureThreshold=0`；目前只有 UpdateTicked 和 OneSecondUpdateTicked 显式启用连续失败熔断，SaveSaving 不会因此被 quarantine。这个现有正确边界必须保留：required 数据准备者不能在几次失败后因将来通用化熔断而从保存条件里消失。本设计没有认定当前存在该漏洞，也未复现存档损坏。

0.7.0 保留旧 SaveSaving/SaveSaved 与已有产品保存/恢复行为。M4 先按 [V-Save](../planning/platform-next/method-validation.md#v-save)和 [R4a 实验](../planning/platform-next/m4-experiments.md)比较官方扩展、已有同档容器、有界序列化适配和 sidecar；选定后端后再实现协调机制。共享的原生保存语义不要求所有后端都建立独立 prepared journal 和磁盘提升阶段。

PN-024 最先证伪已有同档容器在缺 Runtime 的原版冷加载、对话初始化和重存后是否保留数据；通过后才展开该方法的完整保存矩阵。所有候选均须解释一个已发现的原生窗口：研究 build 的 DataPersistenceManager 在文件保存成功后调用 AfterSaveData，外层 DolocAPI 随后还有 UI 操作；这些步骤抛异常可能让外层没有成功返回，但不能反证磁盘未写。当前 Hook 优先外层、缺失时回退 manager；这只是源码/原生方法体证据，尚未证明某次玩家损坏，也不授权在 SDK 改造或方法实验中顺手更换旧 Hook。

实验观测分开记录 `PrepareResult`、`NativeWriteOutcome`、`OuterCallOutcome` 及适用时的 `ParticipantCommitOutcome`。结果绑定同一会话、保存尝试与准确候选；原生磁盘结果至少区分已证明未提交、已证明提交、未知，外层 bool/异常不独自成为磁盘权威。同档载荷若经证明由同一个原生文件提交，就没有额外的 sidecar 提升，不能为凑齐状态机再造第二份权威。原生身份/外部谱系和永久 SaveIdentity 是否需要公开也等待后端及真实作者需求；session epoch 与正确档数据绑定仍必须保证。R4a 确定判据后，实施时同步 PROJECT/Hook 权威；这些实验字段不是新的 Public API。

1. **仅当所选后端含独立提交域时**，Core 的内部 commit participants 采用明确 prepare/commit/abort/health，不走普通 event handler quarantine。每次 native commit 绑定经验证的存档关联、当前 save epoch 和 transaction ID；required participant 未完成 durable prepare 就不能放行 native save。同档后端只做所需校验、快照冻结与原生候选装配，不默认落盘另一份 journal。
2. 参与者失败保留不健康状态直到明确成功重试或受控撤回；不得以计数清零、日志清理或缺少回调证明恢复。日志节流不改变 required 状态。
3. 独立 participant 的 native commit 成功后提升失败，不得声称 native save 已回滚；保留精确候选和 commit 证据用于恢复。已证明原生未提交则不提升候选；原生提交未知或外层后续异常时保留候选及原因，不能从异常推断 abort，也不能自动重放 ordinary gameplay mutation。同档方案以包含该载荷的原生文件结果为准，不因 SaveSaved 未送达再重放或另存一遍。
4. 普通保存观察通知负责告知作者阶段，其异常隔离只影响该 observer；不会偷偷参与交易投票。新 Data 服务是普通作者参加可靠持久化的入口，作者不必重写 journal。
5. 旧 SaveSaving 作为有界兼容 adapter 保留现有 fail-closed 效果及 `failureThreshold=0`，不能悄悄改为“异常无影响”，也不能套用高频事件的 quarantine。错误日志可以节流，但失败后仍继续调用并允许明确成功重试；不得自动恢复已失败的数据准备结果。确切旧包行为由专项负例和 Mono 实验决定是否需要更窄 adapter。
6. 不向原稳定接口添加新抽象成员；新观察/事务语义通过可选服务。旧 `SaveSlot` 参数保留，新的持久 Data 不能用该参数作为唯一身份。

共同顺序为：旧 SaveSaving 兼容回调（允许更新 Working，失败即拒绝）→所选后端校验、冻结并形成原生候选→native save→根据实际磁盘结果完成该后端需要的状态处理→对应观察通知。仅独立提交域在 native save 前加入 durable prepare，成功后加入独立 commit/abort 或保留待恢复；同档后端使用原生提交，不增加第二次持久化。候选冻结之后的新写入属于下一 Working revision，不能改变在途候选；SaveSaved 中写入同样属于下一 revision。保存尝试忙时拒绝嵌套/重入并给明确结果，不能覆盖在途候选。新准备观察回调如有写入用途，必须在冻结前运行，并在契约中明确它不具有旧 SaveSaving 的异常否决含义。

第 5 项把现有正确行为纳入推荐迁移设计，尚未更改当前行为。正式冻结前，GPT-6 在 M4 存档复盘门比较后端成本与作者负担，并检查旧 observer/产品实际语义、连续失败后再次保存、native false/exception、写入后 AfterSaveData/外层 UI 异常、文件替换后杀进程、owner/Runtime 缺失、复制/删除/槽位重用和普通不保存回档；独立提交域另检查 participant 提升失败、映射丢失及强 witness 歧义。同档方案成功不授权迁移 MoreEquipment 旧 sidecar、改变停用退物策略或删除旧 reader。若证据证明无法同时维持既有行为与数据一致性，提交有限兼容方案；不由 Sol 临时静默选择破坏性行为。

存档的 working/committed、原生成功关联、ordinary gameplay 与 owner/orphan recovery 的边界仍由 PROJECT 规范拥有。本文件不另立相冲突的保存规则。

## RT-08：从实现到平台能力的验收

| 边界 | 必需证明 | 不足以宣称成立的证据 |
| --- | --- | --- |
| M1 生命周期事实 | 独立第三方 Mod、实际 Mono、真实标题/新游戏/旧档/换档/退出；准确日志顺序和线程 | 仅源码断言事件方法存在 |
| M2 公共上下文 | native readiness对照、旧epoch无法操作新世界、快照跨线程只读、旧consumer与helper实现者运行 | DTO编译成功 |
| M2 scheduler/命令 | 真正工作线程提交、拥塞公平、队满/过期/取消、scope失效、真实帧源切换、handler异常不阻断别人 | .NET Task测试、计时器仿真 |
| M3 DLL/协作 | 两个仓库外工程+共享contract，缺依赖、不同bytes碰撞、provider Entry失败、缓存旧API在关闭后的规定行为 | 生成manifest、同项目内GetApi通过 |
| M4保存 | 隔离真实native提交、失败窗口与冷恢复、连续SaveSaving失败后重试、无保存回档、错档/复制/重用 | 伪造SaveSaved、写JSON成功 |
| 长期性能/稳定 | 当前精确包组合；cold/warm启动、0/5/25轻量Mod负载、连续标题idle后读档、世界长测、owner根/queue/内存/帧cost与干净退出 | 以前产品smoke、短启动不崩 |

性能的初始预算来自 RT-03 内部保护值，正式回归阈值使用同一机器/游戏 build 的 native-only、Runtime-only和标准Mod组合基线。正常配置的零监听/空队列路径应保持无新增逐帧对象积累；性能报告必须区分 retained roots、分配率与 native 内存。已有 [ISSUE-010](../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md) 长期 GC 风险仍按其专属证据管理，不由新能力 Unit 通过关闭。

上述证据写入任务自身 Update、现行 smoke/Issue/API 矩阵。此架构文件不记录任务 done、当前发布版本或运行 PASS 快照。
