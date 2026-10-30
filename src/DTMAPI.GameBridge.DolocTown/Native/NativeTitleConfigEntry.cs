using System;
using System.Collections;
using System.Reflection;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown.Native
{
    // Internal adapter for the native title action list; it does not own native callbacks.
    internal sealed class NativeTitleConfigEntry : IDisposable
    {
        private readonly DtmApiRuntime runtime;
        private readonly HarmonyReflectionPatcher patcher;
        private readonly NativeUiLayoutRepairService layout;
        private Func<string>? label;
        private Action? open;
        private FieldInfo? actionsField;
        private FieldInfo? callbackField;
        private FieldInfo? labelField;
        private ConstructorInfo? constructor;
        private MethodInfo? render;
        private object? state;
        private IList? actions;
        private object? owned;
        private bool attempted;
        private bool disposed;
        private bool degraded;
        private static NativeTitleConfigEntry? active;

        internal NativeTitleConfigEntry(DtmApiRuntime runtime, Func<string> label, Action open)
        {
            this.runtime = runtime;
            this.label = label;
            this.open = open;
            layout = new NativeUiLayoutRepairService(runtime);
            patcher = new HarmonyReflectionPatcher(runtime, "dtmapi.title-settings.entry");
        }

        internal void Update()
        {
            if (attempted || disposed) return;
            attempted = true;
            try
            {
                Type? home = Type.GetType("DolocTown.HomePageUiState, Assembly-CSharp");
                Type? action = Type.GetType("DolocTown.ButtonAction, Assembly-CSharp");
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                actionsField = home?.GetField("buttonActions", flags);
                callbackField = action?.GetField("action", flags);
                labelField = action?.GetField("textGetter", flags);
                constructor = action?.GetConstructor(new[] { typeof(int), typeof(Func<string>), typeof(Action) });
                render = home?.GetMethod("RenderTextMenu", flags, null, Type.EmptyTypes, null);
                if (actionsField == null || !typeof(IList).IsAssignableFrom(actionsField.FieldType) ||
                    callbackField?.FieldType != typeof(Action) || labelField?.FieldType != typeof(Func<string>) ||
                    constructor == null || render == null || render.ReturnType != typeof(void))
                    throw new InvalidOperationException("HomePageUiState/ButtonAction shape does not match.");
                active = this;
                bool prefix = patcher.TryPatchPrefix(home!.AssemblyQualifiedName!, "RenderTextMenu", Callback(nameof(BeforeRender)), 0);
                bool postfix = patcher.TryPatchPostfix(home.AssemblyQualifiedName!, "RenderTextMenu", Callback(nameof(AfterRender)), 0);
                bool unregister = patcher.TryPatchPostfix(home.AssemblyQualifiedName!, "Unregister", Callback(nameof(AfterUnregister)), 0);
                if (!prefix || !postfix || !unregister) throw new InvalidOperationException("Native title entry hooks unavailable.");
                runtime.SetHookStatus("UI.NativeTitleConfigEntry", "installed", "HomePageUiState.RenderTextMenu/Unregister", "Owned action before native Quit; native navigation and click dispatch retained.");
                // Bootstrap may first tick after the native title has already rendered.
                object? current = NativeUiLayoutRepairService.GetCurrentUiStateObject();
                if (current != null && home.IsInstanceOfType(current)) render.Invoke(current, null);
            }
            catch (Exception ex)
            {
                ReportDegraded(ex);
                Dispose();
            }
        }

        private static MethodInfo Callback(string name) => typeof(NativeTitleConfigEntry).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
        public static void BeforeRender(object __instance) => active?.EnsureAction(__instance);
        public static void AfterRender(object __instance)
        {
            var owner = active;
            if (owner != null && ReferenceEquals(owner.state, __instance))
                owner.layout.RepairHomePageTextMenu(ReadMember(__instance, "textMenu"), "DTMAPI.NativeTitleEntry.Render");
        }
        public static void AfterUnregister(object __instance)
        {
            var owner = active;
            if (owner != null && ReferenceEquals(owner.state, __instance)) owner.Detach();
        }

        private void EnsureAction(object instance)
        {
            if (disposed) return;
            try
            {
                var list = actionsField!.GetValue(instance) as IList;
                if (list == null || list.IsReadOnly || list.IsFixedSize) throw new InvalidOperationException("Native action list is not mutable.");
                if (!ReferenceEquals(state, instance) || !ReferenceEquals(actions, list))
                {
                    Detach();
                    state = instance;
                    actions = list;
                    owned = constructor!.Invoke(new object[] { -1, new Func<string>(() => label?.Invoke() ?? string.Empty), new Action(Activate) });
                }
                if (!OwnedTitleMenuAction.Ensure(list, owned!, IsNativeQuit, out bool changed))
                    throw new InvalidOperationException("Expected exactly one native Quit action at the end; title list left unchanged.");
                if (changed) runtime.RuntimeMonitor.Log("DTMAPI native title config action inserted before Quit; count=" + list.Count + ".");
            }
            catch (Exception ex) { ReportDegraded(ex); }
        }

        private bool IsNativeQuit(object item)
        {
            if (!(callbackField!.GetValue(item) is Action callback)) return false;
            Delegate[] calls = callback.GetInvocationList();
            MethodInfo method = callback.Method;
            return calls.Length == 1 && callback.Target == null && method.IsStatic && method.Name == "Quit" &&
                method.DeclaringType?.FullName == "UnityEngine.Application" && method.GetParameters().Length == 0 && method.ReturnType == typeof(void);
        }

        private void Activate()
        {
            if (disposed || runtime.UI.IsOpen || !runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase)) return;
            runtime.RuntimeMonitor.Log("DTMAPI native title config action confirmed.");
            open?.Invoke();
        }

        private void Detach()
        {
            if (owned != null)
            {
                if (actions != null) OwnedTitleMenuAction.Remove(actions, owned);
                callbackField?.SetValue(owned, null);
                labelField?.SetValue(owned, null);
            }
            state = null; actions = null; owned = null;
        }

        private void ReportDegraded(Exception ex)
        {
            if (degraded) return;
            degraded = true;
            runtime.SetHookStatus("UI.NativeTitleConfigEntry", "degraded", "Native title action shape", ex.GetType().Name + ": " + ex.Message);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ReferenceEquals(active, this)) active = null;
            patcher.TryUnpatchAllOwnedPatches();
            object? previous = state;
            Detach();
            label = null; open = null;
            try
            {
                if (previous != null && ReferenceEquals(previous, NativeUiLayoutRepairService.GetCurrentUiStateObject())) render?.Invoke(previous, null);
            }
            catch (Exception ex) { ReportDegraded(ex); }
        }
    }

    internal static class OwnedTitleMenuAction
    {
        internal static bool Ensure(IList list, object owned, Func<object, bool> isExit, out bool changed)
        {
            changed = false;
            int exits = 0;
            object? exit = null;
            object? lastOther = null;
            foreach (object item in list)
            {
                if (ReferenceEquals(item, owned)) continue;
                lastOther = item;
                if (isExit(item)) { exits++; exit = item; }
            }
            if (exits != 1 || !ReferenceEquals(exit, lastOther)) return false;
            int copies = 0;
            for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], owned)) copies++;
            if (copies == 1 && list.Count >= 2 && ReferenceEquals(list[list.Count - 2], owned)) return true;
            Remove(list, owned);
            list.Insert(list.Count - 1, owned);
            changed = true;
            return true;
        }

        internal static void Remove(IList list, object owned)
        {
            for (int i = list.Count - 1; i >= 0; i--) if (ReferenceEquals(list[i], owned)) list.RemoveAt(i);
        }
    }
}
