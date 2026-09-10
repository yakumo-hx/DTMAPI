# New Scene, Travel, And Map Content Native-Owner Review

**Review ID:** `20260725-0001`

**Date:** 2026-07-25

**Status:** recorded — native content model identified; travel observation is a future bounded candidate; Mod map registration and Content Host implementation remain blocked

**Scope:** read-only comparison of `23762374_public_C416D4` and `24256979_test_7A1907`, the five newly built scenes, their entry/unlock/transition paths, current DTMAPI teleport/content surfaces, and future ownership. No public API, Runtime, Hook, package, game file, save, or release change.

## Source Request

The user identified five newly added native scenes:

- 旧城市废墟;
- 旧城农场;
- 湿地农场;
- 湿地木屋;
- 旧城废屋.

The requested question is whether the official implementation reveals a reusable “more maps” capability. The user proposed four entry models. This Review preserves those four models in the original order and distinguishes entry creation from destination registration and transition completion.

## Executive Verdict

The new build proves a reusable **native content model**, but not an already reusable Mod API.

- All five additions are separate Unity scenes at build indexes 85–89. They were shipped through the official Build Settings, Addressables/preload assets, and Luban configuration tables.
- 旧城市废墟 is one of those scenes, but it contains 85 native Dungeon Rooms. `map_id` groups content for map/UI semantics; it does not mean multiple entries share one Unity scene.
- Gate, station, and Yarn/NPC dialogue entry paths all converge on the same native destination chain: stable mark point -> room -> `DoTransport` / `EnterRoom` -> scene or dungeon-room transition.
- Dialogue is not a fourth transport owner. It is another caller of the same mark-point transport path.
- The strongest newly exposed reusable owner is `IndieFarmManager`: the two new farms are table-driven independent farms which join native farm update, weather, electricity, room query, and save state.
- DTMAPI can later consider read-only location discovery and explicit transition events separately from content creation. A general “register arbitrary map” promise is not supported by the current Loader.
- The least invasive first proof **for integration with an official-scene entrance** would be one externally hosted independent farm entered through an owner-scoped dialogue/mark-point route. The farm itself remains a substantial lifecycle domain: it still requires an authorized G7 Content Host, external scene loading, Room/Farm registration, weather/electricity/update behavior, save owner/orphan recovery, and conflict policy. G7 is currently blocked.

## Evidence Integrity And Limits

The comparison uses local-only reverse evidence:

- old baseline: `references/doloc-town/reverse/builds/23762374_public_C416D4`;
- new baseline: `references/doloc-town/reverse/builds/24256979_test_7A1907`;
- exact scene inventory: `full-baseline-inventory/built-scenes.json`;
- extracted Luban tables, Yarn, MissionGraph assets, scene YAML, and decompiled managed code under the new baseline.

The old build has 85 built scenes and the new build has 90. The exact appended scene set is:

| Build index | Native scene ID | Original asset path | Native content role |
| ---: | --- | --- | --- |
| 85 | `farm_湿地农场` | `Assets/Scenes/Farm/farm_湿地农场.unity` | independent farm |
| 86 | `city_湿地室内_小木屋` | `Assets/Scenes/Dungeons/室内场景/city_湿地室内_小木屋.unity` | one-room interior |
| 87 | `dungeon_旧城市废墟` | `Assets/Scenes/Dungeons/dungeon_旧城市废墟.unity` | one scene containing 85 Dungeon Rooms |
| 88 | `farm_旧城农场` | `Assets/Scenes/Farm/farm_旧城农场.unity` | independent farm |
| 89 | `city_旧城室内_废屋` | `Assets/Scenes/Dungeons/室内场景/city_旧城室内_废屋.unity` | one-room interior |

The supporting table deltas are consistent:

| Table | Old | New | Relevant delta |
| --- | ---: | ---: | --- |
| `room_tbscene` | 88 | 93 | exactly five new native scenes |
| `room_tbroom` | 125 | 214 | 85 old-city rooms, two farms, two interiors |
| `room_tbmaproom` | 80 | 165 | exactly 85 old-city map rooms |
| `room_tbstation` | 8 | 13 | five new travel stations |

AssetRipper scene YAML, reconstructed component references, and decompiled code are research evidence, not author-original source. A later real-build smoke must verify component fidelity and frame ordering before any implementation claim.

## Do Not Collapse Four Different Responsibilities

A future map system must model four separate responsibilities:

| Responsibility | Native evidence | What success means |
| --- | --- | --- |
| Content registration | Build Settings scene, RoomSO/DungeonSO/Farm state, Luban rows, preload cache | the destination exists and native owners can resolve it |
| Entry/unlock | Gate, Station, quest reward, wall/decorator, Yarn command | the player may discover or request the destination |
| Transition | `DoTransport`, `EnterRoom`, `TransitScene`, `DungeonTransitionState` | the native request has been accepted and work has begun |
| Completion/failure | scene loaded, Room entered, NPC/fade/state cleanup, old scene unloaded | the player has actually settled, or the operation failed |

A visible door or successful `bool` does not prove all four.

## The Four User Models

### 1. “在现在地图上，加一个口子，通向同一张地图的新区域”

**Evidence.** None of the five newly built objects is appended into the old main Unity scene. All five are separate scenes. However, 旧城市废墟 demonstrates the related native pattern: one `dungeon_旧城市废墟` scene contains 85 Dungeon Rooms. Same-scene room movement follows:

```text
RoomHandle.OnTriggerEnter2D
  -> DolocAPI.TryEnterDungeonSubRoom
  -> DungeonTransitionState
  -> DungeonRoom.OnEnterRoom
```

`Dungeon._RenderNearRooms` uses room-neighbour data to retain/render the current and adjacent rooms without loading another Unity scene.

**Analysis.** “Add a region” is not a coordinate-only operation. It needs scene GameObjects, trigger/collision geometry, RoomHandle identity, RoomSO, geometry/tile/material maps, the DungeonSO room collection, and neighbour relationships. The current game exposes no reviewed runtime append operation for this graph. Injecting a new region into an official scene also creates patch-order, owner cleanup, collision, map bounds, and official-update merge problems.

**Disposition.** Highest risk and last candidate. Do not make this the first “more maps API”, and do not describe a scene-object patch as stable map registration.

### 2. “通过传送点（车站、船、独立传送点缆车）通向一张新地图、新区域”

**Evidence.** The five new stations point at native mark points:

| Station | Mark point destination |
| --- | --- |
| 公车站-睡莲地 | `farm_湿地农场` |
| 公车站-驻守地 | `dungeon_旧城市废墟.驻扎营地-0` |
| 公车站-住宅区 | an old-city residential Room |
| 公车站-商业区 | an old-city commercial Room |
| 公车站-空闲区 | `farm_旧城农场` |

The shared transition route is:

```text
Station / BusStation / Teleport UI / other caller
  -> mark point ID
  -> DolocAPI.DoTransport
  -> DolocAPI.EnterRoom
  -> GameStateManager.TransitScene
  -> GameStateSceneTransition
  -> Unity SceneManager
  -> Room.OnEnterRoom
```

The first old-city story transport and later station travel both land on mark points in the same registered destination model. The wetland and old-city farms also have station routes.

**Analysis.** This is the most reusable **travel** seam once the target is registered. It does not register the scene. A generic API must not expose Unity build index as stable identity; it should resolve an owner-scoped destination ID to a registered Room/mark point and report transition phases. Boat/cable-car animations, prices, tickets, quest locks, and station UI are route policy, not the core scene loader.

**Disposition.** Candidate for later read-only discovery, route-specific requests, and transition events. Not evidence for arbitrary coordinate teleport, access-rule bypass, or external scene loading.

### 3. “一个门、通向新的自定义室内场景；这个门入口可以做到已有地图的元素中”

**Evidence.** The new build contains two complete bidirectional examples:

```text
湿地农场-木屋入口
  farm_湿地农场
    <-> 湿地木屋-出口
        city_湿地室内_小木屋

旧城农场-废屋入口
  farm_旧城农场
    <-> 旧城废屋-出口
        city_旧城室内_废屋
```

Both pairs use interactable Gate objects, `PortalInfo`, source/target mark points, and separate one-room scenes. The new wetland and old-city farm entrances similarly use paired portals, but are automatic boundary gates rather than interactable doors.

**Analysis.** This model has two independently governed problems:

1. register and load the target interior scene/Room;
2. inject or place the source Gate, trigger, collision, visual, and access rule.

Portal JSON alone does not materialize a door in an already built scene. A future Content Host could own target registration without promising that every Mod may mutate official scenes. Source entrance injection needs its own owner-scoped placement, conflict, restoration, and official-update compatibility design.

**Disposition.** A new interior is a plausible second content-host proof after independent farm hosting. A door inside an external Mod-owned scene is easier than injecting a door into an official scene. Existing-scene injection remains separately blocked.

### 4. “通过一个（与 NPC）对话触发传送，也就是传送点不需要嵌套进现有地图里”

**Evidence.** The old-city first-entry Yarn executes `go 旧城-初始入口`. The native `[Command("go")]` handler calls `DolocAPI.DoTransport` and waits for its callback. Other official Yarn uses the same `go` command for a farm fallback. Quest and faction state decide when these routes become available.

**Analysis.** This is a useful low-intrusion **entry trigger**, but not a fourth map transport implementation. It avoids injecting physical scene geometry into an official map. It still needs a pre-registered destination, valid mark point, truthful failure/settlement signal, dialogue ownership, and quest/access policy. It therefore makes an excellent entry method for a first Content Host experiment while leaving the actual map loader problem unchanged.

**Disposition.** Prefer this over existing-scene door injection for an initial hosted-map pilot. Do not invent an “NPC map API”; dialogue should call the same owner-scoped travel service as other entry routes.

## Actual Five-Scene Entry And Unlock Findings

| Destination | Initial or primary entry | Later/physical entry | Important unlock fact |
| --- | --- | --- | --- |
| 旧城市废墟 | story Yarn `go 旧城-初始入口` | three bus destinations within the same scene | `doloc_bus_upgrade` precedes the story route; dialogue unlocks the stationed return route |
| 湿地农场 | physical boundary Gate from the existing wetland | hovercraft/station mark point | both routes exist; faction mission reward unlocks 公车站-睡莲地, but first-use order is not game-smoke-proven |
| 湿地木屋 | farm Gate | same bidirectional Gate pair | no separate transport owner found |
| 旧城农场 | bus/station mark point is the strong first-entry path | old-city physical boundary Gate | first farm arrival writes `RUINEDCITY_FARM.Open`, removing the nearby blocking wall |
| 旧城废屋 | farm Gate | same bidirectional Gate pair | inherits old-city farm access |

This is why unlock, entry, and transition must not be treated as one API call. In particular, the old-city farm can have a valid physical portal row while a separate event-controlled wall intentionally prevents its initial use.

## The New Independent-Farm Native Owner

The new build adds:

- `IndieFarmManager`;
- `IndieFarmInfo`;
- `player_tbindiefarm`;
- independent-farm state in `FarmArchiveData`;
- all-farm query support in `ArchiveDataHandle`, `ArchiveOperationFarm`, and `DolocAPI.QueryRoom`.

`player_tbindiefarm` currently declares:

| Farm ID | Declared initial mark |
| --- | --- |
| `farm_湿地农场` | `湿地农场-出口` |
| `farm_旧城农场` | `车站-空闲区` |

`IndieFarmManager` is table-driven rather than two product-specific implementations. It:

- creates missing independent `TemplateRoomOutdoor` instances from registered native Farm Room prototypes;
- joins update, no-render update, weather, and archive lifecycle;
- shares the main farm electricity system;
- serializes a dictionary of independent farm state;
- removes state for farm IDs no longer present in the table.

The last behavior is safe for official immutable content but unsafe as an unmodified Mod contract: disabling or removing a map package could delete serialized gameplay state. A Mod host therefore needs explicit owner/orphan retention and reactivation semantics. Ordinary gameplay mutation must still follow native save-commit rules; “write immediately on disable” is not an acceptable substitute.

`FarmArchiveData.currentRoomId` also shows that a registered independent-farm Room can be restored after load. This proves a native save route for registered content, not a Mod registration API.

No direct consumer of `IndieFarmInfo.InitMarkPoint` was proven beyond configuration resolution. This Review does not claim that field itself initiates travel.

## Native Registration Boundary

The official route works because the game build owns all required inputs:

```text
Unity Build Settings scene
  + Addressables/preload RoomSO and DungeonSO
  + Luban Scene/Room/MapRoom/MarkPoint/Portal/Station/Farm rows
  + scene GameObjects and native components
  -> registered native destination
```

At startup, `DolocBundleManager` loads Luban configs, Rooms, City Rooms, and Dungeons from the official preload/cache path. `SceneManagement` derives its scene directory from `SceneManager.sceneCountInBuildSettings` and `SceneUtility.GetScenePathByBuildIndex`. Native `DolocTown.SceneManager` freezes its scene/name maps from that directory during construction; its string load, handle query, and unload paths only resolve entries already in that Build Settings map. No append/register seam was found. A future Host must therefore own an external scene resolver plus load/unload, handle, cache, and cleanup lifecycle rather than merely adding table rows.

An external Mod scene is therefore absent from at least three current authorities:

- Unity Build Settings scene resolution;
- official preload cache / RoomSO / DungeonSO registration;
- coordinated table and scene-object registration timing.

An AssetBundle by itself, or JSON mark-point rows by themselves, cannot close those gaps.

## Native Transition Owner And Completion Gap

The central native owners are:

- request/lookup: `DolocAPI.DoTransport`, `EnterRoom`, `EnterFarm`, `EnterCity`, `EnterDungeon`;
- same-scene Dungeon Room change: `TryEnterDungeonSubRoom`, `DungeonTransitionState`;
- cross-scene state: `GameStateManager`, `GameStateSceneTransition`;
- scene load directory: `SceneManagement` / Unity `SceneManager`;
- target lifecycle: `Room.OnEnterRoom`.

Current native/result semantics are insufficient for a stable public completion promise:

- `DoTransport` / `EnterRoom` `bool` means lookup/request acceptance, not arrival;
- the callback occurs after scene load and `Room.OnEnterRoom`, but before every NPC/fade/state-pop/old-scene cleanup guarantee;
- missing mark point/Room/scene can reject immediately;
- asynchronous load failure is logged but has no complete public failure callback;
- Yarn `go` can wait indefinitely if its callback never runs;
- some transport UI policy spends a ticket or money before final success, with no reviewed rollback.

A future DTO should distinguish at least:

```text
Rejected -> Requested -> SceneLoaded -> RoomEntered -> Settled
                                         \-> Failed
```

Exact phase names remain proposed. They must be tied to native lifecycle evidence, not inferred by polling one room snapshot after a request.

## Current DTMAPI Surface Is Not The Map API

`ITeleportDebugApi` currently:

- enumerates a whitelist built from native farm/station/mark-point data;
- invokes the reflected native `DoTransport` route;
- reports `Before` and `AfterRequest` snapshots;
- remains a Diagnostic/debug surface.

It does not:

- register scenes, Rooms, Dungeons, Farms, mark points, portals, or doors;
- expose a stable transition-completed event;
- own quest/access policy;
- make arbitrary coordinates safe;
- prove external AssetBundle scene loading.

The QA fixture polls current room/position after a request; polling is not a native completion contract. `ITeleportDebugApi` must not be renamed or promoted to claim travel/map completion.

At the generic identity/manifest layer, current `ContentPack` is a non-code managed owner/registry boundary. Existing fixed domain scanners such as CustomAnimals and AudioReplacement do not constitute a general `ContentPackFor` host relationship. `IContentQueryHelper` is read-only, and Batch 6 keeps the general optional G7 Content Host blocked.

## Ownership Decision

| Capability | Proposed physical owner | Current status |
| --- | --- | --- |
| current location and registered destination query | Core DTO contract + SharedNative adapter only after real consumers prove it | proposed, read-only first |
| transition lifecycle observation | native transition owner adapter, with owner-scoped subscription cleanup | proposed; needs runtime phase proof |
| travel to a registered destination | route-specific GameBridge adapter, access policy explicit | Diagnostic today; gameplay API blocked |
| external scene/Room/Farm loading | optional Map Content Host under future G7 authority | blocked |
| one Mod's map rules, entry story, economy, and gameplay | that product/content owner | ProductNative/ContentOwner, not Platform |
| physical entrance in an external Mod-owned scene | Map Content Host / content owner | future candidate |
| injection into an official scene | separately reviewed ProductNative/content patch path | blocked/high risk |
| NPC dialogue trigger | dialogue/content owner calling registered travel | not a separate platform API |

GameBridge may own only genuinely SharedNative adaptation. One map product, future reuse, internal visibility, provider/facade shape, or Harmony use does not prove SharedNative ownership. A single hosted content type may remain host-internal until at least two independent real consumers share an exact native owner/invariant.

## Recommended Sequence

1. **Freeze read-only facts.** Define owner-neutral DTO concepts for current scene/Room/map group and registered destinations. Do not expose Unity, decompiled, Harmony, or build-index types.
2. **Prove transition phases.** Add a diagnostic-only native trace in a later authorized task and verify cross-scene, same-scene Dungeon Room, title, failure, and re-entry ordering.
3. **Design G7, without implementation authority.** Specify external scene bundle loading, Room/Farm registration, owner-scoped IDs, dependency/conflict rules, and unloading.
4. **First hosted proof: independent farm.** Use one Mod-owned scene and a dialogue/mark-point entry so no official scene geometry is injected. Prove save/no-save, owner disable/orphan retention, restart, weather/electricity/update lifecycle, and memory cleanup.
5. **Second hosted proof: Mod-owned interior and Mod-owned door.** Keep both ends inside host-owned content before considering an official-scene entrance.
6. **Existing-scene injection and same-scene expansion last.** Require a separate compatibility and restoration contract.

This is an engineering order, not current authority to implement G7 or admit an eleventh product.

## Minimum Future Acceptance Matrix

Any future hosted map proof must cover:

- cold startup and exact deterministic registration;
- duplicate scene/Room/mark-point/portal IDs across two owners;
- missing dependency and disabled owner;
- enter, exit, re-enter, title return, save reload, and process restart;
- mutate then return to title without native save restores prior state;
- normal native save retains the new state;
- disable/uninstall preserves typed owner/orphan state without making it active;
- re-enable restores exactly once;
- failed scene load produces a terminal failure and does not strand fade/input/UI cost state;
- no leaked scene roots, event subscriptions, callbacks, AssetBundle handles, or retained owner state;
- bounded long-run allocation/GC behavior across repeated transitions.

## Disposition

**GO** to retain this native-owner map as research input for a future travel/query design.

**GO** to treat independent farm as the most promising first map-content domain after G7 is separately authorized.

**NO-GO** to call `ITeleportDebugApi` a stable travel or map API.

**NO-GO** to infer external map loading from official Build Settings/Addressables content.

**NO-GO** to inject doors or append Dungeon Rooms into official scenes as the first implementation.

**NO-GO** to implement G7, admit an eleventh product, or change the public API matrix from this Review.

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Validation

- Compared the two frozen local baselines and exact built-scene inventories.
- Cross-checked Luban Scene/Room/MapRoom/MarkPoint/Portal/Station/Farm rows with scene YAML, Yarn, MissionGraph assets, and decompiled native owners.
- Inspected current DTMAPI `ITeleportDebugApi`, event surface, ContentPack classification, content helper, public API matrix, Batch 6 identity contract, and prior map-domain reviews.
- No game launch, runtime lock, save mutation, Runtime build, Hook change, or public API matrix change was required.
