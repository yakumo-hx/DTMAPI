# 20260802-0001 AutoFishing 与小神增强包 AFK 钓鱼语义、代码量和功能差异基线

- Status: `recorded`
- Date: `2026-08-02`
- Scope: 小神增强包 1.4.0 `FishingAFK` 与 DTMAPI 当前第一方 AutoFishing 的只读语义、生产源码体量、功能和实现边界对照
- Source: 用户要求先对齐两者的语义范围与大致代码量，再详细比较功能差异
- Runtime impact: 本审查不修改 AutoFishing、DTMAPI Runtime、第三方样本、游戏目录或存档；未启动游戏

## 结论先行

1. 两者都复用游戏原生的 `Ready -> Cast -> Wait -> Battle/Pull -> Pull` 钓鱼链，最终鱼种选择、物品生成、掉落、经验和收取仍主要由游戏负责；但它们不是同一个产品语义。
2. 小神增强包是一个窄语义的“续钓宏”：玩家先手动使用某根鱼竿，它随后强制满蓄力、代按咬钩和小游戏输入、空闲时重复使用这根鱼竿，并在移动、体力不足或抛竿失败时结束。
3. DTMAPI AutoFishing 是一个显式启停的“持续自动化引擎”：F6 开启后反复检查当前选中鱼竿，自行抛竿，并可分别改变咬钩等待、抛竿蓄力、小游戏路线和动画速度；遇到部分失败时默认保持启用并重试。
4. 当前可复核的专属生产实现为小神 **2 文件 / 240 物理行**，DTMAPI **23 文件 / 5,647 物理行**，表面比例约 **23.5 倍**。若扣除小神文件内约 45 行多语言字典，则约为 **29 倍**。这说明 DTMAPI 产品确实很重，但不能解释成“功能多 23.5 倍”。
5. DTMAPI 的玩家决策器本身只有 57 行。5,647 行中有 2,593 行原生访问/缓存/事务、689 行中心 session service、802 行 Hook/回调路由，三者合计 4,084 行、约 72.3%。主要差距来自可靠性和原生集成外壳，而不是玩家功能列表。
6. 小神的短实现适合作为“最小可理解语义”的参考基准，不适合作为可直接移植的结构模板。它缺少显式存档/标题边界、输入和状态故障隔离、部分写入回滚，并且当前 1.00.00 已删除它直接读取的 `HorizontalMoveFactor`。
7. DTMAPI 的可靠性外壳有真实价值，但当前边界仍不够自洽：0.5.5 的精确版本门先于产品能力探测而拒载；22 个 Hook 把默认循环与可选动画能力绑成一个全有或全无集合；`InstantBite` 的准备流程也并非完整可回滚事务。0.6.0 应先处理这些边界，而不是按总行数机械删代码。

## 1. 先梳理语义并比较大致代码量

### 1.1 计数口径

本轮使用当前工作树，不使用 2026-07-21 审查中的旧行数：

- “物理行”包含空行；“非空行”用于减少排版差异的影响。
- 小神数据来自本地 1.4.0 DLL 的 ILSpy 反编译输出，不代表作者原始源码排版。
- 小神专属范围只计 `FishingAFK.cs` 和 `FishingAFKController.cs`；通用 Controller、元数据和插件入口由 26 个功能共享，不全部摊给钓鱼。
- DTMAPI 生产范围只计 `products/first-party/AutoFishing/src`。`qa` 不进入玩家产品 DLL，单独列出。
- DTMAPI Core、Loader、SDK、Manager、Doctor 和 BepInEx/Harmony 本身均不计入双方产品行数。
- 行数只表示维护表面积，不表示运行时性能、安全性或功能价值。

### 1.2 当前体量基线

| 范围 | 文件 | 物理行 | 非空行 | 解读 |
| --- | ---: | ---: | ---: | --- |
| 小神 AFK 钓鱼专属实现 | 2 | 240 | 212 | 可运行特性代码；其中约 45 行是内嵌多语言字典 |
| 小神 AFK 行为近似量，扣除上述字典 | 2 | 约 195 | 不单列 | 只用于数量级参考，不是可独立编译范围 |
| 小神全插件共享 `PatchController`、`FeatureUtils`、`DolocPlusMod` | 3 | 757 | 716 | 由 26 个普通功能共享，不能全部归因于钓鱼 |
| 小神整个 DolocPlus 1.4.0 反编译程序集 | 81 | 5,538 | 5,035 | 包含所有功能和共享框架 |
| DTMAPI AutoFishing 玩家生产源码 | 23 | 5,647 | 5,202 | 进入产品 DLL 的完整实现 |
| DTMAPI AutoFishing QA | 23 | 8,476 | 7,880 | 不进入玩家 DLL |
| DTMAPI 产品维护面，生产加 QA | 46 | 14,123 | 13,082 | 只适合估算仓库维护成本 |

因此有三种不同但都值得保留的说法：

1. **玩家生产实现**：`5,647 / 240 = 23.5`，DTMAPI 约为小神专属实现的 23.5 倍。
2. **扣除小神内嵌翻译后的行为参考**：`5,647 / 195 ≈ 29`，但两边的排版与分层差异更大，只能说是 20–30 倍数量级。
3. **整体复杂度信号**：DTMAPI 单个 AutoFishing 的生产源码甚至比当前整个 DolocPlus 反编译程序集多 109 行，约多 2%。这不是同口径“胜负”，但足以证明产品内部复杂度需要持续解释和约束。

QA 的 8,476 行不应拿来声称玩家加载了 14,123 行逻辑；它仍然是项目维护成本，而且目前已经超过生产实现本身。

### 1.3 DTMAPI 的 5,647 行花在哪里

| DTMAPI 责任组 | 文件 | 物理行 | 占生产源码 | 主要责任 |
| --- | ---: | ---: | ---: | --- |
| 产品入口、配置、contracts/context | 7 | 1,067 | 18.9% | F6、MCM、策略选择、产品内部 DTO 和装配 |
| Hook 安装、回调和路由 | 3 | 802 | 14.2% | 22 个补丁、异常隔离、阶段和输入回调 |
| 原生访问、缓存和 mutation | 8 | 2,593 | 45.9% | 类型/成员解析、缓存 delegate、抛竿、咬钩、reel、能量和状态读取 |
| 输入与动画策略 | 4 | 496 | 8.8% | 可见小游戏输入、reel 边沿、蓄力和动画恢复 |
| 中心 session/state service | 1 | 689 | 12.2% | phase、sequence、重入/重复动作防护、session 释放和诊断 |
| **合计** | **23** | **5,647** | **100%** |  |

其中：

- `FishingDecisionEngine.cs` 只有 57 行，负责“何时抛竿、何时准备咬钩、走可见小游戏还是 skip、当前 note 应 hold/tap/release”。
- `AutoFishingConfig.cs + FishingDecisionEngine.cs + ModEntry.cs` 合计 629 行，可粗看作玩家策略与产品控制面；它们仍不是一个可独立运行的“核心”。
- 原生访问/缓存、中心 session 和 Hook/回调合计 4,084 行，占 72.3%。所以原始 23.5 倍主要是实现与保证的差距。
- 2026-07-21 基线曾是 6,266 行，随后 slimming 降到 5,393 行；当前 5,647 行是新的准确基线。它说明既有减重有效，但后续能量门和可靠性工作又增加了约 254 行。

### 1.4 小神增强包的准确语义

“在配置里启用 AFK Fishing”只会安装该功能的 Harmony 补丁，**不会立即开始挂机**。真正的挂机 session 由玩家第一次实际使用鱼竿触发：

1. `ItemFishingRod.OnUseAsTool` 前缀保存这一个鱼竿对象并将内部 AFK 状态置为开启。
2. `AgentStateFishingReady.OnExit` 把蓄力计时和 perfect 记录推到满值；因此语义是固定满蓄力/完美抛竿，而不是可配距离。
3. `AgentStateFishingCast.NextState` 若没有进入任一钓鱼状态，立即停止并提示没有找到可钓水面。
4. `AgentStateFishingWait.NextState` 在原生咬钩已准备好后临时让 `NormalFishing` 返回按下，仍由原生 Wait 扣体力并选择 Battle 或 Pull。
5. `FishingGameScrollBar.UpdateGame` 在当前 note 的有效窗口内，把所有非 `Delay` note 都视为按住；离开窗口后释放。
6. Controller 每秒检查一次；只在普通游戏状态且角色为 Idle 时，以保存的鱼竿再次调用原生 `UseFishRod`。
7. 原生能量不足会结束挂机；检测到移动会结束挂机；已有一次有效 Wait 后结束时会显示提示。

这是一条很短的行为契约：

> 玩家用某根鱼竿开始普通钓鱼；Mod 强制满蓄力并替玩家完成普通输入；每轮结束后继续使用同一根鱼竿；移动、没体力或抛竿失败即结束。

它的短小来自明确舍弃了很多边界：没有单独热键、没有蓄力/咬钩/skip/动画配置、没有显式 save/title reset、没有 session sequence、没有字段事务、没有逐回调异常隔离，也没有把功能配置关闭和内部 AFK 状态清零绑定起来。

### 1.5 DTMAPI AutoFishing 的准确语义

DTMAPI 的起点不是一次手动抛竿，而是一个可独立启停的持续 session：

1. 默认 F6 开启；热键可改绑或设为 `None`。开启后不要求玩家先手动钓一次。
2. 每次准备抛竿时重新读取**当前选中物品**；不是鱼竿就等待并重试，不会搜索背包，也不会保存第一次使用的鱼竿对象。
3. 默认蓄力比例为 `0`，即最短抛竿；可配置到 `1`。这与小神固定满蓄力正好相反。
4. 默认等待原生咬钩、进入真实可见小游戏并自动完成；可分别开启 InstantBite、SkipMiniGame 和 FastAnimations。
5. 内部使用 12 个 phase 与递增 sequence，避免旧帧、重复回调或重复 decision 对同一原生状态再次执行动作。
6. Pull 完成后等待 0.25 秒重抛；无水面或没有选中鱼竿时 1 秒重试；抛竿异常或未到 Wait 就退出时约 2.5 秒重试。
7. 体力不足目前不结束 session，而是保持 F6 开启并按默认 0.25 秒退避继续检查。
8. F6 关闭会停止 updater、释放 session 和输入/动画 override；不会强行终止已经进入的原生钓鱼状态，玩家可以手动接管当前一轮。
9. SaveLoaded 会重置原生缓存/session，并在仍启用时重建；ReturnedToTitle 会明确关闭自动钓鱼。

其更准确的行为契约是：

> 玩家开启一个持久自动钓鱼模式；产品使用当前选中鱼竿，在可恢复条件不足时等待，在失败后按策略重试，并可选择改变等待、蓄力、小游戏和动画四类原生行为，同时维护输入、动画和生命周期清理。

### 1.6 两种语义为什么都合理，但不能混称“同功能”

小神更接近“我已经开始钓鱼，请帮我续下去”；DTMAPI 更接近“保持自动钓鱼任务开启，条件一旦允许就继续”。由此自然产生不同决定：

- 小神把移动、无体力和落水失败看作**终止意图**；DTMAPI 把多数条件看作**暂时不可执行**。
- 小神绑定第一次使用的鱼竿；DTMAPI绑定每次抛竿时的当前快捷栏选择。
- 小神默认最大距离；DTMAPI 默认最小距离。
- 小神没有独立的“自动钓鱼开关”；DTMAPI 的 F6 session 本身就是产品主状态。

因此“实现更简单”首先是语义更窄，其次才是工程实现更少。若要把小神作为 DTMAPI 的基准，最有价值的基准不是 240 行目标，而是先决定 DTMAPI 是否仍要保留“持久任务”语义，以及默认循环与三类可选改写各自值不值得存在。

## 2. 在同一语义坐标上比较功能差异

### 2.1 玩家可见行为矩阵

| 行为 | 小神增强包 1.4.0 | DTMAPI 当前 AutoFishing | 实际影响 |
| --- | --- | --- | --- |
| 功能启用 | BepInEx 配置开启只安装补丁 | 产品 Entry 安装 Hook；F6 控制自动化 session | 两边都有“功能已加载”和“正在挂机”两个状态，但小神第二状态由手动抛竿隐式触发 |
| 开始方式 | 玩家先用一次鱼竿 | F6 开启后自动尝试 | 小神保持原本操作习惯；DTMAPI 更像独立任务 |
| 结束方式 | 移动、无体力、抛竿未进钓鱼状态；或外部关闭配置 | F6、检测到移动、标题返回、session/input fault | DTMAPI 对无体力和落水失败通常不结束，只退避重试 |
| 鱼竿选择 | 保存第一次手动使用的那一个对象 | 每轮要求当前选中物品是鱼竿 | 切换快捷栏后，小神仍可能使用旧杆；DTMAPI 暂停并等玩家重新选杆 |
| 抛竿蓄力 | 固定满值并标记 perfect | 0–1 可配，默认 0 | 默认体验完全不同；只比较普通循环时应明确配置口径 |
| 抛竿动画 | 不加速，等原生 Ready 完成后在退出点写满值 | 可选 1–4 倍，Ready 计时、hook 物理和 Pull 都有独立处理 | DTMAPI 的 FastAnimations 是额外功能，不是 AFK 基线所必需 |
| 咬钩等待 | 完整原生等待 | 默认完整原生等待；InstantBite 可重复调用原生 roll 直至准备结果 | 默认等价；开启 InstantBite 后 DTMAPI 改变等待概率/时间语义 |
| 拉杆输入 | 咬钩准备好时让 `NormalFishing` 为一次按下 | 排队一个有时限、可确认和重试的 scoped 输入边沿 | DTMAPI 处理丢帧/未消费；小神更短但没有确认和超时状态 |
| 可见小游戏 | 对有效窗口内所有非 Delay note 持续返回按下 | Stable 返回 in-progress hold；Bonus 每个 note 只给一次 triggered edge；其他状态释放 | DTMAPI 更接近普通输入的“按住稳定、点一次奖励”语义 |
| Bonus 计分 | Bonus 窗口内每帧都可能返回 triggered | 同一个 Bonus note 只 tap 一次 | 当前原生代码会在后续触发增加 `BonusExtraScore`；小神存在重复吃 BonusExtraScore 的静态可能，尚未做运行实测 |
| 跳过小游戏 | 无独立选项；除非游戏全局 flag 已被别处改变 | 可选，临时写原生 `skipFishingGame` 并在 `finally` 恢复 | DTMAPI 提供第二条结果路线，并承担全局 flag 恢复责任 |
| 鱼/垃圾/经验/掉落 | 不替换原生 `RollFish` 或 Pull 结算 | 默认同样保留；InstantBite 仍用原生 roll，Skip 仍走原生 Wait/Pull | 两者不是直接往背包塞鱼；DTMAPI 的可选模式改变到达原生结果的时机/路线 |
| 体力不足 | 立即停止 AFK | cast 被能量门拒绝，但 session 保持开启并约每 0.25 秒检查 | 这是明确产品策略差异，也会影响日志/轮询频率和玩家预期 |
| 没水面/落点失败 | Cast 未进入钓鱼状态就停止并提示 | 场景无任何 FishingPool 时 1 秒重试；实际落点失败时进入 Interrupted，约 2.5 秒再试 | DTMAPI 可能在玩家站位不正确时持续抛竿，小神要求玩家重新开始 |
| 重抛节奏 | Controller 每 1 秒，从 Idle 重抛 | Pull 后 0.25 秒；失败按 0.25/1/2.5 秒分类 | DTMAPI 更快，也更积极 |
| 原始玩家输入 | AFK 开启时 `NormalFishing` 总被替换为 `_isPressed`，即使为 false | 只在指定 Ready/reel/minigame 窗口覆盖；其余时候放行原 getter | DTMAPI 的输入租约更窄，玩家手动接管和其他 Mod 共存更可控 |
| 开始/停止提示 | 有本地化游戏内消息，落水失败也提示 | 主要写日志，并在配置菜单显示状态 | 小神的即时可见反馈更直接；DTMAPI 的诊断更丰富但玩家可见性较弱 |
| 功能冲突 | 显式声明与 Time Speed 冲突 | 未声明与 DebugConsole time scale 等功能的互斥 | 是否真有 native owner 冲突需另审；至少产品目前没有玩家可见仲裁 |
| 保存/标题边界 | 专属实现没有 SaveLoaded/ReturnedToTitle reset | 显式重置和释放；标题返回强制关闭 | DTMAPI 更可靠；小神静态鱼竿/session 可能跨环境残留 |
| 配置关闭再开启 | 通用 controller 只 unpatch/patch，不调用 `FishingAFK.Stop` | F6 off 释放 session；产品停用时 unpatch 并聚合清理 | 小神重新开启后可能恢复旧 `_isEnabled`/鱼竿引用；DTMAPI 边界更完整 |
| 异常隔离 | 专属 callbacks 无 try/catch；插件逐 controller Update 也无单项隔离 | 回调捕获并记录；输入 fault 有关闭和待清理状态 | DTMAPI 代码量换来了故障不直接穿透游戏回调的保证 |

若只想把 DTMAPI 配成最接近小神的普通钓鱼流程，可用：

- `CastChargeRatio = 1`
- `InstantBite = false`
- `SkipMiniGame = false`
- `FastAnimations = false`

这只能对齐“满蓄力 + 原生等待 + 可见小游戏”三个方面；启动方式、鱼竿绑定、Bonus 输入、体力/失败策略和停止语义仍不相同。

### 2.2 两者对原生结果的介入程度

两者的共同优点是都没有重写鱼表、直接构造奖励或绕过 Pull 的物品/经验路径。当前游戏的 `FishingCache.RollFish` 仍依据 pool、房间 season group 和鱼竿等级生成结果，Pull 仍负责收取、掉落、catch 事件与钓鱼经验。

介入程度从低到高可排列为：

1. 小神默认路径：等待原生 roll，只模拟咬钩和小游戏输入。
2. DTMAPI 默认路径：同样等待原生 roll，但用更精确的 scoped 输入与 phase/session 防护。
3. DTMAPI FastAnimations / CastCharge：改计时、动画和 hook 物理，不改鱼种结果，但会改变可见节奏和落点距离。
4. DTMAPI InstantBite：在 Wait 内连续调用原生 `RollFish`，最多 100 次，直到准备出 native bite；鱼/垃圾仍由原生选择，但等待概率与时间被有意压缩。
5. DTMAPI SkipMiniGame：临时打开游戏自己的 skip 路线，让 Wait 直接选原生 Pull 结果，而不是创建 Battle。

所以“DTMAPI 保留原生结果”是对的，但不能进一步简写为“所有模式都与普通钓鱼等价”。它保留的是结果所有者，部分可选项明确改变了到达结果的过程和概率节奏。

### 2.3 小神更短并不等于所有实现选择都更好

小神值得保留的参考点：

- 语义一句话就能讲清；
- 只依赖 6 个不同原生方法、8 个 patch callbacks；
- 原生普通路径承担绝大多数状态机工作；
- 正常状态、Idle、能量三项 preflight 很直观；
- 玩家能立即看到开始、停止和落水失败原因。

但它同时把风险压给运行环境：

- 直接编译引用私有/游戏类型和已删除成员，版本漂移时容易在执行点失败；
- 四个静态 session 字段和一个计时器没有 save/title generation；
- 配置 unpatch 与内部 `Stop` 不一致；
- `NormalFishing` 在整个 AFK 期间被全量截断，而不是 scoped override；
- patch callback 和 controller Update 缺少特性级异常关闭；
- 没有原生字段修改的 snapshot/rollback，也没有重复回调/旧帧 sequence 防护。

因此它适合回答“普通 AFK 最少需要做什么”，不适合回答“DTMAPI 应保留哪些可靠性承诺”。

### 2.4 DTMAPI 多出来的代码也不是全部已经证明必要

当前 DTMAPI 有 22 个 patch callbacks、21 个不同原生方法目标。它们在产品 Entry 时一次性解析并原子安装，即使 `FastAnimations=false` 也一样。优点是不会在半套核心观察链下启动；缺点是任一仅服务可选动画的目标漂移，也会阻止默认普通循环加载。

当前至少有四类值得 0.6.0 重新划界的表面积：

1. **必要 Hook 与可选 Hook 未分组。** `FishRodRenderer.CastHook/Pull/PullCancel` 主要服务 FastAnimations，不应天然拥有与默认 cast/wait/minigame/result 全相同的拒载权。应保留“必要组原子”，并让可选能力各自探测、禁用和解释。
2. **输入 getter 面偏宽。** 产品 patch 了 `NormalUseTool`、`NormalUseToolInProgress`、`NormalUseItem`、`NormalUseItemInProgress`、`NormalFishing`、`NormalFishingInProgress` 六个 getter；当前所审查的 fishing bodies 未读取 `NormalUseItemInProgress`。0.6.0 应从每个 phase 的真实消费者重新推导最小集合，减少冲突面，而不是假定六个都必须保留。
3. **反射兼容外壳与精确拒载门互相抵消。** 产品花大量代码对成员按类型构建并缓存 delegate、在能力缺失时 latch unavailable；但 0.5.5 又先按旧整包身份拒绝当前游戏，产品没有机会报告哪些能力实际仍可用。若 0.6.0 采用产品级 capability contract，这些代码才真正承担“兼容漂移”价值。
4. **历史迁移层仍可继续收缩。** 2026-07-21 已确认主要 Native 文件从 GameBridge 高相似迁入。当前所有权已经正确归于 ProductNative，但 session、callback、cache、transaction 之间仍应逐项回答“保护了哪个已知失败”。没有对应 invariant 的层可以在产品内部合并；不能因为它只服务一个产品就移回 GameBridge。

## 3. 当前 1.00.00 兼容事实

本节是静态事实，不是运行接受：

1. DTMAPI 0.5.5 的 Advanced 精确策略会在 AutoFishing Entry 前拒绝当前 1.00.00，因此当前产品实际上不会进入上述运行流程。
2. 静态检查显示 AutoFishing 的 21 个不同 Harmony 方法目标仍能在当前反编译中找到；确定变化的是移动状态 owner，而不是整条钓鱼状态机消失。
3. 旧 `AgentPhysicalStatus.HorizontalMoveFactor` 已删除。当前直接输入 owner 是 `AgentPhysicalStatus.MoveModifier.inputMultiplier`；原生 Wait 还分别观察该输入和 `VelocityX` 来决定中断/失败。
4. 小神直接读取旧属性，其 Controller `Update` 在当前版本存在确定的缺失成员风险，而且插件的 controller 更新循环没有单项 try/catch。
5. DTMAPI 通过反射查找旧属性；找不到时不会因此崩溃，而会降到 A/D/Space/Shift 的按键快照。但这只是部分退化：它不能完整覆盖手柄、改键和其他原生移动来源。
6. 更微妙的行为是：若未捕获玩家移动，原生 Wait 自己可能让本轮失败，而 DTMAPI 将其视作 Interrupted 后再次抛竿。这样“移动即停止”的产品承诺会退化成“移动打断一轮但自动化继续”。

移动修复应先确认语义：若目标是“玩家主动移动即关闭”，最接近旧属性的是 `MoveModifier.inputMultiplier`，不应把传送带/外力速度一律当成玩家取消；实际钓鱼状态因速度退出，则继续由 phase observation 处理。键盘、手柄和改键都必须进入接受矩阵。

## 4. 当前实现中需要单独纠正的可靠性判断

### 4.1 `InstantBite` 不是完整的可回滚事务

`FishingNativeTransactionCache.TryPrepareNativeBite` 当前依次执行原生 roll，并写入 `_waitForFishBite`、`_hasRolled`、`_hookProbability`、`_fishOnHookDuration`，随后调用提示和 renderer 刷新。该方法捕获异常并返回失败，但没有先保存这些字段，也没有在后续写入/调用失败时恢复先前值。

因此可能出现：方法向上报告 `native-bite-failed`，原生 Wait 却已经处于部分“有鱼上钩”状态。与此相对，SkipMiniGame 对全局 `skipFishingGame` 的临时写入确实有 `finally` 恢复。

0.6.0 必须二选一：

- 为 bite preparation 建立真实 snapshot、按写入逆序回滚，并通过每个 checkpoint 的 fault injection；或
- 明确把它定义为不可逆的单向原生 transition，一旦任一后续步骤失败就以该原生状态为准进行 fault-close/reconcile，不再用“transaction rollback”描述它。

在此之前，既有审查中“InstantBite 可 settle 或 restore 整个 transaction”的宽泛表述不能作为当前实现事实。

### 4.2 小神的 Bonus 持续按下可能不是普通玩家语义

当前 `FishingNoteType` 只有 `Delay`、`Stable`、`Bonus`。小神在非 Delay 窗口内让 triggered 型 `NormalFishing` 每次查询都为 true；当前原生 Bonus 分支在首次基础分以后，后续触发会继续增加 `BonusExtraScore`。因此静态上存在同一 Bonus 窗口重复计分的可能。

DTMAPI 记录已 tap 的 note index，只给每个 Bonus note 一个 triggered edge。这个实现更复杂，但更接近普通按键“点一次”的输入语义，应该保留，不能为了向 240 行靠拢而改成每帧 true。最终仍需当前版本的真实小游戏计分验证。

### 4.3 原子 Hook 应保留，但原子边界应缩小

小神 `PatchAll(type)` 失败后只记录错误，配置开关仍可能显示为启用；若 Harmony 在异常前已经装入部分 patch，controller 没有显式健康状态和回滚证明。DTMAPI 先解析完整 inventory、安装失败时按 owner unpatch，这一保证值得保留。

应调整的是原子集合，而不是取消原子性：

- 默认 AFK loop 的 required targets 必须全量通过，否则不启动 session；
- InstantBite、SkipMiniGame、FastAnimations 各自拥有 capability 与失败动作；
- 某个可选组不兼容时，产品 shell 和默认 loop 可以继续，配置界面明确禁用该项并说明原因；
- 不允许以“多数 Hook 存在”替代 required group 的完整性。

## 5. 对 0.6.0 的建议边界

### P0：先修当前正式版的确定兼容项

1. 把 movement capability 从已删除的 `HorizontalMoveFactor` 迁到当前原生 owner，并覆盖键盘、手柄、改键及 native Wait 自身中断。
2. 将当前整包精确身份从唯一拒载条件改为证据；AutoFishing 用 required/optional capability contract 决定默认循环和各选项能否启用。
3. 当前正式版 capability 未完成前，不以“21 个 Hook 方法名仍存在”宣称 AutoFishing 已兼容。

### P1：冻结产品语义，避免边修边变

需要明确写进 README/MCM 的四个决定：

1. 产品究竟是小神式“手动开始后的续钓”，还是当前“F6 持久任务”；本审查建议保持当前 F6 模式，不在兼容修复中暗改启动模型。
2. 无体力、没选杆、无水面、落点失败分别是停止还是等待；当前实现并不一致，特别是无体力 0.25 秒轮询应显式决定。
3. 默认蓄力 `0` 是否仍是正式产品意图。它不是小神语义，也不是普通玩家最常见的“满蓄力挂机”的自然推断。
4. F6 off 是否只停止自动化并允许手动接管当前一轮；当前实现如此，建议保留并测试每个 phase。

### P1：拆出最小默认循环与三类可选改写

建议逻辑分组：

- `BaselineLoop`: toggle、selected rod、能量/状态 preflight、cast/wait、scoped reel、可见小游戏、Pull/recast、movement/title/save cleanup。
- `InstantBite`: Wait 私有字段与 roll capability，以及完整 rollback/reconcile contract。
- `SkipMiniGame`: 原生 skip flag 与 NextState/Overwrite capability，保证全局 flag 恢复。
- `FastAnimations`: Ready animator/timer、hook velocity/gravity、Pull duration 与逐项原值恢复。

CastCharge 虽是玩家配置，但其 ratio 读取和 Ready 输入与 BaselineLoop 有交叉；可保留在 baseline required contract，FastAnimations 只负责加速，不拥有 charge target。

### P2：以责任证据减重，不设机械行数目标

1. 重新推导最小输入 getter 集合，先证伪当前明显没有 fishing 消费者的目标。
2. 合并只为历史跨程序集迁移留下、又没有独立 invariant 的 facade/router/cache 层。
3. 对重复 accessor builder 优先生成或集中复用；若要改成更多直接 typed native reference，须另做兼容与 Advanced policy 设计审查。
4. 保留 sequence、防重复 settlement、required Hook 原子安装、scoped input、原值恢复、F6/title/save/owner cleanup 和 fault-close；这些不是为行数服务的装饰层。
5. 不把单产品状态机迁回 GameBridge。按当前架构，它仍是 AutoFishing 的 ProductNative；只有第二个真实消费者共享同一个 native owner 或全局 lifecycle invariant，才有 SharedNative 提炼理由。

## 6. 后续实现的最小接受矩阵

本轮不实施；若进入 0.6.0 修复，至少应验证：

1. 当前第五存档默认循环：选中鱼竿、默认 charge、原生等待、可见小游戏、原生收取和连续重抛。
2. `CastChargeRatio=1` 的小神近似流程，以及默认 `0` 的产品既定流程，分别有准确落点/蓄力证据。
3. 无体力、未选杆、无 FishingPool、实际落点失败各自符合已冻结的 stop/wait/retry 策略，且不刷屏。
4. 键盘、手柄和改键移动均能执行“玩家移动即关闭”；传送带/外力按最终语义单独验证。
5. F6 在 Ready、Wait、Battle 和 Pull 阶段关闭后，不残留 synthetic input、动画倍率、pending reel 或 session；当前原生一轮可按约定手动接管。
6. SaveLoaded、ReturnedToTitle、产品停用/重启和干净退出后，无旧 agent、rod、wait、game handle 或 override 引用。
7. 缺失 FastAnimations 单一可选 target 时，BaselineLoop 仍可用且界面准确禁用 FastAnimations；缺失 required target 时不得半套启动。
8. InstantBite 对每一个字段写入和提示/renderer 调用做 fault injection，证明真实 rollback 或确定 reconcile。
9. 可见小游戏 Stable hold、Delay release、Bonus 每 note 一次 edge，并核对最终计分和原生 success/failure。
10. 启停、日志、Hook owner 和退出检查遵守现有 AutoFishing 第五存档与 runtime lock 规则。

## 7. 本轮验证与限制

- 重新统计当前小神 1.4.0 反编译输出及 DTMAPI `src`/`qa` 行数，并按 23 个生产文件逐组核算，分组合计为 5,647。
- 逐方法阅读小神 `FishingAFK`、`FishingAFKController`、共享 `PatchController` 和插件 Update 生命周期。
- 逐路径阅读 DTMAPI decision、ModEntry、Hook inventory、callbacks、state/session、input、animation、energy、native bite/reel 代码。
- 对照当前 `24456188_test_E861E0` 反编译中的 Ready/Cast/Wait/Battle/Pull、`FishingGameScrollBar`、`FishingCache`、输入 getter 和 movement owner。
- 没有运行第三方 DLL、Cheat Engine、DTMAPI AutoFishing 或游戏；Bonus 重复计分、当前 Mono callback 行为和实际手柄移动仍是待运行验证项。
- 本审查只描述第三方语义和成员身份，不复制第三方方法体，也不授权将其代码并入 DTMAPI。

## 8. 相关权威与前序记录

- [DolocPlus 1.4.0 / Lua CT 1.9 版本与语义兼容审查](../../api/2026/third-party-mods/20260801-dolocplus-140-lua19-version-and-semantics.md)
- [Doloc Town 1.00.00 / DTMAPI 0.6.0 兼容根因审查](20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md)
- [2026-07-21 AutoFishing 产品重量与统一配置复用审查](20260721-0004-autofishing-product-weight-and-config-reuse-review.md)
- [2026-07-21 AutoFishing Product Slimming](../../../updates/2026/20260721-0004-autofishing-product-slimming.md)
- [AutoFishing Hook Map](../../../../hook-map/focused/FishingAutomationCompatibility.md)
- [AutoFishing README](../../../../../products/first-party/AutoFishing/README.md)

本 Review 是当前“小神 AFK 钓鱼与 DTMAPI AutoFishing”语义和代码量的最新基线；前序 2026-07-21 Review 保留当时迁移与 slimming 决策的历史事实。
