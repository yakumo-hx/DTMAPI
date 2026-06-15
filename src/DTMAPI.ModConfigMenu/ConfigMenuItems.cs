using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    internal abstract class ConfigMenuItemBase : IConfigMenuItem
    {
        private string committedValue = string.Empty;
        private string pendingValue = string.Empty;

        protected ConfigMenuItemBase(string itemId, string kind, Func<string> name, Func<string> tooltip)
        {
            ItemId = itemId;
            Kind = kind;
            NameGetter = name;
            TooltipGetter = tooltip;
        }

        public string ItemId { get; }
        public string Kind { get; }
        public string Name => SafeInvoke(NameGetter);
        public string Tooltip => SafeInvoke(TooltipGetter);
        public string DisplayValue => CanEdit ? pendingValue : ReadValue();
        public string PendingValue => pendingValue;
        public virtual bool CanEdit => true;
        public virtual bool IsVisible => true;
        public bool HasPendingChange => CanEdit && !string.Equals(committedValue, pendingValue, StringComparison.Ordinal);
        public string ValidationError { get; private set; } = string.Empty;
        public virtual IReadOnlyList<string> AllowedValues => Array.Empty<string>();
        public virtual double? MinValue => null;
        public virtual double? MaxValue => null;
        public virtual double? Interval => null;
        protected Func<string> NameGetter { get; }
        protected Func<string> TooltipGetter { get; }

        public bool TrySetPendingValue(string value, out string error)
        {
            if (!CanEdit)
            {
                error = "This option is read-only.";
                ValidationError = error;
                return false;
            }
            if (!TryNormalize(value, out string normalized, out error))
            {
                ValidationError = error;
                return false;
            }
            pendingValue = normalized;
            ValidationError = string.Empty;
            return true;
        }

        public virtual void Invoke()
        {
        }

        internal void CaptureCommittedValue()
        {
            committedValue = ReadValue();
            pendingValue = committedValue;
            ValidationError = string.Empty;
        }

        internal void CapturePendingFromGetter()
        {
            pendingValue = ReadValue();
            ValidationError = string.Empty;
        }

        internal void RestoreCommittedValue()
        {
            ApplyValue(committedValue);
            pendingValue = committedValue;
            ValidationError = string.Empty;
        }

        internal virtual void ApplyPendingValue()
        {
            ApplyValue(pendingValue);
        }

        internal string ReadCurrentValueForPreview() => ReadValue();

        internal void ApplyRawValue(string value) => ApplyValue(value);

        internal void SetValidationError(string error) => ValidationError = error ?? string.Empty;

        internal void RestorePendingValue(string value)
        {
            pendingValue = value ?? string.Empty;
        }

        protected abstract string ReadValue();
        protected abstract void ApplyValue(string value);
        protected abstract bool TryNormalize(string value, out string normalized, out string error);

        protected static string SafeInvoke(Func<string> func)
        {
            try
            {
                return func() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return "<error: " + ex.GetType().Name + ">";
            }
        }
    }

    internal sealed class TextConfigItem : ConfigMenuItemBase
    {
        private readonly Func<string> text;

        public TextConfigItem(string itemId, string kind, Func<string> text, Func<string> tooltip) : base(itemId, kind, text, tooltip)
        {
            this.text = text;
        }

        public override bool CanEdit => false;
        protected override string ReadValue() => SafeInvoke(text);
        protected override void ApplyValue(string value) { }
        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            error = string.Empty;
            return true;
        }
    }

    internal sealed class BoolConfigItem : ConfigMenuItemBase
    {
        private readonly Func<bool> getValue;
        private readonly Action<bool> setValue;
        private readonly Func<bool>? canEdit;
        private readonly Func<bool>? isVisible;

        public BoolConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool>? canEdit = null, Func<bool>? isVisible = null) : base(itemId, "Bool", name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.canEdit = canEdit;
            this.isVisible = isVisible;
        }

        public override bool CanEdit => canEdit == null || SafeInvokeBool(canEdit, true);
        public override bool IsVisible => isVisible == null || SafeInvokeBool(isVisible, true);

        protected override string ReadValue() => getValue() ? "true" : "false";
        protected override void ApplyValue(string value) => setValue(value.Equals("true", StringComparison.OrdinalIgnoreCase));

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            if (bool.TryParse(value, out bool parsed))
            {
                normalized = parsed ? "true" : "false";
                error = string.Empty;
                return true;
            }
            normalized = PendingValue;
            error = "Expected true or false.";
            return false;
        }

        private static bool SafeInvokeBool(Func<bool> func, bool fallback)
        {
            try
            {
                return func();
            }
            catch
            {
                return fallback;
            }
        }
    }

    internal sealed class NumberConfigItem : ConfigMenuItemBase
    {
        private readonly Func<double> getValue;
        private readonly Action<double> setValue;
        private readonly double min;
        private readonly double max;
        private readonly double interval;

        public NumberConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval) : base(itemId, "Number", name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.min = min;
            this.max = max;
            this.interval = interval <= 0 ? 1 : interval;
        }

        public override double? MinValue => min;
        public override double? MaxValue => max;
        public override double? Interval => interval;

        protected override string ReadValue() => NormalizeNumber(getValue()).ToString("0.###", CultureInfo.InvariantCulture);
        protected override void ApplyValue(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                setValue(NormalizeNumber(parsed));
        }

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                normalized = PendingValue;
                error = "Expected a number.";
                return false;
            }
            normalized = NormalizeNumber(parsed).ToString("0.###", CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        private double NormalizeNumber(double value)
        {
            double clamped = Math.Min(max, Math.Max(min, value));
            if (interval > 0)
            {
                double steps = Math.Round((clamped - min) / interval, MidpointRounding.AwayFromZero);
                clamped = min + steps * interval;
            }
            return Math.Min(max, Math.Max(min, clamped));
        }
    }

    internal sealed class InlineBoolNumberConfigItem : ConfigMenuItemBase
    {
        private readonly Func<bool> getEnabled;
        private readonly Action<bool> setEnabled;
        private readonly Func<double> getValue;
        private readonly Action<double> setValue;
        private readonly double min;
        private readonly double max;
        private readonly double interval;

        public InlineBoolNumberConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval) : base(itemId, "InlineBoolNumber", name, tooltip)
        {
            this.getEnabled = getEnabled;
            this.setEnabled = setEnabled;
            this.getValue = getValue;
            this.setValue = setValue;
            this.min = min;
            this.max = max;
            this.interval = interval <= 0 ? 1 : interval;
        }

        public override double? MinValue => min;
        public override double? MaxValue => max;
        public override double? Interval => interval;

        protected override string ReadValue() => (getEnabled() ? "true" : "false") + "|" + NormalizeNumber(getValue()).ToString("0.###", CultureInfo.InvariantCulture);

        protected override void ApplyValue(string value)
        {
            if (!TryParse(value, out bool enabled, out double number))
                return;
            setEnabled(enabled);
            setValue(NormalizeNumber(number));
        }

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            if (!TryParse(value, out bool enabled, out double number))
            {
                normalized = PendingValue;
                error = "Expected true|number.";
                return false;
            }
            normalized = (enabled ? "true" : "false") + "|" + NormalizeNumber(number).ToString("0.###", CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        private bool TryParse(string value, out bool enabled, out double number)
        {
            enabled = false;
            number = min;
            string[] parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 2 || !bool.TryParse(parts[0], out enabled))
                return false;
            return double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private double NormalizeNumber(double value)
        {
            double clamped = Math.Min(max, Math.Max(min, value));
            if (interval > 0)
            {
                double steps = Math.Round((clamped - min) / interval, MidpointRounding.AwayFromZero);
                clamped = min + steps * interval;
            }
            return Math.Min(max, Math.Max(min, clamped));
        }
    }

    internal sealed class InlineBoolBoolConfigItem : ConfigMenuItemBase
    {
        private readonly Func<bool> getEnabled;
        private readonly Action<bool> setEnabled;
        private readonly Func<string> secondaryName;
        private readonly Func<string> secondaryTooltip;
        private readonly Func<bool> getSecondaryValue;
        private readonly Action<bool> setSecondaryValue;
        private readonly Func<bool>? secondaryVisible;

        public InlineBoolBoolConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null) : base(itemId, "InlineBoolBool", name, tooltip)
        {
            this.getEnabled = getEnabled;
            this.setEnabled = setEnabled;
            this.secondaryName = secondaryName;
            this.secondaryTooltip = secondaryTooltip;
            this.getSecondaryValue = getSecondaryValue;
            this.setSecondaryValue = setSecondaryValue;
            this.secondaryVisible = secondaryVisible;
        }

        public override IReadOnlyList<string> AllowedValues => new[]
        {
            SafeInvoke(secondaryName),
            SafeInvoke(secondaryTooltip),
            IsSecondaryVisible() ? "true" : "false"
        };

        protected override string ReadValue() => (getEnabled() ? "true" : "false") + "|" + (getSecondaryValue() ? "true" : "false");

        protected override void ApplyValue(string value)
        {
            if (!TryParse(value, out bool enabled, out bool secondary))
                return;
            setEnabled(enabled);
            setSecondaryValue(enabled && IsSecondaryVisible() && secondary);
        }

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            if (!TryParse(value, out bool enabled, out bool secondary))
            {
                normalized = PendingValue;
                error = "Expected true|false.";
                return false;
            }
            normalized = (enabled ? "true" : "false") + "|" + (secondary ? "true" : "false");
            error = string.Empty;
            return true;
        }

        private bool IsSecondaryVisible() => secondaryVisible == null || SafeInvokeBool(secondaryVisible, true);

        private static bool TryParse(string value, out bool enabled, out bool secondary)
        {
            enabled = false;
            secondary = false;
            string[] parts = (value ?? string.Empty).Split('|');
            return parts.Length == 2 && bool.TryParse(parts[0], out enabled) && bool.TryParse(parts[1], out secondary);
        }

        private static bool SafeInvokeBool(Func<bool> func, bool fallback)
        {
            try
            {
                return func();
            }
            catch
            {
                return fallback;
            }
        }
    }

    internal class StringConfigItem : ConfigMenuItemBase
    {
        private readonly Func<string> getValue;
        private readonly Action<string> setValue;
        private readonly bool isKeybind;
        private readonly Func<bool>? canEdit;
        private readonly Func<bool>? isVisible;

        public StringConfigItem(string itemId, string kind, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, bool isKeybind = false, Func<bool>? canEdit = null, Func<bool>? isVisible = null) : base(itemId, kind, name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.isKeybind = isKeybind;
            this.canEdit = canEdit;
            this.isVisible = isVisible;
        }

        public override bool CanEdit => canEdit == null || SafeInvokeBool(canEdit, true);
        public override bool IsVisible => isVisible == null || SafeInvokeBool(isVisible, true);

        protected override string ReadValue()
        {
            string value = getValue() ?? string.Empty;
            return isKeybind && string.IsNullOrWhiteSpace(value) ? "None" : value;
        }

        protected override void ApplyValue(string value) => setValue(value ?? string.Empty);

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            if (isKeybind)
            {
                normalized = normalized.Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                    normalized = "None";
            }
            error = string.Empty;
            return true;
        }

        private static bool SafeInvokeBool(Func<bool> func, bool fallback)
        {
            try
            {
                return func();
            }
            catch
            {
                return fallback;
            }
        }
    }

    internal sealed class ChoiceConfigItem : StringConfigItem
    {
        private readonly IReadOnlyList<string> allowedValues;

        public ChoiceConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues) : base(itemId, "Choice", name, tooltip, getValue, setValue)
        {
            this.allowedValues = (allowedValues ?? Array.Empty<string>()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        }

        public override IReadOnlyList<string> AllowedValues => allowedValues;

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            string? match = allowedValues.FirstOrDefault(v => v.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                normalized = match;
                error = string.Empty;
                return true;
            }
            normalized = PendingValue;
            error = "Expected one of: " + string.Join(", ", allowedValues);
            return false;
        }
    }

    internal sealed class ColorPresetConfigItem : StringConfigItem
    {
        private readonly IReadOnlyList<DtmColorPreset> presets;

        public ColorPresetConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets) : base(itemId, "ColorPreset", name, tooltip, getValue, setValue)
        {
            this.presets = (presets ?? Array.Empty<DtmColorPreset>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Id))
                .ToArray();
        }

        public override IReadOnlyList<string> AllowedValues => presets
            .Select(p => p.Id + "|" + p.Label + "|" + NormalizeHex(p.HexColor))
            .ToArray();

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            DtmColorPreset? preset = presets.FirstOrDefault(p => p.Id.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (preset != null)
            {
                normalized = preset.Id;
                error = string.Empty;
                return true;
            }
            normalized = PendingValue;
            error = "Expected one color preset.";
            return false;
        }

        private static string NormalizeHex(string value)
        {
            value = (value ?? string.Empty).Trim().TrimStart('#');
            return value.Length == 6 ? value.ToUpperInvariant() : "FFFFFF";
        }
    }

    internal sealed class ButtonConfigItem : ConfigMenuItemBase
    {
        private readonly Action onPressed;

        public ButtonConfigItem(string itemId, Func<string> name, Func<string> tooltip, Action onPressed) : base(itemId, "Button", name, tooltip)
        {
            this.onPressed = onPressed;
        }

        public override bool CanEdit => false;
        protected override string ReadValue() => "Press";
        protected override void ApplyValue(string value) { }
        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            error = string.Empty;
            return true;
        }

        public override void Invoke() => ConfigMenuCallbackRunner.Run("button", onPressed);
    }
}
