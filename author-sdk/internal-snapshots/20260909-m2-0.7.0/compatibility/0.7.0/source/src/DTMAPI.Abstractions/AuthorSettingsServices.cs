using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0", Notes = "Candidate owner configuration; versioned storage is separate from legacy ReadConfig. Runtime thread only.")]
    public interface IVersionedConfigHelper
    {
        void RegisterMigration<T>(int fromSchemaVersion, Func<T, T> migrate);
        OwnerDataResult<T> Read<T>(int schemaVersion, Func<T, string?>? validate = null);
        OwnerDataResult<bool> Write<T>(T value, int schemaVersion, Func<T, string?>? validate = null);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0")]
    public enum DtmInputAudience { Normal, PlatformModal, OwnerModal }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0")]
    public sealed class DtmInputContextSnapshot
    {
        public DtmInputContextSnapshot(long frame, DtmInputScope scope, DtmInputAudience audience, string focusOwnerId)
        { Frame = frame; Scope = scope; Audience = audience; FocusOwnerId = focusOwnerId ?? string.Empty; }
        public long Frame { get; }
        public DtmInputScope Scope { get; }
        public DtmInputAudience Audience { get; }
        public string FocusOwnerId { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0")]
    public sealed class DtmInputBindingInfo
    {
        public DtmInputBindingInfo(string ownerId, string id, string keybinds, DtmInputScope scope, bool eligible, bool suppressed, bool requiresNeutral)
        { OwnerId = ownerId; Id = id; Keybinds = keybinds; Scope = scope; Eligible = eligible; Suppressed = suppressed; RequiresNeutral = requiresNeutral; }
        public string OwnerId { get; }
        public string Id { get; }
        public string Keybinds { get; }
        public DtmInputScope Scope { get; }
        public bool Eligible { get; }
        public bool Suppressed { get; }
        public bool RequiresNeutral { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0", Notes = "Read-only snapshots of existing input routing; conflicts are potential chord overlap, not arbitration. Runtime thread only.")]
    public interface IDtmInputDiagnostics
    {
        DtmInputContextSnapshot Snapshot { get; }
        IReadOnlyList<DtmInputBindingInfo> GetRegistrations();
        IReadOnlyList<DtmInputBindingInfo> FindConflicts(DtmKeybindList keybinds, DtmInputScope scope);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0")]
    public sealed class DtmLanguageSnapshot
    {
        public DtmLanguageSnapshot(string requestedLanguage, string resolvedLanguage)
        { RequestedLanguage = requestedLanguage; ResolvedLanguage = resolvedLanguage; }
        public string RequestedLanguage { get; }
        public string ResolvedLanguage { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0")]
    public sealed class DtmTranslationResult
    {
        public DtmTranslationResult(string text, IEnumerable<string> missingParameters)
        { Text = text; MissingParameters = Array.AsReadOnly((missingParameters ?? Array.Empty<string>()).ToArray()); }
        public string Text { get; }
        public IReadOnlyList<string> MissingParameters { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.0", Notes = "Parameterized owner translations and synchronous main-thread language notifications. Dispose subscriptions or close owner to detach.")]
    public interface IDtmTranslations
    {
        DtmLanguageSnapshot Snapshot { get; }
        DtmTranslationResult Get(string key, IReadOnlyDictionary<string, string>? parameters = null, string fallback = "");
        IDisposable SubscribeLanguageChanged(Action<DtmLanguageSnapshot> callback);
    }
}
