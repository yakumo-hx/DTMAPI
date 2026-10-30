using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions
    {
        private IReadOnlyList<InventoryDebugItem> InventoryCatalog()
        {
            if (inventoryCatalog == null)
            {
                inventoryCatalog = EnumerateInventoryItems()
                    .OrderBy(item => item.IsModItem ? 1 : 0)
                    .ThenBy(item => item.RuntimeOrder)
                    .ThenBy(
                        item => item.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        item => item.Id,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                inventorySourceGroups = BuildSourceGroups(inventoryCatalog);
            }
            return inventoryCatalog;
        }

        private IReadOnlyList<InventoryDebugSourceGroup>
            InventorySourceGroups()
        {
            _ = InventoryCatalog();
            return inventorySourceGroups ??
                Array.Empty<InventoryDebugSourceGroup>();
        }

        private IEnumerable<InventoryDebugItem> EnumerateInventoryItems()
        {
            Dictionary<string, IContentItemInfo> sources = runtime
                .GetIndexedItems()
                .GroupBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(item => item.Enabled)
                        .ThenBy(item => item.LoadOrder < 0 ? int.MaxValue : item.LoadOrder)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);
            var runtimeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int order = 0;
            foreach (object proto in Native.Enumerate(Native.TableList("TbItem")))
            {
                string id = Native.Text(proto, "Id");
                if (id.Length == 0)
                    continue;
                runtimeIds.Add(id);
                object? main = Native.Read(proto, "MainType");
                object? sub = Native.Read(proto, "SubType_Ref");
                int stack = Native.Int(proto, "Overlay", 1);
                var item = new InventoryDebugItem
                {
                    Id = id,
                    DisplayName = Native.First(Native.Text(proto, "Title"), id),
                    ChineseName = Native.First(Native.Text(proto, "Title"), id),
                    EnglishName = Native.First(
                        Native.TextFirst(
                            proto,
                            "EnglishTitle",
                            "TitleEn",
                            "Name"),
                        id),
                    Category = NormalizeItemCategory(Native.Text(main, "Id")),
                    SubCategory = Native.First(
                        Native.Text(sub, "Title"),
                        Native.Text(proto, "SubType")),
                    Tags = new[]
                    {
                        Native.Text(main, "Title"),
                        Native.Text(proto, "SubType"),
                        Native.Text(sub, "Title")
                    }.Where(value => value.Length > 0).ToArray(),
                    MaxStack = Math.Max(1, stack),
                    CanSpawn = stack > 0,
                    CanGive = stack > 0,
                    RuntimeLoaded = true,
                    CannotGiveReason = stack > 0 ? string.Empty : "not-spawnable",
                    HasIcon = Native.Read(proto, "UiSpriteAsset") != null,
                    IconAssetKey = Native.Read(proto, "UiSpriteAsset")?.ToString() ?? string.Empty,
                    SourceKind = "Vanilla",
                    SourceModTitle = "Doloc Town",
                    SourceId = "Vanilla",
                    RuntimeOrder = order++
                };
                if (sources.TryGetValue(
                        id,
                        out IContentItemInfo? source) &&
                    source != null)
                {
                    ApplySource(item, source);
                }
                item.SearchText = SearchText(item);
                yield return item;
            }
            foreach (IContentItemInfo source in sources.Values
                .Where(item => !runtimeIds.Contains(item.ItemId))
                .OrderBy(item => item.LoadOrder < 0 ? int.MaxValue : item.LoadOrder)
                .ThenBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase))
            {
                var item = new InventoryDebugItem
                {
                    Id = source.ItemId,
                    DisplayName = Native.First(source.ChineseName, source.EnglishName, source.ItemId),
                    ChineseName = source.ChineseName,
                    EnglishName = source.EnglishName,
                    Category = NormalizeItemCategory(source.Category),
                    SubCategory = source.Category,
                    Tags = source.Tags,
                    RuntimeLoaded = false,
                    IsModItem = true,
                    CanGive = false,
                    CanSpawn = false,
                    CannotGiveReason = source.Enabled ? "not-runtime-loaded" : "source-disabled",
                    HasIcon = source.IconPath.Length > 0 || source.IconAssetKey.Length > 0,
                    IconPath = source.IconPath,
                    IconAssetKey = source.IconAssetKey,
                    SourceKind = source.SourceKind,
                    SourceModTitle = source.SourceModTitle,
                    SourceId = source.SourceId,
                    WorkshopId = source.WorkshopId,
                    SourceEnabled = source.Enabled,
                    SourceEnablementKnown = source.EnablementKnown,
                    RootPath = source.RootPath,
                    ContentPath = source.ContentPath,
                    LoadOrder = source.LoadOrder,
                    RuntimeOrder = int.MaxValue
                };
                item.SearchText = SearchText(item);
                yield return item;
            }
        }

        private static void ApplySource(
            InventoryDebugItem item,
            IContentItemInfo source)
        {
            item.SourceKind = source.SourceKind;
            item.SourceModTitle = source.SourceModTitle;
            item.SourceId = source.SourceId;
            item.WorkshopId = source.WorkshopId;
            item.SourceEnabled = source.Enabled;
            item.SourceEnablementKnown = source.EnablementKnown;
            item.RootPath = source.RootPath;
            item.ContentPath = source.ContentPath;
            item.LoadOrder = source.LoadOrder;
            item.IconPath = source.IconPath;
            item.IconAssetKey = Native.First(item.IconAssetKey, source.IconAssetKey);
            item.IsModItem = !source.SourceKind.Equals(
                "Vanilla",
                StringComparison.OrdinalIgnoreCase);
            item.Tags = (item.Tags ?? Array.Empty<string>())
                .Concat(source.Tags ?? Array.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            item.CanGive = item.RuntimeLoaded && item.CanSpawn && source.Enabled;
            item.CannotGiveReason = item.CanGive
                ? string.Empty
                : !source.Enabled
                    ? "source-disabled"
                    : !item.RuntimeLoaded
                        ? "not-runtime-loaded"
                        : "not-spawnable";
        }

        private static string SearchText(InventoryDebugItem item) =>
            string.Join(
                " ",
                new[]
                    {
                        item.Id,
                        item.DisplayName,
                        item.ChineseName,
                        item.EnglishName,
                        item.Category,
                        item.SubCategory,
                        item.SourceKind,
                        item.SourceModTitle,
                        item.SourceId,
                        item.IconAssetKey
                    }
                    .Concat(item.Tags ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

        private static string NormalizeItemCategory(string value)
        {
            value = (value ?? string.Empty).Trim();
            return StableItemCategoryIds.FirstOrDefault(id =>
                       id.Equals(value, StringComparison.OrdinalIgnoreCase)) ??
                "special";
        }

        private static InventoryDebugSourceGroup[] BuildSourceGroups(
            IEnumerable<InventoryDebugItem> items)
        {
            List<InventoryDebugItem> list = items.ToList();
            var result = new List<InventoryDebugSourceGroup>
            {
                new InventoryDebugSourceGroup
                {
                    Id = "__base",
                    DisplayName = "本体",
                    SourceKind = "Vanilla",
                    Count = list.Count(item => !item.IsModItem)
                },
                new InventoryDebugSourceGroup
                {
                    Id = "__mods",
                    DisplayName = "模组",
                    SourceKind = "Mod",
                    IsModSource = true,
                    Count = list.Count(item => item.IsModItem),
                    Enabled = list.Where(item => item.IsModItem).All(item => item.SourceEnabled),
                    EnablementKnown = list.Where(item => item.IsModItem).All(item => item.SourceEnablementKnown)
                }
            };
            result.AddRange(
                list.Where(item => item.IsModItem && item.SourceId.Length > 0)
                    .GroupBy(item => item.SourceId, StringComparer.OrdinalIgnoreCase)
                    .Select(group =>
                    {
                        InventoryDebugItem first = group.First();
                        return new InventoryDebugSourceGroup
                        {
                            Id = group.Key,
                            DisplayName = Native.First(first.SourceModTitle, first.SourceId),
                            SourceKind = first.SourceKind,
                            IsModSource = true,
                            Count = group.Count(),
                            Enabled = group.Any(item => item.SourceEnabled),
                            EnablementKnown = group.Any(item => item.SourceEnablementKnown),
                            WorkshopId = first.WorkshopId
                        };
                    })
                    .OrderBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase));
            return result.ToArray();
        }

        private static int InvokeInt(
            MethodInfo? method,
            string itemId,
            int fallback)
        {
            try
            {
                object? value = method?.Invoke(
                    null,
                    new object[] { itemId, false });
                return value == null
                    ? fallback
                    : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static string CurrentSeasonGroupId(object? archive)
        {
            object? currentRoom = Native.Read(archive, "currentRoom");
            object? roomInfo = Native.Read(currentRoom, "RoomInfo");
            return Native.First(
                Native.Text(roomInfo, "SeasonGroupId"),
                Native.Text(currentRoom, "SeasonGroupId"));
        }

        private static object? CurrentSeason(
            object? timeData,
            string seasonGroupId)
        {
            MethodInfo? method = Native.Method(
                timeData?.GetType(),
                "GetSeasonInfo",
                1,
                false);
            return method?.Invoke(timeData, new object[] { seasonGroupId });
        }

        private static IEnumerable<object> CurrentDayWeather(
            object? timeData,
            string seasonGroupId)
        {
            MethodInfo? method = Native.Method(
                timeData?.GetType(),
                "GetWeatherInfoOfDay",
                2,
                false);
            return Native.Enumerate(
                method?.Invoke(timeData, new object[] { seasonGroupId, 0 }));
        }

        private static string WeatherId(object value) =>
            Native.Read(value, "Id")?.ToString() ?? string.Empty;

        private static WeatherDebugOption BuildWeather(
            object weather,
            string current,
            HashSet<string> forecast)
        {
            string id = WeatherId(weather);
            return new WeatherDebugOption
            {
                Id = id,
                DisplayName = Native.First(Native.Text(weather, "Title"), id),
                Description = Native.Text(weather, "Description"),
                IsCurrent = id.Equals(current, StringComparison.OrdinalIgnoreCase),
                IsCurrentDayForecast = forecast.Contains(id),
                IsMalignant = Native.Bool(weather, "IsMalignantWeather"),
                IsRainy = Native.Bool(weather, "IsRainy"),
                IsWindy = Native.Bool(weather, "IsWindy"),
                Sun = Native.Double(weather, "Sun"),
                Water = Native.Double(weather, "Water"),
                Wind = Native.Double(weather, "Wind")
            };
        }

        private static WeatherDebugOption BuildUnavailableWeather(
            string id,
            string current,
            HashSet<string> forecast) =>
            new WeatherDebugOption
            {
                Id = id,
                DisplayName = id,
                IsCurrent = id.Equals(current, StringComparison.OrdinalIgnoreCase),
                IsCurrentDayForecast = forecast.Contains(id)
            };

        private sealed class TeleportSpec
        {
            internal TeleportSpec(
                string id,
                string title,
                string markPointId,
                bool station)
            {
                Id = id;
                Title = title;
                MarkPointId = markPointId;
                IsStation = station;
            }

            internal string Id { get; }
            internal string Title { get; }
            internal string MarkPointId { get; }
            internal bool IsStation { get; }
        }

        private static readonly TeleportSpec[] StableTeleportDirectory =
        {
            new TeleportSpec("station.town", "公车站-小镇", "车站-小镇", true),
            new TeleportSpec("station.outpost", "哨站", "车站-哨站", true),
            new TeleportSpec("station.mountain-path", "山间小路", "车站-山间小路", true),
            new TeleportSpec("station.wetland", "湿地", "湿地-码头出口", true),
            new TeleportSpec("station.dock", "码头", "码头-湿地入口", true),
            new TeleportSpec("station.farm-path", "农场小径", "车站-农场上路", true),
            new TeleportSpec("station.pollution-zone", "污染区", "车站-污染区", true),
            new TeleportSpec("station.peatland", "泥炭地", "车站-泥炭地", true),
            new TeleportSpec("station.water-lily", "睡莲地", "车站-睡莲地", true),
            new TeleportSpec("station.old-city-garrison", "旧城驻守地", "车站-驻守地", true),
            new TeleportSpec("station.residential", "住宅区", "车站-住宅区", true),
            new TeleportSpec("station.commercial", "商业区", "车站-商业区", true),
            new TeleportSpec("station.vacant", "空闲区", "车站-空闲区", true),
            new TeleportSpec("landmark.farm", "农场", "农场-初始位置", false),
            new TeleportSpec("landmark.home", "住所", "列车集市-守车正门外", false),
            new TeleportSpec("landmark.town-hall", "市政厅", "镇政厅-正门外", false),
            new TeleportSpec("landmark.botanical-lab", "植生研究所", "植生研究所-正门外", false),
            new TeleportSpec("landmark.tavern", "酒馆", "酒馆-正门外", false),
            new TeleportSpec("landmark.deer-god-pond", "鹿神池塘", "林地深处-右端", false),
            new TeleportSpec("landmark.mountain-cable-car", "后山缆车", "缆车-后山丘陵", false),
            new TeleportSpec("landmark.witch-hut", "女巫小屋", "湿地-女巫小屋正门外", false),
            new TeleportSpec("landmark.valley-summit-stele", "河谷山顶石碑", "河谷-山顶石碑", false)
        };

        private static object? ResolveMark(string markId)
        {
            object? table = Native.Table("TbMarkPoint");
            MethodInfo? method = Native.Method(
                table?.GetType(),
                "GetOrDefault",
                1,
                false);
            return method?.Invoke(table, new object[] { markId });
        }

        private static TeleportDestination BuildDestination(TeleportSpec spec)
        {
            object? mark = ResolveMark(spec.MarkPointId);
            string nativeTitle = Native.Text(mark, "Title");
            string roomId = Native.Text(mark, "RoomId");
            object? position = Native.Read(mark, "Position");
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            bool hasTransport = Native.Method(
                api,
                "DoTransport",
                5,
                true) != null;
            string unavailable = mark == null
                ? "mark-point-missing"
                : Native.CurrentRoom == null
                    ? "current-room-unavailable"
                    : !hasTransport
                        ? "native-host-unavailable"
                        : string.Empty;
            return new TeleportDestination
            {
                Id = spec.Id,
                DisplayName = Native.First(nativeTitle, spec.Title),
                SuggestedDisplayName = Native.First(nativeTitle, spec.Title),
                Group = spec.IsStation ? "Station" : "Landmark",
                MarkPointId = spec.MarkPointId,
                RoomId = roomId,
                X = Native.Vector(position, "x"),
                Y = Native.Vector(position, "y"),
                IsStation = spec.IsStation,
                IsUnlocked = unavailable.Length == 0,
                Source = unavailable.Length == 0
                    ? "exact-mark-point"
                    : unavailable
            };
        }

        private static TeleportSnapshot Snapshot(object? room, object? position) =>
            new TeleportSnapshot
            {
                RoomId = room == null
                    ? string.Empty
                    : Native.First(
                        Native.Text(room, "RoomId"),
                        Native.Text(room, "roomId"),
                        room.GetType().Name),
                RoomTitle = Native.Text(room, "Title"),
                RoomType = room == null
                    ? string.Empty
                    : Native.First(
                        Native.Read(room, "Type")?.ToString() ?? string.Empty,
                        room.GetType().Name),
                X = Native.Vector(position, "x"),
                Y = Native.Vector(position, "y"),
                Z = Native.Vector(position, "z")
            };

        private InstantSaveDebugState GetSaveState()
        {
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                object? archive = Native.Archive;
                int index = Native.Int(archive, "archiveIndex", -1);
                bool ready = api != null &&
                    archive != null &&
                    Native.Method(api, "SaveGame", 1, true) != null &&
                    index >= 0;
                return new InstantSaveDebugState
                {
                    CanSave = ready,
                    SaveSlot = index >= 0 ? index : (int?)null,
                    CurrentLocation = GetCurrentSnapshot(),
                    FailureReason = ready ? string.Empty : "native-save-owner-unavailable",
                    Message = ready
                        ? "Native SaveGame is ready for slot/index " + index + "."
                        : "Native SaveGame is unavailable."
                };
            }
            catch (Exception error)
            {
                runtime.Error("native-save-state", error);
                return new InstantSaveDebugState
                {
                    FailureReason = error.GetType().Name,
                    Message = error.Message
                };
            }
        }

        private TimeDebugState GetTimeState()
        {
            WeatherDebugState weather = GetState();
            object? date = Native.Read(Native.Archive, "DateNow");
            int start = weather.Hour < 6 ? 0 : weather.Hour < 18 ? 6 : 18;
            int end = weather.Hour < 6 ? 6 : weather.Hour < 18 ? 18 : 24;
            return new TimeDebugState
            {
                Year = weather.Year,
                Month = weather.Month,
                Day = weather.Day,
                Hour = weather.Hour,
                Minute = Native.Int(date, "Minute"),
                CurrentWeatherId = weather.CurrentWeatherId,
                CurrentWeatherName = weather.CurrentWeatherName,
                SeasonName = weather.SeasonName,
                Period = start.ToString("00", CultureInfo.InvariantCulture) +
                    ":00-" +
                    end.ToString("00", CultureInfo.InvariantCulture) +
                    ":00"
            };
        }

        private TimeSkipResult AdvanceMinutes(
            IManifest owner,
            int gameMinutes,
            string action,
            int targetHour)
        {
            var result = new TimeSkipResult
            {
                Before = GetTimeState(),
                AdvancedGameMinutes = gameMinutes,
                TargetHour = targetHour
            };
            try
            {
                object? archive = Native.Archive;
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                object? global = Native.Read(api, "GlobalParameter");
                if (archive == null || api == null || global == null)
                    return Fail(result, "missing-native-time", "Native time owner is unavailable.");
                int seconds = GameMinutesToSeconds(global, gameMinutes);
                MethodInfo? pass = archive.GetType().GetMethods(
                        BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method =>
                        method.Name == "PassTimeNoControl" &&
                        method.GetParameters().Length >= 1);
                if (pass == null)
                    return Fail(result, "missing-pass-time", "ArchiveDataHandle.PassTimeNoControl is unavailable.");
                Action wake = () =>
                    api.GetMethod(
                            "OnWakeUp",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new[] { typeof(bool), typeof(bool), typeof(bool) },
                            null)
                        ?.Invoke(null, new object[] { false, true, false });
                ParameterInfo[] parameters = pass.GetParameters();
                object?[] args = parameters.Length >= 3
                    ? new object?[] { seconds, wake, true }
                    : parameters.Length == 2
                        ? new object?[] { seconds, wake }
                        : new object?[] { seconds };
                pass.Invoke(archive, args);
                result.AdvancedSeconds = seconds;
                result.After = GetTimeState();
                result.Success = true;
                result.Message = "Time action=" + action +
                    " owner=" + (owner?.UniqueID ?? "unknown") +
                    " gameMinutes=" + gameMinutes +
                    " nativeSeconds=" + seconds + ".";
                LogMutation("time-" + action, true, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("time-" + action, error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        private static int GameMinutesToSeconds(object global, int minutes)
        {
            MethodInfo? convert = global.GetType().GetMethod(
                "GameMinutes2Secs",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(float) },
                null);
            object? value = convert?.Invoke(global, new object[] { (float)minutes });
            if (value != null)
                return Math.Max(1, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            int length = Math.Max(1, Native.Int(global, "TULength", 1));
            int tuMinutes = Math.Max(1, Native.Int(global, "TU2Min", 1));
            return Math.Max(1, (int)Math.Round(minutes * (double)length / tuMinutes));
        }

        private static object? ResolvePlayerBody()
        {
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            if (api == null)
                return null;
            if (!ReferenceEquals(playerApiType, api))
            {
                playerApiType = api;
                playerAgentMember = null;
                playerAgentMemberResolved = false;
            }
            if (!playerAgentMemberResolved)
            {
                const BindingFlags Flags = BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static;
                for (Type? current = api;
                    current != null && playerAgentMember == null;
                    current = current.BaseType)
                {
                    playerAgentMember =
                        (MemberInfo?)current.GetProperty("agent", Flags) ??
                        current.GetField("agent", Flags);
                }
                playerAgentMemberResolved = true;
            }
            if (playerAgentMember is PropertyInfo property)
                return property.GetValue(null, null);
            if (playerAgentMember is FieldInfo field)
                return field.GetValue(null);
            return Native.Read(api, "agent");
        }

        private MovementDebugState GetMovementState(string source)
        {
            object? player = ResolvePlayerBody();
            return new MovementDebugState
            {
                Multiplier = movementMultiplier,
                MoveSpeed = Native.Double(player, "MoveSpeed"),
                IsDefault = Math.Abs(movementMultiplier - 1d) < 0.001,
                Source = source
            };
        }

        private MovementSpeedResult ResetMovement(string reason)
        {
            var result = new MovementSpeedResult
            {
                RequestedMultiplier = 1d,
                Before = GetMovementState("before-reset")
            };
            try
            {
                if (movementLeaseActive)
                    runtime.SetMovementMultiplier(null, 1d);
                movementMultiplier = 1d;
                movementOwner = null;
                movementLeaseOwnerId = string.Empty;
                movementLeaseActive = false;
                result.AppliedMultiplier = 1d;
                result.After = GetMovementState("after-reset");
                result.Success = true;
                result.Message =
                    "Product movement multiplier cleared reason=" +
                    reason + "; native MoveScaler was not modified.";
                runtime.Status(
                    "DebugConsole.movement-restore",
                    "restored",
                    runtime.NativeOwnerLabel + " final-speed multiplier",
                    result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("movement-reset", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        private BridgeFeatureStatus Status(string details) =>
            new BridgeFeatureStatus(
                "diagnostic/" + runtime.NativeOwnerLabel.ToLowerInvariant(),
                details);

        private void LogMutation(string id, bool success, string details)
        {
            runtime.Monitor.Log(
                "DebugConsole action=" + id +
                " success=" + success +
                " owner=" + runtime.NativeOwnerLabel +
                " classification=working-or-transient; " + details,
                success ? LogLevel.Info : LogLevel.Warn);
            runtime.Status(
                "DebugConsole." + id,
                success ? "verified" : "failed",
                "DTMAPI.DebugConsole " + runtime.NativeOwnerLabel + " allowlist",
                details);
        }

        private static bool NearlyEqual(double left, double right) =>
            Math.Abs(left - right) < 0.0001d;

        private static InventoryGiveResult Fail(
            InventoryGiveResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static WeatherSetResult Fail(
            WeatherSetResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static TeleportResult Fail(
            TeleportResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static InstantSaveDebugResult Fail(
            InstantSaveDebugResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static TimeSkipResult Fail(
            TimeSkipResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static MovementSpeedResult Fail(
            MovementSpeedResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }
    }
}
