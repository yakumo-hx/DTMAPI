using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Production-owned repair for the two native menu layouts DTMAPI extends.
    /// This service is deliberately independent from the optional QA observer.
    /// </summary>
    internal sealed class NativeUiLayoutRepairService
    {
        private readonly DtmApiRuntime runtime;

        internal NativeUiLayoutRepairService(DtmApiRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        internal void RepairHomePageTextMenu(object? textMenu, string source)
        {
            if (IsTypeNameContains(textMenu, "HomePageTextMenu"))
                RepairGridConstraintCount(textMenu!, 1, source, "UI.HomePageTextMenuLayout");
        }

        internal void RepairMainMenu(object? menu, string source)
        {
            if (!IsExactTypeName(menu, "DolocTown.UI.MenuUI") || !GetCurrentUiStateName().Contains("MainMenuUiState"))
                return;

            LayoutSlotSummary slots = SummarizeSlots(menu!);
            int expected = slots.Visible > 0 ? slots.Visible : ReadOptionalInt(menu, "totalCapacity");
            if (expected >= 5)
                RepairGridConstraintCount(menu!, expected, source, "UI.MainMenuLayout");
        }

        internal void UpdateActiveMenuLayout()
        {
            object? state = GetCurrentUiStateObject();
            if (IsTypeNameContains(state, "HomePageUiState"))
            {
                RepairHomePageTextMenu(ReadMember(state!, "textMenu"), "HomePageUiState.Update.TextMenu");
                return;
            }

            if (!IsTypeNameContains(state, "MainMenuUiState"))
                return;
            object? panel = ReadMember(state!, "panel");
            object? menu = panel == null ? null : FindFirstMemberByTypeName(panel, "DolocTown.UI.MenuUI") ?? ReadMember(panel, "menu");
            RepairMainMenu(menu, "MainMenuUiState.Update.MenuUI");
        }

        internal NativeUiLayoutObservation CaptureObservation()
        {
            object? state = GetCurrentUiStateObject();
            object? target = null;
            if (IsTypeNameContains(state, "HomePageUiState"))
                target = ReadMember(state!, "textMenu");
            else if (IsTypeNameContains(state, "MainMenuUiState"))
            {
                object? panel = ReadMember(state!, "panel");
                target = panel == null ? null : FindFirstMemberByTypeName(panel, "DolocTown.UI.MenuUI") ?? ReadMember(panel, "menu");
            }

            LayoutSlotSummary slots = target == null ? new LayoutSlotSummary(0, 0) : SummarizeSlots(target);
            object? layout = target == null ? null : ReadMember(target, "slotLayoutGroup");
            return new NativeUiLayoutObservation(
                GetCurrentUiStateName(),
                runtime.UI.InputContext ?? string.Empty,
                target?.GetType().FullName ?? string.Empty,
                slots.Total,
                slots.Visible,
                ReadOptionalInt(layout, "constraintCount"));
        }

        private bool RepairGridConstraintCount(object target, int expected, string source, string hookId)
        {
            object? layout = ReadMember(target, "slotLayoutGroup");
            if (layout == null || expected <= 0)
                return false;
            int before = ReadOptionalInt(layout, "constraintCount");
            if (before == expected)
                return true;

            bool fixedColumn = TrySetFixedColumnConstraint(layout);
            bool written = SetMemberValue(layout, "constraintCount", expected);
            TryInvokeNoArg(target, "RebuildLayout");
            bool refreshed = ForceUnityLayoutRefresh(target);
            int after = ReadOptionalInt(layout, "constraintCount");
            string details = "source=" + source + "; state=" + GetCurrentUiStateName() + "; before=" + before.ToString(CultureInfo.InvariantCulture) + "; expected=" + expected.ToString(CultureInfo.InvariantCulture) + "; after=" + after.ToString(CultureInfo.InvariantCulture) + "; fixedColumn=" + fixedColumn + "; written=" + written + "; refreshed=" + refreshed + "; productionOwner=true";
            runtime.RuntimeMonitor.Log("Native UI layout repair " + details + ".");
            runtime.SetHookStatus(hookId, after == expected ? "corrected" : "failed", source, details);
            return after == expected;
        }

        private static bool ForceUnityLayoutRefresh(object target)
        {
            bool refreshed = false;
            try
            {
                object? transform = ReadMember(target, "transform");
                Type? rect = ResolveType("UnityEngine.RectTransform, UnityEngine.CoreModule");
                Type? rebuilder = ResolveType("UnityEngine.UI.LayoutRebuilder, UnityEngine.UI");
                MethodInfo? force = rect == null || rebuilder == null ? null : rebuilder.GetMethod("ForceRebuildLayoutImmediate", BindingFlags.Public | BindingFlags.Static, null, new[] { rect }, null);
                if (transform != null && rect != null && rect.IsInstanceOfType(transform) && force != null)
                {
                    force.Invoke(null, new[] { transform });
                    refreshed = true;
                }
            }
            catch { }
            try
            {
                Type? canvas = ResolveType("UnityEngine.Canvas, UnityEngine.UIModule") ?? ResolveType("UnityEngine.Canvas, UnityEngine.CoreModule");
                MethodInfo? force = canvas?.GetMethod("ForceUpdateCanvases", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (force != null)
                {
                    force.Invoke(null, null);
                    refreshed = true;
                }
            }
            catch { }
            return refreshed;
        }

        private static bool TrySetFixedColumnConstraint(object layout)
        {
            try
            {
                Type? type = ReadMember(layout, "constraint")?.GetType();
                return type != null && type.IsEnum && SetMemberValue(layout, "constraint", Enum.Parse(type, "FixedColumnCount"));
            }
            catch { return false; }
        }

        private static bool TryInvokeNoArg(object instance, string name)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(instance.GetType(), name, 0);
                method?.Invoke(instance, null);
                return method != null;
            }
            catch { return false; }
        }

        internal static object? GetCurrentUiStateObject()
        {
            object? input = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "userInput");
            return input == null ? null : ReadMember(input, "CurrentState");
        }

        private static string GetCurrentUiStateName() => GetCurrentUiStateObject()?.GetType().Name ?? "unknown";

        private static bool IsTypeNameContains(object? target, string fragment) =>
            target != null && (target.GetType().FullName ?? target.GetType().Name).IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsExactTypeName(object? target, string name) => target != null && string.Equals(target.GetType().FullName, name, StringComparison.Ordinal);

        private static int ReadOptionalInt(object? target, string name)
        {
            try { return target == null ? -1 : Convert.ToInt32(ReadMember(target, name), CultureInfo.InvariantCulture); }
            catch { return -1; }
        }

        private static LayoutSlotSummary SummarizeSlots(object target)
        {
            int total = 0;
            int visible = 0;
            foreach (object slot in EnumerateObjects(ReadMember(target, "slots")))
            {
                total++;
                object? value = ReadMember(slot, "isVisible") ?? ReadMember(slot, "visible");
                if (!(value is bool flag) || flag)
                    visible++;
            }
            return new LayoutSlotSummary(total, visible);
        }

        private static object? FindFirstMemberByTypeName(object instance, string name)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(Flags))
                {
                    try { object? value = field.GetValue(instance); if (value != null && value.GetType().FullName == name) return value; } catch { }
                }
                foreach (PropertyInfo property in type.GetProperties(Flags))
                {
                    if (property.GetIndexParameters().Length != 0) continue;
                    try { object? value = property.GetValue(instance, null); if (value != null && value.GetType().FullName == name) return value; } catch { }
                }
            }
            return null;
        }

        private sealed class LayoutSlotSummary
        {
            internal LayoutSlotSummary(int total, int visible) { Total = total; Visible = visible; }
            internal int Total { get; }
            internal int Visible { get; }
        }
    }

    internal sealed class NativeUiLayoutObservation
    {
        internal NativeUiLayoutObservation(string state, string inputContext, string targetType, int slots, int visibleSlots, int constraintCount)
        {
            State = state; InputContext = inputContext; TargetType = targetType; Slots = slots; VisibleSlots = visibleSlots; ConstraintCount = constraintCount;
        }
        internal string State { get; }
        internal string InputContext { get; }
        internal string TargetType { get; }
        internal int Slots { get; }
        internal int VisibleSlots { get; }
        internal int ConstraintCount { get; }
    }
}
