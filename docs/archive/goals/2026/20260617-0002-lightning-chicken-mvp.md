# 2026-06-17 Lightning Chicken MVP Goal

## Source Request

Implement DTMAPI `0.5.3-alpha` and add the developer-only local package `DTMAPI.LightningChickenMod` as an MVP proof that a new animal can have an isolated native animal row, package item, animal-shop purchase path, chicken-template AI, and an independent animator binding without polluting the vanilla chicken.

## Target Animal

- Species ID: `dtmapi_lightning_chicken`
- Display name: `闪电鸡`
- Package item ID: `sack_dtmapi_lightning_chicken`
- Shop extension target: `animal_shop`
- Template species: `chicken`
- Movement: chicken values multiplied by 2 (`move_speed = 3.6`, `run_speed = 10.8`; native renderer clamps run speed above 10)
- Production: chicken `produce_spawn_entry` unchanged, `metabolism_increase = 2.08334`
- Adult animal sale price: `2000`
- Package purchase price: `2000`
- Package sale price: `1000`

## Required Boundaries

- Keep raw Doloc Town/Harmony/Unity reflection in `DTMAPI.GameBridge.DolocTown`.
- Do not expose decompiled Doloc Town types through `DTMAPI.Abstractions`.
- Keep `ICustomAnimalApi.RequestSpawn` blocked/experimental; this MVP uses native tables, native animal packages, and the native `Animal` lifecycle.
- Use `Content/DTMAPI/custom-animals.json` as the MVP descriptor with these minimum fields: `speciesId`, `templateSpeciesId`, `aiTemplate`, `animatorMode`, `adultAnimatorKey`, `childAnimatorKey`, `movementMultiplier`, `metabolismMultiplier`, `packageItemId`, `shopItemListId`.
- Ordinary DTMAPI mods must not be installed under `BepInEx/plugins`; the Lightning Chicken package must be a developer-only official-local package.
- Manual/game testing must use save slot 11 and must acquire the runtime lock before install or launch.

## Native Owner Findings

- Native package release path: `ItemAnimalPackage` creates `new Animal(_func.PresetAnimal_Ref, DateNow)` and releases it through `IAnimalHost.AddAnimal`.
- Native shop extension path: `Tables.HandleModStoreExtension` appends `mod_tbmodstoreextension` items into `StoreInfo.ItemRecords_Ref.ItemList`, replacing only duplicate item IDs.
- Native AI owner: `AnimalAI.GetDefaultAnyState(string)` maps unknown species to `Normal_FreeTimeState`; `dtmapi_lightning_chicken` must be mapped narrowly to `Chicken_FreeTimeState`.
- Native chicken-equipment owner: `Animal._HandleChickenNestBroken(AnimalEvent)` has a hard-coded `protoName != "chicken"` guard.
- Native animator owner: `Animal.OnRender` and `DEBUG_SetAdult` assign `Renderer.animatorController` from `proto.Animator.Asset` or `proto.ChildAnimator.Asset`.
- Native movement owner: `AnimalRenderer.MoveTo(Vector2,float,Action)` clamps speed to `[1, 10]`, so `run_speed = 10.8` may observe as `10`.

## Implementation Requirements

- Add `CustomAnimals.NativeTemplateAdapter` inside `DTMAPI.GameBridge.DolocTown`.
- Add narrow hooks for AI template mapping, animal render/growth animator override diagnostics, and chicken-template equipment behavior only if they do not require a broad IL transpiler.
- Add `DTMAPI.LightningChickenMod` content tables:
  - `animal_tbanimal.json`
  - `animal_tbanimaldocument.json`
  - `animal_tbhusbandry.json`
  - `item_tbitem.json`
  - `mod_tbmodstoreextension.json`
  - `Content/DTMAPI/custom-animals.json`
- Prefer official extra-chicken sample PNG assets if present; otherwise temporary generated/placeholder assets must be explicitly labeled temporary.
- Do not replace vanilla `chicken`, `sack_chicken`, or the base `animal_shop` row.

## Validation Requirements

- Static checks for unique species/item/shop IDs, descriptor shape, and hook target signatures.
- Release build/tests and version consistency for `0.5.3-alpha` / `0.5.3.0`.
- Update API matrix, hook map, debug smoke matrix, and update record.
- Add or document planned smoke flag `-AutoExerciseLightningChicken -SaveSlot 11`.
- Game smoke must install DTMAPI plus the developer Lightning Chicken mod under the runtime lock, load save slot 11, buy/release through the native animal shop package path, compare vanilla chicken versus lightning chicken AI/animator/movement/metabolism, reload the save, and exit without leftover `DolocTown.exe`.

## Stop Rules

Stop implementation and write a `docs/reviews/api/2026/...` research/problem report if:

- A genuinely independent native `RuntimeAnimatorController` or clip path requires Unity Editor or AssetBundle authoring.
- The native phone booth/animal shop package route cannot buy and release the custom species.
- AI template support requires a broad IL transpiler or affects vanilla chicken.
- Store, texture, animator, or animal table changes pollute vanilla chicken.
- Two non-Unity animator attempts cannot prove the strict native controller/clip requirement.
- The work needs ChatGPT web deep research; the report must include tried paths, logs/evidence, failure boundaries, and next questions.
