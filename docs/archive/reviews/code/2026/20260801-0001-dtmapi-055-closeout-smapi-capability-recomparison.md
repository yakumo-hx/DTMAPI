# DTMAPI 0.5.5 收尾后与 SMAPI 的能力复比审计

- Review ID: `20260801-0001`
- Date: `2026-08-01`
- Status: `recorded — 0.5.5 closed baseline; Batch 6/7 route revised`
- Scope: final DTMAPI 0.5.5 runtime/product capability, sampled SMAPI version evolution, real SMAPI Mod and final DTMAPI product consumers, Batch 6 prerequisites and later ecosystem route
- Change type: audit-only Review; no Runtime, API, package, Workshop, save or game mutation
- Supersedes only the current-status conclusions in [20260718-0002 SMAPI Version Capability And Batch 5 Route Review](20260718-0002-smapi-version-capability-batch5-route-review.md); that Review remains historical evidence for the pre-Batch-5 tree

## Source Request And Corrected Baseline

The user asked to compare SMAPI again from the position of the completed DTMAPI
0.5.5 release. This changes the audit question:

- 0.5.5 is a completed baseline, not a candidate whose release blockers must be
  rediscovered;
- completed Batch 5 work is judged as current capability, not retained as an
  open route;
- remaining differences are routed only to a Batch 6 prerequisite, Batch 6/7
  productization, Batch 8 or an independent project, or not applicable to Doloc
  Town;
- SMAPI remains architectural and ecosystem evidence. Its API shapes and source
  implementation are not DTMAPI requirements and must not be copied.

### Audited authorities

| Authority | Exact audit baseline |
| --- | --- |
| DTMAPI repository | `2bda4a60` (`release(workshop): record normalized 0.5.5 artifact`) |
| Final player-tested Runtime/product source | `d389da0fe89b38fcc0257fb5213153c9c123cb32`; retained candidate `20260801-d389da0f-manual-pass` |
| DTMAPI version identities | release/API `0.5.5`; binary/file `0.5.5.0`; intentionally retained assembly identity `0.5.3.0` |
| DTMAPI release facts | [0.5.5 与功能 Mod 上传目录发布收口](../../../updates/2026/20260801-0002-workshop-upload-release-closeout.md) and [Runtime 0.5.5 实际发布元数据权威](../../../updates/2026/20260801-0003-runtime-published-metadata-authority.md) |
| DTMAPI identity and API contracts | [Batch 6 Managed Mod Identity Contract](../../../../architecture/batch6-managed-mod-identity-contract.md), [Public API Matrix](../../../../api/public-api-matrix.md), Product Catalog |
| SMAPI repository | `E:\Python_project\SMAPIlearning\SMAPI`, `develop` `5689c8d6aeecf54f670559ffaaed6684a5febc25`, `4.5.2-54-g5689c8d6` |
| SMAPI version sample | `1.15.4`, `2.0`, `2.11.3`, `3.0`, `3.14.0`, `3.18.6`, `4.0.0`, `4.5.2`, current `develop` |
| Real SMAPI Mod sample | `E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods`: 18 code DLLs and two Content Patcher packs |
| Real DTMAPI product sample | final candidate's 11 product DLLs: nine Advanced 1.0.0 products, retained legacy Manbo, and retained Workshop MoreEquipmentSlots `0.3.1-dtmapi` |

The comparison is source, manifest, PE metadata, package and retained-evidence
based. Doloc Town and Stardew Valley were not launched. A MemberRef proves that a
compiled package consumes a surface; it does not prove that every path executed
during a particular play session.

## Executive Verdict

DTMAPI 0.5.5 is no longer accurately described as an early Mod Loader.

It is a narrow, contract-heavy managed Mod platform with a mature player and
first-party product lane:

- source-aware discovery, dependency topology, exact package identity and
  transactional deployment;
- owner-bound Events, Input, Config, API facades, Content publications and
  native cleanup;
- bounded event queues, immutable publication membership, per-handler failure
  isolation and real producer-demand routing;
- exact Advanced ProductNative admission, canonical Harmony ownership,
  restart-required truth and frozen old-ABI compatibility;
- Player Doctor, support collection, QA isolation, release receipts and exact
  published-artifact governance.

Its remaining distance from modern SMAPI is mainly ecosystem breadth, not loader
fundamentals:

- no general content request/edit/invalidate/propagate pipeline;
- no usable `ContentPackFor`/Content Host author relationship;
- no general Mod/save/global/session data helper or broad world context;
- no product-to-product service network, service capability/version negotiation
  or author command/reflection helper;
- only English/Simplified Chinese immutable translation snapshots;
- inactive `UpdateKeys`, no update/compatibility service, and no ecosystem-scale
  deprecation/advisory database;
- no generally available Advanced author lane.

There is therefore no honest sentence of the form “DTMAPI 0.5.5 equals SMAPI
version X”. Along some axes DTMAPI 0.5.5 exceeds modern SMAPI's demonstrated
contract precision; along others it has not yet reached capabilities that SMAPI
already productized in 2.x.

| Axis | Final 0.5.5 position relative to sampled SMAPI |
| --- | --- |
| Installation, source authority, Doctor, evidence and exact release contracts | DTMAPI-specific strength; not meaningfully represented by one old SMAPI version |
| Event kernel and end-to-end optional-work demand | DTMAPI is at least modern in its bounded semantics and is stronger than sampled SMAPI in connecting first/last demand to selected native updater/Hook work |
| Managed ProductNative owner cleanup | DTMAPI is stronger for its exact admitted products; this does not generalize to arbitrary third-party Advanced Mods |
| Loader dependency breadth | Solid required/optional minimum-version topology, but below modern SMAPI in version semantics, player order overrides, update integration and ecosystem compatibility policy |
| Content ecosystem | Below SMAPI 2.0/2.5 capability breadth and far below the 3.14+ request/invalidation generation; DTMAPI's internal atomic generation substrate is not a public content pipeline |
| Cross-Mod API | Strong owner/facade cleanup, but ecosystem adoption and compatibility proxying are much shallower than modern SMAPI |
| Config and adopted localization | Mature for the current bilingual first-party product set; narrow for a multilingual external ecosystem |
| Save/world/multiplayer surface | Strong save-commit governance but narrow public service/context coverage; Stardew multiplayer is not a Doloc requirement |
| ABI and compatibility | Strong exact local ABI retention; lacks the update, compatibility-database and published deprecation ecosystem around it |
| Performance boundary | Real no-demand silence and focused zero-allocation paths are proven; quantified whole-runtime/per-unit memory and allocation budgets are not established |

## SMAPI Version Evolution: Additions, Refactors And Removals

The local SMAPI `develop` commit is unchanged from the July comparison, so the
architectural breakpoints remain stable. The new work is principally the DTMAPI
0.5.5 remapping below.

| Version | Confirmed additions, refactors and removals |
| --- | --- |
| `1.15.4` (`af1a2bde`, 2017-09-08) | Required dependency topology, cycle and minimum-version checks, static multi-frequency events, per-delegate exception containment, translation and reflection caching already exist. Failure logging is generic at this tag; source-Mod owner attribution arrives with later managed events. The tree includes 2.0 transition code, so optional dependencies/new content surfaces in this checkout must not be mislabeled as stable 1.x contracts. |
| `2.0` (`79118316`, 2017-10-14) | Productizes semantic versions, optional dependencies, `UpdateKeys`/update checks, unified input and asset load/edit/invalidation. Enforces complete/unique manifest identity. Removes deprecated 1.x APIs, identityless/duplicate compatibility and Mod reflection into SMAPI internals. |
| `2.11.3` (`6521df7b`, 2019-09-13) | Represents late 2.x accumulation: `GetApi<T>`, owned Content Packs, source-owned `ManagedEvent`, content/input/world tracking refactors, SDK analysis, save backup, web update judgment, native content propagation, nested discovery, save/global data, multiplayer and richer load stages/context. |
| `3.0` (`a3f21685`, 2019-11-24) | Removes static events and the accumulated deprecated surface, completes helper-owned events, separates early Mod entry from `GameLaunched`, freezes observation snapshots, improves scanning/update mappings and modernizes SDK-style/Harmony-capable builds. |
| `3.14.0` (`c8ad50da`, 2022-05-01) | Adds `AssetRequested`, `AssetsInvalidated` and `AssetReady`, edit/load priority and conflict handling, labels and operation caching. Runs this generation beside old loaders/editors as a deprecation runway; expands interface proxy support. |
| `3.18.6` (`59193372`, 2023-10-05) | Accumulates listener-aware EventArgs/final-raise gates, priority and hot-path work; provider calls can receive requester identity; player configuration gains early/late load overrides; deprecated APIs reach pending removal. It still updates watcher state every tick and is not an end-to-end producer activation system. |
| `4.0.0` (`8837303b`, 2024-03-19) | Removes the old content API and other deprecated APIs, keeping the 3.14 event generation; migrates to .NET 6/Stardew 1.6, adds render-step and custom update-manifest support, strengthens rewriting, removes SMAPI ErrorHandler after native ownership changes, and ends seamless upgrades from 2.11.3 or earlier. |
| `4.5.2` (`821167e5`, 2026-03-14) and current `develop` | 4.x accumulates integrity/path anonymization/i18n directory, Git-managed compatibility lists, malicious-Mod and namespaced-ID lookup, content propagation/input fixes, GMCM integration, loose-file checks and automated/attested build provenance. 4.5.2 is mainly a fix release; the following 54 develop commits emphasize input/event/world watcher/inventory allocations, diagnostics and open ecosystem data, not a new in-game Mod-facing Runtime subsystem. |

### Corrections to the July SMAPI Review

1. Private Assemblies are not a current 4.5 capability. SMAPI 4.1.0 added the
   manifest feature, but 4.1.4 removed it in `4af917335b424a77edd33aa132aad55dd7979233`.
2. The 4.5 product line has automated and publicly verifiable build-provenance
   work, but the 4.5.2 tag
   release notes did not originally contain the attestation link. The link was
   added after the tag in `4d44e661381296a4e2d0a84b6dc18b042f6cff8d`.

These corrections matter because DTMAPI should compare current capability, not
temporarily introduced or post-tag documentation state.

## What Changed Since The Pre-Batch-5 Comparison

| July finding | Final 0.5.5 result |
| --- | --- |
| Event mutation/publication semantics were incomplete | Closed. Subscription mutation is owner-bound and Runtime-thread-only; publication captures immutable membership; cleanup/quarantine can cancel a captured registration; add/remove affects the next publication. |
| Zero-listener work and queue policy were unspecified | Closed. Zero-listener publication bypasses EventArgs/snapshot creation; a bounded 512-entry queue has distinct FIFO, latest-by-key coalescing and rejection policies with overflow diagnostics. |
| Listener state did not activate/deactivate native producers | Closed for audited routes. The demand coordinator connects event/operation/session demand to updater and Hook closure; passive `GetApi<T>` remains inactive. |
| CustomAnimals/Audio/ContentQuery did recurring scans without one transaction model | Closed at the internal substrate level. Dirty generations, candidate preparation, atomic commit, last-good retention and requeue are implemented and tested. A public content pipeline is still absent. |
| GameBridge optional work was eager | Closed at the accepted no-demand boundary. Real Unity evidence covers 300 warm-up plus 10,000 measured frames with 18 optional-work metrics at zero delta. Process-pinned dormant is still resident and is not physical unload. |
| Config atomicity documentation exceeded implementation | Closed. Writes use temporary-file plus atomic replacement and no delete-then-move fallback; bad-JSON backup and migrations remain. |
| Manifest dependency spelling and inactive fields were ambiguous | Closed for the 0.5.5 wire contract: `Required` is canonical, conflict/miscasing is rejected, and `UpdateKeys` is explicitly schema-only. `MinimumGameVersion` still warns because the host lacks a version owner. |
| Product behavior remained embedded in mandatory GameBridge | Closed for the admitted migrations. Nine final Advanced products privately own ProductNative behavior; frozen legacy executors are demand-inactive in Compatibility, and `IItemDisplayNameApi` is the only two-consumer SharedNative gameplay adapter. |
| Mod lifecycle had only platform-root cleanup | Substantially closed for admitted products. Core retains the instance, invokes optional `IDisposable`, runs owner participants, reports remaining roots and requires restart after assembly load. `DtmMod` still does not publish a typed deactivation/reason contract to general authors. |
| Old Mod ABI was a release aspiration | Closed for 0.5.5's bounded set. Public ABI has zero removals, exact retained consumer references resolve, and the dormant Compatibility Host activates only on a frozen ABI call. |
| Batch 5 evidence was not tied to one retained performance authority | Closed structurally. The Catalog owns exact no-demand/active-product receipts and truthfully records that a quantified memory/allocation budget remains unestablished. |

The last row is an accepted 0.5.5 boundary, not an unfinished 0.5.5 release
gate. It becomes a prerequisite only before broadening recurring platform work
or making numeric performance promises.

## Real Mod Comparison

### Sample counts

The SMAPI denominator below is all 18 code DLLs in the installed sample,
including bundled Console Commands and Save Backup. The DTMAPI denominator is
the 11 product DLLs in the exact final player candidate: nine Advanced products,
legacy Manbo and retained MoreEquipmentSlots. The samples are intentionally not
claimed as ecosystem prevalence statistics.

| Packaged capability signal | SMAPI sample: 18 code DLLs | Final DTMAPI sample: 11 product DLLs |
| --- | ---: | ---: |
| Uses events | 17 | 10 |
| Uses `UpdateTicked` | 13 | 5 |
| Uses a content API | 9 | 1, DebugConsole read-only query only |
| Consumes `GetApi` | 14 | 11 |
| Provides its own cross-Mod API | 5 | 0 |
| Reads configuration | 14; 15 read or write | 10 |
| Uses translation helper | 12 | 10 |
| Uses data helper | 7 | 0 |
| Uses multiplayer surface | 4 | 0 |
| Uses reflection helper | 13 | 0 general helper; Advanced products use bounded direct reflection where admitted |
| Registers author console commands | 10 | 0 |
| Direct Harmony reference | 2 | 8 |
| Direct game-assembly reference | all 18 use Stardew types | all nine Advanced products use `Assembly-CSharp` |
| Uses save commit-stage `Saving`/`Saved` | 2 | 0 |
| Declares `UpdateKeys` | 18/20 manifests | 0/11 effective declarations |
| Real hosted Content Pack | 2 | 0 |

### Lifecycle and demand patterns

The SMAPI sample uses 36 distinct subscribed events, including the specialized
`LoadStageChanged` event. Its most common signals are
`UpdateTicked` 13, `GameLaunched` 12, `ButtonsChanged` 11, `ButtonPressed` 10,
`RenderedWorld` 9, and `SaveLoaded`, mouse wheel and `Warped` eight each.
Automatic Gates activates world/update/input/render subscriptions after a save
and removes them on title return. Ladder Locator and GMCM use short-lived tick
subscriptions. Overlay components attach and dispose render/input/tick handlers
with their UI lifetime.

Final DTMAPI usage is narrower but more uniform:

- all nine Advanced products implement `Entry` and `Dispose`;
- all nine subscribe to `SaveLoaded` and `ReturnedToTitle`;
- five use `UpdateTicked` and five use `KeybindPressed`;
- MoreSaves only keeps its tick while a native restoration retry is pending;
- AutoFishing's tick is tied to an active automation session;
- eight Advanced products own exact Harmony patches and owner unpatch; MoreSaves
  owns its narrow native state without Harmony;
- retained MoreEquipmentSlots consumes frozen ABI plus `SaveLoaded`; Manbo
  consumes `IAudioReplacementApi` and no event.

This evidence changes the lifecycle verdict. DTMAPI does not lack a lifecycle
kernel for its admitted products. The open question is how much of that exact,
curated discipline can become a supported external author contract without
pretending Mono assemblies can unload.

### Content ecosystem pressure

The two real Content Patcher packs contain 780 parsed actions:

| Pack | JSON files | Actions | Main action types | Conditional/token-bearing actions |
| --- | ---: | ---: | --- | --- |
| Canon Friendly Dialogue Expansion | 43 | 374 | 333 `EditData`, 41 `Include` | 241 conditional; 369 token-bearing |
| Seasonal Cute Characters | 44 | 406 | 294 `EditImage`, 37 `EditData`, 37 `Load`, 38 `Include` | 262 conditional; 403 token-bearing |

Content Patcher itself consumes asset requests, load/edit operations, image/map/
dictionary patches, cache invalidation, owned Content Packs, pack-local JSON/file
access and locale changes. Final DTMAPI products have no hosted Content Pack and
only DebugConsole reads the Experimental source index.

This is the strongest real-consumer evidence for the remaining gap. It justifies
a DTMAPI Content Host substrate; it does not justify copying Content Patcher's
tokens, conditions, action names, asset namespace or conflict model.

### Cross-Mod API depth

All 11 DTMAPI product DLLs call `GetApi`, but nearly all calls target DTMAPI-owned
services: ConfigMenu, the two-consumer `IItemDisplayNameApi`, Manbo's
`IAudioReplacementApi`, or MoreEquipmentSlots' frozen compatibility API. No
final product calls `RegisterApi` to publish a product service, and no final
product depends on another product.

The SMAPI sample has 14 API consumers and five real providers: Automate, Chests
Anywhere, Content Patcher, Data Layers and GMCM. DTMAPI's owner/facade registry
is technically substantial, but a cross-author service ecosystem has not yet
validated version, readiness, optional dependency and provider replacement
semantics.

### Config and translation correction

Calling final 0.5.5 config or localization “early-stage” is no longer accurate.
Ten of the 11 products read config, use ConfigMenu and call translation; each of
those packages carries `english.json` and `schinese.json`. The remaining gap is
product breadth: other languages normalize to Simplified Chinese, translations
are immutable Entry-time snapshots, and there is no locale-change, plural/token,
missing-key or Content Host translation lifecycle.

## Capability-Dimension Mapping

| Capability | Final DTMAPI 0.5.5 fact | Comparison verdict and later need |
| --- | --- | --- |
| Mod discovery, dependencies, version and order | Scans Local, official `MODS` and Workshop; reconciles native enablement/source authority; handles duplicate IDs, required/optional minimum versions, cycles, deterministic topological order and reverse cleanup. Advanced source fingerprint/receipt/game-reference identity is fail-closed. | Mature loader foundation. Version comparison strips suffixes into `System.Version`; no ranges/conflicts/capability edges/load phases, and ordinary `MinimumGameVersion` only warns. Canonical version policy is a Batch 6 prerequisite before a general author/host lane. |
| Per-Mod lifecycle, error isolation and automatic disable | Entry is transactional; Core cleans Events/Input/Config/Content/API/demand/native participants, invokes `IDisposable`, proves remaining roots and uses restart-required after assembly load. High-frequency handlers quarantine after three consecutive failures. | Stronger exact owner cleanup than typical sampled SMAPI Mods. Quarantine is callback-level, not whole-Mod hot unload. A public deactivation contract decision is needed before expanding Advanced authors; universal dynamic unload is not required. |
| Event bus, frequency, subscription demand and threads | 16 standard events; owner-bound Runtime-thread mutation; immutable captured membership; zero-listener fast path; bounded FIFO/coalesce/reject queue; per-handler isolation; first/last demand reaches selected native producers. | Kernel semantics are modern and Batch 5 is closed. Surface breadth is much smaller than SMAPI's world/display/content families. Add only events whose Doloc native owner and two-consumer demand are proven. |
| Content load, cache, invalidation and hot refresh | Internal domains share dirty generations, candidate/atomic commit, last-good retention and requeue. Public ContentQuery is a read-only immutable source/item index; only reviewed Audio author-session reload is live. | Transaction substrate is real, but it is not an author content pipeline. The next Runtime should thin ContentQuery onto the native final table; G7 later owns the first Content Host. |
| API acquisition and cross-Mod services | Provider+contract registry, requester/provider owner facades, passive lookup, provider/consumer cleanup and process-lifetime reconciliation are implemented. | Mechanism is mature; ecosystem proof is shallow. Add one real provider/consumer vertical slice before designing general capability/version negotiation. |
| Manifest, Content Pack, translation and config | Strict/Advanced/ContentPack/External identities, required dependencies, inactive `UpdateKeys`, atomic config/migrations and bilingual per-Mod translation are real. ContentPack is a no-DLL identity but has no host relationship. | Config is strong; localization is adopted but narrow. `ContentPackFor`, host version and owner-scoped file/JSON/translation helpers belong to G7. |
| Save, title, world and multiplayer | SaveLoaded/Saving/Saved and ReturnedToTitle exist; save-commit/no-save rollback/orphan semantics and product proofs are strict. No general save data/session identity/world context; no multiplayer API. | DTMAPI's durability governance is stronger than its public helper breadth. Add only a native-commit-aware data/session service. Multiplayer stays reserved until a Doloc owner exists. |
| Logs, Doctor, crash diagnosis and player support | Bounded Runtime reports, Player/Install Doctor, PE/receipt/placement checks, log collection, crash/native history and Lite-vs-Full diagnostic modes exist. | Product-grade DTMAPI strength. Next-version log levels simplify noise; copying SMAPI log parser/web support is unnecessary. |
| Updates, deprecation and old ABI | 0.5.5 removes no public ABI; exact retained consumers resolve; frozen APIs have lazy Compatibility executors or disabled shells. `UpdateKeys` is inactive. | Strong bounded ABI governance, weak update ecosystem. A later advisory/update/deprecation service should build on Catalog/Doctor facts rather than emulate Nexus-specific keys. |
| Author SDK, templates, packaging and release validation | Create/validate/build/pack/deploy/update/withdraw/status/Doctor, Strict `SDK160`, exact Advanced policies, deterministic packages and crash-safe transactions exist. | Mature curated SDK. Current old examples that omit `CodeModKind` are not valid modern Strict teaching proof; refresh them before external author expansion. General Advanced remains closed. |
| Performance, reflection caching, Harmony ownership and unload | Real no-demand optional-work silence, focused 10,000-frame zero-allocation paths, cached reflection access, exact Advanced Harmony owners and owner unpatch exist. Numeric platform budget is explicitly unestablished; some routes are process-pinned dormant; Mono assemblies remain resident. | Strong structural boundary, honest limitation. Repair measurement before adding recurring shared hosts. Do not promise arbitrary third-party patch cleanup or code unload. |

## Audit Findings Introduced By This Recomparison

No P0 Runtime or 0.5.5 release blocker was found. The following are current
governance findings for the first Batch 6 compatibility slice.

### P1 — Public stability metadata disagrees with the canonical API matrix

The matrix classifies `IModRegistry`, `ITranslationHelper` and
`IDtmConfigMenuApi` as StableCandidate (with optional ConfigMenu pieces still
Experimental). Source attributes classify them respectively as Stable,
Experimental and Experimental:

- `src/DTMAPI.Abstractions/Helpers.cs:24`;
- `src/DTMAPI.Abstractions/Helpers.cs:40`;
- `src/DTMAPI.Abstractions/ConfigMenu.cs:6`.

Both channels are author-visible and both claim to communicate stability. Before
SDK/Doctor consumes this metadata more broadly, choose one canonical projection
and make the other mechanically agree. This is a compatibility-governance fix,
not authority to promote an API.

### P1 — The Batch 6 contract's release-status prose is stale

The verified post-publication Update and Product Catalog record the actual
Runtime 0.5.5 Steam artifact, while the canonical Batch 6 identity contract's
status and current-admission table still say no Steam upload occurred. The user
has explicitly closed 0.5.5, so this is stale fact projection, not an open
publication task. Reconcile the contract by linking the post-publication
authority; do not rewrite historical pre-upload evidence.

### P1 — Deferred MoreEquipmentSlots 1.0 transaction findings remain open

The final 0.5.5 baseline deliberately retains Workshop MoreEquipmentSlots
`0.3.1-dtmapi`; that compatibility result is closed and is not reopened here.
The separately implemented ProductNative 1.0 path remains publication-deferred,
and [its three-pass reaudit](20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)
keeps five next-version P1 findings open:

1. the production mail reader can still convert some malformed/unreadable
   collection or reward authority into a false zero;
2. incoming `CostItem` withdrawal trusts the returned Boolean instead of a
   strict native before/after observation;
3. missing/unreadable native-save fingerprints can compare equal and masquerade
   as an exact preimage;
4. Compatibility cold recovery accepts coarse count growth instead of exact
   destination, count and post-save fingerprint evidence;
5. delayed count-only reconciliation can be confused by unrelated same-item
   gameplay before the next observation.

These are gates only for the new MoreEquipmentSlots 1.0 Product/Host transaction
and any proposed protected-storage generalization. They are not Runtime 0.5.5,
retained ABI or old Workshop 0.3.1 defects.

### P2 — One closeout follow-up points to a nonexistent Update ID

`docs/updates/2026/20260801-0002-workshop-upload-release-closeout.md:176`
names Update `20260731-0006`. The actual owner is
[20260731-0003 ContentQuery/Lifecycle/Frame/Log closeout](../../../../updates/2026/20260731-0003-contentquery-lifecycle-frame-log-closeout.md).
Correct the reference in the next documentation-governance slice.

### Accepted limitation — No quantified platform performance budget

The Product Catalog intentionally says
`claim=bounded-structural-lifecycle-trend`,
`quantifiedBudgetStatus=not-established` and
`blocked-allocation-counter-nonfunctional`. The real Unity no-demand and active
product evidence supports the bounded 0.5.5 release claim, but not a whole-game
GC, memory-risk or per-unit allocation threshold. This becomes a gate before
new recurring shared infrastructure, not a retroactive 0.5.5 defect.

## Gap Classification After 0.5.5 Closeout

### Closed 0.5.5 baseline — do not reopen

- Batch 5 event publication/mutation, bounded queue and handler quarantine;
- listener/operation demand coordination and selected native producer silence;
- internal content generation atomicity and last-good retention;
- exact ProductNative ownership for the admitted products;
- config atomicity and owner-bound migrations;
- strict/Advanced/ContentPack/External identity, SDK receipt and exact Harmony
  owner enforcement;
- frozen 0.5.5 ABI and lazy Compatibility Host;
- no-demand/active-product evidence, player QA, Doctor, exact release candidate
  and published Runtime artifact.

No item in this section is an open “0.5.5 发布前需要” task.

### Batch 6 前必须处理

These are prerequisites for broadening public/platform responsibility, not for
revalidating 0.5.5:

1. Reconcile the three stability metadata rows, the stale Batch 6 publication
   prose and the incorrect Update ID. Freeze one mechanically checkable fact
   projection before new API status changes.
2. Decide and document the general CodeMod deactivation contract: whether
   `IDisposable` is officially supported, ordering relative to platform cleanup,
   failure/remaining-root/retry semantics and the exact restart boundary. Do not
   expose the current product-specific reason-aware reflection hook as a public
   API by accident.
3. Replace duplicated suffix-stripping `System.Version` comparisons with one
   named package-version policy before `ContentPackFor`, service versioning or a
   general author lane depends on it. Either enforce `MinimumGameVersion` from a
   proven game-build authority or keep it explicitly advisory.
4. Refresh Hello/ConfigMenu/AutoHarvest teaching samples through the current SDK
   so they explicitly declare modern Strict identity and `0.5.5`; old legacy
   manifests must remain compatibility fixtures, not the default tutorial.
5. Restore a functional allocation measurement route and set a bounded baseline
   for any new recurring shared host. Keep current no-demand structural receipts
   as a separate invariant; do not invent a numeric threshold from process-memory
   noise.

### Batch 6/7 产品化需要

1. Execute the already recorded next-Runtime simplification as separate slices:
   thin ContentQuery over the native final table/winning source; individual
   lifecycle decisions rather than a second giant state machine; native/PlayerLoop
   frame ownership; real log minimum levels.
2. Before publishing MoreEquipmentSlots 1.0, close its five deferred transaction
   findings through strict mail evidence, observed withdrawal outcome, readable
   native-save fingerprints, exact cold recovery and immediate/no-intervening-
   gameplay ambiguity handling. Do not generalize the duplicated Product/Host
   transaction into SharedNative or protected storage while these remain open.
3. After a separate bounded product/API authority change, build one small real
   product API provider/consumer pair. Prove optional
   dependency, provider readiness/version, both load orders, provider failure,
   consumer/provider deactivation, stale facades and restart behavior before
   generalizing service capability negotiation. A fixture may research the
   contract earlier, but it does not admit a thirteenth product.
4. After a separate bounded G7 authority change, open only one Content Host
   vertical slice:
   `ContentPackFor`, host minimum version, owner-scoped safe path/JSON/
   translation access, deterministic ordering, pack isolation, last-good
   publication and enable/disable/restart rules. Do not open general Advanced as
   a side effect.
5. Add a native-commit-aware owner data service only after classifying Mod-local,
   save-bound, global and session data. Its tests must preserve no-save rollback,
   normal-save commit and interrupted-notification reconciliation.
6. Extend localization through real demand: locale change/republication, missing
   key and malformed-file diagnostics, then parameters/plurals if actual authors
   need them. Preserve the working immutable read path.
7. Add author commands and a cached Strict reflection helper only where real Mods
   justify them. Advanced products may keep private native/Harmony work under
   their exact owner rather than forcing it into GameBridge.
8. Add update/advisory, compatibility and deprecation-runway services after the
   version authority is unified. Steam-managed first-party products do not need
   a literal copy of Nexus `UpdateKeys`.
9. Broaden world/display/content events only from reviewed Doloc native owners
   and at least two independent consumers. Event count is not a maturity target.

### Batch 8 或独立项目

- a Content Patcher-class token/condition/action language, content conflict UI,
  image/table/map patching and broad live-native propagation;
- true assembly isolation/unload or a separate process host;
- a generally available third-party Advanced author channel and private
  dependency-resolution policy;
- ecosystem-scale compatibility database, remote support analysis, publishing
  portal or independent launcher/updater;
- broad HUD/overlay or world-object automation hosts not yet supported by two
  Doloc consumers.

### 不适用于 Doloc Town，或保持 Future-reserved

- SMAPI binary compatibility, Stardew Mod assembly rewriting or loading Stardew
  Mods in Doloc Town;
- MonoGame/XNA render phase names, Stardew asset namespaces, map semantics,
  save repair and ErrorHandler behavior;
- copying Content Patcher's exact DSL or Nexus-specific update-key policy;
- claiming DTMAPI owns every Harmony patch created by an External BepInEx plugin;
- unloading every Mod on return to title;
- Stardew host/farmhand/split-screen contracts. Multiplayer is Future-reserved
  until a Doloc native multiplayer owner is found, not a current platform gap.

## Executable Revised Route

| Order | Bounded route | Required output | Acceptance boundary |
| ---: | --- | --- | --- |
| 1 | B6 authority hygiene | One API status authority/projection, corrected Batch 6 release status link, corrected 20260731 Update reference | Documentation/link checks plus Abstractions metadata/source check; no game run |
| 2 | B6 lifecycle/version contract | Publicly documented `IDisposable`/cleanup/restart rule and one package-version implementation/policy | Focused Core/Abstractions units: Entry failure, Dispose failure, remaining-root retry, provider/consumer order, prerelease comparisons and advisory game version |
| 3 | Next-Runtime simplification | Native-final-table ContentQuery adapter, individually proven lifecycle phases, simplified frame owner and real log levels | Separate focused source/unit gates and only the smallest relevant game evidence for each slice |
| 4 | Deferred MoreEquipment 1.0 correction | Close the five exact Product/Compatibility transaction findings before any new package or publication authority | Focused Product/real-Host fault fixtures, native-save semantics and independent acceptance; retained 0.3.1 remains unchanged |
| 5 | Author proof, authority-gated | After its own bounded product/API authority change: SDK-generated Strict examples plus one real provider/consumer pair | Pack/Doctor/ABI checks, both load orders, optional absence, failure, cleanup and restart; no thirteenth product or general Advanced admission is implied |
| 6 | G7 first Content Host, authority-gated | After its own bounded G7 authority change: one domain-specific hosted pack contract and two independently authored packs before promoting the ContentOwner/public host contract | Cold load, duplicate IDs, dependency/version failure, bad pack last-good, disable/re-enable, title/save/no-save, owner cleanup and no-demand evidence; any later SharedNative adapter needs its own two-consumer/native-owner decision |
| 7 | Ecosystem services | Data/session helper, locale lifecycle and update/deprecation advisory in independent slices | Each service owns its native/file authority and consumer proof; no omnibus “match SMAPI” release |
| 8 | Independent content platform | Separate decision for a Content Patcher-class product | Its own DSL/versioning/conflict/security/performance program; not a Core prerequisite |

## Decisions That Must Not Be Inferred

- 0.5.5 completion does not open a thirteenth product, general Advanced authoring
  or G7.
- A provider registry entry, internal facade or future reuse does not prove
  SharedNative ownership.
- ProductNative Harmony use is not evidence that DTMAPI should expose Harmony or
  game types from `DTMAPI.Abstractions`.
- The current ContentQuery index is not a content loader, and the internal
  generation coordinator is not a public content-edit contract.
- `ProcessPinnedDormant` means bounded inactive work, not unloaded code.
- Exact old-ABI compatibility does not equal an update checker or broad
  compatibility database.
- The accepted GC wording and structural trends do not establish a numeric
  whole-game memory or crash budget.

## Evidence Index

DTMAPI primary evidence:

- [PROJECT.md](../../../../../PROJECT.md)
- [Public API Matrix](../../../../api/public-api-matrix.md)
- [Batch 6 Managed Mod Identity Contract](../../../../architecture/batch6-managed-mod-identity-contract.md)
- [Runtime Query/Lifecycle/Driver/Logging Roadmap](../../../../planning/20260731-runtime-query-lifecycle-driver-logging-roadmap.md)
- [SMAPI Ecosystem Map](../../api/2026/smapi-ecosystem-map/INDEX.md)
- [Local Mods Native-Owner Map](../../api/2026/local-mods-native-owner/INDEX.md)
- [0.5.5 release closeout](../../../updates/2026/20260801-0002-workshop-upload-release-closeout.md)
- [0.5.5 published Runtime artifact](../../../updates/2026/20260801-0003-runtime-published-metadata-authority.md)
- `tools/release/dtmapi-product-catalog.json`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Runtime/ContentRefreshGenerationService.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.Core/Services/ConfigService.cs`
- `src/DTMAPI.Core/Services/TranslationService.cs`
- `tests/DTMAPI.UnitTests/Batch5EventKernelTests.cs`
- `tests/DTMAPI.UnitTests/Batch5DemandCoordinatorTests.cs`
- `tests/DTMAPI.UnitTests/Batch5ContentGenerationTests.cs`

SMAPI primary evidence is under the local repository at
`E:\Python_project\SMAPIlearning\SMAPI`. Tag-qualified anchors include:

- `1.15.4:src/StardewModdingAPI/Framework/ModLoading/ModResolver.cs`;
- `1.15.4:src/StardewModdingAPI/Framework/InternalExtensions.cs`;
- `1.15.4:src/StardewModdingAPI/Framework/Reflection/Reflector.cs`;
- `develop:src/SMAPI/Framework/ModLoading/ModResolver.cs`;
- `develop:src/SMAPI/Framework/Events/ManagedEvent.cs`;
- `develop:src/SMAPI/Framework/SCore.cs`;
- `develop:src/SMAPI/Framework/ContentCoordinator.cs`;
- `develop:src/SMAPI/Framework/Reflection/InterfaceProxyFactory.cs`;
- release-note, Content Pack/helper and `src/SMAPI.ModBuildConfig` history at
  the sampled tags.

## Validation And Limits

- Read every required DTMAPI project/planning/debug/reference/governance/API and
  Batch 6 identity authority before forming the comparison.
- Compared the sampled SMAPI tags and current `develop`; confirmed no local SMAPI
  commit drift since the July Review.
- Scanned the real SMAPI manifest/DLL/Content Pack sample and the exact final
  DTMAPI product candidate using manifests, files and PE metadata.
- Cross-checked current DTMAPI Abstractions, Core, GameBridge, product source,
  Product Catalog, retained compatibility evidence and final 0.5.5 release
  records.
- This Review does not promote or remove a public API, authorize a product or
  Content Host, change 0.5.5, modify Workshop state, or claim runtime behavior
  beyond retained evidence.
- No game launch, runtime lock, build, full Release, save mutation or external
  write was required for this audit.
