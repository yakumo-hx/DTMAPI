# 20260729-0004: MoreEquipmentSlots 旧数据迁移修复审查与分批发布测试方案

Status: `recorded / findings handed to Update 20260723-0008`

## Scope

本记录审查 MoreEquipmentSlots 旧扁平 sidecar 迁移及 Compatibility Host
冷恢复修复，并把 Runtime `0.5.5`、旧 MoreEquipmentSlots 和新
MoreEquipmentSlots `1.0.0` 的分批发布测试方案落盘。

审查基线：

- branch: `codex/major-update-batch0-20260713`;
- reviewed HEAD: `cda6a130`;
- implementation commit: `cba22720`;
- implementation record commit: `cda6a130`;
- worktree 在审查和聚焦验证后均为 clean；
- 没有安装 Runtime、启动游戏、运行完整 Release、L0-L5、GC 或长测；
- 没有接触 Steam 发布、订阅目录或玩家存档。

相关权威：

- [MoreEquipmentSlots Update](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- [旧 Sidecar 迁移 Review](../../manual-qa/2026/20260729-0001-moreequipment-legacy-sidecar-migration.md)
- [0.5.5 发布入口与过渡矩阵 Review](20260729-0002-dtmapi-055-release-entry-and-transition-matrix-audit.md)
- [Save commit semantics](../../../../../PROJECT.md)
- [Batch 6 identity contract](../../../../architecture/batch6-managed-mod-identity-contract.md)

本次是一次 audit-only 工作，拥有这一份 Review，不新建 Update、receipt、
schema、checkpoint 或第二套发布门。后续实现仍由现有
`20260723-0008` Update 管理。

## Result

结论为：

```text
P0 = 0
P1 = 4
P2 = 3
```

`cba22720` 修复了最初发现的主要路径：

- 能区分 nested Product v3、flat schema 1-3、future、ambiguous 和
  invalid；
- scoped flat schema 2/3 能转换成 Product v3；
- Product 文档先写临时文件、回读验证，再原子发布；
- global 源有 SHA-256 migration stamp 和归档收口；
- Compatibility Host 能把明确 flat schema 2/3 交回旧 parser；
- 已有 Product v3、future、ambiguous 和 invalid 保持 fail closed。

因此，“当前测试构造出的 schema 2/3 主路径”是真修复，不是空壳或只改文档。
但不能据此判定真实旧玩家数据问题已经全部修复。下列四项仍会造成旧物品
不可达、采用错误存档的数据、迁移永久卡死或冷恢复错误回落，均须在玩家验收
前修正。

## Findings

### P1-1: 真实历史 global 文档没有 `schemaVersion`

当前探测器把缺失 `schemaVersion` 解释成 `0`，而 legacy 支持范围固定为
`1..3`：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:76`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:107`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:181`

历史源码可由下列命令复核：

```powershell
git show e7d8322c^:src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs
```

该公开时期的 global writer 只写：

```text
ownerId
savedAt
slots
```

它没有 `schemaVersion`、archive、player 或 save clock。该时期
MoreEquipmentSlots 已存在公开版本身份，因此不能把这种文件当成理论 fixture。
当前代码会把它识别为 `UnsupportedLegacy`；字节不会被删除，但玩家的物品
不可达。

所需修复：

1. 增加精确的 `PreSchemaGlobal` 形状，不得把任意 schema `0` JSON 都当成
   legacy。
2. 要求精确 owner、已知字段和合法槽内容；存在 Product/scoped 权威时拒绝。
3. 由于该格式天生没有 save identity，必须明确兼容策略。推荐按历史语义只
   允许一次受控采用：当前 save 没有任何 scoped 权威、源字节和 hash 不变、
   迁移后立即留下单次 claim/stamp 并归档 global，避免被多个存档重复采用。
4. 若不接受上述历史兼容例外，则必须提供显式 Doctor/manual recovery，
   不能静默创建空 Product 文档。

### P1-2: 迁移丢失旧文档已有的存档修订身份

当前 `LegacyDocument` 没有读取 `savedTotalGameSeconds`，
`LegacyScopeMatches` 没有验证 `storageScope`，并把缺失 archive/player
视为通配：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:190`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:207`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:305`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotLegacyMigration.cs:738`

此外，`globalSource=true` 会完全跳过 scope 检查。旧 Compatibility 策略原本
会同时验证 archive、player 和 total game seconds：

- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotProtectedStoragePolicy.cs:43`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:930`

因此：

- native 存档已经回退时，领先于 native save 的旧 sidecar 仍可迁入；
- 带明确其他存档身份的 global 文件也可能被当前存档采用；
- Product v3 的 `EquipmentSlotSaveScope` 只有 archive/player，没有保存修订
  字段，迁入后无法继续表达原有 save-clock 保护。

这违反项目的 save-commit 语义：普通玩法状态不得领先最后一次成功的 native
`SaveGame`。

所需修复：

1. 迁移上下文必须包含当前 archive、player、storage scope 和 native
   total-game-seconds/save revision。
2. 身份完整的 flat schema 2/3 必须复用旧保护策略的匹配规则；空 owner、
   错 owner、错误或回退 save 均 fail closed。
3. 对带明确身份的 global 文档也必须验证，不得因物理路径是 global 而绕过。
4. 明确 Product committed 文档如何保留 native revision。推荐在存储契约中
   持久化并在加载时验证，而不是只在首次迁移时检查。
5. 真正无身份的 `PreSchemaGlobal` 仅能走 P1-1 的受控历史例外。

### P1-3: “精确旧字节备份”不是原子发布，失败后不能自动重试

`EnsureExactLegacyBackup` 直接把源复制到最终确定性备份路径：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.cs:318`

当前顺序是：

```text
File.Copy(source, finalBackup)
→ 计算 finalBackup hash
```

如果进程、机器或磁盘在复制过程中中断，最终路径可能留下部分文件。下次启动
会看到最终路径已经存在，再因 hash 不匹配永久 fail closed；原 sidecar 虽然
仍完好，迁移却不能收敛。

所需修复：

```text
same-directory temporary backup
→ Flush(true)
→ 回读并校验 hash
→ 原子发布到 final backup
```

若 final backup 是不完整、非权威副本，而 source 仍与预期 hash 一致，应能
隔离或替换坏副本并重试。至少增加备份发布前、发布中、发布后重入三组故障
注入。

### P1-4: scoped 权威失败后仍会回落到同 owner 的 global 文件

冷恢复先扫描 scoped，再扫描 global：

- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:1344`

ambiguous、invalid 或 future scoped 文档会返回 `handled=true` 并记录失败，
但调用方没有把该 owner 标记为已经被 canonical scoped 权威占用：

- `src/DTMAPI.GameBridge.DolocTown.Compatibility/EquipmentSlots/EquipmentSlotsProductColdRecovery.cs:61`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:1400`

随后同 owner 的有效 global flat 文档仍可进入旧 parser：

- `src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityService.cs:1417`

这会在保留损坏或未来 scoped 文件的同时，把陈旧 global 物品放回原生背包或
邮件；以后 scoped 文件修复时可能形成错误状态或重复物品。

所需修复：

- scoped canonical 文件只要存在并声明该 owner，无论 valid、future、
  ambiguous 或 invalid，都必须遮蔽同 owner global fallback；
- 增加独立的 claimed/blocked owner 集合，不把“恢复成功”作为唯一 owner
  占用条件；
- 测试 `scoped invalid|ambiguous|future + same-owner valid global`，要求
  native backpack/mail 增量为 `0`、两边字节不变且错误可见。

### P2-1: Product `.previous` 冷恢复分支实际上不可达

`EquipmentSlotDocumentStore.TryLoadValidated` 支持 live 失败后读取
`.previous`：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.cs:555`

但 mandatory demand proxy 和 Compatibility 扫描只把现存的 live
`*.json` 加入候选；cold backend 又在调用 store 前先探测 live。只有
`.previous`、或 live 已损坏时，预期 fallback 不能按当前入口到达。

修复可与 P1-4 的 canonical authority 扫描一起完成。至少覆盖：

- live 缺失、合法 `.previous`；
- live 可恢复损坏、合法 `.previous`；
- live future/ambiguous 时不得错误采用 `.previous`。

### P2-2: Compatibility Host 为只读 Probe 编译了完整迁移实现

`DTMAPI.GameBridge.DolocTown.Compatibility.csproj` 链接了完整
`EquipmentSlotLegacyMigration.cs`，Host 实际只需要无副作用的格式 Probe。
这不增加 mandatory 默认加载 Runtime，但增加 shipped Host 的程序集和维护
体量。

建议在 correctness P1 修完后，把小型格式探测 DTO/枚举拆成独立共享链接
文件；转换、hash、备份和归档继续只属于 MoreEquipmentSlots ProductNative。
该项不是玩家验收阻断。

### P2-3: 文档宣称的覆盖范围超过实际测试

旧 Sidecar Review 的 Implementation Resolution 宣称 wrong owner/save、
重复/负数槽和所有迁移窗口均已有覆盖。实际新增 Product tests 没有覆盖：

- 真实 schema-less global；
- 显式 schema 1；
- 空/错 owner；
- 仅 `storageScope` 不匹配；
- save-clock 回退；
- 带明确错误身份的 global；
- duplicate/negative slots；
- 备份复制中断和重启收敛；
- invalid scoped 与 valid global 的 owner-level 冲突。

修复时应补齐自动测试，并在现有 Update、旧 Sidecar Review 和 Batch 6
contract 中校正当前状态。不得新增测试 receipt 家族。

## Focused Validation Performed

通过仓库公共 .NET 8 resolver 构建
`tests/DTMAPI.UnitTests` Release，结果为：

```text
Build: 0 errors
Warnings: 10 existing DebugConsole nullable warnings
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
```

这些结果证明当前 schema 2/3 fixture 的主要转换和既有冷分流没有退化，
不覆盖上述四个 P1，也不是玩家实机迁移证据。

## Correction Gate Before Any Game Launch

先完成四个 P1，再运行下列最小自动矩阵：

1. 精确历史 `PreSchemaGlobal`、显式 schema 1、schema 2 和 Host flat
   schema 3 的识别及逐字段转换。
2. wrong/empty owner、storage scope、archive、player 和 save-clock rollback
   全部 fail closed。
3. global 带明确错误身份时不得采用；无身份历史 global 只走一次性兼容例外。
4. legacy slot duplicate、negative、越界、第四个非空槽、非法盾值和坏 JSON
   全部保留源字节且不发布空 Product。
5. backup temp/flush/hash/publish 每个故障窗口均可重试收敛。
6. `scoped invalid|ambiguous|future + valid global` 不得回落。
7. live/previous 的发现、优先级和 fail-closed 规则可达。
8. flat schema 2/3、Product v3、invalid/future 只进入一个 parser。
9. 既有四个 focus 继续通过；若修改 Catalog、ABI、Doctor 或包，再运行对应
   聚焦门。

自动矩阵未通过前，不启动游戏，也不运行完整 Release。

## Bounded Transition Test Plan

### 1. Goal

证明实际发布顺序不会让额外栏物品或盾牌丢失、重复或不可见：

```text
旧 Runtime + 旧 MoreEquipmentSlots
→ Runtime 0.5.5 + 旧 MoreEquipmentSlots
→ Runtime 0.5.5 + MoreEquipmentSlots 1.0.0
```

同时证明：

```text
Runtime 0.5.5 + 新旧 Product 都缺席
→ Compatibility Host 按需冷恢复
→ 物品恰好一次进入原生背包或邮件
```

本方案不验证 GC、长测、L0-L5、完整 Release 或其他产品玩法。

### 2. Frozen Inputs

运行前必须绑定：

- 公开旧 Runtime `0.5.2-alpha` 的精确保留 ZIP/tree/hash；
- 旧 MoreEquipmentSlots `0.3.1-dtmapi`，Workshop `3744059735`：
  - `9` files；
  - `539,565` bytes；
  - retained tree SHA-256
    `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`；
- P1 修正后的最终 Runtime `0.5.5` 候选 tree/hash；
- P1 修正后的 MoreEquipmentSlots `1.0.0` ZIP/tree/hash；
- 精确 HEAD、tracked tree、工具链和实际 Loader source。

不得沿用 `cba22720` 修复前或本次 P1 修复前的候选包 hash 作为最终发布
authority。

旧 Runtime `0.5.2` 没有证据支持当前 QA save redirection。不得用它在玩家
存档或云存档上睡觉保存来制造旧基线。旧数据输入应来自：

- 冻结的真实旧 sidecar；或
- 由历史 writer 精确生成并校验的字节 fixture。

旧 Runtime 只用于标题页/minimum-version 拒载 canary。

### 3. Disposable Save Fixture

所有会调用 native save 或修改 sidecar 的运行使用与 Steam AutoCloud 隔离的
disposable fixture：

- 从选定的第三存档单向复制；
- 不含 `steam_autocloud.vdf`；
- 不含 junction、reparse point 或指向玩家存档的链接；
- runner 独占 save、state、profile 和 config 根；
- 不在结束后写回玩家存档；
- NativeSaveExpected 和 ArchiveMutation 结果只留在 disposable fixture。

从同一个只读基线分别克隆：

```text
direct-schema2
host-schema3
pre-schema-global
cold-recovery
```

最小真实物品：

- 普通被动饰品 `grandmas_button`；
- 部分消耗耐久的盾牌 `box_hat`；
- 如真实固定三槽需要，再加入一个 defense-only hat；
- 测试前原生背包和邮件中的这些 item id 基础数量优先为 `0`。

### 4. Unified Exactly-Once Invariant

对每个物品 `i`：

```text
ΔNativeBackpack(i)
+ ΔUnacceptedMail(i)
+ ActiveLegacyCommittedSlot(i)
+ ActiveProductCommittedSlot(i)
= 1
```

终态还必须满足：

- gameplay candidate 和 recovery journal 均不存在；
- `.legacy-migrations` 精确字节备份不是 active authority；
- `.migrated-*` global 归档不是 active authority；
- 扫描器忽略上述备份和归档；
- item id、数量、固定槽顺序、被动/防御字段完全一致；
- 盾牌的 `isShield`、current/max durability 和 defend 值完全一致；
- 标题、Loader 停用和退出后 Hook、callback、session、owner roots 为零。

### 5. Short Unity Matrix

#### C0: 旧 Runtime + 新 MoreEquipmentSlots 拒载 canary

```text
Runtime 0.5.2 + MoreEquipmentSlots 1.0.0
Mode: NoNativeSave / title only
```

要求：

- minimum Runtime `0.5.5` 在 Assembly/Entry 前 fail closed；
- Manager、错误页或日志明确显示当前版本、最低版本和手动安装提示；
- Product 不安装 Hook、不读取或迁移 sidecar；
- 游戏标题和退出仍可用；
- save archive 和 sidecar 不变。

这证明玩家先收到 Workshop Product 更新、尚未手动安装 Runtime 时不会让新
Product 半启动。它不证明迁移功能。

#### U1: Runtime 0.5.5 + 精确旧 MoreEquipmentSlots

```text
Runtime 0.5.5 + retained MoreEquipmentSlots 0.3.1
Mode: NativeSaveExpected
Fixture: host-schema3
```

要求：

- 新 Product 不存在；
- 冻结旧 `IEquipmentSlotsApi` Compatibility 路线加载；
- 旧额外栏 UI、普通物品、盾牌和效果正常；
- 通过一次正常睡觉完成 native save；
- Host 写出真实 flat schema 3；
- 物品仍各恰好一份；
- 标题和 Loader 清理归零。

这同时证明 R0 后旧 Mod 可继续运行，并为 U2b 生成真实 Host schema 3。

#### U2a: 旧 flat schema 2 直接升级

```text
Runtime 0.5.5 + MoreEquipmentSlots 1.0.0
Mode: ArchiveMutation
Fixture: direct-schema2
```

要求：

- 原生 current/prev/bak archive 的 length/hash/mtime 不变；
- canonical sidecar 成为合法 nested Product v3；
- 同一次 SaveLoaded 中物品、效果和盾牌耐久可用；
- 原 flat bytes 与 `.legacy-migrations` 备份 hash 一致；
- legacy/global 不再是 active authority；
- exactly-once、事务终态和 owner 清理通过。

#### U2b: 经 0.5.5 旧 Mod 中转后升级

```text
U1 产生的 Host flat schema 3
→ 只替换为 MoreEquipmentSlots 1.0.0
Mode: ArchiveMutation
```

要求与 U2a 相同，并明确证明“相同数值 schema 3、不同 JSON 形状”的 Host
文档不会被误当成 Product v3。

#### U2c: 真实 schema-less global 升级

```text
Runtime 0.5.5 + MoreEquipmentSlots 1.0.0
Mode: ArchiveMutation
Fixture: pre-schema-global
```

要求：

- 输入字节必须由历史 writer 或真实保留文件证明；
- 没有 scoped Product/legacy 权威；
- 只按 P1-1 最终敲定的策略采用一次；
- Product v3 验证成功后才归档 global；
- 冷重启不得再次采用或复制；
- 发生身份歧义时保持可见 fail closed，不创建空 Product。

#### U3: Product 缺席时的 Host 冷恢复

```text
Runtime 0.5.5 + old/new MoreEquipmentSlots both absent
Mode: NativeSaveExpected
Fixture: cold-recovery
```

要求：

- Compatibility Host 只按 sidecar demand 启动；
- flat 文档只进入旧 parser；
- 至少一件物品通过真实背包放置，背包满时至少一件走真实邮件；
- 只有真实 native `SaveSaved` 后提交恢复事务；
- active legacy/Product 槽清空；
- 每件物品恰好一份；
- Product Hook 和 owner 从未出现，Host 清理归零。

#### U4: 冷恢复后的独立重启

```text
Continue U3 fixture
Mode: NoNativeSave
```

要求：

- 不重复投放背包或邮件；
- archive 与 committed sidecar 在本轮清理前不变；
- journal/candidate 保持终态；
- 每件物品仍恰好一份；
- 退出无残留进程。

#### One migrated-save semantic spot check

只在 U2b 的迁移结果上增加一次聚焦复核，不对每种 schema 重复：

1. 伤盾或破盾后不保存返回标题，冷重启恢复迁移后的 committed 耐久和物品；
2. 再次伤盾后正常睡觉保存，冷重启保留新状态；
3. 两条路线都满足 exactly-once。

### 6. Failure And Rerun Policy

每个阶段从只读基线的新 clone 开始。失败后只重跑受影响的最小阶段：

- converter/identity 修复：聚焦 Unit + U2a/U2b 或 U2c；
- backup/retry 修复：故障注入 + 对应一个 U2；
- cold Host 修复：cold-host Unit + U3/U4；
- Loader/minimum-version 修复：C0；
- fixture/runner 隔离修复：仅重跑受影响的 save mode。

不得因单个短测失败从 C0 开始重跑整套矩阵，也不得增加 checkpoint receipt
或缓存 PASS authority。

### 7. Publication Checkpoints

定义：

```text
R0 = Runtime 0.5.5
R1 = AutoFishing 1.0.0
R2 = MoreEquipmentSlots 1.0.0
```

R0 前：

- U1 证明 `0.5.5 + 精确旧 MoreEquipmentSlots`；
- U3/U4 证明旧 Mod 缺席时不会让旧物品不可达或重复恢复。

R2 前：

- C0、U2a、U2b、U2c 及 migrated-save spot check 全部通过；
- 重新冻结 Product package/hash。

R0 上传后：

- 从 Workshop 重新下载实际 Runtime，绑定 subscription tree/hash；
- 用真实旧 MoreEquipmentSlots subscription 重跑 U1。

R2 上传后：

- 从 Workshop 重新下载实际 MoreEquipmentSlots，绑定 tree/hash；
- 从一份未使用的 schema 2 fixture 重跑 U2a；
- 若下载字节与冻结候选完全一致，不重复 U2b/U2c/U3/U4。

### 8. Relationship To Complete Release

本方案不是完整 Release 的替代，也不要求现在运行完整 Release。

前一份完整 Release PASS 早于 `cba22720` 和本 Review 要求的 P1 修复，不能
作为最终修正候选的完整发布证据。完成：

```text
P1 source corrections
→ focused automatic matrix
→ bounded C0/U1/U2/U3/U4 player acceptance
→ freeze final candidate
```

之后，在最终发布边界只运行一次从头开始的完整 Release，再做 Workshop
玩家包审计。若完整 Release 中途失败，遵守既有 focused/tail 规则，不在每次
修复后从头重复。

## Handoff

Implementation, changed files, package hashes, validation results and current
acceptance state are owned by
[Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md).
The later independent correction audit is
[Review `20260729-0005`](20260729-0005-moreequipment-migration-correction-update-audit.md).
This Review remains the pre-implementation finding set and bounded transition
plan; it does not claim that any finding or acceptance stage is closed.
