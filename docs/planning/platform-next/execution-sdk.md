# SDK 审计后连续执行包

> 后继：本批 PN-038/039/040.a/031.a 与 M4 输入准备已完成。下文是原批次规格，包含当时“不替换默认编译器”的范围；新任务改按 [PN-041 完整 MSBuild 迁移](execution-sdk-msbuild.md)，不得把本页旧范围当成继续实施的限制。

- Lifecycle: accepted-design
- Role: 当前近期规格；覆盖 PN-038、PN-039、PN-040.a 及 PN-031.a 的新验收输入，不维护状态或候选 hash。
- Decision: [本轮审计吸收](../../reviews/code/2026/20260910-0002-sdk-author-path-plan.md)、[作者交付 AD-11/12](../../architecture/platform-author-delivery.md)、[原生签名 P08](../../architecture/platform-package-contracts.md)。
- Queue: [status](status.md)；版本与中期进入条件归 [roadmap](roadmap.md)。旧 execution-next 保留已完成切片的规格，不能覆盖本页新增反例。

## 顺序、范围与交回

当前工作区、新 Astra/high 任务执行：**PN-038（R01→R02→R-Generic）→ PN-039 → PN-040.a → PN-031.a（含 R04）→ M4 两支实验准备**。文档可随代码修正，最终只封一轮统一 SDK/Runtime 候选。R-Generic 支持后继续，不在每张卡完成时等用户“继续”。

本批不重构全部 Core/Bootstrap、不重新实现 M1–M3、不替换默认编译器、不发布 SaveData/通用 GameContent、不上传。PN-040.b、标准 MSBuild 后端原型、R4 写入/编辑实验有中期规格；不把尚未细化的公共产品无限追加到本批。

旧实施任务已经被用户暂停，不恢复它。保留当前未提交的执行流程、TemplateCreator 错误分类及负例测试改动；先读其 Update 和实际 diff，不覆盖或把它们算作自己刚修出的缺陷。Wiki/Microsoft/其他 artifacts 不随实现提交。需要发行来源提交时按实际输入提交相关工作，保持现有来源检查。

## PN-038：正常托管依赖与原生泛型交付

**目标：** 普通委托依赖可恢复、构建与交付；常用 Unity 泛型调用由 build、pack、Doctor、Runtime 一致处理。**归属：** 一份 SDK 正常作者路径 Update；共享原生格式遵循 P08。不把它们包装成又一层准入凭证。

### 1. 恢复器的真实方法分类（R01）

落点：LockedPackageRestore.RequirePureManaged、现有 project-graph/restore 测试和报告。

1. 固定外部报告的普通库/委托库反例，先验证调用确实进入 restore，记录具体错误；新实验根不能覆盖研究目录原结果。常见 JSON 包用准确版本/lock/hash，不能以缺 feed 或 SDK001 当复现成功。
2. Native 按 CodeTypeMask 等值比较；独立检查 P/Invoke、Unmanaged、InternalCall、非 IL-only PE。合法 Runtime 委托方法按 MulticastDelegate 的实际元数据形状识别，包括泛型委托；非委托上的伪 Runtime 方法、混合标志仍明确拒绝。校验不执行被检查库。
3. 诊断指向 package ID/version、所选资产路径、类型/方法及实际拒绝类别。保留 SDK701 这类外层语义，不把本来正常的用户输入变成 SDK999。
4. 正例：普通库、普通/泛型委托、Newtonsoft.Json 13.0.3 的 restore→offline restore→build→pack。负例：实际 P/Invoke、Unmanaged、InternalCall、Native code type、伪 Runtime 方法、非 IL-only；锁/hash/许可失败继续拒绝。负例断言命令、exit category、具体码和成员，不只断言非零。

**运行出口：** 准确作者包在 Mono 实际执行委托及泛型委托；常见库执行一个真实操作。Newtonsoft 若与宿主驻留库冲突，分别记录“误判已修”和“该混装被拒”，不得改名/改签名或放松驻留检查来凑 PASS；核查已有 Advanced 宿主引用路径并给准确示例，不能宣传所有 Strict JSON 依赖都可共存。

### 2. 原生签名完整路径（R02）

落点：Tooling.Metadata 的 NativeMemberMetadata、Shared NativeSignature/NativePackageContract、NativeProjectReferences、NativePackageVerifier、包 marker/schema、Doctor、模板和旧格式语料。GameBridge 只承接真实宿主观察；不把 Cecil 加成新的强制游戏 DLL。

1. 按 P08 实现 V2 的结构化类型/成员模型、规范键、严格 reader 和显式选择。当前 0.7.0 新 Advanced 生成 V2；旧 target 和保留 V1 包仍走其原解释。manifest/native 文件/marker 版本错配、未知版本或混合字段拒绝，无 legacy fallback。
2. 提取实际程序集中的 MethodSpec、TypeSpec、泛型定义及约束；不能仅取消 GetMemberReferences 循环里的异常。调用指令、函数指针引用/ldtoken 涉及的受支持 MethodSpec、签名中的构造类型、作者自己的类型参数和引用库都要进入完整扫描。真正不支持的 function pointer/vararg/custom modifier 等语法按 P08 提前定位，不静默漏掉。
3. SDK 与 Doctor 在磁盘元数据上比较；Runtime 在已批准宿主上比较定义和约束，并核对包中的类型实参与引用身份。预检期间不执行作者/依赖程序集，不借 MakeGenericType/MakeGenericMethod 提前加载作者类。宿主泛型定义未变时，不因编译时 BCL facade 与 Mono 表示差异误拒。
4. 将可交付性检查放在 build 完成前，共用 pack 的提取/匹配代码。build 能成功意味着当次引用与输出可以表达；pack 仍重验宿主/输出漂移。失败指出真实 DLL/类型/成员；有 PDB 源行则附行号，没有就明确只定位成员，不能假造行号。
5. 正例至少包含 GetComponent<Transform> 与非泛型对照、GetComponent<作者自有 Component>、泛型声明类型成员、嵌套/组合泛型和泛型方法实参、数组/ref/out 组合。特殊语法是否支持按 P08 清单，不把“不含泛型的测试”充数。
6. 负例至少包括约束变化、成员/重载删除、arity/参数位置错配、宿主 identity/实参引用错配、仅 marker 改版本、签名字段重复/未知、源码或宿主在 build→pack 间变化。用执行计数器证明拒绝发生在 Entry/静态初始化前。

**R-Generic：** Astra 在同任务审新格式与三端语料，确认 V1/旧 receipt/legacy 保留、闭合参数不促使提前执行、程序集来源不弱化，然后继续 PN-039。格式支持情况与缺口归该 Update；其 Mono 出口可合并到最终候选的真实组合验收，不能提前记产品 verified。

**最终 Mono 出口：** 来自公开 CLI 的新 V2 Advanced 与旧 V1 Advanced、Strict/原 retained helper、独立 Control 共存；真实泛型和非泛型返回同一预期原生对象，至少一个作者自有类型实参；资源清理/退出正常。改变真实必需宿主签名的负例只在可处置 host fixture 做，不改玩家原生 DLL。

## PN-039：作者可决定的工程选项与普通库

**目标：** 作者在正常 C# 工程位置声明常量/XML，引用一个没有 Mod 身份的普通辅助库；构建行为、报告与 IDE 一致。**归属：** 一份工程输入 Update；沿 AD-11，不新建构建服务。

### 1. 编译常量与 XML

- csproj 拥有 DefineConstants、GenerateDocumentationFile；不要求 JSON 中再填一份。支持字面值、Debug/Release 条件和常见的追加现有 DefineConstants 形式，校验标识符并确定性规范化；不执行任意属性函数。
- 归入 BuildPlan、IDE design-time 投影、编译身份与报告；未声明保持旧行为。语言级别、unsafe、自定义 targets/生成器等未开放项保持准确诊断，不将其与常量/XML 一概归为“工程危险”。
- XML 来自同一次 emit，默认与 DLL 同名、位于 SDK 拥有的输出目录；输出权限、失败保全、关闭选项后的陈旧文件处理清楚。可随相应库的文档/符号交付，不把整个 bin 目录塞进包。
- 用 #if 改变真实编译方法/结果证明常量生效；Debug/Release、条件追加、重复/非法值、同输入不同目录、CLI/IDE DLL/PDB 与 XML 对应都检查。XML 校验实际 API 成员和当前源码；编译失败不留下看似有效的新 DLL/旧 XML 组合。

### 2. 普通 SDK-style 辅助库

- 既有 SDK library 路径继续可用。增加受支持的普通 `Microsoft.NET.Sdk`、单目标 netstandard2.0、Library 子项目输入适配；通过父工程已有 ProjectReference 纳入同一 DAG，不要求它有 manifest、UniqueID 或 dtmapi.library.json，不重写原库 csproj。
- 子项目自己的程序集身份/版本及已支持编译属性来自其 csproj，不能继承主 Mod 的名字或隐式改写其编译语义。默认源码 glob 排除 bin/obj，支持明确 Compile Include/Remove/Link 及既有可表达资源；未覆盖的条件/import/target 指到真实子项目并给出外部标准构建后以 managedReferences 接入的路径。
- 继续 workspaceRoot、路径/重解析点、环、同名程序集、闭包与资源碰撞检查。普通子库默认 self-authored/private-managed；需要共享或第三方许可时沿父工程现有引用元数据显式声明。旧字符串 ProjectReference 保留，不能要求原工程自动迁移。
- 支持子库的传递 ProjectReference 和已支持的锁定包引用；纯库不需要虚构 Mod manifest。错误必须区分“普通库子集未支持”与“入口 Mod 缺 manifest”。
- 验收一个仓库外主 Mod + 普通库 + SDK 库 + shared contract，实际调用库、使用资源并生成 XML；原普通库可独立标准构建。对共同支持的编译选项比较行为/有效输入，不能把不同编译器默认偷偷视为等价。主工程 CLI 与 IDE 的 SDK Build 仍要求同输入同 DLL/PDB。

**出口：** 独立作者无需复制第一方布局；真实 Mono 调用至少一个普通库方法；传递 native 引用仍不能从 Strict 穿过。完整 MSBuild/生成器原型留 PN-041/R-AuthorBuild，不以增加大量专用语法替代一次架构评估。

## PN-040.a：本次明确选中的 NuGet 资产

**目标：** 解决 R05 中能够在现行 netstandard2.0 边界内准确判定的整包误拒；不是宣布通用 NuGet 兼容。**归属：** 一份资产选择 Update；沿 AD-12。

1. 复用 NuGet.Frameworks/Packaging 的现有目标与资产分组，先计算 compile/runtime/dependency/build/analyzer 等本次适用集合，再决定是否支持；报告选中/排除路径与原因。不能只删除目录判断。
2. 本批实际入包执行库仍须 netstandard2.0，入口/Runtime 约束不变。支持多目标包中明确属于不兼容 TFM 的 build/ref/lib 或占位资产不影响当前 lib 组；当前适用的 build/generator/native/RID 运行选择仍未支持就准确拒绝，不静默丢掉。
3. 纯 lib 或 ref/lib 确实为同一执行载荷的既有模式继续支持；不同字节的 reference assembly 与 implementation 的完整配对移至 PN-040.b。诊断明确“当前资产模型未支持”，不能误报为坏包/恶意代码。
4. 正反包涵盖多 TFM 无关资产、当前适用的 target/generator、缺 lib/仅 ref、ref/lib 相同及不同、传递锁定版本、哈希/许可/宿主保留名冲突。正例使用真实 NuGet 打包布局，负例核对具体选择结果与拒绝类别；离线重放不重新选源/版本。

**出口：** 正例 restore/build/pack 并从 Mono 调用所选库；R05.a 已解决的集合和仍未支持的集合分别说明。R05 整体不能因此标为 done。更低 netstandard 依赖还牵涉实际 BCL facade identity 和现行游戏加载目标约束，必须在 PN-040.b 用独立证据决定，不仅改字符串比较。

## PN-031.a：真实统一候选，而非补一份 README

继续 `20260909-0019`。R04 与旧发布审查 A1/A2 在这里收口；代码修正不复写到此再记一次实现叙述。

1. 更新 SDK 根目录版本/default target、Strict/Advanced 参数、依赖/泛型支持、常量/XML/普通库、资产限制、API 状态与迁移；披露 CSV 既有删除例外。作者必需入口随包提供或指向已验证的固定公开内容；源码内部历史链接不能伪装成独立 SDK 必读入口。
2. 文档优先投影既有 API/架构权威；不装进 Wiki/全部历史、不创建第二套可手改 API 状态。用实际解压 ZIP 检查本地文件与章节锚点；照 README 执行 Advanced 命令，明确合法 game-root。
3. 先用受影响 focused/阶段修到通过，再冻结实际代码与构建输入。tracked builder 生成独立目录的新 Runtime、SDK、样例/符号；保留 r3/r2 和旧 recipe，不覆盖同名路径冒充同字节。0.7.0 仍是未发布候选，实际公开仍 0.6.1；不提前冒称 0.7.1 已发布。
4. Abstractions 若没有改变，0.7.0 API payload 与 assembly identity 保持原冻结字节；Native V2/SDK 改变的是包/工具能力，不能无理由重生 API 载荷。比对新旧 artifact 的实际差异，TemplateCreator 已有错误分类变化也要计入；不再断言全部 SDK 执行文件相同。
5. 仓库外从新 ZIP 完成默认/显式 0.5.5 Strict、当前 V2 Advanced、旧 V1 包、新旧辅助库/资源/委托、代表性锁定包和共享契约。缺 feed、命令参数错误、SDK999 不得当预期功能拒绝。
6. 完成下节受影响验证、最终完整 Release、准确新 Runtime 安装/升级恢复及 Mono 组合；再按证据更新候选页、原 Update 和队列。发布技术结论限 Windows Developer Preview，新服务/控制器仍实验性。

## 验证与流程：只执行必要门

入口选项以当前工具为准：`tools/scripts/test.ps1 -List`。当前阶段包括 BuildAndUnit、RuntimeInstaller、ScriptContracts、AuthorSdk、ProductContracts、QaLifecycle、RetainedAbi、InstallerMatrices、Governance。`-Stage` 单段和 `-StartAt` 尾段只诊断，不能拼接成完整 PASS。

现有 SDK focus 可复用 `platform-project-graph`、`platform-native-contract`、`platform-package-dependencies`、`pack-build`、`platform-sdk-targets`，通过 SDK Tests 的 `--focus` 选择；新用例登记到这些真实 suite，不伪造 focus。不在全量入口带 focus；不同构建/测试不要争用输出与共享游戏。

| 变化/承诺 | 必须证明 |
| --- | --- |
| R01 方法分类/restore | .NET 元数据正反例、准确 public CLI/lock 离线重放；最终 Mono 委托/代表性库调用 |
| R02 原生 V2 | SDK/Core/Doctor 共享语料、旧格式/ABI、宿主漂移与拒绝前不执行；最终 Mono 真实泛型、作者类型实参和旧包共存 |
| PN-039/040.a | 实际工程与包输出/诊断，CLI/IDE 输入和字节；最终 Mono 的库/资源调用，可合并进同次组合 |
| 新 Runtime/SDK 发行 | 先局部修复，最终输入冻结后完整 Release 一次；准确包 installer/SDK 检查、真实已发布 0.6.1 升级/撤回/恢复与存档/配置保全 |
| 未改变的 UI/帧/长测 | 复用 r3 对应证据并注明边界；不因只换模型、文档或 DLL 总 hash 就重复一小时。若帧/关闭/保留根改变或 ISSUE-010 新反例出现，执行相应有界长测 |

完整 Release 使用现有 `tools/scripts/test.ps1 -Configuration Release`；已包含构建，不先跑另一轮全量 build。前提失败先修进程/索引/输入问题，回到真正失败的具名段。正式完整运行期间冻结源与文档，结束后再写结果，避免运行中改变必需证据索引。新 candidate 的准确 installer 检查可沿其打包阶段完成；不得拿另一个临时包的同版本号代替最终包身份。

普通游戏验证沿 PROJECT 的 NoNativeSave、共享锁和非存档资产恢复，不例行备份/还原存档。只需标题观察可用 `-WaitForManualExit -SaveSlot 0`；调用需要世界时才进入指定槽。Unity 泛型探针创建的 GameObject/组件要归 owner 并正常释放；不为实验写入玩家持久世界。失败注入用独立 host/package fixture。

## 本批末尾：M4 实验准备，不继续扩大 SDK

SDK/候选交回前，沿现有 D02–D05 整理两个下一步实验输入，放入既有 PN-024 / PN-013 任务规格或其唯一有界实验说明；只做源码/元数据调查，不在此发布写入/编辑 API。

- 数据：定位当前 build 的 DataPersistenceManager.SaveGame、底层存档文件替换、AfterSaveData、外层 DolocAPI.SaveGame/UI，以及 ArchiveDataHandle/ExtraArchiveData 的身份字段。列明 PrepareResult、NativeWriteOutcome、OuterCallOutcome、ParticipantCommitOutcome 四结果的观察点，复制/删档/槽复用判据、允许 fixture、故障窗口和恢复出口。不以槽号/mtime 或 outer bool 单独判断提交。
- 内容：定位 ModManager 的官方选择/JSON 合并、DolocConfig.Loader、DolocAssetCache.GetAsset/CheckAsset，以及一个展示表字段和一种静态图标的实际消费者。列明官方最终结果后的接缝、深层候选隔离、撤销/失效/活对象传播、两个编辑者的异常语料及不支持刷新时的准确结果。
- 分别给 R4a/R4b 的可执行实验步骤和尚未观测的事实；没有精确 native 输入则指出缺失及读取入口，不虚构签名。完整 MSBuild、低 TFM 依赖和实体控制器不成为两张实验卡的前置。

全部以上详细任务完成后统一交回准确候选、实际能力范围、保留失败与 M4 实验输入。只有真正的新长期承诺冲突、必要外部条件在独立工作完成后仍阻碍，或本页详细任务用尽时停止；不因 R-Generic、一张卡或模型切换自动停工。
