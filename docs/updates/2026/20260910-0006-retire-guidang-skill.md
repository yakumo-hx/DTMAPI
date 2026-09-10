# 20260910-0006: 归档技能抽样复核与退役

## Metadata

- Update ID: `20260910-0006`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求从旧对话摘要抽样对照原任务，评估额外信息价值；若意义极小则不保留技能。此处合并记录抽样依据和有界退役，不另写重复 Review 或本次聊天摘要。

## Summary

抽样对照 4 份历史摘要、对应已归档 Codex 任务及正式记录。技能是简短摘要器，不是完整对话导出工具；它能快速回忆主题，但本组样本未发现只有它才保存的长期规则、修复证据或未完成事项。按用户条件退役主动生成入口，保留 39 份旧摘要和技能原始字节。

## Changed Files

- 本地 `guidang` 技能移出 `.codex/skills`，原件及旧模板/目录说明留在已忽略的历史目录。
- `.codex/conversation-history/README.md` 与项目历史入口停止指引例行生成聊天摘要。
- 本 Update 和自动月表；不改写历史正文，不修改代码或游戏资产。

## Validation

- PASS：有效技能目录已移走；原技能的 2 个文件、原目录说明和模板共 4 个文件的 SHA-256 与迁移前一致；39 份历史摘要逐文件 SHA-256 不变。
- 原件和局部回退验证位于忽略的 `docs/archive/conversations/retired-tools/guidang-20260910/`，不进入公开 Git。
- 仅进行文档检查；不启动游戏、编译或重跑源码测试。

## Evidence

### 抽样对照

| 摘要 / 已归档任务 | 覆盖与缺口 | 现有信息负责文件 |
| --- | --- | --- |
| 20260830-1924，Y键控制台路线图0823 | 1.1.0/1.1.1 主线、玩家验收和发布身份保留；摘要未保留全量 Unit/Release 被既有断言阻断的具体限制 | [Y 输入修复 Update](../../archive/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md)及其 Manual QA |
| 20260809-1042，项目文件管理和内存空间治理 | 后半段证据/磁盘清理保留；同一任务前半段 Goal 退役、current-state 路由化、六项治理优化和月度索引没有进入摘要 | [治理重构](../../archive/updates/2026/20260711-0007-document-governance-streamlining.md)、[月度索引](../../archive/updates/2026/20260711-0008-monthly-update-indexes.md)、[产物协议](../../debug/protocols/test-artifact-retention.md) |
| 20260712-1805，梳理自动钓鱼路线 | 最后的 smoke 拆分、停止长测和 ISSUE-010 范围保留；早期“F6 是用户手按，自动输入未验证”的限定未保留 | [smoke 拆分](../../archive/updates/2026/20260711-0005-autofishing-smoke-architecture-boundary.md)、[人工辅助输入](../../archive/updates/2026/20260710-0005-input-frame-gc-ready-animation.md) |
| 20260714-0136，克朗垃圾分解器问题排查 | 当前分支的根因、原件保护、定点修复、未实机加载和 JSON 约束覆盖较好；继承的父任务讨论不误计为本任务漏项 | [根因及修档 Review](../../archive/reviews/manual-qa/2026/20260713-0002-garbage-shredder-last-run-log-review.md)、[JSON 作者规则](../../../author-docs/content-packs/json-derived-value-validation.md) |

对应任务 ID 依次为 `019ff5d1-63f1-71d0-849b-f6cf1c00ee78`、`019f4f94-a447-76f3-943f-725b1355bcfd`、`019f496d-e0ae-7790-a4de-d973ee9581bd`、`019f5c01-38cf-7190-9973-2b6175766bf9`。四个原任务当前均可读取；对照使用用户/助手可见消息，排除内部推理。垃圾分解器任务的父任务为 `019f55be-4571-7273-b72e-d5e54b330f8b`。

应用记录的四次归档耗时分别为 148.132、111.033、88.600、77.009 秒；这是各次归档 turn 的观测耗时，不是每次 Mod 修改的额外成本，也不据此预设未来节省比例。技能没有读取完整原任务、保存附件或绑定任务 ID/覆盖起止范围的步骤，因此不能承诺完整度。其优点是简短且多数结论可追溯；缺点是同一事实形成重复摘要，旧 TODO 也可能被误当成当前任务。

本结论来自四份样本的关键事实对照，不声称逐篇审查了全部 39 份摘要，也不声称已归档 Codex 原文具有永久备份保证。长期约束、知识、状态按现有负责文件记录；需要离线对话副本时另行按用户要求导出可见消息，声明任务 ID、时间范围、附件及缺项。

## Rollback Notes

将保留的技能目录移回 `.codex/skills/guidang`，恢复保留的旧 README/模板；仅回退本 Update 对历史入口和月表的修改。其他任务在建改动及 39 份旧摘要保持不动。

## Follow-Up

无。现有技能菜单可能仍显示当前会话已加载的旧项；磁盘上的主动发现入口已经移除。历史摘要与 Codex 原任务仍可读取。
