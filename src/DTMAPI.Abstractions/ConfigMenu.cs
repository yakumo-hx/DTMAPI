using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IDtmConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text);
        void AddParagraph(IManifest mod, Func<string> text);
        void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue);
        void AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool> canEdit, Func<bool>? isVisible = null);
        void AddInlineBoolNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval);
        void AddInlineBoolBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null);
        void AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval);
        void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue);
        void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<bool> canEdit, Func<bool>? isVisible = null);
        void AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues);
        void AddColorPresetOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets);
        void AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue);
        void AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed);
        void SetDisplayName(IManifest mod, Func<string> name);
        IReadOnlyList<IConfigMenuPage> GetPages();
        IConfigMenuPage? GetPage(string uniqueId);
        void BeginEditing(string uniqueId);
        void Save(string uniqueId);
        void Reset(string uniqueId);
        void Cancel(string uniqueId);
        void SetPageLock(string uniqueId, bool locked, string reason);
        IReadOnlyList<string> GetKeybindConflicts(string? uniqueId = null);
    }

    public interface IConfigMenuPage
    {
        IManifest Manifest { get; }
        string DisplayName { get; }
        bool TitleScreenOnly { get; }
        IReadOnlyList<IConfigMenuItem> Items { get; }
        bool IsEditing { get; }
        bool HasPendingChanges { get; }
        bool IsLocked { get; }
        string LockReason { get; }
        void Reset();
        void Save();
        void Cancel();
        void BeginEditing();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.4")]
    public interface IConfigMenuPendingPreview
    {
        IDisposable PreviewPendingValues();
    }

    public interface IConfigMenuItem
    {
        string ItemId { get; }
        string Kind { get; }
        string Name { get; }
        string Tooltip { get; }
        string DisplayValue { get; }
        string PendingValue { get; }
        bool CanEdit { get; }
        bool IsVisible { get; }
        bool HasPendingChange { get; }
        string ValidationError { get; }
        IReadOnlyList<string> AllowedValues { get; }
        double? MinValue { get; }
        double? MaxValue { get; }
        double? Interval { get; }
        bool TrySetPendingValue(string value, out string error);
        void Invoke();
    }

    public sealed class DtmColorPreset
    {
        public DtmColorPreset(string id, string label, string hexColor)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            HexColor = hexColor ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
        public string HexColor { get; }
    }
}
