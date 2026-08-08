using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using DTMAPI.Core.Services;

namespace DTMAPI.BepInExBootstrap
{
    internal static class ReflectedUnityInput
    {
        private static readonly string[] CaptureCandidates = BuildCaptureCandidates();
        private static readonly HashSet<string> win32PreviousDown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Type? inputType;
        private static Type? keyCodeType;
        private static Type? keyboardType;
        private static Type? mouseType;
        private static MethodInfo? getKeyDown;
        private static MethodInfo? getKey;
        private static MethodInfo? getKeyUp;
        private static PropertyInfo? mousePositionProperty;
        private static PropertyInfo? keyboardCurrent;
        private static PropertyInfo? mouseCurrent;
        private static Type? unityTimeType;
        private static PropertyInfo? unityFrameCount;
        private static readonly Dictionary<string, object> cachedLegacyKeyCodes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> cachedLegacyInvalidKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CachedInputSystemControl> cachedInputSystemControls = new Dictionary<string, CachedInputSystemControl>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> cachedInputSystemInvalidKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> cachedVirtualKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> cachedInvalidVirtualKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, LatchedButtonState> latchedButtons = new Dictionary<string, LatchedButtonState>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> staleLatchedButtons = new List<string>();
        private static long latchGeneration;
        private static int lastUnityLatchFrame = -1;
        private static bool cachedLegacyUnavailable;
        private static readonly Dictionary<string, string> KeyboardControlNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Escape", "escapeKey" },
            { "Backspace", "backspaceKey" },
            { "Delete", "deleteKey" },
            { "Space", "spaceKey" },
            { "Tab", "tabKey" },
            { "Return", "enterKey" },
            { "KeypadEnter", "numpadEnterKey" },
            { "LeftShift", "leftShiftKey" },
            { "RightShift", "rightShiftKey" },
            { "LeftControl", "leftCtrlKey" },
            { "RightControl", "rightCtrlKey" },
            { "LeftAlt", "leftAltKey" },
            { "RightAlt", "rightAltKey" },
            { "Insert", "insertKey" },
            { "Home", "homeKey" },
            { "End", "endKey" },
            { "PageUp", "pageUpKey" },
            { "PageDown", "pageDownKey" },
            { "Plus", "equalsKey" },
            { "Equals", "equalsKey" },
            { "Minus", "minusKey" },
            { "KeypadPlus", "numpadPlusKey" },
            { "KeypadMinus", "numpadMinusKey" },
            { "UpArrow", "upArrowKey" },
            { "DownArrow", "downArrowKey" },
            { "LeftArrow", "leftArrowKey" },
            { "RightArrow", "rightArrowKey" }
        };
        private static readonly Dictionary<string, string> MouseControlNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Mouse0", "leftButton" },
            { "Mouse1", "rightButton" },
            { "Mouse2", "middleButton" },
            { "Mouse3", "forwardButton" },
            { "Mouse4", "backButton" }
        };

        public static bool GetKeyDown(string key)
        {
            return InvokeLegacyKeyMethod(ref getKeyDown, "GetKeyDown", key) ||
                InvokeInputSystemButton(key, "wasPressedThisFrame") ||
                InvokeWin32KeyDown(key);
        }

        public static bool GetKey(string key)
        {
            return InvokeLegacyKeyMethod(ref getKey, "GetKey", key) ||
                InvokeInputSystemButton(key, "isPressed") ||
                InvokeWin32Key(key);
        }

        public static bool GetKeyCached(string key)
        {
            return SampleButtonCached(key).IsDownNow;
        }

        public static InputButtonSample SampleButtonCached(string key)
        {
            key = DTMAPI.Abstractions.DtmButton.Normalize(key);
            if (string.IsNullOrWhiteSpace(key))
                return new InputButtonSample(string.Empty, false, false, false);

            if (TryConsumeLatchedSample(key, out InputButtonSample latched))
                return latched;

            return SampleButtonUnlatched(key);
        }

        private static InputButtonSample SampleButtonUnlatched(string key)
        {

            if (key.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return CombineLogicalModifierSampleUnlatched(key, "LeftControl", "RightControl");
            if (key.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return CombineLogicalModifierSampleUnlatched(key, "LeftShift", "RightShift");
            if (key.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return CombineLogicalModifierSampleUnlatched(key, "LeftAlt", "RightAlt");

            return SampleSingleButtonCached(key);
        }

        public static void LatchInputFrame(IReadOnlyList<string> buttons)
        {
            if (!BeginLatchFrame(GetUnityFrameCount()))
                return;
            for (int i = 0; i < buttons.Count; i++)
            {
                string key = DTMAPI.Abstractions.DtmButton.Normalize(buttons[i]);
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                LatchSample(SampleButtonUnlatched(key));
            }
            PruneUnobservedLatchedButtons();
        }

        internal static void LatchInputSamplesForTest(IReadOnlyList<InputButtonSample> samples)
        {
            BeginLatchFrame(-1);
            for (int i = 0; i < samples.Count; i++)
                LatchSample(samples[i]);
            PruneUnobservedLatchedButtons();
        }

        internal static bool LatchInputSamplesForFrameForTest(IReadOnlyList<InputButtonSample> samples, int unityFrame)
        {
            if (!BeginLatchFrame(unityFrame))
                return false;
            for (int i = 0; i < samples.Count; i++)
                LatchSample(samples[i]);
            PruneUnobservedLatchedButtons();
            return true;
        }

        private static bool BeginLatchFrame(int unityFrame)
        {
            if (unityFrame >= 0 && unityFrame == lastUnityLatchFrame)
                return false;
            if (unityFrame >= 0)
                lastUnityLatchFrame = unityFrame;
            latchGeneration++;
            return true;
        }

        public static void DiscardLatchedEdges()
        {
            foreach (LatchedButtonState state in latchedButtons.Values)
            {
                state.PressedEdge = false;
                state.ReleasedEdge = false;
                state.HasSample = false;
            }
        }

        private static void LatchSample(InputButtonSample sample)
        {
            string key = DTMAPI.Abstractions.DtmButton.Normalize(sample.Button);
            if (string.IsNullOrWhiteSpace(key))
                return;
            if (!latchedButtons.TryGetValue(key, out LatchedButtonState state))
            {
                state = new LatchedButtonState();
                latchedButtons[key] = state;
            }

            bool wasDown = state.IsDownNow;
            bool pressed = sample.PressedEdge || (!wasDown && sample.IsDownNow);
            bool released = sample.ReleasedEdge || (wasDown && !sample.IsDownNow);
            if (wasDown && sample.PressedEdge && !sample.ReleasedEdge)
                pressed = false;
            state.IsDownNow = sample.IsDownNow;
            state.PressedEdge |= pressed;
            state.ReleasedEdge |= released;
            state.HasSample = true;
            state.Generation = latchGeneration;
        }

        private static bool TryConsumeLatchedSample(string key, out InputButtonSample sample)
        {
            if (!latchedButtons.TryGetValue(key, out LatchedButtonState state) || !state.HasSample)
            {
                sample = default;
                return false;
            }

            sample = new InputButtonSample(key, state.IsDownNow, state.PressedEdge, state.ReleasedEdge);
            state.PressedEdge = false;
            state.ReleasedEdge = false;
            state.HasSample = false;
            return true;
        }

        private static void PruneUnobservedLatchedButtons()
        {
            staleLatchedButtons.Clear();
            foreach (KeyValuePair<string, LatchedButtonState> pair in latchedButtons)
            {
                if (pair.Value.Generation != latchGeneration)
                    staleLatchedButtons.Add(pair.Key);
            }
            for (int i = 0; i < staleLatchedButtons.Count; i++)
                latchedButtons.Remove(staleLatchedButtons[i]);
        }

        public static void ClearTransientState()
        {
            win32PreviousDown.Clear();
            latchedButtons.Clear();
            staleLatchedButtons.Clear();
            latchGeneration = 0;
            lastUnityLatchFrame = -1;
        }

        private static InputButtonSample CombineLogicalModifierSampleUnlatched(string logicalKey, string leftKey, string rightKey)
        {
            InputButtonSample left = SampleSingleButtonCached(leftKey);
            InputButtonSample right = SampleSingleButtonCached(rightKey);
            bool isDown = left.IsDownNow || right.IsDownNow;
            bool pressed = left.PressedEdge || right.PressedEdge;
            bool released = !isDown && (left.ReleasedEdge || right.ReleasedEdge);
            return new InputButtonSample(logicalKey, isDown, pressed, released);
        }

        private static InputButtonSample SampleSingleButtonCached(string key)
        {
            if (TrySampleInputSystemButtonCached(key, out bool inputDown, out bool inputPressed, out bool inputReleased))
                return new InputButtonSample(key, inputDown, inputPressed, inputReleased);
            if (TrySampleWin32KeyCached(key, out bool win32Down, out bool win32Pressed, out bool win32Released))
                return new InputButtonSample(key, win32Down, win32Pressed, win32Released);
            if (TrySampleLegacyKeyCached(key, out bool legacyDown, out bool legacyPressed, out bool legacyReleased))
                return new InputButtonSample(key, legacyDown, legacyPressed, legacyReleased);
            return new InputButtonSample(key, false, false, false);
        }

        private static bool TrySampleLegacyKeyCached(string key, out bool isDown, out bool pressedEdge, out bool releasedEdge)
        {
            isDown = false;
            pressedEdge = false;
            releasedEdge = false;
            if (cachedLegacyUnavailable || cachedLegacyInvalidKeys.Contains(key))
                return false;
            try
            {
                inputType ??= Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule") ?? Type.GetType("UnityEngine.Input, UnityEngine");
                keyCodeType ??= Type.GetType("UnityEngine.KeyCode, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.KeyCode, UnityEngine");
                if (inputType == null || keyCodeType == null)
                {
                    cachedLegacyUnavailable = true;
                    return false;
                }
                getKey ??= inputType.GetMethod("GetKey", new[] { keyCodeType });
                getKeyDown ??= inputType.GetMethod("GetKeyDown", new[] { keyCodeType });
                getKeyUp ??= inputType.GetMethod("GetKeyUp", new[] { keyCodeType });
                if (getKey == null || getKeyDown == null || getKeyUp == null)
                {
                    cachedLegacyUnavailable = true;
                    return false;
                }
                if (!cachedLegacyKeyCodes.TryGetValue(key, out object keyCode))
                {
                    keyCode = Enum.Parse(keyCodeType, key);
                    cachedLegacyKeyCodes[key] = keyCode;
                }
                pressedEdge = (bool)getKeyDown.Invoke(null, new[] { keyCode });
                isDown = (bool)getKey.Invoke(null, new[] { keyCode });
                releasedEdge = (bool)getKeyUp.Invoke(null, new[] { keyCode });
                return true;
            }
            catch
            {
                cachedLegacyInvalidKeys.Add(key);
                return false;
            }
        }

        private static bool TrySampleInputSystemButtonCached(string key, out bool isDown, out bool pressedEdge, out bool releasedEdge)
        {
            isDown = false;
            pressedEdge = false;
            releasedEdge = false;
            if (cachedInputSystemInvalidKeys.Contains(key))
                return false;
            try
            {
                if (!cachedInputSystemControls.TryGetValue(key, out CachedInputSystemControl cached))
                {
                    object? resolved = ResolveInputSystemControl(key);
                    if (resolved == null)
                    {
                        cachedInputSystemInvalidKeys.Add(key);
                        return false;
                    }
                    cached = new CachedInputSystemControl(resolved);
                    if (cached.IsPressed == null)
                    {
                        cachedInputSystemInvalidKeys.Add(key);
                        return false;
                    }
                    cachedInputSystemControls[key] = cached;
                }

                isDown = cached.IsPressed?.Invoke(cached.Control) == true;
                pressedEdge = cached.WasPressedThisFrame?.Invoke(cached.Control) == true;
                releasedEdge = cached.WasReleasedThisFrame?.Invoke(cached.Control) == true;
                return true;
            }
            catch
            {
                cachedInputSystemInvalidKeys.Add(key);
                return false;
            }
        }

        private static bool TrySampleWin32KeyCached(string key, out bool isDown, out bool pressedEdge, out bool releasedEdge)
        {
            isDown = false;
            pressedEdge = false;
            releasedEdge = false;
            if (cachedInvalidVirtualKeys.Contains(key))
                return false;
            if (!cachedVirtualKeys.TryGetValue(key, out int virtualKey))
            {
                if (!TryGetVirtualKey(key, out virtualKey))
                {
                    cachedInvalidVirtualKeys.Add(key);
                    return false;
                }
                cachedVirtualKeys[key] = virtualKey;
            }
            if (!IsCurrentProcessForeground())
            {
                win32PreviousDown.Remove(key);
                return false;
            }

            short state = GetAsyncKeyState(virtualKey);
            isDown = (state & 0x8000) != 0;
            bool transitioned = (state & 0x0001) != 0;
            bool wasDown = win32PreviousDown.Contains(key);
            pressedEdge = !wasDown && (isDown || transitioned);
            releasedEdge = !isDown && wasDown;
            if (isDown)
                win32PreviousDown.Add(key);
            else
                win32PreviousDown.Remove(key);

            return true;
        }

        public static bool TryGetMousePosition(out double x, out double y)
        {
            x = 0;
            y = 0;
            try
            {
                inputType ??= Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule") ?? Type.GetType("UnityEngine.Input, UnityEngine");
                mousePositionProperty ??= inputType?.GetProperty("mousePosition", BindingFlags.Static | BindingFlags.Public);
                object? legacyPosition = mousePositionProperty?.GetValue(null, null);
                if (legacyPosition != null && TryReadVector2(legacyPosition, out x, out y))
                    return true;
            }
            catch
            {
            }

            try
            {
                mouseType ??= Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
                mouseCurrent ??= mouseType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
                object? mouse = mouseCurrent?.GetValue(null, null);
                object? position = mouse == null ? null : mouseType?.GetProperty("position", BindingFlags.Instance | BindingFlags.Public)?.GetValue(mouse, null);
                MethodInfo? readValue = position?.GetType().GetMethod("ReadValue", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                object? inputSystemPosition = readValue?.Invoke(position, null);
                if (inputSystemPosition != null && TryReadVector2(inputSystemPosition, out x, out y))
                    return true;
            }
            catch
            {
            }

            return false;
        }

        private static bool TryReadVector2(object value, out double x, out double y)
        {
            x = 0;
            y = 0;
            try
            {
                Type type = value.GetType();
                object? xValue = type.GetField("x", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) ??
                    type.GetProperty("x", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value, null);
                object? yValue = type.GetField("y", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) ??
                    type.GetProperty("y", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value, null);
                if (xValue == null || yValue == null)
                    return false;

                x = Convert.ToDouble(xValue);
                y = Convert.ToDouble(yValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeLegacyKeyMethod(ref MethodInfo? method, string methodName, string key)
        {
            try
            {
                inputType ??= Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule") ?? Type.GetType("UnityEngine.Input, UnityEngine");
                keyCodeType ??= Type.GetType("UnityEngine.KeyCode, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.KeyCode, UnityEngine");
                if (inputType == null || keyCodeType == null)
                    return false;
                method ??= inputType.GetMethod(methodName, new[] { keyCodeType });
                if (method == null)
                    return false;
                object keyCode = Enum.Parse(keyCodeType, key);
                return (bool)method.Invoke(null, new[] { keyCode });
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeInputSystemButton(string key, string propertyName)
        {
            try
            {
                object? control = ResolveInputSystemControl(key);
                if (control == null)
                    return false;

                PropertyInfo? property = control.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    return false;

                object? value = property.GetValue(control, null);
                return value is bool isPressed && isPressed;
            }
            catch
            {
                return false;
            }
        }

        private static object? ResolveInputSystemControl(string key)
        {
            string? keyboardControl = MapKeyboardControlName(key);
            if (keyboardControl != null)
            {
                keyboardType ??= Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                keyboardCurrent ??= keyboardType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
                object? keyboard = keyboardCurrent?.GetValue(null, null);
                return keyboard == null ? null : keyboardType?.GetProperty(keyboardControl, BindingFlags.Instance | BindingFlags.Public)?.GetValue(keyboard, null);
            }

            if (MouseControlNames.TryGetValue(key, out string mouseControl))
            {
                mouseType ??= Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
                mouseCurrent ??= mouseType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
                object? mouse = mouseCurrent?.GetValue(null, null);
                return mouse == null ? null : mouseType?.GetProperty(mouseControl, BindingFlags.Instance | BindingFlags.Public)?.GetValue(mouse, null);
            }

            return null;
        }

        private static bool InvokeWin32KeyDown(string key)
        {
            if (!TryGetVirtualKey(key, out int virtualKey))
            {
                win32PreviousDown.Remove(key);
                return false;
            }
            if (!IsCurrentProcessForeground())
            {
                win32PreviousDown.Remove(key);
                return false;
            }

            short state = GetAsyncKeyState(virtualKey);
            bool down = (state & 0x8000) != 0;
            bool transitioned = (state & 0x0001) != 0;
            bool wasDown = win32PreviousDown.Contains(key);
            if (down)
                win32PreviousDown.Add(key);
            else
                win32PreviousDown.Remove(key);
            return !wasDown && (down || transitioned);
        }

        private static bool InvokeWin32Key(string key)
        {
            if (!TryGetVirtualKey(key, out int virtualKey))
                return false;
            if (!IsCurrentProcessForeground())
                return false;
            return IsWin32KeyDown(virtualKey);
        }

        private static bool IsWin32KeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private static bool IsCurrentProcessForeground()
        {
            try
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero)
                    return false;
                GetWindowThreadProcessId(foreground, out int processId);
                return processId == Process.GetCurrentProcess().Id;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetVirtualKey(string key, out int virtualKey)
        {
            virtualKey = 0;
            if (string.IsNullOrWhiteSpace(key) || key.Equals("None", StringComparison.OrdinalIgnoreCase))
                return false;

            if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) && key.Length <= 3 && int.TryParse(key.Substring(1), out int functionKey) && functionKey >= 1 && functionKey <= 24)
            {
                virtualKey = 0x70 + functionKey - 1;
                return true;
            }
            if (key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z')
            {
                virtualKey = key[0];
                return true;
            }
            if (key.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase) && key.Length == 6 && char.IsDigit(key[5]))
            {
                virtualKey = key[5];
                return true;
            }

            switch (key)
            {
                case "Escape": virtualKey = 0x1B; return true;
                case "Backspace": virtualKey = 0x08; return true;
                case "Delete": virtualKey = 0x2E; return true;
                case "Space": virtualKey = 0x20; return true;
                case "Tab": virtualKey = 0x09; return true;
                case "Return": virtualKey = 0x0D; return true;
                case "KeypadEnter": virtualKey = 0x0D; return true;
                case "LeftShift": virtualKey = 0xA0; return true;
                case "RightShift": virtualKey = 0xA1; return true;
                case "LeftControl": virtualKey = 0xA2; return true;
                case "RightControl": virtualKey = 0xA3; return true;
                case "LeftAlt": virtualKey = 0xA4; return true;
                case "RightAlt": virtualKey = 0xA5; return true;
                case "Insert": virtualKey = 0x2D; return true;
                case "Home": virtualKey = 0x24; return true;
                case "End": virtualKey = 0x23; return true;
                case "PageUp": virtualKey = 0x21; return true;
                case "PageDown": virtualKey = 0x22; return true;
                case "Plus": virtualKey = 0xBB; return true;
                case "Equals": virtualKey = 0xBB; return true;
                case "Minus": virtualKey = 0xBD; return true;
                case "KeypadPlus": virtualKey = 0x6B; return true;
                case "KeypadMinus": virtualKey = 0x6D; return true;
                case "UpArrow": virtualKey = 0x26; return true;
                case "DownArrow": virtualKey = 0x28; return true;
                case "LeftArrow": virtualKey = 0x25; return true;
                case "RightArrow": virtualKey = 0x27; return true;
                case "Mouse0": virtualKey = 0x01; return true;
                case "Mouse1": virtualKey = 0x02; return true;
                case "Mouse2": virtualKey = 0x04; return true;
                case "Mouse3": virtualKey = 0x05; return true;
                case "Mouse4": virtualKey = 0x06; return true;
                default: return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private static string? MapKeyboardControlName(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Equals("None", StringComparison.OrdinalIgnoreCase))
                return null;
            if (KeyboardControlNames.TryGetValue(key, out string control))
                return control;
            if (key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z')
                return char.ToLowerInvariant(key[0]) + "Key";
            if (key.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase) && key.Length == 6 && char.IsDigit(key[5]))
                return "digit" + key[5] + "Key";
            if (key.StartsWith("Keypad", StringComparison.OrdinalIgnoreCase) && key.Length == 7 && char.IsDigit(key[6]))
                return "numpad" + key[6] + "Key";
            if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) && key.Length <= 3 && int.TryParse(key.Substring(1), out int functionKey) && functionKey >= 1 && functionKey <= 15)
                return "f" + functionKey + "Key";
            return null;
        }

        private sealed class CachedInputSystemControl
        {
            public CachedInputSystemControl(object control)
            {
                Control = control;
                Type type = control.GetType();
                IsPressed = CompileBooleanGetter(type, "isPressed");
                WasPressedThisFrame = CompileBooleanGetter(type, "wasPressedThisFrame");
                WasReleasedThisFrame = CompileBooleanGetter(type, "wasReleasedThisFrame");
            }

            public object Control { get; }
            public Func<object, bool>? IsPressed { get; }
            public Func<object, bool>? WasPressedThisFrame { get; }
            public Func<object, bool>? WasReleasedThisFrame { get; }

            private static Func<object, bool>? CompileBooleanGetter(Type type, string propertyName)
            {
                PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                MethodInfo? getter = property?.GetGetMethod();
                if (property?.PropertyType != typeof(bool) || getter == null)
                    return null;
                ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                UnaryExpression converted = Expression.Convert(instance, type);
                MemberExpression read = Expression.Property(converted, property);
                return Expression.Lambda<Func<object, bool>>(read, instance).Compile();
            }
        }

        private sealed class LatchedButtonState
        {
            public bool IsDownNow;
            public bool PressedEdge;
            public bool ReleasedEdge;
            public bool HasSample;
            public long Generation;
        }

        public static bool TryGetPressedKey(out string key)
        {
            if (GetKeyDown("Escape") || GetKeyDown("Backspace") || GetKeyDown("Delete"))
            {
                key = "None";
                return true;
            }

            foreach (string candidate in CaptureCandidates)
            {
                if (GetKeyDown(candidate))
                {
                    key = candidate;
                    return true;
                }
            }

            key = string.Empty;
            return false;
        }

        public static int BeginKeyCapture()
        {
            bool foreground = IsCurrentProcessForeground();
            PrimeWin32Transition("Escape", foreground);
            PrimeWin32Transition("Backspace", foreground);
            PrimeWin32Transition("Delete", foreground);
            foreach (string candidate in CaptureCandidates)
                PrimeWin32Transition(candidate, foreground);
            return GetUnityFrameCount();
        }

        public static int GetUnityFrameCount()
        {
            try
            {
                unityTimeType ??= Type.GetType("UnityEngine.Time, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Time, UnityEngine");
                unityFrameCount ??= unityTimeType?.GetProperty("frameCount", BindingFlags.Public | BindingFlags.Static);
                object? value = unityFrameCount?.GetValue(null, null);
                return value == null ? -1 : Convert.ToInt32(value);
            }
            catch
            {
                return -1;
            }
        }

        private static void PrimeWin32Transition(string key, bool foreground)
        {
            if (!foreground || !TryGetVirtualKey(key, out int virtualKey))
            {
                win32PreviousDown.Remove(key);
                return;
            }

            short state = GetAsyncKeyState(virtualKey);
            if ((state & 0x8000) != 0)
                win32PreviousDown.Add(key);
            else
                win32PreviousDown.Remove(key);
        }

        private static string[] BuildCaptureCandidates()
        {
            string[] fixedKeys =
            {
                "Space", "Tab", "Return", "KeypadEnter",
                "LeftShift", "RightShift", "LeftControl", "RightControl", "LeftAlt", "RightAlt",
                "Insert", "Home", "End", "PageUp", "PageDown",
                "UpArrow", "DownArrow", "LeftArrow", "RightArrow",
                "Mouse0", "Mouse1", "Mouse2", "Mouse3", "Mouse4", "Mouse5", "Mouse6"
            };

            var keys = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= 15; i++)
                keys.Add("F" + i);
            for (char c = 'A'; c <= 'Z'; c++)
                keys.Add(c.ToString());
            for (int i = 0; i <= 9; i++)
                keys.Add("Alpha" + i);
            keys.Add("Plus");
            keys.Add("Equals");
            keys.Add("Minus");
            keys.Add("KeypadPlus");
            keys.Add("KeypadMinus");
            keys.AddRange(fixedKeys);
            return keys.ToArray();
        }
    }
}
