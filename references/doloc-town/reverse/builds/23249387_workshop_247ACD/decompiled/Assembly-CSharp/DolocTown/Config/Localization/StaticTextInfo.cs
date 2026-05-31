using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class StaticTextInfo : BeanBase
{
	public const int __ID__ = -1993636866;

	public string SysErrDatalost { get; private set; }

	public string SysErrDatalost_l10n_key { get; }

	public string UiMoneyTip { get; private set; }

	public string UiMoneyTip_l10n_key { get; }

	public string UiItemSimpleTip { get; private set; }

	public string UiItemSimpleTip_l10n_key { get; }

	public string UiItemTip { get; private set; }

	public string UiItemTip_l10n_key { get; }

	public string UiTipConsumable { get; private set; }

	public string UiTipConsumable_l10n_key { get; }

	public string UiErrMaterialNotEnough { get; private set; }

	public string UiErrMaterialNotEnough_l10n_key { get; }

	public string UiErrMoneyNotEnough { get; private set; }

	public string UiErrMoneyNotEnough_l10n_key { get; }

	public string UiErrBackpackIsFull { get; private set; }

	public string UiErrBackpackIsFull_l10n_key { get; }

	public string UiErrNotDisposeItem { get; private set; }

	public string UiErrNotDisposeItem_l10n_key { get; }

	public string UiQuesDisposeItem { get; private set; }

	public string UiQuesDisposeItem_l10n_key { get; }

	public string UiMenuItemnotavaiable { get; private set; }

	public string UiMenuItemnotavaiable_l10n_key { get; }

	public string UiBackpackCellExchange { get; private set; }

	public string UiBackpackCellExchange_l10n_key { get; }

	public string UiErrBackpackCellExchange { get; private set; }

	public string UiErrBackpackCellExchange_l10n_key { get; }

	public string UiSortDefault { get; private set; }

	public string UiSortDefault_l10n_key { get; }

	public string UiSortUnlock { get; private set; }

	public string UiSortUnlock_l10n_key { get; }

	public string UiLoseItemTip { get; private set; }

	public string UiLoseItemTip_l10n_key { get; }

	public string UiLoseMoneyTip { get; private set; }

	public string UiLoseMoneyTip_l10n_key { get; }

	public string UiUnlockSeed { get; private set; }

	public string UiUnlockSeed_l10n_key { get; }

	public string UiAnalyzerTime { get; private set; }

	public string UiAnalyzerTime_l10n_key { get; }

	public string UiAnalyzerOutput { get; private set; }

	public string UiAnalyzerOutput_l10n_key { get; }

	public string UiAnalyzerTotalTime { get; private set; }

	public string UiAnalyzerTotalTime_l10n_key { get; }

	public string UiShredderConfirm { get; private set; }

	public string UiShredderConfirm_l10n_key { get; }

	public string UiShredderConfirmUpgrade { get; private set; }

	public string UiShredderConfirmUpgrade_l10n_key { get; }

	public string UiSubmitConfirm { get; private set; }

	public string UiSubmitConfirm_l10n_key { get; }

	public string UiSubmitConfirmGene { get; private set; }

	public string UiSubmitConfirmGene_l10n_key { get; }

	public string UiSubmitConfirmMoney { get; private set; }

	public string UiSubmitConfirmMoney_l10n_key { get; }

	public string UiSubmitNotEnough { get; private set; }

	public string UiSubmitNotEnough_l10n_key { get; }

	public string UiGiftItemConfirm { get; private set; }

	public string UiGiftItemConfirm_l10n_key { get; }

	public string UiTipDismentle { get; private set; }

	public string UiTipDismentle_l10n_key { get; }

	public string UiTipPlantErr { get; private set; }

	public string UiTipPlantErr_l10n_key { get; }

	public string UiTextYears { get; private set; }

	public string UiTextYears_l10n_key { get; }

	public string UiTextMonths { get; private set; }

	public string UiTextMonths_l10n_key { get; }

	public string UiTextDays { get; private set; }

	public string UiTextDays_l10n_key { get; }

	public string UiTextHours { get; private set; }

	public string UiTextHours_l10n_key { get; }

	public string UiTextMinutes { get; private set; }

	public string UiTextMinutes_l10n_key { get; }

	public string UiTextTimeFormat { get; private set; }

	public string UiTextTimeFormat_l10n_key { get; }

	public string UiTextDemoStatement { get; private set; }

	public string UiTextDemoStatement_l10n_key { get; }

	public string UiTextGeneDescription { get; private set; }

	public string UiTextGeneDescription_l10n_key { get; }

	public string InputTitlePlayerName { get; private set; }

	public string InputTitlePlayerName_l10n_key { get; }

	public string InputTitleContainerName { get; private set; }

	public string InputTitleContainerName_l10n_key { get; }

	public string InputNameEmpty { get; private set; }

	public string InputNameEmpty_l10n_key { get; }

	public string InputNameConfirm { get; private set; }

	public string InputNameConfirm_l10n_key { get; }

	public string InputNameContainsSensitiveWorld { get; private set; }

	public string InputNameContainsSensitiveWorld_l10n_key { get; }

	public string InputTitlePlayerBirthday { get; private set; }

	public string InputTitlePlayerBirthday_l10n_key { get; }

	public string InputBirthdayConfirm { get; private set; }

	public string InputBirthdayConfirm_l10n_key { get; }

	public string UiSavepointDefault { get; private set; }

	public string UiSavepointDefault_l10n_key { get; }

	public string UiSavepointSleep { get; private set; }

	public string UiSavepointSleep_l10n_key { get; }

	public string UiSavepointNap { get; private set; }

	public string UiSavepointNap_l10n_key { get; }

	public string UiSavepointKillTime { get; private set; }

	public string UiSavepointKillTime_l10n_key { get; }

	public string UiSavepointSit { get; private set; }

	public string UiSavepointSit_l10n_key { get; }

	public string UiSavepointExit { get; private set; }

	public string UiSavepointExit_l10n_key { get; }

	public string UiSavepointCancel { get; private set; }

	public string UiSavepointCancel_l10n_key { get; }

	public string UiTipConfirm { get; private set; }

	public string UiTipConfirm_l10n_key { get; }

	public string UiTipCancel { get; private set; }

	public string UiTipCancel_l10n_key { get; }

	public string UiTipQuit { get; private set; }

	public string UiTipQuit_l10n_key { get; }

	public string UiTipSwitchClassifying { get; private set; }

	public string UiTipSwitchClassifying_l10n_key { get; }

	public string UiTipSort { get; private set; }

	public string UiTipSort_l10n_key { get; }

	public string UiTipUpgrade { get; private set; }

	public string UiTipUpgrade_l10n_key { get; }

	public string UiTipBuy { get; private set; }

	public string UiTipBuy_l10n_key { get; }

	public string UiTipSellConfirm { get; private set; }

	public string UiTipSellConfirm_l10n_key { get; }

	public string UiTipSell { get; private set; }

	public string UiTipSell_l10n_key { get; }

	public string UiTipBuyConfirm { get; private set; }

	public string UiTipBuyConfirm_l10n_key { get; }

	public string UiTipUnlock { get; private set; }

	public string UiTipUnlock_l10n_key { get; }

	public string UiTipUnlockSeed { get; private set; }

	public string UiTipUnlockSeed_l10n_key { get; }

	public string UiTipLaunchDrone { get; private set; }

	public string UiTipLaunchDrone_l10n_key { get; }

	public string UiTipSwitchAutomateBot { get; private set; }

	public string UiTipSwitchAutomateBot_l10n_key { get; }

	public string UiTipAutomateBotSwitch { get; private set; }

	public string UiTipAutomateBotSwitch_l10n_key { get; }

	public string UiTipAutomateBotUnload { get; private set; }

	public string UiTipAutomateBotUnload_l10n_key { get; }

	public string UiTipAutomateBotLoad { get; private set; }

	public string UiTipAutomateBotLoad_l10n_key { get; }

	public string UiTipChoice { get; private set; }

	public string UiTipChoice_l10n_key { get; }

	public string UiTipSubmitAll { get; private set; }

	public string UiTipSubmitAll_l10n_key { get; }

	public string UiTipSubmit { get; private set; }

	public string UiTipSubmit_l10n_key { get; }

	public string UiTipLoadGame { get; private set; }

	public string UiTipLoadGame_l10n_key { get; }

	public string UiTipDel { get; private set; }

	public string UiTipDel_l10n_key { get; }

	public string UiTipLockSlot { get; private set; }

	public string UiTipLockSlot_l10n_key { get; }

	public string UiTipTidy { get; private set; }

	public string UiTipTidy_l10n_key { get; }

	public string UiTipDispose { get; private set; }

	public string UiTipDispose_l10n_key { get; }

	public string UiTipDestroyItem { get; private set; }

	public string UiTipDestroyItem_l10n_key { get; }

	public string UiTipSwitchLable { get; private set; }

	public string UiTipSwitchLable_l10n_key { get; }

	public string UiTipPutAll { get; private set; }

	public string UiTipPutAll_l10n_key { get; }

	public string UiTipPutMax { get; private set; }

	public string UiTipPutMax_l10n_key { get; }

	public string UiTipTakeOutAll { get; private set; }

	public string UiTipTakeOutAll_l10n_key { get; }

	public string UiTipTakeOutMax { get; private set; }

	public string UiTipTakeOutMax_l10n_key { get; }

	public string UiTipPutAllTap { get; private set; }

	public string UiTipPutAllTap_l10n_key { get; }

	public string UiTipTakeOutOne { get; private set; }

	public string UiTipTakeOutOne_l10n_key { get; }

	public string UiTipTakeOutHalf { get; private set; }

	public string UiTipTakeOutHalf_l10n_key { get; }

	public string UiTipTakeOutOneGamepad { get; private set; }

	public string UiTipTakeOutOneGamepad_l10n_key { get; }

	public string UiTipPut { get; private set; }

	public string UiTipPut_l10n_key { get; }

	public string UiTipTakeOut { get; private set; }

	public string UiTipTakeOut_l10n_key { get; }

	public string UiTipPutGamepad { get; private set; }

	public string UiTipPutGamepad_l10n_key { get; }

	public string UiTipTakeOutGamepad { get; private set; }

	public string UiTipTakeOutGamepad_l10n_key { get; }

	public string UiTipPickUpGamepad { get; private set; }

	public string UiTipPickUpGamepad_l10n_key { get; }

	public string UiTipQuickPutOne { get; private set; }

	public string UiTipQuickPutOne_l10n_key { get; }

	public string UiTipQuickPutAll { get; private set; }

	public string UiTipQuickPutAll_l10n_key { get; }

	public string UiTipQuickTakeOne { get; private set; }

	public string UiTipQuickTakeOne_l10n_key { get; }

	public string UiTipQuickTakeAll { get; private set; }

	public string UiTipQuickTakeAll_l10n_key { get; }

	public string UiTipQuickEquipment { get; private set; }

	public string UiTipQuickEquipment_l10n_key { get; }

	public string UiTipSwitchBox { get; private set; }

	public string UiTipSwitchBox_l10n_key { get; }

	public string UiTipSubOne { get; private set; }

	public string UiTipSubOne_l10n_key { get; }

	public string UiTipAddOne { get; private set; }

	public string UiTipAddOne_l10n_key { get; }

	public string UiTipSubTen { get; private set; }

	public string UiTipSubTen_l10n_key { get; }

	public string UiTipAddTen { get; private set; }

	public string UiTipAddTen_l10n_key { get; }

	public string UiTipSetMin { get; private set; }

	public string UiTipSetMin_l10n_key { get; }

	public string UiTipSetMax { get; private set; }

	public string UiTipSetMax_l10n_key { get; }

	public string UiTipBroadcast { get; private set; }

	public string UiTipBroadcast_l10n_key { get; }

	public string UiTipMakeOne { get; private set; }

	public string UiTipMakeOne_l10n_key { get; }

	public string UiTipMakeAll { get; private set; }

	public string UiTipMakeAll_l10n_key { get; }

	public string UiTipBuyOne { get; private set; }

	public string UiTipBuyOne_l10n_key { get; }

	public string UiTipBuyAllGamepad { get; private set; }

	public string UiTipBuyAllGamepad_l10n_key { get; }

	public string UiTipSellOne { get; private set; }

	public string UiTipSellOne_l10n_key { get; }

	public string UiTipSellAll { get; private set; }

	public string UiTipSellAll_l10n_key { get; }

	public string UiTipPrevFilter { get; private set; }

	public string UiTipPrevFilter_l10n_key { get; }

	public string UiTipNextFilter { get; private set; }

	public string UiTipNextFilter_l10n_key { get; }

	public string UiTipCapture { get; private set; }

	public string UiTipCapture_l10n_key { get; }

	public string UiTipCollection { get; private set; }

	public string UiTipCollection_l10n_key { get; }

	public string UiTipHistory { get; private set; }

	public string UiTipHistory_l10n_key { get; }

	public string UiTipChangeName { get; private set; }

	public string UiTipChangeName_l10n_key { get; }

	public string UiTipTakeOutSelected { get; private set; }

	public string UiTipTakeOutSelected_l10n_key { get; }

	public string UiTipPutOneSelected { get; private set; }

	public string UiTipPutOneSelected_l10n_key { get; }

	public string UiTipPutAllSelected { get; private set; }

	public string UiTipPutAllSelected_l10n_key { get; }

	public string UiTipAddMemo { get; private set; }

	public string UiTipAddMemo_l10n_key { get; }

	public string UiTipChangeMemo { get; private set; }

	public string UiTipChangeMemo_l10n_key { get; }

	public string UiTipDelMemo { get; private set; }

	public string UiTipDelMemo_l10n_key { get; }

	public string UiTipSwitchMapSize { get; private set; }

	public string UiTipSwitchMapSize_l10n_key { get; }

	public string UiTipMapCentered { get; private set; }

	public string UiTipMapCentered_l10n_key { get; }

	public string UiTipEmailRecycle { get; private set; }

	public string UiTipEmailRecycle_l10n_key { get; }

	public string UiTipEmailDefault { get; private set; }

	public string UiTipEmailDefault_l10n_key { get; }

	public string UiTipTechtreePointFocus { get; private set; }

	public string UiTipTechtreePointFocus_l10n_key { get; }

	public string UiTipTechtreeFocus { get; private set; }

	public string UiTipTechtreeFocus_l10n_key { get; }

	public string UiTipBatteryLow { get; private set; }

	public string UiTipBatteryLow_l10n_key { get; }

	public string UiTipErrTakeBoat { get; private set; }

	public string UiTipErrTakeBoat_l10n_key { get; }

	public string UiOperationTalk { get; private set; }

	public string UiOperationTalk_l10n_key { get; }

	public string UiOperationTalkUnknown { get; private set; }

	public string UiOperationTalkUnknown_l10n_key { get; }

	public string UiOperationInteract { get; private set; }

	public string UiOperationInteract_l10n_key { get; }

	public string UiOperationSleep { get; private set; }

	public string UiOperationSleep_l10n_key { get; }

	public string UiOperationPick { get; private set; }

	public string UiOperationPick_l10n_key { get; }

	public string UiOperationWell { get; private set; }

	public string UiOperationWell_l10n_key { get; }

	public string UiOperationHarvest { get; private set; }

	public string UiOperationHarvest_l10n_key { get; }

	public string UiOperationClear { get; private set; }

	public string UiOperationClear_l10n_key { get; }

	public string UiOperationOpen { get; private set; }

	public string UiOperationOpen_l10n_key { get; }

	public string UiOperationOpenDoor { get; private set; }

	public string UiOperationOpenDoor_l10n_key { get; }

	public string UiOperationCloseDoor { get; private set; }

	public string UiOperationCloseDoor_l10n_key { get; }

	public string UiOperationEnter { get; private set; }

	public string UiOperationEnter_l10n_key { get; }

	public string UiOperationEnterFormat { get; private set; }

	public string UiOperationEnterFormat_l10n_key { get; }

	public string UiOperationExit { get; private set; }

	public string UiOperationExit_l10n_key { get; }

	public string UiOperationView { get; private set; }

	public string UiOperationView_l10n_key { get; }

	public string UiOperationUse { get; private set; }

	public string UiOperationUse_l10n_key { get; }

	public string UiOperationSit { get; private set; }

	public string UiOperationSit_l10n_key { get; }

	public string UiOperationDisplay { get; private set; }

	public string UiOperationDisplay_l10n_key { get; }

	public string UiOperationTakeoff { get; private set; }

	public string UiOperationTakeoff_l10n_key { get; }

	public string UiOperationDump { get; private set; }

	public string UiOperationDump_l10n_key { get; }

	public string UiOperationDisembark { get; private set; }

	public string UiOperationDisembark_l10n_key { get; }

	public string UiOperationCallBoat { get; private set; }

	public string UiOperationCallBoat_l10n_key { get; }

	public string UiOperationShower { get; private set; }

	public string UiOperationShower_l10n_key { get; }

	public string UiOperationOpenBox { get; private set; }

	public string UiOperationOpenBox_l10n_key { get; }

	public string UiOperationStorageShelf { get; private set; }

	public string UiOperationStorageShelf_l10n_key { get; }

	public string UiOperationFuelIn { get; private set; }

	public string UiOperationFuelIn_l10n_key { get; }

	public string UiOperationClose { get; private set; }

	public string UiOperationClose_l10n_key { get; }

	public string UiOperationStart { get; private set; }

	public string UiOperationStart_l10n_key { get; }

	public string UiOperationFill { get; private set; }

	public string UiOperationFill_l10n_key { get; }

	public string UiOperationFondle { get; private set; }

	public string UiOperationFondle_l10n_key { get; }

	public string UiOperationStartSomething { get; private set; }

	public string UiOperationStartSomething_l10n_key { get; }

	public string UiOperationUseSomething { get; private set; }

	public string UiOperationUseSomething_l10n_key { get; }

	public string UiOperationErrNotSeed { get; private set; }

	public string UiOperationErrNotSeed_l10n_key { get; }

	public string UiOperationErrInvalidPlantbasin { get; private set; }

	public string UiOperationErrInvalidPlantbasin_l10n_key { get; }

	public string UiOperationErrInvalidSeason { get; private set; }

	public string UiOperationErrInvalidSeason_l10n_key { get; }

	public string UiOperationErrLackOfAsset { get; private set; }

	public string UiOperationErrLackOfAsset_l10n_key { get; }

	public string UiOperationErrLackOfEnergy { get; private set; }

	public string UiOperationErrLackOfEnergy_l10n_key { get; }

	public string UiOperationErrCannotPlantTree { get; private set; }

	public string UiOperationErrCannotPlantTree_l10n_key { get; }

	public string UiOperationErrCannotPlantOnGround { get; private set; }

	public string UiOperationErrCannotPlantOnGround_l10n_key { get; }

	public string UiOperationErrEmptyDrone { get; private set; }

	public string UiOperationErrEmptyDrone_l10n_key { get; }

	public string UiOperationErrWellFull { get; private set; }

	public string UiOperationErrWellFull_l10n_key { get; }

	public string UiOperationErrWaterFull { get; private set; }

	public string UiOperationErrWaterFull_l10n_key { get; }

	public string UiOperationErrRunoutWater { get; private set; }

	public string UiOperationErrRunoutWater_l10n_key { get; }

	public string UiOperationErrRunoutWaterAround { get; private set; }

	public string UiOperationErrRunoutWaterAround_l10n_key { get; }

	public string UiOperationErrEquipmentCorroded { get; private set; }

	public string UiOperationErrEquipmentCorroded_l10n_key { get; }

	public string UiOperationErrLowToolLevel { get; private set; }

	public string UiOperationErrLowToolLevel_l10n_key { get; }

	public string UiOperationErrFullPlasticFilm { get; private set; }

	public string UiOperationErrFullPlasticFilm_l10n_key { get; }

	public string UiOperationErrCannotFertilizer { get; private set; }

	public string UiOperationErrCannotFertilizer_l10n_key { get; }

	public string UiOperationErrDroneFullBattery { get; private set; }

	public string UiOperationErrDroneFullBattery_l10n_key { get; }

	public string UiOperationErrWaterEvaporated { get; private set; }

	public string UiOperationErrWaterEvaporated_l10n_key { get; }

	public string UiOperationErrFailToPlaceBox { get; private set; }

	public string UiOperationErrFailToPlaceBox_l10n_key { get; }

	public string UiOperationErrCannotDisplay { get; private set; }

	public string UiOperationErrCannotDisplay_l10n_key { get; }

	public string UiOperationErrCannotUseIfRiding { get; private set; }

	public string UiOperationErrCannotUseIfRiding_l10n_key { get; }

	public string UiOperationErrCannotCallMotor { get; private set; }

	public string UiOperationErrCannotCallMotor_l10n_key { get; }

	public string UiOperationErrSwordCannotAutofire { get; private set; }

	public string UiOperationErrSwordCannotAutofire_l10n_key { get; }

	public string UiOperationErrLowPower { get; private set; }

	public string UiOperationErrLowPower_l10n_key { get; }

	public string UiOperationErrCooling { get; private set; }

	public string UiOperationErrCooling_l10n_key { get; }

	public string UiOperationErrBarrelFull { get; private set; }

	public string UiOperationErrBarrelFull_l10n_key { get; }

	public string UiOperationErrFuelFull { get; private set; }

	public string UiOperationErrFuelFull_l10n_key { get; }

	public string UiOperationErrNotFeeds { get; private set; }

	public string UiOperationErrNotFeeds_l10n_key { get; }

	public string UiOperationErrFeederFull { get; private set; }

	public string UiOperationErrFeederFull_l10n_key { get; }

	public string UiOperationErrSlotFull { get; private set; }

	public string UiOperationErrSlotFull_l10n_key { get; }

	public string UiOperationErrNoSuitableEquipments { get; private set; }

	public string UiOperationErrNoSuitableEquipments_l10n_key { get; }

	public string UiStorageShelfFull { get; private set; }

	public string UiStorageShelfFull_l10n_key { get; }

	public string UiOperationWeatherHasChanged { get; private set; }

	public string UiOperationWeatherHasChanged_l10n_key { get; }

	public string UiOperationErrFuel { get; private set; }

	public string UiOperationErrFuel_l10n_key { get; }

	public string UiOperationGiftItemFail { get; private set; }

	public string UiOperationGiftItemFail_l10n_key { get; }

	public string UiOperationMakeEquipment { get; private set; }

	public string UiOperationMakeEquipment_l10n_key { get; }

	public string UiOperationSell { get; private set; }

	public string UiOperationSell_l10n_key { get; }

	public string UiOperationSwitchAutoFire { get; private set; }

	public string UiOperationSwitchAutoFire_l10n_key { get; }

	public string UiOperationSwitchManualFire { get; private set; }

	public string UiOperationSwitchManualFire_l10n_key { get; }

	public string UiOperationCollect { get; private set; }

	public string UiOperationCollect_l10n_key { get; }

	public string UiOperationRide { get; private set; }

	public string UiOperationRide_l10n_key { get; }

	public string UiOperationNoAnimalBuilding { get; private set; }

	public string UiOperationNoAnimalBuilding_l10n_key { get; }

	public string UiOperationErrNoPlantBasin { get; private set; }

	public string UiOperationErrNoPlantBasin_l10n_key { get; }

	public string UiOperationErrNoBuildingHere { get; private set; }

	public string UiOperationErrNoBuildingHere_l10n_key { get; }

	public string UiOperationErrInvalidWallpaper { get; private set; }

	public string UiOperationErrInvalidWallpaper_l10n_key { get; }

	public string UiOperationErrSameWallpaper { get; private set; }

	public string UiOperationErrSameWallpaper_l10n_key { get; }

	public string UiOperationErrShouldInhouse { get; private set; }

	public string UiOperationErrShouldInhouse_l10n_key { get; }

	public string UiOperationErrShouldOutside { get; private set; }

	public string UiOperationErrShouldOutside_l10n_key { get; }

	public string StorePlayerMoneyNotEnough { get; private set; }

	public string StorePlayerMoneyNotEnough_l10n_key { get; }

	public string StoreStoreMoneyNotEnough { get; private set; }

	public string StoreStoreMoneyNotEnough_l10n_key { get; }

	public string StoreItemNotSaleable { get; private set; }

	public string StoreItemNotSaleable_l10n_key { get; }

	public string StoreItemSoldOut { get; private set; }

	public string StoreItemSoldOut_l10n_key { get; }

	public string StoreLackOfAsset { get; private set; }

	public string StoreLackOfAsset_l10n_key { get; }

	public string StoreUpgradePrice { get; private set; }

	public string StoreUpgradePrice_l10n_key { get; }

	public string StoreUpgradeConfirm { get; private set; }

	public string StoreUpgradeConfirm_l10n_key { get; }

	public string StoreSoldOutIcon { get; private set; }

	public string StoreSoldOutIcon_l10n_key { get; }

	public string StoreUpgradeEmptyInfo { get; private set; }

	public string StoreUpgradeEmptyInfo_l10n_key { get; }

	public string StoreItemNotSaleableComment { get; private set; }

	public string StoreItemNotSaleableComment_l10n_key { get; }

	public string StoreQuantitySubmitSelling { get; private set; }

	public string StoreQuantitySubmitSelling_l10n_key { get; }

	public string StoreQuantitySubmitCurrentMoneySelling { get; private set; }

	public string StoreQuantitySubmitCurrentMoneySelling_l10n_key { get; }

	public string StoreQuantitySubmitTotalMoneySelling { get; private set; }

	public string StoreQuantitySubmitTotalMoneySelling_l10n_key { get; }

	public string StoreQuantitySubmitBuying { get; private set; }

	public string StoreQuantitySubmitBuying_l10n_key { get; }

	public string StoreQuantitySubmitCurrentMoneyBuying { get; private set; }

	public string StoreQuantitySubmitCurrentMoneyBuying_l10n_key { get; }

	public string StoreQuantitySubmitTotalMoneyBuying { get; private set; }

	public string StoreQuantitySubmitTotalMoneyBuying_l10n_key { get; }

	public string StoreQuantitySubmitCountInBack { get; private set; }

	public string StoreQuantitySubmitCountInBack_l10n_key { get; }

	public string StoreQuantitySubmitUnitPrice { get; private set; }

	public string StoreQuantitySubmitUnitPrice_l10n_key { get; }

	public string StoreItemTipPlantbasinLocked { get; private set; }

	public string StoreItemTipPlantbasinLocked_l10n_key { get; }

	public string FarmbuilderErrBuildSystemNotSupport { get; private set; }

	public string FarmbuilderErrBuildSystemNotSupport_l10n_key { get; }

	public string FarmbuilderErrLackOfAsset { get; private set; }

	public string FarmbuilderErrLackOfAsset_l10n_key { get; }

	public string FarmbuilderErrIndoorEquipment { get; private set; }

	public string FarmbuilderErrIndoorEquipment_l10n_key { get; }

	public string FarmbuilderErrOutdoorEquipment { get; private set; }

	public string FarmbuilderErrOutdoorEquipment_l10n_key { get; }

	public string FarmbuilderErrInvalidPosition { get; private set; }

	public string FarmbuilderErrInvalidPosition_l10n_key { get; }

	public string FarmbuilderErrOutOfRange { get; private set; }

	public string FarmbuilderErrOutOfRange_l10n_key { get; }

	public string FarmbuilderErrEquipmentAreaNotEmpty { get; private set; }

	public string FarmbuilderErrEquipmentAreaNotEmpty_l10n_key { get; }

	public string FarmbuilderErrPlatformOccupied { get; private set; }

	public string FarmbuilderErrPlatformOccupied_l10n_key { get; }

	public string FarmbuilderQuestionRemove { get; private set; }

	public string FarmbuilderQuestionRemove_l10n_key { get; }

	public string FarmbuilderQuestionRemoveReturn { get; private set; }

	public string FarmbuilderQuestionRemoveReturn_l10n_key { get; }

	public string FarmbuilderErrEquipmentOccupied { get; private set; }

	public string FarmbuilderErrEquipmentOccupied_l10n_key { get; }

	public string FarmbuilderErrPlantbasinTreeOccupied { get; private set; }

	public string FarmbuilderErrPlantbasinTreeOccupied_l10n_key { get; }

	public string FarmbuilderErrParkingApronOccupied { get; private set; }

	public string FarmbuilderErrParkingApronOccupied_l10n_key { get; }

	public string FarmbuilderErrAutomateBotOccupied { get; private set; }

	public string FarmbuilderErrAutomateBotOccupied_l10n_key { get; }

	public string FarmbuilderErrShowCaseOccupied { get; private set; }

	public string FarmbuilderErrShowCaseOccupied_l10n_key { get; }

	public string FarmbuilderErrPlatformRemove { get; private set; }

	public string FarmbuilderErrPlatformRemove_l10n_key { get; }

	public string FarmbuilderErrEquipmentHideDoor { get; private set; }

	public string FarmbuilderErrEquipmentHideDoor_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupied { get; private set; }

	public string FarmbuilderErrBuildingOccupied_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupiedAnimal { get; private set; }

	public string FarmbuilderErrBuildingOccupiedAnimal_l10n_key { get; }

	public string FarmbuilderErrBuildingAreaNotEmpty { get; private set; }

	public string FarmbuilderErrBuildingAreaNotEmpty_l10n_key { get; }

	public string FarmbuilderErrBuildingInvalidHeight { get; private set; }

	public string FarmbuilderErrBuildingInvalidHeight_l10n_key { get; }

	public string FarmbuilderErrBuildingInvalidCoveredRatio { get; private set; }

	public string FarmbuilderErrBuildingInvalidCoveredRatio_l10n_key { get; }

	public string FarmbuilderErrBuildingCanNotRemove { get; private set; }

	public string FarmbuilderErrBuildingCanNotRemove_l10n_key { get; }

	public string FarmbuilderErrBuildingInvalidOtherNoSupport { get; private set; }

	public string FarmbuilderErrBuildingInvalidOtherNoSupport_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupiedBuilding { get; private set; }

	public string FarmbuilderErrBuildingOccupiedBuilding_l10n_key { get; }

	public string FarmbuilderErrBuildingInvalidDoorBeHidden { get; private set; }

	public string FarmbuilderErrBuildingInvalidDoorBeHidden_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupiedCeiling { get; private set; }

	public string FarmbuilderErrBuildingOccupiedCeiling_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupiedDoor { get; private set; }

	public string FarmbuilderErrBuildingOccupiedDoor_l10n_key { get; }

	public string FarmbuilderErrBuildingOccupiedCeilingPt { get; private set; }

	public string FarmbuilderErrBuildingOccupiedCeilingPt_l10n_key { get; }

	public string FarmbuilderErrBuildingGroundInvalid { get; private set; }

	public string FarmbuilderErrBuildingGroundInvalid_l10n_key { get; }

	public string FarmbuilderErrPlatformWidth { get; private set; }

	public string FarmbuilderErrPlatformWidth_l10n_key { get; }

	public string FarmbuilderErrPlatformHeightMax { get; private set; }

	public string FarmbuilderErrPlatformHeightMax_l10n_key { get; }

	public string FarmbuilderErrEquipmentLimited { get; private set; }

	public string FarmbuilderErrEquipmentLimited_l10n_key { get; }

	public string TechtreePanelTitle { get; private set; }

	public string TechtreePanelTitle_l10n_key { get; }

	public string TechtreeNodeLackOfPoints { get; private set; }

	public string TechtreeNodeLackOfPoints_l10n_key { get; }

	public string TechtreeNodeNotAvaiable { get; private set; }

	public string TechtreeNodeNotAvaiable_l10n_key { get; }

	public string TechtreeNodeUnlocked { get; private set; }

	public string TechtreeNodeUnlocked_l10n_key { get; }

	public string TechtreeNodeUnopen { get; private set; }

	public string TechtreeNodeUnopen_l10n_key { get; }

	public string TechtreeNodeUnlockConfirm { get; private set; }

	public string TechtreeNodeUnlockConfirm_l10n_key { get; }

	public string TechtreeNodeBuildingHealth { get; private set; }

	public string TechtreeNodeBuildingHealth_l10n_key { get; }

	public string TechtreeNodeEquipmentElectronic { get; private set; }

	public string TechtreeNodeEquipmentElectronic_l10n_key { get; }

	public string TechtreeProgressInfo { get; private set; }

	public string TechtreeProgressInfo_l10n_key { get; }

	public string TechtreeMaxLevel { get; private set; }

	public string TechtreeMaxLevel_l10n_key { get; }

	public string TechtreeNodeHint { get; private set; }

	public string TechtreeNodeHint_l10n_key { get; }

	public string MissionUpdate { get; private set; }

	public string MissionUpdate_l10n_key { get; }

	public string MissionComplete { get; private set; }

	public string MissionComplete_l10n_key { get; }

	public string MissionCompleteSendEmail { get; private set; }

	public string MissionCompleteSendEmail_l10n_key { get; }

	public string MissionPanelTitle { get; private set; }

	public string MissionPanelTitle_l10n_key { get; }

	public string MissionPanelEmpty { get; private set; }

	public string MissionPanelEmpty_l10n_key { get; }

	public string UiMissionTimeLimit { get; private set; }

	public string UiMissionTimeLimit_l10n_key { get; }

	public string UiMissionNpcPosition { get; private set; }

	public string UiMissionNpcPosition_l10n_key { get; }

	public string SeedPanelTitle { get; private set; }

	public string SeedPanelTitle_l10n_key { get; }

	public string SeedNodeAlreadyUnlock { get; private set; }

	public string SeedNodeAlreadyUnlock_l10n_key { get; }

	public string SeedNodeSucceedUnlock { get; private set; }

	public string SeedNodeSucceedUnlock_l10n_key { get; }

	public string BuildingPanelTitle { get; private set; }

	public string BuildingPanelTitle_l10n_key { get; }

	public string BuildingPanelCoverSizeDescription { get; private set; }

	public string BuildingPanelCoverSizeDescription_l10n_key { get; }

	public string BuildingPanelInnerSizeDescription { get; private set; }

	public string BuildingPanelInnerSizeDescription_l10n_key { get; }

	public string BuildingPanelAnimalCapacityDescription { get; private set; }

	public string BuildingPanelAnimalCapacityDescription_l10n_key { get; }

	public string BuildingPanelSizeDescription { get; private set; }

	public string BuildingPanelSizeDescription_l10n_key { get; }

	public string BuildingPanelEmpty { get; private set; }

	public string BuildingPanelEmpty_l10n_key { get; }

	public string BuildingPanelStartBuild { get; private set; }

	public string BuildingPanelStartBuild_l10n_key { get; }

	public string BuildingPanelMaterialNotEnough { get; private set; }

	public string BuildingPanelMaterialNotEnough_l10n_key { get; }

	public string BuildingPanelMoneyNotEnough { get; private set; }

	public string BuildingPanelMoneyNotEnough_l10n_key { get; }

	public string BuildingPanelInputBuildingName { get; private set; }

	public string BuildingPanelInputBuildingName_l10n_key { get; }

	public string EquipmentPanelEmpty { get; private set; }

	public string EquipmentPanelEmpty_l10n_key { get; }

	public string EquipmentPanelStartBuild { get; private set; }

	public string EquipmentPanelStartBuild_l10n_key { get; }

	public string EquipmentPanelListName { get; private set; }

	public string EquipmentPanelListName_l10n_key { get; }

	public string EquipmentViewerEmpty { get; private set; }

	public string EquipmentViewerEmpty_l10n_key { get; }

	public string EquipmentPanelLatelyEmpty { get; private set; }

	public string EquipmentPanelLatelyEmpty_l10n_key { get; }

	public string EquipmentPanelMakeComplete { get; private set; }

	public string EquipmentPanelMakeComplete_l10n_key { get; }

	public string EquipmentPanelHide { get; private set; }

	public string EquipmentPanelHide_l10n_key { get; }

	public string EquipmentPanelJump { get; private set; }

	public string EquipmentPanelJump_l10n_key { get; }

	public string EquipmentPanelUnlockTip { get; private set; }

	public string EquipmentPanelUnlockTip_l10n_key { get; }

	public string EquipmentPanelElectronicTip { get; private set; }

	public string EquipmentPanelElectronicTip_l10n_key { get; }

	public string EquipmentPanelBatteryTip { get; private set; }

	public string EquipmentPanelBatteryTip_l10n_key { get; }

	public string EquipmentPanelApplianceTip { get; private set; }

	public string EquipmentPanelApplianceTip_l10n_key { get; }

	public string EquipmentPanelGeneratorTip { get; private set; }

	public string EquipmentPanelGeneratorTip_l10n_key { get; }

	public string UiEmailTitle { get; private set; }

	public string UiEmailTitle_l10n_key { get; }

	public string UiEmailEmpty { get; private set; }

	public string UiEmailEmpty_l10n_key { get; }

	public string UiEmailAcceptMission { get; private set; }

	public string UiEmailAcceptMission_l10n_key { get; }

	public string UiEmailAlreadyAccepted { get; private set; }

	public string UiEmailAlreadyAccepted_l10n_key { get; }

	public string UiEmailReciveItem { get; private set; }

	public string UiEmailReciveItem_l10n_key { get; }

	public string UiEmailAlreadyRecived { get; private set; }

	public string UiEmailAlreadyRecived_l10n_key { get; }

	public string UiEmailRecycleErr { get; private set; }

	public string UiEmailRecycleErr_l10n_key { get; }

	public string RecipePanelEmpty { get; private set; }

	public string RecipePanelEmpty_l10n_key { get; }

	public string RecipePanelStartBuild { get; private set; }

	public string RecipePanelStartBuild_l10n_key { get; }

	public string RecipePanelTimeInfo { get; private set; }

	public string RecipePanelTimeInfo_l10n_key { get; }

	public string RecipePanelAlreadyWorking { get; private set; }

	public string RecipePanelAlreadyWorking_l10n_key { get; }

	public string RecipePanelTitle { get; private set; }

	public string RecipePanelTitle_l10n_key { get; }

	public string RecipePanelTask { get; private set; }

	public string RecipePanelTask_l10n_key { get; }

	public string RecipePanelRestTime { get; private set; }

	public string RecipePanelRestTime_l10n_key { get; }

	public string RecipePanelMaxCraftCount { get; private set; }

	public string RecipePanelMaxCraftCount_l10n_key { get; }

	public string RecipePanelLimitUp { get; private set; }

	public string RecipePanelLimitUp_l10n_key { get; }

	public string RecipePanelNotCookable { get; private set; }

	public string RecipePanelNotCookable_l10n_key { get; }

	public string RecipePanelNoMaterial { get; private set; }

	public string RecipePanelNoMaterial_l10n_key { get; }

	public string RecipePanelConfirmWithTime { get; private set; }

	public string RecipePanelConfirmWithTime_l10n_key { get; }

	public string RecipePanelUnlockedRecipeComment { get; private set; }

	public string RecipePanelUnlockedRecipeComment_l10n_key { get; }

	public string RecipePanelUnlockedDishComment { get; private set; }

	public string RecipePanelUnlockedDishComment_l10n_key { get; }

	public string RecipePanelUnknowDishComment { get; private set; }

	public string RecipePanelUnknowDishComment_l10n_key { get; }

	public string RecipePanelConfirmPopBuffer { get; private set; }

	public string RecipePanelConfirmPopBuffer_l10n_key { get; }

	public string RecipePanelMaterialList { get; private set; }

	public string RecipePanelMaterialList_l10n_key { get; }

	public string RecipePanelExistItems { get; private set; }

	public string RecipePanelExistItems_l10n_key { get; }

	public string RecipePanelRandomGeneHint { get; private set; }

	public string RecipePanelRandomGeneHint_l10n_key { get; }

	public string PlatformPanelTitle { get; private set; }

	public string PlatformPanelTitle_l10n_key { get; }

	public string PlatformPanelEmpty { get; private set; }

	public string PlatformPanelEmpty_l10n_key { get; }

	public string PlatformPanelStartBuild { get; private set; }

	public string PlatformPanelStartBuild_l10n_key { get; }

	public string PlatformPanelCostPrefix { get; private set; }

	public string PlatformPanelCostPrefix_l10n_key { get; }

	public string ItemBuildingProtoError { get; private set; }

	public string ItemBuildingProtoError_l10n_key { get; }

	public string ItemEquipmentProtoError { get; private set; }

	public string ItemEquipmentProtoError_l10n_key { get; }

	public string ItemSendEmailOnOverflow { get; private set; }

	public string ItemSendEmailOnOverflow_l10n_key { get; }

	public string ItemSleepingBagConditionFailedMonster { get; private set; }

	public string ItemSleepingBagConditionFailedMonster_l10n_key { get; }

	public string ItemSleepingBagConditionFailedWater { get; private set; }

	public string ItemSleepingBagConditionFailedWater_l10n_key { get; }

	public string ItemRescuePagerConditionFailed { get; private set; }

	public string ItemRescuePagerConditionFailed_l10n_key { get; }

	public string ItemTipPrice { get; private set; }

	public string ItemTipPrice_l10n_key { get; }

	public string ItemTipBasicsPrice { get; private set; }

	public string ItemTipBasicsPrice_l10n_key { get; }

	public string ItemTipMoneyUnit { get; private set; }

	public string ItemTipMoneyUnit_l10n_key { get; }

	public string ItemChipAttackIncrease { get; private set; }

	public string ItemChipAttackIncrease_l10n_key { get; }

	public string ItemChipAttackIncreaseFixed { get; private set; }

	public string ItemChipAttackIncreaseFixed_l10n_key { get; }

	public string ItemChipCriticalRateIncrease { get; private set; }

	public string ItemChipCriticalRateIncrease_l10n_key { get; }

	public string ItemChipAttackSpeedIncrease { get; private set; }

	public string ItemChipAttackSpeedIncrease_l10n_key { get; }

	public string ItemChipAccuracyIncrease { get; private set; }

	public string ItemChipAccuracyIncrease_l10n_key { get; }

	public string ItemChipPowerCostDecrease { get; private set; }

	public string ItemChipPowerCostDecrease_l10n_key { get; }

	public string ItemChipAttackDistanceIncrease { get; private set; }

	public string ItemChipAttackDistanceIncrease_l10n_key { get; }

	public string ItemChipMoveSpeedIncrease { get; private set; }

	public string ItemChipMoveSpeedIncrease_l10n_key { get; }

	public string ItemChipClipCapacityAddition { get; private set; }

	public string ItemChipClipCapacityAddition_l10n_key { get; }

	public string ItemChipReloadDurationDecrease { get; private set; }

	public string ItemChipReloadDurationDecrease_l10n_key { get; }

	public string ItemEngineMoveSpeedIncrease { get; private set; }

	public string ItemEngineMoveSpeedIncrease_l10n_key { get; }

	public string ItemEnginePowerCapacityIncrease { get; private set; }

	public string ItemEnginePowerCapacityIncrease_l10n_key { get; }

	public string ItemEnginePowerRecvIncrease { get; private set; }

	public string ItemEnginePowerRecvIncrease_l10n_key { get; }

	public string ItemStructureMoveSpeedIncrease { get; private set; }

	public string ItemStructureMoveSpeedIncrease_l10n_key { get; }

	public string ItemStructurePowerCapacity { get; private set; }

	public string ItemStructurePowerCapacity_l10n_key { get; }

	public string ItemStructurePowerRecv { get; private set; }

	public string ItemStructurePowerRecv_l10n_key { get; }

	public string ItemMaxDurability { get; private set; }

	public string ItemMaxDurability_l10n_key { get; }

	public string ItemTitleInfoFormat { get; private set; }

	public string ItemTitleInfoFormat_l10n_key { get; }

	public string ItemConfirmUse { get; private set; }

	public string ItemConfirmUse_l10n_key { get; }

	public string ItemWaterCanArea { get; private set; }

	public string ItemWaterCanArea_l10n_key { get; }

	public string ItemWaterCanEndlessWater { get; private set; }

	public string ItemWaterCanEndlessWater_l10n_key { get; }

	public string ItemGeneDescFormat { get; private set; }

	public string ItemGeneDescFormat_l10n_key { get; }

	public string ItemSeedCloned { get; private set; }

	public string ItemSeedCloned_l10n_key { get; }

	public string ItemWpIsAvailableFor { get; private set; }

	public string ItemWpIsAvailableFor_l10n_key { get; }

	public string ItemWpAvailableAll { get; private set; }

	public string ItemWpAvailableAll_l10n_key { get; }

	public string ItemFishFryTitleFormat { get; private set; }

	public string ItemFishFryTitleFormat_l10n_key { get; }

	public string ItemToolLevelFormat { get; private set; }

	public string ItemToolLevelFormat_l10n_key { get; }

	public string ItemPatchValueFormat { get; private set; }

	public string ItemPatchValueFormat_l10n_key { get; }

	public string ItemBoxValueFormat { get; private set; }

	public string ItemBoxValueFormat_l10n_key { get; }

	public string ItemFilmValueFormat { get; private set; }

	public string ItemFilmValueFormat_l10n_key { get; }

	public string ItemDroneWeaponValueFormat { get; private set; }

	public string ItemDroneWeaponValueFormat_l10n_key { get; }

	public string ItemMoneyTitle { get; private set; }

	public string ItemMoneyTitle_l10n_key { get; }

	public string ItemMoneyDesc { get; private set; }

	public string ItemMoneyDesc_l10n_key { get; }

	public string ItemMoneyType { get; private set; }

	public string ItemMoneyType_l10n_key { get; }

	public string UiSystemExitGame { get; private set; }

	public string UiSystemExitGame_l10n_key { get; }

	public string UiSystemReturnHomePage { get; private set; }

	public string UiSystemReturnHomePage_l10n_key { get; }

	public string UiSystemConfirmHome { get; private set; }

	public string UiSystemConfirmHome_l10n_key { get; }

	public string UiSystemConfirmExit { get; private set; }

	public string UiSystemConfirmExit_l10n_key { get; }

	public string RewardInfoBuildingUnlock { get; private set; }

	public string RewardInfoBuildingUnlock_l10n_key { get; }

	public string RewardInfoBusStationUnlock { get; private set; }

	public string RewardInfoBusStationUnlock_l10n_key { get; }

	public string RewardInfoEquipmentUnlock { get; private set; }

	public string RewardInfoEquipmentUnlock_l10n_key { get; }

	public string RewardInfoPlatformUnlock { get; private set; }

	public string RewardInfoPlatformUnlock_l10n_key { get; }

	public string RewardInfoRecipeUnlock { get; private set; }

	public string RewardInfoRecipeUnlock_l10n_key { get; }

	public string RewardInfoRecipeUnlockHint { get; private set; }

	public string RewardInfoRecipeUnlockHint_l10n_key { get; }

	public string RewardInfoItem { get; private set; }

	public string RewardInfoItem_l10n_key { get; }

	public string RewardInfoGold { get; private set; }

	public string RewardInfoGold_l10n_key { get; }

	public string RewardInfoFavorabilityLv1 { get; private set; }

	public string RewardInfoFavorabilityLv1_l10n_key { get; }

	public string RewardInfoFavorabilityLv2 { get; private set; }

	public string RewardInfoFavorabilityLv2_l10n_key { get; }

	public string RewardInfoFavorabilityLv3 { get; private set; }

	public string RewardInfoFavorabilityLv3_l10n_key { get; }

	public string DropoffBoxNoGoods { get; private set; }

	public string DropoffBoxNoGoods_l10n_key { get; }

	public string DropoffBoxNoDrone { get; private set; }

	public string DropoffBoxNoDrone_l10n_key { get; }

	public string DropoffBoxTotalMoney { get; private set; }

	public string DropoffBoxTotalMoney_l10n_key { get; }

	public string DropoffBoxPriceIncreased { get; private set; }

	public string DropoffBoxPriceIncreased_l10n_key { get; }

	public string DropoffBoxLaunchDrone { get; private set; }

	public string DropoffBoxLaunchDrone_l10n_key { get; }

	public string DropoffBoxConfirmLauch { get; private set; }

	public string DropoffBoxConfirmLauch_l10n_key { get; }

	public string AutomateBotPanelStateIdle { get; private set; }

	public string AutomateBotPanelStateIdle_l10n_key { get; }

	public string AutomateBotPanelStateCharge { get; private set; }

	public string AutomateBotPanelStateCharge_l10n_key { get; }

	public string AutomateBotPanelStatePause { get; private set; }

	public string AutomateBotPanelStatePause_l10n_key { get; }

	public string AutomateBotPanelStateWorking { get; private set; }

	public string AutomateBotPanelStateWorking_l10n_key { get; }

	public string AutomateBotPanelCfgEmpty { get; private set; }

	public string AutomateBotPanelCfgEmpty_l10n_key { get; }

	public string AutomateBotPanelRecipeEmpty { get; private set; }

	public string AutomateBotPanelRecipeEmpty_l10n_key { get; }

	public string AutomateBotPanelDateEmpty { get; private set; }

	public string AutomateBotPanelDateEmpty_l10n_key { get; }

	public string AutomateBotPanelRecipeType { get; private set; }

	public string AutomateBotPanelRecipeType_l10n_key { get; }

	public string AutomateBotPanelRecipeSubType { get; private set; }

	public string AutomateBotPanelRecipeSubType_l10n_key { get; }

	public string AutomateBotPanelAutoFertilizer { get; private set; }

	public string AutomateBotPanelAutoFertilizer_l10n_key { get; }

	public string AutomateBotPanelAutoProtect { get; private set; }

	public string AutomateBotPanelAutoProtect_l10n_key { get; }

	public string AutomateBotPanelEnergyType { get; private set; }

	public string AutomateBotPanelEnergyType_l10n_key { get; }

	public string AutomateBotPanelItemError { get; private set; }

	public string AutomateBotPanelItemError_l10n_key { get; }

	public string BoardMissionPanelTitle { get; private set; }

	public string BoardMissionPanelTitle_l10n_key { get; }

	public string BoardMissionUrgencyLow { get; private set; }

	public string BoardMissionUrgencyLow_l10n_key { get; }

	public string BoardMissionUrgencyMiddle { get; private set; }

	public string BoardMissionUrgencyMiddle_l10n_key { get; }

	public string BoardMissionUrgencyHigh { get; private set; }

	public string BoardMissionUrgencyHigh_l10n_key { get; }

	public string BoardMissionUrgencyNone { get; private set; }

	public string BoardMissionUrgencyNone_l10n_key { get; }

	public string BoardMissionAcceptFail { get; private set; }

	public string BoardMissionAcceptFail_l10n_key { get; }

	public string BoardMissionTitle { get; private set; }

	public string BoardMissionTitle_l10n_key { get; }

	public string BoardMissionLowLevel { get; private set; }

	public string BoardMissionLowLevel_l10n_key { get; }

	public string BoardMissionEmpty { get; private set; }

	public string BoardMissionEmpty_l10n_key { get; }

	public string BoardMissionOverdue { get; private set; }

	public string BoardMissionOverdue_l10n_key { get; }

	public string BoardMissionLv { get; private set; }

	public string BoardMissionLv_l10n_key { get; }

	public string BoardMissionExpTip { get; private set; }

	public string BoardMissionExpTip_l10n_key { get; }

	public string InventoryPanelCannotPutIn { get; private set; }

	public string InventoryPanelCannotPutIn_l10n_key { get; }

	public string InventoryPanelContainerFull { get; private set; }

	public string InventoryPanelContainerFull_l10n_key { get; }

	public string BoxPanelRestCount { get; private set; }

	public string BoxPanelRestCount_l10n_key { get; }

	public string BoxPanelUsedUpWarning { get; private set; }

	public string BoxPanelUsedUpWarning_l10n_key { get; }

	public string BoxPanelBroken { get; private set; }

	public string BoxPanelBroken_l10n_key { get; }

	public string BoxPanelAlreadyOpen { get; private set; }

	public string BoxPanelAlreadyOpen_l10n_key { get; }

	public string BoxPanelNoNeedRepair { get; private set; }

	public string BoxPanelNoNeedRepair_l10n_key { get; }

	public string BoxPanelNoRepairCost { get; private set; }

	public string BoxPanelNoRepairCost_l10n_key { get; }

	public string BoxPanelRepairInfo { get; private set; }

	public string BoxPanelRepairInfo_l10n_key { get; }

	public string InventoryPanelDungeonCaseTitle { get; private set; }

	public string InventoryPanelDungeonCaseTitle_l10n_key { get; }

	public string InventoryPanelBackpackTitle { get; private set; }

	public string InventoryPanelBackpackTitle_l10n_key { get; }

	public string FishTankPanelFeedQuantity { get; private set; }

	public string FishTankPanelFeedQuantity_l10n_key { get; }

	public string InventoryPanelSocketTitle { get; private set; }

	public string InventoryPanelSocketTitle_l10n_key { get; }

	public string InventoryPanelPutBoxFirst { get; private set; }

	public string InventoryPanelPutBoxFirst_l10n_key { get; }

	public string InventoryPanelInfoGeneIncubator { get; private set; }

	public string InventoryPanelInfoGeneIncubator_l10n_key { get; }

	public string InventoryPanelCapsuleTitle { get; private set; }

	public string InventoryPanelCapsuleTitle_l10n_key { get; }

	public string InventoryPanelInfoGeneReplicator { get; private set; }

	public string InventoryPanelInfoGeneReplicator_l10n_key { get; }

	public string InventoryPanelGeneReplicatorLocked { get; private set; }

	public string InventoryPanelGeneReplicatorLocked_l10n_key { get; }

	public string InventoryPanelInfoGeneSynthesizer { get; private set; }

	public string InventoryPanelInfoGeneSynthesizer_l10n_key { get; }

	public string InventoryPanelGeneSynthesizerLocked { get; private set; }

	public string InventoryPanelGeneSynthesizerLocked_l10n_key { get; }

	public string UiTipTimeTitle { get; private set; }

	public string UiTipTimeTitle_l10n_key { get; }

	public string UiTipTimeFormat { get; private set; }

	public string UiTipTimeFormat_l10n_key { get; }

	public string UiTipCurrentSeason { get; private set; }

	public string UiTipCurrentSeason_l10n_key { get; }

	public string UiTipOpenMenu { get; private set; }

	public string UiTipOpenMenu_l10n_key { get; }

	public string UiTipOpenMap { get; private set; }

	public string UiTipOpenMap_l10n_key { get; }

	public string UiTipShowMissionTip { get; private set; }

	public string UiTipShowMissionTip_l10n_key { get; }

	public string UiTipHideMissionTip { get; private set; }

	public string UiTipHideMissionTip_l10n_key { get; }

	public string UiTipNoMission { get; private set; }

	public string UiTipNoMission_l10n_key { get; }

	public string UiTipMissionShowDetails { get; private set; }

	public string UiTipMissionShowDetails_l10n_key { get; }

	public string UiTipHealthValue { get; private set; }

	public string UiTipHealthValue_l10n_key { get; }

	public string UiTipEnergyValue { get; private set; }

	public string UiTipEnergyValue_l10n_key { get; }

	public string UiTipCorrosionValue { get; private set; }

	public string UiTipCorrosionValue_l10n_key { get; }

	public string UiTipCurrentSpiritLe50 { get; private set; }

	public string UiTipCurrentSpiritLe50_l10n_key { get; }

	public string UiTipCurrentSpiritLe25 { get; private set; }

	public string UiTipCurrentSpiritLe25_l10n_key { get; }

	public string UiTipCurrentSpiritLe10 { get; private set; }

	public string UiTipCurrentSpiritLe10_l10n_key { get; }

	public string UiTipCurrentSpiritLe0 { get; private set; }

	public string UiTipCurrentSpiritLe0_l10n_key { get; }

	public string UiTipBuffDuration { get; private set; }

	public string UiTipBuffDuration_l10n_key { get; }

	public string UiTipDebuffDuration { get; private set; }

	public string UiTipDebuffDuration_l10n_key { get; }

	public string UiTipBuildHealth { get; private set; }

	public string UiTipBuildHealth_l10n_key { get; }

	public string UiTipElectricityGeneratorInfo { get; private set; }

	public string UiTipElectricityGeneratorInfo_l10n_key { get; }

	public string UiTipElectricityBatteryInfo { get; private set; }

	public string UiTipElectricityBatteryInfo_l10n_key { get; }

	public string UiTipElectricityStatusNone { get; private set; }

	public string UiTipElectricityStatusNone_l10n_key { get; }

	public string UiTipElectricityStatusLackOfGeneration { get; private set; }

	public string UiTipElectricityStatusLackOfGeneration_l10n_key { get; }

	public string UiTipElectricityStatusLackOfGenerationCostBattery { get; private set; }

	public string UiTipElectricityStatusLackOfGenerationCostBattery_l10n_key { get; }

	public string UiTipElectricityStatusBatterySaving { get; private set; }

	public string UiTipElectricityStatusBatterySaving_l10n_key { get; }

	public string UiTipElectricityStatusBatteryFull { get; private set; }

	public string UiTipElectricityStatusBatteryFull_l10n_key { get; }

	public string UiTipElectricityStatusPowerLoss { get; private set; }

	public string UiTipElectricityStatusPowerLoss_l10n_key { get; }

	public string UiTipSeedEnd { get; private set; }

	public string UiTipSeedEnd_l10n_key { get; }

	public string UiTipLoading { get; private set; }

	public string UiTipLoading_l10n_key { get; }

	public string DocumentTitleComputer { get; private set; }

	public string DocumentTitleComputer_l10n_key { get; }

	public string NpcDocumentTitle { get; private set; }

	public string NpcDocumentTitle_l10n_key { get; }

	public string ChipDocumentTitle { get; private set; }

	public string ChipDocumentTitle_l10n_key { get; }

	public string PlantDocumentTitle { get; private set; }

	public string PlantDocumentTitle_l10n_key { get; }

	public string DocumentEmpty { get; private set; }

	public string DocumentEmpty_l10n_key { get; }

	public string ChipDocumentTotle { get; private set; }

	public string ChipDocumentTotle_l10n_key { get; }

	public string GameDataPanelTitle { get; private set; }

	public string GameDataPanelTitle_l10n_key { get; }

	public string GameDataErrRead { get; private set; }

	public string GameDataErrRead_l10n_key { get; }

	public string GameDataTitle { get; private set; }

	public string GameDataTitle_l10n_key { get; }

	public string GameDataDelConfirm { get; private set; }

	public string GameDataDelConfirm_l10n_key { get; }

	public string GameDataDelSuccess { get; private set; }

	public string GameDataDelSuccess_l10n_key { get; }

	public string GameDataErrEmpty { get; private set; }

	public string GameDataErrEmpty_l10n_key { get; }

	public string GameDataSaving { get; private set; }

	public string GameDataSaving_l10n_key { get; }

	public string GameDataSaveFail { get; private set; }

	public string GameDataSaveFail_l10n_key { get; }

	public string GameDataSaveSuccessful { get; private set; }

	public string GameDataSaveSuccessful_l10n_key { get; }

	public string GameDataFull { get; private set; }

	public string GameDataFull_l10n_key { get; }

	public string GameDataDuplicateSucess { get; private set; }

	public string GameDataDuplicateSucess_l10n_key { get; }

	public string GameDataStart { get; private set; }

	public string GameDataStart_l10n_key { get; }

	public string GameDataLoad { get; private set; }

	public string GameDataLoad_l10n_key { get; }

	public string TeleportFail { get; private set; }

	public string TeleportFail_l10n_key { get; }

	public string TeleportConfirm { get; private set; }

	public string TeleportConfirm_l10n_key { get; }

	public string TeleportBuyTicket { get; private set; }

	public string TeleportBuyTicket_l10n_key { get; }

	public string TeleportErrMaterialNotEnough { get; private set; }

	public string TeleportErrMaterialNotEnough_l10n_key { get; }

	public string FactionMissionFinish { get; private set; }

	public string FactionMissionFinish_l10n_key { get; }

	public string FactionMissionCannotSubmit { get; private set; }

	public string FactionMissionCannotSubmit_l10n_key { get; }

	public string FactionMissionFinishSubmit { get; private set; }

	public string FactionMissionFinishSubmit_l10n_key { get; }

	public string FactionMissionMoneySubmit { get; private set; }

	public string FactionMissionMoneySubmit_l10n_key { get; }

	public string FactionMissionErrSubmit { get; private set; }

	public string FactionMissionErrSubmit_l10n_key { get; }

	public string FactionMissionLock { get; private set; }

	public string FactionMissionLock_l10n_key { get; }

	public string TreatyPortTitle { get; private set; }

	public string TreatyPortTitle_l10n_key { get; }

	public string TreatyPortRepairTime { get; private set; }

	public string TreatyPortRepairTime_l10n_key { get; }

	public string TreatyPortRecruitStats { get; private set; }

	public string TreatyPortRecruitStats_l10n_key { get; }

	public string TreatyPortPrincipal { get; private set; }

	public string TreatyPortPrincipal_l10n_key { get; }

	public string TreatyPortFactionMissionProgress { get; private set; }

	public string TreatyPortFactionMissionProgress_l10n_key { get; }

	public string TreatyPortFactionReputation { get; private set; }

	public string TreatyPortFactionReputation_l10n_key { get; }

	public string TreatyPortContactNpc { get; private set; }

	public string TreatyPortContactNpc_l10n_key { get; }

	public string TreatyPortFactionEnterTime { get; private set; }

	public string TreatyPortFactionEnterTime_l10n_key { get; }

	public string TreatyPortBroadcast { get; private set; }

	public string TreatyPortBroadcast_l10n_key { get; }

	public string TreatyPortFactionLock { get; private set; }

	public string TreatyPortFactionLock_l10n_key { get; }

	public string TreatyPortFactionUnopen { get; private set; }

	public string TreatyPortFactionUnopen_l10n_key { get; }

	public string TreatyPortRecruitHint { get; private set; }

	public string TreatyPortRecruitHint_l10n_key { get; }

	public string TreatyPortTimeFormat { get; private set; }

	public string TreatyPortTimeFormat_l10n_key { get; }

	public string TreatyPortFactionRefuse { get; private set; }

	public string TreatyPortFactionRefuse_l10n_key { get; }

	public string TreatyPortOffDutyHoursTip { get; private set; }

	public string TreatyPortOffDutyHoursTip_l10n_key { get; }

	public string TreatyPortInterviewTip { get; private set; }

	public string TreatyPortInterviewTip_l10n_key { get; }

	public string TreatyPortFactionSettledTip { get; private set; }

	public string TreatyPortFactionSettledTip_l10n_key { get; }

	public string TreatyPortNotContactedTip { get; private set; }

	public string TreatyPortNotContactedTip_l10n_key { get; }

	public string SettingPanelSaveSuccessful { get; private set; }

	public string SettingPanelSaveSuccessful_l10n_key { get; }

	public string SettingPanelOtherSaveSuccessful { get; private set; }

	public string SettingPanelOtherSaveSuccessful_l10n_key { get; }

	public string SettingPanelReset { get; private set; }

	public string SettingPanelReset_l10n_key { get; }

	public string SettingPanelResetConfirm { get; private set; }

	public string SettingPanelResetConfirm_l10n_key { get; }

	public string SettingPanelTitle { get; private set; }

	public string SettingPanelTitle_l10n_key { get; }

	public string SettingPanelExitConfirm { get; private set; }

	public string SettingPanelExitConfirm_l10n_key { get; }

	public string SettingPanelConflictHint { get; private set; }

	public string SettingPanelConflictHint_l10n_key { get; }

	public string SettingPanelResetToDefault { get; private set; }

	public string SettingPanelResetToDefault_l10n_key { get; }

	public string SettingPanelCanNotEdit { get; private set; }

	public string SettingPanelCanNotEdit_l10n_key { get; }

	public string SettingPanelCanNotRemove { get; private set; }

	public string SettingPanelCanNotRemove_l10n_key { get; }

	public string SettingPanelNoValidInput { get; private set; }

	public string SettingPanelNoValidInput_l10n_key { get; }

	public string SettingPanelAllowTracedataCollector { get; private set; }

	public string SettingPanelAllowTracedataCollector_l10n_key { get; }

	public string SettingPanelTracedataCollectorDesc { get; private set; }

	public string SettingPanelTracedataCollectorDesc_l10n_key { get; }

	public string SettingPanelDelete { get; private set; }

	public string SettingPanelDelete_l10n_key { get; }

	public string SettingPanelCanNotSaveByConflict { get; private set; }

	public string SettingPanelCanNotSaveByConflict_l10n_key { get; }

	public string SettingPanelNewDevice { get; private set; }

	public string SettingPanelNewDevice_l10n_key { get; }

	public string SettingPanelLoading { get; private set; }

	public string SettingPanelLoading_l10n_key { get; }

	public string EquipmentBarTimeLabel { get; private set; }

	public string EquipmentBarTimeLabel_l10n_key { get; }

	public string EquipmentBarTimeFormat { get; private set; }

	public string EquipmentBarTimeFormat_l10n_key { get; }

	public string EquipmentBarDroneNotEquip { get; private set; }

	public string EquipmentBarDroneNotEquip_l10n_key { get; }

	public string EquipmentBarMotorTitle { get; private set; }

	public string EquipmentBarMotorTitle_l10n_key { get; }

	public string EquipmentBarMotorLock { get; private set; }

	public string EquipmentBarMotorLock_l10n_key { get; }

	public string EquipmentBarHatTip { get; private set; }

	public string EquipmentBarHatTip_l10n_key { get; }

	public string EquipmentBarPositiveTip { get; private set; }

	public string EquipmentBarPositiveTip_l10n_key { get; }

	public string EquipmentBarPassive1Tip { get; private set; }

	public string EquipmentBarPassive1Tip_l10n_key { get; }

	public string EquipmentBarPassive2Tip { get; private set; }

	public string EquipmentBarPassive2Tip_l10n_key { get; }

	public string EquipmentBarDroneTip { get; private set; }

	public string EquipmentBarDroneTip_l10n_key { get; }

	public string EquipmentBarSkillLock { get; private set; }

	public string EquipmentBarSkillLock_l10n_key { get; }

	public string EquipmentBarDoubleJumpDesc { get; private set; }

	public string EquipmentBarDoubleJumpDesc_l10n_key { get; }

	public string EquipmentBarSprintDesc { get; private set; }

	public string EquipmentBarSprintDesc_l10n_key { get; }

	public string AbilityDoubleJumpUnlockTip { get; private set; }

	public string AbilityDoubleJumpUnlockTip_l10n_key { get; }

	public string AbilitySprintUnlockTip { get; private set; }

	public string AbilitySprintUnlockTip_l10n_key { get; }

	public string EquipmentBarBackpackFull { get; private set; }

	public string EquipmentBarBackpackFull_l10n_key { get; }

	public string EquipmentSkillPrefix { get; private set; }

	public string EquipmentSkillPrefix_l10n_key { get; }

	public string EquipmentDefensePrefix { get; private set; }

	public string EquipmentDefensePrefix_l10n_key { get; }

	public string UiTipHomepageStartGame { get; private set; }

	public string UiTipHomepageStartGame_l10n_key { get; }

	public string UiTipHomepageChangelog { get; private set; }

	public string UiTipHomepageChangelog_l10n_key { get; }

	public string UiTipHomepageSettings { get; private set; }

	public string UiTipHomepageSettings_l10n_key { get; }

	public string UiTipHomepageMods { get; private set; }

	public string UiTipHomepageMods_l10n_key { get; }

	public string UiTipHomepageDeveloperList { get; private set; }

	public string UiTipHomepageDeveloperList_l10n_key { get; }

	public string UiTipHomepageExitGame { get; private set; }

	public string UiTipHomepageExitGame_l10n_key { get; }

	public string UiTipParkingApronLocked { get; private set; }

	public string UiTipParkingApronLocked_l10n_key { get; }

	public string UiTipResolving { get; private set; }

	public string UiTipResolving_l10n_key { get; }

	public string UiTipShredderMoney { get; private set; }

	public string UiTipShredderMoney_l10n_key { get; }

	public string UiTipErrShredderEmpty { get; private set; }

	public string UiTipErrShredderEmpty_l10n_key { get; }

	public string UiTipAirWall { get; private set; }

	public string UiTipAirWall_l10n_key { get; }

	public string UiTipNotAvailableToMotor { get; private set; }

	public string UiTipNotAvailableToMotor_l10n_key { get; }

	public string CollectionPanelItemLabel { get; private set; }

	public string CollectionPanelItemLabel_l10n_key { get; }

	public string CollectionPanelItemRecipeTime { get; private set; }

	public string CollectionPanelItemRecipeTime_l10n_key { get; }

	public string CollectionPanelItemRecipeEmpty { get; private set; }

	public string CollectionPanelItemRecipeEmpty_l10n_key { get; }

	public string CollectionPanelItemUnknown { get; private set; }

	public string CollectionPanelItemUnknown_l10n_key { get; }

	public string CollectionPanelItemSource { get; private set; }

	public string CollectionPanelItemSource_l10n_key { get; }

	public string CollectionPanelNpcAddress { get; private set; }

	public string CollectionPanelNpcAddress_l10n_key { get; }

	public string CollectionPanelNpcLikeRecord { get; private set; }

	public string CollectionPanelNpcLikeRecord_l10n_key { get; }

	public string CollectionPanelNpcLikeNone { get; private set; }

	public string CollectionPanelNpcLikeNone_l10n_key { get; }

	public string CollectionPanelNpcLikingLock { get; private set; }

	public string CollectionPanelNpcLikingLock_l10n_key { get; }

	public string CollectionPanelNpcLikingLvLock { get; private set; }

	public string CollectionPanelNpcLikingLvLock_l10n_key { get; }

	public string CollectionPanelNpcContentTitle { get; private set; }

	public string CollectionPanelNpcContentTitle_l10n_key { get; }

	public string CollectionPanelNpcContentLockTip { get; private set; }

	public string CollectionPanelNpcContentLockTip_l10n_key { get; }

	public string CollectionPanelNpcContentEnd { get; private set; }

	public string CollectionPanelNpcContentEnd_l10n_key { get; }

	public string CollectionPanelMonsterUnknown { get; private set; }

	public string CollectionPanelMonsterUnknown_l10n_key { get; }

	public string CollectionPanelMonsterHabitat { get; private set; }

	public string CollectionPanelMonsterHabitat_l10n_key { get; }

	public string CollectionPanelMonsterDropText { get; private set; }

	public string CollectionPanelMonsterDropText_l10n_key { get; }

	public string CollectionPanelMonsterOrganism { get; private set; }

	public string CollectionPanelMonsterOrganism_l10n_key { get; }

	public string CollectionPanelMonsterMachinery { get; private set; }

	public string CollectionPanelMonsterMachinery_l10n_key { get; }

	public string CollectionPanelMonsterBoss { get; private set; }

	public string CollectionPanelMonsterBoss_l10n_key { get; }

	public string CollectionPanelMonsterContentTitle { get; private set; }

	public string CollectionPanelMonsterContentTitle_l10n_key { get; }

	public string CollectionPanelMonsterContentLockTip { get; private set; }

	public string CollectionPanelMonsterContentLockTip_l10n_key { get; }

	public string CollectionPanelDocumentLabel { get; private set; }

	public string CollectionPanelDocumentLabel_l10n_key { get; }

	public string CollectionPanelAnimalPossess { get; private set; }

	public string CollectionPanelAnimalPossess_l10n_key { get; }

	public string CollectionPanelAnimalBreed { get; private set; }

	public string CollectionPanelAnimalBreed_l10n_key { get; }

	public string CollectionPanelAnimalProductText { get; private set; }

	public string CollectionPanelAnimalProductText_l10n_key { get; }

	public string CollectionPanelAnimalContentLockBringUp { get; private set; }

	public string CollectionPanelAnimalContentLockBringUp_l10n_key { get; }

	public string CollectionPanelAnimalContentLockBreed { get; private set; }

	public string CollectionPanelAnimalContentLockBreed_l10n_key { get; }

	public string CollectionPanelAnimalContentLockProduct { get; private set; }

	public string CollectionPanelAnimalContentLockProduct_l10n_key { get; }

	public string CollectionPanelFishCatch { get; private set; }

	public string CollectionPanelFishCatch_l10n_key { get; }

	public string CollectionPanelFishBaitText { get; private set; }

	public string CollectionPanelFishBaitText_l10n_key { get; }

	public string CollectionPanelFishPlace { get; private set; }

	public string CollectionPanelFishPlace_l10n_key { get; }

	public string CollectionPanelFishMonth { get; private set; }

	public string CollectionPanelFishMonth_l10n_key { get; }

	public string CollectionPanelFishWeather { get; private set; }

	public string CollectionPanelFishWeather_l10n_key { get; }

	public string CollectionPanelFishWeatherNone { get; private set; }

	public string CollectionPanelFishWeatherNone_l10n_key { get; }

	public string CollectionPanelFishContentLockTip { get; private set; }

	public string CollectionPanelFishContentLockTip_l10n_key { get; }

	public string CollectionPanelResourceCollect { get; private set; }

	public string CollectionPanelResourceCollect_l10n_key { get; }

	public string CollectionPanelResourceGrowthPeriod { get; private set; }

	public string CollectionPanelResourceGrowthPeriod_l10n_key { get; }

	public string CollectionPanelResourceYearRoundGrowth { get; private set; }

	public string CollectionPanelResourceYearRoundGrowth_l10n_key { get; }

	public string CollectionPanelResourceDropTitle { get; private set; }

	public string CollectionPanelResourceDropTitle_l10n_key { get; }

	public string CollectionPanelResourceContentLockTip { get; private set; }

	public string CollectionPanelResourceContentLockTip_l10n_key { get; }

	public string BuilderPanelLabelBuilding { get; private set; }

	public string BuilderPanelLabelBuilding_l10n_key { get; }

	public string BuilderPanelLabelEquipment { get; private set; }

	public string BuilderPanelLabelEquipment_l10n_key { get; }

	public string BuilderPanelLabelPlatform { get; private set; }

	public string BuilderPanelLabelPlatform_l10n_key { get; }

	public string BuilderPanelSwitchTerrainLayer { get; private set; }

	public string BuilderPanelSwitchTerrainLayer_l10n_key { get; }

	public string BuilderActionSelectedContent { get; private set; }

	public string BuilderActionSelectedContent_l10n_key { get; }

	public string BuilderActionUndo { get; private set; }

	public string BuilderActionUndo_l10n_key { get; }

	public string BuilderActionTurn { get; private set; }

	public string BuilderActionTurn_l10n_key { get; }

	public string BuilderActionDismantle { get; private set; }

	public string BuilderActionDismantle_l10n_key { get; }

	public string BuilderActionBuildingDismantle { get; private set; }

	public string BuilderActionBuildingDismantle_l10n_key { get; }

	public string BuilderActionBuildingStorage { get; private set; }

	public string BuilderActionBuildingStorage_l10n_key { get; }

	public string BuilderActionMoveCamera { get; private set; }

	public string BuilderActionMoveCamera_l10n_key { get; }

	public string BuilderActionMove { get; private set; }

	public string BuilderActionMove_l10n_key { get; }

	public string BuilderActionSelectedItem { get; private set; }

	public string BuilderActionSelectedItem_l10n_key { get; }

	public string BuilderActionPreciseMovement { get; private set; }

	public string BuilderActionPreciseMovement_l10n_key { get; }

	public string BuilderActionSwitchPrecise { get; private set; }

	public string BuilderActionSwitchPrecise_l10n_key { get; }

	public string BuilderActionSwitchBackpack { get; private set; }

	public string BuilderActionSwitchBackpack_l10n_key { get; }

	public string BuilderActionRollingBackpack { get; private set; }

	public string BuilderActionRollingBackpack_l10n_key { get; }

	public string BuilderActionRollingItem { get; private set; }

	public string BuilderActionRollingItem_l10n_key { get; }

	public string BuilderActionToggleBackpack { get; private set; }

	public string BuilderActionToggleBackpack_l10n_key { get; }

	public string BuilderActionToggleBackpackGamepad { get; private set; }

	public string BuilderActionToggleBackpackGamepad_l10n_key { get; }

	public string BuilderPanelExit { get; private set; }

	public string BuilderPanelExit_l10n_key { get; }

	public string BuilderPanelUndoGamepad { get; private set; }

	public string BuilderPanelUndoGamepad_l10n_key { get; }

	public string BuilderPanelExpandBuildingList { get; private set; }

	public string BuilderPanelExpandBuildingList_l10n_key { get; }

	public string BuilderPanellFoldBuildingList { get; private set; }

	public string BuilderPanellFoldBuildingList_l10n_key { get; }

	public string BuilderPanelFinish { get; private set; }

	public string BuilderPanelFinish_l10n_key { get; }

	public string BuilderDismantleBuildingErr { get; private set; }

	public string BuilderDismantleBuildingErr_l10n_key { get; }

	public string BuilderBuilderConstruct { get; private set; }

	public string BuilderBuilderConstruct_l10n_key { get; }

	public string BuilderPanelTempBuildingFull { get; private set; }

	public string BuilderPanelTempBuildingFull_l10n_key { get; }

	public string BuilderExitErrAnimal { get; private set; }

	public string BuilderExitErrAnimal_l10n_key { get; }

	public string BuilderExitErrOccupied { get; private set; }

	public string BuilderExitErrOccupied_l10n_key { get; }

	public string BuilderExitErrSoleBuilding { get; private set; }

	public string BuilderExitErrSoleBuilding_l10n_key { get; }

	public string BuilderExitConfirm { get; private set; }

	public string BuilderExitConfirm_l10n_key { get; }

	public string UiEnvOptimizerPanelTitle { get; private set; }

	public string UiEnvOptimizerPanelTitle_l10n_key { get; }

	public string UiEnvOptimizerConsoleTitle { get; private set; }

	public string UiEnvOptimizerConsoleTitle_l10n_key { get; }

	public string UiEnvOptimizerOverview { get; private set; }

	public string UiEnvOptimizerOverview_l10n_key { get; }

	public string UiEnvOptimizerButtonNoEnergy { get; private set; }

	public string UiEnvOptimizerButtonNoEnergy_l10n_key { get; }

	public string UiEnvOptimizerTipNoEnergy { get; private set; }

	public string UiEnvOptimizerTipNoEnergy_l10n_key { get; }

	public string UiEnvOptimizerButtonNoComponent { get; private set; }

	public string UiEnvOptimizerButtonNoComponent_l10n_key { get; }

	public string UiEnvOptimizerTipNoComponent { get; private set; }

	public string UiEnvOptimizerTipNoComponent_l10n_key { get; }

	public string UiEnvOptimizerButtonValid { get; private set; }

	public string UiEnvOptimizerButtonValid_l10n_key { get; }

	public string UiEnvOptimizerButtonNotValid { get; private set; }

	public string UiEnvOptimizerButtonNotValid_l10n_key { get; }

	public string UiEnvOptimizerCheckSuccess { get; private set; }

	public string UiEnvOptimizerCheckSuccess_l10n_key { get; }

	public string UiEnvOptimizerDateInfo { get; private set; }

	public string UiEnvOptimizerDateInfo_l10n_key { get; }

	public string UiEnvOptimizerSlotTitle { get; private set; }

	public string UiEnvOptimizerSlotTitle_l10n_key { get; }

	public string UiEnvOptimizerComponentAlreadyActive { get; private set; }

	public string UiEnvOptimizerComponentAlreadyActive_l10n_key { get; }

	public string UiEnvOptimizerInRecognition { get; private set; }

	public string UiEnvOptimizerInRecognition_l10n_key { get; }

	public string UiEnvOptimizerLoadingData { get; private set; }

	public string UiEnvOptimizerLoadingData_l10n_key { get; }

	public string UiEnvOptimizerActiveSuccess { get; private set; }

	public string UiEnvOptimizerActiveSuccess_l10n_key { get; }

	public string AnimalInvalidBuilding { get; private set; }

	public string AnimalInvalidBuilding_l10n_key { get; }

	public string AnimalInvalidRoom { get; private set; }

	public string AnimalInvalidRoom_l10n_key { get; }

	public string AnimalFullBuilding { get; private set; }

	public string AnimalFullBuilding_l10n_key { get; }

	public string AnimalInputAnimalName { get; private set; }

	public string AnimalInputAnimalName_l10n_key { get; }

	public string AnimalPackageIsFull { get; private set; }

	public string AnimalPackageIsFull_l10n_key { get; }

	public string AnimalHasEscaped { get; private set; }

	public string AnimalHasEscaped_l10n_key { get; }

	public string AnimalNoAnimal { get; private set; }

	public string AnimalNoAnimal_l10n_key { get; }

	public string AnimalInUse { get; private set; }

	public string AnimalInUse_l10n_key { get; }

	public string CalendarPanelYearTitle { get; private set; }

	public string CalendarPanelYearTitle_l10n_key { get; }

	public string CalendarPanelDateTitle { get; private set; }

	public string CalendarPanelDateTitle_l10n_key { get; }

	public string CalendarPanelEventTitle { get; private set; }

	public string CalendarPanelEventTitle_l10n_key { get; }

	public string CalendarPanelPlayerBirthday { get; private set; }

	public string CalendarPanelPlayerBirthday_l10n_key { get; }

	public string CalendarPanelMemoTitle { get; private set; }

	public string CalendarPanelMemoTitle_l10n_key { get; }

	public string CalendarPanelEmptyHint { get; private set; }

	public string CalendarPanelEmptyHint_l10n_key { get; }

	public string CalendarPanelDelMemoHint { get; private set; }

	public string CalendarPanelDelMemoHint_l10n_key { get; }

	public string CalendarPanelInputEmptyHint { get; private set; }

	public string CalendarPanelInputEmptyHint_l10n_key { get; }

	public string InputTextContainsSensitiveWorld { get; private set; }

	public string InputTextContainsSensitiveWorld_l10n_key { get; }

	public string CalendarMemoMessage { get; private set; }

	public string CalendarMemoMessage_l10n_key { get; }

	public string UiCropInfoGrowthLevel { get; private set; }

	public string UiCropInfoGrowthLevel_l10n_key { get; }

	public string UiCropInfoHarvestCount { get; private set; }

	public string UiCropInfoHarvestCount_l10n_key { get; }

	public string UiAnimalInfoState { get; private set; }

	public string UiAnimalInfoState_l10n_key { get; }

	public string UiAnimalSpace { get; private set; }

	public string UiAnimalSpace_l10n_key { get; }

	public string UiAnimalAgeYear { get; private set; }

	public string UiAnimalAgeYear_l10n_key { get; }

	public string UiAnimalAgeMonth { get; private set; }

	public string UiAnimalAgeMonth_l10n_key { get; }

	public string UiAnimalAgeDay { get; private set; }

	public string UiAnimalAgeDay_l10n_key { get; }

	public string UiAnimalBirthday { get; private set; }

	public string UiAnimalBirthday_l10n_key { get; }

	public string UiAnimalCapacity { get; private set; }

	public string UiAnimalCapacity_l10n_key { get; }

	public string UiAnimalCallBack { get; private set; }

	public string UiAnimalCallBack_l10n_key { get; }

	public string UiAnimalLetOut { get; private set; }

	public string UiAnimalLetOut_l10n_key { get; }

	public string UiAnimalAdult { get; private set; }

	public string UiAnimalAdult_l10n_key { get; }

	public string UiAnimalChild { get; private set; }

	public string UiAnimalChild_l10n_key { get; }

	public string UiAnimalPosition { get; private set; }

	public string UiAnimalPosition_l10n_key { get; }

	public string UiAnimalNoAnimal { get; private set; }

	public string UiAnimalNoAnimal_l10n_key { get; }

	public string UiAnimalNotVisible { get; private set; }

	public string UiAnimalNotVisible_l10n_key { get; }

	public string DronePanelTitle { get; private set; }

	public string DronePanelTitle_l10n_key { get; }

	public string DronePanelErrLoad { get; private set; }

	public string DronePanelErrLoad_l10n_key { get; }

	public string DronePanelErrLocked { get; private set; }

	public string DronePanelErrLocked_l10n_key { get; }

	public string DroneComponentTitle { get; private set; }

	public string DroneComponentTitle_l10n_key { get; }

	public string GarbageSubmitErrItem { get; private set; }

	public string GarbageSubmitErrItem_l10n_key { get; }

	public string DisposeFailRoom { get; private set; }

	public string DisposeFailRoom_l10n_key { get; }

	public string UiTipOpenMapFail { get; private set; }

	public string UiTipOpenMapFail_l10n_key { get; }

	public string UiTipNone { get; private set; }

	public string UiTipNone_l10n_key { get; }

	public string UiTipBuyBackpack { get; private set; }

	public string UiTipBuyBackpack_l10n_key { get; }

	public string UiTipBuyBackpackInfo { get; private set; }

	public string UiTipBuyBackpackInfo_l10n_key { get; }

	public string UiTipEmptyList { get; private set; }

	public string UiTipEmptyList_l10n_key { get; }

	public string UiTipCurrentPosition { get; private set; }

	public string UiTipCurrentPosition_l10n_key { get; }

	public string UiTipCurrentMotorPosition { get; private set; }

	public string UiTipCurrentMotorPosition_l10n_key { get; }

	public string UiOptionShootingRangeStart { get; private set; }

	public string UiOptionShootingRangeStart_l10n_key { get; }

	public string UiOptionShootingRangeEnd { get; private set; }

	public string UiOptionShootingRangeEnd_l10n_key { get; }

	public string UiTipEquipmentUnlock { get; private set; }

	public string UiTipEquipmentUnlock_l10n_key { get; }

	public string UiOptionShootingRangeExit { get; private set; }

	public string UiOptionShootingRangeExit_l10n_key { get; }

	public string UiOptionConfirmShootingRangeExit { get; private set; }

	public string UiOptionConfirmShootingRangeExit_l10n_key { get; }

	public string UiNpcVisit { get; private set; }

	public string UiNpcVisit_l10n_key { get; }

	public string UiNpcFileUpdation { get; private set; }

	public string UiNpcFileUpdation_l10n_key { get; }

	public string UiNpcLikingRise { get; private set; }

	public string UiNpcLikingRise_l10n_key { get; }

	public string UiNpcLikingDecline { get; private set; }

	public string UiNpcLikingDecline_l10n_key { get; }

	public string ItemEdenFruitTip { get; private set; }

	public string ItemEdenFruitTip_l10n_key { get; }

	public string UiTipGameVersion { get; private set; }

	public string UiTipGameVersion_l10n_key { get; }

	public string UiTipContentLock { get; private set; }

	public string UiTipContentLock_l10n_key { get; }

	public string UiTipPhotoSaving { get; private set; }

	public string UiTipPhotoSaving_l10n_key { get; }

	public string UiItemGenerateElectricityEntry { get; private set; }

	public string UiItemGenerateElectricityEntry_l10n_key { get; }

	public string UiTipRename { get; private set; }

	public string UiTipRename_l10n_key { get; }

	public string UiModNoMod { get; private set; }

	public string UiModNoMod_l10n_key { get; }

	public string UiModNeedSubscribe { get; private set; }

	public string UiModNeedSubscribe_l10n_key { get; }

	public string UiModOpenWorkshop { get; private set; }

	public string UiModOpenWorkshop_l10n_key { get; }

	public string UiModNeedCreate { get; private set; }

	public string UiModNeedCreate_l10n_key { get; }

	public string UiModOpenLocalDirectory { get; private set; }

	public string UiModOpenLocalDirectory_l10n_key { get; }

	public string UiModUploadMod { get; private set; }

	public string UiModUploadMod_l10n_key { get; }

	public string UiModUpdateMod { get; private set; }

	public string UiModUpdateMod_l10n_key { get; }

	public string UiModEnable { get; private set; }

	public string UiModEnable_l10n_key { get; }

	public string UiModDisable { get; private set; }

	public string UiModDisable_l10n_key { get; }

	public string UiModConfirmUpload { get; private set; }

	public string UiModConfirmUpload_l10n_key { get; }

	public string UiModConfirmUpdate { get; private set; }

	public string UiModConfirmUpdate_l10n_key { get; }

	public string UiModUploading { get; private set; }

	public string UiModUploading_l10n_key { get; }

	public string UiModUploadSuccess { get; private set; }

	public string UiModUploadSuccess_l10n_key { get; }

	public string UiModUploadFailed { get; private set; }

	public string UiModUploadFailed_l10n_key { get; }

	public string UiModUpdating { get; private set; }

	public string UiModUpdating_l10n_key { get; }

	public string UiModUpdateSuccess { get; private set; }

	public string UiModUpdateSuccess_l10n_key { get; }

	public string UiModUpdateFailed { get; private set; }

	public string UiModUpdateFailed_l10n_key { get; }

	public string UiModReloading { get; private set; }

	public string UiModReloading_l10n_key { get; }

	public string UiModSourceLocal { get; private set; }

	public string UiModSourceLocal_l10n_key { get; }

	public string UiModSourceWorkshop { get; private set; }

	public string UiModSourceWorkshop_l10n_key { get; }

	public StaticTextInfo(JSONNode _json)
	{
		if (!_json["sys_err_datalost"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SysErrDatalost_l10n_key = _json["sys_err_datalost"]["key"];
		if (!_json["sys_err_datalost"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SysErrDatalost = _json["sys_err_datalost"]["text"];
		if (!_json["ui_money_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiMoneyTip_l10n_key = _json["ui_money_tip"]["key"];
		if (!_json["ui_money_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiMoneyTip = _json["ui_money_tip"]["text"];
		if (!_json["ui_item_simple_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiItemSimpleTip_l10n_key = _json["ui_item_simple_tip"]["key"];
		if (!_json["ui_item_simple_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiItemSimpleTip = _json["ui_item_simple_tip"]["text"];
		if (!_json["ui_item_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiItemTip_l10n_key = _json["ui_item_tip"]["key"];
		if (!_json["ui_item_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiItemTip = _json["ui_item_tip"]["text"];
		if (!_json["ui_tip_consumable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipConsumable_l10n_key = _json["ui_tip_consumable"]["key"];
		if (!_json["ui_tip_consumable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipConsumable = _json["ui_tip_consumable"]["text"];
		if (!_json["ui_err_material_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiErrMaterialNotEnough_l10n_key = _json["ui_err_material_not_enough"]["key"];
		if (!_json["ui_err_material_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiErrMaterialNotEnough = _json["ui_err_material_not_enough"]["text"];
		if (!_json["ui_err_money_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiErrMoneyNotEnough_l10n_key = _json["ui_err_money_not_enough"]["key"];
		if (!_json["ui_err_money_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiErrMoneyNotEnough = _json["ui_err_money_not_enough"]["text"];
		if (!_json["ui_err_backpack_is_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiErrBackpackIsFull_l10n_key = _json["ui_err_backpack_is_full"]["key"];
		if (!_json["ui_err_backpack_is_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiErrBackpackIsFull = _json["ui_err_backpack_is_full"]["text"];
		if (!_json["ui_err_not_dispose_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiErrNotDisposeItem_l10n_key = _json["ui_err_not_dispose_item"]["key"];
		if (!_json["ui_err_not_dispose_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiErrNotDisposeItem = _json["ui_err_not_dispose_item"]["text"];
		if (!_json["ui_ques_dispose_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiQuesDisposeItem_l10n_key = _json["ui_ques_dispose_item"]["key"];
		if (!_json["ui_ques_dispose_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiQuesDisposeItem = _json["ui_ques_dispose_item"]["text"];
		if (!_json["ui_menu_itemnotavaiable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiMenuItemnotavaiable_l10n_key = _json["ui_menu_itemnotavaiable"]["key"];
		if (!_json["ui_menu_itemnotavaiable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiMenuItemnotavaiable = _json["ui_menu_itemnotavaiable"]["text"];
		if (!_json["ui_backpack_cell_exchange"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiBackpackCellExchange_l10n_key = _json["ui_backpack_cell_exchange"]["key"];
		if (!_json["ui_backpack_cell_exchange"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiBackpackCellExchange = _json["ui_backpack_cell_exchange"]["text"];
		if (!_json["ui_err_backpack_cell_exchange"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiErrBackpackCellExchange_l10n_key = _json["ui_err_backpack_cell_exchange"]["key"];
		if (!_json["ui_err_backpack_cell_exchange"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiErrBackpackCellExchange = _json["ui_err_backpack_cell_exchange"]["text"];
		if (!_json["ui_sort_default"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSortDefault_l10n_key = _json["ui_sort_default"]["key"];
		if (!_json["ui_sort_default"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSortDefault = _json["ui_sort_default"]["text"];
		if (!_json["ui_sort_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSortUnlock_l10n_key = _json["ui_sort_unlock"]["key"];
		if (!_json["ui_sort_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSortUnlock = _json["ui_sort_unlock"]["text"];
		if (!_json["ui_lose_item_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiLoseItemTip_l10n_key = _json["ui_lose_item_tip"]["key"];
		if (!_json["ui_lose_item_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiLoseItemTip = _json["ui_lose_item_tip"]["text"];
		if (!_json["ui_lose_money_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiLoseMoneyTip_l10n_key = _json["ui_lose_money_tip"]["key"];
		if (!_json["ui_lose_money_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiLoseMoneyTip = _json["ui_lose_money_tip"]["text"];
		if (!_json["ui_unlock_seed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiUnlockSeed_l10n_key = _json["ui_unlock_seed"]["key"];
		if (!_json["ui_unlock_seed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiUnlockSeed = _json["ui_unlock_seed"]["text"];
		if (!_json["ui_analyzer_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerTime_l10n_key = _json["ui_analyzer_time"]["key"];
		if (!_json["ui_analyzer_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerTime = _json["ui_analyzer_time"]["text"];
		if (!_json["ui_analyzer_output"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerOutput_l10n_key = _json["ui_analyzer_output"]["key"];
		if (!_json["ui_analyzer_output"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerOutput = _json["ui_analyzer_output"]["text"];
		if (!_json["ui_analyzer_total_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerTotalTime_l10n_key = _json["ui_analyzer_total_time"]["key"];
		if (!_json["ui_analyzer_total_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnalyzerTotalTime = _json["ui_analyzer_total_time"]["text"];
		if (!_json["ui_shredder_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiShredderConfirm_l10n_key = _json["ui_shredder_confirm"]["key"];
		if (!_json["ui_shredder_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiShredderConfirm = _json["ui_shredder_confirm"]["text"];
		if (!_json["ui_shredder_confirm_upgrade"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiShredderConfirmUpgrade_l10n_key = _json["ui_shredder_confirm_upgrade"]["key"];
		if (!_json["ui_shredder_confirm_upgrade"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiShredderConfirmUpgrade = _json["ui_shredder_confirm_upgrade"]["text"];
		if (!_json["ui_submit_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirm_l10n_key = _json["ui_submit_confirm"]["key"];
		if (!_json["ui_submit_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirm = _json["ui_submit_confirm"]["text"];
		if (!_json["ui_submit_confirm_gene"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirmGene_l10n_key = _json["ui_submit_confirm_gene"]["key"];
		if (!_json["ui_submit_confirm_gene"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirmGene = _json["ui_submit_confirm_gene"]["text"];
		if (!_json["ui_submit_confirm_money"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirmMoney_l10n_key = _json["ui_submit_confirm_money"]["key"];
		if (!_json["ui_submit_confirm_money"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitConfirmMoney = _json["ui_submit_confirm_money"]["text"];
		if (!_json["ui_submit_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitNotEnough_l10n_key = _json["ui_submit_not_enough"]["key"];
		if (!_json["ui_submit_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSubmitNotEnough = _json["ui_submit_not_enough"]["text"];
		if (!_json["ui_gift_item_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiGiftItemConfirm_l10n_key = _json["ui_gift_item_confirm"]["key"];
		if (!_json["ui_gift_item_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiGiftItemConfirm = _json["ui_gift_item_confirm"]["text"];
		if (!_json["ui_tip_dismentle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDismentle_l10n_key = _json["ui_tip_dismentle"]["key"];
		if (!_json["ui_tip_dismentle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDismentle = _json["ui_tip_dismentle"]["text"];
		if (!_json["ui_tip_plant_err"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPlantErr_l10n_key = _json["ui_tip_plant_err"]["key"];
		if (!_json["ui_tip_plant_err"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPlantErr = _json["ui_tip_plant_err"]["text"];
		if (!_json["ui_text_years"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextYears_l10n_key = _json["ui_text_years"]["key"];
		if (!_json["ui_text_years"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextYears = _json["ui_text_years"]["text"];
		if (!_json["ui_text_months"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextMonths_l10n_key = _json["ui_text_months"]["key"];
		if (!_json["ui_text_months"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextMonths = _json["ui_text_months"]["text"];
		if (!_json["ui_text_days"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextDays_l10n_key = _json["ui_text_days"]["key"];
		if (!_json["ui_text_days"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextDays = _json["ui_text_days"]["text"];
		if (!_json["ui_text_hours"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextHours_l10n_key = _json["ui_text_hours"]["key"];
		if (!_json["ui_text_hours"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextHours = _json["ui_text_hours"]["text"];
		if (!_json["ui_text_minutes"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextMinutes_l10n_key = _json["ui_text_minutes"]["key"];
		if (!_json["ui_text_minutes"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextMinutes = _json["ui_text_minutes"]["text"];
		if (!_json["ui_text_time_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextTimeFormat_l10n_key = _json["ui_text_time_format"]["key"];
		if (!_json["ui_text_time_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextTimeFormat = _json["ui_text_time_format"]["text"];
		if (!_json["ui_text_demo_statement"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextDemoStatement_l10n_key = _json["ui_text_demo_statement"]["key"];
		if (!_json["ui_text_demo_statement"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextDemoStatement = _json["ui_text_demo_statement"]["text"];
		if (!_json["ui_text_gene_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTextGeneDescription_l10n_key = _json["ui_text_gene_description"]["key"];
		if (!_json["ui_text_gene_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTextGeneDescription = _json["ui_text_gene_description"]["text"];
		if (!_json["input_title_player_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputTitlePlayerName_l10n_key = _json["input_title_player_name"]["key"];
		if (!_json["input_title_player_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputTitlePlayerName = _json["input_title_player_name"]["text"];
		if (!_json["input_title_container_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputTitleContainerName_l10n_key = _json["input_title_container_name"]["key"];
		if (!_json["input_title_container_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputTitleContainerName = _json["input_title_container_name"]["text"];
		if (!_json["input_name_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputNameEmpty_l10n_key = _json["input_name_empty"]["key"];
		if (!_json["input_name_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputNameEmpty = _json["input_name_empty"]["text"];
		if (!_json["input_name_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputNameConfirm_l10n_key = _json["input_name_confirm"]["key"];
		if (!_json["input_name_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputNameConfirm = _json["input_name_confirm"]["text"];
		if (!_json["input_name_contains_sensitive_world"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputNameContainsSensitiveWorld_l10n_key = _json["input_name_contains_sensitive_world"]["key"];
		if (!_json["input_name_contains_sensitive_world"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputNameContainsSensitiveWorld = _json["input_name_contains_sensitive_world"]["text"];
		if (!_json["input_title_player_birthday"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputTitlePlayerBirthday_l10n_key = _json["input_title_player_birthday"]["key"];
		if (!_json["input_title_player_birthday"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputTitlePlayerBirthday = _json["input_title_player_birthday"]["text"];
		if (!_json["input_birthday_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputBirthdayConfirm_l10n_key = _json["input_birthday_confirm"]["key"];
		if (!_json["input_birthday_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputBirthdayConfirm = _json["input_birthday_confirm"]["text"];
		if (!_json["ui_savepoint_default"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointDefault_l10n_key = _json["ui_savepoint_default"]["key"];
		if (!_json["ui_savepoint_default"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointDefault = _json["ui_savepoint_default"]["text"];
		if (!_json["ui_savepoint_sleep"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointSleep_l10n_key = _json["ui_savepoint_sleep"]["key"];
		if (!_json["ui_savepoint_sleep"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointSleep = _json["ui_savepoint_sleep"]["text"];
		if (!_json["ui_savepoint_nap"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointNap_l10n_key = _json["ui_savepoint_nap"]["key"];
		if (!_json["ui_savepoint_nap"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointNap = _json["ui_savepoint_nap"]["text"];
		if (!_json["ui_savepoint_kill_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointKillTime_l10n_key = _json["ui_savepoint_kill_time"]["key"];
		if (!_json["ui_savepoint_kill_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointKillTime = _json["ui_savepoint_kill_time"]["text"];
		if (!_json["ui_savepoint_sit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointSit_l10n_key = _json["ui_savepoint_sit"]["key"];
		if (!_json["ui_savepoint_sit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointSit = _json["ui_savepoint_sit"]["text"];
		if (!_json["ui_savepoint_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointExit_l10n_key = _json["ui_savepoint_exit"]["key"];
		if (!_json["ui_savepoint_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointExit = _json["ui_savepoint_exit"]["text"];
		if (!_json["ui_savepoint_cancel"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointCancel_l10n_key = _json["ui_savepoint_cancel"]["key"];
		if (!_json["ui_savepoint_cancel"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSavepointCancel = _json["ui_savepoint_cancel"]["text"];
		if (!_json["ui_tip_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipConfirm_l10n_key = _json["ui_tip_confirm"]["key"];
		if (!_json["ui_tip_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipConfirm = _json["ui_tip_confirm"]["text"];
		if (!_json["ui_tip_cancel"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCancel_l10n_key = _json["ui_tip_cancel"]["key"];
		if (!_json["ui_tip_cancel"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCancel = _json["ui_tip_cancel"]["text"];
		if (!_json["ui_tip_quit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuit_l10n_key = _json["ui_tip_quit"]["key"];
		if (!_json["ui_tip_quit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuit = _json["ui_tip_quit"]["text"];
		if (!_json["ui_tip_switch_classifying"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchClassifying_l10n_key = _json["ui_tip_switch_classifying"]["key"];
		if (!_json["ui_tip_switch_classifying"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchClassifying = _json["ui_tip_switch_classifying"]["text"];
		if (!_json["ui_tip_sort"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSort_l10n_key = _json["ui_tip_sort"]["key"];
		if (!_json["ui_tip_sort"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSort = _json["ui_tip_sort"]["text"];
		if (!_json["ui_tip_upgrade"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUpgrade_l10n_key = _json["ui_tip_upgrade"]["key"];
		if (!_json["ui_tip_upgrade"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUpgrade = _json["ui_tip_upgrade"]["text"];
		if (!_json["ui_tip_buy"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuy_l10n_key = _json["ui_tip_buy"]["key"];
		if (!_json["ui_tip_buy"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuy = _json["ui_tip_buy"]["text"];
		if (!_json["ui_tip_sell_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellConfirm_l10n_key = _json["ui_tip_sell_confirm"]["key"];
		if (!_json["ui_tip_sell_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellConfirm = _json["ui_tip_sell_confirm"]["text"];
		if (!_json["ui_tip_sell"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSell_l10n_key = _json["ui_tip_sell"]["key"];
		if (!_json["ui_tip_sell"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSell = _json["ui_tip_sell"]["text"];
		if (!_json["ui_tip_buy_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyConfirm_l10n_key = _json["ui_tip_buy_confirm"]["key"];
		if (!_json["ui_tip_buy_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyConfirm = _json["ui_tip_buy_confirm"]["text"];
		if (!_json["ui_tip_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUnlock_l10n_key = _json["ui_tip_unlock"]["key"];
		if (!_json["ui_tip_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUnlock = _json["ui_tip_unlock"]["text"];
		if (!_json["ui_tip_unlock_seed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUnlockSeed_l10n_key = _json["ui_tip_unlock_seed"]["key"];
		if (!_json["ui_tip_unlock_seed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipUnlockSeed = _json["ui_tip_unlock_seed"]["text"];
		if (!_json["ui_tip_launch_drone"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLaunchDrone_l10n_key = _json["ui_tip_launch_drone"]["key"];
		if (!_json["ui_tip_launch_drone"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLaunchDrone = _json["ui_tip_launch_drone"]["text"];
		if (!_json["ui_tip_switch_automate_bot"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchAutomateBot_l10n_key = _json["ui_tip_switch_automate_bot"]["key"];
		if (!_json["ui_tip_switch_automate_bot"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchAutomateBot = _json["ui_tip_switch_automate_bot"]["text"];
		if (!_json["ui_tip_automate_bot_switch"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotSwitch_l10n_key = _json["ui_tip_automate_bot_switch"]["key"];
		if (!_json["ui_tip_automate_bot_switch"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotSwitch = _json["ui_tip_automate_bot_switch"]["text"];
		if (!_json["ui_tip_automate_bot_unload"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotUnload_l10n_key = _json["ui_tip_automate_bot_unload"]["key"];
		if (!_json["ui_tip_automate_bot_unload"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotUnload = _json["ui_tip_automate_bot_unload"]["text"];
		if (!_json["ui_tip_automate_bot_load"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotLoad_l10n_key = _json["ui_tip_automate_bot_load"]["key"];
		if (!_json["ui_tip_automate_bot_load"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAutomateBotLoad = _json["ui_tip_automate_bot_load"]["text"];
		if (!_json["ui_tip_choice"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChoice_l10n_key = _json["ui_tip_choice"]["key"];
		if (!_json["ui_tip_choice"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChoice = _json["ui_tip_choice"]["text"];
		if (!_json["ui_tip_submit_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubmitAll_l10n_key = _json["ui_tip_submit_all"]["key"];
		if (!_json["ui_tip_submit_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubmitAll = _json["ui_tip_submit_all"]["text"];
		if (!_json["ui_tip_submit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubmit_l10n_key = _json["ui_tip_submit"]["key"];
		if (!_json["ui_tip_submit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubmit = _json["ui_tip_submit"]["text"];
		if (!_json["ui_tip_load_game"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLoadGame_l10n_key = _json["ui_tip_load_game"]["key"];
		if (!_json["ui_tip_load_game"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLoadGame = _json["ui_tip_load_game"]["text"];
		if (!_json["ui_tip_del"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDel_l10n_key = _json["ui_tip_del"]["key"];
		if (!_json["ui_tip_del"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDel = _json["ui_tip_del"]["text"];
		if (!_json["ui_tip_lock_slot"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLockSlot_l10n_key = _json["ui_tip_lock_slot"]["key"];
		if (!_json["ui_tip_lock_slot"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLockSlot = _json["ui_tip_lock_slot"]["text"];
		if (!_json["ui_tip_tidy"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTidy_l10n_key = _json["ui_tip_tidy"]["key"];
		if (!_json["ui_tip_tidy"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTidy = _json["ui_tip_tidy"]["text"];
		if (!_json["ui_tip_dispose"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDispose_l10n_key = _json["ui_tip_dispose"]["key"];
		if (!_json["ui_tip_dispose"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDispose = _json["ui_tip_dispose"]["text"];
		if (!_json["ui_tip_destroy_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDestroyItem_l10n_key = _json["ui_tip_destroy_item"]["key"];
		if (!_json["ui_tip_destroy_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDestroyItem = _json["ui_tip_destroy_item"]["text"];
		if (!_json["ui_tip_switch_lable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchLable_l10n_key = _json["ui_tip_switch_lable"]["key"];
		if (!_json["ui_tip_switch_lable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchLable = _json["ui_tip_switch_lable"]["text"];
		if (!_json["ui_tip_put_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAll_l10n_key = _json["ui_tip_put_all"]["key"];
		if (!_json["ui_tip_put_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAll = _json["ui_tip_put_all"]["text"];
		if (!_json["ui_tip_put_max"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutMax_l10n_key = _json["ui_tip_put_max"]["key"];
		if (!_json["ui_tip_put_max"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutMax = _json["ui_tip_put_max"]["text"];
		if (!_json["ui_tip_take_out_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutAll_l10n_key = _json["ui_tip_take_out_all"]["key"];
		if (!_json["ui_tip_take_out_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutAll = _json["ui_tip_take_out_all"]["text"];
		if (!_json["ui_tip_take_out_max"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutMax_l10n_key = _json["ui_tip_take_out_max"]["key"];
		if (!_json["ui_tip_take_out_max"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutMax = _json["ui_tip_take_out_max"]["text"];
		if (!_json["ui_tip_put_all_tap"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAllTap_l10n_key = _json["ui_tip_put_all_tap"]["key"];
		if (!_json["ui_tip_put_all_tap"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAllTap = _json["ui_tip_put_all_tap"]["text"];
		if (!_json["ui_tip_take_out_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutOne_l10n_key = _json["ui_tip_take_out_one"]["key"];
		if (!_json["ui_tip_take_out_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutOne = _json["ui_tip_take_out_one"]["text"];
		if (!_json["ui_tip_take_out_half"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutHalf_l10n_key = _json["ui_tip_take_out_half"]["key"];
		if (!_json["ui_tip_take_out_half"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutHalf = _json["ui_tip_take_out_half"]["text"];
		if (!_json["ui_tip_take_out_one_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutOneGamepad_l10n_key = _json["ui_tip_take_out_one_gamepad"]["key"];
		if (!_json["ui_tip_take_out_one_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutOneGamepad = _json["ui_tip_take_out_one_gamepad"]["text"];
		if (!_json["ui_tip_put"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPut_l10n_key = _json["ui_tip_put"]["key"];
		if (!_json["ui_tip_put"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPut = _json["ui_tip_put"]["text"];
		if (!_json["ui_tip_take_out"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOut_l10n_key = _json["ui_tip_take_out"]["key"];
		if (!_json["ui_tip_take_out"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOut = _json["ui_tip_take_out"]["text"];
		if (!_json["ui_tip_put_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutGamepad_l10n_key = _json["ui_tip_put_gamepad"]["key"];
		if (!_json["ui_tip_put_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutGamepad = _json["ui_tip_put_gamepad"]["text"];
		if (!_json["ui_tip_take_out_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutGamepad_l10n_key = _json["ui_tip_take_out_gamepad"]["key"];
		if (!_json["ui_tip_take_out_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutGamepad = _json["ui_tip_take_out_gamepad"]["text"];
		if (!_json["ui_tip_pick_up_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPickUpGamepad_l10n_key = _json["ui_tip_pick_up_gamepad"]["key"];
		if (!_json["ui_tip_pick_up_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPickUpGamepad = _json["ui_tip_pick_up_gamepad"]["text"];
		if (!_json["ui_tip_quick_put_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickPutOne_l10n_key = _json["ui_tip_quick_put_one"]["key"];
		if (!_json["ui_tip_quick_put_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickPutOne = _json["ui_tip_quick_put_one"]["text"];
		if (!_json["ui_tip_quick_put_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickPutAll_l10n_key = _json["ui_tip_quick_put_all"]["key"];
		if (!_json["ui_tip_quick_put_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickPutAll = _json["ui_tip_quick_put_all"]["text"];
		if (!_json["ui_tip_quick_take_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickTakeOne_l10n_key = _json["ui_tip_quick_take_one"]["key"];
		if (!_json["ui_tip_quick_take_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickTakeOne = _json["ui_tip_quick_take_one"]["text"];
		if (!_json["ui_tip_quick_take_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickTakeAll_l10n_key = _json["ui_tip_quick_take_all"]["key"];
		if (!_json["ui_tip_quick_take_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickTakeAll = _json["ui_tip_quick_take_all"]["text"];
		if (!_json["ui_tip_quick_equipment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickEquipment_l10n_key = _json["ui_tip_quick_equipment"]["key"];
		if (!_json["ui_tip_quick_equipment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipQuickEquipment = _json["ui_tip_quick_equipment"]["text"];
		if (!_json["ui_tip_switch_box"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchBox_l10n_key = _json["ui_tip_switch_box"]["key"];
		if (!_json["ui_tip_switch_box"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchBox = _json["ui_tip_switch_box"]["text"];
		if (!_json["ui_tip_sub_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubOne_l10n_key = _json["ui_tip_sub_one"]["key"];
		if (!_json["ui_tip_sub_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubOne = _json["ui_tip_sub_one"]["text"];
		if (!_json["ui_tip_add_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddOne_l10n_key = _json["ui_tip_add_one"]["key"];
		if (!_json["ui_tip_add_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddOne = _json["ui_tip_add_one"]["text"];
		if (!_json["ui_tip_sub_ten"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubTen_l10n_key = _json["ui_tip_sub_ten"]["key"];
		if (!_json["ui_tip_sub_ten"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSubTen = _json["ui_tip_sub_ten"]["text"];
		if (!_json["ui_tip_add_ten"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddTen_l10n_key = _json["ui_tip_add_ten"]["key"];
		if (!_json["ui_tip_add_ten"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddTen = _json["ui_tip_add_ten"]["text"];
		if (!_json["ui_tip_set_min"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSetMin_l10n_key = _json["ui_tip_set_min"]["key"];
		if (!_json["ui_tip_set_min"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSetMin = _json["ui_tip_set_min"]["text"];
		if (!_json["ui_tip_set_max"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSetMax_l10n_key = _json["ui_tip_set_max"]["key"];
		if (!_json["ui_tip_set_max"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSetMax = _json["ui_tip_set_max"]["text"];
		if (!_json["ui_tip_broadcast"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBroadcast_l10n_key = _json["ui_tip_broadcast"]["key"];
		if (!_json["ui_tip_broadcast"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBroadcast = _json["ui_tip_broadcast"]["text"];
		if (!_json["ui_tip_make_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMakeOne_l10n_key = _json["ui_tip_make_one"]["key"];
		if (!_json["ui_tip_make_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMakeOne = _json["ui_tip_make_one"]["text"];
		if (!_json["ui_tip_make_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMakeAll_l10n_key = _json["ui_tip_make_all"]["key"];
		if (!_json["ui_tip_make_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMakeAll = _json["ui_tip_make_all"]["text"];
		if (!_json["ui_tip_buy_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyOne_l10n_key = _json["ui_tip_buy_one"]["key"];
		if (!_json["ui_tip_buy_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyOne = _json["ui_tip_buy_one"]["text"];
		if (!_json["ui_tip_buy_all_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyAllGamepad_l10n_key = _json["ui_tip_buy_all_gamepad"]["key"];
		if (!_json["ui_tip_buy_all_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyAllGamepad = _json["ui_tip_buy_all_gamepad"]["text"];
		if (!_json["ui_tip_sell_one"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellOne_l10n_key = _json["ui_tip_sell_one"]["key"];
		if (!_json["ui_tip_sell_one"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellOne = _json["ui_tip_sell_one"]["text"];
		if (!_json["ui_tip_sell_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellAll_l10n_key = _json["ui_tip_sell_all"]["key"];
		if (!_json["ui_tip_sell_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSellAll = _json["ui_tip_sell_all"]["text"];
		if (!_json["ui_tip_prev_filter"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPrevFilter_l10n_key = _json["ui_tip_prev_filter"]["key"];
		if (!_json["ui_tip_prev_filter"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPrevFilter = _json["ui_tip_prev_filter"]["text"];
		if (!_json["ui_tip_next_filter"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNextFilter_l10n_key = _json["ui_tip_next_filter"]["key"];
		if (!_json["ui_tip_next_filter"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNextFilter = _json["ui_tip_next_filter"]["text"];
		if (!_json["ui_tip_capture"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCapture_l10n_key = _json["ui_tip_capture"]["key"];
		if (!_json["ui_tip_capture"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCapture = _json["ui_tip_capture"]["text"];
		if (!_json["ui_tip_collection"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCollection_l10n_key = _json["ui_tip_collection"]["key"];
		if (!_json["ui_tip_collection"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCollection = _json["ui_tip_collection"]["text"];
		if (!_json["ui_tip_history"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHistory_l10n_key = _json["ui_tip_history"]["key"];
		if (!_json["ui_tip_history"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHistory = _json["ui_tip_history"]["text"];
		if (!_json["ui_tip_change_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChangeName_l10n_key = _json["ui_tip_change_name"]["key"];
		if (!_json["ui_tip_change_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChangeName = _json["ui_tip_change_name"]["text"];
		if (!_json["ui_tip_take_out_selected"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutSelected_l10n_key = _json["ui_tip_take_out_selected"]["key"];
		if (!_json["ui_tip_take_out_selected"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTakeOutSelected = _json["ui_tip_take_out_selected"]["text"];
		if (!_json["ui_tip_put_one_selected"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutOneSelected_l10n_key = _json["ui_tip_put_one_selected"]["key"];
		if (!_json["ui_tip_put_one_selected"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutOneSelected = _json["ui_tip_put_one_selected"]["text"];
		if (!_json["ui_tip_put_all_selected"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAllSelected_l10n_key = _json["ui_tip_put_all_selected"]["key"];
		if (!_json["ui_tip_put_all_selected"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPutAllSelected = _json["ui_tip_put_all_selected"]["text"];
		if (!_json["ui_tip_add_memo"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddMemo_l10n_key = _json["ui_tip_add_memo"]["key"];
		if (!_json["ui_tip_add_memo"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAddMemo = _json["ui_tip_add_memo"]["text"];
		if (!_json["ui_tip_change_memo"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChangeMemo_l10n_key = _json["ui_tip_change_memo"]["key"];
		if (!_json["ui_tip_change_memo"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipChangeMemo = _json["ui_tip_change_memo"]["text"];
		if (!_json["ui_tip_del_memo"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDelMemo_l10n_key = _json["ui_tip_del_memo"]["key"];
		if (!_json["ui_tip_del_memo"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDelMemo = _json["ui_tip_del_memo"]["text"];
		if (!_json["ui_tip_switch_map_size"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchMapSize_l10n_key = _json["ui_tip_switch_map_size"]["key"];
		if (!_json["ui_tip_switch_map_size"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSwitchMapSize = _json["ui_tip_switch_map_size"]["text"];
		if (!_json["ui_tip_map_centered"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMapCentered_l10n_key = _json["ui_tip_map_centered"]["key"];
		if (!_json["ui_tip_map_centered"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMapCentered = _json["ui_tip_map_centered"]["text"];
		if (!_json["ui_tip_email_recycle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmailRecycle_l10n_key = _json["ui_tip_email_recycle"]["key"];
		if (!_json["ui_tip_email_recycle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmailRecycle = _json["ui_tip_email_recycle"]["text"];
		if (!_json["ui_tip_email_default"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmailDefault_l10n_key = _json["ui_tip_email_default"]["key"];
		if (!_json["ui_tip_email_default"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmailDefault = _json["ui_tip_email_default"]["text"];
		if (!_json["ui_tip_techtree_point_focus"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTechtreePointFocus_l10n_key = _json["ui_tip_techtree_point_focus"]["key"];
		if (!_json["ui_tip_techtree_point_focus"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTechtreePointFocus = _json["ui_tip_techtree_point_focus"]["text"];
		if (!_json["ui_tip_techtree_focus"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTechtreeFocus_l10n_key = _json["ui_tip_techtree_focus"]["key"];
		if (!_json["ui_tip_techtree_focus"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTechtreeFocus = _json["ui_tip_techtree_focus"]["text"];
		if (!_json["ui_tip_battery_low"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBatteryLow_l10n_key = _json["ui_tip_battery_low"]["key"];
		if (!_json["ui_tip_battery_low"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBatteryLow = _json["ui_tip_battery_low"]["text"];
		if (!_json["ui_tip_err_take_boat"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipErrTakeBoat_l10n_key = _json["ui_tip_err_take_boat"]["key"];
		if (!_json["ui_tip_err_take_boat"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipErrTakeBoat = _json["ui_tip_err_take_boat"]["text"];
		if (!_json["ui_operation_talk"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTalk_l10n_key = _json["ui_operation_talk"]["key"];
		if (!_json["ui_operation_talk"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTalk = _json["ui_operation_talk"]["text"];
		if (!_json["ui_operation_talk_unknown"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTalkUnknown_l10n_key = _json["ui_operation_talk_unknown"]["key"];
		if (!_json["ui_operation_talk_unknown"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTalkUnknown = _json["ui_operation_talk_unknown"]["text"];
		if (!_json["ui_operation_interact"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationInteract_l10n_key = _json["ui_operation_interact"]["key"];
		if (!_json["ui_operation_interact"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationInteract = _json["ui_operation_interact"]["text"];
		if (!_json["ui_operation_sleep"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSleep_l10n_key = _json["ui_operation_sleep"]["key"];
		if (!_json["ui_operation_sleep"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSleep = _json["ui_operation_sleep"]["text"];
		if (!_json["ui_operation_pick"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationPick_l10n_key = _json["ui_operation_pick"]["key"];
		if (!_json["ui_operation_pick"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationPick = _json["ui_operation_pick"]["text"];
		if (!_json["ui_operation_well"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationWell_l10n_key = _json["ui_operation_well"]["key"];
		if (!_json["ui_operation_well"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationWell = _json["ui_operation_well"]["text"];
		if (!_json["ui_operation_harvest"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationHarvest_l10n_key = _json["ui_operation_harvest"]["key"];
		if (!_json["ui_operation_harvest"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationHarvest = _json["ui_operation_harvest"]["text"];
		if (!_json["ui_operation_clear"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationClear_l10n_key = _json["ui_operation_clear"]["key"];
		if (!_json["ui_operation_clear"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationClear = _json["ui_operation_clear"]["text"];
		if (!_json["ui_operation_open"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpen_l10n_key = _json["ui_operation_open"]["key"];
		if (!_json["ui_operation_open"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpen = _json["ui_operation_open"]["text"];
		if (!_json["ui_operation_open_door"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpenDoor_l10n_key = _json["ui_operation_open_door"]["key"];
		if (!_json["ui_operation_open_door"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpenDoor = _json["ui_operation_open_door"]["text"];
		if (!_json["ui_operation_close_door"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCloseDoor_l10n_key = _json["ui_operation_close_door"]["key"];
		if (!_json["ui_operation_close_door"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCloseDoor = _json["ui_operation_close_door"]["text"];
		if (!_json["ui_operation_enter"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationEnter_l10n_key = _json["ui_operation_enter"]["key"];
		if (!_json["ui_operation_enter"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationEnter = _json["ui_operation_enter"]["text"];
		if (!_json["ui_operation_enter_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationEnterFormat_l10n_key = _json["ui_operation_enter_format"]["key"];
		if (!_json["ui_operation_enter_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationEnterFormat = _json["ui_operation_enter_format"]["text"];
		if (!_json["ui_operation_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationExit_l10n_key = _json["ui_operation_exit"]["key"];
		if (!_json["ui_operation_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationExit = _json["ui_operation_exit"]["text"];
		if (!_json["ui_operation_view"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationView_l10n_key = _json["ui_operation_view"]["key"];
		if (!_json["ui_operation_view"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationView = _json["ui_operation_view"]["text"];
		if (!_json["ui_operation_use"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationUse_l10n_key = _json["ui_operation_use"]["key"];
		if (!_json["ui_operation_use"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationUse = _json["ui_operation_use"]["text"];
		if (!_json["ui_operation_sit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSit_l10n_key = _json["ui_operation_sit"]["key"];
		if (!_json["ui_operation_sit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSit = _json["ui_operation_sit"]["text"];
		if (!_json["ui_operation_display"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDisplay_l10n_key = _json["ui_operation_display"]["key"];
		if (!_json["ui_operation_display"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDisplay = _json["ui_operation_display"]["text"];
		if (!_json["ui_operation_takeoff"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTakeoff_l10n_key = _json["ui_operation_takeoff"]["key"];
		if (!_json["ui_operation_takeoff"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationTakeoff = _json["ui_operation_takeoff"]["text"];
		if (!_json["ui_operation_dump"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDump_l10n_key = _json["ui_operation_dump"]["key"];
		if (!_json["ui_operation_dump"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDump = _json["ui_operation_dump"]["text"];
		if (!_json["ui_operation_disembark"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDisembark_l10n_key = _json["ui_operation_disembark"]["key"];
		if (!_json["ui_operation_disembark"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationDisembark = _json["ui_operation_disembark"]["text"];
		if (!_json["ui_operation_call_boat"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCallBoat_l10n_key = _json["ui_operation_call_boat"]["key"];
		if (!_json["ui_operation_call_boat"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCallBoat = _json["ui_operation_call_boat"]["text"];
		if (!_json["ui_operation_shower"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationShower_l10n_key = _json["ui_operation_shower"]["key"];
		if (!_json["ui_operation_shower"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationShower = _json["ui_operation_shower"]["text"];
		if (!_json["ui_operation_open_box"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpenBox_l10n_key = _json["ui_operation_open_box"]["key"];
		if (!_json["ui_operation_open_box"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationOpenBox = _json["ui_operation_open_box"]["text"];
		if (!_json["ui_operation_storage_shelf"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStorageShelf_l10n_key = _json["ui_operation_storage_shelf"]["key"];
		if (!_json["ui_operation_storage_shelf"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStorageShelf = _json["ui_operation_storage_shelf"]["text"];
		if (!_json["ui_operation_fuel_in"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFuelIn_l10n_key = _json["ui_operation_fuel_in"]["key"];
		if (!_json["ui_operation_fuel_in"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFuelIn = _json["ui_operation_fuel_in"]["text"];
		if (!_json["ui_operation_close"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationClose_l10n_key = _json["ui_operation_close"]["key"];
		if (!_json["ui_operation_close"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationClose = _json["ui_operation_close"]["text"];
		if (!_json["ui_operation_start"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStart_l10n_key = _json["ui_operation_start"]["key"];
		if (!_json["ui_operation_start"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStart = _json["ui_operation_start"]["text"];
		if (!_json["ui_operation_fill"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFill_l10n_key = _json["ui_operation_fill"]["key"];
		if (!_json["ui_operation_fill"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFill = _json["ui_operation_fill"]["text"];
		if (!_json["ui_operation_fondle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFondle_l10n_key = _json["ui_operation_fondle"]["key"];
		if (!_json["ui_operation_fondle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationFondle = _json["ui_operation_fondle"]["text"];
		if (!_json["ui_operation_start_something"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStartSomething_l10n_key = _json["ui_operation_start_something"]["key"];
		if (!_json["ui_operation_start_something"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationStartSomething = _json["ui_operation_start_something"]["text"];
		if (!_json["ui_operation_use_something"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationUseSomething_l10n_key = _json["ui_operation_use_something"]["key"];
		if (!_json["ui_operation_use_something"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationUseSomething = _json["ui_operation_use_something"]["text"];
		if (!_json["ui_operation_err_not_seed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNotSeed_l10n_key = _json["ui_operation_err_not_seed"]["key"];
		if (!_json["ui_operation_err_not_seed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNotSeed = _json["ui_operation_err_not_seed"]["text"];
		if (!_json["ui_operation_err_invalid_plantbasin"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidPlantbasin_l10n_key = _json["ui_operation_err_invalid_plantbasin"]["key"];
		if (!_json["ui_operation_err_invalid_plantbasin"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidPlantbasin = _json["ui_operation_err_invalid_plantbasin"]["text"];
		if (!_json["ui_operation_err_invalid_season"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidSeason_l10n_key = _json["ui_operation_err_invalid_season"]["key"];
		if (!_json["ui_operation_err_invalid_season"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidSeason = _json["ui_operation_err_invalid_season"]["text"];
		if (!_json["ui_operation_err_lack_of_asset"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLackOfAsset_l10n_key = _json["ui_operation_err_lack_of_asset"]["key"];
		if (!_json["ui_operation_err_lack_of_asset"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLackOfAsset = _json["ui_operation_err_lack_of_asset"]["text"];
		if (!_json["ui_operation_err_lack_of_energy"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLackOfEnergy_l10n_key = _json["ui_operation_err_lack_of_energy"]["key"];
		if (!_json["ui_operation_err_lack_of_energy"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLackOfEnergy = _json["ui_operation_err_lack_of_energy"]["text"];
		if (!_json["ui_operation_err_cannot_plant_tree"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotPlantTree_l10n_key = _json["ui_operation_err_cannot_plant_tree"]["key"];
		if (!_json["ui_operation_err_cannot_plant_tree"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotPlantTree = _json["ui_operation_err_cannot_plant_tree"]["text"];
		if (!_json["ui_operation_err_cannot_plant_on_ground"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotPlantOnGround_l10n_key = _json["ui_operation_err_cannot_plant_on_ground"]["key"];
		if (!_json["ui_operation_err_cannot_plant_on_ground"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotPlantOnGround = _json["ui_operation_err_cannot_plant_on_ground"]["text"];
		if (!_json["ui_operation_err_empty_drone"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrEmptyDrone_l10n_key = _json["ui_operation_err_empty_drone"]["key"];
		if (!_json["ui_operation_err_empty_drone"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrEmptyDrone = _json["ui_operation_err_empty_drone"]["text"];
		if (!_json["ui_operation_err_well_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWellFull_l10n_key = _json["ui_operation_err_well_full"]["key"];
		if (!_json["ui_operation_err_well_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWellFull = _json["ui_operation_err_well_full"]["text"];
		if (!_json["ui_operation_err_water_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWaterFull_l10n_key = _json["ui_operation_err_water_full"]["key"];
		if (!_json["ui_operation_err_water_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWaterFull = _json["ui_operation_err_water_full"]["text"];
		if (!_json["ui_operation_err_runout_water"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrRunoutWater_l10n_key = _json["ui_operation_err_runout_water"]["key"];
		if (!_json["ui_operation_err_runout_water"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrRunoutWater = _json["ui_operation_err_runout_water"]["text"];
		if (!_json["ui_operation_err_runout_water_around"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrRunoutWaterAround_l10n_key = _json["ui_operation_err_runout_water_around"]["key"];
		if (!_json["ui_operation_err_runout_water_around"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrRunoutWaterAround = _json["ui_operation_err_runout_water_around"]["text"];
		if (!_json["ui_operation_err_equipment_corroded"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrEquipmentCorroded_l10n_key = _json["ui_operation_err_equipment_corroded"]["key"];
		if (!_json["ui_operation_err_equipment_corroded"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrEquipmentCorroded = _json["ui_operation_err_equipment_corroded"]["text"];
		if (!_json["ui_operation_err_low_tool_level"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLowToolLevel_l10n_key = _json["ui_operation_err_low_tool_level"]["key"];
		if (!_json["ui_operation_err_low_tool_level"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLowToolLevel = _json["ui_operation_err_low_tool_level"]["text"];
		if (!_json["ui_operation_err_full_plastic_film"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFullPlasticFilm_l10n_key = _json["ui_operation_err_full_plastic_film"]["key"];
		if (!_json["ui_operation_err_full_plastic_film"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFullPlasticFilm = _json["ui_operation_err_full_plastic_film"]["text"];
		if (!_json["ui_operation_err_cannot_fertilizer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotFertilizer_l10n_key = _json["ui_operation_err_cannot_fertilizer"]["key"];
		if (!_json["ui_operation_err_cannot_fertilizer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotFertilizer = _json["ui_operation_err_cannot_fertilizer"]["text"];
		if (!_json["ui_operation_err_drone_full_battery"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrDroneFullBattery_l10n_key = _json["ui_operation_err_drone_full_battery"]["key"];
		if (!_json["ui_operation_err_drone_full_battery"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrDroneFullBattery = _json["ui_operation_err_drone_full_battery"]["text"];
		if (!_json["ui_operation_err_water_evaporated"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWaterEvaporated_l10n_key = _json["ui_operation_err_water_evaporated"]["key"];
		if (!_json["ui_operation_err_water_evaporated"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrWaterEvaporated = _json["ui_operation_err_water_evaporated"]["text"];
		if (!_json["ui_operation_err_fail_to_place_box"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFailToPlaceBox_l10n_key = _json["ui_operation_err_fail_to_place_box"]["key"];
		if (!_json["ui_operation_err_fail_to_place_box"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFailToPlaceBox = _json["ui_operation_err_fail_to_place_box"]["text"];
		if (!_json["ui_operation_err_cannot_display"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotDisplay_l10n_key = _json["ui_operation_err_cannot_display"]["key"];
		if (!_json["ui_operation_err_cannot_display"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotDisplay = _json["ui_operation_err_cannot_display"]["text"];
		if (!_json["ui_operation_err_cannot_use_if_riding"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotUseIfRiding_l10n_key = _json["ui_operation_err_cannot_use_if_riding"]["key"];
		if (!_json["ui_operation_err_cannot_use_if_riding"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotUseIfRiding = _json["ui_operation_err_cannot_use_if_riding"]["text"];
		if (!_json["ui_operation_err_cannot_call_motor"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotCallMotor_l10n_key = _json["ui_operation_err_cannot_call_motor"]["key"];
		if (!_json["ui_operation_err_cannot_call_motor"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCannotCallMotor = _json["ui_operation_err_cannot_call_motor"]["text"];
		if (!_json["ui_operation_err_sword_cannot_autofire"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSwordCannotAutofire_l10n_key = _json["ui_operation_err_sword_cannot_autofire"]["key"];
		if (!_json["ui_operation_err_sword_cannot_autofire"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSwordCannotAutofire = _json["ui_operation_err_sword_cannot_autofire"]["text"];
		if (!_json["ui_operation_err_low_power"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLowPower_l10n_key = _json["ui_operation_err_low_power"]["key"];
		if (!_json["ui_operation_err_low_power"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrLowPower = _json["ui_operation_err_low_power"]["text"];
		if (!_json["ui_operation_err_cooling"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCooling_l10n_key = _json["ui_operation_err_cooling"]["key"];
		if (!_json["ui_operation_err_cooling"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrCooling = _json["ui_operation_err_cooling"]["text"];
		if (!_json["ui_operation_err_barrel_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrBarrelFull_l10n_key = _json["ui_operation_err_barrel_full"]["key"];
		if (!_json["ui_operation_err_barrel_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrBarrelFull = _json["ui_operation_err_barrel_full"]["text"];
		if (!_json["ui_operation_err_fuel_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFuelFull_l10n_key = _json["ui_operation_err_fuel_full"]["key"];
		if (!_json["ui_operation_err_fuel_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFuelFull = _json["ui_operation_err_fuel_full"]["text"];
		if (!_json["ui_operation_err_not_feeds"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNotFeeds_l10n_key = _json["ui_operation_err_not_feeds"]["key"];
		if (!_json["ui_operation_err_not_feeds"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNotFeeds = _json["ui_operation_err_not_feeds"]["text"];
		if (!_json["ui_operation_err_feeder_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFeederFull_l10n_key = _json["ui_operation_err_feeder_full"]["key"];
		if (!_json["ui_operation_err_feeder_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFeederFull = _json["ui_operation_err_feeder_full"]["text"];
		if (!_json["ui_operation_err_slot_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSlotFull_l10n_key = _json["ui_operation_err_slot_full"]["key"];
		if (!_json["ui_operation_err_slot_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSlotFull = _json["ui_operation_err_slot_full"]["text"];
		if (!_json["ui_operation_err_no_suitable_equipments"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoSuitableEquipments_l10n_key = _json["ui_operation_err_no_suitable_equipments"]["key"];
		if (!_json["ui_operation_err_no_suitable_equipments"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoSuitableEquipments = _json["ui_operation_err_no_suitable_equipments"]["text"];
		if (!_json["ui_storage_shelf_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiStorageShelfFull_l10n_key = _json["ui_storage_shelf_full"]["key"];
		if (!_json["ui_storage_shelf_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiStorageShelfFull = _json["ui_storage_shelf_full"]["text"];
		if (!_json["ui_operation_weather_has_changed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationWeatherHasChanged_l10n_key = _json["ui_operation_weather_has_changed"]["key"];
		if (!_json["ui_operation_weather_has_changed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationWeatherHasChanged = _json["ui_operation_weather_has_changed"]["text"];
		if (!_json["ui_operation_err_fuel"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFuel_l10n_key = _json["ui_operation_err_fuel"]["key"];
		if (!_json["ui_operation_err_fuel"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrFuel = _json["ui_operation_err_fuel"]["text"];
		if (!_json["ui_operation_gift_item_fail"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationGiftItemFail_l10n_key = _json["ui_operation_gift_item_fail"]["key"];
		if (!_json["ui_operation_gift_item_fail"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationGiftItemFail = _json["ui_operation_gift_item_fail"]["text"];
		if (!_json["ui_operation_make_equipment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationMakeEquipment_l10n_key = _json["ui_operation_make_equipment"]["key"];
		if (!_json["ui_operation_make_equipment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationMakeEquipment = _json["ui_operation_make_equipment"]["text"];
		if (!_json["ui_operation_sell"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSell_l10n_key = _json["ui_operation_sell"]["key"];
		if (!_json["ui_operation_sell"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSell = _json["ui_operation_sell"]["text"];
		if (!_json["ui_operation_switch_auto_fire"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSwitchAutoFire_l10n_key = _json["ui_operation_switch_auto_fire"]["key"];
		if (!_json["ui_operation_switch_auto_fire"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSwitchAutoFire = _json["ui_operation_switch_auto_fire"]["text"];
		if (!_json["ui_operation_switch_manual_fire"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSwitchManualFire_l10n_key = _json["ui_operation_switch_manual_fire"]["key"];
		if (!_json["ui_operation_switch_manual_fire"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationSwitchManualFire = _json["ui_operation_switch_manual_fire"]["text"];
		if (!_json["ui_operation_collect"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCollect_l10n_key = _json["ui_operation_collect"]["key"];
		if (!_json["ui_operation_collect"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationCollect = _json["ui_operation_collect"]["text"];
		if (!_json["ui_operation_ride"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationRide_l10n_key = _json["ui_operation_ride"]["key"];
		if (!_json["ui_operation_ride"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationRide = _json["ui_operation_ride"]["text"];
		if (!_json["ui_operation_no_animal_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationNoAnimalBuilding_l10n_key = _json["ui_operation_no_animal_building"]["key"];
		if (!_json["ui_operation_no_animal_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationNoAnimalBuilding = _json["ui_operation_no_animal_building"]["text"];
		if (!_json["ui_operation_err_no_plant_basin"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoPlantBasin_l10n_key = _json["ui_operation_err_no_plant_basin"]["key"];
		if (!_json["ui_operation_err_no_plant_basin"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoPlantBasin = _json["ui_operation_err_no_plant_basin"]["text"];
		if (!_json["ui_operation_err_no_building_here"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoBuildingHere_l10n_key = _json["ui_operation_err_no_building_here"]["key"];
		if (!_json["ui_operation_err_no_building_here"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrNoBuildingHere = _json["ui_operation_err_no_building_here"]["text"];
		if (!_json["ui_operation_err_invalid_wallpaper"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidWallpaper_l10n_key = _json["ui_operation_err_invalid_wallpaper"]["key"];
		if (!_json["ui_operation_err_invalid_wallpaper"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrInvalidWallpaper = _json["ui_operation_err_invalid_wallpaper"]["text"];
		if (!_json["ui_operation_err_same_wallpaper"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSameWallpaper_l10n_key = _json["ui_operation_err_same_wallpaper"]["key"];
		if (!_json["ui_operation_err_same_wallpaper"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrSameWallpaper = _json["ui_operation_err_same_wallpaper"]["text"];
		if (!_json["ui_operation_err_should_inhouse"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrShouldInhouse_l10n_key = _json["ui_operation_err_should_inhouse"]["key"];
		if (!_json["ui_operation_err_should_inhouse"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrShouldInhouse = _json["ui_operation_err_should_inhouse"]["text"];
		if (!_json["ui_operation_err_should_outside"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrShouldOutside_l10n_key = _json["ui_operation_err_should_outside"]["key"];
		if (!_json["ui_operation_err_should_outside"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOperationErrShouldOutside = _json["ui_operation_err_should_outside"]["text"];
		if (!_json["store_player_money_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StorePlayerMoneyNotEnough_l10n_key = _json["store_player_money_not_enough"]["key"];
		if (!_json["store_player_money_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StorePlayerMoneyNotEnough = _json["store_player_money_not_enough"]["text"];
		if (!_json["store_store_money_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreStoreMoneyNotEnough_l10n_key = _json["store_store_money_not_enough"]["key"];
		if (!_json["store_store_money_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreStoreMoneyNotEnough = _json["store_store_money_not_enough"]["text"];
		if (!_json["store_item_not_saleable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemNotSaleable_l10n_key = _json["store_item_not_saleable"]["key"];
		if (!_json["store_item_not_saleable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemNotSaleable = _json["store_item_not_saleable"]["text"];
		if (!_json["store_item_sold_out"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemSoldOut_l10n_key = _json["store_item_sold_out"]["key"];
		if (!_json["store_item_sold_out"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemSoldOut = _json["store_item_sold_out"]["text"];
		if (!_json["store_lack_of_asset"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreLackOfAsset_l10n_key = _json["store_lack_of_asset"]["key"];
		if (!_json["store_lack_of_asset"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreLackOfAsset = _json["store_lack_of_asset"]["text"];
		if (!_json["store_upgrade_price"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradePrice_l10n_key = _json["store_upgrade_price"]["key"];
		if (!_json["store_upgrade_price"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradePrice = _json["store_upgrade_price"]["text"];
		if (!_json["store_upgrade_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradeConfirm_l10n_key = _json["store_upgrade_confirm"]["key"];
		if (!_json["store_upgrade_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradeConfirm = _json["store_upgrade_confirm"]["text"];
		if (!_json["store_sold_out_icon"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreSoldOutIcon_l10n_key = _json["store_sold_out_icon"]["key"];
		if (!_json["store_sold_out_icon"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreSoldOutIcon = _json["store_sold_out_icon"]["text"];
		if (!_json["store_upgrade_empty_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradeEmptyInfo_l10n_key = _json["store_upgrade_empty_info"]["key"];
		if (!_json["store_upgrade_empty_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreUpgradeEmptyInfo = _json["store_upgrade_empty_info"]["text"];
		if (!_json["store_item_not_saleable_comment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemNotSaleableComment_l10n_key = _json["store_item_not_saleable_comment"]["key"];
		if (!_json["store_item_not_saleable_comment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemNotSaleableComment = _json["store_item_not_saleable_comment"]["text"];
		if (!_json["store_quantity_submit_selling"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitSelling_l10n_key = _json["store_quantity_submit_selling"]["key"];
		if (!_json["store_quantity_submit_selling"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitSelling = _json["store_quantity_submit_selling"]["text"];
		if (!_json["store_quantity_submit_current_money_selling"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCurrentMoneySelling_l10n_key = _json["store_quantity_submit_current_money_selling"]["key"];
		if (!_json["store_quantity_submit_current_money_selling"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCurrentMoneySelling = _json["store_quantity_submit_current_money_selling"]["text"];
		if (!_json["store_quantity_submit_total_money_selling"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitTotalMoneySelling_l10n_key = _json["store_quantity_submit_total_money_selling"]["key"];
		if (!_json["store_quantity_submit_total_money_selling"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitTotalMoneySelling = _json["store_quantity_submit_total_money_selling"]["text"];
		if (!_json["store_quantity_submit_buying"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitBuying_l10n_key = _json["store_quantity_submit_buying"]["key"];
		if (!_json["store_quantity_submit_buying"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitBuying = _json["store_quantity_submit_buying"]["text"];
		if (!_json["store_quantity_submit_current_money_buying"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCurrentMoneyBuying_l10n_key = _json["store_quantity_submit_current_money_buying"]["key"];
		if (!_json["store_quantity_submit_current_money_buying"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCurrentMoneyBuying = _json["store_quantity_submit_current_money_buying"]["text"];
		if (!_json["store_quantity_submit_total_money_buying"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitTotalMoneyBuying_l10n_key = _json["store_quantity_submit_total_money_buying"]["key"];
		if (!_json["store_quantity_submit_total_money_buying"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitTotalMoneyBuying = _json["store_quantity_submit_total_money_buying"]["text"];
		if (!_json["store_quantity_submit_count_in_back"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCountInBack_l10n_key = _json["store_quantity_submit_count_in_back"]["key"];
		if (!_json["store_quantity_submit_count_in_back"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitCountInBack = _json["store_quantity_submit_count_in_back"]["text"];
		if (!_json["store_quantity_submit_unit_price"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitUnitPrice_l10n_key = _json["store_quantity_submit_unit_price"]["key"];
		if (!_json["store_quantity_submit_unit_price"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreQuantitySubmitUnitPrice = _json["store_quantity_submit_unit_price"]["text"];
		if (!_json["store_item_tip_plantbasin_locked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemTipPlantbasinLocked_l10n_key = _json["store_item_tip_plantbasin_locked"]["key"];
		if (!_json["store_item_tip_plantbasin_locked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		StoreItemTipPlantbasinLocked = _json["store_item_tip_plantbasin_locked"]["text"];
		if (!_json["farmbuilder_err_build_system_not_support"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildSystemNotSupport_l10n_key = _json["farmbuilder_err_build_system_not_support"]["key"];
		if (!_json["farmbuilder_err_build_system_not_support"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildSystemNotSupport = _json["farmbuilder_err_build_system_not_support"]["text"];
		if (!_json["farmbuilder_err_lack_of_asset"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrLackOfAsset_l10n_key = _json["farmbuilder_err_lack_of_asset"]["key"];
		if (!_json["farmbuilder_err_lack_of_asset"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrLackOfAsset = _json["farmbuilder_err_lack_of_asset"]["text"];
		if (!_json["farmbuilder_err_indoor_equipment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrIndoorEquipment_l10n_key = _json["farmbuilder_err_indoor_equipment"]["key"];
		if (!_json["farmbuilder_err_indoor_equipment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrIndoorEquipment = _json["farmbuilder_err_indoor_equipment"]["text"];
		if (!_json["farmbuilder_err_outdoor_equipment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrOutdoorEquipment_l10n_key = _json["farmbuilder_err_outdoor_equipment"]["key"];
		if (!_json["farmbuilder_err_outdoor_equipment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrOutdoorEquipment = _json["farmbuilder_err_outdoor_equipment"]["text"];
		if (!_json["farmbuilder_err_invalid_position"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrInvalidPosition_l10n_key = _json["farmbuilder_err_invalid_position"]["key"];
		if (!_json["farmbuilder_err_invalid_position"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrInvalidPosition = _json["farmbuilder_err_invalid_position"]["text"];
		if (!_json["farmbuilder_err_out_of_range"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrOutOfRange_l10n_key = _json["farmbuilder_err_out_of_range"]["key"];
		if (!_json["farmbuilder_err_out_of_range"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrOutOfRange = _json["farmbuilder_err_out_of_range"]["text"];
		if (!_json["farmbuilder_err_equipment_area_not_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentAreaNotEmpty_l10n_key = _json["farmbuilder_err_equipment_area_not_empty"]["key"];
		if (!_json["farmbuilder_err_equipment_area_not_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentAreaNotEmpty = _json["farmbuilder_err_equipment_area_not_empty"]["text"];
		if (!_json["farmbuilder_err_platform_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformOccupied_l10n_key = _json["farmbuilder_err_platform_occupied"]["key"];
		if (!_json["farmbuilder_err_platform_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformOccupied = _json["farmbuilder_err_platform_occupied"]["text"];
		if (!_json["farmbuilder_question_remove"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderQuestionRemove_l10n_key = _json["farmbuilder_question_remove"]["key"];
		if (!_json["farmbuilder_question_remove"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderQuestionRemove = _json["farmbuilder_question_remove"]["text"];
		if (!_json["farmbuilder_question_remove_return"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderQuestionRemoveReturn_l10n_key = _json["farmbuilder_question_remove_return"]["key"];
		if (!_json["farmbuilder_question_remove_return"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderQuestionRemoveReturn = _json["farmbuilder_question_remove_return"]["text"];
		if (!_json["farmbuilder_err_equipment_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentOccupied_l10n_key = _json["farmbuilder_err_equipment_occupied"]["key"];
		if (!_json["farmbuilder_err_equipment_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentOccupied = _json["farmbuilder_err_equipment_occupied"]["text"];
		if (!_json["farmbuilder_err_plantbasin_tree_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlantbasinTreeOccupied_l10n_key = _json["farmbuilder_err_plantbasin_tree_occupied"]["key"];
		if (!_json["farmbuilder_err_plantbasin_tree_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlantbasinTreeOccupied = _json["farmbuilder_err_plantbasin_tree_occupied"]["text"];
		if (!_json["farmbuilder_err_parking_apron_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrParkingApronOccupied_l10n_key = _json["farmbuilder_err_parking_apron_occupied"]["key"];
		if (!_json["farmbuilder_err_parking_apron_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrParkingApronOccupied = _json["farmbuilder_err_parking_apron_occupied"]["text"];
		if (!_json["farmbuilder_err_automate_bot_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrAutomateBotOccupied_l10n_key = _json["farmbuilder_err_automate_bot_occupied"]["key"];
		if (!_json["farmbuilder_err_automate_bot_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrAutomateBotOccupied = _json["farmbuilder_err_automate_bot_occupied"]["text"];
		if (!_json["farmbuilder_err_show_case_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrShowCaseOccupied_l10n_key = _json["farmbuilder_err_show_case_occupied"]["key"];
		if (!_json["farmbuilder_err_show_case_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrShowCaseOccupied = _json["farmbuilder_err_show_case_occupied"]["text"];
		if (!_json["farmbuilder_err_platform_remove"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformRemove_l10n_key = _json["farmbuilder_err_platform_remove"]["key"];
		if (!_json["farmbuilder_err_platform_remove"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformRemove = _json["farmbuilder_err_platform_remove"]["text"];
		if (!_json["farmbuilder_err_equipment_hide_door"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentHideDoor_l10n_key = _json["farmbuilder_err_equipment_hide_door"]["key"];
		if (!_json["farmbuilder_err_equipment_hide_door"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentHideDoor = _json["farmbuilder_err_equipment_hide_door"]["text"];
		if (!_json["farmbuilder_err_building_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupied_l10n_key = _json["farmbuilder_err_building_occupied"]["key"];
		if (!_json["farmbuilder_err_building_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupied = _json["farmbuilder_err_building_occupied"]["text"];
		if (!_json["farmbuilder_err_building_occupied_animal"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedAnimal_l10n_key = _json["farmbuilder_err_building_occupied_animal"]["key"];
		if (!_json["farmbuilder_err_building_occupied_animal"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedAnimal = _json["farmbuilder_err_building_occupied_animal"]["text"];
		if (!_json["farmbuilder_err_building_area_not_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingAreaNotEmpty_l10n_key = _json["farmbuilder_err_building_area_not_empty"]["key"];
		if (!_json["farmbuilder_err_building_area_not_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingAreaNotEmpty = _json["farmbuilder_err_building_area_not_empty"]["text"];
		if (!_json["farmbuilder_err_building_invalid_height"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidHeight_l10n_key = _json["farmbuilder_err_building_invalid_height"]["key"];
		if (!_json["farmbuilder_err_building_invalid_height"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidHeight = _json["farmbuilder_err_building_invalid_height"]["text"];
		if (!_json["farmbuilder_err_building_invalid_covered_ratio"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidCoveredRatio_l10n_key = _json["farmbuilder_err_building_invalid_covered_ratio"]["key"];
		if (!_json["farmbuilder_err_building_invalid_covered_ratio"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidCoveredRatio = _json["farmbuilder_err_building_invalid_covered_ratio"]["text"];
		if (!_json["farmbuilder_err_building_can_not_remove"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingCanNotRemove_l10n_key = _json["farmbuilder_err_building_can_not_remove"]["key"];
		if (!_json["farmbuilder_err_building_can_not_remove"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingCanNotRemove = _json["farmbuilder_err_building_can_not_remove"]["text"];
		if (!_json["farmbuilder_err_building_invalid_other_no_support"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidOtherNoSupport_l10n_key = _json["farmbuilder_err_building_invalid_other_no_support"]["key"];
		if (!_json["farmbuilder_err_building_invalid_other_no_support"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidOtherNoSupport = _json["farmbuilder_err_building_invalid_other_no_support"]["text"];
		if (!_json["farmbuilder_err_building_occupied_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedBuilding_l10n_key = _json["farmbuilder_err_building_occupied_building"]["key"];
		if (!_json["farmbuilder_err_building_occupied_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedBuilding = _json["farmbuilder_err_building_occupied_building"]["text"];
		if (!_json["farmbuilder_err_building_invalid_door_be_hidden"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidDoorBeHidden_l10n_key = _json["farmbuilder_err_building_invalid_door_be_hidden"]["key"];
		if (!_json["farmbuilder_err_building_invalid_door_be_hidden"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingInvalidDoorBeHidden = _json["farmbuilder_err_building_invalid_door_be_hidden"]["text"];
		if (!_json["farmbuilder_err_building_occupied_ceiling"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedCeiling_l10n_key = _json["farmbuilder_err_building_occupied_ceiling"]["key"];
		if (!_json["farmbuilder_err_building_occupied_ceiling"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedCeiling = _json["farmbuilder_err_building_occupied_ceiling"]["text"];
		if (!_json["farmbuilder_err_building_occupied_door"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedDoor_l10n_key = _json["farmbuilder_err_building_occupied_door"]["key"];
		if (!_json["farmbuilder_err_building_occupied_door"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedDoor = _json["farmbuilder_err_building_occupied_door"]["text"];
		if (!_json["farmbuilder_err_building_occupied_ceiling_pt"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedCeilingPt_l10n_key = _json["farmbuilder_err_building_occupied_ceiling_pt"]["key"];
		if (!_json["farmbuilder_err_building_occupied_ceiling_pt"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingOccupiedCeilingPt = _json["farmbuilder_err_building_occupied_ceiling_pt"]["text"];
		if (!_json["farmbuilder_err_building_ground_invalid"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingGroundInvalid_l10n_key = _json["farmbuilder_err_building_ground_invalid"]["key"];
		if (!_json["farmbuilder_err_building_ground_invalid"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrBuildingGroundInvalid = _json["farmbuilder_err_building_ground_invalid"]["text"];
		if (!_json["farmbuilder_err_platform_width"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformWidth_l10n_key = _json["farmbuilder_err_platform_width"]["key"];
		if (!_json["farmbuilder_err_platform_width"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformWidth = _json["farmbuilder_err_platform_width"]["text"];
		if (!_json["farmbuilder_err_platform_height_max"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformHeightMax_l10n_key = _json["farmbuilder_err_platform_height_max"]["key"];
		if (!_json["farmbuilder_err_platform_height_max"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrPlatformHeightMax = _json["farmbuilder_err_platform_height_max"]["text"];
		if (!_json["farmbuilder_err_equipment_limited"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentLimited_l10n_key = _json["farmbuilder_err_equipment_limited"]["key"];
		if (!_json["farmbuilder_err_equipment_limited"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FarmbuilderErrEquipmentLimited = _json["farmbuilder_err_equipment_limited"]["text"];
		if (!_json["techtree_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreePanelTitle_l10n_key = _json["techtree_panel_title"]["key"];
		if (!_json["techtree_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreePanelTitle = _json["techtree_panel_title"]["text"];
		if (!_json["techtree_node_lack_of_points"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeLackOfPoints_l10n_key = _json["techtree_node_lack_of_points"]["key"];
		if (!_json["techtree_node_lack_of_points"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeLackOfPoints = _json["techtree_node_lack_of_points"]["text"];
		if (!_json["techtree_node_not_avaiable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeNotAvaiable_l10n_key = _json["techtree_node_not_avaiable"]["key"];
		if (!_json["techtree_node_not_avaiable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeNotAvaiable = _json["techtree_node_not_avaiable"]["text"];
		if (!_json["techtree_node_unlocked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnlocked_l10n_key = _json["techtree_node_unlocked"]["key"];
		if (!_json["techtree_node_unlocked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnlocked = _json["techtree_node_unlocked"]["text"];
		if (!_json["techtree_node_unopen"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnopen_l10n_key = _json["techtree_node_unopen"]["key"];
		if (!_json["techtree_node_unopen"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnopen = _json["techtree_node_unopen"]["text"];
		if (!_json["techtree_node_unlock_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnlockConfirm_l10n_key = _json["techtree_node_unlock_confirm"]["key"];
		if (!_json["techtree_node_unlock_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeUnlockConfirm = _json["techtree_node_unlock_confirm"]["text"];
		if (!_json["techtree_node_building_health"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeBuildingHealth_l10n_key = _json["techtree_node_building_health"]["key"];
		if (!_json["techtree_node_building_health"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeBuildingHealth = _json["techtree_node_building_health"]["text"];
		if (!_json["techtree_node_equipment_electronic"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeEquipmentElectronic_l10n_key = _json["techtree_node_equipment_electronic"]["key"];
		if (!_json["techtree_node_equipment_electronic"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeEquipmentElectronic = _json["techtree_node_equipment_electronic"]["text"];
		if (!_json["techtree_progress_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeProgressInfo_l10n_key = _json["techtree_progress_info"]["key"];
		if (!_json["techtree_progress_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeProgressInfo = _json["techtree_progress_info"]["text"];
		if (!_json["techtree_max_level"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeMaxLevel_l10n_key = _json["techtree_max_level"]["key"];
		if (!_json["techtree_max_level"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeMaxLevel = _json["techtree_max_level"]["text"];
		if (!_json["techtree_node_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeHint_l10n_key = _json["techtree_node_hint"]["key"];
		if (!_json["techtree_node_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TechtreeNodeHint = _json["techtree_node_hint"]["text"];
		if (!_json["mission_update"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionUpdate_l10n_key = _json["mission_update"]["key"];
		if (!_json["mission_update"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionUpdate = _json["mission_update"]["text"];
		if (!_json["mission_complete"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionComplete_l10n_key = _json["mission_complete"]["key"];
		if (!_json["mission_complete"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionComplete = _json["mission_complete"]["text"];
		if (!_json["mission_complete_send_email"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionCompleteSendEmail_l10n_key = _json["mission_complete_send_email"]["key"];
		if (!_json["mission_complete_send_email"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionCompleteSendEmail = _json["mission_complete_send_email"]["text"];
		if (!_json["mission_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionPanelTitle_l10n_key = _json["mission_panel_title"]["key"];
		if (!_json["mission_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionPanelTitle = _json["mission_panel_title"]["text"];
		if (!_json["mission_panel_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionPanelEmpty_l10n_key = _json["mission_panel_empty"]["key"];
		if (!_json["mission_panel_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionPanelEmpty = _json["mission_panel_empty"]["text"];
		if (!_json["ui_mission_time_limit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiMissionTimeLimit_l10n_key = _json["ui_mission_time_limit"]["key"];
		if (!_json["ui_mission_time_limit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiMissionTimeLimit = _json["ui_mission_time_limit"]["text"];
		if (!_json["ui_mission_npc_position"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiMissionNpcPosition_l10n_key = _json["ui_mission_npc_position"]["key"];
		if (!_json["ui_mission_npc_position"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiMissionNpcPosition = _json["ui_mission_npc_position"]["text"];
		if (!_json["seed_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SeedPanelTitle_l10n_key = _json["seed_panel_title"]["key"];
		if (!_json["seed_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SeedPanelTitle = _json["seed_panel_title"]["text"];
		if (!_json["seed_node_already_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SeedNodeAlreadyUnlock_l10n_key = _json["seed_node_already_unlock"]["key"];
		if (!_json["seed_node_already_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SeedNodeAlreadyUnlock = _json["seed_node_already_unlock"]["text"];
		if (!_json["seed_node_succeed_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SeedNodeSucceedUnlock_l10n_key = _json["seed_node_succeed_unlock"]["key"];
		if (!_json["seed_node_succeed_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SeedNodeSucceedUnlock = _json["seed_node_succeed_unlock"]["text"];
		if (!_json["building_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelTitle_l10n_key = _json["building_panel_title"]["key"];
		if (!_json["building_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelTitle = _json["building_panel_title"]["text"];
		if (!_json["building_panel_cover_size_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelCoverSizeDescription_l10n_key = _json["building_panel_cover_size_description"]["key"];
		if (!_json["building_panel_cover_size_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelCoverSizeDescription = _json["building_panel_cover_size_description"]["text"];
		if (!_json["building_panel_inner_size_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelInnerSizeDescription_l10n_key = _json["building_panel_inner_size_description"]["key"];
		if (!_json["building_panel_inner_size_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelInnerSizeDescription = _json["building_panel_inner_size_description"]["text"];
		if (!_json["building_panel_animal_capacity_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelAnimalCapacityDescription_l10n_key = _json["building_panel_animal_capacity_description"]["key"];
		if (!_json["building_panel_animal_capacity_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelAnimalCapacityDescription = _json["building_panel_animal_capacity_description"]["text"];
		if (!_json["building_panel_size_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelSizeDescription_l10n_key = _json["building_panel_size_description"]["key"];
		if (!_json["building_panel_size_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelSizeDescription = _json["building_panel_size_description"]["text"];
		if (!_json["building_panel_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelEmpty_l10n_key = _json["building_panel_empty"]["key"];
		if (!_json["building_panel_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelEmpty = _json["building_panel_empty"]["text"];
		if (!_json["building_panel_start_build"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelStartBuild_l10n_key = _json["building_panel_start_build"]["key"];
		if (!_json["building_panel_start_build"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelStartBuild = _json["building_panel_start_build"]["text"];
		if (!_json["building_panel_material_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelMaterialNotEnough_l10n_key = _json["building_panel_material_not_enough"]["key"];
		if (!_json["building_panel_material_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelMaterialNotEnough = _json["building_panel_material_not_enough"]["text"];
		if (!_json["building_panel_money_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelMoneyNotEnough_l10n_key = _json["building_panel_money_not_enough"]["key"];
		if (!_json["building_panel_money_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelMoneyNotEnough = _json["building_panel_money_not_enough"]["text"];
		if (!_json["building_panel_input_building_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelInputBuildingName_l10n_key = _json["building_panel_input_building_name"]["key"];
		if (!_json["building_panel_input_building_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuildingPanelInputBuildingName = _json["building_panel_input_building_name"]["text"];
		if (!_json["equipment_panel_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelEmpty_l10n_key = _json["equipment_panel_empty"]["key"];
		if (!_json["equipment_panel_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelEmpty = _json["equipment_panel_empty"]["text"];
		if (!_json["equipment_panel_start_build"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelStartBuild_l10n_key = _json["equipment_panel_start_build"]["key"];
		if (!_json["equipment_panel_start_build"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelStartBuild = _json["equipment_panel_start_build"]["text"];
		if (!_json["equipment_panel_list_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelListName_l10n_key = _json["equipment_panel_list_name"]["key"];
		if (!_json["equipment_panel_list_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelListName = _json["equipment_panel_list_name"]["text"];
		if (!_json["equipment_viewer_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentViewerEmpty_l10n_key = _json["equipment_viewer_empty"]["key"];
		if (!_json["equipment_viewer_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentViewerEmpty = _json["equipment_viewer_empty"]["text"];
		if (!_json["equipment_panel_lately_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelLatelyEmpty_l10n_key = _json["equipment_panel_lately_empty"]["key"];
		if (!_json["equipment_panel_lately_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelLatelyEmpty = _json["equipment_panel_lately_empty"]["text"];
		if (!_json["equipment_panel_make_complete"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelMakeComplete_l10n_key = _json["equipment_panel_make_complete"]["key"];
		if (!_json["equipment_panel_make_complete"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelMakeComplete = _json["equipment_panel_make_complete"]["text"];
		if (!_json["equipment_panel_hide"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelHide_l10n_key = _json["equipment_panel_hide"]["key"];
		if (!_json["equipment_panel_hide"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelHide = _json["equipment_panel_hide"]["text"];
		if (!_json["equipment_panel_jump"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelJump_l10n_key = _json["equipment_panel_jump"]["key"];
		if (!_json["equipment_panel_jump"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelJump = _json["equipment_panel_jump"]["text"];
		if (!_json["equipment_panel_unlock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelUnlockTip_l10n_key = _json["equipment_panel_unlock_tip"]["key"];
		if (!_json["equipment_panel_unlock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelUnlockTip = _json["equipment_panel_unlock_tip"]["text"];
		if (!_json["equipment_panel_electronic_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelElectronicTip_l10n_key = _json["equipment_panel_electronic_tip"]["key"];
		if (!_json["equipment_panel_electronic_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelElectronicTip = _json["equipment_panel_electronic_tip"]["text"];
		if (!_json["equipment_panel_battery_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelBatteryTip_l10n_key = _json["equipment_panel_battery_tip"]["key"];
		if (!_json["equipment_panel_battery_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelBatteryTip = _json["equipment_panel_battery_tip"]["text"];
		if (!_json["equipment_panel_appliance_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelApplianceTip_l10n_key = _json["equipment_panel_appliance_tip"]["key"];
		if (!_json["equipment_panel_appliance_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelApplianceTip = _json["equipment_panel_appliance_tip"]["text"];
		if (!_json["equipment_panel_generator_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelGeneratorTip_l10n_key = _json["equipment_panel_generator_tip"]["key"];
		if (!_json["equipment_panel_generator_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentPanelGeneratorTip = _json["equipment_panel_generator_tip"]["text"];
		if (!_json["ui_email_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailTitle_l10n_key = _json["ui_email_title"]["key"];
		if (!_json["ui_email_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailTitle = _json["ui_email_title"]["text"];
		if (!_json["ui_email_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailEmpty_l10n_key = _json["ui_email_empty"]["key"];
		if (!_json["ui_email_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailEmpty = _json["ui_email_empty"]["text"];
		if (!_json["ui_email_accept_mission"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAcceptMission_l10n_key = _json["ui_email_accept_mission"]["key"];
		if (!_json["ui_email_accept_mission"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAcceptMission = _json["ui_email_accept_mission"]["text"];
		if (!_json["ui_email_already_accepted"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAlreadyAccepted_l10n_key = _json["ui_email_already_accepted"]["key"];
		if (!_json["ui_email_already_accepted"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAlreadyAccepted = _json["ui_email_already_accepted"]["text"];
		if (!_json["ui_email_recive_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailReciveItem_l10n_key = _json["ui_email_recive_item"]["key"];
		if (!_json["ui_email_recive_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailReciveItem = _json["ui_email_recive_item"]["text"];
		if (!_json["ui_email_already_recived"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAlreadyRecived_l10n_key = _json["ui_email_already_recived"]["key"];
		if (!_json["ui_email_already_recived"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailAlreadyRecived = _json["ui_email_already_recived"]["text"];
		if (!_json["ui_email_recycle_err"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailRecycleErr_l10n_key = _json["ui_email_recycle_err"]["key"];
		if (!_json["ui_email_recycle_err"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEmailRecycleErr = _json["ui_email_recycle_err"]["text"];
		if (!_json["recipe_panel_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelEmpty_l10n_key = _json["recipe_panel_empty"]["key"];
		if (!_json["recipe_panel_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelEmpty = _json["recipe_panel_empty"]["text"];
		if (!_json["recipe_panel_start_build"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelStartBuild_l10n_key = _json["recipe_panel_start_build"]["key"];
		if (!_json["recipe_panel_start_build"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelStartBuild = _json["recipe_panel_start_build"]["text"];
		if (!_json["recipe_panel_time_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTimeInfo_l10n_key = _json["recipe_panel_time_info"]["key"];
		if (!_json["recipe_panel_time_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTimeInfo = _json["recipe_panel_time_info"]["text"];
		if (!_json["recipe_panel_already_working"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelAlreadyWorking_l10n_key = _json["recipe_panel_already_working"]["key"];
		if (!_json["recipe_panel_already_working"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelAlreadyWorking = _json["recipe_panel_already_working"]["text"];
		if (!_json["recipe_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTitle_l10n_key = _json["recipe_panel_title"]["key"];
		if (!_json["recipe_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTitle = _json["recipe_panel_title"]["text"];
		if (!_json["recipe_panel_task"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTask_l10n_key = _json["recipe_panel_task"]["key"];
		if (!_json["recipe_panel_task"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelTask = _json["recipe_panel_task"]["text"];
		if (!_json["recipe_panel_rest_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelRestTime_l10n_key = _json["recipe_panel_rest_time"]["key"];
		if (!_json["recipe_panel_rest_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelRestTime = _json["recipe_panel_rest_time"]["text"];
		if (!_json["recipe_panel_max_craft_count"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelMaxCraftCount_l10n_key = _json["recipe_panel_max_craft_count"]["key"];
		if (!_json["recipe_panel_max_craft_count"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelMaxCraftCount = _json["recipe_panel_max_craft_count"]["text"];
		if (!_json["recipe_panel_limit_up"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelLimitUp_l10n_key = _json["recipe_panel_limit_up"]["key"];
		if (!_json["recipe_panel_limit_up"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelLimitUp = _json["recipe_panel_limit_up"]["text"];
		if (!_json["recipe_panel_not_cookable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelNotCookable_l10n_key = _json["recipe_panel_not_cookable"]["key"];
		if (!_json["recipe_panel_not_cookable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelNotCookable = _json["recipe_panel_not_cookable"]["text"];
		if (!_json["recipe_panel_no_material"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelNoMaterial_l10n_key = _json["recipe_panel_no_material"]["key"];
		if (!_json["recipe_panel_no_material"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelNoMaterial = _json["recipe_panel_no_material"]["text"];
		if (!_json["recipe_panel_confirm_with_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelConfirmWithTime_l10n_key = _json["recipe_panel_confirm_with_time"]["key"];
		if (!_json["recipe_panel_confirm_with_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelConfirmWithTime = _json["recipe_panel_confirm_with_time"]["text"];
		if (!_json["recipe_panel_unlocked_recipe_comment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnlockedRecipeComment_l10n_key = _json["recipe_panel_unlocked_recipe_comment"]["key"];
		if (!_json["recipe_panel_unlocked_recipe_comment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnlockedRecipeComment = _json["recipe_panel_unlocked_recipe_comment"]["text"];
		if (!_json["recipe_panel_unlocked_dish_comment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnlockedDishComment_l10n_key = _json["recipe_panel_unlocked_dish_comment"]["key"];
		if (!_json["recipe_panel_unlocked_dish_comment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnlockedDishComment = _json["recipe_panel_unlocked_dish_comment"]["text"];
		if (!_json["recipe_panel_unknow_dish_comment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnknowDishComment_l10n_key = _json["recipe_panel_unknow_dish_comment"]["key"];
		if (!_json["recipe_panel_unknow_dish_comment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelUnknowDishComment = _json["recipe_panel_unknow_dish_comment"]["text"];
		if (!_json["recipe_panel_confirm_pop_buffer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelConfirmPopBuffer_l10n_key = _json["recipe_panel_confirm_pop_buffer"]["key"];
		if (!_json["recipe_panel_confirm_pop_buffer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelConfirmPopBuffer = _json["recipe_panel_confirm_pop_buffer"]["text"];
		if (!_json["recipe_panel_material_list"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelMaterialList_l10n_key = _json["recipe_panel_material_list"]["key"];
		if (!_json["recipe_panel_material_list"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelMaterialList = _json["recipe_panel_material_list"]["text"];
		if (!_json["recipe_panel_exist_items"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelExistItems_l10n_key = _json["recipe_panel_exist_items"]["key"];
		if (!_json["recipe_panel_exist_items"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelExistItems = _json["recipe_panel_exist_items"]["text"];
		if (!_json["recipe_panel_random_gene_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelRandomGeneHint_l10n_key = _json["recipe_panel_random_gene_hint"]["key"];
		if (!_json["recipe_panel_random_gene_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RecipePanelRandomGeneHint = _json["recipe_panel_random_gene_hint"]["text"];
		if (!_json["platform_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelTitle_l10n_key = _json["platform_panel_title"]["key"];
		if (!_json["platform_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelTitle = _json["platform_panel_title"]["text"];
		if (!_json["platform_panel_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelEmpty_l10n_key = _json["platform_panel_empty"]["key"];
		if (!_json["platform_panel_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelEmpty = _json["platform_panel_empty"]["text"];
		if (!_json["platform_panel_start_build"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelStartBuild_l10n_key = _json["platform_panel_start_build"]["key"];
		if (!_json["platform_panel_start_build"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelStartBuild = _json["platform_panel_start_build"]["text"];
		if (!_json["platform_panel_cost_prefix"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelCostPrefix_l10n_key = _json["platform_panel_cost_prefix"]["key"];
		if (!_json["platform_panel_cost_prefix"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlatformPanelCostPrefix = _json["platform_panel_cost_prefix"]["text"];
		if (!_json["item_building_proto_error"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemBuildingProtoError_l10n_key = _json["item_building_proto_error"]["key"];
		if (!_json["item_building_proto_error"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemBuildingProtoError = _json["item_building_proto_error"]["text"];
		if (!_json["item_equipment_proto_error"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemEquipmentProtoError_l10n_key = _json["item_equipment_proto_error"]["key"];
		if (!_json["item_equipment_proto_error"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemEquipmentProtoError = _json["item_equipment_proto_error"]["text"];
		if (!_json["item_send_email_on_overflow"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemSendEmailOnOverflow_l10n_key = _json["item_send_email_on_overflow"]["key"];
		if (!_json["item_send_email_on_overflow"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemSendEmailOnOverflow = _json["item_send_email_on_overflow"]["text"];
		if (!_json["item_sleeping_bag_condition_failed_monster"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemSleepingBagConditionFailedMonster_l10n_key = _json["item_sleeping_bag_condition_failed_monster"]["key"];
		if (!_json["item_sleeping_bag_condition_failed_monster"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemSleepingBagConditionFailedMonster = _json["item_sleeping_bag_condition_failed_monster"]["text"];
		if (!_json["item_sleeping_bag_condition_failed_water"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemSleepingBagConditionFailedWater_l10n_key = _json["item_sleeping_bag_condition_failed_water"]["key"];
		if (!_json["item_sleeping_bag_condition_failed_water"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemSleepingBagConditionFailedWater = _json["item_sleeping_bag_condition_failed_water"]["text"];
		if (!_json["item_rescue_pager_condition_failed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemRescuePagerConditionFailed_l10n_key = _json["item_rescue_pager_condition_failed"]["key"];
		if (!_json["item_rescue_pager_condition_failed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemRescuePagerConditionFailed = _json["item_rescue_pager_condition_failed"]["text"];
		if (!_json["item_tip_price"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipPrice_l10n_key = _json["item_tip_price"]["key"];
		if (!_json["item_tip_price"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipPrice = _json["item_tip_price"]["text"];
		if (!_json["item_tip_basics_price"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipBasicsPrice_l10n_key = _json["item_tip_basics_price"]["key"];
		if (!_json["item_tip_basics_price"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipBasicsPrice = _json["item_tip_basics_price"]["text"];
		if (!_json["item_tip_money_unit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipMoneyUnit_l10n_key = _json["item_tip_money_unit"]["key"];
		if (!_json["item_tip_money_unit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemTipMoneyUnit = _json["item_tip_money_unit"]["text"];
		if (!_json["item_chip_attack_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackIncrease_l10n_key = _json["item_chip_attack_increase"]["key"];
		if (!_json["item_chip_attack_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackIncrease = _json["item_chip_attack_increase"]["text"];
		if (!_json["item_chip_attack_increase_fixed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackIncreaseFixed_l10n_key = _json["item_chip_attack_increase_fixed"]["key"];
		if (!_json["item_chip_attack_increase_fixed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackIncreaseFixed = _json["item_chip_attack_increase_fixed"]["text"];
		if (!_json["item_chip_critical_rate_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipCriticalRateIncrease_l10n_key = _json["item_chip_critical_rate_increase"]["key"];
		if (!_json["item_chip_critical_rate_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipCriticalRateIncrease = _json["item_chip_critical_rate_increase"]["text"];
		if (!_json["item_chip_attack_speed_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackSpeedIncrease_l10n_key = _json["item_chip_attack_speed_increase"]["key"];
		if (!_json["item_chip_attack_speed_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackSpeedIncrease = _json["item_chip_attack_speed_increase"]["text"];
		if (!_json["item_chip_accuracy_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAccuracyIncrease_l10n_key = _json["item_chip_accuracy_increase"]["key"];
		if (!_json["item_chip_accuracy_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAccuracyIncrease = _json["item_chip_accuracy_increase"]["text"];
		if (!_json["item_chip_power_cost_decrease"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipPowerCostDecrease_l10n_key = _json["item_chip_power_cost_decrease"]["key"];
		if (!_json["item_chip_power_cost_decrease"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipPowerCostDecrease = _json["item_chip_power_cost_decrease"]["text"];
		if (!_json["item_chip_attack_distance_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackDistanceIncrease_l10n_key = _json["item_chip_attack_distance_increase"]["key"];
		if (!_json["item_chip_attack_distance_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipAttackDistanceIncrease = _json["item_chip_attack_distance_increase"]["text"];
		if (!_json["item_chip_move_speed_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipMoveSpeedIncrease_l10n_key = _json["item_chip_move_speed_increase"]["key"];
		if (!_json["item_chip_move_speed_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipMoveSpeedIncrease = _json["item_chip_move_speed_increase"]["text"];
		if (!_json["item_chip_clip_capacity_addition"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipClipCapacityAddition_l10n_key = _json["item_chip_clip_capacity_addition"]["key"];
		if (!_json["item_chip_clip_capacity_addition"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipClipCapacityAddition = _json["item_chip_clip_capacity_addition"]["text"];
		if (!_json["item_chip_reload_duration_decrease"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipReloadDurationDecrease_l10n_key = _json["item_chip_reload_duration_decrease"]["key"];
		if (!_json["item_chip_reload_duration_decrease"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemChipReloadDurationDecrease = _json["item_chip_reload_duration_decrease"]["text"];
		if (!_json["item_engine_move_speed_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemEngineMoveSpeedIncrease_l10n_key = _json["item_engine_move_speed_increase"]["key"];
		if (!_json["item_engine_move_speed_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemEngineMoveSpeedIncrease = _json["item_engine_move_speed_increase"]["text"];
		if (!_json["item_engine_power_capacity_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemEnginePowerCapacityIncrease_l10n_key = _json["item_engine_power_capacity_increase"]["key"];
		if (!_json["item_engine_power_capacity_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemEnginePowerCapacityIncrease = _json["item_engine_power_capacity_increase"]["text"];
		if (!_json["item_engine_power_recv_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemEnginePowerRecvIncrease_l10n_key = _json["item_engine_power_recv_increase"]["key"];
		if (!_json["item_engine_power_recv_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemEnginePowerRecvIncrease = _json["item_engine_power_recv_increase"]["text"];
		if (!_json["item_structure_move_speed_increase"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructureMoveSpeedIncrease_l10n_key = _json["item_structure_move_speed_increase"]["key"];
		if (!_json["item_structure_move_speed_increase"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructureMoveSpeedIncrease = _json["item_structure_move_speed_increase"]["text"];
		if (!_json["item_structure_power_capacity"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructurePowerCapacity_l10n_key = _json["item_structure_power_capacity"]["key"];
		if (!_json["item_structure_power_capacity"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructurePowerCapacity = _json["item_structure_power_capacity"]["text"];
		if (!_json["item_structure_power_recv"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructurePowerRecv_l10n_key = _json["item_structure_power_recv"]["key"];
		if (!_json["item_structure_power_recv"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemStructurePowerRecv = _json["item_structure_power_recv"]["text"];
		if (!_json["item_max_durability"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemMaxDurability_l10n_key = _json["item_max_durability"]["key"];
		if (!_json["item_max_durability"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemMaxDurability = _json["item_max_durability"]["text"];
		if (!_json["item_title_info_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemTitleInfoFormat_l10n_key = _json["item_title_info_format"]["key"];
		if (!_json["item_title_info_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemTitleInfoFormat = _json["item_title_info_format"]["text"];
		if (!_json["item_confirm_use"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemConfirmUse_l10n_key = _json["item_confirm_use"]["key"];
		if (!_json["item_confirm_use"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemConfirmUse = _json["item_confirm_use"]["text"];
		if (!_json["item_water_can_area"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemWaterCanArea_l10n_key = _json["item_water_can_area"]["key"];
		if (!_json["item_water_can_area"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemWaterCanArea = _json["item_water_can_area"]["text"];
		if (!_json["item_water_can_endless_water"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemWaterCanEndlessWater_l10n_key = _json["item_water_can_endless_water"]["key"];
		if (!_json["item_water_can_endless_water"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemWaterCanEndlessWater = _json["item_water_can_endless_water"]["text"];
		if (!_json["item_gene_desc_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemGeneDescFormat_l10n_key = _json["item_gene_desc_format"]["key"];
		if (!_json["item_gene_desc_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemGeneDescFormat = _json["item_gene_desc_format"]["text"];
		if (!_json["item_seed_cloned"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemSeedCloned_l10n_key = _json["item_seed_cloned"]["key"];
		if (!_json["item_seed_cloned"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemSeedCloned = _json["item_seed_cloned"]["text"];
		if (!_json["item_wp_is_available_for"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemWpIsAvailableFor_l10n_key = _json["item_wp_is_available_for"]["key"];
		if (!_json["item_wp_is_available_for"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemWpIsAvailableFor = _json["item_wp_is_available_for"]["text"];
		if (!_json["item_wp_available_all"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemWpAvailableAll_l10n_key = _json["item_wp_available_all"]["key"];
		if (!_json["item_wp_available_all"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemWpAvailableAll = _json["item_wp_available_all"]["text"];
		if (!_json["item_fish_fry_title_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemFishFryTitleFormat_l10n_key = _json["item_fish_fry_title_format"]["key"];
		if (!_json["item_fish_fry_title_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemFishFryTitleFormat = _json["item_fish_fry_title_format"]["text"];
		if (!_json["item_tool_level_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemToolLevelFormat_l10n_key = _json["item_tool_level_format"]["key"];
		if (!_json["item_tool_level_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemToolLevelFormat = _json["item_tool_level_format"]["text"];
		if (!_json["item_patch_value_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemPatchValueFormat_l10n_key = _json["item_patch_value_format"]["key"];
		if (!_json["item_patch_value_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemPatchValueFormat = _json["item_patch_value_format"]["text"];
		if (!_json["item_box_value_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemBoxValueFormat_l10n_key = _json["item_box_value_format"]["key"];
		if (!_json["item_box_value_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemBoxValueFormat = _json["item_box_value_format"]["text"];
		if (!_json["item_film_value_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemFilmValueFormat_l10n_key = _json["item_film_value_format"]["key"];
		if (!_json["item_film_value_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemFilmValueFormat = _json["item_film_value_format"]["text"];
		if (!_json["item_drone_weapon_value_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemDroneWeaponValueFormat_l10n_key = _json["item_drone_weapon_value_format"]["key"];
		if (!_json["item_drone_weapon_value_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemDroneWeaponValueFormat = _json["item_drone_weapon_value_format"]["text"];
		if (!_json["item_money_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyTitle_l10n_key = _json["item_money_title"]["key"];
		if (!_json["item_money_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyTitle = _json["item_money_title"]["text"];
		if (!_json["item_money_desc"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyDesc_l10n_key = _json["item_money_desc"]["key"];
		if (!_json["item_money_desc"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyDesc = _json["item_money_desc"]["text"];
		if (!_json["item_money_type"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyType_l10n_key = _json["item_money_type"]["key"];
		if (!_json["item_money_type"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemMoneyType = _json["item_money_type"]["text"];
		if (!_json["ui_system_exit_game"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemExitGame_l10n_key = _json["ui_system_exit_game"]["key"];
		if (!_json["ui_system_exit_game"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemExitGame = _json["ui_system_exit_game"]["text"];
		if (!_json["ui_system_return_home_page"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemReturnHomePage_l10n_key = _json["ui_system_return_home_page"]["key"];
		if (!_json["ui_system_return_home_page"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemReturnHomePage = _json["ui_system_return_home_page"]["text"];
		if (!_json["ui_system_confirm_home"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemConfirmHome_l10n_key = _json["ui_system_confirm_home"]["key"];
		if (!_json["ui_system_confirm_home"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemConfirmHome = _json["ui_system_confirm_home"]["text"];
		if (!_json["ui_system_confirm_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemConfirmExit_l10n_key = _json["ui_system_confirm_exit"]["key"];
		if (!_json["ui_system_confirm_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiSystemConfirmExit = _json["ui_system_confirm_exit"]["text"];
		if (!_json["reward_info_building_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoBuildingUnlock_l10n_key = _json["reward_info_building_unlock"]["key"];
		if (!_json["reward_info_building_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoBuildingUnlock = _json["reward_info_building_unlock"]["text"];
		if (!_json["reward_info_bus_station_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoBusStationUnlock_l10n_key = _json["reward_info_bus_station_unlock"]["key"];
		if (!_json["reward_info_bus_station_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoBusStationUnlock = _json["reward_info_bus_station_unlock"]["text"];
		if (!_json["reward_info_equipment_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoEquipmentUnlock_l10n_key = _json["reward_info_equipment_unlock"]["key"];
		if (!_json["reward_info_equipment_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoEquipmentUnlock = _json["reward_info_equipment_unlock"]["text"];
		if (!_json["reward_info_platform_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoPlatformUnlock_l10n_key = _json["reward_info_platform_unlock"]["key"];
		if (!_json["reward_info_platform_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoPlatformUnlock = _json["reward_info_platform_unlock"]["text"];
		if (!_json["reward_info_recipe_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoRecipeUnlock_l10n_key = _json["reward_info_recipe_unlock"]["key"];
		if (!_json["reward_info_recipe_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoRecipeUnlock = _json["reward_info_recipe_unlock"]["text"];
		if (!_json["reward_info_recipe_unlock_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoRecipeUnlockHint_l10n_key = _json["reward_info_recipe_unlock_hint"]["key"];
		if (!_json["reward_info_recipe_unlock_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoRecipeUnlockHint = _json["reward_info_recipe_unlock_hint"]["text"];
		if (!_json["reward_info_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoItem_l10n_key = _json["reward_info_item"]["key"];
		if (!_json["reward_info_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoItem = _json["reward_info_item"]["text"];
		if (!_json["reward_info_gold"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoGold_l10n_key = _json["reward_info_gold"]["key"];
		if (!_json["reward_info_gold"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoGold = _json["reward_info_gold"]["text"];
		if (!_json["reward_info_favorability_lv1"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv1_l10n_key = _json["reward_info_favorability_lv1"]["key"];
		if (!_json["reward_info_favorability_lv1"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv1 = _json["reward_info_favorability_lv1"]["text"];
		if (!_json["reward_info_favorability_lv2"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv2_l10n_key = _json["reward_info_favorability_lv2"]["key"];
		if (!_json["reward_info_favorability_lv2"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv2 = _json["reward_info_favorability_lv2"]["text"];
		if (!_json["reward_info_favorability_lv3"]["key"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv3_l10n_key = _json["reward_info_favorability_lv3"]["key"];
		if (!_json["reward_info_favorability_lv3"]["text"].IsString)
		{
			throw new SerializationException();
		}
		RewardInfoFavorabilityLv3 = _json["reward_info_favorability_lv3"]["text"];
		if (!_json["dropoff_box_no_goods"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxNoGoods_l10n_key = _json["dropoff_box_no_goods"]["key"];
		if (!_json["dropoff_box_no_goods"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxNoGoods = _json["dropoff_box_no_goods"]["text"];
		if (!_json["dropoff_box_no_drone"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxNoDrone_l10n_key = _json["dropoff_box_no_drone"]["key"];
		if (!_json["dropoff_box_no_drone"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxNoDrone = _json["dropoff_box_no_drone"]["text"];
		if (!_json["dropoff_box_total_money"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxTotalMoney_l10n_key = _json["dropoff_box_total_money"]["key"];
		if (!_json["dropoff_box_total_money"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxTotalMoney = _json["dropoff_box_total_money"]["text"];
		if (!_json["dropoff_box_price_increased"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxPriceIncreased_l10n_key = _json["dropoff_box_price_increased"]["key"];
		if (!_json["dropoff_box_price_increased"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxPriceIncreased = _json["dropoff_box_price_increased"]["text"];
		if (!_json["dropoff_box_launch_drone"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxLaunchDrone_l10n_key = _json["dropoff_box_launch_drone"]["key"];
		if (!_json["dropoff_box_launch_drone"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxLaunchDrone = _json["dropoff_box_launch_drone"]["text"];
		if (!_json["dropoff_box_confirm_lauch"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxConfirmLauch_l10n_key = _json["dropoff_box_confirm_lauch"]["key"];
		if (!_json["dropoff_box_confirm_lauch"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DropoffBoxConfirmLauch = _json["dropoff_box_confirm_lauch"]["text"];
		if (!_json["automate_bot_panel_state_idle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateIdle_l10n_key = _json["automate_bot_panel_state_idle"]["key"];
		if (!_json["automate_bot_panel_state_idle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateIdle = _json["automate_bot_panel_state_idle"]["text"];
		if (!_json["automate_bot_panel_state_charge"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateCharge_l10n_key = _json["automate_bot_panel_state_charge"]["key"];
		if (!_json["automate_bot_panel_state_charge"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateCharge = _json["automate_bot_panel_state_charge"]["text"];
		if (!_json["automate_bot_panel_state_pause"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStatePause_l10n_key = _json["automate_bot_panel_state_pause"]["key"];
		if (!_json["automate_bot_panel_state_pause"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStatePause = _json["automate_bot_panel_state_pause"]["text"];
		if (!_json["automate_bot_panel_state_working"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateWorking_l10n_key = _json["automate_bot_panel_state_working"]["key"];
		if (!_json["automate_bot_panel_state_working"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelStateWorking = _json["automate_bot_panel_state_working"]["text"];
		if (!_json["automate_bot_panel_cfg_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelCfgEmpty_l10n_key = _json["automate_bot_panel_cfg_empty"]["key"];
		if (!_json["automate_bot_panel_cfg_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelCfgEmpty = _json["automate_bot_panel_cfg_empty"]["text"];
		if (!_json["automate_bot_panel_recipe_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeEmpty_l10n_key = _json["automate_bot_panel_recipe_empty"]["key"];
		if (!_json["automate_bot_panel_recipe_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeEmpty = _json["automate_bot_panel_recipe_empty"]["text"];
		if (!_json["automate_bot_panel_date_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelDateEmpty_l10n_key = _json["automate_bot_panel_date_empty"]["key"];
		if (!_json["automate_bot_panel_date_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelDateEmpty = _json["automate_bot_panel_date_empty"]["text"];
		if (!_json["automate_bot_panel_recipe_type"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeType_l10n_key = _json["automate_bot_panel_recipe_type"]["key"];
		if (!_json["automate_bot_panel_recipe_type"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeType = _json["automate_bot_panel_recipe_type"]["text"];
		if (!_json["automate_bot_panel_recipe_subType"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeSubType_l10n_key = _json["automate_bot_panel_recipe_subType"]["key"];
		if (!_json["automate_bot_panel_recipe_subType"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelRecipeSubType = _json["automate_bot_panel_recipe_subType"]["text"];
		if (!_json["automate_bot_panel_auto_fertilizer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelAutoFertilizer_l10n_key = _json["automate_bot_panel_auto_fertilizer"]["key"];
		if (!_json["automate_bot_panel_auto_fertilizer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelAutoFertilizer = _json["automate_bot_panel_auto_fertilizer"]["text"];
		if (!_json["automate_bot_panel_auto_protect"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelAutoProtect_l10n_key = _json["automate_bot_panel_auto_protect"]["key"];
		if (!_json["automate_bot_panel_auto_protect"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelAutoProtect = _json["automate_bot_panel_auto_protect"]["text"];
		if (!_json["automate_bot_panel_energy_type"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelEnergyType_l10n_key = _json["automate_bot_panel_energy_type"]["key"];
		if (!_json["automate_bot_panel_energy_type"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelEnergyType = _json["automate_bot_panel_energy_type"]["text"];
		if (!_json["automate_bot_panel_item_error"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelItemError_l10n_key = _json["automate_bot_panel_item_error"]["key"];
		if (!_json["automate_bot_panel_item_error"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AutomateBotPanelItemError = _json["automate_bot_panel_item_error"]["text"];
		if (!_json["board_mission_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionPanelTitle_l10n_key = _json["board_mission_panel_title"]["key"];
		if (!_json["board_mission_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionPanelTitle = _json["board_mission_panel_title"]["text"];
		if (!_json["board_mission_urgency_low"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyLow_l10n_key = _json["board_mission_urgency_low"]["key"];
		if (!_json["board_mission_urgency_low"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyLow = _json["board_mission_urgency_low"]["text"];
		if (!_json["board_mission_urgency_middle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyMiddle_l10n_key = _json["board_mission_urgency_middle"]["key"];
		if (!_json["board_mission_urgency_middle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyMiddle = _json["board_mission_urgency_middle"]["text"];
		if (!_json["board_mission_urgency_high"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyHigh_l10n_key = _json["board_mission_urgency_high"]["key"];
		if (!_json["board_mission_urgency_high"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyHigh = _json["board_mission_urgency_high"]["text"];
		if (!_json["board_mission_urgency_none"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyNone_l10n_key = _json["board_mission_urgency_none"]["key"];
		if (!_json["board_mission_urgency_none"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionUrgencyNone = _json["board_mission_urgency_none"]["text"];
		if (!_json["board_mission_accept_fail"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionAcceptFail_l10n_key = _json["board_mission_accept_fail"]["key"];
		if (!_json["board_mission_accept_fail"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionAcceptFail = _json["board_mission_accept_fail"]["text"];
		if (!_json["board_mission_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionTitle_l10n_key = _json["board_mission_title"]["key"];
		if (!_json["board_mission_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionTitle = _json["board_mission_title"]["text"];
		if (!_json["board_mission_low_level"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionLowLevel_l10n_key = _json["board_mission_low_level"]["key"];
		if (!_json["board_mission_low_level"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionLowLevel = _json["board_mission_low_level"]["text"];
		if (!_json["board_mission_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionEmpty_l10n_key = _json["board_mission_empty"]["key"];
		if (!_json["board_mission_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionEmpty = _json["board_mission_empty"]["text"];
		if (!_json["board_mission_overdue"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionOverdue_l10n_key = _json["board_mission_overdue"]["key"];
		if (!_json["board_mission_overdue"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionOverdue = _json["board_mission_overdue"]["text"];
		if (!_json["board_mission_lv"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionLv_l10n_key = _json["board_mission_lv"]["key"];
		if (!_json["board_mission_lv"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionLv = _json["board_mission_lv"]["text"];
		if (!_json["board_mission_exp_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionExpTip_l10n_key = _json["board_mission_exp_tip"]["key"];
		if (!_json["board_mission_exp_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoardMissionExpTip = _json["board_mission_exp_tip"]["text"];
		if (!_json["inventory_panel_cannot_put_in"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelCannotPutIn_l10n_key = _json["inventory_panel_cannot_put_in"]["key"];
		if (!_json["inventory_panel_cannot_put_in"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelCannotPutIn = _json["inventory_panel_cannot_put_in"]["text"];
		if (!_json["inventory_panel_container_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelContainerFull_l10n_key = _json["inventory_panel_container_full"]["key"];
		if (!_json["inventory_panel_container_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelContainerFull = _json["inventory_panel_container_full"]["text"];
		if (!_json["box_panel_rest_count"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelRestCount_l10n_key = _json["box_panel_rest_count"]["key"];
		if (!_json["box_panel_rest_count"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelRestCount = _json["box_panel_rest_count"]["text"];
		if (!_json["box_panel_used_up_warning"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelUsedUpWarning_l10n_key = _json["box_panel_used_up_warning"]["key"];
		if (!_json["box_panel_used_up_warning"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelUsedUpWarning = _json["box_panel_used_up_warning"]["text"];
		if (!_json["box_panel_broken"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelBroken_l10n_key = _json["box_panel_broken"]["key"];
		if (!_json["box_panel_broken"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelBroken = _json["box_panel_broken"]["text"];
		if (!_json["box_panel_already_open"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelAlreadyOpen_l10n_key = _json["box_panel_already_open"]["key"];
		if (!_json["box_panel_already_open"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelAlreadyOpen = _json["box_panel_already_open"]["text"];
		if (!_json["box_panel_no_need_repair"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelNoNeedRepair_l10n_key = _json["box_panel_no_need_repair"]["key"];
		if (!_json["box_panel_no_need_repair"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelNoNeedRepair = _json["box_panel_no_need_repair"]["text"];
		if (!_json["box_panel_no_repair_cost"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelNoRepairCost_l10n_key = _json["box_panel_no_repair_cost"]["key"];
		if (!_json["box_panel_no_repair_cost"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelNoRepairCost = _json["box_panel_no_repair_cost"]["text"];
		if (!_json["box_panel_repair_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelRepairInfo_l10n_key = _json["box_panel_repair_info"]["key"];
		if (!_json["box_panel_repair_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BoxPanelRepairInfo = _json["box_panel_repair_info"]["text"];
		if (!_json["inventory_panel_dungeon_case_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelDungeonCaseTitle_l10n_key = _json["inventory_panel_dungeon_case_title"]["key"];
		if (!_json["inventory_panel_dungeon_case_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelDungeonCaseTitle = _json["inventory_panel_dungeon_case_title"]["text"];
		if (!_json["inventory_panel_backpack_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelBackpackTitle_l10n_key = _json["inventory_panel_backpack_title"]["key"];
		if (!_json["inventory_panel_backpack_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelBackpackTitle = _json["inventory_panel_backpack_title"]["text"];
		if (!_json["fish_tank_panel_feed_quantity"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FishTankPanelFeedQuantity_l10n_key = _json["fish_tank_panel_feed_quantity"]["key"];
		if (!_json["fish_tank_panel_feed_quantity"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FishTankPanelFeedQuantity = _json["fish_tank_panel_feed_quantity"]["text"];
		if (!_json["inventory_panel_socket_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelSocketTitle_l10n_key = _json["inventory_panel_socket_title"]["key"];
		if (!_json["inventory_panel_socket_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelSocketTitle = _json["inventory_panel_socket_title"]["text"];
		if (!_json["inventory_panel_put_box_first"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelPutBoxFirst_l10n_key = _json["inventory_panel_put_box_first"]["key"];
		if (!_json["inventory_panel_put_box_first"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelPutBoxFirst = _json["inventory_panel_put_box_first"]["text"];
		if (!_json["inventory_panel_info_gene_incubator"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneIncubator_l10n_key = _json["inventory_panel_info_gene_incubator"]["key"];
		if (!_json["inventory_panel_info_gene_incubator"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneIncubator = _json["inventory_panel_info_gene_incubator"]["text"];
		if (!_json["inventory_panel_capsule_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelCapsuleTitle_l10n_key = _json["inventory_panel_capsule_title"]["key"];
		if (!_json["inventory_panel_capsule_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelCapsuleTitle = _json["inventory_panel_capsule_title"]["text"];
		if (!_json["inventory_panel_info_gene_replicator"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneReplicator_l10n_key = _json["inventory_panel_info_gene_replicator"]["key"];
		if (!_json["inventory_panel_info_gene_replicator"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneReplicator = _json["inventory_panel_info_gene_replicator"]["text"];
		if (!_json["inventory_panel_gene_replicator_locked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelGeneReplicatorLocked_l10n_key = _json["inventory_panel_gene_replicator_locked"]["key"];
		if (!_json["inventory_panel_gene_replicator_locked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelGeneReplicatorLocked = _json["inventory_panel_gene_replicator_locked"]["text"];
		if (!_json["inventory_panel_info_gene_synthesizer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneSynthesizer_l10n_key = _json["inventory_panel_info_gene_synthesizer"]["key"];
		if (!_json["inventory_panel_info_gene_synthesizer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelInfoGeneSynthesizer = _json["inventory_panel_info_gene_synthesizer"]["text"];
		if (!_json["inventory_panel_gene_synthesizer_locked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelGeneSynthesizerLocked_l10n_key = _json["inventory_panel_gene_synthesizer_locked"]["key"];
		if (!_json["inventory_panel_gene_synthesizer_locked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InventoryPanelGeneSynthesizerLocked = _json["inventory_panel_gene_synthesizer_locked"]["text"];
		if (!_json["ui_tip_time_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTimeTitle_l10n_key = _json["ui_tip_time_title"]["key"];
		if (!_json["ui_tip_time_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTimeTitle = _json["ui_tip_time_title"]["text"];
		if (!_json["ui_tip_time_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTimeFormat_l10n_key = _json["ui_tip_time_format"]["key"];
		if (!_json["ui_tip_time_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipTimeFormat = _json["ui_tip_time_format"]["text"];
		if (!_json["ui_tip_current_season"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSeason_l10n_key = _json["ui_tip_current_season"]["key"];
		if (!_json["ui_tip_current_season"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSeason = _json["ui_tip_current_season"]["text"];
		if (!_json["ui_tip_open_menu"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMenu_l10n_key = _json["ui_tip_open_menu"]["key"];
		if (!_json["ui_tip_open_menu"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMenu = _json["ui_tip_open_menu"]["text"];
		if (!_json["ui_tip_open_map"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMap_l10n_key = _json["ui_tip_open_map"]["key"];
		if (!_json["ui_tip_open_map"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMap = _json["ui_tip_open_map"]["text"];
		if (!_json["ui_tip_show_mission_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipShowMissionTip_l10n_key = _json["ui_tip_show_mission_tip"]["key"];
		if (!_json["ui_tip_show_mission_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipShowMissionTip = _json["ui_tip_show_mission_tip"]["text"];
		if (!_json["ui_tip_hide_mission_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHideMissionTip_l10n_key = _json["ui_tip_hide_mission_tip"]["key"];
		if (!_json["ui_tip_hide_mission_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHideMissionTip = _json["ui_tip_hide_mission_tip"]["text"];
		if (!_json["ui_tip_no_mission"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNoMission_l10n_key = _json["ui_tip_no_mission"]["key"];
		if (!_json["ui_tip_no_mission"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNoMission = _json["ui_tip_no_mission"]["text"];
		if (!_json["ui_tip_mission_show_details"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMissionShowDetails_l10n_key = _json["ui_tip_mission_show_details"]["key"];
		if (!_json["ui_tip_mission_show_details"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipMissionShowDetails = _json["ui_tip_mission_show_details"]["text"];
		if (!_json["ui_tip_health_value"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHealthValue_l10n_key = _json["ui_tip_health_value"]["key"];
		if (!_json["ui_tip_health_value"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHealthValue = _json["ui_tip_health_value"]["text"];
		if (!_json["ui_tip_energy_value"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEnergyValue_l10n_key = _json["ui_tip_energy_value"]["key"];
		if (!_json["ui_tip_energy_value"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEnergyValue = _json["ui_tip_energy_value"]["text"];
		if (!_json["ui_tip_corrosion_value"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCorrosionValue_l10n_key = _json["ui_tip_corrosion_value"]["key"];
		if (!_json["ui_tip_corrosion_value"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCorrosionValue = _json["ui_tip_corrosion_value"]["text"];
		if (!_json["ui_tip_current_spirit_le_50"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe50_l10n_key = _json["ui_tip_current_spirit_le_50"]["key"];
		if (!_json["ui_tip_current_spirit_le_50"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe50 = _json["ui_tip_current_spirit_le_50"]["text"];
		if (!_json["ui_tip_current_spirit_le_25"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe25_l10n_key = _json["ui_tip_current_spirit_le_25"]["key"];
		if (!_json["ui_tip_current_spirit_le_25"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe25 = _json["ui_tip_current_spirit_le_25"]["text"];
		if (!_json["ui_tip_current_spirit_le_10"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe10_l10n_key = _json["ui_tip_current_spirit_le_10"]["key"];
		if (!_json["ui_tip_current_spirit_le_10"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe10 = _json["ui_tip_current_spirit_le_10"]["text"];
		if (!_json["ui_tip_current_spirit_le_0"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe0_l10n_key = _json["ui_tip_current_spirit_le_0"]["key"];
		if (!_json["ui_tip_current_spirit_le_0"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentSpiritLe0 = _json["ui_tip_current_spirit_le_0"]["text"];
		if (!_json["ui_tip_buff_duration"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuffDuration_l10n_key = _json["ui_tip_buff_duration"]["key"];
		if (!_json["ui_tip_buff_duration"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuffDuration = _json["ui_tip_buff_duration"]["text"];
		if (!_json["ui_tip_debuff_duration"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDebuffDuration_l10n_key = _json["ui_tip_debuff_duration"]["key"];
		if (!_json["ui_tip_debuff_duration"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipDebuffDuration = _json["ui_tip_debuff_duration"]["text"];
		if (!_json["ui_tip_build_health"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuildHealth_l10n_key = _json["ui_tip_build_health"]["key"];
		if (!_json["ui_tip_build_health"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuildHealth = _json["ui_tip_build_health"]["text"];
		if (!_json["ui_tip_electricity_generator_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityGeneratorInfo_l10n_key = _json["ui_tip_electricity_generator_info"]["key"];
		if (!_json["ui_tip_electricity_generator_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityGeneratorInfo = _json["ui_tip_electricity_generator_info"]["text"];
		if (!_json["ui_tip_electricity_battery_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityBatteryInfo_l10n_key = _json["ui_tip_electricity_battery_info"]["key"];
		if (!_json["ui_tip_electricity_battery_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityBatteryInfo = _json["ui_tip_electricity_battery_info"]["text"];
		if (!_json["ui_tip_electricity_status_none"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusNone_l10n_key = _json["ui_tip_electricity_status_none"]["key"];
		if (!_json["ui_tip_electricity_status_none"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusNone = _json["ui_tip_electricity_status_none"]["text"];
		if (!_json["ui_tip_electricity_status_lack_of_generation"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusLackOfGeneration_l10n_key = _json["ui_tip_electricity_status_lack_of_generation"]["key"];
		if (!_json["ui_tip_electricity_status_lack_of_generation"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusLackOfGeneration = _json["ui_tip_electricity_status_lack_of_generation"]["text"];
		if (!_json["ui_tip_electricity_status_lack_of_generation_cost_battery"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusLackOfGenerationCostBattery_l10n_key = _json["ui_tip_electricity_status_lack_of_generation_cost_battery"]["key"];
		if (!_json["ui_tip_electricity_status_lack_of_generation_cost_battery"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusLackOfGenerationCostBattery = _json["ui_tip_electricity_status_lack_of_generation_cost_battery"]["text"];
		if (!_json["ui_tip_electricity_status_battery_saving"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusBatterySaving_l10n_key = _json["ui_tip_electricity_status_battery_saving"]["key"];
		if (!_json["ui_tip_electricity_status_battery_saving"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusBatterySaving = _json["ui_tip_electricity_status_battery_saving"]["text"];
		if (!_json["ui_tip_electricity_status_battery_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusBatteryFull_l10n_key = _json["ui_tip_electricity_status_battery_full"]["key"];
		if (!_json["ui_tip_electricity_status_battery_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusBatteryFull = _json["ui_tip_electricity_status_battery_full"]["text"];
		if (!_json["ui_tip_electricity_status_power_loss"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusPowerLoss_l10n_key = _json["ui_tip_electricity_status_power_loss"]["key"];
		if (!_json["ui_tip_electricity_status_power_loss"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipElectricityStatusPowerLoss = _json["ui_tip_electricity_status_power_loss"]["text"];
		if (!_json["ui_tip_seed_end"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSeedEnd_l10n_key = _json["ui_tip_seed_end"]["key"];
		if (!_json["ui_tip_seed_end"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipSeedEnd = _json["ui_tip_seed_end"]["text"];
		if (!_json["ui_tip_loading"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLoading_l10n_key = _json["ui_tip_loading"]["key"];
		if (!_json["ui_tip_loading"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipLoading = _json["ui_tip_loading"]["text"];
		if (!_json["document_title_computer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DocumentTitleComputer_l10n_key = _json["document_title_computer"]["key"];
		if (!_json["document_title_computer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DocumentTitleComputer = _json["document_title_computer"]["text"];
		if (!_json["npc_document_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		NpcDocumentTitle_l10n_key = _json["npc_document_title"]["key"];
		if (!_json["npc_document_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		NpcDocumentTitle = _json["npc_document_title"]["text"];
		if (!_json["chip_document_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ChipDocumentTitle_l10n_key = _json["chip_document_title"]["key"];
		if (!_json["chip_document_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ChipDocumentTitle = _json["chip_document_title"]["text"];
		if (!_json["plant_document_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		PlantDocumentTitle_l10n_key = _json["plant_document_title"]["key"];
		if (!_json["plant_document_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		PlantDocumentTitle = _json["plant_document_title"]["text"];
		if (!_json["document_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DocumentEmpty_l10n_key = _json["document_empty"]["key"];
		if (!_json["document_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DocumentEmpty = _json["document_empty"]["text"];
		if (!_json["chip_document_totle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ChipDocumentTotle_l10n_key = _json["chip_document_totle"]["key"];
		if (!_json["chip_document_totle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ChipDocumentTotle = _json["chip_document_totle"]["text"];
		if (!_json["game_data_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataPanelTitle_l10n_key = _json["game_data_panel_title"]["key"];
		if (!_json["game_data_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataPanelTitle = _json["game_data_panel_title"]["text"];
		if (!_json["game_data_err_read"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataErrRead_l10n_key = _json["game_data_err_read"]["key"];
		if (!_json["game_data_err_read"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataErrRead = _json["game_data_err_read"]["text"];
		if (!_json["game_data_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataTitle_l10n_key = _json["game_data_title"]["key"];
		if (!_json["game_data_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataTitle = _json["game_data_title"]["text"];
		if (!_json["game_data_del_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDelConfirm_l10n_key = _json["game_data_del_confirm"]["key"];
		if (!_json["game_data_del_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDelConfirm = _json["game_data_del_confirm"]["text"];
		if (!_json["game_data_del_success"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDelSuccess_l10n_key = _json["game_data_del_success"]["key"];
		if (!_json["game_data_del_success"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDelSuccess = _json["game_data_del_success"]["text"];
		if (!_json["game_data_err_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataErrEmpty_l10n_key = _json["game_data_err_empty"]["key"];
		if (!_json["game_data_err_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataErrEmpty = _json["game_data_err_empty"]["text"];
		if (!_json["game_data_saving"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaving_l10n_key = _json["game_data_saving"]["key"];
		if (!_json["game_data_saving"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaving = _json["game_data_saving"]["text"];
		if (!_json["game_data_save_fail"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaveFail_l10n_key = _json["game_data_save_fail"]["key"];
		if (!_json["game_data_save_fail"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaveFail = _json["game_data_save_fail"]["text"];
		if (!_json["game_data_save_successful"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaveSuccessful_l10n_key = _json["game_data_save_successful"]["key"];
		if (!_json["game_data_save_successful"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataSaveSuccessful = _json["game_data_save_successful"]["text"];
		if (!_json["game_data_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataFull_l10n_key = _json["game_data_full"]["key"];
		if (!_json["game_data_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataFull = _json["game_data_full"]["text"];
		if (!_json["game_data_duplicate_sucess"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDuplicateSucess_l10n_key = _json["game_data_duplicate_sucess"]["key"];
		if (!_json["game_data_duplicate_sucess"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataDuplicateSucess = _json["game_data_duplicate_sucess"]["text"];
		if (!_json["game_data_start"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataStart_l10n_key = _json["game_data_start"]["key"];
		if (!_json["game_data_start"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataStart = _json["game_data_start"]["text"];
		if (!_json["game_data_load"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GameDataLoad_l10n_key = _json["game_data_load"]["key"];
		if (!_json["game_data_load"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GameDataLoad = _json["game_data_load"]["text"];
		if (!_json["teleport_fail"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TeleportFail_l10n_key = _json["teleport_fail"]["key"];
		if (!_json["teleport_fail"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TeleportFail = _json["teleport_fail"]["text"];
		if (!_json["teleport_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TeleportConfirm_l10n_key = _json["teleport_confirm"]["key"];
		if (!_json["teleport_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TeleportConfirm = _json["teleport_confirm"]["text"];
		if (!_json["teleport_buy_ticket"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TeleportBuyTicket_l10n_key = _json["teleport_buy_ticket"]["key"];
		if (!_json["teleport_buy_ticket"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TeleportBuyTicket = _json["teleport_buy_ticket"]["text"];
		if (!_json["teleport_err_material_not_enough"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TeleportErrMaterialNotEnough_l10n_key = _json["teleport_err_material_not_enough"]["key"];
		if (!_json["teleport_err_material_not_enough"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TeleportErrMaterialNotEnough = _json["teleport_err_material_not_enough"]["text"];
		if (!_json["faction_mission_finish"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionFinish_l10n_key = _json["faction_mission_finish"]["key"];
		if (!_json["faction_mission_finish"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionFinish = _json["faction_mission_finish"]["text"];
		if (!_json["faction_mission_cannot_submit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionCannotSubmit_l10n_key = _json["faction_mission_cannot_submit"]["key"];
		if (!_json["faction_mission_cannot_submit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionCannotSubmit = _json["faction_mission_cannot_submit"]["text"];
		if (!_json["faction_mission_finish_submit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionFinishSubmit_l10n_key = _json["faction_mission_finish_submit"]["key"];
		if (!_json["faction_mission_finish_submit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionFinishSubmit = _json["faction_mission_finish_submit"]["text"];
		if (!_json["faction_mission_money_submit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionMoneySubmit_l10n_key = _json["faction_mission_money_submit"]["key"];
		if (!_json["faction_mission_money_submit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionMoneySubmit = _json["faction_mission_money_submit"]["text"];
		if (!_json["faction_mission_err_submit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionErrSubmit_l10n_key = _json["faction_mission_err_submit"]["key"];
		if (!_json["faction_mission_err_submit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionErrSubmit = _json["faction_mission_err_submit"]["text"];
		if (!_json["faction_mission_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionLock_l10n_key = _json["faction_mission_lock"]["key"];
		if (!_json["faction_mission_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		FactionMissionLock = _json["faction_mission_lock"]["text"];
		if (!_json["treaty_port_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortTitle_l10n_key = _json["treaty_port_title"]["key"];
		if (!_json["treaty_port_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortTitle = _json["treaty_port_title"]["text"];
		if (!_json["treaty_port_repair_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRepairTime_l10n_key = _json["treaty_port_repair_time"]["key"];
		if (!_json["treaty_port_repair_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRepairTime = _json["treaty_port_repair_time"]["text"];
		if (!_json["treaty_port_recruit_stats"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRecruitStats_l10n_key = _json["treaty_port_recruit_stats"]["key"];
		if (!_json["treaty_port_recruit_stats"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRecruitStats = _json["treaty_port_recruit_stats"]["text"];
		if (!_json["treaty_port_principal"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortPrincipal_l10n_key = _json["treaty_port_principal"]["key"];
		if (!_json["treaty_port_principal"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortPrincipal = _json["treaty_port_principal"]["text"];
		if (!_json["treaty_port_faction_mission_progress"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionMissionProgress_l10n_key = _json["treaty_port_faction_mission_progress"]["key"];
		if (!_json["treaty_port_faction_mission_progress"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionMissionProgress = _json["treaty_port_faction_mission_progress"]["text"];
		if (!_json["treaty_port_faction_reputation"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionReputation_l10n_key = _json["treaty_port_faction_reputation"]["key"];
		if (!_json["treaty_port_faction_reputation"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionReputation = _json["treaty_port_faction_reputation"]["text"];
		if (!_json["treaty_port_contact_npc"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortContactNpc_l10n_key = _json["treaty_port_contact_npc"]["key"];
		if (!_json["treaty_port_contact_npc"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortContactNpc = _json["treaty_port_contact_npc"]["text"];
		if (!_json["treaty_port_faction_enter_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionEnterTime_l10n_key = _json["treaty_port_faction_enter_time"]["key"];
		if (!_json["treaty_port_faction_enter_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionEnterTime = _json["treaty_port_faction_enter_time"]["text"];
		if (!_json["treaty_port_broadcast"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortBroadcast_l10n_key = _json["treaty_port_broadcast"]["key"];
		if (!_json["treaty_port_broadcast"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortBroadcast = _json["treaty_port_broadcast"]["text"];
		if (!_json["treaty_port_faction_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionLock_l10n_key = _json["treaty_port_faction_lock"]["key"];
		if (!_json["treaty_port_faction_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionLock = _json["treaty_port_faction_lock"]["text"];
		if (!_json["treaty_port_faction_unopen"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionUnopen_l10n_key = _json["treaty_port_faction_unopen"]["key"];
		if (!_json["treaty_port_faction_unopen"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionUnopen = _json["treaty_port_faction_unopen"]["text"];
		if (!_json["treaty_port_recruit_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRecruitHint_l10n_key = _json["treaty_port_recruit_hint"]["key"];
		if (!_json["treaty_port_recruit_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortRecruitHint = _json["treaty_port_recruit_hint"]["text"];
		if (!_json["treaty_port_time_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortTimeFormat_l10n_key = _json["treaty_port_time_format"]["key"];
		if (!_json["treaty_port_time_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortTimeFormat = _json["treaty_port_time_format"]["text"];
		if (!_json["treaty_port_faction_refuse"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionRefuse_l10n_key = _json["treaty_port_faction_refuse"]["key"];
		if (!_json["treaty_port_faction_refuse"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionRefuse = _json["treaty_port_faction_refuse"]["text"];
		if (!_json["treaty_port_off_duty_hours_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortOffDutyHoursTip_l10n_key = _json["treaty_port_off_duty_hours_tip"]["key"];
		if (!_json["treaty_port_off_duty_hours_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortOffDutyHoursTip = _json["treaty_port_off_duty_hours_tip"]["text"];
		if (!_json["treaty_port_interview_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortInterviewTip_l10n_key = _json["treaty_port_interview_tip"]["key"];
		if (!_json["treaty_port_interview_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortInterviewTip = _json["treaty_port_interview_tip"]["text"];
		if (!_json["treaty_port_faction_settled_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionSettledTip_l10n_key = _json["treaty_port_faction_settled_tip"]["key"];
		if (!_json["treaty_port_faction_settled_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortFactionSettledTip = _json["treaty_port_faction_settled_tip"]["text"];
		if (!_json["treaty_port_not_contacted_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortNotContactedTip_l10n_key = _json["treaty_port_not_contacted_tip"]["key"];
		if (!_json["treaty_port_not_contacted_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TreatyPortNotContactedTip = _json["treaty_port_not_contacted_tip"]["text"];
		if (!_json["setting_panel_save_successful"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelSaveSuccessful_l10n_key = _json["setting_panel_save_successful"]["key"];
		if (!_json["setting_panel_save_successful"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelSaveSuccessful = _json["setting_panel_save_successful"]["text"];
		if (!_json["setting_panel_other_save_successful"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelOtherSaveSuccessful_l10n_key = _json["setting_panel_other_save_successful"]["key"];
		if (!_json["setting_panel_other_save_successful"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelOtherSaveSuccessful = _json["setting_panel_other_save_successful"]["text"];
		if (!_json["setting_panel_reset"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelReset_l10n_key = _json["setting_panel_reset"]["key"];
		if (!_json["setting_panel_reset"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelReset = _json["setting_panel_reset"]["text"];
		if (!_json["setting_panel_reset_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelResetConfirm_l10n_key = _json["setting_panel_reset_confirm"]["key"];
		if (!_json["setting_panel_reset_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelResetConfirm = _json["setting_panel_reset_confirm"]["text"];
		if (!_json["setting_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelTitle_l10n_key = _json["setting_panel_title"]["key"];
		if (!_json["setting_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelTitle = _json["setting_panel_title"]["text"];
		if (!_json["setting_panel_exit_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelExitConfirm_l10n_key = _json["setting_panel_exit_confirm"]["key"];
		if (!_json["setting_panel_exit_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelExitConfirm = _json["setting_panel_exit_confirm"]["text"];
		if (!_json["setting_panel_conflict_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelConflictHint_l10n_key = _json["setting_panel_conflict_hint"]["key"];
		if (!_json["setting_panel_conflict_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelConflictHint = _json["setting_panel_conflict_hint"]["text"];
		if (!_json["setting_panel_reset_to_default"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelResetToDefault_l10n_key = _json["setting_panel_reset_to_default"]["key"];
		if (!_json["setting_panel_reset_to_default"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelResetToDefault = _json["setting_panel_reset_to_default"]["text"];
		if (!_json["setting_panel_can_not_edit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotEdit_l10n_key = _json["setting_panel_can_not_edit"]["key"];
		if (!_json["setting_panel_can_not_edit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotEdit = _json["setting_panel_can_not_edit"]["text"];
		if (!_json["setting_panel_can_not_remove"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotRemove_l10n_key = _json["setting_panel_can_not_remove"]["key"];
		if (!_json["setting_panel_can_not_remove"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotRemove = _json["setting_panel_can_not_remove"]["text"];
		if (!_json["setting_panel_no_valid_input"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelNoValidInput_l10n_key = _json["setting_panel_no_valid_input"]["key"];
		if (!_json["setting_panel_no_valid_input"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelNoValidInput = _json["setting_panel_no_valid_input"]["text"];
		if (!_json["setting_panel_allow_tracedata_collector"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelAllowTracedataCollector_l10n_key = _json["setting_panel_allow_tracedata_collector"]["key"];
		if (!_json["setting_panel_allow_tracedata_collector"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelAllowTracedataCollector = _json["setting_panel_allow_tracedata_collector"]["text"];
		if (!_json["setting_panel_tracedata_collector_desc"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelTracedataCollectorDesc_l10n_key = _json["setting_panel_tracedata_collector_desc"]["key"];
		if (!_json["setting_panel_tracedata_collector_desc"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelTracedataCollectorDesc = _json["setting_panel_tracedata_collector_desc"]["text"];
		if (!_json["setting_panel_delete"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelDelete_l10n_key = _json["setting_panel_delete"]["key"];
		if (!_json["setting_panel_delete"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelDelete = _json["setting_panel_delete"]["text"];
		if (!_json["setting_panel_can_not_save_by_conflict"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotSaveByConflict_l10n_key = _json["setting_panel_can_not_save_by_conflict"]["key"];
		if (!_json["setting_panel_can_not_save_by_conflict"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelCanNotSaveByConflict = _json["setting_panel_can_not_save_by_conflict"]["text"];
		if (!_json["setting_panel_new_device"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelNewDevice_l10n_key = _json["setting_panel_new_device"]["key"];
		if (!_json["setting_panel_new_device"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelNewDevice = _json["setting_panel_new_device"]["text"];
		if (!_json["setting_panel_loading"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelLoading_l10n_key = _json["setting_panel_loading"]["key"];
		if (!_json["setting_panel_loading"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SettingPanelLoading = _json["setting_panel_loading"]["text"];
		if (!_json["equipment_bar_time_label"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarTimeLabel_l10n_key = _json["equipment_bar_time_label"]["key"];
		if (!_json["equipment_bar_time_label"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarTimeLabel = _json["equipment_bar_time_label"]["text"];
		if (!_json["equipment_bar_time_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarTimeFormat_l10n_key = _json["equipment_bar_time_format"]["key"];
		if (!_json["equipment_bar_time_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarTimeFormat = _json["equipment_bar_time_format"]["text"];
		if (!_json["equipment_bar_drone_not_equip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDroneNotEquip_l10n_key = _json["equipment_bar_drone_not_equip"]["key"];
		if (!_json["equipment_bar_drone_not_equip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDroneNotEquip = _json["equipment_bar_drone_not_equip"]["text"];
		if (!_json["equipment_bar_motor_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarMotorTitle_l10n_key = _json["equipment_bar_motor_title"]["key"];
		if (!_json["equipment_bar_motor_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarMotorTitle = _json["equipment_bar_motor_title"]["text"];
		if (!_json["equipment_bar_motor_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarMotorLock_l10n_key = _json["equipment_bar_motor_lock"]["key"];
		if (!_json["equipment_bar_motor_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarMotorLock = _json["equipment_bar_motor_lock"]["text"];
		if (!_json["equipment_bar_hat_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarHatTip_l10n_key = _json["equipment_bar_hat_tip"]["key"];
		if (!_json["equipment_bar_hat_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarHatTip = _json["equipment_bar_hat_tip"]["text"];
		if (!_json["equipment_bar_positive_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPositiveTip_l10n_key = _json["equipment_bar_positive_tip"]["key"];
		if (!_json["equipment_bar_positive_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPositiveTip = _json["equipment_bar_positive_tip"]["text"];
		if (!_json["equipment_bar_passive1_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPassive1Tip_l10n_key = _json["equipment_bar_passive1_tip"]["key"];
		if (!_json["equipment_bar_passive1_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPassive1Tip = _json["equipment_bar_passive1_tip"]["text"];
		if (!_json["equipment_bar_passive2_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPassive2Tip_l10n_key = _json["equipment_bar_passive2_tip"]["key"];
		if (!_json["equipment_bar_passive2_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarPassive2Tip = _json["equipment_bar_passive2_tip"]["text"];
		if (!_json["equipment_bar_drone_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDroneTip_l10n_key = _json["equipment_bar_drone_tip"]["key"];
		if (!_json["equipment_bar_drone_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDroneTip = _json["equipment_bar_drone_tip"]["text"];
		if (!_json["equipment_bar_skill_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarSkillLock_l10n_key = _json["equipment_bar_skill_lock"]["key"];
		if (!_json["equipment_bar_skill_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarSkillLock = _json["equipment_bar_skill_lock"]["text"];
		if (!_json["equipment_bar_double_jump_desc"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDoubleJumpDesc_l10n_key = _json["equipment_bar_double_jump_desc"]["key"];
		if (!_json["equipment_bar_double_jump_desc"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarDoubleJumpDesc = _json["equipment_bar_double_jump_desc"]["text"];
		if (!_json["equipment_bar_sprint_desc"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarSprintDesc_l10n_key = _json["equipment_bar_sprint_desc"]["key"];
		if (!_json["equipment_bar_sprint_desc"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarSprintDesc = _json["equipment_bar_sprint_desc"]["text"];
		if (!_json["ability_double_jump_unlock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AbilityDoubleJumpUnlockTip_l10n_key = _json["ability_double_jump_unlock_tip"]["key"];
		if (!_json["ability_double_jump_unlock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AbilityDoubleJumpUnlockTip = _json["ability_double_jump_unlock_tip"]["text"];
		if (!_json["ability_sprint_unlock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AbilitySprintUnlockTip_l10n_key = _json["ability_sprint_unlock_tip"]["key"];
		if (!_json["ability_sprint_unlock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AbilitySprintUnlockTip = _json["ability_sprint_unlock_tip"]["text"];
		if (!_json["equipment_bar_backpack_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarBackpackFull_l10n_key = _json["equipment_bar_backpack_full"]["key"];
		if (!_json["equipment_bar_backpack_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentBarBackpackFull = _json["equipment_bar_backpack_full"]["text"];
		if (!_json["equipment_skill_prefix"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentSkillPrefix_l10n_key = _json["equipment_skill_prefix"]["key"];
		if (!_json["equipment_skill_prefix"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentSkillPrefix = _json["equipment_skill_prefix"]["text"];
		if (!_json["equipment_defense_prefix"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentDefensePrefix_l10n_key = _json["equipment_defense_prefix"]["key"];
		if (!_json["equipment_defense_prefix"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EquipmentDefensePrefix = _json["equipment_defense_prefix"]["text"];
		if (!_json["ui_tip_homepage_start_game"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageStartGame_l10n_key = _json["ui_tip_homepage_start_game"]["key"];
		if (!_json["ui_tip_homepage_start_game"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageStartGame = _json["ui_tip_homepage_start_game"]["text"];
		if (!_json["ui_tip_homepage_changelog"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageChangelog_l10n_key = _json["ui_tip_homepage_changelog"]["key"];
		if (!_json["ui_tip_homepage_changelog"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageChangelog = _json["ui_tip_homepage_changelog"]["text"];
		if (!_json["ui_tip_homepage_settings"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageSettings_l10n_key = _json["ui_tip_homepage_settings"]["key"];
		if (!_json["ui_tip_homepage_settings"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageSettings = _json["ui_tip_homepage_settings"]["text"];
		if (!_json["ui_tip_homepage_mods"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageMods_l10n_key = _json["ui_tip_homepage_mods"]["key"];
		if (!_json["ui_tip_homepage_mods"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageMods = _json["ui_tip_homepage_mods"]["text"];
		if (!_json["ui_tip_homepage_developer_list"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageDeveloperList_l10n_key = _json["ui_tip_homepage_developer_list"]["key"];
		if (!_json["ui_tip_homepage_developer_list"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageDeveloperList = _json["ui_tip_homepage_developer_list"]["text"];
		if (!_json["ui_tip_homepage_exit_game"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageExitGame_l10n_key = _json["ui_tip_homepage_exit_game"]["key"];
		if (!_json["ui_tip_homepage_exit_game"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipHomepageExitGame = _json["ui_tip_homepage_exit_game"]["text"];
		if (!_json["ui_tip_parking_apron_locked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipParkingApronLocked_l10n_key = _json["ui_tip_parking_apron_locked"]["key"];
		if (!_json["ui_tip_parking_apron_locked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipParkingApronLocked = _json["ui_tip_parking_apron_locked"]["text"];
		if (!_json["ui_tip_resolving"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipResolving_l10n_key = _json["ui_tip_resolving"]["key"];
		if (!_json["ui_tip_resolving"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipResolving = _json["ui_tip_resolving"]["text"];
		if (!_json["ui_tip_shredder_money"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipShredderMoney_l10n_key = _json["ui_tip_shredder_money"]["key"];
		if (!_json["ui_tip_shredder_money"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipShredderMoney = _json["ui_tip_shredder_money"]["text"];
		if (!_json["ui_tip_err_shredder_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipErrShredderEmpty_l10n_key = _json["ui_tip_err_shredder_empty"]["key"];
		if (!_json["ui_tip_err_shredder_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipErrShredderEmpty = _json["ui_tip_err_shredder_empty"]["text"];
		if (!_json["ui_tip_air_wall"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAirWall_l10n_key = _json["ui_tip_air_wall"]["key"];
		if (!_json["ui_tip_air_wall"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipAirWall = _json["ui_tip_air_wall"]["text"];
		if (!_json["ui_tip_not_available_to_motor"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNotAvailableToMotor_l10n_key = _json["ui_tip_not_available_to_motor"]["key"];
		if (!_json["ui_tip_not_available_to_motor"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNotAvailableToMotor = _json["ui_tip_not_available_to_motor"]["text"];
		if (!_json["collection_panel_item_label"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemLabel_l10n_key = _json["collection_panel_item_label"]["key"];
		if (!_json["collection_panel_item_label"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemLabel = _json["collection_panel_item_label"]["text"];
		if (!_json["collection_panel_item_recipe_time"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemRecipeTime_l10n_key = _json["collection_panel_item_recipe_time"]["key"];
		if (!_json["collection_panel_item_recipe_time"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemRecipeTime = _json["collection_panel_item_recipe_time"]["text"];
		if (!_json["collection_panel_item_recipe_empty"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemRecipeEmpty_l10n_key = _json["collection_panel_item_recipe_empty"]["key"];
		if (!_json["collection_panel_item_recipe_empty"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemRecipeEmpty = _json["collection_panel_item_recipe_empty"]["text"];
		if (!_json["collection_panel_item_unknown"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemUnknown_l10n_key = _json["collection_panel_item_unknown"]["key"];
		if (!_json["collection_panel_item_unknown"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemUnknown = _json["collection_panel_item_unknown"]["text"];
		if (!_json["collection_panel_item_source"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemSource_l10n_key = _json["collection_panel_item_source"]["key"];
		if (!_json["collection_panel_item_source"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelItemSource = _json["collection_panel_item_source"]["text"];
		if (!_json["collection_panel_npc_address"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcAddress_l10n_key = _json["collection_panel_npc_address"]["key"];
		if (!_json["collection_panel_npc_address"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcAddress = _json["collection_panel_npc_address"]["text"];
		if (!_json["collection_panel_npc_like_record"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikeRecord_l10n_key = _json["collection_panel_npc_like_record"]["key"];
		if (!_json["collection_panel_npc_like_record"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikeRecord = _json["collection_panel_npc_like_record"]["text"];
		if (!_json["collection_panel_npc_like_none"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikeNone_l10n_key = _json["collection_panel_npc_like_none"]["key"];
		if (!_json["collection_panel_npc_like_none"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikeNone = _json["collection_panel_npc_like_none"]["text"];
		if (!_json["collection_panel_npc_liking_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikingLock_l10n_key = _json["collection_panel_npc_liking_lock"]["key"];
		if (!_json["collection_panel_npc_liking_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikingLock = _json["collection_panel_npc_liking_lock"]["text"];
		if (!_json["collection_panel_npc_liking_lv_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikingLvLock_l10n_key = _json["collection_panel_npc_liking_lv_lock"]["key"];
		if (!_json["collection_panel_npc_liking_lv_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcLikingLvLock = _json["collection_panel_npc_liking_lv_lock"]["text"];
		if (!_json["collection_panel_npc_content_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentTitle_l10n_key = _json["collection_panel_npc_content_title"]["key"];
		if (!_json["collection_panel_npc_content_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentTitle = _json["collection_panel_npc_content_title"]["text"];
		if (!_json["collection_panel_npc_content_lock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentLockTip_l10n_key = _json["collection_panel_npc_content_lock_tip"]["key"];
		if (!_json["collection_panel_npc_content_lock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentLockTip = _json["collection_panel_npc_content_lock_tip"]["text"];
		if (!_json["collection_panel_npc_content_end"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentEnd_l10n_key = _json["collection_panel_npc_content_end"]["key"];
		if (!_json["collection_panel_npc_content_end"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelNpcContentEnd = _json["collection_panel_npc_content_end"]["text"];
		if (!_json["collection_panel_monster_unknown"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterUnknown_l10n_key = _json["collection_panel_monster_unknown"]["key"];
		if (!_json["collection_panel_monster_unknown"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterUnknown = _json["collection_panel_monster_unknown"]["text"];
		if (!_json["collection_panel_monster_habitat"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterHabitat_l10n_key = _json["collection_panel_monster_habitat"]["key"];
		if (!_json["collection_panel_monster_habitat"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterHabitat = _json["collection_panel_monster_habitat"]["text"];
		if (!_json["collection_panel_monster_drop_text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterDropText_l10n_key = _json["collection_panel_monster_drop_text"]["key"];
		if (!_json["collection_panel_monster_drop_text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterDropText = _json["collection_panel_monster_drop_text"]["text"];
		if (!_json["collection_panel_monster_organism"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterOrganism_l10n_key = _json["collection_panel_monster_organism"]["key"];
		if (!_json["collection_panel_monster_organism"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterOrganism = _json["collection_panel_monster_organism"]["text"];
		if (!_json["collection_panel_monster_machinery"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterMachinery_l10n_key = _json["collection_panel_monster_machinery"]["key"];
		if (!_json["collection_panel_monster_machinery"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterMachinery = _json["collection_panel_monster_machinery"]["text"];
		if (!_json["collection_panel_monster_boss"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterBoss_l10n_key = _json["collection_panel_monster_boss"]["key"];
		if (!_json["collection_panel_monster_boss"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterBoss = _json["collection_panel_monster_boss"]["text"];
		if (!_json["collection_panel_monster_content_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterContentTitle_l10n_key = _json["collection_panel_monster_content_title"]["key"];
		if (!_json["collection_panel_monster_content_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterContentTitle = _json["collection_panel_monster_content_title"]["text"];
		if (!_json["collection_panel_monster_content_lock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterContentLockTip_l10n_key = _json["collection_panel_monster_content_lock_tip"]["key"];
		if (!_json["collection_panel_monster_content_lock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelMonsterContentLockTip = _json["collection_panel_monster_content_lock_tip"]["text"];
		if (!_json["collection_panel_document_label"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelDocumentLabel_l10n_key = _json["collection_panel_document_label"]["key"];
		if (!_json["collection_panel_document_label"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelDocumentLabel = _json["collection_panel_document_label"]["text"];
		if (!_json["collection_panel_animal_possess"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalPossess_l10n_key = _json["collection_panel_animal_possess"]["key"];
		if (!_json["collection_panel_animal_possess"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalPossess = _json["collection_panel_animal_possess"]["text"];
		if (!_json["collection_panel_animal_breed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalBreed_l10n_key = _json["collection_panel_animal_breed"]["key"];
		if (!_json["collection_panel_animal_breed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalBreed = _json["collection_panel_animal_breed"]["text"];
		if (!_json["collection_panel_animal_product_text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalProductText_l10n_key = _json["collection_panel_animal_product_text"]["key"];
		if (!_json["collection_panel_animal_product_text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalProductText = _json["collection_panel_animal_product_text"]["text"];
		if (!_json["collection_panel_animal_content_lock_bring_up"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockBringUp_l10n_key = _json["collection_panel_animal_content_lock_bring_up"]["key"];
		if (!_json["collection_panel_animal_content_lock_bring_up"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockBringUp = _json["collection_panel_animal_content_lock_bring_up"]["text"];
		if (!_json["collection_panel_animal_content_lock_breed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockBreed_l10n_key = _json["collection_panel_animal_content_lock_breed"]["key"];
		if (!_json["collection_panel_animal_content_lock_breed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockBreed = _json["collection_panel_animal_content_lock_breed"]["text"];
		if (!_json["collection_panel_animal_content_lock_product"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockProduct_l10n_key = _json["collection_panel_animal_content_lock_product"]["key"];
		if (!_json["collection_panel_animal_content_lock_product"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelAnimalContentLockProduct = _json["collection_panel_animal_content_lock_product"]["text"];
		if (!_json["collection_panel_fish_catch"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishCatch_l10n_key = _json["collection_panel_fish_catch"]["key"];
		if (!_json["collection_panel_fish_catch"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishCatch = _json["collection_panel_fish_catch"]["text"];
		if (!_json["collection_panel_fish_bait_text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishBaitText_l10n_key = _json["collection_panel_fish_bait_text"]["key"];
		if (!_json["collection_panel_fish_bait_text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishBaitText = _json["collection_panel_fish_bait_text"]["text"];
		if (!_json["collection_panel_fish_place"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishPlace_l10n_key = _json["collection_panel_fish_place"]["key"];
		if (!_json["collection_panel_fish_place"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishPlace = _json["collection_panel_fish_place"]["text"];
		if (!_json["collection_panel_fish_month"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishMonth_l10n_key = _json["collection_panel_fish_month"]["key"];
		if (!_json["collection_panel_fish_month"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishMonth = _json["collection_panel_fish_month"]["text"];
		if (!_json["collection_panel_fish_weather"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishWeather_l10n_key = _json["collection_panel_fish_weather"]["key"];
		if (!_json["collection_panel_fish_weather"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishWeather = _json["collection_panel_fish_weather"]["text"];
		if (!_json["collection_panel_fish_weather_none"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishWeatherNone_l10n_key = _json["collection_panel_fish_weather_none"]["key"];
		if (!_json["collection_panel_fish_weather_none"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishWeatherNone = _json["collection_panel_fish_weather_none"]["text"];
		if (!_json["collection_panel_fish_content_lock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishContentLockTip_l10n_key = _json["collection_panel_fish_content_lock_tip"]["key"];
		if (!_json["collection_panel_fish_content_lock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelFishContentLockTip = _json["collection_panel_fish_content_lock_tip"]["text"];
		if (!_json["collection_panel_resource_collect"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceCollect_l10n_key = _json["collection_panel_resource_collect"]["key"];
		if (!_json["collection_panel_resource_collect"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceCollect = _json["collection_panel_resource_collect"]["text"];
		if (!_json["collection_panel_resource_growth_period"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceGrowthPeriod_l10n_key = _json["collection_panel_resource_growth_period"]["key"];
		if (!_json["collection_panel_resource_growth_period"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceGrowthPeriod = _json["collection_panel_resource_growth_period"]["text"];
		if (!_json["collection_panel_resource_year_round_growth"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceYearRoundGrowth_l10n_key = _json["collection_panel_resource_year_round_growth"]["key"];
		if (!_json["collection_panel_resource_year_round_growth"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceYearRoundGrowth = _json["collection_panel_resource_year_round_growth"]["text"];
		if (!_json["collection_panel_resource_drop_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceDropTitle_l10n_key = _json["collection_panel_resource_drop_title"]["key"];
		if (!_json["collection_panel_resource_drop_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceDropTitle = _json["collection_panel_resource_drop_title"]["text"];
		if (!_json["collection_panel_resource_content_lock_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceContentLockTip_l10n_key = _json["collection_panel_resource_content_lock_tip"]["key"];
		if (!_json["collection_panel_resource_content_lock_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CollectionPanelResourceContentLockTip = _json["collection_panel_resource_content_lock_tip"]["text"];
		if (!_json["builder_panel_label_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelBuilding_l10n_key = _json["builder_panel_label_building"]["key"];
		if (!_json["builder_panel_label_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelBuilding = _json["builder_panel_label_building"]["text"];
		if (!_json["builder_panel_label_equipment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelEquipment_l10n_key = _json["builder_panel_label_equipment"]["key"];
		if (!_json["builder_panel_label_equipment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelEquipment = _json["builder_panel_label_equipment"]["text"];
		if (!_json["builder_panel_label_platform"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelPlatform_l10n_key = _json["builder_panel_label_platform"]["key"];
		if (!_json["builder_panel_label_platform"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelLabelPlatform = _json["builder_panel_label_platform"]["text"];
		if (!_json["builder_panel_switch_terrain_layer"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelSwitchTerrainLayer_l10n_key = _json["builder_panel_switch_terrain_layer"]["key"];
		if (!_json["builder_panel_switch_terrain_layer"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelSwitchTerrainLayer = _json["builder_panel_switch_terrain_layer"]["text"];
		if (!_json["builder_action_selected_content"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSelectedContent_l10n_key = _json["builder_action_selected_content"]["key"];
		if (!_json["builder_action_selected_content"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSelectedContent = _json["builder_action_selected_content"]["text"];
		if (!_json["builder_action_undo"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionUndo_l10n_key = _json["builder_action_undo"]["key"];
		if (!_json["builder_action_undo"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionUndo = _json["builder_action_undo"]["text"];
		if (!_json["builder_action_turn"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionTurn_l10n_key = _json["builder_action_turn"]["key"];
		if (!_json["builder_action_turn"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionTurn = _json["builder_action_turn"]["text"];
		if (!_json["builder_action_dismantle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionDismantle_l10n_key = _json["builder_action_dismantle"]["key"];
		if (!_json["builder_action_dismantle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionDismantle = _json["builder_action_dismantle"]["text"];
		if (!_json["builder_action_building_dismantle"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionBuildingDismantle_l10n_key = _json["builder_action_building_dismantle"]["key"];
		if (!_json["builder_action_building_dismantle"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionBuildingDismantle = _json["builder_action_building_dismantle"]["text"];
		if (!_json["builder_action_building_storage"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionBuildingStorage_l10n_key = _json["builder_action_building_storage"]["key"];
		if (!_json["builder_action_building_storage"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionBuildingStorage = _json["builder_action_building_storage"]["text"];
		if (!_json["builder_action_move_camera"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionMoveCamera_l10n_key = _json["builder_action_move_camera"]["key"];
		if (!_json["builder_action_move_camera"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionMoveCamera = _json["builder_action_move_camera"]["text"];
		if (!_json["builder_action_move"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionMove_l10n_key = _json["builder_action_move"]["key"];
		if (!_json["builder_action_move"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionMove = _json["builder_action_move"]["text"];
		if (!_json["builder_action_selected_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSelectedItem_l10n_key = _json["builder_action_selected_item"]["key"];
		if (!_json["builder_action_selected_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSelectedItem = _json["builder_action_selected_item"]["text"];
		if (!_json["builder_action_precise_movement"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionPreciseMovement_l10n_key = _json["builder_action_precise_movement"]["key"];
		if (!_json["builder_action_precise_movement"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionPreciseMovement = _json["builder_action_precise_movement"]["text"];
		if (!_json["builder_action_switch_precise"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSwitchPrecise_l10n_key = _json["builder_action_switch_precise"]["key"];
		if (!_json["builder_action_switch_precise"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSwitchPrecise = _json["builder_action_switch_precise"]["text"];
		if (!_json["builder_action_switch_backpack"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSwitchBackpack_l10n_key = _json["builder_action_switch_backpack"]["key"];
		if (!_json["builder_action_switch_backpack"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionSwitchBackpack = _json["builder_action_switch_backpack"]["text"];
		if (!_json["builder_action_rolling_backpack"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionRollingBackpack_l10n_key = _json["builder_action_rolling_backpack"]["key"];
		if (!_json["builder_action_rolling_backpack"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionRollingBackpack = _json["builder_action_rolling_backpack"]["text"];
		if (!_json["builder_action_rolling_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionRollingItem_l10n_key = _json["builder_action_rolling_item"]["key"];
		if (!_json["builder_action_rolling_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionRollingItem = _json["builder_action_rolling_item"]["text"];
		if (!_json["builder_action_toggle_backpack"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionToggleBackpack_l10n_key = _json["builder_action_toggle_backpack"]["key"];
		if (!_json["builder_action_toggle_backpack"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionToggleBackpack = _json["builder_action_toggle_backpack"]["text"];
		if (!_json["builder_action_toggle_backpack_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionToggleBackpackGamepad_l10n_key = _json["builder_action_toggle_backpack_gamepad"]["key"];
		if (!_json["builder_action_toggle_backpack_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderActionToggleBackpackGamepad = _json["builder_action_toggle_backpack_gamepad"]["text"];
		if (!_json["builder_panel_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelExit_l10n_key = _json["builder_panel_exit"]["key"];
		if (!_json["builder_panel_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelExit = _json["builder_panel_exit"]["text"];
		if (!_json["builder_panel_undo_gamepad"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelUndoGamepad_l10n_key = _json["builder_panel_undo_gamepad"]["key"];
		if (!_json["builder_panel_undo_gamepad"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelUndoGamepad = _json["builder_panel_undo_gamepad"]["text"];
		if (!_json["builder_panel_expand_building_list"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelExpandBuildingList_l10n_key = _json["builder_panel_expand_building_list"]["key"];
		if (!_json["builder_panel_expand_building_list"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelExpandBuildingList = _json["builder_panel_expand_building_list"]["text"];
		if (!_json["builder_panell_fold_building_list"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanellFoldBuildingList_l10n_key = _json["builder_panell_fold_building_list"]["key"];
		if (!_json["builder_panell_fold_building_list"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanellFoldBuildingList = _json["builder_panell_fold_building_list"]["text"];
		if (!_json["builder_panel_finish"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelFinish_l10n_key = _json["builder_panel_finish"]["key"];
		if (!_json["builder_panel_finish"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelFinish = _json["builder_panel_finish"]["text"];
		if (!_json["builder_dismantle_building_err"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderDismantleBuildingErr_l10n_key = _json["builder_dismantle_building_err"]["key"];
		if (!_json["builder_dismantle_building_err"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderDismantleBuildingErr = _json["builder_dismantle_building_err"]["text"];
		if (!_json["builder_builder_construct"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderBuilderConstruct_l10n_key = _json["builder_builder_construct"]["key"];
		if (!_json["builder_builder_construct"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderBuilderConstruct = _json["builder_builder_construct"]["text"];
		if (!_json["builder_panel_temp_building_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelTempBuildingFull_l10n_key = _json["builder_panel_temp_building_full"]["key"];
		if (!_json["builder_panel_temp_building_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderPanelTempBuildingFull = _json["builder_panel_temp_building_full"]["text"];
		if (!_json["builder_exit_err_animal"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrAnimal_l10n_key = _json["builder_exit_err_animal"]["key"];
		if (!_json["builder_exit_err_animal"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrAnimal = _json["builder_exit_err_animal"]["text"];
		if (!_json["builder_exit_err_occupied"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrOccupied_l10n_key = _json["builder_exit_err_occupied"]["key"];
		if (!_json["builder_exit_err_occupied"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrOccupied = _json["builder_exit_err_occupied"]["text"];
		if (!_json["builder_exit_err_sole_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrSoleBuilding_l10n_key = _json["builder_exit_err_sole_building"]["key"];
		if (!_json["builder_exit_err_sole_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitErrSoleBuilding = _json["builder_exit_err_sole_building"]["text"];
		if (!_json["builder_exit_confirm"]["key"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitConfirm_l10n_key = _json["builder_exit_confirm"]["key"];
		if (!_json["builder_exit_confirm"]["text"].IsString)
		{
			throw new SerializationException();
		}
		BuilderExitConfirm = _json["builder_exit_confirm"]["text"];
		if (!_json["ui_env_optimizer_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerPanelTitle_l10n_key = _json["ui_env_optimizer_panel_title"]["key"];
		if (!_json["ui_env_optimizer_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerPanelTitle = _json["ui_env_optimizer_panel_title"]["text"];
		if (!_json["ui_env_optimizer_console_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerConsoleTitle_l10n_key = _json["ui_env_optimizer_console_title"]["key"];
		if (!_json["ui_env_optimizer_console_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerConsoleTitle = _json["ui_env_optimizer_console_title"]["text"];
		if (!_json["ui_env_optimizer_overview"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerOverview_l10n_key = _json["ui_env_optimizer_overview"]["key"];
		if (!_json["ui_env_optimizer_overview"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerOverview = _json["ui_env_optimizer_overview"]["text"];
		if (!_json["ui_env_optimizer_button_no_energy"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNoEnergy_l10n_key = _json["ui_env_optimizer_button_no_energy"]["key"];
		if (!_json["ui_env_optimizer_button_no_energy"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNoEnergy = _json["ui_env_optimizer_button_no_energy"]["text"];
		if (!_json["ui_env_optimizer_tip_no_energy"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerTipNoEnergy_l10n_key = _json["ui_env_optimizer_tip_no_energy"]["key"];
		if (!_json["ui_env_optimizer_tip_no_energy"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerTipNoEnergy = _json["ui_env_optimizer_tip_no_energy"]["text"];
		if (!_json["ui_env_optimizer_button_no_component"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNoComponent_l10n_key = _json["ui_env_optimizer_button_no_component"]["key"];
		if (!_json["ui_env_optimizer_button_no_component"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNoComponent = _json["ui_env_optimizer_button_no_component"]["text"];
		if (!_json["ui_env_optimizer_tip_no_component"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerTipNoComponent_l10n_key = _json["ui_env_optimizer_tip_no_component"]["key"];
		if (!_json["ui_env_optimizer_tip_no_component"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerTipNoComponent = _json["ui_env_optimizer_tip_no_component"]["text"];
		if (!_json["ui_env_optimizer_button_valid"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonValid_l10n_key = _json["ui_env_optimizer_button_valid"]["key"];
		if (!_json["ui_env_optimizer_button_valid"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonValid = _json["ui_env_optimizer_button_valid"]["text"];
		if (!_json["ui_env_optimizer_button_not_valid"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNotValid_l10n_key = _json["ui_env_optimizer_button_not_valid"]["key"];
		if (!_json["ui_env_optimizer_button_not_valid"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerButtonNotValid = _json["ui_env_optimizer_button_not_valid"]["text"];
		if (!_json["ui_env_optimizer_check_success"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerCheckSuccess_l10n_key = _json["ui_env_optimizer_check_success"]["key"];
		if (!_json["ui_env_optimizer_check_success"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerCheckSuccess = _json["ui_env_optimizer_check_success"]["text"];
		if (!_json["ui_env_optimizer_date_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerDateInfo_l10n_key = _json["ui_env_optimizer_date_info"]["key"];
		if (!_json["ui_env_optimizer_date_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerDateInfo = _json["ui_env_optimizer_date_info"]["text"];
		if (!_json["ui_env_optimizer_slot_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerSlotTitle_l10n_key = _json["ui_env_optimizer_slot_title"]["key"];
		if (!_json["ui_env_optimizer_slot_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerSlotTitle = _json["ui_env_optimizer_slot_title"]["text"];
		if (!_json["ui_env_optimizer_component_already_active"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerComponentAlreadyActive_l10n_key = _json["ui_env_optimizer_component_already_active"]["key"];
		if (!_json["ui_env_optimizer_component_already_active"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerComponentAlreadyActive = _json["ui_env_optimizer_component_already_active"]["text"];
		if (!_json["ui_env_optimizer_in_recognition"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerInRecognition_l10n_key = _json["ui_env_optimizer_in_recognition"]["key"];
		if (!_json["ui_env_optimizer_in_recognition"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerInRecognition = _json["ui_env_optimizer_in_recognition"]["text"];
		if (!_json["ui_env_optimizer_loading_data"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerLoadingData_l10n_key = _json["ui_env_optimizer_loading_data"]["key"];
		if (!_json["ui_env_optimizer_loading_data"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerLoadingData = _json["ui_env_optimizer_loading_data"]["text"];
		if (!_json["ui_env_optimizer_active_success"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerActiveSuccess_l10n_key = _json["ui_env_optimizer_active_success"]["key"];
		if (!_json["ui_env_optimizer_active_success"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiEnvOptimizerActiveSuccess = _json["ui_env_optimizer_active_success"]["text"];
		if (!_json["animal_invalid_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInvalidBuilding_l10n_key = _json["animal_invalid_building"]["key"];
		if (!_json["animal_invalid_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInvalidBuilding = _json["animal_invalid_building"]["text"];
		if (!_json["animal_invalid_room"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInvalidRoom_l10n_key = _json["animal_invalid_room"]["key"];
		if (!_json["animal_invalid_room"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInvalidRoom = _json["animal_invalid_room"]["text"];
		if (!_json["animal_full_building"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalFullBuilding_l10n_key = _json["animal_full_building"]["key"];
		if (!_json["animal_full_building"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalFullBuilding = _json["animal_full_building"]["text"];
		if (!_json["animal_input_animal_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInputAnimalName_l10n_key = _json["animal_input_animal_name"]["key"];
		if (!_json["animal_input_animal_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInputAnimalName = _json["animal_input_animal_name"]["text"];
		if (!_json["animal_package_is_full"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalPackageIsFull_l10n_key = _json["animal_package_is_full"]["key"];
		if (!_json["animal_package_is_full"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalPackageIsFull = _json["animal_package_is_full"]["text"];
		if (!_json["animal_has_escaped"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalHasEscaped_l10n_key = _json["animal_has_escaped"]["key"];
		if (!_json["animal_has_escaped"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalHasEscaped = _json["animal_has_escaped"]["text"];
		if (!_json["animal_no_animal"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalNoAnimal_l10n_key = _json["animal_no_animal"]["key"];
		if (!_json["animal_no_animal"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalNoAnimal = _json["animal_no_animal"]["text"];
		if (!_json["animal_in_use"]["key"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInUse_l10n_key = _json["animal_in_use"]["key"];
		if (!_json["animal_in_use"]["text"].IsString)
		{
			throw new SerializationException();
		}
		AnimalInUse = _json["animal_in_use"]["text"];
		if (!_json["calendar_panel_year_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelYearTitle_l10n_key = _json["calendar_panel_year_title"]["key"];
		if (!_json["calendar_panel_year_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelYearTitle = _json["calendar_panel_year_title"]["text"];
		if (!_json["calendar_panel_date_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelDateTitle_l10n_key = _json["calendar_panel_date_title"]["key"];
		if (!_json["calendar_panel_date_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelDateTitle = _json["calendar_panel_date_title"]["text"];
		if (!_json["calendar_panel_event_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelEventTitle_l10n_key = _json["calendar_panel_event_title"]["key"];
		if (!_json["calendar_panel_event_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelEventTitle = _json["calendar_panel_event_title"]["text"];
		if (!_json["calendar_panel_player_birthday"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelPlayerBirthday_l10n_key = _json["calendar_panel_player_birthday"]["key"];
		if (!_json["calendar_panel_player_birthday"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelPlayerBirthday = _json["calendar_panel_player_birthday"]["text"];
		if (!_json["calendar_panel_memo_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelMemoTitle_l10n_key = _json["calendar_panel_memo_title"]["key"];
		if (!_json["calendar_panel_memo_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelMemoTitle = _json["calendar_panel_memo_title"]["text"];
		if (!_json["calendar_panel_empty_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelEmptyHint_l10n_key = _json["calendar_panel_empty_hint"]["key"];
		if (!_json["calendar_panel_empty_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelEmptyHint = _json["calendar_panel_empty_hint"]["text"];
		if (!_json["calendar_panel_del_memo_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelDelMemoHint_l10n_key = _json["calendar_panel_del_memo_hint"]["key"];
		if (!_json["calendar_panel_del_memo_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelDelMemoHint = _json["calendar_panel_del_memo_hint"]["text"];
		if (!_json["calendar_panel_input_empty_hint"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelInputEmptyHint_l10n_key = _json["calendar_panel_input_empty_hint"]["key"];
		if (!_json["calendar_panel_input_empty_hint"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarPanelInputEmptyHint = _json["calendar_panel_input_empty_hint"]["text"];
		if (!_json["input_text_contains_sensitive_world"]["key"].IsString)
		{
			throw new SerializationException();
		}
		InputTextContainsSensitiveWorld_l10n_key = _json["input_text_contains_sensitive_world"]["key"];
		if (!_json["input_text_contains_sensitive_world"]["text"].IsString)
		{
			throw new SerializationException();
		}
		InputTextContainsSensitiveWorld = _json["input_text_contains_sensitive_world"]["text"];
		if (!_json["calendar_memo_message"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CalendarMemoMessage_l10n_key = _json["calendar_memo_message"]["key"];
		if (!_json["calendar_memo_message"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CalendarMemoMessage = _json["calendar_memo_message"]["text"];
		if (!_json["ui_crop_info_growth_level"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiCropInfoGrowthLevel_l10n_key = _json["ui_crop_info_growth_level"]["key"];
		if (!_json["ui_crop_info_growth_level"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiCropInfoGrowthLevel = _json["ui_crop_info_growth_level"]["text"];
		if (!_json["ui_crop_info_harvest_count"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiCropInfoHarvestCount_l10n_key = _json["ui_crop_info_harvest_count"]["key"];
		if (!_json["ui_crop_info_harvest_count"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiCropInfoHarvestCount = _json["ui_crop_info_harvest_count"]["text"];
		if (!_json["ui_animal_info_state"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalInfoState_l10n_key = _json["ui_animal_info_state"]["key"];
		if (!_json["ui_animal_info_state"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalInfoState = _json["ui_animal_info_state"]["text"];
		if (!_json["ui_animal_space"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalSpace_l10n_key = _json["ui_animal_space"]["key"];
		if (!_json["ui_animal_space"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalSpace = _json["ui_animal_space"]["text"];
		if (!_json["ui_animal_age_year"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeYear_l10n_key = _json["ui_animal_age_year"]["key"];
		if (!_json["ui_animal_age_year"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeYear = _json["ui_animal_age_year"]["text"];
		if (!_json["ui_animal_age_month"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeMonth_l10n_key = _json["ui_animal_age_month"]["key"];
		if (!_json["ui_animal_age_month"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeMonth = _json["ui_animal_age_month"]["text"];
		if (!_json["ui_animal_age_day"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeDay_l10n_key = _json["ui_animal_age_day"]["key"];
		if (!_json["ui_animal_age_day"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAgeDay = _json["ui_animal_age_day"]["text"];
		if (!_json["ui_animal_birthday"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalBirthday_l10n_key = _json["ui_animal_birthday"]["key"];
		if (!_json["ui_animal_birthday"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalBirthday = _json["ui_animal_birthday"]["text"];
		if (!_json["ui_animal_capacity"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalCapacity_l10n_key = _json["ui_animal_capacity"]["key"];
		if (!_json["ui_animal_capacity"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalCapacity = _json["ui_animal_capacity"]["text"];
		if (!_json["ui_animal_call_back"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalCallBack_l10n_key = _json["ui_animal_call_back"]["key"];
		if (!_json["ui_animal_call_back"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalCallBack = _json["ui_animal_call_back"]["text"];
		if (!_json["ui_animal_let_out"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalLetOut_l10n_key = _json["ui_animal_let_out"]["key"];
		if (!_json["ui_animal_let_out"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalLetOut = _json["ui_animal_let_out"]["text"];
		if (!_json["ui_animal_adult"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAdult_l10n_key = _json["ui_animal_adult"]["key"];
		if (!_json["ui_animal_adult"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalAdult = _json["ui_animal_adult"]["text"];
		if (!_json["ui_animal_child"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalChild_l10n_key = _json["ui_animal_child"]["key"];
		if (!_json["ui_animal_child"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalChild = _json["ui_animal_child"]["text"];
		if (!_json["ui_animal_position"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalPosition_l10n_key = _json["ui_animal_position"]["key"];
		if (!_json["ui_animal_position"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalPosition = _json["ui_animal_position"]["text"];
		if (!_json["ui_animal_no_animal"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalNoAnimal_l10n_key = _json["ui_animal_no_animal"]["key"];
		if (!_json["ui_animal_no_animal"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalNoAnimal = _json["ui_animal_no_animal"]["text"];
		if (!_json["ui_animal_not_visible"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalNotVisible_l10n_key = _json["ui_animal_not_visible"]["key"];
		if (!_json["ui_animal_not_visible"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiAnimalNotVisible = _json["ui_animal_not_visible"]["text"];
		if (!_json["drone_panel_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelTitle_l10n_key = _json["drone_panel_title"]["key"];
		if (!_json["drone_panel_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelTitle = _json["drone_panel_title"]["text"];
		if (!_json["drone_panel_err_load"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelErrLoad_l10n_key = _json["drone_panel_err_load"]["key"];
		if (!_json["drone_panel_err_load"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelErrLoad = _json["drone_panel_err_load"]["text"];
		if (!_json["drone_panel_err_locked"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelErrLocked_l10n_key = _json["drone_panel_err_locked"]["key"];
		if (!_json["drone_panel_err_locked"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DronePanelErrLocked = _json["drone_panel_err_locked"]["text"];
		if (!_json["drone_component_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DroneComponentTitle_l10n_key = _json["drone_component_title"]["key"];
		if (!_json["drone_component_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DroneComponentTitle = _json["drone_component_title"]["text"];
		if (!_json["garbage_submit_err_item"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GarbageSubmitErrItem_l10n_key = _json["garbage_submit_err_item"]["key"];
		if (!_json["garbage_submit_err_item"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GarbageSubmitErrItem = _json["garbage_submit_err_item"]["text"];
		if (!_json["dispose_fail_room"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DisposeFailRoom_l10n_key = _json["dispose_fail_room"]["key"];
		if (!_json["dispose_fail_room"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DisposeFailRoom = _json["dispose_fail_room"]["text"];
		if (!_json["ui_tip_open_map_fail"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMapFail_l10n_key = _json["ui_tip_open_map_fail"]["key"];
		if (!_json["ui_tip_open_map_fail"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipOpenMapFail = _json["ui_tip_open_map_fail"]["text"];
		if (!_json["ui_tip_none"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNone_l10n_key = _json["ui_tip_none"]["key"];
		if (!_json["ui_tip_none"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipNone = _json["ui_tip_none"]["text"];
		if (!_json["ui_tip_buy_backpack"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyBackpack_l10n_key = _json["ui_tip_buy_backpack"]["key"];
		if (!_json["ui_tip_buy_backpack"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyBackpack = _json["ui_tip_buy_backpack"]["text"];
		if (!_json["ui_tip_buy_backpack_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyBackpackInfo_l10n_key = _json["ui_tip_buy_backpack_info"]["key"];
		if (!_json["ui_tip_buy_backpack_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipBuyBackpackInfo = _json["ui_tip_buy_backpack_info"]["text"];
		if (!_json["ui_tip_empty_list"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmptyList_l10n_key = _json["ui_tip_empty_list"]["key"];
		if (!_json["ui_tip_empty_list"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEmptyList = _json["ui_tip_empty_list"]["text"];
		if (!_json["ui_tip_current_position"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentPosition_l10n_key = _json["ui_tip_current_position"]["key"];
		if (!_json["ui_tip_current_position"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentPosition = _json["ui_tip_current_position"]["text"];
		if (!_json["ui_tip_current_motor_position"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentMotorPosition_l10n_key = _json["ui_tip_current_motor_position"]["key"];
		if (!_json["ui_tip_current_motor_position"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipCurrentMotorPosition = _json["ui_tip_current_motor_position"]["text"];
		if (!_json["ui_option_shooting_range_start"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeStart_l10n_key = _json["ui_option_shooting_range_start"]["key"];
		if (!_json["ui_option_shooting_range_start"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeStart = _json["ui_option_shooting_range_start"]["text"];
		if (!_json["ui_option_shooting_range_end"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeEnd_l10n_key = _json["ui_option_shooting_range_end"]["key"];
		if (!_json["ui_option_shooting_range_end"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeEnd = _json["ui_option_shooting_range_end"]["text"];
		if (!_json["ui_tip_equipment_unlock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEquipmentUnlock_l10n_key = _json["ui_tip_equipment_unlock"]["key"];
		if (!_json["ui_tip_equipment_unlock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipEquipmentUnlock = _json["ui_tip_equipment_unlock"]["text"];
		if (!_json["ui_option_shooting_range_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeExit_l10n_key = _json["ui_option_shooting_range_exit"]["key"];
		if (!_json["ui_option_shooting_range_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionShootingRangeExit = _json["ui_option_shooting_range_exit"]["text"];
		if (!_json["ui_option_confirm_shooting_range_exit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionConfirmShootingRangeExit_l10n_key = _json["ui_option_confirm_shooting_range_exit"]["key"];
		if (!_json["ui_option_confirm_shooting_range_exit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiOptionConfirmShootingRangeExit = _json["ui_option_confirm_shooting_range_exit"]["text"];
		if (!_json["ui_npc_visit"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcVisit_l10n_key = _json["ui_npc_visit"]["key"];
		if (!_json["ui_npc_visit"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcVisit = _json["ui_npc_visit"]["text"];
		if (!_json["ui_npc_file_updation"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcFileUpdation_l10n_key = _json["ui_npc_file_updation"]["key"];
		if (!_json["ui_npc_file_updation"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcFileUpdation = _json["ui_npc_file_updation"]["text"];
		if (!_json["ui_npc_liking_rise"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcLikingRise_l10n_key = _json["ui_npc_liking_rise"]["key"];
		if (!_json["ui_npc_liking_rise"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcLikingRise = _json["ui_npc_liking_rise"]["text"];
		if (!_json["ui_npc_liking_decline"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcLikingDecline_l10n_key = _json["ui_npc_liking_decline"]["key"];
		if (!_json["ui_npc_liking_decline"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiNpcLikingDecline = _json["ui_npc_liking_decline"]["text"];
		if (!_json["item_eden_fruit_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemEdenFruitTip_l10n_key = _json["item_eden_fruit_tip"]["key"];
		if (!_json["item_eden_fruit_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemEdenFruitTip = _json["item_eden_fruit_tip"]["text"];
		if (!_json["ui_tip_game_version"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipGameVersion_l10n_key = _json["ui_tip_game_version"]["key"];
		if (!_json["ui_tip_game_version"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipGameVersion = _json["ui_tip_game_version"]["text"];
		if (!_json["ui_tip_content_lock"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipContentLock_l10n_key = _json["ui_tip_content_lock"]["key"];
		if (!_json["ui_tip_content_lock"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipContentLock = _json["ui_tip_content_lock"]["text"];
		if (!_json["ui_tip_photo_saving"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPhotoSaving_l10n_key = _json["ui_tip_photo_saving"]["key"];
		if (!_json["ui_tip_photo_saving"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipPhotoSaving = _json["ui_tip_photo_saving"]["text"];
		if (!_json["ui_item_generate_electricity_entry"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiItemGenerateElectricityEntry_l10n_key = _json["ui_item_generate_electricity_entry"]["key"];
		if (!_json["ui_item_generate_electricity_entry"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiItemGenerateElectricityEntry = _json["ui_item_generate_electricity_entry"]["text"];
		if (!_json["ui_tip_rename"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiTipRename_l10n_key = _json["ui_tip_rename"]["key"];
		if (!_json["ui_tip_rename"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiTipRename = _json["ui_tip_rename"]["text"];
		if (!_json["ui_mod_no_mod"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModNoMod_l10n_key = _json["ui_mod_no_mod"]["key"];
		if (!_json["ui_mod_no_mod"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModNoMod = _json["ui_mod_no_mod"]["text"];
		if (!_json["ui_mod_need_subscribe"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModNeedSubscribe_l10n_key = _json["ui_mod_need_subscribe"]["key"];
		if (!_json["ui_mod_need_subscribe"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModNeedSubscribe = _json["ui_mod_need_subscribe"]["text"];
		if (!_json["ui_mod_open_workshop"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModOpenWorkshop_l10n_key = _json["ui_mod_open_workshop"]["key"];
		if (!_json["ui_mod_open_workshop"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModOpenWorkshop = _json["ui_mod_open_workshop"]["text"];
		if (!_json["ui_mod_need_create"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModNeedCreate_l10n_key = _json["ui_mod_need_create"]["key"];
		if (!_json["ui_mod_need_create"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModNeedCreate = _json["ui_mod_need_create"]["text"];
		if (!_json["ui_mod_open_local_directory"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModOpenLocalDirectory_l10n_key = _json["ui_mod_open_local_directory"]["key"];
		if (!_json["ui_mod_open_local_directory"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModOpenLocalDirectory = _json["ui_mod_open_local_directory"]["text"];
		if (!_json["ui_mod_upload_mod"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadMod_l10n_key = _json["ui_mod_upload_mod"]["key"];
		if (!_json["ui_mod_upload_mod"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadMod = _json["ui_mod_upload_mod"]["text"];
		if (!_json["ui_mod_update_mod"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateMod_l10n_key = _json["ui_mod_update_mod"]["key"];
		if (!_json["ui_mod_update_mod"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateMod = _json["ui_mod_update_mod"]["text"];
		if (!_json["ui_mod_enable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModEnable_l10n_key = _json["ui_mod_enable"]["key"];
		if (!_json["ui_mod_enable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModEnable = _json["ui_mod_enable"]["text"];
		if (!_json["ui_mod_disable"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModDisable_l10n_key = _json["ui_mod_disable"]["key"];
		if (!_json["ui_mod_disable"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModDisable = _json["ui_mod_disable"]["text"];
		if (!_json["ui_mod_confirm_upload"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModConfirmUpload_l10n_key = _json["ui_mod_confirm_upload"]["key"];
		if (!_json["ui_mod_confirm_upload"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModConfirmUpload = _json["ui_mod_confirm_upload"]["text"];
		if (!_json["ui_mod_confirm_update"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModConfirmUpdate_l10n_key = _json["ui_mod_confirm_update"]["key"];
		if (!_json["ui_mod_confirm_update"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModConfirmUpdate = _json["ui_mod_confirm_update"]["text"];
		if (!_json["ui_mod_uploading"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploading_l10n_key = _json["ui_mod_uploading"]["key"];
		if (!_json["ui_mod_uploading"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploading = _json["ui_mod_uploading"]["text"];
		if (!_json["ui_mod_upload_success"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadSuccess_l10n_key = _json["ui_mod_upload_success"]["key"];
		if (!_json["ui_mod_upload_success"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadSuccess = _json["ui_mod_upload_success"]["text"];
		if (!_json["ui_mod_upload_failed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadFailed_l10n_key = _json["ui_mod_upload_failed"]["key"];
		if (!_json["ui_mod_upload_failed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUploadFailed = _json["ui_mod_upload_failed"]["text"];
		if (!_json["ui_mod_updating"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdating_l10n_key = _json["ui_mod_updating"]["key"];
		if (!_json["ui_mod_updating"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdating = _json["ui_mod_updating"]["text"];
		if (!_json["ui_mod_update_success"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateSuccess_l10n_key = _json["ui_mod_update_success"]["key"];
		if (!_json["ui_mod_update_success"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateSuccess = _json["ui_mod_update_success"]["text"];
		if (!_json["ui_mod_update_failed"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateFailed_l10n_key = _json["ui_mod_update_failed"]["key"];
		if (!_json["ui_mod_update_failed"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModUpdateFailed = _json["ui_mod_update_failed"]["text"];
		if (!_json["ui_mod_reloading"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModReloading_l10n_key = _json["ui_mod_reloading"]["key"];
		if (!_json["ui_mod_reloading"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModReloading = _json["ui_mod_reloading"]["text"];
		if (!_json["ui_mod_source_local"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModSourceLocal_l10n_key = _json["ui_mod_source_local"]["key"];
		if (!_json["ui_mod_source_local"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModSourceLocal = _json["ui_mod_source_local"]["text"];
		if (!_json["ui_mod_source_workshop"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UiModSourceWorkshop_l10n_key = _json["ui_mod_source_workshop"]["key"];
		if (!_json["ui_mod_source_workshop"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UiModSourceWorkshop = _json["ui_mod_source_workshop"]["text"];
	}

	public StaticTextInfo(string sys_err_datalost, string ui_money_tip, string ui_item_simple_tip, string ui_item_tip, string ui_tip_consumable, string ui_err_material_not_enough, string ui_err_money_not_enough, string ui_err_backpack_is_full, string ui_err_not_dispose_item, string ui_ques_dispose_item, string ui_menu_itemnotavaiable, string ui_backpack_cell_exchange, string ui_err_backpack_cell_exchange, string ui_sort_default, string ui_sort_unlock, string ui_lose_item_tip, string ui_lose_money_tip, string ui_unlock_seed, string ui_analyzer_time, string ui_analyzer_output, string ui_analyzer_total_time, string ui_shredder_confirm, string ui_shredder_confirm_upgrade, string ui_submit_confirm, string ui_submit_confirm_gene, string ui_submit_confirm_money, string ui_submit_not_enough, string ui_gift_item_confirm, string ui_tip_dismentle, string ui_tip_plant_err, string ui_text_years, string ui_text_months, string ui_text_days, string ui_text_hours, string ui_text_minutes, string ui_text_time_format, string ui_text_demo_statement, string ui_text_gene_description, string input_title_player_name, string input_title_container_name, string input_name_empty, string input_name_confirm, string input_name_contains_sensitive_world, string input_title_player_birthday, string input_birthday_confirm, string ui_savepoint_default, string ui_savepoint_sleep, string ui_savepoint_nap, string ui_savepoint_kill_time, string ui_savepoint_sit, string ui_savepoint_exit, string ui_savepoint_cancel, string ui_tip_confirm, string ui_tip_cancel, string ui_tip_quit, string ui_tip_switch_classifying, string ui_tip_sort, string ui_tip_upgrade, string ui_tip_buy, string ui_tip_sell_confirm, string ui_tip_sell, string ui_tip_buy_confirm, string ui_tip_unlock, string ui_tip_unlock_seed, string ui_tip_launch_drone, string ui_tip_switch_automate_bot, string ui_tip_automate_bot_switch, string ui_tip_automate_bot_unload, string ui_tip_automate_bot_load, string ui_tip_choice, string ui_tip_submit_all, string ui_tip_submit, string ui_tip_load_game, string ui_tip_del, string ui_tip_lock_slot, string ui_tip_tidy, string ui_tip_dispose, string ui_tip_destroy_item, string ui_tip_switch_lable, string ui_tip_put_all, string ui_tip_put_max, string ui_tip_take_out_all, string ui_tip_take_out_max, string ui_tip_put_all_tap, string ui_tip_take_out_one, string ui_tip_take_out_half, string ui_tip_take_out_one_gamepad, string ui_tip_put, string ui_tip_take_out, string ui_tip_put_gamepad, string ui_tip_take_out_gamepad, string ui_tip_pick_up_gamepad, string ui_tip_quick_put_one, string ui_tip_quick_put_all, string ui_tip_quick_take_one, string ui_tip_quick_take_all, string ui_tip_quick_equipment, string ui_tip_switch_box, string ui_tip_sub_one, string ui_tip_add_one, string ui_tip_sub_ten, string ui_tip_add_ten, string ui_tip_set_min, string ui_tip_set_max, string ui_tip_broadcast, string ui_tip_make_one, string ui_tip_make_all, string ui_tip_buy_one, string ui_tip_buy_all_gamepad, string ui_tip_sell_one, string ui_tip_sell_all, string ui_tip_prev_filter, string ui_tip_next_filter, string ui_tip_capture, string ui_tip_collection, string ui_tip_history, string ui_tip_change_name, string ui_tip_take_out_selected, string ui_tip_put_one_selected, string ui_tip_put_all_selected, string ui_tip_add_memo, string ui_tip_change_memo, string ui_tip_del_memo, string ui_tip_switch_map_size, string ui_tip_map_centered, string ui_tip_email_recycle, string ui_tip_email_default, string ui_tip_techtree_point_focus, string ui_tip_techtree_focus, string ui_tip_battery_low, string ui_tip_err_take_boat, string ui_operation_talk, string ui_operation_talk_unknown, string ui_operation_interact, string ui_operation_sleep, string ui_operation_pick, string ui_operation_well, string ui_operation_harvest, string ui_operation_clear, string ui_operation_open, string ui_operation_open_door, string ui_operation_close_door, string ui_operation_enter, string ui_operation_enter_format, string ui_operation_exit, string ui_operation_view, string ui_operation_use, string ui_operation_sit, string ui_operation_display, string ui_operation_takeoff, string ui_operation_dump, string ui_operation_disembark, string ui_operation_call_boat, string ui_operation_shower, string ui_operation_open_box, string ui_operation_storage_shelf, string ui_operation_fuel_in, string ui_operation_close, string ui_operation_start, string ui_operation_fill, string ui_operation_fondle, string ui_operation_start_something, string ui_operation_use_something, string ui_operation_err_not_seed, string ui_operation_err_invalid_plantbasin, string ui_operation_err_invalid_season, string ui_operation_err_lack_of_asset, string ui_operation_err_lack_of_energy, string ui_operation_err_cannot_plant_tree, string ui_operation_err_cannot_plant_on_ground, string ui_operation_err_empty_drone, string ui_operation_err_well_full, string ui_operation_err_water_full, string ui_operation_err_runout_water, string ui_operation_err_runout_water_around, string ui_operation_err_equipment_corroded, string ui_operation_err_low_tool_level, string ui_operation_err_full_plastic_film, string ui_operation_err_cannot_fertilizer, string ui_operation_err_drone_full_battery, string ui_operation_err_water_evaporated, string ui_operation_err_fail_to_place_box, string ui_operation_err_cannot_display, string ui_operation_err_cannot_use_if_riding, string ui_operation_err_cannot_call_motor, string ui_operation_err_sword_cannot_autofire, string ui_operation_err_low_power, string ui_operation_err_cooling, string ui_operation_err_barrel_full, string ui_operation_err_fuel_full, string ui_operation_err_not_feeds, string ui_operation_err_feeder_full, string ui_operation_err_slot_full, string ui_operation_err_no_suitable_equipments, string ui_storage_shelf_full, string ui_operation_weather_has_changed, string ui_operation_err_fuel, string ui_operation_gift_item_fail, string ui_operation_make_equipment, string ui_operation_sell, string ui_operation_switch_auto_fire, string ui_operation_switch_manual_fire, string ui_operation_collect, string ui_operation_ride, string ui_operation_no_animal_building, string ui_operation_err_no_plant_basin, string ui_operation_err_no_building_here, string ui_operation_err_invalid_wallpaper, string ui_operation_err_same_wallpaper, string ui_operation_err_should_inhouse, string ui_operation_err_should_outside, string store_player_money_not_enough, string store_store_money_not_enough, string store_item_not_saleable, string store_item_sold_out, string store_lack_of_asset, string store_upgrade_price, string store_upgrade_confirm, string store_sold_out_icon, string store_upgrade_empty_info, string store_item_not_saleable_comment, string store_quantity_submit_selling, string store_quantity_submit_current_money_selling, string store_quantity_submit_total_money_selling, string store_quantity_submit_buying, string store_quantity_submit_current_money_buying, string store_quantity_submit_total_money_buying, string store_quantity_submit_count_in_back, string store_quantity_submit_unit_price, string store_item_tip_plantbasin_locked, string farmbuilder_err_build_system_not_support, string farmbuilder_err_lack_of_asset, string farmbuilder_err_indoor_equipment, string farmbuilder_err_outdoor_equipment, string farmbuilder_err_invalid_position, string farmbuilder_err_out_of_range, string farmbuilder_err_equipment_area_not_empty, string farmbuilder_err_platform_occupied, string farmbuilder_question_remove, string farmbuilder_question_remove_return, string farmbuilder_err_equipment_occupied, string farmbuilder_err_plantbasin_tree_occupied, string farmbuilder_err_parking_apron_occupied, string farmbuilder_err_automate_bot_occupied, string farmbuilder_err_show_case_occupied, string farmbuilder_err_platform_remove, string farmbuilder_err_equipment_hide_door, string farmbuilder_err_building_occupied, string farmbuilder_err_building_occupied_animal, string farmbuilder_err_building_area_not_empty, string farmbuilder_err_building_invalid_height, string farmbuilder_err_building_invalid_covered_ratio, string farmbuilder_err_building_can_not_remove, string farmbuilder_err_building_invalid_other_no_support, string farmbuilder_err_building_occupied_building, string farmbuilder_err_building_invalid_door_be_hidden, string farmbuilder_err_building_occupied_ceiling, string farmbuilder_err_building_occupied_door, string farmbuilder_err_building_occupied_ceiling_pt, string farmbuilder_err_building_ground_invalid, string farmbuilder_err_platform_width, string farmbuilder_err_platform_height_max, string farmbuilder_err_equipment_limited, string techtree_panel_title, string techtree_node_lack_of_points, string techtree_node_not_avaiable, string techtree_node_unlocked, string techtree_node_unopen, string techtree_node_unlock_confirm, string techtree_node_building_health, string techtree_node_equipment_electronic, string techtree_progress_info, string techtree_max_level, string techtree_node_hint, string mission_update, string mission_complete, string mission_complete_send_email, string mission_panel_title, string mission_panel_empty, string ui_mission_time_limit, string ui_mission_npc_position, string seed_panel_title, string seed_node_already_unlock, string seed_node_succeed_unlock, string building_panel_title, string building_panel_cover_size_description, string building_panel_inner_size_description, string building_panel_animal_capacity_description, string building_panel_size_description, string building_panel_empty, string building_panel_start_build, string building_panel_material_not_enough, string building_panel_money_not_enough, string building_panel_input_building_name, string equipment_panel_empty, string equipment_panel_start_build, string equipment_panel_list_name, string equipment_viewer_empty, string equipment_panel_lately_empty, string equipment_panel_make_complete, string equipment_panel_hide, string equipment_panel_jump, string equipment_panel_unlock_tip, string equipment_panel_electronic_tip, string equipment_panel_battery_tip, string equipment_panel_appliance_tip, string equipment_panel_generator_tip, string ui_email_title, string ui_email_empty, string ui_email_accept_mission, string ui_email_already_accepted, string ui_email_recive_item, string ui_email_already_recived, string ui_email_recycle_err, string recipe_panel_empty, string recipe_panel_start_build, string recipe_panel_time_info, string recipe_panel_already_working, string recipe_panel_title, string recipe_panel_task, string recipe_panel_rest_time, string recipe_panel_max_craft_count, string recipe_panel_limit_up, string recipe_panel_not_cookable, string recipe_panel_no_material, string recipe_panel_confirm_with_time, string recipe_panel_unlocked_recipe_comment, string recipe_panel_unlocked_dish_comment, string recipe_panel_unknow_dish_comment, string recipe_panel_confirm_pop_buffer, string recipe_panel_material_list, string recipe_panel_exist_items, string recipe_panel_random_gene_hint, string platform_panel_title, string platform_panel_empty, string platform_panel_start_build, string platform_panel_cost_prefix, string item_building_proto_error, string item_equipment_proto_error, string item_send_email_on_overflow, string item_sleeping_bag_condition_failed_monster, string item_sleeping_bag_condition_failed_water, string item_rescue_pager_condition_failed, string item_tip_price, string item_tip_basics_price, string item_tip_money_unit, string item_chip_attack_increase, string item_chip_attack_increase_fixed, string item_chip_critical_rate_increase, string item_chip_attack_speed_increase, string item_chip_accuracy_increase, string item_chip_power_cost_decrease, string item_chip_attack_distance_increase, string item_chip_move_speed_increase, string item_chip_clip_capacity_addition, string item_chip_reload_duration_decrease, string item_engine_move_speed_increase, string item_engine_power_capacity_increase, string item_engine_power_recv_increase, string item_structure_move_speed_increase, string item_structure_power_capacity, string item_structure_power_recv, string item_max_durability, string item_title_info_format, string item_confirm_use, string item_water_can_area, string item_water_can_endless_water, string item_gene_desc_format, string item_seed_cloned, string item_wp_is_available_for, string item_wp_available_all, string item_fish_fry_title_format, string item_tool_level_format, string item_patch_value_format, string item_box_value_format, string item_film_value_format, string item_drone_weapon_value_format, string item_money_title, string item_money_desc, string item_money_type, string ui_system_exit_game, string ui_system_return_home_page, string ui_system_confirm_home, string ui_system_confirm_exit, string reward_info_building_unlock, string reward_info_bus_station_unlock, string reward_info_equipment_unlock, string reward_info_platform_unlock, string reward_info_recipe_unlock, string reward_info_recipe_unlock_hint, string reward_info_item, string reward_info_gold, string reward_info_favorability_lv1, string reward_info_favorability_lv2, string reward_info_favorability_lv3, string dropoff_box_no_goods, string dropoff_box_no_drone, string dropoff_box_total_money, string dropoff_box_price_increased, string dropoff_box_launch_drone, string dropoff_box_confirm_lauch, string automate_bot_panel_state_idle, string automate_bot_panel_state_charge, string automate_bot_panel_state_pause, string automate_bot_panel_state_working, string automate_bot_panel_cfg_empty, string automate_bot_panel_recipe_empty, string automate_bot_panel_date_empty, string automate_bot_panel_recipe_type, string automate_bot_panel_recipe_subType, string automate_bot_panel_auto_fertilizer, string automate_bot_panel_auto_protect, string automate_bot_panel_energy_type, string automate_bot_panel_item_error, string board_mission_panel_title, string board_mission_urgency_low, string board_mission_urgency_middle, string board_mission_urgency_high, string board_mission_urgency_none, string board_mission_accept_fail, string board_mission_title, string board_mission_low_level, string board_mission_empty, string board_mission_overdue, string board_mission_lv, string board_mission_exp_tip, string inventory_panel_cannot_put_in, string inventory_panel_container_full, string box_panel_rest_count, string box_panel_used_up_warning, string box_panel_broken, string box_panel_already_open, string box_panel_no_need_repair, string box_panel_no_repair_cost, string box_panel_repair_info, string inventory_panel_dungeon_case_title, string inventory_panel_backpack_title, string fish_tank_panel_feed_quantity, string inventory_panel_socket_title, string inventory_panel_put_box_first, string inventory_panel_info_gene_incubator, string inventory_panel_capsule_title, string inventory_panel_info_gene_replicator, string inventory_panel_gene_replicator_locked, string inventory_panel_info_gene_synthesizer, string inventory_panel_gene_synthesizer_locked, string ui_tip_time_title, string ui_tip_time_format, string ui_tip_current_season, string ui_tip_open_menu, string ui_tip_open_map, string ui_tip_show_mission_tip, string ui_tip_hide_mission_tip, string ui_tip_no_mission, string ui_tip_mission_show_details, string ui_tip_health_value, string ui_tip_energy_value, string ui_tip_corrosion_value, string ui_tip_current_spirit_le_50, string ui_tip_current_spirit_le_25, string ui_tip_current_spirit_le_10, string ui_tip_current_spirit_le_0, string ui_tip_buff_duration, string ui_tip_debuff_duration, string ui_tip_build_health, string ui_tip_electricity_generator_info, string ui_tip_electricity_battery_info, string ui_tip_electricity_status_none, string ui_tip_electricity_status_lack_of_generation, string ui_tip_electricity_status_lack_of_generation_cost_battery, string ui_tip_electricity_status_battery_saving, string ui_tip_electricity_status_battery_full, string ui_tip_electricity_status_power_loss, string ui_tip_seed_end, string ui_tip_loading, string document_title_computer, string npc_document_title, string chip_document_title, string plant_document_title, string document_empty, string chip_document_totle, string game_data_panel_title, string game_data_err_read, string game_data_title, string game_data_del_confirm, string game_data_del_success, string game_data_err_empty, string game_data_saving, string game_data_save_fail, string game_data_save_successful, string game_data_full, string game_data_duplicate_sucess, string game_data_start, string game_data_load, string teleport_fail, string teleport_confirm, string teleport_buy_ticket, string teleport_err_material_not_enough, string faction_mission_finish, string faction_mission_cannot_submit, string faction_mission_finish_submit, string faction_mission_money_submit, string faction_mission_err_submit, string faction_mission_lock, string treaty_port_title, string treaty_port_repair_time, string treaty_port_recruit_stats, string treaty_port_principal, string treaty_port_faction_mission_progress, string treaty_port_faction_reputation, string treaty_port_contact_npc, string treaty_port_faction_enter_time, string treaty_port_broadcast, string treaty_port_faction_lock, string treaty_port_faction_unopen, string treaty_port_recruit_hint, string treaty_port_time_format, string treaty_port_faction_refuse, string treaty_port_off_duty_hours_tip, string treaty_port_interview_tip, string treaty_port_faction_settled_tip, string treaty_port_not_contacted_tip, string setting_panel_save_successful, string setting_panel_other_save_successful, string setting_panel_reset, string setting_panel_reset_confirm, string setting_panel_title, string setting_panel_exit_confirm, string setting_panel_conflict_hint, string setting_panel_reset_to_default, string setting_panel_can_not_edit, string setting_panel_can_not_remove, string setting_panel_no_valid_input, string setting_panel_allow_tracedata_collector, string setting_panel_tracedata_collector_desc, string setting_panel_delete, string setting_panel_can_not_save_by_conflict, string setting_panel_new_device, string setting_panel_loading, string equipment_bar_time_label, string equipment_bar_time_format, string equipment_bar_drone_not_equip, string equipment_bar_motor_title, string equipment_bar_motor_lock, string equipment_bar_hat_tip, string equipment_bar_positive_tip, string equipment_bar_passive1_tip, string equipment_bar_passive2_tip, string equipment_bar_drone_tip, string equipment_bar_skill_lock, string equipment_bar_double_jump_desc, string equipment_bar_sprint_desc, string ability_double_jump_unlock_tip, string ability_sprint_unlock_tip, string equipment_bar_backpack_full, string equipment_skill_prefix, string equipment_defense_prefix, string ui_tip_homepage_start_game, string ui_tip_homepage_changelog, string ui_tip_homepage_settings, string ui_tip_homepage_mods, string ui_tip_homepage_developer_list, string ui_tip_homepage_exit_game, string ui_tip_parking_apron_locked, string ui_tip_resolving, string ui_tip_shredder_money, string ui_tip_err_shredder_empty, string ui_tip_air_wall, string ui_tip_not_available_to_motor, string collection_panel_item_label, string collection_panel_item_recipe_time, string collection_panel_item_recipe_empty, string collection_panel_item_unknown, string collection_panel_item_source, string collection_panel_npc_address, string collection_panel_npc_like_record, string collection_panel_npc_like_none, string collection_panel_npc_liking_lock, string collection_panel_npc_liking_lv_lock, string collection_panel_npc_content_title, string collection_panel_npc_content_lock_tip, string collection_panel_npc_content_end, string collection_panel_monster_unknown, string collection_panel_monster_habitat, string collection_panel_monster_drop_text, string collection_panel_monster_organism, string collection_panel_monster_machinery, string collection_panel_monster_boss, string collection_panel_monster_content_title, string collection_panel_monster_content_lock_tip, string collection_panel_document_label, string collection_panel_animal_possess, string collection_panel_animal_breed, string collection_panel_animal_product_text, string collection_panel_animal_content_lock_bring_up, string collection_panel_animal_content_lock_breed, string collection_panel_animal_content_lock_product, string collection_panel_fish_catch, string collection_panel_fish_bait_text, string collection_panel_fish_place, string collection_panel_fish_month, string collection_panel_fish_weather, string collection_panel_fish_weather_none, string collection_panel_fish_content_lock_tip, string collection_panel_resource_collect, string collection_panel_resource_growth_period, string collection_panel_resource_year_round_growth, string collection_panel_resource_drop_title, string collection_panel_resource_content_lock_tip, string builder_panel_label_building, string builder_panel_label_equipment, string builder_panel_label_platform, string builder_panel_switch_terrain_layer, string builder_action_selected_content, string builder_action_undo, string builder_action_turn, string builder_action_dismantle, string builder_action_building_dismantle, string builder_action_building_storage, string builder_action_move_camera, string builder_action_move, string builder_action_selected_item, string builder_action_precise_movement, string builder_action_switch_precise, string builder_action_switch_backpack, string builder_action_rolling_backpack, string builder_action_rolling_item, string builder_action_toggle_backpack, string builder_action_toggle_backpack_gamepad, string builder_panel_exit, string builder_panel_undo_gamepad, string builder_panel_expand_building_list, string builder_panell_fold_building_list, string builder_panel_finish, string builder_dismantle_building_err, string builder_builder_construct, string builder_panel_temp_building_full, string builder_exit_err_animal, string builder_exit_err_occupied, string builder_exit_err_sole_building, string builder_exit_confirm, string ui_env_optimizer_panel_title, string ui_env_optimizer_console_title, string ui_env_optimizer_overview, string ui_env_optimizer_button_no_energy, string ui_env_optimizer_tip_no_energy, string ui_env_optimizer_button_no_component, string ui_env_optimizer_tip_no_component, string ui_env_optimizer_button_valid, string ui_env_optimizer_button_not_valid, string ui_env_optimizer_check_success, string ui_env_optimizer_date_info, string ui_env_optimizer_slot_title, string ui_env_optimizer_component_already_active, string ui_env_optimizer_in_recognition, string ui_env_optimizer_loading_data, string ui_env_optimizer_active_success, string animal_invalid_building, string animal_invalid_room, string animal_full_building, string animal_input_animal_name, string animal_package_is_full, string animal_has_escaped, string animal_no_animal, string animal_in_use, string calendar_panel_year_title, string calendar_panel_date_title, string calendar_panel_event_title, string calendar_panel_player_birthday, string calendar_panel_memo_title, string calendar_panel_empty_hint, string calendar_panel_del_memo_hint, string calendar_panel_input_empty_hint, string input_text_contains_sensitive_world, string calendar_memo_message, string ui_crop_info_growth_level, string ui_crop_info_harvest_count, string ui_animal_info_state, string ui_animal_space, string ui_animal_age_year, string ui_animal_age_month, string ui_animal_age_day, string ui_animal_birthday, string ui_animal_capacity, string ui_animal_call_back, string ui_animal_let_out, string ui_animal_adult, string ui_animal_child, string ui_animal_position, string ui_animal_no_animal, string ui_animal_not_visible, string drone_panel_title, string drone_panel_err_load, string drone_panel_err_locked, string drone_component_title, string garbage_submit_err_item, string dispose_fail_room, string ui_tip_open_map_fail, string ui_tip_none, string ui_tip_buy_backpack, string ui_tip_buy_backpack_info, string ui_tip_empty_list, string ui_tip_current_position, string ui_tip_current_motor_position, string ui_option_shooting_range_start, string ui_option_shooting_range_end, string ui_tip_equipment_unlock, string ui_option_shooting_range_exit, string ui_option_confirm_shooting_range_exit, string ui_npc_visit, string ui_npc_file_updation, string ui_npc_liking_rise, string ui_npc_liking_decline, string item_eden_fruit_tip, string ui_tip_game_version, string ui_tip_content_lock, string ui_tip_photo_saving, string ui_item_generate_electricity_entry, string ui_tip_rename, string ui_mod_no_mod, string ui_mod_need_subscribe, string ui_mod_open_workshop, string ui_mod_need_create, string ui_mod_open_local_directory, string ui_mod_upload_mod, string ui_mod_update_mod, string ui_mod_enable, string ui_mod_disable, string ui_mod_confirm_upload, string ui_mod_confirm_update, string ui_mod_uploading, string ui_mod_upload_success, string ui_mod_upload_failed, string ui_mod_updating, string ui_mod_update_success, string ui_mod_update_failed, string ui_mod_reloading, string ui_mod_source_local, string ui_mod_source_workshop)
	{
		SysErrDatalost = sys_err_datalost;
		UiMoneyTip = ui_money_tip;
		UiItemSimpleTip = ui_item_simple_tip;
		UiItemTip = ui_item_tip;
		UiTipConsumable = ui_tip_consumable;
		UiErrMaterialNotEnough = ui_err_material_not_enough;
		UiErrMoneyNotEnough = ui_err_money_not_enough;
		UiErrBackpackIsFull = ui_err_backpack_is_full;
		UiErrNotDisposeItem = ui_err_not_dispose_item;
		UiQuesDisposeItem = ui_ques_dispose_item;
		UiMenuItemnotavaiable = ui_menu_itemnotavaiable;
		UiBackpackCellExchange = ui_backpack_cell_exchange;
		UiErrBackpackCellExchange = ui_err_backpack_cell_exchange;
		UiSortDefault = ui_sort_default;
		UiSortUnlock = ui_sort_unlock;
		UiLoseItemTip = ui_lose_item_tip;
		UiLoseMoneyTip = ui_lose_money_tip;
		UiUnlockSeed = ui_unlock_seed;
		UiAnalyzerTime = ui_analyzer_time;
		UiAnalyzerOutput = ui_analyzer_output;
		UiAnalyzerTotalTime = ui_analyzer_total_time;
		UiShredderConfirm = ui_shredder_confirm;
		UiShredderConfirmUpgrade = ui_shredder_confirm_upgrade;
		UiSubmitConfirm = ui_submit_confirm;
		UiSubmitConfirmGene = ui_submit_confirm_gene;
		UiSubmitConfirmMoney = ui_submit_confirm_money;
		UiSubmitNotEnough = ui_submit_not_enough;
		UiGiftItemConfirm = ui_gift_item_confirm;
		UiTipDismentle = ui_tip_dismentle;
		UiTipPlantErr = ui_tip_plant_err;
		UiTextYears = ui_text_years;
		UiTextMonths = ui_text_months;
		UiTextDays = ui_text_days;
		UiTextHours = ui_text_hours;
		UiTextMinutes = ui_text_minutes;
		UiTextTimeFormat = ui_text_time_format;
		UiTextDemoStatement = ui_text_demo_statement;
		UiTextGeneDescription = ui_text_gene_description;
		InputTitlePlayerName = input_title_player_name;
		InputTitleContainerName = input_title_container_name;
		InputNameEmpty = input_name_empty;
		InputNameConfirm = input_name_confirm;
		InputNameContainsSensitiveWorld = input_name_contains_sensitive_world;
		InputTitlePlayerBirthday = input_title_player_birthday;
		InputBirthdayConfirm = input_birthday_confirm;
		UiSavepointDefault = ui_savepoint_default;
		UiSavepointSleep = ui_savepoint_sleep;
		UiSavepointNap = ui_savepoint_nap;
		UiSavepointKillTime = ui_savepoint_kill_time;
		UiSavepointSit = ui_savepoint_sit;
		UiSavepointExit = ui_savepoint_exit;
		UiSavepointCancel = ui_savepoint_cancel;
		UiTipConfirm = ui_tip_confirm;
		UiTipCancel = ui_tip_cancel;
		UiTipQuit = ui_tip_quit;
		UiTipSwitchClassifying = ui_tip_switch_classifying;
		UiTipSort = ui_tip_sort;
		UiTipUpgrade = ui_tip_upgrade;
		UiTipBuy = ui_tip_buy;
		UiTipSellConfirm = ui_tip_sell_confirm;
		UiTipSell = ui_tip_sell;
		UiTipBuyConfirm = ui_tip_buy_confirm;
		UiTipUnlock = ui_tip_unlock;
		UiTipUnlockSeed = ui_tip_unlock_seed;
		UiTipLaunchDrone = ui_tip_launch_drone;
		UiTipSwitchAutomateBot = ui_tip_switch_automate_bot;
		UiTipAutomateBotSwitch = ui_tip_automate_bot_switch;
		UiTipAutomateBotUnload = ui_tip_automate_bot_unload;
		UiTipAutomateBotLoad = ui_tip_automate_bot_load;
		UiTipChoice = ui_tip_choice;
		UiTipSubmitAll = ui_tip_submit_all;
		UiTipSubmit = ui_tip_submit;
		UiTipLoadGame = ui_tip_load_game;
		UiTipDel = ui_tip_del;
		UiTipLockSlot = ui_tip_lock_slot;
		UiTipTidy = ui_tip_tidy;
		UiTipDispose = ui_tip_dispose;
		UiTipDestroyItem = ui_tip_destroy_item;
		UiTipSwitchLable = ui_tip_switch_lable;
		UiTipPutAll = ui_tip_put_all;
		UiTipPutMax = ui_tip_put_max;
		UiTipTakeOutAll = ui_tip_take_out_all;
		UiTipTakeOutMax = ui_tip_take_out_max;
		UiTipPutAllTap = ui_tip_put_all_tap;
		UiTipTakeOutOne = ui_tip_take_out_one;
		UiTipTakeOutHalf = ui_tip_take_out_half;
		UiTipTakeOutOneGamepad = ui_tip_take_out_one_gamepad;
		UiTipPut = ui_tip_put;
		UiTipTakeOut = ui_tip_take_out;
		UiTipPutGamepad = ui_tip_put_gamepad;
		UiTipTakeOutGamepad = ui_tip_take_out_gamepad;
		UiTipPickUpGamepad = ui_tip_pick_up_gamepad;
		UiTipQuickPutOne = ui_tip_quick_put_one;
		UiTipQuickPutAll = ui_tip_quick_put_all;
		UiTipQuickTakeOne = ui_tip_quick_take_one;
		UiTipQuickTakeAll = ui_tip_quick_take_all;
		UiTipQuickEquipment = ui_tip_quick_equipment;
		UiTipSwitchBox = ui_tip_switch_box;
		UiTipSubOne = ui_tip_sub_one;
		UiTipAddOne = ui_tip_add_one;
		UiTipSubTen = ui_tip_sub_ten;
		UiTipAddTen = ui_tip_add_ten;
		UiTipSetMin = ui_tip_set_min;
		UiTipSetMax = ui_tip_set_max;
		UiTipBroadcast = ui_tip_broadcast;
		UiTipMakeOne = ui_tip_make_one;
		UiTipMakeAll = ui_tip_make_all;
		UiTipBuyOne = ui_tip_buy_one;
		UiTipBuyAllGamepad = ui_tip_buy_all_gamepad;
		UiTipSellOne = ui_tip_sell_one;
		UiTipSellAll = ui_tip_sell_all;
		UiTipPrevFilter = ui_tip_prev_filter;
		UiTipNextFilter = ui_tip_next_filter;
		UiTipCapture = ui_tip_capture;
		UiTipCollection = ui_tip_collection;
		UiTipHistory = ui_tip_history;
		UiTipChangeName = ui_tip_change_name;
		UiTipTakeOutSelected = ui_tip_take_out_selected;
		UiTipPutOneSelected = ui_tip_put_one_selected;
		UiTipPutAllSelected = ui_tip_put_all_selected;
		UiTipAddMemo = ui_tip_add_memo;
		UiTipChangeMemo = ui_tip_change_memo;
		UiTipDelMemo = ui_tip_del_memo;
		UiTipSwitchMapSize = ui_tip_switch_map_size;
		UiTipMapCentered = ui_tip_map_centered;
		UiTipEmailRecycle = ui_tip_email_recycle;
		UiTipEmailDefault = ui_tip_email_default;
		UiTipTechtreePointFocus = ui_tip_techtree_point_focus;
		UiTipTechtreeFocus = ui_tip_techtree_focus;
		UiTipBatteryLow = ui_tip_battery_low;
		UiTipErrTakeBoat = ui_tip_err_take_boat;
		UiOperationTalk = ui_operation_talk;
		UiOperationTalkUnknown = ui_operation_talk_unknown;
		UiOperationInteract = ui_operation_interact;
		UiOperationSleep = ui_operation_sleep;
		UiOperationPick = ui_operation_pick;
		UiOperationWell = ui_operation_well;
		UiOperationHarvest = ui_operation_harvest;
		UiOperationClear = ui_operation_clear;
		UiOperationOpen = ui_operation_open;
		UiOperationOpenDoor = ui_operation_open_door;
		UiOperationCloseDoor = ui_operation_close_door;
		UiOperationEnter = ui_operation_enter;
		UiOperationEnterFormat = ui_operation_enter_format;
		UiOperationExit = ui_operation_exit;
		UiOperationView = ui_operation_view;
		UiOperationUse = ui_operation_use;
		UiOperationSit = ui_operation_sit;
		UiOperationDisplay = ui_operation_display;
		UiOperationTakeoff = ui_operation_takeoff;
		UiOperationDump = ui_operation_dump;
		UiOperationDisembark = ui_operation_disembark;
		UiOperationCallBoat = ui_operation_call_boat;
		UiOperationShower = ui_operation_shower;
		UiOperationOpenBox = ui_operation_open_box;
		UiOperationStorageShelf = ui_operation_storage_shelf;
		UiOperationFuelIn = ui_operation_fuel_in;
		UiOperationClose = ui_operation_close;
		UiOperationStart = ui_operation_start;
		UiOperationFill = ui_operation_fill;
		UiOperationFondle = ui_operation_fondle;
		UiOperationStartSomething = ui_operation_start_something;
		UiOperationUseSomething = ui_operation_use_something;
		UiOperationErrNotSeed = ui_operation_err_not_seed;
		UiOperationErrInvalidPlantbasin = ui_operation_err_invalid_plantbasin;
		UiOperationErrInvalidSeason = ui_operation_err_invalid_season;
		UiOperationErrLackOfAsset = ui_operation_err_lack_of_asset;
		UiOperationErrLackOfEnergy = ui_operation_err_lack_of_energy;
		UiOperationErrCannotPlantTree = ui_operation_err_cannot_plant_tree;
		UiOperationErrCannotPlantOnGround = ui_operation_err_cannot_plant_on_ground;
		UiOperationErrEmptyDrone = ui_operation_err_empty_drone;
		UiOperationErrWellFull = ui_operation_err_well_full;
		UiOperationErrWaterFull = ui_operation_err_water_full;
		UiOperationErrRunoutWater = ui_operation_err_runout_water;
		UiOperationErrRunoutWaterAround = ui_operation_err_runout_water_around;
		UiOperationErrEquipmentCorroded = ui_operation_err_equipment_corroded;
		UiOperationErrLowToolLevel = ui_operation_err_low_tool_level;
		UiOperationErrFullPlasticFilm = ui_operation_err_full_plastic_film;
		UiOperationErrCannotFertilizer = ui_operation_err_cannot_fertilizer;
		UiOperationErrDroneFullBattery = ui_operation_err_drone_full_battery;
		UiOperationErrWaterEvaporated = ui_operation_err_water_evaporated;
		UiOperationErrFailToPlaceBox = ui_operation_err_fail_to_place_box;
		UiOperationErrCannotDisplay = ui_operation_err_cannot_display;
		UiOperationErrCannotUseIfRiding = ui_operation_err_cannot_use_if_riding;
		UiOperationErrCannotCallMotor = ui_operation_err_cannot_call_motor;
		UiOperationErrSwordCannotAutofire = ui_operation_err_sword_cannot_autofire;
		UiOperationErrLowPower = ui_operation_err_low_power;
		UiOperationErrCooling = ui_operation_err_cooling;
		UiOperationErrBarrelFull = ui_operation_err_barrel_full;
		UiOperationErrFuelFull = ui_operation_err_fuel_full;
		UiOperationErrNotFeeds = ui_operation_err_not_feeds;
		UiOperationErrFeederFull = ui_operation_err_feeder_full;
		UiOperationErrSlotFull = ui_operation_err_slot_full;
		UiOperationErrNoSuitableEquipments = ui_operation_err_no_suitable_equipments;
		UiStorageShelfFull = ui_storage_shelf_full;
		UiOperationWeatherHasChanged = ui_operation_weather_has_changed;
		UiOperationErrFuel = ui_operation_err_fuel;
		UiOperationGiftItemFail = ui_operation_gift_item_fail;
		UiOperationMakeEquipment = ui_operation_make_equipment;
		UiOperationSell = ui_operation_sell;
		UiOperationSwitchAutoFire = ui_operation_switch_auto_fire;
		UiOperationSwitchManualFire = ui_operation_switch_manual_fire;
		UiOperationCollect = ui_operation_collect;
		UiOperationRide = ui_operation_ride;
		UiOperationNoAnimalBuilding = ui_operation_no_animal_building;
		UiOperationErrNoPlantBasin = ui_operation_err_no_plant_basin;
		UiOperationErrNoBuildingHere = ui_operation_err_no_building_here;
		UiOperationErrInvalidWallpaper = ui_operation_err_invalid_wallpaper;
		UiOperationErrSameWallpaper = ui_operation_err_same_wallpaper;
		UiOperationErrShouldInhouse = ui_operation_err_should_inhouse;
		UiOperationErrShouldOutside = ui_operation_err_should_outside;
		StorePlayerMoneyNotEnough = store_player_money_not_enough;
		StoreStoreMoneyNotEnough = store_store_money_not_enough;
		StoreItemNotSaleable = store_item_not_saleable;
		StoreItemSoldOut = store_item_sold_out;
		StoreLackOfAsset = store_lack_of_asset;
		StoreUpgradePrice = store_upgrade_price;
		StoreUpgradeConfirm = store_upgrade_confirm;
		StoreSoldOutIcon = store_sold_out_icon;
		StoreUpgradeEmptyInfo = store_upgrade_empty_info;
		StoreItemNotSaleableComment = store_item_not_saleable_comment;
		StoreQuantitySubmitSelling = store_quantity_submit_selling;
		StoreQuantitySubmitCurrentMoneySelling = store_quantity_submit_current_money_selling;
		StoreQuantitySubmitTotalMoneySelling = store_quantity_submit_total_money_selling;
		StoreQuantitySubmitBuying = store_quantity_submit_buying;
		StoreQuantitySubmitCurrentMoneyBuying = store_quantity_submit_current_money_buying;
		StoreQuantitySubmitTotalMoneyBuying = store_quantity_submit_total_money_buying;
		StoreQuantitySubmitCountInBack = store_quantity_submit_count_in_back;
		StoreQuantitySubmitUnitPrice = store_quantity_submit_unit_price;
		StoreItemTipPlantbasinLocked = store_item_tip_plantbasin_locked;
		FarmbuilderErrBuildSystemNotSupport = farmbuilder_err_build_system_not_support;
		FarmbuilderErrLackOfAsset = farmbuilder_err_lack_of_asset;
		FarmbuilderErrIndoorEquipment = farmbuilder_err_indoor_equipment;
		FarmbuilderErrOutdoorEquipment = farmbuilder_err_outdoor_equipment;
		FarmbuilderErrInvalidPosition = farmbuilder_err_invalid_position;
		FarmbuilderErrOutOfRange = farmbuilder_err_out_of_range;
		FarmbuilderErrEquipmentAreaNotEmpty = farmbuilder_err_equipment_area_not_empty;
		FarmbuilderErrPlatformOccupied = farmbuilder_err_platform_occupied;
		FarmbuilderQuestionRemove = farmbuilder_question_remove;
		FarmbuilderQuestionRemoveReturn = farmbuilder_question_remove_return;
		FarmbuilderErrEquipmentOccupied = farmbuilder_err_equipment_occupied;
		FarmbuilderErrPlantbasinTreeOccupied = farmbuilder_err_plantbasin_tree_occupied;
		FarmbuilderErrParkingApronOccupied = farmbuilder_err_parking_apron_occupied;
		FarmbuilderErrAutomateBotOccupied = farmbuilder_err_automate_bot_occupied;
		FarmbuilderErrShowCaseOccupied = farmbuilder_err_show_case_occupied;
		FarmbuilderErrPlatformRemove = farmbuilder_err_platform_remove;
		FarmbuilderErrEquipmentHideDoor = farmbuilder_err_equipment_hide_door;
		FarmbuilderErrBuildingOccupied = farmbuilder_err_building_occupied;
		FarmbuilderErrBuildingOccupiedAnimal = farmbuilder_err_building_occupied_animal;
		FarmbuilderErrBuildingAreaNotEmpty = farmbuilder_err_building_area_not_empty;
		FarmbuilderErrBuildingInvalidHeight = farmbuilder_err_building_invalid_height;
		FarmbuilderErrBuildingInvalidCoveredRatio = farmbuilder_err_building_invalid_covered_ratio;
		FarmbuilderErrBuildingCanNotRemove = farmbuilder_err_building_can_not_remove;
		FarmbuilderErrBuildingInvalidOtherNoSupport = farmbuilder_err_building_invalid_other_no_support;
		FarmbuilderErrBuildingOccupiedBuilding = farmbuilder_err_building_occupied_building;
		FarmbuilderErrBuildingInvalidDoorBeHidden = farmbuilder_err_building_invalid_door_be_hidden;
		FarmbuilderErrBuildingOccupiedCeiling = farmbuilder_err_building_occupied_ceiling;
		FarmbuilderErrBuildingOccupiedDoor = farmbuilder_err_building_occupied_door;
		FarmbuilderErrBuildingOccupiedCeilingPt = farmbuilder_err_building_occupied_ceiling_pt;
		FarmbuilderErrBuildingGroundInvalid = farmbuilder_err_building_ground_invalid;
		FarmbuilderErrPlatformWidth = farmbuilder_err_platform_width;
		FarmbuilderErrPlatformHeightMax = farmbuilder_err_platform_height_max;
		FarmbuilderErrEquipmentLimited = farmbuilder_err_equipment_limited;
		TechtreePanelTitle = techtree_panel_title;
		TechtreeNodeLackOfPoints = techtree_node_lack_of_points;
		TechtreeNodeNotAvaiable = techtree_node_not_avaiable;
		TechtreeNodeUnlocked = techtree_node_unlocked;
		TechtreeNodeUnopen = techtree_node_unopen;
		TechtreeNodeUnlockConfirm = techtree_node_unlock_confirm;
		TechtreeNodeBuildingHealth = techtree_node_building_health;
		TechtreeNodeEquipmentElectronic = techtree_node_equipment_electronic;
		TechtreeProgressInfo = techtree_progress_info;
		TechtreeMaxLevel = techtree_max_level;
		TechtreeNodeHint = techtree_node_hint;
		MissionUpdate = mission_update;
		MissionComplete = mission_complete;
		MissionCompleteSendEmail = mission_complete_send_email;
		MissionPanelTitle = mission_panel_title;
		MissionPanelEmpty = mission_panel_empty;
		UiMissionTimeLimit = ui_mission_time_limit;
		UiMissionNpcPosition = ui_mission_npc_position;
		SeedPanelTitle = seed_panel_title;
		SeedNodeAlreadyUnlock = seed_node_already_unlock;
		SeedNodeSucceedUnlock = seed_node_succeed_unlock;
		BuildingPanelTitle = building_panel_title;
		BuildingPanelCoverSizeDescription = building_panel_cover_size_description;
		BuildingPanelInnerSizeDescription = building_panel_inner_size_description;
		BuildingPanelAnimalCapacityDescription = building_panel_animal_capacity_description;
		BuildingPanelSizeDescription = building_panel_size_description;
		BuildingPanelEmpty = building_panel_empty;
		BuildingPanelStartBuild = building_panel_start_build;
		BuildingPanelMaterialNotEnough = building_panel_material_not_enough;
		BuildingPanelMoneyNotEnough = building_panel_money_not_enough;
		BuildingPanelInputBuildingName = building_panel_input_building_name;
		EquipmentPanelEmpty = equipment_panel_empty;
		EquipmentPanelStartBuild = equipment_panel_start_build;
		EquipmentPanelListName = equipment_panel_list_name;
		EquipmentViewerEmpty = equipment_viewer_empty;
		EquipmentPanelLatelyEmpty = equipment_panel_lately_empty;
		EquipmentPanelMakeComplete = equipment_panel_make_complete;
		EquipmentPanelHide = equipment_panel_hide;
		EquipmentPanelJump = equipment_panel_jump;
		EquipmentPanelUnlockTip = equipment_panel_unlock_tip;
		EquipmentPanelElectronicTip = equipment_panel_electronic_tip;
		EquipmentPanelBatteryTip = equipment_panel_battery_tip;
		EquipmentPanelApplianceTip = equipment_panel_appliance_tip;
		EquipmentPanelGeneratorTip = equipment_panel_generator_tip;
		UiEmailTitle = ui_email_title;
		UiEmailEmpty = ui_email_empty;
		UiEmailAcceptMission = ui_email_accept_mission;
		UiEmailAlreadyAccepted = ui_email_already_accepted;
		UiEmailReciveItem = ui_email_recive_item;
		UiEmailAlreadyRecived = ui_email_already_recived;
		UiEmailRecycleErr = ui_email_recycle_err;
		RecipePanelEmpty = recipe_panel_empty;
		RecipePanelStartBuild = recipe_panel_start_build;
		RecipePanelTimeInfo = recipe_panel_time_info;
		RecipePanelAlreadyWorking = recipe_panel_already_working;
		RecipePanelTitle = recipe_panel_title;
		RecipePanelTask = recipe_panel_task;
		RecipePanelRestTime = recipe_panel_rest_time;
		RecipePanelMaxCraftCount = recipe_panel_max_craft_count;
		RecipePanelLimitUp = recipe_panel_limit_up;
		RecipePanelNotCookable = recipe_panel_not_cookable;
		RecipePanelNoMaterial = recipe_panel_no_material;
		RecipePanelConfirmWithTime = recipe_panel_confirm_with_time;
		RecipePanelUnlockedRecipeComment = recipe_panel_unlocked_recipe_comment;
		RecipePanelUnlockedDishComment = recipe_panel_unlocked_dish_comment;
		RecipePanelUnknowDishComment = recipe_panel_unknow_dish_comment;
		RecipePanelConfirmPopBuffer = recipe_panel_confirm_pop_buffer;
		RecipePanelMaterialList = recipe_panel_material_list;
		RecipePanelExistItems = recipe_panel_exist_items;
		RecipePanelRandomGeneHint = recipe_panel_random_gene_hint;
		PlatformPanelTitle = platform_panel_title;
		PlatformPanelEmpty = platform_panel_empty;
		PlatformPanelStartBuild = platform_panel_start_build;
		PlatformPanelCostPrefix = platform_panel_cost_prefix;
		ItemBuildingProtoError = item_building_proto_error;
		ItemEquipmentProtoError = item_equipment_proto_error;
		ItemSendEmailOnOverflow = item_send_email_on_overflow;
		ItemSleepingBagConditionFailedMonster = item_sleeping_bag_condition_failed_monster;
		ItemSleepingBagConditionFailedWater = item_sleeping_bag_condition_failed_water;
		ItemRescuePagerConditionFailed = item_rescue_pager_condition_failed;
		ItemTipPrice = item_tip_price;
		ItemTipBasicsPrice = item_tip_basics_price;
		ItemTipMoneyUnit = item_tip_money_unit;
		ItemChipAttackIncrease = item_chip_attack_increase;
		ItemChipAttackIncreaseFixed = item_chip_attack_increase_fixed;
		ItemChipCriticalRateIncrease = item_chip_critical_rate_increase;
		ItemChipAttackSpeedIncrease = item_chip_attack_speed_increase;
		ItemChipAccuracyIncrease = item_chip_accuracy_increase;
		ItemChipPowerCostDecrease = item_chip_power_cost_decrease;
		ItemChipAttackDistanceIncrease = item_chip_attack_distance_increase;
		ItemChipMoveSpeedIncrease = item_chip_move_speed_increase;
		ItemChipClipCapacityAddition = item_chip_clip_capacity_addition;
		ItemChipReloadDurationDecrease = item_chip_reload_duration_decrease;
		ItemEngineMoveSpeedIncrease = item_engine_move_speed_increase;
		ItemEnginePowerCapacityIncrease = item_engine_power_capacity_increase;
		ItemEnginePowerRecvIncrease = item_engine_power_recv_increase;
		ItemStructureMoveSpeedIncrease = item_structure_move_speed_increase;
		ItemStructurePowerCapacity = item_structure_power_capacity;
		ItemStructurePowerRecv = item_structure_power_recv;
		ItemMaxDurability = item_max_durability;
		ItemTitleInfoFormat = item_title_info_format;
		ItemConfirmUse = item_confirm_use;
		ItemWaterCanArea = item_water_can_area;
		ItemWaterCanEndlessWater = item_water_can_endless_water;
		ItemGeneDescFormat = item_gene_desc_format;
		ItemSeedCloned = item_seed_cloned;
		ItemWpIsAvailableFor = item_wp_is_available_for;
		ItemWpAvailableAll = item_wp_available_all;
		ItemFishFryTitleFormat = item_fish_fry_title_format;
		ItemToolLevelFormat = item_tool_level_format;
		ItemPatchValueFormat = item_patch_value_format;
		ItemBoxValueFormat = item_box_value_format;
		ItemFilmValueFormat = item_film_value_format;
		ItemDroneWeaponValueFormat = item_drone_weapon_value_format;
		ItemMoneyTitle = item_money_title;
		ItemMoneyDesc = item_money_desc;
		ItemMoneyType = item_money_type;
		UiSystemExitGame = ui_system_exit_game;
		UiSystemReturnHomePage = ui_system_return_home_page;
		UiSystemConfirmHome = ui_system_confirm_home;
		UiSystemConfirmExit = ui_system_confirm_exit;
		RewardInfoBuildingUnlock = reward_info_building_unlock;
		RewardInfoBusStationUnlock = reward_info_bus_station_unlock;
		RewardInfoEquipmentUnlock = reward_info_equipment_unlock;
		RewardInfoPlatformUnlock = reward_info_platform_unlock;
		RewardInfoRecipeUnlock = reward_info_recipe_unlock;
		RewardInfoRecipeUnlockHint = reward_info_recipe_unlock_hint;
		RewardInfoItem = reward_info_item;
		RewardInfoGold = reward_info_gold;
		RewardInfoFavorabilityLv1 = reward_info_favorability_lv1;
		RewardInfoFavorabilityLv2 = reward_info_favorability_lv2;
		RewardInfoFavorabilityLv3 = reward_info_favorability_lv3;
		DropoffBoxNoGoods = dropoff_box_no_goods;
		DropoffBoxNoDrone = dropoff_box_no_drone;
		DropoffBoxTotalMoney = dropoff_box_total_money;
		DropoffBoxPriceIncreased = dropoff_box_price_increased;
		DropoffBoxLaunchDrone = dropoff_box_launch_drone;
		DropoffBoxConfirmLauch = dropoff_box_confirm_lauch;
		AutomateBotPanelStateIdle = automate_bot_panel_state_idle;
		AutomateBotPanelStateCharge = automate_bot_panel_state_charge;
		AutomateBotPanelStatePause = automate_bot_panel_state_pause;
		AutomateBotPanelStateWorking = automate_bot_panel_state_working;
		AutomateBotPanelCfgEmpty = automate_bot_panel_cfg_empty;
		AutomateBotPanelRecipeEmpty = automate_bot_panel_recipe_empty;
		AutomateBotPanelDateEmpty = automate_bot_panel_date_empty;
		AutomateBotPanelRecipeType = automate_bot_panel_recipe_type;
		AutomateBotPanelRecipeSubType = automate_bot_panel_recipe_subType;
		AutomateBotPanelAutoFertilizer = automate_bot_panel_auto_fertilizer;
		AutomateBotPanelAutoProtect = automate_bot_panel_auto_protect;
		AutomateBotPanelEnergyType = automate_bot_panel_energy_type;
		AutomateBotPanelItemError = automate_bot_panel_item_error;
		BoardMissionPanelTitle = board_mission_panel_title;
		BoardMissionUrgencyLow = board_mission_urgency_low;
		BoardMissionUrgencyMiddle = board_mission_urgency_middle;
		BoardMissionUrgencyHigh = board_mission_urgency_high;
		BoardMissionUrgencyNone = board_mission_urgency_none;
		BoardMissionAcceptFail = board_mission_accept_fail;
		BoardMissionTitle = board_mission_title;
		BoardMissionLowLevel = board_mission_low_level;
		BoardMissionEmpty = board_mission_empty;
		BoardMissionOverdue = board_mission_overdue;
		BoardMissionLv = board_mission_lv;
		BoardMissionExpTip = board_mission_exp_tip;
		InventoryPanelCannotPutIn = inventory_panel_cannot_put_in;
		InventoryPanelContainerFull = inventory_panel_container_full;
		BoxPanelRestCount = box_panel_rest_count;
		BoxPanelUsedUpWarning = box_panel_used_up_warning;
		BoxPanelBroken = box_panel_broken;
		BoxPanelAlreadyOpen = box_panel_already_open;
		BoxPanelNoNeedRepair = box_panel_no_need_repair;
		BoxPanelNoRepairCost = box_panel_no_repair_cost;
		BoxPanelRepairInfo = box_panel_repair_info;
		InventoryPanelDungeonCaseTitle = inventory_panel_dungeon_case_title;
		InventoryPanelBackpackTitle = inventory_panel_backpack_title;
		FishTankPanelFeedQuantity = fish_tank_panel_feed_quantity;
		InventoryPanelSocketTitle = inventory_panel_socket_title;
		InventoryPanelPutBoxFirst = inventory_panel_put_box_first;
		InventoryPanelInfoGeneIncubator = inventory_panel_info_gene_incubator;
		InventoryPanelCapsuleTitle = inventory_panel_capsule_title;
		InventoryPanelInfoGeneReplicator = inventory_panel_info_gene_replicator;
		InventoryPanelGeneReplicatorLocked = inventory_panel_gene_replicator_locked;
		InventoryPanelInfoGeneSynthesizer = inventory_panel_info_gene_synthesizer;
		InventoryPanelGeneSynthesizerLocked = inventory_panel_gene_synthesizer_locked;
		UiTipTimeTitle = ui_tip_time_title;
		UiTipTimeFormat = ui_tip_time_format;
		UiTipCurrentSeason = ui_tip_current_season;
		UiTipOpenMenu = ui_tip_open_menu;
		UiTipOpenMap = ui_tip_open_map;
		UiTipShowMissionTip = ui_tip_show_mission_tip;
		UiTipHideMissionTip = ui_tip_hide_mission_tip;
		UiTipNoMission = ui_tip_no_mission;
		UiTipMissionShowDetails = ui_tip_mission_show_details;
		UiTipHealthValue = ui_tip_health_value;
		UiTipEnergyValue = ui_tip_energy_value;
		UiTipCorrosionValue = ui_tip_corrosion_value;
		UiTipCurrentSpiritLe50 = ui_tip_current_spirit_le_50;
		UiTipCurrentSpiritLe25 = ui_tip_current_spirit_le_25;
		UiTipCurrentSpiritLe10 = ui_tip_current_spirit_le_10;
		UiTipCurrentSpiritLe0 = ui_tip_current_spirit_le_0;
		UiTipBuffDuration = ui_tip_buff_duration;
		UiTipDebuffDuration = ui_tip_debuff_duration;
		UiTipBuildHealth = ui_tip_build_health;
		UiTipElectricityGeneratorInfo = ui_tip_electricity_generator_info;
		UiTipElectricityBatteryInfo = ui_tip_electricity_battery_info;
		UiTipElectricityStatusNone = ui_tip_electricity_status_none;
		UiTipElectricityStatusLackOfGeneration = ui_tip_electricity_status_lack_of_generation;
		UiTipElectricityStatusLackOfGenerationCostBattery = ui_tip_electricity_status_lack_of_generation_cost_battery;
		UiTipElectricityStatusBatterySaving = ui_tip_electricity_status_battery_saving;
		UiTipElectricityStatusBatteryFull = ui_tip_electricity_status_battery_full;
		UiTipElectricityStatusPowerLoss = ui_tip_electricity_status_power_loss;
		UiTipSeedEnd = ui_tip_seed_end;
		UiTipLoading = ui_tip_loading;
		DocumentTitleComputer = document_title_computer;
		NpcDocumentTitle = npc_document_title;
		ChipDocumentTitle = chip_document_title;
		PlantDocumentTitle = plant_document_title;
		DocumentEmpty = document_empty;
		ChipDocumentTotle = chip_document_totle;
		GameDataPanelTitle = game_data_panel_title;
		GameDataErrRead = game_data_err_read;
		GameDataTitle = game_data_title;
		GameDataDelConfirm = game_data_del_confirm;
		GameDataDelSuccess = game_data_del_success;
		GameDataErrEmpty = game_data_err_empty;
		GameDataSaving = game_data_saving;
		GameDataSaveFail = game_data_save_fail;
		GameDataSaveSuccessful = game_data_save_successful;
		GameDataFull = game_data_full;
		GameDataDuplicateSucess = game_data_duplicate_sucess;
		GameDataStart = game_data_start;
		GameDataLoad = game_data_load;
		TeleportFail = teleport_fail;
		TeleportConfirm = teleport_confirm;
		TeleportBuyTicket = teleport_buy_ticket;
		TeleportErrMaterialNotEnough = teleport_err_material_not_enough;
		FactionMissionFinish = faction_mission_finish;
		FactionMissionCannotSubmit = faction_mission_cannot_submit;
		FactionMissionFinishSubmit = faction_mission_finish_submit;
		FactionMissionMoneySubmit = faction_mission_money_submit;
		FactionMissionErrSubmit = faction_mission_err_submit;
		FactionMissionLock = faction_mission_lock;
		TreatyPortTitle = treaty_port_title;
		TreatyPortRepairTime = treaty_port_repair_time;
		TreatyPortRecruitStats = treaty_port_recruit_stats;
		TreatyPortPrincipal = treaty_port_principal;
		TreatyPortFactionMissionProgress = treaty_port_faction_mission_progress;
		TreatyPortFactionReputation = treaty_port_faction_reputation;
		TreatyPortContactNpc = treaty_port_contact_npc;
		TreatyPortFactionEnterTime = treaty_port_faction_enter_time;
		TreatyPortBroadcast = treaty_port_broadcast;
		TreatyPortFactionLock = treaty_port_faction_lock;
		TreatyPortFactionUnopen = treaty_port_faction_unopen;
		TreatyPortRecruitHint = treaty_port_recruit_hint;
		TreatyPortTimeFormat = treaty_port_time_format;
		TreatyPortFactionRefuse = treaty_port_faction_refuse;
		TreatyPortOffDutyHoursTip = treaty_port_off_duty_hours_tip;
		TreatyPortInterviewTip = treaty_port_interview_tip;
		TreatyPortFactionSettledTip = treaty_port_faction_settled_tip;
		TreatyPortNotContactedTip = treaty_port_not_contacted_tip;
		SettingPanelSaveSuccessful = setting_panel_save_successful;
		SettingPanelOtherSaveSuccessful = setting_panel_other_save_successful;
		SettingPanelReset = setting_panel_reset;
		SettingPanelResetConfirm = setting_panel_reset_confirm;
		SettingPanelTitle = setting_panel_title;
		SettingPanelExitConfirm = setting_panel_exit_confirm;
		SettingPanelConflictHint = setting_panel_conflict_hint;
		SettingPanelResetToDefault = setting_panel_reset_to_default;
		SettingPanelCanNotEdit = setting_panel_can_not_edit;
		SettingPanelCanNotRemove = setting_panel_can_not_remove;
		SettingPanelNoValidInput = setting_panel_no_valid_input;
		SettingPanelAllowTracedataCollector = setting_panel_allow_tracedata_collector;
		SettingPanelTracedataCollectorDesc = setting_panel_tracedata_collector_desc;
		SettingPanelDelete = setting_panel_delete;
		SettingPanelCanNotSaveByConflict = setting_panel_can_not_save_by_conflict;
		SettingPanelNewDevice = setting_panel_new_device;
		SettingPanelLoading = setting_panel_loading;
		EquipmentBarTimeLabel = equipment_bar_time_label;
		EquipmentBarTimeFormat = equipment_bar_time_format;
		EquipmentBarDroneNotEquip = equipment_bar_drone_not_equip;
		EquipmentBarMotorTitle = equipment_bar_motor_title;
		EquipmentBarMotorLock = equipment_bar_motor_lock;
		EquipmentBarHatTip = equipment_bar_hat_tip;
		EquipmentBarPositiveTip = equipment_bar_positive_tip;
		EquipmentBarPassive1Tip = equipment_bar_passive1_tip;
		EquipmentBarPassive2Tip = equipment_bar_passive2_tip;
		EquipmentBarDroneTip = equipment_bar_drone_tip;
		EquipmentBarSkillLock = equipment_bar_skill_lock;
		EquipmentBarDoubleJumpDesc = equipment_bar_double_jump_desc;
		EquipmentBarSprintDesc = equipment_bar_sprint_desc;
		AbilityDoubleJumpUnlockTip = ability_double_jump_unlock_tip;
		AbilitySprintUnlockTip = ability_sprint_unlock_tip;
		EquipmentBarBackpackFull = equipment_bar_backpack_full;
		EquipmentSkillPrefix = equipment_skill_prefix;
		EquipmentDefensePrefix = equipment_defense_prefix;
		UiTipHomepageStartGame = ui_tip_homepage_start_game;
		UiTipHomepageChangelog = ui_tip_homepage_changelog;
		UiTipHomepageSettings = ui_tip_homepage_settings;
		UiTipHomepageMods = ui_tip_homepage_mods;
		UiTipHomepageDeveloperList = ui_tip_homepage_developer_list;
		UiTipHomepageExitGame = ui_tip_homepage_exit_game;
		UiTipParkingApronLocked = ui_tip_parking_apron_locked;
		UiTipResolving = ui_tip_resolving;
		UiTipShredderMoney = ui_tip_shredder_money;
		UiTipErrShredderEmpty = ui_tip_err_shredder_empty;
		UiTipAirWall = ui_tip_air_wall;
		UiTipNotAvailableToMotor = ui_tip_not_available_to_motor;
		CollectionPanelItemLabel = collection_panel_item_label;
		CollectionPanelItemRecipeTime = collection_panel_item_recipe_time;
		CollectionPanelItemRecipeEmpty = collection_panel_item_recipe_empty;
		CollectionPanelItemUnknown = collection_panel_item_unknown;
		CollectionPanelItemSource = collection_panel_item_source;
		CollectionPanelNpcAddress = collection_panel_npc_address;
		CollectionPanelNpcLikeRecord = collection_panel_npc_like_record;
		CollectionPanelNpcLikeNone = collection_panel_npc_like_none;
		CollectionPanelNpcLikingLock = collection_panel_npc_liking_lock;
		CollectionPanelNpcLikingLvLock = collection_panel_npc_liking_lv_lock;
		CollectionPanelNpcContentTitle = collection_panel_npc_content_title;
		CollectionPanelNpcContentLockTip = collection_panel_npc_content_lock_tip;
		CollectionPanelNpcContentEnd = collection_panel_npc_content_end;
		CollectionPanelMonsterUnknown = collection_panel_monster_unknown;
		CollectionPanelMonsterHabitat = collection_panel_monster_habitat;
		CollectionPanelMonsterDropText = collection_panel_monster_drop_text;
		CollectionPanelMonsterOrganism = collection_panel_monster_organism;
		CollectionPanelMonsterMachinery = collection_panel_monster_machinery;
		CollectionPanelMonsterBoss = collection_panel_monster_boss;
		CollectionPanelMonsterContentTitle = collection_panel_monster_content_title;
		CollectionPanelMonsterContentLockTip = collection_panel_monster_content_lock_tip;
		CollectionPanelDocumentLabel = collection_panel_document_label;
		CollectionPanelAnimalPossess = collection_panel_animal_possess;
		CollectionPanelAnimalBreed = collection_panel_animal_breed;
		CollectionPanelAnimalProductText = collection_panel_animal_product_text;
		CollectionPanelAnimalContentLockBringUp = collection_panel_animal_content_lock_bring_up;
		CollectionPanelAnimalContentLockBreed = collection_panel_animal_content_lock_breed;
		CollectionPanelAnimalContentLockProduct = collection_panel_animal_content_lock_product;
		CollectionPanelFishCatch = collection_panel_fish_catch;
		CollectionPanelFishBaitText = collection_panel_fish_bait_text;
		CollectionPanelFishPlace = collection_panel_fish_place;
		CollectionPanelFishMonth = collection_panel_fish_month;
		CollectionPanelFishWeather = collection_panel_fish_weather;
		CollectionPanelFishWeatherNone = collection_panel_fish_weather_none;
		CollectionPanelFishContentLockTip = collection_panel_fish_content_lock_tip;
		CollectionPanelResourceCollect = collection_panel_resource_collect;
		CollectionPanelResourceGrowthPeriod = collection_panel_resource_growth_period;
		CollectionPanelResourceYearRoundGrowth = collection_panel_resource_year_round_growth;
		CollectionPanelResourceDropTitle = collection_panel_resource_drop_title;
		CollectionPanelResourceContentLockTip = collection_panel_resource_content_lock_tip;
		BuilderPanelLabelBuilding = builder_panel_label_building;
		BuilderPanelLabelEquipment = builder_panel_label_equipment;
		BuilderPanelLabelPlatform = builder_panel_label_platform;
		BuilderPanelSwitchTerrainLayer = builder_panel_switch_terrain_layer;
		BuilderActionSelectedContent = builder_action_selected_content;
		BuilderActionUndo = builder_action_undo;
		BuilderActionTurn = builder_action_turn;
		BuilderActionDismantle = builder_action_dismantle;
		BuilderActionBuildingDismantle = builder_action_building_dismantle;
		BuilderActionBuildingStorage = builder_action_building_storage;
		BuilderActionMoveCamera = builder_action_move_camera;
		BuilderActionMove = builder_action_move;
		BuilderActionSelectedItem = builder_action_selected_item;
		BuilderActionPreciseMovement = builder_action_precise_movement;
		BuilderActionSwitchPrecise = builder_action_switch_precise;
		BuilderActionSwitchBackpack = builder_action_switch_backpack;
		BuilderActionRollingBackpack = builder_action_rolling_backpack;
		BuilderActionRollingItem = builder_action_rolling_item;
		BuilderActionToggleBackpack = builder_action_toggle_backpack;
		BuilderActionToggleBackpackGamepad = builder_action_toggle_backpack_gamepad;
		BuilderPanelExit = builder_panel_exit;
		BuilderPanelUndoGamepad = builder_panel_undo_gamepad;
		BuilderPanelExpandBuildingList = builder_panel_expand_building_list;
		BuilderPanellFoldBuildingList = builder_panell_fold_building_list;
		BuilderPanelFinish = builder_panel_finish;
		BuilderDismantleBuildingErr = builder_dismantle_building_err;
		BuilderBuilderConstruct = builder_builder_construct;
		BuilderPanelTempBuildingFull = builder_panel_temp_building_full;
		BuilderExitErrAnimal = builder_exit_err_animal;
		BuilderExitErrOccupied = builder_exit_err_occupied;
		BuilderExitErrSoleBuilding = builder_exit_err_sole_building;
		BuilderExitConfirm = builder_exit_confirm;
		UiEnvOptimizerPanelTitle = ui_env_optimizer_panel_title;
		UiEnvOptimizerConsoleTitle = ui_env_optimizer_console_title;
		UiEnvOptimizerOverview = ui_env_optimizer_overview;
		UiEnvOptimizerButtonNoEnergy = ui_env_optimizer_button_no_energy;
		UiEnvOptimizerTipNoEnergy = ui_env_optimizer_tip_no_energy;
		UiEnvOptimizerButtonNoComponent = ui_env_optimizer_button_no_component;
		UiEnvOptimizerTipNoComponent = ui_env_optimizer_tip_no_component;
		UiEnvOptimizerButtonValid = ui_env_optimizer_button_valid;
		UiEnvOptimizerButtonNotValid = ui_env_optimizer_button_not_valid;
		UiEnvOptimizerCheckSuccess = ui_env_optimizer_check_success;
		UiEnvOptimizerDateInfo = ui_env_optimizer_date_info;
		UiEnvOptimizerSlotTitle = ui_env_optimizer_slot_title;
		UiEnvOptimizerComponentAlreadyActive = ui_env_optimizer_component_already_active;
		UiEnvOptimizerInRecognition = ui_env_optimizer_in_recognition;
		UiEnvOptimizerLoadingData = ui_env_optimizer_loading_data;
		UiEnvOptimizerActiveSuccess = ui_env_optimizer_active_success;
		AnimalInvalidBuilding = animal_invalid_building;
		AnimalInvalidRoom = animal_invalid_room;
		AnimalFullBuilding = animal_full_building;
		AnimalInputAnimalName = animal_input_animal_name;
		AnimalPackageIsFull = animal_package_is_full;
		AnimalHasEscaped = animal_has_escaped;
		AnimalNoAnimal = animal_no_animal;
		AnimalInUse = animal_in_use;
		CalendarPanelYearTitle = calendar_panel_year_title;
		CalendarPanelDateTitle = calendar_panel_date_title;
		CalendarPanelEventTitle = calendar_panel_event_title;
		CalendarPanelPlayerBirthday = calendar_panel_player_birthday;
		CalendarPanelMemoTitle = calendar_panel_memo_title;
		CalendarPanelEmptyHint = calendar_panel_empty_hint;
		CalendarPanelDelMemoHint = calendar_panel_del_memo_hint;
		CalendarPanelInputEmptyHint = calendar_panel_input_empty_hint;
		InputTextContainsSensitiveWorld = input_text_contains_sensitive_world;
		CalendarMemoMessage = calendar_memo_message;
		UiCropInfoGrowthLevel = ui_crop_info_growth_level;
		UiCropInfoHarvestCount = ui_crop_info_harvest_count;
		UiAnimalInfoState = ui_animal_info_state;
		UiAnimalSpace = ui_animal_space;
		UiAnimalAgeYear = ui_animal_age_year;
		UiAnimalAgeMonth = ui_animal_age_month;
		UiAnimalAgeDay = ui_animal_age_day;
		UiAnimalBirthday = ui_animal_birthday;
		UiAnimalCapacity = ui_animal_capacity;
		UiAnimalCallBack = ui_animal_call_back;
		UiAnimalLetOut = ui_animal_let_out;
		UiAnimalAdult = ui_animal_adult;
		UiAnimalChild = ui_animal_child;
		UiAnimalPosition = ui_animal_position;
		UiAnimalNoAnimal = ui_animal_no_animal;
		UiAnimalNotVisible = ui_animal_not_visible;
		DronePanelTitle = drone_panel_title;
		DronePanelErrLoad = drone_panel_err_load;
		DronePanelErrLocked = drone_panel_err_locked;
		DroneComponentTitle = drone_component_title;
		GarbageSubmitErrItem = garbage_submit_err_item;
		DisposeFailRoom = dispose_fail_room;
		UiTipOpenMapFail = ui_tip_open_map_fail;
		UiTipNone = ui_tip_none;
		UiTipBuyBackpack = ui_tip_buy_backpack;
		UiTipBuyBackpackInfo = ui_tip_buy_backpack_info;
		UiTipEmptyList = ui_tip_empty_list;
		UiTipCurrentPosition = ui_tip_current_position;
		UiTipCurrentMotorPosition = ui_tip_current_motor_position;
		UiOptionShootingRangeStart = ui_option_shooting_range_start;
		UiOptionShootingRangeEnd = ui_option_shooting_range_end;
		UiTipEquipmentUnlock = ui_tip_equipment_unlock;
		UiOptionShootingRangeExit = ui_option_shooting_range_exit;
		UiOptionConfirmShootingRangeExit = ui_option_confirm_shooting_range_exit;
		UiNpcVisit = ui_npc_visit;
		UiNpcFileUpdation = ui_npc_file_updation;
		UiNpcLikingRise = ui_npc_liking_rise;
		UiNpcLikingDecline = ui_npc_liking_decline;
		ItemEdenFruitTip = item_eden_fruit_tip;
		UiTipGameVersion = ui_tip_game_version;
		UiTipContentLock = ui_tip_content_lock;
		UiTipPhotoSaving = ui_tip_photo_saving;
		UiItemGenerateElectricityEntry = ui_item_generate_electricity_entry;
		UiTipRename = ui_tip_rename;
		UiModNoMod = ui_mod_no_mod;
		UiModNeedSubscribe = ui_mod_need_subscribe;
		UiModOpenWorkshop = ui_mod_open_workshop;
		UiModNeedCreate = ui_mod_need_create;
		UiModOpenLocalDirectory = ui_mod_open_local_directory;
		UiModUploadMod = ui_mod_upload_mod;
		UiModUpdateMod = ui_mod_update_mod;
		UiModEnable = ui_mod_enable;
		UiModDisable = ui_mod_disable;
		UiModConfirmUpload = ui_mod_confirm_upload;
		UiModConfirmUpdate = ui_mod_confirm_update;
		UiModUploading = ui_mod_uploading;
		UiModUploadSuccess = ui_mod_upload_success;
		UiModUploadFailed = ui_mod_upload_failed;
		UiModUpdating = ui_mod_updating;
		UiModUpdateSuccess = ui_mod_update_success;
		UiModUpdateFailed = ui_mod_update_failed;
		UiModReloading = ui_mod_reloading;
		UiModSourceLocal = ui_mod_source_local;
		UiModSourceWorkshop = ui_mod_source_workshop;
	}

	public static StaticTextInfo DeserializeStaticTextInfo(JSONNode _json)
	{
		return new StaticTextInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1993636866;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SysErrDatalost = translator(SysErrDatalost_l10n_key, SysErrDatalost);
		UiMoneyTip = translator(UiMoneyTip_l10n_key, UiMoneyTip);
		UiItemSimpleTip = translator(UiItemSimpleTip_l10n_key, UiItemSimpleTip);
		UiItemTip = translator(UiItemTip_l10n_key, UiItemTip);
		UiTipConsumable = translator(UiTipConsumable_l10n_key, UiTipConsumable);
		UiErrMaterialNotEnough = translator(UiErrMaterialNotEnough_l10n_key, UiErrMaterialNotEnough);
		UiErrMoneyNotEnough = translator(UiErrMoneyNotEnough_l10n_key, UiErrMoneyNotEnough);
		UiErrBackpackIsFull = translator(UiErrBackpackIsFull_l10n_key, UiErrBackpackIsFull);
		UiErrNotDisposeItem = translator(UiErrNotDisposeItem_l10n_key, UiErrNotDisposeItem);
		UiQuesDisposeItem = translator(UiQuesDisposeItem_l10n_key, UiQuesDisposeItem);
		UiMenuItemnotavaiable = translator(UiMenuItemnotavaiable_l10n_key, UiMenuItemnotavaiable);
		UiBackpackCellExchange = translator(UiBackpackCellExchange_l10n_key, UiBackpackCellExchange);
		UiErrBackpackCellExchange = translator(UiErrBackpackCellExchange_l10n_key, UiErrBackpackCellExchange);
		UiSortDefault = translator(UiSortDefault_l10n_key, UiSortDefault);
		UiSortUnlock = translator(UiSortUnlock_l10n_key, UiSortUnlock);
		UiLoseItemTip = translator(UiLoseItemTip_l10n_key, UiLoseItemTip);
		UiLoseMoneyTip = translator(UiLoseMoneyTip_l10n_key, UiLoseMoneyTip);
		UiUnlockSeed = translator(UiUnlockSeed_l10n_key, UiUnlockSeed);
		UiAnalyzerTime = translator(UiAnalyzerTime_l10n_key, UiAnalyzerTime);
		UiAnalyzerOutput = translator(UiAnalyzerOutput_l10n_key, UiAnalyzerOutput);
		UiAnalyzerTotalTime = translator(UiAnalyzerTotalTime_l10n_key, UiAnalyzerTotalTime);
		UiShredderConfirm = translator(UiShredderConfirm_l10n_key, UiShredderConfirm);
		UiShredderConfirmUpgrade = translator(UiShredderConfirmUpgrade_l10n_key, UiShredderConfirmUpgrade);
		UiSubmitConfirm = translator(UiSubmitConfirm_l10n_key, UiSubmitConfirm);
		UiSubmitConfirmGene = translator(UiSubmitConfirmGene_l10n_key, UiSubmitConfirmGene);
		UiSubmitConfirmMoney = translator(UiSubmitConfirmMoney_l10n_key, UiSubmitConfirmMoney);
		UiSubmitNotEnough = translator(UiSubmitNotEnough_l10n_key, UiSubmitNotEnough);
		UiGiftItemConfirm = translator(UiGiftItemConfirm_l10n_key, UiGiftItemConfirm);
		UiTipDismentle = translator(UiTipDismentle_l10n_key, UiTipDismentle);
		UiTipPlantErr = translator(UiTipPlantErr_l10n_key, UiTipPlantErr);
		UiTextYears = translator(UiTextYears_l10n_key, UiTextYears);
		UiTextMonths = translator(UiTextMonths_l10n_key, UiTextMonths);
		UiTextDays = translator(UiTextDays_l10n_key, UiTextDays);
		UiTextHours = translator(UiTextHours_l10n_key, UiTextHours);
		UiTextMinutes = translator(UiTextMinutes_l10n_key, UiTextMinutes);
		UiTextTimeFormat = translator(UiTextTimeFormat_l10n_key, UiTextTimeFormat);
		UiTextDemoStatement = translator(UiTextDemoStatement_l10n_key, UiTextDemoStatement);
		UiTextGeneDescription = translator(UiTextGeneDescription_l10n_key, UiTextGeneDescription);
		InputTitlePlayerName = translator(InputTitlePlayerName_l10n_key, InputTitlePlayerName);
		InputTitleContainerName = translator(InputTitleContainerName_l10n_key, InputTitleContainerName);
		InputNameEmpty = translator(InputNameEmpty_l10n_key, InputNameEmpty);
		InputNameConfirm = translator(InputNameConfirm_l10n_key, InputNameConfirm);
		InputNameContainsSensitiveWorld = translator(InputNameContainsSensitiveWorld_l10n_key, InputNameContainsSensitiveWorld);
		InputTitlePlayerBirthday = translator(InputTitlePlayerBirthday_l10n_key, InputTitlePlayerBirthday);
		InputBirthdayConfirm = translator(InputBirthdayConfirm_l10n_key, InputBirthdayConfirm);
		UiSavepointDefault = translator(UiSavepointDefault_l10n_key, UiSavepointDefault);
		UiSavepointSleep = translator(UiSavepointSleep_l10n_key, UiSavepointSleep);
		UiSavepointNap = translator(UiSavepointNap_l10n_key, UiSavepointNap);
		UiSavepointKillTime = translator(UiSavepointKillTime_l10n_key, UiSavepointKillTime);
		UiSavepointSit = translator(UiSavepointSit_l10n_key, UiSavepointSit);
		UiSavepointExit = translator(UiSavepointExit_l10n_key, UiSavepointExit);
		UiSavepointCancel = translator(UiSavepointCancel_l10n_key, UiSavepointCancel);
		UiTipConfirm = translator(UiTipConfirm_l10n_key, UiTipConfirm);
		UiTipCancel = translator(UiTipCancel_l10n_key, UiTipCancel);
		UiTipQuit = translator(UiTipQuit_l10n_key, UiTipQuit);
		UiTipSwitchClassifying = translator(UiTipSwitchClassifying_l10n_key, UiTipSwitchClassifying);
		UiTipSort = translator(UiTipSort_l10n_key, UiTipSort);
		UiTipUpgrade = translator(UiTipUpgrade_l10n_key, UiTipUpgrade);
		UiTipBuy = translator(UiTipBuy_l10n_key, UiTipBuy);
		UiTipSellConfirm = translator(UiTipSellConfirm_l10n_key, UiTipSellConfirm);
		UiTipSell = translator(UiTipSell_l10n_key, UiTipSell);
		UiTipBuyConfirm = translator(UiTipBuyConfirm_l10n_key, UiTipBuyConfirm);
		UiTipUnlock = translator(UiTipUnlock_l10n_key, UiTipUnlock);
		UiTipUnlockSeed = translator(UiTipUnlockSeed_l10n_key, UiTipUnlockSeed);
		UiTipLaunchDrone = translator(UiTipLaunchDrone_l10n_key, UiTipLaunchDrone);
		UiTipSwitchAutomateBot = translator(UiTipSwitchAutomateBot_l10n_key, UiTipSwitchAutomateBot);
		UiTipAutomateBotSwitch = translator(UiTipAutomateBotSwitch_l10n_key, UiTipAutomateBotSwitch);
		UiTipAutomateBotUnload = translator(UiTipAutomateBotUnload_l10n_key, UiTipAutomateBotUnload);
		UiTipAutomateBotLoad = translator(UiTipAutomateBotLoad_l10n_key, UiTipAutomateBotLoad);
		UiTipChoice = translator(UiTipChoice_l10n_key, UiTipChoice);
		UiTipSubmitAll = translator(UiTipSubmitAll_l10n_key, UiTipSubmitAll);
		UiTipSubmit = translator(UiTipSubmit_l10n_key, UiTipSubmit);
		UiTipLoadGame = translator(UiTipLoadGame_l10n_key, UiTipLoadGame);
		UiTipDel = translator(UiTipDel_l10n_key, UiTipDel);
		UiTipLockSlot = translator(UiTipLockSlot_l10n_key, UiTipLockSlot);
		UiTipTidy = translator(UiTipTidy_l10n_key, UiTipTidy);
		UiTipDispose = translator(UiTipDispose_l10n_key, UiTipDispose);
		UiTipDestroyItem = translator(UiTipDestroyItem_l10n_key, UiTipDestroyItem);
		UiTipSwitchLable = translator(UiTipSwitchLable_l10n_key, UiTipSwitchLable);
		UiTipPutAll = translator(UiTipPutAll_l10n_key, UiTipPutAll);
		UiTipPutMax = translator(UiTipPutMax_l10n_key, UiTipPutMax);
		UiTipTakeOutAll = translator(UiTipTakeOutAll_l10n_key, UiTipTakeOutAll);
		UiTipTakeOutMax = translator(UiTipTakeOutMax_l10n_key, UiTipTakeOutMax);
		UiTipPutAllTap = translator(UiTipPutAllTap_l10n_key, UiTipPutAllTap);
		UiTipTakeOutOne = translator(UiTipTakeOutOne_l10n_key, UiTipTakeOutOne);
		UiTipTakeOutHalf = translator(UiTipTakeOutHalf_l10n_key, UiTipTakeOutHalf);
		UiTipTakeOutOneGamepad = translator(UiTipTakeOutOneGamepad_l10n_key, UiTipTakeOutOneGamepad);
		UiTipPut = translator(UiTipPut_l10n_key, UiTipPut);
		UiTipTakeOut = translator(UiTipTakeOut_l10n_key, UiTipTakeOut);
		UiTipPutGamepad = translator(UiTipPutGamepad_l10n_key, UiTipPutGamepad);
		UiTipTakeOutGamepad = translator(UiTipTakeOutGamepad_l10n_key, UiTipTakeOutGamepad);
		UiTipPickUpGamepad = translator(UiTipPickUpGamepad_l10n_key, UiTipPickUpGamepad);
		UiTipQuickPutOne = translator(UiTipQuickPutOne_l10n_key, UiTipQuickPutOne);
		UiTipQuickPutAll = translator(UiTipQuickPutAll_l10n_key, UiTipQuickPutAll);
		UiTipQuickTakeOne = translator(UiTipQuickTakeOne_l10n_key, UiTipQuickTakeOne);
		UiTipQuickTakeAll = translator(UiTipQuickTakeAll_l10n_key, UiTipQuickTakeAll);
		UiTipQuickEquipment = translator(UiTipQuickEquipment_l10n_key, UiTipQuickEquipment);
		UiTipSwitchBox = translator(UiTipSwitchBox_l10n_key, UiTipSwitchBox);
		UiTipSubOne = translator(UiTipSubOne_l10n_key, UiTipSubOne);
		UiTipAddOne = translator(UiTipAddOne_l10n_key, UiTipAddOne);
		UiTipSubTen = translator(UiTipSubTen_l10n_key, UiTipSubTen);
		UiTipAddTen = translator(UiTipAddTen_l10n_key, UiTipAddTen);
		UiTipSetMin = translator(UiTipSetMin_l10n_key, UiTipSetMin);
		UiTipSetMax = translator(UiTipSetMax_l10n_key, UiTipSetMax);
		UiTipBroadcast = translator(UiTipBroadcast_l10n_key, UiTipBroadcast);
		UiTipMakeOne = translator(UiTipMakeOne_l10n_key, UiTipMakeOne);
		UiTipMakeAll = translator(UiTipMakeAll_l10n_key, UiTipMakeAll);
		UiTipBuyOne = translator(UiTipBuyOne_l10n_key, UiTipBuyOne);
		UiTipBuyAllGamepad = translator(UiTipBuyAllGamepad_l10n_key, UiTipBuyAllGamepad);
		UiTipSellOne = translator(UiTipSellOne_l10n_key, UiTipSellOne);
		UiTipSellAll = translator(UiTipSellAll_l10n_key, UiTipSellAll);
		UiTipPrevFilter = translator(UiTipPrevFilter_l10n_key, UiTipPrevFilter);
		UiTipNextFilter = translator(UiTipNextFilter_l10n_key, UiTipNextFilter);
		UiTipCapture = translator(UiTipCapture_l10n_key, UiTipCapture);
		UiTipCollection = translator(UiTipCollection_l10n_key, UiTipCollection);
		UiTipHistory = translator(UiTipHistory_l10n_key, UiTipHistory);
		UiTipChangeName = translator(UiTipChangeName_l10n_key, UiTipChangeName);
		UiTipTakeOutSelected = translator(UiTipTakeOutSelected_l10n_key, UiTipTakeOutSelected);
		UiTipPutOneSelected = translator(UiTipPutOneSelected_l10n_key, UiTipPutOneSelected);
		UiTipPutAllSelected = translator(UiTipPutAllSelected_l10n_key, UiTipPutAllSelected);
		UiTipAddMemo = translator(UiTipAddMemo_l10n_key, UiTipAddMemo);
		UiTipChangeMemo = translator(UiTipChangeMemo_l10n_key, UiTipChangeMemo);
		UiTipDelMemo = translator(UiTipDelMemo_l10n_key, UiTipDelMemo);
		UiTipSwitchMapSize = translator(UiTipSwitchMapSize_l10n_key, UiTipSwitchMapSize);
		UiTipMapCentered = translator(UiTipMapCentered_l10n_key, UiTipMapCentered);
		UiTipEmailRecycle = translator(UiTipEmailRecycle_l10n_key, UiTipEmailRecycle);
		UiTipEmailDefault = translator(UiTipEmailDefault_l10n_key, UiTipEmailDefault);
		UiTipTechtreePointFocus = translator(UiTipTechtreePointFocus_l10n_key, UiTipTechtreePointFocus);
		UiTipTechtreeFocus = translator(UiTipTechtreeFocus_l10n_key, UiTipTechtreeFocus);
		UiTipBatteryLow = translator(UiTipBatteryLow_l10n_key, UiTipBatteryLow);
		UiTipErrTakeBoat = translator(UiTipErrTakeBoat_l10n_key, UiTipErrTakeBoat);
		UiOperationTalk = translator(UiOperationTalk_l10n_key, UiOperationTalk);
		UiOperationTalkUnknown = translator(UiOperationTalkUnknown_l10n_key, UiOperationTalkUnknown);
		UiOperationInteract = translator(UiOperationInteract_l10n_key, UiOperationInteract);
		UiOperationSleep = translator(UiOperationSleep_l10n_key, UiOperationSleep);
		UiOperationPick = translator(UiOperationPick_l10n_key, UiOperationPick);
		UiOperationWell = translator(UiOperationWell_l10n_key, UiOperationWell);
		UiOperationHarvest = translator(UiOperationHarvest_l10n_key, UiOperationHarvest);
		UiOperationClear = translator(UiOperationClear_l10n_key, UiOperationClear);
		UiOperationOpen = translator(UiOperationOpen_l10n_key, UiOperationOpen);
		UiOperationOpenDoor = translator(UiOperationOpenDoor_l10n_key, UiOperationOpenDoor);
		UiOperationCloseDoor = translator(UiOperationCloseDoor_l10n_key, UiOperationCloseDoor);
		UiOperationEnter = translator(UiOperationEnter_l10n_key, UiOperationEnter);
		UiOperationEnterFormat = translator(UiOperationEnterFormat_l10n_key, UiOperationEnterFormat);
		UiOperationExit = translator(UiOperationExit_l10n_key, UiOperationExit);
		UiOperationView = translator(UiOperationView_l10n_key, UiOperationView);
		UiOperationUse = translator(UiOperationUse_l10n_key, UiOperationUse);
		UiOperationSit = translator(UiOperationSit_l10n_key, UiOperationSit);
		UiOperationDisplay = translator(UiOperationDisplay_l10n_key, UiOperationDisplay);
		UiOperationTakeoff = translator(UiOperationTakeoff_l10n_key, UiOperationTakeoff);
		UiOperationDump = translator(UiOperationDump_l10n_key, UiOperationDump);
		UiOperationDisembark = translator(UiOperationDisembark_l10n_key, UiOperationDisembark);
		UiOperationCallBoat = translator(UiOperationCallBoat_l10n_key, UiOperationCallBoat);
		UiOperationShower = translator(UiOperationShower_l10n_key, UiOperationShower);
		UiOperationOpenBox = translator(UiOperationOpenBox_l10n_key, UiOperationOpenBox);
		UiOperationStorageShelf = translator(UiOperationStorageShelf_l10n_key, UiOperationStorageShelf);
		UiOperationFuelIn = translator(UiOperationFuelIn_l10n_key, UiOperationFuelIn);
		UiOperationClose = translator(UiOperationClose_l10n_key, UiOperationClose);
		UiOperationStart = translator(UiOperationStart_l10n_key, UiOperationStart);
		UiOperationFill = translator(UiOperationFill_l10n_key, UiOperationFill);
		UiOperationFondle = translator(UiOperationFondle_l10n_key, UiOperationFondle);
		UiOperationStartSomething = translator(UiOperationStartSomething_l10n_key, UiOperationStartSomething);
		UiOperationUseSomething = translator(UiOperationUseSomething_l10n_key, UiOperationUseSomething);
		UiOperationErrNotSeed = translator(UiOperationErrNotSeed_l10n_key, UiOperationErrNotSeed);
		UiOperationErrInvalidPlantbasin = translator(UiOperationErrInvalidPlantbasin_l10n_key, UiOperationErrInvalidPlantbasin);
		UiOperationErrInvalidSeason = translator(UiOperationErrInvalidSeason_l10n_key, UiOperationErrInvalidSeason);
		UiOperationErrLackOfAsset = translator(UiOperationErrLackOfAsset_l10n_key, UiOperationErrLackOfAsset);
		UiOperationErrLackOfEnergy = translator(UiOperationErrLackOfEnergy_l10n_key, UiOperationErrLackOfEnergy);
		UiOperationErrCannotPlantTree = translator(UiOperationErrCannotPlantTree_l10n_key, UiOperationErrCannotPlantTree);
		UiOperationErrCannotPlantOnGround = translator(UiOperationErrCannotPlantOnGround_l10n_key, UiOperationErrCannotPlantOnGround);
		UiOperationErrEmptyDrone = translator(UiOperationErrEmptyDrone_l10n_key, UiOperationErrEmptyDrone);
		UiOperationErrWellFull = translator(UiOperationErrWellFull_l10n_key, UiOperationErrWellFull);
		UiOperationErrWaterFull = translator(UiOperationErrWaterFull_l10n_key, UiOperationErrWaterFull);
		UiOperationErrRunoutWater = translator(UiOperationErrRunoutWater_l10n_key, UiOperationErrRunoutWater);
		UiOperationErrRunoutWaterAround = translator(UiOperationErrRunoutWaterAround_l10n_key, UiOperationErrRunoutWaterAround);
		UiOperationErrEquipmentCorroded = translator(UiOperationErrEquipmentCorroded_l10n_key, UiOperationErrEquipmentCorroded);
		UiOperationErrLowToolLevel = translator(UiOperationErrLowToolLevel_l10n_key, UiOperationErrLowToolLevel);
		UiOperationErrFullPlasticFilm = translator(UiOperationErrFullPlasticFilm_l10n_key, UiOperationErrFullPlasticFilm);
		UiOperationErrCannotFertilizer = translator(UiOperationErrCannotFertilizer_l10n_key, UiOperationErrCannotFertilizer);
		UiOperationErrDroneFullBattery = translator(UiOperationErrDroneFullBattery_l10n_key, UiOperationErrDroneFullBattery);
		UiOperationErrWaterEvaporated = translator(UiOperationErrWaterEvaporated_l10n_key, UiOperationErrWaterEvaporated);
		UiOperationErrFailToPlaceBox = translator(UiOperationErrFailToPlaceBox_l10n_key, UiOperationErrFailToPlaceBox);
		UiOperationErrCannotDisplay = translator(UiOperationErrCannotDisplay_l10n_key, UiOperationErrCannotDisplay);
		UiOperationErrCannotUseIfRiding = translator(UiOperationErrCannotUseIfRiding_l10n_key, UiOperationErrCannotUseIfRiding);
		UiOperationErrCannotCallMotor = translator(UiOperationErrCannotCallMotor_l10n_key, UiOperationErrCannotCallMotor);
		UiOperationErrSwordCannotAutofire = translator(UiOperationErrSwordCannotAutofire_l10n_key, UiOperationErrSwordCannotAutofire);
		UiOperationErrLowPower = translator(UiOperationErrLowPower_l10n_key, UiOperationErrLowPower);
		UiOperationErrCooling = translator(UiOperationErrCooling_l10n_key, UiOperationErrCooling);
		UiOperationErrBarrelFull = translator(UiOperationErrBarrelFull_l10n_key, UiOperationErrBarrelFull);
		UiOperationErrFuelFull = translator(UiOperationErrFuelFull_l10n_key, UiOperationErrFuelFull);
		UiOperationErrNotFeeds = translator(UiOperationErrNotFeeds_l10n_key, UiOperationErrNotFeeds);
		UiOperationErrFeederFull = translator(UiOperationErrFeederFull_l10n_key, UiOperationErrFeederFull);
		UiOperationErrSlotFull = translator(UiOperationErrSlotFull_l10n_key, UiOperationErrSlotFull);
		UiOperationErrNoSuitableEquipments = translator(UiOperationErrNoSuitableEquipments_l10n_key, UiOperationErrNoSuitableEquipments);
		UiStorageShelfFull = translator(UiStorageShelfFull_l10n_key, UiStorageShelfFull);
		UiOperationWeatherHasChanged = translator(UiOperationWeatherHasChanged_l10n_key, UiOperationWeatherHasChanged);
		UiOperationErrFuel = translator(UiOperationErrFuel_l10n_key, UiOperationErrFuel);
		UiOperationGiftItemFail = translator(UiOperationGiftItemFail_l10n_key, UiOperationGiftItemFail);
		UiOperationMakeEquipment = translator(UiOperationMakeEquipment_l10n_key, UiOperationMakeEquipment);
		UiOperationSell = translator(UiOperationSell_l10n_key, UiOperationSell);
		UiOperationSwitchAutoFire = translator(UiOperationSwitchAutoFire_l10n_key, UiOperationSwitchAutoFire);
		UiOperationSwitchManualFire = translator(UiOperationSwitchManualFire_l10n_key, UiOperationSwitchManualFire);
		UiOperationCollect = translator(UiOperationCollect_l10n_key, UiOperationCollect);
		UiOperationRide = translator(UiOperationRide_l10n_key, UiOperationRide);
		UiOperationNoAnimalBuilding = translator(UiOperationNoAnimalBuilding_l10n_key, UiOperationNoAnimalBuilding);
		UiOperationErrNoPlantBasin = translator(UiOperationErrNoPlantBasin_l10n_key, UiOperationErrNoPlantBasin);
		UiOperationErrNoBuildingHere = translator(UiOperationErrNoBuildingHere_l10n_key, UiOperationErrNoBuildingHere);
		UiOperationErrInvalidWallpaper = translator(UiOperationErrInvalidWallpaper_l10n_key, UiOperationErrInvalidWallpaper);
		UiOperationErrSameWallpaper = translator(UiOperationErrSameWallpaper_l10n_key, UiOperationErrSameWallpaper);
		UiOperationErrShouldInhouse = translator(UiOperationErrShouldInhouse_l10n_key, UiOperationErrShouldInhouse);
		UiOperationErrShouldOutside = translator(UiOperationErrShouldOutside_l10n_key, UiOperationErrShouldOutside);
		StorePlayerMoneyNotEnough = translator(StorePlayerMoneyNotEnough_l10n_key, StorePlayerMoneyNotEnough);
		StoreStoreMoneyNotEnough = translator(StoreStoreMoneyNotEnough_l10n_key, StoreStoreMoneyNotEnough);
		StoreItemNotSaleable = translator(StoreItemNotSaleable_l10n_key, StoreItemNotSaleable);
		StoreItemSoldOut = translator(StoreItemSoldOut_l10n_key, StoreItemSoldOut);
		StoreLackOfAsset = translator(StoreLackOfAsset_l10n_key, StoreLackOfAsset);
		StoreUpgradePrice = translator(StoreUpgradePrice_l10n_key, StoreUpgradePrice);
		StoreUpgradeConfirm = translator(StoreUpgradeConfirm_l10n_key, StoreUpgradeConfirm);
		StoreSoldOutIcon = translator(StoreSoldOutIcon_l10n_key, StoreSoldOutIcon);
		StoreUpgradeEmptyInfo = translator(StoreUpgradeEmptyInfo_l10n_key, StoreUpgradeEmptyInfo);
		StoreItemNotSaleableComment = translator(StoreItemNotSaleableComment_l10n_key, StoreItemNotSaleableComment);
		StoreQuantitySubmitSelling = translator(StoreQuantitySubmitSelling_l10n_key, StoreQuantitySubmitSelling);
		StoreQuantitySubmitCurrentMoneySelling = translator(StoreQuantitySubmitCurrentMoneySelling_l10n_key, StoreQuantitySubmitCurrentMoneySelling);
		StoreQuantitySubmitTotalMoneySelling = translator(StoreQuantitySubmitTotalMoneySelling_l10n_key, StoreQuantitySubmitTotalMoneySelling);
		StoreQuantitySubmitBuying = translator(StoreQuantitySubmitBuying_l10n_key, StoreQuantitySubmitBuying);
		StoreQuantitySubmitCurrentMoneyBuying = translator(StoreQuantitySubmitCurrentMoneyBuying_l10n_key, StoreQuantitySubmitCurrentMoneyBuying);
		StoreQuantitySubmitTotalMoneyBuying = translator(StoreQuantitySubmitTotalMoneyBuying_l10n_key, StoreQuantitySubmitTotalMoneyBuying);
		StoreQuantitySubmitCountInBack = translator(StoreQuantitySubmitCountInBack_l10n_key, StoreQuantitySubmitCountInBack);
		StoreQuantitySubmitUnitPrice = translator(StoreQuantitySubmitUnitPrice_l10n_key, StoreQuantitySubmitUnitPrice);
		StoreItemTipPlantbasinLocked = translator(StoreItemTipPlantbasinLocked_l10n_key, StoreItemTipPlantbasinLocked);
		FarmbuilderErrBuildSystemNotSupport = translator(FarmbuilderErrBuildSystemNotSupport_l10n_key, FarmbuilderErrBuildSystemNotSupport);
		FarmbuilderErrLackOfAsset = translator(FarmbuilderErrLackOfAsset_l10n_key, FarmbuilderErrLackOfAsset);
		FarmbuilderErrIndoorEquipment = translator(FarmbuilderErrIndoorEquipment_l10n_key, FarmbuilderErrIndoorEquipment);
		FarmbuilderErrOutdoorEquipment = translator(FarmbuilderErrOutdoorEquipment_l10n_key, FarmbuilderErrOutdoorEquipment);
		FarmbuilderErrInvalidPosition = translator(FarmbuilderErrInvalidPosition_l10n_key, FarmbuilderErrInvalidPosition);
		FarmbuilderErrOutOfRange = translator(FarmbuilderErrOutOfRange_l10n_key, FarmbuilderErrOutOfRange);
		FarmbuilderErrEquipmentAreaNotEmpty = translator(FarmbuilderErrEquipmentAreaNotEmpty_l10n_key, FarmbuilderErrEquipmentAreaNotEmpty);
		FarmbuilderErrPlatformOccupied = translator(FarmbuilderErrPlatformOccupied_l10n_key, FarmbuilderErrPlatformOccupied);
		FarmbuilderQuestionRemove = translator(FarmbuilderQuestionRemove_l10n_key, FarmbuilderQuestionRemove);
		FarmbuilderQuestionRemoveReturn = translator(FarmbuilderQuestionRemoveReturn_l10n_key, FarmbuilderQuestionRemoveReturn);
		FarmbuilderErrEquipmentOccupied = translator(FarmbuilderErrEquipmentOccupied_l10n_key, FarmbuilderErrEquipmentOccupied);
		FarmbuilderErrPlantbasinTreeOccupied = translator(FarmbuilderErrPlantbasinTreeOccupied_l10n_key, FarmbuilderErrPlantbasinTreeOccupied);
		FarmbuilderErrParkingApronOccupied = translator(FarmbuilderErrParkingApronOccupied_l10n_key, FarmbuilderErrParkingApronOccupied);
		FarmbuilderErrAutomateBotOccupied = translator(FarmbuilderErrAutomateBotOccupied_l10n_key, FarmbuilderErrAutomateBotOccupied);
		FarmbuilderErrShowCaseOccupied = translator(FarmbuilderErrShowCaseOccupied_l10n_key, FarmbuilderErrShowCaseOccupied);
		FarmbuilderErrPlatformRemove = translator(FarmbuilderErrPlatformRemove_l10n_key, FarmbuilderErrPlatformRemove);
		FarmbuilderErrEquipmentHideDoor = translator(FarmbuilderErrEquipmentHideDoor_l10n_key, FarmbuilderErrEquipmentHideDoor);
		FarmbuilderErrBuildingOccupied = translator(FarmbuilderErrBuildingOccupied_l10n_key, FarmbuilderErrBuildingOccupied);
		FarmbuilderErrBuildingOccupiedAnimal = translator(FarmbuilderErrBuildingOccupiedAnimal_l10n_key, FarmbuilderErrBuildingOccupiedAnimal);
		FarmbuilderErrBuildingAreaNotEmpty = translator(FarmbuilderErrBuildingAreaNotEmpty_l10n_key, FarmbuilderErrBuildingAreaNotEmpty);
		FarmbuilderErrBuildingInvalidHeight = translator(FarmbuilderErrBuildingInvalidHeight_l10n_key, FarmbuilderErrBuildingInvalidHeight);
		FarmbuilderErrBuildingInvalidCoveredRatio = translator(FarmbuilderErrBuildingInvalidCoveredRatio_l10n_key, FarmbuilderErrBuildingInvalidCoveredRatio);
		FarmbuilderErrBuildingCanNotRemove = translator(FarmbuilderErrBuildingCanNotRemove_l10n_key, FarmbuilderErrBuildingCanNotRemove);
		FarmbuilderErrBuildingInvalidOtherNoSupport = translator(FarmbuilderErrBuildingInvalidOtherNoSupport_l10n_key, FarmbuilderErrBuildingInvalidOtherNoSupport);
		FarmbuilderErrBuildingOccupiedBuilding = translator(FarmbuilderErrBuildingOccupiedBuilding_l10n_key, FarmbuilderErrBuildingOccupiedBuilding);
		FarmbuilderErrBuildingInvalidDoorBeHidden = translator(FarmbuilderErrBuildingInvalidDoorBeHidden_l10n_key, FarmbuilderErrBuildingInvalidDoorBeHidden);
		FarmbuilderErrBuildingOccupiedCeiling = translator(FarmbuilderErrBuildingOccupiedCeiling_l10n_key, FarmbuilderErrBuildingOccupiedCeiling);
		FarmbuilderErrBuildingOccupiedDoor = translator(FarmbuilderErrBuildingOccupiedDoor_l10n_key, FarmbuilderErrBuildingOccupiedDoor);
		FarmbuilderErrBuildingOccupiedCeilingPt = translator(FarmbuilderErrBuildingOccupiedCeilingPt_l10n_key, FarmbuilderErrBuildingOccupiedCeilingPt);
		FarmbuilderErrBuildingGroundInvalid = translator(FarmbuilderErrBuildingGroundInvalid_l10n_key, FarmbuilderErrBuildingGroundInvalid);
		FarmbuilderErrPlatformWidth = translator(FarmbuilderErrPlatformWidth_l10n_key, FarmbuilderErrPlatformWidth);
		FarmbuilderErrPlatformHeightMax = translator(FarmbuilderErrPlatformHeightMax_l10n_key, FarmbuilderErrPlatformHeightMax);
		FarmbuilderErrEquipmentLimited = translator(FarmbuilderErrEquipmentLimited_l10n_key, FarmbuilderErrEquipmentLimited);
		TechtreePanelTitle = translator(TechtreePanelTitle_l10n_key, TechtreePanelTitle);
		TechtreeNodeLackOfPoints = translator(TechtreeNodeLackOfPoints_l10n_key, TechtreeNodeLackOfPoints);
		TechtreeNodeNotAvaiable = translator(TechtreeNodeNotAvaiable_l10n_key, TechtreeNodeNotAvaiable);
		TechtreeNodeUnlocked = translator(TechtreeNodeUnlocked_l10n_key, TechtreeNodeUnlocked);
		TechtreeNodeUnopen = translator(TechtreeNodeUnopen_l10n_key, TechtreeNodeUnopen);
		TechtreeNodeUnlockConfirm = translator(TechtreeNodeUnlockConfirm_l10n_key, TechtreeNodeUnlockConfirm);
		TechtreeNodeBuildingHealth = translator(TechtreeNodeBuildingHealth_l10n_key, TechtreeNodeBuildingHealth);
		TechtreeNodeEquipmentElectronic = translator(TechtreeNodeEquipmentElectronic_l10n_key, TechtreeNodeEquipmentElectronic);
		TechtreeProgressInfo = translator(TechtreeProgressInfo_l10n_key, TechtreeProgressInfo);
		TechtreeMaxLevel = translator(TechtreeMaxLevel_l10n_key, TechtreeMaxLevel);
		TechtreeNodeHint = translator(TechtreeNodeHint_l10n_key, TechtreeNodeHint);
		MissionUpdate = translator(MissionUpdate_l10n_key, MissionUpdate);
		MissionComplete = translator(MissionComplete_l10n_key, MissionComplete);
		MissionCompleteSendEmail = translator(MissionCompleteSendEmail_l10n_key, MissionCompleteSendEmail);
		MissionPanelTitle = translator(MissionPanelTitle_l10n_key, MissionPanelTitle);
		MissionPanelEmpty = translator(MissionPanelEmpty_l10n_key, MissionPanelEmpty);
		UiMissionTimeLimit = translator(UiMissionTimeLimit_l10n_key, UiMissionTimeLimit);
		UiMissionNpcPosition = translator(UiMissionNpcPosition_l10n_key, UiMissionNpcPosition);
		SeedPanelTitle = translator(SeedPanelTitle_l10n_key, SeedPanelTitle);
		SeedNodeAlreadyUnlock = translator(SeedNodeAlreadyUnlock_l10n_key, SeedNodeAlreadyUnlock);
		SeedNodeSucceedUnlock = translator(SeedNodeSucceedUnlock_l10n_key, SeedNodeSucceedUnlock);
		BuildingPanelTitle = translator(BuildingPanelTitle_l10n_key, BuildingPanelTitle);
		BuildingPanelCoverSizeDescription = translator(BuildingPanelCoverSizeDescription_l10n_key, BuildingPanelCoverSizeDescription);
		BuildingPanelInnerSizeDescription = translator(BuildingPanelInnerSizeDescription_l10n_key, BuildingPanelInnerSizeDescription);
		BuildingPanelAnimalCapacityDescription = translator(BuildingPanelAnimalCapacityDescription_l10n_key, BuildingPanelAnimalCapacityDescription);
		BuildingPanelSizeDescription = translator(BuildingPanelSizeDescription_l10n_key, BuildingPanelSizeDescription);
		BuildingPanelEmpty = translator(BuildingPanelEmpty_l10n_key, BuildingPanelEmpty);
		BuildingPanelStartBuild = translator(BuildingPanelStartBuild_l10n_key, BuildingPanelStartBuild);
		BuildingPanelMaterialNotEnough = translator(BuildingPanelMaterialNotEnough_l10n_key, BuildingPanelMaterialNotEnough);
		BuildingPanelMoneyNotEnough = translator(BuildingPanelMoneyNotEnough_l10n_key, BuildingPanelMoneyNotEnough);
		BuildingPanelInputBuildingName = translator(BuildingPanelInputBuildingName_l10n_key, BuildingPanelInputBuildingName);
		EquipmentPanelEmpty = translator(EquipmentPanelEmpty_l10n_key, EquipmentPanelEmpty);
		EquipmentPanelStartBuild = translator(EquipmentPanelStartBuild_l10n_key, EquipmentPanelStartBuild);
		EquipmentPanelListName = translator(EquipmentPanelListName_l10n_key, EquipmentPanelListName);
		EquipmentViewerEmpty = translator(EquipmentViewerEmpty_l10n_key, EquipmentViewerEmpty);
		EquipmentPanelLatelyEmpty = translator(EquipmentPanelLatelyEmpty_l10n_key, EquipmentPanelLatelyEmpty);
		EquipmentPanelMakeComplete = translator(EquipmentPanelMakeComplete_l10n_key, EquipmentPanelMakeComplete);
		EquipmentPanelHide = translator(EquipmentPanelHide_l10n_key, EquipmentPanelHide);
		EquipmentPanelJump = translator(EquipmentPanelJump_l10n_key, EquipmentPanelJump);
		EquipmentPanelUnlockTip = translator(EquipmentPanelUnlockTip_l10n_key, EquipmentPanelUnlockTip);
		EquipmentPanelElectronicTip = translator(EquipmentPanelElectronicTip_l10n_key, EquipmentPanelElectronicTip);
		EquipmentPanelBatteryTip = translator(EquipmentPanelBatteryTip_l10n_key, EquipmentPanelBatteryTip);
		EquipmentPanelApplianceTip = translator(EquipmentPanelApplianceTip_l10n_key, EquipmentPanelApplianceTip);
		EquipmentPanelGeneratorTip = translator(EquipmentPanelGeneratorTip_l10n_key, EquipmentPanelGeneratorTip);
		UiEmailTitle = translator(UiEmailTitle_l10n_key, UiEmailTitle);
		UiEmailEmpty = translator(UiEmailEmpty_l10n_key, UiEmailEmpty);
		UiEmailAcceptMission = translator(UiEmailAcceptMission_l10n_key, UiEmailAcceptMission);
		UiEmailAlreadyAccepted = translator(UiEmailAlreadyAccepted_l10n_key, UiEmailAlreadyAccepted);
		UiEmailReciveItem = translator(UiEmailReciveItem_l10n_key, UiEmailReciveItem);
		UiEmailAlreadyRecived = translator(UiEmailAlreadyRecived_l10n_key, UiEmailAlreadyRecived);
		UiEmailRecycleErr = translator(UiEmailRecycleErr_l10n_key, UiEmailRecycleErr);
		RecipePanelEmpty = translator(RecipePanelEmpty_l10n_key, RecipePanelEmpty);
		RecipePanelStartBuild = translator(RecipePanelStartBuild_l10n_key, RecipePanelStartBuild);
		RecipePanelTimeInfo = translator(RecipePanelTimeInfo_l10n_key, RecipePanelTimeInfo);
		RecipePanelAlreadyWorking = translator(RecipePanelAlreadyWorking_l10n_key, RecipePanelAlreadyWorking);
		RecipePanelTitle = translator(RecipePanelTitle_l10n_key, RecipePanelTitle);
		RecipePanelTask = translator(RecipePanelTask_l10n_key, RecipePanelTask);
		RecipePanelRestTime = translator(RecipePanelRestTime_l10n_key, RecipePanelRestTime);
		RecipePanelMaxCraftCount = translator(RecipePanelMaxCraftCount_l10n_key, RecipePanelMaxCraftCount);
		RecipePanelLimitUp = translator(RecipePanelLimitUp_l10n_key, RecipePanelLimitUp);
		RecipePanelNotCookable = translator(RecipePanelNotCookable_l10n_key, RecipePanelNotCookable);
		RecipePanelNoMaterial = translator(RecipePanelNoMaterial_l10n_key, RecipePanelNoMaterial);
		RecipePanelConfirmWithTime = translator(RecipePanelConfirmWithTime_l10n_key, RecipePanelConfirmWithTime);
		RecipePanelUnlockedRecipeComment = translator(RecipePanelUnlockedRecipeComment_l10n_key, RecipePanelUnlockedRecipeComment);
		RecipePanelUnlockedDishComment = translator(RecipePanelUnlockedDishComment_l10n_key, RecipePanelUnlockedDishComment);
		RecipePanelUnknowDishComment = translator(RecipePanelUnknowDishComment_l10n_key, RecipePanelUnknowDishComment);
		RecipePanelConfirmPopBuffer = translator(RecipePanelConfirmPopBuffer_l10n_key, RecipePanelConfirmPopBuffer);
		RecipePanelMaterialList = translator(RecipePanelMaterialList_l10n_key, RecipePanelMaterialList);
		RecipePanelExistItems = translator(RecipePanelExistItems_l10n_key, RecipePanelExistItems);
		RecipePanelRandomGeneHint = translator(RecipePanelRandomGeneHint_l10n_key, RecipePanelRandomGeneHint);
		PlatformPanelTitle = translator(PlatformPanelTitle_l10n_key, PlatformPanelTitle);
		PlatformPanelEmpty = translator(PlatformPanelEmpty_l10n_key, PlatformPanelEmpty);
		PlatformPanelStartBuild = translator(PlatformPanelStartBuild_l10n_key, PlatformPanelStartBuild);
		PlatformPanelCostPrefix = translator(PlatformPanelCostPrefix_l10n_key, PlatformPanelCostPrefix);
		ItemBuildingProtoError = translator(ItemBuildingProtoError_l10n_key, ItemBuildingProtoError);
		ItemEquipmentProtoError = translator(ItemEquipmentProtoError_l10n_key, ItemEquipmentProtoError);
		ItemSendEmailOnOverflow = translator(ItemSendEmailOnOverflow_l10n_key, ItemSendEmailOnOverflow);
		ItemSleepingBagConditionFailedMonster = translator(ItemSleepingBagConditionFailedMonster_l10n_key, ItemSleepingBagConditionFailedMonster);
		ItemSleepingBagConditionFailedWater = translator(ItemSleepingBagConditionFailedWater_l10n_key, ItemSleepingBagConditionFailedWater);
		ItemRescuePagerConditionFailed = translator(ItemRescuePagerConditionFailed_l10n_key, ItemRescuePagerConditionFailed);
		ItemTipPrice = translator(ItemTipPrice_l10n_key, ItemTipPrice);
		ItemTipBasicsPrice = translator(ItemTipBasicsPrice_l10n_key, ItemTipBasicsPrice);
		ItemTipMoneyUnit = translator(ItemTipMoneyUnit_l10n_key, ItemTipMoneyUnit);
		ItemChipAttackIncrease = translator(ItemChipAttackIncrease_l10n_key, ItemChipAttackIncrease);
		ItemChipAttackIncreaseFixed = translator(ItemChipAttackIncreaseFixed_l10n_key, ItemChipAttackIncreaseFixed);
		ItemChipCriticalRateIncrease = translator(ItemChipCriticalRateIncrease_l10n_key, ItemChipCriticalRateIncrease);
		ItemChipAttackSpeedIncrease = translator(ItemChipAttackSpeedIncrease_l10n_key, ItemChipAttackSpeedIncrease);
		ItemChipAccuracyIncrease = translator(ItemChipAccuracyIncrease_l10n_key, ItemChipAccuracyIncrease);
		ItemChipPowerCostDecrease = translator(ItemChipPowerCostDecrease_l10n_key, ItemChipPowerCostDecrease);
		ItemChipAttackDistanceIncrease = translator(ItemChipAttackDistanceIncrease_l10n_key, ItemChipAttackDistanceIncrease);
		ItemChipMoveSpeedIncrease = translator(ItemChipMoveSpeedIncrease_l10n_key, ItemChipMoveSpeedIncrease);
		ItemChipClipCapacityAddition = translator(ItemChipClipCapacityAddition_l10n_key, ItemChipClipCapacityAddition);
		ItemChipReloadDurationDecrease = translator(ItemChipReloadDurationDecrease_l10n_key, ItemChipReloadDurationDecrease);
		ItemEngineMoveSpeedIncrease = translator(ItemEngineMoveSpeedIncrease_l10n_key, ItemEngineMoveSpeedIncrease);
		ItemEnginePowerCapacityIncrease = translator(ItemEnginePowerCapacityIncrease_l10n_key, ItemEnginePowerCapacityIncrease);
		ItemEnginePowerRecvIncrease = translator(ItemEnginePowerRecvIncrease_l10n_key, ItemEnginePowerRecvIncrease);
		ItemStructureMoveSpeedIncrease = translator(ItemStructureMoveSpeedIncrease_l10n_key, ItemStructureMoveSpeedIncrease);
		ItemStructurePowerCapacity = translator(ItemStructurePowerCapacity_l10n_key, ItemStructurePowerCapacity);
		ItemStructurePowerRecv = translator(ItemStructurePowerRecv_l10n_key, ItemStructurePowerRecv);
		ItemMaxDurability = translator(ItemMaxDurability_l10n_key, ItemMaxDurability);
		ItemTitleInfoFormat = translator(ItemTitleInfoFormat_l10n_key, ItemTitleInfoFormat);
		ItemConfirmUse = translator(ItemConfirmUse_l10n_key, ItemConfirmUse);
		ItemWaterCanArea = translator(ItemWaterCanArea_l10n_key, ItemWaterCanArea);
		ItemWaterCanEndlessWater = translator(ItemWaterCanEndlessWater_l10n_key, ItemWaterCanEndlessWater);
		ItemGeneDescFormat = translator(ItemGeneDescFormat_l10n_key, ItemGeneDescFormat);
		ItemSeedCloned = translator(ItemSeedCloned_l10n_key, ItemSeedCloned);
		ItemWpIsAvailableFor = translator(ItemWpIsAvailableFor_l10n_key, ItemWpIsAvailableFor);
		ItemWpAvailableAll = translator(ItemWpAvailableAll_l10n_key, ItemWpAvailableAll);
		ItemFishFryTitleFormat = translator(ItemFishFryTitleFormat_l10n_key, ItemFishFryTitleFormat);
		ItemToolLevelFormat = translator(ItemToolLevelFormat_l10n_key, ItemToolLevelFormat);
		ItemPatchValueFormat = translator(ItemPatchValueFormat_l10n_key, ItemPatchValueFormat);
		ItemBoxValueFormat = translator(ItemBoxValueFormat_l10n_key, ItemBoxValueFormat);
		ItemFilmValueFormat = translator(ItemFilmValueFormat_l10n_key, ItemFilmValueFormat);
		ItemDroneWeaponValueFormat = translator(ItemDroneWeaponValueFormat_l10n_key, ItemDroneWeaponValueFormat);
		ItemMoneyTitle = translator(ItemMoneyTitle_l10n_key, ItemMoneyTitle);
		ItemMoneyDesc = translator(ItemMoneyDesc_l10n_key, ItemMoneyDesc);
		ItemMoneyType = translator(ItemMoneyType_l10n_key, ItemMoneyType);
		UiSystemExitGame = translator(UiSystemExitGame_l10n_key, UiSystemExitGame);
		UiSystemReturnHomePage = translator(UiSystemReturnHomePage_l10n_key, UiSystemReturnHomePage);
		UiSystemConfirmHome = translator(UiSystemConfirmHome_l10n_key, UiSystemConfirmHome);
		UiSystemConfirmExit = translator(UiSystemConfirmExit_l10n_key, UiSystemConfirmExit);
		RewardInfoBuildingUnlock = translator(RewardInfoBuildingUnlock_l10n_key, RewardInfoBuildingUnlock);
		RewardInfoBusStationUnlock = translator(RewardInfoBusStationUnlock_l10n_key, RewardInfoBusStationUnlock);
		RewardInfoEquipmentUnlock = translator(RewardInfoEquipmentUnlock_l10n_key, RewardInfoEquipmentUnlock);
		RewardInfoPlatformUnlock = translator(RewardInfoPlatformUnlock_l10n_key, RewardInfoPlatformUnlock);
		RewardInfoRecipeUnlock = translator(RewardInfoRecipeUnlock_l10n_key, RewardInfoRecipeUnlock);
		RewardInfoRecipeUnlockHint = translator(RewardInfoRecipeUnlockHint_l10n_key, RewardInfoRecipeUnlockHint);
		RewardInfoItem = translator(RewardInfoItem_l10n_key, RewardInfoItem);
		RewardInfoGold = translator(RewardInfoGold_l10n_key, RewardInfoGold);
		RewardInfoFavorabilityLv1 = translator(RewardInfoFavorabilityLv1_l10n_key, RewardInfoFavorabilityLv1);
		RewardInfoFavorabilityLv2 = translator(RewardInfoFavorabilityLv2_l10n_key, RewardInfoFavorabilityLv2);
		RewardInfoFavorabilityLv3 = translator(RewardInfoFavorabilityLv3_l10n_key, RewardInfoFavorabilityLv3);
		DropoffBoxNoGoods = translator(DropoffBoxNoGoods_l10n_key, DropoffBoxNoGoods);
		DropoffBoxNoDrone = translator(DropoffBoxNoDrone_l10n_key, DropoffBoxNoDrone);
		DropoffBoxTotalMoney = translator(DropoffBoxTotalMoney_l10n_key, DropoffBoxTotalMoney);
		DropoffBoxPriceIncreased = translator(DropoffBoxPriceIncreased_l10n_key, DropoffBoxPriceIncreased);
		DropoffBoxLaunchDrone = translator(DropoffBoxLaunchDrone_l10n_key, DropoffBoxLaunchDrone);
		DropoffBoxConfirmLauch = translator(DropoffBoxConfirmLauch_l10n_key, DropoffBoxConfirmLauch);
		AutomateBotPanelStateIdle = translator(AutomateBotPanelStateIdle_l10n_key, AutomateBotPanelStateIdle);
		AutomateBotPanelStateCharge = translator(AutomateBotPanelStateCharge_l10n_key, AutomateBotPanelStateCharge);
		AutomateBotPanelStatePause = translator(AutomateBotPanelStatePause_l10n_key, AutomateBotPanelStatePause);
		AutomateBotPanelStateWorking = translator(AutomateBotPanelStateWorking_l10n_key, AutomateBotPanelStateWorking);
		AutomateBotPanelCfgEmpty = translator(AutomateBotPanelCfgEmpty_l10n_key, AutomateBotPanelCfgEmpty);
		AutomateBotPanelRecipeEmpty = translator(AutomateBotPanelRecipeEmpty_l10n_key, AutomateBotPanelRecipeEmpty);
		AutomateBotPanelDateEmpty = translator(AutomateBotPanelDateEmpty_l10n_key, AutomateBotPanelDateEmpty);
		AutomateBotPanelRecipeType = translator(AutomateBotPanelRecipeType_l10n_key, AutomateBotPanelRecipeType);
		AutomateBotPanelRecipeSubType = translator(AutomateBotPanelRecipeSubType_l10n_key, AutomateBotPanelRecipeSubType);
		AutomateBotPanelAutoFertilizer = translator(AutomateBotPanelAutoFertilizer_l10n_key, AutomateBotPanelAutoFertilizer);
		AutomateBotPanelAutoProtect = translator(AutomateBotPanelAutoProtect_l10n_key, AutomateBotPanelAutoProtect);
		AutomateBotPanelEnergyType = translator(AutomateBotPanelEnergyType_l10n_key, AutomateBotPanelEnergyType);
		AutomateBotPanelItemError = translator(AutomateBotPanelItemError_l10n_key, AutomateBotPanelItemError);
		BoardMissionPanelTitle = translator(BoardMissionPanelTitle_l10n_key, BoardMissionPanelTitle);
		BoardMissionUrgencyLow = translator(BoardMissionUrgencyLow_l10n_key, BoardMissionUrgencyLow);
		BoardMissionUrgencyMiddle = translator(BoardMissionUrgencyMiddle_l10n_key, BoardMissionUrgencyMiddle);
		BoardMissionUrgencyHigh = translator(BoardMissionUrgencyHigh_l10n_key, BoardMissionUrgencyHigh);
		BoardMissionUrgencyNone = translator(BoardMissionUrgencyNone_l10n_key, BoardMissionUrgencyNone);
		BoardMissionAcceptFail = translator(BoardMissionAcceptFail_l10n_key, BoardMissionAcceptFail);
		BoardMissionTitle = translator(BoardMissionTitle_l10n_key, BoardMissionTitle);
		BoardMissionLowLevel = translator(BoardMissionLowLevel_l10n_key, BoardMissionLowLevel);
		BoardMissionEmpty = translator(BoardMissionEmpty_l10n_key, BoardMissionEmpty);
		BoardMissionOverdue = translator(BoardMissionOverdue_l10n_key, BoardMissionOverdue);
		BoardMissionLv = translator(BoardMissionLv_l10n_key, BoardMissionLv);
		BoardMissionExpTip = translator(BoardMissionExpTip_l10n_key, BoardMissionExpTip);
		InventoryPanelCannotPutIn = translator(InventoryPanelCannotPutIn_l10n_key, InventoryPanelCannotPutIn);
		InventoryPanelContainerFull = translator(InventoryPanelContainerFull_l10n_key, InventoryPanelContainerFull);
		BoxPanelRestCount = translator(BoxPanelRestCount_l10n_key, BoxPanelRestCount);
		BoxPanelUsedUpWarning = translator(BoxPanelUsedUpWarning_l10n_key, BoxPanelUsedUpWarning);
		BoxPanelBroken = translator(BoxPanelBroken_l10n_key, BoxPanelBroken);
		BoxPanelAlreadyOpen = translator(BoxPanelAlreadyOpen_l10n_key, BoxPanelAlreadyOpen);
		BoxPanelNoNeedRepair = translator(BoxPanelNoNeedRepair_l10n_key, BoxPanelNoNeedRepair);
		BoxPanelNoRepairCost = translator(BoxPanelNoRepairCost_l10n_key, BoxPanelNoRepairCost);
		BoxPanelRepairInfo = translator(BoxPanelRepairInfo_l10n_key, BoxPanelRepairInfo);
		InventoryPanelDungeonCaseTitle = translator(InventoryPanelDungeonCaseTitle_l10n_key, InventoryPanelDungeonCaseTitle);
		InventoryPanelBackpackTitle = translator(InventoryPanelBackpackTitle_l10n_key, InventoryPanelBackpackTitle);
		FishTankPanelFeedQuantity = translator(FishTankPanelFeedQuantity_l10n_key, FishTankPanelFeedQuantity);
		InventoryPanelSocketTitle = translator(InventoryPanelSocketTitle_l10n_key, InventoryPanelSocketTitle);
		InventoryPanelPutBoxFirst = translator(InventoryPanelPutBoxFirst_l10n_key, InventoryPanelPutBoxFirst);
		InventoryPanelInfoGeneIncubator = translator(InventoryPanelInfoGeneIncubator_l10n_key, InventoryPanelInfoGeneIncubator);
		InventoryPanelCapsuleTitle = translator(InventoryPanelCapsuleTitle_l10n_key, InventoryPanelCapsuleTitle);
		InventoryPanelInfoGeneReplicator = translator(InventoryPanelInfoGeneReplicator_l10n_key, InventoryPanelInfoGeneReplicator);
		InventoryPanelGeneReplicatorLocked = translator(InventoryPanelGeneReplicatorLocked_l10n_key, InventoryPanelGeneReplicatorLocked);
		InventoryPanelInfoGeneSynthesizer = translator(InventoryPanelInfoGeneSynthesizer_l10n_key, InventoryPanelInfoGeneSynthesizer);
		InventoryPanelGeneSynthesizerLocked = translator(InventoryPanelGeneSynthesizerLocked_l10n_key, InventoryPanelGeneSynthesizerLocked);
		UiTipTimeTitle = translator(UiTipTimeTitle_l10n_key, UiTipTimeTitle);
		UiTipTimeFormat = translator(UiTipTimeFormat_l10n_key, UiTipTimeFormat);
		UiTipCurrentSeason = translator(UiTipCurrentSeason_l10n_key, UiTipCurrentSeason);
		UiTipOpenMenu = translator(UiTipOpenMenu_l10n_key, UiTipOpenMenu);
		UiTipOpenMap = translator(UiTipOpenMap_l10n_key, UiTipOpenMap);
		UiTipShowMissionTip = translator(UiTipShowMissionTip_l10n_key, UiTipShowMissionTip);
		UiTipHideMissionTip = translator(UiTipHideMissionTip_l10n_key, UiTipHideMissionTip);
		UiTipNoMission = translator(UiTipNoMission_l10n_key, UiTipNoMission);
		UiTipMissionShowDetails = translator(UiTipMissionShowDetails_l10n_key, UiTipMissionShowDetails);
		UiTipHealthValue = translator(UiTipHealthValue_l10n_key, UiTipHealthValue);
		UiTipEnergyValue = translator(UiTipEnergyValue_l10n_key, UiTipEnergyValue);
		UiTipCorrosionValue = translator(UiTipCorrosionValue_l10n_key, UiTipCorrosionValue);
		UiTipCurrentSpiritLe50 = translator(UiTipCurrentSpiritLe50_l10n_key, UiTipCurrentSpiritLe50);
		UiTipCurrentSpiritLe25 = translator(UiTipCurrentSpiritLe25_l10n_key, UiTipCurrentSpiritLe25);
		UiTipCurrentSpiritLe10 = translator(UiTipCurrentSpiritLe10_l10n_key, UiTipCurrentSpiritLe10);
		UiTipCurrentSpiritLe0 = translator(UiTipCurrentSpiritLe0_l10n_key, UiTipCurrentSpiritLe0);
		UiTipBuffDuration = translator(UiTipBuffDuration_l10n_key, UiTipBuffDuration);
		UiTipDebuffDuration = translator(UiTipDebuffDuration_l10n_key, UiTipDebuffDuration);
		UiTipBuildHealth = translator(UiTipBuildHealth_l10n_key, UiTipBuildHealth);
		UiTipElectricityGeneratorInfo = translator(UiTipElectricityGeneratorInfo_l10n_key, UiTipElectricityGeneratorInfo);
		UiTipElectricityBatteryInfo = translator(UiTipElectricityBatteryInfo_l10n_key, UiTipElectricityBatteryInfo);
		UiTipElectricityStatusNone = translator(UiTipElectricityStatusNone_l10n_key, UiTipElectricityStatusNone);
		UiTipElectricityStatusLackOfGeneration = translator(UiTipElectricityStatusLackOfGeneration_l10n_key, UiTipElectricityStatusLackOfGeneration);
		UiTipElectricityStatusLackOfGenerationCostBattery = translator(UiTipElectricityStatusLackOfGenerationCostBattery_l10n_key, UiTipElectricityStatusLackOfGenerationCostBattery);
		UiTipElectricityStatusBatterySaving = translator(UiTipElectricityStatusBatterySaving_l10n_key, UiTipElectricityStatusBatterySaving);
		UiTipElectricityStatusBatteryFull = translator(UiTipElectricityStatusBatteryFull_l10n_key, UiTipElectricityStatusBatteryFull);
		UiTipElectricityStatusPowerLoss = translator(UiTipElectricityStatusPowerLoss_l10n_key, UiTipElectricityStatusPowerLoss);
		UiTipSeedEnd = translator(UiTipSeedEnd_l10n_key, UiTipSeedEnd);
		UiTipLoading = translator(UiTipLoading_l10n_key, UiTipLoading);
		DocumentTitleComputer = translator(DocumentTitleComputer_l10n_key, DocumentTitleComputer);
		NpcDocumentTitle = translator(NpcDocumentTitle_l10n_key, NpcDocumentTitle);
		ChipDocumentTitle = translator(ChipDocumentTitle_l10n_key, ChipDocumentTitle);
		PlantDocumentTitle = translator(PlantDocumentTitle_l10n_key, PlantDocumentTitle);
		DocumentEmpty = translator(DocumentEmpty_l10n_key, DocumentEmpty);
		ChipDocumentTotle = translator(ChipDocumentTotle_l10n_key, ChipDocumentTotle);
		GameDataPanelTitle = translator(GameDataPanelTitle_l10n_key, GameDataPanelTitle);
		GameDataErrRead = translator(GameDataErrRead_l10n_key, GameDataErrRead);
		GameDataTitle = translator(GameDataTitle_l10n_key, GameDataTitle);
		GameDataDelConfirm = translator(GameDataDelConfirm_l10n_key, GameDataDelConfirm);
		GameDataDelSuccess = translator(GameDataDelSuccess_l10n_key, GameDataDelSuccess);
		GameDataErrEmpty = translator(GameDataErrEmpty_l10n_key, GameDataErrEmpty);
		GameDataSaving = translator(GameDataSaving_l10n_key, GameDataSaving);
		GameDataSaveFail = translator(GameDataSaveFail_l10n_key, GameDataSaveFail);
		GameDataSaveSuccessful = translator(GameDataSaveSuccessful_l10n_key, GameDataSaveSuccessful);
		GameDataFull = translator(GameDataFull_l10n_key, GameDataFull);
		GameDataDuplicateSucess = translator(GameDataDuplicateSucess_l10n_key, GameDataDuplicateSucess);
		GameDataStart = translator(GameDataStart_l10n_key, GameDataStart);
		GameDataLoad = translator(GameDataLoad_l10n_key, GameDataLoad);
		TeleportFail = translator(TeleportFail_l10n_key, TeleportFail);
		TeleportConfirm = translator(TeleportConfirm_l10n_key, TeleportConfirm);
		TeleportBuyTicket = translator(TeleportBuyTicket_l10n_key, TeleportBuyTicket);
		TeleportErrMaterialNotEnough = translator(TeleportErrMaterialNotEnough_l10n_key, TeleportErrMaterialNotEnough);
		FactionMissionFinish = translator(FactionMissionFinish_l10n_key, FactionMissionFinish);
		FactionMissionCannotSubmit = translator(FactionMissionCannotSubmit_l10n_key, FactionMissionCannotSubmit);
		FactionMissionFinishSubmit = translator(FactionMissionFinishSubmit_l10n_key, FactionMissionFinishSubmit);
		FactionMissionMoneySubmit = translator(FactionMissionMoneySubmit_l10n_key, FactionMissionMoneySubmit);
		FactionMissionErrSubmit = translator(FactionMissionErrSubmit_l10n_key, FactionMissionErrSubmit);
		FactionMissionLock = translator(FactionMissionLock_l10n_key, FactionMissionLock);
		TreatyPortTitle = translator(TreatyPortTitle_l10n_key, TreatyPortTitle);
		TreatyPortRepairTime = translator(TreatyPortRepairTime_l10n_key, TreatyPortRepairTime);
		TreatyPortRecruitStats = translator(TreatyPortRecruitStats_l10n_key, TreatyPortRecruitStats);
		TreatyPortPrincipal = translator(TreatyPortPrincipal_l10n_key, TreatyPortPrincipal);
		TreatyPortFactionMissionProgress = translator(TreatyPortFactionMissionProgress_l10n_key, TreatyPortFactionMissionProgress);
		TreatyPortFactionReputation = translator(TreatyPortFactionReputation_l10n_key, TreatyPortFactionReputation);
		TreatyPortContactNpc = translator(TreatyPortContactNpc_l10n_key, TreatyPortContactNpc);
		TreatyPortFactionEnterTime = translator(TreatyPortFactionEnterTime_l10n_key, TreatyPortFactionEnterTime);
		TreatyPortBroadcast = translator(TreatyPortBroadcast_l10n_key, TreatyPortBroadcast);
		TreatyPortFactionLock = translator(TreatyPortFactionLock_l10n_key, TreatyPortFactionLock);
		TreatyPortFactionUnopen = translator(TreatyPortFactionUnopen_l10n_key, TreatyPortFactionUnopen);
		TreatyPortRecruitHint = translator(TreatyPortRecruitHint_l10n_key, TreatyPortRecruitHint);
		TreatyPortTimeFormat = translator(TreatyPortTimeFormat_l10n_key, TreatyPortTimeFormat);
		TreatyPortFactionRefuse = translator(TreatyPortFactionRefuse_l10n_key, TreatyPortFactionRefuse);
		TreatyPortOffDutyHoursTip = translator(TreatyPortOffDutyHoursTip_l10n_key, TreatyPortOffDutyHoursTip);
		TreatyPortInterviewTip = translator(TreatyPortInterviewTip_l10n_key, TreatyPortInterviewTip);
		TreatyPortFactionSettledTip = translator(TreatyPortFactionSettledTip_l10n_key, TreatyPortFactionSettledTip);
		TreatyPortNotContactedTip = translator(TreatyPortNotContactedTip_l10n_key, TreatyPortNotContactedTip);
		SettingPanelSaveSuccessful = translator(SettingPanelSaveSuccessful_l10n_key, SettingPanelSaveSuccessful);
		SettingPanelOtherSaveSuccessful = translator(SettingPanelOtherSaveSuccessful_l10n_key, SettingPanelOtherSaveSuccessful);
		SettingPanelReset = translator(SettingPanelReset_l10n_key, SettingPanelReset);
		SettingPanelResetConfirm = translator(SettingPanelResetConfirm_l10n_key, SettingPanelResetConfirm);
		SettingPanelTitle = translator(SettingPanelTitle_l10n_key, SettingPanelTitle);
		SettingPanelExitConfirm = translator(SettingPanelExitConfirm_l10n_key, SettingPanelExitConfirm);
		SettingPanelConflictHint = translator(SettingPanelConflictHint_l10n_key, SettingPanelConflictHint);
		SettingPanelResetToDefault = translator(SettingPanelResetToDefault_l10n_key, SettingPanelResetToDefault);
		SettingPanelCanNotEdit = translator(SettingPanelCanNotEdit_l10n_key, SettingPanelCanNotEdit);
		SettingPanelCanNotRemove = translator(SettingPanelCanNotRemove_l10n_key, SettingPanelCanNotRemove);
		SettingPanelNoValidInput = translator(SettingPanelNoValidInput_l10n_key, SettingPanelNoValidInput);
		SettingPanelAllowTracedataCollector = translator(SettingPanelAllowTracedataCollector_l10n_key, SettingPanelAllowTracedataCollector);
		SettingPanelTracedataCollectorDesc = translator(SettingPanelTracedataCollectorDesc_l10n_key, SettingPanelTracedataCollectorDesc);
		SettingPanelDelete = translator(SettingPanelDelete_l10n_key, SettingPanelDelete);
		SettingPanelCanNotSaveByConflict = translator(SettingPanelCanNotSaveByConflict_l10n_key, SettingPanelCanNotSaveByConflict);
		SettingPanelNewDevice = translator(SettingPanelNewDevice_l10n_key, SettingPanelNewDevice);
		SettingPanelLoading = translator(SettingPanelLoading_l10n_key, SettingPanelLoading);
		EquipmentBarTimeLabel = translator(EquipmentBarTimeLabel_l10n_key, EquipmentBarTimeLabel);
		EquipmentBarTimeFormat = translator(EquipmentBarTimeFormat_l10n_key, EquipmentBarTimeFormat);
		EquipmentBarDroneNotEquip = translator(EquipmentBarDroneNotEquip_l10n_key, EquipmentBarDroneNotEquip);
		EquipmentBarMotorTitle = translator(EquipmentBarMotorTitle_l10n_key, EquipmentBarMotorTitle);
		EquipmentBarMotorLock = translator(EquipmentBarMotorLock_l10n_key, EquipmentBarMotorLock);
		EquipmentBarHatTip = translator(EquipmentBarHatTip_l10n_key, EquipmentBarHatTip);
		EquipmentBarPositiveTip = translator(EquipmentBarPositiveTip_l10n_key, EquipmentBarPositiveTip);
		EquipmentBarPassive1Tip = translator(EquipmentBarPassive1Tip_l10n_key, EquipmentBarPassive1Tip);
		EquipmentBarPassive2Tip = translator(EquipmentBarPassive2Tip_l10n_key, EquipmentBarPassive2Tip);
		EquipmentBarDroneTip = translator(EquipmentBarDroneTip_l10n_key, EquipmentBarDroneTip);
		EquipmentBarSkillLock = translator(EquipmentBarSkillLock_l10n_key, EquipmentBarSkillLock);
		EquipmentBarDoubleJumpDesc = translator(EquipmentBarDoubleJumpDesc_l10n_key, EquipmentBarDoubleJumpDesc);
		EquipmentBarSprintDesc = translator(EquipmentBarSprintDesc_l10n_key, EquipmentBarSprintDesc);
		AbilityDoubleJumpUnlockTip = translator(AbilityDoubleJumpUnlockTip_l10n_key, AbilityDoubleJumpUnlockTip);
		AbilitySprintUnlockTip = translator(AbilitySprintUnlockTip_l10n_key, AbilitySprintUnlockTip);
		EquipmentBarBackpackFull = translator(EquipmentBarBackpackFull_l10n_key, EquipmentBarBackpackFull);
		EquipmentSkillPrefix = translator(EquipmentSkillPrefix_l10n_key, EquipmentSkillPrefix);
		EquipmentDefensePrefix = translator(EquipmentDefensePrefix_l10n_key, EquipmentDefensePrefix);
		UiTipHomepageStartGame = translator(UiTipHomepageStartGame_l10n_key, UiTipHomepageStartGame);
		UiTipHomepageChangelog = translator(UiTipHomepageChangelog_l10n_key, UiTipHomepageChangelog);
		UiTipHomepageSettings = translator(UiTipHomepageSettings_l10n_key, UiTipHomepageSettings);
		UiTipHomepageMods = translator(UiTipHomepageMods_l10n_key, UiTipHomepageMods);
		UiTipHomepageDeveloperList = translator(UiTipHomepageDeveloperList_l10n_key, UiTipHomepageDeveloperList);
		UiTipHomepageExitGame = translator(UiTipHomepageExitGame_l10n_key, UiTipHomepageExitGame);
		UiTipParkingApronLocked = translator(UiTipParkingApronLocked_l10n_key, UiTipParkingApronLocked);
		UiTipResolving = translator(UiTipResolving_l10n_key, UiTipResolving);
		UiTipShredderMoney = translator(UiTipShredderMoney_l10n_key, UiTipShredderMoney);
		UiTipErrShredderEmpty = translator(UiTipErrShredderEmpty_l10n_key, UiTipErrShredderEmpty);
		UiTipAirWall = translator(UiTipAirWall_l10n_key, UiTipAirWall);
		UiTipNotAvailableToMotor = translator(UiTipNotAvailableToMotor_l10n_key, UiTipNotAvailableToMotor);
		CollectionPanelItemLabel = translator(CollectionPanelItemLabel_l10n_key, CollectionPanelItemLabel);
		CollectionPanelItemRecipeTime = translator(CollectionPanelItemRecipeTime_l10n_key, CollectionPanelItemRecipeTime);
		CollectionPanelItemRecipeEmpty = translator(CollectionPanelItemRecipeEmpty_l10n_key, CollectionPanelItemRecipeEmpty);
		CollectionPanelItemUnknown = translator(CollectionPanelItemUnknown_l10n_key, CollectionPanelItemUnknown);
		CollectionPanelItemSource = translator(CollectionPanelItemSource_l10n_key, CollectionPanelItemSource);
		CollectionPanelNpcAddress = translator(CollectionPanelNpcAddress_l10n_key, CollectionPanelNpcAddress);
		CollectionPanelNpcLikeRecord = translator(CollectionPanelNpcLikeRecord_l10n_key, CollectionPanelNpcLikeRecord);
		CollectionPanelNpcLikeNone = translator(CollectionPanelNpcLikeNone_l10n_key, CollectionPanelNpcLikeNone);
		CollectionPanelNpcLikingLock = translator(CollectionPanelNpcLikingLock_l10n_key, CollectionPanelNpcLikingLock);
		CollectionPanelNpcLikingLvLock = translator(CollectionPanelNpcLikingLvLock_l10n_key, CollectionPanelNpcLikingLvLock);
		CollectionPanelNpcContentTitle = translator(CollectionPanelNpcContentTitle_l10n_key, CollectionPanelNpcContentTitle);
		CollectionPanelNpcContentLockTip = translator(CollectionPanelNpcContentLockTip_l10n_key, CollectionPanelNpcContentLockTip);
		CollectionPanelNpcContentEnd = translator(CollectionPanelNpcContentEnd_l10n_key, CollectionPanelNpcContentEnd);
		CollectionPanelMonsterUnknown = translator(CollectionPanelMonsterUnknown_l10n_key, CollectionPanelMonsterUnknown);
		CollectionPanelMonsterHabitat = translator(CollectionPanelMonsterHabitat_l10n_key, CollectionPanelMonsterHabitat);
		CollectionPanelMonsterDropText = translator(CollectionPanelMonsterDropText_l10n_key, CollectionPanelMonsterDropText);
		CollectionPanelMonsterOrganism = translator(CollectionPanelMonsterOrganism_l10n_key, CollectionPanelMonsterOrganism);
		CollectionPanelMonsterMachinery = translator(CollectionPanelMonsterMachinery_l10n_key, CollectionPanelMonsterMachinery);
		CollectionPanelMonsterBoss = translator(CollectionPanelMonsterBoss_l10n_key, CollectionPanelMonsterBoss);
		CollectionPanelMonsterContentTitle = translator(CollectionPanelMonsterContentTitle_l10n_key, CollectionPanelMonsterContentTitle);
		CollectionPanelMonsterContentLockTip = translator(CollectionPanelMonsterContentLockTip_l10n_key, CollectionPanelMonsterContentLockTip);
		CollectionPanelDocumentLabel = translator(CollectionPanelDocumentLabel_l10n_key, CollectionPanelDocumentLabel);
		CollectionPanelAnimalPossess = translator(CollectionPanelAnimalPossess_l10n_key, CollectionPanelAnimalPossess);
		CollectionPanelAnimalBreed = translator(CollectionPanelAnimalBreed_l10n_key, CollectionPanelAnimalBreed);
		CollectionPanelAnimalProductText = translator(CollectionPanelAnimalProductText_l10n_key, CollectionPanelAnimalProductText);
		CollectionPanelAnimalContentLockBringUp = translator(CollectionPanelAnimalContentLockBringUp_l10n_key, CollectionPanelAnimalContentLockBringUp);
		CollectionPanelAnimalContentLockBreed = translator(CollectionPanelAnimalContentLockBreed_l10n_key, CollectionPanelAnimalContentLockBreed);
		CollectionPanelAnimalContentLockProduct = translator(CollectionPanelAnimalContentLockProduct_l10n_key, CollectionPanelAnimalContentLockProduct);
		CollectionPanelFishCatch = translator(CollectionPanelFishCatch_l10n_key, CollectionPanelFishCatch);
		CollectionPanelFishBaitText = translator(CollectionPanelFishBaitText_l10n_key, CollectionPanelFishBaitText);
		CollectionPanelFishPlace = translator(CollectionPanelFishPlace_l10n_key, CollectionPanelFishPlace);
		CollectionPanelFishMonth = translator(CollectionPanelFishMonth_l10n_key, CollectionPanelFishMonth);
		CollectionPanelFishWeather = translator(CollectionPanelFishWeather_l10n_key, CollectionPanelFishWeather);
		CollectionPanelFishWeatherNone = translator(CollectionPanelFishWeatherNone_l10n_key, CollectionPanelFishWeatherNone);
		CollectionPanelFishContentLockTip = translator(CollectionPanelFishContentLockTip_l10n_key, CollectionPanelFishContentLockTip);
		CollectionPanelResourceCollect = translator(CollectionPanelResourceCollect_l10n_key, CollectionPanelResourceCollect);
		CollectionPanelResourceGrowthPeriod = translator(CollectionPanelResourceGrowthPeriod_l10n_key, CollectionPanelResourceGrowthPeriod);
		CollectionPanelResourceYearRoundGrowth = translator(CollectionPanelResourceYearRoundGrowth_l10n_key, CollectionPanelResourceYearRoundGrowth);
		CollectionPanelResourceDropTitle = translator(CollectionPanelResourceDropTitle_l10n_key, CollectionPanelResourceDropTitle);
		CollectionPanelResourceContentLockTip = translator(CollectionPanelResourceContentLockTip_l10n_key, CollectionPanelResourceContentLockTip);
		BuilderPanelLabelBuilding = translator(BuilderPanelLabelBuilding_l10n_key, BuilderPanelLabelBuilding);
		BuilderPanelLabelEquipment = translator(BuilderPanelLabelEquipment_l10n_key, BuilderPanelLabelEquipment);
		BuilderPanelLabelPlatform = translator(BuilderPanelLabelPlatform_l10n_key, BuilderPanelLabelPlatform);
		BuilderPanelSwitchTerrainLayer = translator(BuilderPanelSwitchTerrainLayer_l10n_key, BuilderPanelSwitchTerrainLayer);
		BuilderActionSelectedContent = translator(BuilderActionSelectedContent_l10n_key, BuilderActionSelectedContent);
		BuilderActionUndo = translator(BuilderActionUndo_l10n_key, BuilderActionUndo);
		BuilderActionTurn = translator(BuilderActionTurn_l10n_key, BuilderActionTurn);
		BuilderActionDismantle = translator(BuilderActionDismantle_l10n_key, BuilderActionDismantle);
		BuilderActionBuildingDismantle = translator(BuilderActionBuildingDismantle_l10n_key, BuilderActionBuildingDismantle);
		BuilderActionBuildingStorage = translator(BuilderActionBuildingStorage_l10n_key, BuilderActionBuildingStorage);
		BuilderActionMoveCamera = translator(BuilderActionMoveCamera_l10n_key, BuilderActionMoveCamera);
		BuilderActionMove = translator(BuilderActionMove_l10n_key, BuilderActionMove);
		BuilderActionSelectedItem = translator(BuilderActionSelectedItem_l10n_key, BuilderActionSelectedItem);
		BuilderActionPreciseMovement = translator(BuilderActionPreciseMovement_l10n_key, BuilderActionPreciseMovement);
		BuilderActionSwitchPrecise = translator(BuilderActionSwitchPrecise_l10n_key, BuilderActionSwitchPrecise);
		BuilderActionSwitchBackpack = translator(BuilderActionSwitchBackpack_l10n_key, BuilderActionSwitchBackpack);
		BuilderActionRollingBackpack = translator(BuilderActionRollingBackpack_l10n_key, BuilderActionRollingBackpack);
		BuilderActionRollingItem = translator(BuilderActionRollingItem_l10n_key, BuilderActionRollingItem);
		BuilderActionToggleBackpack = translator(BuilderActionToggleBackpack_l10n_key, BuilderActionToggleBackpack);
		BuilderActionToggleBackpackGamepad = translator(BuilderActionToggleBackpackGamepad_l10n_key, BuilderActionToggleBackpackGamepad);
		BuilderPanelExit = translator(BuilderPanelExit_l10n_key, BuilderPanelExit);
		BuilderPanelUndoGamepad = translator(BuilderPanelUndoGamepad_l10n_key, BuilderPanelUndoGamepad);
		BuilderPanelExpandBuildingList = translator(BuilderPanelExpandBuildingList_l10n_key, BuilderPanelExpandBuildingList);
		BuilderPanellFoldBuildingList = translator(BuilderPanellFoldBuildingList_l10n_key, BuilderPanellFoldBuildingList);
		BuilderPanelFinish = translator(BuilderPanelFinish_l10n_key, BuilderPanelFinish);
		BuilderDismantleBuildingErr = translator(BuilderDismantleBuildingErr_l10n_key, BuilderDismantleBuildingErr);
		BuilderBuilderConstruct = translator(BuilderBuilderConstruct_l10n_key, BuilderBuilderConstruct);
		BuilderPanelTempBuildingFull = translator(BuilderPanelTempBuildingFull_l10n_key, BuilderPanelTempBuildingFull);
		BuilderExitErrAnimal = translator(BuilderExitErrAnimal_l10n_key, BuilderExitErrAnimal);
		BuilderExitErrOccupied = translator(BuilderExitErrOccupied_l10n_key, BuilderExitErrOccupied);
		BuilderExitErrSoleBuilding = translator(BuilderExitErrSoleBuilding_l10n_key, BuilderExitErrSoleBuilding);
		BuilderExitConfirm = translator(BuilderExitConfirm_l10n_key, BuilderExitConfirm);
		UiEnvOptimizerPanelTitle = translator(UiEnvOptimizerPanelTitle_l10n_key, UiEnvOptimizerPanelTitle);
		UiEnvOptimizerConsoleTitle = translator(UiEnvOptimizerConsoleTitle_l10n_key, UiEnvOptimizerConsoleTitle);
		UiEnvOptimizerOverview = translator(UiEnvOptimizerOverview_l10n_key, UiEnvOptimizerOverview);
		UiEnvOptimizerButtonNoEnergy = translator(UiEnvOptimizerButtonNoEnergy_l10n_key, UiEnvOptimizerButtonNoEnergy);
		UiEnvOptimizerTipNoEnergy = translator(UiEnvOptimizerTipNoEnergy_l10n_key, UiEnvOptimizerTipNoEnergy);
		UiEnvOptimizerButtonNoComponent = translator(UiEnvOptimizerButtonNoComponent_l10n_key, UiEnvOptimizerButtonNoComponent);
		UiEnvOptimizerTipNoComponent = translator(UiEnvOptimizerTipNoComponent_l10n_key, UiEnvOptimizerTipNoComponent);
		UiEnvOptimizerButtonValid = translator(UiEnvOptimizerButtonValid_l10n_key, UiEnvOptimizerButtonValid);
		UiEnvOptimizerButtonNotValid = translator(UiEnvOptimizerButtonNotValid_l10n_key, UiEnvOptimizerButtonNotValid);
		UiEnvOptimizerCheckSuccess = translator(UiEnvOptimizerCheckSuccess_l10n_key, UiEnvOptimizerCheckSuccess);
		UiEnvOptimizerDateInfo = translator(UiEnvOptimizerDateInfo_l10n_key, UiEnvOptimizerDateInfo);
		UiEnvOptimizerSlotTitle = translator(UiEnvOptimizerSlotTitle_l10n_key, UiEnvOptimizerSlotTitle);
		UiEnvOptimizerComponentAlreadyActive = translator(UiEnvOptimizerComponentAlreadyActive_l10n_key, UiEnvOptimizerComponentAlreadyActive);
		UiEnvOptimizerInRecognition = translator(UiEnvOptimizerInRecognition_l10n_key, UiEnvOptimizerInRecognition);
		UiEnvOptimizerLoadingData = translator(UiEnvOptimizerLoadingData_l10n_key, UiEnvOptimizerLoadingData);
		UiEnvOptimizerActiveSuccess = translator(UiEnvOptimizerActiveSuccess_l10n_key, UiEnvOptimizerActiveSuccess);
		AnimalInvalidBuilding = translator(AnimalInvalidBuilding_l10n_key, AnimalInvalidBuilding);
		AnimalInvalidRoom = translator(AnimalInvalidRoom_l10n_key, AnimalInvalidRoom);
		AnimalFullBuilding = translator(AnimalFullBuilding_l10n_key, AnimalFullBuilding);
		AnimalInputAnimalName = translator(AnimalInputAnimalName_l10n_key, AnimalInputAnimalName);
		AnimalPackageIsFull = translator(AnimalPackageIsFull_l10n_key, AnimalPackageIsFull);
		AnimalHasEscaped = translator(AnimalHasEscaped_l10n_key, AnimalHasEscaped);
		AnimalNoAnimal = translator(AnimalNoAnimal_l10n_key, AnimalNoAnimal);
		AnimalInUse = translator(AnimalInUse_l10n_key, AnimalInUse);
		CalendarPanelYearTitle = translator(CalendarPanelYearTitle_l10n_key, CalendarPanelYearTitle);
		CalendarPanelDateTitle = translator(CalendarPanelDateTitle_l10n_key, CalendarPanelDateTitle);
		CalendarPanelEventTitle = translator(CalendarPanelEventTitle_l10n_key, CalendarPanelEventTitle);
		CalendarPanelPlayerBirthday = translator(CalendarPanelPlayerBirthday_l10n_key, CalendarPanelPlayerBirthday);
		CalendarPanelMemoTitle = translator(CalendarPanelMemoTitle_l10n_key, CalendarPanelMemoTitle);
		CalendarPanelEmptyHint = translator(CalendarPanelEmptyHint_l10n_key, CalendarPanelEmptyHint);
		CalendarPanelDelMemoHint = translator(CalendarPanelDelMemoHint_l10n_key, CalendarPanelDelMemoHint);
		CalendarPanelInputEmptyHint = translator(CalendarPanelInputEmptyHint_l10n_key, CalendarPanelInputEmptyHint);
		InputTextContainsSensitiveWorld = translator(InputTextContainsSensitiveWorld_l10n_key, InputTextContainsSensitiveWorld);
		CalendarMemoMessage = translator(CalendarMemoMessage_l10n_key, CalendarMemoMessage);
		UiCropInfoGrowthLevel = translator(UiCropInfoGrowthLevel_l10n_key, UiCropInfoGrowthLevel);
		UiCropInfoHarvestCount = translator(UiCropInfoHarvestCount_l10n_key, UiCropInfoHarvestCount);
		UiAnimalInfoState = translator(UiAnimalInfoState_l10n_key, UiAnimalInfoState);
		UiAnimalSpace = translator(UiAnimalSpace_l10n_key, UiAnimalSpace);
		UiAnimalAgeYear = translator(UiAnimalAgeYear_l10n_key, UiAnimalAgeYear);
		UiAnimalAgeMonth = translator(UiAnimalAgeMonth_l10n_key, UiAnimalAgeMonth);
		UiAnimalAgeDay = translator(UiAnimalAgeDay_l10n_key, UiAnimalAgeDay);
		UiAnimalBirthday = translator(UiAnimalBirthday_l10n_key, UiAnimalBirthday);
		UiAnimalCapacity = translator(UiAnimalCapacity_l10n_key, UiAnimalCapacity);
		UiAnimalCallBack = translator(UiAnimalCallBack_l10n_key, UiAnimalCallBack);
		UiAnimalLetOut = translator(UiAnimalLetOut_l10n_key, UiAnimalLetOut);
		UiAnimalAdult = translator(UiAnimalAdult_l10n_key, UiAnimalAdult);
		UiAnimalChild = translator(UiAnimalChild_l10n_key, UiAnimalChild);
		UiAnimalPosition = translator(UiAnimalPosition_l10n_key, UiAnimalPosition);
		UiAnimalNoAnimal = translator(UiAnimalNoAnimal_l10n_key, UiAnimalNoAnimal);
		UiAnimalNotVisible = translator(UiAnimalNotVisible_l10n_key, UiAnimalNotVisible);
		DronePanelTitle = translator(DronePanelTitle_l10n_key, DronePanelTitle);
		DronePanelErrLoad = translator(DronePanelErrLoad_l10n_key, DronePanelErrLoad);
		DronePanelErrLocked = translator(DronePanelErrLocked_l10n_key, DronePanelErrLocked);
		DroneComponentTitle = translator(DroneComponentTitle_l10n_key, DroneComponentTitle);
		GarbageSubmitErrItem = translator(GarbageSubmitErrItem_l10n_key, GarbageSubmitErrItem);
		DisposeFailRoom = translator(DisposeFailRoom_l10n_key, DisposeFailRoom);
		UiTipOpenMapFail = translator(UiTipOpenMapFail_l10n_key, UiTipOpenMapFail);
		UiTipNone = translator(UiTipNone_l10n_key, UiTipNone);
		UiTipBuyBackpack = translator(UiTipBuyBackpack_l10n_key, UiTipBuyBackpack);
		UiTipBuyBackpackInfo = translator(UiTipBuyBackpackInfo_l10n_key, UiTipBuyBackpackInfo);
		UiTipEmptyList = translator(UiTipEmptyList_l10n_key, UiTipEmptyList);
		UiTipCurrentPosition = translator(UiTipCurrentPosition_l10n_key, UiTipCurrentPosition);
		UiTipCurrentMotorPosition = translator(UiTipCurrentMotorPosition_l10n_key, UiTipCurrentMotorPosition);
		UiOptionShootingRangeStart = translator(UiOptionShootingRangeStart_l10n_key, UiOptionShootingRangeStart);
		UiOptionShootingRangeEnd = translator(UiOptionShootingRangeEnd_l10n_key, UiOptionShootingRangeEnd);
		UiTipEquipmentUnlock = translator(UiTipEquipmentUnlock_l10n_key, UiTipEquipmentUnlock);
		UiOptionShootingRangeExit = translator(UiOptionShootingRangeExit_l10n_key, UiOptionShootingRangeExit);
		UiOptionConfirmShootingRangeExit = translator(UiOptionConfirmShootingRangeExit_l10n_key, UiOptionConfirmShootingRangeExit);
		UiNpcVisit = translator(UiNpcVisit_l10n_key, UiNpcVisit);
		UiNpcFileUpdation = translator(UiNpcFileUpdation_l10n_key, UiNpcFileUpdation);
		UiNpcLikingRise = translator(UiNpcLikingRise_l10n_key, UiNpcLikingRise);
		UiNpcLikingDecline = translator(UiNpcLikingDecline_l10n_key, UiNpcLikingDecline);
		ItemEdenFruitTip = translator(ItemEdenFruitTip_l10n_key, ItemEdenFruitTip);
		UiTipGameVersion = translator(UiTipGameVersion_l10n_key, UiTipGameVersion);
		UiTipContentLock = translator(UiTipContentLock_l10n_key, UiTipContentLock);
		UiTipPhotoSaving = translator(UiTipPhotoSaving_l10n_key, UiTipPhotoSaving);
		UiItemGenerateElectricityEntry = translator(UiItemGenerateElectricityEntry_l10n_key, UiItemGenerateElectricityEntry);
		UiTipRename = translator(UiTipRename_l10n_key, UiTipRename);
		UiModNoMod = translator(UiModNoMod_l10n_key, UiModNoMod);
		UiModNeedSubscribe = translator(UiModNeedSubscribe_l10n_key, UiModNeedSubscribe);
		UiModOpenWorkshop = translator(UiModOpenWorkshop_l10n_key, UiModOpenWorkshop);
		UiModNeedCreate = translator(UiModNeedCreate_l10n_key, UiModNeedCreate);
		UiModOpenLocalDirectory = translator(UiModOpenLocalDirectory_l10n_key, UiModOpenLocalDirectory);
		UiModUploadMod = translator(UiModUploadMod_l10n_key, UiModUploadMod);
		UiModUpdateMod = translator(UiModUpdateMod_l10n_key, UiModUpdateMod);
		UiModEnable = translator(UiModEnable_l10n_key, UiModEnable);
		UiModDisable = translator(UiModDisable_l10n_key, UiModDisable);
		UiModConfirmUpload = translator(UiModConfirmUpload_l10n_key, UiModConfirmUpload);
		UiModConfirmUpdate = translator(UiModConfirmUpdate_l10n_key, UiModConfirmUpdate);
		UiModUploading = translator(UiModUploading_l10n_key, UiModUploading);
		UiModUploadSuccess = translator(UiModUploadSuccess_l10n_key, UiModUploadSuccess);
		UiModUploadFailed = translator(UiModUploadFailed_l10n_key, UiModUploadFailed);
		UiModUpdating = translator(UiModUpdating_l10n_key, UiModUpdating);
		UiModUpdateSuccess = translator(UiModUpdateSuccess_l10n_key, UiModUpdateSuccess);
		UiModUpdateFailed = translator(UiModUpdateFailed_l10n_key, UiModUpdateFailed);
		UiModReloading = translator(UiModReloading_l10n_key, UiModReloading);
		UiModSourceLocal = translator(UiModSourceLocal_l10n_key, UiModSourceLocal);
		UiModSourceWorkshop = translator(UiModSourceWorkshop_l10n_key, UiModSourceWorkshop);
	}

	public override string ToString()
	{
		return "{ SysErrDatalost:" + SysErrDatalost + ",UiMoneyTip:" + UiMoneyTip + ",UiItemSimpleTip:" + UiItemSimpleTip + ",UiItemTip:" + UiItemTip + ",UiTipConsumable:" + UiTipConsumable + ",UiErrMaterialNotEnough:" + UiErrMaterialNotEnough + ",UiErrMoneyNotEnough:" + UiErrMoneyNotEnough + ",UiErrBackpackIsFull:" + UiErrBackpackIsFull + ",UiErrNotDisposeItem:" + UiErrNotDisposeItem + ",UiQuesDisposeItem:" + UiQuesDisposeItem + ",UiMenuItemnotavaiable:" + UiMenuItemnotavaiable + ",UiBackpackCellExchange:" + UiBackpackCellExchange + ",UiErrBackpackCellExchange:" + UiErrBackpackCellExchange + ",UiSortDefault:" + UiSortDefault + ",UiSortUnlock:" + UiSortUnlock + ",UiLoseItemTip:" + UiLoseItemTip + ",UiLoseMoneyTip:" + UiLoseMoneyTip + ",UiUnlockSeed:" + UiUnlockSeed + ",UiAnalyzerTime:" + UiAnalyzerTime + ",UiAnalyzerOutput:" + UiAnalyzerOutput + ",UiAnalyzerTotalTime:" + UiAnalyzerTotalTime + ",UiShredderConfirm:" + UiShredderConfirm + ",UiShredderConfirmUpgrade:" + UiShredderConfirmUpgrade + ",UiSubmitConfirm:" + UiSubmitConfirm + ",UiSubmitConfirmGene:" + UiSubmitConfirmGene + ",UiSubmitConfirmMoney:" + UiSubmitConfirmMoney + ",UiSubmitNotEnough:" + UiSubmitNotEnough + ",UiGiftItemConfirm:" + UiGiftItemConfirm + ",UiTipDismentle:" + UiTipDismentle + ",UiTipPlantErr:" + UiTipPlantErr + ",UiTextYears:" + UiTextYears + ",UiTextMonths:" + UiTextMonths + ",UiTextDays:" + UiTextDays + ",UiTextHours:" + UiTextHours + ",UiTextMinutes:" + UiTextMinutes + ",UiTextTimeFormat:" + UiTextTimeFormat + ",UiTextDemoStatement:" + UiTextDemoStatement + ",UiTextGeneDescription:" + UiTextGeneDescription + ",InputTitlePlayerName:" + InputTitlePlayerName + ",InputTitleContainerName:" + InputTitleContainerName + ",InputNameEmpty:" + InputNameEmpty + ",InputNameConfirm:" + InputNameConfirm + ",InputNameContainsSensitiveWorld:" + InputNameContainsSensitiveWorld + ",InputTitlePlayerBirthday:" + InputTitlePlayerBirthday + ",InputBirthdayConfirm:" + InputBirthdayConfirm + ",UiSavepointDefault:" + UiSavepointDefault + ",UiSavepointSleep:" + UiSavepointSleep + ",UiSavepointNap:" + UiSavepointNap + ",UiSavepointKillTime:" + UiSavepointKillTime + ",UiSavepointSit:" + UiSavepointSit + ",UiSavepointExit:" + UiSavepointExit + ",UiSavepointCancel:" + UiSavepointCancel + ",UiTipConfirm:" + UiTipConfirm + ",UiTipCancel:" + UiTipCancel + ",UiTipQuit:" + UiTipQuit + ",UiTipSwitchClassifying:" + UiTipSwitchClassifying + ",UiTipSort:" + UiTipSort + ",UiTipUpgrade:" + UiTipUpgrade + ",UiTipBuy:" + UiTipBuy + ",UiTipSellConfirm:" + UiTipSellConfirm + ",UiTipSell:" + UiTipSell + ",UiTipBuyConfirm:" + UiTipBuyConfirm + ",UiTipUnlock:" + UiTipUnlock + ",UiTipUnlockSeed:" + UiTipUnlockSeed + ",UiTipLaunchDrone:" + UiTipLaunchDrone + ",UiTipSwitchAutomateBot:" + UiTipSwitchAutomateBot + ",UiTipAutomateBotSwitch:" + UiTipAutomateBotSwitch + ",UiTipAutomateBotUnload:" + UiTipAutomateBotUnload + ",UiTipAutomateBotLoad:" + UiTipAutomateBotLoad + ",UiTipChoice:" + UiTipChoice + ",UiTipSubmitAll:" + UiTipSubmitAll + ",UiTipSubmit:" + UiTipSubmit + ",UiTipLoadGame:" + UiTipLoadGame + ",UiTipDel:" + UiTipDel + ",UiTipLockSlot:" + UiTipLockSlot + ",UiTipTidy:" + UiTipTidy + ",UiTipDispose:" + UiTipDispose + ",UiTipDestroyItem:" + UiTipDestroyItem + ",UiTipSwitchLable:" + UiTipSwitchLable + ",UiTipPutAll:" + UiTipPutAll + ",UiTipPutMax:" + UiTipPutMax + ",UiTipTakeOutAll:" + UiTipTakeOutAll + ",UiTipTakeOutMax:" + UiTipTakeOutMax + ",UiTipPutAllTap:" + UiTipPutAllTap + ",UiTipTakeOutOne:" + UiTipTakeOutOne + ",UiTipTakeOutHalf:" + UiTipTakeOutHalf + ",UiTipTakeOutOneGamepad:" + UiTipTakeOutOneGamepad + ",UiTipPut:" + UiTipPut + ",UiTipTakeOut:" + UiTipTakeOut + ",UiTipPutGamepad:" + UiTipPutGamepad + ",UiTipTakeOutGamepad:" + UiTipTakeOutGamepad + ",UiTipPickUpGamepad:" + UiTipPickUpGamepad + ",UiTipQuickPutOne:" + UiTipQuickPutOne + ",UiTipQuickPutAll:" + UiTipQuickPutAll + ",UiTipQuickTakeOne:" + UiTipQuickTakeOne + ",UiTipQuickTakeAll:" + UiTipQuickTakeAll + ",UiTipQuickEquipment:" + UiTipQuickEquipment + ",UiTipSwitchBox:" + UiTipSwitchBox + ",UiTipSubOne:" + UiTipSubOne + ",UiTipAddOne:" + UiTipAddOne + ",UiTipSubTen:" + UiTipSubTen + ",UiTipAddTen:" + UiTipAddTen + ",UiTipSetMin:" + UiTipSetMin + ",UiTipSetMax:" + UiTipSetMax + ",UiTipBroadcast:" + UiTipBroadcast + ",UiTipMakeOne:" + UiTipMakeOne + ",UiTipMakeAll:" + UiTipMakeAll + ",UiTipBuyOne:" + UiTipBuyOne + ",UiTipBuyAllGamepad:" + UiTipBuyAllGamepad + ",UiTipSellOne:" + UiTipSellOne + ",UiTipSellAll:" + UiTipSellAll + ",UiTipPrevFilter:" + UiTipPrevFilter + ",UiTipNextFilter:" + UiTipNextFilter + ",UiTipCapture:" + UiTipCapture + ",UiTipCollection:" + UiTipCollection + ",UiTipHistory:" + UiTipHistory + ",UiTipChangeName:" + UiTipChangeName + ",UiTipTakeOutSelected:" + UiTipTakeOutSelected + ",UiTipPutOneSelected:" + UiTipPutOneSelected + ",UiTipPutAllSelected:" + UiTipPutAllSelected + ",UiTipAddMemo:" + UiTipAddMemo + ",UiTipChangeMemo:" + UiTipChangeMemo + ",UiTipDelMemo:" + UiTipDelMemo + ",UiTipSwitchMapSize:" + UiTipSwitchMapSize + ",UiTipMapCentered:" + UiTipMapCentered + ",UiTipEmailRecycle:" + UiTipEmailRecycle + ",UiTipEmailDefault:" + UiTipEmailDefault + ",UiTipTechtreePointFocus:" + UiTipTechtreePointFocus + ",UiTipTechtreeFocus:" + UiTipTechtreeFocus + ",UiTipBatteryLow:" + UiTipBatteryLow + ",UiTipErrTakeBoat:" + UiTipErrTakeBoat + ",UiOperationTalk:" + UiOperationTalk + ",UiOperationTalkUnknown:" + UiOperationTalkUnknown + ",UiOperationInteract:" + UiOperationInteract + ",UiOperationSleep:" + UiOperationSleep + ",UiOperationPick:" + UiOperationPick + ",UiOperationWell:" + UiOperationWell + ",UiOperationHarvest:" + UiOperationHarvest + ",UiOperationClear:" + UiOperationClear + ",UiOperationOpen:" + UiOperationOpen + ",UiOperationOpenDoor:" + UiOperationOpenDoor + ",UiOperationCloseDoor:" + UiOperationCloseDoor + ",UiOperationEnter:" + UiOperationEnter + ",UiOperationEnterFormat:" + UiOperationEnterFormat + ",UiOperationExit:" + UiOperationExit + ",UiOperationView:" + UiOperationView + ",UiOperationUse:" + UiOperationUse + ",UiOperationSit:" + UiOperationSit + ",UiOperationDisplay:" + UiOperationDisplay + ",UiOperationTakeoff:" + UiOperationTakeoff + ",UiOperationDump:" + UiOperationDump + ",UiOperationDisembark:" + UiOperationDisembark + ",UiOperationCallBoat:" + UiOperationCallBoat + ",UiOperationShower:" + UiOperationShower + ",UiOperationOpenBox:" + UiOperationOpenBox + ",UiOperationStorageShelf:" + UiOperationStorageShelf + ",UiOperationFuelIn:" + UiOperationFuelIn + ",UiOperationClose:" + UiOperationClose + ",UiOperationStart:" + UiOperationStart + ",UiOperationFill:" + UiOperationFill + ",UiOperationFondle:" + UiOperationFondle + ",UiOperationStartSomething:" + UiOperationStartSomething + ",UiOperationUseSomething:" + UiOperationUseSomething + ",UiOperationErrNotSeed:" + UiOperationErrNotSeed + ",UiOperationErrInvalidPlantbasin:" + UiOperationErrInvalidPlantbasin + ",UiOperationErrInvalidSeason:" + UiOperationErrInvalidSeason + ",UiOperationErrLackOfAsset:" + UiOperationErrLackOfAsset + ",UiOperationErrLackOfEnergy:" + UiOperationErrLackOfEnergy + ",UiOperationErrCannotPlantTree:" + UiOperationErrCannotPlantTree + ",UiOperationErrCannotPlantOnGround:" + UiOperationErrCannotPlantOnGround + ",UiOperationErrEmptyDrone:" + UiOperationErrEmptyDrone + ",UiOperationErrWellFull:" + UiOperationErrWellFull + ",UiOperationErrWaterFull:" + UiOperationErrWaterFull + ",UiOperationErrRunoutWater:" + UiOperationErrRunoutWater + ",UiOperationErrRunoutWaterAround:" + UiOperationErrRunoutWaterAround + ",UiOperationErrEquipmentCorroded:" + UiOperationErrEquipmentCorroded + ",UiOperationErrLowToolLevel:" + UiOperationErrLowToolLevel + ",UiOperationErrFullPlasticFilm:" + UiOperationErrFullPlasticFilm + ",UiOperationErrCannotFertilizer:" + UiOperationErrCannotFertilizer + ",UiOperationErrDroneFullBattery:" + UiOperationErrDroneFullBattery + ",UiOperationErrWaterEvaporated:" + UiOperationErrWaterEvaporated + ",UiOperationErrFailToPlaceBox:" + UiOperationErrFailToPlaceBox + ",UiOperationErrCannotDisplay:" + UiOperationErrCannotDisplay + ",UiOperationErrCannotUseIfRiding:" + UiOperationErrCannotUseIfRiding + ",UiOperationErrCannotCallMotor:" + UiOperationErrCannotCallMotor + ",UiOperationErrSwordCannotAutofire:" + UiOperationErrSwordCannotAutofire + ",UiOperationErrLowPower:" + UiOperationErrLowPower + ",UiOperationErrCooling:" + UiOperationErrCooling + ",UiOperationErrBarrelFull:" + UiOperationErrBarrelFull + ",UiOperationErrFuelFull:" + UiOperationErrFuelFull + ",UiOperationErrNotFeeds:" + UiOperationErrNotFeeds + ",UiOperationErrFeederFull:" + UiOperationErrFeederFull + ",UiOperationErrSlotFull:" + UiOperationErrSlotFull + ",UiOperationErrNoSuitableEquipments:" + UiOperationErrNoSuitableEquipments + ",UiStorageShelfFull:" + UiStorageShelfFull + ",UiOperationWeatherHasChanged:" + UiOperationWeatherHasChanged + ",UiOperationErrFuel:" + UiOperationErrFuel + ",UiOperationGiftItemFail:" + UiOperationGiftItemFail + ",UiOperationMakeEquipment:" + UiOperationMakeEquipment + ",UiOperationSell:" + UiOperationSell + ",UiOperationSwitchAutoFire:" + UiOperationSwitchAutoFire + ",UiOperationSwitchManualFire:" + UiOperationSwitchManualFire + ",UiOperationCollect:" + UiOperationCollect + ",UiOperationRide:" + UiOperationRide + ",UiOperationNoAnimalBuilding:" + UiOperationNoAnimalBuilding + ",UiOperationErrNoPlantBasin:" + UiOperationErrNoPlantBasin + ",UiOperationErrNoBuildingHere:" + UiOperationErrNoBuildingHere + ",UiOperationErrInvalidWallpaper:" + UiOperationErrInvalidWallpaper + ",UiOperationErrSameWallpaper:" + UiOperationErrSameWallpaper + ",UiOperationErrShouldInhouse:" + UiOperationErrShouldInhouse + ",UiOperationErrShouldOutside:" + UiOperationErrShouldOutside + ",StorePlayerMoneyNotEnough:" + StorePlayerMoneyNotEnough + ",StoreStoreMoneyNotEnough:" + StoreStoreMoneyNotEnough + ",StoreItemNotSaleable:" + StoreItemNotSaleable + ",StoreItemSoldOut:" + StoreItemSoldOut + ",StoreLackOfAsset:" + StoreLackOfAsset + ",StoreUpgradePrice:" + StoreUpgradePrice + ",StoreUpgradeConfirm:" + StoreUpgradeConfirm + ",StoreSoldOutIcon:" + StoreSoldOutIcon + ",StoreUpgradeEmptyInfo:" + StoreUpgradeEmptyInfo + ",StoreItemNotSaleableComment:" + StoreItemNotSaleableComment + ",StoreQuantitySubmitSelling:" + StoreQuantitySubmitSelling + ",StoreQuantitySubmitCurrentMoneySelling:" + StoreQuantitySubmitCurrentMoneySelling + ",StoreQuantitySubmitTotalMoneySelling:" + StoreQuantitySubmitTotalMoneySelling + ",StoreQuantitySubmitBuying:" + StoreQuantitySubmitBuying + ",StoreQuantitySubmitCurrentMoneyBuying:" + StoreQuantitySubmitCurrentMoneyBuying + ",StoreQuantitySubmitTotalMoneyBuying:" + StoreQuantitySubmitTotalMoneyBuying + ",StoreQuantitySubmitCountInBack:" + StoreQuantitySubmitCountInBack + ",StoreQuantitySubmitUnitPrice:" + StoreQuantitySubmitUnitPrice + ",StoreItemTipPlantbasinLocked:" + StoreItemTipPlantbasinLocked + ",FarmbuilderErrBuildSystemNotSupport:" + FarmbuilderErrBuildSystemNotSupport + ",FarmbuilderErrLackOfAsset:" + FarmbuilderErrLackOfAsset + ",FarmbuilderErrIndoorEquipment:" + FarmbuilderErrIndoorEquipment + ",FarmbuilderErrOutdoorEquipment:" + FarmbuilderErrOutdoorEquipment + ",FarmbuilderErrInvalidPosition:" + FarmbuilderErrInvalidPosition + ",FarmbuilderErrOutOfRange:" + FarmbuilderErrOutOfRange + ",FarmbuilderErrEquipmentAreaNotEmpty:" + FarmbuilderErrEquipmentAreaNotEmpty + ",FarmbuilderErrPlatformOccupied:" + FarmbuilderErrPlatformOccupied + ",FarmbuilderQuestionRemove:" + FarmbuilderQuestionRemove + ",FarmbuilderQuestionRemoveReturn:" + FarmbuilderQuestionRemoveReturn + ",FarmbuilderErrEquipmentOccupied:" + FarmbuilderErrEquipmentOccupied + ",FarmbuilderErrPlantbasinTreeOccupied:" + FarmbuilderErrPlantbasinTreeOccupied + ",FarmbuilderErrParkingApronOccupied:" + FarmbuilderErrParkingApronOccupied + ",FarmbuilderErrAutomateBotOccupied:" + FarmbuilderErrAutomateBotOccupied + ",FarmbuilderErrShowCaseOccupied:" + FarmbuilderErrShowCaseOccupied + ",FarmbuilderErrPlatformRemove:" + FarmbuilderErrPlatformRemove + ",FarmbuilderErrEquipmentHideDoor:" + FarmbuilderErrEquipmentHideDoor + ",FarmbuilderErrBuildingOccupied:" + FarmbuilderErrBuildingOccupied + ",FarmbuilderErrBuildingOccupiedAnimal:" + FarmbuilderErrBuildingOccupiedAnimal + ",FarmbuilderErrBuildingAreaNotEmpty:" + FarmbuilderErrBuildingAreaNotEmpty + ",FarmbuilderErrBuildingInvalidHeight:" + FarmbuilderErrBuildingInvalidHeight + ",FarmbuilderErrBuildingInvalidCoveredRatio:" + FarmbuilderErrBuildingInvalidCoveredRatio + ",FarmbuilderErrBuildingCanNotRemove:" + FarmbuilderErrBuildingCanNotRemove + ",FarmbuilderErrBuildingInvalidOtherNoSupport:" + FarmbuilderErrBuildingInvalidOtherNoSupport + ",FarmbuilderErrBuildingOccupiedBuilding:" + FarmbuilderErrBuildingOccupiedBuilding + ",FarmbuilderErrBuildingInvalidDoorBeHidden:" + FarmbuilderErrBuildingInvalidDoorBeHidden + ",FarmbuilderErrBuildingOccupiedCeiling:" + FarmbuilderErrBuildingOccupiedCeiling + ",FarmbuilderErrBuildingOccupiedDoor:" + FarmbuilderErrBuildingOccupiedDoor + ",FarmbuilderErrBuildingOccupiedCeilingPt:" + FarmbuilderErrBuildingOccupiedCeilingPt + ",FarmbuilderErrBuildingGroundInvalid:" + FarmbuilderErrBuildingGroundInvalid + ",FarmbuilderErrPlatformWidth:" + FarmbuilderErrPlatformWidth + ",FarmbuilderErrPlatformHeightMax:" + FarmbuilderErrPlatformHeightMax + ",FarmbuilderErrEquipmentLimited:" + FarmbuilderErrEquipmentLimited + ",TechtreePanelTitle:" + TechtreePanelTitle + ",TechtreeNodeLackOfPoints:" + TechtreeNodeLackOfPoints + ",TechtreeNodeNotAvaiable:" + TechtreeNodeNotAvaiable + ",TechtreeNodeUnlocked:" + TechtreeNodeUnlocked + ",TechtreeNodeUnopen:" + TechtreeNodeUnopen + ",TechtreeNodeUnlockConfirm:" + TechtreeNodeUnlockConfirm + ",TechtreeNodeBuildingHealth:" + TechtreeNodeBuildingHealth + ",TechtreeNodeEquipmentElectronic:" + TechtreeNodeEquipmentElectronic + ",TechtreeProgressInfo:" + TechtreeProgressInfo + ",TechtreeMaxLevel:" + TechtreeMaxLevel + ",TechtreeNodeHint:" + TechtreeNodeHint + ",MissionUpdate:" + MissionUpdate + ",MissionComplete:" + MissionComplete + ",MissionCompleteSendEmail:" + MissionCompleteSendEmail + ",MissionPanelTitle:" + MissionPanelTitle + ",MissionPanelEmpty:" + MissionPanelEmpty + ",UiMissionTimeLimit:" + UiMissionTimeLimit + ",UiMissionNpcPosition:" + UiMissionNpcPosition + ",SeedPanelTitle:" + SeedPanelTitle + ",SeedNodeAlreadyUnlock:" + SeedNodeAlreadyUnlock + ",SeedNodeSucceedUnlock:" + SeedNodeSucceedUnlock + ",BuildingPanelTitle:" + BuildingPanelTitle + ",BuildingPanelCoverSizeDescription:" + BuildingPanelCoverSizeDescription + ",BuildingPanelInnerSizeDescription:" + BuildingPanelInnerSizeDescription + ",BuildingPanelAnimalCapacityDescription:" + BuildingPanelAnimalCapacityDescription + ",BuildingPanelSizeDescription:" + BuildingPanelSizeDescription + ",BuildingPanelEmpty:" + BuildingPanelEmpty + ",BuildingPanelStartBuild:" + BuildingPanelStartBuild + ",BuildingPanelMaterialNotEnough:" + BuildingPanelMaterialNotEnough + ",BuildingPanelMoneyNotEnough:" + BuildingPanelMoneyNotEnough + ",BuildingPanelInputBuildingName:" + BuildingPanelInputBuildingName + ",EquipmentPanelEmpty:" + EquipmentPanelEmpty + ",EquipmentPanelStartBuild:" + EquipmentPanelStartBuild + ",EquipmentPanelListName:" + EquipmentPanelListName + ",EquipmentViewerEmpty:" + EquipmentViewerEmpty + ",EquipmentPanelLatelyEmpty:" + EquipmentPanelLatelyEmpty + ",EquipmentPanelMakeComplete:" + EquipmentPanelMakeComplete + ",EquipmentPanelHide:" + EquipmentPanelHide + ",EquipmentPanelJump:" + EquipmentPanelJump + ",EquipmentPanelUnlockTip:" + EquipmentPanelUnlockTip + ",EquipmentPanelElectronicTip:" + EquipmentPanelElectronicTip + ",EquipmentPanelBatteryTip:" + EquipmentPanelBatteryTip + ",EquipmentPanelApplianceTip:" + EquipmentPanelApplianceTip + ",EquipmentPanelGeneratorTip:" + EquipmentPanelGeneratorTip + ",UiEmailTitle:" + UiEmailTitle + ",UiEmailEmpty:" + UiEmailEmpty + ",UiEmailAcceptMission:" + UiEmailAcceptMission + ",UiEmailAlreadyAccepted:" + UiEmailAlreadyAccepted + ",UiEmailReciveItem:" + UiEmailReciveItem + ",UiEmailAlreadyRecived:" + UiEmailAlreadyRecived + ",UiEmailRecycleErr:" + UiEmailRecycleErr + ",RecipePanelEmpty:" + RecipePanelEmpty + ",RecipePanelStartBuild:" + RecipePanelStartBuild + ",RecipePanelTimeInfo:" + RecipePanelTimeInfo + ",RecipePanelAlreadyWorking:" + RecipePanelAlreadyWorking + ",RecipePanelTitle:" + RecipePanelTitle + ",RecipePanelTask:" + RecipePanelTask + ",RecipePanelRestTime:" + RecipePanelRestTime + ",RecipePanelMaxCraftCount:" + RecipePanelMaxCraftCount + ",RecipePanelLimitUp:" + RecipePanelLimitUp + ",RecipePanelNotCookable:" + RecipePanelNotCookable + ",RecipePanelNoMaterial:" + RecipePanelNoMaterial + ",RecipePanelConfirmWithTime:" + RecipePanelConfirmWithTime + ",RecipePanelUnlockedRecipeComment:" + RecipePanelUnlockedRecipeComment + ",RecipePanelUnlockedDishComment:" + RecipePanelUnlockedDishComment + ",RecipePanelUnknowDishComment:" + RecipePanelUnknowDishComment + ",RecipePanelConfirmPopBuffer:" + RecipePanelConfirmPopBuffer + ",RecipePanelMaterialList:" + RecipePanelMaterialList + ",RecipePanelExistItems:" + RecipePanelExistItems + ",RecipePanelRandomGeneHint:" + RecipePanelRandomGeneHint + ",PlatformPanelTitle:" + PlatformPanelTitle + ",PlatformPanelEmpty:" + PlatformPanelEmpty + ",PlatformPanelStartBuild:" + PlatformPanelStartBuild + ",PlatformPanelCostPrefix:" + PlatformPanelCostPrefix + ",ItemBuildingProtoError:" + ItemBuildingProtoError + ",ItemEquipmentProtoError:" + ItemEquipmentProtoError + ",ItemSendEmailOnOverflow:" + ItemSendEmailOnOverflow + ",ItemSleepingBagConditionFailedMonster:" + ItemSleepingBagConditionFailedMonster + ",ItemSleepingBagConditionFailedWater:" + ItemSleepingBagConditionFailedWater + ",ItemRescuePagerConditionFailed:" + ItemRescuePagerConditionFailed + ",ItemTipPrice:" + ItemTipPrice + ",ItemTipBasicsPrice:" + ItemTipBasicsPrice + ",ItemTipMoneyUnit:" + ItemTipMoneyUnit + ",ItemChipAttackIncrease:" + ItemChipAttackIncrease + ",ItemChipAttackIncreaseFixed:" + ItemChipAttackIncreaseFixed + ",ItemChipCriticalRateIncrease:" + ItemChipCriticalRateIncrease + ",ItemChipAttackSpeedIncrease:" + ItemChipAttackSpeedIncrease + ",ItemChipAccuracyIncrease:" + ItemChipAccuracyIncrease + ",ItemChipPowerCostDecrease:" + ItemChipPowerCostDecrease + ",ItemChipAttackDistanceIncrease:" + ItemChipAttackDistanceIncrease + ",ItemChipMoveSpeedIncrease:" + ItemChipMoveSpeedIncrease + ",ItemChipClipCapacityAddition:" + ItemChipClipCapacityAddition + ",ItemChipReloadDurationDecrease:" + ItemChipReloadDurationDecrease + ",ItemEngineMoveSpeedIncrease:" + ItemEngineMoveSpeedIncrease + ",ItemEnginePowerCapacityIncrease:" + ItemEnginePowerCapacityIncrease + ",ItemEnginePowerRecvIncrease:" + ItemEnginePowerRecvIncrease + ",ItemStructureMoveSpeedIncrease:" + ItemStructureMoveSpeedIncrease + ",ItemStructurePowerCapacity:" + ItemStructurePowerCapacity + ",ItemStructurePowerRecv:" + ItemStructurePowerRecv + ",ItemMaxDurability:" + ItemMaxDurability + ",ItemTitleInfoFormat:" + ItemTitleInfoFormat + ",ItemConfirmUse:" + ItemConfirmUse + ",ItemWaterCanArea:" + ItemWaterCanArea + ",ItemWaterCanEndlessWater:" + ItemWaterCanEndlessWater + ",ItemGeneDescFormat:" + ItemGeneDescFormat + ",ItemSeedCloned:" + ItemSeedCloned + ",ItemWpIsAvailableFor:" + ItemWpIsAvailableFor + ",ItemWpAvailableAll:" + ItemWpAvailableAll + ",ItemFishFryTitleFormat:" + ItemFishFryTitleFormat + ",ItemToolLevelFormat:" + ItemToolLevelFormat + ",ItemPatchValueFormat:" + ItemPatchValueFormat + ",ItemBoxValueFormat:" + ItemBoxValueFormat + ",ItemFilmValueFormat:" + ItemFilmValueFormat + ",ItemDroneWeaponValueFormat:" + ItemDroneWeaponValueFormat + ",ItemMoneyTitle:" + ItemMoneyTitle + ",ItemMoneyDesc:" + ItemMoneyDesc + ",ItemMoneyType:" + ItemMoneyType + ",UiSystemExitGame:" + UiSystemExitGame + ",UiSystemReturnHomePage:" + UiSystemReturnHomePage + ",UiSystemConfirmHome:" + UiSystemConfirmHome + ",UiSystemConfirmExit:" + UiSystemConfirmExit + ",RewardInfoBuildingUnlock:" + RewardInfoBuildingUnlock + ",RewardInfoBusStationUnlock:" + RewardInfoBusStationUnlock + ",RewardInfoEquipmentUnlock:" + RewardInfoEquipmentUnlock + ",RewardInfoPlatformUnlock:" + RewardInfoPlatformUnlock + ",RewardInfoRecipeUnlock:" + RewardInfoRecipeUnlock + ",RewardInfoRecipeUnlockHint:" + RewardInfoRecipeUnlockHint + ",RewardInfoItem:" + RewardInfoItem + ",RewardInfoGold:" + RewardInfoGold + ",RewardInfoFavorabilityLv1:" + RewardInfoFavorabilityLv1 + ",RewardInfoFavorabilityLv2:" + RewardInfoFavorabilityLv2 + ",RewardInfoFavorabilityLv3:" + RewardInfoFavorabilityLv3 + ",DropoffBoxNoGoods:" + DropoffBoxNoGoods + ",DropoffBoxNoDrone:" + DropoffBoxNoDrone + ",DropoffBoxTotalMoney:" + DropoffBoxTotalMoney + ",DropoffBoxPriceIncreased:" + DropoffBoxPriceIncreased + ",DropoffBoxLaunchDrone:" + DropoffBoxLaunchDrone + ",DropoffBoxConfirmLauch:" + DropoffBoxConfirmLauch + ",AutomateBotPanelStateIdle:" + AutomateBotPanelStateIdle + ",AutomateBotPanelStateCharge:" + AutomateBotPanelStateCharge + ",AutomateBotPanelStatePause:" + AutomateBotPanelStatePause + ",AutomateBotPanelStateWorking:" + AutomateBotPanelStateWorking + ",AutomateBotPanelCfgEmpty:" + AutomateBotPanelCfgEmpty + ",AutomateBotPanelRecipeEmpty:" + AutomateBotPanelRecipeEmpty + ",AutomateBotPanelDateEmpty:" + AutomateBotPanelDateEmpty + ",AutomateBotPanelRecipeType:" + AutomateBotPanelRecipeType + ",AutomateBotPanelRecipeSubType:" + AutomateBotPanelRecipeSubType + ",AutomateBotPanelAutoFertilizer:" + AutomateBotPanelAutoFertilizer + ",AutomateBotPanelAutoProtect:" + AutomateBotPanelAutoProtect + ",AutomateBotPanelEnergyType:" + AutomateBotPanelEnergyType + ",AutomateBotPanelItemError:" + AutomateBotPanelItemError + ",BoardMissionPanelTitle:" + BoardMissionPanelTitle + ",BoardMissionUrgencyLow:" + BoardMissionUrgencyLow + ",BoardMissionUrgencyMiddle:" + BoardMissionUrgencyMiddle + ",BoardMissionUrgencyHigh:" + BoardMissionUrgencyHigh + ",BoardMissionUrgencyNone:" + BoardMissionUrgencyNone + ",BoardMissionAcceptFail:" + BoardMissionAcceptFail + ",BoardMissionTitle:" + BoardMissionTitle + ",BoardMissionLowLevel:" + BoardMissionLowLevel + ",BoardMissionEmpty:" + BoardMissionEmpty + ",BoardMissionOverdue:" + BoardMissionOverdue + ",BoardMissionLv:" + BoardMissionLv + ",BoardMissionExpTip:" + BoardMissionExpTip + ",InventoryPanelCannotPutIn:" + InventoryPanelCannotPutIn + ",InventoryPanelContainerFull:" + InventoryPanelContainerFull + ",BoxPanelRestCount:" + BoxPanelRestCount + ",BoxPanelUsedUpWarning:" + BoxPanelUsedUpWarning + ",BoxPanelBroken:" + BoxPanelBroken + ",BoxPanelAlreadyOpen:" + BoxPanelAlreadyOpen + ",BoxPanelNoNeedRepair:" + BoxPanelNoNeedRepair + ",BoxPanelNoRepairCost:" + BoxPanelNoRepairCost + ",BoxPanelRepairInfo:" + BoxPanelRepairInfo + ",InventoryPanelDungeonCaseTitle:" + InventoryPanelDungeonCaseTitle + ",InventoryPanelBackpackTitle:" + InventoryPanelBackpackTitle + ",FishTankPanelFeedQuantity:" + FishTankPanelFeedQuantity + ",InventoryPanelSocketTitle:" + InventoryPanelSocketTitle + ",InventoryPanelPutBoxFirst:" + InventoryPanelPutBoxFirst + ",InventoryPanelInfoGeneIncubator:" + InventoryPanelInfoGeneIncubator + ",InventoryPanelCapsuleTitle:" + InventoryPanelCapsuleTitle + ",InventoryPanelInfoGeneReplicator:" + InventoryPanelInfoGeneReplicator + ",InventoryPanelGeneReplicatorLocked:" + InventoryPanelGeneReplicatorLocked + ",InventoryPanelInfoGeneSynthesizer:" + InventoryPanelInfoGeneSynthesizer + ",InventoryPanelGeneSynthesizerLocked:" + InventoryPanelGeneSynthesizerLocked + ",UiTipTimeTitle:" + UiTipTimeTitle + ",UiTipTimeFormat:" + UiTipTimeFormat + ",UiTipCurrentSeason:" + UiTipCurrentSeason + ",UiTipOpenMenu:" + UiTipOpenMenu + ",UiTipOpenMap:" + UiTipOpenMap + ",UiTipShowMissionTip:" + UiTipShowMissionTip + ",UiTipHideMissionTip:" + UiTipHideMissionTip + ",UiTipNoMission:" + UiTipNoMission + ",UiTipMissionShowDetails:" + UiTipMissionShowDetails + ",UiTipHealthValue:" + UiTipHealthValue + ",UiTipEnergyValue:" + UiTipEnergyValue + ",UiTipCorrosionValue:" + UiTipCorrosionValue + ",UiTipCurrentSpiritLe50:" + UiTipCurrentSpiritLe50 + ",UiTipCurrentSpiritLe25:" + UiTipCurrentSpiritLe25 + ",UiTipCurrentSpiritLe10:" + UiTipCurrentSpiritLe10 + ",UiTipCurrentSpiritLe0:" + UiTipCurrentSpiritLe0 + ",UiTipBuffDuration:" + UiTipBuffDuration + ",UiTipDebuffDuration:" + UiTipDebuffDuration + ",UiTipBuildHealth:" + UiTipBuildHealth + ",UiTipElectricityGeneratorInfo:" + UiTipElectricityGeneratorInfo + ",UiTipElectricityBatteryInfo:" + UiTipElectricityBatteryInfo + ",UiTipElectricityStatusNone:" + UiTipElectricityStatusNone + ",UiTipElectricityStatusLackOfGeneration:" + UiTipElectricityStatusLackOfGeneration + ",UiTipElectricityStatusLackOfGenerationCostBattery:" + UiTipElectricityStatusLackOfGenerationCostBattery + ",UiTipElectricityStatusBatterySaving:" + UiTipElectricityStatusBatterySaving + ",UiTipElectricityStatusBatteryFull:" + UiTipElectricityStatusBatteryFull + ",UiTipElectricityStatusPowerLoss:" + UiTipElectricityStatusPowerLoss + ",UiTipSeedEnd:" + UiTipSeedEnd + ",UiTipLoading:" + UiTipLoading + ",DocumentTitleComputer:" + DocumentTitleComputer + ",NpcDocumentTitle:" + NpcDocumentTitle + ",ChipDocumentTitle:" + ChipDocumentTitle + ",PlantDocumentTitle:" + PlantDocumentTitle + ",DocumentEmpty:" + DocumentEmpty + ",ChipDocumentTotle:" + ChipDocumentTotle + ",GameDataPanelTitle:" + GameDataPanelTitle + ",GameDataErrRead:" + GameDataErrRead + ",GameDataTitle:" + GameDataTitle + ",GameDataDelConfirm:" + GameDataDelConfirm + ",GameDataDelSuccess:" + GameDataDelSuccess + ",GameDataErrEmpty:" + GameDataErrEmpty + ",GameDataSaving:" + GameDataSaving + ",GameDataSaveFail:" + GameDataSaveFail + ",GameDataSaveSuccessful:" + GameDataSaveSuccessful + ",GameDataFull:" + GameDataFull + ",GameDataDuplicateSucess:" + GameDataDuplicateSucess + ",GameDataStart:" + GameDataStart + ",GameDataLoad:" + GameDataLoad + ",TeleportFail:" + TeleportFail + ",TeleportConfirm:" + TeleportConfirm + ",TeleportBuyTicket:" + TeleportBuyTicket + ",TeleportErrMaterialNotEnough:" + TeleportErrMaterialNotEnough + ",FactionMissionFinish:" + FactionMissionFinish + ",FactionMissionCannotSubmit:" + FactionMissionCannotSubmit + ",FactionMissionFinishSubmit:" + FactionMissionFinishSubmit + ",FactionMissionMoneySubmit:" + FactionMissionMoneySubmit + ",FactionMissionErrSubmit:" + FactionMissionErrSubmit + ",FactionMissionLock:" + FactionMissionLock + ",TreatyPortTitle:" + TreatyPortTitle + ",TreatyPortRepairTime:" + TreatyPortRepairTime + ",TreatyPortRecruitStats:" + TreatyPortRecruitStats + ",TreatyPortPrincipal:" + TreatyPortPrincipal + ",TreatyPortFactionMissionProgress:" + TreatyPortFactionMissionProgress + ",TreatyPortFactionReputation:" + TreatyPortFactionReputation + ",TreatyPortContactNpc:" + TreatyPortContactNpc + ",TreatyPortFactionEnterTime:" + TreatyPortFactionEnterTime + ",TreatyPortBroadcast:" + TreatyPortBroadcast + ",TreatyPortFactionLock:" + TreatyPortFactionLock + ",TreatyPortFactionUnopen:" + TreatyPortFactionUnopen + ",TreatyPortRecruitHint:" + TreatyPortRecruitHint + ",TreatyPortTimeFormat:" + TreatyPortTimeFormat + ",TreatyPortFactionRefuse:" + TreatyPortFactionRefuse + ",TreatyPortOffDutyHoursTip:" + TreatyPortOffDutyHoursTip + ",TreatyPortInterviewTip:" + TreatyPortInterviewTip + ",TreatyPortFactionSettledTip:" + TreatyPortFactionSettledTip + ",TreatyPortNotContactedTip:" + TreatyPortNotContactedTip + ",SettingPanelSaveSuccessful:" + SettingPanelSaveSuccessful + ",SettingPanelOtherSaveSuccessful:" + SettingPanelOtherSaveSuccessful + ",SettingPanelReset:" + SettingPanelReset + ",SettingPanelResetConfirm:" + SettingPanelResetConfirm + ",SettingPanelTitle:" + SettingPanelTitle + ",SettingPanelExitConfirm:" + SettingPanelExitConfirm + ",SettingPanelConflictHint:" + SettingPanelConflictHint + ",SettingPanelResetToDefault:" + SettingPanelResetToDefault + ",SettingPanelCanNotEdit:" + SettingPanelCanNotEdit + ",SettingPanelCanNotRemove:" + SettingPanelCanNotRemove + ",SettingPanelNoValidInput:" + SettingPanelNoValidInput + ",SettingPanelAllowTracedataCollector:" + SettingPanelAllowTracedataCollector + ",SettingPanelTracedataCollectorDesc:" + SettingPanelTracedataCollectorDesc + ",SettingPanelDelete:" + SettingPanelDelete + ",SettingPanelCanNotSaveByConflict:" + SettingPanelCanNotSaveByConflict + ",SettingPanelNewDevice:" + SettingPanelNewDevice + ",SettingPanelLoading:" + SettingPanelLoading + ",EquipmentBarTimeLabel:" + EquipmentBarTimeLabel + ",EquipmentBarTimeFormat:" + EquipmentBarTimeFormat + ",EquipmentBarDroneNotEquip:" + EquipmentBarDroneNotEquip + ",EquipmentBarMotorTitle:" + EquipmentBarMotorTitle + ",EquipmentBarMotorLock:" + EquipmentBarMotorLock + ",EquipmentBarHatTip:" + EquipmentBarHatTip + ",EquipmentBarPositiveTip:" + EquipmentBarPositiveTip + ",EquipmentBarPassive1Tip:" + EquipmentBarPassive1Tip + ",EquipmentBarPassive2Tip:" + EquipmentBarPassive2Tip + ",EquipmentBarDroneTip:" + EquipmentBarDroneTip + ",EquipmentBarSkillLock:" + EquipmentBarSkillLock + ",EquipmentBarDoubleJumpDesc:" + EquipmentBarDoubleJumpDesc + ",EquipmentBarSprintDesc:" + EquipmentBarSprintDesc + ",AbilityDoubleJumpUnlockTip:" + AbilityDoubleJumpUnlockTip + ",AbilitySprintUnlockTip:" + AbilitySprintUnlockTip + ",EquipmentBarBackpackFull:" + EquipmentBarBackpackFull + ",EquipmentSkillPrefix:" + EquipmentSkillPrefix + ",EquipmentDefensePrefix:" + EquipmentDefensePrefix + ",UiTipHomepageStartGame:" + UiTipHomepageStartGame + ",UiTipHomepageChangelog:" + UiTipHomepageChangelog + ",UiTipHomepageSettings:" + UiTipHomepageSettings + ",UiTipHomepageMods:" + UiTipHomepageMods + ",UiTipHomepageDeveloperList:" + UiTipHomepageDeveloperList + ",UiTipHomepageExitGame:" + UiTipHomepageExitGame + ",UiTipParkingApronLocked:" + UiTipParkingApronLocked + ",UiTipResolving:" + UiTipResolving + ",UiTipShredderMoney:" + UiTipShredderMoney + ",UiTipErrShredderEmpty:" + UiTipErrShredderEmpty + ",UiTipAirWall:" + UiTipAirWall + ",UiTipNotAvailableToMotor:" + UiTipNotAvailableToMotor + ",CollectionPanelItemLabel:" + CollectionPanelItemLabel + ",CollectionPanelItemRecipeTime:" + CollectionPanelItemRecipeTime + ",CollectionPanelItemRecipeEmpty:" + CollectionPanelItemRecipeEmpty + ",CollectionPanelItemUnknown:" + CollectionPanelItemUnknown + ",CollectionPanelItemSource:" + CollectionPanelItemSource + ",CollectionPanelNpcAddress:" + CollectionPanelNpcAddress + ",CollectionPanelNpcLikeRecord:" + CollectionPanelNpcLikeRecord + ",CollectionPanelNpcLikeNone:" + CollectionPanelNpcLikeNone + ",CollectionPanelNpcLikingLock:" + CollectionPanelNpcLikingLock + ",CollectionPanelNpcLikingLvLock:" + CollectionPanelNpcLikingLvLock + ",CollectionPanelNpcContentTitle:" + CollectionPanelNpcContentTitle + ",CollectionPanelNpcContentLockTip:" + CollectionPanelNpcContentLockTip + ",CollectionPanelNpcContentEnd:" + CollectionPanelNpcContentEnd + ",CollectionPanelMonsterUnknown:" + CollectionPanelMonsterUnknown + ",CollectionPanelMonsterHabitat:" + CollectionPanelMonsterHabitat + ",CollectionPanelMonsterDropText:" + CollectionPanelMonsterDropText + ",CollectionPanelMonsterOrganism:" + CollectionPanelMonsterOrganism + ",CollectionPanelMonsterMachinery:" + CollectionPanelMonsterMachinery + ",CollectionPanelMonsterBoss:" + CollectionPanelMonsterBoss + ",CollectionPanelMonsterContentTitle:" + CollectionPanelMonsterContentTitle + ",CollectionPanelMonsterContentLockTip:" + CollectionPanelMonsterContentLockTip + ",CollectionPanelDocumentLabel:" + CollectionPanelDocumentLabel + ",CollectionPanelAnimalPossess:" + CollectionPanelAnimalPossess + ",CollectionPanelAnimalBreed:" + CollectionPanelAnimalBreed + ",CollectionPanelAnimalProductText:" + CollectionPanelAnimalProductText + ",CollectionPanelAnimalContentLockBringUp:" + CollectionPanelAnimalContentLockBringUp + ",CollectionPanelAnimalContentLockBreed:" + CollectionPanelAnimalContentLockBreed + ",CollectionPanelAnimalContentLockProduct:" + CollectionPanelAnimalContentLockProduct + ",CollectionPanelFishCatch:" + CollectionPanelFishCatch + ",CollectionPanelFishBaitText:" + CollectionPanelFishBaitText + ",CollectionPanelFishPlace:" + CollectionPanelFishPlace + ",CollectionPanelFishMonth:" + CollectionPanelFishMonth + ",CollectionPanelFishWeather:" + CollectionPanelFishWeather + ",CollectionPanelFishWeatherNone:" + CollectionPanelFishWeatherNone + ",CollectionPanelFishContentLockTip:" + CollectionPanelFishContentLockTip + ",CollectionPanelResourceCollect:" + CollectionPanelResourceCollect + ",CollectionPanelResourceGrowthPeriod:" + CollectionPanelResourceGrowthPeriod + ",CollectionPanelResourceYearRoundGrowth:" + CollectionPanelResourceYearRoundGrowth + ",CollectionPanelResourceDropTitle:" + CollectionPanelResourceDropTitle + ",CollectionPanelResourceContentLockTip:" + CollectionPanelResourceContentLockTip + ",BuilderPanelLabelBuilding:" + BuilderPanelLabelBuilding + ",BuilderPanelLabelEquipment:" + BuilderPanelLabelEquipment + ",BuilderPanelLabelPlatform:" + BuilderPanelLabelPlatform + ",BuilderPanelSwitchTerrainLayer:" + BuilderPanelSwitchTerrainLayer + ",BuilderActionSelectedContent:" + BuilderActionSelectedContent + ",BuilderActionUndo:" + BuilderActionUndo + ",BuilderActionTurn:" + BuilderActionTurn + ",BuilderActionDismantle:" + BuilderActionDismantle + ",BuilderActionBuildingDismantle:" + BuilderActionBuildingDismantle + ",BuilderActionBuildingStorage:" + BuilderActionBuildingStorage + ",BuilderActionMoveCamera:" + BuilderActionMoveCamera + ",BuilderActionMove:" + BuilderActionMove + ",BuilderActionSelectedItem:" + BuilderActionSelectedItem + ",BuilderActionPreciseMovement:" + BuilderActionPreciseMovement + ",BuilderActionSwitchPrecise:" + BuilderActionSwitchPrecise + ",BuilderActionSwitchBackpack:" + BuilderActionSwitchBackpack + ",BuilderActionRollingBackpack:" + BuilderActionRollingBackpack + ",BuilderActionRollingItem:" + BuilderActionRollingItem + ",BuilderActionToggleBackpack:" + BuilderActionToggleBackpack + ",BuilderActionToggleBackpackGamepad:" + BuilderActionToggleBackpackGamepad + ",BuilderPanelExit:" + BuilderPanelExit + ",BuilderPanelUndoGamepad:" + BuilderPanelUndoGamepad + ",BuilderPanelExpandBuildingList:" + BuilderPanelExpandBuildingList + ",BuilderPanellFoldBuildingList:" + BuilderPanellFoldBuildingList + ",BuilderPanelFinish:" + BuilderPanelFinish + ",BuilderDismantleBuildingErr:" + BuilderDismantleBuildingErr + ",BuilderBuilderConstruct:" + BuilderBuilderConstruct + ",BuilderPanelTempBuildingFull:" + BuilderPanelTempBuildingFull + ",BuilderExitErrAnimal:" + BuilderExitErrAnimal + ",BuilderExitErrOccupied:" + BuilderExitErrOccupied + ",BuilderExitErrSoleBuilding:" + BuilderExitErrSoleBuilding + ",BuilderExitConfirm:" + BuilderExitConfirm + ",UiEnvOptimizerPanelTitle:" + UiEnvOptimizerPanelTitle + ",UiEnvOptimizerConsoleTitle:" + UiEnvOptimizerConsoleTitle + ",UiEnvOptimizerOverview:" + UiEnvOptimizerOverview + ",UiEnvOptimizerButtonNoEnergy:" + UiEnvOptimizerButtonNoEnergy + ",UiEnvOptimizerTipNoEnergy:" + UiEnvOptimizerTipNoEnergy + ",UiEnvOptimizerButtonNoComponent:" + UiEnvOptimizerButtonNoComponent + ",UiEnvOptimizerTipNoComponent:" + UiEnvOptimizerTipNoComponent + ",UiEnvOptimizerButtonValid:" + UiEnvOptimizerButtonValid + ",UiEnvOptimizerButtonNotValid:" + UiEnvOptimizerButtonNotValid + ",UiEnvOptimizerCheckSuccess:" + UiEnvOptimizerCheckSuccess + ",UiEnvOptimizerDateInfo:" + UiEnvOptimizerDateInfo + ",UiEnvOptimizerSlotTitle:" + UiEnvOptimizerSlotTitle + ",UiEnvOptimizerComponentAlreadyActive:" + UiEnvOptimizerComponentAlreadyActive + ",UiEnvOptimizerInRecognition:" + UiEnvOptimizerInRecognition + ",UiEnvOptimizerLoadingData:" + UiEnvOptimizerLoadingData + ",UiEnvOptimizerActiveSuccess:" + UiEnvOptimizerActiveSuccess + ",AnimalInvalidBuilding:" + AnimalInvalidBuilding + ",AnimalInvalidRoom:" + AnimalInvalidRoom + ",AnimalFullBuilding:" + AnimalFullBuilding + ",AnimalInputAnimalName:" + AnimalInputAnimalName + ",AnimalPackageIsFull:" + AnimalPackageIsFull + ",AnimalHasEscaped:" + AnimalHasEscaped + ",AnimalNoAnimal:" + AnimalNoAnimal + ",AnimalInUse:" + AnimalInUse + ",CalendarPanelYearTitle:" + CalendarPanelYearTitle + ",CalendarPanelDateTitle:" + CalendarPanelDateTitle + ",CalendarPanelEventTitle:" + CalendarPanelEventTitle + ",CalendarPanelPlayerBirthday:" + CalendarPanelPlayerBirthday + ",CalendarPanelMemoTitle:" + CalendarPanelMemoTitle + ",CalendarPanelEmptyHint:" + CalendarPanelEmptyHint + ",CalendarPanelDelMemoHint:" + CalendarPanelDelMemoHint + ",CalendarPanelInputEmptyHint:" + CalendarPanelInputEmptyHint + ",InputTextContainsSensitiveWorld:" + InputTextContainsSensitiveWorld + ",CalendarMemoMessage:" + CalendarMemoMessage + ",UiCropInfoGrowthLevel:" + UiCropInfoGrowthLevel + ",UiCropInfoHarvestCount:" + UiCropInfoHarvestCount + ",UiAnimalInfoState:" + UiAnimalInfoState + ",UiAnimalSpace:" + UiAnimalSpace + ",UiAnimalAgeYear:" + UiAnimalAgeYear + ",UiAnimalAgeMonth:" + UiAnimalAgeMonth + ",UiAnimalAgeDay:" + UiAnimalAgeDay + ",UiAnimalBirthday:" + UiAnimalBirthday + ",UiAnimalCapacity:" + UiAnimalCapacity + ",UiAnimalCallBack:" + UiAnimalCallBack + ",UiAnimalLetOut:" + UiAnimalLetOut + ",UiAnimalAdult:" + UiAnimalAdult + ",UiAnimalChild:" + UiAnimalChild + ",UiAnimalPosition:" + UiAnimalPosition + ",UiAnimalNoAnimal:" + UiAnimalNoAnimal + ",UiAnimalNotVisible:" + UiAnimalNotVisible + ",DronePanelTitle:" + DronePanelTitle + ",DronePanelErrLoad:" + DronePanelErrLoad + ",DronePanelErrLocked:" + DronePanelErrLocked + ",DroneComponentTitle:" + DroneComponentTitle + ",GarbageSubmitErrItem:" + GarbageSubmitErrItem + ",DisposeFailRoom:" + DisposeFailRoom + ",UiTipOpenMapFail:" + UiTipOpenMapFail + ",UiTipNone:" + UiTipNone + ",UiTipBuyBackpack:" + UiTipBuyBackpack + ",UiTipBuyBackpackInfo:" + UiTipBuyBackpackInfo + ",UiTipEmptyList:" + UiTipEmptyList + ",UiTipCurrentPosition:" + UiTipCurrentPosition + ",UiTipCurrentMotorPosition:" + UiTipCurrentMotorPosition + ",UiOptionShootingRangeStart:" + UiOptionShootingRangeStart + ",UiOptionShootingRangeEnd:" + UiOptionShootingRangeEnd + ",UiTipEquipmentUnlock:" + UiTipEquipmentUnlock + ",UiOptionShootingRangeExit:" + UiOptionShootingRangeExit + ",UiOptionConfirmShootingRangeExit:" + UiOptionConfirmShootingRangeExit + ",UiNpcVisit:" + UiNpcVisit + ",UiNpcFileUpdation:" + UiNpcFileUpdation + ",UiNpcLikingRise:" + UiNpcLikingRise + ",UiNpcLikingDecline:" + UiNpcLikingDecline + ",ItemEdenFruitTip:" + ItemEdenFruitTip + ",UiTipGameVersion:" + UiTipGameVersion + ",UiTipContentLock:" + UiTipContentLock + ",UiTipPhotoSaving:" + UiTipPhotoSaving + ",UiItemGenerateElectricityEntry:" + UiItemGenerateElectricityEntry + ",UiTipRename:" + UiTipRename + ",UiModNoMod:" + UiModNoMod + ",UiModNeedSubscribe:" + UiModNeedSubscribe + ",UiModOpenWorkshop:" + UiModOpenWorkshop + ",UiModNeedCreate:" + UiModNeedCreate + ",UiModOpenLocalDirectory:" + UiModOpenLocalDirectory + ",UiModUploadMod:" + UiModUploadMod + ",UiModUpdateMod:" + UiModUpdateMod + ",UiModEnable:" + UiModEnable + ",UiModDisable:" + UiModDisable + ",UiModConfirmUpload:" + UiModConfirmUpload + ",UiModConfirmUpdate:" + UiModConfirmUpdate + ",UiModUploading:" + UiModUploading + ",UiModUploadSuccess:" + UiModUploadSuccess + ",UiModUploadFailed:" + UiModUploadFailed + ",UiModUpdating:" + UiModUpdating + ",UiModUpdateSuccess:" + UiModUpdateSuccess + ",UiModUpdateFailed:" + UiModUpdateFailed + ",UiModReloading:" + UiModReloading + ",UiModSourceLocal:" + UiModSourceLocal + ",UiModSourceWorkshop:" + UiModSourceWorkshop + ",}";
	}
}
