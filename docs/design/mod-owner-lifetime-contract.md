# Mod Owner Lifetime Contract

Status: active internal contract; introduced for `0.5.3-alpha` and still authoritative for the current `0.5.5` source.

## Authority

Every discovered ordinary owner has at most one Core-owned lifecycle state at a time. Successful Mono assembly load is recorded separately as an irreversible process fact:

```text
Entering -> Active -> Deactivating -> InactiveRestartRequired
                                      \-> Shutdown
                                      \-> [state removed: no assembly + zero roots]

InactiveRestartRequired --cleanup retry--> Deactivating
    -> InactiveRestartRequired  (assembly loaded or roots still not proven zero)
    -> [state removed]           (no assembly ever loaded and all roots now zero)

Shutdown --cleanup retry--> Deactivating --complete--> Shutdown
```

`Entry` and every DTMAPI-owned registration it performs belong to the same owner transaction. Core publishes loaded-registry, instance, and loaded-list state only after `Entry` returns. A failure at any later publication checkpoint rolls all three back through the same deactivation path. Rollback state is permanent for the process only after `Assembly.LoadFrom` succeeds or while cleanup cannot prove zero roots.

## Owner-Bound Roots

The helper owner is canonical for Event, Input, API, Config, Config migration, ConfigPage, Content publication, CustomEntity, resource-ledger, and GameBridge owner state. Public compatibility methods which still accept `IManifest` must validate that `UniqueID` equals the helper owner and then use the canonical manifest.

- duplicate `(provider owner, API contract)` registration throws and preserves the first object;
- duplicate ConfigPage and migration registration throws;
- owner-scoped API facades are stable while consumer and provider are active;
- stale helper proxies/facades reject new registration after either relevant owner is inactive;
- cleanup removes consumer/provider facade-cache entries and reports removed and remaining root counts.
- discovered DTMAPI Content is not an active root: assets and indexed items publish only from the authoritative loaded-owner set after activation; disabled, unknown and restart-required sources retain package-level Mod diagnostics but are not parsed into item-level all-content rows.

## Process-Lifetime Providers

A process-lifetime provider publishes one canonical manifest and one or more API contracts through an atomic internal registry operation.

- The first contract commits the canonical loaded manifest and API together. If API/factory registration fails, the newly added manifest is rolled back.
- A later distinct contract may reuse the same canonical manifest. Its failure preserves the manifest and every earlier contract.
- Duplicate `(provider owner, API contract)` registration throws without replacement.
- A manifest with the same owner ID but different name, author, version, or type is not canonical and must be rejected before changing any contract.
- Every successfully published provider ID, including a dynamic/non-hard-coded ID, is reserved against ordinary discovery before ordinary assembly loading. A source collision cannot replace, share, or clean the platform provider.
- The reverse collision is also invalid: a late process-lifetime registration cannot claim an ID while the authoritative loaded-registry row belongs to an ordinary source owner.
- Provider dependency availability and version come from that canonical authoritative registry row; ordinary discovery is not an additional authority for process-lifetime providers.

## Deactivation

Entry failure, source removal, official disable, required-dependency invalidation, explicit removal, and shutdown use one idempotent cleanup path. Required dependents deactivate in reverse load order. Optional dependency changes warn but do not deactivate the consumer.

Dependency reconciliation has two authorities:

- process-lifetime platform providers published through Core `RegisterRuntimeApi` are available only when their authoritative registry row remains present and version-compatible; they intentionally do not require an ordinary discovery row;
- source-managed ordinary providers require a loaded registry row plus the current discovered/enabled source. The loaded registry manifest is the process-resident version authority; a refreshed disk manifest can signal an update but can never advertise a newer resident DLL/API. Removal, disable, incompatible loaded version, or a change of `Source + OfficialId + RootPath + manifest Version` deactivates affected owners in reverse dependency/load order.

An unchanged refresh never deactivates an owner merely because its Core, ConfigMenu, GameBridge, or Bootstrap-host provider is registry-only. A loaded code owner's source-identity or in-place manifest-version switch cannot reuse the old assembly/root graph in-process; it enters update-pending/source-handoff deactivation and then `InactiveRestartRequired`, and the newly selected version loads only in a new process. New consumers are checked against the loaded registry version before Entry. A no-assembly/non-code source switch follows the zero-root re-publication rule below.

Successful `Assembly.LoadFrom` is the permanent same-process restart boundary. After that success, any non-shutdown deactivation remains `InactiveRestartRequired`; same-process re-enable never calls `Entry` again even when all DTMAPI-owned roots are zero. DTMAPI does not guess how to unload assemblies, reverse third-party Harmony patches, clear third-party statics, or destroy unknown Unity/native objects.

A non-code owner, missing DLL, or failed `Assembly.LoadFrom` has not crossed that boundary. If Core and every cleanup participant report zero remaining roots and zero failures, Core removes the lifecycle state and a corrected/re-enabled source may publish in the same process. If any root or cleanup failure remains, the owner stays inactive and cleanup may retry; only a later zero-root proof removes the state. No owner may enter while cleanup is incomplete. `Shutdown` remains terminal for actual process shutdown.

## Save And Title Boundaries

`SaveLoaded` and `ReturnedToTitle` clear only save/title transient state, including Core down/pressed/released/suppression state and demand-local Input snapshot watches. They do not remove process-lifetime Event, persistent Input registrations, API, ConfigPage, migration, or Camera lease services belonging to an active Mod. Camera restores the title/native transient application without rewriting the retained lease request, and reapplies that request when a playable save camera returns. Owner deactivation is the only ordinary-Mod boundary which removes those roots.

If a known native reset fails (for example Camera orthographic size, movement scale, or creative debug flags), cleanup must retain a bounded owner root/tombstone and report nonzero `remaining`. A later cleanup pass may retry the native restore, but it must never re-enter the Mod. Missing/destroyed native objects may be classified safe only when they cannot still retain the owner's write.

## Diagnostics Retention

Diagnostics must not become a second owner graph:

- logging, host sinks, and diagnostic callbacks are observation-only; each sink is isolated and no sink failure may block publication, reconciliation, deactivation, cleanup retry, or shutdown;
- quarantined high-frequency delegates are released immediately;
- quarantine metadata retains at most 128 named owners per event; overflow increments a historical `trimmed` scalar and never creates `other` or contributes to current roots;
- owner registrations/cleanups use fixed-kind counters, not successful per-object history;
- config preview aggregates retain counts/timestamps only; arbitrary detail exists only in the shared bounded recent-failure window, and aggregate `LastDetails` remains empty;
- recent scalar owner/config-preview failures share a 64-entry window;
- each monitor retains at most 256 `LogOnce` keys;
- hook, feature, and diagnostic aggregate dictionaries have fixed caps and stable `other/trimmed` counters.
- every retained caller-controlled scalar is sanitized before dictionary-key construction or storage: owner IDs retain at most 160, identifiers 192, short text 256, messages 512, and details 2048 UTF-16 code units;
- truncation appends a streaming SHA-256 hash, caches no caller value, and increments a saturating net-omitted UTF-16 `trimmedBytes` counter in Diagnostics and the owner ledger; hashing occurs before registry/ledger locks are acquired.
- diagnostic DTOs defensively sanitize direct construction, snapshots copy supplied interface rows into bounded scalar models with fixed row caps, and caller overflow buckets are stored separately from ordinary Hook/feature/aggregate keys;
- evidence/runtime-context text retains at most 262144 UTF-16 code units and an exported report summary at most 524288; paths and installer-state scalar fields use the same hash-suffixed truncation and same-export byte accounting.

Current root counts come from authoritative registries, not diagnostic mirrors or historical trimmed counters. Diagnostic publication failure never changes an authoritative lifecycle decision.
