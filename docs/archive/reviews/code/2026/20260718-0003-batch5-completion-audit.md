# 20260718-0003 Batch 5 Completion Audit

Status: recorded

Date: 2026-07-18

Reviewed branch/HEAD: `codex/major-update-batch0-20260713` at `c93c460e5b7a51063ad83351e692d2f46a0828ff`, plus the uncommitted Batch 5 working tree

Scope: independent audit of the implemented Batch 5 event kernel, demand activation, content invalidation, lifecycle/Hook boundaries, performance claims, tests, package/runtime receipts and documentation closure

Owning Update: [Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)

Source route: [SMAPI Version Capability And Batch 5 Route Review](20260718-0002-smapi-version-capability-batch5-route-review.md)

Manual feedback: [Batch 5 Completion Feedback Review](../../manual-qa/2026/20260718-0001-batch5-completion-feedback-review.md)

## Audit Boundary And Verdict

The reviewed worktree is not a small planning change. It contains 60 modified tracked files with 4,208 insertions and 638 deletions plus 17 untracked entries, including the event kernel, demand coordinator/catalog, content generations, GameBridge demand routes, manifest/config changes, unit/QA tests and a GC-ladder runner.

No P0 crash, data-loss, save-corruption or immediate-disable defect was found. Batch 5 nevertheless **is not complete and must remain `in-progress`**. The audit found five groups of P1 source defects plus failed/missing acceptance evidence. Offline unit coverage and five short staged-QA runs are meaningful positives, but they do not establish whole-runtime inactive silence, atomic content publication, the frozen event boundary, ordinary no-QA AnimalViewer behavior or the independent GC ladders.

The native-owner safety clause remains binding for every follow-up:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Findings

### P1-1: Empty runtime queues rebuild detailed diagnostics twice per frame

`DtmApiRuntime.Update` calls `FlushRuntimeQueues` at both `Update.Begin` and `Update.End` (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:615,627`). With the default Batch 5 flags enabled, `FlushRuntimeQueues` unconditionally calls both detailed publication functions even when the hook/event queues are empty (`:1084-1100`). Those functions build a `HookStatusQueueSnapshot`, an event boundary snapshot, policy dictionaries and formatted summary strings (`:1110-1136`; `HookStatusPublicationQueue.cs:85-106`; `EventManager.cs:391-418`).

This is a direct failure of the accepted diagnostics budget: status publication must be change-driven or bounded by a measured cadence, not rebuild detailed state twice on every player frame. The current 10,000-frame GameBridge test bypasses `DtmApiRuntime.Update`, so it cannot detect this cost.

Required correction: make the empty/no-change path scalar and allocation-free, publish detailed snapshots on change or a bounded diagnostic interval, and add a whole-runtime warmed 10,000-frame allocation/call test that includes Core `Update`.

### P1-2: Demand release does not make several retained Hook routes dormant

The strongest case is CustomAnimals. Once any CustomAnimals definition exists, `CustomAnimalAnimatorBridgeFeature.TryInstallHooks` installs the complete HookBridge (`CustomAnimalAnimatorBridgeFeature.cs:79-104`). Removing the final owner clears definitions and demand (`CustomAnimalAnimatorBridgeService.cs:281-359`) but does not remove the Hooks. Retained callbacks still enter the service on animal render, controller update, renderer fixed update and other native paths (`DolocTownHookCallbacks.cs:416-470`). In the zero-definition case, `ApplyPngSpriteOverrideContext` reflects `protoName` before discovering the registration table is empty (`CustomAnimalAnimatorBridgeService.cs:999-1012`), and the controller-update path reflects the animal before checking applicability (`:1293-1297`). This is not a proven stateless no-op and violates disabled/no-definition silence.

Related incomplete demand boundaries:

- the Camera Hook is classified `ProcessPinnedDormant`, but retained `SetEnvCamera` callbacks call `NotifyGameBridgeFeaturesEnvironmentReset`, which fans out `EnvironmentReset` across every Feature and publishes retention diagnostics after the last Camera lease (`DolocTownHookCallbacks.cs:188-190`; `DolocTownGameBridge.cs:302-309`);
- Equipment orphan recovery is catalogued as `save-load-until-terminal`, yet its mandatory process-lifetime updater remains active forever and merely takes an early return after the terminal check (`DolocTownGameBridge.Demand.cs:41,83-84`; `DolocTownExperimentalBridgeApi.EquipmentSlots.cs:850-863`);
- at least Zoom, DebugConsole, ActionSpeed, OneActionComplete, FishBreedingAssistant, AnimalHusbandryProgress and AutoFishing retain permanent `UpdateTicked` subscriptions for polling/evidence even when their active product session is absent. GameBridge demand becoming zero therefore does not close the product-to-event-producer chain;
- all thirteen Feature/Service roots are still eagerly constructed by `EnsureGameBridgeFeatures` (`DolocTownGameBridge.cs:223-229,360-421`), although the old updater traversal has been removed.

Required correction: put a demand/definition fast path at every retained callback before reflection/diagnostics, separate the Camera environment callback into its real base/mandatory owner or gate the fan-out, withdraw Equipment recovery demand at terminal, remove evidence-only product ticks, and prove the whole dependency closure rather than only coordinator counters.

### P1-3: Content generations publish live state before the generation transaction commits

CustomAnimals constructs candidates, then replaces the live registration/generation dictionaries at `CustomAnimalAnimatorBridgeService.cs:538-551`. Only afterward does it perform resource observations, warnings, logging and lifecycle publication (`:615-708`) and finally call `ContentRefreshGenerations.Complete` (`:709-711`). Any exception after the live swap but before `Complete` enters the catch and calls `AbandonAndRequeue` (`:714-721`).

Audio has the same shape: `ReloadContentPackOwner` changes each owner's live entries (`AudioReplacementService.cs:394-425`), then resource/log/lifecycle publication runs (`:428-440`), followed by generation completion (`:441-443`); a later exception requeues the already-published generation (`:445-452`).

This can report a rejected/abandoned generation while its state is already visible, and can repeatedly rebuild the same generation when a non-authoritative diagnostic sink fails. It violates candidate → atomic commit/reject and one-generation/one-terminal-result semantics.

Required correction: finish all fallible candidate preparation before publication; make the live-state swap and generation terminal receipt one authoritative commit boundary; isolate post-commit diagnostics so their failure cannot requeue committed state; add fault-injection tests at every post-swap step.

### P1-4: Event unsubscribe bypasses the runtime-thread mutation boundary and can alter an in-progress publication

Owner-bound event proxies call `ensureOwnerActive` on `+=`, but `-=` directly invokes slot removal (`EventManager.cs:1436-1495`). `EventSlot.Add` therefore enforces the owner/runtime rule (`:803-831`), while `Remove` does not (`:834-869`). This bypasses `DtmApiRuntime.EnsureModOwnerRegistrationAllowed`, whose contract says owner-bound platform resources may only be mutated on the Runtime thread (`DtmApiRuntime.cs:3401-3408`).

The race affects dispatch semantics too. Publication captures only a registration cutoff (`EventManager.cs:439-451`), while `DispatchCore` later reads the then-current `dispatchSnapshot` (`:1045-1057`). A worker-thread `-=` between those points can remove a handler from an event whose publication already began. Existing same-thread add/remove-during-handler tests do not cover this window.

Required correction: route event removal through the same runtime-thread/owner-active guard as addition, or explicitly queue/reject off-thread mutation; carry the frozen dispatch snapshot (or equivalent immutable membership token) from publication preparation; add a deterministic barrier-based race test.

### P1-5: Route and cleanup diagnostics are not authoritative enough for lifecycle acceptance

`CoreLifecycle` is catalogued as a mandatory physical Hook route, but it is never configured with a Hook install/probe delegate in `InitializeGameBridgeDemandRouting` (`GameBridgeDemandRoutes.cs:76`; `DolocTownGameBridge.Demand.cs:38-74`). Its mandatory demand is committed as lifecycle `Active` with patch state `NotInstalled` (`DolocTownGameBridge.Demand.cs:79-84,177-184`), while the actual base Hook set is installed and measured separately in `DolocTownGameBridge.Hooks.cs`. The resulting route state cannot prove physical base-Hook readiness.

Owner cleanup also under-reports. `GameBridgeModOwnerCleanupParticipant` removes demand roots into `demandRemoved` but does not add them to `RemovedResources` (`GameBridgeModOwnerCleanupParticipant.cs:13-16,40-44`). An owner with only demand roots is cleaned correctly in the coordinator but reported as removing zero resources.

Required correction: bind CoreLifecycle to an exact base-Hook readiness probe/route state instead of synthesizing `Active`, expand the core readiness set to the intended physical closure, and include removed demand roots in cleanup accounting. Tests must assert report truth, not only final dictionaries.

### P1-6: Current acceptance evidence is negative or missing

The two current ordinary no-QA receipts are failures:

- `docs/debug/evidence/GAME-SMOKE/20260718-190150/result.json` records `RunStatus=Failed`, `NoQaUiEvidence=Failed`, zero AnimalViewer render receipts and failed two-render/native-close gates;
- `docs/debug/evidence/GAME-SMOKE/20260718-192244/result.json` repeats those failures, despite passing save-load, no-fatal-window and clean process-exit checks.

No later finalized result exists. The screenshot reports two real-animal observations after the deadline, but also says the run must be repeated; those observations are not a replacement receipt.

No real Batch 5 GC ladder has been executed. `test-batch5-gc-ladder.ps1` runs `run-batch5-gc-ladder.ps1 -PlanOnly` and uses source-text assertions; the retained plans have zero completed stages and unavailable metrics. The executable AutoFishing route also does not currently distinguish the declared L4 disable-recovery and L5 title-cycle stages: `run-game-smoke.ps1` selects a native-control case only for L0 and the same positive AutoFishing case for every non-L0 level (`:3467-3468`), while the positive fixture always stops the product and performs title cleanup. L4/L5 are therefore metadata distinctions rather than independent behaviors.

Required correction: repair the runner semantics before spending the multi-hour runtime budget; then execute the independent ActionSpeed and AutoFishing ladders under the lock with available Mono/Unity/process/GC/owner/event/input/API/resource/Hook/demand metrics and terminal receipts.

### P1-7: The lifecycle Update and Catalog overclaim or omit current facts

Before this audit, Update `20260718-0003` still said the revision changed no Runtime behavior, listed only three documentation files, and reported runtime validation `not-run`. The actual worktree contains the implementation and seven current game runs. The monthly ledger repeated the planning-only state.

The product Catalog also says current-tree ordinary no-QA Y-console, AnimalViewer and EquipmentSlots behavior passed (`tools/release/dtmapi-product-catalog.json:106`), but the two latest current-tree ordinary no-QA receipts failed. `check-product-catalog.ps1` checks that sentence text exists rather than binding the statement to a passing receipt (`:1134`).

This audit corrects the owning Update/ledger to `in-progress` with partial runtime validation. It intentionally does not rewrite the Catalog claim during a review-only task; that remains a source/release-contract finding to fix against the eventual passing receipt.

### P2-1: The current 10,000-frame tests prove coordinator silence, not whole-runtime performance

`Batch5GameBridgeDemandTests.cs:83-102` exercises `WarmAndRunNoOptionalDemandFramesForTests` and checks optional updater/hook/status counters. The helper loops only `UpdateRuntimeAutomation` (`DolocTownGameBridge.Demand.cs:537-560`); it does not run Core `DtmApiRuntime.Update`, instrument file/directory/reflection/native calls, or assert allocated bytes. GameBridge tests also override Hook readiness/patch probes and use a fake game directory/host.

These are valid state-machine tests. They are not the G0/G7 whole-GameBridge 10,000-frame acceptance receipt and cannot establish physical Harmony/native behavior.

### P2-2 (resolved during the audit): The first rerun exposed test-artifact ordering, and the final serialized suite is green

The audit ran `tools/scripts/test.ps1` without touching the shared Runtime. Builds, `DTMAPI.UnitTests`, `DTMAPI.QaUnitTests`, InstallDoctor, Player Doctor, Catalog and several package/release gates passed. The first overall command exited `1` when `DTMAPI.AuthorSdk.Tests` could not find a deployed `manifest.json` in its fault-update matrix; an immediate isolated `DTMAPI.AuthorSdk.Tests` rerun exited `0`.

A second full invocation reached the evidence-retention gate and correctly rejected the derived allowlist made stale by this audit's new durable evidence references. After regenerating the allowlist, its source/governance checks passed. The final serialized `tools/scripts/test.ps1` invocation exited `0` in 889.3 seconds. This resolves the audit-time offline-suite observation and establishes a current-tree green baseline; any corrective source changes required by P1-1 through P1-5 must still repeat that suite on the final tree.

### P2-3: Residual dependency-closure and maintainability debt remains

- CustomAnimals installs its entire animator/AI/PNG/sleep Hook bundle for any definition instead of the minimum sub-capability closure.
- Eager Feature construction retains roots even though the new demand snapshot removed old updater traversal.
- `UpdateGameBridgeFeatures` and bucket scheduler code remains in `DolocTownGameBridge.Features.cs:67-89` but is no longer called by the Runtime update path, leaving a misleading dead/compatibility path.

These are not independent release blockers once the P1 inactive-cost and callback issues are fixed and measured, but they should be resolved or explicitly documented before calling the architecture fully demand-created rather than demand-dispatched.

## Confirmed Positive Boundaries

- Event zero-listener preparation exits before EventArgs construction; copy-on-write subscription snapshots, bounded FIFO/coalescing/reject policies, owner failure isolation and quarantine have real in-process tests.
- Demand source/lifetime, lifecycle/patch/restart dimensions, bounded receipts and owner cleanup exist. Passive `GetApi<T>`/facade lookup was not found to activate native work directly.
- Manifest `Required`/`IsRequired` conflict handling, inactive `UpdateKeys` diagnostics, `MinimumGameVersion` warning behavior and Config `File.Replace` preservation are implemented and tested.
- ContentQuery has immutable publication/last-good behavior; CustomAnimals/Audio clean-generation tests cover no recurring file signature/rebuild work in their direct service paths.
- ActionSpeed and ActionCompletion have materially narrower child demand closures; Camera, AnimalViewer, Audio, Fishing, Workshop, Machine, Movement, Creative, Equipment, QA and AuthorSession transitions are represented in the demand Catalog.
- Five short retained staged/base/product runs (`185615`, `185746`, `185900`, `185952`, `190055`) passed with process/fatal/QA cleanup gates.
- At the final read-only process check, no `DolocTown.exe` remained. The same-worktree runtime lock was deliberately not released because the user paused the interactive continuation.

## Validation Performed By This Audit

- Read project-required planning, debug, reference, review, governance, API, Hook and smoke records before auditing.
- Inspected the complete dirty-worktree inventory, targeted Core/GameBridge/QA/product source, unit tests, GC scripts, Catalog and current retained receipts.
- `git diff --check`: passed; only existing LF-to-CRLF normalization warnings were emitted.
- first `tools/scripts/test.ps1`: failed at the Author SDK deployment fault matrix after Runtime/Core/QA/Doctor/Catalog/package sub-gates passed; isolated `DTMAPI.AuthorSdk.Tests` then passed.
- second `tools/scripts/test.ps1`: reached the evidence-retention gate and rejected the derived allowlist made stale by the audit's new evidence references.
- regenerated `docs/debug/evidence-retention-allowlist.json`; allowlist validation, cleanup-fixture governance and documentation governance passed.
- final serialized `tools/scripts/test.ps1`: passed with exit code `0` in 889.3 seconds.
- No game was launched, driven or closed; no Runtime install/package deployment, save/config/profile mutation, lock release or Workshop action was performed.

## Revised Executable Closure Route

1. Fix P1-1 through P1-5 with focused fault/race/performance tests. Do not run long GC stages against known-invalid runner or inactive-cost semantics.
2. Remove permanent evidence ticks and add retained-Hook demand fast paths. Run a whole-runtime warmed 10,000-frame measurement including Core `Update`, retained native callbacks and mandatory routes.
3. Correct AutoFishing L4/L5 semantics and add behavioral tests, not only source `.Contains` assertions.
4. Preserve the current serialized full-suite green baseline; after corrective source changes, rerun it and persist commit/tree hash, command, exit and artifact receipt for the final tree.
5. After explicit continuation, inspect rollback/restoration first; rerun current-tree ordinary no-QA AnimalViewer to a passing two-render/native-close receipt, then replay staged QA and exact eleven-product enabled/disabled matrices.
6. Execute the independent ActionSpeed/AutoFishing GC ladders. Treat them as allocation/root evidence, not ISSUE-010/011 closure without the required long active-game/native-object evidence.
7. Build and audit the exact final five-DLL/no-QA package, correct Catalog claims from receipts, release the runtime lock from the owning continuation, and only then change the Update to `verified`.

## Decision

Keep Batch 5 open. The current checkpoint is best described as: **event/demand/content/lifecycle implementation is substantially present; Core/QA offline tests and short staged runs provide real positive evidence; end-to-end inactive silence, atomic generation publication, the event mutation boundary, route truth, ordinary no-QA AnimalViewer and independent GC acceptance remain unclosed.**

## Resolution Checkpoint

The corrective implementation and later validation remain owned by [Update 20260718-0003](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md). As of 2026-07-19, focused source/unit coverage closes this Review's P1-1 through P1-5 implementation requirements: change-driven empty diagnostics, zero-demand retained-callback fast paths, authoritative generation commit, same-thread frozen event membership, and physical CoreLifecycle/demand-root cleanup reporting. Current ordinary Local11/no-QA receipt `GAME-SMOKE/20260719-034102` closes the P1-6 AnimalViewer receipt gap with exactly two causal renders and native-close/overlay/order/restoration/exit gates. These later facts do not rewrite the audit-time findings.

Batch 5 still remains `in-progress`: `Batch 5 Final Worktree Candidate 20260719-040700` later passed its offline Runtime subscription audit with zero blockers, but the independent ActionSpeed and AutoFishing runtime GC ladders have no terminal evidence, the final full Release suite/Catalog synchronization remains pending, and Published11 is fail-closed at prelaunch because `GAME-SMOKE/20260719-010116` found five retained Steam product tree hashes drifted from the frozen baseline. Local11 or offline-package success does not substitute for that Published11 boundary.

### 2026-07-20 Subsequent Scope Correction

Later receipts closed the historical ladder/Published11 gaps described above, but an independent terminal-authority review found adjacent ContentQuery publication and CustomAnimals post-commit/rejection-observer gaps that this checkpoint did not test. [Review 20260720-0002](20260720-0002-batch5-terminal-receipt-observer-atomicity.md) supersedes only those generation/observer conclusions and feeds the same owning Batch 5 Update; it does not rewrite the audit-time evidence.
