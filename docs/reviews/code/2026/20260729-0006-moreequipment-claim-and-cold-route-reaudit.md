# 20260729-0006: MoreEquipmentSlots Claim 与冷恢复修正复审

Status: `recorded / two P1 and four P2 remain / source correction required`

## Scope

本轮复审覆盖上一份独立审查之后的三个提交：

- `4a395398 fix(moreequipment): serialize legacy global claims`;
- `3937628b test(moreequipment): cover claim publication faults`;
- `2dc600fa docs(moreequipment): restore migration fact ownership`.

基线为 `1399eb17`，当前审查 HEAD 为 `2dc600fa`，工作树起始状态干净。
范围包括：

- pre-schema global durable claim 的发布、恢复和完成；
- Product 缺席时 mandatory proxy 与 Compatibility Host 的冷恢复；
- Review、Update、Batch 6 合同、Catalog、README 和冻结候选事实；
- 聚焦 Unit、Catalog、文档治理、包字节和 hash。

这是 audit-only 工作。本轮没有修改运行时代码、产品代码、Catalog、既有
Update 或权威合同；没有安装 Runtime、启动游戏、修改存档或运行完整 Release、
L0-L5、GC、长测、Workshop subscription stress matrix。

## Result

```text
P0 = 0
P1 = 2
P2 = 4

MoreEquipmentSlots = implemented / acceptance-open
current package = superseded by the next source correction
0.5.5 publication = blocked
```

上一轮的 `.previous` 枚举、正常 Product owner 抑制、canonical cold path、
合同状态冲突和 Review/Update 事实归属均获得真实修正。新 claim 协议也覆盖了
顺序执行的 claim-only、Product-published、archive-published、同 scope 恢复、
另一 scope 拒绝、source drift、坏 claim 和冲突 archive。

但是现有自动矩阵没有覆盖 claim 发布中的并发 TOCTOU，也没有覆盖 pending claim
存在而 source/Product 不可见的入口。两处都会破坏“同一无身份 global 只产生
一个 Product authority”的核心数据所有权承诺，因此当前源码仍不能收口。

## Confirmed Corrections

以下修正成立：

1. mandatory proxy 会枚举 global/scoped 的 live `.json` 与 lone
   `.json.previous`。
2. proxy 使用通用 owner ID 检查抑制已加载 owner 的 storage，不在 mandatory
   代码中硬编码 MoreEquipmentSlots 产品身份。
3. Host 已驻留时会静默跳过正常加载的 Product owner，不再产生此前 U2 日志中的
   `orphan-recovery-failed` 假警告。
4. Product cold recovery 要求当前 archive 的精确 canonical scoped full path，
   不再只比较文件名。
5. Review `20260729-0004` 已恢复为预实现发现和测试计划；实现、包和当前验收事实
   回到 Update `20260723-0008`。Batch 6 合同的状态行、身份表、产品段和总表均将
   MoreEquipmentSlots 保持为 `implemented/acceptance-open`。
6. 当前候选两次 SDK 构建字节一致，Catalog 仍为
   `RebuildBlocked / Experimental`，没有越权提升发布状态。

## Findings

### P1-1: 并发出现的 claim 可被另一 scope 当作自己的 claim

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:593`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:614`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:659`

`EnsurePreSchemaGlobalClaim()` 先枚举并验证已有 claim。枚举之后若 claim path
出现，当前第 614-615 行只做：

```text
if claimPath exists
    return
```

它没有读取并验证这个后来出现的 claim 是否属于当前 target scope。确定的竞争
顺序是：

```text
存档 B 枚举 pending claims：空
→ 存档 A 发布绑定 A scope 的 claim
→ B 在 line 614 看见 claimPath 存在并直接 return
→ B 发布自己的 Product v3
```

B 在后续 global finalization 中即使发现 claim scope 不匹配并抛错，B 的 Product
文件已经原子发布并保留。A 完成 archive/清 claim 后，B 的下一次加载会把这个
合法 Product v3 当成自己的 scoped authority，形成跨存档双权威。

第 659-672 行已经正确处理“在稍晚窗口并发出现的 claim”，但不能覆盖第 614
行的早退窗口。当前测试全部是顺序故障注入，没有线程、进程、barrier 或等价的
deterministic race fixture。

修正门：

1. 任何已存在的 claim 都必须读取并与
   `source hash + global identity + target + complete scope` 精确匹配；
2. 或统一走一次原子 create，然后读取最终 authority 并验证 owner；
3. 不同 scope 的并发竞争必须在任何 Product 发布前 fail closed；
4. 增加可确定复现“枚举后、exists 前出现另一 scope claim”的测试。

### P1-2: pending claim 在 source/Product 不可见时被静默忽略

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:18`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:93`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:100`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:125`

claim 在 Product 发布前已持久化。如果进程停在
`after-preschema-claim-publish`，随后当前 scope 的 global source 被删除、改名，
或出现“archive 已存在但 Product 不可见”的恢复状态，`LoadOrMigrate()` 会看到：

```text
target Product absent
target previous absent
legacy global absent
```

然后直接 `CreateEmpty(expectedScope)`。它完全没有检查已经绑定该 scope 的
pending claim。

这会把明确的未完成迁移伪装成新存档空状态。后续正常保存可以发布空 sidecar，
而原 claim 和可能仍存在的 exact archive/backup 不再进入恢复流程。即使原始
source 的不可见来自外部或存储故障，产品也必须可见 fail closed 或使用已验证的
exact archive 恢复，不能静默创建新的空 authority。

现有 `source drift` 测试只向仍存在的 source 追加字节；没有覆盖：

- claim-only + source missing + archive missing；
- claim + exact archive + Product missing；
- claim + corrupt/mismatched archive + Product missing；
- 无 claim 的真正首次安装仍可正常返回 empty。

修正门是在 `CreateEmpty` 前发现与当前 target/scope 相关的 pending claim，并将
上述状态明确分类为可恢复或 fail-closed。

### P2-1: Host 已由另一旧 ABI consumer 预载时不执行孤儿恢复

位置：

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotsService.cs:78`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotsService.cs:112`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:82`

真实组合：

```text
Other.Owner 在 Entry 调用冻结 IEquipmentSlotsApi，backend 已驻留
+ MoreEquipmentSlots Product 缺席
+ canonical Product .json.previous 或其他 orphan storage 存在
→ SaveLoaded
```

`EquipmentSlotsService.SaveLoaded()` 在 backend 已存在时只调用
`NotifyEquipmentSlotsSaveLoaded()` 后立即 return。Host 的 Notify 只清 UI 和
session state，不调用 `RecoverOrphanEquipmentSlotsIfNeeded()`。
`UpdateColdRecovery()` 虽然存在，但生产代码没有调用者。

因此 previous-only 的静态候选枚举已经修好，真实恢复仍会在“Host 恰好已由另一个
consumer 预载”时被跳过，物品持续不可达。字节未丢失，定为 P2；但这是 U3/U4
之前的发布阻断项。

需要加入：

```text
Other.Owner 注册并预载 EquipmentSlots backend
+ Product owner 缺席
+ canonical Product previous-only
→ SaveLoaded
→ recovery 恰好开始一次
```

当前 `EquipmentSlotsCompatibilityHostTests.cs:34` 只调用静态候选枚举，不是
`SaveLoaded → Broker → Host → Recovery` 的真实 demand route。

### P2-2: 清除 claim 前没有重新验证 completed authorities

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:518`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:537`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:541`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:774`

global 被移动到 deterministic archive 后，代码触发
`after-global-archive` fault callback，随后直接调用
`CompletePreSchemaGlobalClaim()`。Complete 只验证 claim 的字段匹配，然后删除
claim；它不重新验证：

- archive 仍存在且 SHA-256 与 source 一致；
- target Product 仍存在、有效、scope/stamp 与 claim 一致。

顺序执行的普通 `File.Move` 不改变字节，所以这不是当前已证实的玩家故障；但它
与 Update 所写“原 scope 验证 completed archive 后清 claim”不完全一致，也使
after-archive drift/delete 故障注入无法 fail closed。claim 删除前应重新验证
archive 与 Product authority，增加 after-archive drift/delete 测试。

### P2-3: 文档再次超前宣称 claim source 已关闭

位置：

- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md:40`
- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md:757`
- `docs/architecture/batch6-managed-mod-identity-contract.md:629`
- `docs/architecture/batch6-managed-mod-identity-contract.md:937`
- `products/first-party/MoreEquipmentSlots/README.md:16`

这些文档分别声称 `4a395398` 已关闭 cross-save window、另一 scope 一定
fail-closed、原 scope 可幂等恢复，以及 durable claim 已通过完整 focused
matrix。P1-1 和 P1-2 证明这些源码完成表述仍然超前。

产品生命周期与发布阻断状态本身仍然正确，不应改成 `verified/closed`。修正时只
需把 claim source 状态退回“correction attempted / P1 open”，待自动矩阵真正
覆盖后再恢复完成表述。

### P2-4: 完整 claim 测试被归因给错误 commit

位置：

- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md:779`
- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md:791`

Update 写成 `Focused validation against 4a395398`，随后列出 stop-after-claim、
another-save-first 等完整 coverage。但新增 306 行 publication-fault 测试属于
后续提交 `3937628b`。应把事实写成：

```text
implementation = 4a395398
coverage completion = 3937628b
actual execution = exact reviewed HEAD
```

这是 provenance 精度问题，不影响测试当前确实通过。

## Validation

当前 HEAD：`2dc600fa`

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
  PASS (6031 checks)
git diff --check:
  PASS before this Review was added
```

上述测试全绿不推翻发现：它们没有覆盖 P1-1 的并发观察窗口、P1-2 的
claim-only/source-missing 状态或 P2-1 的 backend-preloaded SaveLoaded 路径。

当前磁盘候选与 Update 记录一致：

```text
package files = 7
package bytes = 60,135
package SHA-256 =
  2933E957E45D768CCDF38749D62A203C9CA5A81F4FF12830CED5F326CAB7AF81
entry DLL bytes = 149,504
entry DLL SHA-256 =
  BCEC89CECF969F26B16D2C34F09565859889B4BE74DFF80E637DFCCCA0AD2A0F
```

两份 SDK 输出完全一致，但下一次 source correction 会使这些字节和 hash 失效。
它们不可上传。

## Runtime Weight

本次新增重量主要是 Product migration、聚焦测试和一次性 cold discovery：

- claim 协议只在历史 global 迁移时运行；
- proxy 的 storage 枚举只在首次相关 SaveLoaded 探测时运行并缓存；
- 没有新增每帧文件检查、迁移或 claim 轮询；
- Product 转换器仍未进入 mandatory GameBridge。

因此没有发现新的持续热路径或 GC 压力。本轮问题属于数据所有权原子性和真实
生命周期路由，不是“为防边界失控又增加一套常驻审计基础设施”。

## Bounded Next Gate

1. 修复 claim 的 line-614 TOCTOU，并加入 deterministic concurrent claim test。
2. 在 `CreateEmpty` 前协调 pending claim，覆盖 source/archive/Product
   missing、exact recovery 和 fail-closed。
3. claim 删除前重新验证 exact archive 与 target Product。
4. 让 backend-preloaded SaveLoaded 也启动一次 orphan recovery，并补真实组合
   fixture。
5. 修正文档 source/provenance 状态；重新运行四个聚焦入口、Catalog、文档治理。
6. 重新用 SDK 双构建并冻结 Product 包。
7. 然后只完成既定 claim crash/resume、C0、U1、U3、U4 和 migrated-save
   no-save/save 短矩阵。

不需要现在运行完整 Release、L0-L5、GC 或长测，也不得新增 receipt、checkpoint
或第二套 assurance system。

## Resolution Link

Implementation, test provenance, package identity and remaining acceptance
state are recorded only in
[Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md).
