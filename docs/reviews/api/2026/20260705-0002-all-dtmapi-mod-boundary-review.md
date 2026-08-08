# All Local DTMAPI Mods Boundary Review

Date: 2026-07-05
Status: recorded / docs-only
Scope: Re-scan local DTMAPI-based mods with the same question used for the Stardew Valley SMAPI install: who provides the main feature ability, the framework/API or the mod?

## Method

The SMAPI reference install at `D:\Steam\steamapps\common\Stardew Valley` shows the useful comparison point. For example, `YetAnotherFishingMod` owns its automatic fishing implementation in `YetAnotherFishingMod.dll` (`FishHelper`, `Patches`, `SFishingRod`) while SMAPI provides loader, manifest, events, config, input, reflection, registry, logging, and a Harmony-capable environment.

For DTMAPI, this review scanned active `testmods/*/manifest.json`, `testmods/*/ModEntry.cs`, selected README files, content JSON packages, `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/*`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`, `docs/api/public-api-matrix.md`, and `docs/hook-map/README.md`.

Excluded from product-boundary judgment:

- `BrokenManifestMod`: intentional negative loader sample.
- `archive/second-motor-20260615`: archived historical sample.
- `references/**`: reference material only.

## Executive Judgment

DTMAPI currently carries a large part of the first-party gameplay feature work itself. Most local player-facing DTMAPI mods are thin configuration, hotkey, content, or policy shells over GameBridge services. That is different from the SMAPI pattern observed locally, where third-party gameplay mods usually carry their own gameplay algorithm and SMAPI remains mostly the framework.

This is partly justified by Doloc Town's current technical situation: native ownership is fragile and should live in `DTMAPI.GameBridge.DolocTown`, not in ordinary mods. The boundary problem is not "GameBridge owns reflection/Harmony" by itself. The problem is that several GameBridge services are named, shaped, and hardcoded like product mods rather than neutral reusable APIs.

The practical result:

- DTMAPI is not only a mod manager and API provider today.
- DTMAPI is also a first-party gameplay feature provider and official-local mod suite runtime.
- The project needs an explicit product lane: framework/runtime vs GameBridge adapter vs official DTMAPI mods vs QA/labs.

## Boundary Matrix

| Mod | Main user-visible feature | What the mod owns | What DTMAPI/GameBridge owns | Boundary diagnosis |
| --- | --- | --- | --- | --- |
| `HelloDtmMod` | Load/log smoke | One `Entry` log and one `GameLaunched` log | Loader, event dispatch, logging | Healthy framework sample. DTMAPI is only framework/API. |
| `ConfigMenuExample` | Config-menu coverage sample | Example config fields and option registration | Config menu UI implementation | Healthy API sample, not a gameplay product. |
| `HookProbeMod` | Smoke/diagnostic probe | Event subscriptions and diagnostics exercise | All framework and GameBridge surfaces being probed | Diagnostic tool. Do not treat as product boundary evidence. |
| `DebugConsoleMod` | Y-key debug console | Y/Escape hotkeys, save gating, language setting, API binding | Reflected console host, debug actions, item/weather/teleport/time/movement/instant-save/native APIs, input isolation, debug content item | DTMAPI is the main feature provider. This should stay Diagnostic/product tooling, not ordinary stable gameplay API. |
| `AutoFishingMod` | Automatic fishing | F6/config/menu, movement cancel, strategy DTOs | Native fishing loop: cast, wait, bite, reel, minigame automation/skip, pull/result, animation speed, hooks | DTMAPI is the main feature provider. This is the clearest "DTMAPI as functional mod" case. |
| `ActionSpeedMod` | Faster actions/animations | Config profile, multipliers, menu hotkeys, policy DTOs | Native tool/interact/eat/drink/bottle/plant/harvest/machine/animal timing hooks and classification | DTMAPI is the main feature provider. API is too broad to promote as one stable surface. |
| `OneActionCompleteMod` | Complete resources/fuel/feed after one action | Config toggles and F11 config-page shortcut | `ToolCollider.HandleTools`, resource completion, fuel/feed native exit routes, vegetation exception handling | DTMAPI is the main feature provider. Keep narrow and native-owner-specific. |
| `MoreEquipmentSlotsMod` | Extra attribute equipment slots | Enable flag and slot request | Sidecar storage, UI strip, native stat reload, equip/recover, shield-hat hit path | DTMAPI is the main feature provider. High lifecycle/recovery risk. |
| `MoreSavesMod` | 12 save slots | Enable flag and fixed slot-count request | `archiveFileCount`, save UI slot layout/paging, runtime refresh | DTMAPI is the main feature provider while official save systems still own files/load/delete. |
| `ZoomMod` | Playable camera view zoom | Hotkeys, max/step config, lease request | Camera lease arbitration and world camera orthographic-size writes | Healthier shape than most: mod owns UX, DTMAPI owns a reusable lease API. Still product-like but closer to framework. |
| `AutoHarvestMod` | Automatic crop harvest | Interval scheduling, manual key, max count, crop-kind toggles | Semantic crop scan, opaque target ids, native `PlantBasin.Harvest` execution | Mixed but DTMAPI-heavy. The mod has real scheduling policy; the hard native transaction belongs in GameBridge. |
| `CropHarvestingQaMod` | Manual crop API QA | QA buttons, summaries, keybinds, target-row logging | Same crop scan/harvest API | QA fixture only. It is evidence tooling, not a user mod boundary. |
| `ChestLocatorEnhancerMod` | Wider shared-container lookup | Config policy for shared cases/shelves | Hook over native inventory list and appending native inventories | DTMAPI is the main feature provider. Could be a reusable inventory-provider API, but current shape is feature-specific. |
| `StrongPlantingGunMod` | Three-slot farming gun | Config and policy switches for seed/film/fertilizer | Native farming-gun constructor/use/UI transfer hooks, fixed three-slot behavior | DTMAPI is the main feature provider. Feature-specific API, not neutral equipment/container API. |
| `FishBreedingAssistantMod` | Fish roe title annotation | Provider callback and small cache; current generated lookup is public placeholder and returns no rows | Tooltip hook, native fish-title fallback, item identity/rendering | Mixed. The ideal split is good: mod supplies data, GameBridge renders safely. Current local package relies on DTMAPI fallback for useful output. |
| `AnimalHusbandryProgressMod` | Animal special-produce progress in viewer | Color/config policy | Animal viewer hook, cloned progress-row UI, native animal data/viewer integration | DTMAPI is the main feature provider. Mod supplies display policy only. |
| `ManboCardboardAudioMod` | Replace paper-box sound | WAV asset, target event id, replacement options | Wwise event hook, allowlist, local playback, native suppression/fail-open behavior | Mixed. Mod supplies asset/content, DTMAPI provides most runtime behavior. The current API is event-specific and experimental. |
| `OilMod` | Crude oil item; oil mining drop | Official JSON item and config blurb | Hardcoded `OilCoalDrop` feature rolls `crude_oil` from coal resources via shared tool-collider route | Content item boundary is healthy; hardcoded oil drop in GameBridge is a product-mod responsibility leak. |
| `MineMod` | Mine machine | Official JSON item/equipment/recipe plus machine definition, outputs, weights, config | Runtime machine production loop, tech/recipe mutation, storage/output, visual scale/lifecycle | Mixed but DTMAPI-heavy. Better than pure shells because mod supplies machine definition, but GameBridge still acts as machine engine. |

## Main Boundary Problems

### 1. Product APIs Instead Of Primitives

Several public APIs are named and shaped around a single migrated mod: `IFishingAutomationApi`, `IActionSpeedApi`, `IActionCompletionApi`, `IChestLocatorEnhancerApi`, `IStrongPlantingGunApi`, `ISaveSlotsApi`, and parts of `IEquipmentSlotsApi`.

This makes it easy for DTMAPI to become "the mod" and for the visible mod to become only a config shell. The SMAPI-style alternative is to expose smaller primitives and let the mod assemble the user feature:

- fishing state/readiness events, native input leases, safe rod actions, minigame input decisions;
- animation/interaction timing leases by native subdomain;
- crop scan and harvest transaction API, not an auto-harvest product;
- camera lease API, which is already the best local example;
- resource drop callback API for OilMod, not a hardcoded `crude_oil` GameBridge feature.

### 2. GameBridge Contains Mod-Specific Product Logic

`OilCoalDrop` is the sharpest example. It has no public API and hardcodes `crude_oil`, coal-resource detection, drop chance, and backpack placement inside GameBridge. That is not a neutral adapter; it is OilMod gameplay.

Other examples are softer but similar: fixed StrongPlantingGun three-slot semantics, MoreSaves fixed 12-slot policy, fishing automation loop policy, and ActionSpeed's broad action categories.

### 3. Thin First-Party Mods Can Hide API Weaknesses

When the only consumer is a first-party shell, API design can drift toward "whatever this mod needs this week". That weakens the signal that an API is usable by ordinary mod authors. It also makes promotion dangerous because the API may not have independent consumers, multi-owner behavior, or failure-mode vocabulary.

### 4. Multi-Owner Semantics Are Inconsistent

Camera has a lease shape. Fishing currently has one effective enabled owner. Chest locator has some merge behavior. Many feature APIs have no explicit owner token, priority, release, disable, or conflict model. Global mutating features need one of:

- exclusive lease;
- priority arbitration;
- documented merge semantics;
- explicit single-owner contract with clear diagnostics.

Without that, DTMAPI official mods work, but external mods cannot safely compose.

### 5. Status And Product Packaging Can Mislead Users

Most gameplay surfaces are Experimental or Diagnostic, but official-local packaging and player-facing config pages make them feel like stable shipped features. That is fine for a first-party mod pack if the UI says so, but it should not imply the public API is stable.

Recommended labels:

- DTMAPI Framework: loader, manifest, config, events, registry, logging, diagnostics, Workshop/install state.
- DTMAPI GameBridge Adapters: native-owner wrappers and fragile transaction primitives.
- DTMAPI Official Mods: first-party player features built on those adapters.
- DTMAPI Labs/QA: HookProbe, CropHarvestingQa, smoke-only probes.

### 6. Content And Runtime Behavior Are Conflated

OilMod and MineMod show two different content patterns:

- Oil item JSON is a clean content-package responsibility.
- Oil mining drop is a GameBridge hardcoded product feature.
- Mine JSON/definition is mod-owned content/policy.
- Mine production runtime is DTMAPI-owned engine behavior.

The clean rule should be: content packages own content definitions; DTMAPI may provide generic runtime hooks, but item-specific gameplay such as "coal can drop crude oil" should be registered by the mod through a generic resource/drop API.

## Per-Area Recommendations

### Fishing

Keep `IFishingAutomationApi` Experimental. Long-term, split it into primitives so AutoFishingMod owns the loop:

- fishing state observer;
- safe selected-rod/cast/reel operations;
- input lease for minigame decisions;
- native result/status DTOs;
- optional animation-speed lease.

### Action Speed

Do not promote a broad `IActionSpeedApi`. Split by native domain: tool animation, continuous-use, bottle fill, plant, crop harvest, animal, machine/fuel/feed, resin/forage. Each needs its own owner proof and QA status.

### One Action Complete

Keep it narrow around reviewed resource/fuel/feed operations. Do not generalize to "complete any action". Shared `ToolCollider` dispatch must remain GameBridge-owned, with callback ordering and isolation documented.

### Equipment And Save Slots

These are DTMAPI product features until hot-disable, unregister, visual cleanup, disabled/unsubscribed recovery, mail overflow, delete/copy/restart, and multi-owner policy are proven. Consider read-only query APIs separately from mutation APIs.

### Camera

Use camera as the preferred pattern: a lease-based neutral capability. Keep ZoomMod as UX/hotkeys and keep native camera writes in GameBridge.

### Crops

`ICropHarvestingApi` is closer to a useful native transaction API than a product mod, but target ids are transient and execution scope is narrow. AutoHarvest should remain the scheduling/product mod; GameBridge should remain the crop transaction adapter.

### Audio

Keep `IAudioReplacementApi` event-level and reviewed-event-only. The mod should own assets and desired event mapping. DTMAPI should own playback/suppression/fail-open mechanics. Multi-mod arbitration and unload cleanup are promotion blockers.

### Oil

Move the hardcoded oil drop idea out of GameBridge product logic when possible. Prefer a generic resource-drop API or a content-runtime registration owned by OilMod.

### Mine

MineMod should remain the owner of the machine definition, output table, recipe policy, and content JSON. DTMAPI may own the generic machine production engine, but the current `IMachineProductionApi` is still too broad and experimental to be treated as stable.

## Bottom Line

By count and by responsibility, the current local DTMAPI mod set shows DTMAPI acting as a first-party feature provider, not merely a mod manager/API layer. That is acceptable as an interim strategy for a fragile Unity/Harmony bridge, but it should be made explicit and separated in architecture, docs, packaging, and status labels.

The safest near-term rule is:

- Ordinary mods own UX, config, data, assets, scheduling, and product policy.
- GameBridge owns native owner wrappers, safe transactions, lifecycle cleanup, and hook isolation.
- DTMAPI Core owns boring framework surfaces only.
- First-party feature mods may ship with DTMAPI, but they should not be mistaken for proof that the public API is stable.

## Validation

Documentation-only review. No solution build or game smoke was run. The review was checked against local manifests, ModEntry sources, README files, content JSON, Abstractions API definitions, GameBridge feature registration, public API matrix, and hook-map records.
