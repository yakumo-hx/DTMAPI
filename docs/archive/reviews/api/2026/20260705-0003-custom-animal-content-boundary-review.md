# Custom Animal JSON/PNG/WAV Content Boundary Review

Date: 2026-07-05
Status: recorded / docs-only
Scope: Review the user-identified strongest current DTMAPI core route: adding livestock animals through near-official JSON plus PNG and WAV assets, and compare its boundary with the functional-mod boundary problems recorded earlier today.

## Sources Read

- Author guide: `author-docs/content-packs/custom-animal-json-png-wav.md`.
- Local draft/generator artifacts: `author-docs/content-packs/new-farm-animal-draft/README.md`, generated Hatch frame atlas images.
- Current local runtime content packs under `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`: `DTMAPI_HatchAssets`, `DTMAPI_MoleAssets`, `DTMAPI_DreckoAssets`, `DTMAPI_OilfloaterAssets`, `DTMAPI_LightningChicken`, and `DTMAPI_ShellCrab`.
- Content/runtime code: `src/DTMAPI.Core/Runtime/ContentManifestRegistry.cs`, `src/DTMAPI.Core/Runtime/ShadowContentRegistry.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridge*.cs`, and `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`.
- Public API boundary code: `src/DTMAPI.Abstractions/CustomEntities.cs`, `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`.
- Existing evidence records: `docs/updates/2026/20260630-0001-hatch-png-custom-animal.md`, `20260701-0001-hatch-animal-voice-audio-replacement.md`, `20260701-0002-shell-crab-animalvoice-json.md`, `20260701-0008-mole-drecko-hatch-products.md`, `docs/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md`, `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`, `docs/api/public-api-matrix.md`, and `docs/hook-map/README.md`.

## Executive Judgment

The user's instinct is right: the JSON + PNG + WAV custom livestock path is currently the best DTMAPI boundary in the project.

It is not "DTMAPI as a functional mod with a tiny visible shell". It is closer to the desired SMAPI-like content-pack relationship:

- The content pack owns animal identity, item IDs, shop entry, produce table, document rows, PNG frames, and WAV voice assets.
- Doloc Town's native content and animal systems still own lifecycle, package release, growth, feeding, breeding, production, room capacity, pathing, and ordinary save integration.
- DTMAPI owns only the missing bridge layer: content-pack discovery, DTMAPI bridge schemas, template animator routing, per-renderer PNG sprite mapping, template AI name mapping, short AnimalVoice WAV replacement, validation diagnostics, and custom-animal sleep/task safety boundaries.

This route should become the model for future DTMAPI extension design: authors provide data/assets and product identity; DTMAPI provides generic adapters and diagnostics; native game systems keep authoritative simulation.

## Runtime Package Evidence

The current local runtime packages support the split above. The four main PNG-route packages are pure content-pack-style packages with official JSON tables, DTMAPI JSON bridge files, loose PNG frames, and WAV files.

| Package | UniqueID | Species | Template / AI | Animator mode | PNG | WAV | Sound scopes | Product/shop evidence |
| --- | --- | --- | --- | --- | ---: | ---: | --- | --- |
| `DTMAPI_HatchAssets` | `DTMAPI.HatchAssets` | `hatch` | `chicken` / `chicken` | `pngSpriteOverride` | 44 | 2 | child/adult chicken pet events | `sack_hatch`, `hatch_produce`, shop `sack_hatch` |
| `DTMAPI_MoleAssets` | `DTMAPI.MoleAssets` | `mole` | `marsh_pangolin` / `marsh_pangolin` | `pngSpriteOverride` | 45 | 2 | child/adult pangolin pet events | `sack_mole`, `mole_produce`, shop `sack_mole` |
| `DTMAPI_DreckoAssets` | `DTMAPI.DreckoAssets` | `drecko` | `goat` / `goat` | `pngSpriteOverride` | 36 | 2 | child/adult sheep pet events | `sack_drecko`, `drecko_produce`, shop `sack_drecko` |
| `DTMAPI_OilfloaterAssets` | `DTMAPI.OilfloaterAssets` | `oilfloater` | `slime` / `slime` | `pngSpriteOverride` | 48 | 2 | child/adult honey-amoeba pet events | `sack_oilfloater`, `oilfloater_produce`, shop `sack_oilfloater` |
| `DTMAPI_LightningChicken` | `DTMAPI.LightningChickenMod` | `dtmapi_lightning_chicken` | `chicken` / `chicken` | `runtimeOverrideController` | 0 | 0 | none | older/non-main route |
| `DTMAPI_ShellCrab` | `DTMAPI.ShellCrabMod` | `shell_crab` | `goat` / `goat` | `assetBundle` | 38 | 2 | child/adult sheep pet events | advanced AssetBundle route |

Visual spot-check of the generated Hatch frame atlases (`idle`, `move`, `eat`) also matches the authoring model: the package is made of named loose frames, not a DLL behavior mod or a hardcoded GameBridge animal.

## Ownership Split

| Layer | Owns in this route |
| --- | --- |
| Official-style JSON | `animal_tbanimal.json`, `animal_tbanimaldocument.json`, `item_tbitem.json`, `item_tbitemspawn.json`, `mod_tbmodstoreextension.json`, optional husbandry tables. These describe "what the animal is" through the game's content model. |
| DTMAPI content JSON | `Content/DTMAPI/manifest.json`, `custom-animals.json`, and `audio-replacements.json`. These describe "how DTMAPI bridges this official-like animal to custom assets". |
| Asset files | Loose PNG frames and WAV files owned by the content pack. |
| DTMAPI Core | Manifest/content-pack recognition, official enablement respect, content capability indexing, diagnostics rows. |
| DTMAPI GameBridge | Fragile native hooks and adapters: `AnimatorAsset.TryLoadAsset`, `SpriteOverrideHandler.TryGetModOverrideSprite`, `AnimalAI.GetDefaultAnyState`, `Animal.PlayAnimalSound`, Wwise event interception, and custom-animal sleep/task safety. |
| Native Doloc Town | Animal creation through animal bags/stores, lifecycle, growth, feeding, breeding, production, equipment lookup, room capacity, save ownership, and ordinary AI execution. |

The important point is that DTMAPI is not providing "Hatch gameplay" as a feature product. It provides reusable bridge behavior for any enabled content pack that supplies the same schema.

## Why This Boundary Is Healthier Than Current Functional Mods

Earlier boundary reviews found many local functional mods are thin shells over feature-shaped DTMAPI services. AutoFishing, ActionSpeed, OneActionComplete, MoreEquipmentSlots, MoreSaves, StrongPlantingGun, ChestLocatorEnhancer, and parts of Oil/Mine all make DTMAPI carry much of the product behavior.

The custom animal content route is healthier because:

- There is no ordinary code mod shell. A new animal can ship as content.
- The pack owns user-facing identity and balance: names, items, shop entry, produce, frames, sounds.
- The DTMAPI fields are generic bridge fields, not `HatchFeature` fields.
- Native game paths still do the real animal transaction work.
- Removal/disable boundaries are easier to reason about: an enabled content pack contributes data and assets; DTMAPI's shared bridge remains inert without registered definitions.
- Multiple real examples already exercise the same schema across templates (`chicken`, `goat`, `marsh_pangolin`, `slime`).

This is much closer to the desired DTMAPI role than AutoFishing-style APIs where the mod mostly toggles a GameBridge-owned automation engine.

## Caveats

This route is strong, but it should not be over-claimed.

- It is near-official, not purely official. `custom-animals.json`, `audio-replacements.json`, `pngSpriteOverride`, AI template mapping, and AnimalVoice scoping are DTMAPI extensions.
- It is not the same as the public C# `ICustomAnimalApi.RequestSpawn` path. The matrix still says custom entity runtime creation verbs are blocked; `RequestSpawn` returns `runtime-creation-blocked`. The working livestock route uses official-style content plus native item/store release paths and DTMAPI asset/audio bridges.
- It does not create brand-new AI. A new species currently reuses a native template such as `chicken`, `goat`, `marsh_pangolin`, or `slime`.
- Template choice also selects native production-equipment behavior. JSON can define product tables, but it cannot currently create a fifth production-device family or species-filtered equipment ownership.
- Frame and sound cross-references remain author footguns: missing PNG frames fall back to template frames, mismatched `sound_event` / `nativeSoundEvent` falls back to native audio, and `sprite_size` affects native collider/emotion behavior rather than DTMAPI PNG mapping.
- The AnimalVoice route is deliberately short-SFX scoped. It is not BGM, looped audio, Wwise bank replacement, or arbitrary event replacement.
- Sleep/task safety now has a good Hatch/Shell Crab record, but it is still an internal GameBridge compatibility boundary for template custom animals, not proof of a stable general animal AI API.

## Recommended Direction

1. Treat "content-pack schema + validator + diagnostics" as the stable-candidate product for custom livestock, not the C# `ICustomAnimalApi` runtime spawn verbs.
2. Keep the PNG route as the default author route and the AssetBundle route as advanced. The PNG route is closer to official content authoring and easier for non-programmers.
3. Add or improve a preflight validator for this exact package family:
   - JSON parse and schema checks.
   - `speciesId` / `animal_tbanimal.id` / animal bag `preset_animal` consistency.
   - animator key consistency.
   - template frame-count and missing-number checks.
   - `sound_event` / `nativeSoundEvent` consistency.
   - WAV path-in-pack checks and reviewed-event checks.
   - official document field checks.
   - duplicate `UniqueID`, duplicate species, duplicate suppressing AnimalVoice scopes.
4. Keep GameBridge generic. Do not add species-specific Hatch/Mole/Drecko/Oilfloater behavior unless it is temporary diagnostics with a retirement path.
5. Document custom animal packages as official DTMAPI content packs, not as migrated functional mods.
6. Use this route as the architectural pattern for future content-like features: native official table first, DTMAPI bridge schema second, reusable adapter third, product package last.

## Bottom Line

The PNG + WAV + JSON livestock route is the clearest current example of DTMAPI acting like a modding platform instead of "the mod itself".

The clean rule it demonstrates is:

- Mod/content pack owns identity, data, assets, balance, and author intent.
- DTMAPI owns bridge mechanics, safety guards, validation, and diagnostics.
- Doloc Town owns authoritative simulation and save/gameplay transactions.

This should be protected as the preferred design boundary and used as a counterexample when reviewing DTMAPI-heavy functional mods.

## Validation

Documentation-only review. No code was changed, no build was run, and no game smoke was launched for this record.

Checked:

- Current author guide and Feishu/new-animal draft structure.
- Local runtime content package JSON summaries and asset counts.
- Custom animal GameBridge scanner/bridge source.
- AudioReplacement AnimalVoice scanner/scope source.
- Public custom entity API blocked runtime creation source.
- Public API matrix and hook-map evidence records.
- Existing Hatch/Shell Crab/Mole/Drecko/Oilfloater update and manual QA records.
