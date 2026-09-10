# Batch 6 边界修正前置审查

## 记录状态

- 日期：2026-07-19
- 状态：`recorded / Batch 6 Phase 0 admitted / product migration blocked`
- 性质：Batch 6 架构准入门，不是实现完成报告
- 范围：Core、GameBridge、Author SDK、功能性 CodeMod、内容宿主与 Batch 6 后续产品迁移
- Source：用户要求归纳“功能性 Mod 为什么最终进入 GameBridge”的讨论，并把结论整理为 Batch 6 前置文档
- Owning Update：[20260719-0001 DLL Mod Entry Model Audit](../../../updates/2026/20260719-0001-dll-mod-entry-model-audit.md)
- Correction Update：[20260720-0002 Batch 6 G0 Constraint Alignment](../../../updates/2026/20260720-0002-batch6-g0-constraint-alignment.md)
- 主要证据：
  - [DLL Mod 入口与迁移边界审查](20260719-0008-dll-mod-entry-and-migration-boundary-audit.md)
  - [AutoFishing / SMAPI 重归属边界审查](../../api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md)
  - [SMAPI API 缺口、功能性代码过量与内容宿主审查](../../api/2026/20260719-0011-smapi-api-gap-functional-surplus-and-content-host-review.md)
  - [轻量化与功能性 Mod 拆分路线图](../../../planning/2026/20260712-dtmapi-lightweight-functional-mod-roadmap.md)
  - [Batch 5 Event / Demand / Content Lifecycle / Performance Update](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)

本文中的“Batch 6”指 Batch 1-5 之后的下一轮实现工作，不是 [2026-07-13 第六轮决策裁决](20260713-0012-major-update-sixth-decision-docket.md)。后者关于冻结公开 ABI、分离兼容 canary 与迁移模板的结论仍然有效；本文只纠正“内部 API 等于实现必须留在 DTMAPI Runtime”的错误延伸。

2026-07-20 勘误：执行顺序以 [Batch 5 / Batch 6 前置复审](20260720-0001-batch5-and-batch6-prerequisite-audit.md)为准。G0 完成前只允许文档对齐、清单、消费者扫描、测试/回滚设计；G0 后才实现 G2 最小 vertical fixture；fixture 通过后只特批 AutoFishing pilot，G0-G7 全部通过后才开放其他产品。AutoFishing 原生行为和 GC 使用第五存档。

2026-07-21 保障成本勘误：`atomic` 只约束最终准入/可见性切换，不要求把身份、SDK、Loader、产品迁移、兼容、QA 和运行验收做成一个不可拆任务。产品实现可以在保持阻断的前提下使用小型可回滚切片。G4/G5/G6 复用一份迁移证据及现有 Catalog、SDK package receipt、ABI、Doctor 和 release 权威；不得因 gate 名称不同而为同一迁移复制三套 schema/builder/checker。历史全路径清单留作审计材料，默认门检查当前迁移差异、实际改变归属的 mixed files，以及迁移前已知且迁移后必须从 mandatory Runtime 消失的产品 owner/符号；净增量为零不能替代 zero-leftover。

本轮仅整理既有源码、Git 历史、SMAPI 对照与审查结论。未修改 Runtime、Loader、SDK 或 Mod，未启动游戏，也未取得共享 Runtime lock。

## 一、最终裁决

功能性 Mod 大量进入 GameBridge 不是一次显式裁决的结果，而是三条约束叠加后的唯一出口：

1. 功能性 Mod 要完成真实游戏功能，必须接触 Unity、Harmony 或游戏程序集；
2. 所有受 DTMAPI 管理的 Mod 又被禁止接触这些原生依赖；
3. 产品仍被要求完整实现。

在这三条同时成立时，产品原生实现只能进入 GameBridge。随后“产品 Mod 已经很小”“公开 API 已经内部化”“停用时 Hook 不工作”都可以成立，但 DTMAPI 基础 Runtime 仍会随产品数量线性增长。

因此，真正出错的不是“使用 GameBridge”本身，而是把下面两句话混为一谈：

> 稳定公共 API 不应泄露脆弱的反编译游戏类型。

> 所有受 DTMAPI 管理的 Mod 都不能直接引用 Unity、Harmony 和游戏程序集。

第一句仍然正确；第二句作为普遍规则必须取消。正确边界是：

- 多个独立产品共享、拥有共同 native owner，并且确实需要统一冲突与生命周期治理的原生能力，进入 GameBridge；
- 单一产品专用的原生实现，进入受 DTMAPI 管理的 Advanced CodeMod；
- JSON、PNG、WAV 等领域内容由可选 Content Host 承担领域引擎，不进入 Core，也不复制到每个内容包；
- 加载、版本、依赖、日志、配置、数据目录、命令、Owner Lifetime 与管理 UI 属于通用框架。

Batch 5 不是错误的起点。它为既有边界增加了 demand、generation、event kernel、生命周期和性能证据，解决的是“已有基础实现何时工作、怎样清理、怎样观测”。这些通用机制可以保留，但不能再被用作“单产品实现属于基础 Runtime”的证明。

## 二、错误是怎样形成并被固化的

| 时间 | 事件 | 当时合理部分 | 发生的偏差 | 影响级别 |
| --- | --- | --- | --- | --- |
| 2026-06-01 起始架构 | GameBridge 隔离脆弱游戏实现，同时计划 `helper.Patching` / `helper.Reflection` 高级出口 | 稳定 API 与原生游戏变化隔离是正确的 | 高级出口一直没有实现；普通 Mod 只有安全抽象通道 | 缺口出现 |
| 2026-07-05 SMAPI / 功能边界审查 | 正确识别 Core 应当“无聊”、产品逻辑应留在 Mod | 公开 API 收缩与产品策略归属正确 | “脆弱 Unity/Harmony/反射不进普通 Mod”从默认建议扩大成受管 Mod 的普遍禁令 | 概念偏差 |
| 2026-07-10 至 07-12 AutoFishing 拆分 | 建立内部 fishing primitives、friend opening 与产品门面 | 减少公开 ABI、形成可替换接缝 | 只改变可见性和调用方向，没有改变物理所有权；产品原生实现仍进入 GameBridge | 所有权偏差落地 |
| 2026-07-12 轻量路线图 | 以 AutoFishing / Zoom 为拆分样板，强调配置、策略、状态在 Mod | 产品策略拆分和停用静默正确 | 把“碰 Unity/Harmony/native 就进 GameBridge”写成通用执行模板，缺少每个产品对基础 Runtime 的边际预算 | 决定性执行错误 |
| 2026-07-13 U1 裁决 | 单消费者 gameplay API 默认不进入稳定 public API | ABI 冻结结论正确 | 若把“internal”误读为“必须由 DTMAPI Runtime 实现”，会继续掩盖物理归属问题 | 次级固化风险 |
| 2026-07-15 Author SDK `SDK160` | Strict CodeMod 获得简单、可检查的安全边界 | 严格通道对简单 Mod 仍有价值 | 文本扫描统一拒绝 BepInEx、HarmonyLib、Assembly-CSharp、UnityEngine；连注释命中也会拒绝，把架构偏差变成工具政策 | 工具固化 |
| Batch 5 | 为既有功能增加按需激活、generation、cleanup、观测和性能验证 | 通用底座与 GC 审计价值真实 | 成功指标关注 inactive silence、owner cleanup 和 Hook 数，没有重新检查“新增一个产品会给基础 Runtime 增加多少实现” | 优化错误边界 |

### 2.1 哪一次最关键

如果必须选择一个“决策出问题”的时间点，应分为三层：

- **最早缺失**：计划中的高级 Mod 原生出口没有实现；
- **概念转折**：2026-07-05 左右，把稳定 API 的隔离要求推广到所有受管 Mod；
- **决定性执行点**：2026-07-12 路线图把 AutoFishing 的 GameBridge 归属当成后续功能拆分模板；
- **最终固化点**：2026-07-15 `SDK160` 让作者工具无法表达 Advanced CodeMod。

所以不能只删除一条 SDK 检查就宣告修复。SDK160 是症状和固化器；Loader、打包、管理 UI、重启语义、Harmony owner、文档和迁移模板都必须共同支持 Advanced CodeMod，才算补回当初缺失的出口。

## 三、此前审查为什么没有及时发现

此前的成功指标主要回答：

- 公开 API 是否变少；
- 产品配置与决策是否移到 Mod；
- 没有消费者时是否不装 Hook；
- Owner 卸载时能否清理；
- Smoke / QA 是否离开玩家包；
- 旧 API 是否有兼容翻译。

这些指标都合理，却没有回答最重要的新问题：

> 每增加一个单产品功能，DTMAPI 基础 Runtime 必须永久新增多少产品专用源码、Hook、缓存、兼容面和测试脚手架？

“内部 API”也因此被错误地当成轻量证据。`public`、`internal`、friend assembly 回答的是**谁能调用**；Core、GameBridge、Advanced Mod、Content Host 回答的是**代码归谁维护并由谁装载**。把 5,000 行产品实现改成 internal 仍然是 5,000 行基础 Runtime 产品实现。

## 四、规模证据与风险断言

现有审查已记录：

- SMAPI 没有 AutoFishing 引擎。`Yet Another Fishing Mod 1.2.0` 的 1,474 行功能专用 C# 中，只有 54 行直接命名或调用 SMAPI 类型/成员，约占 3.7%，覆盖入口身份、五个事件、配置、翻译、日志、世界状态、反射、每屏状态、输入和 Mod registry 十类通用服务；804 行钓鱼/游戏/Harmony 实现和 450 行产品 GMCM 表单均由 Mod 自有，另有 195 行作者共享代码编入产品；
- AutoFishing 在关键切换提交中，从父提交的 3,158 行钓鱼实现变成 493 行产品 Mod、5,342 行新原生路径和 2,997 行旧兼容执行器，共 8,832 行；相比父提交净增 5,674 行；
- 当前工作树直接命名的 AutoFishing 相关维护面约为 8,922 行，另有通用/共享脚手架未计入；
- 当前五个 Runtime 项目约 77,071 行，其中 GameBridge 约 41,286 行；
- 当前可识别的产品形态 Runtime 功能至少约 16,249 行；
- 十二组结构化配对的产品/原生领域约为 3,342 行 Mod 对 20,534 行 GameBridge；
- 若一百个单产品原生功能沿用当前平均形态，基础层可能增加约 17 万行产品实现。这个数字不是预测，但足以证明旧规则不存在规模上限。

因此，Batch 6 不能再以“产品 DLL 很小”或“停用时不分配”为轻量化完成条件。新的硬性断言是：

> 单产品 ProductNative 迁移不得向强制安装的 DTMAPI Runtime 增加新的产品实现。任何正的基础 Runtime 增量，都必须被证明是 Platform 或 SharedNative，而不是用内部 API、provider、facade、demand route 或兼容层重新命名单产品代码。

源码行数不是性能 KPI，但“归属收据”必须包含行数和程序集变化，否则无法发现基础层线性膨胀。

以上行数是 2026-07-19 工作树快照，不是会自动更新的当前权威。G1 必须生成带 Git HEAD、采集时间、项目范围及 include/exclude 规则的可重复收据；后续裁决不得继续手写一次性总数。

## 五、Batch 6 统一归属模型

### 5.1 四类代码

| 分类 | 判定条件 | 默认归属 | 例子 |
| --- | --- | --- | --- |
| `Platform` | 对大量 Mod 通用，不表达某个玩法；不依赖单一产品存在 | Core / Abstractions / Bootstrap / GMCM | 加载、语义版本、依赖、日志、配置、命令、数据目录、Owner Lifetime、管理 UI |
| `SharedNative` | 至少两个独立真实消费者；共同 native owner 明确；统一补丁能减少冲突或保证全局不变量 | GameBridge | 可复用生命周期 Hook、共同输入/场景适配、经证明的通用原生事件源 |
| `ProductNative` | 只有一个产品消费者，代码表达该产品的状态机、补丁、缓存、动画或游戏规则 | Advanced CodeMod | AutoFishing、Zoom、OneAction、ActionSpeed、MoreSaves 的产品专用原生实现 |
| `ContentOwner` | 稳定领域 schema + 内容发现/校验/原生创建引擎；内容包本身没有 DLL | 可选官方 Content Host | CustomAnimals、未来 Audio Host；动物物种仍是独立 JSON/PNG/WAV ContentPack |

### 5.2 快速裁决顺序

对每段准备进入 GameBridge 的新代码，必须依次回答：

1. 它是否只是加载、版本、依赖、日志、配置、命令或 Owner 管理？是则归 `Platform`。
2. 它是否有两个以上彼此独立、已经存在的真实消费者？没有则默认归 `ProductNative`。
3. 这些消费者是否共享同一个 native owner、冲突点或全局生命周期不变量？没有则即使代码相似也不进入 GameBridge。
4. 它是否只是在解释 JSON/PNG/WAV 领域内容？是则评估 `ContentOwner`，且宿主应可选加载。
5. 它是否只是为了测试、采样或生成证据？是则归 QA，不进入玩家 Runtime。

“未来可能复用”“第一方 Mod 使用”“需要 Harmony”“放在中心更容易测”都不能单独证明 `SharedNative`。

### 5.3 native owner 安全条款

任何公共 API、SharedNative 或 Content Host 重建，必须先做本轮领域的 native owner 方法体或状态持有者审查。未找到 native owner 或状态持有者前，不得通过 Mod 层补丁、UI 成功、registry 成功或 smoke helper 冒充 API 重做完成。

Advanced CodeMod 可以拥有产品补丁，但仍必须说明它修补的 native owner、会话状态和清理责任；“实现留在产品”不等于允许盲目补丁。

## 六、Batch 1-5 已有成果如何处理

本次修正不是推倒 Batch 1-5：

- Batch 1 删除 Oil 产品所有权的方向应保留；Smoke 等临时替代物继续按 QA/产品归属处理；
- Batch 2 的版本、发布权威、依赖与兼容外壳属于平台能力；
- Batch 3 的 author session / reload 能力需要重新区分通用作者基础设施和特定产品负担，严格 SDK 政策进入本次修正范围；
- Batch 4 把大量 QA 从玩家 Runtime 物理移出，是已经成立的轻量化成果；
- Batch 5 的 event kernel、generation、demand coordinator、runtime boundary 和 owner cleanup 属于可保留的通用底座；
- Batch 5 中只为单个产品存在的 demand route、provider、facade、status publication、兼容 executor 和 QA 编排不得因为已经通过测试就自动升级为永久平台能力。

换言之，保留通用机制，重新裁决产品路由；保留行为证据，不保留错误物理归属。

## 七、Batch 6 准入门

Batch 6 分为两个层级：

- **Phase 0：边界修正与基础设施**，本文件记录后即可开始；
- **产品迁移阶段**，除 G2 最小 fixture 通过后可显式特批唯一 AutoFishing pilot 外，在 G0-G7 满足前保持 `BLOCKED`。

### G0：架构权威统一

状态：`BLOCKED`

必须把以下四种发布/管理身份写入并统一到 PROJECT、Agent 规则、Author SDK、作者文档、Loader 设计和管理 UI 术语：

- Strict CodeMod：只引用 Abstractions；
- Advanced CodeMod：由 DTMAPI 发现、排序、启停、诊断，但可引用 Unity/Harmony/游戏程序集；
- ContentPack：无 DLL，由声明的 Content Host 管理；
- External BepInEx Plugin：不受 DTMAPI Owner 与启停承诺管理。

2026-07-20 文档切片已把四种身份和物理归属的唯一规范移到 `PROJECT.md`，并对齐 Agent/API 工作流。G0 剩余工作只冻结和对齐 Loader、SDK、Doctor、Manager、打包与 UI 的身份词汇、设计和范围，不实现 Advanced 通道；这些组件的真实 vertical slice 属于 G2。G0 仍为 `BLOCKED`，在专门的项目方向 Update 完整通过前不得开始 Advanced CodeMod Runtime 实现。

通过标准：权威文档不再出现“所有 DTMAPI Mod 禁止 native 引用”或同义普遍规则；Strict 和 Advanced 的差异在一处拥有规范定义，其余文档只引用该定义。

### G1：Batch 6 域清单与边际预算

状态：`BLOCKED`

每个计划迁移或重构的领域必须先登记：

- 当前产品消费者和真实消费者数量；
- native owner / 状态持有者；
- 当前 Core、GameBridge、Mod、QA、Compatibility 的文件与物理行数；
- 基线收据的 Git HEAD、采集时间、项目范围、include/exclude 规则及可重复命令；
- 目标分类：Platform、SharedNative、ProductNative 或 ContentOwner；
- 目标程序集和玩家是否强制加载；
- public/internal API 变化；
- Hook、Harmony owner、长期缓存、Updater、事件订阅与清理责任；
- 迁移后基础 Runtime 与产品包的边际变化。

通过标准：所有 Batch 6 域完成分类；ProductNative 的基础 Runtime 产品实现目标增量为零，正增量均有 Platform/SharedNative 证据。

### G2：Advanced CodeMod 通道真实可用

状态：`BLOCKED`

必须实现并验证：

- 显式 manifest/项目通道，不靠扫描源码猜测作者意图；
- Author SDK 对 Strict 与 Advanced 采用不同引用政策；
- 仍由 DTMAPI 的 `MODS` 发现、版本/依赖排序、日志、配置与管理 UI 管理；
- Advanced Mod 使用唯一 Harmony owner，并能在禁用/重载支持范围内明确卸载或声明需要重启；
- Manager/Doctor 能显示“高级原生 Mod、需要重启、依赖游戏版本/程序集”等普通玩家可理解的信息；
- 产物仍在 DTMAPI Mod 包内，不要求作者把普通 Mod DLL 放进 `BepInEx/plugins`；
- 构建、安装、启停/重启、故障隔离和日志收集有静态测试与游戏证据。

分阶段通过标准：先由最小 Advanced fixture 证明 manifest、加载、管理、重启/禁用、诊断和打包的完整受管通道；该 fixture 通过后，才显式准入唯一 AutoFishing pilot。实现切片保持隐藏/阻断，最终准入才是原子动作。AutoFishing 完成 G3/G4 及相关 G5/G6 后，为 G2 补齐真实产品证明；不能只放宽 `SDK160`。

### G3：AutoFishing 作为反向迁移试点

状态：`BLOCKED`

AutoFishing 必须先证明新边界能工作，再把它作为其他功能 Mod 的样板：

- Mod 自有配置、策略、状态机、原生适配、Harmony 补丁、会话缓存、交易/动画协调和清理；
- 从 mandatory GameBridge 删除 fishing-only primitives、friend opening、provider/facade、demand route、状态发布和只为 fishing 存在的脚手架；
- 旧兼容执行器独立、限时、可扫描消费者，并有明确退役版本；
- fishing QA 留在可选 QA 或产品测试工程，不回到玩家基础 Runtime；
- 行为、保存读取、禁用/重启、异常退出、长期 GC 与无残留进程均有证据；
- 迁移前后配置键、Mod ID、Workshop 身份和用户存档兼容策略明确。

通过标准：AutoFishing 不再要求 mandatory GameBridge 承载单产品原生执行器，并通过专门 Update、Review、构建与游戏 smoke。详细文件级裁决以 API Review 0010 为准。

### G4：基础 Runtime 边际增量守门

状态：`BLOCKED`

每个产品迁移 Update 必须附可复核的“归属证据”。优先使用通用迁移 evidence manifest；只有出现新的权威边界时才能新增收据类型。该证据既检查增量，也检查迁移前已知的产品 owner/符号在 mandatory Runtime 中清零：

| 指标 | 迁移前 | 迁移后 | 允许解释 |
| --- | ---: | ---: | --- |
| mandatory Core / GameBridge 产品实现行数 | 记录 | 目标不增加 | 正增量必须证明为 Platform/SharedNative |
| 产品 Mod / Content Host 行数 | 记录 | 记录 | 产品复杂度增加本身不是失败 |
| mandatory 玩家程序集与可选程序集 | 记录 | 记录 | 移到另一个强制 DTMAPI DLL 不算轻量化 |
| public / internal 契约 | 记录 | 记录 | internal 化不等于物理拆分 |
| Hook、updater、长期根和 inactive 分配 | 记录 | 记录 | 与源码归属分别判定 |

通过标准：代码审查、自动测试和 Update 都能复核这张收据；不得只报告产品 DLL 变小或停用时静默。

### G5：Batch 5 机制完成重新分类

状态：`BLOCKED`

对当前产品迁移实际触及或改变归属的 Batch 5 event、demand、generation、reload、performance 路径，以及迁移前已知且迁移后必须清除的产品 owner/符号逐项标注；历史全路径 inventory 只作一次性审计输入，不作为每个产品的永久 exact-set 门：

- 通用底座：保留；
- 单产品路由：随 Advanced Mod 迁移；
- QA/性能观测：进入可选 QA；
- 旧消费者翻译：进入有退役窗口的 Compatibility。

通过标准：Batch 5 的完成状态不再被解释成当前 GameBridge 物理归属已经正确；AutoFishing 等产品的 demand-inactive 证据只证明停用成本，不证明所有权。该结论与 G4/G6 共用一份迁移证据，不另建同形 schema/builder/checker。

### G6：0.5.5 兼容与破坏性变更计划

状态：`BLOCKED`

必须先扫描当前 0.5.5 public/internal/friend 消费者，再决定：

- 旧调用继续由限时兼容程序集翻译；
- 旧 API 在一个明确版本发出 warning 后移除；
- 还是以大版本/预览通道做有意破坏。

通过标准：没有静默删除；兼容层不承载新产品实现；版本、最低 DTMAPI 版本和游戏版本使用统一语义版本规则。消费者、包和二进制事实优先引用 Catalog、Author SDK package receipt、ABI/Doctor/release 既有权威，只在统一迁移证据中记录产品特有差异。

### G7：内容宿主与功能 Mod 分轨

状态：`BLOCKED`

CustomAnimals、未来 Audio、矿井/机器 JSON 与功能性 DLL Mod 不走同一个“全部下放”结论：

- JSON/PNG/WAV 动物包继续由 DTMAPI 管理身份、依赖、启停与配置；
- CustomAnimals 是可选 Content Host，拥有稳定领域 schema、校验、资源加载和原生创建桥；
- Hatch、Mole、Drecko、Oilfloater、Shell Crab 等物种只拥有自己的内容与经济设计，不复制宿主 DLL；
- 缺少宿主时内容包必须明确阻止加载并给出普通玩家可读错误；
- Content Host 不得顺手承载单物种经济、产物或产品策略。

通过标准：`ContentPackFor`、宿主版本约束、owner-scoped helper、资源清理与管理 UI 状态形成稳定契约；CustomAnimals 是否从 mandatory GameBridge 物理移出有独立设计与验证。

## 八、Batch 6 各领域初步归属

这张表是准入前假设，最终以 G1 native owner 审查为准。

| 领域 | 初步分类 | Batch 6 默认动作 |
| --- | --- | --- |
| AutoFishing | ProductNative | 作为 Advanced CodeMod 试点，移出 mandatory GameBridge |
| Zoom 游戏内缩放 | ProductNative | 原生缩放实现进 Advanced Mod；标题页/管理 UI 修复若为全局不变量可单独审为 SharedNative/Platform |
| OneActionComplete | ProductNative | 产品自有补丁与状态，不扩 GameBridge |
| AutoHarvest | ProductNative | 自动收获行为属于产品；只有被多个独立 Mod 消费的稳定收获事件/命令才进入平台 API |
| ActionSpeed | ProductNative | 产品自有补丁；只有被第二个独立产品复用的原生动作事件才考虑 SharedNative |
| MoreSaves | ProductNative | 存档命名、分页/滚动与额外槽位属于产品；通用数据目录和保存生命周期属于 Platform |
| Y Console / Debug Console | ProductNative + Platform seam | Console UI 是产品；命令注册、权限、日志输出契约是 Platform |
| Chest locator / enhancer | ProductNative | 不以内部 service 形式永久留在 GameBridge |
| Fish breeding tooltip | ProductNative | tooltip 产品逻辑留 Mod；未来有多个消费者时再审通用 tooltip seam |
| Animal husbandry progress | ProductNative | 进度展示与规则留 Mod |
| Strong Planting | ProductNative | 产品补丁与策略留 Mod |
| Oil 掉落 | ContentPack / official JSON | 优先由官方 JSON 表达；未正式发布的产品代码不进入 GameBridge，和矿井玩法分开裁决 |
| Mine / powered mineral machine | ProductNative 或 ContentOwner | 耗电产矿、放置预览缩放和玩法规则先归产品；只有稳定、多内容包 schema 已证明时才建立可选机器宿主 |
| Equipment slots | ProductNative | 单产品功能不因接触 native inventory 就进入 GameBridge |
| CustomAnimals | ContentOwner | 可选官方宿主；物种包继续无 DLL |
| AnimalPack | ContentPack 产品 | 合并物种、经济与产物设计归内容产品；不把单物种经济写入 CustomAnimals 宿主 |
| Audio replacement | ContentOwner 候选 | 先完成 native owner 与复用模型；不把每个音频案例硬编码进 GameBridge |
| BGM replacement | ContentOwner 候选 / deferred | 先研究稳定资源与 native owner，不作为 Batch 6 扩基础 GameBridge 的理由 |
| GMCM / Mod 管理 UI | Platform | 继续由 DTMAPI 本体提供，并按普通玩家信息、统一条宽、分页/滚动重构 |
| Event kernel / owner cleanup / version / registry | Platform | 保留并稳定，不表达产品策略 |
| CustomEntities speculative facade | blocked / retire-or-internalize | 未找到真实 native creation owner 前，不作为 Batch 6 平台扩张依据 |
| 联机 | 独立大型项目 / deferred | 不并入 Batch 6 功能迁移，不提前建设投机性公共 API |
| 宠物 / 独立载具 | 未裁决领域 / deferred | 先做 native owner 与状态模型研究；即使未来近似动物内容，也不预设必须进入 CustomAnimals 或 GameBridge |

## 九、Batch 6 开始后允许与禁止的工作

### Phase 0 允许立即开始

- 完成 G0 权威文档与术语统一；
- 建立 G1 产品/领域清单与可重复的源码/程序集基线脚本；
- 设计 G2 Advanced CodeMod 通道、最小 fixture、静态/游戏验证与回滚方案；G0 完成前不得实现其 Runtime；
- 扫描 0.5.5 消费者和兼容窗口；
- 完成 Batch 5 通用机制与产品路由重新分类；
- 修复阻断性正确性、崩溃、存档或 GC 证据链问题；
- 为 AutoFishing 迁移准备源码、测试与回滚边界。

G0 完整通过后才可分片实现、一次原子准入 G2 最小 vertical fixture。fixture 通过后只准入 AutoFishing 作为唯一产品 pilot；完成 G3/G4 及相关 G5/G6、使 G2 获得真实产品证明并令 G0-G7 全部通过后，才准入其他产品迁移。

### 产品迁移通过准入前禁止

- 为新的单产品功能继续新增 GameBridge service、provider、facade、friend primitive 或 demand route；
- 因为代码使用 Unity/Harmony/游戏程序集就自动判入 GameBridge；
- 把 AutoFishing 当前结构复制成 Zoom、MoreSaves、Console 或其他产品的模板；
- 把代码移动到另一个仍由所有玩家强制加载的 DTMAPI DLL 后称为轻量化；
- 以 `internal`、inactive silence、Hook 数下降或产品 DLL 很小替代物理归属证明；
- 为尚无两个独立消费者的“未来通用能力”扩公开 API；
- 未做 native owner 审查就通过 UI、registry 或 smoke helper 宣布 API 完成。

## 十、Batch 6 Ready 定义

Batch 6 的 Phase 0 已由本文件准入；功能产品的批量拆分只有同时满足以下条件才进入 `READY`：

- [ ] G0：Strict、Advanced、ContentPack、External 四种身份在权威文档和工具中一致；
- [ ] G1：Batch 6 全部领域有消费者、native owner、当前基线、目标归属和边际预算；
- [ ] G2：Advanced CodeMod 的构建、加载、管理、重启/禁用和故障诊断通过静态与游戏验证；
- [ ] G3：AutoFishing 反向迁移完成，mandatory GameBridge 不再承载其单产品执行器；
- [ ] G4：通用迁移证据可证明 mandatory Runtime 没有新增 ProductNative，且迁移前已知的本产品 ProductNative owner/符号已清零；
- [ ] G5：当前迁移涉及的 Batch 5 通用底座、产品路由、QA 和 Compatibility 完成分类；
- [ ] G6：0.5.5 消费者、warning、兼容与破坏性版本计划已记录并引用既有 package/ABI 权威；
- [ ] G7：ContentPack / Content Host 与 Advanced CodeMod 的加载、依赖和 UI 语义分轨；
- [ ] AutoFishing 的构建、第五存档原生行为/GC smoke、启停/重启、长期 GC、退出无残留进程及日志证据完整；
- [ ] 路线图不再以“产品 DLL 很小”作为拆分成功代理，且 Batch 6 不再复制旧 AutoFishing 模板。

任一项未满足时，可以继续 Phase 0、关键修复和证据工作，但不得批量迁移其他功能产品。

## 十一、回滚与后续记录规则

本文只改变后续决策门，不改变当前 0.5.5 行为。若后续 Advanced 通道被证明无法安全管理 Harmony/原生依赖，应回滚该实现 Update，而不是恢复“所有产品实现都进 GameBridge”的普遍规则；届时必须重新提出可规模化的第三种物理所有权方案。

后续实施必须留下最小且单一的生命周期权威：

- 一个 G0/G2 项目方向与 Advanced 通道 Update；
- 一个 AutoFishing 反向迁移 Update，并复用已存在的边界 Review；只有新的歧义或根因才新增 Review；
- 每个 Batch 6 产品一份统一迁移证据；G4/G5/G6 不各自复制 receipt family；
- 只有真实 API 状态变化时才更新 public API matrix；
- 只有真实游戏运行时才更新 smoke/debug 证据。

## 十二、结论

轻量 DTMAPI 不是“把功能 Mod 的入口和配置拆出去，再把实现留在 GameBridge”，而是让基础 Runtime 的增长只来自真正通用的平台能力和已经证明共享的原生能力。

这次偏差的修复顺序必须是：

```text
G0 统一架构规则
  -> G1 清单/预算 + G2 最小 Advanced fixture
  -> 只准入 AutoFishing 反向迁移 pilot
  -> G3/G4/相关 G5/G6，并为 G2 补齐真实产品证明
  -> 清理单产品 GameBridge 脚手架与限时兼容
  -> G0-G7 全部通过后再开始其他产品迁移
```

因此，Batch 6 现在可以开始边界修正 Phase 0，但在 Advanced 通道和 AutoFishing 试点完成前，其他功能产品迁移保持阻断。

## 十三、2026-07-20 Phase 0 收口回链

修正后的 Phase 0 已由 [Update 20260720-0003](../../../updates/2026/20260720-0003-batch6-phase0-closure.md) 收口：G0 权威合同、G1 全域 baseline/decision inventory、G2 设计投影以及 G5/G6 持久门禁均已落盘。候选、延期和未决领域没有被伪造为终局归属，完整 G1 ownership gate 仍阻断。该结果只开放后续独立的 G2 最小 synthetic vertical slice；G2 Runtime、Advanced 实装、AutoFishing 与其他真实产品迁移仍保持阻断。
