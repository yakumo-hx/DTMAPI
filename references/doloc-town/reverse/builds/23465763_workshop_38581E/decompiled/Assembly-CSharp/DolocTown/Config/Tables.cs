using System;
using System.Collections.Generic;
using DolocTown.Config.Animal;
using DolocTown.Config.Archives;
using DolocTown.Config.Automate;
using DolocTown.Config.Buff;
using DolocTown.Config.Building;
using DolocTown.Config.Calendar;
using DolocTown.Config.Dialogue;
using DolocTown.Config.Drone;
using DolocTown.Config.Email;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Equipment;
using DolocTown.Config.Festival;
using DolocTown.Config.Fishing;
using DolocTown.Config.Global;
using DolocTown.Config.Item;
using DolocTown.Config.Localization;
using DolocTown.Config.Mission;
using DolocTown.Config.Mod;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Plant;
using DolocTown.Config.Platform;
using DolocTown.Config.Player;
using DolocTown.Config.Recipe;
using DolocTown.Config.Resource;
using DolocTown.Config.Room;
using DolocTown.Config.Settings;
using DolocTown.Config.Sound;
using DolocTown.Config.Store;
using DolocTown.Config.TechTree;
using DolocTown.Config.Time;
using DolocTown.Config.UI;
using DolocTown.Config.Weather;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config;

public sealed class Tables
{
	private readonly Func<string, JSONNode> _dataLoader;

	private readonly Dictionary<Type, string> _dataFileMap = new Dictionary<Type, string>();

	private Dictionary<string, ITextProvider> _textProviders = new Dictionary<string, ITextProvider>();

	private ITextProvider _currentTextProvider;

	private string _currentL10nId;

	public static Action LanguageChange;

	public TbTextMapperZH_CN TbTextMapperZH_CN { get; }

	public TbTextMapperEN TbTextMapperEN { get; }

	public TbTextMapperZH_TW TbTextMapperZH_TW { get; }

	public TbTextMapperJA TbTextMapperJA { get; }

	public TbTextMapperKO TbTextMapperKO { get; }

	public TbTextMapperPT_BR TbTextMapperPT_BR { get; }

	public TbTextMapperFR TbTextMapperFR { get; }

	public TbTextMapperDE TbTextMapperDE { get; }

	public TbTextMapperRU TbTextMapperRU { get; }

	public TbLocalization TbLocalization { get; }

	public TbL10nImage TbL10nImage { get; }

	public TbL10nLink TbL10nLink { get; }

	public TbStaticText TbStaticText { get; }

	public TbL10nText TbL10nText { get; }

	public TbTextTip TbTextTip { get; }

	public TbGlobalParameter TbGlobalParameter { get; }

	public TbTimer TbTimer { get; }

	public TbUserSetting TbUserSetting { get; }

	public TbUserSettingGroup TbUserSettingGroup { get; }

	public TbDialogueOption TbDialogueOption { get; }

	public TbDialogueTextStyle TbDialogueTextStyle { get; }

	public TbDialogueEntity TbDialogueEntity { get; }

	public TbRebindAction TbRebindAction { get; }

	public TbGameKeyIcon TbGameKeyIcon { get; }

	public TbGameCombinedKeyIcon TbGameCombinedKeyIcon { get; }

	public TbGameKeyAction TbGameKeyAction { get; }

	public TbMainMenu TbMainMenu { get; }

	public TbEquipmentMenu TbEquipmentMenu { get; }

	public TbCompendiumMenu TbCompendiumMenu { get; }

	public TbEmailMenu TbEmailMenu { get; }

	public TbModMenu TbModMenu { get; }

	public TbUIEntity TbUIEntity { get; }

	public TbUIEntityGroup TbUIEntityGroup { get; }

	public TbUIPooledObject TbUIPooledObject { get; }

	public TbChipDocument TbChipDocument { get; }

	public TbPlantDocument TbPlantDocument { get; }

	public TbCharacterDocument TbCharacterDocument { get; }

	public TbSpecialChipEvent TbSpecialChipEvent { get; }

	public TbFactionType TbFactionType { get; }

	public TbFactionMissionType TbFactionMissionType { get; }

	public TbFactionMission TbFactionMission { get; }

	public TbTreatyPortFaction TbTreatyPortFaction { get; }

	public TbBoardMission TbBoardMission { get; }

	public TbRewardPool TbRewardPool { get; }

	public TbBoardMissionLevel TbBoardMissionLevel { get; }

	public TbBoardMissionType TbBoardMissionType { get; }

	public TbMission TbMission { get; }

	public TbMissionType TbMissionType { get; }

	public TbPositionType TbPositionType { get; }

	public TbMissionDecorator TbMissionDecorator { get; }

	public TbCustomEvent TbCustomEvent { get; }

	public TbItemSubmitCondition TbItemSubmitCondition { get; }

	public TbItemGeneCondition TbItemGeneCondition { get; }

	public TbEmail TbEmail { get; }

	public TbScene TbScene { get; }

	public TbBackgroundHighLevel TbBackgroundHighLevel { get; }

	public TbMarkPoint TbMarkPoint { get; }

	public TbPortal TbPortal { get; }

	public TbInwalkableArea TbInwalkableArea { get; }

	public TbMapArea TbMapArea { get; }

	public TbMapRoom TbMapRoom { get; }

	public TbMapRoomType TbMapRoomType { get; }

	public TbMapType TbMapType { get; }

	public TbStation TbStation { get; }

	public TbLockableObject TbLockableObject { get; }

	public TbRoom TbRoom { get; }

	public TbDialogueObject TbDialogueObject { get; }

	public TbTrashTalk TbTrashTalk { get; }

	public TbItem TbItem { get; }

	public TbItemOrder TbItemOrder { get; }

	public TbItemMainType TbItemMainType { get; }

	public TbItemSubType TbItemSubType { get; }

	public TbItemAutomationType TbItemAutomationType { get; }

	public TbItemSource TbItemSource { get; }

	public TbEatingEffect TbEatingEffect { get; }

	public TbMissionItem TbMissionItem { get; }

	public TbToolOverride TbToolOverride { get; }

	public TbCountItemList TbCountItemList { get; }

	public TbRangedItemList TbRangedItemList { get; }

	public TbRecipe TbRecipe { get; }

	public TbRecipeGroup TbRecipeGroup { get; }

	public TbRecipeMainType TbRecipeMainType { get; }

	public TbRecipeSubType TbRecipeSubType { get; }

	public TbIngredientGroup TbIngredientGroup { get; }

	public TbDish TbDish { get; }

	public TbDishGroup TbDishGroup { get; }

	public TbDismantleRecipe TbDismantleRecipe { get; }

	public TbDismantleRecipeGroup TbDismantleRecipeGroup { get; }

	public TbBuilding TbBuilding { get; }

	public TbBuildingExterior TbBuildingExterior { get; }

	public TbBuildingWallpaper TbBuildingWallpaper { get; }

	public TbRoomEffect TbRoomEffect { get; }

	public TbPlatform TbPlatform { get; }

	public TbBuildingSupport TbBuildingSupport { get; }

	public TbEquipment TbEquipment { get; }

	public TbChair TbChair { get; }

	public TbLamp TbLamp { get; }

	public TbItemPlaceCondition TbItemPlaceCondition { get; }

	public TbLikingLevelMap TbLikingLevelMap { get; }

	public TbNpcLiking TbNpcLiking { get; }

	public TbNpc TbNpc { get; }

	public TbNpcDocument TbNpcDocument { get; }

	public TbIdleTalkNode TbIdleTalkNode { get; }

	public TbBuff TbBuff { get; }

	public TbSpiritThreshold TbSpiritThreshold { get; }

	public TbRecoveryDecay TbRecoveryDecay { get; }

	public TbFootStep TbFootStep { get; }

	public TbMaterialSound TbMaterialSound { get; }

	public TbBgm TbBgm { get; }

	public TbAmbience TbAmbience { get; }

	public TbExclusiveSFX TbExclusiveSFX { get; }

	public TbStore TbStore { get; }

	public TbStorePriceScale TbStorePriceScale { get; }

	public TbStoreItemList TbStoreItemList { get; }

	public TbExchangeStore TbExchangeStore { get; }

	public TbStoreItemUnlock TbStoreItemUnlock { get; }

	public TbSeed TbSeed { get; }

	public TbSeedType TbSeedType { get; }

	public TbSeedUnlock TbSeedUnlock { get; }

	public TbTreeSeed TbTreeSeed { get; }

	public TbCropGene TbCropGene { get; }

	public TbCropGeneMatrix TbCropGeneMatrix { get; }

	public TbWeather TbWeather { get; }

	public TbDayPeriod TbDayPeriod { get; }

	public TbSeason TbSeason { get; }

	public TbSeasonWeather TbSeasonWeather { get; }

	public TbDungeonSeason TbDungeonSeason { get; }

	public TbTechTree TbTechTree { get; }

	public TbTechNode TbTechNode { get; }

	public TbTechPoint TbTechPoint { get; }

	public TbMonster TbMonster { get; }

	public TbMonsterDocument TbMonsterDocument { get; }

	public TbEnvObject TbEnvObject { get; }

	public TbVegetation TbVegetation { get; }

	public TbResource TbResource { get; }

	public TbResourceType TbResourceType { get; }

	public TbResinCollectorOutput TbResinCollectorOutput { get; }

	public TbResourceDocument TbResourceDocument { get; }

	public TbMonsterSpawn TbMonsterSpawn { get; }

	public TbItemSpawn TbItemSpawn { get; }

	public TbResourceSpawn TbResourceSpawn { get; }

	public TbVegetationSpawn TbVegetationSpawn { get; }

	public TbEnvObjectSpawn TbEnvObjectSpawn { get; }

	public TbGlobalGuaranteed TbGlobalGuaranteed { get; }

	public TbFarmLevel TbFarmLevel { get; }

	public TbBackpackLevel TbBackpackLevel { get; }

	public TbHat TbHat { get; }

	public TbAgentEquipmentSkill TbAgentEquipmentSkill { get; }

	public TbPlayerAnimationFrame TbPlayerAnimationFrame { get; }

	public TbFish TbFish { get; }

	public TbFishingPool TbFishingPool { get; }

	public TbFarmFish TbFarmFish { get; }

	public TbFarmFishFormation TbFarmFishFormation { get; }

	public TbFishDocument TbFishDocument { get; }

	public TbFishFeed TbFishFeed { get; }

	public TbDroneStructure TbDroneStructure { get; }

	public TbDroneWeapon TbDroneWeapon { get; }

	public TbDroneChip TbDroneChip { get; }

	public TbDroneEngine TbDroneEngine { get; }

	public TbDroneAssist TbDroneAssist { get; }

	public TbDroneSkill TbDroneSkill { get; }

	public TbDroneSlot TbDroneSlot { get; }

	public TbEnvOptimizerPoint TbEnvOptimizerPoint { get; }

	public TbEnvOptimizerBranch TbEnvOptimizerBranch { get; }

	public TbEnvOptimizerSlot TbEnvOptimizerSlot { get; }

	public TbAnimal TbAnimal { get; }

	public TbFeed TbFeed { get; }

	public TbHusbandryEnergy TbHusbandryEnergy { get; }

	public TbHusbandry TbHusbandry { get; }

	public TbAnimalState TbAnimalState { get; }

	public TbAnimalDocument TbAnimalDocument { get; }

	public TbCalendar TbCalendar { get; }

	public TbDateEvent TbDateEvent { get; }

	public TbFestival TbFestival { get; }

	public TbAutomateBotAppearance TbAutomateBotAppearance { get; }

	public TbAutomateBotPerformance TbAutomateBotPerformance { get; }

	public TbAutomateBot TbAutomateBot { get; }

	public TbModImageSetting TbModImageSetting { get; }

	public TbModRecipeGroupExtension TbModRecipeGroupExtension { get; }

	public TbModStoreExtension TbModStoreExtension { get; }

	public TbModExchangeStoreExtension TbModExchangeStoreExtension { get; }

	public TbModResourceSpawnExtension TbModResourceSpawnExtension { get; }

	public TbModVegetationSpawnExtension TbModVegetationSpawnExtension { get; }

	public TbModItemSpawnExtension TbModItemSpawnExtension { get; }

	public TbModIngredientGroupExtension TbModIngredientGroupExtension { get; }

	public TbModDishGroupExtension TbModDishGroupExtension { get; }

	public TbModFishingPoolExtension TbModFishingPoolExtension { get; }

	public string CurrentL10nId => _currentL10nId;

	public Tables(Func<string, JSONNode> loader)
	{
		_dataLoader = loader;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		TbTextMapperZH_CN = new TbTextMapperZH_CN(loader("localization_tbtextmapperzh_cn"));
		dictionary.Add("Localization.TbTextMapperZH_CN", TbTextMapperZH_CN);
		_dataFileMap.Add(TbTextMapperZH_CN.GetType(), "localization_tbtextmapperzh_cn");
		TbTextMapperEN = new TbTextMapperEN(loader("localization_tbtextmapperen"));
		dictionary.Add("Localization.TbTextMapperEN", TbTextMapperEN);
		_dataFileMap.Add(TbTextMapperEN.GetType(), "localization_tbtextmapperen");
		TbTextMapperZH_TW = new TbTextMapperZH_TW(loader("localization_tbtextmapperzh_tw"));
		dictionary.Add("Localization.TbTextMapperZH_TW", TbTextMapperZH_TW);
		_dataFileMap.Add(TbTextMapperZH_TW.GetType(), "localization_tbtextmapperzh_tw");
		TbTextMapperJA = new TbTextMapperJA(loader("localization_tbtextmapperja"));
		dictionary.Add("Localization.TbTextMapperJA", TbTextMapperJA);
		_dataFileMap.Add(TbTextMapperJA.GetType(), "localization_tbtextmapperja");
		TbTextMapperKO = new TbTextMapperKO(loader("localization_tbtextmapperko"));
		dictionary.Add("Localization.TbTextMapperKO", TbTextMapperKO);
		_dataFileMap.Add(TbTextMapperKO.GetType(), "localization_tbtextmapperko");
		TbTextMapperPT_BR = new TbTextMapperPT_BR(loader("localization_tbtextmapperpt_br"));
		dictionary.Add("Localization.TbTextMapperPT_BR", TbTextMapperPT_BR);
		_dataFileMap.Add(TbTextMapperPT_BR.GetType(), "localization_tbtextmapperpt_br");
		TbTextMapperFR = new TbTextMapperFR(loader("localization_tbtextmapperfr"));
		dictionary.Add("Localization.TbTextMapperFR", TbTextMapperFR);
		_dataFileMap.Add(TbTextMapperFR.GetType(), "localization_tbtextmapperfr");
		TbTextMapperDE = new TbTextMapperDE(loader("localization_tbtextmapperde"));
		dictionary.Add("Localization.TbTextMapperDE", TbTextMapperDE);
		_dataFileMap.Add(TbTextMapperDE.GetType(), "localization_tbtextmapperde");
		TbTextMapperRU = new TbTextMapperRU(loader("localization_tbtextmapperru"));
		dictionary.Add("Localization.TbTextMapperRU", TbTextMapperRU);
		_dataFileMap.Add(TbTextMapperRU.GetType(), "localization_tbtextmapperru");
		TbLocalization = new TbLocalization(loader("localization_tblocalization"));
		dictionary.Add("Localization.TbLocalization", TbLocalization);
		_dataFileMap.Add(TbLocalization.GetType(), "localization_tblocalization");
		TbL10nImage = new TbL10nImage(loader("localization_tbl10nimage"));
		dictionary.Add("Localization.TbL10nImage", TbL10nImage);
		_dataFileMap.Add(TbL10nImage.GetType(), "localization_tbl10nimage");
		TbL10nLink = new TbL10nLink(loader("localization_tbl10nlink"));
		dictionary.Add("Localization.TbL10nLink", TbL10nLink);
		_dataFileMap.Add(TbL10nLink.GetType(), "localization_tbl10nlink");
		TbStaticText = new TbStaticText(loader("localization_tbstatictext"));
		dictionary.Add("Localization.TbStaticText", TbStaticText);
		_dataFileMap.Add(TbStaticText.GetType(), "localization_tbstatictext");
		TbL10nText = new TbL10nText(loader("localization_tbl10ntext"));
		dictionary.Add("Localization.TbL10nText", TbL10nText);
		_dataFileMap.Add(TbL10nText.GetType(), "localization_tbl10ntext");
		TbTextTip = new TbTextTip(loader("localization_tbtexttip"));
		dictionary.Add("Localization.TbTextTip", TbTextTip);
		_dataFileMap.Add(TbTextTip.GetType(), "localization_tbtexttip");
		TbGlobalParameter = new TbGlobalParameter(loader("settings_tbglobalparameter"));
		dictionary.Add("Settings.TbGlobalParameter", TbGlobalParameter);
		_dataFileMap.Add(TbGlobalParameter.GetType(), "settings_tbglobalparameter");
		TbTimer = new TbTimer(loader("settings_tbtimer"));
		dictionary.Add("Settings.TbTimer", TbTimer);
		_dataFileMap.Add(TbTimer.GetType(), "settings_tbtimer");
		TbUserSetting = new TbUserSetting(loader("settings_tbusersetting"));
		dictionary.Add("Settings.TbUserSetting", TbUserSetting);
		_dataFileMap.Add(TbUserSetting.GetType(), "settings_tbusersetting");
		TbUserSettingGroup = new TbUserSettingGroup(loader("settings_tbusersettinggroup"));
		dictionary.Add("Settings.TbUserSettingGroup", TbUserSettingGroup);
		_dataFileMap.Add(TbUserSettingGroup.GetType(), "settings_tbusersettinggroup");
		TbDialogueOption = new TbDialogueOption(loader("dialogue_tbdialogueoption"));
		dictionary.Add("Dialogue.TbDialogueOption", TbDialogueOption);
		_dataFileMap.Add(TbDialogueOption.GetType(), "dialogue_tbdialogueoption");
		TbDialogueTextStyle = new TbDialogueTextStyle(loader("dialogue_tbdialoguetextstyle"));
		dictionary.Add("Dialogue.TbDialogueTextStyle", TbDialogueTextStyle);
		_dataFileMap.Add(TbDialogueTextStyle.GetType(), "dialogue_tbdialoguetextstyle");
		TbDialogueEntity = new TbDialogueEntity(loader("dialogue_tbdialogueentity"));
		dictionary.Add("Dialogue.TbDialogueEntity", TbDialogueEntity);
		_dataFileMap.Add(TbDialogueEntity.GetType(), "dialogue_tbdialogueentity");
		TbRebindAction = new TbRebindAction(loader("settings_tbrebindaction"));
		dictionary.Add("Settings.TbRebindAction", TbRebindAction);
		_dataFileMap.Add(TbRebindAction.GetType(), "settings_tbrebindaction");
		TbGameKeyIcon = new TbGameKeyIcon(loader("ui_tbgamekeyicon"));
		dictionary.Add("UI.TbGameKeyIcon", TbGameKeyIcon);
		_dataFileMap.Add(TbGameKeyIcon.GetType(), "ui_tbgamekeyicon");
		TbGameCombinedKeyIcon = new TbGameCombinedKeyIcon(loader("ui_tbgamecombinedkeyicon"));
		dictionary.Add("UI.TbGameCombinedKeyIcon", TbGameCombinedKeyIcon);
		_dataFileMap.Add(TbGameCombinedKeyIcon.GetType(), "ui_tbgamecombinedkeyicon");
		TbGameKeyAction = new TbGameKeyAction(loader("ui_tbgamekeyaction"));
		dictionary.Add("UI.TbGameKeyAction", TbGameKeyAction);
		_dataFileMap.Add(TbGameKeyAction.GetType(), "ui_tbgamekeyaction");
		TbMainMenu = new TbMainMenu(loader("ui_tbmainmenu"));
		dictionary.Add("UI.TbMainMenu", TbMainMenu);
		_dataFileMap.Add(TbMainMenu.GetType(), "ui_tbmainmenu");
		TbEquipmentMenu = new TbEquipmentMenu(loader("ui_tbequipmentmenu"));
		dictionary.Add("UI.TbEquipmentMenu", TbEquipmentMenu);
		_dataFileMap.Add(TbEquipmentMenu.GetType(), "ui_tbequipmentmenu");
		TbCompendiumMenu = new TbCompendiumMenu(loader("ui_tbcompendiummenu"));
		dictionary.Add("UI.TbCompendiumMenu", TbCompendiumMenu);
		_dataFileMap.Add(TbCompendiumMenu.GetType(), "ui_tbcompendiummenu");
		TbEmailMenu = new TbEmailMenu(loader("ui_tbemailmenu"));
		dictionary.Add("UI.TbEmailMenu", TbEmailMenu);
		_dataFileMap.Add(TbEmailMenu.GetType(), "ui_tbemailmenu");
		TbModMenu = new TbModMenu(loader("ui_tbmodmenu"));
		dictionary.Add("UI.TbModMenu", TbModMenu);
		_dataFileMap.Add(TbModMenu.GetType(), "ui_tbmodmenu");
		TbUIEntity = new TbUIEntity(loader("ui_tbuientity"));
		dictionary.Add("UI.TbUIEntity", TbUIEntity);
		_dataFileMap.Add(TbUIEntity.GetType(), "ui_tbuientity");
		TbUIEntityGroup = new TbUIEntityGroup(loader("ui_tbuientitygroup"));
		dictionary.Add("UI.TbUIEntityGroup", TbUIEntityGroup);
		_dataFileMap.Add(TbUIEntityGroup.GetType(), "ui_tbuientitygroup");
		TbUIPooledObject = new TbUIPooledObject(loader("ui_tbuipooledobject"));
		dictionary.Add("UI.TbUIPooledObject", TbUIPooledObject);
		_dataFileMap.Add(TbUIPooledObject.GetType(), "ui_tbuipooledobject");
		TbChipDocument = new TbChipDocument(loader("archives_tbchipdocument"));
		dictionary.Add("Archives.TbChipDocument", TbChipDocument);
		_dataFileMap.Add(TbChipDocument.GetType(), "archives_tbchipdocument");
		TbPlantDocument = new TbPlantDocument(loader("archives_tbplantdocument"));
		dictionary.Add("Archives.TbPlantDocument", TbPlantDocument);
		_dataFileMap.Add(TbPlantDocument.GetType(), "archives_tbplantdocument");
		TbCharacterDocument = new TbCharacterDocument(loader("archives_tbcharacterdocument"));
		dictionary.Add("Archives.TbCharacterDocument", TbCharacterDocument);
		_dataFileMap.Add(TbCharacterDocument.GetType(), "archives_tbcharacterdocument");
		TbSpecialChipEvent = new TbSpecialChipEvent(loader("archives_tbspecialchipevent"));
		dictionary.Add("Archives.TbSpecialChipEvent", TbSpecialChipEvent);
		_dataFileMap.Add(TbSpecialChipEvent.GetType(), "archives_tbspecialchipevent");
		TbFactionType = new TbFactionType(loader("mission_tbfactiontype"));
		dictionary.Add("Mission.TbFactionType", TbFactionType);
		_dataFileMap.Add(TbFactionType.GetType(), "mission_tbfactiontype");
		TbFactionMissionType = new TbFactionMissionType(loader("mission_tbfactionmissiontype"));
		dictionary.Add("Mission.TbFactionMissionType", TbFactionMissionType);
		_dataFileMap.Add(TbFactionMissionType.GetType(), "mission_tbfactionmissiontype");
		TbFactionMission = new TbFactionMission(loader("mission_tbfactionmission"));
		dictionary.Add("Mission.TbFactionMission", TbFactionMission);
		_dataFileMap.Add(TbFactionMission.GetType(), "mission_tbfactionmission");
		TbTreatyPortFaction = new TbTreatyPortFaction(loader("mission_tbtreatyportfaction"));
		dictionary.Add("Mission.TbTreatyPortFaction", TbTreatyPortFaction);
		_dataFileMap.Add(TbTreatyPortFaction.GetType(), "mission_tbtreatyportfaction");
		TbBoardMission = new TbBoardMission(loader("mission_tbboardmission"));
		dictionary.Add("Mission.TbBoardMission", TbBoardMission);
		_dataFileMap.Add(TbBoardMission.GetType(), "mission_tbboardmission");
		TbRewardPool = new TbRewardPool(loader("mission_tbrewardpool"));
		dictionary.Add("Mission.TbRewardPool", TbRewardPool);
		_dataFileMap.Add(TbRewardPool.GetType(), "mission_tbrewardpool");
		TbBoardMissionLevel = new TbBoardMissionLevel(loader("mission_tbboardmissionlevel"));
		dictionary.Add("Mission.TbBoardMissionLevel", TbBoardMissionLevel);
		_dataFileMap.Add(TbBoardMissionLevel.GetType(), "mission_tbboardmissionlevel");
		TbBoardMissionType = new TbBoardMissionType(loader("mission_tbboardmissiontype"));
		dictionary.Add("Mission.TbBoardMissionType", TbBoardMissionType);
		_dataFileMap.Add(TbBoardMissionType.GetType(), "mission_tbboardmissiontype");
		TbMission = new TbMission(loader("mission_tbmission"));
		dictionary.Add("Mission.TbMission", TbMission);
		_dataFileMap.Add(TbMission.GetType(), "mission_tbmission");
		TbMissionType = new TbMissionType(loader("mission_tbmissiontype"));
		dictionary.Add("Mission.TbMissionType", TbMissionType);
		_dataFileMap.Add(TbMissionType.GetType(), "mission_tbmissiontype");
		TbPositionType = new TbPositionType(loader("mission_tbpositiontype"));
		dictionary.Add("Mission.TbPositionType", TbPositionType);
		_dataFileMap.Add(TbPositionType.GetType(), "mission_tbpositiontype");
		TbMissionDecorator = new TbMissionDecorator(loader("mission_tbmissiondecorator"));
		dictionary.Add("Mission.TbMissionDecorator", TbMissionDecorator);
		_dataFileMap.Add(TbMissionDecorator.GetType(), "mission_tbmissiondecorator");
		TbCustomEvent = new TbCustomEvent(loader("mission_tbcustomevent"));
		dictionary.Add("Mission.TbCustomEvent", TbCustomEvent);
		_dataFileMap.Add(TbCustomEvent.GetType(), "mission_tbcustomevent");
		TbItemSubmitCondition = new TbItemSubmitCondition(loader("mission_tbitemsubmitcondition"));
		dictionary.Add("Mission.TbItemSubmitCondition", TbItemSubmitCondition);
		_dataFileMap.Add(TbItemSubmitCondition.GetType(), "mission_tbitemsubmitcondition");
		TbItemGeneCondition = new TbItemGeneCondition(loader("mission_tbitemgenecondition"));
		dictionary.Add("Mission.TbItemGeneCondition", TbItemGeneCondition);
		_dataFileMap.Add(TbItemGeneCondition.GetType(), "mission_tbitemgenecondition");
		TbEmail = new TbEmail(loader("email_tbemail"));
		dictionary.Add("Email.TbEmail", TbEmail);
		_dataFileMap.Add(TbEmail.GetType(), "email_tbemail");
		TbScene = new TbScene(loader("room_tbscene"));
		dictionary.Add("Room.TbScene", TbScene);
		_dataFileMap.Add(TbScene.GetType(), "room_tbscene");
		TbBackgroundHighLevel = new TbBackgroundHighLevel(loader("room_tbbackgroundhighlevel"));
		dictionary.Add("Room.TbBackgroundHighLevel", TbBackgroundHighLevel);
		_dataFileMap.Add(TbBackgroundHighLevel.GetType(), "room_tbbackgroundhighlevel");
		TbMarkPoint = new TbMarkPoint(loader("room_tbmarkpoint"));
		dictionary.Add("Room.TbMarkPoint", TbMarkPoint);
		_dataFileMap.Add(TbMarkPoint.GetType(), "room_tbmarkpoint");
		TbPortal = new TbPortal(loader("room_tbportal"));
		dictionary.Add("Room.TbPortal", TbPortal);
		_dataFileMap.Add(TbPortal.GetType(), "room_tbportal");
		TbInwalkableArea = new TbInwalkableArea(loader("room_tbinwalkablearea"));
		dictionary.Add("Room.TbInwalkableArea", TbInwalkableArea);
		_dataFileMap.Add(TbInwalkableArea.GetType(), "room_tbinwalkablearea");
		TbMapArea = new TbMapArea(loader("room_tbmaparea"));
		dictionary.Add("Room.TbMapArea", TbMapArea);
		_dataFileMap.Add(TbMapArea.GetType(), "room_tbmaparea");
		TbMapRoom = new TbMapRoom(loader("room_tbmaproom"));
		dictionary.Add("Room.TbMapRoom", TbMapRoom);
		_dataFileMap.Add(TbMapRoom.GetType(), "room_tbmaproom");
		TbMapRoomType = new TbMapRoomType(loader("room_tbmaproomtype"));
		dictionary.Add("Room.TbMapRoomType", TbMapRoomType);
		_dataFileMap.Add(TbMapRoomType.GetType(), "room_tbmaproomtype");
		TbMapType = new TbMapType(loader("room_tbmaptype"));
		dictionary.Add("Room.TbMapType", TbMapType);
		_dataFileMap.Add(TbMapType.GetType(), "room_tbmaptype");
		TbStation = new TbStation(loader("room_tbstation"));
		dictionary.Add("Room.TbStation", TbStation);
		_dataFileMap.Add(TbStation.GetType(), "room_tbstation");
		TbLockableObject = new TbLockableObject(loader("room_tblockableobject"));
		dictionary.Add("Room.TbLockableObject", TbLockableObject);
		_dataFileMap.Add(TbLockableObject.GetType(), "room_tblockableobject");
		TbRoom = new TbRoom(loader("room_tbroom"));
		dictionary.Add("Room.TbRoom", TbRoom);
		_dataFileMap.Add(TbRoom.GetType(), "room_tbroom");
		TbDialogueObject = new TbDialogueObject(loader("room_tbdialogueobject"));
		dictionary.Add("Room.TbDialogueObject", TbDialogueObject);
		_dataFileMap.Add(TbDialogueObject.GetType(), "room_tbdialogueobject");
		TbTrashTalk = new TbTrashTalk(loader("room_tbtrashtalk"));
		dictionary.Add("Room.TbTrashTalk", TbTrashTalk);
		_dataFileMap.Add(TbTrashTalk.GetType(), "room_tbtrashtalk");
		TbItem = new TbItem(loader("item_tbitem"));
		dictionary.Add("Item.TbItem", TbItem);
		_dataFileMap.Add(TbItem.GetType(), "item_tbitem");
		TbItemOrder = new TbItemOrder(loader("item_tbitemorder"));
		dictionary.Add("Item.TbItemOrder", TbItemOrder);
		_dataFileMap.Add(TbItemOrder.GetType(), "item_tbitemorder");
		TbItemMainType = new TbItemMainType(loader("item_tbitemmaintype"));
		dictionary.Add("Item.TbItemMainType", TbItemMainType);
		_dataFileMap.Add(TbItemMainType.GetType(), "item_tbitemmaintype");
		TbItemSubType = new TbItemSubType(loader("item_tbitemsubtype"));
		dictionary.Add("Item.TbItemSubType", TbItemSubType);
		_dataFileMap.Add(TbItemSubType.GetType(), "item_tbitemsubtype");
		TbItemAutomationType = new TbItemAutomationType(loader("item_tbitemautomationtype"));
		dictionary.Add("Item.TbItemAutomationType", TbItemAutomationType);
		_dataFileMap.Add(TbItemAutomationType.GetType(), "item_tbitemautomationtype");
		TbItemSource = new TbItemSource(loader("item_tbitemsource"));
		dictionary.Add("Item.TbItemSource", TbItemSource);
		_dataFileMap.Add(TbItemSource.GetType(), "item_tbitemsource");
		TbEatingEffect = new TbEatingEffect(loader("item_tbeatingeffect"));
		dictionary.Add("Item.TbEatingEffect", TbEatingEffect);
		_dataFileMap.Add(TbEatingEffect.GetType(), "item_tbeatingeffect");
		TbMissionItem = new TbMissionItem(loader("item_tbmissionitem"));
		dictionary.Add("Item.TbMissionItem", TbMissionItem);
		_dataFileMap.Add(TbMissionItem.GetType(), "item_tbmissionitem");
		TbToolOverride = new TbToolOverride(loader("item_tbtooloverride"));
		dictionary.Add("Item.TbToolOverride", TbToolOverride);
		_dataFileMap.Add(TbToolOverride.GetType(), "item_tbtooloverride");
		TbCountItemList = new TbCountItemList(loader("item_tbcountitemlist"));
		dictionary.Add("Item.TbCountItemList", TbCountItemList);
		_dataFileMap.Add(TbCountItemList.GetType(), "item_tbcountitemlist");
		TbRangedItemList = new TbRangedItemList(loader("item_tbrangeditemlist"));
		dictionary.Add("Item.TbRangedItemList", TbRangedItemList);
		_dataFileMap.Add(TbRangedItemList.GetType(), "item_tbrangeditemlist");
		TbRecipe = new TbRecipe(loader("recipe_tbrecipe"));
		dictionary.Add("Recipe.TbRecipe", TbRecipe);
		_dataFileMap.Add(TbRecipe.GetType(), "recipe_tbrecipe");
		TbRecipeGroup = new TbRecipeGroup(loader("recipe_tbrecipegroup"));
		dictionary.Add("Recipe.TbRecipeGroup", TbRecipeGroup);
		_dataFileMap.Add(TbRecipeGroup.GetType(), "recipe_tbrecipegroup");
		TbRecipeMainType = new TbRecipeMainType(loader("recipe_tbrecipemaintype"));
		dictionary.Add("Recipe.TbRecipeMainType", TbRecipeMainType);
		_dataFileMap.Add(TbRecipeMainType.GetType(), "recipe_tbrecipemaintype");
		TbRecipeSubType = new TbRecipeSubType(loader("recipe_tbrecipesubtype"));
		dictionary.Add("Recipe.TbRecipeSubType", TbRecipeSubType);
		_dataFileMap.Add(TbRecipeSubType.GetType(), "recipe_tbrecipesubtype");
		TbIngredientGroup = new TbIngredientGroup(loader("recipe_tbingredientgroup"));
		dictionary.Add("Recipe.TbIngredientGroup", TbIngredientGroup);
		_dataFileMap.Add(TbIngredientGroup.GetType(), "recipe_tbingredientgroup");
		TbDish = new TbDish(loader("recipe_tbdish"));
		dictionary.Add("Recipe.TbDish", TbDish);
		_dataFileMap.Add(TbDish.GetType(), "recipe_tbdish");
		TbDishGroup = new TbDishGroup(loader("recipe_tbdishgroup"));
		dictionary.Add("Recipe.TbDishGroup", TbDishGroup);
		_dataFileMap.Add(TbDishGroup.GetType(), "recipe_tbdishgroup");
		TbDismantleRecipe = new TbDismantleRecipe(loader("recipe_tbdismantlerecipe"));
		dictionary.Add("Recipe.TbDismantleRecipe", TbDismantleRecipe);
		_dataFileMap.Add(TbDismantleRecipe.GetType(), "recipe_tbdismantlerecipe");
		TbDismantleRecipeGroup = new TbDismantleRecipeGroup(loader("recipe_tbdismantlerecipegroup"));
		dictionary.Add("Recipe.TbDismantleRecipeGroup", TbDismantleRecipeGroup);
		_dataFileMap.Add(TbDismantleRecipeGroup.GetType(), "recipe_tbdismantlerecipegroup");
		TbBuilding = new TbBuilding(loader("building_tbbuilding"));
		dictionary.Add("Building.TbBuilding", TbBuilding);
		_dataFileMap.Add(TbBuilding.GetType(), "building_tbbuilding");
		TbBuildingExterior = new TbBuildingExterior(loader("building_tbbuildingexterior"));
		dictionary.Add("Building.TbBuildingExterior", TbBuildingExterior);
		_dataFileMap.Add(TbBuildingExterior.GetType(), "building_tbbuildingexterior");
		TbBuildingWallpaper = new TbBuildingWallpaper(loader("building_tbbuildingwallpaper"));
		dictionary.Add("Building.TbBuildingWallpaper", TbBuildingWallpaper);
		_dataFileMap.Add(TbBuildingWallpaper.GetType(), "building_tbbuildingwallpaper");
		TbRoomEffect = new TbRoomEffect(loader("room_tbroomeffect"));
		dictionary.Add("Room.TbRoomEffect", TbRoomEffect);
		_dataFileMap.Add(TbRoomEffect.GetType(), "room_tbroomeffect");
		TbPlatform = new TbPlatform(loader("platform_tbplatform"));
		dictionary.Add("Platform.TbPlatform", TbPlatform);
		_dataFileMap.Add(TbPlatform.GetType(), "platform_tbplatform");
		TbBuildingSupport = new TbBuildingSupport(loader("platform_tbbuildingsupport"));
		dictionary.Add("Platform.TbBuildingSupport", TbBuildingSupport);
		_dataFileMap.Add(TbBuildingSupport.GetType(), "platform_tbbuildingsupport");
		TbEquipment = new TbEquipment(loader("equipment_tbequipment"));
		dictionary.Add("Equipment.TbEquipment", TbEquipment);
		_dataFileMap.Add(TbEquipment.GetType(), "equipment_tbequipment");
		TbChair = new TbChair(loader("equipment_tbchair"));
		dictionary.Add("Equipment.TbChair", TbChair);
		_dataFileMap.Add(TbChair.GetType(), "equipment_tbchair");
		TbLamp = new TbLamp(loader("equipment_tblamp"));
		dictionary.Add("Equipment.TbLamp", TbLamp);
		_dataFileMap.Add(TbLamp.GetType(), "equipment_tblamp");
		TbItemPlaceCondition = new TbItemPlaceCondition(loader("equipment_tbitemplacecondition"));
		dictionary.Add("Equipment.TbItemPlaceCondition", TbItemPlaceCondition);
		_dataFileMap.Add(TbItemPlaceCondition.GetType(), "equipment_tbitemplacecondition");
		TbLikingLevelMap = new TbLikingLevelMap(loader("npc_tblikinglevelmap"));
		dictionary.Add("NPC.TbLikingLevelMap", TbLikingLevelMap);
		_dataFileMap.Add(TbLikingLevelMap.GetType(), "npc_tblikinglevelmap");
		TbNpcLiking = new TbNpcLiking(loader("npc_tbnpcliking"));
		dictionary.Add("NPC.TbNpcLiking", TbNpcLiking);
		_dataFileMap.Add(TbNpcLiking.GetType(), "npc_tbnpcliking");
		TbNpc = new TbNpc(loader("npc_tbnpc"));
		dictionary.Add("NPC.TbNpc", TbNpc);
		_dataFileMap.Add(TbNpc.GetType(), "npc_tbnpc");
		TbNpcDocument = new TbNpcDocument(loader("npc_tbnpcdocument"));
		dictionary.Add("NPC.TbNpcDocument", TbNpcDocument);
		_dataFileMap.Add(TbNpcDocument.GetType(), "npc_tbnpcdocument");
		TbIdleTalkNode = new TbIdleTalkNode(loader("npc_tbidletalknode"));
		dictionary.Add("NPC.TbIdleTalkNode", TbIdleTalkNode);
		_dataFileMap.Add(TbIdleTalkNode.GetType(), "npc_tbidletalknode");
		TbBuff = new TbBuff(loader("buff_tbbuff"));
		dictionary.Add("Buff.TbBuff", TbBuff);
		_dataFileMap.Add(TbBuff.GetType(), "buff_tbbuff");
		TbSpiritThreshold = new TbSpiritThreshold(loader("buff_tbspiritthreshold"));
		dictionary.Add("Buff.TbSpiritThreshold", TbSpiritThreshold);
		_dataFileMap.Add(TbSpiritThreshold.GetType(), "buff_tbspiritthreshold");
		TbRecoveryDecay = new TbRecoveryDecay(loader("buff_tbrecoverydecay"));
		dictionary.Add("Buff.TbRecoveryDecay", TbRecoveryDecay);
		_dataFileMap.Add(TbRecoveryDecay.GetType(), "buff_tbrecoverydecay");
		TbFootStep = new TbFootStep(loader("sound_tbfootstep"));
		dictionary.Add("Sound.TbFootStep", TbFootStep);
		_dataFileMap.Add(TbFootStep.GetType(), "sound_tbfootstep");
		TbMaterialSound = new TbMaterialSound(loader("sound_tbmaterialsound"));
		dictionary.Add("Sound.TbMaterialSound", TbMaterialSound);
		_dataFileMap.Add(TbMaterialSound.GetType(), "sound_tbmaterialsound");
		TbBgm = new TbBgm(loader("sound_tbbgm"));
		dictionary.Add("Sound.TbBgm", TbBgm);
		_dataFileMap.Add(TbBgm.GetType(), "sound_tbbgm");
		TbAmbience = new TbAmbience(loader("sound_tbambience"));
		dictionary.Add("Sound.TbAmbience", TbAmbience);
		_dataFileMap.Add(TbAmbience.GetType(), "sound_tbambience");
		TbExclusiveSFX = new TbExclusiveSFX(loader("sound_tbexclusivesfx"));
		dictionary.Add("Sound.TbExclusiveSFX", TbExclusiveSFX);
		_dataFileMap.Add(TbExclusiveSFX.GetType(), "sound_tbexclusivesfx");
		TbStore = new TbStore(loader("store_tbstore"));
		dictionary.Add("Store.TbStore", TbStore);
		_dataFileMap.Add(TbStore.GetType(), "store_tbstore");
		TbStorePriceScale = new TbStorePriceScale(loader("store_tbstorepricescale"));
		dictionary.Add("Store.TbStorePriceScale", TbStorePriceScale);
		_dataFileMap.Add(TbStorePriceScale.GetType(), "store_tbstorepricescale");
		TbStoreItemList = new TbStoreItemList(loader("store_tbstoreitemlist"));
		dictionary.Add("Store.TbStoreItemList", TbStoreItemList);
		_dataFileMap.Add(TbStoreItemList.GetType(), "store_tbstoreitemlist");
		TbExchangeStore = new TbExchangeStore(loader("store_tbexchangestore"));
		dictionary.Add("Store.TbExchangeStore", TbExchangeStore);
		_dataFileMap.Add(TbExchangeStore.GetType(), "store_tbexchangestore");
		TbStoreItemUnlock = new TbStoreItemUnlock(loader("store_tbstoreitemunlock"));
		dictionary.Add("Store.TbStoreItemUnlock", TbStoreItemUnlock);
		_dataFileMap.Add(TbStoreItemUnlock.GetType(), "store_tbstoreitemunlock");
		TbSeed = new TbSeed(loader("plant_tbseed"));
		dictionary.Add("Plant.TbSeed", TbSeed);
		_dataFileMap.Add(TbSeed.GetType(), "plant_tbseed");
		TbSeedType = new TbSeedType(loader("plant_tbseedtype"));
		dictionary.Add("Plant.TbSeedType", TbSeedType);
		_dataFileMap.Add(TbSeedType.GetType(), "plant_tbseedtype");
		TbSeedUnlock = new TbSeedUnlock(loader("plant_tbseedunlock"));
		dictionary.Add("Plant.TbSeedUnlock", TbSeedUnlock);
		_dataFileMap.Add(TbSeedUnlock.GetType(), "plant_tbseedunlock");
		TbTreeSeed = new TbTreeSeed(loader("plant_tbtreeseed"));
		dictionary.Add("Plant.TbTreeSeed", TbTreeSeed);
		_dataFileMap.Add(TbTreeSeed.GetType(), "plant_tbtreeseed");
		TbCropGene = new TbCropGene(loader("plant_tbcropgene"));
		dictionary.Add("Plant.TbCropGene", TbCropGene);
		_dataFileMap.Add(TbCropGene.GetType(), "plant_tbcropgene");
		TbCropGeneMatrix = new TbCropGeneMatrix(loader("plant_tbcropgenematrix"));
		dictionary.Add("Plant.TbCropGeneMatrix", TbCropGeneMatrix);
		_dataFileMap.Add(TbCropGeneMatrix.GetType(), "plant_tbcropgenematrix");
		TbWeather = new TbWeather(loader("weather_tbweather"));
		dictionary.Add("Weather.TbWeather", TbWeather);
		_dataFileMap.Add(TbWeather.GetType(), "weather_tbweather");
		TbDayPeriod = new TbDayPeriod(loader("time_tbdayperiod"));
		dictionary.Add("Time.TbDayPeriod", TbDayPeriod);
		_dataFileMap.Add(TbDayPeriod.GetType(), "time_tbdayperiod");
		TbSeason = new TbSeason(loader("time_tbseason"));
		dictionary.Add("Time.TbSeason", TbSeason);
		_dataFileMap.Add(TbSeason.GetType(), "time_tbseason");
		TbSeasonWeather = new TbSeasonWeather(loader("time_tbseasonweather"));
		dictionary.Add("Time.TbSeasonWeather", TbSeasonWeather);
		_dataFileMap.Add(TbSeasonWeather.GetType(), "time_tbseasonweather");
		TbDungeonSeason = new TbDungeonSeason(loader("time_tbdungeonseason"));
		dictionary.Add("Time.TbDungeonSeason", TbDungeonSeason);
		_dataFileMap.Add(TbDungeonSeason.GetType(), "time_tbdungeonseason");
		TbTechTree = new TbTechTree(loader("techtree_tbtechtree"));
		dictionary.Add("TechTree.TbTechTree", TbTechTree);
		_dataFileMap.Add(TbTechTree.GetType(), "techtree_tbtechtree");
		TbTechNode = new TbTechNode(loader("techtree_tbtechnode"));
		dictionary.Add("TechTree.TbTechNode", TbTechNode);
		_dataFileMap.Add(TbTechNode.GetType(), "techtree_tbtechnode");
		TbTechPoint = new TbTechPoint(loader("techtree_tbtechpoint"));
		dictionary.Add("TechTree.TbTechPoint", TbTechPoint);
		_dataFileMap.Add(TbTechPoint.GetType(), "techtree_tbtechpoint");
		TbMonster = new TbMonster(loader("monster_tbmonster"));
		dictionary.Add("Monster.TbMonster", TbMonster);
		_dataFileMap.Add(TbMonster.GetType(), "monster_tbmonster");
		TbMonsterDocument = new TbMonsterDocument(loader("monster_tbmonsterdocument"));
		dictionary.Add("Monster.TbMonsterDocument", TbMonsterDocument);
		_dataFileMap.Add(TbMonsterDocument.GetType(), "monster_tbmonsterdocument");
		TbEnvObject = new TbEnvObject(loader("resource_tbenvobject"));
		dictionary.Add("Resource.TbEnvObject", TbEnvObject);
		_dataFileMap.Add(TbEnvObject.GetType(), "resource_tbenvobject");
		TbVegetation = new TbVegetation(loader("resource_tbvegetation"));
		dictionary.Add("Resource.TbVegetation", TbVegetation);
		_dataFileMap.Add(TbVegetation.GetType(), "resource_tbvegetation");
		TbResource = new TbResource(loader("resource_tbresource"));
		dictionary.Add("Resource.TbResource", TbResource);
		_dataFileMap.Add(TbResource.GetType(), "resource_tbresource");
		TbResourceType = new TbResourceType(loader("resource_tbresourcetype"));
		dictionary.Add("Resource.TbResourceType", TbResourceType);
		_dataFileMap.Add(TbResourceType.GetType(), "resource_tbresourcetype");
		TbResinCollectorOutput = new TbResinCollectorOutput(loader("resource_tbresincollectoroutput"));
		dictionary.Add("Resource.TbResinCollectorOutput", TbResinCollectorOutput);
		_dataFileMap.Add(TbResinCollectorOutput.GetType(), "resource_tbresincollectoroutput");
		TbResourceDocument = new TbResourceDocument(loader("resource_tbresourcedocument"));
		dictionary.Add("Resource.TbResourceDocument", TbResourceDocument);
		_dataFileMap.Add(TbResourceDocument.GetType(), "resource_tbresourcedocument");
		TbMonsterSpawn = new TbMonsterSpawn(loader("monster_tbmonsterspawn"));
		dictionary.Add("Monster.TbMonsterSpawn", TbMonsterSpawn);
		_dataFileMap.Add(TbMonsterSpawn.GetType(), "monster_tbmonsterspawn");
		TbItemSpawn = new TbItemSpawn(loader("item_tbitemspawn"));
		dictionary.Add("Item.TbItemSpawn", TbItemSpawn);
		_dataFileMap.Add(TbItemSpawn.GetType(), "item_tbitemspawn");
		TbResourceSpawn = new TbResourceSpawn(loader("resource_tbresourcespawn"));
		dictionary.Add("Resource.TbResourceSpawn", TbResourceSpawn);
		_dataFileMap.Add(TbResourceSpawn.GetType(), "resource_tbresourcespawn");
		TbVegetationSpawn = new TbVegetationSpawn(loader("resource_tbvegetationspawn"));
		dictionary.Add("Resource.TbVegetationSpawn", TbVegetationSpawn);
		_dataFileMap.Add(TbVegetationSpawn.GetType(), "resource_tbvegetationspawn");
		TbEnvObjectSpawn = new TbEnvObjectSpawn(loader("resource_tbenvobjectspawn"));
		dictionary.Add("Resource.TbEnvObjectSpawn", TbEnvObjectSpawn);
		_dataFileMap.Add(TbEnvObjectSpawn.GetType(), "resource_tbenvobjectspawn");
		TbGlobalGuaranteed = new TbGlobalGuaranteed(loader("resource_tbglobalguaranteed"));
		dictionary.Add("Resource.TbGlobalGuaranteed", TbGlobalGuaranteed);
		_dataFileMap.Add(TbGlobalGuaranteed.GetType(), "resource_tbglobalguaranteed");
		TbFarmLevel = new TbFarmLevel(loader("player_tbfarmlevel"));
		dictionary.Add("Player.TbFarmLevel", TbFarmLevel);
		_dataFileMap.Add(TbFarmLevel.GetType(), "player_tbfarmlevel");
		TbBackpackLevel = new TbBackpackLevel(loader("player_tbbackpacklevel"));
		dictionary.Add("Player.TbBackpackLevel", TbBackpackLevel);
		_dataFileMap.Add(TbBackpackLevel.GetType(), "player_tbbackpacklevel");
		TbHat = new TbHat(loader("player_tbhat"));
		dictionary.Add("Player.TbHat", TbHat);
		_dataFileMap.Add(TbHat.GetType(), "player_tbhat");
		TbAgentEquipmentSkill = new TbAgentEquipmentSkill(loader("player_tbagentequipmentskill"));
		dictionary.Add("Player.TbAgentEquipmentSkill", TbAgentEquipmentSkill);
		_dataFileMap.Add(TbAgentEquipmentSkill.GetType(), "player_tbagentequipmentskill");
		TbPlayerAnimationFrame = new TbPlayerAnimationFrame(loader("player_tbplayeranimationframe"));
		dictionary.Add("Player.TbPlayerAnimationFrame", TbPlayerAnimationFrame);
		_dataFileMap.Add(TbPlayerAnimationFrame.GetType(), "player_tbplayeranimationframe");
		TbFish = new TbFish(loader("fishing_tbfish"));
		dictionary.Add("Fishing.TbFish", TbFish);
		_dataFileMap.Add(TbFish.GetType(), "fishing_tbfish");
		TbFishingPool = new TbFishingPool(loader("fishing_tbfishingpool"));
		dictionary.Add("Fishing.TbFishingPool", TbFishingPool);
		_dataFileMap.Add(TbFishingPool.GetType(), "fishing_tbfishingpool");
		TbFarmFish = new TbFarmFish(loader("fishing_tbfarmfish"));
		dictionary.Add("Fishing.TbFarmFish", TbFarmFish);
		_dataFileMap.Add(TbFarmFish.GetType(), "fishing_tbfarmfish");
		TbFarmFishFormation = new TbFarmFishFormation(loader("fishing_tbfarmfishformation"));
		dictionary.Add("Fishing.TbFarmFishFormation", TbFarmFishFormation);
		_dataFileMap.Add(TbFarmFishFormation.GetType(), "fishing_tbfarmfishformation");
		TbFishDocument = new TbFishDocument(loader("fishing_tbfishdocument"));
		dictionary.Add("Fishing.TbFishDocument", TbFishDocument);
		_dataFileMap.Add(TbFishDocument.GetType(), "fishing_tbfishdocument");
		TbFishFeed = new TbFishFeed(loader("fishing_tbfishfeed"));
		dictionary.Add("Fishing.TbFishFeed", TbFishFeed);
		_dataFileMap.Add(TbFishFeed.GetType(), "fishing_tbfishfeed");
		TbDroneStructure = new TbDroneStructure(loader("drone_tbdronestructure"));
		dictionary.Add("Drone.TbDroneStructure", TbDroneStructure);
		_dataFileMap.Add(TbDroneStructure.GetType(), "drone_tbdronestructure");
		TbDroneWeapon = new TbDroneWeapon(loader("drone_tbdroneweapon"));
		dictionary.Add("Drone.TbDroneWeapon", TbDroneWeapon);
		_dataFileMap.Add(TbDroneWeapon.GetType(), "drone_tbdroneweapon");
		TbDroneChip = new TbDroneChip(loader("drone_tbdronechip"));
		dictionary.Add("Drone.TbDroneChip", TbDroneChip);
		_dataFileMap.Add(TbDroneChip.GetType(), "drone_tbdronechip");
		TbDroneEngine = new TbDroneEngine(loader("drone_tbdroneengine"));
		dictionary.Add("Drone.TbDroneEngine", TbDroneEngine);
		_dataFileMap.Add(TbDroneEngine.GetType(), "drone_tbdroneengine");
		TbDroneAssist = new TbDroneAssist(loader("drone_tbdroneassist"));
		dictionary.Add("Drone.TbDroneAssist", TbDroneAssist);
		_dataFileMap.Add(TbDroneAssist.GetType(), "drone_tbdroneassist");
		TbDroneSkill = new TbDroneSkill(loader("drone_tbdroneskill"));
		dictionary.Add("Drone.TbDroneSkill", TbDroneSkill);
		_dataFileMap.Add(TbDroneSkill.GetType(), "drone_tbdroneskill");
		TbDroneSlot = new TbDroneSlot(loader("drone_tbdroneslot"));
		dictionary.Add("Drone.TbDroneSlot", TbDroneSlot);
		_dataFileMap.Add(TbDroneSlot.GetType(), "drone_tbdroneslot");
		TbEnvOptimizerPoint = new TbEnvOptimizerPoint(loader("envoptimizer_tbenvoptimizerpoint"));
		dictionary.Add("EnvOptimizer.TbEnvOptimizerPoint", TbEnvOptimizerPoint);
		_dataFileMap.Add(TbEnvOptimizerPoint.GetType(), "envoptimizer_tbenvoptimizerpoint");
		TbEnvOptimizerBranch = new TbEnvOptimizerBranch(loader("envoptimizer_tbenvoptimizerbranch"));
		dictionary.Add("EnvOptimizer.TbEnvOptimizerBranch", TbEnvOptimizerBranch);
		_dataFileMap.Add(TbEnvOptimizerBranch.GetType(), "envoptimizer_tbenvoptimizerbranch");
		TbEnvOptimizerSlot = new TbEnvOptimizerSlot(loader("envoptimizer_tbenvoptimizerslot"));
		dictionary.Add("EnvOptimizer.TbEnvOptimizerSlot", TbEnvOptimizerSlot);
		_dataFileMap.Add(TbEnvOptimizerSlot.GetType(), "envoptimizer_tbenvoptimizerslot");
		TbAnimal = new TbAnimal(loader("animal_tbanimal"));
		dictionary.Add("Animal.TbAnimal", TbAnimal);
		_dataFileMap.Add(TbAnimal.GetType(), "animal_tbanimal");
		TbFeed = new TbFeed(loader("animal_tbfeed"));
		dictionary.Add("Animal.TbFeed", TbFeed);
		_dataFileMap.Add(TbFeed.GetType(), "animal_tbfeed");
		TbHusbandryEnergy = new TbHusbandryEnergy(loader("animal_tbhusbandryenergy"));
		dictionary.Add("Animal.TbHusbandryEnergy", TbHusbandryEnergy);
		_dataFileMap.Add(TbHusbandryEnergy.GetType(), "animal_tbhusbandryenergy");
		TbHusbandry = new TbHusbandry(loader("animal_tbhusbandry"));
		dictionary.Add("Animal.TbHusbandry", TbHusbandry);
		_dataFileMap.Add(TbHusbandry.GetType(), "animal_tbhusbandry");
		TbAnimalState = new TbAnimalState(loader("animal_tbanimalstate"));
		dictionary.Add("Animal.TbAnimalState", TbAnimalState);
		_dataFileMap.Add(TbAnimalState.GetType(), "animal_tbanimalstate");
		TbAnimalDocument = new TbAnimalDocument(loader("animal_tbanimaldocument"));
		dictionary.Add("Animal.TbAnimalDocument", TbAnimalDocument);
		_dataFileMap.Add(TbAnimalDocument.GetType(), "animal_tbanimaldocument");
		TbCalendar = new TbCalendar(loader("calendar_tbcalendar"));
		dictionary.Add("Calendar.TbCalendar", TbCalendar);
		_dataFileMap.Add(TbCalendar.GetType(), "calendar_tbcalendar");
		TbDateEvent = new TbDateEvent(loader("calendar_tbdateevent"));
		dictionary.Add("Calendar.TbDateEvent", TbDateEvent);
		_dataFileMap.Add(TbDateEvent.GetType(), "calendar_tbdateevent");
		TbFestival = new TbFestival(loader("festival_tbfestival"));
		dictionary.Add("Festival.TbFestival", TbFestival);
		_dataFileMap.Add(TbFestival.GetType(), "festival_tbfestival");
		TbAutomateBotAppearance = new TbAutomateBotAppearance(loader("automate_tbautomatebotappearance"));
		dictionary.Add("Automate.TbAutomateBotAppearance", TbAutomateBotAppearance);
		_dataFileMap.Add(TbAutomateBotAppearance.GetType(), "automate_tbautomatebotappearance");
		TbAutomateBotPerformance = new TbAutomateBotPerformance(loader("automate_tbautomatebotperformance"));
		dictionary.Add("Automate.TbAutomateBotPerformance", TbAutomateBotPerformance);
		_dataFileMap.Add(TbAutomateBotPerformance.GetType(), "automate_tbautomatebotperformance");
		TbAutomateBot = new TbAutomateBot(loader("automate_tbautomatebot"));
		dictionary.Add("Automate.TbAutomateBot", TbAutomateBot);
		_dataFileMap.Add(TbAutomateBot.GetType(), "automate_tbautomatebot");
		TbModImageSetting = new TbModImageSetting(loader("mod_tbmodimagesetting"));
		dictionary.Add("Mod.TbModImageSetting", TbModImageSetting);
		_dataFileMap.Add(TbModImageSetting.GetType(), "mod_tbmodimagesetting");
		TbModRecipeGroupExtension = new TbModRecipeGroupExtension(loader("mod_tbmodrecipegroupextension"));
		dictionary.Add("Mod.TbModRecipeGroupExtension", TbModRecipeGroupExtension);
		_dataFileMap.Add(TbModRecipeGroupExtension.GetType(), "mod_tbmodrecipegroupextension");
		TbModStoreExtension = new TbModStoreExtension(loader("mod_tbmodstoreextension"));
		dictionary.Add("Mod.TbModStoreExtension", TbModStoreExtension);
		_dataFileMap.Add(TbModStoreExtension.GetType(), "mod_tbmodstoreextension");
		TbModExchangeStoreExtension = new TbModExchangeStoreExtension(loader("mod_tbmodexchangestoreextension"));
		dictionary.Add("Mod.TbModExchangeStoreExtension", TbModExchangeStoreExtension);
		_dataFileMap.Add(TbModExchangeStoreExtension.GetType(), "mod_tbmodexchangestoreextension");
		TbModResourceSpawnExtension = new TbModResourceSpawnExtension(loader("mod_tbmodresourcespawnextension"));
		dictionary.Add("Mod.TbModResourceSpawnExtension", TbModResourceSpawnExtension);
		_dataFileMap.Add(TbModResourceSpawnExtension.GetType(), "mod_tbmodresourcespawnextension");
		TbModVegetationSpawnExtension = new TbModVegetationSpawnExtension(loader("mod_tbmodvegetationspawnextension"));
		dictionary.Add("Mod.TbModVegetationSpawnExtension", TbModVegetationSpawnExtension);
		_dataFileMap.Add(TbModVegetationSpawnExtension.GetType(), "mod_tbmodvegetationspawnextension");
		TbModItemSpawnExtension = new TbModItemSpawnExtension(loader("mod_tbmoditemspawnextension"));
		dictionary.Add("Mod.TbModItemSpawnExtension", TbModItemSpawnExtension);
		_dataFileMap.Add(TbModItemSpawnExtension.GetType(), "mod_tbmoditemspawnextension");
		TbModIngredientGroupExtension = new TbModIngredientGroupExtension(loader("mod_tbmodingredientgroupextension"));
		dictionary.Add("Mod.TbModIngredientGroupExtension", TbModIngredientGroupExtension);
		_dataFileMap.Add(TbModIngredientGroupExtension.GetType(), "mod_tbmodingredientgroupextension");
		TbModDishGroupExtension = new TbModDishGroupExtension(loader("mod_tbmoddishgroupextension"));
		dictionary.Add("Mod.TbModDishGroupExtension", TbModDishGroupExtension);
		_dataFileMap.Add(TbModDishGroupExtension.GetType(), "mod_tbmoddishgroupextension");
		TbModFishingPoolExtension = new TbModFishingPoolExtension(loader("mod_tbmodfishingpoolextension"));
		dictionary.Add("Mod.TbModFishingPoolExtension", TbModFishingPoolExtension);
		_dataFileMap.Add(TbModFishingPoolExtension.GetType(), "mod_tbmodfishingpoolextension");
		PostInit();
		TbTextMapperZH_CN.Resolve(dictionary);
		TbTextMapperEN.Resolve(dictionary);
		TbTextMapperZH_TW.Resolve(dictionary);
		TbTextMapperJA.Resolve(dictionary);
		TbTextMapperKO.Resolve(dictionary);
		TbTextMapperPT_BR.Resolve(dictionary);
		TbTextMapperFR.Resolve(dictionary);
		TbTextMapperDE.Resolve(dictionary);
		TbTextMapperRU.Resolve(dictionary);
		TbLocalization.Resolve(dictionary);
		TbL10nImage.Resolve(dictionary);
		TbL10nLink.Resolve(dictionary);
		TbStaticText.Resolve(dictionary);
		TbL10nText.Resolve(dictionary);
		TbTextTip.Resolve(dictionary);
		TbGlobalParameter.Resolve(dictionary);
		TbTimer.Resolve(dictionary);
		TbUserSetting.Resolve(dictionary);
		TbUserSettingGroup.Resolve(dictionary);
		TbDialogueOption.Resolve(dictionary);
		TbDialogueTextStyle.Resolve(dictionary);
		TbDialogueEntity.Resolve(dictionary);
		TbRebindAction.Resolve(dictionary);
		TbGameKeyIcon.Resolve(dictionary);
		TbGameCombinedKeyIcon.Resolve(dictionary);
		TbGameKeyAction.Resolve(dictionary);
		TbMainMenu.Resolve(dictionary);
		TbEquipmentMenu.Resolve(dictionary);
		TbCompendiumMenu.Resolve(dictionary);
		TbEmailMenu.Resolve(dictionary);
		TbModMenu.Resolve(dictionary);
		TbUIEntity.Resolve(dictionary);
		TbUIEntityGroup.Resolve(dictionary);
		TbUIPooledObject.Resolve(dictionary);
		TbChipDocument.Resolve(dictionary);
		TbPlantDocument.Resolve(dictionary);
		TbCharacterDocument.Resolve(dictionary);
		TbSpecialChipEvent.Resolve(dictionary);
		TbFactionType.Resolve(dictionary);
		TbFactionMissionType.Resolve(dictionary);
		TbFactionMission.Resolve(dictionary);
		TbTreatyPortFaction.Resolve(dictionary);
		TbBoardMission.Resolve(dictionary);
		TbRewardPool.Resolve(dictionary);
		TbBoardMissionLevel.Resolve(dictionary);
		TbBoardMissionType.Resolve(dictionary);
		TbMission.Resolve(dictionary);
		TbMissionType.Resolve(dictionary);
		TbPositionType.Resolve(dictionary);
		TbMissionDecorator.Resolve(dictionary);
		TbCustomEvent.Resolve(dictionary);
		TbItemSubmitCondition.Resolve(dictionary);
		TbItemGeneCondition.Resolve(dictionary);
		TbEmail.Resolve(dictionary);
		TbScene.Resolve(dictionary);
		TbBackgroundHighLevel.Resolve(dictionary);
		TbMarkPoint.Resolve(dictionary);
		TbPortal.Resolve(dictionary);
		TbInwalkableArea.Resolve(dictionary);
		TbMapArea.Resolve(dictionary);
		TbMapRoom.Resolve(dictionary);
		TbMapRoomType.Resolve(dictionary);
		TbMapType.Resolve(dictionary);
		TbStation.Resolve(dictionary);
		TbLockableObject.Resolve(dictionary);
		TbRoom.Resolve(dictionary);
		TbDialogueObject.Resolve(dictionary);
		TbTrashTalk.Resolve(dictionary);
		TbItem.Resolve(dictionary);
		TbItemOrder.Resolve(dictionary);
		TbItemMainType.Resolve(dictionary);
		TbItemSubType.Resolve(dictionary);
		TbItemAutomationType.Resolve(dictionary);
		TbItemSource.Resolve(dictionary);
		TbEatingEffect.Resolve(dictionary);
		TbMissionItem.Resolve(dictionary);
		TbToolOverride.Resolve(dictionary);
		TbCountItemList.Resolve(dictionary);
		TbRangedItemList.Resolve(dictionary);
		TbRecipe.Resolve(dictionary);
		TbRecipeGroup.Resolve(dictionary);
		TbRecipeMainType.Resolve(dictionary);
		TbRecipeSubType.Resolve(dictionary);
		TbIngredientGroup.Resolve(dictionary);
		TbDish.Resolve(dictionary);
		TbDishGroup.Resolve(dictionary);
		TbDismantleRecipe.Resolve(dictionary);
		TbDismantleRecipeGroup.Resolve(dictionary);
		TbBuilding.Resolve(dictionary);
		TbBuildingExterior.Resolve(dictionary);
		TbBuildingWallpaper.Resolve(dictionary);
		TbRoomEffect.Resolve(dictionary);
		TbPlatform.Resolve(dictionary);
		TbBuildingSupport.Resolve(dictionary);
		TbEquipment.Resolve(dictionary);
		TbChair.Resolve(dictionary);
		TbLamp.Resolve(dictionary);
		TbItemPlaceCondition.Resolve(dictionary);
		TbLikingLevelMap.Resolve(dictionary);
		TbNpcLiking.Resolve(dictionary);
		TbNpc.Resolve(dictionary);
		TbNpcDocument.Resolve(dictionary);
		TbIdleTalkNode.Resolve(dictionary);
		TbBuff.Resolve(dictionary);
		TbSpiritThreshold.Resolve(dictionary);
		TbRecoveryDecay.Resolve(dictionary);
		TbFootStep.Resolve(dictionary);
		TbMaterialSound.Resolve(dictionary);
		TbBgm.Resolve(dictionary);
		TbAmbience.Resolve(dictionary);
		TbExclusiveSFX.Resolve(dictionary);
		TbStore.Resolve(dictionary);
		TbStorePriceScale.Resolve(dictionary);
		TbStoreItemList.Resolve(dictionary);
		TbExchangeStore.Resolve(dictionary);
		TbStoreItemUnlock.Resolve(dictionary);
		TbSeed.Resolve(dictionary);
		TbSeedType.Resolve(dictionary);
		TbSeedUnlock.Resolve(dictionary);
		TbTreeSeed.Resolve(dictionary);
		TbCropGene.Resolve(dictionary);
		TbCropGeneMatrix.Resolve(dictionary);
		TbWeather.Resolve(dictionary);
		TbDayPeriod.Resolve(dictionary);
		TbSeason.Resolve(dictionary);
		TbSeasonWeather.Resolve(dictionary);
		TbDungeonSeason.Resolve(dictionary);
		TbTechTree.Resolve(dictionary);
		TbTechNode.Resolve(dictionary);
		TbTechPoint.Resolve(dictionary);
		TbMonster.Resolve(dictionary);
		TbMonsterDocument.Resolve(dictionary);
		TbEnvObject.Resolve(dictionary);
		TbVegetation.Resolve(dictionary);
		TbResource.Resolve(dictionary);
		TbResourceType.Resolve(dictionary);
		TbResinCollectorOutput.Resolve(dictionary);
		TbResourceDocument.Resolve(dictionary);
		TbMonsterSpawn.Resolve(dictionary);
		TbItemSpawn.Resolve(dictionary);
		TbResourceSpawn.Resolve(dictionary);
		TbVegetationSpawn.Resolve(dictionary);
		TbEnvObjectSpawn.Resolve(dictionary);
		TbGlobalGuaranteed.Resolve(dictionary);
		TbFarmLevel.Resolve(dictionary);
		TbBackpackLevel.Resolve(dictionary);
		TbHat.Resolve(dictionary);
		TbAgentEquipmentSkill.Resolve(dictionary);
		TbPlayerAnimationFrame.Resolve(dictionary);
		TbFish.Resolve(dictionary);
		TbFishingPool.Resolve(dictionary);
		TbFarmFish.Resolve(dictionary);
		TbFarmFishFormation.Resolve(dictionary);
		TbFishDocument.Resolve(dictionary);
		TbFishFeed.Resolve(dictionary);
		TbDroneStructure.Resolve(dictionary);
		TbDroneWeapon.Resolve(dictionary);
		TbDroneChip.Resolve(dictionary);
		TbDroneEngine.Resolve(dictionary);
		TbDroneAssist.Resolve(dictionary);
		TbDroneSkill.Resolve(dictionary);
		TbDroneSlot.Resolve(dictionary);
		TbEnvOptimizerPoint.Resolve(dictionary);
		TbEnvOptimizerBranch.Resolve(dictionary);
		TbEnvOptimizerSlot.Resolve(dictionary);
		TbAnimal.Resolve(dictionary);
		TbFeed.Resolve(dictionary);
		TbHusbandryEnergy.Resolve(dictionary);
		TbHusbandry.Resolve(dictionary);
		TbAnimalState.Resolve(dictionary);
		TbAnimalDocument.Resolve(dictionary);
		TbCalendar.Resolve(dictionary);
		TbDateEvent.Resolve(dictionary);
		TbFestival.Resolve(dictionary);
		TbAutomateBotAppearance.Resolve(dictionary);
		TbAutomateBotPerformance.Resolve(dictionary);
		TbAutomateBot.Resolve(dictionary);
		TbModImageSetting.Resolve(dictionary);
		TbModRecipeGroupExtension.Resolve(dictionary);
		TbModStoreExtension.Resolve(dictionary);
		TbModExchangeStoreExtension.Resolve(dictionary);
		TbModResourceSpawnExtension.Resolve(dictionary);
		TbModVegetationSpawnExtension.Resolve(dictionary);
		TbModItemSpawnExtension.Resolve(dictionary);
		TbModIngredientGroupExtension.Resolve(dictionary);
		TbModDishGroupExtension.Resolve(dictionary);
		TbModFishingPoolExtension.Resolve(dictionary);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TbTextMapperZH_CN.TranslateText(translator);
		TbTextMapperEN.TranslateText(translator);
		TbTextMapperZH_TW.TranslateText(translator);
		TbTextMapperJA.TranslateText(translator);
		TbTextMapperKO.TranslateText(translator);
		TbTextMapperPT_BR.TranslateText(translator);
		TbTextMapperFR.TranslateText(translator);
		TbTextMapperDE.TranslateText(translator);
		TbTextMapperRU.TranslateText(translator);
		TbLocalization.TranslateText(translator);
		TbL10nImage.TranslateText(translator);
		TbL10nLink.TranslateText(translator);
		TbStaticText.TranslateText(translator);
		TbL10nText.TranslateText(translator);
		TbTextTip.TranslateText(translator);
		TbGlobalParameter.TranslateText(translator);
		TbTimer.TranslateText(translator);
		TbUserSetting.TranslateText(translator);
		TbUserSettingGroup.TranslateText(translator);
		TbDialogueOption.TranslateText(translator);
		TbDialogueTextStyle.TranslateText(translator);
		TbDialogueEntity.TranslateText(translator);
		TbRebindAction.TranslateText(translator);
		TbGameKeyIcon.TranslateText(translator);
		TbGameCombinedKeyIcon.TranslateText(translator);
		TbGameKeyAction.TranslateText(translator);
		TbMainMenu.TranslateText(translator);
		TbEquipmentMenu.TranslateText(translator);
		TbCompendiumMenu.TranslateText(translator);
		TbEmailMenu.TranslateText(translator);
		TbModMenu.TranslateText(translator);
		TbUIEntity.TranslateText(translator);
		TbUIEntityGroup.TranslateText(translator);
		TbUIPooledObject.TranslateText(translator);
		TbChipDocument.TranslateText(translator);
		TbPlantDocument.TranslateText(translator);
		TbCharacterDocument.TranslateText(translator);
		TbSpecialChipEvent.TranslateText(translator);
		TbFactionType.TranslateText(translator);
		TbFactionMissionType.TranslateText(translator);
		TbFactionMission.TranslateText(translator);
		TbTreatyPortFaction.TranslateText(translator);
		TbBoardMission.TranslateText(translator);
		TbRewardPool.TranslateText(translator);
		TbBoardMissionLevel.TranslateText(translator);
		TbBoardMissionType.TranslateText(translator);
		TbMission.TranslateText(translator);
		TbMissionType.TranslateText(translator);
		TbPositionType.TranslateText(translator);
		TbMissionDecorator.TranslateText(translator);
		TbCustomEvent.TranslateText(translator);
		TbItemSubmitCondition.TranslateText(translator);
		TbItemGeneCondition.TranslateText(translator);
		TbEmail.TranslateText(translator);
		TbScene.TranslateText(translator);
		TbBackgroundHighLevel.TranslateText(translator);
		TbMarkPoint.TranslateText(translator);
		TbPortal.TranslateText(translator);
		TbInwalkableArea.TranslateText(translator);
		TbMapArea.TranslateText(translator);
		TbMapRoom.TranslateText(translator);
		TbMapRoomType.TranslateText(translator);
		TbMapType.TranslateText(translator);
		TbStation.TranslateText(translator);
		TbLockableObject.TranslateText(translator);
		TbRoom.TranslateText(translator);
		TbDialogueObject.TranslateText(translator);
		TbTrashTalk.TranslateText(translator);
		TbItem.TranslateText(translator);
		TbItemOrder.TranslateText(translator);
		TbItemMainType.TranslateText(translator);
		TbItemSubType.TranslateText(translator);
		TbItemAutomationType.TranslateText(translator);
		TbItemSource.TranslateText(translator);
		TbEatingEffect.TranslateText(translator);
		TbMissionItem.TranslateText(translator);
		TbToolOverride.TranslateText(translator);
		TbCountItemList.TranslateText(translator);
		TbRangedItemList.TranslateText(translator);
		TbRecipe.TranslateText(translator);
		TbRecipeGroup.TranslateText(translator);
		TbRecipeMainType.TranslateText(translator);
		TbRecipeSubType.TranslateText(translator);
		TbIngredientGroup.TranslateText(translator);
		TbDish.TranslateText(translator);
		TbDishGroup.TranslateText(translator);
		TbDismantleRecipe.TranslateText(translator);
		TbDismantleRecipeGroup.TranslateText(translator);
		TbBuilding.TranslateText(translator);
		TbBuildingExterior.TranslateText(translator);
		TbBuildingWallpaper.TranslateText(translator);
		TbRoomEffect.TranslateText(translator);
		TbPlatform.TranslateText(translator);
		TbBuildingSupport.TranslateText(translator);
		TbEquipment.TranslateText(translator);
		TbChair.TranslateText(translator);
		TbLamp.TranslateText(translator);
		TbItemPlaceCondition.TranslateText(translator);
		TbLikingLevelMap.TranslateText(translator);
		TbNpcLiking.TranslateText(translator);
		TbNpc.TranslateText(translator);
		TbNpcDocument.TranslateText(translator);
		TbIdleTalkNode.TranslateText(translator);
		TbBuff.TranslateText(translator);
		TbSpiritThreshold.TranslateText(translator);
		TbRecoveryDecay.TranslateText(translator);
		TbFootStep.TranslateText(translator);
		TbMaterialSound.TranslateText(translator);
		TbBgm.TranslateText(translator);
		TbAmbience.TranslateText(translator);
		TbExclusiveSFX.TranslateText(translator);
		TbStore.TranslateText(translator);
		TbStorePriceScale.TranslateText(translator);
		TbStoreItemList.TranslateText(translator);
		TbExchangeStore.TranslateText(translator);
		TbStoreItemUnlock.TranslateText(translator);
		TbSeed.TranslateText(translator);
		TbSeedType.TranslateText(translator);
		TbSeedUnlock.TranslateText(translator);
		TbTreeSeed.TranslateText(translator);
		TbCropGene.TranslateText(translator);
		TbCropGeneMatrix.TranslateText(translator);
		TbWeather.TranslateText(translator);
		TbDayPeriod.TranslateText(translator);
		TbSeason.TranslateText(translator);
		TbSeasonWeather.TranslateText(translator);
		TbDungeonSeason.TranslateText(translator);
		TbTechTree.TranslateText(translator);
		TbTechNode.TranslateText(translator);
		TbTechPoint.TranslateText(translator);
		TbMonster.TranslateText(translator);
		TbMonsterDocument.TranslateText(translator);
		TbEnvObject.TranslateText(translator);
		TbVegetation.TranslateText(translator);
		TbResource.TranslateText(translator);
		TbResourceType.TranslateText(translator);
		TbResinCollectorOutput.TranslateText(translator);
		TbResourceDocument.TranslateText(translator);
		TbMonsterSpawn.TranslateText(translator);
		TbItemSpawn.TranslateText(translator);
		TbResourceSpawn.TranslateText(translator);
		TbVegetationSpawn.TranslateText(translator);
		TbEnvObjectSpawn.TranslateText(translator);
		TbGlobalGuaranteed.TranslateText(translator);
		TbFarmLevel.TranslateText(translator);
		TbBackpackLevel.TranslateText(translator);
		TbHat.TranslateText(translator);
		TbAgentEquipmentSkill.TranslateText(translator);
		TbPlayerAnimationFrame.TranslateText(translator);
		TbFish.TranslateText(translator);
		TbFishingPool.TranslateText(translator);
		TbFarmFish.TranslateText(translator);
		TbFarmFishFormation.TranslateText(translator);
		TbFishDocument.TranslateText(translator);
		TbFishFeed.TranslateText(translator);
		TbDroneStructure.TranslateText(translator);
		TbDroneWeapon.TranslateText(translator);
		TbDroneChip.TranslateText(translator);
		TbDroneEngine.TranslateText(translator);
		TbDroneAssist.TranslateText(translator);
		TbDroneSkill.TranslateText(translator);
		TbDroneSlot.TranslateText(translator);
		TbEnvOptimizerPoint.TranslateText(translator);
		TbEnvOptimizerBranch.TranslateText(translator);
		TbEnvOptimizerSlot.TranslateText(translator);
		TbAnimal.TranslateText(translator);
		TbFeed.TranslateText(translator);
		TbHusbandryEnergy.TranslateText(translator);
		TbHusbandry.TranslateText(translator);
		TbAnimalState.TranslateText(translator);
		TbAnimalDocument.TranslateText(translator);
		TbCalendar.TranslateText(translator);
		TbDateEvent.TranslateText(translator);
		TbFestival.TranslateText(translator);
		TbAutomateBotAppearance.TranslateText(translator);
		TbAutomateBotPerformance.TranslateText(translator);
		TbAutomateBot.TranslateText(translator);
		TbModImageSetting.TranslateText(translator);
		TbModRecipeGroupExtension.TranslateText(translator);
		TbModStoreExtension.TranslateText(translator);
		TbModExchangeStoreExtension.TranslateText(translator);
		TbModResourceSpawnExtension.TranslateText(translator);
		TbModVegetationSpawnExtension.TranslateText(translator);
		TbModItemSpawnExtension.TranslateText(translator);
		TbModIngredientGroupExtension.TranslateText(translator);
		TbModDishGroupExtension.TranslateText(translator);
		TbModFishingPoolExtension.TranslateText(translator);
	}

	private void PostInit()
	{
		_textProviders.Add("zh-CN", TbTextMapperZH_CN);
		_textProviders.Add("zh-TW", TbTextMapperZH_TW);
		_textProviders.Add("en", TbTextMapperEN);
		_textProviders.Add("ja", TbTextMapperJA);
		_textProviders.Add("ko", TbTextMapperKO);
		_textProviders.Add("pt-BR", TbTextMapperPT_BR);
		_currentL10nId = "zh-CN";
		_currentTextProvider = TbTextMapperZH_CN;
	}

	public void MergeModExtension()
	{
		TryHandle(HandleModRecipeGroupExtension);
		TryHandle(HandleModStoreExtension);
		TryHandle(HandleModExchangeStoreExtension);
		TryHandle(HandleModIngredientGroupExtension);
		TryHandle(HandleModResourceSpawnExtension);
		TryHandle(HandleModVegetationSpawnExtension);
		TryHandle(HandleModItemSpawnExtension);
		TryHandle(HandleModDishGroupExtension);
		TryHandle(HandleModFishingPoolExtension);
	}

	private void TryHandle(Action action)
	{
		if (action == null)
		{
			return;
		}
		try
		{
			action();
		}
		catch (Exception)
		{
			Debug.LogError("[MOD] An error occurred in " + action.Method.Name);
		}
	}

	private void HandleModRecipeGroupExtension()
	{
		if (TbModRecipeGroupExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModRecipeGroupExtensionInfo data in TbModRecipeGroupExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraRecipes_Ref.IsNullOrEmpty())
			{
				continue;
			}
			RecipeInfo[] extraRecipes_Ref = data.ExtraRecipes_Ref;
			foreach (RecipeInfo extraRecipe in extraRecipes_Ref)
			{
				if (extraRecipe != null)
				{
					data.Id_Ref.RecipeIds.RemoveAll((string x) => x == extraRecipe.Id);
					data.Id_Ref.RecipeIds_Ref.RemoveAll((RecipeInfo x) => x.Id == extraRecipe.Id);
					data.Id_Ref.RecipeIds.Add(extraRecipe.Id);
					data.Id_Ref.RecipeIds_Ref.Add(extraRecipe);
				}
			}
		}
	}

	private void HandleModStoreExtension()
	{
		if (TbModStoreExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModStoreExtensionInfo data in TbModStoreExtension.DataList)
		{
			StoreInfo storeInfo = data.Id_Ref;
			string id = data.Id;
			string text = ((id == "kenenimuu_shop") ? "animal_shop" : ((!(id == "seasonseed_villain_shop")) ? data.Id : "seasonseed_shop"));
			string key = text;
			if (storeInfo == null)
			{
				storeInfo = DolocConfig.Tables.TbStore.GetOrDefault(key);
			}
			if (storeInfo == null || data.ExtraItems.IsNullOrEmpty())
			{
				continue;
			}
			StoreItemSeasonData[] extraItems = data.ExtraItems;
			foreach (StoreItemSeasonData extraItem in extraItems)
			{
				if (extraItem.ItemName_Ref != null)
				{
					storeInfo.ItemRecords_Ref.ItemList.RemoveAll((StoreItemSeasonData x) => x.ItemName == extraItem.ItemName);
					storeInfo.ItemRecords_Ref.ItemList.Add(extraItem);
					storeInfo.ItemRecords_Ref.ItemList_Index[extraItem.ItemName] = extraItem;
				}
			}
		}
	}

	private void HandleModExchangeStoreExtension()
	{
		if (TbModExchangeStoreExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModExchangeStoreExtensionInfo data in TbModExchangeStoreExtension.DataList)
		{
			ExchangeStoreInfo exchangeStoreInfo = data.Id_Ref;
			string key = data.Id switch
			{
				"merlin_exchange_shop" => "tool_upgrade", 
				"cod_exchange_shop" => "cod_shop", 
				"lank_exchange_shop" => "garbage_exchange_store", 
				"orlando_exchange_shop" => "orlando_exchange_shop", 
				"sacco_exchange_shop" => "sacco_store", 
				"evernight_kuma_exchange_shop" => "evernight_kuma_shop", 
				_ => data.Id, 
			};
			if (exchangeStoreInfo == null)
			{
				exchangeStoreInfo = DolocConfig.Tables.TbExchangeStore.GetOrDefault(key);
			}
			if (exchangeStoreInfo == null || data.ExtraItems.IsNullOrEmpty())
			{
				continue;
			}
			ExchangeStoreItemData[] extraItems = data.ExtraItems;
			foreach (ExchangeStoreItemData extraItem in extraItems)
			{
				if (extraItem.ItemId_Ref != null)
				{
					exchangeStoreInfo.ItemList.RemoveAll((ExchangeStoreItemData x) => x.ItemId == extraItem.ItemId);
					exchangeStoreInfo.ItemList.Add(extraItem);
					exchangeStoreInfo.StoreItemMap[extraItem.ItemId] = extraItem;
				}
			}
		}
	}

	private void HandleModIngredientGroupExtension()
	{
		if (TbModIngredientGroupExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModIngredientGroupExtensionInfo data in TbModIngredientGroupExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraItems_Ref.IsNullOrEmpty())
			{
				continue;
			}
			ItemInfo[] extraItems_Ref = data.ExtraItems_Ref;
			foreach (ItemInfo extraItem in extraItems_Ref)
			{
				if (extraItem?.Id_Ref != null)
				{
					data.Id_Ref.Items.RemoveAll((string x) => x == extraItem.Id);
					data.Id_Ref.Items_Ref.RemoveAll((ItemInfo x) => x.Id == extraItem.Id);
					data.Id_Ref.Items.Add(extraItem.Id);
					data.Id_Ref.Items_Ref.Add(extraItem);
				}
			}
		}
	}

	private void HandleModResourceSpawnExtension()
	{
		if (TbModResourceSpawnExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModResourceSpawnExtensionInfo data in TbModResourceSpawnExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraResources.IsNullOrEmpty())
			{
				continue;
			}
			ResourceSpawnData[] extraResources = data.ExtraResources;
			foreach (ResourceSpawnData extraResource in extraResources)
			{
				if (extraResource?.ResourceId_Ref != null)
				{
					data.Id_Ref.SpawnDatas.RemoveAll((ResourceSpawnData x) => x.SpawnId == extraResource.SpawnId);
					data.Id_Ref.SpawnDataList.RemoveAll((SpawnData x) => x.SpawnId == extraResource.SpawnId);
					data.Id_Ref.SpawnDatas.Add(extraResource);
					data.Id_Ref.SpawnDatas_Index[extraResource.SpawnId] = extraResource;
					data.Id_Ref.SpawnDataList.Add(extraResource);
				}
			}
		}
	}

	private void HandleModVegetationSpawnExtension()
	{
		if (TbModVegetationSpawnExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModVegetationSpawnExtensionInfo data in TbModVegetationSpawnExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraVegetations.IsNullOrEmpty())
			{
				continue;
			}
			VegetationSpawnData[] extraVegetations = data.ExtraVegetations;
			foreach (VegetationSpawnData extraVegetation in extraVegetations)
			{
				if (extraVegetation?.VegetationId_Ref != null)
				{
					data.Id_Ref.SpawnDatas.RemoveAll((VegetationSpawnData x) => x.SpawnId == extraVegetation.SpawnId);
					data.Id_Ref.SpawnDataList.RemoveAll((SpawnData x) => x.SpawnId == extraVegetation.SpawnId);
					data.Id_Ref.SpawnDatas.Add(extraVegetation);
					data.Id_Ref.SpawnDatas_Index[extraVegetation.SpawnId] = extraVegetation;
					data.Id_Ref.SpawnDataList.Add(extraVegetation);
				}
			}
		}
	}

	private void HandleModItemSpawnExtension()
	{
		if (TbModItemSpawnExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModItemSpawnExtensionInfo data in TbModItemSpawnExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraItems.IsNullOrEmpty())
			{
				continue;
			}
			ItemSpawnData[] extraItems = data.ExtraItems;
			foreach (ItemSpawnData extraItem in extraItems)
			{
				if (extraItem?.ItemName_Ref != null)
				{
					data.Id_Ref.SpawnDatas.RemoveAll((ItemSpawnData x) => x.SpawnId == extraItem.SpawnId);
					data.Id_Ref.SpawnDataList.RemoveAll((SpawnData x) => x.SpawnId == extraItem.SpawnId);
					data.Id_Ref.SpawnDatas.Add(extraItem);
					data.Id_Ref.SpawnDataList.Add(extraItem);
					data.Id_Ref.SpawnDatas_Index[extraItem.SpawnId] = extraItem;
				}
			}
		}
	}

	private void HandleModDishGroupExtension()
	{
		if (TbModDishGroupExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModDishGroupExtensionInfo data in TbModDishGroupExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraDishes_Ref.IsNullOrEmpty())
			{
				continue;
			}
			DishInfo[] extraDishes_Ref = data.ExtraDishes_Ref;
			foreach (DishInfo extraDish in extraDishes_Ref)
			{
				if (extraDish != null)
				{
					data.Id_Ref.DishIds.RemoveAll((string x) => x == extraDish.Id);
					data.Id_Ref.DishIds_Ref.RemoveAll((DishInfo x) => x.Id == extraDish.Id);
					data.Id_Ref.DishIds.Add(extraDish.Id);
					data.Id_Ref.DishIds_Ref.Add(extraDish);
				}
			}
		}
	}

	private void HandleModFishingPoolExtension()
	{
		if (TbModFishingPoolExtension.DataList.IsNullOrEmpty())
		{
			return;
		}
		foreach (ModFishingPoolExtensionInfo data in TbModFishingPoolExtension.DataList)
		{
			if (data?.Id_Ref == null || data.ExtraFishes_Ref.IsNullOrEmpty())
			{
				continue;
			}
			FishInfo[] extraFishes_Ref = data.ExtraFishes_Ref;
			foreach (FishInfo extraFish in extraFishes_Ref)
			{
				if (extraFish != null)
				{
					data.Id_Ref.Fishes.RemoveAll((string x) => x == extraFish.Id);
					data.Id_Ref.Fishes_Ref.RemoveAll((FishInfo x) => x.Id == extraFish.Id);
					data.Id_Ref.Fishes.Add(extraFish.Id);
					data.Id_Ref.Fishes_Ref.Add(extraFish);
				}
			}
		}
	}

	public void SwitchLanguage(string l10nId)
	{
		if (l10nId == _currentL10nId)
		{
			return;
		}
		if (!_textProviders.ContainsKey(l10nId))
		{
			Debug.LogError("未知的langId: " + l10nId);
			return;
		}
		_currentL10nId = l10nId;
		foreach (KeyValuePair<string, ITextProvider> textProvider in _textProviders)
		{
			if (!(textProvider.Key == l10nId))
			{
				textProvider.Value.Unload();
			}
		}
		_currentTextProvider = _textProviders[l10nId];
		_currentTextProvider.Load(_dataLoader(_dataFileMap[_currentTextProvider.GetType()]));
		TranslateText(TextMapper);
		LanguageChange?.Invoke();
	}

	private string TextMapper(string key, string originText)
	{
		if (key.IsNullOrEmpty())
		{
			return originText;
		}
		string text = _currentTextProvider.GetText(key);
		if (text.IsNullOrEmpty())
		{
			if (DolocAPI.gameManager != null && !DolocAPI.gameManager.gameInitConfig.ignoreTextMapperLog)
			{
				Debug.LogWarning("多语言文本key<" + key + ">没有<" + _currentL10nId + ">文本配置! origin: " + originText);
			}
			return originText;
		}
		return text;
	}

	public ITextProvider GetTextProvider(string l10nId)
	{
		_textProviders.TryGetValue(l10nId, out var value);
		return value;
	}
}
