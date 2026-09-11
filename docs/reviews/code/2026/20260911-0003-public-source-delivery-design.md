# 0.7.0 公开源码交付：固定边界、准确提交与贡献回流

- Lifecycle: `proposed`；具体设计已交付，导出器、入口调整和首次导出尚未实施。
- Role: 本任务的专项设计 / Review，拥有路径决策和同步算法；实施开始后冻结设计理由，由实施 Update 记录结果。
- Scope: [其他开发者支持计划](../../../planning/contributor-support.md)事项 2。事项 3 的 Linux 工作、platform-next API/内容队列、远端写入、发行封包及游戏操作均不在本次范围。
- Record: [20260911-0007](../../../updates/2026/20260911-0007-public-source-delivery-design.md)。机器可读附件为[规则草案](20260911-public-source-delivery/export-policy.draft.json)；它不是正在生效的发布规则。

## 1. 决策与准确基线

保留一套实现、一套 MSBuild 工程关系和一套测试分类。从指定 Git 提交的对象生成纯公开树，再与公开 main 做有明确基线的三方合并，通过普通 PR 更新。源码、工程、测试、schema 和版本文件按原路径原字节交付。内部分支及其提交父链不进入新的公开历史。

本设计把 Windows 的公开构建承诺固定为 `PublicWindows` 所选工程及其 MSBuild 依赖闭包。它覆盖 Runtime 源码、桌面 SDK/Doctor、声明的无游戏测试。第一方 Advanced 工程的真实游戏引用、Manbo 音频样例、发行包和完整 Release 各有自己的前提；不能把“所选工程构建通过”写成整个 solution、全部产品、离线首次恢复或玩家行为通过。源码仓库不要求游戏、Steam、内部历史、预先存在的 `.tools` 或 `artifacts` 才能运行这个入口。

### 1.1 本次读取时的事实

| 对象 | 已提交事实 / 工作树观察 | 对首次交付的影响 |
| --- | --- | --- |
| 当前提交与分支 | `cc7044a79ffd432ab2428d3d7e8f0d45b0a19d37`；`codex/workspace-construction-20260908` | 与任务给定一致；这只是观察基线，不是“完整 D7 源码提交” |
| Runtime 旧包 marker 修复 | `1002ae052deeeac9e4ad6641a279daa5ea4495f0` 已是 HEAD 祖先；`src/Shared/AuthorPackageMarker.cs` 的最近提交为该修复 | 不能把所有 0.7.0 修正都说成尚未提交 |
| 根构建入口 | HEAD 的 `build.ps1` 已用一次 `dotnet build DTMAPI.sln -m:1`，工程依赖不由脚本另排图 | 不需要重新发明 MSBuild 构建系统 |
| SDK 的新构建后端 | `src/DTMAPI.Author.Analyzers/`、`src/DTMAPI.Author.Build/`、`author-sdk/build/`、`Enter-DtmApiEnvironment.ps1` 和 `StandardBuildIntegration/StandardNuGetAssets/StandardPackageSnapshot/StandardSdkIntegrity/ManagedRuntimeSurface` 等当前未跟踪；AuthorSdk 工程、builder 和测试有未提交修改 | HEAD 不含标准后端的完整实现；从 HEAD 导出不会得到 D7 的开发体验 |
| SDK 旧模型清退 | `BuildPlan.cs`、`CodeModBuildInput.cs`、`ProjectGraph.cs`、`LockedPackageRestore.cs` 等删除及 author schema4、12 个产品工程/锁文件转换仍属工作树 | 必须作为同一源码变更集纳入选定提交；不能只加新文件而漏掉删除或转换 |
| 最近构建范围修正 | `build.ps1 -Projects`、`Invoke-DtmApiProjectBuild` 及调用方调整仍在工作树；记录归 [20260911-0004](../../../updates/2026/20260911-0004-test-failure-triage-and-build-scope.md) | 可以复用其单次 solution-filter 构建；不能把它冒称为 HEAD 或 D7 验收的原入口 |
| Catalog | `tools/release/dtmapi-product-catalog.json` 是 Core 实际嵌入资源；当前还有发布观察更新 | 这些改动会改变编译输入，不能仅因名称像文档就从来源摘要中删掉 |
| 验收 | [0012](../../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)和[候选页](../../../planning/platform-next/release-candidate.md)选择 D7/r6/r2；这些记录在工作树较新 | 复用其已验收范围；本设计不重开 D7/r6/r2，也不把本轮源码导出称为对原 ZIP 的重现 |

当前 `Directory.Build.targets`、根 `NuGet.Config`、`CONTRIBUTING.md`、`README.en.md`、`CHANGELOG.md` 和根 `licenses/` 并不存在。规则中的这些目标是明确的新建项或可选项，不能在文档中写成已可用。根第三方说明实际名为 `NOTICE.md`，继续以它为唯一根说明，无须再手写同义的 `THIRD-PARTY-NOTICES.md`。

### 1.2 首次选择提交的顺序

1. 维护者归集已接受的 SDK/MSBuild 输入及相应删除、产品转换、测试和构建文件到准确提交 `S0`。本任务不提交任何现有改动，也不要求清掉无关 Wiki、地图或规划改动。
2. 对照 0012 的既有 SDK 输入摘要、逐文件清单和 Runtime 来源记录，列出 `S0` 与已验收输入的差异。记录没有覆盖的文件不推断为相同；换行也要区分 Git blob 字节与当时构建输入字节。
3. 在同一内外源码路径实施本设计必需的公开入口、文档和许可调整，形成可导出的 `S`。不能把尚未提交的新工程从工作树补进 `S0` 的输出。
4. `S` 的公开 Windows 验证作为源码交付门。仅入口、测试选择、文档或许可变化不自动触发完整游戏/发行复验。若实际 Runtime 编译输入发生变化，按既有产品验证流程判定具名影响；保留原 D7/r6/r2 身份，不重包后仍使用原候选名。

导出旧提交可用于研究，但要标为该提交的真实范围。首次 0.7.0 正式源码候选还必须满足上述来源对应关系；不能因 `export` 返回成功就宣称它对应 D7。

## 2. 路径边界与实际依赖

以下是对当前树的交付决策，不是另一份工程图。精确机器规则以附件为准；`hold` 表示已分类但条件未满足，不能用通配 include 绕过。规则不按 ZIP 后缀或年份推断许可，也不从 `.gitignore` 推导公开性。

### 2.1 根与源码目录

| 路径 | 决策 | 理由、例外和实施要求 |
| --- | --- | --- |
| `DTMAPI.sln`、`Directory.Build.props`、`global.json`、`.gitattributes`、`.gitignore`、`LICENSE`、`NOTICE.md` | 原路径包含 | .NET 8.0.421 / patch 策略、版本 Import、路径映射和冻结文件换行有实际作用。保留 MIT 原文；修正 NOTICE 的 BepInEx 表述。Git 属性中针对未导出路径的规则只是惰性文本，不要求把那些文件带出 |
| 根 `README.md` | 使用固定根说明映射 | 内部 README 仍为内部入口；只映射公开版 README，详见第 3 节 |
| `AGENTS.md`、`PROJECT.md`、`.agents/`、`.codex/` | 排除 | 内部协作与治理；测试寻根不能依赖 PROJECT |
| `src/` 的实际工程、`Shared/` | 包含源码和项目文件 | 包括 Core、Abstractions、Bootstrap/Stubs、GameBridge/Compatibility/QA、ModConfigMenu、AuthorSdk/Contracts/Build/Analyzers、Tooling.Metadata、InstallDoctor、PlayerDoctor、MultiPlatformInstaller。游戏驻留程序集保持 `netstandard2.0`，桌面与测试保持原 TFM |
| `src/DTMAPI.ConsoleCommands`、`ContentPatcher`、`TemplateMod` 中仅有的占位 README | 排除占位说明 | 不把占位目录当成已实现能力；没有当前编译依赖 |
| `tests/` 工程、共享支持、`DTMAPI.UnitTests/Suite.props`、`SuiteEntry.cs`、`suites.json` 和 fixture | 包含 | suite 工程用 Link 编译产品/共享源码；不能只保留测试 csproj。ABI harness 可公开其自有源码，但实际私有 ABI lane 不在公开执行集合 |
| `products/first-party/` 的现有产品目录 | 包含自有源码、项目、manifest、schema4 元数据、锁文件、i18n、内容 JSON、QA 源码 | 当前 Compatibility/QA/test 工程直接 Link ActionSpeed、AutoFishing、DebugConsole、MoreEquipmentSlots、Mine、Zoom 等产品代码。`qa/` 里以 Evidence 命名的 C# 类型是生成/判定证据的源码，不能与实际运行结果一起排除 |
| 产品 PNG、`assets/branding/*.png` | 首轮排除，分类为待确认资产 | 当前声明的构建/测试不复制这些 PNG；发布包会用到 branding，源码门不承诺构造该包。以后取得明确来源/许可时按准确路径和摘要纳入，不把“第一方目录”视为素材授权 |
| `products/first-party/ManboCardboardAudio/assets/manbo.wav` | 首轮排除；完整 Manbo 工程构建为未满足的可选能力 | 其 csproj 明确 CopyToOutput；当前没有发现再分发许可记录。`PublicWindows` 根集合不选这个工程，其他所选工程也不引用它。若要承诺全 solution/Manbo 构建，必须先补许可并纳入原文件，或另作有明确行为说明的样例调整；不能添加 `Exists` 跳过复制来假装完整构建 |
| `archive/legacy-product-assets/` | 排除 | 已检查当前项目和构建/测试消费者，未发现声明能力依赖；`update-audit-package` 在介绍里列它不构成依赖。旧图片与 MoreSaves 说明留内部；不能据此推导所有旧 target 都可删 |
| `archive/second-motor-20260615/`、`archive/release-projections/` | 排除 | 退役实验和发布文本；不在声明工程闭包 |
| `author-docs/` | 首轮排除 | 现有内容实验说明不是当前 SDK 构建输入；这次不重写整套内容手册。以后公开独立指南时在唯一来源处整理 |

排除 WAV 不等于从内部删除或重新授权该素材。公开树保留相同的样例源文件路径；README 明示该可选工程需要未随仓库分发的音频，且不在 `PublicWindows` 中。`build.ps1 -SkipTests` 的整个 solution 模式因此不属于首次源码承诺。若实施发现任一被选工程实际引用这个资产，闭包检查必须失败，不能改成运行时缺音频的成功候选。

### 2.2 SDK、共享 Catalog 与文档例外

| 路径 | 决策与实际消费者 |
| --- | --- |
| `author-sdk/build/`、`Enter-DtmApiEnvironment.ps1`、`templates/`、`schemas/`、`ci/`、`examples/`、`samples/` | 原路径包含。新 props/targets 及 Build/Analyzers 工程必须来自同一 `S`；不得在导出中改写模板 Import 或降回旧私有编译图 |
| `author-sdk/target-catalog.json`、`bcl-reference-identities.json` | 包含。Core/Doctor/SDK 嵌入，准备脚本也消费；不能改默认 target 或删旧行 |
| `author-sdk/compatibility/0.5.5/`、`0.6.2/`、`0.6.3/`、`0.6.4/`、`0.6.5/`、`0.7.0/` | 包含合同、props、`source-build.json` 与完整 frozen `source/`。0.5.5 使用 DTMAPI 自有冻结源码重建准确 Abstractions；其余以各自 recipe 构造。准备逻辑按 target-catalog 发现，不在导出器复制一张目标版本表 |
| `author-sdk/advanced-reference-policies/*.json`、`*.Assembly-CSharp.reference.cs.txt` | 有限例外包含。SDK 嵌入旧准入策略/表面，Core/Doctor 嵌入策略；表面文件需逐项证明是有界、独立书写的声明 stub，无原方法体/提取资源。不能因为文件名有 Assembly-CSharp 就删除，也不能把新生成的官方 metadata reference DLL 当成同类公开材料 |
| `author-sdk/advanced-reference-policies/history/` | 包含当前 reader 实际需要的历史策略及说明。Core/Doctor 的 csproj 显式嵌入历史 registry/JSON；SDK 发布仍按原工程排除历史选择通道，不重开旧准入 |
| `author-sdk/internal-snapshots/20260909-m2-0.7.0/` | 排除。冻结的未发布候选及退役 builder，不在当前 target-catalog 的重建路径。只有历史候选脚本的引导文字指向它 |
| `author-sdk/licenses/`、作者指南 | 包含原文与指南；旧 schema 删除按 `S`，不在 exporter 按版本号自行裁剪。指南的纯内部链接在其唯一源文件一次修正 |
| `docs/api/public-api-matrix.md` | **必要公开文档例外**：AuthorSdk.csproj 明确作为 `API-STATUS.md` 的 Content 输入。保留原路径；先在源仓库将内部实施/证据叙事归回既有内部记录，保留公开 API 状态、限制和真实版本事实，然后统一生成 SDK 投影 |
| `author-sdk/API-STATUS.md` | 保留既有生成投影，禁止手改。`Get-AuthorSdkApiStatusProjection` 已实现从同一 matrix 投影；源码检查验其与源一致。导出只复制提交内已生成的结果，不在导出时做另一套 Markdown 替换 |
| `tools/release/dtmapi-runtime-version.props` | 包含；根 Directory.Build.props Import，Core 生成版本常量；不新增公开版本文件 |
| `tools/release/dtmapi-product-catalog.json` | **必要共享元数据例外**：Core 和 InstallDoctor.Tests 嵌入完整文件，源码测试/产品投影也读取。原字节包含，不生成精简版、更换 Runtime 资源或重新手写准入。其内部记录路径、相对产物路径和 `%USERPROFILE%` 模式仅是来源描述，不是公开构建要解析的依赖；相关证据/订阅快照本体排除。若发现凭据或真实机器私有值，停止并在源仓库修正，不能导出时打码 |
| `tools/release/dtmapi-mod-publish-zh.json` | 包含现有共享发布文案数据；Zoom/版本投影等读取。保留已公开内容，不授予上传权限 |
| `tools/release/current-subscription-manifest.json`、`baselines/`、`contracts/`、`batch4-production-qa-semantic-inventory.json` | 排除实际发布/迁移/保留 ABI 记录和内部合同；所选公共测试不能依赖它们。某脚本作为文本 fixture 被读取不代表要执行其完整历史验收 |
| `tools/release/dtmapi-multiplatform/`、`runtime-workshop/`、`runtime-workshop-v2-candidate/`、`player-*/`、`startup-capture-probe/` | 首轮排除发行与现场支持包装；MultiPlatformInstaller 的源码和原子事务 C# 测试仍包含。源码门不因此撤销既有多平台玩家包 |
| `docs/zh-CN/`、`docs/public-root/` | 前者为少量新增公开入口的唯一来源；后者仅储存根映射源，不把模板目录本身再复制一份到公开树 |
| 其余 `docs/` | 排除内部规划、Review、Update、月表、Debug、知识/Hook 研究和归档。公开构建不依赖文档治理检查器 |

Catalog 例外保留的是运行时实际消费的共享数据，公开检查不遍历它的所有 provenance 链。若将来确需拆 Catalog 内部事实，应另作正常的 Catalog/生成器设计，评估嵌入字节影响；本次不为清理目录引入第二份 Catalog 或 Runtime/API 改造。

### 2.3 第三方、二进制与 reference 的逐类判断

已按 [reference 边界](../../../../references/README.md)阅读现有分类；“已跟踪”“网页可访问”和来源链接都不自动证明可再分发。

| 材料 | 当前结论 / 交付条件 |
| --- | --- |
| `tools/release/bootstrap/BepInEx_win_x64_5.4.23.5.zip` | 必需的有条件包含例外。`prepare-unit-test-dependencies.ps1` 从它解出 Harmony/Cecil/MonoMod；7 个 Harmony/legacy fixture 工程实际引用其中 8 个 DLL。读取时整个 ZIP SHA-256 为 `82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4`，22 个成员，没有 LICENSE/NOTICE。原 ZIP 字节不改，许可包在旁边补齐 |
| BepInEx 自身 | v5.4.23.5 上游 [MIT 原文](https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE)已核对；NOTICE 当前声称没有 vendored BepInEx，与 fixture 不一致，必须修正 |
| ZIP 中的 HarmonyX、MonoMod、Cecil | [该 tag 的依赖表](https://github.com/BepInEx/BepInEx/tree/v5.4.23.5)给出 HarmonyX 2.7.0 / 2537257、MonoMod 21.12.13.01 / ede81f4、Cecil 0.10.4 / 98ec890；对应 [HarmonyX](https://raw.githubusercontent.com/BepInEx/HarmonyX/2537257/LICENSE)、[MonoMod](https://raw.githubusercontent.com/MonoMod/MonoMod/ede81f4/LICENSE)、[Cecil](https://raw.githubusercontent.com/jbevain/cecil/98ec890/LICENSE.txt)许可已核对为 MIT 类文本。实施时还要把本地整个 ZIP 与官方 release 资产身份对应，并覆盖 `0Harmony20.dll`、`HarmonyXInterop.dll` 等实际成员的来源/版权，不能只列测试选用的 8 个 DLL |
| 同 ZIP 的 `winhttp.dll` / Doorstop | 上游表指向 UnityDoorstop 4.5.0 / 33dab9a，其 [LICENSE](https://raw.githubusercontent.com/NeighTools/UnityDoorstop/33dab9a/LICENSE)为 LGPL 2.1。因此不能把完整 ZIP 视为全部 MIT。采用保守交付方式：在同一公开交付位置提供对应版本的完整源码及构建所需输入、许可和来源说明；仅贴仓库首页不作为补件完成。身份或对应源码不明时 export 失败。另一条可行后续是改为只恢复有明确许可的测试组件，但须统一修改内外 fixture 准备及消费者，本设计不默认实施这种迁移 |
| SDK NuGet、Newtonsoft、Cecil 与 .NET/NETStandard/Roslyn | 源码中已有 pinned PackageReference、SDK 锁与 `author-sdk/licenses/`。包恢复通过公开源完成，不导出本机 NuGet cache / nupkg / toolchain。复用[SDK 第三方说明](../../../../author-sdk/THIRD-PARTY-NOTICES.md)及其分发校验；Cecil 0.11.6 的 SDK 许可不能冒充 BepInEx ZIP 内 Cecil 0.10.4 的版本记录 |
| `tests/DTMAPI.UnitTests/Fixtures/oil-coal-drop-semantics.v1.json`、自有 native/Unity fixture C# | 包含。Oil fixture 明示为 DTMAPI 自有归一化概率输入，无官方表行/标识/反编译内容；自有 synthetic 类型编译出的 DLL 在测试期生成。不得从真实游戏抽样填充 fixture |
| `references/doloc-town/official-workshop-docs/` 的 PDF、爬取页面、图片、表格 | 排除。Git 已跟踪的副本也未因此取得再分发授权，当前公开工程不消费这些副本。可以在公开说明引用官方网站链接，不能整包带出 |
| `references/doloc-town/research-notes/` | DTMAPI 自有研究笔记，和官方副本分开判断；本轮仍排除，因为不是所声明能力的依赖，且不需要扩展研究资料审查。必要归一化 fixture 已在 tests 下独立拥有 |
| reverse/builds、own-mod-sources、third-party-mods、stardew-smapi | 排除私有官方字节、反编译/提取内容、旧实现和未授权 Mod/SMAPI 副本；忽略目录与错误跟踪的同类材料采用相同结果 |
| `tools/native-function-map/workbench/data/`、reverse/history 工作台数据 | 排除。符号/链接数据不等于官方源码，但不属于当前公开源码能力，不能为了保留既有 Node 测试而公开整套研究数据 |

二进制例外按**完整路径 + 内容摘要 + 成员清单 + 许可材料**锁定；更新依赖时更新这一处规则。普通版本不重复核对未变的整个第三方资料库。附件保留明确的许可 `hold`，并未把以上补件登记为已完成。

### 2.4 工具源文件与执行能力分开

公开交付保留同路径同字节的脚本。附件列出两组必要文件：

- 公开入口及助手：build/test、.NET 主机选择、fixture 准备、SDK 准备/封包助手、产品纯投影、公开选中的脚本测试。
- 源码 fixture：`run-game-smoke.ps1` 与 `game-smoke/**/*.ps1`、Oil 读取的 `install-to-game.ps1` / `check-product-catalog.ps1` / `release-common.ps1`，SourceContracts/QA 读取的 batch5、batch6、Candidate11 和 MoreEquipment runner。部分测试还对子进程的早期参数拒绝做断言，所需 `dump-governance.ps1` / 加载模块一并保留。它们是有真实消费者的源文件，不能只按“内部脚本”删掉。

这些 fixture 保留完整源码，不造一份截断/假 runner，不承诺它们在缺少内部材料时完成真实游戏或历史发行流程。公开 profile 只运行登记的 synthetic/source 分支，不调用安装、共享锁、原生保存或发行入口。原维护者入口照常保有全部断言和前提。

其余 `tools/scripts/`、`tools/workspace-history/`、`tools/portable-reverse-capture/`、`tools/reverse-capture/`、`tools/native-function-map/` 默认为已分类内部工具。新增 public consumer 找不到所需路径时，应补明确例外或修正公开范围，不能自动“把 tools 全带上”。

## 3. 规则结构与一个内容来源

正式配置拟放 `tools/public-source/export-policy.json`，本次只提交相邻 `.draft.json`。配置分为：格式/生命周期、路径安全、目录归属、精确文件例外、少量根映射、带许可的二进制输入。它不记录当前版本、工程依赖边或另一份测试项目清单。

### 3.1 确定的优先级

1. **无条件安全拒绝**：路径穿越/绝对路径、Windows 设备名/ADS/尾点尾空格、NUL、大小写折叠或 Unicode 归一化冲突、`.git` 内容、symlink/gitlink、LFS 指针、密钥/游戏私有载荷。无任何 include 可以覆盖。
2. **保留输出拒绝**：在拟包含范围内出现 `bin`、`obj`、`.tools`、`artifacts`、临时输出或本地 settings 等，报违规，不能忽略后报成功。整个已排除内部子树中的文件只记为排除，不导出其内容。
3. **精确文件 / 根映射**：精确例外胜过目录归属，例如共享 Catalog、API matrix、BepInEx ZIP。两个规则抢占同一源或目标、源/目标大小写碰撞、例外覆盖安全拒绝，均为配置错误。
4. **最长目录归属**：已知 component 下源码/测试文件继承该 component 的许可与类型约束；`author-sdk/internal-snapshots/` 比 `author-sdk/` 更具体，属于内部。映射源目录不二次复制。
5. **未知即停止**：新的根目录、新的 `src/<component>` / `products/first-party/<product>` / `tests/<suite>` 未归属，或已包含目录出现未知文件类型，Plan 列出，Export 拒绝。已有工程中新建普通 C# 子目录/文件可继承，不要求逐文件维护白名单。已明确排除区域中新建笔记仍属内部，不算未知公开材料。

配置采用路径段匹配，`tree` 匹配目录及其后代，`file` 精确匹配；不依赖 PowerShell `-like` 的大小写/通配差异。未知 schema 字段、重复 JSON key、非法路径、重复 ID、空规则、映射链或多对一映射拒绝。必需的精确文件缺失报错；目录 include 不强制每个目录存在，真正必需输入由映射和 MSBuild/测试闭包证明。draft/pending 规则只能 Plan，不能 Export。

普通源码新增只需正常 PR 审查其来源/许可；任何自动扫描都不能证明一段复制代码已获许可。新 binary、图片/音频、fixture、生成的原生表面或许可文件变化必须显示为需审查的变化。最终合并树使用同一边界检查，不能因文件由外部 PR 添加就继承“已审过 N”的结论。

### 3.2 根说明的固定映射

| 内部唯一源 | 公开目标 | 内容与回流 |
| --- | --- | --- |
| `docs/public-root/README.md` | `README.md` | 中文玩家/作者/贡献者入口、准确支持范围及链接；覆盖内部根 README 是唯一明确的同名目标替代 |
| `docs/public-root/README.en.md` | `README.en.md` | 简短英文入口及 Windows/实验限制；不维护整套英文手册 |
| `docs/public-root/CONTRIBUTING.md` | `CONTRIBUTING.md` | 干净克隆命令、测试选择、PR 要求、第三方来源和可选产品边界；外部贡献者不写内部 Update/月表 |
| `docs/public-root/CHANGELOG.md` | `CHANGELOG.md` | 只写已公开的用户可见变化；不复制内部 Update 汇编 |

这四项有一一反向映射，除此之外的 public paths 保持 identity。映射文档的相对链接以**公开目标目录**为基准验证；内部正文可从公开预览打开，内部通用文档检查不能擅自按模板目录改写链接。导出前必须在目标树核验所有公开入口、SDK 指南及源 README 的本地链接。内部证据路径只可在明确的历史来源说明中作为文本，不可作为读者必须点击才能理解的步骤。

`NOTICE.md` 和许可证同路径共用；现有 author-sdk 指南继续拥有作者命令与 schema 事实。API-STATUS 使用既有投影函数，其他公开文档不做导出时字符串替换。若 README 或 SDK 指南发生纯文风/链接修正，只做文档、链接和投影一致性检查；D7 ZIP 不因此重包。

## 4. 从提交对象生成公开树

### 4.1 命令接口

以下都是**待实现接口**，本次没有生成或运行这些生产脚本。一个入口 `tools/public-source/export-source.ps1` 分四个 action，不操作远端，也不隐式 checkout/stash/commit 调用方目录。

```powershell
# S 必须是完整提交 OID，OutputRoot 是新建的专用导出任务目录。
./tools/public-source/export-source.ps1 -Action Plan -SourceCommit $S -OutputRoot $job
./tools/public-source/export-source.ps1 -Action Export -SourceCommit $S -OutputRoot $freshJob

# 只读取已取得的公开仓库对象。参数都是固定 OID，不在中途重解 main。
./tools/public-source/export-source.ps1 -Action PreparePr -ExportDirectory $freshJob `
    -PublicRepository $publicClone -PublicMainCommit $P -ExportBaseCommit $E

# 首次没有 E，必须显式选择 Bootstrap；默认只给差异，不自动删除 P-only 文件。
./tools/public-source/export-source.ps1 -Action PreparePr -ExportDirectory $freshJob `
    -PublicRepository $publicClone -PublicMainCommit $P -Bootstrap

# 公共树检查器也供公开 PR CI 使用，不需要内部仓库。
./tools/public-source/check-public-source.ps1 -TreeCommit $candidate -PolicyCommit $policyCommit
```

| 输入 | 约束 |
| --- | --- |
| `SourceCommit` | 必填、完整 OID，`cat-file -t` 必须是 commit；禁止把未解析字符串、分支名或脏工作树内容当身份。源仓库允许有无关未提交修改 |
| 规则 | 必须读取 `S:tools/public-source/export-policy.json`；配置/工具实现的 OID 写入结果。不能运行工作树规则却把它称为 S 的规则。正式 Export 不接受工作树 override；设计阶段显式 `Plan` 才可用 draft，并清楚标记结果不可发布 |
| `OutputRoot` | 完整路径、专用新目录；不存在或为空。含任何未知文件、已有候选或 `.git` 时拒绝，无 `-Force` 覆盖开关 |
| `PublicRepository` | 已存在、明确指定的公开对象来源；只读取得 P/E 及公开祖先。不能是内部仓库别名或与输出目录重叠 |
| `PublicMainCommit`、`ExportBaseCommit` | 固定准确提交，检查 E 是纯导出链且与已成功公开合入相对应；不猜 E=`merge-base(P,N)` |

输出结构：`tree/` 为没有 `.git` 的纯导出内容，`objects.git/` 为仅含公开对象的独立 Git 仓库，`report.json` / `report.md` 为任务内可丢弃诊断，`candidate/` 为需要审查的合并树。报告记录 S、source tree、rule/tool OID、N tree/commit、E/P、逐路径 mode/blob/mapping/disposition、许可/闭包结果和失败原因。报告不进入公开源码、不成为同步数据库；成功后的长期来源关系由 Git 自身保存。

Plan 可以列出全部问题后返回失败；Export 必须通过静态门才生成可用的 N。失败时保留本轮 staging 与诊断，但不替换已有交付，不创建“完成”标记，不推进 E。临时目录由任务路径明确拥有，清理须验证绝对路径和边界，不递归删除计算出的任意上级目录。

### 4.2 对象级算法

1. 校验源仓库及 S，关闭 replace refs / graft / alternates 对来源解析的影响，拒绝 shallow 缺对象；不得从 git config 中执行 export filter、LFS smudge、外部 merge driver 或 hooks。公共合并仓库使用任务自有配置和空 hooksPath。
2. 以二进制流读取 `git ls-tree -r -z --full-tree S`。按 NUL 解析 mode/type/OID/path，不用逐行 `ls-files` 或 PowerShell 字符串管道传二进制。先分类所有 Git 条目，处理未知项和重复目标，再读取被选 blob。
3. 使用 `git cat-file --batch` 读取原始 blob 字节；不要先 checkout 整个内部树。每个普通条目的目标内容与源 blob 字节完全相同；四个根映射只换目标路径。保留 `100644/100755` mode；拟导出的 symlink/gitlink 等不支持模式明确拒绝。
4. 在新且不共享对象库的 `objects.git/` 中写这些 blob 和 tree。禁止 clone 内部仓库、复制 `.git`、使用 shared clone / alternates / hardlink 对象、把 S 设为 parent，或推送内部 refs。二进制重定向不能经过 Windows PowerShell 5.1 的文本编码。
5. 创建纯导出提交 N：首次无父提交；后续仅以 E 为父提交。N 的 tree 是规则作用于 S 的结果，不含额外公开贡献。提交说明写 `Source-Commit: S`、`Source-Tree:`、`Export-Policy-Blob:`、`Export-Tool-Commit:` 和 `Pure-Export-Tree:`。S 是来源文本，不是可遍历的父节点。[Git commit-tree](https://git-scm.com/docs/git-commit-tree)允许明确指定 tree 和父提交。
6. 报告同时列出源工作树相对 S 的 staged/unstaged/untracked 差异，只作未纳入输入提示。公开相关脏改动必须在首次交付审查中解释，不能被 exporter 自动夹带；无关内部脏改动不阻断对象读取。最终 tree 摘要绝不混用工作树 hash。
7. 从 N 物化干净验证目录。在公开 target 路径空间解释规则：四个映射目标算已归属，公开根 README 不会被“内部 README 排除”错误拒绝。再次验证路径、内容、许可、公开链接和依赖闭包。

重复相同 S/R/工具版本必须得到相同 N **tree OID**。提交时间、作者与父节点会影响 commit OID，所以没有必要承诺独立运行的 commit OID 相同；默认重用已生成且可验证的纯导出提交。ZIP 不是主要同步载体，如需附件则从 N 的纯 tree 生成，不在文件内插入“生成时间”或改版本号。

### 4.3 目标目录安全

输出绝不能与内部 repository、Git common dir、公开 clone、用户指定的输入目录，以及设置/脚本已解析的游戏、官方 MODS、Workshop/live upload 根发生相等、祖先或后代重叠。默认仅在专门的本地 scratch 父目录下创建新任务目录；不把 game auto-discovery、注册表猜测或联机 Steam 查询做成源码导出的必备条件。

沿目标所有已存在祖先检查 junction/reparse point，创建后、物化前再检查；候选目录中的链接/未知文件拒绝。检查的是解析后的绝对路径，不能只做字符串 `StartsWith`，须使用目录分隔符边界和 Windows 大小写规则。预留输出名和占用目录要以 `CreateNew` / 原子创建处理竞态。工具永远不向公开 main 的工作目录镜像覆盖，不使用 robocopy `/MIR`。

### 4.4 可复用与不可复用

| 现有工具 | 可以复用 | 不适用的语义 |
| --- | --- | --- |
| `update-audit-package.ps1` | 路径边界/reparse 检查及 staging 出错保全思路，可抽取通用函数 | `Copy-TrackedSourceSnapshot` 用 `git ls-files` 后复制当前文件；SourceCommit 只是标注；排除全部 ZIP；收集 docs/reverse snippets/运行证据。不能改名后充当 exporter |
| `common.ps1` | `Get-DotNetExe`、无副作用的路径/hash/安全 ZIP 基元 | `Get-DtmApiSourceCommit` 等工作区描述不是精确导出证明；不调用游戏发现/安装/共享锁作为正常源码前提 |
| `test-common.ps1` | `Invoke-DtmApiProjectBuild` 已通过临时 `.slnf` 做一次 MSBuild 调用、传播失败并清理 filter | 测试所选项目集合不是第二份依赖图；不得在 exporter 推导工程拓扑 |
| `runtime-build-source.ps1` | MSBuild 求值输入、目录边界、构建前后输入不变的做法 | Release 专属的干净提交/实际 runtime 输入门不是“copy 工作树即可标 S”的许可；不直接搬完整 Runtime packaging 验收到公共 PR |
| `author-sdk-preparation.ps1` / `author-sdk-compatibility.ps1` | 已有 target-catalog、source-build recipe、包完整性/输入检查 | 不复制 frozen source 清单到导出配置；不把本机生成的 compatibility、SDK `.tools` 或验收 ZIP 当可公开输入 |
| `author-sdk-release-common.ps1` | 安全解包、确定性封包与既有 API 文档投影 | exporter 本身不重包 D7、不新增文档替换器或一套分发收据 |

## 5. 普通 PR 持续更新：E、P、N 与最终树

### 5.1 唯一持久状态

| 名称 | 精确定义 |
| --- | --- |
| `S` | 本轮准确内部源码提交 |
| `E` | **上次成功合入后确认的纯导出提交**；稳定公开 ref 拟为 `refs/heads/codex/source-export` |
| `P` | 开始本轮比较时公开 main 的准确提交，含所有已经合入的外部贡献 |
| `N` | `export(S,R)` 的纯导出提交；后续以 E 为唯一 parent，首次为 root |
| `M` | `merge(base=E, ours=P, theirs=N)` 得到并经人工处理必要冲突的 tree |
| `U` | 更新 PR 的候选 commit，tree=M，parents 为 P、N；两条父链都只含既有公开历史或纯导出对象 |
| `H` | 远端实际接受的 merge commit；E 只能在核验 H 后推进 |

普通 Git ref、commit/tree OID 和提交 trailers 足够保存来源。`codex/source-export` 分支只保存纯树链；禁止把它当持续开发分支。更新 PR 分支使用 `codex/public-source-<S 的短标识>`，准确身份仍用完整 OID。U 含 N 作为父节点，使 N 可从正常公开历史恢复；不需要每版的手写账本、Git notes 数据库、同步守护进程或新服务。工具不得向外部传内部源码提交的对象闭包。

### 5.2 有 E 时的具体操作

1. 通常先把已合入的外部 PR 回流内部，测试相关行为并纳入 S；尚未回流也必须能安全比较，不能假定 P=E。
2. 记录并显示三份差异：`diff E P`（公开独有变化）、`diff E N`（内部新导出）、`diff P N`（两边当前差别），包含 mode、binary、rename/delete。规则版本变化也是需要审查的差异。
3. 在独立公开对象库运行 `git merge-tree --write-tree --merge-base=E P N`，使用 Git 的内容合并、重命名和目录/文件冲突语义。不可改成逐文件 newer-wins，或以自动计算的 ancestry merge-base 代替 E。[Git merge-tree 文档](https://git-scm.com/docs/git-merge-tree)说明该显式基线模式不要求 P/N 共享同一祖先。
4. exit 0 才是无冲突结果；exit 1 的首行也可能给出含冲突标记的 tree，不得把“取得 tree OID”当成功。其他退出码为工具失败。保存 stage 1/2/3、base/ours/theirs blob 和 rename 信息，供维护者在 scratch 中处理；禁止批量 `ours/theirs`。
5. 验证 M；用 P、N 建 U，PR 描述列准确来源、E/P/N/M、变更及验证范围。公开贡献的原始提交仍是 P 的祖先，不会被纯导出提交重写作者。

| 三方情形 | 处理 |
| --- | --- |
| 仅 P 新增文件；N 与 E 无该文件 | M 保留该公开文件；仍要检查其路径/许可/闭包 |
| 仅 N 新增；P 未改对应路径 | M 纳入；有同名 add/add 时由内容合并或明确冲突决定 |
| 两边修改不同内容且 Git 可合并 | 仍验证最终 M 的实际组合行为，不只验 N |
| 同一行、二进制两边修改、rename/rename、目录/文件冲突 | 阻断，显示两侧来源；人工决策后再验证 |
| N 删除且 P 自 E 未改 | 删除进入 M；检查是否仍有 Import/资源/测试引用 |
| 一边删除、另一边修改 | 阻断 modify/delete；不能把外部工作吞掉 |
| 一边重命名、另一边修改内容 | 采用 Git 的 rename/edit 结果；阈值不确定或未识别的 rename 作为增删一起审查，不保证语义猜测 |
| 新规则排除仍存在于 P 的路径 | 比较可能保留或报冲突；最终边界门必须阻断违规文件，要求明确迁移/删除决策，不能静默过滤 M |

### 5.3 首次没有 E

明确 `-Bootstrap` 并固定 P0。P0 不是可信纯导出；不能用空树自动合并后宣称无丢失，也不能用 `--allow-unrelated-histories` 让策略代替逐项取舍。

生成 P0 与 N 的逐路径清单：共有未改、两侧不同、P-only、N-only、重命名候选、旧路径删除。清单由 Git 生成，无需维护一份版本表。每个旧公开文件必须有审查结论：保留在 M、将贡献并入内部再重导、因边界/过时而在 PR 中明确删除。只要有未处置的 P-only/覆盖项，不能形成可合入候选。

最终 U 的 parents 为 P0 与无父 N，tree 为已审查 M。按普通 PR 合入保留 P0 既有作者与历史。首次成功后才创建 `codex/source-export` 指向 N。既有公开标签、旧分支和历史内容保持原状；新快照不清除过去已公开的文件，本设计不授权强推或历史清理。

### 5.4 main 变化、成功提交与恢复

验证身份绑定 `(P,E,N,M,rule OID,tool OID)`。只要合入前 main 从 P 变成 P′，旧候选无效：重新以同一 E/N 对 P′ 比较，处理新冲突，并对 M′ 完成边界/许可/链接及变化影响的验证。未变的构建输入可复用具名结果，不能把旧 M 的测试状态直接贴到不同树。

仅在客户端先读 main、紧接着点 Merge 存在竞态，不能声称这两个动作是原子操作。实施时使用仓库现有 PR 保护的“分支必须最新”或 merge queue，并让检查绑定实际待合入 base/tree；更新 PR 默认使用 **merge commit**，禁用此类同步 PR 的 squash/rebase 合入，保证 P/N 的可恢复关系。若远端无法保证准确 base，保持候选未合入状态，不改用直推 main。合入 API 的 source-head 校验也不能冒充 base 的比较并交换。

合入后 Finalize（独立的本地 action，远端 ref 更新仍由明确的发布步骤执行）必须核验：

1. H 可从当前公开 main 到达；U 确已合入，H 所接受的父/树与最终受检候选一致；若经过 merge queue，匹配队列实际受检版本。
2. N 可到达且其 tree 等于重算 `export(S,R)`；N 的父节点仅为前 E（首次无父）；其中没有额外公开贡献或内部父链。
3. 在[比较并交换 ref](https://git-scm.com/docs/git-update-ref)中执行逻辑 `update-ref refs/heads/codex/source-export N expected=E`；首次要求 ref 不存在。远端只更新该明确 ref，并以相同预期旧值检查；不使用 `--mirror`、`--all` 或通配 refspec。

**E 永远推进到 N，不是 M/U/H。** PR 未合入、测试失败、冲突、main/base 失配或 CAS 失败都不推进。多个更新候选可以存在，但同一 E 上第一个成功 finalize 后，其他候选必须重算；不覆盖已成功者。

若 H 已成功而程序在推进 E 前中断，可从 H/U 的 parents/trailers 找回 N、旧 E 和 S，重新验证后幂等 finalize。若来源关系不完整，停止并修复该 PR 的来源记录，不能从“看起来相似”的树猜 E。H 之后有无关公开提交不妨碍恢复，但要核验 H 当时接受的是准确受检树；这和合入前 P 变化是不同情况。

### 5.5 外部贡献回流

普通外部 PR 在公开仓库开发和测试，不访问内部提交。维护者按原提交顺序回流：同路径变更可 `cherry-pick -x` 或 `format-patch` / `am --3way`，保留原 Author/AuthorDate，并附原 PR URL/commit OID；内部冲突修复由维护者提交，不冒充原作者已经认可新的解决内容。

含四个根说明的提交不能直接 cherry-pick 到内部根 README。用同一映射表反向定位路径，仅转换 patch 的路径元数据；rename 的旧/新路径、删除、binary patch、mode 一并处理，文件内容不做替换。混合提交在 scratch 中组合成一个可审查的内部候选，保留原作者与来源；变更映射规则时还要读取此前 E 所绑定的规则，无法唯一反向映射就停止。不能再维护一份“公共路径→内部路径”手写表。

回流完成后正常构建/测试并进入下一 S。若 P 的贡献尚未回流，三方算法仍保留它；维护者应在 PR 中明确列出这类差异，而不能覆盖或默认删除。公开代码、工程和测试常规同路径，日常回流不需要上述文档映射分支。

### 5.6 最终合并树的门

N 通过只能证明纯导出。每个 M/U 及实际 merge-queue 树都必须重新运行：

- 同一可信规则的目标路径分类；新文件、额外第三方代码/载荷、许可材料及公开链接检查。规则本身变化先单独审查，不能让候选修改规则后自行批准自己的泄漏。
- MSBuild/准备脚本/测试集合的实际依赖闭包；检查 P 新增引用、删除的文件、生成资源和 NuGet 变化。
- 变化所需的公开 Windows 断言；CI 报告实际 runner/focus，不以 `dotnet test` 零退出码替代自定义可执行测试。
- 新引入的 Git ancestry/object 闭包，只允许 P 已有公开历史和经过验证的纯导出链。S 及其他内部 commit 不能成为新可达祖先。既有公开历史中的旧内容不在本任务中改写。

任何失败都保留公开贡献及诊断供修正，不通过最后再跑一次过滤器“洗掉”M 中的违规/冲突文件来制造绿色结果。

检查还要覆盖相对 P **新引入的祖先树/对象**。如果人工处理 M 时发现 N 中某项其实不能公开，仅从 M 删除还不够，因为 U 的父节点仍会公开 N：必须先修正内部唯一来源/规则得到新 S/N，再重新合并。未通过边界的中间提交不能作为公共候选祖先；既有 P 的历史则按此前公开事实保留，不在本任务中重写。

## 6. 公开 Windows 构建与内部验收的分界

### 6.1 一个测试分类来源

扩展现有 `tests/DTMAPI.UnitTests/suites.json` 为版本 2，保留现有 suite/focus 身份；由原 `SuiteEntry` / PowerShell 入口使用，继续运行现有 C# 可执行测试。新增记录可描述 `unit-exe`、`existing-exe-focus`、`powershell`、`build-only` 入口，字段包括 `id`、入口/项目、`distribution`、`platforms`、`requires`、`purpose`。MSBuild 负责 ProjectReference、Import、TFM、资源、analyzer、generator 和产物；分类只选构建根与现有执行入口，不重复边。

`PublicWindows` 为对这些属性的选择规则，脚本不得另有 `$otherSuites`、脚本数组、Linux 工程数组。未知 runner/focus、重复 ID、未分类新测试或空选择一律失败。现有 `default` / `focuses` 继续拥有 Unit 方法集合；把现有执行器的 focus 映射接入分类时保留实际方法和初始化/清理过程，不用反射重造另一套测试框架。

公开 profile 选 `distribution=public`、支持 Windows、无 `private-game/exact-retained-package/internal-record/live-game` 前提的 source/synthetic 集合。内部原有 test.ps1 各 stage 按同一分类调用原入口，保留其 Release/ABI/游戏/治理要求。分类文件可以含指向未导出内部 runner 的记录；公开 preflight 仅要求所选条目的入口存在，列出未选范围及理由，不能宣称这些条目通过。所有公开选中断言必须实际执行。

### 6.2 对当前入口逐项迁移

| 当前集合 / 行为 | 首轮新归属与必要调整 |
| --- | --- |
| 15 个默认 Unit suites：`moresaves`、`core`、`runtime-integration`、`compatibility`、`animalhusbandry`、`autofishing`、`autoharvest`、`chestlocator`、`moreequipment`、`strongplantinggun`、`zoom`、`mine`、`debugconsole`、`oil-content`、`source-contracts` | 保留 `PublicWindows` 的原有断言。它们不应因名称含 runtime、QA 或旧版本就被排除。SourceContracts、Oil、DebugConsole 的脚本源码依赖按第 2.4 节纳入；不删除这些断言以省文件。`source-arbitration` 当前无 default，仅保留原显式 focus，不偷偷计为默认执行 |
| `DTMAPI.QaUnitTests`、`DTMAPI.InstallDoctor.Tests`、`DTMAPI.MultiPlatformInstaller.Tests` | 保留无游戏默认执行。QA 验证自有状态机/runner source 和 synthetic 早期拒绝；MultiPlatformInstaller.Tests 当前检验合成目录内原子 receipt 写入失败保全，不是 Deck/安装矩阵验收 |
| SDK 当前 4 个公开 focus：`platform-session-handshake`、`platform-sdk-targets`、`pack-build`、`official-local` | 保留，准备同一 target-catalog 及 .NET 8 工具链；合成的 official Local 目录不是真实 MODS |
| SDK D7 标准工程/资源/字段回归 | 在同一分类加入 `standard-layout`、`standard-field-assets`、`standard-assets`、`standard-build`、`platform-project-graph`、`platform-package-dependencies`、`platform-native-contract` 和 `author-basics`。已核对 NativeContract/NativeGeneric 的宿主由测试源码生成；不把带 native 名称的全部 focus 归为私有。`standard-ordinary` / `standard-final-assets` 与 project-graph 的重叠保留为显式 focus，不重复当成默认独立运行 |
| SDK 默认尾部的公共契约 / 负例 | 为目前只有 full Main 才调用的公共合同、unsafe command、恢复等断言补一个显式公共选择，复用原方法；分类必须有 before/after 归属对照，不能仅运行上述 focus 后冒称覆盖全部公共断言 |
| `TestActualLegacySdkWhenProvided` 与 `advanced-policy` | 前者现在按 `DTMAPI_AUTHOR_LEGACY_EXE` 有无决定是否执行，后者需要准确私有 Advanced 输入。改为显式内部选择；已选择但缺输入必须失败。公开 session wire matrix 仍完整执行；不能把日志中的“未提供旧 exe”算作真实旧客户端通过 |
| `test-dotnet-toolchain`、`test-unit-routing`、`test-author-sdk-compatibility`、`test-author-sdk-preparation`、`test-workspace-preflight`、`test-product-projections` | 6 个现有脚本测试保留公开。均使用所带源码、公开准备结果或 synthetic 输入；脚本依赖按附件纳入。实际调用 Windows PowerShell 5.1 时必须列明 |
| `test-release-routing`、`test-test-focus-routing -EntryGuardsOnly` | 维护者 ScriptContracts / Release 路由；检查含内部阶段的 test.ps1，不是公开源码 profile 的前提 |
| `test-issue-index`、`test-audit-output-boundaries`、`test-document-archive-tools`、`tools/workspace-history/test_history.py` / `test_links.py` | 内部 Governance / 工具自身测试。保留原断言和调用归属，公开 PR 不准备内部月表、迁移清单或归档附件 |
| `test-player-save-repair`、`test-player-save-collector-slots`、`test-player-save-crash-collector` | 内部现场支持工具的 synthetic 测试，迁回其原工具/ScriptContracts 入口。它们不全是私有测试，但这些支持工具不在此次公开源码能力中；不能误记为删掉保存保护 |
| `test-game-smoke-save-modes`、`test-game-smoke-process-boundaries`、`test-game-smoke-modules` | 原维护者 ScriptContracts；保留测试早期拒绝、进程边界和模块拓扑。公共 C# 测试已需要的同源 runner/module 仍带出；不以此承诺公共 CI 完成全套游戏协议 |
| `test-reverse-capture-stages`、`test-reverse-baseline-path-safety`、`test-portable-reverse-capture-path-safety`、`tools/reverse-capture/test_compare_baselines.py`、`tools/native-function-map/test_workbench.cjs` | 内部 reverse/workbench 工具 lane。移出公开入口后无需在公开 CI 安装 Python/Node 或携带反向整理数据；原工具 lane 继续运行原断言 |
| 真正私有 Advanced/retained ABI、历史 package matrices、完整 Release、游戏 | 保留既有具名入口/阶段，不放宽前提，不作为新贡献者每次 PR 的必选项；改变对应产品行为时仍按产品流程选择所需验收 |

迁移时提供由同一分类生成的旧入口→新归属对照，确认每个现有条目有执行归属。未能满足公开闭包的条目明确失败并修正设计/输入，不能运行后捕获异常转 skip，也不能仅因“此机没有文件”临时移入内部。上表是迁移规格，实际分类只维护在 suites.json；本设计和导出配置都不参与运行时选择。

### 6.3 最小实现改动

1. **寻根**：复用/扩展 `tests/Shared/` 的辅助代码，用 `DTMAPI.sln` 加 `global.json` 作为稳定标记。已发现 Unit Program/SuiteEntry/RepositoryPaths、AuthorSdk、QaUnitTests、MultiPlatformInstaller、AutoFishing、ChestLocator 两处、StrongPlantingGun、Mine、MoreEquipmentSlots、Zoom、AdvancedRuntimeTests 中依赖 `PROJECT.md` 的查找；一起修正，不只改一个 helper。PowerShell `Get-RepoRoot` 已按脚本路径定位，无须另造内部标记。
2. **构建与准备**：`PublicWindows -BuildOnly` 从分类取源码根及所选测试工程，补登记 Bootstrap 和 PlayerDoctor 的 build-only 根；其他引用由 MSBuild 求解。用现有单次 `.slnf` 构建，不重写 .sln，不按层级重复 build。先用 `prepare-unit-test-dependencies` 准备 BepInEx fixture、按 target-catalog 准备 compatibility；这些都写新验证目录的生成区域。
3. **主机与环境**：所有 build 和 SDK 子进程使用 `Get-DotNetExe` 选出的 .NET 8 主机，公开入口按现有 Release 修正设置/恢复 `DTMAPI_AUTHOR_DOTNET`。不得使用 PATH 上的 .NET 9 或 Major roll-forward。`global.json` 是 SDK 策略唯一来源；Windows PowerShell 5.1 和 .NET Framework 4.8 fixture runtime 是明确前提。验证用独立 NuGet/cache/temp，不读取维护者 settings、私有 feed 或游戏目录。
4. **入口**：扩展现有 `test-public-source.ps1 -Profile PublicWindows -List/-BuildOnly/-NoBuild/-TestTempRoot`；首次仅接受 PublicWindows。保留无参数的 Windows 含义，但按上表明确剥离内部工具检查。不在 0.7.0 默默加入 Generic/Linux 承诺。
5. **断言**：把按文件/环境有无执行的可选私有检查改为分类驱动的显式选择。保留原断言实现和内部必需门；公共部分必须报告实际 runner/focus 和失败。SDK 测试写到 `artifacts/pn041/...` 的诊断位置改为当前 `DtmApiTestSession` 的本轮输出，异常日志不是恢复输入。
6. **文档与 CI**：一次整理根映射、必要 SDK/源码 README 和 API matrix 的公开入口/链接；更新 `.github/workflows/public-source.yml`，保留一个 Windows job 复用入口，删除已不需要的 Python/Node 设置。Issue/PR 模板只要求问题、复现、来源和实际测试，不要求内部记录。

必要调整仅在开发/交付工具、测试选择和说明，不在游戏 tick、Hook、API 或加载路径增加逻辑。目录导出没有玩家运行开支；SDK 标准工程关系仍由 MSBuild 拥有。

### 6.4 对外固定命令与闭包证明

实施完成后公开 CONTRIBUTING 只需要这组源码命令：

```powershell
./tools/scripts/test-public-source.ps1 -Profile PublicWindows -List
./tools/scripts/test-public-source.ps1 -Profile PublicWindows -BuildOnly
./tools/scripts/test-public-source.ps1 -Profile PublicWindows
```

前提：Windows x64、Git、可用 PowerShell（fixture 子进程要求 Windows PowerShell 5.1）、global.json 指定的 .NET 8 工具链及 .NET Framework fixture runtime；首次允许从已声明的公开 .NET/NuGet 来源恢复。没有先验 cache、内部 `.tools` 或仓库外 SDK 产物要求。`-NoBuild` 是已有输出的局部入口，缺失/不匹配要失败；首次候选验证不用它绕过 build。

闭包分两层证明，不能只靠字符串搜 `.csproj`：

- **MSBuild 层**：使用选中工程的实际求值/restore/build 信息（含 Import、Compile、Content、EmbeddedResource、AdditionalFiles、Analyzer、generator、ProjectReference、目标生成资产和选中配置）。输入必须是 N/M 中的文件，或同轮由公开 recipe/受控 restore 生成的文件。比如 AuthorSdk 的 Build/Analyzer DLL 标成生成资产，须由其 ProjectReference 实际构造，不能从旧 bin 复制。记录来自求值结果，不提交第二份图。
- **执行层**：静态查路径只是定位帮助；在完全没有内部 docs/references/settings/artifacts/.tools 的干净公开 checkout 中，从空 fixture/cache 开始跑选中 tests。测试期间允许创建输出，结束确认 tracked 输入/版本未被改写；不能从父目录、本机游戏、内部 Git 对象或旧测试遗留满足依赖。任何缺文件/旧 target/错误主机/恢复失败都是真失败。新增 P 贡献后的最终 M 要重复这一边界检查。

准备阶段的公开下载断网或摘要不符必须清晰失败；不降级使用未知旧 cache。网络恢复成功不等于承诺首次离线构建；冻结 target 自有源码和 pinned package 的使用范围按现有 SDK 合同核验。源 tree 边界检查只针对拟提交的 tree，测试生成的 bin/obj/.tools 不因存在就被加进公开 tree。

## 7. 实施分解、验收与维护

### 7.1 实施顺序

| 步骤 | 具体改动 / 完成条件 |
| --- | --- |
| SRC-01：准确来源 | 归集 S0 并对照 0012 输入；记录后来入口/Catalog 变化。公共新工程、删除与 schema4 齐备；不混入无关脏改动 |
| SRC-02：一次性许可与入口材料 | 完成 ZIP 全成员/来源/许可/对应源码、有限声明 stub 审查；保留未分发 PNG/WAV 的明确边界。添加根映射文档、修正 NOTICE、SDK 与 public-link 输入；不重包 D7 |
| SRC-03：公共测试入口 | 修改寻根、统一 suites v2 分类及 SDK 必需/可选选择、PublicWindows 准备/构建/执行、CI；提交完整迁移归属对照。保留内部断言和窗口范围 |
| SRC-04：对象导出 | 实现 policy parser、Plan/Export、字节/mode/路径/许可检查、独立公开对象库、闭包/链接报告；共享低层函数时仍用现有脚本，不复制两份实现 |
| SRC-05：PR 与回流 | 实现显式 E/P/N PreparePr、bootstrap 审查、冲突诊断、映射回流和幂等 finalize；ref CAS、main 变化与可恢复父链必须实际验证 |
| SRC-06：首次候选 | 在已提交 S 上生成 N；从准确 P 做 bootstrap/三方候选；干净 Windows 验证最终 M，交回普通 PR 的完整可审查材料。实际远端发布另按届时授权执行 |

SRC-02 与 SRC-03 可在内容明确后分别准备，但最终 S 必须包含依赖闭包。实施只需一项实际工作自己的 Update；本设计 Update 不扩展成每版同步账本。事项 3 的 Linux 从同一分类/标准工程上另行开始，不阻塞 0.7.0 所声明的 Windows 源码门。

### 7.2 行为验证用例

实现测试放在现有工具测试机制，使用小型独立 Git fixture，不接触游戏或真实远端。以下为需要证明的行为，不是本次已执行结果。

| ID | 输入/动作 | 必须观察到的结果 |
| --- | --- | --- |
| X01 | 指定 S，工作树另改一个公开 .cs、一个内部文档并添加未跟踪文件 | 输出仍是 S 的原 blob；报告指出未纳入；不能混入工作树后标 S |
| X02 | 相同 S/R 重复 Export，系统换行配置不同 | 输出 tree/mode 一致，根映射外路径相同；冻结 recipe 字节不变 |
| X03 | draft、未知规则字段、重复 JSON key、同名目标/循环映射 | 拒绝；active 不接受 null digest 或 hold |
| X04 | 新根、新 src component、已知工程新 C# 子目录、新未知 binary | 前两者要求分类；普通 C# 继承；binary 未审则失败，不按 ZIP 一刀切 |
| X05 | 在被选树放私有 DLL、改名为 .dat 的 PE、LFS 指针、symlink/gitlink、路径穿越、ADS、大小写碰撞 | 静态边界拒绝；例外无权绕过 |
| X06 | 目标是仓库/公开 clone/游戏/MODS 的父子目录、junction、已有非空目录，或创建时被占用 | 任何输入/旧交付不被覆盖或删除；返回准确目标错误 |
| X07 | BepInEx ZIP 缺失、hash/member/许可/对应源码不符 | Export 失败；不删 Harmony suite，不把官方网页链接当完整补件 |
| X08 | 删除 frozen 0.5.5 source/props 或当前 targets 中一项 | 准备/闭包失败；旧版本命名不能成为忽略理由 |
| X09 | 漏 API matrix、Catalog、Shared、产品 QA Link 源、Author.Build/Analyzers、模板或 schema | MSBuild/执行闭包失败；不能从内部树或旧 bin 补文件 |
| X10 | P-only 新文件、N-only 新文件、两侧同内容 / 非重叠修改 | 正常保留/合并，最终 M 受检 |
| X11 | 同行冲突、binary 双改、add/add、modify/delete、rename/rename、目录/文件冲突 | 记录双方及 stage；E/P 不变，不生成可合入标记 |
| X12 | 无争议删除、rename/edit、删除后仍有资源/Import 引用 | Git 正常处理前两者；残余依赖令最终闭包失败 |
| X13 | 首次无 E，P0 有旧目录和外部独有改动 | Bootstrap 列全取舍；未决项阻断；U 保留 P0 作者历史，成功后 E=N |
| X14 | P 有未回流贡献，完成两次导出 | 每次 E 只推进到纯 N，第二轮仍保留该贡献 |
| X15 | M 添加允许目录中的未授权 fixture、修改规则自我放行、或保留新规则禁止的文件 | 最终 M 复查失败；不因 N 通过而放行，不静默删文件 |
| X16 | main 在验证后变化 / 两个候选共用 E | 前者使旧候选失效；后者只有准确 CAS 能推进，其余重算 |
| X17 | PR 未合入、实际合入树不同、错误合入方式、CAS 失败、中断后重试 finalize | 未满足条件不推进；准确 H/N 的重试幂等；不写同步数据库 |
| X18 | 同路径/根映射/混合 PR，含 rename/delete 和两个作者 | 回流路径正确，原作者/日期/PR 来源可追溯；内部 README 不被覆盖 |
| X19 | 从空公开 checkout 运行 PublicWindows，有不同 cwd、空格/中文路径，无 PROJECT/.git 历史/private feed/.tools | 所选构建/测试真实通过；root/helper 不越界寻找内部输入 |
| X20 | .NET 主机不兼容、依赖缺失、错误 focus、选中私有项却无 fixture、测试故意失败 | 明确非零退出；不换成 skip/假 PASS，不临时重分类 |
| X21 | 保留无许可 WAV 排除，但让任一公开根引用 Manbo 或复制 WAV | 闭包失败，准确列出未支持的能力；不能声称完整 solution 已通过 |
| X22 | 纯 README 文风/链接变化且不改有效构建/测试输入 | 只跑文档/链接/边界与所需投影检查；不跑游戏/完整 Release |
| X23 | 新公共依赖、test 分类或 ExportPolicy 变化 | 跑相关导出/闭包/选择用例和受影响的 PublicWindows 门；不机械扩大到所有历史矩阵 |

### 7.3 最低接受条件

- **设计交付**：路径/规则/命令/失败状态/回流与基线生命周期齐备；规则草案可解析且与实际路径一致；本次链接和文档治理通过。它不等于 exporter 已完成。
- **工具实施**：上表用例有实际结果，失败不会写坏输入或推进 E；公开对象库没有新增内部祖先；准确 S/R 到 N 的 blob/mode 等价成立。
- **首次 Windows 源码交付**：从完整已提交 S 生成，所有必需许可 gate 关闭；干净公开目录的声明构建与全部选中测试通过；最终 M 的边界/许可/依赖/链接/来源检查通过；bootstrap 中无未处置旧公开内容。无法证明的可选能力在 README 和 List 中明确列为未支持，不能把它作为本轮 PASS。
- **实际公开更新**：通过普通 PR，合入准确受检候选，才幂等推进 E；源码交付结果与 Steam、SDK ZIP、玩家验收身份分别记录。

本次只完成第一项。D7/r6/r2 已有的实际产品验收仍有效；没有执行本表的生产导出器或真实公开克隆构建，不能登记第二、三、四项 PASS。

### 7.4 本次有界验证

在独立系统临时目录，以 Git `2.54.0.windows.1` 创建纯 synthetic 对象，已实际验证 8 个例子：显式 E 合并无共同祖先的 P、同行冲突、无争议删除、modify/delete 冲突、rename/edit、连续两轮 E=N 保留公开独有文件、误用 M 为 E 会丢失公开文件的负控制、失败 CAS 保持 ref 原值。

这只证明 Git 原语与基线选择的关键语义，不证明生产 exporter、路径/许可检查器、远端分支保护或源码构建已实现。没有在共享目录切分支、创建 worktree、修改源码/脚本、安装游戏或重包。临时例子的可读结果记录在本任务 Update；无需把一套新测试框架加入仓库。

### 7.5 回退与日常更新

设计/工具尚未公开时，按实际 diff 撤回新增项和本次导航行，保留共享目录其他任务变更。Export/PreparePr 失败只保留任务 staging；不要重置调用方目录或 public main。

已经合入的公开变更需要回退时使用普通 revert PR，不移动既有标签或强推。**不把 E 倒退**：E 仍记录最后一次成功纯导出，revert 是 P 的正常变化；随后把需要保留的回退回流内部再生成 N。否则下一轮同步可能重新引入已明确撤回的内容。若是来源记账错误，依据实际 H/N 修复 ref 并留下正常 Git 说明，不能按版本字符串猜目标。

日常步骤固定为：取得准确 P/E → 正常回流公开 PR → 提交相关内部变更得到 S → Plan/Export 得 N → E/P/N 合并及最终树检查 → 运行变化要求的验证 → 普通 PR → 核验实际合入 → E 推进到 N。没有功能变化的公开文档 PR 按文档门结束；新源码/依赖/规则变化只增加受影响的验证。

一次性成本集中在来源归集、有限许可/材料补件、寻根与分类、根入口和 exporter/同步实现。持续成本是普通 PR 中的新 component/外部资产分类、依赖许可变更、冲突和真实缺陷；已有 C# 文件新增、普通版本更新不要求筛全仓文件或复制两套手册。公开路径不另改名，内部与公开使用相同 MSBuild 和 PowerShell 实现，不新增 Bash 系统、同步服务或玩家运行逻辑。
