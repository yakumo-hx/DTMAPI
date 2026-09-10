# 新任务工作空间必读上下文与 Skill 路由审计

## 记录信息

- 日期：`2026-08-31`
- 状态：`recorded`
- 性质：audit-only Review；不承接实现生命周期
- Source：用户要求审阅新任务在 DTMAPI 工作空间开始工作时的必读文档与 Skill，并识别优化空间
- 基线：分支 `codex/major-update-batch0-20260713`，`HEAD=98d75c6c51300768e3d7a8b209c858c9ad110493`
- 实现边界：本轮不修改 `AGENTS.md`、必读文档、Skill 或脚本；若采纳整改，另建对应 Update

审计开始时，根 `AGENTS.md` 和 `docs/workflows/document-governance.md` 已有用户未提交修改，
`.agents/skills/` 整体尚未跟踪；这些状态不是本轮创建或接管的。全局
`D:\OpenAI\CodexHome\AGENTS.md` 当前为空，因此本轮没有发现一份会与仓库规则竞争的有效全局
`AGENTS.md` 内容。

## 1. 执行结论

当前新任务入口的主要问题不是规则缺失，也不是触及 Codex 的 `AGENTS.md` 默认容量上限，而是三类
上下文治理问题：

1. 一份已经完成历史使命的 Debug 规划/对话稿仍被列为所有设计和代码任务的必读材料；
2. 根 `AGENTS.md` 重复展开了 `PROJECT.md`、文档治理、Runtime lock 和 Debug 系统已经拥有的规则；
3. Workshop 审计 Skill 自带的 Windows BAT 测试器已经落后于当前多平台发布边界，并与当前测试产物
   保留规则冲突。

根 `AGENTS.md` 为 16,720 个字符；其当前五份通用必读链接合计 30,301 个字符。一个遵守规则的新
设计/代码任务在进入具体领域前，需要处理约 47,021 个字符的原始文本。这里的 32 KiB 官方默认
上限只约束自动拼接的项目指令链，不约束代理随后主动读取的链接文档；因此本仓库没有已证明的
`AGENTS.md` 截断问题，但存在明显的重复阅读与注意力稀释问题。

如果通用入口先只保留 `PROJECT.md` 与 `docs/onboarding/current-state.md`，在根规则尚未精简的前提下，
初始原始文本就会从约 47,021 个字符下降到约 29,807 个字符，减少 17,214 个字符（约 36.6%）。
这只是上下文规模估算，不等同于模型质量提升比例。

两个工作空间 Skill 的启动描述合计只有 687 个字符。对普通任务而言，Skill 正文按需加载，因而
“把正文再缩短”不是首要收益点；真正需要优化的是描述的触发边界、正文与权威流程的重复，以及
脚本是否仍表达当前发布模型。

## 2. 审计基准

本轮使用以下官方规则作为外部基准：

- [GPT-5.6 Model Guidance](https://developers.openai.com/api/docs/guides/latest-model)：GPT-5.6 更适合
  精简、去重复、每条要求只表达一次且只暴露相关工具的提示结构；页面中的内部编码代理收益数字
  只能视为方向性样本，不能外推成本仓库的预期收益。
- [Codex AGENTS.md guide](https://learn.chatgpt.com/docs/agent-configuration/agents-md)：Codex 在一次运行
  开始时按根目录到当前目录组合指令链；根规则宜保持精炼，目录级规则只有在对应工作目录链上才会
  自动生效。
- [Build skills](https://learn.chatgpt.com/docs/build-skills)：Skill 使用渐进披露；启动时主要暴露名称、
  描述和路径，隐式匹配依赖简洁、有边界且把关键触发词前置的描述；仓库级标准位置是
  `.agents/skills`。

## 3. 当前入口与规模

### 3.1 通用必读

| 文档 | 字符数 | 当前角色 | 审计判断 |
| --- | ---: | --- | --- |
| `AGENTS.md` | 16,720 | 自动生效的根规则 | 必要，但重复展开过多 |
| `PROJECT.md` | 8,942 | 架构、身份、所有权和保存语义权威 | 保留通用必读 |
| `docs/onboarding/current-state.md` | 4,145 | 当前状态与权威入口路由 | 保留通用必读 |
| `docs/planning/Debug.md` | 11,053 | 2026-06-13 的 Debug 规划/对话稿 | 移出通用必读 |
| `references/README.md` | 3,503 | 参考材料边界和索引 | 改为参考研究任务条件读取 |
| `docs/debug/INDEX.md` | 2,658 | 当前 Debug 导航 | 改为 Debug/Runtime 类任务条件读取 |

### 3.2 条件文档热点

- `docs/api/public-api-matrix.md` 约 85,970 个字符，其中约 39.5% 是日期历史。API 任务不应默认顺序
  阅读整份历史；应先读状态词汇和目标 domain/symbol 行，再按需追溯历史。
- `docs/architecture/runtime-workshop-installer-boundary.md` 与
  `docs/workflows/workshop-package-subscription-test-matrix.md` 合计约 41,244 个字符。其规模与高风险
  职责相称，但任务入口应先区分 Windows 基线、多平台、订阅一致性和玩家失败调查，再读取相应段落
  与脚本。
- 冻结的 Batch 6 附录虽然很大，但根规则已经将它限制在历史 Batch 6 / `0.5.5` 证据相关任务；
  这种条件路由是正确范例。

## 4. Findings

### F1 — P1：历史 Debug 规划稿仍是所有设计/代码任务的强制输入

`docs/planning/Debug.md` 保留十个 ChatGPT “Past chat” 链接、过时的拟议文件名、已经实施过的下一步
建议，以及两个遗留 `contentReference` 标记。它没有当前 lifecycle/status 前页，也不是 Debug 事实的
现任 owner。当前 `docs/debug/INDEX.md` 和各 issue/protocol 才是正确路由。

这与当前文档治理规则中“默认设计/代码上下文不携带历史或冻结规划全文”的原则冲突。它应从通用
必读中移除，并改成带有 `superseded` 状态和当前 owner 链接的历史交接页，或把正文归档后只保留
紧凑入口。

### F2 — P1：Workshop 审计 Skill 维护了落后的平行测试系统

`.agents/skills/dtmapi-workshop-release-audit/SKILL.md` 本身能通过 Skill 结构验证，但其 bundled
`scripts/test_subscription_package.ps1` 只接受含 `1_install_dtmapi.bat` 的 Windows 包，并固定执行 BAT
安装、检查、日志收集和卸载。它没有表达当前已存在的 `.sh`、Linux/Steam Deck、macOS CrossOver
或多平台包路线。

仓库已经有受治理的当前入口：

- `tools/scripts/test-runtime-workshop-installer-061.ps1`；
- `tools/scripts/test-dtmapi-multiplatform-package.ps1`；
- `tools/scripts/test-multiplatform-runtime-installer.ps1`。

Skill 脚本的默认 `OutputRoot` 使用系统临时目录并创建顶层
`DTMAPI Workshop Audit <timestamp>`。这与当前规则“Unit/QA fixture 必须处于受管测试 session，
不得新建持久顶层 `%TEMP%\DTMAPI-*` 根目录”不一致。成功路径还会保留结果根，阻断路径保留更多
内容，因此不能只把它解释为瞬时临时文件。

整改时应把 Skill 改为薄路由：先识别 Windows、多平台、订阅一致性或玩家失败调查模式，再调用
当前权威脚本，并遵循受管测试 session 与 evidence retention。不要再维护第二套相邻 assurance
机制。

### F3 — P2：根 `AGENTS.md` 重复了多个 canonical owner

根规则中篇幅最大的几块是 Development Direction、Save Commit 与 Assurance Proportionality，合计
约 8.3k 字符。它们大面积复述：

- `PROJECT.md` 的 Mod 身份、物理所有权和保存提交语义；
- `docs/workflows/document-governance.md` 的 Review/Update/验证职责；
- Runtime lock、Debug、Hook 与测试协议中的操作细节。

这些重复提高了两份规则未来发生漂移的概率。根规则应保留不可妥协的安全边界和“任务类型 -> 权威
文档”路由；详细矩阵、例外和生命周期语义应只在 canonical owner 中表达一次。

不建议为了缩短根文件而机械创建大量嵌套 `AGENTS.md`。官方发现机制以任务当前工作目录链为准，
如果新任务通常从仓库根启动，深层文件不会仅因代理后来打开该目录而自动成为启动指令。条件读取
明确的 workflow/README 比依赖隐含 CWD 更可靠。

### F4 — P2：`PROJECT.md` 顶部混入了快速变化的发布状态

`PROJECT.md` 首段写入两个物理 Workshop item ID、首次上传/订阅/字节一致性结果和 Steam Deck 验收
待办。稳定事实“多个物理分发仍属于一个 Runtime 身份”属于 PROJECT；具体 item、当前订阅集合、
最新验证状态则应由 Product Catalog、current subscription manifest 和 latest release Update 拥有。

继续把这些值放在通用必读里，会让每个任务读取不相关的易变发布快照，也会形成第二个发布事实源。

### F5 — P2：两个工作空间 Skill 的可移植性和持久化意图不一致

- Workshop audit 位于官方仓库级目录 `.agents/skills`，但目录当前尚未被 Git 跟踪；
- `guidang` 位于 `.codex/skills/guidang`，且 `/.codex/skills/` 被本地 `.git/info/exclude` 排除；
- `guidang` 还把输出目录硬编码为
  `E:\Python_project\DTMAPI\.codex\conversation-history`。

因此当前 checkout 能看到两个 Skill，不代表从已提交 HEAD 创建的新 worktree、clone 或其他机器也能
得到相同能力。项目需要明确选择：如果只是此机器的个人 Skill，就应承认其本地性；如果它们属于
仓库能力，就应迁移到并跟踪 `.agents/skills`，且所有路径改成以仓库根为基准。不要同时保留两个
副本，否则会再次造成重复发现。

### F6 — P3：Skill 描述可以更窄，但不是当前上下文成本主因

Workshop audit 描述约 400 个字符，罗列了大量平台、命令和失败形态；`guidang` 描述约 287 个字符。
两者都能表达用途，但前者可以把核心意图前置为“发布前/后、订阅包或玩家安装失败的 Runtime
Workshop 审计”，把具体检查项留在正文；后者应避免把含义宽泛的“close/关闭”单独当成归档意图。

保留隐式调用是合理的：用户希望自然语言提到审计或归档时能命中。优化目标应是减少误触发，而
不是关闭显示或调用。

## 5. 建议的新任务阅读模型

### 5.1 始终读取

1. `PROJECT.md`：只保留稳定架构、身份、所有权和保存语义；
2. `docs/onboarding/current-state.md`：只提供当前状态和权威入口路由。

### 5.2 按任务条件读取

| 任务信号 | 读取入口 |
| --- | --- |
| reverse、官方游戏资料、第三方 Mod、SMAPI 研究 | `references/README.md` |
| 重复缺陷、Runtime、Hook、输入、配置、Workshop 或 Mod loading | `docs/debug/INDEX.md` + 目标 issue/protocol |
| 手工 QA 反馈或 root-cause Review | `codex-feedback-to-goal.md` + `docs/reviews/README.md` |
| 非平凡实现、文档生命周期或状态迁移 | `document-governance.md` |
| Runtime Workshop/安装失败 | installer boundary + 与目标平台/包类型匹配的 matrix lane |
| API rebuild/native owner | API workflow + 目标 domain/symbol + 最新相关 Review；历史按需追溯 |
| Hook 实现 | hook-map 入口 + 目标 decompiled build + relevant smoke/debug record |

### 5.3 Skill 层次

1. 描述只负责匹配意图和排除近邻任务；
2. `SKILL.md` 正文负责选择当前 canonical workflow 和脚本，不复制完整政策；
3. 脚本负责确定性执行，但只有一个受治理的实现入口；
4. 证据、临时目录和清理行为继续服从仓库通用 retention 规则。

## 6. 建议实施顺序与验收门

### 第一批：低风险上下文减负

1. 从通用 Required Context 移除 `docs/planning/Debug.md`；
2. 将 `references/README.md`、`docs/debug/INDEX.md` 改为条件路由；
3. 给历史 Debug 规划稿加 lifecycle/handoff，清理失效引用标记；
4. 把 `PROJECT.md` 的易变 Workshop 状态改成权威路由。

验收：硬安全边界仍可从新任务入口到达；根规则加两份核心文档的原始字符量显著下降；文档治理
检查保持通过。

### 第二批：根规则单一事实源

按段落建立“保留在根规则 / 改为 canonical link / 删除重复”清单，优先收束 Development Direction、
Save Commit、Assurance、Runtime lock 与 Debug 重复。不能仅按字数删减；每个删掉的强约束必须有
唯一、可达的 owner。

验收：针对保存、运行时锁、发布、Debug、API 和文档治理各做一次冷启动路由检查，确认新任务能在
不依赖旧上下文的情况下找到正确约束。

### 第三批：Skill 与脚本收口

1. 缩窄两个描述的触发边界，但保持隐式调用；
2. 把 Workshop audit 改为当前 Windows/多平台/订阅调查入口的薄路由；
3. 退休或改写平行 BAT-only 测试器，使用受管 session；
4. 决定两个 Skill 是 local-only 还是 repo-shared，并据此统一位置、跟踪状态和相对路径。

验收：Skill validator 通过；分别用普通 Mod 上传同步、Runtime Windows 包、多平台包、玩家安装失败和
“归档当前对话”做触发边界样例；新 worktree 的发现行为与所选持久化策略一致。

## 7. 本轮验证

- 完整阅读了根 `AGENTS.md`、五份通用必读文档、文档治理/反馈 Review 入口，以及两个工作空间
  Skill 的 `SKILL.md`；
- 对 Workshop Skill 的 bundled 脚本与当前三条受治理的 installer/multiplatform 脚本做了静态对照；
- 两个 Skill 均通过 `skill-creator` 的 `quick_validate.py`；这只证明结构有效，不证明工作流仍然正确；
- `tools/scripts/check-doc-governance.ps1` 在写入本 Review 前通过 7,429 项检查；
- `tools/scripts/check-test-artifact-governance.ps1` 通过；
- 未启动游戏、未安装/卸载 Runtime、未写 Official MODS 或 Workshop upload 目录；这些运行时动作与
  本次文档/Skill 静态审计无关。

本 Review 是本轮唯一计划内项目变更；没有创建 Update，也没有把建议标记为已实施。

## 8. Resolution

- 用户于 `2026-08-31` 授权先实施第 1、2 批：历史 Debug 规划退出默认上下文，以及根 `AGENTS.md` 单一事实源收口。
- 实施生命周期与验证由 [Update 20260831-0004](../../../updates/2026/20260831-0004-new-task-context-routing-and-agent-rules-slimming.md) 持有；本 Review 不复制完成证据。
- 用户随后授权清理 `PROJECT.md` 的易变 Workshop 快照，并实施第三批中的 Workshop audit Skill/平行脚本收口；该生命周期与验证由 [Update 20260831-0005](../../../updates/2026/20260831-0005-project-workshop-skill-authority-routing.md) 持有。
- `guidang` Skill、两个 Skill 的持久化策略，以及大型 API/installer 文档的进一步渐进披露仍未实施。
