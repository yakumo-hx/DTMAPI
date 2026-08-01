# 20260718-0001 Batch 5 Completion Feedback Review

## Review Header

- Status: recorded
- Time: 2026-07-18
- Source: user screenshot and the follow-up request to audit completed Batch 5 work
- Scope: review and evidence reconciliation only; no Runtime installation, game launch, input automation, save/config/package mutation, Workshop operation, or runtime-lock release
- Related implementation lifecycle: [Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Detailed source/evidence audit: [Batch 5 Completion Audit](../../code/2026/20260718-0003-batch5-completion-audit.md)

## Issue Review

### Issue 1: Computer Use was stopped with physical Escape and the round must pause

Original feedback, transcribed from the screenshot:

> 你已通过实体 Escape 停止 Computer Use，本轮必须暂停。

Review record:

- User-confirmed boundary: the interactive Computer Use run was explicitly stopped and must not be resumed implicitly.
- Codex action: this audit did not invoke Computer Use, send game input, launch or close Doloc Town, continue the smoke runner, release the runtime lock, or mutate the shared game/user Runtime.
- Read-only observation: at the beginning of the audit `DolocTown.exe` was still present and responding. A later check found no remaining `DolocTown.exe`, but the same-worktree runtime lock remained held with reason `Batch 5 final third-save staged/no-QA/product matrix`.
- Interpretation: process absence does not itself prove the requested normal-exit/restart receipt. The retained lock and lack of a new finalized `GAME-SMOKE` result keep the interrupted run outside accepted evidence.

### Issue 2: Code, full tests, package and main runtime matrix were reported passed, but ordinary no-QA AnimalViewer still needed a rerun

Original feedback, transcribed from the screenshot:

> Batch 5 代码、全量测试、发布包及主要运行矩阵已通过；普通无 QA 测试已取得两条真实动物回执，但超过本轮截止时间，尚需重跑。当前游戏可能仍在运行，运行时锁未强制释放。

Review record:

- The implementation is substantial: the reviewed dirty worktree contains 60 modified tracked files with 4,208 insertions and 638 deletions, plus 17 untracked source/test/document entries. Event, demand, content-generation, lifecycle, GameBridge routing, QA and GC-ladder code is present.
- Positive retained runtime evidence exists for five short staged-QA/base/product runs: `20260718-185615`, `185746`, `185900`, `185952`, and `190055` all have `RunStatus=Passed`.
- The two finalized current ordinary no-QA runs are negative evidence:
  - `GAME-SMOKE/20260718-190150/result.json` has `RunStatus=Failed`, `NoQaUiEvidence=Failed`, `NoQaAnimalViewerTwoCausalRenders=Failed`, native-close failures, and `NoQaAnimalViewerRenderReceiptCount=0`.
  - `GAME-SMOKE/20260718-192244/result.json` again has `RunStatus=Failed`, zero AnimalViewer render receipts, and failed native-close/overlay-clear/close-order gates, although save load, process exit and no-fatal-window checks passed.
- The screenshot's two observed real-animal receipts were not converted into a finalized passing run before the timeout. They are useful reproduction context, but they do not supersede the two durable failed receipts.
- The first independent `tools/scripts/test.ps1` invocation built all reviewed projects and passed the Runtime/Core/QA/Doctor/Catalog/package sub-gates, but exited `1` in the Author SDK deployment fault matrix because an expected installed `manifest.json` was absent; the same Author SDK test passed immediately in isolation. A second full invocation reached the evidence-retention gate and correctly rejected the newly stale derived allowlist after this audit added durable evidence references. The allowlist was regenerated and its governance checks passed. A final serialized `tools/scripts/test.ps1` invocation then exited `0` in 889.3 seconds. The current offline full-suite gate is therefore green; this does not substitute for the failed ordinary no-QA game receipts or the missing real GC ladders.
- The independent ActionSpeed/AutoFishing GC ladder has not run. Existing `batch5-plan-*` artifacts are plan-only with zero completed runtime stages and unavailable metrics.
- Conclusion: “substantial implementation and a current serialized offline full-suite pass” is supported. “Batch 5 complete” and “full current acceptance passed” are not supported yet.

### Issue 3: After normal exit, replying “continue” would authorize rollback verification, residual runtime validation, GC ladders and final documentation

Original feedback, transcribed from the screenshot:

> 请正常退出游戏后回复“继续”，我会从回滚校验开始完成剩余运行验证、GC 梯度和最终文档闭环。

Review record:

- The user did not issue the requested `继续` instruction in the current request; the request is an audit of completed work.
- This audit therefore stops at read-only source/evidence review and documentation reconciliation. It does not treat the later process disappearance as authority to resume the interrupted implementation run.
- Required continuation sequence remains:
  1. preserve and inspect the interrupted-run rollback/restoration state;
  2. produce one new current-tree ordinary no-QA receipt with two causal AnimalViewer renders, native close, overlay clear, close order, clean process exit and restored profile/config/save state;
  3. correct the source blockers in the detailed audit and rerun offline/current-tree gates serially;
  4. validate the corrected independent GC ladder semantics, then execute its real runtime stages under the shared lock;
  5. rebuild and audit the exact final package and synchronize the lifecycle Update/Catalog only from passing receipts.

## Cross-Issue Summary

- The physical pause boundary was honored.
- The implementation has real positive offline and staged-QA evidence, but its current ordinary no-QA acceptance is failed, not pending-success.
- Process exit was later observed, but no replacement result receipt appeared and the runtime lock was intentionally left untouched.
- No new game/runtime action is authorized by this audit record. A later explicit continuation may resume from rollback verification rather than assuming the interrupted run passed.

## Implementation Record Decision

- Create/update implementation Update: yes; the existing Batch 5 Update is the lifecycle owner and is revised to record the actual implementation/audit state while remaining `in-progress`.
- Create a Debug/smoke completion claim: no. The two current ordinary no-QA results are failures and no new runtime run was performed by this audit.
- Completion standard: no open P1 source blockers; a new serialized full-suite pass on the corrected final tree; current-tree ordinary no-QA and staged/product matrices pass; real GC ladders complete with available metrics; exact final package/audit passes; normal exit leaves no game process; runtime lock is released by the owning continuation; Update and Catalog match receipts.
