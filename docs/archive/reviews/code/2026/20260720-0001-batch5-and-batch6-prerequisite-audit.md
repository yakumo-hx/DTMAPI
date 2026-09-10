# Batch 5 收口与 Batch 6 前置审计

## 记录状态

- 日期：2026-07-20
- 状态：`recorded / Batch 5 bounded implementation accepted / 0.5.5 release blocked / Batch 6 product migration blocked`
- 性质：独立代码、证据、发布合同与架构准入审计；不是 Runtime 修复或发布授权
- 范围：Batch 5 event/demand/content/lifecycle/performance、最终候选与证据治理；Batch 6 G0-G7、Advanced CodeMod、Content Host 与 current-build reverse 前置
- Source：用户要求“对 Batch 5 已完成内容进行一次审计，同时核对新增的 Batch 6 前置有关内容”
- Owning Update：[20260720-0001 Batch 5 And Batch 6 Prerequisite Audit](../../../updates/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md)
- 主要生命周期记录：
  - [Batch 5 Event / Demand / Content / Lifecycle / Performance Update](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
  - [前一次 Batch 5 Completion Audit](20260718-0003-batch5-completion-audit.md)
  - [Batch 6 边界修正前置审查](20260719-0012-batch6-boundary-correction-prerequisite.md)
  - [DLL Mod 入口模型审查 Update](../../../updates/2026/20260719-0001-dll-mod-entry-model-audit.md)

本次只修改审计文档和相应治理索引，不修改 Runtime、Loader、SDK、Mod、候选包或游戏环境。没有启动游戏、安装 Runtime、切换存档、修改 Workshop/OfficialLocal 内容或取得共享 Runtime lock。

## 用户截图转写与本次核对目标

用户提供的截图记录了一个中间检查点：当时 Batch 5 代码、全量测试、发布包和主要运行矩阵被描述为已经通过；普通无 QA 玩家测试已得到两条真实动物回执，但因用户要求停止 Computer Use 而在截止时暂停，仍需正常退出游戏、恢复现场、完成剩余运行验证、GC 梯度和最终文档闭环。

本次审计把截图观察与后续事实分开：后续确实产生了正常退出、恢复、最终 Candidate11、Published11、no-demand 和两个玩法域梯度证据；但“实现证据存在”仍不能自动推出“发布来源可复现、性能预算已量化、证据保留合同已闭环”。

## 一、审计裁决

| 边界 | 裁决 | 说明 |
| --- | --- | --- |
| Batch 5 旧 P1 源码缺陷 | `PASS` | 前次审计 P1-1 至 P1-5 均有当前源码与定向测试闭环，未发现新的 P0/P1 Runtime 回归。 |
| Batch 5 有界玩家/产品/生命周期验收 | `PASS` | 最终 Local11/Candidate11、Published11 enabled/disabled、真实 10,000 帧 no-demand、24 个 ActionSpeed 子阶段和六阶段 AutoFishing 均有可回溯终态收据。 |
| Batch 5 量化内存/GC 预算 | `NOT ESTABLISHED` | 梯度证明结构、生命周期、真实趋势和测试窗口内无 Fatal；运行时 allocation counter 不工作，runner 也没有 Mono/Unity/process/GC slope 或 L0/per-unit 数值阈值。不得写成“量化内存/GC 预算通过”。 |
| Batch 5 证据保留合同 | `BLOCKED` | 现有 allowlist 绿灯会漏掉正式 AutoFishing、Candidate11、Published11 disabled/timeout 和全部 `BATCH5-NO-DEMAND` durable root。 |
| Batch 5 发布来源/0.5.5 上传 | `BLOCKED` | 候选 DLL 与当前 Release 输出匹配，但候选 `BuildCommit=c93c460e5b7a` 不包含审计开始时的 146 项 dirty-tree 变化；Catalog 也仍主动阻止 Workshop upload/update。 |
| Batch 6 Phase 0 | `ADMITTED WITH CORRECTIONS` | 可以做 G0 权威统一、G1 清单/预算、消费者扫描、设计和证据工作。 |
| Batch 6 G2 Runtime 与 AutoFishing pilot | `BLOCKED` | G0 未通过；G2/G3 现有文字形成循环门禁；Advanced 身份尚未贯穿 manifest/Core/SDK/Doctor/Manager/package。 |
| Batch 6 其他产品迁移 | `BLOCKED` | G0-G7 无一达到 PASS，当前不得批量迁移。 |

因此，Batch 5 的 `verified` 可以继续表示“当前 dirty worktree 上有界实现与既有运行验收成立”，但不能解释成“当前候选已获 0.5.5 发布授权”或“量化 GC 预算已经通过”。Batch 6 只允许 Phase 0；产品迁移必须按本审计修正后的分层门禁推进。

## 二、前次 Batch 5 P1 逐项复核

| 前次 finding | 当前结论 | 复核证据 |
| --- | --- | --- |
| P1-1 空队列每帧重建详细诊断 | `closed` | `DtmApiRuntime.cs:1085-1159` 先用 queue/revision 标量判断；`HookStatusPublicationQueue.cs:21-42` 与 `EventManager.cs:117-128` 提供便宜状态。`Batch5EventKernelTests.cs:61-93` 和 `Batch5GameBridgeDemandTests.cs:148-235` 覆盖真实 Core 及 Core+GameBridge 暖机后 10,000 次路径。 |
| P1-2 需求释放后 retained Hook 仍做可选工作 | `closed within Batch 5 inactive-silence contract` | `DolocTownGameBridge.Demand.cs:11-45,207-233,397-437` 的 retained callback mask 在服务、反射和 fanout 前短路；Camera、Equipment、AutoFishing、Zoom 订阅边界已收窄；`Batch5GameBridgeDemandTests.cs:280-394,524-633` 覆盖全家族无需求。 |
| P1-3 内容 generation 在事务提交前发布可见状态 | `closed for current CustomAnimals/Audio consumers` | `ContentRefreshGenerationService.cs:413-480` 把可见交换串入 terminal commit callback；CustomAnimals/Audio 候选、交换、post-commit fault 隔离和 last-good 路径由 `Batch5ContentGenerationTests.cs:403-703` 覆盖。 |
| P1-4 退订绕过线程/发布 membership 边界 | `closed` | `EventManager.cs:469-552,802-970,1078-1207,1353-1481` 使用 immutable publication membership，Add/Remove 共用 mutation/owner guard；barrier、late subscriber、owner cleanup 和 off-thread 测试均存在。 |
| P1-5 route/cleanup 诊断不是物理真相 | `closed` | CoreLifecycle 使用 exact base-Hook closure probe；shutdown 能区分 `ProcessPinnedDormant`；`GameBridgeModOwnerCleanupParticipant.cs:15-54` 将 demand roots 纳入 authoritative removed count，定向测试验证精确计数。 |
| P1-6 玩家、Published11 和 GC/runtime 验收缺失 | `closed with performance-language limitation` | 终态证据见下一节。ActionSpeed 只能声明 24 个独立子阶段通过，不能声明其包含旧 AutoFishing continuation 的父 plan 整体通过。 |
| P1-7 Update/Catalog 当时过量或遗漏 | `runtime facts closed; durable contract still incomplete` | Catalog 已绑定最终 no-QA receipt，但 formal performance 只存在枚举/自然语言，不解析终态 performance roots；API matrix、旧审计 resolution、timeout 分类和 allowlist 仍有过期事实。 |

前次 P2-3 仍在：GameBridge 仍 eager 构造 feature roots，CustomAnimals 任一定义仍安装整组 Hook，旧 `UpdateGameBridgeFeatures` scheduler 路径仍保留，中央 retained-demand mask 已使用 30/32 bit。它们不是本次发现的 Batch 5 Runtime 回归，但必须进入 Batch 6 G5 的逐文件“通用底座 / 产品路由 / QA / Compatibility”重新分类；不得把“demand-inactive”误写成“物理归属正确”或“模块按需创建”。

## 三、Batch 5 终态证据核对

### 3.1 普通玩家与产品组合

- 最终普通 Local11/无 QA：`docs/debug/evidence/GAME-SMOKE/20260719-214048/result.json` 为 `RunStatus=Passed`，第三存档、真实输入/auto-drive provenance、五个 Runtime DLL、无 QA、YConsole、EquipmentSlots、恰好两次 causal AnimalViewer render、native close、deadline、恢复、无 Fatal 和进程退出均通过。
- Candidate11 transaction：`docs/debug/evidence/CANDIDATE11/batch5-final-manual-handshake-20260719-214044/99-result.json` 为 `Passed=true`，十一产品、Runtime binding、候选源码未变、smoke 后匹配、精确恢复和进程不存在均成立。
- Published11 enabled：`docs/debug/evidence/GAME-SMOKE/20260719-212635/result.json` 通过。
- Published11 disabled/CoreOnly：`docs/debug/evidence/GAME-SMOKE/20260719-212758/result.json` 通过。
- `docs/debug/evidence/GAME-SMOKE/20260719-130406/result.json` 与 `20260719-212913/result.json` 属于 SaveLoaded 前的 deadline/operator-handshake timeout；前者有 terminal result，二者都不能写成产品行为失败。

### 3.2 No-demand 与玩法梯度

- No-demand：`docs/debug/evidence/BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000/result.json` 与 `docs/debug/evidence/GAME-SMOKE/20260719-195806/result.json` 通过 300 帧 warmup + 10,000 个真实 Unity frame。optional demand/updater 为空，文件、目录、projection、reflection、native updater、retained callback、event snapshot、event/Hook diagnostic revision delta 均为零。它证明可选调用/节奏静默，不证明整局零分配。
- ActionSpeed：`docs/debug/evidence/BATCH5-GC-LADDER/20260719-044157-70ce39e6/ladder-plan.json` 的父状态是 `failed`，原因是其后续旧 AutoFishing-L0 continuation 失败；但 `ActionSpeed-L0..L5-{Tool,Interact,Eat,ContinuousUse}` 共 24 个 `stage.json` 都是 `completed`，600 秒、21 samples、`ForcedGc=false`，行为、十一指标类别、source/load/restore、SaveLoaded、save restore、无 Fatal 和进程退出均通过。当前可采信对象是 24 个 child receipts，不是父 plan 的整体状态。
- AutoFishing：`docs/debug/evidence/BATCH5-GC-LADDER/formal-autofishing-vitals250-final-20260719-2014/ladder-plan.json` 为 `completed`，第五存档 L0-L5 六阶段均为 600 秒/21 samples/无 forced GC/十一指标类别可用；L4 独立验证 disable/native recovery，L5 独立验证 title reload cycle。
- 六个 AutoFishing 阶段的 allocation probe 都是 `blocked-allocation-counter-nonfunctional`，符合既有安全口径。Mono used 在窗口内变化，但 Mono heap 固定、process private 有升有降、DTMAPI-owned roots/records/demand 结构稳定；这些数据既不能自动判定 leak，也不能自动判定“内存预算通过”。

### 3.3 包与来源

- `dist/Batch5-Final-Candidate-20260719-1848` 包含 Runtime + 十一产品；Runtime 恰好五个玩家程序集且无 QA/Smoke/HookProbe payload。
- 五个 Runtime 与十一产品 DLL 均与本次审计前当前 Release 输出逐字节匹配；Workshop subscription stress audit `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/20260719-1848-batch5-final/DTMAPI Workshop Audit 20260719-185249/Results/stress-summary.md` 为零 blockers。
- 但候选 `DTMAPI/Content/DTMAPI/release-manifest.json` 的 `BuildCommit` 是 `c93c460e5b7a`，而 Batch 5 实现仍在 dirty worktree。该 commit 不能重建当前 DLL。当前包可作为 byte-bound 测试候选，不能作为 commit-provenance 发布候选。
- Catalog 的 `releaseStop.state=Active` 仍阻止 `NewWorkshopUpload` 与 `ExistingWorkshopUpdate`；本审计不改变这一发布阻断。

## 四、新发现

### P1-1：正式 performance/no-demand 收据没有进入机器可复核的 Catalog 合同

`tools/release/dtmapi-product-catalog.json:105,128` 只用 `...FormalGcVerified` 枚举和 `currentDebt` 自然语言声明完成。`tools/scripts/check-product-catalog.ps1:1121-1220` 检查该枚举、普通 no-QA 与 timeout receipts，但不打开正式 no-demand、ActionSpeed 或 AutoFishing JSON。

`run-batch5-gc-ladder.ps1` 会 fail closed 于指标缺失、行为/身份/清理失败和 observer-effect budget，但没有 Mono/Unity/process/GC slope、L0 comparison 或 per-action/per-fish 数值预算。因此正确表述只能是：

> formal ladder execution、真实趋势、DTMAPI-owned 结构和生命周期门禁通过，测试窗口内未复现 Fatal；量化内存/GC 预算和运行时 per-unit allocation 尚未建立。

修订要求：Catalog 增加 machine-readable `performanceAcceptance`，分别绑定 no-demand result、ActionSpeed 24-child set/专用 aggregate、AutoFishing completed plan；checker 解析 terminal status、stage set、duration、samples、forced-GC、行为、metric availability、source/save/exit。若 0.5.5 还要声称量化预算，必须先冻结 L0/per-unit/trailing-window 阈值再评估；否则把 Catalog 状态改成结构/生命周期/趋势验证，不使用“内存预算通过”的语义。

### P1-2：正式 Batch 5 durable evidence 未被现有 allowlist 完整保留

本审计前的 `docs/debug/evidence-retention-allowlist.json` 能通过 `-Check`，但其 `batch5GcLadderRoots` 只有四个旧根、`candidate11Roots=0`，并遗漏正式 AutoFishing、Candidate11、Published11 disabled、`130406` timeout 和全部 no-demand 根。

根因是 `build-evidence-retention-allowlist.ps1:40,123` 只识别 Markdown 中带完整 `docs/debug/evidence/...` 前缀的 `BATCH5-GC-LADDER`、`CANDIDATE11`、`WORKSHOP-SUBSCRIPTION-AUDIT`；它没有 `BATCH5-NO-DEMAND` 类别，也不扫描 Catalog JSON。当前测试只证明正则能发现样例，不断言最终 Batch 5 exact set。

本 Review 使用完整路径，使现有生成器能够发现它支持的正式 GC/Candidate/GameSmoke 根；这不解决 `BATCH5-NO-DEMAND` 无类别、Catalog JSON 不参与或 exact-set 无断言的问题。修订要求：扩展 schema/生成器/cleanup consumer，新增 `batch5NoDemandRoots` 或更通用的 durable performance root 类型，并在 `test-runtime-evidence-retention.ps1` 精确断言本节终态集合，随后重新生成 allowlist。

### P1-3：最终候选的 commit provenance 不可复现

候选与当前编译输出 hash 一致，但 `BuildCommit=c93c460e5b7a` 只说明包和安装状态互相复述同一个旧 HEAD，不能证明该 commit 含有 Batch 5 源码。本审计开始、尚未添加本轮文档时已有 146 项 tracked/untracked worktree 变化。

修订要求：在用户决定的干净提交边界上重建候选并重跑 package/Catalog/provenance gate；或为开发候选增加独立 content-addressed source-tree manifest，并明确标记 `dirty/non-publishable`。不得上传当前候选。

### P1-4：Batch 6 G0 权威范围不完整，且与“立即实现 G2”冲突

Review 0012 的 G0 正确指出当前普遍 GameBridge 规则冲突，并在 `:165` 明确 G0 完成前不得实现 Advanced Runtime；但 `:304-312` 又把“设计和实现 G2”列为可立即开始。

G0 还必须覆盖或显式 scope/supersede 以下现行权威，而不只是 PROJECT、Agent、SDK、作者文档、Loader 和 UI：

- `AGENTS.md:55-60`；
- `PROJECT.md:13,63`；
- `docs/workflows/codex-api-rebuild.md:49-57,114-124`；
- Batch 5 Update `:51-55` 的当前五程序集/fragile GameBridge invariant。

修订要求：G0 前只允许设计、清单、消费者扫描、测试/回滚方案和文档对齐；G0 专门 Update 通过后才允许 G2 Runtime 实现。建立一处 Strict/Advanced/ContentPack/External 规范权威，其余文件引用该定义。

### P1-5：G2/G3 在当前文字下形成不可达循环

Review 0012 `:149-152` 规定产品迁移在 G0-G7 全部满足前阻断；G2 `:186-200` 的通过标准却要求“最小 Advanced 示例 + 一个真实产品”；该真实产品是 G3 AutoFishing `:202-215`。若逐字执行，G3 不能开始，G2 也永远不能完成。

修订后的门禁必须分层：

```text
G0 authority
  -> G1 inventory/budget + G2 minimal vertical fixture
  -> explicitly admit AutoFishing as the only product pilot
  -> G3 + G4 + relevant G5/G6
  -> G2 receives real-product proof
  -> G0-G7 all pass
  -> admit other product migrations
```

### P1-6：Batch 6 AutoFishing Ready 使用了错误的存档基线

Review 0012 `:336` 要求 AutoFishing “第三存档 smoke、长期 GC”；Batch 5 Update `:59,97` 与正式六阶段 receipts 都规定 AutoFishing native fishing/行为/GC 使用第五存档。第三存档属于普通 Local11、ActionSpeed 和 semantic transport 补充，不能替代第五存档 fishing baseline。

修订要求：Advanced 最小示例/普通加载可用第三存档；AutoFishing native fishing、行为和 GC 使用第五存档。改变 fixture 需另开 Review 并证明新的 native 状态可重复。

### P1-7：新增 current-build reverse 脚本允许把官方 bytes 写入可跟踪仓库路径

`tools/scripts/capture-doloctown-reverse-baseline.ps1:316-347` 的默认 `BuildRoot` 位于已忽略的 `references/doloc-town/reverse/`，但显式 `-BuildRoot` 接受任意绝对路径，随后会复制约 1.7 GB 官方游戏文件。若目标位于仓库内但不受 ignore 保护，会违反项目 source/distribution boundary。

修订要求：解析目标后，仓库内目标只能位于 `references/doloc-town/reverse/` 且必须通过 `git check-ignore`；拒绝 repo root、workspace 普通目录和 traversal/symlink escape。仓库外目标可保留，但要继续 fail closed 于已有目标/运行中的游戏。增加默认安全、允许 ignored root、拒绝 tracked repo root 三类测试。

### P2：权威文档与事实仍未同步

| 文件 | 当前残留 | 正确处理 |
| --- | --- | --- |
| `docs/api/public-api-matrix.md:106,108` | 仍把 `034102` 写成 current，并称 Published11 未接受、GC ladders pending | 改为 `214048`、`212635`、`212758` 和有界梯度事实；API 稳定级别保持不提升。 |
| 前次 Batch 5 Review `:154-156` | resolution checkpoint 仍停在 ladder/Published11 pending | 保留原 finding，追加 final resolution link，不重写历史观察。 |
| Batch 5 Update `:25-28` | pre-implementation 缺口以现在时出现 | 标注为“冻结的实施前基线”；不要与 `:102-110` corrective checkpoint 冲突。 |
| Batch 5 Update `:93,100` | 写 `031553 + 212913` 为 Catalog 保留项，实际 Catalog 是 `130406 + 212913` | 分开记录无 result 的 `031553` 和 result-bearing `130406`；Catalog/Update 保持同一集合。 |
| active smoke matrix | 没有 `130406` runtime-result row | 以 infrastructure/manual-handshake timeout 记录，不列为产品失败。 |
| 轻量路线图 `:112-127,239-245,282` | 仍把 QA 项目建立/首批迁移当未来工作，并说 Smoke/性能探针在 production GameBridge | 标注 Phase 1/Checkpoint A-C 已被 Batch 4/5 完成或 supersede；当前只剩 production QA seam 和产品 QA rehome。 |
| Review 0011/0012 行数快照 | 当前 Runtime/GameBridge 已比文档快照各增加约 90 行 | G1 使用带 HEAD、时间戳、包含/排除口径的可重复脚本，不把一次性文档数字当归属收据。 |

## 五、Batch 6 G0-G7 当前实态

| Gate | 当前实态 | 允许的下一步 |
| --- | --- | --- |
| G0 | `partial / blocked`：路线图已写纠正方向，强制权威仍冲突 | 先完成唯一身份规范和所有权威引用迁移；在此之前不实现 G2 Runtime。 |
| G1 | `partial / blocked`：有初步分类和 current-build reverse baseline，无全域消费者/native-owner/边际预算收据 | 建可重复清单与基线脚本；每域重新看 build `23762374` 方法体。 |
| G2 | `foundation-only / blocked`：已有通用 discovery/dependency/load/owner cleanup/restart 边界，无显式 Advanced lane | G0 后做 manifest/Core/SDK/Doctor/Manager/package/tests 原子 vertical slice。 |
| G3 | `design-ready / blocked`：AutoFishing native-owner 和文件去向有规划，仍通过 GameBridge `IFirstPartyFishingPrimitivesApi` | 只在 G2 最小 fixture 通过后准入唯一 pilot，并使用第五存档。 |
| G4 | `design-only / blocked`：只有归属表 | 把 mandatory/product lines、assemblies、Hook/updater/root/inactive cost 收据加入 Update 模板和 checker。 |
| G5 | `partial / blocked`：原则已写，未逐文件分类 | 先分类 Batch 5 通用底座、产品路由、QA 和 Compatibility；P2-3 债务在此处理。 |
| G6 | `partial / blocked`：已有 retained ABI 和部分消费者事实，无完整 0.5.5 scan/warning/removal 计划 | 输出 exact consumer set、warning version、removal/major-preview decision。 |
| G7 | `design-only / blocked`：已有 ContentPack 身份，无 `ContentPackFor`、owner helper 或可选 Content Host | 设计 ContentPack/Content Host 独立 vertical slice，不与 Advanced 产品 DLL 混为“全部下放”。 |

当前 Core 会对任意 `CodeMod` 直接 `Assembly.LoadFrom`；SDK 仅用源码文本 `SDK160` 阻止 BepInEx/Harmony/Assembly-CSharp/UnityEngine；Doctor 只识别 `CodeMod`/`ContentPack`。手工包可以绕过 SDK，却没有 Advanced 标签、游戏版本、Harmony owner、重启和玩家诊断语义。反过来，单点新增 `AdvancedCodeMod` 也会被 `ContentManifestRegistry.cs:315-319` 当成 ContentPack，SDK deployment 和 Doctor 会拒绝未知类型。因此 G2 必须是原子 vertical slice，不能只放宽 `SDK160` 或只增加一个 manifest 字符串。

## 六、可执行修订顺序

1. **先保护证据。** 扩 allowlist schema/生成器/test，加入正式 no-demand 类别和 exact Batch 5 root set；重新生成后再允许任何 evidence cleanup。
2. **把 performance 合同机器化。** Catalog 绑定三组终态 JSON；为 24 个 ActionSpeed child receipts 生成专用 aggregate 或 exact set；保持 allocation blocked/null 的真实语义。
3. **修正文档权威。** 同步 API matrix、旧 Review final resolution、Batch 5 Update baseline/timeout、smoke row 和路线图 QA 状态。
4. **建立可发布来源。** 在用户决定的提交边界上重建候选，使 `BuildCommit` 可复现 payload；重复 package hash、Catalog 和 Workshop audit。当前 release stop 不解除。
5. **只做 Batch 6 G0。** 建立 Strict/Advanced/ContentPack/External 单一规范，更新 AGENTS、PROJECT、API rebuild workflow、SDK/作者/Loader/Manager 术语，并明确 Batch 5 invariant 的历史/当前 scope。
6. **完成 G1 与 G2 最小 vertical slice。** 先清单/预算，再原子实现 manifest、loader、SDK、Doctor、Manager、package、Harmony owner、restart/failure diagnostics 和最小 fixture。
7. **准入唯一 AutoFishing pilot。** 使用 current build native-owner 审查与第五存档；完成 G3/G4/相关 G5/G6 后，才开放其他产品。
8. **独立推进 G7。** ContentPack/Content Host 不复制 Advanced CodeMod 的所有权模型；先验证 `ContentPackFor`、host version、owner helper、资源清理和 Manager 状态。

## 七、本次验证

- 完整读取项目必读、反馈审查、Review、Update、Debug、API、路线图与 Batch 6 相关记录。
- 逐项检查 Core/GameBridge/QA/first-party source、定向 tests、Catalog/checker、GC/no-demand runner、release candidate、allowlist builder/test 和 current-build reverse script。
- 独立解析最终 Local11/Candidate11/Published11/no-demand/ActionSpeed/AutoFishing/Workshop audit JSON 与 package DLL hash 边界。
- PowerShell 7 `tools/scripts/test.ps1 -Configuration Release`：退出 `0`，耗时 971.1 秒；Runtime/Tooling/产品 builds 零 warnings/errors，UnitTests、QaUnitTests、InstallDoctor、Player Doctor、Author SDK、双宿主 upgrade/install transactions、Candidate11 source transactions、Catalog、release contract、Batch 4 G9 semantic/meta-negative、Batch 5 GC/no-demand source/terminal validators、no-QA deadline、ABI、artifact 和 docs/evidence governance 均通过。该绿灯不反驳 P1-2：当前 allowlist contract 的测试口径本身没有覆盖正式 exact set。
- full suite 结束时报告一个受管 UnitTests session cleanup 待下次重试；不涉及游戏进程、正式证据删除或产品失败。
- 本次没有运行游戏。最后只读检查时 Runtime lock 为 `FREE`，`DolocTown.exe` 不存在。

## 八、最终决定

保留 Batch 5 “有界实现已验证”的事实，不回退已通过的源码和运行证据；同时阻断把当前 dirty-tree candidate 宣传为可复现 0.5.5 发布包，也阻断“量化内存/GC 预算已通过”的措辞。先完成证据保留、performance receipt 机器绑定、文档同步和 clean-source provenance，再做发布签署。

Batch 6 现在只能推进修订后的 Phase 0。G0 必须先于 G2 Runtime；G2 最小 vertical slice 之后仅准入 AutoFishing pilot；G0-G7 全部满足后才允许其他功能产品迁移。

## 九、2026-07-20 后续源码纠正

提交本 audit 所检查的 dirty checkpoint 后，独立 barrier/fault-path 复核发现“旧 P1 源码缺陷均 closed、无新 P1”漏掉三个相邻 authority/observer edge：ContentQuery 在 terminal receipt 前发布、CustomAnimals post-commit fault 可跳过 demand reconciliation、CustomAnimals rejection observer 可在 authority commit 前发布。根因和 acceptance gates 由 [Review 20260720-0002](20260720-0002-batch5-terminal-receipt-observer-atomicity.md) 接管；本 audit 的 performance、retention、provenance、Batch 6 gate 与 release-stop 结论不变。

## 十、Batch 6 Phase 0 收口回链

本 audit 要求的修订版 Phase 0 已由 [Update 20260720-0003](../../../updates/2026/20260720-0003-batch6-phase0-closure.md) 完成并机器化验证；G1 完成的是可复现 baseline/decision inventory，完整 ownership gate 对候选、延期和未决领域仍阻断。该回链不改变 0.5.5 release stop，也不把 G2 Runtime、AutoFishing 或任何真实产品迁移解释为已准入。
