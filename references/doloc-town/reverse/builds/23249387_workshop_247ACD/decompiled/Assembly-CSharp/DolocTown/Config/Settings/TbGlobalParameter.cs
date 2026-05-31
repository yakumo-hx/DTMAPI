using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.Config.Weather;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class TbGlobalParameter
{
	private readonly GlobalParameterInfo _data;

	public int InventoryLineCapacity => _data.InventoryLineCapacity;

	public string AchievementGraphName => _data.AchievementGraphName;

	public string[] ItemCollectionLabels => _data.ItemCollectionLabels;

	public string[] FoodItemSubTypes => _data.FoodItemSubTypes;

	public string ItemDefaultType => _data.ItemDefaultType;

	public int FishTankMaxCountOfSlots => _data.FishTankMaxCountOfSlots;

	public int LikingCeiling => _data.LikingCeiling;

	public float LikingRatioOnBirthday => _data.LikingRatioOnBirthday;

	public int LikingBirthdayParty => _data.LikingBirthdayParty;

	public int UpperFavorabilityLevel => _data.UpperFavorabilityLevel;

	public int WeeklyGiftLimit => _data.WeeklyGiftLimit;

	public int LikingFirstGreetingDay => _data.LikingFirstGreetingDay;

	public int LikingActivity => _data.LikingActivity;

	public Vector2Int NpcRelaxLightmanFishingInterval => _data.NpcRelaxLightmanFishingInterval;

	public float CombinationKeyHoldDuration => _data.CombinationKeyHoldDuration;

	public string QuickInventoryTimer => _data.QuickInventoryTimer;

	public string QuantitySelectTimer => _data.QuantitySelectTimer;

	public string ContinuouslyUseItemTimer => _data.ContinuouslyUseItemTimer;

	public string ContinuouslyInteractTimer => _data.ContinuouslyInteractTimer;

	public string TicketItemName => _data.TicketItemName;

	public int StationTicketMarkup => _data.StationTicketMarkup;

	public float GravelBlockLastDuration => _data.GravelBlockLastDuration;

	public float GravelBlockRefreshDuration => _data.GravelBlockRefreshDuration;

	public int MissionUpperLimit => _data.MissionUpperLimit;

	public int TakeTimeLimitFixed => _data.TakeTimeLimitFixed;

	public int TakeTimeLimitRandom => _data.TakeTimeLimitRandom;

	public int MissionRefreshDay => _data.MissionRefreshDay;

	public Vector3Int MissionRefreshWeights => _data.MissionRefreshWeights;

	public string DropOffBoxSotreId => _data.DropOffBoxSotreId;

	public string[] SeedStoreIds => _data.SeedStoreIds;

	public string[] ItemsCanNotBuyback => _data.ItemsCanNotBuyback;

	public Vector2Int InventoryAroundArea => _data.InventoryAroundArea;

	public Vector2Int InventoryAroundOffset => _data.InventoryAroundOffset;

	public int EquipmentBoundaryPadding => _data.EquipmentBoundaryPadding;

	public Vector2Int BuilderAroundArea => _data.BuilderAroundArea;

	public PrefabAsset UiGroupAsset => _data.UiGroupAsset;

	public string DefaultUiEntityInfo => _data.DefaultUiEntityInfo;

	public int NapHour => _data.NapHour;

	public int WakeUpClock => _data.WakeUpClock;

	public float RecoveredPlayerValuesPerHour => _data.RecoveredPlayerValuesPerHour;

	public float FullSleepBuffThreshold => _data.FullSleepBuffThreshold;

	public string FullSleepBuff => _data.FullSleepBuff;

	public float SitOptionDelay => _data.SitOptionDelay;

	public int FaintMinutesAfterDrowning => _data.FaintMinutesAfterDrowning;

	public int TreatyPortMaintainTime => _data.TreatyPortMaintainTime;

	public string[] TreatyPortGatesName => _data.TreatyPortGatesName;

	public Vector2Int FactionWorkingHours => _data.FactionWorkingHours;

	public int Money => _data.Money;

	public int MaxMoney => _data.MaxMoney;

	public WeatherType InitWeatherType => _data.InitWeatherType;

	public int InitTime => _data.InitTime;

	public string TrainingDungeonName => _data.TrainingDungeonName;

	public string PlayerDefaultName => _data.PlayerDefaultName;

	public int InputPlayerNameMaxLength => _data.InputPlayerNameMaxLength;

	public Vector2Int PlayerDefaultBirthday => _data.PlayerDefaultBirthday;

	public string InitHat => _data.InitHat;

	public int MaxSupportHeight => _data.MaxSupportHeight;

	public float MinFloorCoveredRatio => _data.MinFloorCoveredRatio;

	public float BuilderRaycastDistance => _data.BuilderRaycastDistance;

	public int BuilderPlatformHeightLimit => _data.BuilderPlatformHeightLimit;

	public Vector2Int BuilderPlatformWidth => _data.BuilderPlatformWidth;

	public float EquipmentShakeIntensity => _data.EquipmentShakeIntensity;

	public float PlatformShakeIntensity => _data.PlatformShakeIntensity;

	public int EquipmentSmallThreshold => _data.EquipmentSmallThreshold;

	public float DemolitionRecycleFactor => _data.DemolitionRecycleFactor;

	public string BuilderAnimationNode => _data.BuilderAnimationNode;

	public string BuilderMoveTimer => _data.BuilderMoveTimer;

	public int TemporaryBuildingCount => _data.TemporaryBuildingCount;

	public int TULength => _data.TULength;

	public int TU2Min => _data.TU2Min;

	public int Hour2Min => _data.Hour2Min;

	public int Day2Hour => _data.Day2Hour;

	public int Month2Day => _data.Month2Day;

	public int Year2Month => _data.Year2Month;

	public int EventRefreshClock => _data.EventRefreshClock;

	public int TotalDayOffset => _data.TotalDayOffset;

	public int WeatherUpdateInterval => _data.WeatherUpdateInterval;

	public int SleepTime => _data.SleepTime;

	public SwitchScheduleAsset DefaultSwitchScheduleAsseet => _data.DefaultSwitchScheduleAsseet;

	public SwitchScheduleAsset DroneSwitchScheduleAsset => _data.DroneSwitchScheduleAsset;

	public float SpeedUpTimeScale => _data.SpeedUpTimeScale;

	public float HoldOnIntervalInDialogueNormal => _data.HoldOnIntervalInDialogueNormal;

	public float HoldOnIntervalInDialogueSpeedUp => _data.HoldOnIntervalInDialogueSpeedUp;

	public string HomepageBgmEvent => _data.HomepageBgmEvent;

	public Vector2 BgmRandomIdleTime => _data.BgmRandomIdleTime;

	public int BgmPlayTimeoutDuration => _data.BgmPlayTimeoutDuration;

	public float SfxGroupInterval => _data.SfxGroupInterval;

	public int ReplayableDialogueNodeCount => _data.ReplayableDialogueNodeCount;

	public int InitHealth => _data.InitHealth;

	public int InitEnergy => _data.InitEnergy;

	public int InitSpirit => _data.InitSpirit;

	public int MaxOverflowHealth => _data.MaxOverflowHealth;

	public int MaxOverflowEnergy => _data.MaxOverflowEnergy;

	public float HealthPercentAfterFaint => _data.HealthPercentAfterFaint;

	public float EnergyPercentAfterFaint => _data.EnergyPercentAfterFaint;

	public float SpiritPercentAfterFaint => _data.SpiritPercentAfterFaint;

	public int CorrosionCounterLength => _data.CorrosionCounterLength;

	public int CorrosionHealthCost => _data.CorrosionHealthCost;

	public float HitBackDistance => _data.HitBackDistance;

	public int AgentDefend => _data.AgentDefend;

	public int ToolEnergyCost => _data.ToolEnergyCost;

	public float DashCdDuration => _data.DashCdDuration;

	public float DefendAdjust => _data.DefendAdjust;

	public float CriticalDamageRate => _data.CriticalDamageRate;

	public float SunGrowthAddition => _data.SunGrowthAddition;

	public string[] ClonedCropGeneGroup => _data.ClonedCropGeneGroup;

	public string[] CompressGeneFixed => _data.CompressGeneFixed;

	public Vector2[] CompressGeneCountDistPre => _data.CompressGeneCountDistPre;

	public Vector2[] CompressGeneCountDistPost => _data.CompressGeneCountDistPost;

	public Vector2[] SynthesisGeneCountDist => _data.SynthesisGeneCountDist;

	public SpriteAssetArray BuildingDamageSpriteAssets => _data.BuildingDamageSpriteAssets;

	public int BuildingLinkThresholdLeft => _data.BuildingLinkThresholdLeft;

	public int BuildingLinkThresholdRight => _data.BuildingLinkThresholdRight;

	public int BuildingLinkThresholdTop => _data.BuildingLinkThresholdTop;

	public int BuildingLinkThresholdBottom => _data.BuildingLinkThresholdBottom;

	public int MaxGeneCount => _data.MaxGeneCount;

	public string CellarBuildingId => _data.CellarBuildingId;

	public float CellarDistanceToFarmRight => _data.CellarDistanceToFarmRight;

	public float CellarLinkGateClipThreshold => _data.CellarLinkGateClipThreshold;

	public float HighLevelResourceMinProbabilityInDungeon => _data.HighLevelResourceMinProbabilityInDungeon;

	public int DungeonMaxHistoryThreshold => _data.DungeonMaxHistoryThreshold;

	public float CastDuration => _data.CastDuration;

	public float PerfectCastDuration => _data.PerfectCastDuration;

	public float CastCancelThreshold => _data.CastCancelThreshold;

	public float FishingRollInterval => _data.FishingRollInterval;

	public float FishingBiteInitProbability => _data.FishingBiteInitProbability;

	public float FishingBiteAdditionalProbability => _data.FishingBiteAdditionalProbability;

	public float PullTiming => _data.PullTiming;

	public int FishingEnergyCost => _data.FishingEnergyCost;

	public float FishingOperationInterval => _data.FishingOperationInterval;

	public int FishingNoteScrollSpeedBase => _data.FishingNoteScrollSpeedBase;

	public int FishingNoteScrollSpeedBonus => _data.FishingNoteScrollSpeedBonus;

	public int FishingNoteBarWidthPixel => _data.FishingNoteBarWidthPixel;

	public int FishingProgressBarWidthPixel => _data.FishingProgressBarWidthPixel;

	public float FishingAnimationMinimumInterval => _data.FishingAnimationMinimumInterval;

	public float FishingPunishAnimationDuration => _data.FishingPunishAnimationDuration;

	public float FishingPunishStartTime => _data.FishingPunishStartTime;

	public float FishingGamePreparationTimeScale => _data.FishingGamePreparationTimeScale;

	public float FishingNoteSpeedDecreasePerLevel => _data.FishingNoteSpeedDecreasePerLevel;

	public float AcidRainDamage => _data.AcidRainDamage;

	public float ScorchSunDamage => _data.ScorchSunDamage;

	public Vector2Int ThunderFrequency => _data.ThunderFrequency;

	public int ThunderPower => _data.ThunderPower;

	public float ThunderDamgeToCrop => _data.ThunderDamgeToCrop;

	public float ThunderDamgeToBuilding => _data.ThunderDamgeToBuilding;

	public float SunLightIntensityAdderFactor => _data.SunLightIntensityAdderFactor;

	public float WeatherTransitDuration => _data.WeatherTransitDuration;

	public float[] LightIntensityDecayFactorPerLampInside => _data.LightIntensityDecayFactorPerLampInside;

	public float[] LightIntensityDecayFactorPerLampOutside => _data.LightIntensityDecayFactorPerLampOutside;

	public Vector2Int LightIntensityDecayLampArea => _data.LightIntensityDecayLampArea;

	public float[] EnvLightIntensityAdderPerLamp => _data.EnvLightIntensityAdderPerLamp;

	public Color EnvLightDefaultColor => _data.EnvLightDefaultColor;

	public int WeatherRegulatorCdDuration => _data.WeatherRegulatorCdDuration;

	public float WeatherRegulatorLaunchPower => _data.WeatherRegulatorLaunchPower;

	public float MotorHorizontalAcceleration => _data.MotorHorizontalAcceleration;

	public float MotorHorizontalRevertAcceleration => _data.MotorHorizontalRevertAcceleration;

	public float MotorHorizontalDeceleration => _data.MotorHorizontalDeceleration;

	public float MotorHorizontalMaxSpeed => _data.MotorHorizontalMaxSpeed;

	public float MotorHorizontalReboundSpeedRate => _data.MotorHorizontalReboundSpeedRate;

	public float MotorGravity => _data.MotorGravity;

	public float MotorDropMaxSpeed => _data.MotorDropMaxSpeed;

	public float MotorRaycastLength => _data.MotorRaycastLength;

	public Vector2 MotorNaturalJumpAccRange => _data.MotorNaturalJumpAccRange;

	public float MotorNaturalJumpThreshold => _data.MotorNaturalJumpThreshold;

	public float MotorNaturalJumpMaxSpeed => _data.MotorNaturalJumpMaxSpeed;

	public float MotorVerticalAcceleration => _data.MotorVerticalAcceleration;

	public float MotorVerticalMaxSpeed => _data.MotorVerticalMaxSpeed;

	public float MotorEnduranceDuration => _data.MotorEnduranceDuration;

	public float MotorEnduranceRecv => _data.MotorEnduranceRecv;

	public float MotorCollisionVerticalSpeed => _data.MotorCollisionVerticalSpeed;

	public float MotorVerticalReboundSpeedRate => _data.MotorVerticalReboundSpeedRate;

	public float MotorCallSpeed => _data.MotorCallSpeed;

	public float UiNodeMessageHoldDuration => _data.UiNodeMessageHoldDuration;

	public float FadeDefaultDurationOnMonthChange => _data.FadeDefaultDurationOnMonthChange;

	public float FadeDefaultDurationOnSleep => _data.FadeDefaultDurationOnSleep;

	public float RebindActionInterruptDuration => _data.RebindActionInterruptDuration;

	public float HideCursorDuration => _data.HideCursorDuration;

	public string[] UiActionExclusiveBinds => _data.UiActionExclusiveBinds;

	public string[] UiActionHorizontalMove => _data.UiActionHorizontalMove;

	public string[] UiActionVerticalMove => _data.UiActionVerticalMove;

	public string RebindActionSettingGroup => _data.RebindActionSettingGroup;

	public float UiButtonLongClickDuration => _data.UiButtonLongClickDuration;

	public float UiSceneTextTipDuration => _data.UiSceneTextTipDuration;

	public float UiSceneInfoTipDuration => _data.UiSceneInfoTipDuration;

	public int InputMemoMaxLength => _data.InputMemoMaxLength;

	public SpriteAssetArray ItemSubscriptHasGene => _data.ItemSubscriptHasGene;

	public SpriteAssetArray ItemSubscriptClone => _data.ItemSubscriptClone;

	public SpriteAsset UiPlayerDefaultSprite => _data.UiPlayerDefaultSprite;

	public SpriteAsset UiPlayerHairDefaultSprite => _data.UiPlayerHairDefaultSprite;

	public SpriteAsset UiPlayerBodyDefaultSprite => _data.UiPlayerBodyDefaultSprite;

	public SpriteAsset UiPlayerDefaultPortrait => _data.UiPlayerDefaultPortrait;

	public SpriteAsset DefaultVehiclePreview => _data.DefaultVehiclePreview;

	public string ChipItemName => _data.ChipItemName;

	public int ChipSubmitLimit => _data.ChipSubmitLimit;

	public int ChipAnalyzeHour => _data.ChipAnalyzeHour;

	public float ChipAnalyzeSpeedBuff => _data.ChipAnalyzeSpeedBuff;

	public string ChipProgressCheckDialogueNode => _data.ChipProgressCheckDialogueNode;

	public string[] WaterItems => _data.WaterItems;

	public FoodEffect BetterWaterExtraBuff => _data.BetterWaterExtraBuff;

	public float AnimalThunderProbability => _data.AnimalThunderProbability;

	public int AnimalThunderMoodDecrease => _data.AnimalThunderMoodDecrease;

	public int AnimalUnhappyThreshold => _data.AnimalUnhappyThreshold;

	public int AnimalWeaknessThreshold => _data.AnimalWeaknessThreshold;

	public int AnimalTechpointFondle => _data.AnimalTechpointFondle;

	public int AnimalMoodUpdateInterval => _data.AnimalMoodUpdateInterval;

	public int AnimalMoodContributionWeather => _data.AnimalMoodContributionWeather;

	public int AnimalMoodContributionHungry => _data.AnimalMoodContributionHungry;

	public int AnimalMoodContributionFull => _data.AnimalMoodContributionFull;

	public int AnimalMoodContributionFullToilet => _data.AnimalMoodContributionFullToilet;

	public Vector2Int AnimalMoodContributionRangeEquipment => _data.AnimalMoodContributionRangeEquipment;

	public float AnimalEnergyCostPercentInNight => _data.AnimalEnergyCostPercentInNight;

	public int AnimalMoodShowFlagThreshold => _data.AnimalMoodShowFlagThreshold;

	public int AnimalChickennestEnergyRequire => _data.AnimalChickennestEnergyRequire;

	public int AquaDefaultWeight => _data.AquaDefaultWeight;

	public int AquaTechpointProduce => _data.AquaTechpointProduce;

	public string ItemRefTimeBomb => _data.ItemRefTimeBomb;

	public string ItemRefBottleOfWater => _data.ItemRefBottleOfWater;

	public string ItemRefWastePlasticBottle => _data.ItemRefWastePlasticBottle;

	public string ItemRefFaeces => _data.ItemRefFaeces;

	public string ItemRefSturdySack => _data.ItemRefSturdySack;

	public string ItemRefWeeds => _data.ItemRefWeeds;

	public string ItemRefTicket => _data.ItemRefTicket;

	public string ItemRefBox => _data.ItemRefBox;

	public string ItemRefSeedEndyam => _data.ItemRefSeedEndyam;

	public string ItemRefRoastedEndyam => _data.ItemRefRoastedEndyam;

	public string ItemRefFishFry => _data.ItemRefFishFry;

	public string ItemRefGeneCapsuleEmpty => _data.ItemRefGeneCapsuleEmpty;

	public string ItemRefOldBattery => _data.ItemRefOldBattery;

	public string ItemRefMilk => _data.ItemRefMilk;

	public GlobalParameterInfo Data => _data;

	public TbGlobalParameter(JSONNode _json)
	{
		if (!_json.IsArray)
		{
			throw new SerializationException();
		}
		if (_json.Count != 1)
		{
			throw new SerializationException("table mode=one, but size != 1");
		}
		_data = GlobalParameterInfo.DeserializeGlobalParameterInfo(_json[0]);
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		_data.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		_data.TranslateText(translator);
	}
}
