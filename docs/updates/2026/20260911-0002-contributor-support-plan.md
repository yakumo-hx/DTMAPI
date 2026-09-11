# 20260911-0002: 其他开发者支持计划

## Metadata

- Update ID: `20260911-0002`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求读取有关记录，独立设计 0.7.0 远端源码交付和 0.7.1 有限 Linux 构建测试；不改当前主线内容/API 计划，不执行上传或源码实现。

## Summary

新增[其他开发者支持计划](../../planning/contributor-support.md)，以已验收 0.7.0 的 D7/r6/r2 为基线，定义固定公开边界、准确提交导出、普通 Git 三方回流和单一公开文档来源。Linux 仅覆盖声明范围内的源码构建与通用测试，共用现有 PowerShell/.NET/MSBuild 实现；已有 Windows 发布和多平台玩家包边界保留。

计划为 `proposed`，DEV-01–08 均未实施。本记录的完成只代表计划文档交付，不代表公开远端更新、Linux 验证或脚本拆分完成。

## Changed Files

- `docs/planning/contributor-support.md`：范围、当前事实、公开目录/同步设计、测试分类、WSL 验证、并行顺序、成本、验收与回退。
- `docs/planning/README.md`：独立计划入口，不修改 platform-next 主线队列。
- 本 Update 与 `docs/updates/INDEX-2026-09.md` 的对应投影行。

## Validation

- 两路独立只读复核完成：源码公开/回流设计与 Linux/测试范围分别检查。已补齐同步基线生命周期、最终合并树公开边界，并将纯说明检查与导出/回流行为检查分开，避免普通文档修改触发整套测试。
- `tools/scripts/check-doc-governance.ps1 -Quiet` 通过；本次新增文档的 26 个本地链接均存在；规划索引和月表的 `git diff --check` 通过。
- 本次仅文档变更；未执行构建、Linux 测试、真实游戏、SDK 重包或任何远端写入。

## Evidence

- 最新基线沿用 [20260910-0012](20260910-0012-sdk-msbuild-first-release.md) 中 D7/r6/r2 的验收事实，不新增或改写 PASS。
- 已读取“工作树与远端推送0910”及相关源码/测试。当前问题与未来设计明确分开；用户截图只作为对方未核验的观察。
- 中文计划采用 `humanizer-zh` 的清晰表达要求；未改第三方原文、许可证或技术标识。

## Rollback Notes

如撤回本提案，只删除新计划及其入口，并按治理规则记录撤回；不使用整文件回滚覆盖已有规划索引或月表中的其他任务变更。此次未改产品和脚本。

## Follow-Up

- 若开展实现，从计划 DEV-01 锁定准确源码与公开依赖开始，按实际实现另建 Update。
- 未承诺 Linux 完整 SDK、发布门禁或 Steam Deck 实机支持。0.7.0 上传和主线 API 开发仍归各自既有任务。
