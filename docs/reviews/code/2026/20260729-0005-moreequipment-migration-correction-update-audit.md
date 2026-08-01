# 20260729-0005: MoreEquipmentSlots 迁移修正更新审查

Status: `recorded / one P1 correction required / product acceptance remains open`

## Scope

本轮独立审查覆盖：

- 基线：`cda6a130`;
- 实现：`0331f56d`;
- 文档边界：`15098ebd`;
- 当前 HEAD：`1399eb17`;
- 旧无版本 global、flat schema 1-3、Product v3、`.previous`、冷 Host
  demand、Catalog、冻结包和现有 U2 证据。

这是 audit-only 工作。本轮没有修改运行时代码、产品代码、Catalog、现有
Update、权威合同或测试方案；没有安装 Runtime、启动游戏、修改存档或运行
完整 Release、L0-L5、GC、长测。

## Result

```text
P0 = 0
P1 = 1
P2 = 5

MoreEquipmentSlots lifecycle = implemented / acceptance-open
0.5.5 publication = still blocked by this product gate
```

原审查提出的四个 P1 中，真实历史格式识别、存档身份、精确备份发布和
scoped/global 优先级已经获得实质修正。当前代码不能继续宣称
`source P1 open = 0`：无身份 global 的 single-claim 协议仍有一个跨存档崩溃
窗口。

现有 U2a、U2b、U2c 和 U2c clean restart 证据可以继续证明各自已经实际执行的
成功转换路径，但不能证明该 single-claim 协议在 Product 发布后崩溃时仍安全。
MoreEquipmentSlots 不可提升为 `verified/closed`。

## Confirmed Corrections

以下修正成立，不需要推倒重做：

1. `EquipmentSlotStorageFormatProbe` 对真实历史无版本
   `ownerId/savedAt/slots` 形状采用窄识别；未知字段、错误 owner、畸形槽位不会
   被宽泛认作可迁移输入。
2. flat schema 1-3 要求 owner、archive、有效玩家和 native save clock 与当前
   scope 相容；Product v3 保存并验证该 clock。
3. 精确旧字节备份采用同目录临时文件、`Flush(true)`、SHA-256 校验和原子
   `File.Move`，已覆盖坏 final 隔离和重试收敛。
4. Compatibility scanner 会先建立 canonical scoped owner claim；同 owner 的
   invalid、future 或 ambiguous scoped authority 不再回落 global。
5. Product 转换、备份和 global 归档实现仍在 ProductNative；可选
   Compatibility Host 只链接共享只读格式探针和自身恢复模型，没有把完整迁移
   引擎塞回 mandatory GameBridge，也没有新增每帧路径。

## Findings

### P1-1: pre-schema global 的认领凭证发布得太晚

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:146`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:174`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:437`

当前顺序是：

```text
检查 deterministic global archive 尚不存在
→ 备份源文件
→ 发布存档 A 的 Product v3
→ 移动 global 源到 deterministic archive
```

如果在 `after-product-publish` 或 `before-global-archive` 窗口崩溃：

```text
存档 A 已拥有 scoped Product authority
global 源仍存在
deterministic archive / durable claim 仍不存在
```

下一次进程若先加载存档 B，B 会再次通过 archive-exists 检查并把同一无身份
global 内容复制成自己的 Product authority，造成跨存档重复物品权威。

现有测试没有覆盖该顺序：

- `MoreEquipmentSlotsProductTests.cs:1183` 只在一次迁移已经成功归档后，重新放回
  相同 global 字节并验证第二存档被拒绝；
- `MoreEquipmentSlotsProductTests.cs:1377` 的 `after-product-publish` 故障使用
  带明确 save identity 的 flat schema 3，并且只让同一存档继续恢复。

修正门：

1. Product 对存档 A 可见前，原子发布一个绑定
   `source SHA-256 + target scope/archive/player` 的 durable claim/intent；
2. 同一 scope 可幂等恢复并完成 Product 发布与 global 归档；
3. 其他 scope 看见 claim 后必须 fail closed，不能创建 Product；
4. claim 损坏、source 漂移和 archive 冲突必须保留原字节并可见失败；
5. 故障注入至少覆盖 claim 后、Product publish 后/global archive 前、同存档恢复
   和另一存档抢先加载。

仅把 global 源提前移动走不够：如果没有绑定目标 scope 的可恢复 intent，崩溃后
原存档仍可能失去可证明的恢复路径。

### P2-1: `.json.previous` 单独存在时无法 demand-load Host

位置：

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotsService.cs:145`
- `tests/DTMAPI.UnitTests/Fixtures/EquipmentSlotsHarmonyOwnerFixture/Program.cs:350`

mandatory proxy 的 `HasColdCompatibilityStorage()` 只枚举
`equipment-slots-*.json`，不枚举 `equipment-slots-*.json.previous`。Host 内部虽
已能从 lone previous 推导 canonical base path，现有 fixture 却直接调用已经构造
的 Host service，没有覆盖：

```text
Product 缺席
旧消费者缺席
只有 canonical scoped .json.previous
→ proxy demand-load Host
```

结果是数据仍在磁盘上但恢复入口不触发。该问题不造成字节丢失，却会让额外栏
物品暂时不可达，必须在 U3/U4 前修复并加入真实 demand-route 测试。

### P2-2: Product 正常存在被报告成 orphan recovery failure

位置：

- `src/DTMAPI.GameBridge.DolocTown.Compatibility/EquipmentSlots/EquipmentSlotsProductColdRecovery.cs:67`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:1451`

Product assembly 已加载本应是正常所有权结果。当前 Host 返回
`handled=true/false result`，caller 却记录 Warn 和
`Player.EquipmentSlotsApi = orphan-recovery-failed`。

该假故障在 `201712`、`201914`、`202212`、`202551` 四次 U2 日志中均出现。
此外 mandatory demand probe 对任意 Product sidecar 都返回 true，未先排除已经
加载的 Product，因此没有其他 Compatibility 消费者时也可能为一个正常 Product
sidecar 唤醒可选 Host。

修正应把“Product 正常持有该 storage”表达为显式 no-op/owned-by-product，而不
是恢复失败；mandatory proxy 也应在可证明 Product 已加载时跳过 cold Host demand。

### P2-3: Product cold path 只按文件名认 owner

位置：

- `src/DTMAPI.GameBridge.DolocTown.Compatibility/EquipmentSlots/EquipmentSlotsProductColdRecovery.cs:30`

`IsMoreEquipmentSlotsProductStoragePath()` 只比较文件名，没有证明路径位于当前
存档的 canonical scoped root。官方 writer 不会产生 global-root nested Product
v3，因此这不是当前已证实的数据故障；但在 scoped authority 缺席时，误放在
global 根的 nested Product 仍可能被当作 canonical Product 输入，违背“global
只作为 legacy 输入”的边界。应补 canonical-path 拒绝测试后再关闭。

### P2-4: 权威合同内部同时声称 closed 和 open

位置：

- `docs/architecture/batch6-managed-mod-identity-contract.md:3`
- `docs/architecture/batch6-managed-mod-identity-contract.md:262`
- `docs/architecture/batch6-managed-mod-identity-contract.md:649`
- `docs/architecture/batch6-managed-mod-identity-contract.md:931`

状态行、产品段和总表正确写为 `implemented/acceptance-open`，但身份表仍把
MoreEquipmentSlots 列入 `verified/closed` 产品集合。应修正当前合同的现时投影，
不改历史证据。

### P2-5: Review 吸收了实现与验收叙事，且 single-claim 结论超前

位置：

- `docs/reviews/code/2026/20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md:577`
- `docs/reviews/code/2026/20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md:583`
- `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md:656`
- `products/first-party/MoreEquipmentSlots/README.md:13`

Review `0004` 从 `Implementation Follow-up` 开始追加了实现 commit、changed
result、包 hash 和 Unity 完成叙事。这违反
`docs/reviews/README.md:39` 与文档治理的单一事实所有者规则：Code Review 在实现
开始后应冻结，只保留指向 owning Update 的短 resolution link。

同时上述文档将 deterministic archive 描述成可靠的 single-use claim，并写出
`source P1 open = 0`，与 P1-1 的真实崩溃窗口冲突。机械文档检查无法识别这种
语义重复和超前结论。

修正时应：

- 把实现、包和最终验证事实只留在原 Update；
- 将 Review `0004` 收回为预实现审查、接受门与短 resolution link；
- 在 P1-1 修复和故障矩阵通过前，不恢复 single-claim closed 表述。

## Evidence Assessment

### Retain

- `GAME-SMOKE/20260729-201712`: U2a clean flat schema 2 conversion;
- `GAME-SMOKE/20260729-201914`: U2b clean Host flat schema 3 conversion;
- `GAME-SMOKE/20260729-202212`: U2c clean pre-schema global conversion;
- `GAME-SMOKE/20260729-202551`: same-fixture clean U2c restart.

这些运行使用 disposable、Steam AutoCloud-isolated fixture；U2 属于
`ArchiveMutation`。没有玩家存档例行备份或写回，存档测试模式合规。

### Do Not Promote

上述 `result.json` 中 MoreEquipmentSlots/cold recovery 专项断言为 `Skipped`；
它们只能与 Product 日志和退出后 fixture 检查共同支持实际执行的 U2 存储转换。
它们不支持：

- pre-schema claim 的跨存档 crash safety；
- C0；
- U1；
- U3/U4；
- 迁移后 UI、盾牌 no-save rollback 和正常睡觉保存语义。

`GAME-SMOKE/20260729-195501` 只恢复了一件物品且仍留一项，不能算 U3 PASS。

## Runtime Weight And Package

这轮源码增长主要位于 Product、可选 Host 共享 probe、测试和文档：

- Product-only migration/conversion area约 1,456 物理行；
- 共享只读 probe 为 472 物理行；
- 完整转换、备份和归档实现未进入 mandatory GameBridge；
- 没有新增每帧转换或文件轮询路径。

因此不能把 `+3178/-586` 直接解释为默认 Runtime 热路径变重。不过 P2-2 的
Product-sidecar demand 会削弱“无 Compatibility 需求时 Host 不加载”的边界，
仍需在发布前关闭。

当前磁盘冻结物与记录一致：

```text
package files = 7
package bytes = 58,798
package SHA-256 =
  15DB9926A67147012892BE3550368EBDAB63397BEFA888BF979C077C4C2B9633
entry DLL bytes = 144,896
entry DLL SHA-256 =
  75D35F151572298E9FD4630129F36EBD511C0BF6770DEDAEE5B6C7CB68F52D98
```

修复 P1/P2 源码后这些 hash 必须失效并重新冻结。

## Validation Performed

当前 HEAD：`1399eb17`

```text
Release build tests/DTMAPI.UnitTests:
  PASS, 0 errors
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

聚焦测试全绿不推翻上述发现：现有 fixture 正好没有覆盖 P1-1 的跨存档故障顺序
和 P2-1 的 mandatory proxy `.previous-only` demand 路径。

## Bounded Next Gate

1. 修复 P1-1，并补 pre-schema claim 的跨存档故障/恢复自动矩阵。
2. 修复 `.previous-only` demand、Product-owned no-op/假警告和 canonical path
   约束；运行现有四个聚焦入口。
3. 修正文档真相与 Review/Update 事实所有权。
4. 重新构建并冻结 MoreEquipmentSlots 包。
5. 保留已经通过且代码路径未受影响的 U2a/U2b；只对 claim 协议补最小
   pre-schema fault/resume 运行。
6. 再执行原计划仍开放的 C0、U1、U3、U4 和分开的盾牌 no-save/save 短矩阵。

不需要为这些修正运行完整 Release、L0-L5、GC 或长测，也不得新增 receipt、
checkpoint 或第二套 assurance system。

## Resolution Link

Implementation, changed files, package identity, validation and the remaining
acceptance state are recorded only in
[Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md).
