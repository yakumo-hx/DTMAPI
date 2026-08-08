# DTMAPI 未处理工作、产品设计与冻结兼容债务总审计

## 记录信息

- 日期：`2026-08-02`
- 状态：`recorded`
- 性质：历史 Review / Update / Debug / Planning / Architecture 与当前未提交文档的只读归并审计
- Source：用户要求梳理历史文档和今天新增未提交内容，整理所有仍未处理的问题，包括冻结代码删除与产品设计
- Implementation owner：尚未建立；0.6.0 实现开始时必须另建一个 `docs/updates/2026/...` 生命周期记录
- 本 Review 不修改 Runtime、Loader、SDK、Mod、Catalog、游戏、Workshop、Official MODS、存档或任何第三方样本

本审计只回答“现在还剩什么、为什么还不能算完成、下一步由谁决定或验证”。
它不是实施授权，也不把任何 `recorded` Review 变成已经接受的产品契约。

## 1. 归并口径与权威顺序

“未处理”仅包括以下四类：

1. 有明确 `open`、`blocked`、`deferred`、`pending`、未通过验收或尚未承接实现的事项；
2. 已实现但缺少其自身要求的玩家、运行时、保存或发布证据；
3. 已冻结但仍有兼容消费者、警告窗口或破坏性版本门，因而不能直接删除的代码/API；
4. 尚未冻结语义、owner、失败行为、保存行为或发布范围的产品设计。

以下内容不因为旧文档仍写着 `open` 就自动进入当前清单：

- 已被后续 verified Update、独立 Review 或精确玩家证据关闭的历史前置；
- 已被后续产品边界取代的旧实现路线；
- 仅作研究种子、没有产品授权的 native-owner 主题；
- 已明确 retired 的产品/API；
- 历史 Review 当时的快照状态。

当前事实采用以下优先级：

1. [`PROJECT.md`](../../../../PROJECT.md)、[Batch 6 身份契约](../../../architecture/batch6-managed-mod-identity-contract.md)、Product Catalog、[公共 API 矩阵](../../../api/public-api-matrix.md)与最新 verified Update；
2. 当前 Debug issue 与专项 Planning roadmap；
3. 今天新增、仍为 `recorded` 的 Review，用于记录新发现和提案；
4. 更早 Review/Update 只保留其尚未被后续事实覆盖的债务。

有一项必须显式纠偏：Batch 6 契约头部仍写“未 Steam 上传”，但
[`20260801-0003`](../../../updates/2026/20260801-0003-runtime-published-metadata-authority.md)
已经记录 Runtime `0.5.5` 的实际 Steam 发布物。发布已完成；契约投影陈旧是文档债务，
不是再次发布 0.5.5 的任务。

## 2. 今天未提交文档的状态

审计开始前已有五个未提交 Markdown 变更；它们属于此前工作，不由本 Review 接管：

| 编号 | 文件 | 当前作用 |
| --- | --- | --- |
| U1 | [第三方 Review 索引](../../../reviews/api/third-party-mods/INDEX.md) | 只增加 U2 导航 |
| U2 | [DolocPlus 1.4.0 / Lua CT 1.9 版本与语义审查](../../../reviews/api/third-party-mods/20260801-dolocplus-140-lua19-version-and-semantics.md) | 提出更人性的版本分层、第三方兼容观察和同语义产品差异 |
| U3 | [0.5.5 收口与 SMAPI 能力重比较](20260801-0001-dtmapi-055-closeout-smapi-capability-recomparison.md) | 记录 Batch 6 后的平台、API、产品与治理余项 |
| U4 | [Doloc Town 1.00 / DTMAPI 0.6.0 兼容根因](20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md) | 记录新基线下确定失效的产品代码和发包链 |
| U5 | [AutoFishing 与小神增强语义、代码量和功能基线](20260802-0001-autofishing-qiuzy-semantic-size-and-feature-baseline.md) | 冻结比较口径并发现 AutoFishing 事务/Hook/语义余项 |

U2–U5 全部是 `recorded`、audit-only。它们没有实施、构建、包装、Doctor、游戏或玩家验收，
因此不能据此宣称任何问题已经修复。

### 2.1 提交前必须消除的两处冲突

1. **版本策略冲突。** U4 仍要求每个新 build 建 exact policy family 并由 SDK 重发九个包；
   U2/U5 则提出 `Verified exact / Compatible drift / Partial drift / Unknown unsafe`
   四级结果以及产品 required/optional capability。后者尚未进入 Batch 6 契约、Catalog、
   schema 或 Loader 实现；当前不能把它写成已选方案。
2. **InstantBite 事务事实冲突。** U2 把当前实现描述成可以 settle 或 restore；U5 的源码审查
   证明它没有保存/恢复已经写入的 Wait 字段，后续步骤失败会留下部分状态。应以 U5 的当前
   代码事实修正 U2；修正文档仍不等于修复代码。

## 3. 0.6.0 当前阻塞链

### 3.1 P0：必须先决定或建立的权威

| ID | 未处理事项 | 当前事实 | 完成门 |
| --- | --- | --- | --- |
| `060-A1` | 版本放行模型 | exact identity 能阻止未知 ABI 进入进程，但把微小漂移也变成“全部产品重构/重发”；能力分层目前只是提案 | 在一个专门设计决定中明确 exact identity 是证据还是唯一加载门；定义 required/optional capability、未知/部分漂移、通知、禁用、开发覆盖与 downgrade 规则，并投影到 canonical contract |
| `060-A2` | public 1.00.00 身份 | 当前接受的 `24456188_test_E861E0` 是 `BetaKey=test` 的最后测试分支，只能作为本地实现基线 | 捕获实际 public 分支；若 bytes/build 不同，建立 `24456188_test -> public` 同口径 reverse baseline 和策略身份；不得把 test manifest 冒充 public proof |
| `060-A3` | 0.6.0 实现生命周期 | U2–U5 都是 Review，没有 owning Update | 实现开始前建立一个 0.6.0 Update，列出最终选择、改动、验证、包、回滚与后续；Review 保持 `recorded` |
| `060-A4` | 身份/能力全链 | 当前 13 个 tracked policy 仍绑定旧 `23762374`；九个公开 Advanced 包在 Entry 前被正确拒载 | 按选定模型更新 policy/schema、Loader、SDK、Doctor、Manager、Catalog、package/release contract 与测试；保留旧 policy 历史；manifest/receipt/package 只能由 SDK 生成 |

`060-A1` 是首要设计节点。直接做 `060-A4` 的 exact 重签可以恢复本次运行，但会把“每次小更新
都要重发所有包”继续固化。直接放宽 hash 又会破坏 Advanced 在程序集加载前 fail-closed 的安全边界。
0.6.0 必须先把这两类责任拆开：整包身份用于可追溯证据，产品 capability 决定哪些功能可继续、
哪些可降级、哪些必须阻断。

无论选择哪种模型，0.6 兼容工作都不得顺手改名或删除 BepInEx GUID
`dev.dtmapi.bootstrap`、registry ID `DTMAPI`、既有 provider/Mod/Workshop identity、
`MinimumApiVersion` legacy alias、配置键、安装 schema 或受管路径。版本策略重做不等于身份重做。

### 3.2 P1：已确定需要改代码的兼容面

#### `060-C1` DebugConsole

- `CostItemAt` 从两参数变为四参数；当前 19-patch 原子安装会因一个目标失效而整体拒绝。
- 天气 owner 从全局字段/方法迁到 room/season group；必须解析当前 group、
  `LocalWeatherType` 与 `DateNow.CurrentWeatherKey`，无法证明 key 时 fail-closed。
- 怪物命令从两参数变为三参数，新增明确 `targetRoom`。
- `EnterUICheck`、`UseTool`、`MoveSpeed` 等方法体已变化，即使签名仍在也必须重放输入、创造、
  移动和动作矩阵。
- 科技点 UI 当前把排序后第一个类别当目标，实际固定落到 `ANIMAL`；需要让玩家明确选择六类，
  并说明“未消费点”会在 reload 后从原生派生状态重算。
- 发电机、怪物、资源三个入口的隐藏状态不是本次兼容 bug；重新开放属于后述独立产品设计。

#### `060-C2` AutoFishing

- `HorizontalMoveFactor` 已删除，当前 owner 是 `MoveModifier.inputMultiplier`；原生 Wait 还观察
  `VelocityX`。旧键盘 fallback 漏掉手柄、改键和原生移动语义，可能把“移动即停止”退化为
  “本轮失败后继续抛竿”。
- Wait、Ready、Pull 方法体变化必须重新审查，不能只替换字段名。
- 最小兼容接受必须使用第五存档，覆盖键盘、手柄、改键、每个 fishing phase 的移动/F6 off、
  标题、再载入和清理。

#### `060-C3` MoreEquipmentSlots 0.3.1 Compatibility Host

- `BodyController.OnAttacked` 新增 `AttackProperties`，旧 Host 不能只接收或忽略一个新参数。
- 新方法体同时涉及 attackability、Thunder 提示、drone death escape、invincibility、
  fishing interruption、hitback 与原生盾牌优先级。
- 需要重新定义兼容 seam，并证明旧 Workshop `0.3.1-dtmapi` 在新版本仍保持这些原生语义；
  这与新版 MoreEquipmentSlots 1.0 的五个事务问题是两条独立工作流。

### 3.3 P1：产品范围和发布证据

| ID | 未处理事项 | 需要的决定或证据 |
| --- | --- | --- |
| `060-P1` | ChestLocatorEnhancer scope | 当前 Postfix 扩大所有 `GetAvailableInventories` caller，不只“设备消耗材料”。选择收窄到 equipment-origin，或正式批准、文档化并测试 agent-nearby / backpack-with-boxes 等更广语义 |
| `060-P2` | 其余公开 Advanced 产品 | Zoom、MoreSaves、ActionSpeed、OneActionComplete、FishBreedingAssistant、AnimalHusbandryProgress、ChestLocatorEnhancer 即使暂未发现源码改动，仍需经 SDK 生成新包并做各自 focused gameplay/load/cleanup 接受 |
| `060-P3` | Manbo | 1.00.00 下完成可听替换、native fail-open fallback、owner cleanup 与延迟 ItemDisplayName demand；不能用“已加载”代替声音行为 |
| `060-P4` | 官方内容变化 | 新基线有 44/195 config table 与 19/133 Yarn 文件变化；应做选定官方内容/ContentQuery/旧内容包的 release validation，不能从启动无报错推导全部内容兼容 |
| `060-P5` | 本地非公开产品 | StrongPlantingGun、Mine 不自动进入首批 public 0.6。若另行纳入，Strong 需 policy/package/acceptance；Mine 还需重审变化后的 `EquipmentBuilder.CreateIndicator` 方法体 |
| `060-P6` | 最终集成 | 证明零 Advanced-reference mismatch、选定九个 Entry、旧 MoreEquipment Host、Manbo、真实存档 load/save、标题/Workshop、清理和无残留进程；最终包身份必须与 public baseline 一致 |

## 4. AutoFishing 尚未冻结的产品语义与实现边界

U5 证明“小神增强”是约 240 行的“玩家先手动开始，Mod 继续当前钓鱼循环”宏；当前 DTMAPI
AutoFishing 约 5,647 行，是 F6 驱动的持久、可配置、可恢复 ProductNative 引擎。代码量差异主要
来自生命周期、事务、兼容、诊断与 QA 边界，不能仅以行数判定需要照抄简化实现。

### 4.1 必须形成产品决定

1. F6 是否继续代表跨轮次的持久任务，还是改成只续接玩家手动发起的一轮；当前建议是保留
   F6 产品身份，但这还不是 canonical 决定。
2. 无体力、未选择鱼竿、无水面、落点失败分别是 `stop`、`wait` 还是 `retry`；每类必须有
   明确 UI/status 和上限。
3. `CastChargeRatio=0` 是刻意的正式默认值，还是历史上为避开过充/落水失败的临时规避。
4. F6 off 是否立即停止 Mod 接管、允许玩家完成当前原生一轮；建议允许手动接管，但必须定义
   哪些临时输入/动画/Wait 字段会恢复。

### 4.2 必须处理的实现问题

| ID | 问题 | 当前风险 | 处理方向 |
| --- | --- | --- | --- |
| `AF-D1` | 22 Hook 全量原子组过宽 | FastAnimations 的一个可选 target 漂移也会阻断基础循环 | 拆成 `BaselineLoop` required，以及 `InstantBite`、`SkipMiniGame`、`FastAnimations` 可选 capability；每组独立 fail-closed/状态/清理 |
| `AF-D2` | InstantBite 不是真实 rollback transaction | Wait 字段部分写入后，提示或 renderer 失败会返回错误但不恢复状态 | 选择完整 snapshot + 逆序 rollback，或明确为不可逆 transition 并在失败后 fault-close/reconcile；不得继续称为可恢复事务 |
| `AF-D3` | 输入 Hook 面可能过宽 | 六个 input getter 中未发现当前 fishing body 消费 `NormalUseItemInProgress` | 以当前 native body/调用链证明每个 getter 的消费者；无消费者者删除，保留者记录 phase 和冲突边界 |
| `AF-D4` | 历史迁移层体量 | facade/router/cache/session/transaction 可能重复，但也可能保护独立 invariant | 逐层列出 sequence、原子安装、重复结算、输入/动画恢复、生命周期清理等 invariant；只合并没有独立 invariant 的层，不迁回 GameBridge |
| `AF-D5` | Bonus 计分/停滞证据 | 每 note 一次输入在静态上合理，但真实 Mono callback、手柄和约 30 秒停滞尚未验证 | 第五存档跑真实 Bonus note 计分、键鼠/手柄、30 秒以上循环、fault 和 cleanup；保留失败日志而不把静态推理当运行证据 |

最小接受矩阵仍包括：基础循环、InstantBite、Skip、Fast、键盘/手柄/改键移动、各 phase F6 off、
可选 target 缺失降级、InstantBite fault injection、Bonus 每 note 只结算一次、标题/再载入/退出。

## 5. 冻结代码删除：哪些不能删、哪些可进入破坏性审计

### 5.1 结论

**0.6.0 是部分冻结 ABI 最早可重新讨论的破坏性边界，不是自动删除日期。**

产品已经迁到 ProductNative，只证明 mandatory Runtime 不再需要默认加载旧执行器；它不证明外部
世界没有旧二进制。当前 Compatibility Host 是 dormant-shipped、按真实旧 API 调用加载，并且 Mono
进程内不可卸载。删除必须从消费者和发布契约出发，不能从“新产品已不用”或“源码看起来旧”出发。

### 5.2 仍有精确消费者的十个 Compatibility 家族

以下家族的重执行器仍由单一 optional Host 承担，当前不可删除：

| 家族 | 主要冻结 API/路线 | 已知兼容对象或理由 |
| --- | --- | --- |
| Action completion | `IActionCompletionApi` | 已发布旧 OneActionComplete 消费者 |
| Action speed | `IActionSpeedApi` | 已发布旧 ActionSpeed 消费者 |
| Fishing automation | `IFishingAutomationApi` | 精确旧 AutoFishing DLL；矩阵明确要求 version system、零消费者、迁移指南和已发布 warning cycle |
| Fish tooltip | `IItemTooltipApi` | 已发布旧 FishBreedingAssistant 消费者 |
| Animal viewer | `IAnimalViewerApi` | 已发布旧 AnimalHusbandryProgress 消费者 |
| Save slots | `ISaveSlotsApi` | 精确旧 MoreSaves DLL |
| Chest locator | `IChestLocatorEnhancerApi` | 已发布旧 ChestLocator 消费者 |
| Equipment slots | `IEquipmentSlotsApi` | 当前仍必须支持 Workshop `0.3.1-dtmapi`；也是 0.6 兼容重点 |
| Camera | `ICameraViewApi` / `ICameraZoomApi` | 精确旧 Zoom `0.4.2-dtmapi` 与 35-MemberRef ABI |
| DebugConsole | 八个 frozen Diagnostic API 与七个旧动作 executor | 精确旧 DebugConsole `0.3.1`；当前新产品不消费这些 API |

Batch 6 当前把十一份精确 retained binary 归入这十个兼容家族。发现新的真实消费者会扩大保留范围，
而不是成为删掉未知消费者的理由。

### 5.3 无 Host executor、但仍不能静默删 ABI 的候选

| 候选 | 当前状态 | 可做与不可做 |
| --- | --- | --- |
| Lamp | disabled/retired compatibility shell，无 native mutation/Hook | 可列为破坏性版本 ABI 退休候选；必须先完成 warning/scan/migration/version gate |
| `IStrongPlantingGunApi` + DTO | warning-bearing shell，无 retained Workshop binary、无 Host executor | 可优先做 fresh consumer scan；tracked legacy source 仍需迁移说明，不能从“无 binary”直接推导可删 |
| `IMachineProductionApi` + DTO | warning/binary-shape shell，无 provider/executor | Mine 不消费该 API；可进入破坏性 ABI 审计，但不能把历史 smoke 当现 provider 证明 |
| 四个 CustomEntity API | Experimental/Frozen，Core provider/registry 保留，native creation blocked | 只能在显式 breaking boundary 下决定 retire/internalize；不得先实现通用 native host，也不得扩大 C# surface |
| 公开 string-input wrappers | 内部 orphan string-input seam 已删除，公开 wrapper 仍是已发布 ABI | 没有 removal goal；不能把 Phase 1 的内部删除投影成公开类型可删除 |

`IAudioReplacementApi` 仍有 Manbo 真实行为，`ICropHarvestingApi` 仍是 Experimental 的窄执行 API，
`IContentQueryHelper` 正等待薄化重设计；它们不是“冻结代码批量删除”的同一集合。

### 5.4 每个删除波次都必须满足的门

1. 先统一版本系统，明确 release/API/file/assembly compatibility 与最低版本语义；
2. 对仓库源码、官方/本地包、可获得 Workshop 订阅物、retained binary 和作者补充包做 fresh scan；
3. 对每个精确消费者完成迁移、更新、撤回或明确继续保留；不能只报告 repository zero；
4. 证明至少一个实际发布版本承载 owner-scoped deprecation warning；历史文档里写了 warning 不等于玩家收到；
5. 发布迁移指南，并让 SDK/Doctor 能识别 Deprecated 使用和替代方向；
6. 由一个显式 breaking-version 决定批准删除，列出各版本轴的影响；
7. 跑完整 ABI diff、retained old-DLL 负向/迁移包正向、Loader/Doctor/Catalog/package 与双加载顺序检查；
8. 更新 public API matrix、Batch contract、Catalog 和作者文档的唯一权威，不保留假 provider/假状态；
9. 只宣称默认加载、程序集或源码的实测变化，不把 Host 删除自动写成仓库/下载/总安装体积下降。

若任一 consumer 仍存在，合理结果可以是继续 dormant-shipped，而不是强行把删除放进 0.6.0。

省略 `CodeModKind` 的第三方 native compatibility lane 也不属于“冻结垃圾代码”。它是当前对旧
DtmMod 的 author-managed、restart-required 兼容入口。除非先形成完整的第三方原生契约，0.6 不得
把省略 kind 重新解释为 Strict，也不得声称能在进程内卸载复制到 `BepInEx/plugins` 的外部 payload。

### 5.5 已经删除/退休、不要重复开工

- `IMotorVehicleApi` 和 SecondMotor 已 retired；未来 Vehicle 必须重新做 native-owner Review，不能复活旧 API。
- AutoFishing 旧 internal primitives、已经证明无消费者的迁移脚手架和部分重复层已在 D.5 删除。
- open/closed generic `DolocGridUI<T>` fallback、旧 bucket 调度真相、死输入诊断和若干 QA-only production seam
  已由后续 Update 删除或迁出；不能再作为“冻结代码待删”重复计数。

## 6. 仍需产品设计或单独授权的工作

### 6.1 P1：MoreEquipmentSlots 1.0

MoreEquipmentSlots 的物理 ProductNative 迁移已经实现，但新版 `1.0.0`、Product-v3 迁移和发布继续
`publication-deferred`。除了 0.6 旧 Host attack seam 外，以下五个事务 P1 仍未关闭：

1. production mail reader 会把部分 malformed/unreadable authority 误判为零；
2. incoming `CostItem` 只信返回 Boolean，没有严格核对 before/after；
3. 不可读 native-save fingerprint 都可能投影为空串，从而误判相等；
4. cold recovery 用粗粒度 count growth 代替精确 destination/count/post-save fingerprint；
5. 延迟 count-only reconciliation 会被普通的同物品玩法干扰。

这些问题涉及保存提交、owner/orphan recovery 和物品守恒，必须沿用 `PROJECT.md` 的 native SaveGame/
SaveSaved 语义与 disposable fixture；不得借 0.6 兼容波次把 1.0 偷渡发布，也不得先抽象成
`IProtectedStorageApi`。

0.6 旧 `0.3.1` Host attack seam 与这五项 Product-v3 事务债务必须在 owning Update 中明确分界：
默认解释是前者只恢复精确旧消费者兼容，不自动关闭后者；如果 0.6 要同时改动 mail/save/recovery，
就必须显式扩大验证矩阵，不能在两份文档中分别宣称“只改 Host”和“事务已一并关闭”。

### 6.2 P1：G7、CustomAnimals 与 AnimalPack

- `ContentPack` 身份存在，但通用 `ContentPackFor -> Content Host` 仍 blocked。
- 第一条 G7 只能是一个 domain-specific vertical：host min version、owner-scoped safe path/JSON/翻译、
  deterministic ordering、pack isolation、last-good、enable/disable/restart/no-demand；两个独立内容包
  证明后才推广。
- CustomAnimals 的物理方向是 optional Content Host，不是把旧 C# CustomEntity runtime API 做实。
- AnimalPack 已有保留身份但无 canonical source；Catalog blocker 仍是 `AssetProvenance`、
  `EconomyModel`、`OldInputDuplicatePreflight`、`CanonicalSourceNotCreated`。
- 四个物种角色、隐藏产物、processor、Oil 关系、销售/加工、ID、经济和旧输入迁移都未冻结。
- ShellCrab 的 AssetBundle prototype 保持独立，不能混入 AnimalPack/G7 证明。

G7 不开放 general Advanced；general Advanced 也不能反向替代 G7 的内容隔离责任。

### 6.3 P1/P2：Audio 与 Manbo

- `IAudioReplacementApi` 保持 Experimental；全局 event、平台 WAV fallback、Wwise bus/3D、owner unload
  与多 Mod 仲裁仍不足以稳定化。
- ISSUE-016 仍缺 Manbo + 延迟 ItemDisplayName demand 的精确玩家复验；1.00.00 另缺 audible replacement/
  fallback/cleanup smoke。
- 下一版 bridge 需要 typed physical install result、physical/behavioral status 分层、明确 scheduler 语义、
  owner/事件级错误与 native fail-open。
- Manbo 转成 data-only ContentPack、继续当前 lane 或以后退役，必须等待 G7 和真实迁移证据。
- BGM 是独立 blocked 研究：当前没有 loop/STOP/callback/RTPC/bus/mixer owner 与真实消费者。

### 6.4 P2：Mine 与 Oil

- Mine 的技术 ProductNative 已 verified/closed；公开仍被独立贴图和移除 runtime 2x 阻断。
- Mine 的 economy、Oil output 稀释与配方反馈环仍需产品决定；新基线的 `CreateIndicator` 方法体需在发包前复查。
- Oil 保持 DLL-free 官方 JSON，不重新添加 API/Hook/Host。weight/fuel/价格/掉落仍是 prototype economy。
- 若 Oil 要成为独立 extra roll 而非稀释 coal/amber LUT，需要新 native-owner Review，不能沿用旧原型结论。

### 6.5 P2：DebugConsole world actions 与 UX

发电机、怪物、资源入口当前隐藏。任何一项重新开放前必须具备：具体对象搜索/分页、数量、当前
场景可用性、位置/范围、二次确认、实际结果验证、明确失败，以及返回标题、不保存退出、正常保存、
保存失败、冷重载和 Compatibility 双 owner 审查。详情由
[world-actions roadmap](../../../planning/20260801-debugconsole-world-actions-roadmap.md) 所有。

控制台 history/search/danger confirmation 与 Manager 玩家信息简化是延期 UX，不是 0.6 Hook 兼容的
附带任务。

### 6.6 P2/P3：其他有边界的候选

| 主题 | 当前处理方式 |
| --- | --- |
| MoreSaves UX | 固定 12/6 已关闭；存档显示名、任意数量、滚动/分页是新的高风险产品设计，历史 18/24 smoke 不是承诺 |
| Panorama / 高分辨率拍照 | `IPanoramaCameraApi` 仍 Proposed；必须与 playable Zoom 的 scale/恢复分开 |
| CropHarvesting / AutoHarvest | AutoHarvest 是 `NeverPublish` demand sample，不是第十三产品；Crop API 仍缺 room traversal、crop family、multi-owner、tree owner 与真实田地边界 |
| 通用 protected storage | 仅是讨论名，未声明、未准入；先关闭 MoreEquipment 五项并证明第二个独立消费者/共同 authority |
| StrongPlantingGun nested storage | 当前只承诺 SaveLoaded 扫描 backpack top-level；nested container/warehouse 立即修复是条件性 P3，新需求需 ProductNative 设计与 fixture，不是现有缺陷 |
| game-clock cadence | 与 DebugConsole global simulation scale 语义不同；只有新 ProductNative Review 才能进入 |
| cross-Mod service vertical | 可用一个真实 provider/consumer 对验证 optional dependency、版本、双 load order、failure、deactivation、stale facade、restart；fixture 不准入新产品 |
| Multiplayer、Pets、Vehicles、NPC、drone、地图生成、远程武器 | research-only；Pets/Vehicle 必须拆域，旧 MotorVehicle 不复活，native-owner library 条目不是产品 backlog 授权 |

## 7. 平台与公共 API 未处理项

### 7.1 P1 治理/契约

1. `IModRegistry`、`ITranslationHelper`、`IDtmConfigMenuApi` 的源码 stability attribute 与
   public API matrix 投影不一致；必须选择 canonical 状态并机械对齐，不能让源码和文档各自成立。
2. 通用 CodeMod deactivation contract 未决定：是否正式支持 `IDisposable`、清理顺序、失败/
   remaining-root/retry、同进程 re-enable 与 restart 边界。
3. 多处 suffix-stripping `System.Version` 比较需要统一 package-version policy；
   `MinimumGameVersion` 要么绑定可靠游戏身份，要么保持明确 advisory。
4. Hello/ConfigMenu/AutoHarvest 教学样例仍是旧 manifest，应由当前 SDK 刷新到现代 Strict identity；
   不得手改生成身份。
5. allocation counter 当前不可用，whole-runtime/per-unit 数值预算未建立；新增 recurring shared host 前
   应恢复有效测量路径和基线。
6. 0.6 的 Runtime release version、file version 与 Assembly compatibility version 尚未决定；
   `0.5.5` 实际仍使用 assembly identity `0.5.3.0`，不能在打包时临时推导。
7. `UpdateKeys` 仍 inactive，update/advisory/deprecation runway 不是已实现的平台服务。
8. 默认 synthetic retained-ABI contract 当前主要锁 Fishing/Lamp；其余十家族的一致性依赖显式
   retained artifact lane。若 0.6 继续承诺十家族，必须把公开可获得 consumer 的成员门变成默认
   Release 不会漏跑的机器合同，或记录同等强的统一替代门。
9. Doctor 的部分迁移文案已与矩阵冲突：不能建议从 Frozen `ICameraZoomApi` 迁到同样 Frozen 的
   `ICameraViewApi`，也不能把 fishing 的“0.5.5 compatibility-only”写成 0.6 自动删除承诺。

### 7.2 已有 roadmap、尚未实施的 Next Runtime 切片

以下四项必须保持独立设计/验收，不合成新的大一统状态机：

1. ContentQuery 冻结能力增长，薄化为官方 final table + winning source；先回答 winner、覆盖、主菜单
   not-ready、邮件消费者与旧 `FindAssets/TryReadTextAsset` 兼容方式。
2. lifecycle 逐项决定 `SaveDataReady`、native load returned、scene ready、exactly-once、cleanup retry、
   title stable 和 transaction identity；平台不接管产品玩法状态机。
3. frame owner 从 InputSystem 恢复角色迁到 native gameplay drain / PlayerLoop，只有完成 input/focus/title/
   zero-duplicate 验收后才物理删除 F1 订阅/恢复代码。
4. FileMonitor 建立真实 MinimumLevel；Info/Debug/Trace 分层，但 Warning/Error 与完整异常/最小因果
   上下文永远可见。

Audio bridge roadmap 是第五个独立方向，不应混进上述四项。

### 7.3 需要单独 authority 的平台方向

- 第一个真实产品 API provider/consumer pair；
- native-commit-aware owner data service；
- locale change、missing/malformed translation，再按真实需求扩参数/plural；
- author command 与 cached Strict reflection helper；
- update/advisory/compatibility/deprecation runway；
- 只有在两个独立消费者共享同一 native owner 后才扩 world/display/content events；
- general third-party Advanced lane、真正程序集隔离、Content Patcher 级 DSL、生态兼容数据库/发布平台
  属于 Batch 8/独立项目。

当前 StableCandidate/Experimental 并不自动等于 bug。Config migration、registry、translation、
GameLaunched、ConfigMenu、Diagnostics read side 只有满足矩阵中的真实采用/QA 门才可 promotion；
Events/Input/UI/Workshop/Audio/Crop/CustomEntity 等保持 Experimental 是有意的兼容承诺，不应为追求
“全 Stable”而集中扩面。

## 8. Debug、运行证据与文档状态债务

### 8.1 当前仍是真问题或明确缺证据

| 记录 | 当前归类 | 仍需处理 |
| --- | --- | --- |
| [ISSUE-010](../../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md) | open，真实长期风险 | 标题一小时+十次进档与当前短窗口已通过，只关闭已测路径；长期 gameplay、房间/UI/钓鱼循环的 Mono/Unity 原生 crash class 与量化预算仍 open。若复现需有 native dump/root evidence |
| [ISSUE-011](../../../debug/issues/ISSUE-011-20260623-short-run-native-crash.md) | open / evidence-improved | 需要固定版 clean smoke 与 fresh crash/no-crash package；验证 EventSystem 不再反复报 Input System not initialized、Y console 仍可用、collector 有 dump 或明确 missing reason |
| [ISSUE-016](../../../debug/issues/ISSUE-016-20260731-audio-hook-idempotent-status-republish.md) | mitigated | focused Unit 已过；仍缺 corrected Manbo + delayed ItemDisplayName 的玩家序列。0.6 的 1.00 audible smoke另算 |
| [`20260731-0002`](../../../updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md) | implemented / not-run，后续手测有通过事实 | 2026-07-31 用户已确认本体/三个附加功能/组合在约 489 秒窗口正常，但 Update 没有 resolution 投影，且该窗口未记录最终退出；对 1.00 新基线，U5 的 Bonus、手柄、fault 与完整清理矩阵仍未跑。应先对账旧证据，再只补新基线缺口 |
| [`20260731-0003`](../../../updates/2026/20260731-0003-contentquery-lifecycle-frame-log-closeout.md) | implemented / not-run | 当前 C1/F1/G1 的 bounded player checks 没有在该 Update 中完成；后续 broad green 只有记录了同一断言才可作 resolution |
| [官方教程实境审计](../../../updates/2026/20260715-0003-official-tutorial-real-environment-audit.md) | in-progress / partial | 93-entry native-table expectation 与 representative player-visible behavior 未跑；还要决定教程 `equipment0` 与 sample `decoration0` 的公开纠正口径 |

### 8.2 监控或状态对账，不应直接变成 0.6 代码任务

| 记录 | 正确处理 |
| --- | --- |
| ISSUE-001 | Steam launch mitigation 已长期工作；要么用后续合格 evidence + 用户无 popup 事实关闭，要么明确 DirectExe 永久 unsupported。不是 Runtime Hook bug |
| ISSUE-002 | HookProbe 隔离已实现，后续多次真实热键手测可能已经满足事实；需要对账/关闭记录，不应再改 input 代码 |
| ISSUE-004 | 仅在 Steam 无需完整重启仍可重复恢复后关闭；无复发时保留 monitoring，不做猜测修复 |
| ISSUE-005 | 四个 0.2.4 条目已有实现/自动证据，SecondMotor 又已 retired；当前 `open` 主要是陈旧 ledger 与少量历史手动 polish，不能据此复活车辆产品 |
| ISSUE-012 | restart-resolved；未来只改进 cold-Steam wait、late PID 和 privacy-bounded process env probe，不证明 Runtime 需要修复 |

ISSUE-003 是明确 deferred 的旧 overlay 产品方向；ISSUE-006–009、013–015、017 已 verified。
其中 issue README 仍把 ISSUE-015 写成 `mitigated`，而 issue 文件自身和 `132103`/2026-08-01
证据已经 `verified`，需要修正索引投影。

### 8.3 文档真相漂移

1. Batch 6 contract 仍写“Steam upload 未执行”，与 verified published artifact authority 冲突；
2. [`20260801-0002`](../../../updates/2026/20260801-0002-workshop-upload-release-closeout.md)
   第 176 行引用不存在的 `20260731-0006`，实际 owning Update 是 `20260731-0003`；
3. public API matrix 与三个源码 stability attribute 不一致；
4. debug issue README 的 ISSUE-015 状态落后于 issue 文件；
5. 0.5.5 prerelease roadmap 和 lightweight roadmap 尾部仍保留“等待发布/未上传”的历史快照，后续
   读取者必须走 20260801-0002/0003；历史快照不应被重写成当时已发布，但可增加 resolution link；
6. smoke matrix 中某些“等待最终用户确认”的 row 已被 20260801 玩家证据覆盖。旧 row 的运行结论
   不应改写，但活动索引/路线不能继续把它当 blocker。
7. Catalog 目前只有 Runtime 记录 `currentPublishedArtifact`；若十个产品也已实际上传，需要分别补
   post-publication authority。在此之前不能从“授权上传/0.5.5 已关闭”推导旧产品消费者已经消失。
8. API matrix 对 Lamp 仍称 Doctor migration diagnostics 为 later work，但 `DeprecatedApiGuidance` 已存在；
   当前债务是校正文案和验证覆盖，不是从零实现 deprecated scanning。
9. `20260731-0002` 仍是 `implemented/not-run/open`，而后续 Manual QA 记录 AutoFishing 复测通过；
   应区分已通过的旧候选行为、未记录的退出/状态收口，以及 1.00 基线重新兼容，不能重复全跑也不能
   直接改成全部 closed。

这些属于文档治理修复，不是再跑游戏或重做产品的理由。

## 9. 第三方样本观察：保留研究价值，不自动成为 DTMAPI backlog

U2 的第三方结论应保持 compatibility research 分类：

- DolocPlus 1.4.0 的 Fish Analyzer 天气、NPC Contacts helper、AFK Fishing movement、
  ElectricEel 两个 Hook 与 Synthesizer 参数存在确定 surface break；
- DolocPlus 版本解析没有安全处理非数字版本，`PatchAll` 失败后 toggle 可能仍显示启用，controller
  update 缺少单项故障隔离；
- Lua CT 1.9 的 `<50%` 全局成功阈值只是粗粒度降级，不证明关键功能安全；
- CT Easy Fishing 机器码偏移仍 runtime-unverified；current weather 调用约定不兼容，不能作为
  DebugConsole fallback；tech-point pointer chain 只有静态可信度，未消费点 reload 后会重算。

这些事实可以启发 DTMAPI 的 movement/weather/capability 设计，但不授权复制第三方代码，也不要求
DTMAPI 修复第三方 binary。

## 10. 已关闭或被取代的历史项

后续任务不得把以下内容再次列为当前未完成：

- Runtime `0.5.5` 与 Steam publication 已完成；当前工作是 0.6 兼容，不是再发布 0.5.5。
- 有界 Advanced lane 已实现；未开放的是 general third-party Advanced authoring。
- AutoFishing、OneActionComplete、ActionSpeed、FishBreedingAssistant、AnimalHusbandryProgress、
  MoreSaves、ChestLocator、StrongPlantingGun、Zoom、Mine、DebugConsole 的历史“未准入/拆分 blocked”
  已被后续 verified evidence 关闭。
- MoreEquipmentSlots 是唯一产品状态例外：物理迁移完成，但 1.0 publication 与五项事务 P1 未关闭。
- Mine 早期 false configurable power、Enabled 注册、unused mineral switch、recipe/tech restore 已由后续
  实现关闭；剩余是贴图/2x/经济与新基线方法体。
- MoreSaves 18/24 pager 是历史 smoke/future UX，不是当前 1.0 要求。
- DebugConsole admission、owner 与 2026-07-27 lifecycle transaction 已关闭；当前开放的是 1.00 兼容、
  tech UX 与隐藏 world actions。
- SecondMotor 与 `IMotorVehicleApi` 已退休；ISSUE-005 的旧车辆条目不能复活它。
- CustomEntity 的旧“实现通用 native creation”已被 Frozen/no-host 决定取代；当前只保留 breaking retirement
  或未来逐 family native-owner 项目。
- 旧 roadmap 中“Advanced lane 完全不存在”“所有 ProductNative 尚未迁移”“0.5.5 RC 未完成”等均是
  历史阶段，不是当前 backlog。

## 11. 推荐执行顺序

1. **先做 0.6 版本/能力设计决定**：关闭 `060-A1`，同时纠正 U2/U5 与 U4 的权威关系。
2. **捕获 public authority 并建立 0.6 Update**：关闭 `060-A2/A3`。
3. **处理确定兼容代码**：DebugConsole、AutoFishing movement/body、旧 MoreEquipment Host；同时冻结
   AutoFishing 与 ChestLocator 产品语义。
4. **生成而非手写九个包**：完成 policy/SDK/Doctor/Catalog/package 链，再做七个“无源码变化”产品、
   Manbo 和官方内容 focused 验收。
5. **做一次最终集成接受**：真实存档、标题/Workshop/save、owner cleanup、public identity、无残留进程。
6. **MoreEquipment 1.0 单独处理**：五个事务 P1 和保存矩阵通过后才讨论发布/通用存储。
7. **再排产品设计**：G7/AnimalPack、Audio/Manbo、Mine/Oil、DebugConsole world actions。
8. **冻结 ABI 删除另立 breaking goal**：按 family 跑 fresh scan/warning/migration/ABI gate；不与紧急
   0.6 正式版兼容混在同一不可回滚变更中。
9. **general Advanced、CustomEntity retirement、Batch 8 和 research-only 域继续保持单独授权。**

## 12. 本审计的验证边界

- 阅读并交叉核对 required project/Debug/API/document-governance authorities；
- 审计当前未提交 Markdown、最新 Batch 6/API/Planning/Update/Debug 状态和明确 blocker；
- 未运行 build、Unit、package、Doctor、game smoke、save matrix 或 Release suite；本任务没有生产源码改动，
  这些验证不适用；
- 本 Review 只创建归并事实，不关闭其中任何实现、产品、兼容或运行证据问题。
