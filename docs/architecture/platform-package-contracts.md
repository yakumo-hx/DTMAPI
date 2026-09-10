# 开放生态包、依赖与原生引用契约

- Lifecycle: accepted-design
- Role: A08/A09、AD-07、RT-05 的 M3 格式细则；实现前的推荐方案，R3 只据反证修订。当前 reader 是否已支持由 SDK schema、源码与任务 Update 证明。
- Parents: [主架构](platform-next.md)、[作者交付](platform-author-delivery.md)、[Runtime](platform-runtime-contracts.md)。
- Execution: 当前标准构建迁移见 [execution-sdk-msbuild](../planning/platform-next/execution-sdk-msbuild.md)；旧 M3 / SDK 修复规格按需查 execution-next / execution-sdk。版本安排查 [roadmap](../planning/platform-next/roadmap.md)，本页不拥有版本进度。

## P01：沿现有包协议扩展

复用 manifest.json、dtmapi.author.json、dtmapi-package.json、现有 package inventory、SDK report 和 deployment journal。公共格式由 SDK/Runtime/Doctor 共用的纯数据模型解释；实现放现有 Authoring.Contracts / src/Shared 对应边界，不添加第六个强制 Runtime DLL。

M3 author schema1–3 属于未发布内部 SDK；PN-041 首发采用 AB-02 的单一 schema4，只转换现用内部输入，不承诺旧工程通用迁移。作者工程格式不决定玩家包格式。现有包 manifest 的 DependencyContractVersion=1/缺省 reader 及 marker 规则保留，未知版本/重复关键字段/不一致仍拒绝，不失败后降级或新造凭证。

新模式包内有一份由 SDK 生成的 dtmapi-dependencies.json；它是现有 package inventory 的依赖描述，不是部署收据或身份授权。它有 schemaVersion、ownerId、apiTarget、entryPath、dependencies、assemblies。manifest、依赖描述、native provenance 都作为既有包 inventory 的普通文件计算摘要；描述不能包含自身 hash，避免循环摘要。

manifest 的 Dependencies 仍是 Mod 身份依赖权威；新区间记录在其中，依赖文件仅保存规范化投影，两者不一致拒绝。新增内部字段不向稳定 IManifestDependency 增加抽象成员。SDK schema/契约 DTO 是实现后的字段权威，文档不再复制生成 JSON。

## P02：Mod 依赖及版本

新 Dependencies 项沿 UniqueID、Required，增加 VersionRange 对象，字段为：

| 字段 | 规则 |
| --- | --- |
| minimumInclusive | 必填，完整 SemVer 2 三段数字，含可选 prerelease/build |
| maximumExclusive | 可省略；给出时严格大于下界 |
| includePrerelease | 必填 bool；false 拒绝所有 prerelease，true 才按完整优先级参加区间 |

旧 MinimumVersion 只在旧模式中解释。新模式同项混写 MinimumVersion / IsRequired 等旧别名时给迁移诊断，不能选一个掩盖冲突。Mod Version 新模式按同一 SemVer parser；native game build、SDK target 和 Runtime 现有版本比较不能被一并替换。

数值 prerelease 不接受前导零，按数值比较；非数值按 ordinal，正式版大于相同核心 prerelease，build metadata 不影响优先级。版本算法共享一套语料，使用版本带 build metadata 不意味着两个 DLL 字节可以不同。ID 比较继续既有身份规范。

缺 required / 版本不满足 / required 环：阻止该必需闭包进入 Entry，并给出完整依赖链。optional 缺失或不满足：明确 unavailable，消费者可降级。存在且满足的 optional 只提供尽力排序，形成排序环时按规范化 ID 确定性拆 optional 边并告警；不能随机或把纯 optional 环变成全局无法启动。

optional Mod 不等于 optional CLR 引用。消费者若在自身签名或方法体引用外部 DLL，该程序集仍须存在；必须独立携带共享 contract 或用不触发缺库绑定的设计。不能因分支暂未调用就放过 DLL 闭包缺失。

## P03：程序集清单与分发闭包

assemblies 每项字段如下，全部来自实际 PE/文件，不能相信作者填写的 identity：

| 字段 | 来源及约束 |
| --- | --- |
| path | 包内规范化相对路径；入口仍按既有 EntryDll，附属库位于 lib/shared 或 lib/private |
| role | entry / shared-contract / private-managed，固定枚举 |
| name, assemblyVersion, culture, publicKeyToken | 实际 CLR identity；culture/token 规范化空值；name 与文件名映射必须唯一 |
| length, sha256, targetFramework | 实际文件；游戏加载只接受支持的 netstandard2.0 闭包，不从上层工程 TFM 猜附属 DLL |
| references | PE AssemblyRef 的规范化 identity 列表，包含到 BCL/平台/宿主的边；验证中与实际 PE 相等 |
| distribution | self-authored / licensed-third-party；后者给 licenseFiles，文件须在包内 inventory；来源显示为本地引用或锁定 package ID/version |

SDK 的 author schema 3 用 managedReferences 声明显式 path、role、distribution、licenseFiles；它是输入，不是最终 inventory。共享契约可由多个包分发同一份；任一包单独安装也应有完整闭包。依赖其他 Mod 的 API 不意味着可偷偷从那个 Mod 目录加载未声明库。

Runtime 不做法律裁决；SDK/Doctor 检查许可材料存在且归属明确，未知/无许可的第三方库不进入可发布包。引用不等于允许再分发；平台/游戏/Unity/BepInEx/Harmony 宿主 DLL 禁止随 Mod 复制。宿主边解析到当前已知提供者，BCL 是受支持集合，不是任意 System.* 前缀白名单。Strict 对传递宿主引用仍拒绝，ContentPack 不能携带执行 DLL。

shared-contract 的用途是接口、枚举及 DTO；避免 native 类型、可变静态状态和初始化副作用。格式检查不能声称任意 managed DLL 安全，也不把 IL 检查当沙箱；实际 shared contract 样例必须无初始化执行。任何加载都有潜在不可撤销作用，预检必须使用 PE 元数据读取而非执行目标程序集。

## P04：预检、绑定与失败传播

执行顺序固定为：官方来源/启用选择 → 所有选中 Mod 的 manifest/包完整性 → 全部声明及传递 AssemblyRef 闭包 → Mod 依赖图与冲突计划 → 确定性加载 → provider Entry → 必需 consumer Entry → 可用性发布。不能先 LoadFrom 再“验证”同身份冲突。

首版采用保守的单 AppDomain 策略：

1. 同 simple name、同完整 identity、同 SHA 的 shared-contract 绑定到一份实际 Assembly。顺序按规范化 owner/path，仅用于选相同字节的文件位置。
2. 同 simple name 的 identity 或 SHA 不同：拒绝所有冲突参与包及其必需消费者，列出双方/多方与原因；无关 control Mod 正常执行。private-managed 也遵循此规则，private 不代表隔离。
3. 同字节的 private-managed 可共用一次绑定，但作者不能据此依赖跨包全局状态。入口程序集之间同 simple name 直接要求作者改名，避免多个 owner 共用一个入口/静态实例。平台/宿主保留名字不允许占用。
4. 已驻留的程序集来自允许的宿主或本次绑定计划才能复用，须核对 identity、来源和可取得的字节证据；来源/摘要无法证明时报告 resident-conflict/restart-required。磁盘换新不能替换驻留事实。
5. loader resolver 只从该已验证计划解析，不遍历所有 Mod 目录找“能用的 DLL”。出现加载中二次冲突、静态初始化失败或已校验文件变化，停止受影响闭包；不尝试卸载已进入 CLR 的 DLL。

平台持有依赖图和 owner 的可用性。required provider Entry 失败，consumer 不进入 Entry；已活动 provider 关闭，先关闭其必需 dependents，再释放 provider 的 registry/资源，顺序固定，失败沿既有 cleanup retry。optional consumer 保持运行，查询到 provider 不可用。

GetApi<T> 仍为准确 CLR Type 的直接对象。平台不能撤销别人缓存的对象或隔离其任意方法；样例 provider 必须用 owner guard，consumer 在 provider 可用时调用并在关闭时释放。直接引用失效后的作者错误不伪装为平台自动代理。旧 registry 可用性查询若不够，只新增可选快照/通知服务，不能给旧接口加抽象槽。

## P05：自助 Advanced 与原生来源

沿 A08：manifest CodeModKind=Advanced 且 NativeContractVersion=1 进入开放分支；明确 Advanced 而没有该字段走旧 receipt/policy；省略 CodeModKind 才能进入原有 legacy 分类。任何新格式错误都不能回退成 legacy。无需第一方 Catalog ID 或逐作者政策。

author schema 3 的 nativeReferences 记录显式本机 gameRoot、references 列表和 harmonyOwner（必须可归属该 UniqueID）。gameRoot 只在本地工程，包中不带绝对路径。SDK 先直接使用该安装的合法编译引用；若 Mono/Unity facade 冲突，生成通用、确定性的本机 metadata reference surface，按源 identity/hash 与工具版本缓存。不得退回每产品手写 stub；reference surface 也禁止打包发布。

dtmapi-native-build.json schema 1 字段：ownerId、apiTarget、targetFramework、manifestSha256、entrySha256、dependencyInventorySha256、gameBuild、references、requiredMembers、harmonyOwner、referenceGeneration。references 含 assembly identity/length/SHA 和直接引用或本机 surface 模式；requiredMembers 来自实际输出的宿主 AssemblyRef/TypeRef/MemberRef 及作者补充的反射/Hook 必需签名。referenceGeneration 绑定工具版本/输入摘要，不放官方方法体。

V1 的 requiredMembers 包含宿主程序集 identity、声明类型完整名、成员种类/名称、静态性、参数/返回类型；不从方法名称猜重载，无法表达的签名明确 unsupported。PN-038 的泛型与新旧分支沿下方 P08；不得原地重解释保留 V1。native provenance 的摘要图是 manifest/entry/dependencies → native 描述 → 既有总 inventory，不相互包含 hash。

声明的必需 native 成员缺失时拒绝该包，build/hash 漂移但准确必需签名仍匹配时警告“未验证的游戏版本”，不凭版本字符串猜兼容。动态查找不可能完全静态发现；未声明可选 Hook 的缺失由作者显式降级与日志处理。给出 owner patch/cleanup recipe，不承诺替第三方恢复任意 Harmony 副作用。

SDK、Doctor、Runtime 共用新旧分支语料。新作者、旧 receipt Advanced、旧 Strict、legacy 同进程共存必须单独实测。此新通道落地时只改 PROJECT 的 Advanced 准入段和对应机器规则，不改变 managed Mod 的官方位置与原生代码 ownership。

## P06：多项目、资源与显式 restore

2026-09-10 后续目标由 [SDK 标准构建 AB-02/05/06](platform-sdk-build.md)替代原专用 BuildPlan/DAG/资源/restore 子集。旧 schema 3 的具体语义保留在原 SDK、[已完成执行包](../planning/platform-next/execution-sdk.md)及 Update，不改写历史包。

工程图、资源、生成器、分析器、NuGet 选择交给标准工具链；DTMAPI 只消费实际构建与明确交付项。发布内容需要显式包路径，不能把 CopyLocal/CopyToOutputDirectory 的全部文件当可分发资产。构建期工具和 compile ref 不进入玩家运行清单，所选 runtime DLL 仍执行 P03/04。

author schema4 只是工程迁移，不改变 P01 的 marker、DependencyContractVersion 或 P08 Native 契约。新的标准输出可生成原 marker2/3；旧 target 不因此自动抬最低 Runtime。不同 ref/lib 在 PN-041 绑定实际运行实现与使用成员，低 TFM/facade 等额外运行承诺另经 PN-040.b/R-Assets。

标准锁与恢复来源继续可审计，凭据不入包；日常 restore 与发布锁定/离线模式分别说明。不按源码目录、工程数量或自有解析器限制决定玩家协议；最终包的路径安全、大小写碰撞、归属、身份与依赖检查仍保持。

## P07：R3 冻结对象

R3.shared 在 PN-011 的三个仓库外工程、无关 control 和实际 Mono 冲突矩阵后，确认 P01–P04。R3.native 在 PN-010 的任意 ID 本机引用、旧 reader 共存和 native drift 后，确认 P05。两节点不互等，也不重审无关平台。

格式可在隔离候选中按证据修订；只有经过 PN-022/023 合成旅程后才进入首个公开 target 候选。公开后增量只能新契约版本/新 target 或兼容加法，不能重写冻结载荷。字段草案不授权部署或发行。

## P08：可验证的原生泛型 V2

2026-09-10 的 SDK 审计已反证“V1 的签名子集足够覆盖普通 Unity 开发”。本节拥有 PN-038 的 V2 契约，实际实现/Mono 证据查[对应 Update](../updates/2026/20260910-0003-sdk-delegates-native-generics.md)；PN-041 复用这些规则，不重新设计 V2，也不扩大公共反射或通过删除校验支持泛型。

### 版本和归属

- 新当前目标的 Advanced 使用 `NativeContractVersion=2` 与 `dtmapi-native-build.json schemaVersion=2`，marker 必须一致。DependencyContractVersion=1、author schema 3、Mod 身份及官方来源不因签名表达变化而另起一套协议。
- 新 reader 同时接受准确 V1 与 V2；无选择器的旧 receipt、legacy 继续原分支。旧 target 生成原 V1；现有 V1 包不重写。未知版本、V1 内容冒称 V2、选择器/文件/marker 错配拒绝，禁止回退。
- 新 V2 需要支持它的 0.7.0 Runtime 候选。SDK 生成的最低宿主声明/诊断不得继续暗示 V2 可在旧 0.6.X 运行；旧 host 的未知契约拒绝要有明确升级说明。未发布 r3 与新候选同产品版本不等于字节/能力相同；以确切包与格式诊断区分。已发布 0.6.1 的事实不变。
- 这是 SDK/包/Runtime 能力，不能借此改写 Abstractions 的冻结 0.5.5/0.7.0 payload 或 assembly identity。reference surface 的生成算法若未变，保留其准确 0.6.4 / metadata-surface-v1 identity；CLI 版本、签名格式与引用生成器版本分开。
- 纯模型/规范化/reader 放现有 Shared/Authoring.Contracts；Cecil 元数据提取留 Tooling.Metadata；Core 用已有宿主验证边界。不能为了泛型给玩家加一个新的强制 Cecil/QA 组件。

### 表达与匹配

类型用有界结构而非拼接泛型名字：Named（定义及准确 assembly identity）、TypeParameter（声明类型的位置）、MethodParameter（声明方法的位置）、Constructed（定义与有序实参）、Array（vector/rank）、ByRef，以及现行明确支持的 Pointer。嵌套声明类型保留外层/本层 arity 和参数位置，不以 T/U 等源码名称区分身份。BCL facade 只按已验证的映射归一；不能按 System 前缀任意接受依赖。

成员记录 **定义签名 + 实例化使用**：定义的声明类型、种类/名称、static/calling convention、generic arity、返回/参数结构；类型和方法参数分别记录 special constraints 与规范化约束类型集合。使用记录给出声明类型实参/方法实参及其引用身份，关联同一个定义。字段和构造函数遵循同样的声明类型规则。约束、重载、嵌套类型或 arity 改变必须可区分，不能只匹配一个 GetComponent 名称。

提取应覆盖 PE 中实际使用的 TypeSpec/MethodSpec、成员引用与相关签名/约束；只遍历 TypeRef/MemberRef 会漏掉实例化。SDK 打包对入口及所有附属库扫描，动态反射/Hook 的必要签名仍由作者显式声明。V1 的显式非泛型声明可确定性提升为 V2 定义；新泛型声明使用同一结构化模型，不另建 Hook 许可表。

SDK 与 Doctor 只读 PE。Runtime 对准确宿主定义及约束作匹配，实例化实参通过包 PE/已验证依赖身份核对。尤其 GetComponent<作者类>：预检不可先加载作者 DLL 或构造闭合 CLR Type 来证明约束；比较未变的宿主约束、实际已编译使用及已绑定包闭包，不执行任意第三方代码。实际调用/JIT 的 Mono 证据另作产品门，不宣称 metadata 验证是恶意 IL 沙箱。

未知节点、非法参数位置/arity、重复字段/定义、未声明 assembly、深度/长度越界要拒绝。V2 第一片不承诺 function pointer、vararg、无法准确比较的 custom modifier/non-vector 特殊数组；它们在 build/pack 使用同一成员级诊断。已支持泛型的失败不能一律回退“改用非泛型”。

### 验收与冻结

至少证明普通 Unity 泛型、作者自有 Component 实参、泛型宿主/嵌套组合、数组/ref/out、约束/重载变更、宿主/包漂移和 V1 共存。桌面 metadata 与 Runtime 反射表示必须在固定语料上一致；真实 Mono 再证明返回值、对象身份及释放。build 与 pack 共用可交付性检查，pack 仍检查 build 之后的漂移。

R-Generic 只复盘此格式、预执行边界及实际证据；无反证就继续工程改善和最终候选。新格式进入公开交付前完整闭合 SDK/Core/Doctor/模板/旧 reader/Mono；不能只让编译或 pack 变绿。
