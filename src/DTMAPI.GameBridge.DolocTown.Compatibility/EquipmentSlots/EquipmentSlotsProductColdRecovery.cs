using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DTMAPI.MoreEquipmentSlots;
using ProductDocument =
    DTMAPI.MoreEquipmentSlots.EquipmentSlotStorageDocument;
using ProductDocumentStore =
    DTMAPI.MoreEquipmentSlots.EquipmentSlotDocumentStore;
using ProductEscrow =
    DTMAPI.MoreEquipmentSlots.EquipmentSlotEscrowEntry;
using ProductJournal =
    DTMAPI.MoreEquipmentSlots.EquipmentSlotTransactionJournal;
using ProductScope =
    DTMAPI.MoreEquipmentSlots.EquipmentSlotSaveScope;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        private const string MoreEquipmentSlotsProductOwner =
            "DTMAPI.MoreEquipmentSlotsMod";
        private readonly Dictionary<string, ProductColdRecoverySession>
            productColdRecoverySessions =
                new Dictionary<string, ProductColdRecoverySession>(
                    StringComparer.OrdinalIgnoreCase);

        private bool IsMoreEquipmentSlotsProductStoragePath(
            string path,
            EquipmentSlotSaveScope hostScope)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                hostScope == null ||
                !hostScope.HasArchive)
            {
                return false;
            }
            try
            {
                string expectedPath =
                    Path.Combine(
                        runtime.Paths.ConfigPath,
                        "protected-items",
                        "equipment-slots",
                        "slot-" +
                            hostScope.ArchiveIndex,
                        "equipment-slots-" +
                            MoreEquipmentSlotsProductOwner +
                            ".json");
                return string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(expectedPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsMoreEquipmentSlotsProductLoaded()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(
                assembly => string.Equals(
                    assembly.GetName().Name,
                    "DTMAPI.MoreEquipmentSlots",
                    StringComparison.OrdinalIgnoreCase));
        }

        private bool TryRecoverMoreEquipmentSlotsProductStorage(
            string path,
            EquipmentSlotSaveScope hostScope,
            out bool handled,
            out int recoveredCount,
            out string message)
        {
            handled =
                IsMoreEquipmentSlotsProductStoragePath(
                    path,
                    hostScope);
            recoveredCount = 0;
            message = string.Empty;
            if (!handled)
                return false;
            if (IsMoreEquipmentSlotsProductLoaded())
            {
                message =
                    "ProductNative assembly is loaded; dormant Compatibility Host refused to take over its storage.";
                return false;
            }
            EquipmentSlotStorageFormatProbe format =
                EquipmentSlotStorageFormatClassifier.Probe(
                    path);
            if (format.Format ==
                    EquipmentSlotStorageFormat.LegacyFlat ||
                format.Format ==
                    EquipmentSlotStorageFormat.PreSchemaGlobal)
            {
                handled = false;
                message =
                    "Recognized flat legacy equipment-slot storage; deferred to the frozen Compatibility parser.";
                return false;
            }
            bool mayUsePrevious =
                format.Format ==
                    EquipmentSlotStorageFormat.Missing ||
                format.Format ==
                    EquipmentSlotStorageFormat.Invalid;
            EquipmentSlotStorageFormatProbe previousFormat =
                mayUsePrevious
                    ? EquipmentSlotStorageFormatClassifier.Probe(
                        path + ".previous")
                    : default;
            bool previousProduct =
                mayUsePrevious &&
                previousFormat.Format ==
                    EquipmentSlotStorageFormat.ProductV3;
            if (format.Format !=
                    EquipmentSlotStorageFormat.ProductV3 &&
                !previousProduct)
            {
                message =
                    "The exact MoreEquipmentSlots path has an unsupported, ambiguous or invalid storage generation and was retained fail-closed: " +
                    format.Format +
                    "; schema=" +
                    format.SchemaVersion +
                    "; " +
                    format.Failure;
                return false;
            }
            var expectedScope =
                new ProductScope
                {
                    ArchiveIndex = hostScope.ArchiveIndex,
                    PlayerName = hostScope.PlayerName,
                    CustomPlayerName = hostScope.CustomPlayerName,
                    TotalGameSeconds =
                        hostScope.TotalGameSeconds >= 0
                            ? hostScope.TotalGameSeconds
                            : (long?)null
                };
            if (!TryReadProductStorageValidated(
                path,
                expectedScope,
                out ProductDocument? document,
                out string failure))
            {
                message =
                    "ProductNative sidecar validation failed closed: " +
                    failure;
                return false;
            }

            ProductDocument storage = document!;
            string fingerprint =
                GetProductNativeSaveFingerprint(
                    hostScope.ArchiveIndex);
            if (storage.GameplayCandidate != null)
            {
                EquipmentSlotGameplayRecoveryDecision
                    gameplayDecision =
                    EquipmentSlotGameplayCandidateCoordinator
                        .DecideRecovery(
                            storage,
                            fingerprint,
                            itemId => ObserveProductItem(
                                itemId,
                                fingerprint));
                if (gameplayDecision.Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .FailClosed)
                {
                    message =
                        "ProductNative gameplay candidate retained fail-closed without native replay: " +
                        gameplayDecision.Reason;
                    return false;
                }
                if (gameplayDecision.Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .DiscardUncommitted)
                {
                    EquipmentSlotGameplayCandidateCoordinator
                        .DiscardUncommitted(storage);
                    PersistProductStorage(
                        path,
                        storage,
                        "cold recovery discarded uncommitted gameplay candidate");
                }
                else
                {
                    if (gameplayDecision.Action ==
                        EquipmentSlotGameplayRecoveryAction
                            .PromoteCommitted)
                    {
                        EquipmentSlotGameplayCandidateCoordinator
                            .PromoteCommitted(
                                storage,
                                fingerprint);
                        PersistProductStorage(
                            path,
                            storage,
                            "cold recovery promoted proven gameplay candidate");
                    }
                    EquipmentSlotGameplayCandidateCoordinator
                        .FinalizeCommitted(storage);
                    PersistProductStorage(
                        path,
                        storage,
                        "cold recovery finalized proven gameplay candidate");
                }
            }
            if (!PrepareProductColdRecovery(
                path,
                storage,
                out message))
            {
                return false;
            }

            ProductJournal? journal =
                storage.Journal;
            if (journal == null)
            {
                message =
                    "ProductNative sidecar contains no recoverable item.";
                return true;
            }
            EquipmentSlotRecoveryDecision decision =
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    storage,
                    fingerprint,
                    itemId => ObserveProductItem(
                        itemId,
                        fingerprint));
            if (decision.Action ==
                EquipmentSlotRecoveryAction.FailClosed)
            {
                message =
                    "ProductNative journal retained fail-closed: " +
                    decision.Reason;
                return false;
            }
            if (decision.Action ==
                EquipmentSlotRecoveryAction.FinalizeCommitted)
            {
                bool finalizedReplacement =
                    journal.Replacements.Count > 0;
                if (journal.State ==
                    EquipmentSlotJournalState.Prepared)
                {
                    EquipmentSlotTransactionCoordinator
                        .PromoteCommittedTombstone(
                            storage,
                            fingerprint);
                    PersistProductStorage(
                        path,
                        storage,
                        "cold recovery inferred native commit");
                }
                EquipmentSlotTransactionCoordinator
                    .FinalizeCommitted(storage);
                PersistProductStorage(
                    path,
                    storage,
                    "cold recovery finalized committed tombstone");
                productColdRecoverySessions.Remove(path);
                if (finalizedReplacement)
                {
                    if (!PrepareProductColdRecovery(
                            path,
                            storage,
                            out string stagingMessage))
                    {
                        message = stagingMessage;
                        return false;
                    }
                    if (storage.Journal == null)
                    {
                        message =
                            "ProductNative replacement finalized; no remaining sidecar item required cold recovery.";
                        return true;
                    }
                    journal = storage.Journal;
                }
                else
                {
                    message =
                        "ProductNative committed journal finalized from exact native destination evidence.";
                    return true;
                }
            }
            else if (journal.Replacements.Count > 0)
            {
                EquipmentSlotTransactionCoordinator
                    .DiscardUncommittedReplacementForColdRecovery(
                        storage);
                PersistProductStorage(
                    path,
                    storage,
                    "cold recovery discarded uncommitted replacement intent");
                journal = storage.Journal;
                if (journal == null)
                {
                    message =
                        "Discarded uncommitted replacement intent; native inventory retained the incoming item.";
                    return true;
                }
            }

            if (journal == null)
            {
                message =
                    "ProductNative sidecar contains no recoverable journal.";
                return true;
            }

            if (journal.AttemptStarted)
            {
                EquipmentSlotTransactionCoordinator
                    .ResetFailedAttempt(storage);
                PersistProductStorage(
                    path,
                    storage,
                    "cold recovery reset unsaved prepared attempt");
            }

            EquipmentSlotTransactionCoordinator.StartAttempt(
                storage,
                fingerprint,
                itemId => ObserveProductItem(
                    itemId,
                    fingerprint));
            PersistProductStorage(
                path,
                storage,
                "cold recovery durable prepared journal");

            bool success = true;
            var outcomes = new List<string>();
            for (int index = 0;
                 index < storage.Journal!.Escrow.Count;
                 index++)
            {
                ProductEscrow escrow =
                    storage.Journal.Escrow[index];
                NativeEquipmentPlacement placement =
                    PlaceNativeItem(escrow.ItemId, 1);
                NativePlacementResult result =
                    ToProductPlacement(placement);
                NativeRecoveryObservation observation =
                    ObserveProductItem(
                        escrow.ItemId,
                        fingerprint);
                EquipmentSlotTransactionCoordinator.RecordPlacement(
                    storage.Journal,
                    index,
                    result,
                    observation);
                PersistProductStorage(
                    path,
                    storage,
                    "cold recovery placement outcome " + index);
                if (result.Success)
                {
                    recoveredCount++;
                    outcomes.Add(result.Message);
                }
                else
                {
                    success = false;
                    outcomes.Add(
                        "Retained " +
                        escrow.ItemId +
                        " in durable journal: " +
                        result.Message);
                }
            }

            productColdRecoverySessions[path] =
                new ProductColdRecoverySession(
                    path,
                    storage,
                    expectedScope);
            message = string.Join(" | ", outcomes);
            return success;
        }

        private static bool TryReadProductStorageValidated(
            string path,
            ProductScope expectedScope,
            out ProductDocument? document,
            out string failure)
        {
            bool valid = ProductDocumentStore.TryLoadValidated(
                path,
                expectedScope,
                out document,
                out string sourcePath,
                out failure);
            if (valid &&
                !string.Equals(
                    sourcePath,
                    path,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure =
                    "Recovered exact valid ProductNative previous generation.";
            }
            if (!valid &&
                string.Equals(
                    failure,
                    "scope-mismatch",
                    StringComparison.Ordinal))
            {
                ProductDocument? observed =
                    ReadJson<ProductDocument>(path);
                observed?.Normalize();
                if (observed != null)
                {
                    failure +=
                        " archive=" +
                        observed.Scope.ArchiveIndex +
                        "/" +
                        expectedScope.ArchiveIndex +
                        ";playerMatch=" +
                        string.Equals(
                            observed.Scope.PlayerName,
                            expectedScope.PlayerName,
                            StringComparison.Ordinal) +
                        ";playerLengths=" +
                        observed.Scope.PlayerName.Length +
                        "/" +
                        expectedScope.PlayerName.Length +
                        ";customMatch=" +
                        string.Equals(
                            observed.Scope.CustomPlayerName,
                            expectedScope.CustomPlayerName,
                            StringComparison.Ordinal) +
                        ";customLengths=" +
                        observed.Scope.CustomPlayerName.Length +
                        "/" +
                        expectedScope.CustomPlayerName.Length;
                }
            }
            return valid;
        }

        private bool PrepareProductColdRecovery(
            string path,
            ProductDocument storage,
            out string message)
        {
            message = string.Empty;
            if (storage.Journal != null)
                return true;
            int[] occupied =
                storage.Slots
                    .Where(slot => slot != null && slot.IsOccupied)
                    .Select(slot => slot.Index)
                    .Distinct()
                    .OrderBy(index => index)
                    .ToArray();
            if (occupied.Length == 0)
                return true;

            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                storage,
                occupied,
                EquipmentSlotTransactionOrigin
                    .OrphanRecovery);
            PersistProductStorage(
                path,
                storage,
                "cold recovery staged ProductNative slots");
            message =
                "Staged " +
                occupied.Length +
                " ProductNative slot item(s) in the exact product journal.";
            return true;
        }

        private void NotifyProductColdRecoverySaveSaving(int? slot)
        {
            foreach (ProductColdRecoverySession session in
                productColdRecoverySessions.Values.ToArray())
            {
                if (!slot.HasValue ||
                    slot.Value != session.Scope.ArchiveIndex ||
                    session.Document.Journal == null)
                {
                    continue;
                }
                PersistProductStorage(
                    session.Path,
                    session.Document,
                    "native SaveSaving retained prepared cold journal");
            }
        }

        private void CommitProductColdRecoveryAfterNativeSave(
            int? slot)
        {
            foreach (KeyValuePair<string, ProductColdRecoverySession>
                pair in productColdRecoverySessions.ToArray())
            {
                ProductColdRecoverySession session = pair.Value;
                ProductJournal? journal =
                    session.Document.Journal;
                if (!slot.HasValue ||
                    slot.Value != session.Scope.ArchiveIndex ||
                    journal == null ||
                    !journal.AttemptStarted ||
                    journal.Escrow.Any(item =>
                        !item.AttemptCompleted))
                {
                    continue;
                }

                EquipmentSlotSaveScope committedScope =
                    GetEquipmentSlotSaveScope();
                if (committedScope.ArchiveIndex !=
                        session.Document.Scope.ArchiveIndex ||
                    !string.Equals(
                        committedScope.PlayerName,
                        session.Document.Scope.PlayerName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        committedScope.CustomPlayerName,
                        session.Document.Scope.CustomPlayerName,
                        StringComparison.Ordinal) ||
                    committedScope.TotalGameSeconds < 0)
                {
                    throw new InvalidOperationException(
                        "Compatibility cold recovery refused to advance ProductNative storage because the post-save native identity was incomplete or changed: archive=" +
                        committedScope.ArchiveIndex +
                        "/" +
                        session.Document.Scope.ArchiveIndex +
                        ";player=" +
                        committedScope.PlayerName +
                        "/" +
                        session.Document.Scope.PlayerName +
                        ";custom=" +
                        committedScope.CustomPlayerName +
                        "/" +
                        session.Document.Scope.CustomPlayerName +
                        ";clock=" +
                        committedScope.TotalGameSeconds +
                        ".");
                }
                session.Document.Scope.TotalGameSeconds =
                    committedScope.TotalGameSeconds;
                journal.Scope =
                    session.Document.Scope.Clone();
                if (session.Document.GameplayCandidate != null)
                {
                    session.Document.GameplayCandidate.Scope =
                        session.Document.Scope.Clone();
                }
                string fingerprint =
                    GetProductNativeSaveFingerprint(slot.Value);
                EquipmentSlotTransactionCoordinator
                    .PromoteCommittedTombstone(
                        session.Document,
                        fingerprint);
                PersistProductStorage(
                    session.Path,
                    session.Document,
                    "native SaveSaved product committed tombstone");
                EquipmentSlotTransactionCoordinator
                    .FinalizeCommitted(session.Document);
                PersistProductStorage(
                    session.Path,
                    session.Document,
                    "native SaveSaved product journal cleanup");
                productColdRecoverySessions.Remove(pair.Key);
            }
        }

        private void ClearProductColdRecoverySessions()
        {
            productColdRecoverySessions.Clear();
        }

        private static NativePlacementResult ToProductPlacement(
            NativeEquipmentPlacement placement)
        {
            NativePlacementKind kind =
                placement.Kind ==
                    NativeEquipmentPlacementKind.Backpack
                    ? NativePlacementKind.Backpack
                    : placement.Kind ==
                        NativeEquipmentPlacementKind.Mail
                        ? NativePlacementKind.Mail
                        : NativePlacementKind.Failure;
            return new NativePlacementResult(
                kind,
                kind == NativePlacementKind.Backpack ? 1 : 0,
                kind == NativePlacementKind.Mail ? 1 : 0,
                placement.Message);
        }

        private static NativeRecoveryObservation ObserveProductItem(
            string itemId,
            string fingerprint)
        {
            Type? dolocApi =
                Type.GetType(
                    "DolocAPI, Assembly-CSharp",
                    throwOnError: false);
            return new NativeRecoveryObservation(
                fingerprint,
                CountNativeBackpackItem(dolocApi, itemId),
                dolocApi == null
                    ? 0
                    : CountPendingUnacceptedItemMail(
                        dolocApi,
                        itemId));
        }

        private static string GetProductNativeSaveFingerprint(
            int archiveIndex)
        {
            try
            {
                Type? dolocApi =
                    Type.GetType(
                        "DolocAPI, Assembly-CSharp",
                        throwOnError: false);
                object? persistence =
                    dolocApi == null
                        ? null
                        : DolocTownExperimentalBridgeApi
                            .ReadStaticMember(
                            dolocApi,
                            "dataPersistenceManager");
                object? handler =
                    persistence == null
                        ? null
                        : DolocTownExperimentalBridgeApi.ReadMember(
                            persistence,
                            "fileDataHandler");
                MethodInfo? getPath =
                    handler?.GetType().GetMethod(
                        "GetDataFullPath",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance,
                        binder: null,
                        types: new[] { typeof(int) },
                        modifiers: null);
                string? path =
                    getPath?.Invoke(
                        handler,
                        new object[] { archiveIndex }) as string;
                return EquipmentSlotNativeCommitFingerprint
                    .Compute(path);
            }
            catch
            {
                return
                    "native-v2|current=missing|prev=missing|bak=missing";
            }
        }

        private static void PersistProductStorage(
            string path,
            ProductDocument storage,
            string reason)
        {
            long previousGeneration = storage.Generation;
            storage.Generation++;
            try
            {
                new EquipmentSlotDocumentStore()
                    .WriteAtomic(path, storage);
            }
            catch (Exception ex)
            {
                storage.Generation = previousGeneration;
                throw new InvalidOperationException(
                    "Could not persist exact ProductNative cold recovery boundary '" +
                    reason +
                    "'.",
                    ex);
            }
        }

        private sealed class ProductColdRecoverySession
        {
            internal ProductColdRecoverySession(
                string path,
                ProductDocument document,
                ProductScope scope)
            {
                Path = path;
                Document = document;
                Scope = scope;
            }

            internal string Path { get; }

            internal ProductDocument Document { get; }

            internal ProductScope Scope { get; }
        }

    }
}
