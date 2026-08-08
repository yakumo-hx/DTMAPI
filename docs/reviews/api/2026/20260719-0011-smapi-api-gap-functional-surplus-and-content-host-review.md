# 20260719-0011 SMAPI API Gap, Functional Surplus And Content Host Review

Status: recorded / direction correction proposed

Date: 2026-07-19

Scope: code-level SMAPI comparison, current DTMAPI author API gaps, player-Runtime functional surplus, managed ContentPack semantics, and the JSON/PNG/WAV livestock bridge boundary

Owning Update: [20260719-0001 DLL Mod Entry Model Audit](../../../updates/2026/20260719-0001-dll-mod-entry-model-audit.md)

Prior boundary reviews:

- [20260705-0003 Custom Animal Content Boundary Review](20260705-0003-custom-animal-content-boundary-review.md)
- [20260705-0004 SMAPI-Style Boundary Split Research](20260705-0004-smapi-style-boundary-split-research.md)
- [20260719-0010 AutoFishing SMAPI Rehome Boundary Review](20260719-0010-autofishing-smapi-rehome-boundary-review.md)
- [20260719-0008 DLL Mod Entry And Migration Boundary Audit](../../code/2026/20260719-0008-dll-mod-entry-and-migration-boundary-audit.md)

SMAPI reference: `E:\Python_project\SMAPIlearning\SMAPI`, `develop` at `5689c8d6` (`4.5.2-54-g5689c8d6`)

## Source Request

The user asked for a larger, code-level assertion answering two questions:

1. Which framework APIs does DTMAPI actually lack when compared with the useful SMAPI responsibility model?
2. Which current DTMAPI APIs and GameBridge implementations are actually functional-Mod/product code rather than framework capability?

The user also identified the current JSON/PNG/WAV livestock packs as an important counterexample: they contain no DLL, but DTMAPI discovers, enables, diagnoses, displays and drives them through a shared animal bridge. This review determines whether that bridge is valid DTMAPI platform work, product work, or a separate content-host responsibility.

This is a static source and local-package review. It does not change the current loader, SDK, public API, package format, native hooks or Runtime behavior. It does not launch Doloc Town.

## Executive Assertion

The current DTMAPI boundary is inverted.

- DTMAPI already has a substantial loader and management kernel: manifest discovery, dependency ordering, source/Workshop identity, official enablement, logging, config, events, input, translation, Mod Registry, owner cleanup, Manager UI, bundled GMCM-class config UI and diagnostics.
- DTMAPI still lacks several ordinary framework services that make a loader usable as an author platform: an explicit content-pack owner relationship, owner-scoped content-pack/file access, general Mod data/save data, game/mod content loading and invalidation, command registration, reflection/advanced patch authoring, richer lifecycle events, a typed version contract, active update checks and an advanced managed CodeMod lane.
- At the same time, DTMAPI exposes and implements many full player features: fishing automation, action speed, one-action completion, extra equipment slots, more saves, chest location enhancement, strong planting gun, fish-roe display, animal-viewer policy, machine production and Y-console cheat/debug verbs.
- Therefore the problem is not “DTMAPI has too few APIs”. The problem is **too few boring framework APIs and too many gameplay-shaped APIs**.
- The universal rule “every DTMAPI-managed Mod must be Abstractions-only and cannot reference Unity, Harmony or the game assembly” must be cancelled as a universal admission rule. Keep it as the default strict lane, but add an advanced managed CodeMod lane. Keep `DTMAPI.Abstractions` itself free of raw Unity/Harmony/game types.
- A no-DLL ContentPack is still fully manageable. “Managed by DTMAPI” means DTMAPI owns discovery, identity, version/dependency checks, enablement, diagnostics and Manager presentation; it does not mean every package has a `DtmMod` entry.
- The livestock bridge is reusable and not species-specific, so it is not Hatch/Mole/Drecko/Oilfloater product code. However, under the SMAPI responsibility model it should become an optional official content host such as `DTMAPI.CustomAnimals`, not remain an always-present assumption inside the base GameBridge.

The intended target is:

```text
DTMAPI Core
  -> generic CodeMod and ContentPack management
  -> strict managed CodeMods
  -> advanced managed CodeMods
  -> optional official content hosts
       -> DTMAPI.CustomAnimals
            -> third-party/first-party JSON + PNG + WAV animal packs
  -> bundled GMCM-class configuration UI
```

## Current Code-Weight Evidence

The following is a physical C# line snapshot of the current dirty worktree on 2026-07-19. It excludes `bin` and `obj`; it is a structural measure, not a performance or GC conclusion.

| Player Runtime project | Files | Lines |
| --- | ---: | ---: |
| `DTMAPI.Abstractions` | 13 | 3,834 |
| `DTMAPI.Core` | 46 | 23,866 |
| `DTMAPI.BepInExBootstrap` | 8 | 6,636 |
| `DTMAPI.GameBridge.DolocTown` | 87 | 41,286 |
| `DTMAPI.ModConfigMenu` | 4 | 1,449 |
| **Total** | **158** | **77,071** |

The public surface is not small:

| Abstractions source | Lines | Public types | Public interfaces | Boundary finding |
| --- | ---: | ---: | ---: | --- |
| `ExperimentalGameBridge.cs` | 1,323 | 104 | 25 | Mostly gameplay/debug/product-shaped contracts |
| `CustomEntities.cs` | 964 | 97 | 9 | Large speculative entity model whose native creation verbs remain blocked |
| all Abstractions | 3,834 | 267 | 70 | Public compatibility burden is already broad despite missing framework helpers |

The GameBridge feature directory contains 25,869 lines. At least 16,249 lines are in clearly product-shaped named feature directories:

| Product-shaped feature | Lines | Recommended owner |
| --- | ---: | --- |
| Fishing automation | 5,403 | AutoFishing product |
| Extra equipment slots | 2,814 | MoreEquipmentSlots product |
| Machine production | 1,468 | Mine/product or a later optional multi-pack machine host |
| Action speed | 1,400 | ActionSpeed product |
| Animal viewer | 1,097 | AnimalHusbandryProgress product |
| Strong planting gun | 1,019 | StrongPlantingGun product |
| Save slots | 885 | MoreSaves product |
| Crop harvesting | 644 | AutoHarvest product first; extract a transaction only after real reuse |
| Action completion | 612 | OneActionComplete product |
| Chest locator enhancement | 542 | ChestLocatorEnhancer product |
| Fish-roe tooltip | 365 | FishBreedingAssistant product; later replace with a generic tooltip host if justified |
| **Subtotal** | **16,249** | **Not base framework by default** |

That subtotal does not include:

- 3,070 lines under `Compatibility`, principally the retained legacy fishing executor and facades;
- 2,699 lines under `Diagnostics`, including powerful Y-console gameplay/debug operations;
- 7,156 lines for CustomAnimals plus AudioReplacement content-domain hosts;
- the 1,323-line public product DTO/API surface;
- the 2,273-line blocked CustomEntity declaration/registry/facade stack (`CustomEntities.cs` 964 + registry 1,092 + owner facade 217).

Not every line above should be deleted. The point is that these responsibilities must not all remain part of the mandatory base Runtime.

## What The SMAPI Code Actually Does

This comparison uses the local SMAPI source directly and does not copy its implementation.

### Mod entry and author ownership

`src/SMAPI/Mod.cs` defines a Mod as `Entry(IModHelper)`, optional `GetApi` and `IDisposable`. The Mod owns its behavior. SMAPI supplies the lifecycle and helper container; it does not contain the behavior of every fishing, automation, save-slot or cheat Mod.

`src/SMAPI.ModBuildConfig/build/smapi.targets` gives Mod projects direct references to the game assembly, game data, MonoGame, xTile and SMAPI. Harmony is an explicit opt-in reference through `EnableHarmony`. This is decisive evidence that SMAPI's “managed Mod” concept does not mean “the Mod may never reference the game or Harmony”.

The useful DTMAPI analogue is not to expose raw Unity/game objects through `DTMAPI.Abstractions`. It is to allow an advanced managed Mod assembly to compile against Runtime-provided native references while still entering through `DtmMod` and remaining subject to DTMAPI manifest, dependency, Manager and restart policy.

### The helper container is deliberately generic

`src/SMAPI/IModHelper.cs` exposes events, console commands, game content, Mod content, content packs, data, input, reflection, Mod Registry, multiplayer, translation and config. The corresponding concrete helper files under `Framework/ModHelpers` total about 1,255 lines and implement generic services rather than individual gameplay products.

DTMAPI's `IDtmHelper` currently exposes events, config, registry, Workshop, UI, diagnostics, a read-only content query, input and translation. The missing helper families are not theoretical; they are visible in the difference between these two concrete containers.

### Content packs have an explicit owner

SMAPI's `IManifest.EntryDll` and `ContentPackFor` are mutually exclusive. `IManifestContentPackFor` names the consuming Mod and optional minimum version. `ContentPackHelper.GetOwned()` returns only packs targeting that owner. `ContentPack` then provides safe relative file access, JSON read/write, translations and Mod-content loading.

SMAPI core does not interpret every domain schema. A code/content owner interprets its owned packs. This is the missing indirection in current DTMAPI animal/audio scanners.

### Data, content, commands and events are framework services

SMAPI's concrete `DataHelper` implements three distinct scopes:

- ordinary JSON under the Mod folder;
- owner-namespaced data in the current save;
- owner-namespaced global app data.

Its content helpers separate Mod-local assets from game assets and expose cache invalidation/content events. Its command helper registers generic commands whose behavior belongs to the Mod. Its current event interfaces expose 54 event declarations across game-loop, content, display, input, multiplayer, player, world and specialized groups.

DTMAPI currently declares 16 events and has no equivalent public data helper, command helper, content-pack owner helper, Mod-content loader, game-content edit/invalidation surface or reflection helper. DTMAPI does not need to copy all 54 SMAPI events because Doloc Town has different owners, but the missing capability families are real.

## The Animal Packs Prove That Management Does Not Require A DLL

The current local packages show the working model directly:

| Package | Current local version | Type | PNG | WAV | DLL |
| --- | --- | --- | ---: | ---: | ---: |
| Hatch | `1.0.0` | `ContentPack` | 44 | 2 | 0 |
| Mole | `0.5.3-alpha-oni-png-prototype` | `ContentPack` | 45 | 2 | 0 |
| Drecko | `0.1.0` | `ContentPack` | 36 | 2 | 0 |
| Oilfloater | `0.5.3-alpha-oni-png-prototype` | `ContentPack` | 48 | 2 | 0 |
| Shell Crab | `0.5.3-alpha-shell-crab-prototype` | `ContentPack` | 42 | 2 | 0 |

These local versions are historical/prototype state, not the intended unified 1.0.0 product contract.

Current source management chain:

1. Core discovers the manifest, source identity and official enablement.
2. A non-CodeMod is published into the loaded registry without calling `Assembly.LoadFrom` or `DtmMod.Entry`.
3. `ContentManifestRegistry` classifies `ContentPack`, `CustomAnimals`, `AudioReplacement`, `AnimalVoice` and official JSON capabilities.
4. Manager/diagnostics receive the same discovered/loaded/disabled/error identity rows used for CodeMods.
5. CustomAnimals and AudioReplacement consume only enabled loaded ContentPacks and load the fixed DTMAPI JSON files.
6. The bridge maps the pack's template IDs and asset paths into reviewed native animal/animator/AI/audio hooks.

The absence of a DLL only means the package has no executable entry. It does not remove identity, versions, dependencies, enablement or Manager ownership.

### Current healthy ownership

| Owner | Responsibility |
| --- | --- |
| Animal content pack | species ID, official JSON rows, items, shop/bag, products, economy, PNG frames, WAV files and author intent |
| Reusable animal bridge | schema validation, template animator mapping, PNG sprite mapping, template AI mapping, scoped AnimalVoice mapping, owner generation/isolation, lifecycle safety and native hook adaptation |
| Doloc Town | animal creation/release, growth, feeding, breeding, production, capacity, AI execution and save state |

The current `CustomAnimals` source contains no Hatch, Mole, Drecko, Oilfloater or Shell Crab identifier. That is strong evidence that it is reusable bridge code rather than species product policy.

### Required refinement: make the bridge an optional content host

Reusable does not automatically mean mandatory Core.

Under a SMAPI-style boundary, `DTMAPI.CustomAnimals` should be an explicit official content owner/provider:

```text
DTMAPI Core
  -> discovers AnimalPack
  -> verifies ContentPackFor/required dependency
  -> gives the pack to DTMAPI.CustomAnimals

DTMAPI.CustomAnimals
  -> reads custom-animals.json, frame manifests and AnimalVoice mapping
  -> owns the animal-specific native adapter and validation

DTMAPI.AnimalPack
  -> owns Hatch/Mole/Drecko/Oilfloater content and economy
```

Consequences:

- third-party animal packs remain no-DLL packages and remain visible/manageable through DTMAPI;
- players without custom animals do not need the animal bridge activated or physically loaded;
- the unified first-party AnimalPack can depend on `DTMAPI.CustomAnimals` without owning its generic bridge;
- provider absence or disablement must block the pack before its official JSON is admitted, so players never load half-working animal definitions;
- the loose PNG route is the normal host capability;
- Shell Crab's AssetBundle route remains a separate advanced extension/prototype and must not force AssetBundle lifecycle into the default animal host;
- general audio replacement can become a separate optional host later, while the animal host may consume only a narrow AnimalVoice service.

This keeps the animal author experience simple while removing its 3,716-line domain bridge and the relevant audio path from the mandatory base GameBridge.

## Missing DTMAPI Framework Capabilities

The priorities below are based on current author/product demand, not on copying SMAPI surface area.

| Priority | Missing or incomplete capability | Current evidence | Required direction |
| --- | --- | --- | --- |
| P0 | Advanced managed CodeMod lane | Author SDK supplies only netstandard references plus `DTMAPI.Abstractions`; `SDK160` rejects BepInEx/Harmony/Assembly-CSharp/UnityEngine source strings | Add a named advanced lane with Runtime-provided Unity/game/Harmony references, `DtmMod` entry, unique patch owner, Manager/Doctor visibility and restart-required disable/update semantics |
| P0 | ContentPack owner contract | Manifest has `Type=ContentPack` but no `ContentPackFor`; GameBridge scans every enabled pack directly | Add `ContentPackFor { UniqueID, MinimumVersion }`, mutual exclusion with `EntryDll`, dependency/order checks and an owner-scoped `GetOwned()` helper |
| P0 | Safe content-pack and Mod-local file API | `IContentQueryHelper` is a global read-only index and text reader; animal/audio hosts use internal filesystem scanners | Add safe relative file existence, typed JSON, translation and asset-loading helpers bound to the owning Mod/pack |
| P0 | Mod data and save data | Config exists, but no separate Mod JSON, current-save, global or session data helper exists | Add owner-namespaced data scopes with save-loaded guards, atomic writes and migration/version rules |
| P0 | Managed native cleanup/lifecycle contract | Core can remove DTMAPI-owned event/input/config/API roots, but `DtmMod` has only `Entry`; raw Harmony/statics/native objects become restart-required unknowns | Add an explicit dispose/shutdown/deactivation contract and advanced-lane patch ownership. Do not promise hot DLL unload |
| P0 | Unified version value/policy | Public manifest versions are strings; version parsing/comparison is duplicated; local Mod versions are inconsistent; `UpdateKeys` are inactive metadata | Introduce one DTMAPI semantic-version value/policy for current Mod version, minimum DTMAPI version, dependencies and SDK target; keep player-facing names about DTMAPI/API rather than an unexplained `runtime` noun |
| P1 | Generic command registration | DTMAPI has a large Y-console and many debug APIs but no ordinary author command helper | Add owner-bound command registration; keep cheat verbs and Y-console UI in an optional first-party product |
| P1 | Mod-content and game-content pipeline | Current content query cannot generically load/edit/invalidate game assets; domain scanners are bespoke | Separate Mod-local asset loading from reviewed game-content requests/edit/invalidation; build from Doloc native content owners, not from a Content Patcher clone |
| P1 | Reflection helper or advanced reference support | Original plan required cached friendly reflection; current strict SDK offers neither reflection nor native references | Provide a cached safe helper for strict Mods and direct reflection/Harmony for advanced Mods |
| P1 | Event coverage by native responsibility | Current 16 events omit content, render/display, player/world/room and many pre/post lifecycle boundaries | Add only events backed by reviewed Doloc native owners and actual Mod demand; do not mechanically reproduce all SMAPI events |
| P1 | Generic UI/HUD/tooltip host | Current product-specific tooltip and Y-console UI paths are not a general author surface | Build owner-scoped render/menu/tooltip primitives only after native UI lifecycle and conflict review |
| P1 | Managed dependency assemblies | Author SDK deployment rejects every CodeMod package containing more than its one EntryDll | Define whether advanced Mods may carry private managed dependencies, with resolver, hashes, collision policy and Doctor reporting; do not simply permit arbitrary DLL piles |
| P2 | Local Mod message bus | Cross-Mod API exchange exists, but no event/message bus | Add only if typed APIs are insufficient for real consumers |
| P2 | Active update checks and compatibility database | `UpdateKeys` are parsed but explicitly inactive | Implement after version authority and source ownership are stable |
| Future | Multiplayer | No Doloc Town multiplayer native model is established | Reserve; do not stub a misleading working API |

### Version naming assertion

Use one semantic-version implementation, but keep each field's meaning explicit:

| Location | Field | Meaning |
| --- | --- | --- |
| every Mod/ContentPack manifest | `Version` | this package's current version; first formal functional/content products may use `1.0.0` |
| every Mod/ContentPack manifest | `MinimumDTMApiVersion` | oldest DTMAPI API version the package supports |
| dependency row | `MinimumVersion` | oldest acceptable provider/Mod version |
| `ContentPackFor` | `MinimumVersion` | oldest acceptable content-host version |
| Author SDK metadata | `TargetDTMApiVersion` | exact DTMAPI compatibility surface used for this build; replace the current unexplained `targetRuntimeVersion` wording at the next schema boundary |
| DTMAPI product/runtime metadata | `ApiVersion` / player label `DTMAPI version` | installed DTMAPI framework version, currently targeting `0.5.5` for this update |

Do not introduce a generic public `RuntimeVersion` field whose owner is unclear. “Runtime” can remain an internal assembly/process concept, but author and player contracts should say DTMAPI/API directly.

## Functional And Speculative Surplus To Remove From The Base

| Current area | Classification | Decision |
| --- | --- | --- |
| `FishingAutomation` and first-party fishing primitives | AutoFishing product/native implementation | Move into advanced managed AutoFishing; retain old executor only for an explicit compatibility window, then remove |
| `ActionSpeed`, `ActionCompletion`, `StrongPlantingGun`, `SaveSlots`, `EquipmentSlots`, `ChestLocatorEnhancer`, `AnimalViewer` | Single-product functional code | Move into their product Mods; extract a shared primitive only after a second independent consumer proves reuse |
| `CropHarvesting` | Narrow useful native transaction with current product/QA consumers | Start product-owned; later extract a small transaction API if multiple real Mods need the exact reviewed native owner |
| `MachineProduction` | Mine/product engine presented as platform API | Prefer official JSON. Put remaining native engine in Mine or an optional machine content host only after multiple packs share one schema |
| `FishRoeTooltip` | Product-specific tooltip provider | Move to FishBreedingAssistant; later replace with a generic tooltip host rather than a fish-roe API in Core |
| playable Camera/Zoom capability | Currently a coherent lease but only one clear product owner | Move with Zoom unless a second independent Runtime consumer justifies a shared optional capability; keep only Manager/title UI repair that Core truly owns |
| Y-console debug/cheat verbs and reflected UI | Diagnostic product | Move to an optional first-party YConsole product; retain only generic command registration and support diagnostics in Core |
| CustomAnimals and relevant AnimalVoice bridge | Reusable domain content host | Keep as DTMAPI-provided capability, but move to optional `DTMAPI.CustomAnimals`, not mandatory Core/GameBridge |
| general AudioReplacement | Reusable but specialized domain host | Optionalize; split AnimalVoice from paper-box QA and future BGM work |
| `CustomEntities.cs` plus registry/facades | Speculative API surplus; runtime creation is blocked | Retire/internalize the misleading runtime verbs at the breaking boundary; keep research/native-owner records, not 97 public DTOs as a current framework promise |
| `UiDiagnostics` and generic owner cleanup | Platform safety/support | Keep, but strip product-specific branches when products move |
| bundled GMCM-class config UI | Platform author/user service | Keep in DTMAPI and continue improving its ordinary-user Manager/config experience |

## Replacement Managed Package Model

The replacement is four author/package classes under one Manager, not a single restrictive DLL rule:

| Class | Code | Native references | DTMAPI management | Disable/update expectation |
| --- | --- | --- | --- | --- |
| Managed ContentPack | none | none | Full discovery, version, dependency, enablement, Manager and provider ownership | Host-defined content refresh where safe; otherwise restart |
| Strict managed CodeMod | `DtmMod` | `DTMAPI.Abstractions` only | Full | DTMAPI-owned roots can clean; assembly changes still restart |
| Advanced managed CodeMod | `DtmMod` | May use Unity, game assembly, Harmony/BepInEx runtime references | Full identity/dependency/Manager/Doctor; direct native effects declared | Restart-required for disable/update unless explicit cleanup is proven |
| External BepInEx plugin | `BaseUnityPlugin` or arbitrary plugin | unrestricted | Detected/coexists, but not adopted as a DTMAPI Mod | Owned by plugin/BepInEx; restart by default |

“Full management” does not mean hot-unloading Mono assemblies. It means the package follows the official DTMAPI install, manifest, dependency, enablement, diagnostics and Manager path. Advanced side effects make same-process re-entry unsafe unless the Mod explicitly releases them.

The current Author SDK's exact-one-DLL deployment restriction can remain for the first strict/advanced version if all native dependencies are Runtime-provided. Private third-party dependency DLLs need a separate reviewed resolver/package policy; they must not be smuggled into the base Runtime or `BepInEx/plugins`.

## What The Lightweight Roadmap Must Change

The July 12 roadmap correctly separates QA, Compatibility, product policy and inactive demand. It is not sufficient under the corrected base-weight definition because it still assumes product native code must remain in GameBridge.

Required corrections:

1. “Must touch Unity/Harmony/game types” no longer automatically means “base GameBridge”. Ask whether the native code is shared platform capability or one product's implementation.
2. Phase 3 productization must move single-product native implementation with the advanced managed Mod, not leave a new feature engine behind in GameBridge.
3. Phase 5 must include physically optional content hosts and product-native assemblies, not only one large demand-activated GameBridge.
4. Batch 6 and later product migrations must not begin by creating another product-shaped public API. Each product needs a marginal base-cost statement: retained Core lines, retained shared bridge lines, product lines and compatibility lines.
5. The preferred extraction rule is evidence-first: keep code product-owned until at least two independent consumers and one common native owner justify a shared primitive.
6. The CustomAnimals host is the first no-DLL content-provider model; AutoFishing is the first advanced managed native-CodeMod model.

## Recommended Implementation Order

1. Freeze new product-shaped public APIs and record current consumers.
2. Implement one semantic version authority and settle manifest/SDK field wording.
3. Add `ContentPackFor`, `IContentPack`, safe owner-scoped file/JSON access and provider dependency blocking.
4. Add the advanced managed CodeMod SDK/loader/Doctor lane with restart-required semantics; do not broaden `BepInEx/plugins` placement.
5. Rehome AutoFishing native code using Review 0010 and remove its base-only scaffolding/compatibility after the declared gate.
6. Extract `DTMAPI.CustomAnimals` as an optional official content host and make the unified AnimalPack target it.
7. Move the remaining single-product feature domains in increasing native-risk order.
8. Add data/save, commands, content pipeline, reflection and event families based on real author demand.
9. Re-measure the five mandatory Runtime assemblies, optional hosts and no-consumer process before continuing Batch 6 productization.

## Safety Clause

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

Allowing advanced Mods to reference native types does not waive this rule for DTMAPI-owned stable APIs. A product may own a narrow patch with honest restart/conflict limits; DTMAPI may only promote a reusable primitive after its native owner, lifecycle and multi-Mod semantics are proven.

## Validation

Documentation/static review only. No runtime source was changed, no public API status was changed, no build or game smoke was run, and no local package was modified.

Checked:

- current DTMAPI Abstractions public type/interface counts;
- current five player-Runtime project line counts and GameBridge feature/compatibility/diagnostic breakdown;
- current `DtmMod`, `IDtmHelper`, events, manifest, content registry, non-code loading, owner cleanup and Author SDK validation/package rules;
- current CustomAnimals and AudioReplacement enabled-ContentPack scanner paths;
- current local Hatch/Mole/Drecko/Oilfloater/Shell Crab manifests and PNG/WAV/DLL counts;
- absence of first-party species identifiers from the CustomAnimals bridge source;
- local SMAPI `Mod`, `IModHelper`, helper implementations, manifest/semantic version/content-pack contracts, data helper, event interfaces and ModBuildConfig native/Harmony references;
- earlier SMAPI-style, functional-Mod, CustomAnimals, AutoFishing and Batch 5 boundary reviews.
