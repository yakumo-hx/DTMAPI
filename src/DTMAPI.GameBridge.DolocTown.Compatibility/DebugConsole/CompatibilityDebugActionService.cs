#pragma warning disable CS0618 // This service exists only for the frozen 0.3.1 ABI.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.DebugConsole
{
    /// <summary>
    /// Optional Compatibility-owned implementation of the seven frozen
    /// DebugConsole action APIs. Mandatory GameBridge providers reflect into
    /// this service and contain no native action body.
    /// </summary>
    internal sealed class CompatibilityDebugActionService :
        IInventoryDebugApi,
        IWeatherDebugApi,
        ITeleportDebugApi,
        IInstantSaveDebugApi,
        ITimeDebugApi,
        IMovementDebugApi,
        IAdvancedDebugApi,
        IOwnerBoundApiHost
    {
        internal CompatibilityDebugConsoleRuntimeAdapter Runtime { get; }
        internal DebugConsoleNativeActions Actions { get; }
        private int updateCount;
        private int saveBoundaryResetCount;
        private int titleBoundaryResetCount;
        private int shutdownCount;

        internal CompatibilityDebugActionService(DtmApiRuntime runtime)
        {
            Runtime = new CompatibilityDebugConsoleRuntimeAdapter(runtime);
            Actions = new DebugConsoleNativeActions(Runtime);
        }

        public InventoryDebugPage GetItems(InventoryDebugQuery query) =>
            Actions.GetItems(query);

        public InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count) =>
            Actions.GiveItem(owner, itemId, count);

        BridgeFeatureStatus IInventoryDebugApi.GetStatus() =>
            ((IInventoryActions)Actions).GetStatus();

        public WeatherDebugState GetState() =>
            ((IWeatherActions)Actions).GetState();

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers() =>
            Actions.GetAvailableWeathers();

        public WeatherSetResult SetWeather(
            IManifest owner,
            string weatherId,
            bool patchCurrentPeriod) =>
            Actions.SetWeather(owner, weatherId, patchCurrentPeriod);

        BridgeFeatureStatus IWeatherDebugApi.GetStatus() =>
            ((IWeatherActions)Actions).GetStatus();

        public IReadOnlyList<TeleportDestination> GetDestinations() =>
            Actions.GetDestinations();

        public TeleportSnapshot GetCurrentSnapshot() =>
            Actions.GetCurrentSnapshot();

        public TeleportResult Teleport(
            IManifest owner,
            string destinationId) =>
            Actions.Teleport(owner, destinationId);

        BridgeFeatureStatus ITeleportDebugApi.GetStatus() =>
            ((ITeleportActions)Actions).GetStatus();

        InstantSaveDebugState IInstantSaveDebugApi.GetState() =>
            ((IInstantSaveActions)Actions).GetState();

        public InstantSaveDebugResult Save(
            IManifest owner,
            bool reloadAfterSave) =>
            Actions.Save(owner, reloadAfterSave);

        BridgeFeatureStatus IInstantSaveDebugApi.GetStatus() =>
            ((IInstantSaveActions)Actions).GetStatus();

        TimeDebugState ITimeDebugApi.GetState() =>
            ((ITimeActions)Actions).GetState();

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner) =>
            Actions.SkipToNextWeatherPeriod(owner);

        BridgeFeatureStatus ITimeDebugApi.GetStatus() =>
            ((ITimeActions)Actions).GetStatus();

        MovementDebugState IMovementDebugApi.GetState() =>
            ((IMovementActions)Actions).GetState();

        public MovementSpeedResult SetSpeedMultiplier(
            IManifest owner,
            double multiplier) =>
            Actions.SetSpeedMultiplier(owner, multiplier);

        public MovementSpeedResult ResetSpeed(
            IManifest owner,
            string reason) =>
            Actions.ResetSpeed(owner, reason);

        BridgeFeatureStatus IMovementDebugApi.GetStatus() =>
            ((IMovementActions)Actions).GetStatus();

        public IReadOnlyList<TechPointDebugOption> GetTechPointOptions() =>
            Actions.GetTechPointOptions();

        public IReadOnlyList<SpawnDebugOption> GetMonsterOptions() =>
            Actions.GetMonsterOptions();

        public IReadOnlyList<SpawnDebugOption> GetResourceOptions() =>
            Actions.GetResourceOptions();

        public CreativeModeState GetCreativeModeState() =>
            Actions.GetCreativeModeState();

        public TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount) =>
            Actions.AdvanceTime(owner, kind, amount);

        public TimeScaleDebugResult SetTimeScale(
            IManifest owner,
            double multiplier) =>
            Actions.SetTimeScale(owner, multiplier);

        public TimeScaleDebugResult ResetTimeScale(
            IManifest owner,
            string reason) =>
            Actions.ResetTimeScale(owner, reason);

        public DebugValueResult AddMoney(IManifest owner, int amount) =>
            Actions.AddMoney(owner, amount);

        public DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount) =>
            Actions.AddTechPoint(owner, pointTypeId, amount);

        public DebugCommandResult UnlockAllTechTrees(IManifest owner) =>
            Actions.UnlockAllTechTrees(owner);

        public CropMaturityResult MatureAllCrops(IManifest owner) =>
            Actions.MatureAllCrops(owner);

        public CreativeModeResult SetCreativeMode(
            IManifest owner,
            bool enabled) =>
            Actions.SetCreativeMode(owner, enabled);

        public InventoryGiveResult GiveCreativeGenerator(IManifest owner) =>
            Actions.GiveCreativeGenerator(owner);

        public SpawnDebugResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count) =>
            Actions.SpawnMonster(owner, monsterId, count).Result;

        public SpawnDebugResult SpawnResource(
            IManifest owner,
            string resourceId,
            int count) =>
            Actions.SpawnResource(owner, resourceId, count);

        BridgeFeatureStatus IAdvancedDebugApi.GetStatus() =>
            ((IAdvancedActions)Actions).GetStatus();

        public int CountOwnerResources(string ownerId) =>
            Actions.CountOwnerResources(ownerId);

        public int RemoveOwner(string ownerId, string reason) =>
            Actions.RemoveOwner(ownerId, reason);

        internal void Update()
        {
            updateCount++;
            Actions.Update();
        }

        internal void ResetForTitleBoundary()
        {
            titleBoundaryResetCount++;
            Actions.RestoreTransientState("ReturnedToTitle");
        }

        internal void ResetForSaveBoundary()
        {
            saveBoundaryResetCount++;
            Actions.RestoreTransientState("SaveLoaded");
        }

        internal void Shutdown(string reason)
        {
            shutdownCount++;
            Actions.RestoreTransientState(reason);
            Runtime.ShutdownHooks(reason);
        }

        internal string GetLifecycleSummary() =>
            Actions.BuildLeaseSummary() +
            "; compatibilityPatches=" +
            Runtime.InstalledPatchCount +
            "; lifecycleUpdates=" + updateCount +
            "; saveBoundaryResets=" +
            saveBoundaryResetCount +
            "; titleBoundaryResets=" +
            titleBoundaryResetCount +
            "; shutdowns=" + shutdownCount;
    }
}
