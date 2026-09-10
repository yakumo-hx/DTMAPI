# 20260908-0005: 独立测试项目、默认覆盖与公开源码 CI

## Metadata

- Update ID: `20260908-0005`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求建设整个工作空间并减少开发流程冗余；本记录落实测试工程拆分，文档归档不计为平台工程完成。

## Summary

旧 Unit 项目通过环境变量选择执行，却仍编译 17 个直接项目引用、88 份 C# 输入及 7 个 net48 专项夹具。现按 Core、Runtime 集成、兼容层、产品和源码契约分成独立编译图；日常产品修改可以只构建其相关图。MoreSaves 首拆后只剩 1 个直接引用和 7 份编译输入；Core 为 4 个引用和 25 份输入。Core 初拆时的 26 份输入在后续集成纠正宿主 UI 测试归属后减为 25 份。

`suites.json` 统一拥有项目、默认调用和焦点路由，PowerShell 与测试程序读取同一份源文件。32 个旧 focus 的调用集合完整保留；原默认 261 个入口全部保留，再补 MoreEquipment 产品套件、6 个 DebugConsole 普通边界及 3 个产品验收路由，共 271 个默认入口。专用 actor 和一次性 source-arbitration 不进入默认执行；后者也不进入默认 Solution 构建。

旧 Unit 变成无项目引用的薄入口，转发 `test-unit.ps1 -NoBuild`。独立项目保留原测试程序集身份以复用现有 friend 边界，各自输出并在独立进程执行；产品 actor 返回自己的可执行入口。`-NoBuild` 不准备依赖或调用 MSBuild，缺输出先报出标准构建命令；干净树使用新入口。

## Changed Files

- [测试路由与维护边界](../../../tests/DTMAPI.UnitTests/README.md)：单一 map、共享 harness、最小共享夹具；原独立测试类移到各自项目，原 Program 按职责拆分。
- [test-unit.ps1](../../../tools/scripts/test-unit.ps1)、[test-common.ps1](../../../tools/scripts/test-common.ps1)、`DTMAPI.sln`、现有 build/test 入口：焦点选择实际编译图，多图用临时 Solution filter 一次构建；默认完整覆盖。
- [test-public-source.ps1](../../../tools/scripts/test-public-source.ps1)、[CI workflow](../../../.github/workflows/public-source.yml)：默认 Unit 产品套件、QA/Doctor/installer 源码套件、SDK 三个公开 focus，加上玩家修复/槽位采集、runner 模块/子进程、解包阶段/比较和历史工具的模拟测试。Python 3.12、Node.js 22 由 CI 显式准备；native-map 使用仓库已有公开符号图夹具。不把私有精确输入或游戏验证的缺席记为 PASS。
- [脚本入口说明](../../../tools/scripts/README.md)：补充预检、选定依赖准备、SDK 显式复用和 Advanced 单次编译入口。
- [路由行为测试](../../../tools/scripts/test-unit-routing.ps1)：未知/空白焦点在 SDK 前拒绝、显式焦点优先、多图默认执行、缺输出失败及 NoBuild 边界。

## Validation

- `source PASS`：16 个独立图和旧 launcher 构建通过；从实际编译 DLL 的元数据核对所有默认/focus 方法存在、无参、static、void，32 个旧焦点调用集合与拆分基线一致；没有丢失原默认入口。
- `unit PASS`：MoreSaves 20 项；MoreEquipment 产品与 Host 普通套件（包含所属新进程的 actor）；Core 清理 27 入口、兼容需求套件、SDK target 和版本投影原焦点；旧 launcher 正向 NoBuild 路由。
- `unit PASS`：Windows PowerShell 5.1 路由 11 项；原 full-entry focus guards；所改 PowerShell 解析无错误。
- 公开构建图闭包核对覆盖 67 个源码项目。MultiPlatformInstaller 测试改用标准项目引用与本次构建输出，不再靠旧 RID 目录中的 DLL；SDK 的冻结兼容输入可由随仓库保存的自有源码重建。PowerShell 工作流检查各用独立 WinPS 进程，已证明预期 native 失败后正常 PASS 返回 0、真实 throw 返回 1。
- 第一轮完整 Release 在 21.533 秒的构建阶段报 NETSDK1151：反射测试引用自包含安装器，但未声明 SDK 的测试项目身份。补 `IsTestProject=true` 后，SDK 按其现有测试项目规则处理引用，仍构建并复制本次安装器输出；局部构建及安装器测试通过（0 警告/错误）。完整验收从头重跑，不拼接局部结果为整套通过。
- 第二轮完整 Release 在 41.9 秒时暴露 Core 中的标题 UI 测试依赖 Bootstrap：`Assembly.Load("DTMAPI.BepInExBootstrap")` 隐藏了宿主边界。现把该测试移到已有 Bootstrap 引用的 Runtime 集成图，用 `typeof(ReflectedTitleMenuSettingsUi)` 表达依赖；Core 没有增加项目引用。默认 Core 129、Runtime 集成 54，合计仍为 271 入口；32 个旧 focus 调用集合与原默认覆盖保持完整。
- 第三轮完整 Release（候选 `c7dcb17e`）用时 104.147 秒：15 个 Unit 图、271 个默认入口全部通过，随后在 QA 的退役 Manager 旧单体布局断言失败。保留 `final-release-3.log/json`；同轮排查限定到 QA 六处读取和五个专项脚本的 runner 来源/排序，不逐次用完整 Release 发现同类错误。Doctor 与发布产物投影的独立诊断通过。
- QA 适配后的独立构建通过（6.321 秒、0 警告/错误），48 个顶层用例全部通过（9.266 秒）。退役入口、G3 缺 staging/槽位和 StrongPlantingGun 缺重入改为实际只读路由的正负配对；跨阶段顺序检查入口，保留 ABI 和保存恢复约束。三个旧路由进程助手与新探针复用同一入口，并行读取 stdout/stderr、30 秒截止，超时只清理本测试创建的进程树。日志为 `unit-tests/qa-runner-repair-build.log` 与 `qa-runner-repair.log`；五个 PowerShell 专项结果由 0007 记录。
- 第四轮完整 Release（候选 `d2fd6807`）用时 179.287 秒：15 个 Unit 图、271 个入口、QA 48 项、Doctor 和 Runtime 源码/已发布信息矩阵通过；在实际玩家包的可选命令约束失败，`final-release-4.log/json` 保留。生产投影修复由 0004 记录。随后精确候选矩阵暴露测试继承维护者 `DTMAPI_GAME_DIR`，覆盖了其预期的 Workshop 自动定位；现只在已有环境快照/恢复范围内清空该变量，测试继续使用自己创建的假游戏。
- 本轮限定核对全部 15 个默认图的动态程序集加载与字符串类型查找。其余需要加载的兼容宿主、合成 Harmony/游戏引用和 net48 actor 均有所属项目的明确依赖与暂存路径，未发现同类遗漏。Core/Runtime 两图构建通过（5.21 秒、0 警告/错误）；默认局部执行前 10 组通过后停在 Zoom 的旧发布字段断言。
- Zoom 改为读取 Catalog 中唯一的 `zoom` 记录，继续检查身份、Advanced 来源、策略和 Harmony owner；人工发布文案只通过 `catalogId` 关联。DebugConsole 与 Oil 的源码契约改读实际判定/部署/恢复模块，保留原约束。Zoom 2、Mine 1、DebugConsole 22、Oil 1 入口已通过；本轮曾误删 DebugConsole 仍引用的共享空夹具，重建报 CS2001 后已原样恢复，失败证据保留。
- SourceContracts 改读明确的模块与 phase，跨阶段顺序检查 facade 的实际调用顺序；已有行为自检覆盖的重复变量/提示断言不再保留。首次执行在 MoreSaves 负向路由停住（22.149 秒）：实际命中了同一官方来源选择拒绝，旧测试依赖完整报错句子。现与前面的成功路由配对，只改变额外启用产品，再核对拒绝模块和官方来源语义，排除保存前提等其他失败。重建通过（4.780 秒、0 警告/错误），11 入口通过（23.288 秒）。
- `unit PASS（局部接续）`：本轮所有 15 个默认图、271 个入口都有通过结果。前 10 组通过后按未变输入复用证据，只重跑修复或尚未执行的后 5 组；这不是一次完整 Release/public-source 通过记录。源码与 map 已冻结，完整验收由主智能体从候选提交重新执行。
- 第一次完整 public-source 入口执行 155.925 秒：源码构建、15 个默认 Unit 图、QA/Doctor/安装器、SDK 三个公开 focus 和前六个工作流检查通过，停于 Issue 检查的 WinPS 中文夹具编码；原始失败保留在 `final-public-source-1.log/json`，修复由 0004 记录。这轮不记为完整 profile 通过。
- G2 的冻结失败码表保留历史源码路径和原字节，检查器只为两组已知搬迁解析当前 Core/Runtime 测试正文，再提取原方法并核对精确失败码。12 项搬迁定位通过；提交前的单项执行只剩既有 clean-and-committed 门槛，未跳过该约束，`g2-negative-test-owner-adaptation.log/json` 保留此非零结果。完整候选提交后再验证。
- 第五轮完整 Release（`a7f5351b`，211.305 秒）通过前段全部套件和 Runtime 安装器矩阵，停于证据保留投影过期。按标准生成器补入最终邮件会话的两张观察图与 result.json，旧保留项没有减少；原来 1014 个 smoke、62 个 Runtime 身份和 35 个长期证据根不变。修复后的只读一致性检查通过，`final-release-5.log/json` 与 `final-retention-refresh.json` 保留实际结果。
- 后段诊断（`784520d6`，691.183 秒）通过证据保留、反编译路径、完整 SDK/精确参考/便携矩阵、Runtime 所有权、无效安装目标、双宿主升级事务及开发安装事务，在 Phase 0 的归档规划正文定位失败。该诊断明确不算完整 Release PASS；`final-release-tail-1.log/json` 保留实际退出码。检查器改用已有文档路径解析器读取归档正文，冻结身份不变；单项复验通过（22.479 秒）。
- 后续同类检查先独立诊断：OneActionComplete、FishBreedingAssistant、AnimalHusbandryProgress 的 4 处源码路径改指实际兼容/产品测试；ActionSpeed 原项通过并复用。修复后三项通过，未改变 Hook、owner、原生基线或生命周期约束；首轮失败和复验见 `release-static-tail/product-static-validation.json`。
- 后段 6 项独立检查中 5 项先通过，QA 语义清单的真实边界失败：消费者仍漏列 3 份拆分后的测试文件，3 条 runner 契约和一条禁止直接 PostMessage 的作用域仍指向旧单体。现精确列出消费者，按实际模块拆分契约，跨阶段只核对入口顺序，并绑定当前 deadline 与回执。真实边界复验通过（24.198 秒）；25 条来源/符号/消费者契约、149 条禁止模式、49 个负例及 4 个 IL 工件保持，按模块计的生命周期条目为 24，Catalog 同步。12 项元负例、1 项 Catalog 负例、6 项收据集合负例通过（92.333 秒），实际 Catalog 通过（5.558 秒）；见 `release-static-tail/repair-results.json`。其他 ABI 检查复用，不代表新增 Unity/Mono 实测。
- 已提交的 G2 authority 专项原样通过（95.682 秒）：合同收据正例及 ownership/runtime/paired 外部覆盖拒绝均执行，未绕过 clean-and-committed；精确参考夹具与 10 产品发布集合检查也通过。后段诊断均保持自己的范围，完整 Release 与公开 profile 仍以各自从头运行结果为准。
- 最终完整验收通过：候选 `e1321953f44894f01cf0d8c67148b1857db73478` 从头执行 Release，退出码 0、1243.066 秒；随后正常公开源码入口从头执行，退出码 0、188.744 秒。两者开始/结束提交相同，期间本任务源码无未提交改动。Release 包含双宿主安装/升级、完整 SDK/精确参考、10 产品双打包、语义正负例及保留 ABI；公开 profile 包含 15 个默认图、QA/Doctor/安装器、3 个 SDK 公开 focus、18 个 PowerShell 工作流、35 个 Python 用例及 Node 函数地图检查。
- `not-run`：远端 GitHub Actions 未触发，首次远端执行结果仍需单独记录；本地公开 profile 已通过。游戏、私有精确 ABI 和完整 Release 包矩阵继续分别拥有验证范围，不借公开 profile 冒称执行。

## Evidence

- 完整原始结果：[Release](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-release-6.json)、[公开源码](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-public-source-2.json)，同目录 `.log` 保留全过程；前五次 Release 失败、第一次公开入口失败和诊断后段不改写为 PASS。
- 最终 [保留 Release ABI](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-retained-release-abi.json) 和 [合成 ABI](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-synthetic-retained-abi.json) 从实际 `latest` 报告按字节另存，避免下一次运行覆盖本次结果。
- [初次拆分的构建范围、方法核验与执行结果](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/unit-tests/validation.json)保持当时内容；[集成发现后的归属与断言修正](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/unit-tests/integration-repair-validation.json)另记本轮结果，同目录保留失败及后续必要输出。
- 源文件搬迁表为工作空间建设中的 `tmp/workspace-construction/unit-source-moves.json`，由总整合机械修复历史入链；不为源文件创建空跳转页。

## Rollback Notes

按此变更整体恢复原测试项目、源文件布局和入口路由即可；不涉及游戏、存档或生产程序集 friend 授权变更。不要只恢复旧 map 而保留新源码布局。

## Follow-Up

本地集成验收完成；首次远端 Actions 运行结果作为 CI 环境证据单独确认。日常产品修复仍依据变更行为选择最小焦点，不自动运行整个 profile。
