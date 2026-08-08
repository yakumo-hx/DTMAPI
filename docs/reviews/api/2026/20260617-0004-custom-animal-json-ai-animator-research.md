# 20260617-0004 Custom Animal JSON AI Animator Research

## Status

recorded-docs-only

## Source Request

The user wants DTMAPI to support genuinely new animals, independent from the current built-in livestock/small-animal set. The desired author experience is Unity-free for ordinary authors: JSON should describe behavior such as "when condition X, AI does Y" and animation timing such as "state A plays these frames for X seconds". The research must explain what the current project already knows, where the reverse/unpacked material lives, what is still unclear, and what likely still requires Unity-authored assets.

This report is discovery only. It does not promote `ICustomAnimalApi` runtime creation, does not create a goal file, and does not change code.

## Required Project Context Read

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/api/public-api-matrix.md`

## Existing Reports To Start From

Use these before any animal implementation goal:

- `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
  - Current verdict: existing animal lifecycle owners are partially mapped, but stable custom animal species/runtime creation is blocked.
- `docs/reviews/api/native-owner-domains/review-rounds/ROUND-3-final-confidence.md`
  - Key confidence split: existing animal native creation/release/catch 86, persistence/room host 84, movement/pathing 64, custom animal stable API 20, custom animal experimental native path 66.
- `docs/api/public-api-matrix.md`
  - `ICustomAnimalApi` definition/registry is `StableCandidate`.
  - Native runtime creation verbs remain `Experimental` and blocked; `RequestSpawn` returns `runtime-creation-blocked`.
- `docs/api/040-stable-custom-entity-apis.md`
  - Historical file name says stable custom entity APIs, but current meaning is registry-first: definition/validation/query/status are usable, native creation is not.
- `docs/hook-map/README.md`
  - `CustomAnimals.RegistryContract` is `configured-blocked`.
  - `Animals.ViewerRendering` is a separate verified UI extension path for existing animals only.
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
  - Third-party sample research has auto animal collect/pet demand signals, but it is not permission to copy code.
- `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
  - Notes that DTMAPI has AnimalViewer and CustomAnimal registry contracts, but no custom native creation or automation API yet.

## Reverse And Unpacked Material Locators

Current reverse baseline:

- `references/doloc-town/reverse/builds/23762374_public_C416D4/README.md`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/input/Assembly-CSharp.dll`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/decompiled/Assembly-CSharp/`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/content-configs/`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/diffs/`

Current animal config TextAssets extracted with UnityPy:

- `content-configs/animal_tbanimal.json`
- `content-configs/animal_tbanimaldocument.json`
- `content-configs/animal_tbanimalstate.json`
- `content-configs/animal_tbfeed.json`
- `content-configs/animal_tbhusbandry.json`
- `content-configs/animal_tbhusbandryenergy.json`
- related acquisition/store data in `content-configs/item_tbitem.json`, `content-configs/store_tbstore.json`, and `content-configs/store_tbstoreitemlist.json`

Current decompiled animal code entry points:

- `decompiled/Assembly-CSharp/DolocTown/Animal.cs`
- `AnimalAI.cs`, `AnimalController.cs`, `AnimalTask*.cs`, `AnimalWork*.cs`
- `AnimalRenderer.cs`, `AnimalMove.cs`, `AnimalEat.cs`, `AnimalExcrete.cs`
- `AnimalManager.cs`, `AnimalSystem.cs`, `IAnimalHost.cs`, `ItemAnimalPackage.cs`
- `Feeder.cs`, `Toilet.cs`, `LivestockNursery.cs`, `MilkingMachine.cs`, `LintRoller.cs`, `HoneyComb.cs`
- config classes under `decompiled/Assembly-CSharp/DolocTown/Config/Animal/`

Official Workshop/extracted documentation:

- `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/013_03小动物_POkcwjhF3iPzmikWRZ2cn8n9ncf.md`
- `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/044_03ID对照表（小动物）_UdOwwqfOLiFcFMkW5rrchKVUnpd.md`
- `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/018_01美化模组（替换现有贴图）_SNOywxdl5iccH8kO413czWBKnwe.md`
- `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/017_05贴图锚点说明_Rdp1with9ih1frk7Pl8cbFD9nDc.md`
- sample crawl images under `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/assets/03小动物_*.png`

Important asset gap:

- The current repo contains extracted config JSON and official documentation/sample images.
- It does not currently contain a full exported official `RuntimeAnimatorController`, animation clip, prefab, or Addressables asset tree for `game_anim_animal_*`.
- Any animator/clip/prefab-level inspection needs a new local-only asset extraction pass from the installed game's Addressables/bundles. Do not commit official extracted runtime assets.

## Current Native Animal Shape

The current game has four configured animal IDs in `animal_tbanimal.json`:

| ID | Title | Child animator | Adult animator | Produce LUT |
| --- | --- | --- | --- | --- |
| `slime` | 变形蜜虫 | `game_anim_animal_slime_child` | `game_anim_animal_slime` | `slime_produce` |
| `chicken` | 立尾雉 | `game_anim_animal_chicken_child` | `game_anim_animal_chicken` | `chicken_produce` |
| `goat` | 角羊驼 | `game_anim_animal_goat_child` | `game_anim_animal_goat` | `goat_produce` |
| `marsh_pangolin` | 沼泽兽 | `game_anim_animal_marsh_pangolin_child` | `game_anim_animal_marsh_pangolin` | `marsh_pangolin_produce` |

Native animal instances are not just visual clones:

- `Animal` owns `AnimalInfo proto`, persisted `AnimalData`, room/home membership, mood/energy/metabolism/growth/breeding counters, and an `AnimalController`.
- `IAnimalHost`/room paths add/remove animals through `DM_animal`, `AnimalSystem`, home room, current room, and position validation.
- `ItemAnimalPackage` is the normal acquisition/release path; package rows point at `preset_animal`.
- `AnimalAI` is a state machine using native state/work/task classes. It has specific default free-time states for the four built-in IDs and falls back to normal free-time for unknown IDs.
- `AnimalRenderer` requires `SpriteRenderer` and `Animator`. It assigns `RuntimeAnimatorController` from `AnimalInfo.Levels[*].Animator.Asset`, plays named states such as `idle`, `eat`, `jump_ready`, and `jump`, and uses renderer-side movement/eat/jump lifecycle.

## What Official Workshop Supports Today

The official Workshop docs for small animals are beauty/replacement oriented:

- The small-animal page points to existing IDs and an official example path for replacing small-animal visuals.
- The ID table lists only `chicken`, `goat`, `marsh_pangolin`, and `slime`.
- The beauty-mod page describes replacing images by exact file names, with missing frames falling back to original sprites.
- The pivot docs describe sprite/anim image pivot metadata through sidecar JSON.

This supports existing visual replacement. It does not prove:

- adding a new animal row that survives runtime config loading;
- adding a new `RuntimeAnimatorController`;
- registering a new animal AI state graph;
- creating save/load-safe runtime custom animal instances;
- integrating a new species into animal shop/package/encyclopedia/UI without GameBridge work.

## Animation Boundary

The strongest technical boundary found in this pass is asset loading:

- `DolocAssetCache` has a mod override path for `Sprite`, via `modManager.LoadSpriteFromFile`.
- `AnimatorAsset` loads `RuntimeAnimatorController` through `DolocAPI.GetAsset<RuntimeAnimatorController>`.
- No equivalent official mod-file override path was found for `RuntimeAnimatorController`.

Therefore, a Unity-free author workflow cannot simply say "put a JSON animator in Content and the native Animator will load it." DTMAPI needs one of these adapter strategies:

1. Template controller plus DTMAPI sprite-frame overlay.
   - Reuse a native animal controller only as a state/time source.
   - DTMAPI owns a per-instance frame player that swaps `SpriteRenderer.sprite` from namespaced PNG frames according to JSON.
   - This is the best first Unity-free prototype because authors provide PNGs and frame timings, while DTMAPI avoids creating Unity animator controllers in the player.
2. DTMAPI-owned sidecar sprite animator without native Animator semantics.
   - DTMAPI creates/owns the visual GameObject and behavior tick loop.
   - Better for arbitrary states, but weaker integration with native `AnimalRenderer`, animal panel, and room/host systems.
3. Optional advanced Unity bundle path.
   - Advanced authors can supply Unity-authored controller/clip/prefab bundles later, if DTMAPI defines a safe loader and validation story.
   - This should remain optional and not be required for ordinary JSON authors.

Likely Unity-required content:

- genuine new `RuntimeAnimatorController` and animation clips used directly by native `Animator`;
- prefab authoring with custom colliders/particles/VFX hierarchy;
- Wwise event/bank authoring beyond simple reviewed audio routes;
- complex rigging, blend trees, or controller transitions that cannot be represented by DTMAPI's JSON frame player.

Likely Unity-free content if DTMAPI implements the adapter:

- sprite sheets or named PNG frames;
- pivot/pixels-per-unit sidecars;
- state frame timing and loops;
- template-state mapping (`idle`, `walk`, `run`, `eat`, `sleep`, `produce`, `jump_ready`, `jump`);
- behavior rules compiled into a DTMAPI-owned finite state/action engine.

## AI Boundary

The native `AnimalAI` is not a generic data-driven behavior tree today:

- Built-in species choose between species-specific and normal states in code.
- Decisions call task generators for sleep, hunger/feeders, excretion/toilets, breeding/nursery, chicken nests, honeycombs, movement, rain escape, and wandering.
- Many useful behavior atoms already exist, but the state graph and species selection are not exposed as official JSON.

For DTMAPI, the safest JSON model is a constrained behavior DSL over reviewed atoms, not arbitrary code:

- `when`: time/weather/room/building/hunger/metabolism/mood/breeding/product-ready/interactable-available.
- `priority`: deterministic ordering when multiple rules apply.
- `do`: native-like actions such as `wander`, `sleep`, `find_food`, `eat`, `find_toilet`, `excrete`, `find_nursery`, `breed`, `find_machine`, `produce`, `return_home`, `flee_rain`, `play_animation`, `emit_product`.
- `fallback`: template behavior (`normal`, `chicken`, `slime`, `goat`, `marsh_pangolin`) until custom actions are verified.
- `provider`: optional C# extension point later, isolated under owner diagnostics.

This should be compiled into DTMAPI-owned runtime state, then adapted to native animal/room operations only after each atom has method-body review and smoke evidence.

## Recommended Architecture Direction

Do not repeat the failed vehicle clone pattern. Avoid copying or mutating native global animator/sprite/controller assets and avoid global official names such as `game_anim_animal_*` for mod-owned assets.

Recommended product shape:

```text
Workshop content pack
  -> dtmapi.animal.json
  -> assets/animals/<owner>/<species>/frames/*.png
  -> optional advanced Unity-authored bundle later

DTMAPI Core
  -> validate JSON schema
  -> owner-scoped registry and dependency/status rows
  -> save-scoped runtime state and cleanup

DTMAPI.GameBridge.DolocTown
  -> animal template/native owner adapters
  -> DTMAPI sprite-frame animator
  -> constrained AI atom executor
  -> evidence/status for blocked native edges
```

Suggested implementation slices:

1. Docs/schema-only prototype:
   - Define `dtmapi.animal.json` with species metadata, template, animation states, and AI rules.
   - Map this schema to existing `CustomAnimalSpeciesDefinition` without enabling `RequestSpawn`.
2. Animation proof:
   - Create a DTMAPI-owned sprite-frame animator component over a safe test object or existing animal viewer/debug harness.
   - Prove namespaced PNG frames, pivot metadata, and state timing without touching native animal creation.
3. Existing-template runtime proof:
   - Choose one template species, preferably `slime` or `chicken`, and test whether a DTMAPI-owned visual overlay can follow a native animal instance without replacing global sprites.
   - Evidence must include room transition, save/load, return-to-title cleanup, and clean exit.
4. Custom spawn adapter research:
   - Re-open `AnimalManager`, `IAnimalHost`, `AnimalSystem`, `ItemAnimalPackage`, `Animal`, `AnimalAI`, and `AnimalRenderer` in current build `23762374_public_C416D4`.
   - Decide whether to insert a custom `AnimalInfo` into runtime tables, create sidecar runtime animals, or keep native creation blocked.
5. Full gameplay slice:
   - One developer-only custom animal pack.
   - Third-save smoke: spawn/acquire, render, wander, feed, excrete/product, save/load, room transition, return-to-title, clean exit.

## Open Questions For Product Design

- Does MVP need native animal shop/package/animal panel/encyclopedia integration, or is a DTMAPI custom animal menu acceptable first?
- Is template behavior acceptable for the first release, e.g. custom species chooses `normal`, `chicken`, `slime`, `goat`, or `marsh_pangolin` behavior?
- Should ordinary authors be limited to PNG frame animations first, with Unity bundles as an advanced optional path?
- Are custom animals required to breed with built-in animals, or only with same custom species at first?
- Should products use official item/content JSON only, or may DTMAPI create sidecar products before official item rows exist?
- How should Workshop enable/disable remove live custom animals from a save: hide, freeze, package into recovery item, or block loading with a clear error?

## Must Not Claim Yet

- Do not claim that `ICustomAnimalApi.RequestSpawn` creates native animals.
- Do not cite AnimalViewer hidden-product UI as proof of custom animal lifecycle.
- Do not cite official small-animal texture replacement as proof of new species support.
- Do not cite registry smoke as proof of animation/AI/native spawn.
- Do not clone built-in animal GameObjects/controllers and mutate shared/global assets as a production strategy.

## Validation

No code changed. No build, game launch, or smoke test was run. This is a static research report over current project docs, current reverse baseline metadata/decompiled names, extracted config JSON, and official Workshop documentation extracts.
