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
            : this(owner, message, details, BoundedDiagnosticScalar.StableIdentity(owner), DateTimeOffset.Now)
        {
        }

        internal DtmErrorInfo(string owner, string message, string details, string ownerIdentity)
            : this(owner, message, details, ownerIdentity, DateTimeOffset.Now)
        {
        }

        internal DtmErrorInfo(string owner, string message, string details, string ownerIdentity, DateTimeOffset time)
        {
            long trimmed = 0;
            Time = time;
            Owner = BoundedDiagnosticScalar.Sanitize(owner, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            Message = BoundedDiagnosticScalar.Sanitize(message, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Details = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            OwnerIdentity = BoundedDiagnosticScalar.Sanitize(ownerIdentity, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
        internal string OwnerIdentity { get; }
    }

    public sealed class DtmWarningInfo : IDtmWarningInfo
    {
        public DtmWarningInfo(string owner, string message, string details)
            : this(owner, message, details, BoundedDiagnosticScalar.StableIdentity(owner), DateTimeOffset.Now)
        {
        }

        internal DtmWarningInfo(string owner, string message, string details, string ownerIdentity)
            : this(owner, message, details, ownerIdentity, DateTimeOffset.Now)
        {
        }

        internal DtmWarningInfo(string owner, string message, string details, string ownerIdentity, DateTimeOffset time)
        {
            long trimmed = 0;
            Time = time;
            Owner = BoundedDiagnosticScalar.Sanitize(owner, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            Message = BoundedDiagnosticScalar.Sanitize(message, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Details = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            OwnerIdentity = BoundedDiagnosticScalar.Sanitize(ownerIdentity, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
        internal string OwnerIdentity { get; }
    }

    public sealed class HookStatusInfo : IHookStatusInfo
    {
        public HookStatusInfo(string hookId, string status, string source, string details)
            : this(hookId, status, source, details, DateTimeOffset.Now)
        {
        }

        internal HookStatusInfo(string hookId, string status, string source, string details, DateTimeOffset updatedAt)
        {
            long trimmed = 0;
            HookId = BoundedDiagnosticScalar.Sanitize(hookId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Status = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            Source = BoundedDiagnosticScalar.Sanitize(source, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            Details = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
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
            long trimmed = 0;
            FeatureId = BoundedDiagnosticScalar.Sanitize(featureId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Status = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            LastOperation = BoundedDiagnosticScalar.Sanitize(lastOperation, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            Success = success;
            FailureCount = failureCount;
            LastError = BoundedDiagnosticScalar.Sanitize(lastError, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Details = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
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
            long trimmed = 0;
            UniqueID = BoundedDiagnosticScalar.Sanitize(manifest?.UniqueID, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            Name = BoundedDiagnosticScalar.Sanitize(manifest?.Name, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Version = BoundedDiagnosticScalar.Sanitize(manifest?.Version, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Type = BoundedDiagnosticScalar.Sanitize(manifest?.Type, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            EntryType = BoundedDiagnosticScalar.Sanitize(manifest?.EntryType, BoundedDiagnosticScalar.MessageChars, ref trimmed);
        }

        internal DtmLoadedModInfo(IDtmLoadedModInfo row)
        {
            long trimmed = 0;
            UniqueID = BoundedDiagnosticScalar.Sanitize(row?.UniqueID, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            Name = BoundedDiagnosticScalar.Sanitize(row?.Name, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Version = BoundedDiagnosticScalar.Sanitize(row?.Version, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Type = BoundedDiagnosticScalar.Sanitize(row?.Type, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            EntryType = BoundedDiagnosticScalar.Sanitize(row?.EntryType, BoundedDiagnosticScalar.MessageChars, ref trimmed);
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
            long trimmed = 0;
            UniqueID = BoundedDiagnosticScalar.Sanitize(uniqueId, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            Name = BoundedDiagnosticScalar.Sanitize(name, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Version = BoundedDiagnosticScalar.Sanitize(version, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Type = BoundedDiagnosticScalar.Sanitize(type, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Source = BoundedDiagnosticScalar.Sanitize(source, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            OfficialId = BoundedDiagnosticScalar.Sanitize(officialId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            OfficialEnabled = officialEnabled;
            OfficialEnablementManaged = officialEnablementManaged;
            EnablementReason = BoundedDiagnosticScalar.Sanitize(enablementReason, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            EntryDll = BoundedDiagnosticScalar.Sanitize(entryDll, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            EntryType = BoundedDiagnosticScalar.Sanitize(entryType, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            Loaded = loaded;
            Status = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            StatusCode = BoundedDiagnosticScalar.Sanitize(statusCode, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Reason = BoundedDiagnosticScalar.Sanitize(reason, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            ManifestPath = BoundedDiagnosticScalar.Sanitize(manifestPath, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            RootPath = BoundedDiagnosticScalar.Sanitize(rootPath, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
        }

        internal DtmModStatusInfo(IDtmModStatusInfo row)
            : this(
                row?.UniqueID ?? string.Empty,
                row?.Name ?? string.Empty,
                row?.Version ?? string.Empty,
                row?.Type ?? string.Empty,
                row?.Source ?? string.Empty,
                row?.OfficialId ?? string.Empty,
                row?.OfficialEnabled ?? false,
                row?.OfficialEnablementManaged ?? false,
                row?.EnablementReason ?? string.Empty,
                row?.EntryDll ?? string.Empty,
                row?.EntryType ?? string.Empty,
                row?.Loaded ?? false,
                row?.Status ?? string.Empty,
                row?.StatusCode ?? string.Empty,
                row?.Reason ?? string.Empty,
                row?.ManifestPath ?? string.Empty,
                row?.RootPath ?? string.Empty)
        {
            if (row is DtmModStatusInfo retained)
            {
                ManagedIdentity = retained.ManagedIdentity;
                DeclaredKind = retained.DeclaredKind;
                EffectiveKind = retained.EffectiveKind;
                DeclarationProvenance = retained.DeclarationProvenance;
                ManagedPlacement = retained.ManagedPlacement;
                NativeRisk = retained.NativeRisk;
                GameCompatibility = retained.GameCompatibility;
                RestartPolicy = retained.RestartPolicy;
                ExpectedHarmonyOwner = retained.ExpectedHarmonyOwner;
                AdvancedReferenceVerified = retained.AdvancedReferenceVerified;
                ReferenceReceiptPath = retained.ReferenceReceiptPath;
                ReferencePolicyId = retained.ReferencePolicyId;
                ReferencePolicyVersion = retained.ReferencePolicyVersion;
                ReferencePolicySha256 = retained.ReferencePolicySha256;
                TargetFramework = retained.TargetFramework;
                GameBuildId = retained.GameBuildId;
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
            if (classification == null)
                return;
            long trimmed = 0;
            ManagedIdentity = BoundedDiagnosticScalar.Sanitize(classification.IdentityName, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            DeclaredKind = BoundedDiagnosticScalar.Sanitize(classification.DeclaredKind, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            EffectiveKind = BoundedDiagnosticScalar.Sanitize(classification.EffectiveKind, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            DeclarationProvenance = BoundedDiagnosticScalar.Sanitize(classification.DeclarationProvenance, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            ManagedPlacement = BoundedDiagnosticScalar.Sanitize(classification.Placement, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            NativeRisk = BoundedDiagnosticScalar.Sanitize(classification.NativeRisk, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            GameCompatibility = BoundedDiagnosticScalar.Sanitize(classification.GameCompatibility, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            RestartPolicy = BoundedDiagnosticScalar.Sanitize(classification.RestartPolicy, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            ExpectedHarmonyOwner = BoundedDiagnosticScalar.Sanitize(classification.ExpectedHarmonyOwner, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            AdvancedReferenceVerified = classification.AdvancedReferenceVerified;
            ReferenceReceiptPath = BoundedDiagnosticScalar.Sanitize(classification.ReferenceReceiptPath, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            ReferencePolicyId = BoundedDiagnosticScalar.Sanitize(classification.ReferencePolicyId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            ReferencePolicyVersion = classification.ReferencePolicyVersion;
            ReferencePolicySha256 = BoundedDiagnosticScalar.Sanitize(classification.ReferencePolicySha256, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            TargetFramework = BoundedDiagnosticScalar.Sanitize(classification.TargetFramework, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            GameBuildId = BoundedDiagnosticScalar.Sanitize(classification.GameBuildId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
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
        internal string DeclaredKind { get; } = string.Empty;
        internal string EffectiveKind { get; } = string.Empty;
        internal string DeclarationProvenance { get; } = string.Empty;
        internal string ManagedPlacement { get; } = string.Empty;
        internal string NativeRisk { get; } = string.Empty;
        internal string GameCompatibility { get; } = string.Empty;
        internal string RestartPolicy { get; } = string.Empty;
        internal string ExpectedHarmonyOwner { get; } = string.Empty;
        internal bool AdvancedReferenceVerified { get; }
        internal string ReferenceReceiptPath { get; } = string.Empty;
        internal string ReferencePolicyId { get; } = string.Empty;
        internal int ReferencePolicyVersion { get; }
        internal string ReferencePolicySha256 { get; } = string.Empty;
        internal string TargetFramework { get; } = string.Empty;
        internal string GameBuildId { get; } = string.Empty;
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
            LoadedMods = (loadedMods ?? Array.Empty<IDtmLoadedModInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxModRows)
                .Select(row => (IDtmLoadedModInfo)new DtmLoadedModInfo(row))
                .ToArray();
            Mods = (mods ?? Array.Empty<IDtmModStatusInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxModRows)
                .Select(row => (IDtmModStatusInfo)new DtmModStatusInfo(row))
                .ToArray();
            Errors = (errors ?? Array.Empty<IDtmErrorInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxDiagnosticRows)
                .Select(row => (IDtmErrorInfo)new DtmErrorInfo(row.Owner, row.Message, row.Details, row is DtmErrorInfo retained ? retained.OwnerIdentity : BoundedDiagnosticScalar.StableIdentity(row.Owner), row.Time))
                .ToArray();
            Warnings = (warnings ?? Array.Empty<IDtmWarningInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxDiagnosticRows)
                .Select(row => (IDtmWarningInfo)new DtmWarningInfo(row.Owner, row.Message, row.Details, row is DtmWarningInfo retained ? retained.OwnerIdentity : BoundedDiagnosticScalar.StableIdentity(row.Owner), row.Time))
                .ToArray();
            HookStatuses = (hookStatuses ?? Array.Empty<IHookStatusInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxStatusRows)
                .Select(row => (IHookStatusInfo)new HookStatusInfo(row.HookId, row.Status, row.Source, row.Details, row.UpdatedAt))
                .ToArray();
            FeatureStatuses = (featureStatuses ?? Array.Empty<IDtmFeatureStatusInfo>())
                .Where(row => row != null)
                .Take(BoundedDiagnosticScalar.MaxStatusRows)
                .Select(row => (IDtmFeatureStatusInfo)new DtmFeatureStatusInfo(row.FeatureId, row.Status, row.LastOperation, row.Success, row.FailureCount, row.LastError, row.Details, row.UpdatedAt))
                .ToArray();
            long trimmed = 0;
            LatestLogPath = BoundedDiagnosticScalar.Sanitize(latestLogPath, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            LatestReportPath = BoundedDiagnosticScalar.Sanitize(latestReportPath, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
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
