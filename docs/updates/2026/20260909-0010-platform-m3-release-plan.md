# 20260909-0010: M2 与反射验收、M3 执行包和发布路线

## Metadata

- Update ID: `20260909-0010`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求独立验收、继续细化执行；内部 0.6.X、M3 后最早公开 0.7.0、后续 0.7.X、0.8.0 起清退旧接口；手柄绑定和菜单导航先实验交付。

## Summary

接收已有明确限制的 M2 与反射候选，补齐开放生态的格式、实现步骤和验收；规划版本与已发布事实分开。保持同一 Astra / high 任务在当前工作区连续执行，细化任务完成后统一交回验收。

## Changed Files

平台架构、任务、路线、状态、能力与验收入口；新增 M3 包契约细则和近期执行包。当前生产代码、版本投影和发行产物不在本次规划中改写；版本整理由 PN-036 实施。

## Validation

已独立构建 Core 测试与 SDK 测试项目；platform-runtime-core、platform-reflection-public、platform-data-core、platform-settings-core、AuthorSdk pack-build 通过。相同 Core 输入的后续 focus 使用 NoBuild。游戏证据复用实施 Updates，不声明本轮重新运行 Mono。普通 SDK 0.2.0 ZIP 摘要与其原记录一致。

文档治理/本地链接与月度同步通过；44 行队列的直接依赖核对无重复、缺失或循环。原依赖列的 PN-020/R2 回环已修正。初次显式 LF 差异检查指出两份整页改写末尾混入 CRLF，已仅规范化本次文件换行；不改变其他工作区文件。

## Evidence

[独立验收](../../reviews/code/2026/20260909-0004-platform-m2-m3-acceptance.md)。本轮 focus 日志为 temp/platform-parent-acceptance-*-20260909.log；历史候选、实机与恢复沿对应 Update/证据目录，不再复制收据。

## Rollback Notes

仅撤本次规划 delta，保留上一任务的实现、证据、Wiki 和其他工作区改动。未操作游戏、MODS、玩家存档或上传。

## Follow-Up

PN-036 版本整理先行，再按 status 的依赖继续 M3、发行准备与手柄实验实现。公开发布仍需具体产物和用户授权；无手柄不阻塞独立工作，也不记实体手柄 PASS。
