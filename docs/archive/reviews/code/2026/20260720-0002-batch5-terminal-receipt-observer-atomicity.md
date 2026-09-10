# 20260720-0002：Batch 5 Terminal Receipt 与 Observer 原子性复核

## Metadata

- Review ID: `20260720-0002`
- Date: `2026-07-20`
- Lifecycle: `recorded`
- Scope: Batch 5 content-generation authority edge、post-commit demand 与 rejection observer
- Owning Update: [20260718-0003 Batch 5](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Trigger: 在提交已有 dirty checkpoint 前，对已称为 closed 的 generation 路径做独立源码复核；该复核发现最新 prerequisite audit 没有覆盖的相邻 authority/observer 缺口。

## Known Facts Before Another Fix

- `ContentRefreshGenerationService.CompleteWithAtomicCommit` 是 generation receipt 与 live snapshot 交换共用的权威边界；普通 `Complete` 不提供 prepare-only publication callback。
- 已有 Audio consumer 把 live publication 放在 atomic commit 内，并把 post-commit observer/demand 作为隔离的 best-effort hygiene；因此不需要更换 generation 模型。
- 已有 Batch 5 游戏证据验证的是当时二进制的有界行为。下面的源码修正会改变最终 DLL，旧证据不能自动升级为新 commit 的发布 provenance 或量化性能预算。

## P1-1：ContentQuery 在 terminal receipt 前发布 candidate

### Code Facts

- `WorkshopContentInputUi.cs` 的 `ContentQueryService.RebuildCandidate` 构建 candidate 后立即 `Volatile.Write(ref publication, candidate)`，随后才返回 success result。
- `DtmApiRuntime.RefreshContentQueryIfDirty` 在调用该方法后使用普通 `ContentRefreshGenerations.Complete(...)`，且不检查 stale `false` 结果。
- 因此 barrier 观察者可以在 receipt 仍未 terminal 时看到新 generation；stale completion 也不能撤销已发布 snapshot。

### Root Cause

候选构建与可见发布被同一个“rebuild”方法合并，调用者只把 receipt 当成后续记账，而没有让 receipt authority edge 拥有唯一的 publication callback。

### Rejected / Unproven Hypotheses

- “ContentQuery 没有 GameBridge demand，所以先发布无害”被否定：这里保护的是 generation receipt 与 live state 的一致性，不依赖 demand。
- “当前单线程 update 不会 stale”不能作为合同；服务已经显式提供 generation/stale 语义和测试 barrier。
- 尚无证据要求改变 query DTO 或公共 API；最小修正应只移动 publication authority。

### Acceptance Gate

- candidate preparation 不改变 `CurrentPublication`。
- barrier 前看不到新 generation；只有 `CompleteWithAtomicCommit` 成功回调能交换 snapshot。
- stale completion 不发布；pre-commit fault requeue 且保留 last-good；post-commit observer fault 不 requeue/回滚。
- invalid-root rejection 仍产生 terminal rejected receipt并保留 last-good；owner cleanup 后 snapshot generation 与 receipt 一致。

## P1-2：CustomAnimals post-commit fault 可跳过 demand reconciliation

### Code Facts

- CustomAnimals 已在 `CompleteWithAtomicCommit` callback 内交换 registrations、owner generation 和 callback demand mask。
- 交换成功后，`postCommitFaultForTest` 位于 `ReconcileOwnerDefinitionDemand` 之前。异常会进入 catch；因为 `generationCompleted=true`，不会 requeue，但也不会执行 owner demand 重算。
- 现有 fault 测试使用 active-to-active 替换，前后 demand 均为 `1`，不能发现 zero-to-one 或 one-to-zero 的陈旧 demand。

### Root Cause

terminal state 已正确提交，但派生 demand 被放在可能抛错的 post-commit observer 序列之后，且没有不可跳过的 per-owner `finally` reconciliation。

### Rejected / Unproven Hypotheses

- “既然 callback demand mask 已重建，route demand 一定正确”被否定：owner definition demand 是独立的 RuntimeDemandCoordinator root。
- “吞掉测试 fault”不是修正；既有合同允许 post-commit observer fault 向调用方传播，只禁止它回滚或重新排队已提交 generation。

### Acceptance Gate

- zero-to-definition 在 post-commit fault 后仍有 committed live definition、terminal receipt、无 requeue，route/owner demand 均为 `1`。
- definition-to-removed 在同类 fault 后 live definition/generation 已移除、无 requeue，route/owner demand 均为 `0`。
- 每个 owner 的 demand reconciliation 独立隔离；一个 owner 的 observer failure 不跳过其余 owner。

## P1-3：CustomAnimals rejection observer 在 authority commit 前发布

### Code Facts

- missing schema 和 invalid candidate 分支在构建 `Rejected` completion 时立即调用 `RecordOwnerGenerationRejected`。
- 该方法立即写 Diagnostics、Runtime log 与 lifecycle event；之后才尝试 `CompleteWithAtomicCommit`。
- stale completion 或 pre-commit fault 因而可留下没有 terminal receipt 的“rejected”观察副作用；observer 自身抛错还会让候选进入 abandon/requeue。

### Root Cause

rejection observation 被当作 candidate validation 的一部分，而不是 terminal receipt 成功后的 best-effort publication。

### Rejected / Unproven Hypotheses

- “rejection 不是 live state，所以可以提前记录”被否定：日志/lifecycle 明确宣称一个 owner generation 已被拒绝，属于 terminal outcome observer。
- 不需要把 diagnostics 写进 atomic callback；这样会让 observer failure 阻止权威 receipt。正确边界是先提交 receipt，再隔离发布 staged rejection records。

### Acceptance Gate

- invalid/missing candidate 在 stale 或 pre-commit fault 时不发布 rejection diagnostics/log/lifecycle。
- successor 成功 terminal reject 后每个 staged rejection 正好发布一次；observer failure 不 requeue terminal generation。

## Implementation Boundary

- 最小源码边界：`WorkshopContentInputUi.cs`、`DtmApiRuntime.cs`、`CustomAnimalAnimatorBridgeService.cs` 和现有 Batch 5 定向测试。
- 不改变 manifest kind、Author SDK `SDK160`、Advanced CodeMod 通道、AutoFishing 产品归属或其他 Batch 6 G2 内容。
- 完成后由 owning Update 记录 changed files、自动验证与 release blocker；本 Review 只拥有上述根因、被否定假设和 acceptance gates。

## Resolution

上述三个 P1 authority/observer edge 已按 acceptance gates 修正；实现文件、定向/全量验证、第三存档位 smoke 与仍然有效的发布阻断统一由 [Batch 5 owning Update](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md#2026-07-20-terminal-authority-and-evidence-closure) 记录。本 Review 保持 `recorded`，不接管实现生命周期叙述。
