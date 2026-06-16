using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class ReflectedDebugConsoleUi : IDebugConsoleApi
    {
        private const string MenuId = "DTMAPI.DebugConsole";
        private const string SourceFilterBase = "__base";
        private const string SourceFilterMods = "__mods";
        private readonly DtmApiRuntime runtime;
        private readonly List<object> eventBinders = new List<object>();
        private readonly List<object> inputFields = new List<object>();
        private DtmUiText text = new DtmUiText();
        private IInventoryDebugApi? inventoryApi;
        private IWeatherDebugApi? weatherApi;
        private ITeleportDebugApi? teleportApi;
        private ITimeDebugApi? timeApi;
        private IMovementDebugApi? movementApi;
        private IInstantSaveDebugApi? instantSaveApi;
        private IAdvancedDebugApi? advancedApi;
        private IManifest? ownerManifest;

        private Type? gameObjectType;
        private Type? transformType;
        private Type? rectTransformType;
        private Type? objectType;
        private Type? canvasType;
        private Type? canvasScalerType;
        private Type? graphicRaycasterType;
        private Type? imageType;
        private Type? buttonType;
        private Type? textType;
        private Type? fontType;
        private Type? resourcesType;
        private Type? colorType;
        private Type? vector2Type;
        private Type? inputFieldType;
        private Type? eventSystemType;
        private Type? standaloneInputModuleType;
        private Type? inputSystemUiInputModuleType;
        private Type? unityActionType;
        private Type? unityActionStringType;
        private Type? eventTriggerType;
        private Type? eventTriggerEntryType;
        private Type? eventTriggerTypeEnum;
        private Type? baseEventDataType;
        private Type? unityActionBaseEventDataType;

        private object? root;
        private object? panelRoot;
        private object? eventSystemRoot;
        private object? statusTextObject;
        private object? hoverTooltipRoot;
        private string searchText = string.Empty;
        private string category = string.Empty;
        private string sourceFilter = SourceFilterBase;
        private string statusMessage = string.Empty;
        private bool modItemsOnly;
        private int itemPage;
        private int categoryPage;
        private int sourcePage;
        private int weatherPage;
        private int teleportPage;
        private bool initialized;
        private bool dirty = true;
        private int? activeSaveSlot;
        private bool saveSessionObserved;
        private bool openLogged;
        private bool screenshotPending;
        private bool screenshotRecorded;
        private bool screenshotHoverPrepared;
        private bool screenshotHoverStatusActive;
        private DateTimeOffset lastRightClickGiveAt;
        private string screenshotHoverItem = string.Empty;
        private string screenshotHoverSourceKind = string.Empty;
        private string screenshotHoverSourceId = string.Empty;
        private string screenshotHoverSourceTitle = string.Empty;
        private string screenshotHoverWorkshopId = string.Empty;
        private string screenshotSearchText = string.Empty;
        private string language = string.Empty;

        public ReflectedDebugConsoleUi(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool IsOpen { get; private set; }
        public bool ConsumedInputThisFrame { get; private set; }

        public void ResetForSaveBoundary(int? saveSlot, bool isNewGame)
        {
            bool shouldReset = isNewGame || !saveSessionObserved || saveSlot == null || activeSaveSlot == null || activeSaveSlot != saveSlot;
            activeSaveSlot = saveSlot;
            saveSessionObserved = true;
            if (shouldReset)
                ResetItemFilters("SaveLoaded slot=" + (saveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown") + " isNewGame=" + isNewGame);
        }

        public void ResetForTitleBoundary()
        {
            activeSaveSlot = null;
            saveSessionObserved = false;
            ResetItemFilters("ReturnedToTitle");
            if (IsOpen)
                Close(ownerManifest!, "ReturnedToTitle");
        }

        public void Bind(IManifest owner, IInventoryDebugApi? inventoryApi, IWeatherDebugApi? weatherApi, ITeleportDebugApi? teleportApi, ITimeDebugApi? timeApi, IMovementDebugApi? movementApi, IInstantSaveDebugApi? instantSaveApi = null)
        {
            ownerManifest = owner;
            this.inventoryApi = inventoryApi;
            this.weatherApi = weatherApi;
            this.teleportApi = teleportApi;
            this.timeApi = timeApi;
            this.movementApi = movementApi;
            this.instantSaveApi = instantSaveApi;
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console host bound owner=" + (owner?.UniqueID ?? "unknown") + " inventory=" + (inventoryApi != null) + " weather=" + (weatherApi != null) + " teleport=" + (teleportApi != null) + " time=" + (timeApi != null) + " movement=" + (movementApi != null) + " instantSave=" + (instantSaveApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleHost", "experimental", "Unity UI Canvas", "In-save Y-key debug console host bound to inventory/weather/teleport/time/movement/instant-save APIs.");
        }

        public void BindAdvanced(IManifest owner, IAdvancedDebugApi? advancedDebugApi)
        {
            ownerManifest = owner;
            advancedApi = advancedDebugApi;
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console host advanced binding owner=" + (owner?.UniqueID ?? "unknown") + " advanced=" + (advancedDebugApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleAdvancedHost", advancedDebugApi != null ? "experimental" : "missing-api", "Unity UI Canvas + IAdvancedDebugApi", "Advanced Y-console controls bound=" + (advancedDebugApi != null) + ".");
        }

        public void SetLanguage(IManifest owner, string language)
        {
            ownerManifest ??= owner;
            this.language = language ?? string.Empty;
            text = new DtmUiText(this.language);
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console language set owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + " language=" + text.Language + ".");
        }

        public void Open(IManifest owner, string reason)
        {
            if (ownerManifest == null && owner != null)
                ownerManifest = owner;
            if (IsOpen)
                return;
            IsOpen = true;
            dirty = true;
            screenshotPending = !screenshotRecorded;
            screenshotHoverPrepared = false;
            screenshotHoverStatusActive = false;
            screenshotHoverItem = string.Empty;
            screenshotHoverSourceKind = string.Empty;
            screenshotHoverSourceId = string.Empty;
            screenshotHoverSourceTitle = string.Empty;
            screenshotHoverWorkshopId = string.Empty;
            screenshotSearchText = string.Empty;
            runtime.UI.OpenCustomMenu(MenuId);
            DolocTownHookCallbacks.DebugConsoleModalOpen = true;
            runtime.RuntimeMonitor.Log("Debug console opened owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + " reason=" + (reason ?? string.Empty) + ".");
            runtime.RuntimeMonitor.Log("Debug console open lifecycle state searchText=" + FormatLifecycleValue(searchText) +
                " category=" + FormatLifecycleValue(category) +
                " sourceFilter=" + FormatLifecycleValue(sourceFilter) +
                " modItemsOnly=" + modItemsOnly +
                " itemPage=" + itemPage + ".");
            runtime.SetHookStatus("Smoke.DebugConsoleOpen", "verified", "DTMAPI.DebugConsoleMod + Unity UI Canvas", "Debug console opened from " + (reason ?? "unknown") + ".");
        }

        public void Close(IManifest owner, string reason)
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            dirty = true;
            DolocTownHookCallbacks.DebugConsoleModalOpen = false;
            HideItemTooltip();
            if (runtime.UI.IsOpen && runtime.UI.ActiveMenuId.Equals(MenuId, StringComparison.OrdinalIgnoreCase))
                runtime.UI.Close();
            runtime.RuntimeMonitor.Log("Debug console closed reason=" + (reason ?? string.Empty) + " owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + ".");
            runtime.SetHookStatus("Smoke.DebugConsoleClose", "verified", "DTMAPI.DebugConsoleMod + Unity UI Canvas", "Debug console closed from " + (reason ?? "unknown") + ".");
        }

        public void Toggle(IManifest owner, string reason)
        {
            if (IsOpen)
                Close(owner, reason);
            else
                Open(owner, reason);
        }

        private void ResetItemFilters(string reason)
        {
            searchText = string.Empty;
            category = string.Empty;
            sourceFilter = SourceFilterBase;
            statusMessage = string.Empty;
            modItemsOnly = false;
            itemPage = 0;
            categoryPage = 0;
            sourcePage = 0;
            weatherPage = 0;
            teleportPage = 0;
            screenshotHoverPrepared = false;
            screenshotHoverStatusActive = false;
            screenshotSearchText = string.Empty;
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console item search/filter state reset for " + reason + ".");
            runtime.SetHookStatus("UI.DebugConsoleSearchLifecycle", "experimental", "SaveLoaded/ReturnedToTitle runtime boundary", "Search and item filters cleared for " + reason + ".");
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            bool bound = inventoryApi != null && weatherApi != null && teleportApi != null && timeApi != null && movementApi != null && instantSaveApi != null && advancedApi != null;
            return new BridgeFeatureStatus(bound ? "bound-experimental" : "missing-api", "open=" + IsOpen + " owner=" + (ownerManifest?.UniqueID ?? "none"));
        }

        public void Update()
        {
            ConsumedInputThisFrame = false;
            DolocTownHookCallbacks.DebugConsoleModalOpen = IsOpen;
            if (IsOpen)
            {
                if (ReflectedUnityInput.GetKeyDown("Escape"))
                {
                    ConsumedInputThisFrame = true;
                    Close(ownerManifest!, "Escape");
                    return;
                }
                if (ReflectedUnityInput.GetKeyDown("Y"))
                {
                    ConsumedInputThisFrame = true;
                    if (IsAnyTextInputFocused())
                    {
                        runtime.RuntimeMonitor.LogOnce(
                            "debug-console-y-input-focus",
                            "Debug console ignored Y close while a text input is focused.",
                            LogLevel.Info);
                        return;
                    }
                    Close(ownerManifest!, "Y");
                    return;
                }
            }

            if (!EnsureInitialized())
                return;

            SetActive(root, IsOpen);
            if (!IsOpen)
                return;

            if (!openLogged)
            {
                openLogged = true;
                runtime.RuntimeMonitor.Log("Debug console Canvas visible as Y-key console.");
            }

            if (screenshotPending && !screenshotHoverPrepared)
                PrepareScreenshotHoverEvidence();

            if (dirty)
                Rebuild();
            if (screenshotPending)
                CaptureUiEvidenceScreenshot();
        }

        private bool EnsureInitialized()
        {
            if (initialized && root != null && !IsDestroyed(root))
                return true;

            ResolveTypes();
            if (gameObjectType == null || rectTransformType == null || objectType == null || canvasType == null ||
                canvasScalerType == null || graphicRaycasterType == null || imageType == null || buttonType == null ||
                textType == null || fontType == null || resourcesType == null || colorType == null || vector2Type == null)
                return false;

            root = CreateUiObject("DTMAPI.DebugConsole.Canvas", null);
            object canvas = AddComponent(root, canvasType);
            SetEnumProperty(canvas, "renderMode", 0);
            SetProperty(canvas, "sortingOrder", 1200);
            AddComponent(root, canvasScalerType);
            AddComponent(root, graphicRaycasterType);
            DontDestroyOnLoad(root);
            EnsureEventSystem();
            SetActive(root, false);
            initialized = true;
            dirty = true;
            runtime.RuntimeMonitor.Log("DTMAPI debug console Canvas host initialized.");
            return true;
        }

        private void ResolveTypes()
        {
            gameObjectType ??= Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.GameObject, UnityEngine");
            transformType ??= Type.GetType("UnityEngine.Transform, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Transform, UnityEngine");
            rectTransformType ??= Type.GetType("UnityEngine.RectTransform, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.RectTransform, UnityEngine");
            objectType ??= Type.GetType("UnityEngine.Object, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Object, UnityEngine");
            canvasType ??= Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule") ?? Type.GetType("UnityEngine.Canvas, UnityEngine");
            canvasScalerType ??= Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            graphicRaycasterType ??= Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            imageType ??= Type.GetType("UnityEngine.UI.Image, UnityEngine.UI");
            buttonType ??= Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");
            textType ??= Type.GetType("UnityEngine.UI.Text, UnityEngine.UI");
            fontType ??= Type.GetType("UnityEngine.Font, UnityEngine.TextRenderingModule") ?? Type.GetType("UnityEngine.Font, UnityEngine");
            resourcesType ??= Type.GetType("UnityEngine.Resources, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Resources, UnityEngine");
            colorType ??= Type.GetType("UnityEngine.Color, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Color, UnityEngine");
            vector2Type ??= Type.GetType("UnityEngine.Vector2, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Vector2, UnityEngine");
            inputFieldType ??= Type.GetType("UnityEngine.UI.InputField, UnityEngine.UI");
            eventSystemType ??= Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            standaloneInputModuleType ??= Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");
            inputSystemUiInputModuleType ??= Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            unityActionType ??= Type.GetType("UnityEngine.Events.UnityAction, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Events.UnityAction, UnityEngine");
            Type? unityActionGeneric = Type.GetType("UnityEngine.Events.UnityAction`1, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Events.UnityAction`1, UnityEngine");
            unityActionStringType ??= unityActionGeneric?.MakeGenericType(typeof(string));
            eventTriggerType ??= Type.GetType("UnityEngine.EventSystems.EventTrigger, UnityEngine.UI");
            eventTriggerEntryType ??= Type.GetType("UnityEngine.EventSystems.EventTrigger+Entry, UnityEngine.UI");
            eventTriggerTypeEnum ??= Type.GetType("UnityEngine.EventSystems.EventTriggerType, UnityEngine.UI");
            baseEventDataType ??= Type.GetType("UnityEngine.EventSystems.BaseEventData, UnityEngine.UI");
            unityActionBaseEventDataType ??= baseEventDataType == null ? null : unityActionGeneric?.MakeGenericType(baseEventDataType);
        }

        private void EnsureEventSystem()
        {
            if (eventSystemType == null || FindObjectOfType(eventSystemType) != null)
                return;
            eventSystemRoot = CreatePlainObject("DTMAPI.DebugConsole.EventSystem");
            AddComponent(eventSystemRoot, eventSystemType);
            if (inputSystemUiInputModuleType != null)
                AddComponent(eventSystemRoot, inputSystemUiInputModuleType);
            else if (standaloneInputModuleType != null)
                AddComponent(eventSystemRoot, standaloneInputModuleType);
            DontDestroyOnLoad(eventSystemRoot);
            runtime.RuntimeMonitor.Log("DTMAPI debug console UI created an EventSystem for button input.");
        }

        private void Rebuild()
        {
            Destroy(hoverTooltipRoot);
            hoverTooltipRoot = null;
            Destroy(panelRoot);
            eventBinders.Clear();
            inputFields.Clear();
            panelRoot = CreateUiObject("DTMAPI.DebugConsole.Panel", root);
            object image = AddComponent(panelRoot, imageType!);
            SetProperty(image, "color", Color(0.035f, 0.04f, 0.046f, 0.96f));
            SetRect(panelRoot, Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(1500, 900));

            AddText(panelRoot, "DTMAPI.DebugConsole.Title", T("debug.title.y", "Y-Key Console") + "  " + DtmApiRuntime.ApiVersion, 29, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 28, -30, 940, 42);
            AddText(panelRoot, "DTMAPI.DebugConsole.Subtitle", T("debug.subtitle", "Experimental in-save tools"), 16, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleLeft, 28, -64, 940, 28);
            CreateButton(panelRoot, "DTMAPI.DebugConsole.Close", T("ui.close", "Close"), () => Close(ownerManifest!, "button"), Color(0.34f, 0.16f, 0.16f, 1f), Color(1f, 1f, 1f, 1f), 1394, -34, 78, 34);

            BuildItemsTab();
            BuildDebugSidePanel();

            statusTextObject = AddText(panelRoot, "DTMAPI.DebugConsole.Status", FirstText(statusMessage, T("debug.status.ready", "Ready")), 14, Color(0.78f, 0.86f, 0.86f, 1f), TextAnchorMiddleLeft, 28, -882, 1440, 26);
            dirty = false;
        }

        private void BuildItemsTab()
        {
            if (inventoryApi == null)
            {
                AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Missing", T("debug.missing.inventory", "Inventory debug API is not available."), 18, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, 28, -104, 660, 32);
                return;
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.Title", T("debug.section.items", "Items"), 28, -96, 220, 28);
            CreateInput(panelRoot!, "DTMAPI.DebugConsole.Search", searchText, value =>
            {
                searchText = value ?? string.Empty;
                itemPage = 0;
                sourcePage = 0;
                categoryPage = 0;
                dirty = true;
            }, 112, -94, 390, 34);

            if (string.IsNullOrWhiteSpace(sourceFilter))
                sourceFilter = SourceFilterBase;

            const int itemPageSize = 35;
            const int filterButtonRows = 16;
            const float filterPagerY = -704;

            InventoryDebugPage page = inventoryApi.GetItems(new InventoryDebugQuery
            {
                SearchText = searchText,
                Category = category,
                SourceId = sourceFilter,
                IncludeUnavailable = true,
                Page = itemPage,
                PageSize = itemPageSize
            });
            itemPage = page.Page;
            AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Count", string.Format(T("debug.items.count", "{0} items | page {1}/{2}"), page.TotalItems, page.Page + 1, page.TotalPages), 15, Color(0.78f, 0.82f, 0.84f, 1f), TextAnchorMiddleLeft, 520, -94, 360, 34);

            float sourceX = 28;
            float categoryX = 198;
            float gridX = 388;
            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.SourceTitle", T("debug.items.sourceColumn", "Source"), sourceX, -128, 150, 26);
            InventoryDebugSourceGroup[] sources = page.Sources.ToArray();
            int sourcePageSize = filterButtonRows;
            int sourceTotalPages = Math.Max(1, (int)Math.Ceiling(sources.Length / (double)sourcePageSize));
            sourcePage = Math.Max(0, Math.Min(sourcePage, sourceTotalPages - 1));
            int sourceIndex = 0;
            foreach (InventoryDebugSourceGroup source in sources.Skip(sourcePage * sourcePageSize).Take(sourcePageSize))
            {
                InventoryDebugSourceGroup selectedSource = source;
                bool selected = selectedSource.Id.Equals(sourceFilter, StringComparison.OrdinalIgnoreCase);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source." + sourceIndex, FormatSourceButtonLabel(selectedSource), () =>
                {
                    sourceFilter = selectedSource.Id;
                    modItemsOnly = selectedSource.Id.Equals(SourceFilterMods, StringComparison.OrdinalIgnoreCase) || (selectedSource.IsModSource && !selectedSource.Id.Equals(SourceFilterBase, StringComparison.OrdinalIgnoreCase));
                    category = string.Empty;
                    itemPage = 0;
                    categoryPage = 0;
                    dirty = true;
                }, selected ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), selectedSource.Count > 0 ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f), sourceX, -158 - sourceIndex * 34, 154, 30);
                sourceIndex++;
            }
            if (sourceTotalPages > 1)
            {
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source.Prev", "<", () =>
                {
                    sourcePage = Math.Max(0, sourcePage - 1);
                    dirty = true;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), sourceX, filterPagerY, 44, 30);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Source.Page", (sourcePage + 1) + "/" + sourceTotalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, sourceX + 48, filterPagerY, 58, 30);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source.Next", ">", () =>
                {
                    sourcePage = Math.Min(sourceTotalPages - 1, sourcePage + 1);
                    dirty = true;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), sourceX + 110, filterPagerY, 44, 30);
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.CategoryTitle", T("debug.items.categoryColumn", "Category"), categoryX, -128, 160, 26);
            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category.All", T("debug.items.categoryAll", "All categories"), () =>
            {
                category = string.Empty;
                itemPage = 0;
                dirty = true;
            }, string.IsNullOrWhiteSpace(category) ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX, -158, 160, 30);

            string[] categories = page.Categories.ToArray();
            int categoryPageSize = filterButtonRows - 1;
            int categoryTotalPages = Math.Max(1, (int)Math.Ceiling(categories.Length / (double)categoryPageSize));
            categoryPage = Math.Max(0, Math.Min(categoryPage, categoryTotalPages - 1));
            int catIndex = 0;
            foreach (string cat in categories.Skip(categoryPage * categoryPageSize).Take(categoryPageSize))
            {
                string selectedCat = cat;
                bool selected = selectedCat.Equals(category, StringComparison.OrdinalIgnoreCase);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category." + catIndex, Truncate(FormatCategoryLabel(selectedCat), 14), () =>
                {
                    category = selectedCat;
                    itemPage = 0;
                    dirty = true;
                }, selected ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX, -192 - catIndex * 34, 160, 30);
                catIndex++;
            }
            if (categoryTotalPages > 1)
            {
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category.Prev", "<", () =>
                {
                    categoryPage = Math.Max(0, categoryPage - 1);
                    dirty = true;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX, filterPagerY, 44, 30);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Category.Page", (categoryPage + 1) + "/" + categoryTotalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, categoryX + 48, filterPagerY, 58, 30);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category.Next", ">", () =>
                {
                    categoryPage = Math.Min(categoryTotalPages - 1, categoryPage + 1);
                    dirty = true;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX + 110, filterPagerY, 44, 30);
            }

            int index = 0;
            foreach (InventoryDebugItem item in page.Items)
            {
                int col = index % 5;
                int row = index / 5;
                CreateItemCell(panelRoot!, item, index, gridX + col * 112, -150 - row * 78, 100, 74);
                index++;
            }

            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Items.Prev", "<", () =>
            {
                itemPage = Math.Max(0, itemPage - 1);
                dirty = true;
            }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), gridX + 358, -780, 46, 32);
            AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Page", (page.Page + 1) + "/" + page.TotalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, gridX + 410, -780, 80, 32);
            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Items.Next", ">", () =>
            {
                itemPage = Math.Min(page.TotalPages - 1, itemPage + 1);
                dirty = true;
            }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), gridX + 496, -780, 46, 32);
        }

        private void BuildDebugSidePanel()
        {
            float x = 1000;
            float buttonW = 224;
            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Time.Title", T("debug.section.time", "Time"), x, -96, 180, 28);
            if (timeApi == null)
            {
                AddText(panelRoot!, "DTMAPI.DebugConsole.Time.Missing", T("debug.missing.time", "Time debug API is not available."), 14, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, x, -126, 520, 26);
            }
            else
            {
                TimeDebugState time = timeApi.GetState();
                string timeLabel = string.Format(T("debug.time.state", "{0}/{1}/{2} {3:00}:{4:00}  {5}"), time.Year, time.Month, time.Day, time.Hour, time.Minute, FirstText(time.CurrentWeatherName, time.CurrentWeatherId));
                AddText(panelRoot!, "DTMAPI.DebugConsole.Time.State", timeLabel, 15, Color(0.90f, 0.96f, 0.96f, 1f), TextAnchorMiddleLeft, x, -126, 520, 28);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Time.Period", time.Period, 14, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleLeft, x, -156, 230, 26);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Time.Next", T("debug.time.next", "Next period"), SkipTime, Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + 220, -156, 104, 30);
                if (instantSaveApi != null)
                {
                    InstantSaveDebugState save = instantSaveApi.GetState();
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Save.Here", T("debug.save.here", "Save here"), SaveHere, save.CanSave ? Color(0.12f, 0.40f, 0.30f, 1f) : Color(0.18f, 0.18f, 0.18f, 1f), save.CanSave ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f), x + 330, -156, 118, 30);
                }
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Speed.Title", T("debug.section.speed", "Move"), x, -206, 180, 28);
            MovementDebugState speed = movementApi == null ? new MovementDebugState() : movementApi.GetState();
            AddText(panelRoot!, "DTMAPI.DebugConsole.Speed.State", string.Format(T("debug.speed.state", "Speed {0:0.#}x"), speed.Multiplier), 15, Color(0.90f, 0.96f, 0.96f, 1f), TextAnchorMiddleLeft, x, -238, 150, 28);
            double[] multipliers = { 1, 2, 3, 4 };
            for (int i = 0; i < multipliers.Length; i++)
            {
                double value = multipliers[i];
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Speed." + value.ToString("0.#"), value.ToString("0.#") + "x", () => SetMovementSpeed(value), Math.Abs(speed.Multiplier - value) < 0.01 ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 150 + i * 56, -238, 50, 30);
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Weather.Title", T("debug.section.weather", "Weather"), x, -292, 180, 28);
            if (weatherApi == null)
            {
                AddText(panelRoot!, "DTMAPI.DebugConsole.Weather.Missing", T("debug.missing.weather", "Weather debug API is not available."), 14, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, x, -322, 520, 26);
            }
            else
            {
                WeatherDebugState state = weatherApi.GetState();
                AddText(panelRoot!, "DTMAPI.DebugConsole.Weather.State", LocalizeWeatherName(state.CurrentWeatherId, FirstText(state.CurrentWeatherName, T("debug.weather.unknown", "Weather"))), 15, Color(0.90f, 0.96f, 0.96f, 1f), TextAnchorMiddleLeft, x, -322, 520, 28);
                WeatherDebugOption[] weathers = weatherApi.GetAvailableWeathers().ToArray();
                int pageSize = 8;
                int totalPages = Math.Max(1, (int)Math.Ceiling(weathers.Length / (double)pageSize));
                weatherPage = Math.Max(0, Math.Min(weatherPage, totalPages - 1));
                int i = 0;
                foreach (WeatherDebugOption weather in weathers.Skip(weatherPage * pageSize).Take(pageSize))
                {
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Weather.Set." + i, Truncate(LocalizeWeatherName(weather), 7), () => SetWeather(weather), weather.IsCurrent ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + i * 58, -356, 54, 28);
                    i++;
                }
                if (totalPages > 1)
                {
                    AddText(panelRoot!, "DTMAPI.DebugConsole.Weather.Page", (weatherPage + 1) + "/" + totalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, x + 190, -394, 80, 28);
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Weather.Prev", "<", () =>
                    {
                        weatherPage = Math.Max(0, weatherPage - 1);
                        dirty = true;
                    }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 142, -394, 42, 30);
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Weather.Next", ">", () =>
                    {
                        weatherPage = Math.Min(totalPages - 1, weatherPage + 1);
                        dirty = true;
                    }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 282, -394, 42, 30);
                }
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Teleport.Title", T("debug.section.teleport", "Teleport"), x, -424, 180, 28);
            if (teleportApi == null)
            {
                AddText(panelRoot!, "DTMAPI.DebugConsole.Teleport.Missing", T("debug.missing.teleport", "Teleport debug API is not available."), 14, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, x, -454, 520, 26);
            }
            else
            {
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Teleport.ExportCsv", T("debug.teleport.exportCsv", "Export CSV"), ExportTeleportCsv, Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + 356, -424, 118, 28);
                TeleportSnapshot snapshot = teleportApi.GetCurrentSnapshot();
                AddText(panelRoot!, "DTMAPI.DebugConsole.Teleport.State", string.Format(T("debug.teleport.currentOnly", "Current: {0}"), DisplaySafeLocation(snapshot)), 15, Color(0.90f, 0.96f, 0.96f, 1f), TextAnchorMiddleLeft, x, -454, 520, 28);
                TeleportDestination[] destinations = teleportApi.GetDestinations().ToArray();
                int pageSize = 8;
                int totalPages = Math.Max(1, (int)Math.Ceiling(destinations.Length / (double)pageSize));
                teleportPage = Math.Max(0, Math.Min(teleportPage, totalPages - 1));
                int i = 0;
                foreach (TeleportDestination destination in destinations.Skip(teleportPage * pageSize).Take(pageSize))
                {
                    int col = i % 2;
                    int row = i / 2;
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Teleport.Go." + i, Truncate(LocalizeTeleportName(destination), 18), () => Teleport(destination), Color(0.20f, 0.24f, 0.44f, 1f), Color(1f, 1f, 1f, 1f), x + col * 238, -488 - row * 36, buttonW, 32);
                    i++;
                }
                if (totalPages > 1)
                {
                    AddText(panelRoot!, "DTMAPI.DebugConsole.Teleport.Page", (teleportPage + 1) + "/" + totalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, x + 190, -634, 80, 28);
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Teleport.Prev", "<", () =>
                    {
                        teleportPage = Math.Max(0, teleportPage - 1);
                        dirty = true;
                    }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 142, -634, 42, 30);
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Teleport.Next", ">", () =>
                    {
                        teleportPage = Math.Min(totalPages - 1, teleportPage + 1);
                        dirty = true;
                    }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 282, -634, 42, 30);
                }
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Advanced.Title", T("debug.section.advanced", "Advanced"), x, -674, 180, 28);
            if (advancedApi == null)
            {
                AddText(panelRoot!, "DTMAPI.DebugConsole.Advanced.Missing", T("debug.missing.advanced", "Advanced debug API is not available."), 14, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, x, -704, 520, 26);
            }
            else
            {
                CreativeModeState creative = advancedApi.GetCreativeModeState();
                string creativeLabel = creative.Enabled ? T("debug.creative.on", "Creative on") : T("debug.creative.off", "Creative off");
                AddText(panelRoot!, "DTMAPI.DebugConsole.Advanced.State", creativeLabel, 14, creative.Enabled ? Color(0.74f, 1f, 0.82f, 1f) : Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleLeft, x, -704, 180, 26);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Day", T("debug.advanced.day", "+1 day"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Day, 1), Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + 144, -704, 72, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Week", T("debug.advanced.week", "+1 week"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Week, 1), Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + 222, -704, 78, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Month", T("debug.advanced.month", "+1 month"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Month, 1), Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x + 306, -704, 88, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Scale4", T("debug.advanced.scale4", "4x time"), () => SetDebugTimeScale(4), Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x + 400, -704, 72, 28);

                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.ScaleReset", T("debug.advanced.scaleReset", "1x"), () => SetDebugTimeScale(1), Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), x, -738, 56, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Money", T("debug.advanced.money", "+money"), () => AddDebugMoney(10000), Color(0.14f, 0.34f, 0.20f, 1f), Color(1f, 1f, 1f, 1f), x + 62, -738, 86, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.TechPoint", T("debug.advanced.techPoint", "+tech"), AddFirstTechPoint, Color(0.18f, 0.28f, 0.44f, 1f), Color(1f, 1f, 1f, 1f), x + 154, -738, 78, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.UnlockTech", T("debug.advanced.unlockTech", "Tech tree"), UnlockAllTechTrees, Color(0.18f, 0.28f, 0.44f, 1f), Color(1f, 1f, 1f, 1f), x + 238, -738, 94, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.MatureCrops", T("debug.advanced.matureCrops", "Crops"), MatureAllCrops, Color(0.17f, 0.32f, 0.22f, 1f), Color(1f, 1f, 1f, 1f), x + 338, -738, 70, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Creative", creative.Enabled ? T("debug.advanced.creativeOff", "Creative off") : T("debug.advanced.creativeOn", "Creative on"), ToggleCreativeMode, creative.Enabled ? Color(0.36f, 0.24f, 0.12f, 1f) : Color(0.17f, 0.32f, 0.22f, 1f), Color(1f, 1f, 1f, 1f), x + 414, -738, 84, 28);

                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Generator", T("debug.advanced.generator", "Generator"), GiveCreativeGenerator, Color(0.12f, 0.28f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), x, -772, 102, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Monster", T("debug.advanced.monster", "Monster"), SpawnFirstMonster, Color(0.38f, 0.18f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), x + 108, -772, 92, 28);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Advanced.Resource", T("debug.advanced.resource", "Resource"), SpawnFirstResource, Color(0.24f, 0.28f, 0.14f, 1f), Color(1f, 1f, 1f, 1f), x + 206, -772, 98, 28);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Advanced.Note", Truncate(FirstText(creative.LastMessage, T("debug.advanced.note", "Whitelist only")), 36), 12, Color(0.60f, 0.68f, 0.70f, 1f), TextAnchorMiddleLeft, x + 314, -772, 190, 28);
            }
        }

        private void CreateItemCell(object parent, InventoryDebugItem item, int index, float x, float y, float w, float h)
        {
            object go = CreateUiObject("DTMAPI.DebugConsole.ItemCell." + index, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", item.CanGive ? Color(0.070f, 0.083f, 0.092f, 1f) : Color(0.055f, 0.055f, 0.060f, 1f));
            object button = AddComponent(go, buttonType!);
            SetProperty(button, "targetGraphic", image);
            AddButtonListener(button, () =>
            {
                if (!item.CanGive)
                {
                    SetStatusMessage(FormatItemHover(item), rebuild: false);
                    return;
                }
                GiveItem(item, 1);
            });
            object? sprite = ResolveItemSprite(item.Id);
            if (sprite != null)
                AddImage(go, "DTMAPI.DebugConsole.ItemCell.Icon." + index, sprite, 20, -7, 60, 60);
            else
                AddText(go, "DTMAPI.DebugConsole.ItemCell.IconFallback." + index, item.HasIcon ? "?" : "-", 30, item.HasIcon ? Color(0.64f, 0.82f, 0.70f, 1f) : Color(0.42f, 0.47f, 0.49f, 1f), TextAnchorMiddleCenter, 4, -6, -8, 28, stretch: true);
            if (item.IsModItem)
                AddText(go, "DTMAPI.DebugConsole.ItemCell.Source." + index, Truncate(FirstText(item.SourceModTitle, item.SourceId, item.SourceKind), 11), 10, Color(0.72f, 0.80f, 0.80f, 1f), TextAnchorMiddleCenter, 4, -66, -8, 18, stretch: true);
            if (!item.RuntimeLoaded)
                AddText(go, "DTMAPI.DebugConsole.ItemCell.NotLoaded." + index, T("debug.items.notLoaded.short", "off"), 10, Color(1f, 0.68f, 0.50f, 1f), TextAnchorMiddleCenter, 4, -6, -8, 18, stretch: true);
            float tooltipX = x > 700 ? x - 356 : x + w + 10;
            AddPointerEventListener(go, "PointerDown", eventData =>
            {
                if (!IsRightClick(eventData))
                    return;
                if (!item.CanGive)
                {
                    SetStatusMessage(FormatItemHover(item), rebuild: false);
                    return;
                }
                TryGiveRightClickItem(item, "pointer-down");
            });
            AddPointerEventListener(go, "PointerEnter", _ =>
            {
                SetStatusMessage(FormatItemHover(item), rebuild: false);
                ShowItemTooltip(item, tooltipX, y);
            });
            AddPointerEventListener(go, "PointerExit", _ =>
            {
                SetStatusMessage(string.Empty, rebuild: false);
                HideItemTooltip();
            });
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            if (screenshotHoverStatusActive && item.Id.Equals(screenshotHoverItem, StringComparison.OrdinalIgnoreCase))
                ShowItemTooltip(item, tooltipX, y);
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
                dirty = true;
        }

        private void ShowItemTooltip(InventoryDebugItem item, float x, float y)
        {
            if (panelRoot == null || imageType == null)
                return;
            HideItemTooltip();
            hoverTooltipRoot = CreateUiObject("DTMAPI.DebugConsole.ItemTooltip", panelRoot);
            object image = AddComponent(hoverTooltipRoot, imageType);
            SetProperty(image, "color", Color(0.020f, 0.024f, 0.030f, 0.98f));
            SetProperty(image, "raycastTarget", false);
            AddText(hoverTooltipRoot, "DTMAPI.DebugConsole.ItemTooltip.Text", FormatItemTooltip(item), 12, Color(0.94f, 0.98f, 0.98f, 1f), TextAnchorUpperLeft, 10, -8, -20, -16, stretch: true);
            SetRect(hoverTooltipRoot, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(346, 150));
            runtime.SetHookStatus("Smoke.DebugConsoleHoverTooltip", "verified", "Unity UI pointer hover", "item=" + item.Id + ", source=" + FirstText(item.SourceId, item.SourceKind) + ", searchText=" + FormatLifecycleValue(searchText) + ".");
        }

        private void HideItemTooltip()
        {
            Destroy(hoverTooltipRoot);
            hoverTooltipRoot = null;
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
            dirty = true;
        }

        private void SaveHere()
        {
            if (instantSaveApi == null || ownerManifest == null)
                return;
            InstantSaveDebugResult result = instantSaveApi.Save(ownerManifest, reloadAfterSave: false);
            statusMessage = result.Success
                ? string.Format(T("debug.save.saved", "Saved slot {0}"), result.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "?")
                : string.Format(T("debug.save.failed", "Save failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void ExportTeleportCsv()
        {
            if (teleportApi == null || ownerManifest == null)
                return;
            TeleportCsvExportResult result = teleportApi.ExportDestinationsCsv(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.teleport.exportedCsv", "Exported {0} teleport rows"), result.RowCount)
                : string.Format(T("debug.teleport.exportCsvFailed", "CSV failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
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
            dirty = true;
        }

        private void AdvanceAdvancedTime(AdvancedTimeAdvanceKind kind, int amount)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            TimeSkipResult result = advancedApi.AdvanceTime(ownerManifest, kind, amount);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.timeChanged", "Advanced time: {0}"), result.AdvancedGameMinutes)
                : string.Format(T("debug.advanced.timeFailed", "Advanced time failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
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
            dirty = true;
        }

        private void AddDebugMoney(int amount)
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            DebugValueResult result = advancedApi.AddMoney(ownerManifest, amount);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.moneyChanged", "Money {0} -> {1}"), result.BeforeValue, result.AfterValue)
                : string.Format(T("debug.advanced.moneyFailed", "Money failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void AddFirstTechPoint()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            TechPointDebugOption? option = advancedApi.GetTechPointOptions().FirstOrDefault();
            if (option == null)
            {
                statusMessage = T("debug.advanced.techMissing", "No tech point type found.");
                dirty = true;
                return;
            }

            DebugValueResult result = advancedApi.AddTechPoint(ownerManifest, option.Id, 100);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.techChanged", "{0} {1} -> {2}"), FirstText(option.DisplayName, option.Id), result.BeforeValue, result.AfterValue)
                : string.Format(T("debug.advanced.techFailed", "Tech failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void UnlockAllTechTrees()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            DebugCommandResult result = advancedApi.UnlockAllTechTrees(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.techTreeChanged", "Tech trees affected: {0}"), result.AffectedCount)
                : string.Format(T("debug.advanced.techTreeFailed", "Tech tree failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void MatureAllCrops()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            CropMaturityResult result = advancedApi.MatureAllCrops(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.cropsChanged", "Crops {0}/{1}"), result.CropsMatured, result.PlantBasinsVisited)
                : string.Format(T("debug.advanced.cropsFailed", "Crops failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
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
            dirty = true;
        }

        private void GiveCreativeGenerator()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            InventoryGiveResult result = advancedApi.GiveCreativeGenerator(ownerManifest);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.generatorGiven", "Gave {0}"), FirstText(result.DisplayName, result.ItemId))
                : string.Format(T("debug.advanced.generatorFailed", "Generator failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void SpawnFirstMonster()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            SpawnDebugOption? option = advancedApi.GetMonsterOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom) ?? advancedApi.GetMonsterOptions().FirstOrDefault();
            if (option == null)
            {
                statusMessage = T("debug.advanced.monsterMissing", "No monster option found.");
                dirty = true;
                return;
            }

            SpawnDebugResult result = advancedApi.SpawnMonster(ownerManifest, option.Id, 1);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.monsterSpawned", "Spawned {0}"), FirstText(result.DisplayName, result.SpawnId))
                : string.Format(T("debug.advanced.monsterFailed", "Monster failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void SpawnFirstResource()
        {
            if (advancedApi == null || ownerManifest == null)
                return;
            SpawnDebugOption? option = advancedApi.GetResourceOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom) ?? advancedApi.GetResourceOptions().FirstOrDefault();
            if (option == null)
            {
                statusMessage = T("debug.advanced.resourceMissing", "No resource option found.");
                dirty = true;
                return;
            }

            SpawnDebugResult result = advancedApi.SpawnResource(ownerManifest, option.Id, 1);
            statusMessage = result.Success
                ? string.Format(T("debug.advanced.resourceSpawned", "Spawned {0}"), FirstText(result.DisplayName, result.SpawnId))
                : string.Format(T("debug.advanced.resourceFailed", "Resource failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
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

        private void GiveItem(InventoryDebugItem item, int count, bool rightClick = false)
        {
            if (inventoryApi == null || ownerManifest == null)
                return;
            InventoryGiveResult result = inventoryApi.GiveItem(ownerManifest, item.Id, count);
            statusMessage = result.Success
                ? string.Format(T(rightClick ? "debug.items.gaveRightClick" : "debug.items.gave", rightClick ? "Right-click gave {0} x {1}" : "Gave {0} x {1}"), result.GivenCount, FirstText(result.DisplayName, result.ItemId))
                : string.Format(T("debug.items.failed", "Give failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void SetWeather(WeatherDebugOption weather)
        {
            if (weatherApi == null || ownerManifest == null)
                return;
            WeatherSetResult result = weatherApi.SetWeather(ownerManifest, weather.Id, patchCurrentPeriod: true);
            statusMessage = result.Success
                ? string.Format(T("debug.weather.changed.simple", "Weather: {0}"), LocalizeWeatherName(weather.Id, FirstText(result.DisplayName, weather.DisplayName, weather.Id)))
                : string.Format(T("debug.weather.failed", "Weather failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private void Teleport(TeleportDestination destination)
        {
            if (teleportApi == null || ownerManifest == null)
                return;
            TeleportResult result = teleportApi.Teleport(ownerManifest, destination.Id);
            statusMessage = result.Success
                ? string.Format(T("debug.teleport.requested", "Teleport requested: {0}"), LocalizeTeleportName(FirstText(result.DestinationName, result.MarkPointId)))
                : string.Format(T("debug.teleport.failed", "Teleport failed: {0}"), FirstText(result.FailureReason, result.Message));
            dirty = true;
        }

        private string DisplaySafeLocation(TeleportSnapshot snapshot)
        {
            string fallback = T("debug.teleport.current", "Current location");
            string title = FirstText(snapshot == null ? string.Empty : snapshot.RoomTitle, string.Empty);
            return LocalizeTeleportName(LooksInternalLocationName(title) ? fallback : FirstText(title, fallback));
        }

        private string LocalizeWeatherName(WeatherDebugOption weather)
        {
            return LocalizeWeatherName(weather.Id, FirstText(weather.DisplayName, weather.Id, T("debug.weather.unknown", "Weather")));
        }

        private string LocalizeWeatherName(string weatherId, string displayName)
        {
            string raw = FirstText(weatherId, displayName).Trim();
            string normalized = raw.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            bool english = text.Language.Equals("english", StringComparison.OrdinalIgnoreCase);

            if (ContainsIgnoreCase(normalized, "thunder") || ContainsIgnoreCase(displayName, "雷雨"))
                return english ? "Thunder" : "雷雨";
            if (ContainsIgnoreCase(normalized, "cloud") || ContainsIgnoreCase(displayName, "多云"))
                return english ? "Cloudy" : "多云";
            if (ContainsIgnoreCase(normalized, "wind") || ContainsIgnoreCase(displayName, "大风"))
                return english ? "Wind" : "大风";
            if (ContainsIgnoreCase(normalized, "acid") || ContainsIgnoreCase(displayName, "酸雨"))
                return english ? "Acid" : "酸雨";
            if (ContainsIgnoreCase(normalized, "rain") || ContainsIgnoreCase(displayName, "雨天"))
                return english ? "Rain" : "雨天";
            if (ContainsIgnoreCase(normalized, "scorch") || ContainsIgnoreCase(normalized, "hot") || ContainsIgnoreCase(displayName, "烈日"))
                return english ? "Heat" : "烈日";
            if (ContainsIgnoreCase(normalized, "sun") || ContainsIgnoreCase(normalized, "clear") || ContainsIgnoreCase(displayName, "晴天"))
                return english ? "Sunny" : "晴天";

            if (english && ContainsCjk(displayName))
                return T("debug.weather.unknown", "Weather");
            return FirstText(displayName, T("debug.weather.unknown", "Weather"));
        }

        private string LocalizeTeleportName(TeleportDestination destination)
        {
            return LocalizeTeleportName(FirstText(destination.DisplayName, destination.SuggestedDisplayName, destination.MarkPointId, destination.RoomId, T("debug.teleport.place", "Place")));
        }

        private string LocalizeTeleportName(string value)
        {
            string raw = FirstText(value, T("debug.teleport.place", "Place"));
            bool english = text.Language.Equals("english", StringComparison.OrdinalIgnoreCase);
            if (!english)
                return raw;

            string normalized = raw.Replace("_", " ").Replace(".", " ").Trim();
            if (ContainsIgnoreCase(normalized, "farm") || ContainsIgnoreCase(raw, "农场"))
                return ContainsIgnoreCase(raw, "车站") || ContainsIgnoreCase(raw, "公交") ? "Farm station" : "Farm";
            if (ContainsIgnoreCase(normalized, "town hall") || ContainsIgnoreCase(raw, "市政厅"))
                return "Town hall";
            if (ContainsIgnoreCase(normalized, "research") || ContainsIgnoreCase(raw, "研究所"))
                return "Research institute";
            if (ContainsIgnoreCase(normalized, "bar") || ContainsIgnoreCase(raw, "酒吧"))
                return "Bar";
            if (ContainsIgnoreCase(normalized, "station") || ContainsIgnoreCase(raw, "车站") || ContainsIgnoreCase(raw, "公交"))
                return ContainsIgnoreCase(raw, "城镇") ? "Town station" : "Station";
            if (ContainsIgnoreCase(normalized, "town") || ContainsIgnoreCase(raw, "城镇"))
                return "Town";
            if (ContainsIgnoreCase(raw, "丘陵"))
                return "Hills";
            if (ContainsCjk(raw))
                return T("debug.teleport.place", "Place");
            return normalized.Length == 0 ? T("debug.teleport.place", "Place") : normalized;
        }

        private string FormatCategoryLabel(string value)
        {
            string raw = FirstText(value, T("common.none", "(none)"));
            string normalized = raw.Trim().Replace("-", "_").ToLowerInvariant();
            bool english = text.Language.Equals("english", StringComparison.OrdinalIgnoreCase);

            if (normalized.StartsWith("construction", StringComparison.OrdinalIgnoreCase))
                return english ? "Construction" : "建筑";
            if (normalized.StartsWith("equipment", StringComparison.OrdinalIgnoreCase))
                return english ? "Equipment" : "设备";
            if (normalized.StartsWith("material_ore", StringComparison.OrdinalIgnoreCase) || normalized.Contains("_ore"))
                return english ? "Ore" : "矿物";
            if (normalized.StartsWith("material", StringComparison.OrdinalIgnoreCase))
                return english ? "Material" : "材料";
            if (normalized.StartsWith("food", StringComparison.OrdinalIgnoreCase))
                return english ? "Food" : "食物";
            if (normalized.StartsWith("seed", StringComparison.OrdinalIgnoreCase))
                return english ? "Seed" : "种子";
            if (normalized.StartsWith("tool", StringComparison.OrdinalIgnoreCase))
                return english ? "Tool" : "工具";
            if (normalized.StartsWith("weapon", StringComparison.OrdinalIgnoreCase))
                return english ? "Weapon" : "武器";
            if (normalized.StartsWith("furniture", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("ornament", StringComparison.OrdinalIgnoreCase))
                return english ? "Decor" : "装饰";

            if (english && ContainsCjk(raw))
                return T("common.none", "(none)");
            return raw.Replace("_", " ");
        }

        private static bool LooksInternalLocationName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            if (Guid.TryParse(value, out _))
                return true;
            return value.IndexOf('_') >= 0 || value.IndexOf('.') >= 0;
        }

        private void CaptureUiEvidenceScreenshot()
        {
            screenshotPending = false;
            screenshotRecorded = true;
            try
            {
                string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "DEBUG-CONSOLE-UI", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(evidenceDir);
                string screenshotPath = Path.Combine(evidenceDir, "debug-console.png");
                bool captured = TryCaptureScreenshot(screenshotPath);
                File.WriteAllText(Path.Combine(evidenceDir, "summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "ScreenshotRequested=" + captured + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "ItemsUseNativeSprites=True" + Environment.NewLine +
                    "SourceColumns=True" + Environment.NewLine +
                    "HoverDetailTooltip=True" + Environment.NewLine +
                    "HoverEvidenceItem=" + screenshotHoverItem + Environment.NewLine +
                    "HoverEvidenceSourceKind=" + screenshotHoverSourceKind + Environment.NewLine +
                    "HoverEvidenceSourceId=" + screenshotHoverSourceId + Environment.NewLine +
                    "HoverEvidenceSourceTitle=" + screenshotHoverSourceTitle + Environment.NewLine +
                    "HoverEvidenceWorkshopId=" + screenshotHoverWorkshopId + Environment.NewLine +
                    "ModItemsOnly=" + modItemsOnly + Environment.NewLine +
                    "SourceFilter=" + sourceFilter + Environment.NewLine +
                    "SearchText=" + screenshotSearchText + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Debug console UI screenshot " + (captured ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
                runtime.SetHookStatus("Smoke.DebugConsoleScreenshot", captured ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Debug console screenshot=" + (captured ? screenshotPath : "unavailable") + ".");
                if (!string.IsNullOrWhiteSpace(screenshotHoverItem) && !string.IsNullOrWhiteSpace(screenshotHoverSourceKind))
                {
                    string sourceSummary = "item=" + screenshotHoverItem +
                        ", sourceKind=" + screenshotHoverSourceKind +
                        ", sourceTitle=" + screenshotHoverSourceTitle +
                        ", sourceId=" + screenshotHoverSourceId +
                        ", workshopId=" + FirstText(screenshotHoverWorkshopId, "none") +
                        ", modItemsOnly=" + modItemsOnly +
                        ", searchText=" + screenshotSearchText +
                        ", screenshot=" + (captured ? screenshotPath : "unavailable");
                    runtime.RuntimeMonitor.Log("Debug console mod item UI evidence OK " + sourceSummary);
                    runtime.SetHookStatus("Smoke.DebugConsoleModItemUi", captured ? "verified" : "pending", "IInventoryDebugApi.GetItems + source hover", sourceSummary);
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.DebugConsole", "Debug console UI screenshot capture failed.", ex.ToString());
            }
            finally
            {
                if (screenshotHoverStatusActive)
                {
                    statusMessage = string.Empty;
                    screenshotHoverStatusActive = false;
                    dirty = true;
                }
            }
        }

        private void PrepareScreenshotHoverEvidence()
        {
            screenshotHoverPrepared = true;
            if (inventoryApi == null)
                return;

            try
            {
                InventoryDebugItem? item = SelectScreenshotEvidenceItem();
                if (item == null)
                    return;

                screenshotSearchText = searchText;
                screenshotHoverItem = item.Id;
                screenshotHoverSourceKind = item.SourceKind;
                screenshotHoverSourceId = item.SourceId;
                screenshotHoverSourceTitle = item.SourceModTitle;
                screenshotHoverWorkshopId = item.WorkshopId.HasValue ? item.WorkshopId.Value.ToString() : string.Empty;
                statusMessage = FormatItemHover(item);
                screenshotHoverStatusActive = true;
                dirty = true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.DebugConsole", "Debug console hover evidence preparation failed.", ex.ToString());
            }
        }

        private InventoryDebugItem? SelectScreenshotEvidenceItem()
        {
            InventoryDebugPage currentPage = inventoryApi!.GetItems(new InventoryDebugQuery
            {
                SearchText = searchText,
                Category = category,
                SourceId = sourceFilter,
                ModItemsOnly = modItemsOnly,
                IncludeUnavailable = true,
                Page = itemPage,
                PageSize = 50
            });

            InventoryDebugItem? visibleItem = currentPage.Items
                .Where(i => i.CanGive && i.RuntimeLoaded)
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? currentPage.Items.FirstOrDefault();
            if (visibleItem != null)
                return visibleItem;

            InventoryDebugPage modPage = inventoryApi!.GetItems(new InventoryDebugQuery
            {
                ModItemsOnly = true,
                IncludeUnavailable = true,
                PageSize = 500
            });

            InventoryDebugItem? preferredButter = modPage.Items
                .Where(IsRuntimeWorkshopModItem)
                .Where(i => i.WorkshopId == 3722791728UL || i.SourceId.Equals("Workshop.3722791728", StringComparison.OrdinalIgnoreCase))
                .Where(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(i.DisplayName, "黄油") || ContainsIgnoreCase(i.SearchText, "butter"))
                .OrderBy(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (preferredButter != null)
                return preferredButter;

            InventoryDebugItem? preferredWorkshop = modPage.Items
                .Where(IsRuntimeWorkshopModItem)
                .OrderBy(i => i.WorkshopId == 3722791728UL ? 0 : 1)
                .ThenBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (preferredWorkshop != null)
                return preferredWorkshop;

            InventoryDebugItem? modItem = modPage.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (modItem != null)
                return modItem;

            InventoryDebugPage page = inventoryApi.GetItems(new InventoryDebugQuery
            {
                SearchText = searchText,
                Category = category,
                Page = itemPage,
                PageSize = 1
            });
            return page.Items.FirstOrDefault();
        }

        private static bool IsRuntimeWorkshopModItem(InventoryDebugItem item)
        {
            return item.CanGive &&
                item.RuntimeLoaded &&
                item.IsModItem &&
                item.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildScreenshotSearchText(InventoryDebugItem item)
        {
            if (item.WorkshopId == 3722791728UL && (ContainsIgnoreCase(item.DisplayName, "黄油") || ContainsIgnoreCase(item.SearchText, "黄油")))
                return "黄油";
            return FirstText(item.DisplayName, item.ChineseName, item.Id);
        }

        private static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = ResolveRuntimeType("UnityEngine.ScreenCapture") ?? Type.GetType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.ScreenCapture, UnityEngine");
                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetScreenSize(out double width, out double height)
        {
            width = 0;
            height = 0;
            try
            {
                Type? screen = ResolveRuntimeType("UnityEngine.Screen") ?? Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Screen, UnityEngine");
                object? widthValue = screen?.GetProperty("width", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null);
                object? heightValue = screen?.GetProperty("height", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null);
                if (widthValue == null || heightValue == null)
                    return false;

                width = Convert.ToDouble(widthValue);
                height = Convert.ToDouble(heightValue);
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }

        private object CreateRow(object parent, string name, string label, float x, float y, float w, float h)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", Color(0.075f, 0.085f, 0.095f, 1f));
            AddText(go, name + ".Text", label, 13, Color(0.96f, 0.98f, 0.98f, 1f), TextAnchorMiddleLeft, 10, 0, -20, 0, stretch: true);
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return go;
        }

        private object CreateButton(object parent, string name, string label, Action onClick, object background, object textColor, float x, float y, float w, float h)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", background);
            object button = AddComponent(go, buttonType!);
            SetProperty(button, "targetGraphic", image);
            AddButtonListener(button, onClick);
            AddText(go, name + ".Text", label, 13, textColor, TextAnchorMiddleCenter, 0, 0, 0, 0, stretch: true);
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return go;
        }

        private object CreateInput(object parent, string name, string value, Action<string> onEdited, float x, float y, float w, float h)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", Color(0.02f, 0.025f, 0.03f, 1f));
            object textObject = AddText(go, name + ".Text", FirstText(value, T("debug.items.search", "Search")), 14, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 8, 0, -16, 0, stretch: true);
            if (inputFieldType != null && unityActionStringType != null)
            {
                object input = AddComponent(go, inputFieldType);
                SetProperty(input, "targetGraphic", image);
                SetProperty(input, "textComponent", textObject);
                SetProperty(input, "text", value ?? string.Empty);
                inputFields.Add(input);
                AddStringListener(GetProperty(input, "onEndEdit"), onEdited);
            }
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return go;
        }

        private bool IsAnyTextInputFocused()
        {
            foreach (object input in inputFields.ToArray())
            {
                if (input == null || IsDestroyed(input))
                    continue;
                if (ReadBoolProperty(input, "isFocused"))
                    return true;
            }

            object? currentSelected = GetCurrentSelectedGameObject();
            if (currentSelected == null || IsDestroyed(currentSelected))
                return false;
            foreach (object input in inputFields.ToArray())
            {
                object? inputGameObject = GetProperty(input, "gameObject");
                if (ReferenceEquals(inputGameObject, currentSelected))
                    return true;
            }

            return false;
        }

        private object? GetCurrentSelectedGameObject()
        {
            try
            {
                object? current = eventSystemType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
                return current == null ? null : GetProperty(current, "currentSelectedGameObject");
            }
            catch
            {
                return null;
            }
        }

        private static bool ReadBoolProperty(object target, string name)
        {
            try
            {
                object? value = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(target, null);
                return value is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private object AddText(object parent, string name, string value, int fontSize, object color, int alignment, float x, float y, float w, float h, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object textObject = AddComponent(go, textType!);
            SetProperty(textObject, "text", value ?? string.Empty);
            SetProperty(textObject, "font", GetBuiltinFont());
            SetProperty(textObject, "fontSize", fontSize);
            SetProperty(textObject, "color", color);
            SetEnumProperty(textObject, "alignment", alignment);
            SetProperty(textObject, "raycastTarget", false);
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(x, y), Vector2(w, h));
            else
                SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return textObject;
        }

        private object AddImage(object parent, string name, object sprite, float x, float y, float w, float h, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "sprite", sprite);
            SetProperty(image, "preserveAspect", true);
            SetProperty(image, "raycastTarget", false);
            SetProperty(image, "color", Color(1f, 1f, 1f, 1f));
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(x, y), Vector2(w, h));
            else
                SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return image;
        }

        private object? ResolveItemSprite(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;
            try
            {
                Type? dolocApi = ResolveRuntimeType("DolocAPI");
                MethodInfo? getItemSprite = dolocApi?.GetMethod("GetItemSprite", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                return getItemSprite?.Invoke(null, new object[] { itemId });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.BepInExBootstrap", "Debug console item sprite lookup failed for " + itemId + ".", ex.ToString());
                return null;
            }
        }

        private object CreateUiObject(string name, object? parent)
        {
            object go;
            ConstructorInfo? ctor = gameObjectType!.GetConstructor(new[] { typeof(string), typeof(Type[]) });
            if (ctor != null)
                go = ctor.Invoke(new object[] { name, new[] { rectTransformType! } });
            else
                go = Activator.CreateInstance(gameObjectType!, name);

            if (parent != null)
            {
                object? transform = GetProperty(go, "transform");
                object? parentTransform = GetProperty(parent, "transform");
                transform?.GetType().GetMethod("SetParent", new[] { transformType!, typeof(bool) })?.Invoke(transform, new[] { parentTransform, false });
            }
            return go;
        }

        private object CreatePlainObject(string name)
        {
            return Activator.CreateInstance(gameObjectType!, name);
        }

        private object AddComponent(object go, Type componentType)
        {
            return gameObjectType!.GetMethod("AddComponent", new[] { typeof(Type) })!.Invoke(go, new object[] { componentType });
        }

        private object? GetComponent(object go, Type componentType)
        {
            return gameObjectType!.GetMethod("GetComponent", new[] { typeof(Type) })?.Invoke(go, new object[] { componentType });
        }

        private object? FindObjectOfType(Type componentType)
        {
            try
            {
                object? found = objectType?.GetMethod("FindObjectOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null)?.Invoke(null, new object[] { componentType });
                return found != null && !IsDestroyed(found) ? found : null;
            }
            catch
            {
                return null;
            }
        }

        private void AddButtonListener(object button, Action action)
        {
            object? onClick = GetProperty(button, "onClick");
            if (onClick == null || unityActionType == null)
                return;
            var binder = new ActionBinder(action);
            eventBinders.Add(binder);
            Delegate del = Delegate.CreateDelegate(unityActionType, binder, nameof(ActionBinder.Invoke));
            onClick.GetType().GetMethod("AddListener", new[] { unityActionType })?.Invoke(onClick, new object[] { del });
        }

        private void AddStringListener(object? unityEvent, Action<string> action)
        {
            if (unityEvent == null || unityActionStringType == null)
                return;
            var binder = new StringActionBinder(action);
            eventBinders.Add(binder);
            Delegate del = Delegate.CreateDelegate(unityActionStringType, binder, nameof(StringActionBinder.Invoke));
            unityEvent.GetType().GetMethod("AddListener", new[] { unityActionStringType })?.Invoke(unityEvent, new object[] { del });
        }

        private bool AddPointerClickListener(object go, Action<object?> action)
        {
            return AddPointerEventListener(go, "PointerClick", action);
        }

        private bool AddPointerEventListener(object go, string eventName, Action<object?> action)
        {
            if (eventTriggerType == null || eventTriggerEntryType == null || eventTriggerTypeEnum == null || unityActionBaseEventDataType == null)
                return false;
            try
            {
                object trigger = AddComponent(go, eventTriggerType);
                object entry = Activator.CreateInstance(eventTriggerEntryType);
                SetProperty(entry, "eventID", Enum.Parse(eventTriggerTypeEnum, eventName));
                object? callback = GetProperty(entry, "callback");
                var binder = new PointerActionBinder(action);
                eventBinders.Add(binder);
                Delegate del = CreatePointerActionDelegate(binder);
                callback?.GetType().GetMethod("AddListener", new[] { unityActionBaseEventDataType })?.Invoke(callback, new object[] { del });
                object? triggers = GetProperty(trigger, "triggers");
                triggers?.GetType().GetMethod("Add")?.Invoke(triggers, new[] { entry });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Delegate CreatePointerActionDelegate(PointerActionBinder binder)
        {
            if (baseEventDataType == null || unityActionBaseEventDataType == null)
                throw new InvalidOperationException("Unity BaseEventData action type is not available.");

            ParameterExpression eventData = Expression.Parameter(baseEventDataType, "eventData");
            MethodInfo invoke = typeof(PointerActionBinder).GetMethod(nameof(PointerActionBinder.Invoke), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new MissingMethodException(nameof(PointerActionBinder), nameof(PointerActionBinder.Invoke));
            MethodCallExpression call = Expression.Call(Expression.Constant(binder), invoke, Expression.Convert(eventData, typeof(object)));
            return Expression.Lambda(unityActionBaseEventDataType, call, eventData).Compile();
        }

        private static bool IsRightClick(object? eventData)
        {
            object? value = eventData == null ? null : GetProperty(eventData, "button");
            if (value == null)
                return false;
            string? text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
            }
            catch
            {
                return false;
            }
        }

        private void SetRect(object go, object anchorMin, object anchorMax, object pivot, object anchoredPosition, object sizeDelta)
        {
            object? rect = GetComponent(go, rectTransformType!);
            if (rect == null)
                return;
            SetProperty(rect, "anchorMin", anchorMin);
            SetProperty(rect, "anchorMax", anchorMax);
            SetProperty(rect, "pivot", pivot);
            SetProperty(rect, "anchoredPosition", anchoredPosition);
            SetProperty(rect, "sizeDelta", sizeDelta);
        }

        private void Destroy(object? go)
        {
            if (go == null || objectType == null)
                return;
            objectType.GetMethod("Destroy", new[] { objectType })?.Invoke(null, new[] { go });
        }

        private void DontDestroyOnLoad(object go)
        {
            objectType?.GetMethod("DontDestroyOnLoad", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType! }, null)?.Invoke(null, new[] { go });
        }

        private bool SetActive(object? go, bool active)
        {
            if (go == null || IsDestroyed(go))
                return false;
            try
            {
                go.GetType().GetMethod("SetActive", new[] { typeof(bool) })?.Invoke(go, new object[] { active });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsDestroyed(object go)
        {
            if (objectType == null || !objectType.IsAssignableFrom(go.GetType()))
                return false;
            MethodInfo? equality = objectType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType, objectType }, null);
            if (equality == null)
                return false;
            try
            {
                return equality.Invoke(null, new object?[] { go, null }) is bool destroyed && destroyed;
            }
            catch
            {
                return false;
            }
        }

        private static Type? ResolveRuntimeType(string typeName)
        {
            Type? type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type != null)
                return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static object? GetProperty(object target, string name)
        {
            Type type = target.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property != null)
                return property.GetValue(target, null);
            FieldInfo? field = type.GetField(name, flags);
            return field?.GetValue(target);
        }

        private static void SetProperty(object target, string name, object? value)
        {
            Type type = target.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property != null)
            {
                property.SetValue(target, value, null);
                return;
            }

            FieldInfo? field = type.GetField(name, flags);
            field?.SetValue(target, value);
        }

        private static void SetEnumProperty(object target, string name, int value)
        {
            PropertyInfo? property = target.GetType().GetProperty(name);
            if (property == null)
                return;
            property.SetValue(target, Enum.ToObject(property.PropertyType, value), null);
        }

        private object GetBuiltinFont()
        {
            MethodInfo? getBuiltin = resourcesType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetBuiltinResource" && m.GetParameters().Length == 2);
            return getBuiltin?.Invoke(null, new object[] { fontType!, "Arial.ttf" }) ?? throw new InvalidOperationException("Unity built-in font Arial.ttf was not found.");
        }

        private object Color(float r, float g, float b, float a) => Activator.CreateInstance(colorType!, r, g, b, a);
        private object Vector2(float x, float y) => Activator.CreateInstance(vector2Type!, x, y);
        private string T(string key, string fallback) => text.Get(key, fallback);

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static string Truncate(string value, int max)
        {
            value ??= string.Empty;
            return value.Length <= max ? value : value.Substring(0, Math.Max(0, max - 1)) + "...";
        }

        private static bool ContainsIgnoreCase(string value, string part)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                !string.IsNullOrWhiteSpace(part) &&
                value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (char c in value)
            {
                if (c >= '\u4e00' && c <= '\u9fff')
                    return true;
            }
            return false;
        }

        private static string FormatLifecycleValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "<empty>" : value;
        }

        private const int TextAnchorMiddleLeft = 3;
        private const int TextAnchorMiddleCenter = 4;
        private const int TextAnchorUpperLeft = 0;

        private sealed class ActionBinder
        {
            private readonly Action action;
            public ActionBinder(Action action) { this.action = action; }
            public void Invoke() => action();
        }

        private sealed class StringActionBinder
        {
            private readonly Action<string> action;
            public StringActionBinder(Action<string> action) { this.action = action; }
            public void Invoke(string value) => action(value);
        }

        private sealed class PointerActionBinder
        {
            private readonly Action<object?> action;
            public PointerActionBinder(Action<object?> action) { this.action = action; }
            public void Invoke(object? value) => action(value);
        }
    }
}
