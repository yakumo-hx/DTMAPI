# 0.7.0 SDK 构建职责纠偏与完整迁移决定

- Lifecycle: `accepted-design`
- Scope: 用户要求先转向 SDK 架构设计，吸收 r5 专项研究与网页版 Pro 建议，完整规划 0.7.0，减少之后反复改架构。
- Baseline: 仓库 HEAD `98b04998` 及保留的当前未提交工作；准确 SDK r5 的功能事实沿 [0007](../../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)。多平台安装候选的新进展沿 [0008](../../../updates/2026/20260910-0008-multiplatform-070-candidate.md)，不能照搬上一轮“尚未构建”的旧结论。
- Result owners: [SDK 构建架构](../../../architecture/platform-sdk-build.md)拥有接受的目标；[PN-041 执行包](../../../planning/platform-next/execution-sdk-msbuild.md)拥有任务与验收；[本轮 Update](../../../updates/2026/20260910-0009-sdk-msbuild-plan.md)拥有资料迁入与文档验证。

Resolution 2026-09-10：用户明确此前 SDK 从未公开发布。[0011](../../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)将 0.7.0 作为首次 SDK 交付，取消本 Review 中对任意旧 SDK 工程的通用迁移和历史工具支持要求；只转换实际内部输入。标准 MSBuild 单后端、完整交付/IDE/Mono 目标保留，AB 与执行包已同步。

## 判断

接受标准 MSBuild 单一正式后端，撤销“以统一自有 BuildPlan 为长期基础，继续按需求扩充 csproj 子集”的方向。统一构建、固定引用和验证交付物继续保留；统一应建立在标准工程工具链上。不是重新开发 Runtime、公共 API 或 GameBridge，也不是删掉 SDK。

上轮 SDK 功能验收针对已说明的子集及修复仍然有效；这次新增反例说明该子集与正常 C# 作者预期之间仍有结构性差异。不能把“上轮验收通过”推导为长期架构合理，也不应把已修好的委托、Native 泛型、常量/XML、普通库重新写成当前未实现。

原先选择自有编译有便携离线和短期输入统一的收益，但不执行任意工程扩展的边界也意味着无法自然兼容标准扩展。前期没有充分证明这些目标必须靠自有解释器实现，工程生态成本被低估。此次是纠正取舍；有效的引用、metadata、包与运行成果保留，返工集中于普通工程解释和构建组织。

## 新材料的证据边界

| 输入/主张 | 本次核对与结论 |
| --- | --- |
| [网页版 Pro 原文](20260910-sdk-build-model-inputs/web-pro-recommendation.md)与[包内依据](20260910-sdk-build-model-inputs/DTMAPI-SDK-scope-evidence-20260910.md) | 用户材料；Pro 明确只读 r5 包，没有运行 Windows CLI/MSBuild/Doctor/Mono。采纳职责与风险分析，不记为新运行 PASS。原文的 sandbox 下载链接映射到本表的本地伴随证据，不改写原文 |
| [本地专项研究入口](E:/Python_project/SMAPIlearning/SMAPI_technical_study/reviews/2026-09-10-sdk-build-model/README.md)与 01–03、原始 _evidence/_probes | 完整审阅并对照当前实现；这些编译实验由研究任务执行，本规划没有重复运行。原始结果仍由该研究目录拥有，本地决定不得夸大其范围 |
| EditorConfig CS0168=error | r5 原日志 success/exit0 仍报 warning，标准构建失败；当前 BuildPlan 未接入该严重度。是作者质量规则失效，不是已证明游戏损坏 |
| 主 Mod NETSTANDARD2_0 | 标准方法结果 yes、自有编译 no；框架符号只对普通库补充的源码与结果相符。应交给标准 SDK；此前未证明实际 IDE 设计时显示，不冒称 IDE 错显已复现 |
| 普通 C#10 库被拒 | 当前 7.3/12.0 白名单与 SDK202 相符。是公开说明的工具子集而非这段代码被 Mono 禁止；也不由此承诺所有新语言特性可运行 |
| 两条路径同编译器 | 被核对的 Microsoft.CodeAnalysis.CSharp.dll hash 相同；不表示完整工具链、参数和引用相同 |
| 标准主 Mod 使用冻结引用，Rebuild DLL/PDB/XML 相同 | 支持“自有解释器不是确定性必要条件”。只证明相同目录/固定输入的原型；未证明跨机器/目录/OS、pack、Doctor、实际 IDE/断点或 Mono |
| 标准原型初次 Restore 失败 | 旧冻结 props 的 RestoreProjectStyle=None 是实际障碍；研究只覆盖属性完成原型。新架构应直接消费冻结引用，不能导入旧构建策略后不断补覆盖 |
| SMAPI ModBuildConfig | 本机 SMAPI 源码的标准 targets 扩展模式支持职责划分；Stardew 的 TFM、游戏引用、默认部署和调试宿主不能复制到 DTMAPI |

Microsoft 的[工程 SDK](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)及[构建扩展](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process?view=vs-2022)文档支持标准目标上接入专用任务；[确定性编译](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/code-generation)仍要求工具链及有效输入一致；[NuGet 资产与锁](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)提供普通依赖模型。它们说明可用机制，不是 DTMAPI 新后端的验收结果。

## 源码定位与保留成本

| 源码入口 | 结论 |
| --- | --- |
| `src/DTMAPI.AuthorSdk/CodeModBuilder.cs`、BuildPlan、ProjectBuildInputs、OrdinaryLibraryInput、ProjectCompilerSettings、ProjectGraph | 解释/组织/编译的职责迁出；新后端不重新组织另一套 Csc 参数。迁移 reader 可保留必要旧结构，直接 Compilation/Emit 和子集执行链退出 |
| `author-sdk/templates/codemod/__UNIQUE_ID__.csproj.template` | 当前覆盖 Build/Rebuild/Restore 并调用 CLI；仅令 CLI 启动 MSBuild 会递归。需要新薄集成，与冻结历史模板/props 分开 |
| CompatibilityAssets | 已能独立验证载荷并返回 API/BCL 路径，无须执行冻结 props。校验最终 Csc 引用仍必需，不能只验证目录 hash |
| LockedPackageRestore、PackageAssetSelection | 资产选择和恢复回到标准 NuGet；现有 hash/PE/许可/闭包校验保留，不重写通用 resolver |
| NativeProjectReferences、ManagedPackageReferences、DeterministicPackager、共享 verifier | 复用本机 surface、最终 IL Native 成员提取与包检查；输入改为标准构建完成后的准确结果/暂存，不能随便复制 bin |
| SynchronousCallbackInspector、SymbolInspector | 源码规则迁入实际 Roslyn compilation 的 analyzer；符号与会话能力保留，但新后端的实际源行/调试须重新验证 |
| ProjectValidator、marker writer | author schema 与 package marker 的耦合可拆；Runtime 不读 author.json。schema4 不应制造 marker4 或抬高旧 target 的 Runtime floor |

## 已作出的取舍

1. PN-041 提前到 0.7.0 首发前，目标改为完整迁移与一次正式切换。已发现的普通工程问题进入统一后端验收，不再分别修旧解释器。PN-038/039/040.a 的既有成果和历史状态保留。
2. 默认作者包携完整标准 .NET SDK 和基础离线依赖，接受较大体积，保留便携基础使用；SDK 仍独立发布，不进入任何玩家 Runtime 包。工具链可按 SDK 修订升级，不成为永久固定 ABI。
3. 新 schema4 只表达 DTMAPI 专用信息；普通工程信息只在标准工程声明一次。旧项目显式迁移、保留有效选项和回退；旧包兼容与旧源码重编行为分开。schema 与公开报告具体字段在 PN-041.a 明确映射和测试，沿已决定职责实施，不再另选后端。
4. 同次接通 generator/analyzer、自定义 targets、普通项目图、标准 NuGet 与不同字节 ref/lib。低 TFM/facade、RID/native 和卫星程序集属于后续**运行资产能力**，不能在未验证当前 Runtime 前一并承诺，也不能因此继续禁止普通构建工具。
5. Doctor 保持非执行。标准构建会执行作者 targets/生成器，需退役旧安全表述；精确产物检查保留，记录不等于安全认证。不建通用沙箱或远程构建服务。[Microsoft 的执行边界说明](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-security-best-practices?view=vs-2022)。
6. 一个实际 IDE 是验收门；shipping Mono 断点需真正调查，若宿主不支持，只限制断点承诺。不能用 PDB 代替实际源行/断点，也不能自动替换游戏程序。
7. M4 数据/内容实验仍有独立技术输入；本次工作优先收口 SDK 0.7.0。迁移完成后继续 SaveData、ModContent/GameContent、Host/Pack，不将平台建设长期困在工具链。0.7.X 增加能力，0.8 清退公共旧 API 的计划独立保留。

## 放行与重开边界

R-AuthorBuild 分为标准构建、准确交付、最终产品三次有界核对，均在同一连续实施任务内完成，不逐卡等待新批准。前两次通过可继续代码和 focused 验证，真实 Mono/完整 ZIP 是最终门。新后端失败修新后端，不以双默认、禁用生成器或删除检查换 PASS。

本次已经决定内部及可逆方案，没有必须先由用户选择的新增问题。未来若需要新玩家包格式、放宽游戏加载承诺、独立 debugger 运行环境或原公开兼容不能保持，带具体反证回到对应决策；不因为实现较多就重做整个 SDK 设计。
