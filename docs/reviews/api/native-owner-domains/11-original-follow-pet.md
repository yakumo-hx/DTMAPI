# 11 - Original Follow Pet

Status: Blocked for stable native API; Proposed/Experimental for DTMAPI-owned sidecar entity
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Original game content that does not currently exist: add a pet that follows the player, has animation and behavior, stays on the ground, and needs feeding, such as a dog.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Partial for small animals | `013_*`, `018_*`, `044_*` | Existing small animal assets only |
| Base content mod | Partial for animal/item IDs | small animal ID docs | No follower pet behavior |
| Advanced content mod | Not confirmed | Official docs checked | No custom pet AI pipeline |
| Runtime behavior mutation | Not public | No official follower-pet API found | Would be DTMAPI-owned experimental runtime |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Ambient small animal behavior | `DecorativeAnimal`, `DecorativeAnimalManager.OnRender`, `DecorativeAnimalManager.GenAnimal`, `DecorativeAnimal.Setup`, nested `HideState`, `AppearState`, `IdleState`, `WanderState`, `EatState` | `DecorativeAnimal*.cs`; official small animal docs | Decorative animal manager and state machine | Ambient spawn/wander/eat-at-fixed-points | High private state/prefab/pool risk | `ICustomPetDefinition` registry-only art/metadata | Not a follower owner |
| Livestock movement/feeding as possible base | `Animal`, `AnimalManager`, `AnimalController`, `AnimalAI`, `Animal.StartMove`, `Animal.Move`, `Animal.CallToRoom`, `Animal.Eat`, `AnimalRenderer.MoveTo`, `AnimalRenderer.Eat` | `Animal*.cs`; `Assets_Content.md`; `Action_Interaction.md` | Livestock animal system | Room/home ownership, pathing, renderer movement, feeding/metabolism | Medium/high save/home coupling | `ICustomAnimalDefinition` registry; runtime creation blocked | Not follower semantics |
| Feeding source | `IFeeder`, `Feeder`, `PlantBasin`, `ForageGrass`, `DungeonResourceWeeds`, `AnimalWork_SearchFoodToEat`, `AnimalAI.FindFoodInCurrentEnv`, `AnimalEat` | feeder/animal task classes | Native animal AI | Environment/feeder-driven eating | Medium/high AI coupling | `IPetFeedingSource` only after pet owner exists | Partial reference only |
| Pet animation reference | `AnimalInfo`, `AnimalRenderer`, `DecorativeAnimal.Setup` | animal config/renderer classes | Existing animal/decorative renderers | Existing sprite/animation assets | High for original runtime animation | `CustomPetAnimationSet` registry-only | Blocked |
| Ground-only player follower | No stable owner found | searches for `pet`, `follower`, `Follow`, animal/decorative/NPC/drone systems | None | Native game has no proven dog-like companion path | Blocking | DTMAPI-owned experimental runtime entity | Blocked |

## Rejected Native Owners

| Candidate | Reason rejected |
| --- | --- |
| `AnimalWatchDog` | Present in native symbols but not a usable player-following pet lifecycle owner. |
| `DroneRenderer.SetFollowTarget`, `DolocAPI.DroneFollowTarget`, `BodyController.DroneFollower` | Drone anchors/follow targets, not ground-navigation companion behavior. |
| `MotorController.DroneFollowPoint`, motor auto-follow coroutine paths | Motor/drone anchor behavior, not a pet system. |
| Livestock room transition and pathing such as `AnimalPathFinder*`, `AnimalWork_EnterAnotherRoom` | Useful research references, but they assume livestock home/room/AI semantics and do not provide pet lifecycle. |

## API Translation Notes

- The correct future route is not a stable native adapter. It is a DTMAPI-owned experimental runtime entity that borrows room geometry/pathing facts carefully.
- Public contracts can start as registry-only pet definitions: id, display name, sprite/animation set, feeding needs, ground-only movement policy.
- Runtime spawn/follow/save/load/room-transition/disable cleanup must remain blocked until implemented and game-proven.
- Future pet prototype requirements: save-slot persistence, current-room reparent/despawn, room transition handling, ground navigation, feeding state, title-return cleanup, and mod-disable cleanup.
- Round 3 confidence: no stable native ground-companion owner 86; `AnimalWatchDog` rejected 95; drone follow rejected 92; livestock AI as reference only 82; DTMAPI-owned sidecar plausible 68; stable public pet API blocked 88.

## Blockers And Follow-Up

- No stable native player-following pet owner was found.
- Existing decorative animals do not follow the player.
- Existing livestock require home room, host, metabolism, produce, and AI task semantics that do not match a companion pet.
- Feeding and animation need DTMAPI-owned behavior or a proven official content loader path.

## Evidence Checked

Maps: `Action_Interaction.md`, `Assets_Content.md`, `Resource_Gathering.md`, `NPC_Dialogue.md`.
Classes/symbols: `DecorativeAnimal`, `DecorativeAnimalManager`, `Animal`, `AnimalManager`, `AnimalController`, `AnimalAI`, `IFeeder`, `Feeder`, `AnimalRenderer`, `AnimalInfo`.
