# DTMAPI 下一阶段平台架构与决策

- Lifecycle: `accepted-design`；Implementation: `partial`，逐项验收见[任务状态](../planning/platform-next/status.md)。
- Role: 长期目标架构与跨领域决定的所有者；细分契约归下列领域架构，执行次序与任务状态见[实施入口](../planning/platform-next/README.md)。
- Source: 用户要求接管技术规划，独立复核本体，参考本地 SMAPI，并直接决定内部及可逆方案；既往审核是证据，不是设计前提。
- Current behavior: [PROJECT](../../PROJECT.md)、[公共 API 矩阵](../api/public-api-matrix.md)、当前源码和发布目录仍描述现行产品。本文接受未来设计，不声明新 API、新 SDK 或开放原生通道已经存在。
- Evidence: [初始代码基线](../reviews/code/2026/20260907-0001-platform-next-baseline.md)、[长期能力复核](../reviews/code/2026/20260907-0005-platform-maturity-baseline.md)、最新[基础设施与研究校准](../reviews/code/2026/20260908-0009-platform-plan-reconciliation.md)。

本设计保留 A01–A11 中仍成立的技术选择，并以 A12–A17 覆盖长期能力和交付。2026-09-08 修订校准现行工作流与研究反证，未缩减范围。A06 的反射规格继续有效，但不作为第一阶段出口。后续实现先按[路线](../planning/platform-next/roadmap.md)完成作者工具链和公共运行基础；新领域不能只从 A06 推演全平台。

2026-09-10 的 [SDK 构建职责决定](../reviews/code/2026/20260910-0004-sdk-msbuild-architecture.md)进一步修订作者构建：0.7.0 首发前由 PN-041 完成标准 MSBuild 单后端，具体边界归 [AB-01–09](platform-sdk-build.md)。这是工程解释与交付层调整，Runtime/GameBridge/Public API 骨架保留。

最新事实按 [0011](../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)：旧 SDK 从未公开发布，取消历史工具/通用工程迁移承诺；CSV 是已允许删除的第一方临时接口，不恢复。下文早期会话/模板设计的旧客户端语义是现有内部实现记录，不成为此次首发新增义务。实际 Mod/玩家包和必要恢复状态仍保全。

后续 [方法验证决定](../reviews/code/2026/20260910-0005-platform-method-validation.md)进一步区分产品不变量和内部候选：保存/内容/Host/UI/持久家族先比较标准或原生做法，再建设缺少的机制。具体实验归 [方法规格](../planning/platform-next/method-validation.md)。0.7.0 另以 PN-042 保障准确旧包与现有接入，不用 SDK 迁移替代兼容证明；不要求先完成全部未来实验。

## 产品目标与交付单位

第三方作者应能从仓库外的空目录创建项目，使用公开契约构建，安装到官方 Local 来源，正常启动游戏，定位一次实际错误，修改并重启验证，最后得到可供官方上传工具使用的包。这个作者流程是第一阶段交付单位。

成熟度用完整能力衡量：发现/依赖/诊断可靠，API 语义可理解，Mod 可组合，游戏升级影响可定位，源码与 SDK 能一起交付。公共类型数量、删行数、历史关卡数量不作为成功指标。

完整范围见 [C01–C33 能力地图](../planning/platform-next/capability-map.md)。第一阶段不是项目规划终点：公共运行基础→开放协作→数据内容→游戏领域→长期维护都有确定责任和进入条件。领域契约分别由[作者交付 AD 决策](platform-author-delivery.md)、[Runtime RT 决策](platform-runtime-contracts.md)、[数据内容 D 决策](platform-data-content.md)拥有；主架构不复制精确调度、提交或资源状态机。

近期不包含 Wiki、第一方玩法扩充、任意 DLL 热卸载、通用场景编辑器、全游戏对象模型、跨游戏适配、多人同步或自动上传。这些不阻止后续有需求时独立设计。

## A01：保留程序集骨架，按责任拆内部服务

```text
第三方 Mod -> Abstractions（已有契约 + 新可选能力）
Bootstrap（宿主组合入口）
  -> Core（发现/加载/owner/事件/配置/作者服务） -> Abstractions
  -> GameBridge（游戏生命周期、官方来源、原生适配） -> Core + Abstractions
  -> ModConfigMenu -> Abstractions
可选 Compatibility / QA / Content Host 在各自需要时参与
```

默认保留当前 mandatory Runtime DLL 组合，近期不增加部署单元。`DtmApiRuntime` 保留组合与流程协调；报告投影、发现与依赖计划、加载事务、owner 清理逐步提取为内部服务。平台层不反向引用 GameBridge。此选择不是永久禁止新程序集；只有明确的依赖/宿主收益和部署、Mono、兼容成本证据才有界复盘。物理目录不代表实际编译归属，应核对 csproj 的 Compile 项；SharedNative 仍遵守 PROJECT 的真实消费者要求，不为搬家虚构第二消费者。

保留已实现的 Entry 事务、失败回收、程序集已加载后的重启门、事件快照和异常隔离、零需求快速路径。先移动责任，再单独改变行为，避免把全面搬家与作者新能力放在一个改动中。

通用反射属于 Core 的 BCL 基础设施；游戏成员名、Unity 对象有效性、版本 fallback 和玩法判断属于 GameBridge 或实际 Mod。诊断和 Manager 从运行状态取投影，不能成为第二份加载事实。

## A02：现有作者 ABI 不加必需成员

保持 `IDtmHelper` 的已有成员和 `DtmMod.Entry(IDtmHelper)` 不变。新增独立可选能力接口，约定形状：

```csharp
public interface IDtmHelperServices
{
    TService? GetService<TService>() where TService : class;
}
```

Core 的 `DtmHelper` 同时实现此接口。Abstractions 提供命名扩展，例如 `helper.GetReflection()`，通过该接口取到 `IReflectionHelper`。没有服务时 `GetService` 返回 null；必需能力扩展抛出带所需 Runtime/API target 的 `NotSupportedException`。类型解析使用确切契约类型，不用名字猜测。

这是固定的平台服务入口，不是任意依赖注入容器。只有 Core 注册明确支持的服务；作者注册跨 Mod API 仍用现有 ModRegistry。所有服务绑定 Mod owner。已停用 owner 不能继续使用会访问对象或注册资源的能力。

不建立 `IDtmHelper2/3` 版本链，不用动态接口默认实现解决 Unity Mono 兼容问题。ABI 验收同时覆盖旧消费者和旧实现者：旧 `IDtmHelper` 实现 DLL 必须能加载、实例化并被旧消费者调用。现有 MemberRef 检查继续保留。

## A03：编译目标、Runtime 版本、会话协议各自演进

1. SDK target 是作者可编译的 API 载荷与引用集。
2. Runtime release 是实际执行版本；最低版本是包的执行要求。
3. Session protocol 是双方通信能力；不能拿 SDK 编译目标做 Runtime 的精确版本条件。

旧 `author-sdk/compatibility/0.5.5` 载荷和 hash 原样保留。新 SDK 引入受版本控制的 target catalog，各目标独立绑定兼容载荷、TFM、contract hash 和最低执行版本。现有项目仍明确选择旧目标；新模板默认新目标；迁移显式进行。

源码的目标目录为 [target-catalog.json](../../author-sdk/target-catalog.json)，使用 available/planned 状态。只有完成的 available 目标可作为默认值或构建/打包输入；新模板切换新目标要等其载荷交付，不能在骨架阶段提前切换。共享 reader 链接进现有程序集，发行检查绑定同一目录与各自冻结契约。新 writer 严格验证最低 Runtime；历史 SDK 0.1.0 / target 0.5.5 的 Strict/ContentPack 低 floor 包保留读取及恢复兼容，Advanced 继续执行原 policy 要求。实际使用见 [SDK 说明](../../author-sdk/README.md)。

公开版本安排由[路线](../planning/platform-next/roadmap.md#可发布版本)拥有，准确 Runtime/SDK/target 由版本源、catalog 与候选页拥有，不在本架构保留早期 0.2.0/M2 的重复发布坐标。frozen/available 载荷保持原字节，新增 API 使用后续 target；换 SDK 构建后端不重写 API。保留 AssemblyVersion 兼容身份，只有真实 ABI 反证才另立 breaking 任务，不随 release 字符串同步改它。

新 API 的交付必须包含：声明、Core 实现、新 target 载荷、模板、validator/build/pack、Runtime/Doctor 的一致读取、旧与新作者 DLL 测试。不允许仅把当前 Runtime DLL 替换进名为旧目标的 payload。

## A04：作者会话采用显式协议协商

新 descriptor/envelope 使用 schema 2，独立标识协议 major/minor、编译目标和请求能力；实际宿主版本由 Host 响应提供，不写入离线 descriptor。首版协议 major=1；不支持的 major 拒绝，不认识的可选能力返回 unsupported。必需能力缺失不能默默降级。成功后将协商结果绑定到 session/token/game root，后续 request/response 都按同一结果校验。

旧 schema 1 保留有界兼容适配：现有 SDK 的 `0.5.5` wire version 是已知 legacy 值，可映射到当前支持的旧协议；每个会话保存该 wire 值。后续请求和旧客户端响应仍使用该会话约定的 wire 值，实际 Runtime 版本单独进入状态信息。不能只删 descriptor 的版本比较，让 request 或 response 随后失败；也不能接受任意版本字符串。

保持 token、时效、一次性消费、规范 game root、ACL/pipe 身份和消息大小验证。新 CLI 应查询实际宿主能力；未启动时 prepare 生成能力请求，不从 Abstractions 版本猜宿主。兼容范围通过真实 SDK + Core 组合测试固定，不要求用户安装旧 Runtime。

schema 2 的首版 wire 行为固定如下，PN-002 实施这些规则而不是重新选协议：

| 步骤 / 字段 | 确定行为 |
| --- | --- |
| 离线 prepare | descriptor 写 `schemaVersion=2`、`protocolMajor=1`、`minimumMinor=0`、`maximumMinor=0`、`apiTarget`、`minimumRuntimeVersion`、`requiredCapabilities`、`optionalCapabilities`；以及沿用的 sessionId/token/gameRoot/pipeName/createdAtUtc/expiresAtUtc。没有 hostVersion 字段，客户端此时不知道实际宿主 |
| Host 消费 descriptor | 验证现有凭据/路径/时效后计算协议交集；major 相同，minor 取双方 maximumMinor 较小值，并且不低于双方 minimumMinor 较大值；无交集拒绝；最低执行版本不满足也拒绝 |
| 首个请求 | schema 2 第一条必须为认证的 `hello`，携带已建立的 sessionId/requestId/token 及请求的协议范围；与 descriptor 不一致拒绝。Host 返回选中协议、真实 hostVersion、apiTarget、acceptedCapabilities、unsupportedOptionalCapabilities |
| 能力名称 | 首版固定 `get-source-snapshot/1`、`reload-content/1`，映射到现有同名业务操作；后者只刷新实际支持的非代码内容，DLL 变更仍 restart-required。新操作必须另加能力名，不能默认继承授权 |
| 能力协商 | required 必须全部支持；optional 不支持时列入响应但不启用；未知必需能力拒绝。业务请求只有在 hello 成功且对应能力被接受后才执行 |
| 未知输入 | 忽略未知的可选 JSON 字段；拒绝重复关键字段、未知 major、未知操作、未协商能力、后续切换 schema/protocol/session/root/wire 身份；未知字段不会产生新能力 |
| 请求与响应 | 沿用现有 requestId、token/pipe 安全和重放规则。每个响应绑定 requestId/sessionId/选定协议；错误响应仍不暴露 token；hello 重试仅允许新的 requestId 和完全相同的协商输入，返回同一结果 |
| 旧 schema 1 | 不强制 hello，进入已知旧协议适配；旧 wire runtime 值与当前 hostVersion 分离。新客户端不默默降级写 schema 1；旧项目的编译 target 不因此被改变 |
| 无响应与不兼容诊断 | 只有认证后的 Host 明确报告不兼容，或可核验的本机安装/启动诊断证明该宿主不支持 schema 2，才报告 `upgrade-required`。没有 listener 或连接超时分别报告 `host-unavailable` / `handshake-timeout`，提示启动游戏、查看日志和核对版本；不能由超时推断宿主版本，也不虚构 hostVersion |

以下是去掉公共凭据字段的形状示例，不是可直接使用的 descriptor，也不包含真实 token：

```json
{"schemaVersion":2,"protocolMajor":1,"minimumMinor":0,"maximumMinor":0,"apiTarget":"0.5.5","minimumRuntimeVersion":"0.6.1","requiredCapabilities":["get-source-snapshot/1"],"optionalCapabilities":["reload-content/1"]}
```

```json
{"schemaVersion":2,"operation":"hello","protocolMajor":1,"protocolMinor":0,"hostVersion":"0.7.0","apiTarget":"0.5.5","acceptedCapabilities":["get-source-snapshot/1","reload-content/1"],"unsupportedOptionalCapabilities":[]}
```

现有内部旧客户端可走 schema1 adapter，但旧 SDK 没有公开用户，不增其长期支持矩阵。首发新 SDK/新 Host 使用 hello/schema2；面对不兼容旧 Host 不静默降级，按真实证据区分升级要求与不可达。旧编译目标不决定会话版本，已部署状态恢复独立保留。示例不替代版本权威，见[会话协议](../../author-sdk/SESSION-PROTOCOL.md)，不能推导 0.6.1 已有新协议。

## A05：开发安装只进入官方 Local MODS

新增开发安装路径使用官方 Local MODS；继续尊重官方启用状态。历史 `<game>/Mods` 只保留 recover/withdraw 等旧部署恢复，不恢复其玩家发现。SDK 不写 Steam Workshop 缓存，不自动为玩家启用 Mod。

SDK 的 install/update/withdraw/recover 使用同一事务服务和现有 package inventory / receipt 机制。部署后报告官方 folder、Local 标识、UniqueID、磁盘版本、是否被官方启用及是否需要重启；不能把磁盘文件存在等同于已经加载。

事务约束：

- 从本地设置/显式参数/既有解析器获得 game 与 MODS 路径，不内置机器路径。
- 安装前验证完整包、来源冲突和目标归属；未知文件/非本工具拥有的同名目录拒绝覆盖。
- 同卷 staging，提交前后有可恢复的清单；文件移动失败可重试且不假报成功。
- CodeMod 安装/更新/撤回时若对应游戏进程正在运行，拒绝修改并提示退出重试；不尝试覆盖已加载 DLL。
- 配置、用户数据、存档 sidecar 不属于可替换包目录。withdraw 不删除玩家持久状态。
- 与已有 Local/Workshop 同 ID 的选择策略保持一致，复用现有来源仲裁，不发明第三套优先级。

复用现有官方 Local 安装事务脚本/fixture中可提取的能力；先验证归属和参数边界，不能直接把历史 game/Mods 的字符串替换成 MODS。

## A06：通用反射 V1 的确定边界

平台服务 `IReflectionHelper` 提供实例/静态的字段、普通属性、方法访问。以下为 PN-021 的 V1 契约签名（namespace `DTMAPI.Abstractions`）；实施/候选证据按任务状态路由，是否进入普通 SDK 或已发布仍查 target 权威：

```csharp
public interface IReflectionHelper
{
    IReflectedField<T> GetField<T>(object target, string name);
    IReflectedField<T> GetStaticField<T>(Type type, string name);
    bool TryGetField<T>(object target, string name, out IReflectedField<T>? field);
    bool TryGetStaticField<T>(Type type, string name, out IReflectedField<T>? field);
    IReflectedProperty<T> GetProperty<T>(object target, string name);
    IReflectedProperty<T> GetStaticProperty<T>(Type type, string name);
    bool TryGetProperty<T>(object target, string name, out IReflectedProperty<T>? property);
    bool TryGetStaticProperty<T>(Type type, string name, out IReflectedProperty<T>? property);
    IReflectedMethod GetMethod(object target, string name, Type[] parameterTypes);
    IReflectedMethod GetStaticMethod(Type type, string name, Type[] parameterTypes);
    bool TryGetMethod(object target, string name, Type[] parameterTypes, out IReflectedMethod? method);
    bool TryGetStaticMethod(Type type, string name, Type[] parameterTypes, out IReflectedMethod? method);
}
public interface IReflectedField<T> : IDisposable
{
    bool CanWrite { get; }
    T GetValue();
    void SetValue(T value);
}
public interface IReflectedProperty<T> : IDisposable
{
    bool CanWrite { get; }
    T GetValue();
    void SetValue(T value);
}
public interface IReflectedMethod : IDisposable
{
    Type ReturnType { get; }
    T Invoke<T>(params object?[] arguments);
    void Invoke(params object?[] arguments);
}
public static class DtmHelperExtensions
{
    // 实际实现通过 IDtmHelperServices 取得 IReflectionHelper。
    public static IReflectionHelper GetReflection(this IDtmHelper helper);
}
```

静态类中的方法行是契约签名简写，落地须有方法体。`Try` 为 false 时 out 必须为 null。`Invoke` 非泛型形式只接受返回 void 的方法；`Invoke<T>` 在调用前验证声明返回类型可赋给 T，不通过就不执行目标。字段/属性在绑定时验证声明类型可赋给 T；每次写入再验证实际值可赋给成员类型，不做隐式转换。只有 setter 的属性不在 V1 支持范围。readonly/const 可以绑定读取，CanWrite=false。

解析规则：

| 情况 | 决定 |
| --- | --- |
| 继承和私有成员 | 从请求类型逐级搜索基类；最派生层中符合确切签名的声明优先；静态与实例分开 |
| 重载 | 完整参数类型、成员种类、静态性参与匹配；不按参数数量或实参隐式转换猜方法 |
| 类型转换 | 不做数值/字符串/枚举隐式转换；读值必须可赋给 T；写值必须可赋给成员类型；null 仅用于允许 null 的类型 |
| 缺失与错误 | `Try` 仅对成员不存在返回 false；合法 null 是成功结果；歧义、错误目标、类型错误、不可写、调用异常各有明确诊断 |
| 调用异常 | 包装时保留原异常 InnerException/原始堆栈；不吞成空值或成功 |
| 不支持 | V1 拒绝泛型方法、ref/out、索引器、const/readonly 写入、值类型实例修改，输出明确 unsupported；不隐式展开 params/可选参数 |
| 字符串类型解析 | V1 不公开扫描/加载任意程序集的 helper；内部需要时仅在已加载、明确指定的程序集身份中查找 |
| 缓存 | key 含实际 Type 身份、签名、种类和搜索范围；不只用 FullName；负缓存与 AssemblyLoad generation 一致；容量有界 |
| 生命周期 | 全局缓存只存元数据；绑定目标仅由短生命周期 wrapper 持有，owner 停用后拒绝调用并释放引用；缓存注册不能反向永久持有 wrapper |
| 线程 | 查找元数据可并发；目标访问同步发生，不偷偷切线程；Unity 访问由原生适配或主线程调度负责 |

先用普通反射完成正确性。Expression/委托生成只作为后来有实测收益的优化。至少迁移两个低风险只读内部调用点检验重复消除，但不一轮替换所有 Harmony/Save/FrameDriver 反射。

字段能写入不代表游戏行为合法。稳定玩法接口继续由真实 native responsibility 决定，不把反射包装器变成自动适配游戏升级的承诺。

错误使用 BCL 异常，不新增一套公共错误类层级：

| 条件 | V1 结果 |
| --- | --- |
| null target/type/参数数组或数组含 null 类型；空名字 | ArgumentNullException / ArgumentException，绑定前验证 |
| 请求种类与精确签名不存在 | MissingFieldException / MissingMemberException（属性）/ MissingMethodException；Try 只在这一类返回 false |
| 同一搜索层有多个精确候选 | AmbiguousMatchException，列出候选签名 |
| 字段/属性 T 与声明类型不兼容；Invoke<T> 与声明返回类型不兼容 | InvalidCastException，在绑定/调用前验证，不执行 getter 或目标方法 |
| 写入值、调用参数数量/类型不适配 | ArgumentException，调用前拒绝，不自动转换 |
| 泛型方法、ref/out、索引器、setter-only、值类型实例修改、readonly/const SetValue、非 void 方法走 void Invoke | NotSupportedException；先识别不支持的形状，不能过滤后伪报 Missing |
| wrapper Dispose 或 owner/Runtime 生命周期关闭 | ObjectDisposedException；禁止新的目标调用 |
| 目标 getter/setter/method 抛异常 | TargetInvocationException，InnerException 保留目标原异常与堆栈；不展开后丢失原始原因 |
| CLR 本身拒绝访问 | 保留 MemberAccessException 等具体访问异常，不吞为 Missing |

生命周期接缝使用目前实际存在的 `ensureOwnerActive` 回调及 `RegisterModOwnerCleanupParticipant`，不是假设当前已有统一 token 类。公共包装器绑定 Mod 生命周期；两个内部只读消费者绑定 Runtime 生命周期，不能伪造产品 owner。服务按 owner 维护包装器的弱注册、定期清理死亡注册；包装器自己强持有目标。owner cleanup 必须遍历仍存活的包装器并清空其目标，即使之后没有再次调用也能回收；metadata cache 不引用包装器。Dispose 幂等，立即释放本包装器的目标。

关闭与调用通过同一生命周期锁确定先后：关闭之后不能开始新调用；已开始的调用使用局部强引用正常结束，不被强行中断，也不回滚其副作用。元数据查询线程安全不等于 Unity 对象调用线程安全。Core 不自动识别 Unity 的“已销毁但托管引用非 null”；窄原生适配在交给内核之前检查这种游戏对象状态。

## A07：代码冷启动，平台资源有生命周期

任意 DLL 的更新要求重启；不承诺程序集卸载、未知 Harmony Hook 热卸载或回滚未知静态副作用。现有受管理 owner 的事件、输入、命令、待执行任务等可停止和释放，界面准确显示磁盘新版本与进程仍驻留版本。

Entry 只保证可以注册和订阅；全部 Mod 初始化完成与当前存档/世界就绪是不同边界。跨 Mod API 的正常消费发生在初始化完成后。后续内容截获若需要更早 Entry，先验证游戏真实初始化时序，再增加事件，不能单纯推迟全部加载以便 Entry 能拿到玩家对象。

主线程调度复用当前 frame pump 与 owner 取消机制，提供 owner、save-session、world 作用域，按 [Runtime 契约](platform-runtime-contracts.md)处理 epoch；排队上限和每帧执行预算明确，溢出返回结果，不同步等待自己造成死锁。命令服务与执行界面分开，Y 控制台是可选消费者，不变成 mandatory command engine。

## A08：面向第三方开放 Advanced，自助生成构建证据

本节的 Native V1 是已实施的第一片；2026-09-10 普通 Unity 泛型反例引出的版本化增量统一由 [P08](platform-package-contracts.md#p08可验证的原生泛型-v2)拥有。它保留 V1/legacy 读取和冻结 API，只扩签名表达与三端校验；不要把下述初始版本号当成永久禁止泛型的公共契约。

新格式的字段、摘要依赖、共享 reader 和通用本机引用生成细则由[包契约 P01–P07](platform-package-contracts.md)拥有；采用本页方向，不把本页当成已实现准入。

已选择开放方向，不再把维护者逐个登记产品作为未来第三方开发条件。Strict 继续代表没有直接原生程序集引用的默认模式；它不是安全沙箱，通用 BCL 反射也不构成沙箱。Advanced 表达作者主动使用原生引用与承担游戏版本敏感性。

迁移为加法：

1. 现有 receipt/policy 的 Advanced 包继续进入原来的 schema 验证，不要求全部第一方产品同时迁移。
2. 新作者 schema 3 选择 Advanced；Runtime manifest 保留 `CodeModKind=Advanced`，新增 `NativeContractVersion=1` 显式选择开放契约。明确 Advanced 但缺 NativeContractVersion 的包仍验证旧 receipt/policy；只有省略 CodeModKind 的输入可走既有 legacy 分类。声明未知 NativeContractVersion 直接拒绝，验证失败绝不回退。
3. SDK 生成 `dtmapi-native-build.json`（schema 1），绑定 UniqueID、manifest/入口/依赖清单 hash、API target、TFM、本地 game build/引用身份/长度/hash、声明的 Harmony owner。它是可复核的构建来源证据，不是维护者签名或可信资格证。
4. Runtime 与 Doctor 共享纯数据分类/验证逻辑，接受任意合法作者 ID 的新契约；保留包完整性、依赖冲突和最低执行版本校验。游戏 build 不同先报告未验证兼容；确切所需程序集/成员能力缺失才阻止对应功能。SDK 不允许打包官方游戏/Unity/BepInEx/Harmony DLL。
5. Unity facade/reference 冲突通过本地、可复现的编译引用处理解决；禁止继续为每个作者手工维护游戏 stub。是否能正确生成该引用面是一项有界技术验证，不能假定现有产品裁剪面就是通用答案。

该迁移由 PN-010 完成，并在其实施时同步 PROJECT、API/SDK 文档和现有机器校验。当前生成的第一方准入注册表继续描述现行包，不手工修改；计划本身不让新包被当前 Runtime 接受。

## A09：共享契约和辅助 DLL 先于结构代理

跨 Mod API 近期采用显式、纯托管 `netstandard2.0` Contract DLL 和版本化接口。SDK 提供显式引用输入和包内依赖清单，区分共享契约与私有依赖，Runtime 在任何 Entry 之前完成程序集身份/字节冲突检查。

同一契约完整程序集身份且字节一致可统一加载；相同身份不同字节拒绝并报告双方来源。不同不兼容版本不能静默选第一份；在当前 Unity Mono 单 AppDomain 无可靠隔离时报告依赖冲突。不得承诺每 Mod 都能同时装任意版本同名依赖。

现有 GetApi 的确切类型身份仍有意义；两个独立作者工程使用同一契约程序集验收。公共服务 capability 注册不能替代 ModRegistry。复制接口源码后的鸭子类型/结构代理、复杂 DTO/事件/泛型映射后置。

## A10：Data 与 Content 是不同能力

Data 的配置/全局数据可即时持久化，save-bound data 遵守 PROJECT 的原生提交语义；未保存状态不能变成正式数据。PN-024 先比较原生同档容器/受限适配和外部 sidecar，尤其验证当前 dialogue variableStorage 在缺 Runtime、对话与重存后的保留规则；没有官方通用字典不足以直接选侧车。按选中方法证明绑定正确存档、native 提交、复制/回滚和冷恢复，再开放 SaveData；外部 journal/永久公共身份并非所有后端的必需机制。既有产品 sidecar 不自动迁移，详细语义归[数据架构](platform-data-content.md)。

ModContent 负责 Mod 自有文件的安全相对路径、JSON/字节读取和后续资源加载；数据写入独立可写目录。GameContent 负责游戏资产访问、修改顺序、冲突、失效与活对象传播。两者从第一天分开，不把现有官方内容索引接口改成语义混杂的万能 Content。

首个 GameContent 交付只选一个可核验原生 owner 的数据表和一种图像：逻辑资产 ID、原始输入、确定性编辑顺序、修改归属、冲突说明、错误回退、重复失效不叠加、资源释放责任全部闭合。明确“下次读取/换场景/读档/重启生效”，不声称 cache invalidation 能更新所有现存对象。

## A11：测试以可交付行为分层

保留现有 Unit、QA、Doctor、SDK 与 ABI 资产；新增跨 SDK/Core 的组合测试。纯元数据测试不代替 Mono 调用；build 不代替游戏生命周期验证。把稳定的行为回归与发布快照/历史结构断言分开，避免一个过时 hash 阻断全部后续行为测试。

每个任务按改变的行为选当前独立测试图与 focus，未变的前置证据按产品流程复用；阶段收口验证对应真实旅程，不自动追加全部 C# 套件。完整 Release 包/安装器矩阵只在触及其边界或存在具体集成风险时执行。SDK prepare 复用、普通单次 pack、记录同步沿现有脚本，不再维护平台专用的重复验证流程。不得把本轮未跑的游戏测试或失败套件拼成绿色基线。

性能比较空 Runtime、不同 Mod 数、冷/热反射、正常与诊断模式、反复换档后的存活对象/订阅。已有零监听快速路径应保持。原生访问测试使用可处置 fixture 或无保存模式，不为纯反射任务运行无关存档写入测试。

## SMAPI 依据与不照搬的部分

本地参考根为 `E:/Python_project/SMAPIlearning/SMAPI`；历史目录实际为 `E:/Python_project/SMAPIlearning/SMAPI_version_study`。用户输入的历史路径按本机目录解析到后者。源码快照 HEAD `5689c8d6aeecf54f670559ffaaed6684a5febc25`，describe `4.5.2-54-g5689c8d6`，不是“已发布 4.5.2 的原样源码”。许可为 LGPL v3；这里只提炼设计与行为，DTMAPI 独立实现。

| 本地 SMAPI 证据（路径相对其根） | 采用的决定 / 保留的差异 |
| --- | --- |
| `src/SMAPI/IModHelper.cs`；历史 1.0 `ModHelper.cs`、1.2 `IModHelper.cs` | 模块化作者能力；DTMAPI 用可选服务保护已存在 ABI |
| `Framework/Reflection/Reflector.cs`，在 `src/SMAPI` 下 | 学习包装和基类遍历；不照搬 FullName+名字缓存及按名字选方法，DTMAPI 使用类型身份与精确签名 |
| `Framework/ModLoading/AssemblyLoader.cs`、`Mod.cs` | 冷启动和可释放注册不等于通用 DLL 热卸载；不以 Dispose 作为唯一保存提交点 |
| 历史 `SMAPI-3.0/docs/release-notes.md` | 早期 Entry 为内容截获服务；世界就绪另行表达 |
| `Framework/ModHelpers/ModRegistryHelper.cs` | 结构代理有成本，先交付共享契约 DLL |
| `IDataHelper.cs`、`Framework/ModHelpers/DataHelper.cs` | 存档数据跟随游戏保存；Doloc 不照搬 Stardew CustomData 容器 |
| `docs/release-notes-archived.md` 的 3.14.0 记录 | GameContent/ModContent 分离及事件式内容修改源于冲突、优先级、归属和损坏问题；直接采用职责分离 |
| `src/SMAPI.ModBuildConfig/build/smapi.targets` | 学自动构建/部署/调试/打包体验；DTMAPI 使用官方 MODS，不照搬 game/Mods 或独立 SMAPI EXE 启动 |
| `src/SMAPI/SMAPI.csproj` | SMAPI 当前 net6.0/MonoGame；DTMAPI 保持 Unity Mono/netstandard2.0，不移植 XNB/xTile 内容实现 |

## 决策变更条件

上述决定及下列 A12–A17 形成目标设计，后续执行者无需重新举行全仓架构讨论。只有具体反证（宿主能力不成立、不可兼容的公开使用方式、测试证明数据语义不同）才重新打开对应决定；记录事实、影响和最小替代方案，不重启全平台设计。公共冻结和原生不确定点按路线的 R1–R6 做有界复盘。

对外发布、删除现有玩家数据或改变普通玩家保存语义不属于本轮规划交付。当前计划中的可逆实现、测试和文档维护已在用户授权范围内。

## A12：先证明作者工具链，再扩张公共面

M1 已按当时 available target 和统一 BuildPlan 证明有界作者回路，历史结果保留。当前 0.7.0 SDK 目标沿 AD-01 / AB-01–09：CLI、实际 IDE、CI 使用标准 MSBuild 工程，DTMAPI 消费已求值结果并验证最终交付；不保留 IDE 委托自有编译器的正式链，也不接受“IDE 绿、pack 另取程序集”的双轨语义。

SDK160 必须由真实引用、编译语义和依赖闭包判定，不能把源码库名子串当违规。PN-041 保留这些有效检查和冻结引用，用标准 compilation/analyzer、NuGet 与最终暂存产物完成统一；普通工程不再逐项添加私有语法。配置正确性等已完成 M1 工作不因本次架构调整重做。

Debug/PDB 是可交付产物的一部分，实际 Mono 堆栈行号和断点 attach 分别验证。如果原生运行器不开放断点，先记录调查结论并在 R1 明确受支持方案；不能以 PortablePdb 文件存在宣称完整调试。详细契约见[作者交付](platform-author-delivery.md)。

## A13：生命周期和资源寿命是公共平台的基础

Core 拥有三种寿命、执行队列、命令注册、结果和错误隔离；GameBridge 只把可证明的原生时点投影为 capability 和上下文。SaveLoaded 不能直接重命名为世界全就绪，save epoch 不能充当持久 SaveIdentity。借用对象、资源 lease、内容回调和排队任务都必须有明确失效条件。

公共入口维持 A02 可选服务；内部 owner coordinator、Entry 事务、事件快照和当前单一帧驱动继续使用。新增服务不另建并行加载状态或定时器游戏循环。Runtime 实施依据[领域 RT 决策](platform-runtime-contracts.md)。

## A14：内容激活与游戏生效是独立平台承诺

Core 负责 ModContent 安全路径、逻辑资源归属、内容请求组合与 Host/Pack 依赖；GameBridge 负责官方 Mod 合并后读取、原生资产缓存、线程和存活对象传播。Host 负责 schema、条件解释及具体玩法。新 Pack 必须声明单一 Host 和版本范围；发现/加载/Host 接受/激活要能区分，不能用 loaded 替代实际生效。

先一张数据表和一种图像证明排序、冲突、失败回退、失效与撤销，再逐家族扩充。全量 Content Patcher DSL、地图编辑器和全游戏实体注册器不是首片前提。保存绑定内容依赖 M4 Data，普通只读或无状态内容不依赖它。见[数据与内容 D 决策](platform-data-content.md)。

## A15：领域扩展从原生责任和独立作者需求出发

GameBridge 吸收真正 SharedNative 的成员名、对象有效性、线程、时间边界和版本退化；只属于一个产品的 Hook/玩法仍由 ProductNative/ContentOwner 承担。Abstractions 暴露窄查询 DTO、结果、稳定逻辑身份和可释放句柄，不暴露原生对象图。通用反射是作者高级工具，不能用来免除稳定 API 的游戏适配责任。

首个持久实体 Host 要完整覆盖创建、交互、销毁、保存、读回、升级、依赖缺失和显式恢复。通过第二个消费者检验共性后才抽取共享实体机制；现有 Frozen/blocked 外壳不因路线覆盖同领域而自动转正。

## A16：成熟度、任务完成和发布分别记录

任务 done 只说明该任务的列明交付和测试完成。涉及作者/Mono/游戏行为的能力还须通过对应 E 场景；未运行、跳过和可用性未知不能折算 PASS。阶段产品证明由既有 Update/Smoke/API owner 承载，status 只路由。发布仍由 Catalog/实际发行 Update 管理，规划编号或新版本字符串不产生发行事实。

M1 起维护外部作者样例、可复现包、支持诊断、老 ABI corpus 和性能基线；M6 再确定稳定子集与支持窗口。候选策略为稳定 API 同 major 保持二进制/源码兼容，Experimental 明示变化，破坏性迁移有预告及迁移工具；正式支持期限在 R6 结合维护资源决定。不开自动下载执行或强制在线依赖。

作者不反馈时的更新/弃用沿 AD-09：区分已知消费者、检查范围内未发现与未知；无回复不等于无人使用，也不形成永久阻止演进的条件。替代路径、旧二进制和预览移除的证据支持明确公告的版本/日期边界；研究中的具体天数不自动成为支持承诺。

## A17：裁剪围绕单一责任和可观测结果

保留 owner 事务、失败关闭、保存成功边界、证据路由等已承担真实不变量的机制。优先剪掉重复版本常量、重复分类 reader、平行状态投影、过时默认-suite 历史快照与不再有回退用途的开关；每项必须先列出现存消费者、等价行为和撤销方式。

内部实现可删除或替换，公开入口可保留薄适配层，这两项分别判断。Frozen、缺反馈或当前测试未调用都不能单独授权删除已发布 ABI；也不要求为了薄兼容入口永久保留整套旧内部实现。PN-014/config-correctness 优先删掉只有配置写入使用且会改坏 JSON 的手写格式化，不以裁剪名义改变配置迁移公共语义。

`DtmApiRuntime` 的报告投影、发现/依赖计划、Entry 编排、作者会话协调按 PN-014 分片抽出。GameBridge 按原生责任域和 capability 分组，不能一边移动一边改 Hook 语义。终点是新能力有清楚的 owner 和局部回归，不是一次性重写、清空历史资料或达到删行指标。
