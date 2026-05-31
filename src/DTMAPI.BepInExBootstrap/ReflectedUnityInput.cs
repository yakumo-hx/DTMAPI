using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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
        private static PropertyInfo? keyboardCurrent;
        private static PropertyInfo? mouseCurrent;
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
                return value is bool pressed && pressed;
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
            if (!TryGetVirtualKey(key, out int virtualKey) || !IsCurrentProcessForeground())
            {
                win32PreviousDown.Remove(key);
                return false;
            }

            bool down = IsWin32KeyDown(virtualKey);
            bool wasDown = win32PreviousDown.Contains(key);
            if (down)
                win32PreviousDown.Add(key);
            else
                win32PreviousDown.Remove(key);
            return down && !wasDown;
        }

        private static bool InvokeWin32Key(string key)
        {
            return TryGetVirtualKey(key, out int virtualKey) && IsCurrentProcessForeground() && IsWin32KeyDown(virtualKey);
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
                case "UpArrow": virtualKey = 0x26; return true;
                case "DownArrow": virtualKey = 0x28; return true;
                case "LeftArrow": virtualKey = 0x25; return true;
                case "RightArrow": virtualKey = 0x27; return true;
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
            keys.AddRange(fixedKeys);
            return keys.ToArray();
        }
    }
}
