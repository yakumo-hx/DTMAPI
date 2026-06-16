using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        private double movementSpeedMultiplier = 1;
        private string movementSpeedOwnerId = string.Empty;
        private object? movementSpeedMotionAbility;
        private DateTimeOffset lastMovementSpeedReapplyAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastMovementSpeedLeaseStatusAt = DateTimeOffset.MinValue;
        private int movementSpeedReapplyCount;
        private bool creativeModeEnabled;
        private string creativeModeLastMessage = "Creative mode is off.";
        private bool creativeNoCostHooksInstalled;
        private bool creativeNoTimeHooksInstalled;
        private bool creativeCostBypassObserved;
        private bool creativeNoTimeBypassObserved;
        private bool creativeConfigSnapshotValid;
        private bool creativeOriginalIgnoreMaterialCost;
        private bool creativeOriginalSkipMoneyVerifyInShop;
        private bool creativeOriginalIgnoreSpiritCost;
        private bool creativeNativeConfigApplied;
        private double advancedTimeScaleMultiplier = 1d;
        public void PublishHookStatuses()
        {
            runtime.SetHookStatus("Fishing.Automation", "pending", "DTMAPI.GameBridge.DolocTown API", "F6 auto-cast, bite timing, independent skip, visible minigame completion, and animation-speed smoke paths are GameBridge-owned; waiting for fishing hook install in this run.");
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", "pending", "DTMAPI.GameBridge.DolocTown API", "Tool-animation evidence exists in ACTIONSPEED-001; waiting for AgentStateTool hook install in this run. ACTIONSPEED-002 now covers fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest.");
            runtime.SetHookStatus("Debug.InventoryApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses DolocConfig.Tables.TbItem and native DolocAPI item creation/backpack placement paths.");
            runtime.SetHookStatus("Debug.WeatherApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses native WeatherSystem via ArchiveDataHandle.SetWeather and PatchWeather; current-period patching is experimental.");
            runtime.SetHookStatus("Debug.TeleportApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses a whitelist of native mark points and DolocAPI.DoTransport; arbitrary coordinates are not exposed.");
            runtime.SetHookStatus("Debug.TimeApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses ArchiveDataHandle.PassTimeNoControl plus DolocAPI.OnWakeUp to jump to the next weather period; no raw save edit.");
            runtime.SetHookStatus("Debug.MovementApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses native MotionAbility.SetMoveScaler on the player body; reset restores scale 0.");
            runtime.SetHookStatus("Machine.ProductionApi", "contract", "DTMAPI.GameBridge.DolocTown API", "0.2.4 experimental machine contract accepts JSON-backed machine definitions; production/fuel/electric runtime hooks still require third-save implementation evidence.");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", "contract", "DTMAPI.GameBridge.DolocTown API", "0.2.9 experimental equipment-slot contract records extra attribute slots and safe recovery policy; GameBridge owns DTMAPI slot storage, native stat-function application, interactive player equipment strip rendering, and recovery without exposing raw game types.");
        }
        internal void SetAdvancedCreativeHooksInstalled(bool noCostInstalled, bool noTimeInstalled)
        {
            creativeNoCostHooksInstalled = noCostInstalled;
            creativeNoTimeHooksInstalled = noTimeInstalled;
        }

        internal bool ShouldBypassCreativeCostHooks()
        {
            return creativeModeEnabled && creativeNoCostHooksInstalled;
        }

        internal bool ShouldBypassCreativeTimeHooks()
        {
            return creativeModeEnabled && creativeNoTimeHooksInstalled;
        }

        internal void RecordCreativeCostBypassObserved(string source)
        {
            if (creativeCostBypassObserved)
                return;

            creativeCostBypassObserved = true;
            runtime.SetHookStatus("Debug.CreativeMode", "verified", "Y-console creative toggle + " + source, "Runtime no-cost/no-energy prefix observed while creative mode was enabled. generatorAvailable=" + IsNativeItemAvailable("dtmapi_creative_generator") + ".");
        }

        internal void RecordCreativeNoTimeBypassObserved(int originalRecipeTime)
        {
            if (creativeNoTimeBypassObserved)
                return;

            creativeNoTimeBypassObserved = true;
            runtime.SetHookStatus("Debug.CreativeNoTime", "verified", "Harmony Postfix: Synthesizer.GetRecipeTime", "Creative mode changed synthesizer recipe time from " + originalRecipeTime + " TU to 0 TU.");
        }
        public InventoryDebugPage GetItems(InventoryDebugQuery query)
        {
            query ??= new InventoryDebugQuery();
            int pageSize = Math.Max(1, Math.Min(50, query.PageSize <= 0 ? 12 : query.PageSize));
            int page = Math.Max(0, query.Page);
            string search = (query.SearchText ?? string.Empty).Trim();
            string category = (query.Category ?? string.Empty).Trim();
            string sourceId = (query.SourceId ?? string.Empty).Trim();

            try
            {
                List<InventoryDebugItem> all = EnumerateInventoryDebugItems().ToList();
                IEnumerable<InventoryDebugItem> filtered = all;
                if (!query.IncludeUnavailable)
                    filtered = filtered.Where(i => i.CanGive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(i =>
                        i.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.EnglishName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SubCategory.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.IconAssetKey.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceKind.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceModTitle.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (i.WorkshopId.HasValue && i.WorkshopId.Value.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        i.SearchText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.Tags.Any(t => t.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                List<InventoryDebugItem> searchFiltered = filtered.ToList();
                InventoryDebugSourceGroup[] sources = BuildInventoryDebugSourceGroups(searchFiltered);

                IEnumerable<InventoryDebugItem> sourceFiltered = searchFiltered;
                if (!string.IsNullOrWhiteSpace(sourceId))
                    sourceFiltered = FilterInventoryBySource(sourceFiltered, sourceId);
                else if (query.ModItemsOnly)
                    sourceFiltered = sourceFiltered.Where(i => i.IsModItem);

                string[] categories = sourceFiltered
                    .Select(i => FirstText(i.SubCategory, i.Category))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                IEnumerable<InventoryDebugItem> categoryFiltered = sourceFiltered;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    categoryFiltered = categoryFiltered.Where(i =>
                        i.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                        i.SubCategory.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                List<InventoryDebugItem> list = categoryFiltered
                    .OrderBy(i => i.IsModItem ? 1 : 0)
                    .ThenBy(i => i.IsModItem ? i.RuntimeOrder : int.MaxValue)
                    .ThenBy(i => i.IsModItem ? Math.Max(0, i.LoadOrder) : 0)
                    .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)pageSize));
                page = Math.Min(page, totalPages - 1);
                return new InventoryDebugPage
                {
                    Items = list.Skip(page * pageSize).Take(pageSize).ToArray(),
                    Categories = categories,
                    Sources = sources,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = list.Count,
                    TotalPages = totalPages,
                    Status = "ok"
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Inventory debug item enumeration failed.", ex.ToString());
                return new InventoryDebugPage
                {
                    Items = Array.Empty<InventoryDebugItem>(),
                    Categories = Array.Empty<string>(),
                    Sources = Array.Empty<InventoryDebugSourceGroup>(),
                    Page = 0,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 1,
                    Status = ex.GetType().Name + ": " + ex.Message
                };
            }
        }

        public InventoryGiveResult GiveItem(IManifest owner, string itemId, int count)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            itemId = (itemId ?? string.Empty).Trim();
            count = Math.Max(0, count);
            var result = new InventoryGiveResult { ItemId = itemId, RequestedCount = count };
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                result.FailureReason = "invalid-request";
                result.Message = "Invalid item id or count.";
                return result;
            }

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return InventoryGiveFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = new object?[] { itemId, null };
                if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
                    return InventoryGiveFailed(result, "unknown-item", "Item is not present in DolocConfig.Tables.TbItem.");

                object proto = queryArgs[1]!;
                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), itemId);
                int maxStack = Math.Max(1, ReadIntMember(proto, "Overlay", 1));
                IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem(itemId);
                if (sourceInfo != null && !sourceInfo.Enabled)
                    return InventoryGiveFailed(result, "source-disabled", "Item source is disabled by Doloc Town's official Mod UI or Steam Workshop enablement state.");
                int rawOverlay = ReadIntMember(proto, "Overlay", 1);
                if (rawOverlay <= 0)
                    return InventoryGiveFailed(result, "not-spawnable", "Item is present in the runtime table but is marked as not spawnable/stackable.");
                result.BeforeCount = InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, 0);

                MethodInfo? canPlaceItem = dolocApi.GetMethod("CanPlaceItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
                MethodInfo? tryPlaceInBackpack = dolocApi.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
                if (canPlaceItem == null || tryPlaceInBackpack == null)
                    return InventoryGiveFailed(result, "missing-native-method", "Native backpack placement methods are not available.");

                int remaining = count;
                int placed = 0;
                while (remaining > 0)
                {
                    int chunk = Math.Min(remaining, maxStack);
                    object? canPlace = canPlaceItem.Invoke(null, new object?[] { itemId, chunk });
                    if (!(canPlace is bool can && can))
                    {
                        result.FailureReason = placed > 0 ? "partial-inventory-full" : "inventory-full-or-unspawnable";
                        break;
                    }

                    object? placedResult = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, chunk, false });
                    if (!(placedResult is bool ok && ok))
                    {
                        result.FailureReason = placed > 0 ? "partial-native-placement-failed" : "native-placement-failed";
                        break;
                    }

                    placed += chunk;
                    remaining -= chunk;
                }

                result.AfterCount = InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, result.BeforeCount);
                result.GivenCount = Math.Max(placed, result.AfterCount - result.BeforeCount);
                result.Success = result.GivenCount > 0 && string.IsNullOrWhiteSpace(result.FailureReason);
                result.Message = result.Success
                    ? "Gave " + result.GivenCount + " " + result.ItemId + " through native backpack placement."
                    : "Gave " + result.GivenCount + " of " + result.RequestedCount + " " + result.ItemId + "; reason=" + result.FailureReason + ".";

                runtime.RuntimeMonitor.Log("Inventory debug give owner=" + ownerId + " item=" + itemId + " requested=" + count + " placed=" + placed + " before=" + result.BeforeCount + " after=" + result.AfterCount + " success=" + result.Success + " reason=" + result.FailureReason + ".");
                runtime.SetHookStatus(result.Success ? "Smoke.DebugInventoryGive" : "Debug.InventoryGive", result.Success ? "verified" : "failed", "DolocAPI.TryPlaceInBackpack", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Inventory debug give failed for " + itemId + ".", ex.ToString());
                return InventoryGiveFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IInventoryDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Merges the read-only official local/Workshop item source index with runtime DolocConfig.Tables.TbItem and gives only runtime-loaded, enabled, spawnable items through native DolocAPI.TryPlaceInBackpack.");
        }

        public MailItemDeliveryResult SendItemMail(IManifest owner, MailItemDeliveryRequest request)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            request ??= new MailItemDeliveryRequest();
            string itemId = (request.ItemId ?? string.Empty).Trim();
            int count = Math.Max(0, request.Count);
            string templateName = FirstText(request.TemplateName, "send_item_template");
            var result = new MailItemDeliveryResult
            {
                ItemId = itemId,
                RequestedCount = count,
                EmailName = request.EmailName ?? string.Empty,
                TemplateName = templateName
            };
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
                return MailItemDeliveryFailed(result, "invalid-request", "Invalid item id or count.");

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MailItemDeliveryFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = new object?[] { itemId, null };
                if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
                    return MailItemDeliveryFailed(result, "unknown-item", "Item is not present in DolocConfig.Tables.TbItem.");

                object proto = queryArgs[1]!;
                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), itemId);
                IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem(itemId);
                if (sourceInfo != null)
                {
                    result.SourceId = sourceInfo.SourceId;
                    result.SourceEnabled = sourceInfo.Enabled;
                    result.SourceEnablementKnown = sourceInfo.EnablementKnown;
                }
                else
                {
                    result.SourceEnablementKnown = false;
                }

                string requiredSourceId = (request.RequiredSourceId ?? string.Empty).Trim();
                if (request.RequireEnabledContentSource && sourceInfo == null)
                    return MailItemDeliveryFailed(result, "missing-content-source", "Mail item " + itemId + " has no indexed DTMAPI/Workshop content source.");
                if (!string.IsNullOrWhiteSpace(requiredSourceId) &&
                    (sourceInfo == null || !sourceInfo.SourceId.Equals(requiredSourceId, StringComparison.OrdinalIgnoreCase)))
                    return MailItemDeliveryFailed(result, "source-mismatch", "Mail item " + itemId + " source is " + (sourceInfo?.SourceId ?? "none") + ", expected " + requiredSourceId + ".");
                if (sourceInfo != null && !sourceInfo.Enabled)
                    return MailItemDeliveryFailed(result, "source-disabled", "Item source is disabled by Doloc Town's official Mod UI or Steam Workshop enablement state.");
                if (request.RequireEnabledContentSource && sourceInfo != null && !sourceInfo.EnablementKnown)
                    return MailItemDeliveryFailed(result, "source-enable-state-unknown", "Item source enablement state is unknown for " + sourceInfo.SourceId + ".");
                if (!TryGenerateNativeItem(itemId, count, out _, out string itemReason, out string itemMessage))
                    return MailItemDeliveryFailed(result, "attachment-" + itemReason, "Mail attachment cannot be generated before native delivery: " + itemMessage);

                MethodInfo? countItem = dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
                result.BackpackCount = InvokeInt(countItem, null, new object?[] { itemId, false }, 0);
                result.PendingMailCount = CountPendingUnacceptedItemMail(dolocApi, itemId);
                int pendingMailBeforeSend = result.PendingMailCount;

                if (request.SkipIfAlreadyOwned && result.BackpackCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message = "Skipped mail delivery; backpack already contains " + result.BackpackCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus("Mail.ItemDelivery", "skipped", "DolocAPI.CountItem", result.Message);
                    return result;
                }

                if (request.PreventDuplicatePendingMail && result.PendingMailCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message = "Skipped mail delivery; an unclaimed item mail already contains " + result.PendingMailCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus("Mail.ItemDelivery", "skipped", "EmailManager.emails", result.Message);
                    return result;
                }

                MethodInfo? sendItemAsEmail = dolocApi.GetMethod("SendItemAsEmail", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(string), typeof(string), typeof(string), typeof(string) }, null);
                if (sendItemAsEmail == null)
                    return MailItemDeliveryFailed(result, "missing-native-method", "DolocAPI.SendItemAsEmail was not found.");

                object? sent = sendItemAsEmail.Invoke(null, new object?[]
                {
                    itemId,
                    count,
                    string.IsNullOrWhiteSpace(request.EmailName) ? null : request.EmailName,
                    string.IsNullOrWhiteSpace(request.Content) ? null : request.Content,
                    string.IsNullOrWhiteSpace(request.Sender) ? null : request.Sender,
                    templateName
                });
                if (!(sent is bool ok && ok))
                    return MailItemDeliveryFailed(result, "native-rejected", "DolocAPI.SendItemAsEmail returned false.");

                result.PendingMailCount = CountPendingUnacceptedItemMail(dolocApi, itemId);
                if (result.PendingMailCount < pendingMailBeforeSend + count)
                    return MailItemDeliveryFailed(result, "missing-attachment-after-send", "Native mail send returned true but no unclaimed " + itemId + " attachment was observed. before=" + pendingMailBeforeSend + ", after=" + result.PendingMailCount + ".");

                result.Sent = true;
                result.Success = true;
                result.Message = "Sent " + count + " " + itemId + " through native DolocAPI.SendItemAsEmail template=" + templateName + ".";
                LogMailDeliveryResult(ownerId, result);
                runtime.SetHookStatus("Mail.ItemDelivery", "experimental", "DolocAPI.SendItemAsEmail", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mail item delivery failed for " + itemId + ".", ex.ToString());
                return MailItemDeliveryFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IMailDeliveryApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Uses native DolocAPI.SendItemAsEmail with backpack-count and pending-unclaimed-mail duplicate guards. The current game build ignores custom email title/content/sender parameters for item mail, so DTMAPI treats this as a template-based delivery bridge.");
        }

        public WeatherDebugState GetState()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? dateNow = archive == null ? null : ReadMember(archive, "DateNow");
                object? timeData = archive == null ? null : ReadMember(archive, "timeData");
                object? season = timeData == null ? null : ReadMember(timeData, "SeasonProto");
                string currentId = ReadStaticMember(dolocApi, "archiveHandle") == null ? string.Empty : (ReadMember(archive!, "CurrentWeatherType")?.ToString() ?? string.Empty);
                string[] forecastIds = GetCurrentDayWeatherInfos(timeData).Select(GetWeatherId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                WeatherDebugOption? current = GetAvailableWeathers().FirstOrDefault(w => w.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));
                return new WeatherDebugState
                {
                    CurrentWeatherId = currentId,
                    CurrentWeatherName = current?.DisplayName ?? currentId,
                    Year = dateNow == null ? 0 : ReadIntMember(dateNow, "Year", 0),
                    Month = dateNow == null ? 0 : ReadIntMember(dateNow, "Month", 0),
                    Day = dateNow == null ? 0 : ReadIntMember(dateNow, "Day", 0),
                    Hour = dateNow == null ? 0 : ReadIntMember(dateNow, "Hour", 0),
                    SeasonName = season == null ? string.Empty : FirstText(ReadStringMember(season, "Title"), ReadStringMember(season, "Id")),
                    CurrentDayForecastWeatherIds = forecastIds
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug state failed.", ex.ToString());
                return new WeatherDebugState();
            }
        }

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? timeData = archive == null ? null : ReadMember(archive, "timeData");
                string current = archive == null ? string.Empty : (ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty);
                HashSet<string> forecast = new HashSet<string>(GetCurrentDayWeatherInfos(timeData).Select(GetWeatherId), StringComparer.OrdinalIgnoreCase);
                object? table = GetDolocTable("TbWeather");
                object? dataList = table == null ? null : ReadMember(table, "DataList");
                var options = new List<WeatherDebugOption>();
                foreach (object weather in EnumerateObjects(dataList))
                {
                    string id = GetWeatherId(weather);
                    if (string.IsNullOrWhiteSpace(id) || id.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                        continue;
                    options.Add(BuildWeatherOption(weather, current, forecast));
                }

                return options
                    .OrderByDescending(w => w.IsCurrent)
                    .ThenByDescending(w => w.IsCurrentDayForecast)
                    .ThenBy(w => w.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug list failed.", ex.ToString());
                return Array.Empty<WeatherDebugOption>();
            }
        }

        public WeatherSetResult SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            weatherId = (weatherId ?? string.Empty).Trim();
            var result = new WeatherSetResult { WeatherId = weatherId, PatchedCurrentPeriod = patchCurrentPeriod };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                Type? weatherType = ResolveType("DolocTown.Config.Weather.WeatherType, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (dolocApi == null || weatherType == null || archive == null)
                    return WeatherSetFailed(result, "missing-native-weather", "Native weather types are not available.");

                object parsed = Enum.Parse(weatherType, weatherId, ignoreCase: true);
                result.BeforeWeatherId = ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty;
                MethodInfo? setWeather = archive.GetType().GetMethod("SetWeather", BindingFlags.Public | BindingFlags.Instance, null, new[] { weatherType, typeof(bool) }, null);
                if (setWeather == null)
                    return WeatherSetFailed(result, "missing-setweather", "ArchiveDataHandle.SetWeather was not found.");

                setWeather.Invoke(archive, new[] { parsed, (object)true });
                if (patchCurrentPeriod)
                {
                    MethodInfo? patchWeather = archive.GetType().GetMethod("PatchWeather", BindingFlags.Public | BindingFlags.Instance, null, new[] { weatherType }, null);
                    patchWeather?.Invoke(archive, new[] { parsed });
                }

                result.AfterWeatherId = ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty;
                WeatherDebugOption? option = GetAvailableWeathers().FirstOrDefault(w => w.Id.Equals(result.AfterWeatherId, StringComparison.OrdinalIgnoreCase));
                result.DisplayName = option?.DisplayName ?? result.AfterWeatherId;
                result.Success = result.AfterWeatherId.Equals(weatherId, StringComparison.OrdinalIgnoreCase);
                result.Message = "Weather " + result.BeforeWeatherId + " -> " + result.AfterWeatherId + " patchCurrentPeriod=" + patchCurrentPeriod + ".";
                runtime.RuntimeMonitor.Log("Weather debug set owner=" + ownerId + " " + result.Message);
                runtime.SetHookStatus(result.Success ? "Smoke.DebugWeatherSet" : "Debug.WeatherSet", result.Success ? "verified" : "failed", "ArchiveDataHandle.SetWeather/PatchWeather", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug set failed for " + weatherId + ".", ex.ToString());
                return WeatherSetFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IWeatherDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Lists TbWeather and current-day generated weather candidates, then switches via ArchiveDataHandle.SetWeather and PatchWeather.");
        }

        public IReadOnlyList<TeleportDestination> GetDestinations()
        {
            try
            {
                return BuildTeleportDestinations().ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport debug destination enumeration failed.", ex.ToString());
                return Array.Empty<TeleportDestination>();
            }
        }

        public TeleportSnapshot GetCurrentSnapshot()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            return BuildTeleportSnapshot(room, position);
        }

        public TeleportResult Teleport(IManifest owner, string destinationId)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            destinationId = (destinationId ?? string.Empty).Trim();
            var result = new TeleportResult { DestinationId = destinationId, Before = GetCurrentSnapshot() };
            try
            {
                TeleportDestination? destination = GetDestinations().FirstOrDefault(d => d.Id.Equals(destinationId, StringComparison.OrdinalIgnoreCase));
                if (destination == null || string.IsNullOrWhiteSpace(destination.MarkPointId))
                    return TeleportFailed(result, "not-whitelisted", "Destination is not in the DTMAPI debug teleport whitelist.");

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return TeleportFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? doTransport = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return m.Name == "DoTransport" &&
                            p.Length == 5 &&
                            p[0].ParameterType == typeof(string) &&
                            p[2].ParameterType == typeof(bool) &&
                            p[3].ParameterType == typeof(bool) &&
                            p[4].ParameterType == typeof(bool);
                    });
                if (doTransport == null)
                    return TeleportFailed(result, "missing-dotransport", "DolocAPI.DoTransport(markPointId, callback, ...) was not found.");

                result.DestinationName = destination.DisplayName;
                result.MarkPointId = destination.MarkPointId;
                object? accepted = doTransport.Invoke(null, new object?[] { destination.MarkPointId, null, true, false, false });
                result.Success = accepted is bool ok && ok;
                result.AfterRequest = GetCurrentSnapshot();
                if (!result.Success)
                    return TeleportFailed(result, "native-rejected", "DolocAPI.DoTransport rejected " + destination.MarkPointId + ".");

                result.Message = "Teleport request accepted destination=" + destination.DisplayName + " markPoint=" + destination.MarkPointId + " beforeRoom=" + result.Before.RoomId + " targetRoom=" + destination.RoomId + ".";
                runtime.RuntimeMonitor.Log("Teleport debug request owner=" + ownerId + " " + result.Message);
                runtime.SetHookStatus("Smoke.DebugTeleportRequest", "verified", "DolocAPI.DoTransport", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport debug request failed for " + destinationId + ".", ex.ToString());
                return TeleportFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus ITeleportDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Uses whitelisted stations/key mark points and native DolocAPI.DoTransport instead of arbitrary coordinate writes.");
        }

        public TeleportCsvExportResult ExportDestinationsCsv(IManifest owner)
        {
            var result = new TeleportCsvExportResult();
            try
            {
                TeleportDestination[] destinations = GetDestinations().ToArray();
                string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "TELEPORT-DESTINATIONS", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(evidenceDir);
                string path = Path.Combine(evidenceDir, "teleport-destinations.csv");
                var csv = new StringBuilder();
                csv.AppendLine("internal_id,map,x,y,current_display_name,suggested_name,source,mark_point_id,group,is_station");
                foreach (TeleportDestination destination in destinations)
                {
                    csv.Append(Csv(destination.Id)).Append(',')
                        .Append(Csv(destination.RoomId)).Append(',')
                        .Append(destination.X.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(destination.Y.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(Csv(destination.DisplayName)).Append(',')
                        .Append(Csv(FirstText(destination.SuggestedDisplayName, destination.DisplayName))).Append(',')
                        .Append(Csv(destination.Source)).Append(',')
                        .Append(Csv(destination.MarkPointId)).Append(',')
                        .Append(Csv(destination.Group)).Append(',')
                        .Append(destination.IsStation ? "true" : "false").AppendLine();
                }
                File.WriteAllText(path, csv.ToString(), Encoding.UTF8);

                result.Success = true;
                result.Path = path;
                result.RowCount = destinations.Length;
                result.Message = "Exported " + destinations.Length + " teleport destination row(s) for manual name review.";
                runtime.RuntimeMonitor.Log("Teleport debug CSV export owner=" + (owner?.UniqueID ?? "unknown") + " rows=" + result.RowCount + " path=" + path + ".");
                runtime.SetHookStatus("Debug.TeleportCsvExport", "verified", "ITeleportDebugApi.GetDestinations", result.Message + " path=" + path);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport destination CSV export failed.", ex.ToString());
                runtime.SetHookStatus("Debug.TeleportCsvExport", "failed", "ITeleportDebugApi.GetDestinations", ex.GetType().Name + ": " + ex.Message);
                return TeleportCsvExportFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        InstantSaveDebugState IInstantSaveDebugApi.GetState()
        {
            return GetInstantSaveDebugState();
        }

        public InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new InstantSaveDebugResult
            {
                ReloadAfterSave = reloadAfterSave,
                Before = GetInstantSaveDebugState()
            };
            if (reloadAfterSave)
                return InstantSaveFailed(result, "reload-disabled", "Save-then-immediate-load is disabled because the native running-save reload path can leave scene residue.");

            result.SaveSlot = result.Before.SaveSlot;
            if (!result.Before.CanSave || result.Before.SaveSlot == null)
                return InstantSaveFailed(result, FirstText(result.Before.FailureReason, "not-saveable"), FirstText(result.Before.Message, "Current save slot is not available."));

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                if (dolocApi == null || saveGame == null)
                    return InstantSaveFailed(result, "missing-savegame", "DolocAPI.SaveGame(int) was not found.");

                int gameIndex = result.Before.SaveSlot.Value;
                runtime.RuntimeMonitor.Log("Instant save debug requested owner=" + ownerId + " slot/index=" + gameIndex + " location=" + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".");
                runtime.SetHookStatus("Debug.InstantSave", "pending", "DolocAPI.SaveGame", "Requested save from current location " + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".");
                object? saveResult = saveGame.Invoke(null, new object[] { gameIndex });
                if (saveResult is bool saved && !saved)
                    return InstantSaveFailed(result, "native-rejected", "DolocAPI.SaveGame returned false for slot/index " + gameIndex + ".");

                result.AfterSave = GetInstantSaveDebugState();
                result.Success = true;
                result.Message = "Saved slot/index=" + gameIndex + " at " + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".";

                runtime.RuntimeMonitor.Log("Instant save debug OK owner=" + ownerId + " slot/index=" + gameIndex + " reload=False before=" + FormatTeleportSnapshot(result.Before.CurrentLocation) + " afterSave=" + FormatTeleportSnapshot(result.AfterSave.CurrentLocation) + ".");
                runtime.SetHookStatus("Debug.InstantSave", "verified", "DolocAPI.SaveGame", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Instant save debug failed.", ex.ToString());
                runtime.SetHookStatus("Debug.InstantSave", "failed", "DolocAPI.SaveGame", ex.GetType().Name + ": " + ex.Message);
                return InstantSaveFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IInstantSaveDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental-save-only", "Saves the current loaded slot from the Y console through native DolocAPI.SaveGame. Save-then-immediate-load requests are disabled because the native running-save reload path can leave scene residue.");
        }

        TimeDebugState ITimeDebugApi.GetState()
        {
            return GetTimeDebugState();
        }

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new TimeSkipResult { Before = GetTimeDebugState() };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                if (dolocApi == null || archive == null || globalParameter == null)
                    return TimeSkipFailed(result, "missing-native-time", "DolocAPI archive/global parameter objects are not available.");

                int hour2Min = Math.Max(1, ReadIntMember(globalParameter, "Hour2Min", 60));
                int day2Hour = Math.Max(1, ReadIntMember(globalParameter, "Day2Hour", 24));
                int currentHour = Math.Max(0, result.Before.Hour);
                int currentMinute = Math.Max(0, result.Before.Minute);
                int targetHour = GetNextDebugWeatherPeriodTarget(currentHour);
                int advancedMinutes = ((targetHour > currentHour ? targetHour - currentHour : targetHour + day2Hour - currentHour) * hour2Min) - currentMinute;
                if (advancedMinutes <= 0)
                    advancedMinutes = Math.Max(1, day2Hour * hour2Min - currentMinute);

                int seconds = Math.Max(1, GameMinutesToSeconds(globalParameter, advancedMinutes));
                MethodInfo? passTime = archive.GetType().GetMethod("PassTimeNoControl", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(Action), typeof(bool) }, null)
                    ?? archive.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "PassTimeNoControl" && m.GetParameters().Length >= 1);
                if (passTime == null)
                    return TimeSkipFailed(result, "missing-pass-time", "ArchiveDataHandle.PassTimeNoControl was not found.");

                Action wake = () => InvokeWakeUp(dolocApi);
                ParameterInfo[] parameters = passTime.GetParameters();
                object?[] args = parameters.Length >= 3
                    ? new object?[] { seconds, wake, true }
                    : parameters.Length == 2
                        ? new object?[] { seconds, wake }
                        : new object?[] { seconds };
                passTime.Invoke(archive, args);

                result.After = GetTimeDebugState();
                result.AdvancedGameMinutes = advancedMinutes;
                result.AdvancedSeconds = seconds;
                result.TargetHour = targetHour;
                result.Success = true;
                result.Message = "Time advanced to next weather period owner=" + ownerId +
                    " targetHour=" + targetHour +
                    " minutes=" + advancedMinutes +
                    " seconds=" + seconds +
                    " before=" + FormatTimeDebugState(result.Before) +
                    " after=" + FormatTimeDebugState(result.After) + ".";
                runtime.RuntimeMonitor.Log("Time debug skip OK " + result.Message);
                runtime.SetHookStatus("Smoke.DebugTimeSkip", "verified", "ArchiveDataHandle.PassTimeNoControl + DolocAPI.OnWakeUp", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Time debug skip failed.", ex.ToString());
                return TimeSkipFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus ITimeDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Skips to the next 06:00/18:00/24:00 weather period with ArchiveDataHandle.PassTimeNoControl and DolocAPI.OnWakeUp.");
        }

        MovementDebugState IMovementDebugApi.GetState()
        {
            return GetMovementDebugState("query");
        }

        public MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            multiplier = ClampDebugSpeedMultiplier(multiplier);
            var result = new MovementSpeedResult
            {
                RequestedMultiplier = multiplier,
                Before = GetMovementDebugState("before")
            };

            try
            {
                object? motionAbility = ResolveMotionAbility();
                if (motionAbility == null)
                {
                    if (Math.Abs(multiplier - 1d) < 0.001)
                    {
                        ClearMovementDebugLeaseState();
                        result.After = GetMovementDebugState("after");
                        result.AppliedMultiplier = 1;
                        result.Success = true;
                        result.Message = "Movement debug lease cleared; native MotionAbility was not available for a 1x reset.";
                        runtime.SetHookStatus("Debug.MovementLease", "disabled", "IMovementDebugApi -> MotionAbility.SetMoveScaler lease", result.Message);
                        return result;
                    }

                    return MovementSpeedFailed(result, "missing-motion-ability", "Player MotionAbility was not found.");
                }

                if (!TryApplyMovementScale(motionAbility, multiplier, out string failureReason, out string failureMessage))
                    return MovementSpeedFailed(result, failureReason, failureMessage);
                movementSpeedMultiplier = multiplier;
                movementSpeedOwnerId = Math.Abs(multiplier - 1d) < 0.001 ? string.Empty : ownerId;
                movementSpeedMotionAbility = Math.Abs(multiplier - 1d) < 0.001 ? null : motionAbility;
                lastMovementSpeedReapplyAt = DateTimeOffset.UtcNow;
                movementSpeedReapplyCount = 0;
                result.After = GetMovementDebugState("after");
                result.AppliedMultiplier = movementSpeedMultiplier;
                result.Success = true;
                result.Message = "Movement speed owner=" + ownerId + " multiplier=" + multiplier.ToString("0.###") + " beforeSpeed=" + FormatRatio(result.Before.MoveSpeed) + " afterSpeed=" + FormatRatio(result.After.MoveSpeed) + ".";
                runtime.RuntimeMonitor.Log("Movement debug speed OK " + result.Message);
                runtime.SetHookStatus("Smoke.DebugMovementSpeed", "verified", "MotionAbility.SetMoveScaler", result.Message);
                runtime.SetHookStatus(
                    "Debug.MovementLease",
                    string.IsNullOrWhiteSpace(movementSpeedOwnerId) ? "disabled" : "active",
                    "IMovementDebugApi -> MotionAbility.SetMoveScaler lease",
                    string.IsNullOrWhiteSpace(movementSpeedOwnerId)
                        ? "Movement debug lease is inactive after reset."
                        : "Movement debug lease active owner=" + movementSpeedOwnerId + " multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) + ".");
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Movement debug speed failed.", ex.ToString());
                return MovementSpeedFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MovementSpeedResult ResetSpeed(IManifest owner, string reason)
        {
            MovementSpeedResult result = SetSpeedMultiplier(owner, 1);
            if (result.Success)
                result.Message += " resetReason=" + (reason ?? string.Empty);
            return result;
        }

        internal void UpdateMovementDebugLease(string reason)
        {
            if (Math.Abs(movementSpeedMultiplier - 1d) < 0.001 || string.IsNullOrWhiteSpace(movementSpeedOwnerId))
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            object? motionAbility = ResolveMotionAbility();
            if (motionAbility == null)
                return;

            bool motionChanged = movementSpeedMotionAbility != null && !ReferenceEquals(movementSpeedMotionAbility, motionAbility);
            if (!motionChanged && (now - lastMovementSpeedReapplyAt).TotalSeconds < 1)
                return;

            if (!TryApplyMovementScale(motionAbility, movementSpeedMultiplier, out string failureReason, out string failureMessage))
            {
                if ((now - lastMovementSpeedLeaseStatusAt).TotalSeconds >= 30)
                {
                    lastMovementSpeedLeaseStatusAt = now;
                    runtime.SetHookStatus("Debug.MovementLease", "failed", "IMovementDebugApi -> MotionAbility.SetMoveScaler lease", failureReason + ": " + failureMessage);
                }
                return;
            }

            movementSpeedMotionAbility = motionAbility;
            lastMovementSpeedReapplyAt = now;
            movementSpeedReapplyCount++;
            if (motionChanged || movementSpeedReapplyCount <= 3 || (now - lastMovementSpeedLeaseStatusAt).TotalSeconds >= 30)
            {
                lastMovementSpeedLeaseStatusAt = now;
                MovementDebugState state = GetMovementDebugState("lease-reapply");
                string summary = "owner=" + movementSpeedOwnerId +
                    ", multiplier=" + movementSpeedMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", reason=" + (reason ?? string.Empty) +
                    ", motionChanged=" + motionChanged +
                    ", reapplyCount=" + movementSpeedReapplyCount.ToString(CultureInfo.InvariantCulture) +
                    ", moveSpeed=" + FormatRatio(state.MoveSpeed) + ".";
                runtime.RuntimeMonitor.Log("Movement debug lease reapplied " + summary);
                runtime.SetHookStatus("Debug.MovementLease", "active", "IMovementDebugApi -> MotionAbility.SetMoveScaler lease", summary);
            }
        }

        internal void ResetMovementDebugLease(string reason)
        {
            if (Math.Abs(movementSpeedMultiplier - 1d) < 0.001 && string.IsNullOrWhiteSpace(movementSpeedOwnerId))
                return;

            object? motionAbility = ResolveMotionAbility();
            if (motionAbility != null)
                TryApplyMovementScale(motionAbility, 1, out _, out _);

            ClearMovementDebugLeaseState();
            runtime.SetHookStatus("Debug.MovementLease", "disabled", "ReturnedToTitle/Reset movement boundary", "Movement debug lease reset for " + (reason ?? string.Empty) + ".");
        }

        private void ClearMovementDebugLeaseState()
        {
            movementSpeedMultiplier = 1;
            movementSpeedOwnerId = string.Empty;
            movementSpeedMotionAbility = null;
            movementSpeedReapplyCount = 0;
            lastMovementSpeedReapplyAt = DateTimeOffset.UtcNow;
        }

        BridgeFeatureStatus IMovementDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Sets player movement through MotionAbility.SetMoveScaler and maintains a lightweight debug-only lease until reset or title return.");
        }

        public IReadOnlyList<TechPointDebugOption> GetTechPointOptions()
        {
            var result = new List<TechPointDebugOption>();
            try
            {
                Type? techPointType = ResolveType("DolocTown.Config.TechTree.TechPointType, Assembly-CSharp");
                if (techPointType == null || !techPointType.IsEnum)
                    return result;

                foreach (object value in Enum.GetValues(techPointType))
                {
                    string id = value.ToString() ?? string.Empty;
                    GetNativeTechPointSnapshot(value, out int points, out int level);
                    result.Add(new TechPointDebugOption
                    {
                        Id = id,
                        DisplayName = LocalizeTechPointId(id),
                        CurrentPoints = points,
                        CurrentLevel = level
                    });
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug tech-point option enumeration failed.", ex.ToString());
            }
            return result.OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public IReadOnlyList<SpawnDebugOption> GetMonsterOptions()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? assets = ReadStaticMember(dolocApi, "assets");
                object? monsters = assets == null ? null : ReadMember(assets, "monsters");
                object? totalProtos = monsters == null ? null : ReadMember(monsters, "TotalProtos");
                bool available = IsCurrentRoomSpawnHost("DolocTown.IMonsterHost, Assembly-CSharp");
                return EnumerateObjects(totalProtos)
                    .Select(proto => new SpawnDebugOption
                    {
                        Id = FirstText(ReadStringMember(proto, "Id"), ReadStringMember(proto, "id"), ReadStringMember(proto, "Name"), ReadStringMember(proto, "name")),
                        DisplayName = FirstText(ReadStringMember(proto, "Title"), ReadStringMember(proto, "title"), ReadStringMember(proto, "Name"), ReadStringMember(proto, "Id")),
                        Category = ReadMember(proto, "MonsterType")?.ToString() ?? "monster",
                        IsAvailableInCurrentRoom = available
                    })
                    .Where(o => !string.IsNullOrWhiteSpace(o.Id))
                    .OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Take(80)
                    .ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug monster option enumeration failed.", ex.ToString());
                return Array.Empty<SpawnDebugOption>();
            }
        }

        public IReadOnlyList<SpawnDebugOption> GetResourceOptions()
        {
            try
            {
                object? resourceList = ReadConfigTableList("TbResource");
                bool available = IsCurrentRoomSpawnHost("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                return EnumerateObjects(resourceList)
                    .Select(proto => new SpawnDebugOption
                    {
                        Id = FirstText(ReadStringMember(proto, "Id"), ReadStringMember(proto, "id"), ReadStringMember(proto, "Name"), ReadStringMember(proto, "name")),
                        DisplayName = FirstText(ReadStringMember(proto, "Title"), ReadStringMember(proto, "title"), ReadStringMember(proto, "Id")),
                        Category = FirstText(ReadMember(proto, "ResourceClass")?.ToString() ?? string.Empty, ReadMember(proto, "ResourceType")?.ToString() ?? string.Empty, "resource"),
                        IsAvailableInCurrentRoom = available
                    })
                    .Where(o => !string.IsNullOrWhiteSpace(o.Id))
                    .OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Take(80)
                    .ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug resource option enumeration failed.", ex.ToString());
                return Array.Empty<SpawnDebugOption>();
            }
        }

        public CreativeModeState GetCreativeModeState()
        {
            return new CreativeModeState
            {
                Enabled = creativeModeEnabled,
                RuntimeHooksInstalled = creativeNoCostHooksInstalled && creativeNoTimeHooksInstalled,
                GeneratorRuntimeAvailable = IsNativeItemAvailable("dtmapi_creative_generator"),
                GeneratorItemId = "dtmapi_creative_generator",
                LastMessage = creativeModeLastMessage
            };
        }

        internal string VerifyAdvancedCreativeHooksForSmoke()
        {
            if (!creativeModeEnabled)
                throw new InvalidOperationException("Creative mode is not enabled.");
            CreativeModeState state = GetCreativeModeState();
            if (!state.RuntimeHooksInstalled)
                throw new InvalidOperationException("Creative no-cost/no-time hooks are not installed.");
            if (!ReadCreativeNativeDebugFlags(out bool ignoreMaterialCost, out bool skipMoneyVerifyInShop, out bool ignoreSpiritCost))
                throw new InvalidOperationException("Creative native GameInitConfig flags are unavailable.");
            if (!ignoreMaterialCost || !skipMoneyVerifyInShop || !ignoreSpiritCost)
                throw new InvalidOperationException("Creative native GameInitConfig flags are not active. ignoreMaterialCost=" + ignoreMaterialCost + ", skipMoneyVerifyInShop=" + skipMoneyVerifyInShop + ", ignoreSpiritCost=" + ignoreSpiritCost + ".");

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? canAffordMoney = dolocApi?.GetMethod("CanAffordMoney", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            object? canAffordResult = canAffordMoney?.Invoke(null, new object?[] { int.MaxValue });
            if (!(canAffordResult is bool canAfford) || !canAfford)
                throw new InvalidOperationException("Creative CanAffordMoney(int.MaxValue) did not return true.");

            int? energyBefore = ReadAgentEnergy();
            MethodInfo? costEnergy = dolocApi?.GetMethod("CostEnergy", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            object? costEnergyResult = costEnergy?.Invoke(null, new object?[] { 1 });
            int? energyAfter = ReadAgentEnergy();
            if (!(costEnergyResult is bool costOk) || !costOk)
                throw new InvalidOperationException("Creative CostEnergy(1) did not return true.");
            if (energyBefore.HasValue && energyAfter.HasValue && energyBefore.Value != energyAfter.Value)
                throw new InvalidOperationException("Creative CostEnergy(1) changed energy " + energyBefore.Value + "->" + energyAfter.Value + ".");

            string summary = "nativeFlags{ignoreMaterialCost=" + ignoreMaterialCost + ",skipMoneyVerifyInShop=" + skipMoneyVerifyInShop + ",ignoreSpiritCost=" + ignoreSpiritCost + "}, canAffordMoneyIntMax=True, costEnergyNoChange=" + (energyBefore.HasValue && energyAfter.HasValue ? (energyBefore.Value == energyAfter.Value).ToString() : "unknown") + ", noTimeHookInstalled=" + creativeNoTimeHooksInstalled + ", generatorAvailable=" + state.GeneratorRuntimeAvailable;
            runtime.SetHookStatus("Debug.CreativeMode", "verified", "Y-console creative toggle + Harmony no-cost smoke", summary);
            return summary;
        }

        private bool ApplyCreativeNativeDebugFlags()
        {
            object? config = GetGameInitConfig();
            if (config == null)
            {
                creativeNativeConfigApplied = false;
                return false;
            }

            if (!creativeConfigSnapshotValid)
            {
                creativeOriginalIgnoreMaterialCost = ReadBoolMember(config, "ignoreMaterialCost", false);
                creativeOriginalSkipMoneyVerifyInShop = ReadBoolMember(config, "skipMoneyVerifyInShop", false);
                creativeOriginalIgnoreSpiritCost = ReadBoolMember(config, "ignoreSpiritCost", false);
                creativeConfigSnapshotValid = true;
            }

            bool material = WriteBoolMember(config, "ignoreMaterialCost", true);
            bool shop = WriteBoolMember(config, "skipMoneyVerifyInShop", true);
            bool spirit = WriteBoolMember(config, "ignoreSpiritCost", true);
            creativeNativeConfigApplied = material && shop && spirit;
            return creativeNativeConfigApplied;
        }

        private bool RestoreCreativeNativeDebugFlags()
        {
            object? config = GetGameInitConfig();
            if (!creativeConfigSnapshotValid)
            {
                creativeNativeConfigApplied = false;
                return true;
            }
            if (config == null)
            {
                creativeNativeConfigApplied = false;
                return false;
            }

            bool material = WriteBoolMember(config, "ignoreMaterialCost", creativeOriginalIgnoreMaterialCost);
            bool shop = WriteBoolMember(config, "skipMoneyVerifyInShop", creativeOriginalSkipMoneyVerifyInShop);
            bool spirit = WriteBoolMember(config, "ignoreSpiritCost", creativeOriginalIgnoreSpiritCost);
            bool restored = material && shop && spirit;
            if (restored)
            {
                creativeConfigSnapshotValid = false;
                creativeNativeConfigApplied = false;
            }
            return restored;
        }

        private static bool ReadCreativeNativeDebugFlags(out bool ignoreMaterialCost, out bool skipMoneyVerifyInShop, out bool ignoreSpiritCost)
        {
            object? config = GetGameInitConfig();
            if (config == null)
            {
                ignoreMaterialCost = false;
                skipMoneyVerifyInShop = false;
                ignoreSpiritCost = false;
                return false;
            }

            ignoreMaterialCost = ReadBoolMember(config, "ignoreMaterialCost", false);
            skipMoneyVerifyInShop = ReadBoolMember(config, "skipMoneyVerifyInShop", false);
            ignoreSpiritCost = ReadBoolMember(config, "ignoreSpiritCost", false);
            return true;
        }

        private static object? GetGameInitConfig()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? gameManager = ReadStaticMember(dolocApi, "gameManager");
            return gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
        }

        private static bool IsNativeItemAvailable(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? queryItemProto = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = new object?[] { itemId, null };
                return queryItemProto?.Invoke(null, queryArgs) is bool found && found && queryArgs[1] != null;
            }
            catch
            {
                return false;
            }
        }

        private static int? ReadAgentEnergy()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
                if (agentData == null)
                    return null;
                object? value = ReadMember(agentData, "energy");
                return value == null ? null : Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        public TimeSkipResult AdvanceTime(IManifest owner, AdvancedTimeAdvanceKind kind, int amount)
        {
            amount = Math.Max(1, Math.Min(52, amount));
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new TimeSkipResult { Before = GetTimeDebugState() };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                if (dolocApi == null || archive == null || globalParameter == null)
                    return TimeSkipFailed(result, "missing-native-time", "DolocAPI archive/global parameter objects are not available.");

                int seconds;
                string source;
                if (kind == AdvancedTimeAdvanceKind.Month)
                {
                    seconds = GameMonthsToSeconds(globalParameter, amount);
                    source = "GameMonths2Secs";
                }
                else
                {
                    int days = kind == AdvancedTimeAdvanceKind.Week ? amount * 7 : amount;
                    seconds = GameDaysToSeconds(globalParameter, days);
                    source = kind == AdvancedTimeAdvanceKind.Week ? "GameDays2Secs(week)" : "GameDays2Secs";
                }

                if (!InvokeNativePassTime(archive, dolocApi, seconds, out string passMessage))
                    return TimeSkipFailed(result, "missing-pass-time", passMessage);

                result.After = GetTimeDebugState();
                result.AdvancedSeconds = seconds;
                result.AdvancedGameMinutes = EstimateAdvancedGameMinutes(globalParameter, seconds);
                result.TargetHour = result.After.Hour;
                result.Success = true;
                result.Message = "Advanced time owner=" + ownerId + " kind=" + kind + " amount=" + amount + " seconds=" + seconds + " source=" + source + " before=" + FormatTimeDebugState(result.Before) + " after=" + FormatTimeDebugState(result.After) + ".";
                runtime.RuntimeMonitor.Log("Advanced debug time OK " + result.Message);
                runtime.SetHookStatus("Debug.AdvancedTime", "verified", "ArchiveDataHandle.PassTimeNoControl + DolocAPI.OnWakeUp", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced time debug failed.", ex.ToString());
                return TimeSkipFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public TimeScaleDebugResult SetTimeScale(IManifest owner, double multiplier)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            multiplier = ClampAdvancedTimeScale(multiplier);
            var result = new TimeScaleDebugResult
            {
                RequestedMultiplier = multiplier,
                BeforeMultiplier = advancedTimeScaleMultiplier
            };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? setTimeScale = dolocApi?.GetMethod("SetTimeScale", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(float), typeof(bool) }, null);
                if (dolocApi == null || setTimeScale == null)
                    return TimeScaleFailed(result, "missing-set-time-scale", "DolocAPI.SetTimeScale(float,bool) was not found.");

                setTimeScale.Invoke(null, new object[] { (float)multiplier, true });
                advancedTimeScaleMultiplier = multiplier;
                result.AfterMultiplier = advancedTimeScaleMultiplier;
                result.Success = true;
                result.Message = "Time scale owner=" + ownerId + " multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) + ".";
                runtime.RuntimeMonitor.Log("Advanced debug time scale OK " + result.Message);
                runtime.SetHookStatus("Debug.TimeScale", "verified", "DolocAPI.SetTimeScale", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced time-scale debug failed.", ex.ToString());
                return TimeScaleFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public TimeScaleDebugResult ResetTimeScale(IManifest owner, string reason)
        {
            var result = new TimeScaleDebugResult
            {
                RequestedMultiplier = 1,
                BeforeMultiplier = advancedTimeScaleMultiplier
            };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? revert = dolocApi?.GetMethod("RevertTimeScale", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (dolocApi == null || revert == null)
                    return TimeScaleFailed(result, "missing-revert-time-scale", "DolocAPI.RevertTimeScale() was not found.");

                revert.Invoke(null, null);
                advancedTimeScaleMultiplier = 1;
                result.AfterMultiplier = 1;
                result.Success = true;
                result.Message = "Time scale reset reason=" + (reason ?? string.Empty) + ".";
                runtime.RuntimeMonitor.Log("Advanced debug time scale reset OK " + result.Message);
                runtime.SetHookStatus("Debug.TimeScale", "verified", "DolocAPI.RevertTimeScale", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced time-scale reset failed.", ex.ToString());
                return TimeScaleFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public DebugValueResult AddMoney(IManifest owner, int amount)
        {
            amount = Math.Max(1, Math.Min(100000, amount));
            var result = new DebugValueResult { ValueId = "money", RequestedDelta = amount };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (dolocApi == null || archive == null)
                    return DebugValueFailed(result, "missing-archive", "DolocAPI.archiveHandle is not available.");

                result.BeforeValue = ReadIntMember(archive, "CurrentMoney", 0);
                MethodInfo? commandAddMoney = FindMethod(dolocApi, "Command_AddMoney", 1);
                if (commandAddMoney != null)
                    commandAddMoney.Invoke(null, new object[] { amount });
                else
                    SetMemberValue(archive, "CurrentMoney", result.BeforeValue + amount);

                result.AfterValue = ReadIntMember(archive, "CurrentMoney", result.BeforeValue + amount);
                result.Success = result.AfterValue >= result.BeforeValue + amount;
                result.Message = "Added money +" + amount + " before=" + result.BeforeValue + " after=" + result.AfterValue + ".";
                runtime.RuntimeMonitor.Log("Advanced debug money OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.AddMoney", result.Success ? "verified" : "failed", commandAddMoney != null ? "DolocAPI.Command_AddMoney" : "ArchiveDataHandle.CurrentMoney", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug add money failed.", ex.ToString());
                return DebugValueFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public DebugValueResult AddTechPoint(IManifest owner, string pointTypeId, int amount)
        {
            pointTypeId = (pointTypeId ?? string.Empty).Trim();
            amount = Math.Max(1, Math.Min(1000, amount));
            var result = new DebugValueResult { ValueId = pointTypeId, RequestedDelta = amount };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                Type? techPointType = ResolveType("DolocTown.Config.TechTree.TechPointType, Assembly-CSharp");
                if (dolocApi == null || techPointType == null || !techPointType.IsEnum)
                    return DebugValueFailed(result, "missing-tech-point-type", "DolocAPI or TechPointType is unavailable.");
                if (!TryParseEnum(techPointType, pointTypeId, out object? typeValue))
                    return DebugValueFailed(result, "not-whitelisted", "Unknown tech point type: " + pointTypeId + ".");

                GetNativeTechPointSnapshot(typeValue!, out int before, out _);
                result.BeforeValue = before;
                MethodInfo? addTechPoint = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "AddTechPoint" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int));
                if (addTechPoint == null)
                    return DebugValueFailed(result, "missing-add-tech-point", "DolocAPI.AddTechPoint(TechPointType,int) was not found.");

                addTechPoint.Invoke(null, new object[] { typeValue!, amount });
                GetNativeTechPointSnapshot(typeValue!, out int after, out _);
                result.AfterValue = after;
                result.Success = after >= before + amount;
                result.Message = "Added tech point " + pointTypeId + " +" + amount + " before=" + before + " after=" + after + ".";
                runtime.RuntimeMonitor.Log("Advanced debug tech point OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.AddTechPoint", result.Success ? "verified" : "failed", "DolocAPI.AddTechPoint", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug add tech point failed.", ex.ToString());
                return DebugValueFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public DebugCommandResult UnlockAllTechTrees(IManifest owner)
        {
            var result = new DebugCommandResult { CommandId = "unlock_all_tech_trees" };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? unlocked = farmData == null ? null : ReadMember(farmData, "unlockedTechTree");
                object? techTrees = ReadConfigTableList("TbTechTree");
                if (unlocked == null || techTrees == null)
                    return DebugCommandFailed(result, "missing-tech-tree-data", "Native tech-tree tables or save collection are unavailable.");

                int before = CountEnumerable(unlocked);
                int affected = 0;
                foreach (object proto in EnumerateObjects(techTrees))
                {
                    string id = FirstText(ReadStringMember(proto, "Id"), ReadStringMember(proto, "id"));
                    if (!string.IsNullOrWhiteSpace(id) && AddToNativeCollection(unlocked, id))
                        affected++;
                }

                result.AffectedCount = affected;
                result.Success = affected > 0 || CountEnumerable(unlocked) >= before;
                result.Message = "Unlocked tech tree ids affected=" + affected + " before=" + before + " after=" + CountEnumerable(unlocked) + ".";
                runtime.RuntimeMonitor.Log("Advanced debug unlock tech tree OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.UnlockAllTechTrees", result.Success ? "verified" : "failed", "farmData.unlockedTechTree", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug unlock tech trees failed.", ex.ToString());
                return DebugCommandFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public CropMaturityResult MatureAllCrops(IManifest owner)
        {
            var result = new CropMaturityResult();
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (dolocApi == null || archive == null || currentRoom == null)
                    return CropMaturityFailed(result, "missing-room", "Current room/archive is unavailable.");

                var notes = new List<string>();
                foreach (object equipment in EnumerateMachineCandidateEquipments(dolocApi, archive, currentRoom))
                {
                    string typeName = equipment.GetType().FullName ?? equipment.GetType().Name;
                    if (typeName.IndexOf("PlantBasin", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    result.PlantBasinsVisited++;
                    object? crop = ReadMember(equipment, "Crop");
                    if (crop == null && typeName.IndexOf("PlantBasinGrass", StringComparison.OrdinalIgnoreCase) >= 0)
                        crop = equipment;
                    if (crop == null)
                        continue;

                    if (TryMatureCrop(crop, out string note))
                    {
                        result.CropsMatured++;
                        if (notes.Count < 6)
                            notes.Add(note);
                    }
                }

                result.Success = result.PlantBasinsVisited > 0;
                result.Message = "Matured crops=" + result.CropsMatured + "/" + result.PlantBasinsVisited + (notes.Count == 0 ? string.Empty : " samples=" + string.Join("|", notes));
                runtime.RuntimeMonitor.Log("Advanced debug mature crops OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.MatureAllCrops", result.Success ? "verified" : "pending", "PlantBasin.Crop.DEBUG_SetLevel", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug mature all crops failed.", ex.ToString());
                return CropMaturityFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public CreativeModeResult SetCreativeMode(IManifest owner, bool enabled)
        {
            var result = new CreativeModeResult
            {
                Enabled = enabled,
                Before = GetCreativeModeState()
            };
            creativeModeEnabled = enabled;
            bool nativeConfigApplied = enabled ? ApplyCreativeNativeDebugFlags() : RestoreCreativeNativeDebugFlags();
            if (enabled)
            {
                bool runtimeHooksInstalled = creativeNoCostHooksInstalled && creativeNoTimeHooksInstalled;
                creativeModeLastMessage = "Creative mode is on. runtimeHooksInstalled=" + runtimeHooksInstalled + ", noCostHooks=" + creativeNoCostHooksInstalled + ", noTimeHooks=" + creativeNoTimeHooksInstalled + ", nativeConfigApplied=" + nativeConfigApplied + ", generatorAvailable=" + IsNativeItemAvailable("dtmapi_creative_generator") + ".";
            }
            else
            {
                creativeModeLastMessage = "Creative mode is off. nativeConfigRestored=" + nativeConfigApplied + ".";
            }
            result.After = GetCreativeModeState();
            result.Success = true;
            result.Message = creativeModeLastMessage;
            runtime.RuntimeMonitor.Log("Advanced debug creative mode owner=" + (owner?.UniqueID ?? "unknown") + " enabled=" + enabled + ". " + result.Message, enabled ? DTMAPI.Abstractions.LogLevel.Warn : DTMAPI.Abstractions.LogLevel.Info);
            runtime.SetHookStatus("Debug.CreativeMode", enabled ? (result.After.RuntimeHooksInstalled ? "experimental" : "pending") : "off", "Y-console creative toggle + GameInitConfig debug flags", result.Message);
            return result;
        }

        public InventoryGiveResult GiveCreativeGenerator(IManifest owner)
        {
            InventoryGiveResult result = GiveItem(owner, "dtmapi_creative_generator", 1);
            runtime.SetHookStatus("Debug.CreativeGeneratorGive", result.Success ? "verified" : "pending", "IInventoryDebugApi.GiveItem", result.Message);
            return result;
        }

        public SpawnDebugResult SpawnMonster(IManifest owner, string monsterId, int count)
        {
            monsterId = (monsterId ?? string.Empty).Trim();
            count = Math.Max(1, Math.Min(10, count));
            var result = new SpawnDebugResult { SpawnId = monsterId, RequestedCount = count };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                Type? hostType = ResolveType("DolocTown.IMonsterHost, Assembly-CSharp");
                if (currentRoom == null || hostType == null || !hostType.IsInstanceOfType(currentRoom))
                    return SpawnFailed(result, "unsupported-room", "Current room does not support monster spawning.");

                object? assets = ReadStaticMember(dolocApi, "assets");
                object? monsters = assets == null ? null : ReadMember(assets, "monsters");
                MethodInfo? query = monsters == null ? null : FindMethodInHierarchy(monsters.GetType(), "QueryMonster", 2);
                object?[] queryArgs = new object?[] { monsterId, null };
                if (!(query?.Invoke(monsters, queryArgs) is bool found) || !found || queryArgs[1] == null)
                    return SpawnFailed(result, "unknown-monster", "Monster is not present in native monster assets.");

                object proto = queryArgs[1]!;
                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), monsterId);
                MethodInfo? generate = dolocApi?.GetMethod("Command_GenerateMonster", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
                if (generate == null)
                    return SpawnFailed(result, "missing-official-command", "DolocAPI.Command_GenerateMonster(string,int) was not found.");

                generate.Invoke(null, new object[] { monsterId, count });
                result.SpawnedCount = count;

                result.Success = result.SpawnedCount == count;
                result.Message = "Spawned monster " + monsterId + " count=" + result.SpawnedCount + " through official Command_GenerateMonster.";
                runtime.RuntimeMonitor.Log("Advanced debug monster spawn OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.SpawnMonster", result.Success ? "verified" : "failed", "DolocAPI.Command_GenerateMonster", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug monster spawn failed.", ex.ToString());
                return SpawnFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public SpawnDebugResult SpawnResource(IManifest owner, string resourceId, int count)
        {
            resourceId = (resourceId ?? string.Empty).Trim();
            count = Math.Max(1, Math.Min(10, count));
            var result = new SpawnDebugResult { SpawnId = resourceId, RequestedCount = count };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                Type? hostType = ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                if (currentRoom == null || hostType == null || !hostType.IsInstanceOfType(currentRoom))
                    return SpawnFailed(result, "unsupported-room", "Current room does not support direct resource spawning.");

                object? resourceTable = GetConfigTable("TbResource");
                MethodInfo? getOrDefault = FindMethodInHierarchy(resourceTable?.GetType(), "GetOrDefault", 1);
                object? proto = getOrDefault?.Invoke(resourceTable, new object[] { resourceId });
                if (proto == null)
                    return SpawnFailed(result, "unknown-resource", "Resource is not present in DolocConfig.Tables.TbResource.");

                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), resourceId);
                MethodInfo? createNoRender = FindMethodInHierarchy(hostType, "CreateDungeonResourceNoRender", 2) ?? FindMethodInHierarchy(currentRoom.GetType(), "CreateDungeonResourceNoRender", 2);
                MethodInfo? renderResource = FindMethodInHierarchy(hostType, "RenderResource", 1) ?? FindMethodInHierarchy(currentRoom.GetType(), "RenderResource", 1);
                if (createNoRender == null)
                    return SpawnFailed(result, "missing-create-resource", "IDungeonResourceHost.CreateDungeonResourceNoRender was not found.");

                object? agentPosition = ReadStaticMember(dolocApi, "AgentPosition");
                int baseX = (int)Math.Round(ReadVectorComponent(agentPosition, "x"));
                int baseY = (int)Math.Round(ReadVectorComponent(agentPosition, "y"));
                var created = new List<object>();
                for (int i = 0; i < count; i++)
                {
                    object? pos = CreateUnityVector2Int(baseX + (i % 5), baseY + (i / 5));
                    if (pos == null)
                        continue;
                    object? resource = createNoRender.Invoke(currentRoom, new[] { pos, proto });
                    if (resource != null)
                    {
                        created.Add(resource);
                        result.SpawnedCount++;
                    }
                }

                if (created.Count > 0 && renderResource != null && ReadBoolMember(currentRoom, "isRenderNow", true))
                {
                    Array array = Array.CreateInstance(created[0].GetType(), created.Count);
                    for (int i = 0; i < created.Count; i++)
                        array.SetValue(created[i], i);
                    renderResource.Invoke(currentRoom, new object[] { array });
                }

                result.Success = result.SpawnedCount > 0;
                result.Message = "Spawned resource " + resourceId + " count=" + result.SpawnedCount + " at " + baseX + "," + baseY + ".";
                runtime.RuntimeMonitor.Log("Advanced debug resource spawn OK owner=" + (owner?.UniqueID ?? "unknown") + " " + result.Message);
                runtime.SetHookStatus("Debug.SpawnResource", result.Success ? "verified" : "failed", "IDungeonResourceHost.CreateDungeonResourceNoRender", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced debug resource spawn failed.", ex.ToString());
                return SpawnFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IAdvancedDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Whitelisted Y-console advanced debug API. Implemented: time advance/time scale, money, tech points, tech-tree unlock, crop maturity, creative no-cost/no-time toggle, creative generator give, and current-room monster/resource spawn. Dangerous raw Lua/load/reset/story commands remain excluded.");
        }
        private static IEnumerable<InventoryDebugItem> FilterInventoryBySource(IEnumerable<InventoryDebugItem> items, string sourceId)
        {
            if (sourceId.Equals("__base", StringComparison.OrdinalIgnoreCase))
                return items.Where(i => !i.IsModItem);
            if (sourceId.Equals("__mods", StringComparison.OrdinalIgnoreCase))
                return items.Where(i => i.IsModItem);
            return items.Where(i => i.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
        }

        private static InventoryDebugSourceGroup[] BuildInventoryDebugSourceGroups(IEnumerable<InventoryDebugItem> items)
        {
            List<InventoryDebugItem> list = items.ToList();
            var groups = new List<InventoryDebugSourceGroup>();
            int baseCount = list.Count(i => !i.IsModItem);
            groups.Add(new InventoryDebugSourceGroup
            {
                Id = "__base",
                DisplayName = "本体",
                SourceKind = "Vanilla",
                IsModSource = false,
                Count = baseCount,
                Enabled = true,
                EnablementKnown = true
            });

            List<InventoryDebugItem> modItems = list.Where(i => i.IsModItem).ToList();
            groups.Add(new InventoryDebugSourceGroup
            {
                Id = "__mods",
                DisplayName = "模组",
                SourceKind = "Mod",
                IsModSource = true,
                Count = modItems.Count,
                Enabled = modItems.All(i => i.SourceEnabled),
                EnablementKnown = modItems.All(i => i.SourceEnablementKnown)
            });

            groups.AddRange(modItems
                .Where(i => !string.IsNullOrWhiteSpace(i.SourceId))
                .GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    InventoryDebugItem first = g
                        .OrderBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.RuntimeOrder)
                        .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                        .First();
                    return new InventoryDebugSourceGroup
                    {
                        Id = g.Key,
                        DisplayName = FirstText(first.SourceModTitle, first.SourceId, first.SourceKind),
                        SourceKind = first.SourceKind,
                        IsModSource = true,
                        Count = g.Count(),
                        Enabled = g.Any(i => i.SourceEnabled),
                        EnablementKnown = g.Any(i => i.SourceEnablementKnown),
                        WorkshopId = first.WorkshopId
                    };
                })
                .OrderBy(g => g.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id, StringComparer.OrdinalIgnoreCase));

            return groups.ToArray();
        }

        private IEnumerable<InventoryDebugItem> EnumerateInventoryDebugItems()
        {
            Dictionary<string, IContentItemInfo> sources = runtime.GetIndexedContentItems()
                .GroupBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(i => i.Enabled)
                        .ThenBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.SourceKind, StringComparer.OrdinalIgnoreCase)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);

            var runtimeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int runtimeOrder = 0;
            foreach (InventoryDebugItem item in EnumerateRuntimeItems(runtimeOrderStart: 0, idSink: runtimeIds))
            {
                if (sources.TryGetValue(item.Id, out IContentItemInfo source))
                    ApplyContentSource(item, source);
                else
                {
                    item.SourceKind = "Vanilla";
                    item.SourceModTitle = "Doloc Town";
                    item.SourceId = "Vanilla";
                    item.SourceEnabled = true;
                    item.SourceEnablementKnown = true;
                    item.CanGive = item.CanSpawn;
                    item.CannotGiveReason = item.CanGive ? string.Empty : "not-spawnable";
                    item.SearchText = BuildInventorySearchText(item);
                }

                item.RuntimeOrder = runtimeOrder++;
                yield return item;
            }

            foreach (IContentItemInfo source in sources.Values
                .Where(s => !runtimeIds.Contains(s.ItemId))
                .OrderBy(s => s.LoadOrder < 0 ? int.MaxValue : s.LoadOrder)
                .ThenBy(s => s.ItemId, StringComparer.OrdinalIgnoreCase))
            {
                yield return CreateSourceOnlyInventoryItem(source);
            }
        }

        private IEnumerable<InventoryDebugItem> EnumerateRuntimeItems(int runtimeOrderStart, ISet<string> idSink)
        {
            object? table = GetDolocTable("TbItem");
            object? dataList = table == null ? null : ReadMember(table, "DataList");
            int runtimeOrder = runtimeOrderStart;
            foreach (object proto in EnumerateObjects(dataList))
            {
                string id = ReadStringMember(proto, "Id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                idSink.Add(id);

                object? mainType = ReadMember(proto, "MainType");
                object? subType = ReadMember(proto, "SubType_Ref");
                object? spriteAsset = ReadMember(proto, "UiSpriteAsset");
                string subCategory = FirstText(ReadStringMember(proto, "SubType"), subType == null ? string.Empty : ReadStringMember(subType, "Title"));
                string category = mainType == null ? subCategory : FirstText(ReadStringMember(mainType, "Title"), ReadStringMember(mainType, "Id"), subCategory);
                string englishName = FirstText(ReadStringMember(proto, "EnglishTitle"), ReadStringMember(proto, "TitleEn"), ReadStringMember(proto, "Name"), id);
                string[] tags = CollectInventoryTags(proto, mainType, subType).ToArray();
                string icon = spriteAsset?.ToString() ?? string.Empty;
                int overlay = ReadIntMember(proto, "Overlay", 1);
                var item = new InventoryDebugItem
                {
                    Id = id,
                    DisplayName = FirstText(ReadStringMember(proto, "Title"), id),
                    ChineseName = FirstText(ReadStringMember(proto, "Title"), id),
                    EnglishName = englishName,
                    Category = category,
                    SubCategory = subCategory,
                    Tags = tags,
                    MaxStack = Math.Max(1, overlay),
                    CanSpawn = overlay > 0,
                    CanGive = overlay > 0,
                    RuntimeLoaded = true,
                    IsModItem = false,
                    CannotGiveReason = overlay > 0 ? string.Empty : "not-spawnable",
                    HasIcon = spriteAsset != null,
                    IconAssetKey = icon,
                    RuntimeOrder = runtimeOrder++
                };
                item.SearchText = BuildInventorySearchText(item);
                yield return item;
            }
        }

        private static void ApplyContentSource(InventoryDebugItem item, IContentItemInfo source)
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
            item.IconAssetKey = FirstText(item.IconAssetKey, source.IconAssetKey);
            item.ChineseName = FirstText(item.ChineseName, source.ChineseName);
            item.EnglishName = FirstText(item.EnglishName, source.EnglishName);
            item.IsModItem = !source.SourceKind.Equals("Vanilla", StringComparison.OrdinalIgnoreCase);
            item.Tags = item.Tags.Concat(source.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
            item.CanGive = item.RuntimeLoaded && item.CanSpawn && source.Enabled;
            item.CannotGiveReason = item.CanGive
                ? string.Empty
                : (!source.Enabled ? "source-disabled" : (!item.RuntimeLoaded ? "not-runtime-loaded" : "not-spawnable"));
            item.SearchText = BuildInventorySearchText(item);
        }

        private static InventoryDebugItem CreateSourceOnlyInventoryItem(IContentItemInfo source)
        {
            var item = new InventoryDebugItem
            {
                Id = source.ItemId,
                DisplayName = FirstText(source.ChineseName, source.EnglishName, source.ItemId),
                ChineseName = FirstText(source.ChineseName, source.ItemId),
                EnglishName = source.EnglishName,
                Category = source.Category,
                SubCategory = source.Category,
                Tags = source.Tags,
                MaxStack = 0,
                CanSpawn = false,
                CanGive = false,
                RuntimeLoaded = false,
                IsModItem = true,
                CannotGiveReason = source.Enabled ? "not-runtime-loaded" : "source-disabled",
                HasIcon = !string.IsNullOrWhiteSpace(source.IconPath) || !string.IsNullOrWhiteSpace(source.IconAssetKey),
                IconAssetKey = source.IconAssetKey,
                IconPath = source.IconPath,
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
            item.SearchText = BuildInventorySearchText(item);
            return item;
        }

        private static string BuildInventorySearchText(InventoryDebugItem item)
        {
            return string.Join(" ", new[]
                {
                    item.Id,
                    item.DisplayName,
                    item.ChineseName,
                    item.EnglishName,
                    item.Category,
                    item.SubCategory,
                    item.IconAssetKey,
                    item.SourceKind,
                    item.SourceModTitle,
                    item.SourceId,
                    item.WorkshopId?.ToString() ?? string.Empty,
                    item.ContentPath
                }
                .Concat(item.Tags ?? Array.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray());
        }

        private static IEnumerable<string> CollectInventoryTags(object proto, object? mainType, object? subType)
        {
            var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            AddInventoryTag(tags, ReadStringMember(proto, "Id"));
            AddInventoryTag(tags, ReadStringMember(proto, "ItemType"));
            AddInventoryTag(tags, ReadStringMember(proto, "Type"));
            AddInventoryTag(tags, ReadMember(proto, "MainType")?.ToString() ?? string.Empty);
            AddInventoryTag(tags, ReadMember(proto, "SubType")?.ToString() ?? string.Empty);
            AddInventoryTag(tags, mainType == null ? string.Empty : ReadStringMember(mainType, "Id"));
            AddInventoryTag(tags, subType == null ? string.Empty : ReadStringMember(subType, "Id"));
            foreach (string memberName in new[] { "Tags", "Labels", "CollectionLabels", "ItemCollectionLabels", "FoodItemSubTypes" })
            {
                foreach (string value in ReadStringValues(ReadMember(proto, memberName)))
                    AddInventoryTag(tags, value);
            }
            return tags;
        }

        private static void AddInventoryTag(ISet<string> tags, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                tags.Add(value.Trim());
        }

        private static IEnumerable<string> ReadStringValues(object? value)
        {
            if (value == null)
                yield break;
            if (value is string text)
            {
                yield return text;
                yield break;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null && !string.IsNullOrWhiteSpace(item.ToString()))
                        yield return item.ToString()!;
                }
            }
        }

        private static int CountPendingUnacceptedItemMail(Type dolocApi, string itemId)
        {
            if (dolocApi == null || string.IsNullOrWhiteSpace(itemId))
                return 0;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? emailManager = farmData == null ? null : ReadMember(farmData, "emailManager");
            object? emails = emailManager == null ? null : ReadMember(emailManager, "emails");
            int count = 0;
            foreach (object email in EnumerateObjects(emails))
            {
                object? attaches = ReadMember(email, "emailAttaches");
                foreach (object attach in EnumerateObjects(attaches))
                {
                    if (ReadBoolMember(attach, "IsAccept", false) || ReadBoolMember(attach, "isAccept", false))
                        continue;

                    object? reward = ReadMember(attach, "reward");
                    if (reward == null)
                        continue;

                    string rewardItemId = ReadStringMember(reward, "itemName");
                    if (!itemId.Equals(rewardItemId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    count += Math.Max(0, ReadIntMember(reward, "itemCount", 0));
                }
            }
            return count;
        }

        private void LogMailDeliveryResult(string ownerId, MailItemDeliveryResult result)
        {
            runtime.RuntimeMonitor.Log("Mail item delivery owner=" + ownerId +
                " item=" + result.ItemId +
                " requested=" + result.RequestedCount +
                " sent=" + result.Sent +
                " skipped=" + result.Skipped +
                " backpack=" + result.BackpackCount +
                " pendingMail=" + result.PendingMailCount +
                " template=" + result.TemplateName +
                " source=" + FirstText(result.SourceId, "none") +
                " sourceEnabled=" + result.SourceEnabled +
                " sourceKnown=" + result.SourceEnablementKnown +
                " success=" + result.Success +
                " reason=" + result.FailureReason + ".");
        }

        private static InventoryGiveResult InventoryGiveFailed(InventoryGiveResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static MailItemDeliveryResult MailItemDeliveryFailed(MailItemDeliveryResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static WeatherSetResult WeatherSetFailed(WeatherSetResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TeleportResult TeleportFailed(TeleportResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TeleportCsvExportResult TeleportCsvExportFailed(TeleportCsvExportResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static InstantSaveDebugResult InstantSaveFailed(InstantSaveDebugResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TimeSkipResult TimeSkipFailed(TimeSkipResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static MovementSpeedResult MovementSpeedFailed(MovementSpeedResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private bool GetNativeTechPointSnapshot(object pointType, out int points, out int level)
        {
            points = 0;
            level = 0;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                Type? operationGlobal = ResolveType("DolocTown.GameData.ArchiveOperationGlobal, Assembly-CSharp");
                MethodInfo? getPoint = operationGlobal?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "GetTechPoint" && m.GetParameters().Length == 2);
                MethodInfo? getLevel = operationGlobal?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "GetTechLevel" && m.GetParameters().Length == 2);
                if (archive != null && getPoint != null && getLevel != null)
                {
                    points = Convert.ToInt32(getPoint.Invoke(null, new[] { archive, pointType }), CultureInfo.InvariantCulture);
                    level = Convert.ToInt32(getLevel.Invoke(null, new[] { archive, pointType }), CultureInfo.InvariantCulture);
                    return true;
                }

                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? manager = farmData == null ? null : ReadMember(farmData, "techLevelManager");
                MethodInfo? getLevelData = manager == null ? null : FindMethodInHierarchy(manager.GetType(), "GetLevelData", 1);
                object? data = getLevelData?.Invoke(manager, new[] { pointType });
                if (data == null)
                    return false;

                points = ReadIntMember(data, "AvailablePoints", ReadIntMember(data, "availablePoints", 0));
                level = ReadIntMember(data, "CurrentLevel", ReadIntMember(data, "currentLevel", 0));
                return true;
            }
            catch
            {
                points = 0;
                level = 0;
                return false;
            }
        }

        private static string LocalizeTechPointId(string id)
        {
            switch ((id ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "NATURE":
                    return "自然";
                case "OPERATE":
                    return "经营";
                case "SCIENCE":
                    return "科学";
                case "ANIMAL":
                    return "动物";
                case "BATTLE":
                    return "战斗";
                case "FISHING":
                    return "钓鱼";
                default:
                    return id ?? string.Empty;
            }
        }

        private bool IsCurrentRoomSpawnHost(string hostTypeName)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            Type? hostType = ResolveType(hostTypeName);
            return currentRoom != null && hostType != null && hostType.IsInstanceOfType(currentRoom);
        }

        private object? ReadConfigTableList(string tableName)
        {
            object? table = GetConfigTable(tableName);
            return table == null ? null : ReadMember(table, "DataList");
        }

        private object? GetConfigTable(string tableName)
        {
            return GetDolocTable(tableName);
        }

        private static int GameDaysToSeconds(object globalParameter, int days)
        {
            MethodInfo? convert = globalParameter.GetType().GetMethod("GameDays2Secs", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (convert != null)
            {
                object? value = convert.Invoke(globalParameter, new object[] { (float)Math.Max(1, days) });
                if (value != null)
                    return Math.Max(1, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }

            int day2Hour = Math.Max(1, ReadIntMember(globalParameter, "Day2Hour", 24));
            int hour2Min = Math.Max(1, ReadIntMember(globalParameter, "Hour2Min", 60));
            return GameMinutesToSeconds(globalParameter, Math.Max(1, days) * day2Hour * hour2Min);
        }

        private static int GameMonthsToSeconds(object globalParameter, int months)
        {
            MethodInfo? convert = globalParameter.GetType().GetMethod("GameMonths2Secs", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (convert != null)
            {
                object? value = convert.Invoke(globalParameter, new object[] { (float)Math.Max(1, months) });
                if (value != null)
                    return Math.Max(1, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }

            int month2Day = Math.Max(1, ReadIntMember(globalParameter, "Month2Day", 30));
            return GameDaysToSeconds(globalParameter, Math.Max(1, months) * month2Day);
        }

        private static bool InvokeNativePassTime(object archive, Type dolocApi, int seconds, out string message)
        {
            message = string.Empty;
            MethodInfo? passTime = archive.GetType().GetMethod("PassTimeNoControl", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(Action), typeof(bool) }, null)
                ?? archive.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "PassTimeNoControl" && m.GetParameters().Length >= 1);
            if (passTime == null)
            {
                message = "ArchiveDataHandle.PassTimeNoControl was not found.";
                return false;
            }

            Action wake = () => InvokeWakeUp(dolocApi);
            ParameterInfo[] parameters = passTime.GetParameters();
            object?[] args = parameters.Length >= 3
                ? new object?[] { Math.Max(1, seconds), wake, true }
                : parameters.Length == 2
                    ? new object?[] { Math.Max(1, seconds), wake }
                    : new object?[] { Math.Max(1, seconds) };
            passTime.Invoke(archive, args);
            message = "PassTimeNoControl seconds=" + Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture) + ".";
            return true;
        }

        private static int EstimateAdvancedGameMinutes(object globalParameter, int seconds)
        {
            int tuLength = Math.Max(1, ReadIntMember(globalParameter, "TULength", 1));
            int tu2Min = Math.Max(1, ReadIntMember(globalParameter, "TU2Min", 1));
            return Math.Max(1, (int)Math.Round(Math.Max(1, seconds) * (double)tu2Min / tuLength));
        }

        private static double ClampAdvancedTimeScale(double multiplier)
        {
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return 1d;
            return Math.Max(1d, Math.Min(16d, multiplier));
        }

        private static TimeScaleDebugResult TimeScaleFailed(TimeScaleDebugResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.AfterMultiplier = result.BeforeMultiplier;
            return result;
        }

        private static DebugValueResult DebugValueFailed(DebugValueResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.AfterValue = result.BeforeValue;
            return result;
        }

        private static DebugCommandResult DebugCommandFailed(DebugCommandResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static CropMaturityResult CropMaturityFailed(CropMaturityResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static SpawnDebugResult SpawnFailed(SpawnDebugResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static bool TryParseEnum(Type enumType, string id, out object? value)
        {
            value = null;
            if (enumType == null || !enumType.IsEnum || string.IsNullOrWhiteSpace(id))
                return false;
            try
            {
                value = Enum.Parse(enumType, id.Trim(), ignoreCase: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int CountEnumerable(object? value)
        {
            if (value == null)
                return 0;
            if (value is ICollection collection)
                return collection.Count;
            int count = 0;
            foreach (object _ in EnumerateObjects(value))
                count++;
            return count;
        }

        private static bool AddToNativeCollection(object collection, string value)
        {
            if (collection == null || string.IsNullOrWhiteSpace(value))
                return false;
            MethodInfo? contains = FindMethodInHierarchy(collection.GetType(), "Contains", 1);
            if (contains != null && contains.Invoke(collection, new object[] { value }) is bool alreadyPresent && alreadyPresent)
                return false;

            MethodInfo? add = FindMethodInHierarchy(collection.GetType(), "Add", 1);
            object? added = add?.Invoke(collection, new object[] { value });
            return added is bool boolResult ? boolResult : add != null;
        }

        private static bool TryMatureCrop(object crop, out string note)
        {
            note = crop == null ? "missing-crop" : crop.GetType().Name;
            if (crop == null)
                return false;

            MethodInfo? debugSetLevelWithRender = crop.GetType().GetMethod("DEBUG_SetLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool), typeof(int) }, null);
            MethodInfo? debugSetLevel = crop.GetType().GetMethod("DEBUG_SetLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            int matureLevel = ReadIntMember(crop, "MatureLevel", 5);
            object? proto = ReadMember(crop, "Proto") ?? ReadMember(crop, "proto") ?? ReadMember(crop, "cropInfo");
            matureLevel = Math.Max(matureLevel, proto == null ? matureLevel : ReadIntMember(proto, "MatureLevel", matureLevel));
            matureLevel = Math.Max(1, Math.Min(99, matureLevel));

            if (debugSetLevelWithRender != null)
            {
                debugSetLevelWithRender.Invoke(crop, new object[] { true, matureLevel });
                note = crop.GetType().Name + ":" + matureLevel.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (debugSetLevel != null)
            {
                debugSetLevel.Invoke(crop, new object[] { matureLevel });
                note = crop.GetType().Name + ":" + matureLevel.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        private object? GetDolocTable(string tableName)
        {
            object? tables = GetDolocTables();
            return tables == null ? null : ReadMember(tables, tableName);
        }

        private static object? GetDolocTables()
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            return ReadStaticMember(dolocConfig, "Tables");
        }
        private static string GetWeatherId(object? weatherInfo)
        {
            if (weatherInfo == null)
                return string.Empty;
            return ReadMember(weatherInfo, "Id")?.ToString() ?? string.Empty;
        }

        private static IEnumerable<object> GetCurrentDayWeatherInfos(object? timeData)
        {
            MethodInfo? getWeatherInfoOfDay = timeData?.GetType().GetMethod("GetWeatherInfoOfDay", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            object? result = getWeatherInfoOfDay?.Invoke(timeData, new object?[] { 0 });
            return EnumerateObjects(result).ToArray();
        }

        private static WeatherDebugOption BuildWeatherOption(object weather, string currentWeatherId, HashSet<string> forecastIds)
        {
            string id = GetWeatherId(weather);
            return new WeatherDebugOption
            {
                Id = id,
                DisplayName = FirstText(ReadStringMember(weather, "Title"), id),
                Description = ReadStringMember(weather, "Description"),
                IsCurrent = id.Equals(currentWeatherId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                IsCurrentDayForecast = forecastIds.Contains(id),
                IsMalignant = ReadBoolMember(weather, "IsMalignantWeather", false),
                IsRainy = ReadBoolMember(weather, "IsRainy", false),
                IsWindy = ReadBoolMember(weather, "IsWindy", false),
                Sun = ReadDoubleMember(weather, "Sun", 0),
                Water = ReadDoubleMember(weather, "Water", 0),
                Wind = ReadDoubleMember(weather, "Wind", 0)
            };
        }

        private IEnumerable<TeleportDestination> BuildTeleportDestinations()
        {
            var destinations = new Dictionary<string, TeleportDestination>(StringComparer.OrdinalIgnoreCase);
            string initMarkPoint = GetInitMarkPointId();
            if (!string.IsNullOrWhiteSpace(initMarkPoint))
                AddTeleportDestination(destinations, "farm:" + initMarkPoint, "农场", "Farm", initMarkPoint, isStation: false, suggestedName: string.Empty, source: "GameInitConfig.initMarkPoint");

            object? stationTable = GetDolocTable("TbStation");
            object? stationList = stationTable == null ? null : ReadMember(stationTable, "DataList");
            foreach (object station in EnumerateObjects(stationList))
            {
                string stationId = ReadStringMember(station, "Id");
                string markPointId = ReadStringMember(station, "MarkPointId");
                if (string.IsNullOrWhiteSpace(stationId) || string.IsNullOrWhiteSpace(markPointId))
                    continue;
                object? stationMarkPoint = ResolveMarkPoint(markPointId);
                string stationRoomId = stationMarkPoint == null ? string.Empty : ReadStringMember(stationMarkPoint, "RoomId");
                string title = BuildStationDisplayName(stationId, ReadStringMember(station, "Title"), markPointId, stationRoomId);
                AddTeleportDestination(destinations, "station:" + stationId, title, "Station", markPointId, isStation: true, suggestedName: BuildMarkPointDisplayName(markPointId, stationRoomId), source: "TbStation.MarkPointId");
            }

            object? markPointTable = GetDolocTable("TbMarkPoint");
            object? markPointList = markPointTable == null ? null : ReadMember(markPointTable, "DataList");
            foreach (object markPoint in EnumerateObjects(markPointList))
            {
                string markPointId = ReadStringMember(markPoint, "Id");
                string roomId = ReadStringMember(markPoint, "RoomId");
                if (!IsWhitelistedMarkPoint(markPointId, roomId))
                    continue;
                AddTeleportDestination(destinations, "mark:" + markPointId, BuildMarkPointDisplayName(markPointId, roomId), "Key", markPointId, isStation: false, suggestedName: BuildMarkPointDisplayName(markPointId, roomId), source: "TbMarkPoint whitelist");
            }

            return destinations.Values
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .OrderBy(d => d.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(80)
                .ToArray();
        }

        private void AddTeleportDestination(Dictionary<string, TeleportDestination> destinations, string id, string displayName, string group, string markPointId, bool isStation, string suggestedName, string source)
        {
            if (destinations.ContainsKey(id))
                return;
            object? markPoint = ResolveMarkPoint(markPointId);
            if (markPoint == null)
                return;
            object? position = ReadMember(markPoint, "Position");
            string roomId = ReadStringMember(markPoint, "RoomId");
            destinations[id] = new TeleportDestination
            {
                Id = id,
                DisplayName = FirstText(displayName, markPointId),
                SuggestedDisplayName = FirstText(suggestedName, BuildMarkPointDisplayName(markPointId, roomId), displayName, markPointId),
                Group = group,
                MarkPointId = markPointId,
                RoomId = roomId,
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                IsStation = isStation,
                IsUnlocked = true,
                Source = source ?? string.Empty
            };
        }

        private object? ResolveMarkPoint(string markPointId)
        {
            object? table = GetDolocTable("TbMarkPoint");
            MethodInfo? getOrDefault = table?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            return getOrDefault?.Invoke(table, new object?[] { markPointId });
        }

        private static string GetInitMarkPointId()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? gameManager = ReadStaticMember(dolocApi, "gameManager");
            object? gameInitConfig = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
            return gameInitConfig == null ? string.Empty : ReadStringMember(gameInitConfig, "initMarkPoint");
        }

        private static bool IsWhitelistedMarkPoint(string markPointId, string roomId)
        {
            string text = ((markPointId ?? string.Empty) + " " + (roomId ?? string.Empty)).ToLowerInvariant();
            string[] keywords =
            {
                "farm",
                "station",
                "bus",
                "city",
                "town",
                "hall",
                "municip",
                "council",
                "government",
                "research",
                "laboratory",
                "lab",
                "bar",
                "pub",
                "tavern"
            };
            return keywords.Any(text.Contains);
        }

        private static string BuildStationDisplayName(string stationId, string title, string markPointId, string roomId)
        {
            string nativeTitle = FirstText(title, stationId);
            string detail = BuildMarkPointDisplayName(markPointId, roomId);
            if (string.IsNullOrWhiteSpace(detail) || nativeTitle.IndexOf(detail, StringComparison.OrdinalIgnoreCase) >= 0)
                return nativeTitle;
            if (detail.Equals("车站", StringComparison.OrdinalIgnoreCase))
                return nativeTitle;
            return nativeTitle + " / " + detail;
        }

        private static string BuildMarkPointDisplayName(string markPointId, string roomId)
        {
            string text = ((markPointId ?? string.Empty) + " " + (roomId ?? string.Empty)).ToLowerInvariant();
            string originalMarkPointId = (markPointId ?? string.Empty).Trim();
            string originalRoomId = (roomId ?? string.Empty).Trim();
            string cjkMarkPoint = ContainsCjk(originalMarkPointId) ? originalMarkPointId : string.Empty;
            if (!string.IsNullOrWhiteSpace(cjkMarkPoint))
            {
                if (cjkMarkPoint.Contains("农场") && (cjkMarkPoint.Contains("车站") || cjkMarkPoint.Contains("公交") || cjkMarkPoint.Contains("小径") || cjkMarkPoint.Contains("道路")))
                    return "农场小径/公交站";
                if (cjkMarkPoint.Contains("市政"))
                    return "市政厅";
                if (cjkMarkPoint.Contains("研究"))
                    return "研究所";
                if (cjkMarkPoint.Contains("酒吧"))
                    return "酒吧";
                return cjkMarkPoint;
            }

            if (text.Contains("farm") && (text.Contains("station") || text.Contains("bus") || text.Contains("path") || text.Contains("road")))
                return "农场小径/公交站";
            if (text.Contains("city") && (text.Contains("station") || text.Contains("bus") || text.Contains("path") || text.Contains("road")))
                return "城镇车站";
            if (text.Contains("farm"))
                return BuildDetailedLocationName("农场", originalMarkPointId, originalRoomId);
            if (text.Contains("hall") || text.Contains("municip") || text.Contains("council") || text.Contains("government"))
                return "市政厅";
            if (text.Contains("research") || text.Contains("laboratory") || text.Contains("lab"))
                return "研究所";
            if (text.Contains("bar") || text.Contains("pub") || text.Contains("tavern"))
                return "酒吧";
            if (text.Contains("station"))
                return "车站";
            if (text.Contains("city") || text.Contains("town"))
                return "城镇";
            return FirstText(markPointId ?? string.Empty, roomId ?? string.Empty, "Teleport");
        }

        private static string BuildDetailedLocationName(string baseName, string markPointId, string roomId)
        {
            string detail = ExtractReadableLocationDetail(markPointId);
            if (string.IsNullOrWhiteSpace(detail))
                detail = ExtractReadableLocationDetail(roomId);
            if (string.IsNullOrWhiteSpace(detail))
                return baseName;
            if (detail.Equals(baseName, StringComparison.OrdinalIgnoreCase) || detail.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                return detail;
            return baseName + "-" + detail;
        }

        private static string ExtractReadableLocationDetail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            string trimmed = value.Trim();
            int dash = Math.Max(trimmed.LastIndexOf('-'), trimmed.LastIndexOf('_'));
            if (dash >= 0 && dash < trimmed.Length - 1)
            {
                string suffix = trimmed.Substring(dash + 1).Trim();
                if (ContainsCjk(suffix))
                    return suffix;
            }
            return ContainsCjk(trimmed) ? trimmed : string.Empty;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (char c in value)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                    return true;
            }
            return false;
        }

        private static TeleportSnapshot BuildTeleportSnapshot(object? room, object? position)
        {
            return new TeleportSnapshot
            {
                RoomId = room == null ? string.Empty : FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "roomId"), room.GetType().Name),
                RoomTitle = room == null ? string.Empty : ReadStringMember(room, "Title"),
                RoomType = room == null ? string.Empty : (ReadMember(room, "Type")?.ToString() ?? room.GetType().Name),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z")
            };
        }

        private InstantSaveDebugState GetInstantSaveDebugState()
        {
            var state = new InstantSaveDebugState();
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                object? position = ReadStaticMember(dolocApi, "AgentPosition");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                int archiveIndex = archive == null ? -1 : ReadIntMember(archive, "archiveIndex", -1);
                state.SaveSlot = archiveIndex >= 0 ? archiveIndex : (int?)null;
                state.CurrentLocation = BuildTeleportSnapshot(room, position);
                state.CanSave = dolocApi != null && archive != null && saveGame != null && state.SaveSlot != null;
                state.FailureReason = state.CanSave ? string.Empty : dolocApi == null ? "missing-dolocapi" : archive == null ? "missing-archive" : saveGame == null ? "missing-savegame" : "missing-archive-index";
                state.Message = state.CanSave
                    ? "Ready to save slot/index=" + archiveIndex + " at " + FormatTeleportSnapshot(state.CurrentLocation) + "."
                    : "Instant save is not ready: " + state.FailureReason + ".";
                return state;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Instant save debug state failed.", ex.ToString());
                state.CanSave = false;
                state.FailureReason = ex.GetType().Name;
                state.Message = ex.Message;
                return state;
            }
        }

        private static string FormatTeleportSnapshot(TeleportSnapshot snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return "room=" + FirstText(snapshot.RoomTitle, snapshot.RoomId, "unknown") +
                ", id=" + FirstText(snapshot.RoomId, "unknown") +
                ", pos=" + snapshot.X.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                snapshot.Y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                snapshot.Z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Csv(string value)
        {
            value ??= string.Empty;
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            value = value.Replace("\"", "\"\"");
            return quote ? "\"" + value + "\"" : value;
        }

        private TimeDebugState GetTimeDebugState()
        {
            try
            {
                WeatherDebugState weather = GetState();
                int minute = 0;
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? dateNow = archive == null ? null : ReadMember(archive, "DateNow");
                if (dateNow != null)
                    minute = ReadIntMember(dateNow, "Minute", 0);

                int periodStart = GetDebugWeatherPeriodStart(weather.Hour);
                int periodEnd = GetDebugWeatherPeriodEnd(weather.Hour);
                return new TimeDebugState
                {
                    Year = weather.Year,
                    Month = weather.Month,
                    Day = weather.Day,
                    Hour = weather.Hour,
                    Minute = minute,
                    CurrentWeatherId = weather.CurrentWeatherId,
                    CurrentWeatherName = weather.CurrentWeatherName,
                    SeasonName = weather.SeasonName,
                    Period = periodStart.ToString("00") + ":00-" + periodEnd.ToString("00") + ":00"
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Time debug state failed.", ex.ToString());
                return new TimeDebugState();
            }
        }

        private static int GameMinutesToSeconds(object globalParameter, int gameMinutes)
        {
            MethodInfo? convert = globalParameter.GetType().GetMethod("GameMinutes2Secs", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (convert != null)
            {
                object? value = convert.Invoke(globalParameter, new object[] { (float)gameMinutes });
                if (value != null)
                    return Convert.ToInt32(value);
            }

            int tuLength = Math.Max(1, ReadIntMember(globalParameter, "TULength", 1));
            int tu2Min = Math.Max(1, ReadIntMember(globalParameter, "TU2Min", 1));
            return Math.Max(1, (int)Math.Round(gameMinutes * (double)tuLength / tu2Min));
        }

        private static int GetNextDebugWeatherPeriodTarget(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 6;
            if (currentHour < 18)
                return 18;
            return 24;
        }

        private static int GetDebugWeatherPeriodStart(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 0;
            if (currentHour < 18)
                return 6;
            return 18;
        }

        private static int GetDebugWeatherPeriodEnd(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 6;
            if (currentHour < 18)
                return 18;
            return 24;
        }

        private static void InvokeWakeUp(Type dolocApi)
        {
            MethodInfo? wake = dolocApi.GetMethod("OnWakeUp", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
            wake?.Invoke(null, new object?[] { false, true, false });
        }

        private MovementDebugState GetMovementDebugState(string source)
        {
            object? motionAbility = ResolveMotionAbility();
            double moveSpeed = motionAbility == null ? 0 : ReadDoubleMember(motionAbility, "MoveSpeed", 0);
            return new MovementDebugState
            {
                Multiplier = movementSpeedMultiplier,
                MoveSpeed = moveSpeed,
                IsDefault = Math.Abs(movementSpeedMultiplier - 1d) < 0.001,
                Source = source ?? string.Empty
            };
        }

        private static object? ResolveMotionAbility()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? motion = agent == null ? null : ReadMember(agent, "MotionAbility");
            if (motion != null)
                return motion;

            object? abilitySystem = ReadStaticMember(dolocApi, "AbilitySystem");
            return abilitySystem == null ? null : ReadMember(abilitySystem, "motionAbility");
        }

        private static bool TryApplyMovementScale(object motionAbility, double multiplier, out string failureReason, out string message)
        {
            failureReason = string.Empty;
            message = string.Empty;
            if (motionAbility == null)
            {
                failureReason = "missing-motion-ability";
                message = "Player MotionAbility was not found.";
                return false;
            }

            MethodInfo? setMoveScaler = motionAbility.GetType().GetMethod(
                "SetMoveScaler",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(float) },
                null);
            if (setMoveScaler == null)
            {
                failureReason = "missing-set-move-scaler";
                message = "MotionAbility.SetMoveScaler(float) was not found.";
                return false;
            }

            setMoveScaler.Invoke(motionAbility, new object[] { (float)(multiplier - 1d) });
            return true;
        }

        private static double ClampDebugSpeedMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(0.5, value));
        }

        private static string FormatTimeDebugState(TimeDebugState state)
        {
            if (state == null)
                return "unknown";
            return state.Year + "-" + state.Month + "-" + state.Day + " " + state.Hour.ToString("00") + ":" + state.Minute.ToString("00") + " weather=" + FirstText(state.CurrentWeatherName, state.CurrentWeatherId);
        }
    }
}
