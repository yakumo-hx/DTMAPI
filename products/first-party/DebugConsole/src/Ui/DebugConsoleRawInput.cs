using System;
using System.Reflection;

namespace DTMAPI.DebugConsole
{
    [Flags]
    internal enum DebugConsoleRawInputMask
    {
        None = 0,
        EscapePressed = 1,
        EscapeDown = 2,
        YPressed = 4,
        YDown = 8
    }

    internal readonly struct DebugConsoleRawInputFrame
    {
        internal DebugConsoleRawInputFrame(
            bool escapePressed,
            bool escapeDown,
            bool yPressed,
            bool yDown)
        {
            EscapePressed = escapePressed;
            EscapeDown = escapeDown;
            YPressed = yPressed;
            YDown = yDown;
        }

        internal bool EscapePressed { get; }
        internal bool EscapeDown { get; }
        internal bool YPressed { get; }
        internal bool YDown { get; }
    }

    internal static class DebugConsoleRawInput
    {
        private static readonly Type? LegacyInput =
            ResolveType("UnityEngine.Input, UnityEngine.InputLegacyModule") ??
            ResolveType("UnityEngine.Input, UnityEngine");
        private static readonly Type? LegacyKeyCode =
            ResolveType("UnityEngine.KeyCode, UnityEngine.CoreModule") ??
            ResolveType("UnityEngine.KeyCode, UnityEngine");
        private static readonly MethodInfo? LegacyGetKeyDown =
            ResolveLegacyMethod("GetKeyDown");
        private static readonly MethodInfo? LegacyGetKey =
            ResolveLegacyMethod("GetKey");
        private static readonly object? BoxedYKey = ResolveLegacyKey("Y");
        private static readonly object? BoxedEscapeKey =
            ResolveLegacyKey("Escape");
        private static readonly object?[] LegacyYArguments =
            new object?[] { BoxedYKey };
        private static readonly object?[] LegacyEscapeArguments =
            new object?[] { BoxedEscapeKey };

        private static Type? keyboardType;
        private static PropertyInfo? keyboardCurrentProperty;
        private static PropertyInfo? keyboardYKeyProperty;
        private static PropertyInfo? keyboardEscapeKeyProperty;
        private static PropertyInfo? controlIsPressedProperty;
        private static PropertyInfo? controlWasPressedProperty;

        internal static DebugConsoleRawInputFrame Sample(
            DebugConsoleRawInputMask mask)
        {
            var context = new InputSystemFrameContext();
            bool escapePressed =
                (mask & DebugConsoleRawInputMask.EscapePressed) != 0 &&
                ReadState(
                    LegacyGetKeyDown,
                    LegacyEscapeArguments,
                    isY: false,
                    pressedThisFrame: true,
                    ref context);
            bool escapeDown =
                (mask & DebugConsoleRawInputMask.EscapeDown) != 0 &&
                ReadState(
                    LegacyGetKey,
                    LegacyEscapeArguments,
                    isY: false,
                    pressedThisFrame: false,
                    ref context);
            bool yPressed =
                (mask & DebugConsoleRawInputMask.YPressed) != 0 &&
                ReadState(
                    LegacyGetKeyDown,
                    LegacyYArguments,
                    isY: true,
                    pressedThisFrame: true,
                    ref context);
            bool yDown =
                (mask & DebugConsoleRawInputMask.YDown) != 0 &&
                ReadState(
                    LegacyGetKey,
                    LegacyYArguments,
                    isY: true,
                    pressedThisFrame: false,
                    ref context);

            return new DebugConsoleRawInputFrame(
                escapePressed,
                escapeDown,
                yPressed,
                yDown);
        }

        private static bool ReadState(
            MethodInfo? legacyMethod,
            object?[] legacyArguments,
            bool isY,
            bool pressedThisFrame,
            ref InputSystemFrameContext context)
        {
            if (InvokeLegacy(legacyMethod, legacyArguments))
                return true;
            return ReadInputSystemKey(
                isY,
                pressedThisFrame,
                ref context);
        }

        private static bool InvokeLegacy(
            MethodInfo? method,
            object?[] arguments)
        {
            try
            {
                if (method == null || arguments[0] == null)
                    return false;
                return method.Invoke(null, arguments) is bool result &&
                    result;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadInputSystemKey(
            bool isY,
            bool pressedThisFrame,
            ref InputSystemFrameContext context)
        {
            try
            {
                ResolveInputSystemMetadata();
                object? keyboard = context.GetKeyboard();
                if (keyboard == null)
                    return false;

                object? control = context.GetControl(keyboard, isY);
                if (control == null)
                    return false;

                PropertyInfo? stateProperty = pressedThisFrame
                    ? controlWasPressedProperty
                    : controlIsPressedProperty;
                if (stateProperty == null ||
                    stateProperty.DeclaringType == null ||
                    !stateProperty.DeclaringType.IsAssignableFrom(
                        control.GetType()))
                {
                    stateProperty = control.GetType().GetProperty(
                        pressedThisFrame
                            ? "wasPressedThisFrame"
                            : "isPressed",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (pressedThisFrame)
                        controlWasPressedProperty ??= stateProperty;
                    else
                        controlIsPressedProperty ??= stateProperty;
                }

                return stateProperty?.GetValue(control, null) is bool result &&
                    result;
            }
            catch
            {
                return false;
            }
        }

        private static MethodInfo? ResolveLegacyMethod(string name)
        {
            if (LegacyInput == null || LegacyKeyCode == null)
                return null;
            return LegacyInput.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { LegacyKeyCode },
                null);
        }

        private static object? ResolveLegacyKey(string name)
        {
            if (LegacyKeyCode == null)
                return null;
            try
            {
                return Enum.Parse(
                    LegacyKeyCode,
                    name,
                    ignoreCase: true);
            }
            catch
            {
                return null;
            }
        }

        private static void ResolveInputSystemMetadata()
        {
            keyboardType ??= ResolveType(
                "UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (keyboardType == null)
                return;

            keyboardCurrentProperty ??= keyboardType.GetProperty(
                "current",
                BindingFlags.Public | BindingFlags.Static);
            keyboardYKeyProperty ??= keyboardType.GetProperty(
                "yKey",
                BindingFlags.Public | BindingFlags.Instance);
            keyboardEscapeKeyProperty ??= keyboardType.GetProperty(
                "escapeKey",
                BindingFlags.Public | BindingFlags.Instance);

            Type? buttonControlType = ResolveType(
                "UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
            controlIsPressedProperty ??= buttonControlType?.GetProperty(
                "isPressed",
                BindingFlags.Public | BindingFlags.Instance);
            controlWasPressedProperty ??= buttonControlType?.GetProperty(
                "wasPressedThisFrame",
                BindingFlags.Public | BindingFlags.Instance);
        }

        private static Type? ResolveType(string name)
        {
            return Type.GetType(
                name,
                throwOnError: false,
                ignoreCase: false);
        }

        private struct InputSystemFrameContext
        {
            private bool keyboardRead;
            private object? keyboard;
            private bool yControlRead;
            private object? yControl;
            private bool escapeControlRead;
            private object? escapeControl;

            internal object? GetKeyboard()
            {
                if (!keyboardRead)
                {
                    keyboardRead = true;
                    keyboard = keyboardCurrentProperty?.GetValue(null, null);
                }
                return keyboard;
            }

            internal object? GetControl(object currentKeyboard, bool isY)
            {
                if (isY)
                {
                    if (!yControlRead)
                    {
                        yControlRead = true;
                        yControl = keyboardYKeyProperty?.GetValue(
                            currentKeyboard,
                            null);
                    }
                    return yControl;
                }

                if (!escapeControlRead)
                {
                    escapeControlRead = true;
                    escapeControl = keyboardEscapeKeyProperty?.GetValue(
                        currentKeyboard,
                        null);
                }
                return escapeControl;
            }
        }
    }
}
