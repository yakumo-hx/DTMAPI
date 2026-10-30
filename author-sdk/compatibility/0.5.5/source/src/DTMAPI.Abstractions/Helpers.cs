using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0", Notes = "The helper container shape is stable; individual helper surfaces exposed from this container keep their own DtmApiStatus and may be Experimental, Diagnostic, Proposed, or StableCandidate.")]
    public interface IDtmHelper
    {
        IManifest ModManifest { get; }
        IMonitor Monitor { get; }
        IEventsHelper Events { get; }
        IConfigHelper Config { get; }
        IModRegistry ModRegistry { get; }
        IWorkshopHelper Workshop { get; }
        IUiHelper UI { get; }
        IDiagnosticsHelper Diagnostics { get; }
        IContentQueryHelper Content { get; }
        IInputHelper Input { get; }
        ITranslationHelper Translation { get; }
        TConfig ReadConfig<TConfig>() where TConfig : new();
        void WriteConfig<TConfig>(TConfig config);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.12")]
    public interface ITranslationHelper
    {
        string Language { get; }
        string Get(string key, string fallback = "");
    }

    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IConfigHelper
    {
        TConfig ReadConfig<TConfig>(IManifest manifest) where TConfig : new();
        void WriteConfig<TConfig>(IManifest manifest, TConfig config);
        string GetConfigPath(IManifest manifest);
        void RegisterMigration<TConfig>(IManifest manifest, Action<TConfig> migrate) where TConfig : new();
    }

    [DtmApiStatus(DtmApiStatus.Stable, Since = "0.1.0")]
    public interface IModRegistry
    {
        bool IsLoaded(string uniqueId);
        IManifest? Get(string uniqueId);
        IReadOnlyList<IManifest> GetAll();
        TApi? GetApi<TApi>(string uniqueId) where TApi : class;
        void RegisterApi<TApi>(TApi api) where TApi : class;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0", Notes = "Read-only enabled official/Workshop source index. Disabled or unknown package status remains available through Mod diagnostics; its gameplay item files are not parsed.")]
    public interface IWorkshopHelper
    {
        IReadOnlyList<IWorkshopModInfo> GetOfficialMods();
        IReadOnlyList<IWorkshopModInfo> GetDtmApiMods();
        bool IsOfficialEnablementManaged(IWorkshopModInfo mod);
        string GetEnablementHint(IWorkshopModInfo mod);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IWorkshopModInfo
    {
        string UniqueID { get; }
        string Name { get; }
        string Source { get; }
        string RootPath { get; }
        bool IsEnabledByOfficialPath { get; }
        bool CanDTMApiToggle { get; }
        ulong? WorkshopId { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IUiHelper
    {
        void OpenDtmApiStatusPage();
        void OpenModListPage();
        void OpenConfigPage(string? uniqueId = null);
        void OpenErrorPage();
        void OpenHookStatusPage();
        string ExportLogs();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IDiagnosticsHelper
    {
        IReadOnlyList<IDtmErrorInfo> GetErrors();
        IReadOnlyList<IDtmWarningInfo> GetWarnings();
        IReadOnlyList<IHookStatusInfo> GetHookStatuses();
        string ExportLogs();
        string GetLatestLogPath();
        void RecordEvidence(string caseId, string summary);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IDtmErrorInfo
    {
        DateTimeOffset Time { get; }
        string Owner { get; }
        string Message { get; }
        string Details { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IDtmWarningInfo
    {
        DateTimeOffset Time { get; }
        string Owner { get; }
        string Message { get; }
        string Details { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IHookStatusInfo
    {
        string HookId { get; }
        string Status { get; }
        string Source { get; }
        string Details { get; }
        DateTimeOffset UpdatedAt { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Diagnostic runtime snapshot; not a stable gameplay API.")]
    public interface IDtmDiagnosticsApi
    {
        IDtmDiagnosticsSnapshot GetSnapshot();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Read-only diagnostic snapshot model.")]
    public interface IDtmDiagnosticsSnapshot
    {
        DateTimeOffset StartedAt { get; }
        IReadOnlyList<IDtmLoadedModInfo> LoadedMods { get; }
        IReadOnlyList<IDtmModStatusInfo> Mods { get; }
        IReadOnlyList<IDtmErrorInfo> Errors { get; }
        IReadOnlyList<IDtmWarningInfo> Warnings { get; }
        IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        IReadOnlyList<IDtmFeatureStatusInfo> FeatureStatuses { get; }
        string LatestLogPath { get; }
        string LatestReportPath { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Diagnostic loaded-mod snapshot row.")]
    public interface IDtmLoadedModInfo
    {
        string UniqueID { get; }
        string Name { get; }
        string Version { get; }
        string Type { get; }
        string EntryType { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Diagnostic discovered/loaded/disabled/error mod-status snapshot row.")]
    public interface IDtmModStatusInfo
    {
        string UniqueID { get; }
        string Name { get; }
        string Version { get; }
        string Type { get; }
        string Source { get; }
        string OfficialId { get; }
        bool OfficialEnabled { get; }
        bool OfficialEnablementManaged { get; }
        string EnablementReason { get; }
        string EntryDll { get; }
        string EntryType { get; }
        bool Loaded { get; }
        string Status { get; }
        string StatusCode { get; }
        string Reason { get; }
        string ManifestPath { get; }
        string RootPath { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2", Notes = "Diagnostic GameBridge feature-status snapshot row.")]
    public interface IDtmFeatureStatusInfo
    {
        string FeatureId { get; }
        string Status { get; }
        string LastOperation { get; }
        bool Success { get; }
        int FailureCount { get; }
        string LastError { get; }
        string Details { get; }
        DateTimeOffset UpdatedAt { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IContentQueryHelper
    {
        IReadOnlyList<IContentAssetInfo> FindAssets(string contentType);
        IReadOnlyList<string> GetKnownContentTypes();
        bool TryReadTextAsset(string relativePath, out string text);
        IReadOnlyList<IContentItemInfo> GetIndexedItems();
        IReadOnlyList<IContentItemInfo> GetAllIndexedItems();
        IContentItemInfo? GetIndexedItem(string itemId);
        IContentItemInfo? GetAnyIndexedItem(string itemId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IContentAssetInfo
    {
        string ContentType { get; }
        string RelativePath { get; }
        string SourceModId { get; }
        string SourcePath { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.2")]
    public interface IContentItemInfo
    {
        string ItemId { get; }
        string ChineseName { get; }
        string EnglishName { get; }
        string Category { get; }
        IReadOnlyList<string> Tags { get; }
        string IconAssetKey { get; }
        string IconPath { get; }
        string SourceKind { get; }
        string SourceModTitle { get; }
        string SourceId { get; }
        ulong? WorkshopId { get; }
        bool Enabled { get; }
        bool EnablementKnown { get; }
        bool IsDtmApiContent { get; }
        string RootPath { get; }
        string ContentPath { get; }
        int LoadOrder { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.0")]
    public interface IInputHelper
    {
        void RegisterButton(string button);
        void UnregisterButton(string button);
        IInputRegistration RegisterKeybind(string id, string keybindText, DtmInputScope scope = DtmInputScope.Gameplay);
        IInputRegistration RegisterKeybind(string id, DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay);
        IReadOnlyList<string> GetRegisteredButtons();
        DtmButtonState GetState(DtmButton button);
        bool IsDown(string button);
        bool IsDown(DtmButton button);
        bool WasPressed(string button);
        bool WasPressed(DtmButton button);
        bool WasReleased(string button);
        bool WasReleased(DtmButton button);
        bool IsKeybindDown(string id);
        bool WasKeybindPressed(string id);
        void Suppress(string button);
        IReadOnlyList<string> GetSuppressedButtons();
    }
}
