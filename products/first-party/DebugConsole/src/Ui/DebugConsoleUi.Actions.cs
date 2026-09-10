using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleUi
    {
        private void SpawnMonster(SpawnCatalogOption option, int count)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            if (!option.IsAvailable)
            {
                string reason = TranslateSpawnUnavailable(
                    option.UnavailableReason,
                    isAnimal: false);
                RecordUnavailableSpawnAttempt(
                    "monster",
                    option.Id,
                    count,
                    option.UnavailableReason,
                    reason);
                SetStatusMessage(reason, false);
                return;
            }
            SpawnActionResult outcome = advancedApi.SpawnMonster(
                ownerManifest,
                option.Id,
                count);
            SetSpawnStatus(outcome, option.DisplayName, isAnimal: false);
        }

        private void SpawnAnimal(AnimalCatalogOption option, int count)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            if (!option.IsAvailable)
            {
                string reason = TranslateSpawnUnavailable(
                    option.UnavailableReason,
                    isAnimal: true);
                RecordUnavailableSpawnAttempt(
                    "animal",
                    option.CardId,
                    count,
                    option.UnavailableReason,
                    reason);
                SetStatusMessage(reason, false);
                return;
            }
            SpawnActionResult outcome = advancedApi.SpawnAnimal(
                ownerManifest,
                option.CardId,
                count);
            SetSpawnStatus(outcome, option.DisplayName, isAnimal: true);
        }

        private void SetSpawnStatus(
            SpawnActionResult outcome,
            string fallbackName,
            bool isAnimal)
        {
            SpawnDebugResult result = outcome.Result;
            string name = FirstText(
                result.DisplayName,
                fallbackName,
                result.SpawnId);
            string details = FormatSpawnFailure(
                result,
                outcome.StopReason,
                isAnimal);
            SetStatusMessage(
                result.Success
                    ? string.Format(
                        T(
                            "debug.spawn.complete",
                            "Requested {0}, succeeded {1}: {2}. Actual new entities: {3}."),
                        result.RequestedCount,
                        result.SpawnedCount,
                        name,
                        outcome.AddedEntityCount)
                    : string.Format(
                        T(
                            "debug.spawn.partial",
                            "Requested {0}, succeeded {1}: {2}. Actual new entities: {3}. Stopped: {4}"),
                        result.RequestedCount,
                        result.SpawnedCount,
                        name,
                        outcome.AddedEntityCount,
                        details),
                rebuild: false);
        }

        private string FormatSpawnFailure(
            SpawnDebugResult result,
            string stopReason,
            bool isAnimal)
        {
            string code = (result.FailureReason ?? string.Empty).Trim();
            if (code.Equals(
                "native-host-unavailable",
                StringComparison.OrdinalIgnoreCase))
            {
                return isAnimal
                    ? T(
                        "debug.animal.hostMissing",
                        "The animal host is unavailable in the current room.")
                    : T(
                        "debug.monster.hostMissing",
                        "The monster host is unavailable in the current room.");
            }
            if (code.Equals(
                    "animal-capacity-insufficient",
                    StringComparison.OrdinalIgnoreCase) ||
                code.Equals(
                    "animal-position-capacity-insufficient",
                    StringComparison.OrdinalIgnoreCase))
            {
                return T(
                    "debug.animal.noSpace",
                    "There is not enough valid space near the player for the entire batch.");
            }

            string message = FirstText(stopReason, code, result.Message);
            return code.Length == 0 ||
                message.Equals(code, StringComparison.OrdinalIgnoreCase)
                    ? message
                    : message + " [" + code + "]";
        }

        private string AnimalStateName(AnimalSpawnState state)
        {
            switch (state)
            {
                case AnimalSpawnState.Child:
                    return T("debug.animal.state.child", "Young");
                case AnimalSpawnState.Adult:
                    return T("debug.animal.state.adult", "Adult");
                default:
                    return T("debug.animal.state.ready", "Adult (special produce ready)");
            }
        }

        private string TranslateSpawnUnavailable(
            string reason,
            bool isAnimal)
        {
            if (reason.Equals("native-host-unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return isAnimal
                    ? T(
                        "debug.animal.hostMissing",
                        "The animal host is unavailable in the current room.")
                    : T(
                        "debug.monster.hostMissing",
                        "The monster host is unavailable in the current room.");
            }
            return FirstText(reason, T("debug.catalog.empty", "No matching entries."));
        }

        private void RecordUnavailableSpawnAttempt(
            string kind,
            string id,
            int count,
            string code,
            string details)
        {
            string diagnostic =
                "kind=" + kind +
                " id=" + id +
                " requested=" + count.ToString(CultureInfo.InvariantCulture) +
                " reason=" + FirstText(code, "unavailable") +
                "; " + details;
            runtime.RuntimeMonitor.Log(
                "Debug console spawn request was rejected before native creation: " +
                diagnostic,
                LogLevel.Warn);
            runtime.SetHookStatus(
                "DebugConsole.spawn-" + kind,
                "failed",
                "YConsole catalog availability gate",
                diagnostic);
        }

        private string FormatSourceButtonLabel(InventoryDebugSourceGroup source)
        {
            string name = source.Id.Equals(SourceFilterBase, StringComparison.OrdinalIgnoreCase)
                ? T("debug.items.sourceBase", "Base")
                : source.Id.Equals(SourceFilterMods, StringComparison.OrdinalIgnoreCase)
                    ? T("debug.items.sourceMods", "Mods")
                    : FirstText(source.DisplayName, source.Id);
            return Truncate(name, 13) + " " + source.Count;
        }

        private void SetStatusMessage(string message, bool rebuild)
        {
            statusMessage = message ?? string.Empty;
            if (statusTextObject != null && !IsDestroyed(statusTextObject))
                SetProperty(statusTextObject, "text", FirstText(statusMessage, T("debug.status.ready", "Ready")));
            if (rebuild)
                dirtyRegions |= UiDirtyRegion.Status;
        }

        private void ShowItemTooltip(InventoryDebugItem item, float x, float y)
        {
            if (panelRoot == null || imageType == null)
                return;
            if (hoverTooltipRoot == null || IsDestroyed(hoverTooltipRoot))
            {
                hoverTooltipRoot = CreateUiObject(
                    "DTMAPI.DebugConsole.ItemTooltip",
                    panelRoot);
                object image = AddComponent(hoverTooltipRoot, imageType);
                SetProperty(
                    image,
                    "color",
                    Color(0.020f, 0.024f, 0.030f, 0.98f));
                SetProperty(image, "raycastTarget", false);
                hoverTooltipTextObject = AddText(
                    hoverTooltipRoot,
                    "DTMAPI.DebugConsole.ItemTooltip.Text",
                    string.Empty,
                    12,
                    Color(0.94f, 0.98f, 0.98f, 1f),
                    TextAnchorUpperLeft,
                    10,
                    -8,
                    -20,
                    -16,
                    stretch: true);
            }
            if (hoverTooltipTextObject != null)
            {
                SetProperty(
                    hoverTooltipTextObject,
                    "text",
                    FormatItemTooltip(item));
            }
            SetRect(hoverTooltipRoot, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(346, 150));
            SetActive(hoverTooltipRoot, true);
            runtime.SetHookStatus("UI.DebugConsoleItemTooltip", "visible", "Unity UI pointer hover", "item=" + item.Id + ", source=" + FirstText(item.SourceId, item.SourceKind) + ", searchText=" + FormatLifecycleValue(searchText) + ".");
        }

        private void HideItemTooltip()
        {
            if (hoverTooltipTextObject != null &&
                !IsDestroyed(hoverTooltipTextObject))
            {
                SetProperty(hoverTooltipTextObject, "text", string.Empty);
            }
            SetActive(hoverTooltipRoot, false);
        }

        private void ReleaseItemTooltip()
        {
            Destroy(hoverTooltipRoot);
            hoverTooltipRoot = null;
            hoverTooltipTextObject = null;
        }

        private void AddSectionLabel(object parent, string name, string label, float x, float y, float w, float h)
        {
            AddText(parent, name, label, 15, Color(0.99f, 0.95f, 0.80f, 1f), TextAnchorMiddleLeft, x, y, w, h);
        }

        private string FormatItemHover(InventoryDebugItem item)
        {
            string name = FirstText(item.DisplayName, item.ChineseName, item.EnglishName, item.Id);
            string source = item.IsModItem
                ? "  " + string.Format(T("debug.items.source", "source: {0}"), FirstText(item.SourceModTitle, item.SourceId, item.SourceKind))
                : string.Empty;
            string workshop = item.WorkshopId.HasValue ? "  Workshop " + item.WorkshopId.Value : string.Empty;
            string unavailable = item.CanGive ? string.Empty : "  " + TranslateItemCannotGive(item.CannotGiveReason);
            return name + source + workshop + unavailable;
        }

        private string FormatItemTooltip(InventoryDebugItem item)
        {
            string name = FirstText(item.DisplayName, item.ChineseName, item.EnglishName, item.Id);
            string categoryText = FormatCategoryLabel(FirstText(item.SubCategory, item.Category, T("common.none", "(none)")));
            string sourceText = item.IsModItem
                ? FirstText(item.SourceModTitle, item.SourceId, item.SourceKind)
                : T("debug.items.sourceBase", "Base");
            string tags = item.Tags == null ? string.Empty : string.Join(", ", item.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Take(5).ToArray());
            string status = item.CanGive
                ? T("debug.items.canGive", "can give")
                : TranslateItemCannotGive(item.CannotGiveReason);
            string workshop = item.WorkshopId.HasValue ? " / Workshop " + item.WorkshopId.Value : string.Empty;
            return name + Environment.NewLine +
                string.Format(T("debug.items.idLine", "ID: {0}"), item.Id) + Environment.NewLine +
                string.Format(T("debug.items.categoryLine", "Category: {0}"), categoryText) + Environment.NewLine +
                string.Format(T("debug.items.sourceLine", "Source: {0}"), sourceText + workshop) + Environment.NewLine +
                string.Format(T("debug.items.statusLine", "Status: {0}"), status) +
                (string.IsNullOrWhiteSpace(tags) ? string.Empty : Environment.NewLine + string.Format(T("debug.items.tagsLine", "Tags: {0}"), tags));
        }

        private string TranslateItemCannotGive(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return T("debug.items.unavailable", "unavailable");
            if (reason.Equals("source-disabled", StringComparison.OrdinalIgnoreCase))
                return T("debug.items.sourceDisabled", "source disabled");
            if (reason.Equals("not-runtime-loaded", StringComparison.OrdinalIgnoreCase))
                return T("debug.items.notLoaded", "not loaded in runtime table");
            if (reason.Equals("not-spawnable", StringComparison.OrdinalIgnoreCase))
                return T("debug.items.unspawnable", "not spawnable");
            return reason;
        }

        private void SkipTime()
        {
            if (timeApi == null || ownerManifest == null)
                return;
            TimeSkipResult result = timeApi.SkipToNextWeatherPeriod(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.time.skipped", "Advanced to {0:00}:00"), result.TargetHour)
                : string.Format(T("debug.time.failed", "Time failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.World | UiDirtyRegion.Status;
        }

        private void SaveHere()
        {
            if (instantSaveApi == null || ownerManifest == null)
                return;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!saveConfirmationArmed ||
                now > saveConfirmationExpiresAtUtc)
            {
                saveConfirmationArmed = true;
                saveConfirmationExpiresAtUtc =
                    now.Add(SaveConfirmationWindow);
                statusMessage = T(
                    "debug.save.confirmPrompt",
                    "Click Confirm save within 8 seconds to write the current native save slot.");
                dirtyRegions |= UiDirtyRegion.World | UiDirtyRegion.Status;
                runtime.RuntimeMonitor.Log(
                    "DebugConsole native save confirmation armed; no SaveGame call was made.");
                return;
            }
            ResetSaveConfirmation();
            InstantSaveDebugResult result = instantSaveApi.Save(ownerManifest, reloadAfterSave: false);
            statusMessage = result.Success
                ? string.Format(T("debug.save.saved", "Saved slot {0}"), result.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "?")
                : string.Format(T("debug.save.failed", "Save failed: {0}"), FirstText(result.FailureReason, result.Message));
            runtime.RuntimeMonitor.Log(
                "DebugConsole confirmed native save completed success=" +
                result.Success +
                " slot=" +
                (result.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown") +
                " reason=" +
                FirstText(result.FailureReason, "none") + ".");
            dirtyRegions |= UiDirtyRegion.World | UiDirtyRegion.Status;
        }

        private void ResetSaveConfirmation()
        {
            saveConfirmationArmed = false;
            saveConfirmationExpiresAtUtc = DateTimeOffset.MinValue;
        }

        private void SetMovementSpeed(double multiplier)
        {
            if (movementApi == null || ownerManifest == null)
                return;
            MovementSpeedResult result = multiplier <= 1
                ? movementApi.ResetSpeed(ownerManifest, "debug-console")
                : movementApi.SetSpeedMultiplier(ownerManifest, multiplier);
            statusMessage = result.Success
                ? string.Format(T("debug.speed.changed", "Speed {0:0.#}x"), result.AppliedMultiplier)
                : string.Format(T("debug.speed.failed", "Speed failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.World | UiDirtyRegion.Status;
        }

        private void AdvanceAdvancedTime(AdvancedTimeAdvanceKind kind, int amount)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            TimeSkipResult result = advancedApi.AdvanceTime(ownerManifest, kind, amount);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.timeChanged", "Advanced time: {0}"), result.AdvancedGameMinutes)
                : string.Format(T("debug.advanced.timeFailed", "Advanced time failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.World | UiDirtyRegion.Advanced | UiDirtyRegion.Status;
        }

        private void SetDebugTimeScale(double multiplier)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            TimeScaleDebugResult result = multiplier <= 1
                ? advancedApi.ResetTimeScale(ownerManifest, "debug-console")
                : advancedApi.SetTimeScale(ownerManifest, multiplier);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.scaleChanged", "Time scale {0:0.#}x"), result.AfterMultiplier)
                : string.Format(T("debug.advanced.scaleFailed", "Time scale failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.Advanced | UiDirtyRegion.Status;
        }

        private void AddDebugMoney(int amount)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            DebugValueResult result = advancedApi.AddMoney(ownerManifest, amount);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.moneyChanged", "Money {0} -> {1}"), result.BeforeValue, result.AfterValue)
                : string.Format(T("debug.advanced.moneyFailed", "Money failed: {0}"), FirstText(result.FailureReason, result.Message));
            SetStatusMessage(statusMessage, false);
        }

        private void AddTechnologyPoints()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            string[] pointTypeIds =
            {
                "NATURE",
                "OPERATE",
                "SCIENCE",
                "ANIMAL"
            };
            DebugValueResult[] results = pointTypeIds
                .Select(pointTypeId =>
                    advancedApi.AddTechPoint(
                        ownerManifest,
                        pointTypeId,
                        100))
                .ToArray();
            string[] failures = results
                .Where(result => !result.Success)
                .Select(result =>
                    result.ValueId + "=" +
                    FirstText(result.FailureReason, result.Message, "failed"))
                .ToArray();
            statusMessage = failures.Length == 0
                ? T(
                    "debug.advanced.techAllChanged",
                    "Tech points +100: NATURE, OPERATE, SCIENCE, ANIMAL.")
                : string.Format(
                    T(
                        "debug.advanced.techAllFailed",
                        "Tech point update failed: {0}"),
                    string.Join(", ", failures));
            SetStatusMessage(statusMessage, false);
        }

        private void UnlockAllTechTrees()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            DebugCommandResult result = advancedApi.UnlockAllTechTrees(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.techTreeChanged", "Tech trees affected: {0}"), result.AffectedCount)
                : string.Format(T("debug.advanced.techTreeFailed", "Tech tree failed: {0}"), FirstText(result.FailureReason, result.Message));
            SetStatusMessage(statusMessage, false);
        }

        private void MatureAllCrops()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            CropMaturityResult result = advancedApi.MatureAllCrops(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.cropsChanged", "Crops {0}/{1}"), result.CropsMatured, result.PlantBasinsVisited)
                : string.Format(T("debug.advanced.cropsFailed", "Crops failed: {0}"), FirstText(result.FailureReason, result.Message));
            SetStatusMessage(statusMessage, false);
        }

        private void ToggleCreativeMode()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            bool next = !advancedApi.GetCreativeModeState().Enabled;
            CreativeModeResult result = advancedApi.SetCreativeMode(ownerManifest, next);
            statusMessage = result.Success
                ? result.Message
                : string.Format(T("debug.advanced.creativeFailed", "Creative failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.Advanced | UiDirtyRegion.Status;
        }

        private bool TryGiveRightClickItem(InventoryDebugItem item, string source)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if ((now - lastRightClickGiveAt).TotalMilliseconds < 200)
                return true;
            lastRightClickGiveAt = now;
            if (!item.CanGive)
            {
                SetStatusMessage(FormatItemHover(item), rebuild: false);
                return true;
            }

            runtime.RuntimeMonitor.Log("Debug console right-click give source=" + source + " item=" + item.Id + ".");
            GiveItem(item, 10, rightClick: true);
            return true;
        }

        private DebugConsoleLayout ReadLayout()
        {
            try
            {
                double width = Convert.ToDouble(
                    screenWidthProperty?.GetValue(null, null) ?? 1920,
                    CultureInfo.InvariantCulture);
                double height = Convert.ToDouble(
                    screenHeightProperty?.GetValue(null, null) ?? 1080,
                    CultureInfo.InvariantCulture);
                object? safeArea = screenSafeAreaProperty?.GetValue(null, null);
                double safeX = ReadNumber(safeArea, safeAreaXProperty, 0d);
                double safeY = ReadNumber(safeArea, safeAreaYProperty, 0d);
                double safeWidth = ReadNumber(safeArea, safeAreaWidthProperty, width);
                double safeHeight = ReadNumber(safeArea, safeAreaHeightProperty, height);
                return GetOrCreateLayout(
                    width,
                    height,
                    safeX,
                    safeY,
                    safeWidth,
                    safeHeight);
            }
            catch (Exception error)
            {
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-layout-read-failed",
                    "Debug console used the 1920x1080 safe-area fallback: " +
                    error.GetType().Name + ": " + error.Message,
                    LogLevel.Warn);
                return GetOrCreateLayout(1920, 1080, 0, 0, 1920, 1080);
            }
        }

        private DebugConsoleLayout GetOrCreateLayout(
            double screenWidth,
            double screenHeight,
            double safeX,
            double safeY,
            double safeWidth,
            double safeHeight)
        {
            if (layout != null &&
                layoutInputObserved &&
                layoutInputScreenWidth.Equals(screenWidth) &&
                layoutInputScreenHeight.Equals(screenHeight) &&
                layoutInputSafeX.Equals(safeX) &&
                layoutInputSafeY.Equals(safeY) &&
                layoutInputSafeWidth.Equals(safeWidth) &&
                layoutInputSafeHeight.Equals(safeHeight))
            {
                return layout;
            }

            layoutInputObserved = true;
            layoutInputScreenWidth = screenWidth;
            layoutInputScreenHeight = screenHeight;
            layoutInputSafeX = safeX;
            layoutInputSafeY = safeY;
            layoutInputSafeWidth = safeWidth;
            layoutInputSafeHeight = safeHeight;
            return DebugConsoleLayout.Create(
                screenWidth,
                screenHeight,
                safeX,
                safeY,
                safeWidth,
                safeHeight);
        }

        private static double ReadNumber(
            object? target,
            PropertyInfo? property,
            double fallback)
        {
            if (target == null)
                return fallback;
            try
            {
                object? value = property?.GetValue(target, null);
                return value == null
                    ? fallback
                    : Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private void RecordRightClickBindingStatus(bool bound)
        {
            if (bound)
            {
                if (rightClickGiveBindingVerified)
                    return;
                rightClickGiveBindingVerified = true;
                runtime.SetHookStatus("UI.DebugConsoleRightClickGive", "verified", "Unity EventTrigger.PointerDown", "Right-click give item binding installed on debug console item cells.");
                return;
            }

            if (rightClickGiveBindingFailureLogged)
                return;
            rightClickGiveBindingFailureLogged = true;
            runtime.SetHookStatus("UI.DebugConsoleRightClickGive", "failed", "Unity EventTrigger.PointerDown", "Right-click give binding unavailable; left-click give path remains available.");
            runtime.RuntimeMonitor.LogOnce(
                "debug-console-right-click-bind-failed",
                "Debug console item right-click give binding failed; pointer EventTrigger is unavailable.",
                LogLevel.Warn);
        }

        private void GiveItem(InventoryDebugItem item, int count, bool rightClick = false)
        {
            if (inventoryApi == null || ownerManifest == null)
                return;
            InventoryGiveResult result = inventoryApi.GiveItem(ownerManifest, item.Id, count);
            statusMessage = result.Success
                ? string.Format(T(rightClick ? "debug.items.gaveRightClick" : "debug.items.gave", rightClick ? "Right-click gave {0} x {1}" : "Gave {0} x {1}"), result.GivenCount, FirstText(result.DisplayName, result.ItemId))
                : string.Format(T("debug.items.failed", "Give failed: {0}"), FirstText(result.FailureReason, result.Message));
            runtime.SetHookStatus(
                "DebugConsole.LastGive",
                result.Success ? "verified" : "failed",
                rightClick ? "YConsole.PointerDownRightClick" : "YConsole.ButtonLeftClick",
                "item=" + BreadcrumbValue(item.Id, 160) +
                " requested=" + count.ToString(CultureInfo.InvariantCulture) +
                " given=" + result.GivenCount.ToString(CultureInfo.InvariantCulture) +
                " rightClick=" + rightClick +
                " failure=" + BreadcrumbValue(FirstText(result.FailureReason, result.Message), 220));
            WriteLastGiveBreadcrumb(
                "status=" + (result.Success ? "verified" : "failed") +
                " source=" + (rightClick ? "YConsole.PointerDownRightClick" : "YConsole.ButtonLeftClick") +
                " item=" + BreadcrumbValue(item.Id, 160) +
                " requested=" + count.ToString(CultureInfo.InvariantCulture) +
                " given=" + result.GivenCount.ToString(CultureInfo.InvariantCulture) +
                " rightClick=" + rightClick +
                " failure=" + BreadcrumbValue(FirstText(result.FailureReason, result.Message), 220));
            SetStatusMessage(statusMessage, false);
        }

        private void WriteLastGiveBreadcrumb(string details)
        {
            try
            {
                Directory.CreateDirectory(runtime.DtmApiPath);
                string path = Path.Combine(runtime.DtmApiPath, "debug-console-last-give.txt");
                string text = DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + " " + BreadcrumbValue(details, 1200) + Environment.NewLine;
                File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-last-give-write-failed",
                    "Failed to persist debug console last-give breadcrumb: " + ex.GetType().Name + ": " + ex.Message,
                    LogLevel.Warn);
            }
        }

        private void SetWeather(WeatherDebugOption weather)
        {
            if (weatherApi == null || ownerManifest == null)
                return;
            WeatherSetResult result = weatherApi.SetWeather(ownerManifest, weather.Id, patchCurrentPeriod: true);
            statusMessage = result.Success
                ? string.Format(T("debug.weather.changed.simple", "Weather: {0}"), LocalizeWeatherName(weather.Id, FirstText(result.DisplayName, weather.DisplayName, weather.Id)))
                : string.Format(T("debug.weather.failed", "Weather failed: {0}"), FormatActionFailure(result.Message, result.FailureReason));
            dirtyRegions |= UiDirtyRegion.Weather | UiDirtyRegion.World | UiDirtyRegion.Status;
        }

        private void Teleport(TeleportDestination destination)
        {
            if (teleportApi == null || ownerManifest == null)
                return;
            TeleportResult result = teleportApi.Teleport(ownerManifest, destination.Id);
            statusMessage = result.Success
                ? string.Format(T("debug.teleport.requested", "Teleport requested: {0}"), LocalizeTeleportName(destination))
                : string.Format(T("debug.teleport.failed", "Teleport failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirtyRegions |= UiDirtyRegion.Teleport | UiDirtyRegion.World | UiDirtyRegion.Status;
        }

        private string LocalizeWeatherName(WeatherDebugOption weather)
        {
            return LocalizeWeatherName(weather.Id, FirstText(weather.DisplayName, weather.Id, T("debug.weather.unknown", "Weather")));
        }

        private string LocalizeWeatherName(string weatherId, string displayName)
        {
            switch ((weatherId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "1":
                case "SUNNY":
                    return T("debug.weather.clear", "Clear");
                case "2":
                case "CLOUDY":
                    return T("debug.weather.cloudy", "Cloudy");
                case "3":
                case "RAIN":
                    return T("debug.weather.rain", "Rain");
                case "4":
                case "THUNDERSTORM":
                    return T("debug.weather.thunderstorm", "Thunderstorm");
                case "5":
                case "WINDY":
                    return T("debug.weather.wind", "Strong wind");
                case "6":
                case "ACID_RAIN":
                    return T("debug.weather.acidRain", "Acid rain");
                case "7":
                case "SCORCH_SUN":
                    return T("debug.weather.scorching", "Scorching sun");
                default:
                    return FirstText(displayName, T("debug.weather.unknown", "Weather"));
            }
        }

        private string LocalizeTeleportName(TeleportDestination destination)
        {
            switch (destination.Id)
            {
                case "station.town": return T("debug.teleport.station.town", "Town bus station");
                case "station.outpost": return T("debug.teleport.station.outpost", "Outpost");
                case "station.mountain-path": return T("debug.teleport.station.mountainPath", "Mountain path");
                case "station.wetland": return T("debug.teleport.station.wetland", "Wetland");
                case "station.dock": return T("debug.teleport.station.dock", "Dock");
                case "station.farm-path": return T("debug.teleport.station.farmPath", "Farm path");
                case "station.pollution-zone": return T("debug.teleport.station.pollutedArea", "Polluted area");
                case "station.peatland": return T("debug.teleport.station.peatland", "Peatland");
                case "station.water-lily": return T("debug.teleport.station.lilyland", "Lily fields");
                case "station.old-city-garrison": return T("debug.teleport.station.oldCityPost", "Old City garrison");
                case "station.residential": return T("debug.teleport.station.residential", "Residential district");
                case "station.commercial": return T("debug.teleport.station.commercial", "Commercial district");
                case "station.vacant": return T("debug.teleport.station.freeZone", "Free zone");
                case "landmark.farm": return T("debug.teleport.landmark.farm", "Farm");
                case "landmark.home": return T("debug.teleport.landmark.home", "Home");
                case "landmark.town-hall": return T("debug.teleport.landmark.townHall", "Town hall");
                case "landmark.botanical-lab": return T("debug.teleport.landmark.botanicalInstitute", "Botanical research institute");
                case "landmark.tavern": return T("debug.teleport.landmark.tavern", "Tavern");
                case "landmark.deer-god-pond": return T("debug.teleport.landmark.deerPond", "Deer God's Pond");
                case "landmark.mountain-cable-car": return T("debug.teleport.landmark.backHillCableCar", "Back Mountain Cable Car");
                case "landmark.witch-hut": return T("debug.teleport.landmark.witchHut", "Witch's hut");
                case "landmark.valley-summit-stele": return T("debug.teleport.landmark.riverValleySummitStele", "River Valley Summit Stele");
                default:
                    return FirstText(destination.DisplayName, destination.SuggestedDisplayName, T("debug.teleport.place", "Place"));
            }
        }

        private static string FormatActionFailure(
            string message,
            string code)
        {
            message = (message ?? string.Empty).Trim();
            code = (code ?? string.Empty).Trim();
            if (message.Length == 0)
                return code;
            return code.Length == 0 ||
                message.Equals(code, StringComparison.OrdinalIgnoreCase)
                    ? message
                    : message + " [" + code + "]";
        }

        private string LocalizeTeleportName(string value)
        {
            return FirstText(value, T("debug.teleport.place", "Place"));
        }

        private string TranslateTeleportUnavailable(string reason)
        {
            if (reason.Equals("mark-point-missing", StringComparison.OrdinalIgnoreCase))
                return T("debug.teleport.unavailable.missingMarkPoint", "Unavailable: MarkPoint not found in the current game data.");
            if (reason.Equals("current-room-unavailable", StringComparison.OrdinalIgnoreCase))
                return T("debug.teleport.unavailable.room", "Unavailable from the current room.");
            if (reason.Equals("native-host-unavailable", StringComparison.OrdinalIgnoreCase))
                return T("debug.teleport.unavailable.nativeOwner", "Unavailable: the game's built-in teleport system is not ready.");
            return reason;
        }

        private string FormatCategoryLabel(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "tool": return T("debug.category.tool", "Tools");
                case "material": return T("debug.category.material", "Materials");
                case "farm": return T("debug.category.farm", "Farming");
                case "husbandry": return T("debug.category.husbandry", "Husbandry");
                case "product": return T("debug.category.product", "Products");
                case "food": return T("debug.category.food", "Food");
                case "kit": return T("debug.category.kit", "Kits");
                case "equipment": return T("debug.category.equipment", "Equipment");
                case "construction": return T("debug.category.construction", "Construction");
                case "special": return T("debug.category.special", "Special");
                case "monster": return T("debug.category.monster", "Monsters");
                case "animal": return T("debug.category.animal", "Animals");
                default: return FirstText(value ?? string.Empty, T("common.none", "(none)"));
            }
        }

    }
}
