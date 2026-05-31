# DolocTown Game Startup / Scene Lifecycle API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Locate safe runtime bootstrap points.
- Map scene, room, and world transitions.
- Identify game-loop callbacks suitable for SMAPI events.

## Decompiled scope

- Matched types: 253
- Matched methods: 3006
- Matched fields: 1345
- Matched properties: 799
- Matched events: 0
- Matched call edges: 13502
- Matched strings: 963
- Raw indexes: `maps/index/GameLoop_Scene-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Room | class | Confirmed | visibility=public; methods=136; fields=36 |
| DolocTown.RoomHandle | class | Confirmed | visibility=public; methods=57; fields=38 |
| DolocTown.DungeonResource | class | Confirmed | visibility=public; methods=68; fields=11 |
| DolocTown.Config.Room.PortalInfo | class | Confirmed | visibility=public; methods=42; fields=17 |
| DolocTown.RoomGizmos | class | Confirmed | visibility=public; methods=16; fields=42 |
| DolocTown.Config.Room.MapAreaInfo | class | Confirmed | visibility=public; methods=39; fields=17 |
| DolocTown.DungeonResourceRenderer | class | Confirmed | visibility=public; methods=43; fields=8 |
| DolocTown.GameData.RoomGeometry | struct | Confirmed | visibility=public; methods=35; fields=16 |
| DolocTownGameStateBase | class | Confirmed | visibility=public; methods=38; fields=10 |
| GameManager | class | Confirmed | visibility=public; methods=29; fields=18 |
| DolocTown.Config.Room.MapTypeInfo | class | Confirmed | visibility=public; methods=28; fields=12 |
| DolocTown.Dungeon | class | Confirmed | visibility=public; methods=32; fields=8 |
| DolocTown.Config.Room.SceneInfo | class | Confirmed | visibility=public; methods=27; fields=12 |
| GameStateMachine | class | Confirmed | visibility=public; methods=28; fields=9 |
| DolocTown.IDungeonResourceHost | interface | Confirmed | visibility=public; methods=32; fields=0 |
| IDolocGameState | interface | Confirmed | visibility=public; methods=32; fields=0 |
| DolocTown.UI.OperationTipInScene | class | Confirmed | visibility=public; methods=17; fields=13 |
| DolocTown.Config.Room.RoomEffectInfo | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.UI.SceneBoxGroup | class | Confirmed | visibility=public; methods=17; fields=12 |
| DolocTown.Config.Room.StationInfo | class | Confirmed | visibility=public; methods=20; fields=8 |
| DolocTown.DungeonResourceWeeds | class | Confirmed | visibility=public; methods=25; fields=3 |
| DolocTown.Config.Room.MapRoomInfo | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.Config.Room.RoomConstructInfo | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.Config.Room.RoomInfo | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.SceneManager | class | Confirmed | visibility=public; methods=22; fields=3 |
| DolocTown.Config.Room.LockableObjectInfo | class | Confirmed | visibility=public; methods=17; fields=7 |
| DolocTown.TemplateRoomInHouse | class | Confirmed | visibility=public; methods=22; fields=2 |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.GameData.RoomSO | class | Confirmed | visibility=public; methods=7; fields=16 |
| DolocTown.DungeonResourceManager | class | Confirmed | visibility=public; methods=18; fields=4 |
| DolocTown.GameData.RoomGeometrySO | struct | Confirmed | visibility=public; methods=10; fields=12 |
| DolocTown.ISceneHandle | interface | Confirmed | visibility=public; methods=22; fields=0 |
| DolocTown.NormalGameState | class | Confirmed | visibility=public; methods=19; fields=3 |
| GameStateManager | class | Confirmed | visibility=public; methods=15; fields=7 |
| DolocTown.CutSceneState | class | Confirmed | visibility=public; methods=14; fields=7 |
| DolocTown.DungeonResourceTree | class | Confirmed | visibility=public; methods=18; fields=3 |
| DolocTown.Config.Room.DialogueObjectInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.InwalkableAreaInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.MapRoomTypeInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.RoomSpawnInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.RoomTimeRange | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.RoomScanner | class | Confirmed | visibility=public; methods=13; fields=7 |
| DolocTown.UI.MessageBoxInSceneWithIcon | class | Confirmed | visibility=public; methods=14; fields=6 |
| DolocTown.AnimalRoomEnv | class | Confirmed | visibility=public; methods=16; fields=3 |
| DolocTown.Config.Room.MarkPointInfo | class | Confirmed | visibility=public; methods=15; fields=4 |
| DolocTown.Config.Room.TrashTalkInfo | class | Confirmed | visibility=public; methods=14; fields=5 |
| DolocTown.GameData.RoomProto | class | Confirmed | visibility=public; methods=3; fields=16 |
| DolocTown.WorldContent | class | Confirmed | visibility=public; methods=16; fields=3 |
| DolocTown.DungeonHandle | class | Confirmed | visibility=public; methods=5; fields=13 |
| DolocTown.SceneLight | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.UI.MessageBoxInScene | class | Confirmed | visibility=public; methods=13; fields=5 |
| DolocTown.UI.OperationTipInSceneManager | class | Confirmed | visibility=public; methods=15; fields=3 |
| DolocTown.UI.SceneDialogueBox | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12 | class | Confirmed | visibility=private; methods=10; fields=7 |
| DolocTown.IEquipmentHost/<get_AllEquipmentsIncludeSubrooms>d__13 | class | Confirmed | visibility=private; methods=11; fields=6 |
| DolocTown.NpcManager/<GetNpcsInScene>d__14 | class | Confirmed | visibility=private; methods=9; fields=8 |
| DolocTown.Room/<get_AllAnimals>d__161 | class | Confirmed | visibility=private; methods=11; fields=6 |
| DolocTown.RoomInteractableObjectManager | class | Confirmed | visibility=public; methods=14; fields=3 |
| DolocTown.SceneLightStreetLamp | class | Confirmed | visibility=public; methods=8; fields=9 |
| DolocTown.Config.Room.TbMapArea | class | Confirmed | visibility=public; methods=10; fields=6 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.__InitCitySystem | candidate lifecycle or hook point | Medium |
| DolocAPI.__InitDolocBaseGameSystems | candidate lifecycle or hook point | Medium |
| DolocAPI.__InitDungeonSystem | candidate lifecycle or hook point | Medium |
| DolocAPI.__InitFarmSystem | candidate lifecycle or hook point | Medium |
| DolocAPI.__InitWeatherRenderSystem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI._TryEnterDungeon | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.<ExtendFarm>g__OnSceneLoaded\|346_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.CanOpenMap | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_EnterDungeon | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.EnterCity | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterDungeon | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterFarm | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterRoom | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterRoom | candidate lifecycle or hook point | Medium |
| DolocAPI.get_userInput | candidate lifecycle or hook point | Medium |
| DolocAPI.OpenMap | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshResourceByType | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshSceneResidentTipText | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshWorldVegetation | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.set_userInput | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.TryEnterDungeonSubRoom | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c__DisplayClass333_0.<EnterDungeon>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass336_0.<EnterRoom>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadCityRooms | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadDungeons | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadRooms | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocInputSource/INormalInputActions.OnRoomInteract | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_RoomInteract | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunction.AfterEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunction.UpdatePerTu | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunctionAreaSpeedUp.AfterEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunctionLightningConductor.UpdatePerTu | candidate lifecycle or hook point | Medium |
| DolocTown.AgentHatRenderer.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.AgentHatRendererMinerHelmet.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.EnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.get_isCurrentRoomClosed | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalAI/AnimalAIState.TryEnterToRoom | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalController.InitRoomSearchers | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalPanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPathFinderDebugger.InitializeRoomEnvironment | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalRoomEnv.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalSystem.RefreshEnv | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalTaskHelper._IsRoomClosed | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalTaskHelper.AnimalEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalUtils.IsRoomClosed | candidate lifecycle or hook point | Medium |
| DolocTown.AnimatedGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.ApplianceHandle.UpdatePerSec | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotDecisionMaker.OnEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.RefreshSortingLayerInRoom | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.BuffManager._UpdatePerTU | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuffManager.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.BuffManager.UpdatePerTUNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.CityEquipment.OnLoadData | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CityEquipmentLogic.OnUpdatePerSecond | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CloudShadowController.OnEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.SceneTipArgs.get_ShouldInteract | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.SceneTipArgs.set_ShouldInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.get_InitialMarkPoint_Ref | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.set_InitialMarkPoint_Ref | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Plant.SeedTypeInfo.get_UseRoomEffect | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Plant.SeedTypeInfo.set_UseRoomEffect | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Player.FarmLevelInfo.get_InitMarkPoint_Ref | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Player.FarmLevelInfo.set_InitMarkPoint_Ref | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.DialogueObjectInfo.get_RefreshType | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.DialogueObjectInfo.set_RefreshType | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.MapTypeInfo.get_UseMask | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.MapTypeInfo.set_UseMask | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.PortalInfo.get_InteractKeyType | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.PortalInfo.get_NeedInteract | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.PortalInfo.get_UseTimeRange | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.PortalInfo.set_InteractKeyType | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.PortalInfo.set_NeedInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.PortalInfo.set_UseTimeRange | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.RoomInfo.get_IsInhouse | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.RoomInfo.set_IsInhouse | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.RoomTimeRange.get_StartTime | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| CityPath/CityScene.get_Translators() | direct call candidate | Medium; needs instance source |
| CityPath/CityScene.GetLocalTranslators(UnityEngine.Vector2 position) | direct call candidate | Medium; needs instance source |
| CityPath/CityScene.IsWalkable(System.Single from; System.Single to) | direct call candidate | Medium; needs instance source |
| DolocAPI.__InitCitySystem(GameManager gameManager) | direct call candidate | Medium |
| DolocAPI.__InitDolocBaseGameSystems(GameManager manager) | direct call candidate | Medium |
| DolocAPI.__InitDungeonSystem(GameManager manager) | direct call candidate | Medium |
| DolocAPI.__InitFarmSystem(GameManager manager) | direct call candidate | Medium |
| DolocAPI.CanOpenMap(DolocTown.Room room) | direct call candidate | Medium |
| DolocAPI.ClearSceneOperationTips() | direct call candidate | Medium |
| DolocAPI.ClearSceneResidentTips() | direct call candidate | Medium |
| DolocAPI.EnterCity(DolocTown.CityRoom room; UnityEngine.Vector2 remotePosition; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterDungeon(System.String name; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterFarm(DolocTown.TemplateRoom room; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterRoom(System.String roomId; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterRoom(DolocTown.Room room; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.FinishTrainingDungeon(System.Boolean shouldFade) | direct call candidate | Medium |
| DolocAPI.get_AgentRealRoomCellPosition() | direct call candidate | Medium |
| DolocAPI.get_AgentRoomCellPosition() | direct call candidate | Medium |
| DolocAPI.get_AgentWorldCellPosition() | direct call candidate | Medium |
| DolocAPI.get_CurrentRoom() | direct call candidate | Medium |
| DolocAPI.get_farmRenderer() | direct call candidate | Medium |
| DolocAPI.get_gameManager() | direct call candidate | Medium |
| DolocAPI.get_gameStateManager() | direct call candidate | Medium |
| DolocAPI.get_IsCurrentStateSupportCutscenes() | direct call candidate | Medium |
| DolocAPI.get_IsPlayerInFarmScene() | direct call candidate | Medium |
| DolocAPI.get_RoomGizmos() | direct call candidate | Medium |
| DolocAPI.get_sceneManager() | direct call candidate | Medium |
| DolocAPI.get_userInput() | direct call candidate | Medium |
| DolocAPI.get_worldResolution() | direct call candidate | Medium |
| DolocAPI.GetMapPosByWorldPosition(DolocTown.Room room; UnityEngine.Vector2 worldPosition; UnityEngine.Vector2& mapPosition) | direct call candidate | Medium |
| DolocAPI.GetNpcsInScene(System.Int32 sceneIndex) | direct call candidate | Medium |
| DolocAPI.GetRoom(System.String name) | direct call candidate | Medium |
| DolocAPI.HideSceneBox() | direct call candidate | Medium |
| DolocAPI.HideSceneResidentTip(System.String id) | direct call candidate | Medium |
| DolocAPI.InvokeSceneResidentTip(System.String id; UnityEngine.Vector2 position; System.String customPrompt) | direct call candidate | Medium |
| DolocAPI.InvokeSceneResidentTip(System.String id; System.String customPrompt) | direct call candidate | Medium |
| DolocAPI.IsInPlayerScene(System.String sceneName) | direct call candidate | Medium |
| DolocAPI.IsInSameDungeon(DolocTown.Room L; DolocTown.Room R) | direct call candidate | Medium |
| DolocAPI.IsNpcAtScene(System.String npcName; System.String sceneName) | direct call candidate | Medium |
| DolocAPI.OpenMap(DolocTown.Room room) | direct call candidate | Medium |
| DolocAPI.QueryRoom(System.String name; DolocTown.Room& room) | direct call candidate | Medium |
| DolocAPI.QuerySceneInfo(System.String name; DolocTown.Config.Room.SceneInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryTemplateRoom(System.String name; DolocTown.GameData.RoomProto& proto) | direct call candidate | Medium |
| DolocAPI.QuitCurrentRoom(DolocTown.Room nextRoom; System.Boolean shouldUnloadScene) | direct call candidate | Medium |
| DolocAPI.RefreshResourceByType(DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type) | direct call candidate | Medium |
| DolocAPI.RefreshSceneResidentTipText() | direct call candidate | Medium |
| DolocAPI.ScreenToWorld(UnityEngine.Vector2 position) | direct call candidate | Medium |
| DolocAPI.set_gameManager(GameManager value) | direct call candidate | Medium |
| DolocAPI.SetMotorPosition(DolocTown.Room room; UnityEngine.Vector2 position) | direct call candidate | Medium |
| DolocAPI.SetNpcToCurrentScene(System.String npcName) | direct call candidate | Medium |
| DolocAPI.SetNpcToScene(System.String npcName; System.String sceneName) | direct call candidate | Medium |
| DolocAPI.SetRoomEnvObjectStatus(System.String roomName; System.Boolean status) | direct call candidate | Medium |
| DolocAPI.SetSceneOperationTipEnabled(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneManual(System.String msg; UnityEngine.Vector2 pos; System.Single durShow; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWaiter(System.String msg; UnityEngine.Vector2 pos; System.Single durShow; System.Single durWiat; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWithIconManual(System.String msg; UnityEngine.Sprite icon; UnityEngine.Vector2 pos; System.Single durShow; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWithIconWaiter(System.String msg; UnityEngine.Sprite icon; UnityEngine.Vector2 pos; System.Single durShow; System.Single durWiat; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowSceneDialogueBox(UnityEngine.Vector2 worldPosition; System.String content; System.Action callback) | direct call candidate | Medium |
| DolocAPI.ShowSceneTextBox(UnityEngine.Vector2 worldPosition; DolocTown.Config.Localization.SceneTextBoxArgs sceneTextArgs; UnityEngine.Transform followedTrans; System.Single duration) | direct call candidate | Medium |
| DolocAPI.SpawnResourceDropItems(DolocTown.DungeonResource resource; System.Boolean isRender; System.String overrideSpawnLut) | direct call candidate | Medium |
| DolocAPI.TryEnterDungeonSubRoom(System.String name) | direct call candidate | Medium |
| DolocAPI.WorldSizeToScreenSize(UnityEngine.Vector2 worldSize) | direct call candidate | Medium |
| DolocAPI.WorldToAroundUiPosBottom(UnityEngine.Vector3 posWS; UnityEngine.Vector2 uiSize; System.Int32 dst) | direct call candidate | Medium |
| DolocAPI.WorldToScreen(UnityEngine.Transform t) | direct call candidate | Medium |
| DolocAPI.WorldToScreen(UnityEngine.Vector2 position) | direct call candidate | Medium |
| DolocBundleManager.get_cityRooms() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_dungeons() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_rooms() | direct call candidate | Medium; needs instance source |
| DolocInputSource/INormalInputActions.OnRoomInteract(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/NormalInputActions.get_RoomInteract() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.get_RoomScanner() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.SetRoom(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunction.AfterEnterRoom(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunction.UpdatePerTu() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionAreaSpeedUp.AfterEnterRoom(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionLightningConductor.UpdatePerTu() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentHatRenderer.UpdatePerTU() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentHatRendererMinerHelmet.UpdatePerTU() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnBuildingChanged(DolocTown.Room changedRoom) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnFenceChanged(DolocTown.Room changedRoom) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnPlatformChanged(DolocTown.Room changedRoom) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnTerrainExtent(DolocTown.Room changedRoom; UnityEngine.Vector2Int offset) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.CallToRoom(DolocTown.Room room; UnityEngine.Vector2Int position) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.EnterRoom(DolocTown.Room nextRoom; System.Boolean checkRoomClosed) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_AnotherRoom() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_CurrentEnv() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_HomeEnv() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_isCurrentRoomClosed() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.SetCurrentRoom(DolocTown.Room room; UnityEngine.Vector2Int position) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.SetHomeRoom(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalController.ResetRoomSearcherStatus() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalController.TryGetAnotherRoom(DolocTown.Room& room) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalController/RoomSearcher.get_HasUnvisitedRooms() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalController/RoomSearcher.get_NextRoom() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalController/RoomSearcher.ResetStatus(DolocTown.Animal animal; DolocTown.Config.Weather.WeatherType weatherType) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalEnterRoom.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPanelUiState.HandleStartUpArgs(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.InitializeRoomEnvironment() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderForRoom.FindPath(UnityEngine.Vector2Int from; UnityEngine.Vector2Int to; System.Int32 width) | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| CityPath.allScenes | System.Collections.Generic.Dictionary`2<System.String,CityPath/CityScene> | Risky: reflection/private field | Disable dependent feature if missing |
| CityPath/CityScene._inwalkableAreas | CityPath/InwalkableArea[] | Risky: reflection/private field | Disable dependent feature if missing |
| CityPath/CityScene._translators | CityPath/Translator[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<farmRenderer>k__BackingField | DolocTown.SceneRendererFarm | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<gameManager>k__BackingField | GameManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<gameStateManager>k__BackingField | GameStateManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<RoomGizmos>k__BackingField | DolocTown.RoomGizmos | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<sceneManager>k__BackingField | DolocTown.SceneManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<userInput>k__BackingField | GameStateMachine | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GoRoom>d__907.<>8__1 | DolocAPI/<>c__DisplayClass907_0 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GoRoom>d__907.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<cityRooms>k__BackingField | DolocTown.GameData.CityRoomDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<dungeons>k__BackingField | DolocTown.GameData.DungeonDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<rooms>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.RoomProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_RoomInteract | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.<RoomScanner>k__BackingField | DolocTown.RoomScanner | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Animal._currentRoomEnv | DolocTown.AnimalRoomEnv | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Animal._tmp_currentRoomId | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Animal._tmp_homeRoomId | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalAI/AnimalAIState.availableRooms | System.Collections.Generic.Queue`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalAI/Goat_MetabolismState.hasAnotherRoomEntered | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalAI/MarshPangolin_MetabolismState.hasAnotherRoomEntered | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalController._roomSearchers | System.Collections.Generic.Dictionary`2<System.Type,DolocTown.AnimalController/RoomSearcher> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalController/RoomSearcher.unvisitedRooms | System.Collections.Generic.Queue`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalEnterRoom.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMapForBuilding.farm | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderForRoom.maxWidth | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderForRoom.pathFinders | DolocTown.IAnimalPathFinder[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderForRoom.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderManager.pathFinderCache | System.Collections.Generic.Dictionary`2<DolocTown.Room,DolocTown.AnimalPathFinderForRoom> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalRoomEnv.allPositions | UnityEngine.Vector2Int[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalRoomEnv.allPositionsSet | System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalRoomSearcher.searchedRoom | System.Collections.Generic.HashSet`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalUtils/<GetAllFencePositions>d__8.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterAnotherRoom._anotherRoomType | DolocTown.AnimalWork_EnterAnotherRoom/AnotherRoomType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterAnotherRoom.requireNormalWeather | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterAnotherRoom.requireSunny | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterRoom._roomState | DolocTown.Utils.AnimalRoomState | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterRoom.force | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterRoom.requireNormalWeather | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_EnterRoom.requireSunny | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate._portalProto | DolocTown.Config.Room.PortalInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBot.<RoomChanging>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBot.roomGuid | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/<get_BrokenRoomEnvs>d__4.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/<get_BrokenRoomEnvs>d__4.<>2__current | DolocTown.AutomateStationEnv/RoomEnv | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/<get_BrokenRoomEnvs>d__4.<>7__wrap1 | DolocTown.AutomateStationEnv/RoomEnv[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/<get_BrokenRoomEnvs>d__4.<>7__wrap2 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/<get_BrokenRoomEnvs>d__4.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv.areaLB | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv.areaRT | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv.cells | UnityEngine.Vector2Int[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv.cellsCache | System.Collections.Generic.List`1<UnityEngine.Vector2Int> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv.isFullArea | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get__AllEquipments>d__9.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get__AllEquipments>d__9.<>2__current | DolocTown.Equipment | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get__AllEquipments>d__9.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.Equipment> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get__AllEquipments>d__9.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>2__current | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>2__current | DolocTown.DropItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv.rootRoom | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.roomGuid | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<room>5__2 | DolocTown.TemplateRoom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetEquipmentsNearStation>d__25.<room>5__2 | DolocTown.TemplateRoom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker.rootRoom | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker/<GetEquipmentsNearStation>d__17.<room>5__2 | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskMove.futureRoom | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskMove.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskMoveInt.room | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController._inCutscene | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<room>k__BackingField | DolocTown.TemplateRoomInHouse | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingManager.updateCache | System.Collections.Generic.HashSet`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CameraController.scenePosition | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CameraController.sceneSize | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CellarExtension/<GetAllQuitPositionsPair>d__14.room | DolocTown.TemplateRoomInHouse | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CloudShadowController._currentRoom | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ConditionChecker.roomVisitCondition | DolocTown.RoomVisitCondition | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingLevelData.<RoomEffect_Ref>k__BackingField | DolocTown.Config.Room.RoomEffectInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingLevelData.<RoomEffect>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingLevelData.<TemplateRoomName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingSpriteGroup.<SceneSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoCollectorHelper.<Types>k__BackingField | DolocTown.Config.Resource.DungeonResourceType[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentRoom.<RoomName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom.<BuffId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom.<EnergyRecv>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom.<HealthRecv>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom.<Mask>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom.<WaterCost>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentInfo.<SceneAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.FestivalInfo.<GateWhitelist_Ref>k__BackingField | DolocTown.Config.Room.PortalInfo[] | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.__InitDungeonSystem | Harmony patch candidate | GameManager manager | Medium |
| DolocAPI._TryEnterDungeon | Harmony patch candidate | DolocTown.Dungeon dungeon | Risky |
| DolocAPI.<ExtendFarm>g__OnSceneLoaded\|346_1 | Harmony patch candidate | DolocTown.GameData.ArchiveDataHandle handle | Risky |
| DolocAPI.Command_EnterDungeon | Harmony patch candidate | System.String dungeonName | Risky |
| DolocAPI.EnterCity | Harmony patch candidate | DolocTown.CityRoom room; UnityEngine.Vector2 remotePosition; System.Action callback | Medium |
| DolocAPI.EnterDungeon | Harmony patch candidate | System.String name; System.Action callback | Medium |
| DolocAPI.EnterFarm | Harmony patch candidate | DolocTown.TemplateRoom room; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.EnterRoom | Harmony patch candidate | System.String roomId; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.EnterRoom | Harmony patch candidate | DolocTown.Room room; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.get_userInput | Harmony patch candidate |  | Medium |
| DolocAPI.RefreshResourceByType | Harmony patch candidate | DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type | Medium |
| DolocAPI.RefreshSceneResidentTipText | Harmony patch candidate |  | Medium |
| DolocAPI.RefreshWorldVegetation | Harmony patch candidate |  | Risky |
| DolocAPI.set_userInput | Harmony patch candidate | GameStateMachine value | Risky |
| DolocAPI.TryEnterDungeonSubRoom | Harmony patch candidate | System.String name | Medium |
| DolocAPI/<>c__DisplayClass333_0.<EnterDungeon>b__0 | Harmony patch candidate |  | Risky |
| DolocAPI/<>c__DisplayClass336_0.<EnterRoom>b__0 | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadCityRooms | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadDungeons | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadRooms | Harmony patch candidate |  | Risky |
| DolocInputSource/INormalInputActions.OnRoomInteract | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/NormalInputActions.get_RoomInteract | Harmony patch candidate |  | Medium |
| DolocTown.AgentEquipmentFunction.AfterEnterRoom | Harmony patch candidate | DolocTown.Room room | Medium |
| DolocTown.AgentEquipmentFunction.UpdatePerTu | Harmony patch candidate |  | Medium |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IGameLoopEvents / IGameInfoHelper` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentRoom | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncShowerRoom | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Localization.SceneTextBoxArgs | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Localization.SceneTipArgs | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.DungeonResourceClass | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.DungeonResourceType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.BackgroundHighLevel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.BackgroundHighLevelInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.CfgMapAreaType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.CfgRoomType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.DialogueObjectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.EnvObjectSpawnData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.InwalkableAreaInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.LockableObjectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.MapAreaInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.MapRoomInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.MapRoomTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.MapTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.MarkPointInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.ObjectRefreshFrequency | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.PortalInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.PortalInteractKey | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomConstructInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomEffectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomSpawnInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomTimeRange | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.SceneInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.StationInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbBackgroundHighLevel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbDialogueObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbInwalkableArea | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbLockableObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbMapArea | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbMapRoom | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbMapRoomType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbMapType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbMarkPoint | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbPortal | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbRoom | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbRoomEffect | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbScene | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbStation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbTrashTalk | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TrashTalkInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Time.DungeonSeasonInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Time.TbDungeonSeason | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Editor.DungeonPlatformGenerator/PlatformConfig | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationDungeon | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.CityRoomDatabase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IGameLoopEvents
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Use runtime verification before exposing any timing-sensitive event.
