# Batch 6 AutoFishing Advanced Pilot 前置审查

## 记录状态

- 日期：2026-07-20
- 状态：`recorded / G2 synthetic authority accepted / AutoFishing pilot admitted for implementation only / G3-G6 open`
- 性质：AutoFishing 作为唯一真实 Advanced CodeMod pilot 的 native-owner、物理归属、兼容与验收前置审查；不是实现完成、运行通过或发布授权
- 范围：AutoFishing 产品源码、fishing-only GameBridge/Abstractions/QA/Compatibility、独立 Advanced reference policy、Author SDK/Core/Doctor registry、G3/G4/相关 G5-G6
- Source：用户要求在 G2 synthetic vertical slice 后仅用 AutoFishing 作为唯一真实产品 pilot，完成 G3/G4/相关 G5-G6，同时保持其他真实产品、G7 与 0.5.5 发布阻断
- Owning Update：[20260720-0008 Batch 6 AutoFishing Advanced Pilot](../../../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)
- 上游权威：
  - [Batch 6 Managed Mod Identity Contract](../../../../architecture/batch6-managed-mod-identity-contract.md)
  - [Batch 6 G2 Advanced Synthetic Vertical Slice](../../../updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md)
  - [AutoFishing SMAPI Rehome Boundary Review](../../api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md)
  - [Batch 6 Boundary Correction Prerequisite](20260719-0012-batch6-boundary-correction-prerequisite.md)

G2 已证明的正向范围仅是 SDK 生成的 `DTMAPI.AdvancedFixture`、tracked policy `doloctown-23762374-g2-v1` 与 synthetic runtime/ownership receipts。该 policy、reference surface、hash 和收据不得被解释成通用 Advanced 作者通道，也不得直接改名或复用于 AutoFishing。本 Review 只准入一个新的、独立且 UniqueID-bound 的 AutoFishing policy，并只授权 `Yuuka.DTMAPI.AutoFishing` 这一真实产品进入实现。

本 Review 创建时没有修改 Runtime、SDK、GameBridge、产品源码、包或游戏目录，没有安装/启动 Doloc Town，没有取得 Runtime lock，也没有运行构建、测试、第五存档或 GC 矩阵。所有实现与验证状态由 owning Update 后续如实更新。

## 一、审查裁决

| 边界 | 裁决 | 约束 |
| --- | --- | --- |
| G2 synthetic policy | `KEEP IMMUTABLE` | `doloctown-23762374-g2-v1` 继续只服务 `DTMAPI.AdvancedFixture`；不得扩为真实产品 policy。 |
| AutoFishing pilot | `ADMITTED FOR IMPLEMENTATION` | 唯一准入真实产品；先保持产品行为、配置和身份，再改物理归属。 |
| AutoFishing native implementation | `ProductNative / MOVE-PRODUCT` | 状态机、Harmony patch、native cache/transaction/input/animation/cleanup 进入产品 DLL。 |
| first-party fishing primitive seam | `DELETE` | 单消费者 friend/provider/facade/session/demand 脚手架不再留在 mandatory Runtime。 |
| frozen `IFishingAutomationApi` | `KEEP-COMPAT` | 0.5.5 不静默删除；仅兼容旧二进制，不得成为新产品实现通道。 |
| generic Runtime/Event/Owner/MCM | `KEEP-PLATFORM` | 只保留与 fishing 无关的通用机制及经证明的 SharedNative route。 |
| product QA | `MOVE-QA` | 进入可选产品 QA/测试边界，不进入玩家 mandatory Runtime。 |
| G3/G4/相关 G5-G6 | `OPEN` | 必须由源码、包、收据、第五存档与长期矩阵共同闭环。 |
| 0.5.5、其他产品、G7 | `BLOCKED` | 本 pilot 不解除发布 stop，不准入其他真实产品，也不实现 Content Host。 |

## 二、native owner 与 live state holder

Doloc Town 继续拥有真实 fishing 状态与副作用；AutoFishing 只拥有产品策略和对这些 native owner 的私有适配。当前 build `23762374` 的相关责任面为：

- `BodyController.UseFishRod`、当前选择的 fishing rod 与可用 pool：抛竿准入和 native 使用入口；
- `AgentStateFishingReady`、`AgentStateFishingCast`、`AgentStateFishingWait`、`AgentStateFishingPull`：Ready、cast、wait、bite/reel、pull 的原生状态机；
- `AgentStateFishingWait.RollFish`、`NextState` 与 native state overwrite：咬钩、收竿和结果路径；
- `FishingGameScrollBar.StartGame`、`UpdateGame`、`StopGame` 及 note/status state：可见小游戏及完成结果；
- `DolocUserInput` fishing/tool/item getters：AutoFishing 所需的 synthetic input 观察/覆盖；
- `FishRodRenderer.CastHook`、`Pull`、`PullCancel`，Hook body/velocity/gravity 与 animator：飞钩、拉回、取消和动画加速；
- native energy configuration/check/cost 路径：抛竿和收竿前后的能量安全与实际扣除；
- 当前 Agent/state manager、fishing cache/proto、minigame handle、animator 与 Hook `Rigidbody`：live state holders。

产品必须在这些 holder 的生命周期边界恢复自己改动的 input、animation speed、pending cast/reel、cached handle 与临时 native transaction。SaveLoaded、ReturnedToTitle、重新进入存档、F6 disable、异常 Entry/callback、Manager post-load disable 和进程退出是不同边界，不能只用一个 `UnpatchAll` 冒充完整恢复。

`AgentStateBase.OnExit` 目前被 ActionSpeed 与 fishing 共用。迁移后 fishing patch 必须由 AutoFishing canonical Harmony owner 自行安装；mandatory GameBridge 的共享 route 只保留 ActionSpeed 或真实全局不变量所需部分。一个共享目标方法不自动证明 fishing 产品行为属于 SharedNative。

## 三、独立 UniqueID-bound Advanced policy 与 registry

### 3.1 不可复用 G2 policy

当前 `doloctown-23762374-g2-v1`：

- policy schema 1 没有 UniqueID 绑定；
- SDK、Core 与 Doctor 当前各自只嵌入/接受一个 tracked policy；
- compiler surface 只为 synthetic fixture 的受限目标建立；
- PROJECT/AGENTS/G2 receipts 明确只证明 `DTMAPI.AdvancedFixture`。

若 AutoFishing 直接声明该 policy，即使包和 reference hashes 正确，也会把“仅 synthetic”事实偷换成任意真实产品可用的 lane。若产品通过 `typeof(DolocAPI).Assembly` 加反射，而 policy 又不绑定 UniqueID，其他手写 Advanced 产品也可复用同一收据面。这条路径被拒绝。

### 3.2 新 policy 最小合同

新增 policy 使用独立 ID：

`doloctown-23762374-autofishing-v1`

它至少必须绑定：

- exact manifest UniqueID：`Yuuka.DTMAPI.AutoFishing`；
- Steam public game build：`23762374`；
- exact `Assembly-CSharp.dll` 相对路径、长度与 SHA-256；
- exact Runtime-provided `0Harmony.dll` 相对路径、长度与 SHA-256；
- `copyLocal=false`；
- AutoFishing 独立 compiler reference surface；
- canonical Harmony owner：`dtmapi.mod.yuuka.dtmapi.autofishing`；
- SDK-generated manifest、entry payload 与 `dtmapi-advanced-references.json` 的原子绑定。

新 policy schema 应显式增加 exact UniqueID binding。为保持 G2 policy bytes/hash/receipts 不变，registry 必须同时支持：

- immutable schema-1 G2 policy，通过固定 registry row 绑定 `DTMAPI.AdvancedFixture`；
- schema-2 AutoFishing policy，通过 policy 本身或等价的 hash-bound registry row 绑定 `Yuuka.DTMAPI.AutoFishing`。

SDK、Core 和 Doctor 都必须按 `policyId -> exact policy record` 查找，并同时比较 manifest UniqueID；不得继续使用单一 `TrackedPolicyId` 常量、单一 embedded resource 或“第一个 policy”语义。Manager 只展示 Core 已验证的 classification，不自行从产品名、DLL 名或路径推断。

### 3.3 compiler surface 与边界

第一步优先搬移现有 reflection/cached-delegate 实现，不同时重写所有 fishing mechanic。AutoFishing compiler surface 可以只提供：

- `0Harmony` 的受跟踪编译引用；
- assembly identity 为 `Assembly-CSharp` 的最小 `DolocAPI` anchor，使产品通过 `typeof(DolocAPI).Assembly` 得到当前游戏程序集；
- 不复制、不打包官方 DLL、Unity、Harmony 或 BepInEx 依赖。

产品随后在该 exact assembly 内解析并缓存 fishing types/members。该 surface 是 admission/provenance 与最小编译面，不是安全沙箱；反射能触达的语义仍由本 Review、源码审查、native-owner inventory 和包/运行 receipts 约束。

Strict lane 的 `SDK160` 必须原样保持。新 policy 不允许作者手工发明 `CodeModKind=Advanced`、reference receipt 或 package；唯一权威产物来自 Author SDK build/pack/deploy。

## 四、文件与责任处置

### 4.1 `MOVE-PRODUCT`

以下 fishing-only implementation 从 `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/` 移入 AutoFishing 产品源码，允许先做低风险搬移再在产品内折叠：

- `FishingAnimationController.cs`；
- `FishingAnimationNativeCache.cs`；
- `FishingAutomationHookBridge.cs` 的产品 patch/activation/callback 责任；
- `FishingInputOverride.cs`；
- `FishingMiniGameNativeCache.cs`；
- `FishingNativeAccessors.cs` 的 fishing-specific accessors；
- `FishingNativeAdapter.cs`；
- `FishingNativeEnergyGate.cs`；
- `FishingNativeStateCache.cs`；
- `FishingNativeTransactionCache.cs`；
- `FishingPrimitiveHookRuntime.cs` 的 callback/state-machine 责任；
- `FishingRuntimeComponents.cs` 的 scheduler、product diagnostics、publication gate 与 transient lifecycle；
- `FishingVisibleReelInputState.cs`；
- `FishingPrimitivesService.cs` 中的 state machine、snapshot、sequence check、native transaction、input/animation policy 与 hook router；
- `DolocTownHookCallbacks.cs` 中所有 fishing callback behavior；
- fishing 的 `AgentStateBase.OnExit` patch；
- `IFishingHookRuntime.cs` 与 `IFishingHookCoordinator.cs`，若迁移后仍有测试价值则改为产品私有 seam，否则删除。

`FishingNativeAccessors.cs` 不能因为 generic UI 也调用少量成员工具就整文件留在 GameBridge。只把真正中性的 `FindMember`、static/object getter builder 等最小 helper 提取为 `KEEP-PLATFORM`；fishing type/member/cache 进入产品。Compatibility 若需要同类能力，持有自己的私有副本，不依赖新产品。

`FishingPrimitivesService.cs` 不应原样跨程序集复制。删除 `AcquireSession(IManifest)`、provider registration、owner facade 与跨程序集 lease protocol，产品直接持有唯一 session/state。内部接口只在能改善产品测试时保留。

当前产品 `FishingDecisionEngine.cs`、配置、i18n、MCM option registration、F6 input policy、0.25 秒 recast、manual movement cancel 与产品日志继续留在产品，并与 native implementation 合并为一个 product-owned lifecycle。

### 4.2 `DELETE`

产品新路径可构建且 compatibility 已分离后，删除以下旧 restriction scaffolding：

- `src/DTMAPI.Abstractions/FirstPartyFishingPrimitives.cs`；
- `src/DTMAPI.Abstractions/AssemblyInfo.cs` 中 `InternalsVisibleTo("AutoFishingMod")`；
- `OwnerBoundGameBridgeApis.ForFirstPartyFishingPrimitives`；
- `FirstPartyFishingPrimitivesFacade`、`FirstPartyFishingSessionFacade` 及 handler/session tracking；
- 产品的 `GetApi<IFirstPartyFishingPrimitivesApi>("DTMAPI.GameBridge.DolocTown")` acquire/retry；
- 产品对 `DTMAPI.GameBridge.DolocTown` 的 required dependency；
- GameBridge 的 fishing product field、construction、first-party provider registration、primitive lifecycle forwarding 与 product callback-service dispatch；
- primitive/legacy arbitration、primitive activation checkpoints 和 product demand plumbing；
- `GameBridgeDemandRoutes.FishingAutomation`、`FishingLegacyUpdater` 的产品语义、fishing retained-callback bits、fishing/BaseExit co-owned demand、fishing hook-readiness 与 fishing-only no-demand counters；
- `GameBridgeModOwnerCleanupParticipant`、hook status、readiness/status snapshot 中的产品专用分支；
- `Batch5NoDemandRuntimeSnapshot` 中被命名为产品执行器的 counter；
- 强制 AutoFishing 必须 Abstractions-only、禁止 Harmony/reflection/native types、必须通过 friend primitives 的旧 source gates；
- raw `AutoFishingMod.csproj` 作为生产构建/打包权威，以及 release script 的 `AutoFishingMod.dll -> Yuuka.DTMAPI.AutoFishing.dll` 手工改名路径。

不得顺带删除 generic demand coordinator、event kernel、owner cleanup、registry、diagnostics、MCM、通用 native helper 或仍由 ActionSpeed 使用的 shared lifecycle route。

### 4.3 `KEEP-COMPAT`

以下 surface 在 0.5.5 窗口继续冻结，只服务已知旧二进制消费者：

- `IFishingAutomationApi`；
- `FishingAutomationOptions`、`FishingAutomationState`、strategy/enum DTO；
- `Compatibility/FishingAutomation/LegacyFishingAutomationService.cs`；
- `FishingCompatibilityController.cs`；
- `OwnerBoundFishingAutomationApi.cs`；
- compatibility-specific feature construction、callback route、demand/status/cleanup，仅在实际需要时按 compatibility 命名并可扫描；
- `LegacyFishingAutomationCompatibilityFixtureCase.cs` 与 retained ABI canary。

Compatibility 必须与新产品物理独立：

- 从 `LegacyFishingAutomationService` 删除 `AttachPrimitives`、primitive field/state 与所有 primitive branch；
- compatibility 若仍需要 animation/input/publication helpers，持有私有 compatibility implementation；不得从新 AutoFishing 产品 DLL 反向取实现；
- 新产品源码对 `IFishingAutomationApi` 和 `IFirstPartyFishing*` 的消费都必须为零；
- compatibility demand 不得继续叫 product demand，也不得把新产品状态发布回 mandatory GameBridge；
- 若 0.5.5 前不能把 3,007 行 executor/facades 物理移入可选兼容组件，必须如实记录 mandatory base-weight debt，不能声称 G3 已把所有 fishing weight 移出；
- frozen public DTO/signature 最早只能在 0.6.0 清理，而且要先有真实发布的 warning-bearing preview、迁移指南和 fresh zero-consumer scan。

已知 retained binary：`Yuuka.DTMAPI.AutoFishing.dll`，SHA-256 `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA`；其 `DTMAPI.Abstractions` AssemblyRef 为 `0.5.1.0`，对 `FishingAutomationOptions.StopOnManualMove` setter 有一条 MemberRef。该 exact binary 是 compatibility canary，不是新产品架构的消费者证明。

### 4.4 `KEEP-PLATFORM`

继续留在 mandatory DTMAPI 的责任：

- Bootstrap、Core discovery/classification/dependency/load/owner lifetime/restart-required；
- Author SDK、Core、Doctor 的 exact policy registry 和 package provenance；
- `DtmMod`、Abstractions、events、input、logging、translation、config、Mod registry 与 owner-scoped helper；
- `DTMAPI.ModConfigMenu` 与 Manager；
- generic event/demand/generation/reload/diagnostic/owner-cleanup mechanisms；
- `HarmonyReflectionPatcher` 和 `AgentStateLifecycleHookBridge` 中仍由非-fishing 真实消费者或全局不变量需要的部分；
- `Native/GameBridgeNativeHelpers.cs` 的真正通用 helper；
- `Features/EquipmentSlots/...ResetFishingStateOnNativeHit`，因为它属于 EquipmentSlots 的 native-hit 恢复，不是 AutoFishing route；
- `DolocTownGameBridge.Update.cs`、generic OwnerCleanup 与 QA host seam 中不表达 AutoFishing policy 的部分。

`ExperimentalGameBridge.cs` 中 frozen compatibility declarations 是 `KEEP-COMPAT`，不是新稳定平台 API。`DolocTownExperimentalBridgeApi.Diagnostics.cs` 的文案应只描述 compatibility，不再暗示 mandatory fishing product engine。

### 4.5 `MOVE-QA`

以下进入可选 AutoFishing QA 或产品测试工程：

- `FishingNativeControlQaSession.cs`；
- `AutoFishingFixtureCase.cs`；
- `AutoFishingPrimitiveFixtureCase.cs`，迁移后改为 product-native/black-box fixture；
- `AutoFishingNativeControlFixturePolicy.cs`；
- `AutoFishingNativeVitalsCommandAdapter.cs`；
- `AutoFishingPerformanceOrchestrator`、`FishingPerformanceProbe`、product performance contracts/results；
- G6 scenario/controller/settings 中的 AutoFishing-specific hunks。

Framework tests 继续覆盖所有 Advanced Mod 共用的 classifier、policy、build/pack/deploy、restart、failure isolation 与 cleanup；产品 mechanic、native fixture 和 GC orchestration 不进入 mandatory player DLL。现有行为测试不能因旧 primitive seam 删除而静默丢失，应改为产品私有测试、linked pure source test 或 SDK-built package inspection。生产打包不得通过 UnitTests 的 ProjectReference 绕过 Author SDK。

## 五、目标产品与 package 形态

首个迁移实现保持：

- UniqueID：`Yuuka.DTMAPI.AutoFishing`；
- Workshop item：`3743799721`；
- 产品版本：`1.4.3-dtmapi`，本次归属迁移不夹带产品版本改写；
- 既有配置路径、key、默认值、`VerboseLogging` ignore/migration 语义；
- 默认 F6、Gameplay scope、允许 `None`、单一 owner-bound keybind；
- `InstantBite=false`、`SkipMiniGame=false`、`FastAnimations=false`、animation multiplier `3`、cast charge `0`；
- MCM 为 required platform dependency；
- Runtime minimum 在新 package 中统一为 `0.5.5`；
- `Type=CodeMod`、`CodeModKind=Advanced`、显式 `EntryType`；
- canonical entry DLL：`Yuuka.DTMAPI.AutoFishing.dll`；
- package 只含一个产品 entry DLL、manifest、i18n/assets 与 SDK receipt，不含 native dependencies。

生产 build/pack/deploy 只能从 Author SDK 项目和 receipt 产生。旧 raw csproj 可在过渡期间仅作测试源容器，但不得继续出现在 `build.ps1`、release definition、official package 或 install path 的生产 authority 中。

产品 Entry 必须在订阅产品事件/updater 前完整解析 patch targets/callbacks，并使用 `dtmapi.mod.yuuka.dtmapi.autofishing` 原子安装完整 fishing patch set。当前 21 个 fishing patch target 加产品自有 `AgentStateBase.OnExit` 构成迁移 inventory；最终数量必须由源码和 receipt 重新生成，不以本 Review 手写数字替代。Entry 中任何部分失败由 Core owner rollback 与 canonical-owner cleanup 处理；未知 owner、late patch drift 或无法证明清理时保持 restart-required。

F6 disabled 不是 cold disabled：

- official cold disabled：产品 assembly 不加载、Harmony owner 不出现；
- product loaded + F6 off：canonical patches/静态 callback root 可在进程内存在，但不应有 recurring updater、active transaction、animation/input override 或 product session work；
- post-load Manager disable：保持 restart-required，不宣称 live unload；
- clean next restart：产品 assembly/owner 不加载。

G4 receipt 必须分别记录这些状态，不能用“F6 off 时无 updater”替代“cold disabled 无 assembly/patch”。

## 六、G3-G6 验收

### 6.1 G3：唯一真实 Advanced pilot

静态与 package：

- 新 policy exact UniqueID binding 在 SDK/Core/Doctor 三处一致；G2 fixture policy bytes/hash/receipt 不变；
- Strict `SDK160` 仍拒绝 native references；只有 SDK 生成的新 AutoFishing Advanced project 能进入该 policy；
- 产品无 GameBridge AssemblyRef/API dependency，无 `IFirstPartyFishing*`、`IFishingAutomationApi` 消费；
- package 只有 canonical product DLL 与允许内容，零 `Assembly-CSharp`、Unity、Harmony、BepInEx DLL；
- canonical Harmony owner、patch inventory、game build/reference receipt、manifest/entry hashes 一致；
- Doctor/Manager 展示 Advanced、native risk、game-build compatibility 与 restart-required；
- cold disabled 不 load、不 patch；故障/错 hash/错 UniqueID/手改 receipt/额外 native DLL 在 load 前 fail closed。

行为与生命周期：

- 第五存档真实池塘、真实已选择 fishing rod；不得以 synthetic pool/cache/wait-state 作为最终证据；
- 默认 loop、InstantBite、SkipMiniGame、FastAnimations、Instant+Skip、Instant+visible-complete 等独立组合；
- cast charge、visible reel、native pull/result、energy/spirit readback 与实际 restore；
- manual movement cancel，native horizontal factor 优先、A/D/Space/Shift fallback；
- F6 enable/disable、SaveLoaded、save/title/re-entry、Manager disable/restart、故障恢复；
- zero duplicate patch、zero foreign owner、clean process exit、无 Fatal/forced close/残留 `DolocTown.exe`。

### 6.2 G4：mandatory Runtime ProductNative 零增量

实现前在一个 immutable、clean、post-G2 closure commit 冻结 baseline。`capturedAtUtc` 必须早于包含 baseline/receipt 的提交时间；不得从 dirty tree 事后伪造时间。

receipt 比较 baseline 与 implementation Git trees 的完整 mandatory roots：

- `src/DTMAPI.Abstractions`；
- `src/DTMAPI.Core`；
- `src/DTMAPI.BepInExBootstrap`；
- `src/DTMAPI.GameBridge.DolocTown`；
- `src/DTMAPI.ModConfigMenu`。

每个 changed mandatory file 必须绑定 target blob、per-file diff hash 与 semantic classification。receipt 同时列出：

- mandatory ProductNative physical added/deleted lines，目标净新增 `0`；
- product、QA、Compatibility physical lines；
- 恰好五个 mandatory Runtime DLL 与 AutoFishing 可选 DLL；
- public/internal/friend contracts；
- hooks、updaters、long-lived roots、inactive allocations，分项记录；
- policy registry/loader/Doctor 等正增量的 Platform 语义依据。

mutation probes 至少覆盖：遗漏 mixed hunk、target blob drift、mandatory ProductNative reclassification、QA 进入 player package、额外 product/native DLL、错误 baseline/chronology、把 compatibility product executor 误报为 Platform。

### 6.3 G5：Batch 5 路径逐文件重新分类

权威文件集合来自以下 Git-derived union，不从目录前缀猜测：

```powershell
git diff --name-only 7ff75c4c^ 9e31aae5
git diff --name-only e5aaa964 653487b7463778c23e9b96aed9ef713364def22a
```

两组去重 union 当前审计为 161 个路径。最终 receipt 必须重新计算并断言 exact set equality、ordinal row 与 source blob/range binding。每行只能归入：

- `Platform / Retain`；
- `Product route / MoveProduct`；
- `QA / MoveQa`；
- `Compatibility / RetainCompatibility`；
- `Documentation/authority`。

mixed file 必须列出具体 symbol/hunk，不能把整个 `DolocTownHookCallbacks.cs`、Demand、AgentState lifecycle、QA scenario/controller 或 Abstractions 文件用一个标签吞掉。Batch 5 完成事实保持历史有效，但不再被解释成 fishing 物理归属正确。

### 6.4 G6：消费者、ABI、binary/package 扫描

扫描范围至少包括：

- tracked source 中 public/internal/friend consumers；
- build outputs 的 AssemblyRef/TypeRef/MemberRef；
- 新 AutoFishing ZIP exact entries/hashes；
- retained 0.5.2/旧 AutoFishing binary canary；
- local Official/Workshop/subscription packages；
- public API matrix、retained ABI contract/baseline/harness 与 Doctor guidance。

必须明确外部世界不是封闭集合。0.5.5 继续保留 frozen surface 和 once-per-owner deprecation guidance；新产品只走 self-contained Advanced lane。最早 0.6.0 删除前必须完成 warning-bearing preview 实际发布、迁移指南、fresh external/package zero-consumer scan 与版本化破坏决定。

## 七、长期与第五存档矩阵

新产品 exact package 必须重新运行独立的 AutoFishing evidence，不能把 Batch 5 GameBridge receipt 重新贴标签：

- 六个 independent cold L0-L5；
- 每阶段 600 秒 measure、30 秒 sample、5 fish warmup、10 fish measured；
- 第五存档；
- 不使用 dock transport，不 forced GC；
- energy/spirit pre/post/readback 与 save triple restore；
- L4 独立 disable/recovery；
- L5 独立 title reload/re-entry；
- cold official disabled 与 loaded/F6-off 分开；
- exact source/package/manifest/entry/policy hashes；
- observer-effect、metrics availability、owner roots、patch owner、updater、process exit 分别记录。

所有游戏安装、OfficialLocal 切换、运行、恢复与包验证必须先取得 shared Runtime lock，并在 finally 中恢复 source/profile/save 现场后释放。Fatal dump 仍按 opt-in retention protocol；不得用 forced close 作为通过。

## 八、拒绝路径与主要风险

以下方案被明确拒绝：

1. 复用或改写 G2 synthetic policy，使 `doloctown-23762374-g2-v1` 接受 AutoFishing；
2. 手写/复制 Advanced manifest、receipt、package 或只删除 Strict `SDK160`；
3. 把 first-party fishing primitives 发布为公共 API，或保留 friend/provider/facade 作为产品准入费；
4. 把 fishing files 移到另一个 mandatory DTMAPI companion DLL 后声称轻量化；
5. 把 AutoFishing 放进 `BepInEx/plugins` 或作为 External plugin；
6. 承诺 Unity Mono 中任意 Advanced assembly 的 live unload；
7. 保留 GameBridge dependency，仅把产品入口改成 Advanced；
8. 同时重写所有 fishing mechanics，导致行为回归与 ownership move 无法区分；
9. 继续 raw csproj build、手工 DLL rename/repack，绕过 Author SDK receipt；
10. 删除旧 ABI/compatibility，因为新产品已不消费它；
11. 把旧 GameBridge GC receipts 当成新 product bytes 的长期证明；
12. 因 AutoFishing 成功而自动准入 Zoom、ActionSpeed、MoreSaves、AnimalPack 或其他产品。

主要实现风险：policy registry 从单例扩为多 policy 时的 SDK/Core/Doctor drift；mixed-file 产品代码遗漏；compatibility 私有实现仍误依赖 product；Harmony owner late drift；F6 off 与 cold disabled 混淆；测试通过 raw ProjectReference 绕过 SDK；baseline chronology/target blob 不可复核；第五存档 source restore 失败；旧二进制的 `StopOnManualMove` MemberRef 破坏。

## 九、回滚

本 pilot 必须保持可整体回滚：

1. 先确认 `DolocTown.exe` 不存在并撤回 AutoFishing SDK deployment/OfficialLocal source；
2. 回滚 AutoFishing product package/project/native files；
3. 回滚 product-specific policy registry row/compiler surface，恢复 G2 fixture-only registry；
4. 恢复 old product + primitive/GameBridge route 的上一个可构建状态；
5. 保留 frozen compatibility API 与 retained binary evidence；
6. 重跑 Strict/G2 synthetic regression，证明回滚未开放手工 Advanced lane；
7. 不通过删除用户配置、存档、Workshop subscription 或 shared Runtime 来“清理”。

若 product migration 失败，正确结果是恢复 G2 synthetic-only、记录 blocker，并继续阻断真实产品；不能通过放宽 policy、跳过 receipt 或把 product implementation 留在 mandatory GameBridge 强行宣布完成。

## 十、完成边界

只有 owning Update 同时拥有实现 commit、G4 ownership receipt、G5 exact 161-row reclassification、G6 consumer/package receipt、完整 Release suite、第五存档行为矩阵、长期 L0-L5、Doctor/Manager、old-binary compatibility、clean restart/exit 和提交后 permanent gates，才能把 G3/G4/相关 G5-G6 标成 verified。

即使这些门通过，0.5.5 仍需独立 release provenance/compatibility/候选签署；其他真实产品仍需各自准入；G7 Content Host 仍未实现。本 Review 不改变这些阻断。
