# Product maintenance workflow review

- Status: `recorded`
- Source: 用户要求按 1) 压缩强制阅读，2) 明确产品修复测试前提、触发和结束，3) 减少 Catalog/记录重复投影实施；保留快速文档检查。
- Implementation: [20260907-0007](../../../updates/2026/20260907-0007-product-maintenance-workflow-simplification.md)

## 1. Required context

根 AGENTS、PROJECT、current-state 当前合计 24,319 字符；多份工作流重复保存/证据/记录要求，API 流程还强制列出历史审查。缩减实际读入量，保留 PROJECT 的规范权威，专项内容按行为变化读取；不只把正文转移到另一份全量必读。

## 2. Product validation

用户确认 MoreEquipmentSlots 小修复接近两小时，至少 45 分钟花在失败处理。新截图显示原请求于 8 月 26 日 00:59 发出，明确第十二档可保存、删除、新建且无需恢复，界面显示用时 1h 47m 30s。该历史请求是分析证据，不是本轮启动游戏或改存档的指令。

粘贴对话可确认：错误 profile、侧车路径误判、等待跨日却未保存、受控保存前漏启用产品，以及 Catalog/格式/路径分隔符返工。摘录不足以精确分摊各环节分钟数。新档清理、首存与冷载仍直接验证缺陷；新增普通原地测试路径和明确可处置槽位例外，保留故障注入/多档迁移隔离。手工直测是有效证据入口，不能为适配专项 QA runner 强制重建场景。

## 3. Catalog and records

当前文档治理三次计时 2.357/1.804/1.388 秒；7,913 是断言数，非文档数，不优化其耗时。修复提交包含日期-only 准入表更新、候选版本/日期 canary 和把可变源码版本纳入 identity freeze 的维护链。候选版本保留 Catalog 字段与源码一致性，身份与已发布物证继续严格保护；月度状态由 Update 生成，人工摘要保留。

## Acceptance and limits

- 通用必读显著缩短；无历史全读、无因 Mod 关键词自动触发完整 Release/长测。
- 原地 NoNativeSave 与专用可处置槽明确；隔离只用于具体需要的场景。保存技术提交证据与玩家入口证据分别命名。
- 候选元数据变化不再要求重写日期/版本常量或准入日期；身份漂移、源码版本不一致仍被拒绝。
- 台账同步保留其他行与人工摘要，拒绝重复编号和无效元数据。
- 本轮只改工作流/维护工具，不改变游戏程序集、现有专项 runner 的保存隔离断言、发布物证或上传权限。

## Official guidance

- [GPT-5.6 leaner prompts](https://developers.openai.com/api/docs/guides/latest-model?model=gpt-5.6#favor-leaner-prompts)：去重，每次删一组并比较代表任务。
- [GPT-6 Astra testing](https://developers.openai.com/api/docs/guides/latest-model#testing-and-verification)：相关检查通过后，仅因新改动、失败或未决风险扩展测试。
- [Codex guidance](https://learn.chatgpt.com/guides/best-practices#make-guidance-reusable-with-agentsmd)：精简通用说明，专项按需读取。
