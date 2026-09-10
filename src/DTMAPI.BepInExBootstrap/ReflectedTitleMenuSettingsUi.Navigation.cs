using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DTMAPI.GameBridge.DolocTown.Native;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed partial class ReflectedTitleMenuSettingsUi
    {
        private readonly List<NavigationTarget> navigationTargets = new List<NavigationTarget>();
        private readonly MenuNavigationCycle navigationCycle = new MenuNavigationCycle();
        private NativeMenuInputAdapter? nativeMenuInput;
        private NativeTitleConfigEntry? nativeTitleEntry;
        private string? focusedName;
        private object? navigationEventSystem;
        private bool priorNavigationEvents;
        private object? priorNativeSelection;
        private NavigationTarget? editingTarget;
        private string editingOriginal = string.Empty;
        private bool skipNavigationThisFrame;

        private void TrackNavigation(string name, object go, object control, object image, object background, float x, float y, bool input, Action? action)
        {
            if (!trackingRenderedObjects) return;
            navigationTargets.Add(new NavigationTarget(name, go, control, image, background, x, y, input, action));
        }

        private void TickNavigation()
        {
            nativeMenuInput ??= new NativeMenuInputAdapter(runtime);
            bool open = IsOwnedRuntimeMenuOpen();
            nativeMenuInput.SetModal(open);
            MenuInput sample = nativeMenuInput.Read();
            if (!open)
            {
                navigationCycle.Reset();
                if (!nativeMenuInput.Draining) ReleaseNavigationLease();
                return;
            }
            if (!nativeMenuInput.Ready) return;
            AcquireNavigationLease();
            if (skipNavigationThisFrame || IsCapturingKey)
            {
                navigationCycle.Reset();
                return;
            }

            NavigationTarget? selected = FindFocused();
            object? nativeSelected = navigationEventSystem == null ? null : GetProperty(navigationEventSystem, "currentSelectedGameObject");
            foreach (NavigationTarget target in navigationTargets)
                if (ReferenceEquals(target.Go, nativeSelected) && target.Name != focusedName)
                {
                    SetFocus(target, selectNative: false);
                    selected = target;
                    break;
                }
            if (selected != null && selected.Input && GetProperty(selected.Control, "isFocused") is bool isFocused && isFocused && editingTarget == null)
            {
                editingTarget = selected;
                editingOriginal = Convert.ToString(GetProperty(selected.Control, "text")) ?? string.Empty;
                navigationCycle.Reset();
            }

            int move = navigationCycle.Advance(sample, Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency, out bool confirm, out bool cancel);
            if (editingTarget != null)
            {
                if (cancel || confirm)
                {
                    NavigationTarget editing = editingTarget;
                    editingTarget = null;
                    if (cancel) SetProperty(editing.Control, "text", editingOriginal);
                    editing.Control.GetType().GetMethod("DeactivateInputField", Type.EmptyTypes)?.Invoke(editing.Control, null);
                    SetNativeSelection(null);
                    navigationCycle.Reset();
                }
                return;
            }
            if (cancel)
            {
                CloseMenu();
                nativeMenuInput.SetModal(IsOwnedRuntimeMenuOpen());
                navigationCycle.Reset();
                return;
            }
            if (navigationTargets.Count == 0) return;
            if (selected == null)
            {
                SetFocus(navigationTargets[0]);
                return;
            }
            if (confirm)
            {
                if (selected.Input)
                {
                    editingTarget = selected;
                    editingOriginal = Convert.ToString(GetProperty(selected.Control, "text")) ?? string.Empty;
                    SetNativeSelection(selected.Go);
                    selected.Control.GetType().GetMethod("ActivateInputField", Type.EmptyTypes)?.Invoke(selected.Control, null);
                    navigationCycle.Reset();
                }
                else selected.Action?.Invoke();
                return;
            }
            // Use one input backend: OR-ing legacy/InputSystem/Win32 edges can replay
            // the same physical press when a fallback is first polled on a later frame.
            if (ReflectedUnityInput.SampleButtonCached("Tab").PressedEdge)
            {
                int index = navigationTargets.IndexOf(selected);
                int delta = ReflectedUnityInput.GetKey("LeftShift") || ReflectedUnityInput.GetKey("RightShift") ? -1 : 1;
                SetFocus(navigationTargets[(index + delta + navigationTargets.Count) % navigationTargets.Count]);
            }
            else if (move != 0)
            {
                int next = FindDirectionalTarget(navigationTargets, navigationTargets.IndexOf(selected), move);
                if (next >= 0) SetFocus(navigationTargets[next]);
            }
        }

        private NavigationTarget? FindFocused() => navigationTargets.Find(t => t.Name == focusedName);
        private void RestoreNavigationFocus()
        {
            if (!IsOwnedRuntimeMenuOpen() || navigationTargets.Count == 0) return;
            SetFocus(FindFocused() ?? navigationTargets[0]);
        }
        private void SetFocus(NavigationTarget target, bool selectNative = true)
        {
            NavigationTarget? previous = FindFocused();
            if (previous != null) SetProperty(previous.Image, "color", previous.Background);
            focusedName = target.Name;
            SetProperty(target.Image, "color", Color(0.28f, 0.55f, 0.67f, 1f));
            if (selectNative) SetNativeSelection(target.Input ? null : target.Go);
        }

        private void AcquireNavigationLease()
        {
            object? current = eventSystemType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null);
            if (current == null || ReferenceEquals(current, navigationEventSystem)) return;
            ReleaseNavigationLease();
            navigationEventSystem = current;
            priorNativeSelection = GetProperty(current, "currentSelectedGameObject");
            priorNavigationEvents = GetProperty(current, "sendNavigationEvents") is bool enabled && enabled;
            SetProperty(current, "sendNavigationEvents", false);
        }
        private void SetNativeSelection(object? go)
        {
            navigationEventSystem?.GetType().GetMethod("SetSelectedGameObject", new[] { gameObjectType! })?.Invoke(navigationEventSystem, new[] { go });
        }
        private void ReleaseNavigationLease()
        {
            if (navigationEventSystem == null) return;
            try
            {
                object? selected = GetProperty(navigationEventSystem, "currentSelectedGameObject");
                if (selected == null || (Convert.ToString(GetProperty(selected, "name")) ?? string.Empty).StartsWith("DTMAPI.", StringComparison.Ordinal))
                    SetNativeSelection(priorNativeSelection != null && !IsDestroyed(priorNativeSelection) ? priorNativeSelection : null);
                if (GetProperty(navigationEventSystem, "sendNavigationEvents") is bool value && !value)
                    SetProperty(navigationEventSystem, "sendNavigationEvents", priorNavigationEvents);
            }
            catch { /* The scene may have destroyed its EventSystem before the boundary callback. */ }
            navigationEventSystem = null;
            priorNativeSelection = null;
        }
        private void ResetNavigation(bool shutdown = false)
        {
            editingTarget = null;
            navigationCycle.Reset();
            nativeMenuInput?.SetModal(false);
            ReleaseNavigationLease();
            if (shutdown) { nativeMenuInput?.Dispose(); nativeMenuInput = null; }
        }

        internal static int FindDirectionalTarget(IReadOnlyList<NavigationTarget> targets, int current, int direction)
        {
            if (current < 0 || current >= targets.Count) return -1;
            NavigationTarget from = targets[current];
            int best = -1; double bestScore = double.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                double dx = targets[i].X - from.X, dy = targets[i].Y - from.Y;
                double ahead = direction == 1 ? -dx : direction == 2 ? dx : direction == 3 ? dy : -dy;
                if (ahead < 1) continue;
                double across = direction <= 2 ? Math.Abs(dy) : Math.Abs(dx);
                double score = ahead + across * 4;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        internal sealed class NavigationTarget
        {
            internal NavigationTarget(string name, object go, object control, object image, object background, float x, float y, bool input, Action? action)
            { Name = name; Go = go; Control = control; Image = image; Background = background; X = x; Y = y; Input = input; Action = action; }
            internal readonly string Name;
            internal readonly object Go, Control, Image, Background;
            internal readonly float X, Y;
            internal readonly bool Input;
            internal readonly Action? Action;
        }
    }

    internal sealed class EditRenderBarrier
    {
        internal bool IsPending { get; private set; }
        private bool sawReleasedUpdate;
        internal void Defer() { IsPending = true; sawReleasedUpdate = false; }
        internal void Reset() { IsPending = false; sawReleasedUpdate = false; }
        internal bool Advance(bool pointerHeld)
        {
            if (!IsPending) return true;
            if (pointerHeld) { sawReleasedUpdate = false; return false; }
            // Bootstrap.Update may run before EventSystem.Update on the release frame.
            if (!sawReleasedUpdate) { sawReleasedUpdate = true; return false; }
            Reset();
            return true;
        }
    }

    internal sealed class MenuNavigationCycle
    {
        private bool armed, confirmHeld, cancelHeld;
        private int direction;
        private double nextRepeat;
        internal void Reset() { armed = false; direction = 0; confirmHeld = false; cancelHeld = false; }
        internal int Advance(MenuInput sample, double now, out bool confirm, out bool cancel)
        {
            confirm = cancel = false;
            if (!armed) { if (sample.IsNeutral) armed = true; return 0; }
            confirm = !confirmHeld && (sample.Confirm || sample.ConfirmEdge);
            cancel = !cancelHeld && (sample.Cancel || sample.CancelEdge);
            confirmHeld = sample.Confirm;
            cancelHeld = sample.Cancel;
            double threshold = direction == 0 ? 0.55 : 0.35;
            int next = sample.Left ? 1 : sample.Right ? 2 : sample.Up ? 3 : sample.Down ? 4 :
                Math.Abs(sample.X) >= Math.Abs(sample.Y) && Math.Abs(sample.X) >= threshold ? (sample.X < 0 ? 1 : 2) :
                Math.Abs(sample.Y) >= threshold ? (sample.Y > 0 ? 3 : 4) : 0;
            if (next == 0) { direction = 0; return 0; }
            if (next != direction) { direction = next; nextRepeat = now + 0.35; return next; }
            if (now < nextRepeat) return 0;
            nextRepeat = now + 0.10;
            return next;
        }
    }
}
