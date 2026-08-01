# 20260721-0004 AutoFishing 产品重量与统一配置复用审查

- Status: `recorded`
- Date: `2026-07-21`
- Scope: 当前第一方 AutoFishing 玩家侧源码、小神增强包 AFK 钓鱼对照、DTMAPI 统一配置菜单复用，以及 0.5.5 发布暂停后的下一执行边界
- Source: 用户要求先研究 AutoFishing 为何在拆出 Runtime 后仍然过重，并减少后续长测与全量测试
- Runtime impact: 本审查不修改 Runtime、产品 DLL、游戏目录、Workshop 状态或存档

## 1. 结论

1. 当前 AutoFishing 玩家 DLL 只编译 `products/first-party/AutoFishing/src`，不包含同产品的 `qa`。因此仓库中的 14,614 行不是全部发给玩家；玩家侧仍有 23 个 C# 文件、6,266 个物理行，问题确实存在，但必须与 8,348 行可选 QA 分开讨论。
2. 本地没有找到小神增强包的原始 C# 工程。现有权威输入是 `references/third-party-mods/小神增强包` 下的加密发布归档和既有研究记录。本轮只在临时目录中只读解包、反编译和计数；没有把第三方源码或二进制复制进 DTMAPI。
3. 小神包的 AFK 钓鱼直接专用实现约 227 个反编译物理行，另复用全包通用的 163 行 patch/config controller 和 85 行插件入口。它的玩法、配置、恢复和管理保证少于 DTMAPI AutoFishing，不能把 `227:6266` 当成功能等价比例；但整个 28 功能插件的反编译输出也只有约 5,876 行，这个数量级差距足以触发产品内部减重。
4. 设置面板不是主要重量来源。DTMAPI 已有统一的 `IDtmConfigMenuApi -> ConfigMenuRegistry -> TitleSettingsUi` 路径；AutoFishing 已经通过该路径注册设置，没有自画菜单。其设置注册、配置 DTO 和相邻 glue 粗算约 88 个物理行，只占玩家侧源码约 1.4%。
5. 主要重量集中在单产品原生编排：约 2,504 行 session/router/hook/lifecycle/contracts，加约 3,150 行 native access/cache/transaction/animation。Git 迁移差异进一步证明其中 14 个 Native 文件以 94%-99% 相似度从 GameBridge 直接迁入，当前合计 4,481 行、占产品 71.5%。Batch 6 完成的是所有权纠正，不是轻量化重写。
6. 0.5.5 冻结旧 ABI 的 fishing Compatibility 仍在 mandatory GameBridge 中，另有 9 个文件、4,164 行。它不是新产品引擎，也不能算进 AutoFishing 的 6,266 行，但仍构成玩家 Runtime 的二进制维护面；产品减重不能冒充兼容层已经退役。
7. 下一步应在同一个 AutoFishing 产品内增加 Checkpoint D.5，不创建 `AutoFishing Lite`，不新增 Runtime API、收据族或门禁体系。减重完成后，再显式准入一个第二产品；推荐候选仍是 `OneActionComplete`。两个真实产品都落地后才比较并提炼共同层。

## 2. 量化方法与边界

### 2.1 当前 AutoFishing

计数基于当前 tracked tree，排除 `bin`、`obj` 和生成文件；物理行包含空行，非空行单独列出。Author SDK 项目的 `sourceDirectory` 明确为 `src`。

| 范围 | C# 文件 | 物理行 | 非空行 | 是否进入玩家产品 DLL |
| --- | ---: | ---: | ---: | --- |
| `products/first-party/AutoFishing/src` | 23 | 6,266 | 5,765 | 是 |
| 其中 `src/Native` | 19 | 5,455 | 5,031 | 是 |
| `products/first-party/AutoFishing/qa` | 23 | 8,348 | 7,760 | 否；属于可选 QA 路线 |

当前 `Yuuka.DTMAPI.AutoFishing.dll` 为 155,136 bytes。DLL 大小不是运行时分配或 GC 结论，只用于确认玩家确实加载了一个相对重的单功能产品程序集。

另有 `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation` 的 9 个 C# 文件、4,164 个物理行（3,697 个非空行）随 mandatory Runtime 构建，用来守住 0.5.5 旧 fishing ABI。这是独立的兼容债务：它不会因为新产品禁用就从发布 DLL 消失，但也不能与新产品源码相加后声称 AutoFishing 自身有 10,430 行运行逻辑。

玩家源码按文件职责粗分如下；它是定位优先级，不是应按比例删除的 KPI：

| 职责 | 约物理行 | 判断 |
| --- | ---: | --- |
| 入口、配置、产品决策 | 612 | 玩家策略和少量注册 glue，大部分应保留 |
| Session、Router、Hook、lifecycle、内部 contracts | 2,504 | 重点检查重复状态层、跨程序集时代 facade 和单消费者路由 |
| Native access、cache、transaction、animation | 3,150 | 重点检查同一 native state 的重复镜像、事务包装与恢复路径是否可合并 |

最大的单文件集中在 `FishingPrimitivesService`、`FishingAnimationNativeCache`、`FishingNativeTransactionCache`、`FishingNativeStateCache`、`FishingPrimitiveHookRuntime` 和 `ProductNativeHelpers`。设置菜单不在这个集中区。

### 2.2 小神增强包对照

工作区和 `E:\Python_project` 同级项目中没有发现 `FishingAFK`/`DolocPlus` 原始 C# 源码；搜索结果只有本轮临时反编译输出、发布归档副本和 DTMAPI 既有研究文档。当前可复核的发布输入是：

- `references/third-party-mods/小神增强包/DolocTownEA_BepInEx_DolocPlusMod.7z`；
- 归档内 `DolocPlus.dll` 为 126,976 bytes；
- ILSpy 临时输出为 80 个 C# 文件、约 5,876 个物理行；这是反编译表示，不是作者原始源码行数。

AFK 钓鱼直接文件为：

| 临时反编译类型 | 物理行 | 角色 |
| --- | ---: | --- |
| `FishingAFKController` | 25 | 功能启停 controller |
| `FishingAFK` | 202 | 钓鱼状态、Harmony patch 与输入模拟 |
| **直接合计** | **227** | 只计算 AFK 钓鱼专用实现 |

此外它复用全包的 `PatchController`（163 行）处理 BepInEx bool 配置、Harmony patch/unpatch、冲突与日志，并复用 `DolocPlusMod` 插件入口（85 行）。这些是 28 个功能共享的基础设施，不能全部摊给钓鱼。

公平比较还必须保留以下差异：

- 小神路径负责满蓄力、等待/小游戏按键和空闲重抛等核心 AFK 行为；
- DTMAPI 产品另有可配蓄力、instant bite、skip minigame、动画倍率、独立受管 owner/package/Manager/Doctor 身份；
- DTMAPI 已验证 duplicate settlement 防护、失败关闭、F6 停用、标题/存档边界、输入与动画恢复；小神发布包不能提供同等证据；
- 小神 controller 的通用 unpatch 不等于 DTMAPI 对 session、lease、输入、动画和原生状态的精确恢复。

因此本对照只证明“当前层次值得逐项证伪”，不证明可以把 DTMAPI 实现直接压到 227 行，也不授权复制第三方实现。

## 3. 为什么拆出后仍然重

Batch 6 解决的是物理所有权：AutoFishing 的单产品 ProductNative 不再由 mandatory GameBridge 承担。它没有同时完成产品内部的简化。迁移为了先保持行为和证据，保留了原来跨 Runtime/产品/QA 边界形成的 service、adapter、router、session、cache、transaction、observation 和 facade 层；所有权正确了，层次并不会自动消失。基线到迁移提交的 Git rename 检测显示 14 个 Native 文件保持 94%-99% 相似度，4,481 行高相似迁移量就是这一判断的直接证据。

当前值得逐项验证的候选包括：

- `FishingLifecyclePublicationGate`：全仓库源码扫描只发现声明，没有调用者，是明确的首批删除候选；
- `ProductNativeHelpers`：跨文件实际使用集中在 `ResolveType`，其余泛用 resource/message/member/animator helper 目前只在本文件自引用或只定义；应先用编译和行为检查证实后，把约 300 行无消费者 helper 从玩家 DLL 移除；
- `FishingDecisionExecutionMode` 的 Legacy/Shadow 分支、`CompareShadow()` 与 diagnostics 中未调用的 shadow 计数：属于高置信迁移期脚手架候选；
- `FishingAutomationDiagnostics`、`FishingRuntimeSessionObservation`、`CaptureObservation()` 和大部分 visible-reel telemetry：生产侧维护，已发现的读取者主要或全部在产品 QA，应判断能否迁到 QA、按 QA 激活，或缩成一个有界诊断快照；
- primitives/service 与 native adapter/cache 同时维护同一动作状态的路径：检查是否可由一个产品 session 和一个原生恢复对象拥有；
- router、lease、callback、transaction 的单消费者包装：先证明异常、重入、重复结算或恢复语义，再决定保留、内联或合并；
- 固定 fallback key 的重复解析可静态预解析，但它只是小修，不是 6,266 行的主因。

不得为了行数删除的行为包括：唯一 Harmony owner、进入前检查、失败关闭、重复结算防护、原生状态恢复、F6/title/save/shutdown 清理、输入与动画恢复，以及当前玩家可见配置和 Workshop 身份。旧公开 fishing ABI 的 0.5.5 兼容承诺继续由 Compatibility/Runtime 权威拥有，不能在产品减重中顺手破坏。

本轮不设硬行数指标。合理目标是产生可解释的结构性下降，并让每个保留层都能回答“它保护哪一个已知行为或失败模式”；若删除三分之一左右成为自然结果，可以记录，但不能反过来驱动不安全合并。

## 4. 统一配置菜单复用结论

DTMAPI 已经有可复用的统一注册链：

1. `src/DTMAPI.Abstractions/ConfigMenu.cs` 声明 `IDtmConfigMenuApi`；
2. `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs` 统一拥有页面、选项、owner-bound facade 与 owner 清理；
3. Bootstrap/Core 只创建并发布一份 `DTMAPI.ModConfigMenu` provider；
4. 标题设置 UI 从统一 page snapshot 排序、分页、保存、重置和渲染，不含 AutoFishing 特判；
5. AutoFishing 在 `ModEntry.RegisterConfigMenu()` 中经 `helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")` 注册键位、数字、bool 和 inline 选项，其 manifest 明确依赖该 provider。

这条路径已被多个第一方产品共同使用，应原样复用到第二产品。减重时不应：

- 为 AutoFishing 新建私有设置 UI；
- 把产品标签、范围、默认值和 save/reset delegate 搬回 Runtime；
- 为节省几十行而给 `IDtmHelper` 扩展新公共 ABI；
- 现在就增加反射/attribute 自动表单或第二套注册器。

当前真正的作者侧缺口是 Author SDK README/template 没有统一 ConfigMenu 的最小示例，旧规划还写过并不存在的 `helper.ConfigMenu`。它应在公开作者文档收口时修正为真实的 `ModRegistry.GetApi` 路径；这不是 AutoFishing 玩家 DLL 减重的前置项。`IDtmConfigMenuApi` 的公开稳定性文字也应在正式作者文档前与 API matrix 统一。

## 5. Checkpoint D.5 执行边界

AutoFishing 减重应在现有产品、UniqueID、配置文件、Workshop 身份、Advanced policy 和 package contract 内完成，不新建 Lite 分支产品。

推荐切片：

1. 删除无消费者生产类型和 Legacy/Shadow 脚手架，列出只由 QA 读取的诊断/observation seam；高置信首批候选约 400 行，但仍以编译和行为证据为准；
2. 把 QA-only 采样移出常驻产品状态，或改为显式 QA 激活的有界快照；
3. 合并重复 session/router/lease/transaction 状态，但每次只改变一个 native responsibility；
4. 合并 native cache/accessor/helper 中对同一状态 holder 的重复映射；
5. 最后检查配置/入口 glue，仅做无 ABI 扩张的小整理；
6. 对比减重前后的玩家源码、DLL、inactive roots、每动作短期分配和恢复行为。

每个切片只跑产品 build、受影响的 focused source/unit/package/owner/ABI 检查。最后冻结候选后，只做一次第五存档短回归，覆盖启用、常用配置、F6 停用/恢复、标题返回和干净退出。默认不运行完整 Release suite、L0-L5 梯度、10/30 分钟长测或 100/500 循环；只有热路径/动画改动让短时每动作或每分钟指标回退时，才升级到对应长测。0.5.5 发布保持用户暂停，除非用户明确重新开启。

本轮没有改 Runtime，因此不运行游戏、完整 Release 或长测。

## 6. D.5 之后

AutoFishing 减重完成并通过短回归后，仍须先做一次明确的“唯一第二真实产品 pilot”准入决定；当前架构权威仍阻断其他真实产品，不能从 AutoFishing PASS 自动推导准入。

推荐候选是 `OneActionComplete`：当前产品入口约 145 行，专用 GameBridge 区约 612 行，足以验证小产品是否能避免复制 AutoFishing 的迁移脚手架。迁移时应先按 native owner 拆分：资源完成、填料和执行策略更像 ProductNative；若 `AgentStateInteract.OnExit` 确认与 ActionSpeed 共享同一 native owner 或全局生命周期，则该最小 hook 可能成为 SharedNative 候选，而不是把整个执行器留在 GameBridge。

两个产品完成后再比较：

- ConfigMenu 声明/save/reset glue；
- owner cleanup、输入、session 和 restart 语义；
- Harmony owner 与原生恢复；
- SDK/package、Doctor/Manager 投影；
- QA fixture 与诊断快照。

只有真实双消费者共享同一 native owner、冲突点或全局生命周期不变量时，才提升为 Platform/SharedNative。AutoHarvest 保留为第三方消费者/API 研究输入，不再作为第一方发布产品顺序。

## 7. 安全条款与停止条件

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

出现以下任一情况应停止减重切片并回到证据：

- 相同 native 动作发生重复结算或遗漏；
- F6、标题、存档切换或退出后输入、动画、owner/session/lease/transient 未清零；
- 为删除内部层而扩大公开 ABI 或把 ProductNative 放回 mandatory Runtime；
- QA 只有重新常驻玩家产品才能观察结果；
- source/DLL 变小但每动作或每分钟分配、日志或状态发布反而上升。

## 8. 相关权威

- [轻量化与功能性 Mod 拆分路线图](../../../planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md)
- [Batch 6 Managed Mod Identity Contract](../../../architecture/batch6-managed-mod-identity-contract.md)
- [AutoFishing Advanced Pilot Update](../../../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)
- [AutoFishing SMAPI rehome boundary review](../../api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md)
- [DolocPlus function map](../../../../references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md)
- [DolocPlus deep dive](../../../../references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md)
- [公共 API 矩阵](../../../api/public-api-matrix.md)

Resolution: implementation and final bounded validation are recorded in [20260721-0004 AutoFishing Product Slimming](../../../updates/2026/20260721-0004-autofishing-product-slimming.md); the subsequent second-product decision and comparison are separate Reviews `20260721-0005` and `20260721-0006`.
