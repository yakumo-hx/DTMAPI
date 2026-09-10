# Manual QA Review: MoreEquipmentSlots 旧 Sidecar 代际迁移与冷恢复分流

## Review Header

- Time: `2026-07-29`
- Status: `implemented / U2a-U2c migration accepted / remaining bounded player acceptance open`
- Source: 用户对冻结 Compatibility Host 扁平 sidecar 与 ProductNative
  嵌套 v3 sidecar 的逐字段审查及 0.5.5 R0 冷恢复风险说明。
- Scope: `DTMAPI.MoreEquipmentSlotsMod` 的既有 per-save/global sidecar、
  Compatibility Host 冷恢复入口、ProductNative 首次加载迁移、保存提交边界
  和聚焦故障注入；本记录不扩大 Frozen `IEquipmentSlotsApi` 或玩家
  Runtime。
- User constraints: 旧额外栏中的物品更新后不得因格式切换不可见或丢失；
  禁用、退订或缺失旧 MoreEquipmentSlots 时，0.5.5 冷恢复仍必须把旧物品
  恢复到背包或邮件；必须完成完整代际迁移。
- Related review/update/debug records:
  - [MoreEquipmentSlots Update](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
  - [MoreEquipmentSlots admission Review](../../code/2026/20260723-0009-eighth-product-more-equipment-slots-admission-review.md)
  - [Unsaved save-commit regression Review](20260724-0001-moreequipment-unsaved-save-commit-regression.md)
  - [MoreEquipmentSlots Hook map](../../../../hook-map/focused/MoreEquipmentSlots.md)
- Files/docs inspected:
  - `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotContracts.cs`
  - `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.cs`
  - `products/first-party/MoreEquipmentSlots/src/Native/MoreEquipmentSlotsNativeRuntime.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityTransactions.cs`
  - `src/DTMAPI.GameBridge.DolocTown.Compatibility/EquipmentSlots/EquipmentSlotsProductColdRecovery.cs`
  - current EquipmentSlots Unit and physical Compatibility fixtures.
- Not inspected: 本轮尚未启动游戏；没有把历史 Product v3 实机证据倒推成
  旧 sidecar 迁移证据。

## Issue Review

### Issue 1: 更新后 ProductNative 无法读取旧额外栏

Original feedback:

- 旧版和新版使用同一个 per-save sidecar 文件名，但旧版是顶层
  `ownerId/storageScope/archiveIndex/slots/isShieldHat`，新版是嵌套
  `scope`、固定三槽和 `isShield`。
- ProductNative 遇到旧 schema 2 会报 `schema-mismatch`，遇到 0.5.5
  Compatibility Host 写出的扁平 schema 3 会报 `scope-mismatch`。
- 文件通常仍在磁盘，但新额外栏读不到，玩家表现为物品丢失；global legacy
  路径还可能与 per-save Product 路径同时成为候选权威。
- 当前没有旧 sidecar 到 Product v3 的转换器或测试。

Screenshot/log transcription:

- 无截图。字段和失败路径均来自用户给出的源码位置并由本轮源码检查确认。

Review record:

- User-confirmed facts: 已有玩家存量可以包含扁平旧文档；更新不得要求玩家先
  手工卸下旧额外栏物品。
- Code/doc facts inspected:
  - Product `EquipmentSlotDocumentStore.TryValidateRawV3` 只接受
    `schemaVersion=3` 和匹配的嵌套 `scope`；扁平旧文档反序列化后没有该
    scope，因此不能加载。
  - Compatibility 文档允许顶层 owner/save identity、可变 slot 列表和
    `isShieldHat`；Product 文档要求三个固定槽并使用 `isShield`。
  - Product `OnSaveLoaded` 直接加载当前 per-save 路径，没有格式探测、
    旧字段转换或 global legacy 采用。
  - Compatibility 旧实现以 per-save 路径优先，在缺失时采用 global legacy，
    但 Product 没有继承这段迁移语义。
- Codex inference: 当前实现证明的是“新装 Product v3 可工作”，没有证明
  “冻结旧消费者/Host 产生的数据可升级”。文件未删除不能满足物品安全；
  对玩家而言，唯一可见入口无法读取即是功能性丢失。per-save 与 global
  同时存在而没有精确来源证明时，也不能静默选一份再覆盖另一份。
- Ownership: 格式识别、旧字段到固定三槽的转换、Product v3 发布和迁移
  收口均属于 `MoreEquipmentSlots` ProductNative。Compatibility Host 只
  保留旧 ABI/旧孤儿恢复语义，不成为新 Product 的长期数据 owner。
- Root-cause hypotheses:
  - 已确认：物理拆分时复用了路径和 schema 数字，却没有建立格式代际契约；
    “schema 3”不能区分 Host 扁平 v3 与 Product 嵌套 v3。
  - 已确认：Product 加载器把“不是 Product v3”一律作为损坏/错 scope，
    没有受限的 legacy 分支。
- Rejected/unproven hypotheses:
  - 不允许通过放宽 Product v3 的 scope 校验接受任意 JSON；这会把损坏、
    其他存档或未来 schema 当成本存档。
  - 不允许遇到失败就创建空三个槽并覆盖旧文件；原字节必须保留到迁移成功。
  - 不假定旧版所有 `0..24` 槽都能无损塞入 Product 的三个槽。超出 Product
    固定三槽且含物品时必须 fail closed，交由旧 Host 恢复或明确诊断，不能
    丢弃。
- Required downstream updates:
  - 为 Product 与 Compatibility 冷恢复共用一个无副作用的存储格式分类器。
  - 支持历史 schema 2 和当前 Host 扁平 schema 3，逐字段转换槽、
    Working/Committed、journal/candidate、存档 identity 和 generation。
  - per-save 原地迁移必须先写临时 Product v3、重新验证，再原子替换并保留
    可识别的精确旧字节备份。
  - global legacy 只有在当前 per-save 缺失且 identity 合法时才可采用；
    Product v3 成功发布后再归档 global 源。中断重入必须依靠源 hash/迁移
    标记精确收口，不能产生两份活跃权威。
- Acceptance checks:
  1. 扁平 schema 2 的三个槽、盾牌字段和保存 identity 精确迁入 Product v3。
  2. Compatibility Host 扁平 schema 3 得到同样结果。
  3. global legacy 在 per-save 缺失时迁入当前 save；Product v3 验证成功后
     才归档 global。
  4. per-save Product v3 优先；同时存在的 global 只有与迁移标记/hash 精确
     匹配时才可收口，漂移或歧义 fail closed 且两边字节不变。
  5. future schema、wrong owner、wrong save、重复/越界槽、第四个有物品槽、
     损坏 JSON 均 fail closed，不发布空 Product 文档。
  6. 发布前中断保留旧权威；Product 发布后/global 归档前中断可幂等收口。
  7. 旧 gameplay candidate/journal 映射后仍遵守 native save 的
     Working/Committed 语义，恢复恰好一份物品。
- Blocker conditions: `P1`。在转换器、原子发布/中断重入和上述聚焦测试通过
  前，MoreEquipmentSlots 不得保持 `verified/closed`，0.5.5 玩家更新也不能
  宣称保留旧额外栏物品。

### Issue 2: 冷恢复把旧扁平文件误分流到 Product v3

Original feedback:

- 玩家更新 0.5.5 后禁用、退订或缺失旧 MoreEquipmentSlots 时，冷恢复会
  按文件名先把扁平旧文件当成 ProductNative v3；解析失败后不会回落旧解析器。
- 因此既有“物品回到背包或邮件”的承诺可能失效。

Screenshot/log transcription:

- 无截图。该分流由当前 Compatibility 源码直接成立。

Review record:

- User-confirmed facts: R0 必须覆盖没有 Product 程序集可执行的冷启动恢复；
  不接受要求玩家重新启用旧 Mod 才能取回物品。
- Code/doc facts inspected:
  - `TryRecoverMoreEquipmentSlotsProductStorage` 仅凭精确 Product 文件名就先
    设置 `handled=true`，再调用 Product v3 反序列化。
  - 调用方看见 `productHandled` 后立即跳过旧扁平
    `TryLoadEquipmentSlotStorageDocument` 分支。
  - 因而 schema 2 和 Host 扁平 schema 3 均可形成“Product 解析失败，但旧
    解析器永远没有机会”的半路由。
- Codex inference: 这是已证实的确定性分流缺陷，不是理论兼容风险。正确边界
  是先识别文档形状，再选择唯一 owner；文件名只能限定候选产品，不能证明
  文档代际。
- Ownership: Compatibility Host 的 orphan/cold dispatcher 负责选择旧恢复
  或 Product v3 恢复；实际旧物品放回仍由现有旧恢复后端完成。共享分类器不
  等于新增 SharedNative gameplay API。
- Root-cause hypotheses:
  - 已确认：`handled` 的设置时机早于格式证明。
  - 已确认：旧和新复用文件名，文件名分类无法表达代际。
- Rejected/unproven hypotheses:
  - 不能对任意 Product 解析错误都回落旧解析器；损坏 Product、未来 schema
    或歧义文档必须由 Product 路线 fail closed，避免旧解析器误消费。
  - 不需要扩大 Compatibility Host 常驻或把 Product gameplay executor 搬回
    Host。
- Required downstream updates:
  - 候选路径先做只读格式探测：明确 legacy flat 时令 Product 分支
    `handled=false`；明确 Product v3 时由 Product 冷恢复独占；未知、未来、
    歧义或损坏仍 `handled=true` 并 fail closed。
  - 用真实 Host 可装载 fixture 覆盖 schema 2、Host flat schema 3、Product
    v3 和非法文档的唯一分流。
- Acceptance checks:
  1. 禁用/退订/缺失 Product 时，flat schema 2 与 flat schema 3 都进入旧
     parser，并保持原 backpack/mail/journal 语义。
  2. Product v3 只进入 Product cold backend。
  3. 非法或歧义文档不会串到另一解析器，也不会删除或改写源文件。
  4. cold recovery 成功、失败重试、标题/Loader 清理继续保持 owner 零残留。
- Blocker conditions: `P1 / 0.5.5 R0 blocker`。分流修复和物理 Compatibility
  fixture 未通过前，原 cold-recovery 承诺无效。

## Cross-Issue Summary

- Confirmed user facts: 旧物品必须在“更新启用新 Product”和“更新后 Product
  缺失/禁用的冷恢复”两条路线都可达。
- Code-path findings: 两个缺陷来自同一个缺失边界：相同文件名下没有先证明
  存储代际，Product 加载和 Host 冷恢复分别假定了相反的唯一格式。
- Risks: 静默空槽、双 sidecar 权威、错存档采用、未来 schema 误解析、迁移
  中断后重复物品或不再可恢复。
- Suggested implementation scope: 一个共享、无副作用的形状分类/legacy DTO
  层；Product 原子迁移/幂等收口；Compatibility 唯一分流；聚焦 Unit 和
  physical Host fixture。不得新增公开 API、Host、sidecar 家族或常驻
  Runtime 组件。
- Items that should not be carried forward: 历史 Product v3 游戏矩阵仍只证明
  它原来的 Working/Committed 和生命周期范围；不得把它描述为旧数据迁移
  证据。

## Implementation Record Decision

- Create/update an implementation update record: yes。继续由现有
  `20260723-0008` 拥有生命周期并重新置为 `in-progress/open`；不新建相邻
  receipt 或第二套保证体系。
- Additional debug/API/hook/smoke records required: API matrix 和 Batch 6 当前
  状态必须撤回 verified/closed；若进行实际游戏运行，再按事实更新 Hook/smoke
  记录。仅 Unit/fixture 不伪装成玩家证据。
- Suggested task titles:
  - MoreEquipmentSlots legacy sidecar generational migration
  - EquipmentSlots cold-recovery format routing
- Completion standard: 代码与格式契约完成、聚焦迁移/故障注入/physical Host
  矩阵通过、现有 Product save-commit 回归不退化，并对是否运行游戏和完整
  Release 如实记录。

## 2026-07-29 Initial Implementation Record

This section records commit `cba22720` only. The later independent
[P1/P2 audit](../../code/2026/20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md)
found that the original wording overstated its test coverage. The corrected
scope is recorded here instead of retroactively treating later tests as proof
for the earlier commit.

- Source result:
  - 新增共享、只读的格式分类器，明确区分 nested Product v3、flat
    schema 1-3、future、ambiguous 和 invalid；
  - Product 对 per-save flat 文档执行“精确旧字节备份 -> 临时 Product v3
    写入与回读验证 -> 原子替换”；global legacy 仅在 per-save 缺失时采用，
    并由 migration stamp 的源 SHA-256 收口；
  - Compatibility 冷入口仅在格式明确为 Product v3 时取得 Product owner；
    flat schema 2/3 返回旧 parser，未知、歧义和未来格式保持 fail closed。
- Data-safety result:
  - schema 2/3 的三个旧槽、item id、盾牌/被动/防御字段、
    gameplay candidate 和 journal 有转换测试；
  - Product 发布前中断、Product 已发布但 global 未归档、重启幂等、
    外部漂移、future schema 和第四个有物品槽有故障或拒绝测试；
  - 原始旧字节备份位于受保护 scope 下的 `.legacy-migrations`，明确为
    非活跃、非权威恢复副本；global 源只在精确 hash 仍匹配时归档。
  - 该提交没有覆盖真实无 `schemaVersion` global、显式 schema 1、
    空/错 owner、仅 storageScope 错配、save-clock 回退、重复/负数槽、
    备份复制中断、scoped/global owner 冲突或 `.previous` 冷入口。
- Cold-recovery result:
  - 物理 `net48` Compatibility Host fixture 证明 flat schema 2 与 Host flat
    schema 3 都进入旧恢复器并移动恰好一件物品；歧义文档不改写。
- Validation:
  - `moreequipment-product`: PASS；
  - `moreequipment-cold-host`: PASS；
  - `compatibility-host`: PASS；
  - Catalog: PASS（`27/11/22/48`）；
  - MoreEquipmentSlots Advanced package: PASS，SHA-256
    `C8EE0B3B9AD77DDBAB736F11F4A11F14F52BF476DED49F372051F924F0627F31`。
- Evidence boundary:
  - 本轮没有启动游戏，也没有运行完整 Release。两次广域
    `tools/scripts/test.ps1` 尝试分别在 120 秒和 300 秒命令窗口超时，未取得
    可作为 PASS 的结果；
  - 因而最初的 schema 2/3 主路径为 `implemented`，不能写成四个后续 P1
    已关闭，更不能写成玩家实机迁移已验证。冻结
    Product-v3 历史证据继续有效，但不补足旧 sidecar 代际迁移；仍需一次
    bounded、runtime-locked 玩家迁移/冷恢复验收及独立复核。

## 2026-07-29 Audit Correction Resolution

Commit `0331f56d` closes the later audit's four P1 and three P2 source
findings:

- exact historical `PreSchemaGlobal` recognition and single-use hash claim;
- exact owner/scope/archive/player/save-clock validation for flat schema 1-3;
- persisted Product-v3 native save revision with post-save advancement;
- temp/flush/hash/atomic legacy backup publication and partial-final retry;
- canonical scoped owner claims blocking same-owner global fallback;
- reachable Product `.previous` recovery only for missing/recoverably invalid
  live files;
- lightweight Host probe without the Product migration implementation.

The final automatic matrix covers all generations and each rejection/fault
window named by audit `20260729-0004`. The four focused Units, complete
`DTMAPI.UnitTests`, Catalog projection and a twice-identical frozen-SDK package
all pass. The final package SHA-256 is
`15DB9926A67147012892BE3550368EBDAB63397BEFA888BF979C077C4C2B9633`.

The first Unity transition exposed and then closed a Mono path-length issue in
the deterministic backup name. Backup/temp/rejected files now use short
same-directory names without changing hash addressing or atomic publication.
AutoCloud-isolated evidence `201712`, `201914`, `202212` and `202551` proves
schema 2, full Host flat schema 3, exact historical no-version global and its
single-use cold restart respectively. It proves the U2 storage transition, not
equipment UI or shield gameplay.

U1 remains blocked by the exact-source boundary: DirectExe cannot prove the
retained Workshop tree without Steam's native subscription snapshot, and the
same bytes copied to Local correctly lack an Author receipt. `195501` is only a
partial Product-absent cold-recovery observation: one `box_hat` reached the
native backpack, but the dedicated cold assertion was skipped and one legacy
item remained. C0/U1/U3/U4 and the migrated-save semantic spot check remain
open. This Review does not claim a complete Release PASS.
