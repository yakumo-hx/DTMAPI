#pragma warning disable CS0618 // QA tests intentionally exercise frozen compatibility APIs.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.GameBridge.DolocTown.QA;
using DTMAPI.ModConfigMenu;
using DTMAPI.Testing;

namespace DTMAPI.QaUnitTests
{
    internal static class Program
    {
        private static int Main()
        {
            using DtmApiTestSession testSession = DtmApiTestSession.Start("DTMAPI.QaUnitTests");
            try
            {
                FishingPerformanceProbeUsesRealHundredFishTarget();
                InactiveNoConsumerProbeMeasuresExactWarmedFrameWindow();
                RuntimeMemoryTrendProbeKeepsBoundedIndependentSamples();
                RuntimeMemoryTrendProbeSamplesSixtyHertzObservationAtConfiguredCadence();
                QaSettingsCanonicalizeProfileCase();
                SaveFixtureIsolationSettingsRequireExactModeAndSaveRoot();
                Batch5NoDemandSettingsRequireExplicitPositiveBoundary();
                Batch6AutoFishingPilotSettingsRequireSingleFifthSaveCaseAndHashes();
                Batch6AutoFishingManagerLifecycleIsRetiredBeforeLegacySourceMutation();
                Batch6AutoFishingObserverReadsTheCoreResidentEntryShapeWithoutAProductReference();
                Batch6AutoFishingDirectNeutralPreflightRequiresUntouchedSamePositionNativeParityNeutral();
                Batch6AutoFishingNativeSurfaceDiagnosticsReadDirectOwnerState();
                Batch6AutoFishingL0RequiresIndependentNativeDriver();
                Batch6AutoFishingL4UsesSeparateReflectedProductRecoveryOwner();
                Batch6AutoFishingL5UsesQaOwnedTitleAndReentryLifecycle();
                Batch6AutoFishingBehaviorDiscardsOnlyOneNonProductInitialLoop();
                QaParticipantOwnsHundredFishMeasurementAndTerminalResult();
                AutoFishingNativeControlPolicyAdvancesLiveNonNormalSessions();
                AutoFishingNativeVitalsUseOnlyOfficialCommandDelegatesAndFailClosed();
                Batch5AutoFishingL0RejectsPullExitWithoutVisibleReelReceipt();
                Batch5AutoFishingL4AndL5RequireBehaviorReceipts();
                Batch5ActionSpeedGcLadderRequiresFullActiveWindowAndLevelSemantics();
                GenericQaParticipantDoesNotOwnAutoFishingPerformance();
                QaPerformanceWritesAreRetryableBeforeTerminalCommit();
                G3CustomEntityContractIsOwnerBoundReadOnlyAndDisposable();
                G3LifecycleNotificationsAndCallbackOrderAreDeterministic();
                G3FishRoeObservationAndRunnerProjectionAreQaOwned();
                G4ContinuousHomePageRequiresUninterruptedWindow();
                ScenarioStableObservationRequiresContinuousIndependentWindows();
                G4ReturnHomeWaitsForPostUiGameplayQuiescence();
                G4FixtureSessionsRetainReceiptsUntilVerifiedCleanup();
                G4NativeUiCloseUsesExactGenericRemoveReceipt();
                G4NativeUiTargetedReceiptsUseExactRepairHooks();
                G4BridgeDoesNotReplayStartupTitleAcrossSaveLoaded();
                G4ContinuousHomePageGenerationWaitsForSaveReturnBoundary();
                G4TitleOnlyCasesEnterPostTitleWithoutSaveLoaded();
                G4ReturnedToTitleCleansAndBlocksInSaveCasesBeforePostTitleObservation();
                G4OverlaySessionsAreDistinctReceipts();
                G4ManagerTitleRoutesAreMutuallyExclusiveAndQaOwned();
                G4SourceAndProjectBoundariesAreClosed();
                MoreEquipmentSlotsColdObserverUsesSuppliedBaseline();
                G5WorldMutationRoutingAndRestorationAreClosed();
                G6LifecycleRoutingAndCallbackBoundaryAreClosed();
                G6FailureCloseReleasesSyntheticOwnersAndCameraLeases();
                CameraPlayableSuppressesSynchronousRuntimeReentry();
                MigratedTypesExistOnlyInOptionalQaAssembly();
                AutoFishingQaSourcesAreProductOwnedAndProductionExcluded();
                testSession.MarkSucceeded();
                Console.WriteLine("DTMAPI.QaUnitTests: OK");
                return 0;
            }
            catch (Exception ex)
            {
                testSession.MarkFailed(ex);
                Console.Error.WriteLine("DTMAPI.QaUnitTests: FAILED");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void FishingPerformanceProbeUsesRealHundredFishTarget()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int processGen0 = 10;
            var unavailableTrend = new RuntimeMemoryTrendProbe(
                TimeSpan.FromMilliseconds(100),
                captureProcess: () => new RuntimeProcessMemorySnapshot(100, 80, processGen0++, 2, 1));
            var unavailable = new FishingPerformanceProbe(
                "Unavailable",
                0,
                0,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                0,
                now,
                null,
                null,
                unavailableTrend);
            Assert(unavailable.Observe(0, now) == FishingPerformanceProbeUpdate.MeasurementStarted &&
                unavailable.Result.AllocationProbeStatus == "blocked-allocation-counter-unavailable",
                "An unavailable same-thread allocation API must block only that subprobe.");
            Assert(unavailable.Observe(0, now.AddSeconds(1)) == FishingPerformanceProbeUpdate.Completed &&
                unavailable.Result.RuntimeMemoryTrend.Status == "completed",
                "Independent runtime memory measurement must still complete when allocation capability is unavailable.");

            long allocationCounter = 1000;
            var target = new FishingPerformanceProbe(
                "FishLoop",
                100,
                5,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                10,
                now,
                () => allocationCounter,
                () => allocationCounter += 4096);
            Assert(target.Observe(10, now) == FishingPerformanceProbeUpdate.None, "The real target probe must begin from its initial PullExit count.");
            for (int warmup = 1; warmup < 5; warmup++)
                Assert(target.Observe(10 + warmup, now.AddSeconds(warmup)) == FishingPerformanceProbeUpdate.None, "The probe must not start before all five warm-up fish.");
            Assert(target.Observe(15, now.AddSeconds(5)) == FishingPerformanceProbeUpdate.MeasurementStarted, "The fifth warm-up fish must start measurement.");
            for (int measured = 1; measured < 100; measured++)
                Assert(target.Observe(15 + measured, now.AddSeconds(5 + measured)) == FishingPerformanceProbeUpdate.None, "The 100-fish target must not terminate early at measured=" + measured + ".");
            allocationCounter += 1234;
            Assert(target.Observe(115, now.AddSeconds(105)) == FishingPerformanceProbeUpdate.Completed &&
                target.Result.TargetFish == 100 && target.Result.WarmupFish == 5 && target.Result.MeasuredFish == 100,
                "The positive probe must terminate exactly at 100 measured fish after five warm-up fish.");
        }

        private static void InactiveNoConsumerProbeMeasuresExactWarmedFrameWindow()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            const int warmupFrames = 3;
            const int targetFrames = 10;
            var probe = new FishingPerformanceProbe(
                "InactiveNoConsumer",
                0,
                0,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                0,
                now,
                () => 1000,
                () => { },
                runtimeMemoryTrendProbe: new RuntimeMemoryTrendProbe(),
                positiveMinimumMeasure: TimeSpan.Zero,
                targetFrames: targetFrames,
                warmupFrames: warmupFrames);

            for (int frame = 0; frame < warmupFrames; frame++)
            {
                Assert(probe.Observe(0, now.AddMilliseconds(frame)) == FishingPerformanceProbeUpdate.None,
                    "The frame-target profile must not start before all requested warm-up observations.");
            }
            Assert(probe.Observe(0, now.AddMilliseconds(warmupFrames)) == FishingPerformanceProbeUpdate.MeasurementStarted,
                "The first observation after the complete warm-up must establish the measurement baseline without counting as a measured frame.");
            for (int frame = 1; frame < targetFrames; frame++)
            {
                Assert(probe.Observe(0, now.AddMilliseconds(warmupFrames + frame)) == FishingPerformanceProbeUpdate.None,
                    "The frame-target profile must not terminate early at measured frame " + frame + ".");
            }
            Assert(probe.Observe(0, now.AddMilliseconds(warmupFrames + targetFrames)) == FishingPerformanceProbeUpdate.Completed &&
                probe.Result.WarmupFrames == warmupFrames &&
                probe.Result.WarmupFramesActual == warmupFrames &&
                probe.Result.TargetFrames == targetFrames &&
                probe.Result.MeasuredFrames == targetFrames,
                "InactiveNoConsumer must publish exact warm-up and measured frame target/actual values.");
            Assert(probe.Observe(0, now.AddMilliseconds(warmupFrames + targetFrames + 1)) == FishingPerformanceProbeUpdate.None &&
                probe.Result.MeasuredFrames == targetFrames,
                "The terminal frame receipt must be immutable after exact completion.");
        }

        private static void RuntimeMemoryTrendProbeKeepsBoundedIndependentSamples()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int sample = 0;
            var probe = new RuntimeMemoryTrendProbe(
                TimeSpan.FromSeconds(1),
                capturePlatform: () => new RuntimePlatformMemorySnapshot(100 + sample, 200 + sample, 300 + sample, 400 + sample, 50),
                captureDomain: () => new RuntimeMemoryDomainSnapshot(1 + sample, 2, 3, 4 + sample, 5),
                captureProcess: () => new RuntimeProcessMemorySnapshot(500 + sample, 600 + sample, sample++, 1, 0),
                maxSamples: 4);
            probe.Start(now);
            for (int second = 1; second <= 6; second++)
                probe.Observe(now.AddSeconds(second));
            probe.Complete(now.AddSeconds(7));
            RuntimeMemoryTrendResult result = probe.Result;
            Assert(result.Status == "completed" && result.Samples.Count == 4 && result.TrimmedSamples >= 4,
                "Runtime memory trend samples must remain bounded while retaining a completed result.");
            Assert(result.ProcessPrivate.Start.HasValue && result.ProcessPrivate.End.HasValue &&
                result.DtmApiRecordCount.Delta.HasValue && result.ProcessGen0Collections.Delta.HasValue,
                "Process, domain, and GC trends must remain independent typed metrics after extraction.");
        }

        private static void RuntimeMemoryTrendProbeSamplesSixtyHertzObservationAtConfiguredCadence()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int domainCaptures = 0;
            var probe = new RuntimeMemoryTrendProbe(
                TimeSpan.FromSeconds(30),
                captureDomain: () =>
                {
                    domainCaptures++;
                    return new RuntimeMemoryDomainSnapshot(
                        2,
                        3,
                        4,
                        5,
                        6,
                        resourceRecordCount: 2,
                        resourceSnapshotBuilds: 100 + domainCaptures);
                },
                captureProcess: () => new RuntimeProcessMemorySnapshot(500, 600, 1, 1, 0));

            probe.Start(now);
            for (int frame = 1; frame <= 36000; frame++)
                probe.Observe(now.AddTicks((TimeSpan.TicksPerSecond * frame) / 60));
            probe.Complete(now.AddSeconds(600));

            Assert(domainCaptures == 21 && probe.Result.Samples.Count == 21 && probe.Result.TrimmedSamples == 0 &&
                probe.Result.ResourceSnapshotBuilds.Delta == 20,
                "A 60 Hz fixture observation over 600 seconds must invoke diagnostic domain capture only at the 30-second cadence, including start and completion samples.");
        }

        private static void QaSettingsCanonicalizeProfileCase()
        {
            foreach (var item in new[]
            {
                new { Input = "fishloop", Expected = "FishLoop", Target = 100 },
                new { Input = "inactivenoconsumer", Expected = "InactiveNoConsumer", Target = 0 },
                new { Input = "enablednorod", Expected = "EnabledNoRod", Target = 0 }
            })
            {
                var settings = new AutoFishingQaSettings
                {
                    AutoFishingPerformanceEnabled = true,
                    AutoFishingPerformanceProfile = item.Input,
                    AutoFishingPerformanceTargetFish = item.Target,
                    AutoFishingPerformanceWarmupFish = 5,
                    AutoFishingPerformanceZeroWarmupSeconds = 0,
                    AutoFishingPerformanceZeroMeasureSeconds = 1
                };
                settings.NormalizeAndValidate();
                Assert(settings.AutoFishingPerformanceProfile == item.Expected,
                    "Product QA settings must canonicalize case-insensitive profile " + item.Input + " to " + item.Expected + ".");
            }
        }

        private static void Batch6AutoFishingPilotSettingsRequireSingleFifthSaveCaseAndHashes()
        {
            const string runId = "b6afb6afb6afb6afb6afb6afb6afb6af";
            string lowerHash = new string('a', 64);
            byte[] validBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 5,
                G6LifecycleCases = new[] { Batch6AutoFishingPilotSettings.CaseId },
                Batch6AutoFishingPilot = new
                {
                    Enabled = true,
                    Level = "l3",
                    Scenario = "combinedinstantskip",
                    MeasureSeconds = 600,
                    SampleSeconds = 30,
                    WarmupFish = 5,
                    TargetFish = 10,
                    Multiplier = 3d,
                    Formal = true,
                    FormalContract = "longrun",
                    ExpectedPackageSha256 = lowerHash,
                    ExpectedEntryDllSha256 = lowerHash,
                    ExpectedManifestSha256 = lowerHash,
                    ExpectedReferencePolicySha256 = lowerHash
                }
            }));
            QaHostSettings valid = QaHostSettings.Read(validBytes);
            valid.Validate(runId);
            Assert(valid.Batch6AutoFishingPilot.Enabled &&
                valid.Batch6AutoFishingPilot.Level == "L3" &&
                valid.Batch6AutoFishingPilot.Scenario == "CombinedInstantSkip" &&
                valid.Batch6AutoFishingPilot.ToggleKey == "F6" &&
                valid.Batch6AutoFishingPilot.IsFormalLongRunContract &&
                valid.Batch6AutoFishingPilot.AuthorityScope == "formal-long-run" &&
                valid.Batch6AutoFishingPilot.ExpectedEntryDllSha256 == new string('A', 64),
                "Batch6AutoFishingPilot settings must canonicalize level/scenario/hashes without weakening exact receipt binding.");

            byte[] mixedCases = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 5,
                G6LifecycleCases = new[] { Batch6AutoFishingPilotSettings.CaseId, "ModOwnerLifetime" },
                Batch6AutoFishingPilot = new
                {
                    Enabled = true,
                    Level = "L1",
                    Scenario = "DefaultLoop",
                    MeasureSeconds = 600,
                    SampleSeconds = 30,
                    WarmupFish = 5,
                    TargetFish = 10,
                    Multiplier = 3d,
                    ExpectedPackageSha256 = lowerHash,
                    ExpectedEntryDllSha256 = lowerHash,
                    ExpectedManifestSha256 = lowerHash,
                    ExpectedReferencePolicySha256 = lowerHash
                }
            }));
            AssertThrows<InvalidDataException>(() => QaHostSettings.Read(mixedCases).Validate(runId),
                "Batch6AutoFishingPilot must reject any second G6 case in the same transaction.");

            var invalidHash = new Batch6AutoFishingPilotSettings
            {
                Enabled = true,
                Level = "L1",
                Scenario = "DefaultLoop",
                ExpectedPackageSha256 = "not-a-hash",
                ExpectedEntryDllSha256 = lowerHash,
                ExpectedManifestSha256 = lowerHash,
                ExpectedReferencePolicySha256 = lowerHash
            };
            AssertThrows<InvalidDataException>(() => invalidHash.NormalizeAndValidate(caseSelected: true, saveSlot: 5),
                "Batch6AutoFishingPilot must reject a non-SHA package identity before host activation.");

            var invalidFormal = new Batch6AutoFishingPilotSettings
            {
                Enabled = true,
                Level = "L1",
                Scenario = "DefaultLoop",
                MeasureSeconds = 599,
                SampleSeconds = 30,
                WarmupFish = 5,
                TargetFish = 10,
                Formal = true,
                ExpectedPackageSha256 = lowerHash,
                ExpectedEntryDllSha256 = lowerHash,
                ExpectedManifestSha256 = lowerHash,
                ExpectedReferencePolicySha256 = lowerHash
            };
            AssertThrows<InvalidDataException>(() => invalidFormal.NormalizeAndValidate(caseSelected: true, saveSlot: 5),
                "Formal Batch6AutoFishingPilot must reject any duration/count tuple other than exact 600/30/5/10.");

            var formalBehavior = new Batch6AutoFishingPilotSettings
            {
                Enabled = true,
                Level = "L1",
                Scenario = "FastAnimations",
                MeasureSeconds = 1,
                SampleSeconds = 1,
                WarmupFish = 0,
                TargetFish = 1,
                Multiplier = 3,
                CastChargeRatio = 0.5,
                Formal = true,
                FormalContractKind = "behavior",
                ExpectedPackageSha256 = lowerHash,
                ExpectedEntryDllSha256 = lowerHash,
                ExpectedManifestSha256 = lowerHash,
                ExpectedReferencePolicySha256 = lowerHash
            };
            formalBehavior.NormalizeAndValidate(caseSelected: true, saveSlot: 5);
            Assert(formalBehavior.IsFormalBehaviorContract && !formalBehavior.IsFormalLongRunContract &&
                formalBehavior.AuthorityScope == "formal-behavior-profile" && Math.Abs(formalBehavior.CastChargeRatio - 0.5d) < 0.000001d,
                "Formal Behavior must be an explicit short authority and must never satisfy the distinct LongRun contract.");
            formalBehavior.MeasureSeconds = 2;
            AssertThrows<InvalidDataException>(() => formalBehavior.NormalizeAndValidate(caseSelected: true, saveSlot: 5),
                "Formal Behavior must reject any tuple other than exact 1/1/0/1.");

            var manualMovementBehavior = new Batch6AutoFishingPilotSettings
            {
                Enabled = true,
                Level = "L1",
                Scenario = "DefaultLoop",
                MeasureSeconds = 1,
                SampleSeconds = 1,
                WarmupFish = 0,
                TargetFish = 1,
                Multiplier = 1,
                Formal = true,
                FormalContractKind = "Behavior",
                ManualMovementCancel = true,
                ToggleKey = "f7",
                ExpectedPackageSha256 = lowerHash,
                ExpectedEntryDllSha256 = lowerHash,
                ExpectedManifestSha256 = lowerHash,
                ExpectedReferencePolicySha256 = lowerHash
            };
            manualMovementBehavior.NormalizeAndValidate(caseSelected: true, saveSlot: 5);
            Assert(manualMovementBehavior.ToggleKey == "F7",
                "Formal manual movement must preserve the actual rebound F7 binding.");
            manualMovementBehavior.ToggleKey = "F6";
            AssertThrows<InvalidDataException>(() => manualMovementBehavior.NormalizeAndValidate(caseSelected: true, saveSlot: 5),
                "Formal manual movement must reject the old default F6 path instead of claiming a rebound test.");
        }

        private static void
            SaveFixtureIsolationSettingsRequireExactModeAndSaveRoot()
        {
            const string runId =
                "savefixture000000000000000000000001";
            string fixtureRoot = Path.Combine(
                Path.GetTempPath(),
                "save-fixture-settings");
            string saveRoot =
                Path.Combine(fixtureRoot, "SAVE");
            Directory.CreateDirectory(saveRoot);

            QaHostSettings noSave =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new
                        {
                            schemaVersion =
                                QaHostProtocol
                                    .SchemaVersion,
                            protocolVersion =
                                QaHostProtocol
                                    .ProtocolVersion,
                            runId,
                            mode =
                                QaHostProtocol
                                    .ParticipantOnlyMode,
                            SaveTestMode =
                                "NoNativeSave",
                            RequireDisposableSaveRedirect =
                                false
                        })));
            noSave.Validate(runId);
            Assert(
                noSave.SaveTestMode ==
                    "NoNativeSave" &&
                string.IsNullOrEmpty(
                    noSave
                        .DisposableSaveFixtureSaveRoot),
                "NoNativeSave must use no redirect by default.");

            QaHostSettings isolatedNoSave =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new
                        {
                            schemaVersion =
                                QaHostProtocol.SchemaVersion,
                            protocolVersion =
                                QaHostProtocol.ProtocolVersion,
                            runId,
                            mode =
                                QaHostProtocol.ParticipantOnlyMode,
                            SaveTestMode =
                                "NoNativeSave",
                            DisposableSaveFixtureSaveRoot =
                                saveRoot,
                            RequireDisposableSaveRedirect =
                                true
                        })));
            isolatedNoSave.Validate(runId);
            Assert(
                isolatedNoSave
                    .DisposableSaveFixtureSaveRoot ==
                Path.GetFullPath(saveRoot)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                "NoNativeSave must accept an explicit QA-owned disposable redirect for a multi-process rollback matrix.");

            QaHostSettings native =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new
                        {
                            schemaVersion =
                                QaHostProtocol
                                    .SchemaVersion,
                            protocolVersion =
                                QaHostProtocol
                                    .ProtocolVersion,
                            runId,
                            mode =
                                QaHostProtocol
                                    .ParticipantOnlyMode,
                            SaveTestMode =
                                "NativeSaveExpected",
                            DisposableSaveFixtureSaveRoot =
                                saveRoot,
                            RequireDisposableSaveRedirect =
                                true
                        })));
            native.Validate(runId);
            Assert(
                native
                    .DisposableSaveFixtureSaveRoot ==
                Path.GetFullPath(saveRoot)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                "NativeSaveExpected must bind the exact disposable SAVE root.");

            foreach (var phase in new[]
            {
                new
                {
                    CaseId = "MoreSavesFixed12EnabledLifecycle",
                    SaveMode = "ArchiveMutation"
                },
                new
                {
                    CaseId = "MoreSavesFixed12DisabledCold",
                    SaveMode = "NoNativeSave"
                },
                new
                {
                    CaseId = "MoreSavesFixed12ReenabledCold",
                    SaveMode = "NoNativeSave"
                }
            })
            {
                QaHostSettings moreSaves =
                    QaHostSettings.Read(
                        Encoding.UTF8.GetBytes(
                            JsonSerializer.Serialize(new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 7,
                                SaveTestMode = phase.SaveMode,
                                DisposableSaveFixtureSaveRoot =
                                    saveRoot,
                                RequireDisposableSaveRedirect =
                                    true,
                                G6LifecycleCases =
                                    new[] { phase.CaseId }
                            })));
                moreSaves.Validate(runId);
                Assert(
                    moreSaves.G6LifecycleCases.SequenceEqual(
                        new[] { phase.CaseId },
                        StringComparer.Ordinal),
                    "Each MoreSaves fixed-12 phase must remain one exact disposable cold-process case.");
            }

            QaHostSettings unsafeMoreSaves =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new
                        {
                            schemaVersion =
                                QaHostProtocol.SchemaVersion,
                            protocolVersion =
                                QaHostProtocol.ProtocolVersion,
                            runId,
                            mode =
                                QaHostProtocol.ParticipantOnlyMode,
                            SaveSlot = 6,
                            SaveTestMode = "NoNativeSave",
                            DisposableSaveFixtureSaveRoot = saveRoot,
                            RequireDisposableSaveRedirect = true,
                            G6LifecycleCases = new[]
                            {
                                "MoreSavesFixed12DisabledCold"
                            }
                        })));
            AssertThrows<InvalidDataException>(
                () => unsafeMoreSaves.Validate(runId),
                "MoreSaves fixed-12 QA must reject any slot other than the first extra slot in its disposable fixture.");

            foreach (object invalid in new object[]
            {
                new
                {
                    schemaVersion =
                        QaHostProtocol.SchemaVersion,
                    protocolVersion =
                        QaHostProtocol.ProtocolVersion,
                    runId,
                    mode =
                        QaHostProtocol.ParticipantOnlyMode,
                    SaveTestMode =
                        "NoNativeSave",
                    DisposableSaveFixtureSaveRoot =
                        string.Empty,
                    RequireDisposableSaveRedirect =
                        true
                },
                new
                {
                    schemaVersion =
                        QaHostProtocol.SchemaVersion,
                    protocolVersion =
                        QaHostProtocol.ProtocolVersion,
                    runId,
                    mode =
                        QaHostProtocol.ParticipantOnlyMode,
                    SaveTestMode =
                        "NativeSaveExpected",
                    DisposableSaveFixtureSaveRoot =
                        saveRoot,
                    RequireDisposableSaveRedirect =
                        false
                }
            })
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            invalid));
                AssertThrows<InvalidDataException>(
                    () => QaHostSettings
                        .Read(bytes)
                        .Validate(runId),
                    "Save-test mode and redirect ownership must fail closed when mismatched.");
            }
        }

        private static void Batch6AutoFishingManagerLifecycleIsRetiredBeforeLegacySourceMutation()
        {
            string root = FindRepositoryRoot();
            string outer = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-batch6-autofishing-manager-lifecycle.ps1"));
            string smoke = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-game-smoke.ps1"));
            int outerGuard = outer.IndexOf("throw 'Batch 6 AutoFishing same-process Manager lifecycle is retired", StringComparison.Ordinal);
            int outerMutation = outer.IndexOf("New-Item -ItemType Directory -Path $OutputRoot", StringComparison.Ordinal);
            int smokeGuard = smoke.IndexOf("throw '-Batch6AutoFishingManagerLifecycle is retired", StringComparison.Ordinal);
            int smokeLegacyPath = smoke.IndexOf("Join-Path $gameDir 'Mods\\Yuuka.DTMAPI.AutoFishing'", StringComparison.Ordinal);
            int smokeMarkerWrite = smoke.IndexOf("[System.IO.File]::WriteAllBytes($batch6ManagerMarkerPath", StringComparison.Ordinal);
            Assert(outerGuard >= 0 && outerMutation > outerGuard &&
                smokeGuard >= 0 && smokeLegacyPath > smokeGuard && smokeMarkerWrite > smokeGuard,
                "The retired same-process Manager fixture must fail before evidence creation, Runtime lock/deployment, <game>/Mods lookup, marker mutation, or game launch.");
        }

        private static void Batch6AutoFishingObserverReadsTheCoreResidentEntryShapeWithoutAProductReference()
        {
            const string uniqueId = "Unit.AutoFishing";
            var activeEntry = new FakeBatch6AutoFishingEntry(enabled: true);
            var runtime = new FakeBatch6Runtime(uniqueId, activeEntry);
            var observer = new Batch6AutoFishingReflectionObserver(
                uniqueId,
                typeof(FakeBatch6AutoFishingEntry).FullName ?? nameof(FakeBatch6AutoFishingEntry),
                typeof(Program).Assembly);
            Batch6AutoFishingObservation observed = observer.Observe(runtime, DateTimeOffset.Parse("2026-07-21T00:00:00Z"));
            Assert(observed.ProductPresent && observed.Enabled && observed.UpdateSubscribed && !observed.ToggleAwaitingRelease && observed.SessionPresent && !observed.SessionReleased &&
                observed.InstalledPatchCount == 17 && observed.TransitionCount == 44 && observed.FishCompletedCount == 12 &&
                observed.PullEnteredCount == 13 && observed.PullExitedCount == 12 && observed.NativeVisibleReelCount == 10 &&
                observed.NativeSkipReelCount == 2 && observed.VisibleReelQueued == 11 && observed.VisibleReelConsumed == 10 &&
                observed.VisibleReelNativeAccepted == 9 && observed.VisibleReelRetries == 2 && observed.VisibleReelTimeouts == 1 &&
                observed.NativeAccessorBuildCount == 4 && observed.NativeAccessorFailureCount == 0 &&
                observed.NativeFrameRefreshCount == 120 && observed.NativeTransientCount == 14 && observed.DeepNativeTransientCount == 10 &&
                observed.InputOverrideActive && observed.VisibleReelInputPending && observed.ReadyTargetCount == 2 &&
                observed.ReadyReleasedCount == 1 && observed.CurrentReadyStatePresent && observed.AnimatorSpeedSnapshotCount == 1 &&
                observed.HookGravitySnapshotCount == 1 && observed.HookVelocitySnapshotCount == 1 && observed.PullDurationSnapshotCount == 1 &&
                observed.ToggleKey == "F6" && observed.InstantBite && observed.SkipMiniGame && observed.FastAnimations &&
                Math.Abs(observed.AnimationMultiplier - 3d) < 0.000001d && Math.Abs(observed.CastChargeRatio - 0.5d) < 0.000001d &&
                observed.LastReason == "unit-active" && observed.CastAppliedCount == 14 && observed.NativeBitePreparedCount == 6 &&
                observed.InstantBiteCommittedCount == 5 &&
                observed.SessionPhase == "WaitPlayable" &&
                observed.AnimationApplicationCount == 8 && observed.ReadyChargeApplicationCount == 3 &&
                !observed.QaHasStaticProductAssemblyRef,
                "The reflection observer must read the exact real-ModEntry-shaped fields and nested diagnostics without a compile-time product type.");
            Assert(!observed.PreflightNativeMovementRefreshed && !observed.NativeMovementAvailable &&
                observed.NativeInputMultiplier == 0d && observed.NativeVelocityX == 0d && observed.NativeOffsetX == 0d,
                "The ordinary observer path must not refresh or invent inactive preflight movement state.");

            var inactiveEntry = new FakeBatch6AutoFishingEntry(enabled: false);
            Batch6AutoFishingObservation preflight = observer.Observe(
                new FakeBatch6Runtime(uniqueId, inactiveEntry),
                DateTimeOffset.Parse("2026-07-21T00:00:01Z"),
                refreshInactiveNativeMovement: true);
            Assert(preflight.PreflightNativeMovementRefreshed && preflight.NativeMovementAvailable &&
                Math.Abs(preflight.NativeInputMultiplier - 0.75d) < 0.000001d &&
                Math.Abs(preflight.NativeVelocityX - 0.125d) < 0.000001d &&
                Math.Abs(preflight.NativeOffsetX - (-0.25d)) < 0.000001d &&
                inactiveEntry.NativeRefreshCount == 1 && inactiveEntry.NativeClearCount == 1,
                "The bounded inactive observer path must refresh the ProductNative cache once, project its exact native movement values, and clear the temporary native references.");
            AssertThrows<InvalidDataException>(() => observer.Observe(runtime, refreshInactiveNativeMovement: true),
                "The native movement preflight must fail closed if the real product entry is already active.");

            Batch6AutoFishingObservation absent = observer.Observe(new FakeBatch6Runtime());
            Assert(!absent.ProductPresent && absent.UniqueId == uniqueId,
                "The reflection observer must represent an absent Core-resident product without inventing state.");
            AssertThrows<InvalidDataException>(() => observer.Observe(new object()),
                "The reflection observer must fail closed when DtmApiRuntime.modInstances is unavailable.");

            string[] qaReferences = typeof(QaHostParticipant).Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();
            Assert(!qaReferences.Any(name => name.Equals("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal)),
                "The optional QA assembly must not contain a static Yuuka.DTMAPI.AutoFishing AssemblyRef.");
            string observerSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "products", "first-party", "AutoFishing", "qa", "batch6", "Batch6AutoFishingReflectionObserver.cs"));
            Assert(observerSource.Contains("FishingProductCallbacks", StringComparison.Ordinal) &&
                observerSource.Contains("ProductCallbackRuntimePresent", StringComparison.Ordinal) &&
                observerSource.Contains("CountHarmonyOwnerPatches(\"dtmapi.mod.yuuka.dtmapi.autofishing\", productAssembly)", StringComparison.Ordinal) &&
                observerSource.Contains("productAssembly.GetReferencedAssemblies()", StringComparison.Ordinal) &&
                observerSource.Contains("ProductHarmonyAssemblyName", StringComparison.Ordinal) &&
                observerSource.Contains("GetAllPatchedMethods", StringComparison.Ordinal) &&
                observerSource.Contains("GetPatchInfo", StringComparison.Ordinal) &&
                observerSource.Contains("ReadHarmonyMember(info, collectionName) is IEnumerable patches", StringComparison.Ordinal) &&
                observerSource.Contains("ReadHarmonyMember(patch, \"owner\") ?? ReadHarmonyMember(patch, \"Owner\")", StringComparison.Ordinal),
                "The observer must retain process-level callback-root and canonical Harmony-owner evidence from the product-referenced Harmony registry after Core removes the product instance, including Harmony 2.x field/property collection and owner shapes, while Mono keeps its assembly loaded.");
        }

        private static void Batch6AutoFishingDirectNeutralPreflightRequiresUntouchedSamePositionNativeParityNeutral()
        {
            DateTimeOffset started = DateTimeOffset.Parse("2026-08-04T05:00:00Z");
            Batch6AutoFishingNativeSurfaceReceipt baseline = CreateDirectNeutralSurfaceReceipt(started);
            var gate = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            Assert(gate.Observe(started, true, true, 0d, 0d, 0d, baseline) == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral &&
                gate.DirectNeutralPreconditionObserved && gate.NeutralObservationCount == 1,
                "The untouched face-right fifth-save load position must begin the native-parity neutral window without any preflight input.");
            Assert(gate.Observe(started.AddMilliseconds(999), true, true, 0d, 0d, 0d, CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(999))) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral,
                "A same-position native-parity neutral window shorter than 1000 ms must remain pending.");
            Assert(gate.Observe(started.AddMilliseconds(1000), true, true, 0d, 0d, 0d, CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(1000))) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.Ready && gate.NeutralObservationCount == 3 &&
                Math.Abs(gate.StableNeutralMilliseconds - 1000d) < 0.000001d &&
                gate.Details.Contains("directNeutralPreconditionObserved=true", StringComparison.Ordinal) &&
                gate.Details.Contains("preflightInputObserved=false", StringComparison.Ordinal) &&
                gate.Details.Contains("baselineCell=14,6", StringComparison.Ordinal),
                "Only the unchanged direct native surface with continuous native-parity neutral for 1000 ms may authorize the configured toggle.");

            Batch6AutoFishingNativeSurfaceReceipt loading = CreateDirectNeutralSurfaceReceipt(started);
            loading.GroundTouched = false;
            loading.GroundTouchedExcludePlatforms = false;
            var settling = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            Assert(settling.Observe(started, true, true, 0d, 0d, 0d, loading) == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral &&
                !settling.DirectNeutralPreconditionObserved && settling.LoadContactPendingObservationCount == 1 && settling.NeutralObservationCount == 0,
                "The exact same-position no-contact load frame must wait for directly observed ordinary ground without authorizing input.");
            Batch6AutoFishingNativeSurfaceReceipt loadingAgain = CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(250));
            loadingAgain.GroundTouched = false;
            loadingAgain.GroundTouchedExcludePlatforms = false;
            Assert(settling.Observe(started.AddMilliseconds(250), true, true, 0d, 0d, 0d, loadingAgain) == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral &&
                settling.LoadContactPendingObservationCount == 2,
                "Repeated exact same-position no-contact load observations must remain bounded pending evidence.");
            Assert(settling.Observe(started.AddMilliseconds(500), true, true, 0d, 0d, 0d, CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(500))) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral && settling.NeutralObservationCount == 1,
                "The first directly grounded observation must begin a new native-parity neutral window instead of inheriting load-settling time.");
            Assert(settling.Observe(started.AddMilliseconds(1500), true, true, 0d, 0d, 0d, CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(1500))) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.Ready && Math.Abs(settling.StableNeutralMilliseconds - 1000d) < 0.000001d,
                "A load-settling observation may authorize the toggle only after a subsequent full grounded 1000 ms native-parity neutral window.");

            Batch6AutoFishingNativeSurfaceReceipt movedWhileLoading = CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(1));
            movedWhileLoading.GroundTouched = false;
            movedWhileLoading.GroundTouchedExcludePlatforms = false;
            movedWhileLoading.AgentPositionY += 0.000001d;
            var movingLoad = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            movingLoad.Observe(started, true, true, 0d, 0d, 0d, loading);
            AssertThrows<InvalidDataException>(() => movingLoad.Observe(started.AddMilliseconds(1), true, true, 0d, 0d, 0d, movedWhileLoading),
                "Load-contact waiting must fail closed on any position drift before ordinary ground is observed.");
            Batch6AutoFishingNativeSurfaceReceipt mixedGround = CreateDirectNeutralSurfaceReceipt(started);
            mixedGround.GroundTouchedExcludePlatforms = false;
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, 0d, mixedGround),
                "Inconsistent ground flags must not be treated as the exact no-contact loading state.");
            var losesGround = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            losesGround.Observe(started, true, true, 0d, 0d, 0d, baseline);
            AssertThrows<InvalidDataException>(() => losesGround.Observe(started.AddMilliseconds(1), true, true, 0d, 0d, 0d, loadingAgain),
                "Ordinary ground loss after the stability window begins must fail closed.");

            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, false, true, 0d, 0d, 0d, baseline),
                "The gate must reject a QA observation that did not execute the bounded native refresh.");
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, false, 0d, 0d, 0d, baseline),
                "The gate must reject unavailable native movement instead of waiting or falling back.");
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, double.NaN, 0d, 0d, baseline),
                "The gate must reject non-finite native movement.");
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, double.PositiveInfinity, 0d, baseline),
                "The gate must reject a non-finite ProductNative VelocityX.");
            const double diagnosticInput = 0.125d;
            const double diagnosticVelocity = -1.6153926480910741E-06d;
            try
            {
                new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(
                    started,
                    true,
                    true,
                    diagnosticInput,
                    diagnosticVelocity,
                    0d,
                    baseline);
                throw new InvalidOperationException("The direct gate unexpectedly accepted finite nonzero ProductNative movement.");
            }
            catch (InvalidDataException ex)
            {
                Assert(ex.Message.Contains("actualInputMultiplier=" + diagnosticInput.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal) &&
                    ex.Message.Contains("actualVelocityX=" + diagnosticVelocity.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal) &&
                    ex.Message.Contains("actualOffsetX=0", StringComparison.Ordinal),
                    "A ProductNative native-parity failure must retain all invariant round-trip movement values for root-cause evidence.");
            }
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0.0000001d, 0d, 0d, baseline),
                "The direct gate must fail closed on any finite nonzero ProductNative input.");
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, -0.0011d, 0d, baseline),
                "The direct gate must reject an existing Rigidbody X speed beyond the native Wait pre-base threshold.");
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, double.NaN, baseline),
                "The direct gate must fail closed on a non-finite ProductNative OffsetX.");

            Batch6AutoFishingNativeSurfaceReceipt residualStart = CreateDirectNeutralSurfaceReceipt(started);
            residualStart.RigidbodyVelocityX = diagnosticVelocity;
            Batch6AutoFishingNativeSurfaceReceipt residualReady = CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(1000));
            residualReady.RigidbodyVelocityX = diagnosticVelocity;
            var residualGate = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            Assert(residualGate.Observe(started, true, true, 0d, diagnosticVelocity, 0d, residualStart) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral &&
                residualGate.Observe(started.AddMilliseconds(1000), true, true, 0d, diagnosticVelocity, 0d, residualReady) ==
                Batch6AutoFishingDirectNeutralPreflightStatus.Ready &&
                residualGate.LastVelocityX == diagnosticVelocity && residualGate.LastNativeOffsetX == 0d,
                "A same-position tiny pre-base Rigidbody residual with exact-zero input and OffsetX must follow the native Wait threshold for the full 1000 ms surface window.");

            Batch6AutoFishingNativeSurfaceReceipt wrongFace = CreateDirectNeutralSurfaceReceipt(started);
            wrongFace.AgentFaceRight = false;
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, 0d, wrongFace),
                "The direct gate must fail closed when the untouched load position does not already face right.");
            Batch6AutoFishingNativeSurfaceReceipt offset = CreateDirectNeutralSurfaceReceipt(started);
            offset.ConveyorOffsetX = -1.6153926480910741E-06;
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, offset.ConveyorOffsetX, offset),
                "The direct gate must reject a nonzero conveyor offset even when ProductNative input and VelocityX are zero.");
            Batch6AutoFishingNativeSurfaceReceipt wall = CreateDirectNeutralSurfaceReceipt(started);
            wall.TouchWall = true;
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, 0d, wall),
                "The direct gate must reject wall contact instead of guessing another player action.");
            Batch6AutoFishingNativeSurfaceReceipt platform = CreateDirectNeutralSurfaceReceipt(started);
            platform.ConveyorPlatformInstanceCount = 1;
            platform.ConveyorPlatformTouchedCount = 1;
            platform.ConveyorPlatforms.Add(new Batch6AutoFishingConveyorPlatformReceipt { Verified = true, IsTouched = true });
            AssertThrows<InvalidDataException>(() => new Batch6AutoFishingDirectNeutralPreflightGate(1000).Observe(started, true, true, 0d, 0d, 0d, platform),
                "The direct gate must reject active or touched ConveyorPlatform ownership.");

            var chronological = new Batch6AutoFishingDirectNeutralPreflightGate(1000);
            chronological.Observe(started, true, true, 0d, 0d, 0d, baseline);
            AssertThrows<InvalidDataException>(() => chronological.Observe(started.AddMilliseconds(-1), true, true, 0d, 0d, 0d, CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(-1))),
                "The gate must reject timestamps that move backwards.");
            Batch6AutoFishingNativeSurfaceReceipt moved = CreateDirectNeutralSurfaceReceipt(started.AddMilliseconds(10));
            moved.AgentPositionX += 0.000001d;
            AssertThrows<InvalidDataException>(() => chronological.Observe(started.AddMilliseconds(10), true, true, 0d, 0d, 0d, moved),
                "The gate must fail closed on any position drift during the no-input stability window.");

            string coordinatorSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "products",
                "first-party",
                "AutoFishing",
                "qa",
                "batch6",
                "Batch6AutoFishingPilotCoordinator.cs"));
            Assert(CountTextOccurrences(coordinatorSource, "nativeSurface.Phase = diagnosticPrefix + \"-direct-neutral-failed\";") == 2,
                "Both the initial direct-neutral observation and later stability observations must retain a failure surface receipt.");
        }

        private static Batch6AutoFishingNativeSurfaceReceipt CreateDirectNeutralSurfaceReceipt(DateTimeOffset observedAt) =>
            new Batch6AutoFishingNativeSurfaceReceipt
            {
                ObservedAtUtc = observedAt.ToUniversalTime().ToString("O"),
                SaveLoadOrdinal = 1,
                Phase = "unit-direct-neutral",
                InputContext = "Gameplay",
                Verified = true,
                RoomType = "DolocTown.CityRoom",
                RoomId = "city_郊区-林地深处",
                SceneRawName = "city_郊区-林地深处",
                AgentStateType = "DolocTown.AgentStateIdle",
                AgentFaceRight = true,
                AgentPositionX = 21.59466552734375d,
                AgentPositionY = 8.9901657104492188d,
                AgentPositionZ = -0.5d,
                AgentCellX = 14,
                AgentCellY = 6,
                GroundTouched = true,
                GroundTouchedExcludePlatforms = true,
                GroundPlatformCount = 0,
                TouchWall = false,
                RigidbodyVelocityX = 0d,
                RigidbodyVelocityY = 0d,
                InputMultiplier = 0d,
                ConveyorOffsetX = 0d,
                ConveyorOffsetY = 0d,
                ConveyorPlatformsEnumerated = true,
                ConveyorPlatformInstanceCount = 0,
                ConveyorPlatformTouchedCount = 0,
                ConveyorPlatformStayCount = 0
            };

        private static void Batch6AutoFishingNativeSurfaceDiagnosticsReadDirectOwnerState()
        {
            DateTimeOffset observedAt = DateTimeOffset.Parse("2026-08-04T06:40:00Z");
            var platform = new FakeNativeSurfacePlatform(
                instanceId: 333,
                position: new FakeNativeSurfaceVector(10d, 19d, -1d),
                groupSpeedX: -1.6153926480910741E-06,
                groupSpeedY: 2d,
                touched: true,
                stay: true,
                otherCollider: new FakeNativeSurfaceCollider(444, "ground"));
            Batch6AutoFishingNativeSurfaceReceipt receipt = Batch6AutoFishingNativeSurfaceObserver.CaptureForTesting(
                1,
                "initial-timeout",
                "Gameplay",
                observedAt,
                typeof(FakeNativeSurfaceDolocApi),
                new object[] { platform });

            Assert(receipt.Verified && receipt.Error.Length == 0 && receipt.SaveLoadOrdinal == 1 &&
                receipt.Phase == "initial-timeout" && receipt.InputContext == "Gameplay" &&
                receipt.RoomType.EndsWith(nameof(FakeNativeSurfaceRoom), StringComparison.Ordinal) &&
                receipt.RoomId == "unit-room" && receipt.SceneRawName == "unit-scene" &&
                receipt.AgentStateType.EndsWith(nameof(FakeNativeSurfaceState), StringComparison.Ordinal) &&
                receipt.AgentFaceRight && Math.Abs(receipt.AgentPositionX - 10d) < 0.000001d &&
                Math.Abs(receipt.AgentPositionY - 20d) < 0.000001d && receipt.AgentCellX == 6 && receipt.AgentCellY == 7 &&
                receipt.GroundTouched && !receipt.GroundTouchedExcludePlatforms && receipt.GroundPlatformCount == 1 &&
                receipt.TouchWall && !receipt.NearWallTop && receipt.InputMultiplier == 0d &&
                Math.Abs(receipt.ConveyorOffsetX - (-1.6153926480910741E-06)) < 1E-18 &&
                Math.Abs(receipt.RigidbodyVelocityX - receipt.ConveyorOffsetX) < 1E-18 &&
                receipt.ConveyorPlatformsEnumerated && receipt.ConveyorPlatformInstanceCount == 1 &&
                receipt.ConveyorPlatformTouchedCount == 1 && receipt.ConveyorPlatformStayCount == 1,
                "The direct native-surface receipt must preserve room, position, state, ground/wall, exact conveyor offset, and Rigidbody velocity as separate read-only facts.");

            Batch6AutoFishingConveyorPlatformReceipt owner = receipt.ConveyorPlatforms.Single();
            Assert(owner.Verified && owner.InstanceId == 333 && owner.GameObjectName == "unit-platform" && owner.ActiveInHierarchy &&
                owner.IsTouched && owner.IsStay && owner.GroupPresent &&
                Math.Abs(owner.GroupSpeedX - receipt.ConveyorOffsetX) < 1E-18 && Math.Abs(owner.GroupSpeedY - 2d) < 0.000001d &&
                Math.Abs(owner.GroupMoveSpeed - 2d) < 0.000001d && owner.CurrentOtherColliderPresent &&
                owner.CurrentOtherColliderInstanceId == 444 && owner.CurrentOtherColliderGameObjectName == "ground" &&
                !owner.CurrentCollisionPresent && receipt.Summary.Contains("owner=333:unit-platform", StringComparison.Ordinal),
                "The surface receipt must identify the exact touched platform, group speed, and player ground collider without inferring contact from velocity alone.");

            Batch6AutoFishingNativeSurfaceReceipt broken = Batch6AutoFishingNativeSurfaceObserver.CaptureForTesting(
                1,
                "initial-before-input",
                "Gameplay",
                observedAt,
                typeof(FakeNativeSurfaceDolocApi),
                new object[] { new object() });
            Assert(!broken.Verified && broken.ConveyorPlatformsEnumerated && broken.ConveyorPlatformInstanceCount == 1 &&
                broken.Error.Contains("GetInstanceID", StringComparison.Ordinal),
                "A missing direct platform member must remain explicit failed evidence instead of being projected as no contact.");
        }

        private static void Batch6AutoFishingL0RequiresIndependentNativeDriver()
        {
            Assert(Batch6AutoFishingFailClosedPolicy.EvaluateProductAbsentL0(false, false, 0, false) ==
                    Batch6AutoFishingNativeDriverOutcome.PendingSaveLoaded,
                "L0 must wait for the real fifth SaveLoaded boundary before assigning a terminal.");
            Assert(Batch6AutoFishingFailClosedPolicy.EvaluateProductAbsentL0(true, true, 1, false) ==
                    Batch6AutoFishingNativeDriverOutcome.FailedProductPresent,
                "L0 must fail when a Core-resident product instance or loaded package exists.");
            Assert(Batch6AutoFishingFailClosedPolicy.EvaluateProductAbsentL0(true, false, 0, false) ==
                    Batch6AutoFishingNativeDriverOutcome.BlockedNativeDriverMissing,
                "Product-absent L0 must be Blocked, never Passed, when no safe independent native driver exists.");
            Assert(Batch6AutoFishingFailClosedPolicy.EvaluateProductAbsentL0(true, false, 0, true) ==
                    Batch6AutoFishingNativeDriverOutcome.ContinueWithNativeDriver,
                "Only a separately available safe native driver may advance product-absent L0 beyond the fifth-save boundary.");

            string root = FindRepositoryRoot();
            string batch6Root = Path.Combine(root, "products", "first-party", "AutoFishing", "qa", "batch6");
            string coordinatorSource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingPilotCoordinator.cs"));
            string driverSource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingCompatibilityDriver.cs"));
            string evidenceSource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingPilotEvidence.cs"));
            string settingsSource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingPilotSettings.cs"));
            Assert(driverSource.Contains("internal const string DriverKind = \"CompatibilityNativeControl\"", StringComparison.Ordinal) &&
                driverSource.Contains("internal const int ExpectedPatchCount = 22", StringComparison.Ordinal) &&
                driverSource.Contains("snapshot.PatchCount != ExpectedPatchCount", StringComparison.Ordinal) &&
                driverSource.Contains("GetApi<IFishingAutomationApi>", StringComparison.Ordinal) &&
                driverSource.Contains("GetFishingAutomationLifecycleSnapshot", StringComparison.Ordinal) &&
                driverSource.Contains("ReadNativeExitCount(service)", StringComparison.Ordinal) &&
                driverSource.Contains("api.SetEnabled(owner, true", StringComparison.Ordinal) &&
                driverSource.Contains("api?.SetEnabled(owner, false", StringComparison.Ordinal),
                "L0 must use the separately owned frozen compatibility executor and observe real native lifecycle progress; it may not synthesize a no-product pass.");
            string[] exactCompatibilityPatchReceipts =
            {
                "ReadyEnterPatched", "ReadyPlayPatched", "CastEnterPatched", "WaitEnterPatched", "WaitPlayPatched", "WaitNextStatePatched",
                "MiniGameStartPatched", "MiniGameUpdatePrefixPatched", "MiniGameUpdatePatched", "MiniGameStopPatched",
                "InputUseToolPatched", "InputUseToolInProgressPatched", "InputUseItemPatched", "InputUseItemInProgressPatched",
                "InputFishingPatched", "InputFishingInProgressPatched", "FishRodCastHookPatched", "FishRodPullPatched",
                "FishRodPullCancelPatched", "PullEnterPatched", "PullExitPatched", "BaseExitPatched"
            };
            Assert(exactCompatibilityPatchReceipts.All(name => driverSource.Contains("\"" + name + "\"", StringComparison.Ordinal)),
                "L0 must count the complete frozen 22-patch inventory, including ready/wait continuations, input aliases, and rod patches.");
            Assert(driverSource.Contains("feature?.RemoveOwner(ownerId", StringComparison.Ordinal) &&
                !driverSource.Contains("DeactivateOwner(ownerId", StringComparison.Ordinal) &&
                driverSource.Contains("feature.CountOwnerResources(ownerId)", StringComparison.Ordinal) &&
                driverSource.Contains("serviceAbsent", StringComparison.Ordinal) &&
                driverSource.Contains("callbackAbsent", StringComparison.Ordinal) &&
                driverSource.Contains("hooksAbsent", StringComparison.Ordinal),
                "The L0 compatibility driver must disable and remove its synthetic owner through the feature that owns it, then prove service, callback, hook, and owner-resource cleanup.");
            Assert(coordinatorSource.Contains("new Batch6AutoFishingCompatibilityDriver", StringComparison.Ordinal) &&
                coordinatorSource.Contains("writer.BindDriver(driver)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("AdvanceProductAbsentL0", StringComparison.Ordinal) &&
                coordinatorSource.Contains("NativeExitCount", StringComparison.Ordinal) &&
                coordinatorSource.Contains("AutoCastConfirmedCount", StringComparison.Ordinal) &&
                coordinatorSource.Contains("WarmupFish", StringComparison.Ordinal) &&
                coordinatorSource.Contains("TargetFish", StringComparison.Ordinal) &&
                coordinatorSource.Contains("driver.Cleanup", StringComparison.Ordinal),
                "The live coordinator must drive L0 through real warmup/measurement counters and exact driver cleanup.");
            Assert(settingsSource.Contains("internal const int ExpectedProductPatchCount = 22", StringComparison.Ordinal) &&
                coordinatorSource.Contains("observation.InstalledPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount", StringComparison.Ordinal) &&
                !coordinatorSource.Contains("expectedPatchCount = observation.InstalledPatchCount", StringComparison.Ordinal),
                "L1-L5 must require the authoritative 22-patch AdvancedProduct inventory instead of learning a potentially incomplete first observation.");
            Assert(evidenceSource.Contains("DataMember(Name = \"driverKind\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"driverOwner\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"driverPatchCount\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"driverNativeProgress\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("PackageSha256 = package.PackageSha256", StringComparison.Ordinal) &&
                evidenceSource.Contains("EntryDllSha256 = package.EntryDllSha256", StringComparison.Ordinal) &&
                evidenceSource.Contains("ManifestSha256 = package.ManifestSha256", StringComparison.Ordinal) &&
                evidenceSource.Contains("ReferencePolicySha256 = package.ReferencePolicySha256", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"nativeFishingContexts\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"nativeVitals\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"processMetricsAvailable\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"processMetricsError\"", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"processMetricsSource\"", StringComparison.Ordinal) &&
                coordinatorSource.Contains("Windows.GetProcessMemoryInfo", StringComparison.Ordinal) &&
                coordinatorSource.Contains("GetProcessMemoryInfo(process.Handle", StringComparison.Ordinal) &&
                evidenceSource.Contains("DataMember(Name = \"nativeProgressTrailingWindowVerified\"", StringComparison.Ordinal) &&
                coordinatorSource.Contains("TryPrepareNativeFishingWorkload(saveLoadOrdinal, now)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("WaitingForNativeFishingContext", StringComparison.Ordinal) &&
                coordinatorSource.Contains("NativeFishingContextReadyTimeoutSeconds", StringComparison.Ordinal) &&
                coordinatorSource.Contains("writer.RecordNativeFishingContext(pendingNativeFishingContext)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("ObserveNativeVitalsFinal", StringComparison.Ordinal) &&
                coordinatorSource.Contains("FinalizeNativeEvidence", StringComparison.Ordinal) &&
                coordinatorSource.Contains("RequireTrailingNativeProgress", StringComparison.Ordinal) &&
                coordinatorSource.Contains("NonAuthoritativeCompleted", StringComparison.Ordinal),
                "L0 evidence must identify independent native progress while keeping actual package/entry/manifest/policy hashes empty instead of copying candidate expected hashes.");

            string candidateHash = new string('b', 64);
            var l0Settings = new Batch6AutoFishingPilotSettings
            {
                Enabled = true,
                Level = "L0",
                Scenario = "DefaultLoop",
                ExpectedPackageSha256 = candidateHash,
                ExpectedEntryDllSha256 = candidateHash,
                ExpectedManifestSha256 = candidateHash,
                ExpectedReferencePolicySha256 = candidateHash
            };
            l0Settings.NormalizeAndValidate(caseSelected: true, saveSlot: 5);
            var l0Writer = new Batch6AutoFishingPilotEvidenceWriter(
                Path.GetTempPath(),
                "batch6-l0-unit-no-product",
                l0Settings,
                DateTimeOffset.Parse("2026-07-21T00:00:00Z"));
            Batch6AutoFishingPilotEvidence l0Evidence = l0Writer.Evidence;
            Assert(l0Evidence.PackageSha256.Length == 0 &&
                l0Evidence.EntryDllSha256.Length == 0 &&
                l0Evidence.ManifestSha256.Length == 0 &&
                l0Evidence.ReferencePolicySha256.Length == 0 &&
                l0Evidence.ExpectedPackageSha256 == new string('B', 64) &&
                l0Evidence.ExpectedEntryDllSha256 == new string('B', 64) &&
                !l0Evidence.Formal && !l0Evidence.Authoritative && l0Evidence.Authority == "non-authoritative" &&
                !l0Evidence.ProductAssemblyReferenced &&
                l0Evidence.Package == null,
                "A newly created L0 result must keep candidate expected identity separate from absent actual product provenance.");
            l0Writer.Write();
            using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(l0Writer.ResultPath)))
            {
                JsonElement rootElement = json.RootElement;
                Assert(rootElement.GetProperty("expectedEntryDllSha256").GetString() == new string('B', 64) &&
                    rootElement.GetProperty("expectedReferencePolicySha256").GetString() == new string('B', 64) &&
                    rootElement.GetProperty("entryDllSha256").GetString() == string.Empty &&
                    rootElement.GetProperty("referencePolicySha256").GetString() == string.Empty &&
                    !rootElement.GetProperty("formal").GetBoolean() &&
                    !rootElement.GetProperty("authoritative").GetBoolean() &&
                    rootElement.GetProperty("authority").GetString() == "non-authoritative" &&
                    !rootElement.GetProperty("productAssemblyReferenced").GetBoolean(),
                    "Serialized L0 evidence must use the exact runner field names while preserving absent actual hashes and no static Product AssemblyRef.");
            }
        }

        private static void Batch6AutoFishingL4UsesSeparateReflectedProductRecoveryOwner()
        {
            string root = FindRepositoryRoot();
            string batch6Root = Path.Combine(root, "products", "first-party", "AutoFishing", "qa", "batch6");
            string recoverySource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingProductRecoveryDriver.cs"));
            string coordinatorSource = File.ReadAllText(Path.Combine(batch6Root, "Batch6AutoFishingPilotCoordinator.cs"));
            string qaProject = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "DTMAPI.GameBridge.DolocTown.QA.csproj"));
            Assert(recoverySource.Contains("internal const string DriverKind = \"QaProductNativeRecovery\"", StringComparison.Ordinal) &&
                recoverySource.Contains("GetField(\"modInstances\"", StringComparison.Ordinal) &&
                recoverySource.Contains("Batch6AutoFishingPilotSettings.ProductEntryType", StringComparison.Ordinal) &&
                recoverySource.Contains("ReadRequiredField(entry, \"primitives\")", StringComparison.Ordinal) &&
                recoverySource.Contains("ReadBool(entry, \"enabled\")", StringComparison.Ordinal) &&
                recoverySource.Contains("ReadBool(entry, \"updateSubscribed\")", StringComparison.Ordinal) &&
                recoverySource.Contains("ReadField(entry, \"session\")", StringComparison.Ordinal) &&
                !recoverySource.Contains("using Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal),
                "L4 must find the exact inactive Core-resident product and its private primitives by reflection, without a static product reference or re-enabling ModEntry.");
            Assert(recoverySource.Contains("InvokeRequired(primitives, \"StartSession\", ownerId, null!)", StringComparison.Ordinal) &&
                recoverySource.Contains("CountOwnerResources", StringComparison.Ordinal) &&
                recoverySource.Contains("TryCast", StringComparison.Ordinal) &&
                recoverySource.Contains("TryPrepareNativeBite", StringComparison.Ordinal) &&
                recoverySource.Contains("TryReel", StringComparison.Ordinal) &&
                recoverySource.Contains("PullExitedCount", StringComparison.Ordinal),
                "The separate L4 QA owner must advance a real reflected product-native cast/bite/reel loop and observe PullExited progress.");
            Assert(recoverySource.Contains("InvokeRequiredAllowingVoid(session, \"Release\"", StringComparison.Ordinal) &&
                recoverySource.Contains("InvokeInt(primitives, \"RemoveOwner\"", StringComparison.Ordinal) &&
                recoverySource.Contains("resources == 0 && sessions == 0 && inputLeases == 0 && animationLeases == 0", StringComparison.Ordinal) &&
                recoverySource.Contains("&& !schedulerPending", StringComparison.Ordinal) &&
                recoverySource.Contains("deep.TotalCount == 0", StringComparison.Ordinal) &&
                coordinatorSource.Contains("new Batch6AutoFishingProductRecoveryDriver", StringComparison.Ordinal) &&
                coordinatorSource.Contains("writer.BindRecoveryDriver(recovery)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("writer.SetRecoveryDriverProgress", StringComparison.Ordinal) &&
                coordinatorSource.Contains("ValidateProductUpdaterInactiveDuringRecovery(product)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("sharedNativeTransientCount", StringComparison.Ordinal) &&
                coordinatorSource.Contains("driver.Cleanup(\"native-pull-exited\")", StringComparison.Ordinal) &&
                qaProject.Contains("Batch6AutoFishingProductRecoveryDriver.cs", StringComparison.Ordinal),
                "L4 must link the product-owned reflection driver, bind one recovery receipt, and release its session plus every transient owner root before passing.");
        }

        private static void Batch6AutoFishingL5UsesQaOwnedTitleAndReentryLifecycle()
        {
            string root = FindRepositoryRoot();
            string coordinatorSource = File.ReadAllText(Path.Combine(root, "products", "first-party", "AutoFishing", "qa", "batch6", "Batch6AutoFishingPilotCoordinator.cs"));
            string participantSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            Assert(coordinatorSource.Contains("internal bool ShouldRequestReturnHome", StringComparison.Ordinal) &&
                coordinatorSource.Contains("state != PilotState.WaitingForTitle", StringComparison.Ordinal) &&
                coordinatorSource.Contains("if (!observation.ToggleAwaitingRelease)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("waiting for the completed configured-toggle release transaction", StringComparison.Ordinal) &&
                coordinatorSource.Contains("internal bool ShouldRequestReentryLoad => state == PilotState.WaitingForReentry", StringComparison.Ordinal),
                "L5 must expose coordinator-owned title/re-entry requests and delay ReturnHome until the configured-toggle release transaction completes.");
            Assert(participantSource.Contains("if (batch6AutoFishingPilot?.ShouldRequestReturnHome == true)", StringComparison.Ordinal) &&
                participantSource.IndexOf("if (batch6AutoFishingPilot?.ShouldRequestReturnHome == true)", StringComparison.Ordinal) <
                    participantSource.IndexOf("GetIncompleteG3Requirements().Length > 0 || GetIncompleteG5Requirements().Length > 0 || GetIncompleteG6Requirements().Length > 0", StringComparison.Ordinal),
                "The QA participant must request L5 ReturnHome before the ordinary incomplete-G6 gate can deadlock it.");
            Assert(participantSource.Contains("if (batch6AutoFishingPilot?.ShouldRequestReentryLoad == true)", StringComparison.Ordinal) &&
                participantSource.Contains("batch6AutoFishingReentryLoadRequested = scenarios.TryRequestInitialSaveLoad(settings.SaveSlot)", StringComparison.Ordinal) &&
                participantSource.Contains("scenarios.ContinueInitialSaveLoad()", StringComparison.Ordinal),
                "After ReturnedToTitle, L5 must use the existing QA official save-load authority for the second fifth-save load rather than external menu input.");
        }

        private static void Batch6AutoFishingBehaviorDiscardsOnlyOneNonProductInitialLoop()
        {
            string root = FindRepositoryRoot();
            string coordinatorSource = File.ReadAllText(Path.Combine(root, "products", "first-party", "AutoFishing", "qa", "batch6", "Batch6AutoFishingPilotCoordinator.cs"));
            string runnerSource = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-batch6-autofishing-behavior-matrix.ps1"));
            int rebaselineCall = coordinatorSource.IndexOf("if (TryRebaselineFormalBehaviorAfterNonProductInitialLoop(observation, now))", StringComparison.Ordinal);
            int measurementCompletion = rebaselineCall < 0 ? -1 : coordinatorSource.IndexOf("if (now - measurementStartedAtUtc >= TimeSpan.FromSeconds(settings.MeasureSeconds)", rebaselineCall, StringComparison.Ordinal);
            int rebaselineStart = coordinatorSource.IndexOf("private bool TryRebaselineFormalBehaviorAfterNonProductInitialLoop(", StringComparison.Ordinal);
            int counterGuardStart = rebaselineStart < 0 ? -1 : coordinatorSource.IndexOf("private static void RequireBehaviorCountersNonDecreasing(", rebaselineStart, StringComparison.Ordinal);
            Assert(rebaselineCall >= 0 && measurementCompletion > rebaselineCall && rebaselineStart >= 0 && counterGuardStart > rebaselineStart,
                "Formal behavior must classify a completed non-product initial loop before the ordinary terminal-delta gate.");
            string rebaselineSource = coordinatorSource.Substring(rebaselineStart, counterGuardStart - rebaselineStart);
            Assert(rebaselineSource.Contains("discardedNonProductInitialLoops != 0", StringComparison.Ordinal) &&
                rebaselineSource.Contains("Formal behavior observed a second completed loop without a product-owned cast.", StringComparison.Ordinal) &&
                rebaselineSource.Contains("RequireBehaviorCountersNonDecreasing(baseline, observation)", StringComparison.Ordinal),
                "Formal behavior may discard exactly one completed initial loop with no product-owned cast, while a second such loop or any counter regression must fail closed.");
            Assert(rebaselineSource.Contains("behaviorBaseline = observation", StringComparison.Ordinal) &&
                rebaselineSource.Contains("writer.RecordBehaviorBaseline(observation)", StringComparison.Ordinal) &&
                rebaselineSource.Contains("measurementStartedAtUtc = now", StringComparison.Ordinal) &&
                rebaselineSource.Contains("measurementStartFish = observation.FishCompletedCount", StringComparison.Ordinal) &&
                rebaselineSource.Contains("lastMeasurementNativeProgress = observation.FishCompletedCount", StringComparison.Ordinal) &&
                rebaselineSource.Contains("lastMeasurementNativeProgressAtUtc = now", StringComparison.Ordinal) &&
                rebaselineSource.Contains("nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds)", StringComparison.Ordinal) &&
                rebaselineSource.Contains("MoveTo(", StringComparison.Ordinal),
                "The one allowed discard must rebuild every behavior and measurement baseline so counters from different native loops cannot be combined.");
            Assert(coordinatorSource.Contains("discardedNonProductInitialLoops=\" + discardedNonProductInitialLoops.ToString(CultureInfo.InvariantCulture)", StringComparison.Ordinal) &&
                coordinatorSource.Contains("discardedNonProductInitialLoops=0", StringComparison.Ordinal) &&
                runnerSource.Contains("discardedNonProductInitialLoops=(?<count>[01])", StringComparison.Ordinal) &&
                runnerSource.Contains("$discardedInitialLoopMatches.Count -ne 1", StringComparison.Ordinal),
                "Every formal behavior result, including ManualMovementCancel, must retain one exact 0/1 initial-loop discard provenance value.");
        }

        private static void QaParticipantOwnsHundredFishMeasurementAndTerminalResult()
        {
            const string runId = "1234567890abcdef1234567890abcdef";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var settings = new AutoFishingQaSettings
            {
                AutoFishingPerformanceEnabled = true,
                AutoFishingPerformanceProfile = "FishLoop",
                AutoFishingPerformanceTargetFish = 100,
                AutoFishingPerformanceWarmupFish = 5,
                AutoFishingPerformanceZeroWarmupSeconds = 0,
                AutoFishingPerformanceZeroMeasureSeconds = 1
            };
            settings.NormalizeAndValidate();
            var fixture = new AutoFishingPerformanceOrchestrator(
                settings,
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));

            DateTimeOffset started = DateTimeOffset.UtcNow;
            AutoFishingPerformanceFixtureSnapshot Snapshot(long pullExited, int seconds) => new AutoFishingPerformanceFixtureSnapshot
            {
                ObservedAtUtc = started.AddSeconds(seconds),
                PullExited = pullExited,
                AccessorBuilds = 1,
                Casts = pullExited,
                Fish = pullExited,
                Sessions = 1,
                HookRuntimes = 1,
                NativeTransient = 1,
                NativeReferences = 2,
                SelectedRod = true,
                NativeMovementAvailable = true,
                VisibleReelQueued = pullExited,
                VisibleReelConsumed = pullExited
            };

            Assert(fixture.Observe(Snapshot(100, 0)) == AutoFishingPerformanceFixtureUpdate.None, "QA measurement should bind its initial PullExit count.");
            for (int warmup = 1; warmup < 5; warmup++)
                Assert(fixture.Observe(Snapshot(100 + warmup, warmup)) == AutoFishingPerformanceFixtureUpdate.None, "QA measurement must retain all five warm-up transitions.");
            Assert(fixture.Observe(Snapshot(105, 5)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted, "Runner warmup=5 must reach the QA probe unchanged.");
            for (int measured = 1; measured < 100; measured++)
                Assert(fixture.Observe(Snapshot(105 + measured, 5 + measured)) == AutoFishingPerformanceFixtureUpdate.None, "QA orchestration must not report terminal before measured fish 100.");
            Assert(fixture.Observe(Snapshot(205, 105)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted, "Runner target=100 must reach Observe and complete exactly at fish 100.");
            Assert(fixture.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()) && fixture.TerminalSucceeded,
                "A zero-root title snapshot must produce the terminal verified result.");

            FishingPerformanceResult result = fixture.ResultForTests
                ?? throw new InvalidOperationException("Product QA orchestrator lost its performance result.");
            Assert(result.TargetFish == 100 && result.WarmupFish == 5 && result.MeasuredFish == 100 && result.TitleCleanupVerified,
                "Settings, probe parameters, Observe transitions, and terminal result must retain target=100/warmup=5 end to end.");
            string[] resultFiles = Directory.GetFiles(runtime.Paths.EvidencePath, "auto-fishing-performance.json", SearchOption.AllDirectories);
            Assert(resultFiles.Length == 1 && File.ReadAllText(resultFiles[0]).Contains("\"MeasuredFish\":100", StringComparison.Ordinal),
                "QA must export exactly one terminal 100-fish result outside the activation stage tree.");
            var status = runtime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "Smoke.AutoFishingPerformance");
            Assert(status.Status == "verified" && status.Details.Contains("owner=qa; fallback=false", StringComparison.Ordinal),
                "Terminal status authority must be QA-owned and must never claim an embedded fallback.");

            var invalidSettings = new AutoFishingQaSettings
            {
                AutoFishingPerformanceEnabled = true,
                AutoFishingPerformanceProfile = "FishLoop",
                AutoFishingPerformanceTargetFish = 0,
                AutoFishingPerformanceWarmupFish = 5,
                AutoFishingPerformanceZeroWarmupSeconds = 0,
                AutoFishingPerformanceZeroMeasureSeconds = 1
            };
            AssertThrows<InvalidDataException>(invalidSettings.NormalizeAndValidate,
                "Product QA settings must reject a zero-target FishLoop before orchestrator creation.");
        }

        private static void AutoFishingNativeControlPolicyAdvancesLiveNonNormalSessions()
        {
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, false, string.Empty, false, false, false) ==
                    AutoFishingNativeControlFixtureAction.WaitForNormalState,
                "A non-normal state without a live native-control session must not acquire or act.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(true, false, string.Empty, false, false, false) ==
                    AutoFishingNativeControlFixtureAction.AcquireSession,
                "A new native-control session may be acquired only from NormalGameState.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, true, "Idle", true, true, false) ==
                    AutoFishingNativeControlFixtureAction.Observe,
                "A live non-normal Idle session must remain observable without starting a new cast.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, true, "BiteReady", true, true, false) ==
                    AutoFishingNativeControlFixtureAction.ReelVisible,
                "A live BiteReady session must allow its visible reel while native fishing is non-normal.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, true, "BiteReady", true, true, true) ==
                    AutoFishingNativeControlFixtureAction.Observe,
                "The same BiteReady sequence must not apply its visible reel twice.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, true, "PullExited", false, true, false) ==
                    AutoFishingNativeControlFixtureAction.Observe,
                "A live non-normal PullExited session must remain available to completion and cleanup logic.");
            Assert(
                AutoFishingNativeControlFixturePolicy.Decide(false, false, "PullExited", true, true, false) ==
                    AutoFishingNativeControlFixtureAction.WaitForNormalState,
                "Releasing a session must restore the initial NormalGameState acquisition gate.");

            DateTimeOffset started = DateTimeOffset.UtcNow;
            Assert(AutoFishingNativeControlFixturePolicy.HasProgress(string.Empty, -1, "Idle", 0),
                "The first native-control snapshot must start the progress watchdog.");
            Assert(!AutoFishingNativeControlFixturePolicy.HasProgress("Idle", 0, "Idle", 0),
                "An unchanged phase and sequence must not reset the progress watchdog.");
            Assert(AutoFishingNativeControlFixturePolicy.HasProgress("Idle", 0, "Idle", 1) &&
                AutoFishingNativeControlFixturePolicy.HasProgress("Idle", 1, "BiteReady", 1),
                "Either sequence or phase progress must reset the watchdog.");
            Assert(!AutoFishingNativeControlFixturePolicy.IsStalled(started, started.AddSeconds(90), TimeSpan.FromSeconds(90)) &&
                AutoFishingNativeControlFixturePolicy.IsStalled(started, started.AddSeconds(91), TimeSpan.FromSeconds(90)),
                "The native-control watchdog must fail only after a complete 90-second no-progress window.");
            Assert(!AutoFishingNativeControlFixturePolicy.HasVerifiedVisibleReelCycle(false, 0, 1, 0, 1, 0, 1) &&
                !AutoFishingNativeControlFixturePolicy.HasVerifiedVisibleReelCycle(true, 0, 1, 0, 0, 0, 0) &&
                !AutoFishingNativeControlFixturePolicy.HasVerifiedVisibleReelCycle(true, 0, 1, 0, 1, 0, 0) &&
                AutoFishingNativeControlFixturePolicy.HasVerifiedVisibleReelCycle(true, 5, 6, 7, 8, 9, 10),
                "L4 recovery must bind applied, queued, consumed, and native-accepted evidence from the same visible-reel cycle.");
            Assert(!AutoFishingNativeControlFixturePolicy.HasSufficientVisibleReelUnits(10, 0, 0) &&
                !AutoFishingNativeControlFixturePolicy.HasSufficientVisibleReelUnits(10, 10, 9) &&
                AutoFishingNativeControlFixturePolicy.HasSufficientVisibleReelUnits(10, 10, 10),
                "L0 measured fish must not pass without at least one consumed and native-accepted visible reel per measured PullExited unit.");
        }

        private static void AutoFishingNativeVitalsUseOnlyOfficialCommandDelegatesAndFailClosed()
        {
            FakeDolocApi.Reset(energyPercent: 0.2f, spiritPercent: 0.4f, fishingEnergyCost: 10);
            AssertThrows<InvalidOperationException>(
                () => AutoFishingNativeVitalsCommandAdapter.Create(typeof(FakeDolocApi)),
                "Production native-vitals resolution must fail closed when a command name maps to a non-official delegate Method.DeclaringType/Name identity.");
            AutoFishingNativeVitalsCommandAdapter adapter = AutoFishingNativeVitalsCommandAdapter.CreateForTests(typeof(FakeDolocApi));
            AutoFishingNativeVitalsReceipt prepared = adapter.PrepareWorkload(
                "unit-initial-save",
                1,
                AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase);
            Assert(prepared.WorkloadStart && prepared.EnergyCommandInvoked && prepared.SpiritCommandInvoked &&
                prepared.Source == AutoFishingNativeVitalsReceipt.OfficialCommandSource &&
                prepared.SaveLoadOrdinal == 1 && prepared.WorkloadPhase == AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase &&
                !string.IsNullOrWhiteSpace(prepared.ComposeEnergyDelegateIdentity) &&
                prepared.ComposeEnergyDelegateIdentity != AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity &&
                AutoFishingNativeVitalsCommandAdapter.IsFull(prepared.EnergyPercentAfter) &&
                AutoFishingNativeVitalsCommandAdapter.IsFull(prepared.SpiritPercentAfter) &&
                FakeDolocApi.ComposeEnergyCalls == 1 && FakeDolocApi.ComposeSpiritCalls == 1,
                "Batch 5 AutoFishing preparation must resolve and invoke only the approved official command delegates, then prove full percent readback.");
            Batch6AutoFishingNativeVitalsReceipt rejectedMapped = Batch6AutoFishingNativeVitalsEvidenceMapper.From(prepared);
            Assert(!rejectedMapped.ReadbackVerified && !rejectedMapped.DelegateIdentitiesVerified,
                "Batch 6 evidence must not promote the test-only delegate identities into an official native-vitals receipt.");
            Batch6AutoFishingNativeVitalsReceipt mappedPrepared = Batch6AutoFishingNativeVitalsEvidenceMapper.From(
                CreateNativeVitalsWorkloadReceipt("unit-official-initial-save"));
            Assert(mappedPrepared.Kind == "WorkloadStart" && mappedPrepared.ReadbackVerified && mappedPrepared.DelegateIdentitiesVerified &&
                mappedPrepared.Source == AutoFishingNativeVitalsReceipt.OfficialCommandSource &&
                Math.Abs(mappedPrepared.EnergyPercentBefore - 0.5d) < 0.0001d && Math.Abs(mappedPrepared.EnergyPercentAfter - 1d) < 0.0001d &&
                Math.Abs(mappedPrepared.SpiritPercentBefore - 0.5d) < 0.0001d && Math.Abs(mappedPrepared.SpiritPercentAfter - 1d) < 0.0001d,
                "Batch 6 native-vitals evidence must retain official source/delegate identity and energy/spirit pre/post readback values.");

            FakeDolocApi.SetVitals(energyPercent: 0.05f, spiritPercent: 0.2f);
            Assert(adapter.TryMaintainWorkload("unit-active-window", out AutoFishingNativeVitalsReceipt? maintenance) && maintenance != null &&
                maintenance.MaintenanceRefill && maintenance.NativeEnergyInsufficientObserved &&
                maintenance.EnergyCommandInvoked && maintenance.SpiritCommandInvoked &&
                maintenance.NativeEnergySufficientAfter && maintenance.NativeEnergyReserveSufficientAfter,
                "The native vitals adapter must classify a real HasEnoughEnergy failure before refilling, so the fixture can fail instead of timing out as lifecycle stall.");

            FakeDolocApi.SetVitals(energyPercent: 0.5f, spiritPercent: 0.5f);
            AutoFishingNativeVitalsReceipt final = adapter.ObserveMeasurementEnd("unit-measurement-end");
            Assert(final.FinalObservation && !final.EnergyCommandInvoked && !final.SpiritCommandInvoked &&
                Math.Abs(final.EnergyPercentAfter - 0.5f) < 0.0001f &&
                Math.Abs(final.SpiritPercentAfter - 0.5f) < 0.0001f,
                "Measurement-end vitals must be a read-only official-percent observation and must not top up after the measured window.");
            Batch6AutoFishingNativeVitalsReceipt mappedFinal = Batch6AutoFishingNativeVitalsEvidenceMapper.From(
                CreateNativeVitalsFinalReceipt("unit-official-measurement-end"));
            Assert(mappedFinal.Kind == "FinalReadback" && mappedFinal.ReadbackVerified &&
                Math.Abs(mappedFinal.EnergyPercentBefore - mappedFinal.EnergyPercentAfter) < 0.000001d &&
                Math.Abs(mappedFinal.SpiritPercentBefore - mappedFinal.SpiritPercentAfter) < 0.000001d,
                "Batch 6 measurement-end evidence must be an official readback-only receipt rather than a synthesized zero/default metric.");

            AssertThrows<InvalidOperationException>(
                () => AutoFishingNativeVitalsCommandAdapter.CreateForTests(typeof(FakeDolocApiWithWrongSpiritCommand)),
                "A missing or wrong official command delegate signature must fail closed before a workload starts.");
        }

        private static AutoFishingNativeVitalsReceipt CreateNativeVitalsWorkloadReceipt(
            string context,
            int saveLoadOrdinal = 1,
            string workloadPhase = AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase) =>
            new AutoFishingNativeVitalsReceipt
            {
                Context = context,
                WorkloadStart = true,
                SaveLoadOrdinal = saveLoadOrdinal,
                WorkloadPhase = workloadPhase,
                EnergyCommandInvoked = true,
                SpiritCommandInvoked = true,
                NativeEnergySufficientAfter = true,
                NativeEnergyReserveSufficientAfter = true,
                FishingEnergyCost = 10,
                EnergyPercentBefore = 0.5f,
                EnergyPercentAfter = 1f,
                SpiritPercentBefore = 0.5f,
                SpiritPercentAfter = 1f,
                ComposeEnergyDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity
            };

        private static AutoFishingNativeVitalsReceipt CreateNativeVitalsMaintenanceReceipt(string context) =>
            new AutoFishingNativeVitalsReceipt
            {
                Context = context,
                MaintenanceRefill = true,
                EnergyCommandInvoked = true,
                SpiritCommandInvoked = true,
                NativeEnergySufficientAfter = true,
                NativeEnergyReserveSufficientAfter = true,
                FishingEnergyCost = 10,
                EnergyPercentBefore = 0.4f,
                EnergyPercentAfter = 1f,
                SpiritPercentBefore = 0.2f,
                SpiritPercentAfter = 1f,
                ComposeEnergyDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity
            };

        private static AutoFishingNativeVitalsReceipt CreateNativeVitalsL4CheckpointReceipt(string context) =>
            new AutoFishingNativeVitalsReceipt
            {
                Context = context,
                L4RecoveryCheckpoint = true,
                EnergyCommandInvoked = true,
                SpiritCommandInvoked = true,
                NativeEnergySufficientAfter = true,
                NativeEnergyReserveSufficientAfter = true,
                FishingEnergyCost = 10,
                EnergyPercentBefore = 0.8f,
                EnergyPercentAfter = 1f,
                SpiritPercentBefore = 0.7f,
                SpiritPercentAfter = 1f,
                ComposeEnergyDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity
            };

        private static AutoFishingNativeVitalsReceipt CreateNativeVitalsFinalReceipt(string context) =>
            new AutoFishingNativeVitalsReceipt
            {
                Context = context,
                FinalObservation = true,
                NativeEnergySufficientAfter = true,
                NativeEnergyReserveSufficientAfter = true,
                FishingEnergyCost = 10,
                EnergyPercentBefore = 0.8f,
                EnergyPercentAfter = 0.8f,
                SpiritPercentBefore = 0.7f,
                SpiritPercentAfter = 0.7f,
                ComposeEnergyDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity
            };

        private static void Batch5AutoFishingL0RejectsPullExitWithoutVisibleReelReceipt()
        {
            AutoFishingPerformanceOrchestrator Create(string runId)
            {
                string gameDir = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-auto-fishing-l0-receipt", runId);
                Directory.CreateDirectory(gameDir);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var settings = new AutoFishingQaSettings
                {
                    AutoFishingPerformanceEnabled = true,
                    AutoFishingPerformanceProfile = "FishLoop",
                    AutoFishingPerformanceTargetFish = 1,
                    AutoFishingPerformanceWarmupFish = 0,
                    Batch5GcLadderEnabled = true,
                    Batch5GcLadderDomain = "AutoFishing",
                    Batch5GcLadderLevel = "L0",
                    Batch5GcLadderWorkload = "FishLoop",
                    Batch5GcLadderMultiplier = 1d,
                    Batch5GcLadderMeasureSeconds = 1,
                    Batch5GcLadderSampleSeconds = 1,
                    Batch5GcLadderTargetUnits = 1,
                    G6AutoFishingScenario = "CombinedInstantSkip",
                    SaveSlot = 5
                };
                return new AutoFishingPerformanceOrchestrator(
                    settings,
                    new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
            }

            AutoFishingPerformanceFixtureSnapshot Snapshot(DateTimeOffset at, long pullExited, long visibleReels) =>
                new AutoFishingPerformanceFixtureSnapshot
                {
                    ObservedAtUtc = at,
                    PullExited = pullExited,
                    Casts = pullExited,
                    Fish = pullExited,
                    Sessions = 1,
                    InputLeases = 1,
                    HookRuntimes = 1,
                    NativeReferences = 1,
                    SelectedRod = true,
                    NativeMovementAvailable = true,
                    VisibleReelQueued = visibleReels,
                    VisibleReelConsumed = visibleReels,
                    VisibleReelNativeAccepted = visibleReels
                };

            DateTimeOffset started = DateTimeOffset.UtcNow;
            AutoFishingPerformanceOrchestrator missing = Create("l0missingreceipt");
            Assert(missing.Observe(Snapshot(started, 0, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted &&
                missing.Observe(Snapshot(started.AddSeconds(1), 1, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted,
                "The negative L0 receipt test must reach the measurement boundary.");
            FishingPerformanceResult missingResult = missing.ResultForTests
                ?? throw new InvalidOperationException("The negative L0 receipt test lost its result.");
            Assert(missingResult.Status == "failed-batch5-l0-visible-reel-receipt" &&
                !missingResult.WorkloadCompleted && !missingResult.BehaviorVerified,
                "A PullExited-only L0 measurement must fail closed without consumed/native-accepted visible reels.");

            AutoFishingPerformanceOrchestrator verified = Create("l0verifiedreceipt");
            Assert(verified.Observe(Snapshot(started, 0, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted &&
                verified.Observe(Snapshot(started.AddSeconds(1), 1, 1)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted,
                "The positive L0 receipt test must reach the measurement boundary.");
            FishingPerformanceResult verifiedResult = verified.ResultForTests
                ?? throw new InvalidOperationException("The positive L0 receipt test lost its result.");
            Assert(verifiedResult.Status == "completed" && verifiedResult.WorkloadCompleted && verifiedResult.BehaviorVerified &&
                verifiedResult.VisibleReelConsumedDelta == 1 && verifiedResult.VisibleReelNativeAcceptedDelta == 1,
                "A verified L0 measurement must bind one consumed/native-accepted visible reel to its measured PullExited unit.");
        }

        private static void Batch5AutoFishingL4AndL5RequireBehaviorReceipts()
        {
            AutoFishingPerformanceOrchestrator Create(string level, string runId, out DtmApiRuntime runtime)
            {
                string gameDir = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-auto-fishing-gc", runId);
                Directory.CreateDirectory(gameDir);
                runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var settings = new AutoFishingQaSettings
                {
                    AutoFishingPerformanceEnabled = true,
                    AutoFishingPerformanceProfile = "FishLoop",
                    AutoFishingPerformanceTargetFish = 1,
                    AutoFishingPerformanceWarmupFish = 0,
                    Batch5GcLadderEnabled = true,
                    Batch5GcLadderDomain = "AutoFishing",
                    Batch5GcLadderLevel = level,
                    Batch5GcLadderWorkload = "FishLoop",
                    Batch5GcLadderMultiplier = 2d,
                    Batch5GcLadderMeasureSeconds = 1,
                    Batch5GcLadderSampleSeconds = 1,
                    Batch5GcLadderTargetUnits = 1,
                    G6AutoFishingScenario = "CombinedInstantSkip",
                    SaveSlot = 5
                };
                return new AutoFishingPerformanceOrchestrator(
                    settings,
                    new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
            }

            AutoFishingPerformanceFixtureSnapshot Snapshot(DateTimeOffset at, long pullExited) => new AutoFishingPerformanceFixtureSnapshot
            {
                ObservedAtUtc = at,
                PullExited = pullExited,
                Casts = pullExited,
                Fish = pullExited,
                Sessions = 1,
                HookRuntimes = 1,
                NativeTransient = 1,
                NativeReferences = 1,
                SelectedRod = true,
                NativeMovementAvailable = true
            };

            DateTimeOffset now = DateTimeOffset.UtcNow;
            AutoFishingPerformanceOrchestrator l4 = Create("L4", "44444444444444444444444444444444", out _);
            l4.RecordBatch5NativeVitals(CreateNativeVitalsWorkloadReceipt("unit-l4-initial-save"));
            Assert(l4.Observe(Snapshot(now, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted,
                "Batch 5 L4 must open its timed measurement before periodic maintenance is counted.");
            l4.RecordBatch5NativeVitals(CreateNativeVitalsMaintenanceReceipt("unit-l4-measurement-maintenance"));
            Assert(l4.Observe(Snapshot(now.AddSeconds(2), 1)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted,
                "Batch 5 L4 must complete its timed one-fish measurement before recovery semantics.");
            l4.RecordBatch5NativeVitals(CreateNativeVitalsFinalReceipt("unit-l4-measurement-end"));
            l4.RecordBatch5NativeVitals(CreateNativeVitalsL4CheckpointReceipt(AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext));
            l4.MarkBatch5DisableRecovery(1, "unit-product-disabled-native-recovery");
            Assert(l4.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()) && l4.TerminalSucceeded,
                "Batch 5 L4 must reach terminal only after one product-disabled native recovery unit and zero title cleanup.");
            FishingPerformanceResult l4Result = l4.ResultForTests!;
            Assert(l4Result.SchemaVersion == 3 && l4Result.Domain == "AutoFishing" && l4Result.Level == "L4" &&
                l4Result.Batch5Level == "L4" && l4Result.Workload == "FishLoop" && l4Result.ProductState == "enabled-then-disabled" &&
                Math.Abs(l4Result.Multiplier - 2d) < 0.000001d && l4Result.MeasureSeconds == 1 && l4Result.SampleSeconds == 1 &&
                l4Result.TargetUnits == 1 && l4Result.RequiredActiveDurationSeconds == 1 && l4Result.SaveSlot == 5 && !l4Result.ForcedGc &&
                l4Result.Scenario == "CombinedInstantSkip" && l4Result.Profile == "FishLoop" && l4Result.TargetFish == 1 &&
                l4Result.WarmupFish == 0 && l4Result.MeasuredFish == 1 && l4Result.CompletedUnits == 1 && l4Result.WorkloadCompleted &&
                l4Result.ElapsedSeconds >= 1d && l4Result.ActiveWindowSatisfied &&
                l4Result.BehaviorReceiptKind == "disable-recovery-after-active-window" && l4Result.BehaviorVerified &&
                !l4Result.TitleCycleObserved && l4Result.DisableRecoveryVerified && l4Result.NativeRecoveryUnits == 1 &&
                l4Result.NativeVitalsVerified && l4Result.NativeVitalsReadbackVerified && l4Result.NativeVitalsFinalReadbackVerified &&
                l4Result.NativeVitalsPrepareCount == 1 && l4Result.NativeVitalsMaintenanceReceiptCount == 1 &&
                l4Result.NativeVitalsMeasurementMaintenanceReceiptCount == 1 &&
                l4Result.NativeVitalsL4CheckpointReceiptCount == 1 && l4Result.NativeVitalsL4CheckpointVerified &&
                l4Result.NativeVitalsL4CheckpointContext == AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext &&
                l4Result.NativeVitalsL4CheckpointEnergyCommandInvoked && l4Result.NativeVitalsL4CheckpointSpiritCommandInvoked &&
                l4Result.NativeVitalsInitialSavePrepareCount == 1 && l4Result.NativeVitalsInitialSavePrepareOrdinal == 1 &&
                l4Result.NativeVitalsPostReloadPrepareCount == 0 && l4Result.NativeVitalsPostReloadPrepareOrdinal == 0 &&
                l4Result.NativeVitalsFinalObservationCount == 1 && !l4Result.NativeEnergyExhaustionObserved &&
                l4Result.NativeTryCastEnergyGateUnavailableDelta == 0 && l4Result.NativeTryCastInsufficientEnergyDelta == 0 &&
                l4Result.NativeVitalsDelegateIdentitiesVerified &&
                l4Result.NativeFishingEnergyCost == 10 && l4Result.NativeVitalsMaintenanceCadenceMilliseconds == 250 &&
                Math.Abs(l4Result.NativeEnergyPercentAtStageStart - 1d) < 0.000001d &&
                Math.Abs(l4Result.NativeEnergyPercentAtMeasurementEnd - 0.8d) < 0.000001d,
                "Batch 5 L4 result schema must bind its full stage identity, active window, and independent recovery receipt.");

            AutoFishingPerformanceOrchestrator l5 = Create("L5", "55555555555555555555555555555555", out _);
            l5.RecordBatch5NativeVitals(CreateNativeVitalsWorkloadReceipt("unit-l5-initial-save"));
            Assert(l5.Observe(Snapshot(now, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted,
                "Batch 5 L5 must open its timed measurement before periodic maintenance is counted.");
            l5.RecordBatch5NativeVitals(CreateNativeVitalsMaintenanceReceipt("unit-l5-measurement-maintenance"));
            Assert(l5.Observe(Snapshot(now.AddSeconds(2), 1)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted,
                "Batch 5 L5 must complete its timed one-fish measurement before the title cycle.");
            l5.RecordBatch5NativeVitals(CreateNativeVitalsFinalReceipt("unit-l5-measurement-end"));
            Assert(l5.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()) && !l5.TerminalSucceeded,
                "The first L5 title cleanup must remain non-terminal while fifth-save reload behavior is pending.");
            l5.RecordBatch5NativeVitals(CreateNativeVitalsWorkloadReceipt(
                "unit-l5-post-reload",
                2,
                AutoFishingNativeVitalsReceipt.PostReloadWorkloadPhase));
            l5.MarkBatch5TitleReloadCycle(2, reenabledAfterReload: true, disabledAfterReload: true, source: "unit-title-reload-loop-disable");
            Assert(l5.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()) && l5.TerminalSucceeded,
                "Batch 5 L5 must become terminal only after reload, re-enable, loop, disable, and final title cleanup.");
            FishingPerformanceResult l5Result = l5.ResultForTests!;
            Assert(l5Result.Batch5Level == "L5" && l5Result.Batch5InitialTitleCleanupVerified &&
                l5Result.TitleReloadCycleVerified && l5Result.TitleReloadSaveLoads == 2 &&
                l5Result.ReenabledAfterReload && l5Result.DisabledAfterReload &&
                l5Result.SchemaVersion == 3 && l5Result.Domain == "AutoFishing" && l5Result.Level == "L5" &&
                l5Result.Workload == "FishLoop" && l5Result.ProductState == "enabled" && l5Result.SaveSlot == 5 && !l5Result.ForcedGc &&
                l5Result.ActiveWindowSatisfied && l5Result.BehaviorReceiptKind == "title-reload-after-active-window" &&
                l5Result.BehaviorVerified && l5Result.TitleCycleObserved && l5Result.NativeVitalsVerified &&
                l5Result.NativeVitalsPrepareCount == 2 && l5Result.NativeVitalsFinalObservationCount == 1 &&
                l5Result.NativeVitalsMaintenanceReceiptCount == 1 && l5Result.NativeVitalsMeasurementMaintenanceReceiptCount == 1 &&
                l5Result.NativeVitalsInitialSavePrepareCount == 1 && l5Result.NativeVitalsInitialSavePrepareOrdinal == 1 &&
                l5Result.NativeVitalsPostReloadPrepareCount == 1 && l5Result.NativeVitalsPostReloadPrepareOrdinal == 2 &&
                l5Result.NativeVitalsL4CheckpointReceiptCount == 0 && !l5Result.NativeVitalsL4CheckpointVerified &&
                l5Result.NativeTryCastEnergyGateUnavailableDelta == 0 && l5Result.NativeTryCastInsufficientEnergyDelta == 0 &&
                !l5Result.NativeEnergyExhaustionObserved && l5Result.NativeVitalsDelegateIdentitiesVerified,
                "Batch 5 L5 result schema must bind its full stage identity and complete behavior-level title reload receipt.");
        }

        private static void Batch5ActionSpeedGcLadderRequiresFullActiveWindowAndLevelSemantics()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var activeWindow = new Batch5GcActiveWindow(TimeSpan.FromSeconds(10), 3);
            Assert(activeWindow.IsUnitDue(now), "ActionSpeed active window must drive its first unit immediately.");
            activeWindow.RecordUnit(now);
            Assert(!activeWindow.IsSatisfied && !activeWindow.IsUnitDue(now.AddSeconds(4)),
                "ActionSpeed active window must not collapse its remaining units into the start of the measure window.");
            Assert(activeWindow.IsUnitDue(now.AddSeconds(5)), "ActionSpeed active window must schedule its middle unit across the measure window.");
            activeWindow.RecordUnit(now.AddSeconds(5));
            Assert(!activeWindow.IsSatisfied && activeWindow.IsUnitDue(now.AddSeconds(10)),
                "ActionSpeed active window must retain its final unit until the complete measure duration.");
            activeWindow.RecordUnit(now.AddSeconds(10));
            Assert(activeWindow.IsSatisfied && activeWindow.CompletedUnits == 3 && activeWindow.ActiveDurationSeconds == 10d,
                "ActionSpeed active window must require both target units and the complete duration.");

            var oneUnitWindow = new Batch5GcActiveWindow(TimeSpan.FromSeconds(10), 1);
            oneUnitWindow.RecordUnit(now);
            Assert(!oneUnitWindow.IsSatisfied && oneUnitWindow.IsUnitDue(now.AddSeconds(10)),
                "A nominal one-unit ladder must still drive work at both ends of the measurement window.");
            oneUnitWindow.RecordUnit(now.AddSeconds(10));
            Assert(oneUnitWindow.IsSatisfied && oneUnitWindow.CompletedUnits == 2,
                "A nominal one-unit ladder must not terminate at the beginning of the measurement window.");

            Batch5GcLadderOrchestrator Create(string level, string workload, string runId, out DtmApiRuntime runtime)
            {
                string gameDir = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-action-gc", runId);
                Directory.CreateDirectory(gameDir);
                runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var settings = new QaHostSettings
                {
                    Batch5GcLadderEnabled = true,
                    Batch5GcLadderDomain = "ActionSpeed",
                    Batch5GcLadderLevel = level,
                    Batch5GcLadderWorkload = workload,
                    Batch5GcLadderMultiplier = level == "L3" ? 4d : level == "L0" || level == "L1" ? 1d : 2d,
                    Batch5GcLadderMeasureSeconds = 10,
                    Batch5GcLadderSampleSeconds = 1,
                    Batch5GcLadderTargetUnits = 3,
                    SaveSlot = 3
                };
                var value = new Batch5GcLadderOrchestrator(
                    settings,
                    new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
                value.OnSaveLoaded();
                return value;
            }

            ActionSpeedGcLadderProgress Progress(
                string level,
                string workload,
                DateTimeOffset observedAt,
                double activeSeconds,
                bool activeSatisfied,
                bool fixtureCompleted,
                int recoveryUnits = 0)
            {
                return new ActionSpeedGcLadderProgress
                {
                    Level = level,
                    Workload = workload,
                    ObservedAtUtc = observedAt,
                    CompletedUnits = 3,
                    FirstUnitAtUtc = now,
                    LastUnitAtUtc = now.AddSeconds(activeSeconds),
                    ActiveDurationSeconds = activeSeconds,
                    ActiveWindowSatisfied = activeSatisfied,
                    RecoveryVerified = recoveryUnits > 0,
                    RecoveryUnits = recoveryUnits,
                    BehaviorReceiptKind = fixtureCompleted ? Batch5GcLadderBehavior.ExpectedActionSpeedReceiptKind(level) : string.Empty,
                    FixtureCompleted = fixtureCompleted
                };
            }

            Batch5GcLadderOrchestrator l2 = Create("L2", "Tool", "22222222222222222222222222222220", out DtmApiRuntime l2Runtime);
            l2.Update(Progress("L2", "Tool", now.AddSeconds(1), 1d, activeSatisfied: false, fixtureCompleted: false));
            Assert(!l2.TerminalSucceeded && l2.ResultForTests.Status == "measuring-active-workload",
                "ActionSpeed must not complete after target units when most of the measurement window is idle.");
            l2.Update(Progress("L2", "Tool", now.AddSeconds(10), 10d, activeSatisfied: true, fixtureCompleted: true));
            Batch5GcLadderStageResult l2Result = l2.ResultForTests;
            Assert(l2.TerminalSucceeded && l2Result.SchemaVersion == 2 && l2Result.WorkloadCompleted &&
                l2Result.CompletedUnits == 3 && l2Result.ActiveDurationSeconds == 10d && l2Result.ActiveWindowSatisfied &&
                l2Result.BehaviorReceiptKind == "common-multiplier-active-window" && l2Result.BehaviorVerified &&
                !l2Result.RecoveryVerified && !l2Result.TitleCycleObserved,
                "ActionSpeed L2 result must bind the exact active duration, unit, workload, and common-multiplier semantics.");
            string[] l2RawFiles = Directory.GetFiles(l2Runtime.Paths.EvidencePath, "batch5-gc-ladder-stage.json", SearchOption.AllDirectories);
            Assert(l2RawFiles.Length == 1, "ActionSpeed terminal must write exactly one raw runtime result.");
            using (JsonDocument rawDocument = JsonDocument.Parse(File.ReadAllText(l2RawFiles[0])))
            {
                JsonElement raw = rawDocument.RootElement;
                Assert(raw.GetProperty("SchemaVersion").GetInt32() == 2 &&
                    raw.GetProperty("Level").GetString() == "L2" && raw.GetProperty("Workload").GetString() == "Tool" &&
                    raw.GetProperty("CompletedUnits").GetInt32() == 3 && raw.GetProperty("ActiveDurationSeconds").GetDouble() == 10d &&
                    raw.GetProperty("ActiveWindowSatisfied").GetBoolean() && raw.GetProperty("BehaviorVerified").GetBoolean(),
                    "ActionSpeed raw JSON must retain the strict-bind identity, active-window, and behavior fields.");
            }

            Batch5GcLadderOrchestrator l4 = Create("L4", "Eat", "44444444444444444444444444444440", out _);
            l4.Update(Progress("L4", "Eat", now.AddSeconds(10), 10d, activeSatisfied: true, fixtureCompleted: false));
            Assert(!l4.TerminalSucceeded, "ActionSpeed L4 must remain non-terminal after the active window until native recovery is observed.");
            l4.Update(Progress("L4", "Eat", now.AddSeconds(11), 10d, activeSatisfied: true, fixtureCompleted: true, recoveryUnits: 1));
            Assert(l4.TerminalSucceeded && l4.ResultForTests.RecoveryVerified && l4.ResultForTests.RecoveryUnits == 1 &&
                l4.ResultForTests.BehaviorReceiptKind == "disable-recovery-after-active-window",
                "ActionSpeed L4 result must retain its independent post-disable native recovery receipt.");

            Batch5GcLadderOrchestrator l5 = Create("L5", "ContinuousUse", "55555555555555555555555555555550", out _);
            l5.Update(Progress("L5", "ContinuousUse", now.AddSeconds(10), 10d, activeSatisfied: true, fixtureCompleted: true));
            Assert(!l5.TerminalSucceeded && l5.ShouldRequestReturnHome &&
                l5.ResultForTests.Status == "waiting-title-cycle" && !l5.ResultForTests.BehaviorVerified,
                "ActionSpeed L5 must wait for an actual returned-to-title boundary after its full active window.");
            l5.OnReturnedToTitle();
            Assert(l5.TerminalSucceeded && l5.ResultForTests.TitleCycleObserved && l5.ResultForTests.BehaviorVerified &&
                l5.ResultForTests.BehaviorReceiptKind == "title-cycle-after-active-window",
                "ActionSpeed L5 must publish terminal title-cycle semantics only after ReturnedToTitle.");

            Batch5GcLadderOrchestrator invalid = Create("L3", "Interact", "33333333333333333333333333333330", out _);
            AssertThrows<InvalidOperationException>(() =>
                invalid.Update(Progress("L3", "Interact", now.AddSeconds(1), 1d, activeSatisfied: false, fixtureCompleted: true)),
                "ActionSpeed orchestrator must fail closed when a fixture claims completion before the full active window.");
            Assert(invalid.ResultForTests.Status == "failed" && !invalid.ResultForTests.BehaviorVerified,
                "An invalid ActionSpeed active-window receipt must be durably represented as a failed terminal.");
        }

        private static void GenericQaParticipantDoesNotOwnAutoFishingPerformance()
        {
            const string runId = "11111111111111111111111111111111";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            byte[] settingsBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                AutoFishingPerformanceEnabled = true,
                AutoFishingPerformanceProfile = "FishLoop"
            }));
            var factory = new QaHostFactory();
            var context = new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, settingsBytes);
            GameBridgeFixtureStartupOptions options = factory.PrepareStartupOptions(context);
            var participant = (QaHostParticipant)factory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
            Assert(!((object)participant is IAutoFishingPerformanceFixture) &&
                typeof(QaHostParticipant).GetProperty("PerformanceResultForTests", BindingFlags.Instance | BindingFlags.NonPublic) == null,
                "The generic GameBridge QA participant must not implement or expose the product AutoFishing performance fixture.");
            participant.Start();
            participant.Close("unit-generic-close");
            Assert(!runtime.Diagnostics.GetHookStatuses().Any(item => item.HookId.Contains("AutoFishing", StringComparison.OrdinalIgnoreCase)) &&
                (!Directory.Exists(runtime.Paths.EvidencePath) || !Directory.GetFiles(runtime.Paths.EvidencePath, "auto-fishing-performance.json", SearchOption.AllDirectories).Any()),
                "Retired AutoFishing settings must be inert in the generic QA host and must not emit product receipts.");

            string fixtureSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Performance",
                "Batch5NoDemandProfileFixture.cs"));
            Assert(fixtureSource.Contains("CaptureBatch5NoDemandRuntimeSnapshot", StringComparison.Ordinal) &&
                fixtureSource.Contains("\"BATCH5-NO-DEMAND\"", StringComparison.Ordinal) &&
                fixtureSource.Contains("SchemaVersion { get; set; } = 4", StringComparison.Ordinal) &&
                !fixtureSource.Contains("FishingNativeFrameRefreshes", StringComparison.Ordinal) &&
                !fixtureSource.Contains("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal),
                "The current live no-demand measurement must be a generic QA fixture over mandatory Runtime counters, without a ProductNative fishing dependency or synthesized counter.");
        }

        private static void Batch5NoDemandSettingsRequireExplicitPositiveBoundary()
        {
            const string runId = "10101010101010101010101010101010";
            QaHostSettings Read(object value) => QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));

            QaHostSettings valid = Read(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                Batch5NoDemandEnabled = true,
                Batch5NoDemandWarmupFrames = 3,
                Batch5NoDemandTargetFrames = 10,
                SaveSlot = 3
            });
            valid.Validate(runId);
            Assert(valid.Batch5NoDemandEnabled && valid.Batch5NoDemandWarmupFrames == 3 &&
                valid.Batch5NoDemandTargetFrames == 10 && valid.SaveSlot == 3,
                "The generic Batch 5 no-demand settings must preserve their explicit third-save frame boundary.");

            foreach (QaHostSettings invalid in new[]
            {
                Read(new
                {
                    schemaVersion = QaHostProtocol.SchemaVersion,
                    protocolVersion = QaHostProtocol.ProtocolVersion,
                    runId,
                    mode = QaHostProtocol.ParticipantOnlyMode,
                    Batch5NoDemandEnabled = true,
                    Batch5NoDemandWarmupFrames = 0,
                    Batch5NoDemandTargetFrames = 10,
                    SaveSlot = 3
                }),
                Read(new
                {
                    schemaVersion = QaHostProtocol.SchemaVersion,
                    protocolVersion = QaHostProtocol.ProtocolVersion,
                    runId,
                    mode = QaHostProtocol.ParticipantOnlyMode,
                    Batch5NoDemandEnabled = true,
                    Batch5NoDemandWarmupFrames = 3,
                    Batch5NoDemandTargetFrames = 0,
                    SaveSlot = 3
                }),
                Read(new
                {
                    schemaVersion = QaHostProtocol.SchemaVersion,
                    protocolVersion = QaHostProtocol.ProtocolVersion,
                    runId,
                    mode = QaHostProtocol.ParticipantOnlyMode,
                    Batch5NoDemandEnabled = true,
                    Batch5NoDemandWarmupFrames = 3,
                    Batch5NoDemandTargetFrames = 10,
                    SaveSlot = 0
                })
            })
            {
                AssertThrows<InvalidDataException>(() => invalid.Validate(runId),
                    "The generic Batch 5 no-demand route must fail closed without an explicit positive save/warm-up/measurement boundary.");
            }
        }

        private static void QaPerformanceWritesAreRetryableBeforeTerminalCommit()
        {
            const string runId = "22222222222222222222222222222222";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var settings = new AutoFishingQaSettings
            {
                AutoFishingPerformanceEnabled = true,
                AutoFishingPerformanceProfile = "FishLoop",
                AutoFishingPerformanceTargetFish = 1,
                AutoFishingPerformanceWarmupFish = 0,
                AutoFishingPerformanceZeroWarmupSeconds = 0,
                AutoFishingPerformanceZeroMeasureSeconds = 1
            };
            settings.NormalizeAndValidate();
            var fixture = new AutoFishingPerformanceOrchestrator(
                settings,
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));

            DateTimeOffset now = DateTimeOffset.UtcNow;
            AutoFishingPerformanceFixtureSnapshot Snapshot(long fish, int seconds) => new AutoFishingPerformanceFixtureSnapshot
            {
                ObservedAtUtc = now.AddSeconds(seconds),
                PullExited = fish,
                Casts = fish,
                Fish = fish,
                Sessions = 1,
                HookRuntimes = 1,
                NativeTransient = 1,
                NativeReferences = 1,
                SelectedRod = true,
                NativeMovementAvailable = true
            };
            Assert(fixture.Observe(Snapshot(0, 0)) == AutoFishingPerformanceFixtureUpdate.MeasurementStarted,
                "A zero-warmup focused probe should enter measurement immediately.");

            Directory.CreateDirectory(runtime.Paths.EvidencePath);
            string resultRoot = Path.Combine(runtime.Paths.EvidencePath, "AUTO-FISHING-PERF");
            File.WriteAllText(resultRoot, "block-directory-create");
            AssertThrows<IOException>(() => fixture.Observe(Snapshot(1, 1)),
                "A measurement receipt write failure must surface before reporting MeasurementCompleted.");
            File.Delete(resultRoot);
            Assert(fixture.Observe(Snapshot(1, 2)) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted,
                "A prepared measurement must retry its durable result/status write on the next observation.");

            string resultPath = Directory.GetFiles(runtime.Paths.EvidencePath, "auto-fishing-performance.json", SearchOption.AllDirectories).Single();
            File.Delete(resultPath);
            Directory.CreateDirectory(resultPath);
            AssertThrows<UnauthorizedAccessException>(() => fixture.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()),
                "A terminal overwrite failure must surface before the QA terminal flag commits.");
            Directory.Delete(resultPath);
            Assert(fixture.CompleteTitleCleanup(new AutoFishingPerformanceTitleCleanupSnapshot()) && fixture.TerminalSucceeded,
                "After the durable terminal write path recovers, the same cleanup must remain retryable and commit success exactly once.");
        }

        private static void G3CustomEntityContractIsOwnerBoundReadOnlyAndDisposable()
        {
            const string runId = "33333333333333333333333333333333";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                CustomEntityContractEnabled = true,
                ObserveSaveLoaded = true
            }));
            var factory = new QaHostFactory();
            var context = new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes);
            GameBridgeFixtureStartupOptions options = factory.PrepareStartupOptions(context);
            Assert(options.RunId == runId,
                "G3 startup projection must bind the optional participant without retaining per-case embedded-disable switches.");
            var participant = (QaHostParticipant)factory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
            participant.Start();
            Assert(participant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue,
                "The initial title boundary before SaveLoaded must not close the G3 participant.");
            participant.OnSaveLoaded(2, false);

            Assert(runtime.CustomEntities.GetAnimalSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetMonsterSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetAttackSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetDroneSnapshot().RegisteredDefinitionCount == 0,
                "The owner-bound CustomEntity QA transaction must dispose every in-memory definition before returning.");
            var customStatus = runtime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "Smoke.CustomEntityApis");
            Assert(customStatus.Status == "verified" && customStatus.Details.Contains("runtimeVerbs=0", StringComparison.Ordinal) &&
                customStatus.Details.Contains("cleanupVerified=true", StringComparison.Ordinal),
                "CustomEntity G3 status must prove zero runtime verbs and verified owner cleanup.");
            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Close,
                "After SaveLoaded completes the requested G3 probe, the deterministic pre-exit boundary may close the participant.");
            participant.Close("smoke-auto-exit:unit-g3-custom");

            int cleanupAttempts = 0;
            var retryAccess = new GameBridgeFixtureAccess(runtime, "55555555555555555555555555555555", runtime.Paths.DtmApiPath, () => true);
            var retryProbe = new CustomEntityRegistrationContractProbe(
                retryAccess,
                () => new CustomEntityRegistrationFixtureSession(
                    runtime,
                    "55555555555555555555555555555555",
                    (ownerId, reason) =>
                    {
                        cleanupAttempts++;
                        if (cleanupAttempts == 1)
                            throw new IOException("injected RemoveOwner failure");
                        return runtime.CustomEntities.RemoveOwner(ownerId, reason);
                    }));
            string retrySummary = retryProbe.Run();
            Assert(cleanupAttempts == 2 && retrySummary.Contains("cleanupVerified=true", StringComparison.Ordinal),
                "The real CustomEntity probe path must retry one failed RemoveOwner and return a verified cleanup receipt.");
            Assert(runtime.CustomEntities.GetAnimalSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetMonsterSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetAttackSnapshot().RegisteredDefinitionCount == 0 &&
                runtime.CustomEntities.GetDroneSnapshot().RegisteredDefinitionCount == 0,
                "The real retrying CustomEntity probe must leave every owner snapshot at zero.");

            string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown.QA", "G3", "CustomEntityRegistrationContractProbe.cs"));
            foreach (string forbidden in new[]
            {
                "RequestSpawn(", "SpawnProjectile(", "ExecuteAttack(", "RequestSummon(",
                "RequestRemove(", "Despawn(", "Expire(", "Dismiss(", ".Equip(", ".SetMode("
            })
                Assert(!source.Contains(forbidden, StringComparison.Ordinal), "The G3 registration probe exposes a forbidden runtime verb: " + forbidden);
        }

        private static void G3LifecycleNotificationsAndCallbackOrderAreDeterministic()
        {
            const string runId = "44444444444444444444444444444444";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ObserveSaveLoaded = true,
                ObserveSaveSaved = true,
                ObserveWorkshopReloadCompleted = true
            }));
            var factory = new QaHostFactory();
            var context = new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes);
            factory.PrepareStartupOptions(context);
            var participant = (QaHostParticipant)factory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true));
            participant.Start();
            participant.OnSaveLoaded(2, false);
            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Continue,
                "ReturnHome before SaveSaved/Workshop completion must keep the QA participant attached.");
            participant.OnSaveSaved(2);
            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Continue,
                "ReturnHome before Workshop completion must keep the QA participant attached.");
            participant.OnWorkshopReloadCompleted();
            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Close,
                "ReturnHome may close only after every requested G3 lifecycle terminal is complete.");
            participant.Close("unit-g3-lifecycle");
            foreach (string hookId in new[]
            {
                "Smoke.QaLifecycle.SaveLoaded",
                "Smoke.QaLifecycle.SaveSaved",
                "Smoke.QaLifecycle.WorkshopReloadCompleted"
            })
            {
                var status = runtime.Diagnostics.GetHookStatuses().Single(item => item.HookId == hookId);
                Assert(status.Status == "verified" && status.Details.Contains("owner=qa; fallback=false", StringComparison.Ordinal),
                    hookId + " did not retain QA ownership and terminal observation evidence.");
            }

            const string incompleteRunId = "77777777777777777777777777777777";
            byte[] incompleteBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = incompleteRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ObserveSaveLoaded = true,
                ObserveSaveSaved = true,
                ObserveWorkshopReloadCompleted = true
            }));
            var incompleteFactory = new QaHostFactory();
            var incompleteContext = new QaHostPreparationContext(runtime, incompleteRunId, runtime.Paths.DtmApiPath, incompleteBytes);
            incompleteFactory.PrepareStartupOptions(incompleteContext);
            var incompleteParticipant = (QaHostParticipant)incompleteFactory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, incompleteRunId, runtime.Paths.DtmApiPath, () => true));
            incompleteParticipant.Start();
            incompleteParticipant.OnSaveLoaded(2, false);
            Assert(incompleteParticipant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Continue,
                "An incomplete lifecycle participant must survive ReturnHome for later shutdown classification.");
            AssertThrows<InvalidOperationException>(() => incompleteParticipant.Close("unit-incomplete-shutdown"),
                "A real shutdown Close with incomplete SaveSaved/Workshop requirements must fail closed.");

            const string failureCleanupRunId = "88888888888888888888888888888888";
            byte[] failureCleanupBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = failureCleanupRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ObserveSaveLoaded = true
            }));
            var failureCleanupFactory = new QaHostFactory();
            failureCleanupFactory.PrepareStartupOptions(new QaHostPreparationContext(runtime, failureCleanupRunId, runtime.Paths.DtmApiPath, failureCleanupBytes));
            var failureCleanupParticipant = (QaHostParticipant)failureCleanupFactory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, failureCleanupRunId, runtime.Paths.DtmApiPath, () => true));
            failureCleanupParticipant.Start();
            failureCleanupParticipant.Close("failure:update");
            failureCleanupParticipant.Close("idempotent-after-failure");

            AssertOrderedLifecycleFailureIsolation();

            string callbacks = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "DolocTownHookCallbacks.cs"));
            Assert(callbacks.Contains("RunSaveSavedQaLifecycle(", StringComparison.Ordinal) &&
                callbacks.Contains("bool runtimeSucceeded = TryLifecycleCallback(\"SaveGame.NotifySaveSaved\"", StringComparison.Ordinal) &&
                callbacks.Contains("bool productionSucceeded = TryLifecycleCallback(\"SaveGame.NotifyEquipmentSlotsSaved\"", StringComparison.Ordinal) &&
                callbacks.Contains("if (runtimeSucceeded && productionSucceeded)", StringComparison.Ordinal),
                "SaveSaved must execute Runtime and production persistence independently and gate only QA on both receipts.");
            Assert(callbacks.Contains("RunWorkshopQaLifecycle(", StringComparison.Ordinal) &&
                callbacks.Contains("bool captureSucceeded = TryLifecycleReceipt(\"Workshop.CaptureNativeSubscriptions\"", StringComparison.Ordinal) &&
                callbacks.Contains("bool runtimeSucceeded = TryLifecycleCallback(\"Workshop.NotifyModListChanged\"", StringComparison.Ordinal) &&
                callbacks.Contains("if (captureSucceeded && runtimeSucceeded)", StringComparison.Ordinal) &&
                callbacks.Contains("Workshop.NotifyQaHostReloadCompleted", StringComparison.Ordinal),
                "Workshop capture and Runtime dispatch must produce independent receipts and jointly gate the optional participant notification.");
            Assert(!callbacks.Contains("MarkSmoke", StringComparison.Ordinal) && !callbacks.Contains("legacyMarker", StringComparison.Ordinal),
                "G7 production callbacks must contain no direct embedded Smoke-marker fallback.");
        }

        private static void G3FishRoeObservationAndRunnerProjectionAreQaOwned()
        {
            Assert(FishRoeTooltipObservationProbe.ContainsDecoration("Roe (fish)", string.Empty, string.Empty) &&
                FishRoeTooltipObservationProbe.ContainsDecoration(string.Empty, "Hatches: fish", string.Empty) &&
                !FishRoeTooltipObservationProbe.ContainsDecoration("Roe", "plain", "plain"),
                "The extracted FishRoe observer must recognize only the decorated tooltip forms.");

            string runner = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1"));
            string observer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown.QA", "G3", "FishRoeTooltipObservationProbe.cs"));
            Assert(observer.Contains("Batch6AdvancedHarmonyOwnerObserver.Observe", StringComparison.Ordinal) &&
                observer.Contains("dtmapi.mod.yuuka.dtmapi.fishbreedingassistant", StringComparison.Ordinal) &&
                observer.Contains("owner.ExactOwnerPatchCount != 1", StringComparison.Ordinal),
                "FishRoe observation must prove the product's exact one-patch Harmony owner before accepting visible text.");
            Assert(runner.Contains("protocolVersion = 7", StringComparison.Ordinal) &&
                runner.Contains("CustomEntityContractEnabled = $CustomEntityContractEnabled", StringComparison.Ordinal) &&
                runner.Contains("FishRoeTooltipObservationEnabled = $FishRoeTooltipObservationEnabled", StringComparison.Ordinal) &&
                runner.Contains("DiagnosticsSnapshotEnabled = $DiagnosticsSnapshotEnabled", StringComparison.Ordinal) &&
                runner.Contains("ObserveWorkshopReloadCompleted = $ObserveWorkshopReloadCompleted", StringComparison.Ordinal),
                "Runner must project the complete G5 settings surface while preserving the G3 cases.");
            Assert(runner.Contains("FishRoe, diagnostics, and QA lifecycle observation switches require -StageQaHost", StringComparison.Ordinal) &&
                runner.Contains("$qaCustomEntityContractEnabled = [bool]$StageQaHost -and [bool]$AutoExerciseCustomEntityApis", StringComparison.Ordinal) &&
                runner.Contains("$qaFishRoeTooltipObservationEnabled = [bool]$StageQaHost", StringComparison.Ordinal) &&
                runner.Contains("QA CustomEntity, FishRoe, and diagnostics scenarios require a positive -SaveSlot", StringComparison.Ordinal),
                "G3 runner cases must require explicit staging and route existing CustomEntity/experimental FishRoe cases to exactly one owner.");
            int hookProbeBranch = runner.IndexOf("if ($probeOk -and $IncludeHookProbe -and $SaveSlot -gt 0)", StringComparison.Ordinal);
            int g3Branch = runner.IndexOf("elseif ($probeOk -and $qaG3SaveLoadedBranchRequested)", hookProbeBranch, StringComparison.Ordinal);
            int legacyBranch = runner.IndexOf("elseif ($probeOk -and $AutoExerciseTitleButtonLifecycle", g3Branch, StringComparison.Ordinal);
            Assert(hookProbeBranch >= 0 && g3Branch > hookProbeBranch && legacyBranch > g3Branch,
                "Pure G3 SaveLoaded must have an explicit runner branch before legacy scenario routing.");
            Assert(runner.Contains("$qaDiagnosticsExpectedFeatureIds = @('FishRoeTooltip')", StringComparison.Ordinal) &&
                runner.Contains("-DiagnosticsExpectedFeatureIds $qaDiagnosticsExpectedFeatureIds", StringComparison.Ordinal),
                "Standalone diagnostics must carry an explicit non-empty expected feature set.");
            string routingOutput = RunQaG3RoutingSelfTest();
            Assert(routingOutput.Contains("\"QaG3SaveLoadedBranchRequested\": true", StringComparison.OrdinalIgnoreCase) &&
                routingOutput.Contains("FishRoeTooltip", StringComparison.Ordinal),
                "Executed runner routing self-test did not select the pure G3 SaveLoaded branch and explicit diagnostics feature set.");

            string participantSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            Assert(participantSource.Contains("bool completed = RunG3Probe(\"Smoke.FishRoeTooltip\"", StringComparison.Ordinal) &&
                participantSource.Contains("fishRoeCompleted = fishRoeCompleted || completed", StringComparison.Ordinal) &&
                participantSource.Contains("if (settings.FishRoeTooltipObservationEnabled && !fishRoeCompleted)", StringComparison.Ordinal),
                "G3 completion must consume a verified QA FishRoe result instead of treating scenario selection as success.");
            string diagnosticsProbe = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown.QA", "G3", "DiagnosticsSnapshotProbe.cs"));
            Assert(diagnosticsProbe.Contains("snapshot.Mods.Count == 0", StringComparison.Ordinal),
                "Extracted diagnostics must reject an empty Mods collection.");

            const string invalidDiagnosticsRunId = "66666666666666666666666666666666";
            byte[] invalidDiagnostics = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = invalidDiagnosticsRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                DiagnosticsSnapshotEnabled = true,
                DiagnosticsExpectedFeatureIds = Array.Empty<string>()
            }));
            QaHostSettings invalidSettings = QaHostSettings.Read(invalidDiagnostics);
            AssertThrows<InvalidDataException>(() => invalidSettings.Validate(invalidDiagnosticsRunId),
                "Diagnostics settings must reject an empty expected feature set before activation.");
            string qaProject = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "DTMAPI.GameBridge.DolocTown.QA", "DTMAPI.GameBridge.DolocTown.QA.csproj"));
            Assert(!qaProject.Contains("DTMAPI.BepInExBootstrap", StringComparison.Ordinal), "The optional QA host must not reference Bootstrap.");
        }

        private static void AssertOrderedLifecycleFailureIsolation()
        {
            int runtimeCalls = 0;
            int productionCalls = 0;
            int qaCalls = 0;
            DolocTownHookCallbacks.RunSaveSavedIfNativeSucceededForTests(
                nativeSaveSucceeded: false,
                () => runtimeCalls++,
                () => productionCalls++,
                () => qaCalls++);
            Assert(runtimeCalls == 0 && productionCalls == 0 && qaCalls == 0,
                "A failed native SaveGame result must not publish SaveSaved, commit product persistence, or verify QA.");

            DolocTownHookCallbacks.RunSaveSavedIfNativeSucceededForTests(
                nativeSaveSucceeded: true,
                () => runtimeCalls++,
                () => productionCalls++,
                () => qaCalls++);
            Assert(runtimeCalls == 1 && productionCalls == 1 && qaCalls == 1,
                "A successful native SaveGame result must publish the ordered SaveSaved lifecycle exactly once.");

            runtimeCalls = productionCalls = qaCalls = 0;
            DolocTownHookCallbacks.RunSaveSavedQaLifecycleForTests(
                () => { runtimeCalls++; throw new InvalidOperationException("injected Runtime failure"); },
                () => productionCalls++,
                () => qaCalls++);
            Assert(runtimeCalls == 1 && productionCalls == 1 && qaCalls == 0,
                "Runtime SaveSaved failure must not skip production persistence or verify QA.");

            runtimeCalls = productionCalls = qaCalls = 0;
            DolocTownHookCallbacks.RunSaveSavedQaLifecycleForTests(
                () => runtimeCalls++,
                () => { productionCalls++; throw new InvalidOperationException("injected persistence failure"); },
                () => qaCalls++);
            Assert(runtimeCalls == 1 && productionCalls == 1 && qaCalls == 0,
                "Production persistence failure must suppress QA verification while retaining Runtime calls.");

            int workshopCaptureCalls = 0;
            int workshopRuntimeCalls = 0;
            qaCalls = 0;
            DolocTownHookCallbacks.RunWorkshopQaLifecycleForTests(
                () => { workshopCaptureCalls++; return false; },
                () => workshopRuntimeCalls++,
                () => qaCalls++);
            Assert(workshopCaptureCalls == 1 && workshopRuntimeCalls == 1 && qaCalls == 0,
                "Capture=false must not block Runtime, but must suppress QA verification.");

            workshopCaptureCalls = workshopRuntimeCalls = qaCalls = 0;
            DolocTownHookCallbacks.RunWorkshopQaLifecycleForTests(
                () => { workshopCaptureCalls++; return true; },
                () => { workshopRuntimeCalls++; throw new InvalidOperationException("injected Workshop Runtime failure"); },
                () => qaCalls++);
            Assert(workshopCaptureCalls == 1 && workshopRuntimeCalls == 1 && qaCalls == 0,
                "Workshop Runtime failure must suppress QA while retaining one capture attempt.");

            qaCalls = 0;
            DolocTownHookCallbacks.RunWorkshopQaLifecycleForTests(() => true, () => { }, () => qaCalls++);
            Assert(qaCalls == 1, "Successful Workshop prerequisites must notify QA exactly once.");
        }

        private static string RunQaG3RoutingSelfTest()
        {
            string runner = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1");
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in new[]
            {
                "-NoLogo", "-NoProfile", "-File", runner,
                "-StageQaHost", "-AutoExerciseDiagnosticsSnapshot", "-SaveSlot", "3", "-ValidateQaG3RoutingOnly"
            })
                startInfo.ArgumentList.Add(argument);

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the G3 runner routing self-test.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("G3 runner routing self-test failed: " + error);
            return output;
        }

        private static void G4ContinuousHomePageRequiresUninterruptedWindow()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;
            var observation = new ContinuousHomePageObservation(2d);

            Assert(!observation.Observe("HomePageUiState", start) &&
                !observation.Observe("HomePageUiState", start.AddSeconds(1.75d)),
                "A continuous HomePage terminal must not complete before its full observation window.");
            Assert(!observation.Observe("Gameplay", start.AddSeconds(1.8d)) &&
                observation.ObservedAt == DateTimeOffset.MinValue,
                "One non-HomePage frame must reset the continuous observation timer instead of preserving accumulated time.");
            Assert(!observation.Observe("HomePageUiState", start.AddSeconds(2d)) &&
                !observation.Observe("HomePageUiState", start.AddSeconds(3.99d)) &&
                observation.Observe("HomePageUiState", start.AddSeconds(4d)) &&
                observation.Terminal,
                "After an interruption, the full HomePage window must be observed again before the terminal becomes sticky.");
            Assert(observation.Observe("Gameplay", start.AddSeconds(5d)),
                "A verified continuous terminal should remain terminal after completion.");
        }

        private static void ScenarioStableObservationRequiresContinuousIndependentWindows()
        {
            DateTimeOffset start = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset gameplayObservedAt = default;
            DateTimeOffset homePageObservedAt = default;

            Assert(!QaScenarioController.HasContinuousStableObservationForFixture(true, start, 1.5, ref gameplayObservedAt),
                "The first normal Gameplay frame must start, not satisfy, the READY stability window.");
            Assert(!QaScenarioController.HasContinuousStableObservationForFixture(true, start.AddSeconds(1.49), 1.5, ref gameplayObservedAt),
                "READY must not publish before normal Gameplay has remained continuous for the entire interval.");
            Assert(QaScenarioController.HasContinuousStableObservationForFixture(true, start.AddSeconds(1.5), 1.5, ref gameplayObservedAt),
                "READY may publish after the independent normal Gameplay observation window completes.");

            Assert(!QaScenarioController.HasContinuousStableObservationForFixture(true, start.AddSeconds(30), 1.5, ref homePageObservedAt),
                "The first HomePage frame after ReturnHome must start a separate title-stability window even when the request is old.");
            Assert(!QaScenarioController.HasContinuousStableObservationForFixture(false, start.AddSeconds(31), 1.5, ref homePageObservedAt) && homePageObservedAt == default,
                "Losing HomePageUiState must reset the continuous title observation window.");
            Assert(!QaScenarioController.HasContinuousStableObservationForFixture(true, start.AddSeconds(40), 1.5, ref homePageObservedAt),
                "Re-entering HomePageUiState after a reset must begin a new window.");
            Assert(QaScenarioController.HasContinuousStableObservationForFixture(true, start.AddSeconds(41.5), 1.5, ref homePageObservedAt),
                "HomePage stability may pass only after the restarted continuous observation interval completes.");
        }

        private static void G4ReturnHomeWaitsForPostUiGameplayQuiescence()
        {
            string root = FindRepositoryRoot();
            string participantSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string returnHome = SliceBetween(participantSource, "private bool ShouldRequestReturnHome()", "private bool AllRequirementsComplete()");
            Assert(returnHome.Contains("onlyPostTitleRequirementsRemain", StringComparison.Ordinal) &&
                returnHome.Contains("scenarios.IsNormalGameplay()", StringComparison.Ordinal) &&
                returnHome.Contains("1d", StringComparison.Ordinal) &&
                returnHome.Contains("ref returnHomeQuiescenceObservedAt", StringComparison.Ordinal),
                "Ordinary ReturnHome must wait for one continuous normal-Gameplay second after the last in-save/native-UI terminal, so an exact UI pop cannot race scene teardown in the same frame.");
        }

        private static void G4FixtureSessionsRetainReceiptsUntilVerifiedCleanup()
        {
            int writesWithoutOriginalReceipt = 0;
            var unreadable = new HatchVoiceFixtureSession(
                (out bool adultValue) => { adultValue = false; return false; },
                _ => writesWithoutOriginalReceipt++,
                () => { },
                null);
            AssertThrows<InvalidOperationException>(() => unreadable.Run(),
                "Hatch must fail before mutation when the original adult state cannot be read explicitly.");
            Assert(writesWithoutOriginalReceipt == 0 && unreadable.Close().Contains("armed=false; restored=true", StringComparison.Ordinal),
                "An unreadable Hatch original state must produce zero writes and a no-op cleanup receipt.");

            var hatchTrace = new List<string>();
            bool adult = true;
            bool restorationPhase = false;
            int ignoredRestoreWrites = 0;
            var hatch = new HatchVoiceFixtureSession(
                (out bool adultValue) =>
                {
                    hatchTrace.Add("read:" + adult);
                    adultValue = adult;
                    return true;
                },
                value =>
                {
                    hatchTrace.Add("write:" + value);
                    if (restorationPhase && value && ignoredRestoreWrites++ == 0)
                        return;
                    adult = value;
                },
                () => hatchTrace.Add("hatch"),
                () =>
                {
                    hatchTrace.Add("vanilla");
                    restorationPhase = true;
                    adult = false;
                });

            AssertThrows<InvalidOperationException>(() => hatch.Run(),
                "Hatch Close must fail when the original adult-state write is not confirmed by readback.");
            Assert(hatchTrace.Count > 0 && hatchTrace[0] == "read:True" && !adult,
                "The Hatch receipt must capture the original adult state before the first mutation and remain pending after failed readback.");
            Assert(hatchTrace.Take(7).SequenceEqual(new[] { "read:True", "write:False", "read:False", "hatch", "write:True", "read:True", "hatch" }),
                "Both temporary Hatch adult-state writes must be explicitly read back before their matching sound call.");
            string hatchClose = hatch.Close();
            int hatchWritesAfterVerifiedClose = hatchTrace.Count(item => item.StartsWith("write:", StringComparison.Ordinal));
            Assert(adult && hatchClose.Contains("restored=True", StringComparison.Ordinal),
                "A later Hatch Close must retry the native write and verify restoration before becoming terminal.");
            Assert(hatch.Close() == hatchClose &&
                hatchTrace.Count(item => item.StartsWith("write:", StringComparison.Ordinal)) == hatchWritesAfterVerifiedClose,
                "Verified Hatch cleanup must be idempotent and must not mutate the native state again.");

        }

        private static void G4NativeUiCloseUsesExactGenericRemoveReceipt()
        {
            var manager = new FakeGenericUiManager();
            Assert(QaScenarioController.TryInvokeExactGenericRemoveUiForFixture(manager, typeof(FakeOwnedUiState)) &&
                manager.RemovedType == typeof(FakeOwnedUiState),
                "G4 native UI cleanup must close the exact owned state through RemoveUI<T>() with its concrete runtime type.");
            Assert(!QaScenarioController.TryInvokeExactGenericRemoveUiForFixture(new FakeNonGenericUiManager(), typeof(FakeOwnedUiState)) &&
                !QaScenarioController.TryInvokeExactGenericRemoveUiForFixture(manager, typeof(string)),
                "Missing/non-generic RemoveUI or a generic constraint failure must fail closed instead of claiming cleanup.");

            var exact = new FakeOwnedUiState();
            object? current = exact;
            int popCalls = 0;
            int removeCalls = 0;
            bool stateExitCompleted = false;
            Assert(QaScenarioController.TryCloseExactNativeUiReceiptForFixture(
                    exact,
                    () => current,
                    state =>
                    {
                        popCalls++;
                        Assert(ReferenceEquals(state, exact), "Exact native UI cleanup must pop the owned receipt instance.");
                        current = new object();
                        return true;
                    },
                    stateType =>
                    {
                        removeCalls++;
                        return stateType == typeof(FakeOwnedUiState);
                    },
                    ref stateExitCompleted) &&
                stateExitCompleted && popCalls == 1 && removeCalls == 1,
                "Exact current-state ownership must pop once, verify the state changed, then remove the concrete cached UI type.");

            var mismatch = new FakeOwnedUiState();
            current = new object();
            popCalls = 0;
            removeCalls = 0;
            stateExitCompleted = false;
            Assert(!QaScenarioController.TryCloseExactNativeUiReceiptForFixture(
                    mismatch,
                    () => current,
                    _ => { popCalls++; return true; },
                    _ => { removeCalls++; return true; },
                    ref stateExitCompleted) &&
                !stateExitCompleted && popCalls == 0 && removeCalls == 0,
                "A current-state mismatch must retain the receipt without popping or removing any UI.");

            var rejected = new FakeOwnedUiState();
            current = rejected;
            popCalls = 0;
            removeCalls = 0;
            stateExitCompleted = false;
            Assert(!QaScenarioController.TryCloseExactNativeUiReceiptForFixture(
                    rejected,
                    () => current,
                    _ => { popCalls++; return false; },
                    _ => { removeCalls++; return true; },
                    ref stateExitCompleted) &&
                !stateExitCompleted && popCalls == 1 && removeCalls == 0,
                "A false TryPopState receipt must fail closed before cache removal.");

            var retry = new FakeOwnedUiState();
            current = retry;
            popCalls = 0;
            removeCalls = 0;
            stateExitCompleted = false;
            Assert(!QaScenarioController.TryCloseExactNativeUiReceiptForFixture(
                    retry,
                    () => current,
                    _ =>
                    {
                        popCalls++;
                        current = new object();
                        return true;
                    },
                    _ => ++removeCalls > 1,
                    ref stateExitCompleted) &&
                stateExitCompleted && popCalls == 1 && removeCalls == 1,
                "A cache-removal failure after a verified pop must retain the completed state-exit phase for retry.");
            Assert(QaScenarioController.TryCloseExactNativeUiReceiptForFixture(
                    retry,
                    () => current,
                    _ => { popCalls++; return true; },
                    _ => ++removeCalls > 1,
                    ref stateExitCompleted) &&
                stateExitCompleted && popCalls == 1 && removeCalls == 2,
                "A cache-removal retry must not pop the state a second time.");
        }

        private static void G4NativeUiTargetedReceiptsUseExactRepairHooks()
        {
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var bridge = new DolocTownGameBridge(runtime);
            FieldInfo featureField = typeof(DolocTownGameBridge).GetField("nativeUiLayoutDiagnosticsFeature", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Native UI repair feature field is unavailable.");
            var feature = (NativeUiLayoutDiagnosticsFeature)(featureField.GetValue(bridge)
                ?? throw new InvalidOperationException("Native UI repair feature was not registered."));

            SetPrivateBooleanProperty(feature, nameof(NativeUiLayoutDiagnosticsFeature.HomePageRenderTextMenuPatched), true);
            SetPrivateBooleanProperty(feature, nameof(NativeUiLayoutDiagnosticsFeature.MainMenuPanelStartShowPatched), true);
            SetPrivateBooleanProperty(feature, nameof(NativeUiLayoutDiagnosticsFeature.MenuUiSetCapacityPatched), true);
            Assert(feature.HomePageRepairReceiptReady && feature.MainMenuRepairReceiptReady && feature.TargetedRepairReady,
                "Targeted HomePage/MainMenu repair readiness must depend on the production RepairService and exact menu hooks.");

            feature.PublishHookStatuses();
            IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "UI.NativeLayoutRepair");
            Assert(status.Status == "ready" &&
                status.Details.Contains("HomePage.RenderTextMenu=True", StringComparison.Ordinal) &&
                status.Details.Contains("MainMenuPanel.OnStartShow=True", StringComparison.Ordinal) &&
                status.Details.Contains("MenuUI.SetCapacity=True", StringComparison.Ordinal) &&
                !status.Details.Contains("ResetLayoutSize=", StringComparison.Ordinal) &&
                status.Details.Contains("Generic DolocGridUI<T> hooks", StringComparison.Ordinal),
                "Published Native UI readiness must report only the exact production repairs and identify generic hooks as retired.");

            var controller = new QaScenarioController(
                new GameBridgeFixtureAccess(runtime, "11111111111111111111111111111111", runtime.Paths.DtmApiPath, () => true, bridge));
            MethodInfo validate = typeof(QaScenarioController).GetMethod("TryValidateNativeUiLayoutObservation", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Native UI QA validator is unavailable.");
            object?[] homeArgs =
            {
                new NativeUiLayoutObservation("HomePageUiState", "HomePageUiState", "DolocTown.UI.HomePageTextMenu", 1, 1, 1),
                true,
                null
            };
            object?[] mainArgs =
            {
                new NativeUiLayoutObservation("MainMenuUiState", "MainMenuUiState", "DolocTown.UI.MenuUI", 6, 6, 6),
                false,
                null
            };
            Assert(validate.Invoke(controller, homeArgs) is bool homeValid && homeValid &&
                validate.Invoke(controller, mainArgs) is bool mainValid && mainValid &&
                Convert.ToString(homeArgs[2]) == "repairService=true; home=true; main=true" &&
                Convert.ToString(mainArgs[2]) == "repairService=true; home=true; main=true",
                "The QA validator must accept concrete Home/Main observations using only exact repair receipts.");
        }

        private static void G4BridgeDoesNotReplayStartupTitleAcrossSaveLoaded()
        {
            const string runId = "99999999999999999999999999999999";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            string externalEvidenceRoot = Path.Combine(runtime.Paths.DtmApiPath, "runner-evidence");
            Directory.CreateDirectory(externalEvidenceRoot);
            string marker = Path.Combine(externalEvidenceRoot, "external-player-input-complete.signal");
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ExternalPlayerInputObservationEnabled = true,
                ExternalPlayerInputToken = "unit-input-token",
                ExternalPlayerInputMarkerPath = marker,
                ExternalPlayerInputEvidenceRoot = externalEvidenceRoot,
                ContinuousHomePageSeconds = 0.25d
            }));
            var factory = new QaHostFactory();
            var context = new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes);
            GameBridgeFixtureStartupOptions options = factory.PrepareStartupOptions(context);
            var bridge = new DolocTownGameBridge(runtime, null, new PreparedQaHost(factory, context, options));
            bridge.StartQaHostForTests();

            runtime.NotifyReturnedToTitle();
            Assert(bridge.QaHostAttachedForTests && !bridge.QaHostClosedForTests && !bridge.QaHostFailureObservedForTests,
                "The startup title boundary must remain a pre-save continuation for a save-required G4 case.");
            runtime.NotifySaveLoaded(isNewGame: false);
            bridge.UpdateQaHostForTests();

            IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.ExternalPlayerInput");
            Assert(bridge.QaHostAttachedForTests && bridge.QaHostStartedForTests && !bridge.QaHostClosedForTests &&
                !bridge.QaHostClosingForTests && !bridge.QaHostFailureObservedForTests && bridge.QaHostUpdateCountForTests == 1 &&
                status.Status == "pending",
                "SaveLoaded must clear the stale startup-title receipt before Update so the in-save G4 case remains pending instead of being replayed as ReturnedToTitle.");
        }

        private static void G4ContinuousHomePageGenerationWaitsForSaveReturnBoundary()
        {
            const string runId = "cccccccccccccccccccccccccccccccc";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            runtime.UI.SetUiContext("HomePageUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "unit-generation-boundary");
            var bridge = new DolocTownGameBridge(runtime);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ContinuousHomePageTerminalEnabled = true,
                ContinuousHomePageRequiresSaveLoaded = true,
                ContinuousHomePageSeconds = 0.25d
            }));
            var factory = new QaHostFactory();
            factory.PrepareStartupOptions(new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes));
            var participant = (QaHostParticipant)factory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true, bridge));
            participant.Start();

            Assert(participant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue,
                "A save-generation-bound continuous terminal must ignore the startup title boundary.");
            participant.Update();
            Thread.Sleep(300);
            participant.Update();
            IHookStatusInfo startup = runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.QaContinuousHomePage");
            Assert(startup.Status == "pending" &&
                !(bool)(typeof(QaHostParticipant).GetField("postTitleBoundaryObserved", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(participant) ?? false),
                "Startup HomePage time must not arm or complete a terminal that belongs to the next save generation.");

            participant.OnSaveLoaded(2, false);
            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Continue,
                "The real post-SaveLoaded ReturnedToTitle boundary must arm a fresh continuous HomePage observation.");
            participant.Update();
            Thread.Sleep(300);
            participant.Update();
            IHookStatusInfo terminal = runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.QaContinuousHomePage");
            Assert(terminal.Status == "verified" && participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Close,
                "Only uninterrupted HomePage time after the real save-generation return may complete the terminal.");
            participant.Close("unit-save-generation-terminal");
        }

        private static void G4TitleOnlyCasesEnterPostTitleWithoutSaveLoaded()
        {
            const string titleRunId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            string titleGameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(titleGameDir);
            var titleRuntime = new DtmApiRuntime(new FakeHost(titleGameDir), new ConfigMenuRegistry());
            var titleBridge = new DolocTownGameBridge(titleRuntime);
            byte[] titleBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = titleRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                TitleSettingsUiEnabled = true,
                OfficialModUiEnabled = true
            }));
            var titleFactory = new QaHostFactory();
            titleFactory.PrepareStartupOptions(new QaHostPreparationContext(titleRuntime, titleRunId, titleRuntime.Paths.DtmApiPath, titleBytes));
            var titleParticipant = (QaHostParticipant)titleFactory.CreateParticipant(
                new GameBridgeFixtureAccess(titleRuntime, titleRunId, titleRuntime.Paths.DtmApiPath, () => true, titleBridge));
            titleParticipant.Start();
            Assert(titleParticipant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue &&
                (bool)(typeof(QaHostParticipant).GetField("postTitleBoundaryObserved", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(titleParticipant) ?? false),
                "TitleSettings/Official title-only G4 cases must enter their post-title generation even when no save has loaded.");

            const string terminalRunId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            string terminalGameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(terminalGameDir);
            var terminalRuntime = new DtmApiRuntime(new FakeHost(terminalGameDir), new ConfigMenuRegistry());
            terminalRuntime.UI.SetUiContext("HomePageUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "unit-title-only");
            var terminalBridge = new DolocTownGameBridge(terminalRuntime);
            byte[] terminalBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = terminalRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ContinuousHomePageTerminalEnabled = true,
                ContinuousHomePageSeconds = 0.25d
            }));
            var terminalFactory = new QaHostFactory();
            terminalFactory.PrepareStartupOptions(new QaHostPreparationContext(terminalRuntime, terminalRunId, terminalRuntime.Paths.DtmApiPath, terminalBytes));
            var terminalParticipant = (QaHostParticipant)terminalFactory.CreateParticipant(
                new GameBridgeFixtureAccess(terminalRuntime, terminalRunId, terminalRuntime.Paths.DtmApiPath, () => true, terminalBridge));
            terminalParticipant.Start();
            Assert(terminalParticipant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue,
                "A continuous HomePage-only participant must start post-title observation without SaveLoaded.");
            terminalParticipant.Update();
            Thread.Sleep(300);
            terminalParticipant.Update();
            Assert(terminalParticipant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Close,
                "A continuous HomePage-only participant must reach its terminal close without requiring SaveLoaded.");
            terminalParticipant.Close("unit-title-only-terminal");
        }

        private static void G4ReturnedToTitleCleansAndBlocksInSaveCasesBeforePostTitleObservation()
        {
            const string runId = "88888888888888888888888888888888";
            string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            runtime.UI.SetUiContext("HomePageUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "unit-post-title");
            var bridge = new DolocTownGameBridge(runtime);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                HatchVoiceEnabled = true,
                ContinuousHomePageTerminalEnabled = true,
                ContinuousHomePageSeconds = 0.25d
            }));
            var factory = new QaHostFactory();
            factory.PrepareStartupOptions(new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes));
            var participant = (QaHostParticipant)factory.CreateParticipant(
                new GameBridgeFixtureAccess(runtime, runId, runtime.Paths.DtmApiPath, () => true, bridge));
            participant.Start();
            Assert(participant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue,
                "The startup pre-save ReturnedToTitle boundary must not be classified as an interrupted in-save G4 run.");
            participant.Update();
            Assert(runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.HatchAnimalVoice").Status == "pending",
                "A pre-SaveLoaded title boundary must not clean, block, or fail the future in-save G4 case.");
            participant.OnSaveLoaded(2, false);

            Assert(participant.OnReturnedToTitle(true) == QaHostBoundaryDisposition.Continue,
                "An interrupted in-save G4 case must clean its receipts and leave only the independent post-title HomePage observer running.");
            var hatchFailure = runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.HatchAnimalVoice");
            Assert(hatchFailure.Status == "failed" && hatchFailure.Details.Contains("cleanup=verified", StringComparison.Ordinal) && hatchFailure.Details.Contains("postTitleOnly=true", StringComparison.Ordinal),
                "ReturnedToTitle must publish a fail-closed Hatch terminal only after verified G4 receipt cleanup.");
            participant.OnSaveLoaded(2, false);
            Assert(runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.HatchAnimalVoice").Status == "failed",
                "A late queued SaveLoaded after ReturnedToTitle must not resume or overwrite an interrupted in-save G4 case.");
            participant.Update();
            Thread.Sleep(300);
            AssertThrows<InvalidOperationException>(() => participant.Update(),
                "After the independent continuous HomePage receipt completes, the interrupted in-save G4 case must fail closed.");
            Assert(runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.QaContinuousHomePage").Status == "verified" &&
                runtime.Diagnostics.GetHookStatuses().Last(item => item.HookId == "Smoke.HatchAnimalVoice").Status == "failed",
                "Post-title observation may complete, but no later Update may rerun or overwrite the interrupted in-save G4 failure.");
            participant.Close("unit-post-title-fail-closed");
        }

        private static void G4OverlaySessionsAreDistinctReceipts()
        {
            var ui = new DTMAPI.Core.Services.UiRuntimeService(() => string.Empty, _ => { }, _ => { });
            PropertyInfo sequence = typeof(DTMAPI.Core.Services.UiRuntimeService).GetProperty("OverlaySessionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("UiRuntimeService overlay session sequence is unavailable.");
            ui.OpenConfigPage();
            long configSession = (long)(sequence.GetValue(ui) ?? 0L);
            ui.OpenModListPage();
            long modSession = (long)(sequence.GetValue(ui) ?? 0L);
            Assert(configSession > 0 && modSession == configSession + 1 && ui.ActiveMenuId == "DTMAPI.Mods",
                "Every overlay open/page replacement must produce a distinct session identity so G4 cannot close a later unrelated overlay.");
        }

        private static void G4ManagerTitleRoutesAreMutuallyExclusiveAndQaOwned()
        {
            const string runId = "77777777777777777777777777777777";

            GameBridgeFixtureStartupOptions Prepare(bool basic, bool status, bool mvp)
            {
                string gameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(gameDir);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    schemaVersion = QaHostProtocol.SchemaVersion,
                    protocolVersion = QaHostProtocol.ProtocolVersion,
                    runId,
                    mode = QaHostProtocol.ParticipantOnlyMode,
                    TitleSettingsUiEnabled = basic,
                    ManagerStatusUiEnabled = status,
                    ManagerMvpUiEnabled = mvp
                }));
                var factory = new QaHostFactory();
                return factory.PrepareStartupOptions(new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, bytes));
            }

            GameBridgeFixtureStartupOptions basicRoute = Prepare(true, false, false);
            GameBridgeFixtureStartupOptions statusRoute = Prepare(false, true, false);
            GameBridgeFixtureStartupOptions mvpRoute = Prepare(false, false, true);
            Assert(basicRoute.RunId == runId && statusRoute.RunId == runId && mvpRoute.RunId == runId,
                "Every G4 title route must project only the validated startup identity; scenario selection stays inside the optional participant settings.");
            Assert(typeof(GameBridgeFixtureStartupOptions).GetProperty("DisableEmbeddedTitleSettingsUi") == null &&
                typeof(GameBridgeFixtureStartupOptions).GetProperty("DisableEmbeddedManagerStatusUi") == null &&
                typeof(GameBridgeFixtureStartupOptions).GetProperty("DisableEmbeddedManagerMvpUi") == null,
                "G7 must remove all per-route embedded-disable switches from the production startup contract.");

            foreach (var invalid in new[]
            {
                new { Basic = true, Status = true, Mvp = false },
                new { Basic = true, Status = false, Mvp = true },
                new { Basic = false, Status = true, Mvp = true }
            })
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    schemaVersion = QaHostProtocol.SchemaVersion,
                    protocolVersion = QaHostProtocol.ProtocolVersion,
                    runId,
                    mode = QaHostProtocol.ParticipantOnlyMode,
                    TitleSettingsUiEnabled = invalid.Basic,
                    ManagerStatusUiEnabled = invalid.Status,
                    ManagerMvpUiEnabled = invalid.Mvp
                }));
                QaHostSettings settings = QaHostSettings.Read(bytes);
                AssertThrows<InvalidDataException>(() => settings.Validate(runId),
                    "Basic, Manager Status, and Manager MVP title routes must be mutually exclusive.");
            }

            string titleGameDir = Path.Combine(Path.GetTempPath(), "DTMAPI-QA-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(titleGameDir);
            var titleRuntime = new DtmApiRuntime(new FakeHost(titleGameDir), new ConfigMenuRegistry());
            var titleBridge = new DolocTownGameBridge(titleRuntime);
            byte[] titleBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                ManagerStatusUiEnabled = true
            }));
            var titleFactory = new QaHostFactory();
            titleFactory.PrepareStartupOptions(new QaHostPreparationContext(titleRuntime, runId, titleRuntime.Paths.DtmApiPath, titleBytes));
            var participant = (QaHostParticipant)titleFactory.CreateParticipant(
                new GameBridgeFixtureAccess(titleRuntime, runId, titleRuntime.Paths.DtmApiPath, () => true, titleBridge));
            participant.Start();
            Assert(participant.OnReturnedToTitle(false) == QaHostBoundaryDisposition.Continue &&
                (bool)(typeof(QaHostParticipant).GetField("postTitleBoundaryObserved", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(participant) ?? false),
                "A Manager title-only route must enter its title generation without observing SaveLoaded.");

            string root = FindRepositoryRoot();
            string managerFixture = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G4ManagerFixtures.cs"));
            string access = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "GameBridgeFixtureAccess.cs"));
            string controllerSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "QaScenarioController.cs"));
            string participantSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string statusSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "GameBridgeFixtureStatusExtensions.cs"));
            string titleUi = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.BepInExBootstrap", "ReflectedTitleMenuSettingsUi.cs"));
            string saveRequiredSlice = SliceBetween(participantSource, "private bool RequiresSaveLoadedForG4()", "private bool RequiresSaveLoadedBoundary()");

            Assert(controllerSource.Contains("GetEvidencePath(\"g4/ui\", \"manager-status-page.png\")", StringComparison.Ordinal) &&
                controllerSource.Contains("GetEvidencePath(\"g4/ui\", \"manager-logs-page.png\")", StringComparison.Ordinal) &&
                !access.Contains("manager-status-page.png", StringComparison.Ordinal) &&
                !access.Contains("manager-logs-page.png", StringComparison.Ordinal),
                "Manager QA screenshots must use the fixed qa-host/g4/ui evidence names.");
            Assert(managerFixture.Contains("runtime.UI.RefreshDtmManagerModel();", StringComparison.Ordinal) &&
                managerFixture.Contains("if (modelRows <= 0)", StringComparison.Ordinal) &&
                managerFixture.Contains("ManagerPageRowFormatter.FormatStatusSummary", StringComparison.Ordinal) &&
                managerFixture.Contains("runtime.UI.CopyManagerSummary(text =>", StringComparison.Ordinal) &&
                managerFixture.Contains("clipboard=os-not-used", StringComparison.Ordinal),
                "Manager Status QA must explicitly refresh a nonempty model, format its summary, and exercise CopyManagerSummary through an in-memory callback.");
            Assert(managerFixture.Contains("IsTitleOverlayReceiptCurrent()", StringComparison.Ordinal) &&
                managerFixture.Contains("CaptureTitleOverlayReceipt();", StringComparison.Ordinal) &&
                managerFixture.Contains("CloseTitleOverlayOwnedByFixture();", StringComparison.Ordinal) &&
                managerFixture.Contains("cleanup remains retryable", StringComparison.Ordinal),
                "Manager UI stages must retain exact overlay receipts and leave failed closure retryable.");

            int mods = managerFixture.IndexOf("DtmOverlayPage.Mods", StringComparison.Ordinal);
            int errors = managerFixture.IndexOf("DtmOverlayPage.Errors", mods + 1, StringComparison.Ordinal);
            int hooks = managerFixture.IndexOf("DtmOverlayPage.Hooks", errors + 1, StringComparison.Ordinal);
            int features = managerFixture.IndexOf("DtmOverlayPage.Features", hooks + 1, StringComparison.Ordinal);
            int logs = managerFixture.IndexOf("DtmOverlayPage.Logs", features + 1, StringComparison.Ordinal);
            Assert(mods >= 0 && errors > mods && hooks > errors && features > hooks && logs > features,
                "Manager MVP must visit Mods, Errors, Hooks, Features, and Logs in deterministic order.");
            Assert(managerFixture.Contains("ManagerLogsPageState.From", StringComparison.Ordinal) &&
                managerFixture.Contains("state.PathMatchStatus.Equals(\"matched\"", StringComparison.Ordinal) &&
                managerFixture.Contains("state.SnapshotReportPathMatched", StringComparison.Ordinal) &&
                managerFixture.Contains("File.Exists(exportedPath)", StringComparison.Ordinal) &&
                managerFixture.Contains("exportedPath.Equals(state.SnapshotLatestReportPath", StringComparison.Ordinal),
                "Manager MVP export must verify exported status, snapshot path match, and the concrete report file.");
            Assert(titleUi.Contains("renderedOverlaySessionSequence != runtime.UI.OverlaySessionSequence", StringComparison.Ordinal) &&
                titleUi.Contains("renderedOverlaySessionSequence = runtime.UI.OverlaySessionSequence;", StringComparison.Ordinal),
                "A same-page Manager Logs export must invalidate the rendered overlay so the player-visible status/path-match text is redrawn before evidence capture.");
            foreach (string statusId in new[]
            {
                "Smoke.ManagerStatusPage", "Smoke.ManagerStatusSummaryText", "Smoke.ManagerStatusSummaryCopy",
                "Smoke.ManagerStatusPageScreenshot", "Smoke.ManagerModsPage", "Smoke.ManagerErrorsPage",
                "Smoke.ManagerHooksPage", "Smoke.ManagerFeaturesPage", "Smoke.ManagerLogsPage",
                "Smoke.ManagerLogsExportButton", "Smoke.ManagerLogsExportStateText", "Smoke.ManagerLogsPageScreenshot"
            })
            {
                Assert(statusSource.Contains("PublishG4Status", StringComparison.Ordinal) &&
                    managerFixture.Contains(statusId, StringComparison.Ordinal) &&
                    !access.Contains(statusId, StringComparison.Ordinal),
                    "The optional QA status policy and Manager state machine must retain runner-visible status " + statusId + ".");
            }
            Assert(managerFixture.Contains("; owner=qa; fallback=false", StringComparison.Ordinal),
                "Every Manager substatus must retain explicit QA ownership and no-fallback details.");
            Assert(!saveRequiredSlice.Contains("ManagerStatus", StringComparison.Ordinal) &&
                !saveRequiredSlice.Contains("ManagerMvp", StringComparison.Ordinal),
                "Manager Status and MVP are title-only routes and must not acquire a SaveLoaded boundary.");
            foreach (string forbidden in new[]
            {
                "OpenConfigPage", "System.Windows.Forms", "GUIUtility.systemCopyBuffer", "Clipboard.SetText",
                "DEBUG_Set", "GiveItem", "CreateItem", "SpawnAnimal"
            })
            {
                Assert(!managerFixture.Contains(forbidden, StringComparison.Ordinal),
                    "The Manager QA state machine must not use config, OS clipboard, inventory, or animal mutation token " + forbidden + ".");
            }
        }

        private static void G4SourceAndProjectBoundariesAreClosed()
        {
            string root = FindRepositoryRoot();
            string fixture = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G4Fixtures.cs"));
            string cameraScenario = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "G4CameraPlayableFixtureScenario.cs"));
            string fixtureSessions = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "G4FixtureSessions.cs"));
            string access = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "GameBridgeFixtureAccess.cs"));
            string controllerSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "QaScenarioController.cs"));
            string hostBoundary = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "DolocTownGameBridge.QaHost.cs"));
            string participant = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string qaSettings = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostSettings.cs"));
            string runnerScript = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-game-smoke.ps1"));

            string retiredSmokeDirectory = Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Smoke");
            Assert((!Directory.Exists(retiredSmokeDirectory) || !Directory.EnumerateFiles(retiredSmokeDirectory, "*.cs", SearchOption.AllDirectories).Any()) &&
                participant.Contains("settings.ContentMetadataObservationEnabled", StringComparison.Ordinal) &&
                participant.Contains("scenarios.ObserveContentMetadata", StringComparison.Ordinal) &&
                !access.Contains("ObserveContentMetadata", StringComparison.Ordinal),
                "G7 must delete the embedded Smoke directory while retaining content-metadata observation only in the optional participant.");
            Assert(controllerSource.Contains("private string GetEvidenceDirectory(string relativePath)", StringComparison.Ordinal) &&
                controllerSource.Contains("private string GetEvidencePath(string relativeDirectory, string fileName)", StringComparison.Ordinal) &&
                controllerSource.Contains("private void EditAndSaveConfigPage(string uniqueId, Action<IConfigMenuPage> edit)", StringComparison.Ordinal) &&
                controllerSource.Contains("QA evidence directory escaped the authorized run root", StringComparison.Ordinal) &&
                controllerSource.Contains("DTMAPI.Abstractions.IConfigMenuRuntime", StringComparison.Ordinal) &&
                controllerSource.Contains("runtimeContract?.GetMethod(\"Cancel\")", StringComparison.Ordinal) &&
                controllerSource.Contains("editingBegan && !committed", StringComparison.Ordinal) &&
                !access.Contains("GetEvidenceDirectory(", StringComparison.Ordinal) &&
                !access.Contains("GetEvidencePath(", StringComparison.Ordinal) &&
                !access.Contains("EditAndSaveConfigPage(", StringComparison.Ordinal),
                "G9 must keep evidence path creation and config mutation inside the optional QA controller instead of the production fixture facade.");

            string debugConsoleObservation = SliceBetween(fixture, "internal G4FixtureStepResult ObserveDebugConsoleUiForFixture", "internal G4FixtureStepResult ObservePendingSaveSlotsPagingForFixture");
            Assert(debugConsoleObservation.Contains("runtime.UI.IsOwnerBoundCustomMenuOpen(", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("\"DTMAPI.DebugConsoleMod\"", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("if (!productConsoleOpen && g4DebugScreenshotPath == null)", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("if (g4DebugEvidenceCaptured)", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("EVIDENCE_CAPTURED_WAITING_ESCAPE", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("closeReceipt=product-owner-modal-closed", StringComparison.Ordinal) &&
                debugConsoleObservation.IndexOf("EVIDENCE_CAPTURED_WAITING_ESCAPE", StringComparison.Ordinal) < debugConsoleObservation.LastIndexOf("G4FixtureStepResult.Verified", StringComparison.Ordinal) &&
                debugConsoleObservation.Contains("ownerUnchanged=true", StringComparison.Ordinal) &&
                !debugConsoleObservation.Contains("debugConsoleApi", StringComparison.Ordinal) &&
                !debugConsoleObservation.Contains(".Open(", StringComparison.Ordinal) &&
                !debugConsoleObservation.Contains(".Close(", StringComparison.Ordinal),
                "DebugConsole G4 must capture only the exact product-owned modal surface, wait for runner Escape and owner-bound close readback, and never consult the frozen Compatibility API or forge, open, close, or claim that owner from QA.");

            Assert(qaSettings.Contains("ContinuousHomePageTerminalEnabled || ExternalPlayerInputObservationEnabled", StringComparison.Ordinal) &&
                qaSettings.Contains("ContinuousHomePageSeconds < 0.25d || ContinuousHomePageSeconds > 30d", StringComparison.Ordinal),
                "External player-input post-title observation must enforce the same bounded continuous HomePage duration as the explicit terminal mode.");

            const string equipmentIsolationRunId = "91919191919191919191919191919191";
            QaHostSettings equipmentIsolationConflict = QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId = equipmentIsolationRunId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 3,
                EquipmentSlotsObservationEnabled = true,
                G6RootIsolationProfile = "UiRuntime"
            })));
            AssertThrows<InvalidDataException>(() => equipmentIsolationConflict.Validate(equipmentIsolationRunId),
                "EquipmentSlots G4 must fail closed when UiRuntime isolation disables its production UI owner.");

            Assert(qaSettings.Contains("ExternalPlayerInputEvidenceRoot", StringComparison.Ordinal) &&
                participant.Contains("settings.ExternalPlayerInputEvidenceRoot, settings.ExternalPlayerInputMarkerPath", StringComparison.Ordinal) &&
                participant.Contains("Path.GetDirectoryName(marker)", StringComparison.Ordinal) &&
                participant.Contains("external-player-input-complete.signal", StringComparison.Ordinal) &&
                runnerScript.Contains("ExternalPlayerInputEvidenceRoot = $ExternalPlayerInputEvidenceRoot", StringComparison.Ordinal),
                "The external marker must be the fixed direct child of the explicitly authorized runner evidence root, independent of the game-side QA screenshot root.");

            Assert(participant.Contains("if (!externalPlayerInput.MarkerPassed)", StringComparison.Ordinal) &&
                participant.Contains("throw new InvalidOperationException(failed);", StringComparison.Ordinal) &&
                participant.Contains("ExternalInputMarkerPassed", StringComparison.Ordinal) &&
                participant.Contains("externalPlayerInput.MarkerPassed)", StringComparison.Ordinal),
                "A terminal external-input marker with Passed=False must fail the participant, block normal close, and never emit a cleanup OK receipt.");

            Assert(participant.Contains("private bool failureCloseRequested;", StringComparison.Ordinal) &&
                participant.Contains("if (closeReason.StartsWith(\"failure:\", StringComparison.Ordinal))", StringComparison.Ordinal) &&
                participant.Contains("bool failureClose = failureCloseRequested;", StringComparison.Ordinal),
                "Once a failure close begins, later shutdown/cleanup retries must retain failure semantics instead of reclassifying intentionally incomplete G4 terminals.");

            Assert(runnerScript.Contains("$externalPlayerInputCompletionTempPath = $externalPlayerInputCompletionMarkerPath + '.tmp.'", StringComparison.Ordinal) &&
                runnerScript.Contains("Move-Item -LiteralPath $externalPlayerInputCompletionTempPath -Destination $externalPlayerInputCompletionMarkerPath -ErrorAction Stop", StringComparison.Ordinal) &&
                !runnerScript.Contains(") | Set-Content -LiteralPath $externalPlayerInputCompletionMarkerPath", StringComparison.Ordinal),
                "The external-input terminal marker must become visible only after a complete same-directory temporary write is atomically renamed.");
            Assert(runnerScript.Contains("$strictPlayerInputGate = [bool]$RequireExternalPlayerInputGate -or [bool]$AssertNoQaUiEvidence -or [bool]$qaG4DebugConsoleEnabled", StringComparison.Ordinal) &&
                runnerScript.Contains("[bool]$qaG4DebugConsoleEnabled -or [bool]$qaG4EquipmentSlotsObservationEnabled", StringComparison.Ordinal) &&
                runnerScript.Contains("$externalPlayerInputAttempts.Add", StringComparison.Ordinal) &&
                runnerScript.Contains("Wait-ForLogLineAfterOffsetUntilDeadline -LogPath $logPath -Offset ([int64]$y1Attempt[0].LogOffset)", StringComparison.Ordinal) &&
                runnerScript.Contains("-Deadline (Get-SmokeCappedDeadline -Deadline $debugConsoleActionDeadline -MaximumMilliseconds 5000)", StringComparison.Ordinal) &&
                runnerScript.Contains("-Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE'", StringComparison.Ordinal) &&
                runnerScript.IndexOf("-Pattern 'EVIDENCE_CAPTURED_WAITING_ESCAPE'", StringComparison.Ordinal) < runnerScript.IndexOf("-Label 'YHoldCleanupEscape'", StringComparison.Ordinal),
                "The staged DebugConsole G4 lane must use the same one-attempt foreground SendInput provenance and causal-offset receipt as the no-QA player lane, forbid PostMessage fallback, and close only after QA evidence is ready.");
            Assert(
                runnerScript.IndexOf("if ($saveLoadedOk -and $requiresDebugConsoleKeySmoke)", StringComparison.Ordinal) <
                runnerScript.IndexOf("if ($saveLoadedOk -and $AutoExerciseTitleButtonLifecycle)", StringComparison.Ordinal) &&
                runnerScript.IndexOf("if ($saveLoadedOk -and $requiresDebugConsoleKeySmoke)", StringComparison.Ordinal) <
                runnerScript.IndexOf("if ($saveLoadedOk -and $AssertAdvancedProductOwnerDeactivation)", StringComparison.Ordinal),
                "DebugConsole real-input exercise must run before title lifecycle and Advanced owner-deactivation waits so the QA participant can trigger those terminals.");

            Assert(participant.Contains("scenarios.AdvanceSaveSlotScenario(settings.SaveSlot)", StringComparison.Ordinal) &&
                fixture.Contains("g4SaveIndex = saveSlot - 1;", StringComparison.Ordinal),
                "The restricted save coordinator must project settings.SaveSlot and translate it exactly once to the game's zero-based slot index.");
            Assert(fixture.Contains("if (g4SaveCoordinatorStage == 0)", StringComparison.Ordinal) &&
                fixture.Contains("g4SaveCoordinatorStage = 1;", StringComparison.Ordinal) &&
                fixture.Contains("Official save UI opened; waiting for paging and screenshot receipt.", StringComparison.Ordinal) &&
                fixture.Contains("if (g4SaveCoordinatorStage == 1)", StringComparison.Ordinal) &&
                CountTextOccurrences(fixture, "continueMethod.Invoke(g4SaveUiState, null);") == 1,
                "Opening the official save UI must advance to a separate paging/screenshot stage, and native continuation must be issued at most once.");
            Assert(fixture.Contains("!g4DirectFallbackAttempted && (DateTimeOffset.UtcNow - g4SaveRequestAt).TotalSeconds >= 15d", StringComparison.Ordinal) &&
                fixture.Contains("g4DirectFallbackAttempted = true;", StringComparison.Ordinal) &&
                fixture.Contains("g4SaveCoordinatorStage = 3;", StringComparison.Ordinal) &&
                CountTextOccurrences(fixture, "method.Invoke(null, new object[] { g4SaveIndex });") == 1 &&
                fixture.Contains("out bool issued, out string details", StringComparison.Ordinal) &&
                fixture.Contains("runtime.RefactorOptions.SaveLoadRequestCoordinator", StringComparison.Ordinal) &&
                fixture.Contains("SaveLoadRequestSnapshot snapshot = runtime.SaveLoadRequestSnapshot;", StringComparison.Ordinal) &&
                fixture.Contains("nativeUiTransitionObserved=true; directBlocked=true", StringComparison.Ordinal) &&
                fixture.Contains("ReferenceEquals(GetCurrentNativeUiStateForFixture(), g4SaveUiState)", StringComparison.Ordinal) &&
                fixture.Contains("g4SaveCoordinatorStage >= 2", StringComparison.Ordinal) &&
                fixture.Contains("including after the single delayed fallback attempt", StringComparison.Ordinal),
                "The direct save fallback must be delayed 15 seconds, require the production coordinator, block on native entry/transition receipts, remain a single guarded attempt, and retain the 120-second terminal timeout after stage 3.");
            Assert(fixture.Contains("RestoreG4ModChangeDelegateReceipt()", StringComparison.Ordinal) &&
                fixture.Contains("if (!ReferenceEquals(current, g4ModChangeWrappedAction))", StringComparison.Ordinal) &&
                fixture.Contains("g4ModChangeReceiptField.SetValue(g4ModChangeReceiptState, g4ModChangeOriginalAction);", StringComparison.Ordinal) &&
                fixture.Contains("VerifyRequestedSaveSelectionForFixture(g4SaveUiState, g4SaveIndex", StringComparison.Ordinal) &&
                fixture.Contains("stateIndex == index && panelIndex == index && selectedSlot != null", StringComparison.Ordinal) &&
                fixture.Contains("g4SaveSelectionVerifiedBeforeContinuation = true;", StringComparison.Ordinal) &&
                fixture.Contains("stateStillCurrent", StringComparison.Ordinal) &&
                !fixture.Contains("g4RequestedSelectionRestored", StringComparison.Ordinal) &&
                fixture.Contains("CloseNativeUiOwnedByFixture(ref g4SaveUiState)", StringComparison.Ordinal) &&
                fixture.Contains("if (!ReferenceEquals(getCurrentState(), ownedState))", StringComparison.Ordinal),
                "Save selection and cleanup must read back state.currentIndex, panel.selectedIndex, and the selected slot instead of trusting a sticky call bit, restore the wrapped delegate, then close only the exact QA-owned GameDataUiState.");
            string modChangeContinuation = SliceBetween(fixture, "private bool ContinueModChangeForFixture", "private bool RequestSingleDirectSaveFallbackForFixture");
            Assert(modChangeContinuation.Contains("if (!g4SaveSelectionVerifiedBeforeContinuation || g4SaveIndex < 0)", StringComparison.Ordinal) &&
                modChangeContinuation.Contains("Action wrapped = new Action", StringComparison.Ordinal) &&
                modChangeContinuation.Contains("ReferenceEquals(continuationField.GetValue(state), wrapped)", StringComparison.Ordinal) &&
                modChangeContinuation.Contains("RestoreG4ModChangeDelegateReceipt();", StringComparison.Ordinal) &&
                !modChangeContinuation.Contains("SelectRequestedSaveForFixture(g4SaveUiState", StringComparison.Ordinal) &&
                !modChangeContinuation.Contains("return VerifyRequestedSaveSelectionForFixture(g4SaveUiState, g4SaveIndex, out _) &&", StringComparison.Ordinal),
                "The optional mod-change path must consume the pre-GameData selection receipt, verify the exact wrapper, tolerate the native delayed-frame callback, and never revisit the destroyed GameData panel.");
            object exactSaveState = new object();
            Assert(QaScenarioController.HasExactNativeUiReceiptForFixture(exactSaveState, exactSaveState) &&
                !QaScenarioController.HasExactNativeUiReceiptForFixture(new object(), exactSaveState) &&
                !QaScenarioController.HasExactNativeUiReceiptForFixture(exactSaveState, null),
                "The SaveSlots current-state gate must accept only reference identity with a non-null QA receipt.");
            Assert(CountTextOccurrences(fixture, "HasExactNativeUiReceiptForFixture(GetCurrentNativeUiStateForFixture(), g4SaveUiState)") >= 2 &&
                fixture.Contains("FailSaveSlotCoordinatorOwnershipLoss(\"before paging/select\")", StringComparison.Ordinal) &&
                fixture.Contains("FailSaveSlotCoordinatorOwnershipLoss(\"before native OnConfirm continuation\")", StringComparison.Ordinal) &&
                fixture.Contains("staleReceiptCleared=", StringComparison.Ordinal) &&
                fixture.Contains("unrelatedCurrentUiUntouched=true; continuationIssued=false", StringComparison.Ordinal),
                "SaveSlots must fail closed and clear only its stale receipt when exact current-state ownership is lost before paging or native continuation.");

            Assert(participant.Contains("GetIncompleteInSaveG4Requirements()", StringComparison.Ordinal) &&
                participant.IndexOf("if (!hasObservedSaveLoaded && RequiresSaveLoadedBoundary())", StringComparison.Ordinal) < participant.IndexOf("postTitleBoundaryObserved = true;", StringComparison.Ordinal) &&
                participant.Contains("RequiresSaveLoadedForG4() ||", StringComparison.Ordinal) &&
                participant.Contains("string cleanup = scenarios.CloseG4Fixtures();", StringComparison.Ordinal) &&
                participant.Contains("inSaveG4ExecutionBlocked = true;", StringComparison.Ordinal) &&
                participant.Contains("saveLoadedObserved && !postTitleBoundaryObserved", StringComparison.Ordinal) &&
                participant.Contains("ignored late SaveLoaded after the terminal title boundary", StringComparison.Ordinal) &&
                participant.Contains("homePageTerminals.Count > 0 && !continuousHomePageCompleted", StringComparison.Ordinal),
                "ReturnedToTitle must clean receipts before classifying incomplete in-save G4 cases, permanently block their Update path, fail closed, and preserve only the independent post-title HomePage observer.");
            Assert(CountTextOccurrences(hostBoundary, "participant.OnReturnedToTitle(") == 1 &&
                hostBoundary.Contains("RequestApplicationQuitFromQaHost(\"optional QA participant completed at the title boundary\")", StringComparison.Ordinal),
                "The title boundary must be delivered exactly once; a terminal result must close through the unified application-quit path and must never be replayed from Update before disposition handling.");
            Assert(fixture.Contains("OverlaySessionSequence", StringComparison.Ordinal) &&
                fixture.Contains("IsTitleOverlayReceiptCurrent()", StringComparison.Ordinal) &&
                fixture.Contains("IsExpectedTitleSettingsOverlay()", StringComparison.Ordinal) &&
                fixture.Contains("TryValidateNativeUiLayoutObservation", StringComparison.Ordinal) &&
                fixture.Contains("observation.ConstraintCount == 1", StringComparison.Ordinal) &&
                fixture.Contains("observation.ConstraintCount == observation.VisibleSlots", StringComparison.Ordinal),
                "Title cleanup must use an exact overlay session identity and all Native UI verification must require concrete state, target, constraints, and installed production repair hooks.");
            Assert(fixture.Contains("remove.MakeGenericMethod(ownedStateType).Invoke(manager, null);", StringComparison.Ordinal) &&
                fixture.Contains("TryInvokeExactPopStateForFixture(userInput, state)", StringComparison.Ordinal) &&
                fixture.Contains("if (!ReferenceEquals(getCurrentState(), ownedState))", StringComparison.Ordinal) &&
                fixture.Contains("if (!tryPopState(ownedState))", StringComparison.Ordinal) &&
                fixture.Contains("if (ReferenceEquals(getCurrentState(), ownedState))", StringComparison.Ordinal) &&
                fixture.Contains("return tryRemoveCache(ownedState.GetType());", StringComparison.Ordinal) &&
                fixture.Contains("MarkNativeUiStateExitedForFixture(receipt);", StringComparison.Ordinal),
                "Native UI cleanup must pop only the exact current receipt, verify the transition, then remove the exact generic RemoveUI<T>() cache entry with a retryable two-phase receipt.");

            foreach (string forbidden in new[]
            {
                "EnterUI", "OnConfirm", "LoadGame", "GenerateItem", "GiveItem", "CreateItem",
                "Spawn", "Teleport", "SetWeather", "DEBUG_Set"
            })
            {
                Assert(!access.Contains(forbidden, StringComparison.Ordinal),
                    "The narrow GameBridgeFixtureAccess facade must not expose raw native or world-mutation verb " + forbidden + ".");
            }
            foreach (string forbidden in new[] { "ForSmoke", "SmokeAttemptResult", "DEBUG_SetHusbandryValue" })
                Assert(!fixture.Contains(forbidden, StringComparison.Ordinal), "The G4 fixture implementation must not depend on retired embedded-Smoke or husbandry mutation token " + forbidden + ".");
            string animalObservation = SliceBetween(fixture, "internal G4FixtureStepResult ObserveAnimalsForFixture", "internal G4FixtureStepResult ObserveEquipmentSlotsUiForFixture");
            Assert(animalObservation.Contains("if (g4AnimalProgressRowCount == 0)", StringComparison.Ordinal) &&
                animalObservation.Contains("ReadAnimalProductObservationSummary()", StringComparison.Ordinal) &&
                animalObservation.Contains("state=visible", StringComparison.Ordinal) &&
                animalObservation.Contains("overlayRows=", StringComparison.Ordinal) &&
                animalObservation.IndexOf("state=visible", StringComparison.Ordinal) < animalObservation.IndexOf("ObserveScreenshotForFixture", StringComparison.Ordinal) &&
                animalObservation.Contains("; mutation=false; close=owned-only", StringComparison.Ordinal),
                "G9 staged AnimalViewer must require a configured product progress row and its product-owned visible receipt before QA-owned screenshot evidence; a native panel alone must not pass.");
            string audioObservation = SliceBetween(fixture, "internal G4FixtureStepResult ObserveAudioDefinitionsForFixture()", "internal G4FixtureStepResult ExerciseHatchVoiceForFixture()");
            Assert(!audioObservation.Contains("Reload", StringComparison.Ordinal) &&
                !audioObservation.Contains("Refresh", StringComparison.Ordinal),
                "The G4 audio-definition case must remain read-only and must not refresh or reload definitions.");
            Assert(cameraScenario.Contains("double progress = Math.Min(1d, elapsed / 3d);", StringComparison.Ordinal) &&
                cameraScenario.Contains("double y = originalAgent.Y - 2d * Math.Sin(progress * Math.PI);", StringComparison.Ordinal) &&
                cameraScenario.Contains("item.AgentY > originalAgent!.Y + 0.05d || item.CameraY > originalAgent.Y + 0.05d", StringComparison.Ordinal) &&
                cameraScenario.Contains("path=rightward-ground-safe; maxDownwardExcursion=0", StringComparison.Ordinal) &&
                cameraScenario.Contains("sample.AgentX", StringComparison.Ordinal) &&
                cameraScenario.Contains("sample.CameraX", StringComparison.Ordinal) &&
                cameraScenario.Contains("ValidateMovement();", StringComparison.Ordinal),
                "CameraPlayable must use a bounded three-second, rightward ground-safe movement and verify both AgentPosition and native-camera telemetry readback.");
            foreach (string receipt in new[]
            {
                "before.png", "scale-4x.png", "movement-start.png", "movement-mid.png",
                "movement-end.png", "fallback-2x.png", "reset.png", "telemetry.csv"
            })
                Assert(cameraScenario.Contains(receipt, StringComparison.Ordinal), "CameraPlayable evidence is missing required receipt " + receipt + ".");
            Assert(cameraScenario.Contains("if (roots != 0) failures.Add(\"root=\" + roots);", StringComparison.Ordinal) &&
                cameraScenario.Contains("if (!ownerRestored) failures.Add(\"owner-restore\");", StringComparison.Ordinal) &&
                cameraScenario.Contains("if (!vanillaRestored) failures.Add(\"vanilla-restore\");", StringComparison.Ordinal) &&
                cameraScenario.Contains("if (!agentRestored) failures.Add(\"agent-restore\");", StringComparison.Ordinal) &&
                cameraScenario.Contains("if (!cameraRestored) failures.Add(\"camera-restore\");", StringComparison.Ordinal) &&
                cameraScenario.IndexOf("terminal = true;", StringComparison.Ordinal) > cameraScenario.IndexOf("if (failures.Count > 0)", StringComparison.Ordinal),
                "CameraPlayable Close must not become terminal before independent AgentPosition/camera, QA root=0, prior owner, and vanilla-scale restoration are all verified.");
            var agentReceipt = new AgentPositionFixtureReceipt(true, 1d, 2d, 3d);
            var cameraReceipt = new CameraPositionFixtureReceipt(true, 11d, 12d, 0d);
            Assert(agentReceipt.Captured && cameraReceipt.Captured &&
                agentReceipt.X != cameraReceipt.X && agentReceipt.Y != cameraReceipt.Y,
                "CameraPlayable must preserve distinct agent and camera coordinate receipts instead of assuming the camera starts at the agent position.");
            Assert(fixtureSessions.Contains("internal sealed class CameraPositionFixtureReceipt", StringComparison.Ordinal) &&
                cameraScenario.Contains("captureCameraPosition", StringComparison.Ordinal) &&
                cameraScenario.Contains("restoreCameraPosition", StringComparison.Ordinal) &&
                fixture.Contains("CaptureCameraPositionForFixture", StringComparison.Ordinal) &&
                fixture.Contains("RestoreCameraPositionForFixture", StringComparison.Ordinal) &&
                fixture.Contains("TryWriteAgentPositionForFixture", StringComparison.Ordinal) &&
                fixture.Contains("TryWriteCameraPositionForFixture", StringComparison.Ordinal) &&
                fixture.Contains("item.Name.Equals(\"ForceSetPosition\", StringComparison.Ordinal)", StringComparison.Ordinal) &&
                fixture.Contains("forceSetPosition.Invoke(controller, new[] { cameraTarget, (object)false });", StringComparison.Ordinal) &&
                cameraScenario.Contains("TotalSeconds < 2d", StringComparison.Ordinal),
                "CameraPlayable must capture, restore, and read back agent and CameraController positions through independent receipts and writers.");

            string bridge = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.cs"));
            string ensureFeatures = SliceBetween(bridge, "private void EnsureGameBridgeFeatures()", "private sealed class PendingWorkshopUploadPlanResolution");
            Assert(ensureFeatures.Contains("nativeUiLayoutDiagnosticsFeature ??= new NativeUiLayoutDiagnosticsFeature(runtime);", StringComparison.Ordinal) &&
                !bridge.Contains("UiRuntime", StringComparison.Ordinal) &&
                !bridge.Contains("SmokeRootIsolation", StringComparison.Ordinal),
                "Production Native UI repair must always be registered and UiRuntime isolation must never disable it.");

            foreach (string project in new[]
            {
                Path.Combine("src", "DTMAPI.Abstractions", "DTMAPI.Abstractions.csproj"),
                Path.Combine("src", "DTMAPI.Core", "DTMAPI.Core.csproj"),
                Path.Combine("src", "DTMAPI.ModConfigMenu", "DTMAPI.ModConfigMenu.csproj"),
                Path.Combine("src", "DTMAPI.GameBridge.DolocTown", "DTMAPI.GameBridge.DolocTown.csproj"),
                Path.Combine("src", "DTMAPI.BepInExBootstrap", "DTMAPI.BepInExBootstrap.csproj")
            })
            {
                string projectSource = File.ReadAllText(Path.Combine(root, project));
                Assert(!projectSource.Contains("DTMAPI.GameBridge.DolocTown.QA", StringComparison.Ordinal),
                    "Production project must contain zero QA project/assembly references: " + project + ".");
            }

            string settings = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostSettings.cs"));
            string activation = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.BepInExBootstrap", "QaHostActivationLoader.cs"));
            string runner = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-game-smoke.ps1"));
            Assert(QaHostProtocol.ProtocolVersion == 7 &&
                settings.Contains("ProtocolVersion != QaHostProtocol.ProtocolVersion", StringComparison.Ordinal) &&
                activation.Contains("receipt.ProtocolVersion != QaHostProtocol.ProtocolVersion", StringComparison.Ordinal) &&
                CountTextOccurrences(runner, "protocolVersion = 7") >= 2 &&
                runner.Contains("ProtocolVersion = 7", StringComparison.Ordinal) &&
                !runner.Contains("protocolVersion = 4", StringComparison.Ordinal),
                "Protocol source, settings validation, activation validation, staging receipts, and routing projection must agree on protocol 7.");
        }

        private static void G6LifecycleRoutingAndCallbackBoundaryAreClosed()
        {
            string root = FindRepositoryRoot();
            string settingsSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostSettings.cs"));
            string factorySource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostFactory.cs"));
            string participantSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string fixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G6Fixtures.cs"));
            string moreSavesFixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.MoreSavesFixed12Fixtures.cs"));
            string callbackSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "DolocTownHookCallbacks.cs"));
            string workshopTransactionSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "WorkshopOfficialModUiTransaction.cs"));
            string hookSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.Hooks.cs"));
            string bridgeSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.cs"));
            string nativeProbeSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "NativeLoadContinuationProbe.cs"));
            string patcherSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "HarmonyReflectionPatcher.cs"));
            string controllerSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "QaScenarioController.cs"));
            string fixtureSupportSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "DolocTownGameBridge.FixtureSupport.cs"));
            string productQaRoot = Path.Combine(root, "products", "first-party", "AutoFishing", "qa");
            string autoFishingSource = File.ReadAllText(Path.Combine(productQaRoot, "fifth-save", "AutoFishingFixtureCase.cs"));
            string productQaReadme = File.ReadAllText(Path.Combine(productQaRoot, "README.md"));
            string genericQaProject = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "DTMAPI.GameBridge.DolocTown.QA.csproj"));
            string batch6CoordinatorSource = File.ReadAllText(Path.Combine(productQaRoot, "batch6", "Batch6AutoFishingPilotCoordinator.cs"));
            string legacyFishingSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "LegacyFishingAutomationCompatibilityFixtureCase.cs"));
            string fishingSharedSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "CompatibilityFishingFixtureShared.cs"));
            string runnerSource = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-game-smoke.ps1"));
            string moreSavesPostSaveStage = SliceBetween(
                moreSavesFixtureSource,
                "if (g6MoreSavesFixed12Stage == 3)",
                "if (g6MoreSavesFixed12Stage == 4)");

            Assert(settingsSource.Contains("G6LifecycleCases", StringComparison.Ordinal) &&
                settingsSource.Contains("QA G6 save/load, pending-pressure, long-title, and MoreSaves fixed-12 routes are mutually exclusive", StringComparison.Ordinal) &&
                settingsSource.Contains("A MoreSaves fixed-12 phase requires SaveSlot=7, the disposable save redirect", StringComparison.Ordinal) &&
                settingsSource.Contains("QA G6 pre-load forced-GC probe requires SaveLoadCycle", StringComparison.Ordinal) &&
                factorySource.Contains("saveSlot: settings.SaveSlot", StringComparison.Ordinal) &&
                !factorySource.Contains("DisableEmbedded", StringComparison.Ordinal),
                "G6 must validate an exact lifecycle route and project only the receipt-bound startup values before participant creation.");
            Assert(participantSource.Contains("scenarios.PrepareG6Lifecycle(new G6FixtureOptions", StringComparison.Ordinal) &&
                participantSource.Contains("scenarios.AdvanceG6Lifecycle(caseId)", StringComparison.Ordinal) &&
                participantSource.Contains("scenarios.NotifyG6SaveLoaded(slot, isNewGame)", StringComparison.Ordinal) &&
                participantSource.Contains("scenarios.NotifyG6SaveSaved(slot)", StringComparison.Ordinal) &&
                participantSource.Contains("\"MoreSavesFixed12\"", StringComparison.Ordinal) &&
                participantSource.Contains("GetIncompleteG6Requirements()", StringComparison.Ordinal) &&
                !participantSource.Contains("string.Equals(first, \"LegacyFishingCompatibility\"", StringComparison.Ordinal),
                "The G6 participant must own settings projection, update scheduling, SaveLoaded state, and fail-closed completion.");
            Assert(fixtureSource.Contains("TryExerciseSaveLoadCycleForFixture", StringComparison.Ordinal) &&
                fixtureSource.Contains("TryExerciseSaveLoadCyclePendingPressureForFixture", StringComparison.Ordinal) &&
                fixtureSource.Contains("HasContinuousStableObservationForFixture", StringComparison.Ordinal) &&
                fixtureSource.Contains("G6CaseRequiresSaveLoaded(caseId) && saveLoadedAt == default", StringComparison.Ordinal) &&
                fixtureSource.Contains("string.Equals(caseId, \"LegacyFishingCompatibility\"", StringComparison.Ordinal) &&
                !fixtureSource.Contains("AutoFishing", StringComparison.Ordinal) &&
                !fixtureSource.Contains("FishingPrimitive", StringComparison.Ordinal) &&
                fixtureSource.Contains("productionSaveLoadOrder=unchanged", StringComparison.Ordinal) &&
                fixtureSource.Contains("ExerciseOwnerLifetimeAfterSave", StringComparison.Ordinal) &&
                fixtureSource.Contains("G6 owner-lifetime fixture requesting DolocAPI.ReturnHome after save-preserved evidence.", StringComparison.Ordinal) &&
                fixtureSource.Contains("QaHost.G6.ModOwnerLifetime", StringComparison.Ordinal) &&
                !fixtureSource.Contains("ZoomOwnerLifetime", StringComparison.Ordinal) &&
                !settingsSource.Contains("\"ZoomOwnerLifetime\"", StringComparison.Ordinal),
                "The generic G6 seam must preserve HomePage, save/load, pending-pressure, owner-lifetime, and frozen compatibility semantics without ProductNative routes.");
            Assert(
                fixtureSource.Contains("return AdvanceMoreSavesFixed12ForFixture(caseId);", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("GameDataUiState, Assembly-CSharp", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("\"archiveHandle\"", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("\"archiveIndex\"", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("FindMethodInHierarchy(dolocApi, \"SaveGame\", 1)", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("\"DuplicateGame\"", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("\"DeleteGame\"", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("expectedSlotCount: 6", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("expectedSlotCount: 12", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("officialOwners=GameDataUiState|DolocAPI; productWrites=archiveFileCount-only", StringComparison.Ordinal) &&
                moreSavesPostSaveStage.Contains("g6MoreSavesFixed12SaveSaved", StringComparison.Ordinal) &&
                moreSavesPostSaveStage.Contains("ReadMoreSavesFixed12ArchiveInfo(targetIndex)", StringComparison.Ordinal) &&
                moreSavesPostSaveStage.Contains("TryRetireMoreSavesFixed12FirstPlayDialogue", StringComparison.Ordinal) &&
                moreSavesPostSaveStage.Contains("returnHome.Invoke", StringComparison.Ordinal) &&
                moreSavesPostSaveStage.IndexOf("TryRetireMoreSavesFixed12FirstPlayDialogue", StringComparison.Ordinal) <
                    moreSavesPostSaveStage.IndexOf("returnHome.Invoke", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("DolocTown.DialogueState, Assembly-CSharp", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("\"QuitDialogue\"", StringComparison.Ordinal) &&
                moreSavesFixtureSource.Contains("waiting-normal-gameplay-after-dialogue", StringComparison.Ordinal) &&
                !moreSavesFixtureSource.Contains("System.IO", StringComparison.Ordinal) &&
                !moreSavesFixtureSource.Contains("File.", StringComparison.Ordinal),
                "The MoreSaves fixed-12 acceptance must exercise official UI/native create, save, copy, delete, disable, re-enable, and load owners without adding a QA file-mutation implementation; after SaveSaved it must retire the native first-play dialogue through DialogueState.QuitDialogue and regain stable normal gameplay before ReturnHome.");
            Assert(callbackSource.Contains("SaveLoaded.NotifyRuntime", StringComparison.Ordinal) &&
                callbackSource.Contains("RunSaveSavedQaLifecycle", StringComparison.Ordinal) &&
                callbackSource.Contains("RunWorkshopQaLifecycle", StringComparison.Ordinal) &&
                callbackSource.Contains("QaHostSaveSavedNotification?.Invoke", StringComparison.Ordinal) &&
                workshopTransactionSource.Contains("QaHostWorkshopReloadNotification?.Invoke", StringComparison.Ordinal) &&
                workshopTransactionSource.Contains("ProcessPendingOfficialModUiCommit", StringComparison.Ordinal) &&
                workshopTransactionSource.Contains("runtime.NotifyWorkshopModListChanged();", StringComparison.Ordinal) &&
                !callbackSource.Contains("Bridge?.MarkSaveLoadedForFixture()", StringComparison.Ordinal) &&
                !callbackSource.Contains("Bridge?.MarkSaveSavedForFixture()", StringComparison.Ordinal) &&
                !callbackSource.Contains("Bridge?.MarkWorkshopReloadCompletedForFixture()", StringComparison.Ordinal),
                "Production callbacks must retain ordering but route optional evidence through the participant boundary instead of direct Smoke markers.");
            Assert(factorySource.Contains("disableEquipmentSlotsRuntime: isolateUiRuntime", StringComparison.Ordinal) &&
                factorySource.Contains("disabledFeatureIds: isolateUiRuntime", StringComparison.Ordinal) &&
                participantSource.Contains("PublishRootIsolationStatus", StringComparison.Ordinal) &&
                nativeProbeSource.Contains("Smoke.NativeLoadContinuationProbe", StringComparison.Ordinal) &&
                !bridgeSource.Contains("UiRuntime", StringComparison.Ordinal) &&
                !hookSource.Contains("InstallSmokeNativeLoadContinuationProbeHooks", StringComparison.Ordinal),
                "QA must own root-isolation profile policy and native continuation target selection while production receives only neutral startup suppression values.");
            Assert(nativeProbeSource.Contains("QaHarmonyOwnerPrefix + access.RunId", StringComparison.Ordinal) &&
                nativeProbeSource.Contains("patcher.TryUnpatchAllOwnedPatches()", StringComparison.Ordinal) &&
                nativeProbeSource.Contains("internal bool Close(string reason)", StringComparison.Ordinal) &&
                patcherSource.Contains("method.Name.Equals(\"UnpatchAll\"", StringComparison.Ordinal) &&
                patcherSource.Contains("parameters.Length == 1 && parameters[0].ParameterType == typeof(string)", StringComparison.Ordinal) &&
                patcherSource.Contains("new object[] { ownerId }", StringComparison.Ordinal) &&
                participantSource.IndexOf("nativeLoadContinuationProbe?.Close(closeReason)", StringComparison.Ordinal) < participantSource.IndexOf("closed = true;", StringComparison.Ordinal),
                "The optional native continuation probe must use a run-scoped Harmony owner and exact owner-only unpatch before participant close commits.");
            Assert(fixtureSource.Contains("CleanupOwnerLifetimeOnClose", StringComparison.Ordinal) &&
                fixtureSource.Contains("DisposeOwnerLifetimeLease(ref ownerLifetimeControlCameraLease", StringComparison.Ordinal) &&
                fixtureSource.Contains("DisposeOwnerLifetimeLease(ref ownerLifetimeCleanupCameraLease", StringComparison.Ordinal) &&
                CountTextOccurrences(fixtureSource, "TryDeactivateOwnerLifetimeOwner(") >= 3 &&
                controllerSource.Contains("RunScenarioCloseStep(failures, \"OwnerLifetimeCleanup\"", StringComparison.Ordinal),
                "G6 failure close must release both Camera leases and deactivate both synthetic owners through a retryable close step.");
            string retiredSmokeDirectory = Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Smoke");
            Assert((!Directory.Exists(retiredSmokeDirectory) || !Directory.EnumerateFiles(retiredSmokeDirectory, "*.cs", SearchOption.AllDirectories).Any()) &&
                !fixtureSupportSource.Contains("QaHostOwnsG6Lifecycle", StringComparison.Ordinal) &&
                !fixtureSupportSource.Contains("DisableEmbeddedG6Lifecycle", StringComparison.Ordinal),
                "G7 must delete the rollback harness and every G6 dual-owner switch.");
            Assert(autoFishingSource.Contains("VerifyAutoFishingReportExportForFixture(scenario);", StringComparison.Ordinal) &&
                autoFishingSource.Contains("runtime.SetHookStatus(\"Smoke.AutoFishingLifecycle\", \"verified\"", StringComparison.Ordinal) &&
                autoFishingSource.Contains("Retired historical Batch 5 movement fixture", StringComparison.Ordinal) &&
                autoFishingSource.Contains("Use the formal Batch 6 behavior matrix", StringComparison.Ordinal) &&
                !autoFishingSource.Contains("autoFishingMovementCancelReadyAt", StringComparison.Ordinal) &&
                !autoFishingSource.Contains("AddMilliseconds(1100)", StringComparison.Ordinal) &&
                autoFishingSource.Contains("autoFishingHotkeyInjected = false;", StringComparison.Ordinal) &&
                autoFishingSource.Contains("runtime.Input.ClearFrame();", StringComparison.Ordinal) &&
                productQaReadme.Contains("Historical Batch 5 fixtures", StringComparison.Ordinal) &&
                !File.Exists(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "AutoFishingFixtureCase.cs")),
                "The superseded fifth-save Batch 5 control source must remain product-owned historical material, not return to the generic G6 controller.");
            Assert(legacyFishingSource.Contains("feature.CountOwnerResources(OwnerId) != 0", StringComparison.Ordinal) &&
                legacyFishingSource.Contains("FishingAutomationCompatibilityFeature", StringComparison.Ordinal) &&
                legacyFishingSource.Contains("feature.Service != null || feature.CallbackRuntime != null", StringComparison.Ordinal) &&
                !legacyFishingSource.Contains("Primitives", StringComparison.Ordinal) &&
                !legacyFishingSource.Contains("ProductNative", StringComparison.Ordinal) &&
                fishingSharedSource.Contains("FishingSmokeCaseResult", StringComparison.Ordinal),
                "The old-DLL compatibility case must exercise only the frozen compatibility feature and remove its synthetic owner.");
            Assert(settingsSource.Contains("Batch6AutoFishingPilot", StringComparison.Ordinal) &&
                participantSource.Contains("Batch6AutoFishingPilotCoordinator", StringComparison.Ordinal) &&
                participantSource.Contains("batch6AutoFishingPilot?.Advance()", StringComparison.Ordinal) &&
                batch6CoordinatorSource.Contains("Batch6AutoFishingPilotSettings.CaseId", StringComparison.Ordinal) &&
                genericQaProject.Contains("products\\first-party\\AutoFishing\\qa\\batch6", StringComparison.Ordinal) &&
                !genericQaProject.Contains("ProjectReference Include=\"..\\..\\products\\first-party\\AutoFishing", StringComparison.Ordinal) &&
                !genericQaProject.Contains("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal) &&
                !controllerSource.Contains("AutoFishing", StringComparison.Ordinal),
                "Generic QA may deserialize and delegate the linked product-owned Batch6 pilot, but its shared scenario controller must not own ProductNative logic and the optional DLL must have no product project/assembly reference.");
            Assert(runnerSource.Contains("G6LifecycleCases = @($G6LifecycleCases)", StringComparison.Ordinal) &&
                runnerSource.Contains("G6NativeLoadContinuationProbe = $G6NativeLoadContinuationProbe", StringComparison.Ordinal) &&
                runnerSource.Contains("Hook status: Smoke\\.NativeLoadContinuationProbe = closed\\.", StringComparison.Ordinal) &&
                runnerSource.Contains("unpatchSucceeded=True;", StringComparison.Ordinal) &&
                runnerSource.Contains("SmokeNativeLoadContinuationCleanup", StringComparison.Ordinal) &&
                runnerSource.Contains("Hook status: Smoke\\.OwnerLifetimeCleanup = verified\\.", StringComparison.Ordinal) &&
                runnerSource.Contains("OwnerLifetimeCloseCleanup", StringComparison.Ordinal) &&
                runnerSource.Contains("ProductionSaveLoadOrder = 'unchanged'", StringComparison.Ordinal),
                "The runner must project the complete G6 route, require owner-only probe and synthetic-owner/Camera cleanup, and expose its ordering receipt.");

            const string runId = "cdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcd";
            QaHostSettings valid = QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 3,
                G6LifecycleCases = new[] { "ModOwnerLifetime", "SaveLoadCycle" },
                G6SaveLoadCycleCount = 1,
                G6SaveLoadCycleIntervalSeconds = 5,
                G6SaveLoadCycleInSaveSeconds = 5,
                G6PendingPressureIntervalSeconds = 2d
            })));
            valid.Validate(runId);
            Assert(valid.G6LifecycleCases.SequenceEqual(new[] { "ModOwnerLifetime", "SaveLoadCycle" }),
                "A valid G6 route must preserve deterministic runner order.");

            QaHostSettings invalid = QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 3,
                G6LifecycleCases = new[] { "SaveLoadCycle", "SaveLoadPendingPressure" },
                G6SaveLoadCycleIntervalSeconds = 5,
                G6SaveLoadCycleInSaveSeconds = 5,
                G6PendingPressureIntervalSeconds = 2d
            })));
            AssertThrows<InvalidDataException>(() => invalid.Validate(runId), "Mutually exclusive G6 save/load routes must fail before activation.");

            string routing = RunQaG6RoutingSelfTest();
            Assert(routing.Contains("\"ProtocolVersion\": 7", StringComparison.OrdinalIgnoreCase) &&
                routing.Contains("ModOwnerLifetime", StringComparison.Ordinal) &&
                routing.Contains("SaveLoadCycle", StringComparison.Ordinal) &&
                routing.Contains("\"ProductionSaveLoadOrder\": \"unchanged\"", StringComparison.OrdinalIgnoreCase),
                "Executed G6 routing must select the exact ordered cases and retain production save/load ordering.");
        }

        private static void G6FailureCloseReleasesSyntheticOwnersAndCameraLeases()
        {
            const string runId = "edededededededededededededededed";
            const string controlOwner = "DTMAPI.Smoke.OwnerLifetime.Control";
            const string cleanupOwner = "DTMAPI.Smoke.OwnerLifetime.Cleanup";
            string gameDir = Path.Combine(DtmApiTestSession.Current.RootPath, "g6-owner-failure-close", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gameDir);
            StageCompatibilityHostFixture(gameDir);
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var bridge = new DolocTownGameBridge(runtime);
            (bridge.CameraFeatureForQa ??
                throw new InvalidOperationException(
                    "The test GameBridge Camera feature was not registered."))
                .HookBridge
                .SetCompatibilityOwnerClaimOverrideForTests(
                    () => true);
            var access = new GameBridgeFixtureAccess(runtime, runId, Path.Combine(gameDir, "qa-evidence"), () => true, bridge);
            var controller = new QaScenarioController(access);

            controller.PrepareG6Lifecycle(new G6FixtureOptions
            {
                Cases = new[] { "ModOwnerLifetime" },
                SaveSlot = 3,
                InitialDelaySeconds = 1
            });

            ICameraViewApi camera = bridge.CameraFeatureForQa?.ViewApi
                ?? throw new InvalidOperationException("The test GameBridge Camera API was not registered.");
            IHookStatusInfo preparedStatus = runtime.Diagnostics.GetHookStatuses()
                .Last(status => status.HookId.Equals("Smoke.OwnerLifetime", StringComparison.OrdinalIgnoreCase));
            Assert(preparedStatus.Status.Equals("prepared", StringComparison.OrdinalIgnoreCase) &&
                !preparedStatus.Details.Contains("controlRoots=0", StringComparison.Ordinal) &&
                !preparedStatus.Details.Contains("cleanupRoots=0", StringComparison.Ordinal),
                "G6 setup must create both synthetic Core owner-root sets before the failure-close assertion.");
            Assert(camera.GetState(controlOwner).LeaseCount == 1 && camera.GetState(cleanupOwner).LeaseCount == 1,
                "G6 setup must acquire one Camera lease for each synthetic owner.");

            controller.Close("failure:unit-owner-lifetime");
            controller.Close("failure:unit-owner-lifetime-retry");

            Assert(camera.GetState(controlOwner).LeaseCount == 0 && camera.GetState(cleanupOwner).LeaseCount == 0,
                "G6 failure close must release both synthetic Camera leases.");
            IHookStatusInfo cleanupStatus = runtime.Diagnostics.GetHookStatuses()
                .Last(status => status.HookId.Equals("Smoke.OwnerLifetimeCleanup", StringComparison.OrdinalIgnoreCase));
            Assert(cleanupStatus.Status.Equals("verified", StringComparison.OrdinalIgnoreCase) &&
                cleanupStatus.Details.Contains("controlRoots=0", StringComparison.Ordinal) &&
                cleanupStatus.Details.Contains("cleanupRoots=0", StringComparison.Ordinal) &&
                cleanupStatus.Details.Contains("controlInstance=False", StringComparison.Ordinal) &&
                cleanupStatus.Details.Contains("cleanupInstance=False", StringComparison.Ordinal) &&
                cleanupStatus.Details.Contains("cleanupCameraLeases=0", StringComparison.Ordinal),
                "G6 failure close must publish a zero-root/zero-instance/zero-Camera cleanup receipt.");
        }

        private static void CameraPlayableSuppressesSynchronousRuntimeReentry()
        {
            string evidenceDirectory = Path.Combine(DtmApiTestSession.Current.RootPath, "camera-playable-reentry", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(evidenceDirectory);
            var api = new RetryableCameraViewApi(failFirstLowRelease: false);
            CameraPlayableMovementFixtureScenario? scenario = null;
            G4FixtureStepResult? nested = null;
            int automationCalls = 0;
            scenario = new CameraPlayableMovementFixtureScenario(
                api,
                "reentrant-unit",
                api.CountOwnerRoots,
                () => new AgentPositionFixtureReceipt(true, 100d, 50d, 0d),
                _ => true,
                () => new CameraPositionFixtureReceipt(true, 100d, 50d, 0d),
                _ => true,
                (x, y, z) => new CameraPlayablePositionSample { AgentX = x, AgentY = y, AgentZ = z, CameraX = x, CameraY = y, CameraZ = z },
                () =>
                {
                    automationCalls++;
                    if (automationCalls == 1)
                        nested = scenario!.Advance();
                },
                path =>
                {
                    File.WriteAllBytes(path, new byte[] { 1 });
                    return true;
                },
                evidenceDirectory);

            FieldInfo settle = typeof(CameraPlayableMovementFixtureScenario).GetField("presentationSettleStartedAt", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("CameraPlayable presentation-settle field is unavailable.");
            settle.SetValue(scenario, DateTimeOffset.UtcNow.AddSeconds(-3));

            G4FixtureStepResult before = scenario.Advance();
            Assert(!before.Completed && File.Exists(Path.Combine(evidenceDirectory, "before.png")),
                "CameraPlayable must enter its screenshot-bound activation stage before lease acquisition.");
            G4FixtureStepResult activation = scenario.Advance();
            Assert(!activation.Completed && nested != null && !nested.Completed &&
                nested.Details.Contains("re-entry was suppressed", StringComparison.Ordinal) &&
                api.AcquisitionCount == 2 && api.LiveLeaseCount == 2,
                "A synchronous runtime callback must observe the in-progress guard and exactly one low/high lease pair.");

            string close = scenario.Close();
            Assert(api.AcquisitionCount == 2 && api.LiveLeaseCount == 0 &&
                close.Contains("acquired=2; released=2; root=0", StringComparison.Ordinal) &&
                close.Contains("agentRestored=True; cameraRestored=True", StringComparison.Ordinal),
                "CameraPlayable re-entry cleanup must release the exact pair and retain zero owner roots.");
        }

        private static string RunQaG6RoutingSelfTest()
        {
            string runner = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1");
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in new[]
            {
                "-NoLogo", "-NoProfile", "-File", runner,
                "-StageQaHost", "-SaveSlot", "3", "-AutoExerciseModOwnerLifetime",
                "-AutoExerciseSaveLoadCycle", "-SaveLoadCycleCount", "1", "-ValidateQaG6RoutingOnly"
            })
                startInfo.ArgumentList.Add(argument);
            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the G6 routing self-test.");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert(process.ExitCode == 0, "G6 runner routing self-test failed: " + stderr);
            return stdout;
        }

        private static void G5WorldMutationRoutingAndRestorationAreClosed()
        {
            string root = FindRepositoryRoot();
            string settingsSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostSettings.cs"));
            string factorySource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostFactory.cs"));
            string participantSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string qaHostBridgeSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "DolocTownGameBridge.QaHost.cs"));
            string accessSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "GameBridgeFixtureAccess.cs"));
            string fixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G5Fixtures.cs"));
            string g6FixtureRoutingSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G6Fixtures.cs"));
            string controllerSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "QaScenarioController.cs"));
            string fixtureSupportSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "DolocTownGameBridge.FixtureSupport.cs"));
            string runnerSource = File.ReadAllText(Path.Combine(root, "tools", "scripts", "run-game-smoke.ps1"));
            string actionCompletionSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "ActionCompletionFixtureCase.cs"));
            string actionSpeedSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "ActionSpeedFixtureCase.cs"));
            string advancedOwnerDeactivationSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "AdvancedProductOwnerDeactivationFixture.cs"));
            string moreEquipmentSlotsFixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "MoreEquipmentSlotsFixtureCase.cs"));
            string moreEquipmentSlotsNoNativeSaveFixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "MoreEquipmentSlotsNoNativeSaveFixtureCase.cs"));
            string harmonyOwnerObserverSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Batch6AdvancedHarmonyOwnerObserver.cs"));
            string saveFixtureIsolationSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaSaveFixtureIsolation.cs"));
            string chestSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "ChestLocatorEnhancerFixtureCase.cs"));
            string strongSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "StrongPlantingGunFixtureCase.cs"));
            string zoomProductSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "ZoomProductNativeFixtureCase.cs"));
            string cropSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "CropHarvestingFixtureCase.cs"));
            string contentSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "ContentFixture.cs"));
            string g4FixtureSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G4Fixtures.cs"));
            string equipmentProductRoot = Path.Combine(root, "products", "first-party", "MoreEquipmentSlots", "src", "Native");
            string equipmentProductSource = string.Join(
                Environment.NewLine,
                Directory.GetFiles(equipmentProductRoot, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(File.ReadAllText));
            string equipmentCompatibilityRoot = Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Compatibility", "EquipmentSlots");
            string equipmentCompatibilitySource = string.Join(
                Environment.NewLine,
                Directory.GetFiles(equipmentCompatibilityRoot, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(File.ReadAllText));
            string gameBridgeProjectSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "DTMAPI.GameBridge.DolocTown.csproj"));
            string actionSpeedProductionSource = File.ReadAllText(Path.Combine(root, "products", "first-party", "ActionSpeed", "src", "Native", "ActionSpeedEngine.cs"));
            string mineProductNativeRoot = Path.Combine(root, "products", "first-party", "Mine", "src", "Native");
            string mineProductNativeSource = string.Join(
                Environment.NewLine,
                Directory.GetFiles(mineProductNativeRoot, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(File.ReadAllText));
            string mandatoryGameBridgeProductionSource = string.Join(
                Environment.NewLine,
                Directory.GetFiles(
                        Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown"),
                        "*.cs",
                        SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(File.ReadAllText));
            string bridgeProductionSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.cs"));
            string legacyFishingProductionSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown", "Compatibility", "FishingAutomation", "LegacyFishingAutomationService.cs"));
            string coreRuntimeSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.Core", "Runtime", "DtmApiRuntime.cs"));
            string titleUiSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.BepInExBootstrap", "ReflectedTitleMenuSettingsUi.cs"));
            string debugSource = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "Fixtures", "DebugConsoleFixture.cs"));

            Assert(settingsSource.Contains("G5WorldMutationCases", StringComparison.Ordinal) &&
                settingsSource.Contains("Unsupported G5 world-mutation case(s)", StringComparison.Ordinal) &&
                settingsSource.Contains("require a positive SaveSlot", StringComparison.Ordinal) &&
                !factorySource.Contains("DisableEmbedded", StringComparison.Ordinal),
                "Protocol 5 must validate an exact G5 whitelist without retaining an embedded-owner switch.");
            Assert(participantSource.Contains("scenarios.AdvanceG5WorldMutation(caseId)", StringComparison.Ordinal) &&
                participantSource.Contains("settings.G5WorldMutationCases.Length == 0 || g5WorldMutationCompleted", StringComparison.Ordinal) &&
                participantSource.Contains("saveLoadCycleMayDriveInitialLoad", StringComparison.Ordinal) &&
                participantSource.Contains("!saveLoadedObserved", StringComparison.Ordinal) &&
                participantSource.Contains("string.Equals(settings.G6LifecycleCases[g6LifecycleIndex], \"SaveLoadCycle\"", StringComparison.Ordinal) &&
                participantSource.Contains("GetIncompleteG5Requirements()", StringComparison.Ordinal) &&
                participantSource.Contains("ReturnedToTitle interrupted incomplete G5 world-mutation cases", StringComparison.Ordinal) &&
                controllerSource.Contains("internal G4FixtureStepResult AdvanceG5WorldMutation", StringComparison.Ordinal) &&
                !accessSource.Contains("AdvanceG5WorldMutation", StringComparison.Ordinal) &&
                fixtureSource.Contains("The G5 fixture seam accepts only the reviewed world-mutation whitelist", StringComparison.Ordinal),
                "G5 must have one fail-closed QA coordinator, allow SaveLoadCycle to drive only its initial load, complete before destructive G6 title cycles, and retain a narrow internal bridge seam.");
            Assert(!fixtureSupportSource.Contains("QaHostOwnsG5WorldMutation", StringComparison.Ordinal) &&
                !fixtureSupportSource.Contains("TryObserveQaOwnedCaseTerminal", StringComparison.Ordinal),
                "G7 must remove the embedded G5 scheduler and dual-owner terminal consumer.");
            Assert(!actionCompletionSource.Contains("FindExistingEquipmentForFixture", StringComparison.Ordinal) &&
                !actionCompletionSource.Contains("real DungeonResourceRenderer", StringComparison.Ordinal) &&
                !actionSpeedSource.Contains("FindExistingEquipmentForFixture", StringComparison.Ordinal) &&
                !actionSpeedSource.Contains("FindExistingResinCollectorForFixture", StringComparison.Ordinal),
                "G5 ActionCompletion and ActionSpeed must never fall back to real player world objects.");
            Assert(actionSpeedSource.Contains("SummaryContainsKind(bridgeSummary, \"MachineInteract\")", StringComparison.Ordinal) &&
                !actionSpeedSource.Contains("SummaryContainsKind(bridgeSummary, \"MachineAdd\")", StringComparison.Ordinal),
                "G5 ActionSpeed machine-interaction receipt must match the production bridge kind.");
            Assert(actionCompletionSource.Contains("Smoke.OneActionPartialEnergy", StringComparison.Ordinal) &&
                actionCompletionSource.Contains("paidExtraHits=1; complete=false", StringComparison.Ordinal) &&
                actionCompletionSource.Contains("Smoke.OneActionConfigReload", StringComparison.Ordinal) &&
                actionCompletionSource.Contains("ReadOwnerConfigForFixture", StringComparison.Ordinal) &&
                actionCompletionSource.Contains("overflowEnergy", StringComparison.Ordinal),
                "OneActionComplete continuation QA must prove bounded partial-energy behavior and ConfigMenu save/reload while restoring the original player/config state.");
            Assert(harmonyOwnerObserverSource.Contains("GetPatchInfo", StringComparison.Ordinal) &&
                harmonyOwnerObserverSource.Contains("CountAllOwnerPatchesWithPrefix", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("CountAllOwnerPatchesWithPrefix", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("redirectedSaveRoot", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("installation failed closed", StringComparison.Ordinal) &&
                harmonyOwnerObserverSource.Contains("ExactOwnerTargetCount", StringComparison.Ordinal) &&
                actionSpeedSource.Contains("ActualHarmonyOwnerReady", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("runtime.DeactivateOwner", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("CanonicalHarmonyPatchCount != 0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("oneActionAfter.ExactOwnerPatchCount != 0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("fishBreedingAfter.ExactOwnerPatchCount != 0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("animalHusbandryAfter.ExactOwnerPatchCount != 0", StringComparison.Ordinal) &&
                harmonyOwnerObserverSource.Contains("CountAllOwnerPatches", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("MoreSaves=native12+actual0->instance0+native6+actual0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("moreSavesHarmonyBefore != 0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("moreSavesNativeAfter != 6", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("FishBreedingAssistant=actual1+callback1->instance0+actual0+callback0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("AnimalHusbandryProgress=actual4+targets3+callback1->instance0+actual0+callback0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("ChestLocatorEnhancer=actual1+callback1->instance0+actual0+callback0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("Zoom=title4to1+native1+derived1+actual1+callback1->instance0+actual0+", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("zoomNativeSizeBefore", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("zoomVanillaBefore", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("MoreEquipmentSlots=actual4+targets4+callback1->instance0+actual0+callback0+clones0+listeners0+functions0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("StrongPlantingGun=nativeSave+titleReentry+actual5+callback1->instance0+actual0+callback0+listeners0+cachedObjects0+cachedMembers0+capacitySnapshots0+roots0", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("LifecycleSummaryProvesZero", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("StrongPlantingGunLifecycleSummaryProvesActive", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("StrongPlantingGunLifecycleSummaryProvesZero", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("strongPlantingGunNativeSavePrepared", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("Waiting for the requested third-save reload to publish SaveLoaded", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("IReadOnlyCollection<string> requestedOwnerIds", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (fishBreedingRequested)", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (animalHusbandryRequested)", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (moreSavesRequested)", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (chestLocatorRequested)", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (moreEquipmentSlotsRequested)", StringComparison.Ordinal) &&
                advancedOwnerDeactivationSource.Contains("if (strongPlantingGunRequested)", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("fixedExtraSlots=3", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("patches=4", StringComparison.Ordinal) &&
                participantSource.Contains("settings.AdvancedProductOwnerDeactivationEnabled", StringComparison.Ordinal) &&
                participantSource.Contains("settings.AdvancedProductOwnerDeactivationOwnerIds", StringComparison.Ordinal) &&
                participantSource.Contains("if (!deactivation.Completed)", StringComparison.Ordinal) &&
                participantSource.Contains("\"Smoke.AdvancedProductOwnerDeactivation\", \"pending\"", StringComparison.Ordinal) &&
                participantSource.Contains("RequiresStrongPlantingGunReentry()", StringComparison.Ordinal) &&
                participantSource.Contains("scenarios.AdvanceStrongPlantingGunReentryLoad(", StringComparison.Ordinal) &&
                participantSource.Contains("Smoke.MoreSavesPostTitleOfficialSaveUi", StringComparison.Ordinal) &&
                participantSource.Contains("string.Equals(item, \"MoreSavesPostTitlePanel\", StringComparison.Ordinal)", StringComparison.Ordinal) &&
                g4FixtureSource.Contains("postTitle=true; slotCount=12", StringComparison.Ordinal) &&
                settingsSource.Contains("AdvancedProductOwnerDeactivationOwnerIds", StringComparison.Ordinal) &&
                settingsSource.Contains("\"DTMAPI.ChestLocatorEnhancerMod\"", StringComparison.Ordinal) &&
                settingsSource.Contains("\"DTMAPI.MoreEquipmentSlotsMod\"", StringComparison.Ordinal) &&
                settingsSource.Contains("\"DTMAPI.StrongPlantingGunMod\"", StringComparison.Ordinal) &&
                settingsSource.Contains("\"DTMAPI.MineMod\"", StringComparison.Ordinal) &&
                settingsSource.Contains("requires an explicit non-empty owner ID set exactly when enabled", StringComparison.Ordinal) &&
                runnerSource.Contains("AdvancedProductOwnerDeactivationOwnerIds", StringComparison.Ordinal) &&
                runnerSource.Contains("'DTMAPI.StrongPlantingGunMod' = [bool]$AutoExerciseStrongPlantingGun", StringComparison.Ordinal) &&
                runnerSource.Contains("StrongPlantingGun owner deactivation requires -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 1", StringComparison.Ordinal) &&
                runnerSource.Contains("strong-planting-gun-config-stage.json", StringComparison.Ordinal) &&
                runnerSource.Contains("RestoreOwner = 'strong-planting-gun-exact-config-file'", StringComparison.Ordinal) &&
                runnerSource.Contains("ConfigDirectoryTransaction = 'forbidden'", StringComparison.Ordinal) &&
                runnerSource.IndexOf("g5-external-state-baseline.json", StringComparison.Ordinal) <
                    runnerSource.IndexOf("strong-planting-gun-config-stage.json", StringComparison.Ordinal) &&
                runnerSource.IndexOf("strong-planting-gun-config-stage.json", StringComparison.Ordinal) <
                    runnerSource.IndexOf("$qaHostStage = if ($StageQaHost)", StringComparison.Ordinal) &&
                runnerSource.Contains("PlayerSaveUnchangedBeforeCleanup", StringComparison.Ordinal) &&
                runnerSource.Contains("CommittedSidecarsUnchangedBeforeCleanup", StringComparison.Ordinal) &&
                runnerSource.Contains("RequireDisposableSaveRedirect", StringComparison.Ordinal) &&
                runnerSource.Contains("ownerEvidenceRequirements", StringComparison.Ordinal),
                "Advanced-product QA must observe real Harmony inventory and deactivate only the explicit requested-owner set after title recovery.");
            int participantPrepare = participantSource.IndexOf("public void PrepareBeforeRuntimeStart()", StringComparison.Ordinal);
            int participantStart = participantSource.IndexOf("public void Start()", StringComparison.Ordinal);
            int participantInstall = participantSource.IndexOf("saveFixtureIsolation?.Install();", StringComparison.Ordinal);
            Assert(factorySource.Contains("requirePreRuntimeSaveIsolation: settings.RequireDisposableSaveRedirect", StringComparison.Ordinal) &&
                participantSource.Contains("IQaHostPreRuntimeParticipant", StringComparison.Ordinal) &&
                participantPrepare >= 0 && participantInstall > participantPrepare && participantStart > participantInstall &&
                !participantSource.Substring(participantStart, participantSource.IndexOf("public void Update()", participantStart, StringComparison.Ordinal) - participantStart)
                    .Contains("saveFixtureIsolation?.Install();", StringComparison.Ordinal) &&
                participantSource.Contains("The required disposable save guard was not installed before Runtime Mod loading", StringComparison.Ordinal) &&
                qaHostBridgeSource.Contains("internal void PrepareQaHostBeforeRuntimeStart()", StringComparison.Ordinal) &&
                qaHostBridgeSource.Contains("preRuntimeParticipant.PrepareBeforeRuntimeStart();", StringComparison.Ordinal) &&
                qaHostBridgeSource.Contains("internal void AbortQaHostBeforeRuntimeStart(string reason)", StringComparison.Ordinal) &&
                qaHostBridgeSource.Contains("RetainQaHostForProcessExit(\"failure:\" + operation)", StringComparison.Ordinal) &&
                qaHostBridgeSource.Contains("releaseBoundary=process-shutdown", StringComparison.Ordinal),
                "Disposable save isolation must be an explicit receipt-bound pre-Runtime participant boundary with a Bootstrap cleanup fallback, never a participant Start side effect.");
            Assert(saveFixtureIsolationSource.Contains("get_cloudDirPath", StringComparison.Ordinal) &&
                !saveFixtureIsolationSource.Contains("get_dataDirPath", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("VerifyCurrentNativeSavePaths", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("dataPersistenceManager", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("fileDataHandler", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("GetDataFullPath", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("GetDataBackupPath", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("GetDataPrevPath", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("GetDataTempPath", StringComparison.Ordinal) &&
                saveFixtureIsolationSource.Contains("preRuntime=true;nativePathProbe=true", StringComparison.Ordinal),
                "The current 24456188 QA guard must patch LocalSave.cloudDirPath and execute every native archive-path helper against the exact disposable SAVE root before Runtime Mod loading.");
            Assert(actionSpeedSource.Contains("TryInvokeUseItemContinuesForFixture(dolocApi, 0.2f, out invokeSummary)", StringComparison.Ordinal),
                "G5 transient SimpleWell bottle-fill case must drive ItemBottle's native UseItemContinues owner.");
            Assert(fixtureSupportSource.Contains("bool isCurrentRoom = ReferenceEquals(room, currentRoom);", StringComparison.Ordinal) &&
                fixtureSupportSource.Contains("for (int y = 1; y <= maxY; y++)", StringComparison.Ordinal) &&
                fixtureSupportSource.Contains("if (seen.Add(key))", StringComparison.Ordinal),
                "G5 cross-room transient equipment creation must deterministically search only native-empty grid footprints.");
            Assert(chestSource.Contains("DTMAPI.QA.G5.ChestLocatorEnhancer", StringComparison.Ordinal) &&
                chestSource.Contains("RemoveOwner(qaOwnerId", StringComparison.Ordinal) &&
                cropSource.Contains("DTMAPI.QA.G5.CropHarvesting", StringComparison.Ordinal) &&
                cropSource.Contains("RemoveOwner(qaOwnerId", StringComparison.Ordinal),
                "G5 retained-API feature probes must use internal QA owners and remove every owner at case terminal.");
            string strongSeasonProbe = SliceBetween(
                strongSource,
                "MethodInfo? checkSeason",
                "notes.Add(");
            Assert(
                strongSource.Contains("StrongPlantingGunHarmonyTargets", StringComparison.Ordinal) &&
                strongSource.Contains("\"DolocTown.ItemFarmingGun\"", StringComparison.Ordinal) &&
                strongSource.Contains("\"DolocTown.FarmingGunUiState\"", StringComparison.Ordinal) &&
                strongSource.Contains("\"HandlePlaceToOtherSide\"", StringComparison.Ordinal) &&
                strongSource.Contains("\"HandleSwapOneItem\"", StringComparison.Ordinal) &&
                strongSource.Contains("ExactOwnerPatchCount != 5", StringComparison.Ordinal) &&
                strongSource.Contains("EnterStrongPlantingGunUiForFixture", StringComparison.Ordinal) &&
                strongSource.Contains("PrimeStrongPlantingGunCellTipForFixture", StringComparison.Ordinal) &&
                strongSource.Contains("official AgentCellTip.Show offset=", StringComparison.Ordinal) &&
                strongSource.Contains("CheckCurrentSeasonValid", StringComparison.Ordinal) &&
                strongSource.Contains("seasonValid=True", StringComparison.Ordinal) &&
                strongSeasonProbe.Contains("\"CheckCurrentSeasonValid\"", StringComparison.Ordinal) &&
                strongSeasonProbe.Contains("1);", StringComparison.Ordinal) &&
                strongSeasonProbe.Contains("false", StringComparison.Ordinal) &&
                strongSource.Contains("uiPlaceTakeSwapOneTwice=True", StringComparison.Ordinal) &&
                strongSource.Contains("backpackSeedBaseline + 2", StringComparison.Ordinal) &&
                strongSource.Contains("RequestStrongPlantingGunNativeSave", StringComparison.Ordinal) &&
                strongSource.Contains("AdvanceStrongPlantingGunReentryLoad", StringComparison.Ordinal) &&
                strongSource.Contains("strongPlantingGunReentryLoadRequested", StringComparison.Ordinal) &&
                strongSource.Contains("TryAutoLoadSave(humanSlot)", StringComparison.Ordinal) &&
                strongSource.Contains("ContinueInitialSaveLoad();", StringComparison.Ordinal) &&
                strongSource.Contains("ObserveStrongPlantingGunReentryAfterSaveLoadedForFixture", StringComparison.Ordinal) &&
                strongSource.Contains("strongPlantingGunReentrySaveLoadedObserved", StringComparison.Ordinal) &&
                strongSource.Contains("the QA route does not fall back to the frozen API or Compatibility Host", StringComparison.Ordinal) &&
                g6FixtureRoutingSource.Contains("MarkStrongPlantingGunReentrySaveLoadedForFixture();", StringComparison.Ordinal) &&
                !g6FixtureRoutingSource.Contains("ObserveStrongPlantingGunReentryAfterSaveLoadedForFixture();", StringComparison.Ordinal) &&
                !strongSource.Contains("IStrongPlantingGunApi", StringComparison.Ordinal) &&
                !strongSource.Contains("StrongPlantingGunService", StringComparison.Ordinal) &&
                !strongSource.Contains(".Register(", StringComparison.Ordinal) &&
                !strongSource.Contains("RemoveOwner(", StringComparison.Ordinal),
                "The ninth-product G5/G6 route must externally observe all five ProductNative Hooks, exercise real UI/tool/native-save re-entry, and never create a frozen-ABI or Host fallback consumer.");
            Assert(strongSource.Contains("Waiting for CurrentRoom after SaveLoaded.", StringComparison.Ordinal) &&
                zoomProductSource.Contains("Waiting for CurrentRoom after SaveLoaded.", StringComparison.Ordinal) &&
                zoomProductSource.IndexOf("object? room =", StringComparison.Ordinal) <
                    zoomProductSource.IndexOf("G5 ProductNative 2x", StringComparison.Ordinal) &&
                cropSource.Contains("Waiting for CurrentRoom after SaveLoaded.", StringComparison.Ordinal),
                "G5 world cases must remain pending while the native room is not yet published after SaveLoaded.");
            string advancedDebug = SliceBetween(debugSource, "private void TryExerciseAdvancedDebugForFixture()", "private FixtureAttemptResult TryExerciseDebugTeleportForFixture()");
            Assert(advancedDebug.IndexOf("CurrentRoom IMonsterHost + IDungeonResourceHost preflight", StringComparison.Ordinal) <
                advancedDebug.IndexOf("advancedDebugApi.AdvanceTime(owner", StringComparison.Ordinal) &&
                advancedDebug.Contains("Waiting for the loaded save to publish a room", StringComparison.Ordinal),
                "G5 AdvancedDebug must wait for both native spawn hosts before its first time/money/world mutation.");
            Assert(moreEquipmentSlotsFixtureSource.Contains("TryExerciseMoreEquipmentSlotsForFixture", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("MoreEquipmentSlotsAssemblyName", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("MoreEquipmentSlotsHarmonyOwner", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("TryReadValidated", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("\"sidecarPath\"", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("MoreEquipmentSlotsShieldItemId", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("InvokeRealMoreEquipmentSlotsAttack(", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("replacement=grandmas_button->box_hat", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("shieldBreak=true equipAfterBreak=true unequipAfterBreak=true", StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains("SaveSaved did not commit the real damaged shield state.", StringComparison.Ordinal) &&
                !moreEquipmentSlotsFixtureSource.Contains("RegisterSlots(", StringComparison.Ordinal),
                "The eighth-product NativeSaveExpected route must drive replacement, real ProductNative shield damage/break, equip/unequip, and committed sidecar observation without registering a second frozen-ABI consumer.");
            int interruptedObserver =
                moreEquipmentSlotsFixtureSource.IndexOf(
                    "TryObserveMoreEquipmentSlotsInterruptedCandidateRecovery",
                    StringComparison.Ordinal);
            int firstProductMutation =
                moreEquipmentSlotsFixtureSource.IndexOf(
                    "BeginMoreEquipmentSlotsProtectedTransaction(",
                    StringComparison.Ordinal);
            Assert(
                interruptedObserver >= 0 &&
                firstProductMutation > interruptedObserver &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "expectedMoreEquipmentSlotsCandidatePreGeneration +",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "persistedCleanupWrites=1",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "journal=false",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "candidate=false",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "beforeGiveItem=true",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsFixtureSource.Contains(
                    "beforeNativeSave=true",
                    StringComparison.Ordinal),
                "The NativeSaveExpected interrupted-candidate observation must prove the single persisted discard before the existing product mutation/save stages.");
            Assert(
                fixtureSource.Contains(
                    "case \"MoreEquipmentSlotsNoNativeSave\"",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "Smoke.MoreEquipmentSlotsNoNativeSave",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "backpackBaseline=",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "committedOccupied=",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "committedSlots=",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "workingOccupied=",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "workingDirty",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "replacement=grandmas_button->box_hat",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "InvokeRealMoreEquipmentSlotsAttack(",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "shieldBreak=true",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "noSaveUnequip=true",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "nativeSaveRequested=false",
                    StringComparison.Ordinal) &&
                !moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "RequestMoreEquipmentSlotsNativeSave(",
                    StringComparison.Ordinal) &&
                !moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "SaveSaving",
                    StringComparison.Ordinal),
                "The MoreEquipmentSlots NoNativeSave G5 case must prove real ProductNative replacement, shield damage/break and unequip stay Working-only against an unchanged durable document without entering the native-save route.");
            Assert(
                fixtureSource.Contains(
                    "case \"MoreEquipmentSlotsNoNativeSaveColdObserver\"",
                    StringComparison.Ordinal) &&
                participantSource.Contains(
                    "ConfigureMoreEquipmentSlotsNoNativeSaveColdObserver",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "TryObserveMoreEquipmentSlotsNoNativeSaveColdForFixture",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "workingMatchesCommitted=true",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "workingDirty=false",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "journal=false",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "candidate=false",
                    StringComparison.Ordinal) &&
                moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "ExactOwnerPatchCount != 4",
                    StringComparison.Ordinal) &&
                !moreEquipmentSlotsNoNativeSaveFixtureSource.Contains(
                    "RequestMoreEquipmentSlotsNativeSave(",
                    StringComparison.Ordinal),
                "The second-process MoreEquipmentSlots NoNativeSave observer must compare the first-process baseline read-only after a cold load and require the exact ProductNative owner.");
            Assert(!contentSource.Contains("RegisterSlots(", StringComparison.Ordinal) &&
                !contentSource.Contains("EquipExtraSlot(", StringComparison.Ordinal) &&
                !contentSource.Contains("UnequipExtraSlot(", StringComparison.Ordinal) &&
                !contentSource.Contains("DTMAPI.QA.G5.EquipmentSlots", StringComparison.Ordinal) &&
                contentSource.Contains("migrated-to-dedicated-MoreEquipmentSlots-ProductNative-fixture", StringComparison.Ordinal) &&
                g4FixtureSource.Contains("ObserveEquipmentSlotsUiForFixture(string screenshotPath, string summaryPath)", StringComparison.Ordinal) &&
                g4FixtureSource.Contains("ReadMoreEquipmentSlotsProductObservation()", StringComparison.Ordinal) &&
                g4FixtureSource.Contains("DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks", StringComparison.Ordinal) &&
                g4FixtureSource.Contains("EVIDENCE_CAPTURED_WAITING_ESCAPE", StringComparison.Ordinal) &&
                controllerSource.Contains("ObserveEquipmentSlotsUiForFixture(GetEvidencePath(\"g4/ui\", \"equipment-slots.png\")", StringComparison.Ordinal) &&
                settingsSource.Contains("QA G4 equipment-slot UI observation and G5 NewContent must run as separate participant transactions", StringComparison.Ordinal),
                "The dedicated MoreEquipmentSlots product fixture must own G5 behavior while the independent G4 route observes ProductNative UI without a second frozen-ABI consumer.");
            Assert(!equipmentProductSource.Contains("ForFixture", StringComparison.Ordinal) &&
                !equipmentProductSource.Contains("Smoke.", StringComparison.Ordinal) &&
                !equipmentProductSource.Contains("EQUIPMENT-SLOTS-UI", StringComparison.Ordinal) &&
                equipmentProductSource.Contains("MoreEquipmentSlotsCallbacks", StringComparison.Ordinal) &&
                equipmentProductSource.Contains("RequestUnequip", StringComparison.Ordinal) &&
                equipmentCompatibilitySource.Contains("ClampInt(options.ExtraAttributeSlots, 0, 24)", StringComparison.Ordinal) &&
                !equipmentCompatibilitySource.Contains("Smoke.NewContentEquipmentSlots", StringComparison.Ordinal) &&
                gameBridgeProjectSource.Contains(@"Compile Remove=""Compatibility\EquipmentSlots\EquipmentSlotsCompatibilityService.cs""", StringComparison.Ordinal) &&
                gameBridgeProjectSource.Contains(@"Compile Remove=""Compatibility\EquipmentSlots\EquipmentSlotsCompatibilityHooks.cs""", StringComparison.Ordinal) &&
                typeof(DolocTownGameBridge).Assembly.GetType("DTMAPI.GameBridge.DolocTown.EquipmentSlotsCompatibilityService", throwOnError: false) == null,
                "EquipmentSlots production behavior must live in ProductNative, frozen 0..24 execution must compile only into Compatibility Host, and mandatory GameBridge metadata must retain no heavy executor type.");
            Assert(!actionSpeedProductionSource.Contains("SuppressActionSpeedAutoFillForFixture", StringComparison.Ordinal) &&
                !mineProductNativeSource.Contains("ForceMachineProductionDueForFixture", StringComparison.Ordinal) &&
                !mineProductNativeSource.Contains("forcePoll", StringComparison.Ordinal) &&
                !contentSource.Contains("IMachineProductionApi", StringComparison.Ordinal) &&
                !mandatoryGameBridgeProductionSource.Contains("VerifyAdvancedCreativeHooksForFixture", StringComparison.Ordinal) &&
                debugSource.Contains("private string VerifyAdvancedCreativeHooksForFixture()", StringComparison.Ordinal) &&
                !bridgeProductionSource.Contains("TryOpenTitleSettingsForQa", StringComparison.Ordinal) &&
                !titleUiSource.Contains("ClickTitleButtonForFixture", StringComparison.Ordinal) &&
                !coreRuntimeSource.Contains("RecordSyntheticInputFrameForFixture", StringComparison.Ordinal) &&
                !coreRuntimeSource.Contains("RecordSyntheticInputTapForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("SuppressFishingAutoCastForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("ForceFishingNoWaterForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("ForceFishingNoRodForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("ForceFishingFishForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("ForceFishingNativeBiteForFixture", StringComparison.Ordinal) &&
                !legacyFishingProductionSource.Contains("FishingPoolOverrideForFixture", StringComparison.Ordinal),
                "G9 must delete orphan production fixture mutators and keep the remaining active QA probes inside the optional assembly.");
            Assert(!actionSpeedSource.Contains("Thread.Sleep", StringComparison.Ordinal) &&
                !contentSource.Contains("Thread.Sleep", StringComparison.Ordinal) &&
                actionSpeedSource.Contains("actionSpeedAutoFillFixtureActive", StringComparison.Ordinal) &&
                actionSpeedSource.Contains("actionSpeedAutoFillQuickSlotReceiptPublished", StringComparison.Ordinal) &&
                actionSpeedSource.Contains("!RestoreSmokeQuickSlot", StringComparison.Ordinal) &&
                fixtureSupportSource.Contains("publishRestoreReceipt?.Invoke(inventory, originalSlotItem);", StringComparison.Ordinal) &&
                fixtureSupportSource.Contains("object? restored = read.Invoke", StringComparison.Ordinal) &&
                actionSpeedSource.Contains("return FixtureAttemptResult.Pending;", StringComparison.Ordinal) &&
                contentSource.Contains("newContentPendingMine", StringComparison.Ordinal) &&
                contentSource.Contains("newContentPendingMineStage", StringComparison.Ordinal) &&
                contentSource.Contains("ObserveMineSchedulerEntryCountForFixture()", StringComparison.Ordinal) &&
                contentSource.Contains("two-Mine discovery", StringComparison.Ordinal) &&
                contentSource.Contains("block=low-power", StringComparison.Ordinal) &&
                contentSource.Contains("full-storage refusal plus independent second-Mine production", StringComparison.Ordinal) &&
                contentSource.Contains("hotDisableReenable=", StringComparison.Ordinal) &&
                contentSource.Contains("same-object move/index stability", StringComparison.Ordinal) &&
                contentSource.Contains("dismantled identity pruning", StringComparison.Ordinal) &&
                contentSource.Contains("The replacement Mine inherited stale scheduling state.", StringComparison.Ordinal) &&
                !contentSource.Contains("newContentPendingMinePreparedFrames", StringComparison.Ordinal) &&
                contentSource.Contains("ChargeTransientMineThroughNativeOwnerForFixture(", StringComparison.Ordinal) &&
                contentSource.Contains("\"ChargeToFull\"", StringComparison.Ordinal) &&
                contentSource.Contains("reviewedThreshold=10", StringComparison.Ordinal) &&
                contentSource.Contains("ObserveMineRuntimeSummaryForFixture()", StringComparison.Ordinal) &&
                contentSource.Contains("ObserveMineProductNativeOwnerForFixture()", StringComparison.Ordinal) &&
                contentSource.Contains("return FixtureAttemptResult.Pending;", StringComparison.Ordinal) &&
                contentSource.Contains("if (ReferenceEquals(remaining, equipment))", StringComparison.Ordinal) &&
                controllerSource.Contains("ActionSpeedAutoFillFixtureCleanup", StringComparison.Ordinal) &&
                controllerSource.Contains("PendingMineProductionCleanup", StringComparison.Ordinal),
                "G9 QA-only ActionSpeed and Mine ProductNative probes must wait across real Unity frames, prove low-power/full-storage/two-Mine behavior plus identity-safe move, dismantle and index reuse, publish receipts before mutation, read back restoration, and retain retryable close cleanup instead of sleeping on the frame driver.");
            Assert(participantSource.Contains("DTMAPI.QA.Protocol7.Participant", StringComparison.Ordinal) &&
                !participantSource.Contains("DTMAPI.QA.Protocol6.Participant", StringComparison.Ordinal),
                "The optional participant identity must match global QA protocol version 7.");
            Assert(runnerSource.Contains("ConfigDirectoryTransaction = 'forbidden'", StringComparison.Ordinal) &&
                runnerSource.Contains("case-local exact files only", StringComparison.Ordinal) &&
                runnerSource.Contains("CapturedBeforeQaStage = $true", StringComparison.Ordinal) &&
                runnerSource.Contains("CapturedBeforeOfficialProfile = $true", StringComparison.Ordinal) &&
                runnerSource.Contains("CapturedBeforeProductConfigWrite = $true", StringComparison.Ordinal) &&
                runnerSource.Contains("Set-SmokeRecoveryOnlyAuthorSourceState", StringComparison.Ordinal) &&
                !runnerSource.Contains("Join-Path (Join-Path $canonicalGameRoot 'Mods')", StringComparison.Ordinal) &&
                !runnerSource.Contains(".dtmapi-author-receipt.json", StringComparison.Ordinal) &&
                runnerSource.Contains("Wait-SmokeProcessExitBeforeRecovery", StringComparison.Ordinal) &&
                runnerSource.Contains("StrongPlantingGunExactConfig", StringComparison.Ordinal) &&
                !runnerSource.Contains("smoke-settings.json", StringComparison.OrdinalIgnoreCase) &&
                runnerSource.Contains("QaG5ExternalStateRestored", StringComparison.Ordinal),
                "Runner must snapshot save/sidecar metadata before staging, forbid whole-config restore and retired game-Mods deployment state, leave the deleted settings channel untouched, and restore only explicit config files after stable process exit.");

            const string runId = "abababababababababababababababab";
            QaHostSettings valid = QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 3,
                G5WorldMutationCases = new[] { "ActionSpeedTool", "MoreEquipmentSlots", "CropHarvestingApi" }
            })));
            valid.Validate(runId);
            Assert(valid.G5WorldMutationCases.SequenceEqual(new[] { "ActionSpeedTool", "MoreEquipmentSlots", "CropHarvestingApi" }),
                "A valid G5 whitelist must preserve deterministic runner order.");

            QaHostSettings validEquipmentNoSave =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NoNativeSave",
                                TitleLifecycleEnabled = true,
                                ContinuousHomePageTerminalEnabled = true,
                                ContinuousHomePageRequiresSaveLoaded = true,
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlotsNoNativeSave"
                                    }
                            })));
            validEquipmentNoSave.Validate(runId);
            Assert(
                validEquipmentNoSave.G5WorldMutationCases
                    .SequenceEqual(
                        new[]
                        {
                            "MoreEquipmentSlotsNoNativeSave"
                        }),
                "The QA contract must preserve the independent MoreEquipmentSlots NoNativeSave case.");

            QaHostSettings validEquipmentNoSaveCold =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NoNativeSave",
                                ExpectedMoreEquipmentSlotsBackpackBaseline =
                                    2,
                                ExpectedMoreEquipmentSlotsCommittedGeneration =
                                    7L,
                                ExpectedMoreEquipmentSlotsCommittedOccupied =
                                    1,
                                ExpectedMoreEquipmentSlotsCommittedSlots =
                                    "0:grandmas_button|1:|2:",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlotsNoNativeSaveColdObserver"
                                    }
                            })));
            validEquipmentNoSaveCold.Validate(runId);
            Assert(
                validEquipmentNoSaveCold
                    .ExpectedMoreEquipmentSlotsCommittedGeneration ==
                    7L &&
                validEquipmentNoSaveCold.G5WorldMutationCases
                    .SequenceEqual(
                        new[]
                        {
                            "MoreEquipmentSlotsNoNativeSaveColdObserver"
                        }),
                "The QA contract must preserve the explicit first-process expectations for the independent cold observer.");

            QaHostSettings invalidEquipmentNoSaveCold =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NoNativeSave",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlotsNoNativeSaveColdObserver"
                                    }
                            })));
            AssertThrows<InvalidDataException>(
                () => invalidEquipmentNoSaveCold.Validate(runId),
                "The cold observer must fail closed without all four first-process expected values.");

            string managedSessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT") ??
                throw new InvalidOperationException(
                    "QA Unit candidate fixtures require the managed test session root.");
            string candidateFixtureSaveRoot =
                Path.Combine(
                    managedSessionRoot,
                    "equipment-candidate-fixture",
                    "SAVE");
            Directory.CreateDirectory(
                candidateFixtureSaveRoot);
            QaHostSettings validEquipmentCandidate =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode =
                                    "NativeSaveExpected",
                                DisposableSaveFixtureSaveRoot =
                                    candidateFixtureSaveRoot,
                                RequireDisposableSaveRedirect =
                                    true,
                                MoreEquipmentSlotsInterruptedCandidateObservationEnabled =
                                    true,
                                ExpectedMoreEquipmentSlotsCandidatePreGeneration =
                                    4L,
                                ExpectedMoreEquipmentSlotsCommittedOccupied =
                                    1,
                                ExpectedMoreEquipmentSlotsCommittedSlots =
                                    "0:grandmas_button|1:|2:",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlots"
                                    }
                            })));
            validEquipmentCandidate.Validate(runId);
            Assert(
                validEquipmentCandidate
                    .ExpectedMoreEquipmentSlotsCandidatePreGeneration ==
                    4L &&
                validEquipmentCandidate
                    .RequireDisposableSaveRedirect,
                "The interrupted-candidate observation must bind exact pre-generation/Committed expectations to the disposable NativeSaveExpected route.");

            QaHostSettings invalidEquipmentCandidate =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NoNativeSave",
                                MoreEquipmentSlotsInterruptedCandidateObservationEnabled =
                                    true,
                                ExpectedMoreEquipmentSlotsCandidatePreGeneration =
                                    4L,
                                ExpectedMoreEquipmentSlotsCommittedOccupied =
                                    1,
                                ExpectedMoreEquipmentSlotsCommittedSlots =
                                    "0:grandmas_button|1:|2:",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlots"
                                    }
                            })));
            AssertThrows<InvalidDataException>(
                () => invalidEquipmentCandidate.Validate(runId),
                "The interrupted-candidate observation must reject the live NoNativeSave lane.");

            foreach ((string Phase, string SaveMode) coldPhase in
                new[]
                {
                    ("ColdPrepare", "NativeSaveExpected"),
                    ("ColdCommit", "NativeSaveExpected"),
                    ("ColdObserve", "NoNativeSave")
                })
            {
                QaHostSettings validColdTransition =
                    QaHostSettings.Read(
                        Encoding.UTF8.GetBytes(
                            JsonSerializer.Serialize(
                                new
                                {
                                    schemaVersion =
                                        QaHostProtocol.SchemaVersion,
                                    protocolVersion =
                                        QaHostProtocol.ProtocolVersion,
                                    runId,
                                    mode =
                                        QaHostProtocol.ParticipantOnlyMode,
                                    SaveSlot = 3,
                                    SaveTestMode =
                                        coldPhase.SaveMode,
                                    DisposableSaveFixtureSaveRoot =
                                        candidateFixtureSaveRoot,
                                    RequireDisposableSaveRedirect =
                                        true,
                                    TitleLifecycleEnabled = true,
                                    ContinuousHomePageTerminalEnabled =
                                        true,
                                    ContinuousHomePageRequiresSaveLoaded =
                                        true,
                                    MoreEquipmentSlotsTransitionPhase =
                                        coldPhase.Phase,
                                    G5WorldMutationCases =
                                        new[]
                                        {
                                            "MoreEquipmentSlotsTransition"
                                        }
                                })));
                validColdTransition.Validate(runId);
                Assert(
                    validColdTransition
                        .MoreEquipmentSlotsTransitionPhase ==
                        coldPhase.Phase &&
                    validColdTransition.SaveTestMode ==
                        coldPhase.SaveMode,
                    "The dedicated cold transition phase/save-mode pair drifted for " +
                    coldPhase.Phase +
                    ".");
            }

            QaHostSettings invalidColdCommitMode =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NoNativeSave",
                                DisposableSaveFixtureSaveRoot =
                                    candidateFixtureSaveRoot,
                                RequireDisposableSaveRedirect = true,
                                TitleLifecycleEnabled = true,
                                MoreEquipmentSlotsTransitionPhase =
                                    "ColdCommit",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlotsTransition"
                                    }
                            })));
            AssertThrows<InvalidDataException>(
                () => invalidColdCommitMode.Validate(runId),
                "ColdCommit must fail closed outside NativeSaveExpected.");

            QaHostSettings invalidColdObserveMode =
                QaHostSettings.Read(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                schemaVersion =
                                    QaHostProtocol.SchemaVersion,
                                protocolVersion =
                                    QaHostProtocol.ProtocolVersion,
                                runId,
                                mode =
                                    QaHostProtocol.ParticipantOnlyMode,
                                SaveSlot = 3,
                                SaveTestMode = "NativeSaveExpected",
                                DisposableSaveFixtureSaveRoot =
                                    candidateFixtureSaveRoot,
                                RequireDisposableSaveRedirect = true,
                                TitleLifecycleEnabled = true,
                                MoreEquipmentSlotsTransitionPhase =
                                    "ColdObserve",
                                G5WorldMutationCases =
                                    new[]
                                    {
                                        "MoreEquipmentSlotsTransition"
                                    }
                            })));
            AssertThrows<InvalidDataException>(
                () => invalidColdObserveMode.Validate(runId),
                "ColdObserve must fail closed outside NoNativeSave.");

            QaHostSettings invalid = QaHostSettings.Read(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode,
                SaveSlot = 3,
                G5WorldMutationCases = new[] { "UnknownMutation" }
            })));
            AssertThrows<InvalidDataException>(() => invalid.Validate(runId), "Unknown G5 case IDs must fail before activation.");

            string routing = RunQaG5RoutingSelfTest();
            Assert(routing.Contains("\"ProtocolVersion\": 7", StringComparison.OrdinalIgnoreCase) &&
                routing.Contains("ActionSpeedTool", StringComparison.Ordinal) &&
                routing.Contains("OneActionVegetation", StringComparison.Ordinal) &&
                routing.Contains("ChestLocatorEnhancer", StringComparison.Ordinal) &&
                routing.Contains("MoreEquipmentSlots", StringComparison.Ordinal) &&
                routing.Contains("\"RealWorldFallbackAllowed\": false", StringComparison.OrdinalIgnoreCase),
                "Executed G5 routing must select the requested ordered cases and prohibit real-world fallback.");
        }

        private static void
            MoreEquipmentSlotsColdObserverUsesSuppliedBaseline()
        {
            MoreEquipmentSlotsColdItemDistribution emptyCommitted =
                QaScenarioController
                    .ValidateMoreEquipmentSlotsColdItemDistribution(
                        expectedBackpackButtonCount: 2,
                        expectedCommittedSlots:
                            "0:|1:|2:",
                        backpackButtonCount: 2,
                        nativeShieldCount: 0,
                        mailButtonCount: 0,
                        mailShieldCount: 0,
                        committedButtonCount: 0,
                        committedShieldCount: 0);
            Assert(
                emptyCommitted.ExpectedButtonTotal == 2 &&
                emptyCommitted.ButtonTotal == 2 &&
                emptyCommitted.ExpectedShieldTotal == 0 &&
                emptyCommitted.ShieldTotal == 0,
                "The cold observer must accept an empty Committed document while preserving the supplied backpack-only button baseline.");

            MoreEquipmentSlotsColdItemDistribution shieldOnly =
                QaScenarioController
                    .ValidateMoreEquipmentSlotsColdItemDistribution(
                        expectedBackpackButtonCount: 0,
                        expectedCommittedSlots:
                            "0:box_hat|1:|2:",
                        backpackButtonCount: 0,
                        nativeShieldCount: 0,
                        mailButtonCount: 0,
                        mailShieldCount: 0,
                        committedButtonCount: 0,
                        committedShieldCount: 1);
            Assert(
                shieldOnly.ExpectedButtonTotal == 0 &&
                shieldOnly.ButtonTotal == 0 &&
                shieldOnly.ExpectedShieldTotal == 1 &&
                shieldOnly.ShieldTotal == 1,
                "The cold observer must accept the legal shield-only Committed baseline.");

            MoreEquipmentSlotsColdItemDistribution twoItems =
                QaScenarioController
                    .ValidateMoreEquipmentSlotsColdItemDistribution(
                        expectedBackpackButtonCount: 0,
                        expectedCommittedSlots:
                            "0:box_hat|1:grandmas_button|2:",
                        backpackButtonCount: 0,
                        nativeShieldCount: 0,
                        mailButtonCount: 0,
                        mailShieldCount: 0,
                        committedButtonCount: 1,
                        committedShieldCount: 1);
            Assert(
                twoItems.ExpectedButtonTotal == 1 &&
                twoItems.ButtonTotal == 1 &&
                twoItems.ExpectedShieldTotal == 1 &&
                twoItems.ShieldTotal == 1,
                "The cold observer must retain the accepted two-item Committed projection.");

            AssertMoreEquipmentSlotsColdMailRejected(
                mailButtonCount: 1,
                mailShieldCount: 0,
                expectedItemId: "grandmas_button");
            AssertMoreEquipmentSlotsColdMailRejected(
                mailButtonCount: 0,
                mailShieldCount: 1,
                expectedItemId: "box_hat");

            object readableEmptyMailAuthority =
                CreateMoreEquipmentSlotsMailArchive(
                    Array.Empty<object>());
            Assert(
                QaScenarioController
                    .CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                        readableEmptyMailAuthority,
                        "grandmas_button") == 0,
                "The pending-mail observer may report zero only after enumerating a readable native mail collection.");

            object readableItemMailAuthority =
                CreateMoreEquipmentSlotsMailArchive(
                    new object?[]
                    {
                        new MoreEquipmentSlotsMailEntryFixture(
                            "unrelated_template",
                            null),
                        new MoreEquipmentSlotsMailEntryFixture(
                            "send_item_template",
                            new object?[]
                            {
                                new object(),
                                new DolocTown.EmailAttachReward(
                                    true,
                                    null),
                                new DolocTown.EmailAttachReward(
                                    false,
                                    new DolocTown.RewardItem(
                                        "grandmas_button",
                                        2)),
                                new DolocTown.EmailAttachReward(
                                    false,
                                    new DolocTown.RewardItem(
                                        "box_hat",
                                        1))
                            })
                    });
            Assert(
                QaScenarioController
                    .CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                        readableItemMailAuthority,
                        "grandmas_button") == 2 &&
                QaScenarioController
                    .CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                        readableItemMailAuthority,
                        "box_hat") == 1,
                "The pending-mail reader must execute the real object graph and count readable button/shield attachments independently.");

            var unreadableMailAuthorities =
                new (string Stage, object? Archive)[]
                {
                    ("archiveHandle", null),
                    ("farmData", new object()),
                    (
                        "emailManager",
                        new
                        {
                            farmData =
                                new object()
                        }),
                    (
                        "emails-missing",
                        new
                        {
                            farmData =
                                new
                                {
                                    emailManager =
                                        new object()
                                }
                        }),
                    (
                        "emails-not-enumerable",
                        new
                        {
                            farmData =
                                new
                                {
                                    emailManager =
                                        new
                                        {
                                            emails =
                                                new object()
                                        }
                                }
                        })
                };
            foreach ((string stage, object? archive) in
                     unreadableMailAuthorities)
            {
                AssertThrows<InvalidOperationException>(
                    () =>
                        QaScenarioController
                            .CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                                archive,
                                "grandmas_button"),
                    "The pending-mail observer must fail closed when " +
                    stage +
                    " is unreadable.");
            }

            var unreadableNestedMailAuthorities =
                new (string Scenario, object Archive, string Message)[]
                {
                    (
                        "null-email",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object?[]
                            {
                                null
                            }),
                        "emails[0]"),
                    (
                        "missing-id",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new object()
                            }),
                        "emails[0].Id"),
                    (
                        "null-id",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    null,
                                    Array.Empty<object>())
                            }),
                        "emails[0].Id"),
                    (
                        "missing-attachments",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new
                                {
                                    Id =
                                        "send_item_template"
                                }
                            }),
                        "emailAttaches"),
                    (
                        "non-enumerable-attachments",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object())
                            }),
                        "emailAttaches"),
                    (
                        "null-attachment",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object?[]
                                    {
                                        null
                                    })
                            }),
                        "emailAttaches[0]"),
                    (
                        "unreadable-acceptance",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            null,
                                            null)
                                    })
                            }),
                        "isAccept"),
                    (
                        "missing-reward",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            false,
                                            null)
                                    })
                            }),
                        ".reward"),
                    (
                        "wrong-reward-type",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            false,
                                            new object())
                                    })
                            }),
                        "DolocTown.RewardItem"),
                    (
                        "missing-item-name",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            false,
                                            new DolocTown.RewardItem(
                                                null,
                                                1))
                                    })
                            }),
                        ".itemName"),
                    (
                        "missing-item-count",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            false,
                                            new DolocTown.RewardItem(
                                                "grandmas_button",
                                                null))
                                    })
                            }),
                        ".itemCount"),
                    (
                        "negative-item-count",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new object[]
                                    {
                                        new DolocTown.EmailAttachReward(
                                            false,
                                            new DolocTown.RewardItem(
                                                "grandmas_button",
                                                -1))
                                    })
                            }),
                        "non-negative integer"),
                    (
                        "throwing-emails",
                        CreateMoreEquipmentSlotsMailArchive(
                            new MoreEquipmentSlotsThrowingEnumerable()),
                        "Synthetic mail enumeration failure"),
                    (
                        "throwing-attachments",
                        CreateMoreEquipmentSlotsMailArchive(
                            new object[]
                            {
                                new MoreEquipmentSlotsMailEntryFixture(
                                    "send_item_template",
                                    new MoreEquipmentSlotsThrowingEnumerable())
                            }),
                        "Synthetic mail enumeration failure")
                };
            foreach ((
                         string scenario,
                         object archive,
                         string expectedMessage) in
                     unreadableNestedMailAuthorities)
            {
                AssertMoreEquipmentSlotsMailGraphRejected(
                    scenario,
                    archive,
                    expectedMessage);
            }
        }

        private static object CreateMoreEquipmentSlotsMailArchive(
            object emails)
        {
            return new MoreEquipmentSlotsMailArchiveFixture(
                new MoreEquipmentSlotsMailFarmDataFixture(
                    new MoreEquipmentSlotsMailManagerFixture(
                        emails)));
        }

        private static void AssertMoreEquipmentSlotsMailGraphRejected(
            string scenario,
            object archive,
            string expectedMessage)
        {
            try
            {
                QaScenarioController
                    .CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                        archive,
                        "grandmas_button");
            }
            catch (Exception ex)
            {
                Assert(
                    ex.ToString().Contains(
                        expectedMessage,
                        StringComparison.Ordinal),
                    "The nested pending-mail case '" +
                    scenario +
                    "' failed for the wrong reason. Actual=" +
                    ex);
                return;
            }

            throw new InvalidOperationException(
                "The nested pending-mail case '" +
                scenario +
                "' failed open.");
        }

        private static void AssertMoreEquipmentSlotsColdMailRejected(
            int mailButtonCount,
            int mailShieldCount,
            string expectedItemId)
        {
            try
            {
                QaScenarioController
                    .ValidateMoreEquipmentSlotsColdItemDistribution(
                        expectedBackpackButtonCount: 0,
                        expectedCommittedSlots:
                            "0:|1:|2:",
                        backpackButtonCount: 0,
                        nativeShieldCount: 0,
                        mailButtonCount:
                            mailButtonCount,
                        mailShieldCount:
                            mailShieldCount,
                        committedButtonCount: 0,
                        committedShieldCount: 0);
            }
            catch (InvalidOperationException ex)
            {
                Assert(
                    ex.Message.Contains(
                        "found pending " +
                        expectedItemId +
                        " mail",
                        StringComparison.Ordinal),
                    "The cold observer must reject pending " +
                    expectedItemId +
                    " mail before its logical-total check. Actual=" +
                    ex.Message);
                return;
            }

            throw new InvalidOperationException(
                "The cold observer accepted pending " +
                expectedItemId +
                " mail.");
        }

        private static string RunQaG5RoutingSelfTest()
        {
            string runner = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1");
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in new[]
            {
                "-NoLogo", "-NoProfile", "-File", runner,
                "-StageQaHost", "-SaveSlot", "3", "-AutoExerciseActionSpeedTool",
                "-AutoExerciseOneActionVegetation", "-AutoExerciseChestLocatorEnhancer",
                "-AutoExerciseMoreEquipmentSlots", "-ValidateQaG5RoutingOnly"
            })
                startInfo.ArgumentList.Add(argument);

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the G5 runner routing self-test.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("G5 runner routing self-test failed: " + error);
            return output;
        }

        private static void MigratedTypesExistOnlyInOptionalQaAssembly()
        {
            Assembly core = typeof(DtmApiRuntime).Assembly;
            Assembly bridge = typeof(DolocTownGameBridge).Assembly;
            Assembly qa = typeof(QaHostFactory).Assembly;
            Assembly productQaTests = typeof(FishingPerformanceProbe).Assembly;
            Assert(core.GetType("DTMAPI.Core.Diagnostics.RuntimeMemoryTrendProbe") == null &&
                core.GetType("DTMAPI.Core.Diagnostics.RuntimeThreadAllocationProbe") == null,
                "Core metadata must not retain moved diagnostic probes.");
            Assert(bridge.GetType("DTMAPI.GameBridge.DolocTown.UnityRuntimeMemoryMetricsProvider") == null &&
                bridge.GetType("DTMAPI.GameBridge.DolocTown.FishingPerformanceProbe") == null,
                "Production GameBridge metadata must not retain moved performance probes.");
            foreach (string name in new[]
            {
                "DTMAPI.GameBridge.DolocTown.QA.RuntimeMemoryTrendProbe",
                "DTMAPI.GameBridge.DolocTown.QA.RuntimeThreadAllocationProbe",
                "DTMAPI.GameBridge.DolocTown.QA.UnityRuntimeMemoryMetricsProvider"
            })
                Assert(qa.GetType(name) != null, "Optional QA metadata is missing " + name + ".");
            Assert(qa.GetType("DTMAPI.GameBridge.DolocTown.QA.FishingPerformanceProbe") == null &&
                qa.GetType("DTMAPI.GameBridge.DolocTown.QA.FishingPerformanceResult") == null &&
                productQaTests == Assembly.GetExecutingAssembly() &&
                productQaTests.GetType("DTMAPI.GameBridge.DolocTown.QA.FishingPerformanceResult") != null,
                "AutoFishing performance types must be test-linked from product QA and absent from the generic QA assembly.");
        }

        private static void AutoFishingQaSourcesAreProductOwnedAndProductionExcluded()
        {
            string root = FindRepositoryRoot();
            string productRoot = Path.Combine(root, "products", "first-party", "AutoFishing");
            string authorSettings = File.ReadAllText(Path.Combine(productRoot, "dtmapi.author.json"));
            string productProject = File.ReadAllText(Path.Combine(productRoot, "Yuuka.DTMAPI.AutoFishing.csproj"));
            string qaTestsProject = File.ReadAllText(Path.Combine(root, "tests", "DTMAPI.QaUnitTests", "DTMAPI.QaUnitTests.csproj"));
            string genericQaProject = File.ReadAllText(Path.Combine(root, "src", "DTMAPI.GameBridge.DolocTown.QA", "DTMAPI.GameBridge.DolocTown.QA.csproj"));
            Assert(authorSettings.Contains("\"sourceDirectory\": \"src\"", StringComparison.Ordinal) &&
                !productProject.Contains("qa", StringComparison.OrdinalIgnoreCase),
                "The production AutoFishing Author SDK project must compile only its declared src directory.");
            Assert(qaTestsProject.Contains("products\\first-party\\AutoFishing\\qa", StringComparison.Ordinal) &&
                genericQaProject.Contains("products\\first-party\\AutoFishing\\qa\\batch6", StringComparison.Ordinal) &&
                !genericQaProject.Contains("ProjectReference Include=\"..\\..\\products\\first-party\\AutoFishing", StringComparison.Ordinal) &&
                !genericQaProject.Contains("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal),
                "Pure historical product helpers may remain test-linked while the product-owned Batch6 pilot is source-linked into the optional QA DLL without a Product project/assembly reference.");
            string qaReadme = File.ReadAllText(Path.Combine(productRoot, "qa", "README.md"));
            Assert(Directory.EnumerateFiles(Path.Combine(productRoot, "qa"), "*.cs", SearchOption.AllDirectories).Any() &&
                qaReadme.Contains("Batch6AutoFishingPilot", StringComparison.Ordinal) &&
                qaReadme.Contains("sourceDirectory", StringComparison.Ordinal) &&
                qaReadme.Contains("CompatibilityNativeControl", StringComparison.Ordinal) &&
                qaReadme.Contains("AUTO-FISHING-PERF", StringComparison.Ordinal),
                "Product QA authority must document the live linked Batch6 pilot, its production exclusion, independent L0 driver, and exact evidence root.");
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "PROJECT.md")) && Directory.Exists(Path.Combine(directory.FullName, "tools")))
                    return directory.FullName;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Could not find the DTMAPI repository root.");
        }

        private static int CountTextOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static string SliceBetween(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = start < 0 ? -1 : source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
            if (start < 0 || end <= start)
                throw new InvalidOperationException("Could not locate source slice from '" + startMarker + "' to '" + endMarker + "'.");
            return source.Substring(start, end - start);
        }

        private static void SetPrivateBooleanProperty(object instance, string propertyName, bool value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Property " + propertyName + " is unavailable.");
            property.SetValue(instance, value);
        }

        private static void AssertThrows<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class FakeBatch6Runtime
        {
            private readonly Dictionary<string, object> modInstances = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            internal FakeBatch6Runtime()
            {
            }

            internal FakeBatch6Runtime(string uniqueId, FakeBatch6AutoFishingEntry entry)
            {
                modInstances.Add(uniqueId, entry);
            }
        }

        private sealed class FakeBatch6AutoFishingEntry
        {
            private readonly bool enabled;
            private readonly bool updateSubscribed;
            private readonly bool toggleAwaitingRelease;
            private readonly FakeBatch6Session? session;
            private readonly FakeBatch6NativeRuntime nativeRuntime;
            private readonly FakeBatch6Primitives primitives;
            private readonly FakeBatch6Config config;
            private readonly string lastReason = "unit-active";

            internal FakeBatch6AutoFishingEntry(bool enabled, bool toggleAwaitingRelease = false)
            {
                this.enabled = enabled;
                updateSubscribed = enabled;
                this.toggleAwaitingRelease = toggleAwaitingRelease;
                session = enabled ? new FakeBatch6Session(isReleased: false) : null;
                nativeRuntime = new FakeBatch6NativeRuntime();
                primitives = new FakeBatch6Primitives();
                config = new FakeBatch6Config();
            }

            internal string LastReasonForCompiler => lastReason;
            internal int NativeRefreshCount => primitives.NativeStateCache.RefreshCount;
            internal int NativeClearCount => primitives.NativeStateCache.ClearCount;
        }

        private sealed class FakeBatch6Session
        {
            internal FakeBatch6Session(bool isReleased)
            {
                IsReleased = isReleased;
            }

            internal bool IsReleased { get; }
            internal FakeBatch6Snapshot GetSnapshot() => new FakeBatch6Snapshot();
        }

        private sealed class FakeBatch6Snapshot
        {
            internal string Phase => "WaitPlayable";
        }

        private sealed class FakeBatch6NativeRuntime
        {
            internal int InstalledPatchCount => 17;
        }

        private sealed class FakeBatch6Primitives
        {
            private readonly FakeBatch6Diagnostics diagnostics = new FakeBatch6Diagnostics();
            private readonly FakeBatch6HookRuntime hookRuntime = new FakeBatch6HookRuntime();
            private readonly FakeBatch6NativeStateCache nativeStateCache = new FakeBatch6NativeStateCache();
            internal FakeBatch6Diagnostics EnableQaObservation() => diagnostics;
            internal FakeBatch6NativeStateCache NativeStateCache => nativeStateCache;
            internal int ActiveSessionCount => 1;
            internal int ActiveInputLeaseCount => 1;
            internal int ActiveAnimationLeaseCount => 1;
            internal bool SchedulerPending => true;
            internal int NativeAccessorBuildCount => 4;
            internal int NativeAccessorFailureCount => 0;
            internal long NativeFrameRefreshCount => 120;
        }

        private sealed class FakeBatch6NativeStateCache
        {
            internal int RefreshCount { get; private set; }
            internal int ClearCount { get; private set; }

            internal FakeBatch6NativeAvailability RefreshFrame()
            {
                RefreshCount++;
                return new FakeBatch6NativeAvailability();
            }

            internal void ClearRuntimeReferences()
            {
                ClearCount++;
            }
        }

        private sealed class FakeBatch6NativeAvailability
        {
            internal bool NativeMovementAvailable => true;
            internal double NativeInputMultiplier => 0.75d;
            internal double NativeVelocityX => 0.125d;
            internal double NativeOffsetX => -0.25d;
        }

        private sealed class FakeBatch6HookRuntime
        {
            private readonly FakeBatch6InputOverride inputOverride = new FakeBatch6InputOverride();
            private readonly FakeBatch6VisibleReelInput visibleReelInput = new FakeBatch6VisibleReelInput();
            private readonly HashSet<object> readyTargets = new HashSet<object> { new object(), new object() };
            private readonly HashSet<object> readyReleased = new HashSet<object> { new object() };
            private readonly Dictionary<object, double> animatorSpeeds = new Dictionary<object, double> { [new object()] = 1d };
            private readonly Dictionary<object, double> hookGravityScales = new Dictionary<object, double> { [new object()] = 1d };
            private readonly Dictionary<object, object> hookVelocities = new Dictionary<object, object> { [new object()] = new object() };
            private readonly Dictionary<object, double> pullDurations = new Dictionary<object, double> { [new object()] = 1d };
            private readonly object currentReadyState = new object();
        }

        private sealed class FakeBatch6InputOverride
        {
            internal bool IsActive => true;
        }

        private sealed class FakeBatch6VisibleReelInput
        {
            internal bool IsPending => true;
        }

        private sealed class FakeBatch6Diagnostics
        {
            internal long TransitionCount => 44;
            internal long PullEnteredCount => 13;
            internal long PullExitedCount => 12;
            internal long NativeVisibleReelCount => 10;
            internal long NativeSkipReelCount => 2;
            internal long CastAppliedCount => 14;
            internal long NativeBitePreparedCount => 6;
            internal long InstantBiteCommittedCount => 5;
            internal long VisibleReelQueued => 11;
            internal long VisibleReelConsumed => 10;
            internal long VisibleReelNativeAccepted => 9;
            internal long VisibleReelRetries => 2;
            internal long VisibleReelTimeouts => 1;
            internal int AnimationApplicationCount => 8;
            internal int ReadyChargeApplicationCount => 3;
        }

        private sealed class FakeBatch6Config
        {
            internal string ToggleKey => "F6";
            internal bool InstantBite => true;
            internal bool SkipMiniGame => true;
            internal bool FastAnimations => true;
            internal double AnimationMultiplier => 3d;
            internal double CastChargeRatio => 0.5d;
        }

        private sealed class RetryableCameraViewApi : ICameraViewApi
        {
            private readonly List<RetryableCameraViewLease> leases = new List<RetryableCameraViewLease>();
            private bool failNextLowRelease;

            internal RetryableCameraViewApi(bool failFirstLowRelease = true)
            {
                failNextLowRelease = failFirstLowRelease;
            }

            internal int LiveLeaseCount => leases.Count(item => !item.IsReleased);

            internal int AcquisitionCount => leases.Count;

            internal int ReleaseAttempts { get; private set; }

            public ICameraViewLease AcquireLease(IManifest owner, CameraViewRequest request)
            {
                var lease = new RetryableCameraViewLease(this, owner.UniqueID, request, leases.Count + 1);
                leases.Add(lease);
                return lease;
            }

            public CameraViewState GetState(string uniqueId) => BuildState();

            public CameraViewState GetSnapshot(string uniqueId) => BuildState();

            public BridgeFeatureStatus GetStatus(string uniqueId) => new BridgeFeatureStatus("configured", "qa-unit-fake");

            internal int CountOwnerRoots(string ownerId) =>
                leases.Count(item => !item.IsReleased && item.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));

            internal CameraViewResult Release(RetryableCameraViewLease lease)
            {
                ReleaseAttempts++;
                if (failNextLowRelease && lease.OwnerId.IndexOf(".Low.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    failNextLowRelease = false;
                    throw new InvalidOperationException("injected low-lease release failure");
                }
                lease.MarkReleased();
                return ResultFor(lease, true);
            }

            internal CameraViewState BuildState()
            {
                RetryableCameraViewLease? active = leases
                    .Where(item => !item.IsReleased && item.Request.Enabled)
                    .OrderByDescending(item => item.Request.Priority)
                    .ThenByDescending(item => item.Sequence)
                    .FirstOrDefault();
                return new CameraViewState
                {
                    ActiveOwnerId = active?.OwnerId ?? string.Empty,
                    ActiveLeaseId = active?.LeaseId ?? string.Empty,
                    AppliedViewScale = active?.Request.ViewScale ?? 1d,
                    CurrentViewScale = active?.Request.ViewScale ?? 1d,
                    LeaseCount = LiveLeaseCount
                };
            }

            internal static CameraViewResult ResultFor(RetryableCameraViewLease lease, bool success) => new CameraViewResult
            {
                Success = success,
                OwnerId = lease.OwnerId,
                LeaseId = lease.LeaseId,
                RequestedViewScale = lease.Request.ViewScale,
                AppliedViewScale = lease.Request.ViewScale
            };
        }

        private sealed class RetryableCameraViewLease : ICameraViewLease
        {
            private readonly RetryableCameraViewApi api;

            internal RetryableCameraViewLease(RetryableCameraViewApi api, string ownerId, CameraViewRequest request, int sequence)
            {
                this.api = api;
                OwnerId = ownerId;
                Request = request;
                Sequence = sequence;
                LeaseId = "lease-" + sequence;
                LastResult = RetryableCameraViewApi.ResultFor(this, true);
            }

            public string LeaseId { get; }

            public string OwnerId { get; }

            public bool IsReleased { get; private set; }

            public CameraViewResult LastResult { get; private set; }

            internal CameraViewRequest Request { get; private set; }

            internal int Sequence { get; }

            public CameraViewResult SetViewScale(double viewScale, string reason)
            {
                Request.ViewScale = viewScale;
                return LastResult = RetryableCameraViewApi.ResultFor(this, true);
            }

            public CameraViewResult Update(CameraViewRequest request, string reason)
            {
                Request = request;
                return LastResult = RetryableCameraViewApi.ResultFor(this, true);
            }

            public CameraViewResult Release(string reason) => LastResult = api.Release(this);

            public CameraViewState GetState() => api.BuildState();

            public void Dispose()
            {
                if (!IsReleased)
                    Release("dispose");
            }

            internal void MarkReleased()
            {
                IsReleased = true;
            }
        }

        private abstract class FakeUiStateBase
        {
        }

        private sealed class FakeOwnedUiState : FakeUiStateBase
        {
        }

        private sealed class FakeGenericUiManager
        {
            internal Type? RemovedType { get; private set; }

            public void RemoveUI<T>() where T : FakeUiStateBase
            {
                RemovedType = typeof(T);
            }
        }

        private sealed class FakeNonGenericUiManager
        {
            public void RemoveUI()
            {
            }
        }

        private static class FakeNativeSurfaceDolocApi
        {
            public static FakeNativeSurfaceAgent agent { get; } = new FakeNativeSurfaceAgent();
            public static FakeNativeSurfaceRoom CurrentRoom { get; } = new FakeNativeSurfaceRoom();
            public static FakeNativeSurfaceVector AgentPosition { get; } = new FakeNativeSurfaceVector(10d, 20d, 0d);
            public static FakeNativeSurfaceCell AgentRoomCellPosition { get; } = new FakeNativeSurfaceCell(6, 7);
        }

        private sealed class FakeNativeSurfaceAgent
        {
            private readonly FakeNativeSurfaceGroundChecker groundChecker = new FakeNativeSurfaceGroundChecker();

            public FakeNativeSurfaceStatus Status { get; } = new FakeNativeSurfaceStatus();
            public FakeNativeSurfaceStateManager StateManager { get; } = new FakeNativeSurfaceStateManager();
            public bool IsFaceRight => true;
        }

        private sealed class FakeNativeSurfaceRoom
        {
            public string RoomId => "unit-room";
            public string SceneRawName => "unit-scene";
        }

        private sealed class FakeNativeSurfaceStateManager
        {
            public readonly FakeNativeSurfaceState current = new FakeNativeSurfaceState();
        }

        private sealed class FakeNativeSurfaceState
        {
        }

        private sealed class FakeNativeSurfaceStatus
        {
            public FakeNativeSurfaceMoveModifier MoveModifier = new FakeNativeSurfaceMoveModifier();
            public double VelocityX => -1.6153926480910741E-06;
            public double VelocityY => 2d;
            public bool IsTouchWall => true;
            public bool IsNearWallTop => false;
        }

        private sealed class FakeNativeSurfaceMoveModifier
        {
            public double inputMultiplier = 0d;
            public FakeNativeSurfaceVector conveyorOffset = new FakeNativeSurfaceVector(-1.6153926480910741E-06, 2d, 0d);
        }

        private sealed class FakeNativeSurfaceGroundChecker
        {
            public bool isTouched => true;
            public bool isTouchedExcludePlatforms => false;
            public int PlatformCount => 1;
        }

        private sealed class FakeNativeSurfacePlatform
        {
            private readonly bool isTouched;
            private readonly bool isStay;
            private readonly FakeNativeSurfaceGroup group;
            private readonly int instanceId;

            internal FakeNativeSurfacePlatform(
                int instanceId,
                FakeNativeSurfaceVector position,
                double groupSpeedX,
                double groupSpeedY,
                bool touched,
                bool stay,
                FakeNativeSurfaceCollider? otherCollider)
            {
                this.instanceId = instanceId;
                isTouched = touched;
                isStay = stay;
                group = new FakeNativeSurfaceGroup(groupSpeedX, groupSpeedY);
                transform = new FakeNativeSurfaceTransform(position);
                currentOtherCollider = otherCollider;
            }

            public FakeNativeSurfaceGameObject gameObject { get; } = new FakeNativeSurfaceGameObject("unit-platform", true);
            public FakeNativeSurfaceTransform transform { get; }
            public FakeNativeSurfaceCollider? currentOtherCollider { get; }
            public FakeNativeSurfaceCollision? currentCollision => null;
            public int GetInstanceID() => instanceId;
        }

        private sealed class FakeNativeSurfaceGroup
        {
            private readonly double moveSpeed = 2d;
            private readonly FakeNativeSurfaceVector moveDir;

            internal FakeNativeSurfaceGroup(double speedX, double speedY)
            {
                SpeedX = speedX;
                SpeedY = speedY;
                moveDir = new FakeNativeSurfaceVector(speedX / moveSpeed, speedY / moveSpeed, 0d);
            }

            public double SpeedX { get; }
            public double SpeedY { get; }
        }

        private sealed class FakeNativeSurfaceCollider
        {
            private readonly int instanceId;

            internal FakeNativeSurfaceCollider(int instanceId, string gameObjectName)
            {
                this.instanceId = instanceId;
                gameObject = new FakeNativeSurfaceGameObject(gameObjectName, true);
            }

            public FakeNativeSurfaceGameObject gameObject { get; }
            public int GetInstanceID() => instanceId;
        }

        private sealed class FakeNativeSurfaceCollision
        {
            public FakeNativeSurfaceGameObject gameObject { get; } = new FakeNativeSurfaceGameObject("ground", true);
        }

        private sealed class FakeNativeSurfaceGameObject
        {
            internal FakeNativeSurfaceGameObject(string name, bool activeInHierarchy)
            {
                this.name = name;
                this.activeInHierarchy = activeInHierarchy;
            }

            public string name { get; }
            public bool activeInHierarchy { get; }
        }

        private sealed class FakeNativeSurfaceTransform
        {
            internal FakeNativeSurfaceTransform(FakeNativeSurfaceVector position)
            {
                this.position = position;
            }

            public FakeNativeSurfaceVector position { get; }
        }

        private sealed class FakeNativeSurfaceVector
        {
            internal FakeNativeSurfaceVector(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public double x;
            public double y;
            public double z;
        }

        private sealed class FakeNativeSurfaceCell
        {
            internal FakeNativeSurfaceCell(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public int x;
            public int y;
        }

        private static class FakeDolocApi
        {
            private static float energyPercent;
            private static float spiritPercent;
            private static int fishingEnergyCost;

            public static int ComposeEnergyCalls { get; private set; }
            public static int ComposeSpiritCalls { get; private set; }
            public static FakeGlobalParameter GlobalParameter { get; } = new FakeGlobalParameter();

            public static void Reset(float energyPercent, float spiritPercent, int fishingEnergyCost)
            {
                ComposeEnergyCalls = 0;
                ComposeSpiritCalls = 0;
                FakeDolocApi.energyPercent = energyPercent;
                FakeDolocApi.spiritPercent = spiritPercent;
                FakeDolocApi.fishingEnergyCost = fishingEnergyCost;
                GlobalParameter.FishingEnergyCost = fishingEnergyCost;
            }

            public static void SetVitals(float energyPercent, float spiritPercent)
            {
                FakeDolocApi.energyPercent = energyPercent;
                FakeDolocApi.spiritPercent = spiritPercent;
            }

            public static Delegate GetCommandFunction(string commandName)
            {
                switch (commandName)
                {
                    case "compose_energy": return new Action<int>(_ => { ComposeEnergyCalls++; energyPercent = 1f; });
                    case "compose_spirit": return new Action<int>(_ => { ComposeSpiritCalls++; spiritPercent = 1f; });
                    case "get_energy_percent": return new Func<float>(() => energyPercent);
                    case "get_spirit_percent": return new Func<float>(() => spiritPercent);
                    default: throw new MissingMemberException(commandName);
                }
            }

            public static bool HasEnoughEnergy(int value) => energyPercent * 100f >= value;
        }

        private static class FakeDolocApiWithWrongSpiritCommand
        {
            public static FakeGlobalParameter GlobalParameter { get; } = new FakeGlobalParameter { FishingEnergyCost = 10 };

            public static Delegate GetCommandFunction(string commandName)
            {
                switch (commandName)
                {
                    case "compose_energy": return new Action<int>(_ => { });
                    case "compose_spirit": return new Action<string>(_ => { });
                    case "get_energy_percent": return new Func<float>(() => 1f);
                    case "get_spirit_percent": return new Func<float>(() => 1f);
                    default: throw new MissingMemberException(commandName);
                }
            }

            public static bool HasEnoughEnergy(int value) => true;
        }

        private sealed class FakeGlobalParameter
        {
            public int FishingEnergyCost { get; set; }
        }

        private static void StageCompatibilityHostFixture(string gamePath)
        {
            const string fileName =
                "DTMAPI.GameBridge.DolocTown.Compatibility.dll";
            string source = Path.Combine(
                AppContext.BaseDirectory,
                "CompatibilityHostFixture",
                fileName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    "The QA Unit build did not stage the Compatibility Host fixture.",
                    source);
            }

            string componentDirectory = Path.Combine(
                gamePath,
                "DTMAPI",
                "components",
                "compatibility");
            Directory.CreateDirectory(componentDirectory);
            string target = Path.Combine(
                componentDirectory,
                fileName);
            File.Copy(source, target, overwrite: true);

            string sha256;
            using (FileStream stream = File.OpenRead(target))
            using (SHA256 hash = SHA256.Create())
            {
                sha256 =
                    BitConverter.ToString(
                        hash.ComputeHash(stream))
                        .Replace("-", string.Empty);
            }
            var info = new FileInfo(target);
            var receipt = new
            {
                OptionalComponents = new[]
                {
                    new
                    {
                        ComponentId =
                            "gamebridge-compatibility-host",
                        Distribution = "dormant-shipped",
                        LoadPolicy = "first-frozen-abi-call",
                        RelativePath =
                            "DTMAPI/components/compatibility/" +
                            fileName,
                        Length = info.Length,
                        Sha256 = sha256,
                        AssemblyName =
                            "DTMAPI.GameBridge.DolocTown.Compatibility",
                        AssemblyVersion =
                            AssemblyName.GetAssemblyName(target)
                                .Version?.ToString() ??
                            string.Empty,
                        FileVersion =
                            FileVersionInfo.GetVersionInfo(target)
                                .FileVersion ??
                            string.Empty,
                        TargetFramework = "netstandard2.0",
                        DefaultLoadState = "dormant",
                        IncludedInDownloadPackage = true
                    }
                }
            };
            File.WriteAllText(
                Path.Combine(
                    gamePath,
                    "DTMAPI",
                    "release-manifest.json"),
                JsonSerializer.Serialize(receipt),
                new UTF8Encoding(false));
        }

        private sealed class FakeHost : IRuntimeHost
        {
            internal FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "QaUnitTest";
            public List<string> Logs { get; } = new List<string>();
            public void Log(string message) => Logs.Add(message ?? string.Empty);
            public void LogWarning(string message) => Logs.Add(message ?? string.Empty);
            public void LogError(string message, Exception? exception = null) => Logs.Add(message ?? string.Empty);
        }
    }
}
