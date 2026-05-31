# DolocTown Buildings / Devices / Decoration API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map placeable object definitions.
- Understand occupied tiles and placement rules.
- Scope official device/platform content pack support.

## Decompiled scope

- Matched types: 135
- Matched methods: 1791
- Matched fields: 803
- Matched properties: 601
- Matched events: 1
- Matched call edges: 8219
- Matched strings: 745
- Raw indexes: `maps/index/Buildings-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Config.Building.BuildingInfo | class | Confirmed | visibility=public; methods=113; fields=44 |
| DolocTown.Building | class | Confirmed | visibility=public; methods=94; fields=16 |
| DolocTown.BuildingBuilder | class | Confirmed | visibility=public; methods=32; fields=17 |
| DolocTown.Config.Platform.BuildingSupportInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.IBuildingHost | interface | Confirmed | visibility=public; methods=36; fields=0 |
| DolocTown.PlatformGeometry | class | Confirmed | visibility=public; methods=32; fields=2 |
| DolocTown.UI.BuildingData | struct | Confirmed | visibility=public; methods=17; fields=16 |
| DolocTown.BuildingLinkGate | class | Confirmed | visibility=public; methods=26; fields=6 |
| DolocTown.Config.Building.BuildingLevelData | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.Config.Platform.PlatformInfo | class | Confirmed | visibility=public; methods=23; fields=6 |
| DolocTown.Platform | class | Confirmed | visibility=public; methods=20; fields=8 |
| DolocTown.PlatformBuilder | class | Confirmed | visibility=public; methods=21; fields=7 |
| DolocTown.PlatformBuilderHelper | class | Confirmed | visibility=public; methods=19; fields=9 |
| DolocTown.Config.Room.RoomConstructInfo | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.BuildingItemBuilderTip | class | Confirmed | visibility=public; methods=13; fields=11 |
| DolocTown.Config.Building.BuildingExteriorData | class | Confirmed | visibility=public; methods=18; fields=6 |
| DolocTown.BuildingGate | class | Confirmed | visibility=public; methods=18; fields=5 |
| DolocTown.Config.Platform.PlatformSurfaceData | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.UI.PlatformData | struct | Confirmed | visibility=public; methods=12; fields=11 |
| DolocTown.UI.TempBuildingViewer | class | Confirmed | visibility=public; methods=21; fields=2 |
| DolocTown.BuildingLinkInfo | class | Confirmed | visibility=public; methods=15; fields=7 |
| DolocTown.IPlatformHost | interface | Confirmed | visibility=public; methods=22; fields=0 |
| DolocTown.BuildingRenderer | class | Confirmed | visibility=public; methods=17; fields=4 |
| DolocTown.Config.Building.BuildingWallpaperData | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.PlatformBuilderRenderer | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.ItemBuilding | class | Confirmed | visibility=public; methods=18; fields=2 |
| DolocTown.PlatformCutInfos/<get_AllCutPositions>d__2 | class | Confirmed | visibility=private; methods=11; fields=9 |
| DolocTown.BuildingSupport | class | Confirmed | visibility=public; methods=10; fields=9 |
| DolocTown.DepthFogControllerBuilding | class | Confirmed | visibility=public; methods=10; fields=9 |
| DolocTown.PlatformItemBuilderTip | class | Confirmed | visibility=public; methods=13; fields=6 |
| DolocTown.BuildingManager | class | Confirmed | visibility=public; methods=16; fields=2 |
| DolocTown.BuildingPanelUiState | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.Config.Building.BuildingWallpaperInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Platform.PlatformColumnData | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.PlatformManager | class | Confirmed | visibility=public; methods=14; fields=3 |
| DolocTown.PlatformManager/<get_totalColliders>d__12 | class | Confirmed | visibility=private; methods=10; fields=7 |
| DolocTown.AnimalMap/<FromBuilding>d__24 | class | Confirmed | visibility=private; methods=10; fields=6 |
| DolocTown.BuildingLinkGateMap | class | Confirmed | visibility=public; methods=11; fields=5 |
| DolocTown.PlatformGeometry/<GetColumnPositionsFromPos>d__49 | class | Confirmed | visibility=private; methods=8; fields=8 |
| DolocTown.PlatformManager/<get_totalPlatforms>d__10 | class | Confirmed | visibility=private; methods=10; fields=6 |
| DolocTown.PlatformManager/<GetCollidersAtRow>d__13 | class | Confirmed | visibility=private; methods=9; fields=7 |
| DolocTown.PlatformPanelUiState | class | Confirmed | visibility=public; methods=13; fields=3 |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26 | class | Confirmed | visibility=private; methods=9; fields=6 |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25 | class | Confirmed | visibility=private; methods=9; fields=6 |
| DolocTown.BuildingBuilder/<>c | class | Confirmed | visibility=private; methods=8; fields=7 |
| DolocTown.Config.Building.BuildingExteriorInfo | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.PlatformGeometry/<get_AllPositions>d__30 | class | Confirmed | visibility=private; methods=10; fields=5 |
| DolocTown.PlatformGeometry/<get_ColumnPositions>d__28 | class | Confirmed | visibility=private; methods=8; fields=7 |
| DolocTown.BuildingScanner | class | Confirmed | visibility=public; methods=9; fields=5 |
| DolocTown.Config.Building.BuildingSpriteGroup | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Item.ItemFunctionBuildingExterior | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.ItemBuildingExterior | class | Confirmed | visibility=public; methods=14; fields=0 |
| DolocTown.PlatformGeometry/<get_SurfacePositions>d__21 | class | Confirmed | visibility=private; methods=8; fields=6 |
| DolocTown.PlatformGeometry/<GetColumnPositions>d__41 | class | Confirmed | visibility=private; methods=9; fields=5 |
| DolocTown.TemplateRoomInHouse/<get_Buildings>d__12 | class | Confirmed | visibility=private; methods=9; fields=5 |
| DolocTown.BuildingRecipe | class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocTown.PlatformGeometry/<get_PlatformPositions>d__23 | class | Confirmed | visibility=private; methods=8; fields=5 |
| DolocTown.PlatformGeometry/<get_PlatformPositionsIncludeBorder>d__25 | class | Confirmed | visibility=private; methods=8; fields=5 |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13 | class | Confirmed | visibility=private; methods=8; fields=4 |
| DolocTown.BuildingBuilderRenderer | class | Confirmed | visibility=public; methods=10; fields=2 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_OpenBuildingPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap.LoadBuildingStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap.LoadPlatformStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadPlatformStairs>b__16_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMapForBuilding.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot.EnterBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot.EnterMainFarm | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot/<>c__DisplayClass8_0.<EnterMainFarm>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuilderTipBase.GetUpdateFunc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Building.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.Building.get_isClosed | candidate lifecycle or hook point | Medium |
| DolocTown.Building.get_PositionCenter | candidate lifecycle or hook point | Medium |
| DolocTown.Building.GetInteractText | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Building.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.Building.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.Building.RefreshRender | candidate lifecycle or hook point | Medium |
| DolocTown.Building.RefreshWallpaper | candidate lifecycle or hook point | Medium |
| DolocTown.Building.set_isClosed | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Building.SetClosed | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingBuilder.ExitBuilder | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingBuilder.OnUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingBuilder.UpdateInvalidEquipmentRenderer | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.get_NeedInteract | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.get_UseCacheBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.set_UseCacheBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.Start | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.__UpdateFunc_JoyStick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.__UpdateFunc_KM | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.ExitBuilder | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingItemBuilderTip.OnEnter | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.OnUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.EnterTargetBuilding | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingLinkGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.get_NeedInteract | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.OnDisable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingLinkGate.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.Start | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingLinkGateEditorHelper.LoadSortedGates | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingManager.UpdateNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingManager.UpdateSpec | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingMaterial.Start | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState.OnPause | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingPanelUiState.OnStartButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingRenderer.InitMaterialInfos | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingRenderer.UpdateRenderStatus | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingScanner.TryEnter | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingScanner.TryInteract | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingSupport.InitFulcrums | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingSupport.InitPlatform | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingSupport.InitTileLayer | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingExteriorData.get_CanOpenDoor | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.get_ClosedSpriteGroup | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.get_OpenedSpriteGroup | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.set_ClosedSpriteGroup | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingExteriorData.set_OpenedSpriteGroup | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingInfo.LoadTileTypes | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingInfo/<>c.<LoadTileTypes>b__191_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_BuilderExitErrSoleBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_BuilderExitErrSoleBuilding_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_BuildingPanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_BuildingPanelStartBuild_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_PlatformPanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_PlatformPanelStartBuild_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_BuilderExitErrSoleBuilding | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.set_BuildingPanelStartBuild | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.set_PlatformPanelStartBuild | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_BuilderExitErrSoleBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.TbStaticText.get_BuildingPanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.TbStaticText.get_PlatformPanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.DepthFogControllerBuilding.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUserInput.UpdateActiveDevice | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUserInput/<>c.<UpdateActiveDevice>b__332_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GlobalBuilderState.RefreshOperationTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.IBuildingHost.AfterLoadBuildings | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_ExtendBuilding() | direct call candidate | Medium |
| DolocAPI.get_SelectedBuilding() | direct call candidate | Medium |
| DolocAPI.GetActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; DolocTown.ActionIconGroup& iconGroup) | direct call candidate | Medium |
| DolocAPI.GetAllActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; System.Collections.Generic.List`1<DolocTown.ActionIconGroup>& iconGroups) | direct call candidate | Medium |
| DolocAPI.GetBuildingTitle(System.String buildingName) | direct call candidate | Medium |
| DolocAPI.GetParsedKeystrokeText(DolocTown.DolocInputDeviceType deviceType; System.String text) | direct call candidate | Medium |
| DolocAPI.QueryBuilding(System.String name; DolocTown.Config.Building.BuildingInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryPlatformProto(System.String name; DolocTown.Config.Platform.PlatformInfo& proto) | direct call candidate | Medium |
| DolocBuilder.OnInputDeviceChanged(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_devices() | direct call candidate | Medium; needs instance source |
| DolocInputSource.set_devices(System.Nullable`1<UnityEngine.InputSystem.Utilities.ReadOnlyArray`1<UnityEngine.InputSystem.InputDevice>> value) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.get_BuildingScanner() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentPhysicalStatus.get_IsGroundedExcludePlatform() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnBuildingChanged(DolocTown.Room changedRoom) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.__OnPlatformChanged(DolocTown.Room changedRoom) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalMapForBuilding.CheckWalkableByBuilding(UnityEngine.Vector2Int pos) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalMapForBuilding.get_AllPositions() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalMapForBuilding.Refresh() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalSystem.OnBuildingChanged(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalSystem.OnPlatformChanged(DolocTown.Room room) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalUtils.get_AnimalMapForBuilding() | direct call candidate | Medium |
| DolocTown.AnimalUtils.IsOnPlatform(DolocTown.Room room; UnityEngine.Vector2Int pos) | direct call candidate | Medium |
| DolocTown.AnimalUtils.IsOnPlatformOrBuilding(DolocTown.Room room; UnityEngine.Vector2Int pos) | direct call candidate | Medium |
| DolocTown.AutomateBot.EnterBuilding(DolocTown.Building building) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBot.EnterMainFarm(DolocTown.Building fromBuilding) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotStation.OnBuildingChanged(DolocTown.Building building; System.Boolean isRemoved) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateStationEnv/RoomEnv.get_AllBuildings() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystem.OnBuildingChanged(DolocTown.Building building; System.Boolean isRemoved) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskEnterBuilding.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.BuilderState`2.get_Construct() | direct call candidate | Medium; needs instance source |
| DolocTown.BuilderState`2.GetOperateTip(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocTown.BuilderState`2.OnInputDeviceChanged(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocTown.BuilderTipBase.OnInputDeviceChanged(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocTown.BuilderUtils.CreatePlatformIndicator(DolocTown.PlatformGeometry geometry; DolocTown.Config.Platform.PlatformInfo proto; UnityEngine.Vector2 roomPosition) | direct call candidate | Medium |
| DolocTown.Building.AfterLoadData() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.AfterNewGame() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.BuildingToWorld(UnityEngine.Vector2 position) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.ClearWindow() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.Damage(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.Dismantle(System.Action afterDismantle) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.GenLinkMap(System.Boolean shouldCalculateRemote) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_ArrowTipPosition() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_BuildingName() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_CanPatch() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_canUpgrade() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_currentLevelData() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_customName() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_DoorSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_EntryPosition() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_exteriorData() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_exteriorId() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_GlobalAnchor() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_Health() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_HealthProcess() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_HealthTipPosition() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_InnerSize() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_IsBroken() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_isClosed() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_IsIntact() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_IsOverCellarBorder() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_IsRemoved() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_IsUnique() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_level() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_LinkGateMap() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_maxLevel() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionAssistTip() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionBottom() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionCenter() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionLB() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionTip() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_PositionTop() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_proto() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_Renderer() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_room() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_SceneSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_TargetRoomGuid() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_templateRoomName() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_UiSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_uuid() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_wallpaperData() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.get_wallpaperId() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.GetBuildingCeilingEquipments() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.InvokeChangeNameInputBox(System.Action onConfirm) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnDisTouch() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnInteract() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnMove() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnRemove() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnRender() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnTouch() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.OnUnRender() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.RefreshRender() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.RefreshWallpaper() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.Repair(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.RetrieveItemOnRemoval(System.Boolean putInBackpack) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.set_Renderer(DolocTown.BuildingRenderer value) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.set_Title(System.String value) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.SetClosed(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.Building.SetExterior(DolocTown.Config.Building.BuildingExteriorInfo exteriorInfo; System.Boolean shouldRender) | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocTown.AgentControllerState.<BuildingScanner>k__BackingField | DolocTown.BuildingScanner | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap._buildingStairs | System.Collections.Generic.Dictionary`2<UnityEngine.Vector2Int,DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap._platformStairs | System.Collections.Generic.Dictionary`2<UnityEngine.Vector2Int,DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>2__current | DolocTown.AnimalStair | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.building | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26.<>2__current | DolocTown.AnimalStair | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingCeiling>d__26.building | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25.<>2__current | DolocTown.AnimalStair | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuildingFloor>d__25.building | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMapForBuilding.farm | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMapForBuilding.walkablePositions | System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.drawAnimalMapForBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalUtils._animalMapForBuilding | DolocTown.AnimalMapForBuilding | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>2__current | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllBuildings>d__13.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>7__wrap2 | System.Collections.Generic.IEnumerator`1<DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetEquipmentsNearStation>d__25.<>7__wrap3 | System.Collections.Generic.IEnumerator`1<DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetEquipmentsNearStation>d__25.includeBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetEquipmentsNearStation>d__26`1.includeBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker/<GetEquipmentsNearStation>d__17.<>7__wrap3 | System.Collections.Generic.IEnumerator`1<DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker/<GetEquipmentsNearStation>d__17.includeBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker/<GetEquipmentsNearStation>d__18`1.includeBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskEnterBuilding.building | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskEnterMainFarm.fromBuilding | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building._linkGateMap | DolocTown.BuildingLinkGateMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<customName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<exteriorId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<isClosed>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<level>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<proto>k__BackingField | DolocTown.Config.Building.BuildingInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<Renderer>k__BackingField | DolocTown.BuildingRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<room>k__BackingField | DolocTown.TemplateRoomInHouse | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<uuid>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.<wallpaperId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.health | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.isDoorTouching | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.isRender | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Building.isTouching | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder._buildingViewer | DolocTown.UI.TempBuildingViewer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.<TempBuildingList>k__BackingField | DolocTown.LinearInventory | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.areaEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.buildingProto | DolocTown.Config.Building.BuildingInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.buildingSlot | DolocTown.GameEntitySlot`1<DolocTown.SingleSpriteRender> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.buildingSupport | DolocTown.BuildingSupport | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.ceilingEquipments | System.Collections.Generic.List`1<DolocTown.Equipment> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.coveredRatio | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.currentBuilding | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.doorEquipments | System.Collections.Generic.List`1<DolocTown.Equipment> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.groundCheckValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.heightValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.holdStartTime | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.HoldThreshold | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.indicatorRenderer | DolocTown.BuildingBuilderRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.isTemporaryItem | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.posWS | UnityEngine.Vector3 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilderRenderer.supportIndicator | DolocTown.TilemapGroupManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingGate.<TargetBuilding>k__BackingField | DolocTown.Building | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingGate.<UseCacheBuilding>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingGate.arrow | UnityEngine.GameObject | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingGate.fadeTime | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingGate.sr | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip._anchor | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.areaEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.buildingSupport | DolocTown.BuildingSupport | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.coveredRatio | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.currentItem | DolocTown.ItemBuilding | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.doorEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.groundCheckValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.heightValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.indicaterRenderer | DolocTown.BuildingBuilderRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.lastPos | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.posWS | UnityEngine.Vector3 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGate.<LinkInfo>k__BackingField | DolocTown.BuildingLinkInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGate.index | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGate.indicator | DolocTown.TouchIndicator | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGate.linkType | DolocTown.BuildingLinkType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGate.tipPos | UnityEngine.Transform | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGateMap.allLinkInfos | DolocTown.BuildingLinkInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGateMap.bottomLinks | DolocTown.BuildingLinkInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGateMap.leftLinks | DolocTown.BuildingLinkInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGateMap.rightLinks | DolocTown.BuildingLinkInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkGateMap.topLinks | DolocTown.BuildingLinkInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkInfo.<index>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkInfo.<IsValid>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkInfo.<originIndex>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingLinkInfo.<remotePosition>k__BackingField | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingManager.buildings | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingManager.updateCache | System.Collections.Generic.HashSet`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingMaterial._material | UnityEngine.Material | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingMaterial.gateMask | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocTown.DolocUserInput.OnInputDeviceChanged | native event | System.Action`1<DolocTown.DolocInputDeviceType> | Confirmed metadata; verify usage |
| DolocTown.AnimalMap.LoadBuildingStairs | Harmony patch candidate |  | Risky |
| DolocTown.AnimalMap.LoadPlatformStairs | Harmony patch candidate |  | Risky |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_0 | Harmony patch candidate | DolocTown.AnimalStair stair | Risky |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_1 | Harmony patch candidate | DolocTown.AnimalStair stair | Risky |
| DolocTown.AnimalMap/<>c.<LoadPlatformStairs>b__16_0 | Harmony patch candidate | DolocTown.AnimalStair pt | Risky |
| DolocTown.AnimalMapForBuilding.Refresh | Harmony patch candidate |  | Medium |
| DolocTown.AutomateBot.EnterBuilding | Harmony patch candidate | DolocTown.Building building | Medium |
| DolocTown.AutomateBot.EnterMainFarm | Harmony patch candidate | DolocTown.Building fromBuilding | Medium |
| DolocTown.AutomateBot/<>c__DisplayClass8_0.<EnterMainFarm>b__0 | Harmony patch candidate | DolocTown.Building x | Risky |
| DolocTown.BuilderTipBase.GetUpdateFunc | Harmony patch candidate | DolocTown.DolocInputDeviceType type | Risky |
| DolocTown.Building.AfterLoadData | Harmony patch candidate |  | Medium |
| DolocTown.Building.get_PositionCenter | Harmony patch candidate |  | Medium |
| DolocTown.Building.GetInteractText | Harmony patch candidate |  | Risky |
| DolocTown.Building.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.Building.OnInteract | Harmony patch candidate |  | Medium |
| DolocTown.Building.RefreshRender | Harmony patch candidate |  | Medium |
| DolocTown.Building.RefreshWallpaper | Harmony patch candidate |  | Medium |
| DolocTown.BuildingBuilder.ExitBuilder | Harmony patch candidate |  | Medium |
| DolocTown.BuildingBuilder.OnUpdate | Harmony patch candidate | System.Single deltaTime | Medium |
| DolocTown.BuildingBuilder.UpdateInvalidEquipmentRenderer | Harmony patch candidate |  | Risky |
| DolocTown.BuildingGate.get_InteractKey | Harmony patch candidate |  | Medium |
| DolocTown.BuildingGate.get_NeedInteract | Harmony patch candidate |  | Medium |
| DolocTown.BuildingGate.get_UseCacheBuilding | Harmony patch candidate |  | Medium |
| DolocTown.BuildingGate.OnInteract | Harmony patch candidate |  | Medium |
| DolocTown.BuildingGate.set_UseCacheBuilding | Harmony patch candidate | System.Boolean value | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IBuildingHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.BuildingLinkInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.BuildingLinkInfoForCellar | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingExteriorData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingExteriorInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingLevelData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingSpriteGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingTileType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingWallpaperData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingWallpaperInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.FulcrumData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuilding | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuildingExterior | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuildingWallpaper | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBuilding | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBuildingExterior | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionConstructController | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionPlatform | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.BuildingSupportInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.PlatformColumnData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.PlatformInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.PlatformSurfaceData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.TbBuildingSupport | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Platform.TbPlatform | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.RoomConstructInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Editor.DungeonPlatformGenerator/PlatformConfig | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.PlatformPositionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.PlatformCutInfos | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.PlatformCutInfos/<get_AllCutPositions>d__2 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.BuildingData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.PlatformData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IBuildingHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Placement/save behavior needs careful in-game verification.
