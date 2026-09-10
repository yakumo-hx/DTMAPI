# 20260908-0008: Workspace outputs and audit delivery

## Metadata

- Update ID: `20260908-0008`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户批准的工作空间建设计划，输出归属、可重建产物清理及审计交付适配。

## Summary

本地缓存、临时会话、交付物与长期证据各用明确目录。审计包引用一套正文和完整工具目录，按本次选择携带证据；替换和清理先验证范围，未知或唯一材料保留。

## Changed Files

- 审计包构建器及归档工具测试。
- 输出/保留说明、具体清理清单与必要验证证据。

## Validation

- `test-audit-output-boundaries.ps1`：9 项通过，覆盖越界/源码重叠、重解析点、替换失败恢复、既有交付保留、空证据默认及冲突 ZIP。
- `test-document-archive-tools.ps1`：通过，中文附件和归档正文在源码快照只保留一份，审计入口只导航。
- 公开入口后段诊断发现两处夹具宿主问题：Windows PowerShell 的 junction 清理方式和无 BOM 中文脚本编码。现对夹具自建链接核对路径、属性、唯一目标后非递归移除；未知 reparse 仍拒绝。归档测试只补 UTF-8 BOM，中文附件和字节保持用例不变。两个脚本在 WinPS 5.1 / PowerShell 7 各一次通过，共约 2 秒；生产审计工具没有为此放宽边界，证据见 `runner-test-adaptation/audit-document-fixtures-validation.json`。
- 首次只读输出清单：725 个对象、73177 个文件；152 个有生成标记的未引用历史对象待复核。清单本身不授权删除。
- 重复副本证明已完成：207 个 SDK 对象共 39888 文件逐文件核对 SHA，形成 26 组目录身份和 26 组 ZIP 身份；保留 52 个 canonical 对象，确认 155 个重复副本、29889 文件、19667942297 字节。近期、被引用、未知来源和唯一对象不在删除集合内。
- 限定清理器的 PowerShell 5.1 / 7 合成场景通过，覆盖哈希绑定、重解析点、活跃进程、canonical 同长度篡改、删除中断与恢复。实际预检验证 155 个对象和使用的 32 个 canonical，执行时重新检查边界、引用绑定与活动状态，并逐对象写入日志。
- 实际清理完成：首轮删除 15 个对象后因短时活动进程暂停；确认进程退出后，按同一指纹清单和日志续跑删除剩余 140 个。后验确认全部 155 个候选已不存在，52 个 canonical 及 78 个父目录仍在；共移除 29889 个重复文件、19667942297 个逻辑字节（约 19.67 GB）。不把并发文件活动下的磁盘空闲变化当作精确节省量。具体结果在 `sdk-cleanup-completed.json`。
- 实际审计交付完成：从源码快照提交 `db4ed0fd44bbc78f1dfb54f4bce3156f528e2639` 生成完整审计包，构建及独立 SelfAuditOnly 通过（合计 113.843 秒）。ZIP 为 83593221 字节、4376 个条目，SHA-256 `e748ac51653c504a4a5f1a421a4f59f33b3005196ad9ed395b981a289ecaa655`。内含 115 文件的本次验证报告 ZIP，以及 5 份选定游戏证据。
- 实际 ZIP 内容另行核验通过：五份冻结文本逐项匹配迁移前原件 SHA，五份游戏 result 各出现一次并保持原字节，完整阅读清单及验证报告 ZIP 与源文件相同；未包含玩家存档、私有对话目录、修档格式文件、Wiki 并行区或 DLL/EXE/转储载荷。交付快照之后只登记本段验收收据，不为收据说明重建同一包。
- 最终两条完整入口结束后的受管会话预检为 0 个待清对象、无活跃进程；未因收尾扩展到系统临时目录或未知材料，见 `final-session-cleanup-preview.json`。本次引用的 runner 初始结果与最终 ABI 报告已按原字节保留到长期证据，避免依赖以后可能清理或覆盖的临时/`latest` 文件。

## Evidence

- [完整审计包](../../../dist/audit-packages/DTMAPI-audit-package-Refactor.zip)、[验证报告 ZIP](../../../dist/audit-packages/workspace-construction-validation-20260908.zip)。本地交付未上传或发布。
- [实际构建/自检](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-audit-result.json)、[实际 ZIP 字节与范围检查](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-audit-content-check.json)。
- `docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/outputs/`。
- 主记录：[结构与历史](20260908-0002-workspace-structure-and-history.md)。
- 使用入口：[本地产物与保留](../../workflows/local-output-storage.md)。

## Rollback Notes

按本包提交和迁移/清理清单回退。未确认可重建或未找到 canonical 副本的材料不删除；玩家原件、冻结发布基线与 Wiki 并行内容保留。

## Follow-Up

本包已完成并归入总体验收。以后按现有保留策略处理新产物，不向普通 Mod 修复加入全盘清扫步骤。
