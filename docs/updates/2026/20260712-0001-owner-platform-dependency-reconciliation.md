# 20260712-0001 Owner Platform Dependency Reconciliation

## Metadata

- Update ID: `20260712-0001`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `verified`
- Source: user screenshot and matching runtime log exposed false deactivation of three ordinary owners after the common owner-lifetime refactor.

## Scope

- distinguish process-lifetime registry providers from source-managed ordinary Mod providers during required-dependency reconciliation;
- preserve active ordinary owners across title/Workshop refresh when their platform dependencies remain registered and compatible;
- retain real removal, disable, version-mismatch, optional-warning, reverse-dependency cleanup, and restart-required semantics;
- deactivate a loaded owner when duplicate arbitration changes its authoritative `Source + OfficialId + RootPath`, instead of keeping the old assembly/root active under the replacement discovery row;
- clear Core transient Input state and demand-local watches on SaveLoaded while preserving persistent owner registrations and other process-lifetime services;
- make file/host/diagnostic logging sinks observational so they cannot interrupt activation, reconciliation, deactivation, or cleanup;
- publish each process-lifetime provider's canonical manifest and API contract atomically, and reserve dynamically registered provider IDs against ordinary source collision;
- require permanent same-process restart only after a Mono assembly successfully loads or while owner cleanup cannot prove zero roots; allow zero-root non-code/no-assembly owners to republish;
- count quarantine overflow as historical trimmed metadata rather than a synthetic current root, and make Config preview aggregates counts-only;
- keep Zoom's ordinary-Mod and Camera-lease owner boundary unchanged except for preventing false deactivation;
- keep public API signatures, `0.5.3-alpha`, Hook targets, native game state, and content formats unchanged.

## Source Review

- `docs/reviews/manual-qa/2026/20260712-0001-owner-platform-dependency-reconciliation.md`
- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`

## Related Baseline

- Base implementation: `docs/updates/2026/20260711-0010-general-owner-lifetime-refactor.md`
- Owner contract: `docs/design/mod-owner-lifetime-contract.md`
- Verified regression: `docs/debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md`

## Current Diagnosis

The failing reconciliation treated every dependency as a refreshed ordinary source. `DTMAPI.DebugConsoleHost`, `DTMAPI.GameBridge.DolocTown`, and `DTMAPI.ModConfigMenu` are instead process-lifetime providers published through the authoritative runtime registry. Their intentional absence from ordinary discovery was misread as required-dependency loss, so DebugConsoleMod, ZoomMod, and AutoFishing were deactivated after successful Entry.

The cleanup path itself reported zero cleanup failures. This Update therefore owns a provider-classification correction, not a weakening of unified owner cleanup.

The follow-up code audit found seven adjacent contract gaps in the same reconciliation/lifecycle slice: source arbitration could change a loaded owner's package identity without detaching the old assembly/root; SaveLoaded did not clear Core-local transient Input state; logging sinks could throw through authoritative reconciliation; runtime provider manifest/API publication was split and dynamic provider IDs were not automatically reserved; every non-shutdown cleanup was classified restart-required even before any assembly loaded; quarantine overflow appeared as an unremovable synthetic current root; and Config preview aggregates duplicated arbitrary failure detail. They share the original audit's owner-authority, zero-root, title/save, and bounded-diagnostics acceptance boundary; none requires a public signature or native Hook change.

## Implementation

The verified implementation establishes the following boundary:

- records successfully registered process providers and resolves their dependency version from the authoritative registry without requiring an ordinary discovery row;
- uses an atomic `RegisterProcessLifetimeApiForOwner` path, preserves one canonical manifest across distinct contracts, rejects duplicate/conflicting registrations, reserves dynamic provider IDs against ordinary source loading, and refuses a late process provider that tries to claim an already loaded source-managed owner;
- continues to govern ordinary providers by their loaded registry row plus refreshed source enablement/version, including optional warnings and reverse required-dependency cascades;
- treats a changed selected `Source + OfficialId + RootPath` as an in-process source handoff, deactivates the old owner, and blocks replacement Entry after a loaded assembly until restart;
- clears Core transient Input state/demand-local watches at SaveLoaded before ordinary callbacks while preserving persistent registrations and all other committed owner services;
- records successful `Assembly.LoadFrom` per owner, makes that fact permanently restart-required, and otherwise removes lifecycle state only after Core plus all participants prove zero roots/failures;
- isolates file, host, and diagnostic log sinks so observation failure does not propagate through loader/reconciler correctness;
- reports pre-assembly code-load failures from the actual assembly/restart state, counts named quarantine metadata as a removable current root while keeping overflow only in `trimmedQuarantines`, and removes retained detail from Config preview aggregates while keeping the shared 64-entry recent-failure window;
- extends focused tests for real Zoom/Camera no-op refresh, platform-provider presence/loss/version/optional behavior, both process-first and ordinary-first provider collisions, source identity handoff, source disable/removal/version cascades, SaveLoaded transient Input, participant exceptions under failed log sinks, missing/BadImage DLL repair in the same process, no-assembly/non-code re-publication, quarantine overflow/current-root cleanup, final-health gate truth cases, and preview aggregate retention.

## Changed Files

Current task-scoped implementation and test files:

- `src/DTMAPI.Core/Logging/FileMonitor.cs`;
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`;
- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs`;
- `src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs`;
- `src/DTMAPI.Core/Services/EventManager.cs`;
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`;
- `tests/DTMAPI.UnitTests/Program.cs`;
- `tools/scripts/run-game-smoke.ps1`.

Task-scoped lifecycle/fact-owner documentation:

- `docs/reviews/manual-qa/2026/20260712-0001-owner-platform-dependency-reconciliation.md`;
- `docs/design/mod-owner-lifetime-contract.md`;
- `docs/debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md` and its issue index row;
- `docs/api/public-api-matrix.md`;
- the follow-up link in `docs/updates/2026/20260711-0010-general-owner-lifetime-refactor.md`;
- this Update and its monthly ledger row;
- `docs/debug/regressions/smoke-matrix.md`.

The historical `20260711-0010` implementation result and `20260711-0013` Zoom result remain owned by their original Updates; this record does not rewrite either result.

## Validation

- Final `tools/scripts/test.ps1 -Configuration Release` after the last code/test review passed with exit code `0` in approximately `108s`; all runtime, first-party, test-Mod, and UnitTests projects compiled with `0` warnings and `0` errors, and the executable reported `DTMAPI.UnitTests: OK`.
- The preceding full Release runs also passed in approximately `63.6s` and `85.4s` with the same zero-warning/error and `DTMAPI.UnitTests: OK` result.
- Unit coverage includes platform-provider presence/loss/version/optional behavior, canonical/dynamic provider atomicity, process-first and ordinary-first collision rejection, source identity handoff, source disable/removal/version cascades, SaveLoaded transient Input cleanup, participant and logging-sink failures, missing/BadImage DLL same-process repair, no-assembly/non-code re-publication, real Zoom/Camera no-op refresh and disable, named/overflow quarantine roots, final-health gate truth cases, and counts-only preview aggregates.
- `tools/scripts/check-doc-governance.ps1`: passed all `3952` checks.
- Final `git diff --check`: passed with exit code `0` after documentation closure.
- 2026-07-12 validation correction: a later clean/full Release rerun exposed one CS8602 in the ConfigMenu owner-facade mismatch message, so this record's earlier blanket zero-warning statement was not a reliable clean-build fact. Update `20260712-0003` owns the nullable correction and a replacement full Release run with verified `0 warnings / 0 errors`.

## Runtime Evidence

- First locked slot-3 attempt `GAME-SMOKE/20260712-010905` ran the planned title/save/OwnerLifetime route. All `45` requested runtime gates passed; discovery/load remained `29/6`, all six code Mods entered once, `needsRestart=0`, OwnerLifetime proved `8 -> 8 -> 0`, Camera release and `remaining=0`, and the process exited cleanly. Its outer `RunStatus=Failed` was a harness false negative: optional `GameBridgeZoomCleanupHealth` was treated as required even though `-AutoExerciseZoomOwnerLifetime` was not requested. This is not a runtime failure and is superseded within this Update, so it has no smoke-matrix failed row.
- The harness now evaluates generic GameBridge final health independently; an unrequested Zoom gate remains skipped but cannot bypass a missing or unhealthy generic snapshot, while a requested Zoom owner exercise retains its specialized cleanup health assertion. `tests/DTMAPI.UnitTests/Program.cs` locks the script expression and its four truth cases.
- Final planned locked command `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -AutoExerciseModOwnerLifetime -TimeoutSeconds 360` passed as `GAME-SMOKE/20260712-011551`. `RunStatus`, startup, GameLaunched, HookProbe, SaveLoaded, TitleButtonLifecycle, OwnerLifetime, final health, no-fatal, and process-exit gates all passed. Six code Mods completed Entry exactly once; registry state stayed `29 discovered / 6 loaded`; `needsRestart=0`; control roots were `8 -> 8 -> 0`; the real Camera lease was live before cleanup and released by cleanup; Core/GameBridge `remaining=0`; no `DolocTown.exe` remained.
- Focused locked no-op command `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoReloadMods -TimeoutSeconds 360 -SkipBuild` passed as `GAME-SMOKE/20260712-011924`. At the refresh, both `DiscoverMods` and `LoadMods hot` reported `rows=29; loadedRows=6`, hot load reported `loadedNow=0`, dependency errors/warnings stayed zero, all six Entry completion counts remained one, HookProbe reported `WorkshopModListChanged OK count=29`, and there was no false required-dependency warning, restart classification, or cleanup failure. Final health, no-fatal, normal exit, and no-residual-process checks passed.
- Final combined command `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoReloadMods -AutoExerciseTitleButtonLifecycle -AutoExerciseModOwnerLifetime -TimeoutSeconds 360` passed as `GAME-SMOKE/20260712-080130`. `RunStatus`, StartupLog, GameLaunched, HookProbe, SaveLoaded, TitleButtonLifecycle, OwnerLifetime, ModOwnerLifecycle, ModLoadTransaction, OwnerBoundInput, EventHandlerCleanup, FailedModRollback, DependencyCompatibility, GameBridgeFinalHealthSnapshot, TitleReturnBoundaryLedger, NoFatalInstanceWindow, ForcedClose, and ProcessExited all passed. Initial publication was `rows=29; loadedRows=6`; the hot refresh remained `loadedNow=0; rows=29; loadedRows=6` with zero dependency errors/warnings; each of the six ordinary code Mods completed Entry exactly once. OwnerLifetime reported `savePreserved=True`, `expectedControl=8`, `controlBeforeCleanup=8`, both control/cleanup remaining counts zero, Camera live before and released after cleanup, zero Core/participant cleanup failures, authoritative `remaining=0`, and `restartRequired=False`. No `DolocTown.exe` remained.
- The shared runtime lock was acquired and released for the game operations. Only the three passed acceptance runs above are added to the active smoke matrix.

## Evidence

- Failing local run: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`, observed 2026-07-12; the durable excerpts and reproduction state are transcribed in ISSUE-013.
- Passed evidence: `docs/debug/evidence/GAME-SMOKE/20260712-011551`, `docs/debug/evidence/GAME-SMOKE/20260712-011924`, and `docs/debug/evidence/GAME-SMOKE/20260712-080130`.
- Superseded harness-classification attempt: `docs/debug/evidence/GAME-SMOKE/20260712-010905`.

## Rollback

Revert the task-scoped source, test, harness, and documentation changes listed above without reverting the base common owner transaction or cleanup contract. Rolling back provider classification would restore the false refresh deactivation, so rollback requires a clean process and replacement runtime verification. Any run that successfully loaded and then deactivated an affected Mono assembly still requires restart; a no-assembly/non-code zero-root owner does not manufacture that requirement.

## Follow-Up

ISSUE-013 is verified for this scoped reconciliation and owner-lifetime boundary. No public API/version promotion or Hook Map change is required. Broader native/Mono lifetime work remains owned by ISSUE-010; reopen ISSUE-013 only if an unchanged refresh again reduces active ordinary owners or creates false restart/cleanup state.
