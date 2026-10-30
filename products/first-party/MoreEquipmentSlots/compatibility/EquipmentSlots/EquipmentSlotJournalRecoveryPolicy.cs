using System;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.GameBridge.DolocTown
{
    internal enum EquipmentSlotJournalCheckpoint
    {
        Prepared = 0,
        NativeCommitted = 1,
        Committed = 2
    }

    internal enum EquipmentSlotJournalRecoveryDecision
    {
        RetryPlacement = 0,
        FinalizeCommitted = 1,
        FailClosed = 2
    }

    internal readonly struct EquipmentSlotJournalRecoverySnapshot
    {
        internal EquipmentSlotJournalRecoverySnapshot(
            EquipmentSlotJournalCheckpoint checkpoint,
            bool attemptStarted,
            bool placementRecorded,
            string preSaveFingerprint,
            string postSaveFingerprint,
            string currentSaveFingerprint,
            int beforeBackpackCount,
            int expectedBackpackCount,
            int currentBackpackCount,
            int beforeMailCount,
            int expectedMailCount,
            int currentMailCount)
        {
            Checkpoint = checkpoint;
            AttemptStarted = attemptStarted;
            PlacementRecorded = placementRecorded;
            PreSaveFingerprint = preSaveFingerprint ?? string.Empty;
            PostSaveFingerprint = postSaveFingerprint ?? string.Empty;
            CurrentSaveFingerprint = currentSaveFingerprint ?? string.Empty;
            BeforeBackpackCount = Math.Max(0, beforeBackpackCount);
            ExpectedBackpackCount = expectedBackpackCount;
            CurrentBackpackCount = Math.Max(0, currentBackpackCount);
            BeforeMailCount = Math.Max(0, beforeMailCount);
            ExpectedMailCount = expectedMailCount;
            CurrentMailCount = Math.Max(0, currentMailCount);
        }

        internal EquipmentSlotJournalCheckpoint Checkpoint { get; }

        internal bool AttemptStarted { get; }

        internal bool PlacementRecorded { get; }

        internal string PreSaveFingerprint { get; }

        internal string PostSaveFingerprint { get; }

        internal string CurrentSaveFingerprint { get; }

        internal int BeforeBackpackCount { get; }

        internal int ExpectedBackpackCount { get; }

        internal int CurrentBackpackCount { get; }

        internal int BeforeMailCount { get; }

        internal int ExpectedMailCount { get; }

        internal int CurrentMailCount { get; }
    }

    internal static class EquipmentSlotJournalRecoveryPolicy
    {
        internal static EquipmentSlotJournalRecoveryDecision Decide(
            EquipmentSlotJournalRecoverySnapshot snapshot)
        {
            bool preimageMatches =
                EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                        snapshot.PreSaveFingerprint,
                        snapshot.CurrentSaveFingerprint);
            bool exactDestination =
                snapshot.PlacementRecorded &&
                snapshot.ExpectedBackpackCount >= 0 &&
                snapshot.ExpectedMailCount >= 0 &&
                snapshot.CurrentBackpackCount ==
                    snapshot.ExpectedBackpackCount &&
                snapshot.CurrentMailCount ==
                    snapshot.ExpectedMailCount;
            if (snapshot.Checkpoint == EquipmentSlotJournalCheckpoint.Committed ||
                snapshot.Checkpoint == EquipmentSlotJournalCheckpoint.NativeCommitted)
            {
                return exactDestination &&
                    EquipmentSlotNativeCommitFingerprint
                        .Equivalent(
                            snapshot.PostSaveFingerprint,
                            snapshot.CurrentSaveFingerprint)
                    ? EquipmentSlotJournalRecoveryDecision
                        .FinalizeCommitted
                    : EquipmentSlotJournalRecoveryDecision.FailClosed;
            }

            bool exactPreimageCounts =
                snapshot.CurrentBackpackCount ==
                    snapshot.BeforeBackpackCount &&
                snapshot.CurrentMailCount ==
                    snapshot.BeforeMailCount;
            if (!snapshot.AttemptStarted)
            {
                return preimageMatches &&
                    exactPreimageCounts &&
                    !snapshot.PlacementRecorded
                    ? EquipmentSlotJournalRecoveryDecision
                        .RetryPlacement
                    : EquipmentSlotJournalRecoveryDecision.FailClosed;
            }

            // An invocation whose immediate same-call re-observation never
            // established one exact destination cannot be reconciled later
            // from count growth. It must remain fail-closed until a
            // NoNativeSave reload restores the prior committed projection.
            if (!snapshot.PlacementRecorded)
                return EquipmentSlotJournalRecoveryDecision.FailClosed;

            if (preimageMatches)
            {
                return exactPreimageCounts
                    ? EquipmentSlotJournalRecoveryDecision
                        .RetryPlacement
                    : EquipmentSlotJournalRecoveryDecision.FailClosed;
            }

            return exactDestination
                ? EquipmentSlotJournalRecoveryDecision.FinalizeCommitted
                : EquipmentSlotJournalRecoveryDecision.FailClosed;
        }
    }
}
