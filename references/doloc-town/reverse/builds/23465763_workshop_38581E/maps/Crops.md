# DolocTown Crops / Seeds / Farming API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map farm crop data and lifecycle.
- Identify content-pack data sources.
- Support crop analytics without mutating saves first.

## Decompiled scope

- Matched types: 171
- Matched methods: 2024
- Matched fields: 696
- Matched properties: 525
- Matched events: 0
- Matched call edges: 6776
- Matched strings: 830
- Raw indexes: `maps/index/Crops-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Crop | class | Confirmed | visibility=public; methods=101; fields=11 |
| DolocTown.PlantBasin | class | Confirmed | visibility=public; methods=95; fields=11 |
| DolocTown.Config.Plant.SeedInfo | class | Confirmed | visibility=public; methods=39; fields=15 |
| DolocTown.CropRenderer | class | Confirmed | visibility=public; methods=41; fields=10 |
| DolocTown.CropGeneFunction | class | Confirmed | visibility=public; methods=46; fields=3 |
| DolocTown.CropDecorator | class | Confirmed | visibility=public; methods=43; fields=3 |
| DolocTown.ItemSeed | class | Confirmed | visibility=public; methods=34; fields=6 |
| DolocTown.PlantBasinTree | class | Confirmed | visibility=public; methods=37; fields=3 |
| DolocTown.TreeCrop | class | Confirmed | visibility=public; methods=29; fields=11 |
| DolocTown.Config.Plant.CropGeneInfo | class | Confirmed | visibility=public; methods=27; fields=12 |
| DolocTown.Config.Plant.TreeSeedInfo | class | Confirmed | visibility=public; methods=29; fields=10 |
| DolocTown.PlantBasinSupply | struct | Confirmed | visibility=public; methods=31; fields=7 |
| DolocTown.Config.Archives.PlantDocumentInfo | class | Confirmed | visibility=public; methods=22; fields=10 |
| DolocTown.PlantBasinGrass | class | Confirmed | visibility=public; methods=23; fields=7 |
| DolocTown.Config.Plant.SeedTypeInfo | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocRopeRenderer | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.ItemCrop | class | Confirmed | visibility=public; methods=22; fields=2 |
| DolocTown.Config.Plant.SeedUnlockInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass | class | Confirmed | visibility=public; methods=16; fields=6 |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.TreeCropRenderer | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.VegetationGrow | class | Confirmed | visibility=public; methods=17; fields=4 |
| DolocTown.Config.Item.ItemFunctionCrop | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Plant.CropGeneFuncProtoHanabi | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.PlantDocumentManager | class | Confirmed | visibility=public; methods=14; fields=6 |
| DolocTown.Config.Plant.CropGeneMatrixInfo | class | Confirmed | visibility=public; methods=15; fields=4 |
| DolocTown.VegetationGrowLuminous | class | Confirmed | visibility=public; methods=16; fields=2 |
| DolocTown.Config.Plant.CropGeneFuncProtoSprinkler | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Resource.VegetationFuncGrowLuminous | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.UI.SeedUnLockData | struct | Confirmed | visibility=public; methods=9; fields=8 |
| DolocTown.SeedUnLockUiState | class | Confirmed | visibility=public; methods=14; fields=2 |
| DolocTown.Config.Plant.CropGeneFuncProtoRange | class | Confirmed | visibility=public; methods=12; fields=3 |
| DolocTown.CropEnv/<get_AroundBasins>d__10 | class | Confirmed | visibility=private; methods=8; fields=7 |
| DolocTown.CropGeneFunctionOxygen | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.CropGeneUtils/<>c | class | Confirmed | visibility=private; methods=8; fields=7 |
| DolocTown.Config.Item.ItemFunctionSeed | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Item.ItemFunctionSeedMaternal | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Item.ItemFunctionSeedTree | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoBonsai | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoFertilityEnhance | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoFlorescenceExtend | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoOxygen | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoParasite | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropGeneFuncProtoTimeGift | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.CropLevelData | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Plant.TbCropGene | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.CropEnv/<get_AroundCrops>d__15 | class | Confirmed | visibility=private; methods=9; fields=5 |
| DolocTown.ICropFunction | interface | Confirmed | visibility=public; methods=14; fields=0 |
| DolocTown.UI.CropInfoBox | class | Confirmed | visibility=public; methods=4; fields=10 |
| DolocTown.UI.SeedUINode | class | Confirmed | visibility=public; methods=4; fields=10 |
| DolocTown.CropGeneUtils | static class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocTown.DungeonResourceCrop | class | Confirmed | visibility=public; methods=13; fields=0 |
| DolocTown.GameData.NpcScheduleWorkPlant | class | Confirmed | visibility=public; methods=7; fields=6 |
| DolocTown.ItemSeedTree | class | Confirmed | visibility=public; methods=13; fields=0 |
| DolocTown.UI.CropInfoData | struct | Confirmed | visibility=public; methods=2; fields=11 |
| DolocTown.Config.Plant.TbCropGeneMatrix | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.Config.Resource.VegetationFuncGrowBase | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.PlantBasinSimple | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple | class | Confirmed | visibility=public; methods=9; fields=2 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_OpenPlantDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenSeedUnLockPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenSeedStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenPermissionViewPlantDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocRopeRenderer.FixedUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocRopeRenderer.OnReuse | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotDecisionMakerFarming.__TryReleaseUselessSeeds | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateBotDecisionMakerFarming/<>c__DisplayClass13_0.<__TryReleaseUselessSeeds>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.Command_OpenPlantSubmitPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_CenterOffset | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.set_CenterOffset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_UpdateInterval | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.set_UpdateInterval | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple.get_UseTimes | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple.set_UseTimes | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Plant.CropGeneFuncProtoHanabi.get_StartTime | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Plant.CropGeneFuncProtoHanabi.set_StartTime | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Plant.SeedTypeInfo.get_UseRoomEffect | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Plant.SeedTypeInfo.set_UseRoomEffect | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Crop.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.OriginUpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.OriginUpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.OriginUpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.RefreshMoistStatus | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.UpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.UpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.UpdatePollutedStatus | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Crop.UpdateRenderer | candidate lifecycle or hook point | Medium |
| DolocTown.Crop.UpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.DctUpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.DctUpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.DctUpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.UpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.UpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.CropDecorator.UpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunction.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunction.UpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunction.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunction.UpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunction.UpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionAerialRoot.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionAntiAcidRain.UpdateAcidRain | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionAntiAcidRain/<>c.<UpdateAcidRain>b__2_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropGeneFunctionAntiAcidRain/<>c.<UpdateAcidRain>b__2_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropGeneFunctionCharge.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionCharge/<>c__DisplayClass3_0.<UpdateEx>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropGeneFunctionConiferLeaf.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionConiferLeaf.UpdateNormal | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionConiferLeaf.UpdateScorchSun | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionConiferLeaf/<>c.<UpdateScorchSun>b__5_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropGeneFunctionHanabi.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionNutritionEnrich.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionOxygen.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionParasite.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionParasite.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionSprinkler.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionSprinkler.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionSymbioticSupply.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionTimeGift.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionWindSow.<UpdateEx>b__7_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropGeneFunctionWindSow.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionWindSow.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.CropRenderer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropRenderer.get_PositionCenter | candidate lifecycle or hook point | Medium |
| DolocTown.CropRenderer.OnReuse | candidate lifecycle or hook point | Medium |
| DolocTown.CropRenderer.UpdateMatureRenderer | candidate lifecycle or hook point | Medium |
| DolocTown.CropRenderer/<>c__DisplayClass42_0.<UpdateMatureRenderer>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropTrafficLight.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropTrafficLight.UpdateTrafficLights | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropTrafficLight/<>c.<UpdateTrafficLights>b__9_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CropTrafficLight/<>c.<UpdateTrafficLights>b__9_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DebugCrops.StartWait | candidate lifecycle or hook point | Medium |
| DolocTown.DebugCrops.Update | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResource.InitRandomGrowth | candidate lifecycle or hook point | Medium |
| DolocTown.DungeonResourceCrop.<OnInteract>b__6_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DungeonResourceCrop.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.ForageGrass.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.ICropFunction.UpdateAcidRain | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.QuerySeedProto(System.String name; DolocTown.Config.Plant.SeedInfo& proto) | direct call candidate | Medium |
| DolocAPI.SwingPlantOnBlow(UnityEngine.Animator animator) | direct call candidate | Medium |
| DolocAPI.SwingPlantOnTouch(UnityEngine.Animator animator; System.Boolean light) | direct call candidate | Medium |
| DolocRopeRenderer.get_doubleLocked() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.get_endPosition() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.get_headPosition() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.get_lineRenderer() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.get_zPosition() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.hide() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.OnCreated() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.OnRecycle() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.OnReuse() | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.set_doubleLocked(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.set_endPosition(UnityEngine.Vector2 value) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.set_headPosition(UnityEngine.Vector2 value) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.set_zPosition(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.setLive(System.Single time; System.Action`1<UnityEngine.Vector2[]> callback) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.setMaterial(UnityEngine.Material mat) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.setVisible(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocRopeRenderer.show(UnityEngine.Vector3 p1; UnityEngine.Vector3 p2) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.Grow() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotPatch.AutomatePatchTakeSeedFromContainer(DolocTown.LinearInventory inventory; DolocTown.PlantCondition cond) | direct call candidate | Medium |
| DolocTown.AutomateBotPatch.AutomatePatchTakeSeedsFromContainer(DolocTown.LinearInventory inventory; DolocTown.PlantCondition[] conds) | direct call candidate | Medium |
| DolocTown.AutomateBotPatch.GetPlantCondition(DolocTown.PlantBasin basin) | direct call candidate | Medium |
| DolocTown.AutomateParamFarming.get_AutoPlant() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.get_AutoWatering() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.get_WateringThreshold() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.set_AutoPlant(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.set_AutoWatering(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.set_WateringThreshold(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.get_GatherCrop() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.set_GatherCrop(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetMatureCropNearStation(DolocTown.AutomateBotStation station) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetThirstCropNearStation(DolocTown.AutomateBotStation station; System.Single ratio) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetUnFertilizedNearStation(DolocTown.AutomateBotStation station) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetUnProtectedNearStation(DolocTown.AutomateBotStation station) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetUnseededNearStation(DolocTown.AutomateBotStation station; System.String seedName) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskHarvestCrop.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskPlant.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateTaskWatering.OnExecute(System.Single dt) | direct call candidate | Medium; needs instance source |
| DolocTown.Bullet.get_AffectCrop() | direct call candidate | Medium; needs instance source |
| DolocTown.Bullet.SetCropInfos(System.Boolean affectCrop) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_GrowCost() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_GrowIncrease() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_GrowInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.DeserializePlantDocumentInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Author() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Author_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Content() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Content_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Id_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Reward() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.get_Title_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.PlantDocumentInfo.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.Get(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.get_DataList() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.get_DataMap() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.get_Item(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.GetOrDefault(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.TbPlantDocument.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.DeserializeEquipmentFuncCropTrafficLight(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.get_MaskSprite() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer.DeserializeEquipmentFuncPlantAnalyzer(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin.DeserializeEquipmentFuncPlantBasin(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.DeserializeEquipmentFuncPlantBasinBase(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_CenterOffset() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_SeedType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_SeedType_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_SpriteWetAsset() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_SupplyCapacity() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.DeserializeEquipmentFuncPlantBasinGrass(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_CropName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_CropSkinIndex() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_GrassCount() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_SeedProto() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_UpdateInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.ToString() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocRopeRenderer._rope2D | RedSaw.Physical.Rope2D | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.line | UnityEngine.LineRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.live | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.liveCallback | System.Action`1<UnityEngine.Vector2[]> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.liveTime | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.lockDouble | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocRopeRenderer.zposition | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamFarming.autoPlant | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamFarming.autoWatering | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamFarming.wateringThreshold | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherCrop | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskFertilizer.basin | DolocTown.PlantBasin | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskHarvestCrop.basin | DolocTown.PlantBasin | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskPlant._missionInfo | DolocTown.AutomatePlantMissionInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskProtect.basin | DolocTown.PlantBasin | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskWatering.basins | DolocTown.PlantBasin[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskWatering.sprinklerCost | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bullet.<AffectCrop>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<GrowCost>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<GrowIncrease>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<GrowInterval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Author_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Author>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Content_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Content>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Reward>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Title_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Title>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbPlantDocument._dataList | System.Collections.Generic.List`1<DolocTown.Config.Archives.PlantDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbPlantDocument._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Archives.PlantDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight.<MaskSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.<CenterOffset>k__BackingField | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.<SeedType_Ref>k__BackingField | DolocTown.Config.Plant.SeedTypeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.<SeedType>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.<SpriteWetAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.<SupplyCapacity>k__BackingField | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass._proto | DolocTown.Config.Plant.SeedInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.<CropName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.<CropSkinIndex>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.<GrassCount>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.<UpdateInterval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple.<UseTimes>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinTree.<FertilizerOffset>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FarmFishInfo.<GrowDuration>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionCrop.<EatingEffect_Ref>k__BackingField | DolocTown.Config.Item.EatingEffectInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionCrop.<EatingEffect>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionCrop.<SeedItem_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionCrop.<SeedItem>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionGeneCapsule.<Genes_Ref>k__BackingField | DolocTown.Config.Plant.CropGeneInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeed.<SeedId_Ref>k__BackingField | DolocTown.Config.Plant.SeedInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeed.<SeedId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedMaternal.<SeedItemId_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedMaternal.<SeedItemId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedTree.<TreeSeedId_Ref>k__BackingField | DolocTown.Config.Plant.TreeSeedInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionSeedTree.<TreeSeedId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelResourceGrowthPeriod_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelResourceGrowthPeriod>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelResourceYearRoundGrowth_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelResourceYearRoundGrowth>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<FarmbuilderErrPlantbasinTreeOccupied_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<FarmbuilderErrPlantbasinTreeOccupied>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemSeedCloned_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemSeedCloned>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<PlantDocumentTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<PlantDocumentTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedNodeAlreadyUnlock_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedNodeAlreadyUnlock>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedNodeSucceedUnlock_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedNodeSucceedUnlock>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedPanelTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SeedPanelTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemTipPlantbasinLocked_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemTipPlantbasinLocked>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiCropInfoGrowthLevel_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiCropInfoGrowthLevel>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiCropInfoHarvestCount_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiCropInfoHarvestCount>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotPlantOnGround_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotPlantOnGround>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotPlantTree_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotPlantTree>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrInvalidPlantbasin_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrInvalidPlantbasin>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrNoPlantBasin_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrNoPlantBasin>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrNotSeed_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrNotSeed>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationHarvest_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationHarvest>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipPlantErr_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipPlantErr>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSeedEnd_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSeedEnd>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipUnlockSeed_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipUnlockSeed>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiUnlockSeed_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiUnlockSeed>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mission.ItemGeneConditionInfo.<GeneFilter_Ref>k__BackingField | DolocTown.Config.Plant.CropGeneInfo[] | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.FastButton_OpenSeedStore | Harmony patch candidate |  | Risky |
| DolocAPI.OpenPermissionViewPlantDoc | Harmony patch candidate |  | Risky |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | Harmony patch candidate | DolocTown.StoreUiState state | Risky |
| DolocRopeRenderer.FixedUpdate | Harmony patch candidate |  | Risky |
| DolocRopeRenderer.OnReuse | Harmony patch candidate |  | Medium |
| DolocTown.AutomateBotDecisionMakerFarming.__TryReleaseUselessSeeds | Harmony patch candidate | RedSaw.AI.LinearTask.LinearTask& task | Risky |
| DolocTown.AutomateBotDecisionMakerFarming/<>c__DisplayClass13_0.<__TryReleaseUselessSeeds>b__0 | Harmony patch candidate | DolocTown.Case c | Risky |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.get_CenterOffset | Harmony patch candidate |  | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase.set_CenterOffset | Harmony patch candidate | UnityEngine.Vector2 value | Risky |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.get_UpdateInterval | Harmony patch candidate |  | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass.set_UpdateInterval | Harmony patch candidate | System.Int32 value | Risky |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple.get_UseTimes | Harmony patch candidate |  | Medium |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple.set_UseTimes | Harmony patch candidate | System.Int32 value | Risky |
| DolocTown.Config.Plant.SeedTypeInfo.get_UseRoomEffect | Harmony patch candidate |  | Medium |
| DolocTown.Config.Plant.SeedTypeInfo.set_UseRoomEffect | Harmony patch candidate | System.Boolean value | Risky |
| DolocTown.Crop.AfterLoadData | Harmony patch candidate | DolocTown.PlantBasin basin | Medium |
| DolocTown.Crop.OriginUpdateAcidRain | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition; System.Boolean isProtected; System.Single damage | Medium |
| DolocTown.Crop.OriginUpdateNormal | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition | Medium |
| DolocTown.Crop.OriginUpdateScorchSun | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition; System.Boolean isProtected; System.Single damage | Medium |
| DolocTown.Crop.RefreshMoistStatus | Harmony patch candidate | System.Boolean isMoist; System.Boolean shouldRender | Medium |
| DolocTown.Crop.UpdateAcidRain | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition; System.Boolean isProtected; System.Single damage | Medium |
| DolocTown.Crop.UpdateNormal | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition | Medium |
| DolocTown.Crop.UpdatePollutedStatus | Harmony patch candidate | System.Boolean isMoist; System.Boolean shouldRender | Risky |
| DolocTown.Crop.UpdateRenderer | Harmony patch candidate | System.Boolean animateGrow | Medium |
| DolocTown.Crop.UpdateScorchSun | Harmony patch candidate | System.Boolean shouldRender; System.Boolean isMoist; System.Single addition; System.Single fertilizerAddition; System.Boolean isProtected; System.Single damage | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `ICropHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.AutomatePlantMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.PlantDocumentInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.TbPlantDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantAnalyzer | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasin | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinGrass | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinSimple | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncPlantBasinTree | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncSeedCompressor | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.WDP_AcidRain_PlantBasin | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.WDP_ScorchSun_PlantBasin | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionCrop | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionSeed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionSeedMaternal | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionSeedTree | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProto | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoAerialRoot | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoAntiAcidRain | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoBonsai | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoCharge | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoConiferLeaf | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoFertilityEnhance | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoFirefly | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoFlorescenceExtend | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoFractalCrop | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoGrowUnchecked | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoHanabi | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoHope | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoImmortalJellyfish | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoInfertility | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoNutritionEnrich | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoOxygen | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoParasite | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoRange | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoRobustHealth | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoSporeSpray | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoSprinkler | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoSymbioticSupply | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoTimeGift | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneFuncProtoWindSow | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneMatrixInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneMatrixInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropGeneMatrixInfo/<>c__DisplayClass23_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.CropLevelData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.SeedInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.SeedTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Plant.SeedUnlockInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface ICropHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Cross-check against official crop and resource content docs.
