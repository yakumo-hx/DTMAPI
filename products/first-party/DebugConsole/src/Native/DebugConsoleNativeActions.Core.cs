using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private double movementMultiplier = 1d;
        private object? movementOwner;
        private string movementLeaseOwnerId = string.Empty;
        private bool movementLeaseActive;
        private double timeScaleMultiplier = 1d;
        private string timeScaleLeaseOwnerId = string.Empty;
        private double timeScaleOriginalMultiplier = 1d;
        private double timeScaleAppliedMultiplier = 1d;
        private double timeScalePriorAppliedMultiplier = 1d;
        private bool timeScaleMutationUncertain;
        private bool timeScaleLeaseActive;
        private bool creativeEnabled;
        private string creativeLeaseOwnerId = string.Empty;
        private bool creativeSnapshotValid;
        private bool originalIgnoreMaterialCost;
        private bool originalSkipMoneyVerify;
        private bool originalIgnoreSpiritCost;
        private bool creativeAttemptedMaterial;
        private bool creativeAttemptedShop;
        private bool creativeAttemptedSpirit;
        private bool creativeMutationUncertain;

        internal DebugConsoleNativeActions(
            IDebugConsoleNativeRuntime runtime)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
        }

        internal string BuildLeaseSummary() =>
            "movement=" +
            movementMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
            "/active=" + movementLeaseActive.ToString(CultureInfo.InvariantCulture) +
            "; timeScale=" +
            timeScaleMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
            "/active=" + timeScaleLeaseActive.ToString(CultureInfo.InvariantCulture) +
            "/uncertain=" + timeScaleMutationUncertain.ToString(CultureInfo.InvariantCulture) +
            "; creative=" +
            creativeEnabled.ToString(CultureInfo.InvariantCulture) +
            "/snapshot=" + creativeSnapshotValid.ToString(CultureInfo.InvariantCulture) +
            "/uncertain=" + creativeMutationUncertain.ToString(CultureInfo.InvariantCulture);

        internal void Update()
        {
            if (!movementLeaseActive)
                return;
            object? current = ResolvePlayerBody();
            if (current == null || ReferenceEquals(current, movementOwner))
                return;
            runtime.SetMovementMultiplier(current, movementMultiplier);
            movementOwner = current;
        }

        internal void RestoreTransientState(string reason)
        {
            var failures = new List<string>();
            MovementSpeedResult movement = ResetMovement(reason);
            if (!movement.Success)
                failures.Add("movement=" + FirstFailure(movement.FailureReason, movement.Message));
            TimeScaleDebugResult timeScale = ResetTimeScaleCore(reason);
            if (!timeScale.Success)
                failures.Add("timeScale=" + FirstFailure(timeScale.FailureReason, timeScale.Message));
            if (!RestoreCreative(reason, out string creativeFailure))
                failures.Add("creative=" + creativeFailure);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole transient restoration remains pending: " +
                    string.Join("; ", failures) + ".");
            }
        }

        internal void RestoreModalScopedState(string reason)
        {
            var failures = new List<string>();
            TimeScaleDebugResult timeScale = ResetTimeScaleCore(reason);
            if (!timeScale.Success)
                failures.Add("timeScale=" + FirstFailure(timeScale.FailureReason, timeScale.Message));
            if (!RestoreCreative(reason, out string creativeFailure))
                failures.Add("creative=" + creativeFailure);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole modal-scoped restoration remains pending: " +
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
                ((creativeEnabled || creativeSnapshotValid) &&
                 creativeLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
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
            if ((creativeEnabled || creativeSnapshotValid) &&
                creativeLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                if (RestoreCreative("owner cleanup " + reason, out string failure))
                    removed++;
                else
                    failures.Add("creative=" + failure);
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

        public InventoryDebugPage GetItems(InventoryDebugQuery query)
        {
            query ??= new InventoryDebugQuery();
            int size = Math.Max(1, Math.Min(50, query.PageSize <= 0 ? 12 : query.PageSize));
            int page = Math.Max(0, query.Page);
            try
            {
                List<InventoryDebugItem> items =
                    EnumerateInventoryItems().ToList();
                IEnumerable<InventoryDebugItem> filtered = items;
                if (!query.IncludeUnavailable)
                    filtered = filtered.Where(item => item.CanGive);
                string search = (query.SearchText ?? string.Empty).Trim();
                if (search.Length > 0)
                {
                    filtered = filtered.Where(item =>
                        (item.SearchText ?? string.Empty).IndexOf(
                            search,
                            StringComparison.OrdinalIgnoreCase) >= 0);
                }
                string source = (query.SourceId ?? string.Empty).Trim();
                if (source.Equals("__base", StringComparison.OrdinalIgnoreCase))
                    filtered = filtered.Where(item => !item.IsModItem);
                else if (source.Equals("__mods", StringComparison.OrdinalIgnoreCase))
                    filtered = filtered.Where(item => item.IsModItem);
                else if (source.Length > 0)
                {
                    filtered = filtered.Where(item =>
                        item.SourceId.Equals(
                            source,
                            StringComparison.OrdinalIgnoreCase));
                }
                else if (query.ModItemsOnly)
                    filtered = filtered.Where(item => item.IsModItem);

                List<InventoryDebugItem> sourceFiltered = filtered.ToList();
                string[] categories = sourceFiltered
                    .Select(item => Native.First(item.SubCategory, item.Category))
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                string category = (query.Category ?? string.Empty).Trim();
                if (category.Length > 0)
                {
                    sourceFiltered = sourceFiltered.Where(item =>
                            item.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                            item.SubCategory.Equals(category, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                sourceFiltered = sourceFiltered
                    .OrderBy(item => item.IsModItem ? 1 : 0)
                    .ThenBy(item => item.RuntimeOrder)
                    .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int pages = Math.Max(1, (int)Math.Ceiling(sourceFiltered.Count / (double)size));
                page = Math.Min(page, pages - 1);
                return new InventoryDebugPage
                {
                    Items = sourceFiltered.Skip(page * size).Take(size).ToArray(),
                    Categories = categories,
                    Sources = BuildSourceGroups(items),
                    Page = page,
                    PageSize = size,
                    TotalItems = sourceFiltered.Count,
                    TotalPages = pages,
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

        public InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count)
        {
            itemId = (itemId ?? string.Empty).Trim();
            count = Math.Max(0, Math.Min(999, count));
            var result = new InventoryGiveResult
            {
                ItemId = itemId,
                RequestedCount = count
            };
            try
            {
                if (itemId.Length == 0 || count == 0)
                    return Fail(result, "invalid-request", "Item id and positive count are required.");
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                if (api == null)
                    return Fail(result, "missing-dolocapi", "DolocAPI is unavailable.");
                IContentItemInfo? source = runtime.GetIndexedItem(itemId);
                if (source != null && !source.Enabled)
                    return Fail(result, "source-disabled", "The owning content source is disabled.");
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
                MethodInfo? canPlace = api.GetMethod(
                    "CanPlaceItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);
                MethodInfo? place = api.GetMethod(
                    "TryPlaceInBackpack",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(int), typeof(bool) },
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
                    if (!(canPlace.Invoke(null, new object[] { itemId, chunk }) is bool capacity) ||
                        !capacity)
                        break;
                    if (!(place.Invoke(null, new object[] { itemId, chunk, false }) is bool placed) ||
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
                runtime.Error("item-give", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        BridgeFeatureStatus IInventoryActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " item query and native backpack placement.");

        public WeatherDebugState GetState()
        {
            try
            {
                object? archive = Native.Archive;
                object? date = Native.Read(archive, "DateNow");
                object? timeData = Native.Read(archive, "timeData");
                object? season = Native.Read(timeData, "SeasonProto");
                string id = Native.Read(archive, "CurrentWeatherType")?.ToString() ??
                    string.Empty;
                WeatherDebugOption? current = GetAvailableWeathers()
                    .FirstOrDefault(option =>
                        option.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                return new WeatherDebugState
                {
                    CurrentWeatherId = id,
                    CurrentWeatherName = current?.DisplayName ?? id,
                    Year = Native.Int(date, "Year"),
                    Month = Native.Int(date, "Month"),
                    Day = Native.Int(date, "Day"),
                    Hour = Native.Int(date, "Hour"),
                    SeasonName = Native.First(
                        Native.Text(season, "Title"),
                        Native.Text(season, "Id")),
                    CurrentDayForecastWeatherIds =
                        CurrentDayWeather(timeData)
                            .Select(WeatherId)
                            .Where(value => value.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray()
                };
            }
            catch (Exception error)
            {
                runtime.Error("weather-state", error);
                return new WeatherDebugState();
            }
        }

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers()
        {
            try
            {
                object? archive = Native.Archive;
                string current = Native.Read(archive, "CurrentWeatherType")?.ToString() ??
                    string.Empty;
                object? timeData = Native.Read(archive, "timeData");
                var forecast = new HashSet<string>(
                    CurrentDayWeather(timeData).Select(WeatherId),
                    StringComparer.OrdinalIgnoreCase);
                return Native.Enumerate(Native.TableList("TbWeather"))
                    .Select(weather => BuildWeather(weather, current, forecast))
                    .Where(option =>
                        option.Id.Length > 0 &&
                        !option.Id.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(option => option.IsCurrent)
                    .ThenByDescending(option => option.IsCurrentDayForecast)
                    .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception error)
            {
                runtime.Error("weather-list", error);
                return Array.Empty<WeatherDebugOption>();
            }
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
                Type? weatherType = Native.Resolve(
                    "DolocTown.Config.Weather.WeatherType, Assembly-CSharp");
                if (archive == null || weatherType?.IsEnum != true)
                    return Fail(result, "missing-native-weather", "Native weather owner is unavailable.");
                WeatherDebugOption? allowed = GetAvailableWeathers()
                    .FirstOrDefault(option =>
                        option.Id.Equals(weatherId, StringComparison.OrdinalIgnoreCase));
                if (allowed == null)
                    return Fail(result, "not-whitelisted", "Weather is not in the bounded TbWeather list.");
                object parsed = Enum.Parse(weatherType, weatherId, true);
                result.BeforeWeatherId =
                    Native.Read(archive, "CurrentWeatherType")?.ToString() ??
                    string.Empty;
                MethodInfo? set = archive.GetType().GetMethod(
                    "SetWeather",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { weatherType, typeof(bool) },
                    null);
                if (set == null)
                    return Fail(result, "missing-setweather", "ArchiveDataHandle.SetWeather is unavailable.");
                set.Invoke(archive, new[] { parsed, (object)true });
                if (patchCurrentPeriod)
                {
                    archive.GetType().GetMethod(
                            "PatchWeather",
                            BindingFlags.Public | BindingFlags.Instance,
                            null,
                            new[] { weatherType },
                            null)
                        ?.Invoke(archive, new[] { parsed });
                }
                result.AfterWeatherId =
                    Native.Read(archive, "CurrentWeatherType")?.ToString() ??
                    string.Empty;
                result.DisplayName = allowed.DisplayName;
                result.Success = result.AfterWeatherId.Equals(
                    weatherId,
                    StringComparison.OrdinalIgnoreCase);
                result.Message = "Weather " + result.BeforeWeatherId +
                    " -> " + result.AfterWeatherId +
                    " patchCurrentPeriod=" + patchCurrentPeriod + ".";
                LogMutation("weather-set", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("weather-set", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        BridgeFeatureStatus IWeatherActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " TbWeather allowlist and ArchiveDataHandle weather mutation.");

        public IReadOnlyList<TeleportDestination> GetDestinations()
        {
            try
            {
                var result = new Dictionary<string, TeleportDestination>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (object station in Native.Enumerate(Native.TableList("TbStation")))
                {
                    string stationId = Native.Text(station, "Id");
                    string markId = Native.Text(station, "MarkPointId");
                    AddDestination(
                        result,
                        "station:" + stationId,
                        Native.First(Native.Text(station, "Title"), stationId),
                        "Station",
                        markId,
                        true,
                        "TbStation.MarkPointId");
                }
                foreach (object mark in Native.Enumerate(Native.TableList("TbMarkPoint")))
                {
                    string markId = Native.Text(mark, "Id");
                    string roomId = Native.Text(mark, "RoomId");
                    if (!WhitelistedMark(markId, roomId))
                        continue;
                    AddDestination(
                        result,
                        "mark:" + markId,
                        DisplayMark(markId, roomId),
                        "Key",
                        markId,
                        false,
                        "TbMarkPoint allowlist");
                }
                return result.Values
                    .OrderBy(value => value.Group, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Take(80)
                    .ToArray();
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
                    return Fail(result, "not-whitelisted", "Destination is not in the ProductNative allowlist.");
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                MethodInfo? transport = api?.GetMethods(
                        BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                        method.Name == "DoTransport" &&
                        method.GetParameters().Length == 5);
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

        public TeleportCsvExportResult ExportDestinationsCsv(IManifest owner)
        {
            var result = new TeleportCsvExportResult();
            try
            {
                TeleportDestination[] destinations = GetDestinations().ToArray();
                string directory = Path.Combine(
                    runtime.EvidencePath,
                    "TELEPORT-DESTINATIONS",
                    DateTimeOffset.Now.ToString(
                        "yyyyMMdd-HHmmss",
                        CultureInfo.InvariantCulture));
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "teleport-destinations.csv");
                var csv = new StringBuilder(
                    "internal_id,map,x,y,display_name,source,mark_point_id,group,is_station\r\n");
                foreach (TeleportDestination destination in destinations)
                {
                    csv.Append(Csv(destination.Id)).Append(',')
                        .Append(Csv(destination.RoomId)).Append(',')
                        .Append(destination.X.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(destination.Y.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(Csv(destination.DisplayName)).Append(',')
                        .Append(Csv(destination.Source)).Append(',')
                        .Append(Csv(destination.MarkPointId)).Append(',')
                        .Append(Csv(destination.Group)).Append(',')
                        .Append(destination.IsStation ? "true" : "false")
                        .AppendLine();
                }
                File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
                result.Success = true;
                result.Path = path;
                result.RowCount = destinations.Length;
                result.Message = "Exported " + destinations.Length + " bounded teleport destinations.";
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("teleport-export", error);
                result.FailureReason = error.GetType().Name;
                result.Message = error.Message;
                return result;
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
