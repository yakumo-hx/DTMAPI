# 20260908-0009: Platform plan reconciliation after workspace construction

## Metadata

- Update ID: `20260908-0009`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求结合基础设施建设、Pro 上传包研究和 19 篇 SMAPI 研究修订既有计划，精简旧执行手续而不缩减任务，并新建 Astra high 任务在当前工作区完成近期实现；[本次取舍 Review](../../reviews/code/2026/20260908-0009-platform-plan-reconciliation.md)。

## Summary

保留 M1–M6、全部能力与既有任务，修正与当前开发/测试/治理不一致的执行条款；将配置正确性、SDK160、保存真实提交窗口、内容深层隔离和无作者反馈时的兼容维护纳入对应任务。当前任务只做核对与文档；生产实现交由用户指定的新任务。

## Changed Files

- platform-next 的 tasks/status/roadmap/acceptance/README/capability-map。
- 主架构与作者交付、Runtime、数据内容领域设计。
- 本 Review、Update 与通过 sync 生成的当月记录。

## Validation

- 已核对当前源码、分图测试入口、SDK 准备/单次打包路径、治理与保存模式；三份问题源与研究基线字节一致，保留已有 F01 探针事实，不冒称本轮复现。
- `tools/scripts/check-doc-governance.ps1 -Quiet` 通过；本次改动文档的本地文件链接均解析存在。
- 一次性只读核对确认保留全部 35 项任务规格，执行队列包含 PN-007 a/b 和配置子项共 37 行，前置任务可解析且没有环；人工核对 R1–R6 的契约进入条件、保存四种结果和同一候选复用规则。
- `git diff --check` 通过；`src/tests/tools/author-sdk` 无本轮生产修改。未因规划文档变化重复执行 build/全套测试，静态源核对不记作 Runtime PASS。
- 本任务不修改生产源、不运行游戏；没有新的 smoke 或发布。

## Evidence

输入路径、研究基线限制、已采纳/未采纳理由及具体源码责任见 Review。检查在基线 `95f3cb32` 的当前工作区运行；结果只证明本轮计划与记录一致。没有新增产品、Mono、SDK 发布或存档证明，不创建新的保证/缓存系统。

## Rollback Notes

只撤本轮计划增量，保留工作空间建设、历史记录、Wiki 未提交工作和原有实现。尚未改变游戏、配置、存档、SDK 载荷或发布包。

## Follow-Up

已创建实施任务 `01a0816b-178f-72e2-a411-ceabbe7bd214`，host `local`，模型 `gpt-6-astra` / `high`，直接使用当前工作区。布置范围为配置短修复及整条 M1，真实验证后在同一任务做 R1 有界收口，再报告 M2 接手位置。

通过 wait_threads 确认任务 active / inProgress，已核对配置唯一格式化调用和备份失败路径，开始配置修复；尚未宣布任何实现完成。以上计划检查在实施任务启动前完成，后续代码与验收由其自身 Update/status 承接。本记录的 verified 仅指规划交付。
