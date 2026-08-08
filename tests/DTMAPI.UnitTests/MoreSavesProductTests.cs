using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.MoreSaves;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class MoreSavesProductTests
    {
        internal static void RunAll()
        {
            EnabledProductOwnsOneWriterAndInstallsZeroHooks();
            MissingManagerRetriesAtBoundedCadence();
            DisableRestoresSixBeforeReleasingOwner();
            FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup();
            CompatibilityFirstProductActivationFailsClosed();
            NoLegacyArchivesAreSuccessfulNoOp();
            MissingSaveDirectoryIsSuccessfulNoOp();
            IndependentLegacyRolesMoveToCurrent100Names();
            MismatchedBackupContentMovesByOfficialRole();
            ExistingDestinationsNeverOverwriteOrDeleteLegacySources();
            PartialMoveFailureKeepsCompletedMovesAndResumesIdempotently();
            ReportedFailureAfterActualMoveDoesNotPublishAndReplayCompletes();
            MovePostconditionFailureRejects();
            NativePathsMustShareOneSaveRoot();
            MigrationFailureKeepsNativeSixAndReleasesOwner();
            DeferredUpdateMigrationFailureReleasesOwner();
            DeferredBoundaryMigrationFailureReleasesOwner();
            NativeFormatMismatchRejectsBeforeMigration();
            NativePathMismatchRejectsBeforeFirstMove();
            NativeReflectionMigrationUsesCurrentLocalSaveOwner();
        }

        private static void EnabledProductOwnsOneWriterAndInstallsZeroHooks()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                var retryDemand = new List<bool>();
                int migrationCalls = 0;
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    retryDemandChanged: retryDemand.Add,
                    archiveMigration: value =>
                    {
                        migrationCalls++;
                        Assert(((ManagerFixture)value).archiveFileCount == 6,
                            "Legacy migration must finish before twelve is published to the native UI owner.");
                        return MoreSavesArchiveMigrationResult.NoLegacyFiles();
                    });
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");

                Assert(manager.archiveFileCount == 12, "Enabled MoreSaves must write the fixed twelve-slot contract.");
                Assert(runtime.LeaseHeld && MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.ProductOwner),
                    "Enabled MoreSaves must hold the one ProductNative archive-count lease.");
                Assert(runtime.InstalledPatchCount == 0, "MoreSaves must install zero Harmony patches.");
                Assert(migrationCalls == 1, "Healthy activation must run the one startup migration exactly once.");
                Assert(!runtime.RetrySchedulingRequested && retryDemand.Count == 0,
                    "Healthy MoreSaves must publish no frame-retry subscription demand.");
                Assert(runtime.StatusSummary.Contains("pendingWork=false") &&
                       runtime.StatusSummary.Contains("retryScheduled=false"),
                    "Healthy status must distinguish absent pending work from absent retry scheduling.");

                runtime.ResetBoundary("unit title");
                Assert(manager.archiveFileCount == 12 && !runtime.RetryPending &&
                       !runtime.RetrySchedulingRequested && retryDemand.Count == 0 && migrationCalls == 1,
                    "Title/save lifecycle reconciliation must preserve twelve without repeating startup migration or creating frame-retry demand.");
                runtime.DeactivateOwner("unit cleanup");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void MissingManagerRetriesAtBoundedCadence()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                ManagerFixture? manager = null;
                DateTimeOffset now = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
                var retryDemand = new List<bool>();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    () => now,
                    retryDemand.Add,
                    NoMigration);

                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit missing");
                Assert(runtime.RetryPending && runtime.LeaseHeld && runtime.RetrySchedulingRequested &&
                       retryDemand.Count == 1 && retryDemand[0],
                    "Missing manager must retain one lease and demand-activate one bounded retry subscription.");
                Assert(runtime.StatusSummary.Contains("pendingWork=true") &&
                       runtime.StatusSummary.Contains("retryScheduled=true"),
                    "Active missing-manager status must report both pending work and its live retry scheduler.");
                manager = new ManagerFixture();
                runtime.Update();
                Assert(manager.archiveFileCount == 6, "Retry must not run before the bounded 750ms cadence.");
                now = now.AddMilliseconds(749);
                runtime.Update();
                Assert(manager.archiveFileCount == 6, "Retry must remain dormant before 750ms.");
                now = now.AddMilliseconds(1);
                runtime.Update();
                Assert(manager.archiveFileCount == 12 && !runtime.RetryPending && !runtime.RetrySchedulingRequested &&
                       retryDemand.Count == 2 && !retryDemand[1],
                    "The first due retry must apply twelve, clear pending work and remove the frame subscription.");
                runtime.DeactivateOwner("unit cleanup");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void DisableRestoresSixBeforeReleasingOwner()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                var runtime = Runtime(manager);
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");
                runtime.Configure(new MoreSavesConfig { Enabled = false }, "unit disabled");

                Assert(manager.archiveFileCount == 6, "Disabling MoreSaves must restore the native six-slot count.");
                Assert(!runtime.LeaseHeld && MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "The product lease must release only after native six is observed.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                DateTimeOffset now = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
                bool allowRestore = false;
                var retryDemand = new List<bool>();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) =>
                    {
                        if (count == 6 && !allowRestore)
                            return false;
                        ((ManagerFixture)value).archiveFileCount = count;
                        return true;
                    },
                    () => now,
                    retryDemand.Add,
                    NoMigration);
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");

                bool failed = false;
                try { runtime.DeactivateOwner("unit failing restore"); }
                catch (InvalidOperationException) { failed = true; }
                Assert(failed && manager.archiveFileCount == 12 && runtime.LeaseHeld && runtime.RetryPending,
                    "A failed final restore must fail closed while retaining the exact product lease.");
                Assert(MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.ProductOwner),
                    "A failed restore must not expose the native writer to a second owner.");
                Assert(!runtime.RetrySchedulingRequested && retryDemand.Count == 0,
                    "Final Loader deactivation must not claim an automatic retry after owner event delivery is removed.");
                Assert(runtime.StatusSummary.Contains("pendingWork=true") &&
                       runtime.StatusSummary.Contains("retryScheduled=false"),
                    "Failed final cleanup status must retain pending-work evidence without claiming an automatic scheduler.");

                allowRestore = true;
                now = now.AddMilliseconds(750);
                runtime.DeactivateOwner("unit later explicit cleanup");
                Assert(manager.archiveFileCount == 6 && !runtime.LeaseHeld && !runtime.RetryPending &&
                    MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "A later explicit cleanup pass must observe six before releasing the retained product lease.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void CompatibilityFirstProductActivationFailsClosed()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                Assert(MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, out _),
                    "Compatibility fixture should acquire the one native writer.");
                var manager = new ManagerFixture();
                var runtime = Runtime(manager);
                bool rejected = false;
                try { runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit compatibility-first"); }
                catch (InvalidOperationException) { rejected = true; }

                Assert(rejected && manager.archiveFileCount == 6 && !runtime.LeaseHeld,
                    "Compatibility-first ProductNative activation must reject before any native write or product retention.");
                Assert(MoreSavesNativeOwnerCoordinator.ReleaseAfterNativeRestore(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, true),
                    "Compatibility fixture should release after its native-six proof.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void NoLegacyArchivesAreSuccessfulNoOp()
        {
            string root = NewMigrationRoot("no-legacy");
            File.WriteAllText(CurrentPath(root, 6), "an unrelated current-format archive that the no-op path must not parse");

            MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));

            Assert(result.IsNoOp && result.LegacySourceCount == 0 && result.MovedFileCount == 0,
                "Players with no old expanded archives must receive a successful, content-independent no-op.");
            Assert(File.ReadAllText(CurrentPath(root, 6)).StartsWith("an unrelated", StringComparison.Ordinal),
                "The no-op migration must not touch current-format archives.");
        }

        private static void MissingSaveDirectoryIsSuccessfulNoOp()
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "moresaves-missing-root-" + Guid.NewGuid().ToString("N"));
            Assert(!Directory.Exists(root), "Missing-root fixture must begin without a SAVE directory.");

            MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));

            Assert(result.IsNoOp && !Directory.Exists(root),
                "A new player with no SAVE directory and no legacy files must receive a non-creating successful no-op.");
        }

        private static void IndependentLegacyRolesMoveToCurrent100Names()
        {
            string root = NewMigrationRoot("independent-roles");
            for (int index = 6; index <= 11; index++)
            {
                File.WriteAllText(LegacyCurrentPath(root, index), "opaque-current-" + index);
                File.WriteAllText(LegacyPrevPath(root, index), "opaque-prev-" + index);
                File.WriteAllText(LegacyBackupPath(root, index), "opaque-bak-" + index);
            }
            string outsideScope = Path.Combine(root, "ea-playtest-doloc-archive-12.data");
            File.WriteAllText(outsideScope, "outside-scope");

            MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));

            Assert(result.LegacySourceCount == 18 && result.MovedFileCount == 18 &&
                   result.PreservedDestinationCount == 0,
                "The indices 6-11 current/prev/bak roles must move as eighteen independent files.");
            for (int index = 6; index <= 11; index++)
            {
                Assert(!File.Exists(LegacyCurrentPath(root, index)) && File.Exists(CurrentPath(root, index)),
                    "Legacy current must move to the 1.00 current name for index " + index + ".");
                Assert(!File.Exists(LegacyPrevPath(root, index)) && File.Exists(CurrentPath(root, index) + ".prev0"),
                    "Legacy prev must move to the 1.00 .prev0 name for index " + index + ".");
                Assert(!File.Exists(LegacyBackupPath(root, index)) && File.Exists(CurrentPath(root, index) + ".bak"),
                    "Legacy bak must move to the 1.00 .bak name for index " + index + ".");
                Assert(File.ReadAllText(CurrentPath(root, index)) == "opaque-current-" + index &&
                       File.ReadAllText(CurrentPath(root, index) + ".prev0") == "opaque-prev-" + index &&
                       File.ReadAllText(CurrentPath(root, index) + ".bak") == "opaque-bak-" + index,
                    "File.Move must preserve each opaque fixture while changing only its official role name.");
            }
            Assert(File.ReadAllText(outsideScope) == "outside-scope",
                "Migration must not read, rename or delete indices outside the exact 6-11 scope.");
        }

        private static void MismatchedBackupContentMovesByOfficialRole()
        {
            string root = NewMigrationRoot("mismatched-backup-role");
            File.WriteAllText(LegacyCurrentPath(root, 10), "index=10;healthy-current");
            File.WriteAllText(LegacyBackupPath(root, 10), "index=3;old-backup-with-unrelated-internal-index");

            MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));

            Assert(result.LegacySourceCount == 2 && result.MovedFileCount == 2 &&
                   result.PreservedDestinationCount == 0,
                "Official filename-role migration must not inspect or reject an old backup's internal index.");
            Assert(File.ReadAllText(CurrentPath(root, 10)) == "index=10;healthy-current" &&
                   File.ReadAllText(CurrentPath(root, 10) + ".bak") == "index=3;old-backup-with-unrelated-internal-index" &&
                   !File.Exists(LegacyCurrentPath(root, 10)) && !File.Exists(LegacyBackupPath(root, 10)),
                "The healthy index-10 current and opaque old backup must move independently to their official names.");
        }

        private static void ExistingDestinationsNeverOverwriteOrDeleteLegacySources()
        {
            string root = NewMigrationRoot("destination-conflict");
            File.WriteAllText(LegacyCurrentPath(root, 6), "legacy-current");
            File.WriteAllText(CurrentPath(root, 6), "existing-current");
            File.WriteAllText(LegacyPrevPath(root, 6), "movable-prev");
            string legacyBefore = File.ReadAllText(LegacyCurrentPath(root, 6));
            string destinationBefore = File.ReadAllText(CurrentPath(root, 6));

            MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));

            Assert(result.LegacySourceCount == 2 && result.MovedFileCount == 1 &&
                   result.PreservedDestinationCount == 1,
                "An existing destination must preserve both paths without blocking another role's move.");
            Assert(File.ReadAllText(LegacyCurrentPath(root, 6)) == legacyBefore &&
                   File.ReadAllText(CurrentPath(root, 6)) == destinationBefore &&
                   !File.Exists(LegacyPrevPath(root, 6)) &&
                   File.ReadAllText(CurrentPath(root, 6) + ".prev0") == "movable-prev",
                "The existing target and its old source must stay unchanged while the pass continues.");
        }

        private static void PartialMoveFailureKeepsCompletedMovesAndResumesIdempotently()
        {
            string root = NewMigrationRoot("interrupted");
            File.WriteAllText(LegacyCurrentPath(root, 6), "current");
            File.WriteAllText(LegacyPrevPath(root, 6), "prev");
            File.WriteAllText(LegacyBackupPath(root, 6), "bak");
            int moveCalls = 0;
            bool interrupted = false;
            try
            {
                MoreSavesArchiveMigration.Execute(
                    index => CurrentPath(root, index),
                    (source, destination) =>
                    {
                        moveCalls++;
                        if (moveCalls == 2)
                            throw new IOException("Injected failure before the second move.");
                        File.Move(source, destination);
                    });
            }
            catch (MoreSavesArchiveMigrationException)
            {
                interrupted = true;
            }

            Assert(interrupted && moveCalls == 2,
                "The fault fixture must stop the activation after one completed move.");
            Assert(!File.Exists(LegacyCurrentPath(root, 6)) && File.Exists(CurrentPath(root, 6)) &&
                   File.Exists(LegacyPrevPath(root, 6)) && File.Exists(LegacyBackupPath(root, 6)),
                "Completed moves must remain in place while untouched sources retain their old names.");
            MoreSavesArchiveMigrationResult resumed = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));
            Assert(resumed.LegacySourceCount == 2 && resumed.MovedFileCount == 2,
                "A second startup must move only the two roles that still have old source names.");
            Assert(File.Exists(CurrentPath(root, 6)) && File.Exists(CurrentPath(root, 6) + ".prev0") &&
                   File.Exists(CurrentPath(root, 6) + ".bak") &&
                   !File.Exists(LegacyCurrentPath(root, 6)) && !File.Exists(LegacyPrevPath(root, 6)) &&
                   !File.Exists(LegacyBackupPath(root, 6)),
                "Interrupted current/prev/bak state must converge to the exact 1.00 family without duplication.");
            MoreSavesArchiveMigrationResult replay = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));
            Assert(replay.IsNoOp && replay.MovedFileCount == 0,
                "A fully migrated family must be an idempotent no-op on later startup.");
        }

        private static void ReportedFailureAfterActualMoveDoesNotPublishAndReplayCompletes()
        {
            string root = NewMigrationRoot("final-reported-failure");
            File.WriteAllText(LegacyCurrentPath(root, 6), "opaque-final");
            bool rejected = false;
            try
            {
                MoreSavesArchiveMigration.Execute(
                    index => CurrentPath(root, index),
                    (source, destination) =>
                    {
                        File.Move(source, destination);
                        throw new IOException("Injected error after the exact final rename.");
                    });
            }
            catch (MoreSavesArchiveMigrationException ex)
            {
                rejected = ex.Message.Contains("could not move one legacy archive role", StringComparison.Ordinal);
            }

            Assert(rejected && !File.Exists(LegacyCurrentPath(root, 6)) && File.Exists(CurrentPath(root, 6)),
                "Any reported move failure must reject the current activation even when the rename already completed.");
            MoreSavesArchiveMigrationResult replay = MoreSavesArchiveMigration.Execute(
                index => CurrentPath(root, index));
            Assert(replay.IsNoOp && replay.MovedFileCount == 0,
                "The next startup must infer the completed rename from the absent old source without a journal.");
        }

        private static void MovePostconditionFailureRejects()
        {
            string root = NewMigrationRoot("postcondition");
            File.WriteAllText(LegacyCurrentPath(root, 6), "opaque-source");
            bool rejected = false;
            try
            {
                MoreSavesArchiveMigration.Execute(
                    index => CurrentPath(root, index),
                    (source, destination) => { });
            }
            catch (MoreSavesArchiveMigrationException ex)
            {
                rejected = ex.Message.Contains("source-absent/destination-present", StringComparison.Ordinal);
            }
            Assert(rejected && File.Exists(LegacyCurrentPath(root, 6)) && !File.Exists(CurrentPath(root, 6)),
                "A move callback that returns without the official rename postcondition must fail the activation.");
        }

        private static void NativePathsMustShareOneSaveRoot()
        {
            string firstRoot = NewMigrationRoot("root-a");
            string secondRoot = NewMigrationRoot("root-b");
            File.WriteAllText(LegacyCurrentPath(firstRoot, 6), "must-not-move");
            bool rejected = false;
            try
            {
                MoreSavesArchiveMigration.Execute(
                    index => CurrentPath(index == 11 ? secondRoot : firstRoot, index));
            }
            catch (MoreSavesArchiveMigrationException ex)
            {
                rejected = ex.Message.Contains("one exact SAVE directory", StringComparison.Ordinal);
            }
            Assert(rejected && File.Exists(LegacyCurrentPath(firstRoot, 6)) && !File.Exists(CurrentPath(firstRoot, 6)),
                "All native paths must be resolved and proven to share one SAVE root before the first move.");
        }

        private static void MigrationFailureKeepsNativeSixAndReleasesOwner()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    archiveMigration: value => throw new MoreSavesArchiveMigrationException("unit migration rejection"));
                bool rejected = false;
                try
                {
                    runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit migration failure");
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }
                Assert(rejected && manager.archiveFileCount == 6 && !runtime.LeaseHeld &&
                       MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "A migration failure must keep native six visible and release the exact product owner after cleanup.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void DeferredUpdateMigrationFailureReleasesOwner()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                ManagerFixture? manager = null;
                DateTimeOffset now = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    () => now,
                    archiveMigration: value => throw new MoreSavesArchiveMigrationException("deferred update rejection"));
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit deferred missing");
                manager = new ManagerFixture();
                now = now.AddMilliseconds(750);
                bool rejected = false;
                try { runtime.Update(); }
                catch (InvalidOperationException) { rejected = true; }

                Assert(rejected && manager.archiveFileCount == 6 && !runtime.LeaseHeld && !runtime.RetryPending &&
                       MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "A migration failure on the deferred Update path must disable activation, prove native six and release the exact owner.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void DeferredBoundaryMigrationFailureReleasesOwner()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                ManagerFixture? manager = null;
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    archiveMigration: value => throw new MoreSavesArchiveMigrationException("deferred boundary rejection"));
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit boundary missing");
                manager = new ManagerFixture();
                bool rejected = false;
                try { runtime.ResetBoundary("unit deferred boundary"); }
                catch (InvalidOperationException) { rejected = true; }

                Assert(rejected && manager.archiveFileCount == 6 && !runtime.LeaseHeld && !runtime.RetryPending &&
                       MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "A migration failure on a lifecycle boundary must share the terminal native-six/owner cleanup path.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void NativeFormatMismatchRejectsBeforeMigration()
        {
            bool rejected = false;
            try
            {
                MoreSavesArchiveMigration.ExecuteNative(
                    new NativeGameManagerFixture("unexpected-save-{0}.data"));
            }
            catch (MoreSavesArchiveMigrationException ex)
            {
                rejected = ex.Message.Contains("supports the Doloc Town 1.00 archive format", StringComparison.Ordinal);
            }
            Assert(rejected,
                "MoreSaves must refuse migration when the manager's native archive format is not doloc-save-{0}.data.");
        }

        private static void NativePathMismatchRejectsBeforeFirstMove()
        {
            string root = NewMigrationRoot("native-path-mismatch");
            File.WriteAllText(LegacyCurrentPath(root, 6), "must-remain-old");
            DolocAPI.dataPersistenceManager = new NativeDataPersistenceManagerFixture(
                new NativeLocalSaveFixture(root, returnLegacyName: true));
            try
            {
                bool rejected = false;
                try { MoreSavesArchiveMigration.ExecuteNative(new NativeGameManagerFixture()); }
                catch (MoreSavesArchiveMigrationException ex)
                {
                    rejected = ex.Message.Contains("does not match the exact Doloc Town 1.00 filename", StringComparison.Ordinal);
                }
                Assert(rejected && File.Exists(LegacyCurrentPath(root, 6)) && !File.Exists(CurrentPath(root, 6)),
                    "GetDataFullPath must prove each official filename before any legacy source moves.");
            }
            finally
            {
                DolocAPI.dataPersistenceManager = null;
            }
        }

        private static void NativeReflectionMigrationUsesCurrentLocalSaveOwner()
        {
            string root = NewMigrationRoot("native-reflection");
            File.WriteAllText(LegacyCurrentPath(root, 6), "opaque-native-reflection");
            var localSave = new NativeLocalSaveFixture(root);
            DolocAPI.dataPersistenceManager = new NativeDataPersistenceManagerFixture(localSave);
            try
            {
                var manager = new NativeGameManagerFixture();
                MoreSavesArchiveMigrationResult result = MoreSavesArchiveMigration.ExecuteNative(manager);
                Assert(result.LegacySourceCount == 1 && result.MovedFileCount == 1 &&
                       File.Exists(CurrentPath(root, 6)) && !File.Exists(LegacyCurrentPath(root, 6)),
                    "Native reflection must bind DolocAPI.dataPersistenceManager.fileDataHandler and private GetDataFullPath before moving the official 1.00 role name.");
            }
            finally
            {
                DolocAPI.dataPersistenceManager = null;
            }
        }

        private static MoreSavesNativeRuntime Runtime(ManagerFixture manager) =>
            new MoreSavesNativeRuntime(
                NullMonitor.Instance,
                () => manager,
                value => ((ManagerFixture)value).archiveFileCount,
                (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                archiveMigration: NoMigration);

        private static MoreSavesArchiveMigrationResult NoMigration(object manager) =>
            MoreSavesArchiveMigrationResult.NoLegacyFiles();

        private static string NewMigrationRoot(string name)
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "moresaves-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static string CurrentPath(string root, int index) =>
            Path.Combine(root, "doloc-save-" + index + ".data");

        private static string LegacyCurrentPath(string root, int index) =>
            Path.Combine(root, "ea-playtest-doloc-archive-" + index + ".data");

        private static string LegacyPrevPath(string root, int index) =>
            Path.Combine(root, "ea-playtest-doloc-archive-" + index + "-prev.data");

        private static string LegacyBackupPath(string root, int index) =>
            Path.Combine(root, "ea-playtest-doloc-archive-" + index + "-bak.data");

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ManagerFixture
        {
            internal int archiveFileCount = 6;
        }

        private sealed class NativeGameManagerFixture
        {
            internal NativeGameManagerFixture(string archiveFileNameFormat = "doloc-save-{0}.data") =>
                this.archiveFileNameFormat = archiveFileNameFormat;

            public string archiveFileNameFormat;
        }

        private sealed class NativeDataPersistenceManagerFixture
        {
            private readonly object fileDataHandler;

            internal NativeDataPersistenceManagerFixture(object localSave) =>
                fileDataHandler = localSave;
        }

        private sealed class NativeLocalSaveFixture
        {
            private readonly string root;
            private readonly bool returnLegacyName;

            internal NativeLocalSaveFixture(string root, bool returnLegacyName = false)
            {
                this.root = root;
                this.returnLegacyName = returnLegacyName;
            }

            private string GetDataFullPath(int index) =>
                returnLegacyName ? LegacyCurrentPath(root, index) : CurrentPath(root, index);
        }
    }
}
