# 20260719-0007 - Batch 5 AutoFishing Fifth-Save And Energy-Budget Correction

- Date: 2026-07-19
- Status: recorded; formal implementation and replay are in-progress, not passed
- Severity: P1 formal-acceptance route blocker; no new crash or save-corruption claim
- Source: user correction after the third-save transport smoke and follow-up code audit
- Scope: Batch 5 AutoFishing L0-L5 save selection, long-stage energy/spirit budget, selected-rod readiness, and evidence classification
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Corrects: [20260719-0006 Batch 5 AutoFishing Native Control No-Water Spawn](20260719-0006-batch5-autofishing-native-control-no-water-spawn.md)
- Prior fifth-save authority: [20260613-0005 AutoFishing Native Loop Fifth-Save Review](../../manual-qa/2026/20260613-0005-autofishing-native-loop-fifth-save-review.md)
- Retained exploratory evidence: `docs/debug/evidence/GAME-SMOKE/20260719-105158`

This Review records a user correction. It does not prove that the runner, QA fixture, energy budget, or formal GC ladder has been corrected. The formal implementation and fifth-save L0-L5 replay remain in progress.

## 问题 1：正式 AutoFishing 应使用第五存档

原始反馈：

> 正式 AutoFishing 应用第五存档；第三档码头传送是错误 workaround，应撤销或停用。

- 图片转写：本项没有新增截图；这是用户对正式夹具所有权的直接纠正。

审查记录：

- 用户确认事实：正式 AutoFishing 夹具是第五存档，不是第三存档。第五存档进入后角色位于真实池塘前，既有用法是直接启用 AutoFishing 并走原生钓鱼循环。
- 截图/日志观察：无新增截图。历史第五存档 Review 已记录第五档真实池塘、真实鱼竿和原生循环；本轮 `105158` 则明确是 `SaveSlot=3`。
- 代码/文档事实：
  - `docs/reviews/manual-qa/2026/20260613-0005-autofishing-native-loop-fifth-save-review.md` 已把第五存档定义为替代合成夹具的正式 AutoFishing 夹具，并要求显式 `-SaveSlot 5`。
  - `tools/scripts/run-game-smoke.ps1` 的普通 AutoFishing phase 路线已要求显式第五存档。
  - 当前 Batch 5 GC 路线却在 `tools/scripts/run-batch5-gc-ladder.ps1`、`tools/scripts/run-game-smoke.ps1`、`src/DTMAPI.GameBridge.DolocTown.QA/QaHostSettings.cs` 和 AutoFishing L5 reload 中把 AutoFishing 锁到第三档。
  - 0006 根据一次第三档 `no-water` 失败推导出“把第三档正式传送到码头”的修复路线；该推导忽略了既有第五档夹具事实。
- Codex 推断：第三档 `no-water` 能解释该次失败，但不能改变正式 AutoFishing 夹具的身份。把第三档送往码头只能验证传送和异地钓鱼语义，不能替代第五档 GC 验收。
- 反证/未证实：尚无完成后的源测试或正式第五档 L0-L5 回放，因此不能宣称第三档硬编码、码头传送或 L5 reload 已被纠正。
- 归属：Batch 5 runner 的 stage/save-slot 规划、可选 QA host 设置验证、AutoFishing L5 reload，以及 AutoFishing 正式夹具选择。
- 需要更新：实现发生时由既有 Batch 5 Update 记录 changed files 和验证；本 Review 只拥有根因与验收边界。
- 验收点：
  - ActionSpeed 继续绑定第三存档；AutoFishing L0-L5 绑定第五存档；组合计划按 domain/stage 保存各自的 SaveSlot，不能共享一个全局第三档值。
  - AutoFishing L5 返回标题后重载同一第五存档。
  - 正式 AutoFishing GC 路线不请求 `mark:下船点-左`，不依赖第三档出生点，也不改变第三档 AnimalViewer/Zoom 夹具。
- blocker 判定：任何 AutoFishing stage 仍绑定第三档、仍自动传送码头，或 L5 仍重载第三档，都阻止正式 GC 验收。

## 问题 2：单阶段长测必须有体力与精力预算

原始反馈：

> 单阶段长测有体力/精力预算，不能只把角色放到水边就开始 600 秒与多条鱼的正式测量。

- 图片转写：本项没有新增截图。

审查记录：

- 用户确认事实：正式长测需要显式处理单阶段的体力与精力预算。
- 截图/日志观察：现有失败证据没有形成正式的阶段起始/结束体力与精力回执，也没有证明五条 warmup 加十条 measured fish 能在一个存档会话内持续完成。
- 代码/文档事实：
  - 当前正式定义是 600 秒测量、30 秒采样、五条 warmup fish 与十条 measured fish；L4 还需要一个独立的 disable/recovery 原生单位。
  - 当前公开构建的 `BodyController.UseFishRod(ItemFishingRod)` 在进入 `AgentStateFishingReady` 前调用 `DolocAPI.HasEnoughEnergy(DolocAPI.GlobalParameter.FishingEnergyCost)`；体力不足时只显示错误与表情，不进入 Ready。
  - `AgentStateFishingWait.NextState()` 在原生收竿输入分支调用 `DolocAPI.CostEnergy(FishingEnergyCost)`，所以每个真实收竿单位会消耗原生 fishing energy。
  - 原生命令 `compose_energy` 位于 `references/doloc-town/reverse/builds/23762374_public_C416D4/decompiled/Assembly-CSharp/DolocAPI.cs`：`Command_ComposeEnergy(int value)` 调用 `ChangeEnergy(value)`。
  - 原生命令 `compose_spirit` 位于同一文件：`Command_ComposeSpirit(int value)` 调用 `ChangeSpirit(value)`。
  - 原生查询 `get_energy_percent` 位于 `references/doloc-town/reverse/builds/23762374_public_C416D4/decompiled/Assembly-CSharp/DolocTown/FunctionDefines.cs`，返回 `archiveHandle.CurrentEnergyPercent`。
  - 原生查询 `get_spirit_percent` 位于同一文件，返回 `archiveHandle.CurrentSpiritPercent`。
  - 已审查的钓鱼责任路径直接消费 energy；尚未发现同一钓鱼循环直接消费 spirit。用户要求仍把两者作为长阶段的受保护、可读回状态，不能把“未观察到钓鱼 spirit 消耗”写成无需检查。
- Codex 推断：仅有位置、FishingPool、NormalGameState 和选中鱼竿不足以证明长阶段可完成。若 energy 在 warmup 或 measurement 中耗尽，`UseFishRod` 的原生早退可能被现有反射调用错误记录成已调用，并造成重复重试或外层超时。
- 反证/未证实：本 Review 不决定最终预算算法、补充值或阈值；也不证明当前第五档的实时 energy/spirit 足够。`compose_energy`/`compose_spirit` 的存在是原生命令事实，不是当前 QA 已使用或已通过的事实。
- 归属：QA stage preflight、原生能量读取/补充责任、stage 级世界状态事务和外层存档字节回滚。
- 需要更新：实现时应在既有 Batch 5 Update 中记录采用的阈值、补充策略、read-back receipt、回滚方式和正式证据。
- 验收点：
  - 每个 AutoFishing stage 在启用 session 前读取并记录 `get_energy_percent` 与 `get_spirit_percent`。
  - 若预算不足，使用经过范围约束的原生命令路径或明确失败；任何补充都必须 read-back 验证、只影响本次 QA 会话，并由外层第五档 save triple 字节级恢复门保护。
  - 阶段结果区分 `insufficient-energy`、`insufficient-spirit`、未选中鱼竿、无水与原生进度失败，不能把原生早退统计为真实 cast/fish。
  - 600 秒与目标鱼数全部完成后，结果保留起始/补充/结束百分比及原生命令来源。
- blocker 判定：没有可审计的单阶段 energy/spirit 预算与回滚回执时，正式长测仍为 in-progress，不能声明通过。

## 问题 3：第三档未必选中快捷栏第四格鱼竿

原始反馈：

> 第三档未必选中快捷栏第四格鱼竿；不能把“鱼竿可能在快捷栏里”当作当前选中鱼竿。

- 图片转写：本项没有新增截图。

审查记录：

- 用户确认事实：第三档的当前选中项不稳定，快捷栏第四格存在鱼竿也不等于当前已选中该鱼竿。
- 截图/日志观察：`GAME-SMOKE/20260719-105158/DTMAPI-latest.log` 在完成码头传送后先连续记录 `status=no-selected-rod`，随后才出现 `applied=True; status=cast`。证据没有说明选中状态为何后来改变，不能补写成用户输入、QA 自动选择或加载延迟中的任一种。
- 代码/文档事实：
  - `FishingNativeStateCache` 只在 `DolocAPI.SelectedItem` 是 `ItemFishingRod` 时保留 `SelectedRod`。
  - `FishingNativeAdapter.TryCast` 在 `SelectedRod == null` 时返回 `no-selected-rod`。
  - 既有第五档 Review 记录过第五档早期因选中 `ItemTool` 而正确失败，后来在存档中选中 `carbon_fishrod` 后才通过真实循环；因此第五档也必须运行时 read-back，不能只依赖历史描述。
- Codex 推断：0006 把“selected rod 不是主因”写得过强。`101708` 的主失败确实是 `no-water`，但后续 `105158` 已证明第三档传送成功后仍可能先被当前选中项阻塞。
- 反证/未证实：没有证据证明第三档始终未选中鱼竿，也没有证据证明快捷栏第四格内容或当前 selected index 在所有冷启动中相同。
- 归属：正式第五档 fixture preflight、selected-item native read-back 和清晰的失败分类；不是放宽 `ItemFishingRod` 类型检查的理由。
- 需要更新：实现时更新既有 Batch 5 Update 和对应测试；不把本条复制成 API 稳定性声明。
- 验收点：正式第五档每个 stage 在 session acquire/enable 前确认当前 selected item 是真实 `ItemFishingRod`，并把 item identity 写入回执；否则立即 fail closed，不能靠无限重试等待选择变化。
- blocker 判定：只检查“快捷栏第四格存在鱼竿”或只等待 cast 最终成功，都不能满足正式 fixture readiness。

## 问题 4：保留 105158 为传送语义证据，但不得算正式 GC

原始反馈：

> 保留 105158 为“语义传送点解析 + 实际传送”证据，但它不是正式 GC 结果。

- 图片转写：本项没有新增截图。

审查记录：

- 用户确认事实：`GAME-SMOKE/20260719-105158` 不删除、不降为无意义尝试；它拥有明确但有限的传送语义证明范围。
- 截图/日志观察：
  - 日志记录 whitelist destination `mark:下船点-左` 被解析为 `下船点-左`，从 `farm_type1-平地` 请求到 `city_多洛可码头`。
  - 随后 read-back 为 `room=city_多洛可码头; position=117,10.49; fishingPool=true; normalState=true`，证明了实际房间转换和目标场景 pool 可见性，而不是只解析字符串。
  - 同一运行后来产生真实 cast、visible reel 与 PullExited 循环，但它仍从第三档启动，且前段出现 `no-selected-rod`。
  - `result.json` 是 `RunStatus=Failed`、`QaHostLifecycle=Failed`、`OwnerLifetimeCloseCleanup=Failed`；它没有形成完整 600 秒、十条 measured fish、全部 GC 指标与 level-specific terminal receipt。
  - `PlayerSaveRestored=Passed`、无残留进程与无 fatal window 只证明该次运行的恢复/退出子门，不把失败运行提升为正式 GC。
- 代码/文档事实：传送路径复用 `ITeleportDebugApi -> DolocAPI.DoTransport`，未创建 FishingPool，也未暴露任意坐标公共 API；这些事实可以继续支持 debug/QA 传送语义。
- Codex 推断：`105158` 的正确分类是“语义传送点解析 + 实际传送 + 后续原生钓鱼探索证据”。它不能回答正式 AutoFishing 为何没有使用第五档，也不能替代第五档的 energy/spirit、selected rod 和 GC 梯度验收。
- 反证/未证实：没有正式第五档 L0-L5 完成证据；也没有理由因正式路线撤销码头 workaround 而删除 `105158`。
- 归属：历史 debug/QA transport evidence classification；正式 GC 归属新的第五档 stage receipts。
- 需要更新：不修改该 evidence root；实现与正式回放只在既有 Batch 5 Update 中追加新 evidence root 和准确分类。
- 验收点：后续文档引用 `105158` 时必须附带“非正式 GC、第三档、整体失败”的限定；正式通过只能来自新的第五档 L0-L5 独立结果。
- blocker 判定：把 `105158` 写成 AutoFishing 正式 GC pass、正式第三档替代方案或第五档等价证据，均属于过度声明。

## Corrected Root Cause

0006 对 `101708` 的直接根因判断仍然成立：第三档农场场景没有 active `FishingPool`，所以该次 native-control cast 以 `no-water` 失败。错误发生在后续修复推论：它把“让这次第三档尝试变得可钓”误写成了“正式 AutoFishing 应如何选夹具”。既有用户约束早已把第五档定义为正式池塘/鱼竿夹具。

本轮还补齐两个独立的正式验收前置条件：长阶段必须建立 energy/spirit 预算与回滚回执；selected rod 必须读取当前原生选择，不能从快捷栏位置或历史存档描述推断。

## Implementation Boundary

1. 保留 ActionSpeed 第三档路线；将 AutoFishing L0-L5 和 L5 reload 改回第五档。组合梯度按 stage/domain 记录 SaveSlot。
2. 从正式 AutoFishing GC 路线撤销或禁用第三档码头自动传送；不删除 `105158` 的传送语义证据。
3. 在 session acquire/enable 前建立 current room/pool、selected `ItemFishingRod`、`get_energy_percent` 和 `get_spirit_percent` 的同阶段 readiness receipt。
4. 若采用 `compose_energy` 或 `compose_spirit`，必须使用原生命令责任、范围约束、read-back 验证和第五档 save triple 字节回滚；不得形成玩家持久状态。
5. 保留原生 `HasEnoughEnergy`、`CostEnergy`、`UseFishRod`、visible reel 和 PullExited 语义，不制造成功回执。
6. 实现和正式回放继续标记为 in-progress，直到全部 source/unit、恢复和第五档 L0-L5 证据通过。

## Formal Acceptance Gate

- Source/unit：runner、smoke validation、QA settings、L5 reload 和结果 identity 对 ActionSpeed=slot 3、AutoFishing=slot 5 fail closed；正式 AutoFishing 路径无 dock transport；selected rod 与 energy/spirit readiness 有确定性测试。
- Runtime：六个 AutoFishing levels 使用独立冷启动第五档，完成同一定义的 600/30/10/5 工作负载及各自 behavior receipt；L0 每个 measured fish 有 native cast 与 visible-reel acceptance，L4 有同周期 recovery，L5 有第五档 title reload/re-enable/disable/final cleanup。
- State safety：每个 stage 的第五档 `data`/`prev`/`bak`、OfficialLocal product tree、Author source state、profile/config 均由独立回执恢复；没有残留 `DolocTown.exe`、fatal window 或 Runtime lock。
- Evidence classification：`105158` 继续保留为传送语义证据，但不得列入六阶段正式 GC pass 集合。

在这些门全部通过前，Batch 5 AutoFishing 正式梯度状态是 in-progress；不得宣称已修复、已回放或已通过。
