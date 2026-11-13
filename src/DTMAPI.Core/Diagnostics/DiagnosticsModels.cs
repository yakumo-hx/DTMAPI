using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Diagnostics
{
    public sealed class DtmErrorInfo : IDtmErrorInfo
    {
        public DtmErrorInfo(string owner, string message, string details)
            : this(owner, message, details, DateTimeOffset.Now)
        {
        }

        internal DtmErrorInfo(string owner, string message, string details, DateTimeOffset time)
        {
            Time = time;
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
            : this(owner, message, details, DateTimeOffset.Now)
        {
        }

        internal DtmWarningInfo(string owner, string message, string details, DateTimeOffset time)
        {
            Time = time;
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
            : this(hookId, status, source, details, DateTimeOffset.Now)
        {
        }

        internal HookStatusInfo(string hookId, string status, string source, string details, DateTimeOffset updatedAt)
        {
            HookId = hookId;
            Status = status;
            Source = source;
            Details = details;
            UpdatedAt = updatedAt;
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
            : this(featureId, status, lastOperation, success, failureCount, lastError, details, DateTimeOffset.Now)
        {
        }

        internal DtmFeatureStatusInfo(string featureId, string status, string lastOperation, bool success, int failureCount, string lastError, string details, DateTimeOffset updatedAt)
        {
            FeatureId = featureId;
            Status = status;
            LastOperation = lastOperation;
            Success = success;
            FailureCount = failureCount;
            LastError = lastError;
            Details = details;
            UpdatedAt = updatedAt;
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
            UniqueID = manifest.UniqueID;
            Name = manifest.Name;
            Version = manifest.Version;
            Type = manifest.Type;
            EntryType = manifest.EntryType;
        }

        internal DtmLoadedModInfo(IDtmLoadedModInfo row)
        {
            UniqueID = row.UniqueID;
            Name = row.Name;
            Version = row.Version;
            Type = row.Type;
            EntryType = row.EntryType;
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
            UniqueID = uniqueId;
            Name = name;
            Version = version;
            Type = type;
            Source = source;
            OfficialId = officialId;
            OfficialEnabled = officialEnabled;
            OfficialEnablementManaged = officialEnablementManaged;
            EnablementReason = enablementReason;
            EntryDll = entryDll;
            EntryType = entryType;
            Loaded = loaded;
            Status = status;
            StatusCode = statusCode;
            Reason = reason;
            ManifestPath = manifestPath;
            RootPath = rootPath;
        }

        internal DtmModStatusInfo(IDtmModStatusInfo row)
            : this(
                row.UniqueID,
                row.Name,
                row.Version,
                row.Type,
                row.Source,
                row.OfficialId,
                row.OfficialEnabled,
                row.OfficialEnablementManaged,
                row.EnablementReason,
                row.EntryDll,
                row.EntryType,
                row.Loaded,
                row.Status,
                row.StatusCode,
                row.Reason,
                row.ManifestPath,
                row.RootPath)
        {
            if (row is DtmModStatusInfo retained)
            {
                ManagedIdentity = retained.ManagedIdentity;
                ExpectedHarmonyOwner = retained.ExpectedHarmonyOwner;
            }
        }

        internal DtmModStatusInfo(
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
            string rootPath,
            ManagedModClassification classification)
            : this(
                uniqueId,
                name,
                version,
                type,
                source,
                officialId,
                officialEnabled,
                officialEnablementManaged,
                enablementReason,
                entryDll,
                entryType,
                loaded,
                status,
                statusCode,
                reason,
                manifestPath,
                rootPath)
        {
            ManagedIdentity = classification.IdentityName;
            ExpectedHarmonyOwner = classification.ExpectedHarmonyOwner;
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
        internal string ManagedIdentity { get; } = string.Empty;
        internal string ExpectedHarmonyOwner { get; } = string.Empty;
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
            LoadedMods = loadedMods

                .Select(row => (IDtmLoadedModInfo)new DtmLoadedModInfo(row))
                .ToArray();
            Mods = mods

                .Select(row => (IDtmModStatusInfo)new DtmModStatusInfo(row))
                .ToArray();
            Errors = errors

                .Select(row => (IDtmErrorInfo)new DtmErrorInfo(row.Owner, row.Message, row.Details, row.Time))
                .ToArray();
            Warnings = warnings

                .Select(row => (IDtmWarningInfo)new DtmWarningInfo(row.Owner, row.Message, row.Details, row.Time))
                .ToArray();
            HookStatuses = hookStatuses

                .Select(row => (IHookStatusInfo)new HookStatusInfo(row.HookId, row.Status, row.Source, row.Details, row.UpdatedAt))
                .ToArray();
            FeatureStatuses = featureStatuses

                .Select(row => (IDtmFeatureStatusInfo)new DtmFeatureStatusInfo(row.FeatureId, row.Status, row.LastOperation, row.Success, row.FailureCount, row.LastError, row.Details, row.UpdatedAt))
                .ToArray();
            LatestLogPath = latestLogPath;
            LatestReportPath = latestReportPath;
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
