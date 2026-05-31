using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Buff;
using DolocTown.Config.Building;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.Room;
using DolocTown.Config.Store;
using DolocTown.Config.UI;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class GlobalParameterInfo : BeanBase
{
	public const int __ID__ = -861558071;

	public int InventoryLineCapacity { get; private set; }

	public string AchievementGraphName { get; private set; }

	public string[] ItemCollectionLabels { get; private set; }

	public ItemMainTypeInfo[] ItemCollectionLabels_Ref { get; private set; }

	public string[] FoodItemSubTypes { get; private set; }

	public ItemSubTypeInfo[] FoodItemSubTypes_Ref { get; private set; }

	public string ItemDefaultType { get; private set; }

	public ItemMainTypeInfo ItemDefaultType_Ref { get; private set; }

	public int FishTankMaxCountOfSlots { get; private set; }

	public int LikingCeiling { get; private set; }

	public float LikingRatioOnBirthday { get; private set; }

	public int LikingBirthdayParty { get; private set; }

	public int UpperFavorabilityLevel { get; private set; }

	public int WeeklyGiftLimit { get; private set; }

	public int LikingFirstGreetingDay { get; private set; }

	public int LikingActivity { get; private set; }

	public Vector2Int NpcRelaxLightmanFishingInterval { get; private set; }

	public float CombinationKeyHoldDuration { get; private set; }

	public string QuickInventoryTimer { get; private set; }

	public TimerInfo QuickInventoryTimer_Ref { get; private set; }

	public string QuantitySelectTimer { get; private set; }

	public TimerInfo QuantitySelectTimer_Ref { get; private set; }

	public string ContinuouslyUseItemTimer { get; private set; }

	public TimerInfo ContinuouslyUseItemTimer_Ref { get; private set; }

	public string ContinuouslyInteractTimer { get; private set; }

	public TimerInfo ContinuouslyInteractTimer_Ref { get; private set; }

	public string TicketItemName { get; private set; }

	public ItemInfo TicketItemName_Ref { get; private set; }

	public int StationTicketMarkup { get; private set; }

	public float GravelBlockLastDuration { get; private set; }

	public float GravelBlockRefreshDuration { get; private set; }

	public int MissionUpperLimit { get; private set; }

	public int TakeTimeLimitFixed { get; private set; }

	public int TakeTimeLimitRandom { get; private set; }

	public int MissionRefreshDay { get; private set; }

	public Vector3Int MissionRefreshWeights { get; private set; }

	public string DropOffBoxSotreId { get; private set; }

	public string[] SeedStoreIds { get; private set; }

	public StoreInfo[] SeedStoreIds_Ref { get; private set; }

	public string[] ItemsCanNotBuyback { get; private set; }

	public ItemInfo[] ItemsCanNotBuyback_Ref { get; private set; }

	public Vector2Int InventoryAroundArea { get; private set; }

	public Vector2Int InventoryAroundOffset { get; private set; }

	public int EquipmentBoundaryPadding { get; private set; }

	public Vector2Int BuilderAroundArea { get; private set; }

	public PrefabAsset UiGroupAsset { get; private set; }

	public string DefaultUiEntityInfo { get; private set; }

	public UIEntityInfo DefaultUiEntityInfo_Ref { get; private set; }

	public int NapHour { get; private set; }

	public int WakeUpClock { get; private set; }

	public float RecoveredPlayerValuesPerHour { get; private set; }

	public float FullSleepBuffThreshold { get; private set; }

	public string FullSleepBuff { get; private set; }

	public BuffInfo FullSleepBuff_Ref { get; private set; }

	public float SitOptionDelay { get; private set; }

	public int FaintMinutesAfterDrowning { get; private set; }

	public int TreatyPortMaintainTime { get; private set; }

	public string[] TreatyPortGatesName { get; private set; }

	public MarkPointInfo[] TreatyPortGatesName_Ref { get; private set; }

	public Vector2Int FactionWorkingHours { get; private set; }

	public int Money { get; private set; }

	public int MaxMoney { get; private set; }

	public WeatherType InitWeatherType { get; private set; }

	public int InitTime { get; private set; }

	public string TrainingDungeonName { get; private set; }

	public DolocTown.Config.Room.SceneInfo TrainingDungeonName_Ref { get; private set; }

	public string PlayerDefaultName { get; private set; }

	public string PlayerDefaultName_l10n_key { get; }

	public int InputPlayerNameMaxLength { get; private set; }

	public Vector2Int PlayerDefaultBirthday { get; private set; }

	public string InitHat { get; private set; }

	public ItemInfo InitHat_Ref { get; private set; }

	public int MaxSupportHeight { get; private set; }

	public float MinFloorCoveredRatio { get; private set; }

	public float BuilderRaycastDistance { get; private set; }

	public int BuilderPlatformHeightLimit { get; private set; }

	public Vector2Int BuilderPlatformWidth { get; private set; }

	public float EquipmentShakeIntensity { get; private set; }

	public float PlatformShakeIntensity { get; private set; }

	public int EquipmentSmallThreshold { get; private set; }

	public float DemolitionRecycleFactor { get; private set; }

	public string BuilderAnimationNode { get; private set; }

	public string BuilderMoveTimer { get; private set; }

	public TimerInfo BuilderMoveTimer_Ref { get; private set; }

	public int TemporaryBuildingCount { get; private set; }

	public int TULength { get; private set; }

	public int TU2Min { get; private set; }

	public int Hour2Min { get; private set; }

	public int Day2Hour { get; private set; }

	public int Month2Day { get; private set; }

	public int Year2Month { get; private set; }

	public int EventRefreshClock { get; private set; }

	public int TotalDayOffset { get; private set; }

	public int WeatherUpdateInterval { get; private set; }

	public int SleepTime { get; private set; }

	public SwitchScheduleAsset DefaultSwitchScheduleAsseet { get; private set; }

	public SwitchScheduleAsset DroneSwitchScheduleAsset { get; private set; }

	public float SpeedUpTimeScale { get; private set; }

	public float HoldOnIntervalInDialogueNormal { get; private set; }

	public float HoldOnIntervalInDialogueSpeedUp { get; private set; }

	public string HomepageBgmEvent { get; private set; }

	public Vector2 BgmRandomIdleTime { get; private set; }

	public int BgmPlayTimeoutDuration { get; private set; }

	public float SfxGroupInterval { get; private set; }

	public int ReplayableDialogueNodeCount { get; private set; }

	public int InitHealth { get; private set; }

	public int InitEnergy { get; private set; }

	public int InitSpirit { get; private set; }

	public int MaxOverflowHealth { get; private set; }

	public int MaxOverflowEnergy { get; private set; }

	public float HealthPercentAfterFaint { get; private set; }

	public float EnergyPercentAfterFaint { get; private set; }

	public float SpiritPercentAfterFaint { get; private set; }

	public int CorrosionCounterLength { get; private set; }

	public int CorrosionHealthCost { get; private set; }

	public float HitBackDistance { get; private set; }

	public int AgentDefend { get; private set; }

	public int ToolEnergyCost { get; private set; }

	public float DashCdDuration { get; private set; }

	public float DefendAdjust { get; private set; }

	public float CriticalDamageRate { get; private set; }

	public float SunGrowthAddition { get; private set; }

	public string[] ClonedCropGeneGroup { get; private set; }

	public CropGeneInfo[] ClonedCropGeneGroup_Ref { get; private set; }

	public string[] CompressGeneFixed { get; private set; }

	public CropGeneInfo[] CompressGeneFixed_Ref { get; private set; }

	public Vector2[] CompressGeneCountDistPre { get; private set; }

	public Vector2[] CompressGeneCountDistPost { get; private set; }

	public Vector2[] SynthesisGeneCountDist { get; private set; }

	public SpriteAssetArray BuildingDamageSpriteAssets { get; private set; }

	public int BuildingLinkThresholdLeft { get; private set; }

	public int BuildingLinkThresholdRight { get; private set; }

	public int BuildingLinkThresholdTop { get; private set; }

	public int BuildingLinkThresholdBottom { get; private set; }

	public int MaxGeneCount { get; private set; }

	public string CellarBuildingId { get; private set; }

	public BuildingInfo CellarBuildingId_Ref { get; private set; }

	public float CellarDistanceToFarmRight { get; private set; }

	public float CellarLinkGateClipThreshold { get; private set; }

	public float HighLevelResourceMinProbabilityInDungeon { get; private set; }

	public int DungeonMaxHistoryThreshold { get; private set; }

	public float CastDuration { get; private set; }

	public float PerfectCastDuration { get; private set; }

	public float CastCancelThreshold { get; private set; }

	public float FishingRollInterval { get; private set; }

	public float FishingBiteInitProbability { get; private set; }

	public float FishingBiteAdditionalProbability { get; private set; }

	public float PullTiming { get; private set; }

	public int FishingEnergyCost { get; private set; }

	public float FishingOperationInterval { get; private set; }

	public int FishingNoteScrollSpeedBase { get; private set; }

	public int FishingNoteScrollSpeedBonus { get; private set; }

	public int FishingNoteBarWidthPixel { get; private set; }

	public int FishingProgressBarWidthPixel { get; private set; }

	public float FishingAnimationMinimumInterval { get; private set; }

	public float FishingPunishAnimationDuration { get; private set; }

	public float FishingPunishStartTime { get; private set; }

	public float FishingGamePreparationTimeScale { get; private set; }

	public float FishingNoteSpeedDecreasePerLevel { get; private set; }

	public float AcidRainDamage { get; private set; }

	public float ScorchSunDamage { get; private set; }

	public Vector2Int ThunderFrequency { get; private set; }

	public int ThunderPower { get; private set; }

	public float ThunderDamgeToCrop { get; private set; }

	public float ThunderDamgeToBuilding { get; private set; }

	public float SunLightIntensityAdderFactor { get; private set; }

	public float WeatherTransitDuration { get; private set; }

	public float[] LightIntensityDecayFactorPerLampInside { get; private set; }

	public float[] LightIntensityDecayFactorPerLampOutside { get; private set; }

	public Vector2Int LightIntensityDecayLampArea { get; private set; }

	public float[] EnvLightIntensityAdderPerLamp { get; private set; }

	public Color EnvLightDefaultColor { get; private set; }

	public int WeatherRegulatorCdDuration { get; private set; }

	public float WeatherRegulatorLaunchPower { get; private set; }

	public float MotorHorizontalAcceleration { get; private set; }

	public float MotorHorizontalRevertAcceleration { get; private set; }

	public float MotorHorizontalDeceleration { get; private set; }

	public float MotorHorizontalMaxSpeed { get; private set; }

	public float MotorHorizontalReboundSpeedRate { get; private set; }

	public float MotorGravity { get; private set; }

	public float MotorDropMaxSpeed { get; private set; }

	public float MotorRaycastLength { get; private set; }

	public Vector2 MotorNaturalJumpAccRange { get; private set; }

	public float MotorNaturalJumpThreshold { get; private set; }

	public float MotorNaturalJumpMaxSpeed { get; private set; }

	public float MotorVerticalAcceleration { get; private set; }

	public float MotorVerticalMaxSpeed { get; private set; }

	public float MotorEnduranceDuration { get; private set; }

	public float MotorEnduranceRecv { get; private set; }

	public float MotorCollisionVerticalSpeed { get; private set; }

	public float MotorVerticalReboundSpeedRate { get; private set; }

	public float MotorCallSpeed { get; private set; }

	public float UiNodeMessageHoldDuration { get; private set; }

	public float FadeDefaultDurationOnMonthChange { get; private set; }

	public float FadeDefaultDurationOnSleep { get; private set; }

	public float RebindActionInterruptDuration { get; private set; }

	public float HideCursorDuration { get; private set; }

	public string[] UiActionExclusiveBinds { get; private set; }

	public RebindActionInfo[] UiActionExclusiveBinds_Ref { get; private set; }

	public string[] UiActionHorizontalMove { get; private set; }

	public RebindActionInfo[] UiActionHorizontalMove_Ref { get; private set; }

	public string[] UiActionVerticalMove { get; private set; }

	public RebindActionInfo[] UiActionVerticalMove_Ref { get; private set; }

	public string RebindActionSettingGroup { get; private set; }

	public UserSettingGroupInfo RebindActionSettingGroup_Ref { get; private set; }

	public float UiButtonLongClickDuration { get; private set; }

	public float UiSceneTextTipDuration { get; private set; }

	public float UiSceneInfoTipDuration { get; private set; }

	public int InputMemoMaxLength { get; private set; }

	public SpriteAssetArray ItemSubscriptHasGene { get; private set; }

	public SpriteAssetArray ItemSubscriptClone { get; private set; }

	public SpriteAsset UiPlayerDefaultSprite { get; private set; }

	public SpriteAsset UiPlayerHairDefaultSprite { get; private set; }

	public SpriteAsset UiPlayerBodyDefaultSprite { get; private set; }

	public SpriteAsset UiPlayerDefaultPortrait { get; private set; }

	public SpriteAsset DefaultVehiclePreview { get; private set; }

	public string ChipItemName { get; private set; }

	public int ChipSubmitLimit { get; private set; }

	public int ChipAnalyzeHour { get; private set; }

	public float ChipAnalyzeSpeedBuff { get; private set; }

	public string ChipProgressCheckDialogueNode { get; private set; }

	public string[] WaterItems { get; private set; }

	public ItemInfo[] WaterItems_Ref { get; private set; }

	public FoodEffect BetterWaterExtraBuff { get; private set; }

	public float AnimalThunderProbability { get; private set; }

	public int AnimalThunderMoodDecrease { get; private set; }

	public int AnimalUnhappyThreshold { get; private set; }

	public int AnimalWeaknessThreshold { get; private set; }

	public int AnimalTechpointFondle { get; private set; }

	public int AnimalMoodUpdateInterval { get; private set; }

	public int AnimalMoodContributionWeather { get; private set; }

	public int AnimalMoodContributionHungry { get; private set; }

	public int AnimalMoodContributionFull { get; private set; }

	public int AnimalMoodContributionFullToilet { get; private set; }

	public Vector2Int AnimalMoodContributionRangeEquipment { get; private set; }

	public float AnimalEnergyCostPercentInNight { get; private set; }

	public int AnimalMoodShowFlagThreshold { get; private set; }

	public int AnimalChickennestEnergyRequire { get; private set; }

	public int AquaDefaultWeight { get; private set; }

	public int AquaTechpointProduce { get; private set; }

	public string ItemRefTimeBomb { get; private set; }

	public ItemInfo ItemRefTimeBomb_Ref { get; private set; }

	public string ItemRefBottleOfWater { get; private set; }

	public ItemInfo ItemRefBottleOfWater_Ref { get; private set; }

	public string ItemRefWastePlasticBottle { get; private set; }

	public ItemInfo ItemRefWastePlasticBottle_Ref { get; private set; }

	public string ItemRefFaeces { get; private set; }

	public ItemInfo ItemRefFaeces_Ref { get; private set; }

	public string ItemRefSturdySack { get; private set; }

	public ItemInfo ItemRefSturdySack_Ref { get; private set; }

	public string ItemRefWeeds { get; private set; }

	public ItemInfo ItemRefWeeds_Ref { get; private set; }

	public string ItemRefTicket { get; private set; }

	public ItemInfo ItemRefTicket_Ref { get; private set; }

	public string ItemRefBox { get; private set; }

	public ItemInfo ItemRefBox_Ref { get; private set; }

	public string ItemRefSeedEndyam { get; private set; }

	public ItemInfo ItemRefSeedEndyam_Ref { get; private set; }

	public string ItemRefRoastedEndyam { get; private set; }

	public ItemInfo ItemRefRoastedEndyam_Ref { get; private set; }

	public string ItemRefFishFry { get; private set; }

	public ItemInfo ItemRefFishFry_Ref { get; private set; }

	public string ItemRefGeneCapsuleEmpty { get; private set; }

	public ItemInfo ItemRefGeneCapsuleEmpty_Ref { get; private set; }

	public string ItemRefOldBattery { get; private set; }

	public ItemInfo ItemRefOldBattery_Ref { get; private set; }

	public string ItemRefMilk { get; private set; }

	public ItemInfo ItemRefMilk_Ref { get; private set; }

	public SwitchScheduleGraph DefaultSwitchSchedule => DefaultSwitchScheduleAsseet.Asset;

	public SwitchScheduleGraph DroneSwitchSchedule => DroneSwitchScheduleAsset.Asset;

	public int WeatherRegulatorCdDurationTu
	{
		get
		{
			int seconds = GameDays2Secs(WeatherRegulatorCdDuration);
			return Secs2Tu(seconds);
		}
	}

	public Counter NewTuCounter => new Counter(TULength);

	public DateConfig DateConfig => new DateConfig(TULength, TU2Min, Hour2Min, Day2Hour, Month2Day, Year2Month);

	public GlobalParameterInfo(JSONNode _json)
	{
		if (!_json["inventory_line_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		InventoryLineCapacity = _json["inventory_line_capacity"];
		if (!_json["achievement_graph_name"].IsString)
		{
			throw new SerializationException();
		}
		AchievementGraphName = _json["achievement_graph_name"];
		JSONNode jSONNode = _json["item_collection_labels"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ItemCollectionLabels = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ItemCollectionLabels[num++] = text;
		}
		JSONNode jSONNode2 = _json["food_item_sub_types"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		FoodItemSubTypes = new string[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string text2 = child2;
			FoodItemSubTypes[num2++] = text2;
		}
		if (!_json["item_default_type"].IsString)
		{
			throw new SerializationException();
		}
		ItemDefaultType = _json["item_default_type"];
		if (!_json["fish_tank_max_count_of_slots"].IsNumber)
		{
			throw new SerializationException();
		}
		FishTankMaxCountOfSlots = _json["fish_tank_max_count_of_slots"];
		if (!_json["liking_ceiling"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingCeiling = _json["liking_ceiling"];
		if (!_json["liking_ratio_on_birthday"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingRatioOnBirthday = _json["liking_ratio_on_birthday"];
		if (!_json["liking_birthday_party"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingBirthdayParty = _json["liking_birthday_party"];
		if (!_json["upper_favorability_level"].IsNumber)
		{
			throw new SerializationException();
		}
		UpperFavorabilityLevel = _json["upper_favorability_level"];
		if (!_json["weekly_gift_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		WeeklyGiftLimit = _json["weekly_gift_limit"];
		if (!_json["liking_first_greeting_day"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingFirstGreetingDay = _json["liking_first_greeting_day"];
		if (!_json["liking_activity"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingActivity = _json["liking_activity"];
		if (!_json["npc_relax_lightman_fishing_interval"].IsObject)
		{
			throw new SerializationException();
		}
		NpcRelaxLightmanFishingInterval = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["npc_relax_lightman_fishing_interval"]));
		if (!_json["combination_key_hold_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		CombinationKeyHoldDuration = _json["combination_key_hold_duration"];
		if (!_json["quick_inventory_timer"].IsString)
		{
			throw new SerializationException();
		}
		QuickInventoryTimer = _json["quick_inventory_timer"];
		if (!_json["quantity_select_timer"].IsString)
		{
			throw new SerializationException();
		}
		QuantitySelectTimer = _json["quantity_select_timer"];
		if (!_json["continuously_use_item_timer"].IsString)
		{
			throw new SerializationException();
		}
		ContinuouslyUseItemTimer = _json["continuously_use_item_timer"];
		if (!_json["continuously_interact_timer"].IsString)
		{
			throw new SerializationException();
		}
		ContinuouslyInteractTimer = _json["continuously_interact_timer"];
		if (!_json["ticket_item_name"].IsString)
		{
			throw new SerializationException();
		}
		TicketItemName = _json["ticket_item_name"];
		if (!_json["station_ticket_markup"].IsNumber)
		{
			throw new SerializationException();
		}
		StationTicketMarkup = _json["station_ticket_markup"];
		if (!_json["gravel_block_last_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		GravelBlockLastDuration = _json["gravel_block_last_duration"];
		if (!_json["gravel_block_refresh_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		GravelBlockRefreshDuration = _json["gravel_block_refresh_duration"];
		if (!_json["mission_upper_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		MissionUpperLimit = _json["mission_upper_limit"];
		if (!_json["take_time_limit_fixed"].IsNumber)
		{
			throw new SerializationException();
		}
		TakeTimeLimitFixed = _json["take_time_limit_fixed"];
		if (!_json["take_time_limit_random"].IsNumber)
		{
			throw new SerializationException();
		}
		TakeTimeLimitRandom = _json["take_time_limit_random"];
		if (!_json["mission_refresh_day"].IsNumber)
		{
			throw new SerializationException();
		}
		MissionRefreshDay = _json["mission_refresh_day"];
		if (!_json["mission_refresh_weights"].IsObject)
		{
			throw new SerializationException();
		}
		MissionRefreshWeights = ExternalTypeUtil.Vector3IntConverter(CfgVector3Int.DeserializeCfgVector3Int(_json["mission_refresh_weights"]));
		if (!_json["drop_off_box_sotre_id"].IsString)
		{
			throw new SerializationException();
		}
		DropOffBoxSotreId = _json["drop_off_box_sotre_id"];
		JSONNode jSONNode3 = _json["seed_store_ids"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		SeedStoreIds = new string[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsString)
			{
				throw new SerializationException();
			}
			string text3 = child3;
			SeedStoreIds[num3++] = text3;
		}
		JSONNode jSONNode4 = _json["items_can_not_buyback"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		ItemsCanNotBuyback = new string[count4];
		int num4 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsString)
			{
				throw new SerializationException();
			}
			string text4 = child4;
			ItemsCanNotBuyback[num4++] = text4;
		}
		if (!_json["inventory_around_area"].IsObject)
		{
			throw new SerializationException();
		}
		InventoryAroundArea = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["inventory_around_area"]));
		if (!_json["inventory_around_offset"].IsObject)
		{
			throw new SerializationException();
		}
		InventoryAroundOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["inventory_around_offset"]));
		if (!_json["equipment_boundary_padding"].IsNumber)
		{
			throw new SerializationException();
		}
		EquipmentBoundaryPadding = _json["equipment_boundary_padding"];
		if (!_json["builder_around_area"].IsObject)
		{
			throw new SerializationException();
		}
		BuilderAroundArea = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["builder_around_area"]));
		if (!_json["ui_group_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiGroupAsset = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(_json["ui_group_asset"]));
		if (!_json["default_ui_entity_info"].IsString)
		{
			throw new SerializationException();
		}
		DefaultUiEntityInfo = _json["default_ui_entity_info"];
		if (!_json["nap_hour"].IsNumber)
		{
			throw new SerializationException();
		}
		NapHour = _json["nap_hour"];
		if (!_json["wake_up_clock"].IsNumber)
		{
			throw new SerializationException();
		}
		WakeUpClock = _json["wake_up_clock"];
		if (!_json["recovered_player_values_per_hour"].IsNumber)
		{
			throw new SerializationException();
		}
		RecoveredPlayerValuesPerHour = _json["recovered_player_values_per_hour"];
		if (!_json["full_sleep_buff_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		FullSleepBuffThreshold = _json["full_sleep_buff_threshold"];
		if (!_json["full_sleep_buff"].IsString)
		{
			throw new SerializationException();
		}
		FullSleepBuff = _json["full_sleep_buff"];
		if (!_json["sit_option_delay"].IsNumber)
		{
			throw new SerializationException();
		}
		SitOptionDelay = _json["sit_option_delay"];
		if (!_json["faint_minutes_after_drowning"].IsNumber)
		{
			throw new SerializationException();
		}
		FaintMinutesAfterDrowning = _json["faint_minutes_after_drowning"];
		if (!_json["treaty_port_maintain_time"].IsNumber)
		{
			throw new SerializationException();
		}
		TreatyPortMaintainTime = _json["treaty_port_maintain_time"];
		JSONNode jSONNode5 = _json["treaty_port_gates_name"];
		if (!jSONNode5.IsArray)
		{
			throw new SerializationException();
		}
		int count5 = jSONNode5.Count;
		TreatyPortGatesName = new string[count5];
		int num5 = 0;
		foreach (JSONNode child5 in jSONNode5.Children)
		{
			if (!child5.IsString)
			{
				throw new SerializationException();
			}
			string text5 = child5;
			TreatyPortGatesName[num5++] = text5;
		}
		if (!_json["faction_working_hours"].IsObject)
		{
			throw new SerializationException();
		}
		FactionWorkingHours = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["faction_working_hours"]));
		if (!_json["money"].IsNumber)
		{
			throw new SerializationException();
		}
		Money = _json["money"];
		if (!_json["max_money"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxMoney = _json["max_money"];
		if (!_json["init_weather_type"].IsNumber)
		{
			throw new SerializationException();
		}
		InitWeatherType = (WeatherType)_json["init_weather_type"].AsInt;
		if (!_json["init_time"].IsNumber)
		{
			throw new SerializationException();
		}
		InitTime = _json["init_time"];
		if (!_json["training_dungeon_name"].IsString)
		{
			throw new SerializationException();
		}
		TrainingDungeonName = _json["training_dungeon_name"];
		if (!_json["player_default_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlayerDefaultName_l10n_key = _json["player_default_name"]["key"];
		if (!_json["player_default_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlayerDefaultName = _json["player_default_name"]["text"];
		if (!_json["input_player_name_max_length"].IsNumber)
		{
			throw new SerializationException();
		}
		InputPlayerNameMaxLength = _json["input_player_name_max_length"];
		if (!_json["player_default_birthday"].IsObject)
		{
			throw new SerializationException();
		}
		PlayerDefaultBirthday = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["player_default_birthday"]));
		if (!_json["init_hat"].IsString)
		{
			throw new SerializationException();
		}
		InitHat = _json["init_hat"];
		if (!_json["max_support_height"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxSupportHeight = _json["max_support_height"];
		if (!_json["min_floor_covered_ratio"].IsNumber)
		{
			throw new SerializationException();
		}
		MinFloorCoveredRatio = _json["min_floor_covered_ratio"];
		if (!_json["builder_raycast_distance"].IsNumber)
		{
			throw new SerializationException();
		}
		BuilderRaycastDistance = _json["builder_raycast_distance"];
		if (!_json["builder_platform_height_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		BuilderPlatformHeightLimit = _json["builder_platform_height_limit"];
		if (!_json["builder_platform_width"].IsObject)
		{
			throw new SerializationException();
		}
		BuilderPlatformWidth = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["builder_platform_width"]));
		if (!_json["equipment_shake_intensity"].IsNumber)
		{
			throw new SerializationException();
		}
		EquipmentShakeIntensity = _json["equipment_shake_intensity"];
		if (!_json["platform_shake_intensity"].IsNumber)
		{
			throw new SerializationException();
		}
		PlatformShakeIntensity = _json["platform_shake_intensity"];
		if (!_json["equipment_small_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		EquipmentSmallThreshold = _json["equipment_small_threshold"];
		if (!_json["demolition_recycle_factor"].IsNumber)
		{
			throw new SerializationException();
		}
		DemolitionRecycleFactor = _json["demolition_recycle_factor"];
		if (!_json["builder_animation_node"].IsString)
		{
			throw new SerializationException();
		}
		BuilderAnimationNode = _json["builder_animation_node"];
		if (!_json["builder_move_timer"].IsString)
		{
			throw new SerializationException();
		}
		BuilderMoveTimer = _json["builder_move_timer"];
		if (!_json["temporary_building_count"].IsNumber)
		{
			throw new SerializationException();
		}
		TemporaryBuildingCount = _json["temporary_building_count"];
		if (!_json["TULength"].IsNumber)
		{
			throw new SerializationException();
		}
		TULength = _json["TULength"];
		if (!_json["TU2Min"].IsNumber)
		{
			throw new SerializationException();
		}
		TU2Min = _json["TU2Min"];
		if (!_json["Hour2Min"].IsNumber)
		{
			throw new SerializationException();
		}
		Hour2Min = _json["Hour2Min"];
		if (!_json["Day2Hour"].IsNumber)
		{
			throw new SerializationException();
		}
		Day2Hour = _json["Day2Hour"];
		if (!_json["Month2Day"].IsNumber)
		{
			throw new SerializationException();
		}
		Month2Day = _json["Month2Day"];
		if (!_json["Year2Month"].IsNumber)
		{
			throw new SerializationException();
		}
		Year2Month = _json["Year2Month"];
		if (!_json["event_refresh_clock"].IsNumber)
		{
			throw new SerializationException();
		}
		EventRefreshClock = _json["event_refresh_clock"];
		if (!_json["total_day_offset"].IsNumber)
		{
			throw new SerializationException();
		}
		TotalDayOffset = _json["total_day_offset"];
		if (!_json["weather_update_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		WeatherUpdateInterval = _json["weather_update_interval"];
		if (!_json["sleep_time"].IsNumber)
		{
			throw new SerializationException();
		}
		SleepTime = _json["sleep_time"];
		if (!_json["default_switch_schedule_asseet"].IsObject)
		{
			throw new SerializationException();
		}
		DefaultSwitchScheduleAsseet = ExternalTypeUtil.SwitchScheduleAssetConverter(CfgSwitchScheduleAsset.DeserializeCfgSwitchScheduleAsset(_json["default_switch_schedule_asseet"]));
		if (!_json["drone_switch_schedule_asset"].IsObject)
		{
			throw new SerializationException();
		}
		DroneSwitchScheduleAsset = ExternalTypeUtil.SwitchScheduleAssetConverter(CfgSwitchScheduleAsset.DeserializeCfgSwitchScheduleAsset(_json["drone_switch_schedule_asset"]));
		if (!_json["speed_up_time_scale"].IsNumber)
		{
			throw new SerializationException();
		}
		SpeedUpTimeScale = _json["speed_up_time_scale"];
		if (!_json["hold_on_interval_in_dialogue_normal"].IsNumber)
		{
			throw new SerializationException();
		}
		HoldOnIntervalInDialogueNormal = _json["hold_on_interval_in_dialogue_normal"];
		if (!_json["hold_on_interval_in_dialogue_speed_up"].IsNumber)
		{
			throw new SerializationException();
		}
		HoldOnIntervalInDialogueSpeedUp = _json["hold_on_interval_in_dialogue_speed_up"];
		if (!_json["homepage_bgm_event"].IsString)
		{
			throw new SerializationException();
		}
		HomepageBgmEvent = _json["homepage_bgm_event"];
		JSONNode jSONNode6 = _json["bgm_random_idle_time"];
		if (!jSONNode6.IsObject)
		{
			throw new SerializationException();
		}
		if (!jSONNode6["x"].IsNumber)
		{
			throw new SerializationException();
		}
		float x = jSONNode6["x"];
		if (!jSONNode6["y"].IsNumber)
		{
			throw new SerializationException();
		}
		float y = jSONNode6["y"];
		BgmRandomIdleTime = new Vector2(x, y);
		if (!_json["bgm_play_timeout_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		BgmPlayTimeoutDuration = _json["bgm_play_timeout_duration"];
		if (!_json["sfx_group_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		SfxGroupInterval = _json["sfx_group_interval"];
		if (!_json["replayable_dialogue_node_count"].IsNumber)
		{
			throw new SerializationException();
		}
		ReplayableDialogueNodeCount = _json["replayable_dialogue_node_count"];
		if (!_json["init_health"].IsNumber)
		{
			throw new SerializationException();
		}
		InitHealth = _json["init_health"];
		if (!_json["init_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		InitEnergy = _json["init_energy"];
		if (!_json["init_spirit"].IsNumber)
		{
			throw new SerializationException();
		}
		InitSpirit = _json["init_spirit"];
		if (!_json["max_overflow_health"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxOverflowHealth = _json["max_overflow_health"];
		if (!_json["max_overflow_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxOverflowEnergy = _json["max_overflow_energy"];
		if (!_json["health_percent_after_faint"].IsNumber)
		{
			throw new SerializationException();
		}
		HealthPercentAfterFaint = _json["health_percent_after_faint"];
		if (!_json["energy_percent_after_faint"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyPercentAfterFaint = _json["energy_percent_after_faint"];
		if (!_json["spirit_percent_after_faint"].IsNumber)
		{
			throw new SerializationException();
		}
		SpiritPercentAfterFaint = _json["spirit_percent_after_faint"];
		if (!_json["corrosion_counter_length"].IsNumber)
		{
			throw new SerializationException();
		}
		CorrosionCounterLength = _json["corrosion_counter_length"];
		if (!_json["corrosion_health_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		CorrosionHealthCost = _json["corrosion_health_cost"];
		if (!_json["hit_back_distance"].IsNumber)
		{
			throw new SerializationException();
		}
		HitBackDistance = _json["hit_back_distance"];
		if (!_json["agent_defend"].IsNumber)
		{
			throw new SerializationException();
		}
		AgentDefend = _json["agent_defend"];
		if (!_json["tool_energy_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		ToolEnergyCost = _json["tool_energy_cost"];
		if (!_json["dash_cd_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		DashCdDuration = _json["dash_cd_duration"];
		if (!_json["defend_adjust"].IsNumber)
		{
			throw new SerializationException();
		}
		DefendAdjust = _json["defend_adjust"];
		if (!_json["critical_damage_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		CriticalDamageRate = _json["critical_damage_rate"];
		if (!_json["sun_growth_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		SunGrowthAddition = _json["sun_growth_addition"];
		JSONNode jSONNode7 = _json["cloned_crop_gene_group"];
		if (!jSONNode7.IsArray)
		{
			throw new SerializationException();
		}
		int count6 = jSONNode7.Count;
		ClonedCropGeneGroup = new string[count6];
		int num6 = 0;
		foreach (JSONNode child6 in jSONNode7.Children)
		{
			if (!child6.IsString)
			{
				throw new SerializationException();
			}
			string text6 = child6;
			ClonedCropGeneGroup[num6++] = text6;
		}
		JSONNode jSONNode8 = _json["compress_gene_fixed"];
		if (!jSONNode8.IsArray)
		{
			throw new SerializationException();
		}
		int count7 = jSONNode8.Count;
		CompressGeneFixed = new string[count7];
		int num7 = 0;
		foreach (JSONNode child7 in jSONNode8.Children)
		{
			if (!child7.IsString)
			{
				throw new SerializationException();
			}
			string text7 = child7;
			CompressGeneFixed[num7++] = text7;
		}
		JSONNode jSONNode9 = _json["compress_gene_count_dist_pre"];
		if (!jSONNode9.IsArray)
		{
			throw new SerializationException();
		}
		int count8 = jSONNode9.Count;
		CompressGeneCountDistPre = new Vector2[count8];
		int num8 = 0;
		foreach (JSONNode child8 in jSONNode9.Children)
		{
			if (!child8.IsObject)
			{
				throw new SerializationException();
			}
			if (!child8["x"].IsNumber)
			{
				throw new SerializationException();
			}
			float x2 = child8["x"];
			if (!child8["y"].IsNumber)
			{
				throw new SerializationException();
			}
			float y2 = child8["y"];
			Vector2 vector = new Vector2(x2, y2);
			CompressGeneCountDistPre[num8++] = vector;
		}
		JSONNode jSONNode10 = _json["compress_gene_count_dist_post"];
		if (!jSONNode10.IsArray)
		{
			throw new SerializationException();
		}
		int count9 = jSONNode10.Count;
		CompressGeneCountDistPost = new Vector2[count9];
		int num9 = 0;
		foreach (JSONNode child9 in jSONNode10.Children)
		{
			if (!child9.IsObject)
			{
				throw new SerializationException();
			}
			if (!child9["x"].IsNumber)
			{
				throw new SerializationException();
			}
			float x3 = child9["x"];
			if (!child9["y"].IsNumber)
			{
				throw new SerializationException();
			}
			float y3 = child9["y"];
			Vector2 vector2 = new Vector2(x3, y3);
			CompressGeneCountDistPost[num9++] = vector2;
		}
		JSONNode jSONNode11 = _json["synthesis_gene_count_dist"];
		if (!jSONNode11.IsArray)
		{
			throw new SerializationException();
		}
		int count10 = jSONNode11.Count;
		SynthesisGeneCountDist = new Vector2[count10];
		int num10 = 0;
		foreach (JSONNode child10 in jSONNode11.Children)
		{
			if (!child10.IsObject)
			{
				throw new SerializationException();
			}
			if (!child10["x"].IsNumber)
			{
				throw new SerializationException();
			}
			float x4 = child10["x"];
			if (!child10["y"].IsNumber)
			{
				throw new SerializationException();
			}
			float y4 = child10["y"];
			Vector2 vector3 = new Vector2(x4, y4);
			SynthesisGeneCountDist[num10++] = vector3;
		}
		if (!_json["building_damage_sprite_assets"].IsObject)
		{
			throw new SerializationException();
		}
		BuildingDamageSpriteAssets = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["building_damage_sprite_assets"]));
		if (!_json["building_link_threshold_left"].IsNumber)
		{
			throw new SerializationException();
		}
		BuildingLinkThresholdLeft = _json["building_link_threshold_left"];
		if (!_json["building_link_threshold_right"].IsNumber)
		{
			throw new SerializationException();
		}
		BuildingLinkThresholdRight = _json["building_link_threshold_right"];
		if (!_json["building_link_threshold_top"].IsNumber)
		{
			throw new SerializationException();
		}
		BuildingLinkThresholdTop = _json["building_link_threshold_top"];
		if (!_json["building_link_threshold_bottom"].IsNumber)
		{
			throw new SerializationException();
		}
		BuildingLinkThresholdBottom = _json["building_link_threshold_bottom"];
		if (!_json["max_gene_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxGeneCount = _json["max_gene_count"];
		if (!_json["cellar_building_id"].IsString)
		{
			throw new SerializationException();
		}
		CellarBuildingId = _json["cellar_building_id"];
		if (!_json["cellar_distance_to_farm_right"].IsNumber)
		{
			throw new SerializationException();
		}
		CellarDistanceToFarmRight = _json["cellar_distance_to_farm_right"];
		if (!_json["cellar_link_gate_clip_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		CellarLinkGateClipThreshold = _json["cellar_link_gate_clip_threshold"];
		if (!_json["high_level_resource_min_probability_in_dungeon"].IsNumber)
		{
			throw new SerializationException();
		}
		HighLevelResourceMinProbabilityInDungeon = _json["high_level_resource_min_probability_in_dungeon"];
		if (!_json["dungeon_max_history_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		DungeonMaxHistoryThreshold = _json["dungeon_max_history_threshold"];
		if (!_json["cast_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		CastDuration = _json["cast_duration"];
		if (!_json["perfect_cast_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		PerfectCastDuration = _json["perfect_cast_duration"];
		if (!_json["cast_cancel_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		CastCancelThreshold = _json["cast_cancel_threshold"];
		if (!_json["fishing_roll_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingRollInterval = _json["fishing_roll_interval"];
		if (!_json["fishing_bite_init_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingBiteInitProbability = _json["fishing_bite_init_probability"];
		if (!_json["fishing_bite_additional_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingBiteAdditionalProbability = _json["fishing_bite_additional_probability"];
		if (!_json["pull_timing"].IsNumber)
		{
			throw new SerializationException();
		}
		PullTiming = _json["pull_timing"];
		if (!_json["fishing_energy_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingEnergyCost = _json["fishing_energy_cost"];
		if (!_json["fishing_operation_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingOperationInterval = _json["fishing_operation_interval"];
		if (!_json["fishing_note_scroll_speed_base"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingNoteScrollSpeedBase = _json["fishing_note_scroll_speed_base"];
		if (!_json["fishing_note_scroll_speed_bonus"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingNoteScrollSpeedBonus = _json["fishing_note_scroll_speed_bonus"];
		if (!_json["fishing_note_bar_width_pixel"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingNoteBarWidthPixel = _json["fishing_note_bar_width_pixel"];
		if (!_json["fishing_progress_bar_width_pixel"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingProgressBarWidthPixel = _json["fishing_progress_bar_width_pixel"];
		if (!_json["fishing_animation_minimum_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingAnimationMinimumInterval = _json["fishing_animation_minimum_interval"];
		if (!_json["fishing_punish_animation_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingPunishAnimationDuration = _json["fishing_punish_animation_duration"];
		if (!_json["fishing_punish_start_time"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingPunishStartTime = _json["fishing_punish_start_time"];
		if (!_json["fishing_game_preparation_time_scale"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingGamePreparationTimeScale = _json["fishing_game_preparation_time_scale"];
		if (!_json["fishing_note_speed_decrease_per_level"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingNoteSpeedDecreasePerLevel = _json["fishing_note_speed_decrease_per_level"];
		if (!_json["acid_rain_damage"].IsNumber)
		{
			throw new SerializationException();
		}
		AcidRainDamage = _json["acid_rain_damage"];
		if (!_json["scorch_sun_damage"].IsNumber)
		{
			throw new SerializationException();
		}
		ScorchSunDamage = _json["scorch_sun_damage"];
		if (!_json["thunder_frequency"].IsObject)
		{
			throw new SerializationException();
		}
		ThunderFrequency = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["thunder_frequency"]));
		if (!_json["thunder_power"].IsNumber)
		{
			throw new SerializationException();
		}
		ThunderPower = _json["thunder_power"];
		if (!_json["thunder_damge_to_crop"].IsNumber)
		{
			throw new SerializationException();
		}
		ThunderDamgeToCrop = _json["thunder_damge_to_crop"];
		if (!_json["thunder_damge_to_building"].IsNumber)
		{
			throw new SerializationException();
		}
		ThunderDamgeToBuilding = _json["thunder_damge_to_building"];
		if (!_json["sun_light_intensity_adder_factor"].IsNumber)
		{
			throw new SerializationException();
		}
		SunLightIntensityAdderFactor = _json["sun_light_intensity_adder_factor"];
		if (!_json["weather_transit_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		WeatherTransitDuration = _json["weather_transit_duration"];
		JSONNode jSONNode12 = _json["light_intensity_decay_factor_per_lamp_inside"];
		if (!jSONNode12.IsArray)
		{
			throw new SerializationException();
		}
		int count11 = jSONNode12.Count;
		LightIntensityDecayFactorPerLampInside = new float[count11];
		int num11 = 0;
		foreach (JSONNode child11 in jSONNode12.Children)
		{
			if (!child11.IsNumber)
			{
				throw new SerializationException();
			}
			float num12 = child11;
			LightIntensityDecayFactorPerLampInside[num11++] = num12;
		}
		JSONNode jSONNode13 = _json["light_intensity_decay_factor_per_lamp_outside"];
		if (!jSONNode13.IsArray)
		{
			throw new SerializationException();
		}
		int count12 = jSONNode13.Count;
		LightIntensityDecayFactorPerLampOutside = new float[count12];
		int num13 = 0;
		foreach (JSONNode child12 in jSONNode13.Children)
		{
			if (!child12.IsNumber)
			{
				throw new SerializationException();
			}
			float num14 = child12;
			LightIntensityDecayFactorPerLampOutside[num13++] = num14;
		}
		if (!_json["light_intensity_decay_lamp_area"].IsObject)
		{
			throw new SerializationException();
		}
		LightIntensityDecayLampArea = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["light_intensity_decay_lamp_area"]));
		JSONNode jSONNode14 = _json["env_light_intensity_adder_per_lamp"];
		if (!jSONNode14.IsArray)
		{
			throw new SerializationException();
		}
		int count13 = jSONNode14.Count;
		EnvLightIntensityAdderPerLamp = new float[count13];
		int num15 = 0;
		foreach (JSONNode child13 in jSONNode14.Children)
		{
			if (!child13.IsNumber)
			{
				throw new SerializationException();
			}
			float num16 = child13;
			EnvLightIntensityAdderPerLamp[num15++] = num16;
		}
		if (!_json["env_light_default_color"].IsObject)
		{
			throw new SerializationException();
		}
		EnvLightDefaultColor = ExternalTypeUtil.ColorConverter(CfgHexColor.DeserializeCfgHexColor(_json["env_light_default_color"]));
		if (!_json["weather_regulator_cd_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		WeatherRegulatorCdDuration = _json["weather_regulator_cd_duration"];
		if (!_json["weather_regulator_launch_power"].IsNumber)
		{
			throw new SerializationException();
		}
		WeatherRegulatorLaunchPower = _json["weather_regulator_launch_power"];
		if (!_json["motor_horizontal_acceleration"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorHorizontalAcceleration = _json["motor_horizontal_acceleration"];
		if (!_json["motor_horizontal_revert_acceleration"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorHorizontalRevertAcceleration = _json["motor_horizontal_revert_acceleration"];
		if (!_json["motor_horizontal_deceleration"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorHorizontalDeceleration = _json["motor_horizontal_deceleration"];
		if (!_json["motor_horizontal_max_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorHorizontalMaxSpeed = _json["motor_horizontal_max_speed"];
		if (!_json["motor_horizontal_rebound_speed_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorHorizontalReboundSpeedRate = _json["motor_horizontal_rebound_speed_rate"];
		if (!_json["motor_gravity"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorGravity = _json["motor_gravity"];
		if (!_json["motor_drop_max_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorDropMaxSpeed = _json["motor_drop_max_speed"];
		if (!_json["motor_raycast_length"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorRaycastLength = _json["motor_raycast_length"];
		if (!_json["motor_natural_jump_acc_range"].IsObject)
		{
			throw new SerializationException();
		}
		MotorNaturalJumpAccRange = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["motor_natural_jump_acc_range"]));
		if (!_json["motor_natural_jump_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorNaturalJumpThreshold = _json["motor_natural_jump_threshold"];
		if (!_json["motor_natural_jump_max_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorNaturalJumpMaxSpeed = _json["motor_natural_jump_max_speed"];
		if (!_json["motor_vertical_acceleration"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorVerticalAcceleration = _json["motor_vertical_acceleration"];
		if (!_json["motor_vertical_max_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorVerticalMaxSpeed = _json["motor_vertical_max_speed"];
		if (!_json["motor_endurance_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorEnduranceDuration = _json["motor_endurance_duration"];
		if (!_json["motor_endurance_recv"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorEnduranceRecv = _json["motor_endurance_recv"];
		if (!_json["motor_collision_vertical_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorCollisionVerticalSpeed = _json["motor_collision_vertical_speed"];
		if (!_json["motor_vertical_rebound_speed_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorVerticalReboundSpeedRate = _json["motor_vertical_rebound_speed_rate"];
		if (!_json["motor_call_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MotorCallSpeed = _json["motor_call_speed"];
		if (!_json["ui_node_message_hold_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		UiNodeMessageHoldDuration = _json["ui_node_message_hold_duration"];
		if (!_json["fade_default_duration_on_month_change"].IsNumber)
		{
			throw new SerializationException();
		}
		FadeDefaultDurationOnMonthChange = _json["fade_default_duration_on_month_change"];
		if (!_json["fade_default_duration_on_sleep"].IsNumber)
		{
			throw new SerializationException();
		}
		FadeDefaultDurationOnSleep = _json["fade_default_duration_on_sleep"];
		if (!_json["rebind_action_interrupt_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		RebindActionInterruptDuration = _json["rebind_action_interrupt_duration"];
		if (!_json["hide_cursor_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		HideCursorDuration = _json["hide_cursor_duration"];
		JSONNode jSONNode15 = _json["ui_action_exclusive_binds"];
		if (!jSONNode15.IsArray)
		{
			throw new SerializationException();
		}
		int count14 = jSONNode15.Count;
		UiActionExclusiveBinds = new string[count14];
		int num17 = 0;
		foreach (JSONNode child14 in jSONNode15.Children)
		{
			if (!child14.IsString)
			{
				throw new SerializationException();
			}
			string text8 = child14;
			UiActionExclusiveBinds[num17++] = text8;
		}
		JSONNode jSONNode16 = _json["ui_action_horizontal_move"];
		if (!jSONNode16.IsArray)
		{
			throw new SerializationException();
		}
		int count15 = jSONNode16.Count;
		UiActionHorizontalMove = new string[count15];
		int num18 = 0;
		foreach (JSONNode child15 in jSONNode16.Children)
		{
			if (!child15.IsString)
			{
				throw new SerializationException();
			}
			string text9 = child15;
			UiActionHorizontalMove[num18++] = text9;
		}
		JSONNode jSONNode17 = _json["ui_action_vertical_move"];
		if (!jSONNode17.IsArray)
		{
			throw new SerializationException();
		}
		int count16 = jSONNode17.Count;
		UiActionVerticalMove = new string[count16];
		int num19 = 0;
		foreach (JSONNode child16 in jSONNode17.Children)
		{
			if (!child16.IsString)
			{
				throw new SerializationException();
			}
			string text10 = child16;
			UiActionVerticalMove[num19++] = text10;
		}
		if (!_json["rebind_action_setting_group"].IsString)
		{
			throw new SerializationException();
		}
		RebindActionSettingGroup = _json["rebind_action_setting_group"];
		if (!_json["ui_button_long_click_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		UiButtonLongClickDuration = _json["ui_button_long_click_duration"];
		if (!_json["ui_scene_text_tip_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		UiSceneTextTipDuration = _json["ui_scene_text_tip_duration"];
		if (!_json["ui_scene_info_tip_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		UiSceneInfoTipDuration = _json["ui_scene_info_tip_duration"];
		if (!_json["input_memo_max_length"].IsNumber)
		{
			throw new SerializationException();
		}
		InputMemoMaxLength = _json["input_memo_max_length"];
		if (!_json["item_subscript_has_gene"].IsObject)
		{
			throw new SerializationException();
		}
		ItemSubscriptHasGene = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["item_subscript_has_gene"]));
		if (!_json["item_subscript_clone"].IsObject)
		{
			throw new SerializationException();
		}
		ItemSubscriptClone = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["item_subscript_clone"]));
		if (!_json["ui_player_default_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		UiPlayerDefaultSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_player_default_sprite"]));
		if (!_json["ui_player_hair_default_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		UiPlayerHairDefaultSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_player_hair_default_sprite"]));
		if (!_json["ui_player_body_default_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		UiPlayerBodyDefaultSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_player_body_default_sprite"]));
		if (!_json["ui_player_default_portrait"].IsObject)
		{
			throw new SerializationException();
		}
		UiPlayerDefaultPortrait = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_player_default_portrait"]));
		if (!_json["default_vehicle_preview"].IsObject)
		{
			throw new SerializationException();
		}
		DefaultVehiclePreview = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["default_vehicle_preview"]));
		if (!_json["chip_item_name"].IsString)
		{
			throw new SerializationException();
		}
		ChipItemName = _json["chip_item_name"];
		if (!_json["chip_submit_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		ChipSubmitLimit = _json["chip_submit_limit"];
		if (!_json["chip_analyze_hour"].IsNumber)
		{
			throw new SerializationException();
		}
		ChipAnalyzeHour = _json["chip_analyze_hour"];
		if (!_json["chip_analyze_speed_buff"].IsNumber)
		{
			throw new SerializationException();
		}
		ChipAnalyzeSpeedBuff = _json["chip_analyze_speed_buff"];
		if (!_json["chip_progress_check_dialogue_node"].IsString)
		{
			throw new SerializationException();
		}
		ChipProgressCheckDialogueNode = _json["chip_progress_check_dialogue_node"];
		JSONNode jSONNode18 = _json["water_items"];
		if (!jSONNode18.IsArray)
		{
			throw new SerializationException();
		}
		int count17 = jSONNode18.Count;
		WaterItems = new string[count17];
		int num20 = 0;
		foreach (JSONNode child17 in jSONNode18.Children)
		{
			if (!child17.IsString)
			{
				throw new SerializationException();
			}
			string text11 = child17;
			WaterItems[num20++] = text11;
		}
		if (!_json["better_water_extra_buff"].IsObject)
		{
			throw new SerializationException();
		}
		BetterWaterExtraBuff = FoodEffect.DeserializeFoodEffect(_json["better_water_extra_buff"]);
		if (!_json["animal_thunder_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalThunderProbability = _json["animal_thunder_probability"];
		if (!_json["animal_thunder_mood_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalThunderMoodDecrease = _json["animal_thunder_mood_decrease"];
		if (!_json["animal_unhappy_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalUnhappyThreshold = _json["animal_unhappy_threshold"];
		if (!_json["animal_weakness_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalWeaknessThreshold = _json["animal_weakness_threshold"];
		if (!_json["animal_techpoint_fondle"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalTechpointFondle = _json["animal_techpoint_fondle"];
		if (!_json["animal_mood_update_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodUpdateInterval = _json["animal_mood_update_interval"];
		if (!_json["animal_mood_contribution_weather"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodContributionWeather = _json["animal_mood_contribution_weather"];
		if (!_json["animal_mood_contribution_hungry"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodContributionHungry = _json["animal_mood_contribution_hungry"];
		if (!_json["animal_mood_contribution_full"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodContributionFull = _json["animal_mood_contribution_full"];
		if (!_json["animal_mood_contribution_full_toilet"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodContributionFullToilet = _json["animal_mood_contribution_full_toilet"];
		if (!_json["animal_mood_contribution_range_equipment"].IsObject)
		{
			throw new SerializationException();
		}
		AnimalMoodContributionRangeEquipment = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["animal_mood_contribution_range_equipment"]));
		if (!_json["animal_energy_cost_percent_in_night"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalEnergyCostPercentInNight = _json["animal_energy_cost_percent_in_night"];
		if (!_json["animal_mood_show_flag_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalMoodShowFlagThreshold = _json["animal_mood_show_flag_threshold"];
		if (!_json["animal_chickennest_energy_require"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalChickennestEnergyRequire = _json["animal_chickennest_energy_require"];
		if (!_json["aqua_default_weight"].IsNumber)
		{
			throw new SerializationException();
		}
		AquaDefaultWeight = _json["aqua_default_weight"];
		if (!_json["aqua_techpoint_produce"].IsNumber)
		{
			throw new SerializationException();
		}
		AquaTechpointProduce = _json["aqua_techpoint_produce"];
		if (!_json["item_ref_time_bomb"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefTimeBomb = _json["item_ref_time_bomb"];
		if (!_json["item_ref_bottle_of_water"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefBottleOfWater = _json["item_ref_bottle_of_water"];
		if (!_json["item_ref_waste_plastic_bottle"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefWastePlasticBottle = _json["item_ref_waste_plastic_bottle"];
		if (!_json["item_ref_faeces"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefFaeces = _json["item_ref_faeces"];
		if (!_json["item_ref_sturdy_sack"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefSturdySack = _json["item_ref_sturdy_sack"];
		if (!_json["item_ref_weeds"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefWeeds = _json["item_ref_weeds"];
		if (!_json["item_ref_ticket"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefTicket = _json["item_ref_ticket"];
		if (!_json["item_ref_box"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefBox = _json["item_ref_box"];
		if (!_json["item_ref_seed_endyam"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefSeedEndyam = _json["item_ref_seed_endyam"];
		if (!_json["item_ref_roasted_endyam"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefRoastedEndyam = _json["item_ref_roasted_endyam"];
		if (!_json["item_ref_fish_fry"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefFishFry = _json["item_ref_fish_fry"];
		if (!_json["item_ref_gene_capsule_empty"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefGeneCapsuleEmpty = _json["item_ref_gene_capsule_empty"];
		if (!_json["item_ref_old_battery"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefOldBattery = _json["item_ref_old_battery"];
		if (!_json["item_ref_milk"].IsString)
		{
			throw new SerializationException();
		}
		ItemRefMilk = _json["item_ref_milk"];
	}

	public GlobalParameterInfo(int inventory_line_capacity, string achievement_graph_name, string[] item_collection_labels, string[] food_item_sub_types, string item_default_type, int fish_tank_max_count_of_slots, int liking_ceiling, float liking_ratio_on_birthday, int liking_birthday_party, int upper_favorability_level, int weekly_gift_limit, int liking_first_greeting_day, int liking_activity, Vector2Int npc_relax_lightman_fishing_interval, float combination_key_hold_duration, string quick_inventory_timer, string quantity_select_timer, string continuously_use_item_timer, string continuously_interact_timer, string ticket_item_name, int station_ticket_markup, float gravel_block_last_duration, float gravel_block_refresh_duration, int mission_upper_limit, int take_time_limit_fixed, int take_time_limit_random, int mission_refresh_day, Vector3Int mission_refresh_weights, string drop_off_box_sotre_id, string[] seed_store_ids, string[] items_can_not_buyback, Vector2Int inventory_around_area, Vector2Int inventory_around_offset, int equipment_boundary_padding, Vector2Int builder_around_area, PrefabAsset ui_group_asset, string default_ui_entity_info, int nap_hour, int wake_up_clock, float recovered_player_values_per_hour, float full_sleep_buff_threshold, string full_sleep_buff, float sit_option_delay, int faint_minutes_after_drowning, int treaty_port_maintain_time, string[] treaty_port_gates_name, Vector2Int faction_working_hours, int money, int max_money, WeatherType init_weather_type, int init_time, string training_dungeon_name, string player_default_name, int input_player_name_max_length, Vector2Int player_default_birthday, string init_hat, int max_support_height, float min_floor_covered_ratio, float builder_raycast_distance, int builder_platform_height_limit, Vector2Int builder_platform_width, float equipment_shake_intensity, float platform_shake_intensity, int equipment_small_threshold, float demolition_recycle_factor, string builder_animation_node, string builder_move_timer, int temporary_building_count, int TULength, int TU2Min, int Hour2Min, int Day2Hour, int Month2Day, int Year2Month, int event_refresh_clock, int total_day_offset, int weather_update_interval, int sleep_time, SwitchScheduleAsset default_switch_schedule_asseet, SwitchScheduleAsset drone_switch_schedule_asset, float speed_up_time_scale, float hold_on_interval_in_dialogue_normal, float hold_on_interval_in_dialogue_speed_up, string homepage_bgm_event, Vector2 bgm_random_idle_time, int bgm_play_timeout_duration, float sfx_group_interval, int replayable_dialogue_node_count, int init_health, int init_energy, int init_spirit, int max_overflow_health, int max_overflow_energy, float health_percent_after_faint, float energy_percent_after_faint, float spirit_percent_after_faint, int corrosion_counter_length, int corrosion_health_cost, float hit_back_distance, int agent_defend, int tool_energy_cost, float dash_cd_duration, float defend_adjust, float critical_damage_rate, float sun_growth_addition, string[] cloned_crop_gene_group, string[] compress_gene_fixed, Vector2[] compress_gene_count_dist_pre, Vector2[] compress_gene_count_dist_post, Vector2[] synthesis_gene_count_dist, SpriteAssetArray building_damage_sprite_assets, int building_link_threshold_left, int building_link_threshold_right, int building_link_threshold_top, int building_link_threshold_bottom, int max_gene_count, string cellar_building_id, float cellar_distance_to_farm_right, float cellar_link_gate_clip_threshold, float high_level_resource_min_probability_in_dungeon, int dungeon_max_history_threshold, float cast_duration, float perfect_cast_duration, float cast_cancel_threshold, float fishing_roll_interval, float fishing_bite_init_probability, float fishing_bite_additional_probability, float pull_timing, int fishing_energy_cost, float fishing_operation_interval, int fishing_note_scroll_speed_base, int fishing_note_scroll_speed_bonus, int fishing_note_bar_width_pixel, int fishing_progress_bar_width_pixel, float fishing_animation_minimum_interval, float fishing_punish_animation_duration, float fishing_punish_start_time, float fishing_game_preparation_time_scale, float fishing_note_speed_decrease_per_level, float acid_rain_damage, float scorch_sun_damage, Vector2Int thunder_frequency, int thunder_power, float thunder_damge_to_crop, float thunder_damge_to_building, float sun_light_intensity_adder_factor, float weather_transit_duration, float[] light_intensity_decay_factor_per_lamp_inside, float[] light_intensity_decay_factor_per_lamp_outside, Vector2Int light_intensity_decay_lamp_area, float[] env_light_intensity_adder_per_lamp, Color env_light_default_color, int weather_regulator_cd_duration, float weather_regulator_launch_power, float motor_horizontal_acceleration, float motor_horizontal_revert_acceleration, float motor_horizontal_deceleration, float motor_horizontal_max_speed, float motor_horizontal_rebound_speed_rate, float motor_gravity, float motor_drop_max_speed, float motor_raycast_length, Vector2 motor_natural_jump_acc_range, float motor_natural_jump_threshold, float motor_natural_jump_max_speed, float motor_vertical_acceleration, float motor_vertical_max_speed, float motor_endurance_duration, float motor_endurance_recv, float motor_collision_vertical_speed, float motor_vertical_rebound_speed_rate, float motor_call_speed, float ui_node_message_hold_duration, float fade_default_duration_on_month_change, float fade_default_duration_on_sleep, float rebind_action_interrupt_duration, float hide_cursor_duration, string[] ui_action_exclusive_binds, string[] ui_action_horizontal_move, string[] ui_action_vertical_move, string rebind_action_setting_group, float ui_button_long_click_duration, float ui_scene_text_tip_duration, float ui_scene_info_tip_duration, int input_memo_max_length, SpriteAssetArray item_subscript_has_gene, SpriteAssetArray item_subscript_clone, SpriteAsset ui_player_default_sprite, SpriteAsset ui_player_hair_default_sprite, SpriteAsset ui_player_body_default_sprite, SpriteAsset ui_player_default_portrait, SpriteAsset default_vehicle_preview, string chip_item_name, int chip_submit_limit, int chip_analyze_hour, float chip_analyze_speed_buff, string chip_progress_check_dialogue_node, string[] water_items, FoodEffect better_water_extra_buff, float animal_thunder_probability, int animal_thunder_mood_decrease, int animal_unhappy_threshold, int animal_weakness_threshold, int animal_techpoint_fondle, int animal_mood_update_interval, int animal_mood_contribution_weather, int animal_mood_contribution_hungry, int animal_mood_contribution_full, int animal_mood_contribution_full_toilet, Vector2Int animal_mood_contribution_range_equipment, float animal_energy_cost_percent_in_night, int animal_mood_show_flag_threshold, int animal_chickennest_energy_require, int aqua_default_weight, int aqua_techpoint_produce, string item_ref_time_bomb, string item_ref_bottle_of_water, string item_ref_waste_plastic_bottle, string item_ref_faeces, string item_ref_sturdy_sack, string item_ref_weeds, string item_ref_ticket, string item_ref_box, string item_ref_seed_endyam, string item_ref_roasted_endyam, string item_ref_fish_fry, string item_ref_gene_capsule_empty, string item_ref_old_battery, string item_ref_milk)
	{
		InventoryLineCapacity = inventory_line_capacity;
		AchievementGraphName = achievement_graph_name;
		ItemCollectionLabels = item_collection_labels;
		FoodItemSubTypes = food_item_sub_types;
		ItemDefaultType = item_default_type;
		FishTankMaxCountOfSlots = fish_tank_max_count_of_slots;
		LikingCeiling = liking_ceiling;
		LikingRatioOnBirthday = liking_ratio_on_birthday;
		LikingBirthdayParty = liking_birthday_party;
		UpperFavorabilityLevel = upper_favorability_level;
		WeeklyGiftLimit = weekly_gift_limit;
		LikingFirstGreetingDay = liking_first_greeting_day;
		LikingActivity = liking_activity;
		NpcRelaxLightmanFishingInterval = npc_relax_lightman_fishing_interval;
		CombinationKeyHoldDuration = combination_key_hold_duration;
		QuickInventoryTimer = quick_inventory_timer;
		QuantitySelectTimer = quantity_select_timer;
		ContinuouslyUseItemTimer = continuously_use_item_timer;
		ContinuouslyInteractTimer = continuously_interact_timer;
		TicketItemName = ticket_item_name;
		StationTicketMarkup = station_ticket_markup;
		GravelBlockLastDuration = gravel_block_last_duration;
		GravelBlockRefreshDuration = gravel_block_refresh_duration;
		MissionUpperLimit = mission_upper_limit;
		TakeTimeLimitFixed = take_time_limit_fixed;
		TakeTimeLimitRandom = take_time_limit_random;
		MissionRefreshDay = mission_refresh_day;
		MissionRefreshWeights = mission_refresh_weights;
		DropOffBoxSotreId = drop_off_box_sotre_id;
		SeedStoreIds = seed_store_ids;
		ItemsCanNotBuyback = items_can_not_buyback;
		InventoryAroundArea = inventory_around_area;
		InventoryAroundOffset = inventory_around_offset;
		EquipmentBoundaryPadding = equipment_boundary_padding;
		BuilderAroundArea = builder_around_area;
		UiGroupAsset = ui_group_asset;
		DefaultUiEntityInfo = default_ui_entity_info;
		NapHour = nap_hour;
		WakeUpClock = wake_up_clock;
		RecoveredPlayerValuesPerHour = recovered_player_values_per_hour;
		FullSleepBuffThreshold = full_sleep_buff_threshold;
		FullSleepBuff = full_sleep_buff;
		SitOptionDelay = sit_option_delay;
		FaintMinutesAfterDrowning = faint_minutes_after_drowning;
		TreatyPortMaintainTime = treaty_port_maintain_time;
		TreatyPortGatesName = treaty_port_gates_name;
		FactionWorkingHours = faction_working_hours;
		Money = money;
		MaxMoney = max_money;
		InitWeatherType = init_weather_type;
		InitTime = init_time;
		TrainingDungeonName = training_dungeon_name;
		PlayerDefaultName = player_default_name;
		InputPlayerNameMaxLength = input_player_name_max_length;
		PlayerDefaultBirthday = player_default_birthday;
		InitHat = init_hat;
		MaxSupportHeight = max_support_height;
		MinFloorCoveredRatio = min_floor_covered_ratio;
		BuilderRaycastDistance = builder_raycast_distance;
		BuilderPlatformHeightLimit = builder_platform_height_limit;
		BuilderPlatformWidth = builder_platform_width;
		EquipmentShakeIntensity = equipment_shake_intensity;
		PlatformShakeIntensity = platform_shake_intensity;
		EquipmentSmallThreshold = equipment_small_threshold;
		DemolitionRecycleFactor = demolition_recycle_factor;
		BuilderAnimationNode = builder_animation_node;
		BuilderMoveTimer = builder_move_timer;
		TemporaryBuildingCount = temporary_building_count;
		this.TULength = TULength;
		this.TU2Min = TU2Min;
		this.Hour2Min = Hour2Min;
		this.Day2Hour = Day2Hour;
		this.Month2Day = Month2Day;
		this.Year2Month = Year2Month;
		EventRefreshClock = event_refresh_clock;
		TotalDayOffset = total_day_offset;
		WeatherUpdateInterval = weather_update_interval;
		SleepTime = sleep_time;
		DefaultSwitchScheduleAsseet = default_switch_schedule_asseet;
		DroneSwitchScheduleAsset = drone_switch_schedule_asset;
		SpeedUpTimeScale = speed_up_time_scale;
		HoldOnIntervalInDialogueNormal = hold_on_interval_in_dialogue_normal;
		HoldOnIntervalInDialogueSpeedUp = hold_on_interval_in_dialogue_speed_up;
		HomepageBgmEvent = homepage_bgm_event;
		BgmRandomIdleTime = bgm_random_idle_time;
		BgmPlayTimeoutDuration = bgm_play_timeout_duration;
		SfxGroupInterval = sfx_group_interval;
		ReplayableDialogueNodeCount = replayable_dialogue_node_count;
		InitHealth = init_health;
		InitEnergy = init_energy;
		InitSpirit = init_spirit;
		MaxOverflowHealth = max_overflow_health;
		MaxOverflowEnergy = max_overflow_energy;
		HealthPercentAfterFaint = health_percent_after_faint;
		EnergyPercentAfterFaint = energy_percent_after_faint;
		SpiritPercentAfterFaint = spirit_percent_after_faint;
		CorrosionCounterLength = corrosion_counter_length;
		CorrosionHealthCost = corrosion_health_cost;
		HitBackDistance = hit_back_distance;
		AgentDefend = agent_defend;
		ToolEnergyCost = tool_energy_cost;
		DashCdDuration = dash_cd_duration;
		DefendAdjust = defend_adjust;
		CriticalDamageRate = critical_damage_rate;
		SunGrowthAddition = sun_growth_addition;
		ClonedCropGeneGroup = cloned_crop_gene_group;
		CompressGeneFixed = compress_gene_fixed;
		CompressGeneCountDistPre = compress_gene_count_dist_pre;
		CompressGeneCountDistPost = compress_gene_count_dist_post;
		SynthesisGeneCountDist = synthesis_gene_count_dist;
		BuildingDamageSpriteAssets = building_damage_sprite_assets;
		BuildingLinkThresholdLeft = building_link_threshold_left;
		BuildingLinkThresholdRight = building_link_threshold_right;
		BuildingLinkThresholdTop = building_link_threshold_top;
		BuildingLinkThresholdBottom = building_link_threshold_bottom;
		MaxGeneCount = max_gene_count;
		CellarBuildingId = cellar_building_id;
		CellarDistanceToFarmRight = cellar_distance_to_farm_right;
		CellarLinkGateClipThreshold = cellar_link_gate_clip_threshold;
		HighLevelResourceMinProbabilityInDungeon = high_level_resource_min_probability_in_dungeon;
		DungeonMaxHistoryThreshold = dungeon_max_history_threshold;
		CastDuration = cast_duration;
		PerfectCastDuration = perfect_cast_duration;
		CastCancelThreshold = cast_cancel_threshold;
		FishingRollInterval = fishing_roll_interval;
		FishingBiteInitProbability = fishing_bite_init_probability;
		FishingBiteAdditionalProbability = fishing_bite_additional_probability;
		PullTiming = pull_timing;
		FishingEnergyCost = fishing_energy_cost;
		FishingOperationInterval = fishing_operation_interval;
		FishingNoteScrollSpeedBase = fishing_note_scroll_speed_base;
		FishingNoteScrollSpeedBonus = fishing_note_scroll_speed_bonus;
		FishingNoteBarWidthPixel = fishing_note_bar_width_pixel;
		FishingProgressBarWidthPixel = fishing_progress_bar_width_pixel;
		FishingAnimationMinimumInterval = fishing_animation_minimum_interval;
		FishingPunishAnimationDuration = fishing_punish_animation_duration;
		FishingPunishStartTime = fishing_punish_start_time;
		FishingGamePreparationTimeScale = fishing_game_preparation_time_scale;
		FishingNoteSpeedDecreasePerLevel = fishing_note_speed_decrease_per_level;
		AcidRainDamage = acid_rain_damage;
		ScorchSunDamage = scorch_sun_damage;
		ThunderFrequency = thunder_frequency;
		ThunderPower = thunder_power;
		ThunderDamgeToCrop = thunder_damge_to_crop;
		ThunderDamgeToBuilding = thunder_damge_to_building;
		SunLightIntensityAdderFactor = sun_light_intensity_adder_factor;
		WeatherTransitDuration = weather_transit_duration;
		LightIntensityDecayFactorPerLampInside = light_intensity_decay_factor_per_lamp_inside;
		LightIntensityDecayFactorPerLampOutside = light_intensity_decay_factor_per_lamp_outside;
		LightIntensityDecayLampArea = light_intensity_decay_lamp_area;
		EnvLightIntensityAdderPerLamp = env_light_intensity_adder_per_lamp;
		EnvLightDefaultColor = env_light_default_color;
		WeatherRegulatorCdDuration = weather_regulator_cd_duration;
		WeatherRegulatorLaunchPower = weather_regulator_launch_power;
		MotorHorizontalAcceleration = motor_horizontal_acceleration;
		MotorHorizontalRevertAcceleration = motor_horizontal_revert_acceleration;
		MotorHorizontalDeceleration = motor_horizontal_deceleration;
		MotorHorizontalMaxSpeed = motor_horizontal_max_speed;
		MotorHorizontalReboundSpeedRate = motor_horizontal_rebound_speed_rate;
		MotorGravity = motor_gravity;
		MotorDropMaxSpeed = motor_drop_max_speed;
		MotorRaycastLength = motor_raycast_length;
		MotorNaturalJumpAccRange = motor_natural_jump_acc_range;
		MotorNaturalJumpThreshold = motor_natural_jump_threshold;
		MotorNaturalJumpMaxSpeed = motor_natural_jump_max_speed;
		MotorVerticalAcceleration = motor_vertical_acceleration;
		MotorVerticalMaxSpeed = motor_vertical_max_speed;
		MotorEnduranceDuration = motor_endurance_duration;
		MotorEnduranceRecv = motor_endurance_recv;
		MotorCollisionVerticalSpeed = motor_collision_vertical_speed;
		MotorVerticalReboundSpeedRate = motor_vertical_rebound_speed_rate;
		MotorCallSpeed = motor_call_speed;
		UiNodeMessageHoldDuration = ui_node_message_hold_duration;
		FadeDefaultDurationOnMonthChange = fade_default_duration_on_month_change;
		FadeDefaultDurationOnSleep = fade_default_duration_on_sleep;
		RebindActionInterruptDuration = rebind_action_interrupt_duration;
		HideCursorDuration = hide_cursor_duration;
		UiActionExclusiveBinds = ui_action_exclusive_binds;
		UiActionHorizontalMove = ui_action_horizontal_move;
		UiActionVerticalMove = ui_action_vertical_move;
		RebindActionSettingGroup = rebind_action_setting_group;
		UiButtonLongClickDuration = ui_button_long_click_duration;
		UiSceneTextTipDuration = ui_scene_text_tip_duration;
		UiSceneInfoTipDuration = ui_scene_info_tip_duration;
		InputMemoMaxLength = input_memo_max_length;
		ItemSubscriptHasGene = item_subscript_has_gene;
		ItemSubscriptClone = item_subscript_clone;
		UiPlayerDefaultSprite = ui_player_default_sprite;
		UiPlayerHairDefaultSprite = ui_player_hair_default_sprite;
		UiPlayerBodyDefaultSprite = ui_player_body_default_sprite;
		UiPlayerDefaultPortrait = ui_player_default_portrait;
		DefaultVehiclePreview = default_vehicle_preview;
		ChipItemName = chip_item_name;
		ChipSubmitLimit = chip_submit_limit;
		ChipAnalyzeHour = chip_analyze_hour;
		ChipAnalyzeSpeedBuff = chip_analyze_speed_buff;
		ChipProgressCheckDialogueNode = chip_progress_check_dialogue_node;
		WaterItems = water_items;
		BetterWaterExtraBuff = better_water_extra_buff;
		AnimalThunderProbability = animal_thunder_probability;
		AnimalThunderMoodDecrease = animal_thunder_mood_decrease;
		AnimalUnhappyThreshold = animal_unhappy_threshold;
		AnimalWeaknessThreshold = animal_weakness_threshold;
		AnimalTechpointFondle = animal_techpoint_fondle;
		AnimalMoodUpdateInterval = animal_mood_update_interval;
		AnimalMoodContributionWeather = animal_mood_contribution_weather;
		AnimalMoodContributionHungry = animal_mood_contribution_hungry;
		AnimalMoodContributionFull = animal_mood_contribution_full;
		AnimalMoodContributionFullToilet = animal_mood_contribution_full_toilet;
		AnimalMoodContributionRangeEquipment = animal_mood_contribution_range_equipment;
		AnimalEnergyCostPercentInNight = animal_energy_cost_percent_in_night;
		AnimalMoodShowFlagThreshold = animal_mood_show_flag_threshold;
		AnimalChickennestEnergyRequire = animal_chickennest_energy_require;
		AquaDefaultWeight = aqua_default_weight;
		AquaTechpointProduce = aqua_techpoint_produce;
		ItemRefTimeBomb = item_ref_time_bomb;
		ItemRefBottleOfWater = item_ref_bottle_of_water;
		ItemRefWastePlasticBottle = item_ref_waste_plastic_bottle;
		ItemRefFaeces = item_ref_faeces;
		ItemRefSturdySack = item_ref_sturdy_sack;
		ItemRefWeeds = item_ref_weeds;
		ItemRefTicket = item_ref_ticket;
		ItemRefBox = item_ref_box;
		ItemRefSeedEndyam = item_ref_seed_endyam;
		ItemRefRoastedEndyam = item_ref_roasted_endyam;
		ItemRefFishFry = item_ref_fish_fry;
		ItemRefGeneCapsuleEmpty = item_ref_gene_capsule_empty;
		ItemRefOldBattery = item_ref_old_battery;
		ItemRefMilk = item_ref_milk;
	}

	public static GlobalParameterInfo DeserializeGlobalParameterInfo(JSONNode _json)
	{
		return new GlobalParameterInfo(_json);
	}

	public override int GetTypeId()
	{
		return -861558071;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		int num = ItemCollectionLabels.Length;
		TbItemMainType tbItemMainType = (TbItemMainType)_tables["Item.TbItemMainType"];
		ItemCollectionLabels_Ref = new ItemMainTypeInfo[num];
		for (int i = 0; i < num; i++)
		{
			ItemCollectionLabels_Ref[i] = tbItemMainType.GetOrDefault(ItemCollectionLabels[i]);
		}
		int num2 = FoodItemSubTypes.Length;
		TbItemSubType tbItemSubType = (TbItemSubType)_tables["Item.TbItemSubType"];
		FoodItemSubTypes_Ref = new ItemSubTypeInfo[num2];
		for (int j = 0; j < num2; j++)
		{
			FoodItemSubTypes_Ref[j] = tbItemSubType.GetOrDefault(FoodItemSubTypes[j]);
		}
		ItemDefaultType_Ref = (_tables["Item.TbItemMainType"] as TbItemMainType).GetOrDefault(ItemDefaultType);
		QuickInventoryTimer_Ref = (_tables["Settings.TbTimer"] as TbTimer).GetOrDefault(QuickInventoryTimer);
		QuantitySelectTimer_Ref = (_tables["Settings.TbTimer"] as TbTimer).GetOrDefault(QuantitySelectTimer);
		ContinuouslyUseItemTimer_Ref = (_tables["Settings.TbTimer"] as TbTimer).GetOrDefault(ContinuouslyUseItemTimer);
		ContinuouslyInteractTimer_Ref = (_tables["Settings.TbTimer"] as TbTimer).GetOrDefault(ContinuouslyInteractTimer);
		TicketItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(TicketItemName);
		int num3 = SeedStoreIds.Length;
		TbStore tbStore = (TbStore)_tables["Store.TbStore"];
		SeedStoreIds_Ref = new StoreInfo[num3];
		for (int k = 0; k < num3; k++)
		{
			SeedStoreIds_Ref[k] = tbStore.GetOrDefault(SeedStoreIds[k]);
		}
		int num4 = ItemsCanNotBuyback.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		ItemsCanNotBuyback_Ref = new ItemInfo[num4];
		for (int l = 0; l < num4; l++)
		{
			ItemsCanNotBuyback_Ref[l] = tbItem.GetOrDefault(ItemsCanNotBuyback[l]);
		}
		DefaultUiEntityInfo_Ref = (_tables["UI.TbUIEntity"] as TbUIEntity).GetOrDefault(DefaultUiEntityInfo);
		FullSleepBuff_Ref = (_tables["Buff.TbBuff"] as TbBuff).GetOrDefault(FullSleepBuff);
		int num5 = TreatyPortGatesName.Length;
		TbMarkPoint tbMarkPoint = (TbMarkPoint)_tables["Room.TbMarkPoint"];
		TreatyPortGatesName_Ref = new MarkPointInfo[num5];
		for (int m = 0; m < num5; m++)
		{
			TreatyPortGatesName_Ref[m] = tbMarkPoint.GetOrDefault(TreatyPortGatesName[m]);
		}
		TrainingDungeonName_Ref = (_tables["Room.TbScene"] as TbScene).GetOrDefault(TrainingDungeonName);
		InitHat_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(InitHat);
		BuilderMoveTimer_Ref = (_tables["Settings.TbTimer"] as TbTimer).GetOrDefault(BuilderMoveTimer);
		int num6 = ClonedCropGeneGroup.Length;
		TbCropGene tbCropGene = (TbCropGene)_tables["Plant.TbCropGene"];
		ClonedCropGeneGroup_Ref = new CropGeneInfo[num6];
		for (int n = 0; n < num6; n++)
		{
			ClonedCropGeneGroup_Ref[n] = tbCropGene.GetOrDefault(ClonedCropGeneGroup[n]);
		}
		int num7 = CompressGeneFixed.Length;
		TbCropGene tbCropGene2 = (TbCropGene)_tables["Plant.TbCropGene"];
		CompressGeneFixed_Ref = new CropGeneInfo[num7];
		for (int num8 = 0; num8 < num7; num8++)
		{
			CompressGeneFixed_Ref[num8] = tbCropGene2.GetOrDefault(CompressGeneFixed[num8]);
		}
		CellarBuildingId_Ref = (_tables["Building.TbBuilding"] as TbBuilding).GetOrDefault(CellarBuildingId);
		int num9 = UiActionExclusiveBinds.Length;
		TbRebindAction tbRebindAction = (TbRebindAction)_tables["Settings.TbRebindAction"];
		UiActionExclusiveBinds_Ref = new RebindActionInfo[num9];
		for (int num10 = 0; num10 < num9; num10++)
		{
			UiActionExclusiveBinds_Ref[num10] = tbRebindAction.GetOrDefault(UiActionExclusiveBinds[num10]);
		}
		int num11 = UiActionHorizontalMove.Length;
		TbRebindAction tbRebindAction2 = (TbRebindAction)_tables["Settings.TbRebindAction"];
		UiActionHorizontalMove_Ref = new RebindActionInfo[num11];
		for (int num12 = 0; num12 < num11; num12++)
		{
			UiActionHorizontalMove_Ref[num12] = tbRebindAction2.GetOrDefault(UiActionHorizontalMove[num12]);
		}
		int num13 = UiActionVerticalMove.Length;
		TbRebindAction tbRebindAction3 = (TbRebindAction)_tables["Settings.TbRebindAction"];
		UiActionVerticalMove_Ref = new RebindActionInfo[num13];
		for (int num14 = 0; num14 < num13; num14++)
		{
			UiActionVerticalMove_Ref[num14] = tbRebindAction3.GetOrDefault(UiActionVerticalMove[num14]);
		}
		RebindActionSettingGroup_Ref = (_tables["Settings.TbUserSettingGroup"] as TbUserSettingGroup).GetOrDefault(RebindActionSettingGroup);
		int num15 = WaterItems.Length;
		TbItem tbItem2 = (TbItem)_tables["Item.TbItem"];
		WaterItems_Ref = new ItemInfo[num15];
		for (int num16 = 0; num16 < num15; num16++)
		{
			WaterItems_Ref[num16] = tbItem2.GetOrDefault(WaterItems[num16]);
		}
		BetterWaterExtraBuff?.Resolve(_tables);
		ItemRefTimeBomb_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefTimeBomb);
		ItemRefBottleOfWater_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefBottleOfWater);
		ItemRefWastePlasticBottle_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefWastePlasticBottle);
		ItemRefFaeces_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefFaeces);
		ItemRefSturdySack_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefSturdySack);
		ItemRefWeeds_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefWeeds);
		ItemRefTicket_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefTicket);
		ItemRefBox_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefBox);
		ItemRefSeedEndyam_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefSeedEndyam);
		ItemRefRoastedEndyam_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefRoastedEndyam);
		ItemRefFishFry_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefFishFry);
		ItemRefGeneCapsuleEmpty_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefGeneCapsuleEmpty);
		ItemRefOldBattery_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefOldBattery);
		ItemRefMilk_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemRefMilk);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		PlayerDefaultName = translator(PlayerDefaultName_l10n_key, PlayerDefaultName);
		BetterWaterExtraBuff?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ InventoryLineCapacity:" + InventoryLineCapacity + ",AchievementGraphName:" + AchievementGraphName + ",ItemCollectionLabels:" + StringUtil.CollectionToString(ItemCollectionLabels) + ",FoodItemSubTypes:" + StringUtil.CollectionToString(FoodItemSubTypes) + ",ItemDefaultType:" + ItemDefaultType + ",FishTankMaxCountOfSlots:" + FishTankMaxCountOfSlots + ",LikingCeiling:" + LikingCeiling + ",LikingRatioOnBirthday:" + LikingRatioOnBirthday + ",LikingBirthdayParty:" + LikingBirthdayParty + ",UpperFavorabilityLevel:" + UpperFavorabilityLevel + ",WeeklyGiftLimit:" + WeeklyGiftLimit + ",LikingFirstGreetingDay:" + LikingFirstGreetingDay + ",LikingActivity:" + LikingActivity + ",NpcRelaxLightmanFishingInterval:" + NpcRelaxLightmanFishingInterval.ToString() + ",CombinationKeyHoldDuration:" + CombinationKeyHoldDuration + ",QuickInventoryTimer:" + QuickInventoryTimer + ",QuantitySelectTimer:" + QuantitySelectTimer + ",ContinuouslyUseItemTimer:" + ContinuouslyUseItemTimer + ",ContinuouslyInteractTimer:" + ContinuouslyInteractTimer + ",TicketItemName:" + TicketItemName + ",StationTicketMarkup:" + StationTicketMarkup + ",GravelBlockLastDuration:" + GravelBlockLastDuration + ",GravelBlockRefreshDuration:" + GravelBlockRefreshDuration + ",MissionUpperLimit:" + MissionUpperLimit + ",TakeTimeLimitFixed:" + TakeTimeLimitFixed + ",TakeTimeLimitRandom:" + TakeTimeLimitRandom + ",MissionRefreshDay:" + MissionRefreshDay + ",MissionRefreshWeights:" + MissionRefreshWeights.ToString() + ",DropOffBoxSotreId:" + DropOffBoxSotreId + ",SeedStoreIds:" + StringUtil.CollectionToString(SeedStoreIds) + ",ItemsCanNotBuyback:" + StringUtil.CollectionToString(ItemsCanNotBuyback) + ",InventoryAroundArea:" + InventoryAroundArea.ToString() + ",InventoryAroundOffset:" + InventoryAroundOffset.ToString() + ",EquipmentBoundaryPadding:" + EquipmentBoundaryPadding + ",BuilderAroundArea:" + BuilderAroundArea.ToString() + ",UiGroupAsset:" + UiGroupAsset?.ToString() + ",DefaultUiEntityInfo:" + DefaultUiEntityInfo + ",NapHour:" + NapHour + ",WakeUpClock:" + WakeUpClock + ",RecoveredPlayerValuesPerHour:" + RecoveredPlayerValuesPerHour + ",FullSleepBuffThreshold:" + FullSleepBuffThreshold + ",FullSleepBuff:" + FullSleepBuff + ",SitOptionDelay:" + SitOptionDelay + ",FaintMinutesAfterDrowning:" + FaintMinutesAfterDrowning + ",TreatyPortMaintainTime:" + TreatyPortMaintainTime + ",TreatyPortGatesName:" + StringUtil.CollectionToString(TreatyPortGatesName) + ",FactionWorkingHours:" + FactionWorkingHours.ToString() + ",Money:" + Money + ",MaxMoney:" + MaxMoney + ",InitWeatherType:" + InitWeatherType.ToString() + ",InitTime:" + InitTime + ",TrainingDungeonName:" + TrainingDungeonName + ",PlayerDefaultName:" + PlayerDefaultName + ",InputPlayerNameMaxLength:" + InputPlayerNameMaxLength + ",PlayerDefaultBirthday:" + PlayerDefaultBirthday.ToString() + ",InitHat:" + InitHat + ",MaxSupportHeight:" + MaxSupportHeight + ",MinFloorCoveredRatio:" + MinFloorCoveredRatio + ",BuilderRaycastDistance:" + BuilderRaycastDistance + ",BuilderPlatformHeightLimit:" + BuilderPlatformHeightLimit + ",BuilderPlatformWidth:" + BuilderPlatformWidth.ToString() + ",EquipmentShakeIntensity:" + EquipmentShakeIntensity + ",PlatformShakeIntensity:" + PlatformShakeIntensity + ",EquipmentSmallThreshold:" + EquipmentSmallThreshold + ",DemolitionRecycleFactor:" + DemolitionRecycleFactor + ",BuilderAnimationNode:" + BuilderAnimationNode + ",BuilderMoveTimer:" + BuilderMoveTimer + ",TemporaryBuildingCount:" + TemporaryBuildingCount + ",TULength:" + TULength + ",TU2Min:" + TU2Min + ",Hour2Min:" + Hour2Min + ",Day2Hour:" + Day2Hour + ",Month2Day:" + Month2Day + ",Year2Month:" + Year2Month + ",EventRefreshClock:" + EventRefreshClock + ",TotalDayOffset:" + TotalDayOffset + ",WeatherUpdateInterval:" + WeatherUpdateInterval + ",SleepTime:" + SleepTime + ",DefaultSwitchScheduleAsseet:" + DefaultSwitchScheduleAsseet?.ToString() + ",DroneSwitchScheduleAsset:" + DroneSwitchScheduleAsset?.ToString() + ",SpeedUpTimeScale:" + SpeedUpTimeScale + ",HoldOnIntervalInDialogueNormal:" + HoldOnIntervalInDialogueNormal + ",HoldOnIntervalInDialogueSpeedUp:" + HoldOnIntervalInDialogueSpeedUp + ",HomepageBgmEvent:" + HomepageBgmEvent + ",BgmRandomIdleTime:" + BgmRandomIdleTime.ToString() + ",BgmPlayTimeoutDuration:" + BgmPlayTimeoutDuration + ",SfxGroupInterval:" + SfxGroupInterval + ",ReplayableDialogueNodeCount:" + ReplayableDialogueNodeCount + ",InitHealth:" + InitHealth + ",InitEnergy:" + InitEnergy + ",InitSpirit:" + InitSpirit + ",MaxOverflowHealth:" + MaxOverflowHealth + ",MaxOverflowEnergy:" + MaxOverflowEnergy + ",HealthPercentAfterFaint:" + HealthPercentAfterFaint + ",EnergyPercentAfterFaint:" + EnergyPercentAfterFaint + ",SpiritPercentAfterFaint:" + SpiritPercentAfterFaint + ",CorrosionCounterLength:" + CorrosionCounterLength + ",CorrosionHealthCost:" + CorrosionHealthCost + ",HitBackDistance:" + HitBackDistance + ",AgentDefend:" + AgentDefend + ",ToolEnergyCost:" + ToolEnergyCost + ",DashCdDuration:" + DashCdDuration + ",DefendAdjust:" + DefendAdjust + ",CriticalDamageRate:" + CriticalDamageRate + ",SunGrowthAddition:" + SunGrowthAddition + ",ClonedCropGeneGroup:" + StringUtil.CollectionToString(ClonedCropGeneGroup) + ",CompressGeneFixed:" + StringUtil.CollectionToString(CompressGeneFixed) + ",CompressGeneCountDistPre:" + StringUtil.CollectionToString(CompressGeneCountDistPre) + ",CompressGeneCountDistPost:" + StringUtil.CollectionToString(CompressGeneCountDistPost) + ",SynthesisGeneCountDist:" + StringUtil.CollectionToString(SynthesisGeneCountDist) + ",BuildingDamageSpriteAssets:" + BuildingDamageSpriteAssets?.ToString() + ",BuildingLinkThresholdLeft:" + BuildingLinkThresholdLeft + ",BuildingLinkThresholdRight:" + BuildingLinkThresholdRight + ",BuildingLinkThresholdTop:" + BuildingLinkThresholdTop + ",BuildingLinkThresholdBottom:" + BuildingLinkThresholdBottom + ",MaxGeneCount:" + MaxGeneCount + ",CellarBuildingId:" + CellarBuildingId + ",CellarDistanceToFarmRight:" + CellarDistanceToFarmRight + ",CellarLinkGateClipThreshold:" + CellarLinkGateClipThreshold + ",HighLevelResourceMinProbabilityInDungeon:" + HighLevelResourceMinProbabilityInDungeon + ",DungeonMaxHistoryThreshold:" + DungeonMaxHistoryThreshold + ",CastDuration:" + CastDuration + ",PerfectCastDuration:" + PerfectCastDuration + ",CastCancelThreshold:" + CastCancelThreshold + ",FishingRollInterval:" + FishingRollInterval + ",FishingBiteInitProbability:" + FishingBiteInitProbability + ",FishingBiteAdditionalProbability:" + FishingBiteAdditionalProbability + ",PullTiming:" + PullTiming + ",FishingEnergyCost:" + FishingEnergyCost + ",FishingOperationInterval:" + FishingOperationInterval + ",FishingNoteScrollSpeedBase:" + FishingNoteScrollSpeedBase + ",FishingNoteScrollSpeedBonus:" + FishingNoteScrollSpeedBonus + ",FishingNoteBarWidthPixel:" + FishingNoteBarWidthPixel + ",FishingProgressBarWidthPixel:" + FishingProgressBarWidthPixel + ",FishingAnimationMinimumInterval:" + FishingAnimationMinimumInterval + ",FishingPunishAnimationDuration:" + FishingPunishAnimationDuration + ",FishingPunishStartTime:" + FishingPunishStartTime + ",FishingGamePreparationTimeScale:" + FishingGamePreparationTimeScale + ",FishingNoteSpeedDecreasePerLevel:" + FishingNoteSpeedDecreasePerLevel + ",AcidRainDamage:" + AcidRainDamage + ",ScorchSunDamage:" + ScorchSunDamage + ",ThunderFrequency:" + ThunderFrequency.ToString() + ",ThunderPower:" + ThunderPower + ",ThunderDamgeToCrop:" + ThunderDamgeToCrop + ",ThunderDamgeToBuilding:" + ThunderDamgeToBuilding + ",SunLightIntensityAdderFactor:" + SunLightIntensityAdderFactor + ",WeatherTransitDuration:" + WeatherTransitDuration + ",LightIntensityDecayFactorPerLampInside:" + StringUtil.CollectionToString(LightIntensityDecayFactorPerLampInside) + ",LightIntensityDecayFactorPerLampOutside:" + StringUtil.CollectionToString(LightIntensityDecayFactorPerLampOutside) + ",LightIntensityDecayLampArea:" + LightIntensityDecayLampArea.ToString() + ",EnvLightIntensityAdderPerLamp:" + StringUtil.CollectionToString(EnvLightIntensityAdderPerLamp) + ",EnvLightDefaultColor:" + EnvLightDefaultColor.ToString() + ",WeatherRegulatorCdDuration:" + WeatherRegulatorCdDuration + ",WeatherRegulatorLaunchPower:" + WeatherRegulatorLaunchPower + ",MotorHorizontalAcceleration:" + MotorHorizontalAcceleration + ",MotorHorizontalRevertAcceleration:" + MotorHorizontalRevertAcceleration + ",MotorHorizontalDeceleration:" + MotorHorizontalDeceleration + ",MotorHorizontalMaxSpeed:" + MotorHorizontalMaxSpeed + ",MotorHorizontalReboundSpeedRate:" + MotorHorizontalReboundSpeedRate + ",MotorGravity:" + MotorGravity + ",MotorDropMaxSpeed:" + MotorDropMaxSpeed + ",MotorRaycastLength:" + MotorRaycastLength + ",MotorNaturalJumpAccRange:" + MotorNaturalJumpAccRange.ToString() + ",MotorNaturalJumpThreshold:" + MotorNaturalJumpThreshold + ",MotorNaturalJumpMaxSpeed:" + MotorNaturalJumpMaxSpeed + ",MotorVerticalAcceleration:" + MotorVerticalAcceleration + ",MotorVerticalMaxSpeed:" + MotorVerticalMaxSpeed + ",MotorEnduranceDuration:" + MotorEnduranceDuration + ",MotorEnduranceRecv:" + MotorEnduranceRecv + ",MotorCollisionVerticalSpeed:" + MotorCollisionVerticalSpeed + ",MotorVerticalReboundSpeedRate:" + MotorVerticalReboundSpeedRate + ",MotorCallSpeed:" + MotorCallSpeed + ",UiNodeMessageHoldDuration:" + UiNodeMessageHoldDuration + ",FadeDefaultDurationOnMonthChange:" + FadeDefaultDurationOnMonthChange + ",FadeDefaultDurationOnSleep:" + FadeDefaultDurationOnSleep + ",RebindActionInterruptDuration:" + RebindActionInterruptDuration + ",HideCursorDuration:" + HideCursorDuration + ",UiActionExclusiveBinds:" + StringUtil.CollectionToString(UiActionExclusiveBinds) + ",UiActionHorizontalMove:" + StringUtil.CollectionToString(UiActionHorizontalMove) + ",UiActionVerticalMove:" + StringUtil.CollectionToString(UiActionVerticalMove) + ",RebindActionSettingGroup:" + RebindActionSettingGroup + ",UiButtonLongClickDuration:" + UiButtonLongClickDuration + ",UiSceneTextTipDuration:" + UiSceneTextTipDuration + ",UiSceneInfoTipDuration:" + UiSceneInfoTipDuration + ",InputMemoMaxLength:" + InputMemoMaxLength + ",ItemSubscriptHasGene:" + ItemSubscriptHasGene?.ToString() + ",ItemSubscriptClone:" + ItemSubscriptClone?.ToString() + ",UiPlayerDefaultSprite:" + UiPlayerDefaultSprite?.ToString() + ",UiPlayerHairDefaultSprite:" + UiPlayerHairDefaultSprite?.ToString() + ",UiPlayerBodyDefaultSprite:" + UiPlayerBodyDefaultSprite?.ToString() + ",UiPlayerDefaultPortrait:" + UiPlayerDefaultPortrait?.ToString() + ",DefaultVehiclePreview:" + DefaultVehiclePreview?.ToString() + ",ChipItemName:" + ChipItemName + ",ChipSubmitLimit:" + ChipSubmitLimit + ",ChipAnalyzeHour:" + ChipAnalyzeHour + ",ChipAnalyzeSpeedBuff:" + ChipAnalyzeSpeedBuff + ",ChipProgressCheckDialogueNode:" + ChipProgressCheckDialogueNode + ",WaterItems:" + StringUtil.CollectionToString(WaterItems) + ",BetterWaterExtraBuff:" + BetterWaterExtraBuff?.ToString() + ",AnimalThunderProbability:" + AnimalThunderProbability + ",AnimalThunderMoodDecrease:" + AnimalThunderMoodDecrease + ",AnimalUnhappyThreshold:" + AnimalUnhappyThreshold + ",AnimalWeaknessThreshold:" + AnimalWeaknessThreshold + ",AnimalTechpointFondle:" + AnimalTechpointFondle + ",AnimalMoodUpdateInterval:" + AnimalMoodUpdateInterval + ",AnimalMoodContributionWeather:" + AnimalMoodContributionWeather + ",AnimalMoodContributionHungry:" + AnimalMoodContributionHungry + ",AnimalMoodContributionFull:" + AnimalMoodContributionFull + ",AnimalMoodContributionFullToilet:" + AnimalMoodContributionFullToilet + ",AnimalMoodContributionRangeEquipment:" + AnimalMoodContributionRangeEquipment.ToString() + ",AnimalEnergyCostPercentInNight:" + AnimalEnergyCostPercentInNight + ",AnimalMoodShowFlagThreshold:" + AnimalMoodShowFlagThreshold + ",AnimalChickennestEnergyRequire:" + AnimalChickennestEnergyRequire + ",AquaDefaultWeight:" + AquaDefaultWeight + ",AquaTechpointProduce:" + AquaTechpointProduce + ",ItemRefTimeBomb:" + ItemRefTimeBomb + ",ItemRefBottleOfWater:" + ItemRefBottleOfWater + ",ItemRefWastePlasticBottle:" + ItemRefWastePlasticBottle + ",ItemRefFaeces:" + ItemRefFaeces + ",ItemRefSturdySack:" + ItemRefSturdySack + ",ItemRefWeeds:" + ItemRefWeeds + ",ItemRefTicket:" + ItemRefTicket + ",ItemRefBox:" + ItemRefBox + ",ItemRefSeedEndyam:" + ItemRefSeedEndyam + ",ItemRefRoastedEndyam:" + ItemRefRoastedEndyam + ",ItemRefFishFry:" + ItemRefFishFry + ",ItemRefGeneCapsuleEmpty:" + ItemRefGeneCapsuleEmpty + ",ItemRefOldBattery:" + ItemRefOldBattery + ",ItemRefMilk:" + ItemRefMilk + ",}";
	}

	public Sprite GetBuildingDamageSprite(float process)
	{
		Sprite[] assets = BuildingDamageSpriteAssets.assets;
		if (assets.IsNullOrEmpty())
		{
			return null;
		}
		if (assets.Length == 1 || process <= 0f)
		{
			return assets[0];
		}
		if (process >= 1f)
		{
			return assets[^1];
		}
		int value = Mathf.FloorToInt((float)assets.Length * process) - 1;
		return assets[Mathf.Clamp(value, 0, assets.Length - 1)];
	}

	public int GameMinutes2Secs(float minutes)
	{
		return Mathf.RoundToInt(minutes * (float)TULength / (float)TU2Min);
	}

	public int GameHours2Secs(float hour)
	{
		return GameMinutes2Secs((float)Hour2Min * hour);
	}

	public int GameDays2Secs(float day)
	{
		return GameHours2Secs((float)Day2Hour * day);
	}

	public int GameMonths2Secs(float month)
	{
		return GameDays2Secs((float)Month2Day * month);
	}

	public int GameYears2Secs(float year)
	{
		return GameMonths2Secs((float)Year2Month * year);
	}

	public int Secs2GameHour(int seconds)
	{
		return (int)((float)(seconds * TU2Min) / (float)(Hour2Min * TULength));
	}

	public int Secs2Tu(int seconds)
	{
		return (int)((float)seconds / (float)TULength);
	}

	public int Tu2GameHour(int tu)
	{
		return (int)((float)(tu * TU2Min) / (float)Hour2Min);
	}

	public float Tu2GameDay(int tu)
	{
		return (float)(tu * TU2Min) / (float)(Hour2Min * Day2Hour);
	}
}
