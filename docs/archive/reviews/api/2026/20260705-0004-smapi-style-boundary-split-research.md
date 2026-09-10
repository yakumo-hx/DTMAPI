# SMAPI-Style Boundary Split Research

Date: 2026-07-05
Status: recorded / docs-only
Scope: If DTMAPI follows the SMAPI responsibility model, define how to split framework, native bridge, official feature mods, diagnostics, and content packs.

## Question

The working question is not whether DTMAPI should copy SMAPI source code. It should not. The question is which responsibility boundaries from SMAPI are useful for DTMAPI:

- Where should the mod manager/API platform stop?
- Where should a fragile Doloc Town native adapter live?
- When does an official DTMAPI feature become a bundled first-party mod instead of public API?
- How should a content pack relate to DTMAPI and to the code/content owner that interprets it?

## SMAPI Reference Facts

The relevant SMAPI source shape is:

- `src/SMAPI/Mod.cs` and `src/SMAPI/IMod.cs`: a code mod has an `Entry(IModHelper helper)` entry point. A mod can optionally expose its own integration API through `GetApi()` or `GetApi(IModInfo mod)`.
- `src/SMAPI/IModHelper.cs`: the helper is a container for generic platform services: events, console commands, game content, mod content, content packs, data, input, reflection, mod registry, multiplayer, translation, config read/write, and logging through the mod monitor.
- `src/SMAPI.Toolkit.CoreInterfaces/IManifest.cs`: `EntryDll` and `ContentPackFor` are separate manifest concepts. A manifest can describe either a code mod or a content pack for another mod.
- `src/SMAPI.Toolkit.CoreInterfaces/IManifestContentPackFor.cs`: a content pack declares the unique ID of the mod that can read it, plus an optional minimum version for that owner.
- `src/SMAPI/IContentPack.cs` and `src/SMAPI/IContentPackHelper.cs`: content packs expose generic file, JSON, translation, and mod-content helpers. `ContentPacks.GetOwned()` gives a code mod the content packs that target it. SMAPI does not interpret the pack's domain schema by itself.
- `src/SMAPI/ICommandHelper.cs`: console commands are a generic registration service. The command's gameplay/debug behavior belongs to the registering mod.
- `src/SMAPI/IModRegistry.cs`: the registry exposes loaded mod metadata and mod-provided APIs. It is not a reason to promote every first-party implementation detail into framework API.
- `src/SMAPI/IGameContentHelper.cs`, `src/SMAPI/IModContentHelper.cs`, and `src/SMAPI/Events/IContentEvents.cs`: game-content and mod-content helpers are generic asset/content surfaces. Domain semantics such as fishing automation, crop automation, or animal definitions are supplied by mods/content owners.

The important reference pattern is therefore:

1. SMAPI core is a boring platform: loader, manifest, lifecycle/events, content helpers, input, config, registry, translation, logging, diagnostics, and command registration.
2. Code mods own product logic.
3. Content packs target a specific code/content owner. The owner interprets schema and behavior.
4. A mod can expose its own API to other mods, but that API is owned by the mod, not automatically by SMAPI.

## DTMAPI Current Shape

DTMAPI already has some SMAPI-like framework pieces:

- `DtmMod`, `IManifest`, `IDtmHelper`, `IMonitor`, config, events, input, translation, mod registry, Workshop helper, diagnostics, UI helper, and content query helper.
- A manifest scanner that recognizes `EntryDll`, `EntryType`, `Type`, dependencies, update keys, local/official/Workshop sources, and official enablement.
- A content/manifest index that can identify content packs, official-style JSON, DTMAPI content, custom animals, audio replacements, and AnimalVoice packs.

The boundary problem is that many public GameBridge APIs are not primitive platform services. They are named and shaped as full product features:

- `IFishingAutomationApi`
- `IActionSpeedApi`
- `IActionCompletionApi`
- `IChestLocatorEnhancerApi`
- `IStrongPlantingGunApi`
- `ISaveSlotsApi`
- parts of `IEquipmentSlotsApi`
- hardcoded internal product behavior such as Oil coal drops

That makes DTMAPI act as the main feature mod for many local packages, while the visible mod often owns only config, keybinds, and presentation.

## Target Layer Model

| Layer | Owns | Should not own |
| --- | --- | --- |
| DTMAPI Framework/Core | Loader, manifest, dependency resolution, entry lifecycle, helper container, events, config, input, logging, registry, translation, diagnostics, Workshop/install state, content-pack discovery, command registration. | Fishing loops, one-action completion policy, action-speed product categories, save-slot count policy, hardcoded item drops, animal species-specific behavior. |
| DTMAPI Content Host | Generic content-pack model, owner targeting, safe file/JSON reads, validation entry points, schema discovery, diagnostics, author tooling hooks. | Domain-specific gameplay policy unless the host is an explicit first-party content owner such as `DTMAPI.CustomAnimals`. |
| DTMAPI GameBridge Primitives | Reviewed Doloc Town native-owner adapters, safe transactions, hook isolation, lifecycle cleanup, leases, arbitration, DTOs that hide raw Unity/Harmony/game types. | Product loops, default player-facing feature policy, single-mod hardcoding, broad stable APIs without multi-owner proof. |
| DTMAPI Official Mods | AutoFishing, ActionSpeed, OneActionComplete, MoreEquipmentSlots, MoreSaves, ChestLocatorEnhancer, StrongPlantingGun, Oil drop policy, Mine machine definitions, debug console UX, other first-party player products. | Core framework promises. Their existence should not by itself promote an API to Stable. |
| DTMAPI Diagnostics/QA | HookProbe, smoke cases, CropHarvestingQa, Y-console internals, evidence capture, report exports, invasive debug verbs. | Ordinary gameplay API claims. Diagnostic success is not public API stability proof. |
| DTMAPI Author Tools | Packager, validator, schema docs, Feishu/author docs, generated tutorials, package doctor. | Runtime gameplay authority. |

This model lets DTMAPI keep its necessary Doloc Town native knowledge without letting every first-party feature become the framework.

## Manifest And Content Pack Adjustment

DTMAPI currently uses `Type` plus `EntryDll` to distinguish code mods and non-code content. That is workable, but the SMAPI-style target should add an explicit owner-targeting concept:

- Keep `EntryDll` and `EntryType` for code mods.
- Add `ContentPackFor` with `UniqueID` and optional `MinimumVersion`.
- Treat `EntryDll` and `ContentPackFor` as mutually exclusive in diagnostics and future loader policy.
- Keep `Type=ContentPack` as a compatibility alias during migration.
- Add a framework helper equivalent to `ContentPacks.GetOwned()` so code/content owners can read only packs that target them.
- Keep generic safe relative file, JSON, translation, and asset helpers separate from domain-specific schemas.

Recommended DTMAPI manifest direction:

```json
{
  "UniqueID": "Author.MyAnimalPack",
  "Name": "My Animal Pack",
  "Version": "1.0.0",
  "ContentPackFor": {
    "UniqueID": "DTMAPI.CustomAnimals",
    "MinimumVersion": "0.5.2-alpha"
  }
}
```

This is more precise than only `Type=ContentPack`: it says who owns the schema and who is expected to read the pack.

## Feature Boundary Split

| Area | Current boundary issue | SMAPI-style target boundary | Near-term action |
| --- | --- | --- | --- |
| Custom animals PNG/WAV/JSON | Already healthy compared with functional mods, but DTMAPI scans generic content paths directly and the C# `ICustomAnimalApi.RequestSpawn` name can confuse the story. | Create an explicit first-party content owner such as `DTMAPI.CustomAnimals`. Content packs target that owner. It reads official JSON plus `custom-animals.json` / `audio-replacements.json`; GameBridge exposes only internal/native primitives. | Preserve current route, document it as the preferred content-pack model, add `ContentPackFor` compatibility, and keep runtime spawn verbs blocked. |
| AutoFishing | `IFishingAutomationApi` is almost the whole product. AutoFishingMod mostly supplies config, hotkey, and options. | AutoFishingMod owns the loop and policy. GameBridge exposes fishing state, selected-rod/cast/reel primitives, minigame decision hooks or input leases, native result DTOs, and optional animation lease. | Keep current API Experimental. Start a primitive design under a new namespace without breaking the current first-party mod. |
| ActionSpeed | One broad API spans many native owners: tools, bottle fill, eat/drink, plant, harvest, machine, animal, auto-fill. | Split by native subdomain. Each primitive has its own owner proof, lifecycle, restore, and conflict model. The first-party ActionSpeed mod combines them. | Do not promote `IActionSpeedApi`. Mark it as first-party product API until split. |
| OneActionComplete | Product policy sits close to shared `ToolCollider` and interact-exit native routes. | GameBridge owns native transaction primitives and callback isolation. OneActionCompleteMod owns which resource/fuel/feed families are completed and when. | Keep API narrow and Experimental. Avoid general "complete everything" wording. |
| Equipment slots | Extra attribute slots include sidecar storage, UI strip, native stats, recovery, and shield-hat behavior. | If exposed, use explicit inventory/equipment storage primitives, restore semantics, and owner conflict rules. MoreEquipmentSlots remains a product mod. | Treat as official product until hot-disable, unregister, recovery, and multi-owner rules are proven. |
| Save slots | DTMAPI currently provides the expanded official save UI behavior behind a product-shaped API. | Separate read-only save UI diagnostics from mutation/slot expansion. MoreSaves owns fixed product count and UX. | Keep `ISaveSlotsApi` Experimental and product-scoped. |
| Chest locator | API directly names the product and merges a specific set of storage roots. | Expose a generic inventory-provider/query extension contract only after more native storage families are reviewed. Product mod chooses policy. | Do not stabilize product-shaped API. |
| Strong planting gun | API and GameBridge implement a fixed three-slot farming-gun product. | GameBridge primitive is "reviewed farming-gun transfer/use slots" with strict native limits. StrongPlantingGunMod owns UX/policy. | Keep fixed verified scope. Avoid presenting it as a general equipment/container API. |
| Crop harvesting / AutoHarvest | `ICropHarvestingApi` is closer to a useful native transaction than most, but it is still narrow and transient. | GameBridge owns scan/harvest transaction over reviewed `PlantBasin` owners. AutoHarvestMod owns scheduling, filters, interval, and UI. | Preserve as Experimental transaction API. Do not expand to trees/forage without native-owner review. |
| Camera / Zoom | This is the healthiest functional split: ZoomMod owns UX and DTMAPI owns a lease-based primitive. | Use camera lease as the model for other global mutable features: owner token, priority, snapshot, release, restore. | Keep as the reference pattern, but do not confuse playable zoom with background/panorama ownership. |
| Audio replacement | Event replacement is generic in shape but current public C# allowlist and AnimalVoice content path are separate stories. | Mod/content pack owns assets and desired event mapping. DTMAPI owns reviewed event policy, playback, suppression, fail-open behavior, and unload cleanup. | Keep event-level and reviewed-event-only. Use content owner for AnimalVoice packs. |
| Oil | Item JSON is content-owned, but coal-drop behavior is hardcoded GameBridge product logic. | OilMod registers a resource-drop policy through a generic reviewed drop primitive. GameBridge only supplies safe resource hit/drop transaction. | Move hardcoded `crude_oil` behavior out of GameBridge when the primitive exists. |
| Mine | MineMod supplies content/definitions, while DTMAPI supplies a broad machine production engine. | MineMod owns machine definition, output table, recipe policy, balance. DTMAPI may own a generic machine engine only if multiple packs use the same schema. | Keep machine API Experimental. Separate schema owner from engine owner. |
| Debug console / Y key | Useful product tooling, but it binds powerful native debug verbs through DTMAPI. | Keep as Diagnostics/QA product. Public ordinary mods should use generic console command registration, not Y-console internals. | Add a future `ICommandHelper` style author API if ordinary console commands are needed. |

## Custom Animals As The Positive Model

The custom livestock route should be protected as the preferred pattern:

- The pack owns identity, item IDs, shop entry, produce, official-style JSON rows, PNG frames, and WAV assets.
- DTMAPI owns generic bridge schemas and validation: `custom-animals.json`, AnimalVoice mapping, frame lookup, template animator/AI routing, diagnostics, and package doctor checks.
- Doloc Town owns authoritative simulation: animal bags/stores, growth, feeding, breeding, production, room capacity, save behavior, and normal lifecycle.

The SMAPI-style refinement is to make the schema owner explicit:

- `DTMAPI.CustomAnimals` becomes a content owner, not merely an implicit scanner in the GameBridge.
- Custom animal packs declare `ContentPackFor: DTMAPI.CustomAnimals`.
- `DTMAPI.CustomAnimals` reads its owned packs through a content-pack helper.
- GameBridge exposes internal bridge primitives to that owner, not broad public animal-spawn verbs.
- Public C# custom entity registry definitions can remain StableCandidate, but runtime creation verbs stay blocked until native adapters are proven.

This keeps the strongest current route strong while giving it a cleaner explanation for authors.

## Public API Rules

Following the SMAPI boundary does not mean "less DTMAPI." It means DTMAPI public API must be more boring and more reusable.

Promotion rules should be stricter for product-shaped APIs:

1. A first-party feature mod is not proof that an API is stable.
2. A smoke or QA mod is not proof that an ordinary mod can compose with the API.
3. Public APIs should expose primitives, leases, transactions, read-only status, content helpers, and owner-scoped registration, not whole player features.
4. A global mutating API needs explicit conflict semantics: exclusive lease, priority, merge policy, or documented single-owner behavior.
5. Native-owner proof belongs in GameBridge docs, but product policy belongs in an official mod or content owner.
6. Diagnostic APIs can be powerful, but their status and package labels must keep them out of ordinary gameplay contracts.

## Migration Sequence

1. Rename the architecture in docs and package labels:
   - Framework/Core
   - Content Host
   - GameBridge Primitives
   - Official Mods
   - Diagnostics/QA
   - Author Tools
2. Add manifest model support for `ContentPackFor` while preserving `Type=ContentPack`.
3. Add a content-pack helper that can return packs owned by a code/content owner.
4. Move custom animal scanning behind an explicit `DTMAPI.CustomAnimals` owner without changing file paths at first.
5. Reclassify current product-shaped APIs as first-party product APIs in the matrix where appropriate, keeping their current Experimental/Diagnostic status.
6. Design replacement primitives one area at a time:
   - fishing primitives before changing AutoFishing;
   - animation/timer leases before changing ActionSpeed;
   - resource-drop primitive before moving Oil coal drops;
   - command helper before making Y-console behavior reusable.
7. Convert first-party mods to use primitives internally once each primitive is proven.
8. Only then consider public StableCandidate promotion for primitives used by at least two real mods.

## Non-Goals

- Do not copy SMAPI internals, names, or source code mechanically.
- Do not move fragile Doloc Town reflection/Harmony work into ordinary mods.
- Do not break existing first-party mods only for naming purity.
- Do not pretend content packs are fully official when they still use DTMAPI bridge schemas.
- Do not promote runtime custom animal creation, arbitrary Wwise replacement, generic tree/forage harvest, or broad vehicle/entity APIs through this boundary split.

## Bottom Line

If DTMAPI follows SMAPI's useful boundary model, the main adjustment is not a rewrite. It is a split of authority:

- Core becomes the boring platform.
- Content packs target an explicit owner.
- GameBridge exposes reviewed native primitives.
- Official feature mods own product behavior.
- Diagnostics stay powerful but clearly non-public for gameplay.

Under that model, the PNG + WAV + JSON custom livestock route is the best current example to build around. AutoFishing and ActionSpeed are the clearest examples to split next, because they currently make DTMAPI itself act as the functional mod.

## Validation

Documentation-only research. No runtime code, source code, build, or game smoke was changed or run.

Checked against:

- SMAPI manifest, mod, helper, content pack, content helper, command helper, mod registry, and content event interfaces.
- DTMAPI `IManifest`, `IDtmHelper`, events, content query, manifest scanner, content/manifest registry, shadow content registry, public API matrix, and current boundary reviews.
- Prior records for functional mods, all local DTMAPI mods, and custom animal content packs from 2026-07-05.
