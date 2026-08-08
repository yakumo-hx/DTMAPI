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
        private Func<string>? nameGetter;
        private Func<string>? tooltipGetter;
        private bool isActive = true;

        protected ConfigMenuItemBase(string itemId, string kind, Func<string> name, Func<string> tooltip)
        {
            ItemId = itemId;
            Kind = kind;
            nameGetter = name;
            tooltipGetter = tooltip;
        }

        public string ItemId { get; }
        public string Kind { get; }
        public string Name => isActive ? SafeInvoke(nameGetter) : string.Empty;
        public string Tooltip => isActive ? SafeInvoke(tooltipGetter) : string.Empty;
        public string DisplayValue => !isActive ? string.Empty : CanEdit ? pendingValue : ReadValue();
        public string PendingValue => pendingValue;
        public virtual bool CanEdit => isActive;
        public virtual bool IsVisible => isActive;
        public bool HasPendingChange => isActive && CanEdit && !string.Equals(committedValue, pendingValue, StringComparison.Ordinal);
        public string ValidationError { get; private set; } = string.Empty;
        public virtual IReadOnlyList<string> AllowedValues => Array.Empty<string>();
        public virtual double? MinValue => null;
        public virtual double? MaxValue => null;
        public virtual double? Interval => null;
        protected bool IsActive => isActive;

        public bool TrySetPendingValue(string value, out string error)
        {
            if (!isActive)
            {
                error = "This option is inactive.";
                return false;
            }
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
            ThrowIfInactive();
            committedValue = ReadValue();
            pendingValue = committedValue;
            ValidationError = string.Empty;
        }

        internal void CapturePendingFromGetter()
        {
            ThrowIfInactive();
            pendingValue = ReadValue();
            ValidationError = string.Empty;
        }

        internal void RestoreCommittedValue()
        {
            ThrowIfInactive();
            ApplyValue(committedValue);
            pendingValue = committedValue;
            ValidationError = string.Empty;
        }

        internal virtual void ApplyPendingValue()
        {
            ThrowIfInactive();
            ApplyValue(pendingValue);
        }

        internal string ReadCurrentValueForPreview()
        {
            ThrowIfInactive();
            return ReadValue();
        }

        internal void ApplyRawValue(string value)
        {
            ThrowIfInactive();
            ApplyValue(value);
        }

        internal void SetValidationError(string error) => ValidationError = error ?? string.Empty;

        internal void RestorePendingValue(string value)
        {
            if (!isActive)
                return;
            pendingValue = value ?? string.Empty;
        }

        internal void Deactivate()
        {
            if (!isActive)
                return;

            isActive = false;
            nameGetter = null;
            tooltipGetter = null;
            committedValue = string.Empty;
            pendingValue = string.Empty;
            ValidationError = string.Empty;
            DeactivateCore();
        }

        protected virtual void DeactivateCore()
        {
        }

        protected void ThrowIfInactive()
        {
            if (!isActive)
                throw new InvalidOperationException("This config menu item is inactive because its owner was deactivated.");
        }

        protected abstract string ReadValue();
        protected abstract void ApplyValue(string value);
        protected abstract bool TryNormalize(string value, out string normalized, out string error);

        protected static string SafeInvoke(Func<string>? func)
        {
            if (func == null)
                return string.Empty;
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
        private Func<string>? text;

        public TextConfigItem(string itemId, string kind, Func<string> text, Func<string> tooltip) : base(itemId, kind, text, tooltip)
        {
            this.text = text;
        }

        public override bool CanEdit => false;
        public override bool IsVisible => IsActive;
        protected override string ReadValue() => SafeInvoke(text);
        protected override void ApplyValue(string value) { }
        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            error = string.Empty;
            return true;
        }

        protected override void DeactivateCore() => text = null;
    }

    internal sealed class BoolConfigItem : ConfigMenuItemBase
    {
        private Func<bool>? getValue;
        private Action<bool>? setValue;
        private Func<bool>? canEdit;
        private Func<bool>? isVisible;

        public BoolConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool>? canEdit = null, Func<bool>? isVisible = null) : base(itemId, "Bool", name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.canEdit = canEdit;
            this.isVisible = isVisible;
        }

        public override bool CanEdit => IsActive && (canEdit == null || SafeInvokeBool(canEdit, true));
        public override bool IsVisible => IsActive && (isVisible == null || SafeInvokeBool(isVisible, true));

        protected override string ReadValue() => getValue != null && getValue() ? "true" : "false";
        protected override void ApplyValue(string value) => setValue?.Invoke(value.Equals("true", StringComparison.OrdinalIgnoreCase));

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

        protected override void DeactivateCore()
        {
            getValue = null;
            setValue = null;
            canEdit = null;
            isVisible = null;
        }
    }

    internal sealed class NumberConfigItem : ConfigMenuItemBase
    {
        private Func<double>? getValue;
        private Action<double>? setValue;
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

        protected override string ReadValue() => NormalizeNumber(getValue == null ? min : getValue()).ToString("0.###", CultureInfo.InvariantCulture);
        protected override void ApplyValue(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                setValue?.Invoke(NormalizeNumber(parsed));
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

        protected override void DeactivateCore()
        {
            getValue = null;
            setValue = null;
        }
    }

    internal sealed class InlineBoolNumberConfigItem : ConfigMenuItemBase
    {
        private Func<bool>? getEnabled;
        private Action<bool>? setEnabled;
        private Func<double>? getValue;
        private Action<double>? setValue;
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

        protected override string ReadValue() => (getEnabled != null && getEnabled() ? "true" : "false") + "|" + NormalizeNumber(getValue == null ? min : getValue()).ToString("0.###", CultureInfo.InvariantCulture);

        protected override void ApplyValue(string value)
        {
            if (!TryParse(value, out bool enabled, out double number))
                return;
            setEnabled?.Invoke(enabled);
            setValue?.Invoke(NormalizeNumber(number));
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

        protected override void DeactivateCore()
        {
            getEnabled = null;
            setEnabled = null;
            getValue = null;
            setValue = null;
        }
    }

    internal sealed class InlineBoolBoolConfigItem : ConfigMenuItemBase
    {
        private Func<bool>? getEnabled;
        private Action<bool>? setEnabled;
        private Func<string>? secondaryName;
        private Func<string>? secondaryTooltip;
        private Func<bool>? getSecondaryValue;
        private Action<bool>? setSecondaryValue;
        private Func<bool>? secondaryVisible;

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

        public override IReadOnlyList<string> AllowedValues => IsActive
            ? new[]
            {
                SafeInvoke(secondaryName),
                SafeInvoke(secondaryTooltip),
                IsSecondaryVisible() ? "true" : "false"
            }
            : Array.Empty<string>();

        protected override string ReadValue() => (getEnabled != null && getEnabled() ? "true" : "false") + "|" + (getSecondaryValue != null && getSecondaryValue() ? "true" : "false");

        protected override void ApplyValue(string value)
        {
            if (!TryParse(value, out bool enabled, out bool secondary))
                return;
            setEnabled?.Invoke(enabled);
            setSecondaryValue?.Invoke(enabled && IsSecondaryVisible() && secondary);
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

        protected override void DeactivateCore()
        {
            getEnabled = null;
            setEnabled = null;
            secondaryName = null;
            secondaryTooltip = null;
            getSecondaryValue = null;
            setSecondaryValue = null;
            secondaryVisible = null;
        }
    }

    internal class StringConfigItem : ConfigMenuItemBase, IResettableKeybindConfigMenuItem
    {
        private Func<string>? getValue;
        private Action<string>? setValue;
        private readonly bool isKeybind;
        private Func<bool>? canEdit;
        private Func<bool>? isVisible;
        private Func<string>? getDefaultValue;

        public StringConfigItem(string itemId, string kind, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, bool isKeybind = false, Func<bool>? canEdit = null, Func<bool>? isVisible = null, Func<string>? getDefaultValue = null) : base(itemId, kind, name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.isKeybind = isKeybind;
            this.canEdit = canEdit;
            this.isVisible = isVisible;
            this.getDefaultValue = getDefaultValue;
        }

        public override bool CanEdit => IsActive && (canEdit == null || SafeInvokeBool(canEdit, true));
        public override bool IsVisible => IsActive && (isVisible == null || SafeInvokeBool(isVisible, true));
        public bool HasDefaultValue => IsActive && isKeybind && getDefaultValue != null;
        public string DefaultValue => HasDefaultValue ? NormalizeKeybindDefault(SafeInvokeString(getDefaultValue!)) : string.Empty;

        public bool TryResetPendingValue(out string error)
        {
            if (!IsActive)
            {
                error = "This option is inactive.";
                return false;
            }
            if (!HasDefaultValue)
            {
                error = "This keybind option has no default value.";
                return false;
            }
            try
            {
                return TrySetPendingValue(NormalizeKeybindDefault(getDefaultValue!() ?? string.Empty), out error);
            }
            catch (Exception ex)
            {
                error = "The default keybind could not be read: " + ex.Message;
                return false;
            }
        }

        protected override string ReadValue()
        {
            string value = getValue?.Invoke() ?? string.Empty;
            return isKeybind && string.IsNullOrWhiteSpace(value) ? "None" : value;
        }

        protected override void ApplyValue(string value) => setValue?.Invoke(value ?? string.Empty);

        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            if (isKeybind)
            {
                normalized = DtmKeybindList.Parse(normalized).ToString();
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

        private static string SafeInvokeString(Func<string> func)
        {
            try
            {
                return func() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeKeybindDefault(string value) => DtmKeybindList.Parse(value).ToString();

        protected override void DeactivateCore()
        {
            getValue = null;
            setValue = null;
            canEdit = null;
            isVisible = null;
            getDefaultValue = null;
        }
    }

    internal sealed class ChoiceConfigItem : StringConfigItem
    {
        private string[] allowedValues;

        public ChoiceConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues) : base(itemId, "Choice", name, tooltip, getValue, setValue)
        {
            this.allowedValues = (allowedValues ?? Array.Empty<string>()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        }

        public override IReadOnlyList<string> AllowedValues => IsActive ? allowedValues : Array.Empty<string>();

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

        protected override void DeactivateCore()
        {
            allowedValues = Array.Empty<string>();
            base.DeactivateCore();
        }
    }

    internal sealed class ColorPresetConfigItem : StringConfigItem
    {
        private DtmColorPreset[] presets;

        public ColorPresetConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets) : base(itemId, "ColorPreset", name, tooltip, getValue, setValue)
        {
            this.presets = (presets ?? Array.Empty<DtmColorPreset>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Id))
                .ToArray();
        }

        public override IReadOnlyList<string> AllowedValues => IsActive
            ? presets.Select(p => p.Id + "|" + p.Label + "|" + NormalizeHex(p.HexColor)).ToArray()
            : Array.Empty<string>();

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

        protected override void DeactivateCore()
        {
            presets = Array.Empty<DtmColorPreset>();
            base.DeactivateCore();
        }
    }

    internal sealed class ButtonConfigItem : ConfigMenuItemBase
    {
        private Action? onPressed;

        public ButtonConfigItem(string itemId, Func<string> name, Func<string> tooltip, Action onPressed) : base(itemId, "Button", name, tooltip)
        {
            this.onPressed = onPressed;
        }

        public override bool CanEdit => false;
        public override bool IsVisible => IsActive;
        protected override string ReadValue() => "Press";
        protected override void ApplyValue(string value) { }
        protected override bool TryNormalize(string value, out string normalized, out string error)
        {
            normalized = value ?? string.Empty;
            error = string.Empty;
            return true;
        }

        public override void Invoke()
        {
            Action? callback = onPressed;
            if (!IsActive || callback == null)
                return;
            ConfigMenuCallbackRunner.Run("button", callback);
        }

        protected override void DeactivateCore() => onPressed = null;
    }
}
