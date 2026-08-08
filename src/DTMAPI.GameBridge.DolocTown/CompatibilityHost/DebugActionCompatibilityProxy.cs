#pragma warning disable CS0618 // Exact frozen diagnostic API proxy.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Mandatory ABI-only proxy for the seven frozen DebugConsole action
    /// contracts. Every implementation call is reflected into the optional
    /// Compatibility component.
    /// </summary>
    internal sealed class DebugActionCompatibilityProxy :
        IInventoryDebugApi,
        IWeatherDebugApi,
        ITeleportDebugApi,
        IInstantSaveDebugApi,
        ITimeDebugApi,
        IMovementDebugApi,
        IAdvancedDebugApi,
        IOwnerBoundApiHost
    {
        private readonly CompatibilityHostBroker broker;

        internal DebugActionCompatibilityProxy(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(
                runtime ?? throw new ArgumentNullException(nameof(runtime)));
        }

        private object Backend =>
            broker.GetService("DebugActions", Array.Empty<object>());

        public InventoryDebugPage GetItems(InventoryDebugQuery query) =>
            Invoke<InventoryDebugPage>("GetItems", query);

        public InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count) =>
            Invoke<InventoryGiveResult>("GiveItem", owner, itemId, count);

        BridgeFeatureStatus IInventoryDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.IInventoryDebugApi.GetStatus");

        public WeatherDebugState GetState() =>
            Invoke<WeatherDebugState>("GetState");

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers() =>
            Invoke<IReadOnlyList<WeatherDebugOption>>("GetAvailableWeathers");

        public WeatherSetResult SetWeather(
            IManifest owner,
            string weatherId,
            bool patchCurrentPeriod) =>
            Invoke<WeatherSetResult>(
                "SetWeather",
                owner,
                weatherId,
                patchCurrentPeriod);

        BridgeFeatureStatus IWeatherDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.IWeatherDebugApi.GetStatus");

        public IReadOnlyList<TeleportDestination> GetDestinations() =>
            Invoke<IReadOnlyList<TeleportDestination>>("GetDestinations");

        public TeleportSnapshot GetCurrentSnapshot() =>
            Invoke<TeleportSnapshot>("GetCurrentSnapshot");

        public TeleportResult Teleport(
            IManifest owner,
            string destinationId) =>
            Invoke<TeleportResult>("Teleport", owner, destinationId);

        public TeleportCsvExportResult ExportDestinationsCsv(
            IManifest owner) =>
            Invoke<TeleportCsvExportResult>("ExportDestinationsCsv", owner);

        BridgeFeatureStatus ITeleportDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.ITeleportDebugApi.GetStatus");

        InstantSaveDebugState IInstantSaveDebugApi.GetState() =>
            Invoke<InstantSaveDebugState>("DTMAPI.Abstractions.IInstantSaveDebugApi.GetState");

        public InstantSaveDebugResult Save(
            IManifest owner,
            bool reloadAfterSave) =>
            Invoke<InstantSaveDebugResult>("Save", owner, reloadAfterSave);

        BridgeFeatureStatus IInstantSaveDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.IInstantSaveDebugApi.GetStatus");

        TimeDebugState ITimeDebugApi.GetState() =>
            Invoke<TimeDebugState>("DTMAPI.Abstractions.ITimeDebugApi.GetState");

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner) =>
            Invoke<TimeSkipResult>("SkipToNextWeatherPeriod", owner);

        BridgeFeatureStatus ITimeDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.ITimeDebugApi.GetStatus");

        MovementDebugState IMovementDebugApi.GetState() =>
            Invoke<MovementDebugState>("DTMAPI.Abstractions.IMovementDebugApi.GetState");

        public MovementSpeedResult SetSpeedMultiplier(
            IManifest owner,
            double multiplier) =>
            Invoke<MovementSpeedResult>(
                "SetSpeedMultiplier",
                owner,
                multiplier);

        public MovementSpeedResult ResetSpeed(
            IManifest owner,
            string reason) =>
            Invoke<MovementSpeedResult>("ResetSpeed", owner, reason);

        BridgeFeatureStatus IMovementDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.IMovementDebugApi.GetStatus");

        public IReadOnlyList<TechPointDebugOption> GetTechPointOptions() =>
            Invoke<IReadOnlyList<TechPointDebugOption>>("GetTechPointOptions");

        public IReadOnlyList<SpawnDebugOption> GetMonsterOptions() =>
            Invoke<IReadOnlyList<SpawnDebugOption>>("GetMonsterOptions");

        public IReadOnlyList<SpawnDebugOption> GetResourceOptions() =>
            Invoke<IReadOnlyList<SpawnDebugOption>>("GetResourceOptions");

        public CreativeModeState GetCreativeModeState() =>
            Invoke<CreativeModeState>("GetCreativeModeState");

        public TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount) =>
            Invoke<TimeSkipResult>("AdvanceTime", owner, kind, amount);

        public TimeScaleDebugResult SetTimeScale(
            IManifest owner,
            double multiplier) =>
            Invoke<TimeScaleDebugResult>("SetTimeScale", owner, multiplier);

        public TimeScaleDebugResult ResetTimeScale(
            IManifest owner,
            string reason) =>
            Invoke<TimeScaleDebugResult>("ResetTimeScale", owner, reason);

        public DebugValueResult AddMoney(IManifest owner, int amount) =>
            Invoke<DebugValueResult>("AddMoney", owner, amount);

        public DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount) =>
            Invoke<DebugValueResult>(
                "AddTechPoint",
                owner,
                pointTypeId,
                amount);

        public DebugCommandResult UnlockAllTechTrees(IManifest owner) =>
            Invoke<DebugCommandResult>("UnlockAllTechTrees", owner);

        public CropMaturityResult MatureAllCrops(IManifest owner) =>
            Invoke<CropMaturityResult>("MatureAllCrops", owner);

        public CreativeModeResult SetCreativeMode(
            IManifest owner,
            bool enabled) =>
            Invoke<CreativeModeResult>("SetCreativeMode", owner, enabled);

        public InventoryGiveResult GiveCreativeGenerator(IManifest owner) =>
            Invoke<InventoryGiveResult>("GiveCreativeGenerator", owner);

        public SpawnDebugResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count) =>
            Invoke<SpawnDebugResult>(
                "SpawnMonster",
                owner,
                monsterId,
                count);

        public SpawnDebugResult SpawnResource(
            IManifest owner,
            string resourceId,
            int count) =>
            Invoke<SpawnDebugResult>(
                "SpawnResource",
                owner,
                resourceId,
                count);

        BridgeFeatureStatus IAdvancedDebugApi.GetStatus() =>
            Invoke<BridgeFeatureStatus>("DTMAPI.Abstractions.IAdvancedDebugApi.GetStatus");

        public int CountOwnerResources(string ownerId) =>
            TryInvoke("CountOwnerResources", 0, ownerId);

        internal bool IsLoaded =>
            TryGet(out _);

        public int RemoveOwner(string ownerId, string reason) =>
            TryInvoke("RemoveOwner", 0, ownerId, reason);

        internal void UpdateIfLoaded()
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(backend!, "Update");
        }

        internal void ResetForTitleBoundaryIfLoaded()
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(
                    backend!,
                    "ResetForTitleBoundary");
        }

        internal void ResetForSaveBoundaryIfLoaded()
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(
                    backend!,
                    "ResetForSaveBoundary");
        }

        internal void ShutdownIfLoaded(string reason)
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(
                    backend!,
                    "Shutdown",
                    reason);
        }

        internal string GetLifecycleSummary() =>
            TryInvoke("GetLifecycleSummary", "debugActions=resident-dormant");

        private T Invoke<T>(string method, params object?[] arguments) =>
            CompatibilityHostBroker.Invoke<T>(Backend, method, arguments);

        private bool TryGet(out object? backend) =>
            broker.TryGetService("DebugActions", out backend);

        private T TryInvoke<T>(
            string method,
            T fallback,
            params object?[] arguments) =>
            TryGet(out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(
                    backend,
                    method,
                    arguments)
                : fallback;
    }
}
