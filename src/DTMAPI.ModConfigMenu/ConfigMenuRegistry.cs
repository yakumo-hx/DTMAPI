using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.ModConfigMenu
{
    public sealed class ConfigMenuRegistry : IDtmConfigMenuApi, IDtmConfigMenuKeybindDefaultsApi, IConfigMenuRuntime, IOwnerBoundApiFactory
    {
        private readonly Dictionary<string, ConfigMenuPage> pages = new Dictionary<string, ConfigMenuPage>(StringComparer.OrdinalIgnoreCase);
        private Action<string, string, string, string>? recordOwnerRegistration;
        private Action<string, string, int, string>? recordOwnerCleanup;
        private Action<string, string, string, string, bool, string>? recordPreviewAudit;

        public void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false)
        {
            IManifest manifest = ConfigMenuManifestSnapshot.Capture(mod);
            if (pages.ContainsKey(manifest.UniqueID))
                throw new InvalidOperationException("Config menu page for '" + manifest.UniqueID + "' is already registered.");
            pages.Add(manifest.UniqueID, new ConfigMenuPage(this, mod, reset, save, titleScreenOnly, manifest));
            recordOwnerRegistration?.Invoke(manifest.UniqueID, "ConfigMenuPage", manifest.UniqueID, "Config menu page registration.");
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
        public void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<string> getDefaultValue) => GetRequiredPage(mod).AddItem(new StringConfigItem(GetRequiredPage(mod).NextItemId("Keybind"), "Keybind", name, tooltip, getValue, setValue, isKeybind: true, getDefaultValue: getDefaultValue));
        public void AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed) => GetRequiredPage(mod).AddItem(new ButtonConfigItem(GetRequiredPage(mod).NextItemId("Button"), name, tooltip, onPressed));
        public void SetDisplayName(IManifest mod, Func<string> name) => GetRequiredPage(mod).SetDisplayName(name);

        public IReadOnlyList<string> GetKeybindConflicts(string? uniqueId = null)
        {
            var ownersByKey = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (ConfigMenuPage page in pages.Values)
            {
                foreach (ConfigMenuItemBase item in page.ItemsInternal.Where(i => i.Kind.Equals("Keybind", StringComparison.OrdinalIgnoreCase)))
                {
                    DtmKeybindList keybinds = DtmKeybindList.Parse(item.PendingValue);
                    if (!keybinds.IsBound)
                        continue;
                    foreach (DtmKeybind keybind in keybinds.Keybinds)
                    {
                        foreach (string key in ExpandConflictKeys(keybind))
                        {
                            if (!ownersByKey.TryGetValue(key, out List<string> owners))
                            {
                                owners = new List<string>();
                                ownersByKey[key] = owners;
                            }
                            owners.Add(page.Manifest.UniqueID + "." + item.Name);
                        }
                    }
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
        int IConfigMenuRuntime.RemoveOwner(string uniqueId)
        {
            if (!pages.TryGetValue(uniqueId, out ConfigMenuPage page))
                return 0;

            // Detach the authoritative root before deactivation. Re-entrant UI
            // lookups can no longer discover the page, while stale references are
            // made inert by the idempotent page cleanup below.
            pages.Remove(uniqueId);
            page.Deactivate();

            try
            {
                recordOwnerCleanup?.Invoke(uniqueId, "ConfigMenuPage", 1, "Owner cleanup removed and deactivated config menu page.");
            }
            catch
            {
                // Diagnostics are best-effort and must never resurrect or fail an
                // otherwise complete owner cleanup.
            }
            return 1;
        }

        void IConfigMenuRuntime.ConfigureDiagnostics(
            Action<string, string, string, string>? recordOwnerRegistration,
            Action<string, string, int, string>? recordOwnerCleanup,
            Action<string, string, string, string, bool, string>? recordPreviewAudit)
        {
            this.recordOwnerRegistration = recordOwnerRegistration;
            this.recordOwnerCleanup = recordOwnerCleanup;
            this.recordPreviewAudit = recordPreviewAudit;
        }

        object IOwnerBoundApiFactory.CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
        {
            if (apiType != typeof(IDtmConfigMenuApi))
                throw new InvalidOperationException("Config menu doesn't provide owner-bound contract '" + (apiType.FullName ?? apiType.Name) + "'.");
            return new OwnerBoundConfigMenuApi(this, consumer, ensureOwnerActive);
        }

        internal void RecordPreviewAudit(IManifest owner, string itemId, string kind, string operation, bool success, string details)
        {
            recordPreviewAudit?.Invoke(owner.UniqueID, itemId, kind, operation, success, details);
        }

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
            return DtmKeybindList.Parse(value).ToString();
        }

        private static IReadOnlyList<string> ExpandConflictKeys(DtmKeybind keybind)
        {
            if (keybind == null || !keybind.IsBound)
                return Array.Empty<string>();

            var keys = new List<string> { string.Empty };
            foreach (DtmButton button in keybind.Buttons)
            {
                IReadOnlyList<string> physicalButtons = DtmButton.GetPhysicalButtonIds(button.Id);
                var next = new List<string>(keys.Count * Math.Max(1, physicalButtons.Count));
                foreach (string prefix in keys)
                {
                    foreach (string physical in physicalButtons)
                    {
                        DtmKeybind expanded = string.IsNullOrWhiteSpace(prefix)
                            ? new DtmKeybind(new[] { DtmButton.Parse(physical) })
                            : new DtmKeybind(prefix.Split('+').Select(DtmButton.Parse).Concat(new[] { DtmButton.Parse(physical) }));
                        next.Add(expanded.ToString());
                    }
                }
                keys = next;
            }

            return keys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private sealed class OwnerBoundConfigMenuApi : IDtmConfigMenuApi, IDtmConfigMenuKeybindDefaultsApi, IOwnerBoundApiFacade
        {
            private ConfigMenuRegistry? inner;
            private IManifest? owner;
            private Action? ensureOwnerActive;

            public OwnerBoundConfigMenuApi(ConfigMenuRegistry inner, IManifest owner, Action ensureOwnerActive)
            {
                this.inner = inner;
                this.owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false) { Ensure(mod); inner!.Register(owner!, reset, save, titleScreenOnly); }
            public void AddSectionTitle(IManifest mod, Func<string> text) { Ensure(mod); inner!.AddSectionTitle(owner!, text); }
            public void AddParagraph(IManifest mod, Func<string> text) { Ensure(mod); inner!.AddParagraph(owner!, text); }
            public void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue) { Ensure(mod); inner!.AddBoolOption(owner!, name, tooltip, getValue, setValue); }
            public void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool> canEdit, Func<bool>? isVisible = null) { Ensure(mod); inner!.AddBoolOption(owner!, name, tooltip, getValue, setValue, canEdit, isVisible); }
            public void AddInlineBoolNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval) { Ensure(mod); inner!.AddInlineBoolNumberOption(owner!, name, tooltip, getEnabled, setEnabled, getValue, setValue, min, max, interval); }
            public void AddInlineBoolBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null) { Ensure(mod); inner!.AddInlineBoolBoolOption(owner!, name, tooltip, getEnabled, setEnabled, secondaryName, secondaryTooltip, getSecondaryValue, setSecondaryValue, secondaryVisible); }
            public void AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval) { Ensure(mod); inner!.AddNumberOption(owner!, name, tooltip, getValue, setValue, min, max, interval); }
            public void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) { Ensure(mod); inner!.AddTextOption(owner!, name, tooltip, getValue, setValue); }
            public void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<bool> canEdit, Func<bool>? isVisible = null) { Ensure(mod); inner!.AddTextOption(owner!, name, tooltip, getValue, setValue, canEdit, isVisible); }
            public void AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues) { Ensure(mod); inner!.AddChoiceOption(owner!, name, tooltip, getValue, setValue, allowedValues); }
            public void AddColorPresetOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets) { Ensure(mod); inner!.AddColorPresetOption(owner!, name, tooltip, getValue, setValue, presets); }
            public void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue) { Ensure(mod); inner!.AddKeybindOption(owner!, name, tooltip, getValue, setValue); }
            public void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<string> getDefaultValue) { Ensure(mod); inner!.AddKeybindOption(owner!, name, tooltip, getValue, setValue, getDefaultValue); }
            public void AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed) { Ensure(mod); inner!.AddButton(owner!, name, tooltip, onPressed); }
            public void SetDisplayName(IManifest mod, Func<string> name) { Ensure(mod); inner!.SetDisplayName(owner!, name); }
            public IReadOnlyList<string> GetKeybindConflicts(string? uniqueId = null) { EnsureActive(); return inner!.GetKeybindConflicts(uniqueId); }

            void IOwnerBoundApiFacade.Deactivate()
            {
                ensureOwnerActive = null;
                owner = null;
                inner = null;
            }

            private void Ensure(IManifest manifest)
            {
                EnsureActive();
                if (manifest == null || !string.Equals(manifest.UniqueID, owner!.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Config menu API for owner '" + owner!.UniqueID + "' can't register resources for another owner.");
            }

            private void EnsureActive()
            {
                Action? ensure = ensureOwnerActive;
                if (ensure == null || inner == null || owner == null)
                    throw new InvalidOperationException("Config menu API facade is inactive.");
                ensure();
            }
        }
    }
}
