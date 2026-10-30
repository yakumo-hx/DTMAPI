namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private AutoFishingPrimitiveSmokeCase? autoFishingPrimitiveSmokeCase;

        private FixtureAttemptResult TryExerciseAutoFishingPhaseForFixture()
        {
            autoFishingPrimitiveSmokeCase ??= new AutoFishingPrimitiveSmokeCase(this);
            return autoFishingPrimitiveSmokeCase.ExerciseLoop().Attempt;
        }

        private FixtureAttemptResult TryExerciseAutoFishingMovementCancelForFixture()
        {
            autoFishingPrimitiveSmokeCase ??= new AutoFishingPrimitiveSmokeCase(this);
            return autoFishingPrimitiveSmokeCase.ExerciseMovementCancel().Attempt;
        }

        private FixtureAttemptResult TryExerciseAutoFishingNativeControlForFixture()
        {
            autoFishingPrimitiveSmokeCase ??= new AutoFishingPrimitiveSmokeCase(this);
            return autoFishingPrimitiveSmokeCase.ExerciseNativeControl().Attempt;
        }

        private FixtureAttemptResult TryExerciseAutoFishingDisableRecoveryForFixture()
        {
            autoFishingPrimitiveSmokeCase ??= new AutoFishingPrimitiveSmokeCase(this);
            return autoFishingPrimitiveSmokeCase.ExerciseDisableRecovery().Attempt;
        }

        private FixtureAttemptResult TryExerciseAutoFishingTitleCycleForFixture()
        {
            autoFishingPrimitiveSmokeCase ??= new AutoFishingPrimitiveSmokeCase(this);
            return autoFishingPrimitiveSmokeCase.ExerciseTitleCycle().Attempt;
        }

        private sealed class AutoFishingPrimitiveSmokeCase
        {
            private readonly QaScenarioController bridge;

            internal AutoFishingPrimitiveSmokeCase(QaScenarioController bridge)
            {
                this.bridge = bridge;
            }

            internal FishingSmokeCaseResult ExerciseLoop()
            {
                return new FishingSmokeCaseResult("first-party-primitives", bridge.TryExerciseAutoFishingPrimitiveCore());
            }

            internal FishingSmokeCaseResult ExerciseMovementCancel()
            {
                return new FishingSmokeCaseResult("first-party-primitives-movement", bridge.TryExerciseAutoFishingMovementCancelPrimitiveCore());
            }

            internal FishingSmokeCaseResult ExerciseNativeControl()
            {
                return new FishingSmokeCaseResult("first-party-primitives-native-control", bridge.TryExerciseAutoFishingNativeControlCore());
            }

            internal FishingSmokeCaseResult ExerciseDisableRecovery()
            {
                return new FishingSmokeCaseResult("first-party-primitives-disable-recovery", bridge.TryExerciseAutoFishingPrimitiveCore());
            }

            internal FishingSmokeCaseResult ExerciseTitleCycle()
            {
                return new FishingSmokeCaseResult("first-party-primitives-title-cycle", bridge.TryExerciseAutoFishingPrimitiveCore());
            }
        }
    }
}
