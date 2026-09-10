using DTMAPI.Authoring.Contracts;
using System.Text;
using System.Text.Json;

namespace DTMAPI.AuthorSdk;

internal static class DeploymentService
{
    private const int LegacyDeploymentSchemaVersion = 1;

    private static readonly string[] LegacyJournalProperties =
    {
        "active", "committed", "destinationPath", "gameRoot", "gameRootKey", "packageKind",
        "recoveryArtifacts", "schemaVersion", "status", "uniqueId"
    };

    private static readonly string[] CurrentJournalProperties =
    {
        "active", "codeModKind", "committed", "destinationPath", "gameRoot", "gameRootKey",
        "packageKind", "recoveryArtifacts", "schemaVersion", "status", "uniqueId"
    };

    private static readonly string[] CompositeJournalProperties =
    {
        "active", "codeModKind", "committed", "destinationPath", "gameRoot", "gameRootKey",
        "localInstall", "packageKind", "recoveryArtifacts", "schemaVersion", "status", "uniqueId"
    };

    private static readonly string[] LegacyReceiptProperties =
    {
        "destinationRelativePath", "gameRootKey", "packageKind", "packageSha256", "payloadDirectoryCount",
        "payloadFileCount", "payloadTreeSha256", "schemaVersion", "transactionId", "uniqueId"
    };

    private static readonly string[] CurrentReceiptProperties =
    {
        "advancedReferenceReceiptSha256", "codeModKind", "destinationRelativePath", "entryDllSha256",
        "gameRootKey", "manifestSha256", "packageKind", "packageMarkerSha256", "packageSha256",
        "payloadDirectoryCount", "payloadFileCount", "payloadTreeSha256", "schemaVersion", "transactionId",
        "uniqueId"
    };

    public static CommandReport Deploy(ParsedCommand command) => ExecutePackageOperation(command, "deploy");
    public static CommandReport Update(ParsedCommand command) => ExecutePackageOperation(command, "update");
    public static CommandReport OfficialPackage(ParsedCommand command, string operation) => ExecutePackageOperation(command, operation, official: true);

    public static CommandReport InstallLocalStatus(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root", "expected-version", "expected-package-sha256");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("install-local-status requires one UniqueID and exact expected version/hash options.");
        string gameRoot = RequireGameRoot(command);
        string uniqueId = command.Positionals[0];
        string expectedVersion = command.Option("expected-version");
        string expectedPackageSha256 = command.Option("expected-package-sha256");
        SourceStateService.ValidateLocalInstallUniqueId(uniqueId);
        if (string.IsNullOrWhiteSpace(expectedVersion) ||
            expectedPackageSha256.Length != 64 ||
            expectedPackageSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new CommandLineException(
                "install-local-status requires --expected-version and a 64-hex --expected-package-sha256.");
        }

        var report = NewReport("install-local-status", gameRoot);
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
            ValidateJournal(journal, gameRoot, uniqueId, null);
            if (journal.LocalInstall != null)
            {
                ValidateLocalInstallTransaction(journal, journal.LocalInstall, gameRoot);
                if (!journal.LocalInstall.ExpectedVersion.Equals(expectedVersion, StringComparison.Ordinal) ||
                    !journal.LocalInstall.ExpectedPackageSha256.Equals(
                        expectedPackageSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "Prepared local install does not match the expected version/package SHA-256.");
                }

                report.Success = true;
                report.OutputPath = journal.DestinationPath;
                report.Values["uniqueID"] = uniqueId;
                report.Values["version"] = journal.LocalInstall.ExpectedVersion;
                report.Values["packageSha256"] = journal.LocalInstall.ExpectedPackageSha256;
                report.Values["transactionId"] = journal.LocalInstall.TransactionId;
                report.Values["journalPath"] = journalPath;
                report.Values["sourceStatePath"] = journal.LocalInstall.SourceStatePath;
                report.Values["status"] = "RecoveryRequired";
                report.Diagnostics.Add(Warning(
                    "SDK402",
                    "The exact local install is pending recovery; this status query made no mutation.",
                    journalPath));
                return Finish(report);
            }
            if (journal.Active != null)
                throw new InvalidDataException("Local install is prepared but not committed; explicit recover is required.");
            DeploymentRecord committed = VerifyCommitted(journal);
            if (!committed.PackageSha256.Equals(expectedPackageSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Committed package SHA-256 does not match the expected local-install package.");
            RuntimeManifest manifest = ReadInstalledManifest(journal.DestinationPath);
            if (!manifest.UniqueID.Equals(uniqueId, StringComparison.Ordinal) ||
                !manifest.Version.Equals(expectedVersion, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Committed manifest identity/version does not match the expected local install.");
            }
            string sourceTreeSha256 = IsOfficial(journal) ? AuthorFileTreeDigest.Compute(journal.DestinationPath) : SourceStateService.VerifyLocalInstall(
                gameRoot,
                uniqueId,
                journal.DestinationPath);

            report.Success = true;
            report.OutputPath = journal.DestinationPath;
            report.Sha256 = committed.Inventory.TreeSha256;
            report.FileCount = committed.Inventory.Files.Count;
            report.Values["uniqueID"] = uniqueId;
            report.Values["version"] = manifest.Version;
            report.Values["packageSha256"] = committed.PackageSha256;
            report.Values["transactionId"] = committed.TransactionId;
            report.Values["journalPath"] = journalPath;
            report.Values["sourceStatePath"] = AuthorStatePaths.SourceStatePath(gameRoot);
            report.Values["sourceTreeSha256"] = sourceTreeSha256;
            report.Values["status"] = IsOfficial(journal) ? "CommittedOfficialLocal" : "CommittedLocalDevelopment";
            if (IsOfficial(journal)) OfficialLocalPaths.AddStatus(report, uniqueId);
            report.Diagnostics.Add(Info(
                "SDK400",
                "Reconciled the exact committed package, deployment journal and Local Development source without replaying a mutation."));
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK411", ex.Message, journalPath));
        }
        return Finish(report);
    }

    public static CommandReport InstallLocal(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root", "expected-unique-id", "expected-version", "expected-package-sha256");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("install-local requires one package ZIP and exact expected identity options.");

        string gameRoot = RequireGameRoot(command);
        string packagePath = Path.GetFullPath(command.Positionals[0]);
        string expectedUniqueId = command.Option("expected-unique-id");
        string expectedVersion = command.Option("expected-version");
        string expectedPackageSha256 = command.Option("expected-package-sha256");
        if (string.IsNullOrWhiteSpace(expectedUniqueId) ||
            string.IsNullOrWhiteSpace(expectedVersion) ||
            expectedPackageSha256.Length != 64 ||
            expectedPackageSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new CommandLineException(
                "install-local requires --expected-unique-id, --expected-version and a 64-hex --expected-package-sha256.");
        }
        try
        {
            SourceStateService.ValidateLocalInstallUniqueId(expectedUniqueId);
        }
        catch (InvalidDataException ex)
        {
            throw new CommandLineException(ex.Message);
        }

        var report = NewReport("install-local", gameRoot);
        string transactionId = Guid.NewGuid().ToString("N");
        string modsRoot = Path.Combine(gameRoot, "Mods");
        string destination = Path.Combine(modsRoot, expectedUniqueId);
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, expectedUniqueId);
        string sourceStatePath = AuthorStatePaths.SourceStatePath(gameRoot);
        string inputRoot = Path.Combine(AuthorStatePaths.InstallationStateRoot(gameRoot), "install-inputs");
        string immutablePackagePath = Path.Combine(inputRoot, transactionId + ".zip");
        string stagingPath = Path.Combine(HiddenRoot(modsRoot), "staging", transactionId);
        string failedPath = Path.Combine(HiddenRoot(modsRoot), "failed", expectedUniqueId + "-" + transactionId);
        ExactFileSnapshot? journalBefore = null;
        ExactFileSnapshot? sourceBefore = null;
        StagedDeploymentPackage? package = null;
        DeploymentRecord? previous = null;
        DeploymentJournal? authorityJournal = null;
        bool localPrepared = false;
        bool localPreparePublicationObserved = false;
        string operation = string.Empty;
        string recoveryPath = string.Empty;

        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            PathSafety.RejectBepInExPluginDestination(destination);
            AssertNoPendingLocalInstallForSourceMutation(gameRoot);
            (int orphanInputsCleaned, int orphanStagesCleaned) =
                CleanupUnreferencedLocalInstallArtifacts(gameRoot, modsRoot, inputRoot);
            report.Values["orphanInputsCleaned"] =
                orphanInputsCleaned.ToString(System.Globalization.CultureInfo.InvariantCulture);
            report.Values["orphanStagesCleaned"] =
                orphanStagesCleaned.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AuthorSourceState sourceState = SourceStateService.PrepareLocalInstall(gameRoot);
            journalBefore = ExactFileSnapshot.Capture(journalPath);
            sourceBefore = ExactFileSnapshot.Capture(sourceStatePath);

            if (!File.Exists(packagePath))
                throw new FileNotFoundException("Local-install package ZIP was not found.", packagePath);
            Directory.CreateDirectory(inputRoot);
            File.Copy(packagePath, immutablePackagePath, overwrite: false);
            string immutablePackageSha256 = PathSafety.Sha256File(immutablePackagePath);
            if (!immutablePackageSha256.Equals(expectedPackageSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Local-install package changed after caller preflight; the immutable SDK copy does not match expectedPackageSha256.");
            }

            Directory.CreateDirectory(modsRoot);
            package = DeploymentPackage.ExtractAndReceipt(
                immutablePackagePath,
                stagingPath,
                gameRoot,
                AuthorStatePaths.GameRootKey(gameRoot),
                transactionId);
            if (!package.Manifest.UniqueID.Equals(expectedUniqueId, StringComparison.Ordinal) ||
                !package.Manifest.Version.Equals(expectedVersion, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Local-install package identity/version does not exactly match the caller's Catalog projection.");
            }
            AuthorFaultInjector.Hit("install-local.after-package-validation");

            if (File.Exists(journalPath))
            {
                authorityJournal = ReadJournal(journalPath);
                ValidateJournal(authorityJournal, gameRoot, expectedUniqueId, package.PackageKind, package.CodeModKind);
                if (authorityJournal.LocalInstall != null)
                    throw new InvalidDataException("A prepared local install requires explicit recover before install-local.");
                if (authorityJournal.Active != null)
                    throw new InvalidDataException("A prepared deployment requires explicit recover before install-local.");
                if (authorityJournal.Committed != null)
                {
                    previous = VerifyCommitted(authorityJournal);
                    operation = "update";
                    recoveryPath = Path.Combine(
                        HiddenRoot(modsRoot),
                        "recovery",
                        expectedUniqueId + "-" + previous.TransactionId + "-" + package.Record.TransactionId);
                }
                else
                {
                    if (Directory.Exists(destination) || File.Exists(destination))
                        throw new InvalidDataException("An uncommitted destination exists; install-local refuses adoption.");
                    operation = "deploy";
                }
            }
            else
            {
                if (Directory.Exists(destination) || File.Exists(destination))
                    throw new InvalidDataException("Deployment destination exists without an exact SDK journal; install-local refuses adoption.");
                operation = "deploy";
                authorityJournal = NewJournal(
                    gameRoot,
                    expectedUniqueId,
                    package.PackageKind,
                    package.CodeModKind,
                    destination);
            }

            if (authorityJournal == null)
                throw new InvalidDataException("Local-install journal authority was not prepared.");
            authorityJournal.LocalInstall = new LocalInstallTransaction
            {
                TransactionId = transactionId,
                Phase = "Prepared",
                ExpectedVersion = expectedVersion,
                ExpectedPackageSha256 = immutablePackageSha256,
                JournalBeforeExisted = journalBefore.Existed,
                JournalBeforeBase64 = journalBefore.ToBase64(),
                SourceStateBeforeExisted = sourceBefore.Existed,
                SourceStateBeforeBase64 = sourceBefore.ToBase64(),
                SourceStatePath = sourceStatePath,
                StagingPath = stagingPath,
                RecoveryPath = recoveryPath,
                FailedPath = failedPath,
                Previous = previous,
                Next = package.Record
            };
            try
            {
                WriteJournal(journalPath, authorityJournal, "install-local.prepare");
                localPrepared = true;
                localPreparePublicationObserved = true;
            }
            catch
            {
                localPreparePublicationObserved = journalBefore != null && !journalBefore.MatchesCurrent();
                localPrepared = localPreparePublicationObserved &&
                    HasRetryableLocalInstallAuthority(
                        gameRoot,
                        expectedUniqueId,
                        package.Record.TransactionId);
                throw;
            }
            AuthorFaultInjector.Hit("install-local.after-prepare");

            var deploymentReport = NewReport(operation, gameRoot);
            if (operation == "update")
                ExecuteUpdate(gameRoot, modsRoot, package, deploymentReport);
            else
                ExecuteDeploy(gameRoot, modsRoot, package, deploymentReport);
            AuthorFaultInjector.Hit("install-local.after-deployment");

            AuthorFaultInjector.Hit("install-local.source.before-state");
            string sourceTreeSha256 = SourceStateService.CommitLocalInstall(
                gameRoot,
                sourceState,
                expectedUniqueId,
                destination);
            AuthorFaultInjector.Hit("install-local.after-source-selection");

            DeploymentJournal committed = LoadRequiredJournal(gameRoot, expectedUniqueId);
            ValidateJournal(committed, gameRoot, expectedUniqueId, package.PackageKind, package.CodeModKind);
            DeploymentRecord installed = VerifyCommitted(committed);
            if (!SameRecord(installed, package.Record))
                throw new InvalidDataException("Local-install committed record does not match the immutable package transaction.");
            LocalInstallTransaction localInstall = committed.LocalInstall
                ?? throw new InvalidDataException("Local-install journal lost its durable composite transaction before commit.");
            if (!localInstall.TransactionId.Equals(transactionId, StringComparison.Ordinal) ||
                !SameRecord(localInstall.Next, installed))
            {
                throw new InvalidDataException("Local-install journal composite transaction does not match the committed package.");
            }
            committed.LocalInstall = null;
            WriteJournal(journalPath, committed, "install-local.commit");

            report.Success = true;
            report.OutputPath = destination;
            report.Sha256 = installed.Inventory.TreeSha256;
            report.FileCount = installed.Inventory.Files.Count;
            report.Values["transactionId"] = transactionId;
            report.Values["uniqueID"] = expectedUniqueId;
            report.Values["version"] = expectedVersion;
            report.Values["operation"] = operation;
            report.Values["packageSha256"] = immutablePackageSha256;
            report.Values["journalPath"] = journalPath;
            report.Values["sourceStatePath"] = sourceStatePath;
            report.Values["sourceTreeSha256"] = sourceTreeSha256;
            report.Values["rollback"] = "not-required-committed";
            if (recoveryPath.Length > 0)
                report.Values["recoveryPath"] = recoveryPath;
            report.Diagnostics.Add(Info(
                "SDK400",
                "Atomically committed one receipt-bound package and its exact Local Development selection under the Author SDK game-root lock."));
        }
        catch (SimulatedAuthorCrashException ex)
        {
            if (package != null &&
                TryReconcileCommittedLocalInstall(
                    gameRoot,
                    expectedUniqueId,
                    expectedVersion,
                    package.Record,
                    out DeploymentRecord reconciled,
                    out string reconciledSourceTree))
            {
                PopulateCommittedLocalInstallReport(
                    report,
                    destination,
                    journalPath,
                    sourceStatePath,
                    expectedUniqueId,
                    expectedVersion,
                    operation,
                    reconciled,
                    reconciledSourceTree,
                    recoveryPath,
                    "reconciled-committed-after-write-exception");
                report.Values["simulatedCrash"] = ex.Point;
                report.Diagnostics.Add(Warning(
                    "SDK412",
                    ex.Message + " Exact read-only reconciliation proved that destination, journal and source selection had already committed.",
                    journalPath));
            }
            else
            {
                bool retryable = localPrepared &&
                    package != null &&
                    HasRetryableLocalInstallAuthority(gameRoot, expectedUniqueId, package.Record.TransactionId);
                report.Values["rollback"] = !localPrepared
                    ? localPreparePublicationObserved
                        ? "authority-unknown-fail-closed"
                        : "not-required-before-deployment"
                    : retryable
                        ? "recovery-required"
                        : "authority-unknown-fail-closed";
                report.Values["simulatedCrash"] = ex.Point;
                report.Diagnostics.Add(Error(
                    "SDK498",
                    ex.Message + (!localPrepared
                        ? localPreparePublicationObserved
                            ? " The prepare write changed durable authority, but its exact transaction marker could not be proved; no recovery claim is made."
                            : " No durable local-install mutation was prepared."
                        : retryable
                            ? " The exact durable local-install transaction marker remains available for explicit recover."
                            : " The exact committed state and a matching retry marker could not be proved; no recovery claim is made."),
                    journalPath));
            }
        }
        catch (Exception ex)
        {
            if (package != null &&
                TryReconcileCommittedLocalInstall(
                    gameRoot,
                    expectedUniqueId,
                    expectedVersion,
                    package.Record,
                    out DeploymentRecord reconciled,
                    out string reconciledSourceTree))
            {
                PopulateCommittedLocalInstallReport(
                    report,
                    destination,
                    journalPath,
                    sourceStatePath,
                    expectedUniqueId,
                    expectedVersion,
                    operation,
                    reconciled,
                    reconciledSourceTree,
                    recoveryPath,
                    "reconciled-committed-after-write-exception");
                report.Diagnostics.Add(Warning(
                    "SDK412",
                    "A write reported an exception after the exact local install had committed; read-only reconciliation accepted the committed terminal state. Original error: " + ex.Message,
                    journalPath));
            }
            else
            {
                report.Diagnostics.Add(Error("SDK408", ex.Message, journalPath));
                if (localPrepared)
                {
                    try
                    {
                        using GameOperationLock recoveryLock = GameOperationLock.Acquire(gameRoot);
                        DeploymentJournal current = LoadRequiredJournal(gameRoot, expectedUniqueId);
                        ValidateJournal(current, gameRoot, expectedUniqueId, package?.PackageKind);
                        if (!RecoverLocalInstall(current, gameRoot))
                            throw new InvalidDataException("Durable local-install recovery state was unexpectedly absent.");
                        report.Values["rollback"] = "restored-exact-pre-state";
                        report.Diagnostics.Add(Warning(
                            "SDK409",
                            "The failed local install restored its exact prior destination, deployment journal and source state.",
                            journalPath));
                    }
                    catch (Exception rollbackError)
                    {
                        bool retryable = package != null &&
                            HasRetryableLocalInstallAuthority(gameRoot, expectedUniqueId, package.Record.TransactionId);
                        report.Values["rollback"] = retryable
                            ? "blocked-preserved"
                            : "authority-unknown-fail-closed";
                        report.Diagnostics.Add(Error(
                            "SDK410",
                            (retryable
                                ? "Local-install rollback could not prove an exact restore; its matching durable marker remains available: "
                                : "Local-install rollback could prove neither an exact restore nor a matching retry marker: ") +
                            rollbackError.Message,
                            journalPath));
                    }
                }
                else
                {
                    report.Values["rollback"] = localPreparePublicationObserved
                        ? "authority-unknown-fail-closed"
                        : package == null
                            ? "not-required-before-package-transaction"
                            : "not-required-before-deployment";
                }
            }
        }
        finally
        {
            TryDeleteOwnedFileWithoutThrow(immutablePackagePath);
            if (!report.Values.TryGetValue("rollback", out string? rollback) ||
                (!rollback.Equals("blocked-preserved", StringComparison.Ordinal) &&
                 !rollback.Equals("recovery-required", StringComparison.Ordinal) &&
                 !rollback.Equals("authority-unknown-fail-closed", StringComparison.Ordinal)))
            {
                TryDeleteOwnedDirectoryWithoutThrow(stagingPath);
                TryDeleteEmptyDirectoryWithoutThrow(Path.GetDirectoryName(stagingPath)!);
            }
            TryDeleteEmptyDirectoryWithoutThrow(inputRoot);
        }

        return Finish(report);
    }

    public static CommandReport Withdraw(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("withdraw requires one UniqueID and --game-root.");
        string gameRoot = RequireGameRoot(command);
        string uniqueId = command.Positionals[0];
        var report = NewReport("withdraw", gameRoot);
        ExecuteLocked(report, gameRoot, uniqueId, () => ExecuteWithdraw(gameRoot, uniqueId, report));
        return Finish(report);
    }

    public static CommandReport Recover(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("recover requires one UniqueID and --game-root.");
        string gameRoot = RequireGameRoot(command);
        string uniqueId = command.Positionals[0];
        var report = NewReport("recover", gameRoot);
        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
            ValidateJournal(journal, gameRoot, uniqueId, null);
            using ColdGameMutationLease? cold = IsOfficial(journal) ? ColdGameMutationLease.Acquire(gameRoot) : null;
            bool recovered = RecoverLocalInstall(journal, gameRoot);
            if (!recovered)
                recovered = RecoverPrepared(journal, gameRoot);
            string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
            if (File.Exists(journalPath))
            {
                journal = LoadRequiredJournal(gameRoot, uniqueId);
                ValidateJournal(journal, gameRoot, uniqueId, null);
                if (!recovered && journal.Committed != null)
                    VerifyCommitted(journal);
            }
            report.Success = true;
            report.Values["uniqueID"] = uniqueId;
            report.Values["recovered"] = recovered.ToString().ToLowerInvariant();
            report.Values["status"] = File.Exists(journalPath) ? journal.Status : "Absent";
            report.Values["journalPath"] = journalPath;
            report.Diagnostics.Add(Info("SDK400", recovered ? "Rolled the prepared transaction back to its exact pre-transaction state." : "No prepared transaction required recovery."));
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK401", ex.Message, AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
        }
        return Finish(report);
    }

    public static CommandReport Status(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("deployment-status requires one UniqueID and --game-root.");
        string gameRoot = RequireGameRoot(command);
        string uniqueId = command.Positionals[0];
        var report = NewReport("deployment-status", gameRoot);
        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
            ValidateJournal(journal, gameRoot, uniqueId, null);
            if (journal.LocalInstall != null)
            {
                report.Diagnostics.Add(Warning("SDK402", "A prepared local install requires explicit recover before another mutation.", AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
                report.Values["activeOperation"] = "install-local";
                report.Values["activePhase"] = journal.LocalInstall.Phase;
            }
            else if (journal.Active != null)
            {
                report.Diagnostics.Add(Warning("SDK402", "A prepared transaction requires explicit recover before another mutation.", AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
                report.Values["activeOperation"] = journal.Active.Operation;
                report.Values["activePhase"] = journal.Active.Phase;
            }
            else if (journal.Committed != null)
            {
                VerifyCommitted(journal);
            }
            else if (Directory.Exists(journal.DestinationPath))
            {
                throw new InvalidDataException("Journal says no package is committed, but the destination exists.");
            }
            report.Success = true;
            report.Values["uniqueID"] = uniqueId;
            report.Values["status"] = journal.Status;
            report.Values["destinationPath"] = journal.DestinationPath;
            report.Values["journalPath"] = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
            report.Values["treeSha256"] = journal.Committed?.Inventory.TreeSha256 ?? string.Empty;
            report.Values["recoveryArtifacts"] = journal.RecoveryArtifacts.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (IsOfficial(journal)) OfficialLocalPaths.AddStatus(report, uniqueId);
            report.Diagnostics.Add(Info("SDK400", "Deployment status was read without changing the package."));
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK403", ex.Message, AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
        }
        return Finish(report);
    }

    private static CommandReport ExecutePackageOperation(ParsedCommand command, string operation, bool official = false)
    {
        if (operation == "install-local") command.RequireOnlyOptions("game-root", "expected-unique-id", "expected-version", "expected-package-sha256");
        else command.RequireOnlyOptions("game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException(operation + " requires one package ZIP and --game-root.");
        string gameRoot = RequireGameRoot(command);
        string packagePath = Path.GetFullPath(command.Positionals[0]);
        var report = NewReport(operation, gameRoot);
        string transactionId = Guid.NewGuid().ToString("N");
        string modsRoot = official ? OfficialLocalPaths.ModsRoot : EnsureModsRoot(gameRoot);
        string stagingPath = Path.Combine(HiddenRoot(modsRoot), "staging", transactionId);
        string uniqueId = string.Empty;
        ColdGameMutationLease? cold = null;
        GameOperationLock? operationLock = null;
        try
        {
            operationLock = GameOperationLock.Acquire(gameRoot);
            cold = official ? ColdGameMutationLease.Acquire(gameRoot) : null;
            if (Directory.Exists(modsRoot)) PathSafety.RejectReparsePoints(modsRoot, Array.Empty<string>());
            StagedDeploymentPackage package = DeploymentPackage.ExtractAndReceipt(packagePath, stagingPath, gameRoot, AuthorStatePaths.GameRootKey(gameRoot), transactionId, official);
            uniqueId = package.Manifest.UniqueID;
            if (operation == "install-local")
            {
                if (command.Option("expected-unique-id") != uniqueId || command.Option("expected-version") != package.Manifest.Version ||
                    !command.Option("expected-package-sha256").Equals(package.PackageSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Exact expected UniqueID/version/package hash is required and must match.");
                string existing = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
                operation = File.Exists(existing) && ReadJournal(existing).Committed != null ? "update" : "deploy";
            }
            if (official && File.Exists(AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)) &&
                ReadJournal(AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)).SchemaVersion != AuthorSdkContract.OfficialDeploymentJournalSchemaVersion)
                throw new InvalidDataException("Historical deployment exists. Recover/withdraw it first; official install cannot adopt or update a legacy journal.");
            AuthorFaultInjector.Hit(operation + ".after-stage");
            if (operation == "deploy")
                ExecuteDeploy(gameRoot, modsRoot, package, report);
            else
                ExecuteUpdate(gameRoot, modsRoot, package, report);
            report.Success = true;
            report.OutputPath = Path.Combine(modsRoot, uniqueId);
            report.Sha256 = package.Record.Inventory.TreeSha256;
            report.FileCount = package.Record.Inventory.Files.Count;
            report.Values["transactionId"] = transactionId;
            report.Values["uniqueID"] = uniqueId;
            report.Values["packageKind"] = package.PackageKind;
            report.Values["codeModKind"] = package.CodeModKind;
            report.Values["journalPath"] = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
            if (official) OfficialLocalPaths.AddStatus(report, uniqueId);
            report.Diagnostics.Insert(0, Info("SDK400", "Committed receipt-bound " + operation + (official ? " in official MODS. Enable it in the official game UI." : " in historical game/Mods.")));
        }
        catch (SimulatedAuthorCrashException ex)
        {
            report.Diagnostics.Add(Error("SDK498", ex.Message + " Prepared state was intentionally left for recover; no state was guessed.", uniqueId.Length > 0 ? AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId) : stagingPath));
            report.Values["simulatedCrash"] = ex.Point;
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK404", ex.Message, uniqueId.Length > 0 ? AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId) : stagingPath));
            if (uniqueId.Length > 0 && (!official || cold != null))
                TryRollbackAfterFailure(gameRoot, uniqueId, report);
            if (Directory.Exists(stagingPath))
                report.Values["preservedStagingPath"] = stagingPath;
        }
        finally { cold?.Dispose(); operationLock?.Dispose(); }
        return Finish(report);
    }

    private static void ExecuteLocked(CommandReport report, string gameRoot, string uniqueId, Action action)
    {
        ColdGameMutationLease? cold = null;
        GameOperationLock? operationLock = null;
        bool mayRecover = false;
        try
        {
            operationLock = GameOperationLock.Acquire(gameRoot);
            DeploymentJournal existing = LoadRequiredJournal(gameRoot, uniqueId);
            ValidateJournal(existing, gameRoot, uniqueId, null);
            cold = IsOfficial(existing) ? ColdGameMutationLease.Acquire(gameRoot) : null;
            mayRecover = true;
            action();
            report.Success = true;
        }
        catch (SimulatedAuthorCrashException ex)
        {
            report.Diagnostics.Add(Error("SDK498", ex.Message + " Prepared state was intentionally left for recover; no state was guessed.", AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
            report.Values["simulatedCrash"] = ex.Point;
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK405", ex.Message, AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
            if (mayRecover) TryRollbackAfterFailure(gameRoot, uniqueId, report);
        }
        finally { cold?.Dispose(); operationLock?.Dispose(); }
    }

    private static void ExecuteDeploy(string gameRoot, string modsRoot, StagedDeploymentPackage package, CommandReport report)
    {
        string uniqueId = package.Manifest.UniqueID;
        string destination = Path.Combine(modsRoot, uniqueId);
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
        DeploymentJournal journal;
        if (File.Exists(journalPath))
        {
            journal = ReadJournal(journalPath);
            ValidateJournal(journal, gameRoot, uniqueId, package.PackageKind, package.CodeModKind);
            if (journal.LocalInstall != null && !journal.LocalInstall.TransactionId.Equals(package.Record.TransactionId, StringComparison.Ordinal))
                throw new InvalidDataException("A different prepared local install requires explicit recover before deploy.");
            if (journal.Active != null)
            {
                RecoverPrepared(journal, gameRoot);
                throw new InvalidDataException("Recovered an earlier prepared transaction. Re-run deploy explicitly.");
            }
            if (journal.Committed != null)
                throw new InvalidDataException("A committed deployment already exists. Use update after exact dual-receipt verification.");
        }
        else
        {
            journal = NewJournal(gameRoot, uniqueId, package.PackageKind, package.CodeModKind, destination);
        }
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new InvalidDataException("Deployment destination already exists without a current committed dual receipt. No adopt/force operation is available: " + destination);

        string failedPath = Path.Combine(HiddenRoot(modsRoot), "failed", uniqueId + "-" + package.Record.TransactionId);
        EnsureMoveTargetAbsent(failedPath);
        journal.Status = "Prepared";
        journal.Active = new DeploymentTransaction
        {
            TransactionId = package.Record.TransactionId,
            Operation = "deploy",
            Phase = "Prepared",
            StagingPath = package.StagingPath,
            FailedPath = failedPath,
            Next = package.Record
        };
        WriteJournal(journalPath, journal, "deploy.prepare");

        AuthorFaultInjector.Hit("deploy.publish.before-move");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        Directory.Move(package.StagingPath, destination);
        AuthorFaultInjector.Hit("deploy.publish.after-move");
        journal.Active.Phase = "Published";
        WriteJournal(journalPath, journal, "deploy.publish-state");

        journal.Committed = package.Record;
        journal.Active = null;
        journal.Status = "Installed";
        WriteJournal(journalPath, journal, "deploy.commit");
        report.Values["destinationPath"] = destination;
    }

    private static void ExecuteUpdate(string gameRoot, string modsRoot, StagedDeploymentPackage package, CommandReport report)
    {
        string uniqueId = package.Manifest.UniqueID;
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
        DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
        ValidateJournal(journal, gameRoot, uniqueId, package.PackageKind, package.CodeModKind);
        if (journal.LocalInstall != null && !journal.LocalInstall.TransactionId.Equals(package.Record.TransactionId, StringComparison.Ordinal))
            throw new InvalidDataException("A different prepared local install requires explicit recover before update.");
        if (journal.Active != null)
        {
            RecoverPrepared(journal, gameRoot);
            throw new InvalidDataException("Recovered an earlier prepared transaction. Re-run update explicitly.");
        }
        DeploymentRecord previous = VerifyCommitted(journal);
        string recoveryPath = Path.Combine(HiddenRoot(modsRoot), "recovery", uniqueId + "-" + previous.TransactionId + "-" + package.Record.TransactionId);
        string failedPath = Path.Combine(HiddenRoot(modsRoot), "failed", uniqueId + "-" + package.Record.TransactionId);
        EnsureMoveTargetAbsent(recoveryPath);
        EnsureMoveTargetAbsent(failedPath);
        journal.Status = "Prepared";
        journal.Active = new DeploymentTransaction
        {
            TransactionId = package.Record.TransactionId,
            Operation = "update",
            Phase = "Prepared",
            StagingPath = package.StagingPath,
            RecoveryPath = recoveryPath,
            FailedPath = failedPath,
            Previous = previous,
            Next = package.Record
        };
        WriteJournal(journalPath, journal, "update.prepare");

        AuthorFaultInjector.Hit("update.recovery.before-move");
        Directory.CreateDirectory(Path.GetDirectoryName(recoveryPath)!);
        Directory.Move(journal.DestinationPath, recoveryPath);
        AuthorFaultInjector.Hit("update.recovery.after-move");
        journal.Active.Phase = "PreviousMoved";
        WriteJournal(journalPath, journal, "update.recovery-state");

        AuthorFaultInjector.Hit("update.publish.before-move");
        Directory.Move(package.StagingPath, journal.DestinationPath);
        AuthorFaultInjector.Hit("update.publish.after-move");
        journal.Active.Phase = "Published";
        WriteJournal(journalPath, journal, "update.publish-state");

        AddArtifact(journal, "previous-version", recoveryPath, previous.Inventory.TreeSha256);
        journal.Committed = package.Record;
        journal.Active = null;
        journal.Status = "Installed";
        WriteJournal(journalPath, journal, "update.commit");
        report.Values["destinationPath"] = journal.DestinationPath;
        report.Values["recoveryPath"] = recoveryPath;
    }

    private static void ExecuteWithdraw(string gameRoot, string uniqueId, CommandReport report)
    {
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
        DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
        ValidateJournal(journal, gameRoot, uniqueId, null);
        if (journal.LocalInstall != null)
            throw new InvalidDataException("A prepared local install requires explicit recover before withdraw.");
        if (journal.Active != null)
        {
            RecoverPrepared(journal, gameRoot);
            throw new InvalidDataException("Recovered an earlier prepared transaction. Re-run withdraw explicitly.");
        }
        DeploymentRecord previous = VerifyCommitted(journal);
        string modsRoot = IsOfficial(journal) ? OfficialLocalPaths.ModsRoot : EnsureModsRoot(gameRoot);
        string transactionId = Guid.NewGuid().ToString("N");
        string recoveryPath = Path.Combine(HiddenRoot(modsRoot), "recovery", uniqueId + "-" + previous.TransactionId + "-withdraw-" + transactionId);
        EnsureMoveTargetAbsent(recoveryPath);
        journal.Status = "Prepared";
        journal.Active = new DeploymentTransaction
        {
            TransactionId = transactionId,
            Operation = "withdraw",
            Phase = "Prepared",
            RecoveryPath = recoveryPath,
            Previous = previous
        };
        WriteJournal(journalPath, journal, "withdraw.prepare");

        AuthorFaultInjector.Hit("withdraw.recovery.before-move");
        Directory.CreateDirectory(Path.GetDirectoryName(recoveryPath)!);
        Directory.Move(journal.DestinationPath, recoveryPath);
        AuthorFaultInjector.Hit("withdraw.recovery.after-move");
        journal.Active.Phase = "PreviousMoved";
        WriteJournal(journalPath, journal, "withdraw.recovery-state");

        AddArtifact(journal, "withdrawn-package", recoveryPath, previous.Inventory.TreeSha256);
        journal.Committed = null;
        journal.Active = null;
        journal.Status = "Withdrawn";
        WriteJournal(journalPath, journal, "withdraw.commit");
        report.Values["transactionId"] = transactionId;
        report.Values["uniqueID"] = uniqueId;
        report.Values["recoveryPath"] = recoveryPath;
        report.Values["journalPath"] = journalPath;
        report.Diagnostics.Insert(0, Info("SDK400", "Withdrew the exact receipt-bound package into same-volume recovery; recovery material was retained."));
    }

    private static bool RecoverPrepared(DeploymentJournal journal, string gameRoot)
    {
        if (journal.Active == null)
            return false;
        ValidateJournal(journal, gameRoot, journal.UniqueId, journal.PackageKind);
        DeploymentTransaction active = journal.Active;
        ValidateActivePaths(journal, active, gameRoot);
        if (active.Operation.Equals("deploy", StringComparison.Ordinal))
            RecoverDeploy(journal, active);
        else if (active.Operation.Equals("update", StringComparison.Ordinal))
            RecoverUpdate(journal, active);
        else if (active.Operation.Equals("withdraw", StringComparison.Ordinal))
            RecoverWithdraw(journal, active);
        else
            throw new InvalidDataException("Prepared journal has an unknown operation.");

        PreservePreparedArtifacts(journal, active);
        journal.Active = null;
        journal.Status = journal.Committed != null ? "Installed" : "Absent";
        WriteJournal(AuthorStatePaths.DeploymentJournalPath(gameRoot, journal.UniqueId), journal, "recover.commit");
        return true;
    }

    private static bool RecoverLocalInstall(DeploymentJournal journal, string gameRoot)
    {
        LocalInstallTransaction? local = journal.LocalInstall;
        if (local == null)
            return false;

        AssertSolePendingLocalInstallRecovery(
            gameRoot,
            journal.UniqueId,
            local.TransactionId);
        ValidateLocalInstallTransaction(journal, local, gameRoot);
        string journalPath = AuthorStatePaths.DeploymentJournalPath(gameRoot, journal.UniqueId);
        ExactFileSnapshot journalBefore = ExactFileSnapshot.FromBase64(
            journalPath,
            local.JournalBeforeExisted,
            local.JournalBeforeBase64);
        ExactFileSnapshot sourceBefore = ExactFileSnapshot.FromBase64(
            local.SourceStatePath,
            local.SourceStateBeforeExisted,
            local.SourceStateBeforeBase64);
        string committedTreeSha256 = AuthorFileTreeDigest.Compute(local.Next.Inventory);
        byte[] committedSourceState = SourceStateService.BuildCommittedLocalInstallState(
            gameRoot,
            sourceBefore.Existed,
            sourceBefore.Bytes,
            journal.UniqueId,
            journal.DestinationPath,
            committedTreeSha256);
        AssertSourceStateMayBeRestored(sourceBefore, committedSourceState);
        try
        {
            RollbackLocalInstall(
                gameRoot,
                journal.UniqueId,
                journal.DestinationPath,
                local.StagingPath,
                local.FailedPath,
                local.RecoveryPath,
                local.Previous,
                local.Next,
                journalBefore,
                sourceBefore);
            return true;
        }
        catch (Exception ex)
        {
            if (MatchesExactLocalInstallPreState(
                journal.DestinationPath,
                local.Previous,
                journalBefore,
                sourceBefore))
            {
                return true;
            }
            if (HasRetryableLocalInstallAuthority(gameRoot, journal.UniqueId, local.TransactionId))
            {
                throw new IOException(
                    "Local-install recovery did not reach the exact pre-state; its durable schema-3 authority remains available for retry.",
                    ex);
            }
            throw new InvalidDataException(
                "Local-install recovery reached neither the exact pre-state nor a retryable durable authority.",
                ex);
        }
    }

    private static bool TryReconcileCommittedLocalInstall(
        string gameRoot,
        string uniqueId,
        string expectedVersion,
        DeploymentRecord expectedRecord,
        out DeploymentRecord committed,
        out string sourceTreeSha256)
    {
        committed = new DeploymentRecord();
        sourceTreeSha256 = string.Empty;
        try
        {
            using GameOperationLock reconciliationLock = GameOperationLock.Acquire(gameRoot);
            DeploymentJournal journal = LoadRequiredJournal(gameRoot, uniqueId);
            ValidateJournal(journal, gameRoot, uniqueId, null);
            if (journal.LocalInstall != null || journal.Active != null)
                return false;
            DeploymentRecord current = VerifyCommitted(journal);
            if (!SameRecord(current, expectedRecord))
                return false;
            RuntimeManifest manifest = ReadInstalledManifest(journal.DestinationPath);
            if (!manifest.UniqueID.Equals(uniqueId, StringComparison.Ordinal) ||
                !manifest.Version.Equals(expectedVersion, StringComparison.Ordinal))
            {
                return false;
            }
            string sourceTree = SourceStateService.VerifyLocalInstall(
                gameRoot,
                uniqueId,
                journal.DestinationPath);
            committed = current;
            sourceTreeSha256 = sourceTree;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void PopulateCommittedLocalInstallReport(
        CommandReport report,
        string destination,
        string journalPath,
        string sourceStatePath,
        string uniqueId,
        string version,
        string operation,
        DeploymentRecord committed,
        string sourceTreeSha256,
        string recoveryPath,
        string reconciliation)
    {
        report.Success = true;
        report.OutputPath = destination;
        report.Sha256 = committed.Inventory.TreeSha256;
        report.FileCount = committed.Inventory.Files.Count;
        report.Values["transactionId"] = committed.TransactionId;
        report.Values["uniqueID"] = uniqueId;
        report.Values["version"] = version;
        report.Values["operation"] = operation;
        report.Values["packageSha256"] = committed.PackageSha256;
        report.Values["journalPath"] = journalPath;
        report.Values["sourceStatePath"] = sourceStatePath;
        report.Values["sourceTreeSha256"] = sourceTreeSha256;
        report.Values["rollback"] = "not-required-committed";
        report.Values["reconciliation"] = reconciliation;
        if (recoveryPath.Length > 0)
            report.Values["recoveryPath"] = recoveryPath;
    }

    private static bool MatchesExactLocalInstallPreState(
        string destination,
        DeploymentRecord? previous,
        ExactFileSnapshot journalBefore,
        ExactFileSnapshot sourceBefore)
    {
        try
        {
            bool destinationMatches = previous == null
                ? !Directory.Exists(destination) && !File.Exists(destination)
                : Directory.Exists(destination) &&
                  DeploymentTree.EqualsExact(DeploymentTree.Create(destination), previous.Inventory);
            return destinationMatches &&
                   journalBefore.MatchesCurrent() &&
                   sourceBefore.MatchesCurrent();
        }
        catch
        {
            return false;
        }
    }

    private static bool HasRetryableLocalInstallAuthority(
        string gameRoot,
        string uniqueId,
        string transactionId)
    {
        try
        {
            string path = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
            if (!File.Exists(path))
                return false;
            DeploymentJournal current = ReadJournal(path);
            ValidateJournal(current, gameRoot, uniqueId, null);
            LocalInstallTransaction? localInstall = current.LocalInstall;
            if (localInstall == null)
                return false;
            ValidateLocalInstallTransaction(current, localInstall, gameRoot);
            return localInstall.TransactionId.Equals(transactionId, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static (int Inputs, int Stages) CleanupUnreferencedLocalInstallArtifacts(
        string gameRoot,
        string modsRoot,
        string inputRoot)
    {
        string stagingRoot = Path.Combine(HiddenRoot(modsRoot), "staging");
        var protectedStages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DeploymentJournal journal in ReadValidatedDeploymentJournals(gameRoot))
        {
            if (journal.LocalInstall != null)
            {
                protectedStages.Add(Path.GetFullPath(journal.LocalInstall.StagingPath));
            }
            if (journal.Active != null && !string.IsNullOrWhiteSpace(journal.Active.StagingPath))
            {
                protectedStages.Add(Path.GetFullPath(journal.Active.StagingPath));
            }
            foreach (DeploymentRecoveryArtifact artifact in journal.RecoveryArtifacts)
            {
                if (!string.IsNullOrWhiteSpace(artifact.Path) &&
                    PathsEqual(Path.GetDirectoryName(Path.GetFullPath(artifact.Path)) ?? string.Empty, stagingRoot))
                {
                    protectedStages.Add(Path.GetFullPath(artifact.Path));
                }
            }
        }

        int inputs = 0;
        if (Directory.Exists(inputRoot))
        {
            foreach (string path in Directory.EnumerateFiles(
                         inputRoot,
                         "*.zip",
                         SearchOption.TopDirectoryOnly))
            {
                string transactionId = Path.GetFileNameWithoutExtension(path);
                if (!IsTransactionId(transactionId))
                    continue;
                var info = new FileInfo(path);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    !PathsEqual(info.DirectoryName ?? string.Empty, inputRoot))
                {
                    throw new InvalidDataException(
                        "Local-install orphan input is not an exact direct non-reparse file.");
                }
                File.Delete(info.FullName);
                inputs++;
            }
            TryDeleteEmptyDirectory(inputRoot);
        }

        int stages = 0;
        if (Directory.Exists(stagingRoot))
        {
            foreach (string path in Directory.EnumerateDirectories(
                         stagingRoot,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                var info = new DirectoryInfo(path);
                if (!IsTransactionId(info.Name))
                    continue;
                string fullPath = info.FullName;
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    !PathsEqual(info.Parent?.FullName ?? string.Empty, stagingRoot))
                {
                    throw new InvalidDataException(
                        "Local-install orphan stage is not an exact direct non-reparse directory.");
                }
                if (protectedStages.Contains(fullPath))
                    continue;
                Directory.Delete(fullPath, recursive: true);
                stages++;
            }
            TryDeleteEmptyDirectory(stagingRoot);
        }

        return (inputs, stages);
    }

    internal static void AssertNoPendingLocalInstallForSourceMutation(string gameRoot)
    {
        string[] pending = ReadValidatedDeploymentJournals(gameRoot)
            .Where(journal => journal.LocalInstall != null)
            .Select(journal => journal.UniqueId + ":" + journal.LocalInstall!.TransactionId)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (pending.Length > 0)
        {
            throw new InvalidDataException(
                "A game-root local-install recovery is pending (" +
                string.Join(", ", pending) +
                "). Source-state writes and every other install-local are blocked until explicit recover completes.");
        }
    }

    private static void AssertSolePendingLocalInstallRecovery(
        string gameRoot,
        string uniqueId,
        string transactionId)
    {
        DeploymentJournal[] pending = ReadValidatedDeploymentJournals(gameRoot)
            .Where(journal => journal.LocalInstall != null)
            .ToArray();
        if (pending.Length != 1 ||
            !pending[0].UniqueId.Equals(uniqueId, StringComparison.Ordinal) ||
            !pending[0].LocalInstall!.TransactionId.Equals(transactionId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Local-install recovery requires exactly one validated game-root pending marker matching the requested product/transaction; recovery order was not guessed.");
        }
    }

    private static List<DeploymentJournal> ReadValidatedDeploymentJournals(string gameRoot)
    {
        string deploymentsRoot = Path.Combine(
            AuthorStatePaths.InstallationStateRoot(gameRoot),
            "deployments");
        var journals = new List<DeploymentJournal>();
        if (!Directory.Exists(deploymentsRoot))
            return journals;

        var rootInfo = new DirectoryInfo(deploymentsRoot);
        if ((rootInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Deployment journal root cannot be a reparse point.");

        foreach (string journalPath in Directory.EnumerateFiles(
                     deploymentsRoot,
                     "*.journal.json",
                     SearchOption.TopDirectoryOnly))
        {
            var fileInfo = new FileInfo(journalPath);
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0 ||
                !PathsEqual(fileInfo.DirectoryName ?? string.Empty, deploymentsRoot))
            {
                throw new InvalidDataException(
                    "Deployment journal is not an exact direct non-reparse file.");
            }

            DeploymentJournal journal = ReadJournal(fileInfo.FullName);
            SourceStateService.ValidateLocalInstallUniqueId(journal.UniqueId);
            string expectedPath =
                AuthorStatePaths.DeploymentJournalPath(gameRoot, journal.UniqueId);
            if (!PathsEqual(fileInfo.FullName, expectedPath))
                throw new InvalidDataException("Deployment journal filename does not match its exact UniqueID authority.");
            ValidateJournal(journal, gameRoot, journal.UniqueId, null);
            if (journal.LocalInstall != null)
                ValidateLocalInstallTransaction(journal, journal.LocalInstall, gameRoot);
            if (journal.Active != null)
                ValidateActivePaths(journal, journal.Active, gameRoot);
            journals.Add(journal);
        }
        return journals;
    }

    private static void AssertSourceStateMayBeRestored(
        ExactFileSnapshot sourceBefore,
        byte[] committedSourceState)
    {
        if (sourceBefore.MatchesCurrent())
            return;
        if (File.Exists(sourceBefore.Path) &&
            File.ReadAllBytes(sourceBefore.Path).SequenceEqual(committedSourceState))
        {
            return;
        }
        throw new InvalidDataException(
            "Current source-state matches neither the exact pre-transaction snapshot nor this local-install transaction's exact committed state. Recovery refused before changing deployment or source state.");
    }

    private static bool IsTransactionId(string value) =>
        value.Length == 32 && value.All(character => Uri.IsHexDigit(character));

    private static void ValidateLocalInstallTransaction(
        DeploymentJournal journal,
        LocalInstallTransaction local,
        string gameRoot)
    {
        string modsRoot = Path.Combine(AuthorStatePaths.CanonicalGameRoot(gameRoot), "Mods");
        string hiddenRoot = HiddenRoot(modsRoot);
        string expectedStaging = Path.Combine(hiddenRoot, "staging", local.TransactionId);
        string expectedFailed = Path.Combine(hiddenRoot, "failed", journal.UniqueId + "-" + local.TransactionId);
        string expectedSourceState = AuthorStatePaths.SourceStatePath(gameRoot);
        string expectedRecovery = local.Previous == null
            ? string.Empty
            : Path.Combine(
                hiddenRoot,
                "recovery",
                journal.UniqueId + "-" + local.Previous.TransactionId + "-" + local.TransactionId);
        bool committedShapeValid = local.Previous == null
            ? journal.Committed == null || SameRecord(journal.Committed, local.Next)
            : journal.Committed != null &&
              (SameRecord(journal.Committed, local.Previous) || SameRecord(journal.Committed, local.Next));
        if (local.TransactionId.Length != 32 ||
            local.TransactionId.Any(character => !Uri.IsHexDigit(character)) ||
            !local.Phase.Equals("Prepared", StringComparison.Ordinal) ||
            local.ExpectedVersion.Length == 0 ||
            local.ExpectedPackageSha256.Length != 64 ||
            local.ExpectedPackageSha256.Any(character => !Uri.IsHexDigit(character)) ||
            !local.ExpectedPackageSha256.Equals(local.Next.PackageSha256, StringComparison.OrdinalIgnoreCase) ||
            !local.Next.TransactionId.Equals(local.TransactionId, StringComparison.Ordinal) ||
            !PathsEqual(local.StagingPath, expectedStaging) ||
            !PathsEqual(local.FailedPath, expectedFailed) ||
            !PathsEqual(local.SourceStatePath, expectedSourceState) ||
            !(expectedRecovery.Length == 0
                ? local.RecoveryPath.Length == 0
                : PathsEqual(local.RecoveryPath, expectedRecovery)) ||
            !committedShapeValid)
        {
            throw new InvalidDataException("Prepared local-install journal state is structurally invalid.");
        }

        string expectedJournalPath =
            AuthorStatePaths.DeploymentJournalPath(gameRoot, journal.UniqueId);
        ExactFileSnapshot journalBefore = ExactFileSnapshot.FromBase64(
            expectedJournalPath,
            local.JournalBeforeExisted,
            local.JournalBeforeBase64);
        if (journalBefore.Existed)
        {
            DeploymentJournal previousJournal =
                ReadJournalBytes(journalBefore.Bytes, expectedJournalPath);
            ValidateJournal(
                previousJournal,
                gameRoot,
                journal.UniqueId,
                journal.PackageKind,
                journal.CodeModKind);
            if (previousJournal.Active != null || previousJournal.LocalInstall != null)
                throw new InvalidDataException("Local-install journal snapshot contains an active transaction.");
            bool previousMatches = local.Previous == null
                ? previousJournal.Committed == null
                : previousJournal.Committed != null &&
                  SameRecord(previousJournal.Committed, local.Previous);
            if (!previousMatches)
                throw new InvalidDataException("Local-install journal snapshot does not match the exact previous deployment record.");
        }
        else if (local.Previous != null)
        {
            throw new InvalidDataException("Local-install previous deployment cannot exist without a journal snapshot.");
        }

        ExactFileSnapshot sourceBefore = ExactFileSnapshot.FromBase64(
            expectedSourceState,
            local.SourceStateBeforeExisted,
            local.SourceStateBeforeBase64);
        if (sourceBefore.Existed)
            _ = SourceStateService.ValidateExactSnapshot(gameRoot, sourceBefore.Bytes);
    }

    private static void RecoverDeploy(DeploymentJournal journal, DeploymentTransaction active)
    {
        if (active.Previous != null || active.Next == null)
            throw new InvalidDataException("Prepared deploy record is structurally invalid.");
        VerifyOptionalTree(active.StagingPath, active.Next, "prepared deploy staging");
        VerifyOptionalTree(active.FailedPath, active.Next, "prepared deploy failed recovery");
        if (Directory.Exists(journal.DestinationPath))
        {
            VerifyTree(journal.DestinationPath, active.Next, "prepared deploy destination");
            EnsureMoveTargetAbsent(active.FailedPath);
            Directory.CreateDirectory(Path.GetDirectoryName(active.FailedPath)!);
            AuthorFaultInjector.Hit("recover.deploy.failed.before-move");
            Directory.Move(journal.DestinationPath, active.FailedPath);
            AuthorFaultInjector.Hit("recover.deploy.failed.after-move");
        }
        journal.Committed = null;
    }

    private static void RecoverUpdate(DeploymentJournal journal, DeploymentTransaction active)
    {
        if (active.Previous == null || active.Next == null || journal.Committed == null || !SameRecord(active.Previous, journal.Committed))
            throw new InvalidDataException("Prepared update does not retain the exact prior committed record.");
        VerifyOptionalTree(active.StagingPath, active.Next, "prepared update staging");
        VerifyOptionalTree(active.FailedPath, active.Next, "prepared update failed recovery");
        bool destinationExists = Directory.Exists(journal.DestinationPath);
        bool recoveryExists = Directory.Exists(active.RecoveryPath);
        if (destinationExists)
        {
            DeploymentTreeInventory actual = DeploymentTree.Create(journal.DestinationPath);
            if (DeploymentTree.EqualsExact(actual, active.Next.Inventory))
            {
                EnsureMoveTargetAbsent(active.FailedPath);
                Directory.CreateDirectory(Path.GetDirectoryName(active.FailedPath)!);
                AuthorFaultInjector.Hit("recover.update.failed.before-move");
                Directory.Move(journal.DestinationPath, active.FailedPath);
                AuthorFaultInjector.Hit("recover.update.failed.after-move");
                destinationExists = false;
            }
            else if (!DeploymentTree.EqualsExact(actual, active.Previous.Inventory))
            {
                throw new InvalidDataException("Prepared update destination matches neither prior nor next exact tree; refusing recovery.");
            }
        }
        if (destinationExists && recoveryExists)
            throw new InvalidDataException("Prepared update contains duplicate prior trees at destination and recovery; refusing to guess.");
        if (!destinationExists)
        {
            if (!recoveryExists)
                throw new InvalidDataException("Prepared update lost the prior recovery tree; refusing to guess.");
            VerifyTree(active.RecoveryPath, active.Previous, "prepared update recovery");
            AuthorFaultInjector.Hit("recover.update.restore.before-move");
            Directory.Move(active.RecoveryPath, journal.DestinationPath);
            AuthorFaultInjector.Hit("recover.update.restore.after-move");
        }
        VerifyTree(journal.DestinationPath, active.Previous, "restored update destination");
        journal.Committed = active.Previous;
    }

    private static void RecoverWithdraw(DeploymentJournal journal, DeploymentTransaction active)
    {
        if (active.Previous == null || active.Next != null || journal.Committed == null || !SameRecord(active.Previous, journal.Committed))
            throw new InvalidDataException("Prepared withdraw does not retain the exact prior committed record.");
        bool destinationExists = Directory.Exists(journal.DestinationPath);
        bool recoveryExists = Directory.Exists(active.RecoveryPath);
        if (destinationExists)
        {
            VerifyTree(journal.DestinationPath, active.Previous, "prepared withdraw destination");
            if (recoveryExists)
                throw new InvalidDataException("Prepared withdraw contains duplicate prior trees; refusing to guess.");
        }
        else
        {
            if (!recoveryExists)
                throw new InvalidDataException("Prepared withdraw recovery tree is missing; refusing to guess.");
            VerifyTree(active.RecoveryPath, active.Previous, "prepared withdraw recovery");
            AuthorFaultInjector.Hit("recover.withdraw.restore.before-move");
            Directory.Move(active.RecoveryPath, journal.DestinationPath);
            AuthorFaultInjector.Hit("recover.withdraw.restore.after-move");
        }
        journal.Committed = active.Previous;
    }

    private static DeploymentRecord VerifyCommitted(DeploymentJournal journal)
    {
        DeploymentRecord record = journal.Committed ?? throw new InvalidDataException("External journal has no committed deployment record.");
        VerifyTree(journal.DestinationPath, record, "committed destination");
        string receiptPath = Path.Combine(journal.DestinationPath, DeploymentTree.ReceiptFileName);
        if (!File.Exists(receiptPath) || !PathSafety.Sha256File(receiptPath).Equals(record.ReceiptSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package-local receipt is missing or does not match the external journal.");
        DeploymentReceipt receipt = ReadReceipt(receiptPath);
        if (journal.SchemaVersion == LegacyDeploymentSchemaVersion && receipt.SchemaVersion != LegacyDeploymentSchemaVersion)
            throw new InvalidDataException("Legacy schema-1 journal cannot authorize a non-historical schema-2 receipt/package binding.");
        DeploymentTreeInventory payload = DeploymentTree.Create(journal.DestinationPath, excludeReceipt: true);
        string expectedRelative = (IsOfficial(journal) ? "OfficialLocal/" : "Mods/") + journal.UniqueId;
        bool legacy = receipt.SchemaVersion == LegacyDeploymentSchemaVersion;
        ValidatedPackageBinding binding = DeploymentPackage.ValidateInstalledPackage(
            journal.DestinationPath,
            journal.GameRoot,
            legacy,
            journal.UniqueId,
            journal.PackageKind);
        bool commonMismatch = !receipt.TransactionId.Equals(record.TransactionId, StringComparison.Ordinal)
            || !receipt.GameRootKey.Equals(journal.GameRootKey, StringComparison.Ordinal)
            || !receipt.UniqueId.Equals(journal.UniqueId, StringComparison.Ordinal)
            || !receipt.PackageKind.Equals(journal.PackageKind, StringComparison.Ordinal)
            || !journal.CodeModKind.Equals(binding.CodeModKind, StringComparison.Ordinal)
            || !receipt.DestinationRelativePath.Equals(expectedRelative, StringComparison.Ordinal)
            || !receipt.PackageSha256.Equals(record.PackageSha256, StringComparison.OrdinalIgnoreCase)
            || !receipt.PayloadTreeSha256.Equals(record.PayloadTreeSha256, StringComparison.OrdinalIgnoreCase)
            || !receipt.PayloadTreeSha256.Equals(payload.TreeSha256, StringComparison.OrdinalIgnoreCase)
            || receipt.PayloadFileCount != payload.Files.Count
            || receipt.PayloadDirectoryCount != payload.Directories.Count;
        bool currentBindingMismatch = !legacy
            && (!receipt.CodeModKind.Equals(journal.CodeModKind, StringComparison.Ordinal)
                || !receipt.CodeModKind.Equals(binding.CodeModKind, StringComparison.Ordinal)
                || !receipt.ManifestSha256.Equals(binding.ManifestSha256, StringComparison.OrdinalIgnoreCase)
                || !receipt.EntryDllSha256.Equals(binding.EntryDllSha256, StringComparison.OrdinalIgnoreCase)
                || !receipt.AdvancedReferenceReceiptSha256.Equals(binding.AdvancedReferenceReceiptSha256, StringComparison.OrdinalIgnoreCase)
                || !receipt.PackageMarkerSha256.Equals(binding.PackageMarkerSha256, StringComparison.OrdinalIgnoreCase));
        if (commonMismatch || currentBindingMismatch)
            throw new InvalidDataException("Package-local receipt identity/root/path/kind/hash does not match journal plus exact current tree.");
        return record;
    }

    private static void VerifyTree(string path, DeploymentRecord record, string label)
    {
        if (!Directory.Exists(path))
            throw new InvalidDataException(label + " is missing: " + path);
        DeploymentTreeInventory actual = DeploymentTree.Create(path);
        if (!DeploymentTree.EqualsExact(actual, record.Inventory))
            throw new InvalidDataException(label + " has drift, an unknown addition, removal, or hash mismatch: " + path);
    }

    private static void VerifyOptionalTree(string path, DeploymentRecord record, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;
        VerifyTree(path, record, label);
    }

    private static DeploymentJournal LoadRequiredJournal(string gameRoot, string uniqueId)
    {
        string path = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
        if (!File.Exists(path))
            throw new InvalidDataException("External deployment journal is missing. A package receipt alone grants no mutation authority.");
        return ReadJournal(path);
    }

    private static DeploymentJournal ReadJournal(string path)
    {
        return ReadJournalBytes(File.ReadAllBytes(path), path);
    }

    private static DeploymentJournal ReadJournalBytes(byte[] bytes, string path)
    {
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException(Path.GetFileName(path) + " is not strict UTF-8.", ex);
        }
        int schemaVersion = ReadJournalSchemaVersionAndValidateProperties(text, path);
        DeploymentJournal journal = JsonSerializer.Deserialize<DeploymentJournal>(text, JsonSupport.Tool)
            ?? throw new InvalidDataException(Path.GetFileName(path) + " must contain one JSON object.");
        if (schemaVersion == LegacyDeploymentSchemaVersion)
        {
            if (journal.PackageKind == "CodeMod")
                journal.CodeModKind = "Strict";
            else if (journal.PackageKind == "ContentPack")
                journal.CodeModKind = string.Empty;
            else
                throw new InvalidDataException("Legacy external journal package kind is invalid.");
        }
        return journal;
    }

    private static int ReadJournalSchemaVersionAndValidateProperties(string text, string path)
    {
        using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(Path.GetFileName(path) + " must contain one JSON object.");
        int schemaVersion = document.RootElement.TryGetProperty("schemaVersion", out JsonElement schema)
            && schema.ValueKind == JsonValueKind.Number
            && schema.TryGetInt32(out int parsed)
                ? parsed
                : 0;
        string[] expected = schemaVersion switch
        {
            LegacyDeploymentSchemaVersion => LegacyJournalProperties,
            AuthorSdkContract.DeploymentSchemaVersion => CurrentJournalProperties,
            AuthorSdkContract.DeploymentJournalSchemaVersion => CompositeJournalProperties,
            AuthorSdkContract.OfficialDeploymentJournalSchemaVersion => CompositeJournalProperties,
            _ => throw new InvalidDataException(Path.GetFileName(path) + " has an unsupported deployment journal schemaVersion.")
        };
        string[] actual = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException(Path.GetFileName(path) + " has an unknown, missing, duplicate, or wrong-version deployment journal field.");
        return schemaVersion;
    }

    private static DeploymentReceipt ReadReceipt(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        ReadSchemaVersionAndValidateProperties(text, path, LegacyReceiptProperties, CurrentReceiptProperties);
        return JsonSerializer.Deserialize<DeploymentReceipt>(text, JsonSupport.Tool)
            ?? throw new InvalidDataException(Path.GetFileName(path) + " must contain one JSON object.");
    }

    private static RuntimeManifest ReadInstalledManifest(string destination)
    {
        string path = Path.Combine(destination, AuthorSdkContract.PackageManifestPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new InvalidDataException("Committed local install is missing its package manifest.");
        return JsonSerializer.Deserialize<RuntimeManifest>(File.ReadAllText(path, Encoding.UTF8), JsonSupport.RuntimeManifest)
            ?? throw new InvalidDataException("Committed local-install manifest must contain one JSON object.");
    }

    private static int ReadSchemaVersionAndValidateProperties(string text, string path, string[] legacyProperties, string[] currentProperties)
    {
        using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(Path.GetFileName(path) + " must contain one JSON object.");
        int schemaVersion = document.RootElement.TryGetProperty("schemaVersion", out JsonElement schema)
            && schema.ValueKind == JsonValueKind.Number
            && schema.TryGetInt32(out int parsed)
                ? parsed
                : 0;
        string[] expected = schemaVersion switch
        {
            LegacyDeploymentSchemaVersion => legacyProperties,
            AuthorSdkContract.DeploymentSchemaVersion => currentProperties,
            _ => throw new InvalidDataException(Path.GetFileName(path) + " has an unsupported deployment schemaVersion.")
        };
        string[] actual = document.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException(Path.GetFileName(path) + " has an unknown, missing, duplicate, or wrong-version deployment field.");
        return schemaVersion;
    }

    private static void ValidateJournal(DeploymentJournal journal, string gameRoot, string uniqueId, string? expectedKind, string? expectedCodeModKind = null)
    {
        string canonical = AuthorStatePaths.CanonicalGameRoot(gameRoot);
        string key = AuthorStatePaths.GameRootKey(canonical);
        string destination = IsOfficial(journal) ? OfficialLocalPaths.Destination(uniqueId) : Path.Combine(canonical, "Mods", uniqueId);
        if (journal.SchemaVersion is not (LegacyDeploymentSchemaVersion or AuthorSdkContract.DeploymentSchemaVersion or AuthorSdkContract.DeploymentJournalSchemaVersion or AuthorSdkContract.OfficialDeploymentJournalSchemaVersion)
            || !PathsEqual(journal.GameRoot, canonical)
            || !journal.GameRootKey.Equals(key, StringComparison.Ordinal)
            || !journal.UniqueId.Equals(uniqueId, StringComparison.Ordinal)
            || !PathsEqual(journal.DestinationPath, destination)
            || (expectedKind != null && !journal.PackageKind.Equals(expectedKind, StringComparison.Ordinal))
            || (expectedCodeModKind != null && !journal.CodeModKind.Equals(expectedCodeModKind, StringComparison.Ordinal))
            || journal.PackageKind is not ("CodeMod" or "ContentPack")
            || (journal.SchemaVersion == LegacyDeploymentSchemaVersion && journal.PackageKind == "CodeMod" && journal.CodeModKind != "Strict")
            || (journal.PackageKind == "CodeMod" && journal.CodeModKind is not ("Strict" or "Advanced"))
            || (journal.PackageKind == "ContentPack" && journal.CodeModKind.Length != 0))
            throw new InvalidDataException("External journal schema/root/path/UniqueID/package-kind identity is invalid.");
        if (journal.SchemaVersion != AuthorSdkContract.DeploymentJournalSchemaVersion && journal.LocalInstall != null)
            throw new InvalidDataException("Only the current deployment journal schema may contain local-install recovery state.");
        if (journal.Active != null && !journal.Status.Equals("Prepared", StringComparison.Ordinal))
            throw new InvalidDataException("External journal active transaction does not have Prepared status.");
        if (journal.Active == null && journal.Committed != null && !journal.Status.Equals("Installed", StringComparison.Ordinal))
            throw new InvalidDataException("External journal committed record does not have Installed status.");
        if (journal.Active == null && journal.Committed == null && journal.Status is not ("Absent" or "Withdrawn"))
            throw new InvalidDataException("External journal empty record has an invalid status.");
        PathSafety.RejectBepInExPluginDestination(journal.DestinationPath);
        PathSafety.RejectReparsePoints(Path.GetDirectoryName(journal.DestinationPath)!, new[] { journal.DestinationPath }.Where(path => Directory.Exists(path)));
    }

    private static void ValidateActivePaths(DeploymentJournal journal, DeploymentTransaction active, string gameRoot)
    {
        string modsRoot = IsOfficial(journal) ? OfficialLocalPaths.ModsRoot : Path.Combine(AuthorStatePaths.CanonicalGameRoot(gameRoot), "Mods");
        string hidden = HiddenRoot(modsRoot);
        string expectedStage = Path.Combine(hidden, "staging", active.TransactionId);
        string expectedFailed = Path.Combine(hidden, "failed", journal.UniqueId + "-" + active.TransactionId);
        if ((active.Operation is "deploy" or "update") && !PathsEqual(active.StagingPath, expectedStage))
            throw new InvalidDataException("Prepared transaction staging path is outside its exact same-volume transaction slot.");
        if (active.Operation is "deploy" or "update")
        {
            if (!PathsEqual(active.FailedPath, expectedFailed))
                throw new InvalidDataException("Prepared transaction failed-recovery path is invalid.");
        }
        if (active.Operation == "update")
        {
            string previousId = active.Previous?.TransactionId ?? string.Empty;
            string expected = Path.Combine(hidden, "recovery", journal.UniqueId + "-" + previousId + "-" + active.TransactionId);
            if (!PathsEqual(active.RecoveryPath, expected))
                throw new InvalidDataException("Prepared update recovery path is invalid.");
        }
        if (active.Operation == "withdraw")
        {
            string previousId = active.Previous?.TransactionId ?? string.Empty;
            string expected = Path.Combine(hidden, "recovery", journal.UniqueId + "-" + previousId + "-withdraw-" + active.TransactionId);
            if (!PathsEqual(active.RecoveryPath, expected) || active.StagingPath.Length != 0 || active.FailedPath.Length != 0)
                throw new InvalidDataException("Prepared withdraw recovery paths are invalid.");
        }
    }

    private static DeploymentJournal NewJournal(string gameRoot, string uniqueId, string packageKind, string codeModKind, string destination) => new()
    {
        SchemaVersion = PathsEqual(destination, OfficialLocalPaths.Destination(uniqueId)) ? AuthorSdkContract.OfficialDeploymentJournalSchemaVersion : AuthorSdkContract.DeploymentJournalSchemaVersion,
        GameRoot = AuthorStatePaths.CanonicalGameRoot(gameRoot),
        GameRootKey = AuthorStatePaths.GameRootKey(gameRoot),
        UniqueId = uniqueId,
        PackageKind = packageKind,
        CodeModKind = codeModKind,
        DestinationPath = destination,
        Status = "Absent"
    };

    private static void PreservePreparedArtifacts(DeploymentJournal journal, DeploymentTransaction active)
    {
        if (Directory.Exists(active.StagingPath) && active.Next != null)
            AddArtifact(journal, "prepared-staging", active.StagingPath, active.Next.Inventory.TreeSha256);
        if (Directory.Exists(active.FailedPath) && active.Next != null)
            AddArtifact(journal, "failed-next-tree", active.FailedPath, active.Next.Inventory.TreeSha256);
    }

    private static void AddArtifact(DeploymentJournal journal, string role, string path, string tree)
    {
        if (journal.RecoveryArtifacts.Any(artifact => PathsEqual(artifact.Path, path)))
            return;
        journal.RecoveryArtifacts.Add(new DeploymentRecoveryArtifact { Role = role, Path = path, TreeSha256 = tree });
    }

    private static bool SameRecord(DeploymentRecord left, DeploymentRecord right) =>
        left.TransactionId.Equals(right.TransactionId, StringComparison.Ordinal)
        && left.PackageSha256.Equals(right.PackageSha256, StringComparison.OrdinalIgnoreCase)
        && left.PayloadTreeSha256.Equals(right.PayloadTreeSha256, StringComparison.OrdinalIgnoreCase)
        && left.ReceiptSha256.Equals(right.ReceiptSha256, StringComparison.OrdinalIgnoreCase)
        && DeploymentTree.EqualsExact(left.Inventory, right.Inventory);

    private static void WriteJournal(string path, DeploymentJournal journal, string point)
    {
        if (journal.SchemaVersion != AuthorSdkContract.OfficialDeploymentJournalSchemaVersion)
            journal.SchemaVersion = AuthorSdkContract.DeploymentJournalSchemaVersion;
        if (journal.PackageKind == "CodeMod" && journal.CodeModKind is not ("Strict" or "Advanced"))
            throw new InvalidDataException("Cannot write a CodeMod journal without an explicit schema-2 CodeModKind.");
        if (journal.PackageKind == "ContentPack" && journal.CodeModKind.Length != 0)
            throw new InvalidDataException("Cannot write a ContentPack journal with CodeModKind.");
        AuthorFaultInjector.Hit(point + ".before-journal");
        AtomicStateFile.Write(path, journal);
        AuthorFaultInjector.Hit(point + ".after-journal");
    }

    private static void TryRollbackAfterFailure(string gameRoot, string uniqueId, CommandReport report)
    {
        try
        {
            // Both callers retain their operation lock (and official cold-game
            // lease) through this rollback. Reacquiring would reject ourselves.
            string path = AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId);
            if (!File.Exists(path))
                return;
            DeploymentJournal journal = ReadJournal(path);
            ValidateJournal(journal, gameRoot, uniqueId, null);
            if (RecoverLocalInstall(journal, gameRoot) || RecoverPrepared(journal, gameRoot))
            {
                report.Values["rollback"] = "restored-last-committed";
                report.Diagnostics.Add(Warning("SDK406", "The failed prepared transaction was rolled back; staging/failed material was retained where available.", path));
            }
        }
        catch (Exception recoveryError)
        {
            report.Values["rollback"] = "blocked-preserved";
            report.Diagnostics.Add(Error("SDK407", "Automatic rollback refused: " + recoveryError.Message + " Journal/recovery material was preserved for explicit inspection.", AuthorStatePaths.DeploymentJournalPath(gameRoot, uniqueId)));
        }
    }

    private static void RollbackLocalInstall(
        string gameRoot,
        string uniqueId,
        string destination,
        string stagingPath,
        string failedPath,
        string recoveryPath,
        DeploymentRecord? previous,
        DeploymentRecord next,
        ExactFileSnapshot journalBefore,
        ExactFileSnapshot sourceBefore)
    {
        string expectedDestination = Path.Combine(AuthorStatePaths.CanonicalGameRoot(gameRoot), "Mods", uniqueId);
        if (!PathsEqual(destination, expectedDestination))
            throw new InvalidDataException("Local-install rollback destination escaped its exact managed product path.");

        bool destinationExists = Directory.Exists(destination);
        if (File.Exists(destination))
            throw new InvalidDataException("Local-install rollback found a file where the managed product directory belongs.");
        if (destinationExists)
        {
            DeploymentTreeInventory actual = DeploymentTree.Create(destination);
            if (DeploymentTree.EqualsExact(actual, next.Inventory))
            {
                if (Directory.Exists(failedPath) || File.Exists(failedPath))
                    throw new InvalidDataException("Local-install rollback failed-tree slot already exists.");
                Directory.CreateDirectory(Path.GetDirectoryName(failedPath)!);
                Directory.Move(destination, failedPath);
                destinationExists = false;
            }
            else if (previous == null || !DeploymentTree.EqualsExact(actual, previous.Inventory))
            {
                throw new InvalidDataException("Local-install rollback destination matches neither its prior nor attempted exact tree.");
            }
        }

        if (previous != null)
        {
            bool recoveryExists = Directory.Exists(recoveryPath);
            if (File.Exists(recoveryPath))
                throw new InvalidDataException("Local-install rollback found a file where the exact recovery directory belongs.");
            if (destinationExists && recoveryExists)
                throw new InvalidDataException("Local-install rollback found duplicate prior destinations and refused to guess.");
            if (!destinationExists)
            {
                if (!recoveryExists)
                    throw new InvalidDataException("Local-install rollback lost the exact prior recovery tree.");
                VerifyTree(recoveryPath, previous, "local-install prior recovery");
                Directory.Move(recoveryPath, destination);
                destinationExists = true;
            }
            VerifyTree(destination, previous, "local-install restored destination");
        }
        else if (destinationExists)
        {
            throw new InvalidDataException("Local-install rollback could not restore the previously absent destination.");
        }

        AuthorFaultInjector.Hit("install-local.recover.source.before-state");
        sourceBefore.Restore();
        if (!sourceBefore.MatchesCurrent())
            throw new InvalidDataException("Local-install rollback did not restore exact source-state bytes.");

        if (previous != null)
            VerifyTree(destination, previous, "local-install final restored destination");
        else if (Directory.Exists(destination) || File.Exists(destination))
            throw new InvalidDataException("Local-install final destination is not absent.");

        TryDeleteOwnedDirectory(stagingPath);
        TryDeleteOwnedDirectory(failedPath);
        TryDeleteEmptyDirectory(Path.GetDirectoryName(stagingPath)!);
        TryDeleteEmptyDirectory(Path.GetDirectoryName(failedPath)!);
        if (recoveryPath.Length > 0)
            TryDeleteEmptyDirectory(Path.GetDirectoryName(recoveryPath)!);

        // Clear the durable composite authority last. If source restoration or
        // owned-tree cleanup fails, the current journal still contains every
        // byte/path needed by a later explicit recover retry.
        AuthorFaultInjector.Hit("install-local.recover.journal.before-state");
        journalBefore.Restore();
        AuthorFaultInjector.Hit("install-local.recover.journal.after-state");
        if (!journalBefore.MatchesCurrent())
            throw new InvalidDataException("Local-install rollback did not restore exact deployment-journal bytes.");
    }

    private static void TryDeleteOwnedDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;
        Directory.Delete(path, recursive: true);
    }

    private static void TryDeleteOwnedFile(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            File.Delete(path);
    }

    private static void TryDeleteOwnedFileWithoutThrow(string path)
    {
        try
        {
            TryDeleteOwnedFile(path);
        }
        catch
        {
            // A validated install result must not become an unknown commit just
            // because its immutable input-copy cleanup was denied.
        }
    }

    private static void TryDeleteOwnedDirectoryWithoutThrow(string path)
    {
        try
        {
            TryDeleteOwnedDirectory(path);
        }
        catch
        {
            // The transaction result is authoritative; a leftover owned stage
            // is diagnostic cleanup debt, not a reason to misreport commit.
        }
    }

    private static void TryDeleteEmptyDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) &&
            Directory.Exists(path) &&
            !Directory.EnumerateFileSystemEntries(path).Any())
        {
            Directory.Delete(path);
        }
    }

    private static void TryDeleteEmptyDirectoryWithoutThrow(string path)
    {
        try
        {
            TryDeleteEmptyDirectory(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static void EnsureMoveTargetAbsent(string path)
    {
        if (Directory.Exists(path) || File.Exists(path))
            throw new InvalidDataException("Transaction move target already exists; refusing overwrite: " + path);
    }

    private static string RequireGameRoot(ParsedCommand command)
    {
        string value = command.Option("game-root");
        if (string.IsNullOrWhiteSpace(value))
            throw new CommandLineException("--game-root is required.");
        string root = AuthorStatePaths.CanonicalGameRoot(value);
        if (!Directory.Exists(root))
            throw new CommandLineException("Game root does not exist: " + root);
        return root;
    }

    private static string EnsureModsRoot(string gameRoot)
    {
        string root = Path.Combine(AuthorStatePaths.CanonicalGameRoot(gameRoot), "Mods");
        PathSafety.RejectBepInExPluginDestination(root);
        Directory.CreateDirectory(root);
        return root;
    }

    private static string HiddenRoot(string modsRoot) => Path.Combine(modsRoot, ".dtmapi-author");
    private static bool IsOfficial(DeploymentJournal journal) => journal.SchemaVersion == AuthorSdkContract.OfficialDeploymentJournalSchemaVersion;
    private static bool PathsEqual(string left, string right) => string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    private static CommandReport NewReport(string command, string gameRoot) => new() { Command = command, RootPath = AuthorStatePaths.CanonicalGameRoot(gameRoot) };

    private sealed class ExactFileSnapshot
    {
        private ExactFileSnapshot(string path, bool existed, byte[] bytes)
        {
            Path = path;
            Existed = existed;
            Bytes = bytes;
        }

        public string Path { get; }
        public bool Existed { get; }
        public byte[] Bytes { get; }

        public static ExactFileSnapshot Capture(string path)
        {
            bool existed = File.Exists(path);
            return new ExactFileSnapshot(path, existed, existed ? File.ReadAllBytes(path) : Array.Empty<byte>());
        }

        public static ExactFileSnapshot FromBase64(string path, bool existed, string base64)
        {
            byte[] bytes = DecodeBase64(base64);
            if (!existed && bytes.Length != 0)
                throw new InvalidDataException("An absent exact file snapshot cannot contain bytes.");
            if (existed && bytes.Length == 0)
                throw new InvalidDataException("An existing exact file snapshot cannot be empty.");
            return new ExactFileSnapshot(path, existed, bytes);
        }

        public static byte[] DecodeBase64(string value)
        {
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("Exact file snapshot is not valid base64.", ex);
            }
        }

        public string ToBase64() => Convert.ToBase64String(Bytes);

        public void Restore()
        {
            if (Existed)
                AtomicStateFile.WriteBytes(Path, Bytes);
            else if (File.Exists(Path))
                File.Delete(Path);
        }

        public bool MatchesCurrent()
        {
            if (!Existed)
                return !File.Exists(Path);
            return File.Exists(Path) && File.ReadAllBytes(Path).SequenceEqual(Bytes);
        }
    }

    private static CommandReport Finish(CommandReport report)
    {
        report.Success = report.Success && report.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        return report;
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Warning(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DiagnosticSeverity.Info, Message = message };
}
