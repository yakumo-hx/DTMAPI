# ISSUE-010 Boundary Correlation Research

Date: 2026-07-05 +08:00
Status: recorded / docs-only
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: Explore whether the current Unity/Mono `Fatal error in GC / Unexpected mark stack overflow` investigation is related to unclear DTMAPI/mod/GameBridge/content boundaries.

## Executive Judgment

Current evidence does not prove that unclear boundaries are the direct root cause of ISSUE-010. It does strongly support a boundary-correlation hypothesis:

- The crash is no longer best explained by duplicate `LoadGame`, smoke pending-status pressure, or SaveLoad cycle count alone.
- The minimal reproduced profile requires multiple stable process/title feature families together: CustomAnimals/AnimalVoice, action/utility mods, Manbo audio, and the UI group.
- Latest ledgers show per-save/title DTMAPI transients returning to zero, but the failing profile still has a larger stable root graph: mod-owner records, event/input/config roots, GameBridge feature states, audio replacement entries/platform players, and custom animal registrations.
- Current source already added owner ledgers, lifecycle contracts, and object-delta snapshots. That means the active investigation has implicitly become an ownership-boundary investigation.

The likely relationship is not "one feature leaked one object." It is more likely that product behavior, native bridge state, content-pack state, diagnostics, and UI roots are co-located under long-lived DTMAPI process roots, so Unity/Mono sees a larger reachable graph than a cleaner architecture would create or expose.

## Current Evidence Chain

Relevant retained facts from ISSUE-010 and the latest 2026-07-04/05 records:

- No-idle current/default 20 SaveLoad cycles passed with 20 native enters, 20 returns, 20 `SaveLoaded`, and no duplicate requests.
- Core runtime plus one-hour continuous title idle passed in `CoreOnly`.
- Individual groups passed under the one-hour continuous title-idle route:
  - `CoreCustomAnimals`.
  - `CoreAutoFishing`.
  - `CoreUi`.
  - Action/utility group.
  - CustomAnimals plus action/utility.
  - CustomAnimals plus action/utility plus Manbo audio.
- The minimal reproduced group in this pass was CustomAnimals/AnimalVoice plus action/utility plus Manbo audio plus UI. It failed on the first post-idle native `LoadGame` before `SaveLoaded`, with duplicate requests still zero.
- The same minimal reproduced group passed 20 no-idle cycles.
- The same minimal reproduced group passed an interrupted-idle cadence run: 10 minutes initial title idle, then one save entry every 5 minutes, 20 cycles, about 109 minutes elapsed.
- Short and bisection ledgers show the named DTMAPI per-save/title transients are bounded or zero at title: SaveSlots pager/binders, EquipmentSlots clones/binders/storage owners, AnimalViewer rows/objects, AudioReplacement pending requests/AudioClips/callback owners/animal contexts, CustomAnimals controller/bundle caches, and AutoFishing native handles.

This points away from a simple active leak and toward a long-lived root-set/object-graph pressure problem that requires a certain mixture of owners and an uninterrupted title state.

## Source Boundary Observations

### 1. Bootstrap Is A Process-Long Root Island

`BootstrapPlugin` owns the runtime, `DolocTownGameBridge`, reflected title settings UI, reflected debug console UI, Unity update/fallback-pump state, and object graph providers. It clears DTMAPI-owned UI and bridge roots on `OnApplicationQuit`, and it resets title/debug UI at save/title boundaries.

That is correct for a BepInEx plugin, but it means platform UI, diagnostic UI, product feature host, and smoke/runtime update loops are all reachable from one process-long plugin root.

### 2. GameBridge Hosts Many Product Features In One Long-Lived Object

`DolocTownGameBridge` owns a single feature list and long-lived services for camera, fishing automation, fish roe, chest locator, save slots, UI diagnostics, strong planting gun, crop harvesting, animal viewer, custom animals, audio replacement, oil drops, action speed, action completion, plus shared hook bridges.

It registers many APIs through the same runtime owner manifest `DTMAPI.GameBridge.DolocTown`, including product-shaped APIs like `IFishingAutomationApi`, `IActionSpeedApi`, `ISaveSlotsApi`, `IEquipmentSlotsApi`, `IChestLocatorEnhancerApi`, `IStrongPlantingGunApi`, `IMachineProductionApi`, and diagnostic APIs.

This is the core boundary smell for ISSUE-010: if one official feature mod is thin and the implementation state lives inside GameBridge, owner attribution splits three ways:

- visible mod owner: config, keybinds, event subscriptions;
- GameBridge owner: product implementation state, hooks, runtime service caches;
- native owner: Unity/Doloc Town objects reached through reflection/Harmony.

The current owner ledger can observe this split, but it cannot make the root graph smaller.

### 3. Feature Contracts Exist, But They Are Diagnostic Contracts

`GameBridgeFeatureContract` already tracks good lifecycle vocabulary: `requiresSave`, `allowsTitleScreen`, `requiresUi`, update bucket, `hasSaveLifetimeState`, and `hasTitleLifetimeState`.

That is a positive sign. The remaining problem is that these contracts do not yet split features into separate lifetime containers or enforce a public boundary between framework, product, content, and diagnostics. Every feature still implements `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` by convention inside the same GameBridge host.

### 4. Owner-Bound Framework Cleanup Is A Boundary Repair

The current mainline added owner-bound cleanup for events, input registrations, config pages, registry/API entries, and custom entity registry definitions. Failed code-mod load rollback explicitly removes DTMAPI-owned side effects and marks raw Harmony/static/native/Unity side effects as `NeedsRestart`.

That is exactly the right direction for lifecycle safety. It also proves the architectural issue: DTMAPI can reliably clean only what has a crisp DTMAPI owner. Everything else must be counted, isolated, or left to restart.

### 5. Content Pack Ownership Is Still Implicit

The content/manifest registry can identify content packs, official JSON, CustomAnimals, AudioReplacement, and AnimalVoice. But the runtime still relies on broad scanner/feature ownership rather than a SMAPI-like `ContentPackFor` owner. Custom animal and AnimalVoice definitions therefore land in GameBridge feature state instead of an explicit content-owner island such as `DTMAPI.CustomAnimals`.

This does not make the custom animal route bad. It remains the healthiest product/content boundary. But for GC triage, implicit ownership makes the stable content graph harder to separate from GameBridge product state.

## Correlation Map

| Signal | Boundary interpretation | GC investigation implication |
| --- | --- | --- |
| `CoreOnly` one-hour idle passes. | Core platform roots alone are not enough in current evidence. | The crash likely needs additional product/content/UI roots. |
| `CoreUi` alone passes. | UI feature family by itself is not sufficient. | The UI group is a multiplier, not a standalone proof. |
| CustomAnimals plus action/utility plus Manbo passes. | Content/audio/action roots without UI are not enough. | Stable content/audio roots are suspect only in combination. |
| Adding UI group to that passing profile reproduces. | UI roots/event/input/config plus content/audio/action roots cross a threshold or trigger a native title-state path. | Split Zoom, MoreSaves, MoreEquipmentSlots, and YConsole under continuous title idle. |
| 20 no-idle cycles pass. | Repeated save entry is not sufficient when title state is not long-lived. | Focus on continuous title scene/root state, not raw cycle count. |
| 10-minute plus 5-minute cadence passes for about 109 minutes. | Periodic save entry appears to reset or avoid the fatal condition. | Next repro must use continuous one-hour title idle. |
| Per-save/title transients return to zero. | The latest obvious DTMAPI-owned leaks are not the current evidence leader. | Watch stable root counts and native root-set, not only cleanup counters. |
| Product APIs are implemented in GameBridge while mods own only policy/config. | Visible owner and implementation owner diverge. | Owner deltas must include both mod roots and GameBridge feature roots. |

## Why Boundary Clarity Matters For This GC Class

Unity/Mono mark-stack overflow evidence is about reachable object graphs, not ordinary managed exception stacks. Boundary clarity affects that in five ways:

1. Root co-location: many product features share one process-long GameBridge object, so independent feature roots are not physically separated.
2. Owner split: official mods own config and input, while GameBridge owns implementation state. A per-owner ledger can observe both, but cleanup authority remains split.
3. Lifecycle-by-convention: `SaveLoaded` and `ReturnedToTitle` cleanup depends on each feature implementing its own reset correctly.
4. Content ambiguity: content definitions and bridge schemas are discovered globally, not through an explicit owned content-pack helper.
5. Diagnostic/product overlap: smoke, debug console, manager UI, product features, and bridge diagnostics all contribute roots to the same runtime graph during investigation.

None of these proves the crash. Together, they explain why the crash profile appears only in combinations and why recent work had to add per-owner object deltas after the fact.

## Boundary-Split Implications For Fix Strategy

The current evidence argues for tightening boundaries before speculative native destruction:

- Keep DTMAPI Core small and boring: loader, event/input/config ownership, registry, diagnostics, content-pack ownership, and report export.
- Keep GameBridge as primitive/native adapter host, but avoid product-shaped state inside it where a first-party mod can own the policy.
- Move official feature product state toward first-party mods or explicit product owners, with GameBridge providing leases/transactions.
- Give content packs explicit owners through a future `ContentPackFor` model.
- Promote `GameBridgeFeatureContract` from diagnostic metadata toward actual lifetime policy checks.
- Continue count-first diagnostics for native/unknown Unity objects. Do not unload/destroy `GameObject`, `AudioClip`, `AssetBundle`, controller, or native UI objects without ownership proof.

## Next Experiment Plan

Use the continuous one-hour title-idle route, not the 10-minute/five-minute cadence route.

Start from the known passing base:

- `CoreCustomAnimals`.
- Action/utility extra ids from the passing profile:
  - `Workshop.3742763309` / DTMAPI ActionSpeed.
  - `Workshop.3742763540` / DTMAPI OneActionComplete.
  - `Workshop.3742763843` / DTMAPI AnimalHusbandryProgress.
  - `Workshop.3742763706` / DTMAPI FishBreedingAssistant.
  - `Local.Yuuka_DTMAPI_ManboCardboardAudio`.

Then add exactly one UI owner per run:

- `Workshop.3742717440` or `Local.DTMAPI_Zoom`.
- `Workshop.3742763050` or `Local.DTMAPI_MoreSaves`.
- `Workshop.3744059735` or `Local.DTMAPI_MoreEquipmentSlots`.
- `Workshop.3742714442` or `Local.DTMAPI_YKeyConsole`.

For each run, inspect:

- `SaveLoadRequestSummary`: duplicate requests must remain zero.
- `TitleReturnBoundarySummary`: final boundary and fatal location.
- `SaveLoadCycleObjectDeltaSummary`: `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero`.
- `OwnerRoots.input.byOwner`, `OwnerRoots.events.byOwner`, `OwnerRoots.configPages.byOwner`.
- `UI.byOwner` and `BootstrapUi.uiByOwner`.
- `GameBridge.featureById`.

If no single UI owner reproduces, run UI pairs. If no pair isolates a DTMAPI-owned growing family, escalate to Unity crash dump/root-set analysis while keeping the DTMAPI ledger as the boundary map.

## Bottom Line

The current mainline has already turned ISSUE-010 into an ownership-boundary problem. The new diagnostics are good, but they are compensating for architecture that still lets framework, GameBridge, product mods, content packs, debug UI, and smoke diagnostics share long-lived roots.

The safest interpretation is:

- Boundary ambiguity is probably not the sole cause.
- Boundary ambiguity is very likely amplifying the reachable graph and slowing isolation.
- A SMAPI-style split would not magically fix the GC crash, but it would reduce root co-location, make lifecycle ownership testable, and make the next fatal profile easier to attribute.

## Validation

Documentation-only research. No runtime/source behavior was changed, no build was run, and no game smoke was launched for this record.

Checked:

- Current branch and working tree status.
- ISSUE-010 current record.
- Latest 2026-07-04/05 update records and code reviews for SaveLoad, title-return object graph, lifecycle sweep, and per-owner deltas.
- `BootstrapPlugin`, `DtmApiRuntime`, `ModOwnerLedgerService`, `TitleReturnBoundaryLedgerService`, `EventManager`, owner-bound input/config/registry services, `DolocTownGameBridge`, `IGameBridgeFeature`, `GameBridgeFeatureContract`, representative GameBridge features, and `run-game-smoke.ps1` official mod profiles.
