# Round 1 Challenges

Status: recorded
Date: 2026-06-13
Input: initial reports `01` through `12`
Output: questions, overclaim review, and initial confidence targets

Round 1 used parallel review agents to challenge the initial native-owner reports. The goal was to ask whether each fuzzy domain had the correct native responsibility function or state holder, and whether the report wording was too stable for the evidence.

## 01 World Time Weather Refresh

Key challenges:

- `ArchiveDataHandle` was too broad as a lifecycle owner. The report needed to name weekly/yearly, before/after time-pass, enter/exit room, and no-render refresh callbacks.
- Weather query/control was over-collapsed. It needed separate rows for `SetWeather`, `PatchWeather`, snapshot queries, and history/forecast.
- `PassTime`/`PassLongTime` controlled routes needed to be split from `PassTimeNoControl` and no-render update paths.
- Dungeon weather and independent weather state were not proof of active independent weather ownership.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Date/time query owners found | 90 |
| Weather mutation owner found but high risk | 62 |
| Refresh lifecycle partially mapped | 70 |
| Stable public time/weather mutation API | 20 |

## 07 Maps Dungeons Scenes Resources

Key challenges:

- Teleport/map transition merged several different owners: mark-point transport, direct room entry, city/farm entry, dungeon entry, dungeon sub-room entry, and quit cleanup.
- `PortalInfo`/gate ownership had to be separated from `DolocAPI.DoTransport`.
- Resource refresh and vegetation refresh needed separate native owners.
- `DolocBundleManager.LoadRooms` and `LoadDungeons` are private/load-time database paths, not runtime map authoring APIs.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Fixed room identity query owner found | 88 |
| Teleport transition owner partial | 65 |
| Runtime map/dungeon creation blocked | 90 |
| Geometry/boundary mutation blocked | 90 |

## 02 NPC Body Behavior

Key challenges:

- `NpcManager` was missing from the primary owner list for registration, lookup, and save/load.
- NPC ID/document tables are not proof of full runtime NPC creation.
- Store/trade is store/dialogue/task mediated, not owned by the NPC core.
- Schedules require valid assets, mark points, scenes, NodeCanvas graph state, and Yarn/dialogue nodes.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Existing NPC identity/state | 82 |
| Schedule/behavior ownership | 65 |
| NPC store interaction | 55 |
| Full new NPC stable API | 15 |

## 03 Animal Husbandry Behavior

Key challenges:

- The report missed `AnimalManager.CreateAnimal`, `IAnimalHost.CreateAnimal/AddAnimal`, `ItemAnimalPackage.TryReleaseAnimal`, and catch/release paths.
- Existing animal creation needed to be separated from custom new species/content.
- Defecation referenced a debug method and needed lifecycle caution.
- Product generation needed separate animal eligibility and machine/tool collection owners.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Existing animal lifecycle owners | 76 |
| Product ownership partial | 72 |
| Buy/sell/package ownership | 58 |
| Custom animal stable API | 18 |

## 04 Wild Birds Events Drops

Key challenges:

- Birds are environment events, not the animal/livestock system.
- Spawn/refresh crosses `RoomSpawnInfo.EnvObjectSpawnEntry`, `IEnvObjectHost`, and `EnvObjectManager`.
- Drops are season-table driven and should be separated from per-bird custom behavior.
- New custom bird behavior is class/enum owned and should be blocked.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Bird as env-object event | 88 |
| Touch/flee/drop owner | 86 |
| New bird with custom behavior | 12 |
| Runtime drop-table mutation stable API | 20 |

## 05 Drones Runtime Equipment

Key challenges:

- New drone content/item definition needed to be separated from active runtime summon/equip.
- The native runtime appears singleton-oriented: one equipped drone item, one active controller, one renderer path.
- Drone slot/component install/remove should include UI transaction owners such as `DronePanelUiState`, `DroneWidget`, and `DroneItemSlot`.
- Drone table authoring was inferred from native content loading, not official Workshop documentation.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Drone config/item path likely exists | 65 |
| Active native drone singleton | 92 |
| Multiple simultaneous active native drones blocked | 92 |
| Custom movement modes found | 40 |

## 06 Flying Motor Vehicle Types

Key challenges:

- The report needed to say no native `TbVehicle`, `vehicle_tb`, `VehicleInfo`, registry, save slot, or key-to-vehicle mapping was found.
- `ItemMotorKey.OnUse` summons/repositions the existing motor singleton; it is not a new vehicle creator.
- Ride lifecycle needed `MotorInteractable`, agent ride state, camera/body, drone follow target, scanner, and gate owners.
- Existing motor appearance replacement should not imply new vehicle authoring.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Original motor singleton owner found | 88 |
| Existing motor skin replacement supported | 88 |
| Wholly new vehicle type blocked | 94 |
| Second independent native motor | 8 |

## 08 Hats Accessories Equipment Slots

Key challenges:

- The report needed to distinguish fixed native fields from a stable equip transaction API.
- `ArchiveOperationGlobal.EquipPassiveItem` respects passive2 unlock routing, while direct `EquipPassiveItem2` can bypass progression and should be internal/unsafe.
- Hat render lifecycle spans player body and motor driver renderers.
- Equipment decal/socket placement owners were under-listed.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Native fixed slot snapshot | 94 |
| Passive2 progression gate | 88 |
| Extra stable equipment slots blocked | 95 |
| Runtime render adapter stable | 35 |

## 09 Food Equipment Effects

Key challenges:

- `IEatable` is the eat/effect interface; `ItemFood` is one implementation.
- `DoEffects` can apply effects without being the consume owner.
- `BuffManager` duplicate, queue, timer, and save semantics needed to be included.
- `AgentEquipmentFunction*` discovery exists for native functions, but plugin subclass/proto extension was not stable.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Existing eat flow found | 90 |
| Known multi-effect data | 82 |
| Known buff API experimental | 75 |
| Brand-new data-only effect behavior | 10 |

## 10 Item Stack Quantity Limits

Key challenges:

- Safe native wrappers were under-listed.
- `TryPlaceInBackpack` can partially mutate before returning incomplete/false, especially with mail overflow.
- Cursor/buffer inventory state should stay internal.
- Runtime stack-limit override is not equivalent to content-defined `ItemInfo.Overlay`.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Native stack cap owner | 94 |
| Stack combine/cost owner | 88 |
| Stable public mutation policy | 60 |
| Runtime stack-limit override blocked | 94 |

## 11 Original Follow Pet

Key challenges:

- `AnimalWatchDog` exists but was not a usable pet owner.
- Drone follow targets, motor follow points, and body drone follower anchors are not ground-pet behavior.
- Livestock pathing can inform research but does not supply companion lifecycle.
- Any pet implementation must be DTMAPI-owned experimental sidecar until proven.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| No stable native pet owner found | 82 |
| Drone follow rejected as pet owner | 86 |
| DTMAPI-owned sidecar plausible | 60 |
| Stable public pet API blocked | 86 |

## 12 Held Ranged Weapons Projectiles

Key challenges:

- Projectile runtime should be separated from player-held ranged weapon ownership.
- `NormalFire`, gun reload UI, and drone weapon classes are drone-specific or debug/farming vocabulary, not handheld weapon proof.
- `ItemFarmingGun` is farming/tool-specific.
- Custom bullet/proto injection needs load-time table evidence and cleanup proof.

Initial confidence notes:

| Claim | Confidence |
| --- | ---: |
| Projectile runtime owner found | 88 |
| Damage owner partial | 78 |
| Stable native player-held ranged weapon owner | 16 |
| Stable custom bullet injection | 18 |
