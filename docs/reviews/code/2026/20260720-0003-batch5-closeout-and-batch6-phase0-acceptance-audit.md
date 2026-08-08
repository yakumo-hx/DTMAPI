# Batch 5 收尾与 Batch 6 Phase 0 验收审计

## 记录状态

- 日期：2026-07-20
- 状态：`recorded / Batch 5 bounded closure accepted / Batch 6 Phase 0 correction required / G2 Runtime blocked`
- 性质：独立源码、合同、收据、测试与路线权威审计；不是 Runtime 修复或发布授权
- 审计基线：Batch 5 收尾提交 `653487b7463778c23e9b96aed9ef713364def22a`；Batch 6 Phase 0 提交 `8e24a9e4b79825801a2abd7c76c75413cd0943d8`
- Source：用户要求在 Batch 5 收尾与 Batch 6 Phase 0 完成后进行一次审计
- Owning Update：[20260720-0004 Audit Update](../../../updates/2026/20260720-0004-batch5-closeout-batch6-phase0-audit.md)
- 主要输入：
  - [Batch 5 Update](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
  - [Batch 6 前置审查](20260719-0012-batch6-boundary-correction-prerequisite.md)
  - [Batch 6 Phase 0 Update](../../../updates/2026/20260720-0003-batch6-phase0-closure.md)
  - [受管 Mod 身份合同](../../../architecture/batch6-managed-mod-identity-contract.md)

本次只读审查 Runtime、产品、SDK、合同、测试和历史证据，并新增 Review、Update 与月度索引记录。没有修改 C#、schema、manifest、产品包、游戏目录或 Workshop 状态；没有启动游戏或取得共享 Runtime lock。

## 一、最终裁决

| 验收面 | 裁决 | 说明 |
| --- | --- | --- |
| Batch 5 源码收尾 | `PASS` | 前次 ContentQuery/CustomAnimals terminal-authority 缺口已经修复；未发现新的 P0/P1 Runtime 回归。 |
| Batch 5 有界实现与既有玩家证据 | `PASS` | no-demand、ActionSpeed、AutoFishing、Candidate11、Published11 和证据保留的既有闭环仍成立。 |
| Batch 5 量化 GC/内存预算 | `NOT ESTABLISHED` | 当前证明的是结构、生命周期、趋势和测试窗口内无 Fatal；Mono/Unity 每动作分配与数值预算仍未建立。 |
| 0.5.5 发布候选 | `BLOCKED` | 当前源码提交尚无重新构建并完成发布矩阵的 commit-provenance 候选；历史候选仍绑定旧 `BuildCommit=c93c460e5b7a`。 |
| Batch 6 Phase 0 无越界实现 | `PASS` | `653487b7..8e24a9e4` 没有游戏加载 C#、live schema/template 或产品 manifest 变化；Advanced、G2 fixture 和真实产品迁移均未实现，`SDK160` 仍保留。 |
| Batch 6 Phase 0 方向与清单骨架 | `PASS WITH CORRECTIONS` | 四身份、四所有权类别、23 域、115 文件分类和 G2 原子设计方向成立。 |
| Batch 6 Phase 0 机器门 | `FAIL / CORRECTION REQUIRED` | 保留字段、真实消费者、采集时间和 mandatory Runtime 零增量均存在“自述一致即可通过”的假阳性。 |
| 进入 G2 Runtime | `BLOCKED` | 只允许先做 Phase 0 correction slice；修正并复验前不得修改 Loader/SDK/Core/Doctor/Manager 的 Advanced live wire。 |

所以，Batch 5 可以按“有边界的实现闭环”验收，但不能解释成 0.5.5 已可发布。Batch 6 Phase 0 没有方向性返工，也没有产品越权；其完成声明应降为 `conditional / correction required`，下一项工作不是 G2 实现，而是关闭本 Review 的四项 G2 前置 P1 并同步活动权威。

## 二、Batch 5 收尾复核

### 2.1 已关闭的实现缺口

- ContentQuery 的 candidate 不再在 terminal receipt 前发布；可见输入只在提交成功后交换，stale、pre/post-commit fault 与 last-good 路径由 `DtmApiRuntime.cs`、`WorkshopContentInputUi.cs` 及对应 Batch 5 测试覆盖。
- CustomAnimals rejection observer 已延后到 terminal authority 后；post-commit fault 不再跳过 zero-to-one/one-to-zero demand reconciliation。
- Catalog 已机器解析 no-demand、ActionSpeed 24 个子阶段和 AutoFishing 六阶段，并继续明确 `quantifiedBudgetStatus=not-established`。
- evidence-retention schema 3 已冻结五个 GC root、一个 no-demand root、一个 Candidate11 root 和三个 Workshop audit root；cleanup 对缺失/reparse root fail closed，并比较完整文件 manifest SHA-256。

本次完整 Release suite 在当前 HEAD 通过，未观察到这些修复回退。由于 Phase 0 没有修改游戏加载 C#，本审计没有重复启动游戏；既有第三/第五存档证据仍由 Batch 5 Update 和 active smoke/evidence 记录持有。

### 2.2 仍然有效的边界

- Batch 5 的性能结论仍是“真实梯度执行、生命周期/结构/趋势门通过，测试窗口内未复现 Fatal”，不是“量化 GC 预算通过”。
- 当前源码与历史最终候选没有同一提交来源，因此不得解除 0.5.5 release stop。发布前仍需在最终提交重建候选并重复 package hash、Catalog、Workshop 与必要玩家矩阵。
- 这两个限制是已有、已如实记录的 release work，不是本轮发现的新 Runtime 缺陷。

### P2-1：Candidate11 最终收据仍未由 Catalog checker 机器解析

活动 Batch 5 Update 与 evidence-retention 清单持有最终 Candidate11 `99-result.json`；Catalog 只声明该交易通过，`check-product-catalog.ps1` 不打开终态收据。`test-candidate11-source-transaction.ps1` 验证的是交易工具合同，不等于验证这条历史终态收据。

当前收据经独立核对可信，故不推翻 Batch 5 验收。后续重建 0.5.5 候选时，应让 Catalog checker 直接校验 Candidate11 terminal status、十一产品、源码/安装/恢复 hash 与进程退出。

### P2-2：GC 梯度 checker 的阶段身份交叉绑定不完整

现有 checker 验证阶段状态、domain、存档、时长、样本、行为和来源，但没有完整交叉绑定：

- `StageId` 解出的 Level/Workload 与 stage 自述 Level/Workload；
- `runtime-metrics.json` 的 Domain/Level/Workload/RunId 与 owning stage/plan。

本轮独立核对的 30 个已完成阶段均一致，因此这是防止未来收据错配的硬化项，不是现有证据失败。

## 三、Batch 6 Phase 0 通过项

- 当前仍只有 Strict CodeMod live wire；Advanced、`CodeModKind` 模型、Content Host、G2 fixture 与任何产品迁移都没有进入 C# 或 live schema/template。
- 所有当前第一方产品仍为 `netstandard2.0` 且只直接引用 `DTMAPI.Abstractions`；没有第二个 BepInEx plugin entry，也没有产品直接进入 `BepInEx/plugins`。
- 现有八个省略 `Type` 的 legacy manifest、`SDK160`、one-bootstrap/four-dependency Runtime 拓扑与 0.5.5 retained ABI 均未被破坏。
- 固定提交 `653487b7` 的 23 域、115 文件、LOC/blob/tree-hash baseline 可以逐字重建；Phase 0 的身份/所有权词汇与 G2 原子 vertical slice 设计基本正确。
- G1 完整 ownership、G2 Runtime、AutoFishing pilot、其他产品迁移和 G7 Content Host 都仍被文本标为未完成，没有把规划冒充实现。

这些事实说明 Phase 0 不需要推倒重来。问题集中在机器合同能否证明其声明，而不是身份/所有权方向本身。

## 四、Batch 6 Phase 0 阻断项

### P1-1：G2 保留字段当前不是 fail closed

[身份合同](../../../architecture/batch6-managed-mod-identity-contract.md)声明 G2 前 `CodeModKind` 是 invalid，当前 schema/parser 不接受它。实际链路却允许未知字段通过：

- `author-sdk/schemas/manifest.schema.json` 没有顶层 `additionalProperties: false`；
- `JsonSupport.RuntimeManifest` 没有 `UnmappedMemberHandling=Disallow`；
- `ProjectValidator` 同时保留原始 `JsonObject`；
- `DeterministicPackager` 深拷贝原始 manifest，因此手写字段会进入包；
- Core 的 `ManifestReader`/`JsonFile` 会忽略未建模成员；
- `test-batch6-phase0-contract.ps1:236-251` 只检查 tracked model/schema/template 中没有该 token，没有向 SDK、打包器、部署/Doctor 和 Core 注入 hostile manifest。

因此作者当前可以手写 `"CodeModKind":"Advanced"`，SDK 会保留并打包，Runtime 则按普通 `CodeMod` 解释。它不会获得 Advanced 能力，但“保留身份在 G2 前明确拒绝”的安全断言不成立。

**G2 前修正门：** 在 manifest schema、SDK validate/pack/deploy、Player Doctor 和 Core pre-load 路径建立同一未知/保留身份拒绝语义，并增加真实 hostile-package 负向矩阵；或者明确修改合同为“G2 前忽略未知字段”，但后者会削弱预留身份的 fail-closed 目标，不推荐。

### P1-2：G1 把不存在的 AnimalPack 产物记成当前真实消费者

`batch6-phase0-domain-contract.json` 的 `animal-pack` 行把 `DTMAPI.AnimalPack planned product` 放入 `currentConsumers`，并记录 `realConsumerCount=1`。Catalog 同时把该产品标为 `FrozenReservedNoArtifact`，仓库没有对应源码、manifest 或 artifact。

`test-batch6-phase0-contract.ps1:395` 只检查数字等于字符串数组长度，所以计划对象也能自证为“真实消费者”。这使“每域 current consumers and exact count”的 G1 收据不成立。

**G2 前修正门：** 当前消费者改为零，计划对象进入独立 `plannedConsumers`/`targetProduct` 字段；checker 必须把 current consumer 绑定到 Catalog identity、tracked source/manifest 或明确的外部二进制收据。计划项不能参与 shared-native 的两真实消费者判定。

### P1-3：Phase 0 收据使用了不可能成立的采集时间

domain contract、baseline receipt 与 Update 0003 都记录 `capturedAtUtc=2026-07-20T12:00:00Z`。包含它们的提交 `8e24a9e4` 提交时间为 `2026-07-20T05:58:21Z`；本轮审计开始时也尚未到 `12:00Z`。checker 又把该未来时间硬编码为期望值，所以只能证明多个文件复述一致，不能证明收据时序真实。

源码/blob/hash内容仍可重建，本 finding 不否定 baseline 内容；它否定的是时间 provenance。

**G2 前修正门：** 使用真实 UTC 重新生成合同与收据，冻结生成命令/源提交/收据提交，并校验 `source commit time <= capturedAtUtc <= receipt commit time/current trusted clock`。禁止用预定整点充当已发生事实。

### P1-4：mandatory Runtime ProductNative “零增量门”目前只是常量声明

`build-batch6-phase0-baseline.ps1:81-92` 始终从合同指定的固定提交 `653487b7` 重放 baseline；`test-batch6-phase0-contract.ps1:396` 只检查每个域把 `targetMandatoryRuntimeProductLocDeltaMax` 写成零。它不比较 baseline 与当前 HEAD，也不判定新增文件属于 Platform、SharedNative 或 ProductNative。

因此未来即使把 ProductNative 源码加进 mandatory Runtime，只要 JSON 中的目标常量仍是零，Phase 0 gate 仍会通过。Update 0003 所写 “enforces a zero allowed ... delta” 属于过量声明。本次 `653487b7..8e24a9e4` 实际没有游戏加载 C# 差异，所以本次零增量事实成立；缺的是保护未来 G2/G4 的可执行门。

**G2 前修正门：** 新增 baseline-to-audit-HEAD 差异收据，覆盖 mandatory assembly/file/physical LOC/category；每个新增/移动文件必须有机器可核的 ownership classification。门必须检查实测 delta，而不只是目标值。

### P1-5：两份活动权威仍会给后续任务错误输入

- `docs/api/public-api-matrix.md:51-52,128` 仍把 Runtime 当前基线写成 `0.5.3-alpha/0.5.3.0`，并把 `0.5.4-alpha` 当作 future-blocked；当前 release authority 已是 `0.5.5`/binary `0.5.5.0`/assembly compatibility `0.5.3.0`，测试中的下一 future-blocked 是 `0.5.6-alpha`。历史 ABI `0.5.3.0` 应保留，但不能继续冒充当前 Runtime 版本。
- 路线图 `20260712-dtmapi-lightweight-functional-mod-roadmap.md:283` 写“高级通道建立后优先迁回 Zoom”，与同文件 `:104,147,251-257` 及身份合同的“G2 fixture 后只准入 AutoFishing pilot”冲突。

**G2 前修正门：** API matrix 按版本权威区分 Release、Binary、Assembly compatibility 三种投影；路线图 Zoom 行改成 AutoFishing 完成完整 proof 后才重新准入。Phase 0 checker 应至少验证这两个活动权威的关键路线/版本投影，避免局部修正文档再次漂移。

## 五、修正后的开工顺序

```text
Batch 5 bounded closure（已接受）
  ├─ 独立 release work：最终提交重建 0.5.5 候选与发布矩阵
  └─ Phase 0 correction slice
       1. reserved/unknown identity hostile negative
       2. current/planned consumer 分离及证据绑定
       3. 真实采集时序和可复现收据
       4. baseline -> audited HEAD 的 mandatory Runtime 实测 delta
       5. API version 与 AutoFishing-first 路线权威同步
       6. 定向门 + 完整 Release suite
            -> G2 synthetic Advanced fixture
            -> 唯一 AutoFishing pilot
            -> G3/G4/相关 G5-G6
            -> 其他产品重新准入
```

Phase 0 correction slice 可以修改合同、checker、schema 的“拒绝当前保留字段”行为和活动文档，但不能借修门之名实现 `CodeModKind` live model、放宽 `SDK160`、迁移 AutoFishing，或把任何第一方产品改成 Advanced。

## 六、验证

- `tools/scripts/test-batch6-phase0-contract.ps1`：PowerShell 7 与 Windows PowerShell 5.1 均通过；该结果证明当前 23 域/115 文件合同内部一致，也同时复现了本 Review 指出的门禁盲区。
- 定向 Catalog、evidence-retention、Batch 5 GC ladder、no-demand 与 no-QA deadline 测试均通过。
- `tools/scripts/test.ps1 -Configuration Release`：当前 HEAD 完整串行复跑退出 `0`，耗时约 `680.7s`；所有 managed build 零 warning/零 error，Unit/QA、Doctor、Author SDK、transaction、Catalog、Batch 4/5、ABI、artifact 与 docs governance 均通过。
- 第一次 full-suite 尝试在 UnitTests 已通过后遇到另一并发 UnitTests 进程，受管 cleanup 按设计拒绝清理活动 PID；串行重跑通过，因此不分类为产品或测试失败。
- 当前 HEAD 为 `8e24a9e4b79825801a2abd7c76c75413cd0943d8`；审计开始时 tracked worktree clean。
- 没有运行游戏；`DolocTown.exe` 不存在，没有修改 Runtime/Workshop/OfficialLocal，也没有新增 smoke matrix 行。

完整绿灯不反驳 P1-1 至 P1-4：当前测试恰好把保留字段“未出现在模型中”、自述消费者计数、自述未来时间和目标零常量当成通过条件，尚未验证 hostile input、外部事实或 baseline-to-HEAD 差异。

## 七、结论

Batch 5 收尾可以正式接受为 bounded closure；现有 GC 口径和 0.5.5 release stop 保持不变。Batch 6 Phase 0 的架构方向、无越权事实和 baseline 骨架可以保留，但不能以当前机器门直接进入 G2 Runtime。

下一项唯一准入工作是 Phase 0 correction slice。P1-1 至 P1-5 关闭、定向门和完整 Release suite 再次通过后，才恢复“Phase 0 verified”并开始独立 G2 synthetic fixture Update。

## 八、2026-07-20 修正处置

[20260720-0005](../../../updates/2026/20260720-0005-batch6-phase0-machine-gate-correction.md) 已关闭本 Review 的 P1-1 至 P1-5：保留字段在 SDK/包/Core/Doctor 全链路 fail-closed；AnimalPack 与 mutable-only CustomAnimals prototype 不再计入真实消费者；schema-2 receipt 绑定真实提交/采集时序与 Catalog/source/artifact 证据；mandatory Runtime 门实测 baseline-to-audit 源码/blob/LOC 分类；API 与 AutoFishing-first 活动权威已同步。双宿主 focused gate、`725.2s` 完整 Release suite 和 `GAME-SMOKE/20260720-172620` 第三存档 smoke 均通过，独立复核未留 P0/P1。

Phase 0 因此恢复为 verified，但本 Review 的产品边界不变：只准入独立 G2 synthetic fixture；G2 通过前不得迁移 AutoFishing 或任何真实产品，0.5.5 发布阻断也不因本修正解除。
