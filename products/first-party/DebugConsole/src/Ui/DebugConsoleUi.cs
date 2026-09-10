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
        [Flags]
        private enum UiDirtyRegion
        {
            None = 0,
            Layout = 1,
            Catalog = 2,
            Weather = 4,
            Teleport = 8,
            Status = 16,
            World = 32,
            Advanced = 64,
            Dynamic = Catalog | Weather | Teleport | Status | World | Advanced,
            All = Layout | Dynamic
        }

        private const string MenuId = "DTMAPI.DebugConsole";
        private const string SourceFilterBase = "__base";
        private const string SourceFilterMods = "__mods";
        private const int EscapeCloseDrainRequiredCleanFrames = 2;
        private static readonly TimeSpan SaveConfirmationWindow =
            TimeSpan.FromSeconds(8);
        private static readonly string[] CatalogCategoryIds =
            DebugConsoleNativeActions.StableItemCategoryIds
                .Concat(new[] { "Monster", "Animal" })
                .ToArray();
        private readonly IDebugConsoleRuntime runtime;
        private readonly List<object> eventBinders = new List<object>();
        private readonly List<object> inputFields = new List<object>();
        private readonly List<object> catalogChromeBinders = new List<object>();
        private readonly List<object> catalogPoolBinders = new List<object>();
        private readonly List<object> worldBinders = new List<object>();
        private readonly List<object> advancedBinders = new List<object>();
        private readonly List<object> catalogInputFields = new List<object>();
        private List<object>? activeBinderSink;
        private List<object>? activeInputSink;
        private DebugConsoleText text;
        private IInventoryActions? inventoryApi;
        private IWeatherActions? weatherApi;
        private ITeleportActions? teleportApi;
        private ITimeActions? timeApi;
        private IMovementActions? movementApi;
        private IInstantSaveActions? instantSaveApi;
        private IAdvancedActions? advancedApi;
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
        private Type? tmpInputFieldType;
        private Type? scrollRectType;
        private Type? rectMask2DType;
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
        private MethodInfo? pointerInvokeClosedMethod;
        private PropertyInfo? eventSystemCurrentProperty;
        private PropertyInfo? eventSystemSelectedGameObjectProperty;
        private MethodInfo? gameObjectGetComponentByTypeMethod;
        private PropertyInfo? inputFieldIsFocusedProperty;
        private PropertyInfo? tmpInputFieldIsFocusedProperty;
        private bool textInputFocusMetadataResolved;
        private Type? screenType;
        private PropertyInfo? screenWidthProperty;
        private PropertyInfo? screenHeightProperty;
        private PropertyInfo? screenSafeAreaProperty;
        private Type? screenRectType;
        private PropertyInfo? safeAreaXProperty;
        private PropertyInfo? safeAreaYProperty;
        private PropertyInfo? safeAreaWidthProperty;
        private PropertyInfo? safeAreaHeightProperty;
        private Type? dolocApiType;
        private MethodInfo? getItemSpriteMethod;
        private MethodInfo? getBuiltinResourceMethod;
        private object? builtInFont;

        private object? root;
        private object? panelRoot;
        private object? headerRoot;
        private object? catalogRoot;
        private object? catalogRegionHost;
        private object? catalogChromeRoot;
        private object? catalogGridRoot;
        private object? worldRoot;
        private object? worldRegionHost;
        private object? worldContentRoot;
        private object? advancedRoot;
        private object? advancedRegionHost;
        private object? advancedContentRoot;
        private object? catalogTabBackground;
        private object? worldTabBackground;
        private object? advancedTabBackground;
        private object? eventSystemRoot;
        private object? eventSystemComponent;
        private object? statusTextObject;
        private object? hoverTooltipRoot;
        private object? hoverTooltipTextObject;
        private string searchText = string.Empty;
        private string category = string.Empty;
        private string sourceFilter = SourceFilterBase;
        private string statusMessage = string.Empty;
        private int itemPage;
        private int sourcePage;
        private bool initialized;
        private UiDirtyRegion dirtyRegions = UiDirtyRegion.All;
        private DebugConsoleLayout? layout;
        private string renderedLanguage = string.Empty;
        private bool layoutInputObserved;
        private double layoutInputScreenWidth;
        private double layoutInputScreenHeight;
        private double layoutInputSafeX;
        private double layoutInputSafeY;
        private double layoutInputSafeWidth;
        private double layoutInputSafeHeight;
        private DebugConsoleTab activeTab = DebugConsoleTab.Catalog;
        private readonly List<CatalogCell> catalogCells =
            new List<CatalogCell>();
        private readonly Dictionary<string, object?> itemSpriteCache =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private InventoryDebugSourceGroup[]? unifiedCatalogSources;
        private int? activeSaveSlot;
        private bool saveSessionObserved;
        private bool openLogged;
        private bool suppressYCloseUntilReleased;
        private bool suppressLegacyYCloseUntilReleased;
        private bool suppressEscapeCloseUntilReleased;
        private int escapeCloseDrainCleanFrames;
        private bool eventSystemPendingLogged;
        private int eventSystemFailureCount;
        private string eventSystemFailureFingerprint = string.Empty;
        private bool rightClickGiveBindingFailureLogged;
        private bool rightClickGiveBindingVerified;
        private bool saveConfirmationArmed;
        private DateTimeOffset saveConfirmationExpiresAtUtc =
            DateTimeOffset.MinValue;
        private DateTimeOffset lastRightClickGiveAt;
        private DateTimeOffset nextEventSystemRetryAtUtc = DateTimeOffset.MinValue;

        public DebugConsoleUi(IDebugConsoleRuntime runtime)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            text = new DebugConsoleText(runtime.Translation);
        }

        public bool IsOpen { get; private set; }
        public bool ConsumedInputThisFrame { get; private set; }
        internal bool NeedsUpdate =>
            IsOpen ||
            suppressYCloseUntilReleased ||
            suppressLegacyYCloseUntilReleased ||
            suppressEscapeCloseUntilReleased;

        public string GetLifecycleSummary()
        {
            return "initialized=" + initialized.ToString(CultureInfo.InvariantCulture) +
                "; open=" + IsOpen.ToString(CultureInfo.InvariantCulture) +
                "; rootAlive=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) +
                "; root=" + FormatUnityObjectState(root) +
                "; panelRoot=" + FormatUnityObjectState(panelRoot) +
                "; hoverTooltipRoot=" + FormatUnityObjectState(hoverTooltipRoot) +
                "; statusText=" + FormatUnityObjectState(statusTextObject) +
                "; eventSystemRoot=" + FormatUnityObjectState(eventSystemRoot) +
                "; eventSystemComponent=" + FormatUnityObjectState(eventSystemComponent) +
                "; eventBinders=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; inputFields=" + inputFields.Count.ToString(CultureInfo.InvariantCulture) +
                "; inputFieldsAlive=" + CountAliveUnityObjects(inputFields).ToString(CultureInfo.InvariantCulture) +
                "; owner=" + (ownerManifest?.UniqueID ?? "none") +
                "; activeSaveSlot=" + (activeSaveSlot.HasValue ? activeSaveSlot.Value.ToString(CultureInfo.InvariantCulture) : "none") +
                "; saveSessionObserved=" + saveSessionObserved.ToString(CultureInfo.InvariantCulture) +
                "; saveConfirmationArmed=" + saveConfirmationArmed.ToString(CultureInfo.InvariantCulture) +
                "; searchTextLength=" + searchText.Length.ToString(CultureInfo.InvariantCulture);
        }

        public string GetOwnerObjectGraphSummary()
        {
            string owner = SanitizeMetricKey(ownerManifest?.UniqueID ?? "DTMAPI.DebugConsole");
            int eventSystems = IsAlive(eventSystemRoot) > 0 || IsAlive(eventSystemComponent) > 0 ? 1 : 0;
            int scrollRects = new[]
                {
                    catalogRegionHost,
                    worldRegionHost,
                    advancedRegionHost
                }
                .Where(value => value != null)
                .Distinct()
                .Count(value =>
                    IsAlive(value) > 0 &&
                    scrollRectType != null &&
                    GetComponent(value!, scrollRectType) != null);
            return owner + "={Canvas=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) +
                "; EventSystem=" + eventSystems.ToString(CultureInfo.InvariantCulture) +
                "; Button=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; InputField=" + inputFields.Count.ToString(CultureInfo.InvariantCulture) +
                "; ScrollRect=" + scrollRects.ToString(CultureInfo.InvariantCulture) +
                "; UnityEventListeners=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; DynamicBinders=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; rootAlive=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) + "}";
        }

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
            suppressLegacyYCloseUntilReleased = false;
            if (IsOpen)
                Close(ownerManifest!, "ReturnedToTitle");
            else
            {
                suppressEscapeCloseUntilReleased = false;
                escapeCloseDrainCleanFrames = 0;
                runtime.NativeInputDrainActive = false;
            }
            ResetSaveConfirmation();
            ReleaseOwnerGraph("ReturnedToTitle");
        }

        public void Shutdown(string reason)
        {
            ReleaseOwnerGraph("Shutdown " + (reason ?? string.Empty));
            ClearOwnerBinding();
            TryLog("DTMAPI debug console UI shutdown cleanup reason=" + (reason ?? string.Empty) + ".");
        }

        public void Bind(IManifest owner, IInventoryActions? inventoryApi, IWeatherActions? weatherApi, ITeleportActions? teleportApi, ITimeActions? timeApi, IMovementActions? movementApi, IInstantSaveActions? instantSaveApi = null)
        {
            ownerManifest = ClaimOwner(owner);
            this.inventoryApi = inventoryApi;
            this.weatherApi = weatherApi;
            this.teleportApi = teleportApi;
            this.timeApi = timeApi;
            this.movementApi = movementApi;
            this.instantSaveApi = instantSaveApi;
            unifiedCatalogSources = null;
            dirtyRegions |= UiDirtyRegion.Catalog |
                UiDirtyRegion.Weather |
                UiDirtyRegion.Teleport |
                UiDirtyRegion.Status |
                UiDirtyRegion.World;
            runtime.RuntimeMonitor.Log("Debug console host bound owner=" + (owner?.UniqueID ?? "unknown") + " inventory=" + (inventoryApi != null) + " weather=" + (weatherApi != null) + " teleport=" + (teleportApi != null) + " time=" + (timeApi != null) + " movement=" + (movementApi != null) + " instantSave=" + (instantSaveApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleHost", "product-native", "Unity UI Canvas", "In-save Y-key console bound to private inventory/weather/teleport/time/movement/instant-save action ports.");
        }

        public void BindAdvanced(IManifest owner, IAdvancedActions? advancedDebugApi)
        {
            ownerManifest = ClaimOwner(owner);
            advancedApi = advancedDebugApi;
            unifiedCatalogSources = null;
            dirtyRegions |= UiDirtyRegion.Catalog |
                UiDirtyRegion.Status |
                UiDirtyRegion.World |
                UiDirtyRegion.Advanced;
            runtime.RuntimeMonitor.Log("Debug console host advanced binding owner=" + (owner?.UniqueID ?? "unknown") + " advanced=" + (advancedDebugApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleAdvancedHost", advancedDebugApi != null ? "product-native" : "missing-actions", "Unity UI Canvas + private action allowlist", "Advanced Y-console controls bound=" + (advancedDebugApi != null) + ".");
        }

        public void Open(IManifest owner, string reason)
        {
            ownerManifest = ClaimOwner(owner);
            if (IsOpen)
                return;
            if (!runtime.UI.OpenOwnerBoundCustomMenu(
                    MenuId,
                    ownerManifest!.UniqueID))
            {
                runtime.SetHookStatus(
                    "UI.DebugConsoleVisibility",
                    "blocked-foreign-modal",
                    "DTMAPI owner-bound modal token",
                    "DebugConsole did not open because another owner already holds the modal token.");
                return;
            }
            IsOpen = true;
            text = new DebugConsoleText(runtime.Translation);
            dirtyRegions |= UiDirtyRegion.Dynamic;
            if (panelRoot == null ||
                IsDestroyed(panelRoot) ||
                !string.Equals(
                    renderedLanguage,
                    text.Language,
                    StringComparison.OrdinalIgnoreCase))
            {
                dirtyRegions |= UiDirtyRegion.Layout;
            }
            suppressYCloseUntilReleased = IsYHotkeyReason(reason);
            try
            {
                runtime.ModalOpen = true;
                runtime.NativeInputDrainActive =
                    suppressEscapeCloseUntilReleased;
            }
            catch (Exception hookFailure)
            {
                IsOpen = false;
                dirtyRegions |= UiDirtyRegion.All;
                suppressYCloseUntilReleased = false;
                suppressEscapeCloseUntilReleased = false;
                escapeCloseDrainCleanFrames = 0;
                try
                {
                    runtime.ModalOpen = false;
                    runtime.NativeInputDrainActive = false;
                }
                catch
                {
                }
                if (runtime.UI.IsOpen &&
                    runtime.UI.ActiveMenuId.Equals(
                        MenuId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    runtime.UI.Close();
                }
                runtime.SetHookStatus(
                    "UI.DebugConsoleVisibility",
                    "hook-install-failed",
                    "DTMAPI owner-bound modal token + exact Harmony owner",
                    "DebugConsole released the modal token because native input isolation could not be installed: " +
                    hookFailure.GetType().Name + ": " +
                    hookFailure.Message);
                throw;
            }
            runtime.RuntimeMonitor.Log("Debug console opened owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + " reason=" + (reason ?? string.Empty) + ".");
            runtime.RuntimeMonitor.Log("Debug console open lifecycle state searchText=" + FormatLifecycleValue(searchText) +
                " category=" + FormatLifecycleValue(category) +
                " sourceFilter=" + FormatLifecycleValue(sourceFilter) +
                " itemPage=" + itemPage + ".");
            runtime.SetHookStatus("UI.DebugConsoleVisibility", "open", "DTMAPI.DebugConsoleMod + Unity UI Canvas", "Debug console opened from " + (reason ?? "unknown") + ".");
            ApplyVisibilityState();
        }

        public void Close(IManifest owner, string reason)
        {
            EnsureOwnerMatchesIfBound(owner);
            if (!IsOpen)
                return;
            bool ownedRuntimeMenuOpen = runtime.UI.IsOpen && runtime.UI.ActiveMenuId.Equals(MenuId, StringComparison.OrdinalIgnoreCase);
            string activeRuntimeMenu = runtime.UI.ActiveMenuId;
            bool previousInputDrain =
                suppressEscapeCloseUntilReleased;
            bool nextInputDrain =
                suppressEscapeCloseUntilReleased;
            if (IsEscapeCloseReason(reason))
            {
                nextInputDrain = true;
            }
            else if (IsReturnedToTitleReason(reason))
            {
                nextInputDrain = false;
            }

            try
            {
                runtime.NativeInputDrainActive =
                    nextInputDrain;
                runtime.ModalOpen = false;
                if (runtime.UI.IsOpen &&
                    runtime.UI.ActiveMenuId.Equals(
                        MenuId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    runtime.UI.Close();
                }
            }
            catch (Exception closeFailure)
            {
                var failures =
                    new List<Exception>
                    {
                        closeFailure
                    };
                try
                {
                    if (!runtime.UI.IsOpen ||
                        !runtime.UI.ActiveMenuId.Equals(
                            MenuId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (!runtime.UI.OpenOwnerBoundCustomMenu(
                                MenuId,
                                ownerManifest!.UniqueID))
                        {
                            throw new InvalidOperationException(
                                "DebugConsole could not reacquire its Core modal token after a failed close.");
                        }
                    }
                    runtime.ModalOpen = true;
                    runtime.NativeInputDrainActive =
                        previousInputDrain;
                }
                catch (Exception rollbackFailure)
                {
                    failures.Add(rollbackFailure);
                }

                dirtyRegions |= UiDirtyRegion.All;
                runtime.SetHookStatus(
                    "UI.DebugConsoleVisibility",
                    "close-failed",
                    "DTMAPI owner-bound modal token + exact Harmony owner",
                    "DebugConsole close failed; the open state was restored for retry: " +
                    closeFailure.GetType().Name + ": " +
                    closeFailure.Message);
                ApplyVisibilityState();
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "DebugConsole close failed and restoring its open state was incomplete.",
                    failures);
            }

            IsOpen = false;
            ResetSaveConfirmation();
            suppressYCloseUntilReleased = false;
            suppressEscapeCloseUntilReleased =
                nextInputDrain;
            escapeCloseDrainCleanFrames = 0;
            PrepareRetainedUiForClose();
            runtime.RuntimeMonitor.Log("Debug console close boundary reason=" + (reason ?? string.Empty) + " modalOpen=" + ownedRuntimeMenuOpen + " activeMenu=" + (activeRuntimeMenu.Length == 0 ? "none" : activeRuntimeMenu) + ".");
            runtime.RuntimeMonitor.Log("Debug console closed reason=" + (reason ?? string.Empty) + " owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + ".");
            runtime.SetHookStatus("UI.DebugConsoleVisibility", "closed", "DTMAPI.DebugConsoleMod + Unity UI Canvas", "Debug console closed from " + (reason ?? "unknown") + ".");
            ApplyVisibilityState();
        }

        public void Toggle(IManifest owner, string reason)
        {
            ownerManifest = ClaimOwner(owner);
            if (IsOpen)
                Close(owner, reason);
            else
                Open(owner, reason);
        }

        public int CountOwnerResources(string ownerId)
        {
            return OwnerMatches(ownerId) ? 1 : 0;
        }

        public int RemoveOwner(string ownerId, string reason)
        {
            if (!OwnerMatches(ownerId))
                return 0;

            string canonicalOwnerId = ownerManifest?.UniqueID ?? ownerId ?? string.Empty;
            try
            {
                ReleaseOwnerGraph("Owner deactivated " + canonicalOwnerId + ": " + (reason ?? string.Empty));
            }
            finally
            {
                // The host itself is process-lifetime. These references all belong to its
                // ordinary Mod consumer and must be severed even if Unity cleanup misbehaves.
                ClearOwnerBinding();
            }

            TryLog("Debug console host owner cleanup owner=" + canonicalOwnerId + " reason=" + (reason ?? string.Empty) + ".");
            TrySetOwnerInactiveStatus(canonicalOwnerId);
            return 1;
        }

        private IManifest ClaimOwner(IManifest owner)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.UniqueID))
                throw new ArgumentException("Debug console host requires an owner manifest with a UniqueID.", nameof(owner));
            if (ownerManifest != null && !string.Equals(ownerManifest.UniqueID, owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Debug console host is already bound to owner '" + ownerManifest.UniqueID + "' and can't be replaced by '" + owner.UniqueID + "'.");
            return ownerManifest ?? owner;
        }

        private void EnsureOwnerMatchesIfBound(IManifest owner)
        {
            if (ownerManifest == null)
                return;
            if (owner == null || !string.Equals(ownerManifest.UniqueID, owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Debug console host is bound to owner '" + ownerManifest.UniqueID + "' and can't be controlled by another owner.");
        }

        private bool OwnerMatches(string ownerId)
        {
            return ownerManifest != null &&
                !string.IsNullOrWhiteSpace(ownerId) &&
                string.Equals(ownerManifest.UniqueID, ownerId, StringComparison.OrdinalIgnoreCase);
        }

        private void ReleaseOwnerGraph(string reason)
        {
            IsOpen = false;
            runtime.ModalOpen = false;
            runtime.NativeInputDrainActive = false;
            suppressYCloseUntilReleased = false;
            suppressLegacyYCloseUntilReleased = false;
            suppressEscapeCloseUntilReleased = false;
            escapeCloseDrainCleanFrames = 0;
            ConsumedInputThisFrame = false;

            try
            {
                if (runtime.UI.IsOpen && runtime.UI.ActiveMenuId.Equals(MenuId, StringComparison.OrdinalIgnoreCase))
                    runtime.UI.Close();
            }
            catch
            {
            }

            object? eventSystemToDestroy = eventSystemRoot;
            object? rootToDestroy = root;
            eventSystemRoot = null;
            eventSystemComponent = null;
            root = null;
            panelRoot = null;
            headerRoot = null;
            catalogRoot = null;
            catalogRegionHost = null;
            catalogChromeRoot = null;
            catalogGridRoot = null;
            worldRoot = null;
            worldRegionHost = null;
            worldContentRoot = null;
            advancedRoot = null;
            advancedRegionHost = null;
            advancedContentRoot = null;
            catalogTabBackground = null;
            worldTabBackground = null;
            advancedTabBackground = null;
            hoverTooltipRoot = null;
            hoverTooltipTextObject = null;
            statusTextObject = null;
            eventBinders.Clear();
            inputFields.Clear();
            catalogChromeBinders.Clear();
            catalogPoolBinders.Clear();
            worldBinders.Clear();
            advancedBinders.Clear();
            catalogInputFields.Clear();
            activeBinderSink = null;
            activeInputSink = null;
            catalogCells.Clear();
            itemSpriteCache.Clear();
            unifiedCatalogSources = null;
            layout = null;
            renderedLanguage = string.Empty;
            initialized = false;
            dirtyRegions = UiDirtyRegion.All;
            ResetSaveConfirmation();

            try
            {
                if (eventSystemToDestroy != null && !IsDestroyed(eventSystemToDestroy))
                    Destroy(eventSystemToDestroy);
            }
            catch
            {
            }
            try
            {
                if (rootToDestroy != null && !IsDestroyed(rootToDestroy))
                    Destroy(rootToDestroy);
            }
            catch
            {
            }
            try
            {
                runtime.SetHookStatus("UI.DebugConsoleEventSystem", "released", "DTMAPI-owned EventSystem", reason ?? string.Empty);
            }
            catch
            {
            }
        }

        private void ClearOwnerBinding()
        {
            inventoryApi = null;
            weatherApi = null;
            teleportApi = null;
            timeApi = null;
            movementApi = null;
            instantSaveApi = null;
            advancedApi = null;
            ownerManifest = null;
            activeSaveSlot = null;
            saveSessionObserved = false;
            searchText = string.Empty;
            category = string.Empty;
            sourceFilter = SourceFilterBase;
            statusMessage = string.Empty;
            text = new DebugConsoleText(runtime.Translation);
        }

        private void TryLog(string message)
        {
            try
            {
                runtime.RuntimeMonitor.Log(message);
            }
            catch
            {
            }
        }

        private void TrySetOwnerInactiveStatus(string ownerId)
        {
            try
            {
                runtime.SetHookStatus("UI.DebugConsoleHost", "inactive", "owner lifetime", "Owner " + ownerId + " was deactivated and its host binding was released.");
                runtime.SetHookStatus("UI.DebugConsoleAdvancedHost", "inactive", "owner lifetime", "Owner " + ownerId + " was deactivated and its advanced host binding was released.");
            }
            catch
            {
            }
        }

        private void ResetItemFilters(string reason)
        {
            searchText = string.Empty;
            category = string.Empty;
            sourceFilter = SourceFilterBase;
            statusMessage = string.Empty;
            itemPage = 0;
            sourcePage = 0;
            activeTab = DebugConsoleTab.Catalog;
            unifiedCatalogSources = null;
            dirtyRegions |= UiDirtyRegion.All;
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
            if (ownerManifest == null)
            {
                runtime.ModalOpen = false;
                runtime.NativeInputDrainActive = false;
                suppressLegacyYCloseUntilReleased = false;
                suppressEscapeCloseUntilReleased = false;
                escapeCloseDrainCleanFrames = 0;
                if (HasUnboundOwnerGraph())
                {
                    ReleaseOwnerGraph("unbound owner guard");
                    ClearOwnerBinding();
                }
                return;
            }

            DebugConsoleRawInputMask inputMask =
                DebugConsoleRawInputMask.None;
            if (IsOpen || suppressEscapeCloseUntilReleased)
            {
                inputMask |= DebugConsoleRawInputMask.EscapePressed |
                    DebugConsoleRawInputMask.EscapeDown;
            }
            if (IsOpen)
            {
                inputMask |= DebugConsoleRawInputMask.YPressed |
                    DebugConsoleRawInputMask.YDown;
            }
            else if (suppressYCloseUntilReleased ||
                suppressLegacyYCloseUntilReleased)
            {
                inputMask |= DebugConsoleRawInputMask.YDown;
            }

            DebugConsoleRawInputFrame rawInput =
                DebugConsoleRawInput.Sample(inputMask);
            bool escapePressed = rawInput.EscapePressed;
            bool escapeDown = rawInput.EscapeDown;

            AdvanceEscapeCloseDrain(escapePressed, escapeDown);

            if (suppressLegacyYCloseUntilReleased)
            {
                runtime.Input.Suppress("Y");
                if (!rawInput.YDown)
                    suppressLegacyYCloseUntilReleased = false;
            }

            runtime.ModalOpen = IsOpen;
            runtime.NativeInputDrainActive = suppressEscapeCloseUntilReleased;
            if (IsOpen)
            {
                if (!suppressEscapeCloseUntilReleased && escapePressed)
                {
                    suppressEscapeCloseUntilReleased = true;
                    escapeCloseDrainCleanFrames = 0;
                    runtime.Input.Suppress("Escape");
                    Close(ownerManifest!, "Escape");
                    return;
                }
                if (rawInput.YPressed)
                {
                    if (TryConsumeRawYKeyDown(
                        rawInput.YDown,
                        IsAnyTextInputFocused()))
                    {
                        runtime.Input.Suppress("Y");
                        return;
                    }

                    if (ShouldUseLegacyYCloseFallback() && runtime.TryDispatchLegacyModalButtonPressed(MenuId, ownerManifest!.UniqueID, "Y"))
                    {
                        suppressLegacyYCloseUntilReleased = true;
                        runtime.Input.Suppress("Y");
                        runtime.RuntimeMonitor.Log("Debug console legacy RegisterButton Y close compatibility dispatched owner=" + ownerManifest!.UniqueID + ".");
                        return;
                    }

                    // DebugConsoleMod's owner-bound typed keybind is the sole Y toggle
                    // owner. Leave this edge for SampleDtmInputFrame instead of closing
                    // here and letting the same physical press reopen through Toggle.
                }
                if (suppressYCloseUntilReleased && !rawInput.YDown)
                    suppressYCloseUntilReleased = false;
            }

            ApplyVisibilityState();
        }

        private void ApplyVisibilityState()
        {
            if (!IsOpen)
            {
                SetActive(root, false);
                DestroyOwnedEventSystem("debug console inactive");
                return;
            }

            if (!EnsureInitialized())
                return;

            SetActive(root, true);
            DebugConsoleLayout nextLayout = ReadLayout();
            if (!nextLayout.SameGeometry(layout))
            {
                layout = nextLayout;
                dirtyRegions |= UiDirtyRegion.Layout;
            }
            EnsureEventSystem();

            if (!openLogged)
            {
                openLogged = true;
                runtime.RuntimeMonitor.Log("Debug console Canvas visible as Y-key console.");
            }

            if (!HasVisibleDirtyRegion())
                return;
            if (panelRoot == null ||
                IsDestroyed(panelRoot) ||
                (dirtyRegions & UiDirtyRegion.Layout) != 0)
            {
                Rebuild();
                return;
            }
            RefreshDirtyRegions();
        }

        private void RefreshDirtyRegions()
        {
            UiDirtyRegion pending = dirtyRegions;
            UiDirtyRegion handled = UiDirtyRegion.None;
            bool wide = layout?.Breakpoint == DebugConsoleBreakpoint.Wide;
            if ((pending & UiDirtyRegion.Catalog) != 0 &&
                catalogRoot != null &&
                (wide || activeTab == DebugConsoleTab.Catalog))
            {
                RefreshCatalogRegion();
                handled |= UiDirtyRegion.Catalog;
            }
            bool combinedWideSideDirty =
                wide &&
                (pending & (UiDirtyRegion.World | UiDirtyRegion.Weather |
                    UiDirtyRegion.Teleport | UiDirtyRegion.Advanced)) != 0;
            bool worldTabDirty = !wide &&
                activeTab == DebugConsoleTab.World &&
                (pending & (UiDirtyRegion.World | UiDirtyRegion.Weather |
                    UiDirtyRegion.Teleport)) != 0;
            if ((combinedWideSideDirty || worldTabDirty) &&
                worldRoot != null)
            {
                RefreshWorldRegion();
                handled |= combinedWideSideDirty
                    ? UiDirtyRegion.World | UiDirtyRegion.Weather |
                        UiDirtyRegion.Teleport | UiDirtyRegion.Advanced
                    : UiDirtyRegion.World | UiDirtyRegion.Weather |
                        UiDirtyRegion.Teleport;
            }
            if (!wide &&
                activeTab == DebugConsoleTab.Advanced &&
                (pending & UiDirtyRegion.Advanced) != 0 &&
                advancedRoot != null)
            {
                RefreshAdvancedRegion();
                handled |= UiDirtyRegion.Advanced;
            }
            if ((pending & UiDirtyRegion.Status) != 0 && statusTextObject != null && !IsDestroyed(statusTextObject))
            {
                SetProperty(
                    statusTextObject,
                    "text",
                    FirstText(statusMessage, T("debug.status.ready", "Ready")));
                handled |= UiDirtyRegion.Status;
            }
            dirtyRegions &= ~handled;
        }

        private bool HasVisibleDirtyRegion()
        {
            if (dirtyRegions == UiDirtyRegion.None)
                return false;
            if ((dirtyRegions & UiDirtyRegion.Layout) != 0)
                return true;
            if ((dirtyRegions & UiDirtyRegion.Status) != 0 &&
                statusTextObject != null)
            {
                return true;
            }
            if (layout?.Breakpoint == DebugConsoleBreakpoint.Wide)
            {
                return ((dirtyRegions & UiDirtyRegion.Catalog) != 0 &&
                        catalogRoot != null) ||
                    ((dirtyRegions & (UiDirtyRegion.World |
                        UiDirtyRegion.Weather |
                        UiDirtyRegion.Teleport |
                        UiDirtyRegion.Advanced)) != 0 &&
                        worldRoot != null);
            }
            if (activeTab == DebugConsoleTab.Catalog)
            {
                return (dirtyRegions & UiDirtyRegion.Catalog) != 0 &&
                    catalogRoot != null;
            }
            if (activeTab == DebugConsoleTab.World)
            {
                return (dirtyRegions & (UiDirtyRegion.World |
                    UiDirtyRegion.Weather |
                    UiDirtyRegion.Teleport)) != 0 &&
                    worldRoot != null;
            }
            return (dirtyRegions & UiDirtyRegion.Advanced) != 0 &&
                advancedRoot != null;
        }

        private bool HasUnboundOwnerGraph()
        {
            return IsOpen || initialized || root != null || panelRoot != null || hoverTooltipRoot != null || statusTextObject != null ||
                eventSystemRoot != null || eventSystemComponent != null || eventBinders.Count > 0 || inputFields.Count > 0 ||
                inventoryApi != null || weatherApi != null || teleportApi != null || timeApi != null || movementApi != null ||
                instantSaveApi != null || advancedApi != null;
        }

        private static bool IsYHotkeyReason(string? reason)
        {
            string text = (reason ?? string.Empty).Trim();
            return text.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("hotkey Y", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("input Y", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEscapeCloseReason(string? reason)
        {
            return string.Equals((reason ?? string.Empty).Trim(), "Escape", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReturnedToTitleReason(string? reason)
        {
            return string.Equals((reason ?? string.Empty).Trim(), "ReturnedToTitle", StringComparison.OrdinalIgnoreCase);
        }

        internal bool AdvanceEscapeCloseDrain(bool escapePressed, bool escapeDown)
        {
            if (!suppressEscapeCloseUntilReleased)
            {
                runtime.NativeInputDrainActive = false;
                return false;
            }

            runtime.Input.Suppress("Escape");
            if (escapePressed || escapeDown)
            {
                escapeCloseDrainCleanFrames = 0;
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-escape-close-edge-suppressed",
                    "Debug console ignored a repeated Escape edge from the previous physical close cycle.",
                    LogLevel.Info);
            }
            else
            {
                escapeCloseDrainCleanFrames++;
                if (escapeCloseDrainCleanFrames >= EscapeCloseDrainRequiredCleanFrames)
                {
                    suppressEscapeCloseUntilReleased = false;
                    escapeCloseDrainCleanFrames = 0;
                    runtime.RuntimeMonitor.LogOnce(
                        "debug-console-escape-close-drain-complete",
                        "Debug console completed the Escape close input drain after consecutive clean frames.",
                        LogLevel.Info);
                }
            }

            runtime.NativeInputDrainActive = suppressEscapeCloseUntilReleased;
            return suppressEscapeCloseUntilReleased;
        }

        internal bool ShouldUseLegacyYCloseFallback()
        {
            string ownerId = ownerManifest?.UniqueID ?? string.Empty;
            return ownerId.Length > 0 &&
                runtime.Input.HasOwnerLegacyButtonRegistration(ownerId, "Y") &&
                !runtime.Input.HasOwnerTypedKeybindForButton(ownerId, "Y");
        }

        internal bool TryConsumeRawYKeyDown(bool yIsDown, bool textInputFocused)
        {
            if (suppressYCloseUntilReleased)
            {
                if (!yIsDown)
                    suppressYCloseUntilReleased = false;
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-y-open-edge-suppressed",
                    "Debug console ignored opener Y close until the opening key press is released.",
                    LogLevel.Info);
                return true;
            }

            if (textInputFocused)
            {
                RecordFocusedTextInputYGuard();
                return true;
            }

            return false;
        }

        internal bool TryConsumeFocusedTextInputY()
        {
            if (!IsAnyTextInputFocused())
                return false;

            RecordFocusedTextInputYGuard();
            return true;
        }

        private void RecordFocusedTextInputYGuard()
        {
            runtime.RuntimeMonitor.LogOnce(
                "debug-console-y-input-focus",
                "Debug console left Y with the focused text input instead of toggling the console.",
                LogLevel.Info);
        }

        private bool EnsureInitialized()
        {
            if (initialized && root != null && !IsDestroyed(root))
                return true;

            ResolveTypes();
            if (gameObjectType == null || rectTransformType == null || objectType == null || canvasType == null ||
                canvasScalerType == null || graphicRaycasterType == null || imageType == null || buttonType == null ||
                textType == null || fontType == null || resourcesType == null || colorType == null || vector2Type == null ||
                eventTriggerType == null || eventTriggerEntryType == null || eventTriggerTypeEnum == null ||
                baseEventDataType == null || unityActionBaseEventDataType == null || pointerInvokeClosedMethod == null)
                return false;

            root = CreateUiObject("DTMAPI.DebugConsole.Canvas", null);
            object canvas = AddComponent(root, canvasType);
            SetEnumProperty(canvas, "renderMode", 0);
            SetProperty(canvas, "sortingOrder", 1200);
            object scaler = AddComponent(root, canvasScalerType);
            SetEnumProperty(scaler, "uiScaleMode", 1);
            SetProperty(
                scaler,
                "referenceResolution",
                Vector2(
                    (float)DebugConsoleLayout.ReferenceWidth,
                    (float)DebugConsoleLayout.ReferenceHeight));
            SetEnumProperty(scaler, "screenMatchMode", 0);
            SetProperty(
                scaler,
                "matchWidthOrHeight",
                (float)DebugConsoleLayout.ReferenceMatch);
            AddComponent(root, graphicRaycasterType);
            DontDestroyOnLoad(root);
            SetActive(root, false);
            initialized = true;
            dirtyRegions = UiDirtyRegion.All;
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
            tmpInputFieldType ??= Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            scrollRectType ??= Type.GetType("UnityEngine.UI.ScrollRect, UnityEngine.UI");
            rectMask2DType ??= Type.GetType("UnityEngine.UI.RectMask2D, UnityEngine.UI");
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
            if (pointerInvokeClosedMethod == null && baseEventDataType != null)
            {
                pointerInvokeClosedMethod = typeof(PointerActionBinder)
                    .GetMethod(
                        nameof(PointerActionBinder.InvokeTyped),
                        BindingFlags.Instance | BindingFlags.Public)
                    ?.MakeGenericMethod(baseEventDataType);
            }
            screenType ??= Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule") ??
                Type.GetType("UnityEngine.Screen, UnityEngine");
            screenWidthProperty ??= screenType?.GetProperty("width", BindingFlags.Public | BindingFlags.Static);
            screenHeightProperty ??= screenType?.GetProperty("height", BindingFlags.Public | BindingFlags.Static);
            screenSafeAreaProperty ??= screenType?.GetProperty("safeArea", BindingFlags.Public | BindingFlags.Static);
            screenRectType ??= screenSafeAreaProperty?.PropertyType;
            safeAreaXProperty ??= screenRectType?.GetProperty("x", BindingFlags.Public | BindingFlags.Instance);
            safeAreaYProperty ??= screenRectType?.GetProperty("y", BindingFlags.Public | BindingFlags.Instance);
            safeAreaWidthProperty ??= screenRectType?.GetProperty("width", BindingFlags.Public | BindingFlags.Instance);
            safeAreaHeightProperty ??= screenRectType?.GetProperty("height", BindingFlags.Public | BindingFlags.Instance);
            dolocApiType ??= ResolveRuntimeType("DolocAPI");
            getItemSpriteMethod ??= dolocApiType?.GetMethod(
                "GetItemSprite",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            getBuiltinResourceMethod ??= resourcesType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "GetBuiltinResource" &&
                    method.GetParameters().Length == 2);
            ResolveUiReflectionMetadata();
            ResolveTextInputFocusMetadata();
        }

        private void ResolveTextInputFocusMetadata()
        {
            if (textInputFocusMetadataResolved)
                return;

            gameObjectType ??= Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule") ??
                Type.GetType("UnityEngine.GameObject, UnityEngine");
            inputFieldType ??= Type.GetType("UnityEngine.UI.InputField, UnityEngine.UI");
            tmpInputFieldType ??= Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            eventSystemType ??= Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");

            eventSystemCurrentProperty = eventSystemType?.GetProperty(
                "current",
                BindingFlags.Public | BindingFlags.Static);
            eventSystemSelectedGameObjectProperty = eventSystemType?.GetProperty(
                "currentSelectedGameObject",
                BindingFlags.Public | BindingFlags.Instance);
            gameObjectGetComponentByTypeMethod = gameObjectType?.GetMethod(
                "GetComponent",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Type) },
                null);
            inputFieldIsFocusedProperty = inputFieldType?.GetProperty(
                "isFocused",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            tmpInputFieldIsFocusedProperty = tmpInputFieldType?.GetProperty(
                "isFocused",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            textInputFocusMetadataResolved = true;
        }

        private void EnsureEventSystem()
        {
            if (!IsOpen || eventSystemType == null)
                return;

            object? currentEventSystem = GetCurrentEventSystem();
            if (eventSystemRoot != null)
            {
                if (IsDestroyed(eventSystemRoot))
                {
                    eventSystemRoot = null;
                    eventSystemComponent = null;
                }
                else
                {
                    bool currentIsExternal =
                        currentEventSystem != null &&
                        !IsDestroyed(currentEventSystem) &&
                        !IsOwnedEventSystem(currentEventSystem);
                    if (currentIsExternal || FindExternalEventSystem() != null)
                    {
                        DestroyOwnedEventSystem(
                            "native EventSystem detected");
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (currentEventSystem != null &&
                !IsDestroyed(currentEventSystem))
            {
                RecordNativeEventSystemReuse();
                return;
            }

            if (FindObjectOfType(eventSystemType) != null)
            {
                RecordNativeEventSystemReuse();
                return;
            }
            if (DateTimeOffset.UtcNow < nextEventSystemRetryAtUtc)
                return;

            if (!TrySelectEventSystemInputModule(out Type? inputModuleType, out string inputModuleSource))
            {
                if (!eventSystemPendingLogged)
                {
                    eventSystemPendingLogged = true;
                    runtime.RuntimeMonitor.Log("DTMAPI debug console delayed EventSystem creation: " + inputModuleSource + ".", LogLevel.Warn);
                    runtime.SetHookStatus("UI.DebugConsoleEventSystem", "pending", "Unity UI Canvas", inputModuleSource);
                }
                nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(2);
                return;
            }

            if (TryCreateFallbackEventSystem(inputModuleType, inputModuleSource, out Exception rootCause))
                return;

            string message = rootCause.GetType().Name + ": " + rootCause.Message;
            bool inputSystemDeferred = inputSystemUiInputModuleType != null && !IsUnityInputSystemReady();
            if ((inputModuleType == inputSystemUiInputModuleType && IsInputSystemNotInitializedException(rootCause)) ||
                inputSystemDeferred)
            {
                if (inputModuleType != standaloneInputModuleType &&
                    standaloneInputModuleType != null &&
                    TryCreateFallbackEventSystem(standaloneInputModuleType, "StandaloneInputModule; InputSystemUIInputModule failed during startup and is deferred.", out rootCause))
                {
                    runtime.RuntimeMonitor.LogOnce(
                        "debug-console-eventsystem-input-deferred-standalone",
                        "DTMAPI debug console deferred InputSystemUIInputModule after startup failure and used StandaloneInputModule.",
                        LogLevel.Warn);
                    return;
                }

                message = rootCause.GetType().Name + ": " + rootCause.Message;
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-eventsystem-input-pending",
                    "DTMAPI debug console delayed EventSystem creation because Unity Input System is not initialized yet.",
                    LogLevel.Warn);
                runtime.SetHookStatus("UI.DebugConsoleEventSystem", "pending", inputModuleSource, message);
                nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(2);
                return;
            }

            RecordEventSystemFallbackFailure(inputModuleSource, rootCause);
        }

        private object? GetCurrentEventSystem()
        {
            try
            {
                ResolveTextInputFocusMetadata();
                object? current =
                    eventSystemCurrentProperty?.GetValue(null, null);
                return current != null && !IsDestroyed(current)
                    ? current
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private void RecordNativeEventSystemReuse()
        {
            if (!eventSystemPendingLogged)
                return;
            eventSystemPendingLogged = false;
            runtime.SetHookStatus(
                "UI.DebugConsoleEventSystem",
                "verified",
                "Native EventSystem",
                "Reused scene EventSystem for Y-key console input.");
        }

        private bool TryCreateFallbackEventSystem(Type? inputModuleType, string inputModuleSource, out Exception rootCause)
        {
            object? candidateRoot = null;
            try
            {
                if (eventSystemType == null)
                {
                    rootCause = new InvalidOperationException("Unity EventSystem type is unavailable.");
                    return false;
                }

                candidateRoot = CreatePlainObject("DTMAPI.DebugConsole.EventSystem");
                eventSystemComponent = AddComponent(candidateRoot, eventSystemType);
                if (inputModuleType != null)
                    AddComponent(candidateRoot, inputModuleType);
                eventSystemRoot = candidateRoot;
                eventSystemPendingLogged = false;
                eventSystemFailureCount = 0;
                eventSystemFailureFingerprint = string.Empty;
                runtime.RuntimeMonitor.Log("DTMAPI debug console UI created an EventSystem for button input using " + inputModuleSource + ".");
                runtime.SetHookStatus("UI.DebugConsoleEventSystem", "verified", inputModuleSource, "Owned fallback EventSystem created for Y-key console input; it is destroyed when the console closes or a native EventSystem appears.");
                rootCause = new InvalidOperationException("No error.");
                return true;
            }
            catch (Exception ex)
            {
                if (candidateRoot != null && objectType != null && !IsDestroyed(candidateRoot))
                    Destroy(candidateRoot);
                eventSystemRoot = null;
                eventSystemComponent = null;
                rootCause = UnwrapReflectionException(ex);
                return false;
            }
        }

        private void RecordEventSystemFallbackFailure(string inputModuleSource, Exception rootCause)
        {
            string fingerprint = rootCause.GetType().FullName + "|" + rootCause.Message;
            if (!string.Equals(eventSystemFailureFingerprint, fingerprint, StringComparison.Ordinal))
            {
                eventSystemFailureFingerprint = fingerprint;
                eventSystemFailureCount = 0;
            }

            eventSystemFailureCount++;
            string message = rootCause.GetType().Name + ": " + rootCause.Message;
            if (eventSystemFailureCount <= 3)
                runtime.RecordError("DTMAPI.DebugConsole", "Debug console EventSystem fallback creation failed; retrying.", rootCause.ToString());
            else
                runtime.RuntimeMonitor.LogOnce("debug-console-eventsystem-retry-suppressed-" + rootCause.GetType().Name, "DTMAPI debug console suppressed repeated EventSystem fallback failures for " + rootCause.GetType().Name + "; retry remains active.", LogLevel.Warn);

            nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(eventSystemFailureCount <= 3 ? 2 : 10);
            runtime.SetHookStatus("UI.DebugConsoleEventSystem", eventSystemFailureCount <= 3 ? "retrying" : "degraded", inputModuleSource, message + "; retryCount=" + eventSystemFailureCount.ToString(CultureInfo.InvariantCulture));
        }

        private object? FindExternalEventSystem()
        {
            if (eventSystemType == null || objectType == null)
                return null;

            try
            {
                ResolveUiReflectionMetadata();
                if (unityObjectFindObjectsOfTypeMethod == null)
                    return null;
                findObjectsArguments[0] = eventSystemType;
                var found = unityObjectFindObjectsOfTypeMethod.Invoke(
                    null,
                    findObjectsArguments) as Array;
                findObjectsArguments[0] = null;
                if (found == null)
                    return null;

                foreach (object? candidate in found)
                {
                    if (candidate == null || IsDestroyed(candidate))
                        continue;
                    if (IsOwnedEventSystem(candidate))
                        continue;
                    return candidate;
                }
            }
            catch
            {
            }

            return null;
        }

        private bool IsOwnedEventSystem(object candidate)
        {
            if (eventSystemComponent != null && IsSameUnityObject(candidate, eventSystemComponent))
                return true;
            if (eventSystemRoot != null && IsSameUnityObject(candidate, eventSystemRoot))
                return true;
            if (eventSystemRoot == null)
                return false;
            try
            {
                object? gameObject = GetProperty(candidate, "gameObject");
                return gameObject != null && IsSameUnityObject(gameObject, eventSystemRoot);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSameUnityObject(object left, object right)
        {
            if (ReferenceEquals(left, right))
                return true;
            try
            {
                return left.Equals(right);
            }
            catch
            {
                return false;
            }
        }

        private void DestroyOwnedEventSystem(string reason)
        {
            if (eventSystemRoot == null)
                return;

            object? rootToDestroy = eventSystemRoot;
            eventSystemRoot = null;
            eventSystemComponent = null;
            if (rootToDestroy != null && !IsDestroyed(rootToDestroy))
                Destroy(rootToDestroy);
            runtime.SetHookStatus("UI.DebugConsoleEventSystem", "released", "DTMAPI-owned EventSystem", reason ?? string.Empty);
        }

        private bool TrySelectEventSystemInputModule(out Type? inputModuleType, out string source)
        {
            inputModuleType = null;
            source = "No EventSystem input module type is available.";

            if (inputSystemUiInputModuleType != null && IsUnityInputSystemReady())
            {
                inputModuleType = inputSystemUiInputModuleType;
                source = "InputSystemUIInputModule";
                return true;
            }

            if (standaloneInputModuleType != null)
            {
                inputModuleType = standaloneInputModuleType;
                source = inputSystemUiInputModuleType == null
                    ? "StandaloneInputModule"
                    : "StandaloneInputModule; InputSystemUIInputModule is deferred until Unity Input System is initialized.";
                return true;
            }

            if (inputSystemUiInputModuleType != null)
                source = "InputSystemUIInputModule is available but Unity Input System is not initialized yet.";

            return false;
        }

    }
}
