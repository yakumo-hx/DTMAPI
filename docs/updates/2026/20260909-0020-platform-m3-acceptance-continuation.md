# 20260909-0020: M3、输入与发行准备验收及连续收口

## Metadata

- Update ID: `20260909-0020`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户请求验收当前更新并修正/续派；[独立 Review](../../reviews/code/2026/20260909-0009-platform-m3-input-release-acceptance.md)。

## Summary

接收已验证的 M3 核心能力与已打开配置页的键盘操作，纠正原生标题入口完成状态和失效的 0.6.4 验证入口。将标题入口、真实构建输入来源与最终首发验收细化为原任务连续收口，不新增公共契约、不启动 M4 生产实现。

## Changed Files

- 独立 Review、platform-next 的入口/状态/执行/验收/候选说明，D09 原生标题入口决定。
- PN-037.b 与 PN-031.a 原 Update 的当前缺口和后续任务；ControllerInput 只修正证据范围，不宣称新 Hook 已安装。
- `tools/scripts/test-author-sdk-compatibility.ps1` 去掉过时逐版本文件数断言，保留冻结来源与篡改拒绝检查。

## Validation

独立重建/复跑 Core 依赖、SDK 工程/锁定恢复/依赖包/原生契约、RuntimeIntegration 控制器 focus 均通过。当前 0.6.4 frozen 检查先复现旧断言失败；去掉逐版本文件数断言后，分别对 0.6.4 和原 0.5.5 实际执行精确源码重建/重用/破坏拒绝均通过，fixture 自动清理。冻结源码/载荷/摘要未改。

文档治理及本次文件的 diff whitespace 检查通过；Update 0018 与本次记录的月度投影已同步。本次未运行游戏或发行矩阵，不改变之前固定候选的 Mono/人工操作事实。SDK focused 测试报告临时清理 pending，保留托管 session 的既有重试路径，未强删目录；不会将清理待办误记为测试断言失败。

## Evidence

[Review](../../reviews/code/2026/20260909-0009-platform-m3-input-release-acceptance.md)记录三个具体问题、源定位、命令和固定游戏证据范围。SDK 0.6.5 ZIP 重新计算摘要与 PN-022 一致。未生成新 Runtime/SDK 候选。

## Rollback Notes

本次只改测试入口与工程记忆；撤回本次差异不改 frozen SDK/原运行证据/发布身份。保持用户和原任务其余工作区改动。

## Follow-Up

本次修正与接续资料已验收，execution-next 的连续收口由原实施任务 01a0816b-178f-72e2-a411-ceabbe7bd214 在当前工作区、GPT-6 Astra / high 继续。A1 在 PN-037.b 原 Update 完成，A2 与实际发行门在 PN-031.a 完成；发布和实体设备晋级仍有各自外部条件。本 Update 的 verified 仅指独立验收/测试入口修正/接续资料，不代表 A1/A2、完整 M3 或发行门已经通过。
