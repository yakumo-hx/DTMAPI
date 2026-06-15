using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    public sealed class ConfigMenuRegistry : IDtmConfigMenuApi, IConfigMenuRuntime
    {
        private readonly Dictionary<string, ConfigMenuPage> pages = new Dictionary<string, ConfigMenuPage>(StringComparer.OrdinalIgnoreCase);

        public void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false)
        {
            pages[mod.UniqueID] = new ConfigMenuPage(this, mod, reset, save, titleScreenOnly);
        }

        public void AddSectionTitle(IManifest mod, Func<string> text) => GetRequiredPage(mod).AddItem(new TextConfigItem(GetRequiredPage(mod).NextItemId("Section"), "Section", text, () => string.Empty));
        public void AddParagraph(IManifest mod, Func<string> text) => GetRequiredPage(mod).AddItem(new TextConfigItem(GetRequiredPage(mod).NextItemId("Paragraph"), "Paragraph", text, () => string.Empty));
        public void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue) => GetRequiredPage(mod).AddItem(new BoolConfigItem(GetRequiredPage(mod).NextItemId("Bool"), name, tooltip, getValue, setValue));
        public void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool> canEdit, Func<bool>? isVisible = null) => GetRequiredPage(mod).AddItem(new BoolConfigItem(GetRequiredPage(mod).NextItemId("Bool"), name, tooltip, getValue, setValue, canEdit, isVisible));
        public void AddInlineBoolNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval) => GetRequiredPage(mod).AddItem(new InlineBoolNumberConfigItem(GetRequiredPage(mod).NextItemId("InlineBoolNumber"), name, tooltip, getEnabled, setEnabled, getValue, setValue, min, max, interval));
        public void AddInlineBoolBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null) => GetRequiredPage(mod).AddItem(new InlineBoolBoolConfigItem(GetRequiredPage(mod).NextItemId("InlineBoolBool"), name, tooltip, getEnabled, setEnabled, secondaryName, secondaryTooltip, getSecondaryValue, setSecondaryValue, secondaryVisible));
        public void AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval) => GetRequiredPage(mod).AddItem(new NumberConfigItem(GetRequiredPage(mod).NextItemId("Number"), name, tooltip, getValue, setValue, min, max, interval));
        public void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Text"), "Text", name, tooltip, getValue, setValue));
        public void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<bool> canEdit, Func<bool>? isVisible = null) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Text"), "Text", name, tooltip, getValue, setValue, canEdit: canEdit, isVisible: isVisible));
        public void AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues) => GetRequiredPage(mod).AddItem(new ChoiceConfigItem(GetRequiredPage(mod).NextItemId("Choice"), name, tooltip, getValue, setValue, allowedValues));
        public void AddColorPresetOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets) => GetRequiredPage(mod).AddItem(new ColorPresetConfigItem(GetRequiredPage(mod).NextItemId("ColorPreset"), name, tooltip, getValue, setValue, presets));
        public void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Keybind"), "Keybind", name, tooltip, getValue, setValue, isKeybind: true));
        public void AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed) => GetRequiredPage(mod).AddItem(new ButtonConfigItem(GetRequiredPage(mod).NextItemId("Button"), name, tooltip, onPressed));
        public void SetDisplayName(IManifest mod, Func<string> name) => GetRequiredPage(mod).SetDisplayName(name);

        public IReadOnlyList<string> GetKeybindConflicts(string? uniqueId = null)
        {
            var ownersByKey = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (ConfigMenuPage page in pages.Values)
            {
                foreach (ConfigMenuItemBase item in page.ItemsInternal.Where(i => i.Kind.Equals("Keybind", StringComparison.OrdinalIgnoreCase)))
                {
                    string key = NormalizeKeybind(item.PendingValue);
                    if (string.IsNullOrWhiteSpace(key) || key.Equals("None", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!ownersByKey.TryGetValue(key, out List<string> owners))
                    {
                        owners = new List<string>();
                        ownersByKey[key] = owners;
                    }
                    owners.Add(page.Manifest.UniqueID + "." + item.Name);
                }
            }

            var conflicts = new List<string>();
            foreach (KeyValuePair<string, List<string>> pair in ownersByKey.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Value.Count <= 1)
                    continue;
                if (uniqueId != null && !pair.Value.Any(o => o.StartsWith(uniqueId + ".", StringComparison.OrdinalIgnoreCase)))
                    continue;
                conflicts.Add("按键冲突：" + pair.Key + " -> " + string.Join(", ", pair.Value));
            }
            return conflicts;
        }

        IReadOnlyList<IConfigMenuPage> IConfigMenuRuntime.GetPages() => GetPagesCore();
        IConfigMenuPage? IConfigMenuRuntime.GetPage(string uniqueId) => GetPageCore(uniqueId);
        void IConfigMenuRuntime.BeginEditing(string uniqueId) => GetRequiredPage(uniqueId).BeginEditing();
        void IConfigMenuRuntime.Save(string uniqueId) => GetRequiredPage(uniqueId).Save();
        void IConfigMenuRuntime.Reset(string uniqueId) => GetRequiredPage(uniqueId).Reset();
        void IConfigMenuRuntime.Cancel(string uniqueId) => GetRequiredPage(uniqueId).Cancel();
        void IConfigMenuRuntime.SetPageLock(string uniqueId, bool locked, string reason) => SetPageLockCore(uniqueId, locked, reason);
        IDisposable? IConfigMenuRuntime.PreviewPendingValues(IConfigMenuPage page) => page is IConfigMenuPendingPreview preview ? preview.PreviewPendingValues() : null;

        internal bool HasKeybindConflict(ConfigMenuPage page)
        {
            return GetKeybindConflicts(page.Manifest.UniqueID).Count > 0;
        }

        private IReadOnlyList<IConfigMenuPage> GetPagesCore() => pages.Values.OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).Cast<IConfigMenuPage>().ToArray();

        private IConfigMenuPage? GetPageCore(string uniqueId) => pages.TryGetValue(uniqueId, out ConfigMenuPage page) ? page : null;

        private void SetPageLockCore(string uniqueId, bool locked, string reason)
        {
            if (pages.TryGetValue(uniqueId, out ConfigMenuPage page))
                page.SetLocked(locked, reason);
        }

        private ConfigMenuPage GetRequiredPage(IManifest mod) => GetRequiredPage(mod.UniqueID);

        private ConfigMenuPage GetRequiredPage(string uniqueId)
        {
            if (!pages.TryGetValue(uniqueId, out ConfigMenuPage page))
                throw new InvalidOperationException($"Config menu page for {uniqueId} was not registered.");
            return page;
        }

        private static string NormalizeKeybind(string value)
        {
            value = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    internal sealed class ConfigMenuPage : IConfigMenuPage, IConfigMenuPendingPreview
    {
        private readonly ConfigMenuRegistry registry;
        private readonly Action reset;
        private readonly Action save;
        private int nextItemNumber;

        public ConfigMenuPage(ConfigMenuRegistry registry, IManifest manifest, Action reset, Action save, bool titleScreenOnly)
        {
            this.registry = registry;
            Manifest = manifest;
            this.reset = reset;
            this.save = save;
            TitleScreenOnly = titleScreenOnly;
            displayName = () => Manifest.Name;
        }

        public IManifest Manifest { get; }
        public string DisplayName => SafeInvoke(displayName);
        public bool TitleScreenOnly { get; }
        public bool IsEditing { get; private set; }
        public bool HasPendingChanges => ItemsInternal.Any(i => i.HasPendingChange);
        public bool IsLocked { get; private set; }
        public string LockReason { get; private set; } = string.Empty;
        public List<ConfigMenuItemBase> ItemsInternal { get; } = new List<ConfigMenuItemBase>();
        public IReadOnlyList<IConfigMenuItem> Items => ItemsInternal.Where(i => i.IsVisible).Cast<IConfigMenuItem>().ToArray();
        private Func<string> displayName = null!;

        public string NextItemId(string kind)
        {
            nextItemNumber++;
            return Manifest.UniqueID + ":" + kind + ":" + nextItemNumber.ToString(CultureInfo.InvariantCulture);
        }

        public void SetDisplayName(Func<string> name)
        {
            displayName = name ?? (() => Manifest.Name);
        }

        public void AddItem(ConfigMenuItemBase item)
        {
            item.CaptureCommittedValue();
            ItemsInternal.Add(item);
        }

        public void SetLocked(bool locked, string reason)
        {
            IsLocked = locked;
            LockReason = locked ? reason ?? string.Empty : string.Empty;
        }

        public void BeginEditing()
        {
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public IDisposable PreviewPendingValues()
        {
            if (!TryReadCurrentValues("preview", out string[] previousValues))
                return new PendingPreviewScope(this, Array.Empty<string>());

            var scope = new PendingPreviewScope(this, previousValues);
            for (int i = 0; i < ItemsInternal.Count; i++)
            {
                try
                {
                    ItemsInternal[i].ApplyRawValue(ItemsInternal[i].PendingValue);
                }
                catch (Exception ex)
                {
                    ItemsInternal[i].SetValidationError("Preview failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    scope.Dispose();
                    return new PendingPreviewScope(this, Array.Empty<string>());
                }
            }
            return scope;
        }

        public void Reset()
        {
            ThrowIfLocked();
            if (!IsEditing)
                BeginEditing();
            string[] previousValues = ReadCurrentValues("reset");
            try
            {
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".reset", reset);
                foreach (ConfigMenuItemBase item in ItemsInternal)
                    item.CapturePendingFromGetter();
            }
            catch (Exception ex)
            {
                TryRollbackOrThrow(previousValues, "reset", ex);
                throw new InvalidOperationException("Reset failed and config values were rolled back: " + ConfigMenuCallbackRunner.Describe(ex), ex);
            }
        }

        public void Save()
        {
            ThrowIfLocked();
            if (registry.HasKeybindConflict(this))
                throw new InvalidOperationException("存在按键冲突，无法保存配置。");

            string[] previousValues = ReadCurrentValues("save");
            try
            {
                foreach (ConfigMenuItemBase item in ItemsInternal)
                {
                    try
                    {
                        item.ApplyPendingValue();
                    }
                    catch (Exception ex)
                    {
                        item.SetValidationError("Apply failed: " + ConfigMenuCallbackRunner.Describe(ex));
                        throw;
                    }
                }
                ConfigMenuCallbackRunner.Run(Manifest.UniqueID + ".save", save);
            }
            catch (Exception ex)
            {
                TryRollbackOrThrow(previousValues, "save", ex);
                throw new InvalidOperationException("Save failed and config values were rolled back: " + ConfigMenuCallbackRunner.Describe(ex), ex);
            }

            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public void Cancel()
        {
            var errors = new List<string>();
            foreach (ConfigMenuItemBase item in ItemsInternal)
            {
                try
                {
                    item.RestoreCommittedValue();
                }
                catch (Exception ex)
                {
                    string error = "Cancel restore failed: " + ConfigMenuCallbackRunner.Describe(ex);
                    item.SetValidationError(error);
                    errors.Add(item.Name + ": " + error);
                }
            }
            IsEditing = false;
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));
        }

        private void ThrowIfLocked()
        {
            if (IsLocked)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(LockReason) ? "此 Mod 当前由启用来源锁定，不能在 DTMAPI 中修改。" : LockReason);
        }

        private static string SafeInvoke(Func<string> func)
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

        private string[] ReadCurrentValues(string operation)
        {
            if (TryReadCurrentValues(operation, out string[] values))
                return values;
            throw new InvalidOperationException("Failed to read current config values for " + operation + ".");
        }

        private bool TryReadCurrentValues(string operation, out string[] values)
        {
            var result = new string[ItemsInternal.Count];
            for (int i = 0; i < ItemsInternal.Count; i++)
            {
                try
                {
                    result[i] = ItemsInternal[i].ReadCurrentValueForPreview();
                }
                catch (Exception ex)
                {
                    ItemsInternal[i].SetValidationError(operation + " read failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    values = Array.Empty<string>();
                    return false;
                }
            }
            values = result;
            return true;
        }

        private void RestoreRawValues(string[] values, string operation)
        {
            var errors = new List<string>();
            int count = Math.Min(ItemsInternal.Count, values.Length);
            for (int i = 0; i < count; i++)
            {
                try
                {
                    ItemsInternal[i].ApplyRawValue(values[i]);
                }
                catch (Exception ex)
                {
                    string error = operation + " failed: " + ConfigMenuCallbackRunner.Describe(ex);
                    ItemsInternal[i].SetValidationError(error);
                    errors.Add(ItemsInternal[i].Name + ": " + error);
                }
            }

            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));
        }

        private void TryRollbackOrThrow(string[] values, string operation, Exception original)
        {
            try
            {
                RestoreRawValues(values, operation + " rollback");
            }
            catch (Exception rollback)
            {
                throw new InvalidOperationException(
                    operation + " failed and rollback also failed: " +
                    ConfigMenuCallbackRunner.Describe(original) + "; rollback: " +
                    ConfigMenuCallbackRunner.Describe(rollback),
                    new AggregateException(original, rollback));
            }
        }

        private sealed class PendingPreviewScope : IDisposable
        {
            private readonly ConfigMenuPage page;
            private readonly string[] previousValues;
            private bool disposed;

            public PendingPreviewScope(ConfigMenuPage page, string[] previousValues)
            {
                this.page = page;
                this.previousValues = previousValues;
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                int count = Math.Min(page.ItemsInternal.Count, previousValues.Length);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        page.ItemsInternal[i].ApplyRawValue(previousValues[i]);
                    }
                    catch (Exception ex)
                    {
                        page.ItemsInternal[i].SetValidationError("Preview restore failed: " + ConfigMenuCallbackRunner.Describe(ex));
                    }
                }
            }
        }
    }

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

    internal static class ConfigMenuCallbackRunner
    {
        public static void Run(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(operation + " callback failed: " + Describe(ex), ex);
            }
        }

        public static string Describe(Exception ex)
        {
            string message = ex.Message ?? string.Empty;
            return string.IsNullOrWhiteSpace(message) ? ex.GetType().Name : ex.GetType().Name + ": " + message;
        }
    }
}
