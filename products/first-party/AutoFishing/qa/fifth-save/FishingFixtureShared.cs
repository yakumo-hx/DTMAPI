using System;
using System.Globalization;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private readonly struct FishingSmokeCaseResult
        {
            internal FishingSmokeCaseResult(string scope, FixtureAttemptResult attempt)
            {
                Scope = scope ?? string.Empty;
                Attempt = attempt;
            }

            internal string Scope { get; }
            internal FixtureAttemptResult Attempt { get; }
        }

        private FishingSmokeCleanupSnapshot CaptureFishingSmokeCleanupSnapshot()
        {
            FishingPrimitivesService? primitives = fishingAutomationFeature?.Primitives;
            FishingNativeStateCache? native = primitives?.NativeStateCache;
            int nativeReferences = 0;
            if (native?.Agent != null) nativeReferences++;
            if (native?.SelectedRod != null) nativeReferences++;
            if (native?.WaitState != null) nativeReferences++;
            if (native?.MiniGameHandle != null) nativeReferences++;
            return new FishingSmokeCleanupSnapshot(
                primitives?.ActiveSessionCount ?? 0,
                primitives?.ActiveInputLeaseCount ?? 0,
                primitives?.ActiveAnimationLeaseCount ?? 0,
                fishingAutomationFeature?.HookRuntime == null ? 0 : 1,
                fishingAutomationFeature?.HookRuntimeTransientCount ?? 0,
                nativeReferences,
                fishingAutomationFeature?.ConfiguredOwnerCount ?? 0,
                fishingAutomationFeature?.ConfiguredStateCount ?? 0,
                primitives?.SchedulerPending == true,
                FishingAutomationCallbackService != null);
        }
    }

    internal readonly struct FishingSmokeCleanupSnapshot
    {
        internal FishingSmokeCleanupSnapshot(
            int sessions,
            int inputLeases,
            int animationLeases,
            int hookRuntimes,
            int nativeTransient,
            int nativeReferences,
            int legacyOptions,
            int legacyStates,
            bool schedulerPending,
            bool callbackRuntime)
        {
            Sessions = sessions;
            InputLeases = inputLeases;
            AnimationLeases = animationLeases;
            HookRuntimes = hookRuntimes;
            NativeTransient = nativeTransient;
            NativeReferences = nativeReferences;
            LegacyOptions = legacyOptions;
            LegacyStates = legacyStates;
            SchedulerPending = schedulerPending;
            CallbackRuntime = callbackRuntime;
        }

        internal int Sessions { get; }
        internal int InputLeases { get; }
        internal int AnimationLeases { get; }
        internal int HookRuntimes { get; }
        internal int NativeTransient { get; }
        internal int NativeReferences { get; }
        internal int LegacyOptions { get; }
        internal int LegacyStates { get; }
        internal bool SchedulerPending { get; }
        internal bool CallbackRuntime { get; }

        internal string Format()
        {
            return "sessions=" + Sessions.ToString(CultureInfo.InvariantCulture) +
                ", inputLeases=" + InputLeases.ToString(CultureInfo.InvariantCulture) +
                ", animationLeases=" + AnimationLeases.ToString(CultureInfo.InvariantCulture) +
                ", hookRuntimes=" + HookRuntimes.ToString(CultureInfo.InvariantCulture) +
                ", nativeTransient=" + NativeTransient.ToString(CultureInfo.InvariantCulture) +
                ", nativeReferences=" + NativeReferences.ToString(CultureInfo.InvariantCulture) +
                ", legacyOptions=" + LegacyOptions.ToString(CultureInfo.InvariantCulture) +
                ", legacyStates=" + LegacyStates.ToString(CultureInfo.InvariantCulture) +
                ", schedulerPending=" + SchedulerPending.ToString(CultureInfo.InvariantCulture) +
                ", callbackRuntime=" + CallbackRuntime.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal static class FishingSmokeCleanupAssertions
    {
        internal static void AssertCleared(string scope, FishingSmokeCleanupSnapshot snapshot)
        {
            if (snapshot.Sessions == 0 && snapshot.InputLeases == 0 && snapshot.AnimationLeases == 0 &&
                snapshot.HookRuntimes == 0 && snapshot.NativeTransient == 0 && snapshot.NativeReferences == 0 &&
                snapshot.LegacyOptions == 0 && snapshot.LegacyStates == 0 && !snapshot.SchedulerPending &&
                !snapshot.CallbackRuntime)
            {
                return;
            }

            throw new InvalidOperationException(scope + " cleanup retained fishing state. " + snapshot.Format());
        }

        internal static void AssertClearedPreservingLegacyOwners(
            string scope,
            FishingSmokeCleanupSnapshot before,
            FishingSmokeCleanupSnapshot after)
        {
            if (after.Sessions == 0 && after.InputLeases == 0 && after.AnimationLeases == 0 &&
                after.HookRuntimes == 0 && after.NativeTransient == 0 && after.NativeReferences == 0 &&
                after.LegacyOptions == before.LegacyOptions && after.LegacyStates == before.LegacyStates &&
                !after.SchedulerPending && !after.CallbackRuntime)
            {
                return;
            }

            throw new InvalidOperationException(
                scope + " cleanup did not preserve the pre-existing compatibility owner baseline. before={" +
                before.Format() + "}, after={" + after.Format() + "}");
        }
    }
}
