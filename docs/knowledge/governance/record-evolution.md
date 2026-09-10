# 历史任务如何影响了日常流程

本页是 2026-09-08 建设期间的历史审阅结论，按需阅读。当前必读、记录责任和测试选择分别由 [AGENTS](../../../AGENTS.md)、[文档治理](../../workflows/document-governance.md)和[产品测试工作流](../../workflows/product-change-validation.md)维护。

## 早期 Goal 是一次任务的完整委托

2026 年 6 月的 Goal 往往同时包含产品需求、固定 Runtime 版本、十余份必读文件、第三存档实测和 API/Hook/Debug/Update 全量更新。这些要求属于当时的一次交付，不能从原件的 `active` 或 `must` 字样推出今天仍须执行。

两个明确案例解释了保留历史与撤销任务权威的区别：

- [0.3.1 回归任务](../../archive/goals/2026/20260606-0001-031-regression-new-content.md)保留了八类产品的反馈，但后来确认自动升到 0.3.1 是错误。其版本号不能成为新版本规则。
- [0.4.1 手测任务](../../archive/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.md)经用户确认作为手测归档，不再阻塞 [0.4.2 Camera API 工作](../../archive/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md)。把未实施的旧 Goal 当成全局 blocker，会重复制造已经解除的依赖。

旧 [0.4.0 实体 API 任务](../../archive/goals/2026/20260606-0002-040-stable-custom-entity-apis.md)允许“已注册、原生创建仍受阻”的显式状态，不能据此宣称动物、怪物、弹幕或无人机已在游戏中完整可用。当前能力回到 API matrix、对应 Hook 与现行 Issue 判断。

## 保留有用的证据区别，不继承整套门槛

早期 UI 任务要求验证“首次可见帧”，原因是最终截图不能证明动物行不闪烁、机器切图时不缩小。这一证据区别仍有用；第三槽、指定分支、旧版本和每个产品都跑一遍的安排属于历史任务。

[0.4.2 Camera 记录](../../archive/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md)保留了 Vector2/Vector3 签名误配和异步截图尚未落盘导致的失败。后续处理应先区分产品行为、工具前提和证据采集；修正某个采集问题并不会自动使未变行为的全部证据失效。该记录中的补偿模式和 PASS 仅适用于当时版本。

## 旧发布材料不拥有今天的删除与发布权限

[0.5.0-alpha 检查表](../../archive/releases/0.5.0-alpha-developer-preview-checklist.md)和[卫生报告](../../archive/releases/0.5.0-alpha-release-hygiene-report.md)保存了当时的 marker-based 卸载、HookProbe 和首批测试安排。它们的后续 supersession 指向 2026-07-13 的发布重构；当前 Catalog、发布停止状态、安装器所有权和授权仍由各自现行文件维护。

[旧硬化分支路线](../../archive/architecture/20260608-runtime-hardening-branch-roadmap.md)强调风险分层与如实标注 API 状态，这些判断可复用；其“一切原生实现放 GameBridge”及固定分支/版本安排不能覆盖现在的 PROJECT 原生责任规则。

本页不关闭任何产品问题。未完成事项仍从对应现行 Issue、API/Hook owner 和活跃 Update 进入；原始需求、当时失败与当时结论完整留在归档正文。

## 历史中已有按改动范围选测试的实例

[Shell Crab JSON 试配](../../archive/goals/2026/20260701-0002-shell-crab-animalvoice-json.md)只要求解析 JSON 和检查 WAV，同一已建立音频路径不因增加配置而强制再做完整游戏测试；用户随后补充了实际声音和原生动物未污染的手测。[Hatch 基础设施](../../archive/goals/2026/20260701-0001-hatch-animal-voice-audio-replacement.md)改变匹配上下文和替换行为，才有相应原生事件、播放与 fail-open 验证。二者说明测试取决于改变的责任边界。

[多摩托任务](../../archive/goals/2026/20260614-0001-multi-custom-motor-api.md)明确使用第八、九槽，是为了分别证明真实跨图和原生摩托尚未解锁的路径。此类 fixture 前提属于选定场景；它不应影响不测载具的普通 runner 会话。

[作物手测交接](../../archive/goals/2026/20260612-0002-crops-harvesting-real-field-manual-qa.md)区分临时成熟盆栽 smoke 与真实田地、满包、重复收获等场景。自动化证据可以复用，但不能扩大它实际证明的目标类型；未完成的产品准入仍由现行 owner 持有。

## 讨论稿与已经接受的工作要求要分开

原根目录[《构想》讨论](../../archive/planning/2026/20260606-workspace-ideas.md)保存了合作联机、端到端测试、压力测试和项目履历的早期交流。它提出过“每轮第三槽 smoke、每 3–5 个 Goal 审查”的节奏，但用户当时要求的是讨论；这些建议不是永久执行规则。联机部分的外部项目能力判断本次未重新核实，只保留研究线索，也不启动联机实现。工程改造的当前授权来自 2026-09-08 已批准建设计划。

## 记录系统曾逐次收口，也曾留下重复门槛

[6 月 3 日反馈流程](../../archive/updates/2026/20260603-0001-feedback-review-workflow.md)把截图转写、逐项分析、版本升级与 Goal 配对；[7 月 11 日退役](../../archive/updates/2026/20260711-0006-goal-workflow-retirement.md)已经撤销自动 Goal 双文件。[次日一致性修正](../../archive/updates/2026/20260712-0004-document-governance-index-consistency.md)解决月表与正文不一致，但仍为文档工具运行全 Release。今天保留的是逐项事实、单份实施记录和自动投影；历史验证范围不会成为新文档修正的门槛。

[8 月 9 日权威拆分](../../updates/2026/20260809-0002-document-governance-and-product-authority-split.md)记录过一次 604 秒超时的完整检查，明确只算部分运行。它同时拥有独立的 Runtime 发布物证。归档和去重应保留这两种事实，不能把源码测试未完成解释成发布字节不存在，也不能反向用发布字节证明完整测试通过。

[原始规划退役审查](../../archive/reviews/code/2026/20260831-0001-original-planning-document-retirement-review.md)发现当时治理检查全部通过，却仍把九百余行旧对话放进必读。格式正确不能证明入口职责正确；本次检查阅读量、事实所有权与实际调用链，分别验证它们。[9 月 7 日修复流程调整](../../updates/2026/20260907-0007-product-maintenance-workflow-simplification.md)已记录第十二档的明确授权和原地测试路线，不能再把当时额外隔离解释成缺少用户许可。

## 路由与状态投影的来源

5 月首个 branding Update 已对图片/文档明确不构建、不启动游戏；后来 6 月把 Review 转根 README 总任务册，再生成 Goal、短 prompt、转换 Update，产生多份同义任务和易变完成表。独立 Goal 当时修复了被下一轮覆盖的任务身份，但不构成永久保留全部副本的理由。原 Debug 规划、7 月六份 route summaries 和 phase-summary 中的必读/七项记录模板均属冻结过程，不能重新约束当前代理。

router 只导航匹配任务；链接存在不要求继续读全部子链接。`verified` 是验收层次，不是 API Stable，也不等于 Issue 关闭或 Workshop 发布。当前未知问题只回 Issue/产品/规划 owner，不维护建设日第二张 Current status 表。用户编号、观察与推断以及被推翻结论仍保留在原反馈。

7 月 21 日已决定 G4/G5/G6 共用既有 Catalog/SDK/ABI/Doctor 证据，不新建三套 schema；atomic 约束最终准入，允许有界未准入切片。是否符合零残留需查真实消费者和应移走的已知符号，net-zero 或换目录不能代替证明。来源：[比例治理](../../archive/updates/2026/20260721-0001-assurance-governance-proportionality.md)、[Batch6 前置勘误](../../archive/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md)。

## 月表不是可任意丢弃的生成文本

建设基线五个月表共 573 行，其中 416 Legacy、157 normalized；全文人工摘要、area、Legacy 状态及非表叙述已读，机械标准列逐项与原 Metadata 核对。标准状态来自 Update，人工摘要不应再复制长证据，也不一定能从脚本重建。7 月 `20260705-0012` 原日期 `2026-07-05/06` 保留跨夜含义；旧 partial/source-only/manual-pending 等原词不改写成今天状态。

月表反复出现 build/test/smoke/package，反映当时实施，不为当前任务生成同一套餐。它列出的 AnimalPack、跨平台、Wiki、装备和平台 Mono 未完不能从单个 published/verified 推出闭合，准确后继始终回对应 owner。阅读覆盖与位置只由[迁移清单](../../archive/migrations/20260908-workspace.json)拥有，不复制一个长期审阅状态体系。

## 存储体积与上下文成本分别处理

8 月 31 日快照为主树 145.248 GiB、99.76% ignored，本地 evidence 以外 docs 仅 46.6 MiB。该历史数值说明文本整理主要改善导航/事实重复；磁盘收益来自可再生产物和重复证据，不是删历史文字。raw 不可重取、唯一私有交付、活动 lease、未知 reparse/dirty tree 不能按年龄或 ignored 直接清；移冷存储也不等于释放磁盘。来源：[存储与退役审查](../../archive/reviews/code/2026/20260831-0002-workspace-storage-and-retirement-audit.md)。

自动拼接 AGENTS 容量与主动沿链接读文档是两种负担；没有证据就不宣称指令被截断。Skills 按匹配任务加载，重点在准确触发和复用已有脚本，不以机械缩正文或复制深层 AGENTS 取代职责设计。来源：[上下文与 Skill 审查](../../archive/reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md)。
