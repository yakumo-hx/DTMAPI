using System;
using System.Reflection;

namespace DTMAPI.DebugConsole
{
    internal static class DebugConsoleRawInput
    {
        private static readonly Type? LegacyInput =
            ResolveType("UnityEngine.Input, UnityEngine.InputLegacyModule") ??
            ResolveType("UnityEngine.Input, UnityEngine");
        private static readonly Type? LegacyKeyCode =
            ResolveType("UnityEngine.KeyCode, UnityEngine.CoreModule") ??
            ResolveType("UnityEngine.KeyCode, UnityEngine");

        internal static bool GetKeyDown(string key)
        {
            return InvokeLegacy("GetKeyDown", key) ||
                ReadInputSystemKey(key, "wasPressedThisFrame");
        }

        internal static bool GetKey(string key)
        {
            return InvokeLegacy("GetKey", key) ||
                ReadInputSystemKey(key, "isPressed");
        }

        private static bool InvokeLegacy(
            string methodName,
            string key)
        {
            try
            {
                if (LegacyInput == null || LegacyKeyCode == null)
                    return false;
                object value = Enum.Parse(
                    LegacyKeyCode,
                    key,
                    ignoreCase: true);
                MethodInfo? method = LegacyInput.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { LegacyKeyCode },
                    null);
                return method?.Invoke(null, new[] { value }) is bool result &&
                    result;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadInputSystemKey(
            string key,
            string property)
        {
            try
            {
                Type? keyboardType = ResolveType(
                    "UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                object? keyboard = keyboardType?
                    .GetProperty(
                        "current",
                        BindingFlags.Public | BindingFlags.Static)?
                    .GetValue(null);
                if (keyboard == null)
                    return false;
                string controlName =
                    key.Equals(
                        "Y",
                        StringComparison.OrdinalIgnoreCase)
                        ? "yKey"
                        : key.Equals(
                            "Escape",
                            StringComparison.OrdinalIgnoreCase)
                            ? "escapeKey"
                            : string.Empty;
                if (controlName.Length == 0)
                    return false;
                object? control = keyboard.GetType()
                    .GetProperty(
                        controlName,
                        BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(keyboard);
                return control?.GetType()
                    .GetProperty(
                        property,
                        BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(control) is bool result &&
                    result;
            }
            catch
            {
                return false;
            }
        }

        private static Type? ResolveType(string name)
        {
            return Type.GetType(
                name,
                throwOnError: false,
                ignoreCase: false);
        }
    }
}
