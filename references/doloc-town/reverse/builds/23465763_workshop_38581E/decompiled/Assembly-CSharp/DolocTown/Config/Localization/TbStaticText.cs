using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TbStaticText
{
	private readonly StaticTextInfo _data;

	public string SysErrDatalost => _data.SysErrDatalost;

	public string UiMoneyTip => _data.UiMoneyTip;

	public string UiItemSimpleTip => _data.UiItemSimpleTip;

	public string UiItemTip => _data.UiItemTip;

	public string UiTipConsumable => _data.UiTipConsumable;

	public string UiErrMaterialNotEnough => _data.UiErrMaterialNotEnough;

	public string UiErrMoneyNotEnough => _data.UiErrMoneyNotEnough;

	public string UiErrBackpackIsFull => _data.UiErrBackpackIsFull;

	public string UiErrNotDisposeItem => _data.UiErrNotDisposeItem;

	public string UiQuesDisposeItem => _data.UiQuesDisposeItem;

	public string UiMenuItemnotavaiable => _data.UiMenuItemnotavaiable;

	public string UiBackpackCellExchange => _data.UiBackpackCellExchange;

	public string UiErrBackpackCellExchange => _data.UiErrBackpackCellExchange;

	public string UiSortDefault => _data.UiSortDefault;

	public string UiSortUnlock => _data.UiSortUnlock;

	public string UiLoseItemTip => _data.UiLoseItemTip;

	public string UiLoseMoneyTip => _data.UiLoseMoneyTip;

	public string UiUnlockSeed => _data.UiUnlockSeed;

	public string UiAnalyzerTime => _data.UiAnalyzerTime;

	public string UiAnalyzerOutput => _data.UiAnalyzerOutput;

	public string UiAnalyzerTotalTime => _data.UiAnalyzerTotalTime;

	public string UiShredderConfirm => _data.UiShredderConfirm;

	public string UiShredderConfirmUpgrade => _data.UiShredderConfirmUpgrade;

	public string UiSubmitConfirm => _data.UiSubmitConfirm;

	public string UiSubmitConfirmGene => _data.UiSubmitConfirmGene;

	public string UiSubmitConfirmMoney => _data.UiSubmitConfirmMoney;

	public string UiSubmitNotEnough => _data.UiSubmitNotEnough;

	public string UiGiftItemConfirm => _data.UiGiftItemConfirm;

	public string UiTipDismentle => _data.UiTipDismentle;

	public string UiTipPlantErr => _data.UiTipPlantErr;

	public string UiTextYears => _data.UiTextYears;

	public string UiTextMonths => _data.UiTextMonths;

	public string UiTextDays => _data.UiTextDays;

	public string UiTextHours => _data.UiTextHours;

	public string UiTextMinutes => _data.UiTextMinutes;

	public string UiTextTimeFormat => _data.UiTextTimeFormat;

	public string UiTextDemoStatement => _data.UiTextDemoStatement;

	public string UiTextGeneDescription => _data.UiTextGeneDescription;

	public string InputTitlePlayerName => _data.InputTitlePlayerName;

	public string InputTitleContainerName => _data.InputTitleContainerName;

	public string InputNameEmpty => _data.InputNameEmpty;

	public string InputNameConfirm => _data.InputNameConfirm;

	public string InputNameContainsSensitiveWorld => _data.InputNameContainsSensitiveWorld;

	public string InputTitlePlayerBirthday => _data.InputTitlePlayerBirthday;

	public string InputBirthdayConfirm => _data.InputBirthdayConfirm;

	public string UiSavepointDefault => _data.UiSavepointDefault;

	public string UiSavepointSleep => _data.UiSavepointSleep;

	public string UiSavepointNap => _data.UiSavepointNap;

	public string UiSavepointKillTime => _data.UiSavepointKillTime;

	public string UiSavepointSit => _data.UiSavepointSit;

	public string UiSavepointExit => _data.UiSavepointExit;

	public string UiSavepointCancel => _data.UiSavepointCancel;

	public string UiTipConfirm => _data.UiTipConfirm;

	public string UiTipCancel => _data.UiTipCancel;

	public string UiTipQuit => _data.UiTipQuit;

	public string UiTipSwitchClassifying => _data.UiTipSwitchClassifying;

	public string UiTipSort => _data.UiTipSort;

	public string UiTipUpgrade => _data.UiTipUpgrade;

	public string UiTipBuy => _data.UiTipBuy;

	public string UiTipSellConfirm => _data.UiTipSellConfirm;

	public string UiTipSell => _data.UiTipSell;

	public string UiTipBuyConfirm => _data.UiTipBuyConfirm;

	public string UiTipUnlock => _data.UiTipUnlock;

	public string UiTipUnlockSeed => _data.UiTipUnlockSeed;

	public string UiTipLaunchDrone => _data.UiTipLaunchDrone;

	public string UiTipSwitchAutomateBot => _data.UiTipSwitchAutomateBot;

	public string UiTipAutomateBotSwitch => _data.UiTipAutomateBotSwitch;

	public string UiTipAutomateBotUnload => _data.UiTipAutomateBotUnload;

	public string UiTipAutomateBotLoad => _data.UiTipAutomateBotLoad;

	public string UiTipChoice => _data.UiTipChoice;

	public string UiTipSubmitAll => _data.UiTipSubmitAll;

	public string UiTipSubmit => _data.UiTipSubmit;

	public string UiTipLoadGame => _data.UiTipLoadGame;

	public string UiTipDel => _data.UiTipDel;

	public string UiTipLockSlot => _data.UiTipLockSlot;

	public string UiTipTidy => _data.UiTipTidy;

	public string UiTipDispose => _data.UiTipDispose;

	public string UiTipDestroyItem => _data.UiTipDestroyItem;

	public string UiTipSwitchLable => _data.UiTipSwitchLable;

	public string UiTipPutAll => _data.UiTipPutAll;

	public string UiTipPutMax => _data.UiTipPutMax;

	public string UiTipTakeOutAll => _data.UiTipTakeOutAll;

	public string UiTipTakeOutMax => _data.UiTipTakeOutMax;

	public string UiTipPutAllTap => _data.UiTipPutAllTap;

	public string UiTipTakeOutOne => _data.UiTipTakeOutOne;

	public string UiTipTakeOutHalf => _data.UiTipTakeOutHalf;

	public string UiTipTakeOutOneGamepad => _data.UiTipTakeOutOneGamepad;

	public string UiTipPut => _data.UiTipPut;

	public string UiTipTakeOut => _data.UiTipTakeOut;

	public string UiTipPutGamepad => _data.UiTipPutGamepad;

	public string UiTipTakeOutGamepad => _data.UiTipTakeOutGamepad;

	public string UiTipPickUpGamepad => _data.UiTipPickUpGamepad;

	public string UiTipQuickPutOne => _data.UiTipQuickPutOne;

	public string UiTipQuickPutAll => _data.UiTipQuickPutAll;

	public string UiTipQuickTakeOne => _data.UiTipQuickTakeOne;

	public string UiTipQuickTakeAll => _data.UiTipQuickTakeAll;

	public string UiTipQuickEquipment => _data.UiTipQuickEquipment;

	public string UiTipSwitchBox => _data.UiTipSwitchBox;

	public string UiTipSubOne => _data.UiTipSubOne;

	public string UiTipAddOne => _data.UiTipAddOne;

	public string UiTipSubTen => _data.UiTipSubTen;

	public string UiTipAddTen => _data.UiTipAddTen;

	public string UiTipSetMin => _data.UiTipSetMin;

	public string UiTipSetMax => _data.UiTipSetMax;

	public string UiTipBroadcast => _data.UiTipBroadcast;

	public string UiTipMakeOne => _data.UiTipMakeOne;

	public string UiTipMakeAll => _data.UiTipMakeAll;

	public string UiTipBuyOne => _data.UiTipBuyOne;

	public string UiTipBuyAllGamepad => _data.UiTipBuyAllGamepad;

	public string UiTipSellOne => _data.UiTipSellOne;

	public string UiTipSellAll => _data.UiTipSellAll;

	public string UiTipPrevFilter => _data.UiTipPrevFilter;

	public string UiTipNextFilter => _data.UiTipNextFilter;

	public string UiTipCapture => _data.UiTipCapture;

	public string UiTipCollection => _data.UiTipCollection;

	public string UiTipHistory => _data.UiTipHistory;

	public string UiTipChangeName => _data.UiTipChangeName;

	public string UiTipTakeOutSelected => _data.UiTipTakeOutSelected;

	public string UiTipPutOneSelected => _data.UiTipPutOneSelected;

	public string UiTipPutAllSelected => _data.UiTipPutAllSelected;

	public string UiTipAddMemo => _data.UiTipAddMemo;

	public string UiTipChangeMemo => _data.UiTipChangeMemo;

	public string UiTipDelMemo => _data.UiTipDelMemo;

	public string UiTipSwitchMapSize => _data.UiTipSwitchMapSize;

	public string UiTipMapCentered => _data.UiTipMapCentered;

	public string UiTipEmailRecycle => _data.UiTipEmailRecycle;

	public string UiTipEmailDefault => _data.UiTipEmailDefault;

	public string UiTipTechtreePointFocus => _data.UiTipTechtreePointFocus;

	public string UiTipTechtreeFocus => _data.UiTipTechtreeFocus;

	public string UiTipBatteryLow => _data.UiTipBatteryLow;

	public string UiTipErrTakeBoat => _data.UiTipErrTakeBoat;

	public string UiOperationTalk => _data.UiOperationTalk;

	public string UiOperationTalkUnknown => _data.UiOperationTalkUnknown;

	public string UiOperationInteract => _data.UiOperationInteract;

	public string UiOperationSleep => _data.UiOperationSleep;

	public string UiOperationPick => _data.UiOperationPick;

	public string UiOperationWell => _data.UiOperationWell;

	public string UiOperationHarvest => _data.UiOperationHarvest;

	public string UiOperationClear => _data.UiOperationClear;

	public string UiOperationOpen => _data.UiOperationOpen;

	public string UiOperationOpenDoor => _data.UiOperationOpenDoor;

	public string UiOperationCloseDoor => _data.UiOperationCloseDoor;

	public string UiOperationEnter => _data.UiOperationEnter;

	public string UiOperationEnterFormat => _data.UiOperationEnterFormat;

	public string UiOperationExit => _data.UiOperationExit;

	public string UiOperationView => _data.UiOperationView;

	public string UiOperationUse => _data.UiOperationUse;

	public string UiOperationSit => _data.UiOperationSit;

	public string UiOperationDisplay => _data.UiOperationDisplay;

	public string UiOperationTakeoff => _data.UiOperationTakeoff;

	public string UiOperationDump => _data.UiOperationDump;

	public string UiOperationDisembark => _data.UiOperationDisembark;

	public string UiOperationCallBoat => _data.UiOperationCallBoat;

	public string UiOperationShower => _data.UiOperationShower;

	public string UiOperationOpenBox => _data.UiOperationOpenBox;

	public string UiOperationStorageShelf => _data.UiOperationStorageShelf;

	public string UiOperationFuelIn => _data.UiOperationFuelIn;

	public string UiOperationClose => _data.UiOperationClose;

	public string UiOperationStart => _data.UiOperationStart;

	public string UiOperationFill => _data.UiOperationFill;

	public string UiOperationFondle => _data.UiOperationFondle;

	public string UiOperationStartSomething => _data.UiOperationStartSomething;

	public string UiOperationUseSomething => _data.UiOperationUseSomething;

	public string UiOperationErrNotSeed => _data.UiOperationErrNotSeed;

	public string UiOperationErrInvalidPlantbasin => _data.UiOperationErrInvalidPlantbasin;

	public string UiOperationErrInvalidSeason => _data.UiOperationErrInvalidSeason;

	public string UiOperationErrLackOfAsset => _data.UiOperationErrLackOfAsset;

	public string UiOperationErrLackOfEnergy => _data.UiOperationErrLackOfEnergy;

	public string UiOperationErrCannotPlantTree => _data.UiOperationErrCannotPlantTree;

	public string UiOperationErrCannotPlantOnGround => _data.UiOperationErrCannotPlantOnGround;

	public string UiOperationErrEmptyDrone => _data.UiOperationErrEmptyDrone;

	public string UiOperationErrWellFull => _data.UiOperationErrWellFull;

	public string UiOperationErrWaterFull => _data.UiOperationErrWaterFull;

	public string UiOperationErrRunoutWater => _data.UiOperationErrRunoutWater;

	public string UiOperationErrRunoutWaterAround => _data.UiOperationErrRunoutWaterAround;

	public string UiOperationErrEquipmentCorroded => _data.UiOperationErrEquipmentCorroded;

	public string UiOperationErrLowToolLevel => _data.UiOperationErrLowToolLevel;

	public string UiOperationErrFullPlasticFilm => _data.UiOperationErrFullPlasticFilm;

	public string UiOperationErrCannotFertilizer => _data.UiOperationErrCannotFertilizer;

	public string UiOperationErrDroneFullBattery => _data.UiOperationErrDroneFullBattery;

	public string UiOperationErrWaterEvaporated => _data.UiOperationErrWaterEvaporated;

	public string UiOperationErrFailToPlaceBox => _data.UiOperationErrFailToPlaceBox;

	public string UiOperationErrCannotDisplay => _data.UiOperationErrCannotDisplay;

	public string UiOperationErrCannotUseIfRiding => _data.UiOperationErrCannotUseIfRiding;

	public string UiOperationErrCannotCallMotor => _data.UiOperationErrCannotCallMotor;

	public string UiOperationErrSwordCannotAutofire => _data.UiOperationErrSwordCannotAutofire;

	public string UiOperationErrLowPower => _data.UiOperationErrLowPower;

	public string UiOperationErrCooling => _data.UiOperationErrCooling;

	public string UiOperationErrBarrelFull => _data.UiOperationErrBarrelFull;

	public string UiOperationErrFuelFull => _data.UiOperationErrFuelFull;

	public string UiOperationErrNotFeeds => _data.UiOperationErrNotFeeds;

	public string UiOperationErrFeederFull => _data.UiOperationErrFeederFull;

	public string UiOperationErrSlotFull => _data.UiOperationErrSlotFull;

	public string UiOperationErrNoSuitableEquipments => _data.UiOperationErrNoSuitableEquipments;

	public string UiStorageShelfFull => _data.UiStorageShelfFull;

	public string UiOperationWeatherHasChanged => _data.UiOperationWeatherHasChanged;

	public string UiOperationErrFuel => _data.UiOperationErrFuel;

	public string UiOperationGiftItemFail => _data.UiOperationGiftItemFail;

	public string UiOperationMakeEquipment => _data.UiOperationMakeEquipment;

	public string UiOperationSell => _data.UiOperationSell;

	public string UiOperationSwitchAutoFire => _data.UiOperationSwitchAutoFire;

	public string UiOperationSwitchManualFire => _data.UiOperationSwitchManualFire;

	public string UiOperationCollect => _data.UiOperationCollect;

	public string UiOperationRide => _data.UiOperationRide;

	public string UiOperationNoAnimalBuilding => _data.UiOperationNoAnimalBuilding;

	public string UiOperationErrNoPlantBasin => _data.UiOperationErrNoPlantBasin;

	public string UiOperationErrNoBuildingHere => _data.UiOperationErrNoBuildingHere;

	public string UiOperationErrInvalidWallpaper => _data.UiOperationErrInvalidWallpaper;

	public string UiOperationErrSameWallpaper => _data.UiOperationErrSameWallpaper;

	public string UiOperationErrShouldInhouse => _data.UiOperationErrShouldInhouse;

	public string UiOperationErrShouldOutside => _data.UiOperationErrShouldOutside;

	public string StorePlayerMoneyNotEnough => _data.StorePlayerMoneyNotEnough;

	public string StoreStoreMoneyNotEnough => _data.StoreStoreMoneyNotEnough;

	public string StoreItemNotSaleable => _data.StoreItemNotSaleable;

	public string StoreItemSoldOut => _data.StoreItemSoldOut;

	public string StoreLackOfAsset => _data.StoreLackOfAsset;

	public string StoreUpgradePrice => _data.StoreUpgradePrice;

	public string StoreUpgradeConfirm => _data.StoreUpgradeConfirm;

	public string StoreSoldOutIcon => _data.StoreSoldOutIcon;

	public string StoreUpgradeEmptyInfo => _data.StoreUpgradeEmptyInfo;

	public string StoreItemNotSaleableComment => _data.StoreItemNotSaleableComment;

	public string StoreQuantitySubmitSelling => _data.StoreQuantitySubmitSelling;

	public string StoreQuantitySubmitCurrentMoneySelling => _data.StoreQuantitySubmitCurrentMoneySelling;

	public string StoreQuantitySubmitTotalMoneySelling => _data.StoreQuantitySubmitTotalMoneySelling;

	public string StoreQuantitySubmitBuying => _data.StoreQuantitySubmitBuying;

	public string StoreQuantitySubmitCurrentMoneyBuying => _data.StoreQuantitySubmitCurrentMoneyBuying;

	public string StoreQuantitySubmitTotalMoneyBuying => _data.StoreQuantitySubmitTotalMoneyBuying;

	public string StoreQuantitySubmitCountInBack => _data.StoreQuantitySubmitCountInBack;

	public string StoreQuantitySubmitUnitPrice => _data.StoreQuantitySubmitUnitPrice;

	public string StoreItemTipPlantbasinLocked => _data.StoreItemTipPlantbasinLocked;

	public string FarmbuilderErrBuildSystemNotSupport => _data.FarmbuilderErrBuildSystemNotSupport;

	public string FarmbuilderErrLackOfAsset => _data.FarmbuilderErrLackOfAsset;

	public string FarmbuilderErrIndoorEquipment => _data.FarmbuilderErrIndoorEquipment;

	public string FarmbuilderErrOutdoorEquipment => _data.FarmbuilderErrOutdoorEquipment;

	public string FarmbuilderErrInvalidPosition => _data.FarmbuilderErrInvalidPosition;

	public string FarmbuilderErrOutOfRange => _data.FarmbuilderErrOutOfRange;

	public string FarmbuilderErrEquipmentAreaNotEmpty => _data.FarmbuilderErrEquipmentAreaNotEmpty;

	public string FarmbuilderErrPlatformOccupied => _data.FarmbuilderErrPlatformOccupied;

	public string FarmbuilderQuestionRemove => _data.FarmbuilderQuestionRemove;

	public string FarmbuilderQuestionRemoveReturn => _data.FarmbuilderQuestionRemoveReturn;

	public string FarmbuilderErrEquipmentOccupied => _data.FarmbuilderErrEquipmentOccupied;

	public string FarmbuilderErrPlantbasinTreeOccupied => _data.FarmbuilderErrPlantbasinTreeOccupied;

	public string FarmbuilderErrParkingApronOccupied => _data.FarmbuilderErrParkingApronOccupied;

	public string FarmbuilderErrAutomateBotOccupied => _data.FarmbuilderErrAutomateBotOccupied;

	public string FarmbuilderErrShowCaseOccupied => _data.FarmbuilderErrShowCaseOccupied;

	public string FarmbuilderErrPlatformRemove => _data.FarmbuilderErrPlatformRemove;

	public string FarmbuilderErrEquipmentHideDoor => _data.FarmbuilderErrEquipmentHideDoor;

	public string FarmbuilderErrBuildingOccupied => _data.FarmbuilderErrBuildingOccupied;

	public string FarmbuilderErrBuildingOccupiedAnimal => _data.FarmbuilderErrBuildingOccupiedAnimal;

	public string FarmbuilderErrBuildingAreaNotEmpty => _data.FarmbuilderErrBuildingAreaNotEmpty;

	public string FarmbuilderErrBuildingInvalidHeight => _data.FarmbuilderErrBuildingInvalidHeight;

	public string FarmbuilderErrBuildingInvalidCoveredRatio => _data.FarmbuilderErrBuildingInvalidCoveredRatio;

	public string FarmbuilderErrBuildingCanNotRemove => _data.FarmbuilderErrBuildingCanNotRemove;

	public string FarmbuilderErrBuildingInvalidOtherNoSupport => _data.FarmbuilderErrBuildingInvalidOtherNoSupport;

	public string FarmbuilderErrBuildingOccupiedBuilding => _data.FarmbuilderErrBuildingOccupiedBuilding;

	public string FarmbuilderErrBuildingInvalidDoorBeHidden => _data.FarmbuilderErrBuildingInvalidDoorBeHidden;

	public string FarmbuilderErrBuildingOccupiedCeiling => _data.FarmbuilderErrBuildingOccupiedCeiling;

	public string FarmbuilderErrBuildingOccupiedDoor => _data.FarmbuilderErrBuildingOccupiedDoor;

	public string FarmbuilderErrBuildingOccupiedCeilingPt => _data.FarmbuilderErrBuildingOccupiedCeilingPt;

	public string FarmbuilderErrBuildingGroundInvalid => _data.FarmbuilderErrBuildingGroundInvalid;

	public string FarmbuilderErrPlatformWidth => _data.FarmbuilderErrPlatformWidth;

	public string FarmbuilderErrPlatformHeightMax => _data.FarmbuilderErrPlatformHeightMax;

	public string FarmbuilderErrEquipmentLimited => _data.FarmbuilderErrEquipmentLimited;

	public string TechtreePanelTitle => _data.TechtreePanelTitle;

	public string TechtreeNodeLackOfPoints => _data.TechtreeNodeLackOfPoints;

	public string TechtreeNodeNotAvaiable => _data.TechtreeNodeNotAvaiable;

	public string TechtreeNodeUnlocked => _data.TechtreeNodeUnlocked;

	public string TechtreeNodeUnopen => _data.TechtreeNodeUnopen;

	public string TechtreeNodeUnlockConfirm => _data.TechtreeNodeUnlockConfirm;

	public string TechtreeNodeBuildingHealth => _data.TechtreeNodeBuildingHealth;

	public string TechtreeNodeEquipmentElectronic => _data.TechtreeNodeEquipmentElectronic;

	public string TechtreeProgressInfo => _data.TechtreeProgressInfo;

	public string TechtreeMaxLevel => _data.TechtreeMaxLevel;

	public string TechtreeNodeHint => _data.TechtreeNodeHint;

	public string MissionUpdate => _data.MissionUpdate;

	public string MissionComplete => _data.MissionComplete;

	public string MissionCompleteSendEmail => _data.MissionCompleteSendEmail;

	public string MissionPanelTitle => _data.MissionPanelTitle;

	public string MissionPanelEmpty => _data.MissionPanelEmpty;

	public string UiMissionTimeLimit => _data.UiMissionTimeLimit;

	public string UiMissionNpcPosition => _data.UiMissionNpcPosition;

	public string SeedPanelTitle => _data.SeedPanelTitle;

	public string SeedNodeAlreadyUnlock => _data.SeedNodeAlreadyUnlock;

	public string SeedNodeSucceedUnlock => _data.SeedNodeSucceedUnlock;

	public string BuildingPanelTitle => _data.BuildingPanelTitle;

	public string BuildingPanelCoverSizeDescription => _data.BuildingPanelCoverSizeDescription;

	public string BuildingPanelInnerSizeDescription => _data.BuildingPanelInnerSizeDescription;

	public string BuildingPanelAnimalCapacityDescription => _data.BuildingPanelAnimalCapacityDescription;

	public string BuildingPanelSizeDescription => _data.BuildingPanelSizeDescription;

	public string BuildingPanelEmpty => _data.BuildingPanelEmpty;

	public string BuildingPanelStartBuild => _data.BuildingPanelStartBuild;

	public string BuildingPanelMaterialNotEnough => _data.BuildingPanelMaterialNotEnough;

	public string BuildingPanelMoneyNotEnough => _data.BuildingPanelMoneyNotEnough;

	public string BuildingPanelInputBuildingName => _data.BuildingPanelInputBuildingName;

	public string EquipmentPanelEmpty => _data.EquipmentPanelEmpty;

	public string EquipmentPanelStartBuild => _data.EquipmentPanelStartBuild;

	public string EquipmentPanelListName => _data.EquipmentPanelListName;

	public string EquipmentViewerEmpty => _data.EquipmentViewerEmpty;

	public string EquipmentPanelLatelyEmpty => _data.EquipmentPanelLatelyEmpty;

	public string EquipmentPanelMakeComplete => _data.EquipmentPanelMakeComplete;

	public string EquipmentPanelHide => _data.EquipmentPanelHide;

	public string EquipmentPanelJump => _data.EquipmentPanelJump;

	public string EquipmentPanelUnlockTip => _data.EquipmentPanelUnlockTip;

	public string EquipmentPanelElectronicTip => _data.EquipmentPanelElectronicTip;

	public string EquipmentPanelBatteryTip => _data.EquipmentPanelBatteryTip;

	public string EquipmentPanelApplianceTip => _data.EquipmentPanelApplianceTip;

	public string EquipmentPanelGeneratorTip => _data.EquipmentPanelGeneratorTip;

	public string UiEmailTitle => _data.UiEmailTitle;

	public string UiEmailEmpty => _data.UiEmailEmpty;

	public string UiEmailAcceptMission => _data.UiEmailAcceptMission;

	public string UiEmailAlreadyAccepted => _data.UiEmailAlreadyAccepted;

	public string UiEmailReciveItem => _data.UiEmailReciveItem;

	public string UiEmailAlreadyRecived => _data.UiEmailAlreadyRecived;

	public string UiEmailRecycleErr => _data.UiEmailRecycleErr;

	public string RecipePanelEmpty => _data.RecipePanelEmpty;

	public string RecipePanelStartBuild => _data.RecipePanelStartBuild;

	public string RecipePanelTimeInfo => _data.RecipePanelTimeInfo;

	public string RecipePanelAlreadyWorking => _data.RecipePanelAlreadyWorking;

	public string RecipePanelTitle => _data.RecipePanelTitle;

	public string RecipePanelTask => _data.RecipePanelTask;

	public string RecipePanelRestTime => _data.RecipePanelRestTime;

	public string RecipePanelMaxCraftCount => _data.RecipePanelMaxCraftCount;

	public string RecipePanelLimitUp => _data.RecipePanelLimitUp;

	public string RecipePanelNotCookable => _data.RecipePanelNotCookable;

	public string RecipePanelNoMaterial => _data.RecipePanelNoMaterial;

	public string RecipePanelConfirmWithTime => _data.RecipePanelConfirmWithTime;

	public string RecipePanelUnlockedRecipeComment => _data.RecipePanelUnlockedRecipeComment;

	public string RecipePanelUnlockedDishComment => _data.RecipePanelUnlockedDishComment;

	public string RecipePanelUnknowDishComment => _data.RecipePanelUnknowDishComment;

	public string RecipePanelConfirmPopBuffer => _data.RecipePanelConfirmPopBuffer;

	public string RecipePanelMaterialList => _data.RecipePanelMaterialList;

	public string RecipePanelExistItems => _data.RecipePanelExistItems;

	public string RecipePanelRandomGeneHint => _data.RecipePanelRandomGeneHint;

	public string PlatformPanelTitle => _data.PlatformPanelTitle;

	public string PlatformPanelEmpty => _data.PlatformPanelEmpty;

	public string PlatformPanelStartBuild => _data.PlatformPanelStartBuild;

	public string PlatformPanelCostPrefix => _data.PlatformPanelCostPrefix;

	public string ItemBuildingProtoError => _data.ItemBuildingProtoError;

	public string ItemEquipmentProtoError => _data.ItemEquipmentProtoError;

	public string ItemSendEmailOnOverflow => _data.ItemSendEmailOnOverflow;

	public string ItemSleepingBagConditionFailedMonster => _data.ItemSleepingBagConditionFailedMonster;

	public string ItemSleepingBagConditionFailedWater => _data.ItemSleepingBagConditionFailedWater;

	public string ItemRescuePagerConditionFailed => _data.ItemRescuePagerConditionFailed;

	public string ItemTipPrice => _data.ItemTipPrice;

	public string ItemTipBasicsPrice => _data.ItemTipBasicsPrice;

	public string ItemTipMoneyUnit => _data.ItemTipMoneyUnit;

	public string ItemChipAttackIncrease => _data.ItemChipAttackIncrease;

	public string ItemChipAttackIncreaseFixed => _data.ItemChipAttackIncreaseFixed;

	public string ItemChipCriticalRateIncrease => _data.ItemChipCriticalRateIncrease;

	public string ItemChipAttackSpeedIncrease => _data.ItemChipAttackSpeedIncrease;

	public string ItemChipAccuracyIncrease => _data.ItemChipAccuracyIncrease;

	public string ItemChipPowerCostDecrease => _data.ItemChipPowerCostDecrease;

	public string ItemChipAttackDistanceIncrease => _data.ItemChipAttackDistanceIncrease;

	public string ItemChipMoveSpeedIncrease => _data.ItemChipMoveSpeedIncrease;

	public string ItemChipClipCapacityAddition => _data.ItemChipClipCapacityAddition;

	public string ItemChipReloadDurationDecrease => _data.ItemChipReloadDurationDecrease;

	public string ItemEngineMoveSpeedIncrease => _data.ItemEngineMoveSpeedIncrease;

	public string ItemEnginePowerCapacityIncrease => _data.ItemEnginePowerCapacityIncrease;

	public string ItemEnginePowerRecvIncrease => _data.ItemEnginePowerRecvIncrease;

	public string ItemStructureMoveSpeedIncrease => _data.ItemStructureMoveSpeedIncrease;

	public string ItemStructurePowerCapacity => _data.ItemStructurePowerCapacity;

	public string ItemStructurePowerRecv => _data.ItemStructurePowerRecv;

	public string ItemMaxDurability => _data.ItemMaxDurability;

	public string ItemTitleInfoFormat => _data.ItemTitleInfoFormat;

	public string ItemConfirmUse => _data.ItemConfirmUse;

	public string ItemWaterCanArea => _data.ItemWaterCanArea;

	public string ItemWaterCanEndlessWater => _data.ItemWaterCanEndlessWater;

	public string ItemGeneDescFormat => _data.ItemGeneDescFormat;

	public string ItemSeedCloned => _data.ItemSeedCloned;

	public string ItemWpIsAvailableFor => _data.ItemWpIsAvailableFor;

	public string ItemWpAvailableAll => _data.ItemWpAvailableAll;

	public string ItemFishFryTitleFormat => _data.ItemFishFryTitleFormat;

	public string ItemToolLevelFormat => _data.ItemToolLevelFormat;

	public string ItemPatchValueFormat => _data.ItemPatchValueFormat;

	public string ItemBoxValueFormat => _data.ItemBoxValueFormat;

	public string ItemFilmValueFormat => _data.ItemFilmValueFormat;

	public string ItemDroneWeaponValueFormat => _data.ItemDroneWeaponValueFormat;

	public string ItemContainerConditionFormat => _data.ItemContainerConditionFormat;

	public string ItemMoneyTitle => _data.ItemMoneyTitle;

	public string ItemMoneyDesc => _data.ItemMoneyDesc;

	public string ItemMoneyType => _data.ItemMoneyType;

	public string UiSystemExitGame => _data.UiSystemExitGame;

	public string UiSystemReturnHomePage => _data.UiSystemReturnHomePage;

	public string UiSystemConfirmHome => _data.UiSystemConfirmHome;

	public string UiSystemConfirmExit => _data.UiSystemConfirmExit;

	public string RewardInfoBuildingUnlock => _data.RewardInfoBuildingUnlock;

	public string RewardInfoBusStationUnlock => _data.RewardInfoBusStationUnlock;

	public string RewardInfoEquipmentUnlock => _data.RewardInfoEquipmentUnlock;

	public string RewardInfoPlatformUnlock => _data.RewardInfoPlatformUnlock;

	public string RewardInfoRecipeUnlock => _data.RewardInfoRecipeUnlock;

	public string RewardInfoRecipeUnlockHint => _data.RewardInfoRecipeUnlockHint;

	public string RewardInfoItem => _data.RewardInfoItem;

	public string RewardInfoGold => _data.RewardInfoGold;

	public string RewardInfoFavorabilityLv1 => _data.RewardInfoFavorabilityLv1;

	public string RewardInfoFavorabilityLv2 => _data.RewardInfoFavorabilityLv2;

	public string RewardInfoFavorabilityLv3 => _data.RewardInfoFavorabilityLv3;

	public string DropoffBoxNoGoods => _data.DropoffBoxNoGoods;

	public string DropoffBoxNoDrone => _data.DropoffBoxNoDrone;

	public string DropoffBoxTotalMoney => _data.DropoffBoxTotalMoney;

	public string DropoffBoxPriceIncreased => _data.DropoffBoxPriceIncreased;

	public string DropoffBoxLaunchDrone => _data.DropoffBoxLaunchDrone;

	public string DropoffBoxConfirmLauch => _data.DropoffBoxConfirmLauch;

	public string AutomateBotPanelStateIdle => _data.AutomateBotPanelStateIdle;

	public string AutomateBotPanelStateCharge => _data.AutomateBotPanelStateCharge;

	public string AutomateBotPanelStatePause => _data.AutomateBotPanelStatePause;

	public string AutomateBotPanelStateWorking => _data.AutomateBotPanelStateWorking;

	public string AutomateBotPanelCfgEmpty => _data.AutomateBotPanelCfgEmpty;

	public string AutomateBotPanelRecipeEmpty => _data.AutomateBotPanelRecipeEmpty;

	public string AutomateBotPanelDateEmpty => _data.AutomateBotPanelDateEmpty;

	public string AutomateBotPanelRecipeType => _data.AutomateBotPanelRecipeType;

	public string AutomateBotPanelRecipeSubType => _data.AutomateBotPanelRecipeSubType;

	public string AutomateBotPanelAutoFertilizer => _data.AutomateBotPanelAutoFertilizer;

	public string AutomateBotPanelAutoProtect => _data.AutomateBotPanelAutoProtect;

	public string AutomateBotPanelEnergyType => _data.AutomateBotPanelEnergyType;

	public string AutomateBotPanelItemError => _data.AutomateBotPanelItemError;

	public string BoardMissionPanelTitle => _data.BoardMissionPanelTitle;

	public string BoardMissionUrgencyLow => _data.BoardMissionUrgencyLow;

	public string BoardMissionUrgencyMiddle => _data.BoardMissionUrgencyMiddle;

	public string BoardMissionUrgencyHigh => _data.BoardMissionUrgencyHigh;

	public string BoardMissionUrgencyNone => _data.BoardMissionUrgencyNone;

	public string BoardMissionAcceptFail => _data.BoardMissionAcceptFail;

	public string BoardMissionTitle => _data.BoardMissionTitle;

	public string BoardMissionLowLevel => _data.BoardMissionLowLevel;

	public string BoardMissionEmpty => _data.BoardMissionEmpty;

	public string BoardMissionOverdue => _data.BoardMissionOverdue;

	public string BoardMissionLv => _data.BoardMissionLv;

	public string BoardMissionExpTip => _data.BoardMissionExpTip;

	public string InventoryPanelCannotPutIn => _data.InventoryPanelCannotPutIn;

	public string InventoryPanelContainerFull => _data.InventoryPanelContainerFull;

	public string BoxPanelRestCount => _data.BoxPanelRestCount;

	public string BoxPanelUsedUpWarning => _data.BoxPanelUsedUpWarning;

	public string BoxPanelBroken => _data.BoxPanelBroken;

	public string BoxPanelAlreadyOpen => _data.BoxPanelAlreadyOpen;

	public string BoxPanelNoNeedRepair => _data.BoxPanelNoNeedRepair;

	public string BoxPanelNoRepairCost => _data.BoxPanelNoRepairCost;

	public string BoxPanelRepairInfo => _data.BoxPanelRepairInfo;

	public string InventoryPanelDungeonCaseTitle => _data.InventoryPanelDungeonCaseTitle;

	public string InventoryPanelBackpackTitle => _data.InventoryPanelBackpackTitle;

	public string FishTankPanelFeedQuantity => _data.FishTankPanelFeedQuantity;

	public string InventoryPanelSocketTitle => _data.InventoryPanelSocketTitle;

	public string InventoryPanelPutBoxFirst => _data.InventoryPanelPutBoxFirst;

	public string InventoryPanelInfoGeneIncubator => _data.InventoryPanelInfoGeneIncubator;

	public string InventoryPanelCapsuleTitle => _data.InventoryPanelCapsuleTitle;

	public string InventoryPanelInfoGeneReplicator => _data.InventoryPanelInfoGeneReplicator;

	public string InventoryPanelGeneReplicatorLocked => _data.InventoryPanelGeneReplicatorLocked;

	public string InventoryPanelInfoGeneSynthesizer => _data.InventoryPanelInfoGeneSynthesizer;

	public string InventoryPanelGeneSynthesizerLocked => _data.InventoryPanelGeneSynthesizerLocked;

	public string UiTipTimeTitle => _data.UiTipTimeTitle;

	public string UiTipTimeFormat => _data.UiTipTimeFormat;

	public string UiTipCurrentSeason => _data.UiTipCurrentSeason;

	public string UiTipOpenMenu => _data.UiTipOpenMenu;

	public string UiTipOpenMap => _data.UiTipOpenMap;

	public string UiTipShowMissionTip => _data.UiTipShowMissionTip;

	public string UiTipHideMissionTip => _data.UiTipHideMissionTip;

	public string UiTipNoMission => _data.UiTipNoMission;

	public string UiTipMissionShowDetails => _data.UiTipMissionShowDetails;

	public string UiTipHealthValue => _data.UiTipHealthValue;

	public string UiTipEnergyValue => _data.UiTipEnergyValue;

	public string UiTipCorrosionValue => _data.UiTipCorrosionValue;

	public string UiTipCurrentSpiritLe50 => _data.UiTipCurrentSpiritLe50;

	public string UiTipCurrentSpiritLe25 => _data.UiTipCurrentSpiritLe25;

	public string UiTipCurrentSpiritLe10 => _data.UiTipCurrentSpiritLe10;

	public string UiTipCurrentSpiritLe0 => _data.UiTipCurrentSpiritLe0;

	public string UiTipBuffDuration => _data.UiTipBuffDuration;

	public string UiTipDebuffDuration => _data.UiTipDebuffDuration;

	public string UiTipBuildHealth => _data.UiTipBuildHealth;

	public string UiTipElectricityGeneratorInfo => _data.UiTipElectricityGeneratorInfo;

	public string UiTipElectricityBatteryInfo => _data.UiTipElectricityBatteryInfo;

	public string UiTipElectricityStatusNone => _data.UiTipElectricityStatusNone;

	public string UiTipElectricityStatusLackOfGeneration => _data.UiTipElectricityStatusLackOfGeneration;

	public string UiTipElectricityStatusLackOfGenerationCostBattery => _data.UiTipElectricityStatusLackOfGenerationCostBattery;

	public string UiTipElectricityStatusBatterySaving => _data.UiTipElectricityStatusBatterySaving;

	public string UiTipElectricityStatusBatteryFull => _data.UiTipElectricityStatusBatteryFull;

	public string UiTipElectricityStatusPowerLoss => _data.UiTipElectricityStatusPowerLoss;

	public string UiTipSeedEnd => _data.UiTipSeedEnd;

	public string UiTipLoading => _data.UiTipLoading;

	public string DocumentTitleComputer => _data.DocumentTitleComputer;

	public string NpcDocumentTitle => _data.NpcDocumentTitle;

	public string ChipDocumentTitle => _data.ChipDocumentTitle;

	public string PlantDocumentTitle => _data.PlantDocumentTitle;

	public string DocumentEmpty => _data.DocumentEmpty;

	public string ChipDocumentTotle => _data.ChipDocumentTotle;

	public string GameDataPanelTitle => _data.GameDataPanelTitle;

	public string GameDataErrRead => _data.GameDataErrRead;

	public string GameDataTitle => _data.GameDataTitle;

	public string GameDataDelConfirm => _data.GameDataDelConfirm;

	public string GameDataDelSuccess => _data.GameDataDelSuccess;

	public string GameDataErrEmpty => _data.GameDataErrEmpty;

	public string GameDataSaving => _data.GameDataSaving;

	public string GameDataSaveFail => _data.GameDataSaveFail;

	public string GameDataSaveSuccessful => _data.GameDataSaveSuccessful;

	public string GameDataFull => _data.GameDataFull;

	public string GameDataDuplicateSucess => _data.GameDataDuplicateSucess;

	public string GameDataStart => _data.GameDataStart;

	public string GameDataLoad => _data.GameDataLoad;

	public string TeleportFail => _data.TeleportFail;

	public string TeleportConfirm => _data.TeleportConfirm;

	public string TeleportBuyTicket => _data.TeleportBuyTicket;

	public string TeleportErrMaterialNotEnough => _data.TeleportErrMaterialNotEnough;

	public string FactionMissionFinish => _data.FactionMissionFinish;

	public string FactionMissionCannotSubmit => _data.FactionMissionCannotSubmit;

	public string FactionMissionFinishSubmit => _data.FactionMissionFinishSubmit;

	public string FactionMissionMoneySubmit => _data.FactionMissionMoneySubmit;

	public string FactionMissionErrSubmit => _data.FactionMissionErrSubmit;

	public string FactionMissionLock => _data.FactionMissionLock;

	public string TreatyPortTitle => _data.TreatyPortTitle;

	public string TreatyPortRepairTime => _data.TreatyPortRepairTime;

	public string TreatyPortRecruitStats => _data.TreatyPortRecruitStats;

	public string TreatyPortPrincipal => _data.TreatyPortPrincipal;

	public string TreatyPortFactionMissionProgress => _data.TreatyPortFactionMissionProgress;

	public string TreatyPortFactionReputation => _data.TreatyPortFactionReputation;

	public string TreatyPortContactNpc => _data.TreatyPortContactNpc;

	public string TreatyPortFactionEnterTime => _data.TreatyPortFactionEnterTime;

	public string TreatyPortBroadcast => _data.TreatyPortBroadcast;

	public string TreatyPortFactionLock => _data.TreatyPortFactionLock;

	public string TreatyPortFactionUnopen => _data.TreatyPortFactionUnopen;

	public string TreatyPortRecruitHint => _data.TreatyPortRecruitHint;

	public string TreatyPortTimeFormat => _data.TreatyPortTimeFormat;

	public string TreatyPortFactionRefuse => _data.TreatyPortFactionRefuse;

	public string TreatyPortOffDutyHoursTip => _data.TreatyPortOffDutyHoursTip;

	public string TreatyPortInterviewTip => _data.TreatyPortInterviewTip;

	public string TreatyPortFactionSettledTip => _data.TreatyPortFactionSettledTip;

	public string TreatyPortNotContactedTip => _data.TreatyPortNotContactedTip;

	public string SettingPanelSaveSuccessful => _data.SettingPanelSaveSuccessful;

	public string SettingPanelOtherSaveSuccessful => _data.SettingPanelOtherSaveSuccessful;

	public string SettingPanelReset => _data.SettingPanelReset;

	public string SettingPanelResetConfirm => _data.SettingPanelResetConfirm;

	public string SettingPanelTitle => _data.SettingPanelTitle;

	public string SettingPanelExitConfirm => _data.SettingPanelExitConfirm;

	public string SettingPanelConflictHint => _data.SettingPanelConflictHint;

	public string SettingPanelResetToDefault => _data.SettingPanelResetToDefault;

	public string SettingPanelCanNotEdit => _data.SettingPanelCanNotEdit;

	public string SettingPanelCanNotRemove => _data.SettingPanelCanNotRemove;

	public string SettingPanelNoValidInput => _data.SettingPanelNoValidInput;

	public string SettingPanelAllowTracedataCollector => _data.SettingPanelAllowTracedataCollector;

	public string SettingPanelTracedataCollectorDesc => _data.SettingPanelTracedataCollectorDesc;

	public string SettingPanelDelete => _data.SettingPanelDelete;

	public string SettingPanelCanNotSaveByConflict => _data.SettingPanelCanNotSaveByConflict;

	public string SettingPanelNewDevice => _data.SettingPanelNewDevice;

	public string SettingPanelLoading => _data.SettingPanelLoading;

	public string EquipmentBarTimeLabel => _data.EquipmentBarTimeLabel;

	public string EquipmentBarTimeFormat => _data.EquipmentBarTimeFormat;

	public string EquipmentBarDroneNotEquip => _data.EquipmentBarDroneNotEquip;

	public string EquipmentBarMotorTitle => _data.EquipmentBarMotorTitle;

	public string EquipmentBarMotorLock => _data.EquipmentBarMotorLock;

	public string EquipmentBarHatTip => _data.EquipmentBarHatTip;

	public string EquipmentBarPositiveTip => _data.EquipmentBarPositiveTip;

	public string EquipmentBarPassive1Tip => _data.EquipmentBarPassive1Tip;

	public string EquipmentBarPassive2Tip => _data.EquipmentBarPassive2Tip;

	public string EquipmentBarDroneTip => _data.EquipmentBarDroneTip;

	public string EquipmentBarSkillLock => _data.EquipmentBarSkillLock;

	public string EquipmentBarDoubleJumpDesc => _data.EquipmentBarDoubleJumpDesc;

	public string EquipmentBarSprintDesc => _data.EquipmentBarSprintDesc;

	public string AbilityDoubleJumpUnlockTip => _data.AbilityDoubleJumpUnlockTip;

	public string AbilitySprintUnlockTip => _data.AbilitySprintUnlockTip;

	public string EquipmentBarBackpackFull => _data.EquipmentBarBackpackFull;

	public string EquipmentSkillPrefix => _data.EquipmentSkillPrefix;

	public string EquipmentDefensePrefix => _data.EquipmentDefensePrefix;

	public string UiTipHomepageStartGame => _data.UiTipHomepageStartGame;

	public string UiTipHomepageChangelog => _data.UiTipHomepageChangelog;

	public string UiTipHomepageSettings => _data.UiTipHomepageSettings;

	public string UiTipHomepageMods => _data.UiTipHomepageMods;

	public string UiTipHomepageDeveloperList => _data.UiTipHomepageDeveloperList;

	public string UiTipHomepageExitGame => _data.UiTipHomepageExitGame;

	public string UiTipParkingApronLocked => _data.UiTipParkingApronLocked;

	public string UiTipResolving => _data.UiTipResolving;

	public string UiTipShredderMoney => _data.UiTipShredderMoney;

	public string UiTipErrShredderEmpty => _data.UiTipErrShredderEmpty;

	public string UiTipAirWall => _data.UiTipAirWall;

	public string UiTipNotAvailableToMotor => _data.UiTipNotAvailableToMotor;

	public string CollectionPanelItemLabel => _data.CollectionPanelItemLabel;

	public string CollectionPanelItemRecipeTime => _data.CollectionPanelItemRecipeTime;

	public string CollectionPanelItemRecipeEmpty => _data.CollectionPanelItemRecipeEmpty;

	public string CollectionPanelItemUnknown => _data.CollectionPanelItemUnknown;

	public string CollectionPanelItemSource => _data.CollectionPanelItemSource;

	public string CollectionPanelNpcAddress => _data.CollectionPanelNpcAddress;

	public string CollectionPanelNpcLikeRecord => _data.CollectionPanelNpcLikeRecord;

	public string CollectionPanelNpcLikeNone => _data.CollectionPanelNpcLikeNone;

	public string CollectionPanelNpcLikingLock => _data.CollectionPanelNpcLikingLock;

	public string CollectionPanelNpcLikingLvLock => _data.CollectionPanelNpcLikingLvLock;

	public string CollectionPanelNpcContentTitle => _data.CollectionPanelNpcContentTitle;

	public string CollectionPanelNpcContentLockTip => _data.CollectionPanelNpcContentLockTip;

	public string CollectionPanelNpcContentEnd => _data.CollectionPanelNpcContentEnd;

	public string CollectionPanelMonsterUnknown => _data.CollectionPanelMonsterUnknown;

	public string CollectionPanelMonsterHabitat => _data.CollectionPanelMonsterHabitat;

	public string CollectionPanelMonsterDropText => _data.CollectionPanelMonsterDropText;

	public string CollectionPanelMonsterOrganism => _data.CollectionPanelMonsterOrganism;

	public string CollectionPanelMonsterMachinery => _data.CollectionPanelMonsterMachinery;

	public string CollectionPanelMonsterBoss => _data.CollectionPanelMonsterBoss;

	public string CollectionPanelMonsterContentTitle => _data.CollectionPanelMonsterContentTitle;

	public string CollectionPanelMonsterContentLockTip => _data.CollectionPanelMonsterContentLockTip;

	public string CollectionPanelDocumentLabel => _data.CollectionPanelDocumentLabel;

	public string CollectionPanelAnimalPossess => _data.CollectionPanelAnimalPossess;

	public string CollectionPanelAnimalBreed => _data.CollectionPanelAnimalBreed;

	public string CollectionPanelAnimalProductText => _data.CollectionPanelAnimalProductText;

	public string CollectionPanelAnimalContentLockBringUp => _data.CollectionPanelAnimalContentLockBringUp;

	public string CollectionPanelAnimalContentLockBreed => _data.CollectionPanelAnimalContentLockBreed;

	public string CollectionPanelAnimalContentLockProduct => _data.CollectionPanelAnimalContentLockProduct;

	public string CollectionPanelFishCatch => _data.CollectionPanelFishCatch;

	public string CollectionPanelFishBaitText => _data.CollectionPanelFishBaitText;

	public string CollectionPanelFishPlace => _data.CollectionPanelFishPlace;

	public string CollectionPanelFishMonth => _data.CollectionPanelFishMonth;

	public string CollectionPanelFishWeather => _data.CollectionPanelFishWeather;

	public string CollectionPanelFishWeatherNone => _data.CollectionPanelFishWeatherNone;

	public string CollectionPanelFishContentLockTip => _data.CollectionPanelFishContentLockTip;

	public string CollectionPanelResourceCollect => _data.CollectionPanelResourceCollect;

	public string CollectionPanelResourceGrowthPeriod => _data.CollectionPanelResourceGrowthPeriod;

	public string CollectionPanelResourceYearRoundGrowth => _data.CollectionPanelResourceYearRoundGrowth;

	public string CollectionPanelResourceDropTitle => _data.CollectionPanelResourceDropTitle;

	public string CollectionPanelResourceContentLockTip => _data.CollectionPanelResourceContentLockTip;

	public string BuilderPanelLabelBuilding => _data.BuilderPanelLabelBuilding;

	public string BuilderPanelLabelEquipment => _data.BuilderPanelLabelEquipment;

	public string BuilderPanelLabelPlatform => _data.BuilderPanelLabelPlatform;

	public string BuilderPanelSwitchTerrainLayer => _data.BuilderPanelSwitchTerrainLayer;

	public string BuilderActionSelectedContent => _data.BuilderActionSelectedContent;

	public string BuilderActionUndo => _data.BuilderActionUndo;

	public string BuilderActionTurn => _data.BuilderActionTurn;

	public string BuilderActionDismantle => _data.BuilderActionDismantle;

	public string BuilderActionBuildingDismantle => _data.BuilderActionBuildingDismantle;

	public string BuilderActionBuildingStorage => _data.BuilderActionBuildingStorage;

	public string BuilderActionMoveCamera => _data.BuilderActionMoveCamera;

	public string BuilderActionMove => _data.BuilderActionMove;

	public string BuilderActionSelectedItem => _data.BuilderActionSelectedItem;

	public string BuilderActionPreciseMovement => _data.BuilderActionPreciseMovement;

	public string BuilderActionSwitchPrecise => _data.BuilderActionSwitchPrecise;

	public string BuilderActionSwitchBackpack => _data.BuilderActionSwitchBackpack;

	public string BuilderActionRollingBackpack => _data.BuilderActionRollingBackpack;

	public string BuilderActionRollingItem => _data.BuilderActionRollingItem;

	public string BuilderActionToggleBackpack => _data.BuilderActionToggleBackpack;

	public string BuilderActionToggleBackpackGamepad => _data.BuilderActionToggleBackpackGamepad;

	public string BuilderPanelExit => _data.BuilderPanelExit;

	public string BuilderPanelUndoGamepad => _data.BuilderPanelUndoGamepad;

	public string BuilderPanelExpandBuildingList => _data.BuilderPanelExpandBuildingList;

	public string BuilderPanellFoldBuildingList => _data.BuilderPanellFoldBuildingList;

	public string BuilderPanelFinish => _data.BuilderPanelFinish;

	public string BuilderDismantleBuildingErr => _data.BuilderDismantleBuildingErr;

	public string BuilderBuilderConstruct => _data.BuilderBuilderConstruct;

	public string BuilderPanelTempBuildingFull => _data.BuilderPanelTempBuildingFull;

	public string BuilderExitErrAnimal => _data.BuilderExitErrAnimal;

	public string BuilderExitErrOccupied => _data.BuilderExitErrOccupied;

	public string BuilderExitErrSoleBuilding => _data.BuilderExitErrSoleBuilding;

	public string BuilderExitConfirm => _data.BuilderExitConfirm;

	public string UiEnvOptimizerPanelTitle => _data.UiEnvOptimizerPanelTitle;

	public string UiEnvOptimizerConsoleTitle => _data.UiEnvOptimizerConsoleTitle;

	public string UiEnvOptimizerOverview => _data.UiEnvOptimizerOverview;

	public string UiEnvOptimizerButtonNoEnergy => _data.UiEnvOptimizerButtonNoEnergy;

	public string UiEnvOptimizerTipNoEnergy => _data.UiEnvOptimizerTipNoEnergy;

	public string UiEnvOptimizerButtonNoComponent => _data.UiEnvOptimizerButtonNoComponent;

	public string UiEnvOptimizerTipNoComponent => _data.UiEnvOptimizerTipNoComponent;

	public string UiEnvOptimizerButtonValid => _data.UiEnvOptimizerButtonValid;

	public string UiEnvOptimizerButtonNotValid => _data.UiEnvOptimizerButtonNotValid;

	public string UiEnvOptimizerCheckSuccess => _data.UiEnvOptimizerCheckSuccess;

	public string UiEnvOptimizerDateInfo => _data.UiEnvOptimizerDateInfo;

	public string UiEnvOptimizerSlotTitle => _data.UiEnvOptimizerSlotTitle;

	public string UiEnvOptimizerComponentAlreadyActive => _data.UiEnvOptimizerComponentAlreadyActive;

	public string UiEnvOptimizerInRecognition => _data.UiEnvOptimizerInRecognition;

	public string UiEnvOptimizerLoadingData => _data.UiEnvOptimizerLoadingData;

	public string UiEnvOptimizerActiveSuccess => _data.UiEnvOptimizerActiveSuccess;

	public string AnimalInvalidBuilding => _data.AnimalInvalidBuilding;

	public string AnimalInvalidRoom => _data.AnimalInvalidRoom;

	public string AnimalFullBuilding => _data.AnimalFullBuilding;

	public string AnimalInputAnimalName => _data.AnimalInputAnimalName;

	public string AnimalPackageIsFull => _data.AnimalPackageIsFull;

	public string AnimalHasEscaped => _data.AnimalHasEscaped;

	public string AnimalNoAnimal => _data.AnimalNoAnimal;

	public string AnimalInUse => _data.AnimalInUse;

	public string CalendarPanelYearTitle => _data.CalendarPanelYearTitle;

	public string CalendarPanelDateTitle => _data.CalendarPanelDateTitle;

	public string CalendarPanelEventTitle => _data.CalendarPanelEventTitle;

	public string CalendarPanelPlayerBirthday => _data.CalendarPanelPlayerBirthday;

	public string CalendarPanelMemoTitle => _data.CalendarPanelMemoTitle;

	public string CalendarPanelEmptyHint => _data.CalendarPanelEmptyHint;

	public string CalendarPanelDelMemoHint => _data.CalendarPanelDelMemoHint;

	public string CalendarPanelInputEmptyHint => _data.CalendarPanelInputEmptyHint;

	public string InputTextContainsSensitiveWorld => _data.InputTextContainsSensitiveWorld;

	public string CalendarMemoMessage => _data.CalendarMemoMessage;

	public string UiCropInfoGrowthLevel => _data.UiCropInfoGrowthLevel;

	public string UiCropInfoHarvestCount => _data.UiCropInfoHarvestCount;

	public string UiAnimalInfoState => _data.UiAnimalInfoState;

	public string UiAnimalSpace => _data.UiAnimalSpace;

	public string UiAnimalAgeYear => _data.UiAnimalAgeYear;

	public string UiAnimalAgeMonth => _data.UiAnimalAgeMonth;

	public string UiAnimalAgeDay => _data.UiAnimalAgeDay;

	public string UiAnimalBirthday => _data.UiAnimalBirthday;

	public string UiAnimalCapacity => _data.UiAnimalCapacity;

	public string UiAnimalCallBack => _data.UiAnimalCallBack;

	public string UiAnimalLetOut => _data.UiAnimalLetOut;

	public string UiAnimalAdult => _data.UiAnimalAdult;

	public string UiAnimalChild => _data.UiAnimalChild;

	public string UiAnimalPosition => _data.UiAnimalPosition;

	public string UiAnimalNoAnimal => _data.UiAnimalNoAnimal;

	public string UiAnimalNotVisible => _data.UiAnimalNotVisible;

	public string DronePanelTitle => _data.DronePanelTitle;

	public string DronePanelErrLoad => _data.DronePanelErrLoad;

	public string DronePanelErrLocked => _data.DronePanelErrLocked;

	public string DroneComponentTitle => _data.DroneComponentTitle;

	public string GarbageSubmitErrItem => _data.GarbageSubmitErrItem;

	public string DisposeFailRoom => _data.DisposeFailRoom;

	public string UiTipOpenMapFail => _data.UiTipOpenMapFail;

	public string UiTipNone => _data.UiTipNone;

	public string UiTipBuyBackpack => _data.UiTipBuyBackpack;

	public string UiTipBuyBackpackInfo => _data.UiTipBuyBackpackInfo;

	public string UiTipEmptyList => _data.UiTipEmptyList;

	public string UiTipCurrentPosition => _data.UiTipCurrentPosition;

	public string UiTipCurrentMotorPosition => _data.UiTipCurrentMotorPosition;

	public string UiOptionShootingRangeStart => _data.UiOptionShootingRangeStart;

	public string UiOptionShootingRangeEnd => _data.UiOptionShootingRangeEnd;

	public string UiTipEquipmentUnlock => _data.UiTipEquipmentUnlock;

	public string UiOptionShootingRangeExit => _data.UiOptionShootingRangeExit;

	public string UiOptionConfirmShootingRangeExit => _data.UiOptionConfirmShootingRangeExit;

	public string UiNpcVisit => _data.UiNpcVisit;

	public string UiNpcFileUpdation => _data.UiNpcFileUpdation;

	public string UiNpcLikingRise => _data.UiNpcLikingRise;

	public string UiNpcLikingDecline => _data.UiNpcLikingDecline;

	public string ItemEdenFruitTip => _data.ItemEdenFruitTip;

	public string UiTipGameVersion => _data.UiTipGameVersion;

	public string UiTipContentLock => _data.UiTipContentLock;

	public string UiTipPhotoSaving => _data.UiTipPhotoSaving;

	public string UiItemGenerateElectricityEntry => _data.UiItemGenerateElectricityEntry;

	public string UiTipRename => _data.UiTipRename;

	public string UiModNoMod => _data.UiModNoMod;

	public string UiModNeedSubscribe => _data.UiModNeedSubscribe;

	public string UiModOpenWorkshop => _data.UiModOpenWorkshop;

	public string UiModNeedCreate => _data.UiModNeedCreate;

	public string UiModOpenLocalDirectory => _data.UiModOpenLocalDirectory;

	public string UiModUploadMod => _data.UiModUploadMod;

	public string UiModUpdateMod => _data.UiModUpdateMod;

	public string UiModEnable => _data.UiModEnable;

	public string UiModDisable => _data.UiModDisable;

	public string UiModConfirmUpload => _data.UiModConfirmUpload;

	public string UiModConfirmUpdate => _data.UiModConfirmUpdate;

	public string UiModUploading => _data.UiModUploading;

	public string UiModUploadSuccess => _data.UiModUploadSuccess;

	public string UiModUploadFailed => _data.UiModUploadFailed;

	public string UiModUpdating => _data.UiModUpdating;

	public string UiModUpdateSuccess => _data.UiModUpdateSuccess;

	public string UiModUpdateFailed => _data.UiModUpdateFailed;

	public string UiModReloading => _data.UiModReloading;

	public string UiModSourceLocal => _data.UiModSourceLocal;

	public string UiModSourceWorkshop => _data.UiModSourceWorkshop;

	public TbStaticText(JSONNode _json)
	{
		if (!_json.IsArray)
		{
			throw new SerializationException();
		}
		if (_json.Count != 1)
		{
			throw new SerializationException("table mode=one, but size != 1");
		}
		_data = StaticTextInfo.DeserializeStaticTextInfo(_json[0]);
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
