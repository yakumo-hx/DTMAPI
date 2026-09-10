#pragma warning disable CS0618 // QA exercises the exact frozen compatibility contracts.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// QA-only selector for the admitted ProductNative action engine or the
    /// frozen Compatibility proxies. It never changes production ownership.
    /// </summary>
    internal sealed class DebugConsoleActionFixtureAdapter :
        IInventoryDebugApi,
        IWeatherDebugApi,
        IInstantSaveDebugApi,
        ITimeDebugApi,
        IMovementDebugApi,
        IAdvancedDebugApi
    {
        private const string ProductOwnerId = "DTMAPI.DebugConsoleMod";
        private readonly DolocTownGameBridge bridge;
        private readonly object? productActions;

        internal DebugConsoleActionFixtureAdapter(
            DtmApiRuntime runtime,
            DolocTownGameBridge bridge)
        {
            this.bridge = bridge ??
                throw new ArgumentNullException(nameof(bridge));
            productActions = FindProductActions(
                runtime ?? throw new ArgumentNullException(nameof(runtime)));
        }

        internal string OwnerKind =>
            productActions == null
                ? "Compatibility"
                : "ProductNative";

        internal bool CompatibilityHostAssemblyLoaded =>
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                string.Equals(
                    assembly.GetName().Name,
                    "DTMAPI.GameBridge.DolocTown.Compatibility",
                    StringComparison.Ordinal));

        internal void ValidateOwnerBoundaryAfterAction()
        {
            bool compatibilityActionServiceLoaded =
                bridge.DebugActionCompatibilityLoadedForQa;
            if (productActions != null &&
                compatibilityActionServiceLoaded)
            {
                throw new InvalidOperationException(
                    "ProductNative DebugConsole action execution unexpectedly loaded the Compatibility DebugActions service.");
            }
            if (productActions == null &&
                !compatibilityActionServiceLoaded)
            {
                throw new InvalidOperationException(
                    "Frozen DebugConsole action execution did not load the current Compatibility DebugActions service.");
            }
        }

        internal object? ProductEntry =>
            productActions == null
                ? null
                : FindProductEntry(bridge.RuntimeForQa);

        public InventoryDebugPage GetItems(InventoryDebugQuery query) =>
            productActions == null
                ? bridge.InventoryDebugApiForQa.GetItems(query)
                : Invoke<InventoryDebugPage>("GetItems", query);

        public InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count) =>
            productActions == null
                ? bridge.InventoryDebugApiForQa.GiveItem(owner, itemId, count)
                : Invoke<InventoryGiveResult>("GiveItem", owner, itemId, count);

        BridgeFeatureStatus IInventoryDebugApi.GetStatus() =>
            productActions == null
                ? bridge.InventoryDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        public WeatherDebugState GetState() =>
            productActions == null
                ? bridge.WeatherDebugApiForQa.GetState()
                : Invoke<WeatherDebugState>("GetState");

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers() =>
            productActions == null
                ? bridge.WeatherDebugApiForQa.GetAvailableWeathers()
                : Invoke<IReadOnlyList<WeatherDebugOption>>(
                    "GetAvailableWeathers");

        public WeatherSetResult SetWeather(
            IManifest owner,
            string weatherId,
            bool patchCurrentPeriod) =>
            productActions == null
                ? bridge.WeatherDebugApiForQa.SetWeather(
                    owner,
                    weatherId,
                    patchCurrentPeriod)
                : Invoke<WeatherSetResult>(
                    "SetWeather",
                    owner,
                    weatherId,
                    patchCurrentPeriod);

        BridgeFeatureStatus IWeatherDebugApi.GetStatus() =>
            productActions == null
                ? bridge.WeatherDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        public IReadOnlyList<TeleportDestination> GetDestinations() =>
            productActions == null
                ? bridge.TeleportDebugApiForQa.GetDestinations()
                : Invoke<IReadOnlyList<TeleportDestination>>(
                    "GetDestinations");

        public TeleportSnapshot GetCurrentSnapshot() =>
            productActions == null
                ? bridge.TeleportDebugApiForQa.GetCurrentSnapshot()
                : Invoke<TeleportSnapshot>("GetCurrentSnapshot");

        public TeleportResult Teleport(
            IManifest owner,
            string destinationId) =>
            productActions == null
                ? bridge.TeleportDebugApiForQa.Teleport(
                    owner,
                    destinationId)
                : Invoke<TeleportResult>(
                    "Teleport",
                    owner,
                    destinationId);

        // Keep teleport calls on this concrete QA facade. Runtime 0.6.1 still
        // exposes the retired ExportDestinationsCsv interface slot; directly
        // implementing the current, smaller ITeleportDebugApi makes Mono reject
        // this type before any retained teleport member can be exercised.
        internal BridgeFeatureStatus GetTeleportStatus() =>
            productActions == null
                ? bridge.TeleportDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        InstantSaveDebugState IInstantSaveDebugApi.GetState() =>
            productActions == null
                ? bridge.InstantSaveDebugApiForQa.GetState()
                : Invoke<InstantSaveDebugState>("GetState");

        public InstantSaveDebugResult Save(
            IManifest owner,
            bool reloadAfterSave) =>
            productActions == null
                ? bridge.InstantSaveDebugApiForQa.Save(
                    owner,
                    reloadAfterSave)
                : Invoke<InstantSaveDebugResult>(
                    "Save",
                    owner,
                    reloadAfterSave);

        BridgeFeatureStatus IInstantSaveDebugApi.GetStatus() =>
            productActions == null
                ? bridge.InstantSaveDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        TimeDebugState ITimeDebugApi.GetState() =>
            productActions == null
                ? bridge.TimeDebugApiForQa.GetState()
                : Invoke<TimeDebugState>("GetState");

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner) =>
            productActions == null
                ? bridge.TimeDebugApiForQa.SkipToNextWeatherPeriod(owner)
                : Invoke<TimeSkipResult>(
                    "SkipToNextWeatherPeriod",
                    owner);

        BridgeFeatureStatus ITimeDebugApi.GetStatus() =>
            productActions == null
                ? bridge.TimeDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        MovementDebugState IMovementDebugApi.GetState() =>
            productActions == null
                ? bridge.MovementDebugApiForQa.GetState()
                : Invoke<MovementDebugState>("GetState");

        public MovementSpeedResult SetSpeedMultiplier(
            IManifest owner,
            double multiplier) =>
            productActions == null
                ? bridge.MovementDebugApiForQa.SetSpeedMultiplier(
                    owner,
                    multiplier)
                : Invoke<MovementSpeedResult>(
                    "SetSpeedMultiplier",
                    owner,
                    multiplier);

        public MovementSpeedResult ResetSpeed(
            IManifest owner,
            string reason) =>
            productActions == null
                ? bridge.MovementDebugApiForQa.ResetSpeed(owner, reason)
                : Invoke<MovementSpeedResult>(
                    "ResetSpeed",
                    owner,
                    reason);

        BridgeFeatureStatus IMovementDebugApi.GetStatus() =>
            productActions == null
                ? bridge.MovementDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        public IReadOnlyList<TechPointDebugOption> GetTechPointOptions() =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GetTechPointOptions()
                : Invoke<IReadOnlyList<TechPointDebugOption>>(
                    "GetTechPointOptions");

        public IReadOnlyList<SpawnDebugOption> GetMonsterOptions() =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GetMonsterOptions()
                : Invoke<IReadOnlyList<SpawnDebugOption>>(
                    "GetMonsterOptions");

        public IReadOnlyList<SpawnDebugOption> GetResourceOptions() =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GetResourceOptions()
                : Invoke<IReadOnlyList<SpawnDebugOption>>(
                    "GetResourceOptions");

        public CreativeModeState GetCreativeModeState() =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GetCreativeModeState()
                : Invoke<CreativeModeState>("GetCreativeModeState");

        public TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.AdvanceTime(
                    owner,
                    kind,
                    amount)
                : Invoke<TimeSkipResult>(
                    "AdvanceTime",
                    owner,
                    kind,
                    amount);

        public TimeScaleDebugResult SetTimeScale(
            IManifest owner,
            double multiplier) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.SetTimeScale(
                    owner,
                    multiplier)
                : Invoke<TimeScaleDebugResult>(
                    "SetTimeScale",
                    owner,
                    multiplier);

        public TimeScaleDebugResult ResetTimeScale(
            IManifest owner,
            string reason) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.ResetTimeScale(
                    owner,
                    reason)
                : Invoke<TimeScaleDebugResult>(
                    "ResetTimeScale",
                    owner,
                    reason);

        public DebugValueResult AddMoney(
            IManifest owner,
            int amount) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.AddMoney(owner, amount)
                : Invoke<DebugValueResult>(
                    "AddMoney",
                    owner,
                    amount);

        public DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.AddTechPoint(
                    owner,
                    pointTypeId,
                    amount)
                : Invoke<DebugValueResult>(
                    "AddTechPoint",
                    owner,
                    pointTypeId,
                    amount);

        public DebugCommandResult UnlockAllTechTrees(IManifest owner) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.UnlockAllTechTrees(owner)
                : Invoke<DebugCommandResult>(
                    "UnlockAllTechTrees",
                    owner);

        public CropMaturityResult MatureAllCrops(IManifest owner) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.MatureAllCrops(owner)
                : Invoke<CropMaturityResult>(
                    "MatureAllCrops",
                    owner);

        public CreativeModeResult SetCreativeMode(
            IManifest owner,
            bool enabled) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.SetCreativeMode(
                    owner,
                    enabled)
                : Invoke<CreativeModeResult>(
                    "SetCreativeMode",
                    owner,
                    enabled);

        public InventoryGiveResult GiveCreativeGenerator(
            IManifest owner) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GiveCreativeGenerator(owner)
                : Invoke<InventoryGiveResult>(
                    "GiveCreativeGenerator",
                    owner);

        public SpawnDebugResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.SpawnMonster(
                    owner,
                    monsterId,
                    count)
                : InvokeSpawnResult(
                    "SpawnMonster",
                    owner,
                    monsterId,
                    count);

        public SpawnDebugResult SpawnResource(
            IManifest owner,
            string resourceId,
            int count) =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.SpawnResource(
                    owner,
                    resourceId,
                    count)
                : Invoke<SpawnDebugResult>(
                    "SpawnResource",
                    owner,
                    resourceId,
                    count);

        BridgeFeatureStatus IAdvancedDebugApi.GetStatus() =>
            productActions == null
                ? bridge.AdvancedDebugApiForQa.GetStatus()
                : Invoke<BridgeFeatureStatus>("GetStatus");

        private SpawnDebugResult InvokeSpawnResult(
            string methodName,
            params object?[] arguments)
        {
            object target = productActions ??
                throw new InvalidOperationException(
                    "ProductNative DebugConsole action engine is unavailable.");
            MethodInfo? method = target.GetType().GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .FirstOrDefault(candidate =>
                    (candidate.Name.Equals(
                         methodName,
                         StringComparison.Ordinal) ||
                     candidate.Name.EndsWith(
                         "." + methodName,
                         StringComparison.Ordinal)) &&
                    candidate.GetParameters().Length == arguments.Length);
            if (method == null)
            {
                throw new MissingMethodException(
                    target.GetType().FullName,
                    methodName + "(" + arguments.Length + ")");
            }

            try
            {
                object? outcome = method.Invoke(target, arguments);
                if (outcome is SpawnDebugResult direct)
                    return direct;
                PropertyInfo? resultProperty = outcome?.GetType().GetProperty(
                    "Result",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (resultProperty?.GetValue(outcome, null) is SpawnDebugResult wrapped)
                    return wrapped;
                throw new InvalidCastException(
                    method.Name + " returned " +
                    (outcome?.GetType().FullName ?? "null") +
                    " without a SpawnDebugResult payload.");
            }
            catch (TargetInvocationException error)
                when (error.InnerException != null)
            {
                throw error.InnerException;
            }
        }

        private T Invoke<T>(
            string methodName,
            params object?[] arguments)
        {
            object target = productActions ??
                throw new InvalidOperationException(
                    "ProductNative DebugConsole action engine is unavailable.");
            MethodInfo[] candidates = target.GetType().GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            MethodInfo? method = candidates.FirstOrDefault(candidate =>
                (candidate.Name.Equals(
                     methodName,
                     StringComparison.Ordinal) ||
                 candidate.Name.EndsWith(
                     "." + methodName,
                     StringComparison.Ordinal)) &&
                candidate.GetParameters().Length == arguments.Length &&
                typeof(T).IsAssignableFrom(candidate.ReturnType));
            if (method == null)
            {
                throw new MissingMethodException(
                    target.GetType().FullName,
                    methodName + "(" + arguments.Length + ")");
            }
            try
            {
                object? result = method.Invoke(target, arguments);
                return result is T typed
                    ? typed
                    : throw new InvalidCastException(
                        method.Name + " returned " +
                        (result?.GetType().FullName ?? "null") +
                        " instead of " + typeof(T).FullName + ".");
            }
            catch (TargetInvocationException error)
                when (error.InnerException != null)
            {
                throw error.InnerException;
            }
        }

        private static object? FindProductActions(
            DtmApiRuntime runtime)
        {
            object? entry = FindProductEntry(runtime);
            return entry?.GetType().GetField(
                    "actions",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.GetValue(entry);
        }

        private static object? FindProductEntry(
            DtmApiRuntime runtime)
        {
            FieldInfo? field = runtime.GetType().GetField(
                "modInstances",
                BindingFlags.Instance |
                BindingFlags.NonPublic);
            if (!(field?.GetValue(runtime) is IDictionary instances))
                return null;
            return instances.Contains(ProductOwnerId)
                ? instances[ProductOwnerId]
                : null;
        }
    }
}
