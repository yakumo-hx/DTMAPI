using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleUi
    {
        private const string MenuId = "DTMAPI.DebugConsole";
        private const string SourceFilterBase = "__base";
        private const string SourceFilterMods = "__mods";
        private const int EscapeCloseDrainRequiredCleanFrames = 2;
        private static readonly TimeSpan SaveConfirmationWindow =
            TimeSpan.FromSeconds(8);
        private readonly IDebugConsoleRuntime runtime;
        private readonly List<object> eventBinders = new List<object>();
        private readonly List<object> inputFields = new List<object>();
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
        private object? eventSystemComponent;
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
        private string language = string.Empty;

        public DebugConsoleUi(IDebugConsoleRuntime runtime)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            text = new DebugConsoleText(runtime.Translation);
        }

        public bool IsOpen { get; private set; }
        public bool ConsumedInputThisFrame { get; private set; }

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
            return owner + "={Canvas=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) +
                "; EventSystem=" + eventSystems.ToString(CultureInfo.InvariantCulture) +
                "; Button=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; InputField=" + inputFields.Count.ToString(CultureInfo.InvariantCulture) +
                "; ScrollRect=0" +
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
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console host bound owner=" + (owner?.UniqueID ?? "unknown") + " inventory=" + (inventoryApi != null) + " weather=" + (weatherApi != null) + " teleport=" + (teleportApi != null) + " time=" + (timeApi != null) + " movement=" + (movementApi != null) + " instantSave=" + (instantSaveApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleHost", "product-native", "Unity UI Canvas", "In-save Y-key console bound to private inventory/weather/teleport/time/movement/instant-save action ports.");
        }

        public void BindAdvanced(IManifest owner, IAdvancedActions? advancedDebugApi)
        {
            ownerManifest = ClaimOwner(owner);
            advancedApi = advancedDebugApi;
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console host advanced binding owner=" + (owner?.UniqueID ?? "unknown") + " advanced=" + (advancedDebugApi != null) + ".");
            runtime.SetHookStatus("UI.DebugConsoleAdvancedHost", advancedDebugApi != null ? "product-native" : "missing-actions", "Unity UI Canvas + private action allowlist", "Advanced Y-console controls bound=" + (advancedDebugApi != null) + ".");
        }

        public void SetLanguage(IManifest owner, string language)
        {
            ownerManifest = ClaimOwner(owner);
            this.language = language ?? string.Empty;
            text = new DebugConsoleText(runtime.Translation, this.language);
            dirty = true;
            runtime.RuntimeMonitor.Log("Debug console language set owner=" + (owner?.UniqueID ?? ownerManifest?.UniqueID ?? "unknown") + " language=" + text.Language + ".");
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
            dirty = true;
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
                dirty = true;
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
                " modItemsOnly=" + modItemsOnly +
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

                dirty = true;
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
            dirty = true;
            ResetSaveConfirmation();
            suppressYCloseUntilReleased = false;
            suppressEscapeCloseUntilReleased =
                nextInputDrain;
            escapeCloseDrainCleanFrames = 0;
            HideItemTooltip();
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
            hoverTooltipRoot = null;
            statusTextObject = null;
            eventBinders.Clear();
            inputFields.Clear();
            initialized = false;
            dirty = true;
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
            language = string.Empty;
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
            modItemsOnly = false;
            itemPage = 0;
            categoryPage = 0;
            sourcePage = 0;
            weatherPage = 0;
            teleportPage = 0;
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

            bool escapePressed = false;
            bool escapeDown = false;
            if (IsOpen || suppressEscapeCloseUntilReleased)
            {
                escapePressed = DebugConsoleRawInput.GetKeyDown("Escape");
                escapeDown = DebugConsoleRawInput.GetKey("Escape");
            }

            AdvanceEscapeCloseDrain(escapePressed, escapeDown);

            if (suppressLegacyYCloseUntilReleased)
            {
                runtime.Input.Suppress("Y");
                if (!DebugConsoleRawInput.GetKey("Y"))
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
                if (DebugConsoleRawInput.GetKeyDown("Y"))
                {
                    if (TryConsumeRawYKeyDown(DebugConsoleRawInput.GetKey("Y"), IsAnyTextInputFocused()))
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
                if (suppressYCloseUntilReleased && !DebugConsoleRawInput.GetKey("Y"))
                    suppressYCloseUntilReleased = false;
            }

            ApplyVisibilityState();
        }

        private void ApplyVisibilityState()
        {
            if (!EnsureInitialized())
                return;

            SetActive(root, IsOpen);
            if (!IsOpen)
            {
                DestroyOwnedEventSystem("debug console inactive");
                return;
            }
            EnsureEventSystem();

            if (!openLogged)
            {
                openLogged = true;
                runtime.RuntimeMonitor.Log("Debug console Canvas visible as Y-key console.");
            }

            if (dirty)
                Rebuild();
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
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-y-input-focus",
                    "Debug console left Y with the focused text input instead of toggling the console.",
                    LogLevel.Info);
                return true;
            }

            return false;
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
            if (!IsOpen || eventSystemType == null)
                return;
                if (eventSystemRoot != null)
                {
                    if (IsDestroyed(eventSystemRoot))
                    {
                        eventSystemRoot = null;
                        eventSystemComponent = null;
                    }
                    else
                    {
                        if (FindExternalEventSystem() != null)
                            DestroyOwnedEventSystem("native EventSystem detected");
                    else
                        return;
                }
            }

            if (FindObjectOfType(eventSystemType) != null)
            {
                if (eventSystemPendingLogged)
                {
                    eventSystemPendingLogged = false;
                    runtime.SetHookStatus("UI.DebugConsoleEventSystem", "verified", "Native EventSystem", "Reused scene EventSystem for Y-key console input.");
                }
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
                MethodInfo? findObjects = objectType.GetMethod("FindObjectsOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                if (findObjects == null)
                    return null;
                var found = findObjects.Invoke(null, new object[] { eventSystemType }) as Array;
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

            AddText(panelRoot, "DTMAPI.DebugConsole.Title", T("debug.title.y", "Y-Key Console") + "  " + runtime.ApiVersion, 29, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 28, -30, 940, 42);
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
                    string saveLabel = saveConfirmationArmed &&
                        DateTimeOffset.UtcNow <= saveConfirmationExpiresAtUtc
                            ? T("debug.save.confirm", "Confirm save")
                            : T("debug.save.here", "Save here");
                    CreateButton(panelRoot!, "DTMAPI.DebugConsole.Save.Here", saveLabel, SaveHere, save.CanSave ? Color(0.12f, 0.40f, 0.30f, 1f) : Color(0.18f, 0.18f, 0.18f, 1f), save.CanSave ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f), x + 330, -156, 118, 30);
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
            bool rightClickBinding = AddPointerEventListener(go, "PointerDown", eventData =>
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
            RecordRightClickBindingStatus(rightClickBinding);
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
            runtime.SetHookStatus("UI.DebugConsoleItemTooltip", "visible", "Unity UI pointer hover", "item=" + item.Id + ", source=" + FirstText(item.SourceId, item.SourceKind) + ", searchText=" + FormatLifecycleValue(searchText) + ".");
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
                dirty = true;
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
            dirty = true;
        }

        private void ResetSaveConfirmation()
        {
            saveConfirmationArmed = false;
            saveConfirmationExpiresAtUtc = DateTimeOffset.MinValue;
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
            dirty = true;
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
                runtime.RecordError("DTMAPI.DebugConsole", "Debug console item sprite lookup failed for " + itemId + ".", ex.ToString());
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
                if (callback == null)
                    return false;
                MethodInfo? addListener = callback.GetType().GetMethod("AddListener", new[] { unityActionBaseEventDataType });
                if (addListener == null)
                    return false;
                object? triggers = GetProperty(trigger, "triggers");
                MethodInfo? addTrigger = triggers?.GetType().GetMethod("Add");
                if (triggers == null || addTrigger == null)
                    return false;

                var binder = new PointerActionBinder(action);
                Delegate del = CreatePointerActionDelegate(binder);
                addListener.Invoke(callback, new object[] { del });
                addTrigger.Invoke(triggers, new[] { entry });
                eventBinders.Add(binder);
                return true;
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-pointer-event-bind-" + eventName,
                    "Debug console pointer event binding failed for " + eventName + ": " + ex.GetType().Name + ": " + ex.Message,
                    LogLevel.Warn);
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

        private string FormatUnityObjectState(object? value)
        {
            if (value == null)
                return "null";
            if (IsDestroyed(value))
                return "destroyed";

            PropertyInfo? activeSelf = value.GetType().GetProperty("activeSelf", BindingFlags.Public | BindingFlags.Instance);
            if (activeSelf != null && activeSelf.GetValue(value, null) is bool active)
                return active ? "alive-active" : "alive-inactive";
            return "alive";
        }

        private int IsAlive(object? value)
        {
            return value != null && !IsDestroyed(value) ? 1 : 0;
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
        }

        private int CountAliveUnityObjects(IEnumerable<object> values)
        {
            int count = 0;
            foreach (object value in values)
            {
                if (value != null && !IsDestroyed(value))
                    count++;
            }
            return count;
        }

        private static bool IsUnityInputSystemReady()
        {
            Type? inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystemType == null)
                return false;

            try
            {
                PropertyInfo? settings = inputSystemType.GetProperty("settings", BindingFlags.Public | BindingFlags.Static);
                if (settings != null)
                    return settings.GetValue(null, null) != null;

                PropertyInfo? devices = inputSystemType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                if (devices != null)
                {
                    _ = devices.GetValue(null, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (IsInputSystemNotInitializedException(UnwrapReflectionException(ex)))
                    return false;
                return false;
            }

            return true;
        }

        private static bool IsInputSystemNotInitializedException(Exception ex)
        {
            string text = (ex.Message ?? string.Empty) + " " + ex;
            return text.IndexOf("Input System not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("InputSystem not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Exception UnwrapReflectionException(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
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

        private static string BreadcrumbValue(string? value, int max)
        {
            value ??= string.Empty;
            string normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (normalized.Length == 0)
                return "-";
            return normalized.Length <= max ? normalized : normalized.Substring(0, Math.Max(0, max - 14)) + "...[truncated]";
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
