using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Diagnostics
{
    public sealed class DtmErrorInfo : IDtmErrorInfo
    {
        public DtmErrorInfo(string owner, string message, string details)
        {
            Time = DateTimeOffset.Now;
            Owner = owner;
            Message = message;
            Details = details;
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
    }

    public sealed class DtmWarningInfo : IDtmWarningInfo
    {
        public DtmWarningInfo(string owner, string message, string details)
        {
            Time = DateTimeOffset.Now;
            Owner = owner;
            Message = message;
            Details = details;
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
    }

    public sealed class HookStatusInfo : IHookStatusInfo
    {
        public HookStatusInfo(string hookId, string status, string source, string details)
        {
            HookId = hookId;
            Status = status;
            Source = source;
            Details = details;
            UpdatedAt = DateTimeOffset.Now;
        }

        public string HookId { get; }
        public string Status { get; }
        public string Source { get; }
        public string Details { get; }
        public DateTimeOffset UpdatedAt { get; }
    }

    public sealed class DtmFeatureStatusInfo : IDtmFeatureStatusInfo
    {
        public DtmFeatureStatusInfo(string featureId, string status, string lastOperation, bool success, int failureCount, string lastError, string details)
        {
            FeatureId = featureId ?? string.Empty;
            Status = status ?? string.Empty;
            LastOperation = lastOperation ?? string.Empty;
            Success = success;
            FailureCount = failureCount;
            LastError = lastError ?? string.Empty;
            Details = details ?? string.Empty;
            UpdatedAt = DateTimeOffset.Now;
        }

        public string FeatureId { get; }
        public string Status { get; }
        public string LastOperation { get; }
        public bool Success { get; }
        public int FailureCount { get; }
        public string LastError { get; }
        public string Details { get; }
        public DateTimeOffset UpdatedAt { get; }
    }

    public sealed class DtmLoadedModInfo : IDtmLoadedModInfo
    {
        public DtmLoadedModInfo(IManifest manifest)
        {
            UniqueID = manifest?.UniqueID ?? string.Empty;
            Name = manifest?.Name ?? string.Empty;
            Version = manifest?.Version ?? string.Empty;
            Type = manifest?.Type ?? string.Empty;
            EntryType = manifest?.EntryType ?? string.Empty;
        }

        public string UniqueID { get; }
        public string Name { get; }
        public string Version { get; }
        public string Type { get; }
        public string EntryType { get; }
    }

    public sealed class DtmModStatusInfo : IDtmModStatusInfo
    {
        public DtmModStatusInfo(
            string uniqueId,
            string name,
            string version,
            string type,
            string source,
            string officialId,
            bool officialEnabled,
            bool officialEnablementManaged,
            string enablementReason,
            string entryDll,
            string entryType,
            bool loaded,
            string status,
            string statusCode,
            string reason,
            string manifestPath,
            string rootPath)
        {
            UniqueID = uniqueId ?? string.Empty;
            Name = name ?? string.Empty;
            Version = version ?? string.Empty;
            Type = type ?? string.Empty;
            Source = source ?? string.Empty;
            OfficialId = officialId ?? string.Empty;
            OfficialEnabled = officialEnabled;
            OfficialEnablementManaged = officialEnablementManaged;
            EnablementReason = enablementReason ?? string.Empty;
            EntryDll = entryDll ?? string.Empty;
            EntryType = entryType ?? string.Empty;
            Loaded = loaded;
            Status = status ?? string.Empty;
            StatusCode = statusCode ?? string.Empty;
            Reason = reason ?? string.Empty;
            ManifestPath = manifestPath ?? string.Empty;
            RootPath = rootPath ?? string.Empty;
        }

        public string UniqueID { get; }
        public string Name { get; }
        public string Version { get; }
        public string Type { get; }
        public string Source { get; }
        public string OfficialId { get; }
        public bool OfficialEnabled { get; }
        public bool OfficialEnablementManaged { get; }
        public string EnablementReason { get; }
        public string EntryDll { get; }
        public string EntryType { get; }
        public bool Loaded { get; }
        public string Status { get; }
        public string StatusCode { get; }
        public string Reason { get; }
        public string ManifestPath { get; }
        public string RootPath { get; }
    }

    public sealed class DtmDiagnosticsSnapshot : IDtmDiagnosticsSnapshot
    {
        public DtmDiagnosticsSnapshot(
            DateTimeOffset startedAt,
            IReadOnlyList<IDtmLoadedModInfo> loadedMods,
            IReadOnlyList<IDtmModStatusInfo> mods,
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyList<IDtmWarningInfo> warnings,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IDtmFeatureStatusInfo> featureStatuses,
            string latestLogPath,
            string latestReportPath)
        {
            StartedAt = startedAt;
            LoadedMods = loadedMods;
            Mods = mods;
            Errors = errors;
            Warnings = warnings;
            HookStatuses = hookStatuses;
            FeatureStatuses = featureStatuses;
            LatestLogPath = latestLogPath ?? string.Empty;
            LatestReportPath = latestReportPath ?? string.Empty;
        }

        public DateTimeOffset StartedAt { get; }
        public IReadOnlyList<IDtmLoadedModInfo> LoadedMods { get; }
        public IReadOnlyList<IDtmModStatusInfo> Mods { get; }
        public IReadOnlyList<IDtmErrorInfo> Errors { get; }
        public IReadOnlyList<IDtmWarningInfo> Warnings { get; }
        public IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        public IReadOnlyList<IDtmFeatureStatusInfo> FeatureStatuses { get; }
        public string LatestLogPath { get; }
        public string LatestReportPath { get; }
    }
}
