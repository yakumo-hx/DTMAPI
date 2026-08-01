#pragma warning disable CS0618 // Obsolete fishing DTOs reference each other inside their frozen declaration surface.
using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party OneActionComplete owns the current product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IActionCompletionApi is deprecated and frozen. Use the first-party OneActionComplete product; no replacement public completion API is exposed.", false)]
    public interface IActionCompletionApi
    {
        void Configure(IManifest owner, ActionCompletionOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party AutoFishing is a self-contained Advanced CodeMod and does not consume this API.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IFishingAutomationApi is deprecated and frozen. Use the first-party AutoFishing product; no public Fishing Primitives replacement is exposed. Removal requires the documented preview-cycle gates.", false)]
    public interface IFishingAutomationApi
    {
        void Configure(IManifest owner, FishingAutomationOptions options);
        void SetEnabled(IManifest owner, bool enabled, string reason);
        FishingAutomationState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.12", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party ActionSpeed owns the current product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IActionSpeedApi is deprecated and frozen. Use the first-party ActionSpeed product; no replacement public action-speed API is exposed.", false)]
    public interface IActionSpeedApi
    {
        void Configure(IManifest owner, ActionSpeedOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party FishBreedingAssistant owns the current product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IItemTooltipApi is deprecated and frozen. Use the first-party FishBreedingAssistant product; no replacement public title-formatting API is exposed.", false)]
    public interface IItemTooltipApi
    {
        void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    /// <summary>
    /// On the DTMAPI Runtime/main thread, performs a read-only lookup from an item id to its current localized display name.
    /// Implementations cache only successful non-empty results, do not negatively cache failures, and clear the cache across
    /// save load, return-to-title, and runtime-environment reset boundaries. This contract does not expose item DTOs,
    /// enumeration, mutation, hooks, formatting, or UI behavior.
    /// </summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.5")]
    public interface IItemDisplayNameApi
    {
        bool TryGetDisplayName(string itemId, out string displayName);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party AnimalHusbandryProgress owns the current product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IAnimalViewerApi is deprecated and frozen. Use the first-party AnimalHusbandryProgress product; no replacement public animal-viewer API is exposed.", false)]
    public interface IAnimalViewerApi
    {
        void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface IDebugConsoleApi
    {
        bool IsOpen { get; }
        void Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null);
        void BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi);
        void SetLanguage(IManifest owner, string language);
        void Open(IManifest owner, string reason);
        void Close(IManifest owner, string reason);
        void Toggle(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
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
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface IWeatherDebugApi
    {
        WeatherDebugState GetState();
        IReadOnlyList<WeatherDebugOption> GetAvailableWeathers();
        WeatherSetResult SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.0")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface ITeleportDebugApi
    {
        IReadOnlyList<TeleportDestination> GetDestinations();
        TeleportSnapshot GetCurrentSnapshot();
        TeleportResult Teleport(IManifest owner, string destinationId);
        TeleportCsvExportResult ExportDestinationsCsv(IManifest owner);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface IInstantSaveDebugApi
    {
        InstantSaveDebugState GetState();
        InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.1")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface ITimeDebugApi
    {
        TimeDebugState GetState();
        TimeSkipResult SkipToNextWeatherPeriod(IManifest owner);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.1")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface IMovementDebugApi
    {
        MovementDebugState GetState();
        MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier);
        MovementSpeedResult ResetSpeed(IManifest owner, string reason);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility facade. No runtime provider remains; the first-party Mine product owns its fixed ProductNative behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods. Calls have no registered runtime provider.")]
    [Obsolete("IMachineProductionApi is deprecated and frozen without a runtime provider. Use the first-party Mine product; no replacement public machine-production API is exposed.", false)]
    public interface IMachineProductionApi
    {
        MachineRegisterResult RegisterMachine(IManifest owner, MachineDefinition definition);
        IReadOnlyList<MachineDefinition> GetMachines(string uniqueId);
        MachineProductionState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility facade. The first-party MoreEquipmentSlots product owns a fixed three-slot ProductNative policy; this retained API preserves the historical 0..24 compatibility behavior only.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IEquipmentSlotsApi is deprecated and frozen. Use the first-party MoreEquipmentSlots product; no replacement public equipment-slots API is exposed.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.9", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party MoreSaves owns the current fixed-six/twelve product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("ISaveSlotsApi is deprecated and frozen. Use the first-party MoreSaves product; no replacement public save-slot API is exposed.", false)]
    public interface ISaveSlotsApi
    {
        SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions options);
        SaveSlotsState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Deprecated/Frozen compatibility facade. First-party Zoom owns the current ProductNative camera behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for the published Zoom 0.4.2 binary consumer only; do not adopt in new mods.")]
    [Obsolete("ICameraViewApi is deprecated and frozen. Use the first-party Zoom product; no replacement public camera API is exposed.", false)]
    public interface ICameraViewApi
    {
        ICameraViewLease AcquireLease(IManifest owner, CameraViewRequest request);
        CameraViewState GetState(string uniqueId);
        CameraViewState GetSnapshot(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Deprecated/Frozen lease shape retained with ICameraViewApi binary compatibility.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for the published Zoom 0.4.2 binary consumer only; do not adopt in new mods.")]
    [Obsolete("ICameraViewLease is deprecated and frozen with ICameraViewApi. Use the first-party Zoom product.", false)]
    public interface ICameraViewLease : IDisposable
    {
        string LeaseId { get; }
        string OwnerId { get; }
        bool IsReleased { get; }
        CameraViewResult LastResult { get; }
        CameraViewResult SetViewScale(double viewScale, string reason);
        CameraViewResult Update(CameraViewRequest request, string reason);
        CameraViewResult Release(string reason);
        CameraViewState GetState();
    }

    [DtmApiStatus(DtmApiStatus.Proposed, Since = "0.4.2")]
    public interface IPanoramaCameraApi
    {
    }

    [Obsolete("ICameraZoomApi is deprecated and frozen. Use the first-party Zoom product; no replacement public camera API is exposed.", false)]
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0", Notes = "Deprecated/Frozen compatibility facade retained for binary compatibility.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    public interface ICameraZoomApi
    {
        CameraZoomRegisterResult Register(IManifest owner, CameraZoomOptions options);
        CameraZoomResult SetViewScale(IManifest owner, double viewScale, string reason);
        CameraZoomResult StepViewScale(IManifest owner, int direction, string reason);
        CameraZoomResult ResetViewScale(IManifest owner, string reason);
        CameraZoomState GetState(string uniqueId);
        CameraZoomState GetSnapshot(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0", Notes = "Deprecated/Frozen compatibility facade. No new capabilities will be added; first-party ChestLocatorEnhancer owns the current product behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained for existing binary consumers only; do not adopt in new mods.")]
    [Obsolete("IChestLocatorEnhancerApi is deprecated and frozen. Use the first-party ChestLocatorEnhancer product; no replacement public inventory-query API is exposed.", false)]
    public interface IChestLocatorEnhancerApi
    {
        ChestLocatorEnhancerRegisterResult Register(IManifest owner, ChestLocatorEnhancerOptions options);
        ChestLocatorEnhancerState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0", Notes = "Deprecated/Frozen compatibility shape. No Runtime provider is retained because no published or retained binary consumer exists; first-party StrongPlantingGun owns the fixed-three ProductNative behavior.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5", Notes = "Retained as an ABI warning shell only; no GameBridge compatibility executor is available and new mods must not adopt it.")]
    [Obsolete("IStrongPlantingGunApi is deprecated and frozen. Use the first-party StrongPlantingGun product; no replacement public farming-gun API or compatibility provider is exposed.", false)]
    public interface IStrongPlantingGunApi
    {
        StrongPlantingGunRegisterResult Register(IManifest owner, StrongPlantingGunOptions options);
        StrongPlantingGunState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.0-alpha", Notes = "Semantic crop-container harvesting bridge backed by native PlantBasin maturity and harvest responsibility; first version only executes ordinary PlantBasin-family Harvest(bool,bool) paths and keeps tree-basin cocoa, grass/forage, and other native-owner families unsupported until their ownership is reviewed.")]
    public interface ICropHarvestingApi
    {
        CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request);
        CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.2-alpha", Notes = "Native sound-event replacement bridge backed by WwiseSoundManager hooks. Event names are strings to avoid exposing Doloc Town/Wwise runtime types; replacements must fail open when local audio is unavailable.")]
    public interface IAudioReplacementApi
    {
        AudioReplacementRegisterResult RegisterReplacement(IManifest owner, AudioReplacementOptions options);
        AudioReplacementState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Diagnostic, Since = "0.5.5", Notes = "Frozen DebugConsole compatibility diagnostic surface; retained for exact legacy consumers and not a gameplay authoring API.")]
    public interface IAdvancedDebugApi
    {
        IReadOnlyList<TechPointDebugOption> GetTechPointOptions();
        IReadOnlyList<SpawnDebugOption> GetMonsterOptions();
        IReadOnlyList<SpawnDebugOption> GetResourceOptions();
        CreativeModeState GetCreativeModeState();
        TimeSkipResult AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount);
        TimeScaleDebugResult SetTimeScale(IManifest owner, double multiplier);
        TimeScaleDebugResult ResetTimeScale(IManifest owner, string reason);
        DebugValueResult AddMoney(IManifest owner, int amount);
        DebugValueResult AddTechPoint(IManifest owner, string pointTypeId, int amount);
        DebugCommandResult UnlockAllTechTrees(IManifest owner);
        CropMaturityResult MatureAllCrops(IManifest owner);
        CreativeModeResult SetCreativeMode(IManifest owner, bool enabled);
        InventoryGiveResult GiveCreativeGenerator(IManifest owner);
        SpawnDebugResult SpawnMonster(IManifest owner, string monsterId, int count);
        SpawnDebugResult SpawnResource(IManifest owner, string resourceId, int count);
        BridgeFeatureStatus GetStatus();
    }

    public enum AdvancedTimeAdvanceKind
    {
        Day,
        Week,
        Month
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

    public sealed class TimeScaleDebugResult
    {
        public bool Success { get; set; }
        public double RequestedMultiplier { get; set; }
        public double BeforeMultiplier { get; set; }
        public double AfterMultiplier { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DebugValueResult
    {
        public bool Success { get; set; }
        public string ValueId { get; set; } = string.Empty;
        public int RequestedDelta { get; set; }
        public int BeforeValue { get; set; }
        public int AfterValue { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DebugCommandResult
    {
        public bool Success { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CropMaturityResult
    {
        public bool Success { get; set; }
        public int PlantBasinsVisited { get; set; }
        public int CropsMatured { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CreativeModeState
    {
        public bool Enabled { get; set; }
        public bool RuntimeHooksInstalled { get; set; }
        public bool GeneratorRuntimeAvailable { get; set; }
        public string GeneratorItemId { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public sealed class CreativeModeResult
    {
        public bool Success { get; set; }
        public bool Enabled { get; set; }
        public CreativeModeState Before { get; set; } = new CreativeModeState();
        public CreativeModeState After { get; set; } = new CreativeModeState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TechPointDebugOption
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int CurrentPoints { get; set; }
        public int CurrentLevel { get; set; }
    }

    public sealed class SpawnDebugOption
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsAvailableInCurrentRoom { get; set; } = true;
    }

    public sealed class SpawnDebugResult
    {
        public bool Success { get; set; }
        public string SpawnId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int SpawnedCount { get; set; }
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO. It does not describe the fixed first-party Mine ProductNative policy.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("MachineDefinition is retained only for frozen IMachineProductionApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("MachineRecipeInput is retained only for frozen IMachineProductionApi binary compatibility.", false)]
    public sealed class MachineRecipeInput
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("MachineOutputRule is retained only for frozen IMachineProductionApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("MachineRegisterResult is retained only for frozen IMachineProductionApi binary compatibility.", false)]
    public sealed class MachineRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public MachineDefinition Definition { get; set; } = new MachineDefinition();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("MachineProductionState is retained only for frozen IMachineProductionApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4", Notes = "Deprecated/Frozen compatibility DTO. ExtraAttributeSlots retains the historical 0..24 ABI behavior and does not describe the fixed-three-slot first-party product.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotsOptions is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.9")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("SaveSlotsOptions is retained only for frozen ISaveSlotsApi binary compatibility.", false)]
    public sealed class SaveSlotsOptions
    {
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Experimental compatibility field. In 0.5.1-alpha enabled requests are
        /// normalized to 12 total official save slots; disabled requests remain
        /// the vanilla six slots.
        /// </summary>
        public int SlotCount { get; set; } = 12;
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.9")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("SaveSlotsRegisterResult is retained only for frozen ISaveSlotsApi binary compatibility.", false)]
    public sealed class SaveSlotsRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int PreviousSlotCount { get; set; }
        public int RequestedSlotCount { get; set; }
        public int AppliedSlotCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.9")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("SaveSlotsState is retained only for frozen ISaveSlotsApi binary compatibility.", false)]
    public sealed class SaveSlotsState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public int NativeSlotCount { get; set; }
        public int RequestedSlotCount { get; set; }
        public int AppliedSlotCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraViewRequest is retained only for frozen ICameraViewApi binary compatibility.", false)]
    public sealed class CameraViewRequest
    {
        public bool Enabled { get; set; } = true;
        public double ViewScale { get; set; } = 1d;
        public double MinViewScale { get; set; } = 1d;
        public double MaxViewScale { get; set; } = 4d;
        public double Step { get; set; } = 0.25d;
        public int Priority { get; set; }
        public string LeaseName { get; set; } = string.Empty;
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraViewResult is retained only for frozen ICameraViewApi binary compatibility.", false)]
    public sealed class CameraViewResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string LeaseId { get; set; } = string.Empty;
        public string ActiveOwnerId { get; set; } = string.Empty;
        public string ActiveLeaseId { get; set; } = string.Empty;
        public string ArbitrationStatus { get; set; } = string.Empty;
        public double RequestedViewScale { get; set; }
        public double ClampedViewScale { get; set; }
        public double BeforeViewScale { get; set; }
        public double AfterViewScale { get; set; }
        public double AppliedViewScale { get; set; }
        public double VanillaOrthographicSize { get; set; }
        public double AppliedOrthographicSize { get; set; }
        public string CameraOwnerStatus { get; set; } = string.Empty;
        public string NativeRefreshStatus { get; set; } = string.Empty;
        public string UiScaleStatus { get; set; } = string.Empty;
        public string LifecycleStatus { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraViewState is retained only for frozen ICameraViewApi binary compatibility.", false)]
    public sealed class CameraViewState
    {
        public string OwnerId { get; set; } = string.Empty;
        public string LeaseId { get; set; } = string.Empty;
        public string LeaseName { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public bool IsReleased { get; set; }
        public int Priority { get; set; }
        public int LeaseCount { get; set; }
        public string ActiveOwnerId { get; set; } = string.Empty;
        public string ActiveLeaseId { get; set; } = string.Empty;
        public string ArbitrationStatus { get; set; } = string.Empty;
        public double MinViewScale { get; set; }
        public double MaxViewScale { get; set; }
        public double Step { get; set; }
        public double RequestedViewScale { get; set; } = 1d;
        public double ClampedViewScale { get; set; } = 1d;
        public double CurrentViewScale { get; set; } = 1d;
        public double AppliedViewScale { get; set; } = 1d;
        public double VanillaOrthographicSize { get; set; }
        public double AppliedOrthographicSize { get; set; }
        public bool CameraAvailable { get; set; }
        public string CameraOwnerStatus { get; set; } = string.Empty;
        public string NativeRefreshStatus { get; set; } = string.Empty;
        public string UiScaleStatus { get; set; } = string.Empty;
        public string LifecycleStatus { get; set; } = string.Empty;
        public string CurrentRoomId { get; set; } = string.Empty;
        public string CurrentRoomTitle { get; set; } = string.Empty;
        public bool CurrentRoomShowsBackground { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraZoomOptions is retained only for frozen ICameraZoomApi binary compatibility.", false)]
    public sealed class CameraZoomOptions
    {
        public bool Enabled { get; set; } = true;
        public double MinViewScale { get; set; } = 1d;
        public double MaxViewScale { get; set; } = 4d;
        public double Step { get; set; } = 0.25d;
        [Obsolete("Playable camera zoom no longer calls CameraController.RefreshResolution/SetPosition.")]
        public bool RefreshCameraController { get; set; }
        [Obsolete("Playable camera zoom no longer performs panorama/background transform compensation.")]
        public bool CompensateBackground { get; set; }
        [Obsolete("Playable camera zoom no longer performs panorama/depth-fog transform compensation.")]
        public bool CompensateDepthFog { get; set; }
        [Obsolete("Playable camera zoom no longer calls DolocAPI.RefreshScanner.")]
        public bool RefreshScanners { get; set; }
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraZoomRegisterResult is retained only for frozen ICameraZoomApi binary compatibility.", false)]
    public sealed class CameraZoomRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public double MinViewScale { get; set; }
        public double MaxViewScale { get; set; }
        public double CurrentViewScale { get; set; }
        public double AppliedViewScale { get; set; }
        public string ActiveOwnerId { get; set; } = string.Empty;
        public string CameraControllerStatus { get; set; } = string.Empty;
        public string BackgroundCompensationStatus { get; set; } = string.Empty;
        public string FogCompensationStatus { get; set; } = string.Empty;
        public string ScannerRefreshStatus { get; set; } = string.Empty;
        public string LifecycleRestoreStatus { get; set; } = string.Empty;
        public string UiScaleStatus { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraZoomResult is retained only for frozen ICameraZoomApi binary compatibility.", false)]
    public sealed class CameraZoomResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public double RequestedViewScale { get; set; }
        public double ClampedViewScale { get; set; }
        public double BeforeViewScale { get; set; }
        public double AfterViewScale { get; set; }
        public double AppliedViewScale { get; set; }
        public double VanillaOrthographicSize { get; set; }
        public double AppliedOrthographicSize { get; set; }
        public string ActiveOwnerId { get; set; } = string.Empty;
        public string CameraControllerStatus { get; set; } = string.Empty;
        public string BackgroundCompensationStatus { get; set; } = string.Empty;
        public string FogCompensationStatus { get; set; } = string.Empty;
        public string ScannerRefreshStatus { get; set; } = string.Empty;
        public string LifecycleRestoreStatus { get; set; } = string.Empty;
        public string UiScaleStatus { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("CameraZoomState is retained only for frozen ICameraZoomApi binary compatibility.", false)]
    public sealed class CameraZoomState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public double MinViewScale { get; set; }
        public double MaxViewScale { get; set; }
        public double Step { get; set; }
        public double RequestedViewScale { get; set; } = 1d;
        public double ClampedViewScale { get; set; } = 1d;
        public double CurrentViewScale { get; set; } = 1d;
        public double AppliedViewScale { get; set; } = 1d;
        public string ActiveOwnerId { get; set; } = string.Empty;
        public double VanillaOrthographicSize { get; set; }
        public double AppliedOrthographicSize { get; set; }
        public bool CameraAvailable { get; set; }
        public bool RefreshCameraController { get; set; }
        public bool CompensateBackground { get; set; }
        public bool CompensateDepthFog { get; set; }
        public bool RefreshScanners { get; set; }
        public string CameraControllerStatus { get; set; } = string.Empty;
        public string BackgroundCompensationStatus { get; set; } = string.Empty;
        public string FogCompensationStatus { get; set; } = string.Empty;
        public string ScannerRefreshStatus { get; set; } = string.Empty;
        public string LifecycleRestoreStatus { get; set; } = string.Empty;
        public string UiScaleStatus { get; set; } = string.Empty;
        public string CurrentRoomId { get; set; } = string.Empty;
        public string CurrentRoomTitle { get; set; } = string.Empty;
        public bool CurrentRoomShowsBackground { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("ChestLocatorEnhancerOptions is retained only for frozen IChestLocatorEnhancerApi binary compatibility.", false)]
    public sealed class ChestLocatorEnhancerOptions
    {
        public bool Enabled { get; set; } = true;
        public bool IncludeSharedCases { get; set; } = true;
        public bool IncludeSharedStorageShelfBoxes { get; set; } = true;
        public bool RespectNativeAutoUseBoxSetting { get; set; } = true;
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("ChestLocatorEnhancerRegisterResult is retained only for frozen IChestLocatorEnhancerApi binary compatibility.", false)]
    public sealed class ChestLocatorEnhancerRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool HookInstalled { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("ChestLocatorEnhancerState is retained only for frozen IChestLocatorEnhancerApi binary compatibility.", false)]
    public sealed class ChestLocatorEnhancerState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public bool HookInstalled { get; set; }
        public int ExtensionApplications { get; set; }
        public int LastBaseInventoryCount { get; set; }
        public int LastAppendedInventoryCount { get; set; }
        public int LastScannedRootCount { get; set; }
        public int LastScannedEquipmentCount { get; set; }
        public int LastSharedCaseCount { get; set; }
        public int LastSharedStorageBoxCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0", Notes = "Deprecated/Frozen compatibility DTO. The first-party product remains fixed at three seed/film/fertilizer slots and does not consume this DTO.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("StrongPlantingGunOptions is retained only as a frozen ABI warning shell; no Runtime compatibility provider is exposed.", false)]
    public sealed class StrongPlantingGunOptions
    {
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Historical requested farming-gun slot count. The retired GameBridge
        /// provider normalized it to three; the current first-party product is
        /// independently fixed at three and does not consume this DTO.
        /// </summary>
        public int SlotCount { get; set; } = 3;
        public bool IncludeSeeds { get; set; } = true;
        public bool IncludeFilms { get; set; } = true;
        public bool IncludeFertilizers { get; set; } = true;
        public bool IncludeWater { get; set; }
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("StrongPlantingGunRegisterResult is retained only as a frozen ABI warning shell; no Runtime compatibility provider is exposed.", false)]
    public sealed class StrongPlantingGunRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public int SlotCount { get; set; }
        public bool ToolHookInstalled { get; set; }
        public bool UiHookInstalled { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.3.0")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("StrongPlantingGunState is retained only as a frozen ABI warning shell; no Runtime compatibility provider is exposed.", false)]
    public sealed class StrongPlantingGunState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public int SlotCount { get; set; }
        public bool ToolHookInstalled { get; set; }
        public bool UiHookInstalled { get; set; }
        public int ExpandedGunCount { get; set; }
        public int LastVisitedEquipmentCount { get; set; }
        public int LastSeedActions { get; set; }
        public int LastFilmActions { get; set; }
        public int LastFertilizerActions { get; set; }
        public int LastWaterActions { get; set; }
        public int LastConsumedItemCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public sealed class AudioReplacementOptions
    {
        public bool Enabled { get; set; } = true;
        public string ReplacementId { get; set; } = string.Empty;
        public string NativeSoundEvent { get; set; } = string.Empty;
        public string AudioPath { get; set; } = string.Empty;
        public bool SuppressNativeWhenReady { get; set; } = true;
        public double Volume { get; set; } = 1.0;
        public int CooldownMilliseconds { get; set; }
        public bool VerboseLogging { get; set; }
    }

    public sealed class AudioReplacementRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string ReplacementId { get; set; } = string.Empty;
        public string NativeSoundEvent { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool HookInstalled { get; set; }
        public bool PreloadReady { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class AudioReplacementState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public bool HookInstalled { get; set; }
        public int ReplacementCount { get; set; }
        public IReadOnlyList<AudioReplacementEntryInfo> Replacements { get; set; } = Array.Empty<AudioReplacementEntryInfo>();
        public string LastNativeSoundEvent { get; set; } = string.Empty;
        public string LastReplacementId { get; set; } = string.Empty;
        public bool LastReplacementPlayed { get; set; }
        public bool LastNativeSuppressed { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class AudioReplacementEntryInfo
    {
        public string ReplacementId { get; set; } = string.Empty;
        public string NativeSoundEvent { get; set; } = string.Empty;
        public string AudioPath { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool PreloadReady { get; set; }
        public string LoadStatus { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public enum CropHarvestScope
    {
        CurrentFarmAndFarmRooms
    }

    public enum CropHarvestTargetKind
    {
        Unknown,
        OrdinaryCrop,
        Vine,
        MushroomBag,
        Bush,
        TreeBasinCrop,
        GrassForageBasin
    }

    public enum CropHarvestTargetStatus
    {
        Pending,
        Harvested,
        NotMature,
        AlreadyHarvested,
        UnsupportedBasinType,
        NativeHarvestFailed,
        SkippedByRequestFilter,
        Busy
    }

    public sealed class CropHarvestRequest
    {
        public CropHarvestScope Scope { get; set; } = CropHarvestScope.CurrentFarmAndFarmRooms;
        public bool IncludeOrdinaryCrops { get; set; } = true;
        public bool IncludeVines { get; set; }
        public bool IncludeMushroomBags { get; set; }
        public bool IncludeBushes { get; set; }
        public bool IncludeTreeBasinCrops { get; set; }
        public int MaxHarvests { get; set; } = 24;
        public bool DryRun { get; set; }
        public bool SendNativeMessage { get; set; } = true;
        public bool VerboseLogging { get; set; }
        public IReadOnlyList<string> TargetIds { get; set; } = Array.Empty<string>();
    }

    public sealed class CropHarvestResult
    {
        public bool Success { get; set; }
        public bool DryRun { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public CropHarvestScope Scope { get; set; }
        public int RoomsVisited { get; set; }
        public int PlantBasinsVisited { get; set; }
        public int MatureTargetsFound { get; set; }
        public int HarvestedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public bool Busy { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<CropHarvestTargetResult> Targets { get; set; } = Array.Empty<CropHarvestTargetResult>();
    }

    public sealed class CropHarvestTargetResult
    {
        /// <summary>
        /// Opaque transient target id. It is only valid for an immediate follow-up harvest request in the same loaded world/session and must not be persisted.
        /// </summary>
        public string TargetId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string RoomTitle { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string CropId { get; set; } = string.Empty;
        public string CropTitle { get; set; } = string.Empty;
        public CropHarvestTargetKind Kind { get; set; }
        public CropHarvestTargetStatus Status { get; set; }
        public bool IsMature { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotsRegisterResult is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
    public sealed class EquipmentSlotsRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int ExtraAttributeSlots { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotsState is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotInfo is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotEquipResult is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("EquipmentSlotsRecoveryResult is retained only for frozen IEquipmentSlotsApi binary compatibility.", false)]
    public sealed class EquipmentSlotsRecoveryResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int RecoveredCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Options DTO for the frozen IActionCompletionApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("ActionCompletionOptions is retained only for frozen IActionCompletionApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Strategy enum for the frozen IFishingAutomationApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("Legacy IFishingAutomationApi strategy type; deprecated and frozen.", false)]
    public enum FishingBiteWaitMode
    {
        NativeWait,
        InstantNativeBite
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Strategy enum for the frozen IFishingAutomationApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("Legacy IFishingAutomationApi strategy type; deprecated and frozen.", false)]
    public enum FishingResultMode
    {
        AutoCompleteVisibleMiniGame,
        SkipMiniGameNativeResult
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Strategy enum for the frozen IFishingAutomationApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("Legacy IFishingAutomationApi strategy type; deprecated and frozen.", false)]
    public enum FishingAnimationMode
    {
        Normal,
        FastCastPull
    }

    /// <summary>
    /// Frozen legacy IFishingAutomationApi options retained for binary compatibility.
    /// The self-contained AutoFishing Advanced product does not consume this DTO.
    /// </summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Options DTO for the frozen IFishingAutomationApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("Legacy IFishingAutomationApi options DTO; deprecated and frozen.", false)]
    public sealed class FishingAutomationOptions
    {
        public FishingBiteWaitMode BiteWaitMode { get; set; } = FishingBiteWaitMode.NativeWait;
        public FishingResultMode ResultMode { get; set; } = FishingResultMode.AutoCompleteVisibleMiniGame;
        public FishingAnimationMode AnimationMode { get; set; } = FishingAnimationMode.Normal;
        public double RecastDelaySeconds { get; set; } = 0.25;
        /// <summary>
        /// Target native cast-charge ratio for the Ready phase. 0 releases
        /// immediately; 1 holds until full charge.
        /// </summary>
        public double CastChargeRatio { get; set; }
        public double AnimationMultiplier { get; set; } = 3;
        [Obsolete("Retained for binary compatibility. Movement cancellation remains AutoFishing/consumer policy; GameBridge stores but does not execute this option.", false)]
        public bool StopOnManualMove { get; set; } = true;
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.12", Notes = "Options DTO for the frozen IActionSpeedApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("ActionSpeedOptions is retained only for frozen IActionSpeedApi binary compatibility.", false)]
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

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "State DTO for the frozen IFishingAutomationApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("Legacy IFishingAutomationApi state DTO; deprecated and frozen.", false)]
    public sealed class FishingAutomationState
    {
        public bool Enabled { get; set; }
        public string Phase { get; set; } = "Idle";
        public string LastReason { get; set; } = string.Empty;
        public string NativeOwner { get; set; } = string.Empty;
        public string LastNativeAction { get; set; } = string.Empty;
        public string LastResult { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Options DTO for the frozen IItemTooltipApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("FishRoeTooltipOptions is retained only for frozen IItemTooltipApi binary compatibility.", false)]
    public sealed class FishRoeTooltipOptions
    {
        public bool Enabled { get; set; } = true;
        public bool LabelFishRoeTitle { get; set; } = true;
        public bool LabelFishRoeDetails { get; set; } = true;
        public int CacheSeconds { get; set; } = 30;
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Display DTO for the frozen IItemTooltipApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("FishRoeDisplayInfo is retained only for frozen IItemTooltipApi binary compatibility.", false)]
    public sealed class FishRoeDisplayInfo
    {
        public string FishId { get; set; } = string.Empty;
        public string FishTitle { get; set; } = string.Empty;
        public string RoeTitle { get; set; } = string.Empty;
        public string IncubateText { get; set; } = string.Empty;
        public string GrowText { get; set; } = string.Empty;
        public string ParentSummary { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10", Notes = "Options DTO for the frozen IAnimalViewerApi compatibility facade.")]
    [DtmApiDisposition(DtmApiDisposition.Frozen, Since = "0.5.5")]
    [Obsolete("AnimalHusbandryProgressOptions is retained only for frozen IAnimalViewerApi binary compatibility.", false)]
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
