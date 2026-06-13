using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class SaveSlotsService : ISaveSlotsApi
    {
        private const int VanillaArchiveSlotCount = 6;
        private const int FixedExpandedArchiveSlotCount = 12;
        private const int OfficialSaveUiPageSize = 12;
        private const int OfficialSaveUiLineCapacity = 3;
        private static readonly TimeSpan PendingRefreshInterval = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan ConfiguredRefreshInterval = TimeSpan.FromSeconds(3);
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, SaveSlotsOptions> saveSlotOptions = new Dictionary<string, SaveSlotsOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SaveSlotsState> saveSlotStates = new Dictionary<string, SaveSlotsState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, SaveSlotsPanelPagingState> officialSaveUiStates = new Dictionary<object, SaveSlotsPanelPagingState>();
        private DateTimeOffset lastRuntimeRefreshAttemptAtUtc = DateTimeOffset.MinValue;
        private bool nativeArchiveSlotManagerPending;

        public SaveSlotsService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            SaveSlotsOptions normalized = NormalizeSaveSlotsOptions(options);
            saveSlotOptions[owner.UniqueID] = normalized;
            SaveSlotsRegisterResult result = ApplySaveSlotExpansion(owner.UniqueID, "register");
            runtime.RuntimeMonitor.Log("SaveSlots API register success=" + result.Success + " owner=" + owner.UniqueID + " requested=" + result.RequestedSlotCount + " applied=" + result.AppliedSlotCount + " reason=register message=" + result.Message);
            return result;
        }

        SaveSlotsState ISaveSlotsApi.GetState(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            int nativeCount = GetNativeArchiveSlotCount();
            if (saveSlotStates.TryGetValue(ownerId, out SaveSlotsState state))
            {
                state.NativeSlotCount = nativeCount;
                return state;
            }

            return new SaveSlotsState
            {
                OwnerId = ownerId,
                IsConfigured = false,
                Enabled = false,
                NativeSlotCount = nativeCount,
                RequestedSlotCount = VanillaArchiveSlotCount,
                AppliedSlotCount = nativeCount,
                Status = "not-configured",
                LastMessage = "No save-slot expansion policy registered."
            };
        }

        BridgeFeatureStatus ISaveSlotsApi.GetStatus(string uniqueId)
        {
            SaveSlotsState state = ((ISaveSlotsApi)this).GetState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal void RefreshSaveSlotExpansionForRuntime(bool force, string reason)
        {
            if (saveSlotOptions.Count == 0)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!force && !ShouldRefreshForRuntime(now))
                return;

            lastRuntimeRefreshAttemptAtUtc = now;
            int target = ComputeRequestedSaveSlotCount();
            int native = GetNativeArchiveSlotCount();
            if (force || native != target || HasPendingNativeManagerState())
                ApplySaveSlotExpansion(string.Empty, (reason ?? "runtime refresh") + " native=" + native + " target=" + target);
        }

        internal void ApplyOfficialSavePanelPagingFromUiState(object uiState)
        {
            object? panel = uiState == null ? null : ReadMember(uiState, "panel");
            ApplyOfficialSavePanelPaging(panel, null, "GameDataUiState.Show");
        }

        internal void EnsureOfficialSavePanelPageForSelection(object panel, int index)
        {
            ApplyOfficialSavePanelPaging(panel, index, "GameDataPanel.Select");
        }

        private bool ShouldRefreshForRuntime(DateTimeOffset now)
        {
            TimeSpan interval = nativeArchiveSlotManagerPending || HasPendingNativeManagerState()
                ? PendingRefreshInterval
                : ConfiguredRefreshInterval;
            return now - lastRuntimeRefreshAttemptAtUtc >= interval;
        }

        private bool HasPendingNativeManagerState()
        {
            return saveSlotStates.Values.Any(state => state.Status == "pending-native-game-manager");
        }

        private void ApplyOfficialSavePanelPaging(object? panel, int? targetIndex, string reason)
        {
            if (panel == null)
                return;
            if (!IsOfficialGameDataPanel(panel))
            {
                runtime.RuntimeMonitor.LogOnce(
                    "saveslots-ignore-non-game-data-panel",
                    "SaveSlots ignored non-GameDataPanel layout callback target " + (panel.GetType().FullName ?? panel.GetType().Name) + " from " + (reason ?? "unknown") + ".",
                    LogLevel.Warn);
                return;
            }

            object[] slots = EnumerateObjects(ReadMember(panel, "slots")).ToArray();
            if (slots.Length == 0)
                return;

            if (slots.Length <= OfficialSaveUiPageSize)
            {
                RestoreOfficialSavePanel(panel, slots);
                return;
            }

            SaveSlotsPanelPagingState state = GetOfficialSaveUiState(panel);
            int pageCount = Math.Max(1, (int)Math.Ceiling(slots.Length / (double)OfficialSaveUiPageSize));
            if (targetIndex.HasValue)
                state.PageIndex = ClampInt(targetIndex.Value, 0, slots.Length - 1) / OfficialSaveUiPageSize;
            state.PageIndex = ClampInt(state.PageIndex, 0, pageCount - 1);

            int pageStart = state.PageIndex * OfficialSaveUiPageSize;
            int pageEnd = Math.Min(slots.Length, pageStart + OfficialSaveUiPageSize);
            TryResetLayoutSize(panel, OfficialSaveUiLineCapacity);
            for (int i = 0; i < slots.Length; i++)
                SetOfficialSaveSlotVisible(slots[i], i >= pageStart && i < pageEnd);
            ApplyOfficialSaveSlotNavigation(slots, pageStart, pageEnd);
            RecreateOfficialSavePager(panel, state, slots.Length, pageStart, pageEnd, pageCount);
            TryRebuildLayout(panel);

            string summary = string.Format(CultureInfo.InvariantCulture, "officialSaveUiPaging=True page={0}/{1} slots={2}-{3}/{4} reason={5}", state.PageIndex + 1, pageCount, pageStart + 1, pageEnd, slots.Length, reason ?? string.Empty);
            runtime.RuntimeMonitor.LogOnce("saveslots-official-ui-paging", "SaveSlots official GameDataPanel paging is active. " + summary + ".");
            runtime.SetHookStatus("Save.MoreSlotsUiPaging", "experimental", "Harmony: GameDataUiState.Show + GameDataPanel.Select", summary);
        }

        private void RestoreOfficialSavePanel(object panel, object[] slots)
        {
            if (officialSaveUiStates.TryGetValue(panel, out SaveSlotsPanelPagingState? state))
            {
                DestroyUnityObject(state.PagerRoot);
                officialSaveUiStates.Remove(panel);
            }

            int lineCapacity = Math.Max(1, slots.Length / 5 + 1);
            TryResetLayoutSize(panel, lineCapacity);
            foreach (object slot in slots)
                SetOfficialSaveSlotVisible(slot, true);
            TryRebuildLayout(panel);
        }

        private SaveSlotsPanelPagingState GetOfficialSaveUiState(object panel)
        {
            if (!officialSaveUiStates.TryGetValue(panel, out SaveSlotsPanelPagingState? state))
            {
                state = new SaveSlotsPanelPagingState();
                officialSaveUiStates[panel] = state;
            }
            return state;
        }

        private static void SetOfficialSaveSlotVisible(object slot, bool visible)
        {
            try
            {
                SetMemberValue(slot, "visible", visible);
                object? gameObject = ReadMember(slot, "gameObject");
                SetActive(gameObject, visible);
                object? button = ReadMember(slot, "button");
                if (button != null)
                    SetMemberValue(button, "interactable", visible);
            }
            catch
            {
            }
        }

        private static void ApplyOfficialSaveSlotNavigation(object[] slots, int pageStart, int pageEnd)
        {
            Type? navigationType = ResolveType("UnityEngine.UI.Navigation, UnityEngine.UI");
            if (navigationType == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                object? button = ReadMember(slots[i], "button");
                if (button == null)
                    continue;

                bool visible = i >= pageStart && i < pageEnd;
                object navigation = Activator.CreateInstance(navigationType);
                PropertyInfo? modeProperty = navigationType.GetProperty("mode");
                if (modeProperty != null)
                    modeProperty.SetValue(navigation, Enum.ToObject(modeProperty.PropertyType, visible ? 4 : 0), null);

                if (visible)
                {
                    int local = i - pageStart;
                    int up = i - OfficialSaveUiLineCapacity;
                    int down = i + OfficialSaveUiLineCapacity;
                    int left = local % OfficialSaveUiLineCapacity == 0 ? -1 : i - 1;
                    int right = local % OfficialSaveUiLineCapacity == OfficialSaveUiLineCapacity - 1 ? -1 : i + 1;
                    SetNavigationTarget(navigation, "selectOnUp", up >= pageStart ? GetSlotButton(slots[up]) : null);
                    SetNavigationTarget(navigation, "selectOnDown", down < pageEnd ? GetSlotButton(slots[down]) : null);
                    SetNavigationTarget(navigation, "selectOnLeft", left >= pageStart ? GetSlotButton(slots[left]) : null);
                    SetNavigationTarget(navigation, "selectOnRight", right < pageEnd ? GetSlotButton(slots[right]) : null);
                }

                SetMemberValue(button, "navigation", navigation);
            }
        }

        private static object? GetSlotButton(object slot)
        {
            return ReadMember(slot, "button");
        }

        private static void SetNavigationTarget(object navigation, string propertyName, object? target)
        {
            PropertyInfo? property = navigation.GetType().GetProperty(propertyName);
            property?.SetValue(navigation, target, null);
        }

        private void RecreateOfficialSavePager(object panel, SaveSlotsPanelPagingState state, int total, int pageStart, int pageEnd, int pageCount)
        {
            DestroyUnityObject(state.PagerRoot);
            state.EventBinders.Clear();
            object? panelTransform = ReadMember(panel, "transform");
            if (panelTransform == null)
                return;

            object? root = CreateUnityUiObject("DTMAPI.SaveSlots.Pager", panelTransform);
            if (root == null)
                return;
            state.PagerRoot = root;
            SetRect(root, Vector2(1, 1), Vector2(1, 1), Vector2(1, 1), Vector2(-24, -28), Vector2(286, 34));

            string label = string.Format(CultureInfo.InvariantCulture, "Slots {0}-{1}/{2}  Page {3}/{4}", pageStart + 1, pageEnd, total, state.PageIndex + 1, pageCount);
            CreateUnityText(root, "DTMAPI.SaveSlots.Pager.Label", label, 14, Color(0.95f, 0.92f, 0.82f, 1f), Vector2(-286, -2), Vector2(196, 26), TextAnchorMiddleLeft);
            CreateUnityButton(root, "DTMAPI.SaveSlots.Pager.Prev", "<", state.PageIndex <= 0 ? Color(0.18f, 0.18f, 0.18f, 0.85f) : Color(0.30f, 0.24f, 0.16f, 0.95f), Color(1f, 0.94f, 0.82f, 1f), Vector2(-86, -2), Vector2(34, 26), state, () =>
            {
                state.PageIndex = Math.Max(0, state.PageIndex - 1);
                ApplyOfficialSavePanelPaging(panel, state.PageIndex * OfficialSaveUiPageSize, "pager-prev");
                SelectOfficialSaveSlot(panel, state.PageIndex * OfficialSaveUiPageSize);
            });
            CreateUnityButton(root, "DTMAPI.SaveSlots.Pager.Next", ">", state.PageIndex >= pageCount - 1 ? Color(0.18f, 0.18f, 0.18f, 0.85f) : Color(0.30f, 0.24f, 0.16f, 0.95f), Color(1f, 0.94f, 0.82f, 1f), Vector2(-44, -2), Vector2(34, 26), state, () =>
            {
                state.PageIndex = Math.Min(pageCount - 1, state.PageIndex + 1);
                ApplyOfficialSavePanelPaging(panel, state.PageIndex * OfficialSaveUiPageSize, "pager-next");
                SelectOfficialSaveSlot(panel, state.PageIndex * OfficialSaveUiPageSize);
            });
        }

        private static void SelectOfficialSaveSlot(object panel, int index)
        {
            try
            {
                MethodInfo? select = FindMethodInHierarchy(panel.GetType(), "Select", 1);
                select?.Invoke(panel, new object[] { index });
            }
            catch
            {
            }
        }

        private static bool IsOfficialGameDataPanel(object panel)
        {
            return string.Equals(panel.GetType().FullName, "DolocTown.UI.GameDataPanel", StringComparison.Ordinal);
        }

        private static void TryResetLayoutSize(object panel, int lineCapacity)
        {
            try
            {
                MethodInfo? reset = FindMethodInHierarchy(panel.GetType(), "ResetLayoutSize", 1);
                reset?.Invoke(panel, new object[] { Math.Max(1, lineCapacity) });
            }
            catch
            {
            }
        }

        private static void TryRebuildLayout(object panel)
        {
            try
            {
                MethodInfo? rebuild = FindMethodInHierarchy(panel.GetType(), "RebuildLayout", 0);
                rebuild?.Invoke(panel, null);
            }
            catch
            {
            }
        }

        private static object? CreateUnityUiObject(string name, object parentTransform)
        {
            Type? gameObjectType = ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.GameObject, UnityEngine");
            Type? rectTransformType = ResolveType("UnityEngine.RectTransform, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.RectTransform, UnityEngine");
            Type? transformType = ResolveType("UnityEngine.Transform, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Transform, UnityEngine");
            if (gameObjectType == null || rectTransformType == null || transformType == null)
                return null;

            object? go;
            ConstructorInfo? ctor = gameObjectType.GetConstructor(new[] { typeof(string), typeof(Type[]) });
            if (ctor != null)
                go = ctor.Invoke(new object[] { name, new[] { rectTransformType } });
            else
                go = Activator.CreateInstance(gameObjectType, name);
            if (go == null)
                return null;

            object? transform = ReadMember(go, "transform");
            transform?.GetType().GetMethod("SetParent", new[] { transformType, typeof(bool) })?.Invoke(transform, new object[] { parentTransform, false });
            return go;
        }

        private static object? CreateUnityText(object parent, string name, string value, int fontSize, object color, object anchoredPosition, object sizeDelta, int alignment)
        {
            Type? textType = ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
            if (textType == null)
                return null;
            object? parentTransform = ReadMember(parent, "transform");
            if (parentTransform == null)
                return null;
            object? go = CreateUnityUiObject(name, parentTransform);
            if (go == null)
                return null;

            object? text = AddUnityComponent(go, textType);
            if (text == null)
                return null;
            SetMemberValue(text, "text", value ?? string.Empty);
            object? font = GetBuiltinUnityFont();
            if (font != null)
                SetMemberValue(text, "font", font);
            SetMemberValue(text, "fontSize", fontSize);
            SetMemberValue(text, "color", color);
            SetEnumProperty(text, "alignment", alignment);
            SetMemberValue(text, "raycastTarget", false);
            SetRect(go, Vector2(1, 1), Vector2(1, 1), Vector2(1, 1), anchoredPosition, sizeDelta);
            return text;
        }

        private static object? CreateUnityButton(object parent, string name, string label, object background, object textColor, object anchoredPosition, object sizeDelta, SaveSlotsPanelPagingState state, Action onClick)
        {
            Type? imageType = ResolveType("UnityEngine.UI.Image, UnityEngine.UI");
            Type? buttonType = ResolveType("UnityEngine.UI.Button, UnityEngine.UI");
            if (imageType == null || buttonType == null)
                return null;
            object? parentTransform = ReadMember(parent, "transform");
            if (parentTransform == null)
                return null;
            object? go = CreateUnityUiObject(name, parentTransform);
            if (go == null)
                return null;

            object? image = AddUnityComponent(go, imageType);
            if (image != null)
                SetMemberValue(image, "color", background);
            object? button = AddUnityComponent(go, buttonType);
            if (button != null && image != null)
                SetMemberValue(button, "targetGraphic", image);
            if (button != null)
                AddUnityButtonListener(button, state, onClick);
            CreateUnityText(go, name + ".Text", label, 16, textColor, Vector2(0, 0), sizeDelta, TextAnchorMiddleCenter);
            SetRect(go, Vector2(1, 1), Vector2(1, 1), Vector2(1, 1), anchoredPosition, sizeDelta);
            return go;
        }

        private static object? AddUnityComponent(object go, Type componentType)
        {
            Type? gameObjectType = ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.GameObject, UnityEngine");
            return gameObjectType?.GetMethod("AddComponent", new[] { typeof(Type) })?.Invoke(go, new object[] { componentType });
        }

        private static object? GetUnityComponent(object go, Type componentType)
        {
            Type? gameObjectType = ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.GameObject, UnityEngine");
            return gameObjectType?.GetMethod("GetComponent", new[] { typeof(Type) })?.Invoke(go, new object[] { componentType });
        }

        private static void AddUnityButtonListener(object button, SaveSlotsPanelPagingState state, Action action)
        {
            Type? unityActionType = ResolveType("UnityEngine.Events.UnityAction, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Events.UnityAction, UnityEngine");
            object? onClick = ReadMember(button, "onClick");
            if (unityActionType == null || onClick == null)
                return;

            var binder = new SaveSlotsPagerActionBinder(action);
            state.EventBinders.Add(binder);
            Delegate del = Delegate.CreateDelegate(unityActionType, binder, nameof(SaveSlotsPagerActionBinder.Invoke));
            onClick.GetType().GetMethod("AddListener", new[] { unityActionType })?.Invoke(onClick, new object[] { del });
        }

        private static void SetRect(object go, object anchorMin, object anchorMax, object pivot, object anchoredPosition, object sizeDelta)
        {
            Type? rectTransformType = ResolveType("UnityEngine.RectTransform, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.RectTransform, UnityEngine");
            object? rect = rectTransformType == null ? null : GetUnityComponent(go, rectTransformType);
            if (rect == null)
                return;
            SetMemberValue(rect, "anchorMin", anchorMin);
            SetMemberValue(rect, "anchorMax", anchorMax);
            SetMemberValue(rect, "pivot", pivot);
            SetMemberValue(rect, "anchoredPosition", anchoredPosition);
            SetMemberValue(rect, "sizeDelta", sizeDelta);
        }

        private static void SetEnumProperty(object target, string name, int value)
        {
            PropertyInfo? property = target.GetType().GetProperty(name);
            if (property == null)
                return;
            property.SetValue(target, Enum.ToObject(property.PropertyType, value), null);
        }

        private static object? GetBuiltinUnityFont()
        {
            Type? resourcesType = ResolveType("UnityEngine.Resources, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Resources, UnityEngine");
            Type? fontType = ResolveType("UnityEngine.Font, UnityEngine.TextRenderingModule") ?? ResolveType("UnityEngine.Font, UnityEngine");
            MethodInfo? getBuiltin = resourcesType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetBuiltinResource" && m.GetParameters().Length == 2);
            return getBuiltin == null || fontType == null ? null : getBuiltin.Invoke(null, new object[] { fontType, "Arial.ttf" });
        }

        private static void DestroyUnityObject(object? go)
        {
            if (go == null)
                return;
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            objectType?.GetMethod("Destroy", new[] { objectType })?.Invoke(null, new[] { go });
        }

        private static void SetActive(object? go, bool active)
        {
            try
            {
                go?.GetType().GetMethod("SetActive", new[] { typeof(bool) })?.Invoke(go, new object[] { active });
            }
            catch
            {
            }
        }

        private static object Color(float r, float g, float b, float a)
        {
            Type? colorType = ResolveType("UnityEngine.Color, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Color, UnityEngine");
            return Activator.CreateInstance(colorType!, r, g, b, a);
        }

        private static object Vector2(float x, float y)
        {
            Type? vector2Type = ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2, UnityEngine");
            return Activator.CreateInstance(vector2Type!, x, y);
        }

        private SaveSlotsRegisterResult ApplySaveSlotExpansion(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            SaveSlotsOptions ownerOptions = GetSaveSlotsOptions(ownerId);
            int requested = ownerOptions.Enabled ? FixedExpandedArchiveSlotCount : VanillaArchiveSlotCount;
            var result = new SaveSlotsRegisterResult
            {
                OwnerId = ownerId,
                RequestedSlotCount = requested
            };

            if (!TryGetNativeArchiveSlotManager(out object? manager, out string managerMessage))
            {
                nativeArchiveSlotManagerPending = true;
                result.Success = false;
                result.FailureReason = "missing-game-manager";
                result.Message = managerMessage;
                saveSlotStates[ownerId] = BuildSaveSlotsState(ownerId, ownerOptions, 0, 0, "pending-native-game-manager", managerMessage);
                runtime.SetHookStatus("Save.MoreSlotsApi", "pending", "DolocAPI.gameManager.archiveFileCount", managerMessage);
                return result;
            }

            nativeArchiveSlotManagerPending = false;
            if (ownerId.Length == 0)
                saveSlotStates.Remove(string.Empty);

            int previous = ReadIntMember(manager!, "archiveFileCount", VanillaArchiveSlotCount);
            int applied = ComputeRequestedSaveSlotCount();
            bool set = SetMemberValue(manager!, "archiveFileCount", applied);
            int nativeAfter = ReadIntMember(manager!, "archiveFileCount", previous);
            result.PreviousSlotCount = previous;
            result.AppliedSlotCount = nativeAfter;
            result.Success = set && nativeAfter == applied;
            result.FailureReason = result.Success ? string.Empty : "archive-count-set-failed";
            result.Message = result.Success
                ? "Official save slot count set " + previous + "->" + nativeAfter + " through DolocAPI.gameManager.archiveFileCount; enabled requests are normalized to 12 total slots while LocalSave and GameDataPanel keep owning archive files/UI."
                : "Failed to set DolocAPI.gameManager.archiveFileCount to " + applied + "; native count is " + nativeAfter + ".";

            UpdateSaveSlotsStates(nativeAfter, applied, result.Message);
            runtime.SetHookStatus("Save.MoreSlotsApi", result.Success ? "configured-official-archive-count" : "failed", "DolocAPI.gameManager.archiveFileCount -> LocalSave.GetAllArchiveInfo -> GameDataPanel.Render", result.Message + " reason=" + (reason ?? string.Empty) + ".");
            return result;
        }

        private void UpdateSaveSlotsStates(int nativeSlotCount, int appliedSlotCount, string message)
        {
            foreach (KeyValuePair<string, SaveSlotsOptions> entry in saveSlotOptions.ToArray())
            {
                SaveSlotsOptions options = entry.Value ?? new SaveSlotsOptions { Enabled = false, SlotCount = VanillaArchiveSlotCount };
                saveSlotStates[entry.Key] = BuildSaveSlotsState(entry.Key, options, nativeSlotCount, appliedSlotCount, options.Enabled ? "configured-official-archive-count" : "disabled-vanilla-slot-count", message);
            }
        }

        private static SaveSlotsState BuildSaveSlotsState(string ownerId, SaveSlotsOptions options, int nativeSlotCount, int appliedSlotCount, string status, string message)
        {
            return new SaveSlotsState
            {
                OwnerId = ownerId ?? string.Empty,
                IsConfigured = true,
                Enabled = options.Enabled,
                NativeSlotCount = nativeSlotCount,
                RequestedSlotCount = options.Enabled ? FixedExpandedArchiveSlotCount : VanillaArchiveSlotCount,
                AppliedSlotCount = appliedSlotCount,
                Status = status ?? string.Empty,
                LastMessage = message ?? string.Empty
            };
        }

        private SaveSlotsOptions GetSaveSlotsOptions(string ownerId)
        {
            return saveSlotOptions.TryGetValue(ownerId ?? string.Empty, out SaveSlotsOptions? options)
                ? options
                : new SaveSlotsOptions { Enabled = false, SlotCount = VanillaArchiveSlotCount };
        }

        private int ComputeRequestedSaveSlotCount()
        {
            int target = VanillaArchiveSlotCount;
            foreach (SaveSlotsOptions options in saveSlotOptions.Values)
            {
                if (options?.Enabled == true)
                    target = Math.Max(target, FixedExpandedArchiveSlotCount);
            }
            return target;
        }

        private static SaveSlotsOptions NormalizeSaveSlotsOptions(SaveSlotsOptions? options)
        {
            options ??= new SaveSlotsOptions();
            return new SaveSlotsOptions
            {
                Enabled = options.Enabled,
                SlotCount = options.Enabled ? FixedExpandedArchiveSlotCount : VanillaArchiveSlotCount,
                VerboseLogging = options.VerboseLogging
            };
        }

        private static bool TryGetNativeArchiveSlotManager(out object? manager, out string message)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            manager = ReadStaticMember(dolocApi, "gameManager");
            if (manager == null)
            {
                message = "DolocAPI.gameManager is not available yet; save slot expansion will retry during runtime updates.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static int GetNativeArchiveSlotCount()
        {
            return TryGetNativeArchiveSlotManager(out object? manager, out _)
                ? ReadIntMember(manager!, "archiveFileCount", VanillaArchiveSlotCount)
                : 0;
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private const int TextAnchorMiddleLeft = 3;
        private const int TextAnchorMiddleCenter = 4;

        private sealed class SaveSlotsPanelPagingState
        {
            public int PageIndex { get; set; }
            public object? PagerRoot { get; set; }
            public List<object> EventBinders { get; } = new List<object>();
        }

        private sealed class SaveSlotsPagerActionBinder
        {
            private readonly Action action;

            public SaveSlotsPagerActionBinder(Action action)
            {
                this.action = action;
            }

            public void Invoke()
            {
                action();
            }
        }
    }
}
