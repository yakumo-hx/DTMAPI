# DTMAPI 轻量化与功能性 Mod 拆分路线图

> 日期：2026-07-12
> 状态：执行路线图，尚未表示各阶段已经完成
> 历史输入基线（2026-07-12）：`0.5.3-alpha`、Owner Lifetime 重构与 Zoom/AutoFishing 第一方产品拆分之后；当前版本以 Runtime 版本权威为准

> 2026-07-30 当前状态：边界修正、G2 synthetic fixture 与十一个 Advanced/ProductNative 产品均已 verified/closed；MoreEquipmentSlots 已实现，但新版 1.0.0、迁移和发布验收已由用户延期，其五个事务 P1 保留为下版本输入。Mine 的运行周期/电力/产出已物理拆分，但借用水井贴图与运行时 2x 仍阻塞它自身公开发布；DebugConsole 已完成行为等价拆分与生命周期所有权收口，视觉/UX 重写继续延期。第十三及以后产品与 Content Host G7 仍阻断。0.5.5 采用 owning Update 的既有冻结制品；完整 Release、冻结候选包审计、Manbo 与真实旧 MoreEquipmentSlots 订阅包兼容均已有证据。实际上传目录同步、Catalog releaseStop、三语言人工操作、Steam 上传及上传后复核仍未完成。精确生命周期见 [Batch 6 身份契约](../architecture/batch6-managed-mod-identity-contract.md)，当前发布顺序见 [0.5.5 发布前路线图](20260727-dtmapi-055-prerelease-roadmap.md)。

## 1. 目标

DTMAPI 下一阶段不以“尽快删除代码”为目标，而以缩小普通玩家实际承担的运行时、生命周期根和长期兼容面为目标。

本路线图把“轻量化”分成三个彼此独立的指标：

1. **玩家运行时轻量化**：普通发布包少带、少加载 QA、性能探针和不再需要的兼容执行器。
2. **停用状态轻量化**：没有消费者时不安装功能 Hook，不创建 updater、session、lease、长期集合或每帧查询。
3. **维护面轻量化**：缩小公开 API 和兼容承诺，让产品策略留在 Mod，DTMAPI 只维护通用平台能力与必要的原生桥接。

其中，把 Smoke 移到 QA 程序集会明显减轻玩家包，但不会减少仓库中的测试源码；把功能策略移到 Mod 会改善边界，但不意味着 Unity/Harmony 原生适配也应复制到每个 Mod 中；减少源码行数也不能直接证明 GC 或性能改善。

## 2. 当前基线

以下数字是 2026-07-12 对工作树中 C# 物理行数的快照，仅用于判断代码集中位置，不作为性能结论或强制 KPI。统计排除 `bin`、`obj`、文档和脚本。

| 区域 | C# 文件 | 物理行数 | 当前角色 |
| --- | ---: | ---: | --- |
| `DTMAPI.Abstractions` | 12 | 3,774 | 公共契约与 DTO |
| `DTMAPI.Core` | 37 | 17,487 | 加载、生命周期、事件、配置、注册表与日志 |
| `DTMAPI.BepInExBootstrap` | 7 | 6,649 | 进程入口、帧驱动和宿主 |
| `DTMAPI.GameBridge.DolocTown` | 97 | 50,989 | 游戏原生适配、Hook、诊断、Smoke 与兼容层 |
| `DTMAPI.ModConfigMenu` | 4 | 1,449 | 配置菜单宿主 |
| **玩家运行时源码合计** | **157** | **80,348** | 当前正常构建会进入运行时项目的源码 |
| 第一方产品 Mod | 4 | 684 | AutoFishing、Zoom 等产品策略 |
| `testmods` | 18 | 2,437 | 测试 Mod |
| 单元测试 | 1 | 10,870 | 测试工程 |

本地 SMAPI 源码的可比运行时区域约 42,270 行。DTMAPI 当前约为其 1.9 倍；这个比较只能说明当前 DTMAPI 把更多验证、兼容和游戏适配集中在运行时项目中，不能直接比较功能完整度、启动速度或内存效率。

GameBridge 内部的主要集中区如下：

| GameBridge 区域 | 物理行数 | 判断 |
| --- | ---: | --- |
| `Smoke` | 13,433 | 应保留在仓库，但不应由普通玩家加载 |
| `Compatibility` | 3,077 | 需要消费者审计和退役窗口，不应继续承载新逻辑 |
| `FishingAutomation` 功能区 | 5,315 | 原生能力边界已形成，仍需处理物理装载与兼容尾部 |
| 其他 `Features` | 19,599 | 后续按实际消费者与原生职责逐项审计 |

如果只把 Smoke 和现有 Compatibility 从普通玩家运行时物理移出，理论基线会从约 80,348 行降到约 63,838 行。这个数字不包含拆分所需的新接口和装配代码，因此只用于说明优先级，不是承诺值。

公开面同样需要约束。粗略类型声明统计显示，`DTMAPI.Abstractions` 约有 263 个公开声明，其中 `ExperimentalGameBridge.cs` 单文件约有 104 个。下一阶段应先冻结扩张，再做真实消费者审计，不能因为第一方 Mod 使用就自动升级为稳定公共 API。

## 3. 目标架构

```text
BepInEx
  -> Bootstrap
       只负责进程入口、帧源、基础输入采样与 Runtime 宿主
  -> Core
       Mod 加载、Owner Lifetime、事件、配置、API 注册、日志
  -> GameBridge Runtime Capabilities
       多消费者共享的 Harmony/反射/原生类型适配、共享仲裁、按消费者激活
  -> Functional Mods
       玩家功能、配置、热键、策略与状态机；高级受管 Mod 可同时拥有其单产品原生实现

未来可选官方内容宿主（G7 尚未实现，0.5.5 后下一版本再设计/准入）：
  -> DTMAPI.CustomAnimals / DTMAPI.AudioReplacement optional content substrate
       可由 DTMAPI 官方维护并随 Runtime 分发；只有对应内容需求存在时才加载实现、安装已审查 Hook
       物种、概率、经济、产物和音频映射仍由 AnimalPack、Manbo 等内容产品拥有

0.5.5 当前仍不形成通用 Audio Host：短 SFX 保持既有产品/兼容边界，AnimalVoice 是 CustomAnimals 内容依赖；BGM 继续阻断。

可选开发组件：
  -> GameBridge QA
       Smoke、性能探针、场景编排、断言与证据输出
  -> Compatibility
       有明确版本窗口的旧 API 翻译，不承载新产品逻辑

外部共存组件：
  -> BepInEx-only Plugins
       可借用同一套 BepInEx，但不获得 DTMAPI Owner 清理保证
```

### 3.1 职责判定表

| 问题 | 应归属 |
| --- | --- |
| 是否对所有 Mod 作者都通用，且不含游戏功能策略 | Core / Abstractions |
| 是否必须接触 Unity、Harmony、反射或原生游戏对象 | 先判断消费者：多产品共享且 native owner/冲突/生命周期已证明则为 GameBridge；单产品专用则为高级受管 Mod |
| 是否决定玩家何时启用、如何配置、按什么热键、采取什么动作 | 功能性 Mod |
| 是否只为验证场景、采样性能、生成证据而存在 | QA 程序集 |
| 是否只服务旧消费者并能翻译到当前能力 | Compatibility |
| 是否是独立 BepInEx 插件且不使用 DTMAPI 契约 | 插件自身负责生命周期 |

### 3.2 非目标

- 不复制 SMAPI 的代码、命名或全部历史设计；只借鉴其平台与产品分层原则。
- 不把共享原生 Hook、Harmony 补丁和反射访问器下放并复制到每个产品 Mod。
- 不删除测试、性能证据或调试能力，只改变它们的装载与发布位置。
- 不为了“看起来模块化”立即把每个小功能拆成单独 DLL。
- 不在物理迁移时顺手更改 Mod ID、配置键、Workshop 身份或版本策略。
- 不把源码行数下降解释成 GC 已解决；GC 仍以运行时分配、根与趋势证据判断。

## 4. 分阶段路线

### Phase 0：先关闭正确性前置项

> 2026-07-21 当前状态：下面三项与 Batch 6 Phase 0 correction、G2 synthetic fixture、AutoFishing 相关 G3-G6 均已关闭；精确实现提交和收据只由 [Batch 6 身份契约](../architecture/batch6-managed-mod-identity-contract.md)及其链接 Update 保存。它们是历史前置，不再是当前阻塞，也不需要为 D.5 重放。

在继续拆功能或移动程序集前，先处理当前审查发现的三个问题：

1. **依赖版本必须以实际加载实例为准。** 热刷新时，同源 provider 的新 manifest 不能让依赖检查误以为旧 DLL 已升级。增加原地升级、降级和同源版本变化回归测试。
2. **诊断同时限制条数与字节。** Owner ledger、diagnostic key 和 details 不应保存任意长度原文；采用长度上限、截断标记或哈希，并保留总数、最近失败分类和有限样本。
3. **恢复真实的零警告基线。** 修复 `ConfigMenuRegistry.cs` 当前的 `CS8602`，确保全量 Release 构建的“0 warning”与记录一致。

完成门槛：全量单测通过；热刷新版本回归通过；诊断超长输入有界；Release 构建真实 0 warning。

### Phase 1：把 Smoke/性能探针移出普通玩家运行时

> 2026-07-21 状态：Batch 4 G1-G9 已完成依赖地图、可选 `DTMAPI.GameBridge.DolocTown.QA`、通用 harness/report/JSON/趋势探针与场景迁移、开发暂存和普通玩家包排除；AutoFishing QA 也已随已验收 ProductNative 路线归位。后续只处理已证明仍驻留 production 的 QA-only seam，不重新创建 QA 工程或重复第一批搬迁。

> 2026-07-22 审计：首批真实残余已定位为 Bootstrap 无消费者的输入诊断计数/摘要、Core 旧字符串输入适配器与 ActionSpeed 只供 QA 反射读取的热路径摘要；Animal 的 80ms UI 重写是 ProductNative 热点。中性 QA Host、事务/race fault seam 和 ISSUE-010 生命周期 breadcrumb 必须保留。详见 [生产 QA seam 与 Animal refresh 审计](../reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)。

> 2026-07-23 收口：死输入诊断、ActionSpeed eager QA 汇总、Animal 热点以及 Core 旧字符串输入/fatal-window 端点均已完成。内部 `RecordInputPressed/Released`、`GetRegisteredInputButtons` 和未接通真实探测的 fatal 通知链已删除；公开 `IInputHelper` 字符串 ABI、typed frame、ISSUE-010 正常 breadcrumb 与外部进程 fatal-window 捕获继续保留。聚焦 Core build/Unit/source 检查通过；因无原生行为改动未启动游戏。

这是已建立基础、仍需收尾的结构工作。

1. 保留已完成的 `GameBridge/Smoke` 依赖清单、internal QA seam 与发布排除合同，不重复建立第二套 QA host。
2. 盘点 production GameBridge 中仍存在的 QA host、测试 fault seam 和仅测试消费者，逐项证明保留或迁出。
3. 通用 harness/report/JSON/趋势采样继续由可选 QA 程序集拥有；不得重新编入普通玩家 Runtime。
4. Fishing/Owner 等单产品 native QA 的最终物理归属服从 G0-G2；AutoFishing 已完成该迁移，D.5 只收缩仍由 QA 消费的生产 observation/diagnostic seam，其他产品仍需等待自己的准入。
5. 每次收尾继续验证开发/QA 包显式暂存、普通 Workshop 玩家包排除、协议兼容和 exact cleanup。

完成门槛：

- 普通玩家包不含 QA DLL、Smoke 类型和测试专用静态根。
- 现有第三存档与第五存档 Smoke 仍可由 QA 包运行，结果协议保持兼容或有迁移说明。
- DTMAPI 公共 API、生产 Hook 行为和玩家功能不因迁移而变化。
- 不要求为这一阶段运行钓鱼 100/500 次；与迁移直接相关的短场景足够。

### Phase 2：收缩 Compatibility，而不是继续堆叠旧执行器

Compatibility 的处理顺序必须是“消费者证据 -> 迁移能力 -> 版本窗口 -> 删除”，不能直接删。

> 2026-07-24 完成边界：首轮扫描确认的五个旧 ABI 消费者及后续 MoreSaves、ChestLocatorEnhancer、MoreEquipmentSlots、Zoom，共九套冻结重执行器进入同一个 `netstandard2.0`、无 Mod/Workshop 身份的 `dormant-shipped` Compatibility Host；旧 provider ID、ABI、Harmony owner、双顺序 fail-closed 与生命周期保持不变，普通启动不加载 Host。历史 Host 前置拆分先把 GameBridge 从 1,084,928 降至 966,656 字节，MoreSaves/Chest 再降至 937,984；第八产品降至 851,456，第九候选降至 825,344 字节。当前 Host 为 437,760 字节，十个产品 DLL 也分别随包发货，因此只宣称减少默认加载体量，不宣称减少下载、安装、仓库源码或总发货体量。详见 [旧 ABI 消费者与可选 Host 审计](../reviews/code/2026/20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md)及对应产品 Updates。

1. 扫描官方 Mod、测试 Mod、发布包和已知外部消费者，确认每个旧 API 的真实使用者。
2. 对能一一映射的新能力，把兼容层压成薄 adapter；兼容层不得拥有新的状态机、Hook 或产品策略。
3. 对无法薄适配的旧执行器，优先移入可选 Compatibility DLL，使新玩家默认不加载。
4. `IFishingAutomationApi` 保持冻结与 Obsolete；完整旧 fishing executor 只在确认存在 legacy consumer 时创建。
5. 删除动作与全局版本系统重做绑定：先写废弃窗口和迁移说明，再在明确的破坏性版本边界移除。

完成门槛：已通过。默认第一方 AutoFishing 路径不创建 legacy service；当前玩家包以 dormant-shipped 方式包含唯一兼容 DLL；兼容测试与新 primitives 测试完全分开。未来删除或改为按下载裁剪仍需明确破坏性版本与包策略。

### Phase 3：继续把功能策略产品化

AutoFishing 已完成受管 Advanced ProductNative 迁移、独立验收与 D.5 产品减重；玩家源码从 23 文件 / 6,266 物理行 / 5,765 非空行下降到 22 文件 / 5,393 物理行 / 4,964 非空行。该结构性下降来自删除死代码/迁移脚手架、按需化 QA 观察并折叠单 owner 状态层，不改变身份、配置、22 Hook 或恢复边界。

Checkpoint D.5 通过后，独立准入 Review 已只接受 OneActionComplete。该决定不开放其他产品、G7 或通用 Advanced authoring；OneActionComplete 用 7 个玩家源码文件 / 772 行验证了小产品可直接拥有其薄 ProductNative 边界，不再采用“产品入口在 Mod、执行器在 GameBridge”的旧结构。

第二产品之后，`ActionSpeed` 已通过单独准入、源码迁移和修正包第三存档验收：六个玩家源码文件直接拥有九个原子 Hook、动作/动画/连续使用状态与恢复；旧 `IActionSpeedApi` 只保留 demand-inactive Compatibility。随后 FishBreedingAssistant 与 AnimalHusbandryProgress 分别完成第四、第五产品迁移，MoreSaves 完成第六产品零 Harmony 拆分，ChestLocatorEnhancer、MoreEquipmentSlots、StrongPlantingGun、Zoom、Mine 与 DebugConsole 分别完成第七至第十二 ProductNative 物理拆分。除 MoreEquipmentSlots 新版已延期且事务 findings 仍开放外，其余十一产品保持各自的 verified/closed 结论。十二产品共享 Catalog 驱动的 Advanced SDK/package/Doctor/Manager/安装与零残留工具，但不新增 SharedNative。Fish/Animal 只共享一个精确的只读 item-title native owner；其余产品各自没有第二个真实产品消费者。

推荐顺序：

| 顺序 | 候选 | 原因 |
| ---: | --- | --- |
| 1 | `OneActionComplete` | 范围小，适合验证本地热键、Owner cleanup 和薄原生能力 |
| 2 | `ActionSpeed` | 已完成第三产品源码、包与修正第三存档验收 |
| 3 | `FishBreedingAssistant` | 已完成第四产品迁移；占位数据不作为生产权威，当前共享适配器包的标题与 owner 清理已通过 |
| 4 | `AnimalHusbandryProgress` | 已完成第五产品迁移、只读 UI/native close 与真实 owner 第三存档验收 |
| 5 | `MoreSaves` | 已完成第六产品：只拥有固定六/十二策略和 `archiveFileCount`，零 Harmony；官方存档文件、复制/删除和 UI 仍归游戏 |
| 6 | `ChestLocatorEnhancer` | 独立 Review 唯一准入的第七产品；一条 inventory-query Postfix、配置与精确 owner 属于 ProductNative，旧 ABI 执行器进入现有 Host；聚焦门、`203127` 行为/停用、`210424` 双循环重入、提交范围纠偏与最终精确 HEAD `224200` 均通过 |
| 7 | `MoreEquipmentSlots` | 第八产品物理拆分已实现，但新版 1.0.0、Product-v3 迁移与发布已延期；Review 0016 的五个事务 P1 保留为下版本输入。0.5.5 只保留旧 ABI 0..24/Compatibility Host 对精确 Workshop 0.3.1-dtmapi 的兼容 |
| 8 | `StrongPlantingGun` | 已 verified/closed 的第九产品；固定 seed/film/fertilizer 三槽、五个原子 Hook、函数容量恢复和一次 SaveLoaded 已反序列化背包枪整理均归 ProductNative。直接成功/失败回滚、当前 DLL `101757` 行为/标题/JSON 重入/真实 owner/Loader 清理及独立收口均通过；旧 API 仅为无 retained binary 的 Frozen 警告壳，不新增 Host executor |
| 9 | `Zoom` | 已 verified/closed 的第十产品；ProductNative 直接拥有正交尺寸缩放、配置、原生派生状态恢复与一个 `SetEnvCamera` postfix，旧 CameraView/CameraZoom ABI 执行器进入现有 Host；35-MemberRef exact gate 和 `221902` 分辨率/全屏、失败传播、标题/Loader 恢复通过 |
| 10 | `Mine` | 已 verified/closed 的第十一产品；静态内容留在官方 JSON，周期、电力、产出、恢复和调度归 ProductNative；公开发布仍等待独立贴图并取消运行时 2x |
| 11 | `DebugConsole` | 已 verified 的第十二诊断产品；UI、私有动作、输入 Hook 与事务生命周期已离开 mandatory Runtime，未来视觉/UX 重写另立任务 |
| 消费者/API 研究 | `AutoHarvest` | 不是计划发布的第一方产品；只保留为外部 Mod 需求和单目标 harvest API 研究输入 |

每个功能必须经过同一检查表：

1. 找出玩家策略、原生操作、共享仲裁和测试代码各自的 owner。
2. Mod 保留配置、热键、决策、产品状态和单产品原生实现；只有已有独立复用证据的最小原生能力才留在共享 GameBridge。
3. 普通 Mod 优先读取本地 input snapshot，不为简单热键创建平台长期注册根。
4. 保持 Mod ID、配置、版本、Workshop 身份和玩家可见行为不变。
5. 禁用、移除、返回标题和进程退出时验证 owner 根、事件、输入、lease 和 transient 清零。
6. 仅在涉及原生 Hook、UI 或存档行为时运行对应游戏 Smoke/手测；纯目录与文档迁移不扩大运行时矩阵。

### Phase 4：公开 API 消费者审计与收缩

> 2026-07-23 收口：四个 CustomEntity 接口已降为 ABI 保留的 `Experimental/Frozen` 并只由 Core `DTMAPI` provider 提供；AutoHarvest 不再把 `IInstantSaveDebugApi` 当作普通生命周期；UI/Diagnostics/Workshop/Content/Translation 的 owner、线程与 stale-owner 语义已明确并由聚焦 Unit 覆盖。没有扩展 C# CustomEntity，也没有实现第一方 AutoHarvest。详见 [公共 API consumer/owner/thread/cleanup 审计](../reviews/api/2026/20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md)与 Update `20260723-0005`。

公开契约按以下状态统一登记：`Stable`、`StableCandidate`、`Experimental`、`Internal`、`Deprecated`、`Proposed`。

1. 从 `ExperimentalGameBridge.cs` 和 `CustomEntities.cs` 开始，逐项记录第一方、测试和外部消费者。
2. 只有真实普通 Mod 消费、语义稳定且能长期兼容的能力才进入 `StableCandidate`。
3. 仅第一方产品使用的 primitives 默认保持 internal，不因“能公开”而公开。
4. 没有消费者的实验 API 优先 internalize、延后实现或在版本边界退役。
5. 每次新增公开类型都必须说明：owner、线程/帧语义、清理规则、兼容承诺和无消费者成本。

完成门槛：公共 API 矩阵能解释每个实验区域为何存在；公开声明数量不再因第一方功能拆分而自动增长。

### Phase 5：按能力收缩 GameBridge

只有在 QA 与 Compatibility 物理移出后，才重新测量 GameBridge。先做按消费者激活和源码边界，再根据装载证据决定是否拆 DLL。

可能的逻辑能力域：

- 基础生命周期与输入帧桥接
- Camera
- Fishing
- UI / Equipment / Save
- Content / CustomEntities / Audio

2026-07-19 修正后，这些不再默认全部属于一个强制装载的 GameBridge。CustomAnimals/Audio 等领域 schema 可成为受 Core 管理的可选官方内容宿主；Fishing/Equipment/Save 等单产品原生实现可进入高级受管产品 Mod。共享 GameBridge 只保留已证明的多消费者原生能力和基础生命周期适配。

每个能力域的 inactive 门槛：无 Hook 安装要求、无 updater、无 session/lease/root、无每帧查询、无重复状态发布。只有当物理 DLL 拆分能实际降低启动、装载内存或玩家包复杂度时才执行；否则保留单 DLL 内的惰性模块更稳妥。

### Phase 6：收缩 Bootstrap 与诊断 UI

Bootstrap 最终只保留：BepInEx Entry、帧源、基础输入采样、Runtime 创建与关停。

- DebugConsole 的反射 UI 和产品配置迁入第一方诊断 Mod 或可选诊断程序集。
- Title manager 可以作为平台支持壳，但不得继续吸收新的功能实现。
- 调试能力同样先按 Platform / SharedNative / ProductNative / ContentOwner 分类：已证明多消费者共享的原生诊断能力可通过窄 GameBridge capability 获取；单产品诊断原生实现等待受管 Advanced CodeMod 通道，不把生产原生访问散落到 UI。

### Phase 7：明确生态与发布分类

管理器、文档和发布脚本应区分：

| 类型 | 使用 DTMAPI API | 获得 Owner cleanup | 热禁用预期 |
| --- | --- | --- | --- |
| Strict CodeMod | 是 | 是 | 按契约支持；程序集更新仍需重启 |
| Advanced CodeMod | 是；只可通过 SDK 和 tracked reference policy 按受管契约引用 Unity/Harmony/游戏程序集。G2 对通用通道只证明 synthetic fixture；八个真实产品均由各自独立准入与证据授权 | DTMAPI roots 有；直接原生副作用须由 Mod 声明/清理 | 默认重启；只有明确证明清理后才谈同进程停用 |
| ContentPack | 间接 | 由宿主拥有 | 由宿主定义 |
| Optional Content Host | 是 | 是 | 无消费者不激活；代码更新默认重启 |
| First-party Product Mod | 是 | 是 | 按 strict/advanced 分类；不是因为第一方身份就把原生代码塞回基础 Runtime |
| External BepInEx Plugin | 否 | 否 | 默认要求重启 |

External BepInEx Plugin 可以复用 DTMAPI 安装带来的 BepInEx 环境，但不能因此算作 DTMAPI Mod。除非作者选择迁移，否则 DTMAPI 不为它扩充 API，也不承诺清理其静态字段、Harmony patch 或 DLL 根。

## 5. 验证与度量账本

每个阶段只采集与该阶段有关的证据，避免把长期钓鱼 soak 变成所有重构的前置条件。

| 维度 | 推荐指标 |
| --- | --- |
| 发布包 | DLL 列表、包大小、hash、QA/Compatibility 是否存在 |
| 装载 | 普通玩家场景实际加载的程序集与关键类型 |
| 启动 | Entry 次数、启动耗时、重复初始化与残留进程 |
| Inactive | Hook requirements、updater、session、lease、owner root、每帧查询 |
| 生命周期 | disable/remove/title-return/shutdown 后 event/input/API/config/root 清零 |
| 热路径 | Update 分配、日志字节、字符串/集合创建、状态发布频率 |
| 长期趋势 | Mono/Unity/process memory、Gen0/1/2、DTMAPI 根与计数斜率 |
| API | Stable/Experimental/Internal/Deprecated 数量与真实消费者 |
| 源码 | 各程序集物理行数，仅作结构趋势参考 |

验证按改动风险选择：文档只跑文档治理、链接和 `git diff --check`；产品源码切片只跑受影响的 build、source/unit、package、owner 与 ABI 定向门；修改 native Hook、恢复或玩家行为时，在最终相关集成点追加对应存档的有界短 Smoke。“一次有界验收”表示一份干净最终候选的最小充分证据，不是启动配额；可修复失败先通过聚焦门，再按需重跑同一最小 Smoke。完整 Release suite 只留给最终相关集成或发布边界；只有短时分配/动画指标回退或任务本身处理长期趋势时，才要求 L0-L5、10/30 分钟窗口或 100/500 循环。失败后的重试遵循文档治理中的比例化规则，不另建 checkpoint 收据。

## 6. 近期检查点

### Checkpoint A：正确性闭环

完成 Phase 0 的加载版本、诊断字节上限和零警告修复。此检查点不拆程序集。

### Checkpoint B：QA 依赖地图

**已完成并由 Batch 4 G1-G9 supersede。** 已产出依赖/ownership inventory、QA seam、项目引用与发布装配边界；后续只维护当前债务，不重新执行同一前置审计。

### Checkpoint C：QA 第一批物理迁移

**已完成其原始范围并由 Batch 4 G1-G9 验收。** 通用 harness、report、JSON、趋势探针和场景已进入可选 QA 程序集，开发暂存与普通玩家包排除已有机器合同。剩余 production seam 与单产品 QA rehome 是后续有界工作，不是再次开始 Checkpoint C。

### Checkpoint D：Advanced 通道与 AutoFishing 反向迁移

**已完成。** Phase 0 correction、G2 synthetic fixture 与 AutoFishing 的 G3/G4、相关 G5/G6 已分别验收；历史次序仍记为先完成 AutoFishing，再推进 G0-G7 后续边界。该结果只证明 AutoFishing，不准入第二真实产品、G7、通用 Advanced authoring 或 0.5.5 发布。

### Checkpoint D.5：AutoFishing 产品内部减重

**已完成。** 在同一产品、身份、配置、Workshop 和兼容边界内，删除无消费者迁移脚手架，把 QA-only 诊断改为显式激活的有界观察，折叠单 owner 的重复 session/router/lease/transaction/cache 层，同时保留 22 个原子 Hook 的安装失败关闭、重复结算防护、原生状态恢复和 F6/title/save/shutdown 清理。

结果：玩家 `src` 的物理行从 6,266 降至 5,393（减少 873，13.93%），非空行从 5,765 降至 4,964（减少 801，13.89%）；DLL 从 155,136 字节降至 131,072 字节（约减少 15.5%）；普通包仍不含 QA。聚焦 SDK/Unit/QA/static gates 与第五存档短启停/标题/再载入/恢复回归是本检查点的边界。未重跑完整 Release、L0-L5 或长测；该检查点完成时，0.5.5 仍暂停发布。

### Checkpoint E：第二个功能 Mod 模板

**已完成有界迁移验收。** Admission authority 只准入 `OneActionComplete`：保留 UniqueID、Workshop `3742763540`、版本 `1.1.2-dtmapi`、配置默认值与 F11；用产品自有的资源/能量/燃料/饲料实现和两个原子 Postfix 替代旧 Strict shell + GameBridge executor。新产品不消费 `IActionCompletionApi`；旧 ABI 仅留在 demand-inactive Compatibility，并在两种加载顺序下于物理安装前 fail-closed。

两产品的 ConfigMenu glue、owner/lifecycle、Harmony owner、SDK/package、Doctor/Manager 与 QA seam 比较已完成。唯一新增 Platform 复用是一套 Catalog 驱动的 Advanced validate/build/pack/package 与通用零残留循环，产品只保留薄包装和产品语义检查；两个产品没有共同 native owner，因此 SharedNative 提升为零，也没有创建新公共 API。

### Checkpoint F：第三个功能 Mod

**已完成。** Admission authority 只准入 `ActionSpeed`：保留 UniqueID、Workshop `3742763309`、版本 `1.3.4-dtmapi`、配置键/默认值与玩家策略；产品自有九个原子 Hook、Animator/计时/连续使用/自动装瓶状态和恢复边界，旧 ABI 留在双顺序 fail-closed Compatibility。SDK policy、包、Catalog、Doctor/Manager、安装/卸载与零残留继续使用同一套通用 Advanced 工具，未复制第三套构建器。

第一次第三存档短运行加载了旧 Strict OfficialLocal 来源，随后 G6 标题流程提前打断 G5；第二次加载真实 Advanced DLL并在全局类型名错误处零 patch 安全回滚。修正后的 `GAME-SMOKE/20260722-141220` 通过 Tool/ConfigApply/Interaction、标题/恢复及九目标真实 owner 清零，并同进程关闭 OneActionComplete 三项证据缺口。

### Checkpoint G：FishBreedingAssistant 第四产品

**产品行为、owner 边界与共享查询生命周期均已收口。** 产品拥有一个标题 Postfix 和鱼卵格式规则；空查找表不再执行，旧 `IItemTooltipApi` 三 Hook 执行器冻结为 Compatibility。原始 ProductNative fallback 在 `145154` 通过；Animal 成为第二消费者后，native item-title 查询移入最小共享适配器，修正包在 `180502` 再次显示 `鱼卵 (鱼)`，并完成真实一补丁 owner 停用清零。后续聚焦 Unit 又通过真实 `SetEnvCamera` callback 证明共享缓存失效与 demand 释放。

### Checkpoint H：AnimalHusbandryProgress 第五产品与共享边界

**产品行为与 owner 边界已完成第三存档验收。** 产品拥有四个原子 Hook、只读缓存进度行、缓存 clone 目标、一次性 next-frame guard 与精确 session 清理；旧 `IAnimalViewerApi` 冻结为 Compatibility。修正包 `180502` 验证了当前四补丁/三目标、可见 `羊毛脂 0/100` 行与原生风格 clone、无动物状态写入、native close 后产品行/clone 清零、Loader exact-owner 停用、恢复与退出。Fish/Animal 比较只提升 `IItemDisplayNameApi`；其他相似 glue 不提升。

### Checkpoint I：MoreSaves 第六产品

**拆分真实有效并完成聚焦纠偏。** ProductNative 只拥有固定 enabled `12` / disabled `6` 策略、`GameManager.archiveFileCount` 写入、配置和生命周期，安装零 Harmony；官方存档文件、发现、复制/删除和 UI 仍归游戏。`150215` 的第三存档、标题、真实停用、36 路径恢复和退出证据保留。后续审计修复了 0.5.5 Host 双遗漏假绿、冻结 ABI 未注册/冷态禁用/混合 owner 语义、健康态逐帧订阅、版本三轴和两个组件事务窄窗口；这些源码/工具修正只需聚焦门，不重跑游戏。

### Checkpoint J：ChestLocatorEnhancer 第七产品

**已 verified/closed。** ProductNative 只拥有一个 `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)` Postfix、widening 策略、配置与生命周期；游戏仍拥有 inventory 实例、CountItem/CostItem、UI 和存档。旧 `IChestLocatorEnhancerApi` 保留为 Experimental/Deprecated/Frozen 薄代理，重执行器进入现有单一 Host。`203127` 证明 `0 -> 3 -> 1` 原生计数/扣除、标题恢复与 real Loader exact-owner 清零；`210424` 证明同一进程两次第三存档重入期间只保留一个 exact owner；提交范围纠偏补齐真实 Harmony 双 owner 顺序、可执行 traversal、分配/日志和 Catalog 驱动事务检查，最终 `224200` 将修正后的 DLL 与 Runtime `7ace68260cb1` 绑定并复验行为、停用、标题恢复和退出。七产品横向比较只确认现有 Catalog/SDK/package/Doctor/Manager/零残留工具为 Platform；没有第二个真实产品共享 Chest native owner，因此 SharedNative 提升为零。

### Checkpoint K：MoreEquipmentSlots 第八产品准入

**物理迁移已实现；本节原先的 verified/closed 发布结论已由
Review `20260730-0016` 和用户延期决策取代。** Review
`20260723-0009` 只允许 `DTMAPI.MoreEquipmentSlotsMod` 完成一次高风险、
行为保持的 ProductNative 迁移。产品固定三槽并拥有 protected
sidecar/journal、额外槽排序、AccessoriesBar clone、配置与四个精确 Hook；
游戏继续拥有装备列表/效果、背包与邮件、原生护盾优先级及基础 UI。旧
`IEquipmentSlotsApi` 与六个 DTO 已以不改签名方式标记
Experimental/Deprecated/Frozen，0..24 语义和重执行器/冷态恢复后端只留
在现有 Host。

背包/邮件/失败三态、双顺序/残留 owner fail-closed、真实停用、同进程
UI/监听清零、第三存档 saved-item `053248` 与 ProductNative-v3 管理恢复
`053342` 证据保留。后续修复引入明确 Working/Committed，并隔离
`GameplayMutation` 与可持久重试的 `OwnerRecovery`/`OrphanRecovery`。
真实 Host focused fixture 覆盖普通 Equip/Unequip、盾伤、保存提升、标题
回档与两个崩溃窗口；`155216`/`155344` 证明真实第三存档在已覆盖的
equip/title/cold 路径不变，`161422`/`161536` 证明已覆盖的隔离
equip/unequip native-save/promotion/cold 路径。Review `20260724-0006`
发现 `NoNativeSave` 仍可错误接受 root override，fixture 未拒绝 reparse
point，真实 ProductNative 尚缺 damage/break/replace/no-save unequip 矩阵。
`154906`、`155811`、`161030` 仍是 non-acceptance，历史
PlayerSaveRestored 不倒推为无保存证明。

Review `20260724-0007` 又将最终证据收窄到“正常保存的非空盾牌”。
当前提交绑定的 `222231` 正常保存耐久 80 的 `box_hat`；`222329` 执行
80→40 的未保存真实受击；`222423` 先证明回档到 80，再执行未保存卸下/
重装、替换/卸下替换物和破碎；`222516` 冷读得到相同耐久-80 committed
盾牌、native 0/committed 1/logical 1、无 candidate/journal，并清理隔离
fixture。这些证据只关闭当时执行字节的 Working/Committed 回归矩阵，
不再构成新版 `1.0.0`、Product-v3 迁移或 Steam 发布权威；五个后续事务
P1 仍留待下一版本。

### Checkpoint K.5：九产品横向重量与共享边界

Working/Committed 修复后的冻结九个 ProductNative `src` 合计 81 个 C#
文件、21,828 物理行、20,114 非空行：

| 产品 | 文件 | 物理行 | 非空行 |
| --- | ---: | ---: | ---: |
| AutoFishing | 22 | 5,393 | 4,964 |
| OneActionComplete | 7 | 879 | 802 |
| ActionSpeed | 6 | 1,825 | 1,612 |
| FishBreedingAssistant | 5 | 377 | 342 |
| AnimalHusbandryProgress | 7 | 1,192 | 1,088 |
| MoreSaves | 3 | 425 | 391 |
| ChestLocatorEnhancer | 8 | 1,672 | 1,524 |
| MoreEquipmentSlots | 12 | 7,205 | 6,715 |
| StrongPlantingGun | 11 | 2,860 | 2,676 |

五个 mandatory Runtime 项目的实际编译输入为 66,341 物理 /
59,437 非空行，五 DLL 合计 2,135,552 字节；相对八产品基线
67,224 / 60,195 / 2,155,520，分别减少 883 / 758 / 19,968。
其中 mandatory GameBridge 为 28,592→27,492 物理行、
25,324→24,356 非空行、851,456→825,344 字节。

把同样功能的产品 DLL 计回后，mandatory+products 从
八产品 `84,897 / 76,413 / 2,592,256` 变为九产品的
`88,169 / 79,551 / 2,636,800`；Compatibility Host 为
397,312 字节。因此结论严格限于“默认加载 Runtime 更小”，不能写成
仓库、下载、安装或总交付减重。

横向比较没有发现新的共同 native owner。Catalog、SDK/package、
Doctor/Manager、配置菜单注册、owner 生命周期和零残留循环仍是
Platform 复用；同一个 Compatibility Host 是冻结 ABI 装配边界，不是
SharedNative 证明。当前唯一有两个独立真实产品消费者并共享同一 native
owner 的 gameplay adapter 仍是 Fish/Animal 使用的 Experimental
`IItemDisplayNameApi`。

### Checkpoint L：StrongPlantingGun 第九产品准入与实现

**已准入、已实现并 verified/closed。** Review `20260724-0003` 只允许
`DTMAPI.StrongPlantingGunMod` 进行一次行为保持的 ProductNative 拆分：
产品固定三槽并拥有 seed/film/fertilizer 策略、原生 Farming Gun 容量
快照/恢复、反射缓存、配置和五个具体 Harmony patch（两个构造函数、
一个工具使用和两个 UI 转移）。游戏继续拥有序列化
`ItemFarmingGun.inventory`、背包/容器物品、PlantBasin 交互和原生 UI
对象。

当前只有 tracked legacy/no-Type 源码这一名真实消费者；它仅引用
Abstractions，并不是 SDK-verified Strict artifact。Catalog 无 Workshop、
`publishedVersion` 或 retained artifact，因此只冻结
`IStrongPlantingGunApi` 与三个 DTO 的类型/成员和警告，不新增第九套
Compatibility Host 执行器；实现扫描未发现真实旧二进制消费者，未来若
发现仍必须停下重审。
mandatory GameBridge 毛候选为 1,019 物理 / 895 非空行，最终只能按实际
默认加载 Runtime 净变化宣称减重。

Update `20260724-0001` 已完成原子迁移：mandatory GameBridge 降为
27,476 / 24,341 / 825,344，五个 mandatory Runtime 合计降为
66,137 / 59,241 / 2,132,992；新产品为 2,860 / 2,676 /
46,592。直接 Unit 证明真实静态 archive 路由及中途扫描失败后的容量和
owner 全量回滚；`GAME-SMOKE/20260724-101757` 证明原生保存、标题返回、
一次第三存档 JSON 重入 `3/3/3`、真实五 owner 与 Loader exact-zero
清理。独立收口无 P0/P1/P2；该数据只支持默认加载体量下降。

Mine 虽有 1,468 / 1,315 行毛候选，但仍跨原生电力、运行周期、archive
time、产品 RNG/存储、全局 recipe/tech 与 renderer 生命周期；虚假可调
power、Enabled 未阻止注册、未消费的 runtime-mineral 开关和缺失原值恢复
四项未闭合，继续 `split-decided` / `PrototypeBlocked`。Checkpoint L
当时记录精确九产品；StrongPlantingGun 保持 verified/closed，
MoreEquipmentSlots 的物理前态保留但行为验收已由 Review
`20260724-0006` 重新打开。

### Checkpoint M：Zoom 第十产品准入与实现

Review `20260724-0005` 只准入 `DTMAPI.ZoomMod`。产品的 native 状态持有者
是 `DolocAPI.mainCamera.orthographicSize` 与产品自己的 scale/vanilla
snapshot；游戏继续拥有 camera、follow/clamp、环境、背景、雾和全景。
ProductNative 只允许一个 `DolocAPI.SetEnvCamera` non-suppressing postfix。

旧发布 Zoom 是当前 `ICameraViewApi` 的唯一真实产品消费者；新产品不消费
该 API，旧 CameraView 和 obsolete CameraZoom 执行器迁入现有
dormant-shipped Host，mandatory GameBridge 只留薄代理与 owner/demand
协调。DebugConsole 因缺少现有 Advanced modal/input seam 不准入；Mine
继续 PrototypeBlocked。该切片不新增 Host、公共 API 或 SharedNative，
实现和第三存档 NoNativeSave 短验收由 Update `20260724-0002` 跟踪。

**已实现并 verified/closed。** 最终 ABI 门按真实 retained Zoom DLL 锁定
35 个 CameraView MemberRef；Product-first/Compatibility-first、真实
Harmony owner、原子失败回滚、exact unpatch、Frozen/Obsolete 元数据、
Catalog、SDK/package、Doctor 与零残留聚焦门均通过。
`GAME-SMOKE/20260724-180233` 证明 Product Hook `1` / Compatibility Hook
`0`、Loader instance/callback/Hook/roots 全零，以及 archive/sidecar
NoNativeSave、Doctor、source/profile/QA 恢复和退出；但其
2x=`33.75`、4x=`67.5`、真实 `SetEnvCamera` 后=`270`、配置禁用
恢复=`67.5` 正是错误基线叠乘证据，因此改判 non-acceptance。
`175944` 保留为已修正 `CurrentRoom` 下一帧时序的 non-acceptance。

最终 `GAME-SMOKE/20260724-221902` 绑定
`136a7278ed79614d7dffe259d21789b02329d869`，在 2x/4x 主动触发原生
分辨率/全屏刷新后，证明 direct 1x、Max→1、配置禁用、标题与 Loader
停用均恢复原始 `16.875`、精确 `camSize`/xRange/yRange，并把
instance/callback/Hook/roots 清到零；事务式失败不再被成功状态覆盖。

十个 ProductNative `src` 合计 91 文件 / 23,347 物理 / 21,526 非空行；
五个 mandatory Runtime 为 65,534 / 58,710 / 2,111,488，GameBridge 为
26,658 / 23,602 / 798,208。相对九产品基线，默认加载 Runtime 减少
807 / 727 行和 24,064 字节。产品 DLL 合计 531,456；mandatory+products
为 88,881 / 80,236 / 2,642,944；Host 为 437,760。结论只限默认加载
Runtime 下降，不宣称仓库、下载、安装、总交付或全部产品启用后减重。
十产品横向比较未发现第二个真实产品共享 Zoom native owner，因此新增
SharedNative 为零。

## 7. 决策与停止条件

继续拆分前使用以下规则：

- 通用作者能力才进入 Core。
- 多消费者共享的原生对象、Harmony 和反射访问留在 GameBridge；单产品专用实现留在高级受管 Mod。
- 玩家可见策略、配置和热键进入 Mod。
- 只为验证存在的代码进入 QA。
- 只为旧消费者存在的代码进入 Compatibility，并设置退役条件。
- 没有消费者且没有明确近期用途的实验能力不继续扩建。

遇到以下情况应暂停当前拆分并重新设计：

- QA seam 被迫成为普通 Mod 的公开 API。
- 两个以上产品开始复制同一个 native owner 的补丁、反射缓存或冲突仲裁，而项目仍拒绝提炼共享能力。
- 为拆 DLL 新增的装配与启动复杂度高于可测量收益。
- 迁移同时改变身份、版本、配置和行为，导致无法判断回归来源。
- inactive 场景仍安装功能 Hook、创建 session 或持续执行每帧查询。

## 8. 当前状态判断

| 项目 | 当前判断 | 下一步 |
| --- | --- | --- |
| Owner Lifetime | 主体重构、Phase 0 correction、G2 synthetic fixture、十一个产品的物理 owner 与行为 closeout、ItemDisplayName SharedNative 生命周期、Compatibility Host dormant/resident 边界及 Phase 1/4 helper owner/thread/stale 语义均已完成。MoreEquipmentSlots 已实现但发布延期；Mine 与 DebugConsole 已通过最终独立验收；0.5.5 当前不存在通用 AudioHost 证据 | 冻结十一个 closed 产品与一个 deferred 产品的真实状态；第十三及以后产品继续需要独立准入；CustomAnimals/AudioReplacement 可选基座留到 0.5.5 后下一版本 |
| Zoom | 第十真实 Advanced 产品 verified/closed；native owner 是 orthographicSize、CameraController 派生状态与一个 SetEnvCamera reset 点，旧 Camera ABI 有一名 retained 产品消费者；35-MemberRef、双 owner、focused package/Doctor、`221902` 分辨率/全屏及 Loader-zero 通过 | 冻结产品边界；不提升 SharedNative |
| AutoFishing | Advanced ProductNative rehome 与 D.5 减重已完成；玩家 `src` 为 22 文件 / 5,393 物理行 / 4,964 非空行，QA 仍排除在玩家 DLL/包之外；第五存档深层产品持有状态清零与行为恢复通过 | 冻结该有界基线；不为 0.5.5 重跑全量/长测 |
| OneActionComplete | 第二真实 Advanced 产品当前为 7 文件 / 879 物理行 / 802 非空行，已完成两 Hook 原子安装、双顺序互斥与第三存档完整续验证据 | 冻结已验证基线；不把执行器或 QA 状态移回 mandatory Runtime |
| ActionSpeed | 第三真实 Advanced 产品已完成 6 文件 ProductNative、九 Hook 原子安装、双顺序互斥、SDK/包/Catalog/安装、QA seam 与修正第三存档验收 | 冻结已验证基线；后续 additive API rebuild 不等于行为重验 |
| FishBreedingAssistant | 第四真实 Advanced 产品完成一 Hook/格式/Compatibility 迁移；空数据表退出生产，修正包在 `180502` 通过标题与 exact-owner 验收，Hook-ready fail-closed 与 SetEnvCamera 静态 callback Unit 关闭共享查询 EnvironmentReset 缺口 | 冻结产品行为/owner 基线；不把数据表或标题 Hook 提升为平台能力 |
| AnimalHusbandryProgress | 第五真实 Advanced 产品完成四 Hook、clone 生命周期、Compatibility、SDK/package/Catalog/QA owner 迁移；`115821` 进一步通过三只动物连续切换，每次均观察到至少一个新 render receipt 并完成一次 next-frame guard；每个 `RenderAfterShow` 初始隐藏写入并最多重置一个 guard，最终 receipt sequence 为 6，随后 native close、标题恢复与 exact-owner 清零 | 冻结 ProductNative 缓存刷新；不得把单产品 UI 反射/clone 层提升为 SharedNative |
| MoreSaves | 第六真实 Advanced 产品完成零 Harmony ProductNative 拆分；`150215` 证明十二槽、第三存档、标题、真实停用到六槽、36 路径恢复和退出；聚焦纠偏关闭冻结 ABI、按需 retry 与安装拓扑缺口 | 冻结固定六/十二与官方存档 owner 边界；不把 Save UI 或任意槽数提升为共享能力 |
| ChestLocatorEnhancer | 第七真实 Advanced 产品完成一 Hook ProductNative 拆分；聚焦门、`203127` 原生 Count/Cost/real Loader 清零、`210424` 双循环重入、提交范围纠偏与最终精确 HEAD `224200` 均通过 | 冻结 verified/closed 边界；保持 inventory/UI/save 为原生 owner，不把单消费者 traversal 提升为 SharedNative |
| MoreEquipmentSlots | 第八真实 Advanced 产品已实现，但新版 1.0.0、Product-v3 迁移、验收和 Steam 更新延期；历史 Working/Committed 与玩家证据只约束其执行字节，Review 0016 的五个事务 P1 未关闭。旧 Workshop `0.3.1-dtmapi` 仍是 0.5.5 兼容对象 | 不发布新产品；保留固定三槽源码作下版本输入，Runtime 仅以冻结旧 ABI 0..24/Compatibility Host 支持精确旧 DLL |
| Compatibility | 九个精确旧发布 DLL 分别消费九条冻结 ABI；所有重执行器位于同一 dormant-shipped optional Host，普通 GameBridge 不静态引用，首次旧 ABI 调用或明确冷态恢复 demand 才精确加载；Mono 留驻但 transient 与 backend owner 状态可清零 | 保持 provider/ABI/owner/fail-closed 与唯一 Catalog authority；只宣称默认加载下降，不宣称下载减重，不删除冻结 ABI |
| Oil | G1 已投影为 DLL-free、`content-only-decided` 的官方 JSON ContentPack；weight 25、fuel 1500、售价 45/买价 240 仍是 prototype economy | 保持 `PrototypeBlocked`；不加 API/Hook/Host，经济推广另审 |
| Mine | 第十一真实 Advanced 产品 verified/closed；静态 item/equipment/recipe/group 走官方 JSON，运行周期、archive time、电力/经济、恢复与调度归 Mine ProductNative，独立行为、恢复重试与当前 SDK 包字节验收通过 | 技术边界冻结；独立贴图并取消运行时 2x 后才可公开发布，不把单消费者 machine engine 稳定为平台或 Content Host/API |
| DebugConsole | 第十二真实 Advanced 诊断产品 verified；产品 UI/动作/Hook 与冻结 0.3.1 Compatibility 路线分属独立 owner，SaveLoaded、关闭失败、cleanup debt、标题与 Loader 清理通过 | 冻结行为等价边界；视觉/UX 重写另立任务，不开放通用 native command API |
| CustomAnimals | JSON/PNG/WAV 路线已证明可复用且无物种硬编码，但领域桥仍在基础 GameBridge | 0.5.5 后下一版本设计为 DTMAPI 官方维护、可随 Runtime 分发的按需 G7 内容基座；无动物内容时不加载实现、不装 Hook、不逐帧运行 |
| AudioReplacement | 当前短 SFX/Manbo 与 AnimalVoice 已有真实窄消费者，但不存在已准入的通用 Audio Host，BGM 仍阻断 | 0.5.5 后与 CustomAnimals 一起研究按需 G7 基座；无音频内容时不加载实现、不装 Hook、不逐帧运行，具体映射仍归 Manbo 等内容产品 |
| AnimalPack | 当前只有 Catalog/Phase 0 的无 DLL 内容产品身份，Hatch/Mole/Drecko/OilFloater 仍是延期输入 | 0.5.5 后合并四个物种，验证单 Mod 多物种官方 JSON 并形成作者教程；物种、概率、经济和产物不进入 DTMAPI 基座 |
| Smoke/性能探针 | AutoFishing 深层短回归及六个后续产品的外部 owner 观察均已通过；死输入诊断、ActionSpeed eager summary 与 Core 旧字符串输入/fatal-window orphan 链已清理 | 不建立共享 QA ABI；保留中性 QA Host/fault seam/ISSUE-010 breadcrumb 与外部 fatal-window 捕获 |
| 公开 GameBridge API | Phase 4 消费者/provider/owner/thread/stale-owner 尾项已关闭；CustomEntity 保持 Experimental/Frozen Core-only，AutoHarvest 不再依赖 Diagnostic save state；Chest 与 EquipmentSlots 旧 API 均为 Experimental/Deprecated/Frozen；不存在已声明或准入的 `IProtectedStorageApi` | 冻结当前 ABI/警告/单 provider 边界；不扩展 C# CustomEntity，不实现第一方 AutoHarvest；通用受保护存储 API 延期，旧 EquipmentSlots 0..24 语义只作精确二进制兼容 |
| 版本系统 | Runtime release/API、binary/file 与 assembly compatibility 三种投影已由 `tools/release/dtmapi-runtime-version.props` 统一；G2 author-project schema 2 已用 `targetDtmApiVersion` 明确 API target 语义，schema 1 保留历史兼容字段 | 后续不得把 release/API、file/binary 与 assembly compatibility 重新合并，也不得把 G2 policy 当成任意游戏 build 的承诺 |
| 0.5.5 发布 | 技术 release gates 与产品减重分开；采用 owning Update 的冻结 Runtime 制品并排除后续新版 MoreEquipmentSlots/Host 实验源码。唯一完整 Release、冻结候选包矩阵、Manbo 与精确旧 Workshop MoreEquipmentSlots 兼容均有证据 | 获得授权后同步并复核实际上传目录、解除 Catalog releaseStop、完成人工三语言与 Steam 上传；上传后复核仍单独执行，MoreEquipmentSlots 1.0.0 保持延期 |

## 9. 相关文档

- [功能性 Mod 与 DTMAPI 边界审查](../reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md)
- [公共 API 矩阵](../api/public-api-matrix.md)
- [旧 ABI 消费者与可选 Compatibility Host 审计](../reviews/code/2026/20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md)
- [生产 QA seam 与 Animal refresh 审计](../reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)
- [公共 API consumer/owner/thread/cleanup 审计](../reviews/api/2026/20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md)
- [G1 未决 native owner 与 Oil/Mine 设计审计](../reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md)
- [Mod Owner Lifetime 设计契约](../design/mod-owner-lifetime-contract.md)
- [Owner 平台依赖协调更新](../updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md)
- [Zoom 第一方产品 Owner Lifetime 更新](../updates/2026/20260711-0013-first-party-zoom-owner-lifetime.md)
- [AutoFishing Smoke 架构边界更新](../updates/2026/20260711-0005-autofishing-smoke-architecture-boundary.md)
- [SMAPI API 缺口、功能性代码过量与内容宿主审查](../reviews/api/2026/20260719-0011-smapi-api-gap-functional-surplus-and-content-host-review.md)
- [Batch 6 边界修正前置审查](../reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md)
- [AutoFishing 产品重量与统一配置复用审查](../reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md)
- [DTMAPI 0.5.5 发布前路线图](20260727-dtmapi-055-prerelease-roadmap.md)
