# PN-003: SDK API target catalog and frozen payload selection

## Metadata

- Update ID: `20260907-0004`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户要求已完成 PN-001/002 后继续，并确认已发布 Runtime 为 0.6.1，SDK 在 0.5.5 后未更新。
- Review: [平台基线](../../reviews/code/2026/20260907-0001-platform-next-baseline.md)。
- Task: [PN-003](../../planning/platform-next/tasks.md)，沿 [A03](../../architecture/platform-next.md)实施。

## Scope and decisions

保留当前 Runtime 发布 0.6.1、SDK 工具版本 0.1.0 和冻结 API target 0.5.5。0.7.0 / SDK 0.2.0 仅为 planned 配置，不生成不存在的载荷、切换默认目标或修改发布 Catalog。新 catalog 为 SDK、Runtime、Doctor 与发布脚本提供同一目标、协议外的执行要求和契约路径；冻结 contract 与 payload 原字节保留。

已核对 PN-001/002 的验收记录和源码接缝；不重做它们。现存单 target 常量分布在校验、构建、打包和多个 marker reader。另发现当前 Packager 写 schema 2，但内容重载只接受 schema 1，本项同时统一目标校验的读取行为。旧 Advanced receipt lane 仍固定旧目标，不开放任意原生作者通道。

## Changed Files

- `author-sdk/target-catalog.json`、`src/Shared/AuthorApiTargetCatalog.cs`：唯一 target 元数据与共享 BCL reader，区分 available/planned、writer 严格下限和历史包 reader 兼容；分别链接进现有 SDK/Core/Doctor 程序集，不新增 Runtime DLL。
- `src/DTMAPI.AuthorSdk/SdkApiTargets.cs`、`CompatibilityAssets.cs`、`ProjectValidator.cs`、`TemplateCreator.cs`、`CodeModBuilder.cs`、`DeterministicPackager.cs`、`DeploymentPackage.cs`、`AuthorSessionService.cs`、`AuthorApplication.cs`、SDK `.csproj`：选择目标、独立载荷契约验证、报告/marker 投影、正常输入诊断、显式来源无回退与历史恢复兼容；session target 与协议要求保持独立。
- `src/DTMAPI.Authoring.Contracts/AuthorContracts.cs`：澄清保留常量的 legacy 含义；未改工具/运行版本，也未增加 Abstractions 公共接口。
- `src/Shared/AuthorPackageMarker.cs`、`src/DTMAPI.Core/Manifesting/ManagedModClassification.cs`、`src/DTMAPI.InstallDoctor/DoctorEngine.cs`、Core/Doctor `.csproj`、`src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs`：有保留 marker 时统一检查，schema 1/2 一致读取；不能删 CodeModKind 绕过目标/绑定验证，无 marker 的 legacy 行为保留。
- `tests/Shared/PlatformPackageTargetMatrix.cs`、Unit/Doctor 测试入口及项目文件、`tests/DTMAPI.UnitTests/PlatformSdkTargetRuntimeTests.cs`、`Batch6AdvancedRuntimeTests.cs`、`PlatformVersionProjectionTests.cs`：实际分类器、Doctor 与桥接读取矩阵，旧 Advanced 加载回归，版本投影改用 catalog 与独立破坏 fixture。
- `tests/DTMAPI.AuthorSdk.Tests/PlatformSdkTargetTests.cs`、`Program.cs`：真实 CLI/build/pack、冻结载荷篡改、历史低 floor 包 status/recover/withdraw、会话输入与协议要求回归；新增 focus 与默认 suite 复用。
- `tools/scripts/author-sdk-release-common.ps1`、`build-author-sdk.ps1`、`check-author-sdk-release.ps1`：遍历当前 SDK 的 available 目标，包中记录 catalog/default/available targets，继续验证冻结 contract、完整文件树和确定性 ZIP。
- `author-sdk/README.md`、`schemas/dtmapi-author.schema.json`、两种模板的 author/manifest 和 CodeMod csproj 模板：目标使用说明、schema 结构及按选定目标生成项目。旧 frozen props 文件未改。
- 本 Update、九月台账、平台架构/tasks/status：当前源码行为与执行交接。Wiki、玩家支持记录、已有发布目录与版本文件未改。

## Validation

| 验证 | 结果 | 范围 / 证据 |
| --- | --- | --- |
| 仓库 Release build `-SkipTests` | passed | `build-release.log`；最终受影响项目重编见 `build-unit-final.log`、`build-sdk-final.log` |
| SDK/Core/Doctor `platform-sdk-targets` | passed | 三份 `focus-*.log`，真实构建/打包、分类、读取与历史恢复 |
| `platform-version-projection` | passed | `focus-version.log`，旧 schema 不变，新 schema/catalog 投影与坏 fixture |
| 完整 `DTMAPI.UnitTests` | passed | `full-DTMAPI.UnitTests.log`，无 focus；并非游戏 Mono |
| 完整 `DTMAPI.AuthorSdk.Tests` | passed | `full-DTMAPI.AuthorSdk.Tests.log`；`advancedReferenceFixture=executed`，含真实新会话往返和 legacy wire matrix；本轮未额外提供旧可执行文件入口 |
| 完整 `DTMAPI.InstallDoctor.Tests` | passed | `full-DTMAPI.InstallDoctor.Tests.log`，扫描保持只读 |
| 本地 SDK 候选 ZIP 与发行检查 | passed | `sdk-package-final.log`，最终源码候选已重建并通过实际 ZIP 检查；只包含 available 旧 target |
| 发行负例 | passed | `release-negative-cases.ps1`、`release-negative-final.log`：对最终候选改 catalog、换 contract、塞入 planned 载荷、额外 Abstractions，在重建 top inventory 后仍按对应原因拒绝；原候选树 digest 不变，fixture 已清理 |
| 冻结文件与发布版本 | passed | `frozen-before.json` 对照 contract/props 原字节不变；真实 SDK payload 的 DLL/refs 仍通过原嵌入契约；Runtime/SDK 版本、Product Catalog、订阅 manifest 未改 |
| 文档、链接与改动检查 | passed | `doc-governance.log`，7,848 checks；`doc-links.log`，52 个本地链接；diff whitespace 检查通过 |
| 游戏/Unity Mono/安装/存档/完整 Release 包矩阵 | not-run | 本项是 SDK 与托管包契约交付，PN-008 负责真实作者/Mono 验收 |

独立源码复核发现并修正三类兼容问题：旧 SDK 已允许的低 floor 包读取被新 writer 规则误拒绝、删 CodeModKind 绕过保留 marker、普通目标输入错误误报 internal/IO。均增加实际调用路径回归；不是只改测试期望。新 writer 的严格 floor 与旧 reader 的有界兼容分别验证。

## Evidence

本轮日志使用 [本地报告目录](../../../reports/platform-next/20260907-pn003/)；测试 fixture 使用现有 `DtmApiTestSession`。Unit 中已加载的 fixture DLL 在进程结束前不能删除，退出后的清理由现有清理工具执行并记录 preview/apply；不以恢复或删除掩盖测试失败。

## Rollback Notes

仅撤销本项增量，保留旧 target/payload、已验收 PN-001/002 和其他未提交文件。不修改玩家 Runtime、配置、存档、Workshop 或既有发布记录。

## Follow-up

PN-003 已验收。按状态表进入 PN-004：官方 Local 开发安装；PN-005 的可选服务与 ABI 可独立推进。新公共 API 与 0.7.0 完整载荷仍由 PN-007 交付。本轮没有发布 Runtime 或 SDK，没有游戏验证，不能把本地候选当作已发布版本。
