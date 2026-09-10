# 20260718-0002 SMAPI Version Capability And Batch 5 Route Review

Status: recorded

Date: 2026-07-18

Reviewed DTMAPI branch/HEAD: `codex/major-update-batch0-20260713` at pre-review commit `c93c460e`

SMAPI reference: `E:\Python_project\SMAPIlearning\SMAPI`, `develop` at `5689c8d6` (`4.5.2-54-g5689c8d6`), plus the representative tagged commits listed below

Real SMAPI Mod sample: `E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods`

Scope: compare representative SMAPI architecture generations, map the current DTMAPI by capability, classify gaps by release horizon, and revise the executable Batch 5 route without copying SMAPI implementation

Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)

Prior Batch 5 admission: [20260718-0001 Batch 4 G9 Reacceptance Review](20260718-0001-batch4-g9-reacceptance-review.md)

Frozen architecture audit: [20260712-0003 Full Boundary Audit](20260712-0003-dtmapi-full-boundary-audit.md)

Public API status: [Public API Matrix](../../../../api/public-api-matrix.md)

## Source Request And Evidence Boundary

The user asked for a source-backed comparison across several SMAPI generations, a capability-by-capability DTMAPI map instead of a statement that DTMAPI is “equivalent to SMAPI x.x”, a five-way gap classification, and an executable Batch 5 route. The focus is Mod discovery and dependency order, owner lifecycle and failure isolation, event demand and threading, content cache invalidation, cross-Mod APIs, manifest/content/config/i18n, save/title/world context, player diagnostics, update and ABI policy, Author SDK, performance, reflection, Harmony ownership, and unload boundaries.

This review is clean-room architecture research. SMAPI and third-party Stardew Valley Mods are read-only reference material. No SMAPI or third-party implementation was copied, merged, decompiled into DTMAPI source, or treated as permission to reproduce a DSL. The installed Mod evidence comes from manifests, configuration/i18n/content files and read-only assembly member-reference inspection; it proves packaged dependency surfaces, not that every path executed or that every sample passed a 4.5.2 runtime test.

The DTMAPI side uses current source, Catalog, API matrix, retained Review/Update/debug facts, first-party Mod source and test Mod source. QA-only code proves test coverage, not player demand. The eleven Catalog `PublishedProduct/PublicWorkshop` identities remain `RebuildBlocked`, so their code is valid consumer-shape evidence but not release-readiness evidence.

No Doloc Town install, shared Runtime, local official `MODS`, Workshop upload folder or game process was changed. No game smoke was run. Runtime lock acquisition was therefore not required.

The native-owner rule remains mandatory for every later API or domain change:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Verdict

The user's preliminary judgment is confirmed, with two important corrections.

First, DTMAPI is not uniformly “early”. Its install transaction, player Doctor, bounded support package, QA isolation, package/release contracts, native-evidence audit, owner-bound Core transaction, dependency cleanup and old-ABI gates are already mature. Some are stronger and more explicit than corresponding early or middle SMAPI-era facilities.

Second, the weak areas are not merely a shortage of public APIs. The central Batch 5 defect is that demand does not yet propagate through the whole runtime:

```text
event subscription / capability registration-or-lease-session-operation
    / content definition / mandatory repair / explicit QA
    -> demand ownership
    -> feature activation
    -> hook and updater ownership
    -> content generation and safe invalidation
    -> bounded diagnostics and measurable idle cost
```

Current DTMAPI has useful pieces at every layer, but not one authoritative chain. A zero-consumer runtime can still construct all GameBridge features, traverse the feature scheduler, publish recurring status, retain a shared Harmony owner, create event arguments, copy handler or entry collections, and, for CustomAnimals, build a filesystem signature before an effective pre-work throttle. That is the boundary Batch 5 must close.

Batch 4 remains accepted and Batch 5 may start. “Batch 5 前必须处理” below means the G0 contract and measurement gate before the first Batch 5 runtime mutation; it does not reopen Batch 4 or invent a new admission blocker.

Batch 5 should be defined as:

> 把已有能力变成需求驱动、变更驱动、生命周期明确、可观测且能证明静默的运行时；不是再增加一批公共事件、内容 DSL 或产品 API。

## Method: Compare Architecture Breakpoints, Not Release Numbers

The local SMAPI repository has 248 tags. Eight breakpoints were selected because their code and release notes show a change in product contract, not merely a patch release. The `SMAPI_version_study` copies of the 2.0 `ModResolver` and 3.0 `ManagedEvent` were hash-checked against the corresponding tag blobs; the review still uses the Git history as the authority.

| Breakpoint | Tag commit date / commit | Why it matters |
| --- | --- | --- |
| 1.15.4 | 2017-09-08 / `af1a2bde` | Last 1.x transition: stable required-dependency topology, cycles and minimum dependency versions, static multi-frequency events, per-subscriber exception capture and i18n. Its tree also contains 2.0 transition code, so optional dependency/content prototypes are not counted as stable 1.x product contracts. |
| 2.0 | 2017-10-14 / `79118316` | Formally productizes content edit/load/reload, input, semantic versions, optional dependencies and `UpdateKeys`; requires identity uniqueness and removes 1.x deprecated compatibility. |
| 2.11.3 | 2019-09-13 / `6521df7b` | Represents accumulated late 2.x: cross-Mod API, Content Packs, handler ownership, content coordination, data helpers, world/multiplayer tracking and the transition to per-helper events. |
| 3.0 | 2019-11-24 / `a3f21685` | Deletes static/deprecated event APIs, completes helper-owned events and frozen observation snapshots, separates early Mod load from `GameLaunched`, expands scanning/update recovery, and modernizes the author SDK. |
| 3.14.0 | 2022-05-01 / `c8ad50da` | Introduces request/invalidate/ready content events in parallel with the old editor/loader APIs, adds conflict ordering and better API proxies, and announces the next major replacement runway. The old APIs remain until 4.0. |
| 3.18.6 | 2023-10-05 / `59193372` | Mature pre-4.0 line: widespread `HasListeners` gating around EventArgs, selected snapshots/traversals and dispatch, plus load-order overrides and content allocation refinements. Watcher updates still run every tick, so this is not end-to-end producer or Hook demand activation. |
| 4.0.0 | 2024-03-19 / `8837303b` | Deletes the old content and other deprecated APIs, changes runtime/game integration, adds render-step/custom update-manifest support, and narrows upgrade guarantees. |
| 4.5.2 | 2026-03-14 / `821167e5` | Current stable layer: content integrity, malicious loose-file detection, configuration UX, automated/attested/publicly verifiable build work and continuing performance fixes. |

The date column is the Git tag commit date, not necessarily the public release date. For example, the 3.0 tag commit is dated 2019-11-24 while its release notes say 2019-11-26.

The key historical finding is that “modern SMAPI” is the result of several independently staged generations. Loader topology existed before a stable content pipeline; `GetApi<T>` arrived before the modern event system; broad listener-aware dispatch/argument gating came much later than handler ownership and still did not become end-to-end activation; compatibility governance combined long deprecation windows, proxy/rewriter work and major-version deletion. DTMAPI should therefore select the needed contracts, not imitate the end-state implementation or collapse all of them into Batch 5.

## What SMAPI Added, Refactored And Removed

| Capability | Confirmed evolution across the sampled code | Architectural lesson for DTMAPI |
| --- | --- | --- |
| Discovery, dependencies, versions and order | Stable 1.x contracts resolve required dependency topology, cycles and minimum versions; the 1.15.4 tree also contains 2.0 transition code. 2.0 formally adds optional dependencies and enforces manifest identity/uniqueness. 2.5 brings Content Packs into discovery. 2.8/3.0 improve nested/non-standard scanning and error classification. Explicit early/late load overrides are much later in 3.18. Supply-chain loose-file checks arrive in 4.5. | Do not equate a dependency sorter with the complete loader product. DTMAPI can preserve its stronger source-authority and transaction rules while adding only demand/capability edges now. |
| Per-Mod lifecycle and failure isolation | Subscriber exceptions are caught per handler and attributed to the source Mod. `ManagedEvent` adds cached handler ownership in 2.5. Save/title transitions change Context and raise lifecycle events; they do not unload every Mod. General `Mod.Dispose` occurs at SCore shutdown. No sampled version contains a universal “three errors then dynamically unload the whole loaded Mod” policy. | Keep DTMAPI's handler/feature quarantine explicit. Do not call a quarantined callback, dormant route, or next-start disable recommendation a successful assembly unload. |
| Event model, frequency and demand | 1.x offers static multi-rate ticks. 2.5 adds managed handler ownership and listener checks. 3.0 completes instance/helper events and frozen observation snapshots. Priority and safe add/remove behavior mature afterward. By 3.18, many EventArgs allocations, selected snapshots/traversals and final raises are gated by `HasListeners`, but core watcher update/snapshot/reset still runs each tick. | SMAPI itself does not supply the end-to-end producer/Hook demand chain required by Batch 5. DTMAPI should gate expensive work before it happens and define its own main-thread, phase, snapshot, removal and queue contract. |
| Content cache, invalidation and live refresh | 2.0 formalizes invalidation by asset key and generic type; 2.1 adds predicate invalidation. 2.6 refactors coordination. 2.7-3.0 progressively propagates changes into JSON, maps, NPC and native static caches. 3.14 introduces request/invalidate/ready alongside the old APIs, plus ordering, labels and operation caching; 4.0 removes the old generation. 4.x keeps fixing live-native propagation and integrity. | “Invalidate” is not just delete-a-file-index. The minimum useful DTMAPI flow is dirty generation, coalesced rebuild at a safe boundary, last-good publication and required propagation into already-live Doloc native state. |
| Cross-Mod API and compatibility | `GetApi<T>` begins in 2.3, Content Pack hosting in 2.5, and proxy shape support grows through 3.14 and later failure handling. Compatibility also uses a database, assembly rewriting, update metadata and a deprecation lifecycle. | DTMAPI's owner-aware registry is a strong start, but exact CLR type lookup is not a complete compatibility strategy. Explicit capability/session demand activation belongs in Batch 5; merely obtaining an API facade must remain passive. Version/capability negotiation belongs later. |
| Manifest, Content Pack, i18n and config | Dependencies and i18n exist by 1.14; version constraints by 1.15; semver/optional dependencies/UpdateKeys by 2.0; hosted Content Packs by 2.5; pack translation by 3.0; richer i18n/private assembly handling in 4.1; SMAPI exposes its own settings through GMCM integration in 4.5. | Avoid treating stored manifest fields as enforced behavior. DTMAPI must either implement, validate or clearly label `UpdateKeys`, `MinimumGameVersion` and ContentPack type semantics. |
| Save/title/world/multiplayer | Save/title events precede the later Context model. 2.6-2.10 add world flags, data, peer/message events and load stages. 3.x/4.x add reconnect, split-screen and per-screen propagation fixes. | DTMAPI needs a single-player lifecycle state machine now. Stardew peer, farmhand and split-screen contracts are not a prerequisite while Doloc Town has no corresponding native owner. |
| Logs, Doctor and player support | Crash-memory/recovery appears early; log parsing, save backup, JSON validation, save recovery, community update mapping, path anonymization, content integrity and supply-chain checks accumulate over many releases. | DTMAPI's local Doctor, bounded collection and receipt-oriented package audit are already product-grade strengths. Runtime Diagnostics should export activation/invalidation/queue facts; the support bundle may collect that bounded export, while offline Player Doctor reads static state or an explicitly persisted receipt rather than live process internals. |
| Updates, deprecation and old ABI | Mod update keys become product behavior in 2.0; 2.6 moves most update judgment into the Web API; 3.0 adds community-defined version mappings. Deprecations follow a long warning runway and are removed at major versions, for example 3.14 introduction, 3.18 pending removal and 4.0 deletion. Rewriters help old Mods but do not promise indefinite ABI compatibility. | Separate the 0.5.5 no-break ABI gate from a future compatibility service. Do not make inert metadata look like an update checker. |
| Author SDK and publishing | Build configuration is extracted from real Mod practice; analyzers, SDK-style projects, asset/PDB packaging and optional Harmony support accumulate through 2.6/3.0. Modern validation and supply-chain work are later layers. SMAPI's 4.5 attestation applies to SMAPI's own build and does not automatically attest third-party Mod packages. | DTMAPI Author SDK, deterministic packaging, Doctor and deploy transaction are already mature. Later SDK work should consume capability/deprecation metadata instead of blocking Batch 5 on new templates. |
| Performance, reflection, Harmony and unload | Reflection caching appears early. Native event tracking reduces some polling in 2.6. Event/content rewrites and dispatch/argument allocation gates land over several generations, but full producer activation does not. SMAPI can summarize Harmony ownership but does not own every third-party patch or unpatch every Mod at title. | Define DTMAPI-owned Hook routes precisely. A route that cannot be safely unpatched must use a measured dormant state or an explicit restart boundary, with honest diagnostics. |

## Real SMAPI Mod Consumer Evidence

The installed sample contains 16 third-party code Mods, two third-party Content Packs, and SMAPI's bundled Console Commands and Save Backup. The denominators below exclude the two bundled Mods. Assembly member references prove that a surface is packaged, not that an active configuration executed it. The sample is biased toward a related family of mature Mods, so counts are pressure evidence, not ecosystem prevalence estimates.

| Surface | Read-only packaged evidence | Consequence for DTMAPI |
| --- | --- | --- |
| High-frequency events | 13/16 code Mods reference `UpdateTicked` or `UpdateTicking`. 12/16 reference `GameLaunched`; 8/16 `SaveLoaded`; 4/16 `ReturnedToTitle`. | Tick events are real ecosystem infrastructure. They need cheap zero-listener behavior, owner attribution, safe removal and per-handler cost evidence. |
| Input | 11/16 reference `ButtonsChanged`; 10/16 reference `ButtonPressed`. | Input snapshots, UI/world scope and title/save boundaries are foundational, not one-product helpers. DTMAPI's owner-bound input work is a strong basis. |
| Content | 6/16 reference `AssetRequested`; Lookup Anything also observes `AssetsInvalidated`; none of the 16 references `AssetReady`. | Content request/invalidation is a real shared path, but the sample does not justify copying every modern SMAPI event into Batch 5. |
| Cross-Mod service | 14/16 consume `GetApi<T>`; Content Patcher, Automate, Chests Anywhere, GMCM and Data Layers provide APIs. | Optional integrations are far more common than manifest hard dependencies. Provider readiness, requester identity, failure/version semantics and lease lifetime need explicit contracts. |
| Config and translation | 14/16 reference config reads, 15/16 config writes, and 12/16 the translation helper; filesystem inspection finds an `i18n` directory in 11/16. Some configurations have dozens of top-level keys. | JSON read/write alone is insufficient for a mature ecosystem; migration, validation, menu integration and i18n diagnostics are later productization work. |
| World data and multiplayer | Two samples reference save/global data helpers; four reference multiplayer surfaces. | Persistent world semantics are real but less common than config. Multiplayer evidence is Stardew-specific and does not establish a Doloc requirement. |
| Reflection, Harmony and threads | 13/16 reference reflection helpers; only two reference Harmony. Automatic Gates packages `Task.Delay`, which alone does not prove background game-state access; bundled Save Backup provides the clearer `Task.Run` pure-file-I/O example. | A cached, diagnosable reflection boundary is valuable. Harmony remains an escape hatch, not the default authoring model. Standard callbacks should remain main-thread; background work must not touch Unity/world state. |

The most instructive patterns are lifecycle-shaped rather than API-count-shaped:

- Automatic Gates subscribes only to save/title at Entry, adds world/update/input/render handlers after a save loads, and removes them on return to title.
- Ladder Locator temporarily subscribes to a one-second event after a warp, removes it after the work completes, and adds/removes render handlers with its HUD state.
- GMCM creates a short five-tick subscription after `GameLaunched`, then removes it.
- Reusable overlay components in Lookup Anything, Automate, Content Patcher, Chests Anywhere, Fast Animations, Data Layers and Tractor attach multiple tick/render/input events while open and detach them in `Dispose`.

These are direct evidence for deterministic removal and world/UI-scoped subscription lifetimes. They strongly support frozen-dispatch snapshots and make first-listener/last-listener producer activation a useful DTMAPI design, but they do not uniquely prove that activation policy.

The two installed Content Packs demonstrate a separate author audience. A raw text scan of Seasonal Cute Characters found 38 `Include` key occurrences, 20 `Action: EditImage` occurrences, 63 `When` key occurrences and 61 configuration-schema keys; Canon-Friendly Dialogue Expansion has 41 `Include`, 76 `When` and 37 configuration-schema key occurrences. A `When` object may contain multiple semantic conditions, so these are complexity indicators rather than parsed/effective action counts. Both packs use a host-specific declarative format and permissive JSON features. They justify a stable future host/invalidation substrate, not copying Content Patcher's token names, condition DSL, asset namespace or implementation.

Content Patcher's own packaged references reinforce the substrate boundary: it obtains owned packs, uses pack JSON/file and pack-local content/translation helpers, supplies/edits requested assets, and explicitly invalidates cache entries as context changes. Seasonal also declares six optional dependencies. These are evidence for future host ownership, invalidation and pack error/dependency isolation, not permission to import its DSL.

Manifest and ABI evidence points in the same direction. All 18 sampled third-party packages have `UpdateKeys`, and 15/16 code Mods declare `MinimumApiVersion`. Their compiled SMAPI references span 4.0.8 through 4.5.2, so within-major binary compatibility is a real installed-ecosystem constraint. This sample does not establish 3.x compatibility or prove that every package actually ran successfully. By contrast, the 20 sampled DTMAPI first-party/test manifests contain zero `UpdateKeys` and zero general Content Packs; they also mix `Required` and `IsRequired`. DTMAPI should canonicalize its own contract before exposing an update ecosystem.

## Current DTMAPI First-Party And QA/Test Consumer Evidence

The current `first-party-mods` and `testmods` source shows that DTMAPI already has service-oriented consumer shapes. The corpus deliberately combines intended first-party products with QA/test Mods; it is implementation pressure evidence, not an external ecosystem adoption count or proof that every sample is shipped.

| Surface | Current local sample | Interpretation |
| --- | --- | --- |
| Cross-Mod API | 42 `GetApi<T>` occurrences across 17 source files. | Current first-party/test consumers are service-heavy. Provider availability and demand activation are current implementation problems, not hypothetical interface design. |
| Config / translation | Config reads in 15 source files; translation use in 14. | Basic helpers are adopted, but this does not prove full locale, validation or migration maturity. |
| Events | `UpdateTicked` in eight source files, one-second tick in two, `SaveLoaded` in 14, `ReturnedToTitle` in six. | Lifecycle events are widely adopted. Several high-frequency subscriptions are evidence-only or are active while the product is disabled. |
| Content/save/multiplayer | No shared content request/invalidation event consumer, no general save/global data helper consumer, no multiplayer surface. | The absence is a real platform gap for content, but does not justify adding Stardew multiplayer abstractions. |
| Ordinary Mod Harmony | No ordinary current Mod directly owns Harmony. | The GameBridge adapter boundary is working as intended and should remain the default. |

Concrete examples expose different demand classes:

- `ZoomMod` queries its hotkey from `UpdateTicked`; owner-bound `KeybindPressed` can remove the permanent tick demand.
- `AutoFishingMod` legitimately needs per-frame work while an automation session is enabled, but currently subscribes from Entry and only returns early when disabled. The session should acquire and release frame demand.
- `OneActionComplete`, `FishBreedingAssistant` and `DebugConsole` keep a tick subscription only to log one-time event evidence. That evidence belongs in QA, not a player product's recurring path.
- `ActionSpeedMod` polls each frame to decide whether configuration should be registered; a configuration apply/change boundary should own that transition.
- `AnimalHusbandryProgressMod` uses a tick for one-time evidence and a thirty-second log. Neither should force high-frequency platform work.
- `AutoHarvestMod` performs real one-second periodic behavior. It is a valid periodic consumer, and its first/last subscription should determine whether the producer does optional work.
- AutoFishing's session/input/animation leases and Zoom's camera lease already show a DTMAPI-specific strength: GameBridge services can know the requester and own cleanup instead of forcing every Mod to reflect or patch native state.

The installed SMAPI sample also shows why DTMAPI should not rely only on manifest dependencies. Fourteen of sixteen SMAPI code Mods use optional runtime APIs, while the sampled DTMAPI first-party/test consumers already concentrate on `GetApi<T>`. Batch 5 must separate passive API publication and facade lookup from explicit registration, lease, session or operation demand that activates expensive native work.

## DTMAPI Capability Map

The following is the requested capability mapping. The “position” column deliberately does not name an equivalent SMAPI version.

| Capability | Current DTMAPI fact | Capability position and gap |
| --- | --- | --- |
| Mod discovery, dependency resolution, version and order | Scans local, official `MODS` and Workshop roots; reconciles native subscription/enablement; selects source authority for duplicate IDs; resolves required/optional dependencies, minimum versions, cycles, topology and reverse disable cascades. | Strong loader foundation. Missing explicit load phase/order override, conflict/capability dependency and enforced game-version behavior; successful code assembly load remains restart-bound. |
| Per-Mod lifecycle, error isolation and automatic disable | Entry is transactional. Owner cleanup spans Events, Input, Config, API facade, Content, CustomEntity and GameBridge participants. High-frequency handlers can be quarantined after three consecutive failures. | Strong owner transaction and delegate isolation. No public Mod `Dispose/OnDisable` contract can reclaim arbitrary threads, static roots or self-owned patches. Quarantine is not whole-Mod unload. |
| Event bus, frequency, demand and threads | Standard runtime-thread boundary exists; safe lifecycle publications may queue, unsafe Update/Input cross-thread calls are rejected. Handlers are owner-attributed. | Safety skeleton is ahead of a naive loader, but demand/performance is incomplete: queue capacity/overflow is unspecified, dispatch copies handlers, zero-listener callers still construct/dispatch arguments, and listener transitions do not reach GameBridge producers. |
| GameBridge activation | Thirteen feature objects are constructed eagerly: five every-frame, three 250 ms and five lifecycle-only. The scheduler traverses the full list and records recurring results; feature contracts are descriptive metadata. | No authoritative consumer count, activation state, Hook requirement, lifecycle resource set or stop result. `requiresSave/requiresUi` is not an executable scheduling boundary. This is the core Batch 5 gap. |
| Content load, cache, invalidation and refresh | `ContentQueryService` builds a read-only index at startup/Workshop refresh. Audio has specialized per-owner generation/rejected-generation retention. CustomAnimals has specialized refresh but a parse failure can publish an empty replacement and clear current registrations; ContentQuery has no last-good generation. Resource generations already exist as diagnostics. | Useful discovery and specialized loaders, but no shared asset ownership, dirty-generation producer, atomic candidate/commit/reject rule, coalesced commit, invalidation event or live-native propagation contract. Content query is not a content pipeline. |
| Content hot-path cost | CustomAnimals refreshes from every frame and computes loaded-Mod/file timestamp signatures before the unchanged decision. Audio polls active entries through an entry snapshot every frame. `LoadedMods` itself returns a new array. | Source-proven recurring allocation/I/O pressure. It is not proof of ISSUE-010's Fatal GC cause, but it violates the desired zero-demand contract and is the first Batch 5 containment target. |
| API acquisition and cross-Mod services | Registry keys providers by owner and exact contract type, caches consumer/provider facades and invalidates them with owner cleanup. GameBridge APIs can know requester identity. | Owner semantics are strong. Missing service version/capability negotiation, declared optional integration, explicit capability/session activation behind passive facades and a documented session-vs-process API object lifetime. |
| Manifest / Content Pack | Manifest includes identity, entry, minimum API/game, dependencies and UpdateKeys. Current local manifests mix dependency `Required` and `IsRequired`. There is no general hosted Content Pack contract. | More than a basic manifest parser, but `UpdateKeys` has no checker, game minimum can only warn, and free-form Type does not enforce a host/format. Stored metadata must not be presented as an active guarantee. |
| Translation and config | Config is owner-bound, backs up malformed JSON and supports migrations; translation is used by current Mods. | Basic adopted capability. Locale mapping is effectively English vs Simplified Chinese, loaded once, without locale-change, plural/token or missing-key diagnostics. Config's documented atomic-write fallback must be reconciled with delete-then-move behavior before a strong guarantee. |
| Save/title/world/multiplayer | Save/load/title events exist; some products own specialized per-save sidecars and cleanup. | Lifecycle signals exist but remain Experimental and are not one authoritative state machine. No general save identity/data helper or world load-stage model. No reviewed Doloc multiplayer native owner currently exists, so this route does not introduce multiplayer; discovery of one reopens the decision. |
| Logs, Doctor, crash support | Bounded runtime log export, crash/native history collection, offline player Doctor, support bundle and receipt-based package audit already exist. | Mature strength. Runtime Diagnostics should own live demand, Hook, queue, generation and sampled/low-overhead timing state; the support bundle collects its bounded export. Player Doctor remains static unless an explicit bounded persistent receipt is designed. |
| Updates, deprecation and old ABI | Central versions, ABI harness, retained artifact, `Obsolete(false)`, frozen Fishing compatibility and disabled Lamp shell are enforced. | Strong local release gate; missing actual update checker, compatibility database and automated deprecation runway. All 20 sampled first-party/test manifests have no `UpdateKeys`, while the field exists in the model. |
| Author SDK, templates and publication | SDK supports create, validate, build, pack, deploy, update, withdraw and Doctor; Code Mods stay `netstandard2.0`; Content Packs reject DLL; packaging/deployment are transactional and deterministic. | Mature strength. Later productization should add capability/deprecation checks and compatibility reports, not redesign the Batch 5 runtime around SDK concerns. |
| Performance, reflection and diagnostics | Input registration and selected Fishing delegates have focused zero-allocation/caching evidence; scheduler and resource ledgers expose useful counters. | Local wins, not a platform budget. No whole-GameBridge zero-consumer allocation/I/O/reflection/Hook contract yet. Source pressure must not be mislabeled as GC causal proof. |
| Harmony ownership and unload | Production uses one GameBridge Harmony owner. A testable unpatch helper exists, but normal shutdown only clears selected global bridge/runtime pointers; multiple static patch callbacks remain. | Cannot deactivate by capability today. Batch 5 needs per-route ownership or a documented process-pinned dormant route. Unity Mono assembly unload is not promised. |

## Preliminary Judgment: Confirmed With Nuance

| Preliminary claim | Result |
| --- | --- |
| Install transaction, Doctor, QA isolation, release contract and native evidence are mature. | Confirmed. These are current DTMAPI strengths and should be preserved as Batch 5 gates. |
| Event model is close to an early stage. | Partly confirmed. Surface coverage and demand propagation are early, but owner attribution, main-thread boundary and handler quarantine are already more sophisticated. The missing layer is pre-work demand and lifecycle semantics, not basic `try/catch`. |
| Content pipeline and hot refresh are close to an early stage. | Confirmed for the shared public/platform pipeline. Specialized CustomAnimals/Audio loaders and query indexing exist, but they do not form request → cache → invalidate → reload → live-native propagation. |
| On-demand services are early. | Confirmed. Input watches and some service leases are good local patterns, but thirteen feature hosts and a shared Hook owner remain eager. |
| Cross-Mod API is early. | Corrected. Owner-aware registry/facade cleanup and requester-aware GameBridge services are substantial. Compatibility proxying, capability/version negotiation, declared optional integration and lazy activation are the gaps. |
| Internationalization is early. | Confirmed for product completeness, not adoption. Translation is already used by current Mods, but locale and diagnostic semantics are narrow. |
| Update checks and ecosystem compatibility governance are early. | Confirmed with nuance. Network/update/compatibility services are missing, while ABI retention and package governance are already strong. |

## Gap Classification

### Batch 5 前必须处理

These are G0 decisions and measurements that must be recorded before the first runtime edit. Completing this review opens that gate; implementation follows under the owning in-progress Update.

1. Freeze standard event semantics: main-thread publication, phase order, frozen handler snapshot, add/remove during dispatch, reentrancy, failure isolation, and whether public priority is deferred rather than silently introduced.
2. Freeze an extensible typed demand taxonomy with at least `EventSubscription`, `CapabilityRegistration/Lease/Session/Operation`, `ContentDefinition`, `MandatoryFrameworkRepair` and `ExplicitQa`. `GetApi<T>`/facade creation is passive and must not start an updater or Hook by itself.
3. Freeze lifecycle state separately from physical patch state and restart policy. At minimum distinguish `Cold`, `Activating`, `Active`, `Quiescing`, `Stopped`, `ProcessPinnedDormant` and `RestartRequired`: a dormant no-op patch does not automatically require restart, while a route that cannot roll back/rebind safely does.
4. Identify the authoritative content dirty producers and safe commit points. Filesystem timestamp polling is not an authoritative runtime change feed.
5. Capture a platform baseline before cadence changes: calls, event argument/snapshot allocation, native/reflection scans, file status/reads, Hook owner/patch count, active updater count, handler count and bounded queue depth. Measure timing overhead first, then select a zero/low-overhead aggregate, slow-call sample or explicit diagnostic mode instead of requiring a `Stopwatch` on every handler call.
6. Record mandatory exceptions. Title layout repair and Equipment orphan recovery must not be demand-disabled until their native owners, safety requirement and independent cadence are separated and proven.
7. Apply the native-owner method-body review clause before any API/domain redesign. A convenient Mod behavior, UI success, Registry entry or smoke helper cannot establish the public boundary.

### 0.5.5 发布前需要

1. Complete the Batch 5 event/demand/content/lifecycle/performance route and its source/unit/runtime gates.
2. Remove zero-listener argument construction and dispatch snapshots; use subscription-change snapshots and bounded cross-thread queue/coalescing/overflow diagnostics.
3. Replace per-frame CustomAnimals filesystem signatures and unnecessary Audio/LoadedMods entry snapshots with change/lifecycle-driven generations or a true pre-work throttle where no authoritative event exists.
4. Introduce the demand catalog/coordinator and migrate all eleven current Catalog products plus non-feature GameBridge recurring routes to explicit demand/mandatory/pinned/deferred decisions. Remove evidence-only permanent ticks from player products.
5. Separate optional Hook routes from base lifecycle Hooks. Physically unpatch only where rollback is proven; otherwise keep a measured stateless fast path and report `ProcessPinnedDormant`. Use `RestartRequired` only for operations or transitions that actually require process restart.
6. Add live demand, active updater, content generation, queue overflow, Hook owner and selected low-overhead timing facts to Runtime Diagnostics/report export. Let support bundles collect that bounded export; add only static or explicitly persisted summaries to Player Doctor.
7. Reconcile manifest promises: canonicalize `Required`/`IsRequired`, enforce or downgrade `MinimumGameVersion`, and either implement an offline/schema-only `UpdateKeys` contract or document the field as inactive until the update service exists.
8. Reconcile Config atomic-write documentation with the fallback implementation; preserve bad-JSON backup and migrations.
9. Keep the 0.5.5 ABI gate, player-readable missing/optional API diagnostics, exact five-DLL/no-QA package, eleven-product enable/disable matrix, and existing install/Doctor/release contracts.
10. Run the independent AutoFishing and ActionSpeed `1x -> enabled without acceleration -> common multiplier -> high multiplier -> disabled recovery -> title cycle` GC ladders. Batch 5 source shrink or a short smoke is not a substitute.

### Batch 6/7 产品化需要

1. Versioned/capability-aware cross-Mod services, provider readiness and optional-integration declarations; requester-aware policy where native ownership needs it.
2. A general save identity/per-save data helper and single-player world/load-stage context, based on Doloc native state holders.
3. Full i18n lifecycle: locale change, fallback chain, missing-key diagnostics, parameters/plurals where real author demand proves them.
4. Hosted Content Pack contracts, schema/version validation, pack error isolation and SDK generation. Doloc formats should be domain-specific rather than a Stardew asset clone.
5. General content request/edit ordering, conflict diagnostics and last-good propagation for reviewed Doloc domains. Public stability follows native-owner evidence per domain.
6. Mod update checking, compatibility advisory/database, deprecation warnings and a documented removal runway; small-version ABI regression matrices driven by real external consumers.
7. SDK capability/deprecation analysis, manifest canonicalization, author-facing lifecycle/performance templates and pre-publish validation integration.
8. Broader event/world/query surfaces only where migrated Mods and native-owner reviews prove shared demand.

### Batch 8 或独立项目

1. A Content Patcher-class declarative condition/token/edit framework. The shared invalidation substrate may be Batch 6/7, but the general DSL is a separate product.
2. General hot mutation across maps, sprites, audio, registries and already-instantiated native objects with conflict rollback.
3. True code-assembly isolation/unload or a separate host process. Unity Mono/BepInEx does not make this an ordinary in-process feature.
4. Cloud compatibility telemetry, large-scale compatibility database, remote log analysis or automatic dump symbolization.
5. An independent launcher/updater if product strategy later requires it; do not make it a hidden dependency of the in-game API.

### 不适用于 Doloc Town

1. Stardew split-screen `PerScreen<T>` and host/farmhand/peer authority are outside the current route because no matching Doloc native multiplayer owner has been reviewed. Discovery of such an owner reopens evaluation rather than making multiplayer permanently inapplicable.
2. XNA/MonoGame `ContentManager`, SpriteBatch render phases, Stardew asset names, map patch semantics or Content Patcher's exact DSL.
3. Stardew save-format repair and ErrorHandler behavior whose responsibility belongs to Stardew's native game version.
4. Binary compatibility with SMAPI or an assembly rewriter whose goal is to load Stardew Mods in Doloc Town.
5. A promise that DTMAPI owns, can remove or can diagnose every Harmony patch created by arbitrary third-party Mods.
6. “Unload all Mods when returning to title.” Title transition must release world/save/session roots; process-lifetime assemblies and declared services remain loaded unless a separately proven host boundary exists.

## Revised Executable Batch 5 Route

The route below preserves the admitted Batch 4 ordering constraints and keeps full content/productization work out of Batch 5.

### B5-G0 — Contract And Baseline Freeze

- Record event phase/snapshot/thread/failure semantics, the typed demand taxonomy, passive `GetApi<T>` lookup, content dirty causes, lifecycle/patch/restart outcomes and mandatory exceptions.
- Add or identify counters for zero-consumer cost before changing cadence.
- Inventory every GameBridge feature and every non-feature experimental/runtime route: updater, Hook, static callback, API provider, content definition, save/title root, pending operation and QA path.
- Preserve the current Batch 4 semantic/IL/meta-negative gates and exact package/product matrices as the baseline.

Exit: the implementation can answer who demands each capability, which work starts, what must stop, which physical patches remain `ProcessPinnedDormant`, and which named transitions are `RestartRequired`. This review is the initial G0 decision record; source instrumentation/baseline receipts still belong to the Update.

### B5-G1 — Hot-Path Containment And Content Generation

- Replace CustomAnimals per-frame filesystem signature construction with dirty generations driven by startup, Workshop/source refresh, owner activate/deactivate, Author Session explicit reload or a reviewed developer refresh command.
- Preserve Audio's current per-owner last-good behavior. Add atomic candidate/commit/reject semantics for CustomAnimals and ContentQuery before moving them onto the same owner/domain generation model.
- Remove avoidable `LoadedMods.ToArray`, audio entry snapshots and repeated collection projections from hot paths.
- Coalesce each dirty generation to at most one rebuild at the next safe runtime boundary; include owner/domain/reason/old generation/new generation in the receipt.

Exit: optional content domains perform zero file-state query/directory enumeration in a steady no-change frame, and a source change produces one bounded candidate/rebuild/commit-or-reject receipt while preserving current native behavior. Full reviewed live-native propagation belongs to G6.

### B5-G2 — Event Kernel And Lifecycle Phases

- Build immutable/copy-on-write handler snapshots when subscriptions change; do not create a new array on every dispatch.
- Add a zero-listener fast path before EventArgs creation and before any event-specific native/reflection state extraction.
- Expose first-listener/last-listener transitions internally to the demand coordinator.
- Bound the cross-thread queue; define coalescing, rejection and overflow diagnostics by event class.
- Record the real native phase table first, then freeze add/remove-during-dispatch, same-frame or next-safe-point visibility and reentrancy. Do not invent a simple total `Input → Update → Save/Title` order when native save/title callbacks may occur inside an update path.
- Keep per-handler isolation and owner attribution. Quarantine is a circuit breaker, not a claim that the Mod assembly unloaded; freeze whether recovery requires explicit resubscription, next start or permanent removal for that process, because current quarantine has no automatic recovery.

Exit: zero-listener tests allocate no EventArgs/snapshot and trigger no event-owned native scan; removal during dispatch has deterministic next-observation behavior; queue overflow cannot grow memory without bound.

### B5-G3 — Demand Catalog And Coordinator

- Separate passive provider publication from activation of Hook/update/native state work.
- Key demand by capability, requester owner, source type and lifetime. Count event subscriptions, explicit capability registrations/leases/sessions/operations, enabled content definitions, mandatory repairs and explicit QA independently. Passive provider/facade lookup is not demand.
- Track activation state, physical patch state and restart requirement as separate dimensions; include degraded/quarantined and `ProcessPinnedDormant` outcomes without conflating them with `RestartRequired`.
- Maintain an active updater set rather than traversing all lifecycle-only/optional feature hosts every frame.
- Publish bounded live demand receipts to Runtime Diagnostics/report export. Support bundles may collect them; Player Doctor sees them only through an explicitly bounded persisted receipt.

Exit: with no ordinary Mod and no optional content definition, only documented base lifecycle/mandatory repair work remains; activating one capability activates only its required updater/Hook routes.

### B5-G4 — Low-Risk Hook-Only Products

- Migrate FishRoe, Chest, StrongPlanting and ActionCompletion-class routes one at a time after their native owner and Hook rollback behavior are rechecked.
- Give each optional route a distinct owner or equivalent independently auditable route identity.
- Verify first-consumer install, repeated acquire idempotence, last-consumer dormancy/unpatch, title/save behavior, owner cleanup and next-start recovery.

Exit: each route passes zero-demand, one-consumer, multi-consumer, release, owner-disable and restart tests without changing public gameplay semantics.

### B5-G5 — Camera And AnimalViewer

- Reuse the proven Camera requester/lease model while separating provider availability from live camera application.
- Use two-level AnimalViewer demand: at least one enabled/configured owner permits a lightweight session-detection Hook; opening a viewer session activates clone/render/update work; native close releases those session roots; the last configured owner removes the detection Hook or leaves a proven `ProcessPinnedDormant` no-op route. Explicit QA remains a separate demand source.
- Do not reintroduce production-owned screenshot/evidence policy.

Exit: title/save/viewer transitions restore native state, release transient roots and preserve ordinary no-QA behavior.

### B5-G6 — Audio And CustomAnimals

- Move both domains fully onto the generation/last-good substrate established at G1.
- Separate schema definition demand, local file load state, live native object propagation and recovery.
- If background pure-file work is introduced, bound it and commit only on the game main thread; small change-driven transactions may remain on the main thread. No background Unity/native access is allowed.
- Prove disabled/no-definition cost, invalid generation rollback, language/config/source refresh and title/save cleanup.

Exit: no per-frame filesystem signature exists, a failed generation preserves last-good behavior, and live native objects either receive the reviewed update or report the named transition that is `RestartRequired`.

### B5-G7 — Remaining Runtime Routes, Mandatory Lanes And Release Evidence

- Classify and migrate every route not closed by G4-G6, including ActionSpeed, AutoFishing/Fishing compatibility and primitives, SaveSlots/MoreSaves, CropHarvesting, DebugConsole/Creative/global input Hooks, optional Equipment UI versus mandatory orphan recovery, and the Machine/Movement/Workshop pending-operation paths outside the thirteen feature contracts.
- For each route record exactly one current outcome: demand-activated, mandatory with measured cadence, stopped with removable patch, `ProcessPinnedDormant`, `RestartRequired` for a named transition, or explicitly deferred/removed from 0.5.5.
- Isolate title layout repair and Equipment orphan recovery from optional demand. Measure their cadence and prove why they remain mandatory before optimizing them.
- Add platform-level no-consumer, single-feature and disable/recovery performance profiles.
- Re-run schema-5/meta-negative/Catalog, source/unit/Release, staged QA, ordinary no-QA, exact eleven-product enabled/disabled, receipt-bound five-DLL package and shared-runtime game evidence using the third save and runtime lock.
- Keep ISSUE-010/011 wording and the independent AutoFishing/ActionSpeed GC ladders honest.

Exit: Batch 5 is complete only when the runtime can prove both active correctness and inactive silence. A smaller source tree, fewer calls in a unit test or a minute-scale game smoke is insufficient.

## Acceptance Budget And Receipts

| Boundary | Minimum Batch 5 acceptance |
| --- | --- |
| No optional demand | A warmed 10,000-frame profile performs zero optional feature file-status calls, directory enumeration, per-feature `ToArray`, reflection object search and optional native updater invocation. Only documented base lifecycle and mandatory repair cadence may remain. |
| Event zero-listener path | No EventArgs or handler-snapshot allocation and no event-attributable native/reflection state extraction. Subscription snapshots rebuild only on add/remove. |
| Event dispatch | Per owner/handler call and failure/quarantine counts are bounded/reportable. Timing uses the G0-proven low-overhead aggregate, slow-call sampling or explicit diagnostic mode; it does not impose unmeasured per-call stopwatch cost. One failed handler does not block later handlers. |
| Cross-thread event source | Each event class is main-thread direct, bounded/coalesced queue, or rejected. Queue capacity, overflow count and recovery are tested. No queued callback touches Unity before main-thread commit. |
| Content generation | One dirty generation creates at most one rebuild at the next safe point; multiple reasons may coalesce into that receipt. Receipts include owner/domain/reasons and old/new/last-good generation. No-change frames perform no filesystem signature work. |
| Demand activation | One capability activates only its dependency closure. Last release clears active updater, session, lease and save/title roots. Passive API lookup does not activate native work. Lifecycle, patch presence and restart need are reported independently. |
| Hook ownership | Base lifecycle and optional capability routes are distinguishable. Install is idempotent. Shutdown/disable reports remaining owned routes and callback roots; zero is required where rollback is claimed. A retained stateless no-op route is `ProcessPinnedDormant`, not automatically `RestartRequired`. |
| Diagnostics | Status publication itself has a measured/bounded cadence and does not rebuild detailed strings/snapshots each frame. Runtime Diagnostics/report export contains the bounded live demand/invalidation/queue/Hook/performance summary; support bundles collect it, while offline Doctor consumes static facts or an explicit persisted receipt only. |
| Regression | Exact five Runtime DLLs/no-QA package, staged QA positive paths, ordinary no-QA UI behavior, exact eleven-product enabled/disabled combinations, source semantic gates and owner cleanup remain green. |
| GC claims | Allocation/call/root reductions are reported as such. They are not called a fix for Fatal GC without the separate long/active gameplay evidence and native-object observations required by ISSUE-010/011. No forced GC is introduced as a fix. |

## Roadmap Decision

Proceed with Batch 5 under Update `20260718-0003`, beginning with G0 measurement/inventory and G1 content hot-path containment. Do not expand the batch into a public Content Patcher clone, multiplayer, general save-data ecosystem, compatibility database, network update service or dynamic assembly host.

The design should take lessons from SMAPI's history but remain DTMAPI-native:

- use Doloc native responsibility methods and state holders as the bottom boundary;
- keep fragile reflection/Harmony inside `DTMAPI.GameBridge.DolocTown`;
- make Core own stable demand/lifecycle/content-generation contracts;
- make ordinary Mods acquire typed services/leases instead of patching the game;
- preserve restart-required boundaries wherever complete rollback cannot be proven;
- promote public APIs only after native owner, GameBridge adapter and external consumer evidence agree.

## SMAPI Source Evidence Index

Paths below are relative to `E:\Python_project\SMAPIlearning\SMAPI` at the named tag. They are evidence anchors, not source to copy.

| Finding | Tag / source anchor |
| --- | --- |
| Required dependency topology, minimum versions, static tick families and per-subscriber safe raise | `1.15.4:src/StardewModdingAPI/Framework/ModLoading/ModResolver.cs`; `1.15.4:src/StardewModdingAPI/Events/GameEvents.cs`; `1.15.4:src/StardewModdingAPI/Framework/InternalExtensions.cs` |
| 2.0 content helper and formal product contract | `2.0:src/SMAPI/IContentHelper.cs`; `2.0:docs/release-notes.md` |
| Predicate invalidation after 2.0 | commit `08c30eef` (2.1 line) |
| Managed handler ownership, cached snapshot and per-handler isolation | `2.5.5:src/SMAPI/Framework/Events/ManagedEvent.cs`; `2.5.5:src/SMAPI/Framework/Events/ManagedEventBase.cs` |
| Specialized asynchronous/thread-safety caveat | `2.11.3:src/SMAPI/Events/ISpecialisedEvents.cs` |
| Static-event removal, load/GameLaunched split, SDK and update mapping generation | `3.0:docs/release-notes.md` |
| New content events introduced alongside old APIs | `3.14.0:docs/release-notes.md`; `3.14.0:src/SMAPI/Events/AssetRequestedEventArgs.cs` |
| Dispatch/argument gates but unconditional watcher update/snapshot/reset | `3.18.6:src/SMAPI/Framework/SCore.cs`, especially the tick watcher path around lines 805-807 and the surrounding `HasListeners` call sites |
| Modern resolver/supply-chain and release facts | `4.5.2:src/SMAPI/Framework/ModLoading/ModResolver.cs`; `4.5.2:docs/release-notes.md`; malicious loose-file resolver change `5c3237bd` |

The corresponding DTMAPI anchors are the current `DtmApiRuntime`, `EventManager`, `RegistryAndHelpers`, `ContentQueryService`, GameBridge feature scheduler, Audio/CustomAnimals services, `HarmonyReflectionPatcher`, Catalog, public API matrix and first-party/test consumer sources summarized in the capability table. Exact method/line references should be refreshed in each implementation-stage Review because Batch 5 will deliberately move those boundaries.

## Limitations

- The installed SMAPI Mod sample is not representative enough to turn its percentages into ecosystem prevalence.
- Assembly member references prove a packaged dependency path, not that the path executes under the installed configuration.
- Stardew Valley was not launched; compatibility and performance of the sample were not runtime-tested.
- Only two real Content Packs were inspected; they do not determine the complete future DTMAPI authoring language.
- No real old 2.x/3.x Mod sample was used, so the review does not set a DTMAPI ABI support duration from those packages.
- The negative finding about universal SMAPI auto-disable/unload is strong for the sampled `ManagedEvent`, `Mod` and shutdown paths, but it does not prove that no bundled or third-party private policy ever existed.
- The standard-SMAPI-event main-thread conclusion comes from sampled `SCore`/game call sites, not a global type-system guarantee for every event or future version. DTMAPI must state its own contract explicitly.
- Source-proven DTMAPI hot-path pressure establishes a performance boundary violation, not the cause of a native Fatal GC crash.
