# DolocTown Resource Gathering API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map harvest and drop generation.
- Find item-spawn hooks for analytics and content packs.
- Separate world resources from farm crops.

## Decompiled scope

- Matched types: 270
- Matched methods: 3534
- Matched fields: 1497
- Matched properties: 1017
- Matched events: 0
- Matched call edges: 13916
- Matched strings: 1512
- Raw indexes: `maps/index/Resource_Gathering-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.GameData.DRockEffectsConfig | class | Confirmed | visibility=public; methods=47; fields=46 |
| DolocTown.DungeonResource | class | Confirmed | visibility=public; methods=68; fields=11 |
| DolocTown.Config.Resource.ResourceInfo | class | Confirmed | visibility=public; methods=44; fields=15 |
| DolocTown.StoreUiState | class | Confirmed | visibility=public; methods=49; fields=8 |
| DolocTown.Store | class | Confirmed | visibility=public; methods=48; fields=8 |
| DolocTown.DungeonResourceRenderer | class | Confirmed | visibility=public; methods=43; fields=8 |
| DolocTown.Config.Resource.ResourceDocumentInfo | class | Confirmed | visibility=public; methods=29; fields=14 |
| DolocTown.Vegetation | class | Confirmed | visibility=public; methods=40; fields=3 |
| DolocTown.UI.TechTreeWidget | class | Confirmed | visibility=public; methods=23; fields=19 |
| DolocTown.Config.Store.StoreInfo | class | Confirmed | visibility=public; methods=30; fields=11 |
| DolocTown.Config.TechTree.TechPointInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.ExchangeStore | class | Confirmed | visibility=public; methods=36; fields=5 |
| DolocTown.Config.Store.ExchangeStoreItemData | class | Confirmed | visibility=public; methods=30; fields=10 |
| DolocTown.PlantBasinTree | class | Confirmed | visibility=public; methods=37; fields=3 |
| DolocTown.TreeCrop | class | Confirmed | visibility=public; methods=29; fields=11 |
| DolocTown.Config.Plant.TreeSeedInfo | class | Confirmed | visibility=public; methods=29; fields=10 |
| DolocTown.Config.Resource.VegetationInfo | class | Confirmed | visibility=public; methods=28; fields=10 |
| DolocTown.TechTreeUiState | class | Confirmed | visibility=public; methods=34; fields=4 |
| DolocTown.Config.Resource.GlobalGuaranteedInfo | class | Confirmed | visibility=public; methods=25; fields=10 |
| DolocTown.Config.Resource.ResourceLevelData | class | Confirmed | visibility=public; methods=26; fields=9 |
| DolocTown.VegetationRenderer | class | Confirmed | visibility=public; methods=29; fields=4 |
| DolocTown.IDungeonResourceHost | interface | Confirmed | visibility=public; methods=32; fields=0 |
| DolocTown.Config.Store.ExchangeStoreInfo | class | Confirmed | visibility=public; methods=22; fields=9 |
| DolocTown.Config.Resource.ResinCollectorOutputInfo | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.Config.Resource.VegetationFuncLuminous | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.DungeonResourceWeeds | class | Confirmed | visibility=public; methods=25; fields=3 |
| DolocTown.AffectorElectric | class | Confirmed | visibility=public; methods=23; fields=4 |
| DolocTown.Config.Store.StoreItemSeasonData | class | Confirmed | visibility=public; methods=21; fields=6 |
| DolocTown.Config.TechTree.TechTreeInfo | class | Confirmed | visibility=public; methods=19; fields=8 |
| DolocTown.UI.ResourceDetailData | struct | Confirmed | visibility=public; methods=14; fields=13 |
| DolocTown.Config.Resource.EnvObjectInfo | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.Config.Resource.ResourceSpawnInfo | class | Confirmed | visibility=public; methods=20; fields=6 |
| DolocTown.Config.TechTree.TechNodeInfo | class | Confirmed | visibility=public; methods=18; fields=8 |
| DolocTown.UI.DropItemPickTip | class | Confirmed | visibility=public; methods=10; fields=16 |
| DolocTown.Config.Resource.VegetationSpawnInfo | class | Confirmed | visibility=public; methods=19; fields=6 |
| DolocTown.IDropItemHost | interface | Confirmed | visibility=public; methods=25; fields=0 |
| DolocTown.Config.Resource.ResourceTypeInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.Config.Store.StoreItemUnlockInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.RaindropCollision | class | Confirmed | visibility=public; methods=9; fields=14 |
| DolocTown.Config.Resource.EnvObjectSpawnInfo | class | Confirmed | visibility=public; methods=16; fields=6 |
| DolocTown.DungeonResourceManager | class | Confirmed | visibility=public; methods=18; fields=4 |
| DolocTown.DungeonResourceTree | class | Confirmed | visibility=public; methods=18; fields=3 |
| DolocTown.TreeCropRenderer | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.VegetationGrow | class | Confirmed | visibility=public; methods=17; fields=4 |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Player.AgentEquipmentFuncProtoExtraResource | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.GarbageShredder/<SpawnItems>d__38 | class | Confirmed | visibility=private; methods=8; fields=11 |
| DolocTown.TreeGraph.TreeGraph`1 | class | Confirmed | visibility=public; methods=11; fields=8 |
| DolocTown.Config.Mod.ModStoreExtensionInfo | class | Confirmed | visibility=public; methods=13; fields=5 |
| DolocTown.Config.Resource.EnvObjectSpawnEntry | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.Config.Resource.ResourceSpawnEntry | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.Config.Resource.VegetationSpawnEntry | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.DropItemBase | class | Confirmed | visibility=public; methods=15; fields=3 |
| DolocTown.DropItemRenderer | class | Confirmed | visibility=public; methods=12; fields=6 |
| DolocTown.ResourceManager | class | Confirmed | visibility=public; methods=15; fields=3 |
| DolocTown.VegetationGrowLuminous | class | Confirmed | visibility=public; methods=16; fields=2 |
| DolocTown.VegetationLuminous | class | Confirmed | visibility=public; methods=15; fields=3 |
| RedSaw.CommandLineInterface.SyntaxTreeCode | enum | Confirmed | visibility=public; methods=0; fields=18 |
| DolocTown.AutomateBotDecisionMakerGathering | class | Confirmed | visibility=public; methods=15; fields=2 |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11 | class | Confirmed | visibility=private; methods=11; fields=6 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_OpenExchangeStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshResource | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshResourceByType | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_RefreshStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshVegetation | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenSeedStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenStore | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshResourceByType | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshStore | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshWorldVegetation | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass680_0.<OpenStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass760_0.<RefreshResourceByType>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass927_0.<Command_OpenExchangeStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadTechTrees | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocResources.loadConfig | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocTown.AffectorElectric.<OnInteract>b__26_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.UpdateNoInterval | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.UpdateNoIntervalNoRender | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.UpdateNoRender | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.UpdateSwitchIcon | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDrop.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDrop.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.UpdateNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalSystem.UpdateNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotStation.UpdateNoRender | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateParamGathering.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.Barrel.UpdateNoRender | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BTRunner.StartBT | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuffManager.UpdatePerTUNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingManager.UpdateNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionBookUiState.RefreshResourceList | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionManager.RefreshResourceRecord | candidate lifecycle or hook point | Medium |
| DolocTown.CollectorDropItem.Init | candidate lifecycle or hook point | Medium |
| DolocTown.CollectorDropItem.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.CollectorDropItem.OnTriggerEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectorDropItem.OnTriggerExit2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Fishing.FishInfo.get_BonusExtraScore | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Fishing.FishInfo.set_BonusExtraScore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_TechtreeNodeUnopen | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_TechtreeNodeUnopen_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_TechtreeNodeUnopen | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_TechtreeNodeUnopen | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.LoadStreetPoints | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo/<>c.<LoadStreetPoints>b__115_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo/<>c.<LoadStreetPoints>b__115_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.ExchangeStoreInfo.get_RefreshImmediatly | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.ExchangeStoreInfo.set_RefreshImmediatly | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.ExchangeStoreItemData.get_UnlockOnInit | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoney | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoneyRange | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.set_InitMoney | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.StoreInfo.set_InitMoneyRange | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.TechTree.TechPointInfo.get_UseLevelTip | candidate lifecycle or hook point | Medium |
| DolocTown.Config.TechTree.TechPointInfo.set_UseLevelTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DropItemRenderer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DropItemRenderer.FixedUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Dungeon.UpdateNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.Dungeon.UpdatePerHourNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.get_CanInteractContinues | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.get_DisableAfterInteract | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.get_PositionCenter | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.InitRandomGrowth | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.RefreshSubRenderers | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DungeonResourceCrop.<OnInteract>b__6_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DungeonResourceCrop.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResourceManager.RefreshCounterInterval | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResourceModelPaperBox.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResourceRenderer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DungeonResourceRenderer.get_CanInteractContinues | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResourceRenderer.OnDestroy | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DungeonResourceRenderer.OnInteract | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| <>f__AnonymousType0`2.get_score() | direct call candidate | Medium; needs instance source |
| DolocAPI.AddTechExp(DolocTown.Config.TechTree.TechPointType type; System.Int32 exp) | direct call candidate | Medium |
| DolocAPI.AddTechPoint(DolocTown.Config.TechTree.TechPointType type; System.Int32 pt) | direct call candidate | Medium |
| DolocAPI.CheckResourceUnlocked(System.String resourceId) | direct call candidate | Medium |
| DolocAPI.CheckVegetationUnlocked(System.String vegetationId) | direct call candidate | Medium |
| DolocAPI.Command_RefreshResourceByType(System.String type; System.String roomId) | direct call candidate | Medium |
| DolocAPI.CostTechPoint(System.Int32 count; DolocTown.Config.TechTree.TechPointType type) | direct call candidate | Medium |
| DolocAPI.GenerateDropItem(DolocTown.IDropItemHost host; DolocTown.Item item; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; System.Int32 count; UnityEngine.Vector2 startPos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; System.String itemName; UnityEngine.Vector2 pos; System.Int32 count; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.CountItem countItem; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GetTechTreeTitle(System.String treeName) | direct call candidate | Medium |
| DolocAPI.JumpTechTreeNode(System.String treeId; System.String nodeName) | direct call candidate | Medium |
| DolocAPI.OpenStore(System.String storeId) | direct call candidate | Medium |
| DolocAPI.PlaceInBackpackOrGenerateDropItem(DolocTown.Item item; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.QueryResourceDocument(System.String resourceName; DolocTown.Config.Resource.ResourceDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.QueryResourceProto(System.String name; DolocTown.Config.Resource.ResourceInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryVegetationProto(System.String name; DolocTown.Config.Resource.VegetationInfo& proto) | direct call candidate | Medium |
| DolocAPI.RefreshResourceByType(DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type) | direct call candidate | Medium |
| DolocAPI.RefreshStore(System.String storeName) | direct call candidate | Medium |
| DolocAPI.SetDialogueTargetForeground(System.String targetName; System.Boolean active) | direct call candidate | Medium |
| DolocAPI.SpawnItems(System.String itemName; UnityEngine.Vector2Int countRange) | direct call candidate | Medium |
| DolocAPI.SpawnMonsterDropItems(DolocTown.Monster monster) | direct call candidate | Medium |
| DolocAPI.SpawnResourceDropItems(DolocTown.DungeonResource resource; System.Boolean isRender; System.String overrideSpawnLut) | direct call candidate | Medium |
| DolocAPI.UnlockStoreItem(System.String storeName; System.String itemName) | direct call candidate | Medium |
| DolocBundleManager.get_effectsConfig() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_techTrees() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleTechTree() | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnToggleTechTree(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/INormalInputActions.OnToggleTechTree(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/NormalInputActions.get_ToggleTechTree() | direct call candidate | Medium; needs instance source |
| DolocResources.get_defaultNpcController() | direct call candidate | Medium |
| DolocResources.getConfigSource(System.String name) | direct call candidate | Medium |
| DolocResources.loadConfig(System.String path; System.String& source) | direct call candidate | Medium |
| DolocResources.loadSpriteAtlas(System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Action`1<System.String> log) | direct call candidate | Medium |
| DolocResources.loadSpriteAtlas(System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Func`2<System.String,System.String> handleKey; System.Action`1<System.String> log) | direct call candidate | Medium |
| DolocResources.readConfig(System.String name; System.String& source) | direct call candidate | Medium |
| DolocTown.AffectorElectric.get_IsTurnOn() | direct call candidate | Medium; needs instance source |
| DolocTown.AffectorElectric.get_IsWorking() | direct call candidate | Medium; needs instance source |
| DolocTown.AffectorElectric.get_PositionSwitch() | direct call candidate | Medium; needs instance source |
| DolocTown.AffectorElectric.get_PositionTip() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentDropDownChecker.Check(UnityEngine.GameObject other) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionExtraResource.OnReceiveMessage(DolocTown.GameMessage message) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionFellCountAdditionOre.DoExtraConfig(DolocTown.Params.AgentEquipmentParams agentEquipmentParams) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentPhysicalStatus.Drop(System.Single moveSpeed) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentPhysicalStatus.get_IsDrop() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateDrop.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateDrop.OnExit() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateDrop.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_PositionDropItem() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.UpdateNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalRoomEnv.GetRandomEmptyPositionForEquipment(UnityEngine.Vector2Int coverSize; UnityEngine.Vector2Int& anchor) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalSystem.BeforePassTime() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalSystem.UpdateNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotRenderer.RestoreMove(UnityEngine.Vector2 velocityDir) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.FilterContainer(DolocTown.Case container) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.get_GatherCrop() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.get_GatherDropItems() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.get_GatherEquipmentItems() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.set_GatherCrop(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.set_GatherDropItems(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.set_GatherEquipmentItems(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateStationEnv/RoomEnv.get_AllDropItems() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GenerateDropItem(System.String itemName; UnityEngine.Vector2 pos; System.String roomGuid) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.get_AllDropItems() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAllDropItemsInRoom(System.String roomGuid) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAnyDropItem(System.String roomGuid; System.Boolean locked) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAnyDropItem(System.String roomGuid; DolocTown.Config.Item.ItemMainTypeInfo mainType; System.Boolean locked) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAnyDropItem(System.Boolean locked) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAnyDropItemNearStation(DolocTown.AutomateBotStation station; System.String mainType; System.Boolean locked) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetAnyDropItemNearStation(DolocTown.AutomateBotStation station; System.Boolean locked) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetDropItemsNearStation(DolocTown.AutomateBotStation station) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.HasAnyDropItem(System.String roomGuid; DolocTown.Config.Item.ItemMainTypeInfo type) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.HasAnyDropItem() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.HasAnyDropItem(System.String roomGuid) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.RemoveDropItem(DolocTown.DropItemBase item; System.String roomGuid) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.IsLocked(DolocTown.DropItemBase item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.IsUnlocked(DolocTown.DropItemBase item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.LockDropItem(DolocTown.AutomateBot bot; DolocTown.DropItemBase item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.UnlockDropItem(DolocTown.DropItemBase item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskGatherEquipment.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskHarvestCrop.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.Barrel.AffectNoRender(DolocTown.IAffector affector) | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.Drop() | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.SetToForeground(System.Boolean active) | direct call candidate | Medium; needs instance source |
| DolocTown.BuffManager.UpdatePerTUNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingManager.UpdateNoRender() | direct call candidate | Medium; needs instance source |
| DolocTown.Bullet.get_AffectedResourceTypes() | direct call candidate | Medium; needs instance source |
| DolocTown.Bullet.get_AffectResource() | direct call candidate | Medium; needs instance source |
| DolocTown.Bullet.SetResourceInfos(System.Collections.Generic.HashSet`1<System.Byte> affectResourceNames; System.Int32 toolLevel; System.Int32 chopCount) | direct call candidate | Medium; needs instance source |
| DolocTown.BulletMoverDrop.ConfigureFireInfo(UnityEngine.Vector2 pos; UnityEngine.Vector2 dir) | direct call candidate | Medium; needs instance source |
| DolocTown.BulletMoverDrop.Move(System.Single deltaTime) | direct call candidate | Medium; needs instance source |
| DolocTown.Case.IsAutomateLabelMatch(DolocTown.DropItemBase dropItem) | direct call candidate | Medium; needs instance source |
| DolocTown.CharacterRenderer.SetToForeground(System.Boolean active) | direct call candidate | Medium; needs instance source |
| DolocTown.ChickenNest.Gather() | direct call candidate | Medium; needs instance source |
| DolocTown.ChickenNest.get_IsGatherable() | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.GetResourceDocumentCount() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| <>f__AnonymousType0`2.<score>i__Field | <score>j__TPar | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<host>5__2 | DolocTown.IDropItemHost | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<i>5__6 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<moneyPerDrop>5__3 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<pos>5__5 | UnityEngine.Vector3 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<remainingMoney>5__4 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<handle>5__2 | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<effectsConfig>k__BackingField | DolocTown.GameData.DRockEffectsConfig | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<techTrees>k__BackingField | DolocTown.GameData.TechTreeDatabase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleTechTree | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ToggleTechTree | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AffectorElectric.appliance | DolocTown.ElectronicComponentAppliance | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AffectorElectric.isIdle | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AffectorElectric.isTurnOn | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AffectorElectric.workCounter | RedSaw.Counter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.collectorDropItem | DolocTown.CollectorDropItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionExtraResource.protoExtraResource | DolocTown.Config.Player.AgentEquipmentFuncProtoExtraResource | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionExtraResource.resourceIds | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFellCountAdditionOre._func | DolocTown.Config.Player.AgentEquipmentFuncProtoFellCountAdditionOre | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentPhysicalStatus.maxDropSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateDrop._drop_duration_threshold | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerGathering._idleMode | DolocTown.AutomateBotDecisionMaker/IdleMode | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerGathering._param | DolocTown.AutomateParamGathering | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.containerSkinSet | System.Collections.Generic.HashSet`1<System.Int32> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.filterContainerSkin | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherCrop | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherDropItems | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherEquipmentItems | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>2__current | DolocTown.DropItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv.lockedDropItems | System.Collections.Generic.HashSet`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>7__wrap2 | System.Collections.Generic.IEnumerator`1<DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.roomGuid | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>7__wrap2 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<room>5__2 | DolocTown.TemplateRoom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.station | DolocTown.AutomateBotStation | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker.dropItemLocker | DolocTown.AutomateLocker`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker.lockedDropItems | System.Collections.Generic.HashSet`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskGatherEquipment._equipment | DolocTown.Equipment | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskHarvestCrop.basin | DolocTown.PlantBasin | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskPickupItem.dropItem | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController.isForegroundInDialogue | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController.maxDropSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BTRunner._btOwner | NodeCanvas.BehaviourTrees.BehaviourTreeOwner | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.doorEquipments | System.Collections.Generic.List`1<DolocTown.Equipment> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.doorEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bullet.<AffectedResourceTypes>k__BackingField | System.Collections.Generic.HashSet`1<System.Byte> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bullet.<AffectResource>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BulletMoverDrop._simulator | RedSaw.Physical.PhysicalSimulator | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BulletMoverDrop.gravity | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState.resourceList | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.dropitems | System.Collections.Generic.List`1<DolocTown.DropItemRenderer> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.maxMoveSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.minMoveSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.pickDistance | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.radiusReciprocal | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoBrustCore.<BulletCount>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoBrustCore.<BulletInterval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoCollectorHelper.<Types>k__BackingField | DolocTown.Config.Resource.DungeonResourceType[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoRestrictionReleaser.<DamageIncrease>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneFunctionProtoRestrictionReleaser.<PowerCostIncrease>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.ChairInfo.<Foreground>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.ChairInfo.<ForegroundFlip>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.<StoreName_Ref>k__BackingField | DolocTown.Config.Store.StoreInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.<StoreName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinTree.<FertilizerOffset>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncResinCollector.<DefaultOutput_Ref>k__BackingField | DolocTown.Config.Resource.ResinCollectorOutputInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncShowCase.<ForegroundSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusBaseScore>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusExtraScore>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionFertilizer.<IsTree>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRandomPackage.<DropSpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnEntry | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedTree.<TreeSeedId_Ref>k__BackingField | DolocTown.Config.Plant.TreeSeedInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedTree.<TreeSeedId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass.<SpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnRandom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass.<TargetLevels>k__BackingField | System.Int32[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass.<TargetResourceClass>k__BackingField | DolocTown.Config.Resource.DungeonResourceClass | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName.<SpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnRandom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName.<TargetLevels>k__BackingField | System.Int32[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName.<TargetResourceName_Ref>k__BackingField | DolocTown.Config.Resource.ResourceInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName.<TargetResourceName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<ExtraSpawnLutsByClass>k__BackingField | DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<ExtraSpawnLutsByName>k__BackingField | DolocTown.Config.Item.ToolExtraSpawnLutByResourceName[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideLevel.<TargetResourceClass>k__BackingField | DolocTown.Config.Resource.DungeonResourceClass | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideSpawnLut.<TargetResourceClass>k__BackingField | DolocTown.Config.Resource.DungeonResourceClass | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoxPanelNoRepairCost_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.Command_RefreshResource | Harmony patch candidate | System.String roomId | Risky |
| DolocAPI.Command_RefreshResourceByType | Harmony patch candidate | System.String type; System.String roomId | Medium |
| DolocAPI.Command_RefreshStore | Harmony patch candidate | System.String storeName | Risky |
| DolocAPI.Command_RefreshVegetation | Harmony patch candidate | System.String sceneName | Risky |
| DolocAPI.FastButton_OpenSeedStore | Harmony patch candidate |  | Risky |
| DolocAPI.RefreshResourceByType | Harmony patch candidate | DolocTown.Room room; DolocTown.Config.Resource.DungeonResourceType type | Medium |
| DolocAPI.RefreshStore | Harmony patch candidate | System.String storeName | Medium |
| DolocAPI.RefreshWorldVegetation | Harmony patch candidate |  | Risky |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | Harmony patch candidate | DolocTown.StoreUiState state | Risky |
| DolocAPI/<>c__DisplayClass760_0.<RefreshResourceByType>b__0 | Harmony patch candidate | DolocTown.Config.Resource.ResourceSpawnData x | Risky |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | Harmony patch candidate | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> h | Risky |
| DolocBundleManager.LoadTechTrees | Harmony patch candidate |  | Risky |
| DolocResources.loadConfig | Harmony patch candidate | System.String path; System.String& source | Medium |
| DolocResources.loadSpriteAtlas | Harmony patch candidate | System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Func`2<System.String,System.String> handleKey; System.Action`1<System.String> log | Medium |
| DolocResources.loadSpriteAtlas | Harmony patch candidate | System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Action`1<System.String> log | Medium |
| DolocTown.AffectorElectric.<OnInteract>b__26_0 | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.OnInteract | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.Update | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.UpdateNoInterval | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.UpdateNoIntervalNoRender | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.UpdateNoRender | Harmony patch candidate |  | Risky |
| DolocTown.AffectorElectric.UpdateSwitchIcon | Harmony patch candidate |  | Risky |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateDrop.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateDrop.OnExit | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IResourceHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Automate.AutomateBotFunctionGathering | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Drone.DroneFunctionProtoBrustCore | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Drone.DroneFunctionProtoRestrictionReleaser | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinTree | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionSeedTree | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModResourceSpawnExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModStoreExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModVegetationSpawnExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModExchangeStoreExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModResourceSpawnExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModStoreExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModVegetationSpawnExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoFlorescenceExtend | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoSporeSpray | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.TbTreeSeed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.TreeSeedInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Player.AgentEquipmentFuncProtoExtraResource | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Player.AgentEquipmentFuncProtoFellCountAdditionOre | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.DungeonResourceClass | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.DungeonResourceType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.EnvObjectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.EnvObjectSpawnEntry | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.EnvObjectSpawnInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.EnvObjectSpawnInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.EnvObjectType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.GlobalGuaranteedInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.GuaranteedType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResinCollectorOutputInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceDocumentType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceLevelData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnEntry | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnInfo/<>c__DisplayClass28_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnInfo/<>c__DisplayClass29_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceSpawnInfo/<>c__DisplayClass30_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.ResourceTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbEnvObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbEnvObjectSpawn | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbGlobalGuaranteed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbResinCollectorOutput | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbResource | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbResourceDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Resource.TbResourceSpawn | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IResourceHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Drop tables should be cross-checked with official JSON configs.
