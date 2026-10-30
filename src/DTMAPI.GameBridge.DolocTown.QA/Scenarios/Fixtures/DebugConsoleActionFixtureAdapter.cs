using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    // QA consumes the live product by reflection and owns its result snapshots.
    // Product absence is a missing fixture, not a request for an old executor.
    internal sealed class DebugConsoleActionFixtureAdapter
    {
        private const string ProductOwnerId = "DTMAPI.DebugConsoleMod";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly DtmApiRuntime runtime;
        private readonly object productActions;

        internal DebugConsoleActionFixtureAdapter(DtmApiRuntime runtime, DolocTownGameBridge bridge)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            productActions = FindProductActions() ?? throw new InvalidOperationException("The current Y product must be loaded for this fixture.");
        }

        internal string OwnerKind => "ProductNative";
        internal object? ProductEntry => runtime.GetOwnerInstance(ProductOwnerId);
        internal bool CompatibilityHostAssemblyLoaded => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "DTMAPI.GameBridge.DolocTown.Compatibility");
        internal void ValidateOwnerBoundaryAfterAction()
        {
            if (!ReferenceEquals(productActions, FindProductActions()))
                throw new InvalidOperationException("Y action session is no longer current.");
        }

        public InventoryDebugPage GetItems(InventoryDebugQuery query) => Invoke<InventoryDebugPage>("GetItems", query);
        public InventoryGiveResult GiveItem(IManifest owner, string id, int count) => Invoke<InventoryGiveResult>("GiveItem", owner, id, count);
        public WeatherDebugState GetState() => Invoke<WeatherDebugState>("GetState");
        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers() => Invoke<IReadOnlyList<WeatherDebugOption>>("GetAvailableWeathers");
        public WeatherSetResult SetWeather(IManifest owner, string id, bool patchCurrentPeriod) => Invoke<WeatherSetResult>("SetWeather", owner, id, patchCurrentPeriod);
        public IReadOnlyList<TeleportDestination> GetDestinations() => Invoke<IReadOnlyList<TeleportDestination>>("GetDestinations");
        public TeleportSnapshot GetCurrentSnapshot() => Invoke<TeleportSnapshot>("GetCurrentSnapshot");
        public TeleportResult Teleport(IManifest owner, string id) => Invoke<TeleportResult>("Teleport", owner, id);
        internal BridgeFeatureStatus GetTeleportStatus() => Invoke<BridgeFeatureStatus>("ITeleportActions.GetStatus");
        public InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave) => Invoke<InstantSaveDebugResult>("Save", owner, reloadAfterSave);
        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner) => Invoke<TimeSkipResult>("SkipToNextWeatherPeriod", owner);
        public MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier) => Invoke<MovementSpeedResult>("SetSpeedMultiplier", owner, multiplier);
        public MovementSpeedResult ResetSpeed(IManifest owner, string reason) => Invoke<MovementSpeedResult>("ResetSpeed", owner, reason);
        public TimeSkipResult AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount) => Invoke<TimeSkipResult>("AdvanceTime", owner, kind, amount);
        public TimeScaleDebugResult SetTimeScale(IManifest owner, double multiplier) => Invoke<TimeScaleDebugResult>("SetTimeScale", owner, multiplier);
        public TimeScaleDebugResult ResetTimeScale(IManifest owner, string reason) => Invoke<TimeScaleDebugResult>("ResetTimeScale", owner, reason);
        public DebugValueResult AddMoney(IManifest owner, int amount) => Invoke<DebugValueResult>("AddMoney", owner, amount);
        public DebugValueResult AddTechPoint(IManifest owner, string id, int amount) => Invoke<DebugValueResult>("AddTechPoint", owner, id, amount);
        public DebugCommandResult UnlockAllTechTrees(IManifest owner) => Invoke<DebugCommandResult>("UnlockAllTechTrees", owner);
        public IReadOnlyList<SpawnCatalogOption> GetMonsterCatalog() => Invoke<IReadOnlyList<SpawnCatalogOption>>("GetMonsterCatalog");
        public RoomCropMaturityResult MatureAllCrops(IManifest owner, int radius) => Invoke<RoomCropMaturityResult>("MatureAllCrops", owner, radius);
        public SpawnDebugResult SpawnMonster(IManifest owner, string id, int count)
        {
            object outcome = InvokeRaw("SpawnMonster", null, owner, id, count)!;
            return (SpawnDebugResult)Project(outcome.GetType().GetProperty("Result", Members)!.GetValue(outcome, null), typeof(SpawnDebugResult))!;
        }

        private T Invoke<T>(string method, params object?[] args) => (T)Project(InvokeRaw(method, typeof(T), args), typeof(T))!;

        private object? InvokeRaw(string name, Type? resultType, params object?[] arguments)
        {
            MethodInfo method = productActions.GetType().GetMethods(Members).Single(candidate =>
                (candidate.Name == name || candidate.Name.EndsWith("." + name, StringComparison.Ordinal)) &&
                candidate.GetParameters().Length == arguments.Length &&
                (resultType == null || candidate.ReturnType.Name == resultType.Name));
            var parameters = method.GetParameters();
            object?[] projected = arguments.Select((arg, i) => Project(arg, parameters[i].ParameterType)).ToArray();
            try { return method.Invoke(productActions, projected); }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        }

        private static object? Project(object? value, Type target)
        {
            if (value == null || target.IsInstanceOfType(value)) return value;
            if (target.IsEnum) return Enum.Parse(target, value.ToString());
            if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                Type element = target.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element));
                foreach (object row in (IEnumerable)value) list.Add(Project(row, element));
                return list;
            }
            object snapshot = Activator.CreateInstance(target, nonPublic: true);
            foreach (PropertyInfo destination in target.GetProperties(Members).Where(p => p.CanWrite))
            {
                PropertyInfo source = value.GetType().GetProperty(destination.Name, Members) ??
                    throw new MissingMemberException(value.GetType().FullName, destination.Name);
                destination.SetValue(snapshot, Project(source.GetValue(value, null), destination.PropertyType), null);
            }
            return snapshot;
        }

        private object? FindProductActions() => ProductEntry?.GetType().GetField("actions", Members)?.GetValue(ProductEntry);
    }

    internal sealed class SpawnCatalogOption
    {
        public string Id { get; set; } = "";
        public bool IsAvailable { get; set; }
    }

    internal sealed class RoomCropMaturityResult
    {
        public bool Success { get; set; }
        public int CropsMatured { get; set; }
        public int CandidateBasins { get; set; }
        public string FailureReason { get; set; } = "";
    }
}
