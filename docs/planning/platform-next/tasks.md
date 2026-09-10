# DTMAPI 平台工程任务卡

- Lifecycle: `active`；Role: 任务规格，不保存执行状态。
- Owners: [入口](README.md)、[路线](roadmap.md)、[唯一状态](status.md)、[能力地图](capability-map.md)、[产品验收](acceptance.md)。
- Architecture: [主架构](../../architecture/platform-next.md)、[作者交付 AD](../../architecture/platform-author-delivery.md)、[Runtime RT](../../architecture/platform-runtime-contracts.md)、[数据内容 D](../../architecture/platform-data-content.md)。
- Current contracts: [PROJECT](../../../PROJECT.md)、[API matrix](../../api/public-api-matrix.md)、SDK catalog/release authority；计划不是已发布事实。

## 使用与完成规则

当前近期为 [PN-041 标准 MSBuild](execution-sdk-msbuild.md) a–f＋[PN-042 现有 Mod/入口兼容](execution-compatibility.md) a–c，同任务完成并集中验收后统一交回。后续 [方法验证](method-validation.md)嵌入现有 R 节点。原 SDK/M3 规格在 [execution-sdk](execution-sdk.md)和 [execution-next](execution-next.md)保留，不重领已完成任务。本册只保存规格，状态/版本沿对应 owner。

最新事实：[0011](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)确认旧 SDK 未发布，取消通用旧 SDK 迁移/客户端支持；CSV 是已授权清退的第一方临时接口，不恢复。下文 PN-002/015 等已完成卡中的旧客户端/迁移行为是内部历史规格，不是本批新增支持义务。当前 PN-041/042 共用一份实施 Update；042.b 仅有真实新增回归才实施。

按 status 领取 ready 项，不按数字顺序。每项有独立 Update；切片用“任务 ID / 切片名”。依赖只列直接进入条件，传递依赖仍生效；可提前独立调查，不能据此完成整项。

连续执行终点沿 README/status：先修验收缺口，同任务完成阶段和 R 复盘；结论支持就继续本批已细化的后续任务，直到真实停止条件。无依赖内部片可交错；原生/设备/公开冻结的必需证据不因此豁免。

代码实现、作者/Mono/游戏产品证明、发布可用性分别记录。E01–E08 见[验收规则](acceptance.md)；集成卡使用固定候选，不拼接不同候选的局部 PASS。缺真实证据时保留对应缺口。

E 编号代表场景家族，单任务只执行适用子项；必要未验项保留缺口，未触发家族不用逐项免测说明。PN-020 不等 E07 持久实体；PN-024 先选保存方法，再验证所选后端的 native/复制/故障原型，公开服务的完整作者验收留 PN-012/026。后期场景引用不能成为隐式前置。

M2 的 PN-005/009/018/019 在各卡完成源码、受控 Core/Mono fixture 和适用游戏验证，确定契约草案；正式 SDK 外部作者消费集中在 PN-007.a→PN-020。受控 fixture 可引用该切片生成的测试契约，但明确是内部验证，不冒称 E01 外部工程。对应产品证明在 PN-020 前保持 pending；不能为提前完成一张卡手改外部作者的 Runtime DLL 引用或绕过普通 SDK。

执行细节采用[产品验证流程](../../workflows/product-change-validation.md)和[当前脚本选择](../../../tools/scripts/README.md#choose-validation)。`test-unit.ps1 -Focus <已有名称>` 已包含所选项目的构建与准备；`-NoBuild` 只复用输入未变的输出。旧 Unit DLL 是兼容 dispatcher，不单独构建它来准备全部新套件。SDK 专项测试使用其现有 focus/项目入口，新增用例进入实际 runner。所有工具链由 `Get-DotNetExe` 解析。

需要 SDK 才使用 `prepare-author-sdk.ps1`；普通产品包装沿一次编译的 `pack --build-output`/既有脚本，准备和报告复用当前输入，不重复 build→pack 再编译。目录、准备回执和失败恢复沿已有工具，不再手建计划专用基础设施。冻结旧载荷已有源码准备入口，无需每任务查找旧发行包。

同一候选的源码/依赖/包、游戏 build、配置与场景假设未变时复用受影响证据，只在 Update 写简短理由；修改文档不触发重建，后卡不重复前卡全部故障矩阵。完整 Release、公开 CI、安装器和长期性能套件只在明确触及其边界或任务列为出口时执行，不由“阶段收口”自动触发。必须完整运行的入口仍不能拼接 PASS。游戏加载 DLL 保持 netstandard2.0；共享 Runtime 锁、保存模式、隔离及产物位置沿 PROJECT/现有流程。

记录沿[Update 模板与 sync](../../updates/README.md)：一项有界实施一份 Update，修正、验证和阻碍继续同份；Review 只为新/改变的根因或契约节点，不为每卡再建审核。未变化的 API/Hook/Issue 不重写；实际游戏运行才写 smoke。读过且未变的上下文复用，不把研究目录或内部治理变成作者文档前置。

回滚只撤任务产物与自有测试部署/配置/Author 状态，不覆盖未知文件或玩家存档。未公开契约可撤；已公开 ABI/schema/data reader 保持兼容或显式迁移。

## 任务迁移说明

保留 PN-001–014 ID 与旧引用。PN-001–003 原规格完整保留用于追踪，状态/证据沿 status/Update；其前瞻文字关于固定新默认版本及“PN-007 反射载荷”由本节取代：候选 target 按实际能力冻结，旧载荷不可改。

| 原安排 | 当前范围 |
| --- | --- |
| 反射先于作者验收 | M1 现有 API 作者回路；PN-006/021 反射移 M3 |
| PN-007 反射及固定新版本 | M2 公共 target，不强制反射或某候选版本 |
| PN-008 反射示例集成 | M1 作者/Mono 验收 |
| PN-009 队列和命令 | RT-01–RT-04 上下文/作用域/调度/资源/命令 |
| PN-012 全局与存档混合 | PN-018 全局；PN-024 身份实验；PN-012 SaveData |
| PN-013 兼管 Host | PN-013 内容；PN-025 Host/Pack |
| PN-014 末尾重构 | 连续有界裁剪，非所有任务前置 |

## PN-001：修复并拆分版本投影测试基线

**依赖：** 无。第一项工作，不先实现新 API。
**目的：** 恢复可信的测试信号，把版本投影检查与具体已发布包的字节快照分开。

**代码与事实入口：**

- `tests/DTMAPI.UnitTests/Program.cs`：`PreviewVersionMetadataIsConsistent`，调查时失败点约第 2737 行。
- `tools/release/dtmapi-runtime-version.props`、`dtmapi-product-catalog.json`、`current-subscription-manifest.json`。
- `tools/scripts/check-product-catalog.ps1`、`check-release-contract.ps1` 与 manifest 路由的最新 release Update。
- `Directory.Build.props`、Core 的 `ApiVersion` / `BinaryVersion` 投影及 SDK 的独立 target 常量。

**实施要求：**

1. 使用已有 `DTMAPI_UNIT_TEST_FOCUS=prerelease-step5` 复现，不先更改 Catalog。
2. 对照当前 release authority 核实测试内旧 `8280dc…` / `c73359…` 与 Catalog `846665…` / `b4ec6…` 的来源。
   这些前缀只是本次调查定位线索，不是新的包校验权威；完整值和当前事实从 Catalog 读取。
3. 判明是测试复制旧事实、Catalog 错写，还是包证据不一致。先运行既有 checker 并读其 release 来源，不能盲换常量。
4. 将“源码版本从权威投影”“已发布产物与发行记录匹配”“第一方具体版本约束”拆为独立测试职责。
5. 普通 Unit 检查稳定关系和解析行为；精确已发布文件树摘要由既有 Catalog/release checker 负责。
6. 用可控制 fixture 证明错误投影、篡改元数据会失败，避免只比较同一 JSON 字段与自身。
7. 不在本任务将当前 Runtime 改为未来 `0.7.0`，也不把当前 SDK 改为 `0.2.0`。

**验证：**

- 已有 focus `prerelease-step5` 先红后绿，Catalog 与 release checker 不被降级或绕过。
- focus `platform-version-projection` 必须加入同一 Unit 默认执行路径；执行结果由任务状态路由。
- 修复后完整运行 Unit 一次，确认之前被首个失败遮挡的尾部；按失败范围继续修复或如实登记。
- 不因其他四个 suite 已通过就把未通过的 Unit 或完整 Release suite 标为通过。

**验收交付：** 一个可单独执行的版本关系测试组、一次权威核对结论、无重复维护发行摘要的普通 Unit 断言。
**回滚：** 撤销本任务代码与测试；保留复现及权威核对证据，不能回滚已发布 Catalog 到旧值来迎合测试。

## PN-002：真实 CLI/Core session 握手与协议版本分离

**依赖：** PN-001 的版本维度拆分；与 PN-003 的数据模型保持一致。
**目的：** 作者编译目标与安装 Runtime 不同仍能在受支持协议范围内建立会话。

**代码与测试范围：**

- SDK：`AuthorSessionService.cs`、`AuthorStateInfrastructure.cs`、`Authoring.Contracts/AuthorContracts.cs`。
- Core：`AuthorSessionContracts.cs`、`AuthorSessionDescriptorStore.cs`、`AuthorSessionHost.cs`、`DtmApiRuntime.InitializeAuthorSession`。
- GameBridge：`AuthorSessionReloadBridge.cs` 的请求来源与包版本投影，保留代码更新要求重启的行为。
- `tests/DTMAPI.AuthorSdk.Tests/Program.cs`、Core session 测试和 `author-sdk/schemas/author-session-*.schema.json`。

**实施要求：**

1. 把调查探针变成持久组合测试：真实 `AuthorApplication.RunAsync(session prepare)` 生成 descriptor，真实 Core reader 消费。
2. 空 fixture game root 即可，不需要游戏进程、原生程序集或 SDK 编译载荷；所有凭据写在测试 session 之内。
3. 对照当前 Runtime 与旧 target 分别运行，记录各自结果；不能只手造与 Host 同版本的 descriptor。
4. 新 schema 2 独立表示 protocol、capabilities 和实际 hostVersion；SDK 版本与编译目标不再承担协议匹配。
5. 原内部 schema 1 的有界 reader：接受已知旧客户端时建立受限 wire-version alias `0.5.5`，同时绑定真实 Host/session/token/root。这是已完成内部行为，旧 SDK 未公开发布，不为 PN-041 增加旧客户端支持义务。
   旧客户端的 request 和 response 同样执行精确匹配，后续 envelope 必须使用协商好的 wire 值。
   不能只放松 descriptor 的版本相等判断，也不能改所有消息常量为 `0.7.0` 来形成下一次同类故障。
6. 新会话向客户端明确实际 Host 版本和支持操作；后续请求验证 Host/session/request/operation 的绑定。
7. 旧客户端无法理解的能力不静默启用；schema 2 忽略未知可选 JSON 字段，拒绝重复关键字段、未知必需能力、未来 major 和不兼容操作，错误采用稳定诊断。
8. 保留一次消费、过期、ACL/token、重放拒绝、有界队列和超时；日志及 JSON report 不暴露 token。
9. `snapshot` 使用当前选中来源；`reload` 对 DLL、未知格式、身份变化返回明确 `restart-required`。
10. 新 SDK 不静默降级到 schema 1；仅在认证 Host 响应或可核验的本机安装/启动诊断证实不兼容时返回 `upgrade-required`。没有 listener / 连接超时使用 `host-unavailable` / `handshake-timeout`，不猜测 Host 版本。

**验证：**

- AuthorSdk focus `platform-session-handshake` 必须接入 selector 并与默认 suite 复用函数；这是测试环境变量，不是 CLI 参数。
- 实际 SDK descriptor → 实际 Core reader/Host → 实际 SDK response validator；fake server 仅补故障窗口。
- 覆盖旧 schema 1/current Host、新 schema/current Host、未来协议、Host不兼容、过期、重放、错误来源和响应身份。
- 分别模拟“旧 Host 拒绝 descriptor 后未创建 listener”与“新 Host 尚未启动”；两者仅凭无响应都不能误报需要升级，另用明确版本证据验证 `upgrade-required`。
- 没有 descriptor 的普通 Runtime 不创建 listener；会话关闭后队列与凭据生命周期正确。

**验收交付：** 真实跨组件测试通过，协议文档解释版本字段，当前 session 错配不再存在。
**回滚：** 回退本次兼容适配与新 schema 写入；保留旧 reader，不删除用户既有恢复信息，不撤销凭据检查。

## PN-003：SDK 多 target 与冻结载荷骨架

**依赖：** PN-002；其 session 版本语义不能再次绑定编译目标。
**目的：** 让后续 API 与 SDK 一起演进，旧作者项目继续可重现构建。

**代码与测试范围：**

- `Authoring.Contracts/AuthorContracts.cs`、SDK `CompatibilityAssets.cs`、`ProjectValidator.cs`、`TemplateCreator.cs`。
- `CodeModBuilder.cs`、`DeterministicPackager.cs`、Runtime `ManagedModClassification.cs`、Doctor 对 package marker 的解析。
- `author-sdk/compatibility/0.5.5`、模板、schema、`build-author-sdk.ps1`、`author-sdk-release-common.ps1`。
- `tests/DTMAPI.AuthorSdk.Tests`、`tests/DTMAPI.InstallDoctor.Tests`、现有 target/marker 的 Core 测试。

**实施要求：**

1. 引入 SDK target catalog：目标、兼容契约、payload 路径、哈希及对应 API 能力范围有一个来源。
2. 旧 `0.5.5` 文件和嵌入式 trust anchor 原字节保留；新 target 使用独立 contract，不覆盖旧 DLL 或重算旧承诺。
3. 区分 author project schema、SDK release、API target、最低 Runtime、session protocol 和发行 assembly identity。
4. 构建按项目选择 target；旧 schema 项目保持旧 target，新模板后续默认选择 `0.7.0`。
5. 本任务建立未来 Runtime `0.7.0` / SDK `0.2.0` 目标配置骨架；公共反射载荷直到 PN-007 才封装交付。
6. target 未完成时不能伪报“已支持反射”；新项目不能发布一个实际缺少对应 API 的包。
7. 完整串联构建、pack marker、Core 与 Doctor 的 target 组合解析，拒绝不支持的目标和被换绑的 marker。
8. 复用现有 payload hash、无额外文件和确定性验证，不新增另一套发行资格收据。
9. 现有 Advanced policy 继续引用其旧 target；不借本任务开放原生准入或迁移第一方已发布包。

**验证：**

- 已有 focus `platform-sdk-targets`：SDK 使用 `--focus platform-sdk-targets` 或 `DTMAPI_AUTHOR_SDK_TEST_FOCUS`；Core 使用 `DTMAPI_UNIT_TEST_FOCUS`；Doctor 使用 `DTMAPI_DOCTOR_TEST_FOCUS`。覆盖 available/planned 两种目标、旧 schema、缺载荷、篡改载荷、错误最低版本及历史低 floor 包读取；每组均接入默认套件。
- 用旧目标重新构建旧示例；以其确切 frozen payload 编译，不能从当前游戏安装读 Abstractions。
- 旧 SDK/current Runtime 的既有包仍可加载；新目标包在不支持目标的旧 Runtime 上有可理解的拒绝。
- 用现有 `check-author-sdk-release.ps1 -PackagePath <实际SDK包>` 验证完成的候选；骨架阶段不伪造发行包通过。

**验收交付：** 新能力可按目标扩展的 SDK 代码与契约测试；旧 payload 完全未改。
**回滚：** 新 target catalog/reader 适配可撤销，旧目标入口与哈希仍完整；未发行目标不能留下默认选择。

## M1：真实作者回路

**M1 这一段的范围不缩减：** PN-014 的 `config-correctness` → PN-015→004→016→017→008→R1；已完成部分沿 status 复用，当前先修 A1–A3。R1 支持后按最新连续执行授权进入 M2，不自动结束任务。原编号和产品验收保留，仅去掉已被基础设施替代的重复准备/构建/记录；符号准备可与构建/安装同批做，PN-017 仍拥有实际调试出口。

### PN-015：统一 CLI 与 IDE 构建语义

**依赖：** PN-003。**设计：** AD-01/02。**输入：** SDK `CodeModBuilder.cs`、`TemplateCreator.cs`、`ProjectValidator.cs`、`DeterministicPackager.cs`、`author-sdk/templates/codemod`、冻结 props、AuthorSdk round-trip 测试。

**切片交付：** ① 从现有编译器提取内部规范化编译计划，明确源/引用/metadata/语言级别/常量/Debug-Release 输出；② 新模板的 IDE Build 委托同一 CLI，design-time 使用相同引用与语言设置；③ 构建报告记录实际输入与符号 identity，旧模板迁移为显式操作。manifest 仍拥有 Mod 版本；不原地改冻结 props。当前未支持的工程输入须诊断，不能静默忽略 ProjectReference/PackageReference 后显示成功。

**SDK160 同批纠正：** `ScanForbiddenReferences` 的全文子串不能继续作为硬门。复用 BuildPlan 的真实引用、Roslyn 语义和现有 PE/Runtime/Doctor 闭包校验，项目 Reference/HintPath 和允许的依赖输入从结构读取；不因注释、字符串、未参与编译的文本或同名用户类型报越界。真正宿主引用和间接禁用依赖仍须拒绝；引用无法解析时给真实编译/输入错误，不猜词。SDK161 当前限制保持，M3 再开放普通辅助 DLL；不借本修复提前开放 Advanced 或建立新 analyzer 产品。

**异常与测试：** 中文/空格路径、缺 SDK root、错 target、残留旧输出、故意编译错误、重复构建、Debug/Release 互不覆盖、IDE/CLI 差异输入。复用 AuthorSdk tests；新模板必须通过实际 SDK 子进程和正常 IDE Build 入口验证，不能仅断言 csproj 含某字符串。

增加注释/字符串/同名用户符号不误报，以及真实项目宿主引用、别名或全限定调用、间接闭包拒绝的成对样例。复用 `pack-build`、`platform-sdk-targets` 等已有 SDK 回归；若新建构建/验证 focus，接入现有 AuthorSdk runner 和适用 CI，而不是用默认全量套件替代有界断言。构建+pack 的正常路径只编译一次；确定性验证才明确生成第二份产物。

**验收/退出：** 仓库外项目在同一配置得到相同有效编译输入及可验证产物；Strict 离线不依赖游戏、NuGet 缓存和 PATH dotnet。PDB emit 只算符号产物，调试效果留 PN-017/E02。

**2026-09-09 返修 A2/A3：** [验收反例](../../reviews/code/2026/20260909-0001-platform-m1-acceptance-continuation.md)证明旧模板的合法非默认 sourceDirectory 经 migrate-build 成功后却不能 build；未支持的 Content/复制项和冲突 AssemblyName 也静默通过。迁移必须投影现有输入并在替换前验证候选，保持失败时原项目字节；验证器应完整拒绝未支持输入/权威冲突或真正纳入 BuildPlan。以公开 CLI 的迁移前后 build/pack、真实 IDE 委托及实质负例验收，不为这次修复改成全功能 MSBuild。接续 0011，不新开实现编号。

### PN-004：官方 MODS 开发安装、更新与恢复

**依赖：** PN-015。**设计：** AD-03。**输入：** SDK `AuthorApplication`、`DeploymentService`、`DeploymentPackage`、`SourceStateService`、`PathSafety`；Core `ManifestReader`/`AuthorSourceState`；现有 `test-developer-official-local-install-transaction.ps1`。本任务是作者 Mod 官方 Local 事务；只有实际修改玩家 Runtime 安装器字节/布局时才进入[安装器边界](../../architecture/runtime-workshop-installer-boundary.md)的对应矩阵。

**切片交付：** ① 通用官方 MODS 定位与 Local ID 映射，不依赖产品 Catalog；② 新旧事务 reader 分流，共用 journal/inventory、同卷 staging 和 recovery；③ 公开 CLI 安装/update/withdraw/status 与当前来源读者集成。保留历史 game/Mods 恢复，停止用它演示新开发路径。SDK 不写 Workshop、不自动启用，config/data 不属于替换包。

**异常与测试：** 任意 Author ID、错根/ID/版本/hash、未知目录和额外文件、无权限、并发、移动/提交中断、重复恢复；先在 session fixture 注入失败窗口。实际公开 CLI 不可绕到测试专用旧入口。封住进程检查到发布窗口；游戏运行时拒绝冷代码变更且文件不变。

**验收/退出：** E01 的安装半程：从真实 SDK 包新建外部工程并安装，Runtime 真正的官方来源读者识别一致 ID/root/bytes。完整启用/运行由 PN-008 固定候选复验；文件成功和加载成功分别报告。

### PN-016：真实生命周期事实与外部探针

**依赖：** PN-004。**设计：** RT-01/RT-02/RT-06。**输入：** `DtmApiRuntime.Start/NotifySaveLoaded/NotifyReturnedToTitle`、`ModOwnerLifecycleCoordinator`、`EventManager`、Bootstrap 帧入口、GameBridge Hooks/`DolocTownHookCallbacks`、当前 focused Hook map 与原生 LoadGame 调用。

**切片交付：** ① 优先使用现有生命周期/产品日志记录 Entry、GameLaunched、原生 load、SaveLoaded、首次世界可用、返回标题和退出的线程/epoch；② 外部 Strict 只使用现有 API，缺哪一处原生事实才加最小内部观测，不强制安装 HookProbe 或 QA；③ 新事实回写匹配 focused map/当前记录，作为 PN-009 输入，不为已确立的根因再建 Review。不得为了整齐顺序改变旧事件含义或伪造 SaveSaved。

**异常与测试：** E03 覆盖标题→档 A→标题→档 B、同档再载入、新游戏、可控加载失败、Entry 异常、退出。两档指不同会话身份，不要求另建隔离运行环境；正常路径原地 NoNativeSave，新建/保存或故障窗口按 PROJECT 的授权槽/隔离条件选择。复用可用 native-owner 研究，仅核对观测所依赖方法与实际游戏 build。跟踪 callback 线程及重复帧，不能只看一条 SaveLoaded。

**验收/退出：** 有真实 Mono 同进程顺序和正常退出证据，明确哪些对象在 SaveLoaded 尚不可用。若 ready 点未证明，记录具体 native 问题并阻止对应上下文能力，不推断已就绪。

### PN-017：作者诊断、符号与可用调试流程

**依赖：** PN-016。**设计：** AD-02/04/05。**输入：** SDK builder/packager/session、Doctor、Core `DiagnosticsService`/`RuntimeSnapshotFactory`/`FileMonitor`、现有报告导出和外部样例。

**切片交付：** ① 开发 artifact/安装保留匹配 DLL 的 symbols，明确 Release symbols 选项与路径映射；② 统一实际 host/target、选中来源、磁盘/加载指纹、owner/phase/问题码；③ 发布可复现的异常定位与 trace 流程；④ 有界调查 shipping Mono debugger，分别记录断点、源码行和追踪能力。不可用的断点是技术限制，不能以生成 PDB 代替，也不改普通玩家 executable。

**异常与测试：** E01/E02：错符号、缺符号、故意 Entry/事件异常、旧进程加载旧 DLL、官方未启用、依赖失败、会话超时。真实 Mono 错误须定位正确源码；状态仍不确定时明确 unavailable。报告导出复用 Doctor/日志，检查 token 和可识别用户路径，不默认带存档/全部配置/凭据，不自动上传。

**验收/退出：** 作者可凭 SDK 文档/报告定位、修改并重启；至少源码行或经证明可用的等效定位流程成立。无法断点不能阻止其余能力，但该项不得标通过。

**2026-09-09 返修 A1：** 错配 PDB 当前成为 internal/SDK999；pack-build 两个负例把 SDK191 当命令执行，仅验证 usage/SDK001 非零。接续 0014，修正 SymbolInspector 的预期错误映射，真实执行 symbols 并断言 command/退出类别/SDK191，包含正确配对控制组。复用实际 Mono 的定位精度证据，不为工具错误分类重跑无关完整游戏旅程。

### PN-008：M1 外部作者与真实 Mono 产品验收

**依赖：** PN-017 与 PN-014/config-correctness。**场景：** E01、E02、E03。**输入：** 固定 SDK/Runtime 候选、PN-004 事务、外部样例和 PN-016/017 证据；无需反射或新公共 target。

**执行：** ① 新 ID/空目录，用候选 SDK new→validate→单次 pack 并保留 build output→Doctor→官方 Local 安装，独立 IDE 构建用于证明路径等价，不给同一包重复编译；② 按现有测试授权经官方启用界面操作并从 Steam 正常启动，确认实际 root/DLL 指纹和可见行为，SDK 自身不自动启用；③ 一次故意错误→公开诊断→修复→退出→update→重启；④ 运行中更新拒绝、停用/撤回、未知文件恢复负例；⑤ 标题/档切换/退出与非存档测试资产恢复。样例至少包含日志、配置、事件/输入；不得使用 Core/friend/QA 引用或维护者逐 ID 授权。

**验收/退出：** 实际 SDK 子进程与 Mono 行为、正确加载新字节、普通 Mod 异常隔离、干净退出、NoNativeSave 字节不变、文档从头照做通过。未解决问题按能力留缺口；源码 PASS 不把 M1 标产品通过。此任务交付候选，不上传 Workshop、不宣称已发布 SDK 或所有 API Stable。按现有事务撤回样例。

**收口执行：** 先核对前卡证据的相关输入；原样复用未失效的磁盘故障矩阵、ABI、SDK 组合和时序结果。本卡仍实际完成一次从公开 SDK 到错误修复/更新/重启/撤回的连贯作者旅程，只补缺失和被变更影响的子项，不再重造前卡全部夹具或默认跑完整 Release。记录候选支持的 SDK/Runtime/宿主、调试层级和重启限制。R1 按现有证据作出具体 M2 输入结论；使用 GPT-6 的实施任务可在同任务内完成该有界复盘，无需人为换模型再读全仓。

## M2：可组合公共基础服务

### PN-005：可选服务与旧实现者 ABI

**依赖：** PN-008。**设计：** 主架构 A02、RT-02/RT-04。**输入：** Abstractions `Helpers.cs`、Core `DtmHelper`/`RegistryAndHelpers`、owner coordinator、`tests/DTMAPI.AbiCompatibilityHarness`。

**交付：** 保持 IDtmHelper/Entry 抽象成员不变；新增可选服务入口和命名 extension，确切 Type 固定 allowlist，不开放一般 DI/跨 Mod 旁路。未知服务返回缺失，必需服务给出最低 target 的明确错误。复用 `ensureOwnerActive` 与 `RegisterModOwnerCleanupParticipant`；同 owner 重复查询同一实例，关闭后不能重获活服务。

**测试/退出：** E02/E03：frozen 旧 payload 编译的 helper 实现者及 consumer DLL 真正加载/实例化/调用；未知类型、关闭后调用、清理再入、重复获取及 owner 隔离。复用 retained ABI harness，不能用当前接口重编译冒充旧产物。可先用内部测试服务，不提前声称 Data/Reflection 存在。Mono/产品证据在 PN-020 复验；

### PN-009：上下文、作用域、scheduler、资源与命令

**依赖：** PN-005。**设计：** RT-01–RT-04 已选语义。**输入：** PN-016 ready 事实；Core Runtime/Owner/EventManager、Bootstrap 唯一帧驱动、GameBridge 生命周期、现有 console 命令入口。

**切片：** ① 不可变 context 与 runtime/save/world epoch；② 有界 owner-bound scheduler，Post/NextTick/Delay/结果/取消按 RT-03；③ owned resource 登记与确定清理；④ 命令注册、帮助、冲突和参数解析。通用执行器在 Core，游戏 ready 与失效事实在 GameBridge；不新增 timer/第二 Unity driver，不重做控制台 UI。

**异常：** 标题使用世界作用域、错线程、换档前排队、失效 owner、队列满、回调抛错、超时、关闭再入。每个任务必须唯一终态；已执行的副作用不宣称可取消回滚；未开始工作在作用域失效后不能执行。

**测试/退出：** E03 先可控时钟/队列故障测试，再真实后台纯计算→主线程可见结果、A→标题→B 旧工作拒绝、预算/顺序和正常退出。公开形状按 Runtime 契约确定草案供 PN-007 候选；在 R2 架构复盘使用 PN-020 真实证据后正式冻结，不破坏旧 ABI。恢复旧 driver，撤新服务时不留线程/queue/resource roots。

### PN-018：owner 文件访问与全局数据

**依赖：** PN-005。**设计：** D01、AD-03。**输入：** RuntimePaths、JsonFile、ConfigService、现有 Mod 资源路径和 owner cleanup；不需要等待 SaveData。

**切片：** ① 只读 owner 包路径与规范化 key；② 包外 global data envelope、读/写/删与原子替换；③ schema migration/损坏/未来格式结果。配置、包内资源、全局数据、玩法 SaveData 分开，禁止用此任务承诺存档数据安全；无效 key/越界/链接逃逸/大小写冲突拒绝，返回 Missing/Corrupt/UnsupportedSchema 不混淆。

**测试/退出：** E01/E08 的数据子项：两个 owner 的同 key 隔离、包更新/撤回保留数据、写盘中断与重复迁移、未知新 schema 不被默认值覆盖、无权限。受控 Mono fixture 实际读写非玩法数据并重启保留，无原生保存；正式 SDK 外部样例由 PN-007.a/020 证明。序列化不带任意 CLR 类型名。回滚保留旧 reader 和用户字节，不能清目录。

研究 F05 的具体回归：两个活动包都含 `data/settings.json` 且字节不同，新 owner-bound 服务必须读本 owner；旧 `IContentQueryHelper.TryReadTextAsset` 的全局首匹配行为保持其兼容说明，不直接换掉旧接口语义。

### PN-019：配置、输入与国际化的作者语义

**依赖：** PN-009、PN-018。**设计：** RT-04、D01、AD-09。**输入：** ConfigService、TranslationService、WorkshopContentInputUi 的输入实现、Abstractions Input/ConfigMenu、GameBridge language/input。

**切片：** ① config schema/migration/validation 与幂等读取；② keybind/scope/focus/suppression 的 owner 语义及冲突诊断；③ 参数化译文、回退、语言变更通知和可见 UI 刷新。复用现有服务，新能力走可选入口，不改旧方法含义；参数/新配置结果 DTO 在切片中形成已验证草案，正式 SDK 交付由 PN-007.a/020 完成。

**异常与退出：** E03/E07/E08：坏配置与迁移失败保留原件、重复 Read 不重复变换；UI 焦点/按住返回标题/同时注册/停用后输入不残留；缺 key/参数/语言、运行中切换语言。托管测试之外，真实键盘和已有支持的控制器路径、菜单/游戏 scope、语言切换须可见。未测设备保留限制，不泛称全控制器支持；回滚新增注册和 UI 资产，保留配置。

F01/F02 已由 PN-014 短修复负责，此处不再重做；F06 的旧 Action 每次读取可能执行，保持旧契约并为新版本迁移提供输入/输出版本、候选验证与未来 schema 保护，不能给同一旧 Action 暗增“一生一次”含义。

### PN-007：M2 公共 API target 与 SDK 完整候选

**依赖：** PN-009、PN-018、PN-019。**设计：** AD-06、主架构 A03。**输入：** target catalog/payload contract、SDK templates/build/pack、Runtime/Doctor marker reader、API matrix、PN-005 ABI corpus。

**切片 a（候选）：** ① 整理已实现 M2 service 契约草案/稳定性/最低版本；② 非公开候选 payload、模板与示例，供 PN-020 实测；③ 验证 package target 组合与新旧 reader。具体采用隔离发行 staging 内的候选 catalog/contract，复用现有 available 读取规则指向已生成的候选载荷，让候选 SDK 仍用正常 CLI 编译/打包；源码及普通发行 catalog 保持 planned。候选有准确 hash、版本与报告，但尚无公开不可变承诺，不增加新的信任 schema 或普通 CLI 绕过开关。禁止手改外部测试工程引用 Runtime DLL 来补齐候选。旧冻结载荷不变；本卡不强制反射，反射归 PN-021。

**测试/退出：** E01/E02：受控非公开候选在仓库外编译新服务，普通 SDK 的旧 target 对新调用/未知 planned 目标仍明确报错；旧项目/consumer/helper implementer 在新 Runtime 执行。检查缺/篡改载荷、错最低版本/marker、确定性和实际候选包。PN-007 候选交付即可解锁 PN-020，不以提前不可变冻结制造循环；PN-020 实测通过后经 R2 复盘，才在本卡最终切片冻结新 contract/hash、标 available 并选择 default。候选/可用/发布分开，已发布 ABI 不删除。

### PN-020：M2 基础平台产品验收

**依赖：** PN-007.a 候选。**场景：** E01/E02/E03/E07/E08。两个独立外部 Mod 使用 context/scheduler/command/config/input/i18n/global data，执行正常流程与故意失败；不以本仓库第一方产品替代。
在固定 Runtime/SDK 上验证后台→主线程、换档取消、owner 清理、输入焦点、语言刷新、配置迁移、全局数据重启保留和旧 DLL ABI。完整启动/标题/游戏/退出路径无剩余队列、订阅或线程。对只实现未测的单项保持缺口；通过后进入 R2 架构复盘，回到 PN-007 最终切片冻结/开放 target，再形成可消费的 M2 能力证明；随后进入 M3 原生/依赖边界。回滚测试部署与注册，不动玩家保存。

## M3：开放原生、协作与工程扩展

M3 先由 PN-036 将历史内部候选与新的 0.6.X 输入分开；M2 原产品证据保留。新包格式/构建协议由[包契约 P01–P07](../../architecture/platform-package-contracts.md)规定，步骤及真实语料见[执行包](execution-next.md)。R3.shared 和 R3.native 分别确认共享/原生格式，不互等；PN-023 合成后才产生首个完整公开 target 候选。

### PN-011：共享契约与纯 managed 依赖 DLL

**依赖：** PN-036（含已完成的 M2 前置）。**设计：** RT-05、AD-07、P01–P04；SDK 声明/编译/包检查，Core 预检与绑定，Doctor 同语义。详细切片/字段/真实失败矩阵见执行包 PN-011。
先显式本地引用与 shared/private inventory，不默认任意 NuGet restore。真实 assembly identity/hash/TFM 闭包在执行前检查；同 identity 异 bytes 不任选一份，registry 保持确切 Type；复制接口源码不算共享契约。
**进入/验收：** E04/E02：三个外部工程（provider/consumer/contract）独立构建、实际 Mono 互调；缺失/版本冲突/optional provider/循环依赖/加载顺序/关闭后调用可诊断。private 不能承诺任意版本隔离。旧包保持兼容；失败拒绝新包，不静默丢 DLL。

新版本格式纳入 F04：`1.2.0-beta.1` 不自动满足正式 `1.2.0` 下限；build metadata 不影响优先级。SDK/Core/Doctor 对同一语料同义判断，旧数字版本和可选依赖告警行为留兼容 reader；不因学习 SMAPI 而悄改现有包加载结果。

### PN-010：第三方自助 Advanced

**依赖：** PN-011/R3.shared。**设计：** 主架构原生通道、RT-05/RT-06、AD-07/P05；SDK 本地引用、Core 准入、GameBridge 共享事实；产品 Hook 在作者 Mod。按执行包 PN-010 验证并完成 R3.native。
先实验证明实际游戏引用/facade 下通用编译可行，必要时生成仅本机 reference surface，不为每作者手写 stub。新 schema provenance 与旧第一方 receipt reader 分开；任意 ID 自助，不要求 Catalog。禁止分发原生 DLL/资产，不把 provenance 称为认证；native drift 清楚报告。
**验收：** E01/E02/E04：新作者只读 native Mod build→pack→官方安装→Mono 调用→重启更新；旧 Advanced/legacy 继续正确分类；未知格式、错 build、Hook 抛错与依赖冲突隔离。先完成技术 prototype 后作 M3 复盘，再冻结新准入公共 schema。

### PN-022：扩展作者工程与 CI

**依赖：** PN-011。**设计：** AD-01/07、P06；SDK 规范化 BuildPlan、模板/依赖锁和测试 recipe。执行包 PN-022 已规定 ProjectReference、资源、生成源码输入、显式锁定 restore 与 CLI/IDE/CI 出口。
开放受控 ProjectReference、纯 managed library、资源与显式 restore 输入；每种依赖进入共同闭包/许可证/打包检查，不通过 IDE 可编译绕过 CLI。不先建中央 package server。基础离线 Strict 模式保留，扩展模式明确外部工具链/网络需求。
**验收：** E01/E02/E04：仓库外多项目工程在 CLI/IDE/CI 得到等价有效输入；缓存空/依赖缺失/不支持 TFM/生成源码/许可证缺失负例清楚。实际 Mono 调用辅助库。公开 CI 不带游戏 DLL/存档。

### PN-006：内部通用反射内核

**依赖：** PN-020、PN-007.b；独立内部技术实验可先行。**设计：** 主架构反射规范，Core BCL-only；游戏名称/Unity 存活在 GameBridge。
精确 Type+signature、field/property/method wrapper、Missing 与异常分离、readonly/泛型/ref-out/索引器等 V1 拒绝语义按主架构。不全局缓存实例，owner 清理主动释放 target。迁移 LanguageProvider 的 CurrentL10nId 与 ItemDisplayNameService 的 Title 两条只读调用；QueryItemProto out 调用暂留原适配。
**验收：** E02：私有基类/隐藏/重载/null/晚加载/同名跨 assembly、错返回类型、关闭与 GC roots；两条真实路径 Mono 行为一致。仅元数据缓存可先托管测试；不一轮替换 Harmony patcher。

### PN-021：公开反射与作者示例

**依赖：** PN-006。**设计：** 主架构反射规范、AD-06；Abstractions 契约、Core facade、SDK 新 target 增量。
公开 V1 精确签名/错误/线程/寿命与内核一致，Strict 反射自己的对象不宣称安全沙箱。只包含 BCL/DTMAPI 类型；包装器关闭后失效。按 target 流程交付，不回写冻结载荷，不为反射强制追改 M2 已发布 target。
**验收：** E01/E02：外部 SDK 工程执行私有成员/精确重载/Missing/目标异常，Mono 实测、旧 ABI/new target 负例、两类 owner/target 释放。初始稳定性按矩阵声明，不因测试通过自动 Stable；

### PN-023：M3 开放生态产品验收

**依赖：** PN-010、PN-022、PN-021、PN-036。**场景：** E01/E02/E04/E08。执行包 PN-023 规定同一 0.6.X 内部候选上的主旅程与真实启用/焦点证明；通过后才合成完整 API 0.7.0 候选，首发仍需 PN-031.a/033.a。
不改 Catalog 的全新第三方 Advanced，加独立 Strict consumer、共享 contract 和私有库完成真实互调；另测反射与工程扩展。覆盖新旧包、缺依赖、同 identity 异 bytes、游戏 build 变化、provider 失败/停用和更新重启。记录作者引用/打包/Runtime 实际绑定一致性，无未声明 DLL 偶然来自其他 Mod。接受条件是作者自助和确定失败边界；任何“需维护者补一个 ID/stub”均回到对应任务。此阶段完成后复盘类型绑定/原生兼容，再批量建设 ContentHost。

## SDK 正常作者路径增量

### PN-038：委托判据与原生泛型交付

**进入：** PN-010/011 的既有实现；外部 R01/R02 反例。**决策：** [P08](../../architecture/platform-package-contracts.md#p08可验证的原生泛型-v2)及 [execution-sdk](execution-sdk.md)。先修方法实现标志的掩码及合法委托判断，再以版本化原生 V2 统一 SDK 提取、打包、Doctor 和 Runtime；覆盖真实 TypeSpec/MethodSpec、宿主定义/参数约束及旧 V1。不能通过删掉原生验证或改写作者为非泛型重载完成本卡。

**出口：** E04.sdk 的正反例、仓库外真实库/Unity 泛型工程以及 Mono 调用、旧包和独立 Control。R-Generic 在同任务明确格式兼容、失败阶段和旧 reader。源码与 focused 通过可继续 PN-039；最后 Mono 可随 PN-031.a 同一候选集中完成，产品证明在此之前 pending。

### PN-039：常量、XML 文档与普通 C# 库

**进入：** 现有 BuildPlan/ProjectGraph；PN-038 与其共享输入变化已可集成，不等待最终游戏验收。**决策：** AD-11，详细见 execution-sdk。保留统一 BuildPlan，接受 csproj 的常量/XML 等低风险选项和有界普通 SDK-style netstandard2.0 库工程；普通库不需要 Mod manifest、UniqueID 或先改成专用工程。实际每个节点的选项、引用、资源和预生成源码都进入规范化输入。

**出口：** 常量真正改变行为、XML 含作者成员、CLI/IDE 语义与确定性字节、普通库 DAG/资源/诊断准确；外部原 csproj 可复用并在 Mono 实际调用。任意 MSBuild 导入、构建期生成器和全部 SDK 工程不是本卡承诺，错误必须指明不支持的输入及可行替代。

### PN-040.a：按实际所选资产恢复

**进入：** PN-038 委托判据的源码/focused 修正。**决策：** AD-12；本批保持现行 netstandard2.0 游戏加载规则。以选定框架/依赖/编译和运行资产判断包可用性，未选中的其他 TFM 或无关文件不再因目录名称一律误拒；适用的 build/generator/RID/native 要么被模型解释，要么准确拒绝，不能静默漏掉。

**出口：** 至少一个过去被误拒的真实多目标纯托管包、离线重放/锁定一致性及准确负例，SDK/Doctor/Runtime 依赖身份检查不降级。源代码/focused 可先完成，实际 DLL 绑定纳入最终 Mono。R05 仅完成本片，不能宣布完整 NuGet 兼容。

### PN-040.b：后续运行资产兼容

**进入：** PN-041 的标准恢复/运行资产集成；较低 TFM/facade 或其他确切运行资产的真实作者库与游戏 Mono/BCL 样本。不同字节 ref/lib 已吸收入 PN-041，不在本卡重做。与 M4 独立，不是 PN-024/013 前置。**决策：** AD-12 / AB-05 / R-Assets；不直接删除 exact-TFM 检查。先证明 facade/type-forwarding、运行闭包及宿主 identity，才按实际家族扩展现行规则；标准 NuGet 负责选择，不回到自有 resolver。

**出口：** 选定真实 NuGet 包经外部构建/离线恢复/打包/Doctor/Mono 调用与更新、旧锁/包 reader；较低 TFM 与现行 netstandard2.0 规则的例外在开放前写清。失败时保留有界拒绝和明确替代，不假称整个 NuGet 生态已成立。若需新增公共锁格式或支持承诺，只在 R-Assets 对增量作决定。

### PN-041：0.7.0 标准 MSBuild 完整迁移

**进入：** 已验 SDK r5、PN-038/039/040.a 与新 EditorConfig/框架常量/语言版本反例。**决定：** [AB-01–09](../../architecture/platform-sdk-build.md)；普通工程交给标准工具链，0.7.0 一次切换一个正式后端，不再比较是否添加可选路径。

**实施：** [唯一详细规格](execution-sdk-msbuild.md)分 a–f：标准构建/冻结引用→工程与标准恢复/诊断→准确产物/Native/pack→实际内部工程转换/IDE/CI→旧链裁剪/完整离线 SDK→新作者/真实 Mono/最终候选。与 PN-042 共用一份 Update，R 节点同任务完成；不开发通用旧 SDK 迁移器。低 TFM/facade 仍在 PN-040.b。

**出口：** B01–B12 必需证据齐备，CLI/IDE/CI、完整 ZIP/基础离线、普通工程/生成器/资源、ref/lib、Native、Doctor、当前内部输入/现有包、Mono 与排错通过；裁剪旧默认链，明确 debugger 实测范围。旧未发布 SDK 只作证据，不能代替首发接受，也不要求持续重建其全部历史工程。

PN-041.a 先完成 [V-Build](method-validation.md#v-build) 的薄集成/实际 IDE 骨架证伪，再扩大实现；不重新选择标准后端。与 PN-042 的兼容基线和必要修正并进，最终一次 Release/候选验收合并关闭两卡，避免重复全量测试。

### PN-042：0.7.0 现有 Mod 与接入方式兼容

**进入：** 准确已发布 0.6.1、现有 Mod 包/入口与 r5 候选；旧 SDK 是未发布内部证据。**规格：** [execution-compatibility](execution-compatibility.md)、[V-Compat](method-validation.md#v-compat)。

a 核对真实支持范围与固定旧包的来源/准入/绑定/行为；保留第一方 CSV 精确删除。只有新增退化才做 b 最小修正，无问题直接 c。与 PN-041 共用最终验收；有实际 Runtime 改动才生成新候选和两安装投影。不恢复 CSV，不造历史 SDK 用户支持。

**出口：** 旧 Strict、Advanced receipt/V1/V2、legacy、官方 Local/Workshop、混合官方 Content、外部插件共存与既有恢复方式按 C 矩阵成立。无需旧 Mod 重编、重签、改 manifest 或移动来源；新 SDK 工程迁移与旧玩家包兼容分开。源码/ABI 扫描不能替代真实旧 DLL 与原接入旅程。未完成不放行 0.7.0；不依赖未来 M4。

## M4：数据与内容平台

### PN-024：保存后端方法、身份与提交窗口验证

**依赖：** PN-008；原生实验可提前，与 SDK 无技术依赖。**设计：** D02、RT-07；[V-Save](method-validation.md#v-save) 与 [R4a 实验步骤](m4-experiments.md#r4a保存身份与提交窗口)。现有接缝/故障输入保留，尚未实测通过。

先比较官方扩展、现有同档字符串容器、受限序列化适配和 sidecar；最先证伪当前 dialogue variableStorage 在缺 Runtime、对话初始化/重存后的保留性。不要先实现完整侧车协调器。选定路径再做两 owner、新建/复制/删除/槽复用、备份回滚、原生失败/未保存、底层成功但外层异常和终止冷恢复。sidecar 被选时才要求独立 durable prepare、身份映射和相同 native 字节的 witness 全矩阵；同档方案不凭空制造外部提交阶段。

底层文件切换与外层返回分别观测，不能由 AfterSaveData/UI 异常推断磁盘未变。公开永久 SaveIdentity 和存储布局不先冻结；旧产品 reader/journal 与 SaveSaving 不顺手改变。

**验收：** R4a 先接受方法及作者/玩家成本，再以 E05 接受所选后端真实保存/冷恢复。模拟事件不算；证据不足暂缓公开写入，继续独立内容/Host。长期语义反证只重开 D02/RT-07。

### PN-012：事务 SaveData

**依赖：** PN-020、PN-024、PN-018、R4a。**设计：** D01/02、RT-07、PROJECT 保存规范；使用方法实验接受的后端，GameBridge 吸收 native 容器/提交接缝，Core 提供 owner/key/schema 服务。
实现读写删、迁移、未保存回退和随 native 成功提交；与 global/config 分开。同档承载不增外部 journal；选侧车才实现已验证的 participants/候选提升/unknown 恢复。未知新 schema/损坏数据不得默认清空后保存，故障可定位。GameplayMutation 与 Owner/OrphanRecovery 分流，不自动迁移 MoreEquipment。
**验收：** E05 实际未保存回滚、consume/break 不重复、正常保存保留、每个失败窗口无丢失/复制、换档/复制/删除/升级及显式恢复。故障/中断用 Steam AutoCloud 隔离 disposable fixture，普通保存沿 PROJECT 的授权专用槽条件；旧数据保留且可读取。产品证明欠缺时不开放通用写入。

### PN-013：ModContent 与 GameContent 方法及实现

**依赖：** PN-020。**设计：** D03/04、[V-Content](method-validation.md#v-content)。拆两段：a 只读包输入/资源/owner；b 官方原生内容→薄适配→确有缺口的受限编辑比较，经 R4b 后实现选定方法。a 不等待 b，可先供 PN-025；文本服务沿已完成 PN-018，不重复造 reader。
复用 [M4 实验](m4-experiments.md#r4b一项展示表字段与一种静态图标)的 stone 文本/自制图标和真实消费者。简单 owner 释放与共享租约按实际需要选择；GameContent 不预设通用克隆/传播图。IContentQueryHelper 保持索引语义。原生已有内容仍按官方方式运行；只有增加 Mod 随档状态才依 PN-012，原生自存内容沿自身保存验收。
**验收：** E06 双作者/官方控制组的组合、错误定位、启停/撤回、语言/场景与新旧消费者真实效果。选 callback 编辑时保留深层抛错隔离和 last-good 证明，测复制/纹理成本；不能隔离的类型不得承诺事务编辑，不能传播的范围明确需重载。先接受所选机制，再冻结对应公共格式/刷新规则。

### PN-025：可选 ContentHost 与 Pack 绑定

**依赖：** PN-011、PN-013.a 只读包输入/owner；原生 Host 另依 PN-010。只有内容编辑 Host 才依 PN-013.b；额外随档数据才依 PN-012。**设计：** D05、[V-Host](method-validation.md#v-host)。
先用一个普通 Host＋两 Pack 完成最小绑定原型，再冻结 Host ID/契约版本/schema 和双 owner 生命周期。Core 管发现/依赖/激活；Host 管领域 schema/解释；SDK writer/Doctor 共用版本语义。第一 Host 不强制做平台补丁 DSL。
**验收：** E04/E06 从仓库外真实 SDK 到 Host 消费/效果/撤回；缺 Host、错版本/schema、单包坏数据/Host 失败隔离。尤其验证受 Host 控制资源不会被官方 Content 扫描绕过门或双重加载；现有官方内容不强迫迁入 Host。

### PN-026：M4 Data/Content 产品验收

**依赖：** PN-012、PN-013.b 选定内容能力、PN-025、PN-023。**场景：** E04/E05/E06/E08；只读 Host 的独立交付不等待本合成卡。
固定候选上的独立 Data Mod、内容修改者、Host/Pack 共存。覆盖保存/不保存、关闭/换档、冲突/无效资源、包更新/停用/缺失恢复及所选后端故障窗口。数据恰好一次和可见内容均需真实证据，不以注册表非空替代。复盘已选保存方法、内容顺序和刷新范围，反证只重开相应 D 决策；不要求同档后端补做不存在的 sidecar 提升。

## M5：游戏领域、UI 与持久内容家族

### PN-027：领域 identity、查询与操作

**依赖：** PN-020；两个真实作者需求和选定 native owner。按 D06/RT-06 / [V-Domain](method-validation.md#v-domain)先只读 ID/snapshot/availability，再比较原生一次操作＋结果核验与必要的有限事务。只为实际额外存储/编辑追加 PN-012/013，不等完整 PN-026。GameBridge 管版本/线程/句柄，玩法留产品。E03/E05/E07 实测失效、容量/部分成功、重复/同帧变化及原生保存语义后冻结操作承诺。

### PN-028：作者 UI/HUD、输入与音频

**依赖：** 仅所用 PN-013.a 资源和 PN-027 查询切片。按 D03/D06/RT-04 / [V-UI](method-validation.md#v-ui)，两外部作者先比较现有菜单/原生控件薄适配与新增窄组件树；先固定 owner/焦点/关闭，再选控件/布局表达。E07 实测 HUD/交互面板、音频、翻译/分辨率/输入/场景和退出；不提前造 DSL/渲染框架。PN-037 已接受输入不重做，实体设备仍按实测晋级。

### PN-029：一个可持久实体 Host 家族

**依赖：** PN-025、PN-027、PN-023 和实际所用内容适配；仅额外随档状态/Host 自存实例需要 PN-012。按 D07 / [V-Entity](method-validation.md#v-entity)先对一个原生有限家族比较官方创建/原生保存、附加状态和完整自存模型，再交付选中方案。E05/E06/E07 必须完整创建/交互、真实保存/冷启、缺包/恢复/升级、失败创建与 consume/break，无丢失/复制。原生已存实例不得再重建一份；AI/平衡留 Host。

### PN-030：M5 产品验收

**依赖：** PN-028、PN-029。两个独立作者跨查询/操作/UI/实体写 Mod，执行 E03/E05/E06/E07/E08 的保存、更新、撤回、语言/输入变化及资源/性能检查。只接受实测领域，不宣称万能 API；复盘公共面和 GameBridge 耦合再进入下一领域。

## M6：发行、长期兼容与持续维护

### PN-031：发行、更新/撤回与支持

**依赖：** PN-020；新增域发布另要求其集成验收。按 AD-08/10 交付兼容组合、官方 staging、许可证/符号、清洁玩家安装→更新→运行→失败恢复和支持报告。E01/E08，先手动官方发布完整路径；上传另需授权，不依赖自有服务器。

拆 a 首发准备、b 后续能力的发行维护。a 依 PN-023/033.a，详见执行包，完成真实包/升级/诊断/支持材料与已知 GC 风险门后形成可发布 0.7.0 候选，不自动上传。b 随 0.7.X 的 Data/Content/领域升级扩充组合、恢复及报告；只验证新增承诺与受影响路径，不重建现有 installer/CI 基础。

### PN-031.multi：0.7.0 多平台玩家安装候选

**进入：** PN-031.a 的准确 Windows Runtime 已接受；当前多平台构建/host 仅接受 0.6.1 的反例。**责任：** installer/发布工程，不属于 SDK 或 GameBridge 改造；首发是否多渠道同时公开必须以实际候选说明，不把旧包重标为新版本。

1. 保留 0.6.1 的 published receipt、来源 manifest、旧 schema 和对应回归。增加准确候选来源分支，输入来自已验 Windows Runtime、已有构建来源与 payload 事实；版本/hash 由这一来源投影，不能删掉比较或把新 payload 伪称旧 Steam manifest。
2. 同步 PowerShell builder/common/auditor、C# PackageLayout 与现有多平台 package manifest/host schema 的明确版本分支。新 schema 如有必要只扩现有包模型，保留旧 reader；Candidate 来源记录源码/载荷事实，ObservedPublished 才能记录实际 Steam 发布观察。不新建手填准入/审批凭证。
3. 从真实提交构建 Windows x64 和 Linux x64 安装 host，更新相应 host artifact 证明；用独立输出目录导入准确 0.7.0 Runtime。五个主 DLL、compatibility 组件、BepInEx 和实际导入脚本逐文件匹配源；host 不进入游戏树，SDK 不进入玩家包。保持两个 Workshop 项的既有身份，构建候选不修改订阅/上传事实或旧分发目录。
4. 用现有 test-dtmapi-multiplatform-package.ps1 检查准确新包；用 test-multiplatform-runtime-installer.ps1 跑 Windows 和可用 WSL Linux 的临时游戏安装→status→日志→卸载，以及 0.6.1→0.7.0、撤回/重装、跨 installer 状态拒绝/已支持迁移。保留外部插件/配置、路径/链接、事务中断与错误来源检查；测试只扩受影响版本分支，不能拼接旧包 PASS。
5. 准确新包、两个 host、Windows/WSL 实际执行与未运行的平台逐项交回。Steam Deck/Proton/CrossOver 的实际游戏启动与输入独立证明；fake-game PASS 不等于新 Runtime 已在这些设备运行。不为打包自动操作真实游戏、云存档或 live upload。

**出口：** 能从既有准确 Runtime 重建并验收多平台 0.7.0，仍可审计旧 0.6.1；候选来源不混淆发布身份，两种 host 的安装/升级/恢复证据可复做。此卡与 M4 原生实验无依赖，不能以“完整 Release 已通过”跳过，也不需要重复 SDK 全套或 Windows 游戏长测。

### PN-032：CI、性能与支持平台

**依赖：** PN-020；后续域随合入扩充。已有独立测试图、SDK prepare 复用和公开源码 CI 不重复建设；补外部 Mod corpus、Mono/游戏/升级组合、调度/内容成本、日志/缓存/退出预算。E02/E03/E06/E08 按作者 OS/玩家 OS 实测路径大小写/Unicode/IPC/绑定/安装，不从 TFM 推断跨平台支持。

按 [V-Maintain](method-validation.md#v-maintain)先用既有工具证明可重放问题/实测预算，再按缺口加自动化；不预设新的中心实验室、在线遥测或每 build 完整 Bridge。低 TFM/facade 的方法比较归 [V-Assets](method-validation.md#v-assets) / R-Assets。

### PN-033：兼容、弃用与迁移

**依赖：** a 依 PN-020；b 依 a 与实际公开公告；c 依 b/R-Compat 和已公告窗口。按 AD-09/D08 分 API/二进制/行为/数据，不因 Runtime 0.8 或 target 0.5.5 一个数字删除所有兼容。

- a（0.7.0 前，详细见执行包）：扫描当前/retained 消费者，分清旧实现退役与 ABI 壳；迁移/不再提供能力的说明，Obsolete/Runtime 去重/Doctor 诊断、旧 DLL 控制组和发布说明草案。
- b（0.7 系列）：按 API matrix 逐族发布最早版本和日期，提供替代/回退；隔离候选预览物理移除，保留旧调用用例但预期变成准确拒绝，新样例必须成功。至少一个未回复作者/用量未知案例。公告事实只在实际发布后记录；未到窗口继续薄适配。
- c（0.8.0 起）：R-Compat 给本次精确集合；同步删除类型/DTO/provider/死实现、旧包加载前检测和错误引导，更新 SDK target/文档。扫描直接及传递 MemberRef，真实 Mono 的旧包拒绝、替代包运行、无关旧 target Mod 继续、数据保全/回退全部验证。保存 reader 或稳定 helper 的删除单独举证，不与 Frozen 壳批量绑定。

无反馈不证明零用户，也不无限否决已公告清退。未齐家族顺延后续 0.8.X，已齐家族继续。现行 API matrix 与发行说明拥有名单/日期，不建另一张实时台账；R6 处理更长期稳定窗口，不阻塞早已可判定的 R-Compat。

### PN-035：持续作者文档与样例

**依赖：** PN-008；内容依对应域实际实施。起步、IDE/调试、API/依赖/保存/内容、发布/支持随能力交付，不重启 Wiki。E01/E08，每项教程有外部样例、真实命令和新手执行证据，不靠私有入口；明确候选/发布、稳定性和 Mono 限制。按当前交付范围阶段性退出。

### PN-034：稳定候选与长期承诺

**依赖：** PN-026、PN-031.b、PN-032、PN-033.a/b、PN-035。稳定核心不等待全部 UI/实体领域，也不要求先执行 0.8 的物理移除 c；候选若承诺 M5 能力，另追加对应 PN-030 证据。按实际承诺范围复验 E01–E08、旧 DLL/项目/数据与支持矩阵；架构复盘确认稳定面、支持/退役窗口、游戏范围与维护资源。未证明领域保留非稳定/不支持。交付可审阅候选与承诺，不自动上传；源码全绿不能替代用户/游戏验收。

## PN-036：内部版本与公开首发坐标整理

**依赖：** PN-007.b、PN-021 及本次用户版本决定。**完整规格：** [执行包 PN-036](execution-next.md)。保留旧冻结 0.5.5 和内部 M2/反射快照原字节，新建 0.6.2 起的内部 Runtime/SDK/target，公开 0.7.0 等到 M3/发行出口，0.8 留作清退线。版本、最低要求、模板/包、SDK/Core/Doctor 和新字节 Mono 组合必须一起验证；历史 PASS 不重命名。

## PN-037：控制器绑定与配置菜单导航

**依赖：** a 依 PN-019；b 依 a 和现有 ConfigMenu。**完整规格：** [执行包 PN-037.a/b](execution-next.md)、[D09](../../architecture/platform-data-content.md#d09现有配置菜单的控制器支持先独立交付)。

a 提供实验性可自定义按钮、录入/清除/冲突/neutral，AutoFishing 与配置入口两个消费者；b 提供原生菜单可达入口、完整焦点导航、控件编辑、确认取消与输入占用。默认 0.7.1，有证据且适合首发时可吸收进 0.7.0；不等 M5 通用 UI。

分别记录代码/无设备键鼠/实体设备结果。没有实体手柄不阻塞这两片实现及可行回归；公开仍 Experimental，按 E07.input 的玩家设备矩阵补验证，不能从没人报告问题或数字按钮注册成功推导支持。

c 是收到具体玩家设备反馈后的支持晋级，依 a/b 及 D09 的实测输入；按设备与 Steam Input 路径记录，不等待匿名反馈阻塞本批。没有实体正向完整旅程时保持 Experimental。

## PN-014：连续内部裁剪

**依赖：** PN-001，每片追加相应行为前置。保留五程序集；提取 session/source、加载计划、状态/报告，拆大服务职责。查真实消费者再删影子注册表/开关/Compatibility，已发布 ABI 不按行数裁剪。每片一个责任、行为测试和必要 Mono；结果一致、状态唯一、可单片回滚，不留永久双实现。

**近期优先切片 `config-correctness`（研究 F01/F02）：**

- 输入：Core `Json/JsonFile.cs`、`Services/ConfigService.cs`；当前 `tests/DTMAPI.Core.Tests` 编译图，`suites.json` 的 `BadConfigJsonIsBackedUpAndDefaultedWithTempFileWrites` 与 owner helper 回归。根因见[本次协调 Review](../../reviews/code/2026/20260908-0009-platform-plan-reconciliation.md)，无需实施时再开一轮 Review。
- F01 采用保留 DataContractJsonSerializer 紧凑输出、删除配置写入的手工 Prettyish 后处理；无剩余消费者则删除内部 formatter。保持值/序列化语义和原子替换，不引入新 JSON 依赖。格式可读性的后续改进不能先于数据正确性。
- F02 解析损坏与 IO/访问错误分流；只有原件备份实际成功才写默认。唯一备份名、失败可归因并保留原件，不能记录虚假“已备份”。不改变旧 migration Action 或新增公共配置接口。
- 必需测试：独立 JSON parser 的往返值等价（奇偶反斜杠、转义引号、逗号/冒号/括号/Unicode）；备份失败而主路径仍可写、同名碰撞、读取权限/短时 IO、默认写入失败、正常坏 JSON 恢复。可通过内部 IO/名称接缝注入，实际比较原件和备份字节，不能仅断言记录了异常。
- 在 Core suite 下加可单独选择的配置 focus 并接入默认 graph；由 `test-unit.ps1` 构建所选图，保留现有成功恢复和 owner 隔离用例。本卡源/IO 验证即可退出，不启动保存游戏测试；M1 外部配置流程负责真实采用。改动修正合法 JSON/数据保留，是内部可逆选择，不等待新 API 设计。
- 单独有界 Update 与 status 子项记录；完成该切片不把连续 PN-014 全部标 done，也不借机删除其他公开冻结面。
