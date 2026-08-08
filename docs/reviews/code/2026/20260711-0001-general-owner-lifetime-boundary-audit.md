# 20260711-0001 General Owner Lifetime Boundary Audit

Status: recorded
Date: 2026-07-11
Scope: docs-only code review of Core-owned Mod entry, registry, event, input, config-page, cleanup, and diagnostics lifetimes
Related Update: `docs/updates/2026/20260711-0009-general-owner-lifetime-boundary-audit.md`
Related issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` remains open; this review does not reclassify it

## Source Request

Review the common DTMAPI boundary for:

- atomic `DtmMod.Entry` success, failure, and partial initialization;
- `IModRegistry.RegisterApi/GetApi` owner binding, duplicate registration, and cleanup;
- one owner lifetime across Event, Input, ConfigPage, and API registration;
- no remaining platform roots after load failure, dependency failure, disable, or runtime cleanup;
- no title/save cleanup of Mod services that should remain process-lifetime;
- bounded-count diagnostics that do not create extra long-lived object graphs for ordinary mods.

This is review-only. No runtime implementation, public API shape, game files, Workshop files, or native Hook targets changed.

## Required Context And Current Baseline

The review read the required project/planning/debug/reference documents, document governance, API rebuild workflow, public API matrix, current API/native-owner indexes, relevant 2026-06-07 API audits, recent owner/lifecycle reviews and Updates, the active ISSUE-010 record, and the current source/tests.

Current positive baseline:

- helper `IModRegistry.RegisterApi<T>` is owner-bound through `OwnerBoundModRegistry`; the unbound registry rejects direct public registration;
- Entry exceptions run owner cleanup for Events, registry/APIs, Input, ConfigPage, CustomEntities, GameBridge cleanup participants, and resource-lifecycle records;
- ReturnedToTitle clears Input transient state and save/runtime instances, but does not remove process-lifetime event subscriptions, API objects, config pages, or loaded Mod instances;
- persistent Input registrations are owner-indexed, while ordinary local snapshot watches expire by generation;
- the owner ledger, preview aggregates, recent preview failures, resource timeline, and retained error/warning windows have explicit caps;
- Release source/unit validation currently passes.

These facts are useful but do not close the common lifetime contract. The findings below are ordered by risk.

## Findings

### P1 - A late load-finalization exception can leave a failed Mod in `modInstances` and `loadedMods`

Code path:

- `LoadCodeMod` writes `modInstances[owner]`, appends `loadedMods`, and adds the registry row before recording/committing the transaction and logging completion: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:2632-2638`.
- the catch path calls `CleanupFailedCodeModOwner`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:2641-2652`.
- that cleanup removes Event/API/Input/ConfigPage/CustomEntity/GameBridge state, but does not remove `modInstances` or `loadedMods`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:2656-2723`.
- `FileMonitor.Log` performs filesystem writes and host logging without swallowing IO/host exceptions: `src/DTMAPI.Core/Logging/FileMonitor.cs:24-39`.

Impact:

If transaction publication or the final `monitor.Log("Mod Entry completed.")` throws after the two runtime collections were mutated, `LoadCodeMod` returns false but `IsLoadedMod` can remain true and the `DtmMod` instance remains a process root. The owner registry may simultaneously have been removed, producing a split state: failed status plus loaded list/instance root.

The existing partial-Entry test throws from inside Entry before those final collection writes. It does not cover this late-finalization window.

Required correction boundary:

- make one explicit load state machine (`Created -> Entering -> Committed` or failed);
- stage the instance locally and publish all loaded collections only at one non-throwing commit point;
- make rollback idempotently remove `modInstances`, `loadedMods`, loaded registry state, and every owner participant regardless of the failure point;
- keep completion logging outside the correctness-critical transaction, or isolate logging failure from load state.

Acceptance:

- injected failures before Entry, during partial Entry, after Entry, during commit publication, and during completion logging all end with no loaded list row, instance root, event/input/config/API/GameBridge owner state, or active transaction;
- the success case publishes all surfaces exactly once.

### P1 - Same-process official disable does not run owner unload cleanup

Code path:

- on Workshop/official refresh, an already loaded but now disabled Mod only logs that a restart is required, then continues: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:2513-2524`.
- `CleanupModOwnerParticipants` is called for Entry failure and runtime shutdown, not for that disable branch: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:176-205`, `:1275-1294`, `:2656-2723`.
- the cleanup reason enum already contains `Unload`, but no general runtime unload path consumes it: `src/DTMAPI.Core/Runtime/ModOwnerCleanup.cs:9-14`.

Impact:

The disabled Mod remains in the loaded list and can keep Event delegates, Input registrations, APIs, ConfigPage callbacks, GameBridge policies/leases/state, and its `DtmMod` instance active for the rest of the process. Locking its config page does not deactivate those roots. This contradicts a common boundary where disable/unload detaches DTMAPI-owned platform participation even if Mono cannot unload the assembly itself.

Required correction boundary:

- define a single idempotent `DeactivateOwner(owner, reason)` path shared by Entry rollback, official disable, local disable, and future unload;
- remove DTMAPI-owned platform roots and mark the Mod inactive immediately;
- separately report non-unloadable assembly/static/Harmony/native effects as `restart-required` without using that limitation to keep DTMAPI-owned delegates and registrations active;
- define whether same-process re-enable is unsupported (restart required) or a fresh Entry transaction. Do not silently reactivate half-clean state.

Acceptance:

- an enabled-to-disabled refresh leaves zero Event/Input/API/ConfigPage/GameBridge roots for that owner and prevents further callbacks;
- assembly presence may remain reported, but it is not considered an active Mod service;
- dependency-failed and initially disabled Mods never enter the transaction and create no owner roots.

### P1 - ConfigPage and config migration ownership can be spoofed, escaping failed-owner cleanup

Code path:

- `GetApi<IDtmConfigMenuApi>` returns the shared ConfigMenu object; all registration methods accept a caller-supplied `IManifest`.
- `ConfigMenuRegistry.Register` keys the page only from that manifest and overwrites the row: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:15-18`.
- failed-owner cleanup removes only the failing Mod's UniqueID: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:83-89`.
- `IConfigHelper.RegisterMigration` likewise accepts a caller-supplied manifest and stores its delegate by manifest/type; `ConfigService` exposes no owner cleanup: `src/DTMAPI.Core/Services/ConfigService.cs:14-17`, `:86-91`.

Impact:

An ordinary Mod can obtain another loaded manifest through `IModRegistry.Get`, register a ConfigPage or migration under that other UniqueID, then fail Entry. Cleanup for the actual caller will not remove the spoofed callbacks. Duplicate ConfigPage registration also silently replaces the prior owner's page object under the same key.

This breaks the intended common owner lifetime even though Event, Input, and API registration proxies are owner-bound.

Required correction boundary:

- expose ConfigPage and migration registration through caller-bound proxies/tokens, not caller-supplied identity;
- treat an `IManifest` argument as display metadata only after verifying it matches the bound owner, or remove it from the registration surface in a future compatible contract;
- add Config migration removal to the common owner cleanup participant set;
- reject cross-owner and duplicate page registration unless an explicit, owner-checked replace operation exists.

Acceptance:

- a Mod cannot register or replace another owner's ConfigPage/migration using a manifest obtained from the registry;
- failed Entry and unload remove both page callbacks and migration delegates;
- title/save boundaries preserve committed process-lifetime pages/migrations.

### P2 - Duplicate API registration silently overwrites the provider object

Code path:

- owner binding is correct: the public unbound registration throws, and helper registration routes through the attached manifest: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:34-53`, `:74-90`.
- duplicate `(owner UniqueID, TApi)` registration uses dictionary assignment and silently replaces the previous object: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:46-53`.
- cleanup correctly removes all keys with the owner prefix: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:56-71`.

Impact:

Consumers that already obtained the first object keep it, while later `GetApi<T>` calls receive the replacement. The platform has no duplicate diagnostic, no explicit replacement lifecycle, and no disposal/cleanup rule for the replaced provider. Entry can therefore commit with two live API generations even though the registry exposes only one.

Required correction boundary:

- default to rejecting duplicate `(owner, contract)` registration;
- if replacement is needed, define an explicit replace operation with generation, consumer compatibility, and old-provider cleanup semantics;
- add unit coverage for same owner/type duplicate, different contract types, cleanup, and case-insensitive owner IDs.

### P2 - Event quarantine removes handlers from dispatch but deliberately keeps the failing delegate rooted

Code path:

- after three consecutive failures, high-frequency handlers move from `handlers` into `quarantined`: `src/DTMAPI.Core/Services/EventManager.cs:391-445`.
- `quarantined` stores the full `OwnedHandler<TArgs>`, including the original delegate and its captured object graph: `src/DTMAPI.Core/Services/EventManager.cs:295-303`.
- current tests require the quarantined handler objects to remain counted: `tests/DTMAPI.UnitTests/Program.cs:3152-3186`.

Impact:

Quarantine protects the hot path but becomes a process-lifetime root for the failing Mod delegate until an owner unload that currently does not occur on same-process disable. Diagnostics need the owner/event/failure count, not the full callback object.

Required correction boundary:

- remove the delegate after quarantine and retain only bounded scalar metadata (`owner`, `event`, counts, last failure time/type/message hash or bounded detail);
- ensure owner cleanup removes any retained metadata row without needing the callback;
- add a `WeakReference` test proving the quarantined captured object can become collectible after the dispatch snapshot is released.

### P2 - Diagnostics are capped in several places, but they are not yet “bounded counts only”

Code path:

- `ModOwnerLedgerService` retains up to 2048 detailed registration/cleanup/transaction objects, 256 preview aggregate objects, and 64 recent failure objects: `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs:8-17`, `:100-169`, `:178-195`.
- `FileMonitor.LogOnce` retains every unique caller-controlled key in an unbounded `HashSet<string>` for the Mod's process lifetime: `src/DTMAPI.Core/Logging/FileMonitor.cs:15-18`, `:41-49`.
- errors/warnings are capped, but `diagnosticCounters`, hook statuses, and feature statuses are dictionary-backed without a general key cap: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:17-38`, `:96-135`, `:935-948`.

Impact:

The general owner ledger is bounded, but still creates a second long-lived per-registration object graph for ordinary Mods rather than only maintaining counters. `LogOnce` is directly unbounded by ordinary Mod input. The remaining diagnostics dictionaries are mostly fed by platform-owned fixed IDs today, but the invariant is not enforced structurally.

Required correction boundary:

- keep live owner state in the owning registries; diagnostics should aggregate bounded scalar counts by owner/kind/status and retain only a very small bounded recent-failure window;
- cap or hash/evict `LogOnce` keys, or scope them to a fixed-size per-owner cache;
- enforce caps for every diagnostics key dictionary and report trimmed/other counts;
- avoid full registration-detail snapshots on every status publication.

Acceptance:

- stress with many unique registrations, LogOnce keys, failures, and owner IDs produces fixed upper bounds for retained diagnostics objects/strings;
- the authoritative Event/Input/API/Config registries do not gain duplicate diagnostic mirrors;
- reports retain total/active/cleaned/failed/trimmed counts and a bounded recent-failure sample only.

## Title And Save Lifetime Verdict

The current title/save direction is substantially correct and should be preserved during implementation:

- `NotifyReturnedToTitle` clears transient Input state and runtime CustomEntity instances, invokes feature boundary cleanup, and dispatches the public event; it does not remove loaded Mods, APIs, ConfigPages, or Event subscriptions: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:781-803`.
- persistent Input registrations remain in the owner registry; only down/edge/local snapshot state is cleared: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:697-717`.
- local snapshot watches are sampling caches with two-frame generation expiry, not process-lifetime Mod services: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:860-945`.

Implementation must not “solve” unload/diagnostics problems by clearing committed Event/API/Config/Input services on every SaveLoaded or ReturnedToTitle. The correct split is:

```text
process-lifetime Mod service
  -> preserved across save/title
  -> detached on owner failure/disable/unload/shutdown

save/title transient
  -> cleared/rebound at the matching native boundary

diagnostics
  -> bounded scalar aggregates + bounded recent failure samples
```

## Dependency And Pre-Entry Failure Verdict

- dependency, API-version, game-version, reserved-ID, dependency-cycle, and initially disabled checks occur before `LoadCodeMod`, so they do not create helper-owned Event/Input/API/ConfigPage roots;
- entry-DLL/type resolution can load an assembly before a later entry-type failure. Unity Mono cannot unload that assembly in-process. This should remain a restart/process fact, not be confused with permission to retain DTMAPI-owned service roots;
- no new native owner investigation is required for these Core-owned registries. The authoritative owner is the DTMAPI runtime owner transaction and its registries.

Required safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

For this Core-owned slice, the equivalent state holders are identified above. Any follow-up that touches native/Unity/Harmony side effects must perform the additional native-owner review before implementation.

## Recommended Implementation Order

1. Introduce one idempotent owner activation/deactivation coordinator and make `LoadCodeMod` commit/rollback atomic, including `loadedMods` and `modInstances`.
2. Route Entry failure and same-process disable through that coordinator; report assembly/static/Harmony residue separately as restart-required.
3. Bind ConfigPage and config migration registration to the caller; reject owner spoof and duplicates.
4. Reject duplicate API registration or add an explicit replacement contract.
5. Replace quarantined delegates with bounded failure metadata.
6. Reduce diagnostics to bounded counters/recent failures and cap `LogOnce`/diagnostic key sets.
7. Add source/unit gates for title/save preservation versus owner unload cleanup before any game smoke.

## Validation Performed

- `tools/scripts/test.ps1 -Configuration Release`: passed, including all runtime projects/mods and `DTMAPI.UnitTests: OK`.
- No game launch, runtime lock, game install, Workshop mutation, or runtime smoke was performed.
- Existing tests prove the current ordinary partial-Entry cleanup path, owner-bound API registration, owner-bound Input cleanup, GameBridge participant cleanup, config callback rollback, and current quarantine behavior. They do not cover the findings' failure axes listed above.

## Blocker And Completion State

This review is complete as a recorded pre-implementation audit. The common owner-lifetime boundary is not implementation-complete. Public `DtmMod`/`IDtmHelper` can remain Stable at the container level, but `IModRegistry.RegisterApi/GetApi` should not be promoted from StableCandidate until duplicate semantics, unload cleanup, and the cross-surface owner transaction tests pass.

## Resolution Link

Implementation and validation are owned by [Update 20260711-0010](../../../updates/2026/20260711-0010-general-owner-lifetime-refactor.md). This review remains the pre-implementation fact record.
