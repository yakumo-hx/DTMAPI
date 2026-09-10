# DTMAPI 本体技术债、冗余、过度设计与 SMAPI 净室对照审计

## 记录信息

- 日期：`2026-08-31`
- 状态：`recorded`
- 性质：audit-only 代码 Review；不承接实现生命周期
- Source：用户要求审计 DTMAPI 本体代码的技术债、冗余与过度设计，并对照本地 `SMAPIlearning` 研究后续完善方向
- DTMAPI 基线：分支 `codex/major-update-batch0-20260713`，`HEAD=98d75c6c51300768e3d7a8b209c858c9ad110493`
- SMAPI 基线：实际存在的只读仓库 `E:\Python_project\SMAPIlearning\SMAPI`，`develop@5689c8d6aeecf54f670559ffaaed6684a5febc25`，`4.5.2-54-g5689c8d6`，工作树干净
- 用户给出的 `E:\Python\_project\SMAPIlearning` 不存在；本 Review 只纠正读取路径，不修改该外部仓库
- Implementation owner：尚未建立。任何整改必须另建对应 `docs/updates/2026/...`，不得把本 Review 改写成完成记录

审计开始时 DTMAPI 工作树已有大量未提交修改，主要集中在 DebugConsole、文档、发布脚本及其测试。
这些修改属于用户现有工作，本 Review 不覆盖、不清理、不接管。除特别注明的“当前工作树规模”外，
下述关键缺陷均落在本轮开始时没有未提交修改的文件中；Compatibility 项目链接债同时存在于 `HEAD`
与当前工作树。

本审计不修改 Runtime、Loader、API、产品、游戏、Workshop、Official MODS 或存档，不复制 SMAPI
实现，也不把源码行数当作性能、GC 或玩家行为证据。

## 1. 执行结论

没有发现可仅凭本轮静态审计定为 P0 的发布阻断或数据破坏事实，但发现两项源码可直接证明的 P1
正确性缺陷：

1. 可选依赖构成环时，被当前排序器当成必需依赖环，环内 Mod 全部进入 blocked；
2. `DtmKeybind` / `DtmKeybindList` 以 `IReadOnlyList` 暴露真实数组，调用者可以强转后原地修改，
   使 Core 已缓存的按钮索引与后续实际判断失配。

另有三类高可信运行风险需要优先收口：

- Bootstrap 把 Input System 的“设备尚未就绪/一次读取异常”永久负缓存，并缓存具体设备 Control；
- ConfigMenu 的重叠预览作用域可在乱序释放后留下陈旧值，保存失败又只能回滚内存、不能证明磁盘回滚；
- GameBridge 仍保留“方法名 + 参数个数”的 Harmony 目标选择，遇同参数数 overload 时可能选择错误方法。

结构上，DTMAPI 外层依赖方向仍然健康：一个 Bootstrap 入口，Core/GameBridge/ModConfigMenu 依赖
Abstractions，普通 Mod 不引用 Unity/Harmony/BepInEx 原生类型。2026 年 7 月的 QA/Compatibility
可选化、owner cleanup、按需 Hook/updater、ProductNative 迁移也是真实进展，不应被本轮重新判成“尚未做”。

当前主要技术债已经从“所有功能都塞进默认 GameBridge”转为：

- 同一事实由 Loader、诊断 registry、错误文本、生命周期 ledger、feature contract、demand route 和测试
  harness 多次手写；
- 历史 refactor scaffold 已变成长期配置与诊断表面；
- `DtmApiRuntime` 继续作为 4,640 行的总编排器，持有过多并行状态；
- Compatibility 与测试通过源码 Link 复用生产实现，编译边界不再等于所有权边界；
- 根 solution、真实脚本构建和产品构建清单已经分叉。

因此推荐路线不是重写，也不是按行数删代码，而是：先修五个有界正确性/可靠性问题，再建立一个
结构化加载决策真相源，随后逐步收束编排器、测试入口和过渡诊断表面。公共 ABI、Frozen
Compatibility 与 native-owner 边界必须分别处理，不能作为“瘦身”附带删除。

## 2. 审计范围与规模口径

### 2.1 本体边界

本轮把以下五个玩家必需程序集视为“本体 mandatory Runtime”：

1. `DTMAPI.Abstractions`；
2. `DTMAPI.Core`；
3. `DTMAPI.ModConfigMenu`；
4. `DTMAPI.GameBridge.DolocTown`；
5. `DTMAPI.BepInExBootstrap`。

`DTMAPI.GameBridge.DolocTown.Compatibility` 和 `DTMAPI.GameBridge.DolocTown.QA` 只审计其与
mandatory Runtime 的边界，不把它们误算为默认加载代码。第一方产品实现不作逐产品功能审计；
只有当其源码被 Compatibility 或 UnitTests 直接 Link 编译时，才作为本体耦合证据。

### 2.2 当前编译源规模

使用仓库本地 .NET 8 resolver，并由 MSBuild `-getItem:Compile` 读取每个项目的实际 Compile item，
再统计当前工作树物理行：

| 编译边界 | Compile files | 物理行 | 解释 |
| --- | ---: | ---: | --- |
| Abstractions | 13 | 3,852 | 公共/内部契约叶节点 |
| Core | 51 | 29,679 | Loader、服务、Manager、诊断与生命周期 |
| ModConfigMenu | 4 | 1,449 | 注册、页面、事务回调 |
| mandatory GameBridge | 76 | 23,177 | SharedNative、薄 compatibility proxy、Hook/demand |
| Bootstrap | 8 | 4,907 | BepInEx 入口、输入与反射 UI |
| **mandatory 合计** | **152** | **63,064** | 玩家默认五程序集的编译源审阅面 |
| optional Compatibility | 52 | 30,871 | Frozen ABI executor 与被 Link 的产品/兼容源码 |
| optional QA | 64 | 37,595 | 玩家包外 QA host |
| 本地 SMAPI `src/SMAPI` | 405 | 35,549 | 只作相近主机核心审阅面参照 |

DTMAPI mandatory 编译源大约是本地 SMAPI core 物理行数的 1.77 倍，但这个比值不能推出性能、质量
或功能优劣：两者游戏、native bridge、发布治理和诊断责任不同。它只说明 DTMAPI 在生态更窄的阶段
已经承担了更大的默认代码审阅面，因此“新增一个并行 contract/ledger/registry”应有更高举证门槛。

几个更直接的热点：

- [`DtmApiRuntime.cs`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs) 4,640 行；
- [`EventManager.cs`](../../../../../src/DTMAPI.Core/Services/EventManager.cs) 2,165 行；
- [`WorkshopContentInputUi.cs`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs) 2,926 行，
  同一文件容纳 Workshop、Content、Input、UI 四类服务；
- [`ReflectedTitleMenuSettingsUi.cs`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs)
  当前工作树 2,330 行；
- [`tests/DTMAPI.UnitTests/Program.cs`](../../../../../tests/DTMAPI.UnitTests/Program.cs) 当前工作树
  18,200 行、约 345 个静态方法。

这些是审阅和变更半径信号，不是单独成立的缺陷。

## 3. 发现总表

| ID | 优先级 | 类型 | 结论 |
| --- | --- | --- | --- |
| `TD-01` | P1 | 已证实正确性缺陷 | 可选依赖环被错误地作为致命依赖环，全环阻断 |
| `TD-02` | P1 | 已证实正确性缺陷 | 只读 keybind 集合泄漏真实数组，可导致索引与判断分叉 |
| `TD-03` | P1 | 已证实状态机缺陷 + 持久化契约缺口 | MCM 重叠 preview 乱序释放可遗留值；save 失败只回滚内存 |
| `TD-04` | P1 | 高可信可靠性风险 | Input System 瞬时不可用被永久负缓存，设备替换后继续读旧 Control |
| `TD-05` | P1 | 高可信 Hook 兼容风险 | 部分 Harmony target 仍仅按名称与参数数取第一个匹配 |
| `TD-06` | P1 | 生命周期/API 债 | 初始化方法意外公开，停用却依赖未声明的魔术反射回调和无幂等重试 |
| `TD-07` | P1 | 事实重复/编排器债 | Loader 决策、诊断 status 与 registry 重判；Runtime 持有多套平行 owner 状态 |
| `TD-08` | P1 | 工程与测试拓扑债 | 根 solution 已坏；构建清单分叉；巨型自制测试入口与生产源码 Link |
| `TD-09` | P2 | 过渡脚手架/过度设计 | refactor flags、feature contract、lifecycle ledger、demand 描述存在多重非执行真相 |
| `TD-10` | P2 | Compatibility 编译耦合 | optional Host 依赖同名源码遮蔽、全局 CS0436 抑制及产品源码 Link |
| `TD-11` | P2 | owner 与未来能力边界 | ContentQuery 自建 winner；Frozen CustomEntity 仍有大实现；内容/音频宿主未物理完成 |

## 4. P1 正确性与可靠性问题

### TD-01：可选依赖环被误判为致命依赖环

**当前事实**

- Loader 对缺失或低版本的可选依赖明确只跳过/告警，不阻断：
  [`DtmApiRuntime.cs:4196`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4196)。
- 排序器却遍历全部 dependency edge，没有检查 `dependency.Required`：
  [`DtmApiRuntime.cs:4419`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4419)、
  [`DtmApiRuntime.cs:4462`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4462)。
- 遇到任意回边后，环内所有 ID 都进入 `dependencyCycleBlockedIds`；加载阶段随后无条件拒载：
  [`DtmApiRuntime.cs:4447`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4447)、
  [`DtmApiRuntime.cs:3098`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L3098)。
- 当前 Unit 只覆盖两个 `Required=true` 的环，没有 optional-only 或 mixed cycle：
  [`Program.cs:2854`](../../../../../tests/DTMAPI.UnitTests/Program.cs#L2854)。

**根因**

加载可用性语义与拓扑排序语义分成两套实现。前者知道 optional/required，后者把所有边当成致命
先决关系。

**影响**

两个只希望在对方存在时调整顺序或启用集成的 Mod，可以在都安装时被整体拒载。它不是单纯诊断
显示错误，而是实际改变加载集合。

**未证明/拒绝的推断**

- 尚未证明当前公开产品恰好形成这种 optional 环；不能写成现有玩家普遍故障。
- 不能简单删除 optional edge；无环时它仍可能表达有价值的相对顺序。

**最小整改与接受**

让 required edge 成为致命拓扑边；optional edge 仅在不形成环时参与顺序，形成回边时忽略该 optional
edge 并产生结构化 warning。测试至少覆盖 optional↔optional、required↔required、mixed cycle、
两种 manifest 枚举顺序，以及最终 Loader/Manager status 完全一致。

### TD-02：`IReadOnlyList` 暴露真实 keybind 数组

**当前事实**

- `DtmKeybind.Buttons` 直接返回内部 `DtmButton[]`：
  [`Input.cs:193`](../../../../../src/DTMAPI.Abstractions/Input.cs#L193)。
- `DtmKeybindList.Keybinds` 直接返回内部 `DtmKeybind[]`：
  [`Input.cs:314`](../../../../../src/DTMAPI.Abstractions/Input.cs#L314)。
- Core 注册时保存同一个 `DtmKeybindList`，但另行计算一次 `ButtonIds` 用于索引：
  [`WorkshopContentInputUi.cs:1741`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L1741)。
- 后续帧判断重新读取 `registration.Keybinds`：
  [`WorkshopContentInputUi.cs:1181`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L1181)。

虽然公共类型写成 `IReadOnlyList<T>`，运行时对象仍是数组，调用者可执行
`(DtmKeybind[])list.Keybinds` 或 `(DtmButton[])keybind.Buttons` 后原地改写。Core 的按钮索引仍指向旧值，
而状态求值、抑制与事件分发会读取新值。

**影响**

可能出现旧按钮仍被索引、新按钮参与状态计算、neutral/Release settlement 不一致，以及 owner cleanup
按旧索引清理。该问题可仅由公开 API 调用方触发，不需要反射。

**最小整改与接受**

构造时保留私有数组，但公开属性返回缓存的 `Array.AsReadOnly` 或真正不可变快照；注册边界再对外来
keybind list 做一次规范化复制。公共签名和 ABI 不变。测试必须证明无法取得可写数组，并覆盖修改源
enumerable、强转尝试、Press/Release、neutral settlement 与 owner cleanup。

### TD-03：ConfigMenu preview 重入与 save 持久化语义不完整

#### 3A. 重叠 preview 作用域

`ConfigMenuPage` 允许同时创建多个独立保存/恢复的 preview scope：
[`ConfigMenuPage.cs:84`](../../../../../src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs#L84)、
[`ConfigMenuPage.cs:377`](../../../../../src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs#L377)。

假设真实值为 `O`：scope A 保存 `O` 并应用 `P1`；scope B 保存 `P1` 并应用 `P2`；先 Dispose A 恢复
`O`，再 Dispose B 又恢复 `P1`。所有 scope 都已结束，值却不是原始 `O`。当前 Bootstrap 标题 UI
通常使用单一词法 scope，因此这不是“主菜单必现”；但公共 runtime contract 没有禁止重入或多调用方，
现有测试只覆盖串行 200 次和单次失败。

最小处理可以是每 page 只允许一个 active preview，或维护严格 LIFO/epoch 并只由最外层恢复基线。
测试覆盖 nested LIFO、out-of-order、重复 Dispose、page deactivate、setter/restore failure。

#### 3B. save callback 失败只回滚内存

[`ConfigMenuPage.cs:164`](../../../../../src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs#L164) 在应用 pending 后调用
任意 Mod `save` callback；异常时只通过 setter 恢复旧内存值，却抛出“config values were rolled back”。
[`ConfigMenuTransactions.cs:5`](../../../../../src/DTMAPI.ModConfigMenu/ConfigMenuTransactions.cs#L5) 没有文件事务、
commit result 或 post-write 状态。如果 callback 先部分写盘再抛错，内存与磁盘会分叉。

短期应把 callback 的原子持久化责任写入契约，并把失败状态改成“内存已回滚、持久化结果不确定”；
中期复用 Core 已有 temp + replace 能力或提供结构化 internal save result。不得在未做兼容设计时改变
StableCandidate 公共签名。测试必须包含“先写文件、后抛异常”的 fixture 和重载后的真实结果。

### TD-04：Input System 瞬时失败与设备实例被永久缓存

**当前事实**

- Bootstrap 维护全局 `cachedInputSystemControls` 和 `cachedInputSystemInvalidKeys`：
  [`ReflectedUnityInput.cs:27`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs#L27)。
- `Keyboard.current` / `Mouse.current` 尚未就绪、control 为 null、getter 编译失败或任意读取异常，都会把
  key 永久加入 invalid set：
  [`ReflectedUnityInput.cs:294`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs#L294)。
- 成功后缓存的是具体 Control 对象，而不是类型访问器；后续设备 A→B 不会重新从 `current` 解析：
  [`ReflectedUnityInput.cs:467`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs#L467)、
  [`ReflectedUnityInput.cs:629`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs#L629)。
- `ClearTransientState` 只清 edge/latch，不清 control 与 invalid cache：
  [`ReflectedUnityInput.cs:223`](../../../../../src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs#L223)。

**风险边界**

早期 Input System 尚未初始化、USB/Steam Deck 设备重连或一次 getter 异常后，标题快捷键和 Mod 输入
可能永久走 fallback 或失效。Windows Win32 fallback 会掩盖部分症状；本轮没有完成 Linux/Deck 或
真实设备热替换复现，因此它保持“高可信风险”，不写成已发生玩家故障。

**最小整改与接受**

只永久缓存类型/属性访问器；每次或按 device generation 读取 current device。null 和一次 getter failure
可重试，只有结构性缺少成员才进入永久 invalid。覆盖 null→ready、A→B、一次 throw→recover、F6/F7、
标题输入、Windows 与 Linux/Deck，并守住 warm path allocation 基线。

### TD-05：Harmony target 仍有 count-only 选择

精确 selector 已支持 declaring type、return type 和 parameter type：
[`HarmonyReflectionPatcher.cs:1070`](../../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs#L1070)。
但通用 `FindTarget` 对候选按反射枚举顺序返回第一个匹配，count-only 仍被正式保留：
[`HarmonyReflectionPatcher.cs:822`](../../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs#L822)、
[`HarmonyReflectionPatcher.cs:1169`](../../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs#L1169)。

mandatory 核心残留包括 ReturnHome、部分 Workshop authoring constructor/ResolveUploadPlan、
AgentState OnExit；Frozen fishing compatibility 也仍有残留。当前 Unit 甚至明确断言旧的宽松行为继续存在：
[`Program.cs:14719`](../../../../../tests/DTMAPI.UnitTests/Program.cs#L14719)。

这不证明当前受跟踪 build 已 patch 错目标；风险在于游戏新增同参数数 overload 后可能静默命中错误方法。
整改必须按 native owner 和受跟踪反编译签名分批进行：禁止新增 count-only；仅为已审查的 Frozen
兼容目标保留显式 allowlist。每一批都需 hook-map、两套受跟踪 build 的 target-resolution 和该域最小
game smoke，不能全局机械替换。

## 5. P1 架构、事实真相与工程拓扑

### TD-06：`DtmMod` 生命周期入口倒置

公共基类只声明 `Entry`，却把平台初始化方法 `AttachContext` 暴露为普通 public：
[`DtmMod.cs:3`](../../../../../src/DTMAPI.Abstractions/DtmMod.cs#L3)。Core 已有 friend access，生产调用也只有
Loader：[`DtmApiRuntime.cs:3484`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L3484)。作者或持有实例的
第三方可以重复绑定、传 null 或在 Entry 后改写上下文；这不是应当公开的作者能力。

相反，停用准备与 reason-aware cleanup 依赖字符串命名的魔术反射方法
`DtmApiPrepareOwnerDeactivation(string)` / `DtmApiDeactivateOwner(string)`：
[`DtmApiRuntime.cs:4018`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4018)、
[`DtmApiRuntime.cs:4100`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4100)。清理抛错后实例被保留，
后续 deactivation pass 会重试，但作者回调没有 typed transaction、generation 或幂等契约。

推荐先建立 internal/protected typed lifecycle contract，包含 reason、shutdown 与一次性 transaction token；
既有反射方法保留兼容 fallback。每个 transaction 作者副作用最多一次，平台幂等清理允许重试。
`AttachContext` 改走 internal 初始化，现 public 方法先作为 `[Obsolete(false)]` + hidden ABI shim，下一
breaking release 经 consumer/MemberRef scan 后再决定移除。

### TD-07：加载决策、诊断状态与 Runtime owner 状态存在多重真相

#### 7A. 错误文本反推 status code

`DtmErrorInfo` 只有 owner/message/details：
[`DiagnosticsModels.cs:9`](../../../../../src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs#L9)。
`RuntimeSnapshotFactory` 随后扫描中英文“依赖循环”“MinimumDTMApiVersion”“EntryDll”等文本，反推
`dependency-cycle`、`missing-dependency`、`api-too-new` 等机器状态：
[`RuntimeSnapshotFactory.cs:160`](../../../../../src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs#L160)。
改文案或本地化就可能改变 Manager 状态码。

#### 7B. 依赖与版本规则被重复实现

实际 Loader 在 [`DtmApiRuntime.cs:4196`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4196) 和
[`DtmApiRuntime.cs:4473`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L4473) 判定依赖/版本；所谓
authoritative diagnostic registry 又在
[`ContentManifestRegistry.cs:207`](../../../../../src/DTMAPI.Core/Runtime/ContentManifestRegistry.cs#L207) 和
[`ContentManifestRegistry.cs:325`](../../../../../src/DTMAPI.Core/Runtime/ContentManifestRegistry.cs#L325) 几乎重写
一次。TD-01 正说明 graph 规则很容易只改一处。

#### 7C. 总编排器持有平行 owner 状态

`DtmApiRuntime` 的字段同时维护 discovered/loaded list、snapshot、read-only view、by-ID map、published
instance 与 lifecycle instance，并挂接 lifecycle observation、boundary contract、resource ledger、
save-load coordinator、title-return ledger、owner ledger、demand、author session、UI 和 diagnostics：
[`DtmApiRuntime.cs:26`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L26)。

最小方向不是一次拆成很多“Manager”类，而是先定义 internal immutable `ModLoadDecision`：结构化 code、
stage、owner、dependency/version evidence 与展示参数均由 Loader 产生，Manager/diagnostics 只投影；再用
一个 `ModRuntimeRecord` 派生 discovered/loaded/published/lifecycle view。随后才把 discovery→resolve→activate
和 deactivation transaction 从 facade 中抽出，保持现有 checkpoint/rollback 顺序与 public ABI。

### TD-08：solution、脚本构建与测试拓扑分叉

根 [`DTMAPI.sln`](../../../../../DTMAPI.sln) 仍引用 9 个已不存在的 `testmods/...` 项目。使用仓库本地
`.tools\dotnet\dotnet.exe build DTMAPI.sln -c Release --no-restore` 实测得到 9 个 `MSB3202`，标准
solution build 失败。

真实构建由 [`tools/scripts/build.ps1:11`](../../../../../tools/scripts/build.ps1#L11) 中另一份手写项目数组
驱动；产品、fixture 与 release 又有自己的清单。当前根 solution 既不是开发真相，也不是发布真相。

测试侧同时存在：

- 18,200 行静态 `Program.cs`，以大量 `DTMAPI_UNIT_TEST_FOCUS` if-chain 选择场景；
- UnitTests csproj 直接 Link 多个产品和 Compatibility 生产源码：
  [`DTMAPI.UnitTests.csproj:9`](../../../../../tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj#L9)；
- 自定义 MSBuild target 再构建若干 net48/错误 host fixture；
- optional Compatibility 自己又编译部分相同产品源码。

这并非“测试太多”，而是同一生产文件在产品、Compatibility、UnitTests 中以不同引用和 symbol
环境重复编译，可能出现测试通过的类型并不是最终产品 DLL 中那个类型。

整改顺序：

1. 选一个 machine-readable project inventory 生成/校验 solution 与脚本清单；若 solution 不再受支持，
   就删除而不是保留一个必坏入口；
2. 让标准开发入口与 tracked build 都能从同一 inventory 得到项目集合；
3. 纯函数、resolver、translation、event primitive 逐步迁到标准 fixture/test case；
4. 跨进程、native fixture、ABI 和游戏证据 harness 保留，不为追求测试框架统一而重写；
5. 逐步以编译后程序集或窄 neutral contract project 替代生产源码 Link。

接受门包括：标准 solution/等价入口 clean build、tracked build、按测试名选择、失败定位到单 fixture、
Release/Debug fixture 配置一致，以及删除源码 Link 后测试仍针对最终编译类型。

## 6. P2 冗余与过度设计

### TD-09：过渡 scaffold 已形成长期并行控制面

#### 9A. refactor 配置不是诚实的用户设置

[`RuntimeSubsystemOptions.cs:8`](../../../../../src/DTMAPI.Core/Runtime/RuntimeSubsystemOptions.cs#L8) 仍序列化为
`RefactorScaffoldOptions`，配置文件仍叫 `refactor-scaffold.json`，包含 20 余个 `DTMAPI_REFACTOR_*`
环境变量。`ModLoadTransaction`、`OwnerBoundInput`、`EventHandlerQuarantine` 即使配置为 false 也会在启动时
强制改回 true：[`DtmApiRuntime.cs:1625`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L1625)。
`ShadowResourceLoader`、`RegistryTakesOver`、`ResourceLifecycleTitleAssetRelease` 则只会发布 configured/blocked
诊断，不执行所命名行为。

这些字段混合了三种完全不同的东西：已经成为正确性 invariant 的功能、可选诊断、以及被拒绝的
历史实验。继续把三者当作 feature flag 会让配置看似可控、实际上被忽略或只改变报告。

#### 9B. feature contract 的布尔值不执行 policy

[`GameBridgeFeatureContract.cs:8`](../../../../../src/DTMAPI.GameBridge.DolocTown/Features/GameBridgeFeatureContract.cs#L8)
为每个 feature 手写 requiresSave、allowsTitleScreen、requiresUi、lifetime、autoPause 等 8 个布尔值，
但 `Validate` 只检查 FeatureId 一致：
[`GameBridgeFeatureContract.cs:61`](../../../../../src/DTMAPI.GameBridge.DolocTown/Features/GameBridgeFeatureContract.cs#L61)。
诊断因此可以报告 contract `ok`，却没有证明这些 lifetime 要求被实际 dispatch 执行。

同时 feature construction、lifecycle fanout、Hook install、demand descriptor、实际 TimeSpan/updater、
callback bitmask 各自手写。字符串 cadence（例如 `250ms`、`frame`）与实际配置不共享类型真相。

#### 9C. 生命周期边界被多个 ledger 重复投影

SaveLoaded/ReturnedToTitle 会依次写 SaveLoad coordinator、TitleReturn ledger、LifecycleObservation、
LifecycleBoundaryContract、ResourceLifecycleLedger、public Events、queue 和 object-graph snapshot：
[`DtmApiRuntime.cs:953`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L953)、
[`DtmApiRuntime.cs:1038`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L1038)、
[`DtmApiRuntime.cs:1786`](../../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L1786)。相关 service 合计数千行。

这些 ledger 中有 ISSUE-010、资源 owner、save request 去重等不同价值，不能整批删除；冗余在于边界
事实和格式化 summary 被多次重新构造。应产生一个 immutable lifecycle envelope，再由必要 projection
消费；保留实际保护 invariant 的 ledger，删除只重复计数/格式化的投影。

**整改门**

- invariant 变成常量/无开关；诊断只保留少量明确 opt-in；历史实验一次性读取迁移后退休；
- feature descriptor 要么驱动实际 dispatch，要么退回普通文档，不保留“看起来已验证”的 write-only flag；
- demand route 使用一个 typed spec 产生 updater、Hook/callback mapping 和诊断；不引入 runtime reflection、
  DI 容器或 auto-discovery；
- 收束 lifecycle projection 前必须保留 ISSUE-010 所需 object-graph/GC 证据字段与当前 no-demand acceptance。

### TD-10：Compatibility Host 的源码遮蔽和 Link 编译

Frozen Compatibility Host 本身是经审计批准的 dormant-shipped 兼容策略，不是可直接删除的垃圾代码。
债务在其编译方式：

- Compatibility 项目使用与 mandatory GameBridge 相同的 root namespace；
- 通过 `NoWarn ... CS0436` 全局抑制同名类型冲突；
- 从 mandatory GameBridge 的 excluded `Compatibility/...` 目录 Link heavy executor；
- 还直接 Link DebugConsole 和 MoreEquipmentSlots 产品源码：
  [`DTMAPI.GameBridge.DolocTown.Compatibility.csproj:3`](../../../../../src/DTMAPI.GameBridge.DolocTown.Compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.csproj#L3)；
- factory 再以字符串 service ID 与 `object[]` 构造这些被遮蔽实现。

当前 DebugConsole 拆 partial 时必须同步补 Compatibility csproj Link，已经展示了产品文件拓扑如何直接
改变 Host 编译。全局 CS0436 又可能掩盖非预期的新冲突。

下一步不是删除旧 ABI，也不是动态代理。应建立 host-owned frozen source 或经 package authority 批准的
窄 neutral internal contract，把 blanket warning suppression 缩成零意外冲突，并对 production-file Link
设置明确 allowlist。接受仍须包含 retained MemberRef/ABI、全部 service ID、product-first/compat-first、
dormant/no-demand、cleanup、Doctor 和 package evidence。

### TD-11：Content、CustomEntity 与 Audio/CustomAnimals 的 owner 边界仍未完成

#### 11A. ContentQuery 自建索引，不是 native final table

`ContentQueryService` 递归扫描 active owner 文件，再独立扫描 official local/Workshop root：
[`WorkshopContentInputUi.cs:62`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L62)、
[`WorkshopContentInputUi.cs:215`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L215)。
`TryReadTextAsset(relativePath)` 只取第一个相对路径匹配，没有 source/winner 参数：
[`WorkshopContentInputUi.cs:174`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L174)。item winner
又按自己的 LoadOrder/SourceKind 排序：
[`WorkshopContentInputUi.cs:526`](../../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs#L526)。

它可以作为 Experimental 诊断索引，但不能宣称等于游戏最终加载表。保持冻结，不扩 mutation；先读取
native final table/winning source，测试双 owner 同路径/同 item、坏文件、禁用源，并用最小 game smoke
比较 native winner。

#### 11B. Frozen CustomEntity 仍承担大量活跃维护

[`CustomEntities.cs`](../../../../../src/DTMAPI.Abstractions/CustomEntities.cs) 约 972 行，四个 runtime API 已
Experimental/Frozen/Obsolete；[`CustomEntityRegistryService.cs`](../../../../../src/DTMAPI.Core/Services/CustomEntityRegistryService.cs)
约 1,093 行，仍维护多组 registry/runtime state，但 spawn/attack/drone verb 明确返回
`runtime-creation-blocked`。这不是授权补齐 speculative native host 的理由。

继续把它作为兼容附录冻结；先做真实 consumer/MemberRef scan。只有 family-specific native owner 与
admission 成立后，才在 optional Content Host/ProductNative 中开新 vertical。下一 breaking major 可将
安全 registry/status DTO 与永远 blocked 的 runtime verb 分开审计。

#### 11C. mandatory GameBridge 仍提前构造内容/单产品引擎

`DolocTownGameBridge` 构造即初始化 demand routing 和 Experimental API，`EnsureGameBridgeFeatures` 又构造
CustomAnimals 与 Audio service graph：
[`DolocTownGameBridge.cs:135`](../../../../../src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs#L135)、
[`DolocTownGameBridge.cs:380`](../../../../../src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs#L380)。
真实 Hook/updater 已 demand-gated，因此不能重新宣称“所有 Hook 默认活跃”；剩余债是 schema/resource/
single-product policy 仍物理驻留于 mandatory SharedNative assembly。

后续先做一个 G7 Content Host vertical，证明 ContentPackFor、host version、owner-scoped safe path/JSON/
translation、last-good、disable/restart/resource cleanup；再按真实 owner 拆 CustomAnimals 和 Audio。
Audio 不能整体归为 ContentOwner：AnimalVoice 内容、Manbo 单产品行为和真正多消费者共享 native adapter
必须分开分类。

## 7. 与本地 SMAPI 的净室对照

既有 [0.5.5 收尾后 SMAPI 能力复比审计](20260801-0001-dtmapi-055-closeout-smapi-capability-recomparison.md)
已经记录 SMAPI 1.x→4.x 的版本演化、内容/API/更新能力差距。本 Review 不重复该历史，只把当前技术债
与本地 `develop` 的实现组织方式对照。

SMAPI 源码为 LGPLv3，DTMAPI 为 MIT；更重要的是本仓库规则只允许架构/语义研究。以下都是“借鉴
责任划分”，不是复制类名、代码、API 形状或 Stardew 语义。

### 7.1 值得吸收的组织原则

| SMAPI 本地证据 | DTMAPI 当前差距 | 净室吸收方向 |
| --- | --- | --- |
| `IModMetadata.Status/FailReason/Error` 与集中 `ModFailReason` | DTMAPI 从中英文 error 文本反推状态 | internal `ModLoadDecision` 结构化 code/stage/evidence；UI 只格式化 |
| `ModMetadata` 集中每个 Mod 的状态、实例、API、翻译/更新结果 | Runtime 同时维护多套 discovered/loaded/snapshot/view/instance | 一个 per-owner runtime record 派生视图，不机械删除 Mono/rollback 必需状态 |
| `ManagedEvent` 以 owner、priority、稳定 snapshot、逐 handler 隔离为核心 | DTMAPI EventManager 还承担四类队列、receipt、timing、quarantine、transition 大量计数 | 保留主线程、owner cleanup、隔离/熔断；逐项证明额外策略消费者，合并纯诊断分叉 |
| NUnit 聚焦 fixture/test case | DTMAPI 18,200 行静态 Main + 环境变量路由 | 先迁纯函数/resolver/translation/event 单元；ABI/进程/game harness 保留 |
| `ICommandHelper` 是很窄的 owner 命令注册 | DTMAPI 只有 Diagnostic DebugConsole，普通作者 command 仍 Proposed | 只有稳定可见 host 与真实作者消费者成立时，再做最小 owner-bound command；作弊命令不混入 |
| 大版本清理 deprecated surface，同时提供迁移窗口 | DTMAPI Frozen ABI 与 blocked CustomEntity 长期留在活跃主树 | 先 consumer/MemberRef/warning/migration，再在明确 breaking major 按 family 退休，不按代码量批删 |

对应本地 SMAPI 证据：

- `E:\Python_project\SMAPIlearning\SMAPI\src\SMAPI\Framework\IModMetadata.cs:32-45`；
- `...\Framework\ModLoading\ModFailReason.cs:3-34`；
- `...\Framework\ModLoading\ModMetadata.cs:43-90`；
- `...\Framework\Events\ManagedEvent.cs:58-72,96-120,162-196`；
- `...\src\SMAPI.Tests\Core\ModResolverTests.cs:21-67`；
- `...\src\SMAPI\ICommandHelper.cs:5-18`。

### 7.2 明确不能照搬

1. **运行时 IL rewriting 与生态兼容数据库。** 这会绕开 DTMAPI Strict/Advanced、SDK160、
   SharedNative/ProductNative 准入边界；当前生态规模也不支持其成本。
2. **完整资产拦截、优先级编辑、全局 cache invalidation 和 Content Patcher 式 DSL。** 必须等待 Doloc
   native cache owner、冲突顺序、回滚和 G7 Host 成立；当前正确方向仍是 read-only final-table adapter。
3. **SMAPI Web 式多来源更新、恶意名单与远程兼容库。** DTMAPI `UpdateKeys` 目前明确是 inactive
   schema-only。近期应隐藏/澄清，不应先建 Web 子系统。
4. **捕获任意 game update 异常后继续、自动修存档、强制每日备份。** Doloc/Unity native 状态和
   DTMAPI native SaveGame commit 语义不同；只保留 Mod callback 隔离。修复/备份必须是显式、可选、
   可验证工具，不能推进普通 gameplay sidecar。
5. **Reflection.Emit 动态跨 Mod API proxy。** DTMAPI 当前 exact contract type + owner facade 更可审计；
   只有真实独立编译消费者反复被相同接口副本阻断、且版本安全模型先成立，才值得重审。
6. **Multiplayer、PerScreen、消息路由整套机制。** Doloc networking native owner、session/screen identity
   与断线 cleanup 未证明，保持 Future-reserved。

## 8. 后续完善路线

### Wave 0：先修有界正确性与可靠性

不要先拆大类。先分别建立小 Update，冻结现有行为并修：

1. optional dependency cycle；
2. keybind 不可变快照；
3. MCM preview scope 和“磁盘结果不确定”语义；
4. Input System device generation/retry；
5. count-only Harmony target 分域收口。

输入、UI、Hook 属于高风险面；实施前复用本 Review 的根因块，并按相关 Debug/hook-map/native build
补齐专项 authority。Hook 变更必须先审查当前受跟踪反编译方法体，不能从成功 patch 推导正确目标。

### Wave 1：建立唯一工程与测试入口

1. 用一个 machine-readable project inventory 生成/校验 solution、tracked build 与必要产品清单；
2. 修复或退休当前必坏的 `DTMAPI.sln`；
3. 将纯单元测试从巨型 Program 迁出，保留 ABI/native/process/game harness；
4. 逐步取消 UnitTests/Compatibility 对产品生产源码的直接 Link，优先抽窄 neutral contract/policy，
   不新建第二套 receipt 或 package authority。

### Wave 2：让 Loader 决策成为单一结构化真相

1. 引入 internal `ModLoadDecision` / `DependencyEvaluation` / 当前既定 numeric version policy；
2. Loader、ContentManifestRegistry、Manager、Doctor 投影同一结果，不再扫描文本或重判；
3. 以 `ModRuntimeRecord` 派生 discovered/loaded/published/lifecycle 视图；
4. 在 public `DtmApiRuntime` facade 后抽出 discovery/resolution、activation transaction、deactivation
   transaction；每步保持现 checkpoint、rollback、remaining-root 和 restart-required 语义。

### Wave 3：退休 scaffold，而不是再叠一层抽象

1. 将 correctness invariant 从 `refactor-scaffold.json` 中移出；旧文件一次性兼容读取并记录迁移；
2. 仅保留少量明确诊断开关，删除/忽略被拒绝的实验字段；
3. 用一个 typed demand/feature descriptor 驱动实际 updater/Hook/callback mapping 与诊断；
4. 用一个 lifecycle envelope 驱动必要 ledger projection；保持 ISSUE-010 和 no-demand 证据；
5. 不引入 DI container、auto-discovery、runtime reflection graph 或“所有 feature 都一个万能状态机”。

### Wave 4：按物理 owner 完成内容与生态能力

1. ContentQuery 先接 native final table/winning source；
2. 另经 G7 authority 做一个小 Content Host vertical，不顺手开放 general Advanced；
3. CustomEntity 保持 Frozen/blocked，按 family native owner 决定退休或新建 optional host；
4. Audio/CustomAnimals 按 ContentOwner、ProductNative、SharedNative 实际消费者拆分；
5. 普通 author commands、native-commit-aware data helper、update/advisory 只在真实 host/consumer/version
   authority 成立后按小 vertical 增加；不以“SMAPI 有”为需求证明；
6. Frozen Compatibility 按 consumer/warning/migration/breaking-version family 退休，不作为瘦身捷径。

## 9. 停止条件与后续文档

- 不以“mandatory 行数降到 SMAPI 以下”作为目标或验收；
- 不在结构重构中改变 Strict/Advanced/ContentPack/External 身份、save commit、owner cleanup 或 package
  authority；
- 不把 optional Compatibility/QA 的源码量写成默认玩家加载成本；
- 不从当前无消费者推导 Frozen ABI 可立即删除；
- 不从 UI/diagnostic success 推导 native owner 或 public API stable；
- 不复制 SMAPI 源码、API 命名、Content Patcher DSL、IL rewriter 或 Stardew-specific lifecycle；
- 若一个重构不能说明它删除了哪一份重复真相、保留了哪个 invariant，就不开始该重构。

后续实现按影响更新：

- Loader/Core：owning Update；若状态码或公共语义变化，再更新 public API matrix/author docs；
- Input/MCM/Bootstrap：owning Update，并更新相关 Debug/manual-QA/smoke owner 中真正变化的事实；
- Harmony：owning Update + 对应 hook-map + 当前 build 反编译证据；
- Compatibility：owning Update + retained ABI Review/consumer evidence；
- Content/G7/API：先走 `codex-api-rebuild` 的 native-owner Review，再建 Update；
- 任何实施都不得把完成叙事追加回本 Review，只允许最后增加简短 resolution link。

所需安全条款继续有效：

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## 10. 验证与边界

- 已阅读 required project/onboarding/planning/reference/debug/document-governance/API authorities，以及相关
  2026-07-12 全边界审计、2026-07-22 Frozen Compatibility Host Review、2026-08-01 SMAPI 复比审计；
- 已读取当前 DTMAPI mandatory/optional 项目的实际 MSBuild Compile item 并统计物理行；
- 已逐项核对 Loader、Input、MCM、Bootstrap、Harmony、Runtime diagnostics、Compatibility project 与测试
  源码；
- 已对本地 SMAPI `develop` 的 ModMetadata、FailReason、ManagedEvent、Command、content/update/test 结构做
  只读净室对照；未复制实现；
- 根 `DTMAPI.sln` 的 Release build 已执行并按预期失败：9 个已不存在的 `testmods/...` 项目导致
  `MSB3202`；构建同时显示的 DebugConsole nullable warnings来自审计开始前的现有工作区修改，不归本
  Review 所有，也不据此判定当前提交基线；
- `tools/scripts/build.ps1 -Configuration Release`：全部受跟踪项目编译完成；mandatory 本体项目为
  0 warning / 0 error。UnitTests 项目因直接 Link 用户正在修改的 DebugConsole 源码而产生 16 个 nullable
  warning，但仍编译成功；
- 同一脚本的完整 Unit runner 未通过：`PreviewVersionMetadataIsConsistent` 在
  `tests/DTMAPI.UnitTests/Program.cs:2737` 仍断言旧的 published tree hash，而当前已有工作树中的 Product
  Catalog 已改为另一组 `steamDeliveredTreeSha256` / `playerPayloadTreeSha256`。失败发生在本 Review 写入前
  已存在的 Catalog/测试变更组合，本轮没有修改、回退或替发布 authority 选择任一 hash；因此验证结论是
  “编译通过、完整 Unit 非绿”，不是 Runtime 通过全部测试；
- 邻近本体边界的 focused Unit `phase1-core-cleanup`、`batch5-gamebridge-demand`、`compatibility-host` 均通过；
  它们只保护现有 input/demand/compatibility invariant，不覆盖 TD-01 optional cycle、TD-02 数组强转、
  TD-03 nested preview 或 TD-04 device replacement 新场景；失败 Unit session 已由后续受管 test session
  清理；
- `tools/scripts/check-doc-governance.ps1`：通过，`7429 checks`；
- 新 Review 的 `git diff --check`：通过；本地 Markdown 相对链接检查：0 个缺失；
- 未启动 Doloc Town，未获取 runtime lock，未安装/卸载 Runtime，未修改 Workshop、Official MODS 或
  存档；静态风险不冒充 game smoke。
