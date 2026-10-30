using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Abstractions
{
    public readonly struct DtmButton : IEquatable<DtmButton>
    {
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "+", "Equals" },
            { "Plus", "Equals" },
            { "Equals", "Equals" },
            { "=", "Equals" },
            { "-", "Minus" },
            { "Minus", "Minus" },
            { "Esc", "Escape" },
            { "Escape", "Escape" },
            { "Enter", "Return" },
            { "Return", "Return" },
            { "Ctrl", "Control" },
            { "Control", "Control" },
            { "LeftCtrl", "LeftControl" },
            { "RightCtrl", "RightControl" },
            { "LeftControl", "LeftControl" },
            { "RightControl", "RightControl" },
            { "Alt", "Alt" },
            { "LeftAlt", "LeftAlt" },
            { "RightAlt", "RightAlt" },
            { "Shift", "Shift" },
            { "LeftShift", "LeftShift" },
            { "RightShift", "RightShift" },
            { "NumpadPlus", "KeypadPlus" },
            { "KeypadPlus", "KeypadPlus" },
            { "NumpadMinus", "KeypadMinus" },
            { "KeypadMinus", "KeypadMinus" },
            { "NumpadEnter", "KeypadEnter" },
            { "KeypadEnter", "KeypadEnter" },
            { "Backspace", "Backspace" },
            { "Delete", "Delete" },
            { "Space", "Space" },
            { "Tab", "Tab" },
            { "Insert", "Insert" },
            { "Home", "Home" },
            { "End", "End" },
            { "PageUp", "PageUp" },
            { "PageDown", "PageDown" },
            { "Up", "UpArrow" },
            { "Down", "DownArrow" },
            { "Left", "LeftArrow" },
            { "Right", "RightArrow" },
            { "UpArrow", "UpArrow" },
            { "DownArrow", "DownArrow" },
            { "LeftArrow", "LeftArrow" },
            { "RightArrow", "RightArrow" },
        };

        public DtmButton(string id)
        {
            Id = Normalize(id);
        }

        public string Id { get; }

        public bool IsBound => !string.IsNullOrWhiteSpace(Id);

        public static DtmButton None => new DtmButton(string.Empty);

        public static DtmButton Parse(string? value) => new DtmButton(value ?? string.Empty);

        public static string Normalize(string? value)
        {
            string text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text) ||
                text.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("Unbound", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (text.IndexOf(' ') >= 0)
                text = text.Replace(" ", string.Empty);
            if (Aliases.TryGetValue(text, out string alias))
                return alias;

            if (text.Length == 1)
            {
                char ch = text[0];
                if (ch >= 'a' && ch <= 'z')
                    return char.ToUpperInvariant(ch).ToString();
                if (ch >= 'A' && ch <= 'Z')
                    return text;
                if (ch >= '0' && ch <= '9')
                    return "Alpha" + ch;
            }

            if (text.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase) && text.Length == 6 && char.IsDigit(text[5]))
                return text.StartsWith("Alpha", StringComparison.Ordinal) ? text : "Alpha" + text[5];

            if (text.StartsWith("Keypad", StringComparison.OrdinalIgnoreCase) && text.Length == 7 && char.IsDigit(text[6]))
                return text.StartsWith("Keypad", StringComparison.Ordinal) ? text : "Keypad" + text[6];

            if (text.StartsWith("Numpad", StringComparison.OrdinalIgnoreCase) && text.Length == 7 && char.IsDigit(text[6]))
                return "Keypad" + text[6];

            if (text.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) && text.Length >= 6)
                return "Mouse" + text.Substring(5);

            if ((text[0] == 'F' || text[0] == 'f') && TryParseFunctionKey(text, out int functionKey))
                return text[0] == 'F' && text.Length == (functionKey >= 10 ? 3 : 2) ? text : "F" + functionKey;

            return char.IsUpper(text[0]) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static bool TryParseFunctionKey(string text, out int functionKey)
        {
            functionKey = 0;
            if (text.Length < 2 || text.Length > 3)
                return false;
            for (int index = 1; index < text.Length; index++)
            {
                char value = text[index];
                if (value < '0' || value > '9')
                    return false;
                functionKey = (functionKey * 10) + value - '0';
            }
            return functionKey >= 1 && functionKey <= 24;
        }

        internal static IReadOnlyList<string> GetPhysicalButtonIds(string? value)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
                return Array.Empty<string>();
            if (normalized.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return new[] { "LeftControl", "RightControl" };
            if (normalized.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return new[] { "LeftShift", "RightShift" };
            if (normalized.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return new[] { "LeftAlt", "RightAlt" };
            return new[] { normalized };
        }

        internal static bool MatchesPhysicalState(string? value, Func<string, bool> state)
        {
            if (state == null)
                return false;

            string normalized = Normalize(value);
            if (normalized.Length == 0)
                return false;
            if (state(normalized))
                return true;
            if (normalized.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return state("LeftControl") || state("RightControl");
            if (normalized.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return state("LeftShift") || state("RightShift");
            if (normalized.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return state("LeftAlt") || state("RightAlt");
            return false;
        }

        internal static bool PhysicallyConflicts(string? left, string? right)
        {
            IReadOnlyList<string> leftPhysical = GetPhysicalButtonIds(left);
            IReadOnlyList<string> rightPhysical = GetPhysicalButtonIds(right);
            return leftPhysical.Any(l => rightPhysical.Any(r => l.Equals(r, StringComparison.OrdinalIgnoreCase)));
        }

        public bool Equals(DtmButton other) => string.Equals(Id ?? string.Empty, other.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is DtmButton other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id ?? string.Empty);
        public override string ToString() => IsBound ? Id : "None";
    }

    public readonly struct DtmButtonState
    {
        public DtmButtonState(DtmButton button, bool isDown, bool wasPressed, bool wasReleased)
        {
            Button = button;
            IsDown = isDown;
            WasPressed = wasPressed;
            WasReleased = wasReleased;
        }

        public DtmButton Button { get; }

        public bool IsDown { get; }

        public bool WasPressed { get; }

        public bool WasReleased { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.3", Notes = "A single physical chord such as F6 or LeftControl+F6.")]
    public sealed class DtmKeybind : IEquatable<DtmKeybind>
    {
        private readonly DtmButton[] buttons;

        public DtmKeybind(IEnumerable<DtmButton> buttons)
        {
            this.buttons = (buttons ?? Array.Empty<DtmButton>())
                .Where(button => button.IsBound)
                .GroupBy(button => button.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(button => SortBucket(button.Id), StringComparer.OrdinalIgnoreCase)
                .ThenBy(button => button.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public IReadOnlyList<DtmButton> Buttons => buttons;

        internal int ButtonCount => buttons.Length;

        internal DtmButton GetButtonAt(int index) => buttons[index];

        public bool IsBound => buttons.Length > 0;

        public bool ContainsButton(string button)
        {
            string normalized = DtmButton.Normalize(button);
            if (normalized.Length == 0)
                return false;
            foreach (DtmButton value in buttons)
            {
                if (DtmButton.PhysicallyConflicts(value.Id, normalized))
                    return true;
            }
            return false;
        }

        public bool IsDown(Func<string, bool> isDown)
        {
            if (!IsBound || isDown == null)
                return false;
            foreach (DtmButton button in buttons)
            {
                if (!DtmButton.MatchesPhysicalState(button.Id, isDown))
                    return false;
            }
            return true;
        }

        public bool IsPressed(Func<string, bool> wasPressed, Func<string, bool> isDown)
        {
            return IsPressed(wasPressed, isDown, _ => false);
        }

        public bool IsPressed(Func<string, bool> wasPressed, Func<string, bool> isDown, Func<string, bool> wasReleased)
        {
            if (!IsBound || wasPressed == null || isDown == null)
                return false;

            bool anyPressed = false;
            foreach (DtmButton button in buttons)
            {
                bool pressedNow = DtmButton.MatchesPhysicalState(button.Id, wasPressed);
                if (pressedNow)
                {
                    anyPressed = true;
                    continue;
                }

                if (DtmButton.MatchesPhysicalState(button.Id, isDown))
                    continue;
                if (wasReleased != null && DtmButton.MatchesPhysicalState(button.Id, wasReleased))
                    continue;
                return false;
            }
            return anyPressed;
        }

        public bool IsReleased(Func<string, bool> wasReleased, Func<string, bool> isDown, Func<string, bool> wasPressed)
        {
            if (!IsBound || wasReleased == null || isDown == null)
                return false;

            bool anyReleased = false;
            foreach (DtmButton button in buttons)
            {
                bool releasedNow = DtmButton.MatchesPhysicalState(button.Id, wasReleased);
                if (releasedNow)
                {
                    anyReleased = true;
                    continue;
                }

                if (DtmButton.MatchesPhysicalState(button.Id, isDown))
                    continue;
                if (wasPressed != null && DtmButton.MatchesPhysicalState(button.Id, wasPressed))
                    continue;
                return false;
            }
            return anyReleased;
        }

        public bool Equals(DtmKeybind? other) => other != null && ToString().Equals(other.ToString(), StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is DtmKeybind other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());
        public override string ToString() => IsBound ? string.Join("+", buttons.Select(button => button.Id)) : "None";

        internal IReadOnlyList<string> GetButtonIds() => buttons.Select(button => button.Id).ToArray();

        private static string SortBucket(string id)
        {
            if (id.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0)
                return "0-" + id;
            if (id.IndexOf("Shift", StringComparison.OrdinalIgnoreCase) >= 0)
                return "1-" + id;
            if (id.IndexOf("Alt", StringComparison.OrdinalIgnoreCase) >= 0)
                return "2-" + id;
            return "3-" + id;
        }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.3", Notes = "A keybind list can contain multiple candidate chords, e.g. Equals, KeypadPlus.")]
    public sealed class DtmKeybindList : IEquatable<DtmKeybindList>
    {
        private readonly DtmKeybind[] keybinds;

        public DtmKeybindList(IEnumerable<DtmKeybind> keybinds)
        {
            this.keybinds = (keybinds ?? Array.Empty<DtmKeybind>())
                .Where(keybind => keybind != null && keybind.IsBound)
                .GroupBy(keybind => keybind.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
        }

        public IReadOnlyList<DtmKeybind> Keybinds => keybinds;

        internal int KeybindCount => keybinds.Length;

        internal DtmKeybind GetKeybindAt(int index) => keybinds[index];

        public bool IsBound => keybinds.Length > 0;

        public static DtmKeybindList None { get; } = new DtmKeybindList(Array.Empty<DtmKeybind>());

        public static DtmKeybindList Parse(string? text)
        {
            string value = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
                return None;

            var result = new List<DtmKeybind>();
            foreach (string candidate in value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                DtmKeybind keybind = ParseChord(candidate);
                if (keybind.IsBound)
                    result.Add(keybind);
            }

            return result.Count == 0 ? None : new DtmKeybindList(result);
        }

        public static bool TryParse(string? text, out DtmKeybindList keybinds, out string error)
        {
            keybinds = Parse(text);
            error = string.Empty;
            return true;
        }

        public IReadOnlyList<string> GetButtonIds()
        {
            return keybinds
                .SelectMany(keybind => keybind.GetButtonIds())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public bool ContainsButton(string button)
        {
            string normalized = DtmButton.Normalize(button);
            return normalized.Length > 0 && keybinds.Any(keybind => keybind.ContainsButton(normalized));
        }

        public bool IsDown(Func<string, bool> isDown)
        {
            foreach (DtmKeybind keybind in keybinds)
            {
                if (keybind.IsDown(isDown))
                    return true;
            }
            return false;
        }

        public bool IsDown(IInputHelper input)
        {
            if (input == null)
                return false;

            foreach (DtmKeybind keybind in keybinds)
            {
                if (IsKeybindDown(keybind, input))
                    return true;
            }
            return false;
        }

        public bool IsPressed(Func<string, bool> wasPressed, Func<string, bool> isDown)
        {
            return IsPressed(wasPressed, isDown, _ => false);
        }

        public bool IsPressed(Func<string, bool> wasPressed, Func<string, bool> isDown, Func<string, bool> wasReleased)
        {
            foreach (DtmKeybind keybind in keybinds)
            {
                if (keybind.IsPressed(wasPressed, isDown, wasReleased))
                    return true;
            }
            return false;
        }

        public bool JustPressed(IInputHelper input)
        {
            if (input == null)
                return false;

            foreach (DtmKeybind keybind in keybinds)
            {
                if (IsKeybindPressed(keybind, input))
                    return true;
            }
            return false;
        }

        public bool IsReleased(Func<string, bool> wasReleased, Func<string, bool> isDown, Func<string, bool> wasPressed)
        {
            foreach (DtmKeybind keybind in keybinds)
            {
                if (keybind.IsReleased(wasReleased, isDown, wasPressed))
                    return true;
            }
            return false;
        }

        public bool JustReleased(IInputHelper input)
        {
            if (input == null)
                return false;

            foreach (DtmKeybind keybind in keybinds)
            {
                if (IsKeybindReleased(keybind, input))
                    return true;
            }
            return false;
        }

        public bool Equals(DtmKeybindList? other) => other != null && ToString().Equals(other.ToString(), StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is DtmKeybindList other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());
        public override string ToString() => IsBound ? string.Join(", ", keybinds.Select(keybind => keybind.ToString())) : "None";

        private static DtmKeybind ParseChord(string text)
        {
            string value = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
                return new DtmKeybind(Array.Empty<DtmButton>());
            if (value == "+" || value.Equals("Plus", StringComparison.OrdinalIgnoreCase))
                return new DtmKeybind(new[] { DtmButton.Parse(value) });

            string[] parts = value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return new DtmKeybind(Array.Empty<DtmButton>());
            return new DtmKeybind(parts.Select(DtmButton.Parse));
        }

        private static bool IsKeybindDown(DtmKeybind keybind, IInputHelper input)
        {
            if (keybind == null || !keybind.IsBound)
                return false;
            for (int index = 0; index < keybind.ButtonCount; index++)
            {
                DtmButton button = keybind.GetButtonAt(index);
                if (!IsInputDown(input, button))
                    return false;
            }
            return true;
        }

        private static bool IsKeybindPressed(DtmKeybind keybind, IInputHelper input)
        {
            if (keybind == null || !keybind.IsBound)
                return false;

            bool anyPressed = false;
            for (int index = 0; index < keybind.ButtonCount; index++)
            {
                DtmButton button = keybind.GetButtonAt(index);
                if (WasInputPressed(input, button))
                {
                    anyPressed = true;
                    continue;
                }
                if (IsInputDown(input, button))
                    continue;
                if (WasInputReleased(input, button))
                    continue;
                return false;
            }
            return anyPressed;
        }

        private static bool IsKeybindReleased(DtmKeybind keybind, IInputHelper input)
        {
            if (keybind == null || !keybind.IsBound)
                return false;

            bool anyReleased = false;
            for (int index = 0; index < keybind.ButtonCount; index++)
            {
                DtmButton button = keybind.GetButtonAt(index);
                if (WasInputReleased(input, button))
                {
                    anyReleased = true;
                    continue;
                }
                if (IsInputDown(input, button))
                    continue;
                if (WasInputPressed(input, button))
                    continue;
                return false;
            }
            return anyReleased;
        }

        private static bool IsInputDown(IInputHelper input, DtmButton button) => MatchesInputState(input, button, (helper, value) => helper.IsDown(value));

        private static bool WasInputPressed(IInputHelper input, DtmButton button) => MatchesInputState(input, button, (helper, value) => helper.WasPressed(value));

        private static bool WasInputReleased(IInputHelper input, DtmButton button) => MatchesInputState(input, button, (helper, value) => helper.WasReleased(value));

        private static bool MatchesInputState(IInputHelper input, DtmButton button, Func<IInputHelper, DtmButton, bool> state)
        {
            if (input == null || state == null || !button.IsBound)
                return false;
            if (state(input, button))
                return true;
            if (button.Id.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return state(input, new DtmButton("LeftControl")) || state(input, new DtmButton("RightControl"));
            if (button.Id.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return state(input, new DtmButton("LeftShift")) || state(input, new DtmButton("RightShift"));
            if (button.Id.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return state(input, new DtmButton("LeftAlt")) || state(input, new DtmButton("RightAlt"));
            return false;
        }
    }

    public enum DtmInputScope
    {
        Always = 0,
        Title = 1,
        SaveLoaded = 2,
        Gameplay = 3
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.3", Notes = "Owner-bound keybind registration handle. Dispose releases the root.")]
    public interface IInputRegistration : IDisposable
    {
        string Id { get; }
        string OwnerId { get; }
        DtmKeybindList Keybinds { get; }
        DtmInputScope Scope { get; }
        bool IsDisposed { get; }
        void Update(string keybindText, DtmInputScope scope = DtmInputScope.Gameplay);
        void Update(DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay);
    }
}
