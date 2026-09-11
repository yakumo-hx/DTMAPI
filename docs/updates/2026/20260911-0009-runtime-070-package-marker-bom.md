# 20260911-0009: 0.7.0 旧包 marker 的 UTF-8 BOM 兼容修复

## Metadata

- Update ID: `20260911-0009`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户在[玩家兼容性研究](../../reviews/code/2026/20260911-0004-player-070-subscribed-mod-compatibility.md)后授权最小修复、必要测试及同步上传目录，并明确两个 Runtime 分发都保持 0.7.0。Y 1.1.2 发布观察已归 [0008](20260911-0008-y-console-112-publication-observation.md)。

## Summary

普通 `dtmapi-package.json` reader 忽略文件开头一个标准 UTF-8 BOM，使内容相同的历史包获得相同读取结果。保留原始 64 KiB 限额、深度、schema、身份、目标、文件绑定与降级拒绝规则；空文件及仅含 BOM 的文件明确报格式错误。Windows 和多平台的修复包已同步到上传目录，均保持 **0.7.0 / 0.7.0.0**。用户随后发布了两份补丁，准确发布及订阅确认归 [0011](20260911-0011-runtime-y-hotfix-publication.md)；本条保留原同步与测试事实。

继续保留 SDK marker 校验，是为了让声明的 schema、目标与 manifest/DLL 绑定一致，并防止删除 kind/schema 后绕过已声明契约。无 marker 的旧包仍走原兼容路径。修复读取编码即可处理本次已复现的退化，无需取消准入或改变其他旧 Mod 的分类规则。

## Changed Files

- `src/Shared/AuthorPackageMarker.cs`：最小字节偏移处理，供 Core、Doctor 及 GameBridge 内容重载共用。
- `tests/Shared/PlatformPackageTargetMatrix.cs`：合法旧格式、schema 1/2 及现有坏包反例的 BOM 对照与只读性检查。
- `tests/DTMAPI.RuntimeIntegration.Tests/Batch6AdvancedRuntimeTests.cs`：原生兼容 CodeMod 的 BOM 分类及实际冷启动 Entry 回归。
- 两个 Runtime 官方上传叶：验证后同步，保留工坊控制文件及整个 `info.json` 原字节，版本不变。
- Catalog 的两个 `localUploadSuccessor` 和候选路由：记录同步时的补丁；发布后的当前收据由 [0011](20260911-0011-runtime-y-hotfix-publication.md) 更新，原证据保留。

## Validation

- PASS：新增 BOM 对照在旧 reader 上准确失败于 `current-schema2-utf8-bom`，修复后 `test-unit.ps1 -Focus platform-sdk-targets` 通过。既有无 schema 元数据、schema 1/2 正例与目标、重复字段、身份/版本、hash、删 kind/schema 等反例均增加 BOM 对照；Core、GameBridge 结果一致，读取不改 marker 原字节。另覆盖空文件、仅 BOM、截断/重复 BOM、字面乱码、正文 BOM、错误 token 和含 BOM 超限文件。
- PASS：Doctor 测试通过，包含使用同一矩阵的 `PlatformSdkTargetsMatchPackageBytes`。本次修复 Doctor 的共享源码；没有重新发布 SDK 或给 Runtime 包增加 Doctor。
- PASS：`test-unit.ps1 -Focus batch6-advanced-core` 通过。旧原生兼容 CodeMod 的无 BOM/BOM 分类一致，BOM 原包实际经过离线 Runtime 冷启动 Entry、异常隔离、停用和禁止本进程重入断言；Strict/Advanced 原拒绝边界保留。
- PASS：独立检出基于 `cc7044a7`，其 Runtime 源码与已发布 r6 的 `1002ae05` 相同；补丁提交为 `7d26482a2a95`。相对 r6，在 `src`、`products`、`author-sdk` 和 `tools/release` 范围只有 `src/Shared/AuthorPackageMarker.cs` 改变。采用 [0004 的具名 Runtime 构建入口](20260911-0004-test-failure-triage-and-build-scope.md)，来源检查、强制 Rebuild、构建中输入不变和隔离检出的 Catalog 包门均通过。没有提交主工作树中其他 SDK/地图改动。
- PASS：对最终包内准确 Core 运行既有六组旧包探针：CodeMod/ContentPack 的 plain 与 BOM 均接受，schema 999 均拒绝。随后把五个准确发行 DLL 放入独立测试宿主，用 `-NoBuild` 重跑上述两个 focus，全部通过；不以构建前测试 DLL 的 hash 代替包内字节。Abstractions 仍与原 r6 完全相同。
- PASS：准确 Windows 候选执行 `test-runtime-workshop-installer-061.ps1 -PackageRoot <candidate>`，复杂路径、BAT/CMD、PowerShell 5.1、安装/检查/收集/卸载等现行矩阵通过；脚本名 061 不表示本次输入版本。
- PASS：多平台 builder 自带审计，以及准确上传叶的 `test-dtmapi-multiplatform-package.ps1 -SourceKind Candidate -AcceptedPackageRoot <本次 Windows> -AllowWorkshopControlFile`。21 个共享文件一致；两个安装器 host 沿用已验证的 `d76423f5` 构建，PowerShell 5.1/WSL Bash 语法均通过。
- PASS：`test-multiplatform-runtime-installer.ps1` 使用原 schema 1 / 0.6.1 包作为 `-PreviousPackageRoot`。Windows 与 WSL Linux 两端均完成生命周期、四个 shell 入口、0.6.1 升级/撤回重装、来源/损坏包拒绝、事务恢复、外部文件保留及日志链接隔离矩阵。WSL 的 localhost 代理提示未改变测试出口；两端最终均 PASS。
- PASS：准确原 r6/r2 → 本次包的**同版本**专测。Windows PowerShell 安装器直接覆盖更新成功；多平台在 Windows/Linux 上均以 `DTM-E1202` 拒绝同版本不同构建且不写游戏树，随后使用新包执行卸载、安装、健康检查成功，所有外部插件/配置/Mod/ContentPack 哨兵不变。
- PASS：持锁备份并同步两叶，逐文件长度/SHA256与候选相同（另加原 33 字节 Workshop 控制文件）。两份 `info.json` 与绑定文件逐字节保留，锁已释放；未修改实际游戏安装、启用状态或存档。
- 当前工作树 Catalog 整体仍 **FAILED (2)**：`tests/mod-fixtures/qa/HeartMap/manifest.json` 未登记及源 manifest 数量 22/23 不符，修改前后相同。最终检查显式传入真实 `-ExpectedRuntimeBuildCommit 7d26482a2a95` 后，准确包没有额外报错；不修改其他任务的登记，也不称全仓 Catalog PASS。
- PASS：文档治理 9,967 项、月表同步、61 个本地链接、JSON 限定变更和差异空白检查。三个源码/测试文件与通过验证的独立检出逐字节相同。未启动实际游戏；本次解析器修复不需要重跑 Mono 游戏流程，不能扩大为第三方原包全部玩法或 Steam Deck/CrossOver 实机验收。

测试过程保留了未通过的尝试：初次独立检出漏同步已更名构建辅助函数的调用方；历史 policy JSON 的 CRLF 检出不等于 registry 绑定的 LF 原字节；Catalog 需要现存的忽略目录历史 JSON 证据。均补齐已有输入后按原门重验，没有改 hash、删反例或伪造历史结果。旧 reader 对省略最后一个 `}` 的容忍属于原有解析器行为，本次未更换解析器或新增全量 JSON 语法策略；新增非法 JSON 用例采用明确错误 token。初次用旧 Catalog 核对新版描述、用当前 HEAD 核对独立构建提交的两次审计也保留，最终使用当前元数据和明确的真实提交参数。

### 上传目录与同版本更新方式

| 分发 | 已同步叶 | 内容收据（不含根控制文件） |
| --- | --- | --- |
| Windows | `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI` | 28 文件，4,225,850 字节 |
| 多平台 | 同级 `DTMAPI_MultiPlatform` | 37 文件，30,186,688 字节 |

Windows 可以直接运行新包的安装入口。**已装旧 0.7.0 的多平台用户，需要先运行新包的卸载入口，再运行安装入口**；本次已验证这一顺序，卸载仅作用于 Runtime。版本保持不变，因此必须用本次提交/文件收据区分修复前后，不能只看“0.7.0”判断是否拿到补丁。新装或从 0.6.1 升级按原入口操作。

## Evidence

- `artifacts/runtime-070-bom-fix/windows/DTMAPI` 与 `multiplatform/DTMAPI-MultiPlatform`：本次准确候选；`final-upload-receipts.json` 拥有完整文件收据和 normalized tree hash。
- 同证据根的 `targets-before-fix.log`、`exact-package-target-matrix.log`、`exact-package-legacy-cold-load.log`、`doctor-targets.log`、`exact-payload-classification.json`：回归与准确载荷测试。
- `windows-installer-matrix.log`、`multiplatform-installer-matrix.log`、`same-version-update-result.json`：现行安装器及同版本更新路径。
- `upload-sync-result.json`、`exact-upload-multiplatform-audit.log`、`current-catalog-final.log`：同步和最后检查。安装器临时目录按脚本清理；最后一个离线 Unit 的 cleanup-pending 会话经现有 cleanup 工具预览确认仅属本任务后清理，收据归 `test-cleanup-result.json`。
- 独立源码检出为 `E:/Python_project/DTMAPI-bom-070`，分支 `codex/runtime-070-marker-bom`；构建/测试路由复用 0004 当前脚本，其内容不混入 Runtime 载荷。研究原始证据保持原位。
- 研究阶段准确 0.6.1/0.7.0 差分与原附件归上述 Review；不覆盖旧候选或原始玩家资料。

## Rollback Notes

源码按本次小补丁回退；上传叶原件分别在证据根 `before-dtmapi-windows` / `before-dtmapi-multiplatform`，均经过完整收据核对。旧叶临时交换目录已清理，候选和回滚备份保留。保留其他工作树改动及原有发布/订阅收据，不改变存档、启用状态和实际游戏安装。

## Follow-Up

本次修复、必要测试与双上传目录同步完成。用户发布后的准确订阅一致性已在 [0011](20260911-0011-runtime-y-hotfix-publication.md) 核实。第三方“更好的体验”原始 marker、DLL 及修复后的玩法反馈仍未取得，本修复不关闭那项原包实测缺口；原有 JSON 截断容忍也不在本次编码修复中收紧。
