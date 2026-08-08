using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Yuuka.DTMAPI.AutoFishing;

namespace DTMAPI.UnitTests
{
    internal static class AutoFishingProductTests
    {
        internal static void RunAll()
        {
            CurrentNativeMovementPolicyIsDeviceIndependentAndFailClosed();
            MovementNeutralArmingDefersActionsWithoutABlindWindow();
            InstantBiteWritesCommitBitLastAndFaultClosesEveryUncertainStage();
            InstantBitePostCommitFailuresCannotReverseTheCommit();
            InstantBiteDiagnosticsCountOnlyProductCommitProvenance();
            ProductSourceRetainsCurrentNativeAndAtomicHookBoundaries();
        }

        private static void CurrentNativeMovementPolicyIsDeviceIndependentAndFailClosed()
        {
            Assert(FishingNativeMovementPolicy.ShouldCancel(false, 0d, 0d, 0d, out string unavailable) && unavailable == "native-movement-unavailable",
                "Missing current native movement accessors must fail closed.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, double.NaN, 0d, 0d, out string invalid) && invalid == "native-movement-invalid",
                "Invalid current native movement values must fail closed.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, 0d, double.PositiveInfinity, 0d, out string invalidVelocity) && invalidVelocity == "native-movement-invalid",
                "A non-finite current native VelocityX must fail closed.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, 1d, 0.1d, 0.1d, out string input) && input.StartsWith("manual-move inputMultiplier=", StringComparison.Ordinal),
                "MoveModifier.inputMultiplier must own the first device-independent native movement branch.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, 0d, -0.0011d, 0.1d, out string velocity) && velocity.StartsWith("native-move VelocityX=", StringComparison.Ordinal),
                "VelocityX beyond the native Wait threshold must own the second branch before OffsetX.");
            double nativeThreshold = FishingNativeMovementPolicy.NativePreBaseVelocityThreshold;
            Assert(!FishingNativeMovementPolicy.ShouldCancel(true, 0d, nativeThreshold, 0d, out string threshold) && threshold.Length == 0,
                "The native Wait pre-base VelocityX comparison is strictly greater than its exact 0.001f threshold.");
            Assert(!FishingNativeMovementPolicy.ShouldCancel(true, 0d, -nativeThreshold, 0d, out string negativeThreshold) && negativeThreshold.Length == 0,
                "The native Wait absolute VelocityX comparison must accept the exact negative 0.001f threshold too.");
            Assert(!FishingNativeMovementPolicy.ShouldCancel(true, 0d, 0.0000001d, 0d, out string tinyPositive) && tinyPositive.Length == 0,
                "A tiny pre-base positive Rigidbody residual with exact-zero OffsetX must follow the native Wait threshold.");
            Assert(!FishingNativeMovementPolicy.ShouldCancel(true, 0d, -0.0000001d, 0d, out string tinyNegative) && tinyNegative.Length == 0,
                "A tiny pre-base negative Rigidbody residual with exact-zero OffsetX must follow the native Wait threshold.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, 0d, 0.0000001d, -0.0000001d, out string offset) &&
                offset == "native-offset OffsetX=-1E-07",
                "Any finite nonzero OffsetX must match the native post-base exact-nonzero cancellation boundary.");
            Assert(FishingNativeMovementPolicy.ShouldCancel(true, 0d, 0d, double.PositiveInfinity, out string invalidOffset) && invalidOffset == "native-movement-invalid",
                "An invalid current native OffsetX must fail closed.");
            Assert(!FishingNativeMovementPolicy.ShouldCancel(true, 0d, 0d, 0d, out string idle) && idle.Length == 0,
                "A valid idle native movement snapshot must keep automation active.");
        }

        private static void MovementNeutralArmingDefersActionsWithoutABlindWindow()
        {
            FishingNativeMovementGateResult movingAtEnable = FishingNativeMovementPolicy.Evaluate(false, true, 1d, 0d, 0d);
            Assert(!movingAtEnable.Armed && !movingAtEnable.ShouldCancel && movingAtEnable.DeferActions &&
                movingAtEnable.Reason.StartsWith("neutral-arming manual-move", StringComparison.Ordinal),
                "Enabling while movement is active must defer every product action until neutral without immediately toggling the product off.");

            FishingNativeMovementGateResult neutral = FishingNativeMovementPolicy.Evaluate(movingAtEnable.Armed, true, 0d, -0.0000001d, 0d);
            Assert(neutral.Armed && !neutral.ShouldCancel && !neutral.DeferActions && neutral.Reason == "neutral-armed",
                "The first native-parity neutral snapshot must arm cancellation immediately and permit actions on that snapshot.");

            FishingNativeMovementGateResult movedAfterArm = FishingNativeMovementPolicy.Evaluate(neutral.Armed, true, 0d, 0.0000001d, 0.0000001d);
            Assert(movedAfterArm.Armed && movedAfterArm.ShouldCancel && movedAfterArm.DeferActions &&
                movedAfterArm.Reason == "native-offset OffsetX=1E-07",
                "A native post-base OffsetX after neutral arming must cancel without a time-based grace window.");

            Assert(FishingNativeMovementPolicy.Evaluate(false, false, 0d, 0d, 0d).ShouldCancel &&
                FishingNativeMovementPolicy.Evaluate(false, true, double.NaN, 0d, 0d).ShouldCancel,
                "Unavailable or invalid native movement must fail closed even before neutral arming.");
        }

        private static void InstantBiteWritesCommitBitLastAndFaultClosesEveryUncertainStage()
        {
            BiteCommitFixture probability = BiteCommitFixture.FailBefore("hook-probability");
            AssertFaultClosed(probability, "hook-probability");
            Assert(probability.HookProbability == 0.25f && probability.Duration == 0f && !probability.HasRolled && probability.Waiting,
                "Probability failure must leave every later Wait field untouched.");

            BiteCommitFixture duration = BiteCommitFixture.FailBefore("fish-on-hook-duration");
            AssertFaultClosed(duration, "fish-on-hook-duration");
            Assert(duration.HookProbability == 1f && duration.Duration == 0f && !duration.HasRolled && duration.Waiting,
                "Duration failure must occur before hasRolled and the wait commit bit.");

            BiteCommitFixture hasRolled = BiteCommitFixture.FailBefore("has-rolled");
            AssertFaultClosed(hasRolled, "has-rolled");
            Assert(hasRolled.HookProbability == 1f && hasRolled.Duration == 2f && !hasRolled.HasRolled && hasRolled.Waiting,
                "hasRolled failure must retain a native-rerollable Wait state.");

            BiteCommitFixture commit = BiteCommitFixture.FailBefore("wait-for-bite-commit");
            AssertFaultClosed(commit, "wait-for-bite-commit");
            Assert(commit.HookProbability == 1f && commit.Duration == 2f && commit.HasRolled && commit.Waiting,
                "Commit failure must leave the native self-reconcile fields complete before wait remains true.");

            var ignoredCommit = new BiteCommitFixture { IgnoreCommitWrite = true };
            FishingBitePreparationResult ignored = ignoredCommit.Execute();
            Assert(!ignored.Committed && ignored.RequiresFaultClose && ignored.Stage.Contains("commit-not-observed", StringComparison.Ordinal),
                "A non-throwing commit setter that does not change native Wait must fault close.");

            var unavailableVerification = new BiteCommitFixture { VerificationAlwaysThrows = true };
            FishingBitePreparationResult uncertain = unavailableVerification.Execute();
            Assert(!uncertain.Committed && uncertain.RequiresFaultClose && uncertain.Stage.Contains("verification-unavailable", StringComparison.Ordinal),
                "An unobservable post-roll commit must fault close even when the write likely happened.");

            BiteCommitFixture afterWrite = BiteCommitFixture.FailAfter("wait-for-bite-commit");
            FishingBitePreparationResult afterWriteResult = afterWrite.Execute();
            Assert(afterWriteResult.Committed && !afterWriteResult.RequiresFaultClose && afterWriteResult.CommitError != null && !afterWrite.Waiting &&
                afterWriteResult.Provenance == FishingBitePreparationProvenance.ReconciledAfterWriteFault,
                "An after-write commit exception must reconcile from the observed false wait bit instead of reversing success.");

            var normal = new BiteCommitFixture();
            FishingBitePreparationResult success = normal.Execute();
            Assert(success.Committed && !success.RequiresFaultClose && success.Stage == "committed" &&
                success.Provenance == FishingBitePreparationProvenance.OrderedCommit &&
                normal.HookProbability == 1f && normal.Duration == 2f && normal.HasRolled && !normal.Waiting,
                "The normal InstantBite field transaction must commit exactly once with wait=false last.");
        }

        private static void InstantBiteDiagnosticsCountOnlyProductCommitProvenance()
        {
            var diagnostics = new FishingQaDiagnostics();
            diagnostics.RecordNativeBitePrepared(FishingBitePreparationProvenance.AlreadyNativeCommitted);
            Assert(diagnostics.NativeBitePreparedCount == 1 && diagnostics.InstantBiteCommittedCount == 0 &&
                diagnostics.LastNativeBiteProvenance == nameof(FishingBitePreparationProvenance.AlreadyNativeCommitted),
                "A native bite committed before the product transaction must not count as an InstantBite product commit.");

            diagnostics.RecordNativeBitePrepared(FishingBitePreparationProvenance.OrderedCommit);
            diagnostics.RecordNativeBitePrepared(FishingBitePreparationProvenance.ReconciledAfterWriteFault);
            Assert(diagnostics.NativeBitePreparedCount == 3 && diagnostics.InstantBiteCommittedCount == 2 &&
                diagnostics.LastNativeBiteProvenance == nameof(FishingBitePreparationProvenance.ReconciledAfterWriteFault),
                "Only ordered and after-write-reconciled product commits may advance the dedicated InstantBite evidence counter.");
        }

        private static void InstantBitePostCommitFailuresCannotReverseTheCommit()
        {
            var fixture = new BiteCommitFixture
            {
                TipThrows = true,
                RendererThrows = true,
                FishObservationThrows = true
            };
            FishingBitePreparationResult result = fixture.Execute();
            Assert(result.Committed && !result.RequiresFaultClose && !fixture.Waiting,
                "Post-commit failures must not reverse the native bite commit.");
            Assert(result.PostCommitFailureCount == 3 && result.TipError != null && result.RendererError != null && result.FishObservationError != null,
                "Tip, renderer and fish observation failures must remain separately diagnosable.");
            Assert(!result.FishObservationAvailable && !result.IsFish,
                "An unavailable fish observation must be explicit while native NextState remains authoritative.");
            Assert(fixture.TipCalls == 1 && fixture.RendererCalls == 1 && fixture.FishObservationCalls == 1,
                "Each independent post-commit stage must still run once after an earlier cosmetic failure.");
        }

        private static void ProductSourceRetainsCurrentNativeAndAtomicHookBoundaries()
        {
            string repo = FindRepositoryRoot();
            string sourceRoot = Path.Combine(repo, "products", "first-party", "AutoFishing", "src");
            string state = File.ReadAllText(Path.Combine(sourceRoot, "Native", "FishingNativeStateCache.cs"));
            string transaction = File.ReadAllText(Path.Combine(sourceRoot, "Native", "FishingNativeTransactionCache.cs"));
            string primitives = File.ReadAllText(Path.Combine(sourceRoot, "Native", "FishingPrimitivesService.cs"));
            string entry = File.ReadAllText(Path.Combine(sourceRoot, "ModEntry.cs"));
            string hooks = File.ReadAllText(Path.Combine(sourceRoot, "Native", "FishingProductHookInstaller.cs"));
            string allSource = string.Join("\n", Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert(state.Contains("FindMember(type, \"MoveModifier\")", StringComparison.Ordinal) &&
                state.Contains("FindMember(type, \"inputMultiplier\")", StringComparison.Ordinal) &&
                state.Contains("FindMember(type, \"VelocityX\")", StringComparison.Ordinal) &&
                state.Contains("FindMember(type, \"OffsetX\")", StringComparison.Ordinal),
                "The current Product state cache must bind the exact current native movement owners.");
            Assert(!allSource.Contains("HorizontalMoveFactor", StringComparison.Ordinal) &&
                !allSource.Contains("FallbackManualCancelKeys", StringComparison.Ordinal) &&
                !allSource.Contains("manualCancelKeyButtons", StringComparison.Ordinal),
                "Current Product source must not retain the removed native member or hard-coded keyboard fallback.");
            Assert(transaction.Contains("FishingBitePreparationTransaction.Commit(", StringComparison.Ordinal) &&
                primitives.Contains("native-bite-fault-closed", StringComparison.Ordinal) &&
                entry.Contains("SetAutomation(false, result.Status)", StringComparison.Ordinal),
                "AF-D2 must propagate an explicit Product fault-close result through the session owner.");
            Assert(entry.Contains("movementCancelArmed", StringComparison.Ordinal) &&
                entry.Contains("movement.DeferActions", StringComparison.Ordinal) &&
                !entry.Contains("manualCancelEnabledAtUtc", StringComparison.Ordinal) &&
                !entry.Contains("AddSeconds(1)", StringComparison.Ordinal),
                "Movement cancellation must use native-parity neutral arming instead of a time-based blind window.");
            Assert(Regex.Matches(hooks, @"(?m)^\s+(?:Prefix|Postfix)\(").Count == 22,
                "The current Product must retain its exact 22-Hook atomic plan.");
        }

        private static void AssertFaultClosed(BiteCommitFixture fixture, string stage)
        {
            FishingBitePreparationResult result = fixture.Execute();
            Assert(!result.Committed && result.RequiresFaultClose && result.Provenance == FishingBitePreparationProvenance.None &&
                result.CommitError != null && result.Stage.Contains(stage, StringComparison.Ordinal),
                "Injected " + stage + " failure must produce an explicit fault-close result.");
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "PROJECT.md")))
                    return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Could not locate the DTMAPI repository root.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class BiteCommitFixture
        {
            private string failureStage = string.Empty;
            private bool failAfterWrite;

            internal float HookProbability { get; private set; } = 0.25f;
            internal float Duration { get; private set; }
            internal bool HasRolled { get; private set; }
            internal bool Waiting { get; private set; } = true;
            internal bool IgnoreCommitWrite { get; set; }
            internal bool VerificationAlwaysThrows { get; set; }
            internal bool TipThrows { get; set; }
            internal bool RendererThrows { get; set; }
            internal bool FishObservationThrows { get; set; }
            internal int TipCalls { get; private set; }
            internal int RendererCalls { get; private set; }
            internal int FishObservationCalls { get; private set; }

            internal static BiteCommitFixture FailBefore(string stage) => new BiteCommitFixture { failureStage = stage };
            internal static BiteCommitFixture FailAfter(string stage) => new BiteCommitFixture { failureStage = stage, failAfterWrite = true };

            internal FishingBitePreparationResult Execute()
            {
                return FishingBitePreparationTransaction.Commit(
                    2f,
                    ReadWaiting,
                    value => Write("hook-probability", () => HookProbability = value),
                    value => Write("fish-on-hook-duration", () => Duration = value),
                    value => Write("has-rolled", () => HasRolled = value),
                    value => Write("wait-for-bite-commit", () =>
                    {
                        if (!IgnoreCommitWrite)
                            Waiting = value;
                    }),
                    () =>
                    {
                        TipCalls++;
                        if (TipThrows)
                            throw new InvalidOperationException("tip-failure");
                    },
                    () =>
                    {
                        RendererCalls++;
                        if (RendererThrows)
                            throw new InvalidOperationException("renderer-failure");
                    },
                    () =>
                    {
                        FishObservationCalls++;
                        if (FishObservationThrows)
                            throw new InvalidOperationException("fish-observation-failure");
                        return true;
                    });
            }

            private bool ReadWaiting()
            {
                if (VerificationAlwaysThrows)
                    throw new InvalidOperationException("verification-failure");
                return Waiting;
            }

            private void Write(string stage, Action write)
            {
                if (failureStage.Equals(stage, StringComparison.Ordinal) && !failAfterWrite)
                    throw new InvalidOperationException(stage + "-failure");
                write();
                if (failureStage.Equals(stage, StringComparison.Ordinal) && failAfterWrite)
                    throw new InvalidOperationException(stage + "-after-write-failure");
            }
        }
    }
}
