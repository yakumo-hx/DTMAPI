using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IActionCompletionApi
    {
        void Configure(IManifest owner, ActionCompletionOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IFishingAutomationApi
    {
        void Configure(IManifest owner, FishingAutomationOptions options);
        void SetEnabled(IManifest owner, bool enabled, string reason);
        FishingAutomationState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.12")]
    public interface IActionSpeedApi
    {
        void Configure(IManifest owner, ActionSpeedOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IItemTooltipApi
    {
        void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IAnimalViewerApi
    {
        void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    public interface IDebugConsoleApi
    {
        bool IsOpen { get; }
        void Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null);
        void SetLanguage(IManifest owner, string language);
        void Open(IManifest owner, string reason);
        void Close(IManifest owner, string reason);
        void Toggle(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    public interface IInventoryDebugApi
    {
        InventoryDebugPage GetItems(InventoryDebugQuery query);
        InventoryGiveResult GiveItem(IManifest owner, string itemId, int count);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.3")]
    public interface IMailDeliveryApi
    {
        MailItemDeliveryResult SendItemMail(IManifest owner, MailItemDeliveryRequest request);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    public interface IWeatherDebugApi
    {
        WeatherDebugState GetState();
        IReadOnlyList<WeatherDebugOption> GetAvailableWeathers();
        WeatherSetResult SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    public interface ITeleportDebugApi
    {
        IReadOnlyList<TeleportDestination> GetDestinations();
        TeleportSnapshot GetCurrentSnapshot();
        TeleportResult Teleport(IManifest owner, string destinationId);
        TeleportCsvExportResult ExportDestinationsCsv(IManifest owner);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    public interface IInstantSaveDebugApi
    {
        InstantSaveDebugState GetState();
        InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.1")]
    public interface ITimeDebugApi
    {
        TimeDebugState GetState();
        TimeSkipResult SkipToNextWeatherPeriod(IManifest owner);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.1")]
    public interface IMovementDebugApi
    {
        MovementDebugState GetState();
        MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier);
        MovementSpeedResult ResetSpeed(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.2")]
    public interface IMotorVehicleApi
    {
        event EventHandler<MotorVehicleEventArgs>? VehicleChanged;

        MotorVehicleState GetOriginalMotorState();
        MotorVehicleState GetVehicleState(string vehicleId);
        IReadOnlyList<MotorVehicleState> GetVehicles();
        MotorVehicleRegisterResult RegisterSecondMotor(IManifest owner, SecondMotorOptions options);
        MotorVehicleSummonResult UnlockOriginalMotor(IManifest owner, double yOffset);
        MotorVehicleSummonResult SummonOriginalMotor(IManifest owner);
        MotorVehicleSummonResult SummonVehicle(IManifest owner, string vehicleId);
        MotorVehicleRideResult RideVehicle(IManifest owner, string vehicleId);
        MotorVehicleRideResult DismountVehicle(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    public interface IMachineProductionApi
    {
        MachineRegisterResult RegisterMachine(IManifest owner, MachineDefinition definition);
        IReadOnlyList<MachineDefinition> GetMachines(string uniqueId);
        MachineProductionState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    public interface IEquipmentSlotsApi
    {
        EquipmentSlotsRegisterResult RegisterSlots(IManifest owner, EquipmentSlotsOptions options);
        IReadOnlyList<EquipmentSlotInfo> GetSlots(string uniqueId);
        EquipmentSlotEquipResult EquipExtraSlot(IManifest owner, string slotId, string itemId);
        EquipmentSlotEquipResult UnequipExtraSlot(IManifest owner, string slotId, string reason);
        EquipmentSlotsState GetState(string uniqueId);
        EquipmentSlotsRecoveryResult RecoverExtraSlotItems(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    public sealed class InventoryDebugQuery
    {
        public string SearchText { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public bool ModItemsOnly { get; set; }
        public bool IncludeUnavailable { get; set; } = true;
        public int Page { get; set; }
        public int PageSize { get; set; } = 12;
    }

    public sealed class InventoryDebugPage
    {
        public IReadOnlyList<InventoryDebugItem> Items { get; set; } = Array.Empty<InventoryDebugItem>();
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
        public IReadOnlyList<InventoryDebugSourceGroup> Sources { get; set; } = Array.Empty<InventoryDebugSourceGroup>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public sealed class InventoryDebugSourceGroup
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SourceKind { get; set; } = string.Empty;
        public bool IsModSource { get; set; }
        public int Count { get; set; }
        public bool Enabled { get; set; } = true;
        public bool EnablementKnown { get; set; } = true;
        public ulong? WorkshopId { get; set; }
    }

    public sealed class InventoryDebugItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public string SearchText { get; set; } = string.Empty;
        public int MaxStack { get; set; }
        public bool CanSpawn { get; set; }
        public bool CanGive { get; set; }
        public bool RuntimeLoaded { get; set; }
        public bool IsModItem { get; set; }
        public string CannotGiveReason { get; set; } = string.Empty;
        public bool HasIcon { get; set; }
        public string IconAssetKey { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string SourceKind { get; set; } = "Vanilla";
        public string SourceModTitle { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public ulong? WorkshopId { get; set; }
        public bool SourceEnabled { get; set; } = true;
        public bool SourceEnablementKnown { get; set; } = true;
        public string RootPath { get; set; } = string.Empty;
        public string ContentPath { get; set; } = string.Empty;
        public int LoadOrder { get; set; } = -1;
        public int RuntimeOrder { get; set; } = int.MaxValue;
    }

    public sealed class InventoryGiveResult
    {
        public bool Success { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int BeforeCount { get; set; }
        public int AfterCount { get; set; }
        public int GivenCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MailItemDeliveryRequest
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public string EmailName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string TemplateName { get; set; } = "send_item_template";
        public bool SkipIfAlreadyOwned { get; set; } = true;
        public bool PreventDuplicatePendingMail { get; set; } = true;
        public bool RequireEnabledContentSource { get; set; }
        public string RequiredSourceId { get; set; } = string.Empty;
    }

    public sealed class MailItemDeliveryResult
    {
        public bool Success { get; set; }
        public bool Sent { get; set; }
        public bool Skipped { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int BackpackCount { get; set; }
        public int PendingMailCount { get; set; }
        public string EmailName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public bool SourceEnabled { get; set; }
        public bool SourceEnablementKnown { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class WeatherDebugState
    {
        public string CurrentWeatherId { get; set; } = string.Empty;
        public string CurrentWeatherName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public IReadOnlyList<string> CurrentDayForecastWeatherIds { get; set; } = Array.Empty<string>();
    }

    public sealed class WeatherDebugOption
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public bool IsCurrentDayForecast { get; set; }
        public bool IsMalignant { get; set; }
        public bool IsRainy { get; set; }
        public bool IsWindy { get; set; }
        public double Sun { get; set; }
        public double Water { get; set; }
        public double Wind { get; set; }
    }

    public sealed class WeatherSetResult
    {
        public bool Success { get; set; }
        public string WeatherId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BeforeWeatherId { get; set; } = string.Empty;
        public string AfterWeatherId { get; set; } = string.Empty;
        public bool PatchedCurrentPeriod { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TeleportDestination
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SuggestedDisplayName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string MarkPointId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsStation { get; set; }
        public bool IsUnlocked { get; set; } = true;
        public string Source { get; set; } = string.Empty;
    }

    public sealed class TeleportSnapshot
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomTitle { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public sealed class TeleportResult
    {
        public bool Success { get; set; }
        public string DestinationId { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public string MarkPointId { get; set; } = string.Empty;
        public TeleportSnapshot Before { get; set; } = new TeleportSnapshot();
        public TeleportSnapshot AfterRequest { get; set; } = new TeleportSnapshot();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TeleportCsvExportResult
    {
        public bool Success { get; set; }
        public string Path { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class InstantSaveDebugState
    {
        public bool CanSave { get; set; }
        public int? SaveSlot { get; set; }
        public TeleportSnapshot CurrentLocation { get; set; } = new TeleportSnapshot();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class InstantSaveDebugResult
    {
        public bool Success { get; set; }
        public bool ReloadAfterSave { get; set; }
        public bool ReloadRequested { get; set; }
        public int? SaveSlot { get; set; }
        public InstantSaveDebugState Before { get; set; } = new InstantSaveDebugState();
        public InstantSaveDebugState AfterSave { get; set; } = new InstantSaveDebugState();
        public InstantSaveDebugState AfterReloadRequest { get; set; } = new InstantSaveDebugState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TimeDebugState
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public string CurrentWeatherId { get; set; } = string.Empty;
        public string CurrentWeatherName { get; set; } = string.Empty;
        public string SeasonName { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
    }

    public sealed class TimeSkipResult
    {
        public bool Success { get; set; }
        public TimeDebugState Before { get; set; } = new TimeDebugState();
        public TimeDebugState After { get; set; } = new TimeDebugState();
        public int AdvancedGameMinutes { get; set; }
        public int AdvancedSeconds { get; set; }
        public int TargetHour { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MovementDebugState
    {
        public double Multiplier { get; set; } = 1;
        public double MoveSpeed { get; set; }
        public bool IsDefault { get; set; } = true;
        public string Source { get; set; } = string.Empty;
    }

    public sealed class MovementSpeedResult
    {
        public bool Success { get; set; }
        public double RequestedMultiplier { get; set; }
        public double AppliedMultiplier { get; set; }
        public MovementDebugState Before { get; set; } = new MovementDebugState();
        public MovementDebugState After { get; set; } = new MovementDebugState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class SecondMotorOptions
    {
        public string VehicleId { get; set; } = "dtmapi.second_motor";
        public string DisplayName { get; set; } = "Second Motor";
        public string KeyItemId { get; set; } = "dtmapi_second_motor_key";
        public double SpeedMultiplier { get; set; } = 2;
        public bool UseOriginalMotorVisuals { get; set; } = true;
        public string TextureSourceNote { get; set; } = string.Empty;
        public bool VerboseLogging { get; set; }
    }

    public sealed class MotorVehicleState
    {
        public string VehicleId { get; set; } = string.Empty;
        public string OwnerUniqueId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsOriginalMotor { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRiding { get; set; }
        public bool IsAvailableInCurrentRoom { get; set; }
        public string RoomId { get; set; } = string.Empty;
        public string RoomTitle { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double EnduranceProgress { get; set; }
        public double BaseMaxSpeed { get; set; }
        public double EffectiveMaxSpeed { get; set; }
        public double SpeedMultiplier { get; set; } = 1;
        public string KeyItemId { get; set; } = string.Empty;
        public string LastFailureReason { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public sealed class MotorVehicleRegisterResult
    {
        public bool Success { get; set; }
        public string VehicleId { get; set; } = string.Empty;
        public string KeyItemId { get; set; } = string.Empty;
        public MotorVehicleState State { get; set; } = new MotorVehicleState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MotorVehicleSummonResult
    {
        public bool Success { get; set; }
        public string VehicleId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public MotorVehicleState Before { get; set; } = new MotorVehicleState();
        public MotorVehicleState After { get; set; } = new MotorVehicleState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MotorVehicleRideResult
    {
        public bool Success { get; set; }
        public string VehicleId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public MotorVehicleState Before { get; set; } = new MotorVehicleState();
        public MotorVehicleState After { get; set; } = new MotorVehicleState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MotorVehicleEventArgs : EventArgs
    {
        public string EventType { get; set; } = string.Empty;
        public string VehicleId { get; set; } = string.Empty;
        public MotorVehicleState State { get; set; } = new MotorVehicleState();
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MachineDefinition
    {
        public string MachineId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public string RecipeGroupId { get; set; } = string.Empty;
        public double VisualScale { get; set; } = 1;
        public bool AllowFuelMode { get; set; } = true;
        public bool AllowElectricMode { get; set; } = true;
        public string DefaultMode { get; set; } = "fuel";
        public string NativeTechTreeId { get; set; } = string.Empty;
        public string NativeTechNodeId { get; set; } = string.Empty;
        public string NativeTechNodeTitle { get; set; } = string.Empty;
        public string NativeTechNodeDescription { get; set; } = string.Empty;
        public string NativeTechNodeParentId { get; set; } = string.Empty;
        public string NativeTechNodeAboveTitleContains { get; set; } = string.Empty;
        public int FuelCapacity { get; set; } = 7200;
        public int FuelOnlyFuelCostPerCycle { get; set; } = 120;
        public int ElectricModeFuelCostPerCycle { get; set; } = 20;
        public int ElectricModePowerCostPerCycle { get; set; } = 10;
        public int CycleMinutes { get; set; } = 120;
        public IReadOnlyList<MachineRecipeInput> RecipeInputs { get; set; } = Array.Empty<MachineRecipeInput>();
        public bool IncludeRuntimeModMinerals { get; set; } = true;
        public IReadOnlyList<MachineOutputRule> OutputRules { get; set; } = Array.Empty<MachineOutputRule>();
        public IReadOnlyDictionary<string, double> ProbabilityOverrides { get; set; } = new Dictionary<string, double>();
        public bool VerboseLogging { get; set; }
    }

    public sealed class MachineRecipeInput
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class MachineOutputRule
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double Weight { get; set; } = 1;
        public int MinCount { get; set; } = 1;
        public int MaxCount { get; set; } = 1;
        public string Source { get; set; } = "default";
        public bool AllowProbabilityOverride { get; set; } = true;
    }

    public sealed class MachineRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public MachineDefinition Definition { get; set; } = new MachineDefinition();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class MachineProductionState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public int RegisteredMachineCount { get; set; }
        public bool RuntimeHookInstalled { get; set; }
        public string MachineId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public string RecipeGroupId { get; set; } = string.Empty;
        public double VisualScale { get; set; } = 1;
        public bool AllowFuelMode { get; set; }
        public bool AllowElectricMode { get; set; }
        public string DefaultMode { get; set; } = string.Empty;
        public int FuelCapacity { get; set; }
        public int RemainingFuel { get; set; }
        public int FuelOnlyFuelCostPerCycle { get; set; }
        public int ElectricModeFuelCostPerCycle { get; set; }
        public int ElectricModePowerCostPerCycle { get; set; }
        public int CycleMinutes { get; set; }
        public int CycleTUs { get; set; }
        public int NextDueTotalTUs { get; set; } = -1;
        public int PlacedMachineCount { get; set; }
        public int ProductionCycleCount { get; set; }
        public string LastOutputItemId { get; set; } = string.Empty;
        public string LastOutputDisplayName { get; set; } = string.Empty;
        public int LastOutputCount { get; set; }
        public string LastMachineKey { get; set; } = string.Empty;
        public string LastMode { get; set; } = string.Empty;
        public int LastFuelCost { get; set; }
        public int LastElectricPowerCost { get; set; }
        public int LastObservedTotalTUs { get; set; } = -1;
        public string LastOutputTarget { get; set; } = string.Empty;
        public int LastStorageFilledSlots { get; set; }
        public int LastStorageCapacity { get; set; }
        public int LastStorageLineCapacity { get; set; }
        public string NativeTechTreeSummary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public sealed class EquipmentSlotsOptions
    {
        public bool Enabled { get; set; } = true;
        public int ExtraAttributeSlots { get; set; } = 3;
        public string SlotIdPrefix { get; set; } = "dtmapi.extra";
        public bool PreserveVanillaVisualSlots { get; set; } = true;
        public bool ExtraSlotsAffectVisuals { get; set; }
        public bool SafeUnequipOnDisable { get; set; } = true;
        public bool AutoRecoverOnMissingMod { get; set; } = true;
        public bool VerboseLogging { get; set; }
    }

    public sealed class EquipmentSlotsRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int ExtraAttributeSlots { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class EquipmentSlotsState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool RuntimeUiHookInstalled { get; set; }
        public bool RuntimeStatsHookInstalled { get; set; }
        public int ExtraAttributeSlots { get; set; }
        public int StoredItemCount { get; set; }
        public int AppliedItemCount { get; set; }
        public bool PreserveVanillaVisualSlots { get; set; } = true;
        public bool ExtraSlotsAffectVisuals { get; set; }
        public bool SafeUnequipOnDisable { get; set; } = true;
        public int StatsRefreshCount { get; set; }
        public int PendingRecoveryCount { get; set; }
        public string LastEquippedSlotId { get; set; } = string.Empty;
        public string LastEquippedItemId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastRecoveryMessage { get; set; } = string.Empty;
    }

    public sealed class EquipmentSlotInfo
    {
        public string OwnerId { get; set; } = string.Empty;
        public string SlotId { get; set; } = string.Empty;
        public int Index { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public bool IsApplied { get; set; }
        public bool IsRecoverable { get; set; } = true;
        public bool AttributeOnly { get; set; } = true;
        public bool AffectsVisuals { get; set; }
        public string LastMessage { get; set; } = string.Empty;
    }

    public sealed class EquipmentSlotEquipResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string SlotId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int BeforeBackpackCount { get; set; }
        public int AfterBackpackCount { get; set; }
        public int RecoveredCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class EquipmentSlotsRecoveryResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int RecoveredCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class ActionCompletionOptions
    {
        public bool Enabled { get; set; }
        public bool CompleteTrees { get; set; }
        public bool CompleteOres { get; set; }
        public bool CompleteGarbage { get; set; }
        public bool CompleteWeeds { get; set; }
        public bool CompleteMachineFuel { get; set; }
        public bool CompleteFeeder { get; set; }
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishingAutomationOptions
    {
        public bool AutoRecast { get; set; } = true;
        public bool StopOnManualMove { get; set; } = true;
        public bool RequireSelectedFishingRod { get; set; }
        public double CastReleaseProgress { get; set; }
        public double RecastDelaySeconds { get; set; } = 0.25;
        public bool SkipMiniGame { get; set; }
        public bool AutoCompleteMiniGame { get; set; }
        public bool InstantBite { get; set; }
        public bool FastAnimations { get; set; }
        public double FastAnimationMultiplier { get; set; } = 3;
        public bool VerboseLogging { get; set; }
    }

    public sealed class ActionSpeedOptions
    {
        public bool Enabled { get; set; }
        public bool ToolSpeedEnabled { get; set; }
        public double ToolMultiplier { get; set; } = 3;
        public bool BottleFillSpeedEnabled { get; set; }
        public double BottleFillMultiplier { get; set; } = 3;
        public bool EatDrinkSpeedEnabled { get; set; }
        public double EatDrinkMultiplier { get; set; } = 3;
        public bool MachineAddSpeedEnabled { get; set; }
        public double MachineAddMultiplier { get; set; } = 3;
        public bool HarvestSpeedEnabled { get; set; }
        public double HarvestMultiplier { get; set; } = 3;
        public bool PlantSpeedEnabled { get; set; }
        public double PlantMultiplier { get; set; } = 3;
        public bool AutoFillBottle { get; set; }
        public bool AutoFillStrong { get; set; }
        public double AutoFillCooldownSeconds { get; set; } = 0.25;
        public double AutoFillStrongCooldownSeconds { get; set; } = 0.1;
        public bool ContinuousDrinkWithRightClick { get; set; }
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishingAutomationState
    {
        public bool Enabled { get; set; }
        public string Phase { get; set; } = "Idle";
        public string LastReason { get; set; } = string.Empty;
    }

    public sealed class FishRoeTooltipOptions
    {
        public bool Enabled { get; set; } = true;
        public bool LabelFishRoeTitle { get; set; } = true;
        public bool LabelFishRoeDetails { get; set; } = true;
        public int CacheSeconds { get; set; } = 30;
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishRoeDisplayInfo
    {
        public string FishId { get; set; } = string.Empty;
        public string FishTitle { get; set; } = string.Empty;
        public string RoeTitle { get; set; } = string.Empty;
        public string IncubateText { get; set; } = string.Empty;
        public string GrowText { get; set; } = string.Empty;
        public string ParentSummary { get; set; } = string.Empty;
    }

    public sealed class AnimalHusbandryProgressOptions
    {
        public bool Enabled { get; set; } = true;
        public DtmColor ProgressColor { get; set; } = new DtmColor(1, 0.58, 0.18, 1);
        public int CacheSeconds { get; set; } = 10;
        public bool VerboseLogging { get; set; }
    }

    public sealed class BridgeFeatureStatus
    {
        public BridgeFeatureStatus(string status, string details)
        {
            Status = string.IsNullOrWhiteSpace(status) ? "unknown" : status;
            Details = details ?? string.Empty;
        }

        public string Status { get; }
        public string Details { get; }
    }

    public struct DtmColor
    {
        public DtmColor(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
    }
}
