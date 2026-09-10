# PN-001: Version projection test baseline

## Metadata

- Update ID: `20260907-0002`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求继续平台实施，先完成 PN-001，保留其他未提交工作，并在实际验收后更新状态。
- Review: [平台基线 Review](../../reviews/code/2026/20260907-0001-platform-next-baseline.md)。
- Task: [PN-001](../../planning/platform-next/tasks.md)；架构决定 A03/A11 保持既定边界。

## Scope

拆开源码版本投影、第一方元数据关系和精确发布快照验证。普通 Unit 不重复保存发行摘要；Catalog/release checker 继续拥有发布校验。当前版本、Catalog、订阅记录及其他工作不因测试失败而改写。

## Authority reconciliation

原断言的旧 tree/payload 摘要属于 [8 月 9 日发布核对](20260809-0002-document-governance-and-product-authority-split.md)；当前 Catalog 的摘要属于 [8 月 20 日安装器发布](../../archive/updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md)。后者记录了新增入口、用户验收和发布后的逐文件一致性。当前 subscription manifest 路由的[最新发布 Update](../../archive/updates/2026/20260830-0002-moreequipment-101-workshop-release-closeout.md)另记录 MoreEquipmentSlots 1.0.1。Catalog 和这些来源一致，错误是验证代码复制了过时发布状态。

补齐 release checker 输入后，另发现其九个产物限制及 MoreEquipmentSlots 1.0.0 目标断言也已过时。现有 `Get-DtmApiReleaseContractAdvancedProducts` 已正确选择当前有精确发布观察的产品；调用者改为使用该集合，保留完整目录集合比较、未知/未公开产品排除和逐产物验证。发布专用测试继续固定当前集合，并用移除/添加观察记录证明集合只随实际发布观察变化，不随未来上传授权变化。

## Changed Files

- `tests/DTMAPI.UnitTests/PlatformVersionProjectionTests.cs`：三项独立职责为实际程序集/Core/Catalog 源码版本投影、第一方源码/官方/发布文字关系、受控错误投影与篡改负例；不保存发行 tree/info 摘要。
- `tests/DTMAPI.UnitTests/Program.cs`、`DTMAPI.UnitTests.csproj`：旧 `prerelease-step5`、新 `platform-version-projection` 和默认路径调用同一组测试；直接引用作者编译契约核对其独立 target。
- `tools/scripts/check-release-contract.ps1`：当前产物目录按既有 Catalog selector 精确比较；修正 MoreEquipmentSlots 发布目标；缺输入返回结构化校验失败。
- `tools/scripts/test.ps1`、`build-release-workshop-packages.ps1`：同一 selector 的调用者不再用旧数量及旧排除行阻止当前已发布产物。只运行产品 `PlanOnly`，没有实际准备共享上传目录或上传。
- `tools/scripts/test-dtmapi-060-release-artifact-set.ps1`：更新发布专用集合，增加真实目录 validator 的正确/缺失/多余/空输入负例。
- 本 Update、九月台账、平台任务状态：本项生命周期。

## Validation

- 仓库工具链 `build.ps1 -Configuration Release -SkipTests` 及修改后 Unit 项目 Release build 通过。
- `prerelease-step5` 先以原断言失败，修改后通过；新增 `platform-version-projection` 通过。受控负例实际执行，覆盖三个 Runtime 版本维度、产品身份/版本/最低 Runtime、官方版本和发布文字。
- 完整主 Unit 从头运行，退出 0、`DTMAPI.UnitTests: OK`，此前被阻断的尾部已实际执行。结束时一个 session cleanup-pending，随后既有 cleanup 工具核验并清理该已解锁会话。
- Product Catalog checker 通过。Release checker 无输入的初次调用失败；生成当前 SDK 和双份当前产品产物后，定位并修复两个过时断言，完整 checker 通过。没有改 Catalog、版本文件、发布摘要或订阅记录。
- 发布集合专用测试在 PowerShell 7 与 Windows PowerShell 5.1 均通过，包括逐目录精确集合负例、发布观察与未来上传授权隔离、产品 PlanOnly 及既有事务 fixture。
- 文档治理（7,784 项）与 diff 检查通过。没有运行完整 `test.ps1` Release 套件、游戏、安装或 Hook smoke；本任务无需这些验收，不能由上述结果推导其通过。

## Evidence

- `tmp/platform-next/PN-001/build.log`、`unit-build.log`。
- `tmp/platform-next/PN-001/prerelease-red.log`、`prerelease-step5.log`、`platform-version-projection.log`、`unit-full.log`。
- `tmp/platform-next/PN-001/catalog.log`、`release-contract.log`、`release-contract-with-artifacts.log`、`release-contract-green.log`、`artifact-set.log`、`artifact-set-ps51.log`。
- `tmp/platform-next/PN-001/release-artifact-root.txt` 路由本次 SDK/双份产品产物；`sdk-build.log`、`product-builds.log` 记录构建。它们仅为本地非发布验证输入，不是新发布权威。
- `tmp/platform-next/PN-001/cleanup-preview.json`、`cleanup-applied.json`、`docs-final.log`。

## Rollback Notes

只撤销本项测试与状态改动；保留复现证据，不回滚发布权威以迎合旧断言。

## Follow-up

PN-001 验收通过，解锁 PN-002；完整平台作者/Mono 集成仍由后续任务验收。
