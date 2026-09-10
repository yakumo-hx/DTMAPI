# 20260910-0002: SDK 审计后的执行计划与交接

## Metadata

- Update ID: `20260910-0002`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `source, docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户暂停旧任务，要求结合六份 SMAPI/SDK 审计和新的流程优化修订计划，并新建 Astra/high 实施任务；[本轮决策](../../reviews/code/2026/20260910-0002-sdk-author-path-plan.md)。

## Summary

将原“仅文档收尾”改为 SDK 正常作者路径修正：R01 委托、R02 原生泛型、R03 常量/XML/普通库、R05.a 资产选择及 R04 准确 ZIP 材料。保留成熟平台路线及已经成立的基础，M4 不等待完整 MSBuild/NuGet 生态。原任务维持暂停，新任务在当前工作区连续执行。

## Changed Files

- platform-next 的执行包、队列、路线、任务/验收及入口：指定本批实施顺序、代表性作者证据、版本安排和停止点。
- 作者交付 AD-11/12、包契约 P08：记录工程输入与原生 V2 的技术选择，保留旧 reader/冻结 payload。
- 候选页和原 PN-031.a Update：保留原包事实，将新缺口路由到本批，不再声称剩余改动仅为文档。
- 本轮 Review：记录外部研究的采纳/边界及流程影响；旧 Review 只追加 successor。

## Validation

- 已阅读六份外部报告，并核对现行源码、SDK 模型与本地固定 SMAPI 的相关 targets/说明。
- 方法标志枚举检查确认 Runtime=3 被 Native=1 的旧位测试误中；没有在本规划任务重跑外部作者工程，也没有宣称缺陷已修复。
- 已确认旧实施任务为 interrupted/idle；项目为当前 Git 工作区，按用户既有指令直接在原工作区创建新任务。
- 文档治理通过（9359 检查）；16 份相关文档的 211 个本地文件链接均存在，git diff --check 通过。
- test.ps1 -List 只读确认九个具名阶段，与执行包一致；没有把 List/Stage 解释为运行测试通过。证据保留索引 -Check 通过，无需重建。
- build/Mono/玩家验收：本规划任务 not-run；实施出口在 execution-sdk。

## Evidence

输入审计为 `E:/Python_project/SMAPIlearning/SMAPI_technical_study/reviews/2026-09-10-dtmapi-0.7.0/`；报告保有原 r2 的复现，不覆盖其 _sdk/_probes/_evidence。当前未提交流程修正的事实归 [20260910-0001](20260910-0001-execution-workflow-repairs.md)。原候选与实机证据继续由 [PN-031.a](20260909-0019-platform-release-preparation.md)路由。

## Rollback Notes

仅撤回本次规划/路由修订，不恢复全工作树或覆盖流程、TemplateCreator、Wiki 等已有改动。没有生产实现、安装、玩家数据修改或上传。

## Follow-Up

规划与文档检查完成；交由新 GPT-6 Astra / high 项目任务，环境 local，按 execution-sdk 连续完成本批并统一交回。实际新任务 ID 归 status 的接手位置，本 Update 不重复维护执行进度；本记录 verified 仅指规划交付，不代表任何 SDK 缺陷已修复或产品验证通过。
