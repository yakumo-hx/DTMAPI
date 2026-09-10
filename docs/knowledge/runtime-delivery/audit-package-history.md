# 审查包、构建入口与重复记录的历史

本文提炼早期交付记录中的失误与边界，不拥有当前打包步骤或发布身份。当前入口查 [PROJECT](../../../PROJECT.md) 与 [current-state](../../onboarding/current-state.md)；原件和逐篇阅读记录在 [Runtime delivery 阅读清单](../../archive/migrations/20260908-workspace.json)。

## 早期路线不是每次变更的先决条件

[June 8 roadmap](../../archive/architecture/20260608-runtime-hardening-branch-roadmap.md) 描述当时 Runtime hardening、API matrix、GameBridge split、Camera 和 content 的依次推进；[对应 Update](../../archive/updates/2026/20260608-0002-runtime-hardening-branch-roadmap.md) 本身只改文档，没有构建或游戏。旧 master/逐目标分支、全部 native 进 GameBridge、五层测试都属于当时路线，后续架构和当前验证 workflow 拥有执行规则。

CameraView 在 [June 8 lease rebuild](../../archive/updates/2026/20260608-0020-camera-view-lease-rebuild.md) 已明确普通 playable zoom 只改 orthographic size、panorama 另议；其当时的公开 lease/兼容状态及手工 QA pending 后来继续变化，不能凭旧 PASS 删除冻结 ABI，或凭旧 pending 重开当前产品验收。具体 native 基线和现行权限查 [Camera owner](../../hook-map/focused/Camera.md)。

## 审查包曾形成大量相同事实投影

June 8–9 的连续 package Update 分别重建 source folder、audit folder、zip 和嵌套 report，重复列出许可、脚本、Hook map、构建排除项与同一套运行证据。它们保留独立交付时间和候选内容的历史价值，却不形成今天必须逐层再抄一遍的记录规则。

- [初始包](../../archive/updates/2026/20260608-0021-open-source-audit-package.md) 的根 solution 原本来自 tracked build 项目集合；它后来失效说明两份清单需要同一来源，不能只验证初建时一致。
- [动态证据 refresh](../../archive/updates/2026/20260608-0027-open-source-audit-package-refresh.md) 使用既有运行生成审查包；PATH dotnet 无 SDK 的失败不能靠随意换 host 规避。当前 resolver 由仓库脚本拥有。
- [source hygiene](../../archive/updates/2026/20260608-0029-open-source-audit-package-hygiene-refresh.md) 把公开 source 与 evidence 分开处理：source 的机器路径和本地依赖需要去掉，历史证据里的原始环境路径仍有追溯价值。public placeholder 可构建不等于包含完整游戏数据。
- [June 9 refresh](../../archive/updates/2026/20260609-0003-open-source-audit-package-refresh.md) 明确 Core-only 输入修改不需要为换包再生成游戏报告；旧 Camera 报告按其证据范围保留。缺失根 Directory.Build.props 不能用较新本机编译器掩盖。构建留下的 SDK 锁、bin/obj 是交付暂存的工程问题，不是游戏验证失败。
- [feature-host refresh](../../archive/updates/2026/20260609-0007-audit-package-feature-host-refresh.md)、[hardening refresh](../../archive/updates/2026/20260609-0009-audit-package-feature-host-hardening-refresh.md) 与 [smoke case refresh](../../archive/updates/2026/20260609-0012-audit-package-camera-smoke-case-refresh.md) 曾从上一个外部审查包保留仓库已经没有的 focused maps/reverse snippets。清理重复副本前应识别这种唯一证据；不能仅按包名或可重新打包的表象整包删除。

多份记录反复建议把 inline shell recipe 变成 tracked script。可复用的原则是维护一个内容选择/排除源，再从同一结果生成交付和最小 receipt；不是为每次 refresh 加新的永久格式或重跑无关游戏。完整游戏 DLL、反编译源码和未获许可第三方材料仍不进入公开 source/release；此限制不要求每次普通产品修复都重做整库材料审计。

## 验证复用与输出形态的界限

[June 9 web companion](../../archive/updates/2026/20260609-0015-web-upload-audit-packages.md) 从已经验过的 full 包删掉大截图/report，明确不再构建；完整视觉证据仍由 full 包持有。[June 10 controlled recipe](../../archive/updates/2026/20260610-0007-final-stability-followup-audit-packages.md) 已实现先前反复提出的 tracked script，并明确最终包装不再游戏 smoke、不在交付目录重编制造 bin/obj。精确 source commit 和 generation time 归包内 receipt，不靠手抄的状态句推定。

[midterm package](../../archive/updates/2026/20260610-0015-final-midterm-hardening-audit-packages.md) 用显式 evidence IDs 覆盖过时默认列表；[Oil refresh](../../archive/updates/2026/20260610-0029-post-review-stabilization-audit-packages.md) 明说该模式未导出新 zip，latest-report 仍指旧运行。报告缺失、状态文本与实际行为结果应分别判断，不能为了补一个 report 再跑全部场景，也不能冒用旧报告。

[feature-status throttle](../../archive/updates/2026/20260610-0009-feature-status-publish-throttle.md) 早已分开内部记录与外部状态发布：事实可以更新，而格式化/日志不必每帧重做。10 秒 heartbeat 是当时实现，不是当前普适阈值。[optional dependency correction](../../archive/updates/2026/20260610-0017-optional-dependency-warning-semantics.md) 同样选择 loaded 加 warning，而不增加重复的 loaded-with-warnings 状态；但它未改拓扑排序，不能证明后来发现的 optional 环问题已关闭。

## 版本失败不能靠打包文案掩盖

[0.5.0-alpha Update](../../archive/updates/2026/20260610-0026-api-version-050-alpha-helper-notes.md) 的 `GAME-SMOKE/20260610-120641` 没有 result，是 BepInEx 在加载前拒绝带 alpha 的 plugin metadata。改 numeric BinaryVersion 后才加载成功；这不是简单“测试工具没结果”。其 [policy Update](../../archive/updates/2026/20260610-0035-dtmapi-version-compatibility-policy.md) 只记录既有 numeric suffix-stripping，未引入严格 SemVer 或 AllowPrerelease，也未为文档再启动游戏。当前 release、file、assembly compatibility 与真实 product minimum 分别由当前版本/Catalog/SDK authority 拥有，旧数值不再投影进新包。
AutoFishing 的旧 report pending 并非一直开放：[June 10 0052](../../archive/updates/2026/20260610-0052-fishing-followup-web-audit-package.md) 已由 `GAME-SMOKE/20260610-223354` 证明 fresh report/pointer，0058 随后增加独立 report result 字段。应保留 stale pointer 作为失败经验，同时记录其修复，不能只抽出待办句。[June 11 compact evidence](../../archive/updates/2026/20260611-0006-manager-feedback-roadmap-web-audit-package.md) 又明确历史证据继续引用，但不再全量复制到默认 payload；用户只需要 web 包时 full 包也不生成。

[June 12 路径修复](../../archive/updates/2026/20260612-0005-audit-package-quotepath-fix.md) 的两次包装失败分别来自 Git 对非 ASCII 路径的 C-style quoting，以及 Windows PowerShell 所用运行时缺少 `Path.GetRelativePath`。修复的是文件枚举编码和 host-compatible path helper；这些错误不应触发游戏隔离、存档恢复或重新验证 Mod 行为。具体当前实现由 tracked 脚本拥有。
[June 15 handoff audit](../../archive/reviews/code/2026/20260615-0001-codex-handoff-space-maintainability-audit.md) 当时记录 evidence 约 126GB/404k 文件，并怀疑每次 collect 递归复制历史 evidence。数字只属该截面，不能冒充当前磁盘测量；其价值是禁止默认递归搜/搬大证据，按任务和当前 run 取所需内容。后来的 current-state 已存在，SecondMotor 也已退役，旧“新增入口/保留活跃 vehicle research”不再是当前待办。历史文本原件和唯一证据仍保留，本轮获授权的归档、去重与知识提炼替代其当时“不压缩任何历史”的泛化建议。
## 2026-07-24 的重复 Release 成本已经有独立复盘

该次 5 小时 49 分钟不是一次测试：包含至少十次从头 Release、实现、游戏验收、两轮后置审查及其修复。旧 `test.ps1` 是串行 fail-fast，没有真正 checkpoint/resume；从脚本中截取的诊断尾段 PASS 不能与前段拼成完整 PASS。审查明确认为第八产品的准入本来只需聚焦验收，后来 Update 自行加完整 Release 是范围扩张。

可复用改进：先做便宜且确实适用的入口检查和代码审查，再冻结最终输入；失败只修对应阶段、诊断未达尾部；产品日常门保留当前不变量，把历史 Batch replay 移到显式入口；阶段耗时可观测，完整套件只用于实际整体集成/发布。Workshop ID、ABI、native target、Harmony owner 可固定；产品总数、另一产品源码 token 和过时目录不应硬编码。此审查特别反对为此建立第二套缓存 PASS/收据权威。当前执行语义以 `docs/workflows/product-change-validation.md` 与现有 test 入口为准，不能把历史 clean-commit 要求泛化到一切小修。

该审计还纠正了“测试制造 5,596 行”的误解：此净增来自后置审查发现的真实 MES 丢物、冷恢复、事务和物理 owner 缺口，约 3,097 行是非玩家测试、1,511 行是产品、638 行是 dormant Host。mandatory GameBridge 字节未因此增长。应分别衡量维护量、默认加载量与分发字节，不能只用总代码行数推断玩家成本。

来源：`docs/reviews/code/2026/20260724-0002-complete-release-cost-resume-and-code-growth-audit.md`。
## 长发布日志中的可删重复与不能删的反例

07-27 prerelease Update 的完整正文约 99 KB，同一事务与候选先后出现在 Summary、步骤叙述、Validation、冻结表、审查补记及后续 Resolution。归档保留原件，日常入口只需最终 owner 和一处候选身份；无需把这些表再投影到每个路线页。它的 r1–r13 非验收尝试大量是工具编排问题：dot-source 覆盖外层参数、跨进程数组传参、宿主 hash helper、旧 provenance 分类、schema-3 journal、暂时失效的 source selection、错误 1/1 计数和 nominal 时长断言。ActionSpeed 已通过却因稍后的 AutoFishing 编排错误多次重跑，正是按输入/阶段复用证据的改进依据。

目标与 harness 也需分开：07-30 MES 旧包的明确 UI/两物品/一次保存目标通过，但清理时 runner 仍占 stderr，整轮记录 partial；Manbo Entry/WAV 注册通过但发现三个额外开发产品，不能写“Manbo-only”。另一次重复包审计停在无人回应的 BAT `pause`，没有开始有效包测试，不能取代先前精确候选 PASS。只重算旧验证器时，应绑定未变 raw/stage hash 并记录新解释，不改旧原始收据、不为改时限再开游戏。

0.6 Candidate11 的缺失 QA evidence 目录应视为合法 no-QA 零项状态。08-05 helper 的 `if` success stream 把 branch-local `@()` 展开成 null/单对象，`.Count` 失败发生在游戏前；只测人工 PSCustomObject 比较器漏过真实读取 helper。应测 absent/zero/single/many 的真正 helper，不能反复启动尝试、生成假的 QA 目录或把 prelaunch failure 记作游戏崩溃。该旧 Review 当时要求修后重跑完整 Release 属当时授权，不扩成现在小脚本修复固定门。

来源：`docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md`；`docs/reviews/code/2026/20260805-0001-candidate11-noqa-directory-receipt-array-shape-review.md`。
## 冻结发布总账的成本与后续覆盖

08-02 路线正文约 215 KB，把同一实现、文件清单、验证和后续状态重复投影到多个章节，还保存十轮当时用户要求的五切片并行审查。这些是历史授权和候选证据，不是当前每次修改都应执行的流程。08-09 的冻结头部优先于正文旧“继续更新此 Update”句子；新发布、ActionSpeed 新浇水行为、MES 后续 UI 等各回最新领域 owner。

其中大量失败源于测试环境而非待验行为：相同产品的确定性比较并行写同一中间目录导致 AccessDenied；纯 PowerShell 子脚本后读取未定义或陈旧 LASTEXITCODE；PS5.1 路径/编码与 PS7 发布宿主混淆；文案测试仍断言已暂停的旧部署语义；Catalog 手抄 LOC 过期；全局历史闭环误比较新提交而非冻结 Git blob；复制发布目录后只改日志断言，又重复启动游戏。应先固定适用输入、实际宿主和明确断言，避免以流程修复触发无关产品重验。

MoreSaves 测试曾因隔离整个 persistent root 同时改变官方 Local 发现来源、缺包/资料选择、首局对话等待和旧日志 token 而连续失败。无原生保存不意味着任何加载动作都无副作用：会迁移存档的版本仍需按迁移行为单独安排可丢弃目标；普通游戏行为则按当前验证 workflow 原地进行。旧整族哈希恢复与内部索引重写方案后来被用户选择的按角色 File.Move 简化取代，已通过迁移验证，不能把早期 pending 重新开成门槛。

最终用户明确将 GC 移出 0.6 阻断，仅新玩家 GC/Fatal 反馈才重开；未运行的手柄验收也未阻止该次结束。只复制已经验过且 payload 完全一致的候选不需再跑 stress。某产品行为改变只撤销其相关证据；旧原始收据不覆盖，解释变化记录在新结论中。当前必要测试和结束条件只由 [产品验证工作流](../../workflows/product-change-validation.md) 决定。

来源：`docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`，完整审阅所有正文、验证补记与冻结 FollowUp；其中没有按长度跳过机器段落。
## Skill 重复测试器已退役

08-29 将 Workshop audit Skill 移到仓库，随后 08-31 又将其压为 Runtime 专用路由并删除无调用者的 12,727-byte BAT-only 平行测试器。旧删除对象未被 Git 跟踪，其已知 hash 可定位历史证据，但不应恢复执行入口。普通产品上传同步不触发 Runtime 安装矩阵；Skill 路由变动的实际验证只需链接/描述/脚本语法，不触发包或游戏。该时点已实施的改动不能在本轮再作为待办。

来源：`docs/updates/2026/20260829-0002-repository-scoped-workshop-audit-skill.md`、`docs/updates/2026/20260831-0005-project-workshop-skill-authority-routing.md`。具体隐式调用与发现行为归当前 Skill/OpenAI 规则，历史文档不另设模型权限。