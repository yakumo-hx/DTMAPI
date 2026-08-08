using System;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingBitePreparationProvenance
    {
        None = 0,
        OrderedCommit = 1,
        ReconciledAfterWriteFault = 2,
        AlreadyNativeCommitted = 3
    }

    internal readonly struct FishingBitePreparationResult
    {
        internal FishingBitePreparationResult(
            bool committed,
            bool requiresFaultClose,
            bool isFish,
            bool fishObservationAvailable,
            string stage,
            FishingBitePreparationProvenance provenance,
            Exception? commitError,
            Exception? tipError,
            Exception? rendererError,
            Exception? fishObservationError)
        {
            Committed = committed;
            RequiresFaultClose = requiresFaultClose;
            IsFish = isFish;
            FishObservationAvailable = fishObservationAvailable;
            Stage = stage ?? string.Empty;
            Provenance = provenance;
            CommitError = commitError;
            TipError = tipError;
            RendererError = rendererError;
            FishObservationError = fishObservationError;
        }

        internal bool Committed { get; }
        internal bool RequiresFaultClose { get; }
        internal bool IsFish { get; }
        internal bool FishObservationAvailable { get; }
        internal string Stage { get; }
        internal FishingBitePreparationProvenance Provenance { get; }
        internal Exception? CommitError { get; }
        internal Exception? TipError { get; }
        internal Exception? RendererError { get; }
        internal Exception? FishObservationError { get; }
        internal int PostCommitFailureCount =>
            (TipError == null ? 0 : 1) +
            (RendererError == null ? 0 : 1) +
            (FishObservationError == null ? 0 : 1);
    }

    /// <summary>
    /// Commits the Wait-state fields after the native fish roll has already
    /// changed FishingCache. The native wait flag is deliberately written last.
    /// </summary>
    internal static class FishingBitePreparationTransaction
    {
        internal static FishingBitePreparationResult Commit(
            float fishOnHookDuration,
            Func<bool> getWaitForBite,
            Action<float> setHookProbability,
            Action<float> setFishOnHookDuration,
            Action<bool> setHasRolled,
            Action<bool> setWaitForBite,
            Action invokeFishOnHookTip,
            Action refreshFishingRenderer,
            Func<bool> readIsFish)
        {
            if (getWaitForBite == null)
                throw new ArgumentNullException(nameof(getWaitForBite));
            if (setHookProbability == null)
                throw new ArgumentNullException(nameof(setHookProbability));
            if (setFishOnHookDuration == null)
                throw new ArgumentNullException(nameof(setFishOnHookDuration));
            if (setHasRolled == null)
                throw new ArgumentNullException(nameof(setHasRolled));
            if (setWaitForBite == null)
                throw new ArgumentNullException(nameof(setWaitForBite));
            if (invokeFishOnHookTip == null)
                throw new ArgumentNullException(nameof(invokeFishOnHookTip));
            if (refreshFishingRenderer == null)
                throw new ArgumentNullException(nameof(refreshFishingRenderer));
            if (readIsFish == null)
                throw new ArgumentNullException(nameof(readIsFish));

            string stage = "hook-probability";
            Exception? commitError = null;
            bool committed;
            try
            {
                setHookProbability(1f);
                stage = "fish-on-hook-duration";
                setFishOnHookDuration(fishOnHookDuration);
                stage = "has-rolled";
                setHasRolled(true);
                stage = "wait-for-bite-commit";
                setWaitForBite(false);
                stage = "commit-verification";
                committed = !getWaitForBite();
            }
            catch (Exception ex)
            {
                commitError = ex;
                try
                {
                    committed = !getWaitForBite();
                }
                catch (Exception verificationError)
                {
                    return new FishingBitePreparationResult(
                        false,
                        true,
                        false,
                        false,
                        stage + "+verification-unavailable",
                        FishingBitePreparationProvenance.None,
                        new AggregateException(ex, verificationError),
                        null,
                        null,
                        null);
                }
            }

            if (!committed)
            {
                return new FishingBitePreparationResult(
                    false,
                    true,
                    false,
                    false,
                    stage + "+commit-not-observed",
                    FishingBitePreparationProvenance.None,
                    commitError,
                    null,
                    null,
                    null);
            }

            FishingBitePreparationProvenance provenance = commitError == null
                ? FishingBitePreparationProvenance.OrderedCommit
                : FishingBitePreparationProvenance.ReconciledAfterWriteFault;
            Exception? tipError = TryPostCommit(invokeFishOnHookTip);
            Exception? rendererError = TryPostCommit(refreshFishingRenderer);
            bool isFish = false;
            bool fishObservationAvailable = true;
            Exception? fishObservationError = null;
            try
            {
                isFish = readIsFish();
            }
            catch (Exception ex)
            {
                fishObservationAvailable = false;
                fishObservationError = ex;
            }

            return new FishingBitePreparationResult(
                true,
                false,
                isFish,
                fishObservationAvailable,
                commitError == null ? "committed" : stage + "+committed-after-write-fault",
                provenance,
                commitError,
                tipError,
                rendererError,
                fishObservationError);
        }

        private static Exception? TryPostCommit(Action operation)
        {
            try
            {
                operation();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
