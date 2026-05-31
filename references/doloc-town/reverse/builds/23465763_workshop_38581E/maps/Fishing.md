# DolocTown Fishing API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Support fishing-state queries and events.
- Validate automation and fish-info mods.
- Keep minigame bypasses marked risky.

## Decompiled scope

- Matched types: 102
- Matched methods: 1302
- Matched fields: 576
- Matched properties: 394
- Matched events: 1
- Matched call edges: 4639
- Matched strings: 577
- Raw indexes: `maps/index/Fishing-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Config.Fishing.FishInfo | class | Confirmed | visibility=public; methods=73; fields=29 |
| DolocTown.FarmFishTank | class | Confirmed | visibility=public; methods=51; fields=9 |
| DolocTown.FishTankEcological | class | Confirmed | visibility=public; methods=45; fields=10 |
| DolocTown.FishTank | class | Confirmed | visibility=public; methods=46; fields=6 |
| DolocTown.FishRodRenderer | class | Confirmed | visibility=public; methods=25; fields=18 |
| DolocTown.FishTankUiState | class | Confirmed | visibility=public; methods=32; fields=7 |
| DolocTown.Config.Fishing.FarmFishInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.FishingGameScrollBar | class | Confirmed | visibility=public; methods=14; fields=24 |
| DolocTown.FishTankElectricEel | class | Confirmed | visibility=public; methods=32; fields=6 |
| DolocTown.FishRodHook | class | Confirmed | visibility=public; methods=24; fields=12 |
| DolocTown.Config.Fishing.FarmFishFormationInfo | class | Confirmed | visibility=public; methods=22; fields=7 |
| DolocTown.Config.Item.ItemFunctionFishingRod | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.Config.Fishing.FishDocumentInfo | class | Confirmed | visibility=public; methods=18; fields=8 |
| DolocTown.FishIncubator | class | Confirmed | visibility=public; methods=20; fields=5 |
| DolocTown.Config.Fishing.FishingPoolInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.FishingGameDoubleCircle | class | Confirmed | visibility=public; methods=13; fields=10 |
| DolocTown.UI.FishingNoteBar | class | Confirmed | visibility=public; methods=9; fields=13 |
| DolocTown.ItemFishFry | class | Confirmed | visibility=public; methods=17; fields=4 |
| DolocTown.UI.FishDetailData | struct | Confirmed | visibility=public; methods=11; fields=10 |
| DolocTown.Config.Mod.ModFishingPoolExtensionInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.FishRodLine | class | Confirmed | visibility=public; methods=8; fields=12 |
| DolocTown.AgentStateFishing | class | Confirmed | visibility=public; methods=18; fields=1 |
| DolocTown.UI.FishingTurntable | class | Confirmed | visibility=public; methods=10; fields=9 |
| DolocTown.AgentStateFishingWait | class | Confirmed | visibility=public; methods=10; fields=8 |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.FishingCache | class | Confirmed | visibility=public; methods=13; fields=5 |
| DolocTown.ItemFishRoe | class | Confirmed | visibility=public; methods=15; fields=3 |
| DolocAPI/<GetAnimalAllProduce>d__262 | class | Confirmed | visibility=private; methods=10; fields=7 |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Fishing.FishFeedInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Fishing.FishFormationCondition | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.UI.FishingPanel | class | Confirmed | visibility=public; methods=13; fields=3 |
| DolocTown.FarmFishShoal | class | Confirmed | visibility=public; methods=12; fields=3 |
| DolocTown.FishShadow | class | Confirmed | visibility=public; methods=7; fields=8 |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Equipment.EquipmentFuncFishTankExtension | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Player.AgentEquipmentFuncProtoFishTank | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.FarmFishTank/<GetProductItems>d__71 | class | Confirmed | visibility=private; methods=9; fields=5 |
| DolocTown.FishGameRendererDoubleCircle | class | Confirmed | visibility=public; methods=7; fields=6 |
| DolocTown.Config.Fishing.TbFarmFish | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.FishingNoteSpawner | class | Confirmed | visibility=public; methods=4; fields=8 |
| DolocTown.UI.FishingNoteBar/<SpawnNotes>d__13 | class | Confirmed | visibility=private; methods=6; fields=6 |
| DolocTown.AgentStateFishingWait/<>c | class | Confirmed | visibility=private; methods=6; fields=5 |
| DolocTown.Config.Equipment.EquipmentFuncFishTank | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.Config.Fishing.TbFishFeed | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.Config.Plant.CropGeneFuncProtoImmortalJellyfish | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.AgentStateFishingPull | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.AgentStateFishingReady | class | Confirmed | visibility=public; methods=7; fields=3 |
| DolocTown.Config.Fishing.TbFarmFishFormation | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Fishing.TbFish | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Fishing.TbFishDocument | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Fishing.TbFishingPool | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.FarmFishTank/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.FishIncubator/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.FishingPool | class | Confirmed | visibility=public; methods=7; fields=2 |
| DolocTown.FishRodRenderer/<WaitForNextFrame>d__43 | class | Confirmed | visibility=private; methods=6; fields=3 |
| DolocTown.FishTankUiState/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.UI.FishDetailData/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.UI.FishDetailInfo | class | Confirmed | visibility=public; methods=3; fields=6 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocTown.AgentControllerState/<>c.<UseTool>b__75_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentStateFishing.get_SupportInteract | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.UseFishCam | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentStateFishingBattle.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingBattle.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingCast.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingPull.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingPull.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingReady.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingReady.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingWait.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.UseFishRod | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionManager.RefreshAnimalProductRecord | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionManager.RefreshFishRecord | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionManager/<>c.<RefreshAnimalProductRecord>b__25_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_UpdateInterval | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.set_UpdateInterval | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Fishing.FarmFishFormationInfo.Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Fishing.FishInfo.get_BonusExtraScore | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Fishing.FishInfo.get_InitialStableProbability | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Fishing.FishInfo.set_BonusExtraScore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Fishing.FishInfo.set_InitialStableProbability | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Item.ItemFunctionFishingRod.get_InitProgress | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Item.ItemFunctionFishingRod.set_InitProgress | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.GlobalParameterInfo.get_FishingBiteInitProbability | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.get_FishingPunishStartTime | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.set_FishingBiteInitProbability | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.GlobalParameterInfo.set_FishingPunishStartTime | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.TbGlobalParameter.get_FishingBiteInitProbability | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.TbGlobalParameter.get_FishingPunishStartTime | candidate lifecycle or hook point | Medium |
| DolocTown.FarmFishShoal.UpdateStatus | candidate lifecycle or hook point | Medium |
| DolocTown.FarmFishTank.OpenFishTankUI | candidate lifecycle or hook point | Medium |
| DolocTown.FarmFishTank.RefreshFishTankExtensions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FarmFishTank.RefreshFishTankExtensionsInventory | candidate lifecycle or hook point | Medium |
| DolocTown.FarmFishTank/<>c__DisplayClass75_0.<OpenFishTankUI>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator._OnCloseInventory | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator.<OnInteract>b__17_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator.UpdateRenderer | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator/<>c.<_OnCloseInventory>b__21_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator/<>c.<_OnCloseInventory>b__21_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishIncubator/<>c.<OnInteract>b__17_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishingGame.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGame.OnStart | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGame.OnUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameController.FixedUpdateGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameController.StartGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameController.UpdateGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameDoubleCircle.GetClosestPointInCircle | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishingGameDoubleCircle.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameDoubleCircle.OnStart | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameDoubleCircle.OnUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameDoubleCircle.UpdateGameStatus | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishingGameDoubleCircle.UpdateRenderStatus | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishingGameScrollBar.FixedUpdateGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameScrollBar.RefreshProgress | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishingGameScrollBar.StartGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishingGameScrollBar.UpdateGame | candidate lifecycle or hook point | Medium |
| DolocTown.FishRodHook.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodHook.FixedUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodHook.OnTriggerEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodHook.StartFloatingPhysical | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodLine.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodLine.RefreshRope | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodLine.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodLine.UseRopeLine | candidate lifecycle or hook point | Medium |
| DolocTown.FishRodLine.UseStraightLine | candidate lifecycle or hook point | Medium |
| DolocTown.FishRodRenderer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishRodRenderer.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishShadow.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank._OnCloseInventory | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.AfterLoadEquipment | candidate lifecycle or hook point | Medium |
| DolocTown.FishTank.CheckCurrentItemCanInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.get_CanInteractContinues | candidate lifecycle or hook point | Medium |
| DolocTown.FishTank.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.OpenFishTankUI | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.UpdateNoRender | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTank.UpdateProgressBar | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.CheckFishUnlocked(System.String fishId) | direct call candidate | Medium |
| DolocAPI.GetAnimalAllProduce(System.String animalName) | direct call candidate | Medium |
| DolocAPI.QueryFishDocument(System.String fishName; DolocTown.Config.Fishing.FishDocumentInfo& document) | direct call candidate | Medium |
| DolocAPI.RollFish(System.String poolName; System.Int32 toolLv) | direct call candidate | Medium |
| DolocAPI.RollFishByRarity(System.String poolName; System.Int32 rarity) | direct call candidate | Medium |
| DolocAPI.SendCatchFishEvent(DolocTown.Config.Fishing.FishInfo fish) | direct call candidate | Medium |
| DolocInputSource/INormalInputActions.OnFishing(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/NormalInputActions.get_Fishing() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunction.SetFishingPoolName(System.String poolName) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionFishTank.get_func() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionFishTank.SetFishingPoolName(System.String poolName) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentEquipmentFunctionFishTank.TryRollFish() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.BreakFishing() | direct call candidate | Medium |
| DolocTown.AgentStateFishing.get_IsUiControlled() | direct call candidate | Medium |
| DolocTown.AgentStateFishing.get_SupportDash() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.get_SupportInteract() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.get_SupportJump() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.get_SupportScrollQuickInventoryUI() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.get_SupportUseItem() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishing.UnsetUiControl() | direct call candidate | Medium |
| DolocTown.AgentStateFishingBattle.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingBattle.OnExit() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingBattle.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingCast.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingCast.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingPull.get_IsFailed() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingPull.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingPull.OnExit() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingPull.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingPull.set_IsFailed(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingReady.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingReady.OnExit() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingReady.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingWait.get_IsWaitNow() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingWait.OnEnter() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentStateFishingWait.OnPlay() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_IsMoodSatisfiedProduce() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.Produce() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.ProduceAsItems() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.SimulateProduceAsItems() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_SearchChickenNestToProduce.GenTask(DolocTown.Animal animal; RedSaw.AI.LinearTask.LinearTask& task) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_SearchChickenNestToProduce.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_SearchHoneyCombToProduce.GenTask(DolocTown.Animal animal; RedSaw.AI.LinearTask.LinearTask& task) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_SearchHoneyCombToProduce.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.get_FilterFishFeedsContainerSkin() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.get_FishFeedsContainerSkinIdx() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.get_LowFishFeederThreshold() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.IsFishFeeds(DolocTown.Item item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.IsFishFeedsContainer(DolocTown.Case container) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.set_FilterFishFeedsContainerSkin(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.set_LowFishFeederThreshold(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.get_FishingCache() | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.get_IsFishingNow() | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.SetFishingPool(DolocTown.FishingPool pool) | direct call candidate | Medium; needs instance source |
| DolocTown.BodyController.UseFishRod(DolocTown.ItemFishingRod fishingRod) | direct call candidate | Medium; needs instance source |
| DolocTown.ChickenNest.Produce(DolocTown.CountItem[] inputItems) | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.GetFishDocumentCount() | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.RefreshAnimalProductRecord(System.String name; System.String& title) | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.RefreshFishRecord(System.String name; System.String& title) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_ProduceRequireMood() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_ProduceSpawnEntry() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_ProduceTechPoint() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.get_DefaultFishFeedsType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.get_DefaultFishFeedsType_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.get_FishFeedsThreshold() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncAutomateBotStation.get_MotionPointProduce() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.DeserializeEquipmentFuncFishIncubator(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.get_EmissionSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.get_MaskSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.get_WorkInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.DeserializeEquipmentFuncFishTank(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.get_EnergyCapacity() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.DeserializeEquipmentFuncFishTankBase(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_AlphaMask() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_MetabolismThreshold() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_ProductCapacity() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_UpdateInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.DeserializeEquipmentFuncFishTankEcological(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.get_WorkDuration() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.DeserializeEquipmentFuncFishTankElectricEel(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.get_EfficiencyPerFish() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.get_EnergyCapacity() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.ToString() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocAPI/<GetAnimalAllProduce>d__262.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>2__current | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>7__wrap1 | System.Collections.Generic.List`1/Enumerator<DolocTown.Config.Item.ItemSpawnData> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>7__wrap2 | System.Collections.Generic.List`1/Enumerator<DolocTown.Config.Animal.AnimalHusbandryData> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.animalName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Fishing | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFishTank.poolName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFishTank.shouldGenFish | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishing._isUiControlled | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingBattle.gameHandle | DolocTown.FishingGameScrollBar | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingBattle.isReelInNow | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingCast._cancelHeight | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingCast._shouldWait | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingPull._isAnimationDone | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingPull._pullDuration | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingReady._castTimer | RedSaw.CastTimer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingReady._isAnimationDone | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingReady._powerBar | DolocTown.UI.ProgressCircle | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._animationTimer | RedSaw.RSTimer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._currentAnimationName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._fishOnHookDuration | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._hasRolled | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._hookProbability | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._isPlayAnimationNow | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._tuCounter | RedSaw.RSTimer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingWait._waitForFishBite | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_SearchHoneyCombToProduce.shouldLog | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerFilling._fishFeederCache | DolocTown.IFishTank | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamFilling.fishFeedParams | DolocTown.FillingParams | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController.<FishingCache>k__BackingField | DolocTown.FishingCache | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<ProduceRequireMood>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<ProduceSpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnEntry | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<ProduceTechPoint>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<DefaultFishFeedsType_Ref>k__BackingField | DolocTown.Config.Item.ItemSubTypeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<DefaultFishFeedsType>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<FishFeedsThreshold>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncAutomateBotStation.<MotionPointProduce>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.<EmissionSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.<MaskSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.<WorkInterval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTank.<EnergyCapacity>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.<AlphaMask>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.<MetabolismThreshold>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.<ProductCapacity>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.<UpdateInterval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological.<WorkDuration>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.<EfficiencyPerFish>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel.<EnergyCapacity>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankExtension.<EnergyIncrease>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishTankExtension.<MetabolismIncrease>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo._conditions | DolocTown.Config.Fishing.FarmFishFormationInfo/FormationCondition[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo.<CountRange>k__BackingField | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo.<FormationConditions>k__BackingField | DolocTown.Config.Fishing.FishFormationCondition[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo.<FormationOutputFish>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo.<Weight>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishFormationInfo/FormationCondition.conditions | DolocTown.Config.Fishing.FarmFishFormationInfo/SubFormationCondition[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<EnergyCost>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<GrowDuration>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<IncubateDuration>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<MetabolismIncrease>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<ProduceSpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnEntry | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<RoeItem_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<RoeItem>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<TechPoint>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<Display>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<DocumentInfos>k__BackingField | DolocTown.Config.Archives.DocumentNodeInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<Habitat_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<Habitat>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFeedInfo.<Energy>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFeedInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFeedInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFormationCondition.<CountRange>k__BackingField | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFormationCondition.<FishId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishFormationCondition.<Index>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusBaseScore>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusDuration>k__BackingField | DolocTown.Config.General.RangeInt | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusExtraScore>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusMultiplier>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<BonusProbability>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<CatchMultiplier>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<DefaultUnlock>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<EscapeSpeed>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<ExpFishing>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<FishBait_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<FishBait>k__BackingField | System.String[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<FishingLv>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<FishingRodLv>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<InitialStableProbability>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<IsFish>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<IsGarbage>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<MaxStamina>k__BackingField | DolocTown.Config.General.RangeInt | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishInfo.<Month>k__BackingField | System.Int32[] | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocTown.FishRodHook.OnHooked | native event | System.Action`1<DolocTown.FishingPool> | Confirmed metadata; verify usage |
| DolocTown.AgentControllerState/<>c.<UseTool>b__75_0 | Harmony patch candidate | DolocTown.AgentStateFishingPull s | Risky |
| DolocTown.AgentStateFishing.get_SupportInteract | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishing.get_SupportUseItem | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishing.UseFishCam | Harmony patch candidate |  | Risky |
| DolocTown.AgentStateFishingBattle.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingBattle.OnExit | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingCast.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingPull.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingPull.OnExit | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingReady.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingReady.OnExit | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateFishingWait.OnEnter | Harmony patch candidate |  | Medium |
| DolocTown.BodyController.UseFishRod | Harmony patch candidate | DolocTown.ItemFishingRod fishingRod | Medium |
| DolocTown.CollectionManager.RefreshAnimalProductRecord | Harmony patch candidate | System.String name; System.String& title | Medium |
| DolocTown.CollectionManager.RefreshFishRecord | Harmony patch candidate | System.String name; System.String& title | Medium |
| DolocTown.CollectionManager/<>c.<RefreshAnimalProductRecord>b__25_0 | Harmony patch candidate | System.String item | Risky |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.get_UpdateInterval | Harmony patch candidate |  | Medium |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase.set_UpdateInterval | Harmony patch candidate | System.Int32 value | Risky |
| DolocTown.Config.Fishing.FishInfo.get_BonusExtraScore | Harmony patch candidate |  | Medium |
| DolocTown.Config.Fishing.FishInfo.set_BonusExtraScore | Harmony patch candidate | System.Int32 value | Risky |
| DolocTown.FarmFishShoal.UpdateStatus | Harmony patch candidate |  | Medium |
| DolocTown.FarmFishTank.RefreshFishTankExtensions | Harmony patch candidate |  | Risky |
| DolocTown.FarmFishTank.RefreshFishTankExtensionsInventory | Harmony patch candidate |  | Medium |
| DolocTown.FishIncubator._OnCloseInventory | Harmony patch candidate |  | Risky |
| DolocTown.FishIncubator.<OnInteract>b__17_0 | Harmony patch candidate | DolocTown.FishTankUiState state | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IFishingHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTank | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTankBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTankElectricEel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTankExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo/<>c__DisplayClass30_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo/<>c__DisplayClass34_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo/FormationCondition | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo/FormationCondition/<>c__DisplayClass2_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishFormationInfo/SubFormationCondition | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FarmFishInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishFeedInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishFormationCondition | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishInfo/<>c__DisplayClass128_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishInfo/<>c__DisplayClass130_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.FishingPoolInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFarmFish | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFarmFishFormation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFish | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFishDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFishFeed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Fishing.TbFishingPool | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishFry | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishingRod | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishRoe | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModFishingPoolExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModFishingPoolExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoImmortalJellyfish | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Player.AgentEquipmentFuncProtoFishTank | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.FishDocumentManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.FishingNoteData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FishDetailData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FishDetailData/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FishDetailInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FishingTurntable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IFishingHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Existing AutoFishing and FishingTest mods provide real validation targets.
