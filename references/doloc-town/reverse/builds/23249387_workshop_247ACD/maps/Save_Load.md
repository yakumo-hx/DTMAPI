# DolocTown Save / Load / Archive API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map archive roots and post-load hooks.
- Mark fields that are persisted.
- Keep SMAPI custom data separate from official save schema unless proven safe.

## Decompiled scope

- Matched types: 81
- Matched methods: 1486
- Matched fields: 502
- Matched properties: 318
- Matched events: 0
- Matched call edges: 11595
- Matched strings: 798
- Raw indexes: `maps/index/Save_Load-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.GameData.ArchiveDataHandle | class | Confirmed | visibility=public; methods=103; fields=9 |
| DolocTown.GameData.AgentArchiveData | class | Confirmed | visibility=public; methods=40; fields=31 |
| DolocTown.GameData.TimeArchiveData | class | Confirmed | visibility=public; methods=33; fields=12 |
| DolocTown.GameData.LocalSave | class | Confirmed | visibility=public; methods=41; fields=3 |
| DolocTown.GameData.FarmArchiveData | class | Confirmed | visibility=public; methods=17; fields=26 |
| DolocTown.GameData.ArchiveOperationGlobal | static class | Confirmed | visibility=public; methods=41; fields=0 |
| DolocTown.GameData.ArchiveOperationFarm | static class | Confirmed | visibility=public; methods=36; fields=0 |
| DolocTown.GameData.DataPersistenceManager | class | Confirmed | visibility=public; methods=28; fields=5 |
| DolocTown.Config.Archives.PlantDocumentInfo | class | Confirmed | visibility=public; methods=22; fields=10 |
| DolocTown.Config.Archives.ChipDocumentInfo | class | Confirmed | visibility=public; methods=20; fields=9 |
| DolocTown.Config.Archives.CharacterDocumentInfo | class | Confirmed | visibility=public; methods=18; fields=8 |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo | class | Confirmed | visibility=public; methods=18; fields=7 |
| DolocTown.Config.SteamWorkshopUploader | class | Confirmed | visibility=public; methods=15; fields=10 |
| DolocTown.GameData.ArchiveOperationMission | static class | Confirmed | visibility=public; methods=25; fields=0 |
| DolocTown.Config.Archives.DocumentNodeInfo | class | Confirmed | visibility=public; methods=16; fields=6 |
| DolocTown.GameData.MotorDataManager | class | Confirmed | visibility=public; methods=17; fields=5 |
| XLua.OverloadMethodWrap | class | Confirmed | visibility=public; methods=6; fields=16 |
| DolocTown.Config.Archives.DocumentMissionTip | class | Confirmed | visibility=public; methods=14; fields=5 |
| DolocTown.GameData.ArchiveOperationCity | static class | Confirmed | visibility=public; methods=18; fields=0 |
| DolocTown.GameData.CityArchiveData | class | Confirmed | visibility=public; methods=6; fields=12 |
| DolocTown.BaseArchiveData | class | Confirmed | visibility=public; methods=7; fields=10 |
| DolocTown.VersionPatcher/<LoadAllVersionPatchFunctions>d__2 | class | Confirmed | visibility=private; methods=8; fields=9 |
| DolocTown.MonsterGroupManager/<LoadLocalGroups>d__5 | class | Confirmed | visibility=private; methods=9; fields=7 |
| DolocTown.Config.Archives.ChipEvent | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Archives.SpecialChipEventInfo | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.EnvOptimizerDataManager | class | Confirmed | visibility=public; methods=13; fields=1 |
| DolocTown.SceneManager/<CoroutineLoadSceneAsyncEx>d__11 | class | Confirmed | visibility=private; methods=6; fields=7 |
| DolocTown.DataManager`1 | class | Confirmed | visibility=public; methods=11; fields=1 |
| DolocTown.SceneManager/<CoroutineLoadSceneAsync>d__10 | class | Confirmed | visibility=private; methods=6; fields=6 |
| DolocTown.SceneManager/<CoroutineUnloadSceneAsync>d__14 | class | Confirmed | visibility=private; methods=6; fields=6 |
| DolocAssetCache/<Load>d__3 | class | Confirmed | visibility=private; methods=6; fields=5 |
| DolocTown.GameData.ArchiveOperationUniversalUnlock | static class | Confirmed | visibility=public; methods=11; fields=0 |
| DolocTown.UI.LoadingPanel | class | Confirmed | visibility=public; methods=5; fields=6 |
| DolocTown.Config.Archives.DocumentType | enum | Confirmed | visibility=public; methods=0; fields=10 |
| DolocTown.Config.Archives.TbCharacterDocument | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Archives.TbChipDocument | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Archives.TbPlantDocument | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Archives.TbSpecialChipEvent | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.GameData.DungeonArchiveData | class | Confirmed | visibility=public; methods=5; fields=5 |
| DolocTown.UI.ArchiveViewer | class | Confirmed | visibility=public; methods=4; fields=6 |
| DolocTown.DungeonTransitionState/<AsyncLoad>d__6 | class | Confirmed | visibility=private; methods=6; fields=3 |
| DolocTown.EnvOptimizerDataManager/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.GameDataTracker.DataUploader | static class | Confirmed | visibility=public; methods=9; fields=0 |
| DolocTown.InteractableSavePoint | class | Confirmed | visibility=public; methods=5; fields=3 |
| DolocTown.UI.ArchiveCatalogPanel | class | Confirmed | visibility=public; methods=5; fields=3 |
| DolocCoroutineLoader | class | Confirmed | visibility=public; methods=4; fields=3 |
| DolocTown.Config.WorkshopUploadResult | class | Confirmed | visibility=public; methods=4; fields=3 |
| DolocTown.GameData.ArchiveDataHandle/<>c | class | Confirmed | visibility=private; methods=4; fields=3 |
| DolocTown.UI.ArchiveListViewer | class | Confirmed | visibility=public; methods=6; fields=1 |
| DolocTown.WwiseSoundManager/<LoadBankAsync>d__41 | struct | Confirmed | visibility=private; methods=2; fields=5 |
| DolocTown.GameData.IDataPersistence | interface | Confirmed | visibility=public; methods=6; fields=0 |
| DolocTown.UI.GunReloadTip | class | Confirmed | visibility=public; methods=3; fields=3 |
| XLua.SignatureLoader | class | Confirmed | visibility=public; methods=3; fields=3 |
| DolocTown.Config.Archives.ChipEventType | enum | Confirmed | visibility=public; methods=0; fields=5 |
| DolocTown.Config.WorkshopUploadPlan | class | Confirmed | visibility=public; methods=3; fields=2 |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass118_0 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass119_0 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass121_0 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.GameData.ArchiveOperationFarm/<>c | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.GameData.ExtraArchiveData | class | Confirmed | visibility=public; methods=3; fields=2 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.__LoadStaticAssets | candidate lifecycle or hook point | Medium |
| DolocAPI._LoadSwingAnimationCache | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.<ExtendFarm>g__OnSceneLoaded\|346_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.AfterLoadArchiveData | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_LoadGame | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_SaveGame | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadConfigTables | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadMods | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.get_gameLoadingTip | candidate lifecycle or hook point | Medium |
| DolocAPI.get_IsDataLoaded | candidate lifecycle or hook point | Medium |
| DolocAPI.LoadBackground | candidate lifecycle or hook point | Medium |
| DolocAPI.LoadGame | candidate lifecycle or hook point | Medium |
| DolocAPI.LoadSprite | candidate lifecycle or hook point | Medium |
| DolocAPI.LoadUserSettings | candidate lifecycle or hook point | Medium |
| DolocAPI.SaveGame | candidate lifecycle or hook point | Medium |
| DolocAPI.SaveUserSettings | candidate lifecycle or hook point | Medium |
| DolocAPI.SaveUserSettings | candidate lifecycle or hook point | Medium |
| DolocAPI.set_gameLoadingTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.UnloadDrone | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass37_0.<__LoadStaticAssets>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAssetCache.Load | candidate lifecycle or hook point | Medium |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
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
| DolocCoroutineLoader.Start | candidate lifecycle or hook point | Medium |
| DolocGameAssetsPatch.LoadMaterial | candidate lifecycle or hook point | Medium |
| DolocResources.loadConfig | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocTown.AchievementSystem.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.AchievementSystem.LoadAchievementMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem.LoadMissionGraph | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentHatRendererUtils.TryLoadHatRenderer | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalMap.LoadBuildingStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap.LoadGroundStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap.LoadPlatformStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadPlatformStairs>b__16_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimatorAsset.TryLoadAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AssetBase`1.LoadDefaultAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AssetBase`1.TryLoadAsset | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateBot.AfterLoadEquipment | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotDecisionMaker.OnUnload | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotStation.AfterLoadEquipment | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotStation.Load | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotStation.Unload | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotUiState.TryLoadBot | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateBotUiState.Unload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateParam.AfterLoadAutomateBot | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParam.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamEmpty.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamFarming.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamFilling.AfterLoadAutomateBot | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamFilling.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamGathering.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamLogistics.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamProcessing.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.BackgroundLayerRenderer.LoadLayer | candidate lifecycle or hook point | Medium |
| DolocTown.BackgroundRenderer.LoadDefaultPreset | candidate lifecycle or hook point | Medium |
| DolocTown.BackgroundRenderer.LoadPreset | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.__LoadStaticAssets(System.Action callback) | direct call candidate | Medium |
| DolocAPI.get_archiveHandle() | direct call candidate | Medium |
| DolocAPI.get_gameLoadingTip() | direct call candidate | Medium |
| DolocAPI.get_IsDataLoaded() | direct call candidate | Medium |
| DolocAPI.GetAllArchiveInfos() | direct call candidate | Medium |
| DolocAPI.GetArchiveInfo(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.GetCurrentArchiveDataAsString() | direct call candidate | Medium |
| DolocAPI.LoadBackground(DolocTown.GameData.EnvBackgroundSO background) | direct call candidate | Medium |
| DolocAPI.LoadGame(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.LoadSprite(System.String imageName) | direct call candidate | Medium |
| DolocAPI.LoadUserSettings() | direct call candidate | Medium |
| DolocAPI.SaveGame(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.SaveUserSettings(DolocTown.UserSettings settings) | direct call candidate | Medium |
| DolocAPI.SaveUserSettings() | direct call candidate | Medium |
| DolocAssetCache.Load(System.Action callback) | direct call candidate | Medium; needs instance source |
| DolocBundleManager.LoadAsync(System.Action`1<System.Boolean> callback) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.BeginInvoke(System.AsyncCallback callback; System.Object object) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.EndInvoke(System.IAsyncResult result) | direct call candidate | Medium; needs instance source |
| DolocBundleManager/DataLoader.Invoke() | direct call candidate | Medium; needs instance source |
| DolocCoroutineLoader.AddCoroutine(System.Collections.IEnumerator coroutine) | direct call candidate | Medium; needs instance source |
| DolocCoroutineLoader.OnCoroutineDone() | direct call candidate | Medium; needs instance source |
| DolocCoroutineLoader.Start(UnityEngine.MonoBehaviour driver) | direct call candidate | Medium; needs instance source |
| DolocGameAssetsPatch.LoadMaterial(DolocGameAssets assetId) | direct call candidate | Medium |
| DolocResources.loadConfig(System.String path; System.String& source) | direct call candidate | Medium |
| DolocResources.loadSpriteAtlas(System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Func`2<System.String,System.String> handleKey; System.Action`1<System.String> log) | direct call candidate | Medium |
| DolocResources.loadSpriteAtlas(System.String path; System.Collections.Generic.Dictionary`2<System.String,UnityEngine.Sprite>& lut; System.Action`1<System.String> log) | direct call candidate | Medium |
| DolocTown.AchievementSystem.AfterLoadData() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentHatRendererUtils.TryLoadHatRenderer(System.String hatName; System.Boolean isRiding; DolocTown.AgentHatRenderer& hatRenderer) | direct call candidate | Medium |
| DolocTown.Animal.AfterLoadData() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_isDeserializationValid() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBot.AfterLoadEquipment(DolocTown.AutomateBotStation station) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotDecisionMaker.OnUnload() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotStation.AfterLoadEquipment() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotStation.Load(System.Int32 botIndex; DolocTown.Item item) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBotStation.Unload(System.Int32 botIndex) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParam.AfterLoadAutomateBot(DolocTown.AutomateBot bot) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParam.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamEmpty.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFarming.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.AfterLoadAutomateBot(DolocTown.AutomateBot bot) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamFilling.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamGathering.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamLogistics.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamProcessing.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.BackgroundLayerRenderer.LoadLayer(UnityEngine.Sprite sprite; UnityEngine.Shader shader; System.Single saturation; System.Single lightness; System.String sortingLayer; UnityEngine.Color color; System.Int32 orderInLayer; System.Boolean hasAlpha; System.Single alpha) | direct call candidate | Medium; needs instance source |
| DolocTown.BackgroundRenderer.LoadDefaultPreset() | direct call candidate | Medium; needs instance source |
| DolocTown.BackgroundRenderer.LoadPreset(DolocTown.GameData.EnvBackgroundSO bg) | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetGameDate() | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetGameRealTimeStamp() | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetGameTimeSpan() | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetMoney() | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetPlayerName() | direct call candidate | Medium; needs instance source |
| DolocTown.BaseArchiveData.GetPositionTitle() | direct call candidate | Medium; needs instance source |
| DolocTown.BattleUtils.LoadBattleConfig() | direct call candidate | Medium |
| DolocTown.BodyController.LoadCurrentTool() | direct call candidate | Medium; needs instance source |
| DolocTown.Building.AfterLoadData() | direct call candidate | Medium; needs instance source |
| DolocTown.Case.AfterLoadEquipment() | direct call candidate | Medium; needs instance source |
| DolocTown.CityEquipmentLogic.OnAfterLoadArchiveData(System.Boolean isNewGame) | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionBookUiState.OpenNpcArchiveWithId(System.String documentId) | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.AfterLoadData() | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.GetResourceDocumentList() | direct call candidate | Medium; needs instance source |
| DolocTown.CollectionManager.RefreshResourceRecord(System.String name; System.String& title) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalDocumentInfo.get_DocumentInfos() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.DeserializeCharacterDocumentInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Content() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Content_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Subhead() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Subhead_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.get_Title_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CharacterDocumentInfo.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.DeserializeChipDocumentInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Author() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Author_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Content() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Content_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_PoolId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.get_Title_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipDocumentInfo.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.DeserializeChipEvent(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Archives.ChipEvent.get_EventType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.get_TargetId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.ChipEvent.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.DeserializeCustomizedDocumentNodeInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.get_DescriptionAppend() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.get_DescriptionAppend_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.get_DocumentType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.get_EnvOptimizerPoint() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocAPI.<gameLoadingTip>k__BackingField | DolocTown.UI.GameLoadingTip | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<>2__current | System.Object | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAssetCache/<Load>d__3.<handle>5__2 | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.preloadLabel | UnityEngine.AddressableAssets.AssetLabelReference | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<>7__wrap2 | DolocBundleManager/DataLoader[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager/<InitDataAsync>d__76.<loader>5__5 | DolocBundleManager/DataLoader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocCoroutineLoader.callback | System.Action | Risky: reflection/private field | Disable dependent feature if missing |
| DolocCoroutineLoader.coroutines | System.Collections.Generic.Queue`1<System.Collections.IEnumerator> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocCoroutineLoader.total | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Animal.isInitAfterLoadData | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetArrayBase`2.loaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AssetBase`1._isLoaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BattleUtils._isBattleConfigLoaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuffManager.loadedBuffs | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Buff> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState.archiveTypeIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalDocumentInfo.<DocumentInfos>k__BackingField | DolocTown.Config.Archives.DocumentNodeInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Content_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Content>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Subhead_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Subhead>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Title_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CharacterDocumentInfo.<Title>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Author_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Author>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Content_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Content>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<PoolId>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Title_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipDocumentInfo.<Title>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipEvent.<EventType>k__BackingField | DolocTown.Config.Archives.ChipEventType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.ChipEvent.<TargetId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<DescriptionAppend_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<DescriptionAppend>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<DocumentType>k__BackingField | DolocTown.Config.Archives.DocumentType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<EnvOptimizerPoint>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.CustomizedDocumentNodeInfo.<Value>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<LikingLevel>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Tip_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Tip>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentNodeInfo.<DescriptionAppend_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentNodeInfo.<DescriptionAppend>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentNodeInfo.<DocumentType>k__BackingField | DolocTown.Config.Archives.DocumentType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentNodeInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentNodeInfo.<Value>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Author_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Author>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Content_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Content>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Reward>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Title_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Title>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.SpecialChipEventInfo.<ChipEvents>k__BackingField | System.Collections.Generic.List`1<DolocTown.Config.Archives.ChipEvent> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.SpecialChipEventInfo.<Id>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbCharacterDocument._dataList | System.Collections.Generic.List`1<DolocTown.Config.Archives.CharacterDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbCharacterDocument._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Archives.CharacterDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbChipDocument._dataList | System.Collections.Generic.List`1<DolocTown.Config.Archives.ChipDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbChipDocument._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Archives.ChipDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbPlantDocument._dataList | System.Collections.Generic.List`1<DolocTown.Config.Archives.PlantDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbPlantDocument._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Archives.PlantDocumentInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbSpecialChipEvent._dataList | System.Collections.Generic.List`1<DolocTown.Config.Archives.SpecialChipEventInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.TbSpecialChipEvent._dataMap | System.Collections.Generic.Dictionary`2<System.Int32,DolocTown.Config.Archives.SpecialChipEventInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneChipInfo.<ReloadDurationDecrease>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneWeaponInfo._isBulletLoaded | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Drone.DroneWeaponInfo.<ReloadDuration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.ChairInfo.<Save>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Fishing.FishDocumentInfo.<DocumentInfos>k__BackingField | DolocTown.Config.Archives.DocumentNodeInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<DronePanelErrLoad_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<DronePanelErrLoad>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataLoad_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataLoad>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataSaveFail_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataSaveFail>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataSaveSuccessful_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<GameDataSaveSuccessful>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemChipReloadDurationDecrease_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemChipReloadDurationDecrease>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelCanNotSaveByConflict_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelCanNotSaveByConflict>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelLoading_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelLoading>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelOtherSaveSuccessful_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelOtherSaveSuccessful>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelSaveSuccessful_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<SettingPanelSaveSuccessful>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiEnvOptimizerLoadingData_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiEnvOptimizerLoadingData>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModConfirmUpload_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModConfirmUpload>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModReloading_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModReloading>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModUploadFailed_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModUploadFailed>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModUploading_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.__LoadStaticAssets | Harmony patch candidate | System.Action callback | Medium |
| DolocAPI._LoadSwingAnimationCache | Harmony patch candidate |  | Risky |
| DolocAPI.<ExtendFarm>g__OnSceneLoaded\|346_1 | Harmony patch candidate | DolocTown.GameData.ArchiveDataHandle handle | Risky |
| DolocAPI.AfterLoadArchiveData | Harmony patch candidate | System.Boolean isNewGame | Risky |
| DolocAPI.Command_LoadGame | Harmony patch candidate | System.Int32 index | Risky |
| DolocAPI.Command_SaveGame | Harmony patch candidate | System.Int32 index | Risky |
| DolocAPI.FastButton_ReloadConfigTables | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_ReloadMods | Harmony patch candidate |  | Risky |
| DolocAPI.get_gameLoadingTip | Harmony patch candidate |  | Medium |
| DolocAPI.get_IsDataLoaded | Harmony patch candidate |  | Medium |
| DolocAPI.LoadBackground | Harmony patch candidate | DolocTown.GameData.EnvBackgroundSO background | Medium |
| DolocAPI.LoadGame | Harmony patch candidate | System.Int32 index | Medium |
| DolocAPI.LoadSprite | Harmony patch candidate | System.String imageName | Medium |
| DolocAPI.LoadUserSettings | Harmony patch candidate |  | Medium |
| DolocAPI.SaveGame | Harmony patch candidate | System.Int32 index | Medium |
| DolocAPI.SaveUserSettings | Harmony patch candidate |  | Medium |
| DolocAPI.SaveUserSettings | Harmony patch candidate | DolocTown.UserSettings settings | Medium |
| DolocAPI.set_gameLoadingTip | Harmony patch candidate | DolocTown.UI.GameLoadingTip value | Risky |
| DolocAPI.UnloadDrone | Harmony patch candidate |  | Risky |
| DolocAPI/<>c__DisplayClass37_0.<__LoadStaticAssets>b__0 | Harmony patch candidate | System.Boolean succeed | Risky |
| DolocAssetCache.Load | Harmony patch candidate | System.Action callback | Medium |
| DolocAssetCache/<>c__DisplayClass3_0.<Load>b__0 | Harmony patch candidate | UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<System.Collections.Generic.IList`1<UnityEngine.Object>> h | Risky |
| DolocBundleManager.LoadAsync | Harmony patch candidate | System.Action`1<System.Boolean> callback | Medium |
| DolocBundleManager.LoadBulletMovers | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadBullets | Harmony patch candidate |  | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IDataHelper / IGameLoopEvents` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocAssetCache/<Load>d__3 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocBundleManager/DataLoader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.BaseArchiveData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
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
| DolocTown.Config.ModManager/LocalModUploadPlanCache | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/<>c__DisplayClass27_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadMode | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadPlan | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadResult | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.DataManager`1 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.EnvOptimizerDataManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.EnvOptimizerDataManager/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.AgentArchiveData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass118_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass119_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass121_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveDataHandle/<>c__DisplayClass140_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationCity | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationDungeon | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationEmail | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationExtra | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationFarm | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationFarm/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationFarm/<>c__DisplayClass30_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationGlobal | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationMission/<>c__DisplayClass3_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ArchiveOperationUniversalUnlock | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.CityArchiveData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.DataPersistenceManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.DungeonArchiveData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ExtraArchiveData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.ExtraArchiveData/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IDataHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Save data writes are high risk until verified in-game.
