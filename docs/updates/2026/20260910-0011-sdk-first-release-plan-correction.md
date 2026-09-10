# 20260910-0011: SDK 首次发布事实与近期执行计划修正

## Metadata

- Update ID: `20260910-0011`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户明确旧 CSV 是已允许清退的第一方临时接口，不需要恢复；旧 SDK 从未公开发布。要求修正详细计划后沿用图中任务实施。

## Summary

撤销 PN-042 中的 CSV 恢复/适配及其发布阻塞，保留该精确历史删除。0.7.0 SDK 按首次公开交付建设：取消面向旧 SDK 用户的通用迁移器、旧客户端和历史工程重建承诺，改为转换当前实际使用的内部工程/fixture。已发布 Runtime、现有 Mod、API/包/来源和玩家数据的真实兼容边界保持。

SDK 完整标准构建与产品验收不缩减。PN-042 先核对真实兼容范围，有实际退化才做 b 修正；与 PN-041 共用一次最终候选验证。本轮只修正文档，然后将已细化批次交给用户指定的现有任务继续实施。

## Changed Files

- SDK AB-02/08/09、作者交付和包/会话说明区分未发布作者工具与实际玩家契约。
- PN-041/042 详细执行包、方法规格、路线/任务/验收/状态/候选及接手提示。
- 既有 Review 添加用户更正路由，保留当时推理与历史验收；本 Update 拥有最新更正和交接事实。

## Validation

- 当前目标任务与其已完成多平台 0.7.0 适配已核对；生产迁移未在本任务运行。
- PASS：当前规格已无 CSV 恢复或假定旧 SDK 已发布的执行要求；历史 Review 通过更正路由保留，不改写旧实测。
- PASS：16 份受改文档的本地链接/锚点、文档治理、月度投影与 diff 检查。verified 仅表示计划/治理修正完成，不表示 SDK 已实施或发布。
- SDK/Mono/游戏/安装器验收：本轮 not-run，由后续实施批次按准确候选完成。

## Evidence

- 用户本次明确事实为更正权威；[上一方法 Review](../../reviews/code/2026/20260910-0005-platform-method-validation.md)的 CSV 恢复推论已被撤销。
- [SDK 详细执行](../../planning/platform-next/execution-sdk-msbuild.md)、[现有 Mod 兼容执行](../../planning/platform-next/execution-compatibility.md)和 [状态](../../planning/platform-next/status.md)。

## Rollback Notes

只撤回本轮文档改动；保留当前生产代码、历史候选和其他工作树改动。不得以回退文档重新启用已被用户取消的 CSV 恢复或推定旧 SDK 已发布。

## Follow-Up

已成功向 `01a088de-e9e1-7f60-bbd0-9c31d9653547` 发送接续实施指令，保留原模型/思考设置和当前工作区，明确两项用户更正。执行 PN-041.a–f 与 PN-042 实际兼容检查/必要修正，共用新的实施 Update，最终集中验收交回，不自动 M4 或上传。没有新建任务或恢复另一暂停任务；具体实现进度由该任务及实施 Update 拥有。
