# 0.7.0 SDK 标准 MSBuild 完整执行包

- Lifecycle: `accepted-design`；尚未开始生产迁移。
- Role: PN-041 的完整近期规格，包含原 PN-040.b 的不同 ref/lib 配对部分；不是可选后端小原型。
- Design: [AB-01–09](../../architecture/platform-sdk-build.md)、[本次审查](../../reviews/code/2026/20260910-0004-sdk-msbuild-architecture.md)。任务状态归 [status](status.md)，候选选择归 [release-candidate](release-candidate.md)。
- Previous: [execution-sdk](execution-sdk.md)的 PN-038/039/040.a/031.a 已验收，作为旧实现及反例的历史规格保留，不继续给旧工程解释器添加语法。

## 本轮目标与执行方式

**一次完成标准 MSBuild 迁移，贯通 CLI、实际 IDE、CI、打包、Doctor 和真实 Mono，以一个后端首次公开交付 0.7.0 SDK。** 用户确认旧 SDK 从未发布；既有 ZIP/工程仅作内部证据，不增加旧 SDK 用户迁移/客户端支持任务。默认包仍带完整标准工具链及基础离线输入。

当前已验 SDK r5 是迁移前基线，不能拿它的 PASS 放行新后端。已验 Runtime r5 与多平台候选也保留；SDK-only 改动不无故重建 Runtime。出现 shared verifier/reader 实质变化才重验并更新相应 Runtime 和两个玩家分发投影。实际公开 Runtime 仍以 Catalog 为准。

接手先读 PROJECT/current-state、status、本页和 AB 设计。本批 PN-041 与其 PN-042 兼容验收共用一份实施 Update，各切片在其中跟进，不为属性/fixture/没有发生的修正另建记录。每步实现和 focused 验证后继续，最终证据保持 pending，不反写成循环。a 的有界复盘支持后继续至完整 SDK 和兼容门结束再统一交回，不自动开 M4 或上传。

本规划已决定后端、工具链分发、schema 迁移和边界，不再要求执行者重新比较三种架构。只有实际反证推翻长期契约、必须改变玩家/公共兼容承诺，或必要外部条件在其他可做工作完成后仍阻塞，才交回决定。shipping Mono debugger 不可用仅影响断点承诺；不能把未承诺设备/IDE、无手柄或发布等待作为整个迁移的自动停工理由。

同步执行 [PN-042](execution-compatibility.md) 的现有 Mod/接入门：a 建真实基线，只有新增退化才执行 b，无问题直接进入 c；f/c 合并同一次最终 Release/游戏组合。CSV 是已获授权删除的第一方临时接口，保持精确删除例外，不做恢复实验、stub、实现或 UI。本批没有预设 Runtime 修改；确需修正时才在 e 最终候选前收口并更新受影响投影。

## PN-041.a：标准构建骨架与引用接入

**输入：** CodeModBuilder、BuildPlan、CompatibilityAssets、ProjectValidator、TemplateCreator、CodeMod 模板、冻结 target catalog/contract、SDK builder/common。工作区用 Get-DotNetExe 的 .NET 8 SDK；不从 PATH 换新版绕过兼容。

1. 保留当前准确 r5 SDK/旧工程/冻结 target 作为控制组。建立最小仓库外新工程与普通库，不改真实作者源码；新内部后端仅作迁移开发，暂不修改公开 SDK 材料声称已切换。
2. 创建 SDK `build/` 的薄 props/targets 与标准工程模板。删除模板 Build/Rebuild/Restore 覆盖；提供不启动编译的引用准备和已求值 item/result adapter。采用 AB-03 的进程边界处理 VS 与 dotnet task 宿主差异，不把 net8 CLI DLL 直接加载到 VS MSBuild。
3. 新 props/targets 调用 CompatibilityAssets 取得冻结引用，不执行旧 props 的 RestoreProjectStyle=None。准确 API/BCL 与本地 native surface 接入 ResolveReferences；DesignTimeBuild 引用与正式 Csc 一致，但不触发打包、安装或整条 build。
4. 新 author schema 4 只保留 AB-02 的游戏/交付输入，不新增通用 migration reader，也不在 Runtime 增 marker4。清点当前实际构建/测试/样例入口，将必要的内部旧输入映射到标准工程；未使用的历史输入保留作证据即可。主 Mod 的 manifest/程序集默认版本、TargetFileName 与 EntryDll 一致；首发只接受一个明确作者格式。
5. CLI build 启动标准工程并返回标准编译诊断、实际 SDK/MSBuild/Csc、project/config/TFM/output；工具链解析显式错误不 fallback。父级 global.json 与作者 SDK 冲突准确提示，不默默修改。新工程可指定 SDK 开发环境，非编译 CLI 功能仍可自包含使用。

**先行方法验证：** 按 [V-Build](method-validation.md#v-build)，在扩大 b 前以最小工程验证标准扩展点/子进程桥和实际 IDE 的设计时引用＋Build；同版本便携/已安装 SDK 的解析、global.json、包体和离线输入也先测。选择一个实际使用的内部工程做有效设置对照；不建设通用迁移器。这里不预做完整 pack/Mono，也不为比较另造后端。

**必要验证：** 标准 CLI 与直接 dotnet build 的最小 Strict/普通库成功；实际 Csc ReferencePath 为冻结文件；主 Mod NETSTANDARD2_0 为真、EditorConfig CS0168=error 失败、C#10 简单库成功。没有系统 SDK/错误 SDK/错 API/冻结载荷被改等失败在正确阶段报告；可多次 Restore/Rebuild/Clean，无递归与空 Restore。普通工具项目 net8 不被全图强制成 netstandard2.0。

**R-AuthorBuild.build：** 查实际求值/Csc、三项反例及 IDE 薄接缝，确认最小集成。支持便继续 b，pack/Mono 待后续不阻塞实现。若暂缺实际 IDE 操作条件，先做独立 b/c 并保留 IDE 门 pending；不得将自动化工具缺失写成 IDE 不支持，也不因未测而静默放行。

## PN-041.b：完整工程语义、标准恢复与诊断

**直接前置：** a 的新链可构建。**落点：** ProjectGraph/ProjectBuildInputs/OrdinaryLibraryInput/ProjectCompilerSettings 的调用替换，LockedPackageRestore/PackageAssetSelection 的构建资产职责退役，SDK analyzer、现有 SDK tests。

1. 由 MSBuild 处理 Compile 默认/glob/Include/Remove/Link、Directory.Build.props/targets、Directory.Packages.props、普通 ProjectReference/多目标辅助库、自定义 Configuration、资源、resx、AdditionalFiles、EditorConfig、Analyzer 和真实 source generator。普通库不要求 Mod manifest/库描述，也不继承主 Mod 的 checked/nullable/API target；工具图可采用自身 TFM。
2. NuGet 标准 restore 直接读取普通工程，生成/使用标准锁与 assets；撤掉依赖专用工程抄锁要求及 JSON 重复 packages/项目图。支持显式配置源和标准凭据 provider；日志、manifest 和支持导出不携凭据。日常 restore 与发布 locked restore 分清，明确离线模式和缺缓存错误。
3. 适用 build/buildTransitive/analyzer/generator 按标准语义执行或由作者标准 metadata 排除，不能因为目录存在而拒绝整包。捕获 build-time / compile-time / runtime 角色；不同字节 ref/lib 在本卡接入，优先用有许可、自控真实构建包验证，并加一个现成包对照。标准选择事实不能直接绕过实际运行闭包校验。
4. 游戏 TFM/宿主规则只应用于被选中交付的 runtime DLL；低 TFM、RID/native、卫星资源超出现行 reader 时指出具体资产和已知限制，不将 generator 的 net8 目标误报为玩家包违规。不在本卡偷偷拓宽运行支持或引入新包格式。
5. 将现有有效源码语义检查接到实际 compilation 的 Roslyn analyzer，保留 SDK203 已知直接 async-void 的 Error 及生成代码覆盖。标准 CS/作者 analyzer 严重度由 EditorConfig 生效；建议和必需规则分离。pack 固定启用必需诊断，禁分析器/必需规则或无法证明执行时，用标准 Csc 重新执行或明确拒绝；不能用 PE/Strict 检查替代源码检查。正反例含直接 lambda、method group、生成源码、正常同步回调和关闭分析器，不扩张成任意数据流分析。

**必要验证：** 两层普通库、一个真实 generator、一个自定义生成 target、一个作者 analyzer、resx/embedded/显式内容/AdditionalFiles 可组合；生成方法和资源在产物中确实可调用。配置条件、目录移动、增量新增/删除/改生成输入都改变对应结果。主 Mod 和普通库对相同标准属性的行为与标准工具一致，不要求不同默认值的工程字节相同。

恢复覆盖标准锁更新→locked→离线重放，空缓存错误、缺传递包、篡改 nupkg/资产、错版本、ExcludeAssets/PrivateAssets、build-only 包不进玩家闭包。保留合法委托/真实 Newtonsoft 的恢复正例，私带 JSON 冲突留给原样运行规则。此卡不再以“能被自有 PackageAssetSelection 解释”作为资产可用标准。

## PN-041.c：准确产物、Native 与完整交付链

**直接前置：** a/b 的输出与运行资产可取得。**落点：** DeterministicPackager、ManagedPackageReferences、NativeProjectReferences、共享 PackageDependencyVerifier/NativePackageContract、SymbolInspector/Doctor。

1. 标准 Build 和作者后处理结束后采集 TargetPath、实际 DLL/PDB/XML、运行资产及显式 Content/None 交付项；不同项目/配置独立输出。独立采集/封包目标依赖完整 Build 和封包前扩展，不单靠 AfterTargets 顺序。采用 AB-06 的一次暂存→最终校验→按既有同名冲突规则原子发布，不递归压 bin，不相信旧 report 的成功字样，不新增 no-build。
2. ref 不作为 runtime DLL 交付；从所选实现及它的依赖中验证实际 MemberRef/TypeRef 使用可满足，复用现有 metadata reader，包含泛型、继承/转发的代表例。缺运行实现或签名不满足必须阻止封包，不能通过删校验接受不同 ref/lib。
3. 从最终暂存 DLL 重新提取 Native V1/V2 的真实必需成员和 provenance。覆盖 Unity GetComponent<T>、AddComponent<作者类型>、List 泛型重载、HostJson、显式动态 requiredMembers；后处理新增 native 使用必须进入描述。主 API/BCL/Unity/游戏/平台 DLL 即使 CopyLocal 或内容项声明也不得进入发布包。
4. 保持旧包格式选择：旧 0.5.5 不被 author4 隐式抬最低 Runtime；current 0.7.0 可生成 V2；旧 V1/receipt/legacy reader 不改。构建结果、Native input digest、包输出摘要必须标明各自来源，不能伪造旧 BuildPlan 重放。
5. Doctor 从 zip/PE 做非执行检查，不调用构建。标准构建可成功而产物不满足游戏契约时，build/report 与 pack/Doctor 的失败阶段清楚一致；不将“普通工程被支持”说成“所有产物都能加载”。

6. 既有 CodeMod＋官方 Content 混合包的 JSON/PNG、路径和官方加载语义保持；迁移/pack 不强迫转未来 Host schema，不因新的显式内容 metadata 丢掉旧项目原本交付的内容。与 PN-042 的固定原包控制组作字节/路径对照。

**必要验证：** 新 Strict、shared/private、Advanced 经公开 CLI build→pack→Doctor；正常委托、生成代码、资源、不同 ref/lib 正例。反例覆盖 Strict 直接/传递 native、同 identity 不同 DLL、缺运行闭包/许可、晚期替换 API/BCL、后处理 native、错误 PDB、旧 XML、编译失败后残留 DLL、并发配置、复制期间改文件、越界/碰撞内容路径。失败保留旧成功包及非 SDK 文件；检查一个含恶意构建 target 的 ZIP 时 Doctor 不执行 target。

**R-AuthorBuild.delivery：** 核对实际 ZIP/Doctor 与 Runtime reader，无放松验证、无新凭证体系、无旧/new backend 递归或不明输出来源。只在新事实要求改变长期格式时重开对应设计；正常实现修正继续同一 Update。

## PN-041.d：内部工程转换、实际 IDE、CI 与开发回路

**直接前置：** c 可产有效包；内部转换/IDE 验证可在 b/c 准备。**落点：** 当前实际工程/模板/测试/构建脚本、Session/Deployment/Symbol、指南和 CI；不新增面向旧 SDK 用户的公共迁移命令。

1. 按 a 清单转换仍被当前构建、测试或样例使用的工程/生成模板，复用或一次性脚本处理即可；没有消费者的历史工程不为其建立兼容层。保留原输入/Git 差异，不全覆盖未知标准 imports/targets/global.json。转换失败不损坏原输入；不要求支持任意作者手改历史 schema 的所有组合。
2. 对这些实际工程保留有效 checked、nullable、LangVersion、常量、版本、资源名/包路径、库角色和 Native 意图，记录标准框架常量/EditorConfig/原先漏执行逻辑的行为差异。结果归同一 Update，不另造长期迁移报告协议。
3. 首次接触 SDK 的作者只创建普通工程，编译输入无需 JSON 双写；提供简单代码 Mod、多库/生成器和 Advanced 样例。普通库仍可脱离 SDK 编译，无游戏/DTMAPI 依赖就不需 target 注册；接入现有标准 C# 库无须转换成专用工程。
4. 在至少一个明确命名/版本的实际 IDE 打开新工程和已转换内部样例，验证设计时引用、条件分支、生成代码、错误跳转、Build/Clean/Rebuild、Debug/Release。比较同一 compiler/config 的 CLI、IDE、CI 输出；不以命令行 MSBuild 冒充实际 IDE。
5. 公开 CI 使用相同工程和受支持工具链，普通 Strict 不需要游戏/仓库私有路径；native 构建明确依赖合法本机宿主或自托管 runner，不上传游戏 DLL。区分 SDK 本身可复现与作者自定义生成器有外部输入，不伪造完整 hermetic 记录。
6. 复用官方 Local 的 pack/install/status/update/restart/withdraw/recover；普通 Build 不自动安装、启用或启动游戏。文档明确磁盘和当前驻留版本不同。验证 Debug 符号与源码映射，为 e/f 的实际 Mono 准备已知源码位置的 Entry/事件异常 fixture。

**必要验证：** 实际工程转换前后有效输入与代表行为，当前调用入口全部转用新链；冻结 API 字节保持。旧内部 ZIP/失败样本留档，不要求重建全套历史工程或运行旧 SDK 客户端。两个含空格/中文的绝对目录以确定输入重建 DLL/PDB/XML/ZIP；实际 IDE 证据仍为必需，不确定 target 不冒称可复现。

## PN-041.e：移除旧默认链、完整 SDK 候选与离线交付

**直接前置：** a–d 的集成可用。不是公开发布，先产生完整内部候选供 f 使用。

1. 移除旧 CodeModBuilder 直接 Compilation/Emit、BuildPlan 手工编译控制、ProjectGraph 编译 DAG/子集解释和旧恢复执行器；内部转换完成后无消费者的 author schema1–3/migrate-build 路径退出首发包。可复用 metadata/验证保留；Native reference surface 可继续用 Roslyn，不按“删除所有 Roslyn”误裁剪。
2. 新 build/pack、模板及 IDE/CI 全部选同一链，删过渡后端开关。旧内部输入给明确不受支持/新模板说明，不悄悄忽略字段或 fallback。旧候选/冻结 API 与现有 Mod 包 reader 按各自证据保留；未发布 SDK 工具不因此产生永久支持义务。
3. 更新 tracked build-author-sdk / prepare/check-author-sdk-release，纳入完整标准 SDK、基础离线源、build 集成/analyzer/模板/schema/IDE/CI 与首次使用资料；沿既有 inventory 校验来源/许可。不带游戏 DLL、全用户缓存或历史 SDK 用户迁移产品。
4. 从与仓库无关的解压目录，移除系统 dotnet/PATH 和用户缓存影响，断网完成基础 Strict restore/build/pack/Doctor；已有锁/缓存的扩展项目断网重放。缺依赖准确报错，不静默联网修复。工具链/引用篡改拒绝，完整包体积与所需磁盘空间真实记录。另在编译工具链不可用时，实测 ContentPack 创建/pack/Doctor 及非编译诊断仍按自身前提工作，不能在通用 CLI 启动时强制解析 dotnet。
5. 更新包内全部指南/链接、错误说明、示例和独立 SDK 发布流程。标准构建会执行作者代码、普通 restore 的网络行为、离线条件、实际 IDE/断点范围必须坦率说明。没有真正切换前不修改作者指南来宣称已支持；本卡才同步当前使用说明。

**出口：** 无旧默认链入口；一份真实可解压候选含全部作者输入；源/工具/引用与 package inventory 一致。此时可以标 implemented，产品验收仍等 f。ABI/Runtime DLL 未变则不为 SDK-only 更新重发 Runtime；若 shared verifier 改动进入 Runtime，按实际闭包生成对应新候选并更新多平台导入。

## PN-041.f：最终作者/Mono 验收与默认切换放行

**直接前置：** e 的完整候选。本卡是正式切换门，不是第二轮架构设计。

1. 使用实际 ZIP，在仓库外用全新非 DTMAPI ID 完成 Strict、Advanced Provider、Strict Consumer/shared contract 和普通库/生成器组合。旧 ABI consumer/implementer、旧 target 0.5.5、V1、receipt、legacy 为固定旧二进制控制组，不能用新后端重编来冒充旧包兼容。
2. 持共享 Runtime 锁，按 PROJECT/产品验证流程通过官方 Local 来源、官方启用和 Steam 启动实际候选。默认 NoNativeSave；本次构建迁移不需要制造原生保存事务。运行普通库/委托/生成方法/资源、跨 Mod shared type 调用、Native V2 泛型和作者类型、HostJson；保留 JSON 驻留冲突等必要反例及无关 Control 存活。
3. 分别观察 Entry 与事件错误的真实源码位置、owner 日志、关闭/释放、正常退出、改代码后安装→必须重启→新驻留指纹与行为。不能继承旧 r5 PDB 行号 PASS，因为新后端生成的符号已变化。
4. 用受测 IDE 实际检查 shipping Mono debugger 条件；能 attach 时验证断点命中、局部变量和单步。不可用时记录技术原因和准确支持出口，保留源码行/日志/会话；不能只写“未尝试”便把 debugger 说成不支持。独立 debugger 游戏运行环境不在本批自动实施。
5. 局部失败按具名行为修复/重跑必要场景。全部已知问题修正、最终源码和输入冻结后执行一次完整 Release；该入口已含 build，不先跑另一轮全量 build。不把 Stage/StartAt 的诊断结果拼成完整 PASS。准确最终 SDK 输出若与预验产物相关字节不同，补相应作者/Mono 验证；内容一致时通过来源/hash 复用，不仅看版本号。
6. R-AuthorBuild.release 用下表逐项作最终接受或缺口判断，更新本 Update、status、release-candidate 与作者支持范围；选定新 SDK 候选，历史 r5 保留。不上传、不把旧 ABI 样本推广为“所有既有 Mod 完全不受影响”。

**连续执行终点：** a–f、PN-042.a–c 及必要返修完成，标准后端、独立 SDK、准确新作者/旧包/现有来源、Mono 与发行证据齐全后统一交回。最终验证合并，未变化证据复用。后续 PN-040.b 及 M4 按方法验证路线继续，不能又把普通生成器/属性排成下一批补丁。

## 0.7.0 必需验收矩阵

| 编号 | 产品承诺与最低观察 | 层级 / 不可替代证据 |
| --- | --- | --- |
| B01 工程语义 | 三项原始反例；Directory.Build/Packages、两层库、标准条件/Link、自定义配置、多目标库选择 | 实际作者工程、标准诊断及方法结果；源码结构断言不足 |
| B02 生成/分析/资源 | 自有 target + 真 generator + Analyzer/AdditionalFiles + resx/嵌入资源；生成输入改动参与增量与错误定位 | CLI/实际 IDE 输出；至少代表生成方法/资源在 Mono 调用 |
| B03 工具链/离线 | 完整 ZIP、无仓库/游戏/系统 SDK/用户缓存，基础断网 build/pack；扩展锁/缓存离线；缺失/错误工具明确 | 外部子进程及真实文件/网络条件，不能靠维护者 PATH |
| B04 三入口 | CLI / 实际 IDE / CI 同项目、compiler、配置、引用；Clean/Rebuild 与设计时正确，无递归 | IDE 操作证据与标准构建记录；命令行假装 IDE 不算 |
| B05 资产/锁 | 标准 NuGet 语义；工具/ref/runtime 三分、不同 ref/lib、合法委托、缺失/篡改/冲突失败 | 准确包、Doctor 和代表运行调用；restore 成功不等于 Mono |
| B06 引用/Native | 冻结引用不漂移；Strict 直接/传递拒绝；V2 泛型/作者类型/HostJson、旧 V1/receipt | Csc 引用 + 最终 PE/Native + 实际 Mono，不手写凭证 |
| B07 最终包 | 后处理后准确 DLL/PDB/XML/内容、配置/并发/取消/陈旧产物负例、原子替换/旧包保全 | 真实 CLI pack、文件比较；不信 bin/report 或只看 manifest |
| B08 内部转换/旧包 | 实际使用的内部项目/fixture/脚本转新链，必要设置/行为对照；冻结引用不改 | 当前入口真实运行＋固定旧 DLL Mono；不要求通用迁移器或全部历史 SDK 重建 |
| B09 确定性 | 固定工具链/引用/生成输入，同目录及两绝对目录、同配置 DLL/PDB/XML/ZIP | 实际 hash 对照；不承诺任意外部 target 完全可重现 |
| B10 作者游戏回路 | 官方来源/启用/驻留、普通/生成/资源/共享/native、失败隔离、owner 关闭、修改/重启/撤回 | 当前准确新 SDK 产物 + 实际候选 Mono，旧产品证据只能控制 |
| B11 调试范围 | DLL/PDB、Entry/事件源行、日志/会话；实际检查 debugger，attach/断点单独结论 | 实际游戏/IDE；断点不可用不伪造，也不要求替换玩家可执行文件 |
| B12 发行/裁剪 | 一个正式后端；完整离线包/指南/许可/inventory；最后准确候选完整 Release | 当前产物及完整运行；无上传事实、无新增设备支持推断 |

B01–B10、B12 任一必要正例失败或关键拒绝退化，均不能放行新默认后端。B11 的符号/排错路径必须可用，实际 throw 行、回调行、不可用分别记录；r5 既有 Entry 精确行和事件仅 callback closing line 的 Mono 限制不被偷换成所有 throw 精确行承诺。新后端仍须真实重验可用位置。有事实证明的 debugger 限制只限制断点宣传。某个未承诺 IDE/作者 OS 缺条件不扩为整个 SDK 停止；至少一个实际 IDE 是门。真实外部条件不齐时先完成其他独立项，最后准确列明未验项目，不把 implemented 记 verified。

## 验证入口与证据复用

当前 SDK 测试实际入口为 `tests/DTMAPI.AuthorSdk.Tests/DTMAPI.AuthorSdk.Tests.csproj`，`Program.cs` 已登记 `platform-project-graph`、`platform-native-contract`、`platform-package-dependencies`、`pack-build`、`platform-sdk-targets` 等 focus。测试图不是 tests/DTMAPI.AuthorSdk.Tests/suites.json。示例（需按受改行为选择一次）：

```powershell
. tools/scripts/common.ps1
$sdkTestDotnet = Get-DotNetExe
& $sdkTestDotnet run --project tests/DTMAPI.AuthorSdk.Tests/DTMAPI.AuthorSdk.Tests.csproj -c Release -- --focus platform-project-graph
```

新行为加入对应真实 suite 或登记必要新 focus；更改后的测试先实际运行，不能把未来 runner 当现成命令。内部构建单元用现行 tracked graph；作者验收另用准确 SDK ZIP 的公开入口。最后集成入口是 `tools/scripts/test.ps1 -Configuration Release`；先局部排错，不每个子阶段全量跑。SDK 检查用 `check-author-sdk-release.ps1 -PackagePath <准确ZIP>`。

Runtime 未改变时保留其安装器、旧 UI/帧和长测证据；新作者 DLL 的 Mono 调用/符号不能复用为已测。若修改共有 verifier/格式，则具名补 SDK/Doctor/Core 语料及受影响游戏/安装组合，重新投影准确 Runtime 到两个玩家包。NoNativeSave、锁、非存档资产恢复、测试临时目录和文档更新均沿现行流程，不加一套 SDK 迁移专用凭证或审核账本。
