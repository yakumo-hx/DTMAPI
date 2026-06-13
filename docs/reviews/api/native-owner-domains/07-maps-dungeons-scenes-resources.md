# 07 - Maps Dungeons Scenes Resources

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Create new maps, map teleport, map boundaries, map generation, map resource refresh, split between dungeons and fixed scenes.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Partial for map/resource textures | Beauty docs | Not a geometry API |
| Base content mod | Yes for resources/vegetation/platforms | `016_*`, `032_*`, `036_*`, `050_*`, `028_*` | Content definitions only |
| Advanced content mod | Partial for platform/resource examples | `025_*`, `038_*` | No arbitrary room/dungeon creation |
| Runtime behavior mutation | Not public | No official teleport/boundary/generation API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed scene/room identity | `DolocAPI.get_CurrentRoom`, `GetRoom`, `QueryRoom`, `QuerySceneInfo`, `QueryTemplateRoom`; `ArchiveDataHandle.get_currentRoom`, `get_currentSceneName`, `get_currentRoomName`; `FarmArchiveData.currentRoom` | `GameLoop_Scene` and `Save_Load` maps; `DolocAPI.cs`; `ArchiveDataHandle.cs` | `ArchiveDataHandle`, `FarmArchiveData`, current `Room` | Current room and scene identity | Medium if raw `Room` leaks | `IRoomInfo` DTO | Found for query |
| Scene load/unload | `DolocTown.SceneManager.LoadSceneAsync`, `UnloadSceneAsync`, `IsSceneLoaded`, `GetSceneHandle`, `GetAllSceneHandles`; `Room.LoadSceneHandle`, `LoadMaterialMaps`, `OnEnterRoom`, `OnExitRoom` | `GameLoop_Scene` maps; `SceneManager.cs`; `Room.cs` | `SceneManager` and `Room` | Unity scene lifecycle and room enter/exit effects | High Unity handle risk | Room transition events/query-only scene status | Partial |
| Dungeon vs fixed scene | `Dungeon.rooms`, `currentRenderedRooms`, `weather`, `weatherPatch`; `Dungeon.GetRoom`, `GetRoomFromPosition`, `_RenderNearRooms`, `UnRenderAllRooms`, `ResetAllRoomStopTime`; `DungeonArchiveData.currentDungeon`, `dungeonManager`, `resourceManager` | `GameLoop_Scene` maps; `Dungeon.cs`; `DungeonArchiveData.cs` | `Dungeon`, `DungeonArchiveData` | Dungeon room graph, render set, resource/weather state | High internals risk | `IDungeonInfo` query and dungeon lifecycle observation | Partial |
| Mark-point teleport | `DolocAPI.DoTransport`; `Config.Room.MarkPointInfo`; `TbMarkPoint` | `Action_Interaction.md`; `DolocAPI.cs`; config room classes | `DolocAPI` plus mark-point config | Mark-point destination resolution and transition execution | High lifecycle/fade/input risk | Diagnostic mark-point teleport probe only | Partial |
| Direct room enter | `DolocAPI.EnterRoom`, `QuitCurrentRoom`; `Room.OnEnterRoom`, `Room.OnExitRoom` | `GameLoop_Scene.md`; `DolocAPI.cs`; `Room.cs` | `DolocAPI`, current `Room` | Direct room transition and enter/exit effects | High lifecycle/fade/input risk | Diagnostic direct-room enter probe only | Partial |
| City/farm enter | `DolocAPI.EnterCity`, `EnterFarm`; `FarmArchiveData.currentRoom` | `GameLoop_Scene.md`; `Save_Load.md`; `DolocAPI.cs` | `DolocAPI`, farm/city archive state | City/farm route transition | High lifecycle/save risk | Route-specific diagnostic probe | Partial |
| Dungeon enter/quit/sub-room | `DolocAPI.EnterDungeon`, `_TryEnterDungeon`, `_TryQuitDungeon`, `TryEnterDungeonSubRoom`; `Dungeon._ExitCurrentRoom` | `GameLoop_Scene.md`; `DolocAPI.cs`; `Dungeon.cs` | `DolocAPI`, `Dungeon`, `DungeonArchiveData` | Dungeon entry, sub-room transition, quit cleanup | High dungeon/render/resource risk | Route-specific dungeon transition probe | Partial |
| Portal/gate interaction ownership | `Config.Room.PortalInfo`, `PortalInfo.Resolve`, `PostResolve`; `AnimatedGate`, `SimpleGate`, `ScannerGate`, `ManagerGate`, `BuildingGate`, `BuildingLinkGate` | `Action_Interaction.md`; gate and config room classes | Portal config and gate interactables | Portal/gate interaction policy; may call mark-point transport but is not the teleport owner | Medium/high interaction risk | Portal/gate read-only metadata first | Partial |
| Map UI/coordinate conversion | `DolocAPI.OpenMap`, `CanOpenMap`, `GetMapPosByWorldPosition`; `SceneInfo.get_MapId`; `MapTypeInfo`, `MapAreaInfo`, `MapRoomInfo` | `GameLoop_Scene` and `Assets_Content` maps | Scene/map config tables | Map UI and world/map coordinate conversion | UI-coupled | `IMapQueryApi` | Partial |
| Boundaries/geometry | `Room.get_RoomGridSize`, `get_RoomGridPos`, `get_RoomPosition`, `get_RoomSize`, `get_GroundPositions`, `materialMap*`; `RoomHandle.OnScenePosChanged`, `OnSceneSizeChanged`, `SnapAirWallToRoom`, `SetRoomPositionBySprite`, `SetRoomSizeBySprite`, `CreateRoomGeometrySO`; `DungeonHandle` tilemap fields | `GameLoop_Scene` maps; `Room.cs`; `RoomHandle.cs`; `DungeonHandle.cs` | Room handles, tilemaps, material maps | Bounds, geometry, control maps | Very high Unity/tilemap risk | Bounds DTO only | Blocked for mutation |
| Room/data generation | `Room.InitRoom`, `ReGenRoomDatas`; `DungeonRoom.ReGenRoomDatas`; `DolocBundleManager.LoadRooms`, `LoadCityRooms`, `LoadDungeons`; fields `rooms`, `cityRooms`, `dungeons` | `GameLoop_Scene` and `Assets_Content` maps; `DolocBundleManager.cs` | Bundle manager and room regeneration | Load-time/private database paths and runtime data regeneration; not runtime authoring APIs | High destructive/private loader risk | Registry/content query only | Blocked for runtime creation |
| Dungeon resource refresh | `DolocAPI.RefreshResourceByType`, `ReplaceResource`, `Command_RefreshResource*`, `Command_GenerateDungeonResource`; `IDungeonResourceHost`; `DungeonResourceManager.CreateResource`, `RemoveResource`, `TryGetRandomResource`, `RefreshCounterInterval`; `ResourceSpawnInfo.SpawnResources` | `Resource_Gathering.md`; manager/spawn classes | Dungeon/resource managers | Dungeon resource spawn/refresh | High room/save/multi-mod risk | Experimental resource refresh with owner token | Partial |
| Vegetation refresh | `IVegetationHost`; `VegetationManager.CreateVegetation`; `VegetationSpawnInfo.TrySpawnSingle`; `RefreshWorldVegetation` | `Resource_Gathering.md`; vegetation manager/spawn classes | Vegetation host and vegetation manager | World vegetation spawn/refresh | High room/save/multi-mod risk | Experimental vegetation refresh with owner token | Partial |

## API Translation Notes

- Separate query APIs for room, dungeon, map ids, portals, mark points, and bounds from runtime mutation APIs.
- Runtime map creation, boundary editing, and dungeon generation must be blocked until save/load, scene loading, tilemap, and bundle ownership are proven.
- Teleport must stay route-specific and diagnostic with whitelisted destinations before any stable author API. Mark-point, direct room, city/farm, and dungeon routes need separate lifecycle proof.
- Round 3 confidence: fixed room identity query 90; mark-point teleport diagnostic 82; direct room entry high risk 78; dungeon enter/quit/sub-room split 82; resource refresh experimental 78; vegetation refresh experimental 74; runtime map creation blocked 90; geometry mutation blocked 92.

## Blockers And Follow-Up

- No official arbitrary map/dungeon creation doc was found.
- Boundary edits require Unity scene/tilemap/material map ownership and rollback.
- Resource refresh needs room lifecycle and multi-mod conflict policy.

## Evidence Checked

Maps: `GameLoop_Scene.md`, `Save_Load.md`, `Assets_Content.md`, `Resource_Gathering.md`, `Action_Interaction.md`.
Classes/symbols: `DolocAPI`, `DolocBundleManager`, `Room`, `Dungeon`, `RoomHandle`, `SceneManager`, `DungeonResourceManager`, `VegetationManager`, `ResourceSpawnInfo`, `VegetationSpawnInfo`, `PortalInfo`, `MarkPointInfo`.
