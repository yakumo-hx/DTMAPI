using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed partial class EquipmentSlotDocumentStore
    {
        private static readonly object
            PreSchemaGlobalClaimSync = new object();
        private const string PreSchemaGlobalClaimPending =
            "pending";
        private const string PreSchemaGlobalClaimCompleted =
            "completed";
        private readonly Action<string>? migrationFault;

        internal EquipmentSlotDocumentStore(
            Action<string>? migrationFault = null)
        {
            this.migrationFault = migrationFault;
        }

        internal EquipmentSlotStorageDocument LoadOrMigrate(
            string path,
            string legacyGlobalPath,
            EquipmentSlotSaveScope expectedScope,
            out string migrationMessage)
        {
            if (expectedScope == null)
                throw new ArgumentNullException(nameof(expectedScope));
            expectedScope.Normalize();
            migrationMessage = string.Empty;

            if (TryLoadValidated(
                path,
                expectedScope,
                out EquipmentSlotStorageDocument? loaded,
                out string loadedSourcePath,
                out string loadFailure))
            {
                if (!string.Equals(
                        loadedSourcePath,
                        path,
                        StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(path))
                {
                    EquipmentSlotStorageFormatProbe liveFormat =
                        EquipmentSlotStorageFormatClassifier.Probe(
                            path);
                    if (liveFormat.Format ==
                            EquipmentSlotStorageFormat.Ambiguous ||
                        liveFormat.Format ==
                            EquipmentSlotStorageFormat
                                .UnsupportedProduct ||
                        liveFormat.Format ==
                            EquipmentSlotStorageFormat
                                .UnsupportedLegacy)
                    {
                        throw new InvalidDataException(
                            "The canonical live sidecar declares an unsupported or ambiguous generation and cannot fall back to Product previous: " +
                            liveFormat.Format +
                            "; " +
                            liveFormat.Failure);
                    }
                }
                FinalizeExactGlobalMigration(
                    legacyGlobalPath,
                    path,
                    loaded!,
                    out migrationMessage);
                return loaded!;
            }

            if (File.Exists(path))
            {
                EquipmentSlotStorageFormatProbe probe =
                    EquipmentSlotStorageFormatClassifier.Probe(
                        path);
                if (probe.Format ==
                    EquipmentSlotStorageFormat.LegacyFlat)
                {
                    return MigrateLegacy(
                        path,
                        path,
                        expectedScope,
                        globalSource: false,
                        out migrationMessage);
                }
                throw new InvalidDataException(
                    "The current equipment-slot sidecar is neither a valid Product v3 document nor a supported scoped flat legacy generation: product=" +
                    loadFailure +
                    "; format=" +
                    probe.Format +
                    "; formatFailure=" +
                    probe.Failure);
            }

            if (File.Exists(path + ".previous"))
            {
                throw new InvalidDataException(
                    "The live Product sidecar is missing and its previous generation is not valid: " +
                    loadFailure);
            }

            if (!string.IsNullOrWhiteSpace(legacyGlobalPath) &&
                File.Exists(legacyGlobalPath))
            {
                EquipmentSlotStorageFormatProbe probe =
                    EquipmentSlotStorageFormatClassifier.Probe(
                        legacyGlobalPath);
                if (probe.Format !=
                        EquipmentSlotStorageFormat.LegacyFlat &&
                    probe.Format !=
                        EquipmentSlotStorageFormat.PreSchemaGlobal)
                {
                    throw new InvalidDataException(
                        "The legacy global equipment-slot candidate is not a supported flat generation or exact pre-schema document: " +
                        probe.Format +
                        "; " +
                        probe.Failure);
                }
                return MigrateLegacy(
                    legacyGlobalPath,
                    path,
                    expectedScope,
                    globalSource: true,
                    out migrationMessage);
            }

            EnsureNoPendingPreSchemaGlobalClaimBeforeEmpty(
                legacyGlobalPath,
                path,
                expectedScope);
            return CreateEmpty(expectedScope);
        }

        private EquipmentSlotStorageDocument MigrateLegacy(
            string sourcePath,
            string targetPath,
            EquipmentSlotSaveScope expectedScope,
            bool globalSource,
            out string migrationMessage)
        {
            migrationMessage = string.Empty;
            if (!EquipmentSlotLegacyMigration.TryConvert(
                sourcePath,
                expectedScope,
                globalSource,
                out EquipmentSlotStorageDocument? converted,
                out string sourceSha256,
                out string failure))
            {
                throw new InvalidDataException(
                    "The flat legacy equipment-slot sidecar could not be converted without data loss: " +
                    failure);
            }

            EquipmentSlotLegacyMigrationStamp stamp =
                converted!.LegacyMigration!;
            if (!TryValidateRawV3(
                    converted,
                    expectedScope,
                    out string convertedFailure))
            {
                throw new InvalidDataException(
                    "The converted Product v3 document is invalid before any migration authority can be published: " +
                    convertedFailure);
            }
            if (string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal))
            {
                EnsurePreSchemaGlobalClaim(
                    sourcePath,
                    targetPath,
                    expectedScope,
                    sourceSha256);
            }

            string backupPath =
                EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                    targetPath,
                    sourceSha256);
            EnsureExactLegacyBackup(
                sourcePath,
                backupPath,
                sourceSha256);
            WriteMigratedProduct(
                sourcePath,
                targetPath,
                sourceSha256,
                converted);

            if (globalSource)
            {
                FinalizeExactGlobalMigration(
                    sourcePath,
                    targetPath,
                    converted,
                    out string archiveMessage);
                migrationMessage =
                    "Migrated " +
                    (stamp.SourceSchema == 0
                        ? "exact pre-schema global"
                        : "legacy global flat schema " +
                            stamp.SourceSchema) +
                    " to Product v3 and " +
                    archiveMessage;
            }
            else
            {
                migrationMessage =
                    "Migrated scoped flat schema " +
                    stamp.SourceSchema +
                    " to Product v3; exact source backup=" +
                    backupPath +
                    ".";
            }
            return converted;
        }

        private void EnsureExactLegacyBackup(
            string sourcePath,
            string backupPath,
            string expectedSha256)
        {
            string sourceHash =
                EquipmentSlotLegacyMigration.ComputeFileSha256(
                    sourcePath);
            if (!string.Equals(
                    sourceHash,
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The flat legacy source changed before backup publication.");
            }

            string? directory =
                Path.GetDirectoryName(backupPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "The exact legacy backup path has no directory.");
            }
            Directory.CreateDirectory(directory);

            if (File.Exists(backupPath) &&
                string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(backupPath),
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                return;
            }

            string rejectedPath =
                Path.Combine(
                    directory,
                    ".invalid-" +
                    Guid.NewGuid().ToString("N"));
            if (File.Exists(backupPath))
                File.Move(backupPath, rejectedPath);

            string temporaryPath =
                Path.Combine(
                    directory,
                    ".tmp-" +
                    Guid.NewGuid().ToString("N"));
            try
            {
                migrationFault?.Invoke(
                    "before-backup-temp-write");
                using (var source = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                using (var temporary = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    source.CopyTo(temporary);
                    temporary.Flush(flushToDisk: true);
                }
                migrationFault?.Invoke(
                    "after-backup-temp-flush");

                string temporaryHash =
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(temporaryPath);
                if (!string.Equals(
                        temporaryHash,
                        expectedSha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The temporary flat legacy backup hash does not match the migration source.");
                }

                migrationFault?.Invoke(
                    "before-backup-publish");
                if (File.Exists(backupPath))
                {
                    string concurrentHash =
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(backupPath);
                    if (!string.Equals(
                            concurrentHash,
                            expectedSha256,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "A different backup appeared before exact legacy backup publication.");
                    }
                }
                else
                {
                    File.Move(temporaryPath, backupPath);
                }
                migrationFault?.Invoke(
                    "after-backup-publish");

                string backupHash =
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(backupPath);
                if (!string.Equals(
                        backupHash,
                        expectedSha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The published flat legacy backup hash does not match the migration source.");
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                if (File.Exists(rejectedPath))
                {
                    try
                    {
                        File.Delete(rejectedPath);
                    }
                    catch
                    {
                        // A rejected partial backup is not an authority.
                    }
                }
            }
        }

        private void WriteMigratedProduct(
            string sourcePath,
            string targetPath,
            string sourceSha256,
            EquipmentSlotStorageDocument document)
        {
            string currentSourceHash =
                EquipmentSlotLegacyMigration.ComputeFileSha256(
                    sourcePath);
            if (!string.Equals(
                    currentSourceHash,
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The flat legacy source changed before Product publication.");
            }
            if (!TryValidateRawV3(
                document,
                document.Scope,
                out string validationFailure))
            {
                throw new InvalidDataException(
                    "The converted Product v3 document is invalid: " +
                    validationFailure);
            }

            string? directory =
                Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            string temporaryPath =
                targetPath +
                ".migration-tmp-" +
                Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreateSerializer().WriteObject(
                        stream,
                        document);
                    stream.Flush(flushToDisk: true);
                }
                if (!TryReadValidated(
                    temporaryPath,
                    document.Scope,
                    out EquipmentSlotStorageDocument? roundTrip,
                    out string roundTripFailure) ||
                    !JournalIdentityMatches(
                        document,
                        roundTrip!))
                {
                    throw new InvalidDataException(
                        "The converted Product v3 temporary document failed exact round-trip validation: " +
                        roundTripFailure);
                }

                migrationFault?.Invoke(
                    "before-product-publish");
                if (File.Exists(targetPath))
                {
                    if (!string.Equals(
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(targetPath),
                        sourceSha256,
                        StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "The scoped flat legacy authority drifted before atomic Product replacement.");
                    }
                    File.Replace(
                        temporaryPath,
                        targetPath,
                        destinationBackupFileName: null,
                        ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                }
                migrationFault?.Invoke(
                    "after-product-publish");
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private void FinalizeExactGlobalMigration(
            string globalPath,
            string targetPath,
            EquipmentSlotStorageDocument document,
            out string message)
        {
            message = string.Empty;
            EquipmentSlotLegacyMigrationStamp? stamp =
                document.LegacyMigration;
            if (stamp == null ||
                (!string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .GlobalFlat,
                    StringComparison.Ordinal) &&
                 !string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal)))
            {
                return;
            }
            bool isPreSchemaGlobal =
                string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(globalPath))
                return;

            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    stamp.SourceSha256);
            RecoverInterruptedGlobalCapture(
                globalPath,
                archivePath,
                targetPath,
                stamp.SourceSha256);
            if (!File.Exists(globalPath))
            {
                ValidateFinalGlobalArchive(
                    globalPath,
                    archivePath,
                    stamp.SourceSha256);
                if (isPreSchemaGlobal)
                {
                    CompletePreSchemaGlobalClaim(
                        globalPath,
                        targetPath,
                        document.Scope,
                        stamp.SourceSha256);
                }
                message =
                    "the exact legacy global source was already absent.";
                return;
            }

            if (isPreSchemaGlobal)
            {
                EnsurePreSchemaGlobalClaim(
                    globalPath,
                    targetPath,
                    document.Scope,
                    stamp.SourceSha256);
            }
            string currentHash =
                EquipmentSlotLegacyMigration.ComputeFileSha256(
                    globalPath);
            if (!string.Equals(
                    currentHash,
                    stamp.SourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The legacy global source drifted after Product publication; it was retained fail-closed.");
            }
            migrationFault?.Invoke(
                "before-global-archive");
            CaptureAndArchiveExactGlobalSource(
                globalPath,
                archivePath,
                currentHash);
            migrationFault?.Invoke(
                "after-global-archive");
            ValidateFinalGlobalArchive(
                globalPath,
                archivePath,
                currentHash);
            if (isPreSchemaGlobal)
            {
                CompletePreSchemaGlobalClaim(
                    globalPath,
                    targetPath,
                    document.Scope,
                    currentHash);
            }
            message =
                "archived the exact global source at " +
                archivePath +
                ".";
        }

        private static void RecoverInterruptedGlobalCapture(
            string globalPath,
            string archivePath,
            string targetPath,
            string expectedSha256)
        {
            string? directory =
                Path.GetDirectoryName(globalPath);
            if (string.IsNullOrWhiteSpace(directory) ||
                !Directory.Exists(directory))
            {
                return;
            }

            string[] capturePaths =
                Directory.GetFiles(
                    directory,
                    Path.GetFileName(globalPath) +
                    ".migration-capture-*",
                    SearchOption.TopDirectoryOnly);
            if (capturePaths.Length == 0)
                return;
            if (capturePaths.Length != 1)
            {
                throw new InvalidDataException(
                    "Multiple interrupted legacy global captures require explicit recovery.");
            }

            string capturePath =
                capturePaths[0];
            string captureHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        capturePath);
            if (!string.Equals(
                    captureHash,
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The interrupted legacy global capture does not match the Product migration source.");
            }

            string backupPath =
                EquipmentSlotLegacyMigration
                    .GetLegacyBackupPath(
                        targetPath,
                        expectedSha256);
            if (!File.Exists(backupPath) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(
                            backupPath),
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The interrupted legacy global capture cannot resume without its exact scoped source backup.");
            }
            if (File.Exists(globalPath))
            {
                throw new InvalidDataException(
                    "The interrupted legacy global capture conflicts with a recreated active global source.");
            }

            if (File.Exists(archivePath))
            {
                if (!string.Equals(
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(
                                archivePath),
                        expectedSha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The interrupted legacy global capture conflicts with its deterministic archive.");
                }
                File.Delete(
                    capturePath);
                return;
            }

            try
            {
                File.Move(
                    capturePath,
                    archivePath);
            }
            catch (IOException)
                when (File.Exists(archivePath))
            {
                if (!string.Equals(
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(
                                archivePath),
                        expectedSha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "A different deterministic archive appeared while the interrupted global capture was resuming.");
                }
                File.Delete(
                    capturePath);
            }
            if (!File.Exists(archivePath) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(
                            archivePath),
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The interrupted legacy global capture did not publish its exact deterministic archive.");
            }
        }

        private static void ValidateFinalGlobalArchive(
            string globalPath,
            string archivePath,
            string expectedSha256)
        {
            if (File.Exists(globalPath))
            {
                throw new InvalidDataException(
                    "The active legacy global source was recreated before migration reached its terminal state.");
            }
            if (!File.Exists(archivePath) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(
                            archivePath),
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The legacy global migration did not retain its exact deterministic archive.");
            }
        }

        private void CaptureAndArchiveExactGlobalSource(
            string globalPath,
            string archivePath,
            string expectedSha256)
        {
            string capturePath =
                globalPath +
                ".migration-capture-" +
                Guid.NewGuid().ToString("N");
            bool captureExists = false;
            try
            {
                File.Move(
                    globalPath,
                    capturePath);
                captureExists = true;
                migrationFault?.Invoke(
                    "after-global-capture-before-hash");
                string capturedHash =
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(
                            capturePath);
                if (!string.Equals(
                    capturedHash,
                    expectedSha256,
                    StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The legacy global source changed before atomic archive capture.");
                }

                if (File.Exists(archivePath))
                {
                    string archiveHash =
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(
                                archivePath);
                    if (!string.Equals(
                        archiveHash,
                        expectedSha256,
                        StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "The deterministic legacy global archive path contains different bytes.");
                    }
                    File.Delete(
                        capturePath);
                    captureExists = false;
                    return;
                }

                File.Move(
                    capturePath,
                    archivePath);
                captureExists = false;
            }
            catch
            {
                if (captureExists &&
                    File.Exists(capturePath) &&
                    !File.Exists(globalPath))
                {
                    try
                    {
                        File.Move(
                            capturePath,
                            globalPath);
                        captureExists = false;
                    }
                    catch
                    {
                        // The captured bytes remain at the unique
                        // same-volume quarantine path for explicit recovery.
                    }
                }
                throw;
            }
        }

        private void EnsurePreSchemaGlobalClaim(
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            lock (PreSchemaGlobalClaimSync)
            {
                EnsurePreSchemaGlobalClaimCore(
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256);
            }
        }

        private void EnsurePreSchemaGlobalClaimCore(
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            ValidatePreSchemaGlobalClaimSourceState(
                globalPath,
                targetPath,
                scope,
                sourceSha256);

            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceSha256);
            string claimDirectory =
                Path.GetDirectoryName(claimPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global claim path has no directory.");
            Directory.CreateDirectory(claimDirectory);

            migrationFault?.Invoke(
                "after-preschema-claim-enumeration");
            var claim =
                new PreSchemaGlobalClaim
                {
                    SourceSha256 = sourceSha256,
                    GlobalFileName =
                        Path.GetFileName(globalPath),
                    TargetFileName =
                        Path.GetFileName(targetPath),
                    Scope = scope.Clone(),
                    State =
                        PreSchemaGlobalClaimPending
                };
            string temporaryPath =
                Path.Combine(
                    claimDirectory,
                    ".tmp-" +
                    Guid.NewGuid().ToString("N"));
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreatePreSchemaGlobalClaimSerializer()
                        .WriteObject(stream, claim);
                    stream.Flush(flushToDisk: true);
                }
                PreSchemaGlobalClaim roundTrip =
                    ReadPreSchemaGlobalClaim(temporaryPath);
                if (!PreSchemaGlobalClaimMatches(
                    roundTrip,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256))
                {
                    throw new InvalidDataException(
                        "The pending pre-schema global migration claim failed exact round-trip validation.");
                }

                migrationFault?.Invoke(
                    "before-preschema-claim-publish");
                ValidatePreSchemaGlobalClaimSourceState(
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256);
                migrationFault?.Invoke(
                    "after-preschema-claim-state-check-before-publish");
                using (AcquirePreSchemaGlobalOperationLock(
                    globalPath))
                {
                    string winnerPath =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalWinnerPath(
                                globalPath);
                    if (File.Exists(winnerPath))
                    {
                        ValidateExactPreSchemaGlobalClaim(
                            winnerPath,
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256,
                            PreSchemaGlobalClaimPending);
                        ValidatePreSchemaGlobalClaimSourceState(
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256);
                        EnsureNoConflictingPublishedPreSchemaProduct(
                            targetPath,
                            scope,
                            sourceSha256);
                        return;
                    }

                    ReconcilePreSchemaClaimTransitions(
                        claimDirectory,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    ValidatePendingPreSchemaGlobalClaims(
                        claimDirectory,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    ValidatePreSchemaGlobalClaimSourceState(
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    EnsureNoConflictingPublishedPreSchemaProduct(
                        targetPath,
                        scope,
                        sourceSha256);
                    EnsurePreSchemaGlobalWinner(
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256,
                        PreSchemaGlobalClaimPending);

                    bool publishedByThisCall = false;
                    if (File.Exists(claimPath))
                    {
                        ValidateExactPreSchemaGlobalClaim(
                            claimPath,
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256);
                    }
                    else
                    {
                        try
                        {
                            File.Move(
                                temporaryPath,
                                claimPath);
                            publishedByThisCall = true;
                            migrationFault?.Invoke(
                                "after-preschema-claim-file-publish-before-revalidation");
                        }
                        catch (IOException)
                            when (File.Exists(claimPath))
                        {
                            ValidateExactPreSchemaGlobalClaim(
                                claimPath,
                                globalPath,
                                targetPath,
                                scope,
                                sourceSha256);
                        }
                    }
                    ValidatePendingPreSchemaGlobalClaims(
                        claimDirectory,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    ValidateExactPreSchemaGlobalClaim(
                        winnerPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    ValidateExactPreSchemaGlobalClaim(
                        claimPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    try
                    {
                        ValidatePreSchemaGlobalClaimSourceState(
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256);
                    }
                    catch (Exception validationFailure)
                    {
                        if (publishedByThisCall)
                        {
                            try
                            {
                                ValidateExactPreSchemaGlobalClaim(
                                    claimPath,
                                    globalPath,
                                    targetPath,
                                    scope,
                                    sourceSha256);
                                File.Delete(
                                    claimPath);
                            }
                            catch (Exception cleanupFailure)
                            {
                                throw new InvalidDataException(
                                    "The late pre-schema claim could not be withdrawn after its source authority changed.",
                                    new AggregateException(
                                        validationFailure,
                                        cleanupFailure));
                            }
                        }
                        throw;
                    }
                    migrationFault?.Invoke(
                        "after-preschema-claim-publish");
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void
            ValidatePreSchemaGlobalClaimSourceState(
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256)
        {
            if (!File.Exists(globalPath))
            {
                throw new InvalidDataException(
                    "The pre-schema global source disappeared before claim publication completed.");
            }
            if (!string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(globalPath),
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The pre-schema global source changed before claim publication completed.");
            }

            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    sourceSha256);
            if (File.Exists(archivePath))
            {
                if (!string.Equals(
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(archivePath),
                        sourceSha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The deterministic pre-schema global archive contains different bytes.");
                }
                throw new InvalidDataException(
                    "The exact pre-schema global source hash was already claimed by another migration and cannot be adopted by this save.");
            }

        }

        private static FileStream
            AcquirePreSchemaGlobalOperationLock(
                string globalPath)
        {
            string lockPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalOperationLockPath(
                        globalPath);
            string directory =
                Path.GetDirectoryName(lockPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global operation lock has no directory.");
            Directory.CreateDirectory(
                directory);
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException ex)
            {
                throw new InvalidDataException(
                    "Another process is changing the pre-schema global migration winner.",
                    ex);
            }
        }

        private static void EnsurePreSchemaGlobalWinner(
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string state)
        {
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);
            if (File.Exists(winnerPath))
            {
                ValidateExactPreSchemaGlobalClaim(
                    winnerPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    state);
                return;
            }

            var winner =
                new PreSchemaGlobalClaim
                {
                    SourceSha256 =
                        sourceSha256,
                    GlobalFileName =
                        Path.GetFileName(
                            globalPath),
                    TargetFileName =
                        Path.GetFileName(
                            targetPath),
                    Scope =
                        scope.Clone(),
                    State =
                        state
                };
            PublishNewPreSchemaGlobalClaim(
                winnerPath,
                globalPath,
                targetPath,
                scope,
                sourceSha256,
                state,
                winner);
        }

        private static void PublishNewPreSchemaGlobalClaim(
            string claimPath,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string state,
            PreSchemaGlobalClaim claim)
        {
            string directory =
                Path.GetDirectoryName(claimPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global claim path has no directory.");
            Directory.CreateDirectory(
                directory);
            string temporaryPath =
                Path.Combine(
                    directory,
                    ".tmp-claim-" +
                    Guid.NewGuid().ToString("N"));
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreatePreSchemaGlobalClaimSerializer()
                        .WriteObject(
                            stream,
                            claim);
                    stream.Flush(
                        flushToDisk: true);
                }
                ValidateExactPreSchemaGlobalClaim(
                    temporaryPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    state);
                try
                {
                    File.Move(
                        temporaryPath,
                        claimPath);
                }
                catch (IOException)
                    when (File.Exists(claimPath))
                {
                    ValidateExactPreSchemaGlobalClaim(
                        claimPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256,
                        state);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void
            ValidatePendingPreSchemaGlobalClaims(
                string claimDirectory,
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256,
                string? expectedState =
                    PreSchemaGlobalClaimPending)
        {
            foreach (string pendingClaimPath in
                Directory.GetFiles(
                    claimDirectory,
                    "*.json",
                    SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(
                    pendingClaimPath,
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalWinnerPath(
                            globalPath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                ValidateExactPreSchemaGlobalClaim(
                    pendingClaimPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    expectedState);
            }
        }

        private static void ValidateExactPreSchemaGlobalClaim(
            string claimPath,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string? expectedState =
                PreSchemaGlobalClaimPending)
        {
            PreSchemaGlobalClaim pending =
                ReadPreSchemaGlobalClaim(claimPath);
            if (!PreSchemaGlobalClaimMatches(
                pending,
                globalPath,
                targetPath,
                scope,
                sourceSha256) ||
                (expectedState != null &&
                 !string.Equals(
                    pending.State,
                    expectedState,
                    StringComparison.Ordinal)))
            {
                throw new InvalidDataException(
                    "Another save or source revision already owns the pending pre-schema global migration claim.");
            }
        }

        private static void
            EnsureNoConflictingPublishedPreSchemaProduct(
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256)
        {
            string? targetDirectory =
                Path.GetDirectoryName(targetPath);
            string? scopedRoot =
                string.IsNullOrWhiteSpace(targetDirectory)
                    ? null
                    : Path.GetDirectoryName(targetDirectory);
            if (string.IsNullOrWhiteSpace(scopedRoot) ||
                !Directory.Exists(scopedRoot))
            {
                return;
            }

            string expectedFileName =
                Path.GetFileName(targetPath);
            var candidateLivePaths =
                new HashSet<string>(
                    Directory.GetFiles(
                        scopedRoot,
                        expectedFileName,
                        SearchOption.AllDirectories),
                    StringComparer.OrdinalIgnoreCase);
            foreach (string previousPath in
                Directory.GetFiles(
                    scopedRoot,
                    expectedFileName + ".previous",
                    SearchOption.AllDirectories))
            {
                candidateLivePaths.Add(
                    previousPath.Substring(
                        0,
                        previousPath.Length -
                            ".previous".Length));
            }

            foreach (string candidatePath in
                candidateLivePaths)
            {
                string previousPath =
                    candidatePath + ".previous";
                EquipmentSlotStorageFormatProbe liveFormat =
                    EquipmentSlotStorageFormatClassifier
                        .Probe(candidatePath);
                bool liveProduct =
                    liveFormat.Format ==
                    EquipmentSlotStorageFormat.ProductV3;
                bool mayUsePrevious =
                    liveFormat.Format ==
                        EquipmentSlotStorageFormat.Missing ||
                    liveFormat.Format ==
                        EquipmentSlotStorageFormat.Invalid;
                EquipmentSlotStorageFormatProbe previousFormat =
                    mayUsePrevious
                        ? EquipmentSlotStorageFormatClassifier
                            .Probe(previousPath)
                        : default;
                bool previousProduct =
                    mayUsePrevious &&
                    previousFormat.Format ==
                        EquipmentSlotStorageFormat.ProductV3;
                if (!liveProduct && !previousProduct)
                {
                    continue;
                }

                EquipmentSlotStorageDocument seed;
                try
                {
                    using (var stream =
                        File.OpenRead(
                            liveProduct
                                ? candidatePath
                                : previousPath))
                    {
                        seed =
                            (EquipmentSlotStorageDocument)
                                (CreateSerializer()
                                    .ReadObject(stream)
                                 ?? throw new InvalidDataException(
                                     "The published Product claim candidate was empty."));
                    }
                    seed.Normalize();
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException(
                        "A published Product claim candidate could not be inspected safely.",
                        ex);
                }

                if (seed.Scope == null ||
                    seed.Scope.ArchiveIndex < 0)
                {
                    continue;
                }
                string canonicalPath =
                    Path.Combine(
                        scopedRoot,
                        "slot-" +
                            seed.Scope.ArchiveIndex.ToString(
                                CultureInfo.InvariantCulture),
                        expectedFileName);
                if (!string.Equals(
                        Path.GetFullPath(candidatePath),
                        Path.GetFullPath(canonicalPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!TryLoadValidated(
                        candidatePath,
                        seed.Scope.Clone(),
                        out EquipmentSlotStorageDocument?
                            candidate,
                        out _,
                        out _))
                {
                    continue;
                }

                EquipmentSlotLegacyMigrationStamp? stamp =
                    candidate!.LegacyMigration;
                if (stamp == null ||
                    !string.Equals(
                        stamp.SourceKind,
                        EquipmentSlotLegacyMigrationSourceKinds
                            .PreSchemaGlobal,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(
                        Path.GetFullPath(candidatePath),
                        Path.GetFullPath(targetPath),
                        StringComparison.OrdinalIgnoreCase) &&
                    candidate.Scope.Matches(scope.Clone()) &&
                    string.Equals(
                        stamp.SourceSha256,
                        sourceSha256,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                throw new InvalidDataException(
                    "Another canonical save already contains an eligible published Product authority for a pre-schema global source; automatic winner selection is forbidden.");
            }
        }

        private void CompletePreSchemaGlobalClaim(
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            lock (PreSchemaGlobalClaimSync)
            {
                using (AcquirePreSchemaGlobalOperationLock(
                    globalPath))
                {
                    string claimPath =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalClaimPath(
                                globalPath,
                                sourceSha256);
                    string winnerPath =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalWinnerPath(
                                globalPath);
                    string claimDirectory =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalClaimDirectory(
                                globalPath);
                    if (File.Exists(winnerPath))
                    {
                        PreSchemaGlobalClaim completedWinner =
                            ReadPreSchemaGlobalClaim(
                                winnerPath);
                        if (string.Equals(
                                completedWinner.State,
                                PreSchemaGlobalClaimCompleted,
                                StringComparison.Ordinal))
                        {
                            if (!PreSchemaGlobalClaimMatchesSaveIdentity(
                                    completedWinner,
                                    globalPath,
                                    targetPath,
                                    scope,
                                    sourceSha256))
                            {
                                throw new InvalidDataException(
                                    "The completed pre-schema global winner belongs to another source or save identity.");
                            }
                            ValidateCompletedPreSchemaGlobalTerminalAuthorities(
                                globalPath,
                                targetPath,
                                completedWinner.Scope.Clone(),
                                sourceSha256);
                            TryPublishCompletedPreSchemaGlobalEvidence(
                                claimPath,
                                globalPath,
                                targetPath,
                                completedWinner.Scope.Clone(),
                                sourceSha256,
                                "evidence");
                            return;
                        }
                    }
                    ReconcilePreSchemaClaimTransition(
                        winnerPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    bool pendingWinner = false;
                    if (File.Exists(winnerPath))
                    {
                        ValidateExactPreSchemaGlobalClaim(
                            winnerPath,
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256,
                            expectedState: null);
                        pendingWinner =
                            string.Equals(
                                ReadPreSchemaGlobalClaim(
                                    winnerPath).State,
                                PreSchemaGlobalClaimPending,
                                StringComparison.Ordinal);
                        if (pendingWinner)
                        {
                            EnsureNoConflictingPublishedPreSchemaProduct(
                                targetPath,
                                scope,
                                sourceSha256);
                        }
                    }
                    else
                    {
                        ReconcilePreSchemaClaimTransitions(
                            claimDirectory,
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256);
                        ValidatePendingPreSchemaGlobalClaims(
                            claimDirectory,
                            globalPath,
                            targetPath,
                            scope,
                            sourceSha256,
                            expectedState: null);
                        EnsureNoConflictingPublishedPreSchemaProduct(
                            targetPath,
                            scope,
                            sourceSha256);
                    }
                    ValidateCompletedPreSchemaGlobalAuthorities(
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);

                    migrationFault?.Invoke(
                        "after-preschema-completion-validation-before-publish");
                    ValidateCompletedPreSchemaGlobalAuthorities(
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    if (pendingWinner)
                    {
                        EnsureNoConflictingPublishedPreSchemaProduct(
                            targetPath,
                            scope,
                            sourceSha256);
                    }
                    PublishCompletedPreSchemaGlobalClaim(
                        winnerPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256,
                        "winner");
                    TryPublishCompletedPreSchemaGlobalEvidence(
                        claimPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256,
                        "evidence");
                    migrationFault?.Invoke(
                        "after-preschema-completed-claim-publish");
                    ValidateCompletedPreSchemaGlobalAuthorities(
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256);
                    ValidateExactPreSchemaGlobalClaim(
                        winnerPath,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256,
                        PreSchemaGlobalClaimCompleted);
                }
            }
        }

        private void
            TryPublishCompletedPreSchemaGlobalEvidence(
                string claimPath,
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256,
                string transitionName)
        {
            if (File.Exists(claimPath) &&
                !ClaimFileMatches(
                    claimPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    expectedState: null))
            {
                return;
            }

            string directory =
                Path.GetDirectoryName(claimPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global claim path has no directory.");
            if (!File.Exists(claimPath) &&
                Directory.Exists(directory))
            {
                string[] transitions =
                    Directory.GetFiles(
                        directory,
                        Path.GetFileName(claimPath) +
                            ".transition-*",
                        SearchOption.TopDirectoryOnly);
                if (transitions.Length != 0 &&
                    (transitions.Length != 1 ||
                     !ClaimFileMatches(
                         transitions[0],
                         globalPath,
                         targetPath,
                         scope,
                         sourceSha256,
                         PreSchemaGlobalClaimPending)))
                {
                    return;
                }
            }

            migrationFault?.Invoke(
                "before-preschema-optional-evidence-publish");
            try
            {
                PublishCompletedPreSchemaGlobalClaim(
                    claimPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    transitionName);
            }
            catch
            {
                ValidateExactPreSchemaGlobalClaim(
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalWinnerPath(
                            globalPath),
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    PreSchemaGlobalClaimCompleted);
                ValidateCompletedPreSchemaGlobalTerminalAuthorities(
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256);
            }
        }

        private static bool ClaimFileMatches(
            string claimPath,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string? expectedState)
        {
            try
            {
                PreSchemaGlobalClaim claim =
                    ReadPreSchemaGlobalClaim(claimPath);
                return PreSchemaGlobalClaimMatches(
                        claim,
                        globalPath,
                        targetPath,
                        scope,
                        sourceSha256) &&
                    (expectedState == null ||
                     string.Equals(
                         claim.State,
                         expectedState,
                         StringComparison.Ordinal));
            }
            catch
            {
                return false;
            }
        }

        private void PublishCompletedPreSchemaGlobalClaim(
            string claimPath,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string transitionName)
        {
            ReconcilePreSchemaClaimTransition(
                claimPath,
                globalPath,
                targetPath,
                scope,
                sourceSha256);
            if (!File.Exists(claimPath))
            {
                var completed =
                    new PreSchemaGlobalClaim
                    {
                        SourceSha256 =
                            sourceSha256,
                        GlobalFileName =
                            Path.GetFileName(
                                globalPath),
                        TargetFileName =
                            Path.GetFileName(
                                targetPath),
                        Scope =
                            scope.Clone(),
                        State =
                            PreSchemaGlobalClaimCompleted
                    };
                PublishNewPreSchemaGlobalClaim(
                    claimPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    PreSchemaGlobalClaimCompleted,
                    completed);
                return;
            }

            ValidateExactPreSchemaGlobalClaim(
                claimPath,
                globalPath,
                targetPath,
                scope,
                sourceSha256,
                expectedState: null);
            PreSchemaGlobalClaim claim =
                ReadPreSchemaGlobalClaim(
                    claimPath);
            if (string.Equals(
                claim.State,
                PreSchemaGlobalClaimCompleted,
                StringComparison.Ordinal))
            {
                return;
            }
            if (!string.Equals(
                claim.State,
                PreSchemaGlobalClaimPending,
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The pre-schema global claim has an unsupported completion state.");
            }

            string directory =
                Path.GetDirectoryName(claimPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global claim path has no directory.");
            Directory.CreateDirectory(
                directory);
            string temporaryPath =
                Path.Combine(
                    directory,
                    ".tmp-completed-" +
                    Guid.NewGuid().ToString("N"));
            string transitionPath =
                claimPath +
                ".transition-" +
                Guid.NewGuid().ToString("N");
            claim.State =
                PreSchemaGlobalClaimCompleted;
            bool transitionCaptured = false;
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreatePreSchemaGlobalClaimSerializer()
                        .WriteObject(
                            stream,
                            claim);
                    stream.Flush(
                        flushToDisk: true);
                }
                ValidateExactPreSchemaGlobalClaim(
                    temporaryPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    PreSchemaGlobalClaimCompleted);

                File.Move(
                    claimPath,
                    transitionPath);
                transitionCaptured = true;
                migrationFault?.Invoke(
                    "after-preschema-" +
                    transitionName +
                    "-transition-capture");
                ValidateExactPreSchemaGlobalClaim(
                    transitionPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    PreSchemaGlobalClaimPending);
                try
                {
                    File.Move(
                        temporaryPath,
                        claimPath);
                    migrationFault?.Invoke(
                        "after-preschema-" +
                        transitionName +
                        "-completed-publish");
                }
                catch (IOException ex)
                    when (File.Exists(claimPath))
                {
                    throw new InvalidDataException(
                        "A different pre-schema global claim appeared during the pending-to-completed transition; both authorities were retained.",
                        ex);
                }
                ValidateExactPreSchemaGlobalClaim(
                    claimPath,
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256,
                    PreSchemaGlobalClaimCompleted);
                File.Delete(
                    transitionPath);
                transitionCaptured = false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                if (transitionCaptured &&
                    File.Exists(transitionPath) &&
                    !File.Exists(claimPath))
                {
                    try
                    {
                        File.Move(
                            transitionPath,
                            claimPath);
                        transitionCaptured = false;
                    }
                    catch
                    {
                        // Both byte authorities remain discoverable. The
                        // next load reconciles only an exact transition.
                    }
                }
            }
        }

        private static void ReconcilePreSchemaClaimTransitions(
            string claimDirectory,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            if (!Directory.Exists(claimDirectory))
                return;
            foreach (string transitionPath in
                Directory.GetFiles(
                    claimDirectory,
                    "*.transition-*",
                    SearchOption.TopDirectoryOnly))
            {
                int marker =
                    transitionPath.LastIndexOf(
                        ".transition-",
                        StringComparison.Ordinal);
                if (marker <= 0)
                {
                    throw new InvalidDataException(
                        "An interrupted pre-schema claim transition has an invalid path.");
                }
                ReconcilePreSchemaClaimTransition(
                    transitionPath.Substring(
                        0,
                        marker),
                    globalPath,
                    targetPath,
                    scope,
                    sourceSha256);
            }
        }

        private static void ReconcilePreSchemaClaimTransition(
            string claimPath,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            string directory =
                Path.GetDirectoryName(claimPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global claim path has no directory.");
            if (!Directory.Exists(directory))
                return;
            string[] transitions =
                Directory.GetFiles(
                    directory,
                    Path.GetFileName(claimPath) +
                    ".transition-*",
                    SearchOption.TopDirectoryOnly);
            if (transitions.Length == 0)
                return;
            if (transitions.Length != 1)
            {
                throw new InvalidDataException(
                    "Multiple interrupted transitions exist for one pre-schema global claim.");
            }

            string transitionPath =
                transitions[0];
            ValidateExactPreSchemaGlobalClaim(
                transitionPath,
                globalPath,
                targetPath,
                scope,
                sourceSha256,
                PreSchemaGlobalClaimPending);
            if (!File.Exists(claimPath))
            {
                File.Move(
                    transitionPath,
                    claimPath);
                return;
            }

            ValidateExactPreSchemaGlobalClaim(
                claimPath,
                globalPath,
                targetPath,
                scope,
                sourceSha256,
                expectedState: null);
            PreSchemaGlobalClaim current =
                ReadPreSchemaGlobalClaim(
                    claimPath);
            if (!string.Equals(
                    current.State,
                    PreSchemaGlobalClaimCompleted,
                    StringComparison.Ordinal) &&
                !string.Equals(
                    current.State,
                    PreSchemaGlobalClaimPending,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The pre-schema global claim transition conflicts with an unsupported current state.");
            }
            File.Delete(
                transitionPath);
        }

        private static void
            ValidateCompletedPreSchemaGlobalAuthorities(
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256)
        {
            if (File.Exists(globalPath))
            {
                throw new InvalidDataException(
                    "The pending pre-schema global claim cannot complete while the active global source still exists.");
            }

            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    sourceSha256);
            if (!File.Exists(archivePath) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(archivePath),
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The pending pre-schema global claim cannot complete without its exact deterministic archive.");
            }

            if (!TryLoadValidated(
                    targetPath,
                    scope.Clone(),
                    out EquipmentSlotStorageDocument? product,
                    out _,
                    out string productFailure))
            {
                throw new InvalidDataException(
                    "The pending pre-schema global claim cannot complete without its exact Product authority: " +
                    productFailure);
            }
            EquipmentSlotLegacyMigrationStamp? stamp =
                product!.LegacyMigration;
            if (stamp == null ||
                !string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    stamp.SourceSha256,
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The Product authority does not carry the exact pending pre-schema global migration stamp.");
            }
        }

        private static void
            ValidateCompletedPreSchemaGlobalTerminalAuthorities(
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope winnerScope,
                string sourceSha256)
        {
            if (File.Exists(globalPath))
            {
                throw new InvalidDataException(
                    "The completed pre-schema global winner conflicts with a recreated active global source.");
            }

            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    sourceSha256);
            if (!File.Exists(archivePath) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(archivePath),
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The completed pre-schema global winner cannot prove its exact deterministic archive.");
            }

            EquipmentSlotStorageDocument product =
                ReadEligibleTerminalProductAuthority(
                    targetPath);
            if (!SaveIdentityMatches(
                    product.Scope,
                    winnerScope))
            {
                throw new InvalidDataException(
                    "The completed pre-schema global winner's Product authority drifted to another save identity.");
            }
            if (winnerScope.TotalGameSeconds.HasValue &&
                (!product.Scope.TotalGameSeconds.HasValue ||
                 product.Scope.TotalGameSeconds.Value <
                    winnerScope.TotalGameSeconds.Value))
            {
                throw new InvalidDataException(
                    "The completed pre-schema global winner's Product authority is older than the migration revision.");
            }

            EquipmentSlotLegacyMigrationStamp? stamp =
                product.LegacyMigration;
            if (stamp == null ||
                !string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    stamp.SourceSha256,
                    sourceSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The completed Product authority does not carry the exact pre-schema global migration stamp.");
            }
        }

        private static EquipmentSlotStorageDocument
            ReadEligibleTerminalProductAuthority(
                string targetPath)
        {
            if (TryReadTerminalProductGeneration(
                    targetPath,
                    out EquipmentSlotStorageDocument? live,
                    out string liveFailure))
            {
                return live!;
            }
            if (!IsPreviousFallbackEligible(
                    liveFailure))
            {
                throw new InvalidDataException(
                    "The completed Product live authority is not eligible for terminal fallback: " +
                    liveFailure);
            }

            string previousPath =
                targetPath + ".previous";
            if (TryReadTerminalProductGeneration(
                    previousPath,
                    out EquipmentSlotStorageDocument? previous,
                    out string previousFailure))
            {
                return previous!;
            }
            throw new InvalidDataException(
                "The completed Product terminal authority is invalid: live=" +
                liveFailure +
                "; previous=" +
                previousFailure);
        }

        private static bool
            TryReadTerminalProductGeneration(
                string path,
                out EquipmentSlotStorageDocument? document,
                out string failure)
        {
            document = null;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                failure = "file-missing";
                return false;
            }

            EquipmentSlotStorageDocument? seed;
            try
            {
                using (FileStream stream =
                    File.OpenRead(path))
                {
                    seed =
                        CreateSerializer().ReadObject(stream) as
                            EquipmentSlotStorageDocument;
                }
            }
            catch (Exception ex)
            {
                failure =
                    ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            if (seed?.Scope == null)
            {
                failure = "scope-mismatch";
                return false;
            }

            return TryReadValidated(
                path,
                seed.Scope.Clone(),
                out document,
                out failure);
        }

        private static bool SaveIdentityMatches(
            EquipmentSlotSaveScope left,
            EquipmentSlotSaveScope right)
        {
            EquipmentSlotSaveScope normalizedLeft =
                left.Clone();
            EquipmentSlotSaveScope normalizedRight =
                right.Clone();
            normalizedLeft.Normalize();
            normalizedRight.Normalize();
            return normalizedLeft.ArchiveIndex ==
                    normalizedRight.ArchiveIndex &&
                string.Equals(
                    normalizedLeft.PlayerName,
                    normalizedRight.PlayerName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    normalizedLeft.CustomPlayerName,
                    normalizedRight.CustomPlayerName,
                    StringComparison.Ordinal);
        }

        private static void
            EnsureNoPendingPreSchemaGlobalClaimBeforeEmpty(
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope)
        {
            if (string.IsNullOrWhiteSpace(globalPath))
                return;
            string claimDirectory =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimDirectory(
                        globalPath);
            string operationLockPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalOperationLockPath(
                        globalPath);
            bool claimDirectoryExisted =
                Directory.Exists(claimDirectory);
            bool operationLockExisted =
                File.Exists(operationLockPath);
            bool emptyStateAllowed = false;

            try
            {
                lock (PreSchemaGlobalClaimSync)
                {
                    using (AcquirePreSchemaGlobalOperationLock(
                        globalPath))
                    {
                        string? globalDirectory =
                            Path.GetDirectoryName(globalPath);
                        var allowedTerminalArchives =
                            new HashSet<string>(
                                StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrWhiteSpace(
                                globalDirectory) &&
                            Directory.Exists(globalDirectory) &&
                            Directory.GetFiles(
                                globalDirectory,
                                Path.GetFileName(globalPath) +
                                    ".migration-capture-*",
                                SearchOption.TopDirectoryOnly)
                            .Length != 0)
                        {
                            throw new InvalidDataException(
                                "Interrupted legacy global capture residue forbids empty equipment-slot authority creation.");
                        }

                        string winnerPath =
                            EquipmentSlotLegacyMigration
                                .GetPreSchemaGlobalWinnerPath(
                                    globalPath);
                        bool completedWinnerValidated =
                            false;
                        if (File.Exists(winnerPath))
                        {
                            PreSchemaGlobalClaim winner =
                                ReadPreSchemaGlobalClaim(
                                    winnerPath);
                            if (!PreSchemaGlobalClaimIsStructurallyValid(
                                    winner,
                                    globalPath))
                            {
                                throw new InvalidDataException(
                                    "The pre-schema global winner is invalid while empty state is being considered.");
                            }
                            bool targetsCurrentScope =
                                ClaimTargetsSaveIdentity(
                                    winner,
                                    targetPath,
                                    scope);
                            if (targetsCurrentScope)
                            {
                                throw new InvalidDataException(
                                    "A pending or completed pre-schema global claim targets the current save; empty authority creation is forbidden.");
                            }
                            if (string.Equals(
                                    winner.State,
                                    PreSchemaGlobalClaimCompleted,
                                    StringComparison.Ordinal))
                            {
                                string? targetDirectory =
                                    Path.GetDirectoryName(
                                        targetPath);
                                string? scopedRoot =
                                    string.IsNullOrWhiteSpace(
                                        targetDirectory)
                                        ? null
                                        : Path.GetDirectoryName(
                                            targetDirectory);
                                if (string.IsNullOrWhiteSpace(
                                    scopedRoot))
                                {
                                    throw new InvalidDataException(
                                        "The completed winner cannot prove its canonical scoped Product path.");
                                }
                                string winnerTargetPath =
                                    Path.Combine(
                                        scopedRoot,
                                        "slot-" +
                                            winner.Scope.ArchiveIndex
                                                .ToString(
                                                    CultureInfo
                                                        .InvariantCulture),
                                        winner.TargetFileName);
                                ValidateCompletedPreSchemaGlobalTerminalAuthorities(
                                    globalPath,
                                    winnerTargetPath,
                                    winner.Scope.Clone(),
                                    winner.SourceSha256);
                                allowedTerminalArchives.Add(
                                    Path.GetFullPath(
                                        EquipmentSlotLegacyMigration
                                            .GetGlobalArchivePath(
                                                globalPath,
                                                winner.SourceSha256)));
                                completedWinnerValidated =
                                    true;
                            }
                        }

                        if (!completedWinnerValidated)
                        {
                            string[] transitionPaths =
                                Directory.GetFiles(
                                    claimDirectory,
                                    "*.transition-*",
                                    SearchOption.TopDirectoryOnly);
                            foreach (string transitionPath in
                                transitionPaths)
                            {
                                PreSchemaGlobalClaim transition =
                                    ReadPreSchemaGlobalClaim(
                                        transitionPath);
                                if (!PreSchemaGlobalClaimIsStructurallyValid(
                                        transition,
                                        globalPath))
                                {
                                    throw new InvalidDataException(
                                        "An interrupted pre-schema global claim transition is invalid while empty state is being considered.");
                                }
                                if (ClaimTargetsSaveIdentity(
                                        transition,
                                        targetPath,
                                        scope))
                                {
                                    throw new InvalidDataException(
                                        "An interrupted pre-schema global claim transition targets the current save; empty authority creation is forbidden.");
                                }
                            }

                            string[] claimPaths =
                                Directory.GetFiles(
                                    claimDirectory,
                                    "*.json",
                                    SearchOption.TopDirectoryOnly);
                            foreach (string claimPath in claimPaths)
                            {
                                if (string.Equals(
                                        claimPath,
                                        winnerPath,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                PreSchemaGlobalClaim claim =
                                    ReadPreSchemaGlobalClaim(
                                        claimPath);
                                if (!PreSchemaGlobalClaimIsStructurallyValid(
                                        claim,
                                        globalPath))
                                {
                                    throw new InvalidDataException(
                                        "A pre-schema global claim is invalid while empty state is being considered.");
                                }
                                if (ClaimTargetsSaveIdentity(
                                        claim,
                                        targetPath,
                                        scope))
                                {
                                    throw new InvalidDataException(
                                        "A pending or completed pre-schema global claim targets the current save; empty authority creation is forbidden.");
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(
                                globalDirectory) &&
                            Directory.Exists(globalDirectory))
                        {
                            foreach (string archivePath in
                                Directory.GetFiles(
                                    globalDirectory,
                                    Path.GetFileName(globalPath) +
                                        ".migrated-product-v3-*",
                                    SearchOption.TopDirectoryOnly))
                            {
                                string fullArchivePath =
                                    Path.GetFullPath(archivePath);
                                if (allowedTerminalArchives.Contains(
                                        fullArchivePath) ||
                                    IsDeterministicGlobalArchiveBoundToGlobalFlatProduct(
                                        archivePath,
                                        globalPath,
                                        targetPath))
                                {
                                    continue;
                                }
                                throw new InvalidDataException(
                                    "Unbound deterministic legacy global archive residue forbids empty equipment-slot authority creation.");
                            }
                        }

                        string? currentTargetDirectory =
                            Path.GetDirectoryName(targetPath);
                        string currentBackupDirectory =
                            Path.Combine(
                                currentTargetDirectory
                                ?? throw new InvalidOperationException(
                                    "The current Product target has no directory."),
                                ".legacy-migrations");
                        if (Directory.Exists(
                                currentBackupDirectory) &&
                            Directory.GetFiles(
                                currentBackupDirectory,
                                "*.flat.json",
                                SearchOption.TopDirectoryOnly)
                            .Any(IsCanonicalLegacyBackupResidue))
                        {
                            throw new InvalidDataException(
                                "Current-scope legacy migration backup residue forbids empty equipment-slot authority creation.");
                        }
                        emptyStateAllowed = true;
                    }
                }
            }
            finally
            {
                if (emptyStateAllowed &&
                    !operationLockExisted)
                {
                    CleanupTransientEmptyOperationLock(
                        operationLockPath,
                        claimDirectory,
                        claimDirectoryExisted);
                }
            }
        }

        private static bool
            IsDeterministicGlobalArchiveBoundToGlobalFlatProduct(
                string archivePath,
                string globalPath,
                string targetPath)
        {
            string archivePrefix =
                Path.GetFileName(globalPath) +
                ".migrated-product-v3-";
            string archiveFileName =
                Path.GetFileName(archivePath);
            if (!archiveFileName.StartsWith(
                    archivePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string sourceSha256 =
                archiveFileName.Substring(
                    archivePrefix.Length);
            if (!IsSha256Text(sourceSha256) ||
                !string.Equals(
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(archivePath),
                    sourceSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string? targetDirectory =
                Path.GetDirectoryName(targetPath);
            string? scopedRoot =
                string.IsNullOrWhiteSpace(targetDirectory)
                    ? null
                    : Path.GetDirectoryName(targetDirectory);
            if (string.IsNullOrWhiteSpace(scopedRoot) ||
                !Directory.Exists(scopedRoot))
            {
                return false;
            }

            string expectedFileName =
                Path.GetFileName(targetPath);
            var candidateLivePaths =
                new HashSet<string>(
                    Directory.GetFiles(
                        scopedRoot,
                        expectedFileName,
                        SearchOption.AllDirectories),
                    StringComparer.OrdinalIgnoreCase);
            foreach (string previousPath in
                Directory.GetFiles(
                    scopedRoot,
                    expectedFileName + ".previous",
                    SearchOption.AllDirectories))
            {
                candidateLivePaths.Add(
                    previousPath.Substring(
                        0,
                        previousPath.Length -
                            ".previous".Length));
            }

            int matchingAuthorities = 0;
            foreach (string candidatePath in
                candidateLivePaths)
            {
                EquipmentSlotStorageDocument candidate;
                try
                {
                    candidate =
                        ReadEligibleTerminalProductAuthority(
                            candidatePath);
                }
                catch
                {
                    continue;
                }

                string canonicalPath =
                    Path.Combine(
                        scopedRoot,
                        "slot-" +
                            candidate.Scope.ArchiveIndex
                                .ToString(
                                    CultureInfo.InvariantCulture),
                        expectedFileName);
                if (!string.Equals(
                        Path.GetFullPath(candidatePath),
                        Path.GetFullPath(canonicalPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                EquipmentSlotLegacyMigrationStamp? stamp =
                    candidate.LegacyMigration;
                if (stamp == null ||
                    !string.Equals(
                        stamp.SourceKind,
                        EquipmentSlotLegacyMigrationSourceKinds
                            .GlobalFlat,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        stamp.SourceSha256,
                        sourceSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matchingAuthorities++;
                if (matchingAuthorities > 1)
                    return false;
            }
            return matchingAuthorities == 1;
        }

        private static bool IsSha256Text(string value)
        {
            if (value == null || value.Length != 64)
                return false;
            foreach (char character in value)
            {
                bool hexadecimal =
                    (character >= '0' && character <= '9') ||
                    (character >= 'A' && character <= 'F') ||
                    (character >= 'a' && character <= 'f');
                if (!hexadecimal)
                    return false;
            }
            return true;
        }

        private static bool ClaimTargetsSaveIdentity(
            PreSchemaGlobalClaim claim,
            string targetPath,
            EquipmentSlotSaveScope scope) =>
            string.Equals(
                claim.TargetFileName,
                Path.GetFileName(targetPath),
                StringComparison.Ordinal) &&
            SaveIdentityMatches(
                claim.Scope,
                scope);

        private static void CleanupTransientEmptyOperationLock(
            string operationLockPath,
            string claimDirectory,
            bool claimDirectoryExisted)
        {
            try
            {
                if (File.Exists(operationLockPath))
                {
                    File.Delete(operationLockPath);
                }
                if (!claimDirectoryExisted &&
                    Directory.Exists(claimDirectory) &&
                    !Directory.EnumerateFileSystemEntries(
                        claimDirectory).Any())
                {
                    Directory.Delete(claimDirectory);
                    string? parent =
                        Path.GetDirectoryName(claimDirectory);
                    if (!string.IsNullOrWhiteSpace(parent) &&
                        Directory.Exists(parent) &&
                        !Directory.EnumerateFileSystemEntries(
                            parent).Any())
                    {
                        Directory.Delete(parent);
                    }
                }
            }
            catch (IOException)
            {
                // A concurrent process acquired or populated the same lock
                // boundary after this true-empty check. Retain its artifacts.
            }
            catch (UnauthorizedAccessException)
            {
                // Failure to remove non-authoritative lock metadata must not
                // broaden the empty Product authority decision.
            }
        }

        private static bool IsCanonicalLegacyBackupResidue(
            string path)
        {
            string? directory =
                Path.GetDirectoryName(path);
            if (!string.Equals(
                    Path.GetFileName(directory),
                    ".legacy-migrations",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string fileName =
                Path.GetFileName(path);
            const string suffix =
                ".flat.json";
            if (!fileName.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string hash =
                fileName.Substring(
                    0,
                    fileName.Length - suffix.Length);
            if (hash.Length != 64)
                return false;
            foreach (char value in hash)
            {
                bool hexadecimal =
                    (value >= '0' && value <= '9') ||
                    (value >= 'A' && value <= 'F') ||
                    (value >= 'a' && value <= 'f');
                if (!hexadecimal)
                    return false;
            }
            return true;
        }

        private static bool
            PreSchemaGlobalClaimIsStructurallyValid(
                PreSchemaGlobalClaim claim,
                string globalPath)
        {
            if (claim == null ||
                claim.SchemaVersion != 1 ||
                claim.Scope == null ||
                !string.Equals(
                    claim.GlobalFileName,
                    Path.GetFileName(globalPath),
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(
                    claim.TargetFileName) ||
                claim.SourceSha256 == null ||
                claim.SourceSha256.Length != 64 ||
                (!string.Equals(
                    claim.State,
                    PreSchemaGlobalClaimPending,
                    StringComparison.Ordinal) &&
                 !string.Equals(
                    claim.State,
                    PreSchemaGlobalClaimCompleted,
                    StringComparison.Ordinal)))
            {
                return false;
            }
            foreach (char value in claim.SourceSha256)
            {
                bool hexadecimal =
                    (value >= '0' && value <= '9') ||
                    (value >= 'A' && value <= 'F') ||
                    (value >= 'a' && value <= 'f');
                if (!hexadecimal)
                    return false;
            }
            return true;
        }

        private static PreSchemaGlobalClaim
            ReadPreSchemaGlobalClaim(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    PreSchemaGlobalClaim claim =
                        (PreSchemaGlobalClaim)
                        (CreatePreSchemaGlobalClaimSerializer()
                            .ReadObject(stream)
                         ?? throw new InvalidDataException(
                             "The pending pre-schema global claim was empty."));
                    if (string.IsNullOrWhiteSpace(
                        claim.State))
                    {
                        claim.State =
                            PreSchemaGlobalClaimPending;
                    }
                    return claim;
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "The pending pre-schema global claim is invalid.",
                    ex);
            }
        }

        private static bool PreSchemaGlobalClaimMatches(
            PreSchemaGlobalClaim claim,
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256)
        {
            if (claim == null ||
                claim.SchemaVersion != 1 ||
                claim.Scope == null ||
                (!string.Equals(
                    claim.State,
                    PreSchemaGlobalClaimPending,
                    StringComparison.Ordinal) &&
                 !string.Equals(
                    claim.State,
                    PreSchemaGlobalClaimCompleted,
                    StringComparison.Ordinal)))
            {
                return false;
            }
            return string.Equals(
                    claim.SourceSha256,
                    sourceSha256,
                    StringComparison.Ordinal) &&
                string.Equals(
                    claim.GlobalFileName,
                    Path.GetFileName(globalPath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    claim.TargetFileName,
                    Path.GetFileName(targetPath),
                    StringComparison.Ordinal) &&
                claim.Scope.Matches(scope.Clone());
        }

        private static bool
            PreSchemaGlobalClaimMatchesSaveIdentity(
                PreSchemaGlobalClaim claim,
                string globalPath,
                string targetPath,
                EquipmentSlotSaveScope scope,
                string sourceSha256) =>
            PreSchemaGlobalClaimIsStructurallyValid(
                claim,
                globalPath) &&
            string.Equals(
                claim.SourceSha256,
                sourceSha256,
                StringComparison.Ordinal) &&
            string.Equals(
                claim.TargetFileName,
                Path.GetFileName(targetPath),
                StringComparison.Ordinal) &&
            SaveIdentityMatches(
                claim.Scope,
                scope);

        private static DataContractJsonSerializer
            CreatePreSchemaGlobalClaimSerializer() =>
            new DataContractJsonSerializer(
                typeof(PreSchemaGlobalClaim));

        [DataContract]
        private sealed class PreSchemaGlobalClaim
        {
            [DataMember(Name = "schemaVersion", Order = 1)]
            public int SchemaVersion { get; set; } = 1;

            [DataMember(Name = "sourceSha256", Order = 2)]
            public string SourceSha256 { get; set; } =
                string.Empty;

            [DataMember(Name = "globalFileName", Order = 3)]
            public string GlobalFileName { get; set; } =
                string.Empty;

            [DataMember(Name = "targetFileName", Order = 4)]
            public string TargetFileName { get; set; } =
                string.Empty;

            [DataMember(Name = "scope", Order = 5)]
            public EquipmentSlotSaveScope Scope { get; set; } =
                new EquipmentSlotSaveScope();

            [DataMember(Name = "state", Order = 6, EmitDefaultValue = false)]
            public string State { get; set; } =
                PreSchemaGlobalClaimPending;
        }
    }
}
