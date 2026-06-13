using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class NativeUiLayoutDiagnosticsService
    {
        private const int MaxLoggedLayoutEvents = 80;
        private readonly DtmApiRuntime runtime;
        private readonly HashSet<string> loggedLayoutKeys = new HashSet<string>(StringComparer.Ordinal);

        public NativeUiLayoutDiagnosticsService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal int NormalizeHomePageTextMenuResetLayoutSize(object textMenu, int requestedCount)
        {
            if (!IsTypeNameContains(textMenu, "HomePageTextMenu"))
                return requestedCount;

            const int ExpectedCount = 1;
            if (requestedCount == ExpectedCount)
                return requestedCount;

            RecordLayoutNormalization(textMenu, "HomePageTextMenu.ResetLayoutSize.Prefix", requestedCount, ExpectedCount, "UI.HomePageTextMenuLayout");
            return ExpectedCount;
        }

        internal int NormalizeMenuUiResetLayoutSize(object menu, int requestedCount)
        {
            if (!IsExactTypeName(menu, "DolocTown.UI.MenuUI"))
                return requestedCount;

            LayoutSlotSummary slots = SummarizeSlots(menu);
            int expectedCount = slots.Visible > 0 ? slots.Visible : ReadOptionalInt(menu, "totalCapacity");
            if (expectedCount < 5 || requestedCount == expectedCount)
                return requestedCount;

            RecordLayoutNormalization(menu, "MenuUI.ResetLayoutSize.Prefix", requestedCount, expectedCount, "UI.MainMenuLayout");
            return expectedCount;
        }

        internal int NormalizeGridLayoutConstraintCount(object layoutGroup, int requestedCount)
        {
            if (layoutGroup == null || requestedCount <= 0)
                return requestedCount;

            if (TryGetActiveHomePageTextMenuForLayoutGroup(layoutGroup, out object? textMenu))
            {
                const int ExpectedCount = 1;
                if (requestedCount == ExpectedCount)
                    return requestedCount;

                RecordLayoutNormalization(textMenu!, "GridLayoutGroup.constraintCount.Prefix", requestedCount, ExpectedCount, "UI.HomePageTextMenuLayout");
                return ExpectedCount;
            }

            if (TryGetActiveMainMenuForLayoutGroup(layoutGroup, out object? menu))
            {
                LayoutSlotSummary slots = SummarizeSlots(menu!);
                int expectedCount = slots.Visible > 0 ? slots.Visible : ReadOptionalInt(menu, "totalCapacity");
                if (expectedCount < 5 || requestedCount == expectedCount)
                    return requestedCount;

                RecordLayoutNormalization(menu!, "GridLayoutGroup.constraintCount.Prefix", requestedCount, expectedCount, "UI.MainMenuLayout");
                return expectedCount;
            }

            return requestedCount;
        }

        internal void RecordHomePageRenderTextMenu(object uiState)
        {
            object? textMenu = uiState == null ? null : ReadMember(uiState, "textMenu");
            RecordLayoutEvent(textMenu ?? uiState, "HomePageUiState.RenderTextMenu", requestedTotal: -1, requestedLine: -1, forceLog: true, includeStack: true);
            RepairHomePageTextMenuLayout(textMenu, "HomePageUiState.RenderTextMenu");
        }

        internal void UpdateActiveMainMenuLayout()
        {
            object? currentState = GetCurrentUiStateObject();
            if (IsTypeNameContains(currentState, "HomePageUiState"))
            {
                object? textMenu = ReadMember(currentState!, "textMenu");
                RecordLayoutEvent(textMenu, "HomePageUiState.Update.TextMenu", requestedTotal: -1, requestedLine: 1, forceLog: false, includeStack: false);
                return;
            }

            if (!IsTypeNameContains(currentState, "MainMenuUiState"))
                return;

            object currentStateObject = currentState!;
            object? panel = ReadMember(currentStateObject, "panel");
            object? menu = panel == null ? null : FindFirstMemberByTypeName(panel, "DolocTown.UI.MenuUI") ?? ReadMember(panel, "menu");
            if (menu == null)
                return;

            LayoutSlotSummary slots = SummarizeSlots(menu);
            int expectedConstraintCount = slots.Visible > 0 ? slots.Visible : ReadOptionalInt(menu, "totalCapacity");
            RecordLayoutEvent(menu, "MainMenuUiState.Update.MenuUI", requestedTotal: -1, requestedLine: expectedConstraintCount, forceLog: false, includeStack: false);
        }

        internal void RecordMainMenuPanelOnStartShow(object panel)
        {
            RecordLayoutEvent(panel, "MainMenuPanel.OnStartShow", requestedTotal: -1, requestedLine: -1, forceLog: true, includeStack: true);

            object? menu = panel == null ? null : FindFirstMemberByTypeName(panel, "DolocTown.UI.MenuUI");
            if (menu != null)
            {
                RecordLayoutEvent(menu, "MainMenuPanel.OnStartShow.MenuUI", requestedTotal: -1, requestedLine: -1, forceLog: true, includeStack: false);
            }
        }

        internal void RecordMenuUiSetCapacity(object menu, int totalCapacity)
        {
            RecordLayoutEvent(menu, "MenuUI.SetCapacity", totalCapacity, requestedLine: -1, forceLog: true, includeStack: true);
        }

        internal void RecordHomePageTextMenuResetLayoutSize(object textMenu, int count)
        {
            RecordLayoutEvent(textMenu, "HomePageTextMenu.ResetLayoutSize", requestedTotal: -1, requestedLine: count, forceLog: true, includeStack: true);
            RepairHomePageTextMenuLayout(textMenu, "HomePageTextMenu.ResetLayoutSize");
        }

        internal void RecordMenuUiResetLayoutSize(object menu, int count)
        {
            bool activeMainMenu = IsCurrentUiStateNameContains("MainMenuUiState");
            RecordLayoutEvent(menu, "MenuUI.ResetLayoutSize", requestedTotal: -1, requestedLine: count, forceLog: activeMainMenu || count == 2, includeStack: true);
        }

        internal void RecordGameDataPanelSetCapacity(object panel, int totalCapacity)
        {
            RecordLayoutEvent(panel, "GameDataPanel.SetCapacity", requestedTotal: totalCapacity, requestedLine: -1, forceLog: true, includeStack: true);
        }

        internal void RecordGridResetLayoutSize(object target, int count)
        {
            bool relevant = count == 2 || IsInterestingMenuTarget(target);
            RecordLayoutEvent(target, "DolocGridUI.ResetLayoutSize", requestedTotal: -1, requestedLine: count, forceLog: relevant, includeStack: relevant);
        }

        internal void RecordGridSetCapacity(object target, int totalCapacity, int lineCapacity)
        {
            bool relevant = lineCapacity == 2 || IsInterestingMenuTarget(target);
            RecordLayoutEvent(target, "DolocGridUI.SetCapacity", totalCapacity, lineCapacity, forceLog: relevant, includeStack: relevant);
        }

        private void RecordLayoutEvent(object? target, string source, int requestedTotal, int requestedLine, bool forceLog, bool includeStack)
        {
            if (target == null)
                return;

            string targetType = target.GetType().FullName ?? target.GetType().Name;
            LayoutSlotSummary slots = SummarizeSlots(target);
            int totalCapacity = ReadOptionalInt(target, "totalCapacity");
            int lineCapacity = ReadOptionalInt(target, "lineCapacity");
            int rowCount = ReadOptionalInt(target, "rowCount");
            object? layoutGroup = ReadMember(target, "slotLayoutGroup");
            int constraintCount = layoutGroup == null ? -1 : ReadOptionalInt(layoutGroup, "constraintCount");
            string constraint = layoutGroup == null ? "missing" : Convert.ToString(ReadMember(layoutGroup, "constraint"), CultureInfo.InvariantCulture) ?? "unknown";
            string currentState = GetCurrentUiStateName();
            bool twoColumn = requestedLine == 2 || lineCapacity == 2 || constraintCount == 2;
            bool interesting = forceLog || twoColumn || IsInterestingMenuTarget(target);
            if (!interesting)
                return;

            string key = source + "|" + targetType + "|" + requestedTotal.ToString(CultureInfo.InvariantCulture) + "|" + requestedLine.ToString(CultureInfo.InvariantCulture) + "|" + slots.Total.ToString(CultureInfo.InvariantCulture) + "|" + slots.Visible.ToString(CultureInfo.InvariantCulture) + "|" + totalCapacity.ToString(CultureInfo.InvariantCulture) + "|" + lineCapacity.ToString(CultureInfo.InvariantCulture) + "|" + constraintCount.ToString(CultureInfo.InvariantCulture);
            if (loggedLayoutKeys.Count >= MaxLoggedLayoutEvents && !twoColumn)
                return;
            if (!loggedLayoutKeys.Add(key))
                return;

            string summary = "source=" + source +
                ", target=" + targetType +
                ", currentState=" + currentState +
                ", runtimeContext=" + runtime.UI.InputContext +
                ", requestedTotal=" + FormatInt(requestedTotal) +
                ", requestedLine=" + FormatInt(requestedLine) +
                ", slots=" + slots.Total.ToString(CultureInfo.InvariantCulture) +
                ", visibleSlots=" + slots.Visible.ToString(CultureInfo.InvariantCulture) +
                ", slotSamples=" + slots.Sample +
                ", totalCapacity=" + FormatInt(totalCapacity) +
                ", lineCapacity=" + FormatInt(lineCapacity) +
                ", rowCount=" + FormatInt(rowCount) +
                ", layoutConstraint=" + constraint +
                ", layoutConstraintCount=" + FormatInt(constraintCount) +
                ", twoColumnObserved=" + twoColumn + ".";

            string stack = includeStack ? " stack=" + FormatStackTrace() : string.Empty;
            runtime.RuntimeMonitor.Log("Native UI layout diagnostic " + summary + stack);
            runtime.SetHookStatus("UI.NativeLayoutDiagnostics", twoColumn ? "needs-review" : "diagnostic", source, summary);
        }

        private void RepairHomePageTextMenuLayout(object? textMenu, string source)
        {
            if (!IsTypeNameContains(textMenu, "HomePageTextMenu"))
                return;
            RepairGridConstraintCount(textMenu!, 1, source, "UI.HomePageTextMenuLayout");
        }

        private void RepairActiveMainMenuLayoutFromMenu(object? menu, string source)
        {
            if (!IsTypeNameContains(menu, "MenuUI") || !IsCurrentUiStateNameContains("MainMenuUiState"))
                return;

            LayoutSlotSummary slots = SummarizeSlots(menu!);
            int expectedConstraintCount = slots.Visible > 0 ? slots.Visible : ReadOptionalInt(menu, "totalCapacity");
            RepairGridConstraintCount(menu!, expectedConstraintCount, source, "UI.MainMenuLayout");
        }

        private bool TryGetActiveHomePageTextMenuForLayoutGroup(object layoutGroup, out object? textMenu)
        {
            textMenu = null;
            object? currentState = GetCurrentUiStateObject();
            if (!IsTypeNameContains(currentState, "HomePageUiState"))
                return false;

            textMenu = ReadMember(currentState!, "textMenu");
            if (!IsTypeNameContains(textMenu, "HomePageTextMenu"))
                return false;

            object? activeLayoutGroup = ReadMember(textMenu!, "slotLayoutGroup");
            return ReferenceEquals(layoutGroup, activeLayoutGroup);
        }

        private bool TryGetActiveMainMenuForLayoutGroup(object layoutGroup, out object? menu)
        {
            menu = null;
            object? currentState = GetCurrentUiStateObject();
            if (!IsTypeNameContains(currentState, "MainMenuUiState"))
                return false;

            object? panel = ReadMember(currentState!, "panel");
            menu = panel == null ? null : FindFirstMemberByTypeName(panel, "DolocTown.UI.MenuUI") ?? ReadMember(panel, "menu");
            if (!IsExactTypeName(menu, "DolocTown.UI.MenuUI"))
                return false;

            object? activeLayoutGroup = ReadMember(menu!, "slotLayoutGroup");
            return ReferenceEquals(layoutGroup, activeLayoutGroup);
        }

        private void RecordLayoutNormalization(object target, string source, int requestedCount, int normalizedCount, string hookId)
        {
            string targetType = target.GetType().FullName ?? target.GetType().Name;
            LayoutSlotSummary slots = SummarizeSlots(target);
            string summary = "source=" + source +
                ", target=" + targetType +
                ", currentState=" + GetCurrentUiStateName() +
                ", runtimeContext=" + runtime.UI.InputContext +
                ", requestedCount=" + requestedCount.ToString(CultureInfo.InvariantCulture) +
                ", normalizedCount=" + normalizedCount.ToString(CultureInfo.InvariantCulture) +
                ", slots=" + slots.Total.ToString(CultureInfo.InvariantCulture) +
                ", visibleSlots=" + slots.Visible.ToString(CultureInfo.InvariantCulture) +
                ", slotSamples=" + slots.Sample + ".";
            runtime.RuntimeMonitor.Log("Native UI layout normalized " + summary);
            runtime.RuntimeMonitor.Log("Native UI layout normalization stack source=" + source + " stack=" + FormatStackTrace());
            runtime.SetHookStatus(hookId, "normalized", source, summary);
            runtime.SetHookStatus("UI.NativeLayoutDiagnostics", "normalized", source, summary);
        }

        private bool RepairGridConstraintCount(object target, int expectedConstraintCount, string source, string hookId)
        {
            if (expectedConstraintCount <= 0)
                return false;

            object? layoutGroup = ReadMember(target, "slotLayoutGroup");
            if (layoutGroup == null)
                return false;

            int before = ReadOptionalInt(layoutGroup, "constraintCount");
            if (before == expectedConstraintCount)
                return false;

            bool fixedColumn = TrySetFixedColumnConstraint(layoutGroup);
            bool countWritten = SetMemberValue(layoutGroup, "constraintCount", expectedConstraintCount);
            TryInvokeNoArg(target, "RebuildLayout");
            bool layoutForced = ForceUnityLayoutRefresh(target);

            int after = ReadOptionalInt(layoutGroup, "constraintCount");
            string targetType = target.GetType().FullName ?? target.GetType().Name;
            string summary = "source=" + source +
                ", target=" + targetType +
                ", currentState=" + GetCurrentUiStateName() +
                ", runtimeContext=" + runtime.UI.InputContext +
                ", expectedConstraintCount=" + expectedConstraintCount.ToString(CultureInfo.InvariantCulture) +
                ", before=" + FormatInt(before) +
                ", after=" + FormatInt(after) +
                ", fixedColumn=" + fixedColumn +
                ", countWritten=" + countWritten +
                ", layoutForced=" + layoutForced + ".";
            runtime.RuntimeMonitor.Log("Native UI layout repair " + summary);
            runtime.SetHookStatus(hookId, after == expectedConstraintCount ? "corrected" : "failed", source, summary);
            runtime.SetHookStatus("UI.NativeLayoutDiagnostics", after == expectedConstraintCount ? "corrected" : "needs-review", source, summary);
            return after == expectedConstraintCount;
        }

        private static bool ForceUnityLayoutRefresh(object target)
        {
            bool refreshed = false;
            try
            {
                object? transform = ReadMember(target, "transform");
                Type? rectTransformType = ResolveType("UnityEngine.RectTransform, UnityEngine.CoreModule");
                Type? layoutRebuilderType = ResolveType("UnityEngine.UI.LayoutRebuilder, UnityEngine.UI");
                if (transform != null && rectTransformType != null && rectTransformType.IsAssignableFrom(transform.GetType()) && layoutRebuilderType != null)
                {
                    MethodInfo? forceRebuild = layoutRebuilderType.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static, null, new[] { rectTransformType }, null);
                    if (forceRebuild != null)
                    {
                        forceRebuild.Invoke(null, new[] { transform });
                        refreshed = true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                Type? canvasType = ResolveType("UnityEngine.Canvas, UnityEngine.UIModule") ?? ResolveType("UnityEngine.Canvas, UnityEngine.CoreModule");
                MethodInfo? forceUpdate = canvasType?.GetMethod("ForceUpdateCanvases", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (forceUpdate != null)
                {
                    forceUpdate.Invoke(null, null);
                    refreshed = true;
                }
            }
            catch
            {
            }

            return refreshed;
        }

        private static bool TrySetFixedColumnConstraint(object layoutGroup)
        {
            try
            {
                object? current = ReadMember(layoutGroup, "constraint");
                Type? constraintType = current?.GetType();
                if (constraintType == null || !constraintType.IsEnum)
                    return false;

                object fixedColumn = Enum.Parse(constraintType, "FixedColumnCount");
                return SetMemberValue(layoutGroup, "constraint", fixedColumn);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeNoArg(object? instance, string methodName)
        {
            if (instance == null)
                return false;
            try
            {
                MethodInfo? method = FindMethodInHierarchy(instance.GetType(), methodName, 0);
                method?.Invoke(instance, null);
                return method != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsInterestingMenuTarget(object? target)
        {
            if (target == null)
                return false;
            string typeName = target.GetType().FullName ?? target.GetType().Name;
            return typeName.IndexOf("HomePageTextMenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("MenuUI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("MainMenuPanel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTypeNameContains(object? target, string fragment)
        {
            if (target == null || string.IsNullOrWhiteSpace(fragment))
                return false;
            string typeName = target.GetType().FullName ?? target.GetType().Name;
            return typeName.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsExactTypeName(object? target, string fullName)
        {
            if (target == null || string.IsNullOrWhiteSpace(fullName))
                return false;
            return string.Equals(target.GetType().FullName, fullName, StringComparison.Ordinal);
        }

        private static LayoutSlotSummary SummarizeSlots(object target)
        {
            int total = 0;
            int visible = 0;
            var samples = new List<string>();
            foreach (object slot in EnumerateObjects(ReadMember(target, "slots")))
            {
                total++;
                bool slotVisible = ReadOptionalBool(slot, "isVisible", ReadOptionalBool(slot, "visible", true));
                if (slotVisible)
                    visible++;
                if (samples.Count < 6)
                    samples.Add(slot.GetType().Name + ":" + (slotVisible ? "visible" : "hidden"));
            }

            return new LayoutSlotSummary(total, visible, samples.Count == 0 ? "none" : string.Join("|", samples.ToArray()));
        }

        private string GetCurrentUiStateName()
        {
            try
            {
                object? currentState = GetCurrentUiStateObject();
                return currentState?.GetType().Name ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private bool IsCurrentUiStateNameContains(string fragment)
        {
            return GetCurrentUiStateName().IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static object? GetCurrentUiStateObject()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            return userInput == null ? null : ReadMember(userInput, "CurrentState");
        }

        private static int ReadOptionalInt(object? instance, string name)
        {
            if (instance == null)
                return -1;
            try
            {
                object? value = ReadMember(instance, name);
                return value == null ? -1 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return -1;
            }
        }

        private static bool ReadOptionalBool(object instance, string name, bool fallback)
        {
            try
            {
                object? value = ReadMember(instance, name);
                return value is bool result ? result : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static object? FindFirstMemberByTypeName(object instance, string fullName)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(Flags))
                {
                    object? value = TryReadField(field, instance);
                    if (IsTypeName(value, fullName))
                        return value;
                }

                foreach (PropertyInfo property in type.GetProperties(Flags))
                {
                    if (property.GetIndexParameters().Length != 0)
                        continue;
                    object? value = TryReadProperty(property, instance);
                    if (IsTypeName(value, fullName))
                        return value;
                }
            }

            return null;
        }

        private static object? TryReadField(FieldInfo field, object instance)
        {
            try
            {
                return field.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static object? TryReadProperty(PropertyInfo property, object instance)
        {
            try
            {
                return property.GetValue(instance, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsTypeName(object? value, string fullName)
        {
            return value != null && string.Equals(value.GetType().FullName, fullName, StringComparison.Ordinal);
        }

        private static string FormatInt(int value)
        {
            return value < 0 ? "unknown" : value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatStackTrace()
        {
            string stack = new StackTrace(2, false).ToString();
            stack = stack.Replace("\r", " ").Replace("\n", " ");
            return stack.Length <= 900 ? stack : stack.Substring(0, 900);
        }

        private sealed class LayoutSlotSummary
        {
            public LayoutSlotSummary(int total, int visible, string sample)
            {
                Total = total;
                Visible = visible;
                Sample = sample;
            }

            public int Total { get; }

            public int Visible { get; }

            public string Sample { get; }
        }
    }
}
