# 20260910-0012: SDK 首次公开交付的标准 MSBuild 与现有 Mod 兼容

## Metadata

- Update ID: `20260910-0012`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 用户指定在当前工作区连续执行 PN-041.a–f / PN-042.a–c；决定归 [0011](20260910-0011-sdk-first-release-plan-correction.md)，详细规格归 [SDK](../../planning/platform-next/execution-sdk-msbuild.md)及[兼容](../../planning/platform-next/execution-compatibility.md)。

## Summary

0.7.0 首发 SDK 已切换为标准 MSBuild 单后端，贯通普通工程、库、生成器、资源、标准恢复、实际 IDE/CI、封包/Doctor 和 Windows shipping Mono 作者回路。12 个现用产品及 Advanced fixture 已转换。旧 SDK 从未公开，不增加通用迁移器或任意历史客户端支持。

现有包验收发现真实退化：新 SDK binding reader 误认历史同名元数据，导致原样旧 Mod 被拒。已按[根因与有界修正](../../reviews/code/2026/20260910-0006-existing-package-marker-collision.md)恢复读取，保留损坏新格式拒绝和来源/Strict/Native 边界。独立验收0007的字段、工程根与材料缺口已于2026-09-11完成同批返修及补验，当前交回 Runtime r6、多平台 r2 和 SDK D7；D6及旧候选保留。精确第一方 CSV 删除集合未扩大、未恢复。

## Changed Files

- 独立验收0007返修继续同批：`ManagedRuntimeSurface` 扫描实际方法及嵌套类型的六种字段指令，按最终字段定义核对 static/instance；pack失败保留真实API target。`DTMAPI.Author.props/targets` 与 `StandardBuildIntegration` 区分作者元数据根和所选工程目录，内部文件参数/facts使用绝对路径并核对projectFile。新增字段及布局回归；README/工程指南/许可说明修正首次交付、标准子进程、路径和Doctor边界。

- `author-sdk/build`、`src/DTMAPI.Author.Build`、`src/DTMAPI.Author.Analyzers` 与 AuthorSdk：薄标准工程接入、实际求值/编译事实、SDK203 analyzer、标准 NuGet 与准确运行资产/最终输出采集；build/pack 同一后端，取消过程停止 owned 子进程，发布失败保留旧 ZIP。
- 移除无消费者的手工编译图、私有恢复执行器、旧 author schema1–3 与 migrate-build；当前工程和 fixture 使用 author4，程序集版本由标准工程拥有。原玩家包 reader、旧 receipt/V1/最低 Runtime 和恢复能力保留。
- 版本投影测试转 schema4；历史 G0/G2 源码断言回到其原验收提交，当前标准入口另验，原 Runtime/Doctor 负例与 receipt 不改。测试提交 `cc7044a7` 满足历史脚本干净提交门。
- 完整测试的 AuthorSdk 阶段显式传入 Get-DotNetExe 的 .NET 8 主机，并在 finally 恢复环境，避免测试子进程误选系统 .NET 9；便携测试仍清除该覆盖，独立验证随包工具链。
- 首发完整 SDK builder/preparation/inventory、指南/许可/模板/CI；实际产品 wrapper summary3 分开记录标准 BuildFacts 和产品源码快照，历史 summary1/2 reader 保留。
- `src/Shared/AuthorPackageMarker.cs` 与跨 Core/GameBridge/Doctor 目标矩阵：识别已观察旧无 schema 元数据，检查 ID/版本/类型/有界字段；不授予 SDK binding、来源或 Native 权限。此 Runtime 输入及测试单独提交 `1002ae05` 以满足真实来源构建门，其他无关工作树未纳入该提交。

## Validation

- 2026-09-11协调方[D7独立复核](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/independent-acceptance.md)通过：重新检查准确SDK ZIP与逐文件身份，在新外部目录复跑字段正反例、原ZIP保全、src工程/离线/Debug/Release/XML/PDB/内容和错显式根；源码及实际IDE/CI/完整Release/Mono证据核对支持关闭R01–R03。本Update转verified，0.7.0按Windows Developer Preview技术接受；不授权上传。
- [F1–F4 D7补验](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/README.md)全部完成：字段六指令/泛型继承/ldtoken和正确DLL/PDB、原错误包拒绝及真实target；根/src/中文空格/外部cwd、两配置/Clean、XML/PDB/混合内容与无父obj；准确D7的CLI、公开CI和实际IDE Release Rebuild三产物相同。实际随包指南/链接/inventory/许可通过，旧D6原始证据保留。

- 新完整 `full-release-repair-r2` FromStart/All含build，exit0，2552.98秒，前后输入摘要一致，重产SDK整个ZIP与D7相同。前一完整运行及仓库tmp focused因临时目录Access denied失败，系统Temp focused及使用现有TestTempRoot参数的新完整运行通过；保留失败，不改权限或声称确定系统占用根因。探针自身的字符串/Doctor解析/输出路径错误也保留，未放宽产品保护。
- 新字节 Mono `GAME-SMOKE/20260911-001047` Passed：准确r6＋Spruce.LayoutRepair和原Hazel控制包，Entry/SaveLoaded/认证proof的50/42/8/资源/字段42成立，实际事件源码第13行，驻留MVID与磁盘一致。正常关闭、owner remaining/failures=0、QA关闭；恢复原16个Runtime文件/插件树/配置/启用状态，30个live archive及侧车不变，锁释放。原未变PN-042和r6/r2矩阵沿具名范围复用。
- [独立验收0007](../../reviews/code/2026/20260910-0007-sdk-d6-independent-acceptance.md)的原D6反例与下列实施阶段历史PASS均保留；实施方交回时保留implemented，后由上列协调方独立复核关闭，不把实施自验或技术接受冒充公开发布。
- B01–B12 / C01–C10 的实际输入、命令、逐项观察、复用理由及未测范围归[最终证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/README.md)。最终完整 Release 从头执行并包含 build，`full-release-r5` exit0，2468.84秒，前后 SDK 输入摘要一致；R-AuthorBuild.release / R-Compatibility.release 在 Windows Developer Preview 范围接受，未公开发布。
- 标准工程/资源/生成器/analyzer、不同 ref/lib、标准锁/离线/签名包/提取资产篡改、最终 PE/Native、依赖闭包、并发/取消/原子发布 focused 及完整 SDK suite 通过。真实外部 CLI、VS Code/C# Dev Kit、公开 CI 输出对齐；实际 IDE 打开并构建转换后的 AutoFishing 成功。Composition XML 未有效启用，未冒称该工程 XML 已比对。
- SDK D6 独立完整分发/inventory、实际便携离线门与仓库外 CLI/公开 CI/实际 IDE Release Rebuild 通过；三入口 DLL/PDB 一致，最终混合包整个 ZIP 与 r6 Steam 输入逐字节相同，游戏观察按此准确输入复用。完整 Release 重建 ZIP 与 D6 相同；Runtime 重建的调试身份差异及整个 PE 的有界比较另见证据，不冒称 Runtime 完整 hash 相同。准确公开 0.6.1 surface：仅20项授权 CSV 删除，额外删除0；原 DLL/helper 控制组未重编。
- Runtime r6 own Windows 安装器、多平台 r2 Windows/WSL Linux 的0.6.1升级、撤回/恢复和来源/损坏输入矩阵通过；候选 ZIP 逐项文件 hash/长度验证通过。WSL/fake game 不代表非 Windows 实机注入。
- 最终 Steam r6 `GAME-SMOKE/20260910-200318` Passed：真实订阅/官方启停、原曼波 legacy、旧0.5.5/V1/receipt、新标准工程/V2/shared/资源/不同 ref-lib、外插件自主加载与 Control 共存；authenticated session、改代码后重启与驻留指纹、源行和正常退出/owner清理通过。
- 初次标准编译 fixture/分发/PathMap、过期/错误会话、r5 marker 及日志捕获中断等失败保留。DirectExe 的两次刻意异常组合总体 Failed，不改称总体 PASS；实际 Entry/事件源行和隔离观察可按准确字节复用。受测 IDE attach 未获得 shipping Mono 受管栈/locals/暂停，不承诺断点。

## Evidence

- [统一 B/C 与游戏证据索引](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/README.md)拥有运行、hash和边界；局部 SDK 日志在 `artifacts/pn041/sdk-msbuild-evidence/`，准确公开0.6.1差分及安装器日志在 `artifacts/pn041/compatibility-evidence/`。
- [当前候选选择](../../planning/platform-next/release-candidate.md)路由 Runtime r6、多平台 r2、SDK D7；原 PN-031.multi r1 由 [0008](20260910-0008-multiplatform-070-candidate.md)拥有，不覆盖、不重标。
- 最新完整 Release 归 `artifacts/pn041/full-release-repair-r2-result.json`；原D6 `full-release-r5-result.json`、早期捕获中断和本轮repair-r1访问失败均保留其准确结果。

## Rollback Notes

所有官方 Local 测试包已用 SDK 撤回；临时外插件、配置/启用状态及原16个 Runtime owned 文件/插件树恢复。此前31个archive/fixture保护项原样保留；D7补验的新基线覆盖30个live archive及侧车，均比对不变。未执行原生保存、未建立或恢复存档备份。共享锁已释放。

保留无关工作树、历史 SDK ZIP、冻结引用及原包。未上传，未修改 Catalog/订阅清单中的公开0.6.1事实；SDK 和玩家 Runtime 分开交付。

## Follow-Up

PN-041原任务F1–F4实施、补验及协调方独立复核已完成，D7/r6/r2在既定Windows Developer Preview范围技术接受，本批无剩余详细实现项。PN-042已验范围保留；实际公开仍0.6.1，后续发布操作另行授权。本批不启动M4、不上传；实体控制器、非Windows游戏注入、低TFM/facade和独立debugger环境按原路线处理。
