# 20260712-0003 DTMAPI Full Boundary Audit

Status: recorded
Date: 2026-07-12
Scope: pre-update, repository-wide boundary audit of runtime loading, QA, compatibility, first-party products, public APIs, versions, packaging, author guidance, and retained lifecycle/per-frame cost
Related Update: `docs/updates/2026/20260712-0008-dtmapi-full-boundary-audit.md`
Related roadmap: `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
Related issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` remains open; this audit does not reclassify it

User clarification and decision resolution: `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md` refines Oil/Mine as unpublished DeveloperOnly prototypes and closes the first decision round: native JSON Oil with no GameBridge/OneAction coupling, two-layer Mine, public DTMAPI 0.5.5, a formal functional-Mod 1.0.0 epoch with legacy compatibility, a pre-public naming/readability audit, and a lower crop API with no first-party AutoHarvest. The Oil ownership finding remains valid, but it is not a shipped-product regression. The focused compatibility review also found one existing Workshop binary member which must be restored before 0.5.5 release.

Second-round refinement and closure: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` selects append-only official Oil JSON as the first baseline, moves new-Workshop-Mod versus stale-installed-Runtime compatibility into the early release contract, removes AutoHarvest and late Oil code-product work from the functional-product sequence, and records OfficialLocal shadowing/official enablement timing gaps. Its A/B/C/D closure selects player Runtime-only uninstall plus Author-SDK receipt cleanup, player lifecycle loading plus SDK explicit reload/later dev watching, Catalog-first gradual migration toward a fully classified tree, and one independent versioned Author SDK with only a read-only player Doctor.

Third-round closure: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md` separates the 11 locally evidenced public-product identities from stale release labels and selects E1/F1/G1/H1/I1. The working official-JSON/PNG/WAV husbandry route becomes the formal animal promise while the C# umbrella freezes for compatibility; StrongPlanting is planned while Oil/Mine remain prototypes; Workshop is player authority with explicit Author-SDK overrides; 0.5.5 rolls out before a low-risk Canary and later one-by-one 1.0.0 products; public API retirement follows stability-tier warning windows.

Fourth- and fifth-round closure: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md` closes J1/revised-K1/L1/M1/N1 for the player Manager, protected MoreSaves behavior/API retirement, and optional Y-console extraction. `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md` closes O1/P2/Q1/revised-R1/S1/T0 for the four-species AnimalPack, custom-product/official-JSON industry chain, reviewed short-SFX route, Manbo migration Canary/C# API retirement, layered playback maturity, and no current BGM promise. These decision records supersede older program wording below where they are more specific; they do not authorize public assets/packages or API deletion by themselves.

Sixth-round closure: the animal U-Y draft in `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md` is deferred to the AnimalPack rebuild. The actual mainline sixth round in `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` is closed as U1/V1 revised/W1: single-consumer product APIs default internal with 0.5.5 ABI preservation, DTMAPI has one public 0.5.5 update followed by independent product waves, and the global questionnaire ends in favor of Batch 0/P0 implementation. The parallel AutoFishing/ActionSpeed active-gameplay GC gates are defined by `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`.

## Source Request

The user requested a full boundary audit before beginning a long-running major DTMAPI update. The result must be durable enough for later task branches and compacted Codex contexts to resume from one reviewed fact base instead of rediscovering project intent.

This review is evidence and ordering only. It does not move source files, change public API status, change Mod identity or versions, mutate Workshop/game files, launch Doloc Town, or claim that reduced source/package size fixes Unity/Mono GC behavior.

## Executive Verdict

The outer project dependency direction is substantially healthy:

```text
one BepInEx plugin entry
  -> Bootstrap
  -> Core + GameBridge + ModConfigMenu
  -> Abstractions
  -> ordinary DTMAPI Mods reference Abstractions only
```

The current weight is primarily an internal composition problem, not a reversed project-reference graph. `DTMAPI.GameBridge.DolocTown` combines production native adapters, product services, compatibility code, diagnostics, and a complete in-process Smoke harness. Core and Bootstrap also expose Smoke-specific seams. Most product feature hosts are created and their Hook/update routes are installed without consumer demand.

Two correctness boundaries must be fixed before structural movement:

1. Oil's `8%` coal-drop product rule is active in the base GameBridge without an OilMod owner/demand registration and is directly invoked by OneActionComplete.
2. Public author guidance tells third-party packs to create `dtmapi-package.json`, while the uninstaller treats the mere existence of that file as DTMAPI installer ownership.

The principal long-run order is therefore:

1. freeze public identities and protected behavior;
2. correct the two P0 ownership failures and missing product dependencies;
3. establish one version/product/release contract and player update diagnostics;
4. establish the real CodeMod author path and misplaced-DLL doctor;
5. extract QA through an optional dev-only assembly and remove the player hot-path Smoke probe;
6. measure and reduce recurring work, then make Hooks/updaters demand-driven;
7. productize and split functional Mods one domain at a time;
8. handle compatibility retirement only after a warning-bearing release window;
9. then undertake GMCM, MoreSaves, Y-console, animal-economy, audio, and future-platform feature work.

Directory movement, DLL-count reduction, or a successful UI smoke is not completion of any native API boundary.

Required safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Required Context And Authority

The audit reconciled current source with the required project/planning/debug/reference context, document governance, API rebuild workflow, public API matrix, current native-owner indexes, local-Mod demand reviews, third-party sample boundary, current version policy, current ISSUE-010 record, and the lightweight roadmap.

Canonical ownership remains:

- `docs/api/public-api-matrix.md` owns public API stability, not this review;
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` owns the recurring Fatal GC state, not this review;
- `docs/reviews/api/...` owns native-owner/API findings;
- this review owns the pre-implementation cross-boundary findings and ordered gates;
- each implementation batch must own its changed files, validation, and rollback in its own Update.

Several earlier findings are already superseded and are not reopened here:

- resident manifest version authority and bounded diagnostic scalars were completed by `docs/updates/2026/20260712-0003-owner-version-diagnostic-byte-bounds.md`;
- common owner activation/deactivation and late-Entry rollback were completed by `docs/updates/2026/20260711-0010-general-owner-lifetime-refactor.md`;
- Zoom's first-party owner boundary was verified by `docs/updates/2026/20260711-0013-first-party-zoom-owner-lifetime.md`;
- AutoFishing's public legacy facade is already Deprecated/Frozen while first-party primitives are internal, as recorded by `docs/reviews/api/2026/20260711-0001-autofishing-reliability-version-freeze-review.md`.

The lightweight roadmap remains orientation, not a current-fact ledger. Its earlier version/diagnostic prerequisite is complete; this audit adds the P0 ownership and release-contract prerequisites discovered from current source.

## Repository And Runtime Assembly Inventory

Physical source counts below exclude `bin`/`obj` and count current `.cs` lines. Release sizes are the current five production DLL outputs and are package-size evidence only, not runtime allocation evidence.

| Project/root | Project references | Files / lines | Release DLL bytes | Boundary verdict |
| --- | --- | ---: | ---: | --- |
| `DTMAPI.Abstractions` | none | 12 / 3,774 | 231,424 | Correct dependency leaf, but a very broad public surface. |
| `DTMAPI.Core` | Abstractions | 38 / 17,891 | 578,048 | Correct loader/service owner; Smoke and performance-probe seams remain mixed in. |
| `DTMAPI.ModConfigMenu` | Abstractions | 4 / 1,449 | 40,448 | Small declaration/registry layer; reflected player UI is physically in Bootstrap. |
| `DTMAPI.GameBridge.DolocTown` | Abstractions, Core | 97 / 50,983 | 1,524,736 | Main weight and composition boundary: production, product, compatibility, diagnostics, and Smoke are combined. |
| `DTMAPI.BepInExBootstrap` | all four production projects plus build stubs | 7 / 6,662 | 214,016 | The only BepInEx plugin entry, but it also owns large reflected product/framework UIs. |
| `DTMAPI.BepInExStubs` | none | 1 / 50 | not packaged | Build-only stub boundary. |
| `first-party-mods` | Abstractions only | 4 / 684 | separate Mod DLLs | Healthy compile boundary, but contains only AutoFishing and Zoom. |
| `testmods` | Abstractions only | 18 / 2,437 | separate Mod DLLs | Physically mixes published products, developer products, QA, examples, and negative fixtures. |
| `DTMAPI.UnitTests` | runtime projects plus AutoFishing | 1 / 11,138 | test-only | Broad source gate; does not replace runtime evidence. |

The GameBridge concentration is:

| Area | Files | Lines | Notes |
| --- | ---: | ---: | --- |
| `Features` | 61 | 24,914 | Product adapters/services, including Fishing, CustomAnimals, Equipment, Audio, Machine, Camera, ActionSpeed, UI repair, and others. |
| `Smoke` | 18 | 13,427 | About 26.3% of GameBridge source and compiled into the player DLL. |
| root partials | 7 | 4,037 | composition root, scheduler, core Hook routing, and update. |
| `Compatibility` | 3 | 3,077 | Almost entirely the frozen legacy fishing implementation. |
| `Diagnostics` | 2 | 2,765 | Game/native diagnostics. |
| `Hooking` | 5 | 2,414 | shared patch callbacks/coordinators. |
| `Native` | 1 | 349 | narrow native helpers. |

Additional concentration facts:

- 18 of the 23 files declaring the `DolocTownGameBridge` partial class are under `Smoke`; QA is physically part of the production class, not an external harness.
- `DolocTownExperimentalBridgeApi` spans root, diagnostics, machine, equipment, and cleanup partials and is approximately 7,889 lines; it is a major obstacle to physical feature extraction.
- Bootstrap is dominated by `ReflectedDebugConsoleUi.cs` (2,432 lines), `ReflectedTitleMenuSettingsUi.cs` (1,941), `ReflectedUnityInput.cs` (951), and `BootstrapPlugin.cs` (930).
- Abstractions contains 263 public class/interface/enum/struct declarations. `ExperimentalGameBridge.cs` contributes 104 and `CustomEntities.cs` contributes 97; those two files contain 201/263, or about 76.4%, of the declared public type surface.

All ordinary first-party/test Mod projects currently reference only Abstractions and target `netstandard2.0`. That compile boundary is healthy and must be preserved; product extraction must not push Unity, Harmony, reflected Doloc types, or raw decompiled types into ordinary Mods.

## Production Loading And Packaging Boundary

`tools/scripts/build-release-workshop-packages.ps1:134-140` and `tools/scripts/install-to-game.ps1:214-223` place five production assemblies under `BepInEx/plugins/DTMAPI`:

- `DTMAPI.BepInExBootstrap.dll`;
- `DTMAPI.Abstractions.dll`;
- `DTMAPI.Core.dll`;
- `DTMAPI.GameBridge.DolocTown.dll`;
- `DTMAPI.ModConfigMenu.dll`.

Only Bootstrap contains `[BepInPlugin]`; the other four are co-located runtime dependencies. Documentation that says only the Bootstrap *assembly* goes under `BepInEx/plugins` is inaccurate. The correct contract is: one BepInEx plugin entry and its four runtime dependency DLLs live in the DTMAPI runtime directory; ordinary DTMAPI CodeMods do not.

`ModScanner` discovers game `Mods`, official local `MODS`, and Workshop roots (`src/DTMAPI.Core/Manifesting/ManifestReader.cs:53-100`). First-party products are installed as official content under `MODS/<folder>/Content/DTMAPI`. QA fixtures require explicit `-InstallQaFixtures`. These boundaries are sound and should remain.

Current packaging gaps:

- no independent QA assembly exists, so all GameBridge Smoke code ships in the five-DLL player package;
- the tracked/ignored `dist/workshop-packages/DTMAPI` staging is 0.5.2-alpha from 2026-06-28 while current source/Release metadata is 0.5.3-alpha; older 0.5.0/0.5.2 staging also remains, so `dist` is not a release authority;
- a clean build and exact artifact audit must be the only release input; `-SkipBuild` must validate source/bin/staged identity rather than trusting old outputs.

The player-package invariant during this long update is still five production DLLs and no QA/test assembly. A future deliberate production-assembly redesign may change that only through a dedicated architecture/release review, not as a side effect of QA extraction.

## QA And Smoke Boundary

### Proven production intrusion

- The GameBridge constructor calls `LoadSmokeSettings()` (`DolocTownGameBridge.cs:271-277`).
- Every production frame calls `SmokeUpdate()` (`DolocTownGameBridge.Update.cs:5-11`).
- If `DTMAPI/smoke-settings.json` does not exist, `SmokeUpdate` sees a null settings object and calls `LoadSmokeSettings` again; `LoadSmokeSettings` performs `File.Exists` and leaves the field null (`Smoke/SmokeHarness.cs:26-31,892-913`). A normal player without Smoke settings therefore incurs a filesystem existence check every frame.
- Production Hook callbacks call Smoke markers directly. The runner protocol, private `SmokeSettings` DTO, title-button delegate, root-isolation fields, and scenario state are bound into the production partial class.
- Core contains Smoke-specific virtual input, synthetic frame, owner-lifetime, root-isolation, save/load snapshot, and camera helper paths. Bootstrap contains Smoke input diagnostics. Moving only the `Smoke` directory would leave a half-extracted system.
- `RuntimeMemoryTrendProbe`, `RuntimeThreadAllocationProbe`, `UnityRuntimeMemoryMetricsProvider`, and `FishingPerformanceProbe` contribute roughly 1,040 additional QA/performance lines outside the GameBridge `Smoke` directory.
- `tools/scripts/run-game-smoke.ps1` assumes the harness is embedded: it stages only the player runtime, writes `smoke-settings.json`, and controls cleanup. Extraction must migrate the runner and failure protocol at the same time.

This is both a size boundary and a concrete inactive-cost defect. It is not proof of the Fatal GC root cause.

### Required QA architecture

Create an optional internal `netstandard2.0` QA assembly, tentatively `DTMAPI.GameBridge.DolocTown.QA`, with these constraints:

- the QA project may reference production assemblies; production assemblies must not statically reference QA;
- access must use narrow internal/friend seams, not new public Abstractions APIs;
- the player Workshop runtime must not include the QA DLL, QA DTOs, Smoke partials, Smoke settings polling, or QA-only performance probes;
- the smoke runner must explicitly stage the QA assembly and fail fast if it is absent or version-mismatched;
- QA activation must occur once at startup through a reviewed optional-module/fixture boundary, not through a player per-frame file probe;
- QA roots/listeners must be removable at title/shutdown and leave no process after the run;
- existing settings/result/log identifiers must either remain compatible or have an explicit runner migration and rollback record.

Recommended extraction order:

1. add the QA project, explicit dev staging, load/close protocol, missing/mismatch failure, and player-package exclusion before moving scenarios;
2. move settings/result DTOs and the four obvious QA/performance probes;
3. move low-coupling cases such as CustomEntity/diagnostics/FishRoe/Chest/Save;
4. move Bootstrap/UI/content/audio/camera cases through narrow host seams;
5. move world-changing ActionSpeed/ActionCompletion/Crop/Equipment cases;
6. move Fishing, owner-lifetime, save/load, and long-trend cases last;
7. only then delete production `SmokeUpdate`, Smoke Hook markers, root-isolation fields, and Core/Bootstrap `ForSmoke` helpers that have no production owner.

`NativeUiLayoutDiagnostics` is not a QA-only block. Its normalization and repair methods write Unity layout constraints and force layout refresh while also emitting diagnostic stacks. Before any extraction, split a minimal production UI-repair contract from optional diagnostic instrumentation; removing or demand-disabling the whole feature can regress title/config/menu layout.

## Feature, Hook, And Inactive-Cost Boundary

`RegisterExperimentalApis()` calls `EnsureGameBridgeFeatures()`, and `EnsureGameBridgeFeatures()` constructs nearly every feature host (`DolocTownGameBridge.cs:348-380,536-605`). Installation then dispatches every feature's Hook installer (`DolocTownGameBridge.Features.cs:14-29`). Current feature buckets are:

| Bucket | Features | Current demand behavior |
| --- | --- | --- |
| every frame | Camera, FishingAutomation, ActionSpeed, CustomAnimalAnimatorBridge, AudioReplacement | Fishing's service/Hook runtime is consumer-lazy; most others are constructed and dispatched regardless of consumer. |
| every 250ms | AnimalViewer, SaveSlots, NativeUiLayoutDiagnostics | Hosts and Hooks are installed; service methods may fast-return. UI diagnostics also performs real repair. |
| lifecycle-only | FishRoeTooltip, ChestLocatorEnhancer, StrongPlantingGun, CropHarvesting, ActionCompletion, OilCoalDrop | Most Hook routes are still installed at base startup. CropHarvesting has no own Hook. |

The central scheduler loops over all features each frame, dispatches the five every-frame features, updates skip counters for the other buckets, records success/status state, and periodically formats diagnostics. Static source contains more than one hundred direct `TryPatchPrefix/Postfix` expressions plus wrapper-based patch requests. This is a request-site inventory, not a claim that every Hook installs successfully.

The shared Hook retry timer starts at two-second cadence but is stopped and `AssemblyLoad` is detached when core readiness is reached. It must not be described as an unconditional process-long timer.

### Concrete recurring-work findings

1. The absent-Smoke-settings `File.Exists` check occurs every frame.
2. `DtmApiRuntime.LoadedMods` returns `loadedMods.ToArray()` on every access. CustomAnimals and Audio access it each frame.
3. CustomAnimals `RefreshDefinitions` builds a signature before applying any throttle/unchanged decision. `BuildLoadedContentSignature` uses filtering, ordering, projection, `ToArray`, path construction, `File.Exists`, `GetLastWriteTimeUtc`, string concatenation, and `string.Join` each frame (`CustomAnimalAnimatorBridgeService.cs:351-362,2507-2518`).
4. Audio performs the same signature construction each frame and also snapshots `entries.Values.ToArray()` (`AudioReplacementService.cs:214-231,265-275,1913-1924`). Its five-second interval only labels an unchanged skip after the expensive signature has already been built.
5. Feature dispatch/status/detail formatting and resource-refresh observation remain high-frequency candidates.

These paths are source-proven recurring I/O/allocation pressure and should be removed or event/throttle driven. They are not source proof that any one path caused Unity's native mark-stack failure.

### Demand-activation direction

GameBridge must continue to own Harmony/reflection/native adapters. Ordinary Mods should own product configuration, hotkeys, UX, policy, and enablement. Activation should be driven by one of:

- a live API consumer/owner policy;
- an enabled content definition;
- a mandatory framework repair responsibility;
- an explicitly enabled diagnostic/QA host.

Fishing primitives are the strongest current template: the feature/API shell exists, but the native Hook runtime is created only for an active primitive session or enabled compatibility consumer, and the last consumer deactivates it. CustomAnimals has a useful partial pattern because native hooks are definition-demanded, but its definition discovery is still per-frame. Orphan item recovery in EquipmentSlots and mandatory title layout repair are examples that cannot simply stop when ordinary product consumers reach zero; those safety/repair responsibilities must first be separated from active product UI/stats behavior.

## Compatibility Boundary

The Compatibility directory is almost entirely legacy fishing: `LegacyFishingAutomationService.cs` is about 2,989 of 3,077 lines. Runtime activation is already lazy and should be preserved.

Repository product/source scanning found no ordinary Mod consumer of `IFishingAutomationApi`. That does not prove no external Workshop binary consumes it. The interface is Deprecated/Frozen with an obsolete warning, and current policy requires a warning-bearing preview/release window before removal.

Physical optionalization remains blocked by direct type coupling:

- `FishingAutomationFeature` owns the legacy service type and uses its normalization helper;
- the legacy service implements internal fishing runtime/coordinator contracts and shares animation/input/native state;
- Smoke overrides remain coupled to the service.

Therefore Compatibility extraction follows QA extraction and internal activation-seam cleanup. It does not lead them. The same rule applies to the failed/obsolete `ICameraZoomApi` compatibility route: zero repository consumers is a research signal, not deletion permission.

## First-Party Product Boundary Matrix

The release catalog publishes eight products, but only Zoom is physically under `first-party-mods`; the other seven published products are sourced from `testmods`. The developer catalog mixes actual products and QA. AutoHarvest has publish metadata but no published/developer definition and is never packaged by the current lane.

| Product | Current physical/release identity | Correct responsibility split | Current residual boundary |
| --- | --- | --- | --- |
| Zoom | `first-party-mods`; Published | Mod owns config/keybind/lease policy; GameBridge owns native camera application. | `ICameraViewApi` has one consumer; Camera host/Hook/every-frame route and zero-consumer CameraZoom compatibility still load. Background/fog/panorama are not implemented. |
| AutoFishing | `first-party-mods`; DeveloperOnly | Mod owns product state machine, movement cancel and recast policy; GameBridge owns internal fishing primitives. | `InternalsVisibleTo("AutoFishingMod")` couples product assembly identity; old shadow/legacy product residue remains; frozen public compatibility still ships. Fishing activation itself is a good model. |
| ActionSpeed | `testmods`; Published | Mod should own policy/config; GameBridge should own reviewed native action adapters/arbitration. | Single Experimental consumer; broad feature, Hooks, and every-frame dispatch are unconditional. Manifest omits required GameBridge dependency. |
| OneActionComplete | `testmods`; Published | Mod should own enable/config; GameBridge should own reviewed resource/fuel/feeder native actions. | Single Experimental consumer; shared ToolCollider Hooks always install; OneAction directly invokes Oil product behavior. |
| ChestLocatorEnhancer | `testmods`; Published | Mod policy over a native inventory adapter. | Single Experimental consumer; base Hook has no demand gate. |
| DebugConsole/YConsole | `testmods`; Published | Mod should own hotkeys/config/product lifecycle; optional diagnostic host owns UI; GameBridge owns diagnostic native actions. | Mod is a thin shell while 2,432 UI lines live in Bootstrap; host/API/update and debug Hooks are base-runtime responsibilities today. |
| FishBreedingAssistant | `testmods`; Published | Mod owns tooltip policy; GameBridge owns native tooltip adapter. | Single Experimental consumer; Hook is unconditional and Mod update/log code contains QA residue. |
| MoreSaves | `testmods`; Published | Mod owns desired UX/config; GameBridge owns official save UI adapter. | Both sides hard-code 12; Show Hook and 250ms route are unconditional. First preserve fixed-12 behavior; naming/scrolling/paging is a later UI batch. |
| AnimalHusbandryProgress | `testmods`; Published | Mod owns display policy; GameBridge owns native animal viewer adapter. | Single Experimental consumer; Hooks and 250ms route are unconditional. |
| AutoHarvest | `testmods`; release metadata orphan | Mod correctly owns scan schedule/filter/target selection; GameBridge exposes explicit crop action. | Not packaged; missing required GameBridge dependency; disabled state still retains periodic subscription; API remains Experimental. |
| MoreEquipmentSlots | `testmods`; DeveloperOnly | Mod should own slot policy; GameBridge owns native storage/UI/stats/shield and safe recovery. | Largest/highest-risk product host; active behavior and no-owner orphan recovery are mixed. Must be migrated last and never gate recovery without proof. |
| Mine | `testmods`; DeveloperOnly | Mod owns machine definition/output policy; GameBridge owns generic native production primitive. | Single Experimental consumer; machine polling lives in the monolithic experimental updater. |
| Oil | `testmods`; DeveloperOnly | Mod/content owns oil identity, economy, enablement and drop policy; GameBridge may own a generic reviewed drop/action adapter. | P0: the Mod never registers gameplay demand, but base GameBridge hard-codes and executes the coal-drop rule. |
| StrongPlantingGun | `testmods`; DeveloperOnly | Mod owns product policy; GameBridge owns reviewed native planting adapter. | Single Experimental consumer; Hook unconditional. |
| ManboCardboardAudio | `testmods`; DeveloperOnly | Mod owns WAV/event policy; GameBridge owns reviewed short-SFX bridge. | Single Experimental consumer; Wwise Hooks/every-frame route unconditional. This does not prove BGM support. |
| CropHarvestingQA | `testmods`; DeveloperOnly + `QaFixture` | Real-field QA only. | Must stay QA and does not count as a second independent product adopter of `ICropHarvestingApi`; manifest omits required GameBridge dependency. |

`testmods/README.md` is already stale because it still presents moved AutoFishing/Zoom as testmods and describes published products, prototypes, QA, examples, and negative fixtures under one heading. A single machine-readable release/product catalog must become the classification authority; folder README text cannot own release scope.

### P0 Oil behavior leak

This is the most serious runtime correctness finding:

- `OilCoalDropService.TryRollOilDropFromCoal` hard-codes coal recognition, an `0.08` probability, and `crude_oil` backpack placement (`Features/OilCoalDrop/OilCoalDropService.cs:109-131`).
- `OilCoalDropFeature` registers no owner API. `OilMod/ModEntry.cs` registers only menu text/logs and makes no gameplay enablement handshake.
- `EnsureGameBridgeFeatures` always creates Oil and ActionCompletion and injects `oilCoalDropFeature.Service.TryRollOilDropFromCoal` directly into ActionCompletion (`DolocTownGameBridge.cs:594-605`).
- the shared ToolCollider prefix/postfix always routes coal capture/drop, and a successful OneAction hit explicitly invokes the Oil callback (`Hooking/ToolColliderHitHookBridge.cs`, `Hooking/DolocTownHookCallbacks.cs:408-427`, `ActionCompletionService.cs:129`).

Therefore disabling or omitting the Oil code Mod does not establish that the product rule is inactive. If `crude_oil` is resolvable, base GameBridge can grant it. The first implementation batch must introduce explicit owner/content demand, disconnect OneAction from Oil, and preserve only a generic native adapter in GameBridge. Merely moving these files into another directory would conceal, not fix, the boundary.

## Protected Custom-Animal Capability

The JSON + PNG + WAV animal route is the strongest user-confirmed content capability and is a protected regression baseline. Its correct boundary is not the public C# `ICustomAnimalApi.RequestSpawn` surface.

The protected ownership split is:

- the content pack owns identity, official animal/item/shop/produce/document JSON, `Content/DTMAPI/custom-animals.json`, `audio-replacements.json`, PNG/WAV assets, and economic balance;
- Core owns enabled content discovery, indexing, manifest/dependency diagnostics, and lifecycle notification;
- GameBridge owns template animator/AI/sprite mapping, AnimalVoice/native event safety, and fragile native hooks;
- Doloc Town remains owner of animal lifecycle, growth, feeding, reproduction, production, room placement, and save data.

Protected regression cases include current Hatch/Mole/Drecko/Oilfloater PNG routes, the ShellCrab advanced bundle, and Hatch child/adult WAV behavior. Required checks include identity alignment, template/frame/PNG failure handling, sound event + stage + path matching, no pollution of original animals, official enable/disable/remove refresh, save/reload, and native lifecycle behavior.

Current support is template AI and reviewed short SFX. It does not establish BGM/loop/STOP/callback support. Content-pack success does not promote C# CustomAnimal/Monster/Attack/Drone runtime verbs; those remain StableCandidate definitions with blocked native runtime creation according to the API matrix.

## Public API Consumer And Stability Boundary

The public matrix, not local adoption count, remains authoritative.

Consumer findings:

- widely used framework foundations include `DtmMod`, `IManifest`, helper/monitor/config/event/input/registry surfaces, but each child surface keeps its current Stable/StableCandidate/Experimental status;
- one first-party/product consumer currently uses each of `IActionCompletionApi`, `IActionSpeedApi`, `IItemTooltipApi`, `IAnimalViewerApi`, `IMachineProductionApi`, `IEquipmentSlotsApi`, `ISaveSlotsApi`, `ICameraViewApi`, `IChestLocatorEnhancerApi`, `IStrongPlantingGunApi`, and `IAudioReplacementApi`;
- `ICropHarvestingApi` has AutoHarvest plus a QA fixture, not two independent products;
- `IFirstPartyFishingPrimitivesApi` is internal and used only by AutoFishing;
- DebugConsole, inventory/weather/teleport/instant-save/time/movement/advanced APIs are Diagnostic in practice and used by the console product and Smoke;
- `IFishingAutomationApi` is Deprecated/Frozen with no current product consumer;
- `ICameraZoomApi` is failed/obsolete compatibility with no current product consumer;
- `IMailDeliveryApi`, `IPanoramaCameraApi`, and the C# CustomEntity runtime routes have no current ordinary product adopter.

`DtmApiStatus` can express Proposed, Experimental, Verified, Stable, Disabled, and StableCandidate, but cannot encode the matrix's Diagnostic, Internal, Deprecated, Retired, or Failed distinctions. Several debug APIs are therefore annotated merely Experimental even though the matrix is stricter. A dedicated API metadata task must reconcile IDE/reflection metadata with the canonical matrix; mass promotion to Stable is not a solution.

No API may be removed solely because repository scanning finds zero consumers. External Workshop consumers, published warnings, adapter ownership, and a compatibility window must be reviewed first.

## Version, Product Catalog, Packaging, And Workshop Boundary

### Current version projections

Runtime version is hard-coded in three places:

- `DtmApiRuntime.ApiVersion/BinaryVersion` (`DtmApiRuntime.cs:21-22`);
- `Directory.Build.props:8-10`;
- `release-common.ps1:4-6`.

Unit tests assert literal equality across those copies. They do not create a single authority.

`Directory.Build.props` applies runtime `AssemblyVersion`, `FileVersion`, and `Version` to ordinary Mod projects. Current AutoFishing, Zoom, and ActionSpeed DLLs therefore report runtime `0.5.3.0` / `0.5.3-alpha` metadata instead of their manifest product versions. Abstractions' assembly identity also advances with each runtime patch without an explicit compatibility policy.

The required version model is:

| Dimension | Required authority/policy |
| --- | --- |
| Runtime product release | one machine-readable source projected into MSBuild, runtime constants, scripts, UI, and release manifests. |
| public API compatibility | independent compatibility epoch/policy plus the public API matrix; not every runtime patch. |
| BepInEx/FileVersion | numeric projection; the retained 0.5.0 evidence proves alpha labels are invalid for `[BepInPlugin]`. |
| AssemblyVersion | deliberate binary compatibility policy, especially for Abstractions; do not inherit blindly into Mod projects. |
| Mod product version | each product's reviewed, monotonic manifest version; generate or validate info/publish/DLL metadata from it. |
| minimum DTMAPI/provider version | lowest actually supported numeric compatibility contract, reviewed in source; packaging validates rather than silently rewrites. |
| config/content/package schema | independent schema versions and migrations; never substitute Runtime or Mod version. |
| game compatibility | machine-readable tested Steam build/Assembly identity catalog, initially diagnostic rather than a premature hard block. |
| Workshop identity | immutable mapping of UniqueID, source project, official folder, WorkshopID, scope, and package name. |

The existing numeric suffix-trim compatibility policy from `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md` remains in force. This audit does not authorize strict SemVer prerelease ordering.

### Mod metadata drift

Outside `bin/obj`, 16 functional/product/QA Mod manifests were identified in addition to examples/negative fixtures. All 16 have `official-info.json`; 15 also appear in publish metadata. AutoFishing is the sole identity whose manifest, official info, and publish version agree. Fourteen of the 15 triple-projected Mods disagree, and Manbo's manifest also disagrees with official info while publish metadata is absent.

Examples:

- Zoom: `0.4.2-dtmapi` manifest versus `1.0.0` info/publish;
- ActionSpeed: `1.3.4-dtmapi` versus `1.0.0`;
- OneActionComplete: `1.1.2-dtmapi` versus `1.0.0`;
- FishBreedingAssistant: `1.1.3-dtmapi` versus `1.0.0`;
- Mine: `0.5.1-alpha-dtmapi` versus `1.0.0`;
- AutoFishing: `1.4.3-dtmapi` in all three projections.

Do not mechanically overwrite `1.0.0` with the lower manifest values or vice versa. Steam/public release history must first determine the monotonic product version for every identity. UniqueID, official folder, WorkshopID, config filenames, and persisted sidecar paths are frozen during that decision.

### Packaging and status gaps

- package/install scripts silently replace every Mod's `MinimumDTMApiVersion` and selected provider minimums with the current runtime. Loose source and staged Workshop packages therefore express different compatibility contracts, and every runtime patch forces a product rebuild regardless of the true minimum;
- `info.version` is only filled when absent/blank, so existing `1.0.0` drift survives packaging;
- current product/release definitions are hard-coded lists without one catalog that also owns physical source, scope, official folder, Workshop identity, and metadata;
- AutoHarvest has publish metadata but no package/install definition; Manbo is developer-defined but has no publish metadata; AutoFishing is a stable first-party product yet marked DeveloperOnly and requires a deliberate release-scope decision;
- ActionSpeed, AutoHarvest, and CropHarvestingQA consume GameBridge APIs but omit a required `DTMAPI.GameBridge.DolocTown` manifest dependency;
- `check-dtmapi-status.ps1` reports install-state and installed release-manifest versions, then reports `[OK]` from file presence. It does not compare current Workshop package version, installed state, installed release manifest, or actual Bootstrap DLL FileVersion. A Steam package update without rerunning the installer can therefore look healthy;
- `release-manifest` SchemaVersion 1 receives string `IncludedAssemblies` from the builder and object rows from the installer. Package/installer state schemas lack a centralized shape/evolution contract;
- `MinimumGameVersion` always records a warning and continues because runtime game-version detection is unavailable (`DtmApiRuntime.cs:3488-3501`). The release manifest contains a generic support sentence and an empty minimum.

Required release checks must compare source manifest, built manifest/DLL, staged package, release catalog, installed state, actual DLL version/hash, and Workshop/upload package identity. Status should report a distinct `[UPDATE]` condition instead of treating old-but-present files as ready.

## Package Ownership Marker P0

`author-docs/content-packs/custom-animal-json-png-wav.md:313-324` tells third-party authors to create `Content/DTMAPI/dtmapi-package.json` with their own owner and `packageKind=content-pack`.

`Get-DtmApiOwnedOfficialLocalPackages` treats any directory containing that filename as DTMAPI-owned without validating owner, package kind, generator, or schema (`release-common.ps1:504-559`). With `-RemoveOfficialLocalPackages`, the uninstaller moves those directories to backup because the marker exists (`uninstall-dtmapi.ps1:132-169`). The operation is backed up, but it is still an ownership violation.

Builder and installer markers also have different shapes: the builder includes `packageKind`, while the installer does not. Neither has a schema.

The contract must split:

- optional ecosystem/source metadata that any author may supply; and
- an installer receipt that only the DTMAPI installer writes, with SchemaVersion, `managedBy/installerOwner=DTMAPI`, package identity, transaction/build identity, and preferably installed-file hashes.

Uninstall must require a valid installer receipt and cross-check it against install state. During compatibility transition, a marker whose owner is not exactly DTMAPI or whose kind is outside the official allowlist must never be moved. Tests must cover third-party, malformed, forged, legacy, and current receipts in dry-run and real temporary-root modes. Unknown content must never be automatically deleted or moved.

## Author And Misinstallation Boundary

Current author documentation covers content packs only. `DTMAPI.TemplateMod` contains a one-paragraph README but no project, source, or manifest. There is no reusable third-party CodeMod packager, reference/SDK acquisition path, or CodeMod quickstart. `author-docs` is not included in player or author packages.

Required author classification:

1. **DTMAPI Runtime**: one BepInEx plugin entry plus four co-located runtime dependencies in `BepInEx/plugins/DTMAPI`.
2. **DTMAPI CodeMod**: manifest `Type=CodeMod`, relative `EntryDll`, entry derives from `DtmMod`; installed under game `Mods` or official/Workshop content layout and managed by DTMAPI owner/dependency/config rules.
3. **DTMAPI ContentPack**: `Type=ContentPack`, empty `EntryDll`, enabled through official local/Workshop content paths.
4. **External BepInEx-only plugin**: `BaseUnityPlugin`/`BepInPlugin` under `BepInEx/plugins`; explicitly unmanaged by DTMAPI and outside owner cleanup/hot-disable guarantees.

DTMAPI currently does not scan `BepInEx/plugins` for misplaced CodeMods, and status checks only recognize a fixed set of legacy paths. A CodeMod DLL placed there is commonly invisible to DTMAPI; a BepInEx plugin placed there bypasses DTMAPI management by design.

The doctor must:

- exact-allowlist the DTMAPI runtime directory/files;
- hard-error when a manifest-driven CodeMod or its `EntryDll` is targeted to `BepInEx/plugins`;
- identify valid BepInEx plugin assemblies as external/unmanaged, not automatically wrong;
- report unknown DLLs as warnings only;
- never move/delete external or unknown files automatically;
- give the exact supported package layout and restart implications.

The author deliverable must include a real `netstandard2.0` CodeMod scaffold, Abstractions/SDK reference method, manifest/dependency/version guidance, typed input/config examples, local/official/Workshop layouts, pack/validate/doctor commands, owner lifecycle rules, public API status guidance, and BepInEx migration notes. First-party scripts that overwrite Author with `Yuuka` are not a reusable third-party publishing tool.

## Config, Content Format, And Game Compatibility Boundaries

- Config migration currently stores only an `Action<T>` and can run/rewrite on every read; it has no from/to/current schema or executed-migration ledger. Treat it as idempotent normalization until a per-Mod schema/ordered migration design exists.
- DTMAPI manifests have no package/content format field. `custom-animals.json` and `audio-replacements.json` deserialize top-level arrays without a format version. Freeze the current shape as v1 through validators/fixtures before introducing a wrapper or negotiated content-format field.
- Official Doloc Town JSON schemas belong to the tested game build; a DTMAPI content format version must not pretend to version official game tables.
- `MinimumGameVersion` is diagnostic-only today. Establish a tested build/hash catalog from the reviewed reverse-build identity before enforcing hard blocks.
- Workshop publish metadata does not provide a complete UniqueID-to-WorkshopID mapping. Local untracked state cannot be the sole product identity authority.

## Lifecycle And GC Evidence Boundary

ISSUE-010 remains open. Current durable evidence says:

- the main-menu long-idle input-pressure amplifier was mitigated;
- a FullKnown one-hour-title plus ten save/load cycles passed after that mitigation;
- inactive AutoFishing sustained retention is no longer an active suspect;
- active fishing-loop/long gameplay, representative ActionSpeed loops and the broader Unity/Mono Fatal GC class remain open;
- short owner cleanup and bounded root counts are not proof that the native mark-stack failure is solved.

This audit adds source-proven recurring work: absent-Smoke-settings file checks, `LoadedMods.ToArray`, per-frame content signatures/file timestamps, entry snapshots, and feature/resource diagnostic work. It does not establish causation. QA extraction can reduce player roots and noise, but source/package reduction must not be labeled a GC fix.

Unity Mono's allocation counter path is known to be unavailable/nonfunctional in this environment; a zero value from `GC.GetAllocatedBytesForCurrentThread` must not be used as zero-allocation evidence. Do not add forced `GC.Collect` as a product workaround.

After QA extraction and hot-path correction, the required GC campaign uses controlled third-save profiles. The detailed AutoFishing/ActionSpeed parallel speed ladders and source-topology correction are owned by `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`:

1. player production package, no QA assembly/settings;
2. title idle baseline;
3. normal active gameplay;
4. repeated title/save reload;
5. AutoFishing Fishing Ready/Cast/Pull at native 1x, enabled without acceleration, common acceleration, high multiplier, disable recovery and title-cycle levels, including the defined 10-20-fish product gate;
6. ActionSpeed Tool/Interact/Eat/Continuous-use through the same independent ladder;
7. per-real-minute plus per-fish/per-action counts, Mono/Unity profiler evidence where available, process/fatal capture, lifecycle recovery and clean exit;
8. only after both ladders are classified, an installed-coexistence lifecycle smoke which is not treated as evidence of a shared Animator conflict.

Every performance claim must state the exact profile and distinguish allocation rate, retained managed roots, Unity native objects, package size, Hook count, and player behavior.

## Findings And Risk Ranking

### P0 - fix before any broad movement

1. Oil product behavior executes without OilMod owner demand and is directly coupled to OneAction.
2. Third-party author metadata can be interpreted as a DTMAPI installer ownership receipt by uninstall.

### P1 - first architecture/release waves

1. QA/Smoke is embedded in production and performs a player per-frame filesystem probe when settings are absent.
2. Runtime, binary, Mod, info, publish, package minimum, and installed versions lack a single governed contract; status can report old installs as OK.
3. Product/release scope is split across folders and hard-coded lists; published products live under `testmods`, and AutoHarvest is a packaging orphan.
4. Most feature hosts/Hooks/updaters activate by construction rather than real consumer/content demand.
5. CustomAnimals and Audio rebuild content signatures and read file timestamps before throttling on every frame.
6. The CodeMod author/template/package/doctor path is missing; misplaced CodeMods under BepInEx are not diagnosed.
7. ActionSpeed, AutoHarvest, and CropHarvestingQA omit their required GameBridge provider dependency.
8. API runtime metadata cannot express several canonical matrix statuses and may mislead developers.

### P2 - complete before or during focused product migration

1. global assembly/product metadata makes Mod DLLs look like the Runtime and lacks an Abstractions compatibility-epoch policy;
2. config/content/package schemas are not independently versioned;
3. game compatibility and Workshop identity are not machine-readable end-to-end;
4. DebugConsole product UI remains in Bootstrap, and title/config UI plus repair/instrumentation responsibilities are physically mixed;
5. Equipment orphan recovery, UI/stats, and product policy are not separable yet;
6. legacy compatibility remains physically coupled even though runtime activation is lazy.

## Ordered Implementation Program

Every numbered batch below requires its own Update. Repeated lifecycle/UI/Hook/save issues require a focused Review before implementation, and API/native work requires the native-owner workflow.

### Batch 0 - freeze and contract baseline

- freeze every public UniqueID, known WorkshopID, official folder, package name, config filename, content/save sidecar path, and current public API status;
- record the eleven locally evidenced Published Workshop products, DeveloperOnly products, prototypes, retired research, QA fixtures, examples, negative fixtures, and compatibility components in one machine-readable catalog; live Workshop verification remains a release gate rather than a reason to invent missing identity facts;
- codify the player five-DLL/no-QA invariant and the protected custom-animal/Zoom/AutoFishing behavior matrices;
- stop new Workshop uploads, mass version edits and identity moves until the full 0.5.5 release baseline passes; Batch 0/P0/authority work continues through focused Updates, while unrelated feature work stays paused.

### Batch 1A - runtime ownership safety

- add explicit Oil product/content demand and disable semantics;
- remove the OneAction-to-Oil callback dependency;
- retain GameBridge only as a generic reviewed native adapter;
- add missing required GameBridge dependencies to ActionSpeed, AutoHarvest, and CropHarvestingQA;
- run focused source/unit and third-save Oil-off/Oil-on/OneAction coexistence evidence.

### Batch 1B - installer ownership safety

- separate author metadata from installer receipts;
- strictly validate receipts and cross-check install state;
- add temporary-root uninstall matrices for third-party/malformed/forged/legacy/current packages;
- correct the public animal guide immediately after the contract is implemented.

### Batch 2 - version and release authority

- introduce one machine-readable Runtime version source and generate/project runtime, binary, MSBuild, script, and release fields;
- introduce the product/release catalog and a `check-release-contract` gate;
- decide every Mod's monotonic version from actual published history before reconciling manifest/info/publish/DLL metadata;
- stop silent minimum-version rewriting; validate declared compatibility or use an explicit audited pin policy;
- separate Runtime and Mod MSBuild metadata and define Abstractions AssemblyVersion compatibility;
- compare source/bin/staged/package/install-state/release-manifest/actual DLL version and hash;
- make `3_check` report `[UPDATE]` for an old installed runtime;
- clean-build and audit Runtime plus all Catalog-classified Published packages and local upload parity.

### Batch 3 - author/SDK and installation doctor

- ship a real CodeMod scaffold and Abstractions reference/SDK path;
- add content-pack and CodeMod validators/packagers driven by the same schemas/catalog;
- implement the four-way Runtime/CodeMod/ContentPack/external-BepInEx classification and non-destructive misplaced-DLL diagnosis;
- verify a scaffold from build through local install and Workshop-shaped package without placing an ordinary Mod in BepInEx.

### Batch 4 - optional QA host and staged extraction

- implement the optional QA assembly/load/close/runner skeleton and player-package exclusion;
- migrate DTO/probe/case groups in the order defined above;
- split production UI repair from diagnostic instrumentation;
- remove production Smoke settings polling and all proven orphan `ForSmoke` seams only after their QA replacement passes;
- retain exact rollback to the prior embedded-harness batch after each migration group.

### Batch 5 - recurring-work and demand activation

- replace per-frame content filesystem signatures with lifecycle/change-driven invalidation or a true pre-work throttle;
- remove unnecessary `LoadedMods.ToArray` and entry snapshots from hot paths;
- measure feature-dispatch/diagnostic costs and keep only bounded, cadence-controlled publication;
- introduce a GameBridge demand catalog/coordinator for API consumer, content definition, safety repair, and QA demand;
- apply it first to low-risk Hook-only products, then Camera/AnimalViewer, then Audio/CustomAnimals;
- never demand-disable title layout repair or Equipment orphan recovery until those mandatory responsibilities are separated and proven.

### DTMAPI 0.5.5 public release gate

The batches above describe implementation dependencies, not permission to publish after Batch 2. The single public 0.5.5 release also requires the player uninstaller and Oil/OneAction P0s, unified version/status semantics, the publicly obtainable external-Mod compatibility scan, zero old-ABI removal, a QA-free/no-polling player package, demand activation, the parallel AutoFishing/ActionSpeed active-gameplay gates, and old-Runtime block/update/recovery messages.

After 0.5.5, the public product waves are separate from the internal batch order: AutoFishing 1.0.0 alone; ActionSpeed after its focused gate; low-user products in one publication window but with independent WorkshopID/UniqueID/rollback; and MoreEquipment 1.0.0 alone. OneAction remains the structural-split template and Manbo remains the JSON/WAV ContentPack Canary even when their public timing differs.

### Batch 6 - first-party productization and functional split

Use behavior-equivalent batches and preserve identity/config/content paths:

1. OneActionComplete as the first small product/adapter/demand template after Oil is disconnected;
2. retire AutoHarvest as an API-demand sample rather than a published first-party product; move any still-useful crop query/revalidate/single-target evidence into QA while keeping gameplay policy out of Runtime;
3. ActionSpeed after native action categories/arbitration and product policy are separated;
4. MoreSaves with fixed-12 behavior first; naming/scrolling/pagination later;
5. DebugConsole as an optional Diagnostic product host outside Bootstrap; UI rewrite later;
6. ChestLocator, FishBreeding, AnimalProgress, StrongPlanting, Mine, and data-only Manbo one at a time;
7. Oil as official JSON content with no GameBridge crude-oil or OneAction product coupling;
8. MoreEquipmentSlots last, after orphan recovery is isolated from active slots/UI/stats/shield behavior.

Zoom and AutoFishing are cleanup/reference candidates, not reasons to destabilize their currently verified player behavior. AutoFishing's unused shadow/legacy product residue may be removed separately without widening its primitives contract.

### Batch 7 - compatibility and API surface governance

- reconcile API status attributes/tooling with the canonical matrix;
- collect external-consumer evidence and ship warning diagnostics for frozen compatibility;
- establish internal interfaces that permit optional Fishing/Camera compatibility implementation assemblies;
- remove a public compatibility contract only after its published warning window, migration notes, and package/version gate;
- continue native-owner reviews for any API promotion or new domain.

### Batch 8 - player UX and future platform work

Only after the structural/release gates are stable:

1. GMCM/Manager player-facing information hierarchy, consistent row sizes, paging/scrolling, and ordinary-user wording;
2. MoreSaves naming and scrolling beyond the preserved fixed-12 baseline;
3. full Y-console UI/feature redesign on the optional Diagnostic host;
4. build the selected Hatch/Mole/Drecko/Oilfloater content product with custom primary/hidden outputs processed by official JSON into native resources; keep Shell Crab unpublished and Lightning Chicken retired research;
5. generalize reviewed short-SFX replacement; keep BGM as a separate native-owner research project;
6. pursue BGM, multiplayer, pets, and independent vehicles only through separate large reviews. Pets/vehicles must not borrow animal success as proof, and multiplayer remains its own project.

## Blockers And Stop Conditions

- No gameplay/API batch starts without the native owner method/state-holder review required by `codex-api-rebuild.md`.
- No QA task adds public Abstractions APIs, exposes raw Unity/Harmony/Doloc types, or makes the player package depend on QA.
- No product migration changes UniqueID, Workshop identity, config key/path, content identity, or save sidecar path without explicit migration and rollback.
- No public API is deleted because local source has zero consumers.
- No external/unknown BepInEx plugin or third-party content is auto-moved/deleted.
- No strict SemVer prerelease policy is introduced in these batches without its dedicated compatibility review.
- No Hook/reflection implementation is moved into an ordinary Mod.
- No Equipment demand gate may disable orphan item recovery.
- No UI instrumentation block may be removed until any production repair behavior is isolated and independently verified.
- No source/package-size reduction is called a GC fix; ISSUE-010 requires runtime/profiler evidence and clean restart/exit gates.
- `references/third-party-mods/小神增强包` remains a read-only compatibility/semantic-demand sample. Its binaries, archives, or implementation may not be copied, merged, repackaged, or used as stability proof.

## Validation Performed

- current source/project references, manifests, release scripts, author docs, public API matrix, current reviews/Updates, and relevant issue facts were cross-checked;
- physical line/public declaration and current Release artifact inventories were generated from the workspace;
- `tools/scripts/test.ps1 -Configuration Release`: passed after this review was recorded, with zero build warnings/errors and `DTMAPI.UnitTests: OK`;
- `tools/scripts/check-doc-governance.ps1`: passed;
- `git diff --check`: passed;
- no game launch, runtime lock, install/uninstall, Workshop mutation, or runtime smoke was performed.

The audit does not claim Steam's live item versions/WorkshopIDs were verified. Those external identities are a Batch 0-2 release-catalog prerequisite.

## Resolution Boundary

This Review is complete as the pre-implementation boundary record. The identified runtime/release/API/author/GC issues remain open until their own Updates meet the stated gates. Completion narratives, changed files, runtime evidence, Hook changes, API status changes, and issue-state changes belong to those task-specific owners, not appended here.
