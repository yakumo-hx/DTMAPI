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
        void AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval);
        void AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue);
        void AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues);
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

    public interface IConfigMenuItem
    {
        string ItemId { get; }
        string Kind { get; }
        string Name { get; }
        string Tooltip { get; }
        string DisplayValue { get; }
        string PendingValue { get; }
        bool CanEdit { get; }
        bool HasPendingChange { get; }
        string ValidationError { get; }
        IReadOnlyList<string> AllowedValues { get; }
        double? MinValue { get; }
        double? MaxValue { get; }
        double? Interval { get; }
        bool TrySetPendingValue(string value, out string error);
        void Invoke();
    }
}
