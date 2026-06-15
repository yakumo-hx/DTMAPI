using System;
using System.Collections.Generic;
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
}
