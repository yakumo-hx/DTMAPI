# 20260910-0009: SDK 标准构建架构与 0.7.0 完整迁移计划

## Metadata

- Update ID: `20260910-0009`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求吸收 r5 构建专项研究、迁入网页版 Pro 材料，转向 SDK 架构并完整规划 0.7.0；决定见 [Review](../../reviews/code/2026/20260910-0004-sdk-msbuild-architecture.md)。

## Summary

采用标准 MSBuild 单一正式构建后端，PN-041 从后续原型升级为首发前完整迁移。保留冻结引用、Native/依赖/Doctor/交付与运行验证，撤销继续扩充自有 csproj 子集的长期方向。默认作者包计划携完整标准工具链和基础离线输入；旧 SDK/已交付包与历史证据保留。

本轮只调查、决定、移入资料并修改工程记忆，没有实现新后端、改版本、构建新 SDK、启动游戏或上传。旧 r5 的子集验收仍是有效基线，但 0.7.0 完整 SDK 新放行门待 PN-041。

## Changed Files

- 新 SDK 构建架构及 PN-041.a–f 连续执行包：工程/工具链/恢复/引用、准确暂存/打包、迁移、IDE/CI/Mono 和一次切换门。
- 原作者交付 AD-01/11/12、P06、主架构路由、平台 roadmap/tasks/status/acceptance/capability/handoff/candidate 同步新职责及优先级；旧执行包只增加后继路由，不改写原修复结果。
- 迁入两份用户研究原文，Review 记录采纳与限定；本 Update 记录迁入身份和验证。SDK 当前作者指南仍描述现有实现，不提前宣传新后端能力。

## Validation

- 已核对当前相关源码与最新多平台 Update；完整审阅四份本地专项研究、网页版 Pro 原文及包内依据。研究任务的旧编译日志作证据，本轮未重复运行其探针。
- 原始输入迁入后 SHA256 相同，下载目录原文件已移除；附件原件仍保留。
- PASS：两次独立只读复核确认构建/交付与验收主干可实施；已补正同名 ZIP 冲突策略、封包目标顺序、必需 SDK203、无代码工具与 Mono 行号边界，0.7.0 不新增 no-build。
- PASS：目标文档本地链接、当前计划路由/旧批次后继、文档治理、证据保留索引 Check 和 diff 检查。生产构建、SDK 行为、IDE/Mono 测试不属于本轮规划完成证明，全部留在 PN-041；本记录 verified 只表示规划与资料治理完成。

## Evidence

- 从 `D:\下载\DTMAPI-SDK-scope-evidence-20260910.md` **移动**到 [治理目录](../../reviews/code/2026/20260910-sdk-build-model-inputs/DTMAPI-SDK-scope-evidence-20260910.md)，原字节 SHA256 `60dea20b35cb634f195b55d328a5f28a49ca8253cfc9b1d9df08310e75e9ff2e`。
- 用户附件 `c9ce5e93-e29b-42b3-8e0d-c9e448c3f247/pasted-text.txt` **复制**为 [Pro 原文](../../reviews/code/2026/20260910-sdk-build-model-inputs/web-pro-recommendation.md)，原字节 SHA256 `025f053f58ac8b18d557fb6ebde2d41909ebafbe816e223ffcfe9ea2370102da`。保留其 sandbox 历史链接，实际伴随文件由 Review 指向，不将原文当当前规范。
- [本地专项研究](E:/Python_project/SMAPIlearning/SMAPI_technical_study/reviews/2026-09-10-sdk-build-model/README.md)保持原目录；没有迁移 SMAPI 源码、第三方实现或官方二进制。
- [设计与源码核对](../../reviews/code/2026/20260910-0004-sdk-msbuild-architecture.md)和 [完整任务](../../planning/platform-next/execution-sdk-msbuild.md)。本轮无新 smoke。

## Rollback Notes

可单独撤回本次计划修订并恢复旧路由，不触及生产代码或作者工程。若撤回资料迁移，只在下载原路径未被新文件占用时将同 hash 文件移回；保留附件原件及原有工作树改动，不批量回退其他流程/多平台建设。

## Follow-Up

从 PN-041.a 开始，在同一实施任务连续完成 b–f、必要返修及 R-AuthorBuild，最终统一验收。较低 TFM 等运行资产能力回到 PN-040.b，M4 数据/内容路线继续；本次未启动或恢复其他任务。
