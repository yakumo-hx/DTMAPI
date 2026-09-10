# 产品身份与验收的历史经验

这是项目自有历史文本的主题提炼，按需查阅。历史版本、通过记录和阶段要求不能证明当前产品已实现、已发布或兼容新游戏。身份与保存规则查 [PROJECT](../../../PROJECT.md)，产品及发行事实查 [Catalog](../../../tools/release/dtmapi-product-catalog.json)，当前实施查 [事实路由](../../onboarding/current-state.md)。

## 已接受的区分

- 2026-07 的 Catalog 建设源于脚本 `Published/DeveloperOnly` 标签与真实公开历史不一致。产品已公开、当次订阅可用、进入 Advanced 准入、源码已修改、玩家包已验证及再次上传授权是独立事实。生成 registry 只承担其准入投影，不手写另一份产品名单。
- 当时用户明确选择一次性的正式产品 `1.0.0` epoch；这是有上下文的迁移决定，不是以后可以统一重置版本的通则。Mod 版本、最低 DTMAPI 要求、框架文件版本、程序集绑定身份承担不同职责。
- 当前 API 需求样例 `Yuuka.DTMAPI.AutoHarvest` 不继承旧 `None.AutoHarvest` 的 Workshop 身份；用户明确没有第一方 AutoHarvest 产品承诺。原件内存在的旧实现路径不是复用许可。
- 内部等价拆分、缓存、诊断降频和测试工具修复可以按已授权工程推进。只有具体证据表明需要改变用户行为、公共身份、保存/配置格式或兼容承诺时，才重新讨论那个决定。旧 Batch 投票和顺序不成为新的前置。

## 保留的维护经验

先明确产品、原生责任和测试所证明的行为，再决定检查范围。一个产品自己的 smoke 场景不宜持续扩张公共协调器。支持流程应让玩家提供可定位的日志/报告；维护者将问题、实施与实际验证分别回写原 owner。

来源：[产品路线与社区支持](../../archive/architecture/2026/20260611-product-roadmap-community-loop.md)、[版本与产品决定](../../archive/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md)、[Catalog 事实审查](../../archive/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md)、[工程决定与产品节点](../../archive/reviews/code/2026/20260715-0007-decision-escalation-policy-and-product-nodes.md)。

首次 Advanced 产品迁移曾建立 G2/独立 policy、完整 ownership 及 ABI 原件。保留这些已完成阶段的事实，不为后续产品小改复刻一个新 assurance 系统。删除私有 primitive/friend IL 会改变整份 Abstractions DLL 的字节信任锚，却不必然改变公共 ABI；合约更新必须比较实际字节和公开表面，不能为保留旧 hash 恢复死代码。

独立验收曾接受未改变产品的 L0–L3 与后续单跑 L4/L5 组合，并明确旧完整 Release 退出码仅有实施声明，未补造不存在的机器证明。runner 把 completed 写在 finally 清理前的缺陷仍应修正，但已通过最终清理的旧运行不因此失效。

来源：[首次 pilot 前置](../../archive/reviews/code/2026/20260720-0006-batch6-autofishing-advanced-pilot-prerequisite.md)、[Abstractions 字节合约](../../archive/reviews/code/2026/20260720-0007-batch6-autofishing-abstractions-compatibility-resign.md)、[独立验收](../../archive/reviews/code/2026/20260721-0003-batch6-autofishing-independent-acceptance.md)。

7 月多产品审查已明确两个执行成本问题：通用 builder 仍可每产品两次重建相同 SDK；通用 QA 仍可无条件要求所有已迁移产品。结构共用本身不证明执行范围缩小，必须显式传入所需 owner，并在同一运行复用不变的 SDK 构建。物理行数、非空行数、产品/QA/必载 Runtime 必须同口径比较；把旧 Compatibility 留在 Runtime 不能称作完成体积精简。

当前事实不应在 PROJECT、准入合同、API 表、README 与旧 Review 里复制产品计数和待验收状态。原 Review 保留发生时的判断，后续结论由对应 Update 与 owner 链接承接。前一次验收缺了新增条件时，可以限定原声明，或在下一次最小场景中补齐；不必为了用上“全部通过”而扩大测试。

来源：[多产品继续条件](../../archive/reviews/code/2026/20260722-0001-batch6-multi-product-continuation-readiness-audit.md)、[五产品重复投影与范围](../../archive/reviews/code/2026/20260722-0009-five-product-update-continuation-audit.md)。

无需求性能专项确实需要隔离所有实际已部署产品，官方 CoreOnly profile 只影响其覆盖的源，不自动停掉另一 managed 部署位置。7 月 formal 10,000-frame gate 因残留 Mine/Strong 事件而失败，不能把无 Fatal 算作无需求 PASS；它需要 receipt/destination 验证与只撤销自建 marker 的专项事务。这是 no-demand 测量前提，不是普通产品修复普遍隔离的理由。

来源：[无需求专项中的部署遗漏](../../archive/reviews/code/2026/20260728-0003-prerelease-no-demand-managed-product-isolation.md)。

2026-07-22 两产品收尾把通用 SDK/Catalog 构建器与产品专有 Hook/状态机分开：相似的打包脚本可以合并，但不构成 SharedNative 升级依据。兼容需求先注册、产品后出现的窗口还要求物理安装时重新核对 owner；只检查请求时或只测试相反加载顺序不足以证明 fail-closed。AutoFishing 深层恢复观察扩大到全部产品持有快照，但零快照只证明持有物清空和行为恢复，不等于逐个 native 值全部相等。来源：[两产品基线反馈](../../archive/reviews/manual-qa/2026/20260722-0001-two-product-baseline-closeout-feedback.md)。

上述 Review 内部保存了一处时间层次冲突：Issue 5 修正段写 OneActionComplete 部分能量、ConfigMenu 值保存/重载及实际 owner 停用尚未证明，尾部却写“五项全关闭”。对应 [OneActionComplete 实施 Update](../../archive/updates/2026/20260721-0005-oneactioncomplete-second-advanced-product.md) 提供了后续闭合链：20260722-125239 未执行这些条件，141220 才全部通过，161022 又在加法 API 重建后的字节上重验。原 Review 不改写成当时已通过，也不因旧 PARTIAL 把后续已补证的门永久留开。类似地，SDK receipt、包哈希、源码规模缩小和 DLL 缩小各证明不同事项，都不等价于实机行为或 GC 改善。该文的两次短进程和不跑完整长测是当次用户明确约束，可保留作为范围控制先例，但不形成新的固定测试额度。

官方 Mod UI 的历史反证表明，打开页面的 ReloadMods 只是准备可编辑列表，关闭时的 ReloadMods 也早于 SaveModManager 成功。曾建议的短时合并扫描只能减少重复，不能修正 authority：已提交启用状态应在原生保存成功、关闭事务完成后发布，失败保留上次 committed 来源。相似地，modal 输入隔离不能冻结全部 Mod Update 时钟；D4 的 audience、按住/释放结算与物理状态应分开，Suppress 不得删除物理 down 造成下一帧伪 Pressed。现行协议由平台 owner 持有，产品知识只保存此反证。来源：[Zoom/DebugConsole/D4 审查及用户纠正](../../archive/reviews/manual-qa/2026/20260801-0001-zoom-debugconsole-d4-input-review.md)。

玩家默认诊断层也不应把未启用包、已恢复的备用路径和当前功能失败混成一个红灯。2026-07-31 长 Review 还记录了禁用官方内容先被解析、官方 JSON 注释兼容、重复 SaveLoaded 判据与日志 Full 默认等方案；用户只选择了当次有限修正，下一版本方案归其规划 owner。历史列出的全部选项不是新任务必须实施的清单。来源：[同次审查的后续平台告警与决策](../../archive/reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md)。

最早实机脚本已经暴露场景前提错误：存档可能位于无资源的室内，不能据找不到 DungeonResourceRenderer 判产品 Hook 失败；树脂收集器是 decal，需合法 host/slot；打开动物面板后又等待 NormalGameState 的组合脚本会永远等不到 gameplay。后续 runner 应显式建模并恢复场景/UI 前提，而非不断延长超时或复制隔离目录。历史自动造树/资源等 fallback 只是当时夹具，当前普通测试按现行工作流选择必要前提。来源：[资源实击](../../archive/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md)、[互动各分支](../../archive/updates/2026/20260531-0012-actionspeed-interaction-gameplay-smoke.md)、[动物面板与玩法互斥](../../archive/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md)。

早期 Update 中“普通产品不应拥有 Harmony、必须交 GameBridge”是当时架构；后来 ProductNative 迁移已改变 owner，不能把这些历史规定重新加入必读规则。配置页可见、启动配置生效、同会话 ConfigMenu 保存后真正用到新倍率分别是不同证据；首次构建/原生接通阶段曾分别补证，也不意味着以后只改文字还要重复实机全链。来源：[工具动画首次接通](../../archive/updates/2026/20260531-0008-actionspeed-tool-animation-smoke.md)、[配置同会话生效](../../archive/updates/2026/20260531-0009-actionspeed-config-apply-smoke.md)。

观察层自身也可能制造失败：早期 Zoom GetState 返回内部可变对象，调用后产品更新使“before”同步变化，导致已经完成 4x/恢复的运行失败。应返回独立快照或在观测时冻结值。早期 Y 物品给予证据也明确区分 API 直接调用与真实鼠标命中 UI；后者的旧最近悬停右键 fallback 后来已被输入/命中 owner 修正，不能因一次自动化成功长期保留同类旁路。来源：[Zoom 首次切片](../../archive/updates/2026/20260606-0006-030-zoom-api-mod-slice.md)、[来源栏与 API 证据](../../archive/updates/2026/20260603-0004-y-console-source-columns-teleport-names.md)、[鼠标路径补证](../../archive/updates/2026/20260603-0005-y-console-mouse-give-smoke.md)。

2026-06-08 的 GameBridge 机械拆分对每个 feature 都依次执行 build.ps1、test.ps1（再次 Release build/Unit）和独立游戏进程，并为每个切片另写 Update/矩阵行；记录明确行为、Hook、日志和 API 未变。这是历史成本样本，不能作为今天按文件拆分就必须逐个实机的要求。partial 文件迁移改善导航但未改变编译依赖图，也不构成产品独立构建或性能优化完成。来源：[StrongPlant 机械拆分](../../archive/updates/2026/20260608-0005-gamebridge-strongplant-feature-split.md)、[ActionSpeed 机械拆分](../../archive/updates/2026/20260608-0012-gamebridge-actionspeed-feature-split.md)。

多份旧打包/分支记录反复修正 latest-report.txt 指向上一次 Camera 或 diagnostics 报告的情况。链接“latest”不能单独证明当前运行；有 result/log 的实测证据不必为了修正指针重跑功能，也不应把一个缺少新 runtime report 的产品专项判成没有实测。分支审计包、源码快照、压缩包和其中再次嵌套的同一 smoke 是重复载体，应保留唯一证据与可重建关系。来源：[ActionSpeed 审计包](../../archive/updates/2026/20260609-0014-actionspeed-branch-audit-package.md)、[合并前指针修正](../../archive/updates/2026/20260609-0016-actionspeed-merge-preflight-audit-fixes.md)、[ChestLocator 专项报告说明](../../archive/updates/2026/20260610-0013-chestlocator-smoke-case-file.md)。

旧 AutoFishing 先为缺 fresh report 新增导出，再单独新增 report-result 字段，并各跑一次 build/test/完整专项；这是输出链与功能链耦合产生额外运行的实例。需要报告导出的任务应保持独立结果，报告失败不能抹掉已取得的真实行为证据；只因历史记录存在报告字段，不把 fresh zip 变成每次产品修复的隐含前提。来源：[新增报告导出](../../archive/updates/2026/20260610-0050-autofishing-smoke-report-export.md)、[拆出报告结果](../../archive/updates/2026/20260610-0054-autofishing-report-export-result-field.md)。

QA 隔离与玩家存档隔离是不同问题。CropHarvesting 夹具后来改为显式 InstallQaFixtures、默认热键 None、独立 DTMAPI.Smoke.CropHarvesting owner，避免污染正常开发安装和产品状态；这并不要求每次普通行为测试复制玩家存档。同一记录的并行 build/test 曾抢占 SourceLink 中间文件，串行一次必要构建即可解决，不能把重复构建当互相独立的安全检查。来源：[Crop QA 隔离](../../archive/updates/2026/20260612-0009-crop-harvesting-qa-fixture-isolation.md)。

标题/暂停布局曾用每 500 ms 写 grid 约束与稍后原生 writer 对抗，产生横排/两列反复切换；后续改查具体 native owner 时序。这类周期性“修正”可能掩盖实际 writer，并不因为某次最终截图正确就证明无闪烁。来源：[轮询修正](../../archive/updates/2026/20260613-0002-title-pause-autofishing-follow-up.md)、[反复切换根因](../../archive/updates/2026/20260613-0003-title-pause-autofishing-root-fix.md)。

首次 Advanced pilot 计划四个小切片，实际主迁移一次大提交，后续再修集成；其 Update 如实记录了这点。到了 D.5，用户明确暂停完整 Release、L0–L5 和长循环，只给改变的责任做 focused 检查及最终短回归；误将 short run 路由到 L5 预检、误将‘一次有界验收’理解成运行次数上限，均是流程偏差。有效失败修正后重跑同一最小场景，不需要另造授权/全套验收。来源：[pilot 实际落地与复合证据](../../archive/updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)、[D.5 范围决定](../../archive/updates/2026/20260721-0003-autofishing-product-slimming-route.md)、[D.5 实施](../../archive/updates/2026/20260721-0004-autofishing-product-slimming.md)、[ActionSpeed 纠正运行额度误解](../../archive/updates/2026/20260722-0001-actionspeed-third-advanced-product.md)。

兼容性基线必须来自实际发布字节：7 月 retained Abstractions 的 informational commit 源码没有 Lamp 四类型，物理 DLL 却有，共 92 条签名。扫描十一产品没引用不授权删除，旧 host 程序证明绑定也不自动证明游戏行为；后续 exact-byte Mono canary 只闭合 Entry/StopOnManualMove setter/配置，不等于移动取消或 GC 梯度。带本地订阅输入的专项应显式 opt-in，普通测试不应依赖可变 Workshop 目录。来源：[retained ABI 与 Lamp shell](../../archive/updates/2026/20260715-0012-retained-autofishing-abi-host-gate.md)。

2026-07-15 Published11 与旧 DLL 输入专项要求 retained tree/hash/加载源一致，并独立观察 READY Gameplay 和 ReturnHome 后稳定窗口。其强制三存档文件备份/恢复是当时专项措施，已不能覆盖当前 PROJECT 的 NoNativeSave 默认。恢复任何刻意改写的文件前先确认游戏退出仍是必要边界；普通只读游玩不因此获得复制存档的前提。来源：[专项输入与公开产品矩阵](../../archive/updates/2026/20260715-0013-batch2-steam-player-input-and-public-product-gates.md)。

工具结果与功能证据还需拆开：Chest 2026-07-23 真机功能/owner 已通过，附带旧 Doctor 不认识新 policy 而报错；只重建 Doctor 对同一已安装目录只读复验即可，不需再跑游戏。Mine 后续修复 restore ledger、错误状态及重新打包，也保留 7 月 26 日已接受玩法矩阵，以 focused fault injection 和准确部署字节补齐。来源：[Chest Doctor 独立闭合](../../archive/updates/2026/20260723-0007-chestlocator-seventh-advanced-product.md)、[Mine 后续源码与字节修复](../../archive/updates/2026/20260726-0004-mine-eleventh-advanced-product.md)。

发布文案与焦点修复的历史闭合有可复用范围：Y 1.1.0 只改说明且 DLL 字节不变，精确单产品 SDK 包和 Catalog 检查复用既有玩家/游戏证据，未重跑全产品双构建；1.1.1 上传目录已与验证包一致时不重写。1.1.1 普通十一产品总 gate 因无关 Animal/Equipment 未完成而红，Y 子矩阵与 NoNativeSave 仍有效，最终独立玩家三项复测加 typed guard 日志关闭本题，整体 Failed 保留。来源：[1.1.0 上传准备](../../archive/updates/2026/20260813-0001-y-console-110-workshop-upload-preparation.md)、[1.1.1 焦点修复](../../archive/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md)。

## 物理归属与规模测量不能相互替代

冻结 Batch6 附件中，第十产品迁移默认 Runtime 物理行数减少 807、DLL 减少 24,064 bytes，但 Runtime 加同组产品增加 712 行、6,144 bytes。收益仅限同口径默认加载体积；不能宣传仓库、下载、总发货或全部产品启用后的内存同样减少。Mine 快照还混入先前 Manager 修复，不能作纯 Mine delta。Optional Host 首次加载后驻留，owner 状态清空不等于程序集卸载。

十二产品迁移没有产生十二项 SharedNative；Fish/Animal 的共同原生 item-title owner 曾以两个实际独立消费者支持共享，其他相似 Hook/模板不能照此晋升。C1 分类后 broad Unit 首次在旧 Host fixture 失败，遮住后续仍读已删 testmods 的错误；修正活跃引用才完成，旧 receipt 路径只留历史。构建图、执行默认集合与发布 artifact 集分别由自己的真实 owner 决定。

来源：[冻结身份附件](../../architecture/batch6-managed-mod-identity-contract.md)、[C1 完成记录](../../archive/updates/2026/20260726-0002-legacy-testmods-c1-physical-classification.md)。
