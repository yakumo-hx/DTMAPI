# Five-Product Baseline Correction And Freeze

## Metadata

- Update ID: `20260722-0004`
- Date: `2026-07-22`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: `batch6/five-products/item-display-name/cleanup/qa/author-sdk/policy/tooling/compatibility-host/freeze`

**Target:** DTMAPI `0.5.5` source candidate; five-product baseline and dormant-shipped Compatibility Host verified, publication blocked
**Source request:** user decision following code Review `20260722-0009`
**Owning Review:** [Five-Product Update Continuation Audit](../../reviews/code/2026/20260722-0009-five-product-update-continuation-audit.md)

## Scope

Correct and freeze the existing five-product Batch 6 candidate without admitting a sixth product or broadening the public/native surface:

1. enforce the selected `IItemDisplayNameApi` Experimental contract;
2. independently attempt Fish/Animal state cleanup, callback detach and exact-owner Harmony unpatch;
3. make final Advanced owner QA select only the explicitly requested products;
4. project Advanced policy membership/resources from the tracked registry and reuse one Author SDK build per complete test run;
5. reconcile current-truth documents and record the result honestly as ownership rehome, not mandatory-Runtime slimming;
6. after focused checks, run only the Fish/Animal third-save short acceptance, then freeze the five-product baseline independently from unrelated reverse-capture work.

The safety clause remains binding:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The reviewed shared native responsibility is `DolocAPI.QueryItemProto(itemId)` plus `ItemInfo.Title`; the current build also confirms `Item.title => proto.Title`. Fish `Item.get_title` formatting and Animal `AnimalFullInfoData`/`AnimalViewer.Show`/`AnimalPanelUiState.Unregister` behavior remain ProductNative.

## Selected API Contract

`IItemDisplayNameApi` remains a public `Experimental` API for exactly:

- main-thread calls;
- read-only `itemId -> localized display name` lookup;
- caching successful, non-empty results only;
- no negative caching after missing IDs, native lookup failures or exceptions;
- cache clearing at `SaveLoaded`, `ReturnedToTitle` and runtime-environment reset.

It does not promise item DTOs, enumeration, mutation, Hooks, formatting or UI. FishBreedingAssistant and AnimalHusbandryProgress remain two independent real consumers. Their titles/rows, lifecycle state, Harmony owners, UI and Hooks stay in their own Advanced ProductNative assemblies. This Update does not promote the API for 0.5.5.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/ItemDisplayName/*`, `Features/EnvironmentReset/EnvironmentResetHookBridge.cs`, the GameBridge demand catalog/callback router and `DolocTownGameBridge.cs`: narrowed and enforced the selected Experimental lookup/cache/thread/lifecycle contract. A successful lookup owns one operation-lifetime lifecycle-protection demand; the one physical `DolocAPI.SetEnvCamera` Hook now has a SharedNative owner independent of optional Camera, and successful results remain uncached until it reports ready. Cache clearing releases through an independent exception-isolated fanout step.
- FishBreedingAssistant and AnimalHusbandryProgress ProductNative runtimes plus their focused product checks: made state cleanup, callback detach and exact-owner unpatch independent cleanup attempts.
- optional QA settings/participant/owner observer/fixtures and `run-game-smoke.ps1`: selected an explicit requested owner set, observed actual Harmony inventory and added a one-second normal-Gameplay quiescence boundary after the last native-UI terminal before ReturnHome.
- Unit and QA Unit programs: covered positive-only cache retention, retry after empty/exception, SaveLoaded/ReturnedToTitle clearing, zero-demand callback sleep, the real `SetEnvCamera callback -> GameBridge -> ItemDisplayName cache clear` route, synchronous demand release, main-thread rejection, requested-owner selection and ReturnHome quiescence.
- Core, Author SDK and Install Doctor Advanced policy loaders/projects plus both author schemas: consume the tracked policy registry/resources rather than handwritten per-product constants or schema enums.
- the general Advanced builder, Catalog checker and complete test orchestrator: accept/reuse one same-run Author SDK build while keeping thin product-focused wrappers.
- `DTMAPI.GameBridge.DolocTown.Compatibility`, its fail-closed broker/proxies and the package/install/Doctor/Manager/collector paths: moved the five frozen heavy ABI executors behind one Catalog-declared dormant-shipped optional component. The default-loaded GameBridge has no static Host reference; the first exact frozen-ABI call validates and loads the tracked component from bytes, while normal startup publishes a zero-service dormant state.
- Abstractions compatibility metadata, input/ActionSpeed diagnostics: added a disposition axis independent of stability, marked the five retained API families `Experimental + Frozen` with source warnings but no ABI deletion, removed dead input diagnostics and made ActionSpeed QA summaries explicitly opt-in.
- the public API matrix, Batch 6 contract, Fish/Animal product records, roadmap/Hook/review/smoke/update records: reconciled current ownership, evidence and honest weight wording.

## Validation Plan

- focused Unit test for guarded positive-cache reuse, failure non-retention, Hook-not-ready fail-closed behavior, lifecycle clear and off-thread rejection;
- focused Fish/Animal source and cleanup-independence checks;
- QA unit/source checks for explicit requested-owner selection;
- Core/Author SDK/InstallDoctor policy-registry and schema tests;
- Catalog/live zero-leftover plus both SDK package checks;
- no complete Release run for this bounded correction;
- one final combined Fish/Animal third-save short acceptance proving valid shared lookup, representative product behavior, title/native close and exact requested-owner cleanup.

No L0-L5, GC ladder or long test is authorized. No 0.5.5 publication is authorized.

## Evidence

The focused correction checks pass. `GAME-SMOKE/20260722-180502` retains the corrected product-behavior and exact-owner-cleanup boundary. Current-DLL `GAME-SMOKE/20260723-074311` closes the later SharedNative EnvironmentReset lifecycle gate and returns this Update to `verified/closed`.

- item-display-name focused Unit: positive cache, no negative cache after empty/exception, Hook-not-ready successful lookup without caching, shared physical request/readiness deduplication, SaveLoaded/ReturnedToTitle clearing, zero-demand callback sleep, Camera-disabled shared ownership, production static `DolocApiSetEnvCameraPostfix` invalidation/release and off-thread rejection pass;
- Batch 5 GameBridge demand focused Unit: fixed Catalog classification and the retained-callback zero-demand/allocation boundary pass with the new route dormant at an empty cache;
- QA Unit: explicit requested-owner selector and four supported owner bindings pass;
- Author SDK and Install Doctor focused projects pass, including structural schemas and registry-driven exact fail-closed loading;
- Phase 0, Catalog/live zero-leftover, OneActionComplete, ActionSpeed, Fish and Animal focused scripts pass;
- Fish package `1EE02771C6C50D8ED345BD3E648DAD19136CE785F11A11F318E6A30ABBEE3430` and Animal package `C98B509B95193B9F1191DE966CEFE792FE11833AEDC6C66E0499DAC72305DA55` were generated through the shared builder while reusing one same-run Author SDK build.

The complete Release entry was intentionally not run because it includes named GC-ladder coverage forbidden by the source request. No L0-L5, GC or long test ran.

The first corrected-package run `GAME-SMOKE/20260722-174252` reached save slot 3 and passed Fish/Animal in-save behavior plus ReturnedToTitle cleanup, but the native title transition stopped before continuous HomePage, final owner deactivation and exit. `175832` reproduced the same transition stop. The second log proved the Animal native UI terminal and `ReturnHome` request occurred in the same update frame; the game was visually observed at the transition background with no Steam/cloud modal. The QA participant now requires one continuous second of normal Gameplay after the last in-save/native-UI terminal before requesting ReturnHome. QA build and Unit passed after the repair.

`GAME-SMOKE/20260722-180502` then passed the exact `CoreOnly + Fish + Animal` third-save profile:

- Fish resolved the shared localized name and rendered `鱼卵 (鱼)` with one actual product owner patch/target;
- Animal constructed `20` read-only rows, showed `羊毛脂 0/100` in one native-style overlay row, captured a `437,558`-byte screenshot, reported `mutation=false` and closed only its exact native UI receipt;
- the Animal terminal completed at `18:05:40.978`, ReturnHome was delayed until `18:05:42.422`, and HomePage became visible at `18:05:42.762`;
- title-button lifecycle passed; real Loader deactivation reduced Fish `1` patch/target and Animal `4` patches across `3` targets plus both callbacks, instances and Core roots to zero;
- the third-save data and previous/backup files were restored byte-for-byte, official profile and local source state were restored, no fatal/fresh-crash evidence appeared, and no `DolocTown.exe` remained.

Using the same five player-loaded project boundary as Review `20260722-0004`, the frozen five-product commit `c29393a5` contained 151 C# files / 76,301 physical lines / 68,224 non-empty lines; the first fanout-only correction `025fdf3e` contained 151 / 76,302 / 68,225. The shared-owner/fail-closed filesystem candidate before Host extraction contained 152 / 76,413 / 68,329 (`+112` physical and `+105` non-empty versus `c29393a5`). Fish and Animal product assemblies remained 1,393 physical source lines combined, while their two frozen GameBridge Compatibility directories remained 1,566 physical lines. Those figures describe the pre-Host source boundary and must not be reused as a download-size claim; the later Host closeout below owns the default-load result.

## Post-Commit Audit

Independent review of commit `c29393a521261653575f2aabca51e583c99168fa` found one remaining focused lifecycle defect. `ItemDisplayNameFeature.EnvironmentReset` clears the cache, but `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset` currently dispatches only Camera plus lifecycle counters. The Unit test directly called `ItemDisplayNameService.Clear`, so it proved service behavior without proving the real bridge route.

At the post-commit audit checkpoint, the accepted `180502` Fish/Animal behavior, save/title cleanup, exact owner deactivation, restoration and exit evidence remained valid while the Update temporarily returned to `implemented/partial/open` for the missing EnvironmentReset route. The requested smallest correction was to add ItemDisplayName as an independent existing `RunEnvironmentResetStep` and exercise cache invalidation through the bridge entry; the following closeout records that result.

### Focused EnvironmentReset Closeout

The first correction, commit `025fdf3e`, added `ItemDisplayName.EnvironmentReset` as its own `RunEnvironmentResetStep` and changed the Unit from a direct Service clear to a direct bridge-entry call. That proved exception-isolated fanout, but a follow-up audit found it still bypassed the production callback gate: `DolocApiSetEnvCameraPostfix` returned whenever Camera had no lease/restore demand, and Fish/Animal title lookup creates no Camera demand.

The final correction gives the first successful non-empty ItemDisplayName lookup an independent `ItemDisplayName.EnvironmentReset` demand and retained callback bit. The one physical `DolocAPI.SetEnvCamera` Postfix moves from optional Camera into `EnvironmentResetHookBridge`, so Camera QA isolation cannot remove the shared native owner. Camera and ItemDisplayName retain separate logical routes but share one deduplicated `camera.set-env` physical route. Until the Hook reports ready, successful titles are returned without caching; installation failure therefore cannot leave an unprotected cache. Cache clearing at save load, title return or environment reset releases the demand. The focused Unit calls the production static `DolocApiSetEnvCameraPostfix` boundary and proves zero-demand sleep, Hook-not-ready fail-closed behavior, one physical install request for two simultaneous logical demands, guarded positive-cache reuse, callback-to-bridge invalidation, Camera-disabled cleanup, synchronous release and return to the zero-demand fast path. Assembly-CSharp is unavailable to the Unit, so physical native invocation itself remains covered by the exact source target/readiness gate rather than being overstated as a game Hook run.

- `DTMAPI.UnitTests.csproj` Release build: PASS, `0` warnings / `0` errors.
- `DTMAPI_UNIT_TEST_FOCUS=item-display-name`: PASS.
- `DTMAPI_UNIT_TEST_FOCUS=batch5-gamebridge-demand`: PASS.
- The first current-DLL attempt, `GAME-SMOKE/20260723-073752`, proved both product queries, Hook readiness and a real callback clear/release, then failed closed because a later valid query and the normal `ReturnedToTitle` clear overwrote the single diagnostic status slot. No native leak was observed. The focused repair retains the causal environment-reset clear in `SharedNative.ItemDisplayNameEnvironmentReset` without preventing later queries; its focused Unit passes.
- `GAME-SMOKE/20260723-074311` then passed in 66 seconds with current DLLs, HookProbe and two third-save cycles: Fish and Animal independently queried the API, `SharedNative.EnvironmentReset` became ready, two real `DolocAPI.SetEnvCamera` callbacks cleared `4` then `1` cached entries and released demand, the title recovered, the second save queried again, and terminal status reported `queries=2; realSetEnvCameraClears=2; cache=0; demand=0; retainedCallback=false; titleRecovered=true`. Startup, HookProbe, Fish, Animal, SaveLoadCycle, save/profile/source restoration, no-fatal and clean process exit all passed.
- Complete Release, L0-L5, GC and long testing were not run.

## Compatibility Host Closeout

Review `20260722-0010` identified five exact published old-ABI consumers and about 7,911 physical lines of heavy compatibility executors in the mandatory GameBridge source boundary. The closeout freezes one component only:

- Catalog ID `gamebridge-compatibility-host`;
- `netstandard2.0` assembly `DTMAPI.GameBridge.DolocTown.Compatibility.dll`;
- package path `DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll`;
- `distribution=dormant-shipped`, `loadPolicy=first-frozen-abi-call`, `defaultLoadState=dormant`;
- framework component, not a Mod and not a `BepInPlugin`; no BepInEx scan path and no static GameBridge assembly reference.

The GameBridge retains only the five public provider identities and lightweight typed proxies. On the first frozen-ABI call, the broker verifies the release-manifest component receipt, exact relative path, length, SHA-256, assembly identity, version, target framework and single factory contract before loading bytes. Missing, mismatched or ambiguous inputs remain fail-closed. Once Mono loads the assembly it is process-resident; owner/demand/callback/Hook state can return to zero, but unload is not promised.

Focused validation passed:

- Compatibility Host and Unit Release builds: `0` warnings / `0` errors;
- focused Unit `compatibility-host`, `item-display-name` and `batch5-gamebridge-demand`, plus the QA Unit project;
- the exact ABI/consumer/provider gate: five known consumer hashes/member references, five provider identities, no mandatory Host `AssemblyRef`, no heavy executor markers in GameBridge, proxy IL `5,253` bytes and Host service IL `53,192` bytes;
- Catalog/live zero-leftover and Phase 0 machine contract, including the single optional-component topology;
- Runtime-only package build/install and the upgrade transaction matrix under PowerShell 7 and Windows PowerShell 5.1 (`15` cases each).
- independent post-implementation review found and closed four concrete gaps: exact optional-component receipt path validation, real Fishing facade coverage in the focused set, process-resident Fishing owner dictionary cleanup, and Doctor/status enforcement of `first-frozen-abi-call + dormant + included` policy. The complete Unit project, focused Host Unit, QA Unit, InstallDoctor tests and Catalog checker pass after those corrections.

The first title-only diagnostic `GAME-SMOKE/20260723-083503` observed exactly one startup state, `Compatibility.Host=dormant; loaded=false; services=0; demand=0; callbacks=0; hooks=0`, but its 15-second window ended before generic final health/status publication and is not acceptance. `083634` exposed the expected AutoFishing-product conflict; `083828` then exposed an over-broad `dtmapi.mod.*` collision check on the shared `AgentStateBase.OnExit` target. The correction compares only the exact Catalog-derived AutoFishing Harmony owner, preserving product-first fail-closed behavior without rejecting unrelated ActionSpeed ownership.

`GAME-SMOKE/20260723-084331` remains useful pre-review evidence for dormant-to-resident loading, legacy configure/enable/disable, transient restoration and clean exit, but its fixture checked only the outer feature owner resources. Independent review correctly found that the process-resident Host backend still retained one options row and one state row, so `084331` is superseded as final acceptance. The repair removes all backend owners only after restoration and exact-owner unpatch succeed, keeps a retry tombstone if that cleanup fails, and makes the QA fixture inspect the resident backend directly.

The first post-review run `GAME-SMOKE/20260723-092433` installed the exact `b8fbad8` Runtime-only package and passed startup, HookProbe, save/title and cleanup infrastructure, then deliberately failed closed because the local ProductNative AutoFishing owner was also loaded. After applying and later removing the standard local `dtmapi.disabled` isolation marker, `GAME-SMOKE/20260723-092718` became the bounded third-save acceptance. It passed current-DLL startup/HookProbe, ordinary-start Host dormant state, first-call `FishingAutomation` residency, legacy configure/enable/disable, exact Harmony cleanup, every enumerated transient holder at zero/false, **resident backend `ownerOptions=0` and `ownerStates=0`**, title/profile restoration and clean process exit. The installed status/Doctor path also accepted the exact dormant-shipped receipt at provenance commit `b8fbad8`; Mono residency remains explicit and is not called unload.

The previous GameBridge binary was `1,084,928` bytes. The reviewed final default-loaded GameBridge is `966,656` bytes (`-118,272`, about `10.90%`), while the dormant-shipped Host is `184,832` bytes. This proves reduced **default-loaded GameBridge volume only**. GameBridge plus Host totals `1,151,488` bytes, so neither download-package size nor total shipped bytes decreased; no such claim is made. Moving linked source into an optional compilation boundary also does not mean the repository deleted 7,911 physical source lines.

The complete Release suite, L0-L5, GC ladders and long tests were intentionally not run. The Animal 80ms ProductNative refresh optimization remains deferred.

## Rollback

Revert this Update's focused source/tooling/document changes together, including the Catalog optional-component row, Host broker/proxies and package/install projections. Do not revert the already accepted five-product ownership rehomes or delete frozen Compatibility ABI bodies. Restore the pre-correction packages only if the final candidate fails and no smaller repair is available.

## Follow-up

The exact five-product behavior/owner baseline, `180502` product-owner evidence, `074311` shared-lifecycle evidence and post-review `092718` dormant-to-resident Compatibility Host evidence are frozen. The selected ItemDisplayName API remains Experimental; the five retained old-ABI families remain `Experimental + Frozen`. This Update is `verified/passed/closed`. No sixth product, Content Host G7 implementation or 0.5.5 publication is authorized, and no download-package reduction is claimed.
