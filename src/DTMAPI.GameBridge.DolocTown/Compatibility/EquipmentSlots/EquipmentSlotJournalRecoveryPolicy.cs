using System;

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
            string preSaveFingerprint,
            string currentSaveFingerprint,
            int beforeBackpackCount,
            int currentBackpackCount,
            int beforeMailCount,
            int currentMailCount)
        {
            Checkpoint = checkpoint;
            AttemptStarted = attemptStarted;
            PreSaveFingerprint = preSaveFingerprint ?? string.Empty;
            CurrentSaveFingerprint = currentSaveFingerprint ?? string.Empty;
            BeforeBackpackCount = Math.Max(0, beforeBackpackCount);
            CurrentBackpackCount = Math.Max(0, currentBackpackCount);
            BeforeMailCount = Math.Max(0, beforeMailCount);
            CurrentMailCount = Math.Max(0, currentMailCount);
        }

        internal EquipmentSlotJournalCheckpoint Checkpoint { get; }

        internal bool AttemptStarted { get; }

        internal string PreSaveFingerprint { get; }

        internal string CurrentSaveFingerprint { get; }

        internal int BeforeBackpackCount { get; }

        internal int CurrentBackpackCount { get; }

        internal int BeforeMailCount { get; }

        internal int CurrentMailCount { get; }
    }

    internal static class EquipmentSlotJournalRecoveryPolicy
    {
        internal static EquipmentSlotJournalRecoveryDecision Decide(
            EquipmentSlotJournalRecoverySnapshot snapshot)
        {
            if (snapshot.Checkpoint == EquipmentSlotJournalCheckpoint.Committed ||
                snapshot.Checkpoint == EquipmentSlotJournalCheckpoint.NativeCommitted)
            {
                return EquipmentSlotJournalRecoveryDecision.FinalizeCommitted;
            }

            bool destinationEvidence =
                snapshot.CurrentBackpackCount > snapshot.BeforeBackpackCount ||
                snapshot.CurrentMailCount > snapshot.BeforeMailCount;
            bool fingerprintChanged =
                snapshot.AttemptStarted &&
                !string.IsNullOrWhiteSpace(snapshot.PreSaveFingerprint) &&
                !string.IsNullOrWhiteSpace(snapshot.CurrentSaveFingerprint) &&
                !string.Equals(
                    snapshot.PreSaveFingerprint,
                    snapshot.CurrentSaveFingerprint,
                    StringComparison.Ordinal);
            if (fingerprintChanged && destinationEvidence)
                return EquipmentSlotJournalRecoveryDecision.FinalizeCommitted;

            bool nativeUnchanged =
                string.IsNullOrWhiteSpace(snapshot.PreSaveFingerprint) ||
                string.Equals(
                    snapshot.PreSaveFingerprint,
                    snapshot.CurrentSaveFingerprint,
                    StringComparison.Ordinal);
            if (destinationEvidence || !nativeUnchanged)
                return EquipmentSlotJournalRecoveryDecision.FailClosed;

            return EquipmentSlotJournalRecoveryDecision.RetryPlacement;
        }
    }
}
