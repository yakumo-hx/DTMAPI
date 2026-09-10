# 20260910-0001: 执行流程排错与停止条件修正

## Metadata

- Update ID: `20260910-0001`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求修正 M1–M3 执行审查确认的流程问题；沿用本任务已完成的对话与命令审查，不另建重复 Review。

## Summary

修正全量测试排错入口及过晚的前提检查、标题界面手动测试被迫读档、负例测试把调用错误当作成功的问题。收紧现有流程的完成条件和记录分工；AGENTS、PROJECT、current-state 未改，不增加日常必读文件。

## Changed Files

- [test.ps1](../../../tools/scripts/test.ps1)、test-common 和清理入口：9 个具名阶段、单段/尾段诊断、完整运行前的冲突进程与证据索引检查；较长的安装事务矩阵移到产品/QA/ABI 之后。保留旧 PostRuntimeInstaller 入口和完整覆盖，诊断不冒充完整 PASS。新增路由回归并接入现有公开源码 CI。
- [游戏 runner](../../../tools/scripts/game-smoke/README.md)：`-WaitForManualExit -SaveSlot 0` 支持不挂 QA 的标题界面观察；原读档观察保持原前提。正常退出、超时失败和 NoNativeSave 边界继续沿用。
- Author SDK 测试：区分功能拒绝、明确的参数拒绝和未开放命令，拒绝把调用错误/内部错误当作正确负例。此改动实际揭示旧的未获准 Advanced 身份创建返回 SDK999；[TemplateCreator](../../../src/DTMAPI.AuthorSdk/TemplateCreator.cs) 将该输入拒绝正确分类为 SDK001，未放宽身份准入。
- [产品验证](../../workflows/product-change-validation.md)、[记录治理](../../workflows/document-governance.md)及脚本说明：先局部修复、最终候选完整验收一次；明确原生入口到退出的观察范围；模型切换/压缩不使候选失效；Update、候选页、路线状态各只维护自己的事实。
- 证据索引按现有生成器同步已有独立兼容审查的新入口；保留的证据集合没有变化。

## Validation

- PASS：`test-release-routing.ps1`，28 项，包括实际 Windows PowerShell 子进程选择、错误前提不构建、失败停止、旧入口、大小写和部分成功标识。另实际执行 `test.ps1 -Stage Governance` 成功，仅输出诊断成功。
- PASS：Windows PowerShell 5.1 下的 runner process-boundaries（18 项）、modules（71 项）、save-modes；包含新标题路由、原读档路由及真实工作进程的正常退出/超时边界。
- PASS：Author SDK 测试项目 Release 构建及默认完整套件，`advancedReferenceFixture=executed`。pack-build 新的错误调用回归和现有拒绝用例均被覆盖；全套验证期间暴露的参数错误分类已逐项修正。测试会话报告 cleanup-pending 由既有清理机制处理，不为此重跑测试。
- PASS：原完整入口 focus 拒绝、受影响脚本的 Windows PowerShell 5.1 语法、清理 fixture、文档治理和差异空白检查。
- 本次未启动游戏，也未重跑平台完整 Release；runner 的流程回归不声明新的游戏 UI 验收。SDK 源码错误分类变化不改变既有 Runtime/游戏证据。

## Evidence

本次结果集中在此，不另建 Review 或 smoke 行。机器结果：[路由](../../../tmp/test-runs/release-routing-3535284609f44e04abc44089cc129a26/result.json)、[进程边界](../../../tmp/test-runs/game-smoke-process-boundaries-837fd17ce6374d58b9ac0e4a1cd30c67/result.json)、[runner 模块](../../../tmp/test-runs/game-smoke-modules-5aadc2d09f3a404f925ee5c45deac653/result.json)、[SDK 默认套件](../../../tmp/workflow-repairs-20260910/sdk-default.log)。这些是可清理的源码测试产物，长期结论保留在本节；既有平台 Release/游戏证据仍由原文件负责。

## Rollback Notes

按本 Update 的改动逐文件回退。保留工作树中已有的 Wiki、发布候选、状态及其他 Update 改动；不恢复整棵工作树。

## Follow-Up

本任务范围完成。现有发布候选、状态页和上一批 Update 仍有其他在建改动，本次仅修正其维护规则，不重写这些记录。后续 SDK 交付包若包含此输入错误分类修正，应使用新源码打包并验证该 SDK；当前既有封包未被本任务替换。
