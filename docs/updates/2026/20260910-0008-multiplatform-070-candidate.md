# 20260910-0008: 多平台安装器 0.7.0 候选

## Metadata

- Update ID: `20260910-0008`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户执行 [PN-031.multi](../../planning/platform-next/tasks.md#pn-031multi070-多平台玩家安装候选)；沿用既有 installer ownership/transaction 边界，无需新 Review。

## Summary

完成 0.7.0 多平台安装渠道候选：保留 schema 1 / 0.6.1 固定发布来源，为准确 Runtime r5 增加 schema 2 Candidate 分支；0.2.0-experimental Windows/Linux 安装 host 来自已提交的真实源码。新包独立输出，不改变 Steam 观察或旧分发目录。

## Changed Files

- `multiplatform-package-common.ps1`、builder/auditor：明确来源分支，校验 Runtime/manifest/版本及逐文件来源；新候选要求显式输入与独立输出，从旧 metadata 投影版本。固定旧 receipts 和 Catalog 分支保留。
- `build-multiplatform-runtime-installer.ps1`：复用现有 MSBuild 输入闭包校验，从真实提交构建两端 host，并自动生成事实证明；不新增手填准入/审批文件。
- C# `PackageLayout` / `InstallerModels` / csproj：schema 1/2 有界 reader，候选来源/所有共享文件/内嵌安装器提交检查，准确文件版本；r5 产品定义 JSON 随诊断工具安装。install-state 和 transaction schema 仍为 1。
- `test-multiplatform-runtime-installer.ps1`：在既有矩阵内增加每端 8 种错误来源、旧包识别、实际升级、撤回重装、降级及旧 reader 拒绝；保留原事务、链接、路径、sentinel 和跨引擎断言。
- 当前 installer boundary / test matrix、平台状态与候选选择同步本次事实。Catalog 与 subscription manifest 不变。

## Validation

- PASS：.NET 8 两端 self-contained publish，真实提交的编译输入构建前后不变。
- PASS：准确新包完整结构审计和旧 0.6.1 原包固定 receipt 审计；PowerShell 5.1 / WSL Bash 语法未跳过。
- PASS：Windows 和可用 WSL Linux 实际 host 安装、只读状态、两次日志、卸载、重复卸载；两端旧版升级/撤回重装和错误来源拒绝。
- PASS：原 6 个安装阶段、3 个卸载阶段、跨引擎事务互拒、来源冲突、外部 sentinel、路径与链接矩阵；Linux 外部 Player.log 父目录 symlink 防泄漏。
- PASS：原子收据恢复单元测试；ZIP 每个 entry 与审计后的候选字节相等。
- 未运行真实游戏，因此 Runtime Validation 仍为 not-run。Steam Deck/SteamOS、Proton、macOS/CrossOver 和设备输入不记 PASS；没有重跑 SDK 全套或 Windows 游戏长测。
- PASS：文档治理检查；原始结果见证据目录的 closeout 日志。

## Evidence

- [完整证据、精确包/host hashes、原始执行结果和复现命令](../../../artifacts/pn031/multiplatform-070-evidence/README.md)。输入仍为已验 Runtime r5，选用多平台 candidate r1。
- [候选 ZIP](../../../artifacts/pn031/multiplatform-070-candidate-r1/DTMAPI-MultiPlatform-0.7.0-candidate-r1.zip)。SDK 不进入玩家包，两个 host 也不进入游戏树。
- 为复验旧分支，持锁只读复制已发布 Windows 0.6.1 的精确源文件到独立证据目录，随后释放锁；没有 live upload、订阅或真实游戏写入，fake-game 会话已清理。

## Rollback Notes

撤销本 Update 对应代码可恢复旧构建入口，旧分发目录仍原样保留。新 host 支持旧 schema 1；运行中降级拒绝，撤回路径是 Runtime-only 卸载再显式安装旧包，外部文件保留。新 schema 2 不要求旧 host 读取。

## Follow-Up

PN-031.multi 本切片完成。候选尚未发布；实际平台游戏启动/输入、以及任何公开上传沿各自授权和验收流程，不以本次安装器结果代替。
