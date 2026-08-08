# 20260717-0005 Kenenimuu Decorative Animal Capture Native-Owner Review

Status: recorded / read-only feasibility review / implementation not started
Date: 2026-07-17
Reverse baseline: `references/doloc-town/reverse/builds/23762374_public_C416D4`
Runtime resource checked: the locally installed build matching the reviewed public baseline
Related domain review: `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
Related public matrix: `docs/api/public-api-matrix.md`

## Source Request

The requested behavior is to put the animals outside Kenenimuu's home into the livestock sack and take them back to the player's farm. The visible animals can currently only be fondled. The review must determine whether they are ordinary livestock with a special owner/property, whether a small capture change is possible, and whether the animals have a spawn/rebuild mechanism.

This is a read-only native-owner and resource review. It does not patch DTMAPI or the game, launch the game, edit a save, or promise a public animal-capture API.

Required safety boundary:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Verdict

The Kenenimuu-home animals are **not ordinary `Animal` instances with a special NPC owner flag**. They are a different runtime type and lifecycle:

- ordinary livestock: `Animal` plus `AnimalData`, `AnimalManager`, `AnimalSystem`, a farm `IAnimalHost`, home/current rooms, and save data;
- Kenenimuu-home animals: `DecorativeAnimal` instances created by scene-owned `DecorativeAnimalManager` components, with only local decorative movement/fondle/weather behavior and no livestock owner, home room, husbandry data, or archive registration.

The ordinary sack therefore cannot capture them by changing one Boolean property. A narrow conversion feature is feasible, but the correct smallest boundary is **decorative source -> filled native animal sack -> native barn release**, plus per-save suppression of the captured source slot. Omitting the suppression state makes the scene generate the animal again and creates an unlimited-animal exploit.

## Current Scene Resource Evidence

The current build settings map the outdoor scene containing the house entrance to `Assets/Scenes/City/室外场景/city_多洛可小镇后山.unity` (`level16`). The house interior is a separate scene (`city_凯涅尼木的家`, `level83`); the requested animals are in the outdoor back-mountain scene, not in the interior room.

The outdoor scene contains a `凯涅尼木相关` hierarchy with two conditional groups:

| Scene group | Native component | Serialized child-animal entries | Conditional evidence |
| --- | --- | --- | --- |
| `默认动物/动物群-默认` | `DecorativeAnimalManager` | `chicken` child, `chicken` child, `goat` child | references `MISSION_PROGRESS.AnimalAppear` and `COMMERCIAL_UNLOCK.skychild` |
| `入驻后动物/小动物群-入驻` | `DecorativeAnimalManager` | the same three plus `marsh_pangolin` child | references `COMMERCIAL_UNLOCK.skychild` |

The exact condition reversal/mode is scene serialization detail and was not promoted into a code contract. The resource evidence is sufficient to show that progress selects the decorative group and that the post-entry group adds the marsh animal. Scene object/path IDs are build-local evidence and must not be used as stable runtime identifiers.

## Why Fondling Works But The Sack Does Not

### Decorative path

`DecorativeAnimal.OnTouch` shows the fondle operation tip. `OnInteract` runs a short player interaction, faces the player, emits the love emotion, and delays the decorative idle state. It does not create or mutate `AnimalData` and does not assign `DolocAPI.CurrentAnimal`.

### Ordinary livestock path

`AnimalRenderer.OnTouch` assigns its owned `Animal` to `DolocAPI.CurrentAnimal`; `OnDisTouch` clears it. `ItemAnimalPackage.RefreshCellTip` and `TryCatchAnimal` only look at that `Animal` reference.

The catch transaction then removes the target through:

```text
current Animal
  -> Animal.homeRoom
  -> IAnimalHost.RemoveAnimal
  -> put the same Animal object into ItemAnimalPackage.animal
```

`DecorativeAnimal` cannot enter this chain: it is not an `Animal`, has no `homeRoom`, and is not registered with an `IAnimalHost`. Merely making the sack UI consider the decorative collider valid would leave the native catch transaction with no removable animal host.

## Spawn, Rebuild, And Persistence

`DecorativeAnimalManager.OnRender` iterates its serialized animal array and calls `GenAnimal` for every entry. Instances come from a `NashObjectPool<DecorativeAnimal>` based on `GAME_ENTITY_DECORATIVE_ANIMAL`. `OnUnRender` recycles the whole pool.

This is a scene-render rebuild mechanism, not ordinary livestock saving or a random wild-animal spawn table:

- leaving/unrendering the scene recycles the decorative instances;
- rendering the active conditional group creates/reuses all configured slots again;
- rainy/acid-rain weather or night makes each decorative animal walk out and hide, then it can return when the condition clears;
- no captured/removed slot list exists in `DecorativeAnimalManager`;
- no `AnimalData`, `AnimalManager`, `AnimalSystem`, or archive entry records these decorative instances.

Ordinary livestock instead survives through the room's `DM_animal`, `IAnimalHost.__AfterLoadAnimals`, and the serialized `Animal` held by a full `ItemAnimalPackage` while it is in the sack.

## Feasible Narrow Conversion

The recommended first implementation is internal to one narrowly scoped first-party mod. It is not a general DTMAPI public API and must fail closed outside the reviewed Kenenimuu scene groups.

1. Track a touched `DecorativeAnimal` together with its source manager/group, slot index, species, child state, and reviewed room identity. Do not write it into `DolocAPI.CurrentAnimal`.
2. When an empty livestock sack is used on an allowed decorative target, perform a dedicated transaction instead of entering the ordinary `homeRoom.RemoveAnimal` catch branch.
3. Construct a real native `Animal` from the decorative entry's reviewed `AnimalInfo` and current date, preserve the child state, and place it in the resulting full `ItemAnimalPackage`. The package already serializes its `Animal` field.
4. Persist a per-archive captured-source marker and hide/recycle that exact decorative slot. On later manager renders, suppress only marked slots.
5. Leave release to the native `ItemAnimalPackage.TryReleaseAnimal` route. It already enforces an animal building and capacity, asks for a name for a never-homed animal, then registers it through `IAnimalHost.AddAnimal`.
6. Make the transaction atomic: a failed sack split, full backpack, failed package placement, failed source-marker write, or conversion exception must keep the source animal and empty sack unchanged.

The per-archive marker owner and save/copy/delete lifecycle must be reviewed before implementation. A settings file keyed only by save-slot number is not sufficient because slots can be copied, replaced, or deleted.

## Rejected Shortcuts

- **Set a capturable/owner flag:** no such shared flag bridges the two runtime classes.
- **Assign the decorative animal to `DolocAPI.CurrentAnimal`:** the types differ, and native catch still requires `homeRoom` plus `IAnimalHost.RemoveAnimal`.
- **Spawn ordinary livestock directly in the city scene:** normal `IAnimalHost` capacity/home behavior is farm-animal-building oriented; a city scene is not a valid livestock home.
- **Only create a full sack and hide the current object:** it respawns on the next scene render and permits unlimited captures.
- **Make every `DecorativeAnimal` capturable:** other scene decorations can be story/environment assets. The reviewed request covers only the Kenenimuu hierarchy.
- **Expose a stable public API now:** source identity, per-save persistence, atomic inventory behavior, update compatibility, and runtime evidence are not yet proved.

## Risk And Validation Requirements

Risk is moderate rather than a one-line tweak. The native conversion and release objects already exist, but source identity and persistence are mod-owned.

Before calling an implementation complete, validate at least:

- each active progress variant and each configured species;
- stacked and single empty sacks, full backpack, interrupted interaction, and failed placement rollback;
- leave/re-enter room, day/night, rain/clear weather, title/load, restart, and save copy/delete behavior;
- captured source does not regenerate, uncaptured siblings still do, and the other conditional group cannot duplicate captured slots;
- full sack survives save/load and native barn release/naming/capacity behavior remains unchanged;
- released livestock participates in normal `AnimalManager`/`AnimalSystem` behavior and no scene or process residue remains.

## Evidence Checked

- `DecorativeAnimal.cs`
- `DecorativeAnimalManager.cs`
- `AnimalRenderer.cs`
- `ItemAnimalPackage.cs`
- `Animal.cs`
- `AnimalManager.cs`
- `AnimalSystem.cs`
- `IAnimalHost.cs`
- `Room.cs`
- `ConditionalVisible.cs`
- current build `globalgamemanagers` build-scene list
- current build `level16` serialized GameObject/component hierarchy and manager arrays
- current build `level83` room/interior separation
- `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
- `docs/api/public-api-matrix.md`

No game run or save mutation was performed. The linked wiki page was not used as code evidence because direct access returned HTTP 403; the character/request context is taken from the user's supplied description.
