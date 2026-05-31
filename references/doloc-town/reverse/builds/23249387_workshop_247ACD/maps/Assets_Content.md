# DolocTown Assets / Content Loading API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map JSON/PNG/config loading.
- Support read-only content helper first.
- Prepare future Content Patcher style APIs.

## Decompiled scope

- Matched types: 945
- Matched methods: 15874
- Matched fields: 6851
- Matched properties: 6356
- Matched events: 0
- Matched call edges: 71051
- Matched strings: 15366
- Raw indexes: `maps/index/Assets_Content-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Config.Localization.StaticTextInfo | class | Confirmed | visibility=public; methods=2224; fields=1479 |
| DolocTown.Config.Settings.GlobalParameterInfo | class | Confirmed | visibility=public; methods=557; fields=269 |
| DolocTown.Config.Localization.TbStaticText | class | Confirmed | visibility=public; methods=742; fields=1 |
| DolocTown.Config.Tables | class | Confirmed | visibility=public; methods=204; fields=192 |
| DolocGameAssets | enum | Confirmed | visibility=public; methods=0; fields=235 |
| DolocTown.Config.Settings.TbGlobalParameter | class | Confirmed | visibility=public; methods=233; fields=1 |
| DolocTown.Config.Animal.AnimalInfo | class | Confirmed | visibility=public; methods=119; fields=52 |
| DolocTown.Config.Building.BuildingInfo | class | Confirmed | visibility=public; methods=113; fields=44 |
| DolocTown.Config.Settings.UserSettingType | enum | Confirmed | visibility=public; methods=0; fields=138 |
| DolocTown.InteractableObject | class | Confirmed | visibility=public; methods=72; fields=39 |
| DolocTown.Config.Fishing.FishInfo | class | Confirmed | visibility=public; methods=73; fields=29 |
| DolocTown.Config.Equipment.EquipmentInfo | class | Confirmed | visibility=public; methods=74; fields=19 |
| DolocTown.GameData.DRockEffectsConfig | class | Confirmed | visibility=public; methods=47; fields=46 |
| DolocTown.Config.NPC.NpcInfo | class | Confirmed | visibility=public; methods=60; fields=26 |
| DolocTown.Config.Drone.DroneWeaponInfo | class | Confirmed | visibility=public; methods=56; fields=27 |
| DolocBundleManager | class | Confirmed | visibility=public; methods=57; fields=20 |
| DolocTown.Config.ModManager | class | Confirmed | visibility=public; methods=57; fields=20 |
| DolocTown.Config.Item.ItemInfo | class | Confirmed | visibility=public; methods=50; fields=22 |
| DolocTown.Config.Time.SeasonInfo | class | Confirmed | visibility=public; methods=43; fields=19 |
| DolocTown.Config.Resource.ResourceInfo | class | Confirmed | visibility=public; methods=44; fields=15 |
| DolocTown.Config.Room.PortalInfo | class | Confirmed | visibility=public; methods=42; fields=17 |
| DolocTown.Config.Recipe.DishGroupInfo | class | Confirmed | visibility=public; methods=38; fields=19 |
| DolocTown.Config.ModInfo | class | Confirmed | visibility=public; methods=35; fields=21 |
| DolocTown.Config.Room.MapAreaInfo | class | Confirmed | visibility=public; methods=39; fields=17 |
| DolocTown.Config.Plant.SeedInfo | class | Confirmed | visibility=public; methods=39; fields=15 |
| DolocTown.Config.Mission.FactionMissionInfo | class | Confirmed | visibility=public; methods=36; fields=17 |
| DolocTown.Config.Mission.TreatyPortFactionInfo | class | Confirmed | visibility=public; methods=36; fields=16 |
| DolocTown.GameData.GameInitConfig | class | Confirmed | visibility=public; methods=8; fields=42 |
| DolocTown.Config.Monster.MonsterDocumentInfo | class | Confirmed | visibility=public; methods=33; fields=16 |
| DolocTown.Config.Monster.MonsterInfo | class | Confirmed | visibility=public; methods=33; fields=14 |
| DolocTown.Config.Player.HatInfo | class | Confirmed | visibility=public; methods=32; fields=14 |
| DolocTown.GameServiceLocator.IEffectsConfigProvider | interface | Confirmed | visibility=public; methods=46; fields=0 |
| DolocTown.Config.Buff.BuffInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Drone.DroneChipInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Mission.BoardMissionInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Platform.BuildingSupportInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Weather.WeatherInfo | class | Confirmed | visibility=public; methods=30; fields=14 |
| DolocTown.Config.Resource.ResourceDocumentInfo | class | Confirmed | visibility=public; methods=29; fields=14 |
| DolocTown.Config.Automate.AutomateBotInfo | class | Confirmed | visibility=public; methods=32; fields=10 |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocTown.Config.Mission.ItemSubmitConditionInfo | class | Confirmed | visibility=public; methods=30; fields=12 |
| DolocTown.Config.Recipe.RecipeInfo | class | Confirmed | visibility=public; methods=30; fields=12 |
| DolocTown.Config.Settings.RebindActionInfo | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocTown.Config.Mission.MissionInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.Config.NPC.IdleTalkNodeInfo | class | Confirmed | visibility=public; methods=29; fields=12 |
| DolocTown.Config.Recipe.RecipeGroupInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.Config.Store.StoreInfo | class | Confirmed | visibility=public; methods=30; fields=11 |
| DolocTown.Config.TechTree.TechPointInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.Config.Room.MapTypeInfo | class | Confirmed | visibility=public; methods=28; fields=12 |
| DolocTown.Config.Store.ExchangeStoreItemData | class | Confirmed | visibility=public; methods=30; fields=10 |
| DolocTown.Config.Item.ItemFunctionBox | class | Confirmed | visibility=public; methods=28; fields=11 |
| DolocTown.Config.Plant.CropGeneInfo | class | Confirmed | visibility=public; methods=27; fields=12 |
| DolocTown.Config.Plant.TreeSeedInfo | class | Confirmed | visibility=public; methods=29; fields=10 |
| DolocTown.Config.Room.SceneInfo | class | Confirmed | visibility=public; methods=27; fields=12 |
| DolocTown.Config.Drone.DroneStructureInfo | class | Confirmed | visibility=public; methods=28; fields=10 |
| DolocTown.Config.Equipment.LampInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.Config.Fishing.FarmFishInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.Config.Localization.LocalizationInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.Config.Resource.VegetationInfo | class | Confirmed | visibility=public; methods=28; fields=10 |
| DolocTown.Config.NPC.NpcDocumentInfo | class | Confirmed | visibility=public; methods=24; fields=12 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.__LoadStaticAssets | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_LockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockAllInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadConfigTables | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.LoadSprite | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshResourceByType | candidate lifecycle or hook point | Medium |
| DolocAPI.SetResidentUiInteractable | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c__DisplayClass37_0.<__LoadStaticAssets>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass760_0.<RefreshResourceByType>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAssetCache.Load | candidate lifecycle or hook point | Medium |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.InitDataAsync | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadAsync | candidate lifecycle or hook point | Medium |
| DolocBundleManager.LoadBulletMovers | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadBullets | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadCityRooms | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadDungeons | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadEffects | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadGameParamSettings | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadGameProcessGraphs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadLamps | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadLubanConfigs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadMissionChains | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadMonsters | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadNpcSchedules | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadRooms | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadRoutineGraphs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadSettings | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadSkills | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadTechTrees | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadWaterTemplates | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager/<>c__DisplayClass75_0.<LoadAsync>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocGameAssetsPatch.LoadMaterial | candidate lifecycle or hook point | Medium |
| DolocResources.loadConfig | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.get_ScannerInteractable | candidate lifecycle or hook point | Medium |
| DolocTown.Animal._UpdateData | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Animal._UpdateMood | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalController.InitRoomSearchers | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimatedGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.AnimatorAsset.TryLoadAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AssetBase`1.LoadDefaultAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AssetBase`1.TryLoadAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BackgroundLayerRenderer.LoadLayer | candidate lifecycle or hook point | Medium |
| DolocTown.Battery.RefreshSprite | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BattleUtils.LoadBattleConfig | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingLinkGate.get_InteractKey | candidate lifecycle or hook point | Medium |
| DolocTown.Case.RefreshSprite | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Case/<>c.<AfterLoadEquipment>b__43_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState.<RefreshItemList>b__56_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_3 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionManager.RefreshResourceRecord | candidate lifecycle or hook point | Medium |
| DolocTown.CommandDefines.HideCurrentInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.SetTargetToCurrentInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Animal.TbHusbandry.InitReverseMap | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Buff.RecoveryDecayInfo.get_TimeSinceAwake | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Buff.RecoveryDecayInfo.set_TimeSinceAwake | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingExteriorData.get_CanOpenDoor | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.get_ClosedSpriteGroup | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.get_OpenedSpriteGroup | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Building.BuildingExteriorData.set_ClosedSpriteGroup | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingExteriorData.set_OpenedSpriteGroup | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingInfo.LoadTileTypes | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Building.BuildingInfo/<>c.<LoadTileTypes>b__191_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.DolocConfig.Init | candidate lifecycle or hook point | Medium |
| DolocTown.Config.DolocConfig.Loader | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.DolocConfig.Reload | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Drone.DroneChipInfo.get_ReloadDurationDecrease | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Drone.DroneChipInfo.set_ReloadDurationDecrease | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Drone.DroneWeaponInfo.get_ReloadDuration | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Drone.DroneWeaponInfo.set_ReloadDuration | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.ChairInfo.get_Save | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.ChairInfo.set_Save | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_UpdateInterval | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.set_UpdateInterval | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| AddressablesUtility.GetAddressFromAssetReference(UnityEngine.AddressableAssets.AssetReference reference) | direct call candidate | Medium |
| DolocAPI.__InstallConfigs() | direct call candidate | Medium |
| DolocAPI.__LoadStaticAssets(System.Action callback) | direct call candidate | Medium |
| DolocAPI.AddTechExp(DolocTown.Config.TechTree.TechPointType type; System.Int32 exp) | direct call candidate | Medium |
| DolocAPI.AddTechPoint(DolocTown.Config.TechTree.TechPointType type; System.Int32 pt) | direct call candidate | Medium |
| DolocAPI.Broadcast(DolocTown.Config.Settings.UserSettingType evtType; DolocTown.GameEventArgs e; System.Object sender) | direct call candidate | Medium |
| DolocAPI.CheckAsset(System.String address) | direct call candidate | Medium |
| DolocAPI.CostTechPoint(System.Int32 count; DolocTown.Config.TechTree.TechPointType type) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; System.Int32 count; UnityEngine.Vector2 startPos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateItem(DolocTown.Config.Item.ItemInfo proto; System.Int32 count) | direct call candidate | Medium |
| DolocAPI.get_assets() | direct call candidate | Medium |
| DolocAPI.get_CurrentL10nInfo() | direct call candidate | Medium |
| DolocAPI.get_eftConfig() | direct call candidate | Medium |
| DolocAPI.get_gameConfig() | direct call candidate | Medium |
| DolocAPI.get_GlobalParameter() | direct call candidate | Medium |
| DolocAPI.get_modManager() | direct call candidate | Medium |
| DolocAPI.GetAsset(DolocGameAssets address) | direct call candidate | Medium |
| DolocAPI.GetAsset(System.String address; System.Boolean useLog) | direct call candidate | Medium |
| DolocAPI.GetItemSprite(System.String itemName) | direct call candidate | Medium |
| DolocAPI.LoadSprite(System.String imageName) | direct call candidate | Medium |
| DolocAPI.MoveFadeOutSprite(UnityEngine.Vector2 from; UnityEngine.Vector2 to; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.PerformEnvOptimizerBehaviour(DolocTown.Config.EnvOptimizer.EnvOptimizerBehaviourType evt) | direct call candidate | Medium |
| DolocAPI.PerformEnvOptimizerBehaviour(DolocTown.Config.EnvOptimizer.EnvOptimizerBehaviourType evt; T data) | direct call candidate | Medium |
| DolocAPI.QueryAnimalDocument(System.String animalName; DolocTown.Config.Animal.AnimalDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.QueryBuilding(System.String name; DolocTown.Config.Building.BuildingInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryEquipment(System.String name; DolocTown.Config.Equipment.EquipmentInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryFactionJoinState(DolocTown.Config.Mission.FactionType factionType) | direct call candidate | Medium |
| DolocAPI.QueryFishDocument(System.String fishName; DolocTown.Config.Fishing.FishDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.QueryItemProto(System.String name; DolocTown.Config.Item.ItemInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryItemSpawnLut(System.String id; DolocTown.Config.Item.ItemSpawnInfo& spawnLut) | direct call candidate | Medium |
| DolocAPI.QueryMonsterDocument(System.String monsterId; DolocTown.Config.Monster.MonsterDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.QueryPlatformProto(System.String name; DolocTown.Config.Platform.PlatformInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryRecipeProto(System.String recipeName; DolocTown.Config.Recipe.RecipeInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryResourceDocument(System.String resourceName; DolocTown.Config.Resource.ResourceDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.QueryResourceProto(System.String name; DolocTown.Config.Resource.ResourceInfo& proto) | direct call candidate | Medium |
| DolocAPI.QuerySceneInfo(System.String name; DolocTown.Config.Room.SceneInfo& proto) | direct call candidate | Medium |
| DolocAPI.QuerySeedProto(System.String name; DolocTown.Config.Plant.SeedInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryVegetationProto(System.String name; DolocTown.Config.Resource.VegetationInfo& proto) | direct call candidate | Medium |
| DolocAPI.RaiseGhostShadow(UnityEngine.Vector3 ws; UnityEngine.Vector2 directionScale; UnityEngine.Sprite sprite; System.Boolean useScale; System.Single targetScale) | direct call candidate | Medium |
| DolocAPI.RaiseItemObtainTip(System.String itemName; UnityEngine.Sprite icon; System.String title; System.Int32 count; System.Boolean useSound) | direct call candidate | Medium |
| DolocAPI.RaiseSpriteArrayFadeDown(UnityEngine.Vector2 worldPosition; UnityEngine.Sprite[] icons; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RaiseSpriteArrayFadeUp(UnityEngine.Vector2 worldPosition; UnityEngine.Sprite[] icons; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance; System.Boolean flipX) | direct call candidate | Medium |
| DolocAPI.RaiseSpriteFadeDown(UnityEngine.Vector2 worldPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RaiseSpriteFadeUp(UnityEngine.Vector2 worldPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance; System.Boolean flipX) | direct call candidate | Medium |
| DolocAPI.RaiseSpriteFadeUp(UnityEngine.Vector2 worldPosition; System.String itemName; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance; System.Boolean flipX) | direct call candidate | Medium |
| DolocAPI.RaiseUiSpriteFadeDown(UnityEngine.Vector2 screenPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RaiseUiSpriteFadeUp(UnityEngine.Vector2 screenPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RefreshResourceByType(DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type) | direct call candidate | Medium |
| DolocAPI.RegisterMsgListener(DolocTown.Config.Settings.UserSettingType evtType; System.Action`2<System.Object,DolocTown.GameEventArgs> callback) | direct call candidate | Medium |
| DolocAPI.RollFish(System.String poolName; System.Int32 toolLv) | direct call candidate | Medium |
| DolocAPI.RollFishByRarity(System.String poolName; System.Int32 rarity) | direct call candidate | Medium |
| DolocAPI.SendCatchFishEvent(DolocTown.Config.Fishing.FishInfo fish) | direct call candidate | Medium |
| DolocAPI.SetResidentUiInteractable(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.ShowConfirmBox(DolocTown.Config.UI.AlignmentText title; DolocTown.Config.UI.AlignmentText content; DolocTown.Config.UI.AlignmentText signature; System.Action onConfirm) | direct call candidate | Medium |
| DolocAPI.ShowMessageBox(UnityEngine.Sprite icon; System.String msg; System.Single holdTime) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWithIconManual(System.String msg; UnityEngine.Sprite icon; UnityEngine.Vector2 pos; System.Single durShow; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWithIconWaiter(System.String msg; UnityEngine.Sprite icon; UnityEngine.Vector2 pos; System.Single durShow; System.Single durWiat; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxLarge(UnityEngine.Sprite icon; System.String msg; System.Single holdTime) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxNode(UnityEngine.Sprite icon; System.String msg) | direct call candidate | Medium |
| DolocAPI.ShowSceneTextBox(UnityEngine.Vector2 worldPosition; DolocTown.Config.Localization.SceneTextBoxArgs sceneTextArgs; UnityEngine.Transform followedTrans; System.Single duration) | direct call candidate | Medium |
| DolocAPI.ShowTextByConfig(System.String tipId) | direct call candidate | Medium |
| DolocAPI.TryGetAsset(System.String address; T& asset) | direct call candidate | Medium |
| DolocAPI.UnregisterMsgListener(DolocTown.Config.Settings.UserSettingType evtType; System.Action`2<System.Object,DolocTown.GameEventArgs> handler) | direct call candidate | Medium |
| DolocAssetCache.CheckAsset(System.String address) | direct call candidate | Medium; needs instance source |
| DolocAssetCache.GetAllAssetsOfType() | direct call candidate | Medium; needs instance source |
| DolocAssetCache.GetAsset(System.String address; System.Boolean useLog) | direct call candidate | Medium; needs instance source |
| DolocAssetCache.Load(System.Action callback) | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_bulletMovers() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_bullets() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_cache() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_cityRooms() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_dungeons() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_effectsConfig() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_gameConfig() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_gameProcessGraphs() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_lamps() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_missionChains() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_monsters() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_motionParam() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_npcSchedules() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_rooms() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_routineGraphs() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_skills() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_techTrees() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_WaterTemplates() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.LoadAsync(System.Action`1<System.Boolean> callback) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.BeginInvoke(System.AsyncCallback callback; System.Object object) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.EndInvoke(System.IAsyncResult result) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.Invoke() | direct call candidate | Medium; needs instance source |
| DolocConfigReader.parseIDLut(System.String source; System.Collections.Generic.Dictionary`2<System.String,System.Int32>& output) | direct call candidate | Medium |
| DolocGameAssetsPatch.CreateEntity(DolocGameAssets assetId; UnityEngine.Transform parent) | direct call candidate | Medium |
| DolocGameAssetsPatch.CreateEntity(DolocGameAssets assetId; UnityEngine.Transform parent) | direct call candidate | Medium |
| DolocGameAssetsPatch.CreateNashPool(DolocGameAssets assetsId; UnityEngine.Transform container; System.Int32 frequency) | direct call candidate | Medium |
| DolocGameAssetsPatch.CreatePool(DolocGameAssets assetsId; UnityEngine.Transform container; System.Boolean usePreset) | direct call candidate | Medium |
| DolocGameAssetsPatch.LoadMaterial(DolocGameAssets assetId) | direct call candidate | Medium |
| DolocIcon.get_backgroundSprite() | direct call candidate | Medium; needs instance source |
| DolocIcon.get_iconSprite() | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeDown() | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeDown(UnityEngine.Sprite sprite) | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DebugLight.lightEmissionTex | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DebugLight.sr | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<assets>k__BackingField | DolocBundleManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<eftConfig>k__BackingField | DolocTown.GameServiceLocator.IEffectsConfigProvider | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<gameConfig>k__BackingField | DolocTown.GameServiceLocator.IGameConfigProvider | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.messageSystem | DolocTown.MessageSystem`1<DolocTown.Config.Settings.UserSettingType> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>7__wrap1 | System.Collections.Generic.List`1/Enumerator<DolocTown.Config.Item.ItemSpawnData> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>7__wrap2 | System.Collections.Generic.List`1/Enumerator<DolocTown.Config.Animal.AnimalHusbandryData> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache.assets | System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Object>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache.label | UnityEngine.AddressableAssets.AssetLabelReference | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<>2__current | System.Object | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<handle>5__2 | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<bulletMovers>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.BulletMoverProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<bullets>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.BulletProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<cache>k__BackingField | DolocAssetCache | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<cityRooms>k__BackingField | DolocTown.GameData.CityRoomDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<dungeons>k__BackingField | DolocTown.GameData.DungeonDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<effectsConfig>k__BackingField | DolocTown.GameData.DRockEffectsConfig | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<gameConfig>k__BackingField | DolocTown.GameData.GameConfigSO | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<gameProcessGraphs>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.GameProcessGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<lamps>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.LampProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<missionChains>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.MissionGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<monsters>k__BackingField | DolocTown.GameData.MonsterDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<motionParam>k__BackingField | DolocTown.GameData.MotionParamSO | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<npcSchedules>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.NpcScheduleGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<rooms>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.RoomProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<routineGraphs>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.RoutineGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<skills>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.SkillProto> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<techTrees>k__BackingField | DolocTown.GameData.TechTreeDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<WaterTemplates>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.GameData.WaterParamSO> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.preloadLabel | UnityEngine.AddressableAssets.AssetLabelReference | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<>2__current | System.Object | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<>7__wrap2 | DolocBundleManager/DataLoader[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<>7__wrap3 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<loader>5__5 | DolocBundleManager/DataLoader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<valid>5__2 | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.<asset>k__BackingField | UnityEngine.InputSystem.InputActionAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.configuredAchievements | System.Collections.Generic.Dictionary`2<System.String,DolocTown.NodeCanvas.MissionNodeListener> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ActionIconGroup.largeIconAsset | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ActionIconGroup.smallIconAsset | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Affector.func | DolocTown.Config.Equipment.EquipmentFuncAffector | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.interactableManager | DolocTown.InteractableManagerEx | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.interactableScanner | DolocTown.ScannerInteractable | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.interactableScannerOfMotor | DolocTown.ScannerInteractableOfMotor | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.proto | DolocTown.Config.Player.AgentEquipmentFuncProto | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.skill | DolocTown.Config.Player.AgentEquipmentSkillInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionAreaSpeedUp._func | DolocTown.Config.Player.AgentEquipmentFuncProtoAreaSpeedUp | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionCounterBack._func | DolocTown.Config.Player.AgentEquipmentFuncProtoCounterBack | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionDashCdCooler._func | DolocTown.Config.Player.AgentEquipmentFuncProtoDashCdCooler | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionDisguise._func | DolocTown.Config.Player.AgentEquipmentFuncProtoDisguise | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionExtraResource.protoExtraResource | DolocTown.Config.Player.AgentEquipmentFuncProtoExtraResource | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFellCountAdditionOre._func | DolocTown.Config.Player.AgentEquipmentFuncProtoFellCountAdditionOre | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFoodEffectsAddition._func | DolocTown.Config.Player.AgentEquipmentFuncProtoFoodEffectsAddition | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionHerbPackage._func | DolocTown.Config.Player.AgentEquipmentFuncProtoHerbPackage | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionIncreaseCriticalRate._func | DolocTown.Config.Player.AgentEquipmentFuncProtoIncreaseCriticalRate | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionLightningConductor._func | DolocTown.Config.Player.AgentEquipmentFuncProtoLightningConductor | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShepherd._func | DolocTown.Config.Player.AgentEquipmentFuncProtoShepherd | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShield._func | DolocTown.Config.Player.AgentEquipmentFuncProtoShield | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatAnimator._spriteRenderer | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRenderer._currentHatRenderInfo | DolocTown.Config.Player.HatInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRenderer._currentRevertSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRenderer._currentSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRenderer._useSprite | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.mainSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.maskSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.revertMainSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.revertMaskSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet._lastDayPeriodType | DolocTown.Config.Time.DayPeriodType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet._lightRenderers | UnityEngine.SpriteRenderer[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.mainSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.maskSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.revertMainSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.revertMaskSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AirWall.spriteRenderer | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalDecorator.func | DolocTown.Config.Equipment.EquipmentFuncAnimalDecorator | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalRenderer.<_spriteRenderer>k__BackingField | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalTask_HandleInteractable._interactable | DolocTown.IAnimalInteractable | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalUtils._chicknestProto | DolocTown.Config.Equipment.EquipmentInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate._portalProto | DolocTown.Config.Room.PortalInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate.boardingPoint | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate.movableTarget | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate.waitingPoint | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetArrayBase`2._assets | TAsset[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetArrayBase`2.<refs>k__BackingField | TRef[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetArrayBase`2.loaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetBase`1._asset | T | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetBase`1._isLoaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetBase`1.<AssetUrl>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotChargeRenderer.<Sr>k__BackingField | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerFarming._func | DolocTown.Config.Automate.AutomateBotFunctionFarming | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotRenderer.<Sr>k__BackingField | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotStation.func | DolocTown.Config.Equipment.EquipmentFuncAutomateBotStation | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BackgroundLayerRenderer._spriteRenderer | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BackpackUpgradeUiState.currentLevelProto | DolocTown.Config.Player.BackpackLevelInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BackpackUpgradeUiState.nextLevelProto | DolocTown.Config.Player.BackpackLevelInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Barrel.func | DolocTown.Config.Equipment.EquipmentFuncBarrel | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Battery.lastSpriteIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BattleUtils._isBattleConfigLoaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.__LoadStaticAssets | Harmony patch candidate | System.Action callback | Medium |
| DolocAPI.Command_LockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.Command_UnlockAllInteractableObject | Harmony patch candidate |  | Risky |
| DolocAPI.Command_UnlockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.FastButton_ReloadConfigTables | Harmony patch candidate |  | Risky |
| DolocAPI.LoadSprite | Harmony patch candidate | System.String imageName | Medium |
| DolocAPI.RefreshResourceByType | Harmony patch candidate | DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type | Medium |
| DolocAPI.SetResidentUiInteractable | Harmony patch candidate | System.Boolean value | Medium |
| DolocAPI/<>c__DisplayClass37_0.<__LoadStaticAssets>b__0 | Harmony patch candidate | System.Boolean succeed | Risky |
| DolocAPI/<>c__DisplayClass760_0.<RefreshResourceByType>b__0 | Harmony patch candidate | DolocTown.Config.Resource.ResourceSpawnData x | Risky |
| DolocAssetCache.Load | Harmony patch candidate | System.Action callback | Medium |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | Harmony patch candidate | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> h | Risky |
| DolocBundleManager.LoadAsync | Harmony patch candidate | System.Action`1<System.Boolean> callback | Medium |
| DolocBundleManager.LoadBulletMovers | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadBullets | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadCityRooms | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadDungeons | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadEffects | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadGameParamSettings | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadGameProcessGraphs | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadLamps | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadLubanConfigs | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadMissionChains | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadMonsters | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadNpcSchedules | Harmony patch candidate |  | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IContentHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocAssetCache | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocAssetCache/<>c__8`1 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocAssetCache/<>c__DisplayClass3_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocAssetCache/<Load>d__3 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocBundleManager/<InitDataAsync>d__76 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocBundleManager/DataLoader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocConfigReader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocGameAssets | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocGameAssetsPatch | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AnimalTask_HandleInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AnimatorAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AssetArrayBase`2 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AssetBase`1 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalHusbandryData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalLevelData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalState | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.AnimalStateInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.FeedInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.HusbandryEnergyInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.HusbandryInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbAnimal | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbAnimalDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbAnimalState | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbFeed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbHusbandry | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbHusbandry/ContributionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Animal.TbHusbandryEnergy | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.CharacterDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.ChipDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.ChipEvent | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.ChipEventType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.DocumentMissionTip | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.DocumentNodeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.DocumentType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.PlantDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.SpecialChipEventInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.TbCharacterDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.TbChipDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.TbPlantDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.TbSpecialChipEvent | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ArrayInt | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgAnimatorAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgAssetBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgEffectGroupAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgFontAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgMaterialAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgNpcScheduleAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IContentHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- SMAPI 1.0 should read content; dynamic patching belongs to a later stage.
