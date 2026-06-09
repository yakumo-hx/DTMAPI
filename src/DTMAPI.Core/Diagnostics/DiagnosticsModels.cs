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

    public sealed class DtmDiagnosticsSnapshot : IDtmDiagnosticsSnapshot
    {
        public DtmDiagnosticsSnapshot(
            DateTimeOffset startedAt,
            IReadOnlyList<IDtmLoadedModInfo> loadedMods,
            IReadOnlyList<IDtmErrorInfo> errors,
            IReadOnlyList<IDtmWarningInfo> warnings,
            IReadOnlyList<IHookStatusInfo> hookStatuses,
            IReadOnlyList<IDtmFeatureStatusInfo> featureStatuses,
            string latestLogPath,
            string latestReportPath)
        {
            StartedAt = startedAt;
            LoadedMods = loadedMods;
            Errors = errors;
            Warnings = warnings;
            HookStatuses = hookStatuses;
            FeatureStatuses = featureStatuses;
            LatestLogPath = latestLogPath ?? string.Empty;
            LatestReportPath = latestReportPath ?? string.Empty;
        }

        public DateTimeOffset StartedAt { get; }
        public IReadOnlyList<IDtmLoadedModInfo> LoadedMods { get; }
        public IReadOnlyList<IDtmErrorInfo> Errors { get; }
        public IReadOnlyList<IDtmWarningInfo> Warnings { get; }
        public IReadOnlyList<IHookStatusInfo> HookStatuses { get; }
        public IReadOnlyList<IDtmFeatureStatusInfo> FeatureStatuses { get; }
        public string LatestLogPath { get; }
        public string LatestReportPath { get; }
    }
}
