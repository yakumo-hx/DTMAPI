#pragma warning disable CS0618 // The frozen compatibility APIs still require owner-bound facades.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Creates compile-time owner-bound facades without changing the raw runtime API objects.</summary>
    internal static class OwnerBoundGameBridgeApis
    {
        internal static IOwnerBoundApiFactory ForActionCompletion(IActionCompletionApi api) =>
            Factory<IActionCompletionApi>((owner, active) => new ActionCompletionFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForActionSpeed(IActionSpeedApi api) =>
            Factory<IActionSpeedApi>((owner, active) => new ActionSpeedFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForItemTooltip(IItemTooltipApi api) =>
            Factory<IItemTooltipApi>((owner, active) => new ItemTooltipFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForAnimalViewer(IAnimalViewerApi api) =>
            Factory<IAnimalViewerApi>((owner, active) => new AnimalViewerFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForItemDisplayName(IItemDisplayNameApi api) =>
            Factory<IItemDisplayNameApi>((owner, active) => new ItemDisplayNameFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForLampControl(ILampControlApi api) =>
            Factory<ILampControlApi>((owner, active) => new LampControlFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForDebugConsole(IDebugConsoleApi api) =>
            Factory<IDebugConsoleApi>((owner, active) => new DebugConsoleFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForInventoryDebug(IInventoryDebugApi api) =>
            Factory<IInventoryDebugApi>((owner, active) => new InventoryDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForMailDelivery(IMailDeliveryApi api) =>
            Factory<IMailDeliveryApi>((owner, active) => new MailDeliveryFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForWeatherDebug(IWeatherDebugApi api) =>
            Factory<IWeatherDebugApi>((owner, active) => new WeatherDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForTeleportDebug(ITeleportDebugApi api) =>
            Factory<ITeleportDebugApi>((owner, active) => new TeleportDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForInstantSaveDebug(IInstantSaveDebugApi api) =>
            Factory<IInstantSaveDebugApi>((owner, active) => new InstantSaveDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForTimeDebug(ITimeDebugApi api) =>
            Factory<ITimeDebugApi>((owner, active) => new TimeDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForMovementDebug(IMovementDebugApi api) =>
            Factory<IMovementDebugApi>((owner, active) => new MovementDebugFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForEquipmentSlots(IEquipmentSlotsApi api) =>
            Factory<IEquipmentSlotsApi>((owner, active) => new EquipmentSlotsFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForSaveSlots(ISaveSlotsApi api) =>
            Factory<ISaveSlotsApi>((owner, active) => new SaveSlotsFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForCameraView(ICameraViewApi api) =>
            Factory<ICameraViewApi>((owner, active) => new CameraViewFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForCameraZoom(ICameraZoomApi api) =>
            Factory<ICameraZoomApi>((owner, active) => new CameraZoomFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForChestLocatorEnhancer(IChestLocatorEnhancerApi api) =>
            Factory<IChestLocatorEnhancerApi>((owner, active) => new ChestLocatorEnhancerFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForCropHarvesting(ICropHarvestingApi api) =>
            Factory<ICropHarvestingApi>((owner, active) => new CropHarvestingFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForAudioReplacement(IAudioReplacementApi api) =>
            Factory<IAudioReplacementApi>((owner, active) => new AudioReplacementFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForAdvancedDebug(IAdvancedDebugApi api) =>
            Factory<IAdvancedDebugApi>((owner, active) => new AdvancedDebugFacade(api, owner, active));

        private static IOwnerBoundApiFactory Factory<TApi>(Func<IManifest, Action, TApi> create) where TApi : class =>
            new TypedFactory<TApi>(create);

        private sealed class TypedFactory<TApi> : IOwnerBoundApiFactory where TApi : class
        {
            private readonly Func<IManifest, Action, TApi> create;

            public TypedFactory(Func<IManifest, Action, TApi> create)
            {
                this.create = create ?? throw new ArgumentNullException(nameof(create));
            }

            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
            {
                if (apiType != typeof(TApi))
                    throw new InvalidOperationException("Owner-bound GameBridge factory expected contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "', not '" + (apiType.FullName ?? apiType.Name) + "'.");
                return create(consumer, ensureOwnerActive);
            }
        }

        private abstract class FacadeBase
        {
            private readonly Action ensureOwnerActive;

            protected FacadeBase(IManifest owner, Action ensureOwnerActive)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                this.ensureOwnerActive = ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive));
            }

            protected IManifest Owner { get; }

            protected void EnsureActive()
            {
                ensureOwnerActive();
            }

            protected void EnsureOwner(IManifest supplied)
            {
                ensureOwnerActive();
                if (supplied == null || !string.Equals(supplied.UniqueID, Owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("GameBridge API for owner '" + Owner.UniqueID + "' can't mutate resources for another owner.");
            }
        }

        private sealed class ActionCompletionFacade : FacadeBase, IActionCompletionApi
        {
            private readonly IActionCompletionApi inner;
            public ActionCompletionFacade(IActionCompletionApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public void Configure(IManifest owner, ActionCompletionOptions options) { EnsureOwner(owner); inner.Configure(Owner, options); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class ActionSpeedFacade : FacadeBase, IActionSpeedApi
        {
            private readonly IActionSpeedApi inner;
            public ActionSpeedFacade(IActionSpeedApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public void Configure(IManifest owner, ActionSpeedOptions options) { EnsureOwner(owner); inner.Configure(Owner, options); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class ItemTooltipFacade : FacadeBase, IItemTooltipApi
        {
            private readonly IItemTooltipApi inner;
            public ItemTooltipFacade(IItemTooltipApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup) { EnsureOwner(owner); inner.ConfigureFishRoeProvider(Owner, options, lookup); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class AnimalViewerFacade : FacadeBase, IAnimalViewerApi
        {
            private readonly IAnimalViewerApi inner;
            public AnimalViewerFacade(IAnimalViewerApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options) { EnsureOwner(owner); inner.ConfigureSpecialProduceProgress(Owner, options); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class ItemDisplayNameFacade : FacadeBase, IItemDisplayNameApi
        {
            private readonly IItemDisplayNameApi inner;
            private bool successfulLookupObserved;
            public ItemDisplayNameFacade(IItemDisplayNameApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public bool TryGetDisplayName(string itemId, out string displayName)
            {
                EnsureActive();
                bool succeeded = inner.TryGetDisplayName(itemId, out displayName);
                if (succeeded && !successfulLookupObserved)
                {
                    successfulLookupObserved = true;
                    (inner as ItemDisplayNameService)?.ObserveOwnerLookup(Owner.UniqueID, itemId);
                }
                return succeeded;
            }
        }

        private sealed class LampControlFacade : FacadeBase, ILampControlApi
        {
            private readonly ILampControlApi inner;
            public LampControlFacade(ILampControlApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public LampManualToggleRegisterResult RegisterManualToggle(IManifest owner, LampManualToggleOptions options) { EnsureOwner(owner); return inner.RegisterManualToggle(Owner, options); }
            public LampManualToggleState GetState(string uniqueId) { EnsureOwnerId(uniqueId); return inner.GetState(Owner.UniqueID); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureOwnerId(uniqueId); return inner.GetStatus(Owner.UniqueID); }

            private void EnsureOwnerId(string uniqueId)
            {
                EnsureActive();
                if (!string.Equals(uniqueId, Owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Retired Lamp compatibility API for owner '" + Owner.UniqueID + "' can't query another owner.");
            }
        }

        private sealed class DebugConsoleFacade : FacadeBase, IDebugConsoleApi, IOwnerBoundApiFacade
        {
            private readonly IDebugConsoleApi inner;
            private bool deactivated;
            public DebugConsoleFacade(IDebugConsoleApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public bool IsOpen { get { EnsureActive(); return inner.IsOpen; } }
            public void Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null) { EnsureOwner(owner); inner.Bind(Owner, inventoryApi, weatherApi, teleportApi, timeApi, movementApi, instantSaveApi); }
            public void BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi) { EnsureOwner(owner); inner.BindAdvanced(Owner, advancedDebugApi); }
            public void SetLanguage(IManifest owner, string language) { EnsureOwner(owner); inner.SetLanguage(Owner, language); }
            public void Open(IManifest owner, string reason) { EnsureOwner(owner); inner.Open(Owner, reason); }
            public void Close(IManifest owner, string reason) { EnsureOwner(owner); inner.Close(Owner, reason); }
            public void Toggle(IManifest owner, string reason) { EnsureOwner(owner); inner.Toggle(Owner, reason); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
            public void Deactivate()
            {
                if (deactivated)
                    return;
                deactivated = true;
                (inner as IOwnerBoundApiHost)?.RemoveOwner(Owner.UniqueID, "owner facade deactivated");
            }
        }

        private sealed class InventoryDebugFacade : FacadeBase, IInventoryDebugApi
        {
            private readonly IInventoryDebugApi inner;
            public InventoryDebugFacade(IInventoryDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public InventoryDebugPage GetItems(InventoryDebugQuery query) { EnsureActive(); return inner.GetItems(query); }
            public InventoryGiveResult GiveItem(IManifest owner, string itemId, int count) { EnsureOwner(owner); return inner.GiveItem(Owner, itemId, count); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class MailDeliveryFacade : FacadeBase, IMailDeliveryApi
        {
            private readonly IMailDeliveryApi inner;
            public MailDeliveryFacade(IMailDeliveryApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public MailItemDeliveryResult SendItemMail(IManifest owner, MailItemDeliveryRequest request) { EnsureOwner(owner); return inner.SendItemMail(Owner, request); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class WeatherDebugFacade : FacadeBase, IWeatherDebugApi
        {
            private readonly IWeatherDebugApi inner;
            public WeatherDebugFacade(IWeatherDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public WeatherDebugState GetState() { EnsureActive(); return inner.GetState(); }
            public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers() { EnsureActive(); return inner.GetAvailableWeathers(); }
            public WeatherSetResult SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod) { EnsureOwner(owner); return inner.SetWeather(Owner, weatherId, patchCurrentPeriod); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class TeleportDebugFacade : FacadeBase, ITeleportDebugApi
        {
            private readonly ITeleportDebugApi inner;
            public TeleportDebugFacade(ITeleportDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public IReadOnlyList<TeleportDestination> GetDestinations() { EnsureActive(); return inner.GetDestinations(); }
            public TeleportSnapshot GetCurrentSnapshot() { EnsureActive(); return inner.GetCurrentSnapshot(); }
            public TeleportResult Teleport(IManifest owner, string destinationId) { EnsureOwner(owner); return inner.Teleport(Owner, destinationId); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class InstantSaveDebugFacade : FacadeBase, IInstantSaveDebugApi
        {
            private readonly IInstantSaveDebugApi inner;
            public InstantSaveDebugFacade(IInstantSaveDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public InstantSaveDebugState GetState() { EnsureActive(); return inner.GetState(); }
            public InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave) { EnsureOwner(owner); return inner.Save(Owner, reloadAfterSave); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class TimeDebugFacade : FacadeBase, ITimeDebugApi
        {
            private readonly ITimeDebugApi inner;
            public TimeDebugFacade(ITimeDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public TimeDebugState GetState() { EnsureActive(); return inner.GetState(); }
            public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner) { EnsureOwner(owner); return inner.SkipToNextWeatherPeriod(Owner); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class MovementDebugFacade : FacadeBase, IMovementDebugApi
        {
            private readonly IMovementDebugApi inner;
            public MovementDebugFacade(IMovementDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public MovementDebugState GetState() { EnsureActive(); return inner.GetState(); }
            public MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier) { EnsureOwner(owner); return inner.SetSpeedMultiplier(Owner, multiplier); }
            public MovementSpeedResult ResetSpeed(IManifest owner, string reason) { EnsureOwner(owner); return inner.ResetSpeed(Owner, reason); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }

        private sealed class EquipmentSlotsFacade : FacadeBase, IEquipmentSlotsApi
        {
            private readonly IEquipmentSlotsApi inner;
            public EquipmentSlotsFacade(IEquipmentSlotsApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public EquipmentSlotsRegisterResult RegisterSlots(IManifest owner, EquipmentSlotsOptions options) { EnsureOwner(owner); return inner.RegisterSlots(Owner, options); }
            public IReadOnlyList<EquipmentSlotInfo> GetSlots(string uniqueId) { EnsureActive(); return inner.GetSlots(uniqueId); }
            public EquipmentSlotEquipResult EquipExtraSlot(IManifest owner, string slotId, string itemId) { EnsureOwner(owner); return inner.EquipExtraSlot(Owner, slotId, itemId); }
            public EquipmentSlotEquipResult UnequipExtraSlot(IManifest owner, string slotId, string reason) { EnsureOwner(owner); return inner.UnequipExtraSlot(Owner, slotId, reason); }
            public EquipmentSlotsState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public EquipmentSlotsRecoveryResult RecoverExtraSlotItems(IManifest owner, string reason) { EnsureOwner(owner); return inner.RecoverExtraSlotItems(Owner, reason); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class SaveSlotsFacade : FacadeBase, ISaveSlotsApi
        {
            private readonly ISaveSlotsApi inner;
            public SaveSlotsFacade(ISaveSlotsApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions options) { EnsureOwner(owner); return inner.RegisterSlots(Owner, options); }
            public SaveSlotsState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class CameraViewFacade : FacadeBase, ICameraViewApi
        {
            private readonly ICameraViewApi inner;
            public CameraViewFacade(ICameraViewApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public ICameraViewLease AcquireLease(IManifest owner, CameraViewRequest request) { EnsureOwner(owner); return new CameraViewLeaseFacade(inner.AcquireLease(Owner, request), EnsureActive); }
            public CameraViewState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public CameraViewState GetSnapshot(string uniqueId) { EnsureActive(); return inner.GetSnapshot(uniqueId); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class CameraViewLeaseFacade : ICameraViewLease
        {
            private readonly ICameraViewLease inner;
            private readonly Action ensureOwnerActive;
            public CameraViewLeaseFacade(ICameraViewLease inner, Action ensureOwnerActive)
            {
                this.inner = inner ?? throw new InvalidOperationException("Camera view API returned a null lease.");
                this.ensureOwnerActive = ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive));
            }
            public string LeaseId => inner.LeaseId;
            public string OwnerId => inner.OwnerId;
            public bool IsReleased => inner.IsReleased;
            public CameraViewResult LastResult => inner.LastResult;
            public CameraViewResult SetViewScale(double viewScale, string reason) { ensureOwnerActive(); return inner.SetViewScale(viewScale, reason); }
            public CameraViewResult Update(CameraViewRequest request, string reason) { ensureOwnerActive(); return inner.Update(request, reason); }
            public CameraViewResult Release(string reason) => inner.Release(reason);
            public CameraViewState GetState() { ensureOwnerActive(); return inner.GetState(); }
            public void Dispose() => inner.Dispose();
        }

        private sealed class CameraZoomFacade : FacadeBase, ICameraZoomApi
        {
            private readonly ICameraZoomApi inner;
            public CameraZoomFacade(ICameraZoomApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public CameraZoomRegisterResult Register(IManifest owner, CameraZoomOptions options) { EnsureOwner(owner); return inner.Register(Owner, options); }
            public CameraZoomResult SetViewScale(IManifest owner, double viewScale, string reason) { EnsureOwner(owner); return inner.SetViewScale(Owner, viewScale, reason); }
            public CameraZoomResult StepViewScale(IManifest owner, int direction, string reason) { EnsureOwner(owner); return inner.StepViewScale(Owner, direction, reason); }
            public CameraZoomResult ResetViewScale(IManifest owner, string reason) { EnsureOwner(owner); return inner.ResetViewScale(Owner, reason); }
            public CameraZoomState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public CameraZoomState GetSnapshot(string uniqueId) { EnsureActive(); return inner.GetSnapshot(uniqueId); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class ChestLocatorEnhancerFacade : FacadeBase, IChestLocatorEnhancerApi
        {
            private readonly IChestLocatorEnhancerApi inner;
            public ChestLocatorEnhancerFacade(IChestLocatorEnhancerApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public ChestLocatorEnhancerRegisterResult Register(IManifest owner, ChestLocatorEnhancerOptions options) { EnsureOwner(owner); return inner.Register(Owner, options); }
            public ChestLocatorEnhancerState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class CropHarvestingFacade : FacadeBase, ICropHarvestingApi
        {
            private readonly ICropHarvestingApi inner;
            public CropHarvestingFacade(ICropHarvestingApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request) { EnsureOwner(owner); return inner.ScanMatureCrops(Owner, request); }
            public CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request) { EnsureOwner(owner); return inner.HarvestMatureCrops(Owner, request); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class AudioReplacementFacade : FacadeBase, IAudioReplacementApi
        {
            private readonly IAudioReplacementApi inner;
            public AudioReplacementFacade(IAudioReplacementApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public AudioReplacementRegisterResult RegisterReplacement(IManifest owner, AudioReplacementOptions options) { EnsureOwner(owner); return inner.RegisterReplacement(Owner, options); }
            public AudioReplacementState GetState(string uniqueId) { EnsureActive(); return inner.GetState(uniqueId); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }

        private sealed class AdvancedDebugFacade : FacadeBase, IAdvancedDebugApi
        {
            private readonly IAdvancedDebugApi inner;
            public AdvancedDebugFacade(IAdvancedDebugApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public IReadOnlyList<TechPointDebugOption> GetTechPointOptions() { EnsureActive(); return inner.GetTechPointOptions(); }
            public IReadOnlyList<SpawnDebugOption> GetMonsterOptions() { EnsureActive(); return inner.GetMonsterOptions(); }
            public IReadOnlyList<SpawnDebugOption> GetResourceOptions() { EnsureActive(); return inner.GetResourceOptions(); }
            public CreativeModeState GetCreativeModeState() { EnsureActive(); return inner.GetCreativeModeState(); }
            public TimeSkipResult AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount) { EnsureOwner(owner); return inner.AdvanceTime(Owner, kind, amount); }
            public TimeScaleDebugResult SetTimeScale(IManifest owner, double multiplier) { EnsureOwner(owner); return inner.SetTimeScale(Owner, multiplier); }
            public TimeScaleDebugResult ResetTimeScale(IManifest owner, string reason) { EnsureOwner(owner); return inner.ResetTimeScale(Owner, reason); }
            public DebugValueResult AddMoney(IManifest owner, int amount) { EnsureOwner(owner); return inner.AddMoney(Owner, amount); }
            public DebugValueResult AddTechPoint(IManifest owner, string pointTypeId, int amount) { EnsureOwner(owner); return inner.AddTechPoint(Owner, pointTypeId, amount); }
            public DebugCommandResult UnlockAllTechTrees(IManifest owner) { EnsureOwner(owner); return inner.UnlockAllTechTrees(Owner); }
            public CropMaturityResult MatureAllCrops(IManifest owner) { EnsureOwner(owner); return inner.MatureAllCrops(Owner); }
            public CreativeModeResult SetCreativeMode(IManifest owner, bool enabled) { EnsureOwner(owner); return inner.SetCreativeMode(Owner, enabled); }
            public InventoryGiveResult GiveCreativeGenerator(IManifest owner) { EnsureOwner(owner); return inner.GiveCreativeGenerator(Owner); }
            public SpawnDebugResult SpawnMonster(IManifest owner, string monsterId, int count) { EnsureOwner(owner); return inner.SpawnMonster(Owner, monsterId, count); }
            public SpawnDebugResult SpawnResource(IManifest owner, string resourceId, int count) { EnsureOwner(owner); return inner.SpawnResource(Owner, resourceId, count); }
            public BridgeFeatureStatus GetStatus() { EnsureActive(); return inner.GetStatus(); }
        }
    }
}
