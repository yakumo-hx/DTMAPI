# Production QA Seam And Animal Refresh Audit

**Review ID:** `20260722-0011`
**Date:** 2026-07-22
**Status:** recorded — Phase 1 cleanup candidates identified; Animal optimization remains ProductNative
**Scope:** read-only residual production-QA seam and AnimalHusbandryProgress allocation/refresh audit; no implementation, sixth product, game, Release, L0–L5, GC, long test or 0.5.5 publication

## Result

No new five-product behavior or owner blocker was found. Phase 1 has several bounded cleanup candidates with real default-runtime cost. AnimalHusbandryProgress has a concrete product-local refresh/allocation hotspot, but no second consumer or common native owner justifies a SharedNative promotion.

## Phase 1 Priority 1 — Dead `ReflectedUnityInput` Diagnostics

`src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs` retains the old polling-diagnostics family:

- diagnostic fields from approximately lines 37–67;
- conditional GetKey/GetKeyDown and backend counters;
- unconditional latch frame/edge counters in the ordinary input-frame path;
- public `ConfigureDiagnostics`, `ResetDiagnostics` and `GetDiagnosticsSummary` methods;
- backend-specific sample/attempt/exception counters through approximately line 713.

The three diagnostic methods have no repository consumer, while several latch counters still increment during ordinary frames. A focused cleanup may remove only those fields, counter mutations and summary/configuration methods.

It must retain the actual input implementation: `GetKey`, `GetKeyDown`, title input capture, `LatchInputFrame`, cached button sampling, logical modifiers, edge suppression and `ClearTransientState`. Title UI and DebugConsole still depend on this class.

## Phase 1 Priority 2 — Superseded Core Input And Fatal-Window Endpoints

`DtmApiRuntime.RecordInputPressed`, `RecordInputReleased` and `GetRegisteredInputButtons` have no current production caller; the real production boundary is typed `RecordInputFrame`. The old methods are internal rather than public Abstractions ABI, but deletion still requires one focused assembly/old-consumer scan and conversion of remaining Units to typed-frame assertions for pressed/released/keybind/suppression behavior.

`NotifySaveLoadFatalWindowObserved` likewise has no current caller. It should either become an explicitly owned native watcher boundary through a separate Review or be removed as an orphan diagnostic endpoint; do not leave it as an implied active safeguard.

## Phase 1 Priority 3 — ActionSpeed QA Summaries Allocate In Gameplay Hooks

`products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs` stores application counters and `Last*Summary` strings that are read only by `Batch6ActionSpeedReflectionObserver`. Tool and interaction paths construct `List<string>` samples and joined summary strings; continuous-use callbacks format a new summary on each application.

This is product runtime work created solely for optional QA observation. Follow the accepted AutoFishing D.5 pattern:

- diagnostics are null/disabled by default;
- enabling an explicit QA observer creates the counters/samples;
- gameplay behavior and necessary production logging remain independent;
- disabling/deactivation drops the diagnostic object and any strings;
- the external observer reads the opt-in object rather than requiring always-on product state.

This remains an ActionSpeed product slice, not a new shared QA ABI.

## Lower-Priority Candidates

- `Batch5NoDemandRuntimeSnapshot` and several GameBridge scheduler counters exist mainly for Unit proof. In particular the idle fast-path count mutates every frame. Retain scalar state needed for production health/recovery, but move purely quantitative stress observation behind an explicit observer or remove redundant counts.
- Animal QA observation methods are public only because installer/QA reflection currently searches public static members. They can become internal/private if both installer and QA search `Public | NonPublic` and product callback compatibility is unchanged.
- Ordinary `ForTests` wrappers and pure on-demand formatting helpers have no steady-state cost and are not worthwhile first-slice targets.

## Boundaries That Must Remain

The following are not accidental production QA residue:

- the Batch 4 neutral optional QA Host contracts, activation loader, fixture access and lifecycle bridge; ordinary player runs have no scenario/evidence/mutation cost;
- AutoFishing nullable/on-demand diagnostics, whose ordinary cost is only a null check;
- save-load activation and native-continuation breadcrumbs used by the still-open ISSUE-010 lifecycle investigation;
- atomic-commit fault injection and EventManager race checkpoints that prove transaction/concurrency invariants;
- EnvironmentReset health/failure/recovery accounting; a member named `ForTests` is not removable when the same scalar protects production health semantics;
- `RefactorScaffoldOptions`, which mixes rollback flags, ISSUE-010 options and historical scaffold state and requires its own flag-retirement Review;
- the readiness currently named `SmokeDiagnosticsHookTargetsReady`, because despite its old name it protects real DebugConsole input-isolation Hooks.

Do not recreate a second QA Host while removing individual seams.

## Animal Product-Local Hot Path

AnimalHusbandryProgress subscribes to every `UpdateTicked`. While its panel clones are visible, `AnimalHusbandryNativeRuntime.Refresh` runs approximately every 80 ms and rewrites up to three unchanged rows.

Each pass may repeat type resolution, component and method lookup, object-array allocation, progress-string formatting, color parsing/construction and a full descendant Text traversal. `NativeReflection.ResolveType` can scan loaded assemblies, and `SetChildTexts` walks all descendants. The product's `ProgressRow` values do not change after construction.

The reviewed native `AnimalViewer.Show`/`OnShow` and `ProgressBar` implementations write the displayed state once rather than running a persistent refresh. The product loop is therefore reasserting stable UI, not mirroring a native continuously changing owner.

## Approved Animal Optimization Boundary

The smallest safe ProductNative design is:

1. sort once during row construction and retain at most three rows plus the primary row;
2. cache reflected accessors, the parsed color object, each clone's ProgressBar/Text/Graphic references and preformatted progress text;
3. write hidden clone state before activation, then permit at most one next-frame guard for native first-frame overwrites;
4. stop refreshing once that guard completes unless an explicit dirty boundary rebuilds the overlay;
5. perform no steady-state assembly scan, `GetComponentsInChildren`, color parse, string formatting or reflected method search;
6. destroy tracked clones once in reverse order; use named-child scanning only as a one-time untracked-residue fallback;
7. cache output descriptors by animal ID if useful, clearing them at SaveLoaded, ReturnedToTitle and native close while continuing to rebuild current husbandry values from live data.

Historical `心情` first-frame flicker means the guard cannot simply be deleted without a repeated-switch acceptance. This entire optimization stays in Animal ProductNative: there is one real consumer and no common native owner.

## Focused Validation For Later Implementation

Phase 1 slices:

- `tools/scripts/test-batch4-qa-semantic-inventory.ps1`;
- `tools/scripts/check-batch4-qa-semantic-boundary.ps1`;
- existing typed-input, suppression and EventManager race Units;
- a source check proving the deleted input diagnostics/old adapters have zero production references;
- `tools/scripts/test-batch6-actionspeed-advanced-product.ps1` if ActionSpeed diagnostics become opt-in.

Animal slice:

- `tools/scripts/test-batch6-animalhusbandryprogress-advanced-product.ps1`;
- focused Units proving one initial write plus at most one guard, zero writes across 100 clean updates, stable three-row ordering, exactly-once clone destruction and exception-safe callback detach/exact-owner unpatch;
- only after actual render-code changes, one third-save repeated-animal-switch short acceptance for first-frame `心情` flicker, localized title, unchanged native mood, native close and exact owner cleanup.

No complete Release, L0–L5, GC or long test is required for these bounded slices.

## Disposition

**GO** for small Phase 1 removal/opt-in slices and a later Animal ProductNative refresh optimization.
**NO-GO** for deleting the neutral QA Host, transaction/race/fatal-lifecycle safeguards, or turning Animal UI reflection into SharedNative.
**UNCHANGED:** five-product admission and behavior/owner baseline; sixth product and 0.5.5 publication remain blocked.

Related authorities:

- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md` Phase 1
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md`
- `docs/reviews/code/2026/20260722-0008-fish-animal-shared-boundary-review.md`
- `docs/updates/2026/20260722-0004-five-product-baseline-correction.md`
