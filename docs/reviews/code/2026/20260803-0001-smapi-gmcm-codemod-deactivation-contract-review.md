# SMAPI、GMCM 与通用 CodeMod Deactivation Contract 对比审计

- Review ID: `20260803-0001`
- Date: `2026-08-03`
- Status: `recorded`
- Scope: same-process CodeMod disable/deactivation, Mod instance exit callback, owner-root cleanup, GMCM registration removal, API/UI staleness, retry and restart boundary
- Change type: audit-only Review; no Runtime, API, package, Workshop, save or game mutation
- Related baseline: [DTMAPI 0.5.5 收尾后与 SMAPI 的能力复比审计](20260801-0001-dtmapi-055-closeout-smapi-capability-recomparison.md)
- Related focused audit: [SMAPI 失效 Mod 判定与 Hook 边界审计](20260802-0003-smapi-invalid-mod-classification-and-hook-boundary-audit.md)

## Source Request

The user identified the unresolved public DTMAPI question correctly: owner-bound
handles and registries can remove many resources, but that alone does not define
whether every loaded CodeMod receives one unified exit callback when a player
disables it in the same process, nor whether the platform promises hot unload or
same-process re-enable.

This Review compares that question against current SMAPI and the real Generic
Mod Config Menu (GMCM) package, including real GMCM consumers. It then separates
the decisions DTMAPI can safely formalize from the stronger promises that remain
unsupported on Unity Mono.

## Audited Baselines

### DTMAPI

- repository HEAD: `abe9596cbdde2d0d3b2a66d34f8b867b5af0beac`
- current public base: `src/DTMAPI.Abstractions/DtmMod.cs`
- current internal lifecycle: `src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs`
- current owner cleanup: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- current ConfigMenu owner cleanup:
  `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs` and
  `ConfigMenuPage.cs`
- canonical identity/restart boundary: `PROJECT.md` and
  `docs/architecture/batch6-managed-mod-identity-contract.md`

### SMAPI

- repository: `E:\Python_project\SMAPIlearning\SMAPI`
- branch/commit: `develop`,
  `5689c8d6aeecf54f670559ffaaed6684a5febc25`
- description: `4.5.2-54-g5689c8d6`
- primary paths:
  - `src/SMAPI/Mod.cs`;
  - `src/SMAPI/Framework/SCore.cs`;
  - `src/SMAPI/Framework/ModRegistry.cs`;
  - `src/SMAPI/Framework/Events/ManagedEvent.cs`;
  - `src/SMAPI/Framework/ModHelpers/ModHelper.cs`;
  - `src/SMAPI/Framework/ModHelpers/ModRegistryHelper.cs`;
  - `src/SMAPI/Integrations/GenericModConfigMenu/*`.

### GMCM and real consumers

- installed package:
  `E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods\【mod菜单】GenericModConfigMenu\GenericModConfigMenu`
- manifest/file version: `1.16.0`
- product commit embedded in DLL: `c90f93f65eee9f9704852b7dfa503f4b6d415950`
- DLL SHA-256:
  `E6ED782BB8B1BAC1DEEB1B3A09DB8EB3758BDC7CA510F50E7CCFB7D2C1D9479C`
- publisher pages still list `1.16.0` as the current release at audit time:
  - <https://www.nexusmods.com/stardewvalley/mods/5098?tab=files>
  - <https://www.curseforge.com/stardewvalley/mods/generic-mod-config-menu/files/latest>
- thirteen installed real Mod DLLs reference the GMCM API interface.
- The package and consumers were inspected through manifest/PE metadata and
  read-only IL decompilation. Decompiled third-party code was not copied into
  DTMAPI.

## Executive Verdict

SMAPI and GMCM do **not** provide a model that DTMAPI can label “all CodeMods
support hot unload”. They deliberately avoid that problem:

1. Current SMAPI has no same-process per-Mod disable/re-enable state machine.
   `Mod.Dispose` is a whole-runtime/game-exit callback, not a live Mod
   deactivation protocol.
2. GMCM's `Unregister(IManifest)` removes one config registration from an
   internal dictionary. It is a service-level registration operation, not a
   CodeMod exit, owner cleanup, API revocation or assembly unload.
3. Real GMCM consumers use `Unregister` as “remove old registration before
   rebuilding it”, not as a shutdown hook.
4. DTMAPI 0.5.5 already has materially stronger internal owner deactivation than
   either example, but its author-facing `DtmMod` exposes only `Entry`. The
   reason-aware reflected method, optional `IDisposable` fallback, retry
   semantics and terminal states are not yet one public contract.

The appropriate DTMAPI promise is therefore **managed behavior deactivation with
owner cleanup and honest restart state**, not **physical CodeMod unload**.

## Capability Comparison

| Question | Current SMAPI | GMCM 1.16.0 | Current DTMAPI fact / contract gap |
| --- | --- | --- | --- |
| Can a player disable one loaded CodeMod in-process? | No Loader state/command for it | No; GMCM is an ordinary SMAPI Mod | Internal owner deactivation exists, but public author contract is incomplete |
| Unified Mod callback | `Mod` implements `IDisposable`; called while the whole runtime exits | Inherits SMAPI `Mod`; does not override `Dispose(bool)` | Core recognizes a private reason-aware reflected method, otherwise optional `IDisposable`; `DtmMod` declares neither |
| Callback meaning | Release unmanaged resources when the game exits; explicitly not guaranteed on every exit | No GMCM-specific exit implementation | Currently used for Entry failure, unload and runtime shutdown; that wider meaning is not public |
| Platform-owned event cleanup | Per-handler add/remove; owner attribution for errors, no bulk owner removal | GMCM manually subscribes many long-lived handlers | Owner-wide removal exists and is counted |
| Config UI removal | Not a Core lifecycle feature | `Unregister` removes one dictionary entry only | Registry detaches the page, then makes stale page/item/preview references inert |
| Cross-Mod API revocation | Per-consumer API objects are cached; registry has no remove/invalidate path | Per-consumer `Api` objects retain GMCM manager/delegates | Owner-bound facades can be deactivated and registry roots removed |
| Harmony cleanup | No generic Mod unpatch | GMCM has no Harmony reference | Canonical Advanced owner supervision and cleanup participants exist for admitted products |
| Dispose/unpatch failure | Log and continue whole-process shutdown; no retry/status change | Not implemented | Per-step failure isolation, remaining-root count and restart-required diagnostics exist |
| Retry | None | None | Terminal states allow another cleanup pass; failed Mod instance is retained, but no public maximum/author rule exists |
| Same-process re-enable | Unsupported | Unsupported | `BeginEntry` rejects a second entry; a loaded assembly makes deactivation restart-required |
| Physical assembly unload | Not supported | Not supported | Not claimed; assemblies remain process-pinned |

## What SMAPI's `IDisposable` Actually Means

SMAPI added `IDisposable` to `Mod` in 2017 commit `494f9366` with the explicit
purpose “let mods dispose unmanaged resources when SMAPI is disposing”. The
current `Mod.Dispose(bool)` documentation narrows this further to cleanup “when
the game exits” and states that it is not guaranteed on every exit.

Current shutdown behavior is:

1. `SCore.Dispose` enters whole-runtime disposal;
2. it iterates every registered Mod and calls `IDisposable.Dispose`;
3. one Mod's exception is logged and does not block disposal of later Mods;
4. SMAPI then disposes its content/game/log components.

This is not a dependency-aware per-Mod deactivation transaction:

- the Mod registry has `Add` but no runtime `Remove` path;
- managed events can remove an exact delegate but have no owner-wide removal;
- `ModHelper.Dispose` currently does nothing;
- API instances cached in each consumer's `ModRegistryHelper` are not invalidated;
- SMAPI does not generically unpatch Harmony owners;
- remaining roots are not counted;
- no retry, `restart-required`, or same-process re-entry decision follows a
  disposal exception.

SMAPI can use this weak contract because the process is already leaving. Copying
the same `IDisposable` surface into a live-disable feature would silently give it
a much stronger meaning than SMAPI itself promises.

## What GMCM `Unregister` Actually Means

The current GMCM API exposes:

```text
Register(IManifest, reset callback, save callback, ...)
Unregister(IManifest)
```

The actual `Unregister` implementation resolves the manifest and calls
`ModConfigManager.Remove`. That method removes the dictionary entry keyed by the
manifest UniqueID if present. It does not:

- call the consuming Mod's exit/dispose callback;
- unsubscribe the consuming Mod from SMAPI events;
- remove Harmony patches, commands, input or content changes;
- revoke the consuming Mod's cached GMCM API object;
- close an already open `SpecificModConfigMenu`;
- invalidate all delegates held by an already open menu;
- remove GMCM itself from SMAPI;
- unload either assembly.

The open-menu point is important. A constructed GMCM menu holds the `ModConfig`
object and its reset/save/get/set/option delegates directly. Removing the entry
from the manager dictionary does not mutate that menu. It follows from the
inspected object graph that an already-open menu can retain the old callbacks
until it closes. GMCM does not need to solve that as a general disable race
because SMAPI does not live-disable the consuming Mod.

GMCM itself also demonstrates the process-lifetime assumption. Its `Entry`:

- stores a static Mod instance;
- subscribes to game-loop, display, input and content events;
- registers a global trigger action;
- registers optional Better Game Menu callbacks;
- owns active UI state.

It does not override `Dispose(bool)` and has no matching bulk teardown for those
roots. It contains no Harmony reference, so it is not evidence about patch
uninstallation at all.

### Real consumer behavior

Thirteen installed sample DLLs reference the GMCM API. Read-only decompilation
found six real `Unregister` call sites. Every one immediately performs
`Unregister -> Register`:

- Automate;
- Chests Anywhere;
- Content Patcher;
- Fast Animations;
- Lookup Anything;
- NPC Map Locations.

The shared integrations use this sequence to make config registration
repeatable/rebuildable. No inspected call site uses `Unregister` as a Mod exit
boundary. SMAPI's own built-in GMCM integration declares only the subset it
uses, calls `Register` once, and does not declare `Unregister` at all.

GMCM is therefore useful evidence for **service registration replacement**. It
is counter-evidence for treating a service's `Unregister` method as proof of
whole-Mod deactivation.

## Current DTMAPI Behavior Already Chosen Internally

The current internal state machine is:

```text
Entering -> Active -> Deactivating -> InactiveRestartRequired
                                  `-> Shutdown
```

`BeginEntry` rejects a second entry for an owner that has already entered in the
process. `DeactivateOwner` starts from `Deactivating`, invokes a private
preparation boundary when present, then invokes the private reason-aware
deactivation method or `IDisposable`, and finally removes owner resources in
separate failure-isolated steps.

The current cleanup sequence is substantially:

1. enter `Deactivating`, which blocks new owner registrations;
2. run optional atomic preparation;
3. run reason-aware Mod deactivation or `IDisposable`;
4. remove Events and Input;
5. remove/deactivate ConfigMenu pages, config migrations and Content roots;
6. invalidate registry/API facade roots before native feature participants;
7. run Harmony/GameBridge/native cleanup participants;
8. remove custom entities and runtime demand;
9. remove instance/loaded-list roots and count all remaining roots;
10. publish restart-required and diagnostics.

If preparation fails, current Core restores the previous owner state and keeps
all roots so the operation can be retried atomically. Once committed cleanup
begins, later step failures do not stop the other cleanup steps. If Mod cleanup
throws, the instance remains as an authoritative root for a later cleanup pass.

Even when all known roots reach zero, any non-shutdown deactivation after an
assembly load is `InactiveRestartRequired`. This already decides the central
technical question: DTMAPI does not permit same-process re-entry of a loaded
CodeMod.

DTMAPI ConfigMenu is also already stronger than GMCM for owner cleanup:

- the API facade is requester/owner-bound and rejects registration for another
  manifest;
- `RemoveOwner` removes the authoritative page first;
- `ConfigMenuPage.Deactivate` marks stale references inactive before clearing
  reset/save/display/item/preview delegates;
- stale facades and pages throw or remain inert instead of calling retired Mod
  code.

The missing piece is not the cleanup kernel. It is a stable, author-visible
statement of which callback is called, its allowed operations, retry behavior,
and the exact terminal status vocabulary.

## Recommended Public Decision

### 1. Name the promise correctly

Publish a **managed owner deactivation contract**. Explicitly state that it:

- stops a loaded Mod's managed behavior as far as DTMAPI-owned roots and admitted
  cleanup participants can prove;
- never claims physical assembly unload;
- never promises same-process re-enable after the assembly has loaded;
- does not extend to External BepInEx Plugins or unknown legacy native side
  effects.

Avoid the public phrases “hot unload” and “fully unloaded CodeMod”.

### 2. Do not require every Mod to implement `IDisposable`

DTMAPI should always perform platform owner cleanup whether or not author code
implements a callback. Strict Mods which only use owner-bound DTMAPI services
should need no custom teardown.

Allow `IDisposable` as a compatibility fallback for the admitted products that
already use it, but do not make bare `IDisposable` the final author contract. It
has no reason, phase, safe-operation or retry semantics and SMAPI's precedent
means “whole-process exit” to many authors.

The stable future surface should be a typed optional boundary, for example an
interface or virtual method receiving:

- reason: EntryFailed, PlayerDisabled, DependencyFailed, SourceChanged, or
  RuntimeShutdown;
- whether process shutdown is underway;
- whether the assembly was loaded and restart is therefore required;
- one activation/deactivation generation or transaction ID.

The existing reflected `DtmApiPrepareOwnerDeactivation(string)` and
`DtmApiDeactivateOwner(string)` names remain internal product mechanisms until a
separate API review defines the typed contract. They must not become public by
documentation accident.

### 3. Freeze the cleanup order around quiescence

The public ordering guarantee should be:

1. run only at a DTMAPI runtime-thread safe point, with no owner callback already
   executing;
2. atomically mark the owner `Deactivating`, reject new registrations/API entry
   and cancel future queued/captured callback admission;
3. invoke the optional Mod deactivation callback while its objects still exist
   but all new platform admission is closed;
4. regardless of callback success, make Events/Input/UI/Config roots inert and
   invalidate provider/consumer API facades;
5. remove Content/demand/native/Harmony participants in their documented owner
   order;
6. count roots and publish the terminal state.

The author callback must not be responsible for calling every service's
`Unregister`. GMCM shows why that would be incomplete and order-sensitive.

### 4. Separate preparation failure from committed cleanup failure

Use distinct outcomes:

| Situation | Required visible result |
| --- | --- |
| Deactivation preparation rejects/fails before mutation | Mod remains `Active`; show `deactivation-failed/deferred`, not `disabled` |
| Cleanup commits and all known roots reach zero, but assembly was loaded | `InactiveRestartRequired` |
| Cleanup commits but Dispose/unpatch/participant fails or roots remain | `InactiveWithErrorsRestartRequired` (or equivalent detail under `InactiveRestartRequired`) |
| Cold-disabled and never loaded | `Disabled`; no assembly restart reason |
| Runtime is exiting | `Shutdown`; failures are logged but do not block other owners |

Do not use one undifferentiated `Failed` label for both “still active because
deactivation never committed” and “inactive but residual roots require restart”.

### 5. Bound retries by operation type

Do not automatically loop arbitrary Mod callbacks. A throwing callback may have
partially mutated static/native/save state and is not proven idempotent.

Recommended rule:

- invoke the author deactivation callback at most once per deactivation
  transaction;
- allow explicit retry only if the future typed contract requires idempotence
  and carries the same transaction/generation ID;
- freely retry DTMAPI-owned cleanup steps and cleanup participants only where
  their contracts are explicitly idempotent;
- perform no per-frame retry loop;
- a final shutdown cleanup pass is best effort and must not change the already
  published restart-required truth.

Current Core permits later cleanup passes and can reinvoke a retained failed Mod
instance. Before publishing the contract, either bound that behavior as above or
explicitly require author idempotence and state the maximum attempts. Leaving the
count implicit is the current gap.

### 6. Forbid same-process CodeMod re-enable after load

This should be the Batch 6 public rule, matching current implementation:

- a Mod which was never loaded may follow a separately supported cold-enable
  path;
- once its assembly entered Unity Mono, disabling it deactivates behavior but
  ends in restart-required;
- re-enable, source update or binary replacement takes effect only after a clean
  process restart;
- static fields, unknown native state, failed unpatch or remaining roots add
  diagnostic reasons but are not the only reason restart is required.

This is stricter and easier to support than attempting to decide that a
particular assembly happens to be safe to enter twice.

## Minimum Acceptance Matrix For The Public Contract

1. Disable while an event is queued: no callback begins after `Deactivating`.
2. Disable with the owner's ConfigMenu page open: close/cancel the view, invoke
   no stale save/get/set callback, and make retained page references inert.
3. Disable an API provider with cached consumers: old facades fail closed and no
   call reaches provider state after the boundary.
4. Disable an API consumer: provider registrations/facades owned by that
   consumer are removed without requiring manual `Unregister`.
5. Author callback throws: later platform and Harmony cleanup still run;
   terminal state is inactive-with-errors/restart-required.
6. Harmony unpatch or native participant fails: remaining roots and exact owner
   are reported; sibling Mods remain active.
7. Preparation fails: all roots remain and the Mod is truthfully still active.
8. A second enable/source update after assembly load is rejected with a clear
   restart-required reason.
9. Shutdown cleans dependents before providers where dependency order matters
   and one failure does not prevent later owner cleanup.
10. Repeated platform cleanup is idempotent; arbitrary author callback retry
    follows the published once/idempotence rule exactly.

## Decisions Not Supported By This Comparison

- GMCM `Unregister` does not prove general Mod unload.
- SMAPI `Mod : IDisposable` does not prove live per-Mod deactivation.
- owner-zero roots do not prove the Mono assembly or every static/native side
  effect is gone.
- a canonical Harmony owner does not make unknown third-party patches safe to
  unload.
- this Review does not authorize a public API addition, general Advanced author
  lane, Content Host, or same-process code re-entry.

## Validation And Limits

- Inspected current SMAPI source and the historical commit that introduced Mod
  disposal.
- Read-only decompiled the exact GMCM 1.16.0 DLL and all thirteen installed
  consumer DLLs which carry its API interface.
- Confirmed six actual consumer `Unregister` calls and that all six immediately
  re-register the same config surface.
- Confirmed GMCM itself has no Dispose override, no Harmony reference and no
  owner-wide teardown path.
- Cross-checked current DTMAPI state, cleanup ordering, retryability, ConfigMenu
  stale-reference cleanup and restart enforcement.
- No build, unit test, game launch, runtime lock, save mutation or external
  package write was required. The comparison is source/IL audit evidence, not a
  runtime acceptance run.
