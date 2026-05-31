using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    public sealed class ConfigMenuRegistry : IDtmConfigMenuApi
    {
        private readonly Dictionary<string, ConfigMenuPage> pages = new Dictionary<string, ConfigMenuPage>(StringComparer.OrdinalIgnoreCase);

        public void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false)
        {
            pages[mod.UniqueID] = new ConfigMenuPage(this, mod, reset, save, titleScreenOnly);
        }

        public void AddSectionTitle(IManifest mod, Func<string> text) => GetRequiredPage(mod).AddItem(new TextConfigItem(GetRequiredPage(mod).NextItemId("Section"), "Section", text, () => string.Empty));
        public void AddParagraph(IManifest mod, Func<string> text) => GetRequiredPage(mod).AddItem(new TextConfigItem(GetRequiredPage(mod).NextItemId("Paragraph"), "Paragraph", text, () => string.Empty));
        public void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue) => GetRequiredPage(mod).AddItem(new BoolConfigItem(GetRequiredPage(mod).NextItemId("Bool"), name, tooltip, getValue, setValue));
        public void AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval) => GetRequiredPage(mod).AddItem(new NumberConfigItem(GetRequiredPage(mod).NextItemId("Number"), name, tooltip, getValue, setValue, min, max, interval));
        public void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Text"), "Text", name, tooltip, getValue, setValue));
        public void AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues) => GetRequiredPage(mod).AddItem(new ChoiceConfigItem(GetRequiredPage(mod).NextItemId("Choice"), name, tooltip, getValue, setValue, allowedValues));
        public void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Keybind"), "Keybind", name, tooltip, getValue, setValue, isKeybind: true));
        public void AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed) => GetRequiredPage(mod).AddItem(new ButtonConfigItem(GetRequiredPage(mod).NextItemId("Button"), name, tooltip, onPressed));
        public void SetDisplayName(IManifest mod, Func<string> name) => GetRequiredPage(mod).SetDisplayName(name);

        public IReadOnlyList<IConfigMenuPage> GetPages() => pages.Values.OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).Cast<IConfigMenuPage>().ToArray();
        public IConfigMenuPage? GetPage(string uniqueId) => pages.TryGetValue(uniqueId, out ConfigMenuPage page) ? page : null;
        public void BeginEditing(string uniqueId) => GetRequiredPage(uniqueId).BeginEditing();
        public void Save(string uniqueId) => GetRequiredPage(uniqueId).Save();
        public void Reset(string uniqueId) => GetRequiredPage(uniqueId).Reset();
        public void Cancel(string uniqueId) => GetRequiredPage(uniqueId).Cancel();

        public void SetPageLock(string uniqueId, bool locked, string reason)
        {
            if (pages.TryGetValue(uniqueId, out ConfigMenuPage page))
                page.SetLocked(locked, reason);
        }

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

        internal bool HasKeybindConflict(ConfigMenuPage page)
        {
            return GetKeybindConflicts(page.Manifest.UniqueID).Count > 0;
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

    internal sealed class ConfigMenuPage : IConfigMenuPage
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
        public IReadOnlyList<IConfigMenuItem> Items => ItemsInternal.Cast<IConfigMenuItem>().ToArray();
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

        public void Reset()
        {
            ThrowIfLocked();
            if (!IsEditing)
                BeginEditing();
            reset();
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CapturePendingFromGetter();
        }

        public void Save()
        {
            ThrowIfLocked();
            if (registry.HasKeybindConflict(this))
                throw new InvalidOperationException("存在按键冲突，无法保存配置。");

            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.ApplyPendingValue();
            save();
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.CaptureCommittedValue();
            IsEditing = true;
        }

        public void Cancel()
        {
            foreach (ConfigMenuItemBase item in ItemsInternal)
                item.RestoreCommittedValue();
            IsEditing = false;
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
            pendingValue = committedValue;
            ApplyValue(committedValue);
            ValidationError = string.Empty;
        }

        internal virtual void ApplyPendingValue()
        {
            ApplyValue(pendingValue);
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

        public BoolConfigItem(string itemId, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue) : base(itemId, "Bool", name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

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

    internal class StringConfigItem : ConfigMenuItemBase
    {
        private readonly Func<string> getValue;
        private readonly Action<string> setValue;
        private readonly bool isKeybind;

        public StringConfigItem(string itemId, string kind, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, bool isKeybind = false) : base(itemId, kind, name, tooltip)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.isKeybind = isKeybind;
        }

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

        public override void Invoke() => onPressed();
    }
}
