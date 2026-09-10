# Astra workflow redundancy review

- Status: `recorded`
- Source: 用户要求继续依据官方 Astra 指南，对工作空间做去冗余审查和流程设计；延续上一轮降低非核心消耗、保留必要验收的目标。
- Implementation: [0008 Update](../../../updates/2026/20260907-0008-astra-workflow-routing.md)

## Official basis

已通过 OpenAI Docs 检索并读取 GPT-6 Astra 的 [behavior](https://developers.openai.com/api/docs/guides/latest-model#gpt-6-astra-behavior)、[instruction following](https://developers.openai.com/api/docs/guides/latest-model#instruction-following)、[initiative](https://developers.openai.com/api/docs/guides/latest-model#initiative-and-follow-through) 和 [testing](https://developers.openai.com/api/docs/guides/latest-model#testing-and-verification) 原文。

官方指出该模型更受文件指令影响，应审查冲突和隐性约束；已有授权的工作继续推进；小任务的测试应匹配风险，通过后仅因新变化、失败或未决问题追加；不为低风险可逆改动添加复述实现的测试。下列具体取舍是本仓库的工程判断，非 OpenAI 针对 DTMAPI 的结论。

## Findings and decisions

| Finding | Evidence | Decision |
| --- | --- | --- |
| 已压缩入口下仍有旧指令冲突 | platform-next/README 的“本轮不开始 PN-004”属于上次规划交付，但仍在 active 接手入口；README/提示词重复下一任务 | 执行状态只查 status；接手页读当前任务节和相关决策，不读全任务册，不把上轮叙事当新禁令 |
| 存档隔离规则存在第二份规范 | platform-next/acceptance 仍要求保存/删档一律隔离，与本轮 PROJECT 专用槽例外冲突 | 只引用 PROJECT；保留 E05 中断实验本身需要隔离的实质要求 |
| “局部验证”缺可执行说明，默认命令重复工作 | README 依次列 build、test；build 默认跑五个测试项目，test 又调用 build -SkipTests | 命令说明区分选定项目/已有 focus、全仓构建和完整 Release；完整入口单独调用 |
| focus 拼错会扩大测试；遗留 filter 可缩小完整验收 | Unit/Doctor 未匹配分支落入默认全测；SDK 未知环境 focus 跳过一个分支后继续其余测试；test.ps1 不拒绝环境 filter | 未知 focus 立即失败；完整入口在构建和环境写入前拒绝残留 filter；不删除任何正式验收项 |
| 通过后的证据失效边界仍不够明确 | 当前工作流说“按风险”，但未说明仅改记录、输入变化、失败范围与重跑的关系 | 在原验证工作流补明确失效条件；用既有 Update 列结果/复用理由，不新增缓存或 receipt 系统 |

## Scope and retention

审查覆盖根入口、活跃工作流、平台接手/验收、两个项目技能的触发规则，以及 build/test/focus 的实际调用关系。脚本目录按需查命令，不以搜索命中等同于问题。历史 installer summary 已声明非当前权威，Runtime audit 技能已排除普通 Mod 同步，guidang 只在显式归档时触发，均保留。共享 runtime 锁、原生提交/回档、故障窗口、SDK/ABI/发布物证仍是不同风险的必要门槛。

本轮只修过程指令与测试调度输入，不开始平台产品任务、不更改模型配置、不启动游戏、不改发布权限。全量测试内部的确定性双构建、跨宿主/字节验证不能仅因“运行两次”就认定冗余。

## Acceptance

- 命令调用链和四类代表任务可静态核对：文档修正、产品逻辑修正、仅改验收记录、Runtime 包边界变更。
- 无效 focus 非零退出且不跑默认测试；已存在的 focus 保持成功；完整入口带 filter 在构建前拒绝。
- 文档治理与本轮本地链接通过；不为工作流文字运行游戏或完整 Release。
- 字符数和移除的调用前置可测；没有模型 A/B 或真实下一次产品任务时，不声称已测得 token/耗时节省。
