# Round 3 Final Confidence

Status: recorded
Date: 2026-06-13
Input: Round 2 supplemental report
Output: final confidence scores and required downgrades

Round 3 reviewed the Round 2 supplemental conclusions. The verdict was that Round 2 answered the major Round 1 questions, but several parent reports still needed wording changes so future API rebuilds do not read a research path as a stable native API.

## 01 World Time Weather Refresh

Required downgrades:

- Split weather snapshot, current-weather force, and forecast/history patch.
- State that dungeon/custom independent weather state exists, but active caller ownership is not proven.
- Expand refresh lifecycle beyond daily/monthly to weekly/yearly, before/after time pass, enter/exit room, and no-render paths.
- Treat time advance and weather mutation as diagnostic/internal until game evidence exists.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Time snapshot query owner found | 92 |
| Global/effective room weather snapshot feasible | 84 |
| Dungeon/independent weather active ownership unproven | 78 |
| Current-weather force owner found, diagnostic only | 76 |
| Forecast/history patch split required | 82 |
| Controlled `PassTime`/`PassLongTime` owner found, high risk | 82 |
| No-render pass route exists, internal/diagnostic | 80 |
| Native refresh callbacks observable after proof | 74 |
| Full refresh ordering incomplete for stable API | 88 |

## 07 Maps Dungeons Scenes Resources

Required downgrades:

- Split transport by route: mark point, direct room, city/farm, and dungeon enter/quit/sub-room.
- Keep portal/gate interaction ownership separate from mark-point teleport.
- Split dungeon resource refresh and vegetation refresh.
- State that room/dungeon load-time database paths are not runtime creation APIs.
- Use route-specific diagnostic probes rather than a broad stable teleport concept.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Fixed room identity query owner found | 90 |
| Scene load/unload owner partial | 78 |
| Mark-point teleport owner found, diagnostic only | 82 |
| Direct room entry owner found, high risk | 78 |
| City/farm entry owners found | 80 |
| Dungeon enter/quit/sub-room owners split correctly | 82 |
| Portal/gate ownership separate from mark-point teleport | 86 |
| Resource refresh owner found, experimental | 78 |
| Vegetation refresh owner found, experimental | 74 |
| Runtime map/dungeon creation blocked | 90 |
| Geometry/boundary mutation blocked | 92 |

## 02 NPC Body Behavior

Required downgrades:

- Preserve the distinction between constrained experimental native research and stable public API blocked for complete new NPCs.
- Move store/trade out of NPC core responsibility.
- Treat schedule support as partial unless graph, mark points, scenes, and Yarn nodes are valid.
- Do not use document/table presence as proof of runtime NPC creation.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Existing NPC identity/state owner | 88 |
| NPC manager registration/save/load owner | 86 |
| Schedule/behavior ownership | 72 |
| Dialogue/liking/gift linkage | 78 |
| Movement/location/scene persistence | 84 |
| NPC-linked store interaction | 64 |
| Full new NPC stable API | 18 |
| Full new NPC experimental research path | 68 |

## 03 Animal Husbandry Behavior

Required downgrades:

- Separate existing animal instance creation from custom animal species/content.
- Keep custom animal support experimental only and stable API blocked.
- Split animal-owned product eligibility from tool/machine collection owners.
- Keep buy/sell/package and movement/pathing claims partial.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Existing animal native creation/release/catch | 86 |
| Animal persistence/room host ownership | 84 |
| Feeding/excretion/breeding lifecycle | 70 |
| Product eligibility on animal | 80 |
| Product collection via tools/machines | 82 |
| Buy/sell/package transaction ownership | 66 |
| Movement/location/pathing ownership | 64 |
| Custom animal stable API | 20 |
| Custom animal experimental native path | 66 |

## 04 Wild Birds Events Drops

Required downgrades:

- Birds must be described as `EnvObjectType.BIRD` environment events.
- Texture replacement remains weak unless tied to bird-specific official replacement evidence.
- Custom drops are season-table/content-time influence, not stable runtime per-bird behavior.
- New custom bird behavior is blocked because native dispatch is class/enum based.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Bird as environment event, not animal | 92 |
| Spawn pipeline ownership | 86 |
| Touch/flee/drop behavior owner | 90 |
| Seasonal bird drop-table ownership | 84 |
| Bird texture/appearance replacement | 58 |
| New bird with built-in behavior via config | 62 |
| New bird custom behavior | 12 |
| Runtime drop-table mutation stable API | 20 |

## 05 Drones Runtime Equipment

Required downgrades:

- Rename new drone creation to drone content/item definition.
- State that active runtime equip/summon is separate and unproven for custom drones.
- Add the active runtime singleton owner row.
- Downgrade movement to existing-mode query/command only; custom movement modes are blocked.
- Require content smoke for custom drone table load, item instantiation, equip/load round-trip, and single-active invariant.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Drone config table merge likely possible but undocumented | 65 |
| Drone item definition can likely produce `ItemDroneStructure` | 78 |
| Active equipped drone runtime is singleton-oriented | 96 |
| Multiple simultaneous native active drones blocked | 94 |
| Slot/component install/remove via native panel transaction | 80 |
| Custom drone visuals plausible but smoke-pending | 68 |
| Custom movement modes not proven | 55 |

## 06 Flying Motor Vehicle Types

Required downgrades:

- Add ride lifecycle owners.
- Clarify that motor key use repositions the native singleton, not a new vehicle.
- Keep motor state/summon diagnostic or experimental until lifecycle evidence exists.
- Scope appearance support to existing motor skin/anchors.
- State that new vehicle type is blocked because no native vehicle registry/save/key mapping was found.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| No native vehicle registry/table found | 94 |
| Existing flying motor singleton owner found | 93 |
| `ItemMotorKey.OnUse` summons/repositions existing motor | 90 |
| Ride lifecycle owner spans interact/agent/camera/body/drone/gates | 84 |
| Existing motor texture/skin replacement supported | 90 |
| Independent second motor/new vehicle is DTMAPI sidecar only | 92 |
| Wholly new vehicle type blocked | 95 |

## 08 Hats Accessories Equipment Slots

Required downgrades:

- Use read-only slot snapshot plus experimental native equip transaction, not a broad stable slot API.
- Mark direct `EquipPassiveItem2` as unsafe/internal.
- State that passive2 must not be exposed as an unconditional public slot.
- Keep extra slots beyond native fixed fields blocked.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Native fixed slots can be read as DTO | 95 |
| Generic passive equip respects unlock routing | 92 |
| Direct `EquipPassiveItem2` bypass risk | 90 |
| Extra stable equipment slots blocked | 96 |
| Hat render owner found but runtime adapter experimental | 84 |
| Decal/socket placement owner found but high risk | 82 |

## 09 Food Equipment Effects

Required downgrades:

- Split consume owner from effect application owner.
- Add `BuffManager` duplicate/timer/queue/save semantics.
- Replace extensible registry language with known-native read/observe plus bridge adapters.
- State that custom DTMAPI effect handlers would be sidecar/bridge-owned, not official native JSON behavior.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Existing eat flow through `IEatable`/`ItemFood` found | 94 |
| `DoEffects` can apply effects without owning consume | 90 |
| Known multi-effect data supported | 86 |
| Equipment food-effect addition path can reuse food effects | 90 |
| Buff duplicate/timer/queue/save behavior mapped for experimental known-buff API | 90 |
| `AgentEquipmentFunction*` lifecycle found but not plugin extension point | 80 |
| Brand-new effect behavior blocked for stable JSON-only API | 92 |

## 10 Item Stack Quantity Limits

Required downgrades:

- Add the safe wrapper list and transaction caveats.
- Warn that `TryPlaceInBackpack` may partially mutate before incomplete/false result.
- Keep inventory `buffer`/cursor state internal only.
- Block runtime stack-limit override and over-cap migration until a native policy is found.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| `ItemInfo.Overlay` is native stack-limit source | 96 |
| Native stack operations found | 92 |
| Native add/remove wrappers exist but need transaction policy | 90 |
| `TryPlaceInBackpack`/mail overflow may partially mutate | 84 |
| Inventory buffer/cursor state internal only | 90 |
| Runtime stack-limit override and over-cap migration blocked | 96 |

## 11 Original Follow Pet

Required downgrades:

- Remove any wording that implies a native pet system exists.
- Reject `AnimalWatchDog`, drone follow, motor follow points, and livestock room-transition work as sufficient pet owners.
- Keep stable pet API blocked and only allow a DTMAPI-owned experimental sidecar direction.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| No stable native ground-companion/pet owner found | 86 |
| `AnimalWatchDog` rejected as evidence | 95 |
| Drone follow targets are not pet behavior | 92 |
| Livestock/animal AI can inform pathing but not lifecycle | 82 |
| DTMAPI-owned sidecar pet plausible as experimental architecture | 68 |
| Stable public pet API blocked | 88 |

## 12 Held Ranged Weapons Projectiles

Required downgrades:

- Split projectile runtime from player-held weapon ownership.
- Mark projectile spawning/damage as partial experimental GameBridge candidate, not stable API.
- Mark player-held weapon, animation, attachments, and custom bullet injection as blocked.
- Reject drone gun UI/classes, `ItemFarmingGun`, weapon debugger assets, and monster bullet behaviors as handheld proof.

Final confidence:

| Claim | Confidence |
| --- | ---: |
| Projectile runtime owner is `BattleSystem` -> `BulletFactory` -> `BulletManager` | 92 |
| Damage/projectile behavior can be studied through battle/bullet owners | 82 |
| No stable native player-held ranged weapon owner found | 84 |
| Drone weapon input/UI terms are not handheld weapon support | 91 |
| `ItemFarmingGun` is farming-tool specific | 88 |
| Custom bullet/proto injection not stable without GameBridge evidence | 86 |
| Stable public handheld ranged weapon API blocked | 89 |
