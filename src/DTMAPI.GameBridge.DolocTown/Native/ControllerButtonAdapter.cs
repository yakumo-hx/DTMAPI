using System;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown.Native
{
    // Internal device vocabulary. Raw JoystickButtonN deliberately uses the legacy backend.
    internal static class ControllerButtonAdapter
    {
        internal static readonly string[] Buttons = {
            "GamepadSouth", "GamepadEast", "GamepadWest", "GamepadNorth",
            "GamepadLeftShoulder", "GamepadRightShoulder", "GamepadLeftStickPress", "GamepadRightStickPress",
            "GamepadStart", "GamepadSelect", "GamepadDpadUp", "GamepadDpadDown", "GamepadDpadLeft", "GamepadDpadRight"
        };
        private static readonly string[] Properties = {
            "buttonSouth", "buttonEast", "buttonWest", "buttonNorth", "leftShoulder", "rightShoulder",
            "leftStickButton", "rightStickButton", "startButton", "selectButton", "up", "down", "left", "right"
        };
        private static Func<object?>? current;
        private static Func<bool>? focused;
        private static Func<object, bool>? added;
        private static readonly Func<bool>?[] down = new Func<bool>?[Buttons.Length];
        private static readonly Func<bool>?[] pressed = new Func<bool>?[Buttons.Length];
        private static readonly Func<bool>?[] released = new Func<bool>?[Buttons.Length];
        private static object? device;
        private static bool initialized;
        private static int frame = -1;
        private static readonly ControllerButtonCycle cycle = new ControllerButtonCycle();
        internal static bool Available { get; private set; }

        internal static void Refresh(int unityFrame)
        {
            if (unityFrame >= 0 && frame == unityFrame) return;
            frame = unityFrame;
            try
            {
                if (!initialized)
                {
                    initialized = true;
                    Type? type = Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem");
                    if (type == null) return;
                    PropertyInfo? property = type.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                    if (property != null) current = Expression.Lambda<Func<object?>>(Expression.Convert(Expression.Property(null, property), typeof(object))).Compile();
                    ParameterExpression p = Expression.Parameter(typeof(object));
                    added = Expression.Lambda<Func<object, bool>>(Expression.Property(Expression.Convert(p, type), "added"), p).Compile();
                    PropertyInfo? focus = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule")?.GetProperty("isFocused");
                    if (focus != null) focused = Expression.Lambda<Func<bool>>(Expression.Property(null, focus)).Compile();
                }
                object? next = current?.Invoke();
                Available = next != null && added?.Invoke(next) == true;
                if (!Available) next = null;
                bool changed = !ReferenceEquals(device, next);
                if (changed)
                {
                    device = next;
                    Array.Clear(down, 0, down.Length); Array.Clear(pressed, 0, pressed.Length); Array.Clear(released, 0, released.Length);
                    if (device != null)
                        for (int i = 0; i < Buttons.Length; i++)
                        {
                            object? parent = i < 10 ? device : device.GetType().GetProperty("dpad")?.GetValue(device, null);
                            object? control = parent?.GetType().GetProperty(Properties[i])?.GetValue(parent, null);
                            if (control == null) continue;
                            down[i] = Bind(control, "isPressed");
                            pressed[i] = Bind(control, "wasPressedThisFrame");
                            released[i] = Bind(control, "wasReleasedThisFrame");
                        }
                }
                uint held = 0, press = 0, release = 0;
                if (Available && focused?.Invoke() == true)
                    for (int i = 0; i < Buttons.Length; i++)
                    {
                        if (down[i]?.Invoke() == true) held |= 1u << i;
                        if (pressed[i]?.Invoke() == true) press |= 1u << i;
                        if (released[i]?.Invoke() == true) release |= 1u << i;
                    }
                cycle.Update(Available && focused?.Invoke() == true, changed, held, press, release);
            }
            catch
            {
                // Missing/changed device shapes are unavailable, never an alternative button mapping.
                Available = false;
                cycle.Update(false, true, 0, 0, 0);
            }
        }

        private static Func<bool> Bind(object control, string property) =>
            Expression.Lambda<Func<bool>>(Expression.Property(Expression.Constant(control), property)).Compile();

        internal static bool TrySample(string key, int unityFrame, out InputButtonSample sample)
        {
            for (int i = 0; i < Buttons.Length; i++)
                if (key.Equals(Buttons[i], StringComparison.OrdinalIgnoreCase))
                {
                    Refresh(unityFrame);
                    uint bit = 1u << i;
                    sample = new InputButtonSample(key, (cycle.Held & bit) != 0, (cycle.Pressed & bit) != 0, (cycle.Released & bit) != 0);
                    return true;
                }
            sample = default;
            return false;
        }

        internal static void Reset()
        {
            cycle.Update(false, true, 0, 0, 0);
            frame = -1;
        }
    }

    internal sealed class ControllerButtonCycle
    {
        private bool armed;
        internal uint Held { get; private set; }
        internal uint Pressed { get; private set; }
        internal uint Released { get; private set; }
        internal void Update(bool available, bool changed, uint held, uint pressed, uint released)
        {
            uint previous = Held;
            if (!available || changed) armed = false;
            if (!armed)
            {
                Held = 0; Pressed = 0; Released = previous;
                if (available && held == 0 && pressed == 0) armed = true;
                return;
            }
            Held = held;
            Pressed = (pressed | held) & ~previous;
            Released = released | (previous & ~held);
        }
    }
}
