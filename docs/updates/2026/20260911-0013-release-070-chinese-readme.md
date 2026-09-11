# 20260911-0013: 整理 0.7.0 中文 README 与作者入口

## Metadata

- Update ID: `20260911-0013`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户授权在当前工作空间优化并提交 0.7.0 远端中文说明；沿用[公开交付设计](../../reviews/code/2026/20260911-0003-public-source-delivery-design.md)，没有新架构决策，不另建 Review。

## Summary

根 README 按玩家、Mod 作者和源码贡献者整理入口，说明现有构建命令与环境前提。使用 humanizer-zh 核对中文表达，保留版本、路径、字段与技术限制。SDK 首页补充中文导航，内容包作者入口纠正旧 Advanced 未开放的说法。

Windows 与多平台 Runtime 0.7.0、Y 1.1.3 已发布；Runtime marker 的 UTF-8 BOM 修复包仍以 `7d26482a2a95` 为构建来源。Author SDK D7 只记录已完成 Windows 范围验收，GitHub Release 独立附件尚未发布。文档修改不改变原包身份或验收范围。

## Changed Files

- `README.md`：中文介绍、读者入口、发布状态、构建范围、仓库导航与资料边界。
- `author-sdk/README.md`：中文首页导航和 SDK 发布状态，后续命令与兼容正文保留。
- `author-docs/README.md`：链接当前 SDK，区分开放 V2 与旧凭证路线。
- 本 Update 与月表：本次文案范围和检查结果。

## Validation

- PASS：55 个本地链接均存在；定向验证锚点、公开测试与构建参数已核对脚本。版本、NativeContractVersion、目标框架及附件状态与现有权威记录一致。
- PASS：文档治理（10113 项）与三个指南的差异空白检查。月表仅新增本任务一行，其余工作区段落保留。
- not-run：游戏、完整 Release、SDK 重包。本次只改说明，不改变产品输入。
- 远端 CI 不在本任务中判为通过；交接时的契约 hash 失败由原任务处理。

## Evidence

- 发布事实：[发布确认](20260911-0011-runtime-y-hotfix-publication.md)。
- SDK 验收与附件范围：[D7 交付记录](20260910-0012-sdk-msbuild-first-release.md)、[候选说明](../../planning/platform-next/release-candidate.md)。
- 原源码归集：[0012](20260911-0012-release-070-source-consolidation.md)。
- 本次检查输出保存在 `tmp/readme-070/`，不纳入公开提交。

## Rollback Notes

通过普通后续提交撤回本次文档差异；不覆盖并行地图、Wiki、源码或其他月表段落，不移动标签，不改变玩家包、游戏目录或存档。

## Follow-Up

文档检查完成；本任务提交仅投影上述文档到现有公开分支，公开提交收据保存在 `tmp/readme-070/public.json`，PR #3 保持草稿。正式目录、许可补件、干净公开源码验收及 Linux 贡献者构建仍按原设计另行推进。
