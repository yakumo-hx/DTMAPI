# Development entrypoint redundancy review

- Status: `recorded`
- Source: 用户允许继续拓展优化与精简范围，延续减少非核心消耗、目录错误和重复准备的目标。
- Implementation: [0001 Update](../../../updates/2026/20260908-0001-development-entrypoint-simplification.md)

## Basis and inspected evidence

延用已核实的官方 [GPT-6 Astra testing guidance](https://developers.openai.com/api/docs/guides/latest-model#testing-and-verification)：检查与风险匹配；通过后只因新变化、失败或未决问题追加。本轮的具体实现是仓库工程判断。

| Finding | Evidence | Decision |
| --- | --- | --- |
| 构建范围维护两份，且一份失效 | build.ps1 列 25 个项目并逐一启动 dotnet build；DTMAPI.sln 中 9 个 testmods 项目路径已不存在，缺少 QA Unit 和 ABI harness | 以修正后的 solution 维护普通项目入口；build 脚本调用一次 solution build，保留依赖、五个源测试和 Debug 下额外的 Release 兼容程序集 |
| 查询状态会进入安装流程 | status.ps1 无条件调用 Get-DotNetExe，后者找不到 SDK/.NET 8 时下载并运行安装器 | 状态脚本仅探测已有工具；缺失时报告并继续显示其余状态，安装仍由实际构建命令负责 |
| 根目录导航与实际布局不一致 | README 把不存在的 testmods 和已不含项目的 first-party-mods 作为当前源码入口 | 指向实际 products、author-sdk、tests/mod-fixtures；产品身份继续查 Catalog/PROJECT |
| 普通 Unit 运行容易被带入治理链 | test-artifact-retention 的 Validation 直接列治理、清理 fixture、证据 allowlist 检查，未写变更触发条件 | 自动 session 清理由既有机制执行；仅机制变更、证据引用变化或具体保留异常触发对应检查 |

[Microsoft dotnet build](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build) 支持解决方案及其依赖，并隐式 restore；因此不再分别重复列举和启动所有普通项目。Advanced 产品的 Catalog/Author SDK 构建权威保留，不重新放回 solution。

## Scope, alternatives and acceptance

- 本轮更改开发构建入口、状态查询和相应说明，不改变产品逻辑、玩家安装器 helper、存档或发布物。必要的 RID/Release 特例不能在统一入口时丢失。
- 不立即拆分大型 Unit 项目：涉及链接源码、net48 Harmony 独立宿主和多种兼容性假件，不能把执行 focus 当作独立编译。先消除有直接证据的重复入口；不新增第二套测试框架或项目清单。
- 验收：核对原构建项目覆盖、实际 Release solution 构建与输出、相关现有测试、完整入口 filter 防护、状态只读路径、文档和本地链接。测试驱动未变化的完整 package/ABI 发布矩阵不因本轮自动重跑。
- 记录一次旧入口和新入口在现有构建缓存下的时间；明确这是本机样本，不代表冷构建或完整 Mod 修复时长。只在出现实际失败或尚未验证的构建特例时追加对应检查。
