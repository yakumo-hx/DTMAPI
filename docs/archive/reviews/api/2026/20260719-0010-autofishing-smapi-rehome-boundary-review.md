# 20260719-0010 AutoFishing SMAPI Rehome Boundary Review

## Metadata

- Review ID: `20260719-0010`
- Date: 2026-07-19
- Status: `recorded/docs-only`
- Domain: AutoFishing / managed native CodeMod / GameBridge ownership / GMCM
- Runtime validation: `not-required`
- Source request: the user asked for a SMAPI-backed, assertive disposition of which AutoFishing code stays in DTMAPI, moves into the Mod, moves into optional QA, remains temporarily for compatibility, or is deleted as GameBridge scaffolding. The user also proposed cancelling the universal rule that every DTMAPI-managed Mod must avoid direct Unity, Harmony, and game-assembly references.
- Parent review: `docs/reviews/code/2026/20260719-0008-dll-mod-entry-and-migration-boundary-audit.md`
- Owning Update: `docs/updates/2026/20260719-0001-dll-mod-entry-model-audit.md`

This review changes no loader, SDK, Runtime, public API, Mod source, compatibility promise, package, or roadmap. It records the target boundary before implementation. No game was launched.

## Executive assertions

1. **Cancel the universal native-reference ban.** The rule “all DTMAPI-managed Mods must not reference Unity, Harmony, or `Assembly-CSharp`” is the principal policy that forces single-consumer gameplay implementations into the mandatory GameBridge.
2. **Do not cancel the safe default.** API-only Mods remain the default and recommended authoring lane. The cancellation applies to the word “all”, not to the strict lane itself.
3. **Direct native access does not make a Mod unmanaged.** A `DtmMod` can still be discovered under DTMAPI `Mods`, pass manifest/version/dependency admission, use DTMAPI logging/config/i18n/GMCM, appear in Manager, and obey official cold-start enablement while its own DLL owns Harmony and game access.
4. **Advanced native Mods are restart-bound.** DTMAPI can reliably prevent a disabled Mod assembly from loading at the next process start. It cannot promise general CLR assembly unload or automatic reversal of arbitrary static state, native objects, event subscriptions, or Harmony patches after that DLL has run. Manager must therefore report “restart required” for post-load enable/disable changes unless a narrower cleanup contract is proven.
5. **Keep raw native types out of `DTMAPI.Abstractions`.** Allowing an advanced Mod to reference Unity/game assemblies is not permission to expose those types through the stable public API. The framework API stays portable and stable; native coupling stays private to the advanced Mod.
6. **AutoFishing is ProductNative, not a public fishing API.** Its phase hooks, native caches, energy checks, animation control, input overrides, and state transactions move into `AutoFishingMod`; they are not promoted into a public `IFishingPrimitivesApi`.
7. **Keep DTMAPI's GMCM-class capability in the base product.** `DTMAPI.ModConfigMenu`, its public registration API, and its Bootstrap/UI host remain bundled platform services. AutoFishing owns only its option declarations and page registration.
8. **Do not copy the current split mechanically.** Most of the 5,403-line fishing-native directory moves by responsibility, but cross-assembly session facades, GameBridge feature hosting, first-party friend access, demand routing, and compatibility arbitration must be collapsed or deleted instead of being moved unchanged.

## Current public/API status and target

| Surface/domain | Current status | Target status | Reason |
| --- | --- | --- | --- |
| `IFishingAutomationApi` and its public DTOs | Experimental / Deprecated / Frozen | Transitional compatibility only; no new use; remove only after the recorded preview/consumer gates | The old published AutoFishing binary still binds to this surface. It is not a suitable owner for the new product. |
| `IFirstPartyFishingPrimitivesApi`, `IFirstPartyFishingSession`, related internal DTOs/leases | DTMAPI-internal through `InternalsVisibleTo` | Delete after AutoFishing owns its native runtime | This entire cross-assembly seam exists because the product is forbidden to own its implementation. It has one real product consumer and should not become public. |
| AutoFishing native behavior | Mandatory `DTMAPI.GameBridge.DolocTown` implementation | Advanced DTMAPI-managed product implementation | The behavior and its game-version coupling belong to the product that ships and versions it. |
| Generic loader/events/input/config/log/i18n/registry/dependency/version services | Framework | Keep in DTMAPI | These are reusable SMAPI-like platform responsibilities. |
| `DTMAPI.ModConfigMenu` and UI host | Bundled platform module | Keep in DTMAPI | This is an intentional DTMAPI product choice even though Stardew's GMCM is a separate Mod. |

No public API matrix row changes in this docs-only review. The table above is the recommended target for a later breaking/implementation Update.

## SMAPI recheck

### Framework build boundary

The local SMAPI source does not impose an API-only rule on managed Mods:

- `E:\Python_project\SMAPIlearning\SMAPI\src\SMAPI.ModBuildConfig\build\smapi.targets` adds references to `Stardew Valley.dll`, `StardewValley.GameData.dll`, MonoGame and `StardewModdingAPI.dll` for ordinary Mod builds.
- The same targets add `0Harmony.dll` when the Mod project sets `<EnableHarmony>true</EnableHarmony>`.
- SMAPI owns discovery, entry construction, events, config, logging, translations, assembly resolution/rewriting, compatibility warnings and diagnostics. It does not require each gameplay patch to be moved into SMAPI.
- SMAPI does retain small generic compatibility facades such as the local `FishingRodFacade.cs`; those are cross-version framework compatibility, not an AutoFishing implementation.

### Yet Another Fishing Mod 1.2.0

The installed local package is:

`E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods\【快速钓鱼】YetAnotherFishingMod`

The author's tag `yetanotherfishingmod-v1.2.0` points to commit `e23b8d72ea1d5551cf770733801d712dec28e83f`. The source is MPL-2.0 and was used only as architectural reference.

Physical source lines at that tag:

| File/responsibility | Lines | Owner |
| --- | ---: | --- |
| `ModEntry.cs` | 119 | Mod entry, SMAPI events, config initialization, Harmony owner creation |
| `Framework/FishHelper.cs` | 250 | Fishing product behavior and direct Stardew state access |
| `Framework/Patches.cs` | 417 | Five direct Harmony patch targets and patch callbacks |
| `Framework/SFishingRod.cs` | 117 | Product wrapper over native `FishingRod` |
| `Framework/Enums.cs` | 20 | Product types |
| `Framework/GenericModConfigMenu.cs` | 450 | This Mod's GMCM page/option declarations |
| `Framework/ModConfig.cs` | 101 | This Mod's configuration |
| **Total** | **1,474** | **All feature-specific code stays in the Mod** |

The project sets `<EnableHarmony>true</EnableHarmony>`. `ModEntry.Entry` constructs `Harmony` with the Mod unique ID, initializes patches, subscribes to SMAPI events, and registers its own GMCM page. The patch class directly targets:

- `FishingRod.tickUpdate`;
- `BobberBar.update`;
- private `FishingRod.doDoneFishing`;
- `GameLocation.GetFishFromLocationData`;
- `FishingRod.pullFishFromWater`.

This is the relevant architectural comparison:

```text
SMAPI
  owns platform lifecycle, events, registry, logs, config and compatibility tooling

YetAnotherFishingMod
  owns product policy, game objects, Harmony patches and its GMCM option declarations
```

SMAPI does not gain 1,474 lines of feature code because this Mod exists. That is the base-weight property DTMAPI currently lacks.

### What the Mod actually wrote to be a SMAPI Mod

The previous comparison established physical ownership, but it did not separate the small SMAPI integration surface from the product's own implementation. Rechecking the same pinned tag gives a sharper answer.

SMAPI compliance itself requires only three semantic pieces:

1. a `manifest.json` that declares identity, version, entry DLL and minimum SMAPI version;
2. a class derived from `StardewModdingAPI.Mod` with `Entry(IModHelper)`;
3. a build reference to SMAPI, normally supplied by `Pathoschild.Stardew.ModBuildConfig`.

At this tag those pieces are represented by a 20-line manifest, the `ModEntry : Mod` / `Entry(IModHelper)` declarations, and the 25-line project file. The project file's `<EnableHarmony>true</EnableHarmony>` is not a SMAPI gameplay API and is not required for every SMAPI Mod: it tells the generic build package to provide the Harmony reference. The translation class builder is likewise build tooling, not an AutoFishing implementation supplied by SMAPI.

`ModEntry.cs` is 119 lines, but it must not be counted as 119 lines of mandatory framework adapter. It also contains this product's event routing, native item enumeration, automatic-cast toggle behavior, GMCM registration and config reload policy. A lexical audit of all 1,474 feature-specific C# lines, excluding `using` directives, comments and blank lines, found only 54 physical lines that directly name or invoke SMAPI types, inherited SMAPI members or utilities:

| Source file | Total lines | Direct SMAPI-reference lines | What those lines do |
| --- | ---: | ---: | --- |
| `ModEntry.cs` | 119 | 26 | entry, identity, services, five event subscriptions, context/input checks, config and logging |
| `Framework/Patches.cs` | 417 | 17 | `IMonitor`/`IReflectionHelper` plumbing and 14 patch-failure log calls |
| `Framework/FishHelper.cs` | 250 | 7 | four `PerScreen<T>` state holders, one reflection lookup and SMAPI log-level typing |
| `Framework/ModConfig.cs` | 101 | 2 | two `KeybindList` configuration values |
| `Framework/GenericModConfigMenu.cs` | 450 | 2 | receive SMAPI manifest/registry services and query the external GMCM API once |
| `Framework/Enums.cs` + `Framework/SFishingRod.cs` | 137 | 0 | product/game types only |
| **Total** | **1,474** | **54 (3.7%)** | **SMAPI is a narrow generic host surface, not the fishing engine** |

This is a lexical boundary count, not a claim that exactly 54 lines are mandatory. It intentionally includes declarations and parameter types, while excluding generated `I18n` source and calls to the separate GMCM Mod API. Its purpose is to bound the integration surface reproducibly.

The 54 lines use ten generic SMAPI service families:

| Generic SMAPI family | Actual use in Yet Another Fishing Mod 1.2.0 |
| --- | --- |
| Host entry and identity | derive from `Mod`, implement `Entry(IModHelper)`, read `ModManifest.UniqueID` for the Harmony owner |
| Events | five subscriptions: game launched, update ticked, one-second update, menu changed and buttons changed |
| Config | two `ReadConfig<T>` calls and one `WriteConfig` callback |
| Translation | pass `helper.Translation` into the generated `I18n` class |
| Logging | 17 `IMonitor.Log` calls: 14 patch diagnostics and three product status/config messages |
| World readiness | two `Context.IsWorldReady` checks |
| Reflection | one actual `GetField<SparklingText>` lookup; the reflection helper stored by `Patches` is unused at this tag |
| Per-screen state | four `PerScreen<T>` instances for multiplayer/split-screen-local fishing state |
| Input utilities | two `KeybindList` values and two `JustPressed()` checks |
| Mod registry | one `GetApi<IGenericModConfigMenuApi>` call to locate optional GMCM |

None of these ten families knows how to cast, hook, skip a minigame, change fish quality, preserve bait, accelerate animation or loot treasure. They are the same host services that unrelated Mods can use.

### What SMAPI did not provide

The author wrote and owns the actual fishing implementation:

- `FishHelper.cs` + `Patches.cs` + `SFishingRod.cs` + `Enums.cs`: 804 lines of product behavior, native Stardew state access, wrapper state and Harmony patch logic;
- five direct Harmony patch registrations targeting Stardew methods;
- 101 lines of AutoFishing configuration schema;
- product coordination within the 119-line entry class;
- 450 lines declaring this product's optional GMCM form.

The 450-line GMCM form makes 59 calls across eight methods of the separate `spacechase0.GenericModConfigMenu` API: `Register`, `AddPage`, `AddPageLink`, `AddSectionTitle`, `AddKeybindList`, `AddBoolOption`, `AddNumberOption` and `AddTextOption`. SMAPI contributes only the generic `ModRegistry.GetApi` lookup and the manifest/config services. GMCM renders the UI; neither SMAPI nor GMCM supplies fishing behavior.

The project also imports two author-shared files through `Common.projitems`: a 177-line GMCM interface declaration and an 18-line HUD notifier. Therefore 1,474 is the feature-specific source total; the compiled project includes another 195 lines of author-shared C# before generated translation code, for 1,669 authored physical C# lines. This nuance increases the amount owned by the Mod and does not add anything to SMAPI Core.

The only fishing-named Runtime source found in local SMAPI is the 55-line `FishingRodFacade`, whose documented purpose is rewriting Stardew 1.5.6 `FishingRod` members to their newer form for old Mod compatibility. It is a cross-version framework compatibility shim, not an automatic-fishing service. The SMAPI metadata entry for Yet Another Fishing Mod is compatibility/update metadata, not product implementation.

### Consequence for DTMAPI

The useful SMAPI analogy is therefore not “DTMAPI must first implement a fishing API.” It is:

```text
DTMAPI supplies
  managed discovery + Entry + manifest/version/dependencies
  logging + config + translation + events/input + Mod registry
  bundled GMCM host
  Advanced build/load policy for Harmony/game references

AutoFishing supplies
  fishing state and decisions
  native game access and Harmony patches
  caches, transactions, animation/input behavior and cleanup
  AutoFishing option declarations
```

An `IFishingPrimitivesApi` is justified only if a second independent product needs a stable shared fishing primitive and both consumers share the same native owner/conflict boundary. It is not required merely to let AutoFishing remain DTMAPI-managed.

### GMCM nuance

Stardew's Generic Mod Config Menu is itself a separate Mod, not SMAPI Core. DTMAPI deliberately chose to bundle the equivalent capability. That difference is acceptable and should remain:

```text
DTMAPI base/bundled platform
  owns the generic config-menu registry, transactions, widgets/pages and Unity UI host

AutoFishingMod
  owns labels, descriptions, ranges, defaults and save/reset delegates for AutoFishing
```

The 450-line `GenericModConfigMenu.cs` in the Stardew sample is not GMCM's implementation; it is that product's configuration form. The equivalent AutoFishing registration belongs in AutoFishing, while the DTMAPI GMCM implementation remains in DTMAPI.

## Current DTMAPI AutoFishing footprint

Current dirty-worktree physical C# lines, counted with `Get-Content.Count` on 2026-07-19:

| Current owner | Files | Lines | Classification |
| --- | ---: | ---: | --- |
| `first-party-mods/AutoFishingMod` | 3 | 512 | Product policy/config/entry; stays with product |
| `Features/FishingAutomation` | 18 | 5,403 | Mostly ProductNative; currently in the wrong player assembly |
| `Compatibility/FishingAutomation` | 3 | 3,007 | Frozen old-ABI executor/facades; transitional only |
| **Directly named fishing total** | **24** | **8,922** | Excludes shared host/callback/demand scaffolding and optional QA |

The prior parent review used a different line-count method and an earlier dirty-tree snapshot. The current physical number is recorded here only to make later before/after acceptance reproducible; the ownership conclusion is unchanged.

Additional fishing-specific code is embedded in shared files:

- approximately 150 lines of first-party primitive owner/session facade logic in `OwnerBoundGameBridgeApis.cs`;
- the fishing callback family and demand guards in `DolocTownHookCallbacks.cs`;
- GameBridge feature fields, status properties, construction, API registration, Hook installation and lifecycle forwarding;
- fishing and legacy-updater demand routes, retained-callback bits, patch readiness checks and no-demand metrics;
- fishing-specific owner cleanup, diagnostics publication and source/architecture tests;
- optional QA orchestration, fixtures, performance probes and the production-side `FishingNativeControlQaSession` seam.

Therefore deleting only `Compatibility/FishingAutomation` would reduce weight, but it would not correct the central ownership error.

## Native owner and state-holder conclusion

The earlier native responsibility review remains valid about which Doloc Town owners perform the effect. The relevant native targets/state include:

- `BodyController.UseFishRod` and selected native fishing rod/pool availability for cast admission;
- `AgentStateFishingReady`, `AgentStateFishingCast`, `AgentStateFishingWait` and `AgentStateFishingPull` for the state machine;
- `AgentStateFishingWait.RollFish`, `NextState` and native state overwrite for bite/reel progression;
- `FishingGameScrollBar.StartGame`, `UpdateGame`, `StopGame` and its status/note state for visible-minigame automation;
- `DolocUserInput` fishing/tool/item getters for synthetic input;
- `FishRodRenderer.CastHook`, `Pull`, `PullCancel`, Hook body/velocity/gravity and animation state for animation acceleration;
- native energy configuration/check/cost paths for safe casting and reeling;
- the current Agent/state-manager, fishing cache/proto, minigame handle, animator and Hook Rigidbody as live state holders.

Those findings prove where the product must interact with the game. They do **not** prove that the framework must own the interaction. Under the advanced managed lane, these responsibilities remain native-owned by Doloc Town and are adapted privately by AutoFishing.

## File-by-file disposition

### `KEEP-BASE`: generic DTMAPI and bundled GMCM

Keep these responsibilities in DTMAPI:

- BepInEx bootstrap and Unity lifecycle entry;
- Mod discovery under DTMAPI roots, manifest parsing, entry construction and dependency/version admission;
- official enable/disable state, selected source identity and restart-pending state;
- `DtmMod`, helper, events, input, logging, translation, config and cross-Mod API registry;
- generic owner/resource lifecycle accounting and diagnostics/report export;
- generic content-pack ownership and genuinely shared native adapters that pass the two-consumer/global-invariant test;
- `IDtmConfigMenuApi`, `DTMAPI.ModConfigMenu`, its 1,449-line current registry/page/item/transaction implementation, and the Bootstrap/reflected UI host used to render it;
- the DTMAPI Manager UI for ordinary players.

Keep `HarmonyReflectionPatcher` and `AgentStateLifecycleHookBridge` only for whatever legitimate base/shared responsibilities still use them. AutoFishing must stop adding routes to them. After fishing moves:

- remove the fishing branch/demand from the shared `AgentStateBase.OnExit` route;
- keep the shared lifecycle file only while ActionSpeed/ActionCompletion or a real global invariant still needs it;
- do not expose the current internal GameBridge patcher as AutoFishing's new dependency.

### `MOVE-PRODUCT`: move responsibility into `AutoFishingMod`

The following files are product-native implementation and should leave mandatory GameBridge. They may initially move with minimal behavioral change, then be simplified inside the product:

| Current file | Target disposition |
| --- | --- |
| `FishingAnimationController.cs` | Move; private AutoFishing Ready-charge adapter. |
| `FishingAnimationNativeCache.cs` | Move; private animation/Hook native cache. |
| `FishingAutomationHookBridge.cs` | Move responsibility; rewrite it to patch AutoFishing-owned callbacks with a Mod-unique Harmony owner. |
| `FishingInputOverride.cs` | Move; product synthetic-input state. |
| `FishingMiniGameNativeCache.cs` | Move; product minigame state adapter. |
| `FishingNativeAccessors.cs` | Move or replace with typed/Harmony accessors; never retain merely as a framework service for one product. |
| `FishingNativeAdapter.cs` | Move; cast/bite/reel adapter. |
| `FishingNativeEnergyGate.cs` | Move; AutoFishing admission/safety rule. |
| `FishingNativeStateCache.cs` | Move; product frame/state cache. |
| `FishingNativeTransactionCache.cs` | Move; product native mutations. |
| `FishingPrimitiveHookRuntime.cs` | Move/fold; it is the AutoFishing callback implementation. |
| `FishingRuntimeComponents.cs` | Move/fold; scheduler, diagnostics and lifecycle publication are product state. Use generic DTMAPI logging/diagnostics instead of a framework fishing channel. |
| `FishingVisibleReelInputState.cs` | Move; product retry/acceptance state. |
| fishing callback methods in `DolocTownHookCallbacks.cs` | Move behavior into an AutoFishing callback type, then delete the base callbacks and `Bridge?.FishingAutomationCallbackService` dispatch. Keep safe fallbacks locally. |

`FishingPrimitivesService.cs` must be split by responsibility, not copied unchanged:

- preserve and move the state machine, snapshots, sequence checks, native transactions, input policy, animation policy and Hook router needed by the product;
- collapse the one-consumer `AcquireSession(IManifest)`, provider registration, owner-bound facade and cross-assembly lease protocol into direct private product ownership;
- keep internal interfaces only where they materially improve AutoFishing testing; do not keep them in `DTMAPI.Abstractions`.

`IFishingHookRuntime.cs` and `IFishingHookCoordinator.cs` leave GameBridge. They may become private product test seams or disappear after the callback/activation layers are collapsed.

The first migration should prioritize ownership over stylistic rewrite. Existing reflection/cached-delegate behavior can move first; direct typed game references may replace it only where they materially reduce code or failure modes. The new lane permits direct references but does not require every access to become typed immediately.

### `DELETE-SCAFFOLD`: remove code created solely by the old restriction

Delete the following after the new product path is working:

- `src/DTMAPI.Abstractions/FirstPartyFishingPrimitives.cs`;
- `[assembly: InternalsVisibleTo("AutoFishingMod")]` in Abstractions;
- `OwnerBoundGameBridgeApis.ForFirstPartyFishingPrimitives`, `FirstPartyFishingPrimitivesFacade`, `FirstPartyFishingSessionFacade`, and their handler/session tracking;
- AutoFishing's `GetApi<IFirstPartyFishingPrimitivesApi>("DTMAPI.GameBridge.DolocTown")` acquisition/retry path;
- AutoFishing's required manifest dependency on `DTMAPI.GameBridge.DolocTown` when no other GameBridge public API is used;
- GameBridge's `FishingAutomationFeature` field, construction, API registration, first-party provider registration, lifecycle forwarding and callback-service property;
- `FishingAutomationFeature` primitive/legacy arbitration, activation checkpoints and demand plumbing after compatibility is separated;
- `GameBridgeDemandRoutes.FishingAutomation`, `FishingLegacyUpdater`, fishing retained-callback bits, fishing/BaseExit co-owned demand, fishing Hook-readiness probes and fishing-only no-demand counters;
- fishing-specific entries in `GameBridgeModOwnerCleanupParticipant`, hook-status initialization and other base diagnostics;
- source gates that assert AutoFishing must not contain Harmony, reflection, Unity, game types or GameBridge code;
- tests that exist only to enforce friend-only primitive access or an Abstractions-only AutoFishing project;
- the README statement that native fishing must be GameBridge-owned.

Do not delete generic demand coordination, event cleanup, diagnostics, Mod registry owner binding, or GMCM just because fishing used them. Delete the fishing specialization, not the reusable platform mechanism.

### `MOVE-QA`: optional product QA, never mandatory Runtime

Move or rewrite these as AutoFishing-owned optional QA/test assets:

- `FishingNativeControlQaSession.cs`;
- `AutoFishingFixtureCase.cs`;
- `AutoFishingPrimitiveFixtureCase.cs`;
- `LegacyFishingAutomationCompatibilityFixtureCase.cs` while compatibility remains;
- `AutoFishingNativeControlFixturePolicy.cs`;
- `AutoFishingNativeVitalsCommandAdapter.cs`;
- AutoFishing-specific performance orchestration/probes/contracts.

The production AutoFishing assembly may expose a narrow internal/friend test seam if needed. DTMAPI's mandatory GameBridge must not own product scenario policy or a product QA-control session. The framework test suite should retain only black-box loader/admission/restart/cleanup checks that apply to every advanced Mod.

### `TRANSITION-COMPAT`: remove from the final base, but honor the recorded bridge gate

Current compatibility files:

- `Compatibility/FishingAutomation/LegacyFishingAutomationService.cs` — 2,903 physical lines;
- `FishingCompatibilityController.cs` — 35 lines;
- `OwnerBoundFishingAutomationApi.cs` — 69 lines;
- `IFishingAutomationApi`, `FishingAutomationOptions`, state and strategy DTOs in Abstractions.

They should not remain permanent mandatory GameBridge architecture. They also cannot be silently deleted from a `0.5.5` bridge build while the exact old published AutoFishing DLL is a known consumer and the current matrix promises a warning-bearing preview cycle.

Recommended two-release treatment:

1. **0.5.5 bridge release**
   - ship the new self-contained AutoFishing product;
   - keep the frozen public DTO/signature surface for binary binding;
   - add clear deprecation/Manager/Doctor guidance;
   - preferably move the 3,007-line executor/facades into an optional compatibility component loaded only for the old consumer; if that loader split is not ready, retain them temporarily and state that 0.5.5 has not completed the base-weight cleanup;
   - do not add a second new fishing compatibility API.
2. **0.6.0 breaking cleanup, after the promised gate**
   - confirm no supported package still needs `IFishingAutomationApi`;
   - publish migration guidance and complete at least one warning-bearing preview cycle;
   - delete the legacy executor/facades, public legacy DTO/interface and compatibility-only tests/status/demand paths;
   - keep archived source/history in Git and docs, not in player Runtime.

If the user explicitly chooses a hard `0.5.5` break, that is a new compatibility decision and must revise the current public API matrix/release promise. This review does not silently make that decision.

## Replacement rule for managed Mods

Replace the universal restriction with three explicit lanes:

| Lane | Entry and location | Native references | DTMAPI management promise |
| --- | --- | --- | --- |
| Strict Managed CodeMod (default) | `DtmMod` under DTMAPI-managed `Mods` | `DTMAPI.Abstractions` only; current ban remains | Strongest SDK guidance, dependency/version checks, config/log/i18n/GMCM, owner-bound APIs and cleanup where contracts support it |
| Advanced Managed CodeMod | `DtmMod` under the same DTMAPI-managed `Mods` | May reference provided Unity modules, `0Harmony`, and game assemblies | Discovery, cold-start enablement, manifest/version/dependencies, logs/config/i18n/GMCM/Manager/Doctor; restart-required after load; Mod owns patches/native cleanup and version coupling |
| External BepInEx plugin | `BaseUnityPlugin` under `BepInEx/plugins` | Direct | Detected/reported only; not loaded, enabled, disabled, version-admitted, or lifecycle-managed as a DTMAPI Mod |

Advanced-lane minimum constraints:

- remain `netstandard2.0` under Unity Mono;
- keep one declared DTMAPI entry assembly initially; game/Unity/Harmony references are provided references and must not be copied into the Mod package;
- use a unique Harmony owner derived from the Mod unique ID;
- declare the advanced/native execution model in the manifest once the field name is designed;
- Manager and Doctor show native-access, game-version-coupling and restart-required warnings;
- failure in one advanced Mod must be isolated/logged without converting it to a BepInEx root plugin;
- cold-start disabled means its entry assembly is not loaded and its patches are not installed;
- post-load disable is pending restart unless the Mod supplies and passes a reviewed cleanup contract;
- no stable DTMAPI public API exposes raw Unity/game/Harmony types.

The current `SDK160` check becomes a strict-lane rule. It must not reject an explicitly declared advanced project. The current text scan is also insufficient as a security boundary; the advanced design should use project/assembly metadata validation and truthful warnings, not pretend native code can be made safe by token filtering.

## AutoFishing target shape

```text
BepInEx
  -> DTMAPI Bootstrap
      -> DTMAPI Core loader / events / config / logs / registry / Manager
      -> bundled DTMAPI ModConfigMenu
      -> AutoFishingMod : DtmMod, advanced managed
           -> product config + GMCM registration
           -> decision engine
           -> Harmony owner unique to AutoFishing
           -> private Doloc Town fishing adapters/caches/transactions
           -> product lifecycle cleanup and diagnostics
```

AutoFishing remains fully visible to DTMAPI. It does **not** return to `BepInEx/plugins`, and it does **not** need a feature engine in GameBridge to be manageable.

Expected project references for the first implementation are conceptually:

- `DTMAPI.Abstractions`;
- game/Unity reference assemblies needed by the chosen typed implementation, with copy-local disabled;
- the Runtime-provided `0Harmony`, with copy-local disabled.

The packaged product should still contain one AutoFishing CodeMod DLL plus manifest/i18n/assets. “One CodeMod DLL” must mean one product entry assembly, not “the source may only know Abstractions.”

## Rejected alternatives

### Keep a thin Mod and publish Fishing Primitives

Rejected. It stabilizes a one-consumer product protocol, leaves the 5,403-line native owner in every player's GameBridge, and creates long-term ABI cost. Making the internal interface public would make the ownership error harder to remove.

### Move the files to another mandatory DTMAPI companion DLL

Rejected as the end state. It may reduce one assembly's size but not the mandatory player Runtime or one-Mod-per-feature scaling. A genuinely optional compatibility/QA component is different because it is absent when unused.

### Remove the strict lane entirely

Rejected. Most content/config/event Mods benefit from stable APIs, low coupling and stronger validation. The problem is the universal ban, not the existence of an API-only path.

### Require advanced Mods to install under `BepInEx/plugins`

Rejected. That recreates the exact unmanaged installation/documentation problem the current project is trying to solve.

### Promise live unload for arbitrary native Mods

Rejected. CLR assembly unload is unavailable in this Unity Mono architecture, and Harmony unpatch alone cannot prove that all static/native/event state was reverted. Restart semantics are the honest default.

## Implementation order

1. Freeze new AutoFishing-specific GameBridge/demand growth except critical correctness fixes and current evidence closure.
2. Design the advanced manifest, loader admission, AssemblyResolve/reference model, SDK, Doctor and Manager states without weakening the strict lane.
3. Make AutoFishing compile and package as one advanced DTMAPI CodeMod DLL. First preserve behavior; do not simultaneously redesign every fishing mechanic.
4. Move the native files/callbacks and collapse the internal primitive/session boundary.
5. Remove the first-party friend API, provider facade, GameBridge feature host, fishing demand routes, cleanup branches and architecture gates.
6. Move product QA to optional AutoFishing QA and keep only generic advanced-Mod platform tests in DTMAPI.
7. Complete the `0.5.5` compatibility preview/optionalization decision, then delete frozen fishing compatibility at the approved breaking release.
8. Re-baseline mandatory Runtime LOC/DLL size, disabled startup work, Hook owners, owner roots and long-run GC evidence before using AutoFishing as the Batch 6 template.

## Acceptance gates for the later implementation

### Static/package

- AutoFishing is discovered from a DTMAPI-managed Mod package and still derives from `DtmMod`.
- The product package contains one AutoFishing entry DLL and does not bundle `Assembly-CSharp`, Unity modules, Harmony or BepInEx.
- AutoFishing's manifest declares the advanced/native lane and restart semantics.
- Disabling AutoFishing before launch prevents its assembly and Harmony owner from loading.
- The strict template still fails direct native references; the advanced template accepts declared/provided references.
- `DTMAPI.Abstractions` has no public raw Unity/game/Harmony types.
- No `IFirstPartyFishingPrimitivesApi`, AutoFishing friend access, first-party provider facade or AutoFishing GameBridge dependency remains.
- Mandatory GameBridge contains no AutoFishing native implementation/callback/demand/QA types, except explicitly time-bounded compatibility retained for the bridge release.
- `DTMAPI.ModConfigMenu` remains bundled and AutoFishing registers/edits/saves its own settings through it.

### Behavioral/runtime, required later but not run here

- clean cold-start enabled/disabled matrices;
- cast-charge, instant bite, native skip result, visible minigame, animation speed, movement cancel and energy accounting;
- save load, returned-to-title, re-entry and clean process exit;
- unique Harmony owner and zero duplicate patch registration after lifecycle transitions;
- Manager/Doctor restart-required and native-risk presentation;
- exact old binary compatibility during the promised bridge window;
- bounded owner/native roots and valid long-run GC evidence without high-frequency diagnostic observer effect.

## Blockers and downstream documents

Implementation must update, at minimum:

- `AGENTS.md`, `PROJECT.md` and `docs/workflows/codex-api-rebuild.md` so “fragile native code usually belongs in GameBridge” is not interpreted as a universal product ban;
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md` before AutoFishing is used as a Batch 6 template;
- Author SDK validation/templates/docs, especially `SDK160`;
- manifest schema, loader admission/restart state, package validation, Doctor and Manager UI;
- AutoFishing project/manifest/README/source and its optional QA/tests;
- `docs/api/public-api-matrix.md`, Hook Map, Debug/smoke records and the owning implementation Update only when their facts actually change.

This review is the pre-implementation boundary. It is not authorization to delete the frozen public compatibility surface immediately or to edit the current dirty Batch 5 runtime without a separate implementation Update.
