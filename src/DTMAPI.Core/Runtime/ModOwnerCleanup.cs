using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal enum ModOwnerCleanupReason
    {
        EntryFailed,
        Unload,
        RuntimeShutdown
    }

    internal interface IModOwnerCleanupParticipant
    {
        string ParticipantId { get; }

        ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason);
    }

    internal readonly struct ModOwnerCleanupParticipantResult
    {
        public ModOwnerCleanupParticipantResult(int removedResources, string details)
            : this(removedResources, 0, 0, details)
        {
        }

        public ModOwnerCleanupParticipantResult(int removedResources, int remainingResources, string details)
            : this(removedResources, remainingResources, 0, details)
        {
        }

        public ModOwnerCleanupParticipantResult(int removedResources, int remainingResources, int failureCount, string details)
        {
            RemovedResources = Math.Max(0, removedResources);
            RemainingResources = Math.Max(0, remainingResources);
            FailureCount = Math.Max(0, failureCount);
            Details = details ?? string.Empty;
        }

        public int RemovedResources { get; }

        public int RemainingResources { get; }

        public int FailureCount { get; }

        public string Details { get; }

        public static ModOwnerCleanupParticipantResult None(string details = "") => new ModOwnerCleanupParticipantResult(0, details);
    }

    internal sealed class ModOwnerParticipantCleanupSummary
    {
        public ModOwnerParticipantCleanupSummary(
            string ownerId,
            ModOwnerCleanupReason reason,
            IReadOnlyList<ModOwnerParticipantCleanupEntry> entries)
        {
            OwnerId = ownerId ?? string.Empty;
            Reason = reason;
            Entries = entries ?? Array.Empty<ModOwnerParticipantCleanupEntry>();
        }

        public string OwnerId { get; }

        public ModOwnerCleanupReason Reason { get; }

        public IReadOnlyList<ModOwnerParticipantCleanupEntry> Entries { get; }

        public int RemovedResources => Entries.Sum(entry => entry.RemovedResources);

        public int RemainingResources => Entries.Sum(entry => entry.RemainingResources);

        public int FailureCount => Entries.Sum(entry => entry.FailureCount);

        public string FormatSummary()
        {
            string participants = Entries.Count == 0
                ? "none"
                : string.Join("|", Entries.Select(entry => entry.ParticipantId + ":" +
                    entry.RemovedResources.ToString(CultureInfo.InvariantCulture) + "/" + entry.RemainingResources.ToString(CultureInfo.InvariantCulture) + ":" +
                    (entry.Success ? "ok" : "failed(" + entry.FailureCount.ToString(CultureInfo.InvariantCulture) + ")")));
            string participantDetails = Entries.Count == 0
                ? "none"
                : string.Join("|", Entries.Select(entry => entry.ParticipantId + "{" + FormatDiagnosticDetails(entry.Details) + "}"));
            return "owner=" + OwnerId +
                ", reason=" + Reason +
                ", participants=" + Entries.Count.ToString(CultureInfo.InvariantCulture) +
                ", removed=" + RemovedResources.ToString(CultureInfo.InvariantCulture) +
                ", remaining=" + RemainingResources.ToString(CultureInfo.InvariantCulture) +
                ", failures=" + FailureCount.ToString(CultureInfo.InvariantCulture) +
                ", results=" + participants +
                ", details=" + participantDetails;
        }

        private static string FormatDiagnosticDetails(string details)
        {
            const int maxLength = 1024;
            string normalized = (details ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('|', '/');
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength) + "...[truncated]";
        }
    }

    internal readonly struct ModOwnerParticipantCleanupEntry
    {
        public ModOwnerParticipantCleanupEntry(string participantId, int removedResources, bool success, string details, int remainingResources = 0, int failureCount = 0)
        {
            ParticipantId = participantId ?? string.Empty;
            RemovedResources = Math.Max(0, removedResources);
            RemainingResources = Math.Max(0, remainingResources);
            Success = success;
            FailureCount = success ? 0 : Math.Max(1, failureCount);
            Details = details ?? string.Empty;
        }

        public string ParticipantId { get; }

        public int RemovedResources { get; }

        public int RemainingResources { get; }

        public bool Success { get; }

        public int FailureCount { get; }

        public string Details { get; }
    }
}
