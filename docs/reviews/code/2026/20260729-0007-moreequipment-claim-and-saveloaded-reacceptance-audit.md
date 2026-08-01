# 20260729-0007: MoreEquipmentSlots Claim 与 SaveLoaded 修正复审

Status: `recorded / two P1 and three P2 remain / source correction required`

## Scope

本轮独立复审覆盖：

- `b2cb5430 fix(moreequipment): close claim concurrency windows`;
- `c5fce186 test(moreequipment): cover claim and resident-host races`;
- `6f9fd1bd docs(moreequipment): record claim correction provenance`.

基线为上一轮审查 HEAD `2dc600fa`，本轮审查 HEAD 为 `6f9fd1bd`，
工作树起始状态干净。复审逐项检查
[Review `20260729-0006`](20260729-0006-moreequipment-claim-and-cold-route-reaudit.md)
中的两个 P1 和四个 P2，并额外沿真实 Hook、GameBridge feature fanout、
Compatibility Host 与 Product cold-recovery session 检查生产生命周期。

这是 audit-only 工作。本轮没有修改 Runtime、产品、测试、Catalog、Update
或权威合同；没有安装 Runtime、启动游戏、修改存档或运行完整 Release、
L0-L5、GC、长测及 Workshop subscription stress matrix。

## Result

```text
P0 = 0
P1 = 2
P2 = 3

MoreEquipmentSlots = implemented / acceptance-open
current Runtime and Product candidates = superseded by required source correction
0.5.5 publication = blocked
```

上一轮指出的 claim early-return TOCTOU、pending-claim empty authority、
archive/Product completion revalidation和 resident backend 局部早退均已直接
修正；新增聚焦测试也真实覆盖了多项顺序故障注入。

但完整生产链路仍有两个阻断问题：

1. EquipmentSlots 的同一 `SaveLoaded` 生命周期由专用通知和通用 feature
   fanout 重复分发，第一次 cold recovery 建立的 session 会被第二次通知清除；
2. claim 的 static lock 只覆盖同一进程。进程外落后者可在赢家完成并删除 claim
   后发布 stale claim，使赢家与落后者后续都无法打开自己的 authority。

因此当前四个聚焦入口全绿不能构成独立 source reacceptance。

## Confirmed Corrections

以下修正成立：

1. 任何已存在的 final claim 都会被读取并按 source、global filename、target、
   complete scope 精确校验，不再只有 `File.Exists() -> return`。
2. claim 原子发布前后会重复枚举和校验；同进程竞争由一个静态临界区串行化，
   `File.Move` 目标竞争也会读取最终 authority。
3. Product、previous 与 global source 均缺失时，pending claim 会阻止
   `CreateEmpty()`；exact/conflicting archive 也会保持 recovery-required。
4. claim 删除前已新增 archive SHA、Product scope 和 pre-schema migration
   stamp 校验。
5. resident backend 的局部 `EquipmentSlotsService.SaveLoaded()` 路径已经从
   `Notify -> return` 改为 `Notify -> Recover`。
6. Update 已把 `4a395398`、`3937628b` 的历史尝试与
   `b2cb5430`、`c5fce186` 的当前实现/coverage/execution provenance 分开；
   产品和合同仍保持 `implemented/acceptance-open`，未越权提升为
   `verified/closed`。

## Findings

### P1-1: 真实 SaveLoaded 双分发会清除第一次 cold-recovery session

位置：

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:95`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:275`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotsFeature.cs:59`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotsService.cs:78`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:166`
- `src/DTMAPI.GameBridge.DolocTown.Compatibility/EquipmentSlots/EquipmentSlotsProductColdRecovery.cs:381`

`AfterLoadArchiveDataPostfix()` 当前按以下顺序执行：

```text
NotifyEquipmentSlotsSaveLoaded
→ EquipmentSlotsService.SaveLoaded
→ NotifyGameBridgeFeaturesSaveLoaded
→ EquipmentSlotsFeature.SaveLoaded
→ 同一个 EquipmentSlotsService.SaveLoaded 再执行一次
```

resident Host 修正使两次调用都会执行 `Notify + Recover`。第一次 recovery
把物品放进原生 backpack/mail，持久化 Prepared journal，并把
`ProductColdRecoverySession` 放入内存字典。第二次 `Notify` 随即调用
`ResetEquipmentSlotSessionState()`：

```text
equipmentSlotsOrphanRecoveryChecked = false
ClearProductColdRecoverySessions()
```

第二次 recovery 看到 native save fingerprint 尚未变化、但目的地物品数量已经
增加，会按事务协调器规则 fail-closed。此时第一次 session 已丢失，
`SaveSaved` 无法在本次正常保存后把 journal 提升并清理。

这不只是重复扫描。若玩家第一次睡觉保存后继续使用或消耗该物品并再次保存，
sidecar 仍停在第一次 placement 的 Prepared 证据，后续可能无法用精确 native
delta 调和。它违反 U3 要求的“正常 native save 后事务达到 terminal state”，
属于 P1 存档一致性/可达性阻断。

修正门：

1. 一个 native SaveLoaded generation 只能有一个 EquipmentSlots 生命周期 owner；
2. 不能依赖 recovery 自身幂等来掩盖第二次 `Notify` 清 session；
3. 真实 production route 必须证明一次 Notify、一次 recovery，随后
   `SaveSaving/SaveSaved` 能完成同一 session；
4. loaded Product owner 仍须保持不被 Compatibility 接管。

### P1-2: completed claim 删除后，进程外落后者可发布 stale claim

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:581`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:674`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:860`

`PreSchemaGlobalClaimSync` 只能串行化一个 Mono 进程。文件级协议仍允许：

```text
进程 B 通过 archive/Product/claim 初检并准备 temp claim
→ 进程 A 发布 Product、归档 global、删除 A claim
→ B 再枚举时已经看不到 A claim
→ B 原子发布绑定 B scope 的 claim
→ B 在后续 legacy backup 阶段发现 global 已被 A 移走并失败
```

B 的 Product 尚未发布，但 B claim 会保留。A 下次加载时以 A scope 完成 claim
校验会因 B claim 失败；B 又因 exact archive 已存在而没有 B Product 失败。
真实 A Product 字节仍在，却需要人工处理 stale claim 才能重新可达。

现有同进程测试不能覆盖这个顺序。需要：

1. 保留 pending/completed 可区分的永久完成凭证，避免 claim-deletion 空档；或
2. B 发布 claim 后重新验证 source、archive 和已发布 Product，发现 A 已完成时
   只能撤销自己刚发布的 stale claim并 fail closed；
3. 使用独立进程或能绕开 static lock 的确定性 fixture 证明
   `A remains loadable + B has no claim/Product`。

### P2-1: completed authority 校验没有证明 global 已消失

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:881`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:890`

`ValidateCompletedPreSchemaGlobalAuthorities()` 校验 archive SHA 和 Product，
但没有校验 `globalPath` 已不存在。在现有 `after-global-archive` 故障边界重新
创建 global 时，本次迁移仍会删除 claim 并返回成功；下次加载却会因
`global + completed archive` 冲突而失败。

最低测试是在 `after-global-archive` 重建 global，要求当次
`InvalidDataException` 且 claim 保留。跨进程在检查后瞬时重建仍需由 P1-2 的
完成凭证协议负责。

### P2-2: 两个新增“并发/真实路由”测试没有覆盖其命名所承诺的边界

位置：

- `tests/DTMAPI.UnitTests/MoreEquipmentSlotsProductTests.cs:1327`
- `tests/DTMAPI.UnitTests/MoreEquipmentSlotsProductTests.cs:1439`
- `tests/DTMAPI.UnitTests/EquipmentSlotsCompatibilityHostTests.cs:39`

`ConcurrentPreSchemaClaimPublicationIsSerialized()` 使用共享 static lock 的同进程
线程，只证明进程内串行化；它没有执行跨进程 `File.Move` 仲裁或
completed-claim 空档。`WaitOne(100ms)` 也不能证明竞争线程已经实际尝试进入
临界区，尚未获得调度同样会被解释为 pending。

`ResidentBackendSaveLoadedStartsOrphanRecovery()` 通过反射向 broker 注入 fake
backend，然后只直接调用一次 `service.SaveLoaded()`。它没有走：

```text
AfterLoadArchiveDataPostfix
→ 专用 EquipmentSlots 通知
→ 通用 feature fanout
→ broker
→ 真实 Compatibility Host
```

因此两项测试恰好避开本轮两个 P1。

### P2-3: 当前 source-completion 说明与 provisional candidate 已再次过期

Update、Batch 6 合同和产品 README 正确保留了
`implemented/acceptance-open`，候选包也明确写为 provisional；这些总状态无需
改成更高等级。

但它们对本次 source correction 的细节描述仍称：

- cross-process 竞争能在 Product 发布前被检测；
- resident backend 完成 exactly one recovery；
- 当前 focused matrix 已覆盖 claim concurrency 和 production SaveLoaded。

P1-1/P1-2 使这些具体完成描述失真。修正后需要在同一个 Update 内记录新的实现、
coverage 和实际执行 HEAD，并重建 Runtime 与 MoreEquipmentSlots 候选。不得把
本轮 provisional hash 沿用到后续字节。

## Validation

当前 HEAD：`6f9fd1bd2789a4e15499ec085bfdaac546b97991`

```text
Release build tests/DTMAPI.UnitTests:
  PASS
  0 errors
  10 existing DebugConsole nullable warnings

DTMAPI_UNIT_TEST_FOCUS=moreequipment-product:
  PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host:
  PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host:
  PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing:
  PASS

check-product-catalog.ps1:
  PASS (27 / 11 / 22 / 48)
check-doc-governance.ps1:
  PASS (6,031 checks)
git diff --check:
  PASS before this Review was added
```

四份当前 SDK 输出的 Product ZIP/DLL 字节一致：

```text
package files = 7
package bytes = 60,915
package SHA-256 =
  52F5F075D91E84D2A73DF68F24ACC8B7CD375B0FECBA7122126B0E457044EEF6
entry DLL bytes = 152,576
entry DLL SHA-256 =
  0E754093F16965CE91BEC51F658D690BF586EC0B85BE05DAD1B8E25089811F21
```

ZIP 只包含产品 DLL、manifest、advanced references、package metadata、
`info.json` 和两种 i18n。包结构和确定性成立，但源码修正会使这些 hash 失效。

## Runtime Weight

本轮修正没有新增每帧路径：

- claim 代码只在历史 global migration 时运行；
- cold recovery 只在 SaveLoaded/明确生命周期运行；
- 新问题是重复生命周期 owner 与事务完成原子性，不是新的长期 GC 来源。

不过真实 SaveLoaded 当前会重复通知、重复扫描并可能重复尝试恢复，因此不能把
它视为完全按需且恰好一次的绿色路径。

## Bounded Next Gate

1. 统一 EquipmentSlots SaveLoaded owner，并用分层证据覆盖：
   真实 Hook/feature/broker resident 分支的一次分发与 SaveSaved 终态，以及独立
   physical fixture 中真实 Compatibility Host 的事务/清理；本门不要求把两层
   合并成单条 Hook-to-real-Host 端到端夹具。
2. 关闭 completed-claim deletion 空档，补进程外或等价确定性竞争测试。
3. claim 完成前验证 global 已不存在。
4. 修正两个测试的真实覆盖和 source-completion 文档真相。
5. 只重跑四个聚焦入口、Catalog、文档治理，并重建 Runtime/Product 候选。
6. 源码独立复审通过后，再进入既定 claim crash/resume、C0/U1/U3/U4 和
   migrated-save no-save/save 短矩阵。

本轮不需要完整 Release、L0-L5、GC 或长测，也不需要新的 receipt、checkpoint
或 assurance 系统。
