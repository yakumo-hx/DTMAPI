using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions :
        IInventoryActions,
        IWeatherActions,
        ITeleportActions,
        IInstantSaveActions,
        ITimeActions,
        IMovementActions,
        IAdvancedActions
    {
        private readonly IDebugConsoleNativeRuntime runtime;
        private static Type? playerApiType;
        private static MemberInfo? playerAgentMember;
        private static bool playerAgentMemberResolved;
        private static Type? addTechPointApiType;
        private static MethodInfo? addTechPointMethod;
        private static bool addTechPointMethodResolved;
        private double movementMultiplier = 1d;
        private object? movementOwner;
        private string movementLeaseOwnerId = string.Empty;
        private bool movementLeaseActive;
        private IReadOnlyList<InventoryDebugItem>? inventoryCatalog;
        private IReadOnlyList<InventoryDebugSourceGroup>? inventorySourceGroups;
        private IReadOnlyList<SpawnCatalogOption>? monsterCatalog;
        private IReadOnlyList<AnimalCatalogOption>? animalCatalog;
        private Dictionary<string, object> monsterProtos =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, object> animalProtos =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private WeatherPanelSnapshot? weatherSnapshotCache;
        private DateTimeOffset weatherSnapshotExpiresAtUtc = DateTimeOffset.MinValue;

        internal static readonly string[] StableItemCategoryIds =
        {
            "tool",
            "material",
            "farm",
            "husbandry",
            "product",
            "food",
            "kit",
            "equipment",
            "construction",
            "special"
        };
        private static readonly IReadOnlyList<string> StableItemCategories =
            Array.AsReadOnly(StableItemCategoryIds);

        internal static readonly string[] StableWeatherIds =
        {
            "SUNNY",
            "CLOUDY",
            "RAIN",
            "THUNDERSTORM",
            "WINDY",
            "ACID_RAIN",
            "SCORCH_SUN"
        };

        internal DebugConsoleNativeActions(
            IDebugConsoleNativeRuntime runtime)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
        }

        internal string BuildLeaseSummary() =>
            "movement=" + movementMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
            "/active=" + movementLeaseActive + "; timeScale=" + timeScaleMultiplier +
            "/active=" + timeScaleLeaseActive + "/uncertain=" + timeScaleMutationUncertain +
            "; god=" + GodModeEnabled;

        internal void Update()
        {
            if (!IsCurrentSaveSession)
                return;
            if (godEnabled)
                RefillGodVitals();
            if (!movementLeaseActive)
                return;
            object? current = ResolvePlayerBody();
            if (current == null || ReferenceEquals(current, movementOwner))
                return;
            runtime.SetMovementMultiplier(current, movementMultiplier);
            movementOwner = current;
        }

        internal bool NeedsUpdate => movementLeaseActive || godEnabled;

        internal void InvalidateCatalogs()
        {
            inventoryCatalog = null;
            inventorySourceGroups = null;
            monsterCatalog = null;
            animalCatalog = null;
            monsterProtos.Clear();
            animalProtos.Clear();
            weatherSnapshotCache = null;
            weatherSnapshotExpiresAtUtc = DateTimeOffset.MinValue;
        }

        internal void RestoreTransientState(string reason)
        {
            StopGodMode();
            var failures = new List<string>();
            MovementSpeedResult movement = ResetMovement(reason);
            if (!movement.Success)
                failures.Add("movement=" + FirstFailure(movement.FailureReason, movement.Message));
            TimeScaleDebugResult timeScale = ResetTimeScaleCore(reason);
            if (!timeScale.Success)
                failures.Add("timeScale=" + FirstFailure(timeScale.FailureReason, timeScale.Message));
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole transient restoration remains pending: " +
                    string.Join("; ", failures) + ".");
            }
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return
                (movementLeaseActive &&
                 movementLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ? 1 : 0) +
                (timeScaleLeaseActive &&
                 timeScaleLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ? 1 : 0) +
                (godEnabled &&
                 godOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            var failures = new List<string>();
            int removed = 0;
            if (movementLeaseActive &&
                movementLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                MovementSpeedResult result = ResetMovement("owner cleanup " + reason);
                if (result.Success)
                    removed++;
                else
                    failures.Add("movement=" + FirstFailure(result.FailureReason, result.Message));
            }
            if (timeScaleLeaseActive &&
                timeScaleLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                TimeScaleDebugResult result = ResetTimeScaleCore("owner cleanup " + reason);
                if (result.Success)
                    removed++;
                else
                    failures.Add("timeScale=" + FirstFailure(result.FailureReason, result.Message));
            }
            if (godEnabled && godOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                StopGodMode();
                removed++;
            }
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole owner cleanup remains pending for " +
                    ownerId + ": " + string.Join("; ", failures) + ".");
            }
            return removed;
        }

        private static string FirstFailure(string reason, string message) =>
            string.IsNullOrWhiteSpace(reason)
                ? (message ?? string.Empty)
                : reason + (string.IsNullOrWhiteSpace(message) ? string.Empty : ": " + message);

        public IReadOnlyList<InventoryDebugSourceGroup> GetSources()
        {
            try
            {
                return InventorySourceGroups();
            }
            catch (Exception error)
            {
                runtime.Error("inventory-enumeration", error);
                return Array.Empty<InventoryDebugSourceGroup>();
            }
        }

        public InventoryDebugPage GetItems(InventoryDebugQuery query)
        {
            int size = query.PageSize <= 0 ? 12 : query.PageSize;
            int page = Math.Max(0, query.Page);
            try
            {
                IReadOnlyList<InventoryDebugItem> items = InventoryCatalog();
                string search = query.SearchText.Trim();
                string source = query.SourceId.Trim();
                string category = query.Category.Trim();
                var slice = new CatalogPageAccumulator<InventoryDebugItem>(page, size);
                for (int index = 0; index < items.Count; index++)
                {
                    InventoryDebugItem item = items[index];
                    if (!query.IncludeUnavailable && !item.CanGive)
                        continue;
                    if (search.Length > 0 &&
                        item.SearchText.IndexOf(
                            search,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    if (!InventorySourceMatches(item, source))
                        continue;
                    if (category.Length > 0 &&
                        !item.Category.Equals(
                            category,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    slice.Add(item);
                }

                IReadOnlyList<InventoryDebugItem> pageItems = slice.Complete();
                return new InventoryDebugPage
                {
                    Items = pageItems,
                    Categories = StableItemCategories,
                    Sources = InventorySourceGroups(),
                    Page = slice.Page,
                    PageSize = size,
                    TotalItems = slice.TotalItems,
                    TotalPages = slice.TotalPages,
                    Status = "ok"
                };
            }
            catch (Exception error)
            {
                runtime.Error("inventory-enumeration", error);
                return new InventoryDebugPage
                {
                    PageSize = size,
                    TotalPages = 1,
                    Status = error.GetType().Name + ": " + error.Message
                };
            }
        }

        private static bool InventorySourceMatches(
            InventoryDebugItem item,
            string source)
        {
            if (source.Length == 0)
                return true;
            if (source.Equals("__base", StringComparison.OrdinalIgnoreCase))
                return item.SourceKind.Equals("Vanilla", StringComparison.OrdinalIgnoreCase);
            if (source.Equals("__mods", StringComparison.OrdinalIgnoreCase))
                return item.IsModItem;
            return item.SourceId.Equals(
                source,
                StringComparison.OrdinalIgnoreCase);
        }

        public InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count)
        {
            itemId = itemId.Trim();
            var result = new InventoryGiveResult
            {
                ItemId = itemId,
                RequestedCount = count
            };
            try
            {
                if (itemId.Length == 0 || count <= 0)
                    return Fail(result, "invalid-request", "Item id and positive count are required.");
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                if (api == null)
                    return Fail(result, "missing-dolocapi", "DolocAPI is unavailable.");
                MethodInfo? query = api.GetMethod(
                    "QueryItemProto",
                    BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = { itemId, null };
                if (!(query?.Invoke(null, queryArgs) is bool found) ||
                    !found ||
                    queryArgs[1] == null)
                    return Fail(result, "unknown-item", "The item is not present in TbItem.");
                object proto = queryArgs[1]!;
                result.DisplayName = Native.First(
                    Native.Text(proto, "Title"),
                    itemId);
                int stack = Math.Max(1, Native.Int(proto, "Overlay", 1));
                MethodInfo? countItem = api.GetMethod(
                    "CountItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) },
                    null);
                MethodInfo? generate = api.GetMethod(
                    "GenerateItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);
                if (generate == null)
                    return Fail(result, "missing-native-generation", "Native item generation is unavailable.");
                MethodInfo? canPlace = api.GetMethod(
                    "CanPlaceItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { generate.ReturnType },
                    null);
                MethodInfo? place = api.GetMethod(
                    "TryPlaceInBackpack",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { generate.ReturnType, typeof(bool) },
                    null);
                if (canPlace == null || place == null)
                    return Fail(result, "missing-native-placement", "Native backpack placement is unavailable.");
                result.BeforeCount = InvokeInt(
                    countItem,
                    itemId,
                    result.BeforeCount);
                int remaining = count;
                while (remaining > 0)
                {
                    int chunk = Math.Min(stack, remaining);
                    // String backpack placement lowercases IDs; native generation preserves them.
                    object? item = generate.Invoke(null, new object[] { itemId, chunk });
                    if (item == null)
                    {
                        result.AfterCount = InvokeInt(countItem, itemId, result.BeforeCount + result.GivenCount);
                        return Fail(result, "native-item-generation-failed", "Native item generation returned no item.");
                    }
                    if (!(canPlace.Invoke(null, new[] { item }) is bool capacity) ||
                        !capacity)
                        break;
                    if (!(place.Invoke(null, new object[] { item, false }) is bool placed) ||
                        !placed)
                        break;
                    result.GivenCount += chunk;
                    remaining -= chunk;
                }
                result.AfterCount = InvokeInt(
                    countItem,
                    itemId,
                    result.BeforeCount + result.GivenCount);
                result.Success = result.GivenCount == count;
                result.FailureReason = result.Success
                    ? string.Empty
                    : result.GivenCount == 0
                        ? "inventory-full-or-native-rejected"
                        : "partial-inventory-full";
                result.Message = "Give item owner=" +
                    (owner?.UniqueID ?? "unknown") +
                    " item=" + itemId +
                    " requested=" + count +
                    " given=" + result.GivenCount +
                    " before=" + result.BeforeCount +
                    " after=" + result.AfterCount +
                    " reason=" + Native.First(result.FailureReason, "none") + ".";
                LogMutation("item-give", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                Exception cause = Unwrap(error);
                runtime.Error("item-give", cause);
                return Fail(result, cause.GetType().Name, cause.Message);
            }
        }

        BridgeFeatureStatus IInventoryActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " item query and native backpack placement.");

        public WeatherPanelSnapshot GetPanelSnapshot()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (weatherSnapshotCache != null &&
                now <= weatherSnapshotExpiresAtUtc)
            {
                return weatherSnapshotCache;
            }
            try
            {
                object? archive = Native.Archive;
                object? date = Native.Read(archive, "DateNow");
                object? timeData = Native.Read(archive, "timeData");
                string seasonGroupId = CurrentSeasonGroupId(archive);
                object? season = CurrentSeason(timeData, seasonGroupId);
                string currentId = Native.Read(archive, "LocalWeatherType")?.ToString() ??
                    string.Empty;
                string[] forecastIds = CurrentDayWeather(timeData, seasonGroupId)
                    .Select(WeatherId)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var forecast = new HashSet<string>(
                    forecastIds,
                    StringComparer.OrdinalIgnoreCase);
                var byId = Native.Enumerate(Native.TableList("TbWeather"))
                    .Select(weather => BuildWeather(weather, currentId, forecast))
                    .Where(option => option.Id.Length > 0)
                    .GroupBy(option => option.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First(),
                        StringComparer.OrdinalIgnoreCase);
                string[] fixedIds = StableWeatherIds;
                WeatherDebugOption[] options = fixedIds
                    .Select(id => byId.TryGetValue(id, out var option)
                        ? option
                        : BuildUnavailableWeather(id, currentId, forecast))
                    .ToArray();
                WeatherDebugOption? current = options.FirstOrDefault(option =>
                    option.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));
                weatherSnapshotCache = new WeatherPanelSnapshot
                {
                    State = new WeatherDebugState
                    {
                        CurrentWeatherId = currentId,
                        CurrentWeatherName = current?.DisplayName ?? currentId,
                        Year = Native.Int(date, "Year"),
                        Month = Native.Int(date, "Month"),
                        Day = Native.Int(date, "Day"),
                        Hour = Native.Int(date, "Hour"),
                        SeasonName = Native.First(
                            Native.Text(season, "Title"),
                            Native.Text(season, "Id")),
                        CurrentDayForecastWeatherIds = forecastIds
                    },
                    Options = options,
                    AvailableWeatherIds = fixedIds
                        .Where(byId.ContainsKey)
                        .ToArray()
                };
                weatherSnapshotExpiresAtUtc = now.AddMilliseconds(100);
                return weatherSnapshotCache;
            }
            catch (Exception error)
            {
                runtime.Error("weather-snapshot", error);
                return new WeatherPanelSnapshot();
            }
        }

        public WeatherDebugState GetState() => GetPanelSnapshot().State;

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers()
        {
            WeatherPanelSnapshot snapshot = GetPanelSnapshot();
            return snapshot.Options
                .Where(option => snapshot.IsAvailable(option.Id))
                .ToArray();
        }

        public WeatherSetResult SetWeather(
            IManifest owner,
            string weatherId,
            bool patchCurrentPeriod)
        {
            weatherId = (weatherId ?? string.Empty).Trim();
            var result = new WeatherSetResult
            {
                WeatherId = weatherId,
                PatchedCurrentPeriod = patchCurrentPeriod
            };
            try
            {
                object? archive = Native.Archive;
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                if (archive == null || api == null)
                    return FailWeather(result, "missing-native-weather", "Native weather owner is unavailable.");
                WeatherPanelSnapshot snapshot = GetPanelSnapshot();
                WeatherDebugOption? allowed = snapshot.Options
                    .FirstOrDefault(option =>
                        option.Id.Equals(weatherId, StringComparison.OrdinalIgnoreCase));
                if (allowed == null || !snapshot.IsAvailable(weatherId))
                    return FailWeather(result, "not-whitelisted", "Weather is not in the bounded TbWeather list.");
                result.BeforeWeatherId =
                    Native.Read(archive, "LocalWeatherType")?.ToString() ??
                    string.Empty;
                MethodInfo? command = api.GetMethod(
                    "Command_SetWeather",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(bool) },
                    null);
                if (command == null)
                    return FailWeather(result, "missing-set-weather-command", "DolocAPI.Command_SetWeather(string, bool) is unavailable.");
                command.Invoke(
                    null,
                    new object[] { weatherId, patchCurrentPeriod });
                weatherSnapshotCache = null;
                weatherSnapshotExpiresAtUtc = DateTimeOffset.MinValue;
                result.AfterWeatherId =
                    Native.Read(archive, "LocalWeatherType")?.ToString() ??
                    string.Empty;
                result.DisplayName = allowed.DisplayName;
                result.Success = result.AfterWeatherId.Equals(
                    weatherId,
                    StringComparison.OrdinalIgnoreCase);
                if (!result.Success)
                    result.FailureReason = "result-mismatch";
                result.Message = "Weather " + result.BeforeWeatherId +
                    " -> " + result.AfterWeatherId +
                    " patchCurrentPeriod=" + patchCurrentPeriod + ".";
                LogMutation("weather-set", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("weather-set", error);
                return FailWeather(result, error.GetType().Name, error.Message);
            }
        }

        private WeatherSetResult FailWeather(
            WeatherSetResult result,
            string reason,
            string message)
        {
            result = Fail(result, reason, message);
            LogMutation(
                "weather-set",
                false,
                "Weather request id=" + result.WeatherId +
                " patchCurrentPeriod=" + result.PatchedCurrentPeriod +
                " reason=" + reason +
                "; " + message);
            return result;
        }

        BridgeFeatureStatus IWeatherActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " TbWeather allowlist, official room weather command, and LocalWeatherType verification.");

        public IReadOnlyList<TeleportDestination> GetDestinations()
        {
            try
            {
                var usedMarkPoints = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                var result = new List<TeleportDestination>();
                foreach (TeleportSpec spec in StableTeleportDirectory)
                {
                    if (!usedMarkPoints.Add(spec.MarkPointId))
                        continue;
                    result.Add(BuildDestination(spec));
                }
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("teleport-list", error);
                return Array.Empty<TeleportDestination>();
            }
        }

        public TeleportSnapshot GetCurrentSnapshot() =>
            Snapshot(Native.CurrentRoom, Native.AgentPosition);

        public TeleportResult Teleport(
            IManifest owner,
            string destinationId)
        {
            destinationId = (destinationId ?? string.Empty).Trim();
            var result = new TeleportResult
            {
                DestinationId = destinationId,
                Before = GetCurrentSnapshot()
            };
            try
            {
                TeleportDestination? destination = GetDestinations()
                    .FirstOrDefault(value =>
                        value.Id.Equals(destinationId, StringComparison.OrdinalIgnoreCase));
                if (destination == null)
                    return Fail(result, "not-in-semantic-directory", "Destination is not in the ProductNative semantic directory.");
                if (!destination.IsUnlocked)
                    return Fail(result, destination.Source, "Destination is currently unavailable: " + destination.Source + ".");
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                MethodInfo? transport = Native.Method(
                    api,
                    "DoTransport",
                    5,
                    true);
                if (transport == null)
                    return Fail(result, "missing-do-transport", "DolocAPI.DoTransport is unavailable.");
                result.DestinationName = destination.DisplayName;
                result.MarkPointId = destination.MarkPointId;
                object? accepted = transport.Invoke(
                    null,
                    new object?[]
                    {
                        destination.MarkPointId,
                        null,
                        true,
                        false,
                        false
                    });
                result.Success = accepted is bool ok && ok;
                result.AfterRequest = GetCurrentSnapshot();
                if (!result.Success)
                    return Fail(result, "native-rejected", "Native transport rejected the request.");
                result.Message = "Teleport request accepted owner=" +
                    (owner?.UniqueID ?? "unknown") +
                    " mark=" + destination.MarkPointId + ".";
                LogMutation("teleport", true, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("teleport", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        BridgeFeatureStatus ITeleportActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " station/key-mark allowlist and DolocAPI.DoTransport.");

        InstantSaveDebugState IInstantSaveActions.GetState() =>
            GetSaveState();

        public InstantSaveDebugResult Save(
            IManifest owner,
            bool reloadAfterSave)
        {
            var result = new InstantSaveDebugResult
            {
                ReloadAfterSave = reloadAfterSave,
                Before = GetSaveState()
            };
            if (reloadAfterSave)
                return Fail(result, "reload-disabled", "Save-and-reload remains deliberately disabled.");
            result.SaveSlot = result.Before.SaveSlot;
            if (!result.Before.CanSave || result.SaveSlot == null)
                return Fail(result, Native.First(result.Before.FailureReason, "not-saveable"), result.Before.Message);
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                MethodInfo? save = Native.Method(api, "SaveGame", 1, true);
                if (save == null)
                    return Fail(result, "missing-savegame", "DolocAPI.SaveGame(int) is unavailable.");
                object? accepted = save.Invoke(null, new object[] { result.SaveSlot.Value });
                if (accepted is bool ok && !ok)
                    return Fail(result, "native-rejected", "Native SaveGame returned false.");
                result.AfterSave = GetSaveState();
                result.Success = true;
                result.Message = "Native save requested owner=" +
                    (owner?.UniqueID ?? "unknown") +
                    " slot=" + result.SaveSlot.Value + ".";
                LogMutation("native-save", true, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("native-save", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        BridgeFeatureStatus IInstantSaveActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " explicit confirmed native SaveGame request; reload disabled.");

        TimeDebugState ITimeActions.GetState() =>
            GetTimeState();

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner)
        {
            TimeDebugState before = GetTimeState();
            int target = before.Hour < 6 ? 6 : before.Hour < 18 ? 18 : 24;
            int minutes = ((target > before.Hour ? target - before.Hour : target + 24 - before.Hour) * 60) -
                before.Minute;
            return AdvanceMinutes(owner, Math.Max(1, minutes), "next-weather-period", target);
        }

        BridgeFeatureStatus ITimeActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " PassTimeNoControl allowlist.");

        MovementDebugState IMovementActions.GetState() =>
            GetMovementState("query");

        public MovementSpeedResult SetSpeedMultiplier(
            IManifest owner,
            double multiplier)
        {
            multiplier = Math.Min(4d, Math.Max(0.5d, multiplier));
            if (Math.Abs(multiplier - 1d) < 0.001)
                return ResetMovement("set-1x");
            var result = new MovementSpeedResult
            {
                RequestedMultiplier = multiplier,
                Before = GetMovementState("before")
            };
            try
            {
                object? player = ResolvePlayerBody();
                string ownerId = owner?.UniqueID ?? string.Empty;
                if (movementLeaseActive &&
                    !movementLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    MovementSpeedResult restored = ResetMovement(
                        "lease replaced by " + FirstFailure(ownerId, "unknown"));
                    if (!restored.Success)
                        return Fail(result, restored.FailureReason, restored.Message);
                }
                if (player == null)
                {
                    return Fail(
                        result,
                        "missing-player-body",
                        "The current player BodyController is unavailable.");
                }
                runtime.SetMovementMultiplier(player, multiplier);
                movementOwner = player;
                movementLeaseOwnerId = ownerId;
                movementLeaseActive = true;
                movementMultiplier = multiplier;
                result.AppliedMultiplier = multiplier;
                result.After = GetMovementState("after");
                result.Success = true;
                result.Message = "Movement lease owner=" +
                    (owner?.UniqueID ?? "unknown") +
                    " multiplier=" +
                    multiplier.ToString("0.###", CultureInfo.InvariantCulture) + ".";
                LogMutation("movement-lease", true, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("movement-lease", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public MovementSpeedResult ResetSpeed(
            IManifest owner,
            string reason) =>
            ResetMovement(reason);

        BridgeFeatureStatus IMovementActions.GetStatus() =>
            Status("Product-owned multiplier on the final player BodyController.MoveSpeed result; native MoveScaler remains game-owned.");
    }
}
